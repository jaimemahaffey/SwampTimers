# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BlazorMudApp is a .NET 9 Blazor Server application using MudBlazor UI components. The application features a timer scheduling system with SQLite persistence.

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
docker build -t blazormudapp:latest .
docker run -p 8080:80 blazormudapp:latest
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
- Database: `timers.db` in the application root
- Service is registered as **Scoped** in DI container (required for Blazor Server)
- Database initialization happens on application startup in `Program.cs:18-23`
- Thread-safe using `SemaphoreSlim` for concurrent access

**Storage Design:**
- Single table `TimerSchedules` with discriminator column `TimerType`
- Polymorphic deserialization in `MapFromReader()` method
- `ActiveDays` stored as JSON array
- Time values stored as ISO 8601 strings

### Component Structure

```
Components/
├── Pages/           - Routable pages
│   ├── Timers.razor - Main timer management UI
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

## Important Implementation Notes

1. **Render Mode**: All interactive components must have `@rendermode InteractiveServer` directive
2. **Service Lifetime**: Timer service is Scoped (not Singleton) due to Blazor Server requirements
3. **Database Path**: SQLite database is relative to working directory (`timers.db`)
4. **Time Handling**: Uses `TimeOnly` for time-of-day values (not `DateTime` or `TimeSpan`)
5. **Midnight Spanning**: Both timer types include logic for time ranges that cross midnight

## Dependencies

- **MudBlazor 8.13.0**: UI component library
- **Microsoft.Data.Sqlite 9.0.0**: SQLite ADO.NET provider
- **.NET 9.0**: Target framework
