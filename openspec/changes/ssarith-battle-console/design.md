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
- Labels are centred. On a tab, the label sits in the tube's measured field (x 22-111 of 120, `tabField`); the tally
  marks engraved at the tab's left are not label room.
- Keys the page draws itself have every corner cut at 45 degrees, and knobs are octagons.

**Strips**: drawn 64 px tall, with their readouts on the flat middle.

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
