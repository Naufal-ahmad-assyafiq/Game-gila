import array
import hashlib
import io
import json
import struct
import zipfile
import zlib
from collections import Counter
from pathlib import Path
from PIL import Image, ImageChops, ImageStat

root = Path(__file__).resolve().parent
source = Path('C:/Users/Naufal Ahmad A/Downloads/Kuntilanak-merah')
archive = zipfile.ZipFile(source / 'source/kuntilanak_merah.zip')
fbx_path = next(n for n in archive.namelist() if n.lower().endswith('.fbx'))
data = archive.read(fbx_path)
version = struct.unpack_from('<I', data, 23)[0]
stream = io.BytesIO(data)
stream.seek(27)

def read(fmt):
    return struct.unpack('<' + fmt, stream.read(struct.calcsize('<' + fmt)))[0]

def prop():
    t = stream.read(1).decode()
    formats = {'Y': 'h', 'C': '?', 'I': 'i', 'L': 'q', 'F': 'f', 'D': 'd'}
    if t in formats:
        return read(formats[t])
    if t in ('S', 'R'):
        raw = stream.read(read('I'))
        return raw.decode('utf-8', errors='replace') if t == 'S' else {'bytes': len(raw)}
    if t in ('f', 'd', 'l', 'i', 'b', 'c'):
        count, encoding, length = read('I'), read('I'), read('I')
        raw = stream.read(length)
        if encoding:
            raw = zlib.decompress(raw)
        result = array.array({'f':'f', 'd':'d', 'l':'q', 'i':'i', 'b':'B', 'c':'B'}[t])
        result.frombytes(raw)
        assert len(result) == count
        return result
    raise ValueError('Unsupported property type ' + t)

def node():
    fmt = 'Q' if version >= 7500 else 'I'
    end, count, length, name_length = read(fmt), read(fmt), read(fmt), read('B')
    if end == 0:
        return None
    name = stream.read(name_length).decode('utf-8', errors='replace')
    properties = [prop() for _ in range(count)]
    children = []
    while stream.tell() < end:
        child = node()
        if child is None:
            break
        children.append(child)
    stream.seek(end)
    return {'name': name, 'props': properties, 'children': children}

nodes = []
while True:
    n = node()
    if n is None:
        break
    nodes.append(n)

def child(n, name):
    return next((c for c in n['children'] if c['name'] == name), None)

def clean(v):
    if isinstance(v, array.array):
        return {'count': len(v)}
    if isinstance(v, str):
        return v.replace('\x00\x01', '::')
    return v

objects = next(n for n in nodes if n['name'] == 'Objects')['children']
report = {'fbx_version': version, 'fbx_file': fbx_path, 'fbx_sha256': hashlib.sha256(data).hexdigest(),
          'object_types': dict(Counter(n['name'] for n in objects))}
report['meshes'] = []
for n in objects:
    if n['name'] != 'Geometry' or n['props'][-1] != 'Mesh':
        continue
    verts = child(n, 'Vertices')['props'][0]
    indices = child(n, 'PolygonVertexIndex')['props'][0]
    polygons, triangles, size = 0, 0, 0
    for idx in indices:
        size += 1
        if idx < 0:
            polygons += 1
            triangles += max(0, size - 2)
            size = 0
    report['meshes'].append({'name': clean(n['props'][1]), 'control_points': len(verts)//3,
        'polygons': polygons, 'triangles': triangles,
        'uv_layers': sum(c['name'] == 'LayerElementUV' for c in n['children']),
        'has_normals': child(n, 'LayerElementNormal') is not None})
report['models'] = [{'name': clean(n['props'][1]), 'type': n['props'][-1]} for n in objects if n['name'] == 'Model']
report['deformers'] = [{'name': clean(n['props'][1]), 'type': n['props'][-1],
    'weighted_indices': len(child(n, 'Indexes')['props'][0]) if child(n, 'Indexes') else 0}
    for n in objects if n['name'] == 'Deformer']
report['animations'] = [{'type': n['name'], 'name': clean(n['props'][1])}
    for n in objects if n['name'].startswith('Animation')]
curves = [n for n in objects if n['name'] == 'AnimationCurve']
report['animation_curve_summary'] = {
    'curves': len(curves),
    'key_counts': dict(Counter(len(child(n,'KeyTime')['props'][0]) for n in curves)),
    'varying_curves': sum(max(child(n,'KeyValueFloat')['props'][0])-min(child(n,'KeyValueFloat')['props'][0]) > 0.00001 for n in curves),
    'first_key_seconds': min(min(child(n,'KeyTime')['props'][0]) for n in curves) / 46186158000,
    'last_key_seconds': max(max(child(n,'KeyTime')['props'][0]) for n in curves) / 46186158000,
}
report['materials'] = [{'name': clean(n['props'][1]),
    'properties': [[clean(p) for p in c['props']] for c in (child(n, 'Properties70') or {'children':[]})['children']]}
    for n in objects if n['name'] == 'Material']
report['texture_references'] = [{'name': clean(n['props'][1]),
    'filenames': [c['props'] for c in n['children'] if c['name'] in ('FileName','RelativeFilename','Filename')]}
    for n in objects if n['name'] in ('Texture', 'Video')]
settings = next(n for n in nodes if n['name'] == 'GlobalSettings')
report['global_settings'] = [c['props'] for c in child(settings,'Properties70')['children']]
report['textures'] = []
for path in sorted((source/'textures').glob('*.png')):
    im = Image.open(path)
    item = {'name': path.name, 'size': im.size, 'mode': im.mode, 'range': im.getextrema(),
            'channel_means': ImageStat.Stat(im).mean, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
    suffix = 'KuntilanakMerah_biped_texture_0.png' if path.name == 'texture_0.png' else path.name
    matching = next((n for n in archive.namelist() if n.endswith('/'+suffix)), None)
    if matching:
        internal = Image.open(io.BytesIO(archive.read(matching)))
        diff = ImageChops.difference(im.convert('RGBA'), internal.convert('RGBA'))
        item['archive_version'] = {'size': internal.size, 'mode': internal.mode,
            'same_decoded_pixels': not any(c.getbbox() for c in diff.split()),
            'max_channel_difference': diff.getextrema()}
    report['textures'].append(item)
(root/'source-analysis.json').write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding='utf-8')
print(json.dumps({k: report[k] for k in ('fbx_version','meshes','object_types','animation_curve_summary')}, indent=2))
