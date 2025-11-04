namespace SwampTimers.Models;

/// <summary>
/// Storage backend types supported by the application
/// </summary>
public enum StorageType
{
    Sqlite,
    Yaml
}

/// <summary>
/// Configuration options for timer storage
/// </summary>
public class StorageOptions
{
    /// <summary>
    /// The type of storage backend to use
    /// </summary>
    public StorageType StorageType { get; set; } = StorageType.Sqlite;

    /// <summary>
    /// Path to the SQLite database file
    /// </summary>
    public string SqlitePath { get; set; } = "timers.db";

    /// <summary>
    /// Path to the YAML data file
    /// </summary>
    public string YamlPath { get; set; } = "timers.yaml";
}
