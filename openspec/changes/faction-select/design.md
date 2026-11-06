# Design: faction-select

## The screen

Main menu, Campaign, then this screen. In metres, seen from 7.3 m back and 3.5 m up, looking at (0, 1.22, 0.1), 36 degrees
of field.

The table (owner, 2026-11-06: "I need the light with the dust particle, and a wooden table, selecting a faction shines a
light, and the emblem hovers a bit and turns with my mouse movement", "generate the best wooden textture you can with
material maker") replaced v1's pedestals on a concrete slab:

| Piece | What it is |
|---|---|
| Backdrop | The Star Crew nebula sky `verdigris` with its parallax stars (`docs/mockups/lib/space.js`), a far planet up left |
| Table | A walnut top 11.4 m x 3.4 m, 70 mm thick, its edge rounded 15 mm, its top at 0.78 m, on an apron and four square legs, in the Material Maker graph `table_wood` (below), one tile to 3 m of table: the scene is built large (the badges are 11.5x a hand-held casting), so its boards are 0.75 m wide and its rings a hand's breadth apart. Polished: clearcoat 0.35 at roughness 0.28, the studio's reflections in it held to 5 % so the lamps' pools are the look |
| Rings | A brass ring let into the wood under each badge, 0.84 to 0.91 m across, 3.3 m apart |
| Emblems | Each faction's badge from `docs/mockups/lib/emblem-builds.js`, the same builder as its badge page, 1.15 m across, floating with its centre 1.66 m up over its ring, leaning back 7 degrees |
| Light | A dim hall: hemisphere fill 0.12, a cold rim from behind 0.35, the badge studio's environment map (`FCEmblemStudio`) at 35 % until lit |

**The wood, `table_wood`** (`tools/materials/build_ptex.py`, 1024 px, a 1 m tile): flat-sawn walnut, four boards to the
tile, each from its own log (its own tone, its pith beside or under it, so some boards show cathedral arches and some run
straight), butt-jointed once a tile at a staggered place; rings 4.5 to 7 mm apart, each year light earlywood darkening slowly
into latewood and starting again at once; the rings waved by a warp stretched along the grain; open pores streaking along
it, ray flecks, a few mineral streaks; a hairline seam between boards; a deep polished finish (roughness 0.16 to 0.23,
rougher in the pores and seams). Rendered headless by Material Maker (`tools/materials/export_materials.sh table_wood`);
the contact sheet is `docs/screenshots/materials/contact-sheet.png`.

Every badge floats: a slow bob of 35 mm at 1.1 rad/s, each on its own phase, rising 70 mm higher while its lamp is on. Every
badge turns toward the pointer, the one under it fully (up to 0.5 rad of yaw and 0.3 rad of pitch, 1.6 and 1.0 rad per unit
of the view's width and height), the others at 35 % of that, easing at 6 per second.

The lamps: a spot 6.4 m up and 1.7 m in front of each badge, aimed through it onto the wood, at 0.25 when idle plus 6.5 x its
level: **full on the picked faction** (selecting a faction shines its light), 0.55 on the one under the pointer; it rises over
about 0.2 s and falls over about 0.4 s, with soft shadows (1024 px map) of the badge on the table. Its beam (an additive cone
from the lamp to the table, brightest along its middle and toward its base) and 420 dust motes in it, drifting on slow
currents, each glinting on its own beat, lit only inside the cone and only while the lamp is on; the badge's environment
reflections rise with the lamp.

Clicking an emblem keeps its lamp on and opens a card in that faction's skin: the faction's name and emblem, the saved
campaign (captain, turn, credits, ships) or "No campaign saved", **Continue campaign** (disabled without a save), **New
campaign** (the captain and difficulty sheet, then the map), and Back. Clicking empty space closes the card. On a phone in
landscape the card sits at the right; in portrait along the bottom (survey U2).

The Ssarith Hunt is playable with stand-in ships and the card says so: its ships wait on the owner setting its culture
(survey H1; CLAUDE.md section 1).

## Skins on the campaign

`docs/mockups/lib/kitcss.js` turns a skin's 9-slice kit pieces into CSS border-images with the pieces' own slice margins:
frame.panel and frame.inset (panels), button.idle, hover and pressed (keys), tab.idle and tab.active, bar.track and bar.fill
(red when over). The pieces are drawn by the skin's own painters (`lib/concord.js`, `lib/krai.js`, `lib/ssarith.js`), the
same kit the battle console draws, so nothing is drawn twice. Large rendered frames are scaled so no margin passes 26 px;
keys and tabs are drawn at their element's height. Each skin's colour roles and type come with it.

Which skin a screen wears: the faction screen the faction picked (Concord before a pick), the campaign its side's, the
menus and every skirmish the Concord's (owner, 2026-11-05: "stick to Concord skin for skirmish, its kind of awkward with
skins changing when I select different skirmish maps").

The Concord's panels sit on one black box, borders and all, its corners following the sweep's outer curve with a 6 px band of
black round the outside (owner, 2026-11-05: "can we get a whole box around this? include the border too, the black background
shoule wrap and curve aorund the outer border"). The metal skins keep their frames as their own ground (CLAUDE.md section 7).

## Saves

One campaign per faction, kept in the browser (localStorage) where the host allows it, otherwise for the visit. The game's
own save is designed with the campaign's implementation.

## What it costs

The page carries the three badges' Material Maker maps (about 3.5 MB inlined), the fern bed (2.2 MB) and the table's four
1024 px maps (0.5 MB as WebP), so the page is about 9.9 MB of its 16 MiB limit. The scene is about 1.2 million triangles (most of it the fern bed) and 3 shadowed spots:
fine on a desktop; for phones the game would bake the casting's shadows and use one live spot.

## Open questions

| # | Question | Provisional |
|---|---|---|
| 1 | ~~Concrete~~: the concrete went with the pedestals (2026-11-06); the table's wood is the graph `table_wood` | Done |
| 2 | Does the Ssarith Hunt stay playable before its culture and ships are set? | Yes, with stand-in hulls, said on its card |
| 3 | Menus before a faction is chosen: Concord skin, or a neutral one? | Concord |
| 4 | Sounds for the lamp and the pick | None yet (the Sounds page has no cue for it) |

## In the game (2026-11-06)

`tools/mockups/export_faction.mjs` loads the page headless, builds this scene and exports it: the table and rings
(`assets/scenes/faction/table.glb`), each badge upright in metres (`<faction>.glb`; a material per group split into a mesh per
group), and `manifest.json` with every number above read from the scene (the table, each slot, the lamp, the beam, the dust,
the turn, the camera) and each badge material's Material Maker graph, tint, clearcoat and sheen. The badges carry no maps:
the game builds their materials from the committed maps (`assets/textures/materials/<graph>/`), one copy.
`src/Game/Flow/FactionStage.cs` puts it together in its own world under the faction screen: the same lamps, a port of the
beam and dust shaders (`faction_beam.gdshader`, `faction_dust.gdshader`), a port of the Concord field's hologram
(`holo_field.gdshader`), a soft studio sky for what the metal reflects, and the wood lit by the lamps and the rim alone (the
page's 5 % environment; Godot has no per-material environment strength). `FactionScreen` draws the names and keys over it and
tells it where the pointer is. Differences left: the Concord pearl's thin-film colour shift (Godot's standard material has no
iridescence) and the velvet's sheen are approximated by a clearcoat and a rim light.
