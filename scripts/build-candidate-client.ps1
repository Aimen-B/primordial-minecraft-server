param([string]$Version = '1.2.1')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'client-mods.ps1')
$buildRoot = Join-Path $root 'portable-build'
$distDir = Join-Path $buildRoot ('Primordial-' + $Version + '-' + (Get-Date -Format 'yyyyMMddHHmmss'))
New-Item -ItemType Directory -Path $distDir | Out-Null
$launcher = Join-Path $distDir 'launcher'
$game = Join-Path $launcher 'instances\Primordial-Adventures\.minecraft'
New-Item -ItemType Directory -Force $game,(Join-Path $game 'mods') | Out-Null
$launcherSrc = Join-Path $env:LOCALAPPDATA 'Programs\ElyPrismLauncher'
Copy-Item -Path (Join-Path $launcherSrc '*') -Destination $launcher -Recurse
foreach ($private in @('accounts.json','elyprismlauncher.cfg','launcher_config.ini')) {
    $candidate = Join-Path $launcher $private
    if (Test-Path -LiteralPath $candidate) { Remove-Item -LiteralPath $candidate }
}
Set-Content (Join-Path $launcher 'portable.txt') ''
Set-Content (Join-Path $launcher 'elyprismlauncher.cfg') "[General]`nLanguage=en_US`nConfigVersion=1.3`nSelectedInstance=Primordial-Adventures`nInstanceDir=instances`nMinMemAlloc=512`nMaxMemAlloc=4096`nShowConsole=true"
$publicData = Join-Path $env:APPDATA 'ElyPrismLauncher'
foreach ($sub in @('assets','libraries','meta')) {
    Copy-Item -LiteralPath (Join-Path $publicData $sub) -Destination $launcher -Recurse
}
New-Item -ItemType Directory -Force (Join-Path $distDir 'java') | Out-Null
Copy-Item -LiteralPath (Join-Path (Split-Path $root -Parent) 'runtime\jdk-21.0.12.1+1') -Destination (Join-Path $distDir 'java\jdk-21.0.12.1+1') -Recurse
$instance = Split-Path $game -Parent
Copy-Item -LiteralPath (Join-Path $root 'pack\mmc-pack.json'),(Join-Path $root 'pack\instance.cfg') -Destination $instance
Add-Content (Join-Path $instance 'instance.cfg') "OverrideJavaLocation=true`nJavaPath=../../../../java/jdk-21.0.12.1+1/bin/javaw.exe"
Copy-Item -LiteralPath (Join-Path $root 'pack\config') -Destination $game -Recurse
Copy-Item -LiteralPath (Join-Path $root 'pack\options.txt'),(Join-Path $root 'pack\servers.dat') -Destination $game
$additionalJars = @()
$additionalJars = @((Join-Path $root 'verification\integration-build\primordial-bridge-1.2.0-dev.jar'))
$clientManifest = Get-ClientModManifest -Root $root -AdditionalJars $additionalJars -Version $Version -ValidateServer
Install-LockedClientMods -Root $root -ModsFolder (Join-Path $game 'mods') -Manifest $clientManifest -AdditionalJars $additionalJars
Save-ClientModManifest -Manifest $clientManifest -Path (Join-Path $distDir 'client-mods.json')
& (Join-Path $PSScriptRoot 'build-launcher.ps1') -OutputPath (Join-Path $distDir 'PrimordialLauncher.exe') -AdditionalJars $additionalJars -Version $Version
Copy-Item -LiteralPath (Join-Path $root 'pack\skins') -Destination $distDir -Recurse
Set-Content (Join-Path $distDir 'Play.cmd') "@echo off`r`ncd /d `"%~dp0`"`r`nstart `"`" `"%~dp0PrimordialLauncher.exe`"" -Encoding ASCII
Set-Content (Join-Path $distDir 'README-HOW-TO-PLAY.txt') "Primordial Adventures $Version`nRun PrimordialLauncher.exe. Use the exact nickname approved by the host.`nStart with 4 GB RAM. Join mc.primordial.my from Multiplayer.`nDEVELOPMENT BUILD: requires coordinated server deployment. Enter your approved nickname and password in the launcher; confirm only for first registration. Credentials are remembered for the current Windows user. Imported skins are previews; use bundled skins for multiplayer."
Set-Content (Join-Path $distDir 'pack-version.txt') $Version
$files = Get-ChildItem -LiteralPath $distDir -Recurse -File | ForEach-Object {
    [ordered]@{ path = $_.FullName.Substring($distDir.Length + 1).Replace('\','/'); sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
}
@{ version=$Version; files=@($files) } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $distDir 'package-files.json')
$releaseDir = Join-Path $buildRoot ('release-' + $Version)
New-Item -ItemType Directory -Force $releaseDir | Out-Null
$zip = Join-Path $releaseDir 'Primordial-Adventures-Portable.zip'
if (Test-Path -LiteralPath $zip) { throw "Release output already exists: $zip" }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($distDir,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
Copy-Item -LiteralPath (Join-Path $distDir 'PrimordialLauncher.exe') -Destination (Join-Path $releaseDir 'PrimordialLauncher.exe')
$assets = @{}
Copy-Item -LiteralPath (Join-Path $distDir 'client-mods.json') -Destination $releaseDir
foreach ($jar in $additionalJars) { Copy-Item -LiteralPath $jar -Destination $releaseDir }
foreach ($name in (@('PrimordialLauncher.exe','Primordial-Adventures-Portable.zip','client-mods.json') + @($additionalJars | ForEach-Object { Split-Path $_ -Leaf }))) {
    $file = Get-Item (Join-Path $releaseDir $name)
    $hash = (Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $assets[$name] = @{ url="https://github.com/Aimen-B/primordial-minecraft-server/releases/download/v$Version/$name"; size=$file.Length; sha256=$hash }
    "$hash  $name" | Add-Content (Join-Path $releaseDir 'SHA256SUMS.txt')
}
@{ schema=1; version=$Version; minecraft='1.21.1'; neoforge='21.1.252'; assets=$assets } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $releaseDir 'release.json')
Write-Output "Built $distDir"
Write-Output "Release files: $releaseDir"
