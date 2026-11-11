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

## Version 4: blunt noses, and the picks (owner, 2026-11-10)

The owner, on version 3: "10,9,11 and 15, why did you get rid of 5 and 6? I also dont like pointy noses, please fix
that".

- **Picks.** 9 Broadside, 10 Drum and 11 Cross go on. There is no 15 on the board; which ship was meant is survey Y6.
  Until it is answered 12 Ram, 13 Castle and 14 Delta all stay on the page.
- **5 and 6 were never removed.** They were last on the board and in the list, where they were easy to miss. They now
  come first, each marked "(kept)".
- **No pointed bows (a rule for every Vos ship from here on).** Every bow the Syndicate welds ends in a flat face:
  - 9 Broadside, 13 Castle and 14 Delta: a blunt armoured bulkhead, tapered only a little from the section behind it;
  - 10 Drum and 11 Cross: a flat glazed face;
  - 12 Ram: no longer a wedge to a point but a hammer head, a block 64 m across with a flat face 52 m wide.
- **Not changed.** 5 Twin haul's bows are its Concord freighters' own tapered ends, and 6 Prize frigate's is the Krai
  frigate's own needle. Both are taken parts the owner had already called "cool" as they are, so they are left;
  whether a crew cuts them blunt too is survey Y7.

Sizes after the change, in metres: 9 Broadside 271 x 56 x 46; 10 Drum 190 x 58 x 62; 11 Cross 234 x 135 x 134;
12 Ram 212 x 79 x 47; 13 Castle 264 x 44 x 108; 14 Delta 274 x 158 x 32.

## Version 5: six specialists (owner, 2026-11-10)

The owner, in three messages:
- "my bad, the ships I wanted were not 5,6 it was saucer runner and boarder";
- "pirate ships instead of having larger and larger class, they are all roughly "cruiser" sized ships with hull on the
  lower side, but all are specialized in: Boarding, Mine laying, Tractor Beam, Rock breaking (can split up asteroids
  causing high damage to ships nearb them including it self) and figher launching";
- "oh and of course, disguising themselves as freighters too!".

### D11. The Vos line is trades, not sizes

- **No class ladder.** Every Vos ship is about cruiser size. On the board they run 122 to 185 m; the game's cruisers
  are 111 m (Concord), 134 m (Krai), 150 m (Hunt) and 166 m (Institute).
- **A weaker hull.** A Vos ship has fewer hull points than a warship of its size ("hull on the lower side"). It is a
  freighter's drive block with a trade bolted on, not an armoured warship. The number is set with its class data.
- **One trade a ship.** Five trades the owner named, each a system a captain uses in battle, and the disguise.
- **The disguise** is read here as something every Vos ship does, since every one is made of freighter parts, with one
  ship that does it best. Whether it is a trade of its own or a trait of all is survey Y9.

### D12. The six specialists

Each is one of the owner's picks, cut down to a freighter's drive block and a short length of spine. Which hull takes
which trade is ours (survey Y8). Measured from the built geometry.

| # | Ship | Trade | Built on | What does the work | Size, m |
|---|---|---|---|---|---|
| 3 | Boarder | Boarding | A Hunt freighter's drive block and 30 m of spine | A Hunt frigate's forked bow, to hold a prize; two containers of boarding quarters | 161 x 52 x 32 |
| 10 | Drum | Mine laying | A Concord freighter's drive block and 60 m of spine | Twelve containers of mines in two rings, a chute in each one's outer face | 122 x 64 x 64 |
| 11 | Cross | Tractor beams | A Hunt freighter's drive block and 50 m of spine | Four arms with a tractor emitter at each tip, 80 m apart; the machinery in a container on each arm | 134 x 95 x 94 |
| 12 | Ram | Rock breaking | A Krai freighter's drive block and 40 m of spine | The breaker's driver in the flat face of a hammer head of armour 60 m across; Albatross nacelles to get clear | 148 x 79 x 46 |
| 8 | Saucer runner | Fighter launching | A Hunt freighter's drive block and 20 m of spine | A deck over the drive block for six stolen Institute fighters; a Concord Petrel's saucer as flight control | 185 x 52 x 38 |
| 9 | Broadside | Passing as a freighter | A Krai freighter's drive block and 104 m of spine | Eight containers on its flanks, each a gun house behind its doors | 170 x 46 x 46 |

- **Withdrawn:** 5 Twin haul, 6 Prize frigate, 13 Castle and 14 Delta. The owner's "15" matched no ship; with six
  trades and six picks filling them it is no longer asked, and Y8 offers the Castle and the Delta again.
- **Rock breaking in the game.** The trade needs asteroids in a battle that can be split, and a blast that hurts every
  ship near it, the breaker included. That is its own change, after `campaign-space` 2.3 (rocks in battles). The Ram's
  armoured head is drawn for it: the ship hides behind it when the pieces fly.
- **Trades the game already has:** boarding (`boarding-party`), mines and tractors (`data/battle/systems/tractor.json`),
  fighters (the carriers' parked craft). The disguise is new.
- **The Boarder's bow** is a Hunt frigate's fork, a taken part with two pointed tines. Version 4's rule covers what the
  Syndicate welds, so it is left as taken; whether a crew blunts it is survey Y7.

## Version 6: the Syndicate's own fighters (owner, 2026-11-10)

The owner: "pirate fighters need to be unique, like more fighter looking!". Version 5's Saucer runner carried six stolen
Institute fighters, which said nothing about the Vos.

### D13. The one thing the Vos build

A fighter is about 16 m long, small enough for a breaking yard to weld whole. So the fighters are the exception to D6:
they are built new, in the Syndicate's flat plate, round one or two salvaged engines. Every other Vos ship is still
freighter parts and taken parts.

Each has what makes a fighter read as one: wings, guns set out where they clear the nose, a big engine for its size, and
a glazed nose for the pilot. The nose is blunt (version 4's rule).

| # | Fighter | Layout | Engines | Guns | Size, m |
|---|---|---|---|---|---|
| 21 | Cutter | Wings swept back from the shoulders, a tall fin | 1 | 2, one at each wing tip | 15.6 x 18.6 x 6.0 |
| 22 | Twin boom | Two booms joined by a wing and a tailplane, the pilot in a pod between them | 2 | 2, under the pod | 15.7 x 16.8 x 3.6 |
| 23 | Stack | Two short wings one above the other, braced at the tips | 1 | 4, one at each wing tip | 13.6 x 13.9 x 6.3 |

- **On the Saucer runner:** six Cutters, in place of the Institute fighters. Which fighter the Vos fly is survey Y11.
- **Built with** the same pieces as the ships' Vos-made metal (`made`, `beam`, `gun`), each a closed skin grown on
  `hull-grow.js`; an engine is a drive recessed into a body's stern. The section's rail and deck edge now scale down
  with a small body, which leaves the ships as they were.
- **Scale.** The Institute's fighter is 14 m. On the board the fighters are specks beside 150 m ships; each is framed
  by its own size when picked.

## Version 7: the fighters, detailed (owner, 2026-11-10)

The owner: "love all 3 of those fighters, lets stick to 21 as standard fighter, twin boom as the shuttle stand in ( but
also they will be used as bombers as well) and lets get highres versions fo these built out".

### D14. The fighters' jobs

| Craft | Job | Notes |
|---|---|---|
| 21 Cutter | The standard fighter | What every Vos carrier deck flies; the Saucer runner's six |
| 22 Twin boom | The shuttle stand-in, and a bomber | Carries boarders and cargo where another navy would fly a shuttle; with bombs slung under the pod it is the Vos's bomber |
| 23 Stack | None yet | Liked, kept on the board, not detailed |

The owner's words on the detail ("highres versions") are taken as approval for build of these two fighters only. The
six specialists stay drafted.

### D15. The Breakers' recipe (the `ship-detailing` method)

**The culture**, as accepted: free clans of raiders; "luck, loot, loyalty to the crew, nothing else". The palette's
meanings (`console-skins/design.md`, "Vos Syndicate"): rust is salvage (everything they own was someone's), brass is loot
and luck, verdigris is the spoils kept. The tradition is the Breakers' (the salvage yards, D1), which is still a draft
(survey Y1).

**How the Breakers build** (principles):

1. *Everything was someone's.* Plate is cut from broken hulls, so no two plates match in colour, size or thickness.
2. *The join is left plain.* A weld or a patch is not hidden or faired; it is how the crew knows the work is sound.
3. *Brass is luck.* The one metal kept bright; it marks what the crew trusts their lives to.
4. *Nothing else.* No finish for its own sake: no paint scheme, no fairing, no emblem until a clan has earned one.
5. *The spoils kept.* Light is verdigris: the drive, the gun mouths, the instruments behind the glass.

**The recipe** (numbers in `data/ships/vos/hull_detail.json`):

| Technique | Setting | From |
|---|---|---|
| Hull form | The page's welded bodies (`made`, `beam`, `gun`), exported by `export_blockouts.mjs` | D13 |
| Plating | Uneven stations (0.9 to 2.8 m apart) with flank cuts raked up to 0.3 either way | 1 |
| Relief | 6 cm plate, each plate proud by its own amount up to 4 cm, welds 3 to 6 cm wide, a few open trenches | 1, 2 |
| Paint: plates | A patchwork: ten rusts and three makers' colours gone dull (a grey, an olive, a sand), about one plate in five; no whole plate brass | 1, 3 |
| Liveries | Two brass bands a ship, at the canopy and the engine: the parts a pilot's life hangs on | 3 |
| Emblem | None | 4 (and the Vos emblem is a placeholder) |
| Features | One canopy bubble a craft, dark glass under two crossed straps | D13 (the glazed nose); 2 |
| Exposed machinery, kit, marks | None on a 15 m craft | 4 |
| Light | The render (sky, key and bounce), never the painted light; under layer a dark rust, never black | skill rules 3, 4 |
| Glow | Verdigris drive, gun mouths and instrument cells behind the canopy glass | 5 |
| Hull lights | Small floods forward along the body; a red beacon on the spine; nav lights by the tool's rule, every lamp at 0.22 of a warship's size (`lamp_k`, new in `hull_detail.py`) | Ship Design Bible 9 |

- **Symmetric**, by the skill's rule 2: the patchwork is mirrored about the keel. Whether the Vos, of all people,
  should be patched unevenly side to side is survey Y12.
- **Cost** (measured): the Cutter 512 triangles and the Twin boom 928, unchanged from the blockouts; one 2048 px map set
  each (24 m and 27 m of hull to a UV unit, so about 1.2 cm a texel); 17 and 14 minutes a bake on 4 cloud cores. The page
  grew from 3.5 MB to 6.7 MB.
- **Not yet done:** a room plan (a fighter has a cockpit only), bombs or a boarding hatch modelled on the Twin boom, the
  Vos emblem (a placeholder), and anything in the game: the bakes sit in `assets/ships/vos/baked/`, which Godot does not
  import, until the fighters are approved for implementation.

## Version 8: the specialists' rooms, and the Saucer runner detailed (owner, 2026-11-10)

The owner, asked whether the six specialists were detailed too: "ok go ahead, room plan should be included in that
detailing". So: a room plan for each of the six, then the Saucer runner detailed with its rooms (windows where its people
are), shown before the other five are detailed.

### D16. A conversion's rooms are fitted into its hull

A warship is designed inside out: rooms first, the hull fitted round them (Ship Design Bible chapter 2, H1). A Vos ship is
the other way round: the hull is a freighter's drive block and parts taken from other ships, which exist before anyone
plans a room in them. So each plan (`data/ships/vos/<ship>_plan.json`, the same schema as the Concord's and Krai's) lays
its rooms out inside the parts, in the page's build frame (the drive block's stern at z = 0).

- **Measured, not guessed.** The parts are not boxes. Slicing them on the page (`window.MOCKUP_INSIDE`) showed the Hunt
  block is 48 m across at the top and 34 m below the waist; the Krai block is a long octagon with a ridge where its deck D
  would be; the Concord block has a tower 24 m across from z 20 to 44; the Petrel's body, where the Saucer runner took it,
  is 20 m across and two decks deep; the Hunt prow on the Boarder is a blade 8 m across, too slim for any room.
- **The check.** `window.MOCKUP_ROOMS(n)` grows every room by the plan's clearance and structure (1.05 m) and tests 26
  points on it against every part (a ray-parity test per part, three directions, a majority); with the shared
  `FCShipPlan.check`, every room is on a corridor and none overlap. All six pass: 0 rooms outside, 0 plan failures.
- **The fitter** (`tools/mockups/fit_rooms.mjs`, a measurement instrument) trimmed each room's outer edge or ends until it
  fitted; the plan files are the source. Where a room could not fit it was redrawn by hand (the Boarder's tube through the
  prow was dropped; the Drum's bridge moved into the block's tower; the Krai blocks' bridges moved down a deck).
- **Functional rooms only**, in our own words: bridge, crew bunks, mess, medbay, engineering, reactor, hold for taken goods,
  stores, magazine, and each trade's rooms. What the Syndicate calls them is the owner's (survey U7).

| Ship | Crew | Rooms | Its trade's rooms |
|---|---|---|---|
| 8 Saucer runner | 70 | 51 | Fighter workshop, ready room, ordnance and deck crew under the flight deck; spares and fuel in the two containers; the Petrel's body re-laid as flight control, pilots' quarters, briefing room, its old shuttle bay, its reactor still running the saucer |
| 3 Boarder | 90 | 33 | Armoury, brig, muster rooms; boarding crews quartered in the two containers; the boarding lock and grapple control in the collar behind the prow |
| 10 Drum | 40 | 39 | Mine workshop and magazine inside the two rings; mine control in the bow cap. The twelve containers are racks, not rooms |
| 11 Cross | 45 | 39 | Tractor control, emitter power, the beam crews and field coil stores in the hub; crawlways out along the side arms |
| 12 Ram | 40 | 41 | Breaking charges in the two containers; in the head a blast shelter behind its armour, the breaker crews, head machinery and the breaker driver in its flat face |
| 9 Broadside | 60 | 64 | Eight gun houses in the containers, each with its crew's bunks and shell rooms in the spine beside it |

- **On the page**: a Rooms button on each specialist shows its rooms as blocks in their kinds' colours inside a see-through
  hull, with its deck plans drawn in the panel.

### D17. The Saucer runner, detailed

The first specialist detailed, on D15's Breakers' recipe at a warship's scale, with what a conversion adds:

| Technique | Setting | From |
|---|---|---|
| Hull form | The page's parts as built, exported by `export_blockouts.mjs`: 3,498 triangles. Its containers stay the game's placed cargo (as on the freighters) and its six Cutters are their own bakes | D7, D13 |
| Taken parts in their makers' looks | Each part is exported with its maker (`hull_<maker>_*`), baked into a maker pass on the low-poly, and painted in that faction's freighter look (`painted_bake.py` `makers`, `look_paint`), dulled 0.4 to 0.45 | D6 ("origin stays readable"), survey Y3 |
| Vos metal (collar, flight deck) | The Breakers' patchwork at 5 to 14 m stations, 0.25 m plate, square cuts, seams 8 to 14 cm (after the owner's note, below) | D15, principles 1, 2 |
| Windows | From the room plan: every crew and command room meeting the hull gets a row of lit windows at eye height, a quarter dark | Ship Design Bible W1 |
| The Petrel's shuttle bay | A door on each flank of the saucer body, 7 by 2.2 m | its room plan |
| Flight deck lights | Five amber lamps down each edge of the deck | D12 (fighter launching) |
| Brass | One band at the collar, where the Vos welded the Petrel on | principle 3 |
| Emblem | None | principle 4 |

- **Measured.** 3,498 triangles, unchanged; 42,499 faces of detail; one 2048 px map set at 216 m of hull a UV unit (about
  10.5 cm a texel, coarser than the fighters' 1.2 cm: a 3072 px set, as the Krai's large ships have, is the step up if it
  reads soft close in); 80 panes (78 windows and the two bay doors); 25 minutes a bake on 4 cloud cores, 40 s a repaint.
  The page grew to 9.1 MB.
- **Tuned after the first bake.** The Concord body read near white, so a taken part's dulling now pulls toward a darker grey
  (`dull` in `painted_bake.py`) and the Petrel's look is dulled 0.6; its floods burnt out on the pale body at night, so the
  ship's light map is scaled to 3.5.
- **Softer, ordered seams** (owner, 2026-11-11, of the first bake: "lihnes are too harsh, and hull plating lines are
  crossing over each other"). The first bake's welds were 0.25 to 0.5 m wide over a near-black under layer, its flank cuts
  were raked up to 0.3 so a seam along the plating overran them, and one cut in eight was a trench showing the sub-armour's
  own cuts beneath. Now: seams 8 to 14 cm, every cut square to the ship so a seam ends on the cut it meets, no trenches, plate
  0.25 m standing proud by up to 0.12 m, and the seams' under layer a rust (or the maker's mid tone) a little darker than the
  plate, with the cavity ink and the deepest AO lightened (`ink_k` 0.12, `ao_floor` 0.55). The Breakers' unevenness is now
  in the stations' spacing, the plates' lift and the patchwork, not in crooked cuts (principle 6 of the skill: order, not
  noise).
- **One plating layout.** The detail layer cuts every part the same way; a taken part differs by its paint, not by its
  plate shapes. Giving each maker its own plating (the Concord's large panels, the Hunt's chevrons) is a later step if the
  owner wants it (task 1h.5).

## Version 9: freighter hulls only (owner, 2026-11-11)

The owner, on the Saucer runner's bake: "lihnes are too harsh, and hull plating lines are crossing over each other", then
"other than freighter hulls lets not reuse any other ships hulls too" and "I dont like that they have military ship parts,
the vos would be sort of a militia organzide from various traders".

### D18. A militia of traders flies no warship parts

**Culture, accepted** (owner, 2026-11-11): the Vos Syndicate is a militia organised from various traders. This is the
first culture the owner has set for the Vos beyond the console's palette, and it replaces D6's "parts taken from other
factions' ships": a trader's militia fights in its members' own freighters, refitted in its own yards. So:

- **Hulls are freighters' only**: the game's four freighters (drive blocks, spines, bows and containers), cut and joined.
  Nothing from a warship, of any faction.
- **What is added is welded in the militia's own yards** (`made`, `beam`, `gun` on `hull-grow.js`): collars, decks, hubs,
  arms, heads, pods, guns. Every welded bow ends in a flat face (D10).
- **Its fighters were already its own** (D13) and are unchanged.

| Ship | Was | Now |
|---|---|---|
| 8 | Saucer runner: a Concord Petrel's hull and command deck on a Hunt freighter's drive block | **Deck runner**: two Hunt freighters cut and joined, one's drive block and the other's bow end, cut 20 m short of its taper and capped flat. Renamed, since it has no saucer |
| 3 Boarder | A Hunt frigate's forked prow | A welded lock house with a glazed door and a grapple fork of two blunt arms either side of it |
| 12 Ram | Two nacelles of a Concord Albatross | Two welded drive pods, a salvaged freighter engine recessed in each stern |

The Drum, the Cross and the Broadside were freighters, containers and welded parts already, and are unchanged.

- **Rooms.** The Deck runner's and the Boarder's plans were redrawn for their new parts (the bow end is about 18 m across
  and two decks deep, too slim past z 135; the lock and grapple control are in the lock house) and refitted; all six pass
  the room check again.
- **The Deck runner's detail** keeps D17's recipe with the softer, square seams, its windows from its new plan, and one
  maker: both freighter parts wear the Hunt freighters' look, dulled. The Petrel's bay doors are gone.
- **Provisional:** whose colours a ship wears (survey Y15), and whether the console's palette meanings (rust as salvage)
  still read right for a militia of traders.

### D19. No face without detail (owner, 2026-11-11)

The owner, of the Deck runner's plain bow cap: "I exepect greebles and antena here, the ship guide should clearly state
that there should be noface that is completely devoid of detail", then with reference ships: "examples of greebles at bow
of the ship", "also heres an example of visually pleaseing hull designs", "you can see the homeworld greeble pattern on hull
plating is very visually pleasing also". The references are other games' art and are not committed; what they do is
written into the Ship Design Bible in our own words (T9 to T12).

- **The rule** (Ship Design Bible T9, the `ship-detailing` skill's rule 8, CLAUDE.md section 10): every face carries detail,
  bow and stern faces, caps, collar ends and deck edges included.
- **Machinery where the plating stops**: the Vos faction's `greebles_per_m2` is 0.3, which lands only where the plating
  leaves the hull bare (the end faces), never on the plates.
- **Ordered machinery panels** (T10; a new `fixtures` kind `machinery` in `hull_detail.py`): a framed recess cut into the
  plating, laid out on a grid of cells (blocks, slot banks, pipe pairs, vents, a lamp now and then) in a worn grey. One
  fills the Deck runner's bow face; inset panels run in pairs along the bow end's top and the drive block's upper flanks.
- **Masts as geometry** (T11; `fixtures` kind `antenna`): a mast stands metres off the hull, so baking it smeared it onto
  the plate. The bake now builds only its footing and writes each mast's frame to the manifest (`masts`); the page builds
  it chunky and segmented (block footing, three stepped sections, an equipment head, a lit spike, a cross arm with aerials,
  a dish over flight control) in the shared `baked-ship.js`. The game will build them the same way from the manifest.
- **Not built yet**: T12, colour in large fields with the edges picked out in a pale line, is the painter's next step.

### D20. Every antenna its own, dishes, containers, and the whole line textured (owner, 2026-11-11)

The owner: "Very nice. Another rule. No two antenna cam be the same shape. Also need dishes as well. Containers need
materials. We can reuse that from freighters. And we should begin texture work on all other ships".

- **No two antennas the same shape** (Ship Design Bible T13, the skill's rule 9, CLAUDE.md section 10). The bake numbers each
  antenna and dish fixture of a ship (`style`); the page builds each from a distinct entry of the list of every combination
  of head (box, drum, fork, ball, plate), cross arms (none, one, two), segments (two to four) and a side dish (90 in all):
  its place in the ship's list times 37 plus 11, modulo 90, 37 being coprime with 90, so no two share a design. A port
  and starboard pair differ too: the first version made a mirrored pair one design, under T7, and the owner corrected it
  ("I literally just told you no two antenna should look the same on a starship"), so T13 overrides T7 for antennas and
  dishes. No two the same length either (owner: "Antenna need to also be different lengths. No two antenna shall have
  same length"): each length steps up 13% until it is at least 12% from every other length on the ship.
- **Dishes** (`fixtures` kind `dish`): a bowl on a pedestal and yoke, with its feed, its size from the data and its tilt
  and turn from its style; built as geometry from the manifest like the masts, its footing baked.
- **Containers** wear the freighters' container (`assets/containers/`, baked from a CC0 set) through a new shared module,
  `docs/mockups/lib/container.js`, lifted from the freighters page, at the game's 12 by 12 by 24 m, in two tints. The
  freighters page still carries its own copy until it next moves onto the module (task 1k.3; CLAUDE.md 6.1: reported, not
  left).
- **The whole line textured**: the Boarder, the Drum, the Cross, the Ram, the Broadside and the Stack, each on its own
  unwrap with the Deck runner's recipe: its freighter parts in their makers' looks dulled, the militia's welds in rust,
  windows from its room plan, machinery on every bare face, inset machinery panels, antennas and a dish of its own design,
  a brass band where the militia welded its trade on, a red beacon. The Stack is a fighter, on the Cutter's small-scale
  recipe; its job is still survey Y14 (a heavy fighter, provisional).

### D21. Glow by baked light and shaders (owner, 2026-11-11)

The owner, of the Cross's emitters: "I don't really like these glowy bits on the cross. They look kinda cheap to me. If
you're gonna have glowy bits use baked lighting or shaders". The glowing faces (gun mouths, tractor emitters, drives) were
painted flat cyan with a grid of cells. Now (Ship Design Bible T14): the Vos paint's `strip_k` is 0, so those faces bake
as dark metal lenses, repainted on all nine ships (no rebake: the light they throw on the hull round them was already in
each light map); and the shared `baked-ship.js` `lenses` lays a shader lens on each glowing face, built from the page's own
hull: a hot core fading to a dark lip, a rim that lights as the view grazes it, slow rings drawn inward, a pulse on its own
phase.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| Y1 | Is this how the Vos get their ships: freighter parts and taken parts rearranged, nothing built from new? | Yes |
| Y3 | Do taken parts keep their makers' colours, with Vos metal in rust and brass? | Yes, dulled, with clan paint added in detail |
| Y4 | May a Vos ship carry parts of any faction, or only its neighbours'? | Any faction the clan could have raided |
| Y8 | Is each trade on the right hull? | As drawn |
| Y9 | Is the disguise a trade of one ship or a trait of all? | A trait of all, with the Broadside best at it |
| Y10 | How does rock breaking work? | A slow charged shot at a rock; the rock splits, and everything within reach takes heavy damage |
| Y12 | Should a Vos craft be patched unevenly side to side? | Mirrored, as now (provisional) |
| Y13 | Should a freighter part keep its maker's plating, not only its paint? | Paint only, as now (provisional) |
| Y15 | Whose colours does a militia ship wear? | Its trade house's freighter look, dulled, with the militia's rust and brass on what it welded on (provisional) |
| Y14 | What do the Vos fly the Stack as? | A heavy fighter (provisional) |

Decided (owner, 2026-11-10): Y2, the picks (3, 8, 9, 10, 11, with 12 added by us for the rock breaker); Y5, no class
ladder: every ship about cruiser size, a weaker hull, one trade each.
