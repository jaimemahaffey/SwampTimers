# SwampTimers

A Blazor Server application for managing timer schedules with MudBlazor components.

## Features

- Blazor Server (.NET 9)
- MudBlazor UI component library
- Timer scheduling system with SQLite persistence
- Docker containerization with nginx
- Duration timers and time range timers

## Running Locally

### Prerequisites
- .NET 9.0 SDK

### Run with dotnet
```bash
cd SwampTimers
dotnet run
```

The application will be available at `https://localhost:5001` or `http://localhost:5000`

## Running with Docker

### Prerequisites
- Docker
- Docker Compose (optional)

### Build and run with Docker
```bash
cd SwampTimers
docker build -t swamptimers:latest .
docker run -p 8080:80 swamptimers:latest
```

The application will be available at `http://localhost:8080`

### Run with Docker Compose
```bash
cd SwampTimers
docker-compose up
```

The application will be available at `http://localhost:8080`

To stop the container:
```bash
docker-compose down
```

## Project Structure

- `/Components/Pages` - Razor pages including timer management
- `/Components/Layout` - Layout components including navigation
- `/Components/Timers` - Timer-specific components
- `/Models` - Timer schedule models (DurationTimer, TimeRangeTimer)
- `/Services` - Timer service with SQLite implementation
- `/wwwroot` - Static files and assets
- `Dockerfile` - Multi-stage Docker build configuration
- `docker-compose.yml` - Docker Compose configuration

## Timer Features

Navigate to `/timers` to manage timer schedules:
- Duration timers: Start at a specific time, run for a set duration
- Time range timers: Turn on/off at specific times
- Day-of-week filtering
- SQLite persistence
- Real-time active timer status
