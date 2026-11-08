# Design: exponent-battle-console

## Context

- **The skin.** The Exponent's skin is in `console-skins/design.md` ("The Exponent its own collective, the Red Tithe in
  wood (page version 30)") and on mockup 4. It is painted in canvas, a first pass before Blender: brushed steel and
  grating, rust blooms and runs, salt crust and verdigris, under a layered reef. Every scatter comes from the seeded
  generator (`FCDraw.rng` over `FCDraw.hash`), never a cheap hash (CLAUDE.md section 10).
- **Where it comes from.** The owner's words above. The culture is ours and provisional (survey N2): one collective that
  grows by taking in ships, stations and crews; nothing decorated, everything function; machines run long past any
  refit, and what it absorbs keeps growing on them. Three traditions shape its things: the assimilators (hatches, patch
  plates, modular hulls), the conduit-keepers (sheathed veins with a dim pulse), the reef. Nothing comes from a species'
  body (CLAUDE.md section 1).
- **What the console needs from a skin.** Colour roles, kit pieces by name and optional hooks, as `lib/concord.js` and
  `lib/kelvane.js` give them. The readouts draw inside the panels and over the battle, so every role reads on dark.

## Decisions

### D1. One library, two pages

`docs/mockups/lib/exponent.js` holds the whole look; `skins/exponent.js` keeps the skins page's tokens and registers the
library's painters, column and overgrowth. Mockup 4's three Exponent shots were shot before and after the move and are
pixel-identical.

### D2. Colour roles, lit in the plate's dark wells

Every panel, inset and strip of mockup 4 already holds a dark well (`#091012`) inside its riveted plate, so the console's
light readouts sit there and over the battle alike.

| Role | Colour | Meaning |
|---|---|---|
| Sky | `#8be6dc` lumen | a reading (power carried where it is needed) |
| Text, Title | `#e3dbc6` barnacle bone | prose, a panel's name |
| Lilac | `#9fb0ad` pale steel | a label |
| Accord | `#5fd68e` the condition lamp's green | whole and engaged |
| Caution | `#f0b04a` the work lamp's amber | damaged |
| Alert | `#ff5a36` breach | down, alarms (the hull open to the sea) |
| Ghost | `#56696c` | vacant, disabled (recedes) |

### D3. The window key

Mockup 4's hatch key (a hatch on a two-knuckle hinge, a dog latch, a lit slot) stays the nine-slice key. The console also
needs square keys whose light is their state (the helm, the status boxes, the icon rails), so the assimilators' other
bolted-on part joins the kit: an inspection port. A steel plate, four hex bolts, a recessed window, a crust of barnacles
in one corner. One plate, seven lights in the window:

| State | Light |
|---|---|
| idle | unlit glass |
| hover | lumen |
| on | hot lumen |
| ok | the condition lamp's green |
| warn | amber |
| alert | breach |
| off | dark glass, the corner crusted over |

`key.*` are 48 x 48 (window 9 px in); `keyw.*` are 144 x 48 with 22 px ends, so wide keys keep their bolts. The icon rails
use the square key (`iconKeys`). Allocation cells are small ports lit by their role, welded shut (an X of weld) when
locked. Symbol bars stay mockup 4's barnacles, with a fourth whose aperture glows green for a whole bar.

### D4. The console's own pieces

- **Panel frame.** Mockup 4's frame keeps 32 px margins round a 24 px band; the console's frame (`frame.panel`) is the
  same painter with 28 px margins and a 20 px band, so the panels keep their room. The reef is clipped to the margins, so
  nothing grows over a label (pad 28 px each side).
- **Title placard (`plate.title`).** A stencil plate riveted at its head, the title (and an officer slot's name) in its
  dark slot.
- **Name plate.** The field stops at 362 px and the growth keeps off the Fleet key (370-456 by 6-36); the name in Allerta
  Stencil 15 px.
- **Column backing (`console.backing`).** Mockup 4's column (a bulkhead of riveted plates under reef) at 480 x 1080, with
  the band an expanded panel tiles (x 100-390, y 40-1040) kept to plate and veins: tiled, masses of reef would repeat
  every 290 px and read as a pattern (CLAUDE.md section 10).
- **Strips.** Mockup 4's strips already hold their dark channel from 92 px in; the console uses them at 72 px with the
  content moved into the channel (`stripTop` dy 8, `stripBottom` dy 0).

### D5. Type

"Allerta Stencil" for titles, keys and readouts of 17 px and over; "Chivo Mono" for labels and small numbers (mockup 4's
pair). Measured on the console's own labels they run 1.43 and 1.54 times as wide as Antonio, so they are set at 0.77 and
0.715 times the size asked for: the same line length for both, so a key label rule T shrinks below 17 px (where the face
changes) still fits the room it was measured for.

### D6. Icons

Stencil marks in one ink, solid shapes broken by the bridges a stencil needs: mockup 4's ten and fourteen more (power,
tactical, point defence, fleet, the ship as joined modules, sensors, craft, weapons, warp, engines, reactor, pause, menu,
cargo).

### D7. Names

Placeholders, ours, until the owner sets the Exponent's culture (E1): hulls numbered in the order they were taken in
(Accretion 41, 7, 113), captains named for what they keep (Keeper of Lines, of Hatches, of Salt), ranks by how long a part
has been grown into the whole (Taken, Joined, Grown, Keeper), officers by their function and number (Tiller Nine, Coil
Two).

## Measurements

| Check | Result |
|---|---|
| Mockup 4's Exponent console and kit sheet, before and after the move | pixel-identical |
| Parity, Exponent page against the Concord's | 70 shots, 4,087 presses of 396 controls, every press matched |
| `dom_fit.mjs` (battle, weapons expanded, menu; desktop, phone, portrait) | PASS |
| `console_fit.mjs` on mockup 4 (exponent) | clear |

## Open questions

| Id | Question | Recommendation (provisional) |
|---|---|---|
| E1 | The Exponent's names for ships, ranks and officers (placeholders, D7). | The owner sets the culture (CLAUDE.md section 1; survey N2). |
| E2 | The overgrowth that crosses between pieces on mockup 4 (`overgrow`: cables in the gaps, reef past the column's edge) is not on the battle console. | Add it as an optional console hook once the panels' gaps are measured clear of every readout. |
| E3 | The inspection-port key beside the hatch key (D3). | Keep both: the hatch carries a label, the port a picture of its state. |
