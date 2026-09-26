"""Check maintained JSON, Python and PNG files without local installations."""
import ast
import hashlib
import json
from pathlib import Path
from PIL import Image
root = Path(__file__).resolve().parents[1]
files = list(root.glob('*.json')) + list(root.glob('*.md'))
for folder in ('Tools', 'Skills', 'Examples', 'Packages', 'docs'):
    files.extend(p for p in (root / folder).rglob('*') if p.is_file() and '__pycache__' not in p.parts and p.suffix not in ('.pyc', '.pyo'))
counts = {'json': 0, 'python': 0, 'png': 0}
inventory = []
for path in sorted(files):
    if path.suffix == '.json':
        json.loads(path.read_text(encoding='utf-8-sig'))
        counts['json'] += 1
    elif path.suffix == '.py':
        ast.parse(path.read_text(encoding='utf-8-sig'))
        counts['python'] += 1
    elif path.suffix == '.png':
        with Image.open(path) as picture:
            picture.verify()
        counts['png'] += 1
    inventory.append({'path': path.relative_to(root).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(), 'bytes': path.stat().st_size})
destination = root / 'Audit/Runs/latest'
destination.mkdir(parents=True, exist_ok=True)
(destination / 'file-integrity.json').write_text(json.dumps({'checkedFiles': len(files), 'formatChecks': counts, 'files': inventory}, indent=2), encoding='utf-8')
print(json.dumps({'files': len(files), 'formatChecks': counts}))
