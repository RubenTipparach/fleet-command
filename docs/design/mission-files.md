# Mission files

A mission is one JSON file in `data/missions/`, written by hand or in the mission editor (`tools/mission-editor`). The
game, the tests and the editor all read it through one loader (`src/Core/Campaign/MissionScript.cs`), which checks every
field and every expression when the file loads. A bad file stops with an error that names the file and the field, so a
mistake shows up in the editor, not halfway through a campaign. The format is the campaign mockup's mission scripts
(`docs/mockups/lib/campaign-missions.js`) written as data; the mission types are the Missions guide's
(`docs/design/mission-guide.html`). Change: `openspec/changes/mission-scripting`.

## Running the editor

```sh
.tools/dotnet/dotnet run --project tools/mission-editor     # from the repository; scripts/install-dotnet.sh installs .NET 8
```

The editor is an Avalonia app, so the same code runs on Windows and macOS. Its window has four parts:
- **Mission files**, on the left. **New mission** starts a file from a template.
- **The file**, in the middle: the main fields as a form above, the whole file as JSON below. The two stay in step, and
  the file is checked as you type.
- **The preview**, on the right. It offers the mission in a new campaign for the side and seed you pick. You can hand it
  data the way an offering mission would (`prize_name=Vekk; tier=2`). It shows:
  - where the mission is offered and what its setup rolled;
  - the briefing;
  - every ship of the battle as planned, with its position;
  - the objectives;
  - the outcome when the battle is won, withdrawn from or lost: the grade, credits, prestige, standing, the log line and
    any next mission.
- **The names each expression may use**, along the bottom, read from the loader's own lists.

## The file

| Field | Means |
|---|---|
| `id` | lower case, digits and `_`; unique across files |
| `name`, `glyph` | the mission's name, and the one letter on its banner on the map |
| `kind` | `contract` (offered for `turns`, default 3, then gone; may be withdrawn from), `story` (waits until done; no withdrawal), `special` (offered as a contract, no withdrawal), `event` (never offered at random: the campaign offers it itself, as when hostile hunters catch the fleet (`pursuit.json`, handed `hunters`, `hunter_fac` and `hunter_size`); may be withdrawn from; gone at the end of the turn) or `encounter` (never offered at random: offered when the fleet shares a sector with a freighter, handed her name as `ship` and her faction as `freighter_fac`, and gone at the end of the turn; `escort.json` for a friendly freighter, `raid.json` for anyone else's; owner, 2026-11-07: "no mision flag needed. Just whenever you are on the same tile as freighter, you get a mission option") |
| `weight` | how often a contract is offered against the others, default 1 (Patrol 4; the nebula, belt and orbital patrols 2) |
| `type` | the Missions guide's mission type it plays (shown in the briefing) |
| `reward_cr` | base reward in credits; a contract adds 0 to 200 cr when offered |
| `where` | an expression, true for each sector the mission may be offered in |
| `follow_group` | true: the mission goes wherever the faction fleet in its sector goes |
| `takes` | the names handed to it by the mission that offered it (a prize's name) |
| `setup` | a list of `{ "set": name, "expr": ... }` or `{ "set": name, "pick": [ ... ] }`, rolled when offered |
| `brief`, `objective` | text, or a list of `{ "when": ..., "text": ... }` parts joined in order |
| `needs` | a list of `{ "if": ..., "say": ... }`: while one holds, the fleet cannot launch it, and the briefing says why |
| `battle` | a list of battles; the first whose `when` holds (or that has none) is fought. Every mission has one: none is done on arrival (owner, 2026-11-06: "you just skipped the mission!") |
| `grade` | optional list of `{ "when": ..., "is": grade }`, the first that holds; otherwise the default rule below |
| `result` | a list of outcomes; the first whose `when` holds (or that has none) is applied |

A battle is `{ when?, name?, intel?, groups, markers?, planets?, time_limit_s?, dead_engines?, cargo?, envoy?, objectives }`:
- A **group** is `{ id, faction, count, role, label?, tag?, tier?, hull?, bearing_deg?, range_km?, ship_name? }`.
  - `hull: "freighter"` fields the faction's freighter (`factions.<id>.freighter` in `data/campaign/campaign.json`, its own
    approved freighter) in place of warships; a freighter keeps her course at 600 m/s and never closes to fight, on any side.
    An encounter has one such group: the freighter met. Destroyed or taken, she is off her route for
    `rules.freighter_refit_turns` turns.
  - `role` is `enemy`, `ally` (wingmen beside your fleet) or `neutral` (ships to protect, which hold their fire).
  - `hull` may also field a placeholder (openspec/changes/mission-systems): `station`, `yard`, `asteroid_base`, `outpost` or
    `battery` (fixed in place: they never move), `pod` (a cargo pod to beam goods from; give it a `hold`) or `monster` (the
    void leviathan, of faction `fauna`).
  - `arrive_s`: seconds into the battle the group warps in at its place; until then it is unseen and out of reach.
    `wake_s`: seconds the group sleeps (no orders, no fire) before it powers up; hit first, it wakes at once.
  - `at_planet`: a planet of the battle the group orbits (batteries over a world).
  - `hold`: cargo in each ship's hold, by item (`goods`, `parts`, `rations`, `medicine`): a trader's goods, a pod's salvage.
  - `tag` is `flagship`, `prize`, `rescue`, `decoy`, `drone`, `quarry` (the ship a search is for), `scout` (a runner: it
    holds its course away from the fleet at full speed and never fights) or `truce` (holds its fire and its place until
    someone fires). A **drone** (an enemy group, a shakedown's targets)
    stands where it is put, never fires, and is a light hulk: no shields, 30 % of its hull. A **prize** is taken, not destroyed: beaten down to a quarter of
    its hull (`capture_hull_frac` in `data/battle/tuning.json`), it strikes and is boarded. A ship boarded and captured by
    your marines counts as taken too.
  - Enemy groups spread ahead at 100 km unless `bearing_deg` (clockwise from ahead) and `range_km` place them.
- A **marker** is a beacon to fly to: `{ id, label, bearing_deg?, range_km?, at?, after?, radius_m? }`.
  - It sits at `bearing_deg` (clockwise from ahead) and `range_km` (default 80) from the fleet's start, or at the start itself
    with `"at": "start"`.
  - `after` names a marker that must be reached first; until then it is not shown and cannot be reached.
  - The ship you command reaches it within `radius_m` (default 8000, 1000 to 50000).
  - A battle with markers ends when every primary objective is met (checked as it plays), not when the last enemy goes.
- A **planet** is `{ id, name, kind?, bearing_deg?, range_km?, radius_km?, color?, needs?, sites?, takes_envoy?, waiting? }`:
  - `kind` is its look (`terran`, `ocean`, `desert`, `ice`, `lava`, `barren` or `gas`); it sits at `bearing_deg` and
    `range_km` (default 60, the centre) from the fleet's start, `radius_km` (default 12) across.
  - `needs` is what it asks to be beamed down (relief); `takes_envoy` lets the envoy land there; `waiting` names someone on
    the surface to beam up.
  - `sites` are what stands on it: `{ id, name, kind, lat_deg, lon_deg, hostile?, garrison?, known? }`, `kind` one of
    `outpost`, `lab`, `ruin`, `mine`, `city`, `battery` or `wreck`. A site shows once the planet is surveyed (or from the
    start with `known`). An away team beamed down to it does its work, fights any `garrison`, and holds the site if it wins.
- `time_limit_s` ends the battle at that many seconds (a holding action, a scout's run); `time_up` is then true.
- `dead_engines` is `{ every_s, for_s }`: every ship's engines die for `for_s` seconds in every `every_s`.
- `cargo` is loaded in the flagship's hold at the start (relief supplies); `envoy` names an envoy aboard.
- A battle with markers, planets, a time limit, or a primary objective about scans, sites or cargo ends when its primary
  objectives are met, not only when the last enemy goes.
- An **objective** is `{ id, text, primary?, by_withdraw?, done }`.
  - `done` is checked when the battle ends.
  - The primary objectives decide win or lose; the rest are bonuses.
  - `by_withdraw` means withdrawing meets it, as breaking out of an ambush does.

An outcome is `{ when?, credits?, prestige?, standing?, prize?, haul?, haul_cr?, next?, log?, disband? }`:
- `credits` is an expression; `pay` is the grade's share of the reward, rounded to 10 cr.
- `prestige` defaults to the grade's (`rules.prestige` in `data/campaign/campaign.json`, doubled for story missions).
- `standing` maps a faction (text, so `{side}` works) to a change.
- `prize` adds a ship of that faction to your fleet.
- `haul` (a faction) and `haul_cr` (an expression) go together: the battle's freighter joins your fleet with her cargo,
  worth `haul_cr` credits when cashed out at a port that deals with you (never the faction she was taken from); she sails
  with the fleet but stays out of its battles. With no room in the fleet her cargo is moved across and paid at half.
- `disband: true` breaks up the faction fleet the mission follows (hunters beaten).
- `next` lists missions to offer: `{ mission, at: where | home | here | fleet, data: { name: text } }`.

## Grades

The default rule:

| How the battle ended | Grade |
|---|---|
| The fleet was lost | **crushing** |
| Every primary objective met, and every bonus too | **outstanding** |
| Every primary objective met | **victory** |
| Withdrawn short of the primary objectives | **draw** |
| Fought to the end short of them | **defeat** |

Victory and outstanding are wins. Each grade pays a share of the reward and earns prestige (`rules.pay` and
`rules.prestige`).

## Expressions and text

**Expressions** have:
- numbers, `'text'`, `true` and `false`;
- `+ - * / %`, `< <= > >= == !=`, `&& || !` and `a ? b : c`;
- `min`, `max`, `floor`, `ceil`, `round`, `abs` and `round10`.

`a || b` gives the first value that is set (`rival || 'vos'`).

**Text** fields fill in `{expression}` parts. The editor lists every name each place may use. The main ones:
- **everywhere:** `side`, `rival`, `enemy` (your rival, or the Vos), `foe` (the sector's holder if hostile, else
  `enemy`), `home`, `reward`, `strength` (1 to 3), `fleet` (ships), `marines`, the sector's `sector_owner`,
  `sector_station`, `sector_planet` and `sector_terrain`, `asteroids`, `nebula`, the fleet in the sector (`group`,
  `group_fac`, `group_size`), and `rand()`, `name(faction)`, `name_of(faction, i)`, `hull_of(faction, tier)` and
  `class_of(hull)`;
- **in `where`:** `owner`, `own`, `hostile`, `neutral`, `plain` (no port), `port`, `station`, `planet`, `terrain`,
  `dist_home`, `has_group`, `group_size_here`, `group_fac_here`, `commander_here`, `is_home` and `on_route(faction)`;
- **in `done`:** `how` (`end`, `withdraw` or `lost`), and `size`, `alive`, `destroyed`, `taken` and `hull_min` of a
  group id, `fleet`, `enemies` or `allies`, and `reached('marker')` (the loader refuses a marker the battle does not place);
  `scanned(group)` (how many of its ships your side finished scanning), `cargo('item')` (units in your fleet's holds);
  of a planet id (refused if the battle places no such planet): `surveyed('p')`, `secured('p')` (sites held),
  `sites('p')`, `relieved('p')`, `envoy_landed('p')` and `picked_up('p')`; and `time_up`, `we_fired_first`,
  `they_fired_first`, `no_shots` and `teams_down` (away teams still on a surface);
- **in `grade` and `result`:** `grade`, `win`, `pay`, `how`, `crushing` to `outstanding` as true or false,
  `all_primary`, `all_bonus`, `bonus_count` and `met('id', ...)` (how many of those objectives were met).

## An example

```json
{
 "id": "patrol", "name": "Patrol", "glyph": "P", "type": "Patrol", "reward_cr": 300,
 "where": "plain && (own || neutral)",
 "objective": "Sweep the sector and engage what you find.",
 "brief": [{"text": "Raiders have been seen on the trade lanes."}, {"when": "asteroids", "text": " They hide among the rocks here."}],
 "battle": [{"groups": [{"id": "raiders", "faction": "vos", "count": "strength", "role": "enemy", "label": "Raiders"}],
   "objectives": [{"id": "clear", "text": "Destroy the raiders", "primary": true, "done": "alive(raiders) == 0"},
                  {"id": "intact", "text": "Lose no ships", "done": "destroyed(fleet) == 0"}]}],
 "result": [{"when": "win", "credits": "pay", "standing": {"{side}": "outstanding ? 3 : 2"}}]
}
```
