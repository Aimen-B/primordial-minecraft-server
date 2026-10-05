param([string]$OutputPath = '', [switch]$SyncDistribution, [string[]]$AdditionalJars = @(), [string]$Version = '')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'client-mods.ps1')
$manifest = Get-ClientModManifest -Root $root -AdditionalJars $AdditionalJars -Version $Version
$manifestPath = Join-Path $root 'verification\launcher-client-mods.json'
New-Item -ItemType Directory -Force (Split-Path $manifestPath) | Out-Null
Save-ClientModManifest -Manifest $manifest -Path $manifestPath
$sources=Get-ChildItem (Join-Path $root 'launcher-src') -Filter '*.cs' | ForEach-Object FullName
if (!$OutputPath) { $OutputPath=Join-Path $root 'verification\PrimordialLauncher-development.exe' }
New-Item -ItemType Directory -Force (Split-Path $OutputPath) | Out-Null
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe ('/out:'+$OutputPath) ('/resource:'+$manifestPath+',PrimordialLauncher.client-mods.json') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:System.Web.Extensions.dll /reference:System.Security.dll $sources
if($LASTEXITCODE -ne 0){throw 'Launcher compilation failed'}
if (!$SyncDistribution) { Write-Output "Built development launcher: $OutputPath"; return }
Copy-Item -LiteralPath $OutputPath -Destination (Join-Path $root 'PrimordialLauncher.exe') -Force
Copy-Item -LiteralPath $OutputPath -Destination (Join-Path $root 'web\PrimordialLauncher.exe') -Force
$portableExe = Join-Path $root 'portable-build\Primordial-Adventures-Portable\PrimordialLauncher.exe'
if (Test-Path (Split-Path $portableExe -Parent)) {
    Copy-Item -LiteralPath (Join-Path $root 'PrimordialLauncher.exe') -Destination $portableExe -Force
}
