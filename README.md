# Primordial Minecraft Server (NeoForge 1.21.1)

Modded Minecraft adventure server running on **NeoForge 1.21.1**, designed for cooperative exploration, magic, combat, and self-hosted multiplayer via **Docker** and **Dokploy**.

---

## Features
- **Offline / Free Multiplayer**: Configured with `online-mode=false` and `NefAUTH` password authentication so anyone can play without a Mojang account while keeping accounts secure.
- **Magic & Progression**: *Iron's Spells 'n Spellbooks* + *FTB Quests* (optional beginner guide).
- **Overhauled Combat**: *Simply Swords*, *Better Combat*, *Combat Roll*.
- **Dungeons & Exploration**: *When Dungeons Arise*, *Lootr* (individual loot containers), *Waystones*.
- **Admin & Utility Commands**: *FTB Essentials* (`/god`, `/fly`, `/home`, `/tpa`, `/spawn`, etc.).
- **Docker & Dokploy Ready**: Pre-built `Dockerfile`, `compose.yaml`, and automated startup script.

---

## Directory Structure
```
adventure/
├── docker/                 # Dockerfile, compose.yaml & Dokploy deployment guide
├── server/                 # NeoForge server files, mods, configs, and libraries
│   ├── config/             # Mod configurations & NefAUTH user database
│   ├── mods/               # 25 server-side mods
│   ├── libraries/          # NeoForge runtime libraries
│   ├── ops.json            # Operator permissions (Level 4: Primordial)
│   └── server.properties   # Server network and gameplay rules
├── pack/                   # Client manifest, mods.lock.json, and servers.dat
├── scripts/                # Startup, backup, and portable packaging scripts
└── BEGINNER-GUIDE.md       # In-game progression instructions
```

---

## Online Hosting via Dokploy
Full instructions are documented in [`docker/DOKPLOY.md`](docker/DOKPLOY.md).

### Quick Docker Start:
```bash
cd docker
docker compose up -d --build
```

---

## Client Setup
Friends can play using the pre-packaged portable bundle `Primordial-Adventures-Portable.zip`.
- Extract and run `Play.cmd`.
- Add an offline account in the launcher.
- Connect to `mc.primordial.my` (or local `127.0.0.1:25567`).
- Type `/register <password> <password>` on first join.
