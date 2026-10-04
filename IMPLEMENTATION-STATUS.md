# Review of Gemini's work, 4 October 2026

Gemini published v1.1.0 and added a 16-skin studio and a grappling hook. Its public EXE was re-downloaded and matched its published manifest. Its reported 68 tests are predominantly file assertions and simulated workflows, not proof of actual multiplayer acceptance.

## Corrected locally

- Public self-whitelisting is rejected. The host approves exact nicknames.
- Docker no longer inserts shared host password hashes. Existing account data was not changed by this review.
- The password-chat automation build is disabled. Recovered NefAUTH integration preserves its password format and player restrictions.
- Launcher uses certificate-validated HTTPS sign-in, DPAPI credentials, masked fields, forgetting, confirmation for first claims, and single-use challenge proofs. No password is placed in Minecraft chat or process arguments.
- Invalid skin requests cannot claim an account. Failed password writes restore the in-memory state.
- Strict package version and file hashes are required; repair rollback includes removed mods and the package manifest.
- All 16 bundled skins are recognized by the integration. Custom imported/cloned textures are preview-only for multiplayer.
- Docker build excludes personal world and account records. Deployment must retain existing mounted data.

## Verified

- Launcher and bridge compile; actual launcher DPAPI roundtrip, forgetting, masking and primary control layout pass.
- Fresh install, repeated repair, settings/saves preservation, corrupt archive and path traversal rejection pass.
- Actual server API rejects unapproved names, wrong passwords, competing claims and invalid skin before account creation.
- Proof identity/challenge/UUID binding, replay rejection, ticket replacement and rate limiting pass.
- Two local players joined and automatically authenticated. One client visibly saw the other player's Red Panda skin. The second client subsequently exited unexpectedly, so sustained multiplayer and all-skin mutual visibility are not certified.
- Stopped test-server backup restored 112 identical files, including world, inventory marker, accounts, whitelist and selected skins. Server restart confirmed the world block marker.
- Development archive SHA-256 checks and every one of its 9,730 file hashes passed. It contains 32 mod JARs and 16 skins, with no personal accounts, passwords, saved worlds, screenshots, logs or password-chat mod.
- Launcher skin gallery was visually inspected with named previews and active selection.
- The complete 32-mod candidate installed through the actual installer into a new Windows directory, launched, joined the local server and authenticated. Forest Ranger rendered correctly with SkinRestorer present.
- A native-memory startup failure was reproduced. The fork's optimized JVM settings were disabled explicitly; runtime `jcmd VM.flags` confirmed G1 and absence of ZGC/pre-touch. A subsequent full-pack join and automatic login passed. The configuration settings were checked against the upstream ElyPrismLauncher source.

## Release boundary

Development output: `portable-build/release-1.2.0-dev`. It is not published and requires the matching server integration. Do not send it as a ready replacement for the existing online server.

All 16 skins between two clients and reconnects, sustained grappling gameplay, coordinated public download re-verification, and live deployment remain acceptance work. The grappling hook item and recipe registry were inspected, but its movement physics were not certified.

The user declined Dokploy access for this run. Live configuration inspection, backups and deployment remain pending. Pushing main appears to trigger live deployment, so these changes must not be pushed without resolving that dependency.

Never use the historical `TEST_READY.md` simulation totals as a release-ready verdict.
