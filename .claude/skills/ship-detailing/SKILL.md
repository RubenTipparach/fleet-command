---
name: ship-detailing
description: Detail and texture a Fleet Command starship from a recipe written out of its faction's culture. Read the faction's accepted culture, set down how its makers build (its principles), and compose the ship's recipe from a palette of reusable techniques (hull form, plating layout, relief, exposed machinery, fasteners and frames, features, paint and liveries, wear, light, hull lights), every feature placed from the ship's own rooms and the whole mirror-symmetric. Then model the high-poly detail over the ship's own unwrap in Blender, render it and squash it onto the ship's own UVs (the bake), and write the PBR set (base colour, ORM, tangent normals, emission, light map) and a painted map. Use whenever a ship needs detail, texturing, UVs, AO, armour, greebles, vents, windows, a logo, hull lights, PBR maps or normal maps, whenever a new faction's ships are textured, or when a hull "looks plain", "looks like primitives", "has no distinct features", "repeats", "is too flat", "is not symmetric", "looks like black lines" ("detail the ship", "texture the ship", "custom UVs", "bake the hull", "armour plates", "greebles", "hull lights", "PBR", "normal map", "logo on the hull").
---

# Ship detailing: techniques, composed by each faction's culture

The owner, 2026-11-05, on the Ssarith Hunt ships this skill was first written from: "Now those my friend are gorgeous
looking star ships", "write up a skill for detailing space ships with custom texturing". Then, 2026-11-06, when the Hunt's
own process was laid on the Concord and Krai and failed: "YOU CANT JUST USE THE CULTURE DEVELOPED FOR ONE FACTION AND
SLAP THE SAME SKILLS ON TO OTHERS", "THE SKILL SHOULD BE THE ABSTRACT NOT THE PROCESS", "the actual process for authoring
textures MUST TAKE INTO ACCOUNT THE CULTURE", "THE TECHNIQUES are a pallete that you can reuse between different ships".
So this skill is in three parts: **the rules every ship keeps**, **the method** that turns a culture into a recipe, and
**the palette of techniques** a recipe picks from. The Hunt's recipe is one example of the method, not the method.
Provenance: `.claude/skills/PROVENANCE.md`. The rules it serves are CLAUDE.md sections 1 and 10 and the Ship Design
Bible (`docs/design/ship-bible.html`): chapters 7 (windows), 9 (hull lighting), 10 (markings), 13 (culture, not anatomy),
14 (one structure, F1-F5) and 15 (its own texture, T1-T8).

## The rules every ship keeps

1. **Its recipe comes from its culture.** Never reuse another faction's recipe, palette, plating or wear. Read the
   faction's accepted culture first (survey U7, its console-skin and emblem designs); where it is thin, ask (CLAUDE.md
   section 1). Each choice in the recipe names the principle and the tradition it comes from.
2. **Symmetric** (owner, 2026-11-06: "these empires SHOULD BE SYMMETRIC, they are powerful, proud and have a strong
   culture"). One axis, the keel line: everything off it is built on the port side and mirrored, plate for plate, colour
   for colour, window for window. A room the plan puts on one side gets its windows on both, or the feature moves to the
   centreline; a bay is a pair or sits on the keel. Asymmetry only where the culture asks for it, by name.
3. **Render, then squash** (owner: "the process is: render high poly model, and squash that down into a texture we can use
   on the ship"). The light a texture wears is a render of the high-poly detail: a sky all round, a key light with its
   shadows, and their bounce into the gaps (Cycles, direct and indirect), baked onto the ship's own UVs and multiplied
   with the flat paint. No drawn lines, no ink pass, no painted steps on a new faction.
4. **No black** (owner: "Black is not a good color here, because it doesnt look like the greebles on the ship are casting
   shadows or AO. It looks like you just sketched random BLACK lines"). A gap, a trench, a greeble's foot is as dark as
   the light that reaches it, in the hull's own palette; the layer beneath the plates is a colour, never #000.
5. **Its own UVs, nothing repeated, read at ship scale** (T1-T8): no tile as the main colour map; no plate, vent or
   feature a copy of another (its mirror excepted); seams, plates and features sized so they read from the battle camera.
6. **Order, not noise.** Joints land on the hull's own structure (frames, decks, chines), not at random; a line that
   ends nowhere, a seam that breaks off mid-plate, a notch that serves nothing is a mistake (owner: "there's so much mess
   and mistakes").
7. **Not from the body** (C1-C5) and **one structure** (F1-F5).
8. **No face without detail** (owner, 2026-11-11, of a Vos bow cap: "I exepect greebles and antena here", "there should be
   noface that is completely devoid of detail"; Ship Design Bible T9). Bow and stern faces, caps, collar ends and deck edges
   take no plating (`shell_groups` leaves them bare), so give them machinery (`greebles_per_m2`, which lands only where the
   plating leaves the hull bare), antennas (`fixtures` kind `antenna`), lamps or features. Orbit the bake all round before
   calling it done.

## The method: culture, then principles, then the recipe

1. **The culture.** Quote the faction's accepted words (and mark what is provisional).
2. **Its principles**, three to six lines of *how this people builds*: what they value, and how that shows in a hull.
   For example "edge first: a ship reads by its edges", "light as material: a ship shows itself by light", "kept and added
   to, never replaced".
3. **The recipe.** For every row of the palette below, a setting or "none", each citing its principle. Write it in the
   faction's OpenSpec design and lay its numbers out in `data/ships/<faction>/hull_detail.json`.
4. **The story**, from the ship's room plan (`data/ships/<faction>/<ship>_plan.json`): where its people live (windows),
   where it breathes (vents), sees (sensors), docks (bays), fights (muzzles) and runs (drives), mirrored.
5. **Test one ship** at 1024 px, judge it against the recipe close up and from the battle camera, in both lights; only
   then bake the line. Several designs of one faction are several recipes over one story.

## The palette of techniques

| Technique | What it reads as | Where | Settings |
|---|---|---|---|
| Hull form: grown skin | One body with branches split from its own section (the Hunt) | the ship's page (`Skin`, `hsec`), `export_blockouts.mjs` | section layout, branches |
| Hull form: lofted line | A line's lofted hull (the Concord, the Krai) | the line's builder, the game's glb through `tools/mockups/glb_hull_obj.py` | as built |
| Plating: chevrons | Swept chevrons and raked flank cuts, a kinked spine | `hull_detail.py` `armour_cuts` (`cut_style` "hunt") | stations, sweep, spine |
| Relief: layered armour | Plates standing proud by their own amounts, overlays stepping up (a smaller copy of a plate over it: the Hunt only, never with windows or features on the plate: owner, "why is there two armor plates ontop of eachother?"), a sub-armour layer, trenches | `build_armour`, `overlay` | `armour_t_m`, `lift_m`, `overlay_*`, `sub_t_m`, `trench_*` |
| Relief: flush skin | One continuous skin, its plates told apart by narrow seams only | the same, `lift_m` near 0, thin `armour_t_m`, narrow `gap_m`, no trenches | |
| Exposed machinery | Machinery where the ship's systems run: trenches, service bands, round the drives | `exposed` zones, `greebles` | `greebles_per_m2`; never over the whole hull |
| Plating: large panels and sub panels | Large panels between the room plan's stations, decks and walls (each seam "major"), each split into sub panels at their own places, cut again at the hull's chines; a clean hull | `panel_cuts`, `kit_split`, `chine_slabs` (`cut_style` "panels") | `panel_merge_m`, `station_sweep`, `sub_depth`, `sub_stop`, `kit_min_m`, `major_gap_m`, `chine_deg` |
| Relief: chamfered edges | Every plate's edge rolled a little, so it catches the light (narrow for a crisp hull, broad for forged plate) | `bevel_m` (the Bevel node's radius by kind) | Concord 0.05 m, Krai 0.1 m |
| The detail kit | Distinct pieces over the plating, mirrored, kept off bends and off one another: escape hatches and access panels set into the plating, vents, lit slots, small ports, thin grooves, tags, thruster quads, accent modules, missile and bomb tubes | `scatter_kit`, the `k_*` pieces | `kit`: `per_100m2`, `weights`, `lights`, `gap_m` |
| Ordered machinery | Rows of built machinery in an opened bay: blocks, pipe runs, slatted boxes, tanks, lit panels, accent blocks, one a grid cell | `well_modules` | `well_cell_m` (with `exposed` zones) |
| Bolt wells | Pill-shaped wells indented into a plate's corners (set diagonally) and across its long edges, where the bolts hold it down; never rivet rows | `rivets` | `rivets`: `inset_m`, `pill_l_m`, `share`, `slot_edge_m` |
| Paint: marks | Warning corner brackets, arrows, hazard stripes, chevrons, bars, boxes, warning bands, names and numbers in a block font | `paint_decals`, `decal_shape` (`DECALS` from the kit, `decals` in data) | `DECAL` colours |
| Wear: surface | Shallow dents and fine fore-and-aft scratches in the high-poly's own shading, so the render and the normal map carry them | `WEAR_BUMP` in `kind_material` | `wear_bump` by kind |
| Wear: chips and grime | Paint chipped on the edges and specked on the faces (a lattice hash, no repeats), grime in the cavities | `specks`, `hash3`, `paint_render` | `CHIPS_K`, `DIRT`, `DIRT_K` |
| Features | Domes, fans, louvres, lamps, bays, windows, the emblem | `hull_detail.py` features, from the story | sizes, places |
| Paint: plates | One colour a plate from a palette; or by edge (temper), by section, by generation | `painted_bake.py` `flat_paint`, `plate_colours` | `palette`, `plates` |
| Paint: liveries and marks | Bands, sash, stripe, fields, star, honour edges, tallies, hazard frames, chevrons | `livery_paint` | `liveries` |
| Wear | Soot from vents and drives, scorch at muzzles, scuffs round bays | `wear` (the story list) | `wear` |
| Light: render | The high-poly rendered under sky, key and bounce and squashed onto the UVs | `paint_render` (`PAINT_MODE` "render") | `SKY_RGB`, `SKY_K`, `AO_K` |
| Light: painted | A painter's light in soft steps, inked crevices (the Hunt's own art style) | `paint_ship` (`PAINT_MODE` "painted") | the Hunt only |
| Glow | Light strips, lit gaps, lamps | `strips_and_glow`, `UNDER_GLOW` | |
| Hull lights | Floods grazing the plating, logo flood, nav lights, the light map | `lights`, the light bake | `flood`, `auto_floods`, `lighting` |
| Panes | Windows and bays drawn as quads with rooms and hangars behind | the bake's `panes`, the page's pane mesh | `pane_frame_m` |

## Recipes

- **The Ssarith Hunt** (mockup 12, change `ssarith-ship-concepts`): grown skin; chevrons; layered armour with deep
  trenches of machinery; strapped domes, fans, shaped louvres, the eye lamp; olive and khaki plates, green liveries; the
  painted light. Its numbers are in the tables of the tools below and in `data/ships/ssarith/hull_detail.json`.
- **The Concord and the Krai Warhost** (mockup 35, change `hull-texture-designs`): each written from its own culture in
  that change's design, "Recipes from culture".

## The tools, step by step

What follows is how each tool works, first built for the Hunt. Where it gives numbers, they are the Hunt's recipe.

### The pipeline

| Step | Where | Writes |
|---|---|---|
| 1. Grow the hull | the ship's page (`Skin`, `hsec` in `docs/mockups/ssarith-ship-concepts.html`) | the low-poly hull (300-450 triangles) |
| 2. Export it | `tools/mockups/export_blockouts.mjs` | `tools/materials/raw/painted/ships/<ship>.obj` (gitignored) |
| 3. Lay out its detail | `data/ships/<faction>/hull_detail.json` | where every dome, vent, lamp, window row, flood and logo sits |
| 4. The emblem | `tools/mockups/emblem_masks.mjs <faction>` | `assets/emblems/<faction>/hull_mask.{png,json}` from `docs/mockups/lib/emblems.js` |
| 5. Unwrap, build, bake, paint | `tools/blender/painted_bake.py --ship` with `tools/blender/hull_detail.py` | `assets/ships/<faction>/<ship>_{albedo,base,orm,normal,emit,light,ao}.png` and `manifest.json` |
| 5a. Iterate fast | `painted_bake.py --ship <slug> --repaint` (paint only, about 40 s, no Cycles); `--px 1024 --samples 16 --quick` (a bake without the breakup field, most of a large ship's time) | colour, seam and AO changes in seconds; plate cuts and gaps in minutes, judged on the page. (A `--preview` render of the high-poly was planned here but never built.) |
| 6. Show it | `tools/mockups/inline_data.py` on the page (a manifest with `maps_dir` writes its maps beside the page as WebP files, published with it, for 4096 px maps) | the page's Painted, PBR and Clay looks under Studio or Night light |

```sh
NODE_PATH=$(npm root -g) node tools/mockups/export_blockouts.mjs
NODE_PATH=$(npm root -g) node tools/mockups/emblem_masks.mjs ssarith
/opt/blender/blender -b --python tools/blender/painted_bake.py -- --ship hooded_lance --px 1024 --samples 16   # iterate
/opt/blender/blender -b --python tools/blender/painted_bake.py -- --ship hooded_lance,forked_prow --px 2048 --samples 32   # commit
python3 tools/mockups/inline_data.py docs/mockups/ssarith-ship-concepts.html
```

A full bake costs about 12 minutes a ship at 2048 px on 4 cloud cores, so bake at most two ships per background job (a
job is stopped at 30 minutes). Iterate at 1024 px and 16 samples (about 1.5 minutes a ship).

### 1. Grow the hull as one structure

The low-poly hull is the silhouette and the thing the game hits. It is one closed skin (Ship Design Bible F1-F5):

- **One section layout.** Every station of the body is the same 18 points, counter-clockwise from the keel's bottom
  centre: keel corner, keel root, bottom chine, lower side, side chine (it can swell out), hood root, hood edge, hood top,
  crown, and their mirrors (`hsec`: `hb`, `top`, `bot`, `ch`, `sw`, `hw`, `ht`, `kd`, `kw`, `cr`). A feature a station
  lacks lies flat in its neighbours' line, so every station lofts to every other point to point.
![the Trident's bow section and its four regions](images/00-section-split.png)

- **Branches split from the body's own section.** At the bow the last section is cut along lines (`cutLine`, `seg`) into
  regions; each region lofts on as a named branch (`K.branch(name, region, z, [[dz, sx, sy, dx, dy], ...])`), scaled and
  moved station by station, and capped. Two branches that split from one section share their cut line, so the skin never
  opens or overlaps. The Trident splits from one point low in its middle into a brow, two side prongs and a keel prong;
  the Harpoon's rail is its keel run on past the bow.
- **Details are skin.** A light strip is a face of the skin (`glow(k)` picks the edges); the drive is a recess cut into
  the stern. Nothing is placed on top.
- Every branch has one job and a name (the hood, the lance, the keel blade).

### 2-3. Export it and lay out its detail

`export_blockouts.mjs` asks the page for its picks (`MOCKUP_PICKS`) and their OBJ (`MOCKUP_EXPORT_OBJ`), in ship space
(+x port, +y up, +z bow, metres), one object per part, strips as `glow_*`. Blender imports it with forward Y, up Z, so
nothing is converted and Blender's object space is ship space.

`hull_detail.json` holds the layout, one entry per ship, and the defaults every ship shares. A feature is placed by a
ray: `face` (`top` from above at `[x, z]`, `bottom` from below, `port` from the port side at `[z, y]`, `bow` from ahead
at `[x, y]`). Anything off the centreline is mirrored to starboard.

| Key | What | Ssarith numbers |
|---|---|---|
| `defaults` | armour 0.5 m thick, sub-armour 0.22 m, plate lift up to 0.35 m, gaps 0.5-1.0 m, trenches 2.6-4.6 m in 40 % of cuts, overlays on plates over 22 m2 (drawn in to 0.86, lifted 0.3 m), 1.6 greebles a m2, `light_ref` 0.9 | |
| `domes` | `r_m`, `h_m` (how far it stands proud), `straps` [[angle, width]] | r 2.6-6.4 m |
| `fans` | `r_m`, `fins`, `lamps`, `tilt_deg`, `lamp` colour | r 3.8-4.6 m, 13-21 fins |
| `louvres` | `shape` chevron or slot, `len_m`, `span_m`, `w_m` or `rake_m`, `pitch_m`, `slant_deg` | four a ship, each its own |
| `lamps` | `size_m`, `colour`, `housing` (the eye lamp's two steps) | |
| `exposed` | plan polygons and a y range where the armour is left off (the stern deck round a fan, the neck) | |
| `liveries` | `band_z_m` bands, `chevron` stripes (`z_m`, `sweep`, `w_m`) | green, 0.6-1.4 m |
| `logo` | `at` [x, z] on the top armour, `size_m`, its flood's `flood_w` | 5-7 m |
| `lighting.floods` | spotlights 0.9 m off the hull, grazing it toward `aim`, 90 degree cones | three a ship |
| `windows` | `z_m` stretch and `rows_y_m` deck heights on the port flank | provisional until the room plan |

### 4. The detail layer (`hull_detail.py`)

Built over the low-poly hull, in ship space, never exported: it exists only to be baked.

- **Under layer**: a copy of the hull, near black; its strip faces keep their own material. *Read which faces are strips
  before clearing the copy's materials: Blender zeroes every face's material index when you clear them.*
- **Primary armour**: the hull's faces grouped by facing (top, bottom, port, starboard; bow and stern faces and the
  crotches between branches carry none), thickened outward (Solidify), and cut apart by one Exact boolean against a
  joined cutter of thin slabs along the gap lines: on top and bottom a kinked spine and swept chevrons, every station's
  spacing, sweep and spine width drawn from the ship's seed; on the flanks raked cuts and a chine line in some bays; plus
  the data's exposed areas and every feature's hole. Split into plates (separate by loose parts, slivers dropped); each
  plate raised along its facing by its own amount; plates over 22 m2 get an **overlay**, the plate copied, bisected by a
  raked line, the cut filled, drawn in and lifted, so the armour steps up in layers.
- **Sub-armour**: the same with a thinner shell, its own cuts and wider gaps, so trenches show a second layer.
- **Greebles** only where both armour layers leave the hull bare: points sampled over the hull, kept if a ray up from
  them finds no armour within 0.9 m; blocks, sub-armour slabs, pipe runs and rib sets, each seeded by where it is.
  **Never inside an opening**: a greeble with any vertex inside a hole cutter (a window, a bay, a fan's or dome's well)
  is dropped, or it stands in front of the pane and the bake hides the room behind it (v15's first bake: ragged bays).
- **Features**, each sitting in a hole cut for it: domes (a sphere sunk to stand h proud, a collar ring, straps that hug
  it: a slightly larger sphere intersected with a slab), fans (recess, hub, tilted fins, a lamp ring), louvres (a convex
  outline or two cut through, slats tilted and clipped to the outline with `bisect_plane`), lamps (the eye lamp in a
  two-step housing), windows (1.1 by 0.6 m, groups of 4-12 at 1.6 m pitch, rows 3.5 m apart, about one in five dark).
- **Fixtures** (Ship Design Bible chapter 9): the data's hull floods and the logo flood; a belly pair one forward one
  aft; position lights at the widest frame (port red, starboard green) and white aft; red beacons top and bottom
  mid-ship; white strobes at the bow's tips. Each gets a small lit lamp on the hull and a light for the bake, named
  `LIGHT_<kind>_<n>` (the light-baking skill), and is written to the manifest so the page can blink it.
- **Forward floods** (owner: "we need some more lights pointed forward to paint more of the hull"): `auto_floods` adds rows
  of floods aimed forward along the port flank (mirrored), the top and the belly, every 22 m from the stern to the bow's
  split (`to_z_m`). Every flood's throw is one setting, `defaults.flood`: 4.8 m off the plating, tilted 22 degrees into
  it, a 120 degree cone, 6.4 kW, rows every 44 m with one column down the centreline top and bottom (owner, 2026-11-06:
  "1/2 number of hull lights, 2x size of hull lights, only need 1 column"); a pool's width goes with its standoff and its
  brightness with power over standoff squared (owner, 2026-11-06: "lights are kinda small here, can we get bigger cones"); a flood that
  stands only 0.9 m off and grazes lights a narrow strip. Big ships bake at 3072 px so their texel stays near a frigate's.
- **Panes** (owner: "we need to show interior of ships with windows"): every window and launch bay is a pane. Nothing of
  it is baked but the recess cut for it: no glass, no glowing dot in the maps under the page's pane (owner, 2026-11-06: "I
  would remove window dots from the ships texture since it overlaps with our window cutouts"); `pane_box` writes its entry
  to `PANES`: room kind (quarters, mess, workshop, hold or bay), lit or dark, size, centre and axes. The baker writes them
  into the manifest (`panes`), and the page draws each pane as its own quad from them (below). A window group shares one
  room kind. Never bake the panes into a map: a 1.1 m window is about 17 texels at 2048 px, so its edge comes out ragged
  (owner, 2026-11-05: "windows and hangar bay are kinda messed up, mmake them clean, make the border clean"). Bays (`bays` in the
  data: `w_m` wide by `h_m` tall) are cut through the armour like windows, with
  amber lamps along the lintel. A bay's width always runs level: on a face whose frame runs up the ship (a bow face) its
  frame turns a quarter about the normal, or the mouth comes out on its side.
- **Every pane lies flat on its face, frame and all** (`fits`: each corner and edge middle of the framed rectangle within
  0.12 m of the face's plane). A window that would cross a bend is left out (Ship Design Bible W2); a bay that does not fit
  stops the build, naming it, so the data moves it (owner, 2026-11-05: "hanger window taller than side of the ship": the
  flank bays were 5 m on a face 5.3 m tall, off centre). The frame widths are data (`pane_frame_m`), written into each
  pane, so the check and the page use one number.
- **One axis of symmetry, nothing repeated**: everything off the centreline is built on the port side and mirrored with a
  reflected frame (`frame_at(p, n, mirror=True)`, faces reversed by `place`); every piece's `rid` is a hash of its
  mirrored position (`rid_of`), so a plate and its mirror take the same paint and no two others do.

### 5. The bake

Join the whole detail layer into **one object** first (the bake's setup is per object: 600 pieces cost a minute a pass,
one costs seconds). Select it, make the low-poly hull active, hide the hull from rays (`visible_shadow`, `visible_diffuse`,
`visible_glossy` off), and bake selected-to-active, cage 1.3 m, ray 2.8 m, margin 8 px. Every detail material is one
node graph switched per pass (`kind_material`, `show`).

| Pass | Bake type | What it gives | Seconds at 1024 / 2048 |
|---|---|---|---|
| data | EMIT | kind code, piece id (`rid`), edge amount (Bevel node, armour rolled 0.3 m) | 10 / 40 |
| glow | EMIT | each lamp's colour | 1 / 4 |
| position | EMIT | ship-space position, for liveries, logo and strips | 2 / 6 |
| normal (object) | NORMAL | the paint's own lighting | 11 / 45 |
| normal (tangent) | NORMAL | the normal map for the game and the page | 12 / 45 |
| AO | AO, 2.2 m | occlusion, deep in the trenches | 12 / 90 |
| sun | DIFFUSE direct | a low painter's sun (ship space 0.6, 0.55, 0.58) with its cast shadows | 8 / 60 |
| hull lights | DIFFUSE direct, sun off, fixtures on | the floods' and lamps' light falling on white plating | 15 / 120 |

### 6. The maps

![the unwrap and its painted map](images/04-unwrap-painted-map.jpg)

One unwrap (Smart UV Project at 60 degrees, then `pack_islands` CONCAVE with rotation; about 110-165 m of hull per UV
unit) carries every map. Everything starts from **one flat paint** (`flat_paint`): each texel coloured by its kind
(armour from a palette of olives and khakis by its plate's id with one bronze accent, sub-armour greyer, the under layer
near black, greebles, the dome's green, slats, vents), the liveries, and the emblem on a patch of its own plate colour.

![a close look at the painted map](images/05-painted-map-closeup.jpg)

| Map | Built by | What is in it |
|---|---|---|
| `albedo` (Painted) | `paint_ship` | the flat paint lit the way a painter lights it: the sun in soft steps over a low ambient, AO curved and inked, plates pillowed darker toward their gaps, lit and shaded edges, the dome's highlight, grain |
| `base` (PBR) | `paint_pbr` | the flat paint with no light: cavities kept dark (AO), plates a little darker toward their gaps, worn edges lighter, grain |
| `orm` (PBR) | `paint_pbr` | red AO, green roughness, blue metalness by kind (`PBR`), paint less metallic, worn edges smoother and barer |
| `normal` | the tangent bake | the detail's relief, OpenGL convention |
| `emit` | `strips_and_glow` | lamps, the strips' lit line (0.7 m) in its dark channel, the domes' faint inner light, a soft halo |
| `light` | the hull lights bake | the light falling on the hull, over `light_ref`, stored with a 1/1.6 curve at half size |
| `ao` | the AO bake | kept beside, not inlined (it is in the ORM and the paint) |

The Hooded lance's hood in every map (from the top: the emblem on its plate, the dome, the fan at the stern deck; the
light map here brightened 1.6 times to read):

![the six maps of one ship](images/06-pbr-set.jpg)

### 7. On the page

The page shows the UV mesh from the manifest (indexed, `uv2 = uv` for the AO map, tangents computed) with two materials
over one geometry: **Painted** (the painted map, its own light kept as a third of its emission, no environment) and
**PBR** (base, ORM, tangent normals, under the live light and a soft room environment). Both add the light map
(`diffuse x light^gamma x k`) and the emission. **Studio** and **Night** set the live lights, the light map's strength
and the environment; at night the ship is lit by its own floods, lamps and windows. Strobes flash twice every 1.5 s,
beacons pulse, position lights and floods burn steady, as additive sprites at the fixtures; a shot fixes the clock.

| PBR, Studio | Night |
|---|---|
| ![the Harpoon in its PBR set](images/07-pbr-studio.jpg) | ![the Trident at night](images/08-night.jpg) |

The board at night, each ship lit by its own floods, lamps, windows, strobes and beacons:

![the four picks at night](images/09-board-night.jpg)

#### Rooms, hangars and damage on the page

**Rooms behind the panes.** Each ship's panes are one mesh of quads (`paneMesh`), laid 5 cm off the hull at the place
the bake cut for each pane, each quad the pane plus its frame (0.1 m round a window, 0.35 m round a bay). Every vertex
carries the pane's place in metres, its half size, its axes and its kind, so the frame is exact at any distance: a
bevelled rim of the hull's dark metal lit from above, with no glow of its own (owner, 2026-11-06: "window borders should
also not glow"). Behind a window's glass the view
ray draws the room with `roomBehind(q, D, room, kind, light)` from `docs/mockups/lib/ship-rooms.js` (the same rooms as the
Concord line's), in a frame **level with the decks**, not the face's: up is the ship's up, in is the face's normal turned
level, and the pane cuts the room's front at a slant (owner, 2026-11-05: "Need to account for normal angles on windows, and the hangar bay too").
The quads share the ship's damage uniforms, so a hole or a burn takes its panes with it.

**Hangars are painted too, by parallax** (owner, 2026-11-05: "I actually like the paralax so I'd keep it", after
weighing geometry: "Hangar bay is large enough for me to just recommend using geometry"). `hangarBehind(q, D, room, mouth,
light)` draws the deck, stripe, parked craft, ceiling lights, ribbed walls and back wall behind a bay's open mouth, in the
same level frame as the rooms, its mouth measured level (on a sloped flank the mouth is shorter than the bay is tall). A
hole or a burn takes the pane, and the hole's own decks show through it.

**Damage**, on the shared `docs/mockups/lib/ship-fx.js` (as the Concord and Krai pages): the hull shader runs
`dmgHoles`, `dmgScorch` and `interiorRoom` (holes show the decks behind), per-ship damage uniforms; a phaser scorches, a
torpedo blows a hole and leaves an emitter (fire, smoke, embers) stepped by `emitterStep`. The shared GLSL declares
`uLight`, so a ship's own light map is `uHullLight`.

**Breaking apart**, on the shared `docs/mockups/lib/ship-break.js` (as the Concord and Krai ships): the hull is one piece
while whole, cut into seeded cells when it breaks, every break closed by a cut face. A grown hull has no loft, so its inside
is its **signed distance field**, baked with the ship (`tools/blender/hull_sdf.py`, into the manifest's `sdf`: a
generalised winding number for inside, so overlapping closed branches count as one solid, and the exact distance to the
nearest triangle, one byte a 1 m cell). The same field answers the script's inside test (`sdfAt`) and, as a 3D texture,
the cut faces' shader (`bodyD`), so the cut and the face that closes it agree. Each cell carries the windows and lamps
nearest its seed, and they stutter out when it breaks away. Look at a slice of the field before trusting it: the inside
must be solid and match the silhouette from above and the side.
**The breakable hull is tessellated** (`tessellate`, every edge 2.5 m or less, split at edge middles with its UVs, so the maps
do not move). The shared fracture gives each cell the triangles with a corner near it, which a loft's small triangles
always satisfy; a grown hull of a few hundred big triangles does not, and a cell in the middle of one is left with nothing,
a hole the other cells discard around (owner, 2026-11-05: "it created a big empty hole, no chunks, no fill"). Check that
every cell holds triangles (`window.__huntCells()` on the page) before looking at a break.

| The carrier's bays | Night with the rooms lit | A torpedo's work |
|---|---|---|
| ![bays](images/10-bays.jpg) | ![rooms](images/11-rooms.jpg) | ![damage](images/12-damage.jpg) |

## Another faction: its language is data

The tools are one implementation for every faction (CLAUDE.md 6.1). A faction's look is its own
`data/ships/<faction>/hull_detail.json`, never a copy of a tool. The Kelvane Institute (mockup 23, version 5) was the
second faction through them.

```sh
NODE_PATH=$(npm root -g) node tools/mockups/export_blockouts.mjs docs/mockups/kelvane-ship-concepts.html
NODE_PATH=$(npm root -g) node tools/mockups/emblem_masks.mjs kelvane      # from its SVG hull logo (LOGO_SVG)
/opt/blender/blender -b --python tools/blender/painted_bake.py -- --faction kelvane --ship kelvane_frigate --px 1024 --samples 16
```

| Key | What it sets | The Hunt | The Institute |
|---|---|---|---|
| `paint` | The palette by kind, the accent, the ink, the lit edges, the strip style (`centre` or `whole`), the PBR values | Olives, khakis, bronze | Champagne enamel, brass, smoked glass, velvet red |
| `defaults.cuts` | How the armour is cut | `hunt`: swept chevrons, raked flanks | `grid`: square cuts, a stepped spine plate, seams |
| `defaults.facing_min` | How square a face must be to carry armour | 0.55 | 0.38 (a rounded hull's rolls plated too) |
| `defaults.smooth_deg` | The hull shaded smooth for the tangent bake; the page draws the same normals (`lib/baked-ship.js`) | none (flat) | 40 |
| `defaults.strip_rgb` | The strips' light | green | amber |
| greebles, trenches, overlays | Machinery in the gaps, wide gaps, plates over plates | 1.6 a m2, 40 %, over 22 m2 | none: "clean and sterile" (owner, 2026-11-06) |
| `pods` | Smooth round pieces sunk into the plating | none | 6-14 m, the Institute's in place of machinery |
| `well_*` objects | Dark glass on the low-poly hull, bare of armour, baked as `glass` | none | the wells and slots |
| `liveries.scale` | A measuring scale engraved down the flanks | none | ticks every 1 m, long every 5 m |

## Judge it

Look at the maps and at the ship close up and from the board, in every look and both lights:

- **Silhouette first**: in clay, the hull is one structure and every branch grows from it (F1-F5).
- **Layers**: plates of different outline and height, overlays on the big ones, sub-armour and machinery in the
  trenches. If the gaps are hairlines, widen them and add trenches; the detail must read at ship scale.
- **Features**: each ship has its dome, fan, shaped vents, eye lamp, liveries and emblem; none is a copy of another.
- **Symmetry**: from above, port and starboard mirror plate for plate and colour for colour.
- **Shading**: the trenches are deep, the plates' edges catch the light, the plates stand proud.
- **Lights at night**: the floods pool on the plating without overlapping (L2), the windows read in rows, strobes and
  beacons blink, nothing glows that has no reason to.
- **No seam** shows as a hard colour jump; no z-fighting where a lamp meets the hull.

## Pitfalls hit

| You see | Cause | Fix |
|---|---|---|
| Every gap between plates glows | A mesh copy's materials cleared: Blender zeroes every face's material index | Read which faces are strips before clearing |
| Each bake pass takes minutes | Hundreds of separate objects selected to active | Join the detail into one object first |
| The detail is too fine to read at ship scale | Hairline gaps, small features | Trenches of 2.6-4.6 m in about 40 % of the cuts, domes and fans of 4-6 m, overlays on the big plates |
| The plating reads as brickwork | Plates one length, joints staggered by half | Every station's spacing and sweep its own, from the seed |
| Hundreds of tiny UV islands, empty texture | A hull of placed primitives, every face at an angle to its neighbour | Grow the hull as one structure; pack the islands again by shape |
| A light strip glows metres wide | The keel's whole flat bottom is the strip face | A dark channel with a lit line down its middle |
| The logo cannot be seen | Its field colour matches the khaki plates | Paint the emblem's own plate colour as a patch under it |
| The light map is speckled | The lamps' and windows' faces sampled as lights | More samples (32), a one-pixel blur |
| A break leaves an empty hole, pieces look like hollow cages | The hull's triangles are too big for the fracture: cells in the middle of one get none of it | Tessellate the breakable hull to 2.5 m edges |
| Window and bay borders ragged, rooms speckled | The panes read from a baked map a few texels across | Draw each pane as its own quad from its data (`paneMesh`) |
| Rooms stippled, lit and dark pixels mixed | A per-vertex kind like 8.0 interpolates to 7.9999 and floors to the wrong kind | Round it in the shader (`floor(vKind + 0.5)`) |
| The hull shader will not compile on a damage page | Your uniform has a name the shared GLSL already declares (`uLight`) | Name yours apart (`uHullLight`) |
| The page never becomes ready | A ship in the manifest lacks a map the page asks for | Bake every ship before inlining, or keep the old manifest |
| A flat olive hull | Only the per-part paint, nothing modelled | The detail layer of step 4 |
| Another faction's ships look like the Hunt in other colours | The Hunt's recipe (chevrons, layered armour, trenches, its painted light) laid on them | Write the faction's own recipe from its culture (the method) |
| White plates outlined in black lines | The painted light's ink and double occlusion on a light palette, gaps over a near-black layer | The render light (`PAINT_MODE` "render"); the layer beneath a colour of the palette |
| Seams that end nowhere, notches, T-cuts | Strake seams broken off at random, joints at random | Joints on the frames (`frame_m`); no broken seams |
| Port and starboard differ | Windows and bays placed from a one-sided room plan | Mirror them (rule 2) |
| Greebles cannot be seen | Machinery only in hairline gaps | Open the armour where the systems run (`exposed`), and render it lit |

## Into the game (when the mockup is approved for implementation)

The PBR set maps onto glTF directly: `base` as baseColorTexture, `orm` as metallicRoughnessTexture and
occlusionTexture, `normal` as normalTexture, `emit` as emissiveTexture; the light map goes in as a second emission
layer or a UV2 light map (the light-baking skill), and the fixtures as named `LIGHT_<kind>_<n>` empties so the game can
blink the strobes and darken a lamp when its section loses power. The low-poly hull's triangles and the maps' sizes go in
the ship's manifest against its budget (CLAUDE.md section 10, mid detail, mobile budgets).
