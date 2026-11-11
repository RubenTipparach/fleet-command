# Ship schematics

Every ship seen from directly above, nose up, as a quad wireframe over a darker hull: the systems display's ship
picture, for the battle console and for the store's preview of a ship being bought or outfitted (owner, 2026-11-05).

| File | What it is |
|---|---|
| `<faction>/<id>.hull.png` | The filled hull, lit from above by its faces' normals. 1024 px square |
| `<faction>/<id>.wire.png` | The quad edges seen from above, the ones the hull hides drawn at 0.4 alpha as if through glass (owner, 2026-11-07: "the schematic should be semi transparent"), and the outline, anti-aliased. 1024 px square |
| `manifest.json` | For each ship: its two files, pixel size, metres per pixel, the pixel the glb origin falls on, the axis convention, quad and triangle counts, and the source glb with its sha256 |

## The layers

Both layers are pure white RGB; the drawing is in the alpha channel. A skin tints each one with its own colour and
lays them together: the hull tinted with a dark skin colour, then the wire tinted with a bright one on top. The hull's
alpha stays between 0.1 and 0.38, so it reads as tinted glass, darker than the wire (alpha up to 1), with the hidden edges showing through it.

## Ships whose detail is in their bake

The Kelvane Institute's hulls are smooth grown structures; their plating, machinery, domes and vents are modelled over
them in Blender and baked into each ship's own maps (`tools/blender/painted_bake.py`), not cut into the glb. So their
wire layer takes that detail from the bake (owner, 2026-11-08: "Top down shop sillohuet needs improvement. Let's add some
details so the top down view of these kelvane ships don't look so bad"): each pixel's top surface is rasterised with its
UV, the ship's tangent normal map and ORM (its occlusion) are looked up there, and a texel that tilts or darkens more than
the plating around it (a plate's bevel, a gap, a dome's rim, a vent's slats) is drawn, up to 0.85 alpha. A smooth tilt or
soft shading is not. Of their mesh edges only the outline and the real creases (over 30 degrees) are drawn: the loft's
rings and facets are tessellation. The faction list is `DETAIL_FROM_BAKE` in the generator.

## Placing things over the picture

A glb point (x, y, z), in metres, falls at pixel `(origin_px[0] + x / m_per_px, origin_px[1] + z / m_per_px)`, with
pixel (0, 0) the image's top-left corner. Image right is +X (starboard), image down is +Z, so the bow (-Z) is at the
top. Height (y) is not drawn. So a weapon mount's empty from the glb can be marked on the picture directly.

## What made them

`tools/mockups/ship_schematics.py` reads each glb in `assets/ships/concord/`, `krai/`, `ssarith/` and `kelvane/`, welds its
vertices, pairs its triangles back into quads (near-coplanar, convex, best pairs first), and renders both layers at
4x and scales them down. It also writes the contact sheet `docs/screenshots/schematics/sheet.png`: each ship's hull
layer, wire layer, the two recombined in three tints, and a 200 px copy. Look at it after a change.

```sh
python3 tools/mockups/ship_schematics.py
```

Re-run it whenever `tools/mockups/export_ships.mjs` re-exports the ships: a schematic is stale when its source glb's
sha256 no longer matches the manifest. The run is deterministic; a second run gives byte-identical files.
