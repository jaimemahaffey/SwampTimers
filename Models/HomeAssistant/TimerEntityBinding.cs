namespace SwampTimers.Models.HomeAssistant;

/// <summary>
/// Binds Home Assistant actions to a timer's activation and deactivation events
/// </summary>
public class TimerEntityBinding
{
	/// <summary>
	/// Actions to execute when the timer activates (turns on)
	/// </summary>
	public List<EntityAction> OnActivate { get; set; } = new();

	/// <summary>
	/// Actions to execute when the timer deactivates (turns off)
	/// </summary>
	public List<EntityAction> OnDeactivate { get; set; } = new();

	/// <summary>
	/// Whether Home Assistant integration is enabled for this timer
	/// </summary>
	public bool IsEnabled { get; set; } = true;
}
