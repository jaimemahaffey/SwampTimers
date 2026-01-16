using PeriodicEvents.Proto;
using SwampTimers.Models;

namespace SwampTimers.Tests.Models;

public class RecurringTimerTests
{
	[Fact]
	public void TimerType_ReturnsRecurring()
	{
		var timer = new RecurringTimer();
		Assert.Equal("Recurring", timer.TimerType);
	}

	[Fact]
	public void IsActiveAt_ReturnsFalse_WhenDisabled()
	{
		var timer = CreateRecurringTimer(DateTime.Today, enabled: false);

		Assert.False(timer.IsActiveAt(DateTime.Today));
	}

	[Fact]
	public void IsActiveAt_ReturnsFalse_WhenNoCurrentOccurrence()
	{
		var timer = new RecurringTimer
		{
			IsEnabled = true,
			CurrentOccurrence = null
		};

		Assert.False(timer.IsActiveAt(DateTime.Now));
	}

	[Fact]
	public void IsActiveAt_ReturnsTrue_WhenOccurrenceIsDueToday()
	{
		var timer = CreateRecurringTimer(DateTime.Today);

		Assert.True(timer.IsActiveAt(DateTime.Now));
	}

	[Fact]
	public void IsActiveAt_ReturnsTrue_WhenOccurrenceIsOverdue()
	{
		var timer = CreateRecurringTimer(DateTime.Today.AddDays(-3));

		Assert.True(timer.IsActiveAt(DateTime.Now));
	}

	[Fact]
	public void IsActiveAt_ReturnsFalse_WhenOccurrenceIsInFuture()
	{
		var timer = CreateRecurringTimer(DateTime.Today.AddDays(5));

		Assert.False(timer.IsActiveAt(DateTime.Now));
	}

	[Fact]
	public void GetNextActivation_ReturnsNull_WhenDisabled()
	{
		var timer = CreateRecurringTimer(DateTime.Today, enabled: false);

		Assert.Null(timer.GetNextActivation(DateTime.Now));
	}

	[Fact]
	public void GetNextActivation_ReturnsNull_WhenNoCurrentOccurrence()
	{
		var timer = new RecurringTimer
		{
			IsEnabled = true,
			CurrentOccurrence = null
		};

		Assert.Null(timer.GetNextActivation(DateTime.Now));
	}

	[Fact]
	public void GetNextActivation_ReturnsScheduledDate_WhenInFuture()
	{
		var scheduledDate = DateTime.Today.AddDays(5);
		var timer = CreateRecurringTimer(scheduledDate);

		var result = timer.GetNextActivation(DateTime.Now);

		Assert.Equal(scheduledDate.Date, result);
	}

	[Fact]
	public void GetNextActivation_ReturnsScheduledDate_WhenOverdue()
	{
		var scheduledDate = DateTime.Today.AddDays(-3);
		var timer = CreateRecurringTimer(scheduledDate);

		var result = timer.GetNextActivation(DateTime.Now);

		Assert.Equal(scheduledDate.Date, result);
	}

	[Fact]
	public void GetNextDeactivation_AlwaysReturnsNull()
	{
		var timer = CreateRecurringTimer(DateTime.Today);

		Assert.Null(timer.GetNextDeactivation(DateTime.Now));
	}

	[Fact]
	public void CompleteCurrentOccurrence_ThrowsWhenNoCurrentOccurrence()
	{
		var timer = new RecurringTimer
		{
			IsEnabled = true,
			CurrentOccurrence = null
		};

		Assert.Throws<InvalidOperationException>(() =>
			timer.CompleteCurrentOccurrence(DateTime.Today));
	}

	[Fact]
	public void CompleteCurrentOccurrence_MarksAsCompleted()
	{
		var timer = CreateRecurringTimer(DateTime.Today);
		var completionDate = DateTime.Today;

		timer.CompleteCurrentOccurrence(completionDate, "Test notes");

		Assert.NotNull(timer.LastCompletedOccurrence);
		Assert.Equal(OccurrenceStatus.Completed, timer.LastCompletedOccurrence.Status);
		Assert.Equal("Test notes", timer.LastCompletedOccurrence.Notes);
	}

	[Fact]
	public void CompleteCurrentOccurrence_SchedulesNextOccurrence_Days()
	{
		var timer = CreateRecurringTimer(DateTime.Today, periodType: PeriodType.Days, periodValue: 7);
		var completionDate = DateTime.Today;

		timer.CompleteCurrentOccurrence(completionDate);

		Assert.NotNull(timer.CurrentOccurrence);
		Assert.Equal(OccurrenceStatus.Pending, timer.CurrentOccurrence.Status);

		var expectedNextDate = completionDate.AddDays(7);
		Assert.Equal(expectedNextDate.Year, timer.CurrentOccurrence.ScheduledDate.Year);
		Assert.Equal(expectedNextDate.Month, timer.CurrentOccurrence.ScheduledDate.Month);
		Assert.Equal(expectedNextDate.Day, timer.CurrentOccurrence.ScheduledDate.Day);
	}

	[Fact]
	public void CompleteCurrentOccurrence_SchedulesNextOccurrence_Weeks()
	{
		var timer = CreateRecurringTimer(DateTime.Today, periodType: PeriodType.Weeks, periodValue: 2);
		var completionDate = DateTime.Today;

		timer.CompleteCurrentOccurrence(completionDate);

		var expectedNextDate = completionDate.AddDays(14);
		Assert.Equal(expectedNextDate.Year, timer.CurrentOccurrence!.ScheduledDate.Year);
		Assert.Equal(expectedNextDate.Month, timer.CurrentOccurrence.ScheduledDate.Month);
		Assert.Equal(expectedNextDate.Day, timer.CurrentOccurrence.ScheduledDate.Day);
	}

	[Fact]
	public void CompleteCurrentOccurrence_SchedulesNextOccurrence_Months()
	{
		var timer = CreateRecurringTimer(DateTime.Today, periodType: PeriodType.Months, periodValue: 1);
		var completionDate = DateTime.Today;

		timer.CompleteCurrentOccurrence(completionDate);

		var expectedNextDate = completionDate.AddMonths(1);
		Assert.Equal(expectedNextDate.Year, timer.CurrentOccurrence!.ScheduledDate.Year);
		Assert.Equal(expectedNextDate.Month, timer.CurrentOccurrence.ScheduledDate.Month);
		Assert.Equal(expectedNextDate.Day, timer.CurrentOccurrence.ScheduledDate.Day);
	}

	[Fact]
	public void CompleteCurrentOccurrence_SchedulesNextOccurrence_Years()
	{
		var timer = CreateRecurringTimer(DateTime.Today, periodType: PeriodType.Years, periodValue: 1);
		var completionDate = DateTime.Today;

		timer.CompleteCurrentOccurrence(completionDate);

		var expectedNextDate = completionDate.AddYears(1);
		Assert.Equal(expectedNextDate.Year, timer.CurrentOccurrence!.ScheduledDate.Year);
		Assert.Equal(expectedNextDate.Month, timer.CurrentOccurrence.ScheduledDate.Month);
		Assert.Equal(expectedNextDate.Day, timer.CurrentOccurrence.ScheduledDate.Day);
	}

	[Fact]
	public void SkipCurrentOccurrence_ThrowsWhenNoCurrentOccurrence()
	{
		var timer = new RecurringTimer
		{
			IsEnabled = true,
			CurrentOccurrence = null
		};

		Assert.Throws<InvalidOperationException>(() =>
			timer.SkipCurrentOccurrence("Test reason"));
	}

	[Fact]
	public void SkipCurrentOccurrence_SchedulesNextBasedOnOriginalDate()
	{
		var originalDate = DateTime.Today;
		var timer = CreateRecurringTimer(originalDate, periodType: PeriodType.Days, periodValue: 7);

		timer.SkipCurrentOccurrence("Skipping for vacation");

		Assert.NotNull(timer.CurrentOccurrence);
		Assert.Equal(OccurrenceStatus.Pending, timer.CurrentOccurrence.Status);

		// Next occurrence is based on original scheduled date, not today
		var expectedNextDate = originalDate.AddDays(7);
		Assert.Equal(expectedNextDate.Year, timer.CurrentOccurrence.ScheduledDate.Year);
		Assert.Equal(expectedNextDate.Month, timer.CurrentOccurrence.ScheduledDate.Month);
		Assert.Equal(expectedNextDate.Day, timer.CurrentOccurrence.ScheduledDate.Day);
	}

	[Fact]
	public void InitializeFirstOccurrence_CreatesOccurrence()
	{
		var timer = new RecurringTimer
		{
			Name = "Test Timer",
			Event = new PeriodicEvent { Name = "Test Event" }
		};
		var startDate = DateTime.Today;

		timer.InitializeFirstOccurrence(startDate);

		Assert.NotNull(timer.CurrentOccurrence);
		Assert.Equal(OccurrenceStatus.Pending, timer.CurrentOccurrence.Status);
		Assert.Equal(startDate.Year, timer.CurrentOccurrence.ScheduledDate.Year);
		Assert.Equal(startDate.Month, timer.CurrentOccurrence.ScheduledDate.Month);
		Assert.Equal(startDate.Day, timer.CurrentOccurrence.ScheduledDate.Day);
	}

	[Fact]
	public void InitializeFirstOccurrence_GeneratesEventIdIfEmpty()
	{
		var timer = new RecurringTimer
		{
			Name = "Test Timer",
			Event = new PeriodicEvent { Name = "Test Event", Id = "" }
		};

		timer.InitializeFirstOccurrence(DateTime.Today);

		Assert.False(string.IsNullOrEmpty(timer.Event.Id));
	}

	[Fact]
	public void InitializeFirstOccurrence_PreservesExistingEventId()
	{
		var existingId = "existing-event-id";
		var timer = new RecurringTimer
		{
			Name = "Test Timer",
			Event = new PeriodicEvent { Name = "Test Event", Id = existingId }
		};

		timer.InitializeFirstOccurrence(DateTime.Today);

		Assert.Equal(existingId, timer.Event.Id);
	}

	private static RecurringTimer CreateRecurringTimer(
		DateTime scheduledDate,
		bool enabled = true,
		PeriodType periodType = PeriodType.Days,
		int periodValue = 7)
	{
		var timer = new RecurringTimer
		{
			Id = 1,
			Name = "Test Recurring Timer",
			IsEnabled = enabled,
			Event = new PeriodicEvent
			{
				Id = Guid.NewGuid().ToString(),
				Name = "Test Event",
				PeriodType = periodType,
				PeriodValue = periodValue
			}
		};

		timer.CurrentOccurrence = new EventOccurrence
		{
			Id = Guid.NewGuid().ToString(),
			PeriodicEventId = timer.Event.Id,
			ScheduledDate = new Date
			{
				Year = scheduledDate.Year,
				Month = scheduledDate.Month,
				Day = scheduledDate.Day
			},
			Status = OccurrenceStatus.Pending
		};

		return timer;
	}
}
