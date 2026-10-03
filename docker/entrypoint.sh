#!/bin/bash
set -e

echo "=========================================================="
echo " Starting Primordial Adventures NeoForge Server in Docker "
echo "=========================================================="

# Ensure EULA is accepted
echo "eula=true" > /server/eula.txt

# Default memory settings if not set
if [ -z "$JVM_OPTS" ]; then
    JVM_OPTS="-Xms4G -Xmx6G"
fi
echo "Using JVM Options: $JVM_OPTS"

# Ensure server.properties exists or has correct defaults for Docker
if [ ! -f /server/server.properties ]; then
    echo "Creating default server.properties for Docker..."
    cat <<EOF > /server/server.properties
server-port=25565
server-ip=0.0.0.0
online-mode=false
pvp=false
difficulty=normal
max-players=5
gamemode=survival
motd=Primordial Adventures | Online
allow-flight=true
enforce-secure-profile=false
EOF
else
    # Ensure port and online-mode are correct for online docker hosting
    sed -i 's/^server-port=.*/server-port=25565/' /server/server.properties || true
    sed -i 's/^server-ip=.*/server-ip=0.0.0.0/' /server/server.properties || true
    sed -i 's/^online-mode=.*/online-mode=false/' /server/server.properties || true
fi

# Ensure ops.json exists
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

echo "Launching NeoForge 21.1.252 server..."
exec java $JVM_OPTS @user_jvm_args.txt @libraries/net/neoforged/neoforge/21.1.252/unix_args.txt nogui
