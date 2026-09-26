"""Check installer pruning and user-file preservation in an isolated target."""
import importlib.util
import json
from pathlib import Path
import sys
import tempfile
sys.dont_write_bytecode=True
root=Path(__file__).resolve().parents[1]
(root/'Audit/Runs/latest').mkdir(parents=True,exist_ok=True)
spec=importlib.util.spec_from_file_location('installer',root/'Tools/install-global-skills.py')
installer=importlib.util.module_from_spec(spec)
spec.loader.exec_module(installer)
with tempfile.TemporaryDirectory(prefix='skill-install-') as directory:
    source=Path(directory)/'source';source.mkdir()
    destination=Path(directory)/'installed'
    (source/'SKILL.md').write_text('fixture')
    (source/'obsolete.txt').write_text('old owned resource')
    (source/'__pycache__').mkdir()
    (source/'__pycache__/module.pyc').write_bytes(b'cache')
    installer.install(source,destination)
    assert not (destination/'__pycache__').exists()
    (destination/'user-note.txt').write_text('keep')
    (source/'obsolete.txt').unlink()
    installer.install(source,destination)
    assert not (destination/'obsolete.txt').exists() and (destination/'user-note.txt').read_text()=='keep'
    assert installer.install(source,destination,check=True)['passed']
report={'passed':3,'checks':['Caches not installed','Retired managed file pruned; unknown user note retained','Installed owned files match source']}
(root/'Audit/Runs/latest/installer-regression.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
