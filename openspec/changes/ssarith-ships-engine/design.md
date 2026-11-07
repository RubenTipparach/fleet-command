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
| Glows | Each Hunt lamp's `glow_m` (the page's sprite width: strobe 3.2 m, beacon 3.0 m, position 2.6 m) becomes a camera-facing quad (`lamp_glow.gdshader`), its colour scaled to at most 1 so the battle's bloom (above 1) leaves it a soft dot; on the lamp's empty, since a light's global scale is 1, not the ship's; a strobe's glow flashes with its light, a beacon's pulses, a broken-off piece's goes out. None at the floods | 3.2, 3.0, 2.6 m |
| Windows and bays | One quad each from the window_* and bay_* empties (centre, normal, up, across, glass size, frame width), 0.05 m off the plating, drawn by `pane.gdshader`: the frame dark metal under the battle's light, the glass the room or hangar by interior mapping (`ship_rooms.gdshaderinc`, the page's lib/ship-rooms.js ported), level with the decks; the hull's holes and burns take a pane with them, a broken cell keeps its own panes, a piece that breaks away goes dark. The port's hash is PCG, not the page's fract(sin) (CLAUDE.md 10), so which furniture a window shows can differ from the page | 0.05 m stand-off |
| Breakup | The page's fracture (lib/ship-break.js), exported as the others'; the cut faces' inside is the hull's field, a 3D texture of one byte a 1 m cell | 9 cells (under 120 m), 11, 13 (over 150 m) |
| Classes | Battle lengths 2.7 times the true length, as the other lines; figures and mounts placeholders at the Krai's sizes (frigate, cruiser), the carrier slower and tougher | frigates 297-313 m, cruiser 405 m, carrier 455 m in the battle |

## Open questions

| Question | Provisional answer |
|---|---|
| The Hunt's weapons | The generic ones (lances, torpedoes, point defence, missiles) until the culture is set (survey H1) |
| Its ships' names and captains in the scenarios | Their class names and "Hunt captain" (culture is the owner's, C4) |
| The `.bin` fields in an exported build | Read by `FileAccess`; an export preset must include `*.bin` (no preset in the repository yet) |
| Its general scenes (2026-11-07) | Each the Concord's scene with the Hunt's ships in the Concord's places: the Hunt Cruiser the captain's ship, the Trident for the destroyer, the Hooded Lance carrying troops, the Forked Prow in distress, Pell Reach (its campaign station) the depot, on the mockups' station |
| Its mines | The Concord's by size: 2 a frigate, 4 the cruiser, 2 the carrier (H1) |
