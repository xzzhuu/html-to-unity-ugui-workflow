"""Synthetic matrix records exercise provenance rejection; no user approvals are created."""
import copy
from pathlib import Path
import tempfile
import unittest
from PIL import Image
from game_workflow import write,sha
from validate_directory import validate_native_matrix


class NativeMatrixEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix='synthetic-native-evidence-')
        self.game=Path(self.temp.name)/'SyntheticGame';self.panel=self.game/'Panels/MainPanel'
        write(self.game/'project.assets.json',dict(schemaVersion=1,assets=[]))
        write(self.game/'contracts/screens.json',dict(screens=[dict(panelName='MainPanel',fixtures=['Default'])]))
        html=self.panel/'html/MainPanel.html';html.parent.mkdir(parents=True);html.write_text('SYNTHETIC TEST INPUT',encoding='utf-8')
        self.directory=self.panel/'design/review/unity';self.directory.mkdir(parents=True)
        self.relative=lambda file:'design/review/unity/'+file
        self.inputs={p.relative_to(self.game).as_posix():sha(p) for p in [self.game/'project.assets.json',self.game/'contracts/screens.json']}
        node=dict(path='',row=-1,x=0,y=0,width=100,height=100,visible=True)
        self.source=dict(panel='MainPanel',fixture='Default',width=100,height=100,nodes=[node],sourceFilesSha256={'html/MainPanel.html':sha(html)},gameInputsSha256=self.inputs)
        write(self.directory/'browser.json',self.source)
        Image.new('RGB',(100,100),'white').save(self.directory/'actual.png')
        artifact=dict(path='Assets/Output/MainPanel.prefab',guid='a'*32,sha256='b'*64,metaSha256='c'*64)
        self.audit=dict(schemaVersion=2,status='passed',fixture='Default',width=100,height=100,viewportWidth=100,viewportHeight=100,
            nativeControls=['SYNTHETIC TEST ONLY'],prefab=artifact['path'],prefabSha256=artifact['sha256'],prefabMetaSha256=artifact['metaSha256'],
            referenceSha256=sha(self.directory/'browser.json'),screenshotSha256=sha(self.directory/'actual.png'),artifacts=[artifact],nodes=[copy.deepcopy(node)],issues=[])
        write(self.directory/'audit.json',self.audit);write(self.directory/'geometry.json',dict(status='pass'))
        self.case=dict(fixture='Default',viewport='100x100',reference=self.relative('browser.json'),audit=self.relative('audit.json'),screenshot=self.relative('actual.png'),geometry=self.relative('geometry.json'))
        self.acceptance=dict(gameInputsSha256=self.inputs,matrixCases=[self.case],testedFixtures=['Default'],testedResolutions=[dict(width=100,height=100)],
            finalArtifacts=[artifact],externalDependencies=[],screenshots=[self.relative('actual.png')],evidenceFiles=[self.relative(file) for file in ['browser.json','audit.json','geometry.json']])

    def tearDown(self):self.temp.cleanup()
    def check(self):validate_native_matrix(self.panel,self.game,self.acceptance)

    def test_complete_consistent_matrix_passes(self):self.check()

    def test_missing_viewport_is_not_complete(self):
        self.acceptance['testedResolutions'].append(dict(width=200,height=100))
        with self.assertRaisesRegex(ValueError,'every fixture and viewport'):self.check()

    def test_duplicate_case_is_rejected(self):
        self.acceptance['matrixCases'].append(copy.deepcopy(self.case))
        with self.assertRaisesRegex(ValueError,'duplicate'):self.check()

    def test_stale_game_input_is_rejected(self):
        write(self.game/'project.assets.json',dict(schemaVersion=1,assets=[],changed=True))
        with self.assertRaisesRegex(ValueError,'game input changed'):self.check()

    def test_browser_source_change_is_rejected(self):
        (self.panel/'html/MainPanel.html').write_text('changed',encoding='utf-8')
        with self.assertRaisesRegex(ValueError,'browser source is stale'):self.check()

    def test_wrong_prefab_bytes_are_rejected(self):
        self.audit['prefabSha256']='d'*64;write(self.directory/'audit.json',self.audit)
        with self.assertRaisesRegex(ValueError,'prefab bytes are stale'):self.check()

    def test_wrong_screenshot_is_rejected_even_if_its_hash_is_updated(self):
        Image.new('RGB',(200,100),'white').save(self.directory/'actual.png')
        self.audit['screenshotSha256']=sha(self.directory/'actual.png');write(self.directory/'audit.json',self.audit)
        with self.assertRaisesRegex(ValueError,'viewport mismatch'):self.check()

    def test_dependency_omission_is_rejected(self):
        self.audit['artifacts']=[];write(self.directory/'audit.json',self.audit)
        with self.assertRaisesRegex(ValueError,'dependency coverage mismatch'):self.check()

    def test_geometry_is_recomputed_instead_of_trusting_pass_label(self):
        self.audit['nodes'][0]['x']=20;write(self.directory/'audit.json',self.audit)
        with self.assertRaisesRegex(ValueError,'geometry no longer matches'):self.check()


if __name__=='__main__':unittest.main()
