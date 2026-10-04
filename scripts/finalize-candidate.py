"""Rebuild the candidate ZIP with bounded memory, then refresh its checksums."""
import hashlib,json,shutil,zipfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
stage=root/'portable-build/Primordial-1.2.0-dev-20261004191847'
release=root/'portable-build/release-1.2.0-dev'
shutil.copy2(root/'verification/PrimordialLauncher-development.exe',stage/'PrimordialLauncher.exe')
cfg=(root/'pack/instance.cfg').read_text(encoding='utf-8-sig')
cfg+='\nOverrideJavaLocation=true\nJavaPath=../../../../java/jdk-21.0.12.1+1/bin/javaw.exe\n'
(stage/'launcher/instances/Primordial-Adventures/instance.cfg').write_text(cfg)
def digest(path):
 h=hashlib.sha256()
 with path.open('rb') as stream:
  while chunk:=stream.read(1024*1024):h.update(chunk)
 return h.hexdigest()
files=sorted(path for path in stage.rglob('*') if path.is_file() and path.name!='package-files.json')
manifest={'version':'1.2.0-dev','files':[{'path':path.relative_to(stage).as_posix(),'sha256':digest(path)} for path in files]}
(stage/'package-files.json').write_text(json.dumps(manifest,indent=2))
archive=release/'Primordial-Adventures-Portable.zip'
temporary=release/'Primordial-Adventures-Portable.zip.partial'
with zipfile.ZipFile(temporary,'w',zipfile.ZIP_DEFLATED,compresslevel=3) as output:
 for path in files+[stage/'package-files.json']:
  output.write(path,path.relative_to(stage).as_posix())
temporary.replace(archive)
shutil.copy2(stage/'PrimordialLauncher.exe',release/'PrimordialLauncher.exe')
assets={};sums=[]
for name in ['PrimordialLauncher.exe','Primordial-Adventures-Portable.zip']:
 path=release/name;sha=digest(path)
 assets[name]={'size':path.stat().st_size,'sha256':sha,'url':'https://github.com/Aimen-B/primordial-minecraft-server/releases/download/v1.2.0-dev/'+name}
 sums.append(sha+'  '+name)
(release/'release.json').write_text(json.dumps({'schema':1,'version':'1.2.0-dev','minecraft':'1.21.1','neoforge':'21.1.252','assets':assets},indent=2))
(release/'SHA256SUMS.txt').write_text('\n'.join(sums)+'\n')
print('Rebuilt candidate with bounded-memory ZIP creation, G1 instance defaults, and final launcher.')
