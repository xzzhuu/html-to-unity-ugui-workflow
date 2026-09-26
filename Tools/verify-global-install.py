"""Verify global initialization from an unrelated working directory."""
from pathlib import Path
import json, subprocess, sys, tempfile
root=Path(__file__).resolve().parents[1]
(root/'Audit/Runs/latest').mkdir(parents=True,exist_ok=True)
scripts=Path.home()/'.codex/skills/ui-panel-design-to-html/scripts'
with tempfile.TemporaryDirectory(prefix='HtmlSkillGlobalCheck-') as temp:
    base=Path(temp)
    subprocess.run([sys.executable,'-B','-X','utf8',str(scripts/'init_directory.py'),'--root',str(base/'Inventory'),'--panel-name','InventoryPanel'],cwd=base,check=True,capture_output=True)
    result=subprocess.run([sys.executable,'-B','-X','utf8',str(scripts/'validate_directory.py'),'--root',str(base/'Inventory')],cwd=base,check=True,capture_output=True,text=True,encoding='utf-8')
    report={'workingDirectoryIndependent':True,'installedSkill':str(scripts.parent),'result':json.loads(result.stdout)}
    (root/'Audit/Runs/latest/global-initialization.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('Global skill initializes and validates outside the workspace')
