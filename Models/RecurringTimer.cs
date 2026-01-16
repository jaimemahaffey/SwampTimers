using PeriodicEvents.Proto;

namespace SwampTimers.Models;

/// <summary>
/// Recurring timer that schedules next occurrence based on completion date
/// Unlike time-of-day timers, this tracks individual occurrences and intervals
/// </summary>
public class RecurringTimer : TimerSchedule
{
	public override string TimerType => "Recurring";

	/// <summary>
	/// The protobuf PeriodicEvent definition
	/// </summary>
	public PeriodicEvent Event { get; set; } = new();

	/// <summary>
	/// Current pending occurrence (if any)
	/// </summary>
	public EventOccurrence? CurrentOccurrence { get; set; }

	/// <summary>
	/// Most recent completed occurrence (if any)
	/// </summary>
	public EventOccurrence? LastCompletedOccurrence { get; set; }

	/// <summary>
	/// Checks if the timer is currently active (has a pending occurrence due today or overdue)
	/// </summary>
	public override bool IsActiveAt(DateTime checkTime)
	{
		if (!IsEnabled)
			return false;

		if (CurrentOccurrence == null)
			return false;

		// Check if current occurrence is due or overdue
		var scheduledDate = ConvertProtoDateToDateTime(CurrentOccurrence.ScheduledDate);
		return checkTime.Date >= scheduledDate.Date;
	}

	/// <summary>
	/// Returns the next activation time (scheduled date of current occurrence)
	/// </summary>
	public override DateTime? GetNextActivation(DateTime fromTime)
	{
		if (!IsEnabled)
			return null;

		if (CurrentOccurrence == null)
			return null;

		var scheduledDate = ConvertProtoDateToDateTime(CurrentOccurrence.ScheduledDate);

		// If scheduled date is in the future, return it
		if (scheduledDate.Date >= fromTime.Date)
			return scheduledDate.Date;

		// Otherwise it's overdue, return the scheduled date
		return scheduledDate.Date;
	}

	/// <summary>
	/// For periodic timers, "deactivation" is when the occurrence is completed
	/// This returns null since we don't have a deactivation time until completion
	/// </summary>
	public override DateTime? GetNextDeactivation(DateTime fromTime)
	{
		// Periodic timers don't have automatic deactivation
		// They deactivate when manually completed
		return null;
	}

	/// <summary>
	/// Marks the current occurrence as complete and schedules the next one
	/// </summary>
	public void CompleteCurrentOccurrence(DateTime completionDate, string? notes = null)
	{
		if (CurrentOccurrence == null)
			throw new InvalidOperationException("No current occurrence to complete");

		// Update current occurrence
		CurrentOccurrence.Status = OccurrenceStatus.Completed;
		CurrentOccurrence.CompletedDate = ConvertDateTimeToProtoDate(completionDate);
		CurrentOccurrence.CompletedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(completionDate.ToUniversalTime());
		if (!string.IsNullOrWhiteSpace(notes))
			CurrentOccurrence.Notes = notes;

		// Save as last completed
		LastCompletedOccurrence = CurrentOccurrence;

		// Schedule next occurrence
		var nextDate = CalculateNextScheduledDate(completionDate);
		CurrentOccurrence = new EventOccurrence
		{
			Id = Guid.NewGuid().ToString(),
			PeriodicEventId = Event.Id,
			ScheduledDate = ConvertDateTimeToProtoDate(nextDate),
			Status = OccurrenceStatus.Pending,
			CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
		};

		// Update timer timestamp
		LastModifiedAt = DateTime.UtcNow;
	}

	/// <summary>
	/// Skips the current occurrence without completing it
	/// </summary>
	public void SkipCurrentOccurrence(string reason)
	{
		if (CurrentOccurrence == null)
			throw new InvalidOperationException("No current occurrence to skip");

		CurrentOccurrence.Status = OccurrenceStatus.Skipped;
		CurrentOccurrence.Notes = reason;

		// Schedule next based on original scheduled date (not today)
		var scheduledDate = ConvertProtoDateToDateTime(CurrentOccurrence.ScheduledDate);
		var nextDate = CalculateNextScheduledDate(scheduledDate);

		CurrentOccurrence = new EventOccurrence
		{
			Id = Guid.NewGuid().ToString(),
			PeriodicEventId = Event.Id,
			ScheduledDate = ConvertDateTimeToProtoDate(nextDate),
			Status = OccurrenceStatus.Pending,
			CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
		};

		LastModifiedAt = DateTime.UtcNow;
	}

	/// <summary>
	/// Calculates the next scheduled date based on the period type and value
	/// </summary>
	private DateTime CalculateNextScheduledDate(DateTime fromDate)
	{
		return Event.PeriodType switch
		{
			PeriodType.Days => fromDate.AddDays(Event.PeriodValue > 0 ? Event.PeriodValue : Event.PeriodDays),
			PeriodType.Weeks => fromDate.AddDays((Event.PeriodValue > 0 ? Event.PeriodValue : 1) * 7),
			PeriodType.Months => fromDate.AddMonths(Event.PeriodValue > 0 ? Event.PeriodValue : 1),
			PeriodType.Years => fromDate.AddYears(Event.PeriodValue > 0 ? Event.PeriodValue : 1),
			_ => fromDate.AddDays(Event.PeriodDays) // Fallback to period_days
		};
	}

	/// <summary>
	/// Helper to convert proto Date to DateTime
	/// </summary>
	private static DateTime ConvertProtoDateToDateTime(Date protoDate)
	{
		return new DateTime(protoDate.Year, protoDate.Month, protoDate.Day);
	}

	/// <summary>
	/// Helper to convert DateTime to proto Date
	/// </summary>
	private static Date ConvertDateTimeToProtoDate(DateTime dateTime)
	{
		return new Date
		{
			Year = dateTime.Year,
			Month = dateTime.Month,
			Day = dateTime.Day
		};
	}

	/// <summary>
	/// Initializes a new periodic timer with its first occurrence
	/// </summary>
	public void InitializeFirstOccurrence(DateTime startDate)
	{
		if (string.IsNullOrEmpty(Event.Id))
			Event.Id = Guid.NewGuid().ToString();

		CurrentOccurrence = new EventOccurrence
		{
			Id = Guid.NewGuid().ToString(),
			PeriodicEventId = Event.Id,
			ScheduledDate = ConvertDateTimeToProtoDate(startDate),
			Status = OccurrenceStatus.Pending,
			CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
		};
	}
}
