using System.Text.Json;
using SwampTimers.Models;
using SwampTimers.Models.HomeAssistant;
using SwampTimers.Services.HomeAssistant;

namespace SwampTimers.Services;

/// <summary>
/// Background service that monitors timer schedules and executes Home Assistant actions
/// </summary>
public class TimerMonitoringService : BackgroundService
{
	private readonly ILogger<TimerMonitoringService> _logger;
	private readonly IServiceProvider _serviceProvider;
	private readonly IActionLogService _actionLogService;
	private readonly int _checkIntervalSeconds;
	private readonly Dictionary<int, bool> _lastActiveStates = new();

	public TimerMonitoringService(
		ILogger<TimerMonitoringService> logger,
		IServiceProvider serviceProvider,
		IActionLogService actionLogService,
		IConfiguration configuration)
	{
		_logger = logger;
		_serviceProvider = serviceProvider;
		_actionLogService = actionLogService;
		_checkIntervalSeconds = configuration.GetValue<int>("TimerMonitoring:IntervalSeconds", 30);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Timer Monitoring Service started. Checking every {Interval} seconds.", _checkIntervalSeconds);

		// Wait a bit before starting to allow app to initialize
		await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await CheckTimersAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while checking timers");
			}

			await Task.Delay(TimeSpan.FromSeconds(_checkIntervalSeconds), stoppingToken);
		}

		_logger.LogInformation("Timer Monitoring Service stopped.");
	}

	private async Task CheckTimersAsync()
	{
		using var scope = _serviceProvider.CreateScope();
		var timerService = scope.ServiceProvider.GetRequiredService<ITimerService>();
		var haClient = scope.ServiceProvider.GetService<IHomeAssistantClient>();

		var now = DateTime.Now;
		var enabledTimers = await timerService.GetEnabledAsync();

		_logger.LogDebug("Checking {Count} enabled timers at {Time}", enabledTimers.Count, now.ToString("yyyy-MM-dd HH:mm:ss"));

		foreach (var timer in enabledTimers)
		{
			bool isActive = timer.IsActiveAt(now);
			bool wasActive = _lastActiveStates.GetValueOrDefault(timer.Id, false);

			if (isActive != wasActive)
			{
				// State changed
				if (isActive)
				{
					_logger.LogInformation("Timer ACTIVATED: [{Type}] {Name} (ID: {Id})",
						timer.TimerType, timer.Name, timer.Id);

					var nextDeactivation = timer.GetNextDeactivation(now);
					if (nextDeactivation.HasValue)
					{
						_logger.LogInformation("  → Will deactivate at: {Time}",
							nextDeactivation.Value.ToString("yyyy-MM-dd HH:mm:ss"));
					}

					// Execute Home Assistant activation actions
					if (haClient != null && timer.HomeAssistantBinding?.IsEnabled == true)
					{
						await ExecuteActionsAsync(haClient, timer, timer.HomeAssistantBinding.OnActivate, true);
					}
				}
				else
				{
					_logger.LogInformation("Timer DEACTIVATED: [{Type}] {Name} (ID: {Id})",
						timer.TimerType, timer.Name, timer.Id);

					var nextActivation = timer.GetNextActivation(now);
					if (nextActivation.HasValue)
					{
						_logger.LogInformation("  → Next activation at: {Time}",
							nextActivation.Value.ToString("yyyy-MM-dd HH:mm:ss"));
					}

					// Execute Home Assistant deactivation actions
					if (haClient != null && timer.HomeAssistantBinding?.IsEnabled == true)
					{
						await ExecuteActionsAsync(haClient, timer, timer.HomeAssistantBinding.OnDeactivate, false);
					}
				}

				_lastActiveStates[timer.Id] = isActive;
			}
			else if (isActive)
			{
				// Still active, log every check (at debug level)
				_logger.LogDebug("Timer ACTIVE: [{Type}] {Name} (ID: {Id})",
					timer.TimerType, timer.Name, timer.Id);
			}
		}

		// Clean up states for deleted timers
		var currentTimerIds = enabledTimers.Select(t => t.Id).ToHashSet();
		var staleIds = _lastActiveStates.Keys.Where(id => !currentTimerIds.Contains(id)).ToList();
		foreach (var staleId in staleIds)
		{
			_lastActiveStates.Remove(staleId);
		}
	}

	private async Task ExecuteActionsAsync(
		IHomeAssistantClient client,
		TimerSchedule timer,
		List<EntityAction> actions,
		bool isActivation)
	{
		if (actions.Count == 0)
		{
			return;
		}

		var eventType = isActivation ? "Activated" : "Deactivated";
		_logger.LogInformation("Executing {Count} HA actions for timer '{Name}' ({Event})",
			actions.Count, timer.Name, eventType);

		foreach (var action in actions)
		{
			var logEntry = new ActionLogEntry
			{
				TimerId = Guid.Parse(timer.Id.ToString()),
				TimerName = timer.Name,
				EventType = eventType,
				EntityId = action.EntityId ?? string.Empty
			};

			try
			{
				bool success = action.ActionType switch
				{
					ActionType.Toggle => await ExecuteToggleAsync(client, action, isActivation),
					ActionType.ServiceCall => await ExecuteServiceCallAsync(client, action),
					_ => false
				};

				logEntry.Success = success;
				logEntry.Action = GetActionDescription(action, isActivation);

				if (success)
				{
					_logger.LogInformation("  ✓ {ActionType}: {EntityId}",
						action.ActionType, action.EntityId);
				}
				else
				{
					logEntry.ErrorMessage = "Action returned false";
					_logger.LogWarning("  ✗ Failed {ActionType}: {EntityId}",
						action.ActionType, action.EntityId);
				}
			}
			catch (Exception ex)
			{
				logEntry.Success = false;
				logEntry.ErrorMessage = ex.Message;
				logEntry.Action = GetActionDescription(action, isActivation);
				_logger.LogError(ex, "  ✗ Exception executing action for {EntityId}", action.EntityId);
			}

			// Log the action
			try
			{
				await _actionLogService.LogActionAsync(logEntry);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to write action log entry");
			}
		}
	}

	private static string GetActionDescription(EntityAction action, bool isActivation)
	{
		return action.ActionType switch
		{
			ActionType.Toggle => isActivation ? "Turn On" : "Turn Off",
			ActionType.ServiceCall => $"Call {action.ServiceDomain}.{action.ServiceName}",
			_ => "Unknown"
		};
	}

	private async Task<bool> ExecuteToggleAsync(IHomeAssistantClient client, EntityAction action, bool turnOn)
	{
		if (string.IsNullOrEmpty(action.EntityId))
		{
			_logger.LogWarning("Toggle action has no entity_id");
			return false;
		}

		return turnOn
			? await client.TurnOnAsync(action.EntityId)
			: await client.TurnOffAsync(action.EntityId);
	}

	private async Task<bool> ExecuteServiceCallAsync(IHomeAssistantClient client, EntityAction action)
	{
		if (string.IsNullOrEmpty(action.ServiceDomain) || string.IsNullOrEmpty(action.ServiceName))
		{
			_logger.LogWarning("Service call action missing domain or service name");
			return false;
		}

		Dictionary<string, object>? serviceData = null;

		// Parse service data JSON if provided
		if (!string.IsNullOrEmpty(action.ServiceDataJson))
		{
			try
			{
				serviceData = JsonSerializer.Deserialize<Dictionary<string, object>>(action.ServiceDataJson);
			}
			catch (JsonException ex)
			{
				_logger.LogWarning(ex, "Invalid JSON in service data: {Json}", action.ServiceDataJson);
			}
		}

		// Add entity_id to service data if specified
		if (!string.IsNullOrEmpty(action.EntityId))
		{
			serviceData ??= new Dictionary<string, object>();
			serviceData["entity_id"] = action.EntityId;
		}

		return await client.CallServiceAsync(action.ServiceDomain, action.ServiceName, serviceData);
	}
}
