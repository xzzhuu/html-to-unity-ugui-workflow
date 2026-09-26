"""Validate shipped workspaces, documentation links and generic package boundaries."""
import json
from pathlib import Path
import re
import sys
from urllib.parse import unquote
sys.dont_write_bytecode = True
root = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(root / 'Skills/ui-panel-design-to-html/scripts'))
from validate_directory import validate
examples = sorted(p for p in (root / 'Examples').iterdir() if p.is_dir())
results = [validate(base) for base in examples]
package = root / 'Packages/com.nexvr.html-to-ugui'
manifest = json.loads((package / 'package.json').read_text(encoding='utf-8'))
assert manifest['name'] == 'com.nexvr.html-to-ugui'
assert manifest['dependencies']['com.unity.textmeshpro'] == '3.0.7'
assert not list((root / 'Examples').rglob('*.cs')), 'C# leaked into HTML examples'
assert not list((root / 'Skills').rglob('*.cs')), 'C# leaked into design Skill'
assert not any('TowerCrane' in p.parts or p.name == 'InventoryDemo.cs' for p in package.rglob('*.cs')), 'Business logic in generic package'
for source in list(package.rglob('*.cs')) + list(package.rglob('*.asmdef')):
    assert source.with_suffix(source.suffix + '.meta').is_file(), source
docs = list(root.glob('*.md'))
for folder in ('Examples', 'Skills', 'Packages', 'docs'):
    docs.extend((root / folder).rglob('*.md'))
docs += [root / 'Audit/README.md', root / 'Audit/Reports/OPEN_SOURCE_READINESS.md', root / 'UnityProject/README.md']
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
