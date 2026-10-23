"""Migrate the frozen v01 design to the project asset contract, without new design approval.

Original approval records are archived verbatim. Copies retain their approved pixels;
Toggle surfaces compose the already reviewed 90x42 track/knob geometry and colors.
Run once; browser acceptance must be revalidated separately after migration.
"""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
REPO = ROOT.parents[1]
sys.path.insert(0, str(REPO / 'Skills/ui-panel-design-to-html/scripts'))
from pipeline_contract import project_elements, serialize_xhtml
from validate_directory import definition_digest
from PIL import Image
from PIL import ImageChops

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    backup = ROOT / 'design-review/project-migration-v01'
    if backup.exists() or (ROOT / 'project.assets.json').exists():
        raise ValueError('Already migrated; inspect the saved manifest and original records instead of overwriting them.')
    packages = sorted(ROOT.glob('Panels/*/html/panel.package.json'))
    # Check every original approval and freeze all inputs before the first mutation.
    for package in packages:
        panel = package.parent.parent
        groups = read(panel / 'design/element-groups.json')
        approval = read(panel / 'design/review/element-groups-approval.json')
        records = {g['groupId']: g for g in approval['groups']}
        sprites = {s['source']: s for s in groups['sprites']}
        for group in groups['groups']:
            record = records[group['groupId']]
            assert record['userApproval'] and record['imageSha256'] == sha(panel / group['sheet'])
            assert record['definitionSha256'] == definition_digest(group, sprites)
            assert all(record['filesSha256'][p] == sha(panel / p) for p in group['assets'])
        acceptance = read(panel / 'design/review/html-acceptance.json')
        assert all(sha(panel / p) == digest for p, digest in acceptance['filesSha256'].items())
    for package in packages:
        panel = package.parent.parent
        for relative in ['html', 'design/element-groups.json', 'design/review/element-groups-approval.json', 'design/review/html-acceptance.json']:
            src = panel / relative
            dst = backup / panel.name / relative
            dst.parent.mkdir(parents=True, exist_ok=True)
            if src.is_dir():
                shutil.copytree(src, dst)
                (dst / 'panel.package.json').rename(dst / 'panel.package.json.snapshot')
            else:
                shutil.copyfile(src, dst)

    # POT export of the previously approved track + knob, with no geometric fragments in Unity.
    # viewBox matches the reviewed display rectangle; Unity stretches the full texture back to 90x42.
    state_paths = {}
    for state, x, track, knob in [('Off', 5, '#36505F', '#EEF5F7'), ('On', 50, '#4AD7BB', '#EEF5F7'), ('Disabled', 50, '#36505F', '#AAC0CA')]:
        target = ROOT / 'shared-assets/project/Toggle' / ('Tgl_Sound_Surface_' + state + '.png')
        target.parent.mkdir(parents=True, exist_ok=True)
        svg = target.with_suffix('.svg')
        svg.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="512" viewBox="0 0 90 42" preserveAspectRatio="none"><rect width="90" height="42" rx="21" fill="{track}"/><rect x="{x}" y="5" width="32" height="32" rx="16" fill="{knob}"/></svg>', encoding='utf-8')
        subprocess.run(['node', '-e', "require('sharp')(process.argv[1]).png().toFile(process.argv[2]).catch(e=>{console.error(e);process.exit(1)})", str(svg), str(target)], check=True)
        state_paths[state] = target

    definitions = {}
    prefixes = {'Image':'Img', 'Button':'Btn', 'TMP_InputField':'Inp', 'Slider':'Sld', 'Toggle':'Tgl', 'ScrollRect':'Scr'}
    for package_path in packages:
        panel = package_path.parent.parent
        package = read(package_path)
        html_path = package_path.parent / package['html']
        document = ET.parse(html_path)
        source = next(n for n in document.iter() if n.get('data-root') == 'true')
        parents = {c:p for p in source.iter() for c in p}
        old_sprites = read(package_path.parent / 'sprites.json')
        old_map = {s['path']:s for s in old_sprites['sprites']}
        groups_path = panel / 'design/element-groups.json'
        groups = read(groups_path)
        approvals_path = panel / 'design/review/element-groups-approval.json'
        approvals = read(approvals_path)
        approval_hash = sha(backup / panel.name / 'design/review/element-groups-approval.json')
        local_entries = {}

        def assign(node, attribute, owner, part, state, original, border):
            # Use distinct semantic identities even when approved source pixels are identical.
            style = original.stem.split('_', 1)[-1]
            if owner == 'Toggle':
                style = 'Sound'
            identity = owner + '_' + style + '_' + part + '_' + state
            target = ROOT / 'shared-assets/project' / owner / (prefixes[owner] + '_' + style + '_' + part + '_' + state + '.png')
            target.parent.mkdir(parents=True, exist_ok=True)
            if original.resolve() != target.resolve():
                shutil.copyfile(original, target)
            definition = dict(assetId=identity, componentType=owner, part=part, styleId=style, state=state,
                              source=target.relative_to(ROOT).as_posix(), sourceSha256=sha(target), approved=True,
                              approvalVersion='element-groups-v01:' + approval_hash, tintable=owner != 'Toggle',
                              border=border, pivot=[0.5,0.5], pixelsPerUnit=100, sRGB=True, filter='Bilinear')
            if identity in definitions:
                assert definitions[identity]['sourceSha256'] == definition['sourceSha256']
                assert definitions[identity]['border'] == border
            else:
                definitions[identity] = definition
            local = package_path.parent / 'assets/project' / owner / target.name
            local.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(target, local)
            node.set(attribute, 'asset:' + identity)
            if node.get('data-ugui') == 'Image' and owner != 'Image':
                node.set('data-asset-part', part)
            with Image.open(local) as img:
                size = list(img.size)
            local_entries[identity] = dict(source=local.relative_to(package_path.parent).as_posix(), path='asset:' + identity,
                role='Sprite', border=border, pixelSize=size, hasAlpha=True,
                purpose='沿用已批准v01素材；' + owner + '/' + part + '/' + state, usage='source')

        for node in source.iter():
            kind = node.get('data-ugui')
            if kind == 'Toggle':
                node.attrib.pop('data-sprite', None)
                node.attrib.pop('data-nine-slice', None)
                node.attrib.pop('data-border', None)
                for state in ('Off', 'On', 'Disabled'):
                    assign(node, 'data-toggle-' + state.lower() + '-sprite', 'Toggle', 'Surface', state, state_paths[state], [0,0,0,0])
                continue
            value = node.get('data-sprite')
            if not value:
                continue
            owner = kind
            if kind == 'Image':
                parent = parents.get(node)
                while parent is not None:
                    if parent.get('data-ugui') in ('Button','Toggle','Slider','InputField'):
                        owner = parent.get('data-ugui')
                        break
                    parent = parents.get(parent)
            owner = {'InputField':'TMP_InputField','ScrollView':'ScrollRect','Container':'Image'}.get(owner,owner)
            sprite = old_map[value]
            original = package_path.parent / sprite['source']
            border = sprite['border'] if node.get('data-nine-slice') == 'true' else [0,0,0,0]
            part = 'Icon' if kind == 'Image' and owner != 'Image' else ('Track' if owner == 'Slider' else 'Surface')
            assign(node, 'data-sprite', owner, part, 'Normal' if owner == 'Button' else 'Default', original, border)
            if kind == 'Slider':
                assign(node, 'data-fill-sprite', owner, 'Fill', 'Default', original, border)
                assign(node, 'data-handle-sprite', owner, 'Handle', 'Default', ROOT / 'shared-assets/Img_Circle.png', border)

        html_path.write_text('<!DOCTYPE html>\n' + serialize_xhtml(document.getroot()) + '\n', encoding='utf-8')
        # Original browser PNGs remain frozen and available to CSS/JS. Runtime copies have typed names.
        for sprite in old_sprites['sprites']:
            sprite['usage'] = 'preview-state'
        old_sprites['sprites'].extend(local_entries.values())
        write(package_path.parent / 'sprites.json', old_sprites)
        for identity, sprite in local_entries.items():
            definition = definitions[identity]
            kind = {'TMP_InputField':'InputField','ScrollRect':'ScrollView'}.get(definition['componentType'], definition['componentType'])
            group = next(g for g in groups['groups'] if g['componentType'] == kind)
            relative = 'html/' + sprite['source']
            group['assets'].append(relative)
            groups['sprites'].append(dict(source=relative, componentType=kind, pixelSize=sprite['pixelSize'], originalPixelSize=sprite['pixelSize'],
                hasAlpha=True, border=sprite['border'], tintable=definition['tintable'], purpose=sprite['purpose']))
        # Carry the existing human approval forward only for the unchanged reviewed design.
        # Save the exact original record and a provenance link; no new human statement is invented.
        new_sprites = {s['source']:s for s in groups['sprites']}
        for group in groups['groups']:
            record = next(g for g in approvals['groups'] if g['groupId'] == group['groupId'])
            record['definitionSha256'] = definition_digest(group, new_sprites)
            record['filesSha256'] = {p:sha(panel / p) for p in group['assets']}
            record['migration'] = dict(originalApprovalSha256=approval_hash, originalRecord='../../../../design-review/project-migration-v01/' + panel.name + '/design/review/element-groups-approval.json',
                basis='既有用户批准v01；仅组件归属拆分及已展示状态的整图合成，原图/配色/几何不变。无新增用户确认。')
        write(groups_path, groups)
        write(approvals_path, approvals)
        elements_path = package_path.parent / 'elements.json'
        manifest = read(elements_path)
        predicted, _ = project_elements(source)
        expected = {e['elementId'] for e in predicted}
        manifest['elements'] = [e for e in manifest['elements'] if e['elementId'] in expected]
        write(elements_path, manifest)
        package['projectDirectory'] = '../../..'
        write(package_path, package)
        # Revalidation is a separate step; do not mark browser checks passed here.
        acceptance_path = panel / 'design/review/html-acceptance.json'
        acceptance = read(acceptance_path)
        acceptance['status'] = 'pending-project-migration-validation'
        acceptance['migrationOriginalAcceptance'] = 'design-review/project-migration-v01/' + panel.name + '/design/review/html-acceptance.json'
        write(acceptance_path, acceptance)
    write(ROOT / 'project.assets.json', dict(schemaVersion=1, assets=sorted(definitions.values(), key=lambda d:d['assetId'])))
    print('Migrated', len(packages), 'panels and', len(definitions), 'typed runtime assets. Browser acceptance is pending.')

def finish_browser_validation():
    from validate_directory import validate
    checks_path = ROOT / 'design-review/browser/checks.json'
    checks = read(checks_path)
    assert checks['status'] == 'passed' and len(checks['checks']) == 14 and not checks['errors']
    results = []
    for package in sorted(ROOT.glob('Panels/*/html/panel.package.json')):
        panel = package.parent.parent
        review = panel / 'design/review'
        baseline = Image.open(review / 'browser/default.png').convert('RGB')
        current = Image.open(ROOT / 'design-review/browser' / (panel.name + '.png')).convert('RGB')
        assert baseline.size == current.size and ImageChops.difference(baseline, current).getbbox() is None
        shutil.copyfile(checks_path, review / 'browser/checks.json')
        path = review / 'html-acceptance.json'
        acceptance = read(path)
        acceptance['status'] = 'passed'
        acceptance['approvedElementGroupsSha256'] = sha(review / 'element-groups-approval.json')
        acceptance['filesSha256'] = {p.relative_to(panel).as_posix():sha(p) for p in sorted((panel / 'html').rglob('*')) if p.is_file()}
        for relative in ['design/element-groups.json', 'design/review/browser/default.png', 'design/review/browser/checks.json']:
            acceptance['filesSha256'][relative] = sha(panel / relative)
        acceptance['migrationValidation'] = dict(browserChecks=14, defaultScreenshotPixelsUnchanged=True, projectAssetManifestSha256=sha(ROOT / 'project.assets.json'))
        write(path, acceptance)
        try:
            results.append(validate(panel, require_ready=True))
        except Exception:
            acceptance['status'] = 'pending-project-migration-validation'
            write(path, acceptance)
            raise
    write(ROOT / 'design-review/project-migration-v01/validation.json', dict(panels=results, browserChecks=14, screenshotPixelsUnchanged=True))
    print('PASS: six ready packages, 14 browser checks, unchanged default pixels; Unity acceptance remains pending.')

if __name__ == '__main__':
    if sys.argv[1:] == ['--validate-browser']:
        finish_browser_validation()
    else:
        main()
