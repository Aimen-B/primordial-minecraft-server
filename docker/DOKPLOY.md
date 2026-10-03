# Dokploy & Docker Online Hosting Guide

This guide explains how to deploy **Primordial Adventures** on Dokploy or any Docker-enabled VPS, and connect your domain `mc.primordial.my`.

---

## 1. Domain & DNS Configuration
In your DNS provider (e.g., Cloudflare, Namecheap, or domain registrar for `primordial.my`):
* **Record Type**: `A`
* **Name / Host**: `mc`
* **Target / IP**: `<Your-VPS-Public-IP>`
* **Proxy Status**: **DNS Only / Gray Cloud** (⚠️ Turn Cloudflare Proxy **OFF**, as Minecraft uses raw TCP on port 25565, not HTTP/HTTPS).

---

## 2. Firewall / Port Forwarding
Ensure TCP port **25565** is open on your server:
* On Ubuntu VPS (UFW):
  ```bash
  sudo ufw allow 25565/tcp
  sudo ufw reload
  ```
* In your VPS provider security group (e.g. Hetzner, AWS, Oracle Cloud, DigitalOcean):
  * Add Inbound Rule: Protocol `TCP`, Port `25565`, Source `0.0.0.0/0`.

---

## 3. Deploying on Dokploy

### Option A: Via Dokploy Compose Project
1. Log into your **Dokploy** web dashboard.
2. Navigate to **Projects** -> Create a new project named `Minecraft`.
3. Click **Add Service** -> Select **Compose**.
4. Paste the contents of `docker/compose.yaml`.
5. Under environment variables or compose configuration, ensure the port mapping is `25565:25565/tcp`.
6. Deploy the service.

### Option B: Direct Docker Deployment (CLI)
If deploying directly on the VPS via terminal:
```bash
# Clone or copy the adventure folder to /opt/minecraft
cd /opt/minecraft/docker

# Run via Docker Compose
docker compose up -d --build
```

---

## 4. Migrating Local World & Player Passwords
To bring your existing local world, player accounts, and inventory to Dokploy:
1. Stop the Docker container:
   ```bash
   docker compose down
   ```
2. Copy these folders from your local `D:\minecraft\adventure\server` to the server's `./data` directory:
   * `world/` (saves map progress, buildings, chests, advancements)
   * `config/forgelogin/users.properties` (preserves player registered passwords)
   * `ops.json` (preserves OP permissions for `Primordial`)
3. Restart the container:
   ```bash
   docker compose up -d
   ```

---

## 5. Joining Online
Your friends will join directly using:
```
mc.primordial.my
```
(No port number required, because it uses the default Minecraft port 25565).
