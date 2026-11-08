# Proposal: mission-systems

## Why

The owner, 2026-11-08: "Design placeholders for: monsters and stations", "In game Planets: pull in planets from godot-sandbox
and falchion. Falchion will especially be important for us to beam down or examine planetary installations for exploratory or
tactical purposes. Also just zooming down to planets is supper cool", "Mission: looks like other than monsters and stations.
The one big thing we need is sensor systems. So start implementing that. We have most of transporter abilities working, so
implement missions for that as well", and "All in all, all missions should be implemented".

The Missions guide (`docs/design/mission-guide.html`) catalogued about thirty mission types; the game had files for nine of
them. The rest waited on stations in battle, planets with something on them, scanning, timed arrivals and a mission timer,
props, neutral traffic and a truce.

## What changes

- **Field operations in the battle** (`src/Core/Sim/Battle.Field.cs`, numbers in `data/battle/systems/field.json`):
  - the sensors' reach (base plus energy, times health, less on passive); a contact beyond every friendly ship's reach is an
    **unknown contact** until it comes within reach or is scanned; every finished scan is remembered for the mission;
  - **planet surveys**: a planet scanned from within the survey reach finds what stands on it;
  - **installations and away teams**: a team of marines beamed down to a site does its work, fights any garrison, and beams
    back up when its ship is in reach; the envoy lands on a planet that receives one, and someone waiting on the surface
    can be beamed up;
  - **late arrivals** that warp in at their time, **sleepers** that wake at their time or when hit, **runners** that make for
    home, a **truce** that holds until someone fires, **fixed** installations that never move;
  - a battle **time limit** and a **dead engines** world condition.
- **Placeholders**: battle classes for a shipyard, an asteroid base, an outpost, a planetary battery, a cargo pod and the void
  leviathan (a monster of no faction, the fauna), each drawn by a stand-in scene until designed.
- **Planets in the view** from the owner's other projects (Falchion's procedural planets, godot-sandbox's atmosphere shell):
  one look per kind, installations shown once found, and a player-chosen close view down to a planet.
- **The mission format** gains planets with sites, placeholder hulls, arrivals, sleepers, orbits, cargo, an envoy, time limits,
  dead engines, the tags quarry, scout and truce, and new checks (`scanned`, `cargo`, `surveyed`, `secured`, `relieved`,
  `envoy_landed`, `picked_up`, `time_up`, who fired first).
- **Every catalogue type is a mission file**: 23 new files, and the station and planetary assaults now fight a station and
  batteries over a planet.

## Impact

`src/Core/Sim`, `src/Core/Data`, `src/Core/Campaign` (MissionScript, MissionRun, MissionScopes), `data/battle` (classes,
factions, comms, cloak deck plans, the new field.json), `data/missions`, the game's view and stations panels, the stand-ins in
`scenes/standins/`, `docs/design/mission-files.md`, the Missions guide. Placeholders and planets are first-pass art the owner
asked for directly; their own designs follow as mockups (CLAUDE.md section 4).
