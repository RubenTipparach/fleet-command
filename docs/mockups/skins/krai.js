// The Krai Warhost skin (mockup 4, OpenSpec change console-skins): a combat race (owner, 2026-10-30: "a combat focused
// race, lots of triangular motifs, angry red colors and orange lit buttons that are activated. strong metal framing,
// almost blade like"). Black blade steel with honed bright edges, red enamel, triangles everywhere; keys are blades,
// dark until engaged, then lit hot orange. Kept apart from the Kethran Hegemony (gunmetal plates, wedge keycaps,
// amber chevrons): thinner, sharper, black and red. Painted in canvas, deterministic. Registers itself with the
// page's skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
const KR = { black: '#15100f', steel: '#2a2526', edge: '#c9d0d8', blood: '#b3121b', red: '#ff2a2a', orange: '#ff7a1a', hot: '#ffb070', bone: '#e8dccb', dim: '#a8908a' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
function poly(x, pts) { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); x.closePath(); }

// ---- materials ---------------------------------------------------------------------------------------------------
// Blade steel: near-black, brushed along its length, with a honed edge that catches a hard white line of light.
function steel(x, pts, box, o = {}) {
  const [x0, y0, x1, y1] = box;
  x.save(); poly(x, pts); x.clip();
  const g = x.createLinearGradient(x0, y0, x0, y1); g.addColorStop(0, o.top || '#3a3335'); g.addColorStop(0.5, KR.steel); g.addColorStop(1, KR.black); x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.globalAlpha = 0.06; for (let y = y0; y < y1; y += 2) { x.fillStyle = (y / 2) % 2 ? '#ffffff' : '#000000'; x.fillRect(x0, y, x1 - x0, 1); } x.globalAlpha = 1;
  x.restore();
  // the honed edge: the upper and left faces catch the light
  x.save(); x.lineWidth = 1.3; x.strokeStyle = hexA(KR.edge, o.edge ?? 0.85);
  for (let i = 0; i < pts.length; i++) { const a = pts[i], b = pts[(i + 1) % pts.length], nx = b[1] - a[1], ny = a[0] - b[0]; if (ny < -0.1 || nx < -0.1 && ny <= 0.1) { x.beginPath(); x.moveTo(a[0], a[1]); x.lineTo(b[0], b[1]); x.stroke(); } }
  x.restore();
  poly(x, pts); x.strokeStyle = '#050303'; x.lineWidth = 1; x.stroke();
}
function enamel(x, pts, col = KR.blood, glow = 0) {
  x.save(); if (glow) { x.shadowColor = col; x.shadowBlur = glow; } poly(x, pts);
  const [xs, ys] = [pts.map((p) => p[0]), pts.map((p) => p[1])], g = x.createLinearGradient(0, Math.min(...ys), 0, Math.max(...ys));
  g.addColorStop(0, col === KR.blood ? '#d8232c' : KR.hot); g.addColorStop(1, col); x.fillStyle = g; x.fill(); x.restore();
  poly(x, pts); x.strokeStyle = 'rgba(0,0,0,0.6)'; x.lineWidth = 0.8; x.stroke();
}
const RNG = (k) => F.rng(F.hash('krai.' + k));

// ---- the nine-slice pieces --------------------------------------------------------------------------------------
// A frame of blades: a black steel band with a red inlay; each corner a swept blade with a red triangle cut through it.
function frame(x, w, h, b, c) {
  const band = [[0, 0], [w, 0], [w, h], [0, h]];
  x.fillStyle = '#0b0808'; x.fillRect(b, b, w - 2 * b, h - 2 * b);
  x.save(); x.beginPath(); x.rect(0, 0, w, h); x.rect(b, b, w - 2 * b, h - 2 * b); x.clip('evenodd'); steel(x, band, [0, 0, w, h]); x.restore();
  x.strokeStyle = KR.blood; x.lineWidth = 2; x.strokeRect(b * 0.45, b * 0.45, w - b * 0.9, h - b * 0.9);
  x.strokeStyle = '#050303'; x.lineWidth = 1; x.strokeRect(b, b, w - 2 * b, h - 2 * b);
  for (const [sx, sy] of [[1, 1], [-1, 1], [1, -1], [-1, -1]]) {
    x.save(); x.translate(sx > 0 ? 0 : w, sy > 0 ? 0 : h); x.scale(sx, sy);
    steel(x, [[0, 0], [c, 0], [b + 2, b * 0.55], [b * 0.55, b + 2], [0, c]], [0, 0, c, c], { top: '#4a4244' });
    enamel(x, [[5, 5], [c * 0.55, 5], [5, c * 0.55]], KR.red, 4);
    if (c > 30) steel(x, [[b - 2, b - 2], [c + 2, b + 2], [b + 6, b + 6], [b + 2, c + 2]], [b - 2, b - 2, c + 2, c + 2], { top: '#5a5052' });
    x.restore();
  }
}
// The blade key: a slanted back, a pointed tip; dark until engaged, then lit hot orange from within.
function bladeKey(state) {
  return (x, w, h) => {
    const pts = [[18, 3], [w - 30, 3], [w - 2, h / 2], [w - 30, h - 3], [4, h - 3]];
    if (state === 'pressed') {
      x.save(); x.shadowColor = KR.orange; x.shadowBlur = 12; poly(x, pts);
      const g = x.createLinearGradient(0, 3, 0, h - 3); g.addColorStop(0, KR.hot); g.addColorStop(0.55, KR.orange); g.addColorStop(1, '#b8360a'); x.fillStyle = g; x.fill(); x.restore();
      x.strokeStyle = '#fff1dc'; x.lineWidth = 1.3; x.beginPath(); x.moveTo(18, 3.5); x.lineTo(w - 30, 3.5); x.lineTo(w - 2, h / 2); x.stroke();
      poly(x, pts); x.strokeStyle = '#3a0a04'; x.lineWidth = 1; x.stroke();
    } else {
      steel(x, pts, [0, 0, w, h]);
      x.save(); if (state === 'hover') { x.shadowColor = KR.red; x.shadowBlur = 10; }
      x.strokeStyle = state === 'hover' ? KR.red : hexA(KR.blood, 0.9); x.lineWidth = state === 'hover' ? 2.2 : 1.4;
      x.beginPath(); x.moveTo(8, h - 6); x.lineTo(w - 32, h - 6); x.lineTo(w - 10, h / 2 + 2); x.stroke(); x.restore();
    }
    enamel(x, [[6, h / 2 - 7], [14, h / 2], [6, h / 2 + 7]], state === 'pressed' ? '#3a0a04' : KR.red, state === 'hover' ? 6 : 0);
  };
}
// A tab: a steel bar with fangs hanging from both ends; the active one lit red along its edge.
function fangTab(active) {
  return (x, w, h) => {
    steel(x, [[2, 4], [w - 2, 4], [w - 2, h - 12], [2, h - 12]], [0, 0, w, h]);
    for (const s of [1, -1]) { x.save(); if (s < 0) { x.translate(w, 0); x.scale(-1, 1); } steel(x, [[2, h - 13], [12, h - 13], [5, h - 1]], [0, h - 14, 12, h]); x.restore(); }
    x.save(); if (active) { x.shadowColor = KR.red; x.shadowBlur = 8; } x.fillStyle = active ? KR.red : hexA(KR.blood, 0.6); x.fillRect(14, h - 11, w - 28, 2.5); x.restore();
  };
}
// A strip: a long blade rail with a red inlay, a great swept blade at each end.
function rail(flip) {
  return (x, w, h) => {
    if (flip) { x.translate(0, h); x.scale(1, -1); }
    steel(x, [[0, h - 18], [w, h - 18], [w, h - 4], [0, h - 4]], [0, h - 18, w, h - 4]);
    x.fillStyle = KR.blood; x.fillRect(0, h - 12, w, 2);
    for (const s of [1, -1]) {
      x.save(); if (s < 0) { x.translate(w, 0); x.scale(-1, 1); }
      steel(x, [[0, 2], [24, 2], [96, h - 18], [96, h - 4], [0, h - 4]], [0, 0, 96, h], { top: '#4a4244' });
      enamel(x, [[10, 10], [26, 10], [64, h - 20], [10, h - 20]], KR.blood);
      enamel(x, [[14, 16], [22, 16], [14, 30]], KR.red, 5);
      x.restore();
    }
  };
}
const nine = {
  'frame.panel': { size: [170, 130], m: [42, 42, 42, 42], draw: (x, w, h) => frame(x, w, h, 17, 42) },
  'frame.inset': { size: [64, 48], m: [14, 14, 14, 14], draw: (x, w, h) => frame(x, w, h, 5, 14) },
  'bar.track': { size: [96, 16], m: [0, 14, 0, 14], inset: [4, 3], draw: (x, w, h) => {
    steel(x, [[6, 0], [w, 0], [w - 6, h], [0, h]], [0, 0, w, h]); x.fillStyle = '#080505'; x.fillRect(5, 3, w - 10, h - 6);
    for (const px of [3, w - 3]) enamel(x, [[px - 2, 3], [px + 2, h / 2], [px - 2, h - 3]], KR.blood); } },
  'bar.fill': { size: [64, 10], m: [0, 10, 0, 4], draw: (x, w, h) => fillBlade(x, w, h, KR.orange) },
  'bar.fill.red': { size: [64, 10], m: [0, 10, 0, 4], draw: (x, w, h) => fillBlade(x, w, h, KR.red) },
  'button.idle': { size: [150, 48], m: [20, 32, 20, 20], draw: bladeKey('idle') },
  'button.hover': { size: [150, 48], m: [20, 32, 20, 20], draw: bladeKey('hover') },
  'button.pressed': { size: [150, 48], m: [20, 32, 20, 20], draw: bladeKey('pressed') },
  'tab.idle': { size: [120, 40], m: [14, 14, 16, 14], draw: fangTab(false) },
  'tab.active': { size: [120, 40], m: [14, 14, 16, 14], draw: fangTab(true) },
  'strip.top': { size: [512, 56], m: [10, 100, 10, 100], draw: rail(false) },
  'strip.bottom': { size: [512, 56], m: [10, 100, 10, 100], draw: rail(true) },
  'divider': { size: [128, 10], m: [0, 14, 0, 14], draw: (x, w, h) => { x.fillStyle = KR.blood; x.fillRect(10, h / 2 - 1, w - 20, 2); enamel(x, [[0, 0], [10, h / 2], [0, h]], KR.red); enamel(x, [[w, 0], [w - 10, h / 2], [w, h]], KR.red); } },
};
function fillBlade(x, w, h, col) {
  x.save(); x.shadowColor = col; x.shadowBlur = 6; poly(x, [[0, 0], [w - 8, 0], [w, h / 2], [w - 8, h], [0, h]]);
  const g = x.createLinearGradient(0, 0, 0, h); g.addColorStop(0, KR.hot); g.addColorStop(0.5, col); g.addColorStop(1, '#7a1a08'); x.fillStyle = g; x.fill(); x.restore();
}

// ---- the hero pieces ---------------------------------------------------------------------------------------------
const hero = {
  // A blade plate: angled ends, a red triangle of rank, a black field for the name.
  'nameplate': (x, w, h) => {
    steel(x, [[16, 1], [w - 4, 1], [w - 20, h - 1], [1, h - 1]], [0, 0, w, h]);
    enamel(x, [[10, 8], [56, 8], [33, h - 8]], KR.blood); enamel(x, [[22, 14], [44, 14], [33, h - 18]], KR.red, 6);
    x.fillStyle = '#080505'; poly(x, [[66, 9], [w - 22, 9], [w - 32, h - 9], [62, h - 9]]); x.fill(); x.strokeStyle = KR.blood; x.lineWidth = 1.2; x.stroke();
  },
  // An eight-pointed blade star of steel; eight blade keys point outward between its spikes, a red triangle at its heart.
  'helm.pad': (x, w, h) => {
    const c = w / 2, star = [];
    for (let i = 0; i < 16; i++) { const a = -P / 2 + P / 8 + i * P / 8, r = i % 2 ? 98 : 148; star.push([c + Math.cos(a) * r, c + Math.sin(a) * r]); }
    steel(x, star, [0, 0, w, h], { top: '#3a3335' });
    for (let i = 0; i < 8; i++) { const a = -P / 2 + P / 8 + i * P / 4; x.strokeStyle = KR.blood; x.lineWidth = 2; x.beginPath(); x.moveTo(c + Math.cos(a) * 60, c + Math.sin(a) * 60); x.lineTo(c + Math.cos(a) * 140, c + Math.sin(a) * 140); x.stroke(); }
    for (let i = 0; i < 8; i++) {
      const a = -P / 2 + i * P / 4; x.save(); x.translate(c + Math.cos(a) * 96, c + Math.sin(a) * 96); x.rotate(a);
      steel(x, [[-20, -18], [24, 0], [-20, 18], [-12, 0]], [-20, -18, 24, 18], { top: '#4a4244' }); x.restore();
    }
    enamel(x, [[c, c - 46], [c + 40, c + 24], [c - 40, c + 24]], KR.blood, 0); enamel(x, [[c, c - 30], [c + 26, c + 15], [c - 26, c + 15]], KR.red, 10);
  },
  // A heavy blade frame round a black field.
  'ssd.bezel': (x, w, h) => frame(x, w, h, 16, 44),
  // A hexagonal housing of steel and red teeth.
  'dial': (x, w) => {
    const c = w / 2, hex = [0, 1, 2, 3, 4, 5].map((i) => [c + Math.cos(i * P / 3) * 58, c + Math.sin(i * P / 3) * 58]);
    steel(x, hex, [0, 0, w, w]); x.fillStyle = '#080505'; poly(x, hex.map(([px, py]) => [c + (px - c) * 0.76, c + (py - c) * 0.76])); x.fill();
    for (let i = 0; i < 6; i++) { const a = i * P / 3 + P / 6; enamel(x, [[c + Math.cos(a) * 52, c + Math.sin(a) * 52], [c + Math.cos(a - 0.12) * 44, c + Math.sin(a - 0.12) * 44], [c + Math.cos(a + 0.12) * 44, c + Math.sin(a + 0.12) * 44]], KR.blood); }
  },
  'lamp.alert.green': (x, w) => warnTri(x, w, '#5aff7a'), 'lamp.alert.yellow': (x, w) => warnTri(x, w, '#ffc23a'), 'lamp.alert.red': (x, w) => warnTri(x, w, KR.red),
  // The Warhost's mark (original): a broad falling blade between two fangs, in a triangle of steel.
  'crest': (x, w) => {
    const c = w / 2; steel(x, [[c, 4], [w - 4, w - 10], [4, w - 10]], [0, 0, w, w]);
    enamel(x, [[c - 9, 30], [c + 9, 30], [c, 78]], KR.red, 8); enamel(x, [[c - 26, 52], [c - 15, 52], [c - 20, 74]], KR.blood); enamel(x, [[c + 15, 52], [c + 26, 52], [c + 20, 74]], KR.blood);
  },
};
function warnTri(x, w, col) {
  const c = w / 2; steel(x, [[c, 2], [w - 2, w - 6], [2, w - 6]], [0, 0, w, w]);
  x.save(); x.shadowColor = col; x.shadowBlur = 14; poly(x, [[c, 16], [w - 15, w - 13], [15, w - 13]]); const g = x.createLinearGradient(0, 16, 0, w - 13); g.addColorStop(0, '#ffffff'); g.addColorStop(0.35, col); g.addColorStop(1, hexA(col, 0.6)); x.fillStyle = g; x.fill(); x.restore();
}

// ---- icons: triangles and blades only ---------------------------------------------------------------------------
function icon(name) {
  return F.paint(48, 48, (x) => {
    const T = (pts, col = KR.red) => { x.save(); x.shadowColor = col; x.shadowBlur = 4; poly(x, pts); x.fillStyle = col; x.fill(); x.restore(); x.strokeStyle = hexA(KR.edge, 0.7); x.lineWidth = 0.8; x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); x.lineTo(pts[1][0], pts[1][1]); x.stroke(); };
    ({
      helm: () => { T([[24, 3], [40, 40], [24, 30], [8, 40]]); },
      fire: () => { T([[4, 8], [30, 22], [4, 20]]); T([[4, 28], [30, 26], [4, 40]]); T([[30, 18], [46, 24], [30, 30]], KR.orange); },
      shields: () => { T([[4, 6], [44, 6], [24, 44]]); x.fillStyle = '#15100f'; poly(x, [[14, 12], [34, 12], [24, 32]]); x.fill(); },
      tractor: () => { T([[2, 10], [20, 24], [2, 38]]); T([[46, 10], [28, 24], [46, 38]]); },
      transport: () => { T([[8, 4], [40, 4], [24, 16]]); T([[12, 18], [36, 18], [24, 28]], KR.orange); T([[16, 30], [32, 30], [24, 38]], KR.blood); },
      science: () => { T([[24, 4], [46, 42], [2, 42]]); x.fillStyle = '#15100f'; poly(x, [[24, 22], [30, 31], [24, 38], [18, 31]]); x.fill(); },
      comms: () => { T([[4, 20], [14, 24], [4, 28]]); T([[18, 12], [32, 24], [18, 36]]); T([[34, 4], [46, 24], [34, 44]], KR.orange); },
      repair: () => { T([[4, 6], [10, 4], [44, 40], [40, 44]]); T([[44, 6], [38, 4], [4, 40], [8, 44]], KR.orange); },
      map: () => { T([[24, 2], [29, 19], [19, 19]]); T([[24, 46], [19, 29], [29, 29]]); T([[2, 24], [19, 19], [19, 29]]); T([[46, 24], [29, 29], [29, 19]]); },
      settings: () => { T([[24, 4], [44, 40], [4, 40]]); x.fillStyle = '#15100f'; for (const [px, py] of [[24, 18], [32, 32], [16, 32]]) { poly(x, [[px, py - 4], [px + 4, py + 3], [px - 4, py + 3]]); x.fill(); } },
    })[name]();
  });
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('krai', {
  name: 'Krai Warhost', kind: 'Black blade steel and red enamel; keys lit orange when engaged', bg: '#0a0707',
  css: { '--s-ink': KR.bone, '--s-dim': KR.dim, '--s-num': KR.orange, '--s-body': '"Teko", sans-serif', '--btn-ink': KR.bone, '--btn-shadow': '0 1px 2px #000, 0 0 3px #000', '--btn-justify': 'center',
    '--btn-font': '400 18px/1 "Russo One", sans-serif', '--btn-track': '0.06em', '--btn-case': 'uppercase', '--btn-motion': 'none', '--btn-press': 'translateX(2px)',
    '--tab-font': '400 14px/1 "Russo One", sans-serif', '--tab-ink': KR.dim, '--tab-ink-on': KR.bone, '--title-font': '400 18px/1 "Russo One", sans-serif', '--title-ink': KR.red,
    '--lbl-font': '500 19px/1 "Teko", sans-serif', '--num-font': '600 32px/1 "Teko", sans-serif' },
  accent: KR.orange, warn: KR.red,
  title: (t, m) => { t.style.cssText = `left:${40 - m[3]}px;top:${-m[0] + 7}px;letter-spacing:0.1em;text-shadow:0 1px 0 #000;`; },
  plateText: { left: 76, top: 10, width: 330, height: 36, font: '400 20px/1 "Russo One", sans-serif', color: KR.bone, sub: 'KHARZ · WARLORD UTREK' },
  helmKeys: 'ring', helmR: 96, shield: [KR.orange, '#ffc23a', KR.red],
  fonts: ['400 20px "Russo One"', '500 20px "Teko"', '600 20px "Teko"'], view: { title: '400 26px "Russo One"', body: '500 28px "Teko"', upper: true },
  build: () => F.fromPainters(nine, hero, icon, false),
  live: {
    arrow: (x) => { x.fillStyle = KR.bone; poly(x, [[12, 0], [-6, -6], [-2, 0], [-6, 6]]); x.fill(); x.strokeStyle = KR.blood; x.lineWidth = 1; x.stroke(); },
    helmCentre: (x, c) => { x.fillStyle = KR.bone; x.font = '400 13px "Russo One"'; x.textAlign = 'center'; x.fillText('HOLD', c, c + 8); },
    // A knife-blade hull swept back into wings; shields as rows of red teeth.
    ssd: (x, cx, cy, sh, col) => {
      for (let i = 0; i < 6; i++) { const a0 = -P / 2 + i * P / 3 - P / 6 + 0.1, a1 = a0 + P / 3 - 0.2; x.fillStyle = col(sh[i]); for (let k = 0; k <= 6; k++) { const a = a0 + (a1 - a0) * k / 6; poly(x, [[cx + Math.cos(a) * 98, cy + Math.sin(a) * 98], [cx + Math.cos(a + 0.04) * 110, cy + Math.sin(a + 0.04) * 110], [cx + Math.cos(a - 0.04) * 110, cy + Math.sin(a - 0.04) * 110]]); x.fill(); } }
      steel(x, [[cx, cy - 82], [cx + 10, cy + 10], [cx + 50, cy + 56], [cx + 12, cy + 44], [cx, cy + 66], [cx - 12, cy + 44], [cx - 50, cy + 56], [cx - 10, cy + 10]], [cx - 50, cy - 82, cx + 50, cy + 66]);
      enamel(x, [[cx, cy - 50], [cx + 5, cy + 10], [cx - 5, cy + 10]], KR.red, 6);
      for (const [mx, my] of [[cx, cy - 70], [cx - 36, cy + 46], [cx + 36, cy + 46]]) enamel(x, [[mx, my - 6], [mx + 5, my + 4], [mx - 5, my + 4]], KR.orange, 6);
    },
    // A thin wedge sweeps; contacts are red triangles.
    dial: (x, c, cts) => {
      x.save(); x.shadowColor = KR.orange; x.shadowBlur = 8; x.fillStyle = hexA(KR.orange, 0.55); poly(x, [[c, c], [c + Math.cos(-1.1) * 40, c + Math.sin(-1.1) * 40], [c + Math.cos(-0.8) * 40, c + Math.sin(-0.8) * 40]]); x.fill(); x.restore();
      for (const [dx, dy] of cts) { const px = c + dx * 32, py = c + dy * 32; enamel(x, [[px, py - 5], [px + 5, py + 4], [px - 5, py + 4]], KR.red, 6); }
    },
    // Four red spikes stabbing at the target's corners.
    bracket: (x, bx, by, bw, bh) => {
      for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
        x.save(); x.translate(px, py); x.scale(sx, sy); enamel(x, [[-8, -8], [48, 0], [8, 6], [0, 48]], KR.red, 10); x.restore();
      }
    },
  },
  world: ['War is the only honest test; rank is won in blood.', 'Lean predators with long claws: short, sharp gestures.', 'Forged steel, edged everything; nothing round survives the forge.', 'Knife-blade hulls and swept triangles.'],
  shapes: 'Blades, fangs, spikes and triangles; nothing round.', light: 'Hard white light along honed edges; red enamel; keys dark until engaged, then lit orange.',
  motion: 'A key thrusts forward 2 px when struck.', type: '"Russo One" for keys and titles over "Teko" numbers.',
  palette: [['Black steel', KR.black, 'Frames, keys', 'The forge'], ['Edge', KR.edge, 'Honed highlights', 'Readiness'], ['Blood', KR.blood, 'Inlays', 'Honour paid in blood'],
    ['Red', KR.red, 'Titles, warnings', 'Rage'], ['Orange', KR.orange, 'Engaged keys, readouts', 'The strike'], ['Bone', KR.bone, 'Text', 'Trophies']],
});
})();
