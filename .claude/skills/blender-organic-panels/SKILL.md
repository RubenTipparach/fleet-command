---
name: blender-organic-panels
description: Build and render organic, grown console panels for Fleet Command's living factions (the Vhorr Brood first) in Blender, headless - one sculpted body of flesh, chitin and bone grown round crisp instruments (metaballs, voxel remesh, displacement, ribs, sunk chitin plates, tapering bone spines and tendrils), black smoked-glass screens with bronze rims, square dark keys with glowing rims, a portrait pocket, a ribcage wall joining every panel, bioluminescent accents that light their surroundings, a warm top light with grazing rims, a value-ladder palette, and growth measured clear of every readout and key face. Use whenever making or changing an organic, biological, hive, alien-flesh or grown skin, or when a render "looks like metal with blobs on it", "reads as camo", "looks like plastic or chocolate", or "is missing the hive aesthetic" ("organic UI", "flesh panel", "chitin", "tendrils", "Vhorr", "brood", "hive console").
metadata:
  author: Fleet Command (Claude Code)
  version: "1.0"
---

## In Fleet Command

New in this repository (owner, 2026-10-31, of the Vhorr Brood's version 23: "Nah that's bad lol. Let's make a new
design system for organic", and "The brood is overly simple. It's just metal with ameobas on top. Completely missing the
zerg aesthetic"). This skill is the procedure; its companion guide is the Organic Panel Bible,
`docs/design/organic-bible.html`, whose every image is rendered by the study generator `tools/blender/organic_study.py`.
Read the guide's chapters 1 and 9 before starting. The analog `blender-ui-panels` skill still applies to everything that
is not organic (render harness, kit pieces, fit checks on the page). Take the shape, not the art: the reference
screenshots are never committed, and every shape, word and colour is our own.

## 1. The premise

Start from the creature, never from a machine. An organic console is one body that holds instruments:
- the frame and the backdrop are the same body (a ribbed wall of tissue; tubes, veins and tendrils between panels);
- the tech is crisp (screens, keys, the portrait), the frame is soft (flesh, chitin, bone);
- the body breaks the rectangle (spines, arches, horns past the top edge, asymmetric); the instruments never do.

Adding growth to a finished machine panel (version 23) always reads as decoration ("metal with amoebas on top").

## 2. The five materials

| Material | Job | Recipe |
|---|---|---|
| Flesh | Frames, wall | Principled; colour ramp fold `#080303` 0.3, flesh `#33110f` 0.75, lit `#6a2620` 1.15 driven by AO 14 mm + 0.3 x noise (260, detail 10); bump 0.5 over 0.6 mm from wave bands (90, distortion 18) + noise; roughness 0.62, coat 0.55 at 0.22, subsurface 0.08 random walk 1.5 mm. A darker variant (no lit step) for the wall. |
| Chitin | Armour plates | Noise `#140c0e` to `#4a3236`, roughness 0.3, coat 1.0 at 0.03, thin film 380 nm. |
| Bone | Spines, horns, claws, arches | `#e2d6bb`, AO ramp to `#8a7a5e` at the root, roughness 0.38, coat 0.5, subsurface 0.15. |
| Smoked glass | Screens | `#050405`, roughness 0.12, coat 1; dark-red Voronoi edge veins only outside a squarish oval over the readout; a thin bronze rim (`#9a5a34`, metal). |
| Light | Readouts, key rims and icons, pustules, the hero | Emission. Green only for life, the key accent only on keys, violet only for the hero. |

No grey metal. Too much subsurface or coat makes flesh read as plastic or chocolate; too little bump reads as clay.

## 3. Growing the form

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
- **Hero:** a cluster of hexagonal cones (tip and short root) with transmission and violet emission, a point light inside
  and one below, in a cradle of flesh.

## 5. Light

A warm soft area key from above (180 mm, 420 mm up), two grazing rims from the sides at 30% and 20% of the key (warm
left, faint violet right), a cool fill from below at 3%. Every glowing accent carries a small point light just in front
of it so the flesh round it picks up its colour. Never a flat front light: flesh under it reads as plastic. AgX, medium
high contrast; a bright emission desaturates toward white under AgX, so keep accents' emission low (0.6-1.5) and let
their point lights do the spill.

## 6. Fit, measured

- G1: no growth in front of the glass covers a screen, a key face or the portrait window. `organic_study.py` measures
  it on the evaluated geometry (`measure()`); a skin generator must do the same for every kit piece.
- G2: children inside parents (readout in screen, icon in key, face in pocket), with margins.
- G3: overhangs never cover another panel's content.
- G4: 9-slice pieces keep growth in corners and edges; the stretching middle is plain flesh.
Then the page's `console_fit.mjs` and `dom_fit.mjs` as for every skin.

## 7. Working method

Render the study (`/opt/blender/blender --background --factory-startup --python tools/blender/organic_study.py --
--steps 14 --samples 16 --out /tmp/org` for a quick look, about 1.5 minutes on 4 CPUs), look at it, change one thing,
render again. Convert step renders to JPEG (quality 82) for the bible. When building a kit, render each piece face on
through `tools/blender/ui_render.py` at twice its slot size, and compare a 2x crop of every key, screen and the portrait.
