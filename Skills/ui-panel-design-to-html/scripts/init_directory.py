"""Initialize a portable XHTML workspace without overwriting existing files."""
import argparse
import html
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET
from pipeline_contract import DIRECTORIES, project_elements

def initialize(root, panel=None, title=None, width=None, height=None):
    root = Path(root).resolve()
    state_path = root / '.html-directory.json'
    prior = json.loads(state_path.read_text(encoding='utf-8')) if state_path.exists() else {}
    if prior and prior.get('schemaVersion') != 1:
        raise ValueError('Unsupported workspace schemaVersion')
    inferred = ''.join(part[:1].upper() + part[1:] for part in re.findall('[A-Za-z0-9]+', root.name))
    if not re.fullmatch('[A-Z][A-Za-z0-9]*', inferred or ''):
        inferred = 'Main'
    inferred = inferred if inferred.endswith('Panel') else inferred + 'Panel'
    config = dict(schemaVersion=1, panelName=panel or prior.get('panelName') or inferred,
                  title=title or prior.get('title') or root.name,
                  width=width if width is not None else prior.get('width',1280),
                  height=height if height is not None else prior.get('height',720),
                  status=prior.get('status', 'draft-unapproved'))
    if not re.fullmatch('[A-Z][A-Za-z0-9]*Panel', config['panelName']):
        raise ValueError('Panel name must be English PascalCase ending in Panel')
    if config['width'] < 480 or config['height'] < 320:
        raise ValueError('Starter canvas minimum is 480 x 320; customize after initialization')
    if prior and any(prior.get(key) != config[key] for key in ('panelName', 'title', 'width', 'height')):
        raise ValueError('Existing configuration differs: preserve it or initialize another directory')
    for directory in DIRECTORIES:
        target = root / directory
        if not target.resolve().is_relative_to(root):
            raise ValueError('Workspace directory points outside the selected root')
    for directory in DIRECTORIES:
        (root/directory).mkdir(parents=True,exist_ok=True)
    name, w, h = config['panelName'], config['width'], config['height']
    esc = html.escape(config['title'])
    source = f'''<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml"><head><meta charset="utf-8"/><title>{esc}</title><link rel="stylesheet" href="{name}.css"/></head><body>
<div id="{name}" data-root="true" data-panel-name="{name}" data-ugui="Container" data-asset-prefix="Assets/Art/UI/Sprite/{name}/" data-output-folder="Assets/Art/UIPrefab/{name}" style="width:{w}px;height:{h}px;background-color:#101b2a">
 <div id="Header" data-ui-name="Group_Header" data-ugui="Container" style="left:32px;top:24px;width:{w-64}px;height:64px">
  <span id="Title" data-ui-name="Txt_Title" data-ugui="Text" style="left:0px;top:0px;width:{w-64}px;height:64px;font-size:30px;color:#ffffff">{esc}</span>
 </div>
 <div id="Content" data-ui-name="Group_Content" data-ugui="Container" style="left:32px;top:112px;width:{w-64}px;height:{h-144}px">
  <span id="Description" data-ui-name="Txt_Description" data-ugui="Text" data-wrap="true" style="left:0px;top:0px;width:{w-64}px;height:96px;font-size:20px;color:#b8c8d8">初始化草稿：请补充需求并审核布局图，再制作正式 UI。</span>
 </div>
</div><script src="{name}.preview.js"></script></body></html>
'''
    source_root=next(node for node in ET.fromstring(source).iter() if node.get('data-root')=='true')
    elements,templates=project_elements(source_root)
    purposes={name:'整张 UI 面板的唯一根物体','Header':'面板标题区域','Title':'显示面板标题',
              'Content':'承载待设计的业务内容','Description':'提示当前为未审核的初始化草稿'}
    for item in elements:
        item.update(purpose=purposes[item['htmlId']],dataSource=None,pictureRegion=None,states=[])
    package = dict(schemaVersion=1, panelName=name, html=f'{name}.html',
                   outputFolder=f'Assets/Art/UIPrefab/{name}', assetPrefix=f'Assets/Art/UI/Sprite/{name}/', fontAsset=None, boldWeight=.12)
    files = {
        '.html-directory.json': config,
        'inputs/brief.md': '# UI 需求\n\n填写用途、必须显示的元素、动态数据、交互、状态及参考图路径。当前没有业务需求或布局审批。\n',
        f'html/{name}.html': source,
        f'html/{name}.css': 'body { margin:0; background:#080e17; }\ndiv, span { position:absolute; box-sizing:border-box; }\nspan { font-family:Arial,"Microsoft YaHei",sans-serif; display:block; }\n',
        f'html/{name}.preview.js': 'document.documentElement.dataset.previewReady = "true";\n',
        'html/elements.json': dict(schemaVersion=1, panelName=name, status='draft-unapproved', templates=templates, elements=elements),
        'html/sprites.json': dict(schemaVersion=1, panelName=name, sprites=[]),
        'html/panel.package.json': package,
    }
    created, preserved = [], []
    for relative in files:
        target=root/relative
        if not target.resolve().is_relative_to(root) or target.is_symlink():
            raise ValueError('Output escapes workspace or is a symlink: '+relative)
        if target.exists() and not target.is_file():
            raise ValueError('Expected file but found directory: '+relative)
    for relative, content in files.items():
        target = root / relative
        if target.exists():
            preserved.append(relative)
            continue
        if not target.parent.resolve().is_relative_to(root):
            raise ValueError('Output points outside workspace')
        data = json.dumps(content, ensure_ascii=False, indent=2) + '\n' if isinstance(content, dict) else content
        with target.open('x', encoding='utf-8', newline='\n') as stream:
            stream.write(data)
        created.append(relative)
    return dict(root=str(root), entry=str(root/'html'/f'{name}.html'), status=config['status'], created=created, preserved=preserved)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', default='.')
    parser.add_argument('--panel-name')
    parser.add_argument('--title')
    parser.add_argument('--width', type=int)
    parser.add_argument('--height', type=int)
    args = parser.parse_args()
    print(json.dumps(initialize(args.root, args.panel_name, args.title, args.width, args.height), ensure_ascii=False, indent=2))
