"""Repository entry point for the portable game workflow CLI."""
from pathlib import Path
import runpy
import sys

if __name__ == '__main__':
    scripts=Path(__file__).resolve().parents[1]/'Skills/ui-panel-design-to-html/scripts'
    sys.path.insert(0,str(scripts))
    runpy.run_path(str(scripts/'game_workflow.py'),run_name='__main__')
