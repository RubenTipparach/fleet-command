# Design: freighters

## Where the role comes from

| Source | What it shows | What we take |
|---|---|---|
| SFC3 manual, printed page 37 | A campaign mission can be to attack an enemy base or convoy | Convoys are targets worth a mission |
| SFC3 manual, printed page 42 | Missions to protect a convoy or raid an enemy supply route appear in some hexes; their outcome moves the economy | A freighter is worth guarding and worth raiding; its loss is felt on the map |
| `game-flow` design, "Freighters" | Every faction runs freighters along its routes, one to a route, a sector a turn | Every approved faction needs one |
| `data/missions/defend.json`, `ambush.json`, `distress.json` | Three freighters and two escorts; a freighter calling for help | It is slow, weak and travels in threes |
| Survey F6 (decided) | Prizes can be sold or repaired into the fleet | A captured freighter keeps its builder's look |

The SFC2 manual has no convoys (searched for freighter, convoy, cargo and escort).

## Scope (the owner, 2026-11-07)

- "Freighter should only focus on approved factions": the four whose ships are approved and in the game, the Concord
  (mockup 6), the Krai Warhost (mockup 7), the Ssarith Hunt (mockup 12) and the Kelvane Institute (mockup 23). The
  Kethran Hegemony and the Vos Syndicate have console skins (mockup 4) but are not yet approved to build, so they have no
  freighters here.
- "Narrow down freighter options to ones where cargo is visible": every concept carries its cargo in sight, so the
  version 1 holds (the Open hall, the Ore barge, the Long Watch provisioner, the Sealed case) are gone.
- "make sure all cargo is the same size", "The containers I mean", "The cargo hull shape it self can be the way it was in
  the draft": **one container for every freighter, 6 by 6 by 12 m**, in each faction's colours; the hulls stay as drafted.

## The picks (the owner, 2026-11-07)

"1,4,5,8, but with 8 having really skinny arms and link.tbe arms together with a thin bridge at the end to keep the loop
design ethos", "All freighters should be 2x bigger. Along with the containers too..this way cargo ships are much bigger than any
combat ships..as they should be", then "The institute freighter should be rounder at the tip", "You got the institute
draft wrong". Survey V2 and V5, decided.

| # | Faction | Pick | Shown length | What changes from the blockout |
|---|---|---|---|---|
| 101 | Concord | 1 Gauge carrier | 300 m | Twice the size |
| 102 | Krai Warhost | 4 Spear train | 320 m | Twice the size |
| 103 | Ssarith Hunt | 5 Tally carrier | 340 m | Twice the size |
| 104 | Kelvane Institute | 8 Twin ledger | 330 m | Twice the size, and a loop: the draft's stern case narrows to a slender section that splits into two really skinny arms, 15 m (30 m shown) either side, straight round two identical stacks of containers, curving in at the bow on a quarter circle to meet in a thin bridge with a domed nose (the loop of the Institute's line, `kelvane-ship-concepts`) |

**Twice the size.** Every freighter is shown at twice its drafted size, its containers with it (`FREIGHTER_X` on the
page), so a container is 12 by 12 by 24 m and every freighter, 300-360 m, is longer than any warship (the largest, the
Krai dreadnought, is 226 m as built). The board's eight are shown at the same scale.

**Detailing waits** (owner, 2026-11-07: "Then go ahead and detail them", then "Don't do details yet").

## The rules every concept keeps

- **Culture, never the body** (CLAUDE.md section 1; Ship Design Bible C1-C5). Each concept names the tradition it comes
  from. No faction has a merchant tradition written down, so each is drafted here, marked provisional, and asked in
  survey V1. Described without its species' name, each is a working ship's shape, never a creature's.
- **One grown hull** (bible chapter 14, F1-F5):
  - Each hull is lofted through its faction's cross-section on the shared `docs/mockups/lib/hull-grow.js`.
  - Its branches are split from its own section.
  - Its skin is closed, with every edge shared by exactly two triangles, measured on the page.
- **The containers are not hull.** They are placed, as a crate is set on a deck: the only placed pieces on the page.
  Every one is the same box, 6 by 6 by 12 m as drafted and 12 by 12 by 24 m as shown (`CONTAINER_M` and `FREIGHTER_X`
  on the page; `data/` when the game carries it).
- **The cargo is in sight**: from the bow quarter and from above, every freighter's containers are seen.
- **The faction's own colours** (bible X1, a hull complements its console). Clay shows the massing alone.
- **True scale** beside the Concord Petrel (88 m) and a person (1.8 m).

## Each faction's cross-section on this board

The board uses one section layout for every faction, `fsec`, so the eight can be compared. Each faction sets it its own
way. When a pick becomes a class it moves onto its faction's own section (the Institute's `ksec`, the Hunt's `hsec`) and
is grown again at its size, as the Institute's picks were.

`fsec` is 28 points: the keel, thirteen up the port side, the crown, thirteen down the starboard side. Parameters:

| Parameter | Shapes |
|---|---|
| half beam `hb` | width |
| top, bottom | height |
| corner `r` | chamfered for straight cuts, or rolled |
| keel drop `kd` | how far the keel points down |
| crest `cr` | how far the crown points up |
| upper half beam `hbt` | tumblehome, or an overhang when wider than `hb` |
| belt `bw` | how far the side stands proud |
| shoulder `sy`, `sh` | a step in, or a hood out |

| Faction | Section | Colours (from its console skin) |
|---|---|---|
| Concord | An octagon: 45 degree chamfers, upright sides | Pearl hull, grey service band, Concord-blue stripe, pale blue light |
| Krai Warhost | A blade: a deep keel and a crest, cut flat between | Blued steel, tempered bronze edges, pale gold light |
| Ssarith Hunt | A hood: the upper hull wider than the lower, over a narrow neck | Olive armour, green livery band, pale green-white light |
| Kelvane Institute | A rolled case, every edge rounded | Champagne, dark smoked glass, amber light |

## Ways to carry the containers in sight

| Way | Meaning | Seen at 40 px |
|---|---|---|
| Spine | Containers racked outside along a spine | A rail of blocks |
| Cradle | The hull parts round its containers and closes again | A ring round blocks |
| Between | The containers ride between two hulls, open at the bow | A fork with blocks in it |

## The eight concepts

Lengths are the hull's. Every tradition is drafted and provisional (survey V1).

### Concord

**Tradition: The Common Gauge** (drafted from "decides by consent"). Many worlds agree one standard: every member world
packs to the same container, so any world's cargo fits any Concord freighter and no world's own standard wins. The
containers come in many colours, one size.

| # | Concept | Way | Length | What it is |
|---|---|---|---|---|
| 1 | Gauge carrier | Spine | 150 m | An octagonal spine with containers racked four round it in eight rings (32); the command block raised aft over the drive |
| 2 | Gauge cradle | Cradle | 160 m | The octagonal hull parts port and starboard round containers three across and two high (30), and closes again at the bow |

### Krai Warhost

From the line's design (`krai-ship-concepts`): "war is the only honest test"; nothing flies that is not a blade, edge
first.

| # | Tradition | Concept | Way | Length | What it is |
|---|---|---|---|---|---|
| 3 | The Host's Baggage (drafted): a war host carries its own, edge first | Baggage blades | Cradle | 170 m | Two blades part round containers three across and two high (30), and meet again edge first at the bow |
| 4 | The Host's Baggage | Spear train | Spine | 160 m | A blade spine with its containers stood on end down both sides, each canted out like a blade (26) |

### Ssarith Hunt

From the traditions drafted for the emblem (survey H1 accepted drafting). The hood over a narrow neck, the reach forward
and light in strips are the line's shape language (mockup 12).

| # | Tradition | Concept | Way | Length | What it is |
|---|---|---|---|---|---|
| 5 | The Tally Keepers (the witnesses: everything carried is counted) | Tally carrier | Spine | 170 m | Containers in counted rows of five along a low spine, a light strip between rows (35); the hooded bridge aft over the drive |
| 6 | The Fair Chase (the hunt's law: its stores are never prey) | Fair Chase tender | Cradle | 175 m | A low raked hull parting above and below round its containers, one high (12), unarmed, a green livery band along it |

### Kelvane Institute

From the schools drafted for the line (`kelvane-ship-concepts` D1). Survey I5 recommends keeping the schools not used for
the warships "for its stations, support ships". These freighters are that use, provisional on I5. No pods: the owner,
"I hate those pods" (bible KI rules).

| # | Tradition | Concept | Way | Length | What it is |
|---|---|---|---|---|---|
| 7 | The Field Stations (expedition scientists, years from home) | Field tender | Cradle | 180 m | The case parts above and below round containers three across and two high (18), and closes at a gallery of glass forward |
| 8 | The Open Notebook (every consignment counted twice) | Twin ledger | Between | 160 m | Two identical hulls from one stern, parted to their bows, two identical stacks of containers three high between them (30) |

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
- **Draw calls**: about four a ship (hull, glass, light, containers), so the eight draw in about sixty.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| V1 | The drafted traditions (the Common Gauge; the Host's Baggage; the Tally Keepers, the Fair Chase; the Field Stations, the Open Notebook) | Keep them as drafts until the owner changes them |
| V3 | One freighter class a faction, or both of its concepts? | One a faction now; the other later as a variant |
| V4 | How is a freighter armed? | One point defence, nothing else |
