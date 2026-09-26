"""Install owned skill files, excluding caches; prune only previously managed files."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil

ROOT=Path(__file__).resolve().parents[1]
NAMES=('ui-panel-design-to-html',)
RETIRED_NAMES=('html-directory-generator','unity-ui-pipeline','unity-ui-panel-workflow')
INDEX='.codex-install-files.json'
IGNORED={'__pycache__','.git','.DS_Store'}

def eligible(path):
    return not any(part in IGNORED for part in path.parts) and path.suffix not in ('.pyc','.pyo','.tmp') and path.name!=INDEX

def install(source,destination,check=False):
    source,destination=Path(source).resolve(),Path(destination).resolve()
    if source==destination: raise ValueError('Install source cannot equal destination')
    desired={p.relative_to(source).as_posix():p for p in source.rglob('*') if p.is_file() and eligible(p.relative_to(source))}
    previous=json.loads((destination/INDEX).read_text(encoding='utf-8')).get('files',{}) if (destination/INDEX).exists() else {}
    missing=[name for name,path in desired.items() if not (destination/name).is_file() or path.read_bytes()!=(destination/name).read_bytes()]
    stale=[name for name in previous if name not in desired and (destination/name).exists()]
    caches=[p for p in destination.rglob('*') if p.is_file() and not eligible(p.relative_to(destination)) and p.name!=INDEX] if destination.exists() else []
    if check:
        return dict(name=source.name,passed=not(missing or stale or caches),different=missing,stale=stale,caches=len(caches))
    destination.mkdir(parents=True,exist_ok=True)
    for name in stale:
        target=(destination/name).resolve()
        if not target.is_relative_to(destination): raise ValueError('Managed path outside installed skill')
        target.unlink()
    for path in caches: path.unlink()
    for name,path in desired.items():
        target=destination/name
        if not target.resolve().is_relative_to(destination): raise ValueError('Installed target escapes skill')
        target.parent.mkdir(parents=True,exist_ok=True)
        if name in missing: shutil.copyfile(path,target)
    for folder in sorted(destination.rglob('*'),key=lambda p:len(p.parts),reverse=True):
        if folder.is_dir() and not any(folder.iterdir()): folder.rmdir()
    (destination/INDEX).write_text(json.dumps({'schemaVersion':1,'files':{name:hashlib.sha256(path.read_bytes()).hexdigest() for name,path in desired.items()}},indent=2)+'\n',encoding='utf-8')
    return dict(name=source.name,destination=str(destination),updated=len(missing),removed=len(stale)+len(caches))

def retire_managed_skill(destination,name,check=False):
    folder=(Path(destination)/name).resolve()
    if not folder.is_dir(): return dict(name=name,present=False,removed=0)
    index=folder/INDEX
    if not index.is_file():
        return dict(name=name,present=True,unmanaged=True,active=(folder/'SKILL.md').is_file(),removed=0)
    previous=json.loads(index.read_text(encoding='utf-8')).get('files',{})
    managed=[]
    for relative in previous:
        target=(folder/relative).resolve()
        if target.is_relative_to(folder) and target.is_file(): managed.append(target)
    active=(folder/'SKILL.md').is_file()
    if check:
        return dict(name=name,present=True,unmanaged=False,active=active,installationIndexRemain=True,managedFilesRemain=len(managed),removed=0)
    for target in managed: target.unlink()
    index.unlink()
    for child in sorted(folder.rglob('*'),key=lambda item:len(item.parts),reverse=True):
        if child.is_dir() and not child.is_symlink() and not any(child.iterdir()): child.rmdir()
    if not any(folder.iterdir()): folder.rmdir()
    return dict(name=name,present=True,unmanaged=False,active=False,installationIndexRemain=False,managedFilesRemain=0,removed=len(managed)+1,remainingDirectory=folder.exists())

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check',action='store_true')
    parser.add_argument('--destination',type=Path,default=Path(os.environ.get('CODEX_HOME',str(Path.home()/'.codex')))/'skills')
    args=parser.parse_args()
    installed=[install(ROOT/'Skills'/name,args.destination/name,args.check) for name in NAMES]
    if args.check:
        retired=[retire_managed_skill(args.destination,name,check=True) for name in RETIRED_NAMES]
        clean=all(not item.get('active') and not item.get('installationIndexRemain',False) and item.get('managedFilesRemain',0)==0 for item in retired)
        print(json.dumps({'installed':installed,'retiredSkills':retired},ensure_ascii=False,indent=2))
        if not all(report['passed'] for report in installed) or not clean: raise SystemExit(1)
    else:
        retired=[retire_managed_skill(args.destination,name) for name in RETIRED_NAMES]
        print(json.dumps({'installed':installed,'retiredSkills':retired},ensure_ascii=False,indent=2))
