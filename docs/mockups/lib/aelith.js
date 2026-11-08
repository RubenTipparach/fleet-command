// Fleet Command mockups: the Aelith Conclave skin (openspec/changes/console-skins/design.md, "Aelith Conclave";
// openspec/changes/aelith-battle-console), shared by the console skins page (docs/mockups/skins/aelith.js registers it
// there) and the battle console, where it is the skin of every Conclave ship you command (the console follows the ship
// you are on; openspec/changes/fleet-skins). One implementation of the Conclave's look (CLAUDE.md section 6.1): its
// palette, its holograms, its icons, its live readouts and, for the battle console, its kit: the gold and pearl shells
// rendered in Blender (assets/ui/aelith/manifest.json, tools/blender/build_ui_aelith.py from the fit-checked shapes of
// tools/mockups/aelith_shapes.py) with the holograms drawn as vectors into their wells from the same shapes.
// It exposes the same fields as docs/mockups/lib/concord.js and lib/kelvane.js, so the battle console swaps one for
// another and changes no rule and no layout. Needs docs/mockups/lib/kitdraw.js inlined first. Our own design: no other
// game's or film's names, shapes, glyphs or art.
//
// Where it comes from (design.md; owner, 2026-10-30: "an wise and advanced alien race. holographic buttons, with organic
// shaped metal armor peices surrounding the edges"; "the conclave is one exception. They have a hybrid of both vector
// graphics for holographic buttons and icons, but 3d rendered shell around the UI panels and elements"). Its tradition is
// patient knowledge kept over aeons: light shaped by thought, held in sculpted shells of satin gold and pearl. The
// palette: pearl (wisdom, the slow layering of ages), gold (permanence), void (serenity), holo (thought made visible),
// violet (the psionic), coral (a rare disturbance). Nothing here comes from what the Aelith look like (CLAUDE.md
// section 1): the shells, lozenges and sacred geometry are their makers' tradition, not their bodies.
window.FCAelith = (function () {
'use strict';
const { paint, png, P } = window.FCDraw;
const AE = { pearl: '#e8e4da', gold: '#c9a646', goldHi: '#f6e2a0', goldLo: '#6a4a14', void: '#0b2a33', deep: '#04141a', holo: '#6ff3ff', holoHi: '#dffcff',
  violet: '#9a8cff', coral: '#ff6a5a', amber: '#ffd27a', mint: '#8affc8', dim: '#9fc9cf', bg: '#03111a' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
// Relative luminance of a #rrggbb colour (WCAG), for picking the ink that reads on a fill.
function lum(hex) {
  if (typeof hex !== 'string' || hex[0] !== '#' || hex.length < 7) return 0;
  const c = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
}
function path(x, pts, closed = true) { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) { if (p.length === 4) x.quadraticCurveTo(p[0], p[1], p[2], p[3]); else x.lineTo(p[0], p[1]); } if (closed) x.closePath(); }
const bounds = (pts) => { const xs = pts.map((p) => p[0]), ys = pts.map((p) => p[1]); return [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)]; };

// ---- Holograms ---------------------------------------------------------------------------------------------------
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
// A lit hologram, for a key that is on or shows a state: light made nearly solid in the colour, brightest at its heart,
// its scanlines still running, so dark ink reads on it (the battle console's lit keys and cells).
function solid(x, draw, box, col) {
  const [x0, y0, x1, y1] = box, cx = (x0 + x1) / 2, cy = (y0 + y1) / 2;
  x.save(); draw(); x.clip();
  const g = x.createRadialGradient(cx, cy, 0, cx, cy, Math.max(x1 - x0, y1 - y0) * 0.62);
  g.addColorStop(0, hexA(col, 0.98)); g.addColorStop(0.7, hexA(col, 0.88)); g.addColorStop(1, hexA(col, 0.7));
  x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.fillStyle = 'rgba(255,255,255,0.12)'; for (let y = y0; y < y1; y += 3) x.fillRect(x0, y, x1 - x0, 1);
  x.restore();
  x.save(); x.shadowColor = col; x.shadowBlur = 8; draw(); x.strokeStyle = hexA(AE.holoHi, 0.9); x.lineWidth = 1.2; x.stroke(); x.restore();
}
const shape = (x, pts) => () => path(x, pts);
const field = (x, pts, level, col) => holo(x, shape(x, pts), bounds(pts), level, col);
// A pressed key's hologram is pushed in while its gold shell stays put (owner, 2026-10-31: "I actually prefer the more
// animated versions on click"): it drops PRESS px and draws back into the void (a touch smaller), so its middle moves
// with the label (2 x --btn-sink = 3 px) and it stays inside its well (the key has 2 px of void round it; pushed in,
// 0.46 px is left at its foot).
const PRESS = 2.5;
function pushIn(x, pts) { const [x0, y0, x1, y1] = bounds(pts), cx = (x0 + x1) / 2, cy = (y0 + y1) / 2; x.translate(cx, cy + PRESS); x.scale(0.97, 0.92); x.translate(-cx, -cy); }
// A key's sheen when pressed: a soft bar of light across its middle.
function sheen(x, pts) { const [x0, y0, x1, y1] = bounds(pts); const g = x.createLinearGradient(0, y0, 0, y1); g.addColorStop(0, 'rgba(255,255,255,0)'); g.addColorStop(0.5, 'rgba(240,255,255,0.4)'); g.addColorStop(1, 'rgba(255,255,255,0)'); x.save(); path(x, pts); x.clip(); x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0); x.restore(); }
// A veil over a well: the field the console's readouts are drawn on, its void darkened so light letters read on it
// (cyan on the bare lit void measured about 3 to 1; on the veil 8 to 1), the rim light still showing at its edge. No
// scanlines: a frame or a strip is stretched many times its height on the console, and drawn lines would stretch with it.
function veil(x, pts, a = 0.72) {
  const [x0, y0, x1, y1] = bounds(pts);
  x.save(); path(x, pts); x.clip();
  const g = x.createLinearGradient(0, y0, 0, y1); g.addColorStop(0, hexA(AE.deep, a * 0.85)); g.addColorStop(0.5, hexA(AE.deep, a)); g.addColorStop(1, hexA(AE.deep, a * 0.9));
  x.fillStyle = g; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  x.restore();
}
// A hologram's edge alone, its glow and split-colour rim with no field or lines inside (the veiled wells').
function edge(x, pts, level = 0.3) {
  x.save(); x.translate(-0.8, 0); path(x, pts); x.strokeStyle = 'rgba(255,120,200,0.3)'; x.lineWidth = 1; x.stroke(); x.restore();
  x.save(); x.translate(0.8, 0); path(x, pts); x.strokeStyle = 'rgba(120,140,255,0.3)'; x.lineWidth = 1; x.stroke(); x.restore();
  x.save(); x.shadowColor = AE.holo; x.shadowBlur = 4 + level * 8; path(x, pts); x.strokeStyle = hexA(AE.holo, 0.5 + level * 0.4); x.lineWidth = 1.4; x.stroke(); x.restore();
}

// ---- The holograms in each piece's wells (role names from shapes.json) ---------------------------------------------
const HOLO = {
  'frame.panel': (x, s) => field(x, s.wells[0], 0.1),
  'frame.inset': (x, s) => field(x, s.wells[0], 0.08),
  'button.idle': (x, s) => field(x, s.holo.key, 0.15),
  'button.hover': (x, s) => field(x, s.holo.key, 0.6),
  'button.pressed': (x, s) => { x.save(); pushIn(x, s.holo.key); field(x, s.holo.key, 1); sheen(x, s.holo.key); x.restore(); },
  'tab.idle': (x, s) => field(x, s.holo.tab, 0.08),
  'tab.active': (x, s) => field(x, s.holo.tab, 0.75),
  'strip.top': (x, s) => field(x, s.holo.edge, 0.06),
  'strip.bottom': (x, s) => field(x, s.holo.edge, 0.06),
  'divider': (x, s) => field(x, s.holo.line, 0.6),
  'nameplate': (x, s) => field(x, s.holo.field, 0.15),
  'helm.pad': (x, s) => { for (let i = 0; i < 8; i++) field(x, s.holo['key ' + i], 0.28); field(x, s.holo.crystal, 0.7, AE.violet); },
  'ssd.bezel': (x, s) => field(x, s.wells[0], 0.08),
  'dial': (x, s) => field(x, s.wells[0], 0.1),
  'lamp.alert.green': (x, s) => gem(x, s, '#6fffb0'), 'lamp.alert.yellow': (x, s) => gem(x, s, AE.amber), 'lamp.alert.red': (x, s) => gem(x, s, AE.coral),
  'crest': (x, s) => { field(x, s.holo.triangle, 0.5); field(x, s.holo.crystal, 1, AE.violet); },
};
// The battle console's own holograms where they differ (it draws its readouts in light over a panel's middle, an inset,
// the strips' channels and the name plate's window): the same fields over a veil, so its letters read.
const HOLO_CONSOLE = {
  'frame.panel': (x, s) => { veil(x, s.wells[0]); edge(x, s.wells[0], 0.2); },
  'frame.inset': (x, s) => { veil(x, s.wells[0], 0.6); edge(x, s.wells[0], 0.2); },
  'strip.top': (x, s) => { veil(x, s.holo.edge, 0.55); edge(x, s.holo.edge, 0.2); },
  'strip.bottom': (x, s) => { veil(x, s.holo.edge, 0.55); edge(x, s.holo.edge, 0.2); },
  'nameplate': (x, s) => { veil(x, s.holo.field, 0.5); field(x, s.holo.field, 0.12); },
  'dial': (x, s) => { veil(x, s.wells[0], 0.6); field(x, s.wells[0], 0.06); },
};
// An alert lamp: a crystal of coloured light standing in its gold setting.
function gem(x, s, col) {
  const pts = s.holo.crystal, [x0, y0, x1, y1] = bounds(pts), c = (x0 + x1) / 2;
  x.save(); x.shadowColor = col; x.shadowBlur = 12; field(x, pts, 1, col); x.restore();
  x.strokeStyle = 'rgba(255,255,255,0.6)'; x.lineWidth = 1; x.beginPath(); x.moveTo(c, y0 + 2); x.lineTo(c, y1 - 2); x.stroke();
}
// The two plain fills ride inside the track's channel, so they are light alone (no shell).
const FILLS = { 'bar.fill': AE.holo, 'bar.fill.red': AE.coral, 'bar.fill.accord': AE.mint };
function fillPiece(col) { return paint(64, 10, (x) => holo(x, () => { x.beginPath(); x.roundRect(1, 1, 62, 8, 4); }, [0, 0, 64, 10], 0.9, col)); }
// The symbol bar: a row of small lozenges of light, the Conclave key's own shape (owner, 2026-10-31: "have different
// repeated symbols for them"). Unlit, a faint outline in the same scanlines; lit, the lozenge fills with light.
const CELL_SYMBOLS = [['bar.cell.off', 0.05, AE.holo], ['bar.cell.on', 0.95, AE.holo], ['bar.cell.warn', 0.95, AE.coral], ['bar.cell.ok', 0.95, AE.mint]];
function lozenge(x, level, col) {
  const pts = [[4, 0.6], [7.4, 6], [4, 11.4], [0.6, 6]];
  holo(x, () => path(x, pts), [0, 0, 8, 12], level, col);
  if (level > 0.5) {   // lit: the lozenge fills with light, brightest at its heart
    const g = x.createRadialGradient(4, 6, 0.5, 4, 6, 6); g.addColorStop(0, '#ffffff'); g.addColorStop(0.35, AE.holoHi); g.addColorStop(1, hexA(col, 0.7));
    x.save(); path(x, pts); x.clip(); x.fillStyle = g; x.fillRect(0, 0, 8, 12); x.restore();
  }
  x.save(); x.shadowColor = col; x.shadowBlur = level > 0.5 ? 4 : 0; x.strokeStyle = hexA(col, 0.35 + level * 0.6); x.lineWidth = 0.9; path(x, pts); x.stroke(); x.restore();
}

// The hologram of one piece, as a canvas of the piece's size (2x), clipped to its wells: what the console skins page
// layers over each rendered shell, and the battle console composes with it (console: the console's own field where it
// has one).
function holoCanvas(name, size, raw, console) {
  const draw = (console && HOLO_CONSOLE[name]) || HOLO[name];
  if (!draw || !raw) return null;
  const s = { wells: raw.wells, holo: Object.fromEntries(raw.holo) };
  return paint(size[0], size[1], (x) => { x.save(); x.beginPath(); for (const wl of s.wells) { x.moveTo(wl[0][0], wl[0][1]); for (const q of wl.slice(1)) x.lineTo(q[0], q[1]); x.closePath(); } x.clip(); draw(x, s); x.restore(); });
}
// One kit image for the skins page: the rendered shell and its hologram layered in one SVG, so the page's 9-slice and
// hero slots take it like any other kit image without decoding the render first.
function layered(name, p, raw) {
  const c = holoCanvas(name, p.size, raw, false);
  if (!c) return p.data;
  const W = p.size[0] * 2, H = p.size[1] * 2;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}"><image width="${W}" height="${H}" href="${p.data}"/><image width="${W}" height="${H}" href="${png(c)}"/></svg>`;
  return 'data:image/svg+xml;base64,' + btoa(svg);
}

// ---- The battle console's colour roles -------------------------------------------------------------------------
// The same role names as the Concord's (lib/concord.js), so the page's rules pick a role, never a colour. The Conclave:
// holo cyan for a reading and a hover (Sky), a pale sea-glass print for labels (Lilac), the bright holo for prose and
// titles (each glows in its placard's veiled window), mint light for whole and engaged (Accord; kept clear of the holo so
// whole never reads as a plain reading), amber for damaged, coral only for down and alarms (design.md: "a rare
// disturbance"); the dark roles are the void's own teal.
const CC = {
  deep: '#0f3a45', signal: '#14505c', sky: AE.holo, accord: AE.mint, lilac: AE.dim, caution: AE.amber, alert: AE.coral, text: AE.holoHi,
  ink: '#021317', inkDis: '#2b4c53', night: '#05191f', ghost: '#3f6a72', void: '#03141a', slate: '#1d4650', mute: '#14303a', ver: '#7fa9b0',
  char: '#3a1412', plate: '#04161c', bg: AE.bg, title: AE.holoHi,
};
// The ink that reads on a fill: dark on lit light, holo on the dark void. Nothing on a key is drawn in black.
const ink = (fill) => (lum(fill) > 0.2 ? CC.ink : AE.holoHi);
// Ink on the keys and tabs: a hologram's name glows in its own light, idle, hover and pressed (the skins page's
// --btn-ink); a tab not chosen in the pale print; the chosen tab and a lit square key in dark ink on the light.
const keyInk = { idle: AE.holoHi, hover: '#ffffff', pressed: '#ffffff', tabOn: CC.ink, tabOff: AE.dim };
// Type: "Exo 2" for every readout and label, "Michroma" wide capitals for titles (the skins page's pair). The page asks
// for Antonio sizes; Exo 2 at weight 600 runs about 1.25 times as wide and Michroma about 1.9 (measured on the
// console's labels), so they are set smaller and rule T (the keys' fit) shrinks what still runs long.
function font(spec) {
  const m = /^(\d+) ([\d.]+)px Antonio$/.exec(spec); if (!m) return spec;
  const px = +m[2];
  if (m[1] === '700' && px >= 20) return `400 ${Math.round(px * 0.62 * 10) / 10}px Michroma`;
  return `600 ${Math.round(px * 0.84 * 10) / 10}px "Exo 2"`;
}
const fonts = ['400 20px Michroma', '600 20px "Exo 2"', '300 20px "Exo 2"'];

// ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ---------------------------------------
// Sacred geometry in strokes of light: triangles, lozenges, rings and points, the shapes the Conclave builds its keys
// from. Even strokes with a soft glow; a solid crystal where the thing itself is solid (a hull, a core, a figure), dots
// for what is given off. One ink; on a 48 x 48 grid with 4 px clear, read at 20 px and 48 px.
function icon(name, col = AE.holo) {
  return paint(48, 48, (x) => {
    const bright = lum(col) > 0.2;
    x.strokeStyle = col; x.fillStyle = col; x.lineWidth = 2.6; x.lineJoin = 'round'; x.lineCap = 'round';
    if (bright) { x.shadowColor = hexA(col, 0.7); x.shadowBlur = 4; }
    const L = (pts, closed = false, lw = 2.6) => { x.lineWidth = lw; path(x, pts, closed); x.stroke(); };
    const F = (pts) => { path(x, pts); x.fill(); };
    const D = (cx, cy, r = 2.6) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); };
    const C = (cx, cy, r, lw = 2.6) => { x.lineWidth = lw; x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.stroke(); };
    const A = (cx, cy, r, a0, a1, lw = 2.6) => { x.lineWidth = lw; x.beginPath(); x.arc(cx, cy, r, a0, a1); x.stroke(); };
    const TRI = (cx, cy, r, a = -P / 2, fill = false) => { const pts = [0, 1, 2].map((i) => [cx + Math.cos(a + i * 2 * P / 3) * r, cy + Math.sin(a + i * 2 * P / 3) * r]); if (fill) F(pts); else L(pts, true); };
    const GEM = (cx, cy, rx, ry, fill = false) => { const pts = [[cx, cy - ry], [cx + rx, cy], [cx, cy + ry], [cx - rx, cy]]; if (fill) F(pts); else L(pts, true); };
    ({
      // the helm: a ring with the heading as a crystal at its rim and a triangle of bearing inside
      helm: () => { C(24, 24, 19); TRI(24, 25, 12); GEM(24, 7, 4, 5.5, true); },
      // power: a crystal core in a ring, light given off at the four points
      power: () => { C(24, 24, 18, 2.2); GEM(24, 24, 7, 11, true); for (const [px, py] of [[24, 2], [46, 24], [24, 46], [2, 24]]) D(px, py, 2); },
      // tactical: a lance of light, a lozenge emitter at its root
      tactical: () => { GEM(13, 35, 7, 7, true); L([[18, 30], [41, 7]], false, 3.4); L([[33, 7], [41, 7], [41, 15]], false, 2.6); },
      // point defence: a shield of three facets with a ring of light catching a mote
      pd: () => { L([[24, 4], [41, 13], [36, 36], [24, 44], [12, 36], [7, 13]], true, 2.4); C(24, 23, 7, 2.2); D(24, 23, 2.4); },
      // science: a triangle of study round an eye of light
      science: () => { TRI(24, 27, 19); GEM(24, 27, 8, 5); D(24, 27, 2.4); },
      // comms: a crystal sending three arcs of light
      comms: () => { GEM(12, 24, 5, 9, true); for (const r of [11, 19, 27]) A(12, 24, r, -0.7, 0.7, 2.4); },
      // repair: two lozenges mended into one by a line of light
      repair: () => { GEM(16, 18, 8, 12, true); GEM(32, 30, 8, 12); L([[11, 40], [37, 8]], false, 2); },
      // transport: a figure as a lozenge of light, dissolving into points below
      transport: () => { D(24, 8, 4.6); GEM(24, 21, 8, 8, true); for (const [px, py, r] of [[18, 33, 2.2], [24, 35, 2.2], [30, 33, 2.2], [21, 40, 1.8], [27, 40, 1.8]]) D(px, py, r); },
      // fleet: three crystals in a triangle, the lead ahead
      fleet: () => { GEM(24, 12, 6, 9, true); GEM(11, 34, 5.5, 8, true); GEM(37, 34, 5.5, 8, true); },
      // the ship from above: the Conclave's lozenge hull with its crystal heart
      ship: () => { L([[24, 3], [37, 20], [33, 40], [24, 45], [15, 40], [11, 20]], true, 2.6); GEM(24, 23, 5, 9, true); },
      // shields: a hexagon of light round a crystal
      shields: () => { L([[24, 4], [42, 14], [42, 34], [24, 44], [6, 34], [6, 14]], true, 2.6); GEM(24, 24, 6, 10, true); },
      // sensors: three rings and a sweep, a mote found
      sensors: () => { C(24, 24, 19, 2.2); C(24, 24, 10, 1.6); L([[24, 24], [38, 11]], false, 2.4); D(31, 31, 2.8); },
      // tractor: two crystals held apart by a line of light
      tractor: () => { GEM(10, 24, 6, 10, true); GEM(38, 24, 6, 10); x.setLineDash([3, 3]); L([[17, 24], [31, 24]], false, 2.4); x.setLineDash([]); },
      // craft: a small arrow of light with swept fins
      craft: () => { F([[24, 4], [31, 26], [43, 34], [24, 30], [5, 34], [17, 26]]); D(24, 41, 2.4); },
      // weapons: a beam out of a triangle, a crystal at its end
      weapons: () => { TRI(11, 37, 8, -P / 4, true); L([[15, 33], [33, 15]], false, 3); GEM(37, 11, 5, 5, true); },
      // warp: a lozenge drawn out into speed, three lines of light behind it
      warp: () => { GEM(32, 24, 10, 10, true); L([[4, 16], [18, 16]], false, 2.4); L([[2, 24], [20, 24]], false, 2.4); L([[4, 32], [18, 32]], false, 2.4); },
      // engines: a crystal over three spreading lines of thrust
      engines: () => { GEM(24, 14, 10, 11, true); L([[24, 29], [24, 44]], false, 2.4); L([[18, 29], [12, 42]], false, 2.4); L([[30, 29], [36, 42]], false, 2.4); },
      // reactor: a crystal core and two crossed orbits
      reactor: () => { GEM(24, 24, 5, 7, true); x.lineWidth = 2.2; for (const r of [P / 4, -P / 4]) { x.beginPath(); x.ellipse(24, 24, 19, 7.5, r, 0, 2 * P); x.stroke(); } },
      // map: points joined into a constellation, a crystal at its end
      map: () => { const n = [[7, 38], [15, 25], [27, 31], [33, 16]]; L(n, false, 2); for (const [px, py] of n) D(px, py, 2.4); GEM(39, 9, 5, 6, true); },
      pause: () => { GEM(16, 24, 6, 17, true); GEM(32, 24, 6, 17, true); },
      menu: () => { for (const y of [12, 24, 36]) { GEM(9, y, 3.5, 3.5, true); L([[16, y], [41, y]], false, 3); } },
      // fire: three lances converging on a crystal
      fire: () => { L([[5, 10], [33, 24]], false, 2.4); L([[5, 38], [33, 24]], false, 2.4); L([[3, 24], [33, 24]], false, 2.4); GEM(39, 24, 6, 9, true); },
      cargo: () => { for (const [cx, cy] of [[16, 16], [32, 16], [16, 32], [32, 32]]) GEM(cx, cy, 7, 7); },
      // settings: a hexagon round a triangle round a point
      settings: () => { L([[24, 4], [41, 14], [41, 34], [24, 44], [7, 34], [7, 14]], true, 2.4); TRI(24, 25, 10); D(24, 25, 2.6); },
    })[name]();
  });
}
const ICON_NAMES = ['helm', 'power', 'tactical', 'pd', 'science', 'comms', 'repair', 'transport', 'fleet', 'ship', 'shields', 'sensors', 'tractor', 'craft', 'weapons', 'warp', 'engines', 'reactor', 'map', 'pause', 'menu', 'fire', 'cargo', 'settings'];

// ---- Live readouts (the console skins page and the battle console) ---------------------------------------------
const live = {
  // A pointed mote of light leading a dot.
  arrow: (x) => { x.shadowColor = AE.holo; x.shadowBlur = 6; x.fillStyle = AE.holoHi; path(x, [[12, 0], [-2, -5], [-8, 0], [-2, 5]]); x.fill(); x.beginPath(); x.arc(-13, 0, 2, 0, 2 * P); x.fill(); },
  // The helm's crystal: nothing drawn over it (the crystal is the stop).
  helmCentre: () => {},
  // The ship as a hologram: a shell outlined in light round a crystal core; shields are arcs of light in dotted rings.
  ssd: (x, cx, cy, sh, col) => {
    for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.save(); x.shadowColor = col(sh[i]); x.shadowBlur = 8; x.strokeStyle = col(sh[i]); x.lineWidth = 3; x.beginPath(); x.arc(cx, cy, 104, a - P / 6 + 0.09, a + P / 6 - 0.09); x.stroke(); x.setLineDash([1, 5]); x.lineWidth = 1.5; x.beginPath(); x.arc(cx, cy, 112, a - P / 6 + 0.09, a + P / 6 - 0.09); x.stroke(); x.restore(); }
    const hull = [[cx, cy - 76], [cx + 52, cy - 50, cx + 46, cy + 14], [cx + 36, cy + 58, cx, cy + 66], [cx - 36, cy + 58, cx - 46, cy + 14], [cx - 52, cy - 50, cx, cy - 76]];
    holo(x, () => path(x, hull), [cx - 52, cy - 76, cx + 52, cy + 66], 0.35);
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
  // Four crescents of light cupping the target, a hairline of light between them.
  bracket: (x, bx, by, bw, bh) => {
    x.save(); x.shadowColor = AE.holo; x.shadowBlur = 6; x.strokeStyle = hexA(AE.holo, 0.6); x.lineWidth = 1; x.strokeRect(bx + 10, by + 10, bw - 20, bh - 20);
    x.strokeStyle = AE.holoHi; x.lineWidth = 2.4;
    for (const [px, py, a] of [[bx, by, P], [bx + bw, by, -P / 2], [bx, by + bh, P / 2], [bx + bw, by + bh, 0]]) { x.beginPath(); x.arc(px, py, 18, a - 0.1, a + P / 2 + 0.1); x.stroke(); }
    x.restore();
  },
};

// ---- Shapes for the battle console's vector keys ----------------------------------------------------------------
// The Conclave's drawn keys are lozenges of light: every rounded rectangle the page asks for is drawn with its ends drawn
// to a point (as the hologram keys), and a round knob is a crystal.
function rr(G, X, Y, W, H, r) {
  G.beginPath(); if (!(W > 0 && H > 0)) { G.rect(X, Y, W, H); return; }
  const R = Array.isArray(r) ? Math.max(...r) : (+r || 0), t = Math.max(0, Math.min(R * 0.8, W / 2, H / 2));
  G.moveTo(X + t, Y); G.lineTo(X + W - t, Y); G.lineTo(X + W, Y + H / 2); G.lineTo(X + W - t, Y + H); G.lineTo(X + t, Y + H); G.lineTo(X, Y + H / 2); G.closePath();
}
function knob(G, cx, cy, r) { G.beginPath(); G.moveTo(cx, cy - r * 1.2); G.lineTo(cx + r, cy); G.lineTo(cx, cy + r * 1.2); G.lineTo(cx - r, cy); G.closePath(); }

// ---- The kit ------------------------------------------------------------------------------------------------------
// The shells are rendered in Blender and come from the inlined manifest; their holograms from the inlined shapes; load()
// decodes the shells and lays each piece's hologram over it once. The battle console's own pieces are composed here from
// the same renders and the same holograms (no new shell): the square key is the dial's gold ring round a disc of light,
// the wide key the key's own gold claws round a lozenge of light, in every state the console shows; the allocation
// cells are lozenges of light alone, as the bar symbols are; the name plate is the plate drawn shorter (its straight
// window middle taken in) so the Fleet key stands beside it.
const MANIFEST = 'assets/ui/aelith/manifest.json', SHAPES = 'assets/ui/aelith/shapes.json';
const IMG = {}, kit = {}, hero = {}, heroSize = {}, barCells = {}, cells = {};
// the key states the console asks for (facePiece in the page) and the light each shows
const KEY_STATES = { idle: [0.15, AE.holo], hover: [0.6, AE.holo], on: [null, AE.holo], ok: [null, AE.mint], warn: [null, AE.amber], alert: [null, AE.coral], off: [0.04, AE.holo] };
let loading = null;
function json(src) { const n = document.querySelector(`script[data-src="${src}"]`); return n ? JSON.parse(n.textContent) : null; }
function compose(img, size, holoC) { return paint(size[0], size[1], (x) => { x.drawImage(img, 0, 0, size[0], size[1]); if (holoC) x.drawImage(holoC, 0, 0, size[0], size[1]); }); }
function load() {
  if (loading) return loading;
  const d = json(MANIFEST), sh = json(SHAPES);
  if (!d || !sh) return (loading = Promise.reject(new Error(`the Aelith kit (${MANIFEST}, ${SHAPES}) is not inlined in this page`)));
  loading = Promise.all(Object.entries(d.pieces).map(([id, p]) => new Promise((ok, bad) => { const im = new Image(); im.onload = () => { IMG[id] = im; ok(); }; im.onerror = () => bad(new Error(`Aelith piece ${id} failed to decode`)); im.src = p.data; }))).then(() => {
    const at = (c) => (x, w, h) => x.drawImage(c, 0, 0, w, h);
    for (const [id, p] of Object.entries(d.pieces)) {
      const c = compose(IMG[id], p.size, holoCanvas(id, p.size, sh.pieces[id], true));
      if (p.kind === 'nine') kit[id] = { size: p.size, m: p.margins, inset: p.inset || [3, 3], draw: at(c) };
      else { hero[id] = at(c); heroSize[id] = p.size; }
    }
    for (const [id, col] of Object.entries(FILLS)) kit[id] = { size: [64, 10], m: [0, 6, 0, 6], draw: at(fillPiece(col)) };
    for (const [id, level, col] of CELL_SYMBOLS) barCells[id] = { size: [8, 12], gap: 3, draw: at(paint(8, 12, (x) => lozenge(x, level, col))) };
    // the square key: the dial's ring (rendered, its well a disc of radius 49 in 120) at 48 px, light in its well
    for (const [st, [level, col]] of Object.entries(KEY_STATES)) {
      const c = paint(48, 48, (x) => {
        x.drawImage(IMG.dial, 0, 0, 48, 48);
        const well = () => { x.beginPath(); x.arc(24, 24, 49 * 0.4 - 0.6, 0, 2 * P); };
        x.save(); well(); x.fillStyle = hexA(AE.deep, 0.7); x.fill(); x.restore();
        if (level == null) solid(x, well, [5, 5, 43, 43], col); else holo(x, well, [5, 5, 43, 43], level, col);
      });
      hero['key.' + st] = at(c); heroSize['key.' + st] = [48, 48];
    }
    // the wide key: the key's gold claws (button.idle, 150 x 48) round a lozenge of light; 144 x 48 so its ends hold
    const kb = sh.pieces['button.idle'], keyPts = Object.fromEntries(kb.holo).key;
    for (const [st, [level, col]] of Object.entries(KEY_STATES)) {
      const c = paint(150, 48, (x) => {
        x.drawImage(IMG['button.idle'], 0, 0, 150, 48);
        const well = () => path(x, keyPts);
        x.save(); well(); x.fillStyle = hexA(AE.deep, 0.75); x.fill(); x.restore();
        if (level == null) solid(x, well, bounds(keyPts), col); else holo(x, well, bounds(keyPts), level, col);
      });
      kit['keyw.' + st] = { size: [150, 48], m: [0, 42, 0, 42], draw: at(c) };
    }
    // the allocation cells: lozenges of light, idle faint, lit holo, warning coral, whole mint, locked an empty outline
    const cellShape = (x, w, h) => () => { const t = Math.min(7, w / 3); path(x, [[t, 1], [w - t, 1], [w - 1, h / 2], [w - t, h - 1], [t, h - 1], [1, h / 2]]); };
    for (const [id, level, col] of [['cell.idle', 0.12, AE.holo], ['cell.lit', null, AE.holo], ['cell.warn', null, AE.coral], ['cell.accord', null, AE.mint], ['cell.locked', 0, AE.holo]]) {
      const c = paint(32, 28, (x) => {
        const s = cellShape(x, 32, 28);
        if (id === 'cell.locked') { x.save(); x.setLineDash([2, 3]); s(); x.strokeStyle = hexA(AE.holo, 0.35); x.lineWidth = 1.2; x.stroke(); x.restore(); return; }
        x.save(); s(); x.fillStyle = hexA(AE.deep, 0.75); x.fill(); x.restore();
        if (level == null) solid(x, s, [0, 0, 32, 28], col); else holo(x, s, [0, 0, 32, 28], level, col);
      });
      cells[id] = { size: [32, 28], m: [9, 9, 9, 9], draw: at(c) };
    }
    // the title placard (every panel title and officer slot on a frame's top rail): the tab's gold capsule (tab.idle,
    // 120 x 40, its shell rows 4-36) drawn 24 px tall, its window (rows 4.7-19.3 of the placard) veiled, so a title glows
    // on the void held in gold, as the Krai's and the Kelvane's titles stand on their plates
    const tb = sh.pieces['tab.idle'], tabPts = Object.fromEntries(tb.holo).tab;
    const pt = paint(120, 40, (x) => { x.drawImage(IMG['tab.idle'], 0, 0, 120, 40); veil(x, tabPts, 0.8); edge(x, tabPts, 0.15); });
    kit['plate.title'] = { size: [120, 24], m: [0, 20, 0, 20], draw: (x, w, h) => x.drawImage(pt, 0, 4 * 2, pt.width, 32 * 2, 0, 0, w, h) };
    // the name plate drawn shorter (its window's straight middle, x 120-340 of 460, taken in to 126 px), its own
    // hologram over it, so the plate ends at 366 and the Fleet key (x 370-456) stands beside it on the backing
    const np = compose(IMG.nameplate, [460, 56], holoCanvas('nameplate', [460, 56], sh.pieces.nameplate, true));
    hero.nameplate = (x, w, h) => {
      const k = w / 460, sk = np.width / 460;
      for (const [s0, s1, d0, d1] of [[0, 120, 0, 120], [120, 340, 120, 246], [340, 460, 246, 366]]) x.drawImage(np, s0 * sk, 0, (s1 - s0) * sk, np.height, d0 * k, 0, (d1 - d0) * k, h);
    };
  });
  return loading;
}

const api = {
  id: 'aelith', name: 'Aelith Conclave', AE, CC, kit, hero, cells, barCells, live, icon, ICON_NAMES, ink, keyInk, font, fonts, load, hexA, rr, knob, heroSize,
  holo, field, lozenge, layered, FILLS, fillPiece, CELL_SYMBOLS, path,
  family: '"Exo 2", sans-serif',
  // The content inside a panel: clear of the gold shell and its corner knuckles (44 px margins; the well's straight
  // edges 16 px in, the knuckles reaching 41 px into the corners), 10 px into the veiled well.
  pad: [28, 26, 26, 26],
  // The name plate's live field: the window as drawn shorter (64-300 x 13-43 of the plate), in the holo's light.
  plate: { x: 66, y: 36, max: 230, font: '600 19px "Exo 2"', col: AE.holoHi },
  // The battle's helm ring under the ship, in holo light.
  ring: ['rgba(111,243,255,0)', 'rgba(111,243,255,0.14)', 'rgba(200,250,255,0.9)'],
  // Names for the player's side where the page shows it as the Conclave: ships named for a stillness or a patience of
  // the world, their captains Elders (the skins page's SERENE TIDE, ELDER IAVEN). Placeholders until the owner sets the
  // Conclave's culture (CLAUDE.md section 1; openspec/changes/aelith-battle-console, open question A1).
  player: { faction: 'Aelith Conclave', call: 'Elder', trim: 0x6ff3ff, hull: 0xd8c27a,
    ships: { resolute: ['Serene Tide', 'Elder Iaven'], tallis: ['Long Dawn', 'Elder Soress'], meridian: ['Quiet Lattice', 'Elder Thaliar'] },
    ranks: ['Listener', 'Keeper', 'Warden', 'Elder'],
    officers: { o1: 'Ivel Sarane', o2: 'Oriel Tavan', o3: 'Caelis Morn', o4: 'Tesri Vaun', o5: 'Ileth Avarre', o6: 'Seyan Corel', o7: 'Maedris Holm',
      o8: 'Aunel Ressa', o9: 'Velith Ondra', o10: 'Pirael Sund', o11: 'Lisaen Toll', o12: 'Rhaun Elcer', o13: 'Theral Quen' } },
  link: AE.holo,
  // Labels centred in each key's lozenge of light, clear of its claws (the key's lozenge runs 22-128 of 150, drawn to a
  // point over its first and last 14 px; a label, a third of the key's height, clears the point from 4 px in: the open
  // field 26-124); on a tab, in its window (14-106 of 120, round ends 10 px).
  keyCenter: true, keyField: [26, 26], tabField: [20, 20],
  // A hover name on the wide key in its hover light, the name glowing on it.
  tip: ['button.hover', 'hover'],
  // Icon tabs (the station and ship rails) on the square key: a disc of light in a gold ring.
  iconKeys: true,
  // Panel tab icons at rest in the pale print, apart from the functional icons' holo; glyphs on a dark key in holo.
  tabIcon: AE.dim, glyphOnDark: AE.holoHi, mark: { hover: AE.holoHi, ready: AE.mint, count: AE.dim },
  // The strips (72 px rendered, as the skins page's) at their full height; their channel is rows 9-63, its open band
  // 12-59; the content, laid out for 56 px, moved down to centre in it.
  stripTop: { h: 72, dy: 13 }, stripBottom: { h: 72, dy: 2 },
  // A panel title's baseline 2 px higher than the console's, its Michroma capitals centred in the placard's window.
  titleDy: 2,
};
window.FCSkins = window.FCSkins || {}; window.FCSkins.aelith = api;
return api;
})();
