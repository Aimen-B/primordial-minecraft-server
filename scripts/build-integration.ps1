param([string]$Version='1.2.0-dev')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$java=Join-Path (Split-Path $root -Parent) 'runtime\jdk-21.0.12.1+1\bin'
$clientLibraries=Join-Path $env:APPDATA 'ElyPrismLauncher\libraries'
$serverLibraries=Join-Path $root 'server\libraries'
$output=Join-Path $root 'verification\integration-build'
New-Item -ItemType Directory -Force $output | Out-Null
$client=Join-Path $clientLibraries 'net\neoforged\neoforge\21.1.252\neoforge-21.1.252-client.jar'
$server=Join-Path $serverLibraries 'net\neoforged\neoforge\21.1.252\neoforge-21.1.252-server.jar'
$authBase=Join-Path $root 'verification\NefAUTH-1.21.1-1.0.1.jar'
foreach($required in @($client,$server,$authBase)){if(!(Test-Path $required)){throw "Missing build prerequisite: $required"}}
$classpath=@($client,$server)+@(Get-ChildItem $serverLibraries -Recurse -Filter '*.jar'|ForEach-Object FullName)+@(Get-ChildItem $clientLibraries -Recurse -Filter '*.jar'|Where-Object {$_.FullName -notmatch '\\26\.'}|ForEach-Object FullName)+@($authBase)
$cpFile=Join-Path $output 'classpath.txt'
Set-Content $cpFile ('-cp "'+(($classpath -join ';').Replace('\','/'))+'"')
$sources=Get-ChildItem (Join-Path $root 'bridge-src\main') -Recurse -Filter '*.java'|ForEach-Object FullName
& (Join-Path $java 'javac.exe') ('@'+$cpFile) -proc:none -d (Join-Path $output 'classes') $sources
if($LASTEXITCODE -ne 0){throw 'Bridge compilation failed'}
& (Join-Path $java 'jar.exe') cf (Join-Path $output "primordial-bridge-$Version.jar") -C (Join-Path $output 'classes') com -C (Join-Path $root 'bridge-src\resources') .
if($LASTEXITCODE -ne 0){throw 'Bridge packaging failed'}
& (Join-Path $java 'javac.exe') ('@'+$cpFile) -proc:none -d (Join-Path $output 'auth') (Join-Path $root 'bridge-src\auth\com\codex\forgelogin\AuthHooks.java')
if($LASTEXITCODE -ne 0){throw 'Authentication facade compilation failed'}
$patched=Join-Path $output 'NefAUTH-1.21.1-1.0.1.jar'
Copy-Item -LiteralPath $authBase -Destination $patched -Force
& (Join-Path $java 'jar.exe') uf $patched -C (Join-Path $output 'auth') com
if($LASTEXITCODE -ne 0){throw 'Authentication facade packaging failed'}
Write-Output "Built local integration artifacts: $output"
