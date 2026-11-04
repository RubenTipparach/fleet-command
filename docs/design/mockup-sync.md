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
| 7 | Krai Warhost ships | v16 | the intact ships as exported by `tools/mockups/export_ships.mjs` | `ship-models-engine` |
| 8 | Battle console: Krai Warhost | v13 | the Krai kit on the game's console (the parts the game has) | `krai-console-engine` |

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
| Test scenes | Seventeen scenes | Battle, Raiders and Arming, from both sides | `battle-scenarios` 2.1-2.5 |
| Target box | Holder's emblem, boarding tug of war, Target view and Camera lock | The target's systems display and the release X | `battle-console-engine` 2.2 |
| Backdrop | Nebula sky with streaking dust | A simpler haze and stars | `battle-console-engine` |

### Mockups 6 and 7 (the ships)

| Where | Mockup | Game | Task |
|---|---|---|---|
| Damage | Holes into the rooms, scorch marks, venting, nacelles shot off, the hull breaking apart | Ships vanish when destroyed | `ship-models-engine` |
| Lighting | Floodlights, navigation lights, strobes and beacons in three presets | Not lit yet | `ship-models-engine` |
| Windows | Openings with a room behind (interior mapping) | A warm emissive pane | `ship-models-engine` |
