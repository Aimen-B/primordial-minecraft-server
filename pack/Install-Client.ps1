param([string]$LauncherRoot = "$env:APPDATA\ElyPrismLauncher")
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
if (!(Test-Path -LiteralPath $LauncherRoot)) { throw 'PineconeMC data folder not found. Run PineconeMC once first, or pass -LauncherRoot with your launcher data path.' }
$instance = Join-Path $LauncherRoot 'instances\Primordial-Adventures'
if ((Test-Path -LiteralPath $instance) -and !(Test-Path -LiteralPath (Join-Path $instance '.primordial-installer'))) {
    throw 'Primordial-Adventures already exists. It was not changed. Back it up and use a separate instance for another version.'
}
New-Item -ItemType Directory -Force (Join-Path $instance '.minecraft\mods') | Out-Null
Set-Content -LiteralPath (Join-Path $instance '.primordial-installer') -Value '0.1.0'
$root = Split-Path $PSScriptRoot -Parent
. (Join-Path $root 'scripts\client-mods.ps1')
$clientManifest = Get-ClientModManifest -Root $root
Install-LockedClientMods -Root $root -ModsFolder (Join-Path $instance '.minecraft\mods') -Manifest $clientManifest
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'mmc-pack.json') -Destination $instance
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'instance.cfg') -Destination $instance
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'config') -Destination (Join-Path $instance '.minecraft') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'options.txt') -Destination (Join-Path $instance '.minecraft')
Write-Host 'Installed Primordial Adventures. Restart PineconeMC if needed. Select Java 21 in instance settings if the launcher asks.'
Write-Host 'The server is local-only for now. The host will supply the public address after Dokploy deployment.'
