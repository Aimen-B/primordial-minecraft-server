#!/bin/bash
set -e

echo "=========================================================="
echo " Starting Primordial Adventures NeoForge Server in Docker "
echo "=========================================================="

# Ensure EULA is accepted
echo "eula=true" > /server/eula.txt

# Default memory settings if not set (safe for 4GB-8GB VPS)
if [ -z "$JVM_OPTS" ]; then
    JVM_OPTS="-Xms2G -Xmx4G"
fi
echo "Using JVM Options: $JVM_OPTS"

# Ensure server.properties exists or has correct defaults for Docker
if [ ! -f /server/server.properties ]; then
    echo "Creating default server.properties for Docker..."
    cat <<EOF > /server/server.properties
server-port=25565
server-ip=0.0.0.0
online-mode=false
white-list=true
enforce-whitelist=true
spawn-protection=16
pvp=false
difficulty=normal
max-players=5
gamemode=survival
motd=Primordial Adventures | Online
allow-flight=true
enforce-secure-profile=false
EOF
else
    # Ensure port, online-mode and whitelist security are enforced for docker hosting
    sed -i 's/^server-port=.*/server-port=25565/' /server/server.properties || true
    sed -i 's/^server-ip=.*/server-ip=0.0.0.0/' /server/server.properties || true
    sed -i 's/^online-mode=.*/online-mode=false/' /server/server.properties || true
    if grep -q "^white-list=" /server/server.properties; then
        sed -i 's/^white-list=.*/white-list=true/' /server/server.properties
    else
        echo "white-list=true" >> /server/server.properties
    fi
    if grep -q "^enforce-whitelist=" /server/server.properties; then
        sed -i 's/^enforce-whitelist=.*/enforce-whitelist=true/' /server/server.properties
    else
        echo "enforce-whitelist=true" >> /server/server.properties
    fi
    if grep -q "^spawn-protection=" /server/server.properties; then
        sed -i 's/^spawn-protection=.*/spawn-protection=16/' /server/server.properties
    else
        echo "spawn-protection=16" >> /server/server.properties
    fi
fi

# Ensure ops.json exists with Primordial
if [ ! -f /server/ops.json ]; then
    echo "Creating ops.json with Primordial as Operator..."
    cat <<EOF > /server/ops.json
[
  {
    "uuid": "31e2c49d-c4c2-39eb-94e0-3bfea8cfa448",
    "name": "Primordial",
    "level": 4,
    "bypassesPlayerLimit": false
  }
]
EOF
fi

# Ensure whitelist.json exists with Primordial pre-authorized
if [ ! -f /server/whitelist.json ] || [ ! -s /server/whitelist.json ] || [ "$(cat /server/whitelist.json 2>/dev/null)" = "[]" ]; then
    echo "Configuring whitelist.json with Primordial..."
    cat <<EOF > /server/whitelist.json
[
  {
    "uuid": "31e2c49d-c4c2-39eb-94e0-3bfea8cfa448",
    "name": "Primordial"
  }
]
EOF
fi

# Start automated background world backup routine (runs every 2 hours, keeps last 5 snapshots)
mkdir -p /server/backups
backup_routine() {
    while true; do
        sleep 7200
        if [ -d /server/world ]; then
            TIMESTAMP=$(date +%Y%m%d_%H%M%S)
            BACKUP_FILE="/server/backups/world_backup_${TIMESTAMP}.tar.gz"
            echo "[AutoBackup] Saving world snapshot to ${BACKUP_FILE}..."
            tar -czf "${BACKUP_FILE}" -C /server world 2>/dev/null || true
            # Keep only the 5 most recent backups
            ls -t /server/backups/world_backup_*.tar.gz 2>/dev/null | tail -n +6 | xargs -r rm -f
            echo "[AutoBackup] Snapshot complete. World is safe."
        fi
    done
}
backup_routine > /server/backup.log 2>&1 &

# Start background web server for minecraft.primordial.my on port 8080
if [ -d /server/web ]; then
    echo "Starting Web Server on port 8080 for minecraft.primordial.my..."
    python3 -m http.server 8080 --directory /server/web > /server/web.log 2>&1 &
fi

echo "Launching NeoForge 21.1.252 server on port 25565..."
exec java $JVM_OPTS @user_jvm_args.txt @libraries/net/neoforged/neoforge/21.1.252/unix_args.txt nogui
