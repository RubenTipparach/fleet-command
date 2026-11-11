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
| 1 | Primary game flow (and, since v8, the faction screen and campaign missions) | v31 drafts (2026-11-09): **commander rank** (change `commander-rank`, the top bar's Rank and the `map_rank` shot; awaiting approval, survey CR1-CR7), the standing as Friends and Enemies emblems with the Standings key, and the Unfinished factions option (both small changes to approved UI, in the game too). **Approved for implementation** (2026-11-05, owner in conversation: "implement game flow into the game, but I want you to implmenent missions as well"; changes `game-flow-engine`, `mission-scripting`, `ship-schematics`). Drafted (2026-11-08, v30: the lamp dimmer and the room darker, more dramatic; the paper chart approved for implementation (owner, 2026-11-08: "implement space chart in game, it looks great!") and in the game; v29: twice the cross-hatching, no printed nebulae or belts, planets on hexagon stands; v28: the planets half size and whole, smaller clouds with hatched shadows, and merged with the desktop's v20 (committed, not published): the Kelvane Institute on the faction screen and in the campaign, four badges on the table, the Hunt's campaign ships named as the game's; v27: planets drawn live by our planet shader (after a CC0 Godot shader), heavier cross-hatching; v26: hand-drawn clouds, storms, planets, suns and rocks, ink names, a compass rose; v25: on a phone the sector panel hides and the top bar is two rows; v24: the chart works on phones (WebGL 1 nebula, a smaller sheet, a blur fallback); v23: opens the map at once on a page that asks for it (the Space Chart, mockup 34); v22: the chart a printed star map with a nebula, green walls with walnut trim, darker, the fog of war on the chart with a soft border; v21: the campaign map as a printed paper chart on a walnut table in a carpeted, papered room; v20: the Hunt's schematics; v19: the faction screen fits phones in portrait; v18: the faction screen on a walnut table, free fitting from stores, crew lists by station, the course kept and later turns dotted, Display in Options; v17: ships slide between sectors; v16: the port redesigned; v15: Accept sets course) | `game-flow`, `faction-select`, `campaign-missions`, `paper-map` | `docs/mockups/game-flow.html` | https://claude.ai/artifact/UBbAouwAU3JvcbGf6MhzLt |
| 2 | In-game UI | v85 draft (2026-11-09): **damage and reactor warnings** (change `damage-warnings`: smoke, fire, sparks and shedding scraps by hull, the flashing Low hull integrity and Reactor unstable bands; shots `low_hull`, `reactor_unstable`, `damage_warnings`; every faction page; awaiting approval, survey DW1-DW6). Approved for implementation (2026-11-03; live v79: every skin in one console, following the ship you command; v80 committed, to publish: beams and disruptor bolts keep their ends on their ships, and lances, bolts and torpedoes drawn by the weapons range's effects) | `battle-console`, `battle-console-engine` | `docs/mockups/battle-console.html` | https://claude.ai/artifact/9yertq3bYQRmFiDE7N9eBs |
| 3 | Look dev | Listed | `look-dev` (to write) | `docs/mockups/look-dev.html` | |
| 4 | Console skins (the UI kit, every faction's skin) | Drafted (2026-11-08, v33 published: the Tarsine kit re-rendered as worked iron; v30: the Exponent redrawn as its own collective taken over by a reef, the Red Tithe on oak; v29: three new skins on the owner's ask, "Id like a faction that is purely mathematical. A faction based on fractal patterns and designs", "Then that faction will merge with the organics creating a new hybrid of fractal organic faction", "Id also like another pirate faction. One more brutal": the Axiom, the Exponent (named by the owner, 2026-11-07: "The Exponent"; then its own collective, not a hybrid: "Inspired by Borg. It's ship ui is a metalic utilitarian look. With veins a and tentacles and barnacles growing. Like am abandoned rusted ship at sea. Take over by reefs and coral") and the Red Tithe ("More brown wood on the red tithe"), the other names and the cultures provisional, survey N1-N6; v28, 2026-11-06) | `console-skins` | `docs/mockups/console-skins.html` | https://claude.ai/artifact/UTpgsPvZJrLfkEUmJN7CVb |
| 5 | Concord ships and damage | **Retired** (2026-11-03: superseded by mockup 6, owner: "Concord line (hull concepts) - new standard, get rid of the old concord ships"; off the tracker's mockup list, owner: "Concord ships and damage remove this from the main mockups mpage") | `concord-ship-look`, `ship-damage-visuals` | `docs/mockups/concord-ships.html` | https://claude.ai/artifact/J4baTFsXR3CR51zFvTnRj4 |
| 6 | Concord line (hull concepts): **the standard for Concord ships** (owner, 2026-11-03); its ships are in mockups 1 and 2 through `docs/mockups/lib/concord-line.js` | **Approved for implementation** (2026-11-04, owner in conversation: "I've approved ships to be built", "we need to integrate proper ships in the game"; change `ship-models-engine`). Drafted (2026-11-03, page version v13: the Tern family; the Albatross's pylons from its main hull, mockup 5's lighting and damage; v1 rejected) | `concord-hull-concepts` | `docs/mockups/concord-hull-concepts.html` | https://claude.ai/artifact/LzraYsJq2PQLG4ioVFpfBP |
| 7 | Krai Warhost ships | **Approved for implementation** (2026-11-04, owner in conversation, as mockup 6; change `ship-models-engine`). Drafted (2026-11-04, v21: the Krai line corvette to dreadnought, rooms first on the Ship Design Bible, at the Concord standard, with battle damage, the hull breaking apart like the Concord line, a bayonet per class, a painted livery and detailed nacelles) | `krai-ship-concepts` | `docs/mockups/krai-ship-concepts.html` | https://claude.ai/artifact/ScBxQeDJePA2LvAFbwLpwD |
| 8 | Battle console: Krai Warhost | **Approved for implementation** (2026-11-04, live v15; owner in conversation: "make scenarios for both sides (concord and Krai), and keep the UI updated witht he mockups"; change `krai-console-engine`). Drafted (2026-11-04, v14) | `krai-battle-console` | `docs/mockups/battle-console-krai.html` (written by `tools/mockups/skin_page.py`) | https://claude.ai/artifact/D6PacxkhuY6Q2n8TGHKDTv |
| 9 | Faction emblems: every faction's crest and insignia | Drafted (2026-11-03, page written; to publish from the desktop session) | `faction-emblems` | `docs/mockups/faction-emblems.html` | |
| 10 | Concord badge in 3D: the emblem shape B-E27 (the smooth keyhole) as a badge | Drafted (2026-11-05, v8) | `faction-emblems` | `docs/mockups/concord-emblem-3d.html` | https://claude.ai/artifact/CJSVJxXYLn7cdKF6TDtvDN |
| 11 | Concord emblem on the hull: B-E27 (the smooth keyhole) painted on the Petrel frigate | **Retired** (2026-11-04: the emblem lives on the real model, mockup 6; owner: "Concord emblem on the hull - get rid of this"; off the tracker's mockup list, kept under Misc) | `faction-emblems` | `docs/mockups/concord-emblem-hull.html` | https://claude.ai/artifact/FLP6mnCeUCwn9c2rG2pxK9 |
| 12 | Ssarith Hunt ships | **Approved for implementation** (2026-11-06, owner in conversation: "looks good, implement in game"; approved for build by the same words). Drafted (2026-11-06, v26: no dots at the floods, only the nav lights show as lamps; v25: half as many floods, twice the pool, one column top and bottom; v24: wide floods, the cruiser and carrier at 3072 px, in the game; v23: the cruiser's shell below, the carrier's in the middle with bays above and below, windows without glow; v22: one ship at a time, frigates, a new cruiser and the carrier; v21: breakup fixed, every cell has its skin; v20: hulls break apart; v19: bay lamps on the lintels; v18: all five rebaked with every pane fitted; v17: rooms and hangars level, every pane fitted to its face; v16: clean-framed windows and bays, each its own quad; v15: the four picks and a new carrier, each with modelled hull detail, a PBR set and baked hull lights; rooms behind the windows and hangars behind the bays, forward floods, a greener emission, battle damage on the shared ship-fx.js) | `ssarith-ship-concepts` | `docs/mockups/ssarith-ship-concepts.html` | https://claude.ai/artifact/UbsDVv6EvKbt3wkCDb6Ris |
| 14 | Krai badge in 3D: the emblem KC+ (the fang torc) as a badge | Drafted (2026-11-05, v2) | `faction-emblems` | `docs/mockups/krai-emblem-3d.html` | https://claude.ai/artifact/PTvvbWnntKFDsiWEyuXD48 |
| 15 | Battle console: Ssarith Hunt (and the console that follows the ship you command) | **Approved for implementation** (2026-11-06, owner in conversation: "Yes, approve both for the game"; change `fleet-skins`). Drafted (2026-11-06, v9; v10 committed, to publish) | `ssarith-battle-console`, `fleet-skins` | `docs/mockups/battle-console-ssarith.html` (written by `tools/mockups/skin_page.py`) | [M15 v6](https://claude.ai/artifact/SJeqT1YEfXeH6edbEwzbEM) |
| 16 | Special weapons: two for each finished faction (Concord, Krai), with high-end effects and shaders, and the standard weapons | **Approved for implementation** (2026-11-05, owner in conversation: "Implement weapons in game"; changes `weapon-fx`, `missiles`, `special-weapons`). Drafted (2026-11-05, v4; published from the cloud session's account, to republish from the desktop session onto the tracker) | `special-weapons`, `weapon-fx` | `docs/mockups/special-weapons.html` | https://claude.ai/artifact/JSddbdvMccwMzzcjLi2eYT |
| 17 | Battle sounds: the first sound effects, heard on the Fleet Command Sounds page | Drafted (2026-11-11, v11 after the owner's notes; the console keys and the bridge bed **approved for implementation** and in the game, owner in conversation: "Console keys - this should be implemented in the game"; published from the cloud session's account, audio served beside the page) | `sound-effects` | `docs/mockups/sounds.html` (written by `tools/audio/gen_sfx_page.py`) | https://claude.ai/artifact/P2PEz2NG6Yv8NnKXT2SrNW |
| 18 | Music: the first score, heard on the Fleet Command Music page | Drafted (2026-11-07, v4 (v3 plus fingerprinted links): a battle and an ambient loop for every faction in the game; published from the cloud session's account, audio served beside the page) | `music` | `docs/mockups/music.html` (written by `tools/music/music_page.py`) | https://claude.ai/artifact/NRSwJgnSQkt3ZiKZtG9Lov |
| 19 | Ssarith badge in 3D: the emblem SD (the Bow Rites) as a badge | Drafted (2026-11-05, v5) | `faction-emblems` | `docs/mockups/ssarith-emblem-3d.html` | https://claude.ai/artifact/NLspB15dVMS7dhnbM8PvCB |
| 20 | Battle console: Kelvane Institute | **Approved for implementation** (2026-11-06, owner in conversation: "I meant institute. Build institute into the game", "Yes, approve both for the game"; change `fleet-skins`). Drafted (2026-11-06, v3; v4 committed, to publish) | `kelvane-battle-console` | `docs/mockups/battle-console-kelvane.html` (written by `tools/mockups/skin_page.py`) || https://claude.ai/artifact/FNK2Q5RKWRVN7t4yET16ty |
| 21 | Kelvane badge in 3D: the emblem KID (the hydrogen eigen plot) on an oval coin, with its 2D logo (the hull logo) | Drafted (2026-11-06, v2) | `faction-emblems`, `kelvane-battle-console` | `docs/mockups/kelvane-emblem-3d.html` | https://claude.ai/artifact/JWkaEkgW1aTvxU8s5p9sN3 |
| 22 | Space fly-through: ray-marched nebula clouds and stars | **Paused** (2026-11-06, owner: "just stars is fine for now"); v2 published, its clouds broken in the owner's browser, the fix committed but not republished | `space-flythrough` | `docs/mockups/space-flythrough.html` | https://claude.ai/artifact/9JWXLSiYcoMFwyasYX6fLg |
| 23 | Kelvane Institute ships: the line, five research-vessel classes from the owner's picks (and the blockout board it came from) | **Approved for implementation** (2026-11-07, owner in conversation: "perfect, implment these ships in game"; approved for build by the same words; in the game by change `kelvane-ships-engine`). Drafted (2026-11-07, v10: 215 the Gallery fighter, detailed, the carrier's craft; v9: six variants of the Gallery bus; v8: every plate corner diagonal, shuttles overhead in the carrier, eight shuttle designs; v7: engineered machinery in rows, the carrier's pads and craft, diagonal corners, warm rooms; v6: machinery inside the openings, instrument arrays on the apertures and the nose, windows; v5: every class baked with its own detail and textures; v4: the line, frigate to carrier; v3: the loop family; v1: the board) | `kelvane-ship-concepts` | `docs/mockups/kelvane-ship-concepts.html` | https://claude.ai/artifact/LfkkemDKPhan6Y7ZSBfgmx |
| 24 | Freighters: a freighter for every approved faction (picks 1, 4, 5 and 8) | **Approved for implementation** (2026-11-07, owner in conversation: "implement in game, also implment on campaign map, and implement freighter raid/escort missions"); v16 is in the game (battle classes, the campaign map, escort and raid encounters; `freighter-missions`, `field-repair`). Approved for build (2026-11-07, owner in conversation: "Adjust institute ship len to fit cargo better .After that change. Now I approve. Detail them"). Drafted (2026-11-07, v14: the four picks detailed at 2048 px; the container baked from CC0 textures) | `freighters` | `docs/mockups/freighter-concepts.html` | https://claude.ai/artifact/Gkv9jzwEZd258noT7WGtv4 |
| 25 | Battle console: Axiom | Drafted (2026-11-08, v1). Listed (2026-11-07) (owner, 2026-11-07: "Let's make a big push and get all these unbuilt uis built in a mockup"): the battle console in the Axiom skin from mockup 4 | `axiom-battle-console` | `docs/mockups/battle-console-axiom.html` (written by `tools/mockups/skin_page.py`) | https://claude.ai/artifact/G6CFvuJLxSGsFu5y9i9fzo |
| 26 | Battle console: Exponent | Drafted (2026-11-08, v1). Listed (2026-11-07) (owner, 2026-11-07: "Let's make a big push and get all these unbuilt uis built in a mockup"): the battle console in the Exponent skin from mockup 4 | `exponent-battle-console` | `docs/mockups/battle-console-exponent.html` (written by `tools/mockups/skin_page.py`) | https://claude.ai/artifact/XWGn3TZTPcTDFbtNTf8mV2 |
| 27 | Battle console: Red Tithe | Drafted (2026-11-08, v1). Listed (2026-11-07) (owner, 2026-11-07: "Let's make a big push and get all these unbuilt uis built in a mockup"): the battle console in the Red Tithe skin from mockup 4 | `redtithe-battle-console` | `docs/mockups/battle-console-redtithe.html` (written by `tools/mockups/skin_page.py`) | https://claude.ai/artifact/WiQEWpfRaEN2HQvUXLxqga |
| 28 | Battle console: Vos Syndicate | Drafted (2026-11-08, v1). Listed (2026-11-07) (owner, 2026-11-07: "Let's make a big push and get all these unbuilt uis built in a mockup"): the battle console in the Vos Syndicate skin from mockup 4 | `vos-battle-console` | `docs/mockups/battle-console-vos.html` (written by `tools/mockups/skin_page.py`) | https://claude.ai/artifact/RAdVg9hJhGJwgukAybj5wt |
| 29 | Battle console: Kethran Hegemony | Drafted (2026-11-08, v1). Listed (2026-11-07) (owner, 2026-11-07: "Let's make a big push and get all these unbuilt uis built in a mockup"): the battle console in the Kethran Hegemony skin from mockup 4 | `kethran-battle-console` | `docs/mockups/battle-console-kethran.html` (written by `tools/mockups/skin_page.py`) | https://claude.ai/artifact/DmDc8xBHJYcRfAkdHgYy6w |
| 30 | Battle console: Tarsine Remnant | Drafted (2026-11-08, v2: panel faces rendered as worked iron at their drawn size, the column and backing rendered, on the owner's "tarsine background is terrible, it should be like a metal texture"). Listed (2026-11-07) (owner, 2026-11-07: "Let's make a big push and get all these unbuilt uis built in a mockup"): the battle console in the Tarsine Remnant skin from mockup 4 | `tarsine-battle-console` | `docs/mockups/battle-console-tarsine.html` (written by `tools/mockups/skin_page.py`) | https://claude.ai/artifact/RwHfXVD7XtsM1EjSi8tHXX |
| 31 | Battle console: Aelith Conclave | Drafted (2026-11-08, v1). Listed (2026-11-07) (owner, 2026-11-07: "Let's make a big push and get all these unbuilt uis built in a mockup"): the battle console in the Aelith Conclave skin from mockup 4 | `aelith-battle-console` | `docs/mockups/battle-console-aelith.html` (written by `tools/mockups/skin_page.py`) | https://claude.ai/artifact/4tCTboCgm9dkeE52WcE711 |
| 32 | Battle console: Ombrai | Drafted (2026-11-08, v1). Listed (2026-11-07) (owner, 2026-11-07: "Let's make a big push and get all these unbuilt uis built in a mockup"): the battle console in the Ombrai skin from mockup 4 | `ombrai-battle-console` | `docs/mockups/battle-console-ombrai.html` (written by `tools/mockups/skin_page.py`) | https://claude.ai/artifact/7SiSxjuoQU7uhQicQSndaP |
| 33 | Battle console: Vhorr Brood | Drafted (2026-11-08, v1). Listed (2026-11-07) (owner, 2026-11-07: "Let's make a big push and get all these unbuilt uis built in a mockup"): the battle console in the Vhorr Brood skin from mockup 4 | `vhorr-battle-console` | `docs/mockups/battle-console-vhorr.html` (written by `tools/mockups/skin_page.py`) | https://claude.ai/artifact/MaZY7tPtShdswH2RmZqKYv |
| 34 | Space chart: the campaign's paper star chart as its own page, opening straight onto the map (owner, 2026-11-07: "Our space chart mockup is not available to me as an artifact, please make it") | **Approved for implementation** (2026-11-08, owner in conversation: "implement space chart in game, it looks great!"; in the game, `game-flow-engine` 2.10). Drafted (2026-11-08, v8: the lamp dimmer and the room darker, more dramatic, and the drawn clouds from the bank the game uses; the chart in the game; v7: twice the cross-hatching, no printed nebulae or belts, planets on hexagon stands; v6: renumbered 34, planets half size and whole, smaller clouds with hatched shadows; v5: shader planets, heavier cross-hatching; v4: hand-drawn clouds, storms, planets, suns and rocks, a compass rose; v3: the sector panel hides on a phone, a two-row top bar; v2: works on phones; v1: written from mockup 1 by `tools/mockups/chart_page.py`, so it runs mockup 1's code) | `paper-map` | `docs/mockups/space-chart.html` | https://claude.ai/artifact/A4E9dHbWA5qdvi1Z77sAjX |
| 35 | Hull texture designs: the Concord and Krai frigates, five culture-led texture designs each (owner, 2026-11-06: "Pitch a few different texture designs. Start with frigate of each and propose4-5 different texturing approaches for each. Goal is to follow culture and ideology"; "Concord could also use a bit of detail on their ships too to avoid them looking to much like primitives") | Drafted (2026-11-08, v22: the Condor's windows to three decks; v21: windows on every Concord class, Krai nacelle studs gone; v20: renumbered 35; v19: neutral hangar light; v18: softer warm-white window light; v17: warm yellow windows; v16: the Concord's original light arrangement back; v15: the whole of both lines, the Krai dreadnought added; v14: the Condor, Pelican and Krai cruiser added; v13: the Swift, Albatross and Krai corvette added; v12: designed bow pieces, K1 plan windows, the rest of both lines baking; v11: K1 ends plated, detail tiles by hull area, roughness in the painted look; v10: C1 bow and stern plated, soft light, the detail layer; v9: C1 half the panels, light grey seams, varied plates, plan windows; v8: C1 in rows too, windows and fans fixed, lighter seams; v7: K1 in staggered rows of plates; v6: K1 on it too; v5: C1 on the Kelvane Institute's hull method, the owner's "lets try a new dirtection"; v4: ten designs baked as the Hunt's); **not for the game until the owner enthusiastically approves** | `hull-texture-designs` | `docs/mockups/hull-texture-designs.html` | https://claude.ai/artifact/GyVMZ9s6hxwbDhmeBC9Fqu |

| 36 | Placeholder fleets | **Approved for implementation** (2026-11-08, owner in conversation: "cool start implementation"; the 36 hulls exported to the game, their classes and campaign hulls in the data, the emblems on the faction screen). Drafted (2026-11-08, v1) (owner, 2026-11-08: "generate place holders for all of their ships, class design is your discretion, and a place holder logo for the factions too"): four placeholder classes and a placeholder emblem for each of the nine factions with no ships | `placeholder-fleets` | `docs/mockups/placeholder-fleets.html` | https://claude.ai/artifact/2oi3YHbpmssfDjwZ2WpzPT |
| 37 | Fractal vessels | Drafted (2026-11-10, v2: phone friendly and marched at half resolution; v1) (owner, 2026-11-10: "a faction is designed to be a fractal type of life form. and interdimensional being", "can we build raymarched fractal ships?", "do the mockup yo", "dont worry about phone"): three raymarched vessels with no mesh, one per school of the Axiom's draft culture, beside the Petrel at true scale; desktop only | `fractal-vessels` | `docs/mockups/fractal-vessels.html` | https://claude.ai/artifact/2mUTtEaxfqSAnX94REdZHj |
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

Then, 2026-11-05 (v8): "Can we update campaign UI with new skins? Add test maps to scenarios. And create a faction selection
screen using 3d rendered emblems?"; "Emblems are placed on a wooden pedestal each on a concrete platform with a space backdrop.
Hovering over each causes a light to shine on it with some dust reflecting off the light. The emblem rotates slightly as you
hover your mouse over it. Clicking on it gives you an option to continue campaign or start a new one."; "Then clicking on it
starts a new campaign with the smallest tier ship and you can fly around the galaxy map performing missions. Mockup campaign
systems first. Then I'll get you some mission scripts to implement in our three js mockup env". Changes `faction-select` and
`campaign-missions`; mission scripts go in `docs/mockups/lib/campaign-missions.js`, one `FCMissions.define` call each.

Then, 2026-11-05 (v11): "let's implement all \"ready\" mission types, amke sure to keep this artifact updated. I'd also
like to add \"capture the prize\" mission to capture a designated target ship for research"; "asteroid base assayult: we do
have asteroids". The six ready types and Capture the prize are scripts with five grades; the Missions guide (v2) records
which types are in.

Then, 2026-11-05 (v13): "player ever controls one fleet at a time, the rest of the fleets are controlled by the factions.
The map must be dynamic, all ships move move around every few turns."; "misions should have standees so I know what tile
they are on"; "player fleet should have a gold standee"; "ship fleets should point in the direction they are facing (last
move delta)"; "ship standees should have a flag next to them representing ship strength"; "show freigthers and their
routes for your empire, and show freighter routes for allies. develop and use placeholders for freighters"; the nebula
palette ("here's a better pallete for nebulas, the colros are kinda random right now") and "maybe nebula should have a
couple different colors, like a main color and an accent color".

Then, 2026-11-05 (v14): "its angled weird lol, Ithink mission flags sould just be 3d"; "how come my ship isnt centered?",
"if theres more ships or objects on it, then offset, otherwise I think ships should be centered"; "make this more obvious
its a button" (a mission in the sector panel); "need a win or loose screen, and how many prestige + reputation I got";
"need to show multi turn moves please".

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
v4), the Hunt's hover names in phosphor on the dark tube, and its title placards without the groove. Then, of the console's
Albatross: "looks like the albatros is behind a version, please update ships to latest": version 82 (Krai v21, Ssarith
v6), the ships are the exported glbs the game flies, the Krai's as the Krai line (their hull paint still the stand-in:
`ship-models-engine` 2.5). Then, of an
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
- **On the real model** (owner, 2026-11-04: "can you put that on the actual model?"): the emblem is on every Concord ship of
  mockup 6 (the Concord line, v19), on each one's forward band, port side, at 3 % of its length, with no windows under it.
  The placement, the break and the paint are one shared file, `docs/mockups/lib/concord-emblem.js`, used by both pages.
- **Borders** (owner, 2026-11-04, with a picture of the real model: "lets add that border back in here. so it appears more
  consistent, add some space between strip and emblem, and border on both edge of lines and emblem"): the emblem's frame is
  back in the band's seam colour, the band's cut edges carry the same seam (0.22 m, the band's own seam width), and 0.35 m
  of bare hull stands between them.

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
- Version 3 (owner, 2026-11-04: "now update the damn ships!", "Ssarith ships", "not anyother ships thoo lol, SSarith needs
  work"): three traditions drafted inside the Hunt on the same rooms, each choice naming its line in the culture or
  marked ours, none from the species' body: A Old Lodge, B Trackers, C Course. Survey H2 asks which stand.
- Version 4 (owner, 2026-11-04: "sillhoutte seems a bit rough along the spine. I would prefer something smooth. No go for
  C, A and B are good options I will think about that more. Add new C that replaces nacelles with a secondary hull instead,
  adding some assuymetry or lower or upper secondary hull"): smooth chamfers on every direction; C Course withdrawn; C Field
  Hull with its own rooms (`data/ships/ssarith/frigate_field_hull_plan.json`), below and to port, below, or above.
- Version 5 (owner, 2026-11-04: "I'd like to see these contours smoothed out top down and sides stay flat", "but without
  adding more polygons", "remove the wobble", "yea if this could be smoothed thatd be nice"): a fair hull on every
  direction and on the field hull, one chamfer along the body, smooth outlines, no polygons added.
- Version 6 (owner, 2026-11-04: "oof now theres uv issues"): the plating laid on each face itself, true size on the 45
  degree chamfers, rows lined up along the length; no geometry changed.
- Version 7 (owner, 2026-11-04: "ships arent very complex"): working detail from the plan and the bible (gun houses, bay
  doors, attitude thrusters, the shared thruster bells, a forward emitter) and each tradition's own parts.
- Version 8 (owner, 2026-11-04: "pack in those details by rendering a high poly model into textures", "Then applying metal
  detail texture on top"): three plating tiles and a fine metal tile modelled and rendered in Blender
  (`tools/blender/build_hull_tiles_ssarith.py`, `assets/textures/ssarith/`) and laid on the hulls.
- Version 9 (owner, 2026-11-05: "Bro why are you stuck on this. Delete this design. It sucks, look at the inspiration.
  Block out more stuff. We need a variety of designs. Not just 3 lame ones that look like coffins. Look at my examples -
  gorn from Starfleet command"): versions 1 to 8 deleted (the plans, the baked tiles and their generator, the old shots;
  git keeps them). A blockout board of twelve frigate-sized massings in grey clay; survey H4 asks which go on. The
  owner's examples did not reach the session and are asked for again.
- Version 10 (owner, 2026-11-05, with three images of the reference game's and a fan's Gorn ships, not committed:
  "gorn from star trek", "they looked menacing!", "make ships like this, with this kind of texture", "might need a new
  skill to make high -poly AO baked texture that looks drawn on"): twelve new blockouts in the examples' shape language
  (reach forward, a hood over a narrow neck, low raked blades, chevrons, light in strips), never their designs; a new skill,
  `painted-bake` (`tools/blender/painted_bake.py`), paints over a high-poly tile's Cycles passes so it reads drawn on.
- Version 12 (owner, 2026-11-05): the picks, "spearhead: there we go again with the ltiterals lol / headedf lance: great! /
  forked prow, great! bit of a mess of geometry / trident: awesome! / harpoon yes!"; the new rule, "every ship needs custom
  uv Unwraped textures. From NOW on . ALL SHIPS MUST AVOID REPETTIVE TILE FOR MAIN COLOR MAP", "tile textures are for DETAIL
  only", "dont forget to bake AO as well"; and on the picks, "these shapes need work, they arent connect together well, and
  violate our "no deformed primitive rules", shapes must be made with purpose and flow into the grater structures". Each
  pick is one grown skin (its branches split from the body's own section) and is baked onto its own UVs with AO, plating,
  chevrons and lit ports (`painted_bake.py --ship`, `assets/ships/ssarith/`). Both rules are in CLAUDE.md section 10 and
  the Ship Design Bible (chapters 14 and 15); the Concord and Krai lines have a task to follow.
- Version 13 (owner, 2026-11-05, with the reference game's sheet of a ship, not committed): "discard ships that I didnt
  want, only keep the ramining ones. I'd like more features onthe hulls, the gorn ships have things like vents, lights, and
  sub armor pieces, and primary armor peices. Armor pieces are carefully craftyer to not look the same", "notice stuff like
  liveries, smooth sphereical portions of armor, and specifically shaped vents", "no repeating elemnts, single axis
  symmetry, and lots of shading to give the armor depth". Only the picks stay. Each one's detail is modelled over its hull
  (`tools/blender/hull_detail.py`, `data/ships/ssarith/hull_detail.json`) and baked onto its own UVs; the rules are Ship
  Design Bible T5-T8 and CLAUDE.md section 10.
- Version 14 (owner, 2026-11-05: "Now those my friend are gorgeous looking star ships. Next up, lighting pass, windows,
  pbr, normal maps, logo, emissive texture (you have some features that look like glowy bits)"): the PBR set and the
  hull lights (floods, position lights, beacons, strobes, windows) baked onto each ship's UVs, the emblem from the
  emblems page painted on the top armour, the page's Look and Light switches. The method is the new `ship-detailing`
  skill, with image samples (owner: "write up a skill for detailing space ships with custom texturing", "Include image
  samples in your documentation"). Survey H5: the window and flood colours.
- Version 15 (owner, 2026-11-05, with two night shots: "we need some more lights pointed forward to paint more of the hull",
  "we need a carrier", "we need to show interior of ships with windows", "we need damage systems", "also this light, I
  wish it were more green", "the hull emission"): rows of forward floods, a greener emission, rooms behind every window
  (the Concord line's room code lifted into `docs/mockups/lib/ship-rooms.js`), the carrier (blockout 13) with launch bays
  and hangars, and battle damage on the shared `docs/mockups/lib/ship-fx.js`.

## 14. Krai badge in 3D

Owner, 2026-11-04, of the Fang Torc sketch KC+ on the Emblem Design Guide: "bro this version slaps", "Thats it", "keep
that", then "make the 3d version of the fang torc". Numbered 14 because two other branches each hold a page numbered 12
(the Ssarith console and the Ssarith ships) and 13 is left for them to settle.

- **One source of shapes**: the guide exports KC+'s parts (`tools/emblems/export_parts.mjs krai` writes
  `assets/emblems/krai/kc.parts.json`); the Blender render and the three.js badge both build from them.
- **Blender**: `tools/blender/emblem_krai.py` on the shared harness `tools/blender/emblem_render.py`; renders face on, at
  an angle and as clay into `assets/emblems/krai/`.
- **three.js**: `docs/mockups/krai-emblem-3d.html`, orbitable, 100 mm across, with the Blender renders beside it, on the
  shared badge library `docs/mockups/lib/emblem-badge.js` (the Concord badge, mockup 10, now uses it too).
- **Metals**, Material Maker graphs tinted: blade steel (`emblem_platinum`, `#b9c2cc`) for the ring, torc, thin crescent,
  forge star and three fangs; blackened steel (the same graph, `#2b2426`) for the field; blood enamel
  (`emblem_enamel_pearl` under a clear coat, `#c4161f`) for the founder's fang.
- **Heights** (mm): field 0.3-1.0, thin crescent 0.6-2.2, fangs 0.6-2.6, torc 0.6-3.0, forge star 0.6-3.3, ring 0-3.6.
  No two faces share a plane.

## 19. Ssarith badge in 3D

Owner, 2026-11-05, of the Bow Rites sketch SD on the Emblem Design Guide: "Definitely would like to see more colors and
shading on the bow and green arrows on the 3d rendering. I quite like that..shade the bow parts with like wood and the
arrows green with blood", then "Background should be like a beige, dull brown and dry green forest". Numbered 19 because
main holds 16 to 18.

- **One source of shapes**: the guide exports SD's parts (`tools/emblems/export_parts.mjs ssarith` writes
  `assets/emblems/ssarith/sd.parts.json`); the Blender render and the three.js badge both build from them.
- **Blender**: `tools/blender/emblem_ssarith.py` on the shared harness `tools/blender/emblem_render.py`.
- **three.js**: `docs/mockups/ssarith-emblem-3d.html` on the shared badge library `docs/mockups/lib/emblem-badge.js`, its
  maps listed in `docs/mockups/lib/emblem-materials-ssarith.json` (apart from the shared list, so the other badges stay small).
- **New Material Maker graphs** (`tools/materials/build_ptex.py`, 512 px, 0.05 m a tile): `emblem_wood_laminate` (the bows:
  plies of a warm and a darker wood, grain along them), `emblem_lacquer_watch` (the arrows, Watch green), `emblem_blood_watch`
  (the heads: the lacquer under dried blood), `emblem_enamel_forest` (the field: a dry forest of beige haze, dull brown
  trunks and dry green canopy, the hunting ground). The rim is the platinum graph tinted bone.
- **Heights** (mm): field 0.3-1.0; each bow's string, vanes, shaft, head and limb rise to 1.3, 1.5, 1.7, 1.9 and 2.3 mm,
  each bow 0.9 mm prouder than the one behind it (the smallest in front); the rim 0-4.4. No two faces share a plane.
- **Unique arrowheads** (owner, 2026-11-05: "make each arrowhead unique"), in the guide's SD too: the learner's leaf (no
  barb), the hunter's barb (one), the elder's tally (long, a notch for each hunt).
- **Holographic leaves** (owner, 2026-11-05: "In the 3d version I mean with like holographic leaves", "Should be 3d leaves
  shapes"): 70 leaves laid out once by `tools/emblems/ssarith_leaves.py` (`assets/emblems/ssarith/sd.leaves.json`, seeded),
  hanging at 1.4, 2.1 and 2.8 mm over the enamel forest, each a curled, cupped leaf mesh drawn as light: glowing at its
  edges and midrib, its colour shifting with the viewing angle, with faint scanlines (three.js), and an edge-weighted
  emission seen through (Blender).
- **v2, ferns, steel and glowing blood** (owner, 2026-11-05, with a photograph of ferns in deep shadow: "3d model Fern
  leaves. Arrow made of wood. Arrow head made of steal. Feathera red with orange tip. Arrows dripping with bright glowing
  green blood", then "Fern leaves are the holographic background jungle"): `tools/emblems/ssarith_ferns.py` grows 60 fern
  fronds (a stem with lobed leaflets, arched and curling) and spills the blood (pools on each head, drips down the face) into
  `assets/emblems/ssarith/sd.ferns.json`, one layout for both renders. The ferns are a hologram (light added over the field,
  glowing along every edge) over the forest enamel darkened; the shafts are the laminated wood, the heads steel, the feathers
  red with orange tips (their own polygons in the guide), the blood an emission. Every part of a bow now stands over the
  ferns (tops raised 1.6 mm; the rim to 6.2 mm). The holographic leaves and the green lacquer, blood and fern graphs are gone.
- **v3, a 3D fern bed** (owner, 2026-11-05: "z fighting, the blood doesnt look right, remove blood, and just make arrow hard
  green and arrow body metal", "redo the fern, render a fern forrest sccene. Make THAT exact scene!", "no hologram, just make
  a scene that looks like that as background for the emblem, and it should be 3d"): the badge is a deep frame (rim 10.4 mm,
  floor 0.6 mm), the recess filled with 40 bipinnate ferns in layers up to 6.4 mm (`tools/emblems/ssarith_ferns.py`, light
  falling off with depth; our own scene after the owner's photograph, which is not committed), the bows floating over them
  as thin parts. Steel shafts, hard green heads, no blood. The z-fighting was a feather's orange tip laid over its red body
  at 0.08 mm: they now meet edge to edge. The fern-leaf graph is back; the hologram and the blood are gone.
- **v4, thorny recurves and inner walls** (owner, 2026-11-05: "can you smooth out the bows?", "need walls inner, on emblem",
  "the bow is all wobly", "this is what it really looks like, but I need you to add some character to it, make it more
  alien", "ooh the thorny bows are great", "omit some thornes a the bottom half where the shooter might put their grip
  hand"): each bow one smooth recurve (an analytic curve, the tips flipping forward past the string), tally spurs on the
  back of each limb except where the grip hand closes, a forked hook at each tip; wooden inner walls lining the recess.
- **The Emblem Gallery** (owner, 2026-11-05: "create an emblem gallery artifact"): `docs/mockups/emblem-gallery.html`,
  https://claude.ai/artifact/VdjoRwjMKG3Er8rC2H2epS, every finished badge with its renders; on the tracker under Misc.

## 16. Special weapons

Owner, 2026-11-04: "we need to develop special weapons for each faction, currently only concord and krai are fully done,
so start with those two, we also ened high end fx + shaders for both of those things too". Then, while it was built:
"source and destination of phaser and disruptors must maintain in sync", "this means as they fire, the source and
destination of these beams must follow the ship source and ship destination", "a missile fired can break this rule as it
leaves behind a particle trail".

Version 1 (2026-11-04): the special weapons range. The Concord's Chorus lance (the Albatross's deflector beam, joined by
two Petrels' lances) and Quell torpedo (a Petrel's tube load: the struck shield fails, half the Krai cruiser goes dark for
8 s), the Krai's Forge driver (the cruiser's spinal cannon, 40 % through the Albatross's shield, the hull burning 6 s) and
Blood charge (a frigate's surge and bayonet ram). The approved ships as exported glb files, moving during every run so the
beams can be seen riding their ships; the card's numbers and the target's readout from `data/battle/special_weapons.json`;
High and Phone quality; twelve named shots. Effects: `docs/mockups/lib/weapon-fx.js`. Survey section X.

The owner's beam rule is in the game and in mockup 2 already (v80 after merging main, the Krai page v19, the Ssarith page
v6): change `weapon-fx`, tasks 2.1-2.3, and `docs/design/mockup-sync.md`. Drafted as mockup 12; renumbered 16 on merging,
since main had given 12 to the Ssarith ships (and 14 and 15 were taken).

Design: `openspec/changes/special-weapons/`, `openspec/changes/weapon-fx/`. References: SFC2 manual printed pages 22-25
(heavy weapons: direct fire and seeking, each race its own; weapon status, stunned); SFC3 manual printed pages 12-13
(primary and heavy weapons by empire and loadout) and 28-29 (refit by hardpoint).

Owner, on version 1 (2026-11-04): "Yea add standard weapons too. Kinda hard to see the ships on the phone. Make sure zoom
is appropriate. Those look awesome. I would make the shield bubble 50% larger", then "Also no rotation on on camera
please. I just want to see the fx fired between two ships".

Version 2 (2026-11-04): the standard weapons on the range too (the lance, the disruptor and the pulse torpedo, numbers
from `data/battle/weapons.json`, the lance's life from `data/battle/tuning.json`); one fixed view, side on, that never
turns or follows, with the ships holding station face to face 420 m apart and drifting a few metres so the beams can be
seen riding them; the view's distance fitted to the ships and their shields in the part of the screen the panels leave
free (on a phone the card starts folded and the controls are one row); every shield bubble 50 % larger. The same lance,
bolt and warhead looks now draw the battle console's shots (mockup 2 v77, mockup 8 v16).

Then, while version 2 was built: "pulse torpedo needs that iconic 6 armed flare" (with a picture of a star's six
diffraction spikes); "all factions can have missiles too, so develop missiels (in starfleet command they are refered to
as drones)"; "here is what missiles look like, they got that 3d missile model at the front end, a big ball plume, and a
tail" (with a screenshot of the reference game's missiles); "torpedo should have like discrete trail instead of a long
streak"; "shield bubble invisible unless hit". In version 2: the pulse torpedo flies under a six-armed star and drops a
trail of separate beads; missiles are a weapon for every faction (change `missiles`, survey D), each a plain dart with a
big ball of its faction's drive light behind it and a long tail, fired in salvos both ways with point defence shooting
them down; the shield bubbles show nothing until something hits them. The battle console mockup (v77) draws torpedoes and
missiles the same way.

Then: "torpedos should be purple, and close the gap on those trails, they are too far appart, large spheres to small
spehres"; "is the flare animated? the flare arms should be moving around"; "please avoid the color white for weapons";
"missiles depending on grade will have: red, orange, blue, green", "red being lowest grade, green being highest"; "stronger
purple please for torepodes"; "half the length of tail, I also want it to go from larger (2x) spheeres to smaller
spheres"; "and torpedo tail should fade out". Done: torpedoes a strong purple (`#8f2bff` in `data/battle/weapons.json`, so
the game's torpedoes too), a tail half as long of beads packed close, twice the size at the warhead shrinking and fading
to nothing; the six arms turn and flicker; no weapon core is white (each is its own colour at full strength; point defence
gold `#ffc23d`); missiles in four grades (`data/battle/missiles.json`), red, orange, blue, green, each salvo on the range one
of each.

Then: "Try to fire the torpedos straight. And the missiles should move slower. Different grades have different speeds and
damage values." Done: torpedoes leave the tube straight at the target and hold their line; missiles fly near a ship's
own speed, by grade 240, 320, 400 and 480 m/s with 8, 11, 14 and 18 damage and 30 s of fuel (`data/battle/missiles.json`);
today's missile in `data/battle/weapons.json` and the battle console's table is the orange grade (320 m/s, 9.6 km).

Then: "Missiles follow ships around. Can be countered with counter measures like flares, decoy shuttles, or pdc." Done:
the missile run now has the cruiser throw flares and the Albatross launch a decoy shuttle, each missile fooled by its
grade's chance (`data/battle/missiles.json`), the decoy destroyed by the first missile to reach it and the rest coming back
for the ship (after SFC2, printed pages 136-137); point defence is drawn as point defence cannon tracers, here and in the
battle console. Survey D6.

Then (2026-11-05): "theres some artifact when the ship doe a charge attack", "after the krai ship finishes chargin theres
like the black discharge at the bow"; "quell torpedo should be like a cross between a pulse torpedo and a missle". Done:
the black specks were the shared damage effects' dark smoke and dark shards; on the range the smoke is not drawn and the
debris glows hot. The quell now flies as a missile's body and ball plume with the pulse torpedo's six-armed star round its
lattice head and a bead trail, launched straight, then homing and weaving. Then: "also that pulse torpedo is kinda white, needs to be deep blue" (read as the quell, which was the white one; the
standard pulse torpedo stays the purple asked for on 2026-11-04): the quell is deep blue, `#2a4cff` in
`data/battle/special_weapons.json`, in its star, lattice head, plume, beads, launch flash and ring.

**Approved for implementation** (owner, 2026-11-05, in conversation: "Implement weapons in game"). The game build follows
the changes' tasks: first the standard weapons' looks in the 3D view (`src/Game/View/ShotView.cs` with `beam.gdshader`,
`flare.gdshader` and `beads.gdshader`, ported from `docs/mockups/lib/weapon-fx.js`), then missile grades and
countermeasures, then the special weapons.

## 17. Battle sounds

The owner, 2026-11-04: "pull in audio and music skills from crab breeze, lets get some audio for this game!" A sound is
heard, not seen, so its mockup is a page to listen on: every cue with a play button, grouped as the game uses them
(placed at a mount, where a hit lands, at the ship; the console, the alerts and the bridge bed unplaced). Made by the
`sound-effects` skill: 18 cues cut from CC0 recordings on OpenGameArt, 12 synthesised; every original named in
`assets/audio/sfx/SOURCES.md` and `AUDIO_SOURCES.md`. Chosen and checked by measurement and a spectrogram sheet
(`docs/screenshots/audio/sfx-sheet.jpg`), because a cloud session cannot listen: the owner's ears approve it.

- Change: `openspec/changes/sound-effects`. Shots: `docs/screenshots/mockups/sounds-*.png`.
- Stage: Drafted (2026-11-04). Questions: survey S4 (how a battle sounds in silent space).
- The owner's notes, 2026-11-06, all done in v3: "weapons all are too long, lance fire should sound more punchy" (every
  weapon cut to 0.5-1.2 s from its peak, with a kick; the Lance a crack, a snap and a kick in 0.55 s); "hull hits are
  all great btw, I would make some shorter sfx as alternatives" (the hits kept, each with a `_short` alternative);
  "red_alert sucks" (a recorded whoop klaxon, with a two-tone and a wail as alternatives); "amb_bridge - should be like
  aminute with bleeps and boops on theb ridge and scanning sounds" (a minute-long bed with about 48 bleeps, six sensor
  sweeps and four scan pings scattered over it).
- The owner, 2026-11-07: "bridge loop is no longer backgrouind noise, it has a really annoying whirling noise. I'm
  assuming thats the sensor sweep, get rid of the sweep sound, just subtle bleeps and airduct noise". Done in v4: the
  sweeps and pings gone, the computer-room recording (which swells every 3 s) gone too, the air duct twice over a low
  hum, the bleeps about 10 dB quieter (at most 11 dB over the bed in their band, in under 2% of the minute).
- The owner, 2026-11-07: "soften bridge ambient along with a lot of the other weapons, check the procedure in crab breeze
  how audio was softened with low passes". Done in v5 on Crab Breeze's two rules: the bridge gets its ambience beds'
  treatment ("ambient sounds needs to be softer and queiter", Crab Breeze `openspec/changes/ambience`: compressed 3:1
  from -18 dB, low passed at 3.5 kHz, a diffuse reverb, written at -14 dB peak), the bleeps now filtered with it; every
  weapon gets its outside-sound seal (Crab Breeze `openspec/changes/audio`: low passed at 4 kHz twice over, 6 dB down),
  heard from the bridge through the hull, with less saturation on the Lance and the Disruptor.
- The owner, 2026-11-07: "low rumble bridge ambient, it hurts my ears a bit", "make it sound pleaasent like proper
  ambience". Done in v6: the generator layer (86% of it a 53 Hz hum) removed; a soft room tone (pink air, 200 Hz to
  6 kHz, two uncorrelated sides) under the air duct, which is cut below 180 Hz; the whole bed cut below 180 Hz at
  24 dB/octave and above 6.5 kHz, so the 3.5 kHz dark roar of v5 opens to soft air. Measured: 0.03% of the energy under
  150 Hz, the rest even from 200 Hz to 3.5 kHz, the loudness within 5.3 dB, the bleeps soft over it (at most 11 dB).
- The owner, 2026-11-07, on v6: "no, it got worse, now sounds like water!", "I want it to sound like a hum". Done in v7:
  the pink-noise air removed (broadband noise reads as water or rain); the bed is a hum: our warm tone at 152 Hz with soft
  harmonics to about 1.2 kHz under a recorded hum raised an octave (76 to 152 Hz), the air duct faint beneath, nothing
  under 110 Hz. Measured: 69% of the energy on the hum's own pitches, none under 100 Hz, 2% over 2 kHz, the loudness
  within 2.6 dB, the bleeps at most 9 dB over the hum as the ear weights it (A-weighted).
- The owner, 2026-11-07, on v7: "audio still busted", "now it soiunds like electrical buz", "it sounds like electrical buzz
  which is wrong!", "why cant you do something like brownian noise", "https://opengameart.org/content/sci-fi-sound-effects-library
  space ambience right here". Done in v8: the synthesised hums removed (a stack of harmonics is what mains buzz is); the bed
  is Little Robot Sound Factory's recorded space ambience from the pack the owner sent (a soft drone between 60 and
  120 Hz), looped to a minute with the sub-bass under 45 Hz trimmed, the bleeps at about 10 dB over it as heard; and a
  brown-noise alternative (`amb_bridge_brown`, 60-600 Hz). The pack is CC-BY 3.0, used at the owner's request: its credit,
  "Little Robot Sound Factory, www.littlerobotsoundfactory.com", is on the page, in `AUDIO_SOURCES.md` and the sources
  tables, and must be in the game's credits (`tools/audio/licence_exceptions.json`). v9: every sound's link carries a
  fingerprint of its file, so a sound changed at the same path is never played from the viewer's cache.
- The owner, 2026-11-07: "Console keys - this should be implemented in the game": the console key sounds approved for
  implementation (`openspec/changes/sound-effects` tasks 3.x).
- The owner, 2026-11-07: "delete amb_bridge, use amb_bridge_brown", "put in game now". Done in v10: the brown-noise bed is
  `amb_bridge` and the recorded space ambience is gone (so the CC-BY pack is no longer used and no credit is owed); in
  the game the bridge bed loops under a battle and every console key plays its skin's key sound
  (`src/Game/Ui/ConsoleSounds.cs`, `data/audio/cues.json`; `docs/design/mockup-sync.md`). Built and checked by the tests,
  not yet heard in a playtest.
- The owner, 2026-11-11: "sheild ht STILL HAS THE DING IN FRONT, IT IS NOT GLASS< NOT CERAMIC< NOT METAL< IT CANNOT DING",
  "what METAL PANELS< IT HAS NONE< IT IS ELECTROMAGNETIC". Measured: the shield hit rang (66% of its energy after the
  attack in a few sharp peaks at 200-540 Hz): our "glassy shimmer" layer (96%) and every take in the "space ship shield
  sounds" pack (58-100%) were dings. Done in v11: both gone; the shield hit is our field whump (a noise burst whose
  brightness falls from 900 to 120 Hz, no tone) with an electric fizz, a crackle and a spark, now 7% ringing; its short
  alternative and `shields_collapse` lose the ringing takes too. `tools/audio/inspect_sfx.py check` now prints every
  cue's ringing and refuses a shield sound that rings.
- The owner, 2026-11-11: "ok implment it". The battle's sounds are in the game: every weapon firing (by its shot's kind,
  point defence its own), every hit landing (shield, hull, a torpedo through to the hull, a ship destroyed, the short
  alternatives when hits come fast), placed in 3D where they happen and heard from the camera
  (`src/Game/View/BattleView.Sounds.cs`, `src/Core/Audio/BattleSoundRules.cs`, `data/audio/battle.json`). Played in the
  scripted battles (`docs/playtests/2026-11-11-battle-sounds.md`), not yet heard by the owner.

## 18. Music

The same request. The first score, orchestral space opera over a synth bed (survey S1), built from four motifs written
down first in `docs/design/music.md`: the Concord (the player's fleet, an 8-bar theme in E flat major), the wonder
chord, the Threat (a C minor ostinato and horn call for any enemy) and the Lost. Cues: the Main Title, Battle Stations
(a seamless 43.64 s loop), Victory, Ship Lost and the motif sheet, rendered headless in LMMS with the GeneralUser GS
orchestra. Checked by loudness profile and spectrogram (`docs/screenshots/audio/music-spectrograms.jpg`), not heard.

- Change: `openspec/changes/music`. Shots: `docs/screenshots/mockups/music-*.png`.
- Stage: Drafted (2026-11-04). Questions: survey S2 (faction themes), S3 (battle layers).
- The owner, 2026-11-07: "we need custom music for each faction (just do the ones in game right now) and add ambient
  music as well". Done in v3: the Concord's ambient (Concord Watch) beside its title and battle; the Krai Warhost (the
  Line, the Forge at Rest), the Ssarith Hunt (the Bows, the Long Watch) and the Kelvane Institute (the Measurement, the
  Long Baseline), each from its written culture with every source named in `docs/design/music.md`. S2 decided by these
  words; S5-S7 ask whether each reading is right. The Kethran and the Vos, opponents with no skin yet, wait.

## 20. Battle console: Kelvane Institute

Owner, 2026-11-06: "start work on the UI for the institute, get latest from main and maintain the highest possible
standard for UI design".

- **The page.** It is the battle console of mockup 2, played in the Kelvane Institute skin of mockup 4, written by
  `tools/mockups/skin_page.py` from the one source page. Every rule, panel and control is the same code.
- **Hopping between ships.** The console follows the ship you command:
  - the research cruiser Patience joins the allied fleet and the swap scene, which is now "Four races" (keys 1 to 4);
  - the whole console turns Kelvane when you take her.
- **The construction.** It is the Institute's instrument bench, built on the Analog Panel Bible:
  - champagne bezels rolled round wells of dark smoked glass, where the readouts glow;
  - amber for a reading, phosphor green for whole, amber-orange for damaged, needle red for an alarm;
  - window keys whose light is their state (unlit at rest, amber on hover, hot when pressed);
  - title placards with a lit pilot jewel;
  - strips with their needle-meter bays;
  - a column backing engraved with a measuring scale.

  Every piece is rendered in Blender from fit-checked shapes, and every colour role is measured against the glass
  (design.md D2).
- **Live meters.** The owner, 2026-11-06: "that looks really good! can we have these meters be tied to the speed and hull
  integrity?" The strips' needle meters are now live: hull integrity on the top strip, speed on the bottom.
- **The emblem.** The owner, 2026-11-06: "I would like this hydrogen atom eigen plot used as the logo/emblem". Hydrogen's
  3d z² orbital, computed and drawn as a wireframe eigen plot, is on the crest in brass wire, on the name plate and in the
  live emblem.
- **Type and icons.** Share Tech Mono sets every readout. All 24 icons of the icon guide are drawn in the Institute's
  schematic hand.
- **Names.** The ships, officers and ranks carry placeholder names until the owner sets the Institute's culture (K1).
- **Change:** `openspec/changes/kelvane-battle-console`.
- **Shots:** `docs/screenshots/mockups/battle-console-kelvane-*.png`.
- **Stage:** Drafted (2026-11-06, v1).
- **Questions:** K1-K3 in the change's design.

## 21. Kelvane badge in 3D

Owner, 2026-11-06, with a book cover's hydrogen-atom plot: "I would like this hydrogen atom eigen plot used as the
logo/emblem", "in 2d logo, I also would like a 2 -tone render with wireframe and shape asw ell", "shade the 3d plot in
the 3d render of the emblem, use our emblem designer for this please", "green hydrogen icon, irrdescent and red satin soft
fabric background on an oval emblem coin", then "the 2d version the green wireframe and oval background can be used for
ship hull logo".

- **The emblem.** KID, as the Emblem Design Guide names it, picked: hydrogen's 3d z² orbital, computed (not traced from the
  cover) by `tools/mockups/kelvane_emblem.py`. That one file writes every form of it, in KID's colours ("on the emblem,
  background red, shape green"):
  - the console crest's brass wire and the name plates' mark;
  - the 2D logo and hull logo (`assets/ui/kelvane/emblem.svg`): the oval coin, red satin, the plot in two greens with its
    wireframe light green and its outline deeper;
  - the badge's relief, its grid and its satin.
- **The badge.** Built on the emblem designer (`tools/blender/emblem_render.py`, `docs/mockups/lib/emblem-badge.js`): an
  oval coin in a champagne rim, red velvet in broad soft folds, and the orbital embossed on it as a slight bump, iridescent,
  its fill dark green and its wireframe green, shaded on the full 3D orbital's normals (owner: "the logo should be embossed
  slightly , popping out with a slight bump, it sohuld also be irridescent, the wireframe lines should be green, the fill
  should be darker green, and the background should be more like velvet fabric than plastic"). It is rendered in Blender
  (`tools/blender/emblem_kelvane.py`) and built live from the same meshes on the page.
- **Change:** `faction-emblems` and `kelvane-battle-console` (D9, D10).
- **Stage:** Drafted (2026-11-06, v1).

## 22. Space fly-through

Owner, 2026-11-05: "write up a beautiful fly through of space with ray marched nebula clouds and stars" (after "never to use
poor RNG stuff for stars", "can you make a better proc gen starfield?").

The camera flies a loop through volumetric nebula clouds, ray-marched in the shader (integer-hash gradient noise, warped,
glowing gas, dark dust lanes, stars buried in the gas lighting it), past 33,000 seeded stars in the game's star classes and
colours, which the clouds dim. Four palettes (Verdigris, Crimson reach, Ice halo, Ember), three quality levels, a free look
that stays where it is left. A candidate backdrop for the main menu.

- Change: `openspec/changes/space-flythrough`. Shots: `docs/screenshots/mockups/space-flythrough/`.
- Stage: Drafted (2026-11-05, v1).

## 23. Kelvane Institute ships

Owner, 2026-11-06: "begin designs on the institute ships".

**Approved for implementation** (owner, 2026-11-07, of version 10: "perfect, implment these ships in game"; approved for
build by the same words). In the game by change `kelvane-ships-engine`: the five classes and the Gallery fighter exported
from the page, the carrier's fighters parked on its pads, the Institute fought from the Concord's and the Krai's side and
played as a side of its own. To record in the tracker (the desktop session republishes it).

- **Breadth first.** As the Ssarith Hunt's ships went well once they went broad (mockup 12, version 9), the Institute
  starts with a board to pick from: twelve frigate-sized blockouts, 104-122 m, beside the Concord Petrel's 88 m.
- **Traditions, drafted.** The Institute's culture is one line ("truth is measured, never assumed"), so five traditions
  inside it are drafted for the owner to pick, change or reject (Ship Design Bible C4), none from the Kelvane's bodies:
  - the Calibrators, the Open Notebook and the Long Baseline (the three drafted for the emblem);
  - the Field Stations and the Collider Halls (new: long voyages and a weapon).
- **The blockouts.** Case, Stepped deck, Runners; Twin hulls, Slotted; Waisted baseline, Spread arms, Long spar; Gallery
  bow, Terraces; Loop, Through-beam.
- **How they are grown.** Each is one closed grown structure (chapter 14) on the shared `docs/mockups/lib/hull-grow.js`,
  lifted from the Hunt page, whose ships are unchanged. The Institute has its own section layout: a rounded case after the
  console's rolled plates.
- **The look.** Champagne, dark glass and amber from the console, or plain clay.
- **Change:** `openspec/changes/kelvane-ship-concepts`.
- **Shots:** `docs/screenshots/mockups/kelvane-ship-concepts/`.
- **The loop family** (version 3). Owner, 2026-11-06: "l1 loop looks cool, can you make me some more variations of ships
  with loops? horizontal, vertical, both?". Seven more, 13 to 19, beside the Loop:
  - horizontal: Long loop, Twin loops, Wide ring;
  - vertical: Tall loop, Keel loop;
  - both: Crossed loops, Quartered loop.

  Each is the hull parting round an opening and closing again, its inner walls lit as the beamline. A Loop family switch
  shows them alone (design D7).
- **The line** (version 4). The owner, 2026-11-06: "12,11,18.19,17 all really good shapes, start detailing these, use more
  complex ones for larger captial ships like destroyer, cruiser, batlecruiser and carrier, we might want to theme them after
  advanced science and research vessels, this faction will specialize in scientific missions". The picks, simplest to most
  complex, are the classes, each a research vessel, its accelerator its main instrument and its lance:
  - the Parallax-class survey frigate (5 Slotted, 126 m; the owner: "12 is no go use 5");
  - the Candour-class beamline destroyer (11 Loop, 136 m);
  - the Patience-class research cruiser (17 Keel loop, 166 m);
  - the Rigour-class collider battlecruiser (18 Crossed loops, 196 m);
  - the Curiosity-class probe carrier (19 Quartered loop, 214 m).

  Each is grown again at its size: two identical instrument decks round a glazed slot, stepped instrument and laboratory
  decks with glass along their risers, a collision hall, a hangar deck, a launch well. The four larger ships are the Collider
  Halls', the frigate the Open Notebook's. The names are placeholders (survey I6).
- **The first detail pass** (version 5). Each class is unwrapped and baked onto its own UVs from a modelled detail layer (the
  ship-detailing skill):
  - clean and sterile (the owner: "Mind the clean and sterile and almost scientific aesthetics. They would have decorated
    their ships with smooth round pieces rather than industrial greebles"): calibrated grid seams in enamelled champagne,
    pillowed edges, no greebles, trenches or slatted vents;
  - smoked-glass sensor domes and smooth round pods sunk into the plating;
  - amber jewel lamps, red livery bands, the KID logo;
  - a measuring scale engraved down each flank;
  - floods, position lights, beacons and strobes.

  The bake tools take the faction as data (`--faction kelvane`; the Hunt's values unchanged). Look: Painted, PBR,
  Champagne, Clay. Light: Studio, Night. Windows, rooms, bays, damage and breakup come next, as they did for the Hunt.
- **v6** (owner, 2026-11-06: "we need greebles on the inside", "missing texture on the nose", "also where are windows to
  give scale"): machinery on every face that looks across an opening, the outer hull still clean; a lit aperture's middle and
  the frigate's glass nose an instrument's array of cells; rows of windows along every flank with rooms behind them, drawn by
  the pane code now shared with the Hunt (`docs/mockups/lib/baked-ship.js`). Design D10.
- **v7** (owner, 2026-11-06: "greebles look too random", "large vents, fuel pods, coils and pipes", "carrier should have
  landing pads and fighters", "diagonals on corners", "these lines are quire dark", windows "in awkward spotrs", the flood
  "light dots", rooms "more yellow/orange tinted like they are well lit for all ships"): rows of engineered machinery,
  the carrier's pads with a craft on each, diagonal plate corners, lighter seams, windows that look out, no flood dots,
  warm rooms on every ship (mockups and game); then, after the owner's reference images ("some examples of greebles
  for you to study", "some hull armor examples", "details idea"; their shape, never their art): low-relief panelled
  machinery with no fuel pods ("I hate those pods"), plate grooves and small marks, inspection hatches. Design D11.
- **v8** (owner, 2026-11-07: "Love the carrier. I see some square corners on hull plating here. Please make mockup designs
  of shuttles. Carrier should have shuttles upside down as well. We are in space afterall"): every plate corner where cuts
  meet on the diagonal; the carrier's ceiling pads with shuttles hanging upside down; the Small craft family, eight
  shuttles grown from the board's hulls, the Runner skiff the carrier's (survey I7). Design D12.
- **v9** (owner, 2026-11-07: "Make variants of 206."): six variants of the Gallery bus (211 high gallery, 212 long hop, 213
  glass nose, 214 wide body, 215 keel skids, 216 deep chin), grown from one gallery hull function shared with blockout 9
  (survey I8). Design D13.
- **v10** (owner, 2026-11-07: "detail gallery bus 215 try adding mounts for phasers or something, these are supposed to be
  fighters"): 215 is the Gallery fighter, its chin parted into two phaser prongs ending in emitters, detailed and baked at
  its own scale, parked by the carrier on its floors and ceilings. Design D14.
- **Stage:** Drafted (2026-11-07, v10).
- **Questions:** survey I1-I4 (traditions, picks, palette, the first class).

## 24. Freighters

The owner, 2026-11-07: "we need to design freighter for every faction in the game currently". Freighters already have
work: the campaign map runs each faction's freighters along its routes, Convoy defense and Convoy ambush keep or destroy
two of three, and a Distress call can come from one. Today the map draws a placeholder and the battle flies the Kethran
frigate stand-in. The reference game's convoys: SFC3 manual, printed pages 37 and 42.

- **v1**: a blockout board of eighteen, three a faction (Concord 1-3, Kethran 4-6, Vos 7-9, Krai 10-12, Hunt 13-15,
  Institute 16-18), each a spine (loads racked outside), a hold (the cargo shut inside) or a cradle (the hull parts round
  its cargo), 140-210 m. Each names a tradition inside its faction; no faction has a merchant tradition written down, so
  most are drafted, and the Kethran and Vos concepts use none of their console rows that read the species' bodies. Each
  hull is one closed grown structure on `docs/mockups/lib/hull-grow.js`; only the cargo is placed. In faction colours or
  clay, beside the Concord Petrel and a person.
- **v2, v3**: the notes name survey section V; the libraries re-inlined.
- **v4** (owner, 2026-11-07: "Freighter should only focus on approved factions", "Narrow down freighter options to ones
  where cargo is visible. And make sure all cargo is the same size", "The containers I mean", "The cargo hull shape it
  self can be the way it was in the draft"): eight concepts, two for each of the Concord (1-2), Krai (3-4), Hunt (5-6) and
  Institute (7-8), every one carrying the same 6 by 6 by 12 m container in sight; the holds and the Kethran and Vos
  concepts are gone; the hulls are as drafted, the Twin ledger's containers stacked between its hulls.
- **v5** (owner, on a phone: "Nothing renders"): no flatShading, the canvas follows its stage's size, a notice if the 3D
  view loses its graphics context.
- **v6** (owner, 2026-11-07: "1,4,5,8, but with 8 having really skinny arms and link.tbe arms together with a thin bridge
  at the end to keep the loop design ethos", "All freighters should be 2x bigger. Along with the containers too..this way
  cargo ships are much bigger than any combat ships..as they should be", "The institute freighter should be rounder at the
  tip", "You got the institute draft wrong", "Show m artifact with just the four"): the page opens on the four picks
  (101-104), every freighter and container twice the drafted size (300-360 m, 12 by 12 by 24 m containers), the Twin
  ledger made a loop on really skinny arms curving in to a thin bridge with a domed nose; the board of eight beside them.
  "Then go ahead and detail them", then "Don't do details yet": detailing waits.
- **v7** (owner: "Fix the institute", with an outline of the shape wanted, "I made an outline of what I want"): the Twin
  ledger's stern case at full width, its outer strips running straight on as the skinny arms to the rounded bow.
- **v8** (owner: "Fix the institute again.  No nipple at the end", "Flatter bow"): the Twin ledger's arms turn in on a
  squared quarter to a flat bow, closed flush, no nose standing proud.
- **v9** (owner: "The institute ship will be shorter than is cargo. That is the cargo is taller in height"): the Twin
  ledger's hull 16 m tall as shown, under container stacks 25 m tall.
- **v10** (owner: "Adjust institute ship len to fit cargo better"): the Twin ledger's straight run is its eight rows of
  containers with 1.5 m clear at each end; 274 m as shown.
- **v11**: the four picks detailed (the ship-detailing pipeline): each unwrapped and baked at 1024 px with layered armour
  in its faction's look, liveries, emblem, floods, lamps and windows with rooms; Painted, PBR, Studio and Night; the
  containers given a drawn face.
- **v12**: the four baked again at 2048 px and 32 samples.
- **v14** (owner: "you need like better cargo textures, lookem up online for cc0 options, back ao and get diffuse and
  normals and specular for each container"): the container modelled and baked from Poly Haven's CC0 "Container Side":
  diffuse, normals, AO, roughness and specular.
- **v15** (owner: "also containers dont seem to be lit correctly", "they are glowing"): the containers lit as the hulls are,
  their paint kept to the hulls' level by day and dark at night.
- **Stage:** **Approved for build** (2026-11-07, owner in conversation: "After that change. Now I approve. Detail them").
  Drafted v10. Survey V2 and V5 decided.
- **Questions:** survey V1, V3, V4 (traditions, one class or two, arms).

## 37. Fractal vessels

Owner, 2026-11-10: "im just curious, a faction is designed to be a fractal type of life form. and interdimensional being.
what would that look like in our game? can we build raymarched fractal ships?" Then: "do the mockup yo", "dont worry
about phone", "I dont really need this stuff to work on a phone", "three js mockup lets go".

- **The page**: three vessels drawn by raymarching a distance field (each a 12-triangle box whose shader marches to the
  surface and writes its depth):
  - a 4D quaternion Julia set sliced into our space (the escape-time school), 132 m;
  - a chain of five folded Menger blocks (the tilers), 112 m;
  - a branching tree (the branchers), 128 m.
- The Concord's Petrel (86 m) sits beside them for scale.
- **Controls**: the 4th-dimension slice (or the fold turn, or the branch angle), drift, damage, glow, halo, iterations, march
  steps, render scale, and a frame-time readout.
- **Damage**: fold levels lost and holes cut from the field, with glowing rims.
- **v1** (2026-11-10): first draft. Shot at desktop only.
- **v2** (2026-11-10; owner: "Make this thing phone friendly please! Also half resolution ray match if you don't mind"):
  - the vessels are marched at half the screen's resolution (a quarter of the pixels), the Petrel still hiding what is
    behind it, with a switch for full size;
  - on a phone the controls are a sheet you can fold away (upright) or a narrow column (sideways), every control a 44 px
    target, the view buttons a strip that scrolls;
  - shots at desktop, phone and portrait sizes.
- **Stage:** Drafted.
- **Questions:** survey FV1-FV5 (whose vessels, what a vessel is, which forms, phones, hits).
