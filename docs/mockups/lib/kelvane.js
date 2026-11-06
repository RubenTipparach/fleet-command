// Fleet Command mockups: the Kelvane Institute skin (openspec/changes/console-skins/design.md, "Kelvane Institute";
// openspec/changes/kelvane-battle-console), shared by the console skins page (docs/mockups/skins/kelvane.js registers it
// there) and the battle console, where it is the skin of every Kelvane ship you command (the console follows the ship you
// are on; openspec/changes/fleet-skins). One implementation of the Institute's look (CLAUDE.md section 6.1): its palette,
// its icons, its live readouts and, for the battle console, its kit loaded from the Blender render
// (assets/ui/kelvane/manifest.json, tools/blender/build_ui_kelvane.py from tools/mockups/kelvane_shapes.py).
// It exposes the same fields as docs/mockups/lib/concord.js, lib/krai.js and lib/ssarith.js, so the battle console swaps
// one for another and changes no rule and no layout. Needs docs/mockups/lib/kitdraw.js inlined first. Our own design: no
// other game's or film's names, shapes, glyphs or art.
//
// Where it comes from (design.md; owner, 2026-10-31: "Kelvane 100% can be modeled, let and rendered just take the
// osciloscope dash board for inspiration, old school electircal dashboards, dials, switches etc", "They were inspired by
// the nucleo look"): the laboratory bench. Its tradition is the calibrated instrument, built to be checked by eye: pillowed
// champagne plates, readouts lit amber behind glass, phosphor green for a measurement taken, needle red for the reading
// that matters. Nothing here comes from what the Kelvane look like (CLAUDE.md section 1).
window.FCKelvane = (function () {
'use strict';
const { paint, P } = window.FCDraw;
// The palette and what it stands for (design.md): the instrument (champagne), a reading patiently lit (amber), the
// measurement itself (phosphor), the reading that matters (needle red), the record (ink), craft and calibration (brass).
const KV = { champagne: '#c9bfa9', amber: '#ffc865', amberHi: '#ffe2a4', phos: '#5df08a', phosHi: '#b4ffcf', needle: '#ff4f3a', ink: '#4a3412', inkOn: '#2a1a06',
  brass: '#b0915a', parchment: '#f3e4c2', glass: '#1b150d', bg: '#1c1a16' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
// Relative luminance of a #rrggbb colour (WCAG), for picking the ink that reads on a fill.
function lum(hex) {
  if (typeof hex !== 'string' || hex[0] !== '#' || hex.length < 7) return 0;
  const c = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
}

// ---- The battle console's colour roles -------------------------------------------------------------------------
// The same role names as the Concord's (lib/concord.js), so the page's rules pick a role, never a colour. The console's
// readouts glow on the smoked glass of the Institute's screens and over the battle: amber for a reading (Sky), brass print
// for a label (Lilac), parchment for prose and titles, phosphor green for whole and engaged (Accord), a hotter
// amber-orange for damaged (Caution), needle red only for down and alarms (Alert); the dark roles are the glass's own.
// Caution is pulled toward orange so it never reads as a plain reading.
const CC = {
  deep: '#4a3d26', signal: '#4a3820', sky: KV.amber, accord: KV.phos, lilac: '#b8a074', caution: '#ff8f3a', alert: KV.needle, text: KV.parchment,
  ink: KV.inkOn, inkDis: '#5a4a30', night: '#0f0b07', ghost: '#75674f', void: '#0c0906', slate: '#3e3426', mute: '#2a2318', ver: '#a8916a',
  char: '#2c140c', plate: '#16110a', bg: KV.bg, title: '#ffd27a',
};
// The ink that reads on a fill: dark on the lit colours, amber on the dark glass. Nothing on a key is white.
const ink = (fill) => (lum(fill) > 0.2 ? CC.ink : KV.amber);
// Ink on the rendered window keys and tabs (button.*, tab.*): at rest the window is unlit smoke and the name glows on it in
// amber (a tab's a little dimmer, as a choice not taken); lit, on hover and pressed and the chosen tab, the name is dark
// ink on the film, as printed on it (mockup 4).
const keyInk = { idle: KV.amber, hover: KV.inkOn, pressed: KV.inkOn, tabOn: KV.inkOn, tabOff: '#d9aa58' };
// Type: "Share Tech Mono", the instrument's readout face, for every readout (the skin's pair keeps "Courier Prime" for
// labels engraved on metal: the skins page's; the console's labels are all lit on glass). The page asks for Antonio sizes;
// Share Tech Mono runs 1.31 times as wide (measured on the console's own labels), so it is set at 0.8 times the size and
// rule T (keyCaps, the tabs' fit) shrinks what still runs long.
function font(spec) {
  const m = /^(\d+) ([\d.]+)px Antonio$/.exec(spec); if (!m) return spec;
  return `400 ${Math.round(+m[2] * 0.8 * 10) / 10}px "Share Tech Mono"`;
}
const fonts = ['400 20px "Share Tech Mono"', '700 20px "Courier Prime"'];

// ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ---------------------------------------
// Each a picture of the job in the Institute's hand: a schematic drawing, as on an instrument's legend plate. Even
// strokes with round caps and joins, true circles and arcs, sine waves for anything given off; a solid where the thing
// itself is solid (a hull, a turret, a figure), a small dot where a line ends at a terminal. One ink; on a 48 x 48 grid
// with 4 px clear, read at 20 px and 48 px. A bright ink glows softly, as a readout does behind glass.
function icon(name, col = CC.sky) {
  return paint(48, 48, (x) => {
    const bright = lum(col) > 0.2;
    x.strokeStyle = col; x.fillStyle = col; x.lineWidth = 3; x.lineJoin = 'round'; x.lineCap = 'round';
    if (bright) { x.shadowColor = hexA(col, 0.6); x.shadowBlur = 3; }
    const L = (pts, closed = false, lw = 3) => { x.lineWidth = lw; x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); if (closed) x.closePath(); x.stroke(); };
    const F = (pts) => { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); x.closePath(); x.fill(); };
    const C = (cx, cy, r, lw = 3) => { x.lineWidth = lw; x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.stroke(); };
    const A = (cx, cy, r, a0, a1, lw = 3) => { x.lineWidth = lw; x.beginPath(); x.arc(cx, cy, r, a0, a1); x.stroke(); };
    const D = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); };
    const E = (cx, cy, rx, ry, rot = 0) => { x.beginPath(); x.ellipse(cx, cy, rx, ry, rot, 0, 2 * P); x.fill(); };
    const RB = (x0, y0, x1, y1, r, fill, lw = 3) => { x.lineWidth = lw; x.beginPath(); x.roundRect(x0, y0, x1 - x0, y1 - y0, r); if (fill) x.fill(); else x.stroke(); };
    // a sine wave from x0 to x1 about y, amp px high, cycles whole periods
    const wave = (x0, x1, y, amp, cycles, lw = 2.4) => { const pts = []; for (let i = 0; i <= 32; i++) pts.push([x0 + (x1 - x0) * i / 32, y - Math.sin(i / 32 * cycles * 2 * P) * amp]); L(pts, false, lw); };
    ({
      // the helm: a heading dial, its needle on the course and the pivot solid; ticks at the cardinal points
      helm: () => { C(24, 24, 18, 2.6); for (let i = 0; i < 4; i++) { const a = i * P / 2; L([[24 + Math.cos(a) * 14, 24 + Math.sin(a) * 14], [24 + Math.cos(a) * 18, 24 + Math.sin(a) * 18]], false, 2.4); }
        F([[24, 8], [29, 24], [24, 27], [19, 24]]); D(24, 24, 3.6); },
      // power: a bolt inside a meter's ring
      power: () => { C(24, 24, 19, 2.6); F([[27, 9], [15, 27], [23, 27], [20, 40], [33, 21], [25, 21]]); },
      // tactical: a turret on its ring mount, the barrel out to the upper right
      tactical: () => { x.beginPath(); x.arc(22, 32, 13, P, 2 * P); x.closePath(); x.fill(); RB(6, 34, 38, 41, 3, true); L([[26, 24], [41, 9]], false, 4.5); },
      // point defence: a shield with a ranging reticle on it
      pd: () => { L([[24, 5], [39, 11], [39, 25], [24, 43], [9, 25], [9, 11]], true, 2.8); C(24, 22, 7, 2.4); L([[24, 11], [24, 15]], false, 2.4); L([[24, 29], [24, 33]], false, 2.4); L([[13, 22], [17, 22]], false, 2.4); L([[31, 22], [35, 22]], false, 2.4); },
      // science: a lens over a trace, the reading under study
      science: () => { C(20, 20, 13, 3); L([[30, 30], [42, 42]], false, 5); wave(11, 29, 20, 4.5, 1.5, 2.2); },
      // comms: a dish on its stand, two waves going out
      comms: () => { x.beginPath(); x.arc(18, 26, 13, P * 0.75, P * 1.75); x.closePath(); x.fill(); L([[18, 26], [12, 42]], false, 3); L([[6, 42], [20, 42]], false, 3);
        A(18, 26, 21, -P * 0.45, -P * 0.05, 2.6); A(18, 26, 28, -P * 0.42, -P * 0.08, 2.6); },
      // repair: a spanner, its jaw open to the upper right
      repair: () => { L([[9, 39], [27, 21]], false, 5.5); x.beginPath(); x.arc(32, 16, 9, 0, 2 * P); x.fill(); x.save(); x.globalCompositeOperation = 'destination-out'; RB(32, 10, 44, 18, 1.5, true); x.restore(); },
      // transport: a figure, its lower half breaking into lines as it beams
      transport: () => { D(24, 9, 5); F([[15, 30], [17, 18], [31, 18], [33, 30]]); for (const [y, w] of [[34, 9], [38, 7], [42, 5]]) L([[24 - w, y], [24 + w, y]], false, 2.4); },
      // fleet: three ships in formation, the lead ahead, each a pointed hull
      fleet: () => { const sh = (cx, cy, k) => F([[cx, cy - 10 * k], [cx + 6 * k, cy - 1 * k], [cx + 5 * k, cy + 9 * k], [cx - 5 * k, cy + 9 * k], [cx - 6 * k, cy - 1 * k]]); sh(24, 13, 1); sh(11, 33, 0.9); sh(37, 33, 0.9); },
      // the ship from above: a rounded research hull, its prow forward, on two engine pods
      ship: () => { E(24, 19, 8, 15); L([[17, 28], [11, 31]], false, 2.6); L([[31, 28], [37, 31]], false, 2.6); RB(6, 26, 14, 44, 3.5, true); RB(34, 26, 42, 44, 3.5, true); },
      // shields: six arcs round the hull
      shields: () => { for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; A(24, 24, 19, a - 0.4, a + 0.4, 3.2); } E(24, 24, 5, 9); },
      // sensors: a scope with its graticule, a sweep and two contacts
      sensors: () => { C(24, 24, 19, 2.6); L([[24, 24], [37, 11]], false, 2.6); A(24, 24, 9, 0, 2 * P, 1.4); D(31, 30, 2.8); D(13, 17, 2.8); },
      // tractor: an emitter, the beam widening to the hull it holds
      tractor: () => { RB(3, 19, 12, 29, 2, true); L([[12, 21], [31, 12]], false, 2.2); L([[12, 27], [31, 36]], false, 2.2); RB(32, 14, 45, 34, 4, false, 2.6); },
      // craft: a small swept-wing craft
      craft: () => { F([[24, 4], [29, 22], [44, 30], [29, 33], [24, 44], [19, 33], [4, 30], [19, 22]]); },
      // weapons: a beam from an emitter, ending in a strike
      weapons: () => { D(9, 39, 5); L([[12, 36], [30, 18]], false, 3.4); for (let i = 0; i < 6; i++) { const a = i * P / 3; L([[34 + Math.cos(a) * 4, 14 + Math.sin(a) * 4], [34 + Math.cos(a) * 10, 14 + Math.sin(a) * 10]], false, 2.2); } },
      // warp: a chevron trailing speed lines
      warp: () => { L([[22, 10], [38, 24], [22, 38]], false, 4.5); L([[5, 16], [17, 16]], false, 2.6); L([[3, 24], [21, 24]], false, 2.6); L([[5, 32], [17, 32]], false, 2.6); },
      // engines: a nozzle and the exhaust wave out of it
      engines: () => { F([[14, 5], [34, 5], [38, 21], [10, 21]]); L([[16, 27], [32, 27]], false, 2.6); L([[19, 34], [29, 34]], false, 2.6); L([[22, 41], [26, 41]], false, 2.6); },
      // reactor: a solid core and two crossed orbits
      reactor: () => { D(24, 24, 5.5); x.lineWidth = 2.6; for (const r of [P / 4, -P / 4]) { x.beginPath(); x.ellipse(24, 24, 19, 8, r, 0, 2 * P); x.stroke(); } },
      // map: a plotted course, dotted, to a mark
      map: () => { for (const [px, py] of [[8, 40], [12, 33], [12, 26], [17, 20], [24, 18]]) D(px, py, 2.4); L([[29, 6], [41, 18]], false, 3.4); L([[41, 6], [29, 18]], false, 3.4); },
      pause: () => { L([[17, 11], [17, 37]], false, 6.5); L([[31, 11], [31, 37]], false, 6.5); },
      menu: () => { for (const y of [13, 24, 35]) L([[10, y], [38, y]], false, 4.2); },
      // fire: four brackets closing on a solid mark
      fire: () => { for (const [a, b, c] of [[[8, 17], [8, 8], [17, 8]], [[31, 8], [40, 8], [40, 17]], [[40, 31], [40, 40], [31, 40]], [[17, 40], [8, 40], [8, 31]]]) L([a, b, c], false, 3.2); D(24, 24, 5); },
      cargo: () => { for (const [cx, cy] of [[15, 15], [33, 15], [15, 33], [33, 33]]) RB(cx - 7.5, cy - 7.5, cx + 7.5, cy + 7.5, 3, false, 2.6); },
      // settings: a cog with round teeth round its bore
      settings: () => { C(24, 24, 12, 3.4); for (let i = 0; i < 8; i++) { const a = i * P / 4 + P / 8; D(24 + Math.cos(a) * 16, 24 + Math.sin(a) * 16, 3.6); } C(24, 24, 4.4, 2.6); },
    })[name]();
  });
}
const ICON_NAMES = ['helm', 'power', 'tactical', 'pd', 'science', 'comms', 'repair', 'transport', 'fleet', 'ship', 'shields', 'sensors', 'tractor', 'craft', 'weapons', 'warp', 'engines', 'reactor', 'map', 'pause', 'menu', 'fire', 'cargo', 'settings'];

// ---- Live readouts (the console skins page and the battle console) ---------------------------------------------
// Each drawn inside its own well: on a domed key, on the phosphor scopes, over the battle.
const live = {
  // An arrow engraved on each domed key.
  arrow: (x) => { x.fillStyle = 'rgba(30, 24, 16, 0.85)'; x.beginPath(); x.moveTo(7, 0); x.lineTo(-4, -6); x.lineTo(-4, 6); x.closePath(); x.fill(); },
  // STOP glows on the helm's phosphor scope (radius 50), well inside it.
  helmCentre: (x, c) => { x.save(); x.shadowColor = KV.phos; x.shadowBlur = 8; x.fillStyle = KV.phosHi; x.font = '400 17px "Share Tech Mono"'; x.textAlign = 'center'; x.fillText('STOP', c, c + 6); x.restore(); },
  // The ship as a phosphor trace on the scope: a rounded research hull with dishes; shields as dotted arcs. Drawn at 1.4x
  // the old scope's scale, so the shield rings ride radius 134 on the 135 px graticule (owner: "ship display should take up
  // 90% of the panel or more").
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
  // The radar's sweep and a decaying trace, inside its scope (radius 51.5); contacts as red dots.
  dial: (x, c, cts) => {
    x.save(); x.shadowColor = KV.phos; x.shadowBlur = 8; x.strokeStyle = KV.phos; x.lineWidth = 1.6; x.beginPath(); x.moveTo(c, c); x.lineTo(c + Math.cos(-0.7) * 44, c + Math.sin(-0.7) * 44); x.stroke();
    x.lineWidth = 1.2; x.beginPath(); for (let i = 0; i <= 60; i++) { const px = c - 36 + i * 72 / 60, py = c + 20 + Math.sin(i / 60 * 6 * P) * 5 * Math.exp(-i / 40); if (i) x.lineTo(px, py); else x.moveTo(px, py); } x.stroke();
    x.fillStyle = '#ff6a50'; x.shadowColor = '#ff6a50'; for (const [dx, dy] of cts) { x.beginPath(); x.arc(c + dx * 34, c + dy * 34, 3, 0, 2 * P); x.fill(); }
    x.restore();
  },
  // A strip meter's needle (the battle console's stripMeter): a needle-red pointer from the pivot over the printed scale,
  // angle a in radians, length r, with a dark brass hub on the pivot and a faint shadow on the film below it.
  needle: (x, cx, cy, r, a) => {
    x.save(); x.lineCap = 'round';
    x.strokeStyle = 'rgba(60, 36, 10, 0.35)'; x.lineWidth = 1.6; x.beginPath(); x.moveTo(cx + 0.8, cy + 1.2); x.lineTo(cx + 0.8 + Math.cos(a) * r, cy + 1.2 + Math.sin(a) * r); x.stroke();
    x.strokeStyle = '#c8302a'; x.lineWidth = 1.4; x.beginPath(); x.moveTo(cx - Math.cos(a) * 3, cy - Math.sin(a) * 3); x.lineTo(cx + Math.cos(a) * r, cy + Math.sin(a) * r); x.stroke();
    x.fillStyle = '#5a4520'; x.beginPath(); x.arc(cx, cy, 2.6, 0, 2 * P); x.fill(); x.fillStyle = '#b0915a'; x.beginPath(); x.arc(cx - 0.5, cy - 0.5, 1.2, 0, 2 * P); x.fill();
    x.restore();
  },
  // Scope cursors: thin phosphor corners with graduation ticks, as an oscilloscope's measuring cursors.
  bracket: (x, bx, by, bw, bh) => {
    x.save(); x.shadowColor = KV.phos; x.shadowBlur = 8; x.strokeStyle = KV.phos; x.lineWidth = 2;
    const arm = Math.min(44, bw * 0.4), n = Math.max(2, Math.floor(arm / 9));
    for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
      x.beginPath(); x.moveTo(px + sx * arm, py); x.lineTo(px, py); x.lineTo(px, py + sy * arm); x.stroke();
      x.lineWidth = 1; for (let i = 1; i < n; i++) { x.beginPath(); x.moveTo(px + sx * i * 9, py); x.lineTo(px + sx * i * 9, py + sy * 5); x.moveTo(px, py + sy * i * 9); x.lineTo(px + sx * 5, py + sy * i * 9); x.stroke(); } x.lineWidth = 2;
    }
    x.restore();
  },
};
// The Institute's mark where the console draws an emblem (a portrait, the holder of a ship): the tuning fork over a sine
// wave, as the crest has it, in brass light.
function emblem(G, cx, cy, r) {
  G.save(); G.translate(cx, cy); G.scale(r / 24, r / 24); G.strokeStyle = CC.title; G.lineWidth = 2.6; G.lineCap = 'round'; G.lineJoin = 'round';
  G.beginPath(); G.moveTo(-8, -20); G.lineTo(-8, -2); G.arc(0, -2, 8, P, 0, true); G.lineTo(8, -20); G.stroke();
  G.beginPath(); G.moveTo(0, 6); G.lineTo(0, 20); G.stroke();
  G.lineWidth = 2; G.beginPath(); for (let i = 0; i <= 32; i++) { const px = -20 + i * 40 / 32, py = 12 - Math.sin(i / 32 * 4 * P) * 4; if (i) G.lineTo(px, py); else G.moveTo(px, py); } G.stroke();
  G.restore();
}

// ---- Shapes for the battle console's vector keys ----------------------------------------------------------------
// The Institute's controls are rounded, never cut: pressed plates with soft corners and round knobs.
function rr(G, X, Y, W, H, r) { G.beginPath(); if (!(W > 0 && H > 0)) { G.rect(X, Y, W, H); return; } G.roundRect(X, Y, W, H, Math.min(Array.isArray(r) ? Math.max(...r) : (+r || 0), W / 2, H / 2)); }
function knob(G, cx, cy, r) { G.beginPath(); G.arc(cx, cy, r, 0, 2 * P); }

// ---- The kit ------------------------------------------------------------------------------------------------------
// Every piece is rendered in Blender (tools/blender/build_ui_kelvane.py) and comes from the inlined manifest; load()
// decodes them once. The skins page (mockup 4) shows the kit's plain plates under their own names; the battle console
// draws its readouts in light over a panel's middle, its strips and its cards, so here the console's own screened pieces
// stand in under those names (CONSOLE): the plate rolled round a well of smoked glass, the strips with a long screen, a
// key and a tab unlit at rest (their legend lit on the dark window), the name plate with a flat for the Fleet key.
const MANIFEST = 'assets/ui/kelvane/manifest.json';
const CONSOLE = { 'frame.panel': 'frame.screen', 'frame.inset': 'inset.screen', 'strip.top': 'strip.top.screen', 'strip.bottom': 'strip.bottom.screen',
  'button.idle': 'button.idle.screen', 'tab.idle': 'tab.idle.screen', nameplate: 'nameplate.console' };
const IMG = {}, kit = {}, hero = {}, heroSize = {}, barCells = {};
let loading = null;
function load() {
  if (loading) return loading;
  const n = document.querySelector(`script[data-src="${MANIFEST}"]`);
  if (!n) return (loading = Promise.reject(new Error(`the Kelvane kit (${MANIFEST}) is not inlined in this page`)));
  const d = JSON.parse(n.textContent);
  loading = Promise.all(Object.entries(d.pieces).map(([id, p]) => new Promise((ok, bad) => { const im = new Image(); im.onload = () => { IMG[id] = im; ok(); }; im.onerror = () => bad(new Error(`Kelvane piece ${id} failed to decode`)); im.src = p.data; }))).then(() => {
    for (const [id, p] of Object.entries(d.pieces)) {
      const draw = (x, w, h) => x.drawImage(IMG[id], 0, 0, w, h);
      if (p.kind === 'nine') kit[id] = { size: p.size, m: p.margins, inset: p.inset || [3, 3], draw };
      else if (p.kind === 'hero') { hero[id] = draw; heroSize[id] = p.size; }
      else if (p.kind === 'cell') barCells[id] = { size: p.size, gap: p.gap ?? 1, draw };
    }
    for (const [name, piece] of Object.entries(CONSOLE)) {
      if (kit[piece]) kit[name] = kit[piece];
      else if (hero[piece]) { hero[name] = hero[piece]; heroSize[name] = heroSize[piece]; }
      else throw new Error(`Kelvane console piece ${piece} is missing from ${MANIFEST}`);
    }
  });
  return loading;
}

const api = {
  id: 'kelvane', name: 'Kelvane Institute', KV, CC, kit, hero, cells: {}, barCells, live, icon, ICON_NAMES, emblem, ink, keyInk, font, fonts, load, hexA, rr, knob, heroSize,
  family: '"Share Tech Mono", monospace',
  // The content inside a panel: clear of the 12 px bezel and 12 px into the glass (frame.screen's well, 12 px in).
  pad: [26, 24, 24, 24],
  // The name plate's live field (nameplate.console's amber window, 46-360 x 9-47), 8 px clear of its left end, in ink on
  // the lit film, as the skins page's.
  plate: { x: 54, y: 36, max: 300, font: '400 22px "Share Tech Mono"', col: KV.ink },
  // The battle's helm ring under the ship, in amber.
  ring: ['rgba(255,200,101,0)', 'rgba(255,200,101,0.14)', 'rgba(255,220,150,0.9)'],
  // Names for the player's side where the page shows it as the Institute: research vessels named for the virtues of
  // inquiry, their captains doctors (the skins page's RV PATIENCE, DR. ORREL). Placeholders until the owner sets the
  // Institute's culture (CLAUDE.md section 1; openspec/changes/kelvane-battle-console, open question K1).
  player: { faction: 'Kelvane Institute', call: 'Doctor', trim: 0xffc865, hull: 0xc9bfa9,
    ships: { resolute: ['Patience', 'Dr. Orrel'], tallis: ['Candour', 'Dr. Vessimer'], meridian: ['Parallax', 'Dr. Ithane'] },
    ranks: ['Fellow', 'Lecturer', 'Reader', 'Professor'],
    officers: { o1: 'Amsel Tey', o2: 'Corin Halvard', o3: 'Isra Penhallow', o4: 'Teodor Quill', o5: 'Mireth Oake', o6: 'Benne Calloway', o7: 'Ostrid Vale',
      o8: 'Halden Pryce', o9: 'Lisbet Arrow', o10: 'Nemo Kask', o11: 'Vida Sorrel', o12: 'Ewan Teague', o13: 'Rhosyn Mallory' } },
  link: KV.amber,
  // Labels centred in each window key's open field, its window: on a key 44-140 of 150 (46 px from the left, past the
  // domed key, 12 from the right; the 9-slice caps run further, 54 and 16 px, to keep the window's corners whole); on a
  // tab 28-112 of 120 (30 px from the left, past the pilot lamp, 10 from the right).
  keyCenter: true, tabField: [30, 10], keyField: [46, 12],
  // A hover name on the wide window key lit amber, its name in ink, as every lit window on the instrument.
  tip: ['keyw.hover', 'hover'],
  // Icon tabs (the station and ship rails) on the square window key, the icon inside the window; dark at rest, lit amber
  // on hover, hot when chosen.
  iconKeys: true,
  // Panel tab icons at rest in parchment, apart from the functional icons' amber; glyphs on a dark window in amber.
  tabIcon: KV.parchment, glyphOnDark: KV.amber, mark: { hover: KV.amberHi, ready: KV.phos, count: KV.parchment },
  // The strips (strip.top.screen and strip.bottom.screen, 72 px, as the skins page's: owner, 2026-10-31, "we need to make
  // parent taller") at their full height; the content, laid out for 56 px, moved down to centre in the screen (9-63).
  stripTop: { h: 72, dy: 14 }, stripBottom: { h: 72, dy: 4 },
  // The strips' needle meter (strip.*.screen's bay): its pivot (46, 50) and needle length 20 px in the 72 px strip, swept
  // 205 to 335 degrees over the printed scale; it settles at 4 per second toward the reading.
  meter: { cx: 46, cy: 50, h: 72, r: 20, a0: 205 * P / 180, a1: 335 * P / 180, settle: 4 },
  // A panel title's baseline 2 px higher than the console's, centred in the title placard's window (y 3-21 of 24).
  titleDy: 2,
};
window.FCSkins = window.FCSkins || {}; window.FCSkins.kelvane = api;
return api;
})();
