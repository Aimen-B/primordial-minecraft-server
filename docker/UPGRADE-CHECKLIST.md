# Coordinated authentication update

Do not deploy this development update over the existing live service yet.

1. Inspect the actual Dokploy service, ports, domain proxy, image and mounted paths. Keep copies of the previous image and release.
2. Stop Minecraft cleanly. Back up the complete world, configuration including `forgelogin/users.properties`, whitelist, operators, and server properties. Restore the backup to a temporary directory and compare hashes.
3. Preserve existing mounted data. A volume mounted on `/server/mods` can hide updated image JARs; update that volume explicitly after backing it up. Remove older integration JARs to prevent duplicate mod IDs.
4. Install the compatible patched NefAUTH JAR and matching bridge JAR from the verified integration build. Keep existing password records intact. Install the same pinned gameplay additions on server and clients.
5. Route the existing HTTPS domain to web port 8080. Keep authentication port 8081 loopback-only. Do not publish it. Verify TLS certificates and API responses through the real HTTPS domain.
6. Test existing accounts and a newly approved exact nickname. Verify registration, incorrect passwords, restart persistence, inventory, world progress, and all skins between two clients.
7. Publish matching EXE, ZIP, manifest and SHA-256 sums only after the coordinated server works. Re-download and verify them in a fresh Windows directory before updating the public release link.
8. For rollback, stop the service, restore the prior mod set/image and backed-up mounted data, then verify the previous release joins.

Host approval: `/whitelist add <name>`, `/whitelist list`, `/whitelist remove <name>`. Use the exact name stored in the whitelist, including capitalization. Do not grant operator permissions to every friend.

Password resets require a private host maintenance workflow with the server stopped and account file backed up. Do not erase the entire account store or insert a shared password hash. Launcher “Forget account” only removes the remembered local credential.
