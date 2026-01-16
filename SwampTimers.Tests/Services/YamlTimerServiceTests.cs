using SwampTimers.Models;
using SwampTimers.Services;
using PeriodicEvents.Proto;

namespace SwampTimers.Tests.Services;

public class YamlTimerServiceTests : IDisposable
{
	private readonly string _testYamlPath;
	private readonly YamlTimerService _service;

	public YamlTimerServiceTests()
	{
		// Create a unique test YAML file for each test run
		_testYamlPath = $"test_timers_{Guid.NewGuid()}.yaml";
		_service = new YamlTimerService(_testYamlPath);
	}

	public void Dispose()
	{
		_service.Dispose();
		if (File.Exists(_testYamlPath))
		{
			File.Delete(_testYamlPath);
		}
	}

	[Fact]
	public async Task InitializeAsync_ShouldCreateYamlFile()
	{
		// Act
		await _service.InitializeAsync();

		// Assert
		Assert.True(File.Exists(_testYamlPath));
	}

	[Fact]
	public async Task CreateAsync_DurationTimer_ShouldAssignIdAndReturnTimer()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new DurationTimer
		{
			Name = "Test Timer",
			Description = "Test Description",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60,
			ActiveDays = new List<DayOfWeek> { DayOfWeek.Monday }
		};

		// Act
		var result = await _service.CreateAsync(timer);

		// Assert
		Assert.Equal(1, result.Id); // First timer should get ID 1
		Assert.Equal("Test Timer", result.Name);
		Assert.NotEqual(default, result.CreatedAt);
		Assert.Null(result.LastModifiedAt);
	}

	[Fact]
	public async Task CreateAsync_TimeRangeTimer_ShouldAssignIdAndReturnTimer()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new TimeRangeTimer
		{
			Name = "Test Range Timer",
			Description = "Test Description",
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0),
			ActiveDays = new List<DayOfWeek> { DayOfWeek.Monday }
		};

		// Act
		var result = await _service.CreateAsync(timer);

		// Assert
		Assert.Equal(1, result.Id);
		Assert.Equal("Test Range Timer", result.Name);
		Assert.NotEqual(default, result.CreatedAt);
	}

	[Fact]
	public async Task CreateAsync_MultipleTimers_ShouldAssignIncrementalIds()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer1 = new DurationTimer
		{
			Name = "Timer 1",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var timer2 = new DurationTimer
		{
			Name = "Timer 2",
			IsEnabled = true,
			StartTime = new TimeOnly(11, 0),
			DurationMinutes = 60
		};

		// Act
		var result1 = await _service.CreateAsync(timer1);
		var result2 = await _service.CreateAsync(timer2);

		// Assert
		Assert.Equal(1, result1.Id);
		Assert.Equal(2, result2.Id);
	}

	[Fact]
	public async Task GetByIdAsync_WhenExists_ShouldReturnTimer()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new DurationTimer
		{
			Name = "Test Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var created = await _service.CreateAsync(timer);

		// Act
		var result = await _service.GetByIdAsync(created.Id);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(created.Id, result.Id);
		Assert.Equal("Test Timer", result.Name);
		Assert.IsType<DurationTimer>(result);
	}

	[Fact]
	public async Task GetByIdAsync_WhenNotExists_ShouldReturnNull()
	{
		// Arrange
		await _service.InitializeAsync();

		// Act
		var result = await _service.GetByIdAsync(999);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task GetAllAsync_ShouldReturnAllTimers()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer1 = new DurationTimer
		{
			Name = "Timer 1",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var timer2 = new TimeRangeTimer
		{
			Name = "Timer 2",
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
		};
		await _service.CreateAsync(timer1);
		await _service.CreateAsync(timer2);

		// Act
		var result = await _service.GetAllAsync();

		// Assert
		Assert.Equal(2, result.Count);
		Assert.Contains(result, t => t.Name == "Timer 1");
		Assert.Contains(result, t => t.Name == "Timer 2");
	}

	[Fact]
	public async Task GetByTypeAsync_ShouldReturnOnlyDurationTimers()
	{
		// Arrange
		await _service.InitializeAsync();
		var durationTimer = new DurationTimer
		{
			Name = "Duration Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var timeRangeTimer = new TimeRangeTimer
		{
			Name = "Time Range Timer",
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
		};
		await _service.CreateAsync(durationTimer);
		await _service.CreateAsync(timeRangeTimer);

		// Act
		var result = await _service.GetByTypeAsync<DurationTimer>();

		// Assert
		Assert.Single(result);
		Assert.Equal("Duration Timer", result[0].Name);
		Assert.IsType<DurationTimer>(result[0]);
	}

	[Fact]
	public async Task GetByTypeAsync_ShouldReturnOnlyTimeRangeTimers()
	{
		// Arrange
		await _service.InitializeAsync();
		var durationTimer = new DurationTimer
		{
			Name = "Duration Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var timeRangeTimer = new TimeRangeTimer
		{
			Name = "Time Range Timer",
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0)
		};
		await _service.CreateAsync(durationTimer);
		await _service.CreateAsync(timeRangeTimer);

		// Act
		var result = await _service.GetByTypeAsync<TimeRangeTimer>();

		// Assert
		Assert.Single(result);
		Assert.Equal("Time Range Timer", result[0].Name);
		Assert.IsType<TimeRangeTimer>(result[0]);
	}

	[Fact]
	public async Task GetEnabledAsync_ShouldReturnOnlyEnabledTimers()
	{
		// Arrange
		await _service.InitializeAsync();
		var enabledTimer = new DurationTimer
		{
			Name = "Enabled Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var disabledTimer = new DurationTimer
		{
			Name = "Disabled Timer",
			IsEnabled = false,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		await _service.CreateAsync(enabledTimer);
		await _service.CreateAsync(disabledTimer);

		// Act
		var result = await _service.GetEnabledAsync();

		// Assert
		Assert.Single(result);
		Assert.Equal("Enabled Timer", result[0].Name);
		Assert.True(result[0].IsEnabled);
	}

	[Fact]
	public async Task GetActiveAsync_ShouldReturnOnlyActiveTimers()
	{
		// Arrange
		await _service.InitializeAsync();
		var activeTimer = new DurationTimer
		{
			Name = "Active Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var inactiveTimer = new DurationTimer
		{
			Name = "Inactive Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(14, 0),
			DurationMinutes = 60
		};
		await _service.CreateAsync(activeTimer);
		await _service.CreateAsync(inactiveTimer);

		var testTime = new DateTime(2026, 1, 12, 10, 30, 0); // 10:30 AM

		// Act
		var result = await _service.GetActiveAsync(testTime);

		// Assert
		Assert.Single(result);
		Assert.Equal("Active Timer", result[0].Name);
	}

	[Fact]
	public async Task UpdateAsync_ShouldUpdateTimer()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new DurationTimer
		{
			Name = "Original Name",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var created = await _service.CreateAsync(timer);

		created.Name = "Updated Name";
		created.IsEnabled = false;

		// Act
		var result = await _service.UpdateAsync(created);

		// Assert
		Assert.Equal("Updated Name", result.Name);
		Assert.False(result.IsEnabled);
		Assert.NotNull(result.LastModifiedAt);

		// Verify persistence
		var retrieved = await _service.GetByIdAsync(created.Id);
		Assert.NotNull(retrieved);
		Assert.Equal("Updated Name", retrieved.Name);
		Assert.False(retrieved.IsEnabled);
	}

	[Fact]
	public async Task UpdateAsync_WhenNotExists_ShouldThrowException()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new DurationTimer
		{
			Id = 999,
			Name = "Nonexistent Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(timer));
	}

	[Fact]
	public async Task DeleteAsync_WhenExists_ShouldReturnTrueAndRemoveTimer()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new DurationTimer
		{
			Name = "Test Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var created = await _service.CreateAsync(timer);

		// Act
		var result = await _service.DeleteAsync(created.Id);

		// Assert
		Assert.True(result);

		// Verify deletion
		var retrieved = await _service.GetByIdAsync(created.Id);
		Assert.Null(retrieved);
	}

	[Fact]
	public async Task DeleteAsync_WhenNotExists_ShouldReturnFalse()
	{
		// Arrange
		await _service.InitializeAsync();

		// Act
		var result = await _service.DeleteAsync(999);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public async Task ToggleEnabledAsync_WhenExists_ShouldToggleState()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new DurationTimer
		{
			Name = "Test Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var created = await _service.CreateAsync(timer);

		// Act
		var result = await _service.ToggleEnabledAsync(created.Id);

		// Assert
		Assert.True(result);

		// Verify toggle
		var retrieved = await _service.GetByIdAsync(created.Id);
		Assert.NotNull(retrieved);
		Assert.False(retrieved.IsEnabled);

		// Toggle again
		await _service.ToggleEnabledAsync(created.Id);
		retrieved = await _service.GetByIdAsync(created.Id);
		Assert.NotNull(retrieved);
		Assert.True(retrieved.IsEnabled);
	}

	[Fact]
	public async Task ToggleEnabledAsync_WhenNotExists_ShouldReturnFalse()
	{
		// Arrange
		await _service.InitializeAsync();

		// Act
		var result = await _service.ToggleEnabledAsync(999);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public async Task DurationTimer_ShouldPreserveActiveDays()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new DurationTimer
		{
			Name = "Test Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60,
			ActiveDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }
		};

		// Act
		var created = await _service.CreateAsync(timer);
		var retrieved = await _service.GetByIdAsync(created.Id) as DurationTimer;

		// Assert
		Assert.NotNull(retrieved);
		Assert.Equal(3, retrieved.ActiveDays.Count);
		Assert.Contains(DayOfWeek.Monday, retrieved.ActiveDays);
		Assert.Contains(DayOfWeek.Wednesday, retrieved.ActiveDays);
		Assert.Contains(DayOfWeek.Friday, retrieved.ActiveDays);
	}

	[Fact]
	public async Task TimeRangeTimer_ShouldPreserveActiveDays()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new TimeRangeTimer
		{
			Name = "Test Timer",
			IsEnabled = true,
			OnTime = new TimeOnly(10, 0),
			OffTime = new TimeOnly(11, 0),
			ActiveDays = new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Thursday }
		};

		// Act
		var created = await _service.CreateAsync(timer);
		var retrieved = await _service.GetByIdAsync(created.Id) as TimeRangeTimer;

		// Assert
		Assert.NotNull(retrieved);
		Assert.Equal(2, retrieved.ActiveDays.Count);
		Assert.Contains(DayOfWeek.Tuesday, retrieved.ActiveDays);
		Assert.Contains(DayOfWeek.Thursday, retrieved.ActiveDays);
	}

	[Fact]
	public async Task DurationTimer_ShouldSerializeTimeOnlyAsString()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new DurationTimer
		{
			Name = "Test Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(14, 30, 15), // 2:30:15 PM
			DurationMinutes = 90
		};

		// Act
		await _service.CreateAsync(timer);
		var yamlContent = await File.ReadAllTextAsync(_testYamlPath);

		// Assert - Should contain time in string format (HH:mm:ss)
		Assert.Contains("14:30:15", yamlContent);
	}

	[Fact]
	public async Task TimeRangeTimer_ShouldSerializeTimeOnlyAsString()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new TimeRangeTimer
		{
			Name = "Test Timer",
			IsEnabled = true,
			OnTime = new TimeOnly(23, 0, 0), // 11 PM
			OffTime = new TimeOnly(1, 0, 0) // 1 AM
		};

		// Act
		await _service.CreateAsync(timer);
		var yamlContent = await File.ReadAllTextAsync(_testYamlPath);

		// Assert - Should contain times in string format
		Assert.Contains("23:00:00", yamlContent);
		Assert.Contains("01:00:00", yamlContent);
	}

	[Fact]
	public async Task DurationTimer_ShouldPreserveTimeOnlyValues()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new DurationTimer
		{
			Name = "Test Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(14, 30, 15), // 2:30:15 PM
			DurationMinutes = 90
		};

		// Act
		var created = await _service.CreateAsync(timer);
		var retrieved = await _service.GetByIdAsync(created.Id) as DurationTimer;

		// Assert
		Assert.NotNull(retrieved);
		Assert.Equal(new TimeOnly(14, 30, 15), retrieved.StartTime);
		Assert.Equal(90, retrieved.DurationMinutes);
	}

	[Fact]
	public async Task TimeRangeTimer_ShouldPreserveTimeOnlyValues()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new TimeRangeTimer
		{
			Name = "Test Timer",
			IsEnabled = true,
			OnTime = new TimeOnly(23, 0, 0), // 11 PM
			OffTime = new TimeOnly(1, 0, 0) // 1 AM
		};

		// Act
		var created = await _service.CreateAsync(timer);
		var retrieved = await _service.GetByIdAsync(created.Id) as TimeRangeTimer;

		// Assert
		Assert.NotNull(retrieved);
		Assert.Equal(new TimeOnly(23, 0, 0), retrieved.OnTime);
		Assert.Equal(new TimeOnly(1, 0, 0), retrieved.OffTime);
	}

	[Fact]
	public async Task InitializeAsync_WithExistingFile_ShouldDetermineNextId()
	{
		// Arrange - Create initial service and add timers
		await _service.InitializeAsync();
		await _service.CreateAsync(new DurationTimer
		{
			Name = "Timer 1",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		});
		await _service.CreateAsync(new DurationTimer
		{
			Name = "Timer 2",
			IsEnabled = true,
			StartTime = new TimeOnly(11, 0),
			DurationMinutes = 60
		});
		_service.Dispose();

		// Act - Create new service instance with same file
		var newService = new YamlTimerService(_testYamlPath);
		await newService.InitializeAsync();
		var newTimer = await newService.CreateAsync(new DurationTimer
		{
			Name = "Timer 3",
			IsEnabled = true,
			StartTime = new TimeOnly(12, 0),
			DurationMinutes = 60
		});

		// Assert - New timer should get ID 3
		Assert.Equal(3, newTimer.Id);

		newService.Dispose();
	}

	[Fact]
	public async Task CreateAsync_RecurringTimer_ShouldAssignIdAndReturnTimer()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new RecurringTimer
		{
			Name = "Test Recurring Timer",
			Description = "Test Description",
			IsEnabled = true,
			Event = new PeriodicEvent
			{
				Id = "test-event-id",
				Name = "Test Event",
				PeriodType = PeriodType.Days,
				PeriodValue = 7
			}
		};
		timer.InitializeFirstOccurrence(DateTime.Today);

		// Act
		var result = await _service.CreateAsync(timer);

		// Assert
		Assert.NotEqual(0, result.Id);
		Assert.Equal("Test Recurring Timer", result.Name);
		Assert.NotEqual(default, result.CreatedAt);
	}

	[Fact]
	public async Task RecurringTimer_ShouldPreserveEventData()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new RecurringTimer
		{
			Name = "Test Recurring Timer",
			IsEnabled = true,
			Event = new PeriodicEvent
			{
				Id = "my-event-id",
				Name = "My Periodic Event",
				Description = "Event description",
				PeriodType = PeriodType.Months,
				PeriodValue = 3
			}
		};
		timer.InitializeFirstOccurrence(DateTime.Today);

		// Act
		var created = await _service.CreateAsync(timer);
		var retrieved = await _service.GetByIdAsync(created.Id) as RecurringTimer;

		// Assert
		Assert.NotNull(retrieved);
		Assert.Equal("my-event-id", retrieved.Event.Id);
		Assert.Equal("My Periodic Event", retrieved.Event.Name);
		Assert.Equal("Event description", retrieved.Event.Description);
		Assert.Equal(PeriodType.Months, retrieved.Event.PeriodType);
		Assert.Equal(3, retrieved.Event.PeriodValue);
	}

	[Fact]
	public async Task RecurringTimer_ShouldPreserveCurrentOccurrence()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new RecurringTimer
		{
			Name = "Test Recurring Timer",
			IsEnabled = true,
			Event = new PeriodicEvent
			{
				Id = "event-id",
				Name = "Event",
				PeriodType = PeriodType.Days,
				PeriodValue = 14
			}
		};
		timer.InitializeFirstOccurrence(DateTime.Today.AddDays(5));

		// Act
		var created = await _service.CreateAsync(timer);
		var retrieved = await _service.GetByIdAsync(created.Id) as RecurringTimer;

		// Assert
		Assert.NotNull(retrieved);
		Assert.NotNull(retrieved.CurrentOccurrence);
		Assert.Equal(OccurrenceStatus.Pending, retrieved.CurrentOccurrence.Status);
		Assert.Equal(DateTime.Today.AddDays(5).Year, retrieved.CurrentOccurrence.ScheduledDate.Year);
		Assert.Equal(DateTime.Today.AddDays(5).Month, retrieved.CurrentOccurrence.ScheduledDate.Month);
		Assert.Equal(DateTime.Today.AddDays(5).Day, retrieved.CurrentOccurrence.ScheduledDate.Day);
	}

	[Fact]
	public async Task RecurringTimer_ShouldPreserveLastCompletedOccurrence()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new RecurringTimer
		{
			Name = "Test Recurring Timer",
			IsEnabled = true,
			Event = new PeriodicEvent
			{
				Id = "event-id",
				Name = "Event",
				PeriodType = PeriodType.Weeks,
				PeriodValue = 2
			}
		};
		timer.InitializeFirstOccurrence(DateTime.Today);
		timer.CompleteCurrentOccurrence(DateTime.Today, "Completed on time");

		// Act
		var created = await _service.CreateAsync(timer);
		var retrieved = await _service.GetByIdAsync(created.Id) as RecurringTimer;

		// Assert
		Assert.NotNull(retrieved);
		Assert.NotNull(retrieved.LastCompletedOccurrence);
		Assert.Equal(OccurrenceStatus.Completed, retrieved.LastCompletedOccurrence.Status);
		Assert.Equal("Completed on time", retrieved.LastCompletedOccurrence.Notes);
	}

	[Fact]
	public async Task RecurringTimer_ShouldUpdateCorrectly()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new RecurringTimer
		{
			Name = "Original Name",
			IsEnabled = true,
			Event = new PeriodicEvent
			{
				Id = "event-id",
				Name = "Original Event",
				PeriodType = PeriodType.Days,
				PeriodValue = 7
			}
		};
		timer.InitializeFirstOccurrence(DateTime.Today);
		var created = await _service.CreateAsync(timer) as RecurringTimer;

		// Modify
		created!.Name = "Updated Name";
		created.Event.Name = "Updated Event";
		created.Event.PeriodValue = 14;

		// Act
		await _service.UpdateAsync(created);
		var retrieved = await _service.GetByIdAsync(created.Id) as RecurringTimer;

		// Assert
		Assert.NotNull(retrieved);
		Assert.Equal("Updated Name", retrieved.Name);
		Assert.Equal("Updated Event", retrieved.Event.Name);
		Assert.Equal(14, retrieved.Event.PeriodValue);
	}

	[Fact]
	public async Task GetByTypeAsync_ShouldReturnOnlyRecurringTimers()
	{
		// Arrange
		await _service.InitializeAsync();
		var durationTimer = new DurationTimer
		{
			Name = "Duration Timer",
			IsEnabled = true,
			StartTime = new TimeOnly(10, 0),
			DurationMinutes = 60
		};
		var recurringTimer = new RecurringTimer
		{
			Name = "Recurring Timer",
			IsEnabled = true,
			Event = new PeriodicEvent { Id = "id", Name = "Event", PeriodType = PeriodType.Days, PeriodValue = 7 }
		};
		recurringTimer.InitializeFirstOccurrence(DateTime.Today);

		await _service.CreateAsync(durationTimer);
		await _service.CreateAsync(recurringTimer);

		// Act
		var result = await _service.GetByTypeAsync<RecurringTimer>();

		// Assert
		Assert.Single(result);
		Assert.Equal("Recurring Timer", result[0].Name);
		Assert.IsType<RecurringTimer>(result[0]);
	}

	[Fact]
	public async Task RecurringTimer_ShouldSerializeEventAsJson()
	{
		// Arrange
		await _service.InitializeAsync();
		var timer = new RecurringTimer
		{
			Name = "Test Recurring Timer",
			IsEnabled = true,
			Event = new PeriodicEvent
			{
				Id = "my-event-id",
				Name = "My Event"
			}
		};
		timer.InitializeFirstOccurrence(DateTime.Today);

		// Act
		await _service.CreateAsync(timer);
		var yamlContent = await File.ReadAllTextAsync(_testYamlPath);

		// Assert - Should contain eventJson field with JSON content
		Assert.Contains("eventJson", yamlContent);
		Assert.Contains("my-event-id", yamlContent);
	}
}
