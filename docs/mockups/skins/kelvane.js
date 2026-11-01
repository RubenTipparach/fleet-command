// The Kelvane Institute skin (mockup 4, OpenSpec change console-skins): a race of experimenters who trust only what an
// instrument can show. Their instruments were inspired by the Nucleo NLog look (owner, 2026-10-31: "They were inspired
// by the nucleo look. So.lets update that"), so the kit is built the way the session's NLog study was, in our own design:
// pillowed champagne plates over recessed displays, amber backlit film for readouts and green phosphor glass for scopes.
// It is modelled, lit and rendered in Blender (tools/blender/build_ui_kelvane.py, from the fit-checked shapes of
// tools/mockups/kelvane_shapes.py) and inlined into the page; this module adds the skin's tokens and draws only the live
// readouts, each inside its own well: names and keys in a calculator face on the amber windows, traces on the scopes.
// Registers itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
const KV = { champagne: '#c9bfa9', amber: '#f2c46e', ink: '#4a3412', inkOn: '#3a2408', engrave: '#2f2a22', dim: '#5a5245',
  phos: '#4dff88', phosHi: '#b4ffcf', needle: '#c8302a', brass: '#b0915a', bg: '#1c1a16' };

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('kelvane', {
  name: 'Kelvane Institute', kind: 'An NLog-style instrument kit in Blender: champagne plates, amber readouts, phosphor scopes', bg: KV.bg, sampleBg: KV.champagne,
  css: { '--s-ink': KV.engrave, '--s-dim': KV.dim, '--s-num': KV.engrave, '--s-body': '"Courier Prime", monospace', '--btn-ink': KV.ink, '--btn-justify': 'center',
    '--btn-shadow': '0 0 3px rgba(255, 226, 160, 0.55)', '--btn-font': '400 17px/1 "Share Tech Mono", monospace', '--btn-track': '0.12em', '--btn-case': 'uppercase',
    '--btn-motion': 'padding-top 0.06s', '--btn-sink': '1.5px', '--tab-font': '400 13px/1 "Share Tech Mono", monospace', '--tab-pb': '1px', '--tab-ink': '#2a1d0a', '--tab-ink-on': KV.inkOn,
    '--title-font': '700 13px/1 "Courier Prime", monospace', '--title-ink': KV.engrave, '--lbl-font': '700 12px/1 "Courier Prime", monospace',
    '--num-font': '400 27px/1 "Share Tech Mono", monospace',
    // The strips are 72 px (owner, 2026-10-31: "we need to make parent taller"); their open band is the plate inside its rolled rim, rows 4-69,
    // measured by tools/mockups/console_fit.mjs, so each label sits over its bar with 3 px clear of the art.
    '--stt-pt': '4px', '--stt-pb': '3px', '--stb-pt': '4px', '--stb-pb': '3px', '--strip-lamp': '44px' },
  accent: KV.phos, warn: KV.needle,
  // The title is engraved inside the panel's fine line (it ends 10 px in), starting 13 px clear of the corner screw (18 +/- 3 px).
  title: (t, m) => { t.style.cssText = `left:${34 - m[3]}px;top:${12 - m[0]}px;letter-spacing:0.16em;`; },
  // The captain's name sits in the name plate's amber window (46-360 x 9-47), 8 px clear of its left end.
  plateText: { left: 54, top: 12, width: 290, height: 32, font: '400 22px/32px "Share Tech Mono", monospace', color: KV.ink, sub: 'RV PATIENCE · DR. ORREL' },
  helmKeys: 'ring', helmR: 92, shield: [KV.phos, '#ffcf6a', '#ff4a3a'],
  fonts: ['700 20px "Courier Prime"', '400 20px "Share Tech Mono"'], view: { title: '400 26px "Share Tech Mono"', body: '400 24px "Share Tech Mono"', upper: true, ink: '#8affb0', dim: KV.amber },
  build: () => { const d = F.kitData('assets/ui/kelvane/manifest.json'); return d ? F.fromManifest(d, () => F.paint(48, 48, () => {})) : null; },
  live: {
    // An arrow engraved on each domed key.
    arrow: (x) => { x.fillStyle = 'rgba(30, 24, 16, 0.85)'; x.beginPath(); x.moveTo(7, 0); x.lineTo(-4, -6); x.lineTo(-4, 6); x.closePath(); x.fill(); },
    // STOP glows on the helm's phosphor scope (radius 50), well inside it.
    helmCentre: (x, c) => { x.save(); x.shadowColor = KV.phos; x.shadowBlur = 8; x.fillStyle = KV.phosHi; x.font = '400 17px "Share Tech Mono"'; x.textAlign = 'center'; x.fillText('STOP', c, c + 6); x.restore(); },
    // The ship as a phosphor trace on the scope (radius 124, rings at 96): a rounded research hull with dishes; shields as dotted arcs.
    // The ship display fills its screen (owner: "ship display should take up 90% of the panel or more"): drawn at 1.4x
    // the old scope's scale, so the shield rings ride radius 134 on the 135 px graticule.
    ssd: (x, cx, cy, sh, col) => {
      x.save(); x.translate(cx, cy); x.scale(1.4, 1.4); x.translate(-cx, -cy); x.shadowBlur = 6;
      for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.shadowColor = col(sh[i]); x.strokeStyle = col(sh[i]); x.lineWidth = 2.4; x.setLineDash([2, 3]); x.beginPath(); x.arc(cx, cy, 96, a - P / 6 + 0.08, a + P / 6 - 0.08); x.stroke(); }
      x.setLineDash([]); x.shadowColor = KV.phos; x.strokeStyle = KV.phos; x.lineWidth = 1.8;
      x.beginPath(); x.ellipse(cx, cy, 30, 62, 0, 0, 2 * P); x.stroke();
      for (const s of [-1, 1]) { x.beginPath(); x.arc(cx + s * 44, cy - 10, 12, 0, 2 * P); x.stroke(); x.beginPath(); x.moveTo(cx + s * 30, cy - 10); x.lineTo(cx + s * 32, cy - 10); x.stroke(); }
      x.beginPath(); x.moveTo(cx - 20, cy + 30); x.lineTo(cx + 20, cy + 30); x.moveTo(cx - 24, cy); x.lineTo(cx + 24, cy); x.stroke();
      x.fillStyle = KV.phos; for (const [mx, my] of [[cx, cy - 50], [cx - 44, cy - 10], [cx + 44, cy - 10]]) { x.beginPath(); x.arc(mx, my, 3, 0, 2 * P); x.fill(); }
      x.restore();
    },
    // The radar's sweep and blips, inside its scope (radius 51.5).
    dial: (x, c, cts) => {
      x.save(); x.shadowColor = KV.phos; x.shadowBlur = 8; x.strokeStyle = KV.phos; x.lineWidth = 1.6; x.beginPath(); x.moveTo(c, c); x.lineTo(c + Math.cos(-0.7) * 44, c + Math.sin(-0.7) * 44); x.stroke();
      x.lineWidth = 1.2; x.beginPath(); for (let i = 0; i <= 60; i++) { const px = c - 36 + i * 72 / 60, py = c + 20 + Math.sin(i / 60 * 6 * P) * 5 * Math.exp(-i / 40); if (i) x.lineTo(px, py); else x.moveTo(px, py); } x.stroke();
      x.fillStyle = '#ff6a50'; x.shadowColor = '#ff6a50'; for (const [dx, dy] of cts) { x.beginPath(); x.arc(c + dx * 34, c + dy * 34, 3, 0, 2 * P); x.fill(); }
      x.restore();
    },
    // Scope cursors: thin phosphor corners with graduation ticks.
    bracket: (x, bx, by, bw, bh) => {
      x.save(); x.shadowColor = KV.phos; x.shadowBlur = 8; x.strokeStyle = KV.phos; x.lineWidth = 2;
      for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
        x.beginPath(); x.moveTo(px + sx * 44, py); x.lineTo(px, py); x.lineTo(px, py + sy * 44); x.stroke();
        x.lineWidth = 1; for (let i = 1; i < 5; i++) { x.beginPath(); x.moveTo(px + sx * i * 9, py); x.lineTo(px + sx * i * 9, py + sy * 5); x.moveTo(px, py + sy * i * 9); x.lineTo(px + sx * 5, py + sy * i * 9); x.stroke(); } x.lineWidth = 2;
      }
      x.restore();
    },
  },
  world: ['Truth is measured, never assumed: every reading shown on an instrument a scientist can check by eye.', 'Careful, many-handed experimenters who like a control that clicks.',
    'Pressed champagne plates over backlit amber windows and phosphor scopes, built to be calibrated.', 'Rounded research hulls studded with sensor dishes and gauge clusters.'],
  shapes: 'Pillowed plates, keys in pressed dimples, a cradle of keys round a scope, meters in bays.', light: 'Amber film lit from behind and from under each lip; phosphor scopes; a soft main light from the upper left.',
  motion: 'A key sinks with a click as its pilot ring lights; needles swing and settle.', type: '"Share Tech Mono" calculator readouts on amber; "Courier Prime" engraved labels on the metal.',
  palette: [['Champagne', KV.champagne, 'Plates', 'The instrument itself'], ['Amber', KV.amber, 'Readout windows', 'A reading, patiently lit'], ['Phosphor', KV.phos, 'Scopes, traces', 'The measurement itself'],
    ['Needle red', KV.needle, 'Needles, alerts', 'The reading that matters'], ['Ink', KV.ink, 'Print on the film', 'The record'], ['Brass', KV.brass, 'Bezels, the crest', 'Craft and calibration']],
});
})();
