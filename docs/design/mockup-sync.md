# Mockup sync

The sync record (CLAUDE.md section 4, "The game stays in step with its mockups"; owner, 2026-11-04: "keep the UI updated
with the mockups", "add to claude md to keep stuff updated and synced with mockups"). One row per mockup approved for
implementation: the version the game matches, and every difference still open, each with the task that closes it.
Update it with every change to the mockup or the game. Side-by-side sheets: `docs/screenshots/compare/`
(`tools/compare/side_by_side.py`).

## Where each implemented mockup stands (2026-11-04)

| # | Mockup | Live version | Game matches | Change |
|---|---|---|---|---|
| 2 | In-game UI (the battle console, Concord) | v74 | v65's first slice, plus the pause menu's background and the shield view always on (v67), and the scene selector | `battle-console-engine`, `battle-scenarios` |
| 6 | Concord line (the ships) | v17 | the intact ships as exported by `tools/mockups/export_ships.mjs` | `ship-models-engine` |
| 7 | Krai Warhost ships | v19 | the intact ships as exported by `tools/mockups/export_ships.mjs` | `ship-models-engine` |
| 8 | Battle console: Krai Warhost | v13 | the Krai kit on the game's console (the parts the game has), with the column plate and the 60 and 80 px strips | `krai-console-engine` |

Beams and bolts (2026-11-04, owner: "source and destination of phaser and disruptors must maintain in sync"): in step in
both. Mockup 2 v76 (and mockup 8 v15, written from it) resolve a beam's ends each step from the firing mount and the
target as they now stand (`shotEnds`) and fly disruptor bolts on that live line (`boltStep`); the game does the same in
`Battle.Ends` and `BoltStep` (`src/Core/Sim/Battle.cs`), drawn by `ViewOverlay`. Torpedoes and missiles fly free in both.
Change `weapon-fx`, tasks 2.1-2.3.

## Open differences

### Mockup 2 and 8 (one console, two skins)

Everything below is open in both skins: the Krai console is mockup 2's page in another kit, so each task closes it in both.

| Where | Mockup | Game | Task |
|---|---|---|---|
| Stations | Tactical, point defence, science, comms, repair, transport, fleet | "Not yet in the game" | `battle-console-engine` 2.1 |
| Ship pages | Sensors, tractor (the contest), craft | "Not yet in the game" | `battle-console-engine` 2.2 |
| Power | Reactor and Warp modes, the warp key on the speed slider | Reactor and Warp tabs disabled | `battle-console-engine` 2.3 |
| Radar row | Map opens the tactical map; the event log | Map disabled; no log page | `battle-console-engine` 2.5 |
| Weapon targeting (v71) | Aim-at icons, by-arc and precise weapons, the chopping block, shift-click hit details, pinned fire, the expanded Tactical grid | Not built | `weapon-targeting` |
| Damage control (v72, v73) | The repair queue with crews, ordered by drag and drop | Repair power mends the worst system | `damage-control` |
| Arming (v72) | Proximity torpedoes, overloaded lances | Overload for torpedoes and disruptors only | `weapon-arming` |
| Cloaking (v72) | Cloaks on both sides, the ECCM sweep | Not built | `cloaking` |
| Officers (v72) | An officer slot on every station with its hover card | Not built | `officers` |
| Boarding defence (v72) | The Security page, the deck plan in 3D, teams and boarders | Not built | `boarding-defence` |
| Wide keys, target weapons (v74) | Wide keys; the target's weapons as aim keys; point defence and missiles independently powered | Not built | `weapon-targeting` |
| Test scenes | Seventeen scenes | Battle, Duel and Arming, from both sides, the Krai against the Concord in the approved ships only | `battle-scenarios` 2.1-2.5 |
| Target box | Holder's emblem, boarding tug of war, Target view and Camera lock | The target's systems display and the release X | `battle-console-engine` 2.2 |
| Backdrop | Nebula sky with streaking dust | A simpler haze and stars | `battle-console-engine` |

### Mockup 8 only (the Krai skin)

Parity between the two skins is checked by the engine (`-- --parity-test`, in `scripts/run-tests.sh`): the same controls
in both, every control inside its panel, in 20 console states. What the Krai skin still draws differently from mockup 8:

| Where | Mockup | Game | Task |
|---|---|---|---|
| Panel titles | The ship and weapons titles on the divider above the panel | On the panel's own top edge (panels are clipped to their rectangles) | `krai-console-engine` 2.1 |
| Column light | The red channel blooms (v8) | No bloom | `krai-console-engine` 2.2 |
| Hover cards | The Krai's card shape | The game's card shape in Krai colours | `krai-console-engine` 2.3 |
| Icons | A glow, mitred corners | No glow, round corners | `krai-console-engine` 2.4 |
| Names | Kharz, Vekk, Ruhn and the Krai officers' names and ranks; the wireframe ship display | The scenario's names (the same three ships); no officers yet; the plan view | `krai-console-engine` 2.5 |

Not yet seen in a still: the Krai console has only been drawn headless. Stills on the owner's PC are task 1.7.

### Mockups 6 and 7 (the ships)

| Where | Mockup | Game | Task |
|---|---|---|---|
| Damage | Holes into the rooms, scorch marks, venting, nacelles shot off, the hull breaking apart | Ships vanish when destroyed | `ship-models-engine` |
| Lighting | Floodlights, navigation lights, strobes and beacons in three presets | Not lit yet | `ship-models-engine` |
| Windows | Openings with a room behind (interior mapping) | A warm emissive pane | `ship-models-engine` |
