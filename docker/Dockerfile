FROM eclipse-temurin:21-jre-jammy

LABEL maintainer="Primordial"
LABEL description="Primordial Adventures - NeoForge 1.21.1 Modded Server"

WORKDIR /server

# Install curl and ca-certificates for healthchecks and downloads
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    procps \
    && rm -rf /var/lib/apt/lists/*

# Standard Minecraft Server Port
EXPOSE 25565

# Copy base server runtime files
COPY server/libraries ./libraries
COPY server/user_jvm_args.txt ./
COPY server/eula.txt ./

# Copy entrypoint script
COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Volumes for persistent state
VOLUME ["/server/world", "/server/config", "/server/mods", "/server/logs"]

ENTRYPOINT ["/entrypoint.sh"]
