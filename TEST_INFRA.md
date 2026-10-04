# Primordial Adventures 1.2.0 — End-to-End Test Infrastructure

## 1. Executive Summary & Architecture

The Primordial Adventures 1.2.0 End-to-End (E2E) Test Suite provides opaque-box, requirement-driven verification across the entire client, launcher, server security, and portable distribution ecosystem. It adheres strictly to the 4-tier testing methodology, validating observable contracts, cryptographic properties, binary formats, network protocols, and real-world user workflows.

```
                          +-------------------------------------------------+
                          |             run-e2e-tests.ps1 / .py             |
                          |            (Master E2E Test Runner)             |
                          +-----------------------+-------------------------+
                                                  |
         +--------------------+-------------------+--------------------+--------------------+
         |                    |                                        |                    |
         v                    v                                        v                    v
  +--------------+     +--------------+                         +--------------+     +--------------+
  |    Tier 1    |     |    Tier 2    |                         |    Tier 3    |     |    Tier 4    |
  |   Feature    |     |  Boundary &  |                         |Cross-Feature |     | Real-World   |
  |   Coverage   |     | Corner Cases |                         | Combinations |     |  Scenarios   |
  |  (27 tests)  |     |  (26 tests)  |                         |  (10 tests)  |     |  (5 tests)   |
  +--------------+     +--------------+                         +--------------+     +--------------+
```

---

## 2. 4-Tier Testing Methodology

The test suite is structured into four progressive tiers of increasing integration scope:

### Tier 1: Feature Coverage (27 Test Cases)
Validates primary happy-path behavior, schemas, and interface contracts for every feature in the scope inventory (minimum 5 tests per feature):
- **Launcher & Character Skin Studio**: Compilation via .NET 4.0 `csc.exe`, catalog v2 schema, 16 skin inventory, category partitioning, 64x64 textures & 80x144 previews, 2D composite blit rendering algorithm, Minotar URL generation, skin preferences persistence.
- **Seamless Auto-Login**: DPAPI credential encryption roundtrip (`ProtectedData.Protect`/`Unprotect`), `autologin.json` schema emission, client mod (`primordial_autologin`) JAR structure & metadata, server chat challenge pattern matching, credential forget/removal.
- **Host Account Security**: `users.properties` pre-registration of `primordial`, PBKDF2-HMAC-SHA256 hash conformance (120,000 iterations, 16-byte salt, 256-bit key), Docker entrypoint security enforcement, operator level 4 (`ops.json`) & whitelist pre-authorization, case-insensitive username normalization.
- **Grappling Hook Mobility Mod**: Symmetrical JAR deployment between server and client mods, all 22 survival crafting recipes validation, `NonConflictingKeyBinding` collision prevention, NeoForge metadata & dependencies, mod item registry definitions (`grapplehook`, `longfallboots`).
- **Distribution Packaging**: Portable bundle directory layout, Adoptium Java 21 LTS runtime verification, clean package validator, package manifest generator (`generate-package-manifest.py`), web distribution assets synchronization (`web/release.json`).

### Tier 2: Boundary & Corner Cases (26 Test Cases)
Validates edge conditions, failure recovery, adversarial inputs, resource stress, and offline modes (minimum 5 tests per feature):
- **Launcher & Skin Studio**: Empty/whitespace category queries, corrupted or non-PNG file rejection (magic byte validation), invalid skin dimension rejection (non-64x64/32), Minotar offline/unreachable network resilience, corrupted preference file fallback to `cozy_frog`, nickname path traversal sanitization.
- **Seamless Auto-Login**: Empty/whitespace password rejection, special characters & unicode password escaping in DPAPI and JSON, malformed `autologin.json` recovery without crash, chat challenge casing and color code variations, 2500ms debounce interval preventing command flooding.
- **Host Account Security**: Case-insensitive username lookup preventing account claim race, corrupted PBKDF2 hash detection and rejection, unregistered player redirection to `/register`, idempotent Docker registration check preventing duplicates, unauthenticated player capability stripping (Adventure gamemode, frozen movement).
- **Grappling Hook**: Malformed recipe JSON resilience, extreme physics & rope length parameter clamping, keybinding conflict prevention with Better Combat and Combat Roll, client/server byte-level parity, attachment packet coordinate boundary checking.
- **Distribution Packaging**: Corrupted download archive detection via SHA-256 comparison, path traversal attack prevention (`../` sequences), zero-byte or truncated file detection via manifest, user settings & world saves preservation on re-install/repair, cross-platform path separator normalization (`/`).

### Tier 3: Cross-Feature Combinations (10 Test Cases)
Validates pairwise interactions between interrelated subsystems:
1. **T3-01: Skin Selection + Game Launch**: Skin Studio selection updates ElyPrismLauncher offline account profile with matching texture and model metadata.
2. **T3-02: DPAPI Credential Storage + autologin.json + Client Mod Intercept**: Launcher saves credentials, emits `autologin.json`, and client mod parses JSON to auto-dispatch `/login`.
3. **T3-03: Auto-Login + Pre-Registered Host Account**: Launcher credentials configured for host account match server pre-registered `primordial` PBKDF2 hash.
4. **T3-04: GrappleMod + Portable Bundle**: Portable client bundle contains Java 21, ElyPrismLauncher, grapplemod, and all 32 client mods ready for launch.
5. **T3-05: Skin Studio Custom Import + Bundle Staging Isolation**: Importing custom skins writes strictly to local app data and preserves clean portable staging.
6. **T3-06: Auto-Login Offline Degraded Fallback**: Client mod safely bypasses login hooks when playing singleplayer offline without server connectivity.
7. **T3-07: Host Security Whitelist & Ops Consistency**: Pre-registered `Primordial` account is consistently configured across `users.properties`, `whitelist.json`, and `ops.json` (level 4).
8. **T3-08: Symmetrical Grapple Recipes (Client & Server)**: Recipe definitions inside client and server mod jars match identically, preventing multiplayer desync.
9. **T3-09: Launcher Build Pipeline + Web Assets Sync**: Building launcher updates root, web, and portable bundle copies with matching hashes.
10. **T3-10: Clean Client Install + Isolated User State**: Installing portable client to clean target creates fresh instance without inheriting previous user accounts.

### Tier 4: Real-World Application Scenarios (5 Test Cases)
Validates complete end-to-end player journeys and operational workflows:
1. **T4-01: First-Time Player Journey**: Fresh install -> input nickname & password -> select "Gentleman Duck" in Skin Studio -> verify active badge -> launcher emits DPAPI credential, offline profile, and `autologin.json` -> client launches with Java 21 -> client mod issues `/register`.
2. **T4-02: Returning Player Workflow**: Returning player opens launcher -> DPAPI pre-loads saved credentials -> skin badge restores "Gentleman Duck" -> clicks Play Game -> fresh `autologin.json` emitted -> client connects -> client mod issues `/login`.
3. **T4-03: Server Host Security Workflow**: Server boots via `docker/entrypoint.sh` -> `users.properties` pre-registers `primordial` -> host connects -> server verifies PBKDF2 hash -> Operator level 4 granted -> movement unlocked from AuthSpot.
4. **T4-04: Offline / Degraded Network Mode Workflow**: Client launches with zero internet connection -> Minotar cloner catches network failure gracefully -> local catalog displays 16 built-in skins -> singleplayer world launches with Java 21 and 32 mods.
5. **T4-05: Full Modded Survival Gameplay Verification**: NeoForge 21.1.252 + Java 21 LTS launches with all 32 mods -> grapplemod survival recipes verified (grapplebow, modifier block) -> non-conflicting keybindings verified alongside Better Combat -> autologin chat hooks verified.

---

## 3. Feature Inventory Coverage Matrix

| Feature ID | Feature Name | Requirement Source | Tier 1 Tests | Tier 2 Tests | Tier 3 Tests | Tier 4 Tests | Expected Output Authority |
|---|---|---|---|---|---|---|---|
| **F1** | Launcher Build & Compilation | `ORIGINAL_REQUEST §R1` | `T1-F1-01` | `T2-F1-06` | `T3-09` | `T4-01`, `T4-02` | .NET 4.0 `csc.exe` exit code 0, valid PE header |
| **F2** | Skin Studio UI & Layout | `ORIGINAL_REQUEST §R1` | `T1-F1-02` | `T2-F1-01` | `T3-01` | `T4-01` | `pack/skins/catalog.json` schema v2 |
| **F3** | 16 Built-in Skins & Categories | `ORIGINAL_REQUEST §R1` | `T1-F1-03` | `T2-F1-01` | `T3-01` | `T4-01`, `T4-04` | Catalog partitions: 4 funny, 6 cute, 6 fantasy |
| **F4** | Live 2D/3D Preview Panel | `ORIGINAL_REQUEST §R1` | `T1-F1-04`, `T1-F1-05` | `T2-F1-03` | `T3-01` | `T4-01` | 64x64 texture & 80x144 preview dimension specs |
| **F5** | Player Skin Cloning via Minotar | `ORIGINAL_REQUEST §R1` | `T1-F1-06` | `T2-F1-04` | `T3-05` | `T4-04` | Minotar HTTPS endpoint contract & error handling |
| **F6** | Local .PNG File Browsing | `ORIGINAL_REQUEST §R1` | `T1-F1-04` | `T2-F1-02` | `T3-05` | `T4-01` | PNG magic bytes `\x89PNG\r\n\x1a\n`, 64x64/64x32 |
| **F7** | Active Skin Badge on Play Screen | `ORIGINAL_REQUEST §R1` | `T1-F1-07` | `T2-F1-05` | `T3-01` | `T4-01`, `T4-02` | `%LOCALAPPDATA%` preference file & badge format |
| **F8** | Launcher DPAPI & autologin.json | `ORIGINAL_REQUEST §R2` | `T1-F2-01`, `T1-F2-02` | `T2-F2-01`, `T2-F2-02` | `T3-02` | `T4-01`, `T4-02` | Windows DPAPI entropy & JSON schema contract |
| **F9** | Client Auto-Login Mod Intercept | `ORIGINAL_REQUEST §R2` | `T1-F2-03`, `T1-F2-04` | `T2-F2-03`, `T2-F2-04` | `T3-02`, `T3-06` | `T4-01`, `T4-02` | ForgeLogin chat challenge strings & bytecode |
| **F10** | Host Account Pre-Registration | `ORIGINAL_REQUEST §R2` | `T1-F3-01`, `T1-F3-02` | `T2-F3-01`, `T2-F3-02` | `T3-03`, `T3-07` | `T4-03` | PBKDF2 120,000 iters in `users.properties` |
| **F11** | GrappleMod JAR Deployment | `ORIGINAL_REQUEST §R3` | `T1-F4-01`, `T1-F4-04` | `T2-F4-04` | `T3-04`, `T3-08` | `T4-05` | Symmetrical SHA-256 match in server & client |
| **F12** | GrappleMod 22 Crafting Recipes | `ORIGINAL_REQUEST §R3` | `T1-F4-02`, `T1-F4-03` | `T2-F4-01`, `T2-F4-03` | `T3-08` | `T4-05` | 22 survival recipe JSONs in mod JAR |
| **F13** | Portable Client Cleanliness | `ORIGINAL_REQUEST §R4` | `T1-F5-01`, `T1-F5-03` | `T2-F5-04` | `T3-05`, `T3-10` | `T4-01` | Absence of saves, screenshots, logs, credentials |
| **F14** | Package Files Manifest Hasher | `ORIGINAL_REQUEST §R4` | `T1-F5-04` | `T2-F5-03`, `T2-F5-05` | `T3-04` | `T4-05` | `scripts/generate-package-manifest.py` execution |
| **F15** | Web Distribution Assets Sync | `ORIGINAL_REQUEST §R4` | `T1-F5-05` | `T2-F5-01` | `T3-09` | `T4-01` | `web/PrimordialLauncher.exe` & `web/release.json` |
| **F16** | Local Startup & Modpack Loading | `ORIGINAL_REQUEST §R4` | `T1-F5-02` | `T2-F4-02`, `T2-F4-05` | `T3-04` | `T4-05` | Adoptium Java 21 LTS execution & 32 mods |

---

## 4. Test Suite File Structure

```
D:\minecraft\adventure\
├── TEST_INFRA.md                             # Test infrastructure architecture & matrix (this file)
├── TEST_READY.md                             # Test readiness signal with summary metrics
└── verification\
    ├── run-e2e-tests.ps1                     # Primary PowerShell test runner
    └── e2e\
        ├── __init__.py                       # Package initializer
        ├── base_test.py                      # Base test harness, DPAPI helpers, crypto & PNG utils
        ├── tier1_feature_coverage.py         # Tier 1 tests (27 test cases)
        ├── tier2_boundary_corner.py          # Tier 2 tests (26 test cases)
        ├── tier3_cross_feature.py            # Tier 3 tests (10 test cases)
        ├── tier4_real_world.py               # Tier 4 tests (5 test cases)
        └── test_runner.py                    # Master test runner with formatted terminal output
```

---

## 5. Execution Commands & Runner Usage

### Running the Full E2E Test Suite (All 4 Tiers)
Execute the master PowerShell runner from project root:
```powershell
powershell -ExecutionPolicy Bypass -File verification\run-e2e-tests.ps1
```

Or via direct Python invocation:
```cmd
python verification\e2e\test_runner.py
```

### Running an Isolated Tier
```powershell
# Tier 1: Feature Coverage only
powershell -ExecutionPolicy Bypass -File verification\run-e2e-tests.ps1 -Tier 1

# Tier 2: Boundary & Corner Cases only
powershell -ExecutionPolicy Bypass -File verification\run-e2e-tests.ps1 -Tier 2

# Tier 3: Cross-Feature Combinations only
powershell -ExecutionPolicy Bypass -File verification\run-e2e-tests.ps1 -Tier 3

# Tier 4: Real-World Scenarios only
powershell -ExecutionPolicy Bypass -File verification\run-e2e-tests.ps1 -Tier 4
```

### Exit Code Convention
- **Exit Code 0**: All executed test cases passed successfully.
- **Exit Code 1**: One or more test cases failed or threw an error. Failure details and stack traces are printed to standard output.
