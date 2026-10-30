// The Ssarith Hunt skin (mockup 4, OpenSpec change console-skins): a reptilian race that hunts for sport, its console a
// hunter's kit carried through a dense jungle (owner, 2026-10-30: "A gurilla style faction, gun metal, beveled corner,
// industrial retro CRT style buttons", then "that faction is from the gorn, which are more hunter like faction, they
// enjoy the sport, so their ui is reminiscent of a dense jungle akin to the Predator"). The kit is modelled, lit and
// rendered in Blender (tools/blender/build_ui_ssarith.py) and inlined into the page; this module adds the skin's tokens
// and draws only the live readouts, as phosphor traces on the tubes. Registers itself with the page's skin registry
// (docs/mockups/skins). Our own design: no other game's or film's names, shapes, glyphs or art.
(function () {
'use strict';
const F = window.FCK, P = F.P;
const SS = { phos: '#6dff8a', phosLo: '#2f9a4f', bone: '#d8cfb4', kill: '#e0533c', amber: '#ffb340', ink: '#d8cfb4', dim: '#7f8f6e', glow: 'rgba(109,255,138,0.55)' };

// A phosphor stroke: the trace drawn twice, a soft glow under a sharp line.
function glowStroke(x, w, col) {
  x.save(); x.shadowColor = SS.glow; x.shadowBlur = 8; x.strokeStyle = col || SS.phos; x.lineWidth = w; x.stroke(); x.restore();
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('ssarith', {
  name: 'Ssarith Hunt', kind: 'A hunter\'s jungle kit modelled in Blender: gunmetal, camouflage, vines, CRT keys', bg: '#0c120d', sampleBg: '#1c2a1d',
  css: { '--s-ink': SS.bone, '--s-dim': SS.dim, '--s-num': SS.phos, '--s-body': '"VT323", monospace', '--btn-ink': SS.phos, '--btn-shadow': '0 0 6px rgba(109,255,138,0.75)', '--btn-justify': 'center',
    '--btn-font': '400 22px/1 "VT323", monospace', '--btn-track': '0.12em', '--btn-case': 'uppercase', '--btn-motion': 'none', '--btn-press': 'translateY(1px)',
    '--tab-font': '400 18px/1 "VT323", monospace', '--tab-ink': SS.dim, '--tab-ink-on': SS.phos, '--title-font': '400 15px/1 "Black Ops One", sans-serif', '--title-ink': SS.bone,
    '--lbl-font': '400 20px/1 "VT323", monospace', '--num-font': '400 32px/1 "VT323", monospace', '--bt-rep': 'round', '--bf-rep': 'round' },
  accent: SS.phos, warn: SS.kill,
  title: (t, m) => { t.style.cssText = `left:${30 - m[3]}px;top:${-m[0] + 6}px;letter-spacing:0.14em;text-shadow:0 1px 0 rgba(0,0,0,0.8);`; },
  plateText: { left: 56, top: 13, width: 300, height: 30, font: '400 26px "VT323", monospace', color: SS.phos, sub: 'STALKER VESH-ORR · 14 TAKEN' },
  helmKeys: 'ring', helmR: 94, shield: [SS.phos, SS.amber, SS.kill],
  fonts: ['400 20px "VT323"', '400 16px "Black Ops One"'], view: { title: '400 30px "VT323"', body: '400 26px "VT323"', upper: true, ink: SS.phos, dim: SS.bone },
  build: () => { const d = F.kitData('assets/ui/ssarith/manifest.json'); return d ? F.fromManifest(d, () => F.paint(48, 48, () => {})) : null; },
  live: {
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
    // A tracking sweep: a fading wedge, the quarry as bone-white marks.
    dial: (x, c, cts) => {
      x.save();
      const g = x.createRadialGradient(c, c, 0, c, c, 46); g.addColorStop(0, 'rgba(109,255,138,0.0)'); g.addColorStop(1, 'rgba(109,255,138,0.28)');
      x.fillStyle = g; x.beginPath(); x.moveTo(c, c); x.arc(c, c, 44, -1.2, -0.55); x.closePath(); x.fill();
      x.beginPath(); x.moveTo(c, c); x.lineTo(c + Math.cos(-0.55) * 44, c + Math.sin(-0.55) * 44); glowStroke(x, 1.6);
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
  },
  world: ['The hunt is the only honest sport: the quarry must be worthy, the stalk patient, the kill earned and counted.', 'Big-boned reptiles with heavy clawed hands, slow and silent until they strike.', 'A hunter\'s kit: worn gunmetal, field camouflage, tracking tubes, grown over by the jungle it hides in.', 'Squat armoured hunting ships with broad prows, dark and mottled, built to wait unseen.'],
  shapes: 'Cut-cornered gunmetal, CRT keys in thick bezels, vines and fronds over the hardware, tally marks.', light: 'A dim light falling through the canopy; the tubes spill green onto the steel.',
  motion: 'A key\'s tube flares and settles; a tracker sweeps; nothing moves until it strikes.', type: '"Black Ops One" stencilled titles over "VT323" phosphor readouts.',
  palette: [['Gunmetal', '#3a4044', 'Plates, bezels, keys', 'The hunter\'s gear: plain, made to outlast the hunt'], ['Canopy', '#2f4a2a', 'Camouflage, field paint', 'The hunting ground; staying unseen'],
    ['Moss', '#5b6340', 'Camouflage mottling', 'Patience: the hunter becomes the jungle'], ['Phosphor', SS.phos, 'Tubes, readouts, traces', 'The tracker: the quarry in sight'],
    ['Bone', SS.bone, 'Tally marks, trophies, titles', 'The count of worthy kills'], ['Kill red', '#c2412b', 'Alerts only', 'The strike, given once']],
});
})();
