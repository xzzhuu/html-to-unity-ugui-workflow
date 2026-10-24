"""Record and validate actual Unity Editor business-flow evidence, separately from native UI."""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
from game_workflow import inside, read, sha
from record_unity_acceptance import json_bytes, publish


def validate_report(root, project, report, evidence_root):
    from PIL import Image
    root=Path(root).resolve()
    if not isinstance(report,dict) or report.get('schemaVersion')!=1 or report.get('projectName')!=root.name or report.get('target')!='UnityEditor' or report.get('scope')!='editor-demo' or report.get('status')!='passed':
        raise ValueError('Missing completed Editor demo report')
    checks=report.get('checks')
    if not isinstance(checks,list) or not checks or any(not isinstance(c,dict) or c.get('status')!='passed' or not c.get('name') for c in checks):
        raise ValueError('Editor checks must all pass')
    expected={p.name for p in (root/'Panels').iterdir() if (p/'html/panel.package.json').is_file()}
    if set(report.get('panels',[]))!=expected:raise ValueError('Editor panel coverage differs')
    screenshots=report.get('screenshots')
    if not isinstance(screenshots,list) or not screenshots or {s.get('panel') for s in screenshots}!=expected:raise ValueError('Editor screenshots must cover every panel')
    names=set()
    for item in screenshots:
        if item.get('step') in names:raise ValueError('Duplicate Editor screenshot step')
        names.add(item.get('step'));path=inside(evidence_root,item['path'])
        if sha(path)!=item['sha256']:raise ValueError('Editor screenshot changed')
        with Image.open(path) as image:
            if image.size!=(1920,1080):raise ValueError('Editor screenshot dimensions differ')
            image.verify()
    inputs=report.get('inputFilesSha256')
    if not isinstance(inputs,dict) or not inputs:raise ValueError('Missing Editor input fingerprints')
    for relative,digest in inputs.items():
        if sha(inside(root,relative))!=digest:raise ValueError('Editor input changed: '+relative)
    if not isinstance(report.get('adapterPath'),str) or not report['adapterPath'].startswith('Assets/Editor/') or not report['adapterPath'].endswith('.cs'):raise ValueError('Editor adapter must be an Editor-only source')
    source=inside(evidence_root,'editor-adapter.cs.txt')
    if sha(source)!=report.get('adapterSha256'):raise ValueError('Archived Editor adapter changed')
    protected=report.get('protectedAssetsSha256')
    if not isinstance(protected,dict) or not protected:raise ValueError('Missing protected asset fingerprints')
    if project:
        project=Path(project).resolve()
        if sha(inside(project,report['adapterPath']))!=report['adapterSha256']:raise ValueError('Editor adapter is stale')
        for path,digest in protected.items():
            if not path.startswith('Assets/'):raise ValueError('Protected asset path must be under Assets')
            if sha(inside(project,path))!=digest:raise ValueError('Editor protected asset changed: '+path)


def validate_editor_acceptance(panel, project=None):
    panel=Path(panel).resolve();root=panel.parent.parent
    record=read(panel/'design/review/business-acceptance.json')
    if not isinstance(record,dict) or record.get('status')!='passed' or record.get('scope')!='editor-demo' or record.get('target')!='UnityEditor':raise ValueError('Missing Editor business acceptance')
    if record.get('nativeAcceptanceSha256')!=sha(panel/'design/review/unity-acceptance.json'):raise ValueError('Editor native acceptance is stale')
    evidence=inside(root,record['evidence']);report=read(evidence)
    if sha(evidence)!=record.get('evidenceSha256'):raise ValueError('Editor evidence changed')
    validate_report(root,project,report,evidence.parent)
    return dict(status='passed',scope='editor-demo',target='UnityEditor',checks=len([c for c in report['checks'] if not c['name'].startswith('asset unchanged:')]),evidence=record['evidence'])


def record(root, project, evidence, reviewed=False):
    from validate_directory import validate_unity_acceptance
    if not reviewed:raise ValueError('Review actual Editor screenshots before recording')
    root=Path(root).resolve();project=Path(project).resolve();evidence=Path(evidence).resolve()
    report=read(evidence/'editor-flow.json');payloads={}
    adapter=inside(project,report['adapterPath'])
    if sha(adapter)!=report.get('adapterSha256'):raise ValueError('Editor adapter is stale')
    # Validate without mutating the evidence source; its archived source is mandatory on replay.
    source=adapter.read_bytes();archived=evidence/'editor-adapter.cs.txt'
    if not archived.is_file() or archived.read_bytes()!=source:raise ValueError('Archive the exact tested adapter next to editor-flow.json')
    validate_report(root,project,report,evidence)
    target=root/'design-review/editor';payloads[target/'editor-flow.json']=(evidence/'editor-flow.json').read_bytes();payloads[target/archived.name]=source
    for item in report['screenshots']:payloads[target/item['path']]=inside(evidence,item['path']).read_bytes()
    panels=[]
    for name in report['panels']:
        panel=inside(root,'Panels/'+name+'/html/panel.package.json').parent.parent
        validate_unity_acceptance(panel,project,require_import_report=True)
        acceptance=dict(schemaVersion=1,status='passed',scope='editor-demo',target='UnityEditor',reviewedAt=datetime.now(timezone.utc).isoformat(),
            nativeAcceptanceSha256=sha(panel/'design/review/unity-acceptance.json'),evidence='design-review/editor/editor-flow.json',evidenceSha256=sha(evidence/'editor-flow.json'),
            notes='Actual delivered Prefabs and native UI events in isolated Editor scenes; deterministic three-second test clock; in-memory demo session.')
        payloads[panel/'design/review/business-acceptance.json']=json_bytes(acceptance);panels.append(panel)
    publish(payloads,lambda:[validate_editor_acceptance(p,project) for p in panels])
    return dict(status='passed',scope='editor-demo',panels=len(panels),screenshots=len(report['screenshots']))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--root',required=True);parser.add_argument('--unity-project',required=True);parser.add_argument('--evidence',required=True);parser.add_argument('--reviewed-editor-ui',action='store_true');args=parser.parse_args()
    try:print(json.dumps(record(args.root,args.unity_project,args.evidence,args.reviewed_editor_ui),ensure_ascii=False))
    except (ValueError,OSError,KeyError,TypeError) as error:parser.exit(1,'Editor acceptance error: '+str(error)+'\n')
