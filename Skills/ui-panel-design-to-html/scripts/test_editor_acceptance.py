"""Synthetic evidence rejection tests; these never approve a real design."""
from pathlib import Path
import tempfile
import unittest
from PIL import Image
from game_workflow import write,sha
from editor_acceptance import validate_report,validate_editor_acceptance


class EditorAcceptanceTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix='synthetic-editor-evidence-');base=Path(self.temp.name)
        self.root=base/'SyntheticGame';self.project=base/'Project';self.panel=self.root/'Panels/MainPanel';self.evidence=self.root/'design-review/editor'
        write(self.panel/'html/panel.package.json',dict(synthetic=True));write(self.root/'demo.json',dict(synthetic=True))
        self.adapter=self.project/'Assets/Editor/Adapter.cs';self.adapter.parent.mkdir(parents=True);self.adapter.write_text('// SYNTHETIC TEST ONLY',encoding='utf-8')
        self.asset=self.project/'Assets/Output/Main.prefab';self.asset.parent.mkdir();self.asset.write_text('SYNTHETIC TEST ONLY',encoding='utf-8')
        self.evidence.mkdir(parents=True);(self.evidence/'editor-adapter.cs.txt').write_bytes(self.adapter.read_bytes());Image.new('RGB',(1920,1080),'white').save(self.evidence/'actual.png')
        self.report=dict(schemaVersion=1,projectName='SyntheticGame',target='UnityEditor',scope='editor-demo',status='passed',checks=[dict(name='SYNTHETIC TEST ONLY',status='passed')],
            panels=['MainPanel'],screenshots=[dict(panel='MainPanel',step='synthetic',path='actual.png',sha256=sha(self.evidence/'actual.png'))],
            inputFilesSha256={'demo.json':sha(self.root/'demo.json')},adapterPath='Assets/Editor/Adapter.cs',adapterSha256=sha(self.adapter),protectedAssetsSha256={'Assets/Output/Main.prefab':sha(self.asset)})
        write(self.panel/'design/review/unity-acceptance.json',dict(synthetic=True))
        write(self.evidence/'editor-flow.json',self.report)
        self.record=dict(status='passed',scope='editor-demo',target='UnityEditor',evidence='design-review/editor/editor-flow.json',evidenceSha256=sha(self.evidence/'editor-flow.json'),nativeAcceptanceSha256=sha(self.panel/'design/review/unity-acceptance.json'))
        write(self.panel/'design/review/business-acceptance.json',self.record)

    def tearDown(self):self.temp.cleanup()
    def validate(self):validate_report(self.root,self.project,self.report,self.evidence)
    def test_complete_report(self):self.validate();self.assertEqual(validate_editor_acceptance(self.panel,self.project)['status'],'passed')
    def test_stale_adapter(self):
        self.adapter.write_text('changed',encoding='utf-8')
        with self.assertRaisesRegex(ValueError,'adapter is stale'):self.validate()
    def test_stale_input(self):
        write(self.root/'demo.json',dict(changed=True))
        with self.assertRaisesRegex(ValueError,'input changed'):self.validate()
    def test_stale_asset(self):
        self.asset.write_text('changed',encoding='utf-8')
        with self.assertRaisesRegex(ValueError,'protected asset changed'):self.validate()
    def test_incomplete_panels(self):
        self.report['screenshots'][0]['panel']='AnotherPanel'
        with self.assertRaisesRegex(ValueError,'every panel'):self.validate()
    def test_failed_check(self):
        self.report['checks'][0]['status']='failed'
        with self.assertRaisesRegex(ValueError,'all pass'):self.validate()
    def test_bad_screenshot_size_even_with_current_hash(self):
        Image.new('RGB',(10,10),'white').save(self.evidence/'actual.png');self.report['screenshots'][0]['sha256']=sha(self.evidence/'actual.png')
        with self.assertRaisesRegex(ValueError,'dimensions'):self.validate()
    def test_native_acceptance_change(self):
        write(self.panel/'design/review/unity-acceptance.json',dict(changed=True))
        with self.assertRaisesRegex(ValueError,'native acceptance is stale'):validate_editor_acceptance(self.panel,self.project)
    def test_archived_source_change(self):
        (self.evidence/'editor-adapter.cs.txt').write_text('changed',encoding='utf-8')
        with self.assertRaisesRegex(ValueError,'Archived Editor adapter changed'):self.validate()
    def test_runtime_adapter_is_rejected(self):
        self.report['adapterPath']='Assets/Runtime/Adapter.cs'
        with self.assertRaisesRegex(ValueError,'Editor-only'):self.validate()


if __name__=='__main__':unittest.main()
