$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$java = Join-Path (Split-Path $root -Parent) 'runtime\jdk-21.0.12.1+1\bin\java.exe'
$server = Join-Path $root 'server'
$guard = $null
try {
    $guard = [IO.File]::Open((Join-Path $server '.run.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
    Push-Location $server
    try {
        Write-Host 'Primordial Adventures: join 127.0.0.1:25567'
        Write-Host 'Wait for Done. Type stop to save, shut down, and create a backup.'
        & $java '@user_jvm_args.txt' '@libraries/net/neoforged/neoforge/21.1.252/win_args.txt' nogui
        $result = $LASTEXITCODE
    } finally { Pop-Location }
} finally { if ($guard) { $guard.Dispose() } }
if ($result -eq 0) { & "$PSScriptRoot\Backup-Server.ps1" } else { throw "Server exit code $result. See server\logs\latest.log." }
