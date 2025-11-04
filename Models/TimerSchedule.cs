namespace SwampTimers.Models;

/// <summary>
/// Base class for all timer-based schedules
/// </summary>
public abstract class TimerSchedule
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastModifiedAt { get; set; }

    /// <summary>
    /// Gets the timer type discriminator for storage
    /// </summary>
    public abstract string TimerType { get; }

    /// <summary>
    /// Determines if the timer should be active at the given time
    /// </summary>
    public abstract bool IsActiveAt(DateTime time);

    /// <summary>
    /// Gets the next time this timer will activate
    /// </summary>
    public abstract DateTime? GetNextActivation(DateTime fromTime);

    /// <summary>
    /// Gets the next time this timer will deactivate
    /// </summary>
    public abstract DateTime? GetNextDeactivation(DateTime fromTime);
}
