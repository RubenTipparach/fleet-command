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
| 11 | Loop (see D7 for the loop family, 13-19) | Collider Halls | Split down the centreline; the halves bow out round an opening about 23 m across lit along its walls and meet again | 104 x 41 x 11 | 772 |
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

### D7. Version 3: the loop family (owner, 2026-11-06)

**The ask.** "l1 loop looks cool, can you make me some more variations of ships with loops? horizontal, vertical, both?"

**The decision.** Seven more loop ships, 13 to 19, join the Loop (11) in the Collider Halls. A ring may lie flat (a loop
seen from above), stand on edge (seen from the side) or both. Each loop is grown one way, by `loopOf()` on the page:
- **Split.** The body's section is split into regions:
  - `halves`: port and starboard, down the centreline;
  - `decks`: upper and lower, at the belt's upper edge;
  - `quarters`: both cuts, through the centre at the belt's lower edge.
- **Bow apart.** Each region lofts through 12 stations (14 on the Wide ring) on a smooth arch. Its offset and scale follow
  sin(pi t)^0.7 (0.55 on the Wide ring, for longer straights), and it comes back to its split shape.
- **Close.** A new body from that station closes the loop (`stern: 'open'`). `bow()` then takes it to a rounded nose with a
  lit aperture.
- **Light.** The walls that face the opening are lit: they are the beamline.

| # | Blockout | Loop | Offset of each arc, scale | Size, m | Triangles |
|---|---|---|---|---|---|
| 13 | Long loop | Horizontal, 78 m long | 8 m out, 0.55 wide | 104 x 32 x 10 | 1,280 |
| 14 | Twin loops | Horizontal, two in a row (40 m and 36 m) round an 8 m hall | 8.5 m, then 6 m | 106 x 35 x 11 | 2,196 |
| 15 | Wide ring | Horizontal, 70 m long, nearly round, thin low arcs | 19 m out, 0.5 wide | 104 x 57 x 8 | 1,424 |
| 16 | Tall loop | Vertical, 72 m long | 9 m up and down, 0.6 tall | 104 x 18 x 31 | 1,376 |
| 17 | Keel loop | Vertical: the upper hull nearly straight, the keel dropping 12 m | 0.8 m up; 12 m down | 104 x 20 x 25 | 1,376 |
| 18 | Crossed loops | Both, in series: a horizontal loop aft, a vertical loop forward, an 8 m hall between | 9 m out; 7 m up and down | 106 x 35 x 24 | 2,292 |
| 19 | Quartered loop | Both at once: four quarters bow out diagonally round one hollow | 7 m out and 6 m up or down, 0.7 | 104 x 33 x 24 | 1,808 |

**Checked.** Every one is closed (zero open edges) and passes the section check.

**On the page.** A new switch, All ships or Loop family, lays out only that family on the board. The list groups the
loops as horizontal, vertical and both. Each loop ship is shot from its quarter, its side, above and ahead.

**Provisional.** The loops' purpose is the Collider Halls' (D1), which is survey I1. Their sizes and proportions are
provisional, as in D4.

### D8. Version 4: the line (owner, 2026-11-06)

**The ask.** "12,11,18.19,17 all really good shapes, start detailing these, use more complex ones for larger captial ships
like destroyer, cruiser, batlecruiser and carrier, we might want to theme them after advanced science and research vessels,
this faction will specialize in scientific missions". Then: "12 is no go use 5"; and, of the first detail pass, "Mind the
clean and sterile and almost scientific aesthetics. They would have decorated their ships with smooth round pieces rather
than industrial greebles".

**What the owner set.**
- **The picks.** Blockouts 5, 11, 17, 18 and 19 (survey I2). The owner first picked 12, then: "12 is no go use 5".
- **The classes.** The more complex blockouts go to the larger ships (survey I4).
- **The culture, in part.** The Institute specialises in scientific missions, and its ships are advanced science and
  research vessels (survey I1, in part).
- **Two traditions.** The four larger ships come from the Collider Halls, the frigate from the Open Notebook (D1). The
  other three schools are survey I5.

**The decision.** The picks, simplest to most complex, become the five classes. Each is grown again at its size, with the
structure its scientific work needs. Every class is a research vessel first. On the four larger ships the accelerator is
the main instrument and also the lance; the frigate carries its instruments twice.

| Code | Class (placeholder name, K1) | From | Its work | What grows at its size | Size, m | Triangles |
|---|---|---|---|---|---|---|
| FF | Parallax-class survey frigate | 5 Slotted | Charts a system on its own, every reading taken twice | Two identical instrument decks round a glazed slot 74 m long, joined at a glass nose; glass along the stern's upper side | 126 x 24 x 25 | 656 |
| DD | Candour-class beamline destroyer | 11 Loop | Escort and picket, its beam held ready | A stepped instrument deck aft with glass, the storage ring (an opening 50 m long), the aperture | 136 x 47 x 17 | 1,516 |
| CA | Patience-class research cruiser | 17 Keel loop | Research: laboratory decks over the ring | Stepped laboratory decks the length of the upper hull, glass on every riser; the keel ring dropping 15 m | 166 x 26 x 34 | 1,884 |
| BC | Rigour-class collider battlecruiser | 18 Crossed loops | Line of battle: two beams crossing | A stepped reactor hall, a ring seen from above, a broad collision hall with glass, a ring seen from the side | 196 x 49 x 33 | 2,808 |
| CV | Curiosity-class probe carrier | 19 Quartered loop | Probes and survey craft | A broad stepped hangar deck aft; four arms round a launch well 120 m long, open above, below and to each side | 214 x 52 x 37 | 2,596 |

- **Checked.** Every class is closed and passes the section check.
- **On the page.** The Line family (the default) shows one class at a time from the bow quarter, the side, above or ahead,
  and a line-up of all five side by side at true scale. The blockout board and the loop family stay as the record, the
  picks marked.
- **Names.** The Patience keeps the name the console's research cruiser already has (kelvane-battle-console D7). All five
  names are placeholders (survey I6).
- **Provisional.**
  - Each class's rooms: the Ship Design Bible designs them first (task 3.1), and the hull is then refitted round them.
  - Its weapons' mounts.
  - The carrier's bays into the launch well.

### D9. Version 5: the first detail pass (owner, 2026-11-06: "start detailing these")

**The method.** Each class is detailed the way the Hunt's ships were (the ship-detailing skill):
1. The page hands its hull out as OBJ (`window.MOCKUP_EXPORT_OBJ`, through `tools/mockups/export_blockouts.mjs`, which now
   takes the page).
2. `tools/blender/painted_bake.py --faction kelvane --ship <slug>` unwraps it once.
3. `tools/blender/hull_detail.py` builds a high-poly detail layer over it from `data/ships/kelvane/hull_detail.json`.
4. The detail is baked onto the ship's own UVs and painted into a painted map, a PBR set (base colour, ORM, tangent normals,
   emission) and a hull-light map.

**The Institute's language is data, not a copy of the tools (CLAUDE.md 6.1).** The tools gained faction options. The Hunt
keeps its own values, now read from its data. Its paint is moved into `data/ships/ssarith/hull_detail.json` with exactly
the values the code had.

| Option | The Hunt | The Institute | Where it comes from |
|---|---|---|---|
| `cuts` | `hunt`: swept chevrons, raked flanks | `grid`: straight cuts square to the ship at stations of differing spacing, a spine plate that steps at every station, seams along the plating and the belt | The Calibrators' "true faces" (D1) |
| `smooth_deg` | none (flat: chamfered hulls) | 40: the hull shaded smooth for the tangent bake, and the page draws the same normals | The rolled case |
| `facing_min` | 0.55 (chamfers bare) | 0.38 (the rounded hull's rolls plated too) | The rolled case (D2) |
| Gaps, trenches | 0.5-1.0 m, trenches 2.6-4.6 m in 40 %, overlays on plates over 22 m2, greebles 1.6 a m2 | Seams of 0.2-0.32 m, no trenches, no overlays, no greebles, plates lifted no more than 0.05 m | Clean and sterile (owner: "Mind the clean and sterile and almost scientific aesthetics") |
| `paint` | Olives and khakis, a bronze accent | Champagne enamel in seven tones, a brass accent, darker champagne sub-armour, a dark bronze under layer, smoked-glass domes and wells, the logo's velvet red for liveries, the console's dark ink | The console (kelvane-battle-console D2) |
| Strips | `centre`: a lit line down a keel | `whole`: the belt and the beamline walls lit amber | The amber of a reading |
| Glass | (none) | The hull's dark-glass faces (`well_*`) bare of armour, baked as their own kind (`glass`) | The console's wells |

**What each ship carries** (`data/ships/kelvane/hull_detail.json`), smooth and round, never machinery (owner: "They would
have decorated their ships with smooth round pieces rather than industrial greebles"):
- smoked-glass sensor domes on its halls and decks, in collars, never strapped;
- **pods**, a new feature of `hull_detail.py`: smooth ellipsoids, round in section, sunk into the plating to stand 0.7-1.1 m
  proud, 6-14 m long, plated and painted as the champagne round them;
- amber jewel lamps;
- two red livery bands toward the bow;
- the KID logo on the top plating, under its own flood;
- **a measuring scale engraved down each flank**: a tick every 1 m and a long tick every 5 m, above the belt (the Institute
  marks every surface it works on, the console's backing rule, D4 of kelvane-battle-console);
- floods, position lights, beacons and strobes, placed as the Hunt's are.

**The logo.** It is drawn from its own file, `assets/ui/kelvane/emblem.svg`. `tools/mockups/emblem_masks.mjs` now reads a
faction's hull logo from its SVG: the satin oval is the field, the plot is the device, and the rim is left to the champagne
plate. The shared emblem library still holds the Institute's old tuning-fork sketch; the faction-emblems change brings it
up to date.

**On the page.**
- **The viewer.** The shared `docs/mockups/lib/baked-ship.js` shows the baked ships: geometry, maps, the Painted and PBR
  looks with the hull lights, and blinking fixtures.
- **The switches.** Look: Painted, PBR, Champagne, Clay. Light: Studio, Night.
- **Duplication, reported.** The Hunt page still has its own copy of the same rules, woven into its damage, panes and
  breakup. Moving it onto the library is task 3.4.

**Not yet** (as the Hunt's ships took them in versions 14 and 15):
- windows and the rooms behind them, once each class is planned rooms first;
- the carrier's bays into the launch well;
- battle damage and breaking apart.

## Open questions

| Id | Question | Recommendation (provisional) |
|---|---|---|
| I1 | Which of the five traditions does the Institute keep (or how is its culture set)? | In part decided (D8): the Institute specialises in scientific missions; the line is the Collider Halls' and the Open Notebook's frigate. The rest is I5 |
| I2 | Which blockouts go on to be grown in detail? | **Decided** (D8): 5, 11, 17, 18, 19 |
| I3 | Do the hulls wear the console's palette (champagne, dark glass, amber)? | Yes, as drawn: the exterior complements the console (X1) |
| I4 | Which class comes first, and at what size? | **Decided** (D8): all five at once, frigate to carrier, 126-214 m (sizes provisional) |
| I5 | What becomes of the other four schools? | Keep them as schools inside the Institute for its stations, support ships and the console |
| I6 | The class names (Parallax, Candour, Patience, Rigour, Curiosity) | Placeholders until the owner names the Institute's ships (K1) |
| K1 | The Institute's names for ships, ranks and officers (from kelvane-battle-console) | Placeholders until the owner sets the culture |
