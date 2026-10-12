from pathlib import Path
from PIL import Image, ImageChops
import zipfile, hashlib, shutil
root=Path.cwd()
dest=root/'Assets/HorrorPrototype/KuntilanakMerah'
(dest/'Models').mkdir(parents=True,exist_ok=True)
(dest/'Textures').mkdir(exist_ok=True)
(dest/'Materials').mkdir(exist_ok=True)
(dest/'Prefabs').mkdir(exist_ok=True)
source=Path('C:/Users/Naufal Ahmad A/Downloads/Kuntilanak-merah')
with zipfile.ZipFile(source/'source/kuntilanak_merah.zip') as z:
    name=next(n for n in z.namelist() if n.lower().endswith('.fbx'))
    data=z.read(name)
    (dest/'Models/KuntilanakMerah.fbx').write_bytes(data)
    assert hashlib.sha256(data).hexdigest()=='3fd1434140ac25041a576ce35563e5bc42ae2a42b1897eb7e355e3843cbf4943'
textures={
'KuntilanakMerah_BaseColor.png':'texture_0.png',
'KuntilanakMerah_Normal.png':'KuntilanakMerah_biped_texture_0_normal.png',
'KuntilanakMerah_Roughness.png':'KuntilanakMerah_biped_texture_0_roughness.png',
'KuntilanakMerah_Metallic.png':'KuntilanakMerah_biped_texture_0_metallic.png'}
for target,old in textures.items():
    shutil.copy2(source/'textures'/old,dest/'Textures'/target)
metal=Image.open(dest/'Textures/KuntilanakMerah_Metallic.png').convert('L')
rough=Image.open(dest/'Textures/KuntilanakMerah_Roughness.png').convert('L')
neutral=Image.new('L',metal.size,255)
mask=Image.merge('RGBA',(metal,neutral,neutral,ImageChops.invert(rough)))
mask.save(dest/'Textures/KuntilanakMerah_MaskMap.png')
assert mask.getchannel('R').tobytes()==metal.tobytes()
assert mask.getchannel('A').tobytes()==ImageChops.invert(rough).tobytes()
print('Original FBX/textures copied; HDRP mask channels verified.')