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
Bible, `docs/design/organic-bible.html`, to be published) and misc.
If the owner changes accounts, every mockup, the tracker and the survey are republished from the new
account and every link here is updated.

| # | Mockup | Stage | OpenSpec change | Page | Link |
|---|---|---|---|---|---|
| 1 | Primary game flow | Drafted (2026-10-30, v5) | `game-flow` | `docs/mockups/game-flow.html` | https://claude.ai/artifact/UBbAouwAU3JvcbGf6MhzLt |
| 2 | In-game UI | Listed | `battle-console` (to write) | `docs/mockups/battle-console.html` | |
| 3 | Look dev | Listed | `look-dev` (to write) | `docs/mockups/look-dev.html` | |
| 4 | Console skins | Drafted (2026-10-31, v23) | `console-skins` | `docs/mockups/console-skins.html` | https://claude.ai/artifact/UTpgsPvZJrLfkEUmJN7CVb |

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
