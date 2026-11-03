// The Krai Warhost skin (mockup 4, OpenSpec change console-skins): a combat race (owner, 2026-10-30: "a combat focused
// race, lots of triangular motifs, angry red colors and orange lit buttons that are activated. strong metal framing,
// almost blade like"), rebuilt on the analog panel bible (owner, 2026-10-31: "update krai, onbrai, and hunt to use the
// new guildine standards"). The kit is modelled and rendered in Blender (tools/blender/build_ui_krai.py) from fit-checked
// shapes (tools/mockups/krai_shapes.py, assets/ui/krai/shapes.json): blackened blade steel plates with a tight honed
// roll, red enamel bezels round every well, ember glass lit from below like a forge, Blade keys dark at rest, red on
// hover and forge orange when engaged, and bars of blade chevrons that light one by one. This module adds the type,
// the ink, the icons and the live readouts drawn into the measured windows. Kept apart from the Kethran Hegemony
// (gunmetal plates, wedge keycaps, amber chevrons): thinner, sharper, black and red. Registers itself with the page's
// skin registry (docs/mockups/skins).
(function () {
'use strict';
// The Krai look has one implementation, docs/mockups/lib/krai.js (window.FCKrai, inlined before this module): its
// palette, its icons (the full subject list of docs/design/icon-guide.html) and its live readouts. This module only
// registers it with this page's skin registry.
const F = window.FCK, K = window.FCKrai, KR = K.KR;
F.register('krai', {
  name: 'Krai Warhost', kind: 'Black blade steel and red enamel; keys lit orange when engaged', bg: '#0a0707',
  css: { '--s-ink': KR.bone, '--s-dim': KR.dim, '--s-num': KR.orange, '--s-body': '"Teko", sans-serif', '--btn-ink': KR.bone, '--btn-shadow': '0 1px 2px #000, 0 0 3px #000', '--btn-justify': 'center',
    '--btn-font': '400 18px/1 "Russo One", sans-serif', '--btn-track': '0.06em', '--btn-case': 'uppercase', '--btn-motion': 'padding-top 0.06s', '--btn-sink': '1.5px', '--btn-pb': '2px',
    '--tab-font': '400 14px/1 "Russo One", sans-serif', '--tab-ink': KR.dim, '--tab-ink-on': KR.bone, '--title-font': '400 18px/1 "Russo One", sans-serif', '--title-ink': KR.red,
    '--lbl-font': '500 19px/1 "Teko", sans-serif', '--num-font': '600 28px/1 "Teko", sans-serif',
    // The strips are 72 px (owner, 2026-10-31: "we need to make parent taller"); their open band is the blade channel, rows 13-58,
    // measured by tools/mockups/console_fit.mjs, so each label sits over its bar with 3 px clear of the art.
    '--stt-pt': '13px', '--stt-pb': '13px', '--stb-pt': '13px', '--stb-pb': '13px', '--strip-lamp': '38px' },
  accent: KR.orange, warn: KR.red,
  // The title heads the open field of the well (in flow, below the corner fangs), never on the steel or its enamel.
  title: (t, m) => { t.style.cssText = `position:relative;justify-self:start;align-self:flex-start;margin-left:4px;letter-spacing:0.1em;text-shadow:0 1px 0 #000;`; },
  plateText: { left: 86, top: 11, width: 318, height: 34, font: '400 20px/1 "Russo One", sans-serif', color: KR.bone, sub: 'KHARZ · WARLORD UTREK' },
  helmKeys: 'ring', helmR: 96, shield: [KR.orange, '#ffc23a', KR.red],
  fonts: ['400 20px "Russo One"', '500 20px "Teko"', '600 20px "Teko"'], view: { title: '400 26px "Russo One"', body: '500 28px "Teko"', upper: true },
  build: () => F.fromManifest(F.kitData('assets/ui/krai/manifest.json'), (n) => K.icon(n)),
  live: K.live,
  world: ['War is the only honest test; rank is won in blood.', 'Lean predators with long claws: short, sharp gestures.', 'Forged steel, edged everything; nothing round survives the forge.', 'Knife-blade hulls and swept triangles.'],
  shapes: 'Blades, fangs, spikes and triangles; nothing round.', light: 'A forge glow rising from under every lip; honed steel edges; red enamel; keys dark until engaged, then lit orange.',
  motion: 'A struck key stays put and burns: dark ember, red on hover, forge orange when engaged.', type: '"Russo One" for keys and titles over "Teko" numbers.',
  palette: [['Black steel', KR.black, 'Frames, keys', 'The forge'], ['Edge', KR.edge, 'Honed highlights', 'Readiness'], ['Blood', KR.blood, 'Inlays', 'Honour paid in blood'],
    ['Red', KR.red, 'Titles, warnings', 'Rage'], ['Orange', KR.orange, 'Engaged keys, readouts', 'The strike'], ['Bone', KR.bone, 'Text', 'Trophies']],
});
})();
