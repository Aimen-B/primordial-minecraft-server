import os
import hashlib
import json

dist_dir = r"D:\minecraft\adventure\portable-build\Primordial-Adventures-Portable"
version = "1.2.0"

files_list = []

for root, _, files in os.walk(dist_dir):
    for f in files:
        if f == "package-files.json":
            continue
        full_path = os.path.join(root, f)
        rel_path = os.path.relpath(full_path, dist_dir).replace("\\", "/")
        
        hasher = hashlib.sha256()
        with open(full_path, "rb") as fp:
            while chunk := fp.read(1024 * 1024):
                hasher.update(chunk)
        sha256 = hasher.hexdigest()
        
        files_list.append({
            "path": rel_path,
            "sha256": sha256
        })

# Sort for deterministic manifest
files_list.sort(key=lambda x: x["path"])

manifest = {
    "version": version,
    "files": files_list
}

out_path = os.path.join(dist_dir, "package-files.json")
with open(out_path, "w", encoding="utf-8") as fp:
    json.dump(manifest, fp, indent=2)

print(f"Generated {out_path} with {len(files_list)} files.")
