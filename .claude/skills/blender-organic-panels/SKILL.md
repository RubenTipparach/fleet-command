---
name: blender-organic-panels
description: Build and render organic, grown console panels for Fleet Command's living factions (the Vhorr Brood first) in Blender, headless - one surface sculpted in layers (bone, then muscle, then leather stretched over both, veins and sacs of light under the skin) as a height field, its materials blended by masks from the sculpt, never a raw primitive placed on the body, black smoked-glass screens with bronze rims, square dark keys with glowing rims, a portrait pocket, a ribcage wall joining every panel, bioluminescent accents that light their surroundings, a warm top light with grazing rims, a value-ladder palette, and growth measured clear of every readout and key face. Use whenever making or changing an organic, biological, hive, alien-flesh or grown skin, or when a render "looks like metal with blobs on it", "reads as camo", "looks like plastic or chocolate", or "is missing the hive aesthetic" ("organic UI", "flesh panel", "chitin", "tendrils", "Vhorr", "brood", "hive console").
metadata:
  author: Fleet Command (Claude Code)
  version: "1.0"
---

> **Hold: do not use this skill or its guide to build anything yet.** The Organic Panel Bible is a draft that has not
> been reviewed. Use it only to continue the study until it is finished and a human has reviewed and approved it (owner,
> 2026-10-31: "Make a note to not use the guide until it's ready and human reviewed").

## In Fleet Command

New in this repository (owner, 2026-10-31, of the Vhorr Brood's version 23: "Nah that's bad lol. Let's make a new
design system for organic", and "The brood is overly simple. It's just metal with ameobas on top. Completely missing the
zerg aesthetic"). This skill is the procedure; its companion guide is the Organic Panel Bible,
`docs/design/organic-bible.html`, whose every image is rendered by the study generator `tools/blender/organic_study.py`.
Read the guide's chapters 1 and 9 before starting. The analog `blender-ui-panels` skill still applies to everything that
is not organic (render harness, kit pieces, fit checks on the page). Take the shape, not the art: the reference
screenshots are never committed, and every shape, word and colour is our own.

## 0. Rule 1: never use raw primitives

The owner, 2026-10-31: "I don't like the pubbles and the white tendril. These look like low effort shapes. Whereas the
organic leather frame looks a little more purposeful and organic", and "never use raw primitives".

- No sphere, capsule, cylinder, cone, torus, plain tube or smooth untextured spike may appear as a visible form: not as a
  bead, pustule, spine, claw, rib or tendril.
- Every form is sculpted into the one body and carries its skin detail (pores, wrinkles, veins, wetness): vertebrae
  under the skin instead of a bead row, a bone crest the skin thins over instead of a spine, a swelling with light
  glowing through the stretched skin instead of a glossy sphere, a sinew cord raised under the skin instead of a tube.
- Primitives may only be invisible construction (metaball seeds, cutters, the shapes a height field is computed from)
  that the sculpt and its remesh absorb.
- The crisp instruments (screen glass, key caps, the portrait) are tech, not body, and keep their clean edges.

## 1. The premise

Start from the creature, never from a machine. An organic console is one body that holds instruments:
- the frame and the backdrop are the same body (a ribbed wall of tissue; tubes, veins and tendrils between panels);
- the tech is crisp (screens, keys, the portrait), the frame is soft (flesh, chitin, bone);
- the body breaks the rectangle (spines, arches, horns past the top edge, asymmetric); the instruments never do.

Adding growth to a finished machine panel (version 23) always reads as decoration ("metal with amoebas on top").

## 2. The five materials

| Material | Job | Recipe |
|---|---|---|
| Body (from step 20) | The sculpted frame | One Principled blended by the sculpt's masks: leather ramp `#080303` / `#2e140d` / `#55281a` on AO 10 mm + broad noise; raw muscle `#62140f`, veins `#1c1028`, bone stained `#7d6b4c` to ivory `#cfc2a2` in patches, creases `#080303` with dried fluid `#3a2c14`; sacs emit green through the skin; depth to black. Bump 0.8 over 0.7 mm (pores, wrinkles in the webs, grain). Wet where flesh is wet: coat 0.12 + 0.6 muscle + 0.8 crease - 0.5 bone, coat roughness broken 0.08-0.48. |
| Flesh (first round) | Frames, wall | Principled; colour ramp fold `#080303` 0.3, flesh `#33110f` 0.75, lit `#6a2620` 1.15 driven by AO 14 mm + 0.3 x noise (260, detail 10); bump 0.5 over 0.6 mm from wave bands (90, distortion 18) + noise; roughness 0.62, coat 0.55 at 0.22, subsurface 0.08 random walk 1.5 mm. A darker variant (no lit step) for the wall. |
| Chitin (first round; placed plates banned by rule 1) | Armour plates | Noise `#140c0e` to `#4a3236`, roughness 0.3, coat 1.0 at 0.03, thin film 380 nm. |
| Bone (first round; placed spines banned by rule 1) | Spines, horns, claws, arches | `#e2d6bb`, AO ramp to `#8a7a5e` at the root, roughness 0.38, coat 0.5, subsurface 0.15. Now bone is a mask of the body material. |
| Smoked glass | Screens | `#050405`, roughness 0.12, coat 1; dark-red Voronoi edge veins only outside a squarish oval over the readout; a thin bronze rim (`#9a5a34`, metal). |
| Light | Readouts, key rims and icons, pustules, the hero | Emission. Green only for life, the key accent only on keys, violet only for the hero. |

No grey metal. Too much subsurface or coat makes flesh read as plastic or chocolate; too little bump reads as clay.

## 3. Growing the form: layered anatomy, sculpted

The owner, 2026-10-31: "Your approach used discrete shapes. The zerg have a couple layers. There's a bone underneath.
The leather stretches over muscle and bone with veins underneath. One thing you could try is sculpting." This replaces
the first round's discrete shapes (kept below as a record; every visible primitive in it is banned by rule 1).

The frame is ONE height field `z = h(x, y)`, computed in numpy by `tools/blender/organic_anatomy.py` (`Sculpt`) and
written into a 0.5 mm grid mesh (one sample per pixel at 2 px/mm), with its masks as colour attributes:
1. **Bone**: rings round every opening, a spine of varied, turned vertebrae with crest blades and shadowed hollows,
   ribs, an arch over the portrait, horn crests past the frame, claws. Coordinates warped by noise (9 mm at 34 mm, 3 mm
   at 11 mm) so nothing runs parallel to a rectangle.
2. **Muscle** rises from the bone (`rise(under, top, profile)`: never a cliff): lumpy bundles following the frame,
   striated along it; sinew cords between panels; the hero's cradle.
3. **Skin**: `max(core + 0.9, blur(core, 6 mm) - 0.6)`: tight on every crest (thin there, so the bone shows), webbed
   across gaps (in tension, so it wrinkles), diving into every opening (`-14 + 5 (d - 2.6)` mm).
4. **Under the skin**: veins as seeded walks raised 0.7 mm; sacs of light raised 7 mm; creases from the Laplacian.
5. **Masks** for the one material: bone (skin thin over a locally proud crest), muscle, vein, crease, web, sac, depth.

Why a height field: kit pieces are rendered face on, so a surface that never folds under itself loses nothing, and it
repeats exactly. Scripted `bpy.ops.sculpt` strokes need a window context headless Blender lacks; a shrinkwrapped skin
bridges gaps poorly. Look at a 2x crop: if a row of bumps reads as beads, turn and vary them and lower their crests.

### The first round's discrete shapes (record only)

1. **Metaball body.** Capsules round each opening (thicker on top, thinner below), balls swelling at the corners,
   negative cubes (stiffness 10) carving each protected rectangle 3 mm larger. Every panel, the neighbour's frame and
   the hero's cradle grow from elements of bodies like this, so they merge with no seam.
2. **Mesh it.** Convert; squash the back to 20%; Remesh voxel 1.4 mm; Displace clouds 18 mm by 2.2 mm; Corrective
   Smooth 5; shade smooth.
3. **Ribs.** Capsules across a frame's sides, lengths following a sine (longest in the middle), turned 12-15 degrees,
   4-6 mm proud. The wall: tubes every 24 mm, bowed and of uneven radius.
4. **Chitin sunk, not stuck.** Squashed spheres overlapping by a third along a run, swelling to its middle, tilted, half
   inside the flesh. Separate beans on a tube read as decoration.
5. **Bone.** Bezier tubes tapering from radius 1.0 at the root to 0.05 at the tip; long spines arch over the panel and
   its neighbour (a canopy), short ones sweep back; one side heavier than the other.
6. **Tendrils.** Tubes thick at both roots, thin between, with a knot or two (radius pattern 1.0, 0.45, 0.8, 0.35, 0.95).

## 4. The instruments

- **Screen:** a recess 6 mm into the body, smoked glass, a 1.6 mm bronze rim 4.5 mm outside the rectangle (corners 4 mm,
  so the rounded corner does not cut into the screen), content emissive on top of the glass.
- **Key:** a near-black box cap (2 mm bevel, 4 segments, coat 1) in a pit; a 0.7 mm emissive rim 2.5 mm inside the
  face; a flat emissive icon; a small point light in front. Pressed: the cap goes back 3 mm and down 2 mm and its rim
  and icon brighten; the flesh does not move. The page sinks the label by the same 3 px (`--btn-sink: 1.5px`).
- **Portrait:** a tall pocket under an arch of balls in the body, a near-black back, the face lit from below.
- **Hero:** a cluster of shards cut uneven (no raw cone: subdivided, vertices pushed 0.7-1.25 x, flat shaded), frosted
  (roughness 0.5, transmission 1, a fine bump), a core of light inside each, a faint violet volume as mist, grown from a
  sculpted cradle; a point light inside and one below.
- **Key accent:** a saturated orange (`#ff8400`) at emission 1.8. AgX turns bright orange pale pink as it is driven
  harder (the first round's peach rims): keep the accent saturated and gentle and let the dark round it carry it.

## 5. Light

A warm soft area key from above (180 mm, 420 mm up), two grazing rims from the sides at 30% and 20% of the key (warm
left, faint violet right), a cool fill from below at 3%. Every glowing accent carries a small point light just in front
of it so the flesh round it picks up its colour, except a sac under the skin: a lamp that close burns a hot dot into
it; its own emission lights it. From step 23 the light is at 70% so the instruments carry the light. Never a flat front light: flesh under it reads as plastic. AgX, medium
high contrast; a bright emission desaturates toward white under AgX, so keep accents' emission low (0.6-1.5) and let
their point lights do the spill.

## 6. Fit, measured

- G1: no growth in front of the glass comes within 2 mm of a screen, a key face or the portrait window.
  `organic_study.py` measures objects on their evaluated geometry (`measure()`) and the sculpt on its height field
  (`measure_field()`); a skin generator must do the same for every kit piece.
- G2: children inside parents (readout in screen, icon in key, face in pocket), with margins.
- G3: overhangs never cover another panel's content.
- G4: 9-slice pieces keep growth in corners and edges; the stretching middle is plain flesh.
Then the page's `console_fit.mjs` and `dom_fit.mjs` as for every skin.

## 7. Working method

Render the study (`/opt/blender/blender --background --factory-startup --python tools/blender/organic_study.py --
--steps 24 --samples 16 --out /tmp/org` for a quick look, about 1.5 minutes on 4 CPUs; the sculpt itself takes 3 s), look at it, change one thing,
render again. Convert step renders to JPEG (quality 82) for the bible. When building a kit, render each piece face on
through `tools/blender/ui_render.py` at twice its slot size, and compare a 2x crop of every key, screen and the portrait.
