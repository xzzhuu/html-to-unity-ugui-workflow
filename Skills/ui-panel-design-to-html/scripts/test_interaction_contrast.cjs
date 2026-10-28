const test = require('node:test');
const assert = require('node:assert/strict');
const { contrastRatio, audit } = require('./interaction_contrast.cjs');
const example = pairs => [{ presetId: 'synthetic', kind: 'SyntheticPanel', interactionContrasts: pairs }];
const pair = (foreground, background, minimum = 3, extra = {}) => ({ role: 'control', foreground, background, minimum, ...extra });
test('black and white are 21:1; identical colors are 1:1', () => {
  assert.equal(contrastRatio('#000000', '#FFFFFF'), 21);
  assert.equal(contrastRatio('#515151', '#515151'), 1);
});
test('alpha uses the composited color, rather than the source white', () => {
  assert.equal(contrastRatio('#FFFFFF00', '#000000'), 1);
  assert.ok(contrastRatio('#FFFFFF80', '#000000') > 5 && contrastRatio('#FFFFFF80', '#000000') < 6);
});
test('unknown translucent backgrounds must be resolved first', () => {
  assert.throws(() => contrastRatio('#FFFFFF', '#00000080'), /opaque/);
});
test('transparent-looking secondary controls fail even when their label is readable', () => {
  const result = audit(example([pair('#1E2329', '#1E2329'), pair('#FFFFFF', '#1E2329', 4.5)]));
  assert.equal(result.status, 'failed'); assert.equal(result.violations.length, 1);
});
test('missing off-track or scrollbar contrast is rejected', () => {
  const result = audit(example([pair('#30343A', '#2B333C', 3, { role: 'slider track' }), pair('#FFFFFF', '#000000')]));
  assert.equal(result.status, 'failed'); assert.equal(result.violations[0].role, 'slider track');
});
test('values are not rounded up to a threshold', () => {
  const value = contrastRatio('#767676', '#FFFFFF');
  assert.equal(audit(example([pair('#767676', '#FFFFFF', value + 0.000001)])).status, 'failed');
});
test('disabled exemption does not exempt an enabled off toggle', () => {
  const result = audit(example([pair('#777777', '#777777', 3, { disabled: true }), pair('#777777', '#777777', 3, { role: 'Off toggle', disabled: false })]));
  assert.equal(result.exemptDisabled, 1); assert.equal(result.violations.length, 1);
});
test('missing data and invalid numeric or color inputs fail clearly', () => {
  assert.throws(() => audit([]), /nonempty/);
  assert.throws(() => audit([{ kind: 'Panel' }]), /Missing measurements/);
  assert.throws(() => audit(example([pair('#FFFFFF', '#000000', NaN)])), /threshold/);
  assert.throws(() => contrastRatio('white', '#000000'), /Expected/);
});
