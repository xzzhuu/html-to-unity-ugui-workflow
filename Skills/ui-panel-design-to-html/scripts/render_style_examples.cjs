#!/usr/bin/env node
// Offline, editable reference art. No project, runtime UI, approval, or SDK writes.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { audit } = require('./interaction_contrast.cjs');
const root = path.resolve(__dirname, '..');
const ids = ['meta-friendly', 'pico-spatial', 'microsoft-mrdl', 'spatial-soft'];
const options = {};
for (let i = 2; i < process.argv.length; i += 2) {
  if (!['--output', '--sharp-module'].includes(process.argv[i]) || !process.argv[i + 1]) {
    throw new Error('Usage: node render_style_examples.cjs [--output directory] [--sharp-module installed-path]');
  }
  options[process.argv[i].slice(2)] = process.argv[i + 1];
}
const output = path.resolve(options.output || path.join(root, 'assets/style-presets'));
let sharp;
try { sharp = require(options['sharp-module'] || 'sharp'); }
catch { throw new Error('PNG rendering needs an existing sharp installation. Do not install automatically; bundled SVG/PNG remain usable.'); }
const sourceManifest = JSON.parse(fs.readFileSync(path.join(root, 'assets/icon-library/sources.json'), 'utf8'));
const license = fs.readFileSync(path.join(root, 'assets/icon-library', sourceManifest.licensePath), 'utf8');
const iconSource = new Map(sourceManifest.files.map(i => [i.name, fs.readFileSync(path.join(root, 'assets/icon-library', i.localPath), 'utf8')]));
const esc = s => String(s).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
const hash = data => crypto.createHash('sha256').update(data).digest('hex');

class Drawing {
  constructor(style, kind) {
    this.style = style; this.c = style.colors; this.g = style.geometry; this.kind = kind;
    this.parts = []; this.hits = []; this.texts = []; this.surfaces = []; this.interactionContrasts = [];
    this.rect(0, 0, 1600, 900, this.c.base, this.g.panelRadius);
  }
  rect(x, y, w, h, color, r = 0) {
    if (color !== 'transparent') {
      this.parts.push(`<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${r}" fill="${color}"/>`);
      this.surfaces.push({ x, y, w, h, color });
    }
  }
  backgroundAt(x, y) {
    return this.surfaces.findLast(s => x >= s.x && x < s.x + s.w && y >= s.y && y < s.y + s.h)?.color || this.c.base;
  }
  measure(role, foreground, background, minimum = 3, disabled = false) {
    this.interactionContrasts.push({ role, foreground, background, minimum, disabled });
  }
  text(x, y, label, size = 24, color = this.c.text, weight = 400, anchor = 'start') {
    this.parts.push(`<text x="${x}" y="${y}" font-size="${size}" fill="${color}" font-weight="${weight}" text-anchor="${anchor}" dominant-baseline="text-before-edge">${esc(label)}</text>`);
    this.texts.push({ x, y, label, size, weight, anchor });
  }
  icon(name, x, y, size = 28, color = this.c.text) {
    const source = iconSource.get(name);
    if (!source) throw new Error('Missing bundled icon: ' + name);
    const viewBox = source.match(/viewBox="([^"]+)"/)[1];
    const body = source.replace(/^[\s\S]*?<svg\b[^>]*>/, '').replace(/<\/svg>\s*$/, '').replace(/fill="(?!none)[^"]*"/g, `fill="${color}"`);
    this.parts.push(`<svg x="${x}" y="${y}" width="${size}" height="${size}" viewBox="${viewBox}" fill="${color}" color="${color}" aria-label="${name}">${body}</svg>`);
  }
  hit(x, y, w, h, label, type = 'Button', disabled = false) { this.hits.push({ x, y, width: w, height: h, label, type, disabled }); }
  button(x, y, w, label, kind = 'primary', icon = null, h = 64) {
    const c = this.c;
    const bg = kind === 'primary' ? c.primary : kind === 'disabled' ? c.disabled : c.secondary;
    const fg = kind === 'primary' ? c.onPrimary : kind === 'disabled' ? c.disabledText : c.onSecondary;
    this.measure(label + '/surface', bg, this.backgroundAt(x + w / 2, y + h / 2), 3, kind === 'disabled');
    this.measure(label + '/label-icon', fg, bg, 4.5, kind === 'disabled');
    this.rect(x, y, w, h, bg, this.g.buttonRadius);
    if (icon) this.icon(icon, x + 20, y + (h - 28) / 2, 28, fg);
    this.text(x + w / 2 + (icon ? 12 : 0), y + (h - 30) / 2, label, 24, fg, 600, 'middle');
    this.hit(x, y, w, h, label, 'Button', kind === 'disabled');
  }
  badge(x, y, label, tone = 'success', w = 112) {
    const bg = this.c[tone + 'Surface'] || this.c.accent;
    this.rect(x, y, w, 38, bg, 19);
    this.text(x + w / 2, y + 5, label, 20, this.c[tone] || this.c.text, 600, 'middle');
  }
  toggle(x, y, on, disabled = false, label = '开关') {
    const color = disabled ? this.c.disabled : on ? this.c.primary : this.c.muted;
    const thumb = disabled ? this.c.disabledText : on ? this.c.onPrimary : this.c.card;
    this.measure(label + '/track', color, this.backgroundAt(x + 44, y + 23), 3, disabled);
    this.measure(label + '/thumb', thumb, color, 3, disabled);
    this.rect(x, y, 88, 46, color, 23);
    this.rect(x + (on ? 46 : 5), y + 5, 36, 36, thumb, 18);
    this.hit(x - 8, y - 9, 104, 64, label, 'Toggle', disabled);
  }
  slider(x, y, w, value = 0.66) {
    const parent = this.backgroundAt(x + w / 2, y + 25);
    this.measure('Slider/track-parent', this.c.controlTrack, parent);
    this.measure('Slider/fill-track', this.c.controlFill, this.c.controlTrack);
    this.measure('Slider/handle-parent', this.c.controlFill, parent);
    this.rect(x, y + 20, w, 10, this.c.controlTrack, 5);
    this.rect(x, y + 20, w * value, 10, this.c.controlFill, 5);
    this.rect(x + w * value - 16, y + 9, 32, 32, this.c.controlFill, 16);
    this.hit(x - 8, y - 7, w + 16, 64, '透视亮度', 'Slider');
  }
  nav(mode, monitoring = false) {
    const labels = monitoring ? ['设备概览', '设备列表', '告警记录', '运行历史'] : ['工作空间', '显示与舒适', '交互方式', '通知与声音'];
    const names = monitoring ? ['grid', 'apps', 'alert', 'document_table'] : ['cube', 'eye', 'hand_right', 'settings'];
    if (mode === 'top') {
      this.rect(88, 122, 1424, 68, this.c.navBackground, 34);
      labels.forEach((t, i) => {
        const x = 100 + i * 348;
        const surface = i === 0 ? this.c.navSelectedSurface : this.backgroundAt(x + 160, 156);
        const foreground = i === 0 ? this.c.text : this.c.muted;
        const iconColor = i === 0 ? this.c.primary : this.c.muted;
        this.measure('Tab/' + t + '/label', foreground, surface, 4.5);
        this.measure('Tab/' + t + '/icon', iconColor, surface);
        if (i === 0) this.rect(x, 130, 332, 52, surface, 26);
        this.icon(names[i], x + 42, 142, 28, iconColor);
        this.text(x + 88, 140, t, 24, foreground, i === 0 ? 600 : 400);
        this.hit(x, 130, 332, 52, t);
      });
      return;
    }
    const wide = mode === 'pill', w = wide ? 280 : mode === 'rail' ? 200 : 224;
    this.rect(wide ? 16 : 0, wide ? 16 : 0, w, wide ? 868 : 900, this.c.navBackground, wide ? 28 : this.g.panelRadius);
    this.icon('cube', 40, 40, 36, this.c.primary);
    this.text(88, 40, '空间工作区', wide ? 24 : 20, this.c.text, 600);
    this.text(40, 104, '工作区 A', 20, this.c.muted);
    labels.forEach((t, i) => {
      const x = wide ? 32 : 20, y = 172 + i * (wide ? 88 : 80), bw = wide ? 248 : w - 40;
      const surface = i === 0 ? this.c.navSelectedSurface : this.backgroundAt(x + bw / 2, y + 32);
      const foreground = i === 0 ? this.c.text : this.c.muted;
      const iconColor = i === 0 ? this.c.primary : this.c.muted;
      this.measure('Nav/' + t + '/label', foreground, surface, 4.5);
      this.measure('Nav/' + t + '/icon', iconColor, surface);
      if (i === 0) this.rect(x, y, bw, 64, surface, wide ? 32 : 12);
      this.icon(names[i], x + 18, y + 18, 28, iconColor);
      this.text(x + 60, y + 17, t, wide ? 24 : 20, foreground, i === 0 ? 600 : 400);
      this.hit(x, y, bw, 64, t);
    });
    this.badge(40, 770, '已连接', 'success', w - 64);
    this.text(40, 826, '工作头显 01', 18, this.c.muted);
  }
  heading(x, monitoring = false) {
    this.text(x, 42, monitoring ? '设备监控' : '空间设置', 40, this.c.text, 650);
    this.text(x, 104, monitoring ? '工作区 A    6 台设备    最近更新 14:32' : '为当前工作空间配置显示与交互', 24, this.c.muted);
  }
  settingRow(x, y, w, title, description, on) {
    this.text(x, y, title, 26, this.c.text, 550);
    this.text(x, y + 42, description, 20, this.c.muted);
    this.toggle(x + w - 96, y + 12, on, false, title);
  }
  footer(x = 1120) {
    this.button(x, 800, 192, '恢复默认', 'secondary', 'arrow_reset');
    this.button(1344, 800, 216, '保存设置', 'primary', 'checkmark');
  }
  settings() {
    const id = this.style.designDirection.presetId, c = this.c, r = this.g.cardRadius;
    if (id === 'meta-friendly') {
      this.nav('pill'); this.heading(336);
      this.rect(336, 160, 1224, 160, c.card, r);
      this.rect(368, 192, 96, 96, c.accent, 28); this.icon('location', 394, 218, 44, c.primary);
      this.text(492, 184, '桌面工作区', 32, c.text, 650);
      this.text(492, 238, '固定世界位置    手柄射线', 24, c.muted);
      this.badge(1408, 194, '已连接');
      this.rect(336, 344, 1224, 392, c.card, r);
      this.settingRow(368, 374, 1152, '显示空间网格', '辅助识别空间边界与定位', true);
      this.settingRow(368, 490, 1152, '面板跟随', '关闭后保持当前世界位置', false);
      this.text(368, 612, '透视亮度', 26, c.text, 550); this.text(368, 654, '保持真实环境清楚可见', 20, c.muted);
      this.text(1520, 600, '66%', 24, c.primary, 600, 'end'); this.slider(1104, 650, 384);
      this.text(368, 818, '更改将在保存后生效', 20, c.muted); this.footer();
    } else if (id === 'pico-spatial') {
      this.nav('side'); this.heading(264);
      this.rect(264, 164, 824, 572, c.card, r);
      this.text(296, 196, '显示与位置', 28, c.text, 650);
      this.settingRow(296, 270, 760, '显示空间网格', '辅助识别空间边界与定位', true);
      this.settingRow(296, 388, 760, '面板跟随', '关闭后保持当前世界位置', false);
      this.text(296, 524, '透视亮度', 26, c.text, 550); this.text(1056, 524, '66%', 26, c.primary, 600, 'end');
      this.slider(304, 586, 728); this.text(296, 668, '保持真实环境清楚可见', 20, c.muted);
      this.rect(1112, 164, 448, 572, c.deep, r); this.text(1144, 196, '当前空间', 24, c.muted);
      this.rect(1240, 270, 192, 144, c.accent, 32); this.icon('cube', 1296, 302, 80, c.primary);
      this.text(1336, 450, '桌面工作区', 32, c.text, 600, 'middle'); this.badge(1280, 512, '已连接');
      this.text(1144, 596, '固定世界位置', 24); this.text(1144, 644, '输入方式：手柄射线', 22, c.muted);
      this.text(264, 820, '工作头显 01', 22, c.muted); this.footer();
    } else if (id === 'microsoft-mrdl') {
      this.nav('rail'); this.heading(248);
      this.rect(248, 164, 628, 254, c.card, r); this.icon('location', 280, 196, 36, c.primary);
      this.text(332, 196, '空间定位', 28, c.text, 650); this.text(280, 266, '桌面工作区', 34, c.text, 600);
      this.text(280, 326, '固定世界位置', 24, c.muted); this.badge(724, 330, '已连接');
      this.rect(900, 164, 660, 254, c.deep, r); this.icon('hand_right', 932, 196, 36, c.primary);
      this.text(984, 196, '交互方式', 28, c.text, 650); this.text(932, 266, '手柄射线', 34, c.text, 600);
      this.text(932, 326, '工作头显 01', 24, c.muted);
      this.rect(248, 442, 628, 294, c.deep, r); this.text(280, 472, '显示选项', 28, c.text, 650);
      this.settingRow(280, 538, 564, '显示空间网格', '辅助识别空间边界与定位', true);
      this.settingRow(280, 638, 564, '面板跟随', '关闭后保持当前世界位置', false);
      this.rect(900, 442, 660, 294, c.card, r); this.text(932, 472, '透视亮度', 28, c.text, 650);
      this.text(932, 536, '66', 64, c.text, 600); this.text(1022, 572, '%', 24, c.muted);
      this.slider(1180, 558, 332); this.text(932, 654, '保持真实环境清楚可见', 22, c.muted);
      this.text(248, 820, '更改将在保存后生效', 22, c.muted); this.footer();
    } else {
      this.heading(88); this.nav('top');
      this.rect(88, 224, 448, 512, c.deep, r); this.rect(248, 274, 128, 128, c.card, 40); this.icon('cube', 280, 306, 64, c.primary);
      this.text(312, 438, '桌面工作区', 32, c.text, 600, 'middle'); this.badge(256, 502, '已连接');
      this.text(312, 570, '固定世界位置', 24, c.muted, 400, 'middle'); this.text(312, 616, '手柄射线', 24, c.muted, 400, 'middle');
      this.text(312, 680, '工作头显 01', 20, c.muted, 400, 'middle');
      this.rect(568, 224, 944, 512, c.card, r);
      this.settingRow(608, 262, 864, '显示空间网格', '辅助识别空间边界与定位', true);
      this.settingRow(608, 390, 864, '面板跟随', '关闭后保持当前世界位置', false);
      this.text(608, 534, '透视亮度', 26, c.text, 550); this.text(1472, 534, '66%', 26, c.primary, 600, 'end');
      this.slider(616, 594, 824); this.text(608, 670, '保持真实环境清楚可见', 20, c.muted);
      this.text(88, 820, '更改将在保存后生效', 22, c.muted);
      this.button(1064, 800, 200, '恢复默认', 'secondary', 'arrow_reset'); this.button(1296, 800, 216, '保存设置', 'primary', 'checkmark');
    }
  }
  metric(x, y, w, name, value, unit = '', surface = true) {
    if (surface) this.rect(x, y, w, 136, this.c.card, this.g.cardRadius);
    this.text(x + 24, y + 18, name, 22, this.c.muted); this.text(x + 24, y + 54, value, 48, this.c.text, 650);
    if (unit) this.text(x + w - 24, y + 82, unit, 20, this.c.muted, 400, 'end');
  }
  table(x, y, w) {
    const c = this.c; this.rect(x, y, w, 422, c.card, this.g.cardRadius);
    const cols = [x + 24, x + w * 0.34, x + w * 0.64, x + w - 24];
    ['设备编号', '运行状态', '温度', '剩余电量'].forEach((t, i) => this.text(cols[i], y + 20, t, 20, c.muted, 400, i > 1 ? 'end' : 'start'));
    [78, 86, 42, 93, 64, 71].forEach((battery, i) => {
      const yy = y + 66 + i * 54, warn = i === 2;
      if (warn) this.rect(x + 8, yy - 5, w - 16, 48, c.warningSurface, 8);
      this.text(cols[0], yy, 'XR-0' + (i + 1), 24, c.text, 600);
      this.icon(warn ? 'alert' : 'checkmark', cols[1], yy + 3, 24, warn ? c.warning : c.success);
      this.text(cols[1] + 34, yy, warn ? '需关注' : '运行中', 22, warn ? c.warning : c.success);
      this.text(cols[2], yy, (warn ? '49' : 35 + i) + ' °C', 24, warn ? c.warning : c.text, 400, 'end');
      this.text(cols[3], yy, battery + '%', 24, c.text, 400, 'end');
    });
  }
  alertCard(x, y, w, h = 422) {
    const c = this.c; this.rect(x, y, w, h, c.deep, this.g.cardRadius);
    this.rect(x + 24, y + 24, 56, 56, c.warningSurface, 16); this.icon('alert', x + 38, y + 38, 28, c.warning);
    this.text(x + 24, y + 108, 'XR-03 温度偏高', 26, c.text, 600);
    this.text(x + 24, y + 162, '当前温度', 20, c.muted); this.text(x + 24, y + 204, '49 °C', 48, c.warning, 600);
    this.text(x + 24, y + 282, '建议暂停使用并检查散热', 20, c.muted);
    this.button(x + 24, y + h - 88, w - 48, '查看告警', 'soft', 'alert');
  }
  monitoring() {
    const id = this.style.designDirection.presetId, c = this.c;
    const apple = id === 'spatial-soft';
    if (apple) { this.heading(88, true); this.nav('top', true); }
    else { this.nav(id === 'meta-friendly' ? 'pill' : id === 'pico-spatial' ? 'side' : 'rail', true); this.heading(id === 'meta-friendly' ? 336 : id === 'pico-spatial' ? 264 : 248, true); }
    if (id === 'meta-friendly') {
      this.metric(336, 162, 392, '在线设备', '06', '台'); this.metric(752, 162, 392, '今日运行', '12.6', '小时'); this.metric(1168, 162, 392, '待处理告警', '02', '条');
      this.table(336, 330, 848); this.alertCard(1208, 330, 352);
    } else if (id === 'pico-spatial') {
      this.rect(264, 162, 1296, 136, c.deep, this.g.cardRadius);
      this.metric(264, 162, 416, '在线设备', '06', '台', false); this.metric(704, 162, 416, '今日运行', '12.6', '小时', false); this.metric(1144, 162, 416, '待处理告警', '02', '条', false);
      this.table(264, 330, 888); this.alertCard(1176, 330, 384);
    } else if (id === 'microsoft-mrdl') {
      this.table(248, 326, 900); this.alertCard(1172, 326, 388);
      this.metric(248, 162, 412, '在线设备', '06', '台'); this.metric(684, 162, 412, '今日运行', '12.6', '小时'); this.metric(1120, 162, 440, '待处理告警', '02', '条');
    } else {
      this.metric(88, 214, 448, '在线设备', '06', '台', false); this.metric(568, 214, 448, '今日运行', '12.6', '小时', false); this.metric(1048, 214, 464, '待处理告警', '02', '条', false);
      this.table(88, 374, 968); this.alertCard(1080, 374, 432);
    }
    const left = apple ? 88 : id === 'meta-friendly' ? 336 : id === 'pico-spatial' ? 264 : 248;
    this.text(left, 828, '显示全部 6 台设备', 20, c.muted);
    this.button(apple ? 1296 : 1344, 812, 216, '导出记录', 'primary', 'arrow_download');
  }
  components() {
    const c = this.c, r = this.g.cardRadius;
    this.text(48, 32, '组件结构与反馈', 40, c.text, 650); this.text(48, 94, '按职责组合，颜色变化共用素材；图标统一使用 Fluent Filled', 24, c.muted);
    this.rect(48, 152, 1504, 144, c.card, r);
    this.button(80, 192, 216, '保存设置', 'primary', 'checkmark'); this.button(320, 192, 192, '恢复默认', 'secondary', 'arrow_reset');
    this.button(552, 192, 216, '空间设置', 'soft', 'settings');
    this.measure('ActionCard/surface', c.secondary, c.card); this.measure('ActionCard/label-icon', c.onSecondary, c.secondary, 4.5);
    this.rect(816, 176, 704, 96, c.secondary, r); this.icon('cube', 852, 208, 32, c.onSecondary);
    this.text(924, 188, '桌面工作区', 26, c.onSecondary, 600); this.text(924, 232, '图标、标题与状态组合为操作卡', 20, c.onSecondary);
    this.hit(816, 176, 704, 96, '桌面工作区', 'Button');
    this.rect(48, 320, 720, 300, c.card, r); this.text(80, 344, '次操作与交互状态', 28, c.text, 600);
    const states = [['正常', c.secondary, c.onSecondary], ['悬停', c.controlHover, c.onSecondary], ['聚焦', c.controlFocus, c.onSecondary], ['按下', c.controlPressed, c.onSecondary], ['禁用', c.disabled, c.disabledText]];
    states.forEach(([label, bg, fg], i) => {
      this.measure('Selectable/' + label + '/surface', bg, c.card, 3, label === '禁用'); this.measure('Selectable/' + label + '/label', fg, bg, 4.5, label === '禁用');
      this.rect(80 + i * 132, 404, 112, 64, bg, this.g.buttonRadius); this.text(136 + i * 132, 420, label, 24, fg, 600, 'middle');
    });
    [['Off', false, false], ['On', true, false], ['Disabled', false, true]].forEach(([label, on, disabled], i) => { this.toggle(92 + i * 222, 512, on, disabled, 'Toggle ' + label); this.text(136 + i * 222, 570, label, 20, c.muted, 400, 'middle'); });
    this.rect(792, 320, 760, 300, c.deep, r); this.text(824, 344, '信息层级与图标', 28, c.text, 600);
    this.text(824, 410, '透视亮度', 26, c.text, 600); this.text(824, 452, '数值和单位分层，信息不依赖颜色', 20, c.muted); this.text(1508, 402, '66%', 42, c.primary, 600, 'end');
    ['cube', 'location', 'eye', 'hand_right', 'settings', 'alert', 'grid', 'apps'].forEach((name, i) => { this.rect(824 + i * 84, 532, 64, 64, c.card, 16); this.icon(name, 840 + i * 84, 548, 32, c.text); });
    this.rect(48, 644, 1504, 208, c.card, r);
    this.text(80, 666, '输入、选择、滑动与滚动', 26, c.text, 600);
    this.text(80, 800, '必要识别部位 ≥3:1，普通控件文字 ≥4.5:1；数值检查不代替 VR 实测。', 20, c.muted);
    for (const [x, w, label, type] of [[80, 392, '设备名称', 'InputField'], [496, 336, '工作区 A', 'Dropdown']]) {
      this.measure(type + '/surface', c.secondary, c.card); this.measure(type + '/label', c.onSecondary, c.secondary, 4.5);
      this.rect(x, 716, w, 64, c.secondary, this.g.inputRadius); this.text(x + 24, 732, label, 24, c.onSecondary);
      if (type === 'Dropdown') this.icon('chevron_down', x + w - 48, 734, 28, c.onSecondary);
      this.hit(x, 716, w, 64, label, type);
    }
    this.slider(888, 724, 432);
    this.measure('Scrollbar/track-parent', c.controlTrack, c.card); this.measure('Scrollbar/thumb-track', c.controlFill, c.controlTrack); this.measure('Scrollbar/thumb-parent', c.controlFill, c.card);
    this.rect(1440, 684, 24, 112, c.controlTrack, 12); this.rect(1440, 714, 24, 48, c.controlFill, 12);
    this.hit(1416, 676, 72, 128, '滚动条', 'Scrollbar');
  }
  svg() {
    const title = this.style.designDirection.displayName;
    const name = { SettingsPanel: '空间设置', MonitoringPanel: '设备监控', Components: '组件一览' }[this.kind];
    const meta = JSON.stringify({ status: 'draft-unapproved', kind: this.kind, presetId: this.style.designDirection.presetId, iconRepository: sourceManifest.repository, iconCommit: sourceManifest.commit, license });
    return `<svg xmlns="http://www.w3.org/2000/svg" width="1920" height="1080" viewBox="0 0 1920 1080" data-status="draft-unapproved"><title>${esc(title + ' / ' + name + ' / 未批准范例')}</title><metadata>${esc(meta)}</metadata><g font-family="Microsoft YaHei, Noto Sans CJK SC, sans-serif"><rect width="1920" height="1080" fill="${this.style.canvas.background}"/><text x="160" y="26" font-size="24" font-weight="600" fill="#192434" dominant-baseline="text-before-edge">${esc(title + ' / ' + name)}</text><text x="1760" y="30" text-anchor="end" font-size="20" fill="#253247" dominant-baseline="text-before-edge">原创适配 / 合成数据 / 未批准示范</text><g transform="translate(160 90)">${this.parts.join('')}</g><text x="160" y="1010" font-size="20" fill="#192434" dominant-baseline="text-before-edge">画布 1920×1080    主体 1600×900 / B 档    宽、高占比均为 83.3%</text><text x="1760" y="1044" text-anchor="end" font-size="18" fill="#253247" dominant-baseline="text-before-edge">世界尺寸、观看距离和设备可读性均待验证</text></g></svg>`;
  }
}

async function main() {
  fs.mkdirSync(output, { recursive: true });
  const entries = [], settings = [];
  for (const id of ids) {
    const style = JSON.parse(fs.readFileSync(path.join(root, 'assets/style-presets', id, 'tokens.json'), 'utf8'));
    const folder = path.join(output, id); fs.mkdirSync(folder, { recursive: true });
    for (const kind of ['SettingsPanel', 'MonitoringPanel', 'Components']) {
      const art = new Drawing(style, kind);
      if (kind === 'SettingsPanel') art.settings(); else if (kind === 'MonitoringPanel') art.monitoring(); else art.components();
      const contrast = audit([{ presetId: id, kind, interactionContrasts: art.interactionContrasts }]);
      if (contrast.status !== 'passed') throw new Error('Interaction contrast failed: ' + JSON.stringify(contrast.violations));
      const svg = art.svg(); const png = await sharp(Buffer.from(svg)).png().toBuffer();
      fs.writeFileSync(path.join(folder, kind + '.svg'), svg); fs.writeFileSync(path.join(folder, kind + '.png'), png);
      entries.push({ presetId: id, kind, status: 'draft-unapproved', canvas: { width: 1920, height: 1080 }, panel: { x: 160, y: 90, width: 1600, height: 900, sizeTier: 'B' }, svg: `${id}/${kind}.svg`, png: `${id}/${kind}.png`, svgSha256: hash(svg), pngSha256: hash(png), hitTargets: art.hits, texts: art.texts, interactionContrasts: art.interactionContrasts });
      if (kind === 'SettingsPanel') settings.push({ title: style.designDirection.displayName, svg });
    }
  }
  const tiles = settings.map((item, i) => {
    const x = 32 + (i % 2) * 1024, y = 172 + Math.floor(i / 2) * 648;
    const inner = item.svg.replace(/^[\s\S]*?<svg\b[^>]*>/, '').replace(/<\/svg>\s*$/, '');
    return `<text x="${x}" y="${y - 48}" font-size="28" font-weight="600" dominant-baseline="text-before-edge" fill="#EFF3F7">${esc(item.title)}</text><g transform="translate(${x} ${y}) scale(0.5)">${inner}</g>`;
  }).join('');
  const overview = `<svg xmlns="http://www.w3.org/2000/svg" width="2048" height="1400" viewBox="0 0 2048 1400"><metadata>${esc(license)}</metadata><g font-family="Microsoft YaHei, Noto Sans CJK SC, sans-serif"><rect width="2048" height="1400" fill="#263242"/><text x="32" y="24" font-size="42" dominant-baseline="text-before-edge" fill="#FFFFFF" font-weight="600">四种 XR 视觉方向</text><text x="32" y="86" font-size="24" dominant-baseline="text-before-edge" fill="#CBD5E1">同一业务与主体尺寸，同尺度完整画布。风格范例均未批准，不代表官方主题或设备验收。</text>${tiles}</g></svg>`;
  fs.writeFileSync(path.join(output, 'Overview.svg'), overview);
  fs.writeFileSync(path.join(output, 'Overview.png'), await sharp(Buffer.from(overview)).png().toBuffer());
  fs.writeFileSync(path.join(output, 'examples.json'), JSON.stringify({ schemaVersion: 1, status: 'reference-only-unapproved', units: 'layout pixels, not dp or meters', iconSources: '../icon-library/sources.json', visualReview: 'not-checked-by-renderer', interactionContrastAudit: audit(entries), examples: entries }, null, 2) + '\n');
  console.log(JSON.stringify({ output, examples: entries.length, overview: 'Overview.png', status: 'draft-unapproved' }));
}
main().catch(error => { console.error(error.stack); process.exitCode = 1; });
