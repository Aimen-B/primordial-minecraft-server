"""Build a pinned local candidate from official Modrinth metadata and CurseForge files."""
import json, urllib.request, urllib.parse, hashlib, pathlib, zipfile, shutil
ROOT=pathlib.Path(__file__).resolve().parents[1]
BASE=ROOT.parent
HEAD={'User-Agent':'PrimordialLocalPack/0.1 (local Minecraft pack setup)'}
def fetch(url):
    with urllib.request.urlopen(urllib.request.Request(url,headers=HEAD),timeout=90) as r:return r.read()
def api(path):return json.loads(fetch('https://api.modrinth.com/v2/'+path))
records={}
def mod(slug,vid=None):
    p=api('project/'+slug)
    if p['id'] in records:return
    if p['id']=='gedNE4y2':vid='HJZB6bmA'  # Required animation library is published as beta.
    if vid:v=api('version/'+vid)
    else:
        vs=api('project/'+p['id']+'/version?'+urllib.parse.urlencode({'loaders':'["neoforge"]','game_versions':'["1.21.1"]'}))
        vs=[v for v in vs if v['version_type']=='release']
        if not vs:raise RuntimeError('No stable NeoForge 1.21.1 file: '+slug)
        v=vs[0]
    f=next((f for f in v['files'] if f['primary']),v['files'][0])
    path=ROOT/'downloads'/f['filename']
    content=path.read_bytes() if path.exists() else fetch(f['url'])
    for algorithm,digest in f['hashes'].items():assert hashlib.new(algorithm,content).hexdigest()==digest
    assert zipfile.is_zipfile(__import__('io').BytesIO(content))
    path.write_bytes(content)
    records[p['id']]={'name':p['title'],'slug':p['slug'],'project_id':p['id'],'version_id':v['id'],'version':v['version_number'],'filename':f['filename'],'url':f['url'],'sha256':hashlib.sha256(content).hexdigest(),'client':p['client_side'],'server':p['server_side'],'dependencies':v['dependencies'],'source':'modrinth'}
    print(p['title'],v['version_number'],flush=True)
    for d in v['dependencies']:
        if d['dependency_type']=='required':
            if d['project_id']:mod(d['project_id'],d.get('version_id'))
            elif d.get('version_id'):
                dep=api('version/'+d['version_id']);mod(dep['project_id'],dep['id'])
            else:raise RuntimeError('Unresolved dependency '+str(d))
def curse(name,project,fileid,filename):
    url=f'https://edge.forgecdn.net/files/{fileid//1000}/{fileid%1000}/{urllib.parse.quote(filename)}'
    path=ROOT/'downloads'/filename
    data=path.read_bytes() if path.exists() else fetch(url)
    assert zipfile.is_zipfile(__import__('io').BytesIO(data))
    path.write_bytes(data)
    records['cf-'+str(project)]={'name':name,'source':'curseforge','project_id':project,'file_id':fileid,'filename':filename,'url':url,'sha256':hashlib.sha256(data).hexdigest(),'client':'required','server':'required'}
    print(name,filename,flush=True)
if __name__=='__main__':
    for slug in ['simply-swords','better-combat','combat-roll','when-dungeons-arise','lootr','waystones','yigd','geckolib','curios','playeranimator','irons-lib','jei','ferrite-core','modernfix','sodium']:
        mod(slug)
    curse("Iron's Spells 'n Spellbooks",855414,8680204,'irons_spellbooks-1.21.1-3.16.3.jar')
    curse('FTB Quests',289412,8885017,'ftb-quests-neoforge-2101.1.36.jar')
    curse('FTB Library',404465,9008089,'ftb-library-neoforge-2101.1.37.jar')
    curse('FTB Teams',404468,8724782,'ftb-teams-neoforge-2101.1.11.jar')
    (ROOT/'pack/mods.lock.json').write_text(json.dumps({'minecraft':'1.21.1','neoforge':'21.1.252','mods':list(records.values())},indent=2))
    for r in records.values():
        if r['server']!='unsupported':shutil.copy2(ROOT/'downloads'/r['filename'],ROOT/'server/mods'/r['filename'])
