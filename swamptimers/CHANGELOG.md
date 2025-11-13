# Changelog

All notable changes to the SwampTimers Home Assistant Add-on will be documented in this file.

## [1.0.2] - 2025-11-12

### Fixed
- **ARM64 Architecture Support**: Fixed "exec format error" on ARM-based Home Assistant devices (Yellow, RPi)
	- Corrected .NET runtime installation to match target architecture (aarch64 -> arm64)
	- Added architecture mapping in Dockerfile for proper cross-platform builds
	- Added diagnostic logging to display system and .NET runtime information on startup

### Added
- System diagnostics logged on startup:
	- CPU architecture (uname -m)
	- .NET runtime version and location
	- Binary file type information for troubleshooting
- Build-time logging showing target architecture and .NET Runtime Identifier (RID)

### Changed
- Restricted add-on to aarch64 (ARM64) architecture only
	- Optimized for Home Assistant Yellow and Raspberry Pi devices
	- Removed amd64 support to simplify deployment and reduce build complexity

## [1.0.1] - 2025-11-12

### Changed
- Version bump to force image rebuild with architecture fixes
- Attempted fix for architecture mismatch issues

## [1.0.0] - 2025-11-11

### Added
- Initial Home Assistant Add-on release
- Blazor Server UI with Material Design (MudBlazor)
- Top navigation bar with responsive hamburger menu for mobile
- Timer scheduling system:
	- Duration Timers: Start at specific time, run for duration
	- Time Range Timers: Turn on/off at specific times
	- Day-of-week filtering
	- Midnight-spanning support
- Dual storage backends:
	- SQLite for structured data
	- YAML for human-readable configuration
- Home Assistant Ingress support for embedded web UI
- Data persistence in `/data` directory
- Configurable options:
	- Storage type (yaml/sqlite)
	- Log level (debug/info/warning/error)
	- Update interval (10-300 seconds)
	- Timezone (auto or specific)
- Dark theme matching Home Assistant aesthetic
- Data protection key persistence for secure session management

### Technical Details
- .NET 9.0 runtime
- Blazor Server with SignalR
- MudBlazor 8.13.0 UI components
- Multi-architecture Docker builds (aarch64, amd64)
- GitHub Container Registry (GHCR) image hosting
