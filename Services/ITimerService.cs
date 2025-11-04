using SwampTimers.Models;

namespace SwampTimers.Services;

/// <summary>
/// Abstract interface for timer schedule storage and retrieval
/// </summary>
public interface ITimerService
{
    /// <summary>
    /// Gets all timer schedules
    /// </summary>
    Task<List<TimerSchedule>> GetAllAsync();

    /// <summary>
    /// Gets a specific timer by ID
    /// </summary>
    Task<TimerSchedule?> GetByIdAsync(int id);

    /// <summary>
    /// Gets all timers of a specific type
    /// </summary>
    Task<List<T>> GetByTypeAsync<T>() where T : TimerSchedule;

    /// <summary>
    /// Gets all enabled timers
    /// </summary>
    Task<List<TimerSchedule>> GetEnabledAsync();

    /// <summary>
    /// Gets all timers that are currently active
    /// </summary>
    Task<List<TimerSchedule>> GetActiveAsync(DateTime? atTime = null);

    /// <summary>
    /// Creates a new timer schedule
    /// </summary>
    Task<TimerSchedule> CreateAsync(TimerSchedule timer);

    /// <summary>
    /// Updates an existing timer schedule
    /// </summary>
    Task<TimerSchedule> UpdateAsync(TimerSchedule timer);

    /// <summary>
    /// Deletes a timer schedule
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Toggles the enabled state of a timer
    /// </summary>
    Task<bool> ToggleEnabledAsync(int id);

    /// <summary>
    /// Initializes the storage (creates tables, etc.)
    /// </summary>
    Task InitializeAsync();
}
