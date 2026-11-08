// The Aelith Conclave skin (mockup 4, OpenSpec change console-skins): a wise and advanced race (owner, 2026-10-30:
// "an wise and advanced alien race. holographic buttons, with organic shaped metal armor peices surrounding the
// edges"). The one hybrid skin (owner: "the conclave is one exception. They have a hybrid of both vector graphics for
// holographic buttons and icons, but 3d rendered shell around the UI panels and elements"), built on the analog panel
// bible (owner, 2026-10-31: "Using these design principles. Update conclave UI"): every piece's shell is satin gold and
// pearl modelled and rendered in Blender (tools/blender/build_ui_aelith.py), its wells a rim-lit void; the holograms are
// drawn as vectors into those wells: pointed lozenges of light for keys, fields with scanlines and a split-colour edge,
// crystals, sacred-geometry icons. Both sides read one set of shapes (assets/ui/aelith/shapes.json, fit-checked by
// tools/mockups/aelith_shapes.py), and every hologram is clipped to its well, so none spills onto the gold. A piece is
// the render and its hologram layered in one SVG image, so the page's 9-slice and hero slots take it like any other kit
// image. Registers itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
// The Conclave's look has one implementation, docs/mockups/lib/aelith.js (window.FCAelith, inlined before this module),
// shared with the battle console: its holograms, the layering of each over its rendered shell, its icons and its live
// readouts. This module keeps the skins page's own tokens and registers the skin with this page's registry.
const F = window.FCK, H = window.FCAelith, AE = H.AE;
function build() {
  const d = F.kitData('assets/ui/aelith/manifest.json'), shapes = F.kitData('assets/ui/aelith/shapes.json');
  if (!d || !shapes) return null;
  const pieces = {};
  for (const [name, p] of Object.entries(d.pieces)) pieces[name] = Object.assign({}, p, { data: H.layered(name, p, shapes.pieces[name]) });
  for (const [name, col] of Object.entries(H.FILLS)) pieces[name] = { size: [64, 10], margins: [0, 6, 0, 6], data: F.png(H.fillPiece(col)) };
  for (const [name, level, col] of H.CELL_SYMBOLS) pieces[name] = { kind: 'cell', size: [8, 12], gap: 3, data: F.png(F.paint(8, 12, (x) => H.lozenge(x, level, col))) };
  // the column's ground: the rendered gold backing (CLAUDE.md section 7, "No black behind a metallic skin's panels")
  return Object.assign(F.fromManifest({ pieces }, (n) => H.icon(n)), d.pieces['console.backing'] ? { column: d.pieces['console.backing'].data } : {});
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('aelith', {
  name: 'Aelith Conclave', kind: 'Hybrid: a gold and pearl shell rendered in Blender, holograms drawn as vectors in its wells', bg: '#03111a',
  css: { '--s-ink': AE.holoHi, '--s-dim': '#9fc9cf', '--s-num': AE.holo, '--s-body': '"Exo 2", sans-serif', '--btn-ink': AE.holoHi, '--btn-shadow': `0 0 6px ${AE.holo}`, '--btn-justify': 'center',
    '--btn-font': '400 11px/1 "Michroma", sans-serif', '--btn-track': '0.08em', '--btn-case': 'uppercase', '--btn-motion': 'filter 0.25s, padding-top 0.06s', '--btn-sink': '1.5px',
    '--tab-font': '400 9.5px/1 "Michroma", sans-serif', '--tab-pb': '1px', '--btn-pb': '3px', '--tab-ink': '#9fc9cf', '--tab-ink-on': AE.holoHi, '--title-font': '400 15px/1 "Michroma", sans-serif', '--title-ink': AE.goldHi,
    '--lbl-font': '600 15px/1 "Exo 2", sans-serif', '--num-font': '300 28px/1 "Exo 2", sans-serif',
    // The strips are 72 px (owner, 2026-10-31: "we need to make parent taller"); their open band is the channel, rows 12-59,
    // measured by tools/mockups/console_fit.mjs, so each label sits over its bar with 3 px clear of the art.
    '--stt-pt': '12px', '--stt-pb': '12px', '--stb-pt': '12px', '--stb-pb': '12px', '--strip-lamp': '40px' },
  accent: AE.holo, warn: AE.coral,
  // The title sits inside the well, just under its top rim and right of the corner knuckle (well from 16 px, knuckles to
  // 41 px), so it never crosses the gold.
  title: (t, m) => { t.style.cssText = `left:${48 - m[3]}px;top:${-m[0] + 21}px;letter-spacing:0.18em;text-shadow:0 0 6px ${AE.holo};color:${AE.holoHi};`; },
  plateText: { left: 60, top: 12, width: 340, height: 32, font: '400 15px/1 "Michroma", sans-serif', color: AE.holoHi, sub: 'SERENE TIDE · ELDER IAVEN' },
  helmKeys: 'ring', helmR: 98, shield: [AE.holo, AE.amber, AE.coral],
  fonts: ['400 20px "Michroma"', '600 20px "Exo 2"', '300 20px "Exo 2"'], view: { title: '400 22px "Michroma"', body: '300 24px "Exo 2"', upper: true },
  build,
  live: H.live,
  world: ['Patient knowledge kept over aeons: act rarely, act exactly.', 'Graceful bodies and psionic minds: they touch light, not switches.', 'Light shaped by thought, held in sculpted alloy shells.', 'Smooth golden shells round crystal cores.'],
  shapes: 'Pointed lozenges of light held in pillowed gold shells whose wells are grown into knuckles, scallops and embracing arms; pearls set in the metal.', light: 'A soft main light from the upper left on satin gold; each well a dark void rim-lit in cyan from under its lip; holograms with scanlines and a split-colour edge.',
  motion: 'Light brightens and settles; nothing moves.', type: '"Michroma" wide capitals over "Exo 2".',
  palette: [['Pearl', AE.pearl, 'Inlays', 'Wisdom: the slow layering of ages'], ['Gold', AE.gold, 'Armour', 'Permanence'], ['Void', AE.void, 'Fields', 'Serenity, depth'],
    ['Holo', AE.holo, 'Light, readouts', 'Thought made visible'], ['Violet', AE.violet, 'Crystal cores', 'The psionic'], ['Coral', AE.coral, 'Alerts', 'A rare disturbance']],
});
})();
