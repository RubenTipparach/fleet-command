# Design: axiom-battle-console

## Context

- **The skin.** The Axiom's skin is in `console-skins/design.md` ("Three more factions (page version 29)") and on mockup 4.
  It is flat and exact, drawn by the mathematics in canvas with no painted material or depth (CLAUDE.md section 7:
  vector for flat styles): thin lines and filled regions on the void, the depth of an iteration shown as its colour,
  indigo for shallow and cyan for deep. There is no randomness at all; every shape is an explicit rule.
- **Where it comes from.** The owner, 2026-11-07: "Id like a faction that is purely mathematical. A faction based on
  fractal patterns and designs." The culture is ours and provisional (survey N1-N6): every true thing is a rule and its
  iteration, and a structure is honoured by how deep its rule has been carried. Three schools own parts of the console:
  the escape-time school the screens, the tilers the frames and plates, the branchers the icons and marks. Nothing comes
  from the Axiom's bodies, which are not described (CLAUDE.md section 1).
- **What the console needs from a skin.** The battle console (`docs/mockups/battle-console.html`) draws its whole
  interface live from colour roles (`CC`), kit pieces by name and a few optional hooks (`docs/mockups/lib/concord.js` is
  the simplest model). Its readouts draw both inside the panels and over the battle, so every role must read on a dark
  ground.

## Decisions

### D1. One library, two pages

`docs/mockups/lib/axiom.js` holds the whole look. The skins page's `skins/axiom.js` keeps only that page's tokens (its CSS,
type, title and plate placement, world view and palette rows) and registers the library's painters. Mockup 4's three
Axiom shots (the console and the kit sheet) were shot before and after the move and are pixel-identical.

### D2. Colour roles

The panels' plate is `#080a18` on the void `#05060c`. The roles keep the palette's own meanings:

| Role | Colour | Meaning |
|---|---|---|
| Sky | `#3fe0e6` iteration cyan | a reading (a rule carried deep) |
| Text | `#e8ecf6` proof white | prose (stated truth) |
| Lilac | `#a3afe6` pale indigo | a label |
| Title | `#3fe0e6` | a panel's name |
| Accord | `#f2c14e` convergence gold | whole and engaged (what converges is true; ready) |
| Caution | `#ff9d3a` bound amber | damaged (provisional, A2) |
| Alert | `#ff3d8b` divergence magenta | down, alarms (what escapes) |
| Ghost | `#5b619a` | vacant, disabled (recedes) |

The six palette colours give no warm colour for "damaged" between gold (whole) and magenta (down); a bound amber, a rule
near the bound it must not cross, fills it until the owner says otherwise (A2).

### D3. Keys are the Koch key's family

- **Nine-slice keys** are mockup 4's Koch keys: an elongated hexagon with Koch-edged ends, one level deeper on hover,
  solid indigo inside a gold outline when pressed. Labels sit centred in the plain middle (`keyCenter`).
- **Drawn keys** (the helm's square keys, the status boxes, the expand key) are flat fills, as the Concord's are, in the
  Koch key's hexagon (`rr`): its ends pointed by a fifth of its height. The expand key is a hexagon point up (`knob`).
- **Allocation cells** are the same hexagon: outlined at rest, solid in the role's colour when lit, dashed when locked.
- **Symbol bars** are mockup 4's Sierpinski carpet cells, with a gold one for a whole bar.

### D4. Type

"Space Mono" sets every readout and key, "Martian Mono" a panel's title (mockup 4's pair). Measured on the console's own
labels, Space Mono runs 1.57 times as wide as Antonio and Martian Mono 1.79, so they are set at 0.7 and 0.62 times the
size asked for, keeping a line about 1.1 times its Antonio length; rule T shrinks what still runs long, never under
10 px.

### D5. The console's own pieces

- **Strips.** Mockup 4's strips carry their depth ladders (the Cantor set and the Koch curve iterated row by row) 116 px
  in from each end; the console lays its readouts from 96 px in, so the console's strips draw the ladders inside 92 px.
  Both strips are 72 px, the content moved down into the open band (`stripTop` dy 10, `stripBottom` dy 4).
- **Name plate.** Mockup 4's Koch key runs the plate's whole width; the console's stops at 363 px so the Fleet key
  (370-456 by 6-36) has its own end. The name is set in Space Mono 16 px in the plain middle (52-320).
- **No column ground.** The Axiom is a flat skin, drawn on the void as the Concord's is on black (CLAUDE.md section 7:
  black stays only for flat skins).

### D6. Icons

The ten icons mockup 4 had (helm, fire, shields, tractor, transport, science, comms, repair, map, settings) and fourteen
more, so the console finds every subject of the icon guide: power (a bolt in a Cantor-cut ring), tactical (a turret with
Cantor-cut barrels), point defence (a shield with a Koch star), fleet (three chevrons, the wing smaller by the golden
ratio), the ship (a Koch key on end with two nacelles), sensors, craft (a Sierpinski triangle of one level), weapons (a
Cantor-cut bolt bursting as a Koch star), warp (speed lines each the last over the golden ratio), engines, reactor, pause,
menu and cargo (a carpet of crates). One ink, glowing a little when it is bright; dark on a lit key with the glow off.

### D7. Names

Placeholders, ours, until the owner sets the Axiom's culture (A1): ships named for what iteration finds (Fixed Point,
Attractor, Limit Cycle), captains called Iterant, ranks by how far a statement has been carried (Conjecture, Lemma,
Theorem, Canon), and thirteen officers' names.

## Measurements

| Check | Result |
|---|---|
| Mockup 4's Axiom console and kit sheet, before and after the move | pixel-identical |
| Parity, Axiom page against the Concord's | 70 shots, 4,087 presses of 396 controls, every press matched |
| `dom_fit.mjs` (battle, weapons expanded, menu; desktop, phone, portrait) | PASS |
| `console_fit.mjs` on mockup 4 (axiom) | clear |

## Open questions

| Id | Question | Recommendation (provisional) |
|---|---|---|
| A1 | The Axiom's names for ships, ranks and officers (placeholders: Fixed Point, Attractor, Limit Cycle; Iterant; Conjecture, Lemma, Theorem, Canon). | The owner sets the Axiom's culture (CLAUDE.md section 1; survey N1-N6). Until then the placeholders stand, marked as such. |
| A2 | "Damaged" in bound amber `#ff9d3a`, a seventh colour beside the palette's six (D2). | Keep it: gold is whole, magenta is down, and damaged must be warm and different from both. |
| A3 | Drawn keys as flat fills in the Koch key's hexagon, not rendered glass keys (D3). | Flat fills: the Axiom is a flat skin, as the Concord is. |
