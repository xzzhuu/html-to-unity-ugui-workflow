"""Temporary synthetic fixtures verify routing, preservation and approval freshness.

These fixtures are not user approvals or evidence for any actual UI project.
"""
import json
from pathlib import Path
import struct
import tempfile
import unittest
import zlib

from init_directory import initialize
from validate_directory import validate, definition_digest, file_digest, validate_content_rect
from project_asset_contract import load_project_assets


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')


def png(path, color=255, width=4, height=4):
    def chunk(kind, data):
        return struct.pack('>I', len(data))+kind+data+struct.pack('>I', zlib.crc32(kind+data))
    data=b'\x89PNG\r\n\x1a\n'
    data+=chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0))
    data+=chunk(b'IDAT', zlib.compress((b'\0'+bytes([color,color,color,255])*width)*height))
    data+=chunk(b'IEND', b'')
    path.write_bytes(data)


class WorkflowTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix='ui-skill-tests-')
        self.root=Path(self.temp.name)/'Settings'

    def tearDown(self):
        self.temp.cleanup()

    def test_initialize_game_panels_resolves_shared_project(self):
        import xml.etree.ElementTree as ET
        game = Path(self.temp.name) / 'TestGame'
        game.mkdir()
        write_json(game / 'project.assets.json', {'schemaVersion': 1, 'assets': []})
        panel = game / 'Panels' / 'SettingsPanel'
        initialize(panel)
        package_path = panel / 'html/panel.package.json'
        package = json.loads(package_path.read_text(encoding='utf-8'))
        self.assertEqual(package['projectName'], 'TestGame')
        self.assertEqual(package['projectDirectory'], '../../..')
        self.assertEqual(package['stagingRoot'], 'Assets/HtmlToUGUIStage/TestGame/SettingsPanel')
        source = next(n for n in ET.parse(panel / 'html/SettingsPanel.html').iter() if n.get('data-root') == 'true')
        self.assertEqual(source.get('data-project-name'), package['projectName'])
        self.assertEqual(source.get('data-project-directory'), package['projectDirectory'])
        self.assertEqual(source.get('data-staging-root'), package['stagingRoot'])
        validate(panel)
        before = package_path.read_bytes()
        initialize(panel)
        self.assertEqual(before, package_path.read_bytes())

    def test_standalone_html_and_package_share_staging_root(self):
        import xml.etree.ElementTree as ET
        initialize(self.root)
        package = json.loads((self.root/'html/panel.package.json').read_text(encoding='utf-8'))
        source = next(n for n in ET.parse(self.root/'html/SettingsPanel.html').iter() if n.get('data-root') == 'true')
        self.assertEqual(source.get('data-staging-root'), package['stagingRoot'])

    def test_checkout_without_empty_scaffolding_keeps_real_requirements(self):
        initialize(self.root)
        for directory in ('inputs/references','html/assets/icons','html/assets/sprites','html/assets/textures'):
            (self.root/directory).rmdir()
        validate(self.root)
        (self.root/'design/review').rmdir()
        with self.assertRaisesRegex(ValueError, 'Missing directory: design/review'):
            validate(self.root)

    def test_validation_rejects_inconsistent_import_routes(self):
        initialize(self.root)
        package_path = self.root/'html/panel.package.json'
        package = json.loads(package_path.read_text(encoding='utf-8'))
        for changes in ({'stagingRoot':'Assets/Other'}, {'projectName':'WrongGame'}, {'projectDirectory':'..'}):
            with self.subTest(changes=changes):
                write_json(package_path, dict(package, **changes))
                with self.assertRaisesRegex(ValueError, 'routing|projectDirectory'):
                    validate(self.root)

    def test_invalid_game_name_leaves_no_partial_workspace(self):
        game = Path(self.temp.name)/'Invalid Game'
        game.mkdir()
        write_json(game/'project.assets.json', {'schemaVersion':1, 'assets':[]})
        panel = game/'Panels'/'SettingsPanel'
        with self.assertRaises(ValueError):
            initialize(panel)
        self.assertFalse(panel.exists())

    def fixture(self, mode='visual-only'):
        initialize(self.root, panel='SettingsPanel', mode=mode)
        png(self.root/'design/review/layout-approved.png')
        write_json(self.root/'design/review/approval.json', {
            'status':'approved', 'imageSha256':file_digest(self.root/'design/review/layout-approved.png'),
            'userApproval':'SYNTHETIC TEST ONLY: panel approval'})
        kind='Button' if mode=='visual-only' else 'Text'
        prefix='Btn' if kind=='Button' else 'Txt'
        group={'groupId':'Primary', 'componentType':kind, 'purpose':'Synthetic test group',
               'usedBy':['SettingsPanel/Btn_Confirm' if kind=='Button' else 'SettingsPanel/Group_Header/Txt_Title'],
               'sheet':f'design/element-groups/{prefix}_Primary_Sheet.png', 'assets':[]}
        png(self.root/group['sheet'])
        sprites=[]
        if mode=='visual-only':
            source='assets/sprites/Btn_Primary_Surface.png'
            png(self.root/source)
            group['assets']=[source]
            sprites=[{'source':source, 'componentType':'Button', 'pixelSize':[4,4],
                      'originalPixelSize':[4,4], 'hasAlpha':True, 'border':[1,1,1,1],
                      'tintable':True, 'purpose':'Shared neutral surface'}]
        manifest={'schemaVersion':1,'panelName':'SettingsPanel','requiredComponents':[kind],
                  'groups':[group],'sprites':sprites}
        write_json(self.root/'design/element-groups.json',manifest)
        approval={'groupId':group['groupId'], 'imageSha256':file_digest(self.root/group['sheet']),
                  'definitionSha256':definition_digest(group,{s['source']:s for s in sprites}),
                  'filesSha256':{s['source']:file_digest(self.root/s['source']) for s in sprites},
                  'userApproval':'SYNTHETIC TEST ONLY: element approval'}
        write_json(self.root/'design/review/element-groups-approval.json',{'status':'approved','groups':[approval]})
        state=json.loads((self.root/'.html-directory.json').read_text(encoding='utf-8'))
        state['status']='ready'
        write_json(self.root/'.html-directory.json',state)
        acceptance={'status':'passed',
                    'approvedLayoutSha256':file_digest(self.root/'design/review/layout-approved.png'),
                    'approvedElementGroupsSha256':file_digest(self.root/'design/review/element-groups-approval.json')}
        if mode=='full':
            elements=json.loads((self.root/'html/elements.json').read_text(encoding='utf-8'))
            elements['status']='ready'
            write_json(self.root/'html/elements.json',elements)
            acceptance['checks']={key:'passed' for key in ('elements','naming','sprites','layout','visual','interactions')}
            files=[p.relative_to(self.root).as_posix() for p in (self.root/'html').rglob('*') if p.is_file()]
            report='html-acceptance.json'
        else:
            acceptance['checks']={key:'passed' for key in ('structure','visual','interactions','references','resolution')}
            shot='design/review/Unity_Test.png'
            png(self.root/shot)
            acceptance['screenshots']=[shot]
            acceptance['referenceImages']=[{'source':source,'unityPath':'Assets/Art/UI/DesignReferences/'+Path(source).name,'sha256':file_digest(self.root/source)} for source in ['design/review/layout-approved.png',group['sheet']]]
            files=[shot]+[s['source'] for s in sprites]
            acceptance['finalArtifacts']=[dict(path='Assets/Output/SettingsPanel.prefab',guid='a'*32,sha256='b'*64,metaSha256='c'*64)]
            report='unity-acceptance.json'
        files.append('design/element-groups.json')
        acceptance['filesSha256']={source:file_digest(self.root/source) for source in files}
        write_json(self.root/'design/review'/report,acceptance)
        return manifest

    def test_ready_requires_skill_gate_and_staging(self):
        self.fixture('full')
        path=self.root/'html/panel.package.json'
        original=json.loads(path.read_text(encoding='utf-8'))
        for key,value in [('requireSkillApproval',False),('stagingRoot',None),('stagingRoot','Assets/../Formal')]:
            package=dict(original);package[key]=value;write_json(path,package)
            with self.assertRaisesRegex(ValueError,'requires'):
                validate(self.root,True)

    def test_each_additional_preview_requires_its_own_approval(self):
        manifest=self.fixture('full')
        source='design/review/SecondPanel.png';png(self.root/source)
        manifest['panelPreviews']=['design/review/layout-approved.png',source]
        write_json(self.root/'design/element-groups.json',manifest)
        with self.assertRaisesRegex(ValueError,'individual preview approval'):
            validate(self.root,True)
        path=self.root/'design/review/approval.json'
        record=json.loads(path.read_text(encoding='utf-8'))
        record['previews']=[dict(source=source,imageSha256=file_digest(self.root/source),userApproval='SYNTHETIC TEST ONLY')]
        write_json(path,record)
        acceptance_path=self.root/'design/review/html-acceptance.json'
        acceptance=json.loads(acceptance_path.read_text(encoding='utf-8'))
        acceptance['filesSha256']['design/element-groups.json']=file_digest(self.root/'design/element-groups.json')
        write_json(acceptance_path,acceptance)
        self.assertTrue(validate(self.root,True)['designApproved'])
        png(self.root/source,128)
        with self.assertRaisesRegex(ValueError,'individual preview approval'):
            validate(self.root,True)

    def test_final_prefab_evidence_is_required(self):
        self.fixture()
        path=self.root/'design/review/unity-acceptance.json'
        acceptance=json.loads(path.read_text(encoding='utf-8'));acceptance.pop('finalArtifacts')
        write_json(path,acceptance)
        with self.assertRaisesRegex(ValueError,'final business Prefab'):
            validate(self.root,True)

    def test_device_result_requires_current_unity_evidence(self):
        self.fixture()
        result=validate(self.root,True)
        self.assertTrue(result['designApproved'] and result['artifactReady'] and result['unityAccepted'])
        self.assertFalse(result['deviceAccepted'])
        shot='design/review/Device.png';png(self.root/shot)
        path=self.root/'design/review/device-acceptance.json'
        write_json(path,dict(status='passed',device='Synthetic headset',testedAt='2026-10-22',reviewer='SYNTHETIC TEST ONLY',
            unityAcceptanceSha256=file_digest(self.root/'design/review/unity-acceptance.json'),screenshots=[shot],filesSha256={shot:file_digest(self.root/shot)}))
        self.assertTrue(validate(self.root,True)['deviceAccepted'])
        (self.root/'design/review/unity-acceptance.json').write_text((self.root/'design/review/unity-acceptance.json').read_text(encoding='utf-8')+' ',encoding='utf-8')
        with self.assertRaisesRegex(ValueError,'Device acceptance stale'):
            validate(self.root,True)

    def test_legacy_ready_workspace_omitted_mode_is_full(self):
        self.fixture('full')
        path=self.root/'.html-directory.json';state=json.loads(path.read_text(encoding='utf-8'));state.pop('mode');write_json(path,state)
        self.assertEqual(validate(self.root,True)['mode'],'full')

    def test_visual_material_readiness_is_separate_from_unity_acceptance(self):
        self.fixture()
        path=self.root/'.html-directory.json';state=json.loads(path.read_text(encoding='utf-8'));state['status']='draft-unapproved';write_json(path,state)
        (self.root/'design/review/unity-acceptance.json').unlink()
        result=validate(self.root)
        self.assertTrue(result['designApproved'] and result['artifactReady'])
        self.assertFalse(result['unityAccepted'] or result['deviceAccepted'])

    def test_defaults_and_idempotency(self):
        result=initialize(self.root)
        before={p.relative_to(self.root):p.read_bytes() for p in self.root.rglob('*') if p.is_file()}
        self.assertEqual(result['mode'],'full')
        state=json.loads(before[Path('.html-directory.json')])
        self.assertEqual((state['width'],state['height']),(1920,1080))
        package=json.loads(before[Path('html/panel.package.json')])
        self.assertEqual(package['stagingRoot'],'Assets/HtmlToUGUIStage/'+state['panelName'])
        self.assertFalse(initialize(self.root)['created'])
        self.assertEqual(before,{p.relative_to(self.root):p.read_bytes() for p in self.root.rglob('*') if p.is_file()})
        self.assertEqual(validate(self.root)['structure'],'passed')

    def test_visual_draft_has_no_html_or_scripts(self):
        initialize(self.root,mode='visual-only')
        self.assertFalse((self.root/'html').exists())
        self.assertFalse(list(self.root.rglob('*.cs')))
        self.assertIsNone(validate(self.root)['entry'])
        with self.assertRaises(ValueError):
            initialize(self.root,mode='full')
        self.assertFalse((self.root/'html').exists())

    def test_legacy_dimensions_preserved(self):
        initialize(self.root,width=1280,height=720)
        state=json.loads((self.root/'.html-directory.json').read_text(encoding='utf-8'))
        state.pop('mode')
        write_json(self.root/'.html-directory.json',state)
        initialize(self.root)
        self.assertEqual(validate(self.root)['mode'],'full')
        self.assertEqual(json.loads((self.root/'.html-directory.json').read_text(encoding='utf-8'))['width'],1280)
        with self.assertRaises(ValueError):
            initialize(self.root,width=1920,height=1080)

    def test_visual_ready_without_html(self):
        self.fixture()
        self.assertEqual(validate(self.root,True)['structure'],'passed')
        self.assertFalse((self.root/'html').exists())

    def test_full_ready_requires_element_confirmation(self):
        self.fixture('full')
        self.assertEqual(validate(self.root,True)['structure'],'passed')
        (self.root/'design/review/element-groups-approval.json').unlink()
        with self.assertRaises(OSError):
            validate(self.root,True)

    def test_layout_sheet_asset_and_definition_changes_reject(self):
        manifest=self.fixture()
        paths=['design/review/layout-approved.png',manifest['groups'][0]['sheet'],manifest['sprites'][0]['source']]
        for source in paths:
            with self.subTest(source=source):
                original=(self.root/source).read_bytes()
                png(self.root/source,128)
                with self.assertRaises(ValueError):
                    validate(self.root,True)
                (self.root/source).write_bytes(original)
        manifest['groups'][0]['purpose']='Changed definition'
        write_json(self.root/'design/element-groups.json',manifest)
        with self.assertRaises(ValueError):
            validate(self.root,True)

    def test_unrequested_types_prefix_and_downsampling_reject(self):
        manifest=self.fixture()
        group=manifest['groups'][0]
        group['componentType']='Toggle'
        write_json(self.root/'design/element-groups.json',manifest)
        with self.assertRaisesRegex(ValueError,'Unrequested'):
            validate(self.root)
        group['componentType']='Button'
        sprite=manifest['sprites'][0]
        old=sprite['source']
        new='assets/sprites/Surface.png'
        (self.root/old).rename(self.root/new)
        sprite['source']=new
        group['assets']=[new]
        write_json(self.root/'design/element-groups.json',manifest)
        with self.assertRaisesRegex(ValueError,'abbreviation prefix'):
            validate(self.root)
        (self.root/new).rename(self.root/old)
        sprite['source']=old
        group['assets']=[old]
        sprite['originalPixelSize']=[8,8]
        write_json(self.root/'design/element-groups.json',manifest)
        with self.assertRaisesRegex(ValueError,'downsampling'):
            validate(self.root)

    def test_html_cannot_replace_unity_evidence(self):
        self.fixture()
        report=self.root/'design/review/unity-acceptance.json'
        report.rename(self.root/'design/review/html-acceptance.json')
        with self.assertRaises(OSError):
            validate(self.root,True)

    def test_mode_mismatch_rejects(self):
        initialize(self.root,mode='visual-only')
        with self.assertRaisesRegex(ValueError,'mode mismatch'):
            validate(self.root,mode='full')

    def test_non_power_of_two_material_rejects(self):
        manifest=self.fixture()
        source=manifest['sprites'][0]['source']
        png(self.root/source,width=6,height=4)
        manifest['sprites'][0].update(pixelSize=[6,4],originalPixelSize=[6,4])
        write_json(self.root/'design/element-groups.json',manifest)
        with self.assertRaisesRegex(ValueError,'powers of two'):
            validate(self.root)

    def test_missing_reference_import_rejects(self):
        self.fixture()
        path=self.root/'design/review/unity-acceptance.json'
        acceptance=json.loads(path.read_text(encoding='utf-8'))
        acceptance.pop('referenceImages')
        write_json(path,acceptance)
        with self.assertRaisesRegex(ValueError,'imported panel/element reference'):
            validate(self.root,True)

    def test_deleted_and_added_html_files_invalidate_acceptance(self):
        self.fixture('full')
        optional=self.root/'html/optional-preview.js'
        optional.write_text('// optional test script',encoding='utf-8')
        report=self.root/'design/review/html-acceptance.json'
        acceptance=json.loads(report.read_text(encoding='utf-8'))
        acceptance['filesSha256']['html/optional-preview.js']=file_digest(optional)
        write_json(report,acceptance)
        validate(self.root,True)
        optional.unlink()
        with self.assertRaisesRegex(ValueError,'file set changed'):
            validate(self.root,True)
        optional.write_text('// optional test script',encoding='utf-8')
        (self.root/'html/unreviewed.js').write_text('// new file',encoding='utf-8')
        with self.assertRaisesRegex(ValueError,'file set changed'):
            validate(self.root,True)

    def test_content_rect_bounds_types_and_unsupported_padding(self):
        manifest=self.fixture()
        for rect in ([-10,999,999,0],[0,0,0,4],[0,0,5,4],[0,0,True,4],[0,0,4.0,4]):
            with self.subTest(rect=rect):
                manifest['sprites'][0]['contentRect']=rect
                write_json(self.root/'design/element-groups.json',manifest)
                with self.assertRaisesRegex(ValueError,'contentRect'):
                    validate(self.root)

    def test_padding_is_only_allowed_for_manual_visual_adaptation(self):
        item={'contentRect':[0,0,3,4]}
        validate_content_rect(item,4,4,allow_padding=True)
        with self.assertRaisesRegex(ValueError,'not supported'):
            validate_content_rect(item,4,4)

    def test_full_texture_content_rect_is_supported(self):
        manifest=self.fixture()
        manifest['sprites'][0]['contentRect']=[0,0,4,4]
        write_json(self.root/'design/element-groups.json',manifest)
        state=json.loads((self.root/'.html-directory.json').read_text(encoding='utf-8'))
        state['status']='draft-unapproved'
        write_json(self.root/'.html-directory.json',state)
        self.assertEqual(validate(self.root)['structure'],'passed')

    def test_full_ready_does_not_imply_unity_completion(self):
        self.fixture('full')
        self.assertEqual(validate(self.root,True)['unityAcceptance'],'not-verified')
        with self.assertRaises(OSError):
            validate(self.root,require_unity=True)

    def test_unity_project_reference_bytes_are_verified(self):
        self.fixture()
        acceptance=json.loads((self.root/'design/review/unity-acceptance.json').read_text(encoding='utf-8'))
        project=self.root.parent/'Unity'
        for item in acceptance['referenceImages']:
            target=project/item['unityPath']
            target.parent.mkdir(parents=True,exist_ok=True)
            target.write_bytes((self.root/item['source']).read_bytes())
        prefab=project/'Assets/Output/SettingsPanel.prefab';prefab.parent.mkdir(parents=True,exist_ok=True)
        prefab.write_text('synthetic final prefab',encoding='utf-8')
        meta=prefab.with_suffix('.prefab.meta');meta.write_text('guid: '+'a'*32+'\n',encoding='utf-8')
        acceptance['finalArtifacts']=[dict(path='Assets/Output/SettingsPanel.prefab',guid='a'*32,sha256=file_digest(prefab),metaSha256=file_digest(meta))]
        write_json(self.root/'design/review/unity-acceptance.json',acceptance)
        validate(self.root,require_unity=True,unity_project=project)
        target.write_bytes(b'changed')
        with self.assertRaisesRegex(ValueError,'reference bytes'):
            validate(self.root,require_unity=True,unity_project=project)

    def test_full_unity_gate_links_report_inputs_and_real_outputs(self):
        manifest=self.fixture('full')
        project=self.root.parent/'Unity'
        shot='design/review/Unity_Test.png'
        png(self.root/shot)
        refs=[]
        for source in ['design/review/layout-approved.png',manifest['groups'][0]['sheet']]:
            target='Assets/References/'+Path(source).name
            output=project/target
            output.parent.mkdir(parents=True,exist_ok=True)
            output.write_bytes((self.root/source).read_bytes())
            refs.append(dict(source=source,unityPath=target,sha256=file_digest(self.root/source)))
        prefab=project/'Assets/Output/SettingsPanel.prefab'
        prefab.parent.mkdir(parents=True,exist_ok=True)
        prefab.write_text('synthetic prefab bytes',encoding='utf-8')
        meta=prefab.with_suffix('.prefab.meta')
        meta.write_text('fileFormatVersion: 2\nguid: '+'a'*32+'\n',encoding='utf-8')
        report_source='design/review/unity-import-report.json'
        write_json(self.root/report_source,dict(converterVersion='0.3.0',
            inputsSha256={path.relative_to(self.root/'html').as_posix():file_digest(path) for path in (self.root/'html').rglob('*') if path.is_file()},
            artifacts=[dict(path='Assets/Output/SettingsPanel.prefab',guid='a'*32,sha256=file_digest(prefab),metaSha256=file_digest(meta))]))
        html_acceptance=json.loads((self.root/'design/review/html-acceptance.json').read_text(encoding='utf-8'))
        write_json(self.root/'design/review/unity-acceptance.json',dict(status='passed',
            approvedLayoutSha256=html_acceptance['approvedLayoutSha256'],approvedElementGroupsSha256=html_acceptance['approvedElementGroupsSha256'],
            checks={key:'passed' for key in ('structure','visual','interactions','references','resolution')},screenshots=[shot],referenceImages=refs,
            importReport=report_source,sourceImportReportSha256=file_digest(self.root/report_source),
            finalArtifacts=[dict(path='Assets/Output/SettingsPanel.prefab',guid='a'*32,sha256=file_digest(prefab),metaSha256=file_digest(meta))],
            filesSha256={source:file_digest(self.root/source) for source in [shot,report_source,'design/element-groups.json']}))
        validate(self.root,require_unity=True,unity_project=project)
        original=prefab.read_bytes()
        prefab.write_text('changed',encoding='utf-8')
        with self.assertRaisesRegex(ValueError,'artifact bytes changed'):
            validate(self.root,require_unity=True,unity_project=project)
        prefab.write_bytes(original)
        meta.write_text('guid: changed\n',encoding='utf-8')
        with self.assertRaisesRegex(ValueError,'artifact meta changed'):
            validate(self.root,require_unity=True,unity_project=project)

    def test_html_native_state_contract_rejects_conflicts(self):
        self.fixture('full')
        entry=self.root/'html/SettingsPanel.html'
        source=entry.read_text(encoding='utf-8')
        entry.write_text(source.replace('data-root="true"','data-root="true" data-selected-color="#FF0000"'),encoding='utf-8')
        with self.assertRaisesRegex(ValueError,'Selectable'):
            validate(self.root)

    def test_project_asset_mapping_preserves_predicted_component_count(self):
        self.fixture('full')
        state_path=self.root/'.html-directory.json'
        state=json.loads(state_path.read_text(encoding='utf-8'));state['status']='draft-unapproved';write_json(state_path,state)
        image=self.root/'html/assets/icons/Img_Primary_Surface.png';png(image)
        package_path=self.root/'html/panel.package.json'
        package=json.loads(package_path.read_text(encoding='utf-8'))
        package.update(projectName='SyntheticGame',projectDirectory='..',stagingRoot='Assets/HtmlToUGUIStage/SyntheticGame/SettingsPanel');write_json(package_path,package)
        write_json(self.root/'project.assets.json',dict(schemaVersion=1,assets=[dict(assetId='Primary',componentType='Image',
            part='Surface',styleId='Primary',state='Default',source='html/assets/icons/Img_Primary_Surface.png',
            sourceSha256=file_digest(image),approved=True,approvalVersion='SYNTHETIC TEST ONLY')]))
        entry=self.root/'html/SettingsPanel.html'
        entry.write_text(entry.read_text(encoding='utf-8').replace('data-root="true"','data-root="true" data-sprite="asset:Primary" data-project-name="SyntheticGame" data-project-directory=".."').replace('Assets/HtmlToUGUIStage/SettingsPanel','Assets/HtmlToUGUIStage/SyntheticGame/SettingsPanel'),encoding='utf-8')
        write_json(self.root/'html/sprites.json',dict(schemaVersion=1,panelName='SettingsPanel',sprites=[dict(
            source='assets/icons/Img_Primary_Surface.png',path='asset:Primary',role='Sprite',border=[0,0,0,0],
            pixelSize=[4,4],hasAlpha=True,purpose='Synthetic approved root surface',usage='source')]))
        result=validate(self.root)
        self.assertIsInstance(result['components'],int)
        self.assertGreater(result['components'],0)


class ProjectAssetTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix='game-assets-tests-')
        self.root=Path(self.temp.name)
        png(self.root/'surface.png')
        self.item=dict(assetId='Primary',componentType='Button',part='Surface',styleId='Primary',state='Normal',
                       source='surface.png',sourceSha256=file_digest(self.root/'surface.png'),approved=True,approvalVersion='SYNTHETIC TEST ONLY')

    def tearDown(self):
        self.temp.cleanup()

    def save(self,items):
        write_json(self.root/'project.assets.json',dict(schemaVersion=1,assets=items))

    def test_full_inventory_checks_unreferenced_approved_sources(self):
        self.save([self.item])
        self.assertEqual(set(load_project_assets(self.root)),{'Primary'})
        png(self.root/'surface.png',color=128)
        with self.assertRaisesRegex(ValueError,'Approved project asset changed'):
            load_project_assets(self.root)

    def test_duplicate_identity_and_unapproved_source(self):
        self.save([self.item,dict(self.item,assetId='primary')])
        with self.assertRaisesRegex(ValueError,'duplicate'):
            load_project_assets(self.root)
        self.save([dict(self.item,approved=False,source='not-created.png')])
        self.assertEqual(load_project_assets(self.root),{})

    def test_toggle_states_are_explicit_and_aligned(self):
        states=[dict(self.item,assetId=state,componentType='Toggle',styleId='Option',state=state) for state in ('Off','On','Disabled')]
        self.save(states)
        self.assertEqual(len(load_project_assets(self.root)),3)
        self.save(states[:2])
        with self.assertRaisesRegex(ValueError,'Off, On and Disabled'):
            load_project_assets(self.root)
        self.save([*states[:2],dict(states[2],pivot=[0,0])])
        with self.assertRaisesRegex(ValueError,'identical canvas'):
            load_project_assets(self.root)

    def test_project_profile_rejects_non_pot_and_escape(self):
        png(self.root/'surface.png',width=3)
        self.save([dict(self.item,sourceSha256=file_digest(self.root/'surface.png'))])
        with self.assertRaisesRegex(ValueError,'POT'):
            load_project_assets(self.root)
        self.save([dict(self.item,source='../outside.png')])
        with self.assertRaisesRegex(ValueError,'escapes'):
            load_project_assets(self.root)

    def test_project_import_profile_rejects_wrong_json_types(self):
        for change in ({'pixelsPerUnit':True}, {'sRGB':'false'}, {'tintable':'false'},
                       {'approved':'true'}, {'approvalVersion':' '}, {'pivot':[True,.5]}, {'border':[False,0,0,0]}):
            with self.subTest(change=change):
                self.save([dict(self.item, **change)])
                with self.assertRaises(ValueError):
                    load_project_assets(self.root)


if __name__=='__main__':
    unittest.main()
