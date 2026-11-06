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
| 1 | Primary game flow (menus, faction screen, campaign, missions) | v19 | v18 and v19 (the walnut faction screen, stores, crew lists, kept routes, options; 2026-11-06); v17 (ships slide between sectors); v15 (Accept sets course; the debrief over the battle); v14's flow and rules (2026-11-05): the menu, the faction screen, skirmish, the campaign map with the same territory, moves, routes by turn, factions' fleets, freighters, mission banners, briefing, port, debrief with prestige and reputation; missions from `data/missions/`; the differences below | `game-flow-engine`, `mission-scripting` |
| 16 | Special weapons range | v5 | v5's simulation and looks: the four missile grades with flares, decoy shuttle and point defence; the chorus lance, quell torpedo, forge driver and blood charge with their rules and effects. Not its shield bubble ripple, haze or motes (below) | `weapon-fx`, `missiles`, `special-weapons` |
| 12 | Ssarith Hunt ships | v26 | v26's ships (2026-11-06; v25 halves the floods and doubles their pools; v26 drops the page's dots at the floods, which the game never drew): the four frigates, the cruiser and the carrier exported by `tools/mockups/export_ships.mjs`, dressed by their own baked maps (`baked_hull.gdshader`), their lamps with the page's glows (a strobe's flashing, a beacon's pulsing; none at the floods), every window and launch bay a pane with its room or hangar behind it (`pane.gdshader`, `ship_rooms.gdshaderinc`), and the same damage and breakup as the other lines (cut faces by each hull's signed distance field). Differs: which furniture a given window shows (the game's room hash is PCG, the page's fract(sin)). The Hunt is also a side you command (its console, change `ssarith-console-engine`). Still open: its own weapons (2.3), `*.bin` in an export preset (2.5) | `ssarith-ships-engine` |

Distances (2026-11-06, owner: "also multiply all distance by 10", "1km -> 10km"): the game's battle distances are ten times
the mockups'. Every distance in `data/battle/` and `data/missions/` (positions, ranges, speeds, accelerations, map scales,
reaches and radii) is tenfold, a scene unit is 1 km (`BattleView.MPerU`), and `tuning.json` `ship_frame_x` (10) frames each
ship at ten times its `length_m`, so the battle looks and plays as before (the golden seed is unchanged) while every
readout says ten times the figure: a ship 1.1 km off now reads 11 km. Ship lengths, deck plans and the boarding parties'
walking inside a hull are not scaled. The mockups (2, 8, 15, 16 and the console skins) still use the old figures and
`tools/mockups/inline_data.py` inlines the scaled data into them, so they are out of step until their scene unit is 1 km and
their ships are drawn at `ship_frame_x` times their length. Task: `battle-console-engine` 2.8, "distances x10 in the
mockups".

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

Shield bands by class (2026-11-06, owner: "frigates have only 1 layer of shields as opposed to larger ships"): in step.
`shield_layers` in `data/battle/classes.json` (frigates and corvettes 1, destroyers, corsairs and troop ships 2, cruisers and
up 3) sets how many bands a facing shows at full strength, read by the game's `ShieldShell.Layers` and by the four battle
console pages' `shieldLayers` from their inlined table.

Shots between steps (2026-11-06, owner: "why does weapon appear jittery?"): the game draws a shot in flight between its last
two fixed steps (`Battle.DrawnAt`), as the mockups already do by stepping every animation frame. Nothing to sync.

Game-side changes the game-flow mockup (mockup 1) does not show yet (2026-11-06), task `game-flow-engine` 2.10 "sync mockup 1
to the game's 2026-11-06 changes":
- the mission goals over the view have a close key, and the Comms station lists them with an Over view key to show them
  again; a battle opens on Comms (owner: "add close button to goals, and put them in comms panel. Start game with coms panel
  open by default");
- the campaign map's Menu opens the pause menu (Resume, Main menu, Options, Quit) in place of going straight to the main
  menu (owner: "menu button in campaign map should be the pause menu");
- the debrief stands on black, the black the battle fades to (owner: "I think this background should be black").

## Open differences

### Mockup 1 (the game flow)

| Where | Mockup | Game | Task |
|---|---|---|---|
| Faction screen | the badges floating over a walnut table (the `table_wood` graph) on brass rings, bobbing, turning toward the pointer; the picked faction's lamp full with its beam and dust, the hovered one's half | matched 2026-11-06 (`FactionStage.cs`, from `tools/mockups/export_faction.mjs`: the page's table, badges and numbers; `holo_field.gdshader`, `faction_beam.gdshader`, `faction_dust.gdshader`). Still different: the Concord pearl's thin-film colour shift and the satin's sheen are a clearcoat and a rim light; the wood takes no environment light at all where the page gives it 5 % | `faction-select` 3.1, 3.4 |
| Map look | the board reflecting the sky in a planar mirror (three.js Reflector); stars that streak as the view pans (the FreeSpace-style streak shader) | matched 2026-11-05 (`CampaignMap.cs`, `MapPieces.cs`, `puff.gdshader`, `banner.gdshader`): the quiet nebula sky and its stars behind, three layers of parallax stars under the board, dark glass hexes washed in the holder's colour with ports lighter, holder-coloured border ribbons, nebula puffs with a main and an accent colour from the owner's palette (the mockup's own random numbers), asteroid belts, stars with halos, textured planets with atmospheres and orbit rings, stations, flight stands (gold for the player, with its lamp ring), ships facing their last move and flying their moves sector by sector, strength flags and mission gonfalons as rippling cloth, freighters on dashed routes (allies' routes dimmer), labels that keep their size and leave at far zoom, the route ribbon with turn badges and arrowhead, the fixed camera angle; the HUD's top bar (captain, credits, prestige, turn, moves, the Missions key and mission log, standing chips, fleet chips with hull bars), zoom and Fleet keys, End turn, the sector panel with the port's keys. Still different: the glass reflects the sky panorama (an environment light), not a planar mirror of the pieces; the stars under the board are points, not streaks | `game-flow-engine` 2.2 |
| Menu backdrop (game behind) | Mockup 1's menu stands in front of a 3D scene: the verdigris nebula sky, a planet and a Petrel | The game's menu draws a flat `Starfield2D` field (seeded, Poisson disc, a band and clusters) in place of the old modular-hash dots; no sky, planet or ship yet. Mockup 22 (the fly-through) is the owner's candidate for it | `space-flythrough` 3.1 |
| Skirmish | fleet building and loadouts on a points budget | the game's scenarios, played as written | `game-flow-engine` 2.3 |
| Port (v16) | Shipyard, Loadout, Supplies, Crew tabs, the fleet down the left, schematics, quoted prices | the same (`PortPanel.cs`), in sync 2026-11-05; the game's port covers the width with the map dimmed, the mockup's on desktop too, and on a phone it keeps the map strip | none |
| Refits | fittings priced and shown | priced and saved, but a hull fights as its battle class | `game-flow-engine` 2.3 |
| Stores and crew lists | fitting free from the fleet's stores, a store to buy and sell systems; every crew station's list of three experienced officers and five cadets | matched 2026-11-06 (`Campaign.cs`, `PortPanel.cs`) | `game-flow-engine` 1.13 |
| Options | volumes in boxes, the window mode; graphics quality and interface scale | the volumes and the window mode matched 2026-11-06 (`OptionsCard`); quality and interface scale not in the game | `game-flow-engine` 2.6 |
| Routes | the set course kept until End turn, later turns dotted | matched 2026-11-06 (`CampaignMap.ShowRoute`) | `game-flow-engine` 1.13 |
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
| Stations | Tactical, point defence, science, comms, repair, transport, fleet | All built (2026-11-06): Tactical (aim keys, contacts, pins, the expanded grid), Point defence (modes, own power), Science (scan, ECM, ECCM, cloak, sweep), Comms (hail, fleet orders, the event log), Repair (the crews' queue by drag and drop), Transport (capture, hit and run, cargo, mines, security), Fleet (stances, rules, retreat, take command) | none: in step; smaller gaps below |
| Ship pages | Sensors, tractor (the contest), craft | All built: Sensors, Tractor (Hold and Break, grip, rounds, tether), Craft (fighters, barrage, shuttle) | none: in step |
| Power | Reactor and Warp modes, the warp key on the speed slider | Built: the Reactor mode (coils, loops, heat), the Warp mode (banks, plot, risk), the warp key arming, plotting and jumping | none: in step |
| Radar row | Map opens the tactical map; the event log | Built: the Map key and dial open the tactical map (10, 20, 40 km); the event log from Comms | none: in step |
| Weapon targeting (v71) | Aim-at icons, by-arc and precise weapons, the chopping block, shift-click hit details, pinned fire, the expanded Tactical grid | Built (the chopping block is every hit's rule) | none: in step |
| Damage control (v72, v73) | The repair queue with crews, ordered by drag and drop | Built | none: in step |
| Arming (v72) | Proximity torpedoes, overloaded lances | Built | none: in step |
| Cloaking (v72) | Cloaks on both sides, the ECCM sweep | Built, with nebulae, ECM and ECCM | the tactical map's nebula disc; the Target box's detail waiting on a scan |
| Officers (v72) | An officer slot on every station with its hover card | Built: slots, hover cards, the helm, power, repair and science officers' abilities | the tactical, fleet, comms, point defence and security officers' abilities (`officers` 2.3); campaign officers in the battle's slots (2.5) |
| Boarding defence (v72) | The Security page, the deck plan in 3D, teams and boarders | Built on the Transport station's Security tab; the deck plan drawn by projection, not a second renderer | each class's own deck plan (Petrel's and the Krai frigate's stand in) |
| Wide keys, target weapons (v74) | Wide keys; the target's weapons as aim keys; point defence and missiles independently powered | Built | none: in step |
| Test scenes | Twenty scenes | All twenty in the game from both sides (and the Ssarith's own), picked by type then faction | mines and rocks on the radar minimap; point defence against fighters; planets as solid rocks |
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
| Missions flown (game ahead) | `lib/campaign-missions.js`: the Shakedown cruise and Deliver the prize are done on arrival | Every mission has a battle: the Shakedown is flown (a marker beacon, target drones, back to the start), Deliver the prize fought (the prize's side comes to take her back); marker beacons, drones and battles that end on their objectives in the mission format (owner, 2026-11-06: "you just skipped the mission!") | `mission-scripting` 2.1 (the mockup to read the mission files) |
| Mission strip and Withdraw (game ahead) | The strip over the battle with its objectives and a Withdraw key | The strip's frame with nothing behind the objectives, each ticked when met; Withdraw in the battle's menu in a mission (owner, 2026-11-06: "withdraw button should be in main meny, not here. make background transparent on goal") | `game-flow-engine` 2.8 |
| Skirmish (game ahead) | One key per scenario and side | The scenario types once each, scrolled (wheel or bar), the type picked on the right with its objective and a Play as key for each faction (owner, 2026-11-06: "I only need the scenario type, then when I select a scenario, I need to see what faction I can play that scenario AS", "Also need a scroll bar") | `game-flow-engine` 2.9 |
| Friendly targets (game ahead) | Mockup 2 selects friendly ships for transport | The game now locks friendly ships too (green bracket), its weapons holding on them (owner, 2026-11-06: "I also cant target friendly units", "firting between friendly units is not possible", "mainly for transporiting goods") | `battle-console-engine` (transport) |
| Faction consoles (game ahead of nothing) | Mockups 2, 8, 15, 20: the Concord, Krai, Ssarith and Kelvane consoles | All four kits in the game, the console following the ship you command (Allied fleet, Four races), layout and parity tests across all four | the skins' own live drawings (systems outline, radar sweep, brackets), their own ship and officer names, the Kelvane emblem (`fleet-skins`) |

Helm keys and mines (2026-11-06; owner: "the numbers arent in the buttons for turn angle, also these icons need a little
more space on these buttons", "Institute 90degree to the right is busted. Stop isnt centered. The ssarith tol has same
issue", "Uncaught ReferenceError: Rm is not defined ... mines just failed on the institution mockup screen"). In step on
both sides: battle console v86, Krai v25, Ssarith v10 and Kelvane v4 (committed, to publish) and the game's four skins
measure each key's glass from its own renders, walked out from the key's centre (the page's keyGlass, the game's
RenderedPieces.Glass), and lay the glyph and the turn's angle inside it by the same numbers (the page's helmKeyLayout, the
game's HelmKeyLayout, tested). The mine preview's crash was the mockup's alone (the game's mines are drawn elsewhere).

