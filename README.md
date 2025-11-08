# SwampTimers

Advanced timer scheduling for Home Assistant - A Blazor Server add-on with MudBlazor UI.

## Features

- **Home Assistant Add-on** - Native integration with Home Assistant
- **Blazor Server (.NET 9)** - Modern web UI framework
- **MudBlazor UI** - Material Design component library
- **Flexible Storage** - YAML or SQLite persistence
- **Duration Timers** - Start at a specific time, run for a duration
- **Time Range Timers** - Turn on/off at specific times
- **Day-of-Week Filtering** - Run timers on selected days only
- **Midnight Spanning** - Timers can cross midnight boundary
- **Future: HA Entity Control** - Control any Home Assistant entity

## Installation as Home Assistant Add-on

### Option 1: Add Repository (Recommended)
1. In Home Assistant, navigate to **Settings** → **Add-ons** → **Add-on Store**
2. Click the **⋮** (three dots) menu → **Repositories**
3. Add this repository URL: `https://github.com/yourusername/swamptimers`
4. Find **SwampTimers** in the add-on list
5. Click **Install**
6. Configure options (see DOCS.md)
7. Click **Start**
8. Access via the sidebar panel

### Option 2: Local Development
Build and test locally before publishing:
```bash
# Build the add-on
docker build -t local/swamptimers .

# Run locally
docker run -p 8080:8080 \
  -e SUPERVISOR_TOKEN=your_token \
  -v $(pwd)/data:/data \
  local/swamptimers
```

## Running Standalone (Without Home Assistant)

### Prerequisites
- .NET 9.0 SDK

### Run with dotnet
```bash
cd SwampTimers
dotnet run
```

The application will be available at `http://localhost:5095`

### Build Docker image for standalone use
```bash
docker build -t swamptimers:standalone .
docker run -p 8080:8080 -v $(pwd)/data:/data swamptimers:standalone
```

## Project Structure

```
SwampTimers/
├── config.yaml              # Home Assistant add-on configuration
├── build.yaml               # Multi-arch build configuration
├── Dockerfile               # Container definition for HA add-on
├── run.sh                   # Add-on entry point script
├── DOCS.md                  # User documentation
├── Components/
│   ├── Pages/              # Blazor pages (Timers, Settings)
│   ├── Layout/             # Layout components
│   └── Timers/             # Timer-specific components
├── Models/                 # Timer models and configuration options
├── Services/               # Storage services (SQLite, YAML)
└── wwwroot/                # Static assets
```

## Configuration

Add-on options (edit in HA add-on configuration):

```yaml
storage_type: yaml          # yaml or sqlite
log_level: info            # debug, info, warning, error
update_interval: 30        # seconds between timer checks
timezone: auto             # auto or specific timezone
```

## Timer Types

**Duration Timer:**
- Start time: When to activate (e.g., 6:00 AM)
- Duration: How long to stay active (e.g., 120 minutes)
- Active days: Which days of the week
- Example: Pool pump runs 6 AM - 8 AM on weekdays

**Time Range Timer:**
- On time: When to activate (e.g., sunset)
- Off time: When to deactivate (e.g., 11:00 PM)
- Can span midnight (e.g., 11 PM to 2 AM)
- Active days: Which days of the week
- Example: Outdoor lights on 6 PM - 11 PM daily

## Development Roadmap

### Phase 1: Core Add-on (Current)
- ✅ Add-on packaging and configuration
- ✅ YAML/SQLite storage backends
- ✅ Timer scheduling system
- ✅ Blazor Server UI with Ingress support

### Phase 2: Home Assistant Integration (Next)
- 🔄 HA API client
- 🔄 Entity control (turn on/off)
- 🔄 Custom service calls
- 🔄 Entity picker UI
- 🔄 Background execution service
- 🔄 HA event firing

### Phase 3: Advanced Features (Future)
- MQTT discovery (optional)
- Timer templates
- Conditional actions
- Scene support
- Multi-instance support
