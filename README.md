# BlazorMudApp

A containerized Blazor WebAssembly application using MudBlazor components.

## Features

- Blazor WebAssembly (.NET 9)
- MudBlazor UI component library
- Docker containerization with nginx
- Sample page demonstrating various MudBlazor components

## Running Locally

### Prerequisites
- .NET 9.0 SDK

### Run with dotnet
```bash
cd BlazorMudApp
dotnet run
```

The application will be available at `https://localhost:5001` or `http://localhost:5000`

## Running with Docker

### Prerequisites
- Docker
- Docker Compose (optional)

### Build and run with Docker
```bash
cd BlazorMudApp
docker build -t blazormudapp:latest .
docker run -p 8080:80 blazormudapp:latest
```

The application will be available at `http://localhost:8080`

### Run with Docker Compose
```bash
cd BlazorMudApp
docker-compose up
```

The application will be available at `http://localhost:8080`

To stop the container:
```bash
docker-compose down
```

## Project Structure

- `/Pages` - Razor pages including the MudBlazor demo page
- `/Layout` - Layout components including navigation
- `/wwwroot` - Static files and assets
- `Dockerfile` - Multi-stage Docker build configuration
- `nginx.conf` - Nginx configuration for serving the Blazor WASM app
- `docker-compose.yml` - Docker Compose configuration

## MudBlazor Demo Page

Navigate to `/muddemo` to see a demonstration of various MudBlazor components including:
- Cards and Papers
- Buttons
- Text Fields
- Switches and Checkboxes
- Alerts
- Chips
- Progress bars
- Sliders
- Rating components
