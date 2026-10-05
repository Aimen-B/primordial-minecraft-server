"""Refresh extra-mod pins without rewriting the maintained release builder."""
from pathlib import Path
import hashlib
import json
import subprocess
from urllib.parse import quote

root = Path(__file__).resolve().parents[1]
if subprocess.check_output(['git', 'status', '--porcelain', '--', 'server/mods'], cwd=root, text=True).strip():
    raise RuntimeError('Commit server mod changes before pinning their GitHub download URLs.')
revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
path = root / 'pack/extra-mods.lock.json'
lock = json.loads(path.read_text(encoding='utf-8-sig'))
for mod in lock['mods']:
    jar = root / 'server/mods' / mod['filename']
    mod['sha256'] = hashlib.sha256(jar.read_bytes()).hexdigest()
    mod['url'] = f'https://raw.githubusercontent.com/Aimen-B/primordial-minecraft-server/{revision}/server/mods/{quote(mod["filename"], safe="")}'
path.write_text(json.dumps(lock, indent=2) + '\n', encoding='utf-8')
print('Extra mod downloads pinned. Use the maintained build-candidate-client.ps1 to package.')
