"""Compare browser and Unity binding geometry; image review is a separate gate."""
import argparse
import json
from pathlib import Path


def compare(browser, unity, tolerance=1.0):
    result = {'status': 'pass', 'tolerancePx': tolerance, 'differences': [], 'visualStatus': 'pending-human-or-agent-image-review'}
    for axis in ('width', 'height'):
        if browser[axis] != unity[axis]:
            result['differences'].append(f'Canvas {axis}: {browser[axis]} != {unity[axis]}')
    left = {b['key']: b for b in browser['bindings']}
    right = {b['key']: b for b in unity['bindings']}
    for key in sorted(left.keys() | right.keys()):
        if key not in left or key not in right:
            result['differences'].append(f'{key}: missing in {"browser" if key not in left else "Unity"}')
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
