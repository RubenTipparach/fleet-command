# Design: freighters

## Where the role comes from

| Source | What it shows | What we take |
|---|---|---|
| SFC3 manual, printed page 37 | A campaign mission can be to attack an enemy base or convoy | Convoys are targets worth a mission |
| SFC3 manual, printed page 42 | Missions to protect a convoy or raid an enemy supply route appear in some hexes; their outcome moves the economy | A freighter is worth guarding and worth raiding; its loss is felt on the map |
| `game-flow` design, "Freighters" | Every faction runs freighters along its routes, one to a route, a sector a turn | Every faction in the game needs one |
| `data/missions/defend.json`, `ambush.json`, `distress.json` | Three freighters and two escorts; a freighter calling for help | It is slow, weak and travels in threes |
| Survey F6 (decided) | Prizes can be sold or repaired into the fleet | A captured freighter keeps its builder's look |

The SFC2 manual has no convoys (searched for freighter, convoy, cargo and escort).

## The rules every concept keeps

- **Culture, never the body** (CLAUDE.md section 1; Ship Design Bible C1-C5). Each concept names the tradition it comes
  from. No faction has a merchant tradition written down, so each is drafted here, marked provisional, and asked in
  survey V1. Described without its species' name, each is a working ship's shape, never a creature's.
- **One grown hull** (bible chapter 14, F1-F5):
  - Each hull is lofted through its faction's cross-section on the shared `docs/mockups/lib/hull-grow.js`.
  - Its branches are split from its own section.
  - Its skin is closed, with every edge shared by exactly two triangles, measured on the page.
- **The cargo is not hull.** Containers, cassettes, cases and lashed loads are placed, as a crate is set on a deck. They
  are the only placed pieces on the page.
- **The faction's own colours** (bible X1, a hull complements its console). Clay shows the massing alone.
- **True scale** beside the Concord Petrel (88 m) and a person (1.8 m).

## Each faction's cross-section on this board

The board uses one section layout for every faction, `fsec`, so the eighteen can be compared. Each faction sets it its
own way. When a pick becomes a class it moves onto its faction's own section (the Institute's `ksec`, the Hunt's `hsec`)
and is grown again at its size, as the Institute's picks were.

`fsec` is 22 points: the keel, ten up the port side, the crown, ten down the starboard side. Parameters:

| Parameter | Shapes |
|---|---|
| half beam `hb` | width |
| top, bottom | height |
| corner `r` | chamfered for straight cuts, or rolled |
| keel drop `kd` | how far the keel points down |
| crest `cr` | how far the crown points up |
| upper half beam `hbt` | tumblehome, or an overhang when wider than `hb` |
| belt `bw` | how far the side stands proud |
| offset `ox` | the whole section moved to one side |

| Faction | Section | Colours (from its console skin) |
|---|---|---|
| Concord | An octagon: 45 degree chamfers, upright sides | Pearl hull, grey service band, Concord-blue stripe, pale blue light |
| Kethran Hegemony | Flat-bottomed, the upper sides stepping in over a proud belt | Gunmetal plate, oxide belt, signal red band, ember light |
| Vos Syndicate | As the Kethran, but every station differs and is offset: nothing matches | Rust plate, brass and verdigris patches, brass light |
| Krai Warhost | A blade: a deep keel and a crest, cut flat between | Blued steel, tempered bronze edges, pale gold light |
| Ssarith Hunt | A hood: the upper hull wider than the lower, over a narrow neck | Olive armour, green livery band, pale green-white light |
| Kelvane Institute | A rolled case, every edge rounded | Champagne, dark smoked glass, amber light |

## Three ways to carry cargo

| Way | Meaning | Seen at 40 px |
|---|---|---|
| Spine | Loads racked outside along a spine | A rail of blocks |
| Hold | The cargo shut inside a deep hull | One mass |
| Cradle | The hull parts round its cargo and closes again | A ring round blocks |

## The eighteen concepts

Lengths are the hull's. Every tradition is drafted and provisional (survey V1).

### Concord

**Tradition: The Common Gauge** (drafted from "decides by consent"). Many worlds agree one standard: every member world
packs to the same octagonal cassette, 12 m by 6 m, so any world's cargo fits any Concord hold and no world's own
standard wins. The cassettes come in many colours, one gauge.

| # | Concept | Way | Length | What it is |
|---|---|---|---|---|
| 1 | Gauge carrier | Spine | 150 m | An octagonal spine with cassettes racked four round it in eight rings; the command block raised aft over the drive |
| 3 | Gauge cradle | Cradle | 160 m | The octagonal hull parts port and starboard round a rack of cassettes and closes again at the bow |

**Tradition: The Open Hand** (drafted from "explores, mediates, defends"). Aid goes first and in the open: the hold opens
wide along both flanks, so anyone can see what is carried.

| # | Concept | Way | Length | What it is |
|---|---|---|---|---|
| 2 | Open hall | Hold | 140 m | A deep octagonal hull with long dark bay doors down both flanks, its bridge on the crown forward |

### Kethran Hegemony

The written culture is one line: "an empire of rank and conquest: order is strength". Its technology is "fusion,
foundries, armour plate". Its emblem is the prow leading, its great houses flanking it. The rest is drafted.

| # | Tradition | Concept | Way | Length | What it is |
|---|---|---|---|---|---|
| 4 | The Tithe Road (drafted): the great houses send their due to the throne under armour, the cargo never shown | Tithe citadel | Hold | 170 m | A stepped armoured wedge: three decks rising toward the stern, the prow forward, ember seams at each step |
| 5 | The Foundries (from the written technology: "machines are proud of their mass") | Foundry hauler | Hold | 190 m | A deep slab hull carrying ore and metal, a heavy proud belt along both sides, a row of hatches down its deck |
| 6 | The Great Houses (from the emblem) | House train | Cradle | 180 m | A drive section, then two flanking hold hulls round the cargo, then an armoured prow leading |

### Vos Syndicate

The written culture is one line: "free clans of raiders: luck, loot, loyalty to the crew". Its technology is "salvage,
patched and grown over; nothing matches, everything works". Its ships line includes "mandible prows", which reads the
species' body and is not used. The rest is drafted.

| # | Tradition | Concept | Way | Length | What it is |
|---|---|---|---|---|---|
| 7 | The Prize Share (drafted): what a crew takes is the crew's, so loads are lashed on as they are taken | Prize string | Spine | 160 m | A crooked spine of mismatched sections, loads of every size lashed on at odd angles |
| 8 | The Breakers (from the written technology) | Patchwork hauler | Hold | 150 m | A hold made of other ships' sections, each a different size and set off true, patched in brass and verdigris |
| 9 | The Clan Raft (drafted): a clan ties its hulls together and shares one drive | Clan raft | Cradle | 170 m | Two hulls of different sizes joined at stern and bow, loot slung between them |

### Krai Warhost

From the line's design (`krai-ship-concepts`): "war is the only honest test"; nothing flies that is not a blade, edge
first. From the emblem: "tradition is kept, carried and added to, never replaced".

| # | Tradition | Concept | Way | Length | What it is |
|---|---|---|---|---|---|
| 10 | The Host's Baggage (drafted): a war host carries its own, edge first | Baggage blades | Cradle | 170 m | Two blades part round the cargo and meet again edge first at the bow |
| 11 | Carried, never replaced (from the emblem) | Ore barge | Hold | 210 m | A long low blade with a deep keel hold, lengthened hold by hold: each hold a little different, bronze seams between them. The mission guide's ore barges (The Long Haul) |
| 12 | The Host's Baggage | Spear train | Spine | 160 m | A blade spine with loads racked edge on down both sides |

### Ssarith Hunt

From the traditions drafted for the emblem (survey H1 accepted drafting). The hood over a narrow neck, the reach forward
and light in strips are the line's shape language (mockup 12).

| # | Tradition | Concept | Way | Length | What it is |
|---|---|---|---|---|---|
| 13 | The Long Watch (stargazers turned navigators, who carry a hunt's stores the long way out) | Long Watch provisioner | Hold | 180 m | A deep low hold, its navigators' bridge hooded on a long neck reaching forward |
| 14 | The Tally Keepers (the witnesses: everything carried is counted) | Tally carrier | Spine | 170 m | Loads in counted rows, a light strip at each row; the hooded bridge aft over the drive |
| 15 | The Fair Chase (the hunt's law: its stores are never prey) | Fair Chase tender | Cradle | 175 m | A low raked hull parting round its cargo, unarmed, a green livery band along it |

### Kelvane Institute

From the schools drafted for the line (`kelvane-ship-concepts` D1). Survey I5 recommends keeping the three not used for
the warships "for its stations, support ships". These freighters are that use, provisional on I5. No pods: the owner,
"I hate those pods" (bible KI rules).

| # | Tradition | Concept | Way | Length | What it is |
|---|---|---|---|---|---|
| 16 | The Calibrators (a reading is only as true as its standard) | Sealed case | Hold | 150 m | One closed case in whole ratios, a band of dark glass round it, the hold a sealed standard |
| 17 | The Field Stations (expedition scientists, years from home) | Field tender | Cradle | 180 m | The case parts round a rack of sealed cases, a gallery of glass forward |
| 18 | The Open Notebook (every consignment counted twice) | Twin ledger | Hold | 160 m | Two identical hold hulls from one stern, parted all the way to their bows |

## In the game (provisional, after approval for implementation)

One battle class per faction, from its pick (survey V3), replacing the `kff` stand-in. Numbers in the battle's own units,
as `data/battle/classes.json` writes them:

| Field | Provisional | Beside |
|---|---|---|
| length | From the model, by the ratio the warships use | |
| speed max | 2,400 m/s | The frigate's 3,600 |
| accel | 300 m/s2 | |
| turn | 16 deg/s | The frigate's 44 |
| hull | 90 | The troop ship's 55, the frigate's 60 |
| shields | 16, 14, 12, 10, 12, 14 | |
| mounts | One point defence (survey V4) | |
| hold | 60 | Between the troop ship's 30 and the starbase's 200 (survey B28) |

The campaign map draws each faction's freighter in place of the placeholder. Convoy defense, Convoy ambush and Distress
field the faction's own.

## One copy (CLAUDE.md section 6.1)

The board's `cradle` (a hull parted round an opening and closed again) does what `loopOf` on the Institute page does, in
the board's own section. That is two copies of one rule. When the picks are grown again, both move into the shared
`docs/mockups/lib/hull-grow.js` as one function taking any faction's split (task 2.4), and both pages inline it.

## Cost

- **Triangles**: a few hundred to a few thousand a hull on the board, measured on the page. A ship's budget comes with
  its class, after detailing (bible chapter 18).
- **Draw calls**: about four a ship (hull, glass, light, cargo), so the eighteen draw in under a hundred.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| V1 | The drafted traditions (Common Gauge, Open Hand; Tithe Road, Foundries, Great Houses; Prize Share, Breakers, Clan Raft; Host's Baggage, Carried never replaced; Long Watch, Tally Keepers, Fair Chase; Calibrators, Field Stations, Open Notebook) | Keep them as drafts until the owner changes them |
| V2 | Which concept for each faction | None picked; the board waits |
| V3 | One freighter class a faction, or several (one per way of carrying)? | One a faction now; others later as variants |
| V4 | How is a freighter armed? | One point defence, nothing else |
| V5 | Size: a cruiser to a battleship of its own faction, about 140-210 m | As drawn |
| V6 | The Kethran and Vos have no warships yet; should their freighters wait for their lines? | Draw them now; their lines can grow from the picks |
| V7 | The Vos Syndicate and the Kethran Hegemony were named by a session (2026-10-30), not the owner (owner, 2026-11-07: "Vos? Who is Vos? I don't remember we ever implemented the."). Keep them? | Keep both; their concepts (4-9) stay provisional until the owner answers |
