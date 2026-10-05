# Verified client mods

The launcher verifies the active Primordial Adventures instance before each Play.
It checks SHA-256 for every mod, stages missing or corrupted downloads, backs up
old and unexpected jars outside `mods`, and verifies the resulting folder before
authentication and game launch. Failed downloads leave the active pack untouched;
failed file moves roll back. Player settings, saves and accounts are not modified
by mod repair. This is a managed modpack: additional jars are backed up rather
than loaded alongside the supported pack.

Both lock files feed `scripts/client-mods.ps1`. Extra mods have pinned GitHub
commit URLs and checksums; updates must change the URL and hash together.
Release builds also check shared jar hashes against `server/mods`, preventing
client/server drift in the produced bundle.

## Build and validate

Run `scripts/test-client-mods.ps1` on Windows. It compiles the launcher and runs
regressions without downloading Minecraft or launching a game.

Run `scripts/build-launcher.ps1 -OutputPath <path>` to build a replacement
launcher for an existing installation. The embedded base manifest includes both
lock files. Old portable bundles retain their Primordial authentication bridge
only if it is covered by their installed `package-files.json` hashes.

Use `scripts/build-portable-client.ps1 -Version 1.2.1` for the standard bundle,
or `scripts/build-candidate-client.ps1 -Version 1.2.1` when the matching bridge
has been built with the integration builder. Both rebuild the launcher, include
all client mods, and emit `client-mods.json` alongside the ZIP and `release.json`.
The candidate also emits the bridge jar as a release asset for individual repair.
The existing packaging prerequisites (Java, Prism libraries, and integration
artifacts for the candidate) are still required.

## Rollout

Upload **all** files emitted in `portable-build/release-1.2.1` to the same GitHub
release. Do not mix an older launcher, ZIP, mod manifest or release checksum file.
The website already redirects downloads to the latest GitHub release.

Users with an existing installation need the new launcher once. Place it beside
their existing launcher files under the name `PrimordialLauncher.exe` while
Minecraft and PineconeMC are closed. Play then repairs the pack automatically;
there is no separate repair script or manual jar copying.

Once `client-mods.json` is published, Play reads it through the checksum-protected
release manifest. Pack-only updates can then be repaired without downloading the
whole portable bundle. A successful manifest is cached; if the release check is
unavailable, the cached or embedded pack is still hash-verified and can launch
when its files are intact. If a required download or verification fails, launch
stops and Play can be retried.

Minecraft/NeoForge version changes require a coordinated launcher and full game
update. Verification here covers jars; an actual multiplayer handshake should be
checked against the deployed server before marking a new release stable.
