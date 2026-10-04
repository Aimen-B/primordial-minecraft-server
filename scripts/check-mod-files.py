import urllib.request
import urllib.parse
import json

def check_project(slug):
    url = f"https://api.modrinth.com/v2/project/{slug}/version?game_versions=%5B%221.21.1%22%5D&loaders=%5B%22neoforge%22%5D"
    req = urllib.request.Request(url, headers={"User-Agent": "Primordial/1.0"})
    try:
        with urllib.request.urlopen(req) as resp:
            data = json.loads(resp.read().decode())
            print(f"=== Project: {slug} ===")
            print(f"Found {len(data)} versions for NeoForge 1.21.1")
            for v in data[:3]:
                print(f"Version: {v['name']} ({v['version_number']})")
                print(f"Dependencies: {v.get('dependencies', [])}")
                for f in v['files']:
                    print(f"  File: {f['filename']} (Size: {f['size']}) URL: {f['url']}")
    except Exception as e:
        print(f"Error checking {slug}: {e}")

check_project("hGWjTxOA")
check_project("tHHGzOFQ")
check_project("t1p8V2nl")
