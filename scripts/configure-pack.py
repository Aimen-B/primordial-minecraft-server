import json,pathlib,shutil,os,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[1]
SERVER=ROOT/'server'
def write(path,data):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(data,indent=2),encoding='utf8')
g=SERVER/'config/yigd.json';d=json.loads(g.read_text())
d['graveConfig']['graveRobbing']['enabled']=False
d['graveConfig']['tryGenerateOnGround']=True
d['graveConfig']['generateOnLastGroundPos']=True
d['graveConfig']['unlockable']=False
d['extraFeatures']['graveCompass']['receiveOnRespawn']=True
d['expConfig']['dropBehaviour']='PERCENTAGE';d['expConfig']['dropPercentage']=100
write(g,d)
w=SERVER/'config/waystones-common.toml';w.write_text(w.read_text().replace('enableCosts = true','enableCosts = false'))
b=SERVER/'config/bettercombat/server.json5';s=b.read_text()
for key in ['minecraft:player','player_relation_to_self_and_pets','player_relation_to_teammates']:
    s=s.replace('"'+key+'": "NEUTRAL"','"'+key+'": "FRIENDLY"')
b.write_text(s)
pack=SERVER/'world/datapacks/primordial_compat'
write(pack/'pack.mcmeta',{'pack':{'pack_format':48,'description':'Primordial Adventures: optional recipe and advancement fixes'}})
for recipe,modid in [('mythicmetals_compat/adamantite/adamantite_twinblade','mythicmetals'),('eldritch_end/dreadtide','eldritch_end')]:
    z=zipfile.ZipFile(next((SERVER/'mods').glob('simplyswords-*.jar')))
    name='data/simplyswords/recipe/'+recipe+'.json'; data=json.loads(z.read(name))
    data['neoforge:conditions']=[{'type':'neoforge:mod_loaded','modid':modid}];write(pack/name,data)
z=zipfile.ZipFile(next((SERVER/'mods').glob('*Dungeons*.jar')))
for adv in ['find_fishing_hut','find_thornborn_towers']:
    name='data/dungeons_arise/advancement/'+adv+'.json';data=json.loads(z.read(name))
    data['parent']='minecraft:adventure/root'
    icon=data['display']['icon']
    if 'item' in icon:icon['id']=icon.pop('item')
    write(pack/name,data)
# FTB Quests v13: localized text is stored separately from quest objects.
folder=SERVER/'config/ftbquests/quests'
write(folder/'data.snbt',{'version':13,'default_consume_items':False,'default_reward_team':False,'default_quest_shape':'circle','fallback_locale':'en_us'})
write(folder/'chapter_groups.snbt',{'chapter_groups':[]})
chapters=[
 ('Settle in','minecraft:crafting_table',[
  ('Welcome to Primordial','This guide is optional. Open your inventory and click the quest book in the top-left sidebar. Each page has a manual checkmark: tick it when you feel ready. Nothing here locks crafting or exploration. Mix any weapons and spells you enjoy.'),
  ('Make a shared home','Gather wood, make tools, and choose a place to meet. Place a bed and sleep at night. Keep food and spare tools at home. PvP is off, but avoid testing spells near friends until you know their effects.'),
  ('Food before adventure','Bring cooked food, torches, blocks, a pickaxe and a shield. JEI is the item list beside your inventory: search an item and press R over it for its recipe. Keep your first trip close to home.')]),
 ('Your first magic','minecraft:enchanted_book',[
  ('Find your first spellbook','Search spell book in JEI and inspect the available recipes. Iron\'s Spells uses spellbooks, scrolls and mana. Start with an accessible book, then collect scrolls and magic resources while exploring. You do not need to choose a permanent class.'),
  ('Prepare a spell','Read the tooltip on a scroll and check whether your book can accept it. Search inscription table in JEI to see the recipe and uses. Use the mod\'s progression guide at iron.wiki/progression if you want more detail.'),
  ('Practise safely','Open Options > Controls and search spell. Check your spell selection and casting keys. Practise a simple spell outside your home, away from friends, pets and valuable builds. Watch mana and cooldowns. Terrain damage from spells is disabled.')]),
 ('Find your fighting style','minecraft:iron_sword',[
  ('Try light and heavy weapons','Search @simplyswords in JEI. Compare attack speed, damage and reach. Try an accessible fast weapon and a slower heavy weapon before choosing a favourite. Rare unique weapons are longer-term exploration goals.'),
  ('Mix melee and magic','Better Combat gives supported weapons different attack sequences. Some weapons support dual wielding, while others are two-handed. Read tooltips and try your preferred combination. You can always change your equipment.'),
  ('Learn to roll','Open Options > Controls and search roll. Practise moving sideways and rolling in a safe clearing. Rolling needs food and has a cooldown; it does not grant invulnerability in this pack. Avoid rolling toward cliffs.')]),
 ('Your first expedition','minecraft:compass',[
  ('Discover a waystone','Find or craft a waystone and activate it. You must discover destinations before travelling to them. Travel costs no XP; item cooldowns still apply. Name your home waystone clearly so the group can regroup.'),
  ('Choose a small adventure','Scout a modest structure first. Large towers, ships and fortresses can be much harder than ordinary survival even on Normal. Retreat if needed, mark the location, and return with better equipment. Never feel obliged to clear everything now.'),
  ('Everyone gets a turn','Open supported Lootr containers yourself: their loot is personal, so each friend should check the chest. Ordinary storage chests are still shared. Agree on a meeting point before splitting up.')]),
 ('Recover and choose what is next','minecraft:recovery_compass',[
  ('Know your grave','After death, your equipment is stored in a protected grave. Keep the location message and use the recovery compass supplied on respawn. Right-click your own grave to retrieve your gear. Grave theft and timed expiry are disabled. Do not die deliberately to complete this page.'),
  ('Plan a recovery trip','If a grave is in danger, ask a friend to escort you and take spare supplies. Graves prefer your last grounded position. Use /yigd to inspect your grave information. If a grave is inaccessible, ask the host for recovery help rather than repeatedly dying.'),
  ('Pick the next adventure','Choose together: improve a spellbook, hunt for a unique weapon, explore another structure, or improve your base. No quest rewards grant powerful equipment. Tell the host which parts felt fun, confusing or too difficult before adding more mods.')])]
translations={};guide=['# Primordial Adventures: optional guide','\nThese are manual learning checklists, with no locked progression or rewards.\n']
for ci,(title,icon,quests) in enumerate(chapters,1):
    cid=f'{0x1000000000000000+ci:016X}'; qlist=[]
    translations[f'chapter.{cid}.title']=title
    guide.append('## '+title)
    for qi,(qt,body) in enumerate(quests,1):
        qid=f'{0x2000000000000000+ci*256+qi:016X}';tid=f'{0x3000000000000000+ci*256+qi:016X}'
        qlist.append({'id':qid,'x':float((qi-1)*3),'y':0.0,'icon':{'id':icon},'tasks':[{'id':tid,'type':'checkmark'}]})
        translations[f'quest.{qid}.title']=qt
        translations[f'quest.{qid}.quest_desc']=[body]
        guide.extend(['### '+qt,body,''])
    write(folder/'chapters'/f'{ci:02d}_guide.snbt',{'id':cid,'order_index':ci,'icon':{'id':icon},'quests':qlist})
write(folder/'lang/en_us.snbt',translations)
(ROOT/'BEGINNER-GUIDE.md').write_text('\n'.join(guide),encoding='utf8')
# A distinct launcher instance uses the existing launcher caches, not its worlds or accounts.
inst=pathlib.Path(os.environ['APPDATA'])/'ElyPrismLauncher/instances/Primordial-Adventures'
mc=inst/'.minecraft';(mc/'mods').mkdir(parents=True,exist_ok=True)
(inst/'mmc-pack.json').write_text(json.dumps({'formatVersion':1,'components':[{'uid':'net.minecraft','version':'1.21.1','important':True},{'uid':'net.neoforged','version':'21.1.252'}]},indent=2))
(inst/'instance.cfg').write_text('[General]\nConfigVersion=1.3\nInstanceType=OneSix\nname=Primordial Adventures - Magic and Weapons\niconKey=default\nOverrideJavaLocation=true\nJavaPath=D:/minecraft/runtime/jdk-21.0.12.1+1/bin/javaw.exe\nOverrideMemory=true\nMinMemAlloc=512\nMaxMemAlloc=4096\n')
(mc/'options.txt').write_text('maxFps:60\nrenderDistance:8\nsimulationDistance:6\nautoJump:false\n')
lock=json.loads((ROOT/'pack/mods.lock.json').read_text())
for r in lock['mods']:
    if r['client']!='unsupported':shutil.copy2(ROOT/'downloads'/r['filename'],mc/'mods'/r['filename'])
shutil.copytree(SERVER/'config',mc/'config',dirs_exist_ok=True)
shutil.copytree(SERVER/'config',ROOT/'pack/config',dirs_exist_ok=True)
print('Configured protected graves, co-op melee, free waystone travel, 15 guide pages and client instance')
