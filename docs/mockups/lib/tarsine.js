// Fleet Command mockups: the Tarsine Remnant skin (openspec/changes/console-skins/design.md, "The Remnant on the bible, grime
// kept"; openspec/changes/tarsine-battle-console), shared by the console skins page (docs/mockups/skins/tarsine.js
// registers it there) and the battle console, where it is the skin of the Remnant ship you command
// (docs/mockups/battle-console-tarsine.html, written from battle-console.html by tools/mockups/skin_page.py). One
// implementation of the Remnant's look (CLAUDE.md section 6.1): its materials (corroded iron, rust streaks, rivets), its
// palette, its carved glyph icons, its live readouts and, for the battle console, its kit loaded from the Blender render
// (assets/ui/tarsine/manifest.json, tools/blender/build_ui_tarsine.py from tools/mockups/tarsine_shapes.py) with the few
// pieces only the console draws composed here from that render: the square key is the helm pad's own arched tablet, the
// wide key the rendered tablet key, each with the same caked dirt in every state and only the backlight changing (the
// Remnant's rule, design.md version 17). The column, its ground, the title lintel and the allocation cells are rendered
// with the rest of the kit (version 2, owner, 2026-11-08: "tarsine background is terrible, it should be like a metal
// texture, but here it just looks like sludge stretched out").
// It exposes the same fields as docs/mockups/lib/concord.js and lib/kelvane.js, so the battle console swaps one for another
// and changes no rule and no layout. Needs docs/mockups/lib/kitdraw.js inlined first. Our own design: no other game's
// names, shapes, glyphs or art.
//
// Where it comes from (design.md; owner, 2026-10-30: "a powerful ancient race. rusty, with back lit buttons clogged with
// dirt. their ships havent been upgraded in ages, rough around the edges"; 2026-10-31: "please dont remove the grimey
// industrial feel, I love that story telling so much"): a fallen empire that remembers being first and will not change,
// its machines older than their crews, patched and never replaced (the provisional account, survey U7). Nothing here comes
// from what the Tarsine look like (CLAUDE.md section 1).
window.FCTarsine = (function () {
'use strict';
const { rng, hash, paint, P } = window.FCDraw;
// The palette and what it stands for (design.md): ages of use (soot), decline they will not admit (rust), time
// (verdigris), old majesty (old gold), the fire still burning (backlight), anger slow to wake (ember).
const TA = { soot: '#1c1814', iron: '#3a332c', ironHi: '#6a5e50', rust: '#7a3b1c', rustHi: '#b0602e', verd: '#4f6b5a', gold: '#a88a46', goldHi: '#d8bc6e',
  light: '#d6c76a', ember: '#d2482a', bone: '#e8dcc0', dim: '#a8987a', glass: '#0e0c0a' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
const RNG = (k) => rng(hash('tarsine.' + k));
// Relative luminance of a #rrggbb colour (WCAG), for picking the ink that reads on a fill.
function lum(hex) {
  if (typeof hex !== 'string' || hex[0] !== '#' || hex.length < 7) return 0;
  const c = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
}

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

// ---- The battle console's colour roles -------------------------------------------------------------------------
// The same role names as the Concord's (lib/concord.js), so the page's rules pick a role, never a colour. Readouts glow
// pale through the old glass (Sky); the backlight, the fire still burning, is whole and engaged (Accord); a tired amber
// is damaged (Caution) and ember down and alarms (Alert), the skins page's shield ramp; labels are worn gilt (Lilac),
// prose bone, titles old gold; a key at rest is dark bronze (Signal).
const CC = {
  deep: '#2a241e', signal: '#5a4a36', sky: '#efe2a0', accord: TA.light, lilac: TA.dim, caution: '#d08a3a', alert: '#e8553a', text: TA.bone,
  ink: '#1e160c', inkDis: '#5a4e40', night: '#16120e', ghost: '#6a5e50', void: '#0e0b08', slate: '#3a332c', mute: '#2a241e', ver: '#8a7c62',
  char: '#3a0b09', plate: '#14100c', bg: '#110e0b', title: TA.goldHi,
};
// The ink that reads on a fill: dark on the lit glass, bone on iron and dark glass.
const ink = (fill) => (lum(fill) > 0.2 ? CC.ink : TA.bone);
// Ink on the rendered tablet keys and lintel tabs: dark carved letters on the lit tablets in every state (their glass is
// lit through the dirt at rest too); dark on the lit lintel, worn gilt on a dark one.
const keyInk = { idle: CC.ink, hover: CC.ink, pressed: CC.ink, tabOn: CC.ink, tabOff: TA.dim };
// Type: "Cinzel" carved capitals for headings and the name plate, as the skins page's keys and titles, and "Alegreya Sans
// SC" for everything else (the skin's pair). The page asks for Antonio sizes. Alegreya Sans SC runs 1.2 times as wide at
// the same size (measured on the console's own labels, 2026-11-07), so it is set at 0.86 of it; weight 700 at 22 px and up
// is a heading: Cinzel, 1.55 times as wide, at 0.7; rule T (the keys' and tabs' fit) shrinks what still runs long.
function font(spec) {
  const m = /^(\d+) ([\d.]+)px Antonio$/.exec(spec); if (!m) return spec;
  const w = +m[1], s = +m[2];
  if (w >= 700 && s >= 22) return `700 ${Math.round(s * 0.7 * 10) / 10}px Cinzel`;
  return `${w >= 700 ? 700 : 500} ${Math.round(s * 0.86 * 10) / 10}px "Alegreya Sans SC"`;
}
const fonts = ['700 20px "Cinzel"', '700 20px "Alegreya Sans SC"', '500 20px "Alegreya Sans SC"'];

// ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ---------------------------------------
// Each a picture of the job as a glyph carved into old metal and worn: straight cut strokes with square ends, a dark cut
// under each stroke and a glint along its upper edge, a few places worn away by time. One ink; on a 48 x 48 grid with 4 px
// clear, read at 20 px and 48 px. On a lit tablet (a dark ink) the cut is a faint glint instead of a shadow.
function icon(name, col = TA.goldHi) {
  return paint(48, 48, (x) => {
    const R = RNG('icon.' + name), dark = lum(col) < 0.2;
    const layers = dark ? [['rgba(255,240,200,0.35)', 0.7, 0.7, 4.4], [col, 0, 0, 2.8]] : [['rgba(0,0,0,0.7)', 0, 0, 4.6], [col, 0, 0, 2.8], ['rgba(255,240,200,0.35)', -0.6, -0.6, 1]];
    const S = (pts, lw = 1) => { for (const [c, dx, dy, w] of layers) { x.strokeStyle = c; x.lineWidth = w * lw; x.lineCap = 'square'; x.lineJoin = 'miter'; x.beginPath(); x.moveTo(pts[0][0] + dx, pts[0][1] + dy); for (const p of pts.slice(1)) x.lineTo(p[0] + dx, p[1] + dy); x.stroke(); } };
    const F = (pts) => { for (const [c, dx, dy, w] of layers.slice(0, 2)) { x.fillStyle = c; x.strokeStyle = c; x.lineWidth = w - 2.8 + 0.5; x.beginPath(); x.moveTo(pts[0][0] + dx, pts[0][1] + dy); for (const p of pts.slice(1)) x.lineTo(p[0] + dx, p[1] + dy); x.closePath(); x.fill(); if (w > 3) x.stroke(); } };
    const ring = (cx, cy, r, n = 16) => Array.from({ length: n + 1 }, (_, i) => [cx + Math.cos(i * 2 * P / n) * r, cy + Math.sin(i * 2 * P / n) * r]);
    const arc = (cx, cy, rx, ry, a0, a1, n = 10) => Array.from({ length: n + 1 }, (_, i) => { const a = a0 + (a1 - a0) * i / n; return [cx + Math.cos(a) * rx, cy + Math.sin(a) * ry]; });
    ({
      // the helm: a carved heading dial, its pointer on the course
      helm: () => { S(ring(24, 24, 17)); S([[24, 24], [24, 9]], 1.3); S([[18, 15], [24, 8], [30, 15]]); S([[24, 37], [24, 41]]); S([[7, 24], [11, 24]]); S([[37, 24], [41, 24]]); },
      // power: a bolt cut in a square frame
      power: () => { S([[8, 8], [40, 8], [40, 40], [8, 40], [8, 8]]); S([[27, 12], [18, 25], [30, 25], [21, 37]], 1.2); },
      // tactical: a battery: a stepped base, a housing, its barrel up to the right
      tactical: () => { S([[6, 41], [42, 41]]); S([[11, 34], [35, 34], [31, 26], [15, 26], [11, 34]]); S([[26, 28], [40, 12]], 1.4); },
      // point defence: a shield with a ranging cross
      pd: () => { S([[8, 7], [40, 7], [40, 25], [24, 41], [8, 25], [8, 7]]); S([[24, 13], [24, 31]]); S([[16, 21], [32, 21]]); },
      // science: a lens over a trace, the reading under study
      science: () => { S(ring(20, 20, 12)); S([[29, 29], [41, 41]], 1.6); S([[11, 22], [15, 16], [20, 24], [25, 16], [29, 20]], 0.8); },
      // comms: a mast, two calls going out
      comms: () => { S([[12, 42], [12, 8]]); S([[6, 42], [18, 42]]); S([[12, 10], [26, 16], [12, 22]]); S(arc(20, 24, 12, 12, -0.7, 0.7, 6)); S(arc(20, 24, 19, 19, -0.6, 0.6, 6)); },
      // repair: a spanner, its jaw open to the upper right
      repair: () => { S([[9, 39], [27, 21]], 1.6); S([[24, 14], [28, 10], [36, 10], [38, 12], [33, 17], [35, 19], [40, 14], [40, 20], [34, 26], [28, 26], [24, 22], [24, 14]], 0.9); },
      // transport: a figure, its lower half breaking into cut lines as it beams
      transport: () => { S(ring(24, 10, 4.5, 8)); S([[16, 30], [18, 19], [30, 19], [32, 30]]); for (const [y, w] of [[34, 9], [38, 6.5], [42, 4]]) S([[24 - w, y], [24 + w, y]]); },
      // fleet: three hulls in formation, the lead ahead
      fleet: () => { const sh = (cx, cy, k) => S([[cx, cy - 10 * k], [cx + 6 * k, cy + 8 * k], [cx - 6 * k, cy + 8 * k], [cx, cy - 10 * k]]); sh(24, 13, 1); sh(11, 33, 0.85); sh(37, 33, 0.85); },
      // the ship from above: a long dreadnought, stepped and scarred
      ship: () => { S([[20, 4], [28, 4], [31, 14], [31, 22], [38, 26], [38, 36], [31, 39], [29, 44], [19, 44], [17, 39], [10, 36], [10, 26], [17, 22], [17, 14], [20, 4]]); S([[19, 22], [29, 22]]); S([[19, 32], [29, 32]]); },
      // shields: six cut arcs round a hull
      shields: () => { for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; S(arc(24, 24, 18, 18, a - 0.42, a + 0.42, 4)); } S([[24, 17], [28, 28], [20, 28], [24, 17]]); },
      // sensors: a scope, its sweep and two contacts
      sensors: () => { S(ring(24, 24, 18)); S([[24, 24], [36, 12]]); S([[30, 30], [32, 32]], 1.6); S([[14, 16], [16, 18]], 1.6); },
      // tractor: an emitter, the beam widening to the hull it holds
      tractor: () => { S([[4, 18], [12, 18], [12, 30], [4, 30], [4, 18]]); S([[12, 21], [30, 13]]); S([[12, 27], [30, 35]]); S([[32, 14], [44, 14], [44, 34], [32, 34], [32, 14]]); },
      // craft: a small swept craft
      craft: () => { S([[24, 5], [29, 21], [43, 30], [29, 32], [24, 43], [19, 32], [5, 30], [19, 21], [24, 5]]); },
      // weapons: a beam from an emitter, ending in a strike
      weapons: () => { S([[6, 36], [12, 36], [12, 42], [6, 42], [6, 36]]); S([[12, 36], [30, 18]], 1.2); for (let i = 0; i < 6; i++) { const a = i * P / 3; S([[35 + Math.cos(a) * 4, 13 + Math.sin(a) * 4], [35 + Math.cos(a) * 9, 13 + Math.sin(a) * 9]], 0.8); } },
      // warp: a chevron trailing speed lines
      warp: () => { S([[22, 10], [37, 24], [22, 38]], 1.5); S([[5, 16], [16, 16]]); S([[3, 24], [20, 24]]); S([[5, 32], [16, 32]]); },
      // engines: a nozzle and the exhaust out of it
      engines: () => { S([[14, 5], [34, 5], [38, 20], [10, 20], [14, 5]]); S([[16, 27], [32, 27]]); S([[19, 34], [29, 34]]); S([[22, 41], [26, 41]]); },
      // reactor: a core in two crossed orbits
      reactor: () => { S(ring(24, 24, 5, 8)); for (const r of [P / 4, -P / 4]) S(arc(0, 0, 19, 8, 0, 2 * P, 20).map(([a, b]) => [24 + a * Math.cos(r) - b * Math.sin(r), 24 + a * Math.sin(r) + b * Math.cos(r)]), 0.8); },
      // map: a cut course to a mark
      map: () => { for (const [a, b] of [[[7, 41], [10, 35]], [[12, 30], [14, 25]], [[17, 21], [22, 18]]]) S([a, b]); S([[29, 6], [41, 18]], 1.2); S([[41, 6], [29, 18]], 1.2); },
      pause: () => { S([[17, 10], [17, 38]], 2); S([[31, 10], [31, 38]], 2); },
      menu: () => { for (const y of [13, 24, 35]) S([[9, y], [39, y]], 1.4); },
      // fire: four brackets closing on a cut mark
      fire: () => { for (const [a, b, c] of [[[8, 17], [8, 8], [17, 8]], [[31, 8], [40, 8], [40, 17]], [[40, 31], [40, 40], [31, 40]], [[17, 40], [8, 40], [8, 31]]]) S([a, b, c], 1.1); S([[21, 21], [27, 21], [27, 27], [21, 27], [21, 21]], 1.2); },
      cargo: () => { for (const [cx, cy] of [[15, 15], [33, 15], [15, 33], [33, 33]]) S([[cx - 7, cy - 7], [cx + 7, cy - 7], [cx + 7, cy + 7], [cx - 7, cy + 7], [cx - 7, cy - 7]], 0.9); },
      // settings: a cog cut with eight teeth round a bore
      settings: () => { const o = []; for (let i = 0; i <= 16; i++) { const a = i * 2 * P / 16, r = i % 2 ? 13 : 19; o.push([24 + Math.cos(a) * r, 24 + Math.sin(a) * r]); } S(o, 0.9); S(ring(24, 24, 5, 8)); },
    })[name]();
    // time wore parts of it away
    x.globalCompositeOperation = 'destination-out'; for (let i = 0; i < 4; i++) { x.fillStyle = `rgba(0,0,0,${0.3 + R() * 0.4})`; x.beginPath(); x.arc(R() * 48, R() * 48, 1.5 + R() * 3, 0, 2 * P); x.fill(); }
    x.globalCompositeOperation = 'source-over';
  });
}
const ICON_NAMES = ['helm', 'power', 'tactical', 'pd', 'science', 'comms', 'repair', 'transport', 'fleet', 'ship', 'shields', 'sensors', 'tractor', 'craft', 'weapons', 'warp', 'engines', 'reactor', 'map', 'pause', 'menu', 'fire', 'cargo', 'settings'];

// ---- Live readouts (the console skins page and the battle console) ---------------------------------------------
const live = {
  // A carved arrow, dark in the lit tablet.
  arrow: (x) => { x.fillStyle = '#1e160c'; x.beginPath(); x.moveTo(12, 0); x.lineTo(0, -10); x.lineTo(0, -4); x.lineTo(-11, -4); x.lineTo(-11, 4); x.lineTo(0, 4); x.lineTo(0, 10); x.closePath(); x.fill(); },
  helmCentre: (x, c) => { x.fillStyle = '#1e160c'; x.font = '700 18px Cinzel'; x.textAlign = 'center'; x.fillText('HALT', c, c + 6); },
  // A long scarred dreadnought in worn gold lines; shields as dim broken arcs.
  ssd: (x, cx, cy, sh, col) => {
    x.save();
    for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.strokeStyle = col(sh[i]); x.lineWidth = 6; x.setLineDash([10, 4, 3, 4]); x.beginPath(); x.arc(cx, cy, 100, a - P / 6 + 0.08, a + P / 6 - 0.08); x.stroke(); }
    x.setLineDash([]); x.strokeStyle = TA.goldHi; x.lineWidth = 2.2;
    x.beginPath(); x.moveTo(cx - 12, cy - 78); x.lineTo(cx + 12, cy - 78); x.lineTo(cx + 22, cy - 50); x.lineTo(cx + 22, cy - 10); x.lineTo(cx + 40, cy); x.lineTo(cx + 40, cy + 40); x.lineTo(cx + 22, cy + 50); x.lineTo(cx + 18, cy + 76); x.lineTo(cx - 18, cy + 76); x.lineTo(cx - 22, cy + 50); x.lineTo(cx - 40, cy + 40); x.lineTo(cx - 40, cy); x.lineTo(cx - 22, cy - 10); x.lineTo(cx - 22, cy - 50); x.closePath(); x.stroke();
    for (const y of [-40, -10, 22, 50]) { x.beginPath(); x.moveTo(cx - 20, cy + y); x.lineTo(cx + 20, cy + y); x.stroke(); }
    x.strokeStyle = 'rgba(210,72,42,0.7)'; x.lineWidth = 1.2; x.beginPath(); x.moveTo(cx + 26, cy + 4); x.lineTo(cx + 10, cy + 18); x.lineTo(cx + 16, cy + 30); x.stroke();
    x.fillStyle = TA.ember; for (const [mx, my] of [[cx, cy - 64], [cx - 32, cy + 20], [cx + 32, cy + 20], [cx, cy + 64]]) { x.fillRect(mx - 4, my - 4, 8, 8); }
    x.restore();
  },
  // An old needle behind cracked glass, contacts as ember specks.
  dial: (x, c, cts) => {
    x.strokeStyle = TA.ember; x.lineWidth = 2.4; x.beginPath(); x.moveTo(c, c); x.lineTo(c + Math.cos(-0.9) * 34, c + Math.sin(-0.9) * 34); x.stroke();
    x.fillStyle = '#2a221a'; x.beginPath(); x.arc(c, c, 5, 0, 2 * P); x.fill();
    for (const [dx, dy] of cts) { x.fillStyle = hexA(TA.ember, 0.85); x.fillRect(c + dx * 30 - 3, c + dy * 30 - 3, 6, 6); }
  },
  // Heavy iron corner brackets with a rivet each (the same rust every frame: one seed).
  bracket: (x, bx, by, bw, bh) => {
    const R = RNG('bracket');
    for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
      x.save(); x.translate(px, py); x.scale(sx, sy);
      iron(x, () => { x.beginPath(); x.moveTo(0, 0); x.lineTo(46, 0); x.lineTo(46, 10); x.lineTo(10, 10); x.lineTo(10, 46); x.lineTo(0, 46); x.closePath(); }, [0, 0, 46, 46], R, { top: '#6a5a48' });
      rivet(x, 5, 5, 3, R); x.restore();
    }
  },
};

// ---- The kit ------------------------------------------------------------------------------------------------------
// Every piece of the skins page's kit is rendered in Blender and comes from the inlined manifest; load() decodes them once.
// Most of the battle console's own pieces are rendered too (console.column, console.backing, plate.title, cell.*: the
// manifest's pieces, read as they come); the keys are composed here from the render:
//  - key.*: the helm pad's top-left tablet in its iron (helm.pad.png, 148 x 148 image px from (54, 50)); its caked glass
//    keeps the same dirt in every state and only the backlight behind it changes: dim at rest, half lit on hover, full
//    gold engaged and whole, a tired amber damaged, ember down, all but dark off;
//  - keyw.*: the rendered tablet key (button.idle) lit the same way; bar.cell.ok: the lit window's light in the same gold;
//  - console.column: one plate of worked iron with the three docked panels formed into it as framed wells, their floors
//    the panel faces, rendered whole at 480 x 1080 (the Krai's standard), so the column's panels draw no frame of their
//    own and nothing in them is stretched; console.backing: the same iron, plain, riveted at its edges, behind an
//    expanded panel (CLAUDE.md section 7: no black behind a metallic skin's panels).
const MANIFEST = 'assets/ui/tarsine/manifest.json';
const IMG = {}, kit = {}, hero = {}, heroSize = {}, cells = {}, barCells = {};
// the glass of the tablet (crop px) and of the tablet key (button.idle, reference px of 140 x 52): x0, y0, x1, y1, top and
// bottom corner radii
const GLASS = [15, 15, 133, 138, 16, 8], WGLASS = [15, 11, 125, 40, 8, 3];
// the light behind the dirt, multiplied over the lit glass: the render's own lit glass is 'on'
const BACK = { idle: '#5a4c36', hover: '#b8a67a', on: null, ok: null, warn: '#ffa860', alert: '#ff6a4a', off: '#2e281e' };
function glass(x, g, kx, ky) { const [a, b, c, d, rt, rb] = g; x.beginPath(); x.roundRect(a * kx, b * ky, (c - a) * kx, (d - b) * ky, [rt * kx, rt * kx, rb * kx, rb * kx]); }
function backlit(x, g, kx, ky, state) { if (!BACK[state]) return; x.save(); glass(x, g, kx, ky); x.clip(); x.globalCompositeOperation = 'multiply'; x.fillStyle = BACK[state]; x.fillRect(0, 0, 1000, 1000); x.restore(); }
function keyPaint(state) { return (x, w, h) => { x.drawImage(IMG['helm.pad'], 54, 50, 148, 148, 0, 0, w, h); backlit(x, GLASS, w / 148, h / 148, state); }; }
function keywPaint(state) { return (x, w, h) => { x.drawImage(IMG['button.idle'], 0, 0, w, h); backlit(x, WGLASS, w / 140, h / 52, state); }; }
let loading = null;
function load() {
  if (loading) return loading;
  const n = document.querySelector(`script[data-src="${MANIFEST}"]`);
  if (!n) return (loading = Promise.reject(new Error(`the Tarsine kit (${MANIFEST}) is not inlined in this page`)));
  const d = JSON.parse(n.textContent);
  loading = Promise.all(Object.entries(d.pieces).map(([id, p]) => new Promise((ok, bad) => { const im = new Image(); im.onload = () => { IMG[id] = im; ok(); }; im.onerror = () => bad(new Error(`Tarsine piece ${id} failed to decode`)); im.src = p.data; }))).then(() => {
    for (const [id, p] of Object.entries(d.pieces)) {
      const draw = (x, w, h) => x.drawImage(IMG[id], 0, 0, w, h);
      if (p.kind === 'nine') kit[id] = { size: p.size, m: p.margins, inset: p.inset || [3, 3], draw };
      else if (p.kind === 'hero') { hero[id] = draw; heroSize[id] = p.size; }
      else if (p.kind === 'cell') barCells[id] = { size: p.size, gap: p.gap ?? 2, draw };
    }
    // the ok symbol: the lit window as it is (whole is the backlight itself), so the whole bar reads as the fire burning
    if (barCells['bar.cell.on']) barCells['bar.cell.ok'] = barCells['bar.cell.on'];
    for (const s of Object.keys(BACK)) { hero['key.' + s] = keyPaint(s); heroSize['key.' + s] = [48, 48]; kit['keyw.' + s] = { size: [144, 48], m: [0, 30, 0, 30], draw: keywPaint(s) }; }
    // the name plate with its lamps turned low: the same render and the same caked dirt, the window's light dimmed so the
    // captain's name reads in pale carved letters (the skins page sets pale letters outlined dark on the lit window; the
    // console draws its text plain)
    hero.nameplate = (x, w, h) => { x.drawImage(IMG.nameplate, 0, 0, w, h); x.save(); x.beginPath(); x.roundRect(36 * w / 460, 10 * h / 56, 390 * w / 460, 36 * h / 56, 4); x.clip();
      x.globalCompositeOperation = 'multiply'; x.fillStyle = '#4a3c28'; x.fillRect(0, 0, w, h); x.restore(); };
    kit['bar.fill.accord'] = kit['bar.fill'];
  });
  return loading;
}
// The Remnant's crest wherever the console draws one (a portrait, the holder of a ship): the rendered crest, r its radius.
function emblem(G, cx, cy, r) { if (IMG.crest) G.drawImage(IMG.crest, cx - r, cy - r, 2 * r, 2 * r); }
// Drawn keys and knobs (the slider handles, the expand key, the tug bar): the arched tablet, round above and square at its
// foot; a knob is a domed rivet head.
function rr(G, X, Y, W, H, r) { G.beginPath(); if (!(W > 0 && H > 0)) { G.rect(X, Y, W, H); return; } const q = Math.min(Array.isArray(r) ? Math.max(...r) : (+r || 0), W / 2, H / 2); G.roundRect(X, Y, W, H, [q, q, q * 0.2, q * 0.2]); }
function knob(G, cx, cy, r) { G.beginPath(); G.arc(cx, cy, r, 0, 2 * P); }

const api = {
  id: 'tarsine', name: 'Tarsine Remnant', TA, CC, kit, hero, heroSize, cells, barCells, live, icon, ICON_NAMES, emblem, ink, keyInk, font, fonts, load, hexA, rr, knob,
  iron, streak, rivet, RNG,
  family: '"Alegreya Sans SC", sans-serif',
  // The content inside a panel: inside the well's worn gold rim, clear of the stepped capitals at its corners.
  pad: [30, 26, 26, 26],
  // The name plate's live field: the window (36-426 x 10-46), its lamps turned low, 10 px clear of its left end, pale carved
  // letters, clear of the Fleet key at 370.
  plate: { x: 46, y: 35, max: 314, font: '700 18px Cinzel', col: TA.bone },
  // The battle's helm ring under the ship, in the backlight's gold.
  ring: ['rgba(214,199,106,0)', 'rgba(214,199,106,0.13)', 'rgba(232,220,150,0.85)'],
  // Names for the player's side where the page shows it as the Remnant: ships named for what the empire remembers,
  // captains by age (the skins page's ANKHAR, ELDEST VOR). Placeholders until the owner sets the Remnant's culture
  // (CLAUDE.md section 1; openspec/changes/tarsine-battle-console, open question T1).
  player: { faction: 'Tarsine Remnant', call: 'Eldest', trim: 0xd6c76a, hull: 0x6a5e50,
    ships: { resolute: ['Ankhar', 'Eldest Vor'], tallis: ['Oathkeeper', 'Elder Sabeth'], meridian: ['Long Memory', 'Eldest Corvane'] },
    ranks: ['Younger', 'Keeper', 'Elder', 'Eldest'],
    officers: { o1: 'Ithemar Sol', o2: 'Sabeth Orn', o3: 'Callon Vey', o4: 'Aurest Mal', o5: 'Teodras Ilm', o6: 'Ysmene Kar', o7: 'Oradel Fenn',
      o8: 'Haskar Ume', o9: 'Merova Tal', o10: 'Ondric Sael', o11: 'Velisane Ro', o12: 'Bastre Hollan', o13: 'Quorin Ade' } },
  link: TA.goldHi,
  // Labels centred on the tablet key's glass (15-125 of 140 at 52 px) and in a lintel tab's slot of glass.
  keyCenter: true, keyField: [17, 17], tabField: [10, 10],
  // A hover name on the half-lit tablet, in dark carved letters.
  tip: ['button.hover', 'hover'],
  // Icon tabs (the station and ship rails) on the square tablet, the icon in its glass.
  iconKeys: true,
  // Panel tab icons at rest in bone on the dim glass, apart from the functional icons' gold; glyphs on dim glass in the
  // backlight's gold.
  tabIcon: TA.bone, glyphOnDark: TA.light, mark: { hover: '#fff0b0', ready: TA.light, count: TA.bone },
  // The strips (strip.top and strip.bottom, 72 px beams) at their full height; the content, laid out for 56 px, moved down
  // into the beam's channel of grimy glass (rows 13-59).
  stripTop: { h: 72, dy: 8 }, stripBottom: { h: 72, dy: 8 },
  titleDy: 2,
};
window.FCSkins = window.FCSkins || {}; window.FCSkins.tarsine = api;
return api;
})();
