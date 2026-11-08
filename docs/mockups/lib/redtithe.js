// Fleet Command mockups: the Red Tithe skin (openspec/changes/console-skins/design.md, "Three more factions" and "the Red
// Tithe in wood"; openspec/changes/redtithe-battle-console), shared by the console skins page (docs/mockups/skins/redtithe.js
// registers it there) and the battle console (docs/mockups/battle-console.html). One implementation of the Tithe's look
// (CLAUDE.md section 6.1): its materials, trophies, kit painters, hero pieces, icons and live readouts, and the fields the
// battle console reads (the same as docs/mockups/lib/concord.js). Needs docs/mockups/lib/kitdraw.js inlined first.
//
// The Red Tithe (mockup 4, OpenSpec change console-skins): a second, more brutal pirate faction (owner,
// 2026-11-07: "Id also like another pirate faction. One more brutal. Decorate their UI with sharp teeth, bullets and
// blades. Blood stained buttons and crusty paper with burn marks and drops of blood and grime on them"). Name and culture
// are provisional, ours, and go to the survey: raiders who take a tithe of blood from every crew they board, whose orders
// are written on bounty sheets and pinned up with knives, and whose captains count their standing in trophies (teeth
// pulled from beaten crews of every species, spent cartridges, broken blades). Three traditions inside it, each named
// where it shapes a piece: the sheet-keepers (paper, ledgers, burned seals), the gunners (bandoliers, brass, the revolver
// cylinder of the helm) and the cutters (blades, the trap-jaw keys). Nothing here is drawn from how the Tithe look
// (CLAUDE.md section 1): the teeth are other crews' trophies, never their own.
//
// This first pass is painted in canvas: crusted, stained and scorched paper nailed to the dark, scarred oak planks of the
// Tithe's hall (owner, 2026-11-07: "More brown wood on the red tithe"), strap iron and rivets where steel is still needed,
// the trophies shaded by hand. A later pass models the wood and metal (planks, jaws, knives, cartridges, teeth) in Blender and paints the paper in Krita
// (krita-brushwork); until then this module is the one implementation. Every speck, stain, tear, drop and scratch comes
// from the seeded generator (FCDraw.rng(FCDraw.hash('redtithe.<piece>')), docs/mockups/lib/kitdraw.js), never a sine or product-modulo hash
// (CLAUDE.md section 10). Blood, grime and burns sit only in a piece's margins, never under a label or readout.
// Our own design: no other game's names, shapes or art.
window.FCRedTithe = (function () {
'use strict';
const F = window.FCDraw, P = F.P;
const RT = {
  steel: '#2e3236', steelHi: '#7c848b', steelLo: '#141618', paper: '#c9b48a', paperHi: '#ddcba1', paperLo: '#9c8256',
  dried: '#5a1a12', driedLo: '#33100a', fresh: '#c4141c', freshLo: '#6e0a0e', brass: '#c8963a', brassHi: '#f0cf7a', brassLo: '#6e4d16',
  soot: '#1b1714', ink: '#1e1611', oak: '#4a301c', oakHi: '#62412a', oakLo: '#24160b', bone: '#e6d8b4', boneLo: '#9d8c6c', leather: '#3d2618', leatherHi: '#6b4429', bg: '#1a100a',
};
const RNG = (k) => F.rng(F.hash('redtithe.' + k));
const rgba = (hex, a) => { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; };
const trace = F.trace;

// ---- edges --------------------------------------------------------------------------------------------------------
// A torn edge: the outline resampled every `step` px and each point pushed in or out by the generator, with now and then
// a deeper bite, as paper tears. `amp` is the tear's depth in px; `bite` the chance of a bite per point.
function tear(pts, closed, amp, R, step = 3, bite = 0.06) {
  const out = []; const segs = closed ? pts.length : pts.length - 1;
  for (let i = 0; i < segs; i++) {
    const a = pts[i], b = pts[(i + 1) % pts.length], L = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1, k = Math.max(1, Math.ceil(L / step));
    const nx = -(b[1] - a[1]) / L, ny = (b[0] - a[0]) / L;
    for (let j = 0; j < k; j++) {
      const t = j / k, d = (R() - 0.5) * amp + (R() < bite ? -amp * (1 + R() * 1.5) : 0);
      out.push([a[0] + (b[0] - a[0]) * t + nx * d, a[1] + (b[1] - a[1]) * t + ny * d]);
    }
  }
  if (!closed) out.push(pts[pts.length - 1]);
  return out;
}
// An irregular blob of radius r round (cx, cy), n points: for burn holes, drops and stains.
function blob(cx, cy, r, R, n = 18, j = 0.3, sx = 1, sy = 1) {
  const o = []; for (let i = 0; i < n; i++) { const a = i * 2 * P / n, q = r * (1 - j / 2 + R() * j); o.push([cx + Math.cos(a) * q * sx, cy + Math.sin(a) * q * sy]); } return o;
}
const bbox = (pts) => { const xs = pts.map((p) => p[0]), ys = pts.map((p) => p[1]); return [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)]; };

// ---- materials ----------------------------------------------------------------------------------------------------
// Gun steel: a dark plate, brushed along its length, scratched and pitted, lit from the upper left, its edge worn bright.
function steel(x, pts, R, o = {}) {
  const [x0, y0, x1, y1] = bbox(pts), w = x1 - x0, h = y1 - y0;
  x.save(); trace(x, pts, true); x.clip();
  const g = x.createLinearGradient(x0, y0, x0 + w * 0.3, y1); g.addColorStop(0, o.hi || '#4b5157'); g.addColorStop(0.5, o.mid || RT.steel); g.addColorStop(1, o.lo || '#191b1e');
  x.fillStyle = g; x.fillRect(x0, y0, w, h);
  for (let i = 0; i < h * 1.4; i++) { const y = y0 + R() * h; x.strokeStyle = R() < 0.5 ? 'rgba(255,255,255,0.045)' : 'rgba(0,0,0,0.10)'; x.lineWidth = 0.5; x.beginPath(); x.moveTo(x0 + R() * w * 0.5, y); x.lineTo(x0 + w * (0.5 + R() * 0.5), y + (R() - 0.5)); x.stroke(); }
  const ns = Math.max(2, Math.round(w * h / 900));
  for (let i = 0; i < ns; i++) { const sx = x0 + R() * w, sy = y0 + R() * h, a = R() * P, L = 4 + R() * 16; x.strokeStyle = `rgba(210,215,220,${0.12 + R() * 0.22})`; x.lineWidth = 0.5; x.beginPath(); x.moveTo(sx, sy); x.lineTo(sx + Math.cos(a) * L, sy + Math.sin(a) * L); x.stroke(); }
  for (let i = 0; i < w * h / 60; i++) { x.fillStyle = R() < 0.6 ? 'rgba(0,0,0,0.35)' : 'rgba(150,120,90,0.18)'; x.fillRect(x0 + R() * w, y0 + R() * h, 0.7, 0.7); }
  // grime pooled toward the bottom
  const gr = x.createLinearGradient(0, y0 + h * 0.5, 0, y1); gr.addColorStop(0, 'rgba(0,0,0,0)'); gr.addColorStop(1, 'rgba(14,10,8,0.45)'); x.fillStyle = gr; x.fillRect(x0, y0, w, h);
  x.restore();
  x.save(); trace(x, pts, true); x.clip(); x.translate(1, 1); trace(x, pts, true); x.strokeStyle = 'rgba(220,226,232,0.32)'; x.lineWidth = 1.2; x.stroke(); x.restore();
  trace(x, pts, true); x.strokeStyle = '#08090a'; x.lineWidth = 1.2; x.stroke();
}
// Crusty paper: yellowed, darker toward its scorched edge, fibre grain, foxing, laid over the steel with a shadow.
// `stains` are [cx, cy, r] tea-water rings, placed by the caller in the piece's margins.
function paper(x, pts, R, o = {}) {
  const [x0, y0, x1, y1] = bbox(pts), w = x1 - x0, h = y1 - y0, tone = o.tone || RT.paper;
  x.save(); x.shadowColor = 'rgba(0,0,0,0.7)'; x.shadowBlur = 5; x.shadowOffsetX = 1.5; x.shadowOffsetY = 2.5; trace(x, pts, true); x.fillStyle = tone; x.fill(); x.restore();
  x.save(); trace(x, pts, true); x.clip();
  x.fillStyle = tone; x.fillRect(x0, y0, w, h);
  if (o.light) { x.fillStyle = o.light; x.fillRect(x0, y0, w, h); }
  // fibre grain and foxing: uniform, so a stretched centre stays plain
  for (let i = 0; i < w * h / 5; i++) { x.fillStyle = R() < 0.5 ? 'rgba(70,45,20,0.10)' : 'rgba(255,248,225,0.10)'; x.fillRect(x0 + R() * w, y0 + R() * h, 0.6 + R() * 1.4, 0.5); }
  for (let i = 0; i < w * h / 700; i++) { const cx = x0 + R() * w, cy = y0 + R() * h, r = 0.6 + R() * 1.4; x.fillStyle = 'rgba(120,70,30,0.18)'; x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); }
  // mottling: broad faint blotches of age, spread evenly so a stretched middle stays plain
  for (let i = 0; i < Math.max(4, w * h / 260); i++) { const cx = x0 + R() * w, cy = y0 + R() * h, r = 6 + R() * 18, g = x.createRadialGradient(cx, cy, 0, cx, cy, r); g.addColorStop(0, R() < 0.6 ? 'rgba(110,70,30,0.10)' : 'rgba(255,240,200,0.08)'); g.addColorStop(1, 'rgba(0,0,0,0)'); x.fillStyle = g; x.fillRect(cx - r, cy - r, 2 * r, 2 * r); }
  for (const [cx, cy, r] of o.stains || []) {
    const g = x.createRadialGradient(cx, cy, r * 0.2, cx, cy, r); g.addColorStop(0, 'rgba(150,100,45,0.10)'); g.addColorStop(0.85, 'rgba(120,75,30,0.22)'); g.addColorStop(1, 'rgba(0,0,0,0)');
    x.fillStyle = g; x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill();
    x.strokeStyle = 'rgba(100,60,25,0.35)'; x.lineWidth = 0.8; trace(x, blob(cx, cy, r * 0.88, R, 22, 0.12), true); x.stroke();
  }
  // the scorched, handled edge: browner and darker toward it
  for (const [lw, a] of [[28, 0.07], [16, 0.12], [8, 0.2], [3.6, 0.42], [1.4, 0.85]]) { trace(x, pts, true); x.strokeStyle = `rgba(${lw > 4 ? '85,48,18' : '30,16,8'},${a})`; x.lineWidth = lw; x.stroke(); }
  x.restore();
}
// Soot, grime and blood: only ever placed in a piece's margins by the caller.
function grime(x, cx, cy, r, a = 0.35, col = '20,14,10') { const g = x.createRadialGradient(cx, cy, 0, cx, cy, r); g.addColorStop(0, `rgba(${col},${a})`); g.addColorStop(1, `rgba(${col},0)`); x.fillStyle = g; x.fillRect(cx - r, cy - r, 2 * r, 2 * r); }
function burn(x, cx, cy, r, R) {
  grime(x, cx, cy, r * 2.6, 0.55, '70,34,10');
  const rim = blob(cx, cy, r * 1.35, R, 20, 0.45); trace(x, rim, true); x.fillStyle = '#1a0e07'; x.fill();
  const hole = blob(cx, cy, r, R, 18, 0.5); trace(x, hole, true);
  const g = x.createRadialGradient(cx - r * 0.3, cy - r * 0.3, 0, cx, cy, r); g.addColorStop(0, '#3a3e42'); g.addColorStop(1, '#0e1012'); x.fillStyle = g; x.fill();
  trace(x, hole, true); x.strokeStyle = 'rgba(200,90,30,0.35)'; x.lineWidth = 0.7; x.stroke();
}
// A drop of blood: fresh (red, glossy, a dark rim) or dried (brown, matte, cracked), with a short tail along `dir`.
function drop(x, cx, cy, r, R, fresh, dir = R() * 2 * P) {
  const pts = blob(cx, cy, r, R, 16, 0.18); const tx = cx + Math.cos(dir) * r * 1.9, ty = cy + Math.sin(dir) * r * 1.9;
  x.save(); x.beginPath(); trace(x, pts, true); x.moveTo(cx + Math.cos(dir + 1.2) * r * 0.7, cy + Math.sin(dir + 1.2) * r * 0.7); x.lineTo(tx, ty); x.lineTo(cx + Math.cos(dir - 1.2) * r * 0.7, cy + Math.sin(dir - 1.2) * r * 0.7); x.closePath();
  const g = x.createRadialGradient(cx - r * 0.3, cy - r * 0.3, 0, cx, cy, r * 1.3);
  if (fresh) { g.addColorStop(0, '#d8262c'); g.addColorStop(0.7, '#9a0c12'); g.addColorStop(1, '#4a0508'); } else { g.addColorStop(0, '#6e2618'); g.addColorStop(0.75, RT.dried); g.addColorStop(1, RT.driedLo); }
  x.fillStyle = g; x.fill('nonzero'); x.restore();
  if (fresh) { x.fillStyle = 'rgba(255,230,230,0.6)'; x.beginPath(); x.ellipse(cx - r * 0.35, cy - r * 0.38, r * 0.28, r * 0.16, -0.6, 0, 2 * P); x.fill(); }
  else { x.strokeStyle = 'rgba(25,8,4,0.6)'; x.lineWidth = 0.4; for (let i = 0; i < 2; i++) { x.beginPath(); x.moveTo(cx + (R() - 0.5) * r, cy + (R() - 0.5) * r); x.lineTo(cx + (R() - 0.5) * r, cy + (R() - 0.5) * r); x.stroke(); } }
}
// Spatter: a drop and its satellites flung along `dir`, smaller and longer as they fly.
function spatter(x, cx, cy, dir, n, s, R, fresh) {
  drop(x, cx, cy, s, R, fresh, dir + P);
  for (let i = 0; i < n; i++) {
    const d = s * (1.8 + R() * 5), a = dir + (R() - 0.5) * 0.9, px = cx + Math.cos(a) * d, py = cy + Math.sin(a) * d, r = s * (0.18 + R() * 0.4) * (1.2 - d / (s * 8));
    x.fillStyle = fresh ? '#a8101a' : RT.dried; x.beginPath(); x.ellipse(px, py, Math.max(0.4, r * 1.6), Math.max(0.3, r * 0.8), a, 0, 2 * P); x.fill();
  }
}
// Blood run down from (px, py): a thin trail thickening to a bead at its foot.
function drip(x, px, py, len, R, fresh) {
  const col = fresh ? '#9a0c12' : RT.dried, pts = []; for (let i = 0; i <= 10; i++) pts.push([px + (R() - 0.5) * 0.8, py + len * i / 10]);
  x.save(); x.strokeStyle = col; x.lineCap = 'round'; for (let i = 0; i < 10; i++) { x.lineWidth = 0.8 + i * 0.12; x.beginPath(); x.moveTo(pts[i][0], pts[i][1]); x.lineTo(pts[i + 1][0], pts[i + 1][1]); x.stroke(); } x.restore();
  drop(x, px, py + len, 1.5, R, fresh, -P / 2);
}
// A smear dragged along a path: a low wash and the streaks fingers leave in it.
function smear(x, pts, w, R, col = RT.dried) {
  x.save(); x.lineCap = 'round'; x.lineJoin = 'round';
  trace(x, pts, false); x.strokeStyle = rgba(col, 0.38); x.lineWidth = w; x.stroke();
  for (let i = 0; i < 4; i++) { const o = (R() - 0.5) * w * 0.8; x.save(); x.translate(o * 0.3, o); trace(x, pts, false); x.strokeStyle = rgba(col, 0.25 + R() * 0.3); x.lineWidth = 0.6 + R() * 1.4; x.stroke(); x.restore(); }
  x.restore();
}
// A fingerprint left in grime or blood: broken concentric ridges.
function print(x, cx, cy, s, rot, R, col = '40,26,16', a = 0.35) {
  x.save(); x.translate(cx, cy); x.rotate(rot); x.strokeStyle = `rgba(${col},${a})`; x.lineWidth = 0.55;
  for (let k = 1; k <= 7; k++) { const rx = s * 0.6 * k / 7, ry = s * k / 7; let a0 = R() * 2 * P; while (a0 < 2 * P + 6) { const a1 = a0 + 0.6 + R() * 1.8; x.beginPath(); x.ellipse(0, 0, rx, ry, 0, a0, a1); x.stroke(); a0 = a1 + 0.2 + R() * 0.5; } }
  x.restore();
}
function rivet(x, px, py, r) {
  const g = x.createRadialGradient(px - r * 0.4, py - r * 0.45, 0.2, px, py, r); g.addColorStop(0, '#b9c0c6'); g.addColorStop(0.45, '#4d5358'); g.addColorStop(1, '#111315');
  x.fillStyle = 'rgba(0,0,0,0.6)'; x.beginPath(); x.arc(px + 0.8, py + 1, r, 0, 2 * P); x.fill();
  x.fillStyle = g; x.beginPath(); x.arc(px, py, r, 0, 2 * P); x.fill();
}
// A square nail through the paper, its rust bled down the sheet.
function nail(x, px, py, r, R) {
  const g = x.createLinearGradient(px, py + r, px, py + r + 6 + R() * 8); g.addColorStop(0, 'rgba(120,52,20,0.55)'); g.addColorStop(1, 'rgba(120,52,20,0)');
  x.fillStyle = g; x.fillRect(px - r * 0.5, py, r, r + 10);
  x.fillStyle = 'rgba(0,0,0,0.55)'; x.fillRect(px - r + 0.8, py - r + 1, 2 * r, 2 * r);
  const h = x.createLinearGradient(px - r, py - r, px + r, py + r); h.addColorStop(0, '#8a8f93'); h.addColorStop(1, '#25282b'); x.fillStyle = h; x.fillRect(px - r, py - r, 2 * r, 2 * r);
  x.strokeStyle = 'rgba(0,0,0,0.6)'; x.lineWidth = 0.5; x.strokeRect(px - r, py - r, 2 * r, 2 * r);
}

// ---- the hall's boards (owner, 2026-11-07: "More brown wood on the red tithe") -------------------------------------
// Weathered plank: dark brown oak, its grain running along the board ('h' or 'v'), pores, silvered streaks where the
// weather has raised the grain, lit from the upper left with its worn edges catching the light. Grain lines run the
// board's whole length and drift only slowly, so a board stretched along its grain (a 9-slice edge) stays clean.
function wood(x, pts, R, o = {}) {
  const [x0, y0, x1, y1] = bbox(pts), w = x1 - x0, h = y1 - y0, H = (o.dir || 'h') === 'h', L = H ? w : h, A = H ? h : w;
  const along = (p, t0, t1, wob) => { const out = []; for (let i = 0; i <= 12; i++) { const t = t0 + (t1 - t0) * i / 12, q = p + wob * Math.sin(i / 12 * P * (1 + R() * 0.2)) + (R() - 0.5) * 0.25; out.push(H ? [x0 + t, y0 + q] : [x0 + q, y0 + t]); } return out; };
  x.save(); trace(x, pts, true); x.clip();
  const g = H ? x.createLinearGradient(0, y0, 0, y1) : x.createLinearGradient(x0, 0, x1, 0);
  g.addColorStop(0, o.hi || RT.oakHi); g.addColorStop(0.45, o.tone || RT.oak); g.addColorStop(1, o.lo || RT.oakLo); x.fillStyle = g; x.fillRect(x0, y0, w, h);
  // broad bands of heart and sap wood along the grain
  for (let i = 0; i < Math.max(3, A * 0.22); i++) { const p = R() * A, t = 1.5 + R() * 6; x.fillStyle = R() < 0.55 ? `rgba(22,11,4,${0.10 + R() * 0.16})` : `rgba(170,118,70,${0.05 + R() * 0.09})`; if (H) x.fillRect(x0, y0 + p, w, t); else x.fillRect(x0 + p, y0, t, h); }
  // fine grain: long dark lines, a few pale ones, the whole length of the board
  for (let i = 0; i < A * 1.1; i++) { const p = R() * A; trace(x, along(p, -4, L + 4, (R() - 0.5) * 2.4), false); x.strokeStyle = R() < 0.8 ? `rgba(18,8,3,${0.16 + R() * 0.3})` : `rgba(200,150,100,${0.08 + R() * 0.12})`; x.lineWidth = 0.4 + R() * 0.7; x.stroke(); }
  // pores and silvered, raised grain
  for (let i = 0; i < w * h / 14; i++) { const px = x0 + R() * w, py = y0 + R() * h, l = 1.5 + R() * 4; x.fillStyle = R() < 0.7 ? 'rgba(10,5,2,0.4)' : 'rgba(176,164,146,0.13)'; if (H) x.fillRect(px, py, l, 0.6); else x.fillRect(px, py, 0.6, l); }
  // grime settled toward the foot, as on every board of the hall
  const gr = x.createLinearGradient(0, y0 + h * 0.4, 0, y1); gr.addColorStop(0, 'rgba(0,0,0,0)'); gr.addColorStop(1, 'rgba(10,6,3,0.35)'); x.fillStyle = gr; x.fillRect(x0, y0, w, h);
  x.restore();
  // worn arrises: bright where the light from the upper left catches them, dark below and right
  x.save(); trace(x, pts, true); x.clip(); x.translate(1, 1); trace(x, pts, true); x.strokeStyle = 'rgba(214,168,118,0.32)'; x.lineWidth = 1.4; x.stroke(); x.restore();
  x.save(); trace(x, pts, true); x.clip(); x.translate(-1.2, -1.4); trace(x, pts, true); x.strokeStyle = 'rgba(8,4,2,0.55)'; x.lineWidth = 2; x.stroke(); x.restore();
  trace(x, pts, true); x.strokeStyle = '#0c0704'; x.lineWidth = 1.1; x.stroke();
}
// A board laid as a rectangle: a shortcut for wood() with a per-board tone drawn from the generator, so no two match.
function board(x, X, Y, W, H, R, dir, o = {}) {
  const k = (R() - 0.5) * 0.22, sh = (hex) => { const n = parseInt(hex.slice(1), 16), f = (c) => Math.max(0, Math.min(255, Math.round(c * (1 + k)))); return `rgb(${f(n >> 16)},${f((n >> 8) & 255)},${f(n & 255)})`; };
  wood(x, rect(X, Y, W, H), R, Object.assign({ dir, tone: sh(o.tone || RT.oak), hi: sh(o.hi || RT.oakHi), lo: sh(o.lo || RT.oakLo) }, o.extra || {}));
}
// A knot: a dark eye drawn out along the grain, its rings round it and a check split from it.
function knot(x, cx, cy, r, R, dir = 'h') {
  const sx = dir === 'h' ? 1.9 : 1, sy = dir === 'h' ? 1 : 1.9;
  grime(x, cx, cy, r * 2.4, 0.3, '30,14,5');
  x.strokeStyle = 'rgba(20,9,3,0.45)'; x.lineWidth = 0.6; for (let k = 4; k >= 1; k--) { trace(x, blob(cx, cy, r * (0.6 + k * 0.4), R, 20, 0.12, sx, sy), true); x.stroke(); }
  const g = x.createRadialGradient(cx - r * 0.2, cy - r * 0.2, 0, cx, cy, r); g.addColorStop(0, '#5a3418'); g.addColorStop(0.6, '#2a1608'); g.addColorStop(1, '#120803');
  trace(x, blob(cx, cy, r, R, 16, 0.25, sx * 0.8, sy * 0.8), true); x.fillStyle = g; x.fill();
  x.strokeStyle = 'rgba(8,4,2,0.8)'; x.lineWidth = 0.7; const a = dir === 'h' ? 0 : P / 2; x.beginPath(); x.moveTo(cx + Math.cos(a) * r * 1.2, cy + Math.sin(a) * r * 1.2); x.lineTo(cx + Math.cos(a) * r * (2.6 + R()), cy + Math.sin(a) * r * (2.6 + R()) + (R() - 0.5)); x.stroke();
}
// A knife gouge: a V cut, dark in its throat, the fresh pale wood of its lit wall along one side.
function gouge(x, ax, ay, bx, by, wd = 1.6) {
  const L = Math.hypot(bx - ax, by - ay) || 1, nx = -(by - ay) / L, ny = (bx - ax) / L;
  x.save(); x.lineCap = 'round';
  x.strokeStyle = 'rgba(205,160,110,0.55)'; x.lineWidth = wd * 0.7; x.beginPath(); x.moveTo(ax - nx * wd * 0.6, ay - ny * wd * 0.6); x.lineTo(bx - nx * wd * 0.6, by - ny * wd * 0.6); x.stroke();
  x.strokeStyle = 'rgba(6,3,1,0.9)'; x.lineWidth = wd; x.beginPath(); x.moveTo(ax, ay); x.quadraticCurveTo((ax + bx) / 2 + nx * 0.6, (ay + by) / 2 + ny * 0.6, bx, by); x.stroke();
  x.restore();
}
// A tally scratched into the board: `n` marks in fives, each five struck through, scored pale into the dark wood.
function tally(x, X, Y, n, hgt, R, col = 'rgba(214,176,128,0.7)') {
  x.save(); x.strokeStyle = col; x.lineWidth = 0.9; x.lineCap = 'round';
  let px = X;
  for (let i = 0; i < n; i++) {
    if (i % 5 === 4) { x.beginPath(); x.moveTo(px - 4 * 3.2 - 1.5, Y + hgt * 0.8); x.lineTo(px + 0.5, Y + hgt * 0.15); x.stroke(); px += 4.5; continue; }
    x.beginPath(); x.moveTo(px + (R() - 0.5) * 0.6, Y + (R() - 0.5)); x.lineTo(px + (R() - 0.5) * 1.2, Y + hgt + (R() - 0.5)); x.stroke(); px += 3.2;
  }
  x.restore();
}
// A scorch: wood charred black at its heart and checked into small blocks, browned round it.
function scorch(x, cx, cy, r, R) {
  grime(x, cx, cy, r * 2.2, 0.55, '40,18,6'); grime(x, cx, cy, r * 1.3, 0.75, '10,6,3');
  x.strokeStyle = 'rgba(0,0,0,0.6)'; x.lineWidth = 0.5;
  for (let i = 0; i < r * 1.4; i++) { const px = cx + (R() - 0.5) * r * 1.6, py = cy + (R() - 0.5) * r * 1.2; x.beginPath(); x.moveTo(px - 2, py); x.lineTo(px + 2, py); x.moveTo(px, py - 1.5); x.lineTo(px, py + 1.5); x.stroke(); }
  for (let i = 0; i < 5; i++) { x.fillStyle = `rgba(230,110,40,${0.18 + R() * 0.2})`; x.fillRect(cx + (R() - 0.5) * r, cy + (R() - 0.5) * r * 0.8, 0.9, 0.9); }
}
// Old blood soaked into the grain: drawn out along it, darkest where it pooled, its edges running down the grain lines.
function soak(x, cx, cy, r, R, dir = 'h') {
  const H = dir === 'h';
  x.save(); x.translate(cx, cy); if (!H) x.rotate(P / 2);
  const g = x.createRadialGradient(0, 0, 0, 0, 0, r); g.addColorStop(0, 'rgba(70,10,6,0.6)'); g.addColorStop(0.7, 'rgba(60,10,6,0.32)'); g.addColorStop(1, 'rgba(60,10,6,0)');
  x.scale(2.2, 0.8); x.fillStyle = g; x.beginPath(); x.arc(0, 0, r, 0, 2 * P); x.fill();
  x.restore();
  x.save(); x.translate(cx, cy); if (!H) x.rotate(P / 2);
  for (let i = 0; i < 9; i++) { const y = (R() - 0.5) * r * 1.1, l = r * (1.2 + R() * 1.6), s = (R() - 0.5) * r; x.strokeStyle = `rgba(80,12,8,${0.25 + R() * 0.3})`; x.lineWidth = 0.5 + R() * 0.9; x.beginPath(); x.moveTo(s - l, y); x.lineTo(s + l, y + (R() - 0.5)); x.stroke(); }
  x.restore();
}
// A nail hole left by an older sheet: a dark square pit with a ring of rust.
function nailHole(x, px, py, R) { grime(x, px, py + 1.5, 4, 0.4, '110,48,18'); x.fillStyle = '#080402'; x.fillRect(px - 1.1, py - 1.1, 2.2, 2.2); x.fillStyle = 'rgba(220,170,120,0.3)'; x.fillRect(px - 1.1, py + 1.1, 2.2, 0.5); }
// Strap iron: a flat black-iron band, forged rough and rusted at its edges, riveted through.
function iron(x, pts, R, rivets = []) {
  steel(x, pts, R, { hi: '#474b4e', mid: '#26292b', lo: '#0f1011' });
  x.save(); trace(x, pts, true); x.clip(); for (let i = 0; i < 6; i++) { const [px, py] = pts[Math.floor(R() * pts.length)]; grime(x, px + (R() - 0.5) * 6, py + (R() - 0.5) * 6, 5 + R() * 5, 0.35, '120,52,20'); } x.restore();
  for (const [px, py, r] of rivets) rivet(x, px, py, r || 2.6);
}
// An iron corner strap: an L of strap iron over the joint of two boards, at corner (cx, cy) opening toward (sx, sy).
function cornerStrap(x, cx, cy, sx, sy, arm, wd, R) {
  const p = [[0, 0], [arm, 0], [arm, wd], [wd, wd], [wd, arm], [0, arm]].map(([a, b]) => [cx + a * sx, cy + b * sy]);
  iron(x, p, R, [[cx + (arm - wd * 0.55) * sx, cy + wd * 0.5 * sy, 2.2], [cx + wd * 0.5 * sx, cy + (arm - wd * 0.55) * sy, 2.2], [cx + wd * 0.5 * sx, cy + wd * 0.5 * sy, 2.4]]);
}
// A frame of four boards round a rectangle: top and bottom with their grain across, the sides with theirs down, butted at
// the corners. `t` is the boards' width. A dark plank backs the middle, where the torn sheet lets it show.
function boardFrame(x, w, h, t, R, o = {}) {
  board(x, 0.5, 0.5, w - 1, h - 1, R, 'h', { tone: '#2c1b0f', hi: '#3a2414', lo: '#160d06' });
  board(x, 0.5, 0.5, w - 1, t, R, 'h', o); board(x, 0.5, h - t - 0.5, w - 1, t, R, 'h', o);
  board(x, 0.5, t, t, h - 2 * t, R, 'v', o); board(x, w - t - 0.5, t, t, h - 2 * t, R, 'v', o);
}

// ---- trophies (the cutters' blades, the gunners' brass, the teeth of beaten crews) --------------------------------
// A knife driven through the sheet: handle at (x0, y0), the blade entering the paper at (x1, y1); scale s.
function knife(x, x0, y0, x1, y1, s, R, o = {}) {
  const L = Math.hypot(x1 - x0, y1 - y0), a = Math.atan2(y1 - y0, x1 - x0);
  x.save(); x.translate(x0, y0); x.rotate(a);
  const hw = 2.6 * s, bw = 3.4 * s, gx = L * 0.46;
  x.save(); x.shadowColor = 'rgba(0,0,0,0.75)'; x.shadowBlur = 4; x.shadowOffsetX = 2; x.shadowOffsetY = 3;
  // blade: a straight back, a curved belly, a fuller, bloodied toward the point
  x.beginPath(); x.moveTo(gx, -bw); x.lineTo(L + 2, -bw * 0.9); x.lineTo(L + 2, bw * 0.4); x.quadraticCurveTo(L * 0.8, bw * 1.3, gx, bw); x.closePath();
  const bg = x.createLinearGradient(0, -bw, 0, bw); bg.addColorStop(0, '#e1e6ea'); bg.addColorStop(0.45, '#8d959c'); bg.addColorStop(0.55, '#4c5359'); bg.addColorStop(1, '#a9b1b8'); x.fillStyle = bg; x.fill(); x.restore();
  x.strokeStyle = '#0b0c0d'; x.lineWidth = 0.7; x.stroke();
  x.strokeStyle = 'rgba(30,34,38,0.7)'; x.lineWidth = 0.6; x.beginPath(); x.moveTo(gx + 3, -bw * 0.3); x.lineTo(L * 0.85, -bw * 0.3); x.stroke();
  if (o.blood !== false) { x.save(); x.clip(); smear(x, [[L * 0.7, 0], [L + 2, 0.4]], bw * 2.2, R, '#6a1410'); x.restore(); }
  // where it goes through: a dark slit with the paper lifted round it
  x.fillStyle = '#050505'; x.fillRect(L, -bw * 1.2, 2, bw * 2.4); x.fillStyle = 'rgba(255,240,210,0.35)'; x.fillRect(L + 2, -bw * 1.1, 0.8, bw * 2.2);
  // guard, wrapped grip, pommel
  x.save(); x.shadowColor = 'rgba(0,0,0,0.75)'; x.shadowBlur = 4; x.shadowOffsetX = 2; x.shadowOffsetY = 3;
  const gg = x.createLinearGradient(0, -bw * 2, 0, bw * 2); gg.addColorStop(0, '#9ba3aa'); gg.addColorStop(1, '#25292c'); x.fillStyle = gg; x.fillRect(gx - 2.2 * s, -bw * 1.9, 2.8 * s, bw * 3.8);
  // a grip of dark wood scales, worn pale where the hand holds it, pinned with brass
  const hg = x.createLinearGradient(0, -hw, 0, hw); hg.addColorStop(0, '#8a5e38'); hg.addColorStop(0.45, RT.oak); hg.addColorStop(1, '#170d05'); x.fillStyle = hg; x.beginPath(); x.moveTo(3 * s, -hw * 0.8); x.lineTo(gx - 2 * s, -hw); x.lineTo(gx - 2 * s, hw); x.lineTo(3 * s, hw * 0.8); x.closePath(); x.fill();
  x.restore();
  x.strokeStyle = 'rgba(14,7,2,0.6)'; x.lineWidth = 0.4; for (let i = 0; i < 4; i++) { const y = (R() - 0.5) * hw * 1.5; x.beginPath(); x.moveTo(3.5 * s, y); x.lineTo(gx - 2.5 * s, y + (R() - 0.5) * 1.2); x.stroke(); }
  for (const t of [0.3, 0.72]) { const px = 3 * s + (gx - 5 * s) * t; x.fillStyle = RT.brassHi; x.beginPath(); x.arc(px, 0, 0.9 * s, 0, 2 * P); x.fill(); x.fillStyle = RT.brassLo; x.beginPath(); x.arc(px + 0.25 * s, 0.25 * s, 0.5 * s, 0, 2 * P); x.fill(); }
  const pg = x.createRadialGradient(1.5 * s, -1 * s, 0.3, 2 * s, 0, 3.4 * s); pg.addColorStop(0, '#c9d0d6'); pg.addColorStop(1, '#1d2023'); x.fillStyle = pg; x.beginPath(); x.arc(2 * s, 0, 3.2 * s, 0, 2 * P); x.fill();
  x.restore();
}
// A hatchet left buried in the boards (the cutters' tradition): an oak haft from its knob at (x0, y0) to the iron head
// at (x1, y1), the bit sunk into the wood across the haft; scale s.
function hatchet(x, x0, y0, x1, y1, s, R) {
  const L = Math.hypot(x1 - x0, y1 - y0), a = Math.atan2(y1 - y0, x1 - x0);
  x.save(); x.translate(x0, y0); x.rotate(a);
  x.save(); x.shadowColor = 'rgba(0,0,0,0.75)'; x.shadowBlur = 5; x.shadowOffsetX = 2; x.shadowOffsetY = 3;
  x.beginPath(); x.moveTo(0, -3.4 * s); x.quadraticCurveTo(L * 0.5, -2.2 * s, L + 2 * s, -2.6 * s); x.lineTo(L + 2 * s, 2.6 * s); x.quadraticCurveTo(L * 0.5, 2.2 * s, 0, 3.4 * s); x.quadraticCurveTo(-2.6 * s, 0, 0, -3.4 * s); x.closePath();
  const g = x.createLinearGradient(0, -3.4 * s, 0, 3.4 * s); g.addColorStop(0, '#a87648'); g.addColorStop(0.45, '#6a4426'); g.addColorStop(1, '#1e1108'); x.fillStyle = g; x.fill(); x.restore();
  x.strokeStyle = '#0c0704'; x.lineWidth = 0.8; x.stroke();
  x.strokeStyle = 'rgba(14,7,2,0.5)'; x.lineWidth = 0.4; for (const y of [-1.6, 0.4, 1.8]) { x.beginPath(); x.moveTo(2 * s, y * s); x.lineTo(L, y * s + (R() - 0.5)); x.stroke(); }
  // a wrap of cord where the hand grips, darkened with old blood
  x.fillStyle = '#3a1a10'; x.fillRect(L * 0.12, -3.3 * s, L * 0.2, 6.6 * s); x.strokeStyle = 'rgba(0,0,0,0.6)'; x.lineWidth = 0.6; for (let t = L * 0.12; t < L * 0.32; t += 2 * s) { x.beginPath(); x.moveTo(t, -3.3 * s); x.lineTo(t + s, 3.3 * s); x.stroke(); }
  // the head: a bearded iron head round the haft's end, its bit sunk in the board
  const hd = [[L - 5 * s, -5 * s], [L + 6 * s, -5 * s], [L + 6 * s, 3 * s], [L + 10 * s, 15 * s], [L - 11 * s, 15 * s], [L - 5 * s, 4 * s]];
  x.save(); x.shadowColor = 'rgba(0,0,0,0.75)'; x.shadowBlur = 4; x.shadowOffsetX = 2; x.shadowOffsetY = 3; trace(x, hd, true); x.fillStyle = '#2a2d30'; x.fill(); x.restore();
  steel(x, hd, R, { hi: '#80888f', mid: '#3a3f44', lo: '#141618' });
  x.fillStyle = '#050505'; x.fillRect(L - 12 * s, 14.6 * s, 23 * s, 1.6 * s);
  x.fillStyle = 'rgba(230,236,240,0.5)'; x.fillRect(L - 9 * s, 12.6 * s, 17 * s, 0.8);
  x.restore();
}
// A trophy tooth, hung by its drilled root: a canine, a molar or a fang, from crews of every kind. (cx, cy) is the hole.
function tooth(x, cx, cy, s, kind, rot, R) {
  x.save(); x.translate(cx, cy); x.rotate(rot); x.scale(s, s);
  x.beginPath();
  if (kind === 'molar') { x.moveTo(-3, -1); x.lineTo(-2.4, 4); x.lineTo(-4.2, 6); x.quadraticCurveTo(-5.2, 10, -3, 11.5); x.quadraticCurveTo(0, 12.5, 3, 11.5); x.quadraticCurveTo(5.2, 10, 4.2, 6); x.lineTo(2.4, 4); x.lineTo(3, -1); x.quadraticCurveTo(0, -2.6, -3, -1); }
  else if (kind === 'fang') { x.moveTo(-2.6, -1); x.quadraticCurveTo(-3.6, 6, -1.2, 14); x.quadraticCurveTo(1.6, 9, 2.8, -1); x.quadraticCurveTo(0, -2.4, -2.6, -1); }
  else { x.moveTo(-2.4, -1); x.quadraticCurveTo(-3.4, 5, -2.2, 8); x.lineTo(0, 13); x.lineTo(2.2, 8); x.quadraticCurveTo(3.4, 5, 2.4, -1); x.quadraticCurveTo(0, -2.4, -2.4, -1); }
  x.closePath();
  x.save(); x.shadowColor = 'rgba(0,0,0,0.7)'; x.shadowBlur = 2.5 / s; x.shadowOffsetX = 1.2; x.shadowOffsetY = 1.8;
  const g = x.createLinearGradient(-4, 0, 4, 0); g.addColorStop(0, '#f4ead0'); g.addColorStop(0.55, RT.bone); g.addColorStop(1, '#8a7a5c'); x.fillStyle = g; x.fill(); x.restore();
  x.strokeStyle = '#2a2018'; x.lineWidth = 0.55 / s * 1.4; x.stroke();
  // the root darker, a crack, a stain
  x.save(); x.clip(); x.fillStyle = 'rgba(110,80,45,0.4)'; x.fillRect(-6, -3, 12, 4.5); x.strokeStyle = 'rgba(60,40,25,0.7)'; x.lineWidth = 0.35; x.beginPath(); x.moveTo(-0.6 + R(), 3); x.lineTo(0.4, 6.5); x.lineTo(-0.2 + R() * 0.6, 9); x.stroke(); x.restore();
  x.fillStyle = '#120c08'; x.beginPath(); x.arc(0, 0.6, 0.85, 0, 2 * P); x.fill();
  x.restore();
}
// A string of trophy teeth on a cord sagging from (ax, ay) to (bx, by).
function teethString(x, ax, ay, bx, by, sag, n, s, R) {
  const at = (t) => [ax + (bx - ax) * t, ay + (by - ay) * t + Math.sin(t * P) * sag];
  x.strokeStyle = '#2a1a10'; x.lineWidth = 1.3; x.beginPath(); for (let i = 0; i <= 20; i++) { const [px, py] = at(i / 20); if (i) x.lineTo(px, py); else x.moveTo(px, py); } x.stroke();
  x.strokeStyle = 'rgba(160,120,80,0.45)'; x.lineWidth = 0.4; x.stroke();
  const kinds = ['canine', 'molar', 'fang'];
  for (let i = 0; i < n; i++) { const t = (i + 0.5 + (R() - 0.5) * 0.3) / n, [px, py] = at(t); tooth(x, px, py, s * (0.85 + R() * 0.3), kinds[Math.floor(R() * 3)], (R() - 0.5) * 0.35, R); }
}
// A cartridge standing at (cx, base y by), w wide, h tall: 'live' brass with its bullet, 'red' with a painted tip
// (the warning round), 'spent' an empty dented case, 'ok' a nickel case (the battle console's whole bar).
function cartridge(x, cx, by, w, h, state, R) {
  const cw = w / 2, caseTop = by - h * 0.62, neck = by - h * 0.7;
  x.save(); x.shadowColor = 'rgba(0,0,0,0.6)'; x.shadowBlur = 1.5; x.shadowOffsetX = 0.8; x.shadowOffsetY = 1;
  if (state !== 'spent') {
    x.beginPath(); x.moveTo(cx - cw * 0.62, neck + 0.5); x.lineTo(cx - cw * 0.62, by - h * 0.86); x.quadraticCurveTo(cx - cw * 0.5, by - h, cx, by - h); x.quadraticCurveTo(cx + cw * 0.5, by - h, cx + cw * 0.62, by - h * 0.86); x.lineTo(cx + cw * 0.62, neck + 0.5); x.closePath();
    const bg = x.createLinearGradient(cx - cw, 0, cx + cw, 0); bg.addColorStop(0, '#e9a27a'); bg.addColorStop(0.4, '#b5643e'); bg.addColorStop(1, '#4a2213'); x.fillStyle = bg; x.fill();
    if (state === 'red') { x.save(); x.clip(); const rg = x.createLinearGradient(cx - cw, 0, cx + cw, 0); rg.addColorStop(0, '#ff5a50'); rg.addColorStop(0.45, RT.fresh); rg.addColorStop(1, RT.freshLo); x.fillStyle = rg; x.fillRect(cx - cw, by - h - 1, w, h * 0.22); x.restore(); }
  }
  x.beginPath(); x.moveTo(cx - cw * 0.7, neck); x.lineTo(cx - cw * 0.7, caseTop + 0.6); x.lineTo(cx - cw, caseTop + h * 0.06); x.lineTo(cx - cw, by - 1); x.lineTo(cx + cw, by - 1); x.lineTo(cx + cw, caseTop + h * 0.06); x.lineTo(cx + cw * 0.7, caseTop + 0.6); x.lineTo(cx + cw * 0.7, neck); x.closePath();
  const g = x.createLinearGradient(cx - cw, 0, cx + cw, 0);
  if (state === 'spent') { g.addColorStop(0, '#7a6440'); g.addColorStop(0.4, '#4e3d22'); g.addColorStop(1, '#1d160c'); } else if (state === 'ok') { g.addColorStop(0, '#f2f4f6'); g.addColorStop(0.35, '#b8bec4'); g.addColorStop(1, '#4a5056'); } else { g.addColorStop(0, RT.brassHi); g.addColorStop(0.35, RT.brass); g.addColorStop(1, RT.brassLo); }
  x.fillStyle = g; x.fill(); x.restore();
  x.strokeStyle = 'rgba(20,12,4,0.8)'; x.lineWidth = 0.4; x.stroke();
  x.fillStyle = state === 'spent' ? '#2a2014' : RT.brassLo; x.fillRect(cx - cw, by - 1.6, w, 1.6);
  if (state === 'spent') { x.fillStyle = '#0a0806'; x.beginPath(); x.ellipse(cx, neck + 0.4, cw * 0.62, 0.8, 0, 0, 2 * P); x.fill(); x.strokeStyle = 'rgba(0,0,0,0.6)'; x.lineWidth = 0.4; x.beginPath(); x.moveTo(cx - cw * 0.3, by - h * 0.4); x.lineTo(cx + cw * 0.2, by - h * 0.3); x.stroke(); }
  else { x.fillStyle = 'rgba(255,250,220,0.55)'; x.fillRect(cx - cw * 0.6, caseTop + 2, Math.max(0.5, cw * 0.3), h * 0.45); }
}
// Sawteeth along a line from (ax, ay) to (bx, by), pointing to the line's left (dir 1) or right (-1): the trap's bite.
function saw(x, ax, ay, bx, by, n, depth, dir) {
  const L = Math.hypot(bx - ax, by - ay), ux = (bx - ax) / L, uy = (by - ay) / L, nx = -uy * dir, ny = ux * dir, t = L / n;
  x.beginPath(); x.moveTo(ax, ay);
  for (let i = 0; i < n; i++) { const m = (i + 0.55) * t; x.lineTo(ax + ux * m + nx * depth, ay + uy * m + ny * depth); x.lineTo(ax + ux * (i + 1) * t, ay + uy * (i + 1) * t); }
  x.closePath();
  const g = x.createLinearGradient(ax, ay, ax + nx * depth, ay + ny * depth); g.addColorStop(0, '#5a6066'); g.addColorStop(1, '#d9dfe4');
  x.save(); x.shadowColor = 'rgba(0,0,0,0.6)'; x.shadowBlur = 1.5; x.shadowOffsetY = 1; x.fillStyle = g; x.fill(); x.restore();
  x.strokeStyle = '#0b0c0d'; x.lineWidth = 0.6; x.stroke();
}

// ---- the kit ------------------------------------------------------------------------------------------------------
// A 9-slice piece: a painter with its own generator.
const piece = (name, size, m, draw, extra) => Object.assign({ size, m, draw: (x, w, h) => draw(x, w, h, RNG(name)) }, extra || {});
const rect = (X, Y, W, H) => [[X, Y], [X + W, Y], [X + W, Y + H], [X, Y + H]];

// The trap-jaw key (the cutters' tradition): a slip of the sheet-keepers' paper clamped between two steel jaws, hinged at
// each end on a spring eye, sawteeth biting the paper at both ends. The centre (between the 40 px ends) is plain jaw and
// plain paper, where the label goes. Hover: the jaws ease open and the paper lifts and brightens. Pressed: the jaws shut
// on it and blood seeps from every tooth along the bite.
function trapJaw(state) {
  return (x, w, h, R) => {
    const gap = state === 'pressed' ? 1.4 : state === 'hover' ? -1 : 0;   // how far the jaws close past rest, px
    const top = 10 + gap, bot = h - 10 - gap;
    // the paper slip, torn at its ends under the hinges
    const slip = tear([[12, 7], [w - 12, 7], [w - 12, h - 7], [12, h - 7]], true, 1.4, RNG('btn.slip'), 3, 0.03);
    paper(x, slip, RNG('btn.paper'), { tone: state === 'hover' ? '#d9c597' : state === 'pressed' ? '#b9a272' : '#bea77b', light: state === 'hover' ? 'rgba(255,236,190,0.10)' : null });
    // old dried stains and grime at the ends only, with a thumbprint where captains slam it
    const Rs = RNG('btn.stain');
    x.save(); trace(x, slip, true); x.clip();
    grime(x, 20, h / 2, 15, 0.5); grime(x, w - 20, h / 2, 15, 0.5);
    smear(x, [[15, h * 0.6], [24, h * 0.52], [32, h * 0.58]], 4, Rs); print(x, w - 25, h * 0.5, 6, 0.5, Rs, '60,20,12', 0.45); drop(x, 22, h * 0.36, 1.4, Rs, false);
    if (state === 'pressed') {
      // blood seeping from the bite along both jaws, pooling at the teeth
      for (const [y, d] of [[top, 1], [bot, -1]]) { const g = x.createLinearGradient(0, y, 0, y + d * 3); g.addColorStop(0, 'rgba(150,12,18,0.85)'); g.addColorStop(1, 'rgba(150,12,18,0)'); x.fillStyle = g; x.fillRect(0, Math.min(y, y + d * 3), w, 3); }
      for (const ex of [24, w - 24]) { drip(x, ex, top + 4, 7, Rs, true); drop(x, ex + (ex < w / 2 ? 5 : -5), bot - 4, 1.8, Rs, true, -P / 2); }
    }
    x.restore();
    // the two jaws: plain bars across, toothed and curved up into the hinge at each end
    const Rj = RNG('btn.jaw');
    const jaw = (y0, y1, dir) => {
      const p = [[14, y0], [w - 14, y0], [w - 14, y1], [14, y1]]; steel(x, p, Rj, dir > 0 ? {} : { hi: '#3b4045', mid: '#2a2e32', lo: '#121416' });
      x.strokeStyle = dir > 0 ? 'rgba(230,236,240,0.55)' : 'rgba(0,0,0,0.7)'; x.lineWidth = 0.8; x.beginPath(); x.moveTo(16, dir > 0 ? y1 - 0.6 : y0 + 0.6); x.lineTo(w - 16, dir > 0 ? y1 - 0.6 : y0 + 0.6); x.stroke();
    };
    jaw(1, top, 1); jaw(bot, h - 1, -1);
    // sawteeth only in the ends, where the jaws bite hardest
    for (const [a, b] of [[15, 34], [w - 34, w - 15]]) { saw(x, a, top, b, top, 3, 7 + gap * 0.6, 1); saw(x, b, bot, a, bot, 3, 7 + gap * 0.6, 1); }
    // the trap's stakes: a block of the hall's oak at each end, bound top and bottom with strap iron, the jaws' pivot
    // pinned through it
    for (const ex of [8, w - 8]) {
      const Rk = RNG('btn.stake' + (ex < w / 2 ? 'L' : 'R'));
      wood(x, F.rrPts(ex - 7, 1.5, 14, h - 3, 3), Rk, { dir: 'v' });
      for (const y of [4, h - 10]) iron(x, rect(ex - 7.5, y, 15, 6), Rk);
      rivet(x, ex, h / 2, 3);
    }
    if (state === 'hover') { x.fillStyle = 'rgba(255,220,150,0.08)'; x.fillRect(14, top, w - 28, bot - top); }
  };
}
// A tab: a tally stick of the hall's oak, notched at both ends with the counts it has kept. Idle: the bare stick, read in
// pale ink. Active: a slip of the ledger nailed over it with the dried thumbprint of the captain who chose it, read in
// black. The notches and nails sit in the 20 px ends; the middle is plain.
function ledgerTab(active) {
  return (x, w, h, R) => {
    const Rw = RNG('tab.wood'), st = tear([[2, 4], [w - 2, 3], [w - 2, h - 4], [2, h - 3]], true, 0.8, RNG('tab.stick'), 4, 0.02);
    wood(x, st, Rw, { dir: 'h', tone: active ? '#4a301c' : '#3e2716', hi: active ? '#644229' : '#56371f', lo: '#1c1008' });
    for (const ex of [5, w - 15]) for (let i = 0; i < 3; i++) { const nx = ex + i * 4; trace(x, [[nx, 3], [nx + 1.8, 7], [nx + 3.6, 3]], true); x.fillStyle = '#0b0603'; x.fill(); trace(x, [[nx, h - 3], [nx + 1.8, h - 7], [nx + 3.6, h - 3]], true); x.fill(); }
    if (active) {
      const p = tear([[16, 7], [w - 16, 8], [w - 16, h - 8], [w * 0.5, h - 7], [16, h - 8]], true, 1.3, RNG('tab.slip'), 3, 0.05);
      paper(x, p, RNG('tab.paper'), { tone: '#d4c092' });
      x.save(); trace(x, p, true); x.clip(); print(x, 22, h / 2 + 2, 5.5, -0.4, R, '90,26,16', 0.55); grime(x, w - 20, h - 10, 9, 0.3); x.restore();
    } else {
      x.save(); trace(x, st, true); x.clip(); soak(x, w - 18, h / 2 + 4, 6, R, 'h'); grime(x, 14, h / 2, 10, 0.35); x.restore();
    }
    nail(x, 18, h / 2, 2.4, R); nail(x, w - 18, h / 2, 2.4, R);
  };
}
// The bandolier's strap (the gunners' tradition): stitched leather with a buckle and a tip; its middle tiles (--bt-rep).
function strap(x, w, h, R) {
  const p = [[1, 2], [w - 1, 2], [w - 1, h - 2], [1, h - 2]];
  x.save(); x.shadowColor = 'rgba(0,0,0,0.7)'; x.shadowBlur = 3; x.shadowOffsetY = 1.5; trace(x, p, true); x.fillStyle = RT.leather; x.fill(); x.restore();
  const g = x.createLinearGradient(0, 2, 0, h - 2); g.addColorStop(0, '#5a3a24'); g.addColorStop(0.5, RT.leather); g.addColorStop(1, '#1e120a'); x.fillStyle = g; x.fillRect(1, 2, w - 2, h - 4);
  for (let i = 0; i < w * h / 4; i++) { x.fillStyle = R() < 0.5 ? 'rgba(0,0,0,0.18)' : 'rgba(255,220,180,0.06)'; x.fillRect(R() * w, 2 + R() * (h - 4), 0.8, 0.6); }
  x.strokeStyle = 'rgba(200,170,120,0.5)'; x.lineWidth = 0.7; x.setLineDash([2.5, 1.5]);
  for (const y of [3.6, h - 3.6]) { x.beginPath(); x.moveTo(2, y); x.lineTo(w - 2, y); x.stroke(); }
  x.setLineDash([]);
  // the buckle at the left end, the cut tip at the right, both inside the 16 px ends
  const b = x.createLinearGradient(0, 0, 0, h); b.addColorStop(0, RT.brassHi); b.addColorStop(1, RT.brassLo);
  x.strokeStyle = b; x.lineWidth = 2.2; x.strokeRect(3, 1.5, 10, h - 3); x.fillStyle = RT.brassLo; x.fillRect(7.5, 2, 1.6, h - 4);
  x.fillStyle = '#120a06'; x.beginPath(); x.moveTo(w - 6, 2); x.lineTo(w, 2); x.lineTo(w, h - 2); x.lineTo(w - 6, h - 2); x.lineTo(w - 2, h / 2); x.closePath(); x.fill();
}
// A loop of the bandolier: empty (spent), holding a brass round, or a red-tipped round. One seed for all three.
function loop(state) {
  return (x, w, h) => {
    const R = RNG('cell');
    if (state === 'spent') { x.fillStyle = 'rgba(0,0,0,0.55)'; x.fillRect(w * 0.18, 1, w * 0.64, h * 0.62); cartridge(x, w / 2, h - 3, w * 0.6, h * 0.42, 'spent', R); }
    else cartridge(x, w / 2, h - 1, w * 0.62, h - 1, state === 'warn' ? 'red' : state === 'ok' ? 'ok' : 'live', R);
    // the leather loop over the round's waist
    const ly = h * 0.52, lh = h * 0.24, g = x.createLinearGradient(0, ly, 0, ly + lh); g.addColorStop(0, '#6b4429'); g.addColorStop(1, '#2a180c');
    x.fillStyle = g; x.fillRect(0, ly, w, lh); x.strokeStyle = 'rgba(0,0,0,0.6)'; x.lineWidth = 0.4; x.strokeRect(0.2, ly, w - 0.4, lh);
    x.fillStyle = 'rgba(210,180,130,0.45)'; for (const sx of [1.2, w - 1.8]) x.fillRect(sx, ly + 1, 0.6, lh - 2);
  };
}
// A strip: a long bounty sheet nailed across a riveted steel beam; a knife and a burn at one end, trophies at the other.
function stripPiece(bottom) {
  return (x, w, h, R) => {
    // a beam of two planks laid along the strip, butted on a seam, banded with strap iron at both ends
    const Rb = RNG('strip.wood' + bottom);
    board(x, 0.5, 0.5, w - 1, h / 2, Rb, 'h'); board(x, 0.5, h / 2 + 0.5, w - 1, h / 2 - 1, Rb, 'h');
    x.save(); x.beginPath(); x.rect(0, 0, w, h); x.clip();
    if (bottom) { knot(x, w - 19, h - 30, 2.6, Rb, 'h'); tally(x, 15, 14, 3, 12, Rb); soak(x, w - 60, 4, 6, Rb); gouge(x, 30, h - 4, 60, h - 6, 1.1); } else { knot(x, 19, 36, 2.4, Rb); gouge(x, w - 98, 4, w - 80, 6); scorch(x, w - 19, h - 18, 5, Rb); tally(x, 60, h - 7, 9, 5, Rb); }
    x.restore();
    for (const ex of [0.5, w - 11.5]) iron(x, rect(ex, 0.5, 11, h - 1), Rb, [[ex + 5.5, 9, 2.4], [ex + 5.5, h - 9, 2.4]]);
    const p = tear([[24, 9], [w - 24, 8.5], [w - 24, h - 9], [24, h - 8.5]], true, 1.5, RNG('strip.edge' + bottom), 3, 0.04);
    paper(x, p, RNG('strip.paper' + bottom), { stains: [[52, h - 22, 20], [w - 60, 22, 16]] });
    x.save(); trace(x, p, true); x.clip();
    if (bottom) {
      burn(x, 36, 22, 6, R); grime(x, 70, h - 14, 22, 0.35); spatter(x, w - 72, h - 22, -0.5, 9, 3.2, R, false); print(x, w - 40, 26, 8, 0.8, R, '50,24,14', 0.4);
    } else {
      burn(x, w - 34, h - 22, 5, R); grime(x, w - 70, 16, 20, 0.35); spatter(x, 58, 20, 0.4, 10, 3.4, R, false); drop(x, 30, h - 18, 2.6, R, true);
      smear(x, [[w - 88, h - 12], [w - 60, h - 15], [w - 30, h - 12]], 5, R);
    }
    x.restore();
    if (bottom) {
      // three spent rounds standing on the beam at the left: the gunners' count
      for (let i = 0; i < 3; i++) cartridge(x, 50 + i * 11, h - 8, 7, 26, i === 2 ? 'spent' : 'live', R);
      knife(x, w - 4, 4, w - 60, h - 24, 1.35, R);
    } else {
      knife(x, 2, h - 4, 58, 24, 1.35, R);
      teethString(x, w - 88, 10, w - 22, 12, 8, 5, 1.2, R);
      nail(x, w - 88, 10, 2.2, R); nail(x, w - 22, 12, 2.2, R);
    }
  };
}

const kit = {
  // A panel: a torn, scorched sheet nailed over riveted steel; a knife through its top-left corner, a string of teeth at
  // its top right, a burn and a smear of dried blood at its foot. The middle is plain paper.
  'frame.panel': piece('frame.panel', [220, 170], [36, 64, 36, 38], (x, w, h, R) => {
    // four boards of the hall's oak round the sheet, butted at the corners and bound there with strap iron
    const Rw = RNG('panel.wood');
    boardFrame(x, w, h, 19, Rw);
    // the boards' scars, in the corners only: a knot, gouges, a captain's tally, a scorch and old blood in the grain
    knot(x, w - 9, h - 30, 2.8, Rw, 'v'); gouge(x, 4, h - 34, 11, h - 26); gouge(x, 6, h - 29, 13, h - 22, 1.2);
    tally(x, 19, h - 14, 5, 8, Rw); soak(x, w - 42, h - 10, 8, Rw, 'h'); scorch(x, w - 10, 27, 5, Rw); nailHole(x, w - 32, 8, Rw);
    for (const [cx, cy, sx, sy] of [[0.5, h - 0.5, 1, -1], [w - 0.5, h - 0.5, -1, -1], [w - 0.5, 0.5, -1, 1]]) cornerStrap(x, cx, cy, sx, sy, 24, 8, Rw);
    const p = tear([[15, 15], [w - 15, 16], [w - 16, h - 15], [w - 46, h - 14], [28, h - 15], [15, h - 28]], true, 2, RNG('panel.edge'), 3, 0.05);
    paper(x, p, RNG('panel.paper'), { stains: [[w - 34, h - 30, 18], [28, 46, 12]] });
    x.save(); trace(x, p, true); x.clip();
    burn(x, 22, h - 22, 5.5, R); grime(x, w - 26, h - 20, 22, 0.4); smear(x, [[w - 58, h - 17], [w - 38, h - 21], [w - 18, h - 26]], 6, R);
    spatter(x, w - 24, 52, 1.9, 10, 3, R, true); print(x, 22, 64, 7, 0.3, R); drip(x, w - 30, 30, 14, R, false); drip(x, 20, 30, 10, R, true);
    burn(x, w - 52, h - 16, 3.5, R);
    x.restore();
    nail(x, w - 17, h - 17, 2.2, R);
    knife(x, 1, 1, 33, 31, 1.3, R);
    teethString(x, w - 60, 9, w - 10, 11, 10, 4, 1.25, R); nail(x, w - 60, 9, 2.4, R); nail(x, w - 10, 11, 2.4, R);
  }),
  // A readout well: a scrap of the sheet nailed into a steel recess.
  'frame.inset': piece('frame.inset', [80, 60], [15, 15, 15, 15], (x, w, h, R) => {
    // a well cut into the board: its four sides dark oak, a scrap of the sheet nailed into it
    const Rw = RNG('inset.wood');
    boardFrame(x, w, h, 7, Rw, { tone: '#3a2415', hi: '#4f3320', lo: '#1a0f07' });
    x.strokeStyle = 'rgba(0,0,0,0.7)'; x.lineWidth = 1.5; x.strokeRect(6.5, 6.5, w - 13, h - 13);
    const p = tear([[6, 6], [w - 6, 6], [w - 6, h - 6], [6, h - 6]], true, 1.2, RNG('inset.edge'), 2.5, 0.04);
    paper(x, p, RNG('inset.paper'), { stains: [[w - 8, h - 8, 7]] });
    x.save(); trace(x, p, true); x.clip(); grime(x, 7, h - 7, 10, 0.45); drop(x, w - 9, 10, 1.6, R, false); x.restore();
    for (const [px, py] of [[8, 8], [w - 8, 8], [8, h - 8], [w - 8, h - 8]]) nail(x, px, py, 1.9, R);
  }),
  'bar.track': piece('bar.track', [120, 22], [0, 16, 0, 16], strap, { inset: [16, 3] }),
  'bar.fill': piece('bar.fill', [64, 10], [0, 8, 0, 6], (x, w, h) => { const g = x.createLinearGradient(0, 0, 0, h); g.addColorStop(0, RT.brassHi); g.addColorStop(1, RT.brassLo); x.fillStyle = g; x.fillRect(0, 1, w, h - 2); }),
  'bar.fill.red': piece('bar.fill.red', [64, 10], [0, 8, 0, 6], (x, w, h) => { const g = x.createLinearGradient(0, 0, 0, h); g.addColorStop(0, '#ff4a44'); g.addColorStop(1, RT.freshLo); x.fillStyle = g; x.fillRect(0, 1, w, h - 2); }),
  'bar.cell.off': { size: [10, 16], gap: 1.5, draw: loop('spent') },
  'bar.cell.on': { size: [10, 16], gap: 1.5, draw: loop('live') },
  'bar.cell.warn': { size: [10, 16], gap: 1.5, draw: loop('warn') },
  'button.idle': piece('button.idle', [176, 48], [11, 36, 11, 36], trapJaw('idle')),
  'button.hover': piece('button.hover', [176, 48], [11, 36, 11, 36], trapJaw('hover')),
  'button.pressed': piece('button.pressed', [176, 48], [11, 36, 11, 36], trapJaw('pressed')),
  'tab.idle': piece('tab.idle', [128, 40], [10, 20, 10, 20], ledgerTab(false)),
  'tab.active': piece('tab.active', [128, 40], [10, 20, 10, 20], ledgerTab(true)),
  'strip.top': piece('strip.top', [560, 72], [14, 100, 14, 100], stripPiece(false)),
  'strip.bottom': piece('strip.bottom', [560, 72], [14, 100, 14, 100], stripPiece(true)),
  // A divider: a lath of the hall's oak nailed along the sheet, a nick cut in it at each end.
  'divider': piece('divider', [128, 10], [0, 14, 0, 14], (x, w, h, R) => {
    const Rw = RNG('divider.wood');
    x.save(); x.shadowColor = 'rgba(0,0,0,0.6)'; x.shadowBlur = 2; x.shadowOffsetY = 1; x.fillStyle = RT.oakLo; x.fillRect(2, 1.5, w - 4, 7); x.restore();
    wood(x, rect(2, 1.5, w - 4, 7), Rw, { dir: 'h' });
    for (const nx of [11, w - 13]) { trace(x, [[nx, 1.5], [nx + 1, 4], [nx + 2, 1.5]], true); x.fillStyle = '#0b0603'; x.fill(); }
    nail(x, 6, h / 2, 2.2, R); nail(x, w - 6, h / 2, 2.2, R);
  }),
};

// ---- hero pieces --------------------------------------------------------------------------------------------------
// A wax seal, burned to close the sheet (the sheet-keepers' mark): dried-blood wax with a tooth pressed into it.
function seal(x, cx, cy, r, R) {
  grime(x, cx, cy, r * 1.9, 0.6, '60,26,8');
  const p = blob(cx, cy, r, R, 22, 0.22);
  x.save(); x.shadowColor = 'rgba(0,0,0,0.6)'; x.shadowBlur = 3; x.shadowOffsetY = 1.5; trace(x, p, true);
  const g = x.createRadialGradient(cx - r * 0.3, cy - r * 0.35, 1, cx, cy, r); g.addColorStop(0, '#8c2a1c'); g.addColorStop(0.7, RT.dried); g.addColorStop(1, RT.driedLo); x.fillStyle = g; x.fill(); x.restore();
  x.strokeStyle = 'rgba(20,6,2,0.6)'; x.lineWidth = 1; x.beginPath(); x.arc(cx, cy, r * 0.7, 0, 2 * P); x.stroke();
  x.save(); x.translate(cx, cy - r * 0.38); x.scale(r / 15, r / 15);
  x.fillStyle = 'rgba(25,6,3,0.75)'; x.beginPath(); x.moveTo(-3.6, 0); x.quadraticCurveTo(0, -2, 3.6, 0); x.quadraticCurveTo(4.6, 6, 2.2, 9); x.lineTo(0, 13); x.lineTo(-2.2, 9); x.quadraticCurveTo(-4.6, 6, -3.6, 0); x.fill();
  x.strokeStyle = 'rgba(200,110,80,0.35)'; x.lineWidth = 0.7; x.stroke(); x.restore();
  x.fillStyle = 'rgba(255,200,170,0.25)'; x.beginPath(); x.ellipse(cx - r * 0.4, cy - r * 0.45, r * 0.25, r * 0.12, -0.6, 0, 2 * P); x.fill();
}
// A trophy molar for the crest, crown up and roots down so it reads as a tooth at any size (a long canine read as a bone
// at 32 px): a glossy two-cusped crown of enamel over two darker roots, the line between them plain. (cx, cy) is the
// crown's middle; k its scale.
function crestTooth(x, cx, cy, k) {
  x.save(); x.translate(cx, cy); x.scale(k, k);
  const path = () => { x.beginPath(); x.moveTo(-12, 2); x.bezierCurveTo(-13.5, -8, -9, -14.5, -4.5, -12.5); x.quadraticCurveTo(-2, -10.5, 0, -10.2); x.quadraticCurveTo(2, -10.5, 4.5, -12.5);
    x.bezierCurveTo(9, -14.5, 13.5, -8, 12, 2); x.quadraticCurveTo(11, 6, 10, 9); x.quadraticCurveTo(8.6, 18, 6, 20); x.quadraticCurveTo(4, 19, 3.4, 10); x.quadraticCurveTo(0, 6.5, -3.4, 10);
    x.quadraticCurveTo(-4, 19, -6, 20); x.quadraticCurveTo(-8.6, 18, -10, 9); x.quadraticCurveTo(-11, 6, -12, 2); x.closePath(); };
  x.save(); x.shadowColor = 'rgba(0,0,0,0.8)'; x.shadowBlur = 4 / k; x.shadowOffsetX = 1.2 / k; x.shadowOffsetY = 2 / k; path(); x.fillStyle = '#f2ead2'; x.fill(); x.restore();
  path(); x.save(); x.clip();
  const g = x.createLinearGradient(-12, 0, 12, 0); g.addColorStop(0, '#fffaf0'); g.addColorStop(0.5, '#efe4c6'); g.addColorStop(1, '#a8987a'); x.fillStyle = g; x.fillRect(-14, -15, 28, 18);
  const r = x.createLinearGradient(0, 3, 0, 20); r.addColorStop(0, '#c8a876'); r.addColorStop(1, '#6e5030'); x.fillStyle = r; x.fillRect(-14, 3, 28, 18);
  x.strokeStyle = 'rgba(80,55,30,0.7)'; x.lineWidth = 0.8; x.beginPath(); x.moveTo(-13, 3.2); x.quadraticCurveTo(0, 5.2, 13, 3.2); x.stroke();
  x.fillStyle = 'rgba(255,255,255,0.75)'; x.beginPath(); x.ellipse(-6, -7, 2.2, 4.2, 0.3, 0, 2 * P); x.fill();
  x.fillStyle = 'rgba(90,20,12,0.55)'; x.beginPath(); x.ellipse(5, 15, 2.6, 4, 0, 0, 2 * P); x.fill();
  x.restore();
  path(); x.strokeStyle = '#1a0f08'; x.lineWidth = 1.3; x.stroke();
  x.restore();
}
function crestMark(x, w, R) {
  const c = w / 2;
  // a roundel of the hall's oak bound in an iron hoop; on it a drop of blood, the tithe; two cutters' blades crossed
  // behind; a tooth taken in trophy over all
  const disc = F.arcPts(c, c, 46, 46, 0, 2 * P, 56);
  x.save(); x.shadowColor = 'rgba(0,0,0,0.7)'; x.shadowBlur = 5; x.shadowOffsetY = 2; trace(x, disc, true); x.fillStyle = RT.oakLo; x.fill(); x.restore();
  wood(x, disc, R, { dir: 'v' });
  x.strokeStyle = '#1b1d1f'; x.lineWidth = 3.4; x.beginPath(); x.arc(c, c, 44.5, 0, 2 * P); x.stroke();
  x.strokeStyle = 'rgba(190,196,200,0.35)'; x.lineWidth = 0.7; x.beginPath(); x.arc(c, c, 45.5, P * 0.95, P * 1.6); x.stroke();
  knife(x, 14, 82, 72, 24, 1.15, R, { blood: false }); knife(x, 82, 82, 24, 24, 1.15, R, { blood: false });
  const d = []; for (let i = 0; i < 48; i++) { const t = i * 2 * P / 48; d.push([c + 29 * Math.sin(t) * Math.sin(t / 2), c + 8 - 33 * Math.cos(t)]); }
  x.save(); x.shadowColor = 'rgba(0,0,0,0.7)'; x.shadowBlur = 5; x.shadowOffsetY = 2; trace(x, d, true);
  const g = x.createRadialGradient(c - 8, c + 2, 2, c, c + 10, 32); g.addColorStop(0, '#d0262c'); g.addColorStop(0.6, '#8a0c12'); g.addColorStop(1, '#3a0406'); x.fillStyle = g; x.fill(); x.restore();
  trace(x, d, true); x.strokeStyle = '#1a0304'; x.lineWidth = 1.4; x.stroke();
  x.fillStyle = 'rgba(255,220,220,0.5)'; x.beginPath(); x.ellipse(c - 12, c + 6, 3.4, 7, 0.3, 0, 2 * P); x.fill();
  crestTooth(x, c, c + 8, 1.15);
}
function lamp(col, hi) {
  return (x, w, h) => {
    const R = RNG('lamp'), c = w / 2;
    // a steel trap ring, toothed inward, round a thick lens
    steel(x, F.arcPts(c, c, 30, 30, 0, 2 * P, 40), R);
    for (let i = 0; i < 12; i++) { const a = i * P / 6; const p = [[c + Math.cos(a - 0.18) * 24, c + Math.sin(a - 0.18) * 24], [c + Math.cos(a) * 19, c + Math.sin(a) * 19], [c + Math.cos(a + 0.18) * 24, c + Math.sin(a + 0.18) * 24]]; trace(x, p, true); x.fillStyle = '#c3cad0'; x.fill(); x.strokeStyle = '#0b0c0d'; x.lineWidth = 0.5; x.stroke(); }
    x.save(); x.shadowColor = col; x.shadowBlur = 12; const g = x.createRadialGradient(c - 5, c - 6, 1, c, c, 18); g.addColorStop(0, hi); g.addColorStop(0.55, col); g.addColorStop(1, 'rgba(0,0,0,0.85)'); x.fillStyle = g; x.beginPath(); x.arc(c, c, 18, 0, 2 * P); x.fill(); x.restore();
    x.fillStyle = 'rgba(255,255,255,0.55)'; x.beginPath(); x.ellipse(c - 6, c - 7, 5, 2.6, -0.6, 0, 2 * P); x.fill();
    // grime caked on the glass edge and a dried thumb smear on the ring
    x.save(); x.beginPath(); x.arc(c, c, 18, 0, 2 * P); x.clip(); grime(x, c + 10, c + 12, 14, 0.5); x.restore();
    for (const a of [0.8, 2.4, 4.0, 5.6]) rivet(x, c + Math.cos(a) * 27, c + Math.sin(a) * 27, 1.8);
  };
}
const hero = {
  // A bounty sheet with a burned seal nailed to a plank of the hall's oak: the captain's name typed into its field
  // (64-404 x 10-46). The plank's bare right end carries the knife and the captain's tally.
  'nameplate': (x, w, h) => {
    const R = RNG('nameplate'), Rw = RNG('plate.wood');
    board(x, 0.5, 0.5, w - 1, h - 1, Rw, 'h');
    knot(x, w - 22, h - 13, 2.6, Rw); tally(x, w - 46, h - 15, 6, 9, Rw); soak(x, w - 30, 8, 6, Rw); gouge(x, w - 48, 6, w - 36, 9, 1.3); nailHole(x, 8, 7, Rw);
    for (const ex of [0.5]) iron(x, rect(ex, 0.5, 10, h - 1), Rw, [[ex + 5, 8, 2.2], [ex + 5, h - 8, 2.2]]);
    const p = tear([[13, 5], [w - 70, 4], [w - 50, 6], [w - 51, h - 5], [w * 0.6, h - 4], [13, h - 6]], true, 2, RNG('plate.edge'), 3, 0.05);
    paper(x, p, RNG('plate.paper'), { stains: [[w - 76, h - 16, 12]] });
    x.save(); trace(x, p, true); x.clip(); burn(x, w - 60, h - 11, 4, R); grime(x, 34, h - 6, 24, 0.4); spatter(x, w - 88, 12, 0.2, 7, 2.2, R, false);
    x.strokeStyle = 'rgba(60,40,20,0.25)'; x.lineWidth = 0.6; x.beginPath(); x.moveTo(62, h - 9); x.lineTo(404, h - 9); x.stroke(); x.restore();
    nail(x, w - 56, 9, 2.2, R); nail(x, w - 57, h - 9, 2.2, R);
    seal(x, 32, h / 2, 17, R);
    knife(x, w - 1, 1, w - 30, h / 2 + 4, 1.15, R);
  },
  // The helm: a revolver's cylinder (the gunners' tradition) set in a wheel of the hall's oak, its four turned handles
  // reaching into the corners of a plank door banded with strap iron. Eight chambers, each a round's brass head, ring
  // the ejector, the stop key; the arrows are stamped live on the brass.
  'helm.pad': (x, w, h) => {
    const R = RNG('helm'), Rw = RNG('helm.wood'), c = w / 2;
    // three planks, grain down, scarred in the corners the wheel leaves bare
    for (let i = 0; i < 3; i++) board(x, 1 + i * (w - 2) / 3, 1, (w - 2) / 3, h - 2, Rw, 'v');
    tally(x, 12, 30, 9, 11, Rw); gouge(x, w - 40, 26, w - 18, 40, 1.8); gouge(x, w - 36, 34, w - 20, 46, 1.2); knot(x, w - 26, h - 50, 3.2, Rw, 'v');
    soak(x, 26, h - 40, 11, Rw, 'v'); scorch(x, w - 30, h - 30, 7, Rw); nailHole(x, 40, 52, Rw); nailHole(x, w - 52, 12, Rw);
    for (const y of [6, h - 18]) iron(x, rect(1, y, w - 2, 12), Rw, [[16, y + 6], [w / 3 + 14, y + 6], [2 * w / 3 + 14, y + 6], [w - 16, y + 6]]);
    // the wheel's four handles, turned and worn pale where hands have hauled on them
    for (let i = 0; i < 4; i++) {
      const a = -P / 4 + i * P / 2; x.save(); x.translate(c, c); x.rotate(a);
      x.save(); x.shadowColor = 'rgba(0,0,0,0.7)'; x.shadowBlur = 5; x.shadowOffsetX = 2; x.shadowOffsetY = 3;
      x.beginPath(); x.moveTo(140, -8); x.lineTo(160, -6); x.quadraticCurveTo(166, -11, 176, -10); x.quadraticCurveTo(192, -9, 192, 0); x.quadraticCurveTo(192, 9, 176, 10); x.quadraticCurveTo(166, 11, 160, 6); x.lineTo(140, 8); x.closePath();
      const g = x.createLinearGradient(0, -11, 0, 11); g.addColorStop(0, '#9a6a40'); g.addColorStop(0.4, '#6a4426'); g.addColorStop(1, '#1e1108'); x.fillStyle = g; x.fill(); x.restore();
      x.strokeStyle = '#0c0704'; x.lineWidth = 1; x.stroke();
      x.strokeStyle = 'rgba(14,7,2,0.5)'; x.lineWidth = 0.5; for (const y of [-5, -1.5, 3, 6]) { x.beginPath(); x.moveTo(142, y * 0.9); x.lineTo(190, y); x.stroke(); }
      x.strokeStyle = 'rgba(0,0,0,0.6)'; x.lineWidth = 1.2; for (const t of [160, 170]) { x.beginPath(); x.moveTo(t, -9); x.lineTo(t, 9); x.stroke(); }
      x.restore();
    }
    // the wheel's rim: eight felloes of oak round the cylinder, pinned at their joints, an iron tyre round the outside
    const rim = F.arcPts(c, c, 148, 148, 0, 2 * P, 72);
    x.save(); x.shadowColor = 'rgba(0,0,0,0.8)'; x.shadowBlur = 10; x.shadowOffsetX = 3; x.shadowOffsetY = 5; trace(x, rim, true); x.fillStyle = RT.oakLo; x.fill(); x.restore();
    wood(x, rim, Rw, { dir: 'h', tone: '#5a3a20', hi: '#7a5232', lo: '#24150a' });
    x.strokeStyle = 'rgba(12,6,2,0.9)'; x.lineWidth = 1.4;
    for (let i = 0; i < 8; i++) { const a = -P / 2 + (i + 0.5) * P / 4; x.beginPath(); x.moveTo(c + Math.cos(a) * 128, c + Math.sin(a) * 128); x.lineTo(c + Math.cos(a) * 146, c + Math.sin(a) * 146); x.stroke(); rivet(x, c + Math.cos(a + 0.07) * 138, c + Math.sin(a + 0.07) * 138, 2); }
    x.strokeStyle = '#1b1d1f'; x.lineWidth = 3.5; x.beginPath(); x.arc(c, c, 146.5, 0, 2 * P); x.stroke();
    x.strokeStyle = 'rgba(190,196,200,0.35)'; x.lineWidth = 0.8; x.beginPath(); x.arc(c, c, 147.5, P * 0.95, P * 1.6); x.stroke();
    // the cylinder
    const cyl = []; for (let i = 0; i < 64; i++) { const a = i * 2 * P / 64, f = Math.cos(8 * (a + P / 8)) > 0.92 ? 0.95 : 1; cyl.push([c + Math.cos(a) * 128 * f, c + Math.sin(a) * 128 * f]); }
    x.save(); x.shadowColor = 'rgba(0,0,0,0.8)'; x.shadowBlur = 8; x.shadowOffsetX = 2; x.shadowOffsetY = 4; trace(x, cyl, true); x.fillStyle = '#2a2e32'; x.fill(); x.restore();
    steel(x, cyl, RNG('helm.cyl'), { hi: '#5a6168', mid: '#30353a', lo: '#121416' });
    // flutes between the chambers
    for (let i = 0; i < 8; i++) { const a = -P / 2 + (i + 0.5) * P / 4; x.save(); x.translate(c + Math.cos(a) * 112, c + Math.sin(a) * 112); x.rotate(a); const g = x.createLinearGradient(0, -6, 0, 6); g.addColorStop(0, '#0d0f11'); g.addColorStop(1, '#5d646b'); x.fillStyle = g; x.beginPath(); x.ellipse(0, 0, 13, 5, 0, 0, 2 * P); x.fill(); x.restore(); }
    // eight chambers: a round's head in each
    for (let i = 0; i < 8; i++) {
      const a = -P / 2 + i * P / 4, px = c + Math.cos(a) * 92, py = c + Math.sin(a) * 92;
      x.fillStyle = '#060707'; x.beginPath(); x.arc(px, py, 30, 0, 2 * P); x.fill();
      x.strokeStyle = 'rgba(200,206,212,0.35)'; x.lineWidth = 1; x.beginPath(); x.arc(px, py, 30, P * 0.1, P * 0.9); x.stroke();
      const g = x.createRadialGradient(px - 8, py - 9, 2, px, py, 27); g.addColorStop(0, RT.brassHi); g.addColorStop(0.55, RT.brass); g.addColorStop(1, RT.brassLo);
      x.fillStyle = g; x.beginPath(); x.arc(px, py, 26.5, 0, 2 * P); x.fill(); x.strokeStyle = 'rgba(60,40,10,0.9)'; x.lineWidth = 1; x.stroke();
      x.strokeStyle = 'rgba(80,55,15,0.55)'; x.lineWidth = 0.8; x.beginPath(); x.arc(px, py, 22, 0, 2 * P); x.stroke();
      // grime in the extractor groove and a dried print on one or two of them
      x.save(); x.beginPath(); x.arc(px, py, 26.5, 0, 2 * P); x.clip(); grime(x, px + 14, py + 16, 18, 0.35); if (i === 1 || i === 6) print(x, px + 13, py - 12, 7, a, R, '70,22,12', 0.45); x.restore();
    }
    // the ejector star and its rod: the stop key, a brass boss in the middle
    const star = []; for (let i = 0; i < 16; i++) { const a = -P / 2 + i * P / 8, r = i % 2 ? 40 : 50; star.push([c + Math.cos(a) * r, c + Math.sin(a) * r]); }
    steel(x, star, RNG('helm.star'), { hi: '#8a9198', mid: '#4a5157', lo: '#1a1d20' });
    const b = x.createRadialGradient(c - 10, c - 12, 3, c, c, 36); b.addColorStop(0, RT.brassHi); b.addColorStop(0.6, RT.brass); b.addColorStop(1, RT.brassLo);
    x.fillStyle = b; x.beginPath(); x.arc(c, c, 34, 0, 2 * P); x.fill(); x.strokeStyle = 'rgba(40,25,5,0.9)'; x.lineWidth = 1.2; x.stroke();
    x.save(); x.beginPath(); x.arc(c, c, 34, 0, 2 * P); x.clip(); grime(x, c + 16, c + 20, 22, 0.35); x.restore();
  },
  // The systems display: a bounty sheet nailed into a frame of the hall's oak, a blade through three corners and a
  // hatchet left in the fourth, strap iron at the middle of each long board; its field clear for the ship drawn live.
  'ssd.bezel': (x, w, h) => {
    const R = RNG('ssd'), Rw = RNG('ssd.wood');
    boardFrame(x, w, h, 28, Rw);
    knot(x, w * 0.3, 12, 3, Rw); knot(x, 13, h * 0.62, 3.2, Rw, 'v'); knot(x, w * 0.66, h - 9, 2.4, Rw); tally(x, w * 0.36, h - 19, 12, 10, Rw); tally(x, w - 19, 48, 4, 9, Rw);
    gouge(x, w - 18, h * 0.32, w - 8, h * 0.48, 1.6); gouge(x, w - 14, h * 0.36, w - 6, h * 0.46, 1.1); gouge(x, w * 0.14, h - 8, w * 0.27, h - 17, 1.3); gouge(x, 8, 64, 18, 86, 1.4);
    soak(x, w * 0.8, 13, 10, Rw); soak(x, 14, h * 0.8, 9, Rw, 'v'); scorch(x, 14, h * 0.3, 6, Rw); scorch(x, w * 0.58, 10, 5, Rw); nailHole(x, w * 0.4, h - 12, Rw); nailHole(x, w - 13, h * 0.7, Rw); nailHole(x, w * 0.18, 10, Rw);
    for (const [px, py, ww, hh] of [[w / 2 - 8, 0.5, 16, 30], [w / 2 - 8, h - 30.5, 16, 30]]) iron(x, rect(px, py, ww, hh), Rw, [[px + 8, py + 6, 2.6], [px + 8, py + hh - 6, 2.6]]);
    const p = tear([[24, 23], [w * 0.45, 21], [w - 24, 24], [w - 22, h * 0.5], [w - 24, h - 23], [w * 0.55, h - 22], [24, h - 24], [22, h * 0.4]], true, 2.4, RNG('ssd.edge'), 3, 0.05);
    paper(x, p, RNG('ssd.paper'), { stains: [[76, h - 66, 28], [w - 76, 64, 22]] });
    x.save(); trace(x, p, true); x.clip();
    // margins only: the ship is drawn within 110 px of (230, 150)
    burn(x, w - 64, h - 104, 9, R); burn(x, 56, 82, 5, R); burn(x, w - 96, 46, 3, R); grime(x, 46, h - 54, 32, 0.45); grime(x, w - 38, 46, 28, 0.4);
    spatter(x, 70, h - 88, -0.5, 16, 4.2, R, false); smear(x, [[w - 34, 74], [w - 38, 112], [w - 32, 150]], 7, R); print(x, w - 66, 110, 9, 0.6, R); print(x, 70, 150, 9, -0.3, R, '80,22,14', 0.45);
    drop(x, w - 118, 50, 3.2, R, true, P / 2); drip(x, w - 106, 42, 20, R, true); drip(x, 106, h - 72, 14, R, false);
    // the sheet's ruled frame and its typed header
    x.strokeStyle = 'rgba(40,24,12,0.45)'; x.lineWidth = 0.8; x.strokeRect(32, 30, w - 64, h - 60);
    x.fillStyle = 'rgba(30,20,12,0.75)'; x.font = '400 11px "IM Fell English SC"'; x.fillText('TITHE OWED · SHEET 114', 58, 46); x.fillText('PAID IN FULL: NOT YET', 58, h - 38);
    x.restore();
    for (const [px, py] of [[w * 0.25, 26], [w * 0.75, 26], [w * 0.25, h - 26], [w * 0.36, h - 26], [26, h * 0.5], [w - 26, h * 0.5]]) nail(x, px, py, 2.3, R);
    knife(x, 1, 1, 46, 38, 1.4, R); knife(x, w - 1, 1, w - 46, 38, 1.4, R); knife(x, 1, h - 1, 46, h - 38, 1.4, R); hatchet(x, w - 96, h - 62, w - 30, h - 38, 1.3, R);
  },
  // A gauge behind cracked, bloodied glass in a riveted steel ring, set in a case of the hall's oak.
  'dial': (x, w, h) => {
    const R = RNG('dial'), c = w / 2;
    // a case of the hall's oak, its corners bound with strap iron, the gauge's steel ring set into it
    const Rw = RNG('dial.wood');
    x.save(); x.shadowColor = 'rgba(0,0,0,0.7)'; x.shadowBlur = 5; x.shadowOffsetY = 2; trace(x, F.rrPts(1, 1, w - 2, h - 2, 8), true); x.fillStyle = RT.oakLo; x.fill(); x.restore();
    wood(x, F.rrPts(1, 1, w - 2, h - 2, 8), Rw, { dir: 'v' });
    tally(x, 8, h - 15, 4, 7, Rw); knot(x, w - 10, 14, 2, Rw, 'v');
    for (const [cx, cy, sx, sy] of [[1, 1, 1, 1], [w - 1, 1, -1, 1], [1, h - 1, 1, -1], [w - 1, h - 1, -1, -1]]) cornerStrap(x, cx, cy, sx, sy, 18, 6, Rw);
    steel(x, F.arcPts(c, c, 53, 53, 0, 2 * P, 48), RNG('dial.ring'), { hi: '#6b737a', mid: '#353a3f', lo: '#121416' });
    for (let i = 0; i < 8; i++) { const a = i * P / 4 + P / 8; rivet(x, c + Math.cos(a) * 49.5, c + Math.sin(a) * 49.5, 1.8); }
    const f = x.createRadialGradient(c, c - 8, 4, c, c, 46); f.addColorStop(0, '#2c2620'); f.addColorStop(1, '#0d0b09'); x.fillStyle = f; x.beginPath(); x.arc(c, c, 46, 0, 2 * P); x.fill();
    x.strokeStyle = 'rgba(200,150,58,0.7)'; x.lineWidth = 1;
    for (let i = 0; i < 36; i++) { const a = i * P / 18, r0 = i % 3 ? 41 : 37; x.beginPath(); x.moveTo(c + Math.cos(a) * r0, c + Math.sin(a) * r0); x.lineTo(c + Math.cos(a) * 45, c + Math.sin(a) * 45); x.stroke(); }
    x.strokeStyle = 'rgba(200,150,58,0.25)'; for (const r of [14, 28]) { x.beginPath(); x.arc(c, c, r, 0, 2 * P); x.stroke(); }
    // the glass: a glint, a star of cracks from one blow at the edge, dried blood caked along the rim
    x.save(); x.beginPath(); x.arc(c, c, 46, 0, 2 * P); x.clip();
    const sx = c + 30, sy = c - 30; x.strokeStyle = 'rgba(235,240,245,0.55)'; x.lineWidth = 0.7;
    for (let i = 0; i < 7; i++) { const a = P * 0.55 + i * 0.22 + (R() - 0.5) * 0.15; let px = sx, py = sy; x.beginPath(); x.moveTo(px, py); for (let k = 0; k < 4; k++) { px += Math.cos(a + (R() - 0.5) * 0.5) * (8 + R() * 10); py += Math.sin(a + (R() - 0.5) * 0.5) * (8 + R() * 10); x.lineTo(px, py); } x.stroke(); }
    x.strokeStyle = 'rgba(235,240,245,0.35)'; for (const r of [6, 11]) { x.beginPath(); x.arc(sx, sy, r, P * 0.4, P * 1.2); x.stroke(); }
    smear(x, F.arcPts(c, c, 43, 43, P * 0.65, P * 1.05, 10), 6, R, '#5a120c'); drop(x, c - 30, c + 34, 2.4, R, false);
    const gl = x.createLinearGradient(c - 40, c - 40, c, c); gl.addColorStop(0, 'rgba(255,255,255,0.16)'); gl.addColorStop(1, 'rgba(255,255,255,0)'); x.fillStyle = gl; x.beginPath(); x.ellipse(c - 14, c - 18, 28, 14, -0.7, 0, 2 * P); x.fill();
    x.restore();
    x.strokeStyle = '#08090a'; x.lineWidth = 1.4; x.beginPath(); x.arc(c, c, 46, 0, 2 * P); x.stroke();
  },
  'lamp.alert.green': lamp('#5f9a45', '#d4f0a8'), 'lamp.alert.yellow': lamp('#d39a1e', '#fff0b0'), 'lamp.alert.red': lamp(RT.fresh, '#ffb0a8'),
  'crest': (x, w) => crestMark(x, w, RNG('crest')),
};

// ---- icons: cut into the sheet with a blade -----------------------------------------------------------------------
// Each subject is one path, cut in: a pale scored rim where the blade lifted the paper, the dark cut itself, stencil
// bridges left uncut, and a scratch or two across it. Readable at 20 px; colour for state only.
// Every subject of the icon guide (docs/design/icon-guide.html), so the battle console finds the same pictures as every
// other skin. col: the cut's ink, the sheet's black by default; a light ink (the battle console's, on dark boards) is cut
// into the wood instead, its rim a shadow where the paper's was a lifted pale edge.
const ICON_NAMES = ['helm', 'power', 'tactical', 'pd', 'science', 'comms', 'repair', 'transport', 'fleet', 'ship', 'shields', 'sensors', 'tractor', 'craft', 'weapons', 'warp', 'engines', 'reactor', 'map', 'pause', 'menu', 'fire', 'cargo', 'settings'];
function icon(name, col = RT.ink) {
  const n = parseInt(col.slice(1, 7), 16), light = ((n >> 16) * 0.3 + ((n >> 8) & 255) * 0.59 + (n & 255) * 0.11) > 110;
  return F.paint(48, 48, (x) => {
    const R = RNG('icon.' + name);
    const ops = [];   // [kind, fn]: 's' stroked path, 'f' filled path
    const S = (fn, lw = 4) => ops.push(['s', fn, lw]), Fi = (fn) => ops.push(['f', fn]);
    const circ = (cx, cy, r) => (q) => { q.beginPath(); q.arc(cx, cy, r, 0, 2 * P); };
    const poly = (pts, close = true) => (q) => { q.beginPath(); q.moveTo(pts[0][0], pts[0][1]); for (const p of pts.slice(1)) q.lineTo(p[0], p[1]); if (close) q.closePath(); };
    ({
      // a ship's wheel
      helm: () => { S(circ(24, 24, 13), 4.2); Fi(circ(24, 24, 4.5)); for (let i = 0; i < 8; i++) { const a = i * P / 4, c = Math.cos(a), s = Math.sin(a); S(poly([[24 + c * 4, 24 + s * 4], [24 + c * 21, 24 + s * 21]], false), 3.2); } },
      // a round leaving the muzzle, bursting ahead
      fire: () => { Fi(poly([[6, 19], [24, 19], [33, 24], [24, 29], [6, 29]])); for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3 + 0.3; S(poly([[39 + Math.cos(a) * 3, 24 + Math.sin(a) * 3], [39 + Math.cos(a) * 8, 24 + Math.sin(a) * 8]], false), 2.6); } },
      // six facings round the ship
      shields: () => { for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; S((q) => { q.beginPath(); q.arc(24, 24, 18, a - P / 6 + 0.17, a + P / 6 - 0.17); }, 4); } Fi(poly([[24, 13], [30, 32], [24, 29], [18, 32]])); },
      // an emitter throwing a widening beam onto a box
      tractor: () => { Fi(poly([[4, 19], [12, 19], [12, 29], [4, 29]])); S(poly([[13, 21], [32, 11]], false), 2.4); S(poly([[13, 27], [32, 37]], false), 2.4); Fi(poly([[33, 15], [45, 15], [45, 33], [33, 33]])); },
      // a figure, solid above the waist, dissolving below
      transport: () => { Fi(circ(24, 9, 5)); Fi(poly([[15, 16], [33, 16], [31, 27], [17, 27]])); for (let i = 0; i < 3; i++) Fi(poly([[17 + i, 30 + i * 5], [31 - i, 30 + i * 5], [31 - i, 32.5 + i * 5], [17 + i, 32.5 + i * 5]])); },
      // an atom, three orbits
      science: () => { for (let i = 0; i < 3; i++) S((q) => { q.beginPath(); q.ellipse(24, 24, 20, 7.5, i * P / 3, 0, 2 * P); }, 2.6); Fi(circ(24, 24, 4.5)); },
      // a dish on a mast, sending
      comms: () => { Fi(poly([[10, 22], [24, 36], [20, 40], [6, 26]])); S(poly([[17, 33], [17, 44], [11, 44], [23, 44]], false), 3); S((q) => { q.beginPath(); q.arc(16, 30, 16, -P * 0.5, -P * 0.05); }, 2.4); S((q) => { q.beginPath(); q.arc(16, 30, 24, -P * 0.45, -P * 0.1); }, 2.4); },
      // a wrench
      repair: () => { S(poly([[12, 36], [30, 18]], false), 6); S((q) => { q.beginPath(); q.arc(34, 14, 8, P * 0.95, P * 2.45); }, 5); Fi(circ(10, 38, 4.5)); },
      // a folded chart, the course marked to a cross
      map: () => { S(poly([[5, 11], [17, 7], [31, 11], [43, 7], [43, 37], [31, 41], [17, 37], [5, 41]]), 3); S(poly([[17, 7], [17, 37]], false), 2); S(poly([[31, 11], [31, 41]], false), 2); S(poly([[10, 33], [20, 26], [28, 29]], false), 2.2); S(poly([[32, 17], [40, 25]], false), 2.6); S(poly([[40, 17], [32, 25]], false), 2.6); },
      // a gear
      settings: () => { const o = []; for (let i = 0; i < 16; i++) { const a = i * P / 8, r = i % 2 ? 13 : 19; o.push([24 + Math.cos(a - 0.12) * r, 24 + Math.sin(a - 0.12) * r], [24 + Math.cos(a + 0.12) * r, 24 + Math.sin(a + 0.12) * r]); } Fi(poly(o)); },
      // power: a bolt in a ring
      power: () => { S(circ(24, 24, 19), 3.4); Fi(poly([[27, 8], [15, 26], [23, 26], [20, 40], [33, 21], [25, 21]])); },
      // tactical: a turret on its base, twin barrels
      tactical: () => { Fi(poly([[7, 39], [41, 39], [41, 44], [7, 44]])); Fi(poly([[12, 39], [15, 27], [33, 27], [36, 39]])); S(poly([[25, 30], [42, 13]], false), 3.6); S(poly([[21, 28], [38, 11]], false), 3.6); },
      // point defence: a shield with a burst on it
      pd: () => { S(poly([[24, 5], [40, 11], [40, 25], [24, 43], [8, 25], [8, 11]]), 3.4); const o = []; for (let i = 0; i < 16; i++) { const a = -P / 2 + i * P / 8, r = i % 2 ? 4 : 9.5; o.push([24 + Math.cos(a) * r, 23 + Math.sin(a) * r]); } Fi(poly(o)); },
      // fleet: three chevrons in formation, one ahead
      fleet: () => { const ch = (cx, cy, k) => Fi(poly([[cx, cy - 8 * k], [cx + 9 * k, cy + 7 * k], [cx, cy + 2 * k], [cx - 9 * k, cy + 7 * k]])); ch(24, 14, 1.3); ch(11, 34, 1.05); ch(37, 34, 1.05); },
      // the ship from above: a raider's long hull with a ram, two outrigger pods
      ship: () => { Fi(poly([[24, 3], [30, 14], [29, 38], [24, 44], [19, 38], [18, 14]])); Fi(poly([[6, 24], [12, 24], [12, 44], [6, 44]])); Fi(poly([[36, 24], [42, 24], [42, 44], [36, 44]])); S(poly([[12, 31], [19, 29]], false), 2.6); S(poly([[36, 31], [29, 29]], false), 2.6); },
      // sensors: a scope, its sweep and two contacts
      sensors: () => { S(circ(24, 24, 19), 3); Fi((q) => { q.beginPath(); q.moveTo(24, 24); q.arc(24, 24, 17, -P / 2, -P / 2 + 1); q.closePath(); }); Fi(poly([[31, 28], [37, 28], [37, 34], [31, 34]])); Fi(poly([[11, 30], [16, 30], [16, 35], [11, 35]])); },
      // craft: a fighter from above, swept wings, twin tails
      craft: () => { Fi(poly([[24, 4], [29, 22], [44, 31], [44, 35], [29, 33], [28, 41], [33, 45], [15, 45], [20, 41], [19, 33], [4, 35], [4, 31], [19, 22]])); },
      // weapons: an emitter firing a bolt that bursts
      weapons: () => { Fi(circ(10, 38, 5.5)); S(poly([[14, 34], [30, 18]], false), 4); const o = []; for (let i = 0; i < 12; i++) { const a = i * P / 6, r = i % 2 ? 3.5 : 9; o.push([35 + Math.cos(a) * r, 13 + Math.sin(a) * r]); } Fi(poly(o)); },
      // warp: a ring, speed lines trailing behind
      warp: () => { S(circ(33, 24, 8), 3.4); Fi(circ(33, 24, 2.6)); S(poly([[5, 15], [21, 15]], false), 3.2); S(poly([[2, 24], [21, 24]], false), 3.2); S(poly([[5, 33], [21, 33]], false), 3.2); },
      // engines: a nozzle and its flame
      engines: () => { Fi(poly([[18, 4], [30, 4], [30, 12], [18, 12]])); Fi(poly([[17, 12], [31, 12], [37, 26], [11, 26]])); S(poly([[13, 30], [24, 37], [35, 30]], false), 3.4); S(poly([[17, 38], [24, 43], [31, 38]], false), 3); },
      // reactor: the trefoil round a core
      reactor: () => { Fi(circ(24, 24, 3.8)); for (let i = 0; i < 3; i++) { const a = -P / 2 + i * 2 * P / 3; Fi((q) => { q.beginPath(); q.arc(24, 24, 19, a - 0.52, a + 0.52); q.arc(24, 24, 7, a + 0.52, a - 0.52, true); q.closePath(); }); } },
      pause: () => { Fi(poly([[13, 10], [21, 10], [21, 38], [13, 38]])); Fi(poly([[27, 10], [35, 10], [35, 38], [27, 38]])); },
      menu: () => { for (const y of [13, 24, 35]) S(poly([[10, y], [38, y]], false), 5); },
      // cargo: four crates
      cargo: () => { for (const [cx, cy] of [[15, 15], [33, 15], [15, 33], [33, 33]]) Fi(poly([[cx - 7.5, cy - 7.5], [cx + 7.5, cy - 7.5], [cx + 7.5, cy + 7.5], [cx - 7.5, cy + 7.5]])); },
    })[name]();
    const draw = (lwAdd, col, dx = 0, dy = 0) => {
      x.save(); x.translate(dx, dy);
      for (const [k, fn, lw] of ops) { fn(x); if (k === 's') { x.strokeStyle = col; x.lineWidth = lw + lwAdd; x.lineCap = 'butt'; x.lineJoin = 'miter'; x.stroke(); } else { x.fillStyle = col; x.fill(); if (lwAdd) { x.strokeStyle = col; x.lineWidth = lwAdd; x.lineJoin = 'miter'; x.stroke(); } } }
      x.restore();
    };
    draw(3.2, light ? 'rgba(10,6,3,0.85)' : 'rgba(230,214,176,0.92)', 0.6, 0.8);   // the scored rim the blade lifted (a shadow, in wood)
    draw(0, col);                                                                  // the cut
    // stencil bridges and blade scratches, taken out of the cut
    x.globalCompositeOperation = 'source-atop';
    x.strokeStyle = light ? 'rgba(90,64,40,0.8)' : 'rgba(120,96,70,0.9)'; x.lineWidth = 0.6;
    for (let i = 0; i < 3; i++) { const sx = 6 + R() * 36, sy = 6 + R() * 36, a = -0.9 + (R() - 0.5) * 0.4, L = 8 + R() * 12; x.beginPath(); x.moveTo(sx, sy); x.lineTo(sx + Math.cos(a) * L, sy + Math.sin(a) * L); x.stroke(); }
    x.globalCompositeOperation = 'source-over';
  });
}

// ---- the ground behind the column ---------------------------------------------------------------------------------
// A wall of the hall (CLAUDE.md section 7: no black behind a metallic skin's panels): rough planks stood on end, each its
// own width and tone, butted with dark gaps, nailed to two cross battens under strap iron, and scarred where the sheets
// leave it bare: knots, nail holes of sheets long paid, knife gouges, tallies, scorches and old blood in the grain.
// Drawn once at the column's size (472 x 1080) and handed to the page as the kit's optional `column` image.
const GROUND = [472, 1080];
function ground(x, w, h) {
  const R = RNG('ground');
  x.fillStyle = '#0c0704'; x.fillRect(0, 0, w, h);
  const xs = [0]; while (xs[xs.length - 1] < w) xs.push(xs[xs.length - 1] + 66 + R() * 30);
  for (let i = 0; i + 1 < xs.length; i++) board(x, xs[i] + 1, 0, xs[i + 1] - xs[i] - 2, h, R, 'v', { tone: '#3d2716', hi: '#523520', lo: '#1c1109' });
  // the battens behind, seen as rows of nail heads and an iron band across the planks
  for (const y of [h * 0.29, h * 0.71]) {
    iron(x, rect(0, y - 7, w, 14), R, []);
    for (let i = 0; i + 1 < xs.length; i++) for (const f of [0.3, 0.7]) nail(x, xs[i] + (xs[i + 1] - xs[i]) * f, y, 2.4, R);
  }
  // scars, spread by the seeded generator over the whole wall
  for (let i = 0; i < 26; i++) nailHole(x, R() * w, R() * h, R);
  for (let i = 0; i < 9; i++) { const cx = R() * w, cy = R() * h; knot(x, cx, cy, 2.5 + R() * 3, R, 'v'); }
  for (let i = 0; i < 14; i++) { const cx = R() * w, cy = R() * h, a = R() * P, l = 8 + R() * 22; gouge(x, cx, cy, cx + Math.cos(a) * l, cy + Math.sin(a) * l, 1 + R()); }
  for (let i = 0; i < 6; i++) tally(x, 10 + R() * (w - 60), 20 + R() * (h - 40), 4 + Math.floor(R() * 12), 8 + R() * 6, R);
  for (let i = 0; i < 5; i++) scorch(x, R() * w, R() * h, 6 + R() * 10, R);
  for (let i = 0; i < 6; i++) soak(x, R() * w, R() * h, 8 + R() * 12, R, 'v');
  // the hall's soot: darker toward the floor and the corners
  const g = x.createLinearGradient(0, 0, 0, h); g.addColorStop(0, 'rgba(8,4,2,0.35)'); g.addColorStop(0.2, 'rgba(8,4,2,0)'); g.addColorStop(0.8, 'rgba(8,4,2,0)'); g.addColorStop(1, 'rgba(8,4,2,0.5)'); x.fillStyle = g; x.fillRect(0, 0, w, h);
  const s = x.createLinearGradient(w - 30, 0, w, 0); s.addColorStop(0, 'rgba(0,0,0,0)'); s.addColorStop(1, 'rgba(0,0,0,0.55)'); x.fillStyle = s; x.fillRect(w - 30, 0, 30, h);
}

// ---- the live readouts (the skins page and the battle console) -------------------------------------------------------
const live = {
  // A chevron stamped into the brass head of each chamber.
  arrow: (x) => { x.fillStyle = 'rgba(30,18,6,0.9)'; x.beginPath(); x.moveTo(13, 0); x.lineTo(-3, -12); x.lineTo(-3, -5); x.lineTo(-12, -5); x.lineTo(-12, 5); x.lineTo(-3, 5); x.lineTo(-3, 12); x.closePath(); x.fill(); x.strokeStyle = 'rgba(255,230,160,0.45)'; x.lineWidth = 0.8; x.save(); x.translate(0.7, 0.9); x.stroke(); x.restore(); },
  helmCentre: (x, c) => { x.fillStyle = '#1a1006'; x.font = '400 19px "Pirata One"'; x.textAlign = 'center'; x.fillText('HOLD', c, c + 7); },
  // The ship drawn in ink on the sheet; shields as heavy strokes in brass, rust and fresh blood by strength.
  ssd: (x, cx, cy, sh, col) => {
    for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.strokeStyle = col(sh[i]); x.lineWidth = 7; x.lineCap = 'butt'; x.beginPath(); x.arc(cx, cy, 98, a - P / 6 + 0.1, a + P / 6 - 0.1); x.stroke(); x.strokeStyle = 'rgba(20,12,6,0.6)'; x.lineWidth = 0.8; x.stroke(); }
    const hull = [[cx, cy - 76], [cx + 10, cy - 46], [cx + 34, cy - 30], [cx + 20, cy - 18], [cx + 22, cy + 14], [cx + 46, cy + 34], [cx + 18, cy + 36], [cx + 12, cy + 62], [cx, cy + 50], [cx - 12, cy + 62], [cx - 18, cy + 36], [cx - 46, cy + 34], [cx - 22, cy + 14], [cx - 20, cy - 18], [cx - 34, cy - 30], [cx - 10, cy - 46]];
    trace(x, hull, true); x.fillStyle = 'rgba(40,26,14,0.12)'; x.fill(); x.strokeStyle = RT.ink; x.lineWidth = 2.2; x.lineJoin = 'miter'; x.stroke();
    x.lineWidth = 1; x.beginPath(); x.moveTo(cx, cy - 40); x.lineTo(cx, cy + 40); x.moveTo(cx - 16, cy - 4); x.lineTo(cx + 16, cy - 4); x.stroke();
    for (const [mx, my] of [[cx, cy - 58], [cx - 32, cy + 28], [cx + 32, cy + 28]]) { x.fillStyle = '#6b1810'; x.beginPath(); x.moveTo(mx, my - 7); x.lineTo(mx + 4, my + 4); x.lineTo(mx, my + 2); x.lineTo(mx - 4, my + 4); x.closePath(); x.fill(); }
  },
  // A bone needle on the dark face, contacts as brass pips.
  dial: (x, c, cts) => {
    x.strokeStyle = RT.bone; x.lineWidth = 2.4; x.lineCap = 'round'; x.beginPath(); x.moveTo(c - Math.cos(-0.9) * 8, c - Math.sin(-0.9) * 8); x.lineTo(c + Math.cos(-0.9) * 38, c + Math.sin(-0.9) * 38); x.stroke();
    x.fillStyle = '#3a3e42'; x.beginPath(); x.arc(c, c, 4.5, 0, 2 * P); x.fill();
    for (const [dx, dy] of cts) { x.fillStyle = RT.brassHi; x.beginPath(); x.moveTo(c + dx * 30, c + dy * 30 - 4); x.lineTo(c + dx * 30 + 3.5, c + dy * 30 + 3); x.lineTo(c + dx * 30 - 3.5, c + dy * 30 + 3); x.closePath(); x.fill(); }
  },
  // Toothed steel corners, a trap closing on the target.
  bracket: (x, bx, by, bw, bh) => {
    const R = RNG('bracket');
    for (const [px, py, sx, sy] of [[bx, by, 1, 1], [bx + bw, by, -1, 1], [bx, by + bh, 1, -1], [bx + bw, by + bh, -1, -1]]) {
      x.save(); x.translate(px, py); x.scale(sx, sy);
      steel(x, [[0, 0], [48, 0], [48, 8], [8, 8], [8, 48], [0, 48]], R, { hi: '#9aa2a9', mid: '#565d63', lo: '#202326' });
      saw(x, 12, 8, 46, 8, 4, 6, -1); saw(x, 8, 46, 8, 12, 4, 6, -1);
      x.restore();
    }
  },
};

// =====================================================================================================================
// The battle console (openspec/changes/redtithe-battle-console). The same fields as docs/mockups/lib/concord.js, so the
// console swaps one skin for another and changes no rule and no layout.
// =====================================================================================================================
// The console draws its readouts in light, inside its panels and over the battle, so its panels are the hall's boards
// tarred black where the tithe is chalked (#1f140b), not mockup 4's paper; the paper stays where dark ink is printed on it:
// the keys, the chosen tab, the name plate. Colour roles, the Concord's names: old paper for a reading, bone for prose,
// worn paper for a label, brass for whole and engaged (spent rounds counted as standing), burnt orange for damaged, fresh
// blood only for down and alarms.
const CC = {
  deep: '#3d2a1b', signal: '#5a3a22', sky: '#efdcae', accord: '#e2ad48', lilac: '#c0a27a', caution: '#e07a32', alert: '#ff4b3e', text: '#f6ecd2',
  ink: '#1a120c', inkDis: '#4a3828', night: '#0d0805', ghost: '#76604a', void: '#0e0905', slate: '#2a1d13', mute: '#251a12', ver: '#a88f6c',
  char: '#3a0c08', plate: '#1a110a', bg: RT.bg, title: '#efdcae',
};
// Relative luminance of a #rrggbb colour (WCAG), for picking the ink that reads on a fill.
function lum(hex) {
  if (typeof hex !== 'string' || hex[0] !== '#' || hex.length < 7) return 0;
  const c = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((v) => (v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
}
// The ink that reads on a fill: typewriter black on paper and brass, bone on the dark boards.
const ink = (fill) => (lum(fill) > 0.2 ? CC.ink : CC.text);
// Ink on the trap-jaw keys (black on their paper slips) and the tally-stick tabs (pale on the bare stick, black on the
// chosen tab's slip).
const keyInk = { idle: '#16100b', hover: '#16100b', pressed: '#16100b', tabOn: '#16100b', tabOff: '#d9c49a' };
// Type (owner, 2026-11-08: "the font for red tithe is too "millitary" when they should be more "pirates""): "Pirata One",
// a cut blackletter, for titles, keys and the larger readouts; "IM Fell English SC", an old ship's-log print, for small
// labels (the skins page's pair). The page asks for Antonio sizes: Pirata One is compact, set at 0.95 times the size; Fell
// runs about 1.4 times as wide, set at 0.79 times; never under 10 px.
function font(spec) {
  const m = /^(\d+) ([\d.]+)px Antonio$/.exec(spec); if (!m) return spec;
  const s = +m[2];
  return s >= 17 ? `400 ${Math.round(s * 0.95 * 10) / 10}px "Pirata One"` : `400 ${Math.max(10, Math.round(s * 0.79 * 10) / 10)}px "IM Fell English SC"`;
}
const fonts = ['400 20px "Pirata One"', '400 20px "Pirata One"', '400 20px "IM Fell English SC"'];
// The icons for the console: cut into the dark boards in its reading colour, a shadowed cut (icon's default ink is the
// sheet's black, for mockup 4's paper).
const consoleIcon = (name, col = CC.sky) => icon(name, col);

// ---- the console's own pieces (mockup 4 keeps its own) ---------------------------------------------------------------
// A dark, tarred board: the field the tithe is chalked on (the strips' long middles, stretched only along their grain).
const TAR = { tone: '#1f140b', hi: '#2a1b0f', lo: '#110a05' };
// The same tar laid flat for a field that stretches both ways (a panel's, an inset's): the colour, a soft light from the
// upper left and the tar's own sheen, nothing with a grain to smear when the 9-slice centre is stretched.
function tar(x, X, Y, W, H) {
  const g = x.createLinearGradient(X, Y, X + W * 0.3, Y + H); g.addColorStop(0, '#271a0e'); g.addColorStop(0.5, '#1f140b'); g.addColorStop(1, '#170e07');
  x.fillStyle = g; x.fillRect(X, Y, W, H);
  const s = x.createLinearGradient(X, Y, X, Y + Math.min(14, H * 0.3)); s.addColorStop(0, 'rgba(0,0,0,0.6)'); s.addColorStop(1, 'rgba(0,0,0,0)'); x.fillStyle = s; x.fillRect(X, Y, W, Math.min(14, H * 0.3));
  x.strokeStyle = 'rgba(0,0,0,0.75)'; x.lineWidth = 2; x.strokeRect(X, Y, W, H);
}
// A panel: four boards of the hall's oak bound at the corners with strap iron round a tarred field; a scrap of a paid
// sheet nailed in the top-left corner, a knife driven into the bottom board at the right; scars only in the margins.
function consolePanel(x, w, h, R) {
  const Rw = RNG('cpanel.wood');
  boardFrame(x, w, h, 19, Rw);
  tar(x, 19, 19, w - 38, h - 38);
  knot(x, 9, h - 24, 2.4, Rw, 'v'); tally(x, 6, 34, 4, 8, Rw); gouge(x, w - 14, 34, w - 7, 46, 1.2); scorch(x, w - 10, h - 30, 4, Rw); soak(x, 40, h - 9, 7, Rw, 'h');
  for (const [cx, cy, sx, sy] of [[0.5, 0.5, 1, 1], [w - 0.5, 0.5, -1, 1], [0.5, h - 0.5, 1, -1], [w - 0.5, h - 0.5, -1, -1]]) cornerStrap(x, cx, cy, sx, sy, 22, 7, Rw);
  const p = tear([[9, 9], [27, 8], [28, 25], [10, 27]], true, 1.4, RNG('cpanel.scrap'), 2.5, 0.08);
  paper(x, p, RNG('cpanel.paper')); x.save(); trace(x, p, true); x.clip(); burn(x, 25, 24, 3, R); x.restore(); nail(x, 16, 15, 2, R);
  knife(x, w - 3, h - 5, w - 50, h - 19, 1.0, R);
}
// A readout well cut into the board: dark oak sides round a tarred floor, a nail at each corner.
function consoleInset(x, w, h, R) {
  boardFrame(x, w, h, 7, RNG('cinset.wood'), { tone: '#3a2415', hi: '#4f3320', lo: '#1a0f07' });
  tar(x, 7, 7, w - 14, h - 14);
  for (const [px, py] of [[4, 4], [w - 4, 4], [4, h - 4], [w - 4, h - 4]]) nail(x, px, py, 1.7, R);
}
// A strip: a beam of the hall's oak banded with strap iron at its ends, its long middle tarred for the readouts. The
// ends (92 px each: the console lays its readouts from 96 px in) carry the trophies: a scrap of a bounty sheet knifed to
// the beam and a string of teeth on the top strip, three spent rounds and a knife on the bottom.
function consoleStrip(bottom) {
  return (x, w, h, R) => {
    const Rb = RNG('cstrip.wood' + bottom);
    board(x, 0.5, 0.5, w - 1, h - 1, Rb, 'h');
    wood(x, rect(90, 7, w - 180, h - 14), RNG('cstrip.tar' + bottom), Object.assign({ dir: 'h' }, TAR));
    x.strokeStyle = 'rgba(0,0,0,0.75)'; x.lineWidth = 1.6; x.strokeRect(90, 7, w - 180, h - 14);
    for (const ex of [0.5, w - 11.5]) iron(x, rect(ex, 0.5, 11, h - 1), Rb, [[ex + 5.5, 9, 2.4], [ex + 5.5, h - 9, 2.4]]);
    if (bottom) {
      knot(x, 70, 14, 2.4, Rb); tally(x, 18, 10, 7, 9, Rb); soak(x, w - 50, h - 12, 6, Rb);
      for (let i = 0; i < 3; i++) cartridge(x, 40 + i * 13, h - 8, 8, 30, i === 2 ? 'spent' : 'live', R);
      knife(x, w - 14, 10, w - 82, h - 18, 1.2, R);
    } else {
      const p = tear([[16, 10], [74, 9], [76, h - 10], [18, h - 9]], true, 1.6, RNG('cstrip.scrap'), 3, 0.06);
      paper(x, p, RNG('cstrip.paper')); x.save(); trace(x, p, true); x.clip(); burn(x, 64, h - 18, 4, R); spatter(x, 28, 22, 0.4, 7, 2.6, R, false); x.restore();
      knife(x, 4, h - 6, 52, 22, 1.2, R);
      teethString(x, w - 84, 12, w - 18, 14, 9, 4, 1.2, R); nail(x, w - 84, 12, 2.2, R); nail(x, w - 18, 14, 2.2, R);
      scorch(x, w - 50, h - 14, 5, Rb);
    }
  };
}
// A title placard: a slat of charred oak nailed on at its head, the title (and an officer slot's name) chalked on it.
function titleSlat(x, w, h, R) {
  board(x, 0.5, 0.5, w - 1, h - 1, RNG('slat.wood'), 'h', { tone: '#24170c', hi: '#3a2615', lo: '#100904' });
  nail(x, 8, h / 2, 2.4, R);
}
// A slot key (the sheet-keepers' pigeonhole): a block of oak bound with strap iron, a slot cut in it. Empty (idle, off)
// the slot is dark and its mark is cut pale into the wood behind; engaged, a slip of the ledger is pushed into the slot,
// tinted by the state, its mark in black: hover old paper, on fresh paper, whole a brass-washed slip, damaged a
// rust-stained one, down one soaked in blood. Off, the slot is barred with iron.
const SLIP = { idle: null, hover: '#d8c597', on: '#f0e2bc', ok: '#e2ad48', warn: '#e08a4a', alert: '#ee5a48', off: null };
function slotKey(state, wide) {
  return (x, w, h) => {
    const R = RNG('key.' + (wide ? 'w' : 's')), ix = wide ? 12 : 9, iy = 9;
    board(x, 0.5, 0.5, w - 1, h - 1, R, wide ? 'h' : 'v');
    for (const [cx, cy, sx, sy] of [[0.5, 0.5, 1, 1], [w - 0.5, 0.5, -1, 1], [0.5, h - 0.5, 1, -1], [w - 0.5, h - 0.5, -1, -1]]) cornerStrap(x, cx, cy, sx, sy, 10, 4, R);
    const slot = rect(ix, iy, w - 2 * ix, h - 2 * iy);
    trace(x, slot, true); x.fillStyle = '#0d0805'; x.fill();
    x.save(); trace(x, slot, true); x.clip(); const g = x.createLinearGradient(0, iy, 0, iy + 8); g.addColorStop(0, 'rgba(0,0,0,0.85)'); g.addColorStop(1, 'rgba(0,0,0,0)'); x.fillStyle = g; x.fillRect(ix, iy, w - 2 * ix, 8); x.restore();
    trace(x, slot, true); x.strokeStyle = 'rgba(214,168,118,0.3)'; x.lineWidth = 1; x.stroke();
    const col = SLIP[state];
    if (col) {
      const p = tear(rect(ix + 1.5, iy + 1.5, w - 2 * ix - 3, h - 2 * iy - 3), true, 0.7, RNG('key.slip'), 3, 0.02);
      paper(x, p, RNG('key.paper'), { tone: col });
      if (state === 'alert') { x.save(); trace(x, p, true); x.clip(); drop(x, w - ix - 4, iy + 4, 2.2, R, true); x.restore(); }
    }
    if (state === 'off') for (const t of [0.35, 0.65]) iron(x, rect(ix - 2, iy + (h - 2 * iy) * t - 2, w - 2 * ix + 4, 4), R);
  };
}
// An allocation cell, the discrete boxes of CLAUDE.md section 7: a notch in a tally board, empty and dark at rest, a slip
// of the role's colour pushed into it when lit, barred with iron when locked.
function allocCell(col, locked) {
  return (x, w, h) => {
    const R = RNG('cell.alloc');
    board(x, 0.5, 0.5, w - 1, h - 1, R, 'h', { tone: '#3a2415', hi: '#4f3320', lo: '#1a0f07' });
    const s = rect(5, 5, w - 10, h - 10); trace(x, s, true); x.fillStyle = '#0d0805'; x.fill();
    if (col) { trace(x, s, true); x.fillStyle = col; x.fill(); x.fillStyle = 'rgba(255,248,225,0.25)'; x.fillRect(5, 5, w - 10, 1.5); x.fillStyle = 'rgba(70,40,15,0.25)'; x.fillRect(5, h - 7, w - 10, 2); }
    if (locked) iron(x, rect(3, h / 2 - 2, w - 6, 4), R);
  };
}
const cells = {
  'cell.idle': { size: [32, 28], m: [9, 9, 9, 9], draw: allocCell(null) },
  'cell.lit': { size: [32, 28], m: [9, 9, 9, 9], draw: allocCell('#efdcae') },
  'cell.warn': { size: [32, 28], m: [9, 9, 9, 9], draw: allocCell('#ee4a3c') },
  'cell.accord': { size: [32, 28], m: [9, 9, 9, 9], draw: allocCell('#e2ad48') },
  'cell.locked': { size: [32, 28], m: [9, 9, 9, 9], draw: allocCell(null, true) },
  'bar.fill.accord': { size: [64, 10], m: [0, 8, 0, 6], draw: (x, w, h) => { const g = x.createLinearGradient(0, 0, 0, h); g.addColorStop(0, '#f6dc8a'); g.addColorStop(1, RT.brassLo); x.fillStyle = g; x.fillRect(0, 1, w, h - 2); } },
};
const consoleKit = Object.assign({}, kit, {
  'frame.panel': piece('cpanel', [220, 170], [30, 56, 30, 30], consolePanel),
  'frame.inset': piece('cinset', [80, 60], [12, 12, 12, 12], consoleInset),
  'strip.top': piece('cstrip.top', [560, 72], [12, 92, 12, 92], consoleStrip(false)),
  'strip.bottom': piece('cstrip.bottom', [560, 72], [12, 92, 12, 92], consoleStrip(true)),
  'plate.title': piece('slat', [160, 24], [0, 8, 0, 18], titleSlat),
});
for (const s of Object.keys(SLIP)) consoleKit['keyw.' + s] = { size: [144, 48], m: [0, 22, 0, 22], draw: slotKey(s, true) };
const consoleHero = Object.assign({}, hero, {
  // The name plate: the bounty sheet stops short of the Fleet key (x 370-456 by 6-36), a knife pinning its lower corner,
  // the plank's bare end under the key.
  'nameplate': (x, w, h) => {
    const R = RNG('nameplate'), Rw = RNG('plate.wood');
    board(x, 0.5, 0.5, w - 1, h - 1, Rw, 'h');
    tally(x, 378, h - 15, 9, 9, Rw); knot(x, w - 22, h - 10, 2.4, Rw); nailHole(x, 8, 7, Rw);
    iron(x, rect(0.5, 0.5, 10, h - 1), Rw, [[5.5, 8, 2.2], [5.5, h - 8, 2.2]]);
    const p = tear([[13, 5], [352, 4], [362, 6], [361, h - 5], [220, h - 4], [13, h - 6]], true, 2, RNG('plate.edge'), 3, 0.05);
    paper(x, p, RNG('plate.paper'), { stains: [[336, h - 16, 12]] });
    x.save(); trace(x, p, true); x.clip(); burn(x, 348, h - 11, 4, R); grime(x, 34, h - 6, 24, 0.4); x.restore();
    nail(x, 356, 9, 2.2, R);
    seal(x, 32, h / 2, 17, R);
    knife(x, 424, h - 3, 350, h - 13, 1.0, R);
  },
  'console.backing': (x, w, h) => ground(x, w, h),
});
for (const s of Object.keys(SLIP)) consoleHero['key.' + s] = slotKey(s, false);
const heroSize = { 'console.backing': [480, 1080] };
for (const s of Object.keys(SLIP)) heroSize['key.' + s] = [48, 48];
// The bars as the bandolier: the skins page's spent, live and red rounds, and a whole one, nickel.
const barCells = { 'bar.cell.off': kit['bar.cell.off'], 'bar.cell.on': kit['bar.cell.on'], 'bar.cell.warn': kit['bar.cell.warn'],
  'bar.cell.ok': { size: [10, 16], gap: 1.5, draw: loop('ok') } };

const api = {
  id: 'redtithe', name: 'Red Tithe', RT, CC, kit: consoleKit, cells, barCells, live, icon: consoleIcon, iconSheet: icon, ICON_NAMES, ink, keyInk, font, fonts, lum, heroSize,
  // The skins page's kit (mockup 4): its nine-slice pieces, hero pieces and column as they were.
  nine: kit, heroSheet: hero, hero: consoleHero, ground, GROUND,
  load: () => Promise.resolve(), family: '"IM Fell English SC", monospace',
  // The content inside a panel: clear of the boards (19 px), the corner straps and the knife in the bottom-right margin.
  pad: [30, 28, 28, 28],
  // The name plate's live field: on the bounty sheet, right of the seal (64-354), typed in black.
  plate: { x: 62, y: 35, max: 290, font: '400 17px "IM Fell English SC"', col: '#1a120c' },
  // The battle's helm ring under the ship, in old paper.
  ring: ['rgba(239,220,174,0)', 'rgba(239,220,174,0.14)', 'rgba(250,236,200,0.9)'],
  // Names for the player's side where the page shows it as the Red Tithe: ships named for debts and their keeping,
  // captains as the sheets name them, ranks by the share of the tithe a hand may take. Ours and provisional until the
  // owner sets the Tithe's culture (CLAUDE.md section 1; openspec/changes/redtithe-battle-console, question R1).
  player: { faction: 'Red Tithe', call: 'Tithe-taker', trim: 0xc4141c, hull: 0x4a301c,
    ships: { resolute: ['Red Ledger', 'Capt. Hesk Varro'], tallis: ['Bone Tally', 'Capt. Morra Dask'], meridian: ['Last Due', 'Capt. Grell Anso'] },
    ranks: ['Deckhand', 'Cutter', 'Bloodhand', 'Captain'],
    officers: { o1: 'Tamsin Gall', o2: 'Rusk Odder', o3: 'Wen Harrow', o4: 'Kesk Mabry', o5: 'Lida Crane', o6: 'Ferro Dunn', o7: 'Haskel Bray', o8: 'Nell Scarrow',
      o9: 'Coby Ashe', o10: 'Ulla Venn', o11: 'Pike Morrow', o12: 'Gant Sully', o13: 'Edda Rook' } },
  link: '#e2ad48',
  // Labels centred on the trap-jaw keys, on the paper slip between the stakes.
  keyCenter: true, keyField: [24, 24],
  // A hover name on a trap-jaw key eased open, in black on its slip.
  tip: ['button.hover', 'hover'],
  // Icon tabs (the station and ship rails) on the slot key, the icon in its slot.
  iconKeys: true,
  // Panel tab icons at rest in worn paper, apart from the functional icons' old paper; glyphs in an empty slot in old paper.
  tabIcon: '#c0a27a', glyphOnDark: '#efdcae', mark: { hover: '#f6ecd2', ready: '#e2ad48', count: '#f6ecd2' },
  // The strips at their full 72 px, the content, laid out for 56 px, moved down onto the tarred middle (7-65).
  stripTop: { h: 72, dy: 10 }, stripBottom: { h: 72, dy: 0 },
};
window.FCSkins = window.FCSkins || {}; window.FCSkins.redtithe = api;
return api;
})();
