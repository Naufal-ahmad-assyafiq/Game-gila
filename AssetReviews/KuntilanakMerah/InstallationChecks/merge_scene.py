"""Transplant only the native-authored ghost changes; retain unrelated scene bytes."""
from pathlib import Path
import re, json, hashlib, shutil

root = Path(__file__).resolve().parents[3]
checks = Path(__file__).resolve().parent
backup = root / 'Backups/MainScene_before_KuntilanakMerah_2026-10-08.unity'
main = root / 'Assets/FpsHorrorKit/Scenes/MainScene.unity'
fixture = checks / 'UnityProject/Assets/FpsHorrorKit/Scenes/MainScene.unity'

def split(data):
    sections = re.split(rb'(?=^--- !u!)', data, flags=re.M)
    blocks = {}
    for item in sections[1:]:
        ident = int(re.match(rb'--- !u!\d+ &(\d+)', item)[1])
        assert ident not in blocks
        blocks[ident] = item
    return sections[0], blocks

before = backup.read_bytes()
assert main.read_bytes() == before, 'MainScene changed since backup: inspect before merging'
header, original = split(before)
_, authored = split(fixture.read_bytes())
holder = 9100000000000018
holder_go = 9100000000000017
ghost = 9100000000000032
descendants = set()
while True:
    newly = {i for i,b in original.items()
             if re.search(rb'^  m_Father: \{fileID: (\d+)\}', b, re.M)
             and int(re.search(rb'^  m_Father: \{fileID: (\d+)\}', b, re.M)[1]) in ({holder}|descendants)}
    if newly <= descendants: break
    descendants |= newly
gos = {int(re.search(rb'^  m_GameObject: \{fileID: (\d+)\}',original[i],re.M)[1]) for i in descendants}
removed = descendants | gos | {i for i,b in original.items() if
    (m:=re.search(rb'^  m_GameObject: \{fileID: (\d+)\}',b,re.M)) and int(m[1]) in gos}
assert len(gos)==7 and len(removed)==28, (len(gos),len(removed))
assert removed.isdisjoint(authored), 'Old ghost primitives still present'
guid = re.search(r'guid: (\w+)',(checks/'UnityProject/Assets/HorrorPrototype/KuntilanakMerah/Prefabs/KuntilanakMerah_Visual.prefab.meta').read_text())[1]
instances = {i for i,b in authored.items() if (f'm_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}'.encode()) in b}
assert len(instances)==1
native_new = instances | {i for i,b in authored.items() if any(f'm_PrefabInstance: {{fileID: {inst}}}'.encode() in b for inst in instances)}
assert len(native_new)==2, native_new
mapping = {i:9200000000000000+n for n,i in enumerate(sorted(native_new))}
assert set(mapping.values()).isdisjoint(original)
def remap(b):
    for src,dst in mapping.items():
        b=re.sub(rb'(?<!\d)'+str(src).encode()+rb'(?!\d)',str(dst).encode(),b)
    return b
changes={holder,holder_go,ghost}
assert all(i in authored for i in changes)
final = {i:remap(authored[i]) if i in changes else b for i,b in original.items() if i not in removed}
final.update({mapping[i]:remap(authored[i]) for i in native_new})
assert b'm_Name: KuntilanakMerah_Model' in final[holder_go]
assert b'verticalOffset: -1.56' in final[ghost]
assert final[1149439012087369111] == original[1149439012087369111], 'Door overrides changed'
unchanged=set(original)-removed-changes
assert all(final[i]==original[i] for i in unchanged)
output = header+b''.join(final.values())
main.write_bytes(output)
# Use the exact same transplanted blocks and IDs in the isolated native fixture for testing.
fh,fb=split(fixture.read_bytes())
for i in native_new: del fb[i]
for i in changes: fb[i]=final[i]
for i in mapping.values(): fb[i]=final[i]
fixture.write_bytes(fh+b''.join(fb.values()))
source=checks/'UnityProject/Assets/HorrorPrototype/KuntilanakMerah'
target=root/'Assets/HorrorPrototype/KuntilanakMerah'
shutil.copytree(source,target,dirs_exist_ok=True)
root_meta=source.with_suffix('.meta')
if root_meta.exists(): shutil.copy2(root_meta,target.with_suffix('.meta'))
report={'backup_sha256':hashlib.sha256(before).hexdigest(),'new_scene_sha256':hashlib.sha256(output).hexdigest(),
        'unchanged_scene_blocks':len(unchanged),'changed_blocks':sorted(changes),'removed_primitive_blocks':sorted(removed),
        'new_prefab_blocks':sorted(mapping.values()),'prefab_guid':guid,'door_instance_byte_identical':True}
(checks/'scene-merge-verification.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
