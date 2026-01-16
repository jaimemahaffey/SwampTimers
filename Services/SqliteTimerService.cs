using SwampTimers.Models;
using SwampTimers.Models.HomeAssistant;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using Google.Protobuf;
using PeriodicEvents.Proto;

namespace SwampTimers.Services;

/// <summary>
/// SQLite implementation of timer storage
/// Note: This implementation is designed for server-side use.
/// For Blazor WebAssembly, consider using localStorage via JS interop or a backend API.
/// </summary>
public class SqliteTimerService : ITimerService, IDisposable
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public SqliteTimerService(string databasePath = "timers.db")
    {
        _connectionString = $"Data Source={databasePath}";
    }

    public async Task InitializeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS TimerSchedules (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TimerType TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    LastModifiedAt TEXT,
                    StartTime TEXT,
                    OnTime TEXT,
                    OffTime TEXT,
                    DurationMinutes INTEGER,
                    ActiveDays TEXT,
                    PeriodicData TEXT,
                    HomeAssistantBinding TEXT
                )";
            await command.ExecuteNonQueryAsync();

            // Migration: Add PeriodicData column if it doesn't exist
            await TryAddColumnAsync(connection, "PeriodicData", "TEXT");

            // Migration: Add HomeAssistantBinding column if it doesn't exist
            await TryAddColumnAsync(connection, "HomeAssistantBinding", "TEXT");
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
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM TimerSchedules ORDER BY CreatedAt DESC";

            var timers = new List<TimerSchedule>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var timer = MapFromReader(reader);
                if (timer != null)
                    timers.Add(timer);
            }

            return timers;
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
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM TimerSchedules WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }

            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<T>> GetByTypeAsync<T>() where T : TimerSchedule
    {
        var timerType = typeof(T).Name switch
        {
            nameof(DurationTimer) => "Duration",
            nameof(TimeRangeTimer) => "TimeRange",
            nameof(RecurringTimer) => "Recurring",
            _ => throw new InvalidOperationException($"Unknown timer type: {typeof(T).Name}")
        };

        await _semaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM TimerSchedules WHERE TimerType = @type ORDER BY CreatedAt DESC";
            command.Parameters.AddWithValue("@type", timerType);

            var timers = new List<T>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var timer = MapFromReader(reader);
                if (timer is T typedTimer)
                    timers.Add(typedTimer);
            }

            return timers;
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
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM TimerSchedules WHERE IsEnabled = 1 ORDER BY CreatedAt DESC";

            var timers = new List<TimerSchedule>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var timer = MapFromReader(reader);
                if (timer != null)
                    timers.Add(timer);
            }

            return timers;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<TimerSchedule>> GetActiveAsync(DateTime? atTime = null)
    {
        var checkTime = atTime ?? DateTime.Now;
        var allTimers = await GetEnabledAsync();

        return allTimers.Where(t => t.IsActiveAt(checkTime)).ToList();
    }

    public async Task<TimerSchedule> CreateAsync(TimerSchedule timer)
    {
        await _semaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            timer.CreatedAt = DateTime.UtcNow;
            timer.LastModifiedAt = null;

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO TimerSchedules
                (TimerType, Name, Description, IsEnabled, CreatedAt, LastModifiedAt,
                 StartTime, OnTime, OffTime, DurationMinutes, ActiveDays, PeriodicData, HomeAssistantBinding)
                VALUES
                (@timerType, @name, @description, @isEnabled, @createdAt, @lastModifiedAt,
                 @startTime, @onTime, @offTime, @durationMinutes, @activeDays, @periodicData, @homeAssistantBinding);
                SELECT last_insert_rowid();";

            AddParameters(command, timer);

            var id = (long)(await command.ExecuteScalarAsync() ?? 0);
            timer.Id = (int)id;

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
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            timer.LastModifiedAt = DateTime.UtcNow;

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE TimerSchedules
                SET TimerType = @timerType,
                    Name = @name,
                    Description = @description,
                    IsEnabled = @isEnabled,
                    LastModifiedAt = @lastModifiedAt,
                    StartTime = @startTime,
                    OnTime = @onTime,
                    OffTime = @offTime,
                    DurationMinutes = @durationMinutes,
                    ActiveDays = @activeDays,
                    PeriodicData = @periodicData,
                    HomeAssistantBinding = @homeAssistantBinding
                WHERE Id = @id";

            AddParameters(command, timer);

            await command.ExecuteNonQueryAsync();

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
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM TimerSchedules WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
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
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE TimerSchedules
                SET IsEnabled = CASE WHEN IsEnabled = 1 THEN 0 ELSE 1 END,
                    LastModifiedAt = @now
                WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static void AddParameters(SqliteCommand command, TimerSchedule timer)
    {
        command.Parameters.AddWithValue("@id", timer.Id);
        command.Parameters.AddWithValue("@timerType", timer.TimerType);
        command.Parameters.AddWithValue("@name", timer.Name);
        command.Parameters.AddWithValue("@description", timer.Description ?? string.Empty);
        command.Parameters.AddWithValue("@isEnabled", timer.IsEnabled ? 1 : 0);
        command.Parameters.AddWithValue("@createdAt", timer.CreatedAt.ToString("o"));
        command.Parameters.AddWithValue("@lastModifiedAt", timer.LastModifiedAt?.ToString("o") ?? (object)DBNull.Value);

        if (timer is DurationTimer durationTimer)
        {
            command.Parameters.AddWithValue("@startTime", durationTimer.StartTime.ToString("o"));
            command.Parameters.AddWithValue("@onTime", DBNull.Value);
            command.Parameters.AddWithValue("@offTime", DBNull.Value);
            command.Parameters.AddWithValue("@durationMinutes", durationTimer.DurationMinutes);
            command.Parameters.AddWithValue("@activeDays", JsonSerializer.Serialize(durationTimer.ActiveDays));
            command.Parameters.AddWithValue("@periodicData", DBNull.Value);
        }
        else if (timer is TimeRangeTimer timeRangeTimer)
        {
            command.Parameters.AddWithValue("@startTime", DBNull.Value);
            command.Parameters.AddWithValue("@onTime", timeRangeTimer.OnTime.ToString("o"));
            command.Parameters.AddWithValue("@offTime", timeRangeTimer.OffTime.ToString("o"));
            command.Parameters.AddWithValue("@durationMinutes", DBNull.Value);
            command.Parameters.AddWithValue("@activeDays", JsonSerializer.Serialize(timeRangeTimer.ActiveDays));
            command.Parameters.AddWithValue("@periodicData", DBNull.Value);
        }
        else if (timer is RecurringTimer recurringTimer)
        {
            command.Parameters.AddWithValue("@startTime", DBNull.Value);
            command.Parameters.AddWithValue("@onTime", DBNull.Value);
            command.Parameters.AddWithValue("@offTime", DBNull.Value);
            command.Parameters.AddWithValue("@durationMinutes", DBNull.Value);
            command.Parameters.AddWithValue("@activeDays", DBNull.Value);

            // Serialize recurring timer data as JSON
            var periodicData = new
            {
                Event = JsonFormatter.Default.Format(recurringTimer.Event),
                CurrentOccurrence = recurringTimer.CurrentOccurrence != null
                    ? JsonFormatter.Default.Format(recurringTimer.CurrentOccurrence)
                    : null,
                LastCompletedOccurrence = recurringTimer.LastCompletedOccurrence != null
                    ? JsonFormatter.Default.Format(recurringTimer.LastCompletedOccurrence)
                    : null
            };
            command.Parameters.AddWithValue("@periodicData", JsonSerializer.Serialize(periodicData));
        }

        // Serialize HomeAssistantBinding for all timer types
        command.Parameters.AddWithValue("@homeAssistantBinding",
            timer.HomeAssistantBinding != null
                ? JsonSerializer.Serialize(timer.HomeAssistantBinding)
                : DBNull.Value);
    }

    private static TimerSchedule? MapFromReader(SqliteDataReader reader)
    {
        var timerType = reader.GetString(reader.GetOrdinal("TimerType"));

        TimerSchedule timer = timerType switch
        {
            "Duration" => new DurationTimer(),
            "TimeRange" => new TimeRangeTimer(),
            "Recurring" => new RecurringTimer(),
            "Periodic" => new RecurringTimer(), // Backward compatibility
            _ => throw new InvalidOperationException($"Unknown timer type: {timerType}")
        };

        timer.Id = reader.GetInt32(reader.GetOrdinal("Id"));
        timer.Name = reader.GetString(reader.GetOrdinal("Name"));
        timer.Description = reader.GetString(reader.GetOrdinal("Description"));
        timer.IsEnabled = reader.GetInt32(reader.GetOrdinal("IsEnabled")) == 1;
        timer.CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt")));

        var lastModifiedOrdinal = reader.GetOrdinal("LastModifiedAt");
        if (!reader.IsDBNull(lastModifiedOrdinal))
        {
            timer.LastModifiedAt = DateTime.Parse(reader.GetString(lastModifiedOrdinal));
        }

        var activeDaysOrdinal = reader.GetOrdinal("ActiveDays");
        var activeDaysJson = reader.IsDBNull(activeDaysOrdinal) ? "[]" : reader.GetString(activeDaysOrdinal);

        if (timer is DurationTimer durationTimer)
        {
            durationTimer.StartTime = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("StartTime")));
            durationTimer.DurationMinutes = reader.GetInt32(reader.GetOrdinal("DurationMinutes"));
            durationTimer.ActiveDays = JsonSerializer.Deserialize<List<DayOfWeek>>(activeDaysJson) ?? new();
        }
        else if (timer is TimeRangeTimer timeRangeTimer)
        {
            timeRangeTimer.OnTime = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("OnTime")));
            timeRangeTimer.OffTime = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("OffTime")));
            timeRangeTimer.ActiveDays = JsonSerializer.Deserialize<List<DayOfWeek>>(activeDaysJson) ?? new();
        }
        else if (timer is RecurringTimer recurringTimer)
        {
            var periodicDataOrdinal = reader.GetOrdinal("PeriodicData");
            if (!reader.IsDBNull(periodicDataOrdinal))
            {
                var periodicDataJson = reader.GetString(periodicDataOrdinal);
                var periodicData = JsonSerializer.Deserialize<JsonElement>(periodicDataJson);

                if (periodicData.TryGetProperty("Event", out var eventJson))
                {
                    recurringTimer.Event = JsonParser.Default.Parse<PeriodicEvent>(eventJson.GetString() ?? "{}");
                }

                if (periodicData.TryGetProperty("CurrentOccurrence", out var currentOccurrenceJson) &&
                    currentOccurrenceJson.GetString() != null)
                {
                    recurringTimer.CurrentOccurrence = JsonParser.Default.Parse<EventOccurrence>(currentOccurrenceJson.GetString()!);
                }

                if (periodicData.TryGetProperty("LastCompletedOccurrence", out var lastCompletedJson) &&
                    lastCompletedJson.GetString() != null)
                {
                    recurringTimer.LastCompletedOccurrence = JsonParser.Default.Parse<EventOccurrence>(lastCompletedJson.GetString()!);
                }
            }
        }

        // Deserialize HomeAssistantBinding for all timer types
        var haBindingOrdinal = reader.GetOrdinal("HomeAssistantBinding");
        if (!reader.IsDBNull(haBindingOrdinal))
        {
            var haBindingJson = reader.GetString(haBindingOrdinal);
            timer.HomeAssistantBinding = JsonSerializer.Deserialize<TimerEntityBinding>(haBindingJson);
        }

        return timer;
    }

    private static async Task TryAddColumnAsync(SqliteConnection connection, string columnName, string columnType)
    {
        try
        {
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $"ALTER TABLE TimerSchedules ADD COLUMN {columnName} {columnType}";
            await alterCommand.ExecuteNonQueryAsync();
        }
        catch (SqliteException)
        {
            // Column already exists, ignore error
        }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}
