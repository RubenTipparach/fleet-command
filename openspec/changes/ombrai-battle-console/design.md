# Design: ombrai-battle-console

## Context

- **The skin.** The Ombrai's skin is in `console-skins/design.md` ("Ombrai", rebuilt on the analog panel bible) and on
  mockup 4: pillowed anodised plum metal with a thin-film sheen, lilac bezels, pink eye jewels, formed wells in the
  Ombrai's own shapes (crescent-bitten keys, petal wells round the helm's pupil, frame wells bitten like a waning moon),
  each lit from below so its top stays veiled. Built by `tools/blender/build_ui_ombrai.py` from the fit-checked shapes of
  `tools/mockups/ombrai_shapes.py`.
- **Where it comes from.** The owner, 2026-10-30: "an enigmatic alien race with cool dark purple colors and pink
  buttons, progress bars are curcular, almost organic like eyes". The tradition is hidden purpose and watching: they
  watch and are seldom seen, so their instruments are veiled from above and lit from below, and every gauge is an eye
  that looks back. Nothing here comes from what the Ombrai look like (CLAUDE.md section 1).
- **What the console needs from a skin.** As for every faction: colour roles, kit pieces by name, keys whose light is
  their state, type mapped from the console's Antonio sizes, every role readable on a dark ground.

## Decisions

### D1. One library, both pages

`docs/mockups/lib/ombrai.js` (`window.FCOmbrai`, registered as `FCSkins.ombrai`) holds the palette, the icons and the
live readouts (the comets on the helm, the petal ship and its iris shields, the iris radar, the crescent target bracket).
The skins page's module keeps only its tokens and registers the skin. CLAUDE.md section 6.1.

### D2. The wells are veiled from above for the console's readouts

Pink on the bare lit plum well measured about 2.5 to 1. For the console only, the frame, inset and strip wells, the name
plate's field and the dial's well are veiled: the void (`#120822`) laid over the well at 72 % at its top, easing to 40 %
at its foot, where the Ombrai's light rises. This is the skin's own lighting rule (lit from below, its top veiled) carried
into the console; mockup 4's pieces are unchanged. No lines are drawn, so a stretched frame shows no bands.

### D3. The console's own pieces, composed

| Piece | Shell | Light |
|---|---|---|
| `key.*` (48 x 48) | the dial's plum ring at 0.4 scale (its well a disc of radius 19) | lit from below: idle faint pink, hover brighter, on solid pink, ok solid moon, warn solid amber, alert solid crimson, off dark; a veil over the top of each |
| `keyw.*` (120 x 40, margins 0, 24, 0, 24) | `tab.idle`, the tab's capsule, even at both ends, so a centred label is never under the key's moon jewel (the first try used `button.idle`, whose jewel covered the C of "CW") | the same lights |
| `cell.*` (32 x 28) | `frame.inset`'s small plate | idle faint, lit pink, warn crimson, accord moon; locked the plate dimmed |
| `plate.title` (120 x 24) | `tab.idle`'s plate rows 4-36 drawn 24 px tall | its well veiled, the title in lilac |
| name plate | the plate drawn shorter: its field's straight middle (x 140-380) taken in to 146 px, so it ends at 366 and the Fleet key stands beside it | its field veiled |
| `bar.fill.accord`, `bar.cell.ok` | the rendered fill and lit eye cell | their light turned to the moon colour |

### D4. Colour roles

Measured against the veiled well (`#170e26`, sampled from the battle shot; WCAG contrast ratio):

| Role | Colour | Contrast | Meaning |
|---|---|---|---|
| Sky | `#ff4fb0` | 6.2 | a reading, a hover: the watching gaze |
| Lilac | `#b9a0d6` | 8.0 | a label: memory |
| Text | `#f4e6ff` | 15.6 | prose |
| Title | `#c58cff` | 7.6 | a panel's name |
| Accord | `#9ff2e0` | 14.4 | whole and engaged: a pale moon, an eye wide open in calm |
| Caution | `#ffd36a` | 13.1 | damaged (the skins page's middling shield) |
| Alert | `#ff2f5a` | 5.2 | down, alarms: a glance that turns hostile |
| Ghost | `#5a4a72` | 2.4 | vacant, disabled (meant to recede) |

The moon for Accord is new: mockup 4 has no colour for "whole". It is kept far from the pink so a status is never read as
a reading.

### D5. Type

"Quicksand" (weight 700) sets every readout and label at 0.76 times the console's Antonio size; "Syncopate" sets the
titles at 0.58 times. Both are mockup 4's pair.

### D6. Icons

All 24 subjects in the Ombrai's hand: eyes, moons, rings, petals and dots, nothing straight if a curve will do; 2.6 px
strokes with a soft glow, solids where the thing is solid. Mockup 4's ten icons were drawn by the same code at 2 px; they
are now these. The page's drawn keys (`rr`) have fully round ends and its knobs are round.

### D7. The backing

`console.backing` (480 x 1080) is one pillowed plum plate, roughness 0.5 with a light coat, with a pink eye jewel on the
centre line in each gap where two of the column's panels part (rows 65, 491, 773 and 939): the watchers between the
instruments. Fit rules K, L and B pass.

### D8. Names (placeholders)

Ships named for what is hidden or seen (Veil of Ish, the skins page's, with Lantern Below and Quiet Lid), their captains
Watchers, the ranks Glance, Gaze, Watcher and Far-seer, thirteen officers' names of our own. Open question O1.

## Measurements

| Check | Result |
|---|---|
| Fit rules on `console.backing` | 5 of 5 pass; every other piece's shapes byte-identical |
| Render time | `console.backing` 178 s at 48 samples on the cloud session's CPU |
| Parity, Ombrai page against the Concord's | 70 shots, 4,087 presses of 396 controls, every press matched |
| `console_fit.mjs` on the skins page, the Ombrai skin | clear |
| `dom_fit.mjs` on the Ombrai page | PASS, every shot at desktop, phone and portrait |

## Open questions

| Id | Question | Recommendation (provisional) |
|---|---|---|
| O1 | The Ombrai's names for ships, ranks and officers (D8). | The owner sets the Ombrai's culture (CLAUDE.md section 1). Until then the placeholders stand. |
| O2 | A pale moon as the colour of whole and engaged (D4), which mockup 4 lacks. | Keep: pink must stay the reading. |
| O3 | Keys, cells and placards composed from existing renders, not modelled as new pieces (D3). | Composed for the mockup; model dedicated pieces if the owner approves the look for build. |
