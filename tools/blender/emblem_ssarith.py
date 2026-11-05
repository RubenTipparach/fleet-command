"""Fleet Command: the Ssarith Hunt's emblem SD, the Bow Rites, rendered as a 3D badge (openspec/changes/faction-emblems;
the Emblem Design Guide, docs/design/emblem-guide.html). The owner, 2026-11-04 and 05: "3 bow and arrows overlapping
pointed to the right", "more colors and shading on the bow and green arrows on the 3d rendering", "shade the bow parts
with like wood and the arrows green with blood", "Background should be like a beige, dull brown and dry green forest".

The shapes are the guide's own: tools/emblems/export_parts.mjs ssarith writes assets/emblems/ssarith/sd.parts.json (every
outline as polygons, 1 grid unit = 1 mm). The shared rig (lights, studio, shadow catcher, Material Maker map loader and
the polygon slab) is tools/blender/emblem_render.py; this file owns the parts, their heights and their materials.

Materials, each a Material Maker graph (tools/materials/build_ptex.py):
    laminated wood  the bows' limbs (emblem_wood_laminate), and darkened, their strings
    Watch green     the arrows' shafts and vanes (emblem_lacquer_watch)
    blood           the arrowheads: the green lacquer under dried blood (emblem_blood_watch)
    dry forest      the field, the hunting ground (emblem_enamel_forest), under a light clear coat
    pale metal      the pennon's rim: the platinum graph tinted bone

Heights (mm). The bows overlap like a hand of cards, so each stands higher than the one behind it: the largest (back)
at level 0, the middle at 1, the smallest (front) at 2, every top raised 0.9 mm a level. No two faces share a plane:
    field   0.3 to 1.0
    string  0.6 to 1.3   + 0.9 a level
    vanes   0.6 to 1.5   + 0.9 a level
    shaft   0.6 to 1.7   + 0.9 a level
    head    0.6 to 1.9   + 0.9 a level
    limb    0.6 to 2.3   + 0.9 a level (the front bow's limb tops out at 4.1)
    frame   0.0 to 4.4   the pennon's rim, highest

    /opt/blender/blender -b -P tools/blender/emblem_ssarith.py -- [--out DIR] [--samples N] [--px N] [--steps]

Writes <out>/ssarith/sd-face.png (face on, orthographic) and sd-angle.png (three-quarter, perspective); with --steps, the
clay blockout sd-clay.png too.
"""
import json
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import emblem_render as R  # noqa: E402

PARTS = os.path.join(R.ROOT, "assets", "emblems", "ssarith", "sd.parts.json")
BONE = "#d8cfb4"    # the pennon's rim: the Tally Keepers' bone, in metal
CORD = "#5a4a38"    # the strings: the laminate darkened, a waxed cord
LEVEL_MM = 0.9
# (top mm at level 0, bevel mm) of each part of a bow.
HEIGHTS = {"string": (1.3, 0.15), "vanes": (1.5, 0.2), "shaft": (1.7, 0.3), "head": (1.9, 0.35), "limb": (2.3, 0.5)}


def build(parts):
    rim = R.mm_material("emblem_platinum", tint=BONE)
    forest = R.mm_material("emblem_enamel_forest", coat=0.4)
    wood = R.mm_material("emblem_wood_laminate", coat=0.15)
    cord = R.mm_material("emblem_wood_laminate", tint=CORD)
    green = R.mm_material("emblem_lacquer_watch", coat=0.5)
    blood = R.mm_material("emblem_blood_watch", coat=0.3)
    mats = {"string": cord, "vanes": green, "shaft": green, "head": blood, "limb": wood}
    R.poly_slab("frame", parts["frame"], 0.0, 4.4, 0.8, rim)
    R.poly_slab("field", parts["field"], 0.3, 1.0, 0.15, forest)
    n = len(parts["bows"])
    for i, bow in enumerate(parts["bows"]):
        level = n - 1 - i  # bows[0] is the smallest, in front
        for part, (top, bevel) in HEIGHTS.items():
            rings = bow[part] if part == "vanes" else [bow[part]]
            for k, ring in enumerate(rings):
                R.poly_slab(f"bow{i}.{part}.{k}", [ring], 0.6, top + LEVEL_MM * level, bevel, mats[part])


def main():
    a = R.args()
    parts = json.load(open(PARTS))
    out = os.path.join(a["out"], "ssarith")
    R.reset()
    R.setup(px=a["px"], samples=a["samples"])
    build(parts)
    if a["steps"]:
        restore = R.with_material(R.clay())
        R.render(os.path.join(out, "sd-clay.png"))
        restore()
    R.render(os.path.join(out, "sd-face.png"))
    bpy.context.scene.camera = R.angle_camera()
    R.render(os.path.join(out, "sd-angle.png"))


main()
