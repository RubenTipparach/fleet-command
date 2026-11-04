# Design: ssarith-battle-console

## The skin's fields

`FCSsarith` gives the battle console the same fields as `FCConcord` and `FCKrai`.

**Colour roles (`CC`)**: dark tubes at rest; phosphor `#6dff8a` for readouts, hover and the whole; amber `#ffb340` for
the damaged; kill red `#e0533c` only for the down and for warnings ("Alerts only", console-skins design); moss for
labels; bone for text and titles.

**Type**: Black Ops One for headings (weight 700, 22 px and up, set at 0.8x). VT323 for everything else, set at 1.2x the
Antonio size the page asks for.

**Icons**: the 24 subjects of the icon guide as straight phosphor strokes with 45 degree turns and cut corners. The ship
is the Hunt's own squat hull with a broad prow.

**Keys**:
- The rendered tube keys, in seven lights: `key.*` 48 x 48 and `keyw.*` 144 x 48, nine-sliced in x.
- Labels are centred. On a tab, the label sits in the tube's measured field (x 15-105 of 120, `tabField`), centred.
  The tab carried three tally marks at its left until 2026-11-04 (owner: "The panel icons have ||| on them but the icons
  don't fit because of that extra symbol. I want those cleaned up and fit the incons in the button diaplay"); now its tube
  runs x 9-111 with the same metal at each end, and every label and icon sits inside the tube's glass.
- Keys the page draws itself have every corner cut at 45 degrees, and knobs are octagons.

**Strips**: drawn 64 px tall, with their readouts on the flat middle.

**Placards** (owner, 2026-11-04: "the labels here need work, they are just dark background rectangles with words", "see
how we resolved this with the Krai labels"): each panel title and officer slot on a frame's rail is set on `plate.title`,
140 x 24 and nine-sliced in x. It is a cut-cornered plate of the key bezels' dark gunmetal, with a phosphor diamond lit at
its head and a bone groove along its foot. The Krai's labels also sit on placards (`plate.label`), but the Hunt has none:
its labels inside a panel are on the tracker's screen, where text is lit on the glass itself. A placard there would sink
into the glass and leave only its pip, a stray dot.

**The column's ground** (owner, 2026-11-04: "Black background is forbidden for metallic panel based factions"):
`console.backing`, 480 x 1080, one plate of the hide-pressed olive gunmetal behind the column, with bolts down both edges,
a groove along each edge, the tally at its foot and fronds over two corners. Behind an expanded panel the page lays a
plain band of it (x 100-390) side by side, every other copy mirrored. The Krai's expanded panels get their own
`console.backing` in blackened blade steel.

**Names**: ships Sskarr, Hissath and Thrael; ranks Unblooded, Tracker, Stalker and Huntmaster; thirteen officers.
These names are ours and placeholders.

## The Hunt stalker (`shs`)

| Field | Value |
|---|---|
| Length | 260 m |
| Hull | 120 |
| Cloak | Yes ("built to wait unseen") |
| Mounts | 2 disruptors, 1 torpedo, 2 lances, point defence |

The model is simple geometry: a squat hull extruded from a broad-prowed outline, with a dorsal armour block, a prow
plate, two engine blocks with phosphor exhausts, and canopy-green side plates. The ship systems display draws the same
outline.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| H1 | Should the Hunt's column be one joined plate, as the Krai's is? | No: separate cut-cornered frames, as on the skins page |
| H2 | Ship, officer and rank names | Ours, placeholders until the Hunt's thread names them |
| H3 | Should the Hunt stalker's numbers come from its ships thread? | Yes, when it has them |
