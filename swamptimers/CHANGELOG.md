# Changelog

All notable changes to the SwampTimers Home Assistant Add-on will be documented in this file.

## [1.0.5] - 2025-11-14

### Fixed
- **Critical Build Architecture Bug**: Fixed BUILD_ARCH variable not available in build stage
	- Re-declared BUILD_ARCH in build stage (after FROM statement)
	- v1.0.4 only fixed runtime stage, but build stage was still publishing linux-x64 instead of linux-arm64
	- Application DLLs are now correctly built for ARM64 architecture

## [1.0.4] - 2025-11-13

### Fixed
- **Critical Architecture Bug**: Fixed BUILD_ARCH variable not being passed to runtime stage
	- Added ARG re-declaration in runtime stage (Docker ARGs don't persist across FROM statements)
	- This was causing .NET runtime to install x64 instead of arm64 despite architecture mapping logic
	- Root cause: BUILD_ARCH was empty in runtime stage, defaulting to x64

## [1.0.3] - 2025-11-12

### Fixed
- **Build Performance**: Optimized GitHub Actions build process
	- Removed amd64 from build matrix (aarch64 only)
	- Added NuGet package caching to speed up restore
	- Added 60-minute timeout to prevent infinite hangs
	- Reduced verbosity in dotnet restore for cleaner logs

### Changed
- GitHub Actions workflow now only builds for aarch64 architecture
- Dockerfile uses BuildKit cache mounts for faster NuGet package restore

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
