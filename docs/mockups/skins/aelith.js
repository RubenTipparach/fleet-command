// The Aelith Conclave skin (mockup 4, OpenSpec change console-skins): a wise and advanced race (owner, 2026-10-30:
// "an wise and advanced alien race. holographic buttons, with organic shaped metal armor peices surrounding the
// edges"). Every control is light: translucent hologram fields with scanlines and a chromatic edge, held in flowing,
// polished gold and pearl armour that wraps the corners like shells. Keys are pointed lozenges of light cupped by two
// small armour claws. Icons are sacred geometry: triangles, crystals and dots joined by hairlines. Painted in canvas,
// deterministic. Registers itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
const AE = { pearl: '#e8e4da', gold: '#c9a646', goldHi: '#f6e2a0', goldLo: '#6a4a14', void: '#0b2a33', deep: '#04141a', holo: '#6ff3ff', holoHi: '#dffcff',
  violet: '#9a8cff', coral: '#ff6a5a', amber: '#ffd27a' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
function path(x, pts, closed = true) { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) { if (p.length === 4) x.quadraticCurveTo(p[0], p[1], p[2], p[3]); else x.lineTo(p[0], p[1]); } if (closed) x.closePath(); }

// ---- materials ---------------------------------------------------------------------------------------------------
// Polished gold armour: a bright top-left sheen, a deep shadow bottom right, a pearl reflection band, a fine dark seam.
function gold(x, draw, box, sheen = 1) {
  const [x0, y0, x1, y1] = box;
  x.save(); draw(); x.clip();
  const g = x.createLinearGradient(x0, y0, x1, y1); g.addColorStop(0, AE.goldHi); g.addColorStop(0.35, AE.gold); g.addColorStop(0.75, '#8e6b26'); g.addColorStop(1, AE.goldLo);
  x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.globalAlpha = 0.45 * sheen; const r = x.createLinearGradient(x0, y1, x1, y0); r.addColorStop(0.42, 'rgba(255,255,255,0)'); r.addColorStop(0.5, 'rgba(255,252,240,0.9)'); r.addColorStop(0.58, 'rgba(255,255,255,0)');
  x.fillStyle = r; x.fillRect(x0, y0, x1 - x0, y1 - y0); x.globalAlpha = 1;
  draw(); x.strokeStyle = 'rgba(255,248,220,0.6)'; x.lineWidth = 2; x.stroke();
  x.restore(); draw(); x.strokeStyle = '#3a2a0a'; x.lineWidth = 1.1; x.stroke();
}
// A pearl inlay: soft white with a faint iridescence.
function pearl(x, draw, box) {
  const [x0, y0, x1, y1] = box;
  x.save(); draw(); x.clip();
  const g = x.createLinearGradient(x0, y0, x1, y1); g.addColorStop(0, '#fbf8f0'); g.addColorStop(0.6, '#d8d2c4'); g.addColorStop(1, '#9a9488'); x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  const i = x.createLinearGradient(x0, y1, x1, y0); i.addColorStop(0, 'rgba(111,243,255,0.18)'); i.addColorStop(0.5, 'rgba(255,170,220,0.12)'); i.addColorStop(1, 'rgba(154,140,255,0.18)'); x.fillStyle = i; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.restore(); draw(); x.strokeStyle = '#6a6250'; x.lineWidth = 0.8; x.stroke();
}
// A hologram field: translucent light, scanlines, a glowing edge split slightly into colours.
function holo(x, draw, box, level, col = AE.holo) {
  const [x0, y0, x1, y1] = box;
  x.save(); draw(); x.clip();
  const g = x.createLinearGradient(0, y0, 0, y1); g.addColorStop(0, hexA(col, 0.10 + level * 0.32)); g.addColorStop(0.5, hexA(col, 0.04 + level * 0.18)); g.addColorStop(1, hexA(col, 0.12 + level * 0.3));
  x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.fillStyle = hexA(AE.holoHi, 0.06 + level * 0.08); for (let y = y0; y < y1; y += 3) x.fillRect(x0, y, x1 - x0, 1);
  x.restore();
  x.save(); x.translate(-0.8, 0); draw(); x.strokeStyle = 'rgba(255,120,200,0.35)'; x.lineWidth = 1; x.stroke(); x.restore();
  x.save(); x.translate(0.8, 0); draw(); x.strokeStyle = 'rgba(120,140,255,0.35)'; x.lineWidth = 1; x.stroke(); x.restore();
  x.save(); x.shadowColor = col; x.shadowBlur = 4 + level * 8; draw(); x.strokeStyle = hexA(level > 0.8 ? AE.holoHi : col, 0.55 + level * 0.45); x.lineWidth = 1.4; x.stroke(); x.restore();
}
// An armour shell wrapping a corner: two tapering wings, one along each edge, meeting in a swollen knuckle.
function shell(x, len, thick, R) {
  const pts = [[0, thick * 0.4], [thick * 0.3, 0], [len * 0.55, 1], [len, thick * 0.12], [len * 0.55, thick * 0.55, len * 0.32, thick * 0.62],
    [thick * 0.62, thick * 0.62, thick * 0.62, len * 0.32], [thick * 0.55, len * 0.55, thick * 0.12, len], [1, len * 0.55], [0, thick * 0.4]];
  gold(x, () => path(x, pts), [0, 0, len, len]);
  pearl(x, () => { x.beginPath(); x.ellipse(thick * 0.42, thick * 0.42, thick * 0.2, thick * 0.13, P / 4, 0, 2 * P); }, [0, 0, thick, thick]);
}
const RNG = (k) => F.rng(F.hash('aelith.' + k));

// ---- the nine-slice pieces --------------------------------------------------------------------------------------
function frame(x, w, h, len, thick, r) {
  const field = () => { x.beginPath(); x.roundRect(6, 6, w - 12, h - 12, r); };
  x.fillStyle = hexA(AE.deep, 0.92); field(); x.fill();
  holo(x, field, [6, 6, w - 6, h - 6], 0.12);
  for (const [sx, sy] of [[1, 1], [-1, 1], [1, -1], [-1, -1]]) { x.save(); x.translate(sx > 0 ? 0 : w, sy > 0 ? 0 : h); x.scale(sx, sy); shell(x, len, thick); x.restore(); }
}
// The holo lozenge: pointed at both ends, light inside, a small gold claw cupping each point.
function lozenge(level) {
  return (x, w, h) => {
    const m = h / 2, shape = () => path(x, [[12, m], [24, 6, 40, 6], [w - 40, 6], [w - 24, 6, w - 12, m], [w - 24, h - 6, w - 40, h - 6], [40, h - 6], [24, h - 6, 12, m]]);
    holo(x, shape, [0, 0, w, h], level);
    if (level >= 1) { const g = x.createLinearGradient(0, 6, 0, h - 6); g.addColorStop(0, 'rgba(255,255,255,0)'); g.addColorStop(0.5, 'rgba(240,255,255,0.4)'); g.addColorStop(1, 'rgba(255,255,255,0)'); x.save(); shape(); x.clip(); x.fillStyle = g; x.fillRect(30, 0, w - 60, h); x.restore(); }
    for (const s of [1, -1]) {
      x.save(); if (s < 0) { x.translate(w, 0); x.scale(-1, 1); }
      gold(x, () => path(x, [[2, m], [6, m - 13, 20, m - 16], [12, m - 8, 11, m], [12, m + 8, 20, m + 16], [6, m + 13, 2, m]]), [0, m - 17, 22, m + 17]);
      x.restore();
    }
  };
}
// A tab: a bar of light underlined in gold, the active one filled.
function tab(active) {
  return (x, w, h) => {
    const shape = () => { x.beginPath(); x.roundRect(10, 8, w - 20, h - 18, 3); };
    holo(x, shape, [0, 0, w, h], active ? 0.75 : 0.08);
    gold(x, () => { x.beginPath(); x.roundRect(4, h - 7, w - 8, 3.5, 1.75); }, [0, h - 8, w, h - 3], 0.6);
    for (const cx of [6, w - 6]) gold(x, () => { x.beginPath(); x.moveTo(cx, h - 10); x.lineTo(cx + 4, h - 5); x.lineTo(cx, h - 1); x.lineTo(cx - 4, h - 5); x.closePath(); }, [cx - 4, h - 10, cx + 4, h - 1]);
  };
}
// The strip: a line of light along the outer edge, a swept gold wing at each end.
function strip(flip) {
  return (x, w, h) => {
    if (flip) { x.translate(0, h); x.scale(1, -1); }
    holo(x, () => { x.beginPath(); x.roundRect(90, h - 11, w - 180, 4, 2); }, [0, h - 12, w, h - 6], 0.8);
    for (const s of [1, -1]) {
      x.save(); if (s < 0) { x.translate(w, 0); x.scale(-1, 1); }
      gold(x, () => path(x, [[2, h - 4], [6, 14, 40, 6], [70, 4, 104, h - 12], [70, h - 10, 34, h - 8], [14, h - 6, 2, h - 4]]), [0, 4, 104, h]);
      pearl(x, () => { x.beginPath(); x.ellipse(32, h - 22, 12, 6, -0.3, 0, 2 * P); }, [18, h - 30, 46, h - 14]);
      x.restore();
    }
  };
}
const nine = {
  'frame.panel': { size: [180, 140], m: [44, 44, 44, 44], draw: (x, w, h) => frame(x, w, h, 46, 30, 16) },
  'frame.inset': { size: [64, 48], m: [14, 14, 14, 14], draw: (x, w, h) => { const f = () => { x.beginPath(); x.roundRect(3, 3, w - 6, h - 6, 9); }; x.fillStyle = hexA(AE.deep, 0.9); f(); x.fill(); holo(x, f, [0, 0, w, h], 0.1); pearl(x, () => { x.beginPath(); x.ellipse(10, 6, 6, 2.5, 0, 0, 2 * P); }, [4, 3, 16, 9]); } },
  'bar.track': { size: [96, 16], m: [0, 14, 0, 14], inset: [4, 3], draw: (x, w, h) => {
    x.fillStyle = hexA(AE.deep, 0.95); x.beginPath(); x.roundRect(6, 2, w - 12, h - 4, 6); x.fill(); x.strokeStyle = hexA(AE.holo, 0.35); x.lineWidth = 1; x.stroke();
    for (const s of [1, -1]) { x.save(); if (s < 0) { x.translate(w, 0); x.scale(-1, 1); } gold(x, () => path(x, [[1, h / 2], [4, 1, 12, 1], [9, h / 2], [12, h - 1], [4, h - 1, 1, h / 2]]), [0, 0, 12, h]); x.restore(); } } },
  'bar.fill': { size: [64, 10], m: [0, 6, 0, 6], draw: (x, w, h) => holo(x, () => { x.beginPath(); x.roundRect(1, 1, w - 2, h - 2, 4); }, [0, 0, w, h], 0.9) },
  'bar.fill.red': { size: [64, 10], m: [0, 6, 0, 6], draw: (x, w, h) => holo(x, () => { x.beginPath(); x.roundRect(1, 1, w - 2, h - 2, 4); }, [0, 0, w, h], 0.9, AE.coral) },
  'button.idle': { size: [150, 48], m: [20, 42, 20, 42], draw: lozenge(0.15) },
  'button.hover': { size: [150, 48], m: [20, 42, 20, 42], draw: lozenge(0.6) },
  'button.pressed': { size: [150, 48], m: [20, 42, 20, 42], draw: lozenge(1) },
  'tab.idle': { size: [120, 40], m: [14, 16, 14, 16], draw: tab(false) },
  'tab.active': { size: [120, 40], m: [14, 16, 14, 16], draw: tab(true) },
  'strip.top': { size: [512, 56], m: [10, 110, 10, 110], draw: strip(false) },
  'strip.bottom': { size: [512, 56], m: [10, 110, 10, 110], draw: strip(true) },
  'divider': { size: [128, 10], m: [0, 16, 0, 16], draw: (x, w, h) => {
    holo(x, () => { x.beginPath(); x.roundRect(14, h / 2 - 1, w - 28, 2, 1); }, [0, 0, w, h], 0.6);
    for (const cx of [7, w - 7]) gold(x, () => { x.beginPath(); x.ellipse(cx, h / 2, 6, 3.5, 0, 0, 2 * P); }, [cx - 6, 1, cx + 6, h - 1]); } },
};

// ---- the hero pieces ---------------------------------------------------------------------------------------------
const hero = {
  // A pearl-and-gold shell holding a slit of light for the name.
  'nameplate': (x, w, h) => {
    gold(x, () => path(x, [[2, h / 2], [16, 2, 60, 3], [w - 60, 3], [w - 16, 2, w - 2, h / 2], [w - 16, h - 2, w - 60, h - 3], [60, h - 3], [16, h - 2, 2, h / 2]]), [0, 0, w, h]);
    x.fillStyle = AE.deep; x.beginPath(); x.roundRect(46, 10, w - 92, h - 20, 18); x.fill();
    holo(x, () => { x.beginPath(); x.roundRect(46, 10, w - 92, h - 20, 18); }, [46, 10, w - 46, h - 10], 0.15);
    for (const cx of [24, w - 24]) pearl(x, () => { x.beginPath(); x.ellipse(cx, h / 2, 10, 13, 0, 0, 2 * P); }, [cx - 10, h / 2 - 13, cx + 10, h / 2 + 13]);
  },
  // A disc of light in a gold ring; eight lozenge keys point outward round a crystal stop key.
  'helm.pad': (x, w, h) => {
    const c = w / 2;
    gold(x, () => { x.beginPath(); x.arc(c, c, 146, 0, 2 * P); x.arc(c, c, 132, 0, 2 * P, true); }, [0, 0, w, h]);
    x.fillStyle = hexA(AE.deep, 0.95); x.beginPath(); x.arc(c, c, 132, 0, 2 * P); x.fill();
    holo(x, () => { x.beginPath(); x.arc(c, c, 130, 0, 2 * P); }, [c - 130, c - 130, c + 130, c + 130], 0.08);
    for (let i = 0; i < 8; i++) {
      const a = -P / 2 + i * P / 4; x.save(); x.translate(c + Math.cos(a) * 98, c + Math.sin(a) * 98); x.rotate(a);
      holo(x, () => path(x, [[-26, 0], [-12, -16, 4, -15], [26, 0], [4, 15], [-12, 16, -26, 0]]), [-26, -16, 26, 16], 0.28); x.restore();
    }
    for (let i = 0; i < 8; i++) { const a = -P / 2 + (i + 0.5) * P / 4; x.save(); x.translate(c + Math.cos(a) * 139, c + Math.sin(a) * 139); x.rotate(a + P / 2); pearl(x, () => { x.beginPath(); x.ellipse(0, 0, 5, 3, 0, 0, 2 * P); }, [-5, -3, 5, 3]); x.restore(); }
    holo(x, () => path(x, [[c, c - 40], [c + 30, c], [c, c + 40], [c - 30, c]]), [c - 30, c - 40, c + 30, c + 40], 0.7, AE.violet);
  },
  // Two armour arms embrace a field of light, like hands round a lantern.
  'ssd.bezel': (x, w, h) => {
    x.fillStyle = hexA(AE.deep, 0.95); x.beginPath(); x.roundRect(20, 14, w - 40, h - 28, 20); x.fill();
    holo(x, () => { x.beginPath(); x.roundRect(20, 14, w - 40, h - 28, 20); }, [20, 14, w - 20, h - 14], 0.1);
    for (const s of [1, -1]) {
      x.save(); if (s < 0) { x.translate(w, 0); x.scale(-1, 1); }
      gold(x, () => path(x, [[70, 4], [10, 6, 6, 60], [2, h / 2], [6, h - 60, 70, h - 4], [40, h - 14, 26, h - 70], [22, h / 2], [26, 70, 70, 4]]), [0, 0, 70, h]);
      pearl(x, () => { x.beginPath(); x.ellipse(14, h / 2, 6, 22, 0, 0, 2 * P); }, [8, h / 2 - 22, 20, h / 2 + 22]);
      x.restore();
    }
  },
  'dial': (x, w) => {
    const c = w / 2; gold(x, () => { x.beginPath(); x.arc(c, c, 58, 0, 2 * P); x.arc(c, c, 48, 0, 2 * P, true); }, [0, 0, w, w]);
    x.fillStyle = hexA(AE.deep, 0.95); x.beginPath(); x.arc(c, c, 48, 0, 2 * P); x.fill(); holo(x, () => { x.beginPath(); x.arc(c, c, 46, 0, 2 * P); }, [c - 46, c - 46, c + 46, c + 46], 0.1);
  },
  'lamp.alert.green': (x, w) => crystal(x, w, '#6fffb0'), 'lamp.alert.yellow': (x, w) => crystal(x, w, AE.amber), 'lamp.alert.red': (x, w) => crystal(x, w, AE.coral),
  // The Conclave's mark (original): a triangle of light in a circle, a crystal at its heart, gold wings beneath.
  'crest': (x, w) => {
    const c = w / 2;
    for (const s of [1, -1]) { x.save(); if (s < 0) { x.translate(w, 0); x.scale(-1, 1); } gold(x, () => path(x, [[c - 2, c + 30], [10, c + 26, 4, c - 6], [18, c + 6, c - 2, c + 18]]), [0, c - 8, c, c + 32]); x.restore(); }
    holo(x, () => { x.beginPath(); x.arc(c, c - 4, 30, 0, 2 * P); }, [c - 30, c - 34, c + 30, c + 26], 0.25);
    holo(x, () => path(x, [[c, c - 30], [c + 26, c + 11], [c - 26, c + 11]]), [c - 26, c - 30, c + 26, c + 11], 0.5);
    holo(x, () => path(x, [[c, c - 14], [c + 7, c - 3], [c, c + 8], [c - 7, c - 3]]), [c - 7, c - 14, c + 7, c + 8], 1, AE.violet);
  },
};
// A lamp: a crystal of coloured light in a gold setting.
function crystal(x, w, col) {
  const c = w / 2;
  gold(x, () => path(x, [[c - 20, c + 14], [c, c + 28], [c + 20, c + 14], [c + 13, c + 12], [c, c + 20], [c - 13, c + 12]]), [c - 20, c + 10, c + 20, c + 28]);
  x.save(); x.shadowColor = col; x.shadowBlur = 16; holo(x, () => path(x, [[c, 4], [c + 16, c - 4], [c, c + 18], [c - 16, c - 4]]), [c - 16, 4, c + 16, c + 18], 1, col); x.restore();
  x.strokeStyle = 'rgba(255,255,255,0.6)'; x.lineWidth = 1; x.beginPath(); x.moveTo(c, 4); x.lineTo(c, c + 18); x.moveTo(c - 16, c - 4); x.lineTo(c + 16, c - 4); x.stroke();
}

// ---- icons: sacred geometry in hairlines of light ---------------------------------------------------------------
function icon(name) {
  return F.paint(48, 48, (x) => {
    x.strokeStyle = AE.holo; x.fillStyle = AE.holoHi; x.lineWidth = 1.6; x.shadowColor = AE.holo; x.shadowBlur = 5;
    const L = (pts, closed = false) => { path(x, pts, closed); x.stroke(); };
    const D = (cx, cy, r = 2.4) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); };
    const TRI = (cx, cy, r, a = -P / 2) => L([0, 1, 2].map((i) => [cx + Math.cos(a + i * 2 * P / 3) * r, cy + Math.sin(a + i * 2 * P / 3) * r]), true);
    const GEM = (cx, cy, rx, ry) => L([[cx, cy - ry], [cx + rx, cy], [cx, cy + ry], [cx - rx, cy]], true);
    ({
      helm: () => { x.beginPath(); x.arc(24, 24, 19, 0, 2 * P); x.stroke(); TRI(24, 24, 15); D(24, 9); },
      fire: () => { L([[6, 10], [36, 24]]); L([[6, 38], [36, 24]]); L([[4, 24], [36, 24]]); GEM(40, 24, 5, 7); },
      shields: () => { L([[24, 4], [42, 14], [42, 32], [24, 44], [6, 32], [6, 14]], true); TRI(24, 25, 9); },
      tractor: () => { GEM(10, 24, 6, 9); GEM(38, 24, 6, 9); x.setLineDash([2, 3]); L([[16, 24], [32, 24]]); x.setLineDash([]); },
      transport: () => { GEM(24, 16, 8, 11); D(16, 36); D(24, 40); D(32, 36); },
      science: () => { TRI(24, 26, 19); L([[24, 7], [24, 36]]); D(24, 26, 3.2); },
      comms: () => { D(24, 24, 3.4); for (let i = 0; i < 3; i++) { const a = -P / 2 + i * 2 * P / 3; TRI(24 + Math.cos(a) * 14, 24 + Math.sin(a) * 14, 6, a); } },
      repair: () => { GEM(24, 24, 11, 18); L([[18, 20], [24, 26], [30, 22]]); D(24, 26, 2); },
      map: () => { const n = [[8, 12], [22, 8], [38, 16], [30, 32], [12, 38]]; L(n); for (const [px, py] of n) D(px, py); },
      settings: () => { L([[24, 4], [41, 14], [41, 34], [24, 44], [7, 34], [7, 14]], true); TRI(24, 25, 10); D(24, 25, 2.4); },
    })[name]();
  });
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('aelith', {
  name: 'Aelith Conclave', kind: 'Hologram light held in sculpted gold and pearl armour', bg: '#03111a',
  css: { '--s-ink': AE.holoHi, '--s-dim': '#9fc9cf', '--s-num': AE.holo, '--s-body': '"Exo 2", sans-serif', '--btn-ink': AE.holoHi, '--btn-shadow': `0 0 6px ${AE.holo}`, '--btn-justify': 'center',
    '--btn-font': '400 13px/1 "Michroma", sans-serif', '--btn-track': '0.12em', '--btn-case': 'uppercase', '--btn-motion': 'filter 0.25s', '--btn-press': 'none',
    '--tab-font': '400 10px/1 "Michroma", sans-serif', '--tab-ink': '#9fc9cf', '--tab-ink-on': AE.holoHi, '--title-font': '400 15px/1 "Michroma", sans-serif', '--title-ink': AE.goldHi,
    '--lbl-font': '600 15px/1 "Exo 2", sans-serif', '--num-font': '300 28px/1 "Exo 2", sans-serif' },
  accent: AE.holo, warn: AE.coral,
  title: (t, m) => { t.style.cssText = `left:${40 - m[3]}px;top:${-m[0] + 9}px;letter-spacing:0.18em;text-shadow:0 1px 0 #3a2a0a;`; },
  plateText: { left: 60, top: 12, width: 340, height: 32, font: '400 15px/1 "Michroma", sans-serif', color: AE.holoHi, sub: 'SERENE TIDE · ELDER IAVEN' },
  helmKeys: 'ring', helmR: 98, shield: [AE.holo, AE.amber, AE.coral],
  fonts: ['400 20px "Michroma"', '600 20px "Exo 2"', '300 20px "Exo 2"'], view: { title: '400 22px "Michroma"', body: '300 24px "Exo 2"', upper: true },
  build: () => F.fromPainters(nine, hero, icon, false),
  live: {
    // A pointed mote of light leading a dot.
    arrow: (x) => { x.shadowColor = AE.holo; x.shadowBlur = 6; x.fillStyle = AE.holoHi; path(x, [[12, 0], [-2, -5], [-8, 0], [-2, 5]]); x.fill(); x.beginPath(); x.arc(-13, 0, 2, 0, 2 * P); x.fill(); },
    // A golden shell round a crystal core; shields are arcs of light in dotted rings.
    ssd: (x, cx, cy, sh, col) => {
      for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.save(); x.shadowColor = col(sh[i]); x.shadowBlur = 8; x.strokeStyle = col(sh[i]); x.lineWidth = 3; x.beginPath(); x.arc(cx, cy, 104, a - P / 6 + 0.09, a + P / 6 - 0.09); x.stroke(); x.setLineDash([1, 5]); x.lineWidth = 1.5; x.beginPath(); x.arc(cx, cy, 112, a - P / 6 + 0.09, a + P / 6 - 0.09); x.stroke(); x.restore(); }
      gold(x, () => path(x, [[cx, cy - 76], [cx + 52, cy - 50, cx + 46, cy + 14], [cx + 36, cy + 58, cx, cy + 66], [cx - 36, cy + 58, cx - 46, cy + 14], [cx - 52, cy - 50, cx, cy - 76]]), [cx - 52, cy - 76, cx + 52, cy + 66]);
      x.fillStyle = AE.deep; x.beginPath(); x.ellipse(cx, cy, 22, 34, 0, 0, 2 * P); x.fill();
      holo(x, () => path(x, [[cx, cy - 28], [cx + 14, cy], [cx, cy + 28], [cx - 14, cy]]), [cx - 14, cy - 28, cx + 14, cy + 28], 1, AE.violet);
      for (const [mx, my] of [[cx, cy - 60], [cx - 36, cy + 20], [cx + 36, cy + 20]]) holo(x, () => path(x, [[mx, my - 6], [mx + 5, my], [mx, my + 6], [mx - 5, my]]), [mx - 5, my - 6, mx + 5, my + 6], 1);
    },
    // Rings of light, a soft sweep, contacts as coral motes.
    dial: (x, c, cts) => {
      x.strokeStyle = hexA(AE.holo, 0.5); x.lineWidth = 1; for (const r of [16, 30, 42]) { x.beginPath(); x.arc(c, c, r, 0, 2 * P); x.stroke(); }
      const g = x.createConicGradient ? x.createConicGradient(-P / 2, c, c) : null;
      if (g) { g.addColorStop(0, hexA(AE.holo, 0.45)); g.addColorStop(0.18, hexA(AE.holo, 0)); g.addColorStop(1, hexA(AE.holo, 0)); x.fillStyle = g; x.beginPath(); x.arc(c, c, 44, 0, 2 * P); x.fill(); }
      for (const [dx, dy] of cts) { x.save(); x.shadowColor = AE.coral; x.shadowBlur = 6; x.fillStyle = AE.coral; path(x, [[c + dx * 34, c + dy * 34 - 5], [c + dx * 34 + 4, c + dy * 34], [c + dx * 34, c + dy * 34 + 5], [c + dx * 34 - 4, c + dy * 34]]); x.fill(); x.restore(); }
    },
    // Four gold crescents cupping the target, a hairline of light between them.
    bracket: (x, bx, by, bw, bh) => {
      x.save(); x.shadowColor = AE.holo; x.shadowBlur = 6; x.strokeStyle = hexA(AE.holo, 0.6); x.lineWidth = 1; x.strokeRect(bx + 10, by + 10, bw - 20, bh - 20); x.restore();
      for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) { x.save(); x.translate(px, py); x.scale(sx, sy); shell(x, 50, 18); x.restore(); }
    },
  },
  world: ['Patient knowledge kept over aeons: act rarely, act exactly.', 'Graceful bodies and psionic minds: they touch light, not switches.', 'Light shaped by thought, held in sculpted alloy shells.', 'Smooth golden shells round crystal cores.'],
  shapes: 'Pointed lozenges of light, flowing armour shells, crystals, triangles in circles.', light: 'Emissive hologram light with scanlines and a split-colour edge; polished gold with a sharp sheen.',
  motion: 'Light brightens and settles; nothing moves.', type: '"Michroma" wide capitals over "Exo 2".',
  palette: [['Pearl', AE.pearl, 'Inlays', 'Wisdom: the slow layering of ages'], ['Gold', AE.gold, 'Armour', 'Permanence'], ['Void', AE.void, 'Fields', 'Serenity, depth'],
    ['Holo', AE.holo, 'Light, readouts', 'Thought made visible'], ['Violet', AE.violet, 'Crystal cores', 'The psionic'], ['Coral', AE.coral, 'Alerts', 'A rare disturbance']],
});
})();
