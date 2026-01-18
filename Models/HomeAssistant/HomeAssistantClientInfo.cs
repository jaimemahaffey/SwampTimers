namespace SwampTimers.Models.HomeAssistant;

/// <summary>
/// Stores information about which Home Assistant client is being used.
/// This is registered as a singleton so the UI can display connection status.
/// </summary>
public class HomeAssistantClientInfo
{
	/// <summary>
	/// True if using the real HomeAssistantClient, false if using MockHomeAssistantClient.
	/// </summary>
	public bool IsRealClient { get; set; }

	/// <summary>
	/// Display-friendly description of the client type.
	/// </summary>
	public string ClientType => IsRealClient ? "Real (Home Assistant API)" : "Mock (Testing Mode)";
}
