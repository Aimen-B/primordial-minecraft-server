# Dokploy deployment guide

The current local candidate is a coordinated launcher and server update. It is not deployed. Follow [UPGRADE-CHECKLIST.md](UPGRADE-CHECKLIST.md) before changing the existing service.

## Connections

- `mc.primordial.my`: DNS points at the server, with any HTTP proxy disabled. Publish Minecraft TCP 25565.
- Simple Voice Chat uses UDP 24454 when enabled.
- `minecraft.primordial.my`: certificate-valid HTTPS routed to web port 8080.
- Authentication bridge port 8081 remains loopback-only inside the service. Never publish it directly.

Verify these against the actual Dokploy ports, proxy configuration and mounted directories. Preserve existing world, accounts, whitelist, operators and configuration. Do not assume image files replace a mounted mods directory.

## Friend access

The host approves each exact nickname with `whitelist add <name>` in the server console, or `/whitelist add <name>` in game with operator permission. Keep the whitelist enabled.

An approved friend enters that nickname and a password in the repaired launcher. An unclaimed nickname requires password confirmation. Subsequent launches use the remembered Windows-user credential. Registration and login use HTTPS and the matching client/server authentication bridge; ordinary password chat commands are not the normal flow.

Public self-whitelisting and shared invite codes are disabled in the local candidate. The old live website may still show the previous flow until the coordinated deployment is completed.

## Release and rollback

Back up the stopped live service and verify restoration before deployment. Retain its previous image, mod set and release. Test existing accounts, world persistence, two-player authentication and skin synchronization before publishing matching downloads and changing public links.

The detailed checklist is authoritative for this candidate. A local test backup is not a backup of the live service.
