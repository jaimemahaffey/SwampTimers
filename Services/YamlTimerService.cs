using SwampTimers.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SwampTimers.Services;

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

        // Configure YAML serializer with camelCase naming
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
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
                _ => throw new InvalidOperationException($"Unknown timer type: {rawTimer.TimerType}")
            };

            data.Timers.Add(timer);
        }

        return data;
    }

    private async Task SaveDataAsync(YamlTimerData data)
    {
        // Convert to raw format for serialization
        var rawData = new YamlTimerDataRaw
        {
            Timers = data.Timers.Select(t => new YamlTimerRaw
            {
                Id = t.Id,
                TimerType = t.TimerType,
                Name = t.Name,
                Description = t.Description,
                IsEnabled = t.IsEnabled,
                CreatedAt = t.CreatedAt,
                LastModifiedAt = t.LastModifiedAt,
                StartTime = t is DurationTimer dt ? dt.StartTime : null,
                DurationMinutes = t is DurationTimer dtm ? dtm.DurationMinutes : null,
                OnTime = t is TimeRangeTimer trt ? trt.OnTime : null,
                OffTime = t is TimeRangeTimer trf ? trf.OffTime : null,
                ActiveDays = t switch
                {
                    DurationTimer duration => duration.ActiveDays,
                    TimeRangeTimer timeRange => timeRange.ActiveDays,
                    _ => new List<DayOfWeek>()
                }
            }).ToList()
        };

        var yaml = _serializer.Serialize(rawData);
        await File.WriteAllTextAsync(_filePath, yaml);
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
    }
}
