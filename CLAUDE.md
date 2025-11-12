# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SwampTimers is a .NET 9 Blazor Server application using MudBlazor UI components. The application features a timer scheduling system with configurable storage backends (SQLite or YAML).

## Development Commands

### Build and Run
```bash
dotnet restore
dotnet build
dotnet run
```

The application runs at `https://localhost:5001` or `http://localhost:5000`.

### Docker
```bash
docker build -t swamptimers:latest .
docker run -p 8080:80 swamptimers:latest
```

## Architecture

### Application Type
This is a **Blazor Server** application (not WebAssembly), using interactive server-side rendering with SignalR for real-time component updates. All components use `@rendermode InteractiveServer`.

### Timer Scheduling System

The core feature is a polymorphic timer scheduling system with two timer types:

**Timer Hierarchy:**
- `TimerSchedule` (abstract base class in `Models/TimerSchedule.cs`)
  - `DurationTimer`: Starts at a specific time and runs for a set duration
  - `TimeRangeTimer`: Turns on at one time and off at another

**Key Timer Features:**
- Both timer types support day-of-week filtering via `ActiveDays` property
- Timers can span midnight (e.g., 11 PM to 2 AM)
- Each timer calculates:
  - `IsActiveAt(DateTime)`: Whether the timer is active at a given time
  - `GetNextActivation(DateTime)`: Next time the timer will turn on
  - `GetNextDeactivation(DateTime)`: Next time the timer will turn off

### Data Persistence

**Service Layer:**
- `ITimerService`: Abstract interface for timer CRUD operations
- `SqliteTimerService`: SQLite implementation using ADO.NET (Microsoft.Data.Sqlite)
- `YamlTimerService`: YAML file-based implementation using YamlDotNet
- `TimerServiceFactory`: Factory pattern for creating service instances based on configuration
- Storage backend is configured in `appsettings.json` (see Configuration section below)
- Service is registered as **Scoped** in DI container (required for Blazor Server)
- Storage initialization happens on application startup in `Program.cs`
- Thread-safe using `SemaphoreSlim` for concurrent access

**SQLite Storage Design:**
- Single table `TimerSchedules` with discriminator column `TimerType`
- Polymorphic deserialization in `MapFromReader()` method
- `ActiveDays` stored as JSON array
- Time values stored as ISO 8601 strings
- Default file: `timers.db` in application root

**YAML Storage Design:**
- Human-readable YAML format for easy manual editing
- Timers stored in a list with type discriminator field
- Uses YamlDotNet with camelCase naming convention
- Default file: `timers.yaml` in application root
- **Custom TimeOnly Converter**: `TimeOnlyConverter` class handles `TimeOnly` serialization
	- Writes `TimeOnly` values as ISO time strings (e.g., `"14:30:00"`)
	- Reads both formats for backward compatibility:
		- New format: `startTime: "14:30:00"` (string)
		- Old format: `startTime: { hour: 14, minute: 30, second: 0 }` (object)
	- Automatic migration: Old format timers convert to new format on save
	- Located in `Services/YamlTimerService.cs`

### Component Structure

```
Components/
├── Pages/           - Routable pages
│   ├── Timers.razor - Main timer management UI
│   ├── Settings.razor - Application settings and storage configuration
│   ├── MudDemo.razor - MudBlazor component showcase
│   └── ...
├── Timers/          - Timer-specific components
│   ├── TimerEditDialog.razor - Edit dialog for timers
│   ├── DurationTimerEditor.razor - Duration timer form
│   └── TimeRangeTimerEditor.razor - Time range timer form
└── Layout/          - Layout components
```

### Dialog Pattern

The app uses MudBlazor's dialog system with a custom pattern:
- `DialogRefWrapper` class holds dialog reference for closure from callbacks
- `OnCancel` and `OnSubmit` EventCallbacks passed as dialog parameters
- Dialog closes programmatically via `dialogRefWrapper.Reference?.Close()`

## Configuration

The application uses `appsettings.json` for configuration. Storage backend is configured in the `Storage` section:

```json
{
  "Storage": {
    "StorageType": "Sqlite",  // "Sqlite" or "Yaml"
    "SqlitePath": "timers.db",
    "YamlPath": "timers.yaml"
  }
}
```

**Changing Storage Backend:**
1. Edit `appsettings.json` or `appsettings.Development.json`
2. Set `StorageType` to either `"Sqlite"` or `"Yaml"`
3. Optionally adjust file paths
4. Restart the application (changes require restart)

**Storage Configuration Model:**
- `StorageOptions` class in `Models/StorageOptions.cs`
- Registered in DI container via `IOptions<StorageOptions>`
- Accessible from Settings page (`/settings`)

## Important Implementation Notes

1. **Render Mode**: All interactive components must have `@rendermode InteractiveServer` directive
2. **Service Lifetime**: Timer service is Scoped (not Singleton) due to Blazor Server requirements
3. **Storage Paths**: Storage files are relative to working directory
4. **Time Handling**: Uses `TimeOnly` for time-of-day values (not `DateTime` or `TimeSpan`)
5. **Midnight Spanning**: Both timer types include logic for time ranges that cross midnight
6. **Storage Switching**: Changing storage type requires application restart and does not migrate data
7. **YAML TimeOnly Format**: YAML service uses custom converter for backward compatibility with existing data
8. **Code Style**: Use tabs for indentation, not spaces

## Dependencies

- **MudBlazor 8.13.0**: UI component library
- **Microsoft.Data.Sqlite 9.0.0**: SQLite ADO.NET provider
- **YamlDotNet 16.3.0**: YAML serialization library
- **.NET 9.0**: Target framework

## Troubleshooting

### YAML TimeOnly Serialization Error

**Issue**: "Expected 'Scalar', got 'MappingStart'" error when loading existing YAML files.

**Cause**: YamlDotNet's default camelCase naming convention attempts to serialize `TimeOnly` struct properties (`Hour`, `Minute`, `Second`) as nested objects, which causes deserialization to fail.

**Solution**: Custom `TimeOnlyConverter` class (in `Services/YamlTimerService.cs`) handles both formats:
- Old format (object): `startTime: { hour: 14, minute: 30, second: 0 }`
- New format (string): `startTime: "14:30:00"`

The converter automatically reads existing data in either format and writes new data in the cleaner string format. No manual data migration required.

**Prevention**: The custom converter is registered with both the YAML serializer and deserializer during `YamlTimerService` initialization, ensuring all `TimeOnly` values are handled correctly.

## Home Assistant Add-on

**Branch**: `feature/home-assistant-integration`

The project is being developed as a Home Assistant add-on to enable native integration with Home Assistant for automated entity control based on timer schedules.

### Add-on Architecture

**Repository Structure:**
- Add-on files are in the `swamptimers/` subdirectory
- `config.yaml`: Home Assistant add-on configuration
- `build.yaml`: Multi-architecture build configuration
- `Dockerfile`: Container definition for the add-on
- `run.sh`: Entry point script that handles initialization

**Integration Features:**
- **Ingress Support**: Web UI accessible through Home Assistant interface
- **API Access**: Configured with `hassio_api`, `homeassistant_api`, and `auth_api` for future entity control
- **Data Persistence**: Uses `/data` volume mount for timer storage
- **Multi-architecture**: Supports amd64 and aarch64 architectures

**Configuration Options** (in `config.yaml`):
- `storage_type`: Choose between "yaml" or "sqlite" (default: yaml)
- `log_level`: Set logging level - debug, info, warning, error (default: info)
- `update_interval`: Seconds between timer checks, 10-300 range (default: 30)
- `timezone`: Timezone handling, "auto" or specific timezone (default: auto)

**Container Images:**
- Published to GitHub Container Registry (GHCR)
- Image naming: `ghcr.io/jaimemahaffey/swamptimers-{arch}`
- Built via GitHub Actions on push to main branch
- **Note**: Repository and container images are private; see `PRIVATE_REPO_SETUP.md` for deployment instructions

### Development Phases

**Phase 1: Core Add-on (Complete)**
- Add-on packaging and configuration
- Blazor Server UI with Ingress support
- YAML/SQLite storage backends
- Timer scheduling system with UI

**Phase 2: Home Assistant Integration (Planned)**
- Home Assistant API client implementation
- Entity control (switches, lights, etc.)
- Custom service calls
- Entity picker UI component
- Background execution service for timer monitoring
- Home Assistant event firing on timer state changes

**Phase 3: Advanced Features (Future)**
- MQTT discovery support
- Timer templates
- Conditional actions
- Scene support
- Multi-instance support

### Local Testing

For testing the add-on locally:

```bash
# Build and run without full Home Assistant
docker build -t swamptimers:dev .
docker run -p 8080:8080 -v $(pwd)/data:/data swamptimers:dev
```

For complete deployment instructions, see `DEPLOYMENT.md` and `PRIVATE_REPO_SETUP.md`.

### Important Notes

- The add-on uses Blazor Server with Ingress, accessed via Home Assistant's sidebar panel
- Storage files persist in `/data` directory (mapped to add-on config directory)
- Changing storage type in add-on configuration requires restart
- Repository is configured as private; GitHub Personal Access Token required for installation
- GitHub Actions automatically builds multi-architecture images on push