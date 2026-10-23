"""Verify the maintained Starport example against the actual Unity project."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

sys.dont_write_bytecode = True
root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--unity-project', type=Path, default=root/'UnityProject')
args = parser.parse_args()
project = args.unity_project.resolve()
sys.path.insert(0, str(root/'Skills/ui-panel-design-to-html/scripts'))
from validate_directory import validate

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def guid(path):
    return re.search(r'^guid: (\w+)', path.read_text(encoding='utf-8'), re.M)[1]

game = root/'Workspaces/StarportSalvage'
delivery = json.loads((game/'unity-delivery.json').read_text(encoding='utf-8'))
assert delivery['projectName'] == 'StarportSalvage'
assert delivery['font'] == 'Assets/Art/Fonts/PICOSansVFESC SDF.asset'
font = project/delivery['font']
assert font.is_file() and guid(Path(str(font)+'.meta')) == delivery['fontGuid']
settings = [p for p in (project/'Assets').rglob('TMP Settings.asset') if p.parent.name == 'Resources']
assert len(settings) == 1, 'Expected one Resources/TMP Settings.asset'
assert 'guid: '+delivery['fontGuid'] in re.search(r'm_defaultFontAsset: (.*)', settings[0].read_text(encoding='utf-8'))[1], 'TMP default font differs from PICOSans'
native = set()
for pattern in ('com.unity.ugui@*/Runtime', 'com.unity.textmeshpro@*/Scripts/Runtime'):
    for folder in (project/'Library/PackageCache').glob(pattern):
        native.update(guid(p) for p in folder.rglob('*.cs.meta'))
assert native, 'Resolve UGUI/TMP packages before verifying the project'
generic = {guid(p) for p in (root/'Packages/com.nex.html-to-ugui/Runtime').rglob('*.cs.meta')}
stage = project/'Assets/HtmlToUGUIStage/StarportSalvage'
stage_guids = {guid(p) for p in stage.rglob('*.meta')}
for record in delivery['assets']+delivery['stagingAssets']:
    path = project/record['path']
    meta = Path(str(path)+'.meta')
    assert path.is_file() and meta.is_file(), path
    assert digest(path) == record['sha256'] and digest(meta) == record['metaSha256'] and guid(meta) == record['guid'], 'Changed artifact: '+str(path)
    if path.suffix != '.prefab':
        continue
    text = path.read_text(encoding='utf-8')
    scripts = set(re.findall(r'm_Script: \{fileID: \d+, guid: (\w+), type: \d+\}', text))
    assert scripts <= native | generic, 'Missing or unexpected script: '+str(path)
    fonts = re.findall(r'm_fontAsset: \{fileID: \d+, guid: (\w+), type: \d+\}', text)
    assert fonts and set(fonts) == {delivery['fontGuid']}, 'Wrong TMP font: '+str(path)
    materials = re.findall(r'm_sharedMaterial: \{fileID: (-?\d+), guid: (\w+), type: \d+\}', text)
    assert materials and set(materials) == {(str(delivery['fontMaterialFileId']), delivery['fontMaterialGuid'])}, 'Wrong shared material: '+str(path)
    if record in delivery['assets']:
        assert not stage_guids.intersection(re.findall(r'guid: (\w+)', text)), 'Formal prefab references staging: '+str(path)

panels = sorted(p.name for p in (game/'Panels').iterdir() if p.is_dir())
assert len(panels) == 6
expected = {f'Assets/Art/UIPrefab/StarportSalvage/{p}/{p}.prefab' for p in panels}
expected |= {f'Assets/Art/UIPrefab/StarportSalvage/Item/Item_{p}.prefab' for p in ('InventoryEntry','LoadoutEntry','MissionEntry','RewardEntry')}
actual = {p.relative_to(project).as_posix() for p in (project/'Assets/Art/UIPrefab/StarportSalvage').rglob('*.prefab')}
assert actual == expected
for panel in panels:
    validate(game/'Panels'/panel, require_ready=True)
for record in delivery['sourceImportReports']:
    assert digest(project/record['path']) == record['sha256'], 'Import report changed'
report = project/delivery['copyReport']
assert digest(report) == delivery['copyReportSha256'], 'Copy evidence changed'
assert delivery['formalSpriteRoot'] == 'Assets/Art/UI/Sprites/StarportSalvage'
sprite_root = project/delivery['formalSpriteRoot']
expected_images = {r['path'] for r in delivery['assets'] if r['path'].endswith('.png')}
assert {p.relative_to(project).as_posix() for p in sprite_root.rglob('*.png')} == expected_images
assert not (sprite_root/'Sprites').exists(), 'Repeated formal Sprites directory'
for record in delivery.get('migrationReports', []):
    assert digest(project/record['path']) == record['sha256'], 'Migration evidence changed'
assert not list((project/'Assets/Art/UIPrefab/StarportSalvage').rglob('*.import-report.json'))
assert not list((project/'Assets/Art/UIPrefab/StarportSalvage').rglob('*.mat'))
for relative in ('Examples','UnityProject/Assets/Art/UIPrefab/Example','UnityProject/Assets/HtmlToUGUIStage/Example'):
    assert not (root/relative).exists(), 'Retired Example remains: '+relative
for old in ('InventoryPanel','SettingsPanel','StarterPanel'):
    assert not (project/'Assets/HtmlToUGUIStage'/old).exists()
    assert not (project/'Assets/Art/UIPrefab'/old).exists()
print(json.dumps(dict(status='passed', panels=len(panels), items=4, formalAssets=len(delivery['assets']), stagingAssets=len(delivery['stagingAssets']), font=delivery['font'], unityProject=str(project)), ensure_ascii=False))
