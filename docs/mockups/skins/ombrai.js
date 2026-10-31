// The Ombrai skin (mockup 4, OpenSpec change console-skins): an enigmatic race (owner, 2026-10-30: "an enigmatic
// alien race with cool dark purple colors and pink buttons, progress bars are curcular, almost organic like eyes").
// Smoked violet glass, soft pink light, curves everywhere; keys are crescent bars whose bitten end cradles a small
// glowing moon; every gauge is circular and watches you: bars are rows of eye-rings that light as they fill, the
// radar is an iris, shields fill round the ship like rings of an eye. Painted in canvas, deterministic. Registers
// itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
const OM = { void: '#120822', deep: '#3b1763', smoke: '#241a33', pink: '#ff4fb0', lilac: '#c58cff', crimson: '#ff2f5a', pale: '#f4e6ff', dim: '#b9a0d6' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }

// ---- materials ---------------------------------------------------------------------------------------------------
// Smoked violet glass: deep and dark, a faint iridescent sheen across it, a soft highlight along the top.
function glass(x, draw, box, o = {}) {
  const [x0, y0, x1, y1] = box;
  x.save(); draw(); x.clip();
  const g = x.createLinearGradient(x0, y0, x0, y1); g.addColorStop(0, o.top || '#3a2456'); g.addColorStop(0.5, OM.smoke); g.addColorStop(1, OM.void); x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  const s = x.createLinearGradient(x0, y1, x1, y0); s.addColorStop(0.35, 'rgba(255,79,176,0)'); s.addColorStop(0.5, 'rgba(255,79,176,0.10)'); s.addColorStop(0.58, 'rgba(120,200,255,0.08)'); s.addColorStop(0.7, 'rgba(197,140,255,0)');
  x.fillStyle = s; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.restore();
  x.save(); draw(); x.clip(); x.translate(0, 1.2); draw(); x.strokeStyle = 'rgba(244,230,255,0.22)'; x.lineWidth = 1.4; x.stroke(); x.restore();
  draw(); x.strokeStyle = '#07030d'; x.lineWidth = 1; x.stroke();
}
// A soft glowing line.
function glow(x, draw, col = OM.pink, w = 1.4, blur = 6, a = 0.9) { x.save(); x.shadowColor = col; x.shadowBlur = blur; draw(); x.strokeStyle = hexA(col, a); x.lineWidth = w; x.stroke(); x.restore(); }
// An eye: rings round a slit pupil, lit pink.
function eye(x, cx, cy, r, open = 1) {
  x.fillStyle = OM.void; x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill();
  glow(x, () => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); }, OM.lilac, 1, 4, 0.7);
  glow(x, () => { x.beginPath(); x.arc(cx, cy, r * 0.62, 0, 2 * P); }, OM.pink, 1.2, 5, 0.8);
  x.save(); x.shadowColor = OM.pink; x.shadowBlur = 8; x.fillStyle = OM.pink; x.beginPath(); x.ellipse(cx, cy, r * 0.14 * open + 0.6, r * 0.5, 0, 0, 2 * P); x.fill(); x.restore();
}
const RNG = (k) => F.rng(F.hash('ombrai.' + k));

// ---- the nine-slice pieces --------------------------------------------------------------------------------------
// A frame: a smoked glass band with deep round corners, a pink line along its inner edge, an eye in every corner.
function frame(x, w, h, b, r, eyes = true) {
  const both = () => { x.beginPath(); x.roundRect(2, 2, w - 4, h - 4, r); x.roundRect(2 + b, 2 + b, w - 4 - 2 * b, h - 4 - 2 * b, Math.max(4, r - b)); };
  x.fillStyle = '#0a0414'; x.beginPath(); x.roundRect(2 + b, 2 + b, w - 4 - 2 * b, h - 4 - 2 * b, Math.max(4, r - b)); x.fill();
  x.save(); both(); x.clip('evenodd'); glass(x, () => { x.beginPath(); x.rect(0, 0, w, h); }, [0, 0, w, h]); x.restore();
  glow(x, () => { x.beginPath(); x.roundRect(2 + b, 2 + b, w - 4 - 2 * b, h - 4 - 2 * b, Math.max(4, r - b)); }, OM.pink, 1.2, 6, 0.8);
  x.beginPath(); x.roundRect(2, 2, w - 4, h - 4, r); x.strokeStyle = '#07030d'; x.lineWidth = 1.2; x.stroke();
  if (eyes) for (const [cx, cy] of [[b * 0.95, b * 0.95], [w - b * 0.95, b * 0.95], [b * 0.95, h - b * 0.95], [w - b * 0.95, h - b * 0.95]]) eye(x, cx, cy, b * 0.58);
}
// The crescent bar: its left end bitten in like a crescent moon, a small moon resting in the bite; the right end round.
function crescent(level) {
  return (x, w, h) => {
    const m = h / 2, shape = () => { x.beginPath(); x.moveTo(20, 4); x.lineTo(w - m, 4); x.arc(w - m, m, m - 4, -P / 2, P / 2); x.lineTo(20, h - 4); x.quadraticCurveTo(38, m, 20, 4); x.closePath(); };
    glass(x, shape, [0, 0, w, h], { top: '#4a2a66' });
    x.save(); shape(); x.clip();
    const g = x.createRadialGradient(w - m, m, 2, w - m, m, w * 0.85); g.addColorStop(0, hexA(level > 0.9 ? OM.pale : OM.pink, 0.25 + level * 0.65)); g.addColorStop(0.5, hexA(OM.pink, 0.1 + level * 0.35)); g.addColorStop(1, 'rgba(0,0,0,0)');
    x.fillStyle = g; x.fillRect(0, 0, w, h); x.restore();
    glow(x, shape, OM.pink, 1.2 + level, 4 + level * 10, 0.5 + level * 0.5);
    x.save(); x.shadowColor = OM.pink; x.shadowBlur = 6 + level * 8; x.fillStyle = level > 0.9 ? OM.pale : hexA(OM.pink, 0.55 + level * 0.45); x.beginPath(); x.arc(13, m, 5.5, 0, 2 * P); x.fill(); x.restore();
  };
}
// A tab: a smoked glass slip with three small eyes at its left; the active one opens them and glows beneath.
function slip(active) {
  return (x, w, h) => {
    const shape = () => { x.beginPath(); x.roundRect(3, 5, w - 6, h - 10, (h - 10) / 2); };
    glass(x, shape, [0, 0, w, h]);
    if (active) glow(x, () => { x.beginPath(); x.moveTo(20, h - 6); x.quadraticCurveTo(w / 2, h - 2, w - 20, h - 6); }, OM.pink, 2, 8);
    for (let i = 0; i < 2; i++) { x.save(); x.fillStyle = active ? OM.pink : hexA(OM.lilac, 0.5); x.shadowColor = OM.pink; x.shadowBlur = active ? 6 : 0; x.beginPath(); x.ellipse(10 + i * 7, h / 2, 1.6, 3, 0, 0, 2 * P); x.fill(); x.restore(); }
  };
}
// A strip: a long band of smoked glass, softly lit along its edge, an eye at each end.
function band(flip) {
  return (x, w, h) => {
    if (flip) { x.translate(0, h); x.scale(1, -1); }
    const shape = () => { x.beginPath(); x.roundRect(2, h - 22, w - 4, 18, 9); };
    glass(x, shape, [0, h - 22, w, h - 4]);
    glow(x, () => { x.beginPath(); x.moveTo(40, h - 6); x.lineTo(w - 40, h - 6); }, OM.pink, 1.2, 6, 0.7);
    for (const cx of [26, w - 26]) { glass(x, () => { x.beginPath(); x.arc(cx, h - 24, 22, 0, 2 * P); }, [cx - 22, h - 46, cx + 22, h - 2]); eye(x, cx, h - 24, 14); }
  };
}
const nine = {
  'frame.panel': { size: [160, 120], m: [34, 34, 34, 34], draw: (x, w, h) => frame(x, w, h, 20, 28) },
  'frame.inset': { size: [64, 48], m: [14, 14, 14, 14], draw: (x, w, h) => frame(x, w, h, 6, 12, false) },
  // Bars are rows of eye-rings (the owner: "progress bars are curcular, almost organic like eyes"): one ring is the
  // tile, repeated along the bar; the fill is a row of open pink pupils over it.
  'bar.track': { size: [16, 16], m: [0, 0, 0, 0], inset: [0, 3], draw: (x) => { x.fillStyle = OM.void; x.beginPath(); x.arc(8, 8, 7, 0, 2 * P); x.fill(); x.strokeStyle = hexA(OM.lilac, 0.55); x.lineWidth = 1; x.stroke(); } },
  'bar.fill': { size: [16, 10], m: [0, 0, 0, 0], draw: (x) => { x.save(); x.shadowColor = OM.pink; x.shadowBlur = 5; x.strokeStyle = OM.pink; x.lineWidth = 1.4; x.beginPath(); x.arc(8, 5, 4.2, 0, 2 * P); x.stroke(); x.fillStyle = OM.pale; x.beginPath(); x.ellipse(8, 5, 1, 3, 0, 0, 2 * P); x.fill(); x.restore(); } },
  'bar.fill.red': { size: [16, 10], m: [0, 0, 0, 0], draw: (x) => { x.save(); x.shadowColor = OM.crimson; x.shadowBlur = 5; x.strokeStyle = OM.crimson; x.lineWidth = 1.4; x.beginPath(); x.arc(8, 5, 4.2, 0, 2 * P); x.stroke(); x.fillStyle = '#ffd0dc'; x.beginPath(); x.ellipse(8, 5, 1, 3, 0, 0, 2 * P); x.fill(); x.restore(); } },
  'button.idle': { size: [150, 48], m: [20, 26, 20, 34], draw: crescent(0.1) },
  'button.hover': { size: [150, 48], m: [20, 26, 20, 34], draw: crescent(0.55) },
  'button.pressed': { size: [150, 48], m: [20, 26, 20, 34], draw: crescent(1) },
  'tab.idle': { size: [120, 40], m: [14, 16, 14, 26], draw: slip(false) },
  'tab.active': { size: [120, 40], m: [14, 16, 14, 26], draw: slip(true) },
  'strip.top': { size: [512, 56], m: [10, 56, 10, 56], draw: band(false) },
  'strip.bottom': { size: [512, 56], m: [10, 56, 10, 56], draw: band(true) },
  'divider': { size: [128, 10], m: [0, 16, 0, 16], draw: (x, w, h) => { glow(x, () => { x.beginPath(); x.moveTo(14, h / 2); x.lineTo(w - 14, h / 2); }, OM.pink, 1, 4, 0.6); for (const cx of [6, w - 6]) { x.strokeStyle = OM.lilac; x.lineWidth = 1.2; x.beginPath(); x.arc(cx, h / 2, 3.5, 0, 2 * P); x.stroke(); } } },
};

// ---- the hero pieces ---------------------------------------------------------------------------------------------
const hero = {
  // A glass capsule with an eye at its head.
  'nameplate': (x, w, h) => {
    glass(x, () => { x.beginPath(); x.roundRect(2, 4, w - 4, h - 8, (h - 8) / 2); }, [0, 0, w, h]);
    glow(x, () => { x.beginPath(); x.roundRect(54, 10, w - 70, h - 20, (h - 20) / 2); }, OM.pink, 1, 5, 0.6);
    glass(x, () => { x.beginPath(); x.arc(28, h / 2, 26, 0, 2 * P); }, [2, h / 2 - 26, 54, h / 2 + 26], { top: '#4a2a66' }); eye(x, 28, h / 2, 17);
  },
  // An iris of eight crescent petals round a pupil.
  'helm.pad': (x, w, h) => {
    const c = w / 2;
    glass(x, () => { x.beginPath(); x.arc(c, c, 146, 0, 2 * P); }, [0, 0, w, h]);
    for (const r of [140, 128]) glow(x, () => { x.beginPath(); x.arc(c, c, r, 0, 2 * P); }, OM.lilac, 1, 4, 0.5);
    for (let i = 0; i < 8; i++) {
      const a = -P / 2 + i * P / 4; x.save(); x.translate(c, c); x.rotate(a);
      const petal = () => { x.beginPath(); x.moveTo(62, -20); x.quadraticCurveTo(100, -34, 128, 0); x.quadraticCurveTo(100, 34, 62, 20); x.quadraticCurveTo(78, 0, 62, -20); x.closePath(); };
      glass(x, petal, [60, -34, 130, 34], { top: '#4a2a66' }); glow(x, petal, OM.pink, 1.1, 6, 0.75); x.restore();
    }
    x.fillStyle = OM.void; x.beginPath(); x.arc(c, c, 50, 0, 2 * P); x.fill(); eye(x, c, c, 46, 1.4);
  },
  // An eye-shaped frame: lids of smoked glass curving over a dark field.
  'ssd.bezel': (x, w, h) => {
    frame(x, w, h, 18, 40);
    for (const s of [1, -1]) { x.save(); if (s < 0) { x.translate(0, h); x.scale(1, -1); } glow(x, () => { x.beginPath(); x.moveTo(60, 30); x.quadraticCurveTo(w / 2, -2, w - 60, 30); }, OM.lilac, 1.4, 6, 0.7); x.restore(); }
  },
  // A ring housing for the iris radar.
  'dial': (x, w) => { const c = w / 2; glass(x, () => { x.beginPath(); x.arc(c, c, 58, 0, 2 * P); }, [0, 0, w, w]); x.fillStyle = OM.void; x.beginPath(); x.arc(c, c, 48, 0, 2 * P); x.fill(); glow(x, () => { x.beginPath(); x.arc(c, c, 48, 0, 2 * P); }, OM.lilac, 1, 4, 0.6); },
  'lamp.alert.green': (x, w) => moon(x, w, '#7affc8'), 'lamp.alert.yellow': (x, w) => moon(x, w, '#ffd36a'), 'lamp.alert.red': (x, w) => moon(x, w, OM.crimson),
  // The Ombrai sign (original): a crescent moon cradling an open eye, three stars above.
  'crest': (x, w) => {
    const c = w / 2;
    x.save(); x.shadowColor = OM.pink; x.shadowBlur = 10; x.fillStyle = OM.lilac; x.beginPath(); x.arc(c, c + 6, 36, 0.15 * P, 0.85 * P); x.arc(c, c - 6, 34, 0.82 * P, 0.18 * P, true); x.closePath(); x.fill(); x.restore();
    eye(x, c, c + 4, 18, 1.2);
    x.fillStyle = OM.pink; for (const [dx, dy] of [[-16, -32], [0, -38], [16, -32]]) { x.beginPath(); x.arc(c + dx, c + dy, 3, 0, 2 * P); x.fill(); }
  },
};
// A lamp: a glowing moon with a crescent shadow across it.
function moon(x, w, col) {
  const c = w / 2;
  glass(x, () => { x.beginPath(); x.arc(c, c, 30, 0, 2 * P); }, [0, 0, w, w]);
  x.save(); x.shadowColor = col; x.shadowBlur = 16; x.fillStyle = col; x.beginPath(); x.arc(c, c, 20, 0, 2 * P); x.fill(); x.restore();
  x.fillStyle = 'rgba(18,8,34,0.55)'; x.beginPath(); x.arc(c + 9, c - 5, 18, 0, 2 * P); x.fill();
}

// ---- icons: eyes, moons, rings, petals, dots ---------------------------------------------------------------------
function icon(name) {
  return F.paint(48, 48, (x) => {
    x.strokeStyle = OM.pink; x.fillStyle = OM.pink; x.lineWidth = 2; x.shadowColor = OM.pink; x.shadowBlur = 5;
    const C = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.stroke(); };
    const D = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); };
    const EYE = (cx, cy, rx, ry) => { x.beginPath(); x.moveTo(cx - rx, cy); x.quadraticCurveTo(cx, cy - ry * 2, cx + rx, cy); x.quadraticCurveTo(cx, cy + ry * 2, cx - rx, cy); x.stroke(); x.beginPath(); x.ellipse(cx, cy, 2, ry * 0.8, 0, 0, 2 * P); x.fill(); };
    const MOON = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0.3 * P, 1.7 * P); x.arc(cx + r * 0.45, cy, r * 0.8, 1.62 * P, 0.38 * P, true); x.closePath(); x.fill(); };
    ({
      helm: () => { C(24, 24, 18); x.beginPath(); x.ellipse(24, 14, 3, 8, 0, 0, 2 * P); x.fill(); },
      fire: () => { EYE(24, 24, 20, 9); C(24, 24, 4); },
      shields: () => { x.beginPath(); x.arc(24, 30, 18, P * 1.1, P * 1.9); x.stroke(); x.beginPath(); x.arc(24, 30, 11, P * 1.15, P * 1.85); x.stroke(); D(24, 30, 3); },
      tractor: () => { MOON(14, 24, 10); x.save(); x.translate(48, 0); x.scale(-1, 1); MOON(14, 24, 10); x.restore(); },
      transport: () => { for (let i = 0; i < 5; i++) D(24 + Math.cos(i * 1.3) * (4 + i * 3.4), 24 + Math.sin(i * 1.3) * (4 + i * 3.4), 1.8 + i * 0.4); },
      science: () => { EYE(24, 24, 18, 8); C(24, 24, 8); x.beginPath(); x.moveTo(24, 4); x.lineTo(24, 10); x.moveTo(24, 38); x.lineTo(24, 44); x.stroke(); },
      comms: () => { D(12, 24, 3.5); for (const r of [10, 18, 26]) { x.beginPath(); x.arc(12, 24, r, -0.7, 0.7); x.stroke(); } },
      repair: () => { for (let i = 0; i < 4; i++) { x.save(); x.translate(24, 24); x.rotate(i * P / 2); x.beginPath(); x.moveTo(0, 0); x.quadraticCurveTo(10, -8, 16, 0); x.quadraticCurveTo(10, 8, 0, 0); x.stroke(); x.restore(); } D(24, 24, 3); },
      map: () => { C(24, 24, 18); C(24, 24, 9); D(32, 14, 2.6); D(14, 30, 2.6); D(30, 34, 2); },
      settings: () => { MOON(24, 24, 18); },
    })[name]();
  });
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('ombrai', {
  name: 'Ombrai', kind: 'Smoked violet glass, pink light; every gauge a watching eye', bg: '#0b0516',
  css: { '--s-ink': OM.pale, '--s-dim': OM.dim, '--s-num': OM.pink, '--s-body': '"Quicksand", sans-serif', '--btn-ink': OM.pale, '--btn-shadow': `0 0 6px ${OM.pink}`, '--btn-justify': 'center',
    '--btn-font': '700 13px/1 "Syncopate", sans-serif', '--btn-track': '0.14em', '--btn-case': 'uppercase', '--btn-motion': 'filter 0.4s', '--btn-press': 'none',
    '--tab-font': '700 10px/1 "Syncopate", sans-serif', '--tab-ink': OM.dim, '--tab-ink-on': OM.pale, '--title-font': '700 14px/1 "Syncopate", sans-serif', '--title-ink': OM.lilac,
    '--lbl-font': '700 15px/1 "Quicksand", sans-serif', '--num-font': '500 28px/1 "Quicksand", sans-serif', '--bt-rep': 'round', '--bf-rep': 'round', '--bfw-rep': 'round' },
  accent: OM.pink, warn: OM.crimson,
  title: (t, m) => { t.style.cssText = `left:${46 - m[3]}px;top:${-m[0] + 8}px;letter-spacing:0.22em;text-shadow:0 0 8px ${OM.pink};`; },
  plateText: { left: 70, top: 14, width: 360, height: 28, font: '700 13px/1 "Syncopate", sans-serif', color: OM.pale, sub: 'VEIL OF ISH · THE WATCHER' },
  helmKeys: 'ring', helmR: 98, shield: [OM.pink, '#ffd36a', OM.crimson],
  fonts: ['700 20px "Syncopate"', '700 20px "Quicksand"', '500 20px "Quicksand"'], view: { title: '700 20px "Syncopate"', body: '500 26px "Quicksand"', upper: true },
  build: () => F.fromPainters(nine, hero, icon, false),
  live: {
    // A comet: a small moon trailing light.
    arrow: (x) => { x.shadowColor = OM.pink; x.shadowBlur = 8; const g = x.createLinearGradient(-14, 0, 8, 0); g.addColorStop(0, 'rgba(255,79,176,0)'); g.addColorStop(1, OM.pale); x.fillStyle = g; x.beginPath(); x.moveTo(-14, -2); x.lineTo(6, -5); x.arc(6, 0, 5, -P / 2, P / 2); x.lineTo(-14, 2); x.closePath(); x.fill(); },
    // A petal hull with an eye at its heart; each shield is a ring arc that fills like an iris.
    ssd: (x, cx, cy, sh, col) => {
      for (let i = 0; i < 6; i++) {
        const a0 = -P / 2 + i * P / 3 - P / 6 + 0.1, a1 = a0 + P / 3 - 0.2;
        x.strokeStyle = hexA(OM.lilac, 0.25); x.lineWidth = 8; x.beginPath(); x.arc(cx, cy, 100, a0, a1); x.stroke();
        glow(x, () => { x.beginPath(); x.arc(cx, cy, 100, a0, a0 + (a1 - a0) * sh[i]); }, col(sh[i]), 8, 8, 0.9);
      }
      const petal = (s) => () => { x.beginPath(); x.moveTo(cx, cy - 74); x.quadraticCurveTo(cx + s * 70, cy - 10, cx, cy + 70); x.quadraticCurveTo(cx + s * 24, cy, cx, cy - 74); x.closePath(); };
      for (const s of [1, -1]) { glass(x, petal(s), [cx - 70, cy - 74, cx + 70, cy + 70], { top: '#5a3a7a' }); glow(x, petal(s), OM.pink, 1, 5, 0.7); }
      eye(x, cx, cy, 18);
      for (const [mx, my] of [[cx, cy - 56], [cx - 30, cy + 22], [cx + 30, cy + 22]]) { x.save(); x.shadowColor = OM.pink; x.shadowBlur = 8; x.fillStyle = OM.pale; x.beginPath(); x.arc(mx, my, 3.5, 0, 2 * P); x.fill(); x.restore(); }
    },
    // An iris: three rings filling clockwise, a slit pupil, contacts as pink motes.
    dial: (x, c, cts) => {
      for (const [r, f] of [[42, 0.82], [34, 0.55], [26, 0.3]]) { x.strokeStyle = hexA(OM.lilac, 0.25); x.lineWidth = 4; x.beginPath(); x.arc(c, c, r, 0, 2 * P); x.stroke(); glow(x, () => { x.beginPath(); x.arc(c, c, r, -P / 2, -P / 2 + 2 * P * f); }, OM.pink, 4, 6, 0.9); }
      x.save(); x.shadowColor = OM.pink; x.shadowBlur = 8; x.fillStyle = OM.pale; x.beginPath(); x.ellipse(c, c, 2.4, 12, 0, 0, 2 * P); x.fill(); x.restore();
      for (const [dx, dy] of cts) { x.save(); x.shadowColor = OM.crimson; x.shadowBlur = 6; x.fillStyle = OM.crimson; x.beginPath(); x.arc(c + dx * 44, c + dy * 44, 3, 0, 2 * P); x.fill(); x.restore(); }
    },
    // Four crescents curling round the target's corners.
    bracket: (x, bx, by, bw, bh) => {
      for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
        x.save(); x.translate(px, py); x.scale(sx, sy); glow(x, () => { x.beginPath(); x.arc(30, 30, 30, P, 1.5 * P); }, OM.pink, 3, 10, 0.95);
        x.fillStyle = OM.pale; x.beginPath(); x.arc(4, 4, 3, 0, 2 * P); x.fill(); x.restore();
      }
    },
  },
  world: ['Hidden purpose: they watch, and are seldom seen.', 'Shadowy and many-eyed; their glances do the work of hands.', 'Technology that looks back at you: every instrument an eye.', 'Dark, curved, petal-like hulls with glowing eyes.'],
  shapes: 'Crescents, petals, moons, rings and eyes; nothing straight if a curve will do.', light: 'Soft pink glow inside smoked violet glass, a faint iridescent sheen.',
  motion: 'Light swells slowly, like an eye opening.', type: '"Syncopate" wide capitals over "Quicksand".',
  palette: [['Void', OM.void, 'Fields', 'Secrecy'], ['Deep purple', OM.deep, 'Glass', 'The dream they live in'], ['Smoke', OM.smoke, 'Frames', 'Veils'],
    ['Pink', OM.pink, 'Keys, readouts', 'Attention: the watching gaze'], ['Lilac', OM.lilac, 'Rings, titles', 'Memory'], ['Crimson', OM.crimson, 'Alerts', 'A glance that turns hostile']],
});
})();
