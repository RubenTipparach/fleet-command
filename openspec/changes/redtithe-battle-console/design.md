# Design: redtithe-battle-console

## Context

- **The skin.** The Red Tithe's skin is in `console-skins/design.md` ("Three more factions" and "the Red Tithe in wood")
  and on mockup 4. It is painted in canvas, a first pass before the wood and metal are modelled in Blender and the paper
  painted in Krita: crusted, stained and scorched bounty sheets nailed to dark, scarred oak, strap iron and rivets, the
  trophies shaded by hand. Every speck, stain, drop and scratch comes from the seeded generator, never a cheap hash
  (CLAUDE.md section 10).
- **Where it comes from.** The owner's words in the proposal. The culture is ours and provisional (survey N3-N6): raiders
  who take a tithe of blood from every crew they board, whose orders are written on bounty sheets and nailed up with
  knives, and whose captains count their standing in trophies. Three traditions: the sheet-keepers (paper, ledgers,
  seals), the gunners (bandoliers, brass, the revolver cylinder), the cutters (blades, the trap jaws). The teeth are other
  crews' trophies, never the Tithe's own (CLAUDE.md section 1).
- **What the console needs from a skin.** Colour roles, kit pieces by name and optional hooks. The readouts draw inside
  the panels and over the battle, so every role must read on a dark ground.

## Decisions

### D1. One library, two pages

`docs/mockups/lib/redtithe.js` holds the whole look; `skins/redtithe.js` keeps the skins page's tokens and registers the
library's painters and column. Mockup 4's three Red Tithe shots were shot before and after the move and are
pixel-identical.

### D2. The boards are tarred where the readouts are

**The problem.** Mockup 4's panels are paper, with dark ink typed on them. The console draws its readouts in light, and
the same colours draw over the battle (the target bracket, the objective, the log), where dark ink cannot be read. The
Kelvane met the same problem (`kelvane-battle-console` D1).

**The decision.** The console's panels, insets and strips are the hall's boards round a field tarred black (`#1f140b`),
the board the tithe is chalked on; the readouts are chalked on it in light. The paper stays where dark ink is printed on
it: the trap-jaw keys, the chosen tab's slip, the name plate, and the scraps of paid sheets nailed in the margins.

- **Panel (`frame.panel`).** Four boards bound with strap iron at the corners round the tarred field; a scrap of a paid
  sheet nailed in the top-left corner, a knife driven into the bottom board at the right, knots, gouges, a tally, a
  scorch and old blood only in the margins (30, 56, 30, 30 px). The field is laid flat, with no grain to smear when the
  nine-slice centre stretches to a full-screen panel.
- **Inset.** A well cut into the board: dark oak sides round a tarred floor, a nail at each corner.
- **Strips.** A beam of oak banded with strap iron, its middle tarred; in the 92 px ends a sheet's scrap knifed to the
  beam and a string of teeth (top), three spent rounds and a knife (bottom).
- **Title placard (`plate.title`).** A slat of charred oak nailed at its head.

| Considered | Why not |
|---|---|
| Mockup 4's paper panels with dark ink | The roles also draw over the battle, where dark ink is invisible. |
| A dark leather ground | Leather is the bandolier's; the boards are the Tithe's hall (owner: "More brown wood"). |

### D3. Colour roles, chalked on the tar

| Role | Colour | Meaning |
|---|---|---|
| Sky | `#efdcae` old paper | a reading (every debt written down) |
| Text | `#f6ecd2` bone | prose |
| Title | `#efdcae` | a panel's name |
| Lilac | `#c0a27a` worn paper | a label |
| Accord | `#e2ad48` brass | whole and engaged (spent rounds counted as standing) |
| Caution | `#e07a32` burnt orange | damaged |
| Alert | `#ff4b3e` fresh blood | down, alarms (the tithe being taken) |
| Ghost | `#76604a` | vacant, disabled (recedes) |

Fresh blood is drawn brighter than mockup 4's `#c4141c` so it reads on the tar.

### D4. The slot key

The console's square keys show their state as what is in them: a block of oak bound with strap iron at its corners, a
slot cut in it (the sheet-keepers' pigeonhole). Empty (idle) the slot is dark and the glyph is cut pale into the wood
behind; engaged, a slip of the ledger is pushed into the slot, tinted by the state, its glyph in black:

| State | In the slot |
|---|---|
| idle | nothing: the dark slot |
| hover | a slip of old paper |
| on | a fresh slip |
| ok | a brass-washed slip |
| warn | a rust-stained slip |
| alert | a slip soaked in blood, a fresh drop on it |
| off | the slot barred with two straps of iron |

`key.*` are 48 x 48; `keyw.*` are 144 x 48 with 22 px ends. The icon rails use the square key (`iconKeys`). Allocation
cells are notches in a tally board, a slip of the role's colour pushed in when lit, barred when locked. The bars stay
mockup 4's bandolier, with a nickel round for a whole bar.

### D5. Type

"Stardos Stencil" for titles, keys and readouts of 17 px and over; "Special Elite", the typed sheet, for labels and small
numbers (mockup 4's pair). Both run about 1.4 times as wide as Antonio (measured on the console's own labels), so both
are set at 0.79 times the size asked for; a key label rule T shrinks below 17 px changes face and keeps its width. The
trap-jaw keys centre their labels 24 px in from each end (`keyField`), between the stakes.

### D6. Icons

Each subject is cut with a blade: mockup 4's ten and fourteen more (power, tactical, point defence, fleet, the ship as a
raider's hull with a ram and outrigger pods, sensors, craft, weapons, warp, engines, reactor, pause, menu, cargo). In black
ink the cut has the pale rim the blade lifted from the paper (mockup 4); in a light ink, the console's on its dark boards,
the cut is in the wood and its rim is a shadow.

### D7. The console's name plate and ground

- **Name plate.** The bounty sheet ends at 362 px, a knife pinning its lower corner, so the Fleet key (370-456 by 6-36)
  sits on the plank's bare end over a tally. The name is typed in Special Elite 17 px.
- **Column backing (`console.backing`).** Mockup 4's plank wall at 480 x 1080 (CLAUDE.md section 7: no black behind a
  skin of material).

### D8. Names

Placeholders, ours, until the owner sets the Tithe's culture (R1): ships named for debts and their keeping (Red Ledger,
Bone Tally, Last Due), captains as the sheets name them (Capt. Hesk Varro), ranks by the share of the tithe a hand may take
(Deckhand, Cutter, Bloodhand, Captain), and thirteen officers' names.

## Measurements

| Check | Result |
|---|---|
| Mockup 4's Red Tithe console and kit sheet, before and after the move | pixel-identical |
| Parity, Red Tithe page against the Concord's | 70 shots, 4,087 presses of 396 controls, every press matched |
| `dom_fit.mjs` (battle, weapons expanded, menu; desktop, phone, portrait) | PASS |
| `console_fit.mjs` on mockup 4 (redtithe) | clear |

## Open questions

| Id | Question | Recommendation (provisional) |
|---|---|---|
| R1 | The Tithe's names for ships, ranks and officers (placeholders, D8). | The owner sets the culture (CLAUDE.md section 1; survey N3-N6). |
| R2 | Tarred boards for the console's panels instead of mockup 4's paper (D2). | Tarred boards: the readouts must also read over the battle. |
| R3 | The slot key with tinted slips for state (D4). | Keep it: the state reads at a glance and the glyph stays black on paper. |
| R4 | The bandolier's rounds are small at the console's 10 px bar height. | Keep the console's bar height; the rounds read as a brass belt. |
