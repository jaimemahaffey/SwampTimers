using SwampTimers.Models;

namespace SwampTimers.Tests.Models;

public class TimeRangeTimerTests
{
	[Fact]
	public void TimerType_ShouldReturnTimeRange()
	{
		// Arrange
		var timer = new TimeRangeTimer();

		// Act
		var result = timer.TimerType;

		// Assert
		Assert.Equal("TimeRange", result);
	}

	[Fact]
	public void IsActiveAt_WhenDisabled_ShouldReturnFalse()
	{
		// Arrange
		var timer = new TimeRangeTimer
		{
			IsEnabled = false,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
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
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0),
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
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0),
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
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0),
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
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
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
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
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
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
		};
		var testTime = new DateTime(2026, 1, 12, 11, 30, 0); // 11:30 AM

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void IsActiveAt_AtOnTime_ShouldReturnTrue()
	{
		// Arrange
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
		};
		var testTime = new DateTime(2026, 1, 12, 10, 0, 0); // Exactly 10:00 AM

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsActiveAt_AtOffTime_ShouldReturnTrue()
	{
		// Arrange
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
		};
		var testTime = new DateTime(2026, 1, 12, 11, 0, 0); // Exactly 11:00 AM

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsActiveAt_SpanningMidnight_BeforeMidnight_ShouldReturnTrue()
	{
		// Arrange
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(23, 0), // 11 PM
			OffTime = new TimeOnly(1, 0) // 1 AM
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
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(23, 0), // 11 PM
			OffTime = new TimeOnly(1, 0) // 1 AM
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
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(23, 0), // 11 PM
			OffTime = new TimeOnly(1, 0) // 1 AM
		};
		var testTime = new DateTime(2026, 1, 12, 22, 0, 0); // 10 PM (before on time)

		// Act
		var result = timer.IsActiveAt(testTime);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void GetNextActivation_WhenDisabled_ShouldReturnNull()
	{
		// Arrange
		var timer = new TimeRangeTimer
		{
			IsEnabled = false,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
		};
		var testTime = new DateTime(2026, 1, 12, 9, 0, 0);

		// Act
		var result = timer.GetNextActivation(testTime);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void GetNextActivation_BeforeOnTimeToday_ShouldReturnToday()
	{
		// Arrange
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
		};
		var testTime = new DateTime(2026, 1, 12, 9, 0, 0);

		// Act
		var result = timer.GetNextActivation(testTime);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(new DateTime(2026, 1, 12, 10, 0, 0), result.Value);
	}

	[Fact]
	public void GetNextActivation_AfterOnTimeToday_ShouldReturnTomorrow()
	{
		// Arrange
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
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
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0),
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
	public void GetNextDeactivation_WhenDisabled_ShouldReturnNull()
	{
		// Arrange
		var timer = new TimeRangeTimer
		{
			IsEnabled = false,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
		};
		var testTime = new DateTime(2026, 1, 12, 9, 0, 0);

		// Act
		var result = timer.GetNextDeactivation(testTime);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void GetNextDeactivation_BeforeOffTimeToday_ShouldReturnToday()
	{
		// Arrange
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
		};
		var testTime = new DateTime(2026, 1, 12, 10, 30, 0);

		// Act
		var result = timer.GetNextDeactivation(testTime);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(new DateTime(2026, 1, 12, 11, 0, 0), result.Value);
	}

	[Fact]
	public void GetNextDeactivation_AfterOffTimeToday_ShouldReturnTomorrow()
	{
		// Arrange
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
		};
		var testTime = new DateTime(2026, 1, 12, 11, 30, 0);

		// Act
		var result = timer.GetNextDeactivation(testTime);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(new DateTime(2026, 1, 13, 11, 0, 0), result.Value);
	}

	[Fact]
	public void GetNextDeactivation_SpanningMidnight_BeforeOffTime_ShouldReturnToday()
	{
		// Arrange
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(23, 0), // 11 PM
			OffTime = new TimeOnly(1, 0) // 1 AM
		};
		var testTime = new DateTime(2026, 1, 13, 0, 30, 0); // 12:30 AM

		// Act
		var result = timer.GetNextDeactivation(testTime);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(new DateTime(2026, 1, 13, 1, 0, 0), result.Value);
	}

	[Fact]
	public void GetNextDeactivation_WithActiveDays_ShouldReturnNextMatchingDay()
	{
		// Arrange
		var timer = new TimeRangeTimer
		{
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0),
			ActiveDays = new List<DayOfWeek> { DayOfWeek.Wednesday, DayOfWeek.Friday }
		};
		// Monday, Jan 12, 2026
		var testTime = new DateTime(2026, 1, 12, 9, 0, 0);

		// Act
		var result = timer.GetNextDeactivation(testTime);

		// Assert
		Assert.NotNull(result);
		// Should be Wednesday, Jan 14, 2026 at 11:00 AM
		Assert.Equal(new DateTime(2026, 1, 14, 11, 0, 0), result.Value);
		Assert.Equal(DayOfWeek.Wednesday, result.Value.DayOfWeek);
	}
}
