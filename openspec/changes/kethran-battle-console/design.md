# Design: kethran-battle-console

## Context

- **The skin.** The Kethran Hegemony's skin is in `console-skins/design.md` ("Kethran Hegemony: modelled in Blender") and
  on mockup 4: gunmetal plates bolted in layers, chamfers catching a harsh top light, recessed dark glass, conduits, plain
  trapezoid wedge keycaps that sink when pressed, ember lenses, red rank stripes, red and black hazard notches. Every kit
  piece is rendered by `tools/blender/build_ui_kethran.py` into `assets/ui/kethran/`.
- **Where it comes from.**
  - The owner, 2026-10-30: "build the other design language using 3d models in blender like starcraft"; "buttons should
    be more plain".
  - The provisional account (survey U7): an empire of rank and conquest whose machines are proud of their mass. The
    tradition the design draws on is the foundry and the armoury: plate, rank and fire.
  - Nothing in this design comes from what the Kethran look like (CLAUDE.md section 1). The skins page's "Bodies" row is
    still to be reviewed with the owner (console-skins design, rule 3).
- **What the console needs from a skin.** The battle console draws its whole interface live from a skin's fields (the
  ones `lib/concord.js` and `lib/kelvane.js` expose): colour roles, kit pieces by name, square keys whose light is their
  state, the type and the live readouts. The roles also draw over the 3D battle, so every role must read on a dark
  ground. The Kethran frames are dark recessed glass already, so, unlike the Kelvane's champagne plates, the console uses
  mockup 4's frames as they are.

## Decisions

### D1. The console's extra pieces are composed from the committed render

The render has the skins page's kit and no more. The console also draws square and wide keys in seven lights, allocation
cells, title placards and a ground behind its column. Rather than a second render, `lib/kethran.js` composes them from
the render already in the page, in its language:

| Piece | Made from | States |
|---|---|---|
| `key.*` (48 x 48) | the helm pad's top-left keycap in its black socket (`helm.pad.png`, 144 x 144 image px from (52, 84)) | steel at rest, lighter steel on hover, ember engaged and whole, hot orange damaged, signal red down, dark steel off |
| `keyw.*` (144 x 48, margins 0, 24, 0, 24) | the rendered wedge key (`button.idle`, `.hover`, `.pressed`) | the same lights |
| `cell.*` (32 x 28) | drawn: a wedge lens in a cut-cornered gunmetal bezel | dark glass, ember, red, deep ember, hatched red when locked |
| `plate.title` (140 x 24) | drawn: a gunmetal placard, a red rank stripe at its head and an ember pip at its foot | |
| `console.backing` (480 x 1080) | drawn: stepped gunmetal sheets, bolted, a conduit down each side, red and black hazard notches top and bottom, grain from a seeded generator | |

A lit cap is the lamp colour with the render's own shading laid back over it (soft light) and a glow from its top edge,
so the cap keeps its bevel and highlight. A cap's face is a trapezoid measured on the render: (15, 17), (129, 17),
(110, 123), (35, 123) in the key's crop; (11, 8), (149, 8), (140, 48), (19, 48) on the 160 x 56 wedge key.

| Considered | Why not |
|---|---|
| Rendering the extra pieces in Blender, as the Kelvane's | A second render for pieces the first already holds; composing keeps one source of every surface. A later render can replace them under the same names. |
| No square keys (the console's drawn fallback: a filled shape) | The helm, the rails and the status boxes would be flat colour in a skin made of modelled plate. |

### D2. Colour roles on dark glass

| Role | Colour | Meaning |
|---|---|---|
| Sky | `#ffc46a` | a reading: ember behind a lens |
| Accord | `#ffa83a` | whole and engaged: the foundry's deeper fire |
| Caution | `#ff7a2a` | damaged: hot orange (the skins page's ramp: ember, orange, red) |
| Alert | `#ff3b2e` | down, alarms: signal red |
| Lilac | `#a99a86` | a label: worn stencilled paint |
| Text, Title | `#d8cfbf` | prose and titles: bone |
| Signal | `#4a4f55` | a drawn key at rest: plain steel |

Readouts and wholeness are both fire, as the Krai's are both forge orange; the console tells them apart by place, never
by colour alone. Dark ink (`#1a0d05`) reads on the lit caps; bone stencil on steel.

### D3. Keys, knobs and labels

- The wedge keycap is the Hegemony's key everywhere: the square key is its helm keycap, the wide key its rendered key
  (CLAUDE.md section 7, rule 1).
- Drawn keys and knobs (`rr`, `knob`) cut every corner at 45 degrees and are never round: a chamfered box, an octagon.
- Labels centre on the keycap's plain face, 22 px in from each end at the key's 56 px (`keyField`), and on a tab clear
  of its lamp (18 px) and its cut corner (10 px) (`tabField`).

### D4. Type

Saira Stencil One for headings (700 weight at 22 px and up, set at 0.78 of the size: measured 1.43 times as wide as
Antonio); Saira Condensed for the name plate and everything else, at the size asked (1.04 times as wide). The name plate
was first set in the stencil face, which trimmed every captain's name at the plate's 262 px; it is set in the condensed
face at 18 px, which fits.

### D5. Icons

All 24 subjects of the icon guide, drawn as solid wedges, chevrons and blades with every corner cut (from the prow:
console-skins design), glowing ember when bright. The skins page draws its ten from the same function. Mockup 4's map
(a triangle of wedges) becomes the guide's course to a mark.

### D6. Strips

The strips are drawn at their rendered 72 px; the content, laid out for 56 px, moves down 10 px (top) and 9 px (bottom)
into the channel between the conduit rail and the lip (rows 17-59 and 16-58, measured on mockup 4).

## Measurements

| Check | Result |
|---|---|
| Parity, Kethran page against the Concord's (`skin_parity.mjs`) | 70 shots, 4,087 presses of 396 controls, every press matched (run on the page before its final name-plate font change, which changes no behaviour) |
| Console fit on mockup 4 (`console_fit.mjs`, kethran) | clear |
| DOM fit on the page (`dom_fit.mjs`), every shot at desktop, phone and portrait | PASS |

## Open questions

| Id | Question | Recommendation (provisional) |
|---|---|---|
| H1 | The Hegemony's names for ships, ranks and officers. Placeholders: ships named as verdicts and deeds (Iron Verdict, Sentence, Unbowed), the ranks Blade, Spear, Commander and Warlord (the scenario's own Kethran are Warlords and Blades). | The owner sets the Hegemony's culture (CLAUDE.md section 1). Until then the placeholders stand, marked as such. |
| H2 | The console's keys, cells, placards and backing composed from the committed render (D1), not rendered anew. | Composed for now; a render can replace them under the same names if the owner wants more depth. |
| H3 | Readouts and wholeness both in fire (D2). | Keep: the palette has one light, the foundry's. |
| H4 | Mockup 4's ten icons replaced by the icon guide's subjects (D5). | Replace: one subject, one picture everywhere. |
