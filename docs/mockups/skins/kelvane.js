// The Kelvane Institute skin (mockup 4, OpenSpec change console-skins): a race of experimenters who trust only what an
// instrument can show (owner, 2026-10-30, with the Nucleo NLog Winamp skin: "the old science, nucleo nlog skin from
// winamp. UI love this old osciloscope aesthetic from science labs. Some race will value thier anolog physics
// instruments and design their ui vbased on this"). Cream enamel faceplates warmed by a lamp from above, chrome bezels,
// domed chrome push buttons, amber backlit displays with engraved scales, phosphor-green oscilloscopes. Our own design
// after the feeling of lab instruments, nothing taken from the skin itself. Painted in canvas, deterministic.
// Registers itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, P = F.P;
const KV = { cream: '#e9dcc0', creamLo: '#b9a27a', chrome: '#cfd3d6', amber: '#ffcf6a', amberLo: '#d9a94a', phos: '#4dff88', needle: '#c8302a', ink: '#2e2416', dim: '#6a5634', crt: '#06140c' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
const RNG = (k) => F.rng(F.hash('kelvane.' + k));
const rrect = (x, X, Y, W, H, r) => () => { x.beginPath(); x.roundRect(X, Y, W, H, r); };

// ---- materials ---------------------------------------------------------------------------------------------------
// Cream enamel: warmed by a lamp above, shaded toward the bottom, a fine speckle, a bevel catching the light.
function enamel(x, draw, box, R) {
  const [x0, y0, x1, y1] = box, w = x1 - x0, h = y1 - y0;
  x.save(); draw(); x.clip();
  const g = x.createLinearGradient(x0, y0, x0, y1); g.addColorStop(0, '#f6ecd4'); g.addColorStop(0.6, KV.cream); g.addColorStop(1, '#cdbb94'); x.fillStyle = g; x.fillRect(x0, y0, w, h);
  const l = x.createRadialGradient(x0 + w / 2, y0 - h * 0.2, 2, x0 + w / 2, y0, Math.max(w, h) * 0.7); l.addColorStop(0, 'rgba(255,214,120,0.35)'); l.addColorStop(1, 'rgba(255,214,120,0)'); x.fillStyle = l; x.fillRect(x0, y0, w, h);
  for (let i = 0; i < w * h / 50; i++) { x.fillStyle = R() < 0.5 ? 'rgba(120,96,60,0.08)' : 'rgba(255,255,255,0.12)'; x.fillRect(x0 + R() * w, y0 + R() * h, 0.8, 0.8); }
  x.restore();
  x.save(); draw(); x.clip(); x.translate(1, 1); draw(); x.strokeStyle = 'rgba(255,255,255,0.7)'; x.lineWidth = 1.5; x.stroke(); x.translate(-2, -2); draw(); x.strokeStyle = 'rgba(110,86,50,0.45)'; x.lineWidth = 1.5; x.stroke(); x.restore();
  draw(); x.strokeStyle = '#7a6440'; x.lineWidth = 1; x.stroke();
}
// Chrome: bands of light and dark, as a polished curve reflects the room.
function chrome(x, draw, box, vertical = true) {
  const [x0, y0, x1, y1] = box;
  x.save(); draw(); x.clip();
  const g = vertical ? x.createLinearGradient(x0, y0, x0, y1) : x.createLinearGradient(x0, y0, x1, y0);
  g.addColorStop(0, '#ffffff'); g.addColorStop(0.18, '#c9ced3'); g.addColorStop(0.42, '#6e757c'); g.addColorStop(0.55, '#e9edf0'); g.addColorStop(0.8, '#8e959c'); g.addColorStop(1, '#4a5056');
  x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0); x.restore();
  draw(); x.strokeStyle = '#3a3e42'; x.lineWidth = 0.9; x.stroke();
}
// A domed chrome push button sitting in a dark socket, with an amber pilot ring round it when lit.
function dome(x, cx, cy, r, level = 0, sunk = false) {
  x.fillStyle = '#1e1a14'; x.beginPath(); x.arc(cx, cy, r + 3, 0, 2 * P); x.fill();
  if (level > 0) { x.save(); x.shadowColor = KV.amber; x.shadowBlur = 8 * level; x.strokeStyle = hexA(KV.amber, 0.5 + level * 0.5); x.lineWidth = 2; x.beginPath(); x.arc(cx, cy, r + 2, 0, 2 * P); x.stroke(); x.restore(); }
  const k = sunk ? 0.88 : 1, g = x.createRadialGradient(cx - r * 0.35, cy - r * 0.4, 0.5, cx, cy, r * k);
  g.addColorStop(0, '#ffffff'); g.addColorStop(0.3, sunk ? '#9aa1a8' : '#d8dde1'); g.addColorStop(0.75, '#6a7178'); g.addColorStop(1, '#2e3236');
  x.fillStyle = g; x.beginPath(); x.arc(cx, cy, r * k, 0, 2 * P); x.fill(); x.strokeStyle = '#2a2d30'; x.lineWidth = 0.8; x.stroke();
}
function screw(x, cx, cy, r = 2.6) {
  const g = x.createRadialGradient(cx - 0.8, cy - 0.8, 0.3, cx, cy, r); g.addColorStop(0, '#f2f2f0'); g.addColorStop(1, '#6a6e72');
  x.fillStyle = g; x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); x.strokeStyle = '#3a3e42'; x.lineWidth = 0.7; x.stroke();
  x.beginPath(); x.moveTo(cx - r * 0.7, cy + r * 0.3); x.lineTo(cx + r * 0.7, cy - r * 0.3); x.stroke();
}
// An amber backlit display, sunk into the plate: warm light, a glass sheen, a dark recessed rim.
function amberWin(x, draw, box, level = 1) {
  const [x0, y0, x1, y1] = box;
  x.save(); draw(); x.clip();
  const g = x.createLinearGradient(x0, y0, x0, y1); g.addColorStop(0, hexA('#fbe2a0', 0.6 + level * 0.4)); g.addColorStop(1, hexA(KV.amberLo, 0.7 + level * 0.3)); x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  const s = x.createLinearGradient(x0, y0, x1, y1); s.addColorStop(0.2, 'rgba(255,255,255,0)'); s.addColorStop(0.32, 'rgba(255,255,255,0.22)'); s.addColorStop(0.44, 'rgba(255,255,255,0)'); x.fillStyle = s; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.translate(1.5, 1.5); draw(); x.strokeStyle = 'rgba(70,46,14,0.45)'; x.lineWidth = 3; x.stroke();
  x.restore(); draw(); x.strokeStyle = '#4a3a22'; x.lineWidth = 1.4; x.stroke();
}
// A phosphor screen: deep green-black glass, a dim graticule, a soft glow at its heart.
function crt(x, draw, box, divs = [10, 8]) {
  const [x0, y0, x1, y1] = box, w = x1 - x0, h = y1 - y0;
  x.save(); draw(); x.clip(); x.fillStyle = KV.crt; x.fillRect(x0, y0, w, h);
  const g = x.createRadialGradient(x0 + w / 2, y0 + h / 2, 2, x0 + w / 2, y0 + h / 2, Math.max(w, h) * 0.6); g.addColorStop(0, 'rgba(77,255,136,0.10)'); g.addColorStop(1, 'rgba(0,0,0,0.3)'); x.fillStyle = g; x.fillRect(x0, y0, w, h);
  x.strokeStyle = 'rgba(77,255,136,0.16)'; x.lineWidth = 0.8;
  for (let i = 1; i < divs[0]; i++) { x.beginPath(); x.moveTo(x0 + w * i / divs[0], y0); x.lineTo(x0 + w * i / divs[0], y1); x.stroke(); }
  for (let i = 1; i < divs[1]; i++) { x.beginPath(); x.moveTo(x0, y0 + h * i / divs[1]); x.lineTo(x1, y0 + h * i / divs[1]); x.stroke(); }
  x.restore();
}

// ---- the nine-slice pieces --------------------------------------------------------------------------------------
// An instrument panel: a cream faceplate in a thin chrome bezel, screws at the corners, an amber display sunk in it.
function panel(x, w, h, b, R) {
  chrome(x, rrect(x, 0.5, 0.5, w - 1, h - 1, 12), [0, 0, w, h]);
  enamel(x, rrect(x, 3, 3, w - 6, h - 6, 10), [3, 3, w - 3, h - 3], R);
  amberWin(x, rrect(x, b, b, w - 2 * b, h - 2 * b, 6), [b, b, w - b, h - b]);
  for (const [cx, cy] of [[11, 11], [w - 11, 11], [11, h - 11], [w - 11, h - 11]]) screw(x, cx, cy);
}
// The instrument key: an enamel faceplate in a chrome bezel, a domed chrome button set into its left end.
function key(state) {
  return (x, w, h) => {
    const R = RNG('key'), sunk = state === 'pressed';
    chrome(x, rrect(x, 0.5, 2.5, w - 1, h - 5, 9), [0, 2, w, h - 2]);
    enamel(x, rrect(x, 3, 5, w - 6, h - 10, 7), [3, 5, w - 3, h - 5], R);
    if (sunk) { x.save(); rrect(x, 3, 5, w - 6, h - 10, 7)(); x.clip(); x.strokeStyle = 'rgba(70,50,20,0.35)'; x.lineWidth = 6; rrect(x, 3, 5, w - 6, h - 10, 7)(); x.stroke(); x.restore(); }
    dome(x, 22, h / 2, 12, { idle: 0, hover: 0.8, pressed: 1 }[state], sunk);
    x.strokeStyle = 'rgba(110,86,50,0.5)'; x.lineWidth = 1; x.beginPath(); x.moveTo(41, 10); x.lineTo(41, h - 10); x.stroke();
  };
}
// A tab: a toggle plate, a little chrome lever at its left; the active one thrown, its amber lamp lit.
function toggle(active) {
  return (x, w, h) => {
    const R = RNG('toggle.' + active);
    enamel(x, rrect(x, 1, 4, w - 2, h - 8, 6), [1, 4, w - 1, h - 4], R);
    x.fillStyle = '#2a241a'; x.beginPath(); x.arc(13, h / 2, 5, 0, 2 * P); x.fill();
    chrome(x, () => { x.beginPath(); x.roundRect(10.5, active ? h / 2 - 13 : h / 2, 5, 13, 2.5); }, [10, h / 2 - 13, 16, h / 2 + 13], false);
    x.save(); if (active) { x.shadowColor = KV.amber; x.shadowBlur = 8; } x.fillStyle = active ? KV.amber : '#5a4a30'; x.beginPath(); x.arc(w - 10, h / 2, 3, 0, 2 * P); x.fill(); x.restore();
  };
}
// The strip: a cream instrument rail with an engraved line; a small needle meter at the left end, a chrome knob at the right.
function rail(flip) {
  return (x, w, h) => {
    const R = RNG('rail.' + flip);
    if (flip) { x.translate(0, h); x.scale(1, -1); }
    chrome(x, rrect(x, 0.5, 0.5, w - 1, h - 1, 8), [0, 0, w, h]);
    enamel(x, rrect(x, 3, 3, w - 6, h - 6, 6), [3, 3, w - 3, h - 3], R);
    x.strokeStyle = 'rgba(110,86,50,0.55)'; x.lineWidth = 1; for (const y of [8, h - 8]) { x.beginPath(); x.moveTo(90, y); x.lineTo(w - 90, y); x.stroke(); }
    if (flip) { x.translate(0, h); x.scale(1, -1); }
    // the meter: an amber window with a ruled arc and a red needle (static, it is part of the rail)
    amberWin(x, rrect(x, 10, 9, 70, 38, 4), [10, 9, 80, 47]);
    x.strokeStyle = KV.ink; x.lineWidth = 0.8; for (let i = 0; i <= 8; i++) { const a = P * 1.2 + i * P * 0.075; x.beginPath(); x.moveTo(45 + Math.cos(a) * 26, 46 + Math.sin(a) * 26); x.lineTo(45 + Math.cos(a) * (i % 2 ? 22 : 20), 46 + Math.sin(a) * (i % 2 ? 22 : 20)); x.stroke(); }
    x.strokeStyle = KV.needle; x.lineWidth = 1.2; x.beginPath(); x.moveTo(45, 46); x.lineTo(45 + Math.cos(P * 1.62) * 26, 46 + Math.sin(P * 1.62) * 26); x.stroke();
    x.save(); x.translate(w - 44, h / 2); dome(x, 0, 0, 14); for (let i = 0; i < 12; i++) { const a = i * P / 6; x.strokeStyle = 'rgba(110,86,50,0.7)'; x.beginPath(); x.moveTo(Math.cos(a) * 18, Math.sin(a) * 18); x.lineTo(Math.cos(a) * 21, Math.sin(a) * 21); x.stroke(); } x.restore();
    screw(x, w - 12, 12); screw(x, w - 12, h - 12);
  };
}
const nine = {
  'frame.panel': { size: [160, 120], m: [26, 26, 26, 26], draw: (x, w, h) => panel(x, w, h, 18, RNG('panel')) },
  'frame.inset': { size: [64, 48], m: [12, 12, 12, 12], draw: (x, w, h) => { chrome(x, rrect(x, 0.5, 0.5, w - 1, h - 1, 7), [0, 0, w, h]); amberWin(x, rrect(x, 3, 3, w - 6, h - 6, 5), [3, 3, w - 3, h - 3]); } },
  // A ruled scale: one tick per tile, tiled along the bar; the fill is the lit amber column over it.
  'bar.track': { size: [8, 16], m: [0, 0, 0, 0], inset: [0, 3], draw: (x, w, h) => { x.fillStyle = '#3a2e1c'; x.fillRect(0, 0, w, h); x.fillStyle = '#5a4428'; x.fillRect(0, 3, w, h - 6); x.fillStyle = 'rgba(233,220,192,0.6)'; x.fillRect(0, 1, 1, 3); x.fillRect(0, h - 4, 1, 3); } },
  'bar.fill': { size: [8, 10], m: [0, 0, 0, 0], draw: (x, w, h) => { const g = x.createLinearGradient(0, 0, 0, h); g.addColorStop(0, '#fff2c0'); g.addColorStop(0.5, KV.amber); g.addColorStop(1, KV.amberLo); x.fillStyle = g; x.fillRect(0, 0, w, h); x.fillStyle = 'rgba(80,50,10,0.35)'; x.fillRect(w - 1, 0, 1, h); } },
  'bar.fill.red': { size: [8, 10], m: [0, 0, 0, 0], draw: (x, w, h) => { const g = x.createLinearGradient(0, 0, 0, h); g.addColorStop(0, '#ff9a80'); g.addColorStop(0.5, KV.needle); g.addColorStop(1, '#7a1a10'); x.fillStyle = g; x.fillRect(0, 0, w, h); x.fillStyle = 'rgba(40,10,6,0.35)'; x.fillRect(w - 1, 0, 1, h); } },
  'button.idle': { size: [150, 48], m: [20, 16, 20, 46], draw: key('idle') },
  'button.hover': { size: [150, 48], m: [20, 16, 20, 46], draw: key('hover') },
  'button.pressed': { size: [150, 48], m: [20, 16, 20, 46], draw: key('pressed') },
  'tab.idle': { size: [120, 40], m: [14, 18, 14, 26], draw: toggle(false) },
  'tab.active': { size: [120, 40], m: [14, 18, 14, 26], draw: toggle(true) },
  'strip.top': { size: [512, 56], m: [10, 88, 10, 90], draw: rail(false) },
  'strip.bottom': { size: [512, 56], m: [10, 88, 10, 90], draw: rail(true) },
  'divider': { size: [128, 10], m: [0, 12, 0, 12], draw: (x, w, h) => { x.strokeStyle = 'rgba(110,86,50,0.7)'; x.lineWidth = 1; for (const y of [3.5, 6.5]) { x.beginPath(); x.moveTo(10, y); x.lineTo(w - 10, y); x.stroke(); } screw(x, 5, h / 2, 2.4); screw(x, w - 5, h / 2, 2.4); } },
};

// ---- the hero pieces ---------------------------------------------------------------------------------------------
const hero = {
  // A cream plate with an amber display for the name and an engraved maker's line.
  'nameplate': (x, w, h) => {
    const R = RNG('plate');
    chrome(x, rrect(x, 0.5, 0.5, w - 1, h - 1, 10), [0, 0, w, h]); enamel(x, rrect(x, 3, 3, w - 6, h - 6, 8), [3, 3, w - 3, h - 3], R);
    amberWin(x, rrect(x, 46, 9, w - 100, h - 18, 4), [46, 9, w - 54, h - 9]);
    x.fillStyle = 'rgba(110,86,50,0.85)'; x.font = '700 9px "Courier Prime", monospace'; x.save(); x.translate(26, h / 2); x.rotate(-P / 2); x.textAlign = 'center'; x.fillText('KV-2', 0, 3); x.restore();
    x.save(); x.translate(w - 28, h / 2); dome(x, 0, 0, 10, 0.6); x.restore(); screw(x, 12, 12); screw(x, 12, h - 12);
  },
  // A control plate: eight domed keys round a large chrome knob, an engraved ring of ticks.
  'helm.pad': (x, w, h) => {
    const R = RNG('helm'), c = w / 2;
    chrome(x, rrect(x, 0.5, 0.5, w - 1, h - 1, 18), [0, 0, w, h]); enamel(x, rrect(x, 4, 4, w - 8, h - 8, 15), [4, 4, w - 4, h - 4], R);
    x.strokeStyle = 'rgba(110,86,50,0.6)'; x.lineWidth = 1; x.beginPath(); x.arc(c, c, 128, 0, 2 * P); x.stroke();
    for (let i = 0; i < 48; i++) { const a = i * P / 24, r1 = i % 6 ? 124 : 118; x.beginPath(); x.moveTo(c + Math.cos(a) * 128, c + Math.sin(a) * 128); x.lineTo(c + Math.cos(a) * r1, c + Math.sin(a) * r1); x.stroke(); }
    for (let i = 0; i < 8; i++) { const a = -P / 2 + i * P / 4; dome(x, c + Math.cos(a) * 92, c + Math.sin(a) * 92, 20, 0.25); }
    dome(x, c, c, 40, 0.5); for (let i = 0; i < 24; i++) { const a = i * P / 12; x.strokeStyle = 'rgba(40,40,40,0.35)'; x.beginPath(); x.moveTo(c + Math.cos(a) * 34, c + Math.sin(a) * 34); x.lineTo(c + Math.cos(a) * 40, c + Math.sin(a) * 40); x.stroke(); }
    for (const [sx, sy] of [[16, 16], [w - 16, 16], [16, h - 16], [w - 16, h - 16]]) screw(x, sx, sy);
  },
  // An oscilloscope: a chrome bezel round a phosphor screen with its graticule, set in a cream case.
  'ssd.bezel': (x, w, h) => {
    const R = RNG('ssd');
    chrome(x, rrect(x, 0.5, 0.5, w - 1, h - 1, 14), [0, 0, w, h]); enamel(x, rrect(x, 4, 4, w - 8, h - 8, 12), [4, 4, w - 4, h - 4], R);
    chrome(x, rrect(x, 16, 12, w - 32, h - 24, 18), [16, 12, w - 16, h - 12]);
    crt(x, rrect(x, 22, 18, w - 44, h - 36, 14), [22, 18, w - 22, h - 18]);
    for (const [sx, sy] of [[10, 10], [w - 10, 10], [10, h - 10], [w - 10, h - 10]]) screw(x, sx, sy, 2.4);
  },
  // A round scope in a chrome ring.
  'dial': (x, w) => { const c = w / 2; chrome(x, () => { x.beginPath(); x.arc(c, c, 58, 0, 2 * P); }, [0, 0, w, w]); crt(x, () => { x.beginPath(); x.arc(c, c, 48, 0, 2 * P); }, [c - 48, c - 48, c + 48, c + 48], [6, 6]); },
  'lamp.alert.green': (x, w) => jewel(x, w, '#5aff8a'), 'lamp.alert.yellow': (x, w) => jewel(x, w, KV.amber), 'lamp.alert.red': (x, w) => jewel(x, w, '#ff4a3a'),
  // The Institute's mark (original): a tuning fork standing over a sine wave, engraved in a chrome roundel.
  'crest': (x, w) => {
    const c = w / 2, R = RNG('crest');
    chrome(x, () => { x.beginPath(); x.arc(c, c, 46, 0, 2 * P); }, [0, 0, w, w]); enamel(x, () => { x.beginPath(); x.arc(c, c, 40, 0, 2 * P); }, [c - 40, c - 40, c + 40, c + 40], R);
    x.strokeStyle = KV.ink; x.lineWidth = 3; x.lineCap = 'round'; x.beginPath(); x.moveTo(c - 9, 20); x.lineTo(c - 9, 46); x.quadraticCurveTo(c - 9, 56, c, 56); x.quadraticCurveTo(c + 9, 56, c + 9, 46); x.lineTo(c + 9, 20); x.moveTo(c, 56); x.lineTo(c, 74); x.stroke();
    x.strokeStyle = KV.needle; x.lineWidth = 2; x.beginPath(); for (let i = 0; i <= 40; i++) { const px = c - 30 + i * 1.5, py = c + 2 + Math.sin(i / 40 * 4 * P) * 6; if (i) x.lineTo(px, py); else x.moveTo(px, py); } x.stroke();
  },
};
// A pilot lamp: a faceted jewel lens glowing in a chrome bezel.
function jewel(x, w, col) {
  const c = w / 2;
  chrome(x, () => { x.beginPath(); x.arc(c, c, 26, 0, 2 * P); }, [0, 0, w, w]);
  x.save(); x.shadowColor = col; x.shadowBlur = 14; const g = x.createRadialGradient(c - 5, c - 6, 1, c, c, 20); g.addColorStop(0, '#ffffff'); g.addColorStop(0.35, col); g.addColorStop(1, hexA(col, 0.55));
  x.fillStyle = g; x.beginPath(); x.arc(c, c, 19, 0, 2 * P); x.fill(); x.restore();
  x.strokeStyle = 'rgba(255,255,255,0.35)'; x.lineWidth = 0.8; for (let i = 0; i < 6; i++) { const a = i * P / 3; x.beginPath(); x.moveTo(c, c); x.lineTo(c + Math.cos(a) * 19, c + Math.sin(a) * 19); x.stroke(); }
}

// ---- icons: engraved instrument placards ------------------------------------------------------------------------
function icon(name) {
  return F.paint(48, 48, (x) => {
    const R = RNG('icon.' + name);
    chrome(x, rrect(x, 1, 1, 46, 46, 8), [0, 0, 48, 48]); enamel(x, rrect(x, 3.5, 3.5, 41, 41, 6), [3, 3, 45, 45], R);
    x.strokeStyle = KV.ink; x.fillStyle = KV.ink; x.lineWidth = 2.2; x.lineCap = 'round'; x.lineJoin = 'round';
    const L = (pts) => { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); x.stroke(); };
    ({
      helm: () => { x.beginPath(); x.arc(24, 28, 13, P * 1.1, P * 1.9); x.stroke(); L([[24, 28], [31, 17]]); x.beginPath(); x.arc(24, 28, 2.4, 0, 2 * P); x.fill(); },
      fire: () => L([[27, 8], [17, 25], [26, 25], [20, 40]]),
      shields: () => { x.beginPath(); x.arc(24, 22, 11, P, 0); x.stroke(); L([[13, 22], [13, 36]]); L([[35, 22], [35, 36]]); L([[10, 36], [16, 36]]); L([[32, 36], [38, 36]]); },
      tractor: () => { x.beginPath(); x.arc(24, 20, 9, P, 0); x.stroke(); L([[15, 20], [15, 28]]); L([[33, 20], [33, 28]]); x.lineWidth = 1.2; for (const r of [6, 10]) { x.beginPath(); x.arc(24, 34, r, 0.2 * P, 0.8 * P); x.stroke(); } },
      transport: () => { x.beginPath(); x.ellipse(24, 20, 9, 12, 0, P, 0); x.stroke(); L([[15, 20], [15, 34], [33, 34], [33, 20]]); L([[20, 38], [20, 42]]); L([[28, 38], [28, 42]]); L([[24, 16], [24, 28]]); },
      science: () => { L([[20, 8], [20, 20], [11, 38], [37, 38], [28, 20], [28, 8]]); L([[17, 8], [31, 8]]); L([[15, 30], [33, 30]]); },
      comms: () => { x.beginPath(); for (let i = 0; i <= 32; i++) { const px = 8 + i, py = 24 + Math.sin(i / 32 * 4 * P) * 9; if (i) x.lineTo(px, py); else x.moveTo(px, py); } x.stroke(); },
      repair: () => { L([[14, 34], [30, 18]]); x.beginPath(); x.arc(33, 15, 6, 0.9 * P, 2.6 * P); x.stroke(); x.beginPath(); x.arc(13, 35, 3, 0, 2 * P); x.stroke(); },
      map: () => { L([[8, 34], [40, 34]]); for (let i = 0; i <= 8; i++) L([[8 + i * 4, 34], [8 + i * 4, i % 2 ? 30 : 26]]); L([[10, 14], [38, 14]]); L([[10, 10], [10, 18]]); L([[38, 10], [38, 18]]); },
      settings: () => { x.beginPath(); x.arc(24, 24, 10, 0, 2 * P); x.stroke(); L([[24, 24], [24, 15]]); x.lineWidth = 1.4; for (let i = 0; i < 9; i++) { const a = P * 0.75 + i * P * 0.1875; L([[24 + Math.cos(a) * 14, 24 + Math.sin(a) * 14], [24 + Math.cos(a) * 18, 24 + Math.sin(a) * 18]]); } },
    })[name]();
  });
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('kelvane', {
  name: 'Kelvane Institute', kind: 'Lab instruments: cream enamel, chrome, amber displays, phosphor scopes', bg: '#24211c', sampleBg: KV.cream,
  css: { '--s-ink': KV.ink, '--s-dim': KV.dim, '--s-num': '#7a1e0e', '--s-body': '"Courier Prime", monospace', '--btn-ink': KV.ink, '--btn-shadow': '0 1px 0 rgba(255,255,255,0.6)', '--btn-justify': 'center',
    '--btn-font': '700 15px/1 "Courier Prime", monospace', '--btn-track': '0.1em', '--btn-case': 'uppercase', '--btn-motion': 'none', '--btn-press': 'translateY(1px)',
    '--tab-font': '700 12px/1 "Courier Prime", monospace', '--tab-ink': KV.dim, '--tab-ink-on': KV.ink, '--title-font': '700 16px/1 "Courier Prime", monospace', '--title-ink': KV.ink,
    '--lbl-font': '700 16px/1 "Courier Prime", monospace', '--num-font': '400 28px/1 "Share Tech Mono", monospace', '--bt-rep': 'round', '--bf-rep': 'round' },
  accent: KV.phos, warn: KV.needle,
  title: (t, m) => { t.style.cssText = `left:${30 - m[3]}px;top:${-m[0] + 5}px;letter-spacing:0.16em;text-shadow:0 1px 0 rgba(255,255,255,0.7);`; },
  plateText: { left: 54, top: 12, width: 290, height: 32, font: '700 17px/1 "Courier Prime", monospace', color: KV.ink, sub: 'RV PATIENCE · DR. ORREL' },
  helmKeys: 'ring', helmR: 92, shield: [KV.phos, KV.amber, '#ff4a3a'],
  fonts: ['700 20px "Courier Prime"', '400 20px "Share Tech Mono"'], view: { title: '400 26px "Share Tech Mono"', body: '400 24px "Share Tech Mono"', upper: true, ink: '#8affb0', dim: KV.amber },
  build: () => F.fromPainters(nine, hero, icon, false),
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
