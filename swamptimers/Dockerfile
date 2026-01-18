# =============================================================================
# SwampTimers Home Assistant Add-on Dockerfile
# 
# Strategy: Do ALL downloads and compilation in x64 build stage, then just
# copy files to ARM64 runtime stage (avoids QEMU issues with install scripts)
# =============================================================================

# Build stage - runs NATIVELY on x64
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

# Download .NET runtime for ARM64 (in x64 stage to avoid QEMU)
RUN mkdir -p /dotnet-arm64 \
    && wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 9.0 --runtime aspnetcore \
       --install-dir /dotnet-arm64 --architecture arm64 --no-path \
    && rm /tmp/dotnet-install.sh

# =============================================================================
# Runtime stage - ARM64 Home Assistant base image
# =============================================================================
FROM ghcr.io/home-assistant/aarch64-base:latest

# Install .NET runtime dependencies (these packages don't have post-install scripts)
# Using --no-scripts to avoid QEMU issues with post-install scripts
RUN apk add --no-cache --no-scripts \
    icu-libs \
    krb5-libs \
    libgcc \
    libintl \
    libssl3 \
    libstdc++ \
    zlib \
    bash \
    jq

# Copy pre-downloaded .NET runtime from build stage
ENV DOTNET_ROOT=/usr/share/dotnet
COPY --from=build /dotnet-arm64 ${DOTNET_ROOT}
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
