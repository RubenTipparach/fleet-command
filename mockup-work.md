# Mockup work

The rolling list of mockups Fleet Command needs (owner, 2026-10-30: "we'll begin with a rolling list
of mockups needed"). Every mockup is an interactive three.js page, written up in OpenSpec first, and
passes two approvals before it reaches the game (CLAUDE.md section 4):

1. **Drafted**: the write-up and the page exist and are published.
2. **Approved for build**: the owner approves it as the design to build.
3. **Approved for implementation**: the owner approves putting it in the game. Only then does game
   code start, in a separate request.

Every mockup is checked into the repository, the page in `docs/mockups/` and its record in
`docs/artifacts/<name>/` (owner, 2026-10-30: "All mockups are to be checked into repo especially when
they are html claude artifacts"). The **mockup tracker** lists every link and stage, and is where the
owner approves: https://claude.ai/artifact/8VqA9YjhCrgoiY2CGR1Qxu (record in `docs/artifacts/mockup-tracker/`).
It is the master artifact (owner, 2026-10-30: "master artifact shall contain a reference to ALL artifacts"): game
mockups, guides (the Analog Panel Bible, https://claude.ai/artifact/Gzo38Dh7o1EqmfzfpJr8rF, and the Organic Panel
Bible, https://claude.ai/artifact/RNyM5ka91uporfETHjAQcD, `docs/design/organic-bible.html`) and misc.
If the owner changes accounts, every mockup, the tracker and the survey are republished from the new
account and every link here is updated.

| # | Mockup | Stage | OpenSpec change | Page | Link |
|---|---|---|---|---|---|
| 1 | Primary game flow | Drafted (2026-11-03, v7) | `game-flow` | `docs/mockups/game-flow.html` | https://claude.ai/artifact/UBbAouwAU3JvcbGf6MhzLt |
| 2 | In-game UI | Approved for implementation (2026-11-03; live v76: every skin in one console, following the ship you command) | `battle-console`, `battle-console-engine` | `docs/mockups/battle-console.html` | https://claude.ai/artifact/9yertq3bYQRmFiDE7N9eBs |
| 3 | Look dev | Listed | `look-dev` (to write) | `docs/mockups/look-dev.html` | |
| 4 | Console skins | Drafted (2026-10-31, v25) | `console-skins` | `docs/mockups/console-skins.html` | https://claude.ai/artifact/UTpgsPvZJrLfkEUmJN7CVb |
| 5 | Concord ships and damage | **Retired** (2026-11-03: superseded by mockup 6, owner: "Concord line (hull concepts) - new standard, get rid of the old concord ships"; off the tracker's mockup list, owner: "Concord ships and damage remove this from the main mockups mpage") | `concord-ship-look`, `ship-damage-visuals` | `docs/mockups/concord-ships.html` | https://claude.ai/artifact/J4baTFsXR3CR51zFvTnRj4 |
| 6 | Concord line (hull concepts): **the standard for Concord ships** (owner, 2026-11-03); its ships are in mockups 1 and 2 through `docs/mockups/lib/concord-line.js` | **Approved for implementation** (2026-11-04, owner in conversation: "I've approved ships to be built", "we need to integrate proper ships in the game"; change `ship-models-engine`). Drafted (2026-11-03, page version v13: the Tern family; the Albatross's pylons from its main hull, mockup 5's lighting and damage; v1 rejected) | `concord-hull-concepts` | `docs/mockups/concord-hull-concepts.html` | https://claude.ai/artifact/LzraYsJq2PQLG4ioVFpfBP |
| 7 | Krai Warhost ships | **Approved for implementation** (2026-11-04, owner in conversation, as mockup 6; change `ship-models-engine`). Drafted (2026-11-04, v21: the Krai line corvette to dreadnought, rooms first on the Ship Design Bible, at the Concord standard, with battle damage, the hull breaking apart like the Concord line, a bayonet per class, a painted livery and detailed nacelles) | `krai-ship-concepts` | `docs/mockups/krai-ship-concepts.html` | https://claude.ai/artifact/ScBxQeDJePA2LvAFbwLpwD |
| 8 | Battle console: Krai Warhost | **Approved for implementation** (2026-11-04, live v15; owner in conversation: "make scenarios for both sides (concord and Krai), and keep the UI updated witht he mockups"; change `krai-console-engine`). Drafted (2026-11-04, v14) | `krai-battle-console` | `docs/mockups/battle-console-krai.html` (written by `tools/mockups/skin_page.py`) | https://claude.ai/artifact/D6PacxkhuY6Q2n8TGHKDTv |
| 9 | Faction emblems: every faction's crest and insignia | Drafted (2026-11-03, page written; to publish from the desktop session) | `faction-emblems` | `docs/mockups/faction-emblems.html` | |
| 10 | Concord badge in 3D: the emblem shape B-E27 (the smooth keyhole) as a badge | Drafted (2026-11-04, v6) | `faction-emblems` | `docs/mockups/concord-emblem-3d.html` | https://claude.ai/artifact/CJSVJxXYLn7cdKF6TDtvDN |
| 11 | Concord emblem on the hull: B-E27 (the smooth keyhole) painted on the Petrel frigate | Drafted (2026-11-04, v9) | `faction-emblems` | `docs/mockups/concord-emblem-hull.html` | https://claude.ai/artifact/FLP6mnCeUCwn9c2rG2pxK9 |
| 12 | Ssarith Hunt ships | **Version 1 withdrawn** (2026-11-04, owner: "you are over stereotyping an alien species which is extremely insensitive"; it read the species' body into the ships). Waits on the owner setting the Hunt's culture (survey H1) | `ssarith-ship-concepts` | `docs/mockups/ssarith-ship-concepts.html` | https://claude.ai/artifact/UbsDVv6EvKbt3wkCDb6Ris |
| 15 | Battle console: Ssarith Hunt (and the console that follows the ship you command) | Drafted (2026-11-04, v4) | `ssarith-battle-console`, `fleet-skins` | `docs/mockups/battle-console-ssarith.html` (written by `tools/mockups/skin_page.py`) | [M15 v4](https://claude.ai/artifact/SJeqT1YEfXeH6edbEwzbEM) |

New mockups are added at the bottom with the owner's words, never renumbered.

---

## 1. Primary game flow

Owner, 2026-10-30: "let start with the primary game flow in three js".

- **Main menu**: Campaign, Skirmish, Options, Quit.
- **Skirmish**: preset scenarios (for now). The scenario gives you points to pick ships and configure
  their loadouts, then you play the game.
- **Campaign** (no story yet): a world map made of a hexagon grid, viewed at a perspective angle like
  Civilization V, with zoom. Ships sit on little standees, and so do planets and stations.
  - The world map has panels: **shipyard**; **ship configuration**, with an ammunition and parts shop
    to buy and sell; **recruiting center**. These open only at a friendly station or planet.
  - Otherwise, **missions**: patrol, enemy station assault, planetary assault, convoy ambush, convoy
    defense, assassination, capture a ship.
  - Click **Deploy** on a mission, play the game, and at the end return to the campaign map.

Later direction, the owner, 2026-10-30 (folded into the design and the page): "look at this star map, the
elements of each sector is inside the hex, with the hex borders communicating territory"; "ships, planets
and stations should be 3d"; "3d ships on stands, 3d stations on stands, 3d planets, asteroid belts, nebula"
(with photos of tabletop miniatures on flight stands); and the survey's F answers (prices by buyer, standing,
prizes repaired into the fleet). Then: "can we have stars not be flat but a 3d paralax background? also map rotates
when I hit edges, I dont like that, a cool nebula background would be nice too"; "Star crew bridge simulator! also
borrow its star shader from there too, so when we scroll around the stars trace an outline" (its nebula skies and
FreeSpace-style streaking stars, `data/space/`); "elements shouldnt hide when zooming out"; "I prefer routs be traced
like a line, 4x the map size" (30 x 22 sectors); "show tool tip of what faction space my mouse is hovering over". Then: "remove numbers, can you use some sort of ssr? combined with
reflection probe of space skybox? to make this look nicer? nebulas can be of different color, make the clouds more pronounced and 3d
looking, use the puffs method from crab-breeeze"; "tiles with planets or stations should be a different color"; "hex orientation all
need to be same orientation"; "what is standing in relation to? can we merge this with the top? so tis not floating? looks more like a
panel hanging from top of the screen"; "I should be able to click on ship to quicky go there".

Design: `openspec/changes/game-flow/`. References: SFC2 manual printed pages 74-89 (menus, skirmish,
campaign map, supply dock, shipyard); SFC3 manual printed pages 26-37 (skirmish points, refit by
hardpoint, resupply, officers, hex facilities, missions).

## 2. In-game UI

**Approved for implementation** (owner, 2026-11-03, in conversation: "based on the ui mockup start implmenting this ui
stuff in game, we have chosen Godot 4.7 with c# so begin integration now"). The game build is the change
`battle-console-engine`; its first slice (the console layout, the Concord kit, helm, power, the systems display, shields,
weapons, radar, the strips and the stand-in battle) is in `src/`, with stills in `docs/screenshots/game/`. The tracker's
stage for this mockup still needs updating from the desktop session (cloud sessions do not publish).

Version 67 (2026-11-03; owner: "Pause menu should have a background"): the menu sheet has a solid background inside its
frame, in the mockup and the game alike. Then (owner: "Re move shield view toggle"): the Shield view key is gone and the 3D
shield display is always shown; the Target key (show or hide the Target box) moves to the corner. Committed from the cloud
session; the desktop session publishes it.

Owner, 2026-10-30. Ship systems on the left.

- **Main panels**: helm, repairs, science, transport, tactical, comms, point defense, map, fleet, power,
  in-game menu and settings.
- **Ship overview panels**:
  - **Shields and weapons** (the primary display): the ship's schematic, top down, with its weapons and
    other ship systems. A very compressed version first; a larger view with more detail can be
    expanded later.
  - Shield array.
  - Sensor systems.
  - Transport and cargo (perhaps combined with the transporter system above).
  - Tractor beam.
  - Shuttles and fighters.
- **Ship 3D view**: the player's ship, a target camera, the weapons capacitor, a speed slider, energy
  and reactor output, the enemy view, the enemy targeting system (point and right click), and a 3D
  display of the ship's shield array.

Notes for its write-up: CLAUDE.md section 7 bans sliders for whole-number allocation; speed is a
continuous value, so the write-up argues the speed slider as an exception (the-federation allowed one
for tow distance) or proposes discrete boxes, for the owner to choose.

Owner, 2026-11-01, starting the UI thread with a screenshot of the reference game's battle screen: "we're gonna start a
thread to handle the ui design mockup of the game, the first design system we will implement will be the concord, we
will then reuse this design once it has been fully approved across all other factions, and each faction's implementation
will be manually verified such that all of it is functional and readable." And: "review this starfleet command ui
implementation and my initial prompt on how we would go about this, break down the elements, create a javascript three
js mockup, 3d gameplay can be placeholder of a ship moving through space for now, but the ui must be fully interactive.
I'll walk you through and iterate the fine details, but lets get something built so we can begin the iteration process."

Version 1 (2026-11-01): the reference screen broken down element by element (design section 1, with the SFC2 manual's
printed pages); the console in the Concord skin, drawn from the kit's pieces by the shared `docs/mockups/lib/concord.js`;
nine stations on a rail, the ship panel (systems display, shields, sensors, tractor, craft), weapons, the radar row and
tactical map, the strips, right-click targeting with a tap equivalent, the target camera and the 3D shield display, all
working against a seeded stand-in battle with a three-ship fleet and configurable wingmen; every panel expands with the
battle beside it, at desktop, phone landscape and portrait. The speed slider question is survey B1 (boxes built, a
Boxes or Slider switch in the menu to compare).

Owner's notes on version 1, 2026-11-01: "lets focus on iconography instead of text for the different tabs"; "the ship you
are controlling should always be at a certain Y height (in screen coords) despite the zoom level. It shouldnt be
centered, it should be below center" (with the reference screenshot); "text will show as you hover over above these
tabs, group icons into a narrow column, lets first only care about the desktop experience FIRST"; "DO NOT MOCKUP MOBILE
YET"; "speed needs a slider handle"; "power and systems need to sit on top of each other so we can see how much power
our systems are draining". Version 2: icon tabs in narrow columns with their names shown above on hover, the ship held
at 74% of the view's height, a handle on the speed boxes (B1 decided), Power over Systems on one scale, desktop only.

Then: "gotta get rid of the lines, from now on, hold off the publishing stuff, just take screenshots for me, I have to
manually approve artifacts publish apparently", and "see these slice lines?" (a close-up of seams in the tabs and keys).
Version 3 composes each kit element whole at its pixel size, so no seam shows; shown as screenshots, not published.

Then, on the power station: "power generated = blue, systems = green"; "need a config to allow generation rate to go
negative, I'd like to also show how much power is in batteries, which can drain if systems drain more than is
generated, but by default this config will be off, should be in the reactor tab"; "reactor tab will be found under
'power' which will have a button to swap between allocation and reactor. reactor will be a special new mode for our
game specifically. (it didnt exist in sfc)"; "reactor has two components: magnetic confinement, and coolant systems";
"the generator as it takes damage will lose alignment, so the player has to adjust alignment, and balance coolant
systems to keep the reactor from melting down. a meltdown can happen if coolant systems are offline for a duration, and
result in an explosion of the reactor". Version 4 adds the Reactor mode (OpenSpec change `reactor`), Power blue over
Systems green with a Battery bar, and the Draw past output setting; shown as screenshots. Then: "I want like an actual diagram of the
nuclear reactor and be able to like interact with it": the Reactor mode is now a diagram of the core, coils and coolant
loops, worked by dragging the coils home and pressing minus and plus on the pumps. Then: "please improve that diagram of a reactor", "the
loop a and loop b should look like loops i think", "it would make more sense": the diagram redrawn as a schematic (a
vessel with a field-coil cage and the plasma in its channel, a temperature dial, wall contact, closed coolant loops
running through the vessel with hot and cold legs, turning pumps, glowing radiators, the power bus). Then: "simpler please!": only the core,
the three coils, the two loops with their pumps and radiators, and the warnings.

Version 5 published at the owner's request ("publish it so I can play with this"). Then: "can you show this as discrete
marks/ticks?" (Power and Systems), "can I get red, yellow, green alerts?", and "change this box to 'target' that way we
dont need to toggle between the two on the ship systems display". Version 6: Power and Systems as ticks, three alert
lamps the captain sets, the Target box showing the target's systems display, the ship panel's toggle gone.

Then: "how would this work with various amounts of power?" (survey B7), "falling off the screen, please fix" (the
ticks), and "can you render this ui in engine as opposed to html? there are some issues with html rendering that causes
components to twitch and change sizes. (by engine I mean js engine)". Version 7 draws the whole console on a canvas in
the page's own loop: kit pieces composed at their pixel size, fixed text, grow-only number slots, immediate-mode input.

Then: "we need some iconography for each weapon type, on the side, show subsystems, as well. Tooltips: when I hover over
subsystem or weapon box: show what subsystem name is, health, status: Active/Disabled/Off/Damaged/Destroyed", with the
five statuses defined ("all of the status description is for your spec ... we are currently only concerned with just ui
- which will be driving gameplay spec in the future"). Version 8: weapon glyphs, subsystem columns, hover cards. Then:
the white arc over the facing toward the enemy, square weapon boxes with glyphs, "shield facing should be hexagons here"
(3D), "going from red 1 layer -> yellow 2 layers -> green 3 layers", "make sure weapon icons dont overlap!". Version 9. Then: "this should be a shield diagram that I can
pick what sides to reinforce" and "adjust how much energy I want to allocate - same energy slider as the one found in the
power menu". Version 10: the shield page is a hexagon diagram, click a side to reinforce, the power station's shield row. Then: "Color coding and numbers still help" (each facing's number coloured), and "to avoid clutter. The UI itself should display 1 layer. Where the game should display multiple": version 11, one band per facing on the console, layers only in 3D. Then: "make shields like bigger buttons like in starfleet command where they were like trapezoids, with the base pointing outwards, and the sections getting highlighted when selected for reinforcement": version 12. Then: "those shield numbers need to be inside the shield buttons", "or instead you can have a fill amount to show how much of that shield is charged", "and show numbers when you hover over it": version 13, keys fill with their charge, numbers in a hover card. Then: "Fill the icon boxes fully. Please. And communicate icon system status with different colors. Show that as the ship gets hit and takes damage": version 14, solid status-coloured boxes, a Take a hit key in the menu.

Versions 15-24 (2026-11-01): the lance glyph fixed, mounts on a grid, the selection ring flush (measured), cooldown
seconds in a darker shade, mines on a 3 s cooldown, the tractor contest (Hold and Break, power-up, the beam turning),
the helm by pointing (after the-federation), and test scenes for mines, the tractor and boarding. Version 25: the
transporter's three modes (Capture, Hit & run, Inventory) with crews by role (survey B14, B15), reeling (B16), stations
and planets, and four more scenes: Delivery, VIP, Shuttle, Fighters.

Versions 26-29 (2026-11-02): a scene selector in the view, the camera lock (smoothed), a round expand key, and the
warp drive (change `warp`: Power, Warp mode; nacelles, banks, plot, six risk factors, the safety override near a
neutron star, the jump; a Warp scene; survey J1-J5).

Version 33 (2026-11-02): the helm rebuilt from the manual's Helm Officer display (B4), a red target bracket with a
double frame on a seeker lock, fresnel shield keys, the weapons page as the ship with Fire keys and a detail half, ready
weapons outlined, ships four times tougher.

**Queued from the survey (2026-11-01), not built yet:** power ticks per ship scaled to
fit (B7); enemies starting tractor holds (B11); meltdown from a material temperature limit, a scram, coils aligned by
dragging (R1, R2, R4). The kit's cells and icons wait for the Concord master UI (B6).

Owner, 2026-11-04 ("[General for all UI]"): "this aim at should be icon based - not all weapons can precisely aim, so
recommend shift + click to see weapon hit stats in details"; torpedoes hit "based on arc receiving the hit ... a small
bracket would show above those subsystems to show they were on the chopping block", phasers and disruptors "had a better
chance of delivering the direct hit on stuff, but would damage other systems on rthe current arc as well (like a 50/50
spred)"; "an expand button to view viable targets to shoow with various weapons"; scenes to disable subsystems, to target
several ships at once with "Pin weapon fire", and a nebula for ECM and ECCM. Version 71 (both skins; change
`weapon-targeting`): the chopping block, precise and by-arc weapons, Aim at as icons, shift+click hit details, brackets on
the target's display, pinned fire, the expanded Tactical grid, nebulae, and scenes Disable, Pinned fire and Nebula.

Owner, 2026-11-04, on the recommendations: "Do 1, you narrowly escape a battle but damaged the reactor core and warp drive
among other things prioritize repairs and quickly jump out", "2 cool", "3. Yes, we need both sides to cloak. And scan with
eccm", "4. We need officer slots for stations. Then when you hover over officer. Slots it shows their rank and abilities",
"10.. boarding def sounds cool. Put a high security officer, transport units around the ship to setup ambushes. We need
to mock up units moving about the ship.in 3d space"; and "aim at should show status of subsystems", "I should be able to
click to aim at specific systems here too", "get rid of bloom button, it works great no need for toggle", "this test scene
menu is useless, I have them the drop down". Version 72 (both skins; changes `damage-control`, `weapon-arming`,
`cloaking`, `officers`, `boarding-defence`, `weapon-targeting`): the repair queue with crews at work and the Escape scene;
Proximity torpedoes and overloaded lances and the Arming scene; cloaks on both sides, the ECCM sweep and the Cloak scene;
an officer slot on every station with its hover card; the Security page with the deck plan in 3D, teams walking or beamed
to set ambushes, boarders, and the Repel boarders scene; Aim at keys lit in the target's subsystem status; target
subsystems that aim on a click; the Bloom key and the menu's scene tabs removed.

Owner, 2026-11-04: "6 and 5 are good ideas too though low.priority" (shuttle and fighter recall with scatter-packs, and
electronic warfare drones: accepted, not started), and "Drag and drop interface for repairs". Version 73 (both skins;
change `damage-control`): the repair queue reordered by dragging its rows, in place of the Up and Top keys.

Owner, 2026-11-04, of the Krai's Tactical grid: "this looks rough, can you better render a version of these butons? or
slice them better"; and "in this expanded view, I expect to be able to target individual weapons as well, weapons control
system dont affect PDCs or missiles actually so disabling that wouldnt disable independently powered weapons". Version 74
(both skins; changes `weapon-targeting`, `krai-battle-console`): wide keys rendered in Blender and nine-sliced; the
target's weapons as aim keys on the expanded Tactical page and on the Target box; point defence and missiles independently
powered.

Owner, 2026-11-04, of the Transport queues: "by default marines get transported, so show darkend marines in the queue if
nothing is queued" (Capture and Hit & run), and "capture - medics heal marines ... hackers speed up the process of
capturing, but are easy to get shot and die if there are less than 5 marines. pilots arent needed right away, but are
required to activate and fly the ship, if no pilot show a "pilot needed" after capturing the ship". Version 75 (both
skins; change `boarding-party`): the default load of marines, marine health and medics, hackers' speed and exposure,
Pilot needed, and the expanded Capture page's party panel and boarding log.

Owner, 2026-11-04: "lets next work on: Ssarith Hunt UI, this time combine uis together, so we can hop between different
ships", "use simple geometric ships for the Ssarith", "we'll be developing their ships in another thread", and of the
Hunt's rail tabs, "These buttons here don't quite fit". Version 76 (all skins; changes `ssarith-battle-console`,
`fleet-skins`): every skin in one page; the console wears the skin of the ship you command; the Allied fleet scene with a
Concord, a Krai and a Ssarith ship to hop between, and the quiet Three races scene to try them at leisure (keys 1, 2, 3
take each ship); every ship you can command has its own officers; the Ssarith Hunt's
battle console (mockup 15; numbered 12 until main took 12 for the Ssarith ships) with tube keys, wide keys and cells rendered in Blender, its 24 icons, and a Hunt stalker in
simple geometry; the Hunt's icon rails on square tube keys.

Owner, 2026-11-04: "We need a background for the lleft panel area. Black background is forbidden for metallic panel based
factions", "The panel icons have ||| on them but the icons don't fit because of that extra symbol. I want those cleaned
up and fit the incons in the button diaplay", "the labels here need work, they are just dark background rectangles with
words", "see how we resolved this with the Krai labels". Version 77 (Krai v16, Ssarith v2): the Hunt's tabs without the
tally, every label and icon inside the tube; its titles and officer slots on rendered gunmetal placards with a phosphor
pip; its column on a rendered plain gunmetal backing plate, and every metal skin's expanded panels on their own backing
(the Krai's blackened steel); the rule in CLAUDE.md section 7. Then "this text is hard to read" (a key's name on hover,
dark letters on the lit tube) and "remove underline" (the title placard's bone groove): version 78 (Krai v17, Ssarith
v4), the Hunt's hover names in phosphor on the dark tube, and its title placards without the groove. Then, of an
expanded panel: "background should wrap panel properly", "when expanded": version 79 (Krai v18, Ssarith v5), the backing
clipped to the panel's side of the screen (a mirrored copy had run into the live view) and its band clear of the tally.

## 3. Look dev

Owner, 2026-10-30: "this is the look dev area".

- **Detail level**: decide how detailed the ships are crafted (survey A1 says mid detail).
- **A design language for each faction**:
  - colour palettes, hull plating and hull patterns, insignia, hull details (vents, markings, greebles,
    windows), liveries;
  - hull shape, silhouette, and the ship elements that have gameplay impact.
- **Fully destructible hull pieces, and hull interiors** (see Crab Breeze), with a new skill written
  for this.
- **Effects**: engines and engine plumes, explosions, hits, warp.
- **Lighting**: navigation lights, hull lighting, registry lighting. Baked for efficiency; real-time
  where it does not cost too much (`light-baking`).
  - The owner's reference, 2026-10-30, an airliner lighting guide: "logo lights will be cool, but we'll
    need additional hull lighting too, as that highlights details on the ship in dark lighting
    conditions". The aircraft lights, as ship lights:

    | Aircraft light | On a ship | Look |
    |---|---|---|
    | Logo lights (on the tail fin) | Floodlights on the faction insignia and the registry | Steady pools on the markings |
    | Wing inspection lights | Floods from the hull washing the nacelles, pylons and wings | Steady, cool white |
    | Anti-collision strobes, white (wingtips, tail) | Strobes at the ship's extremities: nacelle tips, the stern | Short double flash |
    | Anti-collision beacons, red (top and bottom) | Red beacons on the dorsal and ventral hull | Slow pulse |
    | Position lights (red left, green right, white aft) | Port red, starboard green, white aft | Steady |
    | Landing lights | Forward docking lights | Bright beams, on when docking |
    | Taxi and turnoff lights | Lower side lamps for manoeuvring near stations | Dimmer, wider |
    | (the owner's addition) | **Hull floodlighting**: lamps set into the hull that graze its plating, so panel lines, vents and greebles read when the ship is in shadow | Steady, baked |


## 4. Console skins

Owner, 2026-10-30, with two screenshots of the reference game's battle screen (not committed: someone
else's art):

- "I want the ui to look really nice, like modeled and lit in blender then baked into a texture, also it
  should be deisnged for 9slicing for some elements, and others are like hero elements."
- "I really like espeically the mirak ui design, its so playful and pirate like."
- "I also enjoy the sleek, utilitarian, LCARS blue version of the federation, very sci-fi very minimal."
- "whatever we do, we gotta design it for like UI kit that we can sweap out between factions."
- "before I approve this start another artifact so we can design the design language of the ui, which changes based on
  what faction origin we are."
- On version 1, with screenshots of Starfleet Command's Federation and Mirak consoles, Starfleet Academy's science station
  and StarCraft's Terran console: "this ui language thing, it just different colors, not very interesting. 1. do research on
  LCARS design language [Wikipedia, the LCARS Manifesto and guideline at lcars-terminal.de, thelcars.com] 2. extract
  colorpallete from starfleet command and starfleet academy, build ONE design language based on that 3. build the other
  design language using 3d models in blender like starcraft 4. design a hand drawn aesthetic of the mirak ui". Rules: "NO
  skin uses the same button shape"; "every ui has a color palette inspired by that factions ideologies"; "ui elements and
  iconography is inspired by how that faction thinks. each ui skin uses different shapes, lighting, and shading on their UI
  and different icon shapes shaped by their world view, their body shapes, their technology, the shape of their ships".
  Version 2 builds Concord on LCARS's principles (palette sampled from the two games), Kethran as Blender renders, and
  the Vos hand-drawn (`openspec/changes/console-skins/design.md`).
- On version 2: "buttons should be more plain. BUT YES YOU ARE GETTINGIT". Version 3 makes the Kethran keys plain wedge caps.
- On version 3, with screenshots of StarCraft II's Zerg console, StarCraft's Protoss console and Starfleet Command's Klingon,
  Romulan and Gorn skins: "Some more UI design ideas: an organic alien race, lots of leather shining through light, slimy
  carapace, tendrils; an wise and advanced alien race. holographic buttons, with organic shaped metal armor peices
  surrounding the edges; a powerful ancient race. rusty, with back lit buttons clogged with dirt. their ships havent been
  upgraded in ages, rough around the edges; a combat focused race, lots of triangular motifs, angry red colors and orange
  lit buttons that are activated. strong metal framing, almost blade like; an enigmatic alien race with cool dark purple
  colors and pink buttons, progress bars are curcular, almost organic like eyes. this is all first pass stuff, really love
  how you did the hand drawn one". Version 4 adds five skins as first passes.
- Then: "one more, the old science, nucleo nlog skin from winamp. UI love this old osciloscope aesthetic from science labs. Some race
  will value thier anolog physics instruments and design their ui vbased on this". The Kelvane Institute is the sixth.
- On version 9: "svgs should be reserved for flat stuff like the cocnord. Voorr, conclave, Krai ,Ombrai and Kelvane could all use
  rendering in blender"; "Vhor needs special treatment with organic materials and shaders, and organic looking modeling, things
  likeslimey leather, tendrils, glaws, carapaces, fibers"; "Kelvane 100% can be modeled, let and rendered just take the osciloscope
  dash board for inspiration, old school electircal dashboards, dials, switches etc"; "the conclave is one exception. They have a
  hybrid of both vector graphics for holographic buttons and icons, but 3d rendered shell around the UI panels and elements".
  Version 10 renders the Vhorr in Blender; the others follow one at a time.
- With a screenshot of a Starfleet Command battle console: "A gurilla style faction, gun metal, beveled corner, industrial retro CRT
  style buttons", then "that faction is from the gorn, which are more hunter like faction, they enjoy the sport, so their ui is
  reminiscent of a dense jungle akin to the Predator". Version 13 adds the Ssarith Hunt, an original hunter race whose console is a
  jungle hunter's kit, modelled and rendered in Blender (name provisional, survey U10).
- 2026-10-31: "Using these design principles. Update conclave UI" (the analog panel bible and the `blender-ui-panels` skill). Version
  14 rebuilds the Aelith Conclave on them: pillowed satin-gold shells with formed wells and pearls, each well a void rim-lit in cyan,
  the holograms still vector, drawn into the wells from the same fit-checked shapes. The Concord top strip's bars now sit inside the
  strip, clear of its thin bar (the owner's example of a child overlapping its parent).
- 2026-10-31: "Ok conclave looks better not quite the rpotoss look I wanted but we'll revisit that in a bit..let's talk about the
  institute", then "They were inspired by the nucleo look. So.lets update that". Version 15 rebuilds the Kelvane Institute on the
  Nucleo NLog construction, our own design: pillowed champagne plates over amber backlit readout windows and green phosphor scopes,
  keys in pressed dimples, the helm's keys in a pressed cradle, meters in bays. The Conclave is to be revisited.
- 2026-10-31, of the Vhorr Brood's version 23: "Nah that's bad lol. Let's make a new design system for organic", which became
  the Organic Panel Bible; then "Looks dope, now let's see if this new design system can be applied to the design
  language/brood skins design system", "Go" (on the blockout), and "If you structured your sculpting tools well. Then we
  should be able to sculpt panels, buttons and elements of any size". Version 25 rebuilds the Vhorr Brood on the bible:
  every slot sculpted at its exact, pinned size by parametric sculptors (`tools/blender/organic_components.py`, run by
  `tools/blender/build_ui_vhorr_organic.py` over the blockout), no nine-slice, floating over the game, the portrait
  live under the hex filter (design.md, "The Vhorr Brood on the organic bible").
- 2026-11-01, survey section W (one picture per option): strips H (ridge and tentacles), ship systems display E (hanging
  tentacles), helm D, weapons D and the bottom row D (tentacle wrap), name plate B (plain heavy leather), keys "both"
  (larvae for the big keys, low warts for the small ones and the tabs); then "survey completed, this is a good start, I
  still dont want a plain black backround on the ui area though". The space between the panels is survey W8 (tissue,
  tendrils or lattice). Recorded in design.md, "The Brood's picks from the survey".
- 2026-11-01: "this ship display should span the whole width", then survey W8 tissue and "nice, survey andswered, make
  changes and publish that" (the approval of the Brood console under rule 0). Version 26 builds the Brood from the picks:
  the tissue backing under the column, darkened so the panels stand off it, and the ship systems display across the
  column's width.

What the mockup shows:

- **The kit**: every console piece, sorted into 9-slice pieces (frames, bars, buttons, tabs, shown
  stretched to several sizes to prove the slicing) and hero pieces (helm pad, systems display bezel,
  dials, alert lamp, crest).
- **The kit's piece list** first: every named piece, its kind (9-slice or hero), its slot size and its
  margins, shared by every skin. Mockups 1 and 2 are then redrawn from kit pieces so a skin can be
  swapped on them live.
- **Skins on one layout**: the same console (mockup 2's layout) in each faction's skin, switchable:
  Concord sleek, utilitarian and minimal in blues; Vos Syndicate playful, organic and pirate-like;
  Kethran to be designed. Our own designs, after the qualities the owner named.
- **How they are made**: modelled and lit in Blender (`panel-textures`, `blender-hard-surface`), baked
  to PBR maps, then to the flat lit images the console draws, with each 9-slice image's margins recorded.
- At desktop and phone sizes, panels expanded (CLAUDE.md section 7).

Order: it follows mockup 2, whose layout it dresses; a first skin can be shown on mockup 2's page.

## 5. Concord ships and damage

Owner, 2026-11-02, with five screenshots of fan-made starship models (not committed: someone else's art): "let's
develop the concord ships in 3 js artifact". Their notes, in their words:

- "take notes on how: windows communicate scale."
- "ships have liveries and markings that make it feel lived in"
- "branding is super important, from sillhoute to colors of features, lighting, which emphasizes registry numbers and
  ship features in low lighting conditions"
- "damage is very purposeful, ships break into pieces, damage texture has burnmarks towards the dges"
- "some ships have more markings than ohters, depends on how much they emphasize it"
- "ship textures vary from differnt metallic patterns to show ship construction processes, and what kinds of features
  they want to emphasize"
- "federation emphasizes nacelles like legs and disc like head or brain"; "romulan and klingon emphasizes bird like
  head with bird wings"; "gorn emphasizes claw like features, since they enjoy hunting"
- "make some mockups, make sure they are destryuctible, the more detailed the descrtuction the better, emphasize how we
  can communicate subsuustem destruction with ship destruction"
- The question to ask: "how can I understand ship subsystem damage by looking at it?", including coolant leaks, "ship
  systems on fire (venting fire)", scorch marks, "paralx shader that shows holes in ships, or actual holes in ships
  modeled?", and "chunks of ships being shot off - nacelles can be lost in the game, you only really need 1 nacelle to
  limp away" (survey A2-A9).

What the mockup shows: the Bastion cruiser (312 m), Lantern frigate (196 m) and Swift corvette (118 m) on one builder,
at true scale with a 1.8 m figure by the windows; livery and markings by class density; plating by class; floodlit
registry, grazing hull floods and navigation lights in three light presets; then the Damage tab: click the hull to
fire phasers or torpedoes, shields, a signature per subsystem in four states, scorch marks, holes modelled or parallax,
venting fire, coolant and plasma, a nacelle shot off (the ship limps), the bridge crown blown off, and the break-up
with charred edges. It is the first part of mockup 3 (look dev), for one faction.

- On version 1, 2026-11-02, of the break-up: "interiors are hollowed, this is not what I want"; "the ship breaks very
  programmatically, how do we make these chunks look more structurally random?"; "hull pieces look interesting, I think
  they should look more scorched, and have paralax so they look like actual interiors" (Spider-Man PS4's interior mapping).
  Then: "The walls seems slanted there. Also I think all the wall sections should be glowing slightly from the heat ... add a
  shader for like wires and sparks as they fly apart. I also expect pieces to fall off the ship before the main piece to fall
  off..also please give the rector some more health before exploding"; "no orange rooms"; "I like the orange burn highlights, I
  wanted something like that for the room wall dividers"; "these stripy pattern rooms, can they just be like other rooms?";
  "can walls also be scorched with this reddish orange flame? while the orange flames on the edges be more pronounced?";
  "im also not seeing any interior paralax in the holes"; "shots from the ship should have lighting, shots hitting the ship
  should also have lighting (explosions + whatever weapon was fired)".
  Then: "Paralax should be on by default for damage scars"; "Some missing geometry on this corner. There should be a big
  explosion when a chunk falls off". Version 3: parallax by default, cut faces that reach the corners, a big blast for
  every chunk.
  Version 2 breaks the saucer and engineering hull into random cells and closes every break with a scorched cut face
  that shows decks, bulkheads and interior-mapped rooms square to the break, burn rims on the dividers, flame-scorched room
  walls, wires that stretch, arc and snap, a 3.6 s breach in which pieces come off before the main blast, chunks knocked off
  by heavy hits, a tougher reactor, phasers that burn through, and light from every shot and blast, including the ship's own.

Design: `openspec/changes/concord-ship-look/` and `openspec/changes/ship-damage-visuals/`.

## 6. Concord hull concepts

Owner, 2026-11-03, on mockup 5's ships: "ship designs suck, I want somethign more original ,check out star crew and
adventure con repos for how ships are designed".

From Star Crew (`federation-ship-concepts`, `grabon-ship-concepts`, `ship-exteriors`): ships designed inside out, zones
first, a hull wrapped round them, then the two fitted; windows only on real rooms; three alternatives reviewed in three
stages with measured checks. From Adventure (CLAUDE.md 13.2 and 13.2b, `mockups/fleet/`): one shape language per faction
that reads at 40 px, the player's faction the one you can see through, classes counted by a repeated element.

Version 1 (three directions, Halo, Lantern spine, Light sail) was rejected the same day: "those concepts are all bad.
didnt you take a look at the otehr star crew repos?", "they dont fit my star crrew ships atall", "star crew bridge sim",
"star crew 64 and star raster are irrelevant". Then: "try some vertically aligned pulons too ... 1 pylon vertical, 2
pulons vertical".

What version 2 shows: the Concord line built in Star Crew Bridge Simulator's Tern family (octagonal hulls lofted
through stations, white armour over a grey subhull and service band, dorsal channels, copper-rimmed windows on decks,
long octagonal nacelles on swept pylons, radiators, turrets, engine bells, painted registry): the Swift corvette (51 m),
Petrel frigate (88 m) and Albatross cruiser (131 m), with the Tern itself from its own data as the yardstick. Three pylon
arrangements on every Concord class (level, one upright, two upright), four liveries (Concord, and the Tern's Copper,
Cyan, Rescue), measured facts and 40 px silhouettes. Survey A11 (layouts), A12 (sizes).

Version 4, on the owner's notes ("swift having 1 below would be better, petrel having 1 above and below", "albatross is a
bid long, I would prefer it to have like a secondary hull where pulons extend outr of", "instead of opptions. please just
apply the variants for both petrel and swift and include horizontal aligned ones aas well"): the Swift and the Petrel each
shown as three ships (one below, above and below, level), and the Albatross shortened to 112 m in two hulls, its nacelle
pylons sweeping out of a secondary hull. The line-up is a grid of all eight at true scale.

Version 5, on the owner's note ("remove petrel with 1 pylon, add detail textures, norrmal maps, fix up windows, need to bring
lighting standards up to what we had before, and damage systems"): the one-below Petrel is gone; the page is now mockup 5's
page with the Tern-family builder, so every ship has its light presets, floodlights, navigation lights and the whole damage
system (holes, venting, nacelles snapping off at the pylon, fracture with cut faces, wires, the staged breach); detail and
normal maps in the paint's plate grid; windows drawn as openings with a copper rim, reflecting glass and a room behind.

Then, the same day: furnished rooms behind the windows (carpet, sci-fi wall panels, furniture, a plant), phaser burn-throughs
that show a room, 90 degree spotlights grazing the hull instead of round pools, upright pylons 5.5 m out, turrets clear of the
command block, no z-fighting under the command block, the Tern from its latest model's geometry in our paint, a second
Albatross with pylons from the main hull, detailed thrusters, and lights that flicker a couple of times and then hold. Survey
A13 (which Albatross).

Then: hull lamps on the top and bottom face forward and side lamps aim down, a belly lamp pair forward and aft, and two new
classes (owner: "we need 2 additional ship classes: a battle ship and a carrier", "somewhat different than the standard ship
shape, carrier needs bays for fighters, battleship needs an array of cannons", "large cannons"): the Condor battleship and the
Pelican carrier. Survey A14.

Design: `openspec/changes/concord-hull-concepts/`.

**Approved for implementation** (owner, 2026-11-04, in conversation: "we need to integrate proper ships in the game. I've
approved ships to be built, and we have some test scenarios ready to go"). The ships reach the game as glb files exported
from this page's own builder (`tools/mockups/export_ships.mjs`, change `ship-models-engine`); the battle draws the Concord
classes as the battle console mockup maps them (CA Albatross, DD Petrel level, FF Swift level).

## 7. Krai Warhost ships

Owner, 2026-11-03: "Let's start mocking up krai ships". Built on the Krai console skin's language (the owner's words
there: "combat focused ... triangular motifs, angry red ... orange lit buttons ... blade like", "rough gun metal ... akin
to volcanic magma type metal") and survey A8's recommendation (a bird's head and wings). Version 1: three directions at
frigate size (A Stoop, B Crossed blades, C Spearhead), direction A as a corvette, frigate, cruiser and dreadnought, each
beside the Concord ship of its rank, and 40 px silhouettes. Names are placeholders (U7); survey A15-A17 ask the rest.
- On version 1: "Those are fighter jets, not spaceships. Think about how we defined culture and story". Version 2 builds
  spaceships from the culture as written (war as the only honest test, forged steel and edged everything, knife-blade
  hulls): A Standing blade, B Lance, C Three-edged dagger; nothing flies, the hull is the weapon.

**Approved for implementation** (owner, 2026-11-04, in conversation, with mockup 6). The four classes reach the game as
glb files exported from `buildKrai` (change `ship-models-engine`), with battle figures in `data/battle/classes.json`
(change `battle-scenarios`).

## 8. Battle console: Krai Warhost

Owner, 2026-11-01, of the battle console: "the first design system we will implement will be the concord, we will then
reuse this design once it has been fully approved across all other factions, and each faction's implementation will be
manually verified such that all of it is functional and readable". Then, 2026-11-03: "Krai Warhost - start work on
mocking up gameplay with this UI now", and "verify all functionality from concord works here".

- The battle console of mockup 2, played in the Krai Warhost skin of mockup 4: the same page source, every rule, panel
  and control unchanged, written for the Krai by `tools/mockups/skin_page.py`. The Krai kit rendered in Blender, its
  palette, type (Teko and Russo One), blade-cut drawn keys, the full icon subject list in its language, and the player's
  side named as the Warhost (Kharz, Vekk, Ruhn).
- Verified by `tools/mockups/skin_parity.mjs`: every control of every one of the 40 named shots pressed in both skins
  (2,175 presses of 270 controls), the console and the battle identical a second later, press for press.
- On version 1 (2026-11-03): "Labels on the krai UI need better integration. They should have a metallic background. The
  UI panels should be somewhat connected like they are part of the same console", then "Cool but you didn't render the
  stitching elements in blender like you are supposed to". Every label and panel title now sits on a metal placard, the
  column is one cast backplate with a spine of red light, steel clamps bolt each panel to the next and gussets brace the
  column to the strips, all rendered in Blender (`tools/blender/build_ui_krai.py`) from fit-checked shapes, with the
  allocation cells.
- Then: "Didn't be afraid to rerender stuff in blender. One of they key principals is to make the UI flow well. These more
  metallic interfaces should use some connected shapes across panels", "Some of the little clamps also obscure the text
  labels on the panels", "Some of the elements on the bottom bar like red alerts energy, battery, warp button don't fit in
  the container. Shrink.the elements or grow container height". Version 2: the clamps gone; the column re-rendered as
  one plate with the three panels formed into it as wells joined through necks and a channel of red light down its edge
  crossing every panel; the strips re-rendered 80 px tall with their metal running on into the column, every bottom-bar
  element inside the channel.
- Then, of version 2: "I dont like the gaps between panels, I think that gap should close", and "the buttons with icons
  need work ... Just changing the color to red or leaving it green doesnt work. I love the buttons you did for chaning the
  panels. repeating that design style is prefered". Version 3: the panels meet at a thin rib; every icon key and status box
  is a rendered square key in the rail tabs' style, its state the light in its glass.
- Then, of version 3's rib: "bring back the thicker divider and make this connected rather than seperated", and "back
  light more red please?". Version 4: one unbroken 20 px divider between panels with an enamel blade and the title on it;
  the forge backlight and the ember glass red, the whole Krai kit re-rendered.
- Then: "White shadows on buttons like bad. Same with green buttons we have as color palletes guide". Version 5: states in
  the Krai palette (orange whole, gold damaged, red down), glyphs orange on dark glass, no white outlines or figures. Then
  "Panel icons should be red. Just to differentiate it from functional icons": the panel tabs' icons red at rest. Then
  "White icons on buttons look bad I.mean": version 6, every label and glyph on a dark key forge orange, never white.
  Then "top bar is way thicker than it needs to be": version 7, the top strip re-rendered at 60 px. Then "lets do an
  experiment and try bloom on this led strip?": version 8, bloom on the column's channel of red light, with a Bloom key in
  the view to compare. Then, 2026-11-04, of the hover popups: "these text popups need to be a bit wider so they dont get
  thier letters cut off, add to guide": version 9, every popup sized by its key's own end caps, and rule T in the Analog
  Panel Bible.

Design: `openspec/changes/krai-battle-console/`.

**Approved for implementation** (owner, 2026-11-04, in conversation: "make scenarios for both sides (concord and Krai), and
keep the UI updated witht he mockups"). The game draws the console in the Krai kit whenever the player commands the Krai
Warhost (change `krai-console-engine`); the scenarios for both sides are change `battle-scenarios`. What still differs from
this mockup is in `docs/design/mockup-sync.md`.

## 9. Faction emblems

Owner, 2026-11-03: a session dedicated to designing the emblems (faction crests and insignia) for every faction in the
game. Numbered 9 because 7 (Krai Warhost ships) and 8 (the Krai battle console) are taken on other threads' branches.

- **Every faction's emblem**: Concord, Kethran Hegemony, Vos Syndicate, Vhorr Brood, Aelith Conclave, Tarsine Remnant,
  Krai Warhost, Ombrai, Kelvane Institute, Ssarith Hunt; each keeps the idea its console crest already had.
- **At every size it is seen**: the console crest (96 x 96 px), 40 px and 20 px, in colour and in one colour, beside the
  console's icons (the icon standard, `docs/design/icon-guide.html`).
- **On a ship's flank**: a stand-in hull section in 3D at true scale, the marking at 6 % of ship length (5.3 m on an
  88 m frigate, 20 px on a phone at battle distance).
- What each emblem means is the owner's (survey E1-E12); the recommendations are drawn and provisional.
- The Krai frigate's placeholder flank marking (mockup 7) is replaced by the Krai thread once the Krai emblem is
  approved; this mockup does not touch that page.

Later direction, the owner, 2026-11-03 (with the reference game's box art and its empire badges): "emblems are poor
quality, I expected better 3d renders from all of them. extract the details from these, and build a guideline for
them. start with the concord render first"; "these are quintessential to the cultural ideologies of the factions"; "get
material maker too"; on the first Concord render (a ring of five domes): "I'm not a fan of the circle design here", "its
too simple and easy to make, see our rules about just slapping on primitives", "this is bad design", "make a few
different designs", "make an emblem design guide to keep track of my notes"; "when you design something cultural, I
want you tell me why you did it that way, sketch a 2d figure out of svg first", "the shapes matter before we go 3d with
it", "make a single image with text pointing to design elements and choices"; with the STARFLEET Visual Standards Manual
2026: "add a link to references for this, and compile a design guide for emplems from this".

So each faction's emblem is now designed on the **Emblem Design Guide** (https://claude.ai/artifact/1ELwhue4vzFPjySnVc475V,
`docs/design/emblem-guide.html`): flat SVG sketches, each one image with its choices labelled, the owner's pick, then 3D
(Blender, swept and cut forms, surfaces from Material Maker graphs). The Concord's four sketches wait for a pick; 3D is
on hold until then. The flat drafts on this mockup are placeholders.

Design: `openspec/changes/faction-emblems/`. Shapes: `docs/mockups/lib/emblems.js`.

## 10. Concord badge in 3D

Owner, 2026-11-04, on the Emblem Design Guide's B-E23 (the rounded peak, drawn from the owner's own sketch): "ok I'm
kinda liking it, lets see how it looks in 3d, render something in blender, make a 3d version in thre js, add in
materials, normals, specular map, AO, and if you can make the background of the emblem, the bell shape some kind of
holographic stars and nebula shader", then "Central shape should be like irridescent".

- **One source of shapes**: the guide exports B-E23's parts (`tools/emblems/export_parts.mjs` writes
  `assets/emblems/concord/b-e23.parts.json`), and both the Blender render and the three.js badge build from them.
- **Blender**: `tools/blender/emblem_concord.py` on the shared harness `tools/blender/emblem_render.py`; renders face on,
  at an angle and as clay into `assets/emblems/concord/`.
- **three.js**: `docs/mockups/concord-emblem-3d.html`, orbitable, 100 mm across, with the Blender renders beside it.
- **Materials**: Material Maker graphs (`emblem_platinum` for the frame, `emblem_enamel_pearl` for the ship) with
  albedo, normal, ORM (AO, roughness, metal) and a specular map derived from each graph's own roughness and AO.
- **The field**: a holographic night of nebula and stars on three depths that shift with the viewing angle, under a
  colour-shifting sheen. **The ship**: iridescent pearl.
- **Since then** (owner, 2026-11-04): "Can you make the start like a satin in blue?", the arrow foot ("slightly arrow
  shaped at the bottom"), "Can we make the to point too?" (B-E25, now the badge), then "That look good i like that. We
  need some small subtle details to.the 3d model.loke a double bevel on the saucer. A small layer of detail. Maybe some
  segment marks on the legs": the saucer has an upper tier inset 1.6 mm, each leg four segments with grooves 0.5 mm
  deep, both from the guide's shapes (v2).

## 11. Concord emblem on the hull

Owner, 2026-11-04: "nice! can you show what this emblem looks on concord ship hull?", then "dont care about phone right now".

- **The ship**: the Concord line's Petrel frigate (88 m, level nacelles) from the shared builder
  `docs/mockups/lib/concord-line.js` (`plainModel`, plain pearl; mockup 6 keeps the windows, lamps and damage).
- **The paint**: B-E25 from the guide's parts, flat in the guide's two versions (navy on a pearl field; pearl on a night
  field), mixed into the hull's colour in its shader by a planar projection: no decal plane, no z-fighting.
- **Where** (owner, 2026-11-04: "too many logos on the hull there", "1/2 size on the left side", "only one logo"): one
  emblem, 2.6 m tall (3 % of 88 m, half of E7's 6 %), on the port side of the bow just ahead of the last windows, clear of
  the swept pylons, under its own logo floodlight. v1 had a dorsal crest and one on each flank at 5.3 m.
- **Paint** (owner, 2026-11-04: "on the ship hull I like the solid shape more"): the solid version, pearl on a night field,
  is the default; the navy outline is the other choice.
- **The emblem** (owner, 2026-11-04: "Beacon sucks. Keyhole but smoother", then "Yea ok. I'll accept that. Let's see it ont be
  ship hull. Solid color variant with a cyan colored star in the middle"): B-E27, the smooth keyhole, accepted as B's
  sketch and painted here in the solid version with a cyan star (`#2fd0f0`).
- **Livery and pinstripes** (owner, 2026-11-04: "Have it use the same color scheme as the liveries and integrate it into
  one of the stripes", then, with a reference picture of a hull whose emblem heads a pair of pinstripes, "See how this
  works? Although don't rotate our logo"): the emblem is in the livery's colours (field the stripe blue, frame the seam
  navy, read from `CL.LIVERIES`), upright on the port bow, and heads two pinstripes (broad stripe blue over thin seam navy)
  that run aft along the flank into the forward livery band. The reference picture is not committed; only the idea is taken.
- **The hull breaks the stripe** (owner, 2026-11-04: "The stripe having a circle around it where the logo crosses", "The
  hull should break the stripe livery at that spot"): the pinstripes carry on forward past the emblem, so it crosses them,
  and stop at a circle of bare plating round it, clear of its farthest corner by 0.35 m.
- **Now** (owner, 2026-11-04: "No outline, place emblem on liver stripe dp not add livery in this change"): the pinstripes
  are gone (no livery is added by this change); the emblem sits upright on the forward livery band, in the stripe's own
  blue with no outline, a pearl ship and the cyan star, and the band is cut away round it in a circle of bare hull.
- **Tightened** (owner, 2026-11-04: "tighten the livery line a bit so they arent so distant from the meblem", "bottom line
  can be flat to conform to the key hole/beacon shape more"): the cut is 0.15 m clear of the emblem where the band crosses
  it: an arc over the head and a flat edge under the foot.

## 12. Ssarith Hunt ships

Owner, 2026-11-04: "using current bibles and mockups on ship design, lets consider the Ssarith Hunt culture and how their
ships will look."

- Starts from the Hunt's culture as written in the console-skins design and approved as written (survey U7), the owner's
  "gorn emphasizes claw like features, since they enjoy hunting" (mockup 5's notes) and survey A8 (claws).
- On the Ship Design Bible, as the Krai line (mockup 7) was redone: one frigate's rooms first
  (`data/ships/ssarith/frigate_plan.json`, from `tools/mockups/ssarith_plans.py` on the Krai plan layer), the hull fitted
  round them, then three directions of claws on that one hull: A Grasp (recommended), B Jaw, C Crouch; Stalk and Strike.
- Every cultural reading is provisional until survey section H (H1-H8) is answered.
- **Withdrawn** (owner, 2026-11-04): "part of me loves that these are space aligators, but at last, you are over
  stereotyping an alien species which is extremely insensitive. We've stated multiple times before, the look of an alien
  species is not their UI, not their spaceship. Cultural ideologies are far more deep and complex than what someone's
  skin or features are. And that no culture in the world follows a single line of thinking when it comes to desigining
  UI, space ships, logos, dressing etc, and none of those reflect the way they look". Then: "make sure these notes are
  added to the cultural guides of designing spaceships": the Ship Design Bible's chapter 13, Culture, not anatomy (C1-C5),
  and CLAUDE.md section 1. Next: the Hunt's culture, set by the owner (survey H1), before any ship is redrawn.

Design: `openspec/changes/ssarith-ship-concepts/`.
