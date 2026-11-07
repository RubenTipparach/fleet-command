// The Exponent skin (mockup 4, OpenSpec change console-skins): the hybrid of the Axiom, a purely mathematical, fractal
// faction, and the Vhorr Brood (owner, 2026-11-07: the mathematical faction "will merge with the organics creating a new
// hybrid of fractal organic faction"; named by the owner: the Exponent, after growth that compounds by a fixed ratio).
// Provisional culture, ours, to be corrected on the survey: when the Axiom met the Brood, the
// Brood took the Axiom's rules into its growth, and its tissue now grows by rule. It holds several traditions at once:
// phyllotaxis growers (seeds at the golden angle), branch-veiners (L-system veins that branch by exact ratios),
// cell-builders (Voronoi membranes) and shell-turners (chambers along a logarithmic spiral). The design is drawn from
// those ideas, never from any species' body (CLAUDE.md section 1).
//
// First pass, painted in canvas like the Brood's first pass: translucent plum membrane with a wet sheen, lit from within,
// whose structure is computed by rule. Every scatter comes from the page's seeded generator (FCK.rng, Mulberry32) through
// a Poisson disc (CLAUDE.md section 10). The final version is to be sculpted in Blender on the Organic Panel Bible (no
// nine-slice); for now the kit's nine-slice pieces keep their detail in the margins and their centres plain: Voronoi cells
// in the corners, fibres that run along the stretching edges. Registers itself with the page's skin registry
// (docs/mockups/skins). Our own design: nothing traced from another game.
(function () {
'use strict';
const F = window.FCK, P = F.P;
const PH = { void: '#0b0610', wall: '#1c0a17', membrane: '#3a1830', membraneHi: '#6e2f52', fibre: '#4b2040', glass: '#0a050c',
  gold: '#f0bf4c', goldHot: '#ffe8a6', cyan: '#58dce0', cyanHot: '#d2fcff', pith: '#f2e6d0', dim: '#c9a3b8', magenta: '#ff4a7d', life: '#8ff0b0' };
const PHI = (1 + Math.sqrt(5)) / 2, GA = P * (3 - Math.sqrt(5));   // the golden ratio and the golden angle (137.5 degrees)
const B = Math.log(PHI * PHI) / (2 * P);                             // a shell that grows by phi squared every turn
const RNG = (k) => F.rng(F.hash('exponent.' + k));
function hexA(hex, a) { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; }
function mix(h1, h2, t) {
  const a = parseInt(h1.slice(1), 16), b = parseInt(h2.slice(1), 16), l = (s) => Math.round(((a >> s) & 255) * (1 - t) + ((b >> s) & 255) * t);
  return `rgb(${l(16)},${l(8)},${l(0)})`;
}

// ---- the rules ---------------------------------------------------------------------------------------------------
// A closed path through points, smoothed by quadratic curves between midpoints (a cell's wall has no corners).
function smooth(x, pts) {
  const n = pts.length, m = (i) => [(pts[i % n][0] + pts[(i + 1) % n][0]) / 2, (pts[i % n][1] + pts[(i + 1) % n][1]) / 2];
  x.beginPath(); const s = m(0); x.moveTo(s[0], s[1]);
  for (let i = 1; i <= n; i++) { const p = pts[i % n], q = m(i); x.quadraticCurveTo(p[0], p[1], q[0], q[1]); }
  x.closePath();
}
// The cell-builders: a Poisson disc of sites (Bridson), no two closer than r, from the seeded generator.
function poisson(R, x0, y0, x1, y1, r, k = 24) {
  const cs = r / Math.SQRT2, gw = Math.ceil((x1 - x0) / cs), gh = Math.ceil((y1 - y0) / cs), grid = new Int32Array(gw * gh).fill(-1), pts = [], act = [];
  const add = (p) => { pts.push(p); act.push(p); grid[Math.floor((p[1] - y0) / cs) * gw + Math.floor((p[0] - x0) / cs)] = pts.length - 1; };
  add([x0 + R() * (x1 - x0), y0 + R() * (y1 - y0)]);
  while (act.length) {
    const i = Math.floor(R() * act.length), a = act[i]; let found = false;
    for (let t = 0; t < k && !found; t++) {
      const ang = R() * 2 * P, d = r * (1 + R()), p = [a[0] + Math.cos(ang) * d, a[1] + Math.sin(ang) * d];
      if (p[0] < x0 || p[0] >= x1 || p[1] < y0 || p[1] >= y1) continue;
      const gx = Math.floor((p[0] - x0) / cs), gy = Math.floor((p[1] - y0) / cs); let ok = true;
      for (let yy = Math.max(0, gy - 2); yy <= Math.min(gh - 1, gy + 2) && ok; yy++) for (let xx = Math.max(0, gx - 2); xx <= Math.min(gw - 1, gx + 2); xx++) {
        const j = grid[yy * gw + xx]; if (j >= 0 && Math.hypot(pts[j][0] - p[0], pts[j][1] - p[1]) < r) { ok = false; break; }
      }
      if (ok) { add(p); found = true; }
    }
    if (!found) act.splice(i, 1);
  }
  return pts;
}
// Keep the part of a polygon where nx * x + ny * y <= c.
function clipHalf(poly, nx, ny, c) {
  const out = [];
  for (let i = 0; i < poly.length; i++) {
    const a = poly[i], b = poly[(i + 1) % poly.length], da = nx * a[0] + ny * a[1] - c, db = nx * b[0] + ny * b[1] - c;
    if (da <= 0) out.push(a);
    if ((da < 0 && db > 0) || (da > 0 && db < 0)) { const t = da / (da - db); out.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]); }
  }
  return out;
}
// Each site's Voronoi cell inside a box, by clipping the box with the bisector of every near neighbour.
function voronoi(pts, box, reach) {
  return pts.map((p, i) => {
    let poly = [[box[0], box[1]], [box[2], box[1]], [box[2], box[3]], [box[0], box[3]]];
    for (let j = 0; j < pts.length && poly.length; j++) {
      if (j === i) continue; const q = pts[j], nx = q[0] - p[0], ny = q[1] - p[1];
      if (nx * nx + ny * ny > reach * reach) continue;
      poly = clipHalf(poly, nx, ny, (nx * (p[0] + q[0]) + ny * (p[1] + q[1])) / 2);
    }
    return { p, poly };
  });
}
// Membrane: Voronoi cells of translucent tissue, each lit from inside, walls dark, a wet bead of light on each.
// lit(x, y) gives the light behind a cell, 0 to 1.
function tissue(x, path, box, R, o = {}) {
  const [x0, y0, x1, y1] = box, r = o.cell || 8, pad = r * 1.5;
  x.save(); path(); x.clip();
  x.fillStyle = PH.wall; x.fillRect(x0, y0, x1 - x0, y1 - y0);
  const pts = poisson(R, x0 - pad, y0 - pad, x1 + pad, y1 + pad, r);
  for (const c of voronoi(pts, [x0 - pad, y0 - pad, x1 + pad, y1 + pad], r * 3)) {
    if (c.poly.length < 3) continue;
    const [sx, sy] = c.p, k = o.wallK || 0.8, poly = c.poly.map(([px, py]) => [sx + (px - sx) * k, sy + (py - sy) * k]);
    const lift = o.lit ? Math.max(0, Math.min(1, o.lit(sx, sy))) : 0.25;
    const g = x.createRadialGradient(sx - r * 0.2, sy - r * 0.25, 0, sx, sy, r * 1.05);
    g.addColorStop(0, mix(PH.membraneHi, o.glow || PH.gold, lift * 0.55)); g.addColorStop(0.7, mix(PH.membrane, PH.membraneHi, lift * 0.5)); g.addColorStop(1, PH.membrane);
    x.fillStyle = g; smooth(x, poly); x.fill();
    x.fillStyle = `rgba(255,240,235,${0.1 + lift * 0.18})`; x.beginPath(); x.ellipse(sx - r * 0.3, sy - r * 0.35, r * 0.22, r * 0.11, -0.6, 0, 2 * P); x.fill();
  }
  x.restore();
}
// The branch-veiners' rule, F -> F[+F][-F]: every child is its parent's length over phi, turned by exactly `turn`.
// mono: the main axis runs on and one side branch alternates (F -> F[+F]F), each side branch phi squared shorter.
function lsys(x0, y0, a, len, depth, turn, segs, w = 1.6, mono = false, side = 1) {
  const x1 = x0 + Math.cos(a) * len, y1 = y0 + Math.sin(a) * len; segs.push([x0, y0, x1, y1, w, depth === 0]);
  if (depth <= 0) return segs;
  if (mono) { lsys(x1, y1, a, len / PHI, depth - 1, turn, segs, w * 0.85, true, -side); lsys(x1, y1, a + side * turn, len / (PHI * PHI), depth - 1, turn, segs, w * 0.62, true, side); }
  else { lsys(x1, y1, a + turn, len / PHI, depth - 1, turn, segs, w * 0.7); lsys(x1, y1, a - turn, len / PHI, depth - 1, turn, segs, w * 0.7); }
  return segs;
}
// Veins carry light: a soft glow, the vessel, a hot thread down its middle, and a seed at every tip.
function veins(x, segs, col = PH.gold, hot = PH.goldHot, k = 1) {
  x.save(); x.lineCap = 'round';
  for (const [a, b, c, d, w] of segs) { x.strokeStyle = hexA(col, 0.22 * k); x.lineWidth = w + 3.2; x.beginPath(); x.moveTo(a, b); x.lineTo(c, d); x.stroke(); }
  for (const [a, b, c, d, w] of segs) { x.strokeStyle = hexA(col, 0.9 * k); x.lineWidth = w; x.beginPath(); x.moveTo(a, b); x.lineTo(c, d); x.stroke(); }
  for (const [a, b, c, d, w] of segs) { x.strokeStyle = hexA(hot, 0.8 * k); x.lineWidth = w * 0.35; x.beginPath(); x.moveTo(a, b); x.lineTo(c, d); x.stroke(); }
  for (const [, , c, d, w, tip] of segs) if (tip) { x.fillStyle = hexA(hot, 0.9 * k); x.beginPath(); x.arc(c, d, w * 0.9 + 0.5, 0, 2 * P); x.fill(); }
  x.restore();
}
// A straight vein (along a stretching edge) drawn the same three ways.
function vline(x, a, b, c, d, w, col = PH.gold, hot = PH.goldHot, k = 1) { veins(x, [[a, b, c, d, w, false]], col, hot, k); }
// The phyllotaxis growers: seed k at radius c sqrt(k) and angle k times the golden angle. col(t) colours seed k of n.
function seeds(x, cx, cy, n0, n1, c, sz, col, glow = 0) {
  x.save();
  for (let k = n0; k < n1; k++) {
    const r = c * Math.sqrt(k), t = k * GA, px = cx + Math.cos(t) * r, py = cy + Math.sin(t) * r, s = sz * (0.55 + 0.45 * Math.sqrt(k / n1));
    if (glow) { x.shadowColor = col(k / n1); x.shadowBlur = glow; }
    x.fillStyle = col(k / n1); x.beginPath(); x.ellipse(px, py, s, s * 0.78, t, 0, 2 * P); x.fill();
  }
  x.restore();
}
// The shell-turners: a logarithmic spiral r = R exp(B (t - tMax)), turned by rot, its chambers parted by curved septa.
// state: 0 dim, 1 septa lit, 2 chambers filled with light.
function shell(x, cx, cy, R, rot, state, o = {}) {
  const N = o.septa || 9, rad = (t) => R * Math.exp(B * t), pt = (t) => [cx + Math.cos(t + rot) * rad(t), cy + Math.sin(t + rot) * rad(t)];
  const spiral = (t0, t1, first) => { const n = Math.max(4, Math.ceil(Math.abs(t1 - t0) / 0.15)); for (let i = 0; i <= n; i++) { const p = pt(t0 + (t1 - t0) * i / n); if (first && i === 0) x.moveTo(p[0], p[1]); else x.lineTo(p[0], p[1]); } };
  const sept = (t, rev) => { const a = pt(t), b = pt(t - 2 * P), m = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2], tg = [-Math.sin(t + rot), Math.cos(t + rot)], L = Math.hypot(a[0] - b[0], a[1] - b[1]) * 0.28;
    const c = [m[0] - tg[0] * L, m[1] - tg[1] * L], [s, e] = rev ? [b, a] : [a, b]; x.lineTo(s[0], s[1]); x.quadraticCurveTo(c[0], c[1], e[0], e[1]); };
  // the whole shell: its last whorl and the aperture
  x.save(); x.beginPath(); spiral(-2 * P, 0, true); x.closePath();
  const g = x.createRadialGradient(cx, cy, 0, cx, cy, R); g.addColorStop(0, PH.membraneHi); g.addColorStop(1, PH.membrane);
  x.fillStyle = g; x.fill(); x.clip();
  for (let k = 0; ; k++) {
    const t1 = -k * 2 * P / N, t0 = -(k + 1) * 2 * P / N;
    if (rad(t0 - 2 * P) > 0 && rad(t1) < 0.8) break;
    // chamber k, between septum k+1 and septum k (chamber 0 is the open, living one)
    x.beginPath(); spiral(t0, t1, true); sept(t1, false); spiral(t1 - 2 * P, t0 - 2 * P, false); sept(t0, true); x.closePath();
    if (state === 2) { const f = Math.pow(0.88, k); x.fillStyle = mix(PH.membraneHi, k % 2 ? PH.gold : PH.cyan, 0.35 + 0.6 * f); x.fill(); }
    else { x.fillStyle = k % 2 ? hexA(PH.wall, 0.25) : hexA(PH.membraneHi, 0.25); x.fill(); }
    if (k > 0) {
      x.beginPath(); const a = pt(t1); x.moveTo(a[0], a[1]); sept(t1, false);
      x.strokeStyle = state === 0 ? hexA(PH.dim, o.faint ? 0.25 : 0.45) : state === 1 ? PH.cyanHot : hexA(PH.wall, 0.7); x.lineWidth = state === 1 ? 1.1 : 0.9;
      if (state === 1) { x.shadowColor = PH.cyan; x.shadowBlur = 4; } x.stroke(); x.shadowBlur = 0;
    }
  }
  x.restore();
  // the seam: the spiral itself, wound in from the aperture
  x.save(); x.beginPath(); spiral(-8 * P, 0, true); const a = pt(-2 * P); x.moveTo(pt(0)[0], pt(0)[1]); x.lineTo(a[0], a[1]);
  x.strokeStyle = state === 0 ? hexA(PH.dim, o.faint ? 0.35 : 0.6) : PH.gold; x.lineWidth = state === 0 ? 1 : 1.3;
  if (state) { x.shadowColor = PH.gold; x.shadowBlur = 4; } x.stroke(); x.restore();
}
const seedCol = (a, b) => (t) => mix(a, b, t);

// ---- the kit -----------------------------------------------------------------------------------------------------
// The frame: one Voronoi membrane round the band, brighter at the corners (growing nodes), one vein running round it,
// branching into each corner by the rule, a seed node where it turns; plain glass inside. The image is drawn large
// (420 x 300) so a console panel's edges stretch its cells little.
function frame(x, w, h, o) {
  const { m, band, r, key } = o, R = RNG(key);
  const outer = () => F.rr(x, 1, 1, w - 2, h - 2, r);
  // one membrane of cells round the whole band: the source image is long enough that a console panel barely stretches it
  tissue(x, outer, [0, 0, w, h], R, { cell: o.cell, lit: (px, py) => 0.7 - Math.min(px, py, w - px, h - py) / (band * 1.4) + (Math.min(Math.hypot(px, py), Math.hypot(w - px, py), Math.hypot(px, h - py), Math.hypot(w - px, h - py)) < m * 1.2 ? 0.25 : 0) });
  x.save(); outer(); x.clip();
  x.strokeStyle = 'rgba(255,236,236,0.16)'; x.lineWidth = 1.2; x.beginPath(); x.moveTo(r, 2.5); x.lineTo(w - r, 2.5); x.stroke();
  x.restore();
  // the glass, plain
  const gi = band;
  F.rr(x, gi, gi, w - 2 * gi, h - 2 * gi, o.gr); x.fillStyle = PH.glass; x.fill();
  x.strokeStyle = hexA(PH.gold, 0.32); x.lineWidth = 1; x.stroke();
  // the vein round the band, and the rule branching from it into each corner
  const vi = band / 2;
  F.rr(x, vi, vi, w - 2 * vi, h - 2 * vi, Math.max(4, r - vi)); x.strokeStyle = hexA(PH.gold, 0.2); x.lineWidth = 3.4; x.stroke();
  x.strokeStyle = hexA(PH.gold, 0.85); x.lineWidth = 1.1; x.stroke(); x.strokeStyle = hexA(PH.goldHot, 0.7); x.lineWidth = 0.4; x.stroke();
  x.save(); outer(); x.clip();
  for (const [sx, sy, a] of [[1, 1, -3 * P / 4], [-1, 1, -P / 4], [1, -1, 3 * P / 4], [-1, -1, P / 4]]) {
    const px = sx > 0 ? vi + 3 : w - vi - 3, py = sy > 0 ? vi + 3 : h - vi - 3;
    for (const turn of [0, 1]) {   // two rules per corner, one toward each edge
      const along = a + (turn ? 1 : -1) * sx * sy * P / 2.6;
      veins(x, lsys(px, py, along, m * 0.32, 2, P / 5, [], 0.9, true), PH.gold, PH.goldHot, 0.75);
    }
    seeds(x, px, py, 1, o.node || 13, 1.05, 1.0, seedCol(PH.cyanHot, PH.gold), 3);
  }
  x.restore();
  outer(); x.strokeStyle = '#08030a'; x.lineWidth = 1.4; x.stroke();
}
// The chambered key: a nautilus section seen flat at its left, its open living chamber the key's face, a seed at its
// growing tip. state 0 idle (dim), 1 hover (veins and septa light), 2 pressed (the chambers fill with light).
function keyShape(x, w) {
  x.beginPath(); x.moveTo(37.5, 6); x.lineTo(w - 15, 6); x.arcTo(w - 1.5, 6, w - 1.5, 20, 14); x.lineTo(w - 1.5, 28); x.arcTo(w - 1.5, 42, w - 15, 42, 14);
  x.lineTo(37.5, 42); x.arc(24, 24, 22.5, 0.9273, 2 * P - 0.9273); x.closePath();
}
function key(state) {
  return (x, w, h) => {
    x.save(); x.scale(1, h / 48);
    keyShape(x, w);
    const g = x.createLinearGradient(0, 6, 0, 42); g.addColorStop(0, state === 2 ? '#7a3558' : PH.membraneHi); g.addColorStop(0.5, state === 2 ? '#4e1f3e' : PH.membrane); g.addColorStop(1, PH.wall);
    x.fillStyle = g; x.fill();
    x.save(); keyShape(x, w); x.clip();
    if (state === 2) { const b = x.createLinearGradient(0, 26, 0, 42); b.addColorStop(0, 'rgba(0,0,0,0)'); b.addColorStop(1, hexA(PH.gold, 0.35)); x.fillStyle = b; x.fillRect(0, 0, w, 48); }
    x.strokeStyle = 'rgba(255,236,236,0.2)'; x.lineWidth = 1.2; x.beginPath(); x.moveTo(40, 8.2); x.lineTo(w - 16, 8.2); x.stroke();
    x.restore();
    // the veins along the face, idle dim, lit on hover and press
    const k = state === 0 ? 0.32 : state === 1 ? 0.95 : 1;
    vline(x, 46, 10, w - 18, 10, 1, PH.gold, PH.goldHot, k); vline(x, 46, 39.5, w - 18, 39.5, 0.9, PH.gold, PH.goldHot, k);
    shell(x, 24, 24, 20, 0, state);
    seeds(x, w - 15, 24, 1, 13, 1.6, 1.25, state ? seedCol(PH.cyanHot, PH.gold) : () => hexA(PH.dim, 0.55), state ? 4 : 0);
    keyShape(x, w); x.strokeStyle = '#08030a'; x.lineWidth = 1.5; x.stroke();
    x.restore();
  };
}
// A tab: a plain membrane leaf with a seed node at each end; the active one lit, a vein along its foot.
function tab(on) {
  return (x, w, h) => {
    F.rr(x, 1, 2, w - 2, h - 4, 9);
    const g = x.createLinearGradient(0, 2, 0, h - 2); g.addColorStop(0, on ? '#7a3558' : PH.membrane); g.addColorStop(1, PH.wall); x.fillStyle = g; x.fill();
    x.strokeStyle = 'rgba(255,236,236,0.16)'; x.lineWidth = 1; x.beginPath(); x.moveTo(10, 4); x.lineTo(w - 10, 4); x.stroke();
    vline(x, 14, h - 6, w - 14, h - 6, 1, PH.gold, PH.goldHot, on ? 1 : 0.3);
    for (const cx of [8, w - 8]) seeds(x, cx, h / 2, 1, 8, 1.35, 1.05, on ? seedCol(PH.cyanHot, PH.gold) : () => hexA(PH.dim, 0.5), on ? 3 : 0);
    F.rr(x, 1, 2, w - 2, h - 4, 9); x.strokeStyle = '#08030a'; x.lineWidth = 1.3; x.stroke();
  };
}
// A strip, drawn long so it barely stretches: membrane lips top and bottom, a shell at the left end and a seed disc at the right, glass between.
function strip(flip) {
  return (x, w, h) => {
    const R = RNG('strip' + (flip ? 'b' : 't'));
    x.save(); if (flip) { x.translate(0, h); x.scale(1, -1); }
    const outer = () => F.rr(x, 1, 1, w - 2, h - 2, 18);
    x.save(); outer(); x.clip();
    tissue(x, outer, [0, 0, w, h], R, { cell: 6, lit: (px, py) => Math.max(0.85 - Math.hypot(px - 40, py - 36) / 60, 0.85 - Math.hypot(px - w + 40, py - 36) / 60, 0.2) });
    x.restore();
    F.rr(x, 80, 10, w - 160, h - 20, 10); x.fillStyle = PH.glass; x.fill(); x.strokeStyle = hexA(PH.gold, 0.3); x.lineWidth = 1; x.stroke();
    vline(x, 64, 5, w - 64, 5, 1.1); vline(x, 64, h - 5, w - 64, h - 5, 1.1);
    shell(x, 40, 36, 29, P, 1, { septa: 11 });
    x.beginPath(); x.arc(w - 40, 36, 27, 0, 2 * P); x.fillStyle = hexA(PH.wall, 0.75); x.fill();
    seeds(x, w - 40, 36, 1, 89, 2.75, 1.5, seedCol(PH.cyanHot, PH.gold), 3);
    outer(); x.strokeStyle = '#08030a'; x.lineWidth = 1.4; x.stroke();
    x.restore();
  };
}
// A symbol cell: one Voronoi cell of a fixed seed; off, lit (cyan heart, gold wall) or warning (divergence magenta).
const CELL = (() => { const R = RNG('cell'), o = []; for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3 + (R() - 0.5) * 0.25, r = 5.3 + (R() - 0.5) * 0.9; o.push([6 + Math.cos(a) * r, 7 + Math.sin(a) * r * 1.12]); } return o; })();
function cell(mode) {
  return (x) => {
    smooth(x, CELL);
    if (!mode) { x.fillStyle = PH.membrane; x.fill(); x.strokeStyle = hexA(PH.dim, 0.35); x.lineWidth = 0.8; x.stroke(); x.fillStyle = hexA(PH.dim, 0.35); x.beginPath(); x.arc(6, 7, 1.2, 0, 2 * P); x.fill(); return; }
    const hot = mode === 2 ? '#ffd0dc' : PH.cyanHot, mid = mode === 2 ? PH.magenta : PH.cyan, rim = mode === 2 ? PH.magenta : PH.gold;
    const g = x.createRadialGradient(6, 7, 0, 6, 7, 6.5); g.addColorStop(0, hot); g.addColorStop(0.55, mid); g.addColorStop(1, mix(rim, PH.membrane, 0.35));
    x.fillStyle = g; x.fill(); x.strokeStyle = rim; x.lineWidth = 0.9; x.stroke();
    x.fillStyle = '#ffffff'; x.beginPath(); x.arc(6, 7, 1.3, 0, 2 * P); x.fill();
  };
}
const nine = {
  'frame.panel': { size: [420, 300], m: [30, 30, 30, 30], draw: (x, w, h) => frame(x, w, h, { m: 30, band: 22, r: 22, gr: 9, cell: 6.5, node: 13, key: 'panel' }) },
  'frame.inset': { size: [220, 160], m: [14, 14, 14, 14], draw: (x, w, h) => frame(x, w, h, { m: 14, band: 9, r: 10, gr: 5, cell: 4, node: 6, key: 'inset' }) },
  'bar.track': { size: [96, 22], m: [0, 14, 0, 14], inset: [10, 4], draw: (x, w, h) => {
    F.rr(x, 1, 1, w - 2, h - 2, 9); x.fillStyle = PH.membrane; x.fill();
    F.rr(x, 9, 3.5, w - 18, h - 7, 4); x.fillStyle = PH.glass; x.fill(); x.strokeStyle = hexA(PH.gold, 0.3); x.lineWidth = 0.8; x.stroke();
    for (const cx of [5, w - 5]) seeds(x, cx, h / 2, 1, 5, 1.2, 0.9, seedCol(PH.cyanHot, PH.gold), 2);
    F.rr(x, 1, 1, w - 2, h - 2, 9); x.strokeStyle = '#08030a'; x.lineWidth = 1.2; x.stroke();
  } },
  'bar.fill': { size: [64, 10], m: [0, 8, 0, 8], draw: (x, w, h) => vline(x, 4, h / 2, w - 4, h / 2, 5, PH.cyan, PH.cyanHot) },
  'bar.fill.red': { size: [64, 10], m: [0, 8, 0, 8], draw: (x, w, h) => vline(x, 4, h / 2, w - 4, h / 2, 5, PH.magenta, '#ffd0dc') },
  'bar.cell.off': { size: [12, 14], gap: 2, draw: cell(0) },
  'bar.cell.on': { size: [12, 14], gap: 2, draw: cell(1) },
  'bar.cell.warn': { size: [12, 14], gap: 2, draw: cell(2) },
  'button.idle': { size: [150, 48], m: [24, 30, 24, 48], draw: key(0) },
  'button.hover': { size: [150, 48], m: [24, 30, 24, 48], draw: key(1) },
  'button.pressed': { size: [150, 48], m: [24, 30, 24, 48], draw: key(2) },
  'tab.idle': { size: [120, 40], m: [16, 16, 16, 16], draw: tab(false) },
  'tab.active': { size: [120, 40], m: [16, 16, 16, 16], draw: tab(true) },
  'strip.top': { size: [1200, 72], m: [12, 100, 12, 100], draw: strip(false) },
  'strip.bottom': { size: [1200, 72], m: [12, 100, 12, 100], draw: strip(true) },
  'divider': { size: [128, 10], m: [0, 18, 0, 18], draw: (x, w, h) => {
    vline(x, 10, h / 2, w - 10, h / 2, 1.2, PH.gold, PH.goldHot, 0.75);
    for (const [cx, a] of [[10, P], [w - 10, 0]]) { veins(x, lsys(cx, h / 2, a, 4, 1, P / 4, [], 1), PH.gold, PH.goldHot, 0.9); }
  } },
};

// ---- hero pieces -------------------------------------------------------------------------------------------------
// The helm's eight keys stand on one logarithmic spiral: north nearest the heart, each next key clockwise a fixed ratio
// further out and larger, so the pad reads clockwise like a compass and grows like a shell.
const HG = Math.pow(116 / 90, 1 / 7);
const HELM = [0, 1, 2, 3, 4, 5, 6, 7].map((i) => { const a = -P / 2 + i * P / 4, r = 90 * Math.pow(HG, i); return [150 + Math.cos(a) * r, 150 + Math.sin(a) * r, 21 * Math.pow(HG, i), a]; });
function disc(x, cx, cy, r) { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); }
const hero = {
  nameplate: (x, w, h) => {
    const R = RNG('plate'), outer = () => F.rr(x, 1, 1, w - 2, h - 2, 16);
    tissue(x, outer, [0, 0, w, h], R, { cell: 7, lit: (px) => 0.7 - Math.abs(px - 32) / 120 });
    F.rr(x, 62, 9, 366, 38, 9); x.fillStyle = PH.glass; x.fill(); x.strokeStyle = hexA(PH.gold, 0.5); x.lineWidth = 1; x.stroke();
    disc(x, 32, 28, 23); x.fillStyle = PH.wall; x.fill();
    seeds(x, 32, 28, 1, 89, 2.3, 1.35, seedCol(PH.cyanHot, PH.gold), 3);
    shell(x, 444, 28, 13, P / 2, 1, { septa: 8 });
    x.save(); outer(); x.clip();
    veins(x, lsys(56, 6, 0, 16, 3, P / 5, [], 1, true), PH.gold, PH.goldHot, 0.8); veins(x, lsys(56, 50, 0, 16, 3, -P / 5, [], 1, true, -1), PH.gold, PH.goldHot, 0.8);
    x.restore();
    outer(); x.strokeStyle = '#08030a'; x.lineWidth = 1.5; x.stroke();
  },
  'helm.pad': (x, w) => {
    const R = RNG('helm'), c = w / 2;
    tissue(x, () => disc(x, c, c, 148), [0, 0, w, w], R, { cell: 11, lit: (px, py) => 0.85 - Math.hypot(px - c, py - c) / 170 });
    // eight veins grow out between the keys, each by the same rule
    for (let i = 0; i < 8; i++) { const a = -P / 2 + (i + 0.5) * P / 4; veins(x, lsys(c + Math.cos(a) * 46, c + Math.sin(a) * 46, a, 40, 3, P / 7, [], 1.6), PH.gold, PH.goldHot, 0.75); }
    // the heart: a disc of seeds round a plain glass core, the stop key
    disc(x, c, c, 46); x.fillStyle = PH.wall; x.fill(); x.strokeStyle = hexA(PH.gold, 0.6); x.lineWidth = 1.4; x.stroke();
    seeds(x, c, c, 76, 230, 2.95, 1.6, seedCol(PH.cyan, PH.gold), 2);
    disc(x, c, c, 23); x.fillStyle = PH.glass; x.fill(); x.strokeStyle = hexA(PH.gold, 0.7); x.lineWidth = 1; x.stroke();
    // the chambered keys, each a shell whose aperture faces out
    for (const [kx, ky, kr, a] of HELM) {
      x.save(); x.shadowColor = '#000'; x.shadowBlur = 8; disc(x, kx, ky, kr); x.fillStyle = PH.membrane; x.fill(); x.restore();
      x.save(); disc(x, kx, ky, kr); x.clip(); shell(x, kx, ky, kr * 0.92, a, 0, { faint: true }); x.restore();
      disc(x, kx, ky, kr); x.strokeStyle = hexA(PH.gold, 0.75); x.lineWidth = 1.4; x.stroke();
      disc(x, kx, ky, kr + 1.6); x.strokeStyle = '#08030a'; x.lineWidth = 1.4; x.stroke();
      x.fillStyle = 'rgba(255,240,235,0.22)'; x.beginPath(); x.ellipse(kx - kr * 0.35, ky - kr * 0.45, kr * 0.3, kr * 0.12, -0.6, 0, 2 * P); x.fill();
    }
    disc(x, c, c, 148); x.strokeStyle = '#08030a'; x.lineWidth = 1.6; x.stroke();
  },
  'ssd.bezel': (x, w, h) => {
    const R = RNG('ssd'), outer = () => F.rr(x, 2, 2, w - 4, h - 4, 30);
    tissue(x, outer, [0, 0, w, h], R, { cell: 10, lit: (px, py) => 0.75 - Math.min(Math.hypot(px - 30, py - 240), Math.hypot(px - 430, py - 60)) / 200 });
    F.rr(x, 58, 16, w - 116, h - 32, 46); x.fillStyle = PH.glass; x.fill(); x.strokeStyle = hexA(PH.gold, 0.55); x.lineWidth = 1.2; x.stroke();
    x.save(); outer(); x.clip();
    veins(x, lsys(30, 215, -P / 2, 70, 4, P / 6, [], 1.6, true), PH.gold, PH.goldHot, 0.8);
    veins(x, lsys(w - 30, 86, P / 2, 70, 4, P / 6, [], 1.6, true, -1), PH.gold, PH.goldHot, 0.8);
    x.restore();
    shell(x, 30, 246, 24, -P / 2, 1, { septa: 10 });
    disc(x, w - 30, 54, 24); x.fillStyle = PH.wall; x.fill(); seeds(x, w - 30, 54, 1, 89, 2.4, 1.4, seedCol(PH.cyanHot, PH.gold), 3);
    outer(); x.strokeStyle = '#08030a'; x.lineWidth = 1.6; x.stroke();
  },
  dial: (x, w) => {
    const R = RNG('dial'), c = w / 2;
    tissue(x, () => disc(x, c, c, 58), [0, 0, w, w], R, { cell: 6, lit: (px, py) => 0.8 - Math.hypot(px - c, py - c) / 90 });
    disc(x, c, c, 45); x.fillStyle = PH.glass; x.fill(); x.strokeStyle = hexA(PH.gold, 0.65); x.lineWidth = 1.2; x.stroke();
    for (let i = 0; i < 8; i++) { const a = i * P / 4; seeds(x, c + Math.cos(a) * 51.5, c + Math.sin(a) * 51.5, 1, 4, 1.1, 1.1, seedCol(PH.cyanHot, PH.gold), 2); }
    disc(x, c, c, 58); x.strokeStyle = '#08030a'; x.lineWidth = 1.4; x.stroke();
  },
  'lamp.alert.green': (x, w) => lamp(x, w, PH.life), 'lamp.alert.yellow': (x, w) => lamp(x, w, PH.gold), 'lamp.alert.red': (x, w) => lamp(x, w, PH.magenta),
  // The Exponent's mark (original): a disc of seeds set at the golden angle inside a ring that branches by the rule.
  crest: (x, w) => {
    const c = w / 2;
    disc(x, c, c, 46); x.fillStyle = PH.void; x.fill();
    disc(x, c, c, 38); x.strokeStyle = hexA(PH.gold, 0.25); x.lineWidth = 4; x.stroke(); x.strokeStyle = PH.gold; x.lineWidth = 1.4; x.stroke();
    for (let i = 0; i < 8; i++) {
      const a = -P / 2 + i * P / 4, px = c + Math.cos(a) * 38, py = c + Math.sin(a) * 38;
      veins(x, lsys(px, py, a, 5, 1, P / 5, [], 1.4), PH.gold, PH.goldHot); veins(x, lsys(px, py, a + P, 5, 1, P / 5, [], 1.2), PH.gold, PH.goldHot, 0.7);
    }
    seeds(x, c, c, 1, 144, 2.15, 1.45, seedCol(PH.goldHot, PH.cyan), 3);
  },
};
function lamp(x, w, col) {
  const c = w / 2;
  disc(x, c, c, 29); x.fillStyle = PH.membrane; x.fill();
  const g = x.createRadialGradient(c, c, 0, c, c, 28); g.addColorStop(0, hexA(col, 0.55)); g.addColorStop(1, 'rgba(0,0,0,0)'); x.fillStyle = g; x.fill();
  seeds(x, c, c, 1, 89, 2.7, 1.55, (t) => mix('#ffffff', col, Math.min(1, 0.25 + t)), 4);
  disc(x, c, c, 29); x.strokeStyle = hexA(PH.gold, 0.6); x.lineWidth = 1.2; x.stroke(); disc(x, c, c, 30.5); x.strokeStyle = '#08030a'; x.lineWidth = 1.3; x.stroke();
}

// ---- icons: pictures of the job, drawn as grown lines ----------------------------------------------------------------
// Every stroke tapers from root to tip like a vein, and every joint and tip is a seed. One ink (gold): colour is for state.
function icon(name) {
  return F.paint(48, 48, (x) => {
    const ink = PH.gold;
    x.strokeStyle = ink; x.fillStyle = ink; x.lineCap = 'round'; x.lineJoin = 'round';
    const T = (pts, w0 = 3.6, w1 = 1.8) => { for (let i = 1; i < pts.length; i++) { x.lineWidth = w0 + (w1 - w0) * i / (pts.length - 1); x.beginPath(); x.moveTo(pts[i - 1][0], pts[i - 1][1]); x.lineTo(pts[i][0], pts[i][1]); x.stroke(); } };
    const S = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); };
    const A = (cx, cy, r, a0, a1, lw = 3) => { x.lineWidth = lw; x.beginPath(); x.arc(cx, cy, r, a0, a1); x.stroke(); };
    ({
      // a ship's wheel: a rim, eight spokes grown from the hub, a seed at each handle
      helm: () => { A(24, 24, 12, 0, 2 * P, 3); S(24, 24, 4.5); for (let i = 0; i < 8; i++) { const a = i * P / 4, c = Math.cos(a), s = Math.sin(a); T([[24 + c * 4, 24 + s * 4], [24 + c * 19, 24 + s * 19]], 3, 1.8); S(24 + c * 20, 24 + s * 20, 2.6); } },
      // a target: two rings and a seed at its heart
      fire: () => { A(24, 24, 18, 0, 2 * P, 3); A(24, 24, 10, 0, 2 * P, 2.6); S(24, 24, 4); for (const a of [0, P / 2, P, 1.5 * P]) T([[24 + Math.cos(a) * 18, 24 + Math.sin(a) * 18], [24 + Math.cos(a) * 23, 24 + Math.sin(a) * 23]], 3, 1.6); },
      // six shield facings round the hull
      shields: () => { for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; A(24, 24, 18, a - P / 6 + 0.17, a + P / 6 - 0.17, 4.2); } S(24, 24, 5.5); },
      // an emitter's widening beam drawing a crate in
      tractor: () => { S(8, 24, 5); T([[12, 21], [32, 11]], 3, 1.6); T([[12, 27], [32, 37]], 3, 1.6); A(8, 24, 13, -0.4, 0.4, 2.2); A(8, 24, 19, -0.38, 0.38, 2.2); x.lineWidth = 3; x.strokeRect(34, 16, 10, 16); },
      // a figure, whole above, dissolving into slices below
      transport: () => { S(24, 9, 5.5); x.beginPath(); x.moveTo(14, 36); x.lineTo(15, 21); x.quadraticCurveTo(24, 15, 33, 21); x.lineTo(34, 36); x.closePath(); x.fill(); x.fillRect(16, 35, 6, 9); x.fillRect(26, 35, 6, 9); x.globalCompositeOperation = 'destination-out'; for (let y = 26, k = 0; y < 46; y += 3.4, k++) x.fillRect(0, y, 48, 1 + k * 0.32); x.globalCompositeOperation = 'source-over'; },
      // an atom: three orbits and a nucleus seed
      science: () => { x.lineWidth = 2.6; for (const r of [0, P / 3, -P / 3]) { x.beginPath(); x.ellipse(24, 24, 20, 7.5, r, 0, 2 * P); x.stroke(); } S(24, 24, 4.2); },
      // a dish on its stand, sending
      comms: () => { x.lineWidth = 3; x.beginPath(); x.arc(17, 27, 12, P * 0.75, P * 1.75, true); x.closePath(); x.stroke(); T([[17, 27], [12, 44]], 3, 2); T([[17, 27], [22, 44]], 3, 2); S(26, 18, 2.6); A(26, 18, 8, -P / 2, 0, 2.6); A(26, 18, 15, -P / 2, 0, 2.6); },
      // a wrench
      repair: () => { T([[11, 38], [27, 22]], 6, 4.4); A(31, 17, 9, -P / 4 + 0.95, -P / 4 - 0.95 + 2 * P, 5); S(11, 38, 3.2); },
      // a star map: three systems joined by grown lanes
      map: () => { for (const [a, b] of [[[12, 14], [36, 20]], [[36, 20], [20, 36]], [[20, 36], [12, 14]]]) T([a, b], 2.6, 1.4); for (const [cx, cy] of [[12, 14], [36, 20], [20, 36]]) { S(cx, cy, 5); } },
      // a gear: a ring and its teeth
      settings: () => { A(24, 24, 9, 0, 2 * P, 3.4); for (let i = 0; i < 8; i++) { const a = i * P / 4; T([[24 + Math.cos(a) * 12, 24 + Math.sin(a) * 12], [24 + Math.cos(a) * 19, 24 + Math.sin(a) * 19]], 5, 3.4); } S(24, 24, 3); },
    })[name]();
  });
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('exponent', {
  name: 'Exponent', kind: 'Fractal growth in living tissue, painted in canvas (first pass): Voronoi membrane, L-system veins, golden-angle seeds, chambered spiral keys', bg: PH.void, sampleBg: '#1e0c1a',
  css: { '--s-ink': PH.pith, '--s-dim': PH.dim, '--s-num': PH.cyan, '--s-body': '"Jura", sans-serif', '--btn-ink': PH.pith, '--btn-shadow': '0 1px 2px #000, 0 0 6px rgba(240,191,76,0.35)', '--btn-justify': 'center',
    '--btn-font': '600 17px/1 "Fraunces", serif', '--btn-track': '0.03em', '--btn-case': 'none', '--btn-motion': 'padding-top 0.06s', '--btn-sink': '1px', '--btn-pb': '3px',
    '--tab-font': '600 14px/1 "Fraunces", serif', '--tab-ink': PH.dim, '--tab-ink-on': PH.goldHot, '--title-font': '600 19px/1 "Fraunces", serif', '--title-ink': PH.gold,
    '--lbl-font': '700 15px/1 "Jura", sans-serif', '--num-font': '700 26px/1 "Jura", sans-serif',
    // The strips are 72 px; their open band is the glass between the fibre lips, rows 10-62.
    '--stt-pt': '12px', '--stt-pb': '12px', '--stb-pt': '12px', '--stb-pb': '12px', '--strip-lamp': '44px' },
  accent: PH.cyan, warn: PH.magenta,
  // The frame's band (22 px) holds the vein and its cells, so the title heads the glass, in gold.
  title: (t) => { t.style.cssText = 'position:relative;justify-self:start;align-self:flex-start;margin-left:2px;text-shadow:0 1px 2px #000;'; },
  plateText: { left: 72, top: 13, width: 344, height: 30, font: '600 21px/1 "Fraunces", serif', color: PH.pith, shadow: '0 1px 2px #000', sub: 'Third Whorl · Tender Ossaine' },
  helmKeys: HELM.map(([px, py]) => [Math.round(px * 10) / 10, Math.round(py * 10) / 10]), shield: [PH.cyan, PH.gold, PH.magenta],
  fonts: ['600 20px "Fraunces"', '700 20px "Jura"'], view: { title: '600 28px "Fraunces"', body: '700 24px "Jura"', upper: false },
  build: () => F.fromPainters(nine, hero, icon, false),
  live: {
    // A grown arrow: a tapered shaft, two barbs branched by the rule, a seed at the tip.
    arrow: (x) => {
      for (const [col, k] of [['#08030a', 2.2], [PH.pith, 0]]) {
        x.strokeStyle = col; x.lineCap = 'round';
        x.lineWidth = 3.4 + k; x.beginPath(); x.moveTo(-10, 0); x.lineTo(8, 0); x.stroke();
        x.lineWidth = 2.4 + k; for (const s of [-1, 1]) { x.beginPath(); x.moveTo(8, 0); x.lineTo(8 - Math.cos(P / 5) * 10 / PHI * 1.4, s * Math.sin(P / 5) * 10 / PHI * 1.4); x.stroke(); }
        x.fillStyle = col; x.beginPath(); x.arc(10, 0, 2.6 + k / 2, 0, 2 * P); x.fill();
      }
    },
    helmCentre: (x, c) => { x.fillStyle = PH.goldHot; x.font = '700 13px Jura'; x.textAlign = 'center'; x.fillText('STOP', c, c + 4); },
    // A grown hull: a lens of tissue whose chambers part along its spine, a vein to each mount; shields are rows of seeds.
    ssd: (x, cx, cy, sh, col) => {
      for (let i = 0; i < 6; i++) {
        const a = -P / 2 + i * P / 3; x.save(); x.shadowColor = col(sh[i]); x.shadowBlur = 6; x.fillStyle = col(sh[i]);
        for (let k = -4; k <= 4; k++) { const t = a + k * (P / 3 - 0.2) / 8; x.beginPath(); x.arc(cx + Math.cos(t) * 100, cy + Math.sin(t) * 100, 3.6 - Math.abs(k) * 0.25, 0, 2 * P); x.fill(); }
        x.restore();
      }
      const hull = []; for (let i = 0; i < 24; i++) { const t = i / 24 * 2 * P, s = Math.sin(t), c = Math.cos(t); hull.push([cx + c * (34 + 10 * Math.cos(2 * t)) * (1 - 0.25 * s), cy + s * 72]); }
      smooth(x, hull); const g = x.createLinearGradient(cx - 40, 0, cx + 40, 0); g.addColorStop(0, PH.membrane); g.addColorStop(0.5, PH.membraneHi); g.addColorStop(1, PH.membrane);
      x.fillStyle = g; x.fill(); x.strokeStyle = PH.gold; x.lineWidth = 1.6; x.stroke();
      x.save(); smooth(x, hull); x.clip(); x.strokeStyle = hexA(PH.dim, 0.6); x.lineWidth = 1;
      for (let k = 0; k < 7; k++) { const y = cy - 60 + 60 * (1 - Math.pow(1 / PHI, k * 0.6)) * 2; x.beginPath(); x.moveTo(cx - 50, y + 6); x.quadraticCurveTo(cx, y - 6, cx + 50, y + 6); x.stroke(); }
      x.restore();
      vline(x, cx, cy - 66, cx, cy + 66, 1.4, PH.gold, PH.goldHot);
      for (const [mx, my] of [[cx, cy - 56], [cx - 30, cy + 8], [cx + 30, cy + 8], [cx, cy + 52]]) {
        vline(x, cx, my, mx, my, 1, PH.gold, PH.goldHot, 0.8);
        x.save(); x.shadowColor = PH.cyan; x.shadowBlur = 8; seeds(x, mx, my, 1, 13, 1.5, 1.2, seedCol(PH.cyanHot, PH.cyan)); x.restore();
      }
    },
    // A sweep wound as a logarithmic spiral arm; contacts as seeds, coloured by nearness (cyan far, gold, magenta near).
    dial: (x, c, cts) => {
      x.save(); x.strokeStyle = hexA(PH.cyan, 0.7); x.lineWidth = 1.6; x.beginPath();
      for (let i = 0; i <= 40; i++) { const t = -2 * P + i / 40 * 2 * P, r = 42 * Math.exp(B * t); const p = [c + Math.cos(t - 0.9) * r, c + Math.sin(t - 0.9) * r]; if (i) x.lineTo(p[0], p[1]); else x.moveTo(p[0], p[1]); }
      x.stroke(); x.restore();
      for (const r of [14, 28]) { x.strokeStyle = hexA(PH.gold, 0.22); x.lineWidth = 1; x.beginPath(); x.arc(c, c, r, 0, 2 * P); x.stroke(); }
      for (const [dx, dy] of cts) {
        const d = Math.hypot(dx, dy), cl = d < 0.56 ? PH.magenta : d < 0.8 ? PH.gold : PH.cyan;
        x.save(); x.shadowColor = cl; x.shadowBlur = 7; x.fillStyle = cl; x.beginPath(); x.arc(c + dx * 34, c + dy * 34, d < 0.56 ? 4.5 : 3.6, 0, 2 * P); x.fill(); x.restore();
      }
    },
    // Four corners grown by the rule: a vein down each edge, side branches reaching out, a seed node at the corner.
    bracket: (x, bx, by, bw, bh) => {
      for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
        x.save(); x.translate(px, py); x.scale(sx, sy);
        veins(x, lsys(0, 0, 0, 28, 3, -P / 4, [], 2.4, true, -1), PH.gold, PH.goldHot);
        veins(x, lsys(0, 0, P / 2, 28, 3, P / 4, [], 2.4, true, 1), PH.gold, PH.goldHot);
        seeds(x, 0, 0, 1, 21, 1.35, 1.2, seedCol(PH.cyanHot, PH.gold), 4);
        x.restore();
      }
    },
  },
  world: ['The Brood took the Axiom\'s rules into its growth: what grows by rule is kept; what grows without one is pruned.', 'Provisional and the owner\'s to set; the design draws on none of it. Four traditions shape its things: seed growers, branch veiners, cell builders, shell turners.', 'Tissue grown to a rule: veins that branch by exact ratios, membranes parted into cells, seeds at the golden angle, chambers along a logarithmic spiral.', 'Grown hulls whose chambers wind out along one spiral, each sealed behind a new septum as the ship grows.'],
  shapes: 'Chambered keys (a shell section seen flat, its open chamber the key\'s face), Voronoi membrane at the corners, fibres along the edges, L-system veins, golden-angle seed discs.',
  light: 'Lit from within: the veins carry the Axiom\'s gold and the seeds its cyan through translucent plum membrane under a wet sheen.',
  motion: 'Hover lights a key\'s veins and septa; pressed, its chambers fill with light and the label sinks a pixel; the membrane stays put.',
  type: '"Fraunces", a soft serif on a strict grid, for keys and titles over "Jura" numbers.',
  palette: [['Membrane', PH.membrane, 'Every frame, key bodies', 'The Brood\'s one body, still growing'], ['Cell wall', PH.wall, 'Walls between cells, rims', 'The rule that parts growth into order'],
    ['Pith', PH.pith, 'Text, arrows', 'The settled core: what the growth has proven'], ['Rule gold', PH.gold, 'Veins, titles, icons', 'Convergence: a rule that settles'],
    ['Seed cyan', PH.cyan, 'Lit seeds and cells, readouts', 'Iteration: growth counted step by step'], ['Divergence', PH.magenta, 'Alerts only', 'Growth that obeys no rule']],
});
})();
