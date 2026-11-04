"""Fleet Command: the Krai Warhost's emblem, KC+ (the fang torc), rendered as a 3D badge (openspec/changes/faction-emblems;
the Emblem Design Guide, docs/design/emblem-guide.html). The owner, 2026-11-04, of the sketch: "bro this version slaps",
"Thats it", "keep that", then "make the 3d version of the fang torc".

The shapes are the guide's own: tools/emblems/export_parts.mjs krai writes assets/emblems/krai/kc.parts.json (every
outline as polygons, 1 grid unit = 1 mm). The shared rig (lights, studio, shadow catcher, Material Maker map loader and
the polygon slab) is tools/blender/emblem_render.py; this file owns the parts, their heights and their metals.

Metals, each a Material Maker graph (tinted where the Krai's colour differs from the graph's own):
    blade steel     the ring, the torc, the thin crescent, the forge star and three fangs: the platinum graph, cooled
    blackened steel the field: the same metal graph, darkened, a forged black
    blood enamel    the founder's fang: the pearl enamel graph tinted blood red, under a clear coat

Heights (mm), lowest to highest, so no two faces ever share a plane (no z-fighting):
    field   0.3 to 1.0   the blackened field inside the ring
    thin    0.6 to 2.2   the thin crescent outside the torc
    fangs   0.6 to 2.6   the fangs, rooted inside the torc's body
    torc    0.6 to 3.0   the faceted torc
    star    0.6 to 3.3   the forge star, struck proud
    ring    0.0 to 3.6   the rim, highest, catching the key light

    /opt/blender/blender -b -P tools/blender/emblem_krai.py -- [--out DIR] [--samples N] [--px N] [--steps]

Writes <out>/krai/kc-face.png (face on, orthographic) and kc-angle.png (three-quarter, perspective); with --steps, the
clay blockout kc-clay.png too.
"""
import json
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import emblem_render as R  # noqa: E402

PARTS = os.path.join(R.ROOT, "assets", "emblems", "krai", "kc.parts.json")
STEEL = "#b9c2cc"      # blade steel, a little colder than the platinum graph
BLACKENED = "#2b2426"  # forged black, with the faintest warmth of the forge
BLOOD = "#c4161f"


def build(parts):
    steel = R.mm_material("emblem_platinum", tint=STEEL)
    black = R.mm_material("emblem_platinum", tint=BLACKENED)
    blood = R.mm_material("emblem_enamel_pearl", coat=0.6, tint=BLOOD)
    R.poly_slab("ring", parts["ring"], 0.0, 3.6, 0.8, steel)
    R.poly_slab("field", parts["field"], 0.3, 1.0, 0.15, black)
    R.poly_slab("thin", [parts["thin"]], 0.6, 2.2, 0.3, steel)
    R.poly_slab("torc", [parts["body"]], 0.6, 3.0, 0.6, steel)
    for i, fang in enumerate(parts["fangs"]):
        R.poly_slab(f"fang.{i}", [fang], 0.6, 2.6, 0.35, blood if i == parts["founder"] else steel)
    R.poly_slab("star", [parts["star"]], 0.6, 3.3, 0.5, steel)


def main():
    a = R.args()
    parts = json.load(open(PARTS))
    out = os.path.join(a["out"], "krai")
    R.reset()
    R.setup(px=a["px"], samples=a["samples"])
    build(parts)
    if a["steps"]:
        restore = R.with_material(R.clay())
        R.render(os.path.join(out, "kc-clay.png"))
        restore()
    R.render(os.path.join(out, "kc-face.png"))
    bpy.context.scene.camera = R.angle_camera()
    R.render(os.path.join(out, "kc-angle.png"))


main()
