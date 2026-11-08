# Design: vhorr-battle-console

## Context

- **The skin.** The Vhorr Brood's skin is in `console-skins/design.md` ("Vhorr Brood") and on mockup 4, built on the
  Organic Panel Bible (`docs/design/organic-bible.html`, the `blender-organic-panels` skill): one surface sculpted in
  layers (bone, muscle, leather stretched over both, veins and sacs of light under the skin), black smoked-glass screens
  with bronze rims, dark key caps with lit rims, panels floating over the game with no backdrop, the column behind them
  one grown body of tissue. Every slot is sculpted at its exact size by the parametric sculptors
  (`tools/blender/organic_components.py`, run by `tools/blender/build_ui_vhorr_organic.py`); there is no nine-slice
  (CLAUDE.md section 14).
- **Where it comes from.** The owner, 2026-10-30: "an organic alien race, lots of leather shining through light, slimy
  carapace, tendrils"; 2026-10-31: "the alien hive mind aesthetic". The tradition is the brood as one body: its consoles
  are grown, not built. Nothing here comes from what the Vhorr look like (CLAUDE.md section 1): the grown body is how the
  brood makes and thinks of a ship.
- **The problem.** Mockup 4's sculpts are at that page's slots (a helm pad, a helm readout, a ship display, a weapons
  panel, measured from its DOM). The battle console's column is three panels of 468 x 420, 276 and 160 px, its strips
  1440 px long, its name plate shorter for the Fleet key, and it draws keys, tabs, insets and cells at many sizes.

## Decisions

### D1. A blockout of the battle console's own slots

`tools/mockups/vhorr_battle_blockout.py` writes the console's slots from its own rectangles (`rects()` and the `ui*`
functions of `battle-console.html`), in three layouts sculpted apart, so no layout carves keep-outs into another's flesh:

| Layout | Slots | Rendered to |
|---|---|---|
| `battle` | the name plate (356 x 56), the station, ship and weapons panels (468 wide), the radar's pocket (120 x 120), the crest (72 x 72), the two strips (1440 x 72); and the tissue backing under the column | `assets/ui/vhorr/organic/battle/column/` |
| `expanded` | the expanded panel's frame (1268 x 1040) | `.../battle/expanded/` |
| `tray` | a tray the console never shows, holding a key (150 x 48: idle, hover, pressed), a square key (48 x 48, the same states), a tab (120 x 40: idle, active), an inset (180 x 100) with its screen, a divider cord (160 x 10) and the alert lamp (40 x 40: green, yellow, red) | `.../battle/tray/` |

Each panel is a body of flesh round **one screen** for everything the console draws in it (the panel less 20-22 px each
side), with its titles kept clear on its rim (the station's officer slot and title; the ship's and weapons' titles on the
gap above them, where the console centres them under a column). Each strip is a spine round one glass channel for its
readouts, 92 px in from each end. The fit (rule G1, growth at least 2 mm clear of every protected rectangle) is measured
before rendering (`--fit-only`): the battle layout's least clearance is 2.5 mm, the tray's 3.5 mm.

### D2. The column is one grown body

`lib/vhorr.js` composes `console.column` (480 x 1080) from the backing and each column slot's render at its rectangle
grown by its bleed. With a column, the console draws no frame of its own for the station, ship and weapons panels (the
Krai's way), so the column's sculpts are drawn whole at their sculpted size: never sliced. The console's own draws of the
name plate, the radar's pocket and the crest are empty, because the column holds them.

### D3. Styles

The owner's survey W picks (2026-11-01), carried to the console's slots (`styles.json`): the station, weapons and expanded
panels in W4's wrap, the ship panel (which holds the ship display) in W2's hang, the radar's pocket and crest in W5's wrap,
the name plate plain (W6), larva keys and wart small keys (W7), the tissue backing (W8). The strips are ridge, not W1's
mixed: the console draws a strip inside its own rectangle, where mixed's crown of tentacles would be cut off (V2).

### D4. What is still stretched (V1)

The console draws some pieces at sizes the sculpt was not made at:
- the strips on a screen wider than 1920 x 1080 (a phone in landscape), and the expanded frame on any other screen shape;
- the tray's keys, tabs, insets and cells, which the console draws at many widths (a 70 px key, a 400 px tab row, a
  hover card).

These go through the console's `nine()` with their ends and rims held, so off the reference size they are stretched,
which the bible forbids. At the desktop reference the column, strips and expanded frame are exact. Sculpting every size
the console draws is a re-run of the same sculptors per size (the bible's "any size"), at a cost of render time and page
weight; that is the owner's call (V1).

### D5. Colour roles

The readouts glow on black glass. Roles: Sky `#b8f27a` (the green of life through hide, a reading), Lilac `#c9a48c`
(a warm hide print, labels), Text and Title `#f3e6d0` (bone), Accord `#c99bff` (the heart's violet, the brood's will:
whole and engaged, kept clear of the reading's green), Caution `#f0b43c`, Alert `#ff5040` (a wound; brighter than mockup
4's blood so it reads on black). Key states the sculpt has no render for (ok, warn, alert, lit cells) are the pressed
cap with the state's light glowing through it, as a sac glows through the skin.

### D6. Type and icons

"Barlow Condensed" (600) at the console's Antonio size for every readout and label; "Metamorphous" at 0.8 times for
titles; mockup 4's pair. All 24 icons in the brood's hand: pods, talons, tendrils, spores and rings of membrane, every
line swelling and tapering as flesh does. Mockup 4 now draws these too, in place of its ten version 23 icon renders (one
picture per subject everywhere). The bar's track, fill and symbols stay the version 23 renders, as on mockup 4.

### D7. Names (placeholders)

Ships named as living things of the brood (Deep Root, Spore Tide, Hollow Bloom), their captains Matriarchs (Matriarch
Vhaal, mockup 4's), the ranks Drone, Tender, Warden and Matriarch, thirteen officers' names of our own. Open question V3.

## Measurements

| Check | Result |
|---|---|
| Rule G1 on the battle blockout (`--fit-only`) | least clearance 2.5 mm (battle), 3.5 mm (tray), 3.5 mm (expanded) |
| Parity, Vhorr page against the Concord's | 70 shots, 4,087 presses of 396 controls, every press matched |
| `console_fit.mjs` on the skins page, the Vhorr skin (icons, arrow and bracket from the lib) | clear |
| `dom_fit.mjs` on the Vhorr page | PASS, every shot at desktop, phone and portrait |
| Render time | the expanded frame 167 s, the tray 752 s and its states 20-76 s each, the column's backing 619 s, the column's panels and strips about an hour, all at 24 samples (16 for the expanded frame) on a CPU shared with other sessions |
| Page weight | the Vhorr kit adds about 1.3 MB to every battle console page (WebP) |

## Open questions

| Id | Question | Recommendation (provisional) |
|---|---|---|
| V1 | Sculpt every size the console draws its keys, tabs, insets and cells at, and the strips and expanded frame per screen shape, instead of stretching the tray's pieces (D4). | For the mockup, the tray's pieces stretched; per-size sculpts if the owner approves the look for build, as a list of sizes measured from the console. |
| V2 | The strips in ridge, not the mixed style the owner picked for mockup 4 (D3). | Ridge, until the console can draw a strip's growth past its rectangle. |
| V3 | The Brood's names for ships, ranks and officers (D7). | The owner sets the Brood's culture (CLAUDE.md section 1). |
