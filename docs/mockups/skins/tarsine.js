// The Tarsine Remnant skin (mockup 4, OpenSpec change console-skins): a powerful ancient race that stopped changing
// long ago (owner, 2026-10-30: "a powerful ancient race. rusty, with back lit buttons clogged with dirt. their ships
// havent been upgraded in ages, rough around the edges"). Monumental corroded iron, pitted and streaked with rust, old
// gold worn through to the metal, grime in every corner; keys are arched tablets whose backlit windows glow through
// caked dirt; gauges sit behind cracked glass; icons are worn carved glyphs. Painted in canvas, deterministic.
// Registers itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
const TA = { soot: '#1c1814', iron: '#3a332c', ironHi: '#6a5e50', rust: '#7a3b1c', rustHi: '#b0602e', verd: '#4f6b5a', gold: '#a88a46', goldHi: '#d8bc6e',
  light: '#d6c76a', ember: '#d2482a', bone: '#e8dcc0', dim: '#a8987a', glass: '#0e0c0a' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
const RNG = (k) => F.rng(F.hash('tarsine.' + k));

// ---- materials ---------------------------------------------------------------------------------------------------
// Corroded iron: dark plate, rust blooms, pitting, grime pooled toward the bottom, a worn bright edge broken by rust.
function iron(x, draw, box, R, o = {}) {
  const [x0, y0, x1, y1] = box, w = x1 - x0, h = y1 - y0;
  x.save(); draw(); x.clip();
  const g = x.createLinearGradient(x0, y0, x0, y1); g.addColorStop(0, o.top || '#4a4038'); g.addColorStop(1, o.bottom || '#241e1a'); x.fillStyle = g; x.fillRect(x0, y0, w, h);
  const n = Math.max(3, Math.round(w * h / (o.rustEvery || 900)));
  for (let i = 0; i < n; i++) {
    const cx = x0 + R() * w, cy = y0 + R() * h, r = 3 + R() * 12, b = x.createRadialGradient(cx, cy, 0, cx, cy, r);
    b.addColorStop(0, hexA(R() < 0.7 ? TA.rust : TA.verd, 0.5 + R() * 0.3)); b.addColorStop(0.6, hexA(TA.rust, 0.18)); b.addColorStop(1, 'rgba(0,0,0,0)'); x.fillStyle = b; x.fillRect(cx - r, cy - r, 2 * r, 2 * r);
  }
  for (let i = 0; i < w * h / 70; i++) { const px = x0 + R() * w, py = y0 + R() * h; x.fillStyle = 'rgba(10,8,6,0.45)'; x.fillRect(px, py, 1, 1); x.fillStyle = 'rgba(190,150,110,0.18)'; x.fillRect(px + 0.6, py + 0.6, 0.6, 0.6); }
  const gr = x.createLinearGradient(x0, y0 + h * 0.55, x0, y1); gr.addColorStop(0, 'rgba(0,0,0,0)'); gr.addColorStop(1, 'rgba(12,9,6,0.45)'); x.fillStyle = gr; x.fillRect(x0, y0, w, h);
  x.restore();
  // the worn edge: bright where hands and time rubbed it, broken where rust ate it
  x.save(); draw(); x.clip(); x.translate(1, 1); draw(); x.strokeStyle = 'rgba(200,180,150,0.35)'; x.setLineDash([6 + R() * 10, 2 + R() * 4]); x.lineWidth = 1.4; x.stroke(); x.restore();
  draw(); x.strokeStyle = '#0c0907'; x.lineWidth = 1.4; x.stroke();
}
// A rust streak running down from a point (a rivet, a seam): rain on old metal.
function streak(x, px, py, len, R) {
  const g = x.createLinearGradient(px, py, px, py + len); g.addColorStop(0, hexA(TA.rustHi, 0.55)); g.addColorStop(1, 'rgba(0,0,0,0)');
  x.strokeStyle = g; x.lineWidth = 1.6 + R() * 1.6; x.beginPath(); x.moveTo(px, py); x.quadraticCurveTo(px + (R() - 0.5) * 3, py + len / 2, px + (R() - 0.5) * 4, py + len); x.stroke();
}
function rivet(x, px, py, r, R) {
  streak(x, px, py + r, 8 + R() * 10, R);
  const g = x.createRadialGradient(px - r * 0.4, py - r * 0.4, 0.5, px, py, r); g.addColorStop(0, '#9a8a74'); g.addColorStop(0.5, '#5a4a3a'); g.addColorStop(1, '#1c1510');
  x.fillStyle = g; x.beginPath(); x.arc(px, py, r, 0, 2 * P); x.fill(); x.strokeStyle = '#0c0907'; x.lineWidth = 0.8; x.stroke();
}
// Light through a dirty window: an uneven backlight, caked grime, specks and a smear, cracks if asked.
function grimeLight(x, draw, box, level, R, col = TA.light, cracked = false) {
  const [x0, y0, x1, y1] = box, w = x1 - x0, h = y1 - y0;
  x.save(); draw(); x.clip();
  x.fillStyle = TA.glass; x.fillRect(x0, y0, w, h);
  if (level > 0) {
    const g = x.createRadialGradient(x0 + w * (0.4 + R() * 0.2), y0 + h * 0.5, 1, x0 + w * 0.5, y0 + h * 0.5, Math.max(w, h) * 0.75);
    g.addColorStop(0, hexA(col, 0.1 + level * 0.85)); g.addColorStop(0.7, hexA(col, 0.05 + level * 0.5)); g.addColorStop(1, hexA(col, 0.02 + level * 0.2));
    x.fillStyle = g; x.fillRect(x0, y0, w, h);
  }
  for (let i = 0; i < 6 + w * h / 160; i++) { const cx = x0 + R() * w, cy = y0 + R() * h, r = 1 + R() * 5, b = x.createRadialGradient(cx, cy, 0, cx, cy, r); b.addColorStop(0, `rgba(30,22,12,${0.25 + R() * 0.4})`); b.addColorStop(1, 'rgba(0,0,0,0)'); x.fillStyle = b; x.fillRect(cx - r, cy - r, 2 * r, 2 * r); }
  for (let i = 0; i < w * h / 25; i++) { x.fillStyle = `rgba(20,14,8,${0.2 + R() * 0.5})`; x.fillRect(x0 + R() * w, y0 + R() * h, 0.8, 0.8); }
  const sm = x.createLinearGradient(x0, y1, x1, y0); sm.addColorStop(0.3, 'rgba(40,30,18,0)'); sm.addColorStop(0.45, 'rgba(40,30,18,0.35)'); sm.addColorStop(0.6, 'rgba(40,30,18,0)'); x.fillStyle = sm; x.fillRect(x0, y0, w, h);
  if (cracked) { x.strokeStyle = 'rgba(220,210,190,0.35)'; x.lineWidth = 0.7; for (let c = 0; c < 2; c++) { let cx = x0 + R() * w, cy = y0 + R() * h * 0.3; x.beginPath(); x.moveTo(cx, cy); for (let k = 0; k < 5; k++) { cx += (R() - 0.3) * 14; cy += R() * 12; x.lineTo(cx, cy); } x.stroke(); } }
  x.restore();
  draw(); x.strokeStyle = '#0c0907'; x.lineWidth = 1.2; x.stroke();
}

// ---- the nine-slice pieces --------------------------------------------------------------------------------------
// A monumental frame: a thick stepped iron border round dark grimy glass, stepped capitals and great rivets at the
// corners, rust running from each.
function frame(x, w, h, R, b) {
  const outer = () => { x.beginPath(); x.rect(1, 1, w - 2, h - 2); };
  iron(x, outer, [0, 0, w, h], R);
  // the step: a second, lower lip inside the border
  x.strokeStyle = 'rgba(0,0,0,0.6)'; x.lineWidth = 2; x.strokeRect(b * 0.55, b * 0.55, w - b * 1.1, h - b * 1.1);
  x.strokeStyle = 'rgba(200,180,150,0.18)'; x.lineWidth = 1; x.strokeRect(b * 0.55 + 1.5, b * 0.55 + 1.5, w - b * 1.1 - 3, h - b * 1.1 - 3);
  grimeLight(x, () => { x.beginPath(); x.rect(b, b, w - 2 * b, h - 2 * b); }, [b, b, w - b, h - b], 0, R);
  for (const [cx, cy] of [[0, 0], [w - b - 6, 0], [0, h - b - 6], [w - b - 6, h - b - 6]]) {
    iron(x, () => { x.beginPath(); x.rect(cx + 1, cy + 1, b + 4, b + 4); }, [cx, cy, cx + b + 6, cy + b + 6], R, { top: '#57493c' });
    x.strokeStyle = hexA(TA.gold, 0.55); x.lineWidth = 1; x.strokeRect(cx + 4, cy + 4, b - 2, b - 2);
    rivet(x, cx + (b + 6) / 2, cy + (b + 6) / 2, 4, R);
  }
}
// The arched tablet: a round-topped slab standing on a wider foot, a backlit window in it clogged with dirt.
function tablet(level, sunk) {
  return (x, w, h) => {
    const R = RNG('tablet'), d = sunk ? 2 : 0, r = 20;
    const slab = () => { x.beginPath(); x.moveTo(6, h - 6); x.lineTo(6, r + 2 + d); x.arcTo(6, 2 + d, 6 + r, 2 + d, r); x.lineTo(w - 6 - r, 2 + d); x.arcTo(w - 6, 2 + d, w - 6, r + 2 + d, r); x.lineTo(w - 6, h - 6); x.closePath(); };
    const foot = () => { x.beginPath(); x.rect(1, h - 7, w - 2, 6); };
    iron(x, slab, [0, 0, w, h], R, { rustEvery: 600 });
    iron(x, foot, [0, h - 7, w, h], R, { top: '#4f443a' });
    const win = () => { const rr = 13; x.beginPath(); x.moveTo(14, h - 12); x.lineTo(14, rr + 9 + d); x.arcTo(14, 9 + d, 14 + rr, 9 + d, rr); x.lineTo(w - 14 - rr, 9 + d); x.arcTo(w - 14, 9 + d, w - 14, rr + 9 + d, rr); x.lineTo(w - 14, h - 12); x.closePath(); };
    grimeLight(x, win, [14, 9, w - 14, h - 12], level, RNG('win.' + level));
    if (sunk) { x.save(); slab(); x.clip(); x.strokeStyle = 'rgba(0,0,0,0.6)'; x.lineWidth = 6; slab(); x.stroke(); x.restore(); }
    rivet(x, 10, h - 4, 2.2, R); rivet(x, w - 10, h - 4, 2.2, R);
  };
}
// A tab: a lintel bar of iron, chipped at the ends; the active one shows a slot of light.
function lintel(active) {
  return (x, w, h) => {
    const R = RNG('lintel.' + active);
    iron(x, () => { x.beginPath(); x.moveTo(4, 6); x.lineTo(w - 6, 4); x.lineTo(w - 2, 10); x.lineTo(w - 3, h - 4); x.lineTo(6, h - 3); x.lineTo(2, h - 10); x.closePath(); }, [0, 0, w, h], R);
    grimeLight(x, () => { x.beginPath(); x.rect(10, h - 12, w - 20, 5); }, [10, h - 12, w - 10, h - 7], active ? 0.9 : 0.05, R);
  };
}
// A strip: an iron beam with a worn gold stripe, heavy brackets riveted at each end.
function beam(flip) {
  return (x, w, h) => {
    const R = RNG('beam.' + flip);
    if (flip) { x.translate(0, h); x.scale(1, -1); }
    iron(x, () => { x.beginPath(); x.rect(0, 30, w, 24); }, [0, 30, w, 54], R);
    x.fillStyle = hexA(TA.gold, 0.5); x.fillRect(0, 36, w, 3); x.fillStyle = 'rgba(0,0,0,0.25)'; for (let i = 0; i < 30; i++) x.fillRect(R() * w, 36, 2 + R() * 8, 3);
    for (const s of [1, -1]) {
      x.save(); if (s < 0) { x.translate(w, 0); x.scale(-1, 1); }
      iron(x, () => { x.beginPath(); x.moveTo(1, 2); x.lineTo(70, 2); x.lineTo(92, 26); x.lineTo(92, 55); x.lineTo(1, 55); x.closePath(); }, [0, 0, 94, 56], R, { top: '#57493c' });
      x.strokeStyle = hexA(TA.gold, 0.5); x.lineWidth = 1.2; x.beginPath(); x.moveTo(8, 8); x.lineTo(64, 8); x.lineTo(84, 30); x.stroke();
      rivet(x, 16, 20, 4, R); rivet(x, 40, 20, 4, R); rivet(x, 16, 42, 4, R); rivet(x, 70, 42, 4, R);
      x.restore();
    }
  };
}
const nine = {
  'frame.panel': { size: [160, 120], m: [34, 34, 34, 34], draw: (x, w, h) => frame(x, w, h, RNG('panel'), 22) },
  'frame.inset': { size: [64, 48], m: [14, 14, 14, 14], draw: (x, w, h) => { const R = RNG('inset'); iron(x, () => { x.beginPath(); x.rect(1, 1, w - 2, h - 2); }, [0, 0, w, h], R); grimeLight(x, () => { x.beginPath(); x.rect(7, 7, w - 14, h - 14); }, [7, 7, w - 7, h - 7], 0, R); } },
  'bar.track': { size: [96, 16], m: [0, 14, 0, 14], inset: [4, 3], draw: (x, w, h) => { const R = RNG('track'); iron(x, () => { x.beginPath(); x.rect(0, 0, w, h); }, [0, 0, w, h], R); grimeLight(x, () => { x.beginPath(); x.rect(4, 3, w - 8, h - 6); }, [4, 3, w - 4, h - 3], 0, R); rivet(x, 4, h / 2, 2, R); rivet(x, w - 4, h / 2, 2, R); } },
  'bar.fill': { size: [64, 10], m: [0, 6, 0, 6], draw: (x, w, h) => grimeLight(x, () => { x.beginPath(); x.rect(0, 0, w, h); }, [0, 0, w, h], 0.95, RNG('fill')) },
  'bar.fill.red': { size: [64, 10], m: [0, 6, 0, 6], draw: (x, w, h) => grimeLight(x, () => { x.beginPath(); x.rect(0, 0, w, h); }, [0, 0, w, h], 0.95, RNG('fillr'), TA.ember) },
  'button.idle': { size: [140, 52], m: [24, 30, 12, 30], draw: tablet(0.45, false) },
  'button.hover': { size: [140, 52], m: [24, 30, 12, 30], draw: tablet(0.8, false) },
  'button.pressed': { size: [140, 52], m: [24, 30, 12, 30], draw: tablet(1, true) },
  'tab.idle': { size: [120, 40], m: [12, 14, 12, 14], draw: lintel(false) },
  'tab.active': { size: [120, 40], m: [12, 14, 12, 14], draw: lintel(true) },
  'strip.top': { size: [512, 56], m: [10, 100, 10, 100], draw: beam(false) },
  'strip.bottom': { size: [512, 56], m: [10, 100, 10, 100], draw: beam(true) },
  'divider': { size: [128, 10], m: [0, 14, 0, 14], draw: (x, w, h) => { const R = RNG('div'); iron(x, () => { x.beginPath(); x.rect(6, 3, w - 12, 4); }, [6, 3, w - 6, 7], R); rivet(x, 6, h / 2, 3.5, R); rivet(x, w - 6, h / 2, 3.5, R); } },
};

// ---- the hero pieces ---------------------------------------------------------------------------------------------
const hero = {
  // An iron plaque with a worn gold field for the name, engraved like a monument.
  'nameplate': (x, w, h) => {
    const R = RNG('plate');
    iron(x, () => { x.beginPath(); x.moveTo(14, 1); x.lineTo(w - 14, 1); x.lineTo(w - 1, h / 2); x.lineTo(w - 14, h - 1); x.lineTo(14, h - 1); x.lineTo(1, h / 2); x.closePath(); }, [0, 0, w, h], R);
    const field = () => { x.beginPath(); x.rect(36, 9, w - 72, h - 18); };
    x.save(); field(); x.clip(); const g = x.createLinearGradient(0, 9, 0, h - 9); g.addColorStop(0, TA.goldHi); g.addColorStop(1, '#6a5426'); x.fillStyle = g; x.fillRect(36, 9, w - 72, h - 18);
    for (let i = 0; i < 40; i++) { x.fillStyle = `rgba(40,28,14,${0.2 + R() * 0.4})`; x.beginPath(); x.arc(36 + R() * (w - 72), 9 + R() * (h - 18), 1 + R() * 4, 0, 2 * P); x.fill(); }
    x.restore(); field(); x.strokeStyle = '#0c0907'; x.lineWidth = 2; x.stroke();
    rivet(x, 20, h / 2, 4, R); rivet(x, w - 20, h / 2, 4, R);
  },
  // A temple keyboard: nine arched tablets in a heavy stepped frame; the centre one lit like a hearth.
  'helm.pad': (x, w, h) => {
    const R = RNG('helm');
    frame(x, w, h, R, 18);
    for (let r = 0; r < 3; r++) for (let c = 0; c < 3; c++) {
      x.save(); x.translate(26 + c * 86, 24 + r * 86); tablet(r === 1 && c === 1 ? 1 : 0.35, false)(x, 76, 78); x.restore();
    }
  },
  // A monument frame round cracked, grimy glass for the ship.
  'ssd.bezel': (x, w, h) => {
    const R = RNG('ssd'); frame(x, w, h, R, 24);
    grimeLight(x, () => { x.beginPath(); x.rect(24, 24, w - 48, h - 48); }, [24, 24, w - 24, h - 24], 0.04, R, TA.light, true);
  },
  // An iron gauge housing round cracked glass, a worn scale.
  'dial': (x, w) => {
    const R = RNG('dial'), c = w / 2;
    iron(x, () => { x.beginPath(); x.arc(c, c, 58, 0, 2 * P); }, [0, 0, w, w], R);
    grimeLight(x, () => { x.beginPath(); x.arc(c, c, 44, 0, 2 * P); }, [c - 44, c - 44, c + 44, c + 44], 0.18, R, TA.light, true);
    x.strokeStyle = hexA(TA.bone, 0.55); x.lineWidth = 1.4; for (let i = 0; i <= 10; i++) { const a = P * 0.75 + i * P * 0.15; if (R() < 0.2) continue; x.beginPath(); x.moveTo(c + Math.cos(a) * 36, c + Math.sin(a) * 36); x.lineTo(c + Math.cos(a) * 42, c + Math.sin(a) * 42); x.stroke(); }
    for (const a of [0, P / 2, P, 1.5 * P]) rivet(x, c + Math.cos(a + P / 4) * 51, c + Math.sin(a + P / 4) * 51, 3, R);
  },
  'lamp.alert.green': (x, w) => bulb(x, w, '#8ad46a'), 'lamp.alert.yellow': (x, w) => bulb(x, w, TA.light), 'lamp.alert.red': (x, w) => bulb(x, w, TA.ember),
  // The Remnant's seal (original): a stepped crown of three tiers in a corroded ring, worn to the metal.
  'crest': (x, w) => {
    const R = RNG('crest'), c = w / 2;
    iron(x, () => { x.beginPath(); x.arc(c, c, 46, 0, 2 * P); }, [0, 0, w, w], R);
    const g = x.createLinearGradient(0, 18, 0, 78); g.addColorStop(0, TA.goldHi); g.addColorStop(1, '#6a5426');
    x.fillStyle = g; x.beginPath(); x.moveTo(c - 30, 70); x.lineTo(c - 30, 58); x.lineTo(c - 20, 58); x.lineTo(c - 20, 44); x.lineTo(c - 10, 44); x.lineTo(c - 10, 28); x.lineTo(c + 10, 28); x.lineTo(c + 10, 44); x.lineTo(c + 20, 44); x.lineTo(c + 20, 58); x.lineTo(c + 30, 58); x.lineTo(c + 30, 70); x.closePath(); x.fill();
    x.strokeStyle = '#0c0907'; x.lineWidth = 1.4; x.stroke();
    for (let i = 0; i < 30; i++) { x.fillStyle = hexA(TA.rust, 0.5); x.beginPath(); x.arc(c - 30 + R() * 60, 28 + R() * 42, 1 + R() * 2.5, 0, 2 * P); x.fill(); }
    x.strokeStyle = hexA(TA.gold, 0.6); x.lineWidth = 2; x.beginPath(); x.arc(c, c, 40, 0, 2 * P); x.stroke();
  },
};
// A lamp: an old caged bulb, dirty glass, a dim colour.
function bulb(x, w, col) {
  const R = RNG('bulb.' + col), c = w / 2;
  x.save(); x.shadowColor = col; x.shadowBlur = 10; grimeLight(x, () => { x.beginPath(); x.arc(c, c - 2, 20, 0, 2 * P); }, [c - 20, c - 22, c + 20, c + 18], 0.85, R, col); x.restore();
  x.strokeStyle = '#2a221a'; x.lineWidth = 2.4; for (const dx of [-10, 0, 10]) { x.beginPath(); x.moveTo(c + dx, c - 22); x.quadraticCurveTo(c + dx * 1.9, c - 2, c + dx, c + 18); x.stroke(); }
  iron(x, () => { x.beginPath(); x.rect(c - 18, c + 16, 36, 10); }, [c - 18, c + 16, c + 18, c + 26], R);
}

// ---- icons: worn carved glyphs ------------------------------------------------------------------------------------
function icon(name) {
  return F.paint(48, 48, (x) => {
    const R = RNG('icon.' + name);
    const S = (pts) => {
      for (const [col, dx, dy, lw] of [['rgba(0,0,0,0.7)', 0, 0, 4.4], [hexA(TA.goldHi, 0.85), 0, 0, 2.6], ['rgba(255,240,200,0.35)', -0.6, -0.6, 1]]) {
        x.strokeStyle = col; x.lineWidth = lw; x.lineCap = 'square'; x.beginPath(); x.moveTo(pts[0][0] + dx, pts[0][1] + dy); for (const p of pts.slice(1)) x.lineTo(p[0] + dx, p[1] + dy); x.stroke();
      }
    };
    ({
      helm: () => { S([[24, 42], [24, 6]]); S([[12, 18], [24, 6], [36, 18]]); S([[16, 30], [32, 30]]); },
      fire: () => { S([[8, 40], [24, 8], [40, 40]]); S([[24, 8], [24, 40]]); },
      shields: () => { S([[8, 8], [40, 8], [40, 26], [24, 42], [8, 26], [8, 8]]); S([[24, 8], [24, 42]]); },
      tractor: () => { S([[8, 8], [8, 40]]); S([[40, 8], [40, 40]]); S([[8, 24], [40, 24]]); S([[16, 16], [24, 24], [16, 32]]); },
      transport: () => { S([[24, 6], [24, 42]]); S([[10, 14], [24, 24], [38, 14]]); S([[10, 34], [24, 24], [38, 34]]); },
      science: () => { S([[24, 6], [40, 24], [24, 42], [8, 24], [24, 6]]); S([[24, 18], [24, 30]]); },
      comms: () => { S([[10, 42], [10, 6]]); S([[10, 10], [30, 18], [10, 26]]); S([[30, 30], [40, 26]]); S([[30, 38], [42, 38]]); },
      repair: () => { S([[8, 40], [36, 12]]); S([[30, 6], [42, 18]]); S([[14, 30], [20, 36]]); },
      map: () => { S([[6, 12], [18, 8], [30, 12], [42, 8]]); S([[6, 12], [6, 40], [18, 36], [30, 40], [42, 36], [42, 8]]); S([[18, 8], [18, 36]]); S([[30, 12], [30, 40]]); },
      settings: () => { S([[24, 6], [24, 42]]); S([[6, 24], [42, 24]]); S([[12, 12], [36, 36]]); S([[36, 12], [12, 36]]); },
    })[name]();
    // time wore parts of it away
    x.globalCompositeOperation = 'destination-out'; for (let i = 0; i < 5; i++) { x.fillStyle = `rgba(0,0,0,${0.4 + R() * 0.5})`; x.beginPath(); x.arc(R() * 48, R() * 48, 2 + R() * 4, 0, 2 * P); x.fill(); }
    x.globalCompositeOperation = 'source-over';
  });
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('tarsine', {
  name: 'Tarsine Remnant', kind: 'Corroded monumental iron; backlights clogged with dirt', bg: '#110e0b',
  css: { '--s-ink': TA.bone, '--s-dim': TA.dim, '--s-num': TA.light, '--s-body': '"Alegreya Sans SC", sans-serif', '--btn-ink': '#1e160c', '--btn-shadow': '0 1px 0 rgba(255,230,160,0.35)', '--btn-justify': 'center',
    '--btn-pt': '0px', '--btn-pb': '2px', '--btn-font': '700 17px/1 "Cinzel", serif', '--btn-track': '0.08em', '--btn-case': 'uppercase', '--btn-motion': 'none', '--btn-press': 'translateY(1px)',
    '--tab-font': '700 13px/1 "Cinzel", serif', '--tab-ink': TA.dim, '--tab-ink-on': TA.light, '--title-font': '700 17px/1 "Cinzel", serif', '--title-ink': TA.goldHi,
    '--lbl-font': '700 16px/1 "Alegreya Sans SC", sans-serif', '--num-font': '700 24px/1 "Alegreya Sans SC", sans-serif',
    // Strip padding is each strip's open band beside its beam (top rows 3-29, bottom 26-52), measured by
    // tools/mockups/console_fit.mjs.
    '--bar-flow': 'column', '--bar-cols': 'auto 1fr', '--bar-gap': '10px', '--stt-pt': '3px', '--stt-pb': '26px', '--stb-pt': '26px', '--stb-pb': '3px', '--strip-lamp': '21px' },
  accent: TA.light, warn: TA.ember,
  title: (t, m) => { t.style.cssText = `left:${40 - m[3]}px;top:${-m[0] + 15}px;letter-spacing:0.12em;text-shadow:0 1px 0 #000, 0 0 3px #000;`; },
  plateText: { left: 44, top: 12, width: 372, height: 32, font: '700 19px/1 "Cinzel", serif', color: '#2a1c0c', sub: 'ANKHAR · ELDEST VOR' },
  helmKeys: [[150, 63], [236, 63], [236, 149], [236, 235], [150, 235], [64, 235], [64, 149], [64, 63]], shield: [TA.light, '#c9873a', TA.ember],
  fonts: ['700 20px "Cinzel"', '700 20px "Alegreya Sans SC"'], view: { title: '700 26px "Cinzel"', body: '700 24px "Alegreya Sans SC"', upper: true },
  build: () => F.fromPainters(nine, hero, icon, false),
  live: {
    // A carved arrow, dark in the lit tablet.
    arrow: (x) => { x.fillStyle = '#1e160c'; x.beginPath(); x.moveTo(12, 0); x.lineTo(0, -10); x.lineTo(0, -4); x.lineTo(-11, -4); x.lineTo(-11, 4); x.lineTo(0, 4); x.lineTo(0, 10); x.closePath(); x.fill(); },
    helmCentre: (x, c) => { x.fillStyle = '#1e160c'; x.font = '700 18px Cinzel'; x.textAlign = 'center'; x.fillText('HALT', c, c + 6); },
    // A long scarred dreadnought in worn gold lines; shields as dim broken arcs.
    ssd: (x, cx, cy, sh, col) => {
      for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.strokeStyle = col(sh[i]); x.lineWidth = 6; x.setLineDash([10, 4, 3, 4]); x.beginPath(); x.arc(cx, cy, 100, a - P / 6 + 0.08, a + P / 6 - 0.08); x.stroke(); }
      x.setLineDash([]); x.strokeStyle = TA.goldHi; x.lineWidth = 2.2;
      x.beginPath(); x.moveTo(cx - 12, cy - 78); x.lineTo(cx + 12, cy - 78); x.lineTo(cx + 22, cy - 50); x.lineTo(cx + 22, cy - 10); x.lineTo(cx + 40, cy); x.lineTo(cx + 40, cy + 40); x.lineTo(cx + 22, cy + 50); x.lineTo(cx + 18, cy + 76); x.lineTo(cx - 18, cy + 76); x.lineTo(cx - 22, cy + 50); x.lineTo(cx - 40, cy + 40); x.lineTo(cx - 40, cy); x.lineTo(cx - 22, cy - 10); x.lineTo(cx - 22, cy - 50); x.closePath(); x.stroke();
      for (const y of [-40, -10, 22, 50]) { x.beginPath(); x.moveTo(cx - 20, cy + y); x.lineTo(cx + 20, cy + y); x.stroke(); }
      x.strokeStyle = 'rgba(210,72,42,0.7)'; x.lineWidth = 1.2; x.beginPath(); x.moveTo(cx + 26, cy + 4); x.lineTo(cx + 10, cy + 18); x.lineTo(cx + 16, cy + 30); x.stroke();
      x.fillStyle = TA.ember; for (const [mx, my] of [[cx, cy - 64], [cx - 32, cy + 20], [cx + 32, cy + 20], [cx, cy + 64]]) { x.fillRect(mx - 4, my - 4, 8, 8); }
    },
    // An old needle behind cracked glass, contacts as ember specks.
    dial: (x, c, cts) => {
      x.strokeStyle = TA.ember; x.lineWidth = 2.4; x.beginPath(); x.moveTo(c, c); x.lineTo(c + Math.cos(-0.9) * 34, c + Math.sin(-0.9) * 34); x.stroke();
      x.fillStyle = '#2a221a'; x.beginPath(); x.arc(c, c, 5, 0, 2 * P); x.fill();
      for (const [dx, dy] of cts) { x.fillStyle = hexA(TA.ember, 0.85); x.fillRect(c + dx * 30 - 3, c + dy * 30 - 3, 6, 6); }
    },
    // Heavy iron corner brackets with a rivet each.
    bracket: (x, bx, by, bw, bh) => {
      const R = RNG('bracket');
      for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
        x.save(); x.translate(px, py); x.scale(sx, sy);
        iron(x, () => { x.beginPath(); x.moveTo(0, 0); x.lineTo(46, 0); x.lineTo(46, 10); x.lineTo(10, 10); x.lineTo(10, 46); x.lineTo(0, 46); x.closePath(); }, [0, 0, 46, 46], R, { top: '#6a5a48' });
        rivet(x, 5, 5, 3, R); x.restore();
      }
    },
  },
  world: ['A fallen empire that remembers being first, and will not change.', 'Huge, slow and long-lived: the crews outlast their worlds.', 'Machines older than their crews, patched and never replaced.', 'Massive, scarred, rusted dreadnoughts.'],
  shapes: 'Arched tablets, stepped frames, lintels, heavy brackets: monuments.', light: 'Weak backlight through grime; worn bright edges on rusted iron.',
  motion: 'Almost none: a key gives a millimetre and the light behind it steadies.', type: '"Cinzel" carved capitals over "Alegreya Sans SC".',
  palette: [['Soot', TA.soot, 'Grime, shadow', 'Ages of use'], ['Rust', TA.rust, 'Frames', 'Decline they will not admit'], ['Verdigris', TA.verd, 'Corrosion', 'Time'],
    ['Old gold', TA.gold, 'Inlays, glyphs', 'Old majesty'], ['Backlight', TA.light, 'Keys, readouts', 'The fire still burning'], ['Ember', TA.ember, 'Alerts', 'Anger, slow to wake']],
});
})();
