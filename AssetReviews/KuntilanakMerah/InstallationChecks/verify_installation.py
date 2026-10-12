from pathlib import Path
import hashlib,json,re,zipfile
from PIL import Image,ImageChops,ImageOps

checks=Path(__file__).resolve().parent
root=Path(__file__).resolve().parents[3]
baseline=json.loads((checks/'before-hashes.json').read_text(encoding='utf-8-sig'))
changed=[];missing=[]
for rel,digest in baseline.items():
    path=root/rel
    if not path.exists(): missing.append(rel)
    elif hashlib.sha256(path.read_bytes()).hexdigest().lower()!=digest.lower(): changed.append(rel)
allowed={'Assets/FpsHorrorKit/Scenes/MainScene.unity','Assets/HorrorPrototype/CREDITS.md','Assets/HorrorPrototype/README.md'}
assert not missing,missing
assert {p.replace('\\','/') for p in changed} <= allowed,changed
folder=root/'Assets/HorrorPrototype/KuntilanakMerah'
assert hashlib.sha256((folder/'Models/KuntilanakMerah.fbx').read_bytes()).hexdigest()=='3fd1434140ac25041a576ce35563e5bc42ae2a42b1897eb7e355e3843cbf4943'
mask=Image.open(folder/'Textures/KuntilanakMerah_MaskMap.png')
metal=Image.open(folder/'Textures/KuntilanakMerah_Metallic.png').convert('L')
rough=Image.open(folder/'Textures/KuntilanakMerah_Roughness.png').convert('L')
assert ImageChops.difference(mask.getchannel('R'),metal).getbbox() is None
assert ImageChops.difference(mask.getchannel('A'),ImageOps.invert(rough)).getbbox() is None
assert mask.getchannel('G').getextrema()==(255,255) and mask.getchannel('B').getextrema()==(255,255)
metas={}
for p in (root/'Assets').rglob('*.meta'):
    match=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig',errors='replace'),re.M)
    if match: metas[match[1]]=p
for p in list(folder.rglob('*.prefab'))+list(folder.rglob('*.mat')):
    for guid in re.findall(r'guid: (\w+)',p.read_text()):
        # Built-in objects and installed HDRP shader live outside Assets.
        if guid=='0000000000000000f000000000000000':continue
        if guid not in metas:
            hdrp=root/'Library/PackageCache/com.unity.render-pipelines.high-definition@8310d0e96a2e'
            package_guids={re.search(r'^guid: (\w+)',(hdrp/rel).read_text(),re.M)[1] for rel in
                           ['Runtime/Material/Lit/Lit.shader.meta','Editor/AssetProcessors/AssetVersion.cs.meta']}
            assert p.suffix=='.mat' and guid in package_guids, (p,guid)
for log,marker in [('install-build.log','KUNTILANAK_BUILD_OK'),('playmode-hdrp.log','KUNTILANAK_PLAYMODE_OK'),('playmode-hdrp.log','KUNTILANAK_RENDER_OK')]:
    assert marker in (checks/log).read_text(errors='replace'),marker
result={'changed_existing_project_files':changed,'missing_files':missing,'unchanged_existing_files':len(baseline)-len(changed),
        'source_fbx_unchanged':True,'mask_channels_verified':True,'backup':str(root/'Backups/MainScene_before_KuntilanakMerah_2026-10-08.unity'),
        'scope':'Native Unity 6000.0.43f1 isolated HDRP fixture uses original Single_Door prefab, exact stage objects and installed ghost prefab. Calls interaction methods after a physics raycast; does not drive keyboard E or run the complete MainScene environment.'}
(checks/'verification.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
