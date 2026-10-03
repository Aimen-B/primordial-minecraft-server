# Dokploy & Docker Online Hosting Guide

This guide explains how to deploy **Primordial Adventures** on Dokploy or any Docker-enabled VPS, configure game connections on `mc.primordial.my`, and serve the web showcase on `minecraft.primordial.my`.

---

## 1. Domain & DNS Configuration
In your DNS provider (e.g. Cloudflare or domain registrar for `primordial.my`), add two DNS A records:

| Record Type | Name / Subdomain | Target IP | Proxy Status (Cloudflare) | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| **A** | `mc` | `<Your-VPS-IP>` | **DNS Only** (Gray cloud / Off) | Minecraft Game Connection (Port 25565) |
| **A** | `minecraft` | `<Your-VPS-IP>` | **Proxied** (Orange cloud / On) | Web Landing Page & Launcher Download |

> ⚠️ **Important:** Keep Cloudflare proxy **OFF** for `mc` because Minecraft uses raw TCP packets on port 25565. Cloudflare proxy can be **ON** for `minecraft` because it is an HTTPS website.

---

## 2. Firewall / Port Forwarding
Ensure TCP port **25565** is open on your VPS firewall:
* On Ubuntu VPS (UFW):
  ```bash
  sudo ufw allow 25565/tcp
  sudo ufw reload
  ```
* In your VPS provider security group (e.g. Hetzner, AWS, Oracle Cloud, DigitalOcean):
  * Add Inbound Rule: Protocol `TCP`, Port `25565`, Source `0.0.0.0/0`.

---

## 3. Configuring Dokploy (Application Service)

Your single Docker service runs **both** the Minecraft server and the web download hub:

### A. Ports Tab (for Minecraft)
1. In Dokploy, go to your service -> **Ports** tab.
2. Click **Add Port**:
   * **Published Port:** `25565`
   * **Target Port:** `25565`
   * **Protocol:** `TCP`
3. Click **Save**.

### B. Domains Tab (for the Minecraft Website & Downloads)
1. In Dokploy, go to your service -> **Domains** tab.
2. Click **Add Domain**:
   * **Host:** `minecraft.primordial.my`
   * **Path:** `/`
   * **Container Port:** `8080`
   * **HTTPS:** **Enabled** (Let's Encrypt SSL)
3. Click **Save**.

### C. Deploy
Click **Deploy**. Dokploy will:
* Build the container with all 25 mods and the web landing page.
* Run Minecraft NeoForge on port `25565`.
* Run the lightweight web server on port `8080` with automatic HTTPS on `minecraft.primordial.my`.

---

## 4. Player Joining & Downloads
* **Website:** Visitors go to `https://minecraft.primordial.my` to view the server status and download `PrimordialLauncher.exe`.
* **Game Connection:** Minecraft connects directly to `mc.primordial.my` (or `mc.primordial.my:25565`).
