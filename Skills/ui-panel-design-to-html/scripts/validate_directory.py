"""Validate complete workspaces: semantic objects, templates, PNGs and readiness."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
from urllib.parse import unquote,urlsplit
import xml.etree.ElementTree as ET
from pipeline_contract import DIRECTORIES,VISUAL_DIRECTORIES,ASSET_PREFIXES,PANEL_NAME,local_tag,project_elements,require

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

def file_digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def definition_digest(group, sprites):
    definition={'group':group,'sprites':sorted((sprites[path] for path in group['assets']),key=lambda item:item['source'])}
    return hashlib.sha256(json.dumps(definition,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode('utf-8')).hexdigest()

def validate_layout_approval(root):
    approval=read_json(root/'design/review/approval.json')
    digest=file_digest(root/'design/review/layout-approved.png')
    require(approval.get('status')=='approved' and approval.get('imageSha256')==digest and bool(approval.get('userApproval','').strip()),'Missing/stale explicit layout approval')
    return digest

def validate_groups(root,name,mode,ready):
    path=root/'design/element-groups.json'
    if not path.exists() and not ready and mode=='full':
        return 0  # Legacy drafts remain inspectable; readiness requires the new gate.
    manifest=read_json(path)
    require(manifest.get('schemaVersion')==1 and manifest.get('panelName')==name,'Element-group identity mismatch')
    required=manifest.get('requiredComponents')
    require(isinstance(required,list) and all(kind in ASSET_PREFIXES for kind in required) and len(set(required))==len(required),'Invalid required component list')
    groups=manifest.get('groups')
    require(isinstance(groups,list) and isinstance(manifest.get('sprites'),list),'Missing element groups/sprites')
    sprites={}
    base='html/assets/' if mode=='full' else 'assets/'
    for item in manifest['sprites']:
        source=item['source']
        require(source.startswith(base),'Sprite outside mode asset directory')
        file=local_file(root,source,root)
        require(file.suffix.lower()=='.png','Element material must be PNG')
        kind=item.get('componentType')
        require(kind in ASSET_PREFIXES,'Unknown material component type')
        require(re.fullmatch(ASSET_PREFIXES[kind]+r'_[A-Z][A-Za-z0-9]*(?:_[A-Z][A-Za-z0-9]*)*',file.stem) is not None,'Missing component abbreviation prefix: '+source)
        require(source.casefold() not in {key.casefold() for key in sprites},'Duplicate element material')
        width,height,alpha=png_info(file)
        require(width & (width-1)==0 and height & (height-1)==0,'Material dimensions must be powers of two: '+source)
        require(item.get('pixelSize')==[width,height] and item.get('hasAlpha')==alpha,'Element material metadata mismatch: '+source)
        original=item.get('originalPixelSize')
        require(isinstance(original,list) and len(original)==2 and all(isinstance(n,int) and n>0 for n in original),'Missing original pixel dimensions')
        if width<original[0] or height<original[1]:
            require(bool(item.get('resolutionChangeReason','').strip()),'Unexplained source downsampling: '+source)
        border=item.get('border')
        require(isinstance(border,list) and len(border)==4 and all(isinstance(n,(int,float)) and n>=0 for n in border),'Invalid element L/B/R/T border')
        require(border[0]+border[2]<width and border[1]+border[3]<height,'Element border leaves no center')
        require(isinstance(item.get('tintable'),bool) and bool(item.get('purpose','').strip()),'Missing material tint/purpose specification')
        sprites[source]=item
    ids,coverage,used=set(),set(),set()
    approvals={}
    if ready:
        review=read_json(root/'design/review/element-groups-approval.json')
        require(review.get('status')=='approved','Element groups are not approved')
        entries=review.get('groups',[])
        require(isinstance(entries,list),'Invalid element approvals')
        approvals={item['groupId']:item for item in entries}
        require(len(approvals)==len(entries),'Duplicate element approval')
    for group in groups:
        identifier=group['groupId']
        require(isinstance(identifier,str) and re.fullmatch(r'[A-Za-z][A-Za-z0-9_-]*',identifier) and identifier.casefold() not in ids,'Invalid/duplicate group ID')
        ids.add(identifier.casefold())
        kind=group.get('componentType')
        require(kind in required,'Unrequested component group: '+str(kind))
        coverage.add(kind)
        require(bool(group.get('purpose','').strip()) and isinstance(group.get('usedBy'),list) and bool(group['usedBy']) and all(isinstance(value,str) and value.strip() for value in group['usedBy']),'Missing actual group usage')
        sheet=local_file(root,group['sheet'],root)
        require(group['sheet'].startswith('design/element-groups/') and sheet.suffix.lower()=='.png' and sheet.stem.startswith(ASSET_PREFIXES[kind]+'_'),'Invalid group sheet location/prefix')
        png_info(sheet)
        assets=group.get('assets')
        require(isinstance(assets,list) and len(set(assets))==len(assets) and all(source in sprites for source in assets),'Unknown/duplicate group material')
        used.update(assets)
        if ready:
            approval=approvals.get(identifier,{})
            require(bool(approval.get('userApproval','').strip()) and approval.get('imageSha256')==file_digest(sheet),'Missing/stale group sheet approval: '+identifier)
            require(approval.get('definitionSha256')==definition_digest(group,sprites),'Group definition approval stale: '+identifier)
            for source in assets:
                require(approval.get('filesSha256',{}).get(source)==file_digest(local_file(root,source,root)),'Group material approval stale: '+source)
    require(set(sprites)==used,'Material outside required groups')
    if ready:
        require(bool(groups) and coverage==set(required),'Required element group coverage incomplete')
        require(set(approvals)=={group['groupId'] for group in groups},'Element approval coverage mismatch')
    return len(groups)

def validate(root,require_ready=False,mode=None):
    root=Path(root).resolve()
    state=read_json(root/'.html-directory.json')
    actual_mode=state.get('mode','full')
    require(actual_mode in ('full','visual-only') and (mode is None or mode==actual_mode),'Workflow mode mismatch')
    mode=actual_mode
    for directory in (DIRECTORIES if mode=='full' else VISUAL_DIRECTORIES): require((root/directory).is_dir(),'Missing directory: '+directory)
    name=state.get('panelName','')
    require(state.get('schemaVersion')==1 and re.fullmatch(PANEL_NAME,name) is not None,'Invalid workspace identity')
    require(all(isinstance(state.get(axis),int) and state[axis]>0 for axis in ('width','height')),'Invalid canvas dimensions')
    ready=require_ready or state.get('status')=='ready'
    group_count=validate_groups(root,name,mode,ready)
    if mode=='visual-only':
        if ready:
            digest=validate_layout_approval(root)
            acceptance=read_json(root/'design/review/unity-acceptance.json')
            require(state.get('status')=='ready' and acceptance.get('status')=='passed','Visual replacement is not complete')
            require(acceptance.get('approvedLayoutSha256')==digest and acceptance.get('approvedElementGroupsSha256')==file_digest(root/'design/review/element-groups-approval.json'),'Unity acceptance approval stale')
            require(all(acceptance.get('checks',{}).get(key)=='passed' for key in ('structure','visual','interactions','references','resolution')),'Unity acceptance checks incomplete')
            screenshots=acceptance.get('screenshots')
            require(isinstance(screenshots,list) and bool(screenshots),'Missing Unity screenshot evidence')
            manifest=read_json(root/'design/element-groups.json')
            files=['design/element-groups.json']+[item['source'] for item in manifest['sprites']]+screenshots
            for source in files:
                require(acceptance.get('filesSha256',{}).get(source)==file_digest(local_file(root,source,root)),'Unity acceptance stale: '+source)
            previews=manifest.get('panelPreviews',['design/review/layout-approved.png'])
            required_refs=set(previews+[group['sheet'] for group in manifest['groups']])
            refs=acceptance.get('referenceImages',[])
            require(isinstance(refs,list) and {item['source'] for item in refs}>=required_refs,'Missing imported panel/element reference images')
            for item in refs:
                require(item.get('sha256')==file_digest(local_file(root,item['source'],root)),'Imported reference image stale')
                target=item.get('unityPath','')
                require(target.startswith('Assets/') and '\\' not in target and '..' not in target.split('/') and target.lower().endswith('.png'),'Invalid Unity reference image path')
        return dict(mode=mode,entry=None,groups=group_count,status=state['status'],structure='passed',
                    visualAcceptance='recorded-evidence-consistent; Unity/device-verification-remains-the-reviewer-responsibility' if ready else 'not-established')
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
    package=read_json(root/'html/panel.package.json')
    prefix=package.get('assetPrefix','')
    output=package.get('outputFolder','')
    for folder in (prefix,output):
        require(isinstance(folder,str) and folder.startswith('Assets/') and '\\' not in folder and not any(part in ('.','..','') for part in folder.rstrip('/').split('/')),'Invalid project asset folder')
    require(prefix.endswith('/'),'Asset prefix needs a trailing slash')
    require(source.get('data-asset-prefix')==prefix and source.get('data-output-folder')==output,'HTML Unity output mismatch')
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
    if ready:
        digest=validate_layout_approval(root)
        acceptance=read_json(root/'design/review/html-acceptance.json')
        require(acceptance.get('status')=='passed' and acceptance.get('approvedLayoutSha256')==digest,'Missing/stale HTML acceptance')
        require(acceptance.get('approvedElementGroupsSha256')==file_digest(root/'design/review/element-groups-approval.json'),'HTML element-group acceptance stale')
        require(acceptance.get('filesSha256',{}).get('design/element-groups.json')==file_digest(root/'design/element-groups.json'),'HTML element definitions acceptance stale')
        require(all(acceptance.get('checks',{}).get(key)=='passed' for key in ('elements','naming','sprites','layout','visual','interactions')),'Acceptance checks incomplete')
        fingerprints=acceptance.get('filesSha256',{})
        for path in (root/'html').rglob('*'):
            if path.is_file(): require(fingerprints.get(path.relative_to(root).as_posix())==hashlib.sha256(path.read_bytes()).hexdigest(),'HTML acceptance stale: '+str(path))
        require(manifest.get('status')=='ready' and state.get('status')=='ready','Readiness state mismatch')
        readiness='recorded-evidence-consistent; visual-judgment-remains-the-reviewer-responsibility'
        common=read_json(root/'design/element-groups.json')
        require({local_file(root,item['source'],root) for item in common['sprites']}==source_assets,'HTML/group material coverage mismatch')
    return dict(mode=mode,entry=str(entry),groups=group_count,components=len(expected),templates=len(templates),sprites=len(destinations),structure='passed',status=state['status'],visualAcceptance=readiness)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root',default='.')
    parser.add_argument('--require-ready',action='store_true')
    parser.add_argument('--mode',choices=('full','visual-only'))
    args=parser.parse_args()
    try: print(json.dumps(validate(args.root,args.require_ready,args.mode),ensure_ascii=False,indent=2))
    except (ValueError,KeyError,TypeError,OSError,ET.ParseError) as error: parser.exit(1,'Validation failed: '+str(error)+'\n')
