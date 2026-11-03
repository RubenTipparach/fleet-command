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
| `console.column` | hero 480 x 1080 | the whole column: one plate with the station, ship and weapons panels formed into it as joined wells; a docked panel then draws no frame of its own |
| `strip.top.join`, `strip.bottom.join` | 9-slice, any x 80 | the strips, their metal running on into the column; with `stripH` 80 the strips' 56 px of content is centred in the open channel |
| `bar.cell.on`, `.off`, `.warn` | cells | every bar becomes a row of the skin's symbol (the owner's 2026-10-31 rule) |

### 4. The column flows as one console

The owner, 2026-11-03, of the first pass (separate frames held by rendered clamps): "One of they key principals is to make
the UI flow well. These more metallic interfaces should use some connected shapes across panels", and "Some of the
little clamps also obscure the text labels on the panels". The clamps are gone. The column is one plate: the three
docked panels are wells formed into it at their exact rectangles (inset 13 px, 16 on the right), each with the frame's
fangs at its corners, neighbouring wells joined through a waisted neck (x 92-184) so the cavities run into each other, a
channel of red light down the right land (x 465-470, y 92-988) crossing every panel, enamel blades and pyramids on the
bands between the wells, and flanges past x 480 at the strips' rows so the plate has no rolled edge where it meets
them. The strips' plates likewise run off their left edge into the column.

The bottom strip did not hold its row (owner: "Some of the elements on the bottom bar like red alerts energy, battery,
warp button don't fit in the container"): the Krai strip art is 72 px with a 45 px channel, squeezed to the page's
56 px. The joined strips are rendered at 80 px with a 60 px channel (rows 10-70), and the page centres its 56 px of
strip content in a taller strip (`SDY`), so the alert lamps, the power and systems ticks, the battery and the warp key
all sit inside it. The 3D view is 48 px shorter in the Krai.

### 5. Closed gaps and rendered icon keys (version 3)

The owner, 2026-11-03, on version 2: "I dont like the gaps between panels, I think that gap should close", and "the
buttons with icons need work. we need to come up with a much more immersive way to in corporate those, make custom
design for this skin. Just changing the color to red or leaving it green doesnt work. I love the buttons you did for
chaning the panels. repeating that design style is prefered".

- The column's wells now run to 1 px of their panels' rectangles where two panels meet, so neighbouring panels are
  parted by an 8 px rib (with the neck still cut through it), not a 32 px band.
- Square keys rendered in the rail tab's style: its slanted steel bezel made square round a well of glass, one render
  per light (`key.idle` ember, `key.hover` red, `key.on` forge orange, `key.ok` green, `key.warn` yellow, `key.alert`
  red, `key.off` unlit). The page's `face()` draws every icon key from them when the kit has them (helm keys, role and
  cargo keys, the queue, team chips, needs, and the status boxes of the ship systems display, the raid picker and the
  mounts); the Concord keeps its filled shapes. A status box is unlit glass lit in its status colour, a cooling mount is
  lit from the bottom up as it charges, and the icon's ink follows the glass: dark on lit glass, bone or orange on dark.

### 6. One divider, a red backlight (version 4)

The owner, 2026-11-03, of version 3's 8 px rib (with a crop of it): "bring back the thicker divider and make this
connected rather than seperated", then "back light more red please?".

- Neighbouring panels are parted by a 20 px divider of the plate's own metal: one unbroken piece from the left land to
  the spine. The neck no longer cuts through it, and the wells' corners at a divider are only cut, with no fangs
  pinching its ends (the fangs stay on the column's outer corners). A blade of red enamel runs along each divider, and
  the next panel's title placard sits centred on it.
- The Krai backlight is red: the forge lamps under every well's lips go from orange (1.0, 0.42, 0.15) to red
  (1.0, 0.13, 0.05), and the ember glass at rest from `#2a0806` at 0.14 to `#4c0705` at 0.24. The whole kit is
  re-rendered, so every frame, key, tab and strip, and mockup 4's Krai skin, carries it. Engaged keys stay forge orange.

### 7. The Krai palette only, nothing white (version 5)

The owner, 2026-11-03: "White shadows on buttons like bad. Same with green buttons we have as color palletes guide".
This answers open question K1 the other way.

- The state colours come from the Krai palette, the ramp its shields already used: whole in forge orange (`#ff7a1a`),
  damaged in gold (`#ffc23a`), down in red. The status keys' "ok" glass is forge orange (re-rendered), the second row of
  ticks (Systems under Power) bone glass, the ok bar fill forge orange. No green anywhere in the Krai console except the
  alert condition lamp named green.
- Glyphs on dark glass are forge orange, as on the rail tabs (`glyphOnDark`), never near-white bone. The slider handles
  and the orbit direction switch are rendered keys. The marks the Concord draws in white take the skin's colours
  (`mark`): a weapon box's hover outline in hot orange, its ready-to-fire outline in red, countdown figures in pale ember.
- The panel tabs' icons at rest are red (`tabIcon`; owner: "Panel icons should be red. Just to differentiate it from
  functional icons"), so a tab that switches a panel reads apart from a key that acts; the chosen tab still lights orange.
- Nothing on a key is white (owner: "White icons on buttons look bad I.mean", version 6): the ink on a dark key, drawn or
  rendered, is forge orange (the blade keys' labels, the expand arrows, the close crosses), dark on a lit one; a mode tab's
  name at rest is red, like the panel tabs' icons.
- The top strip is 60 px (owner: "top bar is way thicker than it needs to be", version 7): it holds only a label over
  a bar per readout, so `strip.top.join` is re-rendered at 60 px (channel rows 9-51) and the column's flange under it
  shortened to match; the bottom strip keeps its 80 px for its taller row. The page reads each strip's height and the
  offset of its content from the skin (`stripTop`, `stripBottom`).
- Bloom, an experiment (owner, of the column's channel of red light: "lets do an experiment and try bloom on this led
  strip?", version 8): the page picks the bright, strongly red pixels out of the column's render once (`bloom`: red at
  least 140 and three times its green and blue, so only the channel's light), blurs them twice (16 and 5.6 device px)
  and adds them back over the console, spilling past the column's edge onto the battle. A Bloom key in the view turns
  it on and off to compare; it is on by default. A Krai-only control: the parity check presses the Concord's controls.

### 8. Every Krai piece is rendered in Blender

The owner, 2026-11-03: "you didn't render the stitching elements in blender like you are supposed to". The placards,
the joined column, the joined strips and the allocation cells are modelled by `tools/blender/build_ui_krai.py` in the
skin's cast volcanic gunmetal, the shaped ones from outlines in `tools/mockups/krai_shapes.py` that pass rules K, L, R,
P and B (every shaped render passes rule B). Render cost at 48 samples on the cloud CPU: the small pieces 0.2-2.6 s
each, the joined strips 23 s each, the column (960 x 2160 px) 271 s. The page carries the kit as WebP: the Krai page is 1.3 MiB.

### 9. Verification of function: press every control in both skins

`tools/mockups/skin_parity.mjs` replays each of the 40 named shots, then for every control the Concord registered in
it: replays the shot again, presses the control at its centre (click, or a drag to it), steps the battle 30 steps
(1 s) and records the console state (station, tab, expanded panel, modes, toggles, the scene, the raid picks) and the
battle state (every ship's position, heading, speed, hull, shields, power, systems, weapons, orders, queues, pads,
hold, warp; mines, craft, shots, log length). The faction's page must give the same record press for press; the Concord
is first checked against itself so a difference cannot come from the check. Result: 2,175 presses of 270 distinct
controls in 40 shots, every one identical; 28 presses changed nothing a second later in either skin (toggles that act
later or hover cards). It found one Concord bug (decision 10).

### 10. Concord fixes

- The Fleet key stepped through `own[(own.indexOf(s) + 1) % own.length]` and threw when `own` was empty (the player's
  last ship jumped out). It is now dark and does nothing when no ship is left.
- Named shots now start from a clean console: explosions finished, the scene list and log closed, no hover.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| K1 | The Krai's state colours: green, yellow and red, or the skin's shield ramp (orange, gold, red)? | **Decided** (owner, 2026-11-03: "Same with green buttons we have as color palletes guide"): the Krai palette's orange, gold and red |
| K2 | Vector pieces the console draws itself (helm keys, role keys, the warp key, the reactor diagram, the ship schematic) take the Krai's colours and blade shape; should they become rendered pieces too? | Yes, piece by piece as the owner reviews; the kit already decides which pieces a skin renders |
| K3 | The player's ships keep the stand-in Concord hulls in the 3D view | Kept until mockup 3 (look dev) gives the Krai their own ships |
