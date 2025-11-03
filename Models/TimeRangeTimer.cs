namespace BlazorMudApp.Models;

/// <summary>
/// Timer that turns on at a specific time and off at another specific time
/// </summary>
public class TimeRangeTimer : TimerSchedule
{
    public override string TimerType => "TimeRange";

    /// <summary>
    /// The time when this timer should turn on
    /// </summary>
    public TimeOnly OnTime { get; set; }

    /// <summary>
    /// The time when this timer should turn off
    /// </summary>
    public TimeOnly OffTime { get; set; }

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

        // Handle case where range spans midnight
        if (OffTime < OnTime)
        {
            return currentTime >= OnTime || currentTime <= OffTime;
        }

        return currentTime >= OnTime && currentTime <= OffTime;
    }

    public override DateTime? GetNextActivation(DateTime fromTime)
    {
        if (!IsEnabled)
            return null;

        var today = DateOnly.FromDateTime(fromTime);
        var currentTime = TimeOnly.FromDateTime(fromTime);

        // Try today first
        if (currentTime < OnTime && (ActiveDays.Count == 0 || ActiveDays.Contains(fromTime.DayOfWeek)))
        {
            return today.ToDateTime(OnTime);
        }

        // Look ahead up to 7 days
        for (int i = 1; i <= 7; i++)
        {
            var nextDate = today.AddDays(i);
            var nextDateTime = nextDate.ToDateTime(OnTime);

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

        var today = DateOnly.FromDateTime(fromTime);
        var currentTime = TimeOnly.FromDateTime(fromTime);

        // If range spans midnight and we're before off time, off time is today
        if (OffTime < OnTime && currentTime <= OffTime)
        {
            if (ActiveDays.Count == 0 || ActiveDays.Contains(fromTime.DayOfWeek))
            {
                return today.ToDateTime(OffTime);
            }
        }

        // Try today first
        if (currentTime < OffTime && (ActiveDays.Count == 0 || ActiveDays.Contains(fromTime.DayOfWeek)))
        {
            return today.ToDateTime(OffTime);
        }

        // Look ahead up to 7 days
        for (int i = 1; i <= 7; i++)
        {
            var nextDate = today.AddDays(i);
            var nextDateTime = nextDate.ToDateTime(OffTime);

            if (ActiveDays.Count == 0 || ActiveDays.Contains(nextDateTime.DayOfWeek))
            {
                return nextDateTime;
            }
        }

        return null;
    }
}
