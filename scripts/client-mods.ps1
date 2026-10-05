$ErrorActionPreference = 'Stop'

function Get-ClientModManifest {
    param([string]$Root, [string[]]$AdditionalJars = @(), [string]$Version = '', [switch]$ValidateServer)
    $records = @{}
    foreach ($lockName in @('mods.lock.json', 'extra-mods.lock.json')) {
        $lock = Get-Content -LiteralPath (Join-Path $Root ('pack\' + $lockName)) -Raw | ConvertFrom-Json
        if ($lock.minecraft -ne '1.21.1' -or $lock.neoforge -ne '21.1.252') { throw "Incompatible lock: $lockName" }
        foreach ($mod in $lock.mods) {
            if ($mod.client -eq 'unsupported') { continue }
            if ($mod.filename -notmatch '^[A-Za-z0-9_.+\-]+\.jar$' -or $mod.sha256 -notmatch '^[a-f0-9]{64}$') { throw 'Invalid locked mod' }
            if ($records.ContainsKey($mod.filename)) { throw "Duplicate locked mod: $($mod.filename)" }
            if (!$mod.url) { throw "No pinned download URL: $($mod.filename)" }
            if ($ValidateServer -and $mod.server -ne 'unsupported') {
                $serverJar = Join-Path $Root ('server\mods\' + $mod.filename)
                if (!(Test-Path -LiteralPath $serverJar) -or (Get-FileHash -LiteralPath $serverJar -Algorithm SHA256).Hash -ne $mod.sha256) { throw "Server/client mod mismatch: $($mod.filename)" }
            }
            $records[$mod.filename] = [ordered]@{ filename=$mod.filename; sha256=$mod.sha256; url=$mod.url }
        }
    }
    foreach ($jar in $AdditionalJars) {
        $file = Get-Item -LiteralPath $jar
        if (!$Version -or $records.ContainsKey($file.Name)) { throw 'Additional mods need a release version and a unique name' }
        $records[$file.Name] = [ordered]@{ filename=$file.Name; sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant(); url="https://github.com/Aimen-B/primordial-minecraft-server/releases/download/v$Version/$([Uri]::EscapeDataString($file.Name))" }
    }
    [ordered]@{ schema=1; minecraft='1.21.1'; neoforge='21.1.252'; mods=@($records.GetEnumerator() | Sort-Object Key | ForEach-Object Value) }
}

function Save-ClientModManifest {
    param($Manifest, [string]$Path)
    [IO.File]::WriteAllText($Path, ($Manifest | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
}

function Install-LockedClientMods {
    param([string]$Root, [string]$ModsFolder, $Manifest, [string[]]$AdditionalJars = @())
    New-Item -ItemType Directory -Force $ModsFolder | Out-Null
    foreach ($mod in $Manifest.mods) {
        $target = Join-Path $ModsFolder $mod.filename
        if ((Test-Path -LiteralPath $target) -and (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -eq $mod.sha256) { continue }
        $candidates = @((Join-Path $Root ('downloads\' + $mod.filename)), (Join-Path $Root ('server\mods\' + $mod.filename))) + $AdditionalJars
        $source = $candidates | Where-Object { (Test-Path -LiteralPath $_) -and (Split-Path $_ -Leaf) -eq $mod.filename -and (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash -eq $mod.sha256 } | Select-Object -First 1
        $partial = $target + '.partial'
        try {
            if ($source) { Copy-Item -LiteralPath $source -Destination $partial }
            else { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -UseBasicParsing -Uri $mod.url -OutFile $partial }
            if ((Get-FileHash -LiteralPath $partial -Algorithm SHA256).Hash -ne $mod.sha256) { throw "Mod checksum failed: $($mod.filename)" }
            Move-Item -LiteralPath $partial -Destination $target -Force
        } finally { if (Test-Path -LiteralPath $partial) { Remove-Item -LiteralPath $partial } }
    }
    Assert-ClientMods -ModsFolder $ModsFolder -Manifest $Manifest
}

function Assert-ClientMods {
    param([string]$ModsFolder, $Manifest)
    foreach ($mod in $Manifest.mods) {
        $path = Join-Path $ModsFolder $mod.filename
        if (!(Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $mod.sha256) { throw "Missing or incorrect packaged mod: $($mod.filename)" }
    }
    foreach ($file in Get-ChildItem -LiteralPath $ModsFolder -Filter '*.jar') {
        if ($file.Name -notin @($Manifest.mods | ForEach-Object filename)) { throw "Unexpected packaged mod: $($file.Name)" }
    }
}
