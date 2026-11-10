# Design: vos-ship-concepts

## Context

- **The ask.** The owner, 2026-11-10: "lets desgin some vos pirate ships!".
- **What exists.**
  - The Syndicate's console, mockup 28 (`vos-battle-console`): hand-drawn ink, washes and claw tags. The owner's
    favourite of the reference skins: "so playful and pirate like".
  - Its palette, each colour with its meaning (`console-skins/design.md`, "Vos Syndicate"): ink, rust (salvage:
    everything they own was someone's), brass (loot and luck), verdigris (the spoils kept), bone (the charts they steal),
    blood (alerts).
  - Its one line of culture: free clans of raiders; luck, loot, loyalty to the crew, nothing else.
  - A placeholder line and emblem (mockup 36): Skiff, Corsair, Privateer, Galleon; a spiral cut into a coin.
- **What the Hunt and the Institute taught.** Ships go well when they start broad: a board of twelve massings, then the
  owner's picks, then detail. The Vos start with the board.
- **The rules that bind it.**
  - Ship Design Bible chapter 13 (C1-C5): never from the body; many traditions; every choice sourced; the culture is the
    owner's; the stereotype test. The first notes on the Vos (`console-skins/design.md`) read ships off their bodies
    ("mandible prows"); none of that is used.
  - Chapter 14 (F1-F5): one structure, grown; no placed primitive.
  - Chapter 11 (X1): the exterior complements the console.
  - The owner's "no literals". No ship is a picture of a sailing ship, a skull, a hook or the emblem's spiral.

## Decisions

### D1. Five traditions, drafted for the owner to pick

The culture is too thin to design from, so (C4) traditions inside the Syndicate are drafted as options. Each is a way of
living that free raider clans would have, taken from the one line of culture and the palette's meanings.

| Tradition | Who | What it builds from | What that gives a hull |
|---|---|---|---|
| The Boarders | The prize crews | A ship taken whole is worth ten sunk | Hulls that close with a prize, hold it and put a crew across |
| The Breakers | The salvage yards | Everything the clans own was someone's (the rust's meaning) | Ships joined from taken hulls, the join left plain |
| The Runners | The smugglers | Luck favours whoever is gone first | Slim hulls that are mostly drive and hold |
| The Chartkeepers | The pilots | They keep the stolen charts (the bone's meaning) and know the ways through cloud and rock | Ships built round where the pilot stands and what she sees |
| The Share | The quartermasters | Loot is split by the crew's own count; the ship is home and strongroom (loyalty to the crew) | Broad hulls round the common deck and the hold |

- Every tradition is provisional (survey Y1). The owner may keep, rename, merge or drop any.
- None comes from the Vos's bodies.
- A culture holds several at once (C2), and free clans would not agree on one. The line may mix them.

### D2. The Syndicate's section layout, `vsec`

**The shape.** Flat plate welded at hard chines. A clan's yard cuts and welds what it has taken; it has no mill to roll
plate. This follows from how a salvage yard works, and it sets the Vos apart from the Institute's rolled case (`ksec`)
and the Hunt's keel and hood (`hsec`).

**The layout.** 18 points, counter-clockwise from the keel's bottom centre (0) round the port side to the crown (9). Point
i mirrors point 18 - i, and edge k mirrors edge 17 - k.

| Points | Feature | Parameters |
|---|---|---|
| 0, 1 | The keel plate: a centre that can drop, and its edge | `kd`, `kw` (default 0, 1 m) |
| 2 | The chine | `cf` of the half beam out, `ch` of the way up to the rail (default 0.78, 0.4) |
| 3, 4 | The rail: the hull's widest line, a band standing proud | `ym`, `bh`, `bw` (default 1 m tall, 0.5 m proud) |
| 5 | The upper chine: the side leans in above the rail | `hbu` (default 0.8 of the half beam), `sy` |
| 6 | The deckhouse's foot | `hbt`, `sh` (none by default) |
| 7, 8 | The deck's edge and the coaming | `tc`, `cw` |
| 9 | The crown | `cr` |

- A feature a station lacks lies in its neighbours' line.
- Every section is checked as it is built: its port half must climb and stay on its own side. A section that fails is a
  console error, which fails the page's shots.
- Normals are shared only within 18 degrees, so every plate reads flat and every chine hard.

### D3. The skin answers the console

| Face | Colour | Where it comes from | Where it goes |
|---|---|---|---|
| Hull | Rust plate `#6a2c16`, roughness 0.7, metalness 0.35 | The console's rust: salvage | Everywhere |
| Rail | Brass `#a8741c` | Loot and luck | The rail, the seams where two hulls are joined |
| Glass | Ink-dark `#1a0d0a` | The crews' ink | Deckhouses, the pilot's bow, the walls where a hull parts |
| Light | Verdigris `#3fb7a3` | The spoils kept | Drives, the boarding collar, the chin lamp |

- **Clay** swaps in plain grey, so the massing alone can be judged.
- **Provisional.** Whether the hulls wear the console's palette is survey Y3. The rust and brass are darker than the
  console's own, so they hold under the studio light (the console's values read salmon and white).

### D4. The twelve blockouts

Measured from the built geometry (`window.MOCKUP_MEASURE`). All about frigate size, beside the Concord Petrel's 88 m.
Every skin is closed (zero open edges).

| # | Blockout | Tradition | How it is grown | Size, m | Triangles |
|---|---|---|---|---|---|
| 1 | Fork | Boarders | Split down the centreline 54 m from the stern; the halves part 18 m as two prongs, their facing walls glass | 108 x 40 x 13 | 378 |
| 2 | Clamp | Boarders | Split at the rail: an upper beak and a longer lower sill, opening apart toward the bow | 108 x 27 x 17 | 340 |
| 3 | Gangway | Boarders | The deck splits off as a spar reaching 45 m past the bow to a lit collar | 116 x 27 x 18 | 348 |
| 4 | Splice | Breakers | One body: a broad low stern and a tall narrow bow, joined at a brass seam 3 m long | 104 x 39 x 20 | 352 |
| 5 | Lashed three | Breakers | Split in three by two chords: the centre runs on, each flank grows into a side hull | 114 x 58 x 12 | 504 |
| 6 | Breaking berth | Breakers | Split down the centreline; the halves part round an opening about 20 m across and meet again | 104 x 43 x 12 | 412 |
| 7 | Dart | Runners | One body: a stern 34 m across narrowing to a needle 10 m across | 112 x 35 x 13 | 288 |
| 8 | Deep hold | Runners | One body: a slim deck over a keel 15 m deep amidships | 104 x 19 x 20 | 320 |
| 9 | High deck | Chartkeepers | One body: a glass deckhouse 9 m tall right aft over a long low foredeck | 104 x 29 x 19 | 308 |
| 10 | Pilot's bow | Chartkeepers | Split at the rail: the upper bow all glass, over a chin that ends in a lamp | 108 x 25 x 16 | 340 |
| 11 | Common deck | Share | One body: a flat hull 41 m across under a wide glazed house amidships | 100 x 41 x 14 | 328 |
| 12 | Strongroom | Share | One body: a belly 26 m deep amidships, banded twice in brass | 100 x 31 x 26 | 384 |

- **The stereotype test (C5).** Each line describes a working shape (a fork, a clamp, a gangway, a splice, a berth, a
  hold) without naming the species, and none is a picture of a body.
- **Literals.** None is shaped like a sailing ship, a skull or a hook. The names say what the hull does.
- **Provisional.** Sizes and proportions stay provisional until the classes are set (survey Y5). A pick's rooms come
  first once it is chosen (task 2.1).

### D5. The page

The page is the Institute's first board (mockup 23, version 1) with the Syndicate's section, materials, traditions and
hulls:
- the board in four columns by three rows, bows up the screen, a label over each hull;
- board views from behind, above and ahead; each blockout alone from its bow quarter, its side and above;
- the list grouped by tradition, each led by its source;
- painted or clay, labels, and the Concord Petrel for scale.

Its type is the Vos console's (Permanent Marker, Patrick Hand).

**Checked.** `tools/mockups/dom_fit.mjs` passes at every size. Shots are in
`docs/screenshots/mockups/vos-ship-concepts/`: the board views and blockout 1 at desktop, phone landscape and portrait,
and every blockout from its quarter and its side at desktop.

| Considered | Why not |
|---|---|
| Detailing the placeholder line (Skiff, Corsair, Privateer, Galleon) | The placeholders were made cheaply to fill a gap; the owner has picked none of their shapes |
| Reusing the Institute's section with other numbers | A rolled case is the Institute's language; a second faction on it would be a recolour |
| Unlike port and starboard sides, for a salvaged look | The owner's rule: single axis symmetry |

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| Y1 | Which of the five traditions are the Vos? | All five, as schools inside the Syndicate |
| Y2 | Which blockouts go on? | 1 Fork, 4 Splice, 5 Lashed three, 7 Dart, 12 Strongroom |
| Y3 | Do the hulls wear the console's palette? | Yes |
| Y4 | Flat plate and hard chines? | Yes |
| Y5 | How do the Vos get their classes? | Each pick a class by size |
