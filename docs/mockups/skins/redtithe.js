// The Red Tithe skin (mockup 4, OpenSpec change console-skins): a second, more brutal pirate faction (owner,
// 2026-11-07: "Id also like another pirate faction. One more brutal. Decorate their UI with sharp teeth, bullets and
// blades. Blood stained buttons and crusty paper with burn marks and drops of blood and grime on them"). Name and culture
// are provisional, ours, and go to the survey: raiders who take a tithe of blood from every crew they board, whose orders
// are written on bounty sheets and pinned up with knives, and whose captains count their standing in trophies (teeth
// pulled from beaten crews of every species, spent cartridges, broken blades). Three traditions inside it, each named
// where it shapes a piece: the sheet-keepers (paper, ledgers, burned seals), the gunners (bandoliers, brass, the revolver
// cylinder of the helm) and the cutters (blades, the trap-jaw keys). Nothing here is drawn from how the Tithe look
// (CLAUDE.md section 1): the teeth are other crews' trophies, never their own.
//
// This first pass is painted in canvas: crusted, stained and scorched paper pinned to dark gun steel, the trophies shaded
// by hand. A later pass models the metal (jaws, knives, cartridges, teeth) in Blender and paints the paper in Krita
// (krita-brushwork); until then this module is the one implementation. Every speck, stain, tear, drop and scratch comes
// from the page's seeded generator (FCK.rng(FCK.hash('redtithe.<piece>'))), never a sine or product-modulo hash
// (CLAUDE.md section 10). Blood, grime and burns sit only in a piece's margins, never under a label or readout.
// Registers itself with the page's skin registry (docs/mockups/skins). Our own design: no other game's names, shapes or art.
(function () {
'use strict';
const F = window.FCK, P = F.P;
const RT = {
  steel: '#2e3236', steelHi: '#7c848b', steelLo: '#141618', paper: '#c9b48a', paperHi: '#ddcba1', paperLo: '#9c8256',
  dried: '#5a1a12', driedLo: '#33100a', fresh: '#c4141c', freshLo: '#6e0a0e', brass: '#c8963a', brassHi: '#f0cf7a', brassLo: '#6e4d16',
  soot: '#1b1714', ink: '#1e1611', bone: '#e6d8b4', boneLo: '#9d8c6c', leather: '#3d2618', leatherHi: '#6b4429', bg: '#202326',
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
  const hg = x.createLinearGradient(0, -hw, 0, hw); hg.addColorStop(0, RT.leatherHi); hg.addColorStop(0.5, RT.leather); hg.addColorStop(1, '#1a0f08'); x.fillStyle = hg; x.fillRect(3 * s, -hw, gx - 5 * s, 2 * hw);
  x.restore();
  x.strokeStyle = 'rgba(10,6,4,0.8)'; x.lineWidth = 0.6; for (let t = 4 * s; t < gx - 3 * s; t += 2.4 * s) { x.beginPath(); x.moveTo(t, -hw); x.lineTo(t + 1.4 * s, hw); x.stroke(); }
  const pg = x.createRadialGradient(1.5 * s, -1 * s, 0.3, 2 * s, 0, 3.4 * s); pg.addColorStop(0, '#c9d0d6'); pg.addColorStop(1, '#1d2023'); x.fillStyle = pg; x.beginPath(); x.arc(2 * s, 0, 3.2 * s, 0, 2 * P); x.fill();
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
// (the warning round), 'spent' an empty dented case.
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
  if (state === 'spent') { g.addColorStop(0, '#7a6440'); g.addColorStop(0.4, '#4e3d22'); g.addColorStop(1, '#1d160c'); } else { g.addColorStop(0, RT.brassHi); g.addColorStop(0.35, RT.brass); g.addColorStop(1, RT.brassLo); }
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
    // hinge eyes: steel spring coils with a rivet, the jaws' pivot
    for (const ex of [8, w - 8]) {
      steel(x, F.rrPts(ex - 7, 2, 14, h - 4, 5), Rj);
      x.strokeStyle = 'rgba(0,0,0,0.65)'; x.lineWidth = 1; for (let y = 8; y < h - 6; y += 4) { x.beginPath(); x.moveTo(ex - 7, y); x.quadraticCurveTo(ex, y + 2.2, ex + 7, y); x.stroke(); }
      x.strokeStyle = 'rgba(220,226,232,0.25)'; x.lineWidth = 0.6; for (let y = 9; y < h - 6; y += 4) { x.beginPath(); x.moveTo(ex - 6, y); x.quadraticCurveTo(ex, y + 2, ex + 6, y); x.stroke(); }
      rivet(x, ex, h / 2, 3);
    }
    if (state === 'hover') { x.fillStyle = 'rgba(255,220,150,0.08)'; x.fillRect(14, top, w - 28, bot - top); }
  };
}
// A tab: a slip of the ledger nailed at both ends. Idle: soot-darkened, read in pale ink. Active: clean paper with the
// dried thumbprint of the captain who chose it, read in black.
function ledgerTab(active) {
  return (x, w, h, R) => {
    const p = tear([[4, 3], [w - 4, 4], [w - 3, h - 5], [w * 0.5, h - 3], [4, h - 4]], true, 1.6, RNG('tab.slip'), 3, 0.05);
    steel(x, F.rrPts(1, 1, w - 2, h - 2, 3), RNG('tab.steel'), { hi: '#30353a', mid: '#23272a', lo: '#121416' });
    paper(x, p, RNG('tab.paper'), { tone: active ? '#d4c092' : '#5c4c37', stains: active ? [] : [[w - 14, h / 2, 9]] });
    x.save(); trace(x, p, true); x.clip();
    if (active) { print(x, 13, h / 2 + 2, 6, -0.4, R, '90,26,16', 0.55); grime(x, w - 12, h - 8, 10, 0.3); }
    else { grime(x, 10, h / 2, 12, 0.5); grime(x, w - 10, h / 2, 12, 0.5); }
    x.restore();
    nail(x, 9, h / 2, 2.6, R); nail(x, w - 9, h / 2, 2.6, R);
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
    else cartridge(x, w / 2, h - 1, w * 0.62, h - 1, state === 'warn' ? 'red' : 'live', R);
    // the leather loop over the round's waist
    const ly = h * 0.52, lh = h * 0.24, g = x.createLinearGradient(0, ly, 0, ly + lh); g.addColorStop(0, '#6b4429'); g.addColorStop(1, '#2a180c');
    x.fillStyle = g; x.fillRect(0, ly, w, lh); x.strokeStyle = 'rgba(0,0,0,0.6)'; x.lineWidth = 0.4; x.strokeRect(0.2, ly, w - 0.4, lh);
    x.fillStyle = 'rgba(210,180,130,0.45)'; for (const sx of [1.2, w - 1.8]) x.fillRect(sx, ly + 1, 0.6, lh - 2);
  };
}
// A strip: a long bounty sheet nailed across a riveted steel beam; a knife and a burn at one end, trophies at the other.
function stripPiece(bottom) {
  return (x, w, h, R) => {
    steel(x, F.rrPts(0.5, 0.5, w - 1, h - 1, 3), RNG('strip.steel' + bottom));
    for (const px of [8, w - 8]) for (const py of [9, h - 9]) rivet(x, px, py, 3);
    const p = tear([[16, 7], [w - 16, 6], [w - 16, h - 7], [16, h - 6]], true, 2.2, RNG('strip.edge' + bottom), 3, 0.05);
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
    steel(x, F.rrPts(0.5, 0.5, w - 1, h - 1, 4), RNG('panel.steel'));
    for (const [px, py] of [[7, 7], [w - 7, 7], [7, h - 7], [w - 7, h - 7]]) rivet(x, px, py, 3.2);
    const p = tear([[12, 12], [w - 12, 13], [w - 13, h - 12], [w - 46, h - 11], [26, h - 12], [12, h - 26]], true, 2.2, RNG('panel.edge'), 3, 0.05);
    paper(x, p, RNG('panel.paper'), { stains: [[w - 34, h - 30, 18], [28, 46, 12]] });
    x.save(); trace(x, p, true); x.clip();
    burn(x, 22, h - 22, 5.5, R); grime(x, w - 26, h - 20, 22, 0.4); smear(x, [[w - 58, h - 17], [w - 38, h - 21], [w - 18, h - 26]], 6, R);
    spatter(x, w - 24, 52, 1.9, 10, 3, R, true); print(x, 22, 64, 7, 0.3, R); drip(x, w - 30, 30, 14, R, false); drip(x, 20, 30, 10, R, true);
    burn(x, w - 52, h - 16, 3.5, R);
    x.restore();
    knife(x, 1, 1, 33, 31, 1.3, R);
    teethString(x, w - 60, 9, w - 10, 11, 10, 4, 1.25, R); nail(x, w - 60, 9, 2.4, R); nail(x, w - 10, 11, 2.4, R);
  }),
  // A readout well: a scrap of the sheet nailed into a steel recess.
  'frame.inset': piece('frame.inset', [80, 60], [15, 15, 15, 15], (x, w, h, R) => {
    steel(x, F.rrPts(0.5, 0.5, w - 1, h - 1, 3), RNG('inset.steel'), { hi: '#272b2f', mid: '#1d2023', lo: '#0f1113' });
    const p = tear([[5, 5], [w - 5, 5], [w - 5, h - 5], [5, h - 5]], true, 1.3, RNG('inset.edge'), 2.5, 0.04);
    paper(x, p, RNG('inset.paper'), { stains: [[w - 8, h - 8, 7]] });
    x.save(); trace(x, p, true); x.clip(); grime(x, 6, h - 6, 10, 0.45); drop(x, w - 8, 9, 1.6, R, false); x.restore();
    for (const [px, py] of [[7.5, 7.5], [w - 7.5, 7.5], [7.5, h - 7.5], [w - 7.5, h - 7.5]]) nail(x, px, py, 1.9, R);
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
  // A divider: a blade's back laid along the sheet, nailed at both ends.
  'divider': piece('divider', [128, 10], [0, 14, 0, 14], (x, w, h, R) => {
    const g = x.createLinearGradient(0, 2, 0, 8); g.addColorStop(0, '#d5dbe0'); g.addColorStop(0.5, '#6d757c'); g.addColorStop(1, '#22262a');
    x.save(); x.shadowColor = 'rgba(0,0,0,0.6)'; x.shadowBlur = 2; x.shadowOffsetY = 1; x.fillStyle = g; x.fillRect(8, 3, w - 16, 4); x.restore();
    x.strokeStyle = 'rgba(0,0,0,0.7)'; x.lineWidth = 0.5; x.strokeRect(8, 3, w - 16, 4);
    nail(x, 5, h / 2, 2.4, R); nail(x, w - 5, h / 2, 2.4, R);
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
function crestMark(x, w, R) {
  const c = w / 2;
  // a drop of blood, the tithe; two cutters' blades crossed behind; a tooth taken in trophy over all
  const d = []; for (let i = 0; i < 48; i++) { const t = i * 2 * P / 48; d.push([c + 36 * Math.sin(t) * Math.sin(t / 2), c + 6 - 40 * Math.cos(t)]); }
  x.save(); x.shadowColor = 'rgba(0,0,0,0.7)'; x.shadowBlur = 5; x.shadowOffsetY = 2; trace(x, d, true);
  const g = x.createRadialGradient(c - 10, c, 2, c, c + 10, 38); g.addColorStop(0, '#d0262c'); g.addColorStop(0.6, '#8a0c12'); g.addColorStop(1, '#3a0406'); x.fillStyle = g; x.fill(); x.restore();
  trace(x, d, true); x.strokeStyle = '#1a0304'; x.lineWidth = 1.4; x.stroke();
  x.fillStyle = 'rgba(255,220,220,0.5)'; x.beginPath(); x.ellipse(c - 14, c + 2, 4, 9, 0.3, 0, 2 * P); x.fill();
  knife(x, 10, 86, 74, 22, 1.25, R, { blood: false }); knife(x, 86, 86, 22, 22, 1.25, R, { blood: false });
  tooth(x, c, c - 14, 3.3, 'canine', 0, R);
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
  // A bounty sheet with a burned seal: the captain's name typed into its field (64-404 x 10-46).
  'nameplate': (x, w, h) => {
    const R = RNG('nameplate');
    steel(x, F.rrPts(0.5, 0.5, w - 1, h - 1, 4), RNG('plate.steel'));
    const p = tear([[6, 5], [w - 30, 4], [w - 8, 7], [w - 6, h - 5], [w * 0.6, h - 4], [6, h - 6]], true, 2, RNG('plate.edge'), 3, 0.05);
    paper(x, p, RNG('plate.paper'), { stains: [[w - 34, h - 16, 14]] });
    x.save(); trace(x, p, true); x.clip(); burn(x, w - 16, h - 12, 4.5, R); grime(x, 34, h - 6, 24, 0.4); spatter(x, w - 46, 12, 0.2, 7, 2.2, R, false);
    x.strokeStyle = 'rgba(60,40,20,0.25)'; x.lineWidth = 0.6; x.beginPath(); x.moveTo(62, h - 9); x.lineTo(404, h - 9); x.stroke(); x.restore();
    seal(x, 32, h / 2, 17, R);
    knife(x, w - 1, 1, w - 40, h / 2 + 6, 1.15, R);
  },
  // The helm: a revolver's cylinder (the gunners' tradition). Eight chambers, each a round's brass head, ring the
  // ejector, the stop key; the arrows are stamped live on the brass.
  'helm.pad': (x, w, h) => {
    const R = RNG('helm'), c = w / 2;
    steel(x, F.rrPts(2, 2, w - 4, h - 4, 10), RNG('helm.plate'), { hi: '#30353a', mid: '#23272a', lo: '#101214' });
    for (const [px, py] of [[16, 16], [w - 16, 16], [16, h - 16], [w - 16, h - 16]]) rivet(x, px, py, 4.5);
    // a scrap of the ledger pinned behind the cylinder, burned and bloodied at its corners
    const sp = tear([[34, 24], [w - 22, 34], [w - 30, h - 26], [24, h - 36]], true, 3, RNG('helm.scrap'), 4, 0.06);
    paper(x, sp, RNG('helm.paper'), { tone: '#b39d71', stains: [[w - 50, h - 50, 22]] });
    x.save(); trace(x, sp, true); x.clip(); burn(x, 46, h - 52, 7, R); spatter(x, w - 46, 52, 2.4, 12, 3.4, R, false); x.restore();
    // the cylinder
    const cyl = []; for (let i = 0; i < 64; i++) { const a = i * 2 * P / 64, f = Math.cos(8 * (a + P / 8)) > 0.92 ? 0.95 : 1; cyl.push([c + Math.cos(a) * 136 * f, c + Math.sin(a) * 136 * f]); }
    x.save(); x.shadowColor = 'rgba(0,0,0,0.8)'; x.shadowBlur = 10; x.shadowOffsetX = 3; x.shadowOffsetY = 5; trace(x, cyl, true); x.fillStyle = '#2a2e32'; x.fill(); x.restore();
    steel(x, cyl, RNG('helm.cyl'), { hi: '#5a6168', mid: '#30353a', lo: '#121416' });
    // flutes between the chambers
    for (let i = 0; i < 8; i++) { const a = -P / 2 + (i + 0.5) * P / 4; x.save(); x.translate(c + Math.cos(a) * 118, c + Math.sin(a) * 118); x.rotate(a); const g = x.createLinearGradient(0, -6, 0, 6); g.addColorStop(0, '#0d0f11'); g.addColorStop(1, '#5d646b'); x.fillStyle = g; x.beginPath(); x.ellipse(0, 0, 16, 5, 0, 0, 2 * P); x.fill(); x.restore(); }
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
  // The systems display: a bounty sheet pinned at each corner with a blade, its field clear for the ship drawn live.
  'ssd.bezel': (x, w, h) => {
    const R = RNG('ssd');
    steel(x, F.rrPts(0.5, 0.5, w - 1, h - 1, 5), RNG('ssd.steel'));
    for (const [px, py] of [[8, 8], [w - 8, 8], [8, h - 8], [w - 8, h - 8], [w / 2, 6], [w / 2, h - 6]]) rivet(x, px, py, 3);
    const p = tear([[14, 14], [w * 0.45, 12], [w - 14, 15], [w - 13, h * 0.5], [w - 15, h - 14], [w * 0.55, h - 13], [14, h - 15], [12, h * 0.4]], true, 2.6, RNG('ssd.edge'), 3, 0.05);
    paper(x, p, RNG('ssd.paper'), { stains: [[70, h - 60, 30], [w - 70, 60, 24]] });
    x.save(); trace(x, p, true); x.clip();
    // margins only: the ship is drawn within 110 px of (230, 150)
    burn(x, w - 50, h - 70, 11, R); burn(x, 50, 78, 5, R); burn(x, w - 92, 40, 3, R); grime(x, 40, h - 50, 34, 0.45); grime(x, w - 30, 40, 30, 0.4);
    spatter(x, 66, h - 86, -0.5, 16, 4.4, R, false); smear(x, [[w - 26, 70], [w - 30, 110], [w - 24, 150]], 7, R); print(x, w - 64, 106, 9, 0.6, R); print(x, 66, 150, 9, -0.3, R, '80,22,14', 0.45);
    drop(x, w - 116, 46, 3.2, R, true, P / 2); drip(x, w - 104, 38, 22, R, true); drip(x, 104, h - 70, 16, R, false);
    // the sheet's ruled frame and its typed header
    x.strokeStyle = 'rgba(40,24,12,0.45)'; x.lineWidth = 0.8; x.strokeRect(24, 22, w - 48, h - 44);
    x.fillStyle = 'rgba(30,20,12,0.75)'; x.font = '400 11px "Special Elite"'; x.fillText('TITHE OWED · SHEET 114', 56, 40); x.fillText('PAID IN FULL: NOT YET', 56, h - 26);
    x.restore();
    knife(x, 1, 1, 40, 34, 1.4, R); knife(x, w - 1, 1, w - 40, 34, 1.4, R); knife(x, 1, h - 1, 40, h - 34, 1.4, R); knife(x, w - 1, h - 1, w - 40, h - 34, 1.4, R);
  },
  // A gauge behind cracked, bloodied glass in a riveted steel ring.
  'dial': (x, w, h) => {
    const R = RNG('dial'), c = w / 2;
    steel(x, F.arcPts(c, c, 58, 58, 0, 2 * P, 48), RNG('dial.ring'), { hi: '#6b737a', mid: '#353a3f', lo: '#121416' });
    for (let i = 0; i < 8; i++) { const a = i * P / 4 + P / 8; rivet(x, c + Math.cos(a) * 52, c + Math.sin(a) * 52, 2.2); }
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
function icon(name) {
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
    })[name]();
    const draw = (lwAdd, col, dx = 0, dy = 0) => {
      x.save(); x.translate(dx, dy);
      for (const [k, fn, lw] of ops) { fn(x); if (k === 's') { x.strokeStyle = col; x.lineWidth = lw + lwAdd; x.lineCap = 'butt'; x.lineJoin = 'miter'; x.stroke(); } else { x.fillStyle = col; x.fill(); if (lwAdd) { x.strokeStyle = col; x.lineWidth = lwAdd; x.lineJoin = 'miter'; x.stroke(); } } }
      x.restore();
    };
    draw(3.2, 'rgba(230,214,176,0.92)', 0.6, 0.8);   // the scored rim the blade lifted
    draw(0, RT.ink);                                  // the cut
    // stencil bridges and blade scratches, taken out of the cut
    x.globalCompositeOperation = 'source-atop';
    x.strokeStyle = 'rgba(120,96,70,0.9)'; x.lineWidth = 0.6;
    for (let i = 0; i < 3; i++) { const sx = 6 + R() * 36, sy = 6 + R() * 36, a = -0.9 + (R() - 0.5) * 0.4, L = 8 + R() * 12; x.beginPath(); x.moveTo(sx, sy); x.lineTo(sx + Math.cos(a) * L, sy + Math.sin(a) * L); x.stroke(); }
    x.globalCompositeOperation = 'source-over';
  });
}

// ---- the skin -----------------------------------------------------------------------------------------------------
F.register('redtithe', {
  name: 'Red Tithe', kind: 'Painted in canvas: crusted, bloodied bounty sheets pinned to gun steel by knives, trap-jaw keys, a bandolier for every bar', bg: RT.bg, sampleBg: RT.paper,
  css: { '--s-ink': RT.ink, '--s-dim': '#3b2a1e', '--s-num': '#1c130d', '--s-body': '"Special Elite", monospace', '--btn-ink': '#16100b', '--btn-justify': 'center',
    '--btn-shadow': '0 1px 0 rgba(255,240,205,0.35)', '--btn-font': '700 17px/1 "Stardos Stencil", sans-serif', '--btn-track': '0.05em', '--btn-case': 'uppercase', '--btn-motion': 'padding-top 0.05s', '--btn-sink': '1.5px',
    '--tab-font': '400 14px/1 "Special Elite", monospace', '--tab-ink': '#d8c69a', '--tab-ink-on': '#1a120c',
    '--title-font': '700 19px/1 "Stardos Stencil", sans-serif', '--title-ink': '#6b1810', '--lbl-font': '400 15px/1 "Special Elite", monospace', '--num-font': '400 25px/1 "Special Elite", monospace',
    // The strips are 72 px; their open band is the sheet inside its torn edge, rows 11-61, so each label stands over its
    // strap with room to spare; the bandolier's middle tiles, so its stitches keep their pitch at any width.
    '--stt-pt': '12px', '--stt-pb': '12px', '--stb-pt': '12px', '--stb-pb': '12px', '--strip-lamp': '44px', '--bt-rep': 'round' },
  accent: RT.brass, warn: RT.fresh,
  // The frame's top margin carries the knife and the teeth, so the title is stencilled at the head of the sheet itself.
  title: (t, m) => { t.style.cssText = 'position:relative;justify-self:start;align-self:flex-start;margin-left:2px;letter-spacing:0.12em;'; },
  // The captain's name is typed into the bounty sheet's field (64-404 x 10-46), clear of the seal and the knife.
  plateText: { left: 64, top: 12, width: 336, height: 30, font: '400 21px/30px "Special Elite", monospace', color: '#1a120c', sub: 'RED LEDGER · CAPT. HESK VARRO' },
  helmKeys: 'ring', helmR: 92, shield: [RT.brass, '#b8561c', RT.fresh],
  fonts: ['700 20px "Stardos Stencil"', '400 20px "Special Elite"'], view: { title: '400 28px "Special Elite"', body: '400 24px "Special Elite"', upper: true, ink: RT.bone, dim: RT.brass },
  build: () => F.fromPainters(kit, hero, icon, false),
  live: {
    // A chevron stamped into the brass head of each chamber.
    arrow: (x) => { x.fillStyle = 'rgba(30,18,6,0.9)'; x.beginPath(); x.moveTo(13, 0); x.lineTo(-3, -12); x.lineTo(-3, -5); x.lineTo(-12, -5); x.lineTo(-12, 5); x.lineTo(-3, 5); x.lineTo(-3, 12); x.closePath(); x.fill(); x.strokeStyle = 'rgba(255,230,160,0.45)'; x.lineWidth = 0.8; x.save(); x.translate(0.7, 0.9); x.stroke(); x.restore(); },
    helmCentre: (x, c) => { x.fillStyle = '#1a1006'; x.font = '700 19px "Stardos Stencil"'; x.textAlign = 'center'; x.fillText('HOLD', c, c + 7); },
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
  },
  world: ['Every crew boarded pays a tithe in blood; every debt is written on a sheet and pinned up with a knife until it is paid.',
    'Raiders of many peoples; a captain\'s standing is counted in trophies taken from others: teeth of beaten crews, spent cartridges, broken blades.',
    'Three traditions: the sheet-keepers\' ledgers and burned seals, the gunners\' bandoliers and brass, the cutters\' blades and traps.',
    'Scarred boarding hulls with ram prows and grapples, hung with the plates of ships they took.'],
  shapes: 'Trap-jaw keys, torn bounty sheets nailed to riveted steel, knives through corners, strings of teeth, a revolver cylinder, bandolier bars.',
  light: 'Painted: a hard light from the upper left, deep cast shadows under blades and paper, no glow but the alert lamps.',
  motion: 'A key\'s jaws snap shut and blood seeps along the bite; a bar loads round by round.',
  type: '"Stardos Stencil" stencilled titles and keys, "Special Elite" typed readouts.',
  palette: [['Dried blood', RT.dried, 'Stains, seals, titles', 'The tithe already taken'], ['Fresh blood', RT.fresh, 'Alerts only', 'The tithe being taken now'],
    ['Old paper', RT.paper, 'Sheets, keys, readout grounds', 'The ledger: every debt written down'], ['Soot', RT.soot, 'Ink, burns, grime', 'A sheet closed by fire'],
    ['Brass', RT.brass, 'Rounds, ready states', 'Spent cartridges counted as standing'], ['Gun steel', RT.steel, 'Plates, jaws, blades', 'What holds the sheet holds the crew']],
});
})();
