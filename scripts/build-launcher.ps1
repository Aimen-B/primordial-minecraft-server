param([string]$OutputPath = '', [switch]$SyncDistribution)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$sources=Get-ChildItem (Join-Path $root 'launcher-src') -Filter '*.cs' | ForEach-Object FullName
if (!$OutputPath) { $OutputPath=Join-Path $root 'verification\PrimordialLauncher-development.exe' }
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe ('/out:'+$OutputPath) /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:System.Web.Extensions.dll /reference:System.Security.dll $sources
if($LASTEXITCODE -ne 0){throw 'Launcher compilation failed'}
if (!$SyncDistribution) { Write-Output "Built development launcher: $OutputPath"; return }
Copy-Item -LiteralPath $OutputPath -Destination (Join-Path $root 'PrimordialLauncher.exe') -Force
Copy-Item -LiteralPath $OutputPath -Destination (Join-Path $root 'web\PrimordialLauncher.exe') -Force
$portableExe = Join-Path $root 'portable-build\Primordial-Adventures-Portable\PrimordialLauncher.exe'
if (Test-Path (Split-Path $portableExe -Parent)) {
    Copy-Item -LiteralPath (Join-Path $root 'PrimordialLauncher.exe') -Destination $portableExe -Force
}
