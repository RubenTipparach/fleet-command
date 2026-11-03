// Fleet Command mockups: the Krai Warhost skin (openspec/changes/console-skins/design.md, "Krai: cast volcanic gunmetal";
// openspec/changes/krai-battle-console), shared by the console skins page (docs/mockups/skins/krai.js registers it there)
// and the battle console in the Krai skin (docs/mockups/battle-console-krai.html, written from battle-console.html by
// tools/mockups/skin_page.py). One implementation of the Krai look (CLAUDE.md section 6.1): its palette, its icons, its
// live readouts and, for the battle console, its kit loaded from the Blender render (assets/ui/krai/manifest.json,
// tools/blender/build_ui_krai.py) plus the few pieces the render does not have (the allocation cells).
// It exposes the same fields as docs/mockups/lib/concord.js, so the battle console swaps one for the other and changes
// no rule and no layout. Needs docs/mockups/lib/kitdraw.js inlined first.
window.FCKrai = (function () {
'use strict';
const { paint, P } = window.FCDraw;
const KR = { black: '#15100f', steel: '#2a2526', edge: '#c9d0d8', blood: '#b3121b', red: '#ff2a2a', orange: '#ff7a1a', hot: '#ffb070', bone: '#e8dccb', dim: '#a8908a' };
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
function poly(x, pts) { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); x.closePath(); }
// Relative luminance of a #rrggbb colour (WCAG), for picking the ink that reads on a fill.
function lum(hex) {
  if (typeof hex !== 'string' || hex[0] !== '#' || hex.length < 7) return 0;
  const c = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
}
function enamel(x, pts, col = KR.blood, glow = 0) {
  x.save(); if (glow) { x.shadowColor = col; x.shadowBlur = glow; } poly(x, pts);
  const ys = pts.map((p) => p[1]), g = x.createLinearGradient(0, Math.min(...ys), 0, Math.max(...ys));
  g.addColorStop(0, col === KR.blood ? '#d8232c' : KR.hot); g.addColorStop(1, col); x.fillStyle = g; x.fill(); x.restore();
  poly(x, pts); x.strokeStyle = 'rgba(0,0,0,0.6)'; x.lineWidth = 0.8; x.stroke();
}

// ---- The battle console's colour roles -------------------------------------------------------------------------
// The same role names as the Concord's (lib/concord.js), so the page's rules pick a role, never a colour. Krai: blood
// keys at rest, the forge orange for readouts and hover, ash labels, bone text, black steel. The state colours (accord,
// caution, alert) are the Krai kit's own alert lamps, green, yellow and red, so a status reads the same in every skin
// (the icon guide: "colour carries state").
const CC = {
  deep: '#3b1714', signal: '#a3161a', sky: '#ff8a2a', accord: '#6fe39a', lilac: '#b8a097', caution: '#ffd23a', alert: '#ff2a2a', text: '#efe2d0',
  // the roles the Concord page had written as bare colours
  ink: '#160806', inkDis: '#6a3a32', night: '#1a0b09', ghost: '#6a4038', void: '#120706', slate: '#4a2622', mute: '#2e2220', ver: '#8a6a60',
  char: '#3a0b09', plate: '#0a0707', bg: '#0a0707', title: KR.red, bone: KR.bone,
};
// The ink that reads on a fill: dark on the lit colours, bone on blood and steel.
const ink = (fill) => (lum(fill) > 0.2 ? CC.ink : CC.text);
// Ink on the rendered keys and tabs: bone on the dark ember key at rest, dark on the red hover and the orange engaged key.
const keyInk = { idle: CC.text, hover: CC.ink, pressed: CC.ink, tabOn: CC.ink, tabOff: CC.text };
// Type: "Russo One" for titles and the name plate, "Teko" for everything else (the skin's own pair). The page asks for
// Antonio sizes; Teko is sized to Antonio's width (measured: Teko 500 at 1.1x is as wide as Antonio 600), so every slot
// that fits a Concord label fits a Krai one. Weight 700 at 22 px and up is a heading: Russo One at 0.75x.
function font(spec) {
  const m = /^(\d+) ([\d.]+)px Antonio$/.exec(spec); if (!m) return spec;
  const w = +m[1], s = +m[2];
  if (w >= 700 && s >= 22) return `400 ${Math.round(s * 0.75 * 10) / 10}px "Russo One"`;
  return w >= 700 ? `600 ${Math.round(s * 1.05 * 10) / 10}px Teko` : `500 ${Math.round(s * 1.1 * 10) / 10}px Teko`;
}
const fonts = ['400 20px "Russo One"', '500 20px Teko', '600 20px Teko'];

// ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ---------------------------------------
// Each is a picture of the job in the Krai's language: blades, wedges, triangles and facets, nothing round ("forged
// steel, edged everything"). Solid for the thing, thinner lines for what it gives off; one ink (orange on a dark key,
// dark on a lit one); on a 48 x 48 grid with 4 px clear, read at 20 px and 48 px.
function icon(name, col = CC.sky) {
  return paint(48, 48, (x) => {
    const bright = lum(col) > 0.2;
    x.strokeStyle = col; x.fillStyle = col; x.lineWidth = 3; x.lineJoin = 'miter'; x.lineCap = 'butt';
    if (bright) { x.shadowColor = col; x.shadowBlur = 3; }
    const F = (pts) => { poly(x, pts); x.fill(); };
    const S = (pts, closed = true, lw = 3) => { x.lineWidth = lw; x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) x.lineTo(p[0], p[1]); if (closed) x.closePath(); x.stroke(); x.lineWidth = 3; };
    const L = (x0, y0, x1, y1, lw = 3) => S([[x0, y0], [x1, y1]], false, lw);
    const ngon = (cx, cy, r, n, a0 = -P / 2) => Array.from({ length: n }, (_, i) => [cx + Math.cos(a0 + i * 2 * P / n) * r, cy + Math.sin(a0 + i * 2 * P / n) * r]);
    const star = (cx, cy, r0, r1, n, a0 = -P / 2) => Array.from({ length: 2 * n }, (_, i) => { const r = i % 2 ? r1 : r0, a = a0 + i * P / n; return [cx + Math.cos(a) * r, cy + Math.sin(a) * r]; });
    const rot = (pts, cx, cy, a) => pts.map(([px, py]) => [cx + px * Math.cos(a) - py * Math.sin(a), cy + px * Math.sin(a) + py * Math.cos(a)]);
    const cut = (fn) => { const sb = x.shadowBlur; x.shadowBlur = 0; x.globalCompositeOperation = 'destination-out'; fn(); x.globalCompositeOperation = 'source-over'; x.shadowBlur = sb; };
    ({
      // a war wheel: an eight-sided rim, eight spokes ending in spikes, a diamond hub
      helm: () => { S(ngon(24, 24, 13, 8, -P / 2 + P / 8), true, 3.5); for (let i = 0; i < 8; i++) { const a = -P / 2 + i * P / 4, c = Math.cos(a), s = Math.sin(a); L(24 + c * 4, 24 + s * 4, 24 + c * 17, 24 + s * 17, 2.6);
        F([[24 + c * 22, 24 + s * 22], [24 + c * 16 - s * 3.2, 24 + s * 16 + c * 3.2], [24 + c * 16 + s * 3.2, 24 + s * 16 - c * 3.2]]); } F(ngon(24, 24, 5.5, 4)); },
      // a jagged bolt in a hexagon
      power: () => { S(ngon(24, 24, 20, 6), true, 2.6); F([[28, 6], [15, 26], [23, 26], [19, 42], [33, 20], [25, 20]]); },
      // a turret: a stepped base, a wedge housing, twin blade barrels
      tactical: () => { F([[5, 44], [43, 44], [39, 38], [9, 38]]); F([[11, 37], [37, 37], [33, 27], [24, 23], [15, 27]]); F([[23, 30], [41, 11], [44, 13], [27, 32]]); F([[19, 27], [36, 8], [39, 10], [23, 30]]); },
      // point defence: a pointed shield with a burst on it
      pd: () => { S([[24, 4], [41, 10], [38, 30], [24, 44], [10, 30], [7, 10]], true, 3); F(star(24, 23, 10, 4, 6)); },
      // science: three bladed orbits, a diamond nucleus
      science: () => { for (const a of [0, P / 3, -P / 3]) S(rot([[-21, 0], [0, -7.5], [21, 0], [0, 7.5]], 24, 24, a), true, 2.4); F(ngon(24, 24, 5, 4)); },
      // comms: a wedge dish on a stand, its feed spike, two angled waves
      comms: () => { F([[4, 18], [30, 42], [20, 37], [11, 30], [6, 24]]); L(16, 31, 26, 20, 2.6); F(ngon(27, 19, 3.5, 4)); L(15, 34, 11, 45, 3); L(5, 45, 22, 45, 3);
        S([[30, 11], [36, 13], [38, 19]], false, 2.4); S([[31, 3], [42, 7], [46, 18]], false, 2.4); },
      // repair: a wrench with a hexagonal jaw
      repair: () => { F([[7, 37], [26, 18], [31, 23], [12, 42]]); F(ngon(32, 16, 10, 6, 0)); cut(() => F(rot([[-4, -6], [4, -6], [4, 8], [-4, 8]], 37, 11, P / 4))); F([[7, 37], [12, 42], [4, 45]]); },
      // transport: a figure, solid above the waist, sliced into bars below it
      transport: () => { F(ngon(24, 9, 6, 4)); F([[13, 37], [15, 21], [24, 16], [33, 21], [35, 37]]); F([[15, 36], [22, 36], [21, 45], [15, 45]]); F([[26, 36], [33, 36], [33, 45], [27, 45]]);
        cut(() => { for (let yy = 26, k = 0; yy < 46; yy += 3.4, k++) x.fillRect(0, yy, 48, 1 + k * 0.32); }); for (const [sx, sy] of [[8, 30], [40, 33], [9, 40], [39, 42]]) x.fillRect(sx - 3, sy - 0.9, 6, 1.8); },
      // fleet: three swept blades in formation
      fleet: () => { const chev = (cx, cy, k) => F([[cx, cy - 10 * k], [cx + 7.5 * k, cy + 7 * k], [cx, cy + 1.5 * k], [cx - 7.5 * k, cy + 7 * k]]); chev(24, 13, 1.25); chev(11, 33, 1.1); chev(37, 33, 1.1); },
      // the ship from above: a knife-blade hull with swept wings
      ship: () => F([[24, 3], [28, 20], [44, 37], [42, 41], [29, 35], [27, 45], [21, 45], [19, 35], [6, 41], [4, 37], [20, 20]]),
      // shields: six armour plates round a triangle core
      shields: () => { for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3, d = 0.4; F([[24 + Math.cos(a - d) * 13, 24 + Math.sin(a - d) * 13], [24 + Math.cos(a - d) * 21, 24 + Math.sin(a - d) * 21], [24 + Math.cos(a + d) * 21, 24 + Math.sin(a + d) * 21], [24 + Math.cos(a + d) * 13, 24 + Math.sin(a + d) * 13]]); } F(ngon(24, 25, 6, 3)); },
      // sensors: a hexagonal scope, a wedge sweep, two triangle contacts
      sensors: () => { S(ngon(24, 24, 20, 6), true, 2.6); S(ngon(24, 24, 9, 6), true, 1.8); F([[24, 24], [24, 4], [41, 14]]); F(ngon(34, 31, 3.4, 3)); F(ngon(14, 33, 3, 3)); },
      // tractor: an emitter wedge, a widening beam with chevron waves, the held hull
      tractor: () => { F([[2, 16], [13, 21], [13, 27], [2, 32]]); L(13, 21, 33, 10, 2.4); L(13, 27, 33, 38, 2.4); S([[18, 17], [22, 24], [18, 31]], false, 2); S([[25, 13], [30, 24], [25, 35]], false, 2); S([[35, 24], [46, 14], [46, 34]], true, 2.6); },
      // craft: a fighter, a thin blade fuselage with forward-swept wings
      craft: () => { F([[24, 3], [27.5, 24], [24, 45], [20.5, 24]]); F([[22, 21], [4, 13], [8, 26], [22, 31]]); F([[26, 21], [44, 13], [40, 26], [26, 31]]); },
      // weapons: an emitter's blade beam ending in a burst
      weapons: () => { F([[3, 45], [15, 41], [7, 33]]); L(9, 39, 30, 18, 4); F(star(36, 12, 10, 3.5, 6, -P / 4)); },
      // warp: a forward chevron trailing speed lines
      warp: () => { F([[22, 9], [42, 24], [22, 39], [29, 24]]); L(4, 15, 19, 15); L(2, 24, 22, 24); L(4, 33, 19, 33); },
      // engines: a nozzle and its flame
      engines: () => { F([[16, 4], [32, 4], [37, 23], [11, 23]]); S([[11, 27], [37, 27], [24, 46]], true, 2.4); F([[18, 27], [30, 27], [24, 38]]); },
      // reactor: three blades round a core
      reactor: () => { F(ngon(24, 24, 4, 3)); for (let i = 0; i < 3; i++) { const a = -P / 2 + i * 2 * P / 3, d = 0.55; F([[24 + Math.cos(a - d * 0.6) * 8, 24 + Math.sin(a - d * 0.6) * 8], [24 + Math.cos(a - d) * 20, 24 + Math.sin(a - d) * 20], [24 + Math.cos(a + d) * 20, 24 + Math.sin(a + d) * 20], [24 + Math.cos(a + d * 0.6) * 8, 24 + Math.sin(a + d * 0.6) * 8]]); } },
      // map: three triangle waypoints joined by a course
      map: () => { L(13, 16, 34, 21, 2); L(33, 25, 22, 34, 2); L(18, 32, 13, 19, 2); F(ngon(12, 14, 6.5, 3)); F(ngon(36, 21, 6.5, 3)); F(ngon(20, 36, 6.5, 3)); },
      pause: () => { F([[15, 10], [23, 10], [19, 38], [11, 38]]); F([[29, 10], [37, 10], [33, 38], [25, 38]]); },
      menu: () => { for (const y of [12, 22, 32]) F([[12, y], [40, y], [36, y + 6], [8, y + 6]]); },
      // fire: three blades closing on a diamond
      fire: () => { for (const a of [-P / 2, P / 6, 5 * P / 6]) { const c = Math.cos(a), s = Math.sin(a); F([[24 + c * 9, 24 + s * 9], [24 + c * 20 - s * 6, 24 + s * 20 + c * 6], [24 + c * 20 + s * 6, 24 + s * 20 - c * 6]]); } F(ngon(24, 24, 3.5, 4)); },
      cargo: () => { for (const [cx, cy] of [[15, 15], [33, 15], [15, 33], [33, 33]]) S(ngon(cx, cy, 8, 4), true, 2.6); },
      // settings: a cog with square teeth and a diamond bore
      settings: () => { const o = []; for (let i = 0; i < 8; i++) { const a = -P / 2 + i * P / 4; for (const [r, d] of [[14, -0.32], [20, -0.2], [20, 0.2], [14, 0.32]]) o.push([24 + Math.cos(a + d) * r, 24 + Math.sin(a + d) * r]); } F(o); cut(() => F(ngon(24, 24, 6, 4))); },
    })[name]();
  });
}
const ICON_NAMES = ['helm', 'power', 'tactical', 'pd', 'science', 'comms', 'repair', 'transport', 'fleet', 'ship', 'shields', 'sensors', 'tractor', 'craft', 'weapons', 'warp', 'engines', 'reactor', 'map', 'pause', 'menu', 'fire', 'cargo', 'settings'];

// ---- Live readouts ------------------------------------------------------------------------------------------------
// The ship display is a wireframe schematic (owner, 2026-10-31: "The krai ship display should be a wire frame
// schematic"): the blade hull top-down in glowing hairlines, a node at every vertex, its frame ribs and spine, the wing
// spars, the lower hull in dashed hidden lines, the weapon mounts boxed and called out, the engines as nozzle rings.
function wireframe(x, cx, cy) {
  const P2 = (pts) => pts.map(([a, b]) => [cx + a, cy + b]);
  const hull = P2([[0, -84], [9, -40], [12, 4], [54, 52], [52, 60], [16, 46], [8, 66], [-8, 66], [-16, 46], [-52, 60], [-54, 52], [-12, 4], [-9, -40]]);
  const lower = P2([[0, -70], [6, -36], [8, 2], [38, 40], [10, 34], [0, 54], [-10, 34], [-38, 40], [-8, 2], [-6, -36]]);
  const line = (pts, closed, w, a, dash) => { x.beginPath(); x.moveTo(pts[0][0], pts[0][1]); for (const q of pts.slice(1)) x.lineTo(q[0], q[1]); if (closed) x.closePath(); x.lineWidth = w; x.strokeStyle = hexA(KR.hot, a); x.setLineDash(dash || []); x.stroke(); };
  x.save(); x.shadowColor = KR.orange; x.shadowBlur = 6; x.lineJoin = 'round';
  line(lower, true, 0.9, 0.45, [3, 3]);
  line(hull, true, 1.6, 0.95);
  line(P2([[0, -84], [0, 66]]), false, 0.9, 0.7);
  for (const y of [-56, -36, -16, 4, 24, 44]) { const half = y < 4 ? 9 + (y + 84) / 88 * 3 : 12 + (y - 4) / 48 * 4; line(P2([[-half, y], [half, y]]), false, 0.8, 0.6); }
  for (const s of [-1, 1]) { line(P2([[12 * s, 4], [54 * s, 52]]), false, 0.8, 0.6); line(P2([[12 * s, 18], [44 * s, 50]]), false, 0.7, 0.45); line(P2([[16 * s, 46], [12 * s, 4]]), false, 0.7, 0.45); }
  x.setLineDash([]); x.shadowBlur = 0; x.fillStyle = KR.hot;
  for (const [a, b] of hull) { x.beginPath(); x.arc(a, b, 1.6, 0, 2 * P); x.fill(); }
  x.shadowColor = KR.orange; x.shadowBlur = 8; x.strokeStyle = KR.orange; x.lineWidth = 1.2;
  for (const [a, b] of [[0, -68], [-38, 44], [38, 44]]) { x.strokeRect(cx + a - 4, cy + b - 4, 8, 8); x.beginPath(); x.moveTo(cx + a + 4, cy + b - 4); x.lineTo(cx + a + 12, cy + b - 12); x.stroke(); }
  for (const a of [-5, 5]) { x.beginPath(); x.arc(cx + a, cy + 70, 3.4, 0, 2 * P); x.stroke(); }
  x.restore();
}
const live = {
  arrow: (x) => { x.fillStyle = KR.bone; poly(x, [[12, 0], [-6, -6], [-2, 0], [-6, 6]]); x.fill(); x.strokeStyle = KR.blood; x.lineWidth = 1; x.stroke(); },
  helmCentre: (x, c) => { x.fillStyle = KR.bone; x.font = '400 13px "Russo One"'; x.textAlign = 'center'; x.fillText('HOLD', c, c + 8); },
  // A knife-blade hull swept back into wings; shields as rows of red teeth.
  ssd: (x, cx, cy, sh, col) => {
    for (let i = 0; i < 6; i++) { const a0 = -P / 2 + i * P / 3 - P / 6 + 0.1, a1 = a0 + P / 3 - 0.2; x.fillStyle = col(sh[i]); for (let k = 0; k <= 6; k++) { const a = a0 + (a1 - a0) * k / 6; poly(x, [[cx + Math.cos(a) * 98, cy + Math.sin(a) * 98], [cx + Math.cos(a + 0.04) * 110, cy + Math.sin(a + 0.04) * 110], [cx + Math.cos(a - 0.04) * 110, cy + Math.sin(a - 0.04) * 110]]); x.fill(); } }
    wireframe(x, cx, cy);
  },
  // A thin wedge sweeps; contacts are red triangles.
  dial: (x, c, cts) => {
    x.save(); x.shadowColor = KR.orange; x.shadowBlur = 8; x.fillStyle = hexA(KR.orange, 0.55); poly(x, [[c, c], [c + Math.cos(-1.1) * 40, c + Math.sin(-1.1) * 40], [c + Math.cos(-0.8) * 40, c + Math.sin(-0.8) * 40]]); x.fill(); x.restore();
    for (const [dx, dy] of cts) { const px = c + dx * 32, py = c + dy * 32; enamel(x, [[px, py - 5], [px + 5, py + 4], [px - 5, py + 4]], KR.red, 6); }
  },
  // Four red spikes stabbing at the target's corners, scaled to the bracket.
  bracket: (x, bx, by, bw, bh) => {
    const k = Math.min(1, Math.min(bw, bh) / 120);
    for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
      x.save(); x.translate(px, py); x.scale(sx * k, sy * k); enamel(x, [[-8, -8], [48, 0], [8, 6], [0, 48]], KR.red, 10); x.restore();
    }
  },
};
// The Krai mark for the player's faction where the console draws an emblem (a portrait, the holder of a ship): a red
// war-shield triangle round a black blade.
function emblem(G, cx, cy, r) {
  G.save(); G.translate(cx, cy); G.scale(r / 24, r / 24);
  enamel(G, [[0, -24], [22, 18], [0, 10], [-22, 18]], KR.red, 6);
  G.fillStyle = KR.black; poly(G, [[0, -12], [5, 6], [0, 2], [-5, 6]]); G.fill();
  G.restore();
}

// ---- Shapes for the battle console's vector keys ----------------------------------------------------------------
// The page draws its own keys (helm keys, role keys, the warp key, sliders) as rounded rectangles in the Concord. The
// Krai cut them as blades instead (nothing round survives the forge): the top-left and bottom-right corners cut deep,
// the other two nicked, the cut as big as the rounding asked for. Round knobs become hexagons.
function rr(G, X, Y, W, H, r) {
  G.beginPath(); if (!(W > 0 && H > 0)) { G.rect(X, Y, W, H); return; }
  const R = Array.isArray(r) ? Math.max(...r) : (+r || 0), c = Math.max(0, Math.min(R * 0.8, W / 2, H / 2)), s = c * 0.3;
  G.moveTo(X + c, Y); G.lineTo(X + W - s, Y); G.lineTo(X + W, Y + s); G.lineTo(X + W, Y + H - c); G.lineTo(X + W - c, Y + H); G.lineTo(X + s, Y + H); G.lineTo(X, Y + H - s); G.lineTo(X, Y + c); G.closePath();
}
function knob(G, cx, cy, r) { G.beginPath(); for (let i = 0; i < 6; i++) { const a = i * P / 3; G.lineTo(cx + Math.cos(a) * r * 1.08, cy + Math.sin(a) * r * 1.08); } G.closePath(); }

// ---- The battle console's kit -----------------------------------------------------------------------------------
// Every piece, the battle console's own included (the placards, the column, the clamps and gussets, the allocation
// cells; owner, 2026-11-03: "you didn't render the stitching elements in blender"), is rendered in Blender by
// tools/blender/build_ui_krai.py and comes from the inlined manifest (tools/mockups/inline_data.py); load() decodes
// them once.
const MANIFEST = 'assets/ui/krai/manifest.json';
const IMG = {}, kit = {}, hero = {}, heroSize = {}, barCells = {};
let loading = null;
function load() {
  if (loading) return loading;
  const n = document.querySelector(`script[data-src="${MANIFEST}"]`);
  if (!n) return (loading = Promise.reject(new Error(`the Krai kit (${MANIFEST}) is not inlined in this page`)));
  const d = JSON.parse(n.textContent);
  loading = Promise.all(Object.entries(d.pieces).map(([id, p]) => new Promise((ok, bad) => { const im = new Image(); im.onload = () => { IMG[id] = im; ok(); }; im.onerror = () => bad(new Error(`Krai piece ${id} failed to decode`)); im.src = p.data; }))).then(() => {
    for (const [id, p] of Object.entries(d.pieces)) {
      const draw = (x, w, h) => x.drawImage(IMG[id], 0, 0, w, h);
      if (p.kind === 'nine') kit[id] = { size: p.size, m: p.margins, inset: p.inset || [3, 3], draw };
      else if (p.kind === 'hero') { hero[id] = draw; heroSize[id] = p.size; }
      else if (p.kind === 'cell') barCells[id] = { size: p.size, gap: p.gap ?? 2, draw };
    }
    // the green (ok) symbol: the lit chevron's light turned to the state colour
    const on = barCells['bar.cell.on'];
    if (on) barCells['bar.cell.ok'] = { ...on, draw: (x, w, h) => { on.draw(x, w, h); x.globalCompositeOperation = 'hue'; x.fillStyle = CC.accord; x.fillRect(0, 0, w, h); x.globalCompositeOperation = 'destination-in'; on.draw(x, w, h); x.globalCompositeOperation = 'source-over'; } };
  });
  return loading;
}

const api = {
  id: 'krai', name: 'Krai Warhost', KR, CC, kit, hero, cells: {}, barCells, live, icon, ICON_NAMES, emblem, ink, keyInk, font, fonts, load, enamel, poly, hexA, rr, knob, heroSize, family: 'Teko, sans-serif',
  // The content inside a panel frame: clear of the honed rim and the corner fangs (the 9-slice margins hold the fangs
  // whole, so they are larger than the rim).
  pad: [26, 20, 20, 22],
  // The name plate's live field (docs/mockups/skins/krai.js plateText), right of the war-shield triangle.
  plate: { x: 92, y: 35, max: 270, font: '400 19px "Russo One"', col: KR.bone },
  // The battle's helm ring under the ship, in forge light.
  ring: ['rgba(255,140,60,0)', 'rgba(255,110,40,0.15)', 'rgba(255,170,110,0.9)'],
  // Names and colours for the player's side, which the battle console's stand-in battle calls the Concord.
  player: { faction: 'Krai Warhost', call: 'Krai', trim: 0xd02a20, hull: 0x6a6062,
    ships: { resolute: ['Kharz', 'Warlord Utrek'], tallis: ['Vekk', 'Blade Sora'], meridian: ['Ruhn', 'Fang Idra'] } },
  link: '#ff8a2a',
  // labels centred on the keys and tabs, clear of the blade's point (the skin's --btn-justify: center)
  keyCenter: true,
};
window.FCSkins = window.FCSkins || {}; window.FCSkins.krai = api;
return api;
})();
