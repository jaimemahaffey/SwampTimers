# =============================================================================
# SwampTimers Home Assistant Add-on Dockerfile
# 
# Uses cross-compilation to build for ARM64 from x64 runner (avoids QEMU issues)
# =============================================================================

# Build stage - runs NATIVELY on x64, cross-compiles output for ARM64
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project file and restore dependencies
COPY ["SwampTimers.csproj", "./"]
RUN dotnet restore "SwampTimers.csproj"

# Copy source code and cross-compile for ARM64
COPY . .
RUN dotnet publish "SwampTimers.csproj" \
    -c Release \
    -o /app/publish \
    --runtime linux-arm64 \
    --self-contained false \
    /p:UseAppHost=false

# =============================================================================
# Runtime stage - ARM64 Home Assistant base image
# =============================================================================
FROM ghcr.io/home-assistant/aarch64-base:latest

# Install .NET runtime dependencies
RUN apk add --no-cache \
    icu-libs \
    krb5-libs \
    libgcc \
    libintl \
    libssl3 \
    libstdc++ \
    zlib \
    bash \
    jq

# Install .NET ASP.NET Core runtime for ARM64
ENV DOTNET_ROOT=/usr/share/dotnet
RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 9.0 --runtime aspnetcore \
       --install-dir ${DOTNET_ROOT} --architecture arm64 --no-path \
    && rm dotnet-install.sh
ENV PATH="${PATH}:${DOTNET_ROOT}"

# Copy published app from build stage
WORKDIR /app
COPY --from=build /app/publish .

# Copy run script
COPY swamptimers/run.sh /
RUN chmod a+x /run.sh

# Environment configuration
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Home Assistant labels
LABEL io.hass.name="SwampTimers" \
      io.hass.description="Advanced timer scheduling for Home Assistant" \
      io.hass.arch="aarch64" \
      io.hass.type="addon"

CMD [ "/run.sh" ]
