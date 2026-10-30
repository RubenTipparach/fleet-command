// The Vhorr Brood skin (mockup 4, OpenSpec change console-skins): an organic race whose consoles are grown, not built
// (owner, 2026-10-30: "an organic alien race, lots of leather shining through light, slimy carapace, tendrils", then "Vhor
// needs special treatment with organic materials and shaders, and organic looking modeling"). The kit is modelled, shaded
// and rendered in Blender (tools/blender/build_ui_vhorr.py: hide with light through it, iridescent carapace, flesh with
// subsurface scattering, blister pods) and inlined into the page; this module adds the skin's tokens and draws only the
// live readouts. Registers itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
const VH = { chitin: '#2b1d2a', chitinHi: '#6a4a68', hide: '#5a3424', hideDark: '#2e1814', flesh: '#c4566a', bile: '#a6e34a', bileHot: '#e4ff9a',
  slime: '#f4f0e0', blood: '#e0342a', pit: '#120a10', ink: '#f3e6d0', dim: '#c9a48c', amber: '#f0b43c' };

// ---- organic shape helpers -------------------------------------------------------------------------------------
// A closed path through points, smoothed by quadratic curves between midpoints (no corners anywhere: nothing is cut).
function smooth(x, pts) {
  const n = pts.length, m = (i) => [(pts[i % n][0] + pts[(i + 1) % n][0]) / 2, (pts[i % n][1] + pts[(i + 1) % n][1]) / 2];
  x.beginPath(); const s = m(0); x.moveTo(s[0], s[1]);
  for (let i = 1; i <= n; i++) { const p = pts[i % n], q = m(i); x.quadraticCurveTo(p[0], p[1], q[0], q[1]); }
  x.closePath();
}
// A blob: an ellipse whose radius breathes with seeded noise.
function blob(cx, cy, rx, ry, R, amp = 0.08, n = 28) {
  const nz = F.noise1(R); const o = [];
  for (let i = 0; i < n; i++) { const a = i / n * 2 * P, k = 1 + nz(a * 1.7) * amp; o.push([cx + Math.cos(a) * rx * k, cy + Math.sin(a) * ry * k]); }
  return o;
}
// Light through hide: a leathery fill, the glow behind it, veins, pores, and a dark rim where the skin thickens.
function hide(x, path, R, box, glow = VH.bile, strength = 0.55) {
  const [x0, y0, x1, y1] = box, w = x1 - x0, h = y1 - y0;
  x.save(); path(); x.clip();
  x.fillStyle = VH.hide; x.fillRect(x0, y0, w, h);
  const g = x.createRadialGradient(x0 + w / 2, y0 + h / 2, 0, x0 + w / 2, y0 + h / 2, Math.max(w, h) * 0.62);
  g.addColorStop(0, hexA(glow, strength)); g.addColorStop(0.55, hexA(VH.flesh, strength * 0.55)); g.addColorStop(1, 'rgba(0,0,0,0)');
  x.fillStyle = g; x.fillRect(x0, y0, w, h);
  veins(x, box, R, Math.max(3, Math.round(w * h / 1800)));
  for (let i = 0; i < w * h / 40; i++) { x.fillStyle = `rgba(30,12,10,${0.08 + R() * 0.12})`; x.beginPath(); x.arc(x0 + R() * w, y0 + R() * h, 0.4 + R() * 0.7, 0, 2 * P); x.fill(); }
  path(); x.strokeStyle = 'rgba(20,8,8,0.55)'; x.lineWidth = 5; x.stroke();
  x.restore();
}
// Branching veins, dark red, thinning as they go.
function veins(x, [x0, y0, x1, y1], R, n) {
  x.strokeStyle = 'rgba(110,20,30,0.55)';
  const grow = (px, py, a, len, w, d) => {
    if (d > 3 || len < 3) return;
    const pts = [[px, py]]; let cx = px, cy = py;
    for (let i = 0; i < 4; i++) { a += (R() - 0.5) * 0.7; cx += Math.cos(a) * len / 4; cy += Math.sin(a) * len / 4; pts.push([cx, cy]); }
    x.lineWidth = w; x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); x.stroke();
    grow(cx, cy, a + 0.6, len * 0.6, w * 0.6, d + 1); grow(cx, cy, a - 0.6, len * 0.55, w * 0.6, d + 1);
  };
  for (let i = 0; i < n; i++) grow(x0 + R() * (x1 - x0), y0 + R() * (y1 - y0), R() * 2 * P, 14 + R() * 26, 1.6, 0);
}
// Wet chitin: a dark plate, a purple rim light, one hard white specular streak, a few cracks.
function chitin(x, path, R, box, hi = 0.75) {
  const [x0, y0, x1, y1] = box;
  x.save(); path(); x.clip();
  const g = x.createLinearGradient(x0, y0, x1, y1); g.addColorStop(0, VH.chitinHi); g.addColorStop(0.45, VH.chitin); g.addColorStop(1, '#0e080e');
  x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.strokeStyle = 'rgba(0,0,0,0.5)'; x.lineWidth = 0.8;
  for (let i = 0; i < 3; i++) { x.beginPath(); let cx = x0 + R() * (x1 - x0), cy = y0 + R() * (y1 - y0); x.moveTo(cx, cy); for (let k = 0; k < 3; k++) { cx += (R() - 0.5) * 10; cy += (R() - 0.5) * 10; x.lineTo(cx, cy); } x.stroke(); }
  x.restore();
  x.save(); path(); x.clip(); path(); x.strokeStyle = 'rgba(190,140,220,0.35)'; x.lineWidth = 3; x.stroke(); x.restore();
  path(); x.strokeStyle = '#0a050a'; x.lineWidth = 1.6; x.stroke();
  // the slime: a bright streak and a few droplets near the top left
  x.save(); path(); x.clip(); x.globalAlpha = hi;
  const sx = x0 + (x1 - x0) * 0.22, sy = y0 + (y1 - y0) * 0.24;
  x.strokeStyle = VH.slime; x.lineWidth = 1.6; x.beginPath(); x.ellipse(sx + 4, sy + 3, (x1 - x0) * 0.22, (y1 - y0) * 0.12, -0.5, P * 1.05, P * 1.55); x.stroke();
  for (let i = 0; i < 3; i++) { x.fillStyle = VH.slime; x.beginPath(); x.arc(sx + R() * (x1 - x0) * 0.3, sy + R() * (y1 - y0) * 0.3, 0.6 + R() * 0.9, 0, 2 * P); x.fill(); }
  x.restore();
}
// A tendril: a tapered, shaded stroke along a curve, with a wet highlight down its back.
function tendril(x, pts, w0, w1, col = VH.flesh) {
  const L = pts.length, left = [], right = [];
  for (let i = 0; i < L; i++) {
    const a = pts[Math.max(0, i - 1)], b = pts[Math.min(L - 1, i + 1)], dx = b[0] - a[0], dy = b[1] - a[1], d = Math.hypot(dx, dy) || 1, w = (w0 + (w1 - w0) * i / (L - 1)) / 2;
    left.push([pts[i][0] - dy / d * w, pts[i][1] + dx / d * w]); right.push([pts[i][0] + dy / d * w, pts[i][1] - dx / d * w]);
  }
  x.beginPath(); x.moveTo(left[0][0], left[0][1]); for (const p of left.slice(1)) x.lineTo(p[0], p[1]); for (const p of right.reverse()) x.lineTo(p[0], p[1]); x.closePath();
  x.fillStyle = col; x.fill(); x.strokeStyle = 'rgba(20,6,8,0.8)'; x.lineWidth = 1; x.stroke();
  x.strokeStyle = 'rgba(255,240,230,0.45)'; x.lineWidth = 0.9; x.beginPath(); x.moveTo(pts[0][0] - 0.6, pts[0][1] - 0.8);
  for (let i = 1; i < L - 1; i++) x.lineTo(pts[i][0] - 0.6, pts[i][1] - 0.8); x.stroke();
}
function curl(cx, cy, r, a0, turns, dir = 1) { const o = []; for (let i = 0; i <= 18; i++) { const t = i / 18, a = a0 + dir * t * turns * 2 * P; o.push([cx + Math.cos(a) * r * (1 - t * 0.75), cy + Math.sin(a) * r * (1 - t * 0.75)]); } return o; }
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
const RNG = (k) => F.rng(F.hash('vhorr.' + k));

// ---- live drawing helpers (the kit itself is rendered in Blender: tools/blender/build_ui_vhorr.py) ---------------
// One pod seen from above, its long axis along x, lit from within.
function podDisc(x, rx, ry, core, R, col = VH.bile) {
  const pts = blob(0, 0, rx, ry, R, 0.07, 18);
  x.save(); smooth(x, pts); x.clip();
  x.fillStyle = VH.hide; x.fillRect(-rx - 4, -ry - 4, 2 * rx + 8, 2 * ry + 8);
  const g = x.createRadialGradient(0, 0, 1, 0, 0, Math.max(rx, ry)); g.addColorStop(0, hexA(col, core)); g.addColorStop(0.6, hexA(VH.flesh, 0.3)); g.addColorStop(1, 'rgba(30,10,12,0.9)');
  x.fillStyle = g; x.fillRect(-rx - 4, -ry - 4, 2 * rx + 8, 2 * ry + 8); veins(x, [-rx, -ry, rx, ry], R, 2); x.restore();
  smooth(x, pts); x.strokeStyle = '#1a0a0c'; x.lineWidth = 2; x.stroke();
  x.fillStyle = 'rgba(255,248,235,0.7)'; x.beginPath(); x.ellipse(-rx * 0.4, -ry * 0.45, rx * 0.22, ry * 0.14, -0.4, 0, 2 * P); x.fill();
}
// ---- the skin --------------------------------------------------------------------------------------------------
F.register('vhorr', {
  name: 'Vhorr Brood', kind: 'Grown in Blender: hide lit from behind, wet iridescent carapace, tendrils, sinew', bg: '#0d0709',
  css: { '--s-ink': VH.ink, '--s-dim': VH.dim, '--s-num': VH.bile, '--s-body': '"Barlow Condensed", sans-serif', '--btn-ink': VH.ink, '--btn-shadow': '0 1px 2px #000, 0 0 5px #1a0a0c', '--btn-justify': 'center',
    '--btn-font': '400 20px/1 "Metamorphous", serif', '--btn-track': '0.02em', '--btn-case': 'none', '--btn-motion': 'transform 0.2s cubic-bezier(.3,1.7,.5,1)', '--btn-press': 'scale(0.97, 0.9)',
    '--tab-font': '400 17px/1 "Metamorphous", serif', '--tab-ink': VH.dim, '--tab-ink-on': VH.bileHot, '--title-font': '400 21px/1 "Metamorphous", serif', '--title-ink': VH.bileHot,
    '--lbl-font': '600 17px/1 "Barlow Condensed", sans-serif', '--num-font': '600 27px/1 "Barlow Condensed", sans-serif' },
  accent: VH.bile, warn: VH.blood,
  title: (t, m) => { t.style.cssText = `left:${44 - m[3]}px;top:${-m[0] + 6}px;padding:1px 10px 3px;border-radius:12px;background:#1a0c0e;box-shadow:0 0 0 2px #140808;text-shadow:0 0 6px ${VH.bile};`; },
  plateText: { left: 82, top: 9, width: 300, height: 38, font: '400 21px/1 "Metamorphous", serif', color: VH.ink, sub: 'Sskarr · Matriarch Vhaal' },
  helmKeys: 'ring', helmR: 100, shield: [VH.bile, VH.amber, VH.blood],
  fonts: ['400 20px "Metamorphous"', '600 20px "Barlow Condensed"'], view: { title: '400 28px "Metamorphous"', body: '600 25px "Barlow Condensed"', upper: false },
  build: () => { const d = F.kitData('assets/ui/vhorr/manifest.json'); return d ? F.fromManifest(d, () => F.paint(48, 48, () => {})) : null; },
  live: {
    // A talon pointing the way.
    arrow: (x) => { x.fillStyle = VH.slime; x.strokeStyle = '#1a0a0c'; x.lineWidth = 1.4; x.beginPath(); x.moveTo(13, 0); x.quadraticCurveTo(0, -3, -9, -9); x.quadraticCurveTo(-4, 0, -9, 9); x.quadraticCurveTo(0, 3, 13, 0); x.fill(); x.stroke(); },
    // A living hull: carapace segments over glowing flesh, trailing tendrils; shields are membranes.
    ssd: (x, cx, cy, sh, col) => {
      for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.save(); x.shadowColor = col(sh[i]); x.shadowBlur = 10; x.strokeStyle = hexA(col(sh[i]), 0.75); x.lineWidth = 9; x.beginPath(); x.arc(cx, cy, 100, a - P / 6 + 0.1, a + P / 6 - 0.1); x.stroke(); x.restore(); }
      for (const s of [-1, 1]) tendril(x, [[cx + s * 10, cy + 40], [cx + s * 22, cy + 62], [cx + s * 14, cy + 84]], 5, 1, VH.flesh);
      const R = RNG('ssd.ship');
      x.save(); x.translate(cx, cy); x.rotate(P / 2); podDisc(x, 62, 30, 0.75, R); x.restore();
      for (let i = 0; i < 4; i++) { x.save(); x.translate(cx, cy - 40 + i * 22); chitin(x, () => smooth(x, [[-24, 0], [0, -9], [24, 0], [0, 7]]), R, [-24, -9, 24, 8], 0.5); x.restore(); }
      for (const [mx, my] of [[cx, cy - 58], [cx - 26, cy + 4], [cx + 26, cy + 4]]) { x.save(); x.shadowColor = VH.bileHot; x.shadowBlur = 8; x.fillStyle = VH.bileHot; x.beginPath(); x.arc(mx, my, 5, 0, 2 * P); x.fill(); x.restore(); }
    },
    // A pulse: rings spreading from the heart, contacts as red spores.
    dial: (x, c, cts) => {
      for (const [r, a] of [[14, 0.7], [26, 0.45], [36, 0.25]]) { x.strokeStyle = hexA(VH.bile, a); x.lineWidth = 2; x.beginPath(); x.arc(c, c, r, 0, 2 * P); x.stroke(); }
      for (const [dx, dy] of cts) { x.save(); x.shadowColor = VH.blood; x.shadowBlur = 8; x.fillStyle = VH.blood; x.beginPath(); x.arc(c + dx * 32, c + dy * 32, 4, 0, 2 * P); x.fill(); x.restore(); }
    },
    // Four tendrils hooking round the target.
    bracket: (x, bx, by, bw, bh) => {
      for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
        x.save(); x.translate(px, py); x.scale(sx, sy); x.shadowColor = VH.bile; x.shadowBlur = 10;
        tendril(x, [[50, -2], [20, -2], [2, 4], [-2, 22], [6, 44]], 2, 6, VH.bile); tendril(x, curl(52, 6, 8, -P / 2, 0.7, 1), 3, 0.5, VH.bile); x.restore();
      }
    },
  },
  world: ['The brood is one body: it grows, consumes and adapts.', 'Grown, not built: hide, chitin, membranes, nerve tendrils.', 'Living tissue: light is bile glowing through skin, armour is shed carapace.', 'Living hulls of carapace plates over glowing flesh.'],
  shapes: 'Blister pods, shells, ribs, tendrils and blobs. Nothing straight, nothing cut.', light: 'From within: bile glowing through hide; wet specular highlights on chitin.',
  motion: 'A pod squeezes flat when pressed and swells back.', type: '"Metamorphous" for keys and titles over "Barlow Condensed" numbers.',
  palette: [['Chitin', VH.chitin, 'Plates, claws', 'Armour grown, not forged'], ['Hide', VH.hide, 'Frames, membranes', 'The brood\'s one skin'], ['Flesh', VH.flesh, 'Tendrils, tissue', 'Hunger'],
    ['Bile', VH.bile, 'Light, readouts', 'Growth: the light inside the body'], ['Slime', VH.slime, 'Wet highlights', 'The newly grown'], ['Blood', VH.blood, 'Alerts', 'A wound']],
});
})();
