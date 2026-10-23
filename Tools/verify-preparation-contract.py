"""Negative tests ensure preparation validation catches the actual failure modes."""
import importlib.util
import json
from pathlib import Path
import sys
import tempfile
import xml.etree.ElementTree as ET
from PIL import Image
sys.dont_write_bytecode=True
root=Path(__file__).resolve().parents[1]
(root/'Audit/Runs/latest').mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(root/'Skills/ui-panel-design-to-html/scripts'))
from validate_directory import validate
from init_directory import initialize
from pipeline_contract import project_elements, serialize_xhtml

def fixture(target):
    initialize(target,panel="SyntheticPanel")
    entry=target/"html/SyntheticPanel.html"
    tree=ET.parse(entry); panel=next(n for n in tree.iter() if n.get("data-root")=="true")
    ns="{http://www.w3.org/1999/xhtml}"
    ET.SubElement(panel,ns+"input",{"id":"Name","data-ui-name":"Input_Name","data-ugui":"InputField","style":"width:200px;height:40px"})
    scroll=ET.SubElement(panel,ns+"div",{"id":"List","data-ui-name":"Scroll_List","data-ugui":"ScrollView","style":"width:200px;height:180px"})
    template=ET.SubElement(scroll,ns+"template",{"data-prefab":"Rows"})
    ET.SubElement(template,ns+"div",{"id":"Row","data-ui-name":"Item_Row","data-ugui":"Button","style":"width:100px;height:30px"})
    entry.write_text(serialize_xhtml(tree.getroot()),encoding="utf-8")
    elements,templates=project_elements(panel)
    for item in elements: item.update(purpose="Synthetic test element",dataSource=None,states=[])
    for item in templates: item.update(itemSize=[100,30],stepPx=34,dataSource="Synthetic.Rows",states=["default"])
    (target/"html/elements.json").write_text(json.dumps(dict(schemaVersion=1,panelName="SyntheticPanel",status="draft-unapproved",elements=elements,templates=templates)),encoding="utf-8")
    validate(target)

passed=[]
def reject(name,mutate):
    with tempfile.TemporaryDirectory(prefix='preparation-contract-') as directory:
        target=Path(directory)/'Inventory'
        fixture(target)
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
reject('Missing linked CSS',lambda p:(p/'html/SyntheticPanel.css').unlink())
reject('Non-void HTML self-closing tag',lambda p:(p/'html/SyntheticPanel.html').write_text((p/'html/SyntheticPanel.html').read_text(encoding='utf-8').replace('</body>','<span /></body>'),encoding='utf-8'))
reject('Wrong Unity destination',lambda p:(p/'html/panel.package.json').write_text((p/'html/panel.package.json').read_text().replace('Assets/Art/UIPrefab/SyntheticPanel','Assets/Other'),encoding='utf-8'))
with tempfile.TemporaryDirectory(prefix='sprite-contract-') as directory:
    target=Path(directory)/'Settings'
    fixture(target)
    path=target/'html/sprites.json'
    data=json.loads(path.read_text(encoding='utf-8'))
    source=target/'html/assets/sprites/Img_MetadataTest.png'
    Image.new('RGBA',(4,4),(255,255,255,255)).save(source)
    prefix=json.loads((target/'html/panel.package.json').read_text(encoding='utf-8'))['assetPrefix']
    data['sprites'].append(dict(source='assets/sprites/Img_MetadataTest.png',path=prefix+'sprites/Img_MetadataTest.png',
        pixelSize=[1,1],hasAlpha=True,role='Sprite',border=[0,0,0,0],purpose='Synthetic metadata rejection fixture',usage='preview-state'))
    path.write_text(json.dumps(data),encoding='utf-8')
    try: validate(target)
    except ValueError as error:
        assert 'Image metadata mismatch' in str(error), error
        passed.append('Wrong PNG metadata')
    else: raise RuntimeError('Wrong sprite metadata passed')
report={'passed':len(passed),'checks':passed}
(root/'Audit/Runs/latest/preparation-regression.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
