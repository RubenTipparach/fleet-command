# Design: kelvane-ship-concepts

## Context

- **The ask.** The owner, 2026-11-06: "begin designs on the institute ships".
- **What exists.**
  - The Institute's console, mockup 20 (`kelvane-battle-console`). Its tradition is the laboratory bench and the
    calibrated instrument: pillowed champagne plates, wells of dark smoked glass where a reading glows, amber for a
    reading, phosphor green for whole.
  - Its emblem, mockup 21: hydrogen's 3d z² orbital on an oval coin.
  - Its one line of culture: "truth is measured, never assumed; every reading is shown on an instrument a scientist can
    check by eye" (`console-skins/design.md`).
  - Its placeholder names: research vessels named for the virtues of inquiry, doctors as captains (kelvane-battle-console
    K1).
- **What the Hunt taught.** The Hunt's ships went well once they went broad: a board of twelve massings (mockup 12, version
  9), then the owner's picks grown as one structure (version 12), then detail (versions 13 onward). Narrowing to one hull
  first produced "3 lame ones that look like coffins". The Institute starts with the board.
- **The rules that bind it.**
  - Ship Design Bible chapter 13 (C1-C5): never from the body; many traditions; every choice sourced; the culture is the
    owner's; the stereotype test.
  - Chapter 14 (F1-F5): one structure, grown; no placed primitive.
  - Chapter 11 (X1): the exterior complements the console.
  - The owner's "no literals" (on the Hunt's Spearhead, 2026-11-05: "there we go again with the ltiterals lol"). No ship is
    shaped like an instrument, a tuning fork, an atom or the emblem's orbital.

## Decisions

### D1. Five traditions, drafted for the owner to pick

The culture is too thin to design from, so (C4) traditions inside the Institute are drafted as options. The first three are
the ones drafted for the emblem (`faction-emblems/design.md`, "The Kelvane Institute"), so the Institute keeps one set of
schools. Two are new, because ships do work the emblem never had to: long voyages, and a weapon.

| Tradition | Who | What it builds from | What that gives a hull |
|---|---|---|---|
| The Calibrators | The instrument makers | A reading is only as true as the standard it was checked against | One closed case; true faces; proportions in whole ratios; every panel where it can be checked |
| The Open Notebook | The reviewers | No result is true until another laboratory repeats it | Every reading taken twice: two identical hulls, or two identical decks |
| The Long Baseline | The surveyors and astronomers | The Institute began by measuring the distance to a star | Ranging by parallax: two stations as far apart as the hull allows |
| The Field Stations (new) | The expedition scientists | They work years from home | Deep self-sufficient hulls; the laboratory behind a wide gallery of glass that looks out |
| The Collider Halls (new) | The particle physicists | Their great machines are beamlines and rings | Ships built round the accelerator that is also their lance |

- Every tradition is provisional (survey I1). The owner may keep, rename, merge or drop any of them.
- None comes from the Kelvane's bodies. The console notes' "many-handed" stays out, as it did for the emblem.
- A culture holds several of these at once (C2). The line the owner picks may mix them: one yard's frigate and another's
  cruiser.

### D2. The Institute's section layout

**The shape.** The Hunt's section (`hsec`) is a keel, chines and a hood: the Hunt's own language. The Institute gets its own,
`ksec`: a rounded case after the console's rolled plates (X1).

**The layout.** 30 points, counter-clockwise from the keel's bottom centre (0) round the port side to the crown (15). Point i
mirrors point 30 - i, and edge k mirrors edge 29 - k.

| Points | Feature | Parameters |
|---|---|---|
| 0, 1 | The keel: a bottom centre that can drop, and a flat | `kd`, `kw` (default 0, 1 m) |
| 2-5 | The bottom roll, a quarter circle in three steps | `r` (default 2 m) |
| 6, 7 | The belt: a band of the side that can stand proud and carry light | `ym`, `bh`, `bw` |
| 8, 9 | The shoulder: a shelf from the side in to a narrower upper deck | `sy`, `sh`, `hbt` |
| 10-13 | The top roll | `rt` (default `r`) |
| 14, 15 | The crest and the crown | `cw`, `cr` |

- A feature a station lacks lies flat in its neighbours' line: no shelf when `hbt` is `hb`, no belt when `bh` is 0.
- Every section is checked as it is built. Its port half must climb, never turning back on itself, and stay on its own side.
  A section that fails is reported as a console error, which fails the page's shots (the checks found 15 bad stations in
  the first draft).

### D3. The skin answers the console

| Face | Colour | Where it comes from | Where it goes |
|---|---|---|---|
| Hull | Champagne `#c9bfa9`, roughness 0.5, metalness 0.35 | The console's plate | Everywhere |
| Glass | Dark smoked glass `#1b150d`, roughness 0.18 | The console's wells | Bands of the side, galleries, the faces where hulls part |
| Light | Amber `#ffc865` | A reading | Belts, drives, the Loop's beamline, apertures |

- **Clay** swaps in plain grey, so the massing alone can be judged.
- **Normals** are shared across faces within 40 degrees, worked out over the whole skin at once. The rolls read smooth,
  the caps stay crisp, and no seam shows where a branch leaves its body.
- **Provisional.** The colours are the console's. Whether the hulls wear its palette is survey I3.

### D4. The twelve blockouts

Measured from the built geometry. All about frigate size, beside the Concord Petrel's 88 m.

| # | Blockout | Tradition | How it is grown | Size, m | Triangles |
|---|---|---|---|---|---|
| 1 | Case | Calibrators | One body in 8:3:1, a glass band above the lit belt | 104 x 39 x 13 | 412 |
| 2 | Stepped deck | Calibrators | A shelf in the section; at the bow the upper deck splits off as a visor, the lower hull as its own bow | 107 x 40 x 16 | 564 |
| 3 | Runners | Calibrators | The section split at the belt and down the centreline: the case's nose above, two runners below reaching 31 m past it | 122 x 31 x 16 | 636 |
| 4 | Twin hulls | Open Notebook | Split down the centreline 40 m from the stern; the halves part 14 m, their facing sides glass | 104 x 47 x 12 | 630 |
| 5 | Slotted | Open Notebook | Split at the belt; the decks part about 5 m for 40 m and meet again, the slot's walls glass | 104 x 26 x 22 | 656 |
| 6 | Waisted baseline | Long Baseline | One body: two stations about 70 m apart, joined by a waist 36 m long and 8 m across | 106 x 26 x 14 | 676 |
| 7 | Spread arms | Long Baseline | Split in four: the crown and keel close as a short head, the flanks reach on as arms 52 m apart at their glazed tips | 104 x 65 x 12 | 644 |
| 8 | Long spar | Long Baseline | The crown splits off as a spar reaching about 70 m forward to a glazed station | 116 x 28 x 15 | 538 |
| 9 | Gallery bow | Field Stations | Split at the belt: the upper bow all glass, sweeping down, over a deep chin | 106 x 32 x 20 | 566 |
| 10 | Terraces | Field Stations | Three decks stepping down to the bow, each riser glass | 104 x 32 x 19 | 660 |
| 11 | Loop | Collider Halls | Split down the centreline; the halves bow out round an opening about 23 m across lit along its walls and meet again | 104 x 41 x 11 | 772 |
| 12 | Through-beam | Collider Halls | A long slender body lit end to end, with a stepped hall amidships | 118 x 24 x 16 | 588 |

- **The stereotype test (C5).** Each line above describes a working shape (a case, a bench, a rangefinder, a gallery, a
  beamline) without naming the species, and none is a picture of a body.
- **Literals.** None is shaped like an instrument. The names say what the hull does, not what it resembles.
- **Provisional.** Their sizes and every proportion stay provisional until the classes are set (survey I4). Their rooms
  come first once picked (task 3.1).

### D5. One grown hull: `docs/mockups/lib/hull-grow.js`

**The lift.** The Hunt page held the only copy of the grown skin (`Skin`, `seg`, `cutLine`, `at3`). A second faction
needs the same code, so it is lifted into `docs/mockups/lib/hull-grow.js`, inlined into both pages (CLAUDE.md 6.1). Each
faction passes its own section layout and materials. The Hunt page keeps its `hsec`, `underHood` and `msec`.

**What it gains.** Each addition is off by default, so the Hunt's ships are unchanged:
- **A face of another kind.** A glow selector may name a kind (`'well'`), not only light.
- **An opening.** A branch may end open (`end: 'open'`), and a body may start open (`stern: 'open'`). Two branches lofted
  back to their split shape at one station are then closed by a new body. This grows the Slotted and the Loop.
- **Other ends.** A branch may end in a drive, and a cap may be glass or light (`capKind`).
- **Smooth normals** over the whole skin (`crease`).
- **Fan caps** (`fanCaps`). The triangulator drops the points of a straight cut line, so a cap met its bands at T-junctions
  and left the skin open; the first draft had 44 open edges in three blockouts. A cap that is star-shaped about its centre
  is closed by a fan through every point, and any other cap falls back to the triangulator.

**Checked.**
- The Hunt's six ships, exported through `window.MOCKUP_EXPORT_OBJ` before and after the lift, are identical byte for byte.
- Every Institute blockout's skin reports zero open edges (`window.MOCKUP_MEASURE`).

| Considered | Why not |
|---|---|
| A second copy of `Skin` on the Institute page | CLAUDE.md 6.1: the same behaviour is the same code |
| Changing the Hunt's caps to fans as well | It would change baked ships whose maps are keyed to their geometry; the Hunt's ships are approved for implementation |

### D6. The page

- **The board.** Four columns by three rows, the traditions in order. Bows point up the screen. A label stands over each
  hull, and on a phone shows only its number.
- **Views.** The board, from above and from ahead. Each blockout can be seen alone from its bow quarter, the side and
  above. The Concord Petrel can be stood beside the board or the ship.
- **The list.** Grouped by tradition, each group led by its source. Each blockout gives its one-line tell, and in the
  focused view its size, triangles, closure and tradition.
- **Shots.** `docs/screenshots/mockups/kelvane-ship-concepts/`:
  - the board views and blockout 1 at desktop, phone landscape and portrait;
  - every blockout from its quarter and its side at desktop.
- **Fit.** `tools/mockups/dom_fit.mjs` passes at every size.

## Open questions

| Id | Question | Recommendation (provisional) |
|---|---|---|
| I1 | Which of the five traditions does the Institute keep (or how is its culture set)? | Keep all five as the Institute's schools for now; the owner renames, merges or drops |
| I2 | Which blockouts go on to be grown in detail? | The owner's picks; until then none is detailed |
| I3 | Do the hulls wear the console's palette (champagne, dark glass, amber)? | Yes, as drawn: the exterior complements the console (X1) |
| I4 | Which class comes first, and at what size? | A frigate of about 105 m, as the Hunt's; the research cruiser the console flies today follows |
| K1 | The Institute's names for ships, ranks and officers (from kelvane-battle-console) | Placeholders until the owner sets the culture |
