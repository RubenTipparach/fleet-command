# Design: paper-map

## The room, at scale

One scene unit is 5 cm here, so a hex is 10 cm across and the chart about 2.5 m by 2.2 m (30 by 22 hexes), a big wall poster
laid flat. It lies on a walnut table 3.1 m by 2.8 m, 0.75 m high (`walnutTable`, the faction screen's table builder: the
`table_wood` graph, a tile to 1 m). The room is 8.5 m square, its floor a deep red cut-pile carpet (`carpet_red`, a tile to
0.5 m), its walls 2.8 m high papered in light red-orange roses on cream (`wallpaper_floral`, four roses to a 0.5 m tile) over
a dark wooden skirting board. The map's camera keeps its fixed angle; zoomed in, the chart fills the view; zoomed out, the
table, the carpet and the papered walls show round it.

Light: a warm lamp hanging 1.6 m over the table (a spot light, its shadows on: the miniatures throw shadows on the chart, the
chart on the table), a warm fill from the room (hemisphere, cream above, red below), a weak key from one side. What the hulls,
planets and stands reflect is the room's colours (a small probe: warm walls, a red floor, a bright ceiling).

## The chart (`paperSheet`)

Drawn into one canvas at 56 px to the unit (2,800 by 2,440 px) from the campaign's own map, seeded by the side played, so a
campaign's chart is the same every time it is drawn:

| Layer | What it is |
|---|---|
| Paper | The `map_paper` graph (aged cream, mottled, fibres, foxing), a tile to 0.25 m |
| White border | 1.8 units (9 cm) round the print, kept pale; the print inside it warmed a little |
| Territory | Each held hex washed faintly in its holder's colour |
| Grid | Every hex outlined in its holder's colour (unclaimed space in brown ink); a heavier rule inside each hex along every edge where the holder changes, as the glass board's ribbons did |
| Terrain | Nebulae as watercolour washes in their cloud's main and accent colours (`nebulaRegions`); asteroid belts stippled; stars as printed suns |
| Numbers | Each hex's column and row (`0412`), small, at its top |
| Frame | A double rule round the print; the title "A CHART OF THE MARCHES" and a line under it naming the side it was surveyed for; a five-sector scale bar |
| Wear | Ink rubbed pale in patches and specks; three folds across and one down, rubbed pale with a shadow along them; two coffee rings, water stains, foxing thickest toward the edges |
| Edge | Frayed; six tears running in toward the print; one corner torn away; browned in toward the border and crusted dark at the tear |

The nebulae's 3D clouds, the glass hexes' look, the board's mirror and the space sky are not drawn on the paper map; the glass
hexes stay, unseen, as what the pointer picks. Planets, stations, ships, banners, freighters, the stars' suns and the belts'
rocks stand on the chart as before (the rocks dark, as pebbles).

## The sheet in 3D (`paperMesh`)

A plane of 150 by 130 quads lying 1.75 mm over the table, alpha-tested to the torn outline. Its corners lift 1.3 to 3.5 cm
where it has curled, its edges a little, and each fold is a ridge or a valley of 1.25 mm, never down into the wood.

## Cost

Three 1024 px maps for the carpet and the wallpaper each (albedo, normal, ORM, specular) and one for the paper, about 0.9 MB in
the page as WebP. The chart is one 2,800 by 2,440 canvas texture (27 MB of texture memory) and one shadowed spot light (a
2048 px map): fine on a desktop; in the game on phones it would be a 2048 px texture and a baked table shadow.

## Open questions

| # | Question | Provisional |
|---|---|---|
| 1 | Paper map in the game, or the mockup only for now? | The mockup only until the owner asks (sync task `game-flow-engine` 2.10) |
| 2 | Keep the space board as an option (a setting), or retire it? | Kept in the page behind `?look=space` |
| 3 | Nebulae as printed washes only, or also their 3D clouds over the paper? | Printed only |
