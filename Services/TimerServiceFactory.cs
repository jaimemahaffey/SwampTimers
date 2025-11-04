using SwampTimers.Models;

namespace SwampTimers.Services;

/// <summary>
/// Factory for creating timer service instances based on configuration
/// </summary>
public static class TimerServiceFactory
{
    /// <summary>
    /// Creates an ITimerService instance based on the provided storage options
    /// </summary>
    public static ITimerService Create(StorageOptions options)
    {
        return options.StorageType switch
        {
            StorageType.Sqlite => new SqliteTimerService(options.SqlitePath),
            StorageType.Yaml => new YamlTimerService(options.YamlPath),
            _ => throw new InvalidOperationException($"Unsupported storage type: {options.StorageType}")
        };
    }
}
