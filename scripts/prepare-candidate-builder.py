from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[1]
files=['FarmersDelight-1.21.1-1.3.4.jar','artifacts-neoforge-13.2.5.jar','journeymap-neoforge-1.21.1-6.0.9.jar','skinrestorer-2.11.0+1.21-neoforge.jar','voicechat-neoforge-1.21.1-2.6.24.jar','grapplemod-neoforge-1.21.1-1.21.1-v13.jar']
mods=[{'filename':name,'sha256':hashlib.sha256((root/'server/mods'/name).read_bytes()).hexdigest()} for name in files]
(root/'pack/extra-mods.lock.json').write_text(json.dumps({'minecraft':'1.21.1','neoforge':'21.1.252','mods':mods},indent=2))
source=(root/'scripts/build-portable-client.ps1').read_text(encoding='utf-8-sig')
source=source.replace("param([string]$Version = '1.0.1')", "param([string]$Version = '1.2.0-dev')")
source=source.replace("(Join-Path $root 'PrimordialLauncher.exe')", "(Join-Path $root 'verification\\PrimordialLauncher-development.exe')")
point="Copy-Item -LiteralPath (Join-Path $root 'verification\\PrimordialLauncher-development.exe') -Destination $distDir"
source=source.replace(point,'''$extras=Get-Content (Join-Path $root 'pack\\extra-mods.lock.json') -Raw | ConvertFrom-Json
foreach($mod in $extras.mods) {
 $source=Join-Path $root ('server\\mods\\'+$mod.filename)
 if((Get-FileHash $source -Algorithm SHA256).Hash -ne $mod.sha256){throw 'Extra mod checksum failed'}
 Copy-Item -LiteralPath $source -Destination (Join-Path $game 'mods')
}
Copy-Item -LiteralPath (Join-Path $root 'verification\\integration-build\\primordial-bridge-1.2.0-dev.jar') -Destination (Join-Path $game 'mods')
Copy-Item -LiteralPath (Join-Path $root 'pack\\skins') -Destination $distDir -Recurse
'''+point)
source=source.replace('First join: /register <password> <password>. Later: /login <password>.','DEVELOPMENT BUILD: requires coordinated server deployment. Enter your approved nickname and password in the launcher; confirm only for first registration. Credentials are remembered for the current Windows user. Imported skins are previews; use bundled skins for multiplayer.')
source=source.replace("-Destination $distDir\nSet-Content", "-Destination (Join-Path $distDir 'PrimordialLauncher.exe')\nSet-Content")
source=source.replace("-Destination $releaseDir\n$assets", "-Destination (Join-Path $releaseDir 'PrimordialLauncher.exe')\n$assets")
(root/'scripts/build-candidate-client.ps1').write_text(source,encoding='utf-8')
print('Prepared pinned development candidate builder.')
