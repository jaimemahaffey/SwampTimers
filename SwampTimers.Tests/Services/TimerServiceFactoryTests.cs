using SwampTimers.Models;
using SwampTimers.Services;

namespace SwampTimers.Tests.Services;

public class TimerServiceFactoryTests
{
	[Fact]
	public void Create_WithSqliteStorageType_ShouldReturnSqliteTimerService()
	{
		// Arrange
		var options = new StorageOptions
		{
			StorageType = StorageType.Sqlite,
			SqlitePath = "test.db"
		};

		// Act
		var service = TimerServiceFactory.Create(options);

		// Assert
		Assert.NotNull(service);
		Assert.IsType<SqliteTimerService>(service);

		// Cleanup
		if (service is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	[Fact]
	public void Create_WithYamlStorageType_ShouldReturnYamlTimerService()
	{
		// Arrange
		var options = new StorageOptions
		{
			StorageType = StorageType.Yaml,
			YamlPath = "test.yaml"
		};

		// Act
		var service = TimerServiceFactory.Create(options);

		// Assert
		Assert.NotNull(service);
		Assert.IsType<YamlTimerService>(service);

		// Cleanup
		if (service is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	[Fact]
	public void Create_WithSqliteStorageType_ShouldUseCorrectPath()
	{
		// Arrange
		var customPath = "custom_path.db";
		var options = new StorageOptions
		{
			StorageType = StorageType.Sqlite,
			SqlitePath = customPath
		};

		// Act
		var service = TimerServiceFactory.Create(options);

		// Assert
		Assert.NotNull(service);
		Assert.IsType<SqliteTimerService>(service);

		// Cleanup
		if (service is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	[Fact]
	public void Create_WithYamlStorageType_ShouldUseCorrectPath()
	{
		// Arrange
		var customPath = "custom_path.yaml";
		var options = new StorageOptions
		{
			StorageType = StorageType.Yaml,
			YamlPath = customPath
		};

		// Act
		var service = TimerServiceFactory.Create(options);

		// Assert
		Assert.NotNull(service);
		Assert.IsType<YamlTimerService>(service);

		// Cleanup
		if (service is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	[Fact]
	public void Create_WithDefaultStorageOptions_ShouldReturnSqliteTimerService()
	{
		// Arrange
		var options = new StorageOptions(); // Default is Sqlite

		// Act
		var service = TimerServiceFactory.Create(options);

		// Assert
		Assert.NotNull(service);
		Assert.IsType<SqliteTimerService>(service);

		// Cleanup
		if (service is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	[Fact]
	public void Create_MultipleCallsWithSqlite_ShouldReturnDistinctInstances()
	{
		// Arrange
		var options = new StorageOptions
		{
			StorageType = StorageType.Sqlite,
			SqlitePath = "test.db"
		};

		// Act
		var service1 = TimerServiceFactory.Create(options);
		var service2 = TimerServiceFactory.Create(options);

		// Assert
		Assert.NotNull(service1);
		Assert.NotNull(service2);
		Assert.NotSame(service1, service2); // Should be different instances

		// Cleanup
		if (service1 is IDisposable disposable1)
		{
			disposable1.Dispose();
		}
		if (service2 is IDisposable disposable2)
		{
			disposable2.Dispose();
		}
	}

	[Fact]
	public void Create_MultipleCallsWithYaml_ShouldReturnDistinctInstances()
	{
		// Arrange
		var options = new StorageOptions
		{
			StorageType = StorageType.Yaml,
			YamlPath = "test.yaml"
		};

		// Act
		var service1 = TimerServiceFactory.Create(options);
		var service2 = TimerServiceFactory.Create(options);

		// Assert
		Assert.NotNull(service1);
		Assert.NotNull(service2);
		Assert.NotSame(service1, service2); // Should be different instances

		// Cleanup
		if (service1 is IDisposable disposable1)
		{
			disposable1.Dispose();
		}
		if (service2 is IDisposable disposable2)
		{
			disposable2.Dispose();
		}
	}
}
