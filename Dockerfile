# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["SwampTimers.csproj", "./"]
RUN dotnet restore "SwampTimers.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "SwampTimers.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SwampTimers.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - using nginx to serve the static files
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Copy published app from publish stage
COPY --from=publish /app/publish/wwwroot .

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80
