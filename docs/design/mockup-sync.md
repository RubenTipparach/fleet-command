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
| 6 | Concord line (the ships) | v20 | v20: the intact ships exported by `tools/mockups/export_ships.mjs` (2026-11-04, Mac), the detail tile and the lamps; not its paint rules (below) | `ship-models-engine` |
| 7 | Krai Warhost ships | v21 | v21: the intact ships exported by `tools/mockups/export_ships.mjs` (2026-11-04, Mac), the detail tile, the livery and the lamps | `ship-models-engine` |
| 8 | Battle console: Krai Warhost | v13 | the Krai kit on the game's console (the parts the game has), with the column plate and the 60 and 80 px strips | `krai-console-engine` |

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
| The ships in the view | A stand-in Concord block against Kethran and Vos ships (v74, mockup 8 v14) | The approved ships, the Krai against the Concord only (owner: "NO PLACE HOLDERS SHOULD BE USED FOR KRAI VS CONCORD"); here the mockups are behind the game (sheets 2026-11-04: `docs/screenshots/compare/`) | the mockups' next version |

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

Seen in a real window on the Mac (2026-11-04): `docs/screenshots/game/krai-battle.png`, `krai-duel.png`, and the six
recorded bot runs (`docs/playtests/2026-11-04-scenario-bot-runs.md`).

### Mockups 6 and 7 (the ships)

| Where | Mockup | Game | Task |
|---|---|---|---|
| Damage | Holes into the rooms, scorch marks, venting, nacelles shot off, the hull breaking apart | Ships vanish when destroyed | `ship-models-engine` |
| Lighting | Floodlights, navigation lights, strobes and beacons in three presets | Every lamp from the export's light empties (`src/Game/View/ShipSkin.cs`): floods as spot lights, running lights and beacons, strobes double flashing; one preset (healthy), no presets switch | `ship-models-engine` |
| Concord paint | The hull shader's paint rules (`hullMat`: the subhull service band, livery stripes, dorsal seams, window rows, light pools) | The armour white and the detail tile only (`hull.gdshader` ports the shared detail and the Krai livery) | `ship-models-engine` |
| Phone lights | One renderer for all | About 120 lamps in a six-ship battle: fine in Forward+; the Compatibility renderer (phones) caps lights per mesh, so phones need a cut set | `ship-models-engine` |
| Windows | Openings with a room behind (interior mapping) | A warm emissive pane | `ship-models-engine` |
