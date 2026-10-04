"""Recover our earlier integration sources from retained local test artifacts.

This does not modify live server data or published release binaries.
"""
from pathlib import Path
import shutil
import zipfile

root = Path(__file__).resolve().parents[1]
source = root / 'verification/bridge-decompiled/com'
target = root / 'bridge-src/main/com'
shutil.copytree(source, target, dirs_exist_ok=True)
auth = root / 'bridge-src/auth/com/codex/forgelogin'
auth.mkdir(parents=True, exist_ok=True)
shutil.copy2(root / 'verification/auth-decompiled-101/com/codex/forgelogin/AuthHooks.java', auth)
resources = root / 'bridge-src/resources'
with zipfile.ZipFile(root / 'verification/primordial-bridge-1.1.0.jar') as jar:
    for name in jar.namelist():
        if name.endswith('/') or name.startswith('com/') or name == 'META-INF/MANIFEST.MF':
            continue
        destination = resources / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(jar.read(name))
entrypoint = root / 'docker/entrypoint.sh'
text = entrypoint.read_text()
start = text.index('# Ensure forgelogin users.properties exists')
end = text.index('# Start automated background world backup', start)
text = text[:start] + '# Existing account records are mounted data. Never seed shared password hashes.\n\n' + text[end:]
entrypoint.write_text(text, newline='\n')
print('Recovered integration sources; removed Docker password seeding; existing account data untouched.')
