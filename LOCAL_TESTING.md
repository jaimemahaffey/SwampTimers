# Local Docker Testing Guide

This guide explains how to build and test the SwampTimers Home Assistant Add-on locally using Docker before deploying to Home Assistant.

## Prerequisites

- **Docker Desktop** installed and running on Windows
- **PowerShell** (comes with Windows)
- Basic understanding of Docker concepts

## Quick Start

### 1. Build the Docker Image

```powershell
.\build-local.ps1
```

This builds the Docker image using the same Dockerfile that Home Assistant uses, tagged as `swamptimers:local-test`.

### 2. Run the Container

```powershell
.\run-local.ps1
```

This starts the container with default settings:
- **Port:** http://localhost:8080
- **Storage:** YAML
- **Log Level:** Info
- **Data Directory:** `./test-data`

### 3. Access the Application

Open your browser and navigate to:
```
http://localhost:8080
```

### 4. Stop the Container

Press `Ctrl+C` in the PowerShell window where the container is running.

## Advanced Usage

### Custom Storage Type

Test with SQLite instead of YAML:

```powershell
.\run-local.ps1 -StorageType sqlite
```

### Custom Log Level

Run with debug logging:

```powershell
.\run-local.ps1 -LogLevel debug
```

### Custom Port

Run on a different port (e.g., 9090):

```powershell
.\run-local.ps1 -Port 9090
```

### Run in Background (Detached Mode)

Run the container in the background:

```powershell
.\run-local.ps1 -Detached
```

Then manage it with Docker commands:

```powershell
# View logs
docker logs -f swamptimers-local

# Stop the container
docker stop swamptimers-local

# Remove the container
docker rm -f swamptimers-local
```

### Combine Options

```powershell
.\run-local.ps1 -StorageType sqlite -LogLevel debug -Port 9090
```

## Development Workflow

### Typical Iteration Cycle

1. **Make code changes** to Blazor components, services, or models
2. **Build the image**: `.\build-local.ps1`
3. **Run the container**: `.\run-local.ps1`
4. **Test your changes** at http://localhost:8080
5. **Check logs** in the PowerShell window
6. **Stop the container** (Ctrl+C)
7. Repeat until satisfied

### Testing Different Scenarios

#### Test YAML Storage

```powershell
.\run-local.ps1 -StorageType yaml
# Create some timers in the UI
# Check test-data/timers.yaml to see the output
```

#### Test SQLite Storage

```powershell
.\run-local.ps1 -StorageType sqlite
# Create some timers in the UI
# Check test-data/timers.db with a SQLite viewer
```

#### Test Data Persistence

```powershell
# Run with YAML storage
.\run-local.ps1 -StorageType yaml

# Create some timers
# Stop the container (Ctrl+C)

# Run again - data should persist
.\run-local.ps1 -StorageType yaml
```

#### Test Fresh Start

To start with a clean slate, delete the test data:

```powershell
Remove-Item test-data\* -Force
.\run-local.ps1
```

## Understanding the Test Environment

### Data Directory

All timer data is stored in the `test-data/` directory:

- **YAML mode:** `test-data/timers.yaml`
- **SQLite mode:** `test-data/timers.db`

This directory is:
- Created automatically if it doesn't exist
- Mounted to `/data` inside the container
- Ignored by git (safe to delete and recreate)

### Environment Variables

The run script sets these environment variables (simulating Home Assistant):

```json
{
  "storage_type": "yaml",
  "log_level": "info",
  "update_interval": 30,
  "timezone": "auto"
}
```

These are passed to the container via the `OPTIONS` environment variable, just like Home Assistant does.

### Port Mapping

- **Container port:** 8080 (fixed, defined in Dockerfile)
- **Host port:** 8080 (default, customizable with `-Port`)

### Differences from Home Assistant

| Feature | Local Testing | Home Assistant |
|---------|--------------|----------------|
| **Access** | http://localhost:8080 | Via Ingress UI |
| **Data location** | `./test-data/` | `/data/` (HA managed) |
| **Authentication** | None | HA authentication |
| **API access** | Not available | Full HA API access |
| **MQTT** | Not available | Optional via HA |

## Troubleshooting

### Error: "Docker is not running"

**Solution:** Start Docker Desktop and wait for it to fully start.

### Error: "Image not found"

**Solution:** Build the image first:
```powershell
.\build-local.ps1
```

### Error: "Port already in use"

**Solution:** Either stop the conflicting service or use a different port:
```powershell
.\run-local.ps1 -Port 9090
```

### Build fails with .NET errors

**Possible causes:**
- NuGet package restore failed
- .NET SDK version mismatch
- Missing dependencies

**Solution:** Check the Dockerfile and ensure the .NET 9 SDK is properly configured.

### Container starts but UI doesn't load

**Check:**
1. Is the container running? `docker ps`
2. Are there errors in the logs? Check the PowerShell output
3. Is the port correct? Try http://localhost:8080 explicitly

### Data not persisting between runs

**Check:**
- Are you running with `-Detached` and recreating the container?
- Is the `test-data/` directory intact?
- Are file permissions correct?

### Changes not appearing after rebuild

**Solution:** Ensure you:
1. Stopped the old container
2. Rebuilt the image: `.\build-local.ps1`
3. Started a new container: `.\run-local.ps1`

## Deployment to Home Assistant

Once local testing is successful:

1. **Commit your changes:**
   ```bash
   git add .
   git commit -m "Your changes"
   git push origin main
   ```

2. **GitHub Actions automatically builds** the multi-architecture images

3. **In Home Assistant:**
   - Go to Settings → Add-ons
   - Click "Check for updates"
   - Update the SwampTimers add-on

## Tips

### Speed Up Builds

Docker caches layers. To speed up rebuilds:
- Make code changes without changing dependencies
- Only modify files, don't add/remove NuGet packages frequently

### Inspect the Container

To explore the running container:

```powershell
docker exec -it swamptimers-local /bin/sh
```

### View Build Logs

For detailed build output, add `--progress=plain`:

```powershell
docker build --progress=plain -t swamptimers:local-test -f swamptimers/Dockerfile .
```

### Clean Up

Remove all test artifacts:

```powershell
# Remove test data
Remove-Item test-data\* -Force

# Remove Docker image
docker rmi swamptimers:local-test

# Remove any stopped containers
docker rm swamptimers-local
```

## Script Reference

### build-local.ps1

**Purpose:** Builds the Docker image for local testing.

**Options:** None (uses hardcoded defaults)

**Output:** Docker image tagged as `swamptimers:local-test`

### run-local.ps1

**Purpose:** Runs the Docker container for local testing.

**Parameters:**
- `-StorageType <yaml|sqlite>` - Storage backend (default: yaml)
- `-LogLevel <debug|info|warning|error>` - Logging verbosity (default: info)
- `-Port <number>` - Host port to bind to (default: 8080)
- `-Detached` - Run in background mode

**Examples:**
```powershell
# Default
.\run-local.ps1

# SQLite with debug logging
.\run-local.ps1 -StorageType sqlite -LogLevel debug

# Custom port in background
.\run-local.ps1 -Port 9090 -Detached
```

## Additional Resources

- [Dockerfile](swamptimers/Dockerfile) - Container build configuration
- [config.yaml](swamptimers/config.yaml) - Add-on configuration
- [CLAUDE.md](CLAUDE.md) - Project documentation
- [Home Assistant Add-on Documentation](https://developers.home-assistant.io/docs/add-ons)
