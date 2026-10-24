"""Compare browser and Unity binding geometry; image review is a separate gate."""
import argparse
import json
import math
from pathlib import Path


def compare(browser, unity, tolerance=1.0):
    if not math.isfinite(tolerance) or tolerance<0:
        raise ValueError('Tolerance must be finite and nonnegative')
    result = {'status': 'pass', 'tolerancePx': tolerance, 'differences': [], 'visualStatus': 'pending-human-or-agent-image-review'}
    for axis in ('width', 'height'):
        if browser[axis] != unity[axis]:
            result['differences'].append(f'Canvas {axis}: {browser[axis]} != {unity[axis]}')
    def index(snapshot,label):
        indexed={}
        for node in snapshot.get('nodes',snapshot.get('bindings',[])):
            key=(str(node.get('row',-1))+'::'+node['path']) if 'path' in node else node['key']
            if key in indexed:result['differences'].append(label+': duplicate node '+str(key))
            if any(type(node.get(axis)) not in (int,float) or not math.isfinite(node[axis]) for axis in ('x','y','width','height')):
                result['differences'].append(label+': invalid geometry '+str(key))
            indexed[key]=node
        return indexed
    left=index(browser,'browser');right=index(unity,'Unity')
    for key in sorted(left.keys() | right.keys()):
        if key not in left or key not in right:
            result['differences'].append(f'{key}: missing in {"browser" if key not in left else "Unity"}')
            continue
        if left[key].get('visible',True)!=right[key].get('visible',True):
            result['differences'].append(str(key)+': visibility differs')
        if left[key].get('visible',True)==right[key].get('visible',True)==False:
            continue
        if any(type(node.get(axis)) not in (int,float) or not math.isfinite(node[axis]) for node in (left[key],right[key]) for axis in ('x','y','width','height')):
            continue
        for axis in ('x', 'y', 'width', 'height'):
            delta = abs(left[key][axis] - right[key][axis])
            if delta > tolerance:
                result['differences'].append(f'{key}.{axis}: browser={left[key][axis]:.3f}, Unity={right[key][axis]:.3f}, delta={delta:.3f}')
    result['issues'] = browser.get('issues', []) + unity.get('issues', [])
    if result['differences'] or result['issues']:
        result['status'] = 'fail'
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('browser', type=Path)
    parser.add_argument('unity', type=Path)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--tolerance', type=float, default=1.0)
    args = parser.parse_args()
    report = compare(json.loads(args.browser.read_text(encoding='utf-8-sig')), json.loads(args.unity.read_text(encoding='utf-8-sig')), args.tolerance)
    text = json.dumps(report, ensure_ascii=False, indent=2)
    if args.output: args.output.write_text(text, encoding='utf-8')
    print(text)
    raise SystemExit(0 if report['status'] == 'pass' else 1)
