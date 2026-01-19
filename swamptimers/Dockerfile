# =============================================================================
# SwampTimers Home Assistant Add-on Dockerfile
# 
# Strategy: Do ALL downloads and compilation in x64 build stage, then just
# copy files to ARM64 runtime stage (avoids QEMU issues with install scripts)
# =============================================================================

# Use Docker's automatic platform args
ARG BUILDPLATFORM
ARG TARGETPLATFORM
ARG TARGETARCH

# Build stage - runs on the BUILD platform (native x64 on GitHub runners)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Re-declare ARGs after FROM
ARG TARGETARCH

WORKDIR /src

# Copy project file and restore dependencies
COPY ["SwampTimers.csproj", "./"]
RUN dotnet restore "SwampTimers.csproj"

# Copy source code and cross-compile for target architecture (musl for Alpine)
COPY . .
RUN DOTNET_RID="linux-musl-arm64" && \
    if [ "$TARGETARCH" = "amd64" ]; then DOTNET_RID="linux-musl-x64"; fi && \
    echo "Cross-compiling for $DOTNET_RID (TARGETARCH=$TARGETARCH)" && \
    dotnet publish "SwampTimers.csproj" \
        -c Release \
        -o /app/publish \
        --runtime $DOTNET_RID \
        --self-contained false \
        /p:UseAppHost=false

# Download .NET runtime for Alpine/musl (while still on build platform)
# Must use linux-musl-* variant for Alpine-based Home Assistant images
RUN DOTNET_VERSION="9.0.1" && \
    DOTNET_ARCH="arm64" && \
    if [ "$TARGETARCH" = "amd64" ]; then DOTNET_ARCH="x64"; fi && \
    echo "Downloading .NET ASP.NET Core runtime ${DOTNET_VERSION} for linux-musl-${DOTNET_ARCH}" && \
    mkdir -p /dotnet-runtime && \
    wget -q "https://dotnetcli.azureedge.net/dotnet/aspnetcore/Runtime/${DOTNET_VERSION}/aspnetcore-runtime-${DOTNET_VERSION}-linux-musl-${DOTNET_ARCH}.tar.gz" -O /tmp/dotnet.tar.gz && \
    tar -xzf /tmp/dotnet.tar.gz -C /dotnet-runtime && \
    rm /tmp/dotnet.tar.gz

# =============================================================================
# Runtime stage - Target platform (ARM64 Home Assistant base image)
# =============================================================================
FROM ghcr.io/home-assistant/aarch64-base:latest

# Install .NET runtime dependencies
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
COPY --from=build /dotnet-runtime ${DOTNET_ROOT}
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

# S6 Overlay v3: Preserve environment variables (including SUPERVISOR_TOKEN)
# Without this, S6 clears the environment before starting services
ENV S6_KEEP_ENV=1

# Home Assistant labels
LABEL io.hass.name="SwampTimers" \
      io.hass.description="Advanced timer scheduling for Home Assistant" \
      io.hass.arch="aarch64" \
      io.hass.type="addon"

CMD [ "/run.sh" ]
