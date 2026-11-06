# Ship schematics

Every ship seen from directly above, nose up, as a quad wireframe over a darker hull: the systems display's ship
picture, for the battle console and for the store's preview of a ship being bought or outfitted (owner, 2026-11-05).

| File | What it is |
|---|---|
| `<faction>/<id>.hull.png` | The filled hull, lit from above by its faces' normals. 1024 px square |
| `<faction>/<id>.wire.png` | The quad edges seen from above (hidden edges left out) and the outline, anti-aliased. 1024 px square |
| `manifest.json` | For each ship: its two files, pixel size, metres per pixel, the pixel the glb origin falls on, the axis convention, quad and triangle counts, and the source glb with its sha256 |

## The layers

Both layers are pure white RGB; the drawing is in the alpha channel. A skin tints each one with its own colour and
lays them together: the hull tinted with a dark skin colour, then the wire tinted with a bright one on top. The hull's
alpha stays between 0.15 and 0.55, so it always reads darker than the wire (alpha up to 1).

## Placing things over the picture

A glb point (x, y, z), in metres, falls at pixel `(origin_px[0] + x / m_per_px, origin_px[1] + z / m_per_px)`, with
pixel (0, 0) the image's top-left corner. Image right is +X (starboard), image down is +Z, so the bow (-Z) is at the
top. Height (y) is not drawn. So a weapon mount's empty from the glb can be marked on the picture directly.

## What made them

`tools/mockups/ship_schematics.py` reads each glb in `assets/ships/concord/`, `assets/ships/krai/` and `assets/ships/ssarith/`, welds its
vertices, pairs its triangles back into quads (near-coplanar, convex, best pairs first), and renders both layers at
4x and scales them down. It also writes the contact sheet `docs/screenshots/schematics/sheet.png`: each ship's hull
layer, wire layer, the two recombined in three tints, and a 200 px copy. Look at it after a change.

```sh
python3 tools/mockups/ship_schematics.py
```

Re-run it whenever `tools/mockups/export_ships.mjs` re-exports the ships: a schematic is stale when its source glb's
sha256 no longer matches the manifest. The run is deterministic; a second run gives byte-identical files.
