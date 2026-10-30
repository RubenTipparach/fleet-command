# Design: game-flow

Mockup 1. Everything here is shown on `docs/mockups/game-flow.html`; numbers are provisional until the
owner approves the mockup for build. Names of ships, factions and places are our own (CLAUDE.md
section 1). The engine choice (survey T4) does not change this design.

## References

Two manuals show how the reference games did each screen (CLAUDE.md section 1: cite the page, take the
shape, not the text):

| Screen | Starfleet Command 2 manual | Starfleet Command 3 manual |
|---|---|---|
| Main menu, exit with confirmation | printed pages 74-75 | 6-7 |
| Skirmish: scenario list, map, briefing, customize, play | 76-80 | 26-27 (points budget, scenario types) |
| Refit by hardpoint group, firing arcs, buy and trade, resupply, saved variants, officers | 78-80 (space dock) | 28-29 |
| Campaign map: hexes, zoom levels, ownership colour, hex data, adjacent moves | 84-87 | 32-35 |
| Facilities only where the hex has a base or planet (supply dock, shipyard, officers) | 88-89 | 34-37 |
| Missions offered in a hex, accept or forfeit, rewards | 86-87 | 36-37 |

What we take: a scenario list with a briefing and a point budget; refit by hardpoint group with firing
arcs and full-value trade-ins; a hex map coloured by owner with three levels of zoom; facilities that
grey out where there is no friendly port; missions offered where the fleet is.

## The flow

```
Main menu ── Options (sheet) ── back
   │  └─ Quit (confirm in the page) ── back
   ├─ Skirmish ── scenario list + briefing ── fleet (budget) ── refit (per ship) ── Launch
   │                                                                               └─ battle ── results ── Main menu
   └─ Campaign ── new campaign (name, difficulty) ── campaign map
                     ├─ at a friendly port: Shipyard | Outfitter (refit + supplies) | Recruiting
                     ├─ a mission on the fleet's hex: briefing ── Deploy ── battle ── results ── campaign map
                     └─ End turn: missions age, new ones appear, the turn counter advances
```

Every screen has Back, except the battle (CLAUDE.md section 7: a battle is left by ending it).

## Main menu

Title, then four buttons stacked at the left over a slowly turning 3D backdrop (a planet, a star, a ship
standing in for the real model): **Campaign, Skirmish, Options, Quit**. Options is a sheet: master,
music and effects volume as discrete boxes (0-10), graphics quality (Low, Medium, High), UI scale (90,
100, 115 percent). Quit asks in the page (the artifact host refuses `confirm()`), and in the game quits.

## Skirmish

**Scenarios** (presets for now, the owner's words). Each has a briefing, a small map of its objects, the
sides, and a point budget:

| Scenario | Sides | Objective | Budget (points) |
|---|---|---|---|
| Border Picket | your fleet against one patrol group | destroy or drive off the patrol | 600 |
| Convoy Raid | your fleet against three freighters and two escorts | destroy two freighters | 900 |
| Station Siege | your fleet against an outpost station and its picket | destroy the station | 1200 |
| Fleet Action | your fleet against an equal fleet | destroy the enemy flagship | 1500 |

The budget can be set Small (x0.75), Standard (x1) or Large (x1.5) (survey F4). Like the reference
(SFC3 printed page 26), points cap what a fleet may spend; the enemy is built to the same budget.

**Fleet**: up to four ships (survey F5): one flagship, the captain's own, and up to three wingmen. Pick a
hull from the catalogue; its stock loadout is bought with it. The budget bar shows spent and left, and
Launch is refused over budget or with no ship.

| Hull (class) | Points | Shield slots | Primary | Heavy | Hull | Bridge | Engine |
|---|---|---|---|---|---|---|---|
| Swift (corvette) | 160 | 1 | 2 | 0 | 1 | 1 | 1 |
| Lantern (frigate) | 230 | 1 | 2 | 1 | 2 | 1 | 1 |
| Halberd (destroyer) | 320 | 2 | 3 | 1 | 2 | 1 | 1 |
| Bastion (cruiser) | 450 | 2 | 4 | 2 | 3 | 2 | 1 |
| Paragon (battlecruiser) | 620 | 3 | 4 | 3 | 3 | 2 | 2 |

## Ship loadout (one screen for skirmish and campaign)

After SFC3 printed page 28: the ship's schematic, top down, with its hardpoints in six groups: **Shields,
Primary weapons, Heavy weapons, Hull systems, Bridge systems, Engines**. Pick a group, pick a hardpoint,
pick a system from the list (its cost and a one-line description), then **Fit** (an empty hardpoint),
**Trade** (an occupied one: the old system is refunded) or **Remove** (refunded). In skirmish setup a
refund is the full price (SFC3 page 29); in the campaign a removed system is sold to the port's owner
(see Selling and standing). Weapon groups
show each hardpoint's firing arc on the schematic.

| Group | Systems (cost in points or x10 credits) |
|---|---|
| Shields | Deflector I 20, Deflector II 40, Deflector III 70 |
| Primary weapons | Pulse cannon 20, Beam lance 30, Point-defense turret 15 |
| Heavy weapons | Missile rack 30, Torpedo tube 45, Plasma projector 60 |
| Hull systems | Armour plating 20, Tractor emitter 25, Shuttle bay 30, Cargo pod 10 |
| Bridge systems | Sensor suite 20, Targeting computer 25, ECM suite 25 |
| Engines | Impulse drive I 15, Impulse drive II 35 |

**Supplies** (SFC3 page 28, "Resupply"): marines, shuttles, mines, missile reloads and spare parts, each
with the count carried, the hull's maximum and the unit cost, changed with + and -. A hull carries only
what its systems allow (no shuttles without a shuttle bay).

**Wingman behaviour** (survey G3): each wingman has one of Escort (stay with the flagship), Attack my
target, Hold at range, Defensive (protect itself first). The flagship has none: the player flies it.

**Variants**: a fitted loadout can be saved by name and picked again (SFC3 page 28), so a favourite
refit is one tap.

## Campaign map

**New campaign**: captain's name and difficulty (Ensign, Captain, Admiral). No story yet.

**The map** is a grid of flat-topped hexagons, 15 columns by 11 rows (165 hexes), seen like Civilization
V: a perspective camera tilted about 50 degrees from vertical, which pans by drag and zooms by wheel or
pinch between three levels of detail (SFC3 page 32):

| Zoom | Shows |
|---|---|
| Far | territory colours and borders only |
| Middle | terrain, planets and stations |
| Near | ship standees and labels |

**Territory**: each hex belongs to the Concord (the player's, blue), the Kethran Hegemony (red), the Vos
Syndicate (amber, raiders) or no one (grey) (SFC3 page 33). **Territory is told by the hex borders, not by
filling the hexes** (owner, 2026-10-30, with a box-back star map: "the elements of each sector is inside the
hex, with the hex borders communicating territory"): every hex is dark glass over a painted galaxy, every
edge is a faint line in its owner's colour, and where ownership changes a bright band runs along the edge,
inside each side's hex in that side's colour. Each hex carries its sector number (column and row, `0705`),
printed like a wargame map.

**Terrain** sets how far the fleet moves in a turn (impedance, SFC3 page 33):

| Terrain | Move cost | Note |
|---|---|---|
| Open space | 1 | |
| Nebula | 2 | sensors and shields weaker in battle |
| Asteroid field | 2 | |
| Star | impassable | |

**Miniatures** (owner, 2026-10-30, with photos of a wargame and a board game: "3d ships on stands, 3d
stations on stands, 3d planets, asteroid belts, nebula"): every ship group and station is a 3D miniature
on a clear flight post over a black hex base ringed in its faction's colour; planets are 3D globes floating
in their hex with an orbit ring in the owner's colour; asteroid fields are belts of rocks curving across
the hex; nebulae are 3D clouds of glowing puffs at several heights. The fleet's miniature is the
flagship's, with an amber ring. Names show at near zoom; hovering or tapping one names it. The models are
stand-ins until mockup 3 sets each faction's ships.

**Moving**: tap a hex; the path and its move cost show; tap again to go. The fleet has 3 move points a
turn (survey F2: turn based). **End turn** spends the rest, ages missions and spawns new ones.

**The top bar**: captain, credits, turn, the fleet's ships with hull integrity. **The hex panel**: the
hex's owner, terrain and contents, and the port and mission buttons.

## Campaign ports

At a hex with a friendly station or planet (survey F1: all three facilities need one; the reference greys
them out elsewhere, SFC3 page 34):

- **Shipyard**: buy a hull (credits), sell a ship (half its value), repair damaged ships (cost by hull
  points lost).
- **Outfitter**: the ship loadout screen above, paid in credits, plus supplies.
- **Recruiting center**: officers for six stations (helm, tactical, engineering, science, comms,
  security), each with a skill rating from Green to Legendary and a cost; and marines (SFC3 pages 29
  and 36).

**Markets**: another faction's station or planet, while your standing with that faction is 0 or above,
opens a **market** where you sell ships, systems and supplies to them (survey F6a, provisional). Only your
own faction's ports have the shipyard, outfitter and recruiting (F1).

## Selling and standing

Survey F6, the owner: "depends who you sell it to as well, a rival of that faction might pay more,
pirates migth pay less, but you get reputation increase etc, depending on where you sell it. you can
also fix it up and join your crew".

**Price**: a sale pays a quarter of the item's price (F6), times the buyer's interest in it:

| Buyer | Pays | Standing |
|---|---|---|
| A rival of the faction that built it (Concord for a Kethran ship, and the reverse) | x1.5 | +1 with the buyer |
| The faction that built it, or anyone else | x1 | +1 with the buyer |
| Pirates (the Vos Syndicate) | x0.6 | +5 with the Vos |

**Standing**: each faction's view of you, from -100 to 100 (survey F7, provisional). It starts at Concord
+40, Vos 0, Kethran -40. Sales raise it as above; a mission against a faction lowers it by 10 and raises
its rival's by 5. Below 0 a faction's ports are closed to you. The top bar shows all three.

**Prizes**: a captured ship is built by its old owner (it keeps its maker, so a rival pays more for it).
It joins the fleet damaged; repair it at a shipyard to fly it ("fix it up and join your crew"), or sell
it.

On a phone a port panel takes the screen and the live map moves to the right third (U1), or the bottom in
portrait (U2).

## Missions

Missions appear as markers on hexes, each eligible where it makes sense:

| Mission | Offered on | Objective | Base reward (credits) |
|---|---|---|---|
| Patrol | own or contested open space | sweep the sector, engage what you find | 300 |
| Enemy station assault | an enemy station's hex | destroy the station | 1200 |
| Planetary assault | an enemy planet's hex | destroy the orbital defences | 1000 |
| Convoy ambush | enemy space | destroy two of three freighters | 700 |
| Convoy defense | own space | keep two of three freighters alive | 600 |
| Assassination | a hex with an enemy commander's ship | destroy that ship | 900 |
| Capture a ship | a hex with a lone enemy ship | board and take it (needs marines) | 800, and the ship |

A mission lasts 3 turns, then expires. Tapping a marker opens its briefing: type, objective, the enemy
strength, the reward and the turns left. **Deploy** is enabled when the fleet is on that hex (survey F3).
After the battle, the results screen shows the outcome, each ship's hull, losses, credits earned and,
for a capture, the captured ship; Continue returns to the map with the mission removed, credits added
and damage carried. A defeat costs no ship unless it was destroyed in the battle.

## Phones

Landscape and portrait (U2). The menus are single columns. The map takes the screen; the top bar
shortens to credits and turn; the hex panel becomes a bottom sheet. A port panel expands to full screen
with the live map in a strip (U1). Every control is at least 44 x 44 CSS px.

## What it costs

The mockup draws 165 hexes as one instanced mesh (1 draw call, about 2 000 triangles), plus about 40
standees (3 small meshes each, about 120 draw calls, under 5 000 triangles), plus a starfield and a few
planets: well under 50 000 triangles and 200 draw calls. A game map of this size costs the same order:
trivial next to a battle. Campaign state is plain data: a few kilobytes to save.

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| F1 | Do the shipyard and outfitter also need a friendly port, or only recruiting? | **Decided** (owner, 2026-10-30): all three need one |
| F2 | Does the campaign map move in turns, or in real time? | **Decided**: turns, 3 move points, End turn |
| F3 | Where must the fleet be to deploy a mission? | **Decided**: on the mission's hex |
| F4 | Can the player change a scenario's point budget? | **Decided**: yes, Small, Standard, Large |
| F5 | How many ships in a fleet? | **Decided**: up to four, the flagship and three wingmen |
| F6 | What does selling return in the campaign? | **Decided**: a quarter, more from a rival of the builder, less from pirates who raise your standing; prizes can be repaired into the fleet |
| F6a | Where can you sell to another faction? | At its ports, open while your standing with it is 0 or above (provisional) |
| F7 | How does standing work? | -100 to 100 per faction; sales and missions move it; below 0 its ports close (provisional) |
