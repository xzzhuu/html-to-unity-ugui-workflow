"""Read the approved game asset inventory without importing anything into Unity."""
import hashlib
import json
from pathlib import Path
import re
from PIL import Image

COMPONENTS = {'Button', 'Toggle', 'Slider', 'Scrollbar', 'Dropdown', 'TMP_Dropdown',
              'InputField', 'TMP_InputField', 'ScrollRect', 'Image', 'RawImage', 'Mask'}


def load_project_assets(directory):
    directory = Path(directory).resolve()
    data = json.loads((directory / 'project.assets.json').read_text(encoding='utf-8-sig'))
    if data.get('schemaVersion') != 1 or not isinstance(data.get('assets'), list):
        raise ValueError('Unsupported project asset manifest')
    entries, identifiers = {}, set()
    for item in data['assets']:
        key = item.get('assetId', '')
        if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*', key) or key.casefold() in identifiers:
            raise ValueError('Invalid/duplicate project assetId: ' + key)
        identifiers.add(key.casefold())
        if type(item.get('approved', False)) is not bool:
            raise ValueError('Asset approved must be a boolean: ' + key)
        if item.get('approved') is not True:
            continue
        for field in ('styleId', 'part', 'state'):
            if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*', item.get(field, 'Default' if field == 'state' else '')):
                raise ValueError('Invalid asset ' + field + ': ' + key)
        version = item.get('approvalVersion')
        if item.get('componentType') not in COMPONENTS or not isinstance(version, str) or not version.strip():
            raise ValueError('Missing component type or approval version: ' + key)
        if any(type(item.get(field, default)) is not bool for field, default in (('tintable', False), ('sRGB', True))):
            raise ValueError('Asset tintable and sRGB must be booleans: ' + key)
        relative = Path(item.get('source', ''))
        if relative.is_absolute() or '..' in relative.parts or not relative.parts:
            raise ValueError('Project asset source escapes source root: ' + key)
        source = directory / relative
        if source.is_symlink() or any(parent.is_symlink() for parent in source.parents if parent != directory.parent):
            raise ValueError('Project asset source traverses a link: ' + key)
        source = source.resolve()
        if not source.is_relative_to(directory) or source.suffix.lower() != '.png' or not source.is_file():
            raise ValueError('Missing/outside project PNG: ' + key)
        digest = hashlib.sha256(source.read_bytes()).hexdigest()
        if digest != item.get('sourceSha256'):
            raise ValueError('Approved project asset changed: ' + key)
        with Image.open(source) as image:
            width, height = image.size
            if image.format != 'PNG' or any(v <= 0 or v > 16384 or v & (v - 1) for v in (width, height)):
                raise ValueError('Project elements require POT PNG dimensions <=16384: ' + key)
            if item.get('tintable') and any(a and (r != g or g != b) for r, g, b, a in image.convert('RGBA').getdata()):
                raise ValueError('Tintable asset must use a neutral RGB mask: ' + key)
        border = item.get('border', [0, 0, 0, 0])
        pivot = item.get('pivot', [.5, .5])
        if (not isinstance(border, list) or len(border) != 4 or any(type(v) not in (int, float) or not 0 <= v < float('inf') for v in border)
                or border[0] + border[2] >= width or border[1] + border[3] >= height
                or not isinstance(pivot, list) or len(pivot) != 2 or any(type(v) not in (int, float) or not 0 <= v <= 1 for v in pivot)
                or type(item.get('pixelsPerUnit', 100)) not in (int, float) or not 0 < item.get('pixelsPerUnit', 100) < float('inf') or item.get('filter', 'Bilinear') not in ('Point', 'Bilinear')):
            raise ValueError('Invalid project import profile: ' + key)
        entries[key] = dict(item, sourcePath=source, border=border, pivot=pivot, width=width, height=height)
    styles = {}
    for entry in entries.values():
        if entry['componentType'] == 'Toggle' and entry['part'] == 'Surface':
            styles.setdefault(entry['styleId'], []).append(entry)
    for style, group in styles.items():
        if {e.get('state', 'Default') for e in group} != {'Off', 'On', 'Disabled'}:
            raise ValueError('Toggle Surface requires Off, On and Disabled: ' + style)
        if len({(e['width'], e['height'], tuple(e['border']), tuple(e['pivot']), e.get('pixelsPerUnit', 100)) for e in group}) != 1:
            raise ValueError('Toggle surfaces require identical canvas, border, pivot and PPU: ' + style)
    return entries


def find_project_directory(html_directory, package):
    if package.get('projectDirectory'):
        return (Path(html_directory) / package['projectDirectory']).resolve()
    for directory in (Path(html_directory), *Path(html_directory).parents):
        if (directory / 'project.assets.json').is_file():
            return directory
    raise ValueError('Project generation requires project.assets.json in the source root')
