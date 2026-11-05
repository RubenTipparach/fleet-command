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
MAIN COLOR MAP", "tile textures are for DETAIL only", "Also dont forget to bake AO as well". A ship's own texture is now its
own skill, **`ship-detailing`** (`.claude/skills/ship-detailing/SKILL.md`): growing the hull, its unwrap, the modelled
detail layer (`tools/blender/hull_detail.py`), the bake and the paint (`tools/blender/painted_bake.py --ship`, which uses
this skill's paint-over for its painted map), the PBR set and the hull lights. This skill keeps the tile method, for
detail.
