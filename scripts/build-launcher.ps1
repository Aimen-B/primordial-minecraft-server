$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$sources=Get-ChildItem (Join-Path $root 'launcher-src') -Filter '*.cs' | ForEach-Object FullName
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe ('/out:'+(Join-Path $root 'PrimordialLauncher.exe')) /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:System.Web.Extensions.dll /reference:System.Security.dll $sources
if($LASTEXITCODE -ne 0){throw 'Launcher compilation failed'}
Copy-Item -LiteralPath (Join-Path $root 'PrimordialLauncher.exe') -Destination (Join-Path $root 'web\PrimordialLauncher.exe') -Force
