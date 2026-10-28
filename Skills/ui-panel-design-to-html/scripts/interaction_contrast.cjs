// Numeric reference-art checks, not approval, a full accessibility audit, or XR certification.
const fs = require('node:fs');
function rgba(value) {
  if (!/^#[0-9a-f]{6}([0-9a-f]{2})?$/i.test(value)) throw new Error('Expected #RRGGBB or #RRGGBBAA: ' + value);
  return [1, 3, 5].map(i => parseInt(value.slice(i, i + 2), 16) / 255).concat(value.length === 9 ? parseInt(value.slice(7, 9), 16) / 255 : 1);
}
function luminance(rgb) {
  const linear = rgb.map(c => c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4);
  return linear[0] * 0.2126 + linear[1] * 0.7152 + linear[2] * 0.0722;
}
function contrastRatio(foreground, background) {
  const fg = rgba(foreground), bg = rgba(background);
  if (bg[3] !== 1) throw new Error('Resolve the actual opaque adjacent background before measuring');
  const composite = fg.slice(0, 3).map((v, i) => v * fg[3] + bg[i] * (1 - fg[3]));
  const a = luminance(composite), b = luminance(bg);
  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
}
function audit(examples) {
  if (!Array.isArray(examples) || !examples.length) throw new Error('Expected nonempty examples array');
  const violations = []; let measurements = 0, exemptDisabled = 0;
  for (const example of examples) {
    if (!Array.isArray(example.interactionContrasts) || !example.interactionContrasts.length) throw new Error('Missing measurements for ' + example.kind);
    for (const item of example.interactionContrasts) {
      if (item.disabled === true) { exemptDisabled++; continue; }
      if (typeof item.minimum !== 'number' || !Number.isFinite(item.minimum) || item.minimum < 3) throw new Error('Invalid contrast threshold');
      const ratio = contrastRatio(item.foreground, item.background); measurements++;
      // Threshold decisions use the raw ratio; rounding is display-only.
      if (ratio < item.minimum) violations.push({ presetId: example.presetId, kind: example.kind, ...item, ratio });
    }
  }
  return { status: violations.length ? 'failed' : 'passed', measurements, exemptDisabled, violations, scope: 'reference color pairs only; not design approval or device acceptance' };
}
module.exports = { contrastRatio, audit };
if (require.main === module) {
  try {
    if (!process.argv[2]) throw new Error('Usage: node interaction_contrast.cjs path/to/examples.json');
    const result = audit(JSON.parse(fs.readFileSync(process.argv[2], 'utf8')).examples);
    console.log(JSON.stringify(result, null, 2)); process.exitCode = result.status === 'passed' ? 0 : 1;
  } catch (error) { console.error(error.message); process.exitCode = 1; }
}
