// The Kelvane Institute skin (mockup 4, OpenSpec change console-skins): a race of experimenters who trust only what an
// instrument can show. Their instruments were inspired by the Nucleo NLog look (owner, 2026-10-31: "They were inspired
// by the nucleo look. So.lets update that"), so the kit is built the way the session's NLog study was, in our own design:
// pillowed champagne plates over recessed displays, amber backlit film for readouts and green phosphor glass for scopes.
// It is modelled, lit and rendered in Blender (tools/blender/build_ui_kelvane.py, from the fit-checked shapes of
// tools/mockups/kelvane_shapes.py) and inlined into the page; this module adds the skin's tokens and draws only the live
// readouts, each inside its own well: names and keys in a calculator face on the amber windows, traces on the scopes.
// Registers itself with the page's skin registry (docs/mockups/skins).
(function () {
'use strict';
// The Institute's live readouts have one implementation, docs/mockups/lib/kelvane.js (window.FCKelvane, inlined before
// this module), shared with the battle console; this module keeps the skins page's own tokens (the kit's materials, as
// tools/blender/build_ui_kelvane.py's palette) and registers the skin with this page's registry.
const F = window.FCK, H = window.FCKelvane;
const KV = { champagne: '#c9bfa9', amber: '#f2c46e', ink: '#4a3412', inkOn: '#3a2408', engrave: '#2f2a22', dim: '#5a5245',
  phos: '#4dff88', phosHi: '#b4ffcf', needle: '#c8302a', brass: '#b0915a', bg: '#1c1a16' };

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('kelvane', {
  name: 'Kelvane Institute', kind: 'An NLog-style instrument kit in Blender: champagne plates, amber readouts, phosphor scopes', bg: KV.bg, sampleBg: KV.champagne,
  css: { '--s-ink': KV.engrave, '--s-dim': KV.dim, '--s-num': KV.engrave, '--s-body': '"Courier Prime", monospace', '--btn-ink': KV.ink, '--btn-justify': 'center',
    '--btn-shadow': '0 0 3px rgba(255, 226, 160, 0.55)', '--btn-font': '400 17px/1 "Share Tech Mono", monospace', '--btn-track': '0.12em', '--btn-case': 'uppercase',
    '--btn-motion': 'padding-top 0.06s', '--btn-sink': '1.5px', '--tab-font': '400 13px/1 "Share Tech Mono", monospace', '--tab-pb': '1px', '--tab-ink': '#2a1d0a', '--tab-ink-on': KV.inkOn,
    '--title-font': '700 13px/1 "Courier Prime", monospace', '--title-ink': KV.engrave, '--lbl-font': '700 12px/1 "Courier Prime", monospace',
    '--num-font': '400 27px/1 "Share Tech Mono", monospace',
    // The strips are 72 px (owner, 2026-10-31: "we need to make parent taller"); their open band is the plate inside its rolled rim, rows 4-69,
    // measured by tools/mockups/console_fit.mjs, so each label sits over its bar with 3 px clear of the art.
    '--stt-pt': '4px', '--stt-pb': '3px', '--stb-pt': '4px', '--stb-pb': '3px', '--strip-lamp': '44px' },
  accent: KV.phos, warn: KV.needle,
  // The title is engraved inside the panel's fine line (it ends 10 px in), starting 13 px clear of the corner screw (18 +/- 3 px).
  title: (t, m) => { t.style.cssText = `left:${34 - m[3]}px;top:${12 - m[0]}px;letter-spacing:0.16em;`; },
  // The captain's name sits in the name plate's amber window (46-360 x 9-47), 8 px clear of its left end.
  plateText: { left: 54, top: 12, width: 290, height: 32, font: '400 22px/32px "Share Tech Mono", monospace', color: KV.ink, sub: 'RV PATIENCE · DR. ORREL' },
  helmKeys: 'ring', helmR: 92, shield: [KV.phos, '#ffcf6a', '#ff4a3a'],
  fonts: ['700 20px "Courier Prime"', '400 20px "Share Tech Mono"'], view: { title: '400 26px "Share Tech Mono"', body: '400 24px "Share Tech Mono"', upper: true, ink: '#8affb0', dim: KV.amber },
  build: () => { const d = F.kitData('assets/ui/kelvane/manifest.json'); return d ? F.fromManifest(d, () => F.paint(48, 48, () => {})) : null; },
  live: H.live,
  world: ['Truth is measured, never assumed: every reading shown on an instrument a scientist can check by eye.', 'Careful, many-handed experimenters who like a control that clicks.',
    'Pressed champagne plates over backlit amber windows and phosphor scopes, built to be calibrated.', 'Rounded research hulls studded with sensor dishes and gauge clusters.'],
  shapes: 'Pillowed plates, keys in pressed dimples, a cradle of keys round a scope, meters in bays.', light: 'Amber film lit from behind and from under each lip; phosphor scopes; a soft main light from the upper left.',
  motion: 'A key sinks with a click as its pilot ring lights; needles swing and settle.', type: '"Share Tech Mono" calculator readouts on amber; "Courier Prime" engraved labels on the metal.',
  palette: [['Champagne', KV.champagne, 'Plates', 'The instrument itself'], ['Amber', KV.amber, 'Readout windows', 'A reading, patiently lit'], ['Phosphor', KV.phos, 'Scopes, traces', 'The measurement itself'],
    ['Needle red', KV.needle, 'Needles, alerts', 'The reading that matters'], ['Ink', KV.ink, 'Print on the film', 'The record'], ['Brass', KV.brass, 'Bezels, the crest', 'Craft and calibration']],
});
})();
