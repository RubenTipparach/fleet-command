# Design: ssarith-ships-engine

## One copy of every ship rule

The Hunt's geometry is the low-poly hull the bake unwrapped (`tools/blender/painted_bake.py --ship`, the manifest's
`mesh` in `assets/ships/ssarith/manifest.json`), which the mockup page also draws. The exporter takes it from the page
(`MAPS[i].lowGeo`), not the page's own breakup copy (split to 2.5 m edges for the page's fracture, which hands each cell
the triangles near it): the game's fracture draws a whole copy of the hull per cell and keeps that cell's skin in the
shader (`dmg_break`), so it needs no splitting. The maps stay where the bake wrote them and the manifest names them, so a
rebake is picked up by the game with no export.

| What | How | Numbers |
|---|---|---|
| Axes | The exporter's half turn about Y (bow -Z, port -X), origin at the bounding box's centre, as every ship | |
| UVs | The bake's, v flipped (glTF's v runs down the image); the normal map is OpenGL, as Godot reads it | |
| Hull look | `baked_hull.gdshader`: albedo the base map, AO, roughness and metalness from ORM, the normal map, emission = the emission map + albedo * light^1.6 * light_k | light_k 1.4 (the page: 0.3 studio, 4.5 night) |
| Lamps | Strobes, beacons and position lights as the other lines' (OmniLight3D from the light empties); floods are baked (`light_baked_flood_*`, no light) | |
| Breakup | The page's fracture (lib/ship-break.js), exported as the others'; the cut faces' inside is the hull's field, a 3D texture of one byte a 1 m cell | 9 cells (under 120 m), 11, 13 (over 150 m) |
| Classes | Battle lengths 2.7 times the true length, as the other lines; figures and mounts placeholders at the Krai's sizes (frigate, cruiser), the carrier slower and tougher | frigates 297-313 m, cruiser 405 m, carrier 455 m in the battle |

## Open questions

| Question | Provisional answer |
|---|---|
| The Hunt's weapons | The generic ones (lances, torpedoes, point defence, missiles) until the culture is set (survey H1) |
| Its ships' names and captains in the scenarios | Their class names and "Hunt captain" (culture is the owner's, C4) |
| The `.bin` fields in an exported build | Read by `FileAccess`; an export preset must include `*.bin` (no preset in the repository yet) |
