"""Prepare deterministic local integration resources. Does not deploy."""
from pathlib import Path
import json
import shutil

root = Path(__file__).resolve().parents[1]
catalog=json.loads((root/'pack/skins/catalog.json').read_text(encoding='utf-8-sig'))
ids=[skin['id'] for skin in catalog['skins']]
source=root/'bridge-src/main/com/primordial/bridge/SkinStore.java'
text=source.read_text()
start=text.index('Set.of(')
end=text.index(');',start)
text=text[:start]+'Set.of('+','.join(json.dumps(x) for x in ['default']+ids)+text[end:]
source.write_text(text)
target=root/'bridge-src/resources/assets/primordial_bridge/textures/skins'
target.mkdir(parents=True,exist_ok=True)
for skin in catalog['skins']:
    shutil.copy2(root/'pack/skins'/Path(skin['texture']).name,target/(skin['id']+'.png'))
print(f'Prepared {len(ids)} bundled multiplayer skins.')
