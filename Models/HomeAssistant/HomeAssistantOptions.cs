namespace SwampTimers.Models.HomeAssistant;

/// <summary>
/// Configuration options for Home Assistant integration
/// </summary>
public class HomeAssistantOptions
{
	/// <summary>
	/// The Home Assistant API URL (e.g., "http://supervisor/core/api")
	/// </summary>
	public string ApiUrl { get; set; } = "http://supervisor/core/api";

	/// <summary>
	/// The supervisor token for authentication (from SUPERVISOR_TOKEN env var)
	/// </summary>
	public string SupervisorToken { get; set; } = string.Empty;

	/// <summary>
	/// How often to check timer states (in seconds)
	/// </summary>
	public int UpdateInterval { get; set; } = 30;

	/// <summary>
	/// Whether Home Assistant integration is globally enabled
	/// </summary>
	public bool IsEnabled { get; set; } = true;
}
