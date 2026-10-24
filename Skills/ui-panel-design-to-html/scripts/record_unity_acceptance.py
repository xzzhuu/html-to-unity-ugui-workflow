"""Publish a complete, reviewed native fixture matrix as one recoverable transaction."""
import argparse
from datetime import datetime, timezone, timedelta
import importlib.util
import json
import os
from pathlib import Path
import re
import tempfile
import hashlib
from PIL import Image
from game_workflow import read, sha, inside
from validate_directory import validate, validate_unity_acceptance, verify_project_artifact


def json_bytes(value):
    return (json.dumps(value,ensure_ascii=False,indent=2,allow_nan=False)+'\n').encode('utf-8')


def object_file(path):
    value=read(path)
    if not isinstance(value,dict):raise ValueError('Expected JSON object: '+str(path))
    return value


def unique_index(items, key, label):
    if not isinstance(items,list) or not items:raise ValueError('Missing '+label)
    indexed={}
    for item in items:
        if not isinstance(item,dict):raise ValueError('Invalid '+label+' entry')
        identity=key(item)
        if identity in indexed:raise ValueError('Duplicate '+label+' identity: '+str(identity))
        indexed[identity]=item
    return indexed


def atomic_bytes(path,data):
    path=Path(path);path.parent.mkdir(parents=True,exist_ok=True)
    with tempfile.NamedTemporaryFile(dir=path.parent,suffix='.tmp',delete=False) as stream:
        temporary=Path(stream.name);stream.write(data)
    try:os.replace(temporary,path)
    finally:
        if temporary.exists():temporary.unlink()


def publish(payloads, verify):
    """Restore every touched file if writing or final validation fails."""
    before={path:path.read_bytes() if path.is_file() else None for path in payloads}
    touched=[];new_directories=set()
    try:
        for path,data in payloads.items():
            ancestor=path.parent
            while not ancestor.exists():new_directories.add(ancestor);ancestor=ancestor.parent
            touched.append(path);atomic_bytes(path,data)
        verify()
    except Exception:
        for path in reversed(touched):
            if before[path] is None:
                if path.exists():path.unlink()
            else:atomic_bytes(path,before[path])
        for directory in sorted(new_directories,key=lambda p:len(p.parts),reverse=True):
            if directory.exists() and not any(directory.iterdir()):directory.rmdir()
        raise


def record(root,project,evidence,browser_reference,reviewed=False):
    if reviewed is not True:raise ValueError('Inspect actual native screenshots before recording native UI acceptance')
    root=Path(root).resolve();project=Path(project).resolve();evidence=Path(evidence).resolve();browser_reference=Path(browser_reference).resolve()
    delivery=object_file(root/'unity-delivery.json')
    references=object_file(root/'design-review/unity-reference-import.json')['panels']
    screens=object_file(root/'contracts/screens.json')['screens']
    expected={(s['panelName'],f) for s in screens if s.get('implemented') for f in s['fixtures']}
    browser=object_file(browser_reference/'matrix.json');native=object_file(evidence/'matrix.json')
    for matrix in (browser,native):
        if matrix.get('status')!='passed' or matrix.get('projectName')!=root.name:raise ValueError('Matrix is incomplete or belongs to another game')
    if native.get('browserMatrixSha256')!=sha(browser_reference/'matrix.json'):raise ValueError('Native matrix uses stale browser data')
    views=browser.get('viewports')
    if not isinstance(views,list) or not views or any(not isinstance(v,list) or len(v)!=2 or any(type(n) is not int or not 0<n<=8192 for n in v) for v in views):raise ValueError('Invalid native viewport matrix')
    viewports={str(w)+'x'+str(h):(w,h) for w,h in views}
    if len(viewports)!=len(views):raise ValueError('Duplicate viewport')
    browser_cases=unique_index(browser.get('cases'),lambda c:(c['panel'],c['fixture']),'browser case')
    native_cases=unique_index(native.get('cases'),lambda c:(c['panel'],c['fixture'],c['viewport']),'native case')
    if set(browser_cases)!=expected:raise ValueError('Browser matrix does not cover the declared screen fixtures')
    if set(native_cases)!={(p,f,v) for p,f in expected for v in viewports}:raise ValueError('Native matrix does not cover every declared fixture and viewport')
    spec=importlib.util.spec_from_file_location('geometry_compare',Path(__file__).with_name('compare-layout.py'))
    comparator=importlib.util.module_from_spec(spec);spec.loader.exec_module(comparator)
    final_index={a['path']:a for a in delivery['assets']}
    payloads={};completed=[]
    for panel_name in sorted({p for p,_ in expected}):
        panel=inside(root,'Panels/'+panel_name,False)
        validate(panel,require_ready=True)
        formals=[path for path in final_index if Path(path).name==panel_name+'.prefab']
        if len(formals)!=1:raise ValueError('Formal panel identity is missing or ambiguous: '+panel_name)
        formal=formals[0]
        screenshots=[];evidence_files=[];cases=[];artifacts=None;sample=None
        def add(relative,data):
            target=inside(panel,relative,False)
            if target.exists() and not target.is_file():raise ValueError('Evidence target is not a file: '+relative)
            if target in payloads and payloads[target]!=data:raise ValueError('Conflicting evidence destination: '+relative)
            payloads[target]=data;return relative
        html_files={p.relative_to(panel).as_posix():sha(p) for p in (panel/'html').rglob('*') if p.is_file()}
        for _,fixture in sorted(pair for pair in expected if pair[0]==panel_name):
            entry=browser_cases[panel_name,fixture]
            reference=inside(browser_reference,entry['reference']);source=object_file(reference)
            if source.get('panel')!=panel_name or source.get('fixture')!=fixture or not source.get('nodes'):raise ValueError('Browser fixture identity/nodes mismatch')
            if sha(reference)!=entry['referenceSha256'] or source.get('sourceFilesSha256')!=html_files:raise ValueError('Browser source files changed after capture')
            inputs=source.get('gameInputsSha256')
            if not isinstance(inputs,dict) or 'project.assets.json' not in inputs:raise ValueError('Missing game input fingerprints')
            for relative,digest in inputs.items():
                if sha(inside(root,relative))!=digest:raise ValueError('Game inputs changed after browser capture: '+relative)
            canonical=fixture==('Result' if panel_name=='ExpeditionResultPanel' else 'Prepared')
            prefix='design/review/unity' if canonical else 'design/review/unity/fixtures/'+fixture
            reference_name=add(prefix+'/browser-reference.json',reference.read_bytes());evidence_files.append(reference_name)
            for viewport in sorted(viewports):
                entry=native_cases[panel_name,fixture,viewport]
                if entry.get('status')!='passed':raise ValueError('Native case has not passed: '+str((panel_name,fixture,viewport)))
                audit_path=inside(evidence,entry['audit']);shot=inside(evidence,entry['screenshot']);audit=object_file(audit_path)
                if audit.get('schemaVersion')!=2 or audit.get('prefab')!=formal or audit.get('fixture')!=fixture:raise ValueError('Native audit identity/version mismatch')
                if audit.get('status')!='passed' or not audit.get('nativeControls'):raise ValueError('Native controls have not passed')
                if audit.get('referenceSha256')!=sha(reference) or audit.get('screenshotSha256')!=sha(shot):raise ValueError('Audit source or screenshot changed')
                with Image.open(shot) as image:
                    if image.size!=viewports[viewport] or image.size!=(audit.get('viewportWidth'),audit.get('viewportHeight')):raise ValueError('Screenshot dimensions differ from native viewport')
                    image.verify()
                current=unique_index(audit.get('artifacts'),lambda a:a['path'],'native artifact')
                if formal not in current or current[formal]['sha256']!=audit.get('prefabSha256') or current[formal]['metaSha256']!=audit.get('prefabMetaSha256'):raise ValueError('Audit is not bound to the actual prefab bytes')
                for artifact in current.values():verify_project_artifact(project,artifact)
                if artifacts is not None and artifacts!=current:raise ValueError('Native dependencies changed within the matrix')
                artifacts=current;sample=source
                comparison=comparator.compare(source,audit,1.1)
                if comparison['status']!='pass':raise ValueError('Native geometry failed: '+str((panel_name,fixture,viewport))+': '+str(comparison['differences']))
                target=prefix+'/'+viewport
                audit_name=add(target+'/native-audit.json',audit_path.read_bytes())
                screenshot_name=add(target+'/actual.png',shot.read_bytes())
                geometry_name=add(target+'/geometry.json',json_bytes(comparison))
                screenshots.append(screenshot_name);evidence_files.extend([audit_name,geometry_name])
                cases.append(dict(fixture=fixture,viewport=viewport,reference=reference_name,audit=audit_name,screenshot=screenshot_name,geometry=geometry_name))
        refs=[]
        for item in references[panel_name]:
            source=inside(panel,item['source'])
            if not item['unityPath'].startswith('Assets/'):raise ValueError('Reference must be a Unity Assets path')
            target=inside(project,item['unityPath'])
            if sha(source)!=item['sha256'] or sha(target)!=item['sha256']:raise ValueError('Imported reference bytes differ')
            meta=Path(str(target)+'.meta');match=re.search(r'^guid:\s*(\w+)',meta.read_text(encoding='utf-8'),re.M)
            if not match:raise ValueError('Imported reference GUID missing')
            refs.append(dict(item,guid=match[1],metaSha256=sha(meta)))
        reports=[r for r in delivery['sourceImportReports'] if Path(r['path']).name==panel_name+'.import-report.json']
        if len(reports)!=1 or sha(project/reports[0]['path'])!=reports[0]['sha256']:raise ValueError('Source conversion report changed')
        report_source=add('design/review/unity-import-report.json',(project/reports[0]['path']).read_bytes())
        manifest=object_file(panel/'design/element-groups.json')
        sources=['design/element-groups.json',report_source]+[s['source'] for s in manifest['sprites']]+screenshots+evidence_files
        digests={s:sha(inside(panel,s)) if inside(panel,s,False) not in payloads else hashlib.sha256(payloads[inside(panel,s,False)]).hexdigest() for s in set(sources)}
        acceptance=dict(schemaVersion=2,status='passed',scope='native-ui',reviewer='Codex: actual fixture image review and native component checks',
            reviewedAt=datetime.now(timezone(timedelta(hours=8))).isoformat(),
            approvedLayoutSha256=sha(panel/'design/review/layout-approved.png'),approvedElementGroupsSha256=sha(panel/'design/review/element-groups-approval.json'),
            checks={key:'passed' for key in ('structure','visual','interactions','references','resolution')},screenshots=screenshots,
            evidenceFiles=sorted(set(evidence_files)),matrixCases=cases,referenceImages=refs,importReport=report_source,sourceImportReportSha256=digests[report_source],
            finalArtifacts=[a for path,a in artifacts.items() if path in final_index],externalDependencies=[a for path,a in artifacts.items() if path not in final_index],
            gameContractsSha256=sample['gameContractsSha256'],gameInputsSha256=sample['gameInputsSha256'],filesSha256=digests,
            testedResolutions=[dict(width=w,height=h,mode='isolated-uniform-fit') for w,h in views],testedFixtures=sorted({c['fixture'] for c in cases}),
            businessIntegration=dict(status='not-checked',reason='Game-service wiring is outside native UI acceptance'),device=dict(status='not-checked',reason='No target-device run'),
            notes='All declared native fixtures and viewports are covered; dynamic presentation uses declared fixture rules. Existing business callbacks are isolated. This does not certify game services or device behavior.')
        add('design/review/unity-acceptance.json',json_bytes(acceptance));completed.append(panel)
    def verify():
        for panel in completed:validate_unity_acceptance(panel,project,require_import_report=True)
    publish(payloads,verify)
    return dict(status='passed',scope='native-ui',panels=[p.name for p in completed],fixtureCases=len(expected),viewportCases=len(native_cases))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root',required=True);parser.add_argument('--unity-project',required=True)
    parser.add_argument('--evidence',required=True);parser.add_argument('--browser-reference',required=True)
    parser.add_argument('--reviewed-native-ui',action='store_true');args=parser.parse_args()
    try:print(json.dumps(record(args.root,args.unity_project,args.evidence,args.browser_reference,args.reviewed_native_ui),ensure_ascii=False))
    except (ValueError,OSError,KeyError,TypeError) as error:parser.exit(1,'Acceptance evidence error: '+str(error)+'\n')
