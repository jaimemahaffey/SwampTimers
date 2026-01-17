# Build stage - use multi-stage build with .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Accept BUILD_ARCH from builder (default to aarch64 for Home Assistant)
ARG BUILD_ARCH=aarch64
ARG TARGETPLATFORM

WORKDIR /src

# Copy project files and restore
COPY ["SwampTimers.csproj", "./"]
RUN dotnet restore "SwampTimers.csproj" --verbosity minimal

# Copy source and build
COPY . .

# Determine RID from BUILD_ARCH or TARGETPLATFORM
# Priority: BUILD_ARCH (from HA builder) > TARGETPLATFORM (from buildx) > default
RUN if [ -n "${BUILD_ARCH}" ]; then \
      case "${BUILD_ARCH}" in \
        "aarch64") DOTNET_RID="linux-arm64" ;; \
        "amd64") DOTNET_RID="linux-x64" ;; \
        "armv7"|"armhf") DOTNET_RID="linux-arm" ;; \
        *) DOTNET_RID="linux-arm64" ;; \
      esac; \
    elif [ -n "${TARGETPLATFORM}" ]; then \
      case "${TARGETPLATFORM}" in \
        "linux/arm64") DOTNET_RID="linux-arm64" ;; \
        "linux/amd64") DOTNET_RID="linux-x64" ;; \
        "linux/arm/v7") DOTNET_RID="linux-arm" ;; \
        *) DOTNET_RID="linux-arm64" ;; \
      esac; \
    else \
      DOTNET_RID="linux-arm64"; \
    fi && \
    echo "Building for BUILD_ARCH=${BUILD_ARCH}, TARGETPLATFORM=${TARGETPLATFORM}, RID=${DOTNET_RID}" && \
    dotnet publish "SwampTimers.csproj" \
      -c Release \
      -o /app/publish \
      --self-contained false \
      --runtime ${DOTNET_RID} \
      /p:UseAppHost=false

# Runtime stage
ARG BUILD_FROM=ghcr.io/home-assistant/aarch64-base:latest
FROM ${BUILD_FROM}

# Re-declare ARGs for this stage with defaults
ARG BUILD_ARCH=aarch64
ARG BUILD_VERSION=dev

# Install .NET 9 runtime dependencies
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

# Install .NET runtime from Microsoft
ENV DOTNET_ROOT=/usr/share/dotnet
RUN DOTNET_ARCH="arm64" && \
    if [ "${BUILD_ARCH}" = "amd64" ]; then DOTNET_ARCH="x64"; fi && \
    if [ "${BUILD_ARCH}" = "i386" ]; then DOTNET_ARCH="x86"; fi && \
    echo "Installing .NET runtime for BUILD_ARCH=${BUILD_ARCH} (dotnet arch: ${DOTNET_ARCH})" && \
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 9.0 --runtime aspnetcore --install-dir ${DOTNET_ROOT} --architecture ${DOTNET_ARCH} --no-path \
    && rm dotnet-install.sh

ENV PATH="${PATH}:${DOTNET_ROOT}"

# Copy published app
WORKDIR /app
COPY --from=build /app/publish .

# Copy add-on specific files
COPY swamptimers/run.sh /
RUN chmod a+x /run.sh

# Environment variables for Blazor Server
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Labels for Home Assistant
LABEL \
    io.hass.name="SwampTimers" \
    io.hass.description="Advanced timer scheduling for Home Assistant" \
    io.hass.arch="${BUILD_ARCH}" \
    io.hass.type="addon" \
    io.hass.version="${BUILD_VERSION}" \
    maintainer="Jaime Mahaffey <jaimemahaffey@gmail.com>"

CMD [ "/run.sh" ]
