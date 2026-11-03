// Fleet Command mockups: the Concord skin's kit painters (openspec/changes/console-skins/design.md, "Concord"), shared by
// the console skins page and the battle console (docs/mockups/battle-console.html). One implementation of the Concord
// look (CLAUDE.md section 6.1). Needs docs/mockups/lib/kitdraw.js inlined first.
window.FCConcord = (function () {
'use strict';
const { paint, P, rr } = window.FCDraw;
// ===============================================================================================================
// CONCORD: flat back-lit vector on LCARS's principles. Palette sampled from Starfleet Command and Starfleet Academy.
// ===============================================================================================================
const CC = { deep: '#1b2a6b', signal: '#2f62e0', sky: '#3fa9f0', accord: '#2fbf5a', lilac: '#b7a6f2', caution: '#f0b43c', alert: '#e8352b', text: '#eef3ff' };
// The sweep: a thick bar on the left joined by outer-rounded elbows to thin arms along the top and bottom, which end in
// a small rounded cap of another colour. Thick to thin at every turn (the LCARS rule).
function ccSweep(x, X, Y, W, H, o) {
  const { bar, arm, R, r, cap, col, capCol } = o, end = X + W - cap - 5;
  x.fillStyle = col; x.beginPath();
  x.moveTo(X + R, Y); x.lineTo(end, Y); x.lineTo(end, Y + arm); x.lineTo(X + bar + r, Y + arm);
  x.arcTo(X + bar, Y + arm, X + bar, Y + arm + r, r); x.lineTo(X + bar, Y + H - arm - r); x.arcTo(X + bar, Y + H - arm, X + bar + r, Y + H - arm, r);
  x.lineTo(end, Y + H - arm); x.lineTo(end, Y + H); x.lineTo(X + R, Y + H); x.arcTo(X, Y + H, X, Y + H - R, R); x.lineTo(X, Y + R); x.arcTo(X, Y, X + R, Y, R);
  x.closePath(); x.fill();
  if (cap) { x.fillStyle = capCol; rr(x, end + 5, Y, cap, arm, arm / 2); x.fill(); rr(x, end + 5, Y + H - arm, cap, arm, arm / 2); x.fill(); }
}
function ccArcSeg(x, cx, cy, r0, r1, a0, a1) { x.beginPath(); x.arc(cx, cy, r1, a0, a1); x.arc(cx, cy, r0, a1, a0, true); x.closePath(); }
const concordKit = {
  'frame.panel': { size: [160, 120], m: [22, 28, 22, 44], draw: (x, w, h) => ccSweep(x, 0, 0, w, h, { bar: 28, arm: 10, R: 24, r: 10, cap: 16, col: CC.signal, capCol: CC.sky }) },
  'frame.inset': { size: [64, 48], m: [14, 14, 14, 14], draw: (x, w, h) => { x.strokeStyle = CC.deep; x.lineWidth = 3; rr(x, 1.5, 1.5, w - 3, h - 3, 12); x.stroke(); x.fillStyle = CC.sky; rr(x, 8, 0, 14, 3, 1.5); x.fill(); } },
  'bar.track': { size: [96, 16], m: [0, 10, 0, 10], inset: [3, 3], draw: (x, w, h) => { x.fillStyle = CC.deep; rr(x, 0, 0, w, h, 8); x.fill(); x.fillStyle = '#000'; rr(x, 2, 2, w - 4, h - 4, 6); x.fill(); } },
  'bar.fill': { size: [64, 10], m: [0, 6, 0, 6], draw: (x, w, h) => { x.fillStyle = CC.sky; rr(x, 0, 0, w, h, 5); x.fill(); } },
  'bar.fill.red': { size: [64, 10], m: [0, 6, 0, 6], draw: (x, w, h) => { x.fillStyle = CC.alert; rr(x, 0, 0, w, h, 5); x.fill(); } },
  // The pill: round caps both ends (LCARS's button). Labels black, right-aligned, as on a back-lit panel.
  'button.idle': { size: [120, 48], m: [22, 24, 22, 24], draw: (x, w, h) => { x.fillStyle = CC.signal; rr(x, 0, 0, w, h, 24); x.fill(); } },
  'button.hover': { size: [120, 48], m: [22, 24, 22, 24], draw: (x, w, h) => { x.fillStyle = CC.sky; rr(x, 0, 0, w, h, 24); x.fill(); } },
  'button.pressed': { size: [120, 48], m: [22, 24, 22, 24], draw: (x, w, h) => { x.fillStyle = CC.lilac; rr(x, 0, 0, w, h, 24); x.fill(); } },
  // A tab is a button that grew a flat side: one cap on the left, square on the right.
  'tab.idle': { size: [120, 40], m: [18, 6, 18, 20], draw: (x, w, h) => { x.fillStyle = CC.deep; rr(x, 0, 0, w, h, [20, 2, 2, 20]); x.fill(); } },
  'tab.active': { size: [120, 40], m: [18, 6, 18, 20], draw: (x, w, h) => { x.fillStyle = CC.accord; rr(x, 0, 0, w, h, [20, 2, 2, 20]); x.fill(); } },
  'strip.top': { size: [512, 72], m: [10, 96, 16, 96], draw: (x, w, h) => ccStrip(x, w, h, false) },
  'strip.bottom': { size: [512, 72], m: [16, 96, 10, 96], draw: (x, w, h) => ccStrip(x, w, h, true) },
  'divider': { size: [128, 10], m: [0, 18, 0, 18], draw: (x, w, h) => { x.fillStyle = CC.deep; rr(x, 16, 3, w - 32, 4, 2); x.fill(); x.fillStyle = CC.signal; rr(x, 0, 2, 12, 6, 3); x.fill(); rr(x, w - 12, 2, 12, 6, 3); x.fill(); } },
};
// Proposed kit pieces for the battle console (openspec/changes/battle-console): an allocation cell, the discrete box of
// CLAUDE.md section 7 ("allocation is discrete boxes"), in four states. Concord: a small flat pill, lit from behind.
const ccCells = {
  'cell.idle': { size: [32, 28], m: [10, 10, 10, 10], draw: (x, w, h) => { x.fillStyle = CC.deep; rr(x, 0, 0, w, h, 8); x.fill(); } },
  'cell.lit': { size: [32, 28], m: [10, 10, 10, 10], draw: (x, w, h) => { x.fillStyle = CC.sky; rr(x, 0, 0, w, h, 8); x.fill(); } },
  'cell.warn': { size: [32, 28], m: [10, 10, 10, 10], draw: (x, w, h) => { x.fillStyle = CC.alert; rr(x, 0, 0, w, h, 8); x.fill(); } },
  'bar.fill.accord': { size: [64, 10], m: [0, 6, 0, 6], draw: (x, w, h) => { x.fillStyle = CC.accord; rr(x, 0, 0, w, h, 5); x.fill(); } },
  'cell.accord': { size: [32, 28], m: [10, 10, 10, 10], draw: (x, w, h) => { x.fillStyle = CC.accord; rr(x, 0, 0, w, h, 8); x.fill(); } },
  'cell.locked': { size: [32, 28], m: [10, 10, 10, 10], draw: (x, w, h) => { x.strokeStyle = CC.deep; x.lineWidth = 2; rr(x, 1, 1, w - 2, h - 2, 7); x.stroke(); } },
};
function ccStrip(x, w, h, flip) {
  // A tall block at the left, rounded on its outer side, sweeping into a thin bar along one edge with a cap at the end.
  x.save(); if (flip) { x.translate(0, h); x.scale(1, -1); }
  const by = h - 12, bt = 8;
  x.fillStyle = CC.signal; x.beginPath();
  x.moveTo(28, 0); x.lineTo(72, 0); x.lineTo(72, by - 10); x.arcTo(72, by, 82, by, 10); x.lineTo(w - 44, by); x.lineTo(w - 44, by + bt);
  x.lineTo(28, by + bt); x.arcTo(0, by + bt, 0, by + bt - 28, 28); x.lineTo(0, 28); x.arcTo(0, 0, 28, 0, 28); x.closePath(); x.fill();
  x.fillStyle = CC.sky; rr(x, w - 39, by, 22, bt, 4); x.fill();
  x.fillStyle = CC.deep; rr(x, w - 13, by, 13, bt, 4); x.fill();
  x.restore();
}
const concordHero = {
  'nameplate': (x, w, h) => {
    x.fillStyle = CC.signal; rr(x, 0, 0, 96, h, [28, 4, 4, 28]); x.fill();
    x.fillStyle = CC.deep; rr(x, 102, h - 10, w - 140, 8, 4); x.fill();
    x.fillStyle = CC.sky; rr(x, w - 32, h - 10, 32, 8, 4); x.fill();
  },
  // Helm: a saucer. Eight arc keys round a round stop key.
  'helm.pad': (x, w, h) => {
    const c = w / 2, gap = 0.06;
    for (let i = 0; i < 8; i++) { const a = -P / 2 + (i - 0.5) * P / 4; x.fillStyle = i % 2 ? CC.deep : CC.signal; ccArcSeg(x, c, c, 58, 128, a + gap, a + P / 4 - gap); x.fill(); }
    x.fillStyle = CC.signal; x.beginPath(); x.arc(c, c, 46, 0, 2 * P); x.fill();
    x.strokeStyle = CC.deep; x.lineWidth = 3; x.beginPath(); x.arc(c, c, 140, 0, 2 * P); x.stroke();
    x.fillStyle = '#000'; for (const a of [0, P / 2, P, 1.5 * P]) { x.save(); x.translate(c, c); x.rotate(a); x.fillRect(136, -6, 10, 12); x.restore(); }
  },
  // The systems display: two sweeps facing each other, thick on the left, thinner on the right.
  'ssd.bezel': (x, w, h) => {
    ccSweep(x, 0, 0, 220, h, { bar: 26, arm: 10, R: 24, r: 10, cap: 18, col: CC.signal, capCol: CC.sky });
    x.save(); x.translate(w, 0); x.scale(-1, 1); ccSweep(x, 0, 0, 220, h, { bar: 14, arm: 6, R: 16, r: 8, cap: 0, col: CC.deep, capCol: CC.deep }); x.restore();
  },
  'dial': (x, w, h) => {
    const c = w / 2; x.strokeStyle = CC.deep; x.lineWidth = 6; x.beginPath(); x.arc(c, c, 54, 0, 2 * P); x.stroke();
    x.lineWidth = 2; x.beginPath(); x.arc(c, c, 36, 0, 2 * P); x.stroke(); x.beginPath(); x.arc(c, c, 18, 0, 2 * P); x.stroke();
    x.strokeStyle = CC.signal; x.lineWidth = 6; for (let i = 0; i < 4; i++) { x.beginPath(); x.arc(c, c, 54, i * P / 2 - 0.12, i * P / 2 + 0.12); x.stroke(); }
  },
  'lamp.alert.green': (x, w) => ccLamp(x, w, CC.accord), 'lamp.alert.yellow': (x, w) => ccLamp(x, w, CC.caution), 'lamp.alert.red': (x, w) => ccLamp(x, w, CC.alert),
  // The Concord mark (original): five worlds round an open circle, crossed by one orbit.
  'crest': (x, w) => {
    const c = w / 2; x.strokeStyle = CC.sky; x.lineWidth = 4; x.beginPath(); x.arc(c, c, 44, 0, 2 * P); x.stroke();
    x.fillStyle = CC.signal; for (let i = 0; i < 5; i++) { const a = -P / 2 + i * 2 * P / 5; x.beginPath(); x.arc(c + Math.cos(a) * 27, c + Math.sin(a) * 27, 7, 0, 2 * P); x.fill(); }
    x.strokeStyle = CC.lilac; x.lineWidth = 3; x.beginPath(); x.arc(c, c, 11, 0, 2 * P); x.stroke();
    x.strokeStyle = CC.sky; x.lineWidth = 2; x.beginPath(); x.ellipse(c, c, 40, 13, -0.5, 0, 2 * P); x.stroke();
  },
};
function ccLamp(x, w, col) { const c = w / 2; x.fillStyle = CC.deep; x.beginPath(); x.arc(c, c, 30, 0, 2 * P); x.fill(); x.fillStyle = '#000'; x.beginPath(); x.arc(c, c, 25, 0, 2 * P); x.fill(); x.fillStyle = col; x.beginPath(); x.arc(c, c, 21, 0, 2 * P); x.fill(); }
// Concord icons: open circles and arcs in one even line. The station icons are pictures of their jobs (owner,
// 2026-11-03: "make icons more unique and interesting, transporter should be a guy dematerializing with horizontal lines
// for example. fleet should be a formation of 3 chevrons, repair should be a wrench"): a ship's wheel, a bolt, a
// turret (owner: "tactical should be a turret?"), a shield with a burst, an atom, a dish, a wrench, a figure solid above the waist and dissolving into slices below it, three chevrons.
// col: the ink, Sky by default; black on a lit (active) tab, as the pill keys' labels are.
function ccIcon(name, col = CC.sky) {
  return paint(48, 48, (x) => {
    x.strokeStyle = col; x.fillStyle = col; x.lineWidth = 3;
    const C = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.stroke(); };
    const D = (cx, cy, r) => { x.beginPath(); x.arc(cx, cy, r, 0, 2 * P); x.fill(); };
    const A = (cx, cy, r, a0, a1) => { x.beginPath(); x.arc(cx, cy, r, a0, a1); x.stroke(); };
    ({
      helm: () => { C(24, 24, 12); D(24, 24, 4); for (let i = 0; i < 8; i++) { const a = i * P / 4, c = Math.cos(a), sn = Math.sin(a); x.beginPath(); x.moveTo(24 + c * 4, 24 + sn * 4); x.lineTo(24 + c * 19, 24 + sn * 19); x.stroke(); D(24 + c * 20, 24 + sn * 20, 2.6); } },
      fire: () => { C(24, 24, 18); C(24, 24, 10); D(24, 24, 3.5); },
      shields: () => { A(24, 30, 19, P * 1.12, P * 1.88); A(24, 30, 12, P * 1.15, P * 1.85); D(24, 32, 4); },
      tractor: () => { C(10, 24, 6); C(38, 24, 6); x.beginPath(); x.moveTo(17, 20); x.lineTo(31, 20); x.moveTo(17, 28); x.lineTo(31, 28); x.stroke(); },
      transport: () => { x.beginPath(); x.arc(24, 9, 5.5, 0, 2 * P); x.fill(); x.beginPath(); x.moveTo(14, 36); x.lineTo(15, 21); x.quadraticCurveTo(24, 15, 33, 21); x.lineTo(34, 36); x.closePath(); x.fill(); x.fillRect(16, 35, 6, 9); x.fillRect(26, 35, 6, 9); x.globalCompositeOperation = 'destination-out'; for (let yy = 26, k = 0; yy < 46; yy += 3.4, k++) x.fillRect(0, yy, 48, 1 + k * 0.32); x.globalCompositeOperation = 'source-over'; for (const [sx, sy] of [[9, 30], [39, 33], [10, 40], [38, 42]]) x.fillRect(sx - 3, sy - 0.9, 6, 1.8); },
      science: () => { for (const r of [0, P / 3, -P / 3]) { x.beginPath(); x.ellipse(24, 24, 20, 7.5, r, 0, 2 * P); x.stroke(); } D(24, 24, 4); },
      comms: () => { x.beginPath(); x.arc(17, 27, 12, P * 0.75, P * 1.75, true); x.closePath(); x.stroke(); x.beginPath(); x.moveTo(17, 27); x.lineTo(12, 44); x.moveTo(17, 27); x.lineTo(22, 44); x.moveTo(9, 44); x.lineTo(25, 44); x.stroke(); D(26, 18, 2.5); A(26, 18, 8, -P * 0.5, 0); A(26, 18, 15, -P * 0.5, 0); },
      repair: () => { x.lineWidth = 6; x.lineCap = 'round'; x.beginPath(); x.moveTo(11, 37); x.lineTo(26, 22); x.stroke(); x.lineWidth = 5; A(31, 17, 9, -P / 4 + 0.95, -P / 4 - 0.95 + 2 * P); x.lineWidth = 3; },
      map: () => { C(12, 14, 6); C(36, 20, 6); C(20, 36, 6); x.lineWidth = 2; x.beginPath(); x.moveTo(18, 15); x.lineTo(30, 19); x.moveTo(33, 25); x.lineTo(24, 32); x.moveTo(17, 30); x.lineTo(13, 20); x.stroke(); },
      // Proposed for the battle console (openspec/changes/battle-console): the same open circles and arcs, one even line.
      power: () => { C(24, 24, 19); x.beginPath(); x.moveTo(27, 7); x.lineTo(15, 26); x.lineTo(23, 26); x.lineTo(20, 41); x.lineTo(33, 20); x.lineTo(25, 20); x.closePath(); x.fill(); },
      tactical: () => { x.fillRect(7, 39, 34, 5); x.beginPath(); x.roundRect(12, 27, 24, 13, [7, 7, 2, 2]); x.fill(); x.lineWidth = 3.5; x.lineCap = 'round'; x.beginPath(); x.moveTo(25, 31); x.lineTo(42, 14); x.moveTo(21, 29); x.lineTo(38, 12); x.stroke(); x.fillRect(19, 22, 10, 6); x.lineWidth = 3; },
      pd: () => { x.beginPath(); x.moveTo(24, 5); x.lineTo(40, 11); x.quadraticCurveTo(40, 33, 24, 43); x.quadraticCurveTo(8, 33, 8, 11); x.closePath(); x.stroke(); x.beginPath(); for (let i = 0; i < 12; i++) { const a = -P / 2 + i * P / 6, r = i % 2 ? 4 : 9; x.lineTo(24 + Math.cos(a) * r, 23 + Math.sin(a) * r); } x.closePath(); x.fill(); },
      fleet: () => { const chev = (cx, cy, k) => { x.beginPath(); x.moveTo(cx, cy - 8 * k); x.lineTo(cx + 8 * k, cy + 6 * k); x.lineTo(cx, cy + 2 * k); x.lineTo(cx - 8 * k, cy + 6 * k); x.closePath(); x.fill(); }; chev(24, 13, 1.35); chev(11, 33, 1.15); chev(37, 33, 1.15); },
      ship: () => { C(24, 16, 10); x.beginPath(); x.roundRect(11, 26, 6, 18, 3); x.stroke(); x.beginPath(); x.roundRect(31, 26, 6, 18, 3); x.stroke(); D(24, 32, 3); },
      sensors: () => { D(24, 24, 4); C(24, 24, 11); A(24, 24, 19, -P * 0.8, -P * 0.2); A(24, 24, 19, P * 0.2, P * 0.8); },
      cargo: () => { for (const [cx, cy] of [[15, 15], [33, 15], [15, 33], [33, 33]]) C(cx, cy, 7); },
      craft: () => { C(30, 18, 9); C(14, 34, 4.5); C(24, 40, 3); D(30, 18, 3); },
      weapons: () => { C(18, 24, 10); D(18, 24, 3); x.beginPath(); x.moveTo(28, 24); x.lineTo(40, 24); x.stroke(); D(42, 24, 3.5); },
      warp: () => { C(33, 24, 8); D(33, 24, 2.5); x.beginPath(); x.moveTo(5, 15); x.lineTo(21, 15); x.moveTo(2, 24); x.lineTo(21, 24); x.moveTo(5, 33); x.lineTo(21, 33); x.stroke(); },
      engines: () => { x.fillRect(19, 4, 10, 9); x.beginPath(); x.moveTo(18, 13); x.lineTo(30, 13); x.lineTo(37, 28); x.lineTo(11, 28); x.closePath(); x.fill(); x.beginPath(); x.moveTo(13, 31); x.quadraticCurveTo(15, 41, 24, 46); x.quadraticCurveTo(33, 41, 35, 31); x.stroke(); x.beginPath(); x.moveTo(19, 31); x.quadraticCurveTo(20, 37, 24, 40); x.quadraticCurveTo(28, 37, 29, 31); x.closePath(); x.fill(); },
      reactor: () => { C(24, 24, 17); C(24, 24, 8); for (let i = 0; i < 3; i++) { const a = -P / 2 + i * 2 * P / 3; D(24 + Math.cos(a) * 12.5, 24 + Math.sin(a) * 12.5, 2.8); } },
      pause: () => { x.beginPath(); x.roundRect(13, 10, 8, 28, 4); x.fill(); x.beginPath(); x.roundRect(27, 10, 8, 28, 4); x.fill(); },
      menu: () => { for (const y of [12, 22, 32]) { x.beginPath(); x.roundRect(10, y, 28, 6, 3); x.fill(); } },
      settings: () => { C(24, 24, 8); for (let i = 0; i < 6; i++) { const a = i * P / 3; D(24 + Math.cos(a) * 17, 24 + Math.sin(a) * 17, 3.5); } },
    })[name]();
  });
}

// The live readouts drawn over the hero pieces: helm arrows, the systems display, the dial, the target bracket.
const ccLive = {
  arrow: (x) => { x.fillStyle = '#000'; x.beginPath(); x.arc(6, 0, 6, 0, 2 * P); x.fill(); x.strokeStyle = '#000'; x.lineWidth = 3; x.beginPath(); x.moveTo(-12, 0); x.lineTo(0, 0); x.stroke(); },
  helmCentre: (x, c) => { x.fillStyle = '#000'; x.font = '700 22px Antonio'; x.textAlign = 'center'; x.fillText('STOP', c, c + 8); },
  ssd: (x, cx, cy, sh, col) => {
    for (let i = 0; i < 6; i++) { const a = -P / 2 + i * P / 3; x.strokeStyle = col(sh[i]); x.lineWidth = 7; x.beginPath(); x.arc(cx, cy, 102, a - P / 6 + 0.08, a + P / 6 - 0.08); x.stroke(); }
    x.strokeStyle = CC.sky; x.lineWidth = 2.5; x.beginPath(); x.arc(cx, cy - 26, 40, 0, 2 * P); x.stroke();
    rr(x, cx - 10, cy - 4, 20, 50, 10); x.stroke(); rr(x, cx - 52, cy + 4, 14, 70, 7); x.stroke(); rr(x, cx + 38, cy + 4, 14, 70, 7); x.stroke();
    x.beginPath(); x.moveTo(cx - 10, cy + 24); x.lineTo(cx - 38, cy + 30); x.moveTo(cx + 10, cy + 24); x.lineTo(cx + 38, cy + 30); x.stroke();
    x.fillStyle = CC.accord; for (const [mx, my] of [[cx, cy - 56], [cx - 22, cy - 34], [cx + 22, cy - 34], [cx, cy + 40]]) { x.beginPath(); x.arc(mx, my, 5, 0, 2 * P); x.fill(); }
  },
  dial: (x, c, cts) => { x.strokeStyle = CC.sky; x.lineWidth = 3; x.beginPath(); x.arc(c, c, 46, -P / 2, 0.4); x.stroke(); for (const [dx, dy] of cts) { x.fillStyle = CC.alert; x.beginPath(); x.arc(c + dx * 36, c + dy * 36, 4, 0, 2 * P); x.fill(); } },
  bracket: (x, bx, by, bw, bh) => { x.strokeStyle = CC.sky; x.lineWidth = 3; for (const [px, py, a] of [[bx, by, P], [bx + bw, by, 1.5 * P], [bx + bw, by + bh, 0], [bx, by + bh, P / 2]]) { x.beginPath(); x.arc(px + (a === P || a === P / 2 ? 30 : -30), py + (a === P || a === 1.5 * P ? 30 : -30), 30, a, a + P / 2); x.stroke(); } },
};
return { cells: ccCells, live: ccLive, CC, sweep: ccSweep, arcSeg: ccArcSeg, strip: ccStrip, lamp: ccLamp, kit: concordKit, hero: concordHero, icon: ccIcon };
})();
