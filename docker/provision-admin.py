"""Provision a preclaimed admin before Minecraft reads persistent account files."""
import json
import os
import re
import shutil
from pathlib import Path

def write(path, content):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        shutil.copy2(path, path.with_name(path.name + '.before-admin-bootstrap'))
    # Preserve inodes: ops.json may be an individual Docker bind mount.
    with path.open('w', encoding='utf-8') as stream:
        stream.write(content)
        stream.flush()
        os.fsync(stream.fileno())

def provision(root, account):
    name = account['name']
    if not re.fullmatch(r'[A-Za-z0-9_]{3,16}', name):
        raise ValueError('Invalid nickname')
    users = root / 'config/forgelogin/users.properties'
    text = users.read_text(encoding='latin-1') if users.exists() else ''
    previous = re.search(r'^' + re.escape(name.lower()) + r'\s*[=:]\s*(.*)$', text, re.M)
    if previous and previous.group(1).strip() != account['password_hash']:
        print('[Admin bootstrap] Existing aymen password retained; no new privileges granted.')
        return False
    paths = [root / 'ops.json', root / 'whitelist.json']
    lists = [json.loads(path.read_text()) if path.exists() else [] for path in paths]
    if not all(isinstance(records, list) for records in lists):
        raise ValueError('Invalid ops or whitelist JSON')
    if not previous:
        write(users, text.rstrip() + '\n' + name.lower() + '=' + account['password_hash'] + '\n')
    for path, records in zip(paths, lists):
        existing = next((record for record in records if record.get('name', '').lower() == name.lower() or record.get('uuid') == account['uuid']), None)
        entry = {'uuid': account['uuid'], 'name': name}
        if path.name == 'ops.json':
            entry.update(level=4, bypassesPlayerLimit=False)
        if existing is None:
            records.append(entry)
            write(path, json.dumps(records, indent=2) + '\n')
        elif any(existing.get(key) != value for key, value in entry.items()):
            existing.update(entry)
            write(path, json.dumps(records, indent=2) + '\n')
    print('[Admin bootstrap] aymen registered, whitelisted and operator level 4.')
    return True

if __name__ == '__main__':
    provision(Path('/server'), json.loads(Path('/admin-bootstrap.json').read_text()))
