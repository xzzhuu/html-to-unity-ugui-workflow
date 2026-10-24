"""Regression for project contracts, diagnostics and evidence semantics."""
import importlib.util
import tempfile
import unittest
from pathlib import Path
from game_workflow import initialize,read,write,diagnostics,fingerprints,impact,evidence_state,render_tokens


class GameWorkflowTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.root=Path(self.temp.name)/'SyntheticGame'
        initialize(self.root)

    def tearDown(self):self.temp.cleanup()

    def test_initializer_is_draft_and_preserves_user_files(self):
        p=self.root/'contracts/style.json';data=read(p);data['userExtension']='preserve';write(p,data)
        self.assertFalse(initialize(self.root)['created'])
        self.assertEqual(read(p)['userExtension'],'preserve')
        self.assertFalse(read(self.root/'project.assets.json')['assets'])
        self.assertFalse(list(self.root.rglob('*approval*')))

    def test_collects_multiple_diagnostics(self):
        p=self.root/'contracts/style.json';data=read(p);data['canvas']['width']=True;data['colors']['teal']='wrong';write(p,data)
        errors=diagnostics(self.root)
        self.assertEqual({e['field'] for e in errors},{'canvas.width','colors.teal'})

    def test_unknown_slider_assets_and_boolean_numbers_are_rejected(self):
        p=self.root/'contracts/native-assembly.json';data=read(p)
        data['status']='active'
        data['slider']=dict(trackHeight=True,handleSize=28,trackRadius=5,trackAssetId='missing',handleAssetId='missing',trackColorToken='card',fillColorToken='teal',handleColorToken='text')
        write(p,data);self.assertEqual(len(diagnostics(self.root)),3)

    def test_impact_includes_added_and_deleted_inputs_without_writes(self):
        panel=self.root/'Panels/MainPanel';(panel/'html').mkdir(parents=True)
        p=panel/'html/MainPanel.html';p.write_text('before',encoding='utf-8')
        baseline=self.root/'baseline.json';write(baseline,fingerprints(self.root))
        p.unlink();(panel/'html/new.css').write_text('new',encoding='utf-8')
        plan=impact(self.root,baseline)
        self.assertEqual(plan['changedFiles'],['Panels/MainPanel/html/MainPanel.html','Panels/MainPanel/html/new.css'])
        self.assertEqual(plan['panels'][0]['panelName'],'MainPanel')
        self.assertTrue(plan['readOnly']);self.assertFalse((panel/'design').exists())

    def test_style_change_marks_design_review(self):
        p=self.root/'contracts/screens.json';data=read(p);data['screens']=[dict(panelName='MainPanel',implemented=True)];write(p,data)
        baseline=self.root/'baseline.json';write(baseline,fingerprints(self.root))
        p=self.root/'contracts/style.json';data=read(p);data['colors']['teal']='#FFFFFF';write(p,data)
        self.assertTrue(impact(self.root,baseline)['panels'][0]['designReviewRequired'])

    def test_editor_demo_json_is_an_input_to_impact(self):
        write(self.root/'contracts/screens.json',dict(screens=[dict(panelName='MainPanel',implemented=True)]))
        source=self.root/'design-tools/editor-demo-data.json';write(source,dict(synthetic=True,credits=1))
        baseline=self.root/'baseline.json';write(baseline,fingerprints(self.root));write(source,dict(synthetic=True,credits=2))
        plan=impact(self.root,baseline)
        self.assertEqual(plan['changedFiles'],['design-tools/editor-demo-data.json'])
        self.assertEqual([p['panelName'] for p in plan['panels']],['MainPanel'])

    def test_missing_failed_and_stale_evidence_are_distinct(self):
        p=self.root/'Panels/MainPanel';self.assertEqual(evidence_state(p)['status'],'not-checked')
        write(p/'design/review/unity-acceptance.json',dict(status='failed'))
        self.assertEqual(evidence_state(p)['status'],'failed')
        write(p/'design/review/unity-acceptance.json',dict(status='passed'))
        self.assertEqual(evidence_state(p)['status'],'stale')

    def test_tokens_require_known_scalar_fields(self):
        self.assertEqual(render_tokens('color:{{colors.teal}};n:{{n}}',dict(colors=dict(teal='#123456'),n=8)),'color:#123456;n:8')
        with self.assertRaisesRegex(ValueError,'Unknown style token'):render_tokens('{{colors.missing}}',dict(colors={}))

    def test_canonical_type_roles_follow_style_without_changing_custom_sizes(self):
        from game_workflow import font_size
        self.assertEqual(font_size(24,dict(typography=dict(body=26))),26)
        self.assertEqual(font_size(22,dict(typography=dict(body=26))),22)

    def test_project_name_and_traversal_are_rejected(self):
        with self.assertRaises(ValueError):initialize(self.root,'Other')
        from game_workflow import inside
        with self.assertRaises(ValueError):inside(self.root,'../outside.json',False)

    def test_malformed_shapes_return_diagnostics_instead_of_crashing(self):
        for file,value in [('screens',{'screens':[{'panelName':{},'fixtures':[{}]}]}),('style',{'colors':[],'canvas':True}),('native-assembly',{'slider':[],'rounding':'false'})]:
            with self.subTest(file=file):
                path=self.root/'contracts'/f'{file}.json';before=path.read_bytes();write(path,value)
                self.assertTrue(diagnostics(self.root));path.write_bytes(before)

    def test_invalid_registry_is_reported_for_every_contract_shape(self):
        path=self.root/'project.assets.json'
        for value in ([],{'assets':[None]},{'assets':None}):
            with self.subTest(value=value):write(path,value);self.assertTrue(diagnostics(self.root))

    def test_source_pixel_change_is_included_in_impact(self):
        path=self.root/'shared-assets/track.png';path.parent.mkdir();path.write_bytes(b'old source')
        write(self.root/'project.assets.json',dict(schemaVersion=1,assets=[dict(assetId='Track',source='shared-assets/track.png')]))
        for name,key in [('FirstPanel','Track'),('OtherPanel','Other')]:
            write(self.root/'Panels'/name/'html/sprites.json',dict(sprites=[dict(path='asset:'+key)]))
        baseline=self.root/'baseline.json';write(baseline,fingerprints(self.root));path.write_bytes(b'new source')
        plan=impact(self.root,baseline)
        self.assertEqual(plan['changedAssets'],['track']);self.assertTrue(plan['assetSyncRequired'])
        self.assertEqual([p['panelName'] for p in plan['panels']],['FirstPanel'])

    def test_stale_evidence_does_not_count_as_tested_coverage(self):
        panel=self.root/'Panels/MainPanel';write(panel/'.html-directory.json',dict(panelName='MainPanel'))
        write(panel/'design/review/unity-acceptance.json',dict(status='passed',testedFixtures=['Default']))
        data=read(self.root/'contracts/screens.json');data['screens']=[dict(panelName='MainPanel',entryPoints=[],actions=[],states=[],fixtures=['Default'],tests=[],implemented=True)];write(self.root/'contracts/screens.json',data)
        from game_workflow import status
        result=status(self.root)['panels'][0]
        self.assertEqual(result['fixtureCoverage']['nativeTested'],[])
        self.assertEqual(result['fixtureCoverage']['nativePending'],['Default'])

    def test_shared_native_contract_cases(self):
        import copy
        from PIL import Image
        from game_workflow import sha
        image=self.root/'surface.png';Image.new('RGBA',(64,64),'white').save(image)
        defaults=read(self.root/'contracts/native-assembly.json')
        defaults.update(status='active',slider=dict(trackHeight=10,handleSize=28,trackRadius=5,trackAssetId='Track',handleAssetId='Handle',trackColorToken='card',fillColorToken='teal',handleColorToken='text'))
        assets={name:dict(assetId=name,componentType='Slider',part=name,approved=True,source='surface.png',sourceSha256=sha(image),approvalVersion='SYNTHETIC TEST ONLY') for name in ('Track','Handle')}
        baseline=dict(config=defaults,style=read(self.root/'contracts/style.json'),assets=assets)
        fixture=Path(__file__).parents[1]/'references/fixtures/native-assembly-cases.json'
        for case in read(fixture)['cases']:
            with self.subTest(case=case['id']):
                data=copy.deepcopy(baseline)
                for path,value in case['changes'].items():
                    parts=path.split('.');target=data
                    for key in parts[:-1]:target=target[key]
                    target[parts[-1]]=value
                write(self.root/'contracts/native-assembly.json',data['config']);write(self.root/'contracts/style.json',data['style'])
                write(self.root/'project.assets.json',dict(schemaVersion=1,assets=list(data['assets'].values())))
                self.assertEqual(bool(diagnostics(self.root)),case['expected']=='rejected')
        package=Path(__file__).parents[3]/'Packages/com.nex.html-to-ugui/Editor/Core/NativeAssemblyContractCases.json'
        if package.exists():self.assertEqual(fixture.read_bytes(),package.read_bytes())


class GeometryEvidenceTests(unittest.TestCase):
    def test_duplicate_keys_and_invalid_numbers_fail(self):
        spec=importlib.util.spec_from_file_location('geometry_compare',Path(__file__).with_name('compare-layout.py'))
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        node=dict(key='same',x=0,y=0,width=10,height=10)
        browser=dict(width=100,height=100,bindings=[node,node])
        unity=dict(width=100,height=100,bindings=[node])
        self.assertEqual(module.compare(browser,unity)['status'],'fail')
        with self.assertRaises(ValueError):module.compare(browser,unity,float('nan'))


if __name__=='__main__':unittest.main()
