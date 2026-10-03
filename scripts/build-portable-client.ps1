$ErrorActionPreference = 'Stop'

Write-Host "=========================================================="
Write-Host " Building Primordial Adventures Portable Pack for Friends "
Write-Host "=========================================================="

$root = "D:\minecraft\adventure"
$distDir = Join-Path $root "portable-build\Primordial-Adventures-Portable"
$zipPath = Join-Path $root "Primordial-Adventures-Portable.zip"

if (Test-Path $distDir) {
    Write-Host "Cleaning previous build folder..."
    Remove-Item -Recurse -Force $distDir
}

New-Item -ItemType Directory -Force (Join-Path $distDir "launcher") | Out-Null
New-Item -ItemType Directory -Force (Join-Path $distDir "java") | Out-Null
New-Item -ItemType Directory -Force (Join-Path $distDir "instances") | Out-Null

# 1. Copy ElyPrism Launcher Binaries
Write-Host "Copying ElyPrism Launcher binaries..."
$launcherSrc = "$env:LOCALAPPDATA\Programs\ElyPrismLauncher"
Copy-Item -Path "$launcherSrc\*" -Destination (Join-Path $distDir "launcher") -Recurse -Force

# Mark launcher as portable
Set-Content -Path (Join-Path $distDir "launcher\portable.txt") -Value ""

# Copy base configuration & assets from AppData so no downloading is needed
$appDataSrc = "$env:APPDATA\ElyPrismLauncher"
foreach ($sub in @("assets", "libraries", "meta", "catpacks", "icons", "iconthemes", "themes")) {
    $srcPath = Join-Path $appDataSrc $sub
    if (Test-Path $srcPath) {
        Write-Host "Copying $sub..."
        Copy-Item -Path $srcPath -Destination (Join-Path $distDir "launcher\$sub") -Recurse -Force
    }
}

# Copy base launcher config
if (Test-Path (Join-Path $appDataSrc "elyprismlauncher.cfg")) {
    Copy-Item -Path (Join-Path $appDataSrc "elyprismlauncher.cfg") -Destination (Join-Path $distDir "launcher\elyprismlauncher.cfg") -Force
}

# 2. Copy Bundled Java 21 Runtime
Write-Host "Copying Java 21 Runtime..."
$javaSrc = "D:\minecraft\runtime\jdk-21.0.12.1+1"
Copy-Item -Path $javaSrc -Destination (Join-Path $distDir "java\jdk-21.0.12.1+1") -Recurse -Force

# 3. Copy Pre-configured Instance
Write-Host "Copying Primordial Adventures instance..."
$instanceSrc = "$appDataSrc\instances\Primordial-Adventures"
Copy-Item -Path $instanceSrc -Destination (Join-Path $distDir "instances\Primordial-Adventures") -Recurse -Force

# Copy updated servers.dat to instance
Copy-Item -Path (Join-Path $root "pack\servers.dat") -Destination (Join-Path $distDir "instances\Primordial-Adventures\.minecraft\servers.dat") -Force

# 4. Create 1-Click Launch Script
Write-Host "Creating Play.cmd..."
$playCmdContent = @'
@echo off
setlocal
cd /d "%~dp0"
title Primordial Adventures Launcher

echo ==========================================================
echo          Starting Primordial Adventures Client
echo ==========================================================
echo.

:: Configure relative Java 21 path dynamically
set "JAVA_EXE=%~dp0java\jdk-21.0.12.1+1\bin\javaw.exe"
set "JAVA_CFG=%JAVA_EXE:\=/%"
powershell -NoProfile -Command "(Get-Content 'instances\Primordial-Adventures\instance.cfg') -replace '^JavaPath=.*', ('JavaPath=' + '%JAVA_CFG%') | Set-Content 'instances\Primordial-Adventures\instance.cfg'"

:: Launch directly into Primordial Adventures
start "" "%~dp0launcher\elyprismlauncher.exe" --launch "Primordial-Adventures"
'@
Set-Content -Path (Join-Path $distDir "Play.cmd") -Value $playCmdContent -Encoding ASCII

# 5. Create README Guide for Friends
Write-Host "Creating README-HOW-TO-PLAY.txt..."
$readmeContent = @'
================================================================
            PRIMORDIAL ADVENTURES - QUICK START GUIDE
================================================================

NO PURCHASE OR MOJANG ACCOUNT REQUIRED TO PLAY!

1. HOW TO START:
   - Double-click "Play.cmd".
   
2. IF ASKED FOR AN ACCOUNT:
   - Click "Accounts" in the top-right corner of the launcher.
   - Click "Add Offline" (or Ely.by if you have an Ely skin account).
   - Type your desired username and click OK.

3. LAUNCHING THE GAME:
   - Click "Launch" on Primordial Adventures.
   
4. JOINING THE SERVER:
   - Click "Multiplayer".
   - You will see two pre-saved servers:
     * "Primordial Online (mc.primordial.my)" -> For playing online together.
     * "Primordial Local (127.0.0.1:25567)"   -> For local network testing.
   - Double-click the server to join!

5. IN-GAME REGISTRATION (FIRST TIME ONLY):
   - When you join, your character will be protected by a password system.
   - Press 'T' to open chat.
   - Type: /register <password> <password>
     Example: /register secret123 secret123
   - On future visits, just type: /login <password>

================================================================
'@
Set-Content -Path (Join-Path $distDir "README-HOW-TO-PLAY.txt") -Value $readmeContent -Encoding UTF8

# 6. Create Zip Distribution
Write-Host "Creating zip package: $zipPath..."
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
Compress-Archive -Path "$distDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "Done! Portable client package ready at: $zipPath"
