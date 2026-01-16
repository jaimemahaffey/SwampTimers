namespace SwampTimers.Models.HomeAssistant;

/// <summary>
/// Defines an action to perform on a Home Assistant entity
/// </summary>
public class EntityAction
{
	/// <summary>
	/// The entity ID (e.g., "switch.living_room", "light.bedroom")
	/// </summary>
	public string EntityId { get; set; } = string.Empty;

	/// <summary>
	/// The action type - either toggle the entity or call a custom service
	/// </summary>
	public ActionType ActionType { get; set; } = ActionType.Toggle;

	/// <summary>
	/// For custom service calls: the domain (e.g., "light", "climate", "script")
	/// </summary>
	public string? ServiceDomain { get; set; }

	/// <summary>
	/// For custom service calls: the service name (e.g., "turn_on", "set_temperature")
	/// </summary>
	public string? ServiceName { get; set; }

	/// <summary>
	/// Custom service call data as JSON (e.g., {"brightness": 255, "color_temp": 400})
	/// </summary>
	public string? ServiceDataJson { get; set; }
}

public enum ActionType
{
	/// <summary>
	/// Simple on/off based on timer state
	/// </summary>
	Toggle,

	/// <summary>
	/// Custom service call with optional data
	/// </summary>
	ServiceCall
}
