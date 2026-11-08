// The Tarsine Remnant skin (mockup 4, OpenSpec change console-skins): a powerful ancient race that stopped changing
// long ago (owner, 2026-10-30: "a powerful ancient race. rusty, with back lit buttons clogged with dirt. their ships
// havent been upgraded in ages, rough around the edges"). Since 2026-10-31 it is rendered in Blender on the analog panel
// bible (owner: "I love the Remnant ui, i htink it could use some polish from our guide, but please dont remove the
// grimey industrial feel"): monumental corroded iron with worn rolled edges, rust, verdigris, pitting and soot in every
// crevice, great rivets with rust running down from them, worn gold bezels and brackets, arched-tablet keys and lintel
// tabs whose grimy glass carries the same caked dirt in every state (only the backlight changes), cracked gauge glass,
// lit by old lamps under the lips that do not all still burn (tools/mockups/tarsine_shapes.py,
// tools/blender/build_ui_tarsine.py, assets/ui/tarsine/). This module registers that kit; the live readouts, the carved
// glyph icons and the view's brackets are the library's. Registers itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
// The Remnant's materials, icons and live readouts have one implementation, docs/mockups/lib/tarsine.js (window.FCTarsine,
// inlined before this module), shared with the battle console; this module keeps the skins page's own tokens and
// registers the skin with this page's registry.
const F = window.FCK, H = window.FCTarsine, TA = H.TA;

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('tarsine', {
  name: 'Tarsine Remnant', kind: 'Corroded monumental iron rendered in Blender; backlights clogged with dirt', bg: '#110e0b',
  css: { '--s-ink': TA.bone, '--s-dim': TA.dim, '--s-num': TA.light, '--s-body': '"Alegreya Sans SC", sans-serif', '--btn-ink': '#1e160c', '--btn-shadow': '0 1px 0 rgba(255,230,160,0.35)', '--btn-justify': 'center',
    '--btn-pt': '0px', '--btn-pb': '3px', '--btn-font': '700 16px/1 "Cinzel", serif', '--btn-track': '0.08em', '--btn-case': 'uppercase', '--btn-motion': 'padding-top 0.06s', '--btn-sink': '1.5px',
    '--tab-font': '700 12px/1 "Cinzel", serif', '--tab-ink': TA.dim, '--tab-ink-on': '#1e160c', '--title-font': '700 17px/1 "Cinzel", serif', '--title-ink': TA.goldHi,
    '--lbl-font': '700 16px/1 "Alegreya Sans SC", sans-serif', '--num-font': '700 24px/1 "Alegreya Sans SC", sans-serif',
    // The strips are 72 px; their open band is the beam's channel of grimy glass, rows 13-59 inside its worn rolled lip
    // (tools/mockups/tarsine_shapes.py), so each label stands over its bar with 3 px clear of the art.
    '--stt-pt': '13px', '--stt-pb': '13px', '--stb-pt': '13px', '--stb-pb': '13px', '--strip-lamp': '40px' },
  accent: TA.light, warn: TA.ember,
  // The title sits in the frame's well, right of the top-left capital, under the well's worn lip.
  title: (t, m) => { t.style.cssText = `left:${38 - m[3]}px;top:${19 - m[0]}px;letter-spacing:0.12em;text-shadow:0 1px 0 #000, 0 0 3px #000;`; },
  plateText: { left: 44, top: 13, width: 372, height: 30, font: '700 19px/1 "Cinzel", serif', color: '#f2e6c4',
    // pale carved letters outlined dark: they read over the lit gold and the grime stains alike (the dirt stays)
    shadow: '0 0 2px #0c0703, 0 0 2px #0c0703, 0 1px 0 #0c0703', sub: 'ANKHAR · ELDEST VOR' },
  helmKeys: [[150, 63], [236, 63], [236, 149], [236, 235], [150, 235], [64, 235], [64, 149], [64, 63]], shield: [TA.light, '#c9873a', TA.ember],
  fonts: ['700 20px "Cinzel"', '700 20px "Alegreya Sans SC"'], view: { title: '700 26px "Cinzel"', body: '700 24px "Alegreya Sans SC"', upper: true },
  build: () => { const d = F.kitData('assets/ui/tarsine/manifest.json'); return d ? F.fromManifest(d, (n) => H.icon(n)) : null; },
  live: H.live,
  world: ['A fallen empire that remembers being first, and will not change.', 'Huge, slow and long-lived: the crews outlast their worlds.', 'Machines older than their crews, patched and never replaced.', 'Massive, scarred, rusted dreadnoughts.'],
  shapes: 'Arched tablets, stepped frames with capitals, lintels, heavy brackets: monuments.', light: 'Old lamps under the lips, not all still burning; backlight through caked dirt; worn rolled edges on rusted iron.',
  motion: 'Almost none: a key gives a millimetre and the light behind it steadies.', type: '"Cinzel" carved capitals over "Alegreya Sans SC".',
  palette: [['Soot', TA.soot, 'Grime, shadow', 'Ages of use'], ['Rust', TA.rust, 'Frames', 'Decline they will not admit'], ['Verdigris', TA.verd, 'Corrosion', 'Time'],
    ['Old gold', TA.gold, 'Inlays, glyphs', 'Old majesty'], ['Backlight', TA.light, 'Keys, readouts', 'The fire still burning'], ['Ember', TA.ember, 'Alerts', 'Anger, slow to wake']],
});
})();
