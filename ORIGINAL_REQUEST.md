# Original User Request

## 2026-10-04T15:45:57Z

Deliver, verify, and package the complete Primordial Adventures 1.2.0 client and server release: featuring the Character Skin Studio (16 cute/funny/fantasy skins and live cloner), zero-effort automatic launcher authentication, the survival Grappling Hook mobility mod, and verified portable distribution.

Working directory: D:\minecraft\adventure
Integrity mode: demo

## Requirements

### R1. Complete Launcher Build & Skin Studio UI
- Rebuild `PrimordialLauncher.exe` (v1.2.0) with the dedicated `🎨 Custom Skins` studio tab.
- Support all 16 built-in skins with category filters (`🌟 All`, `😂 Funny & Memes`, `💖 Cute & Cozy`, `⚔️ Fantasy`), live 2D/3D preview panel, player skin cloning via Minotar, and local `.png` file browsing.
- Display the active skin badge directly on the `Play Game` screen.

### R2. Seamless Auto-Login (Zero In-Game Chat Typing)
- Implement launcher-integrated automatic login (Option 3): launcher saves user credentials, writes `autologin.json`, and client auto-authenticates without requiring manual in-game chat commands.
- Pre-register and protect the `Primordial` host account in server `users.properties` so no unauthorized player can claim it.

### R3. Grappling Hook Integration
- Integrate and verify `grapplemod` for NeoForge 1.21.1 across both server (`server/mods/`) and client (`portable-build/.../mods/`).
- Verify crafting, physics, and gameplay controls without conflicts with existing combat and movement mods.

### R4. Verification & Distribution Packaging
- Rebuild the complete portable client package (`Primordial-Adventures-Portable`), update `package-files.json` checksums, and update web distribution assets.
- Verify local startup, mod loading, and server connectivity.

## Verification Resources

- Compiler: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` via `scripts/build-launcher.ps1`
- Java 21: `portable-build/Primordial-Adventures-Portable/java/jdk-21.0.12.1+1/bin/java.exe`
- Package Hasher: `scripts/generate-package-manifest.py`
- Server Configuration: `server/config/forgelogin/users.properties` and `docker/entrypoint.sh`

## Acceptance Criteria

### Launcher & UI
- [ ] `PrimordialLauncher.exe` compiles with 0 errors via .NET 4.0 `csc.exe`.
- [ ] Launching the executable displays the sidebar with Play, Custom Skins, Modpack, Settings, and Guide tabs.
- [ ] Category tabs filter skins instantly; clicking any skin updates the preview and active skin badge.

### Gameplay & Security
- [ ] Joining the server automatically authenticates without freezing the player or requiring manual `/register` or `/login` in chat.
- [ ] The `Primordial` account is pre-registered on the server and protected against impersonation.
- [ ] Grappling hook items are functional and accessible in-game.

### Packaging & Release
- [ ] `package-files.json` contains accurate SHA-256 hashes of all distribution files.
- [ ] Web distribution assets (`web/PrimordialLauncher.exe`, portable client bundle) are updated and ready for upload.
