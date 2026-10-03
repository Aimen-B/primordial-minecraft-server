$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$server = Join-Path $root 'server'
$guard = $null
$worldLock = $null
try {
    $guard = [IO.File]::Open((Join-Path $server '.run.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
    $lockPath = Join-Path $server 'world\session.lock'
    if (Test-Path $lockPath) {
        $worldLock = [IO.File]::Open($lockPath, 'Open', 'ReadWrite', 'None')
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $name = 'primordial-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '.zip'
    $destination = Join-Path (Join-Path $root 'backups') $name
    $archive = [IO.Compression.ZipFile]::Open($destination, 'Create')
    try {
        Get-ChildItem $server -Recurse -File -Force | ForEach-Object {
            $relative = $_.FullName.Substring($server.Length + 1)
            if ($relative -notmatch '^(libraries|logs|crash-reports)[\\/]' -and $_.Name -notin @('session.lock','.run.lock')) {
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $relative.Replace('\','/'), 'Optimal') | Out-Null
            }
        }
    } finally { $archive.Dispose() }
    Write-Host "Backup saved: $destination"
} finally {
    if ($worldLock) { $worldLock.Dispose() }
    if ($guard) { $guard.Dispose() }
}
