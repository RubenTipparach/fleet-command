// Fleet Command mockups: the Ssarith Hunt skin (openspec/changes/console-skins/design.md, "Ssarith Hunt";
// openspec/changes/ssarith-battle-console), shared by the console skins page (docs/mockups/skins/ssarith.js registers it
// there) and the battle console, where it is the skin of every Ssarith ship you command (the console follows the ship
// you are on; openspec/changes/fleet-skins). One implementation of the Hunt's look (CLAUDE.md section 6.1): its palette,
// its icons, its live readouts and, for the battle console, its kit loaded from the Blender render
// (assets/ui/ssarith/manifest.json, tools/blender/build_ui_ssarith.py from tools/mockups/ssarith_shapes.py).
// It exposes the same fields as docs/mockups/lib/concord.js and lib/krai.js, so the battle console swaps one for another
// and changes no rule and no layout. Needs docs/mockups/lib/kitdraw.js inlined first. Our own design: no other game's
// or film's names, shapes, glyphs or art.
window.FCSsarith = (function () {
'use strict';
const { paint, P } = window.FCDraw;
// The palette and what it stands for (design.md): the hunter's gear, the hunting ground, the tracker, the count of
// kills, the strike.
const SS = { gun: '#3a4044', canopy: '#2f4a2a', moss: '#5b6340', phos: '#6dff8a', phosLo: '#2f9a4f', bone: '#d8cfb4', kill: '#e0533c', amber: '#ffb340',
  ink: '#d8cfb4', dim: '#7f8f6e', glow: 'rgba(109,255,138,0.55)' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
// Relative luminance of a #rrggbb colour (WCAG), for picking the ink that reads on a fill.
function lum(hex) {
  if (typeof hex !== 'string' || hex[0] !== '#' || hex.length < 7) return 0;
  const c = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
}
// A phosphor stroke: the trace drawn with a soft glow under a sharp line.
function glowStroke(x, w, col) { x.save(); x.shadowColor = SS.glow; x.shadowBlur = 8; x.strokeStyle = col || SS.phos; x.lineWidth = w; x.stroke(); x.restore(); }

// ---- The battle console's colour roles -------------------------------------------------------------------------
// The same role names as the Concord's (lib/concord.js), so the page's rules pick a role, never a colour. The Hunt:
// dark tubes at rest, phosphor for readouts, hover and the whole, amber for the damaged, kill red only for the down and
// the warnings (design.md: "Alerts only"), moss labels, bone text and titles, gunmetal and canopy for the dark.
const CC = {
  deep: '#163020', signal: '#1d4a2a', sky: '#6dff8a', accord: '#6dff8a', lilac: '#9aa886', caution: '#ffb340', alert: '#e0533c', text: '#d8cfb4',
  ink: '#06120a', inkDis: '#3a4a3a', night: '#0a140d', ghost: '#4f5e4a', void: '#07100a', slate: '#2e3c30', mute: '#1c241e', ver: '#7f8f6e',
  char: '#2a1410', plate: '#141b16', bg: '#0c120d', title: SS.bone, bone: SS.bone,
};
// The ink that reads on a fill: dark on the lit colours, phosphor on the dark tubes. Nothing on a key is white.
const ink = (fill) => (lum(fill) > 0.2 ? CC.ink : SS.phos);
// Ink on the rendered tube keys and tabs: phosphor on the dark tube at rest, bone on the lit tube (hover, the active
// tab), dark on the flaring engaged tube; a mode tab's name at rest in moss.
const keyInk = { idle: SS.phos, hover: SS.bone, pressed: CC.ink, tabOn: SS.bone, tabOff: SS.dim };
// Type: "Black Ops One" stencilled for titles and headings, "VT323" phosphor for everything else (the skin's own pair).
// The page asks for Antonio sizes; VT323 is set 1.2x Antonio's size, which measures about as wide (a pixel face is short),
// so every slot that fits a Concord label fits a Hunt one. Weight 700 at 22 px and up is a heading: Black Ops One at 0.8x.
function font(spec) {
  const m = /^(\d+) ([\d.]+)px Antonio$/.exec(spec); if (!m) return spec;
  const w = +m[1], s = +m[2];
  if (w >= 700 && s >= 22) return `400 ${Math.round(s * 0.8 * 10) / 10}px "Black Ops One"`;
  return `400 ${Math.round(s * 1.2 * 10) / 10}px VT323`;
}
const fonts = ['400 20px VT323', '400 16px "Black Ops One"'];

// ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ---------------------------------------
// Each a picture of the job in the Hunt's hand: straight strokes and 45 degree turns, cut corners, glowing phosphor
// traces as on its tubes (lines only, a hunter's field marks), a small solid only where the thing itself is solid. One
// ink; on a 48 x 48 grid with 4 px clear, read at 20 px and 48 px.
function icon(name, col = CC.sky) {
  return paint(48, 48, (x) => {
    const bright = lum(col) > 0.2;
    x.strokeStyle = col; x.fillStyle = col; x.lineWidth = 3; x.lineJoin = 'miter'; x.lineCap = 'square';
    if (bright) { x.shadowColor = hexA(col, 0.7); x.shadowBlur = 4; }
    const S = (pts, closed = false, lw = 3) => { x.lineWidth = lw; x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); if (closed) x.closePath(); x.stroke(); x.lineWidth = 3; };
    const F = (pts) => { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); x.closePath(); x.fill(); };
    const oct = (cx, cy, r) => Array.from({ length: 8 }, (_, i) => [cx + Math.cos(P / 8 + i * P / 4) * r, cy + Math.sin(P / 8 + i * P / 4) * r]);
    const cutBox = (x0, y0, x1, y1, c) => [[x0 + c, y0], [x1 - c, y0], [x1, y0 + c], [x1, y1 - c], [x1 - c, y1], [x0 + c, y1], [x0, y1 - c], [x0, y0 + c]];
    ({
      // a wheel: an octagonal rim, four spokes, a square hub
      helm: () => { S(oct(24, 24, 17), true); S([[24, 7], [24, 41]], false, 2.4); S([[7, 24], [41, 24]], false, 2.4); F(cutBox(19, 19, 29, 29, 3)); },
      // power: a jagged bolt inside a cut-cornered cell
      power: () => { S(cutBox(6, 6, 42, 42, 9), true, 2.4); F([[27, 9], [15, 26], [23, 26], [20, 39], [33, 21], [25, 21]]); },
      // tactical: a gun on its mount: a stepped base, a housing, one long barrel
      tactical: () => { F([[6, 42], [42, 42], [38, 36], [10, 36]]); S([[13, 35], [13, 27], [21, 21], [31, 21], [35, 27], [35, 35]], false); S([[26, 24], [42, 8]], false, 4); },
      // point defence: a cut-cornered shield with a crosshair
      pd: () => { S([[24, 5], [39, 11], [39, 27], [24, 43], [9, 27], [9, 11]], true); S([[24, 15], [24, 33]], false, 2.2); S([[15, 24], [33, 24]], false, 2.2); },
      // science: a lens with a reading in it, the tracker's scope
      science: () => { S(oct(20, 20, 13), true); S([[30, 30], [42, 42]], false, 4); S([[12, 22], [16, 22], [19, 15], [22, 25], [25, 19], [28, 19]], false, 2); },
      // comms: a dish on a stand and two waves going out
      comms: () => { S([[6, 18], [26, 38], [12, 34], [6, 18]], true, 2.6); S([[16, 28], [26, 18]], false, 2.4); S([[14, 36], [10, 44]], false); S([[4, 44], [20, 44]], false);
        S([[30, 12], [36, 18]], false, 2.4); S([[32, 4], [44, 16]], false, 2.4); },
      // repair: a wrench, its jaw open
      repair: () => { S([[8, 40], [26, 22]], false, 4.5); S([[24, 16], [30, 10], [38, 10], [32, 16], [32, 20], [38, 20], [38, 28], [30, 28], [24, 22]], true, 2.6); },
      // transport: a figure, its lower half breaking into bars as it beams
      transport: () => { F(cutBox(19, 4, 29, 14, 3)); S([[14, 30], [16, 19], [32, 19], [34, 30]], false); for (const [y, w] of [[33, 16], [37, 12], [41, 8], [45, 5]]) S([[24 - w, y], [24 + w, y]], false, 2); },
      // fleet: three ships in formation, broad prows forward
      fleet: () => { const sh = (cx, cy, k) => S([[cx - 6 * k, cy - 3 * k], [cx, cy - 8 * k], [cx + 6 * k, cy - 3 * k], [cx + 5 * k, cy + 7 * k], [cx - 5 * k, cy + 7 * k]], true, 2.4); sh(24, 13, 1.1); sh(11, 33, 1); sh(37, 33, 1); },
      // the ship from above: a squat armoured hull with a broad prow
      ship: () => { S([[12, 12], [24, 5], [36, 12], [40, 28], [33, 43], [15, 43], [8, 28]], true); S([[16, 20], [32, 20]], false, 2); S([[14, 31], [34, 31]], false, 2); },
      // shields: a ring of six plates round the hull
      shields: () => { for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; S([[24 + Math.cos(a - 0.38) * 19, 24 + Math.sin(a - 0.38) * 19], [24 + Math.cos(a + 0.38) * 19, 24 + Math.sin(a + 0.38) * 19]], false, 3.4); } F(cutBox(19, 18, 29, 30, 3)); },
      // sensors: a scope with a sweep and two contacts
      sensors: () => { S(oct(24, 24, 19), true, 2.4); S([[24, 24], [24, 6]], false, 2.4); S([[24, 24], [37, 11]], false, 2.4); F(cutBox(30, 27, 36, 33, 1.5)); F(cutBox(12, 29, 17, 34, 1.5)); },
      // tractor: an emitter, a widening beam, the held hull boxed at its end
      tractor: () => { F([[3, 19], [12, 21], [12, 27], [3, 29]]); S([[12, 21], [32, 12]], false, 2.2); S([[12, 27], [32, 36]], false, 2.2); S(cutBox(33, 15, 45, 33, 3), true, 2.6); },
      // craft: a small hunter, swept wings
      craft: () => { S([[24, 4], [28, 22], [44, 30], [28, 32], [24, 44], [20, 32], [4, 30], [20, 22]], true, 2.6); },
      // weapons: a beam from an emitter ending in a strike
      weapons: () => { F([[4, 44], [14, 41], [7, 34]]); S([[10, 38], [30, 18]], false, 3.6); S([[30, 8], [30, 28]], false, 2.4); S([[20, 18], [40, 18]], false, 2.4); S([[23, 11], [37, 25]], false, 2); S([[37, 11], [23, 25]], false, 2); },
      // warp: a chevron trailing speed lines
      warp: () => { S([[22, 10], [38, 24], [22, 38]], false, 4); S([[4, 16], [18, 16]], false); S([[2, 24], [22, 24]], false); S([[4, 32], [18, 32]], false); },
      // engines: a nozzle and its exhaust
      engines: () => { S([[15, 4], [33, 4], [37, 22], [11, 22]], true, 2.8); S([[14, 27], [34, 27], [24, 44]], true, 2.4); S([[24, 30], [24, 38]], false, 2); },
      // reactor: an octagonal core with three vanes
      reactor: () => { F(oct(24, 24, 5)); for (let i = 0; i < 3; i++) { const a = -P / 2 + i * 2 * P / 3; S([[24 + Math.cos(a) * 9, 24 + Math.sin(a) * 9], [24 + Math.cos(a) * 20, 24 + Math.sin(a) * 20], [24 + Math.cos(a + 0.6) * 20, 24 + Math.sin(a + 0.6) * 20]], false, 3); } },
      // map: a trail to an X
      map: () => { S([[8, 40], [14, 32], [12, 24], [20, 18], [28, 20], [32, 13]], false, 2.4); S([[30, 4], [42, 16]], false); S([[42, 4], [30, 16]], false); },
      pause: () => { S([[17, 10], [17, 38]], false, 6); S([[31, 10], [31, 38]], false, 6); },
      menu: () => { for (const y of [13, 24, 35]) S([[10, y], [38, y]], false, 4); },
      // fire: brackets closing on a mark
      fire: () => { for (const [a, b, c] of [[[8, 16], [8, 8], [16, 8]], [[32, 8], [40, 8], [40, 16]], [[40, 32], [40, 40], [32, 40]], [[16, 40], [8, 40], [8, 32]]]) S([a, b, c], false, 3); F(cutBox(20, 20, 28, 28, 2.5)); },
      cargo: () => { for (const [cx, cy] of [[15, 15], [33, 15], [15, 33], [33, 33]]) S(cutBox(cx - 8, cy - 8, cx + 8, cy + 8, 3), true, 2.6); },
      // settings: a cog with square teeth round an octagonal bore
      settings: () => { S(oct(24, 24, 13), true, 3); for (let i = 0; i < 8; i++) { const a = i * P / 4; S([[24 + Math.cos(a) * 15, 24 + Math.sin(a) * 15], [24 + Math.cos(a) * 21, 24 + Math.sin(a) * 21]], false, 4.5); } S(oct(24, 24, 5), true, 2.4); },
    })[name]();
  });
}
const ICON_NAMES = ['helm', 'power', 'tactical', 'pd', 'science', 'comms', 'repair', 'transport', 'fleet', 'ship', 'shields', 'sensors', 'tractor', 'craft', 'weapons', 'warp', 'engines', 'reactor', 'map', 'pause', 'menu', 'fire', 'cargo', 'settings'];

// ---- Live readouts (the console skins page and the battle console) ---------------------------------------------
const live = {
  arrow: (x) => { x.save(); x.shadowColor = SS.glow; x.shadowBlur = 6; x.fillStyle = SS.phos; x.beginPath(); x.moveTo(10, 0); x.lineTo(-6, -8); x.lineTo(-2, 0); x.lineTo(-6, 8); x.closePath(); x.fill(); x.restore(); },
  helmCentre: () => {},
  // The ship as a tracker sees it: a squat armoured hull with a broad prow, drawn as phosphor; shields as dashed arcs.
  ssd: (x, cx, cy, sh, col) => {
    x.save();
    for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.setLineDash([6, 4]); x.beginPath(); x.arc(cx, cy, 98, a - P / 6 + 0.1, a + P / 6 - 0.1); x.shadowColor = col(sh[i]); x.shadowBlur = 8; x.strokeStyle = col(sh[i]); x.lineWidth = 3; x.stroke(); }
    x.setLineDash([]);
    x.beginPath(); x.moveTo(cx, cy - 66); x.lineTo(cx + 26, cy - 40); x.lineTo(cx + 36, cy + 10); x.lineTo(cx + 22, cy + 56); x.lineTo(cx - 22, cy + 56); x.lineTo(cx - 36, cy + 10); x.lineTo(cx - 26, cy - 40); x.closePath();
    glowStroke(x, 2);
    x.beginPath(); x.moveTo(cx - 18, cy - 20); x.lineTo(cx + 18, cy - 20); x.moveTo(cx - 24, cy + 20); x.lineTo(cx + 24, cy + 20); glowStroke(x, 1.4, SS.phosLo);
    x.fillStyle = SS.phos; for (const [mx, my] of [[cx, cy - 54], [cx - 28, cy + 2], [cx + 28, cy + 2]]) { x.beginPath(); x.rect(mx - 3, my - 3, 6, 6); x.fill(); }
    x.restore();
  },
  // A tracking sweep on the radar's tube (radius 46): a fading wedge, the quarry as bone-white marks.
  dial: (x, c, cts) => {
    x.save();
    const g = x.createRadialGradient(c, c, 0, c, c, 46); g.addColorStop(0, 'rgba(109,255,138,0.0)'); g.addColorStop(1, 'rgba(109,255,138,0.28)');
    x.fillStyle = g; x.beginPath(); x.moveTo(c, c); x.arc(c, c, 40, -1.2, -0.55); x.closePath(); x.fill();
    x.beginPath(); x.moveTo(c, c); x.lineTo(c + Math.cos(-0.55) * 40, c + Math.sin(-0.55) * 40); glowStroke(x, 1.6);
    x.beginPath(); x.arc(c, c, 22, 0, 2 * P); x.setLineDash([2, 4]); glowStroke(x, 1, SS.phosLo); x.setLineDash([]);
    x.strokeStyle = SS.bone; x.lineWidth = 2;
    for (const [dx, dy] of cts) { const px = c + dx * 36, py = c + dy * 36; x.beginPath(); x.moveTo(px - 4, py - 4); x.lineTo(px + 4, py + 4); x.moveTo(px + 4, py - 4); x.lineTo(px - 4, py + 4); x.stroke(); }
    x.restore();
  },
  // Tracking chevrons closing on the quarry from the four corners.
  bracket: (x, bx, by, bw, bh) => {
    x.save();
    for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
      x.beginPath(); x.moveTo(px + sx * 34, py); x.lineTo(px, py); x.lineTo(px, py + sy * 34); glowStroke(x, 2.4);
      x.beginPath(); x.moveTo(px + sx * 10, py + sy * 18); x.lineTo(px + sx * 18, py + sy * 18); x.lineTo(px + sx * 18, py + sy * 10); glowStroke(x, 1.4, SS.phosLo);
    }
    x.restore();
  },
};
// The Hunt's mark where the console draws an emblem (a portrait, the holder of a ship): a trophy fang hung inside a
// tracking reticle, in bone, as the crest has it.
function emblem(G, cx, cy, r) {
  G.save(); G.translate(cx, cy); G.scale(r / 24, r / 24); G.strokeStyle = SS.bone; G.lineWidth = 2.4; G.lineJoin = 'miter';
  for (const a of [0.25, 0.75, 1.25, 1.75]) { const ang = a * P; G.beginPath(); G.moveTo(Math.cos(ang - 0.35) * 22, Math.sin(ang - 0.35) * 22); G.lineTo(Math.cos(ang) * 11, Math.sin(ang) * 11); G.lineTo(Math.cos(ang + 0.35) * 22, Math.sin(ang + 0.35) * 22); G.stroke(); }
  G.fillStyle = SS.bone; G.beginPath(); G.moveTo(-5, -11); G.lineTo(5, -11); G.lineTo(3, 3); G.lineTo(0, 12); G.lineTo(-3, 3); G.closePath(); G.fill();
  G.restore();
}

// ---- Shapes for the battle console's vector keys ----------------------------------------------------------------
// The hunter's kit is cut-cornered, never round (design.md): the page's rounded keys are drawn with every corner cut at
// 45 degrees, the cut as big as the rounding asked for; round knobs become octagons.
function rr(G, X, Y, W, H, r) {
  G.beginPath(); if (!(W > 0 && H > 0)) { G.rect(X, Y, W, H); return; }
  const R = Array.isArray(r) ? Math.max(...r) : (+r || 0), c = Math.max(0, Math.min(R * 0.7, W / 2, H / 2));
  G.moveTo(X + c, Y); G.lineTo(X + W - c, Y); G.lineTo(X + W, Y + c); G.lineTo(X + W, Y + H - c); G.lineTo(X + W - c, Y + H); G.lineTo(X + c, Y + H); G.lineTo(X, Y + H - c); G.lineTo(X, Y + c); G.closePath();
}
function knob(G, cx, cy, r) { G.beginPath(); for (let i = 0; i < 8; i++) { const a = P / 8 + i * P / 4; G.lineTo(cx + Math.cos(a) * r * 1.05, cy + Math.sin(a) * r * 1.05); } G.closePath(); }

// ---- The kit ------------------------------------------------------------------------------------------------------
// Every piece is rendered in Blender (tools/blender/build_ui_ssarith.py), the battle console's own included (the tube
// keys, the wide keys and the allocation cells), and comes from the inlined manifest; load() decodes them once.
const MANIFEST = 'assets/ui/ssarith/manifest.json';
const IMG = {}, kit = {}, hero = {}, heroSize = {}, barCells = {};
let loading = null;
function load() {
  if (loading) return loading;
  const n = document.querySelector(`script[data-src="${MANIFEST}"]`);
  if (!n) return (loading = Promise.reject(new Error(`the Ssarith kit (${MANIFEST}) is not inlined in this page`)));
  const d = JSON.parse(n.textContent);
  loading = Promise.all(Object.entries(d.pieces).map(([id, p]) => new Promise((ok, bad) => { const im = new Image(); im.onload = () => { IMG[id] = im; ok(); }; im.onerror = () => bad(new Error(`Ssarith piece ${id} failed to decode`)); im.src = p.data; }))).then(() => {
    for (const [id, p] of Object.entries(d.pieces)) {
      const draw = (x, w, h) => x.drawImage(IMG[id], 0, 0, w, h);
      if (p.kind === 'nine') kit[id] = { size: p.size, m: p.margins, inset: p.inset || [3, 3], draw };
      else if (p.kind === 'hero') { hero[id] = draw; heroSize[id] = p.size; }
      else if (p.kind === 'cell') barCells[id] = { size: p.size, gap: p.gap ?? 2, draw };
    }
    // the ok symbol: the lit claw marks' light turned to bone (the count of the whole)
    const on = barCells['bar.cell.on'];
    if (on) barCells['bar.cell.ok'] = { ...on, draw: (x, w, h) => { on.draw(x, w, h); x.globalCompositeOperation = 'color'; x.fillStyle = SS.bone; x.fillRect(0, 0, w, h); x.globalCompositeOperation = 'destination-in'; on.draw(x, w, h); x.globalCompositeOperation = 'source-over'; } };
  });
  return loading;
}

const api = {
  id: 'ssarith', name: 'Ssarith Hunt', SS, CC, kit, hero, cells: {}, barCells, live, icon, ICON_NAMES, emblem, ink, keyInk, font, fonts, load, hexA, glowStroke, rr, knob, heroSize,
  family: 'VT323, monospace',
  // The content inside a panel frame: clear of the bezel (28 px margins, the tube's own rim 4 px inside them).
  pad: [26, 24, 24, 24],
  // The name plate's live field (the nameplate's tube, 54-372 x 9-47), 8 px clear of its ends.
  plate: { x: 64, y: 37, max: 296, font: '400 26px VT323', col: SS.phos },
  // The battle's helm ring under the ship, in phosphor.
  ring: ['rgba(109,255,138,0)', 'rgba(109,255,138,0.14)', 'rgba(150,255,170,0.9)'],
  // The Hunt's own names for its ships and officers (used where the page shows the player's side as the Hunt).
  player: { faction: 'Ssarith Hunt', call: 'Hunter', trim: 0x6dff8a, hull: 0x4a5048,
    ships: { resolute: ['Sskarr', 'Stalker Vesh-Orr'], tallis: ['Hissath', 'Tracker Ssev'], meridian: ['Thrael', 'Blooded Karsh'] },
    ranks: ['Unblooded', 'Tracker', 'Stalker', 'Huntmaster'],
    officers: { o1: 'Ssev', o2: 'Varrak', o3: 'Thessil', o4: 'Kesh', o5: 'Orrath', o6: 'Sisk', o7: 'Grathul', o8: 'Vesh-Kar', o9: 'Ssorna', o10: 'Tikk', o11: 'Harsk', o12: 'Ossiv', o13: 'Rekh' } },
  link: SS.phos,
  // labels centred on the tube keys (the skin's --btn-justify: center); on a tab, in its tube (x 9-111 of 120, centred
  // since 2026-11-04, when the tally at its left came off), with 6 px of glass round the label
  keyCenter: true, tabField: [15, 15],
  // a hover name on the dark tube in phosphor, as the Hunt's readouts, never dark letters on the lit tube through its
  // scan lines (owner, 2026-11-04: "this text is hard to read")
  tip: ['button.idle', 'idle'],
  // icon tabs (the station and ship rails) on the square tube key, not the long tab tube, so the icon sits in the glass
  iconKeys: true,
  // panel tab icons at rest in bone, apart from the functional icons' phosphor; glyphs on a dark tube in phosphor
  tabIcon: SS.bone, glyphOnDark: SS.phos, mark: { hover: '#b8ffc8', ready: SS.phos, count: SS.bone },
  // the strips (strip.top and strip.bottom, 72 px rendered) drawn 64 px tall, their readouts on the flat middle
  stripTop: { h: 64, dy: 4 }, stripBottom: { h: 64, dy: 4 },
};
window.FCSkins = window.FCSkins || {}; window.FCSkins.ssarith = api;
return api;
})();
