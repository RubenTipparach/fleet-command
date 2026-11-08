# Design: tarsine-battle-console

## Context

- **The skin.** The Tarsine Remnant's skin is in `console-skins/design.md` ("The Remnant on the bible, grime kept") and on
  mockup 4: corroded iron with rust blooms, verdigris and pitting, great rivets with rust running down from them, worn
  gold bezels, arched-tablet keys and lintel tabs whose grimy glass carries the same caked dirt in every state, lit by old
  lamps that do not all still burn. Every kit piece is rendered by `tools/blender/build_ui_tarsine.py` from the
  fit-checked shapes of `tools/mockups/tarsine_shapes.py`.
- **Where it comes from.**
  - The owner, 2026-10-30: "a powerful ancient race. rusty, with back lit buttons clogged with dirt. their ships havent
    been upgraded in ages, rough around the edges"; 2026-10-31: "please dont remove the grimey industrial feel"; "need to
    sync up dust with out and unlit button to give it that illusion".
  - The provisional account (survey U7): a fallen empire that remembers being first and will not change; machines older
    than their crews, patched and never replaced. The tradition the design draws on is the monument: tablets, lintels,
    capitals, carved glyphs.
  - Nothing in this design comes from what the Tarsine look like (CLAUDE.md section 1).
- **What the console needs from a skin.** The battle console draws its whole interface live from a skin's fields (the
  ones `lib/concord.js` and `lib/kelvane.js` expose): colour roles, kit pieces by name, square keys whose light is their
  state, the type and the live readouts. The roles also draw over the 3D battle, so every role must read on a dark
  ground. The Remnant's frames hold dark grimy glass already, so the console uses mockup 4's frames as they are.

## Decisions

### D1. The console's extra pieces are composed from the committed render

| Piece | Made from | States |
|---|---|---|
| `key.*` (48 x 48) | the helm pad's top-left tablet in its iron (`helm.pad.png`, 148 x 148 image px from (54, 50)) | the light behind the dirt: dim at rest, half lit on hover, full gold engaged and whole, a tired amber damaged, ember down, all but dark off |
| `keyw.*` (144 x 48, margins 0, 30, 0, 30) | the rendered tablet key (`button.idle`) | the same lights |
| `bar.cell.ok` | the lit window (`bar.cell.on`) | whole is the backlight itself |
| `cell.*` (32 x 28) | drawn: a little arched window in corroded iron, fine caked dust over the glass | dark glass, gold for power out, a verdigris-tinged light for power in use, ember, dark when locked |
| `plate.title` (140 x 24) | drawn: a lintel of corroded iron, a worn gold inlay along its foot, a rivet at each end | |
| `console.backing` (480 x 1080) | drawn: one corroded iron plate, riveted round its edge and along two seams, rust running from every rivet, a worn gold rule down each side | |
| `nameplate` | the rendered name plate, its window's light multiplied down | |

The state is a multiply of the light over the render's own lit glass, clipped to the glass (measured on the render: the
tablet's glass 15-133 x 15-138 of the 148 px crop, top corners 16 px, foot corners 8 px; the tablet key's 15-125 x 11-40
of 140 x 52). The dirt is the render's, so it is the same in every state, and only the backlight changes: the Remnant's
rule from version 17.

**The name plate.** Mockup 4 sets pale carved letters outlined dark on the lit gold window. The console draws its text
plain, and dark letters over the grime read poorly in shots, so the console's plate is the same render with its lamps
turned low (the window multiplied by `#4a3c28`), and the name is in bone.

| Considered | Why not |
|---|---|
| Rendering the extra pieces in Blender, as the Kelvane's | A second render for pieces the first already holds; composing keeps one source of every surface. |
| A shadow hook on the console's name plate text | A rule for one skin in the shared console; the dimmed plate needs none. |

### D2. Colour roles on dark grimy glass

| Role | Colour | Meaning |
|---|---|---|
| Sky | `#efe2a0` | a reading: pale light through the old glass |
| Accord | `#d6c76a` | whole and engaged: the backlight, the fire still burning |
| Caution | `#d08a3a` | damaged: a tired amber (the skins page's ramp: light, amber, ember) |
| Alert | `#e8553a` | down, alarms: ember, anger slow to wake |
| Lilac | `#a8987a` | a label: worn gilt |
| Text | `#e8dcc0` | prose: bone |
| Title | `#d8bc6e` | a panel's name: old gold |
| Signal | `#5a4a36` | a drawn key at rest: dark bronze |

Dark carved ink (`#1e160c`) reads on the lit tablets; on the dim glass the glyphs are the backlight's gold.

### D3. Keys, knobs and labels

- The arched tablet is the Remnant's key everywhere: the square key is its helm tablet, the wide key its rendered tablet
  key (CLAUDE.md section 7, rule 1). The rendered tablet key is lit at rest (mockup 4), so its label is dark in every
  state.
- Drawn keys (`rr`) are arched tablets: round above, square at the foot; a knob is a domed rivet head.
- Labels centre on the tablet key's glass, 17 px in from each end (`keyField`), and in a lintel's slot 10 px in
  (`tabField`).

### D4. Type

Cinzel carved capitals for headings and the name plate (700 weight at 22 px and up, set at 0.7 of the size: measured 1.55
times as wide as Antonio); Alegreya Sans SC for everything else, at 0.86 of the size (1.2 times as wide). Its figures are
old-style, so a zero is short and round; see T3.

### D5. Icons

All 24 subjects of the icon guide, drawn as glyphs carved into old metal and worn: straight cut strokes with square ends,
a dark cut under each and a glint along its upper edge, a few places worn away by time (seeded). On a lit tablet (a dark
ink) the cut is a faint glint instead of a shadow. The skins page draws its ten from the same function.

### D6. Strips

The strips are drawn at their rendered 72 px; the content, laid out for 56 px, moves down 8 px into the beam's channel of
grimy glass (rows 13-59, measured on mockup 4).

## Measurements

| Check | Result |
|---|---|
| Parity, Tarsine page against the Concord's (`skin_parity.mjs`) | 70 shots, 4,087 presses of 396 controls, every press matched |
| Console fit on mockup 4 (`console_fit.mjs`, tarsine) | clear |
| DOM fit on the page (`dom_fit.mjs`), every shot at desktop, phone and portrait | PASS |

## Open questions

| Id | Question | Recommendation (provisional) |
|---|---|---|
| T1 | The Remnant's names for ships, ranks and officers. Placeholders: ships named for what the empire remembers (Ankhar, Oathkeeper, Long Memory), captains by age, the ranks Younger, Keeper, Elder and Eldest. | The owner sets the Remnant's culture (CLAUDE.md section 1). Until then the placeholders stand, marked as such. |
| T2 | The console's name plate with its lamps turned low and the name in bone (D1), unlike mockup 4's lit plate. | Dimmed: the name must read at a glance under fire. |
| T3 | Alegreya Sans SC's old-style figures in readouts (a zero reads close to a small o). | Keep for now; a lining-figure face for numbers if the owner finds them hard to read. |
| T4 | Mockup 4's ten icons replaced by the icon guide's subjects (D5). | Replace: one subject, one picture everywhere. |
