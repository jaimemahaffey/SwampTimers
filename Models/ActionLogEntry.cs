namespace SwampTimers.Models;

/// <summary>
/// Represents a single action execution log entry.
/// </summary>
public class ActionLogEntry
{
	/// <summary>
	/// Unique identifier for this log entry.
	/// </summary>
	public Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>
	/// When the action was executed.
	/// </summary>
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// The timer that triggered this action.
	/// </summary>
	public Guid TimerId { get; set; }

	/// <summary>
	/// Display name of the timer.
	/// </summary>
	public string TimerName { get; set; } = string.Empty;

	/// <summary>
	/// The event type that triggered the action (e.g., "Activated", "Deactivated").
	/// </summary>
	public string EventType { get; set; } = string.Empty;

	/// <summary>
	/// The Home Assistant entity ID that was acted upon.
	/// </summary>
	public string EntityId { get; set; } = string.Empty;

	/// <summary>
	/// Description of the action performed (e.g., "Turn On", "Turn Off", "Call light.turn_on").
	/// </summary>
	public string Action { get; set; } = string.Empty;

	/// <summary>
	/// Whether the action executed successfully.
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Error message if the action failed.
	/// </summary>
	public string? ErrorMessage { get; set; }
}
