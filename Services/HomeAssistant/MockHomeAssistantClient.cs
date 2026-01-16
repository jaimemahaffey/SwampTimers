using Microsoft.Extensions.Logging;
using SwampTimers.Models.HomeAssistant;

namespace SwampTimers.Services.HomeAssistant;

/// <summary>
/// Mock Home Assistant client for development and testing
/// Provides fake entities when not running inside Home Assistant
/// </summary>
public class MockHomeAssistantClient : IHomeAssistantClient
{
	private readonly ILogger<MockHomeAssistantClient> _logger;
	private readonly Dictionary<string, bool> _entityStates = new();

	// Sample entities for development
	private readonly List<HaEntity> _mockEntities = new()
	{
		new HaEntity { EntityId = "switch.living_room", State = "off", Attributes = new() { ["friendly_name"] = "Living Room Switch" } },
		new HaEntity { EntityId = "switch.bedroom", State = "off", Attributes = new() { ["friendly_name"] = "Bedroom Switch" } },
		new HaEntity { EntityId = "switch.garage", State = "off", Attributes = new() { ["friendly_name"] = "Garage Switch" } },
		new HaEntity { EntityId = "switch.outdoor_lights", State = "off", Attributes = new() { ["friendly_name"] = "Outdoor Lights" } },
		new HaEntity { EntityId = "light.kitchen", State = "off", Attributes = new() { ["friendly_name"] = "Kitchen Light", ["brightness"] = 0 } },
		new HaEntity { EntityId = "light.living_room", State = "off", Attributes = new() { ["friendly_name"] = "Living Room Light", ["brightness"] = 0 } },
		new HaEntity { EntityId = "light.bedroom", State = "off", Attributes = new() { ["friendly_name"] = "Bedroom Light", ["brightness"] = 0 } },
		new HaEntity { EntityId = "fan.ceiling", State = "off", Attributes = new() { ["friendly_name"] = "Ceiling Fan", ["speed"] = "off" } },
		new HaEntity { EntityId = "cover.garage_door", State = "closed", Attributes = new() { ["friendly_name"] = "Garage Door" } },
		new HaEntity { EntityId = "climate.thermostat", State = "heat", Attributes = new() { ["friendly_name"] = "Thermostat", ["temperature"] = 72 } },
		new HaEntity { EntityId = "script.morning_routine", State = "off", Attributes = new() { ["friendly_name"] = "Morning Routine" } },
		new HaEntity { EntityId = "automation.motion_lights", State = "on", Attributes = new() { ["friendly_name"] = "Motion Lights" } },
		new HaEntity { EntityId = "scene.movie_mode", State = "scening", Attributes = new() { ["friendly_name"] = "Movie Mode" } },
	};

	public MockHomeAssistantClient(ILogger<MockHomeAssistantClient> logger)
	{
		_logger = logger;

		// Initialize entity states
		foreach (var entity in _mockEntities)
		{
			_entityStates[entity.EntityId] = entity.State == "on";
		}

		_logger.LogInformation("MockHomeAssistantClient initialized with {Count} mock entities", _mockEntities.Count);
	}

	public Task<bool> IsConnectedAsync()
	{
		_logger.LogDebug("MockHomeAssistantClient.IsConnectedAsync: returning true (mock mode)");
		return Task.FromResult(true);
	}

	public Task<List<HaEntity>> GetEntitiesAsync()
	{
		// Update states from our tracking dictionary
		foreach (var entity in _mockEntities)
		{
			if (_entityStates.TryGetValue(entity.EntityId, out var isOn))
			{
				entity.State = isOn ? "on" : "off";
			}
		}

		return Task.FromResult(_mockEntities.ToList());
	}

	public Task<List<HaEntity>> GetEntitiesByDomainAsync(params string[] domains)
	{
		var domainSet = new HashSet<string>(domains, StringComparer.OrdinalIgnoreCase);
		var filtered = _mockEntities
			.Where(e => domainSet.Contains(e.Domain))
			.ToList();

		return Task.FromResult(filtered);
	}

	public Task<HaEntity?> GetEntityStateAsync(string entityId)
	{
		var entity = _mockEntities.FirstOrDefault(e =>
			e.EntityId.Equals(entityId, StringComparison.OrdinalIgnoreCase));

		if (entity != null && _entityStates.TryGetValue(entity.EntityId, out var isOn))
		{
			entity.State = isOn ? "on" : "off";
		}

		return Task.FromResult(entity);
	}

	public Task<bool> TurnOnAsync(string entityId, Dictionary<string, object>? serviceData = null)
	{
		_logger.LogInformation("[MOCK] Turning ON entity: {EntityId}", entityId);
		_entityStates[entityId] = true;
		return Task.FromResult(true);
	}

	public Task<bool> TurnOffAsync(string entityId, Dictionary<string, object>? serviceData = null)
	{
		_logger.LogInformation("[MOCK] Turning OFF entity: {EntityId}", entityId);
		_entityStates[entityId] = false;
		return Task.FromResult(true);
	}

	public Task<bool> CallServiceAsync(string domain, string service, Dictionary<string, object>? serviceData = null)
	{
		_logger.LogInformation("[MOCK] Calling service: {Domain}.{Service} with data: {Data}",
			domain, service, serviceData != null ? string.Join(", ", serviceData.Select(kv => $"{kv.Key}={kv.Value}")) : "none");

		// Track entity state if entity_id is in service data
		if (serviceData?.TryGetValue("entity_id", out var entityIdObj) == true)
		{
			var entityId = entityIdObj?.ToString() ?? string.Empty;
			if (service.Contains("on", StringComparison.OrdinalIgnoreCase))
			{
				_entityStates[entityId] = true;
			}
			else if (service.Contains("off", StringComparison.OrdinalIgnoreCase))
			{
				_entityStates[entityId] = false;
			}
		}

		return Task.FromResult(true);
	}
}
