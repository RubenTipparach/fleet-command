# Design: paper-map

## The room, at scale

One scene unit is 5 cm here, so a hex is 10 cm across and the chart about 2.5 m by 2.2 m (30 by 22 hexes), a big wall poster
laid flat. It lies on a walnut table 3.1 m by 2.8 m, 0.75 m high (`walnutTable`, the faction screen's table builder: the
`table_wood` graph, a tile to 1 m). The room is 8.5 m square, its floor a deep red cut-pile carpet (`carpet_red`, a tile to
0.5 m). The walls (owner: "a rustic dark green wall with wood trimmings") are 2.8 m high, 3.1 m from the table's middle: dark
green limewash on old lime plaster (`wall_green_rustic`, a tile to 1 m: the colour uneven, brush strokes every way, hairline
cracks, the pale plaster showing in a few worn places) above a wainscot of walnut boards to 0.95 m, a chair rail, battens
every 1.6 m up to a crown moulding, a post in each corner and a skirting board, all in the table's walnut darkened; a brass
sconce in the middle of each wall washes it with warm light (the first version's rose wallpaper is gone). The map's camera keeps its fixed angle; zoomed in, the chart fills the view; zoomed out, the
table, the carpet and the papered walls show round it.

Light: kept low (owner: "The map is a bit bright"): a warm lamp hanging 1.6 m over the table (a spot light at 0.85, its shadows on: the miniatures throw shadows on the chart, the
chart on the table), a warm fill from the room (hemisphere, cream above, red below), a weak key from one side. What the hulls,
planets and stands reflect is the room's colours (a small probe: warm walls, a red floor, a bright ceiling).

## The chart (`paperSheet`)

Drawn into one canvas at 56 px to the unit (2,800 by 2,440 px) from the campaign's own map, seeded by the side played, so a
campaign's chart is the same every time it is drawn:

| Layer | What it is |
|---|---|
| Paper | The `map_paper` graph (aged cream, mottled, fibres, foxing), a tile to 0.25 m |
| White border | 1.8 units (9 cm) round the print, kept pale |
| Star map | The print is a star map (owner: "like 2d printed star map. With a cool nebula background"): night-blue ink with a nebula across the sheet (`nebulaPrint`: a shader rendered once into an image, domain-warped noise on an integer hash, never `fract(sin)`; a galactic band from lower left to upper right; clouds in the nebula palette, blue to violet, teal and rose, gold at bright edges; dark dust lanes; dim cores) and its stars (`printStars`: one jittered point to a 9 px cell where the seeded draw says so, brightness and colour by the shares in `data/space/starfield.json`, the brilliant ones with a small four-point flare); the paper's grain laid over at 30 % |
| Territory | Each held hex washed faintly in its holder's colour |
| Grid | Every hex outlined in its holder's colour, lightened to read on the dark ink (unclaimed space in a pale ink); a heavier rule inside each hex along every edge where the holder changes, as the glass board's ribbons did |
| Terrain | Stars as glowing printed suns. Nebulae and asteroid belts are not printed: their drawn billboards stand on the chart (owner, 2026-11-08: "nebula and asteroid drawings dont need to appear as part of map texture") |
| Fog of war | The game's three states (change `campaign-space`): unexplored sectors flat black ink, nothing charted; revealed sectors under a lighter veil (45 %), their grid in the holder last seen (`CAMP.known`); visible sectors as they are now. One mask over the print, the charted hexes cut out of it 8 % wide, drawn at a quarter size and blurred by 0.28 units (1.4 cm), so the border is smooth (owner: "Fog of war border should be smoother") |
| Numbers | Each hex's column and row (`0412`), small, at its top |
| Frame | A double rule round the print; the title "A CHART OF THE MARCHES" and a line under it naming the side it was surveyed for; a five-sector scale bar |
| Wear | Ink rubbed pale in patches and specks; three folds across and one down, rubbed pale with a shadow along them; two coffee rings, water stains, foxing thickest toward the edges |
| Edge | Frayed; six tears running in toward the print; one corner torn away; browned in toward the border and crusted dark at the tear |

The nebulae's 3D clouds, the glass hexes' look, the board's mirror and the space sky are not drawn on the paper map; the glass
hexes stay, unseen, as what the pointer picks. Planets, stations, ships, banners, freighters, the stars' suns and the belts'
rocks stand on the chart as before (the rocks dark, as pebbles).

## The fog of war on the chart

The game's rules, the mockup's code (`visibleSet`, `reveal`, `fogOf`, `shownOwner`, `applyFog`): seen now is every sector the
player's empire holds, every sector within 2 of the fleet (`map.sight.fleet`) and within 1 of a friendly fleet or freighter
(`map.sight.friendly`; friendly is your own or an ally at standing 25 or more). What is seen is revealed, with its holder, at
the campaign's start and load, at every sector the fleet flies through, and after the factions' turn; an offered mission
reveals its sector. On the chart (redrawn when the fog changes) the three states print as in the table above; on the table, a
faction's fleets and freighters stand only where seen now, ports, suns, rocks and mission banners wherever charted, and
nothing on the unexplored. The hover card and the sector panel say "Unexplored" or "(last seen)".

## The drawn pieces (`docs/mockups/lib/drawn.js`)

The owner, 2026-11-08: "Can we make the nebula or storm clouds like hand drawn billboard puffs of cloud. And make the planets
have that hand drawn quality too? I just want everything in space campaign map to look very pirate and very space". The chart
becomes a pirate's chart of the stars: what stands on it is drawn as a chart maker would draw it, and only the ships and
stations stay as models (the owner, the same day: "The ships as their original models look great").

| Piece | Drawn as | On the chart |
|---|---|---|
| Ink | Sepia (`#2a1b10`), the pen's line wobbling on a smooth seeded wave (never jitter), swelling and thinning with pressure, tapered at open ends, a faint second pass where it bled | Every outline |
| Wash | Watercolour: an even coat, blooms of thicker and thinner paint, the paper's grain lifting through, pigment pooled darker at the edge | Every fill |
| Light | From the upper left, as the lamp. Tone is built by the pen in three zones deeper into the shadow (lower right): single lines through the mid-tone, crossed in the shadow, a third set in the deepest part (owner, 2026-11-08: "More cross hatch for space terrain stuff"), the lines at twice their first density, a little finer ("2x cross hatch density"): 1.6 % of a cloud's width apart, a planet's every 4 of its 220 cells | Every piece |
| Nebula cloud | A cartoon cumulus: crowns heaped at the back, a row of puffs in front overlapping them, each puff its own washed disc with its shadow crescent hatched and its edge inked, then a heavy silhouette and a flat base. The cloud's main colour low, its accent on the crowns. Three drawings per cloud, shared by its sectors | A billboard at the back of each nebula sector, 1.2-1.5 units wide, in three sectors of five a smaller one in front, so a cloud of sectors reads as a bank; under each, its shadow on the chart: a soft dark pool, cross-hatched, 1.75 by 1.55 units, over the sector |
| Storm | The same cloud in slate, hatched heavier, a forked yellow bolt out of its base | One cloud in three (by its id); a look only, no rule changes |
| Planet | Drawn live by a shader (`docs/mockups/lib/planet-shader.js`, below): a turning world of its kind, its light in five flat bands, the night side cross-hatched band by band, coastlines and limb inked, a ring in its holder's colour | A camera-facing quad twice the disc's width (the ring fits at any tilt), the disc 0.58 units across for a home world, on a hexagon flight stand in its holder's colour as the ships and stations stand (owner, 2026-11-08: "give planets those hexagon standees as well") |
| Sun | A disc in warm wash with a darker rim and curls of churn, sixteen rays alternating a straight spike and a wavy flame | A billboard 2.0 units wide, with its warm point light kept |
| Asteroid | A lumpy outline, grey-brown wash, a crater or two, the dark side hatched; twelve drawings shared by every belt | Half as many rocks as the 3D belt, each a small billboard |
| Compass rose | Two rings with ticks, sixteen points (cardinals long, half and quarter points shorter), each point half washed red and half bare, a fleur at north | Printed on a patch of bare paper in the lower left of the print |
| Rhumb lines | Lines fanning from two hubs (32 from the rose, 16 from the upper right), every fourth one red | Printed faintly across the whole print |
| Names | The engraved face IM Fell English SC (SIL Open Font License, Google Fonts) in sepia with a paper halo, a stroke of the holder's colour under it; the chart's title in the same face | Every label on the map |

Every drawing is seeded with Mulberry32 (CLAUDE.md section 10), drawn once into a canvas and kept as a texture for every map built
after (`drawnTex`), and stood on the chart as a sprite whose foot is the drawing's lowest point (`drawnSprite`). The pieces hide
under the fog of war as the 3D ones did (`fogKey`). The space look (`?look=space`) keeps its 3D clouds, planets and stars.

Taken from the reference games: nothing. The look is a sea chart's (compass roses, rhumb lines, inked and washed vignettes),
which is a tradition of map making, not anyone's artwork.

### The planet shader (`docs/mockups/lib/planet-shader.js`)

The owner, 2026-11-08, of Francisco Cidade's Pixel Planet Godot shader (https://store.godotengine.org/asset/fcidade/pixel-planet-godot-shader/,
CC0 1.0, inspired by Deep-Fold's Pixel Planet Generator): "use this to draw our planets in space ... but maybe less pixelated. More
cross hatching". Its source is a store download, not public, so this is our own code with its features, the asset named as the
inspiration in the file's header:

| Feature (the asset's) | Ours |
|---|---|
| A procedural world on one quad, no textures | The same, in three.js r147: a quad that always faces the camera, its foot at its origin |
| Six kinds: terran (seas, land, clouds), desert (dunes), ice, lava (dark rock, glowing rivers), gas giant (bands), moon (craters) | The same six, each with its own four-step palette |
| `pixels` across the planet (64 by default) | 220: still cut into cells, fine enough to read as drawn, not pixel art |
| `shade_steps` of flat light | Five bands, from the upper left as the lamp |
| Dithered shadow | Cross-hatching in sepia ink instead: a set of lines at 45 degrees from the second band, crossed at -45 from the third, horizontal from the fourth, vertical in the deepest |
| Clouds turning twice as fast as the ground, throwing shadows | The same |
| Polar caps, axis tilt, rings, atmosphere glow | The same; the ring in the holder's colour, its far half behind the disc, hatched where the planet's shadow falls; the atmosphere a thin washed rim on the lit side |
| | Coastlines, gas bands' edges and crater rims inked; the limb inked heavy with a wandering pen |

The noise is value noise over a 256 by 256 lattice of Mulberry32 numbers, read with the two-channel trick (one 2D lookup, two
layers of 3D noise), so there is no trigonometric hash (CLAUDE.md section 10) and it runs on WebGL 1. Each world is a few
hundred pixels of fragment work a frame; ten on the chart cost about what one textured sphere did.

## The sheet in 3D (`paperMesh`)

A plane of 150 by 130 quads lying 1.75 mm over the table, alpha-tested to the torn outline. Its corners lift 1.3 to 3.5 cm
where it has curled, its edges a little, and each fold is a ridge or a valley of 1.25 mm, never down into the wood.

## Cost

Four 1024 px maps for the carpet and the green wall each (albedo, normal, ORM, specular) and one for the paper, about 0.9 MB in
the page as WebP. The nebula is rendered once per chart at half the chart's size (1,300 by 1,120 px), a few milliseconds. The drawn pieces are about fifty small canvases (clouds 256 by 200 px, planets 256 px, suns 320 px, rocks 96 px), about 10 MB of texture memory, drawn once. The chart is one 2,800 by 2,440 canvas texture (27 MB of texture memory) and one shadowed spot light (a
2048 px map): fine on a desktop; in the game on phones it would be a 2048 px texture and a baked table shadow.

## In the game

The page stays the one design (CLAUDE.md section 4). `tools/mockups/export_chart.mjs` loads mockup 1 headless and writes
`assets/scenes/chart/`:

| File | What |
|---|---|
| `sheet_base.png`, `sheet_screen.png`, `sheet_over.png`, `sheet_mul.png` | `paperSheet` drawn layer by layer: the paper, print, stars, numbers and suns; the worn ink (screen); rhumb lines, compass, title and folds (over); frame, stains, foxing and the browned edge (multiply). 56 px a unit |
| `room.glb`, `paper.glb` | `buildRoom` and `paperMesh`, each textured material a named stand-in; `manifest.json` gives each name its Material Maker graph, tiling, tint and finish, and `GraphMaterial.Make` builds it from the graph's committed maps, so the maps have one copy |
| `drawn/*.png` | The bank of drawn pieces (`chartBank`): a cloud and a storm in three variants for each of the palette's 23 colours, three suns, twelve rocks, the cloud's shadow |
| `manifest.json` | The sheet's bounds, the lights (three.js r147 units; the game scales them by 2.4 by eye against the shots), each drawing's foot and aspect, the planets' kinds and palettes |

The page's map is built from its own random terrain and the game's from its star systems, so placements cannot be exported:
`ChartRoom.BuildDrawn` stands the bank's drawings on the game's own sectors by the page's rules, ported with the same Mulberry32
numbers drawn in the same order (a cloud by its region's palette index, one region in three a storm; a sun on each star with
its warm light; half a belt of rocks across each asteroid sector). What changes in play is drawn live by
`chart_sheet.gdshader` over the still layers: each charted hex's wash, outline and the heavier rule where the holder changes,
and the fog from a mask the CPU blurs once a turn (8 px a unit, two box passes) so a phone does no blur per pixel.

## Open questions

| # | Question | Provisional |
|---|---|---|
| 1 | Paper map in the game, or the mockup only for now? | Answered 2026-11-08: in the game ("implement space chart in game, it looks great!"); the glass board stays in the code behind `CampaignMap.Paper = false` |
| 2 | Keep the space board as an option (a setting), or retire it? | Kept in the page behind `?look=space` |
| 3 | Nebulae as printed washes only, or also their 3D clouds over the paper? | Answered 2026-11-08: drawn billboard clouds over the printed washes |
| 4 | Do storms do anything (a move cost, damage), or are they a look? | A look only, until the owner asks |
| 5 | Should the stations and ships be drawn too? | No: the owner likes the ships as models; stations stay models with them |
| 6 | Which kind of world is each planet? | Provisional, a look only, set in the page (`PLANET_KIND`): Halcyon and Calder terran, Meridian and Selene ice, Aster and Tessaly gas giants, Vahr and Ouro desert, Kesh Prime lava, Sabine a moon. Into `data/campaign/campaign.json` when the game draws them |
