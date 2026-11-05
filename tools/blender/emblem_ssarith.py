"""Fleet Command: the Ssarith Hunt's emblem SD, the Bow Rites, rendered as a 3D badge (openspec/changes/faction-emblems;
the Emblem Design Guide, docs/design/emblem-guide.html). The owner, 2026-11-04 and 05: "3 bow and arrows overlapping
pointed to the right", "shade the bow parts with like wood", "make each arrowhead unique", "Feathera red with orange tip",
then "z fighting, the blood doesnt look right, remove blood, and just make arrow hard green and arrow body metal", "redo
the fern, render a fern forrest sccene. Make THAT exact scene!", "no hologram, just make a scene that looks like that as
background for the emblem, and it should be 3d" (with a photograph of dense ferns in deep shadow).

The shapes are the guide's own: tools/emblems/export_parts.mjs ssarith writes assets/emblems/ssarith/sd.parts.json (every
outline as polygons, 1 grid unit = 1 mm). The fern bed is tools/emblems/ssarith_ferns.py's (assets/emblems/ssarith/
sd.ferns.json: finished triangles and each vertex's light). The shared rig (lights, studio, shadow catcher, Material Maker
map loader and the polygon slab) is tools/blender/emblem_render.py; this file owns the parts, their heights and materials.

Materials, each a Material Maker graph (tools/materials/build_ptex.py):
    laminated wood  the bows' limbs (emblem_wood_laminate), and darkened, their strings
    steel           the arrows' shafts: the platinum graph, cooled
    hard green      the arrowheads: the pearl enamel graph tinted green under a hard clear coat
    feathers        red, their tips orange: the pearl enamel graph tinted, matte
    fern            the fern bed (emblem_fern), each vertex's light from the generator multiplying it
    the floor       the recess's floor: the forest enamel graph, near black (the shadow under the ferns)
    inner walls     the laminated wood, darkened: the recess lined like a shadow box
    pale metal      the pennon's rim: the platinum graph tinted bone

Heights (mm). The badge is a deep frame: the rim stands 10.4 mm, the field sinks to a floor at 0.6 mm, and the fern bed
fills the recess up to 6.4 mm. The bows float over the ferns as thin parts, each bow 0.9 mm prouder than the one behind it
(the largest at the back, level 0). Each part's (bottom, top) at level 0, no two faces sharing a plane:
    floor      0.3 to 0.6
    walls      0.6 to 9.8   a band 2 mm wide inside the rim (owner: "need walls inner, on emblem")
    ferns      0.7 to 6.4
    string     6.17 to 6.9
    feathers   6.63 to 7.1   body and tip meet edge to edge, never overlapping
    shaft      6.27 to 7.3
    head       6.59 to 7.5
    limb       6.11 to 7.9   (the front bow's limb tops out at 9.7)
    frame      0.0 to 10.4

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
FERNS = os.path.join(R.ROOT, "assets", "emblems", "ssarith", "sd.ferns.json")
BONE = "#d8cfb4"    # the pennon's rim: the Tally Keepers' bone, in metal
CORD = "#5a4a38"    # the strings: the laminate darkened, a waxed cord
STEEL = "#b9c2cc"   # the shafts
HARD_GREEN = "#1f9d3a"   # the heads
FEATHER, FEATHER_TIP = "#c4161f", "#f07a1a"
FLOOR = "#141812"   # the recess's floor, in the ferns' shadow
FERN_TINT = "#7dff5c"   # the graph's green pushed toward the photograph's: red and blue pulled down
LEVEL_MM = 0.9
RIM_MM = 10.4
WALL_MM = 9.8          # the inner walls stop just under the rim's top
WALL = "#5a3e28"       # the inner walls: the laminate, darkened, a shadow box
# (top mm at level 0, thickness mm, bevel mm) of each part of a bow; the thicknesses differ so no two bottoms share a plane.
HEIGHTS = {"string": (6.9, 0.73, 0.15), "vanes": (7.1, 0.47, 0.15), "vaneTips": (7.1, 0.47, 0.15), "shaft": (7.3, 1.03, 0.3),
           "head": (7.5, 0.91, 0.3), "limb": (7.9, 1.79, 0.5)}


def build(parts):
    rim = R.mm_material("emblem_platinum", tint=BONE)
    floor = R.mm_material("emblem_enamel_forest", tint=FLOOR)
    wall = R.mm_material("emblem_wood_laminate", tint=WALL)
    wood = R.mm_material("emblem_wood_laminate", coat=0.15)
    cord = R.mm_material("emblem_wood_laminate", tint=CORD)
    steel = R.mm_material("emblem_platinum", tint=STEEL)
    green = R.mm_material("emblem_enamel_pearl", coat=1.0, tint=HARD_GREEN)
    feather = R.mm_material("emblem_enamel_pearl", tint=FEATHER)
    feather_tip = R.mm_material("emblem_enamel_pearl", tint=FEATHER_TIP)
    mats = {"string": cord, "vanes": feather, "vaneTips": feather_tip, "shaft": steel, "head": green, "limb": wood}
    R.poly_slab("frame", parts["frame"], 0.0, RIM_MM, 0.8, rim)
    R.poly_slab("floor", parts["field"], 0.3, 0.6, 0.1, floor)
    R.poly_slab("wall", parts["wall"], 0.6, WALL_MM, 0.3, wall)
    n = len(parts["bows"])
    for i, bow in enumerate(parts["bows"]):
        level = n - 1 - i  # bows[0] is the smallest, in front
        for part, (top, thick, bevel) in HEIGHTS.items():
            rings = bow[part] if part in ("vanes", "vaneTips") else [bow[part]]
            for k, ring in enumerate(rings):
                z1 = top + LEVEL_MM * level
                R.poly_slab(f"bow{i}.{part}.{k}", [ring], z1 - thick, z1, bevel, mats[part])


def ferns(data):
    """The fern bed, as the generator grew it: one mesh of fern material, each vertex's light (`shade`) multiplying the
    leaf's colour, so the deep fronds fall into shadow and the top ones catch the light, as in the owner's photograph."""
    v, f, sh = data["v"], data["f"], data["s"]
    verts = [R.xy((v[i] / 100, v[i + 1] / 100)) + (v[i + 2] / 100 * R.MM,) for i in range(0, len(v), 3)]
    faces = [tuple(f[i:i + 3]) for i in range(0, len(f), 3)]
    me = bpy.data.meshes.new("ferns")
    me.from_pydata(verts, [], faces)
    at = me.attributes.new("shade", "FLOAT", "POINT")
    at.data.foreach_set("value", [x / 100 for x in sh])
    m = R.mm_material("emblem_fern", tint=FERN_TINT)
    m.node_tree.nodes["Principled BSDF"].inputs["Specular IOR Level"].default_value = 0.25  # waxy, not glossy
    nt = m.node_tree
    p = nt.nodes["Principled BSDF"]
    link = p.inputs["Base Color"].links[0]
    attr = nt.nodes.new("ShaderNodeAttribute")
    attr.attribute_name = "shade"
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs["Factor"].default_value = 1.0
    nt.links.new(link.from_socket, mul.inputs["A"])
    nt.links.new(attr.outputs["Fac"], mul.inputs["B"])
    nt.links.new(mul.outputs["Result"], p.inputs["Base Color"])
    ob = bpy.data.objects.new("ferns", me)
    ob.data.materials.append(m)
    bpy.context.scene.collection.objects.link(ob)


def main():
    a = R.args()
    parts = json.load(open(PARTS))
    out = os.path.join(a["out"], "ssarith")
    R.reset()
    R.setup(px=a["px"], samples=a["samples"])
    build(parts)
    ferns(json.load(open(FERNS))["ferns"])
    if a["steps"]:
        restore = R.with_material(R.clay())
        R.render(os.path.join(out, "sd-clay.png"))
        restore()
    R.render(os.path.join(out, "sd-face.png"))
    bpy.context.scene.camera = R.angle_camera()
    R.render(os.path.join(out, "sd-angle.png"))


main()
