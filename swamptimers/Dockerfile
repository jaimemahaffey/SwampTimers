# Build stage
ARG BUILD_FROM
ARG BUILD_ARCH
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore (with caching)
COPY ["SwampTimers.csproj", "./"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    echo "Restoring NuGet packages..." && \
    dotnet restore "SwampTimers.csproj" --verbosity minimal

# Copy source and build
# Map Home Assistant arch to .NET Runtime Identifier (RID)
# aarch64 -> linux-arm64, amd64 -> linux-x64
COPY . .
RUN DOTNET_RID=$(case "${BUILD_ARCH}" in \
      "aarch64") echo "linux-arm64" ;; \
      "amd64") echo "linux-x64" ;; \
      *) echo "linux-x64" ;; \
    esac) && \
    echo "Building for architecture: ${BUILD_ARCH} (RID: ${DOTNET_RID})" && \
    dotnet publish "SwampTimers.csproj" \
      -c Release \
      -o /app/publish \
      --self-contained false \
      --runtime ${DOTNET_RID} \
      /p:UseAppHost=false

# Runtime stage
FROM ${BUILD_FROM}

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
# Map Home Assistant arch to .NET architecture
ENV DOTNET_ROOT=/usr/share/dotnet
RUN DOTNET_ARCH=$(case "${BUILD_ARCH}" in \
      "aarch64") echo "arm64" ;; \
      "amd64") echo "x64" ;; \
      *) echo "x64" ;; \
    esac) && \
    echo "Installing .NET runtime for architecture: ${BUILD_ARCH} (dotnet arch: ${DOTNET_ARCH})" && \
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
