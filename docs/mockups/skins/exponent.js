// The Exponent skin (mockup 4, OpenSpec change console-skins). The owner, 2026-11-07: "Exponent is not a mix of two races.
// It is its own thing. Inspired by Borg. It's ship ui is a metalic utilitarian look. With veins a and tentacles and
// barnacles growing. Like am abandoned rusted ship at sea. Take over by reefs and coral". Take the shape, not the text
// (CLAUDE.md section 1). The look has one implementation, docs/mockups/lib/exponent.js (window.FCExponent, inlined before
// this module), shared with the battle console (openspec/changes/exponent-battle-console); this module keeps the skins
// page's own tokens (its CSS, type, title and plate placement, world view and palette rows) and registers the skin with
// this page's registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, H = window.FCExponent, EX = H.EX;

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('exponent', {
  name: 'Exponent', kind: 'Painted in canvas (first pass): a utilitarian steel console overgrown like a wreck on a reef: rust, salt, barnacles, coral, cabled tendrils and lit veins, hatch keys', bg: EX.bg, sampleBg: '#2a3236',
  css: { '--s-ink': EX.bone, '--s-dim': '#9fb0ad', '--s-num': EX.lumen, '--s-body': '"Chivo Mono", monospace', '--btn-ink': '#eef8f4', '--btn-shadow': '0 1px 2px #000, 0 0 3px #000', '--btn-justify': 'center',
    '--btn-font': '400 15px/1 "Allerta Stencil", sans-serif', '--btn-track': '0.08em', '--btn-case': 'uppercase', '--btn-motion': 'padding-top 0.06s', '--btn-sink': '1px', '--btn-pb': '2px',
    '--tab-font': '400 12px/1 "Allerta Stencil", sans-serif', '--tab-ink': '#9fb0ad', '--tab-ink-on': EX.lumenHot, '--title-font': '400 18px/1 "Allerta Stencil", sans-serif', '--title-ink': EX.bone,
    '--lbl-font': '500 13px/1 "Chivo Mono", monospace', '--num-font': '700 24px/1 "Chivo Mono", monospace',
    // The strips are 72 px; their open band is the dark channel, rows 11-61.
    '--stt-pt': '12px', '--stt-pb': '12px', '--stb-pt': '12px', '--stb-pb': '12px', '--strip-lamp': '44px', '--tab-pb': '4px' },
  accent: EX.lumen, warn: EX.breach,
  // The title stencilled at the head of the well, in bone, over a shadow.
  title: (t) => { t.style.cssText = 'position:relative;justify-self:start;align-self:flex-start;margin-left:2px;letter-spacing:0.1em;text-transform:uppercase;text-shadow:0 1px 2px #000;'; },
  plateText: { left: 72, top: 13, width: 344, height: 30, font: '400 19px/1 "Allerta Stencil", sans-serif', color: EX.bone, shadow: '0 1px 2px #000', sub: 'ACCRETION 41 · KEEPER OF LINES' },
  helmKeys: H.HELM, shield: [EX.lumen, EX.amber, EX.breach],
  fonts: ['400 20px "Allerta Stencil"', '700 20px "Chivo Mono"', '500 20px "Chivo Mono"'], view: { title: '400 26px "Allerta Stencil"', body: '700 21px "Chivo Mono"', upper: true, ink: EX.bone, dim: '#a9c4bf' },
  build: () => Object.assign(F.fromPainters(H.nine, H.heroSheet, (n) => H.icon(n), false), { column: F.png(F.paint(472, 1080, H.column)) }),
  overgrow: H.overgrow,
  live: H.live,
  world: ['One collective that grows by taking in ships, stations and crews; nothing is decorated, everything is function, and nothing taken is thrown away.',
    'Provisional and the owner\'s to set; the design draws on none of it. Three traditions shape its things: the assimilators, the conduit-keepers, the reef.',
    'Machinery kept running long past any refit: whatever is taken is bolted on as found, power runs in sheathed veins of cable and fluid line, and the growth it has absorbed is left to accrete in layers.',
    'Hulks of joined hulls under barnacle, coral and cable, like wrecks that have lain on a sea floor and still run.'],
  shapes: 'Hatch keys (a steel access hatch on a hinge, a dog latch, four bolts, a lit slot), riveted plate with patches bolted on, and a reef over all of it: coralline crust, plate, brain and branching coral, sponges, anemones, tube worms, dense barnacles, thick cabled tendrils across the gaps between panels, sheathed veins pulsing along every foot. Growth keeps to margins, edges and gaps, never a label or key face.',
  light: 'A hard work light from the upper left over chipped paint, rust and salt; the only glow is the cold lumen in the slots, the veins and the lenses.',
  motion: 'Hover lights a hatch\'s slot; pressed, the hatch sinks a pixel and its slot flares; the growth on it never moves.',
  type: '"Allerta Stencil", a stencilled sans, for keys and titles, over "Chivo Mono" for labels and numbers.',
  palette: [['Hull steel', EX.steel, 'Frames, keys, plates', 'The assimilators: nothing decorated, the plate as it was taken'], ['Rust', EX.rust, 'Wear, chips, runs', 'Kept running long past any refit'],
    ['Reef coral', EX.coral, 'Coral, tube worm plumes', 'The reef: growth tolerated and built on, in layers'], ['Barnacle bone', EX.bone, 'Text, barnacles, labels', 'What has settled and stays'],
    ['Lumen', EX.lumen, 'Lit slots, veins, readouts, icons', 'The conduit-keepers: power carried where it is needed, no further'], ['Breach', EX.breach, 'Alerts only', 'The hull open to the sea']],
});
})();
