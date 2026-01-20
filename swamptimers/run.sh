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

# Convert log level to .NET format (capitalize first letter)
case "$LOG_LEVEL" in
    debug) DOTNET_LOG_LEVEL="Debug" ;;
    info) DOTNET_LOG_LEVEL="Information" ;;
    warning) DOTNET_LOG_LEVEL="Warning" ;;
    error) DOTNET_LOG_LEVEL="Error" ;;
    *) DOTNET_LOG_LEVEL="Information" ;;
esac

# Set environment variables for .NET app
export ASPNETCORE_ENVIRONMENT=Production
export Logging__LogLevel__Default="${DOTNET_LOG_LEVEL}"
export Storage__StorageType="${STORAGE_TYPE}"
export Storage__YamlPath="/data/timers.yaml"
export Storage__SqlitePath="/data/timers.db"
export HomeAssistant__SupervisorToken="${SUPERVISOR_TOKEN}"
export HomeAssistant__ApiUrl="http://supervisor/core"
export HomeAssistant__UpdateInterval="${UPDATE_INTERVAL}"

# Set Ingress path for URL rewriting (if provided by Supervisor)
if [ -n "$INGRESS_PATH" ]; then
    echo "Ingress path: ${INGRESS_PATH}"
fi

# Home Assistant Integration diagnostics
echo ""
echo "=== Home Assistant Integration ==="
if [ -n "$SUPERVISOR_TOKEN" ]; then
    echo "Supervisor Token: PRESENT (${#SUPERVISOR_TOKEN} chars)"
    echo "Client Mode: REAL (will connect to Home Assistant)"
else
    echo "Supervisor Token: NOT FOUND"
    echo "Client Mode: MOCK (using fake entities for testing)"
    echo ""
    echo "DEBUG: Checking for token in alternative locations..."
    # Check if token might be in a file
    if [ -f "/run/s6/container_environment/SUPERVISOR_TOKEN" ]; then
        echo "  Found: /run/s6/container_environment/SUPERVISOR_TOKEN"
        SUPERVISOR_TOKEN=$(cat /run/s6/container_environment/SUPERVISOR_TOKEN)
        export SUPERVISOR_TOKEN
        export HomeAssistant__SupervisorToken="${SUPERVISOR_TOKEN}"
        echo "  Token loaded from S6 container environment (${#SUPERVISOR_TOKEN} chars)"
    fi
    # List relevant environment variables for debugging
    echo ""
    echo "DEBUG: Relevant environment variables:"
    env | grep -E "^(SUPERVISOR|HASSIO|HOME_ASSISTANT|INGRESS)" | sed 's/=.*/=<redacted>/' || echo "  (none found)"
fi
echo "API URL: ${HomeAssistant__ApiUrl}"
echo "=================================="
echo ""

# Ensure data directory exists
mkdir -p /data

# Log startup
echo "Starting SwampTimers..."
echo ""
echo "=== System Information ==="
echo "Architecture: $(uname -m)"
echo "Kernel: $(uname -r)"
echo ".NET Runtime: $(dotnet --version 2>&1 || echo 'Not found')"
echo ".NET Location: $(which dotnet)"
if [ -f /usr/share/dotnet/dotnet ]; then
	echo ".NET Binary Info: $(file /usr/share/dotnet/dotnet)"
fi
echo "=========================="
echo ""

# Start the application
cd /app
exec dotnet SwampTimers.dll
