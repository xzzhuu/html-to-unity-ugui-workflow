"""Validate complete workspaces: semantic objects, templates, PNGs and readiness."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
from urllib.parse import unquote,urlsplit
import xml.etree.ElementTree as ET
from pipeline_contract import DIRECTORIES,PANEL_NAME,local_tag,project_elements,require

def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def local_file(base,reference,workspace):
    url=urlsplit(reference)
    require(not url.scheme and not url.netloc and not unquote(url.path).startswith(('/','\\')),'Use portable relative references: '+reference)
    path=(base/unquote(url.path)).resolve()
    require(path.is_relative_to(workspace) and path.is_file(),'Missing/outside source file: '+reference)
    return path

def png_info(path):
    data=path.read_bytes()
    require(len(data)>=33 and data[:8]==b'\x89PNG\r\n\x1a\n' and data[12:16]==b'IHDR','Invalid PNG: '+str(path))
    width,height=struct.unpack('>II',data[16:24])
    require(width>0 and height>0,'Empty PNG')
    return width,height,data[25] in (4,6) or b'tRNS' in data

def validate(root,require_ready=False):
    root=Path(root).resolve()
    for directory in DIRECTORIES: require((root/directory).is_dir(),'Missing directory: '+directory)
    state=read_json(root/'.html-directory.json')
    name=state.get('panelName','')
    require(state.get('schemaVersion')==1 and re.fullmatch(PANEL_NAME,name) is not None,'Invalid workspace identity')
    entry=root/'html'/f'{name}.html'
    document=ET.parse(entry)
    roots=[node for node in document.iter() if node.get('data-root')=='true']
    require(len(roots)==1,'Exactly one root Panel required')
    source=roots[0]
    require(source.get('data-panel-name')==name and source.get('id')==name,'Root name/ID mismatch')
    expected,templates=project_elements(source)
    require(all(node in list(source.iter()) for node in document.iter() if local_tag(node)=='template'),'Template outside root')
    manifest=read_json(root/'html/elements.json')
    require(manifest.get('schemaVersion')==1 and manifest.get('panelName')==name,'Element manifest identity mismatch')
    records={item['elementId']:item for item in manifest['elements']}
    require(len(records)==len(manifest['elements']),'Duplicate manifest ID')
    require(set(records)=={item['elementId'] for item in expected},'Manifest source/helper coverage mismatch')
    for item in expected:
        record=records[item['elementId']]
        require(isinstance(record.get('purpose'),str) and bool(record['purpose'].strip()),'Missing semantic purpose: '+item['elementId'])
        for key in ('unityName','parentId','hierarchyPath','uguiComponent','sourceType','htmlId','generated','template','bindingKey'):
            require(record.get(key)==item[key],key+' mismatch: '+item['elementId'])
        require('dataSource' in record and isinstance(record.get('states'),list),'Missing data/state specification')
    actual_templates=manifest.get('templates',[])
    require(len(actual_templates)==len(templates),'Template count mismatch')
    for expected_template,record in zip(templates,actual_templates):
        require(all(record.get(key)==value for key,value in expected_template.items()),'Template ownership specification mismatch')
        size=record.get('itemSize')
        require(isinstance(size,list) and len(size)==2 and all(isinstance(n,(int,float)) and n>0 for n in size),'Missing positive template itemSize')
        require(isinstance(record.get('stepPx'),(int,float)) and record['stepPx']>=size[1],'Missing template row step/spacing')
        require(isinstance(record.get('dataSource'),str) and bool(record['dataSource'].strip()),'Missing template data source')
        require(isinstance(record.get('states'),list) and bool(record['states']),'Missing template states')
    references=set()
    for node in document.iter():
        for attribute in ('src','href'):
            if node.get(attribute): references.add(local_file(entry.parent,node.get(attribute),root))
    empty_tags=re.findall(r'<([A-Za-z][\w:-]*)\b[^>]*?/>',entry.read_text(encoding='utf-8'))
    require(all(tag.lower() in ('img','input','br','hr','meta','link','area','base','col','embed','param','source','track','wbr') for tag in empty_tags),'Non-void HTML tags need explicit end tags')
    for path in list(references):
        if path.suffix.lower()=='.css':
            for reference in re.findall(r'url\(\s*[\"\x27]?([^\"\x27)\s]+)',path.read_text(encoding='utf-8')):
                references.add(local_file(path.parent,reference,root))
    prefix=f'Assets/Art/UI/Sprite/{name}/'
    output=f'Assets/Art/UIPrefab/{name}'
    require(source.get('data-asset-prefix')==prefix and source.get('data-output-folder')==output,'HTML Unity output mismatch')
    package=read_json(root/'html/panel.package.json')
    require(package.get('schemaVersion')==1 and package.get('panelName')==name and package.get('html')==entry.name,'Package mismatch')
    require(package.get('assetPrefix')==prefix and package.get('outputFolder')==output,'Package Unity output mismatch')
    sprites=read_json(root/'html/sprites.json')
    require(sprites.get('schemaVersion')==1 and sprites.get('panelName')==name,'Sprite manifest identity mismatch')
    destinations,source_assets={},set()
    for item in sprites['sprites']:
        destination=item['path']
        require(destination.startswith(prefix) and '..' not in destination.split('/'),'Invalid Unity image destination')
        require(destination.casefold() not in destinations,'Duplicate image destination')
        file=local_file(entry.parent,item['source'],root)
        require(file.suffix.lower()=='.png' and item['source']=='assets/'+destination[len(prefix):],'PNG source/destination mismatch')
        width,height,alpha=png_info(file)
        require(item.get('pixelSize')==[width,height] and item.get('hasAlpha')==alpha,'Image metadata mismatch: '+item['source'])
        border=item.get('border')
        require(isinstance(border,list) and len(border)==4 and all(isinstance(side,(int,float)) and side>=0 for side in border),'Invalid L/B/R/T border')
        require(border[0]+border[2]<width and border[1]+border[3]<height,'Nine-slice border leaves no center')
        require(item.get('role') in ('Sprite','Texture') and isinstance(item.get('purpose'),str) and bool(item['purpose'].strip()),'Invalid asset role/purpose')
        require(item.get('usage') in ('source','preview-state'),'Missing image usage classification')
        destinations[destination.casefold()]=item
        source_assets.add(file)
    mappings=set()
    for node in source.iter():
        for attribute,role in (('data-sprite','Sprite'),('data-texture','Texture'),('data-normal-sprite','Sprite'),('data-selected-sprite','Sprite')):
            destination=node.get(attribute)
            if destination:
                item=destinations.get(destination.casefold())
                require(item is not None and item['role']==role,'Missing/wrong-role image mapping: '+destination)
                border=[float(p) for p in node.get('data-border','0,0,0,0').split(',')] if node.get('data-nine-slice')=='true' else [0,0,0,0]
                require(item['border']==border,'Image border mismatch: '+destination)
                mappings.add(destination.casefold())
        if node.get('data-ugui')=='RawImage': require(node.get('data-texture') is not None,'RawImage needs data-texture')
        if node.get('src') and node.get('data-ugui')=='Image': require(node.get('data-sprite') is not None,'Image needs data-sprite')
    require(all(key in mappings or item['usage']=='preview-state' for key,item in destinations.items()),'Unreferenced image requires preview-state classification')
    require(all(path in source_assets for path in references if path.suffix.lower()=='.png'),'Browser image missing from manifest')
    for axis in ('width','height'):
        match=re.search(r'(?:^|;)\s*'+axis+r'\s*:\s*(\d+(?:\.\d+)?)px\s*(?:;|$)',source.get('style',''))
        require(match is not None and float(match.group(1))==state.get(axis),'Canvas '+axis+' mismatch')
    readiness='not-established'
    if require_ready or state.get('status')=='ready':
        approval=read_json(root/'design/review/approval.json')
        digest=hashlib.sha256((root/'design/review/layout-approved.png').read_bytes()).hexdigest()
        require(approval.get('status')=='approved' and approval.get('imageSha256')==digest and bool(approval.get('userApproval','').strip()),'Missing/stale explicit layout approval')
        acceptance=read_json(root/'design/review/html-acceptance.json')
        require(acceptance.get('status')=='passed' and acceptance.get('approvedLayoutSha256')==digest,'Missing/stale HTML acceptance')
        require(all(acceptance.get('checks',{}).get(key)=='passed' for key in ('elements','naming','sprites','layout','visual','interactions')),'Acceptance checks incomplete')
        fingerprints=acceptance.get('filesSha256',{})
        for path in (root/'html').rglob('*'):
            if path.is_file(): require(fingerprints.get(path.relative_to(root).as_posix())==hashlib.sha256(path.read_bytes()).hexdigest(),'HTML acceptance stale: '+str(path))
        require(manifest.get('status')=='ready' and state.get('status')=='ready','Readiness state mismatch')
        readiness='recorded-evidence-consistent; visual-judgment-remains-the-reviewer-responsibility'
    return dict(entry=str(entry),components=len(expected),templates=len(templates),sprites=len(destinations),structure='passed',status=state['status'],visualAcceptance=readiness)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root',default='.')
    parser.add_argument('--require-ready',action='store_true')
    args=parser.parse_args()
    try: print(json.dumps(validate(args.root,args.require_ready),ensure_ascii=False,indent=2))
    except (ValueError,KeyError,OSError,ET.ParseError) as error: parser.exit(1,'Validation failed: '+str(error)+'\n')
