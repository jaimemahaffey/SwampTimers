#!/usr/bin/env pwsh
# Run script for local Docker testing of SwampTimers Home Assistant Add-on
# This simulates the Home Assistant environment for local development

param(
    [string]$StorageType = "yaml",  # "yaml" or "sqlite"
    [string]$LogLevel = "info",     # "debug", "info", "warning", or "error"
    [int]$Port = 8080,
    [switch]$Detached
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "SwampTimers Local Docker Run" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ImageName = "swamptimers:local-test"
$ContainerName = "swamptimers-local"
$DataPath = Join-Path $PSScriptRoot "test-data"

# Validate storage type
if ($StorageType -notin @("yaml", "sqlite")) {
    Write-Host "ERROR: Invalid storage type '$StorageType'. Must be 'yaml' or 'sqlite'." -ForegroundColor Red
    exit 1
}

# Validate log level
if ($LogLevel -notin @("debug", "info", "warning", "error")) {
    Write-Host "ERROR: Invalid log level '$LogLevel'. Must be 'debug', 'info', 'warning', or 'error'." -ForegroundColor Red
    exit 1
}

# Check if Docker is running
Write-Host "Checking Docker status..." -ForegroundColor Yellow
try {
    docker info | Out-Null
    Write-Host "Docker is running" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if image exists
Write-Host "Checking if image exists..." -ForegroundColor Yellow
$ImageExists = docker images -q $ImageName
if (-not $ImageExists) {
    Write-Host "ERROR: Image '$ImageName' not found." -ForegroundColor Red
    Write-Host "Please run: .\build-local.ps1" -ForegroundColor Yellow
    exit 1
}
Write-Host "Image found" -ForegroundColor Green

# Create test-data directory if it doesn't exist
if (-not (Test-Path $DataPath)) {
    Write-Host "Creating test-data directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $DataPath | Out-Null
    Write-Host "Created: $DataPath" -ForegroundColor Green
}

# Stop and remove existing container if it exists
Write-Host "Checking for existing container..." -ForegroundColor Yellow
$ExistingContainer = docker ps -a -q -f name=$ContainerName
if ($ExistingContainer) {
    Write-Host "Removing existing container..." -ForegroundColor Yellow
    docker rm -f $ContainerName | Out-Null
    Write-Host "Removed existing container" -ForegroundColor Green
}

# Prepare options JSON (simulates Home Assistant add-on options)
$OptionsJson = @{
    storage_type = $StorageType
    log_level = $LogLevel
    update_interval = 30
    timezone = "auto"
} | ConvertTo-Json -Compress

Write-Host ""
Write-Host "Starting container..." -ForegroundColor Yellow
Write-Host "  Image: $ImageName" -ForegroundColor Gray
Write-Host "  Container: $ContainerName" -ForegroundColor Gray
Write-Host "  Port: $Port" -ForegroundColor Gray
Write-Host "  Data path: $DataPath" -ForegroundColor Gray
Write-Host "  Storage type: $StorageType" -ForegroundColor Gray
Write-Host "  Log level: $LogLevel" -ForegroundColor Gray
Write-Host ""

# Build docker run command
$DockerArgs = @(
    "run"
    "--name", $ContainerName
    "-p", "${Port}:8080"
    "-v", "${DataPath}:/data"
    "-e", "OPTIONS=$OptionsJson"
)

if ($Detached) {
    $DockerArgs += "-d"
} else {
    $DockerArgs += "-it"
    $DockerArgs += "--rm"
}

$DockerArgs += $ImageName

# Run the container
& docker $DockerArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host "Container started successfully!" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Access the application at:" -ForegroundColor Yellow
    Write-Host "  http://localhost:$Port" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Data is stored in:" -ForegroundColor Yellow
    Write-Host "  $DataPath" -ForegroundColor Cyan
    Write-Host ""

    if ($Detached) {
        Write-Host "Container is running in the background." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Useful commands:" -ForegroundColor Yellow
        Write-Host "  View logs:    docker logs -f $ContainerName" -ForegroundColor White
        Write-Host "  Stop:         docker stop $ContainerName" -ForegroundColor White
        Write-Host "  Remove:       docker rm -f $ContainerName" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host "Press Ctrl+C to stop the container." -ForegroundColor Yellow
        Write-Host ""
    }
} else {
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Red
    Write-Host "Failed to start container!" -ForegroundColor Red
    Write-Host "=====================================" -ForegroundColor Red
    Write-Host ""
    exit 1
}
