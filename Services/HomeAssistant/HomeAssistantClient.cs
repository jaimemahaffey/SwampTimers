using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwampTimers.Models.HomeAssistant;

namespace SwampTimers.Services.HomeAssistant;

/// <summary>
/// HTTP client for the Home Assistant REST API
/// </summary>
public class HomeAssistantClient : IHomeAssistantClient
{
	private readonly HttpClient _httpClient;
	private readonly ILogger<HomeAssistantClient> _logger;
	private readonly HomeAssistantOptions _options;

	// Cache for entities
	private List<HaEntity>? _cachedEntities;
	private DateTime _lastCacheUpdate = DateTime.MinValue;
	private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);
	private readonly SemaphoreSlim _cacheLock = new(1, 1);

	public HomeAssistantClient(
		HttpClient httpClient,
		IOptions<HomeAssistantOptions> options,
		ILogger<HomeAssistantClient> logger)
	{
		_httpClient = httpClient;
		_options = options.Value;
		_logger = logger;

		// Configure base address if not already set
		if (_httpClient.BaseAddress == null && !string.IsNullOrEmpty(_options.ApiUrl))
		{
			_httpClient.BaseAddress = new Uri(_options.ApiUrl.TrimEnd('/') + "/");
		}

		// Configure authentication
		if (!string.IsNullOrEmpty(_options.SupervisorToken))
		{
			_httpClient.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", _options.SupervisorToken);
		}
	}

	public async Task<bool> IsConnectedAsync()
	{
		try
		{
			var response = await _httpClient.GetAsync("api/");
			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to connect to Home Assistant API");
			return false;
		}
	}

	public async Task<List<HaEntity>> GetEntitiesAsync()
	{
		await _cacheLock.WaitAsync();
		try
		{
			// Return cached entities if still valid
			if (_cachedEntities != null && DateTime.UtcNow - _lastCacheUpdate < _cacheExpiry)
			{
				return _cachedEntities;
			}

			var response = await _httpClient.GetAsync("api/states");
			response.EnsureSuccessStatusCode();

			var entities = await response.Content.ReadFromJsonAsync<List<HaEntity>>();
			_cachedEntities = entities ?? new List<HaEntity>();
			_lastCacheUpdate = DateTime.UtcNow;

			_logger.LogDebug("Fetched {Count} entities from Home Assistant", _cachedEntities.Count);
			return _cachedEntities;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to get entities from Home Assistant");
			return _cachedEntities ?? new List<HaEntity>();
		}
		finally
		{
			_cacheLock.Release();
		}
	}

	public async Task<List<HaEntity>> GetEntitiesByDomainAsync(params string[] domains)
	{
		var allEntities = await GetEntitiesAsync();
		var domainSet = new HashSet<string>(domains, StringComparer.OrdinalIgnoreCase);

		return allEntities
			.Where(e => domainSet.Contains(e.Domain))
			.OrderBy(e => e.FriendlyName)
			.ToList();
	}

	public async Task<HaEntity?> GetEntityStateAsync(string entityId)
	{
		try
		{
			var response = await _httpClient.GetAsync($"api/states/{entityId}");

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("Entity {EntityId} not found (status: {StatusCode})",
					entityId, response.StatusCode);
				return null;
			}

			return await response.Content.ReadFromJsonAsync<HaEntity>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to get state for entity {EntityId}", entityId);
			return null;
		}
	}

	public async Task<bool> TurnOnAsync(string entityId, Dictionary<string, object>? serviceData = null)
	{
		var domain = GetDomain(entityId);
		var data = serviceData ?? new Dictionary<string, object>();
		data["entity_id"] = entityId;

		// Use domain-specific service if available, otherwise use homeassistant.turn_on
		var serviceDomain = domain switch
		{
			"switch" or "light" or "fan" or "cover" or "climate" => domain,
			_ => "homeassistant"
		};

		return await CallServiceAsync(serviceDomain, "turn_on", data);
	}

	public async Task<bool> TurnOffAsync(string entityId, Dictionary<string, object>? serviceData = null)
	{
		var domain = GetDomain(entityId);
		var data = serviceData ?? new Dictionary<string, object>();
		data["entity_id"] = entityId;

		// Use domain-specific service if available, otherwise use homeassistant.turn_off
		var serviceDomain = domain switch
		{
			"switch" or "light" or "fan" or "cover" or "climate" => domain,
			_ => "homeassistant"
		};

		return await CallServiceAsync(serviceDomain, "turn_off", data);
	}

	public async Task<bool> CallServiceAsync(string domain, string service, Dictionary<string, object>? serviceData = null)
	{
		try
		{
			var url = $"api/services/{domain}/{service}";
			var content = serviceData != null
				? JsonContent.Create(serviceData)
				: null;

			_logger.LogInformation("Calling HA service: {Domain}.{Service} with data: {Data}",
				domain, service, serviceData != null ? JsonSerializer.Serialize(serviceData) : "null");

			var response = await _httpClient.PostAsync(url, content);

			if (response.IsSuccessStatusCode)
			{
				_logger.LogInformation("Successfully called {Domain}.{Service}", domain, service);
				return true;
			}

			var errorBody = await response.Content.ReadAsStringAsync();
			_logger.LogError("Failed to call {Domain}.{Service}: {StatusCode} - {Error}",
				domain, service, response.StatusCode, errorBody);
			return false;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Exception calling {Domain}.{Service}", domain, service);
			return false;
		}
	}

	private static string GetDomain(string entityId)
	{
		var dotIndex = entityId.IndexOf('.');
		return dotIndex > 0 ? entityId.Substring(0, dotIndex) : string.Empty;
	}
}
