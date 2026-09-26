"""Build the separate UPM tarball and verify every delivered file hash."""
import argparse, hashlib, json, tarfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--package',type=Path,default=root/'Packages/com.nexvr.html-to-ugui')
parser.add_argument('--output',type=Path,default=root/'dist')
args=parser.parse_args()
package=args.package.resolve()
manifest=json.loads((package/'package.json').read_text(encoding='utf-8'))
files=[p for p in package.rglob('*') if p.is_file() and not any(part in ('__pycache__','.git','Library','Temp') for part in p.relative_to(package).parts)]
args.output.mkdir(parents=True,exist_ok=True)
target=args.output/(manifest['name']+'-'+manifest['version']+'.tgz')
with tarfile.open(target,'w:gz') as archive:
    for path in sorted(files): archive.add(path,arcname='package/'+path.relative_to(package).as_posix(),recursive=False)
with tarfile.open(target,'r:gz') as archive:
    members=archive.getmembers()
    assert all(member.isfile() and member.name.startswith('package/') and '..' not in member.name.split('/') for member in members)
    assert len(members)==len(files)
    for member in members:
        assert archive.extractfile(member).read()==(package/member.name.removeprefix('package/')).read_bytes(),member.name
report={'package':manifest['name'],'version':manifest['version'],'tarball':str(target),'files':len(files),'allFileBytesMatched':True,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
(args.output/(target.name+'.verification.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
