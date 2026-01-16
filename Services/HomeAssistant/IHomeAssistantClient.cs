using SwampTimers.Models.HomeAssistant;

namespace SwampTimers.Services.HomeAssistant;

/// <summary>
/// Interface for interacting with the Home Assistant REST API
/// </summary>
public interface IHomeAssistantClient
{
	/// <summary>
	/// Check if the client is connected and authenticated to Home Assistant
	/// </summary>
	Task<bool> IsConnectedAsync();

	/// <summary>
	/// Get all entities from Home Assistant
	/// </summary>
	Task<List<HaEntity>> GetEntitiesAsync();

	/// <summary>
	/// Get entities filtered by domain(s) (e.g., "switch", "light")
	/// </summary>
	Task<List<HaEntity>> GetEntitiesByDomainAsync(params string[] domains);

	/// <summary>
	/// Get the current state of a specific entity
	/// </summary>
	Task<HaEntity?> GetEntityStateAsync(string entityId);

	/// <summary>
	/// Turn on an entity (calls homeassistant.turn_on or domain-specific service)
	/// </summary>
	Task<bool> TurnOnAsync(string entityId, Dictionary<string, object>? serviceData = null);

	/// <summary>
	/// Turn off an entity (calls homeassistant.turn_off or domain-specific service)
	/// </summary>
	Task<bool> TurnOffAsync(string entityId, Dictionary<string, object>? serviceData = null);

	/// <summary>
	/// Call a custom Home Assistant service
	/// </summary>
	Task<bool> CallServiceAsync(string domain, string service, Dictionary<string, object>? serviceData = null);
}
