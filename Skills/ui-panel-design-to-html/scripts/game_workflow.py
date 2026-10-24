"""Portable game contracts, evidence status and read-only change impact plans."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import re
import os
import tempfile

CONTRACTS = ('style', 'screens', 'native-assembly')
STATES = ('passed', 'failed', 'not-checked', 'stale')


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'), parse_constant=lambda value: (_ for _ in ()).throw(ValueError('Non-finite JSON number: '+value)))


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def write(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    data=(json.dumps(value,ensure_ascii=False,indent=2,allow_nan=False)+'\n').encode('utf-8')
    with tempfile.NamedTemporaryFile(dir=path.parent,suffix='.tmp',delete=False) as stream:
        temporary=Path(stream.name);stream.write(data)
    try:os.replace(temporary,path)
    finally:
        if temporary.exists():temporary.unlink()


def finite_number(value,positive=False,unity_float=False):
    if type(value) not in (int,float):return False
    try:
        return math.isfinite(value) and (value>0 if positive else True) and (not unity_float or 1.401298464e-45<=value<=3.402823466e38)
    except OverflowError:return False


def render_tokens(template, values):
    def replace(match):
        value=values
        for part in match.group(1).split('.'):
            if not isinstance(value,dict) or part not in value:raise ValueError('Unknown style token: '+match.group(1))
            value=value[part]
        if not isinstance(value,(str,int,float)):raise ValueError('Token must be scalar: '+match.group(1))
        return str(value)
    return re.sub(r'\{\{([A-Za-z0-9_.]+)\}\}',replace,template)


def font_size(size, style):
    role={40:'title',28:'section',24:'body',20:'auxiliary'}.get(size)
    return style.get('typography',{}).get(role,size) if role else size


def inside(root, relative, exists=True):
    if not isinstance(relative, str) or not relative or '\\' in relative or ':' in relative:
        raise ValueError('Expected portable relative path: '+str(relative))
    raw=Path(root)/relative
    for ancestor in (raw,*raw.parents):
        if ancestor==Path(root).parent:break
        try:
            if ancestor.is_symlink() or getattr(ancestor.lstat(),'st_file_attributes',0)&0x400:raise ValueError('Game paths cannot traverse links: '+relative)
        except FileNotFoundError:pass
    path = raw.resolve()
    if not path.is_relative_to(Path(root).resolve()) or (exists and not path.is_file()):
        raise ValueError('Missing/outside game file: '+relative)
    return path


def defaults(name):
    return {
        'style': dict(schemaVersion=1, projectName=name, status='draft',
            canvas=dict(width=1920, height=1080, background='#89939C'),
            platform=dict(target='PC', outlineAllowed=True),
            colors=dict(base='#142330', card='#203745', deep='#192E3C', text='#EEF5F7', muted='#AAC0CA',
                        teal='#4AD7BB', amber='#F2B65C', red='#F07878', line='#36505F', ink='#102B2C'),
            typography=dict(title=40, section=28, body=24, auxiliary=20),
            spacing=dict(unit=8, content=24, section=32),
            geometry=dict(panelRadius=20, buttonRadius=10, inputRadius=8),
            previewFonts=dict(regular='C:/Windows/Fonts/msyh.ttc', bold='C:/Windows/Fonts/msyhbd.ttc'),
            panels=[]),
        'screens': dict(schemaVersion=1, projectName=name, status='draft', screens=[]),
        'native-assembly': dict(schemaVersion=1, projectName=name, status='draft',
            rounding=dict(fromCssRadius=False), slider=None),
    }


def initialize(root, name=None):
    root = Path(root).resolve()
    name = name or root.name
    if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_-]*', name) or re.fullmatch(r'CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9]',name,re.I):
        raise ValueError('Game name must be a safe English directory name')
    if name != root.name:
        raise ValueError('projectName must match the game directory')
    files = {'project.assets.json': dict(schemaVersion=1, assets=[])}
    files.update({'contracts/'+key+'.json':value for key,value in defaults(name).items()})
    targets = [(inside(root,p,False),v) for p,v in files.items()]
    for relative in list(files)+['Panels']:
        raw=root/relative
        if any(p.is_symlink() for p in [raw,*raw.parents] if p==root or p.is_relative_to(root)):
            raise ValueError('Game outputs cannot cross directory links: '+relative)
    if (root/'Panels').exists() and not (root/'Panels').is_dir():
        raise ValueError('Panels must be a directory')
    for path,_ in targets:
        if path.exists() and (path.is_symlink() or not path.is_file()):
            raise ValueError('Unsafe existing output: '+str(path))
    created=[]
    for path,value in targets:
        if not path.exists():
            write(path,value); created.append(path.relative_to(root).as_posix())
    (root/'Panels').mkdir(parents=True,exist_ok=True)
    return dict(status='draft',created=created)


def diagnostics(root):
    root=Path(root).resolve()
    errors=[]
    def check(ok,path,field,message):
        if not ok:errors.append(dict(file=path,field=field,message=message))
    def obj(value,path,field):
        check(isinstance(value,dict),path,field,'Expected object')
        return value if isinstance(value,dict) else {}
    def array(value,path,field):
        check(isinstance(value,list),path,field,'Expected list')
        return value if isinstance(value,list) else []
    def strings(value,path,field):
        values=array(value,path,field)
        for i,item in enumerate(values):check(isinstance(item,str) and bool(item.strip()),path,f'{field}[{i}]','Expected nonempty string')
        valid=[x for x in values if isinstance(x,str)]
        check(len(valid)==len(set(valid)),path,field,'Duplicate entry')
        return valid
    values={}
    for name in CONTRACTS:
        path='contracts/'+name+'.json'
        try:
            data=obj(read(inside(root,path)),path,'$');values[name]=data
            check(type(data.get('schemaVersion')) is int and data['schemaVersion']==1,path,'schemaVersion','Expected version 1')
            check(data.get('projectName')==root.name,path,'projectName','Must match game directory')
            check(data.get('status') in ('draft','active'),path,'status','Expected draft or active; this is not design approval')
        except (ValueError,OSError) as error:check(False,path,'$',str(error))
    path='contracts/style.json';style=values.get('style',{})
    canvas=obj(style.get('canvas'),path,'canvas')
    for axis in ('width','height'):
        n=canvas.get(axis);check(type(n) is int and n>0,path,'canvas.'+axis,'Expected positive integer')
    valid_color=lambda color:isinstance(color,str) and bool(re.fullmatch(r'#[0-9a-fA-F]{6}(?:[0-9a-fA-F]{2})?',color))
    check(valid_color(canvas.get('background')),path,'canvas.background','Expected hex color')
    colors=obj(style.get('colors'),path,'colors')
    for key,color in colors.items():check(valid_color(color),path,'colors.'+key,'Expected hex color')
    for section in ('typography','geometry','spacing'):
        for key,n in obj(style.get(section,{}),path,section).items():
            check(finite_number(n) and (n>=0 if section=='spacing' else n>0),path,section+'.'+key,'Expected finite design value')
    for key,value in obj(style.get('previewFonts',{}),path,'previewFonts').items():
        check(isinstance(value,str) and bool(value.strip()),path,'previewFonts.'+key,'Expected font dependency path')
    assets={}
    try:
        data=obj(read(inside(root,'project.assets.json')),'project.assets.json','$')
        check(type(data.get('schemaVersion')) is int and data['schemaVersion']==1,'project.assets.json','schemaVersion','Expected version 1')
        for i,item in enumerate(array(data.get('assets'),'project.assets.json','assets')):
            if not isinstance(item,dict):check(False,'project.assets.json',f'assets[{i}]','Expected object');continue
            key=item.get('assetId')
            if isinstance(key,str):assets[key.casefold()]=item
    except (ValueError,OSError) as error:check(False,'project.assets.json','$',str(error))
    path='contracts/native-assembly.json';assembly=values.get('native-assembly',{})
    rounding=obj(assembly.get('rounding'),path,'rounding')
    check(type(rounding.get('fromCssRadius')) is bool,path,'rounding.fromCssRadius','Expected boolean')
    slider=assembly.get('slider')
    if slider is not None:
        slider=obj(slider,path,'slider')
        for key in ('trackHeight','handleSize','trackRadius'):
            n=slider.get(key);check(finite_number(n,positive=True,unity_float=True),path,'slider.'+key,'Expected positive finite Unity float')
        for key,part in (('trackAssetId','Track'),('handleAssetId','Handle')):
            identifier=slider.get(key);asset=assets.get(identifier.casefold()) if isinstance(identifier,str) else None
            if assembly.get('status')=='active':check(asset is not None and asset.get('componentType')=='Slider' and asset.get('part')==part and asset.get('approved') is True,path,'slider.'+key,'Expected approved Slider '+part+' asset')
        for key in ('trackColorToken','fillColorToken','handleColorToken'):
            value=slider.get(key)
            if assembly.get('status')=='active':check(isinstance(value,str) and value in colors,path,'slider.'+key,'Unknown style color token')
    fixture_ids=None
    if (root/'contracts/fixtures.json').exists():
        path='contracts/fixtures.json'
        try:
            data=obj(read(inside(root,path)),path,'$');fixture_ids=[]
            check(type(data.get('schemaVersion')) is int and data['schemaVersion']==1,path,'schemaVersion','Expected version 1')
            check(data.get('projectName')==root.name,path,'projectName','Must match game directory')
            for i,item in enumerate(array(data.get('fixtures'),path,'fixtures')):
                item=obj(item,path,f'fixtures[{i}]');identifier=item.get('id')
                check(isinstance(identifier,str) and bool(re.fullmatch(r'[A-Za-z][A-Za-z0-9_-]*',identifier)),path,f'fixtures[{i}].id','Invalid fixture ID')
                if isinstance(identifier,str):fixture_ids.append(identifier)
                obj(item.get('expected'),path,f'fixtures[{i}].expected')
            check(len(fixture_ids)==len(set(fixture_ids)),path,'fixtures','Duplicate fixture ID')
        except (ValueError,OSError) as error:check(False,path,'$',str(error))
    path='contracts/screens.json';names=[]
    for i,item in enumerate(array(values.get('screens',{}).get('screens'),path,'screens')):
        item=obj(item,path,f'screens[{i}]');name=item.get('panelName')
        valid=isinstance(name,str) and bool(re.fullmatch(r'[A-Z][A-Za-z0-9]*Panel',name))
        check(valid,path,f'screens[{i}].panelName','Invalid panel name')
        if valid:names.append(name)
        for key in ('entryPoints','actions','states','fixtures','tests'):
            entries=strings(item.get(key),path,f'screens[{i}].{key}')
            if key=='fixtures' and fixture_ids is not None:
                for fixture in entries:check(fixture in fixture_ids,path,f'screens[{i}].fixtures','Unknown fixture: '+fixture)
        check(type(item.get('implemented',False)) is bool,path,f'screens[{i}].implemented','Expected boolean')
        if valid and item.get('implemented') is True:check((root/'Panels'/name/'.html-directory.json').is_file(),path,f'screens[{i}].panelName','Implemented panel missing')
        for j,rule in enumerate(array(item.get('fixturePresentation',[]),path,f'screens[{i}].fixturePresentation')):
            field=f'screens[{i}].fixturePresentation[{j}]';rule=obj(rule,path,field);kind=rule.get('kind')
            check(kind in ('text-length','text-height','ratio-width'),path,field+'.kind','Unknown presentation rule')
            required=['targetBinding']+(['sourceBinding'] if kind!='ratio-width' else ['numerator','denominator'])
            for key in required:check(isinstance(rule.get(key),str) and bool(rule[key].strip()),path,field+'.'+key,'Expected binding or data key')
            if kind=='text-length':
                check(type(rule.get('threshold')) is int and 0<=rule['threshold']<=2147483647,path,field+'.threshold','Expected nonnegative Int32')
                for state in ('short','long'):
                    settings=obj(rule.get(state),path,field+'.'+state)
                    for key,n in settings.items():check(key in ('fontSize','fontSizeMin','height','canvasY') and finite_number(n) and (n>=0 if key=='canvasY' else n>0),path,field+'.'+state+'.'+key,'Invalid presentation setting')
                    if 'fontSizeMin' in settings:check(finite_number(settings.get('fontSize'),positive=True) and finite_number(settings['fontSizeMin'],positive=True) and settings['fontSizeMin']<=settings['fontSize'],path,field+'.'+state+'.fontSizeMin','Invalid minimum font size')
            for key in (('minimumHeight','charactersPerLine','lineHeight') if kind=='text-height' else ('maximumWidth',) if kind=='ratio-width' else ()):
                check(finite_number(rule.get(key),positive=True,unity_float=True),path,field+'.'+key,'Expected positive finite presentation size')
    check(len(names)==len(set(names)),path,'screens','Duplicate panelName')
    return errors


def evidence_state(panel, project=None):
    from validate_directory import validate_unity_acceptance
    path=Path(panel)/'design/review/unity-acceptance.json'
    if not path.exists():return dict(status='not-checked',reason='Missing unity-acceptance.json')
    try:
        record=read(path)
        if not isinstance(record,dict):raise ValueError('Unity acceptance must be an object')
        if record.get('status')=='failed':return dict(status='failed',reason=record.get('notes','Recorded failed acceptance'))
        if record.get('status')!='passed':return dict(status='not-checked',reason='Acceptance is not complete')
        validate_unity_acceptance(Path(panel),project,require_import_report=True)
        return dict(status='passed',evidence=path.name,scope=record.get('scope','native-ui'))
    except (ValueError,KeyError,TypeError,OSError) as e:return dict(status='stale',reason=str(e))


def status(root, project=None):
    from validate_directory import validate,device_state
    root=Path(root).resolve();errors=diagnostics(root);panels=[]
    try:screens=read(root/'contracts/screens.json')
    except (ValueError,OSError):screens={}
    screen_map={s['panelName']:s for s in screens.get('screens',[]) if isinstance(s,dict) and isinstance(s.get('panelName'),str)} if isinstance(screens,dict) and isinstance(screens.get('screens'),list) else {}
    for config in sorted((root/'Panels').glob('*/.html-directory.json')):
        panel=config.parent
        try:
            result=validate(panel)
            html=dict(status='passed' if result['artifactReady'] else 'not-checked')
            design=dict(status='passed' if result['designApproved'] else 'not-checked')
        except (ValueError,KeyError,TypeError,OSError) as error:
            html=dict(status='stale',reason=str(error));design=dict(status='not-checked')
        unity=evidence_state(panel,project)
        try:acceptance=read(panel/'design/review/unity-acceptance.json')
        except (ValueError,OSError):acceptance={}
        if not isinstance(acceptance,dict):acceptance={}
        declared=screen_map.get(panel.name,{}).get('fixtures',[])
        declared=[f for f in declared if isinstance(f,str)] if isinstance(declared,list) else []
        tested=acceptance.get('testedFixtures',[]) if unity['status']=='passed' else []
        tested=[f for f in tested if isinstance(f,str)] if isinstance(tested,list) else []
        device=dict(status='not-checked',reason='No target device acceptance evidence')
        if (panel/'design/review/device-acceptance.json').is_file():
            try:
                value=device_state(panel);device=dict(status='not-checked' if value=='not-verified' else value)
            except (ValueError,KeyError,TypeError,OSError) as error:device=dict(status='stale',reason=str(error))
        business=dict(status='not-checked',reason='No Editor business-flow acceptance evidence')
        if (panel/'design/review/business-acceptance.json').is_file():
            try:
                from editor_acceptance import validate_editor_acceptance
                business=validate_editor_acceptance(panel,project)
                if unity['status']!='passed':business=dict(status='stale',reason='Native UI evidence is stale')
            except (ValueError,KeyError,TypeError,OSError) as error:business=dict(status='stale',reason=str(error))
        panels.append(dict(panelName=panel.name,design=design,html=html,unity=unity,
            fixtureCoverage=dict(declared=declared,nativeTested=tested,nativePending=[f for f in declared if f not in tested]),
            businessIntegration=business,device=device))
    complete=bool(panels) and not errors and all(p['unity']['status']=='passed' and not p['fixtureCoverage']['nativePending'] for p in panels)
    editor_complete=complete and all(p['businessIntegration']['status']=='passed' for p in panels)
    policy=root/'design-review/verification-scope.json'
    if policy.is_file():
        try:
            policy=read(policy)
            if policy.get('target')=='UnityEditor' and policy.get('deviceRequired') is False and policy.get('requestedBy')=='user':
                for panel in panels:
                    if panel['device']['status']=='not-checked':panel['device']=dict(status='not-applicable',reason='User requested Unity Editor testing only')
        except (ValueError,AttributeError,OSError):pass
    return dict(schemaVersion=1,projectName=root.name,scope='native-ui',status='passed' if complete else 'incomplete',editorVerification='passed' if editor_complete else 'incomplete',contractErrors=errors,panels=panels)


def fingerprints(root):
    root=Path(root).resolve();files={};consumers={};asset_sources={}
    registry=read(root/'project.assets.json');definitions={}
    if not isinstance(registry,dict) or not isinstance(registry.get('assets'),list):raise ValueError('Project asset registry must contain an assets list')
    for item in registry.get('assets',[]):
        if not isinstance(item,dict) or not isinstance(item.get('assetId'),str):continue
        key=item['assetId'].casefold();definitions[key]=item
        if isinstance(item.get('source'),str):asset_sources.setdefault(item['source'],[]).append(key)
    candidates=list((root/'contracts').rglob('*.json'))+[root/'project.assets.json']
    candidates.extend(p for p in (root/'design-tools').rglob('*') if p.suffix in ('.py','.js','.cjs','.css','.json'))
    candidates.extend(inside(root,path,False) for path in asset_sources)
    candidates.extend((root/'shared-art-source').rglob('*'))
    for panel in (root/'Panels').glob('*'):
        if not panel.is_dir():continue
        for folder in ('inputs','html'):candidates.extend((panel/folder).rglob('*'))
        candidates.extend([panel/'design/element-groups.json',panel/'design/review/approval.json',panel/'design/review/element-groups-approval.json'])
        path=panel/'html/sprites.json'
        if path.is_file():
            for sprite in read(path).get('sprites',[]):
                key=sprite.get('path','')
                if isinstance(key,str) and key.startswith('asset:'):consumers.setdefault(key[6:].casefold(),[]).append(panel.name)
    for p in candidates:
        if p.is_file():
            relative=p.relative_to(root).as_posix();inside(root,relative)
            files[relative]=sha(p)
    return dict(schemaVersion=1,projectName=root.name,files=dict(sorted(files.items())),assetDefinitions=definitions,
                assetSources=asset_sources,assetConsumers={key:sorted(set(names)) for key,names in consumers.items()})


def impact(root, baseline):
    current=fingerprints(root);old=read(baseline)
    if not isinstance(old,dict) or old.get('projectName')!=current['projectName']:raise ValueError('Baseline belongs to another game')
    if not isinstance(old.get('files'),dict):raise ValueError('Invalid baseline file fingerprints')
    changes=[p for p in sorted(set(old['files'])|set(current['files'])) if old['files'].get(p)!=current['files'].get(p)]
    screens=read(Path(root)/'contracts/screens.json').get('screens',[])
    all_panels=sorted({s['panelName'] for s in screens if s.get('implemented')} | {p.parent.name for p in (Path(root)/'Panels').glob('*/.html-directory.json')})
    affected={};changed_assets=set()
    for path in changes:
        identities=set(old.get('assetSources',{}).get(path,[]))|set(current['assetSources'].get(path,[]))
        if path=='project.assets.json' and 'assetDefinitions' in old:
            identities={key for key in set(old['assetDefinitions'])|set(current['assetDefinitions']) if old['assetDefinitions'].get(key)!=current['assetDefinitions'].get(key)}
        if path.startswith('Panels/'):targets=[path.split('/')[1]]
        elif identities:
            changed_assets.update(identities)
            targets=sorted({p for key in identities for p in old.get('assetConsumers',{}).get(key,[])+current['assetConsumers'].get(key,[])})
            if not old.get('assetConsumers') and not current['assetConsumers']:targets=all_panels
        else:targets=all_panels
        for panel in targets:affected.setdefault(panel,[]).append(path)
    return dict(schemaVersion=1,projectName=current['projectName'],readOnly=True,changedFiles=changes,changedAssets=sorted(changed_assets),
        assetSyncRequired=bool(changed_assets or 'project.assets.json' in changes),
        panels=[dict(panelName=panel,reasons=paths,requires=['html','native-ui','formal-artifact-fingerprints'],
            designReviewRequired=any('/design/' in path or path.startswith(('contracts/style','shared-art-source/','shared-assets/')) for path in paths)) for panel,paths in sorted(affected.items())])


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action',choices=('init','check','status','snapshot','impact'))
    parser.add_argument('--root',required=True)
    parser.add_argument('--unity-project')
    parser.add_argument('--baseline')
    parser.add_argument('--output')
    parser.add_argument('--require-unity',action='store_true',help='Make status fail unless every declared native fixture is verified')
    parser.add_argument('--require-editor',action='store_true',help='Also require current Editor business-flow evidence')
    args=parser.parse_args()
    try:
        if args.action=='init':result=initialize(args.root)
        elif args.action=='check':result=dict(errors=diagnostics(args.root))
        elif args.action=='status':result=status(args.root,args.unity_project)
        elif args.action=='snapshot':result=fingerprints(args.root)
        else:
            if not args.baseline:raise ValueError('impact requires --baseline')
            result=impact(args.root,args.baseline)
        if args.output:write(args.output,result)
        print(json.dumps(result,ensure_ascii=False,indent=2))
        return 1 if result.get('errors') or result.get('contractErrors') or (args.require_unity and result.get('status')!='passed') or (args.require_editor and result.get('editorVerification')!='passed') else 0
    except (ValueError,KeyError,TypeError,OSError) as error:
        parser.exit(1,'Workflow error: '+str(error)+'\n')


if __name__=='__main__':raise SystemExit(main())
