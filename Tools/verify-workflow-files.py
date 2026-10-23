"""Validate shipped workspaces, documentation links and generic package boundaries."""
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET
from urllib.parse import unquote
sys.dont_write_bytecode = True
root = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(root / 'Skills/ui-panel-design-to-html/scripts'))
from validate_directory import validate
assert not (root/'Examples').exists(), 'Retired Example sources must not be restored'
results = []
package = root / 'Packages/com.nex.html-to-ugui'
manifest = json.loads((package / 'package.json').read_text(encoding='utf-8'))
assert manifest['name'] == 'com.nex.html-to-ugui'
assert manifest['dependencies']['com.unity.textmeshpro'] == '3.0.7'
assert not list((root / 'Skills').rglob('*.cs')), 'C# leaked into design Skill'
assert not any(p.name == 'InventoryDemo.cs' for p in package.rglob('*.cs')), 'Business logic in generic package'
for source in list(package.rglob('*.cs')) + list(package.rglob('*.asmdef')):
    assert source.with_suffix(source.suffix + '.meta').is_file(), source

shipped = {'StarportHomePanel', 'ExpeditionSelectPanel', 'SupplyInventoryPanel', 'GameSettingsPanel', 'ActionConfirmPanel', 'ExpeditionResultPanel'}
starport = root/'Workspaces/StarportSalvage'
assert shipped <= {p.name for p in (starport/'Panels').iterdir() if p.is_dir()}, 'Missing shipped Starport panel'
for state_file in sorted((root/'Workspaces').rglob('.html-directory.json')):
    panel = state_file.parent
    game = panel.parent.parent
    assert panel.parent.name == 'Panels' and game.parent == root/'Workspaces', 'Panel source must live at Workspaces/{GameName}/Panels/{PanelName}: '+str(panel)
    assert (game/'project.assets.json').is_file(), 'Game root is missing project.assets.json: '+str(game)
    state = json.loads(state_file.read_text(encoding='utf-8'))
    assert state['panelName'] == panel.name, 'Panel directory must match panelName: '+str(panel)
    if state.get('mode','full') == 'full':
        source = panel/'html/panel.package.json'
        settings = json.loads(source.read_text(encoding='utf-8'))
        assert settings['projectName'] == game.name
        assert settings['projectDirectory'] == '../../..'
        assert (source.parent/settings['projectDirectory']).resolve() == game
        assert settings['stagingRoot'] == f'Assets/HtmlToUGUIStage/{game.name}/{panel.name}'
    results.append(validate(panel, require_ready=game == starport and panel.name in shipped))
assert not list(starport.glob('*/html/panel.package.json')), 'A panel still lives at the game root'
link = json.loads((root / 'unity-package-link.json').read_text(encoding='utf-8'))
assert link['packageName'] == manifest['name'] and (root / link['packagePath']).resolve() == package
unity_manifest = json.loads((root / 'UnityProject/Packages/manifest.json').read_text(encoding='utf-8'))
dependency = unity_manifest['dependencies'][manifest['name']]
assert dependency.startswith('file:')
assert (root / 'UnityProject/Packages' / dependency[5:]).resolve() == package
docs = list(root.glob('*.md'))
for folder in ('Skills', 'Packages', 'docs', 'Tools', 'Workspaces'):
    docs.extend((root / folder).rglob('*.md'))
docs += [root / 'Audit/README.md', root / 'UnityProject/README.md']
docs.extend((root / 'Audit/Reports').rglob('*.md'))
docs.extend((root / 'UnityProject/Assets/docs').rglob('*.md'))
links = []
for path in docs:
    text = path.read_text(encoding='utf-8')
    targets = re.findall(r'!?\[[^\]]*\]\(([^)]+)\)', text)
    targets += re.findall(r'<img\b[^>]*\bsrc=["\x27]([^"\x27]+)', text)
    for target in targets:
        target = unquote(target.strip('<>')).split('#')[0]
        if not target or re.match(r'\w+://|app:|mailto:', target):
            continue
        resolved = (path.parent / target).resolve()
        assert resolved.exists(), (path.relative_to(root), target)
        links.append(target)
report = {'workspaces': results, 'verifiedLocalLinks': len(links), 'package': manifest['name'], 'unityRuntimeChecks': 'not performed by this script'}
destination = root / 'Audit/Runs/latest'
destination.mkdir(parents=True, exist_ok=True)
(destination / 'workflow-file-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'workspaces': len(results), 'links': len(links), 'package': manifest['name']}))
