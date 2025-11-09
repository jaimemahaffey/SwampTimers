# Build stage
ARG BUILD_FROM=ghcr.io/home-assistant/amd64-base:latest
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["SwampTimers.csproj", "./"]
RUN dotnet restore "SwampTimers.csproj"

# Copy source and build
COPY . .
RUN dotnet publish "SwampTimers.csproj" \
    -c Release \
    -o /app/publish \
    --self-contained false \
    --runtime linux-x64 \
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
ENV DOTNET_ROOT=/usr/share/dotnet
RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 9.0 --runtime aspnetcore --install-dir ${DOTNET_ROOT} --no-path \
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
