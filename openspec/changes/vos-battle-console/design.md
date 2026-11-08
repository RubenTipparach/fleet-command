# Design: vos-battle-console

## Context

- **The skin.** The Vos Syndicate's skin is in `console-skins/design.md` ("Vos Syndicate: hand-drawn") and on mockup 4:
  wobbly ink outlines over stained washes, hatching on the shadow side, dry-brush highlights, spirals in the corners,
  lopsided claw-tag keys, a ship's wheel for the helm and a gauge drawn as an eye. Every stroke comes from a seeded
  generator (`FCDraw.rng`, Mulberry32), so a piece is drawn the same way every time.
- **Where it comes from.**
  - The owner, 2026-10-30: "I really like espeically the mirak ui design, its so playful and pirate like"; "design a hand
    drawn aesthetic of the mirak ui". We take the shape of a crew-painted console, never the reference game's art.
  - The provisional account (survey U7): free clans of raiders, luck, loot and loyalty to the crew; salvage patched and
    painted over. The tradition the design draws on is salvage and crew marks.
  - Nothing in this design comes from what the Vos look like (CLAUDE.md section 1). The skins page's "Bodies" row is
    still to be reviewed with the owner (console-skins design, rule 3).
- **What the console needs from a skin.** The battle console (`docs/mockups/battle-console.html`) draws its whole
  interface live from a skin's fields (the ones `lib/concord.js` and `lib/kelvane.js` expose): colour roles, kit pieces
  by name, the square keys whose light is their state, the type and the live readouts. The roles also draw over the 3D
  battle, so every role must read on a dark ground.

## Decisions

### D1. One library for both pages

The Vos code lived inside `console-skins.html`'s own script, and other modules use its helpers through `window.FCK`.
`lib/vos.js` now holds all of it, moved as it was (the same seeds, so every piece mockup 4 draws is drawn the same), and
the skins page reads it: `FCVos.draw` gives the page its `FCK` helpers, `FCVos.painters` its kit, `FCVos.live` its
readouts. The library is inlined before the skins page's main script, since that script builds its registry from it.

### D2. Colour roles on the dark wash

| Role | Colour | Meaning |
|---|---|---|
| Sky | `#7fdcc8` | a reading: pale verdigris, the palette's "lenses, readouts" |
| Accord | `#3fb79a` | whole and engaged: the old coin's verdigris, the spoils kept |
| Caution | `#e8b33a` | damaged: gold (the skins page's shield ramp) |
| Alert | `#e23a48` | down, alarms: blood, a warning once |
| Lilac | `#cfae8c` | a label: faded tan ink |
| Text | `#efe0bd` | prose: bone |
| Title | `#efc56a` | a panel's name: brass |
| Signal | `#6a3422` | a drawn key at rest: salvage rust |

The ink on a fill is tattoo ink (`#2a1410`) on a washed colour and bone on a dark one.

### D3. Keys are tags

- **Square keys (`key.*`, 48 x 48)**: a square tag with an eyelet in its upper left corner, inked by hand. The state is
  the wash inside the ink; the strokes are the same in every state (one seed), so lighting a key changes only its colour:
  dark wine at rest (brass ink round it), pale verdigris on hover, verdigris engaged and whole, gold damaged, blood down,
  near black off.
- **Wide keys (`keyw.*`, 144 x 48, margins 0, 40, 0, 26)**: the kit's claw tag in the same seven lights.
- **Allocation cells (`cell.*`, 32 x 28, margins 10)**: a daub of paint inside an inked box; an empty one inked in faded
  brass so it shows on the dark wash; a locked one hatched.
- No other skin has a tag (CLAUDE.md section 7, rule 1). Drawn keys and knobs (`rr`, `knob`) are lopsided: corners cut
  unevenly, a knob a little out of round, fixed per size so nothing wobbles from frame to frame.

### D4. Title ticket and the ground behind the column

- **Title ticket (`plate.title`, 140 x 24)**: a dark wine ticket torn at both ends and inked in brass, so the brass
  title and the officer's bone name read on it.
- **The column's ground (`console.backing`, 480 x 1080)**: patched sailcloth, stained dark, its patches stitched on in
  ink, a crew's spiral painted faint in one corner. The Vos skin is drawn, not metal, but its column still gets a ground
  of its own in its language rather than black.

### D5. Type

Permanent Marker for headings (700 weight at 22 px and up, set at 0.72 of the size: measured 1.48 times as wide as
Antonio) and the name plate; Patrick Hand for everything else, at the size asked (1.09 times as wide). Rule T (the keys'
and tabs' fit) shrinks what still runs long.

### D6. Icons

All 24 subjects of the icon guide (`docs/design/icon-guide.html`), drawn in the crew's brush: crooked tapering ink,
solid where the thing is solid, single strokes for what it gives off; a spiral where a crew would sign it. The skins page
draws its ten from the same function, so a subject has one picture everywhere. Where mockup 4 had used a mark for a
subject that is not a picture of the job (three claw scratches for Fire, an X for Map), it now draws the guide's subject.

## Measurements

| Check | Result |
|---|---|
| Parity, Vos page against the Concord's (`skin_parity.mjs`) | 70 shots, 4,087 presses of 396 controls, every press matched |
| Console fit on mockup 4 (`console_fit.mjs`, vos) | clear |
| DOM fit on the page (`dom_fit.mjs`), every shot at desktop, phone and portrait | PASS |

## Open questions

| Id | Question | Recommendation (provisional) |
|---|---|---|
| V1 | The Syndicate's names for ships, ranks and officers. Placeholders: ships named as a crew names its luck and debts (Grinning Debt, Lucky Rake, Second Helping), captains called Mother and Father by their crews, the ranks Deckhand, Bosun, Mate, Quartermaster. | The owner sets the Syndicate's culture (CLAUDE.md section 1). Until then the placeholders stand, marked as such. |
| V2 | Square tag keys dark at rest and washed with colour when lit (D3), like the rendered kits' keys, rather than brass tags at rest like mockup 4's buttons. | Dark at rest: a lit key must stand out from the rest, and the rails' icons read on the dark wash. |
| V3 | Patched sailcloth behind the column (D4) rather than the skins page's plain dark ground. | Sailcloth: the column gets a ground in the skin's language, as every other skin's has. |
| V4 | Mockup 4's ten icons replaced by the icon guide's subjects (D6). | Replace: one subject, one picture everywhere. |
