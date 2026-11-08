// The Ombrai skin (mockup 4, OpenSpec change console-skins): an enigmatic race (owner, 2026-10-30: "an enigmatic
// alien race with cool dark purple colors and pink buttons, progress bars are curcular, almost organic like eyes"),
// rebuilt on the analog panel bible (owner, 2026-10-31: "we need to update krai, onbrai, and hunt to use the new
// guildine standards ... Please make them look nice"). Every kit piece is modelled and rendered in Blender
// (tools/blender/build_ui_ombrai.py, from the fit-checked shapes of tools/mockups/ombrai_shapes.py): pillowed
// anodised plum metal with a thin-film sheen, lilac bezels, pink eye jewels on flat metal, and formed wells in the
// Ombrai's own shapes (crescent-bitten keys, petal wells round the helm's pupil, frame wells bitten like a waning moon,
// an eye-shaped ship display under two lids), each lit from below, so the top of every well stays veiled. Bars are rows
// of eyes that open as they fill. The live readouts (the comets on the helm, the ship and its shield rings, the iris
// radar, the target's crescents) and the icons are drawn in the same hand by docs/mockups/lib/ombrai.js, shared with
// the battle console. Registers itself with the
// page's skin registry (docs/mockups/skins).
(function () {
'use strict';
// The Ombrai's look has one implementation, docs/mockups/lib/ombrai.js (window.FCOmbrai, inlined before this module),
// shared with the battle console: its icons and its live readouts. This module keeps the skins page's own tokens and
// registers the skin with this page's registry; the kit is the render as it is.
const F = window.FCK, H = window.FCOmbrai, OM = H.OM;
function build() {
  const d = F.kitData('assets/ui/ombrai/manifest.json');
  if (!d) return null;
  // the column's ground: the rendered plum backing (CLAUDE.md section 7, "No black behind a metallic skin's panels")
  return Object.assign(F.fromManifest(d, (n) => H.icon(n)), d.pieces['console.backing'] ? { column: d.pieces['console.backing'].data } : {});
}

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('ombrai', {
  name: 'Ombrai', kind: 'Plum metal over violet wells lit from below; every gauge a watching eye', bg: '#0b0516',
  css: { '--s-ink': OM.pale, '--s-dim': OM.dim, '--s-num': OM.pink, '--s-body': '"Quicksand", sans-serif', '--btn-ink': OM.pale, '--btn-shadow': `0 0 6px ${OM.pink}`, '--btn-justify': 'center',
    '--btn-font': '700 13px/1 "Syncopate", sans-serif', '--btn-track': '0.14em', '--btn-case': 'uppercase', '--btn-motion': 'filter 0.4s, padding-top 0.06s', '--btn-sink': '1.5px',
    '--tab-font': '700 10px/1 "Syncopate", sans-serif', '--tab-ink': OM.dim, '--tab-ink-on': OM.pale, '--title-font': '700 14px/1 "Syncopate", sans-serif', '--title-ink': OM.lilac,
    '--lbl-font': '700 15px/1 "Quicksand", sans-serif', '--num-font': '500 26px/1 "Quicksand", sans-serif',
    // The strips are 72 px (owner, 2026-10-31: "we need to make parent taller"); their open band is the channel, rows 11-60,
    // measured by tools/mockups/console_fit.mjs, so each label sits over its bar with 3 px clear of the art.
    '--stt-pt': '11px', '--stt-pb': '11px', '--stb-pt': '11px', '--stb-pb': '11px', '--strip-lamp': '40px',
    // the well's bitten end glows 5-10 px past the 76 px slice margin: the strip's readouts start clear of it
    '--st-l': '88px', '--st-r': '88px' },
  accent: OM.pink, warn: OM.crimson,
  // The title sits in the frame's well, under its top lip and right of the bitten corner.
  title: (t, m) => { t.style.cssText = `left:${34 - m[3]}px;top:${24 - m[0]}px;letter-spacing:0.22em;text-shadow:0 0 8px ${OM.pink};`; },
  plateText: { left: 78, top: 12, width: 352, height: 32, font: '700 13px/1 "Syncopate", sans-serif', color: OM.pale, sub: 'VEIL OF ISH · THE WATCHER' },
  helmKeys: 'ring', helmR: 98, shield: [OM.pink, '#ffd36a', OM.crimson],
  fonts: ['700 20px "Syncopate"', '700 20px "Quicksand"', '500 20px "Quicksand"'], view: { title: '700 20px "Syncopate"', body: '500 26px "Quicksand"', upper: true },
  build,
  live: H.live,
  world: ['Hidden purpose: they watch, and are seldom seen.', 'Shadowy and many-eyed; their glances do the work of hands.', 'Technology that looks back at you: every instrument an eye.', 'Dark, curved, petal-like hulls with glowing eyes.'],
  shapes: 'Crescents, petals, moons, rings and eyes; nothing straight if a curve will do.', light: 'Lit from below: pink-violet light rising from the foot of each well, its top veiled; a thin-film sheen on the plum metal.',
  motion: 'Light swells slowly, like an eye opening.', type: '"Syncopate" wide capitals over "Quicksand".',
  palette: [['Void', OM.void, 'Wells', 'Secrecy'], ['Plum', OM.plum, 'Metal', 'The dream they live in'], ['Smoke', OM.smoke, 'Shadows in the wells', 'Veils'],
    ['Pink', OM.pink, 'Keys, readouts', 'Attention: the watching gaze'], ['Lilac', OM.lilac, 'Rings, titles', 'Memory'], ['Crimson', OM.crimson, 'Alerts', 'A glance that turns hostile']],
});
})();
