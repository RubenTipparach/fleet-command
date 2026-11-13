// Fleet Command mockups: the Concord skin's crew skill icons (change crew-leveling, mockup 39; owner, 2026-11-12: "use art
// drawing skills to make skill icons"). One picture for each ability a skill changes (data/campaign/crew.json fx keys),
// drawn to the Fleet Command Icons standard (docs/design/icon-guide.html): a picture of the job, one subject one icon
// everywhere, a 48 x 48 grid with about 4 px clear and a 3 px line, solid for the thing and lines for what it gives off,
// one ink (colour is state), readable at 20 px. The Concord's flat vector, as its station icons in lib/concord.js; another
// skin draws the same subject list its own way. One copy; inlined by tools/mockups/inline_data.py after lib/kitdraw.js.
window.FCSkillIcons = (function () {
'use strict';
const { paint, P } = window.FCDraw;
// The subjects, each with what it pictures (the guide's contract between skins).
const SUBJECTS = {
  turn: 'A ship swinging round a pivot on a curved arrow', accel: 'A ship with three speed lines behind it', speed: 'A gauge with its needle hard over',
  het: 'A ship at the end of a tight hairpin turn', het_safe: 'A hairpin turn inside a shield', evade: 'A ship leaving a zig-zag wake',
  hit: 'A gun sight on a dot', charge: 'A cell with a bolt in it, its charge rising', pd: 'A shield with a burst on it (the station icon)', pd_double: 'Two bursts on two missiles',
  drift: 'A coil settling to a level line', shield_rate: 'A shield ringed by a recharge arrow', repair_rate: 'A wrench with speed lines',
  repair_crews: 'Two crew figures', reactor_e: 'The radiation trefoil with an extra ring', scan: 'A lens over a ship',
  sweep_km: 'A radar sweep reaching out past its ring', anticloak: 'A dashed ship outline coming into view', scan_weak: 'A ship with one part bracketed',
  jam: 'A clean wave through a burst of noise', wing_hit: 'Three ships in formation, a sight ahead', standing: 'A star on a ribbon',
  aboard: 'A figure stepping through a hatch', ambush: 'A figure behind a corridor corner, a burst beyond', pad_rate: 'A pad casting a column of light',
};
// A small ship from above, nose up, its centre at (cx, cy), scale k.
function ship(x, cx, cy, k = 1, a = 0) { x.save(); x.translate(cx, cy); x.rotate(a); x.scale(k, k); x.beginPath(); x.moveTo(0, -8); x.lineTo(6, 7); x.lineTo(0, 4); x.lineTo(-6, 7); x.closePath(); x.fill(); x.restore(); }
function figure(x, cx, cy, k = 1) { x.beginPath(); x.arc(cx, cy - 9 * k, 4 * k, 0, 2 * P); x.fill(); x.beginPath(); x.moveTo(cx - 7 * k, cy + 9 * k); x.lineTo(cx - 6 * k, cy - 1 * k); x.quadraticCurveTo(cx, cy - 5 * k, cx + 6 * k, cy - 1 * k); x.lineTo(cx + 7 * k, cy + 9 * k); x.closePath(); x.fill(); }
function shieldPath(x, cx, cy, k = 1) { x.beginPath(); x.moveTo(cx, cy - 18 * k); x.lineTo(cx + 15 * k, cy - 12 * k); x.quadraticCurveTo(cx + 15 * k, cy + 9 * k, cx, cy + 19 * k); x.quadraticCurveTo(cx - 15 * k, cy + 9 * k, cx - 15 * k, cy - 12 * k); x.closePath(); }
function burst(x, cx, cy, r1, r2, n = 10) { x.beginPath(); for (let i = 0; i < n * 2; i++) { const a = i * P / n, r = i % 2 ? r1 : r2; x.lineTo(cx + Math.cos(a) * r, cy + Math.sin(a) * r); } x.closePath(); x.fill(); }
function arrowHead(x, tx, ty, a, s = 6) { x.beginPath(); x.moveTo(tx + Math.cos(a) * s, ty + Math.sin(a) * s); x.lineTo(tx + Math.cos(a + 2.4) * s, ty + Math.sin(a + 2.4) * s); x.lineTo(tx + Math.cos(a - 2.4) * s, ty + Math.sin(a - 2.4) * s); x.closePath(); x.fill(); }
// The painter: a 48 x 48 canvas (at 2x) in one ink, as the kit's icons are. col: the ink, Sky by default.
function icon(name, col = '#3fa9f0') {
  return paint(48, 48, (x) => {
    x.strokeStyle = col; x.fillStyle = col; x.lineWidth = 3; x.lineCap = 'round'; x.lineJoin = 'round';
    const C = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.stroke(); };
    const D = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); };
    const L = (pts) => { x.beginPath(); x.moveTo(pts[0], pts[1]); for (let i = 2; i < pts.length; i += 2) x.lineTo(pts[i], pts[i + 1]); x.stroke(); };
    const draw = {
      turn: () => { x.beginPath(); x.arc(24, 26, 15, P * 0.85, P * 2.15); x.stroke(); arrowHead(x, 38, 31, P * 0.62, 7); D(24, 26, 3); ship(x, 12, 18, 1, -0.9); },
      accel: () => { ship(x, 30, 22, 1.6, P / 2); x.lineWidth = 2.5; L([4, 15, 15, 15]); L([2, 22, 15, 22]); L([4, 29, 15, 29]); },
      speed: () => { x.beginPath(); x.arc(24, 30, 18, P, 2 * P); x.stroke(); for (let i = 0; i <= 4; i++) { const a = P + i * P / 4; L([24 + Math.cos(a) * 13, 30 + Math.sin(a) * 13, 24 + Math.cos(a) * 18, 30 + Math.sin(a) * 18]); }
        x.lineWidth = 3.5; L([24, 30, 38, 17]); D(24, 30, 4); x.fillRect(6, 36, 36, 4); },
      het: () => { x.beginPath(); x.moveTo(12, 44); x.lineTo(12, 18); x.arc(24, 18, 12, P, 2 * P); x.lineTo(36, 30); x.stroke(); ship(x, 36, 38, 1.1, P); x.lineWidth = 2; L([18, 8, 14, 3]); L([24, 5, 24, 0.5]); L([30, 8, 34, 3]); },
      het_safe: () => { shieldPath(x, 24, 24, 1.15); x.stroke(); x.lineWidth = 3; x.beginPath(); x.moveTo(18, 34); x.lineTo(18, 21); x.arc(24, 21, 6, P, 2 * P); x.lineTo(30, 28); x.stroke(); arrowHead(x, 30, 31, P / 2, 5); },
      evade: () => { ship(x, 35, 12, 1.4, 0.5); x.lineWidth = 2.5; L([31, 22, 22, 26, 30, 32, 18, 36, 26, 43, 10, 45]); },
      hit: () => { C(24, 24, 15); D(24, 24, 4.5); L([24, 3, 24, 13]); L([24, 35, 24, 45]); L([3, 24, 13, 24]); L([35, 24, 45, 24]); },
      charge: () => { x.lineWidth = 3; x.beginPath(); x.roundRect(10, 9, 28, 35, 4); x.stroke(); x.fillRect(19, 4, 10, 5); x.fillRect(15, 34, 18, 6);
        x.beginPath(); x.moveTo(27, 12); x.lineTo(17, 25); x.lineTo(23, 25); x.lineTo(20, 31); x.lineTo(31, 20); x.lineTo(25, 20); x.closePath(); x.fill(); },
      pd: () => { shieldPath(x, 24, 24, 1.05); x.stroke(); burst(x, 24, 22, 4, 9, 8); },
      pd_double: () => { x.lineWidth = 2.5; L([6, 44, 15, 30]); L([42, 44, 33, 30]); burst(x, 16, 24, 4, 10, 8); burst(x, 32, 16, 4, 10, 8); },
      drift: () => { x.lineWidth = 3; x.beginPath(); for (let t = 0; t <= 1.001; t += 0.02) { const xx = 4 + t * 40, amp = 12 * (1 - t) ** 1.5, yy = 24 + Math.sin(t * P * 6) * amp; t === 0 ? x.moveTo(xx, yy) : x.lineTo(xx, yy); } x.stroke(); D(44, 24, 3); },
      shield_rate: () => { shieldPath(x, 24, 25, 0.78); x.fill(); x.lineWidth = 2.5; x.beginPath(); x.arc(24, 25, 20, -P * 0.35, P * 1.35); x.stroke(); arrowHead(x, 24 + Math.cos(-P * 0.35) * 20, 25 + Math.sin(-P * 0.35) * 20, -P * 0.35 + P / 2, 6); },
      repair_rate: () => { x.lineWidth = 6; L([16, 40, 29, 27]); x.lineWidth = 5; x.beginPath(); x.arc(33, 22, 8, -P / 4 + 0.95, -P / 4 - 0.95 + 2 * P); x.stroke(); x.lineWidth = 2.5; L([3, 22, 11, 22]); L([5, 29, 13, 29]); L([3, 36, 9, 36]); },
      repair_crews: () => { figure(x, 16, 26, 1.15); figure(x, 33, 26, 1.15); x.fillRect(6, 40, 36, 4); },
      reactor_e: () => { D(24, 24, 3); for (let i = 0; i < 3; i++) { const a = -P / 2 + i * 2 * P / 3; x.beginPath(); x.arc(24, 24, 14, a - 0.5, a + 0.5); x.arc(24, 24, 6, a + 0.5, a - 0.5, true); x.closePath(); x.fill(); } x.lineWidth = 2.5; C(24, 24, 20); },
      scan: () => { x.lineWidth = 3; C(28, 19, 14); ship(x, 28, 19, 0.95); x.lineWidth = 6; L([18, 29, 7, 41]); },
      sweep_km: () => { C(20, 28, 14); x.beginPath(); x.moveTo(20, 28); x.arc(20, 28, 14, -P / 2, -P / 2 + 1.0); x.closePath(); x.fill(); x.lineWidth = 2; x.beginPath(); x.arc(20, 28, 21, -P * 0.45, -P * 0.05); x.stroke(); x.beginPath(); x.arc(20, 28, 27, -P * 0.4, -P * 0.1); x.stroke(); },
      anticloak: () => { x.lineWidth = 2.5; x.setLineDash([4, 4]); x.beginPath(); x.moveTo(24, 6); x.lineTo(38, 40); x.lineTo(24, 33); x.lineTo(10, 40); x.closePath(); x.stroke(); x.setLineDash([]); x.beginPath(); x.moveTo(24, 6); x.lineTo(31, 23); x.lineTo(17, 23); x.closePath(); x.fill(); },
      scan_weak: () => { ship(x, 24, 22, 2.1); x.lineWidth = 2.5; for (const [px, py, sx, sy] of [[31, 32, -1, -1], [43, 32, 1, -1], [31, 44, -1, 1], [43, 44, 1, 1]]) L([px, py - 4 * sy, px, py, px - 4 * sx, py]); D(37, 38, 2.5); },
      jam: () => { x.lineWidth = 2; x.beginPath(); for (let i = 0; i <= 20; i++) { const xx = 8 + i * 1.6, yy = 24 + (i % 2 ? -1 : 1) * (4 + (i * 7 % 5)); i ? x.lineTo(xx, yy) : x.moveTo(xx, yy); } x.stroke(); x.lineWidth = 3.5; L([3, 24, 41, 24]); arrowHead(x, 42, 24, 0, 6); },
      wing_hit: () => { ship(x, 24, 30, 1.2); ship(x, 12, 40, 0.9); ship(x, 36, 40, 0.9); x.lineWidth = 2.5; C(24, 10, 6); L([24, 1, 24, 4]); },
      standing: () => { x.beginPath(); x.moveTo(17, 28); x.lineTo(11, 45); x.lineTo(17, 41); x.lineTo(21, 46); x.lineTo(24, 30); x.lineTo(27, 46); x.lineTo(31, 41); x.lineTo(37, 45); x.lineTo(31, 28); x.closePath(); x.fill();
        x.beginPath(); for (let i = 0; i < 10; i++) { const a = -P / 2 + i * P / 5, r = i % 2 ? 6 : 14; x.lineTo(24 + Math.cos(a) * r, 18 + Math.sin(a) * r); } x.closePath(); x.fill(); },
      aboard: () => { x.lineWidth = 3; x.beginPath(); x.roundRect(22, 6, 20, 38, 6); x.stroke(); figure(x, 18, 28, 1.25); x.lineWidth = 2.5; L([3, 22, 9, 22]); L([3, 30, 8, 30]); },
      ambush: () => { x.fillRect(4, 6, 8, 38); x.fillRect(4, 36, 26, 8); figure(x, 21, 24, 1.05); burst(x, 38, 16, 3.5, 8, 7); },
      pad_rate: () => { x.beginPath(); x.ellipse(24, 40, 17, 5, 0, 0, 2 * P); x.fill(); x.lineWidth = 2.5; L([11, 34, 11, 8]); L([18, 34, 18, 4]); L([24, 34, 24, 2]); L([30, 34, 30, 4]); L([37, 34, 37, 8]); },
    };
    (draw[name] || draw.hit)();
  });
}
return { icon, SUBJECTS };
})();
