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

## Version 2: conversions (owner, 2026-11-10)

The owner, on version 1: "a lot of this resumble other factions, you should think about how pirate ships work? they are
usualy modified freighters. So I would avoid sculpting random shapes and consider how a pirate faction might design
their ships - out of scrapped parts or stolen parts from other ships".

Version 1's decisions D1, D2 and D4 are superseded: the five schools, the twelve sculpted hulls and the full section
layout are gone from the page. D3's palette stays, for what the Syndicate makes itself.

### D6. How a Vos ship comes to be

A clan has no shipyard that builds hulls. It has a breaking yard. So a Vos ship is never designed from a blank sheet:

1. **It starts as a freighter.** Freighters are what raiders meet, what they can take, and what their crews know how to
   run. The four in the game (mockup 24: the Concord's, the Krai's, the Hunt's and the Institute's) are the bases.
2. **It is cut and stripped for a job.** Containers come off, a bow is cut away, a frame is emptied.
3. **Taken parts go on.** Nacelles, a saucer, a bow, a whole small warship, fighters: each from another faction's ship.
4. **The yard joins them.** Collars round each cut, struts, a welded prow, gun tubs. This is the only Vos-made metal.

Rules the page holds to:
- **Every taken part is the game's own ship.** The page inlines the glbs the game flies (`assets/ships/`) and copies
  their triangles; no part is redrawn (CLAUDE.md 6.1).
- **Nothing is scaled.** A crew uses what it could take, at its true size. A cruiser's nacelle is small beside a 300 m
  freighter, and it stays small.
- **A cut is a clean line.** Triangles crossing a cut are clipped on it, and a collar a little larger than the measured
  section covers the open end.
- **Origin stays readable.** Each taken part wears a dulled version of its maker's colour; Vos-made metal is rust with
  a brass rail.
- **Mirror symmetry** about the centre plane, as every ship (the owner's single axis symmetry).

**The hull rule.** CLAUDE.md section 10 says no stretched primitive is a visible part of a hull. The taken parts are
whole pieces of approved ships, not primitives. The joining pieces are grown on `hull-grow.js` from the Syndicate's
section (`vsec`, now 18 points with no deckhouse), each a closed skin. The containers are the game's container, which
is a box by nature (mockup 24).

### D7. The eight conversions

Measured from the built geometry (`window.MOCKUP_MEASURE`).

| # | Conversion | Job | Starts as | Taken | Vos-made | Size, m |
|---|---|---|---|---|---|---|
| 1 | Wolf | Pass as a freighter | Concord freighter, whole, 28 of its 32 containers | Nothing that shows | Four gun tubs where containers were | 300 x 44 x 56 |
| 2 | Chaser | Catch a freighter | Krai freighter, stripped, its bow cut off at 190 m | Both nacelles of a Concord Albatross | A collar and a welded prow; four struts | 226 x 60 x 46 |
| 3 | Boarder | Board a freighter | Hunt freighter, cut 60 m short at the bow | The forward half of a Hunt frigate (the Forked prow) | The collar joining them | 339 x 52 x 36 |
| 4 | Gun barge | Outgun an escort | Institute freighter, its frame emptied forward | A whole Krai corvette | Four struts holding it in the frame | 274 x 80 x 25 |
| 5 | Twin haul | Carry the clan | Two Concord freighters, 70 m apart | The saucer and command deck of a Concord Petrel | Five struts; a pedestal under the saucer | 300 x 92 x 65 |
| 6 | Prize frigate | Keep what you took | No freighter: a captured Krai frigate's hull | Both nacelles of a Concord Albatross; two containers | Four struts | 110 x 56 x 34 |
| 7 | Tender | Launch small craft | Institute freighter, whole | Twelve Institute fighters | A deck welded across the frame | 274 x 80 x 25 |
| 8 | Saucer runner | Outrun a patrol | Hunt freighter, only its aft 120 m | The saucer and command deck of a Concord Petrel | The collar joining them | 213 x 52 x 36 |

- **Sizes.** Seven of the eight are freighter-sized, 213-339 m, longer than any faction's warship. That is what a
  converted freighter is. Whether the Vos also fly small ships is survey Y5.
- **Not from the body, and no literals.** Nothing here is shaped at all: every form is a freighter or a taken part.
- **Why these jobs.** Each is something a raider must do to a convoy: get close unnoticed, catch it, board it, beat its
  escort, carry the take and the crew home, put small craft out, and get away.

### D8. The page, version 2

- The board is four columns by two rows, 330 m apart, on a 100 m grid.
- A focused ship is framed by its own size. Its facts give what it starts as and what was taken.
- The list gives each conversion's job and its one-line tell. The Concord Petrel (the game's glb) stands in for scale.
- The page inlines ten glbs and the ship manifest (for the freighters' containers): 3.5 MB.

| Considered | Why not |
|---|---|
| Keeping version 1's sculpted hulls beside the conversions | The owner's note rejects sculpted shapes for this faction |
| Scaling a taken part to suit the freighter | A crew cannot scale a nacelle; true size is what makes it read as taken |
| Redrawing freighters and parts as simpler shapes | A second copy of each hull (CLAUDE.md 6.1), and it would stop being the game's ship |
| Unlike port and starboard sides | The owner's rule: single axis symmetry |

## Version 3: battle freighters (owner, 2026-11-10)

The owner, on version 2: "6 and 5 are cool, but I would like you to create some unique battle freighter designs".

- **Kept:** 5 Twin haul and 6 Prize frigate, as they are.
- **Withdrawn:** 1 Wolf, 2 Chaser, 3 Boarder, 4 Gun barge, 7 Tender and 8 Saucer runner. Each was another faction's
  freighter with one thing swapped, so it read as that faction's ship.
- **New:** six battle freighters, numbered 9 to 14, whose layouts are the Syndicate's own.

### D9. What makes a battle freighter the Vos's own

D6 still holds: a clan cuts and welds, and its ships are made of what it took. What changes is how much it rearranges.

1. **Only a freighter's working parts are kept**: its drive block and a length of its spine, cut from the game's glb
   (`base(key, length)`), never scaled.
2. **The layout is new.** The spine is a keel to hang things on, and what hangs on it is arranged for fighting. That
   arrangement is the design, and no other faction's ship has it.
3. **A container is never just cargo.** It is the game's container (12 by 12 by 24 m) put to a fighting use: a gun
   house, a missile pod, a magazine, quarters for a boarding crew, a floor of a tower.
4. **Vos metal does the fighting work**: armour, arms, hubs, prows and guns, in rust plate with a brass rail, each a
   closed skin grown on `hull-grow.js`. A gun's mouth is lit verdigris.
5. **Taken warship parts are accents**: a cruiser's nacelles, a frigate's saucer.

### D10. The six battle freighters

Measured from the built geometry (`window.MOCKUP_MEASURE`).

| # | Ship | Job | Built on | The layout | Size, m |
|---|---|---|---|---|---|
| 9 | Broadside | Fight an escort side on | A Krai freighter's drive block and 195 m of its tall spine | Seven gun-house containers down each flank, each muzzle through its outer wall, armour above and below, a welded prow | 297 x 56 x 46 |
| 10 | Drum | Break a convoy from far off | A Concord freighter's drive block and 130 m of spine | Twelve missile-pod containers in two rings of six round the spine, a hub of plate under each ring | 206 x 58 x 62 |
| 11 | Cross | Guns that bear on any side | A Hunt freighter's drive block and 150 m of spine | Four arms 62 m long near the bow, a gun at each tip, two magazines on each; a Concord Albatross's nacelles on the side arms | 242 x 135 x 134 |
| 12 | Ram | Break through and board | A Krai freighter's drive block and 100 m of spine | A wedge of armour 64 m across and 90 m long; six containers of boarding quarters behind its shoulders; Albatross nacelles | 242 x 79 x 47 |
| 13 | Castle | Command a raid | A Concord freighter's drive block and 190 m of spine | Twelve containers stacked three high on the drive block, a Concord Petrel's saucer on top; five guns along the spine | 276 x 44 x 108 |
| 14 | Delta | Run down a convoy and carry it off | A Hunt freighter's drive block and 190 m of spine | Twenty containers in rows widening aft as two swept wings on a welded deck, an armoured edge and a tip gun on each | 284 x 158 x 32 |

- **Silhouettes.** Each reads from one view: a long slab with a row of ports; a drum on a stick; a cross; an arrowhead on
  a shaft; a tower over a low deck; a delta.
- **Sizes.** 206 to 297 m, freighter-sized, as their bases are.
- **Literals and bodies.** The names say the layout, as a gunner would. Nothing is drawn from the Vos's bodies, and none
  is a picture of a sailing ship, though Broadside and Castle borrow those words for what the layouts do.
- **Rough.** These are massings: the Drum's containers and the Cross's arms are placed by rule, and none has rooms yet.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| Y1 | Is this how the Vos get their ships: freighter parts and taken parts rearranged, nothing built from new? | Yes |
| Y2 | Which battle freighters go on, beside 5 and 6? | 9 Broadside, 10 Drum, 12 Ram, 13 Castle |
| Y3 | Do taken parts keep their makers' colours, with Vos metal in rust and brass? | Yes, dulled, with clan paint added in detail |
| Y4 | May a Vos ship carry parts of any faction, or only its neighbours'? | Any faction the clan could have raided |
| Y5 | Which becomes which class? | Prize frigate the small ship, Ram and Drum the middle, Broadside and Castle the capitals, Twin haul the clan's home ship |
