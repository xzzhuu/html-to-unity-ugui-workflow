"""Create evidence for approved-layout versus rendered-HTML review, never auto-approve."""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageChops, ImageStat


def compare(approved, rendered, output):
    output.mkdir(parents=True, exist_ok=True)
    with Image.open(approved) as a, Image.open(rendered) as b:
        a, b = a.convert('RGB'), b.convert('RGB')
        if a.size != b.size:
            raise ValueError(f'Canvas mismatch: approved {a.size}, HTML {b.size}; compare at the approved resolution.')
        pair = Image.new('RGB', (a.width * 2, a.height))
        pair.paste(a, (0, 0)); pair.paste(b, (a.width, 0))
        pair.save(output / 'approved-vs-html.png')
        diff = ImageChops.difference(a, b)
        diff.save(output / 'approved-vs-html.diff.png')
        report = {'status': 'pending-visual-review', 'width': a.width, 'height': a.height,
                  'meanAbsoluteRgbDifference': ImageStat.Stat(diff).mean,
                  'note': 'Inspect completeness and visual appearance; these metrics do not establish approval.'}
        (output / 'approved-vs-html.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
        return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('approved', type=Path)
    parser.add_argument('rendered', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    print(json.dumps(compare(args.approved, args.rendered, args.output)))
