using System.Text.Json.Serialization;

namespace SwampTimers.Models.HomeAssistant;

/// <summary>
/// Represents a Home Assistant entity state
/// </summary>
public class HaEntity
{
	/// <summary>
	/// The entity ID (e.g., "switch.living_room")
	/// </summary>
	[JsonPropertyName("entity_id")]
	public string EntityId { get; set; } = string.Empty;

	/// <summary>
	/// Current state (e.g., "on", "off", "unavailable")
	/// </summary>
	[JsonPropertyName("state")]
	public string State { get; set; } = string.Empty;

	/// <summary>
	/// Entity attributes including friendly_name, device_class, etc.
	/// </summary>
	[JsonPropertyName("attributes")]
	public Dictionary<string, object>? Attributes { get; set; }

	/// <summary>
	/// Last changed timestamp
	/// </summary>
	[JsonPropertyName("last_changed")]
	public DateTime? LastChanged { get; set; }

	/// <summary>
	/// Last updated timestamp
	/// </summary>
	[JsonPropertyName("last_updated")]
	public DateTime? LastUpdated { get; set; }

	/// <summary>
	/// Gets the friendly name from attributes, or entity_id if not available
	/// </summary>
	public string FriendlyName =>
		Attributes?.TryGetValue("friendly_name", out var name) == true
			? name?.ToString() ?? EntityId
			: EntityId;

	/// <summary>
	/// Gets the domain portion of the entity ID (e.g., "switch" from "switch.living_room")
	/// </summary>
	public string Domain =>
		EntityId.Contains('.') ? EntityId.Split('.')[0] : string.Empty;

	/// <summary>
	/// Gets the object ID portion of the entity ID (e.g., "living_room" from "switch.living_room")
	/// </summary>
	public string ObjectId =>
		EntityId.Contains('.') ? EntityId.Split('.')[1] : EntityId;

	/// <summary>
	/// Whether the entity is currently in an "on" state
	/// </summary>
	public bool IsOn => State.Equals("on", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Whether the entity is available (not unavailable or unknown)
	/// </summary>
	public bool IsAvailable =>
		!State.Equals("unavailable", StringComparison.OrdinalIgnoreCase) &&
		!State.Equals("unknown", StringComparison.OrdinalIgnoreCase);
}
