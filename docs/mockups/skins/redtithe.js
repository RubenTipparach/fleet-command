// The Red Tithe skin (mockup 4, OpenSpec change console-skins): a second, more brutal pirate faction (owner,
// 2026-11-07: "Id also like another pirate faction. One more brutal. Decorate their UI with sharp teeth, bullets and
// blades. Blood stained buttons and crusty paper with burn marks and drops of blood and grime on them"; "More brown wood
// on the red tithe"). Name and culture are ours and provisional. The look has one implementation,
// docs/mockups/lib/redtithe.js (window.FCRedTithe, inlined before this module), shared with the battle console
// (openspec/changes/redtithe-battle-console); this module keeps the skins page's own tokens (its CSS, type, title and plate
// placement, world view and palette rows) and registers the skin with this page's registry (docs/mockups/skins).
(function () {
'use strict';
const F = window.FCK, H = window.FCRedTithe, RT = H.RT;

// ---- the skin -----------------------------------------------------------------------------------------------------
F.register('redtithe', {
  name: 'Red Tithe', kind: 'Painted in canvas: crusted, bloodied bounty sheets nailed to scarred oak planks, strap iron and knives, trap-jaw keys, a bandolier for every bar', bg: RT.bg, sampleBg: RT.paper,
  css: { '--s-ink': RT.ink, '--s-dim': '#3b2a1e', '--s-num': '#1c130d', '--s-body': '"Special Elite", monospace', '--btn-ink': '#16100b', '--btn-justify': 'center',
    // a halo of the slip's own paper behind the stencil: invisible on the slip, it keeps a long label legible where a
    // narrow key's label runs out over the jaws' teeth and the stakes (the kit sheet's 120 px key)
    '--btn-shadow': '0 1px 0 rgba(255,240,205,0.35), 0 0 2px #bea77b, 0 0 3px #bea77b, 0 0 5px rgba(190,167,123,0.85)', '--btn-font': '700 17px/1 "Stardos Stencil", sans-serif', '--btn-track': '0.05em', '--btn-case': 'uppercase', '--btn-motion': 'padding-top 0.05s', '--btn-sink': '1.5px',
    '--tab-font': '400 14px/1 "Special Elite", monospace', '--tab-ink': '#d8c69a', '--tab-ink-on': '#1a120c',
    '--title-font': '700 19px/1 "Stardos Stencil", sans-serif', '--title-ink': '#6b1810', '--lbl-font': '400 15px/1 "Special Elite", monospace', '--num-font': '400 25px/1 "Special Elite", monospace',
    // The strips are 72 px; their open band is the sheet inside its torn edge, rows 11-61, so each label stands over its
    // strap with room to spare; the bandolier's middle tiles, so its stitches keep their pitch at any width.
    '--stt-pt': '12px', '--stt-pb': '12px', '--stb-pt': '12px', '--stb-pb': '12px', '--strip-lamp': '44px', '--bt-rep': 'round' },
  accent: RT.brass, warn: RT.fresh,
  // The frame's top margin carries the knife and the teeth, so the title is stencilled at the head of the sheet itself.
  title: (t, m) => { t.style.cssText = 'position:relative;justify-self:start;align-self:flex-start;margin-left:2px;letter-spacing:0.12em;'; },
  // The captain's name is typed into the bounty sheet's field (64-404 x 10-46), clear of the seal and the knife.
  plateText: { left: 64, top: 12, width: 336, height: 30, font: '400 21px/30px "Special Elite", monospace', color: '#1a120c', sub: 'RED LEDGER · CAPT. HESK VARRO' },
  helmKeys: 'ring', helmR: 92, shield: [RT.brass, '#b8561c', RT.fresh],
  fonts: ['700 20px "Stardos Stencil"', '400 20px "Special Elite"'], view: { title: '400 28px "Special Elite"', body: '400 24px "Special Elite"', upper: true, ink: RT.bone, dim: RT.brass },
  // The column's ground: a wall of the hall's planks, drawn once at the column's size (a JPEG: it is all grain, and a PNG of it runs to megabytes).
  build: () => Object.assign(F.fromPainters(H.nine, H.heroSheet, (n) => H.iconSheet(n), false), { column: F.paint(H.GROUND[0], H.GROUND[1], H.ground).toDataURL('image/jpeg', 0.88) }),
  live: H.live,
  world: ['Every crew boarded pays a tithe in blood; every debt is written on a sheet and nailed to the boards of the hall with a knife until it is paid.',
    'Raiders of many peoples; a captain\'s standing is counted in trophies taken from others: teeth of beaten crews, spent cartridges, broken blades, and tallies cut into the boards.',
    'Three traditions: the sheet-keepers\' ledgers and burned seals, the gunners\' bandoliers and brass, the cutters\' blades and traps. They build as they build their halls: oak planks bound with strap iron, steel only where it must bite or fire.',
    'Scarred boarding hulls with ram prows and grapples, hung with the plates of ships they took.'],
  shapes: 'Trap-jaw keys on oak stakes, torn bounty sheets nailed to scarred plank boards bound with strap iron, knives and a hatchet in the wood, strings of teeth, a revolver cylinder in an oak wheel, tally-stick tabs, bandolier bars.',
  light: 'Painted: a hard hall light from the upper left; worn plank edges catch it, gouges, grain and tallies hold shadow, blades and iron glint, deep cast shadows under blades and paper; no glow but the alert lamps.',
  motion: 'A key\'s jaws snap shut and blood seeps along the bite; a bar loads round by round.',
  type: '"Stardos Stencil" stencilled titles and keys, "Special Elite" typed readouts.',
  palette: [['Dried blood', RT.dried, 'Stains, seals, titles', 'The tithe already taken'], ['Fresh blood', RT.fresh, 'Alerts only', 'The tithe being taken now'],
    ['Old paper', RT.paper, 'Sheets, keys, readout grounds', 'The ledger: every debt written down'], ['Hall oak', RT.oak, 'Boards, frames, stakes, tally sticks, the column', 'The boards every sheet is nailed to: the Tithe\'s hall, where debts are posted and paid'],
    ['Brass', RT.brass, 'Rounds, ready states', 'Spent cartridges counted as standing'], ['Gun steel', RT.steel, 'Jaws, blades, strap iron', 'What holds the sheet holds the crew']],
});
})();
