using SwampTimers.Models;

namespace SwampTimers.Services;

/// <summary>
/// Service for logging and retrieving timer action execution history.
/// </summary>
public interface IActionLogService
{
	/// <summary>
	/// Log an action execution.
	/// </summary>
	Task LogActionAsync(ActionLogEntry entry);

	/// <summary>
	/// Get recent log entries, newest first.
	/// </summary>
	/// <param name="count">Maximum number of entries to return.</param>
	Task<IEnumerable<ActionLogEntry>> GetRecentEntriesAsync(int count = 100);

	/// <summary>
	/// Clear all log entries.
	/// </summary>
	Task ClearAsync();
}
