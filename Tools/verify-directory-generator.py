"""Exercise idempotence and existing-content preservation in independent directories."""
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import sys
sys.dont_write_bytecode=True

root=Path(__file__).resolve().parents[1]
(root/'Audit/Runs/latest').mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(root/'Skills/ui-panel-design-to-html/scripts'))
def load(name, relative):
    spec=importlib.util.spec_from_file_location(name,root/relative)
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module
initializer=load('initializer','Skills/ui-panel-design-to-html/scripts/init_directory.py')
validator=load('validator','Skills/ui-panel-design-to-html/scripts/validate_directory.py')
checks=[]
with tempfile.TemporaryDirectory(prefix='html-skill-') as temporary:
    target=Path(temporary)/'新文件目录'
    initializer.initialize(target)
    checks.append(validator.validate(target)['components']==5)
    entry=target/'html/MainPanel.html'
    entry.write_text(entry.read_text(encoding='utf-8').replace('初始化草稿：','用户修改：'),encoding='utf-8')
    before={str(p.relative_to(target)):hashlib.sha256(p.read_bytes()).hexdigest() for p in target.rglob('*') if p.is_file()}
    result=initializer.initialize(target)
    after={str(p.relative_to(target)):hashlib.sha256(p.read_bytes()).hexdigest() for p in target.rglob('*') if p.is_file()}
    checks.append(before==after and not result['created'])
    try:
        initializer.initialize(target,panel='OtherPanel')
    except ValueError:
        checks.append(True)
    else:
        checks.append(False)
    other=Path(temporary)/'inventory'
    initializer.initialize(other,title='A & B <demo>')
    checks.append(validator.validate(other)['components']==5)
    try:
        initializer.initialize(Path(temporary)/'invalid-size',width=0)
    except ValueError:
        checks.append(True)
    else:
        checks.append(False)
    state_path=other/'.html-directory.json'
    state=json.loads(state_path.read_text(encoding='utf-8'))
    state['status']='ready'
    state_path.write_text(json.dumps(state),encoding='utf-8')
    try:
        validator.validate(other)
    except (ValueError,FileNotFoundError):
        checks.append(True)
    else:
        checks.append(False)
assert all(checks)
report={'checks':['Chinese directory defaults to MainPanel','Repeated invocation preserves edited HTML and all file hashes','Conflicting configuration rejected','English directory inference and XHTML text escaping','Explicit zero dimension rejected','Ready without real approval evidence rejected'], 'passed':len(checks)}
(root/'Audit/Runs/latest/directory-generator-regression.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
