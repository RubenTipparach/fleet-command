# Mockup sync

The sync record (CLAUDE.md section 4, "The game stays in step with its mockups"; owner, 2026-11-04: "keep the UI updated
with the mockups", "add to claude md to keep stuff updated and synced with mockups"). One row per mockup approved for
implementation: the version the game matches, and every difference still open, each with the task that closes it.
Update it with every change to the mockup or the game. Side-by-side sheets: `docs/screenshots/compare/`
(`tools/compare/side_by_side.py`).

## Where each implemented mockup stands (2026-11-05)

| # | Mockup | Live version | Game matches | Change |
|---|---|---|---|---|
| 2 | In-game UI (the battle console, Concord) | v83 | v65's first slice, plus the pause menu's background and the shield view always on (v67), and the scene selector | `battle-console-engine`, `battle-scenarios` |
| 6 | Concord line (the ships) | v24 | v24: the intact ships exported by `tools/mockups/export_ships.mjs` (2026-11-04, Mac), the detail tile, the lamps, the hot bells and the Albatross main hull's two-bell stern; its damage (hits, scorch marks, torpedo holes, parts blown off, the core breach and break-up into burning cells with cut faces) but not the subsystem signatures (`ship-damage-visuals` 3.2b); not its paint rules (below) | `ship-models-engine` |
| 7 | Krai Warhost ships | v21 | v21: the intact ships exported by `tools/mockups/export_ships.mjs` (2026-11-04, Mac), the detail tile, the livery and the lamps, and the same damage as mockup 6's | `ship-models-engine` |
| 8 | Battle console: Krai Warhost | v22 | the Krai kit on the game's console (the parts the game has), with the column plate and the 60 and 80 px strips | `krai-console-engine` |
| 1 | Primary game flow (menus, faction screen, campaign, missions) | v15 | v15 (Accept sets course; the debrief over the battle); v14's flow and rules (2026-11-05): the menu, the faction screen, skirmish, the campaign map with the same territory, moves, routes by turn, factions' fleets, freighters, mission banners, briefing, port, debrief with prestige and reputation; missions from `data/missions/`; the differences below | `game-flow-engine`, `mission-scripting` |
| 16 | Special weapons range | v5 | v5's simulation and looks: the four missile grades with flares, decoy shuttle and point defence; the chorus lance, quell torpedo, forge driver and blood charge with their rules and effects. Not its shield bubble ripple, haze or motes (below) | `weapon-fx`, `missiles`, `special-weapons` |

Ships (2026-11-05, owner: "looks like the albatros is behind a version, please update ships to latest"): in step. The
battle console mockup (mockup 2 v82, mockup 8 v21, mockup 15 v6) flies the same exported glbs the game flies, each class's
`model` from `data/battle/classes.json`, with the Krai's ships as the Krai line. Still open on the mockup side: the hull
paint (livery bands, windows, detail, light pools) is the game's `hull.gdshader` but has no shared three.js copy yet, so
the console shows the glbs' stand-in paint. Task: `ship-models-engine` 2.5.

Kelvane console (2026-11-06, change `kelvane-battle-console`): mockup 2 v83 and mockup 8 v22 change nothing the game's
Concord or Krai console shows:
- they add the Kelvane research cruiser to the allied and swap scenes, which the game does not have;
- they add two skin hooks, `keyField` and `titleDy`, which the Concord and the Krai leave unset.

The Kelvane console (mockup 20) is not approved for implementation, so the game has no Kelvane skin to keep in step.

Beams and bolts (2026-11-04, owner: "source and destination of phaser and disruptors must maintain in sync"): in step in
both. Mockup 2 v76 (and mockup 8 v15, written from it) resolve a beam's ends each step from the firing mount and the
target as they now stand (`shotEnds`) and fly disruptor bolts on that live line (`boltStep`); the game does the same in
`Battle.Ends` and `BoltStep` (`src/Core/Sim/Battle.cs`), drawn by `ShotView`. Torpedoes and missiles fly free in both.
Change `weapon-fx`, tasks 2.1-2.3.

Shot looks (mockup 2 v77, mockup 16 v5): in step since 2026-11-05. The game draws every shot in 3D as the range and the
mockup draw it (`src/Game/View/ShotView.cs` with `beam`, `flare` and `beads` shaders, glow on): lances as lit strips,
disruptor bolts as hot streaks, torpedoes as six-armed purple stars with bead trails, missiles as body, ball plume and
tail in their grade's colour, point defence as gold tracers, flares and the decoy shuttle. Task `weapon-fx` 3.4.

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
| Map look | the board reflecting the sky in a planar mirror (three.js Reflector); stars that streak as the view pans (the FreeSpace-style streak shader) | matched 2026-11-05 (`CampaignMap.cs`, `MapPieces.cs`, `puff.gdshader`, `banner.gdshader`): the quiet nebula sky and its stars behind, three layers of parallax stars under the board, dark glass hexes washed in the holder's colour with ports lighter, holder-coloured border ribbons, nebula puffs with a main and an accent colour from the owner's palette (the mockup's own random numbers), asteroid belts, stars with halos, textured planets with atmospheres and orbit rings, stations, flight stands (gold for the player, with its lamp ring), ships facing their last move and flying their moves sector by sector, strength flags and mission gonfalons as rippling cloth, freighters on dashed routes (allies' routes dimmer), labels that keep their size and leave at far zoom, the route ribbon with turn badges and arrowhead, the fixed camera angle; the HUD's top bar (captain, credits, prestige, turn, moves, the Missions key and mission log, standing chips, fleet chips with hull bars), zoom and Fleet keys, End turn, the sector panel with the port's keys. Still different: the glass reflects the sky panorama (an environment light), not a planar mirror of the pieces; the stars under the board are points, not streaks | `game-flow-engine` 2.2 |
| Skirmish | fleet building and loadouts on a points budget | the game's scenarios, played as written | `game-flow-engine` 2.3 |
| Port (v16) | Shipyard, Loadout, Supplies, Crew tabs, the fleet down the left, schematics, quoted prices | the same (`PortPanel.cs`), in sync 2026-11-05; the game's port covers the width with the map dimmed, the mockup's on desktop too, and on a phone it keeps the map strip | none |
| Refits | fittings priced and shown | priced and saved, but a hull fights as its battle class | `game-flow-engine` 2.3 |
| Officers | hired | hired, no effect in battle | `game-flow-engine` 2.4 |
| Phones | every screen at phone sizes | not yet checked at phone sizes | `game-flow-engine` 2.5 |
| Options | sound levels, quality, UI scale | display mode only | `game-flow-engine` 2.6 |
| Transitions and loading | screens switch at once | screens fade through black; a loading screen with a progress bar at boot (shader prewarm), before the map and before a battle | `game-flow-engine` 2.7 |
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
| Damage range (game ahead) | The ship mockups' Damage tab (mockups 6 and 7: fire with a click, scenarios, repair); not on the battle console's scene list | Damage range, from both sides: every approved class on the range, weapon keys, click the plating to fire, programs (Battle scars, Lance rake, Bolt burns, Torpedo holes, Missile salvo, Break a part, Core breach), Shields, Slow motion, Repair all (owner, 2026-11-05) | `battle-scenarios` 3.6, 3.7 |
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

### Mockup 16 (the weapons range)

The range is approved for implementation (2026-11-05, "Implement weapons in game"). Two kinds of difference remain: what
the range shows that the game lacks, and what the game now has that the battle console mockups (2 and 8) do not show yet.

| What | Mockup | Game | Task |
|---|---|---|---|
| Shield bubble | Invisible at rest, a ripple where a hit lands | A blue particle flare where a stopped shot strikes (`Fx.ShieldFlare`); no bubble ripple | `weapon-fx` 3.1 |
| Haze, motes, scars | Haze round beams, drifting motes, scars on the hull | Not built (the hull's damage marks are `ship-damage-visuals`') | `weapon-fx` 3.1, 3.2 |
| Blood charge order | The run is ordered on the range | Only the AI charges; no player input action or key yet | `special-weapons` 2.8, 1.5 |
| Specials and grades on the panel | Not on the console mockups yet | Specials fire as ordinary mount rows with new glyphs (chorus, quell, slug); a rack's grade is not shown | `special-weapons` 1.5, `missiles` 1.5, 2.6 |
| Flares and decoy orders | On the range they fire by themselves | Automatic, as on the range; no player order yet | `missiles` 2.6 |

### Mockups 6 and 7 (the ships)

| Where | Mockup | Game | Task |
|---|---|---|---|
| Damage | Holes into the rooms, scorch marks, venting, nacelles shot off, the hull breaking apart | Ships vanish when destroyed | `ship-models-engine` |
| Lighting | Floodlights, navigation lights, strobes and beacons in three presets | Every lamp from the export's light empties (`src/Game/View/ShipSkin.cs`): floods baked into the hull as the mockups' light pools (2026-11-04, owner: "I thought we were using emissive baked lights + shader based multi specular? for ship hull detail lighting"), each with its own specular glint, which the mockups' pools() lack (game ahead: `ship-models-engine`, add the glint to pools()); running lights and beacons, strobes double flashing; one preset (healthy), no presets switch | `ship-models-engine` |
| Concord paint | The hull shader's paint rules (`hullMat`: the subhull service band, livery stripes, dorsal seams, window rows) | The armour white and the detail tile, the light pools (`hull.gdshader` ports the shared detail, the Krai livery and pools()) | `ship-models-engine` |
| Phone lights | One renderer for all | The floods are baked pools now, so a six-ship battle carries only the small running lights, strobes and beacons (about 80 omni lights); the Compatibility renderer still caps lights per mesh, so phones need a cut set of those | `ship-models-engine` |
| Windows | Openings with a room behind (interior mapping) | A warm emissive pane | `ship-models-engine` |
