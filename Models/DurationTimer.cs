namespace BlazorMudApp.Models;

/// <summary>
/// Timer that starts at a specific time and runs for a set duration
/// </summary>
public class DurationTimer : TimerSchedule
{
    public override string TimerType => "Duration";

    /// <summary>
    /// The time when this timer should start
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// How long the timer should run (in minutes)
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Days of week when this timer is active (0 = Sunday, 6 = Saturday)
    /// </summary>
    public List<DayOfWeek> ActiveDays { get; set; } = new();

    public override bool IsActiveAt(DateTime time)
    {
        if (!IsEnabled)
            return false;

        // Check if today is an active day
        if (ActiveDays.Count > 0 && !ActiveDays.Contains(time.DayOfWeek))
            return false;

        var currentTime = TimeOnly.FromDateTime(time);
        var endTime = StartTime.AddMinutes(DurationMinutes);

        // Handle case where duration spans midnight
        if (endTime < StartTime)
        {
            return currentTime >= StartTime || currentTime <= endTime;
        }

        return currentTime >= StartTime && currentTime <= endTime;
    }

    public override DateTime? GetNextActivation(DateTime fromTime)
    {
        if (!IsEnabled)
            return null;

        var today = DateOnly.FromDateTime(fromTime);
        var currentTime = TimeOnly.FromDateTime(fromTime);

        // Try today first
        if (currentTime < StartTime && (ActiveDays.Count == 0 || ActiveDays.Contains(fromTime.DayOfWeek)))
        {
            return today.ToDateTime(StartTime);
        }

        // Look ahead up to 7 days
        for (int i = 1; i <= 7; i++)
        {
            var nextDate = today.AddDays(i);
            var nextDateTime = nextDate.ToDateTime(StartTime);

            if (ActiveDays.Count == 0 || ActiveDays.Contains(nextDateTime.DayOfWeek))
            {
                return nextDateTime;
            }
        }

        return null;
    }

    public override DateTime? GetNextDeactivation(DateTime fromTime)
    {
        if (!IsEnabled)
            return null;

        var activation = GetNextActivation(fromTime);
        if (activation.HasValue)
        {
            return activation.Value.AddMinutes(DurationMinutes);
        }

        return null;
    }
}
