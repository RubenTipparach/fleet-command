# Mockup sync

The sync record (CLAUDE.md section 4, "The game stays in step with its mockups"; owner, 2026-11-04: "keep the UI updated
with the mockups", "add to claude md to keep stuff updated and synced with mockups"). One row per mockup approved for
implementation: the version the game matches, and every difference still open, each with the task that closes it.
Update it with every change to the mockup or the game. Side-by-side sheets: `docs/screenshots/compare/`
(`tools/compare/side_by_side.py`).

## Where each implemented mockup stands (2026-11-04)

| # | Mockup | Live version | Game matches | Change |
|---|---|---|---|---|
| 2 | In-game UI (the battle console, Concord) | v81 | v65's first slice, plus the pause menu's background and the shield view always on (v67), and the scene selector | `battle-console-engine`, `battle-scenarios` |
| 6 | Concord line (the ships) | v24 | v24: the intact ships exported by `tools/mockups/export_ships.mjs` (2026-11-04, Mac), the detail tile, the lamps, the hot bells and the Albatross main hull's two-bell stern; its damage (hits, scorch marks, torpedo holes, parts blown off, the core breach and break-up into burning cells with cut faces) but not the subsystem signatures (`ship-damage-visuals` 3.2b); not its paint rules (below) | `ship-models-engine` |
| 7 | Krai Warhost ships | v21 | v21: the intact ships exported by `tools/mockups/export_ships.mjs` (2026-11-04, Mac), the detail tile, the livery and the lamps, and the same damage as mockup 6's | `ship-models-engine` |
| 8 | Battle console: Krai Warhost | v13 | the Krai kit on the game's console (the parts the game has), with the column plate and the 60 and 80 px strips | `krai-console-engine` |
| 1 | Primary game flow (menus, faction screen, campaign, missions) | v14 | v14's flow and rules (2026-11-05): the menu, the faction screen, skirmish, the campaign map with the same territory, moves, routes by turn, factions' fleets, freighters, mission banners, briefing, port, debrief with prestige and reputation; missions from `data/missions/`; the differences below | `game-flow-engine`, `mission-scripting` |

Ships (2026-11-05, owner: "looks like the albatros is behind a version, please update ships to latest"): in step. The
battle console mockup (mockup 2 v82, mockup 8 v21, mockup 15 v6) flies the same exported glbs the game flies, each class's
`model` from `data/battle/classes.json`, with the Krai's ships as the Krai line. Still open on the mockup side: the hull
paint (livery bands, windows, detail, light pools) is the game's `hull.gdshader` but has no shared three.js copy yet, so
the console shows the glbs' stand-in paint. Task: `ship-models-engine` 2.5.

Beams and bolts (2026-11-04, owner: "source and destination of phaser and disruptors must maintain in sync"): in step in
both. Mockup 2 v76 (and mockup 8 v15, written from it) resolve a beam's ends each step from the firing mount and the
target as they now stand (`shotEnds`) and fly disruptor bolts on that live line (`boltStep`); the game does the same in
`Battle.Ends` and `BoltStep` (`src/Core/Sim/Battle.cs`), drawn by `ViewOverlay`. Torpedoes and missiles fly free in both.
Change `weapon-fx`, tasks 2.1-2.3.

Shot looks (mockup 2 v77): open. The mockup draws lances as lit strips, disruptor bolts as hot streaks and torpedoes
and missiles as glowing warheads with trails (`docs/mockups/lib/weapon-fx.js`); the game's overlay still draws lines and
dots. Task: `weapon-fx` 3.4.

Schematics (2026-11-05, owner: "generate schematics for each ship ... I want these to be used in game, and in store"): the
game is ahead of the mockups. The battle's systems display and the campaign's shipyard, fleet and outfitter draw each ship's
two-layer schematic (`assets/ships/schematics/`, `src/Game/Ui/Schematics.cs`); the battle console mockup (mockups 2, 8, 15)
still draws its outline, and the campaign mockup's shipyard its own. Task: `ship-schematics` 2.1.

Missions (2026-11-05): the game reads `data/missions/`; the campaign mockup still runs `docs/mockups/lib/campaign-missions.js`,
kept the same by hand (the files were written from it, 2026-11-05). Task: `mission-scripting` 2.1.

## Open differences

### Mockup 1 (the game flow)

| Where | Mockup | Game | Task |
|---|---|---|---|
| Faction screen | 3D emblems on wooden pedestals on concrete, a light and dust on hover, the emblem turning | the rendered emblems flat on kit frames; the Ssarith card disabled (no console skin in the game) | `game-flow-engine` 2.1 |
| Map look | parallax star shader, the board reflecting the sky, nebula puffs with an accent colour, cloth banners | flat stars, unlit hex tiles, puffs in one colour, flat flags that sway | `game-flow-engine` 2.2 |
| Skirmish | fleet building and loadouts on a points budget | the game's scenarios, played as written | `game-flow-engine` 2.3 |
| Refits | fittings priced and shown | priced and saved, but a hull fights as its battle class | `game-flow-engine` 2.3 |
| Officers | hired | hired, no effect in battle | `game-flow-engine` 2.4 |
| Phones | every screen at phone sizes | not yet checked at phone sizes | `game-flow-engine` 2.5 |
| Options | sound levels, quality, UI scale | display mode only | `game-flow-engine` 2.6 |
| Battles | the stand-in battle with an End battle key | the real battle console, the mission's groups placed by bearing; Withdraw; the battle ends when one side is gone | none: the game is the target |
| Station and planet assaults | fight the base | fight its guard squadron (no stations or planets in the battle yet) | `mission-scripting` 2.3 |

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
| Target box | Holder's emblem, boarding tug of war | The target's systems display, the release X, and (2026-11-04) Target view and Camera lock: camera lock is the default on every target lock, a drag frees it (`docs/screenshots/game/target-view.png`) | `battle-console-engine` 2.2 |
| Backdrop | Nebula sky with streaking dust | A simpler haze and stars | `battle-console-engine` |
| Pointing in the view | three.js's raycaster from the pointer, through the camera | The same ray, by the inverse of the camera's own projection (2026-11-04: Godot's ProjectRayNormal ignored the view's offset frustum and missed by up to 2.16 km; owner: "mouse to screen ray cast is wrong, its not working like the JS version", "cant properly turn my ship in the desired direction"); the layout test checks a click lands within 1 m | none: in step |
| Menu: Display (game ahead) | Resume, mockup settings, restart, quit | A Display key: borderless fullscreen (the default) or a window, also F11 and Alt+Enter, remembered (owner: "need a fullscreen /window option") | `battle-console`: the page's own fullscreen key (requestFullscreen) |
| 3D shield display (v81, mockup 8 v20) | Soft glowing bands in the ship's plane, one to three per facing by strength, the inner one flashing when hit, on every ship (owner, 2026-11-04: "shield display not matching mockup, also one change needed is to add shield display to all ships as well") | The same: `ShieldShell`, a port of the mockup's shieldShell(), in place of the thin lines the overlay drew round your ship only | none: in step |
| Strips (v79, mockup 8 v18) | The top and bottom strips on black (owner, 2026-11-04: "this top part needs a blcak background, same with bottom stuff too") | The same | none: in step |
| Core breach (game ahead, mockups 6 and 7) | The main blast: an orange fireball, flash and shockwave | A white star with pink edges, a flat blue disc racing out through the battle plane, debris trailing flame (owner, 2026-11-04: "center is white with pink edges, and a blue ring glowing outward", "I really want it to have this exact look and colors, and the debris should have trails of flame"); `Fx.Nova`, `novastar.gdshader`, `novadisc.gdshader` | `ship-damage-visuals`: port the breach to `lib/ship-break.js` mainBlast() |
| Wrecks scene (game ahead) | Not on the scene list | Wrecks, from both sides: every class of the other side, crippled, to watch each come apart (owner, 2026-11-04: "show me you fixed each ship type") | `battle-scenarios`: add Wrecks to the mockup's scene list |
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
| Lighting | Floodlights, navigation lights, strobes and beacons in three presets | Every lamp from the export's light empties (`src/Game/View/ShipSkin.cs`): floods baked into the hull as the mockups' light pools (2026-11-04, owner: "I thought we were using emissive baked lights + shader based multi specular? for ship hull detail lighting"), each with its own specular glint, which the mockups' pools() lack (game ahead: `ship-models-engine`, add the glint to pools()); running lights and beacons, strobes double flashing; one preset (healthy), no presets switch | `ship-models-engine` |
| Concord paint | The hull shader's paint rules (`hullMat`: the subhull service band, livery stripes, dorsal seams, window rows) | The armour white and the detail tile, the light pools (`hull.gdshader` ports the shared detail, the Krai livery and pools()) | `ship-models-engine` |
| Phone lights | One renderer for all | The floods are baked pools now, so a six-ship battle carries only the small running lights, strobes and beacons (about 80 omni lights); the Compatibility renderer still caps lights per mesh, so phones need a cut set of those | `ship-models-engine` |
| Windows | Openings with a room behind (interior mapping) | A warm emissive pane | `ship-models-engine` |
