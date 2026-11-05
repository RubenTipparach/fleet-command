"""Fleet Command: the Ssarith Hunt's emblem SD, the Bow Rites, rendered as a 3D badge (openspec/changes/faction-emblems;
the Emblem Design Guide, docs/design/emblem-guide.html). The owner, 2026-11-04 and 05: "3 bow and arrows overlapping
pointed to the right", "shade the bow parts with like wood", "Background should be like a beige, dull brown and dry green
forest", "make each arrowhead unique", then, with a photograph of ferns in deep shadow: "3d model Fern leaves. Arrow made
of wood. Arrow head made of steal. Feathera red with orange tip. Arrows dripping with bright glowing green blood".

The shapes are the guide's own: tools/emblems/export_parts.mjs ssarith writes assets/emblems/ssarith/sd.parts.json (every
outline as polygons, 1 grid unit = 1 mm). The ferns and the blood are tools/emblems/ssarith_ferns.py's
(assets/emblems/ssarith/sd.ferns.json: the fronds as finished triangles, the blood as polygons). The shared rig (lights,
studio, shadow catcher, Material Maker map loader and the polygon slab) is tools/blender/emblem_render.py; this file owns
the parts, their heights and their materials.

Materials, each a Material Maker graph (tools/materials/build_ptex.py) but the blood:
    laminated wood  the bows' limbs and the arrows' shafts (emblem_wood_laminate), and darkened, the strings
    steel           the arrowheads: the platinum graph, cooled
    feathers        red with an orange tip: the pearl enamel graph tinted, matte
    fern            the fronds (emblem_fern), each leaflet shaded by its light from the generator
    dry forest      the field (emblem_enamel_forest), under a light clear coat
    pale metal      the pennon's rim: the platinum graph tinted bone
    blood           bright glowing green: an emission under a wet clear coat (light, so no graph)

Heights (mm). The ferns arch off the field up to 2.7 mm; every part of a bow stands above them, and each bow higher than
the one behind it: the largest (back) at level 0, the middle at 1, the smallest (front) at 2, every top raised 0.9 mm a
level. No two faces share a plane:
    field      0.3 to 1.0
    ferns      1.08 to 2.7
    string     0.6 to 2.9   + 0.9 a level
    feathers   0.6 to 3.1   + 0.9 a level (their orange tips to 3.18)
    shaft      0.6 to 3.3   + 0.9 a level
    head       0.6 to 3.5   + 0.9 a level, its blood pooled 0.15 over it and dripping 0.2 over it
    limb       0.6 to 3.9   + 0.9 a level (the front bow's limb tops out at 5.7)
    frame      0.0 to 6.2   the pennon's rim, highest

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
STEEL = "#b9c2cc"   # the arrowheads
FEATHER, FEATHER_TIP = "#c4161f", "#f07a1a"
BLOOD = "#5dff3a"   # bright glowing green
LEVEL_MM = 0.9
# (top mm at level 0, bevel mm) of each part of a bow.
HEIGHTS = {"string": (2.9, 0.15), "vanes": (3.1, 0.2), "vaneTips": (3.18, 0.15), "shaft": (3.3, 0.3), "head": (3.5, 0.35),
           "limb": (3.9, 0.5)}


def blood_material():
    """Bright glowing green blood: a wet, glossy body that gives off its own light."""
    m = R.principled("blood", BLOOD, rough=0.15, coat=1.0, emission=BLOOD, strength=4.0)
    return m


def build(parts, spill):
    rim = R.mm_material("emblem_platinum", tint=BONE)
    forest = R.mm_material("emblem_enamel_forest", coat=0.4)
    wood = R.mm_material("emblem_wood_laminate", coat=0.15)
    cord = R.mm_material("emblem_wood_laminate", tint=CORD)
    steel = R.mm_material("emblem_platinum", tint=STEEL)
    feather = R.mm_material("emblem_enamel_pearl", tint=FEATHER)
    feather_tip = R.mm_material("emblem_enamel_pearl", tint=FEATHER_TIP)
    blood = blood_material()
    mats = {"string": cord, "vanes": feather, "vaneTips": feather_tip, "shaft": wood, "head": steel, "limb": wood}
    R.poly_slab("frame", parts["frame"], 0.0, 6.2, 0.8, rim)
    R.poly_slab("field", parts["field"], 0.3, 1.0, 0.15, forest)
    n = len(parts["bows"])
    for i, bow in enumerate(parts["bows"]):
        level = n - 1 - i  # bows[0] is the smallest, in front
        for part, (top, bevel) in HEIGHTS.items():
            rings = bow[part] if part in ("vanes", "vaneTips") else [bow[part]]
            for k, ring in enumerate(rings):
                R.poly_slab(f"bow{i}.{part}.{k}", [ring], 0.6, top + LEVEL_MM * level, bevel, mats[part])
        head_top = HEIGHTS["head"][0] + LEVEL_MM * level
        for k, ring in enumerate(spill[i]["rings"]):
            pool = k < 3  # the first three are the pools on the steel; the rest drip down the face
            R.poly_slab(f"bow{i}.blood.{k}", [ring], 0.6, head_top + (0.15 if pool else 0.2), 0.2 if pool else 0.15, blood)


def ferns(data):
    """The fronds, as the generator built them: one mesh of fern material, each vertex's light (`shade`) multiplying the
    leaf's colour so whole fronds sit in shadow and leaflets brighten toward their tips, as in the owner's photograph."""
    v, f, sh = data["v"], data["f"], data["s"]
    verts = [R.xy((v[i] / 100, v[i + 1] / 100)) + (v[i + 2] / 100 * R.MM,) for i in range(0, len(v), 3)]
    faces = [tuple(f[i:i + 3]) for i in range(0, len(f), 3)]
    me = bpy.data.meshes.new("ferns")
    me.from_pydata(verts, [], faces)
    at = me.attributes.new("shade", "FLOAT", "POINT")
    at.data.foreach_set("value", [x / 100 for x in sh])
    for poly in me.polygons:
        poly.use_smooth = True
    m = R.mm_material("emblem_fern")
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
    grown = json.load(open(FERNS))
    build(parts, grown["blood"])
    ferns(grown["ferns"])
    if a["steps"]:
        restore = R.with_material(R.clay())
        R.render(os.path.join(out, "sd-clay.png"))
        restore()
    R.render(os.path.join(out, "sd-face.png"))
    bpy.context.scene.camera = R.angle_camera()
    R.render(os.path.join(out, "sd-angle.png"))


main()
