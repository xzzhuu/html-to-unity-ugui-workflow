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
from validate_directory import validate, definition_digest, file_digest


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
            source='assets/sprites/Img_RoundedSurface.png'
            png(self.root/source)
            group['assets']=[source]
            sprites=[{'source':source, 'componentType':'Image', 'pixelSize':[4,4],
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
            report='unity-acceptance.json'
        files.append('design/element-groups.json')
        acceptance['filesSha256']={source:file_digest(self.root/source) for source in files}
        write_json(self.root/'design/review'/report,acceptance)
        return manifest

    def test_defaults_and_idempotency(self):
        result=initialize(self.root)
        before={p.relative_to(self.root):p.read_bytes() for p in self.root.rglob('*') if p.is_file()}
        self.assertEqual(result['mode'],'full')
        state=json.loads(before[Path('.html-directory.json')])
        self.assertEqual((state['width'],state['height']),(1920,1080))
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


if __name__=='__main__':
    unittest.main()
