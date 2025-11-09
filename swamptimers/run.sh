#!/usr/bin/env bash
set -e

# Get add-on configuration from options.json
CONFIG_PATH="/data/options.json"

# Parse configuration using jq (available in Home Assistant base images)
if [ -f "$CONFIG_PATH" ]; then
    STORAGE_TYPE=$(jq -r '.storage_type // "yaml"' "$CONFIG_PATH")
    LOG_LEVEL=$(jq -r '.log_level // "info"' "$CONFIG_PATH")
    UPDATE_INTERVAL=$(jq -r '.update_interval // 30' "$CONFIG_PATH")
    TIMEZONE=$(jq -r '.timezone // "auto"' "$CONFIG_PATH")
else
    # Defaults if config file doesn't exist
    STORAGE_TYPE="yaml"
    LOG_LEVEL="info"
    UPDATE_INTERVAL=30
    TIMEZONE="auto"
fi

# Set timezone (default to UTC if auto)
if [ "$TIMEZONE" == "auto" ]; then
    TIMEZONE="UTC"
fi
echo "Setting timezone to: ${TIMEZONE}"
export TZ="${TIMEZONE}"

# Prepare configuration
echo "Storage type: ${STORAGE_TYPE}"
echo "Log level: ${LOG_LEVEL}"
echo "Update interval: ${UPDATE_INTERVAL}s"

# Set environment variables for .NET app
export ASPNETCORE_ENVIRONMENT=Production
export Logging__LogLevel__Default="${LOG_LEVEL}"
export Storage__StorageType="${STORAGE_TYPE}"
export Storage__YamlPath="/data/timers.yaml"
export Storage__SqlitePath="/data/timers.db"
export HomeAssistant__SupervisorToken="${SUPERVISOR_TOKEN}"
export HomeAssistant__ApiUrl="http://supervisor/core/api"
export HomeAssistant__UpdateInterval="${UPDATE_INTERVAL}"

# Set Ingress path for URL rewriting (if provided by Supervisor)
if [ -n "$INGRESS_PATH" ]; then
    echo "Ingress path: ${INGRESS_PATH}"
fi

# Ensure data directory exists
mkdir -p /data

# Log startup
echo "Starting SwampTimers..."

# Start the application
cd /app
exec dotnet SwampTimers.dll
