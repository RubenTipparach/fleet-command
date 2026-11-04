// The Ssarith Hunt skin (mockup 4, OpenSpec change console-skins): a reptilian race that hunts for sport, its console a
// hunter's kit carried through a dense jungle (owner, 2026-10-30: "A gurilla style faction, gun metal, beveled corner,
// industrial retro CRT style buttons", then "that faction is from the gorn, which are more hunter like faction, they
// enjoy the sport, so their ui is reminiscent of a dense jungle akin to the Predator"). Rebuilt on the analog panel bible
// (owner, 2026-10-31: "update krai, onbrai, and hunt to use the new guildine standards"): pillowed, cut-cornered olive
// gunmetal (the hero pieces pressed with the hunters' hide scales) over recessed CRT tubes that light themselves, the
// bible's "backlight only" option, since a hunter shows nothing the quarry could see. Its bars are rows of claw marks on
// tiny tubes, lit as they fill (owner: "have different repeated symbols for them"). The kit is modelled, lit and rendered
// in Blender (tools/blender/build_ui_ssarith.py) from the fit-checked shapes of tools/mockups/ssarith_shapes.py and
// inlined into the page; this module adds the skin's tokens and draws only the live readouts, as phosphor traces inside
// the same tubes. Registers itself with the page's skin registry (docs/mockups/skins). Our own design: no other game's or
// film's names, shapes, glyphs or art.
(function () {
'use strict';
// The Hunt's look has one implementation, docs/mockups/lib/ssarith.js (window.FCSsarith, inlined before this module): its
// palette, its icons and its live readouts, shared with the battle console. This module registers it with this page's
// skin registry.
const F = window.FCK, H = window.FCSsarith, SS = H.SS;

// ---- the skin --------------------------------------------------------------------------------------------------
F.register('ssarith', {
  name: 'Ssarith Hunt', kind: 'A hunter\'s kit on the panel bible: hide-scaled olive gunmetal over CRT tubes, claw-mark bars', bg: '#0c120d', sampleBg: '#1c2a1d',
  css: { '--s-ink': SS.bone, '--s-dim': SS.dim, '--s-num': SS.phos, '--s-body': '"VT323", monospace', '--btn-ink': SS.phos, '--btn-shadow': '0 0 2px #000, 0 0 3px #000, 0 0 7px rgba(109,255,138,0.6)', '--btn-justify': 'center',
    '--btn-font': '400 22px/1 "VT323", monospace', '--btn-track': '0.12em', '--btn-case': 'uppercase', '--btn-motion': 'padding-top 0.06s', '--btn-sink': '1.5px',
    '--tab-font': '400 18px/1 "VT323", monospace', '--tab-ink': SS.dim, '--tab-ink-on': SS.phos, '--title-font': '400 15px/1 "Black Ops One", sans-serif', '--title-ink': SS.bone,
    '--lbl-font': '400 20px/1 "VT323", monospace', '--num-font': '400 32px/1 "VT323", monospace' },
  accent: SS.phos, warn: SS.kill,
  // The frame's bezel (20 px) is too thin for a title, so it heads the panel's tube, stencilled in bone.
  title: (t, m) => { t.style.cssText = `position:relative;justify-self:start;align-self:flex-start;margin-left:2px;letter-spacing:0.14em;text-shadow:0 1px 0 rgba(0,0,0,0.8);`; },
  // The captain's name glows in the name plate's tube (54-372 x 9-47), 8 px clear of its ends.
  plateText: { left: 64, top: 13, width: 296, height: 30, font: '400 26px "VT323", monospace', color: SS.phos, sub: 'STALKER VESH-ORR · 14 TAKEN' },
  helmKeys: 'ring', helmR: 94, shield: [SS.phos, SS.amber, SS.kill],
  fonts: ['400 20px "VT323"', '400 16px "Black Ops One"'], view: { title: '400 30px "VT323"', body: '400 26px "VT323"', upper: true, ink: SS.phos, dim: SS.bone },
  build: () => { const d = F.kitData('assets/ui/ssarith/manifest.json'); return d ? F.fromManifest(d, () => F.paint(48, 48, () => {})) : null; },
  live: H.live,
  world: ['The hunt is the only honest sport: the quarry must be worthy, the stalk patient, the kill earned and counted.', 'Big-boned reptiles with heavy clawed hands, slow and silent until they strike.', 'A hunter\'s kit: worn gunmetal, field camouflage, tracking tubes, grown over by the jungle it hides in.', 'Squat armoured hunting ships with broad prows, dark and mottled, built to wait unseen.'],
  shapes: 'Pillowed cut-cornered gunmetal pressed with hide scales, CRT tube keys, fronds on the flat, tally marks, claw-mark bars.', light: 'Backlight only: the tubes light themselves under a dim canopy light; nothing glows that the quarry could see.',
  motion: 'A key\'s tube flares and settles; a tracker sweeps; nothing moves until it strikes.', type: '"Black Ops One" stencilled titles over "VT323" phosphor readouts.',
  palette: [['Gunmetal', '#3a4044', 'Plates, bezels, keys', 'The hunter\'s gear: plain, made to outlast the hunt'], ['Canopy', '#2f4a2a', 'Camouflage, field paint', 'The hunting ground; staying unseen'],
    ['Moss', '#5b6340', 'Camouflage mottling', 'Patience: the hunter becomes the jungle'], ['Phosphor', SS.phos, 'Tubes, readouts, traces', 'The tracker: the quarry in sight'],
    ['Bone', SS.bone, 'Tally marks, trophies, titles', 'The count of worthy kills'], ['Kill red', '#c2412b', 'Alerts only', 'The strike, given once']],
});
})();
