using SwampTimers.Models;

namespace SwampTimers.Tests.Models;

public class DurationTimerTests
{
	[Fact]
	public void TimerType_ShouldReturnDuration()
	{
		// Arrange
		var timer = new DurationTimer();

		// Act
		var result = timer.TimerType;

		// Assert
		Assert.Equal("Duration", result);
	}

	[Fact]
	public void IsActiveAt_WhenDisabled_ShouldReturnFalse()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = false,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var testTime = new DateTime(2026, 1, 12, 10, 30, 0);

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void IsActiveAt_WhenInactiveDayOfWeek_ShouldReturnFalse()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60,
			ActiveDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday }
		};
		// Sunday
		var testTime = new DateTime(2026, 1, 11, 10, 30, 0);

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void IsActiveAt_WhenActiveDayOfWeek_ShouldReturnTrue()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60,
			ActiveDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday }
		};
		// Monday
		var testTime = new DateTime(2026, 1, 12, 10, 30, 0);

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsActiveAt_WhenNoActiveDays_ShouldRunEveryDay()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60,
			ActiveDays = new List<DayOfWeek>() // Empty = all days
		};
		var testTime = new DateTime(2026, 1, 11, 10, 30, 0); // Sunday

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsActiveAt_DuringActiveWindow_ShouldReturnTrue()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var testTime = new DateTime(2026, 1, 12, 10, 30, 0); // 10:30 AM

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsActiveAt_BeforeActiveWindow_ShouldReturnFalse()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var testTime = new DateTime(2026, 1, 12, 9, 30, 0); // 9:30 AM

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void IsActiveAt_AfterActiveWindow_ShouldReturnFalse()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var testTime = new DateTime(2026, 1, 12, 11, 30, 0); // 11:30 AM

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void IsActiveAt_AtStartTime_ShouldReturnTrue()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var testTime = new DateTime(2026, 1, 12, 10, 0, 0); // Exactly 10:00 AM

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsActiveAt_AtEndTime_ShouldReturnTrue()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var testTime = new DateTime(2026, 1, 12, 11, 0, 0); // Exactly 11:00 AM (end time)

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsActiveAt_SpanningMidnight_BeforeMidnight_ShouldReturnTrue()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(23, 0), // 11 PM
			DurationMinutes = 120 // 2 hours (ends at 1 AM)
		};
		var testTime = new DateTime(2026, 1, 12, 23, 30, 0); // 11:30 PM

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsActiveAt_SpanningMidnight_AfterMidnight_ShouldReturnTrue()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(23, 0), // 11 PM
			DurationMinutes = 120 // 2 hours (ends at 1 AM)
		};
		var testTime = new DateTime(2026, 1, 13, 0, 30, 0); // 12:30 AM next day

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsActiveAt_SpanningMidnight_OutsideWindow_ShouldReturnFalse()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(23, 0), // 11 PM
			DurationMinutes = 120 // 2 hours (ends at 1 AM)
		};
		var testTime = new DateTime(2026, 1, 12, 22, 0, 0); // 10 PM (before start)

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void GetNextActivation_WhenDisabled_ShouldReturnNull()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = false,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var testTime = new DateTime(2026, 1, 12, 9, 0, 0);

		// Act
		var result = timer.GetNextActivation(testTime);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void GetNextActivation_BeforeStartTimeToday_ShouldReturnToday()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var testTime = new DateTime(2026, 1, 12, 9, 0, 0);

		// Act
		var result = timer.GetNextActivation(testTime);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(new DateTime(2026, 1, 12, 10, 0, 0), result.Value);
	}

	[Fact]
	public void GetNextActivation_AfterStartTimeToday_ShouldReturnTomorrow()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var testTime = new DateTime(2026, 1, 12, 11, 0, 0);

		// Act
		var result = timer.GetNextActivation(testTime);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(new DateTime(2026, 1, 13, 10, 0, 0), result.Value);
	}

	[Fact]
	public void GetNextActivation_WithActiveDays_ShouldReturnNextMatchingDay()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60,
			ActiveDays = new List<DayOfWeek> { DayOfWeek.Wednesday, DayOfWeek.Friday }
		};
		// Monday, Jan 12, 2026
		var testTime = new DateTime(2026, 1, 12, 9, 0, 0);

		// Act
		var result = timer.GetNextActivation(testTime);

		// Assert
		Assert.NotNull(result);
		// Should be Wednesday, Jan 14, 2026 at 10:00 AM
		Assert.Equal(new DateTime(2026, 1, 14, 10, 0, 0), result.Value);
		Assert.Equal(DayOfWeek.Wednesday, result.Value.DayOfWeek);
	}

	[Fact]
	public void GetNextActivation_WithActiveDaysAfterTime_ShouldSkipToNextWeek()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60,
			ActiveDays = new List<DayOfWeek> { DayOfWeek.Monday }
		};
		// Monday, Jan 12, 2026 at 11:00 AM (after start time)
		var testTime = new DateTime(2026, 1, 12, 11, 0, 0);

		// Act
		var result = timer.GetNextActivation(testTime);

		// Assert
		Assert.NotNull(result);
		// Should be next Monday, Jan 19, 2026 at 10:00 AM
		Assert.Equal(new DateTime(2026, 1, 19, 10, 0, 0), result.Value);
		Assert.Equal(DayOfWeek.Monday, result.Value.DayOfWeek);
	}

	[Fact]
	public void GetNextDeactivation_WhenDisabled_ShouldReturnNull()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = false,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var testTime = new DateTime(2026, 1, 12, 9, 0, 0);

		// Act
		var result = timer.GetNextDeactivation(testTime);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void GetNextDeactivation_ShouldReturnActivationPlusDuration()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var testTime = new DateTime(2026, 1, 12, 9, 0, 0);

		// Act
		var result = timer.GetNextDeactivation(testTime);

		// Assert
		Assert.NotNull(result);
		// Activation at 10:00, deactivation at 11:00
		Assert.Equal(new DateTime(2026, 1, 12, 11, 0, 0), result.Value);
	}

	[Fact]
	public void GetNextDeactivation_SpanningMidnight_ShouldReturnNextDay()
	{
		// Arrange
		var timer = new DurationTimer
		{
			IsEnabled = true,
			StartTime = new TimeOnly(23, 0), // 11 PM
			DurationMinutes = 120 // 2 hours
		};
		var testTime = new DateTime(2026, 1, 12, 9, 0, 0);

		// Act
		var result = timer.GetNextDeactivation(testTime);

		// Assert
		Assert.NotNull(result);
		// Activation at 11:00 PM, deactivation at 1:00 AM next day
		Assert.Equal(new DateTime(2026, 1, 13, 1, 0, 0), result.Value);
	}
}
