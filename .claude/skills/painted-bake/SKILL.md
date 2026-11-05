---
name: painted-bake
description: Make Fleet Command hull textures that read drawn on, the way late-1990s ship textures were painted over their renders - model a high-poly armour tile (or a ship's own panels) in Blender, render its occlusion, normals, edges and per-plate ids face-on in Cycles, then paint over those passes in numpy (flat colour per plate from a palette, a painter's light in soft steps from the upper left, lit and shaded bevels, inked crevices, brush grain, glowing strips) into albedo, normal and glow maps; and in ship mode, the binding way to texture a ship, unwrap each ship on its own and bake its AO, plating, markings and lit ports onto its own UVs as its main colour map, with any tile laid over it as detail only. Use whenever a ship should look painted, hand-drawn or "like the old games", when a texture "looks too clean", "looks like a render", "needs to look drawn on", or when baking high-poly AO detail into a hull texture, or when a ship needs its own UV-unwrapped texture or AO bake ("unwrap the ship", "custom UVs", "bake AO", "painted texture", "drawn on", "baked AO", "hand painted hull", "like starfleet command").
---

# Painted bake

The owner, 2026-11-05, with screenshots of the reference game's ships: "make ships like this, with this kind of texture",
"might need a new skill to make high -poly AO baked texture that looks drawn on". This is that skill, written in this
repository (provenance: `.claude/skills/PROVENANCE.md`). It sits beside `panel-textures` (which renders physically
based maps from a modelled panel) and borrows its kit (`tools/blender/panel_kit.py`): the difference is the last step,
where the passes are painted over instead of used as they come.

**Take the look, never the art** (CLAUDE.md section 1). The reference images are someone else's and are never committed
or traced. What is taken is how such a texture is made and how it reads: the light is part of the paint, crevices are
dark, edges catch the light, plates are flat colour, light strips glow.

## How it reads, and why

A painted texture carries its own light, so it reads at a distance and in any lighting:

| Look | From which pass | Rule |
|---|---|---|
| Flat colour per plate | `rid` (a random per object) | A palette of 4-6 close tones from the faction's colours, one rare accent (bronze, rust); never a gradient across a plate |
| Painted light | the Normal pass | Upper-left light, a little toward the viewer, in 4 soft steps blended with the smooth value (0.55 smooth, 0.45 stepped): reads painted, not posterised |
| Dark crevices | the AO pass, reaching about 0.45 m at hull scale | Curved (power 1.6), then inked to near black where it is deepest; blurred one pixel first so the render's speckle never shows |
| Lit and shaded edges | `edge` (the bevelled normal against the true one) | Lit edges toward a pale highlight, shaded edges darker: the line a painter draws round a plate |
| Brush grain | noise | Faint streaks along the ship (6 %) and a coarse mottle (5 %): enough that a flat is not machine flat |
| Glow | the Emit pass | Its own paint plus a soft glow map |

## The method

1. **Model the tile** in `tools/blender/painted_bake.py` (`TILES`): real pieces from the panel kit (`P.poly`, `P.box`,
   `P.tube`, `P.cyl`), raised 5-20 cm with bevels of 3-6 cm so the edges have something to catch. Lay it so it repeats:
   nothing crosses an edge except runs that span it whole (a spine, a conduit), and seams fall half on each edge.
2. **Convex pieces only.** A concave outline (a chevron in one piece) bevels badly and its filler overlaps: build it from
   convex parts that meet edge to edge. Two coplanar faces overlapping speckle black in the AO.
3. **Render** (`bake`): orthographic, face-on, 32 samples, 128 px per metre; the passes as 32-bit EXR into
   `tools/materials/raw/painted/` (gitignored); every hull piece shares `pass_material` (AOVs `edge` and `rid`), light
   strips take its emissive version.
4. **Paint** (`paint`): the rules in the table above, in that order: base, light, crevices, ink, edges, grain, glow.
5. **Write** `assets/textures/painted/<tile>_{albedo,normal,emit}.png` and the manifest (size in metres, sha256 of each).
6. **A tile is detail only** (owner, 2026-11-05: "ALL SHIPS MUST AVOID REPETTIVE TILE FOR MAIN COLOR MAP", "tile textures
   are for DETAIL only"). A ship's main colour is its own painted map (ship mode, below); a tile only adds fine detail
   over it. The steps below are how a tile is laid as that detail, or on a blockout that has no unwrap yet.
7. **Lay it on the ship**: inline the manifest (`tools/mockups/inline_data.py` carries each map as WebP); in the page the
   albedo multiplies the base colour, about a third of it shows as emission (the paint's own light reads in shadow), the
   normal map takes the live light, the glow map adds to emission. On a ship built of primitives with no UVs, project from
   three sides in world space; on a ship with its own parts, lay the sheet on each part.

```sh
/opt/blender/blender -b --python tools/blender/painted_bake.py -- --tile hunt_chevron --samples 32 --ppm 128
python3 tools/mockups/inline_data.py docs/mockups/<page>.html
```

## Judge it

Look at the tile (albedo beside its normal map, and tiled 2 x 2) and at the ship close up and at board distance:

- Does it read painted, not rendered and not a photo? Flat plates, light in the paint, dark crevices, lit edges.
- **Scale**: at board distance a repeat must not read as a pattern. A 100 m ship wants features of a few metres (the
  Hunt's chevrons are laid at 2.5 times their bake, about 5 m across); a tile repeating a dozen times down a hull reads as
  camouflage, not plating.
- Speckle in the shadows means overlapping faces or too few samples.
- No z-fighting where a strip or plate meets the hull (CLAUDE.md 10).

## Pitfalls hit

| You see | Cause | Fix |
|---|---|---|
| Black speckle in a plate's shadow | Two coplanar pieces overlapping (a chevron's filler under its arms) | Convex pieces meeting edge to edge; blur the AO one pixel before painting |
| Chevrons flat in the normal map | One concave outline: the bevel modifier skips its edges | Build each chevron as two convex arms |
| The hull reads as busy camouflage | The tile laid at its bake size, repeating 12 times down the hull | Lay it larger; next, a painted sheet per ship that follows its parts |
| Angled faces mottled | Three-side projection blending on 45 degree faces | Lay the sheet on each part along its own surface (the Hunt page's version 6 did this for lofted hulls) |

## Ship mode: every ship its own unwrap, colour and AO

The owner, 2026-11-05: "every ship needs custom uv Unwraped textures. From NOW on . ALL SHIPS MUST AVOID REPETTIVE TILE FOR
MAIN COLOR MAP", "tile textures are for DETAIL only, we'll probably need a detail mask in the future", "Also dont forget to
bake AO as well". The rule is in CLAUDE.md section 10 and the Ship Design Bible, chapter 15 (T1-T4). This is the binding way
to texture a ship; the tile steps above are for detail.

1. **Export the ship from its page** as OBJ in ship space (+x port, +y up, +z bow, metres), one object per named part,
   light strips as `glow_*` objects: `NODE_PATH=$(npm root -g) node tools/mockups/export_blockouts.mjs` (the page's
   `MOCKUP_EXPORT_OBJ`), into `tools/materials/raw/painted/ships/` (gitignored).
2. **Unwrap** in Blender: import as it is (forward Y, up Z, so nothing is converted), the light strips' faces given
   their own material, join, Smart UV Project (60 degrees), then pack the islands again by shape with rotation
   (`pack_islands`, CONCAVE) so the texels go on the hull.
3. **Build the detail over it** (`tools/blender/hull_detail.py`, laid out in `data/ships/<faction>/hull_detail.json`;
   owner, 2026-11-05: "vents, lights, and sub armor pieces, and primary armor peices", "liveries, smooth sphereical
   portions of armor, and specifically shaped vents", "no repeating elemnts, single axis symmetry, and lots of shading"):
   - the under layer: a copy of the hull, dark, its strips kept;
   - primary armour: the hull's faces grouped by facing, thickened 0.5 m outward (Solidify) and cut apart by one Exact
     boolean against gap slabs (top and bottom: a kinked spine and swept chevrons, every station its own spacing and
     sweep; flanks: raked cuts), the data's exposed areas and every feature's hole; split into plates, each raised by its
     own amount, the big ones with a smaller plate laid over part of them (cut by a raked line, drawn in, lifted);
   - sub-armour: a thinner shell (0.22 m) with its own cuts and wide gaps;
   - greebles only where both armour layers leave the hull bare (a ray up from the hull finds no armour);
   - domes (a sunk sphere in a collar ring, straps that hug it), fans (recess, hub, tilted fins, lamp ring), louvres
     (chevron or raked slot outlines cut through, slanted slats clipped to the outline), lamps;
   - everything off the centreline built on the port side and mirrored with a reflected frame, and every piece's `rid`
     a hash of its mirrored position, so the ship has one axis of symmetry and nothing repeats.
   Join the detail into one object before baking: the bake's setup is per object, and 600 pieces cost minutes a pass.
   Bake it onto the hull's UVs, selected to active (cage 1.3 m, ray 2.8 m), the hull itself hidden from rays: a data pass
   (kind, rid, Bevel-node edges), glow, position, object-space normals, AO (2.2 m) and a low sun's direct light with its
   cast shadows (DIFFUSE, direct only).
4. **Paint** (`paint_ship`): each texel by its kind (armour from the palette by rid, sub-armour greyer, the under layer
   near black, greebles, the dome's green, slats, vents), liveries from the position, then the sun's light in soft steps
   over a low ambient from the AO, the AO curved and inked, each plate pillowed darker toward its gaps, lit and shaded
   edges, the dome's highlight, grain, and the lamps' glow; the light strips a dark channel with a lit line.
5. **Write** `assets/ships/<faction>/<ship>_{albedo,ao,emit,normal}.png` and the manifest (the UV mesh, `uv_m` metres of
   hull per UV unit, sha256 of each map; the AO is kept beside, not inlined, as it is already in the albedo). Inline it
   into the page, which shows the UV mesh with its own map, its glow and its object-space normal map, and no tile.

```sh
NODE_PATH=$(npm root -g) node tools/mockups/export_blockouts.mjs
/opt/blender/blender -b --python tools/blender/painted_bake.py -- --ship hooded_lance,forked_prow,harpoon,trident --px 2048 --samples 64
python3 tools/mockups/inline_data.py docs/mockups/ssarith-ship-concepts.html
```

Judge it on the page, close up and at board distance: plates read as plating that follows the form, not bricks; markings
read; no seam shows as a hard colour jump; the AO darkens the joints and the crevices between branches.

| You see | Cause | Fix |
|---|---|---|
| Hundreds of tiny islands and mostly empty texture | A hull of stuck-on primitives, every face at an angle to its neighbour | Grow the hull as one structure (Ship Design Bible chapter 14); pack the islands again by shape |
| The hull reads as brickwork | Plates all one length, joints staggered by half | A plate length and an offset per course, from a hash |
| A flat olive hull | Only the per-part paint, nothing laid over it | The modelled detail of step 3 |
| Every gap between plates glows | A mesh copy's materials cleared: Blender zeroes every face's material index | Read which faces are strips before clearing |
| Each bake pass takes minutes | Hundreds of separate objects selected to active | Join the detail into one object first |
| The detail is too fine to read at ship scale | Hairline gaps, small features | Trenches of 2.6-4.6 m in about 40 % of the cuts, domes and fans of 4-6 m, a second plate over the big ones |
