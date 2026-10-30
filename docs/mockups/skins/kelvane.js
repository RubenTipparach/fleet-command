// The Kelvane Institute skin (mockup 4, OpenSpec change console-skins): a race of experimenters who trust only what an
// instrument can show (owner, 2026-10-30: "the old science, nucleo nlog skin from winamp ... osciloscope aesthetic from
// science labs", then "Kelvane 100% can be modeled, let and rendered just take the osciloscope dash board for
// inspiration, old school electircal dashboards, dials, switches etc"). The kit is modelled, lit and rendered in Blender
// (tools/blender/build_ui_kelvane.py) and inlined into the page; this module adds the skin's tokens and draws only the
// live readouts (phosphor traces on the scopes). Registers itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
const KV = { cream: '#e9dcc0', creamLo: '#b9a27a', chrome: '#cfd3d6', amber: '#ffcf6a', amberLo: '#d9a94a', phos: '#4dff88', needle: '#c8302a', ink: '#2e2416', dim: '#6a5634', crt: '#06140c' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
const RNG = (k) => F.rng(F.hash('kelvane.' + k));
const rrect = (x, X, Y, W, H, r) => () => { x.beginPath(); x.roundRect(X, Y, W, H, r); };

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('kelvane', {
  name: 'Kelvane Institute', kind: 'A lab dashboard modelled in Blender: enamel, chrome, dials, toggles, phosphor scopes', bg: '#24211c', sampleBg: KV.cream,
  css: { '--s-ink': KV.ink, '--s-dim': KV.dim, '--s-num': '#7a1e0e', '--s-body': '"Courier Prime", monospace', '--btn-ink': KV.ink, '--btn-shadow': '0 1px 0 rgba(255,255,255,0.6)', '--btn-justify': 'center',
    '--btn-font': '700 15px/1 "Courier Prime", monospace', '--btn-track': '0.1em', '--btn-case': 'uppercase', '--btn-motion': 'none', '--btn-press': 'translateY(1px)',
    '--tab-font': '700 12px/1 "Courier Prime", monospace', '--tab-ink': KV.dim, '--tab-ink-on': KV.ink, '--title-font': '700 16px/1 "Courier Prime", monospace', '--title-ink': KV.ink,
    '--lbl-font': '700 16px/1 "Courier Prime", monospace', '--num-font': '400 28px/1 "Share Tech Mono", monospace', '--bt-rep': 'round', '--bf-rep': 'round' },
  accent: KV.phos, warn: KV.needle,
  title: (t, m) => { t.style.cssText = `left:${30 - m[3]}px;top:${-m[0] + 5}px;letter-spacing:0.16em;text-shadow:0 1px 0 rgba(255,255,255,0.7);`; },
  plateText: { left: 54, top: 12, width: 290, height: 32, font: '700 17px/1 "Courier Prime", monospace', color: KV.ink, sub: 'RV PATIENCE · DR. ORREL' },
  helmKeys: 'ring', helmR: 92, shield: [KV.phos, KV.amber, '#ff4a3a'],
  fonts: ['700 20px "Courier Prime"', '400 20px "Share Tech Mono"'], view: { title: '400 26px "Share Tech Mono"', body: '400 24px "Share Tech Mono"', upper: true, ink: '#8affb0', dim: KV.amber },
  build: () => { const d = F.kitData('assets/ui/kelvane/manifest.json'); return d ? F.fromManifest(d, () => F.paint(48, 48, () => {})) : null; },
  live: {
    arrow: (x) => { x.fillStyle = KV.ink; x.beginPath(); x.moveTo(9, 0); x.lineTo(-5, -7); x.lineTo(-5, 7); x.closePath(); x.fill(); },
    helmCentre: (x, c) => { x.fillStyle = KV.ink; x.font = '700 13px "Courier Prime"'; x.textAlign = 'center'; x.fillText('STOP', c, c + 5); },
    // The ship as a phosphor trace on the scope: a rounded research hull with dishes; shields as dotted trace arcs.
    ssd: (x, cx, cy, sh, col) => {
      x.save(); x.shadowBlur = 6;
      for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.shadowColor = col(sh[i]); x.strokeStyle = col(sh[i]); x.lineWidth = 2.4; x.setLineDash([2, 3]); x.beginPath(); x.arc(cx, cy, 96, a - P / 6 + 0.08, a + P / 6 - 0.08); x.stroke(); }
      x.setLineDash([]); x.shadowColor = KV.phos; x.strokeStyle = KV.phos; x.lineWidth = 1.8;
      x.beginPath(); x.ellipse(cx, cy, 30, 62, 0, 0, 2 * P); x.stroke();
      for (const s of [-1, 1]) { x.beginPath(); x.arc(cx + s * 44, cy - 10, 12, 0, 2 * P); x.stroke(); x.beginPath(); x.moveTo(cx + s * 30, cy - 10); x.lineTo(cx + s * 32, cy - 10); x.stroke(); }
      x.beginPath(); x.moveTo(cx - 20, cy + 30); x.lineTo(cx + 20, cy + 30); x.moveTo(cx - 24, cy); x.lineTo(cx + 24, cy); x.stroke();
      x.fillStyle = KV.phos; for (const [mx, my] of [[cx, cy - 50], [cx - 44, cy - 10], [cx + 44, cy - 10]]) { x.beginPath(); x.arc(mx, my, 3, 0, 2 * P); x.fill(); }
      x.restore();
    },
    // A sweep trace and phosphor blips.
    dial: (x, c, cts) => {
      x.save(); x.shadowColor = KV.phos; x.shadowBlur = 8; x.strokeStyle = KV.phos; x.lineWidth = 1.6; x.beginPath(); x.moveTo(c, c); x.lineTo(c + Math.cos(-0.7) * 46, c + Math.sin(-0.7) * 46); x.stroke();
      x.lineWidth = 1.2; x.beginPath(); for (let i = 0; i <= 60; i++) { const px = c - 40 + i * 80 / 60, py = c + 22 + Math.sin(i / 60 * 6 * P) * 5 * Math.exp(-i / 40); if (i) x.lineTo(px, py); else x.moveTo(px, py); } x.stroke();
      x.fillStyle = '#ff6a50'; x.shadowColor = '#ff6a50'; for (const [dx, dy] of cts) { x.beginPath(); x.arc(c + dx * 36, c + dy * 36, 3, 0, 2 * P); x.fill(); }
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
  world: ['Truth is measured, never assumed: every reading shown on an instrument a scientist can check by eye.', 'Careful, many-handed experimenters who like a control that clicks.', 'Cream-enamelled instruments, chrome, brass and valves, built to be calibrated.', 'Rounded research hulls studded with sensor dishes and gauge clusters.'],
  shapes: 'Faceplates, domed push buttons, toggles, ruled scales, round scopes.', light: 'A warm lamp above the bench; amber backlit displays; phosphor-green traces.',
  motion: 'A dome sinks with a click; needles swing and settle.', type: '"Courier Prime" engraved labels over "Share Tech Mono" readouts.',
  palette: [['Enamel', KV.cream, 'Faceplates', 'The steady bench'], ['Chrome', KV.chrome, 'Bezels, buttons', 'Precision'], ['Amber', KV.amber, 'Displays', 'Warm patient light'],
    ['Phosphor', KV.phos, 'Scopes, traces', 'The measurement itself'], ['Needle red', KV.needle, 'Needles, alerts', 'The reading that matters'], ['Ink', KV.ink, 'Engraved labels', 'The record']],
});
})();
