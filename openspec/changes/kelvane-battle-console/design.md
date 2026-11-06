# Design: kelvane-battle-console

## Context

- **The skin.** The Kelvane Institute's skin is in `console-skins/design.md` ("Kelvane Institute") and on mockup 4. It is
  built in Blender from the fit-checked shapes of `tools/mockups/kelvane_shapes.py`, the way the Analog Panel Bible
  builds an NLog-style instrument (`docs/design/ui-bible.html`): pillowed champagne plates (`#c9bfa9`) over recessed
  displays. Amber backlit film with dark ink carries the readouts; green phosphor glass carries the scopes.
- **Where it comes from.**
  - The owner, 2026-10-31: "Kelvane 100% can be modeled, let and rendered just take the osciloscope dash board for
    inspiration, old school electircal dashboards, dials, switches etc"; "They were inspired by the nucleo look".
  - The tradition is the laboratory bench and the calibrated instrument: a reading lit behind glass, built to be checked
    by eye.
  - Nothing in this design comes from what the Kelvane look like (CLAUDE.md section 1).
- **What the console needs from a skin.** The battle console (`docs/mockups/battle-console.html`) draws its whole
  interface live:
  - colour roles (`CC`) used about 400 times;
  - kit pieces by name;
  - rendered keys whose light is their state.

  The roles draw both inside the panels and over the 3D battle (the target bracket, the objective, the log lines), so
  every role must read on a dark ground.

## Decisions

### D1. Console panels are instruments with a screen

**The problem.** Mockup 4's panel frame is plain champagne metal with dark ink printed on it. The console's readouts
are light, and the same light colours draw over the battle. Amber on champagne measures a contrast ratio of about 1.4,
which is unreadable.

**The decision.** The console gets its own frame, `frame.screen`:
- the same champagne plate (180 x 140, 9-slice margins 28 px);
- rolled 3 mm round one well of dark smoked glass (`#1b150d`), 12 px in from the edge;
- the readouts glow in that glass, as on a laboratory instrument's display window.

The same treatment goes to the inset (`inset.screen`: the contacts box, the hover cards, the portrait and the target
box) and to the strips (`strip.top.screen` and `strip.bottom.screen`). In the strips a long screen, 87 to 442 of 512 px,
runs between the needle-meter bay and the knob. The console lays its readouts from 96 px in at each end, so 9 px of
glass shows clear each side.

**Why this is the Institute's look.** It keeps both of the Institute's lights:
- the amber film stays where mockup 4 has it: on the keys, the tabs, the name plate and the meter bays;
- the phosphor scopes stay as the radar dial.

**How the console gets it.** `lib/kelvane.js` hands these pieces to the console under the kit's names (its `CONSOLE`
map). The console's code is unchanged, and mockup 4's plates are unchanged.

| Considered | Why not |
|---|---|
| Amber film filling each panel, with dark ink (mockup 4's look) | The roles also draw over the battle, where dark ink is invisible. One role set must work on both grounds. |
| Green phosphor glass in each panel | It reads as the Ssarith Hunt's tubes. No skin shares a look (CLAUDE.md section 7, rule 1). |

### D2. Colour roles on the glass

Each colour is measured against the glass, `#1b150d` (WCAG contrast ratio):

| Role | Colour | Contrast | Meaning |
|---|---|---|---|
| Sky | `#ffc865` | 11.8 | a reading (amber, as the film) |
| Lilac | `#b8a074` | 7.2 | a label (brass print) |
| Text | `#f3e4c2` | 14.4 | prose (parchment) |
| Title | `#ffd27a` | 12.7 | a panel's name |
| Accord | `#5df08a` | 12.3 | whole and engaged (phosphor, the measurement taken) |
| Caution | `#ff8f3a` | 8.0 | damaged |
| Alert | `#ff4f3a` | 5.5 | down, alarms (needle red) |
| Ghost | `#75674f` | 3.3 | vacant, disabled (meant to recede) |

- Caution is pulled toward orange so it never reads as a plain amber reading.
- Dark ink (`#2a1a06`) on each lit window:
  - amber: 10.3
  - hot amber: 13.1
  - green: 12.1
  - orange: 8.0
  - red: 4.8
- Amber glyphs on an unlit key window (`#33261a`): 9.6.

### D3. The window key

**The shape.** Every square key, wide key and allocation cell is the Institute's window key: a champagne plate with
one well of glass. One plate and one glass serve every state; the light in the window is the state.

| Piece | Window | States, in order |
|---|---|---|
| `key.*` (48 x 48) | `rbox(8, 8, 40, 40, 5)` | idle unlit smoke, hover amber, on hot amber, ok phosphor green, warn orange, alert red, off dark |
| `keyw.*` (144 x 48) | `rbox(10, 9, 134, 39, 5)` (margins 0, 22, 0, 22) | the same lights |
| `cell.*` (32 x 28) | `rbox(6, 6, 26, 22, 3)` (margins 10) | idle smoke, lit hot amber, warn red, accord green, locked dark |

**Lamps.** The lit amber windows carry the bible's lip lamps (10 mm, 180 degrees), at 0.02-0.03 power on keys.

**Where they are used.** The icon rails use the square key (`iconKeys`), because the tab piece's window is too short for
an icon at 44 px.

### D4. Placards, name plate and backing

- **Title placard (`plate.title`, 140 x 24).**
  - A champagne plate with a lit amber jewel in a dimple at its head: the toggles' pilot lamp, meaning this instrument is
    on.
  - A smoke window, `rbox(20, 3, 134, 21, 3)`, holds the title and the officer slot's rank pips and name.
  - The roll is 0.8 mm with 0.8 mm of land. Rule R needs 2.4 mm and has 2.5 mm.
  - The console sets a title's baseline 19 px under the plate's top. `CON.titleDy` (2 px) raises it, so the title's
    caps centre in the window. This is a generic hook; other skins leave it at 0.
- **Name plate (`nameplate.console`).** It is mockup 4's name plate, with the right end left as plain metal for the
  console's Fleet key (370-456 x 6-36). Two screws sit below the key.
- **Column backing (`console.backing`, 480 x 1080).**
  - One plain champagne plate (CLAUDE.md section 7: no black behind a metal skin's panels).
  - It is bead-blasted (satin at roughness 0.5 with no clear coat). At roughness 0.22 the plate caught the softbox as a
    bright patch across its upper third, and the expanded panel's tiled band would have repeated that patch.
  - Screws sit at the corners and mid-height.
  - Down each edge runs an engraved measuring scale: a tick every 10 mm and a long tick every 50 mm. The Institute marks
    every surface it works at, so a reading can be checked against it (design.md: "Truth is measured").

### D5. Type

- **The face.** "Share Tech Mono", the instrument's readout face, sets every readout.
- **The scale.** Measured on the console's own labels, it runs 1.31 times as wide as Antonio at the same size, so it is
  set at 0.8 times the size. Rule T (`keyCaps`, the tabs' fit) shrinks anything that still runs long.
- **The pair's other face.** "Courier Prime", the skin's engraved-label face, stays on mockup 4. The console has no
  label engraved on metal: all its labels are lit on glass.

### D6. Icons

All 24 subjects of the icon guide are drawn in the Institute's hand:
- the style of an instrument's legend plate, a schematic drawing;
- even strokes with round caps, true circles and arcs;
- sine waves for anything given off;
- solid fills where the thing itself is solid;
- dots at terminals;
- a soft glow on a bright ink, as a readout behind glass.

Mockup 4's ten rendered `icon.*` pieces predate the icon standard. Two of them break it:
- fire is a lightning bolt, which would share a picture with power;
- map is a ruler.

Task 1.8 re-renders them from these subjects.

### D7. The ship

- The Kelvane faction joins the console's `FACTION` table with the skin `kelvane`.
- The research cruiser Patience (Dr. Orrel, class `ca`) joins the allied fleet and the swap scene ("Four races", key 4).
- The Institute has no line of ships yet, so it flies the stand-in hull in amber trim and champagne hull.

### D8. Live meters in the strips

**The ask.** The owner, 2026-11-06, of the strips' needle meters: "that looks really good! can we have these meters be
tied to the speed and hull integrity?"

**The decision.** The console's strip renders keep the printed scale (205-335 degrees about (46, 50), ticks at radius
18-22) but not the printed needle. The console draws the needle live:
- **The readings.** The top strip reads hull integrity. The bottom strip reads speed, out of the speed slider's full
  scale of 300 m/s. They are the same values the strip's bar and number show (CLAUDE.md section 6.1).
- **The needle.** Needle red with a brass hub, swinging toward its reading and settling at 4 per second.
- **Snapping.** After a pause, a scene change or a ship hop, the needle is set straight to its reading.
- **Hover.** Pointing at a meter names its reading.

**How a skin opts in.** A skin gives the meter's geometry (`CON.meter`) and its needle (`CON.live.needle`). The other
skins give neither, so their strips are unchanged. Mockup 4's strips keep their printed needle.

## Measurements

| Check | Result |
|---|---|
| Fit rules K, L, R, P and B on every piece's shapes | 0 failures |
| Parity, Kelvane page against the Concord's | 70 shots, 4,087 presses of 396 controls, every press matched |
| Rule B on the renders | every piece, 0 lit pixels outside its plate |
| Pieces mockup 4 uses | byte-identical manifest entries |
| Render time | about 4 minutes at 48 samples on 4 cores, the backing taking most of it |

## Open questions

| Id | Question | Recommendation (provisional) |
|---|---|---|
| K1 | The Institute's names for ships, ranks and officers. Placeholders: research vessels named for the virtues of inquiry (Patience, Candour, Parallax), doctors as captains, the ranks Fellow, Lecturer, Reader and Professor. | The owner sets the Institute's culture (CLAUDE.md section 1). Until then the placeholders stand, marked as such. |
| K2 | Dark smoked-glass screens in each panel, not mockup 4's champagne panels with printed ink (D1). | Screens: the readouts must also read over the battle. |
| K3 | One readout face (Share Tech Mono) for the whole console, Courier Prime only on mockup 4 (D5). | One face, as an instrument's display has. |
