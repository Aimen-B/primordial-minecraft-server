$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root 'verification'
New-Item -ItemType Directory -Force $output | Out-Null
$launcher = Join-Path $output 'PrimordialLauncher-tests.exe'
& (Join-Path $PSScriptRoot 'build-launcher.ps1') -OutputPath $launcher
$runner = Join-Path $output 'ClientModsTests.exe'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe ('/out:'+$runner) ('/reference:'+$launcher) /reference:System.Core.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:System.Web.Extensions.dll (Join-Path $root 'launcher-tests\ClientModsTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& $runner
if ($LASTEXITCODE -ne 0) { throw 'Client mod regression tests failed' }
. (Join-Path $PSScriptRoot 'client-mods.ps1')
$manifest = Get-ClientModManifest -Root $root
if ($manifest.mods.Count -ne 31 -or !($manifest.mods | Where-Object filename -Like 'grapplemod-*')) { throw 'Merged client mod manifest is incomplete' }
foreach ($file in @('build-portable-client.ps1','build-candidate-client.ps1','client-mods.ps1','build-launcher.ps1','test-client-mods.ps1')) {
    $tokens=$null; $errors=$null
    [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $file), [ref]$tokens, [ref]$errors) | Out-Null
    if ($errors.Count) { throw ($errors | Out-String) }
}
Write-Output 'Both release builders and shared manifest functions passed syntax checks.'
