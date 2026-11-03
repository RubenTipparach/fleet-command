# Design: krai-battle-console

## Context

The battle console (mockup 2, change `battle-console`) is drawn in the engine: one canvas, kit pieces composed at their
pixel size, fixed text, immediate-mode input. It was written in the Concord skin, with the Concord's colours, Antonio
and black-on-lit ink written straight into its drawing code (448 colour uses, 64 font strings, 43 bare blacks, 49
rounded rectangles). The owner wants every faction's console to be the same console, each "manually verified such that
all of it is functional and readable" (2026-11-01), and the Krai Warhost first (2026-11-03).

## Decisions

### 1. One source page; a faction's page is written from it

A copy per faction would be a divergent code path (CLAUDE.md section 6.1). The source page reads
`<meta name="fc-skin">` and takes `window.FCSkins[id]`; `tools/mockups/skin_page.py <id>` writes the faction's page,
changing only the meta, the title, the font link and the inlined skin library and kit. A faction gets its own page so
it gets its own artifact and its own approvals in the tracker; the code stays one.

### 2. What a skin gives the console

The same fields from `lib/concord.js` and `lib/krai.js`:

| Field | Concord | Krai |
|---|---|---|
| `CC` colour roles (deep, signal, sky, accord, lilac, caution, alert, text, plus ink, inkDis, night, ghost, void, slate, mute, ver, char, plate, bg, title) | as before | blood keys, forge orange readouts, ash labels, bone text; state green, yellow and red from the Krai kit's own alert lamps |
| `ink(fill)`: the ink that reads on a drawn fill | black | dark on lit fills (luminance over 0.2), bone on blood and steel |
| `keyInk`: ink on rendered keys and tabs by state | black, tab text when idle | bone at rest, dark on the red hover and the orange engaged key |
| `font(spec)`: maps the page's Antonio sizes | unchanged | Teko 500 at 1.1x (600) and Teko 600 at 1.05x (700), measured as wide as Antonio; 700 at 22 px and up in Russo One at 0.75x |
| `rr`, `knob`: drawn keys and round knobs | rounded rectangles, circles | blade-cut (top-left and bottom-right cut, the others nicked), hexagons |
| `pad`: content inside a panel frame | the frame's margins | 26, 20, 20, 22 px, clear of the rim and fangs |
| `plate`: the name plate's text field | as before | Russo One 19 px from x 92 |
| `keyCenter` | no | labels centred, clear of the blade's point |
| `player`: names for the player's side | none | Krai Warhost; Kharz, Vekk, Ruhn (the ids, classes and rules unchanged) |
| `load()`: kit ready | immediate | decodes the inlined Blender renders |

Measured fonts (Chromium, 100 px): Antonio 600 cap 86 px, width of "HULL INTEGRITY 0123456789" 981 px; Teko 500 cap 64
px, 891 px; Russo One cap 70 px, 1484 px. So Teko at 1.1x fills every slot a Concord label fills, at 82 % of the cap
height; Russo One only where there is room.

### 3. Optional kit pieces, used when a kit has them

The page draws a piece only when the skin's kit carries it, so the Concord is unchanged:

| Piece | Kind, slot | What the page does with it |
|---|---|---|
| `plate.label` | 9-slice, any x 18 | every label (lilac ink) sits on a placard, its red pip ahead of the letters |
| `plate.title` | 9-slice, any x 24 | a panel's title on a placard set into the frame's top rail |
| `console.column` | 9-slice, 480 x 1080 | the column behind the panels, and the left two thirds behind an expanded panel |
| `join.bridge` | hero 52 x 24 | two clamps across each gap between stacked panels, on the left half, clear of the titles |
| `join.gusset` | hero 34 x 36 | braces the column's spine to the top strip, and flipped to the bottom strip |
| `bar.cell.on`, `.off`, `.warn` | cells | every bar becomes a row of the skin's symbol (the owner's 2026-10-31 rule) |

### 4. Every Krai piece is rendered in Blender

The owner, 2026-11-03: "you didn't render the stitching elements in blender like you are supposed to". The placards,
the column, the clamps, the gussets and the allocation cells are modelled by `tools/blender/build_ui_krai.py` in the
skin's cast volcanic gunmetal, the shaped ones from outlines in `tools/mockups/krai_shapes.py` that pass rules K, L, R,
P and B (24 of 24 renders pass rule B). Render cost at 48 samples on the cloud CPU: the small pieces 0.2-2.6 s each, the
column (960 x 2160 px) 343 s. The page carries the kit as WebP: the Krai page is 1.3 MiB.

### 5. Verification of function: press every control in both skins

`tools/mockups/skin_parity.mjs` replays each of the 40 named shots, then for every control the Concord registered in
it: replays the shot again, presses the control at its centre (click, or a drag to it), steps the battle 30 steps
(1 s) and records the console state (station, tab, expanded panel, modes, toggles, the scene, the raid picks) and the
battle state (every ship's position, heading, speed, hull, shields, power, systems, weapons, orders, queues, pads,
hold, warp; mines, craft, shots, log length). The faction's page must give the same record press for press; the Concord
is first checked against itself so a difference cannot come from the check. Result: 2,175 presses of 270 distinct
controls in 40 shots, every one identical; 28 presses changed nothing a second later in either skin (toggles that act
later or hover cards). It found one Concord bug (decision 6).

### 6. Concord fixes

- The Fleet key stepped through `own[(own.indexOf(s) + 1) % own.length]` and threw when `own` was empty (the player's
  last ship jumped out). It is now dark and does nothing when no ship is left.
- Named shots now start from a clean console: explosions finished, the scene list and log closed, no hover.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| K1 | The Krai's state colours: green, yellow and red (the Krai kit's own alert lamps), or the skin's first shield ramp (orange, gold, red)? | Green, yellow, red: a status reads the same in every skin; the Krai's orange stays the readouts' and engaged keys' colour |
| K2 | Vector pieces the console draws itself (helm keys, role keys, the warp key, the reactor diagram, the ship schematic) take the Krai's colours and blade shape; should they become rendered pieces too? | Yes, piece by piece as the owner reviews; the kit already decides which pieces a skin renders |
| K3 | The player's ships keep the stand-in Concord hulls in the 3D view | Kept until mockup 3 (look dev) gives the Krai their own ships |
