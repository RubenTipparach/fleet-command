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
| 2 | In-game UI | Approved for implementation (2026-11-03; live v67, v69 in the repo) | `battle-console`, `battle-console-engine` | `docs/mockups/battle-console.html` | https://claude.ai/artifact/9yertq3bYQRmFiDE7N9eBs |
| 3 | Look dev | Listed | `look-dev` (to write) | `docs/mockups/look-dev.html` | |
| 4 | Console skins | Drafted (2026-10-31, v25) | `console-skins` | `docs/mockups/console-skins.html` | https://claude.ai/artifact/UTpgsPvZJrLfkEUmJN7CVb |
| 5 | Concord ships and damage | **Retired** (2026-11-03: superseded by mockup 6, owner: "Concord line (hull concepts) - new standard, get rid of the old concord ships"; off the tracker's mockup list, owner: "Concord ships and damage remove this from the main mockups mpage") | `concord-ship-look`, `ship-damage-visuals` | `docs/mockups/concord-ships.html` | https://claude.ai/artifact/J4baTFsXR3CR51zFvTnRj4 |
| 6 | Concord line (hull concepts): **the standard for Concord ships** (owner, 2026-11-03); its ships are in mockups 1 and 2 through `docs/mockups/lib/concord-line.js` | Drafted (2026-11-03, page version v13: the Tern family; the Albatross's pylons from its main hull, mockup 5's lighting and damage; v1 rejected) | `concord-hull-concepts` | `docs/mockups/concord-hull-concepts.html` | https://claude.ai/artifact/LzraYsJq2PQLG4ioVFpfBP |
| 7 | Krai Warhost ships | Drafted (2026-11-03, v10: the frigate rooms first on the Ship Design Bible, brought to the Concord standard: square-cut plates, windows with Krai rooms, thrusters, warm floods) | `krai-ship-concepts` | `docs/mockups/krai-ship-concepts.html` | https://claude.ai/artifact/ScBxQeDJePA2LvAFbwLpwD |
| 8 | Battle console: Krai Warhost | Drafted (2026-11-03, v8) | `krai-battle-console` | `docs/mockups/battle-console-krai.html` (written by `tools/mockups/skin_page.py`) | https://claude.ai/artifact/D6PacxkhuY6Q2n8TGHKDTv |

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

## 7. Krai Warhost ships

Owner, 2026-11-03: "Let's start mocking up krai ships". Built on the Krai console skin's language (the owner's words
there: "combat focused ... triangular motifs, angry red ... orange lit buttons ... blade like", "rough gun metal ... akin
to volcanic magma type metal") and survey A8's recommendation (a bird's head and wings). Version 1: three directions at
frigate size (A Stoop, B Crossed blades, C Spearhead), direction A as a corvette, frigate, cruiser and dreadnought, each
beside the Concord ship of its rank, and 40 px silhouettes. Names are placeholders (U7); survey A15-A17 ask the rest.
- On version 1: "Those are fighter jets, not spaceships. Think about how we defined culture and story". Version 2 builds
  spaceships from the culture as written (war as the only honest test, forged steel and edged everything, knife-blade
  hulls): A Standing blade, B Lance, C Three-edged dagger; nothing flies, the hull is the weapon.

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
  the view to compare.

Design: `openspec/changes/krai-battle-console/`.
