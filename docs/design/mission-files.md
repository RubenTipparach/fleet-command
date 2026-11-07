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
| `kind` | `contract` (offered for `turns`, default 3, then gone) or `story` (waits until done) |
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

A battle is `{ when?, name?, intel?, groups, markers?, objectives }`:
- A **group** is `{ id, faction, count, role, label?, tag?, tier?, bearing_deg?, range_km?, ship_name? }`.
  - `role` is `enemy`, `ally` (wingmen beside your fleet) or `neutral` (ships to protect, which hold their fire).
  - `tag` is `flagship`, `prize`, `rescue`, `decoy` or `drone`. A **drone** (an enemy group, a shakedown's targets)
    stands where it is put, never fires, and is a light hulk: no shields, 30 % of its hull. A **prize** is taken, not destroyed: beaten down to a quarter of
    its hull (`capture_hull_frac` in `data/battle/tuning.json`), it strikes and is boarded.
  - Enemy groups spread ahead at 100 km unless `bearing_deg` (clockwise from ahead) and `range_km` place them.
- A **marker** is a beacon to fly to: `{ id, label, bearing_deg?, range_km?, at?, after?, radius_m? }`.
  - It sits at `bearing_deg` (clockwise from ahead) and `range_km` (default 80) from the fleet's start, or at the start itself
    with `"at": "start"`.
  - `after` names a marker that must be reached first; until then it is not shown and cannot be reached.
  - The ship you command reaches it within `radius_m` (default 8000, 1000 to 50000).
  - A battle with markers ends when every primary objective is met (checked as it plays), not when the last enemy goes.
- An **objective** is `{ id, text, primary?, by_withdraw?, done }`.
  - `done` is checked when the battle ends.
  - The primary objectives decide win or lose; the rest are bonuses.
  - `by_withdraw` means withdrawing meets it, as breaking out of an ambush does.

An outcome is `{ when?, credits?, prestige?, standing?, prize?, next?, log? }`:
- `credits` is an expression; `pay` is the grade's share of the reward, rounded to 10 cr.
- `prestige` defaults to the grade's (`rules.prestige` in `data/campaign/campaign.json`, doubled for story missions).
- `standing` maps a faction (text, so `{side}` works) to a change.
- `prize` adds a ship of that faction to your fleet.
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
