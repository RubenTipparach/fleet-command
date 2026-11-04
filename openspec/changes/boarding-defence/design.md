# Design: boarding-defence

## The interior

Every ship's interior is the Petrel's deck plan (`data/ships/concord/petrel_plan.json`, the Ship Design Bible's rooms)
until each class has its own plan; the Meridian in the scene is a Petrel. `INTERIOR` is built from the plan once: a node
in every room, a door where each room meets its deck's corridor, the doors along a corridor joined in order, and a ladder
between neighbouring decks' corridors. `route()` is a shortest path over it. Units carry metres in the plan's axes.

The 3D view is its own small three.js renderer under the console's canvas; the Security page clears a window for it and
draws the tags, paths and room targets over it. Decks are drawn 13 m apart (an exploded view) so all three show. Rooms
are floors and outlines coloured by kind; a boarder's goal is lit in Alert.

## Fighting aboard

| Number | Value | Meaning |
|---|---|---|
| `team` | 3 | Marines a team, from the ship's marines aboard |
| `walk_m_s` | 3 m/s | Walking pace |
| `engage_m` | 5 m | A team and a squad this close on one deck fight |
| `volley_s` | 3 s | Between volleys; each marine one shot at the boarding rule's 50 % (`BOARD.hit_chance`) |
| `ambush_s` | 6 s | Holding a room this long sets an ambush |
| `ambush_bonus` | +25 % | On the ambush's first volley, which the boarders cannot answer |
| `board_every_s`, `board_squad` | 18 s, 5 | The boarding enemy's rhythm |
| `sabotage_s`, `sabotage_dmg` | 12 s, 25 % | Unopposed at its goal, a squad damages the room's system |
| `capture_s` | 30 s | Boarders holding the bridge this long take the ship |

`volley(n, p, R)` is the one rule for a side's shots, used by the boarding party on an enemy ship and by fights aboard.
The ship's marines aboard are its teams: marines beamed away leave the last teams, marines back form a team at the pads.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| B1 | Should each class have its own deck plan before this goes into the game? | Yes; the Petrel stands in |
| B2 | Should armed crew (not only marines) defend their rooms? | Not yet |
| B3 | Should boarders be able to take other rooms (engineering stops the engines outright)? | Sabotage only, the bridge takes the ship |
| B4 | Doors the captain can seal or open? | Not yet; a good next step |
| B5 | Should the morale half of the recommendation (crew quality falling with damage) follow? | Later, its own change |
