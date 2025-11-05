#!/usr/bin/with-contenv bashio

# Get add-on configuration
CONFIG_PATH="/data/options.json"
STORAGE_TYPE=$(bashio::config 'storage_type')
LOG_LEVEL=$(bashio::config 'log_level')
UPDATE_INTERVAL=$(bashio::config 'update_interval')
TIMEZONE=$(bashio::config 'timezone')

# Set timezone (use HA timezone if auto)
if [ "$TIMEZONE" == "auto" ]; then
    TIMEZONE=$(bashio::timezone)
fi
bashio::log.info "Setting timezone to: ${TIMEZONE}"
export TZ="${TIMEZONE}"

# Prepare configuration
bashio::log.info "Storage type: ${STORAGE_TYPE}"
bashio::log.info "Log level: ${LOG_LEVEL}"
bashio::log.info "Update interval: ${UPDATE_INTERVAL}s"

# Set environment variables for .NET app
export ASPNETCORE_ENVIRONMENT=Production
export Logging__LogLevel__Default="${LOG_LEVEL}"
export Storage__StorageType="${STORAGE_TYPE}"
export Storage__YamlPath="/data/timers.yaml"
export Storage__SqlitePath="/data/timers.db"
export HomeAssistant__SupervisorToken="${SUPERVISOR_TOKEN}"
export HomeAssistant__ApiUrl="http://supervisor/core/api"
export HomeAssistant__UpdateInterval="${UPDATE_INTERVAL}"

# Set Ingress path for URL rewriting
if bashio::var.has_value "$(bashio::addon.ingress_entry)"; then
    export INGRESS_PATH="$(bashio::addon.ingress_entry)"
    bashio::log.info "Ingress path: ${INGRESS_PATH}"
fi

# Ensure data directory exists
mkdir -p /data

# Log startup
bashio::log.info "Starting SwampTimers..."

# Start the application
cd /app
exec dotnet SwampTimers.dll
