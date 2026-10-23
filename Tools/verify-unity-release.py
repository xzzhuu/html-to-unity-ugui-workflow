"""Run real Unity release checks in an existing, closed validation project."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--editor',required=True)
parser.add_argument('--project',required=True)
parser.add_argument('--evidence',required=True)
args=parser.parse_args()
project=Path(args.project).resolve()
evidence=Path(args.evidence).resolve()
if evidence.is_relative_to(project/'Assets'):
    parser.error('Evidence must stay outside Assets')
if (project/'Temp/UnityLockfile').exists():
    parser.error('Close the validation project before batch execution')
source=Path(__file__).resolve().parents[1]/'Packages/com.nex.html-to-ugui'
manifest=json.loads((project/'Packages/manifest.json').read_text(encoding='utf-8-sig'))
dependency=manifest['dependencies'].get('com.nex.html-to-ugui','')
if not dependency.startswith('file:'):
    parser.error('Validation project must reference this local package via file:')
installed=Path(dependency[5:])
if not installed.is_absolute(): installed=(project/'Packages'/installed).resolve()
for path in source.rglob('*'):
    if path.is_file() and path.suffix!='.meta':
        candidate=installed/path.relative_to(source)
        if not candidate.is_file() or candidate.read_bytes()!=path.read_bytes():
            parser.error('Installed package differs: '+str(path.relative_to(source)))
evidence.mkdir(parents=True,exist_ok=True)
result=evidence/'release-validation.json'
if result.exists():result.unlink()  # A prior success cannot certify this run.
environment=dict(os.environ,HTML_UI_EVIDENCE_DIRECTORY=str(evidence))
run=subprocess.run([args.editor,'-batchmode','-nographics','-projectPath',str(project),
    '-executeMethod','HtmlToUGUI.Editor.UiReleaseValidation.BatchRun','-logFile',str(evidence/'unity.log')],env=environment)
if run.returncode or not result.is_file():raise SystemExit(run.returncode or 1)
report=json.loads(result.read_text(encoding='utf-8-sig'))
if report.get('status')!='passed':raise SystemExit('Unity release checks did not pass')
report['packageFilesSha256']={p.relative_to(source).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in source.rglob('*') if p.is_file()}
result.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('Unity release validation passed: '+str(result))
