using SwampTimers.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SwampTimers.Services;

/// <summary>
/// YAML file-based implementation of IActionLogService.
/// Stores action logs in a separate YAML file with automatic trimming to keep the last N entries.
/// </summary>
public class YamlActionLogService : IActionLogService
{
	private readonly string _filePath;
	private readonly int _maxEntries;
	private readonly SemaphoreSlim _lock = new(1, 1);
	private readonly ISerializer _serializer;
	private readonly IDeserializer _deserializer;

	public YamlActionLogService(string filePath = "/data/action_log.yaml", int maxEntries = 100)
	{
		_filePath = filePath;
		_maxEntries = maxEntries;

		_serializer = new SerializerBuilder()
			.WithNamingConvention(CamelCaseNamingConvention.Instance)
			.Build();

		_deserializer = new DeserializerBuilder()
			.WithNamingConvention(CamelCaseNamingConvention.Instance)
			.IgnoreUnmatchedProperties()
			.Build();
	}

	public async Task LogActionAsync(ActionLogEntry entry)
	{
		await _lock.WaitAsync();
		try
		{
			var entries = await LoadEntriesAsync();
			
			// Add new entry at the beginning (newest first)
			entries.Insert(0, entry);
			
			// Trim to max entries
			if (entries.Count > _maxEntries)
			{
				entries = entries.Take(_maxEntries).ToList();
			}
			
			await SaveEntriesAsync(entries);
		}
		finally
		{
			_lock.Release();
		}
	}

	public async Task<IEnumerable<ActionLogEntry>> GetRecentEntriesAsync(int count = 100)
	{
		await _lock.WaitAsync();
		try
		{
			var entries = await LoadEntriesAsync();
			return entries.Take(Math.Min(count, _maxEntries));
		}
		finally
		{
			_lock.Release();
		}
	}

	public async Task ClearAsync()
	{
		await _lock.WaitAsync();
		try
		{
			await SaveEntriesAsync(new List<ActionLogEntry>());
		}
		finally
		{
			_lock.Release();
		}
	}

	private async Task<List<ActionLogEntry>> LoadEntriesAsync()
	{
		if (!File.Exists(_filePath))
		{
			return new List<ActionLogEntry>();
		}

		try
		{
			var yaml = await File.ReadAllTextAsync(_filePath);
			if (string.IsNullOrWhiteSpace(yaml))
			{
				return new List<ActionLogEntry>();
			}

			var entries = _deserializer.Deserialize<List<ActionLogEntry>>(yaml);
			return entries ?? new List<ActionLogEntry>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[ActionLog] Error loading action log: {ex.Message}");
			return new List<ActionLogEntry>();
		}
	}

	private async Task SaveEntriesAsync(List<ActionLogEntry> entries)
	{
		try
		{
			var directory = Path.GetDirectoryName(_filePath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var yaml = _serializer.Serialize(entries);
			await File.WriteAllTextAsync(_filePath, yaml);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[ActionLog] Error saving action log: {ex.Message}");
		}
	}
}
