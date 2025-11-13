#!/usr/bin/env pwsh
# Build script for local Docker testing of SwampTimers Home Assistant Add-on
# This mimics the Home Assistant add-on build process for local development

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "SwampTimers Local Docker Build" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ImageName = "swamptimers"
$ImageTag = "local-test"
$DockerfilePath = "swamptimers/Dockerfile"
$BuildContext = "."

# Check if Docker is running
Write-Host "Checking Docker status..." -ForegroundColor Yellow
try {
    docker info | Out-Null
    Write-Host "Docker is running" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if Dockerfile exists
if (-not (Test-Path $DockerfilePath)) {
    Write-Host "ERROR: Dockerfile not found at $DockerfilePath" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Building Docker image..." -ForegroundColor Yellow
Write-Host "  Image: ${ImageName}:${ImageTag}" -ForegroundColor Gray
Write-Host "  Dockerfile: $DockerfilePath" -ForegroundColor Gray
Write-Host "  Context: $BuildContext" -ForegroundColor Gray
Write-Host ""

# Build the Docker image
$BuildStartTime = Get-Date

# Detect architecture for local builds
$Architecture = if ([System.Environment]::Is64BitOperatingSystem) { "amd64" } else { "amd64" }
Write-Host "Build Context: ${BuildContext}"
Write-Host "Architecture: ${Architecture}"

docker build `
    -t "${ImageName}:${ImageTag}" `
    -f "$DockerfilePath" `
    --build-arg BUILD_ARCH=$Architecture `
    "$BuildContext"

if ($LASTEXITCODE -eq 0) {
    $BuildEndTime = Get-Date
    $BuildDuration = $BuildEndTime - $BuildStartTime

    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Image: ${ImageName}:${ImageTag}" -ForegroundColor Cyan
    Write-Host "Build time: $($BuildDuration.ToString('mm\:ss'))" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Run: .\run-local.ps1" -ForegroundColor White
    Write-Host "  2. Open: http://localhost:8080" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Red
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host "=====================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check the error messages above for details." -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
