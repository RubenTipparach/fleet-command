# Design: aelith-battle-console

## Context

- **The skin.** The Aelith Conclave's skin is in `console-skins/design.md` ("Aelith Conclave", version 14 on the analog
  panel bible) and on mockup 4. It is the one hybrid skin: every piece's shell is satin gold and pearl, modelled and
  rendered in Blender (`tools/blender/build_ui_aelith.py`) from the fit-checked shapes of `tools/mockups/aelith_shapes.py`;
  the holograms in its wells (lozenges of light, fields with scanlines and a split-colour edge, crystals) are drawn as
  vectors from the same shapes, clipped to the wells.
- **Where it comes from.** The owner, 2026-10-30: "an wise and advanced alien race. holographic buttons, with organic
  shaped metal armor peices surrounding the edges"; "the conclave is one exception. They have a hybrid of both vector
  graphics for holographic buttons and icons, but 3d rendered shell around the UI panels and elements". The tradition is
  patient knowledge kept over aeons: light shaped by thought, held in sculpted shells. Nothing here comes from what the
  Aelith look like (CLAUDE.md section 1): the lozenges, triangles and crystals are the shapes their makers build with.
- **What the console needs from a skin.** The battle console draws its whole interface live: colour roles (`CC`), kit
  pieces by name, rendered keys whose light is their state, and type mapped from its Antonio sizes. The roles draw both
  inside the panels and over the 3D battle, so every role must read on a dark ground.

## Decisions

### D1. One library, both pages

`docs/mockups/lib/aelith.js` (`window.FCAelith`, registered as `FCSkins.aelith`) holds the palette, the holograms
(`holo`, `field`, the per-piece `HOLO` table, the pressed key's push-in and sheen), their layering over each shell
(`layered`, the SVG image the skins page slots take), the bar symbols, the 24 icons and the live readouts. The skins
page's module keeps only its own tokens (CSS, title placement, the plate text) and registers the skin; before this change
it held all of the above. CLAUDE.md section 6.1.

### D2. The wells are veiled for the console's readouts

**The problem.** Mockup 4's frame wells are lit voids with a faint hologram field and scanlines; a readout in holo cyan on
the bare lit void measured about 3 to 1. The console's frames are also stretched many times their height (an expanded
panel is 1040 px tall against the piece's 140), so drawn scanlines would stretch into bands.

**The decision.** For the console only (`HOLO_CONSOLE`), the frame, inset and strip wells, the name plate's window and the
dial's scope are veiled: the void darkened (`#04141a` at 55-72 %), no scanlines, only the hologram's glowing split-colour
edge kept at the rim. Mockup 4's pieces are unchanged.

| Considered | Why not |
|---|---|
| The skins page's fields as they are | 3 to 1 for a reading; scanlines stretch into bands on the expanded panel |
| A re-rendered "screen" shell, as the Kelvane's `frame.screen` | The well is already a void; the darkness is the hologram's, so it belongs to the vector half of the hybrid |

### D3. The console's own pieces, composed

No new shell is modelled for them; each is a render already in the kit with the Conclave's light drawn into its well:

| Piece | Shell | Light |
|---|---|---|
| `key.*` (48 x 48) | the dial's gold ring at 0.4 scale (its well a disc of radius 19) | idle a faint field, hover brighter, on solid holo, ok solid mint, warn solid amber, alert solid coral, off almost dark |
| `keyw.*` (150 x 48, margins 0, 42, 0, 42) | `button.idle`, the key's gold claws and pearls | the same lights in its lozenge |
| `cell.*` (32 x 28) | none: lozenges of light alone, as the bar symbols are | idle faint, lit holo, warn coral, accord mint, locked a dashed outline |
| `plate.title` (120 x 24) | `tab.idle`'s shell rows 4-36, drawn 24 px tall | its window veiled, the title in bright holo |
| name plate | the plate drawn shorter: its window's straight middle (x 120-340) taken in to 126 px, so it ends at 366 and the Fleet key (370-456) stands beside it | its window veiled |
| `bar.fill.accord`, `bar.cell.ok` | none | mint, the lozenge symbol's whole |

A lit key is light made nearly solid (`solid`: 70-98 % of the colour, still with scanlines), so dark ink reads on it.

### D4. Colour roles

Measured against the veiled well (`#0d1f26`, sampled from the battle shot; WCAG contrast ratio):

| Role | Colour | Contrast | Meaning |
|---|---|---|---|
| Sky | `#6ff3ff` | 12.8 | a reading, a hover: thought made visible |
| Lilac | `#9fc9cf` | 9.5 | a label, sea-glass print |
| Text, Title | `#dffcff` | 15.7 | prose and titles |
| Accord | `#8affc8` | 13.9 | whole and engaged (mint, kept clear of the reading's cyan) |
| Caution | `#ffd27a` | 11.9 | damaged |
| Alert | `#ff6a5a` | 6.0 | down, alarms: coral, "a rare disturbance" |
| Ghost | `#3f6a72` | 2.8 | vacant, disabled (meant to recede) |

### D5. Type

"Exo 2" (weight 600) sets every readout and label at 0.84 times the console's Antonio size; "Michroma" sets the titles
(Antonio 700 at 20 px or more) at 0.62 times. Both are mockup 4's pair. Rule T (the keys' and tabs' fit) shrinks what
still runs long.

### D6. Icons

All 24 subjects of the icon guide in the Conclave's sacred geometry: rings, triangles, lozenges and points, the shapes
its keys are made of; even 2.6 px strokes with a soft glow; a solid crystal where the thing is solid; dots for what is
given off. Mockup 4's ten icons were 1.6 px hairlines drawn by the same code; they are now these.

### D7. The backing

`console.backing` (480 x 1080) is one pillowed gold shell, roughness 0.5 with a thin coat so so large a plate does not
mirror the softbox as one bright patch, with a pearl on the centre line in each 6 px gap where two of the column's panels
part (rows 65, 491, 773 and 939). No well, so no hologram. Fit rules K, L and B pass. The expanded panel tiles its band
(x 100-390), as every metal skin's.

### D8. Names (placeholders)

Ships named for a stillness of the world (Serene Tide, the skins page's, with Long Dawn and Quiet Lattice), their captains
Elders, the ranks Listener, Keeper, Warden and Elder, thirteen officers' names of our own. Open question A1.

## Measurements

| Check | Result |
|---|---|
| Fit rules on `console.backing` | 5 of 5 pass; every other piece's shapes byte-identical |
| Parity, Aelith page against the Concord's | see tasks 1.5 and the commit message |
| `console_fit.mjs` on the skins page, the Aelith skin | see tasks 1.5 |
| `dom_fit.mjs` on the Aelith page | see tasks 1.5 |
| Render time | `console.backing` 131 s at 48 samples on the cloud session's CPU |

## Open questions

| Id | Question | Recommendation (provisional) |
|---|---|---|
| A1 | The Conclave's names for ships, ranks and officers (D8). | The owner sets the Conclave's culture (CLAUDE.md section 1). Until then the placeholders stand, marked as such. |
| A2 | Veiled wells in the console, not mockup 4's lit voids (D2). | Veiled: the readouts must read, and the veil is the hologram's own half of the hybrid. |
| A3 | Keys, cells and placards composed from existing renders, not modelled as new shells as the Kelvane's were (D3). | Composed for the mockup; model dedicated shells if the owner approves the look for build. |
