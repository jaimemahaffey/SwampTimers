using SwampTimers.Models;
using SwampTimers.Models.HomeAssistant;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Google.Protobuf;
using PeriodicEvents.Proto;

namespace SwampTimers.Services;

/// <summary>
/// Custom YAML type converter for TimeOnly to handle serialization/deserialization
/// </summary>
public class TimeOnlyConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(TimeOnly) || type == typeof(TimeOnly?);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		// Check if we're dealing with a scalar (new format) or mapping (old format)
		if (parser.Current is Scalar scalar)
		{
			parser.MoveNext();
			if (string.IsNullOrEmpty(scalar.Value) && type == typeof(TimeOnly?))
			{
				return null;
			}
			return TimeOnly.Parse(scalar.Value);
		}
		else if (parser.Current is MappingStart)
		{
			// Handle old format where TimeOnly was serialized as an object with hour, minute, second
			var mapping = rootDeserializer(typeof(Dictionary<string, int>)) as Dictionary<string, int>;
			if (mapping == null || mapping.Count == 0)
			{
				return type == typeof(TimeOnly?) ? null : TimeOnly.MinValue;
			}

			int hour = mapping.GetValueOrDefault("hour", 0);
			int minute = mapping.GetValueOrDefault("minute", 0);
			int second = mapping.GetValueOrDefault("second", 0);

			return new TimeOnly(hour, minute, second);
		}
		else
		{
			throw new YamlException($"Expected scalar or mapping for TimeOnly, got {parser.Current?.GetType().Name}");
		}
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value == null)
		{
			emitter.Emit(new Scalar(string.Empty));
		}
		else
		{
			var timeOnly = (TimeOnly)value;
			emitter.Emit(new Scalar(timeOnly.ToString("HH:mm:ss")));
		}
	}
}

/// <summary>
/// YAML file-based implementation of timer storage
/// Thread-safe implementation using SemaphoreSlim for file access
/// </summary>
public class YamlTimerService : ITimerService, IDisposable
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private int _nextId = 1;

    public YamlTimerService(string filePath = "timers.yaml")
    {
        _filePath = filePath;

        // Configure YAML serializer with camelCase naming and TimeOnly converter
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new TimeOnlyConverter())
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new TimeOnlyConverter())
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
            {
                // Create empty YAML file with empty list
                var emptyData = new YamlTimerData { Timers = new List<TimerSchedule>() };
                await SaveDataAsync(emptyData);
            }
            else
            {
                // Load existing data to determine next ID
                var data = await LoadDataAsync();
                if (data.Timers.Any())
                {
                    _nextId = data.Timers.Max(t => t.Id) + 1;
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<TimerSchedule>> GetAllAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var data = await LoadDataAsync();
            return data.Timers.OrderByDescending(t => t.CreatedAt).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<TimerSchedule?> GetByIdAsync(int id)
    {
        await _semaphore.WaitAsync();
        try
        {
            var data = await LoadDataAsync();
            return data.Timers.FirstOrDefault(t => t.Id == id);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<T>> GetByTypeAsync<T>() where T : TimerSchedule
    {
        await _semaphore.WaitAsync();
        try
        {
            var data = await LoadDataAsync();
            return data.Timers
                .OfType<T>()
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<TimerSchedule>> GetEnabledAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var data = await LoadDataAsync();
            return data.Timers
                .Where(t => t.IsEnabled)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<TimerSchedule>> GetActiveAsync(DateTime? atTime = null)
    {
        var checkTime = atTime ?? DateTime.Now;
        var enabledTimers = await GetEnabledAsync();

        return enabledTimers.Where(t => t.IsActiveAt(checkTime)).ToList();
    }

    public async Task<TimerSchedule> CreateAsync(TimerSchedule timer)
    {
        await _semaphore.WaitAsync();
        try
        {
            var data = await LoadDataAsync();

            timer.Id = _nextId++;
            timer.CreatedAt = DateTime.UtcNow;
            timer.LastModifiedAt = null;

            data.Timers.Add(timer);
            await SaveDataAsync(data);

            return timer;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<TimerSchedule> UpdateAsync(TimerSchedule timer)
    {
        await _semaphore.WaitAsync();
        try
        {
            var data = await LoadDataAsync();
            var index = data.Timers.FindIndex(t => t.Id == timer.Id);

            if (index == -1)
            {
                throw new InvalidOperationException($"Timer with ID {timer.Id} not found");
            }

            timer.LastModifiedAt = DateTime.UtcNow;
            data.Timers[index] = timer;

            await SaveDataAsync(data);

            return timer;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await _semaphore.WaitAsync();
        try
        {
            var data = await LoadDataAsync();
            var removed = data.Timers.RemoveAll(t => t.Id == id);

            if (removed > 0)
            {
                await SaveDataAsync(data);
                return true;
            }

            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> ToggleEnabledAsync(int id)
    {
        await _semaphore.WaitAsync();
        try
        {
            var data = await LoadDataAsync();
            var timer = data.Timers.FirstOrDefault(t => t.Id == id);

            if (timer == null)
            {
                return false;
            }

            timer.IsEnabled = !timer.IsEnabled;
            timer.LastModifiedAt = DateTime.UtcNow;

            await SaveDataAsync(data);

            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<YamlTimerData> LoadDataAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new YamlTimerData { Timers = new List<TimerSchedule>() };
        }

        var yaml = await File.ReadAllTextAsync(_filePath);

        if (string.IsNullOrWhiteSpace(yaml))
        {
            return new YamlTimerData { Timers = new List<TimerSchedule>() };
        }

        // Deserialize with custom type discriminator handling
        var rawData = _deserializer.Deserialize<YamlTimerDataRaw>(yaml);
        var data = new YamlTimerData { Timers = new List<TimerSchedule>() };

        foreach (var rawTimer in rawData.Timers)
        {
            TimerSchedule timer = rawTimer.TimerType switch
            {
                "Duration" => new DurationTimer
                {
                    Id = rawTimer.Id,
                    Name = rawTimer.Name,
                    Description = rawTimer.Description,
                    IsEnabled = rawTimer.IsEnabled,
                    CreatedAt = rawTimer.CreatedAt,
                    LastModifiedAt = rawTimer.LastModifiedAt,
                    StartTime = rawTimer.StartTime ?? TimeOnly.MinValue,
                    DurationMinutes = rawTimer.DurationMinutes ?? 0,
                    ActiveDays = rawTimer.ActiveDays ?? new List<DayOfWeek>()
                },
                "TimeRange" => new TimeRangeTimer
                {
                    Id = rawTimer.Id,
                    Name = rawTimer.Name,
                    Description = rawTimer.Description,
                    IsEnabled = rawTimer.IsEnabled,
                    CreatedAt = rawTimer.CreatedAt,
                    LastModifiedAt = rawTimer.LastModifiedAt,
                    OnTime = rawTimer.OnTime ?? TimeOnly.MinValue,
                    OffTime = rawTimer.OffTime ?? TimeOnly.MinValue,
                    ActiveDays = rawTimer.ActiveDays ?? new List<DayOfWeek>()
                },
                "Recurring" or "Periodic" => CreateRecurringTimer(rawTimer),
                _ => throw new InvalidOperationException($"Unknown timer type: {rawTimer.TimerType}")
            };

            // Set HomeAssistantBinding for all timer types
            timer.HomeAssistantBinding = rawTimer.HomeAssistantBinding;

            data.Timers.Add(timer);
        }

        return data;
    }

    private static RecurringTimer CreateRecurringTimer(YamlTimerRaw rawTimer)
    {
        var recurringTimer = new RecurringTimer
        {
            Id = rawTimer.Id,
            Name = rawTimer.Name,
            Description = rawTimer.Description,
            IsEnabled = rawTimer.IsEnabled,
            CreatedAt = rawTimer.CreatedAt,
            LastModifiedAt = rawTimer.LastModifiedAt
        };

        if (!string.IsNullOrEmpty(rawTimer.EventJson))
        {
            recurringTimer.Event = JsonParser.Default.Parse<PeriodicEvent>(rawTimer.EventJson);
        }

        if (!string.IsNullOrEmpty(rawTimer.CurrentOccurrenceJson))
        {
            recurringTimer.CurrentOccurrence = JsonParser.Default.Parse<EventOccurrence>(rawTimer.CurrentOccurrenceJson);
        }

        if (!string.IsNullOrEmpty(rawTimer.LastCompletedOccurrenceJson))
        {
            recurringTimer.LastCompletedOccurrence = JsonParser.Default.Parse<EventOccurrence>(rawTimer.LastCompletedOccurrenceJson);
        }

        return recurringTimer;
    }

    private async Task SaveDataAsync(YamlTimerData data)
    {
        // Convert to raw format for serialization
        var rawData = new YamlTimerDataRaw
        {
            Timers = data.Timers.Select(t => CreateRawTimer(t)).ToList()
        };

        var yaml = _serializer.Serialize(rawData);
        await File.WriteAllTextAsync(_filePath, yaml);
    }

    private static YamlTimerRaw CreateRawTimer(TimerSchedule timer)
    {
        var raw = new YamlTimerRaw
        {
            Id = timer.Id,
            TimerType = timer.TimerType,
            Name = timer.Name,
            Description = timer.Description,
            IsEnabled = timer.IsEnabled,
            CreatedAt = timer.CreatedAt,
            LastModifiedAt = timer.LastModifiedAt
        };

        switch (timer)
        {
            case DurationTimer dt:
                raw.StartTime = dt.StartTime;
                raw.DurationMinutes = dt.DurationMinutes;
                raw.ActiveDays = dt.ActiveDays;
                break;
            case TimeRangeTimer tr:
                raw.OnTime = tr.OnTime;
                raw.OffTime = tr.OffTime;
                raw.ActiveDays = tr.ActiveDays;
                break;
            case RecurringTimer rt:
                raw.EventJson = JsonFormatter.Default.Format(rt.Event);
                raw.CurrentOccurrenceJson = rt.CurrentOccurrence != null
                    ? JsonFormatter.Default.Format(rt.CurrentOccurrence)
                    : null;
                raw.LastCompletedOccurrenceJson = rt.LastCompletedOccurrence != null
                    ? JsonFormatter.Default.Format(rt.LastCompletedOccurrence)
                    : null;
                break;
        }

        // Copy HomeAssistantBinding for all timer types
        raw.HomeAssistantBinding = timer.HomeAssistantBinding;

        return raw;
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }

    // Internal data structures for YAML serialization
    private class YamlTimerData
    {
        public List<TimerSchedule> Timers { get; set; } = new();
    }

    private class YamlTimerDataRaw
    {
        public List<YamlTimerRaw> Timers { get; set; } = new();
    }

    private class YamlTimerRaw
    {
        public int Id { get; set; }
        public string TimerType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public TimeOnly? StartTime { get; set; }
        public int? DurationMinutes { get; set; }
        public TimeOnly? OnTime { get; set; }
        public TimeOnly? OffTime { get; set; }
        public List<DayOfWeek>? ActiveDays { get; set; }
        // RecurringTimer fields (protobuf data stored as JSON strings)
        public string? EventJson { get; set; }
        public string? CurrentOccurrenceJson { get; set; }
        public string? LastCompletedOccurrenceJson { get; set; }
        // Home Assistant binding (applies to all timer types)
        public TimerEntityBinding? HomeAssistantBinding { get; set; }
    }
}
