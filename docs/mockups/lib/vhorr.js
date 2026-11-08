// Fleet Command mockups: the Vhorr Brood skin (openspec/changes/console-skins/design.md, "Vhorr Brood";
// openspec/changes/vhorr-battle-console), shared by the console skins page (docs/mockups/skins/vhorr.js registers it
// there with its own slot overlay) and the battle console, where it is the skin of every Brood ship you command (the
// console follows the ship you are on; openspec/changes/fleet-skins). One implementation of what both draw (CLAUDE.md
// section 6.1): the palette, the icons, the live arrow and target bracket and, for the battle console, its kit: the
// battle console's own slots sculpted at their exact sizes by the parametric sculptors on the Organic Panel Bible
// (tools/blender/organic_components.py, run by tools/blender/build_ui_vhorr_organic.py over the blockout that
// tools/mockups/vhorr_battle_blockout.py writes from the console's rectangles; scripts/render-vhorr-battle.sh).
// It exposes the same fields as docs/mockups/lib/concord.js and lib/kelvane.js, so the battle console swaps one for
// another and changes no rule and no layout. Needs docs/mockups/lib/kitdraw.js inlined first. Our own design: no other
// game's or film's names, shapes, glyphs or art.
//
// Where it comes from (design.md; owner, 2026-10-30: "an organic alien race, lots of leather shining through light,
// slimy carapace, tendrils"; 2026-10-31: "the alien hive mind aesthetic"; the Organic Panel Bible,
// docs/design/organic-bible.html). Its tradition is the brood as one body: its consoles are grown, not built, bone then
// muscle then leather stretched over both, holding hard crisp instruments (black glass, dark keys with hot rims). Nothing
// here comes from what the Vhorr look like (CLAUDE.md section 1): the grown body is what the brood makes and how it
// thinks of a ship, a living thing, not a picture of its makers.
window.FCVhorr = (function () {
'use strict';
const { paint, P } = window.FCDraw;
const VH = { chitin: '#2b1d2a', chitinHi: '#6a4a68', hide: '#5a3424', hideDark: '#2e1814', flesh: '#c4566a', bile: '#a6e34a', bileHot: '#e4ff9a',
  slime: '#f4f0e0', blood: '#e0342a', pit: '#120a10', ink: '#f3e6d0', dim: '#c9a48c', amber: '#f0b43c',
  readout: '#b8f27a', keyInk: '#ffd9a0', leather: '#33110f', bone: '#e2d6bb', glass: '#050405', hero: '#b35cff', will: '#c99bff', wound: '#ff5040' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
// Relative luminance of a #rrggbb colour (WCAG), for picking the ink that reads on a fill.
function lum(hex) {
  if (typeof hex !== 'string' || hex[0] !== '#' || hex.length < 7) return 0;
  const c = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
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

// ---- The battle console's colour roles -------------------------------------------------------------------------
// The same role names as the Concord's (lib/concord.js), so the page's rules pick a role, never a colour. The brood's
// readouts glow on its black glass (the bible: crisp instruments in a grown body): the green of life through hide for a
// reading (Sky), a warm hide print for labels (Lilac), bone-white prose and titles, the heart's violet for whole and
// engaged (Accord: the brood's will, kept clear of the reading's green), amber for damaged, blood for down and alarms
// (design.md: "a wound"); the dark roles are the body's own.
const CC = {
  deep: '#3a1c1a', signal: '#4a2620', sky: VH.readout, accord: VH.will, lilac: VH.dim, caution: VH.amber, alert: VH.wound, text: VH.ink,
  ink: '#140806', inkDis: '#4a3430', night: '#120a0c', ghost: '#5a4440', void: '#0a0607', slate: '#2e1e1c', mute: '#221614', ver: '#9a8070',
  char: '#3a0c0a', plate: '#140a0a', bg: '#0b080a', title: VH.ink,
};
// The ink that reads on a fill: dark on a lit fill, bone on the dark body.
const ink = (fill) => (lum(fill) > 0.2 ? CC.ink : VH.ink);
// Ink on the keys and tabs: the dark metal caps carry their names in the warm key light (the skins page's --btn-ink), a
// tab not chosen in the hide print, the chosen tab in the green of life (its rim lit green); a lit square key's glyph
// in dark ink on its light.
const keyInk = { idle: VH.keyInk, hover: '#fff0d8', pressed: '#fff0d8', tabOn: VH.readout, tabOff: VH.dim };
// Type: "Barlow Condensed" for every readout and label, "Metamorphous" for titles (the skins page's pair). Barlow
// Condensed runs about as wide as Antonio, Metamorphous about 1.3 times (measured on the console's titles).
function font(spec) {
  const m = /^(\d+) ([\d.]+)px Antonio$/.exec(spec); if (!m) return spec;
  const px = +m[2];
  if (m[1] === '700' && px >= 20) return `400 ${Math.round(px * 0.8 * 10) / 10}px Metamorphous`;
  return `600 ${px}px "Barlow Condensed"`;
}
const fonts = ['400 20px Metamorphous', '600 20px "Barlow Condensed"'];

// ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ---------------------------------------
// Grown shapes: pods, talons, tendrils, spores and rings of membrane, every line swelling and tapering as flesh does.
// A solid where the thing itself is solid (a hull, a heart, a figure), spores for what is given off. One ink with a soft
// glow; on a 48 x 48 grid with 4 px clear, read at 20 px and 48 px.
function icon(name, col = VH.readout) {
  return paint(48, 48, (x) => {
    const bright = lum(col) > 0.2;
    x.strokeStyle = col; x.fillStyle = col; x.lineWidth = 2.8; x.lineJoin = 'round'; x.lineCap = 'round';
    if (bright) { x.shadowColor = hexA(col, 0.6); x.shadowBlur = 4; }
    // a limb: a stroke swelling from w0 to w1 along a quadratic curve
    const T = (x0, y0, cx, cy, x1, y1, w0, w1) => { const pts = []; for (let i = 0; i <= 12; i++) { const t = i / 12, u = 1 - t; pts.push([u * u * x0 + 2 * u * t * cx + t * t * x1, u * u * y0 + 2 * u * t * cy + t * t * y1]); } const L = pts.length, l = [], r = []; for (let i = 0; i < L; i++) { const a = pts[Math.max(0, i - 1)], b = pts[Math.min(L - 1, i + 1)], dx = b[0] - a[0], dy = b[1] - a[1], d = Math.hypot(dx, dy) || 1, w = (w0 + (w1 - w0) * i / (L - 1)) / 2; l.push([pts[i][0] - dy / d * w, pts[i][1] + dx / d * w]); r.push([pts[i][0] + dy / d * w, pts[i][1] - dx / d * w]); } x.beginPath(); x.moveTo(l[0][0], l[0][1]); for (const p of l.slice(1)) x.lineTo(p[0], p[1]); for (const p of r.reverse()) x.lineTo(p[0], p[1]); x.closePath(); x.fill(); };
    const D = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); };
    const E = (cx, cy, rx, ry, a = 0) => { x.beginPath(); x.ellipse(cx, cy, rx, ry, a, 0, 2 * P); x.fill(); };
    const C = (cx, cy, r, lw = 2.8) => { x.lineWidth = lw; x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.stroke(); };
    const A = (cx, cy, r, a0, a1, lw = 2.8) => { x.lineWidth = lw; x.beginPath(); x.arc(cx, cy, r, a0, a1); x.stroke(); };
    const hole = (fn) => { x.save(); x.globalCompositeOperation = 'destination-out'; fn(); x.restore(); };
    ({
      // the helm: a ring of membrane, a talon in it pointing the heading
      helm: () => { C(24, 24, 18); T(24, 32, 22, 20, 24, 7, 7, 1.5); },
      // power: a heart-sac swelling, three veins out of it
      power: () => { E(24, 26, 10, 12); T(24, 14, 22, 9, 20, 4, 4, 1); T(15, 30, 9, 32, 5, 38, 4, 1); T(33, 30, 39, 32, 43, 38, 4, 1); },
      // tactical: a striking talon over a pod
      tactical: () => { E(14, 34, 9, 7, -0.5); T(18, 30, 30, 22, 42, 6, 7, 1.2); },
      // point defence: a carapace shell over a spore
      pd: () => { x.beginPath(); x.arc(24, 30, 19, P, 2 * P); x.quadraticCurveTo(24, 22, 5, 30); x.fill(); D(24, 37, 4); },
      // science: a pod split open on a seed under study
      science: () => { E(24, 24, 18, 12); hole(() => E(24, 24, 12, 7)); D(24, 24, 4); },
      // comms: a spore-sac sending rings of membrane
      comms: () => { E(11, 24, 6, 8); for (const r of [12, 20, 28]) A(11, 24, r, -0.7, 0.7, 2.6); },
      // repair: two tendrils knotting a wound closed
      repair: () => { T(8, 40, 20, 18, 40, 8, 6, 1.5); T(8, 8, 28, 30, 40, 40, 6, 1.5); D(24, 24, 4.5); },
      // transport: a figure, its lower half breaking into spores
      transport: () => { D(24, 9, 5); E(24, 22, 8, 7); for (const [px, py, r] of [[18, 33, 2.6], [24, 35, 2.6], [30, 33, 2.6], [21, 41, 2], [27, 41, 2]]) D(px, py, r); },
      // fleet: three pods in a cluster, the lead ahead
      fleet: () => { E(24, 13, 6, 9); E(12, 33, 5.5, 8); E(36, 33, 5.5, 8); },
      // the ship from above: a grown hull, a heart in it, two trailing tendrils
      ship: () => { E(24, 20, 10, 16); hole(() => D(24, 18, 3.5)); T(18, 32, 14, 40, 10, 46, 4, 1); T(30, 32, 34, 40, 38, 46, 4, 1); },
      // shields: a membrane round a pod
      shields: () => { C(24, 24, 18, 3.2); E(24, 24, 6, 10); },
      // sensors: a stalked eye-pod and its ring
      sensors: () => { C(24, 24, 18, 2.4); T(24, 40, 22, 30, 24, 22, 3, 6); D(24, 19, 5); },
      // tractor: a tendril hooked round a pod, drawing it in
      tractor: () => { T(4, 26, 18, 20, 30, 18, 6, 2); E(37, 26, 7, 9); },
      // craft: a small swept spore-ship
      craft: () => { T(24, 44, 24, 24, 24, 4, 8, 2); T(24, 30, 12, 34, 4, 40, 4, 1); T(24, 30, 36, 34, 44, 40, 4, 1); },
      // weapons: a spine flung from a sac
      weapons: () => { E(11, 37, 7, 6); T(15, 33, 26, 22, 42, 6, 5, 1); },
      // warp: a pod drawn out, tendrils of speed behind it
      warp: () => { E(32, 24, 10, 8); T(20, 18, 12, 16, 3, 16, 3, 0.5); T(20, 24, 12, 24, 2, 24, 3, 0.5); T(20, 30, 12, 32, 3, 32, 3, 0.5); },
      // engines: a sac pulsing out spores behind it
      engines: () => { E(24, 14, 11, 10); D(24, 30, 3.2); D(18, 37, 2.6); D(30, 37, 2.6); D(24, 43, 2.2); },
      // reactor: a heart and two crossed rings of membrane
      reactor: () => { D(24, 24, 5.5); x.lineWidth = 2.4; for (const r of [P / 4, -P / 4]) { x.beginPath(); x.ellipse(24, 24, 19, 7.5, r, 0, 2 * P); x.stroke(); } },
      // map: a vein branching to places, a pod where it ends
      map: () => { T(6, 42, 14, 28, 24, 24, 4, 2); T(24, 24, 30, 18, 38, 8, 2.5, 1.5); D(40, 8, 4); D(12, 30, 2.4); },
      pause: () => { E(16, 24, 5, 16); E(32, 24, 5, 16); },
      menu: () => { for (const y of [12, 24, 36]) T(8, y, 24, y - 3, 40, y, 4.5, 2); },
      // fire: four talons closing on a heart
      fire: () => { for (const [a, b] of [[[6, 6], [18, 18]], [[42, 6], [30, 18]], [[42, 42], [30, 30]], [[6, 42], [18, 30]]]) T(a[0], a[1], (a[0] + b[0]) / 2, (a[1] + b[1]) / 2 + 2, b[0], b[1], 5, 1); D(24, 24, 5); },
      cargo: () => { for (const [cx, cy] of [[15, 15], [33, 15], [15, 33], [33, 33]]) E(cx, cy, 7, 6.5); },
      // settings: a ring of spines round a heart
      settings: () => { C(24, 24, 11, 3.4); for (let i = 0; i < 8; i++) { const a = i * P / 4 + P / 8; T(24 + Math.cos(a) * 13, 24 + Math.sin(a) * 13, 24 + Math.cos(a) * 16, 24 + Math.sin(a) * 16, 24 + Math.cos(a) * 20, 24 + Math.sin(a) * 20, 4, 1); } D(24, 24, 3.5); },
    })[name]();
  });
}
const ICON_NAMES = ['helm', 'power', 'tactical', 'pd', 'science', 'comms', 'repair', 'transport', 'fleet', 'ship', 'shields', 'sensors', 'tractor', 'craft', 'weapons', 'warp', 'engines', 'reactor', 'map', 'pause', 'menu', 'fire', 'cargo', 'settings'];

// ---- Live readouts the battle console shares with the skins page -------------------------------------------------
const live = {
  // A talon pointing the way.
  arrow: (x) => { x.fillStyle = VH.slime; x.strokeStyle = '#1a0a0c'; x.lineWidth = 1.4; x.beginPath(); x.moveTo(13, 0); x.quadraticCurveTo(0, -3, -9, -9); x.quadraticCurveTo(-4, 0, -9, 9); x.quadraticCurveTo(0, 3, 13, 0); x.fill(); x.stroke(); },
  helmCentre: () => {},
  // Four tendrils hooking round the target.
  bracket: (x, bx, by, bw, bh) => {
    for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
      x.save(); x.translate(px, py); x.scale(sx, sy); x.shadowColor = VH.bile; x.shadowBlur = 10;
      tendril(x, [[50, -2], [20, -2], [2, 4], [-2, 22], [6, 44]], 2, 6, VH.bile); tendril(x, curl(52, 6, 8, -P / 2, 0.7, 1), 3, 0.5, VH.bile); x.restore();
    }
  },
};

// ---- Shapes for the battle console's vector keys ----------------------------------------------------------------
// Nothing is cut: every rounded rectangle the page asks for is a pod with fully round ends, and a knob is round.
function rr(G, X, Y, W, H, r) { G.beginPath(); if (!(W > 0 && H > 0)) { G.rect(X, Y, W, H); return; } G.roundRect(X, Y, W, H, Math.min(W, H) / 2); }
function knob(G, cx, cy, r) { G.beginPath(); G.arc(cx, cy, r, 0, 2 * P); }

// ---- The kit ------------------------------------------------------------------------------------------------------
// Every piece is a sculpt at the battle console's own size (assets/ui/vhorr/organic/battle/: the blockout, its styles
// and three renders, each with its manifest), so nothing is nine-sliced at the reference size:
//   column    the column as one grown body: the tissue backing, the name plate (drawn 356 wide, the Fleet key beside
//             it), the station, ship and weapons panels each round one black screen, the radar's pocket and the crest,
//             composed into console.column, which the console draws behind its panels in place of their frames
//   strips    the top and bottom strips, each a spine round one glass channel, at 1440 x 72
//   expanded  the expanded panel's frame round one screen, at 1268 x 1040
//   tray      the controls the console draws at many sizes: a key (idle, hover, pressed), a square key, a tab, an inset
//             with its screen, a divider cord and the alert lamp (green, yellow, red)
// The strips, the expanded frame and the tray's controls are drawn at sizes other than their sculpt's wherever the console
// asks for one (a key 70 px wide, a phone's wider strips): that is done by the console's nine(), the controls' ends and
// the frames' rims held, so off the reference size they are stretched, which the Organic Panel Bible forbids. That is an
// open question for the owner (openspec/changes/vhorr-battle-console, V1): sculpting every size the console draws.
// The bar symbols, the bar's track and fill still come from the version 23 kit (assets/ui/vhorr/manifest.json), as on
// the console skins page.
const SRC = { column: 'assets/ui/vhorr/organic/battle/column/manifest.json', expanded: 'assets/ui/vhorr/organic/battle/expanded/manifest.json',
  tray: 'assets/ui/vhorr/organic/battle/tray/manifest.json', v23: 'assets/ui/vhorr/manifest.json' };
const IMG = {}, kit = {}, hero = {}, heroSize = {}, barCells = {}, cells = {};
let loading = null;
function json(src) { const n = document.querySelector(`script[data-src="${src}"]`); return n ? JSON.parse(n.textContent) : null; }
const decode = (key, data) => new Promise((ok, bad) => { const im = new Image(); im.onload = () => { IMG[key] = im; ok(); }; im.onerror = () => bad(new Error(`Vhorr piece ${key} failed to decode`)); im.src = data; });
// A slot's image placed where it belongs: a top-level slot's image covers its rectangle grown by its bleed.
function placeSlot(x, man, key, ox = 0, oy = 0) {
  const p = man.pieces[key], s = man.slots[p.slot], b = s.bleed || 0, [rx, ry, rw, rh] = s.rect;
  x.drawImage(IMG[key], rx - b - ox, ry - b - oy, rw + 2 * b, rh + 2 * b);
}
// A top-level slot's own rectangle cut out of its bled image, as a canvas of the rectangle's size (2x).
function cropSlot(man, key) {
  const p = man.pieces[key], s = man.slots[p.slot], b = s.bleed || 0, [, , rw, rh] = s.rect, im = IMG[key], k = im.width / (rw + 2 * b);
  return paint(rw, rh, (x) => x.drawImage(im, b * k, b * k, rw * k, rh * k, 0, 0, rw, rh));
}
// A state's light laid over a key's dark cap (the console's key states the sculpt has no render for: ok, warn, alert, and
// the lit cells): the cap's face glows in the colour, as a sac glows through the skin.
function glowCap(x, w, h, col, a = 0.55) {
  x.save(); x.globalCompositeOperation = 'source-atop';
  const g = x.createRadialGradient(w / 2, h / 2, 0, w / 2, h / 2, Math.max(w, h) * 0.5); g.addColorStop(0, hexA(col, a)); g.addColorStop(0.6, hexA(col, a * 0.6)); g.addColorStop(1, hexA(col, 0));
  x.fillStyle = g; x.fillRect(0, 0, w, h); x.restore();
}
function load() {
  if (loading) return loading;
  const M = Object.fromEntries(Object.entries(SRC).map(([k, s]) => [k, json(s)]));
  const missing = Object.entries(M).filter(([, v]) => !v).map(([k]) => SRC[k]);
  if (missing.length) return (loading = Promise.reject(new Error(`the Vhorr kit (${missing.join(', ')}) is not inlined in this page`)));
  const jobs = [];
  for (const [k, m] of Object.entries(M)) for (const [id, p] of Object.entries(m.pieces)) jobs.push(decode(`${k}:${id}`, p.data));
  loading = Promise.all(jobs).then(() => {
    const at = (c) => (x, w, h) => x.drawImage(c, 0, 0, w, h);
    const piece = (k, id) => IMG[`${k}:${id}`];
    // re-key each manifest's images by their local ids for placeSlot and cropSlot
    for (const [k, m] of Object.entries(M)) for (const id of Object.keys(m.pieces)) IMG[id + '@' + k] = IMG[`${k}:${id}`];
    const local = (k) => ({ pieces: Object.fromEntries(Object.entries(M[k].pieces).map(([id, p]) => [id + '@' + k, p])), slots: M[k].slots });
    // the column: the tissue backing, then each of the column's slots at rest, in the console's order
    const C = local('column'), col = M.column;
    const colCanvas = paint(480, 1080, (x) => {
      if (col.backing) { const [bx, by, bw, bh] = col.backing.image_rect; x.drawImage(piece('column', 'backing'), bx, by, bw, bh); }
      for (const sid of ['nameplate', 'panel.station', 'panel.ship', 'panel.weapons', 'dial', 'crest']) placeSlot(x, C, `${sid}.idle@column`);
    });
    hero['console.column'] = at(colCanvas); heroSize['console.column'] = [480, 1080];
    // the column holds the name plate, the radar's pocket and the crest: the console's own draws of them are empty
    for (const id of ['nameplate', 'dial', 'crest']) hero[id] = () => {};
    // the strips at their sculpted 1440 x 72, their ends held when a wider screen stretches them
    for (const id of ['strip.top', 'strip.bottom']) kit[id] = { size: [1440, 72], m: [0, 140, 0, 140], draw: at(cropSlot(C, `${id}.idle@column`)) };
    // the expanded frame at its sculpted 1268 x 1040, its rim held when another screen shape stretches it
    kit['frame.panel'] = { size: [1268, 1040], m: [90, 90, 90, 90], draw: at(cropSlot(local('expanded'), 'panel.expanded.idle@expanded')) };
    // the tray's controls (children: cut at exactly their rectangles)
    const T = (id) => piece('tray', id);
    for (const st of ['idle', 'hover', 'pressed']) kit['button.' + st] = { size: [150, 48], m: [0, 40, 0, 40], draw: (x, w, h) => x.drawImage(T(`key.${st}`), 0, 0, w, h) };
    for (const st of ['idle', 'active']) kit['tab.' + st] = { size: [120, 40], m: [0, 22, 0, 22], draw: (x, w, h) => x.drawImage(T(`tab.${st}`), 0, 0, w, h) };
    kit['frame.inset'] = { size: [180, 100], m: [26, 26, 26, 26], draw: (x, w, h) => x.drawImage(T('inset.idle'), 0, 0, w, h) };
    kit.divider = { size: [160, 10], m: [0, 30, 0, 30], draw: (x, w, h) => x.drawImage(T('divider.idle'), 0, 0, w, h) };
    for (const c of ['green', 'yellow', 'red']) { hero['lamp.alert.' + c] = (x, w, h) => x.drawImage(T(`lamp.alert.yellow.${c}`), 0, 0, w, h); heroSize['lamp.alert.' + c] = [40, 40]; }
    // the square key in the console's states: at rest, hovered, on (pressed), off (at rest, dimmed), and the cap lit in
    // the state's colour for whole, damaged and down
    const sq = { idle: [T('key.square.idle')], hover: [T('key.square.hover')], on: [T('key.square.pressed')], off: [T('key.square.idle'), null, 0.55],
      ok: [T('key.square.pressed'), VH.will], warn: [T('key.square.pressed'), VH.amber], alert: [T('key.square.pressed'), VH.wound] };
    for (const [st, [im, glow, alpha]] of Object.entries(sq)) {
      const c = paint(48, 48, (x) => { if (alpha) x.globalAlpha = alpha; x.drawImage(im, 0, 0, 48, 48); x.globalAlpha = 1; if (glow) glowCap(x, 48, 48, glow, 0.6); });
      hero['key.' + st] = at(c); heroSize['key.' + st] = [48, 48];
    }
    // the wide key the same way, from the key's sculpt
    const wk = { idle: [T('key.idle')], hover: [T('key.hover')], on: [T('key.pressed')], off: [T('key.idle'), null, 0.55],
      ok: [T('key.pressed'), VH.will], warn: [T('key.pressed'), VH.amber], alert: [T('key.pressed'), VH.wound] };
    for (const [st, [im, glow, alpha]] of Object.entries(wk)) {
      const c = paint(150, 48, (x) => { if (alpha) x.globalAlpha = alpha; x.drawImage(im, 0, 0, 150, 48); x.globalAlpha = 1; if (glow) glowCap(x, 150, 48, glow, 0.5); });
      kit['keyw.' + st] = { size: [150, 48], m: [0, 40, 0, 40], draw: at(c) };
    }
    // the allocation cells: the square key at 32 x 28, lit in the reading's green, blood, or the will's violet
    for (const [id, im, glow, alpha] of [['cell.idle', T('key.square.idle')], ['cell.lit', T('key.square.pressed'), VH.readout], ['cell.warn', T('key.square.pressed'), VH.wound],
      ['cell.accord', T('key.square.pressed'), VH.will], ['cell.locked', T('key.square.idle'), null, 0.35]]) {
      const c = paint(48, 48, (x) => { if (alpha) x.globalAlpha = alpha; x.drawImage(im, 0, 0, 48, 48); x.globalAlpha = 1; if (glow) glowCap(x, 48, 48, glow, 0.7); });
      cells[id] = { size: [32, 28], m: [8, 8, 8, 8], draw: (x, w, h) => x.drawImage(c, 0, 0, w, h) };
    }
    // the title placard: the tab's sculpt drawn 24 px tall (rows 4-36 of 40), its dark cap holding the title
    kit['plate.title'] = { size: [120, 24], m: [0, 22, 0, 22], draw: (x, w, h) => { const im = T('tab.idle'), k = im.height / 40; x.drawImage(im, 0, 4 * k, im.width, 32 * k, 0, 0, w, h); } };
    // the bar's track, fill and symbols: the version 23 kit, as the console skins page has them
    for (const [id, p] of Object.entries(M.v23.pieces)) {
      const im = IMG[`v23:${id}`], draw = (x, w, h) => x.drawImage(im, 0, 0, w, h);
      if (/^bar\.(track|fill)/.test(id)) kit[id] = { size: p.size, m: p.margins, inset: p.inset || [3, 3], draw };
      else if (p.kind === 'cell') barCells[id] = { size: p.size, gap: p.gap ?? 2, draw };
    }
    const tint = (src, c) => (x, w, h) => { src.draw(x, w, h); x.globalCompositeOperation = 'color'; x.fillStyle = c; x.fillRect(0, 0, w, h); x.globalCompositeOperation = 'destination-in'; src.draw(x, w, h); x.globalCompositeOperation = 'source-over'; };
    barCells['bar.cell.ok'] = { ...barCells['bar.cell.on'], draw: tint(barCells['bar.cell.on'], VH.will) };
    kit['bar.fill.accord'] = { ...kit['bar.fill'], draw: tint(kit['bar.fill'], VH.will) };
  });
  return loading;
}

const api = {
  id: 'vhorr', name: 'Vhorr Brood', VH, CC, kit, hero, cells, barCells, live, icon, ICON_NAMES, ink, keyInk, font, fonts, load, hexA, rr, knob, heroSize, tendril, curl,
  family: '"Barlow Condensed", sans-serif',
  // The content inside a panel: on its black screen (the screen 20-22 px in from the slot's edges; the content 6 px in).
  pad: [28, 26, 26, 26],
  // The name plate's live field, on its sculpted plate (356 wide), in bone.
  plate: { x: 26, y: 38, max: 300, font: '400 20px Metamorphous', col: VH.ink },
  // The battle's helm ring under the ship, in the green of life.
  ring: ['rgba(184,242,122,0)', 'rgba(184,242,122,0.14)', 'rgba(210,255,160,0.9)'],
  // Names for the player's side where the page shows it as the Brood: ships named as living things of the brood, their
  // captains Matriarchs (the skins page's SSKARR's MATRIARCH VHAAL kept for its captain). Placeholders until the owner
  // sets the Brood's culture (CLAUDE.md section 1; openspec/changes/vhorr-battle-console, open question V3).
  player: { faction: 'Vhorr Brood', call: 'Matriarch', trim: 0xa6e34a, hull: 0x5a3424,
    ships: { resolute: ['Deep Root', 'Matriarch Vhaal'], tallis: ['Spore Tide', 'Matriarch Ossk'], meridian: ['Hollow Bloom', 'Matriarch Ruvh'] },
    ranks: ['Drone', 'Tender', 'Warden', 'Matriarch'],
    officers: { o1: 'Ssev Ka', o2: 'Uhrr Vael', o3: 'Kesh Ombra', o4: 'Thul Iss', o5: 'Ovvo Rhen', o6: 'Ishk Tal', o7: 'Grava Uun',
      o8: 'Vhet Sor', o9: 'Nuu Akkra', o10: 'Rissh Ol', o11: 'Hasp Vehr', o12: 'Okko Dren', o13: 'Sulv Ath' } },
  link: VH.readout,
  // Labels centred on each key's dark cap (the key's cap runs 27-123 of 150, the tab's 13-107 of 120).
  keyCenter: true, keyField: [30, 30], tabField: [16, 16],
  // A hover name on the hovered key, its name in the warm key light.
  tip: ['button.hover', 'hover'],
  // Icon tabs (the station and ship rails) on the square key: a cap in a wart of flesh.
  iconKeys: true,
  // Panel tab icons at rest in the hide print, apart from the functional icons' green; glyphs on a dark cap in the key light.
  tabIcon: VH.dim, glyphOnDark: VH.keyInk, mark: { hover: VH.bileHot, ready: VH.readout, count: VH.dim },
  // The strips (sculpted at 72 px) at their full height; the glass channel runs rows 12-60, and the content, laid out
  // for 56 px, moved down to centre in it.
  stripTop: { h: 72, dy: 13 }, stripBottom: { h: 72, dy: 2 },
  titleDy: 2,
};
window.FCSkins = window.FCSkins || {}; window.FCSkins.vhorr = api;
return api;
})();
