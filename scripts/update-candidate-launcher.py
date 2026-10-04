"""Update small candidate files without loading or recompressing the full ZIP.

Uses Python 3.11 ZipFile's central-directory bookkeeping. Final archive integrity
is independently verified by verification/verify-candidate.py.
"""
from pathlib import Path
import copy,hashlib,json,shutil,struct,zipfile
root=Path(__file__).resolve().parents[1]
stage=root/'portable-build/Primordial-1.2.0-dev-20261004191847'
release=root/'portable-build/release-1.2.0-dev'
shutil.copy2(root/'verification/PrimordialLauncher-development.exe',stage/'PrimordialLauncher.exe')
def digest(path):
 h=hashlib.sha256()
 with path.open('rb') as stream:
  while chunk:=stream.read(1024*1024):h.update(chunk)
 return h.hexdigest()
manifest=json.loads((stage/'package-files.json').read_text())
for item in manifest['files']:
 if item['path']=='PrimordialLauncher.exe':item['sha256']=digest(stage/'PrimordialLauncher.exe')
(stage/'package-files.json').write_text(json.dumps(manifest,indent=2))
archive=release/'Primordial-Adventures-Portable.zip'
temporary=release/'launcher-update.zip.partial'
replacements={'PrimordialLauncher.exe','package-files.json'}
with zipfile.ZipFile(archive) as old:
 assert all(not (item.flag_bits&8) for item in old.infolist()),'Use the streaming full builder for data-descriptor archives'
 with zipfile.ZipFile(temporary,'w',zipfile.ZIP_DEFLATED,compresslevel=3) as output:
  for item in old.infolist():
   if item.filename in replacements:
    output.write(stage/item.filename,item.filename);continue
   old.fp.seek(item.header_offset)
   header=old.fp.read(30)
   assert header[:4]==b'PK\x03\x04','Invalid local record'
   name_length,extra_length=struct.unpack_from('<HH',header,26)
   old.fp.seek(item.header_offset)
   record_size=30+name_length+extra_length+item.compress_size
   info=copy.copy(item);info.header_offset=output.fp.tell()
   while record_size:
    block=old.fp.read(min(record_size,1024*1024))
    assert block,'Truncated source archive'
    output.fp.write(block);record_size-=len(block)
   output.filelist.append(info);output.NameToInfo[info.filename]=info
   output.start_dir=output.fp.tell();output._didModify=True
temporary.replace(archive)
shutil.copy2(stage/'PrimordialLauncher.exe',release/'PrimordialLauncher.exe')
manifest=json.loads((release/'release.json').read_text())
sums=[]
for name,asset in manifest['assets'].items():
 path=release/name;asset['size']=path.stat().st_size;asset['sha256']=digest(path)
 sums.append(asset['sha256']+'  '+name)
(release/'release.json').write_text(json.dumps(manifest,indent=2))
(release/'SHA256SUMS.txt').write_text('\n'.join(sums)+'\n')
print('Updated final launcher in candidate with bounded-memory raw ZIP copy.')
