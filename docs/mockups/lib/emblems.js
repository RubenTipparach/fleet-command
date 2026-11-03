// Fleet Command mockups: the faction emblems (openspec/changes/faction-emblems/design.md). One implementation of every
// faction's mark (CLAUDE.md section 6.1): the console crest, the 40 px and 20 px marks and the hull marking are all drawn
// from the shapes here, so the three can never disagree. Inlined into docs/mockups/faction-emblems.html by
// tools/mockups/inline_data.py; a later skin generator or hull painter reads the same shapes.
//
// An emblem is drawn on a 100 x 100 grid. It has a FIELD (the outer shape, in the field colour), DEVICES (the picture
// inside, in the device colour) and CUTS (holes through the field to the ground). Every device sits in a window cut
// through the field GAP units wider than itself, so the mark still reads in one colour, painted through a stencil or
// shown at 20 px (design rule E3). All shapes are our own (CLAUDE.md section 1); the meanings are provisional (survey E).
window.FCEmblems = (function () {
'use strict';
const P = Math.PI;
const GAP = 5; // grid units between a device and the field round it: 1 px at 20 px, 2 px at 40 px

// ---- shape helpers: each returns a Path2D on the 100 grid -------------------------------------------------------
function circle(cx, cy, r) { const p = new Path2D(); p.arc(cx, cy, r, 0, 2 * P); return p; }
function ellipse(cx, cy, rx, ry, a0 = 0, a1 = 2 * P) { const p = new Path2D(); p.ellipse(cx, cy, rx, ry, 0, a0, a1); return p; }
function poly(pts) { const p = new Path2D(); pts.forEach(([x, y], i) => (i ? p.lineTo(x, y) : p.moveTo(x, y))); p.closePath(); return p; }
function svg(d) { return new Path2D(d); }
function many(...paths) { const p = new Path2D(); for (const q of paths) p.addPath(q); return p; }
function mirror(pts) { return pts.map(([x, y]) => [100 - x, y]); }
// A shape scaled about a point, for an inner outline the same shape as the outer one.
function scaled(path, s, cx, cy) { const p = new Path2D(); p.addPath(path, new DOMMatrix([s, 0, 0, s, cx * (1 - s), cy * (1 - s)])); return p; }

// ---- the ten emblems -----------------------------------------------------------------------------------------------
// Each: name, people (one line of world view, from openspec/changes/console-skins/design.md), outline (the outer shape
// class: no two factions share one, rule E4), meaning (the recommendation in the survey), q (its survey id), colours for
// the mark (field, device, ground), for the console crest (a quick stand-in of the skin's material: base, light, dark,
// device, glow) and for the stand-in hull (plate, field, device, emissive device or not), and ops, the drawing.
const DEFS = [
  {
    id: 'concord', name: 'Concord', people: 'A federation of many species that decides by consent',
    outline: 'Ring', q: 'E2', meaning: 'Five founding worlds held as equals inside one ring, round an open centre that any people may join.',
    mark: { field: '#2f62e0', device: '#3fa9f0', ground: '#000000' },
    crest: { base: '#2f62e0', light: '#3fa9f0', dark: '#1b2a6b', device: '#3fa9f0', glow: 0, flat: true },
    hull: { plate: '#e4e6ea', field: '#1d3b8f', device: '#2f62e0', lit: false },
    // The shapes as numbers (grid units), read by these ops and by the 3D render (tools/blender/emblem_concord.py).
    geo: { ring: [38, 46], worlds: { count: 5, orbit: 24, r: 7, first_deg: -90 }, hub: [5, 9] },
    ops: (g) => {
      const worlds = []; for (let i = 0; i < g.worlds.count; i++) { const a = (g.worlds.first_deg * P / 180) + i * 2 * P / g.worlds.count; worlds.push(circle(50 + Math.cos(a) * g.worlds.orbit, 50 + Math.sin(a) * g.worlds.orbit, g.worlds.r)); }
      return [
        { op: 'field', path: many(circle(50, 50, g.ring[1]), circle(50, 50, g.ring[0])) },
        { op: 'device', path: many(...worlds) },
        { op: 'device', path: many(circle(50, 50, g.hub[1]), circle(50, 50, g.hub[0])) },
      ];
    },
  },
  {
    id: 'kethran', name: 'Kethran Hegemony', people: 'An empire of rank and conquest: order is strength',
    outline: 'Heater shield', q: 'E3', meaning: 'The prow of the Hegemony between two blades: the empire leads, its great houses flank it.',
    mark: { field: '#8a9096', device: '#d9483b', ground: '#1a1c1f' },
    crest: { base: '#4a4f55', light: '#8a9096', dark: '#2b2f33', device: '#d9483b', glow: 0.25 },
    hull: { plate: '#5d6266', field: '#2b2f33', device: '#b8342a', lit: false },
    ops: () => [
      { op: 'field', path: svg('M18 6 L82 6 L92 16 L92 48 Q92 78 50 96 Q8 78 8 48 L8 16 Z') },
      { op: 'device', path: poly([[50, 16], [60, 50], [50, 86], [40, 50]]) },
      { op: 'device', path: many(poly([[17, 24], [30, 44], [30, 58], [17, 38]]), poly(mirror([[17, 24], [30, 44], [30, 58], [17, 38]]))) },
    ],
  },
  {
    id: 'vos', name: 'Vos Syndicate', people: 'Free clans of raiders: luck, loot, loyalty to the crew',
    outline: 'Bitten coin', q: 'E4', meaning: 'A stolen coin with a bite out of it, the crew\'s spiral struck on one face and raked by three claws.',
    mark: { field: '#d6a043', device: '#3fb7a3', ground: '#24110d' },
    crest: { base: '#d6a043', light: '#efc56a', dark: '#a8482a', device: '#2a1410', glow: 0 },
    hull: { plate: '#6b4a35', field: '#d6a043', device: '#2a1410', lit: false },
    ops: () => {
      const sp = new Path2D(); const turns = 2.2, cx = 34, cy = 56, r0 = 2, r1 = 20;
      for (let i = 0; i <= 90; i++) { const t = i / 90, a = t * turns * 2 * P, r = r0 + (r1 - r0) * t; const x = cx + Math.cos(a) * r, y = cy + Math.sin(a) * r; i ? sp.lineTo(x, y) : sp.moveTo(x, y); }
      const claw = (dx) => poly([[63 + dx, 18 + dx * 0.4], [67 + dx, 34], [72 + dx, 58], [74 + dx, 84 - dx * 0.6], [68 + dx, 60], [62 + dx, 36]]);
      return [
        { op: 'field', path: circle(50, 50, 44) },
        { op: 'cut', path: circle(92, 16, 17) },
        { op: 'device', path: sp, stroke: 6 },
        { op: 'cut', path: many(claw(0), claw(9), claw(18)) },
      ];
    },
  },
  {
    id: 'vhorr', name: 'Vhorr Brood', people: 'The brood is one body; it grows, consumes and adapts',
    outline: 'Egg in ribs', q: 'E5', meaning: 'The brood\'s egg cradled in its ribs: one body, always growing.',
    mark: { field: '#e2d6bb', device: '#a6e34a', ground: '#120a10' },
    crest: { base: '#6a4a68', light: '#c9a8c4', dark: '#2b1d2a', device: '#e4ff9a', glow: 0.9 },
    hull: { plate: '#3a2a35', field: '#c9b98f', device: '#a6e34a', lit: true },
    ops: () => {
      const rib = (rx, ry, a0, a1) => ellipse(50, 52, rx, ry, a0, a1);
      return [
        { op: 'field', path: many(rib(30, 38, P * 0.6, P * 1.4), rib(30, 38, -P * 0.4, P * 0.4)), stroke: 7 },
        { op: 'field', path: many(rib(41, 46, P * 0.7, P * 1.3), rib(41, 46, -P * 0.3, P * 0.3)), stroke: 6 },
        { op: 'device', path: ellipse(50, 52, 19, 27) },
      ];
    },
  },
  {
    id: 'aelith', name: 'Aelith Conclave', people: 'Patient knowledge kept over aeons; act rarely, act exactly',
    outline: 'Drop', q: 'E6', meaning: 'A triangle of light held in a drop of gold: knowledge kept safe in something that lasts.',
    mark: { field: '#c9a646', device: '#6ff3ff', ground: '#04141a' },
    crest: { base: '#c9a646', light: '#f6e2a0', dark: '#6a4a14', device: '#6ff3ff', glow: 0.8 },
    hull: { plate: '#d8cfae', field: '#8a6a22', device: '#2fb8c8', lit: true },
    ops: () => {
      const drop = svg('M50 4 C78 4 93 26 93 46 C93 68 70 84 50 97 C30 84 7 68 7 46 C7 26 22 4 50 4 Z');
      return [
        { op: 'field', path: many(drop, scaled(drop, 0.8, 50, 50)) },
        { op: 'device', path: poly([[30, 33], [70, 33], [50, 70]]) },
        { op: 'cut', path: poly([[50, 39], [55, 45], [50, 53], [45, 45]]) },
      ];
    },
  },
  {
    id: 'tarsine', name: 'Tarsine Remnant', people: 'A fallen empire that remembers being first and will not change',
    outline: 'Stepped throne', q: 'E7', meaning: 'The stepped throne under the first star: they were first, and they remember it.',
    mark: { field: '#a88a46', device: '#d6c76a', ground: '#1c1814' },
    crest: { base: '#a88a46', light: '#d8bc6e', dark: '#7a3b1c', device: '#d6c76a', glow: 0.35 },
    hull: { plate: '#4a3b30', field: '#a88a46', device: '#d6c76a', lit: false },
    ops: () => {
      const star = []; for (let i = 0; i < 8; i++) { const a = -P / 2 + i * P / 4, r = i % 2 ? 4 : 13; star.push([50 + Math.cos(a) * r, 17 + Math.sin(a) * r]); }
      return [
        { op: 'field', path: poly([[6, 92], [94, 92], [94, 72], [80, 72], [80, 54], [66, 54], [66, 36], [34, 36], [34, 54], [20, 54], [20, 72], [6, 72]]) },
        { op: 'cut', path: svg('M43 92 L43 77 A7 7 0 0 1 57 77 L57 92 Z') },
        { op: 'device', path: poly(star) },
      ];
    },
  },
  {
    id: 'krai', name: 'Krai Warhost', people: 'War is the only honest test; rank is won in blood',
    outline: 'Triangle', q: 'E8', meaning: 'The bayonet driven into the war host\'s triangle, between two fangs: rank won in blood.',
    mark: { field: '#b3121b', device: '#c9d0d8', ground: '#15100f' },
    crest: { base: '#2a2526', light: '#6a5a5a', dark: '#15100f', device: '#ff2a2a', glow: 0.7 },
    hull: { plate: '#2c3646', field: '#9e1420', device: '#c08a4a', lit: false },
    ops: () => [
      { op: 'field', path: poly([[50, 2], [96, 94], [4, 94]]) },
      { op: 'device', path: poly([[47, 32], [53, 32], [58, 58], [55, 73], [47, 84], [49, 72], [44, 67], [47, 63], [42, 57], [45, 52], [41, 46], [45, 41]]) },
      { op: 'cut', path: many(poly([[22, 94], [34, 94], [28, 79]]), poly(mirror([[22, 94], [34, 94], [28, 79]]))) },
    ],
  },
  {
    id: 'ombrai', name: 'Ombrai', people: 'Hidden purpose; they watch, and are seldom seen',
    outline: 'Crescent and eye', q: 'E9', meaning: 'An eye held in a crescent: they watch from the dark, and are seldom seen.',
    mark: { field: '#c58cff', device: '#ff4fb0', ground: '#120822' },
    crest: { base: '#3b1763', light: '#c58cff', dark: '#120822', device: '#ff4fb0', glow: 0.9 },
    hull: { plate: '#241a33', field: '#7a4fb0', device: '#ff4fb0', lit: true },
    ops: () => [
      { op: 'field', path: many(circle(50, 58, 39), circle(50, 43, 35)) },
      { op: 'device', path: svg('M20 40 Q50 15 80 40 Q50 65 20 40 Z') },
      { op: 'cut', path: ellipse(50, 40, 4.5, 10) },
    ],
  },
  {
    id: 'kelvane', name: 'Kelvane Institute', people: 'Truth is measured, never assumed',
    outline: 'Gauge arch', q: 'E10', meaning: 'A tuning fork standing in a gauge, over the wave it sounds: truth measured, never assumed.',
    mark: { field: '#b0915a', device: '#4dff88', ground: '#1c1a16' },
    crest: { base: '#c9bfa9', light: '#f2ead6', dark: '#7a6e58', device: '#b0915a', glow: 0 },
    hull: { plate: '#d9d0bb', field: '#8a6a3a', device: '#2f6a46', lit: false },
    ops: () => {
      const ticks = new Path2D(); for (let i = 1; i < 6; i++) { const a = P + i * P / 6; ticks.moveTo(50 + Math.cos(a) * 33, 70 + Math.sin(a) * 33); ticks.lineTo(50 + Math.cos(a) * 41, 70 + Math.sin(a) * 41); }
      const wave = new Path2D(); for (let i = 0; i <= 60; i++) { const x = 12 + i * 76 / 60, y = 89 + Math.sin(i / 60 * 4 * P) * 4; i ? wave.lineTo(x, y) : wave.moveTo(x, y); }
      return [
        { op: 'field', path: svg('M4 70 A46 46 0 0 1 96 70 L84 70 A34 34 0 0 0 16 70 Z') },
        { op: 'cut', path: ticks, stroke: 2.5 },
        { op: 'device', path: svg('M43 42 L43 60 Q43 68 50 68 Q57 68 57 60 L57 42'), stroke: 5 },
        { op: 'device', path: svg('M50 68 L50 78'), stroke: 5 },
        { op: 'device', path: wave, stroke: 4.5 },
      ];
    },
  },
  {
    id: 'ssarith', name: 'Ssarith Hunt', people: 'The hunt is the only honest sport; the kill is earned and counted',
    outline: 'Target brackets', q: 'E11', meaning: 'A worthy quarry\'s fang held in the hunter\'s brackets, notched once for each kill.',
    mark: { field: '#6dff8a', device: '#d8cfb4', ground: '#1b2218' },
    crest: { base: '#3a4044', light: '#7f8a86', dark: '#1e2224', device: '#d8cfb4', glow: 0.2 },
    hull: { plate: '#3a4a32', field: '#c9c0a2', device: '#e8e0c8', lit: false },
    ops: () => [
      { op: 'field', path: svg('M8 32 L8 8 L32 8 M68 8 L92 8 L92 32 M92 68 L92 92 L68 92 M32 92 L8 92 L8 68'), stroke: 8, square: true },
      { op: 'device', path: svg('M35 20 Q50 13 65 20 Q61 50 50 84 Q39 50 35 20 Z') },
      { op: 'cut', path: svg('M41 30 L59 30 M42 39 L58 39 M44 48 L56 48'), stroke: 3 },
    ],
  },
];
const BY_ID = Object.fromEntries(DEFS.map((d) => [d.id, d]));
const OPS = {}; // id -> ops, built once

function opsOf(id) { return OPS[id] || (OPS[id] = BY_ID[id].ops(BY_ID[id].geo)); }

// Draw one op's shape: filled with even-odd (so a ring is one path), or stroked when it has a stroke width.
function shape(x, o, grow = 0) {
  if (o.stroke) { x.lineWidth = o.stroke + 2 * grow; x.lineCap = o.square ? 'square' : 'round'; x.lineJoin = o.square ? 'miter' : 'round'; x.stroke(o.path); return; }
  x.fill(o.path, 'evenodd');
  if (grow) { x.lineWidth = 2 * grow; x.lineJoin = 'round'; x.stroke(o.path); }
}

// Paint emblem `id` into context x, filling a size x size square at (0, 0). colours: { field, device } (any CSS
// colour), or a function (role, x) returning a fill style (the crest's gradients). The ground is left transparent.
function draw(x, id, size, colours) {
  const ops = opsOf(id); const col = (role) => (typeof colours === 'function' ? colours(role, x) : colours[role]);
  x.save(); x.scale(size / 100, size / 100);
  // The emblem is built on its own layer so its cuts erase only the emblem, never what is under it.
  const c = document.createElement('canvas'); c.width = c.height = Math.ceil(size * 2); const y = c.getContext('2d'); y.scale(c.width / 100, c.height / 100);
  for (const o of ops) {
    if (o.op === 'field') { y.fillStyle = y.strokeStyle = col('field'); shape(y, o); }
    else if (o.op === 'cut') { y.globalCompositeOperation = 'destination-out'; y.fillStyle = y.strokeStyle = '#000'; shape(y, o); y.globalCompositeOperation = 'source-over'; }
    else if (o.op === 'device') {
      y.globalCompositeOperation = 'destination-out'; y.fillStyle = y.strokeStyle = '#000'; shape(y, o, GAP); y.globalCompositeOperation = 'source-over';
      y.fillStyle = y.strokeStyle = col('device'); shape(y, o);
    }
  }
  x.drawImage(c, 0, 0, 100, 100); x.restore();
}

// A canvas holding the emblem at size CSS px (drawn at the device's pixel ratio).
function canvas(id, size, colours, ratio = Math.max(1, Math.min(3, window.devicePixelRatio || 1))) {
  const c = document.createElement('canvas'); c.width = c.height = Math.round(size * ratio); c.style.width = c.style.height = size + 'px';
  draw(c.getContext('2d'), id, c.width, colours); return c;
}

// The console crest at the kit's 96 x 96 slot: a quick stand-in of the skin's material on the same shapes (a lit
// gradient field, a dark rim, the device lit or engraved). The finished crest is rendered by the skin's own generator
// from these shapes (design, "Three renderings, one source").
function crest(id, size = 96) {
  const d = BY_ID[id], k = d.crest, ratio = Math.max(1, Math.min(3, window.devicePixelRatio || 1));
  const c = document.createElement('canvas'); c.width = c.height = Math.round(size * ratio); c.style.width = c.style.height = size + 'px';
  const x = c.getContext('2d'); const S = c.width, pad = S * 0.08, inner = S - 2 * pad;
  const fills = (role, ctx) => {
    if (role === 'device') return k.device;
    if (k.flat) return k.base;
    const g = ctx.createLinearGradient(0, 0, 60, 100); g.addColorStop(0, k.light); g.addColorStop(0.45, k.base); g.addColorStop(1, k.dark); return g;
  };
  // A soft shadow under the whole emblem lifts it off the panel (none on the flat Concord skin).
  x.save(); x.translate(pad, pad);
  if (!k.flat) { x.save(); x.filter = `blur(${S * 0.02}px)`; x.globalAlpha = 0.6; x.translate(S * 0.015, S * 0.025); draw(x, id, inner, { field: '#000', device: '#000' }); x.restore(); }
  if (k.glow) { x.save(); x.filter = `blur(${S * 0.04}px)`; x.globalAlpha = k.glow; draw(x, id, inner, { field: 'rgba(0,0,0,0)', device: k.device }); x.restore(); }
  draw(x, id, inner, fills);
  // A light catching the top edge of the field (rendered skins only).
  if (!k.flat) { x.save(); x.globalCompositeOperation = 'source-atop'; const h = x.createLinearGradient(0, 0, 0, inner * 0.5); h.addColorStop(0, 'rgba(255,255,255,0.35)'); h.addColorStop(1, 'rgba(255,255,255,0)'); x.fillStyle = h; x.fillRect(0, 0, inner, inner * 0.5); x.restore(); }
  x.restore(); return c;
}

return { DEFS, BY_ID, GAP, draw, canvas, crest };
})();
