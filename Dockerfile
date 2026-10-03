FROM eclipse-temurin:21-jre-jammy

LABEL maintainer="Primordial"
LABEL description="Primordial Adventures - NeoForge 1.21.1 Modded Server"

WORKDIR /server

# Install curl, procps and ca-certificates
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    procps \
    && rm -rf /var/lib/apt/lists/*

# Standard Minecraft Server Port
EXPOSE 25565

# Copy server files
COPY server/libraries ./libraries
COPY server/mods ./mods
COPY server/config ./config
COPY server/defaultconfigs ./defaultconfigs
COPY server/user_jvm_args.txt ./
COPY server/eula.txt ./
COPY server/server.properties ./
COPY server/ops.json ./
COPY server/world ./world

# Copy entrypoint script
COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Volumes for persistent state
VOLUME ["/server/world", "/server/config", "/server/mods", "/server/logs"]

ENTRYPOINT ["/entrypoint.sh"]
