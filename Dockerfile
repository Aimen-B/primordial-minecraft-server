FROM eclipse-temurin:21-jre-jammy

LABEL maintainer="Primordial"
LABEL description="Primordial Adventures - NeoForge 1.21.1 Modded Server & Web Hub"

WORKDIR /server

# Install curl, procps, ca-certificates and python3 for lightweight web serving
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    procps \
    python3 \
    && rm -rf /var/lib/apt/lists/*

# Expose Minecraft Game Port, Voice Chat UDP, and Web Landing Page Port
EXPOSE 25565
EXPOSE 24454/udp
EXPOSE 8080

# Copy server files
COPY server/libraries ./libraries
COPY server/mods ./mods
COPY server/config ./config
COPY server/defaultconfigs ./defaultconfigs
COPY server/user_jvm_args.txt ./
COPY server/eula.txt ./
COPY server/server.properties ./
COPY server/ops.json ./
COPY server/whitelist.json ./
RUN mkdir -p /server/world /server/config/forgelogin

# Copy Web Landing Page and Launcher download
COPY web ./web

# Copy entrypoint script
COPY docker/entrypoint.sh /entrypoint.sh
COPY docker/webserver.py /webserver.py
COPY docker/provision-admin.py /provision-admin.py
COPY docker/admin-bootstrap.json /admin-bootstrap.json
RUN chmod +x /entrypoint.sh

# Volumes for persistent state
VOLUME ["/server/world", "/server/config", "/server/mods", "/server/logs", "/server/backups"]

ENTRYPOINT ["/entrypoint.sh"]
