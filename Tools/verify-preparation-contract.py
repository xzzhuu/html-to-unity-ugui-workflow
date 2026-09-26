"""Negative tests ensure preparation validation catches the actual failure modes."""
import importlib.util
import json
from pathlib import Path
import shutil
import sys
import tempfile
import xml.etree.ElementTree as ET
sys.dont_write_bytecode=True
root=Path(__file__).resolve().parents[1]
(root/'Audit/Runs/latest').mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(root/'Skills/ui-panel-design-to-html/scripts'))
from validate_directory import validate

passed=[]
def reject(name,mutate):
    with tempfile.TemporaryDirectory(prefix='preparation-contract-') as directory:
        target=Path(directory)/'Inventory'
        shutil.copytree(root/'Examples/Inventory',target)
        mutate(target)
        try: validate(target)
        except (ValueError,KeyError,OSError): passed.append(name)
        else: raise RuntimeError('Invalid workspace passed: '+name)

def change_manifest(target,mutate):
    path=target/'html/elements.json'
    data=json.loads(path.read_text(encoding='utf-8'))
    mutate(data)
    path.write_text(json.dumps(data,ensure_ascii=False),encoding='utf-8')

reject('Generated Input helper omission',lambda p:change_manifest(p,lambda m:m['elements'].remove(next(e for e in m['elements'] if e['generated']))))
reject('Template ownership mismatch',lambda p:change_manifest(p,lambda m:m['templates'][0].update(parentId='Wrong')))
reject('Native component mismatch',lambda p:change_manifest(p,lambda m:m['elements'][0].update(uguiComponent='Image')))
reject('Blank semantic purpose',lambda p:change_manifest(p,lambda m:m['elements'][0].update(purpose=' ')))
reject('Missing linked CSS',lambda p:(p/'html/InventoryPanel.css').unlink())
reject('Non-void HTML self-closing tag',lambda p:(p/'html/InventoryPanel.html').write_text((p/'html/InventoryPanel.html').read_text(encoding='utf-8').replace('</body>','<span /></body>'),encoding='utf-8'))
reject('Wrong Unity destination',lambda p:(p/'html/panel.package.json').write_text((p/'html/panel.package.json').read_text().replace('Assets/Art/UIPrefab/InventoryPanel','Assets/Other'),encoding='utf-8'))
with tempfile.TemporaryDirectory(prefix='sprite-contract-') as directory:
    target=Path(directory)/'TowerCrane'
    shutil.copytree(root/'Examples/TowerCrane',target)
    path=target/'html/sprites.json'
    data=json.loads(path.read_text(encoding='utf-8'))
    data['sprites'][0]['pixelSize']=[1,1]
    path.write_text(json.dumps(data),encoding='utf-8')
    try: validate(target)
    except ValueError: passed.append('Wrong PNG metadata')
    else: raise RuntimeError('Wrong sprite metadata passed')
report={'passed':len(passed),'checks':passed}
(root/'Audit/Runs/latest/preparation-regression.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
