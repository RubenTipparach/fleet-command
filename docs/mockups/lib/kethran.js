// Fleet Command mockups: the Kethran Hegemony skin (openspec/changes/console-skins/design.md, "Kethran Hegemony: modelled
// in Blender"; openspec/changes/kethran-battle-console), shared by the console skins page (docs/mockups/console-skins.html,
// whose Kethran entry takes its icons and live readouts from here) and the battle console, where it is the skin of the
// Kethran ship you command (docs/mockups/battle-console-kethran.html, written from battle-console.html by
// tools/mockups/skin_page.py). One implementation of the Hegemony's look (CLAUDE.md section 6.1): its palette, its icons,
// its live readouts and, for the battle console, its kit loaded from the Blender render (assets/ui/kethran/manifest.json,
// tools/blender/build_ui_kethran.py) with the few pieces only the console draws composed here from that render: the square
// key is the helm pad's own rendered keycap in its socket, the wide key the rendered button, each lit by state; the
// allocation cells, the title placard and the gunmetal backing plate behind the column are drawn in the kit's language.
// It exposes the same fields as docs/mockups/lib/concord.js and lib/kelvane.js, so the battle console swaps one for another
// and changes no rule and no layout. Needs docs/mockups/lib/kitdraw.js inlined first. Our own design: no other game's
// names, shapes, glyphs or art.
//
// Where it comes from (design.md; owner, 2026-10-30: "build the other design language using 3d models in blender like
// starcraft", "buttons should be more plain"): an empire of rank and conquest, whose machines are proud of their mass
// (the provisional account, survey U7). Armoured gunmetal plates bolted in layers, plain trapezoid wedge keycaps, ember
// lenses, red rank stripes. Nothing here comes from what the Kethran look like (CLAUDE.md section 1).
window.FCKethran = (function () {
'use strict';
const { rng, hash, paint, P } = window.FCDraw;
// The palette and what it stands for (design.md): iron, the empire's material and patience (gunmetal); mass and rank
// (iron); old wars (oxide); blood and command (signal red); the foundry's fire (ember); trophies of the defeated (bone).
const KC = { gun: '#2b2f33', iron: '#4a4f55', steel: '#8a9096', oxide: '#9e2a1e', red: '#d9483b', ember: '#ffb547', bone: '#d8cfbf', glass: '#121417' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
function poly(x, pts) { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); x.closePath(); }
// Relative luminance of a #rrggbb colour (WCAG), for picking the ink that reads on a fill.
function lum(hex) {
  if (typeof hex !== 'string' || hex[0] !== '#' || hex.length < 7) return 0;
  const c = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
}

// ---- The battle console's colour roles -------------------------------------------------------------------------
// The same role names as the Concord's (lib/concord.js), so the page's rules pick a role, never a colour. Readouts burn
// ember on the dark glass and over the battle (Sky), the foundry's deeper fire marks whole and engaged (Accord), a hot
// orange damaged (Caution), signal red down and alarms (Alert); labels in the worn tan of stencilled paint (Lilac), prose
// and titles in bone; a key at rest is plain steel (Signal). The shields' ramp is the skins page's: ember, orange, red.
const CC = {
  deep: '#24272b', signal: '#4a4f55', sky: '#ffc46a', accord: '#ffa83a', lilac: '#a99a86', caution: '#ff7a2a', alert: '#ff3b2e', text: KC.bone,
  ink: '#1a0d05', inkDis: '#5a5048', night: '#15171a', ghost: '#5a5f66', void: '#0d0e10', slate: '#33373c', mute: '#26282b', ver: '#8a8478',
  char: '#3a0b09', plate: '#15171a', bg: '#0b0c0d', title: KC.bone,
};
// The ink that reads on a fill: dark on the lit colours, bone stencil on steel and glass.
const ink = (fill) => (lum(fill) > 0.2 ? CC.ink : KC.bone);
// Ink on the rendered keycaps and tabs: bone stencil on the plain steel cap at rest and on hover, ember on the pressed cap
// sunk into its socket; ember on the chosen tab beside its lit lamp, worn tan on a tab at rest.
const keyInk = { idle: KC.bone, hover: KC.bone, pressed: KC.ember, tabOn: KC.ember, tabOff: '#a99a86' };
// Type: "Saira Stencil One" for headings, as the skins page's keys and titles, and "Saira Condensed" for the name plate and
// everything else (the skin's pair). The page asks for Antonio sizes. Saira Condensed runs 1.04 times as wide at the same
// size (measured on the console's own labels, 2026-11-07), so it is set at the size asked; weight 700 at 22 px and up is
// a heading: Saira Stencil One, 1.43 times as wide, at 0.78 of the size; rule T (the keys' and tabs' fit) shrinks what
// still runs long.
function font(spec) {
  const m = /^(\d+) ([\d.]+)px Antonio$/.exec(spec); if (!m) return spec;
  const w = +m[1], s = +m[2];
  if (w >= 700 && s >= 22) return `400 ${Math.round(s * 0.78 * 10) / 10}px "Saira Stencil One"`;
  return `${w >= 700 ? 700 : 600} ${s}px "Saira Condensed"`;
}
const fonts = ['400 20px "Saira Stencil One"', '600 20px "Saira Condensed"', '700 20px "Saira Condensed"'];

// ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ---------------------------------------
// Each a picture of the job in the Hegemony's hand: solid wedges, chevrons and blades, every corner cut, nothing round
// (from the prow: design.md). Solid for the thing, thinner bars for what it gives off; one ink, lit ember behind a lens
// when bright; on a 48 x 48 grid with 4 px clear, read at 20 px and 48 px.
function icon(name, col = CC.sky) {
  return paint(48, 48, (x) => {
    const bright = lum(col) > 0.2;
    x.fillStyle = col; x.strokeStyle = col; x.lineJoin = 'miter'; x.lineCap = 'butt'; x.lineWidth = 3;
    if (bright) { x.shadowColor = hexA(col, 0.7); x.shadowBlur = 4; }
    const F = (pts) => { poly(x, pts); x.fill(); };
    const S = (pts, closed = false, lw = 3) => { x.lineWidth = lw; x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); if (closed) x.closePath(); x.stroke(); };
    const ngon = (cx, cy, r, n, a0 = -P / 2) => Array.from({ length: n }, (_, i) => [cx + Math.cos(a0 + i * 2 * P / n) * r, cy + Math.sin(a0 + i * 2 * P / n) * r]);
    const cut = (fn) => { const sb = x.shadowBlur; x.shadowBlur = 0; x.globalCompositeOperation = 'destination-out'; fn(); x.globalCompositeOperation = 'source-over'; x.shadowBlur = sb; };
    ({
      // the helm: an eight-sided wheel of plate, a wedge needle on the heading
      helm: () => { S(ngon(24, 24, 18, 8, -P / 2 + P / 8), true, 3.6); F([[24, 6], [30, 26], [24, 22], [18, 26]]); F(ngon(24, 28, 4, 4)); },
      // power: a bolt cut from plate in a hexagon
      power: () => { S(ngon(24, 24, 20, 6), true, 2.8); F([[28, 6], [15, 26], [23, 26], [19, 42], [33, 20], [25, 20]]); },
      // tactical: a turret on a stepped base, its barrel out to the upper right
      tactical: () => { F([[5, 44], [43, 44], [39, 37], [9, 37]]); F([[11, 36], [37, 36], [33, 26], [15, 26]]); F([[24, 30], [40, 10], [44, 13], [28, 33]]); },
      // point defence: a chamfered shield with a ranging cross
      pd: () => { S([[8, 6], [40, 6], [40, 26], [24, 43], [8, 26]], true, 3); F([[22, 13], [26, 13], [26, 31], [22, 31]]); F([[15, 20], [33, 20], [33, 24], [15, 24]]); },
      // science: a scanning wedge over a cut lens
      science: () => { F([[24, 4], [44, 40], [4, 40]]); cut(() => F([[24, 16], [34, 34], [14, 34]])); F([[24, 24], [28, 31], [20, 31]]); },
      // comms: an emitter mast, three bars going out
      comms: () => { F([[4, 20], [14, 20], [14, 28], [4, 28]]); F([[18, 14], [26, 10], [26, 38], [18, 34]]); F([[30, 6], [44, 2], [44, 46], [30, 42]]); },
      // repair: a spanner cut from plate, its jaw open up and left
      repair: () => { F([[6, 6], [26, 6], [30, 14], [22, 20], [42, 40], [36, 46], [16, 26], [10, 30], [4, 20]]); },
      // transport: a figure in three bars, the lower dissolving
      transport: () => { F(ngon(24, 9, 5, 6)); F([[16, 29], [19, 17], [29, 17], [32, 29]]); for (const [y, w] of [[33, 9], [38, 6.5], [43, 4]]) F([[24 - w, y], [24 + w, y], [24 + w - 1.5, y + 2.6], [24 - w + 1.5, y + 2.6]]); },
      // fleet: three wedges in formation, the lead ahead
      fleet: () => { const sh = (cx, cy, k) => F([[cx, cy - 10 * k], [cx + 7 * k, cy + 8 * k], [cx, cy + 4 * k], [cx - 7 * k, cy + 8 * k]]); sh(24, 13, 1); sh(11, 33, 0.9); sh(37, 33, 0.9); },
      // the ship from above: an armoured wedge with a forward prow and swept blades
      ship: () => { F([[24, 3], [31, 20], [42, 38], [30, 34], [24, 45], [18, 34], [6, 38], [17, 20]]); },
      // shields: six plates round a hull
      shields: () => { for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3, c = Math.cos(a), s = Math.sin(a), q = Math.cos(a + 0.42), r = Math.sin(a + 0.42), u = Math.cos(a - 0.42), v = Math.sin(a - 0.42);
        F([[24 + u * 16, 24 + v * 16], [24 + q * 16, 24 + r * 16], [24 + q * 21, 24 + r * 21], [24 + c * 22, 24 + s * 22], [24 + u * 21, 24 + v * 21]]); } F([[24, 16], [29, 28], [24, 32], [19, 28]]); },
      // sensors: an octagonal scope, a sweep and two contacts
      sensors: () => { S(ngon(24, 24, 19, 8, -P / 2 + P / 8), true, 2.8); S([[24, 24], [37, 11]], false, 2.8); F(ngon(31, 31, 3.4, 4)); F(ngon(14, 17, 3.4, 4)); },
      // tractor: two jaws closing on a held block
      tractor: () => { F([[4, 8], [20, 24], [4, 40], [10, 24]]); F([[44, 8], [28, 24], [44, 40], [38, 24]]); F([[20, 21], [28, 21], [28, 27], [20, 27]]); },
      // craft: a small swept blade
      craft: () => { F([[24, 4], [29, 22], [44, 32], [28, 31], [24, 44], [20, 31], [4, 32], [19, 22]]); },
      // weapons: a blade of fire from an emitter to a strike
      weapons: () => { F([[5, 37], [12, 37], [12, 44], [5, 44]]); F([[11, 34], [31, 14], [34, 17], [14, 37]]); F([[36, 4], [38, 11], [44, 12], [39, 16], [41, 23], [35, 18], [29, 22], [31, 15], [26, 11], [33, 10]]); },
      // warp: a chevron trailing speed bars
      warp: () => { F([[20, 8], [28, 8], [42, 24], [28, 40], [20, 40], [34, 24]]); for (const [y, w] of [[15, 12], [23, 16], [31, 12]]) F([[4, y], [4 + w, y], [4 + w, y + 3], [4, y + 3]]); },
      // engines: a cut nozzle, the exhaust in wedges
      engines: () => { F([[12, 4], [36, 4], [40, 20], [8, 20]]); F([[14, 25], [34, 25], [30, 30], [18, 30]]); F([[18, 34], [30, 34], [27, 39], [21, 39]]); F([[21, 42], [27, 42], [24, 46]]); },
      // reactor: a core of plate in two crossed rings
      reactor: () => { F(ngon(24, 24, 6.5, 6)); x.lineWidth = 2.6; for (const r of [P / 4, -P / 4]) { x.beginPath(); x.ellipse(24, 24, 19, 8, r, 0, 2 * P); x.stroke(); } },
      // map: a course of wedges to a mark
      map: () => { for (const [px, py] of [[8, 41], [12, 33], [14, 25], [20, 19]]) F(ngon(px, py, 3, 4)); F([[28, 5], [32, 5], [35, 9], [38, 5], [42, 5], [37, 12], [42, 19], [38, 19], [35, 15], [32, 19], [28, 19], [33, 12]]); },
      pause: () => { F([[13, 9], [21, 9], [21, 39], [13, 39]]); F([[27, 9], [35, 9], [35, 39], [27, 39]]); },
      menu: () => { for (const y of [10, 21, 32]) F([[8, y], [40, y], [37, y + 6], [11, y + 6]]); },
      // fire: four cut brackets closing on a solid wedge
      fire: () => { for (const [a, b, c] of [[[7, 18], [7, 7], [18, 7]], [[30, 7], [41, 7], [41, 18]], [[41, 30], [41, 41], [30, 41]], [[18, 41], [7, 41], [7, 30]]]) S([a, b, c], false, 3.6); F(ngon(24, 24, 6, 4)); },
      cargo: () => { for (const [cx, cy] of [[15, 15], [33, 15], [15, 33], [33, 33]]) S([[cx - 7, cy - 5], [cx - 5, cy - 7], [cx + 7, cy - 7], [cx + 7, cy + 5], [cx + 5, cy + 7], [cx - 7, cy + 7]], true, 2.8); },
      // settings: an eight-toothed cog of plate with a square bore
      settings: () => { const o = []; for (let i = 0; i < 16; i++) { const a = P / 16 + i * P / 8, r = i % 2 ? 14 : 20; o.push([24 + Math.cos(a) * r, 24 + Math.sin(a) * r]); } F(o); cut(() => F([[19, 19], [29, 19], [29, 29], [19, 29]])); },
    })[name]();
  });
}
const ICON_NAMES = ['helm', 'power', 'tactical', 'pd', 'science', 'comms', 'repair', 'transport', 'fleet', 'ship', 'shields', 'sensors', 'tractor', 'craft', 'weapons', 'warp', 'engines', 'reactor', 'map', 'pause', 'menu', 'fire', 'cargo', 'settings'];

// ---- Live readouts (the console skins page and the battle console) ---------------------------------------------
const live = {
  arrow: (x) => { x.fillStyle = KC.bone; x.shadowColor = '#000'; x.shadowBlur = 3; x.beginPath(); x.moveTo(11, 0); x.lineTo(-6, -10); x.lineTo(-2, 0); x.lineTo(-6, 10); x.closePath(); x.fill(); x.shadowBlur = 0; },
  ssd: (x, cx, cy, sh, col) => {
    x.save(); x.shadowColor = KC.ember; x.shadowBlur = 6;
    for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.fillStyle = col(sh[i]); x.save(); x.translate(cx, cy); x.rotate(a); x.beginPath(); x.moveTo(96, -40); x.lineTo(110, -46); x.lineTo(110, 46); x.lineTo(96, 40); x.closePath(); x.fill(); x.restore(); }
    x.strokeStyle = KC.ember; x.lineWidth = 2.5; x.beginPath(); x.moveTo(cx, cy - 74); x.lineTo(cx + 42, cy + 40); x.lineTo(cx + 18, cy + 30); x.lineTo(cx, cy + 66); x.lineTo(cx - 18, cy + 30); x.lineTo(cx - 42, cy + 40); x.closePath(); x.stroke();
    x.beginPath(); x.moveTo(cx, cy - 40); x.lineTo(cx, cy + 40); x.moveTo(cx - 20, cy); x.lineTo(cx + 20, cy); x.stroke();
    x.fillStyle = KC.red; for (const [mx, my] of [[cx, cy - 60], [cx - 30, cy + 26], [cx + 30, cy + 26]]) { x.beginPath(); x.moveTo(mx, my - 7); x.lineTo(mx + 6, my + 5); x.lineTo(mx - 6, my + 5); x.closePath(); x.fill(); }
    x.restore();
  },
  dial: (x, c, cts) => {
    x.save(); x.strokeStyle = KC.ember; x.lineWidth = 3; x.shadowColor = KC.ember; x.shadowBlur = 6; x.beginPath(); x.moveTo(c, c); x.lineTo(c + 24, c - 14); x.stroke();
    for (const [dx, dy] of cts) { const px = c + dx * 24, py = c + dy * 24; x.fillStyle = KC.red; x.beginPath(); x.moveTo(px, py - 5); x.lineTo(px + 5, py + 4); x.lineTo(px - 5, py + 4); x.fill(); }
    x.restore();
  },
  // The target's brackets: ember corner plates, stepped like the frame's own corners.
  bracket: (x, bx, by, bw, bh) => { x.save(); x.fillStyle = KC.ember; x.shadowColor = KC.ember; x.shadowBlur = 10; for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) { x.beginPath(); x.moveTo(px, py); x.lineTo(px + sx * 46, py); x.lineTo(px + sx * 30, py + sy * 10); x.lineTo(px + sx * 10, py + sy * 10); x.lineTo(px + sx * 10, py + sy * 30); x.lineTo(px, py + sy * 46); x.closePath(); x.fill(); } x.restore(); },
};

// ---- The kit ------------------------------------------------------------------------------------------------------
// Every piece of the skins page's kit is rendered in Blender (tools/blender/build_ui_kethran.py) and comes from the inlined
// manifest; load() decodes them once. The battle console also draws square and wide keys, allocation cells, title
// placards and a ground behind its column, which the render does not have, so they are composed here from the render
// itself and drawn in its language:
//  - key.*: the helm pad's top-left keycap in its black socket (helm.pad.png, 144 x 144 image px from (52, 84)), its plain
//    steel cap lit by state through the cap's own shading: steel at rest, lighter steel on hover, ember engaged and whole,
//    hot orange damaged, signal red down, dark steel off;
//  - keyw.*: the rendered wedge key (button.idle, .hover, .pressed) and the idle key's cap lit the same way;
//  - cell.*: a small wedge lens in a gunmetal bezel; plate.title: a gunmetal placard with a red rank stripe;
//  - console.backing: a gunmetal plate, bolted, with seams, a conduit down each side and red and black hazard notches
//    (CLAUDE.md section 7: no black behind a metallic skin's panels).
const MANIFEST = 'assets/ui/kethran/manifest.json';
const IMG = {}, kit = {}, hero = {}, heroSize = {}, cells = {};
// the keycap's face, in the 144 x 144 crop (image px), and in the 160 x 56 wedge key (reference px)
const CAP = [[15, 17], [129, 17], [110, 123], [35, 123]], WCAP = [[11, 8], [149, 8], [140, 48], [19, 48]];
const LIGHT = { on: CC.accord, ok: CC.accord, warn: CC.caution, alert: CC.alert };
// Light a cap: its face filled with the lamp colour, the render's own shading laid back over it (soft light), a glow from
// the cap's top edge; hover lightens the steel; off darkens it.
function lightCap(x, face, state, drawSrc) {
  x.save(); poly(x, face); x.clip();
  if (LIGHT[state]) {
    x.fillStyle = LIGHT[state]; x.fill();
    x.globalCompositeOperation = 'soft-light'; x.globalAlpha = 0.9; drawSrc(); x.globalAlpha = 1;
    x.globalCompositeOperation = 'screen'; const ys = face.map((p) => p[1]), y0 = Math.min(...ys), y1 = Math.max(...ys), gr = x.createLinearGradient(0, y0, 0, y1);
    gr.addColorStop(0, 'rgba(255,240,200,0.45)'); gr.addColorStop(0.35, 'rgba(255,200,120,0.08)'); gr.addColorStop(1, 'rgba(0,0,0,0)'); x.fillStyle = gr; x.fill();
  } else if (state === 'hover') { x.globalCompositeOperation = 'screen'; x.fillStyle = 'rgba(80,86,94,0.75)'; x.fill(); }
  else if (state === 'off') { x.globalCompositeOperation = 'multiply'; x.fillStyle = '#5a5a5a'; x.fill(); }
  x.restore();
}
function keyPaint(state) {
  return (x, w, h) => {
    const k = w / 144, src = () => x.drawImage(IMG['helm.pad'], 52, 84, 144, 144, 0, 0, w, h);
    src(); lightCap(x, CAP.map(([a, b]) => [a * k, b * h / 144]), state, src);
  };
}
function keywPaint(state) {
  const piece = state === 'hover' ? 'button.hover' : state === 'off' ? 'button.pressed' : 'button.idle';
  return (x, w, h) => { const src = () => x.drawImage(IMG[piece], 0, 0, w, h); src(); lightCap(x, WCAP.map(([a, b]) => [a * w / 160, b * h / 56]), state, src); };
}
// A cut-cornered gunmetal box (the bezel of a lens, a placard), lit from the top left.
function plate(x, X, Y, W, H, c, top = '#4e535a', bot = '#2a2d31') {
  const pts = [[X + c, Y], [X + W - c, Y], [X + W, Y + c], [X + W, Y + H - c], [X + W - c, Y + H], [X + c, Y + H], [X, Y + H - c], [X, Y + c]];
  const g = x.createLinearGradient(0, Y, 0, Y + H); g.addColorStop(0, top); g.addColorStop(1, bot); poly(x, pts); x.fillStyle = g; x.fill();
  x.strokeStyle = 'rgba(0,0,0,0.85)'; x.lineWidth = 1.2; x.stroke();
  x.beginPath(); x.moveTo(X + 1, Y + H - c); x.lineTo(X + 1, Y + c); x.lineTo(X + c, Y + 1); x.lineTo(X + W - c, Y + 1); x.strokeStyle = 'rgba(200,210,220,0.35)'; x.lineWidth = 1; x.stroke();
  return pts;
}
function cellPaint(col, locked) {
  return (x, w, h) => {
    plate(x, 0.5, 0.5, w - 1, h - 1, 5);
    const lens = [[6, 5], [w - 6, 5], [w - 9, h - 5], [9, h - 5]];
    poly(x, lens); x.fillStyle = locked ? '#0c0d0f' : col ? col : KC.glass; x.fill();
    if (col && !locked) { x.save(); poly(x, lens); x.clip(); const g = x.createLinearGradient(0, 5, 0, h - 5); g.addColorStop(0, 'rgba(255,245,220,0.5)'); g.addColorStop(0.4, 'rgba(255,255,255,0)'); g.addColorStop(1, 'rgba(0,0,0,0.25)'); x.fillStyle = g; x.fillRect(0, 0, w, h); x.restore(); }
    if (locked) { x.save(); poly(x, lens); x.clip(); x.strokeStyle = 'rgba(217,72,59,0.3)'; x.lineWidth = 2; for (let i = -h; i < w; i += 6) { x.beginPath(); x.moveTo(i, h); x.lineTo(i + h, 0); x.stroke(); } x.restore(); }
    poly(x, lens); x.strokeStyle = 'rgba(0,0,0,0.9)'; x.lineWidth = 1.2; x.stroke();
  };
}
// The title placard: a gunmetal plate with chamfered ends, a red rank stripe and a lit ember pip at its left end.
function placard(x, w, h) {
  plate(x, 0.5, 0.5, w - 1, h - 1, 6, '#3a3e44', '#1c1f22');
  x.fillStyle = KC.red; poly(x, [[6, 3], [14, 3], [10, h - 3], [2, h - 3]]); x.fill();
  x.fillStyle = KC.ember; x.shadowColor = KC.ember; x.shadowBlur = 4; x.fillRect(w - 10, h / 2 - 2, 4, 4); x.shadowBlur = 0;
}
// The ground behind the column: one gunmetal plate, its sheets bolted on in a stepped layout with seams, a conduit down each
// side, hazard notches top and bottom, the grain from a seeded generator.
function backing(x, w, h) {
  const R = rng(hash('kethran.backing'));
  const g = x.createLinearGradient(0, 0, w, h); g.addColorStop(0, '#30343a'); g.addColorStop(1, '#1d2023'); x.fillStyle = g; x.fillRect(0, 0, w, h);
  for (let i = 0; i < w * h / 40; i++) { x.fillStyle = R() < 0.5 ? 'rgba(0,0,0,0.12)' : 'rgba(255,255,255,0.05)'; x.fillRect(R() * w, R() * h, 1.2, 1.2); }
  // stepped sheets
  let y = 0; while (y < h) { const sh = 140 + Math.floor(R() * 120); const inset = 18 + Math.floor(R() * 3) * 6;
    plate(x, 10 + (R() < 0.5 ? 0 : 4), y + 6, w - 20 - inset, Math.min(sh, h - y) - 12, 14, '#373b41', '#24272b');
    for (const [bx, by] of [[22, y + 18], [w - inset - 22, y + 18], [22, y + Math.min(sh, h - y) - 18], [w - inset - 22, y + Math.min(sh, h - y) - 18]]) {
      x.fillStyle = '#121416'; x.beginPath(); x.arc(bx, by, 4.2, 0, 2 * P); x.fill(); x.fillStyle = '#6a7078'; x.beginPath(); x.arc(bx - 0.6, by - 0.6, 2.6, 0, 2 * P); x.fill(); }
    y += sh; }
  // conduits down the edges
  for (const cx of [4, w - 12]) { const gg = x.createLinearGradient(cx, 0, cx + 8, 0); gg.addColorStop(0, '#15171a'); gg.addColorStop(0.5, '#4a4f55'); gg.addColorStop(1, '#15171a'); x.fillStyle = gg; x.fillRect(cx, 0, 8, h);
    for (let yy = 30; yy < h; yy += 120) { x.fillStyle = '#2b2f33'; x.fillRect(cx - 2, yy, 12, 10); x.fillStyle = KC.oxide; x.fillRect(cx - 2, yy + 3, 12, 3); } }
  // hazard notches, red and black (design.md)
  for (const yy of [0, h - 14]) { x.save(); x.beginPath(); x.rect(40, yy, w - 80, 14); x.clip(); x.fillStyle = '#0d0e10'; x.fillRect(40, yy, w - 80, 14);
    x.fillStyle = KC.oxide; for (let i = 30; i < w; i += 22) { poly(x, [[i, yy], [i + 10, yy], [i - 4, yy + 14], [i - 14, yy + 14]]); x.fill(); } x.restore(); }
}
let loading = null;
function load() {
  if (loading) return loading;
  const n = document.querySelector(`script[data-src="${MANIFEST}"]`);
  if (!n) return (loading = Promise.reject(new Error(`the Kethran kit (${MANIFEST}) is not inlined in this page`)));
  const d = JSON.parse(n.textContent);
  loading = Promise.all(Object.entries(d.pieces).map(([id, p]) => new Promise((ok, bad) => { const im = new Image(); im.onload = () => { IMG[id] = im; ok(); }; im.onerror = () => bad(new Error(`Kethran piece ${id} failed to decode`)); im.src = p.data; }))).then(() => {
    for (const [id, p] of Object.entries(d.pieces)) {
      const draw = (x, w, h) => x.drawImage(IMG[id], 0, 0, w, h);
      if (p.kind === 'nine') kit[id] = { size: p.size, m: p.margins, inset: p.inset || [3, 3], draw };
      else if (p.kind === 'hero') { hero[id] = draw; heroSize[id] = p.size; }
    }
    for (const s of ['idle', 'hover', 'on', 'ok', 'warn', 'alert', 'off']) {
      hero['key.' + s] = keyPaint(s); heroSize['key.' + s] = [48, 48];
      kit['keyw.' + s] = { size: [144, 48], m: [0, 24, 0, 24], draw: keywPaint(s) };
    }
    hero['console.backing'] = backing; heroSize['console.backing'] = [480, 1080];
    // the bars keep the rendered ember fill (design.md: the Hegemony keeps the plain bar); whole is the same fire
    kit['bar.fill.accord'] = kit['bar.fill'];
    Object.assign(cells, { 'cell.idle': { size: [32, 28], m: [10, 10, 10, 10], draw: cellPaint(null) }, 'cell.lit': { size: [32, 28], m: [10, 10, 10, 10], draw: cellPaint(CC.sky) },
      'cell.warn': { size: [32, 28], m: [10, 10, 10, 10], draw: cellPaint(CC.alert) }, 'cell.accord': { size: [32, 28], m: [10, 10, 10, 10], draw: cellPaint(CC.accord) },
      'cell.locked': { size: [32, 28], m: [10, 10, 10, 10], draw: cellPaint(null, true) },
      'plate.title': { size: [140, 24], m: [0, 18, 0, 18], draw: placard } });
  });
  return loading;
}
// The Hegemony's crest wherever the console draws one (a portrait, the holder of a ship): the rendered crest, r its radius.
function emblem(G, cx, cy, r) { if (IMG.crest) G.drawImage(IMG.crest, cx - r, cy - r, 2 * r, 2 * r); }
// Drawn keys and knobs (the slider handles, the expand key, the tug bar): every corner cut at 45 degrees, never round.
function rr(G, X, Y, W, H, r) { G.beginPath(); if (!(W > 0 && H > 0)) { G.rect(X, Y, W, H); return; } const c = Math.min(Array.isArray(r) ? Math.max(...r) : (+r || 0), W / 2, H / 2) * 0.7;
  G.moveTo(X + c, Y); G.lineTo(X + W - c, Y); G.lineTo(X + W, Y + c); G.lineTo(X + W, Y + H - c); G.lineTo(X + W - c, Y + H); G.lineTo(X + c, Y + H); G.lineTo(X, Y + H - c); G.lineTo(X, Y + c); G.closePath(); }
function knob(G, cx, cy, r) { G.beginPath(); for (let i = 0; i < 8; i++) { const a = P / 8 + i * P / 4; G.lineTo(cx + Math.cos(a) * r, cy + Math.sin(a) * r); } G.closePath(); }

const api = {
  id: 'kethran', name: 'Kethran Hegemony', KC, CC, kit, hero, heroSize, cells, barCells: null, live, icon, ICON_NAMES, emblem, ink, keyInk, font, fonts, load, hexA, rr, knob,
  family: '"Saira Condensed", sans-serif',
  // The content inside a panel: clear of the bolted bezel, 4 px into the recessed glass (frame.panel's well, 22 px in).
  pad: [28, 26, 26, 26],
  // The name plate's live field: the dark window between the chevron block and the bolt box (84-421 x 10-46), clear of the
  // Fleet key at 370.
  plate: { x: 100, y: 36, max: 262, font: '700 18px "Saira Condensed"', col: KC.bone },
  // The battle's helm ring under the ship, in ember.
  ring: ['rgba(255,181,71,0)', 'rgba(255,181,71,0.14)', 'rgba(255,210,140,0.9)'],
  // Names for the player's side where the page shows it as the Hegemony: ships named as verdicts and deeds, captains by
  // their rank (the scenario's own Kethran are Warlords and Blades). Placeholders until the owner sets the Hegemony's
  // culture (CLAUDE.md section 1; openspec/changes/kethran-battle-console, open question H1).
  player: { faction: 'Kethran Hegemony', call: 'Warlord', trim: 0xd9483b, hull: 0x8c6a64,
    ships: { resolute: ['Iron Verdict', 'Warlord Haddan'], tallis: ['Sentence', 'Commander Ruhl'], meridian: ['Unbowed', 'Warlord Kessa'] },
    ranks: ['Blade', 'Spear', 'Commander', 'Warlord'],
    officers: { o1: 'Garan Dov', o2: 'Ruhl Askeran', o3: 'Mesk Tavor', o4: 'Ira Kosk', o5: 'Dendar Vul', o6: 'Saska Oren', o7: 'Torvek Han',
      o8: 'Brenna Kol', o9: 'Hask Ardun', o10: 'Velka Drom', o11: 'Orsk Malen', o12: 'Tamar Gesh', o13: 'Kaddo Ren' } },
  link: KC.ember,
  // Labels centred on the wedge keycap's plain face (between its slanted sides, 22 px in from each end at 56 px) and on
  // a tab clear of its lamp at the left (18 px) and its cut corner at the right (10 px).
  keyCenter: true, keyField: [22, 22], tabField: [18, 10],
  // A hover name on the pressed key, in ember (the console's default).
  // Icon tabs (the station and ship rails) on the square keycap, the icon on its face.
  iconKeys: true,
  // Panel tab icons at rest in bone, apart from the functional icons' ember; glyphs on a steel cap in bone.
  tabIcon: KC.bone, glyphOnDark: KC.bone, mark: { hover: '#ffd89a', ready: CC.accord, count: KC.bone },
  // The strips (strip.top and strip.bottom, 72 px rendered) at their full height; the content, laid out for 56 px, moved
  // down into the channel between the conduit rail and the lip (rows 17-59 of the top, 16-58 of the bottom).
  stripTop: { h: 72, dy: 10 }, stripBottom: { h: 72, dy: 9 },
  // A panel title's baseline 2 px higher, centred on the placard.
  titleDy: 2,
};
window.FCSkins = window.FCSkins || {}; window.FCSkins.kethran = api;
return api;
})();
