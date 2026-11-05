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
    leaves  1.4, 2.1 and 2.8, holographic: light, not solid (tools/emblems/ssarith_leaves.py lays them out)

    /opt/blender/blender -b -P tools/blender/emblem_ssarith.py -- [--out DIR] [--samples N] [--px N] [--steps]

Writes <out>/ssarith/sd-face.png (face on, orthographic) and sd-angle.png (three-quarter, perspective); with --steps, the
clay blockout sd-clay.png too.
"""
import json
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import emblem_render as R  # noqa: E402

PARTS = os.path.join(R.ROOT, "assets", "emblems", "ssarith", "sd.parts.json")
LEAVES = os.path.join(R.ROOT, "assets", "emblems", "ssarith", "sd.leaves.json")
LEAF_TINTS = ("#8ca847", "#668f38", "#d9c766")   # dry green, olive, straw: the holographic canopy's light
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


def hologram(name, colour):
    """Light, not a solid: an emission in the leaf's colour, brighter toward its edges (the `edge` attribute, 0 on the
    midrib and 1 at the rim), seen through where it is faint, so the forest shows behind it."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.blend_method = "BLEND" if hasattr(m, "blend_method") else None
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    attr = nt.nodes.new("ShaderNodeAttribute")
    attr.attribute_name = "edge"
    ramp = nt.nodes.new("ShaderNodeMapRange")
    ramp.inputs["To Min"].default_value = 1.2
    ramp.inputs["To Max"].default_value = 4.0
    nt.links.new(attr.outputs["Fac"], ramp.inputs["Value"])
    emi = nt.nodes.new("ShaderNodeEmission")
    emi.inputs["Color"].default_value = R.hex_rgb(colour)
    nt.links.new(ramp.outputs["Result"], emi.inputs["Strength"])
    clear = nt.nodes.new("ShaderNodeBsdfTransparent")
    add = nt.nodes.new("ShaderNodeAddShader")
    nt.links.new(clear.outputs[0], add.inputs[0])
    nt.links.new(emi.outputs[0], add.inputs[1])
    mix = nt.nodes.new("ShaderNodeMixShader")
    mix.inputs["Fac"].default_value = 0.45
    nt.links.new(clear.outputs[0], mix.inputs[1])
    nt.links.new(add.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs["Surface"])
    return m


def leaves(layout):
    """The holographic canopy: each leaf a strip from stalk to tip (midrib and both edges), curled by its bend and cupped
    across, as the three.js badge builds it. One mesh per tint."""
    half = layout["outline"][:13]
    mats = [hologram(f"leaf.{i}", c) for i, c in enumerate(LEAF_TINTS)]
    for tint in range(len(LEAF_TINTS)):
        verts, faces, edge = [], [], []
        for x, y, z, length, ang, bend, t in layout["leaves"]:
            if t != tint:
                continue
            a = math.radians(ang)
            ca, sa, base = math.cos(a), math.sin(a), len(verts)
            for u, w in half:
                lift = bend * length * u * u
                for side in (-1, 0, 1):
                    lx, ly = u * length, side * w * length
                    cup = 0.15 * length * w * w * (1 if side else 0)
                    gx, gy = x + lx * ca - ly * sa, y - (lx * sa + ly * ca)   # the page's rotation, in mm (y up), back to the grid (y down)
                    px, py = R.xy((gx, gy))
                    verts.append((px, py, (z + lift + cup) * R.MM))
                    edge.append(abs(side))
            for i in range(len(half) - 1):
                for j in range(2):
                    p0 = base + i * 3 + j
                    faces.append((p0, p0 + 3, p0 + 4, p0 + 1))
        me = bpy.data.meshes.new(f"leaves.{tint}")
        me.from_pydata(verts, [], faces)
        at = me.attributes.new("edge", "FLOAT", "POINT")
        at.data.foreach_set("value", edge)
        ob = bpy.data.objects.new(f"leaves.{tint}", me)
        ob.data.materials.append(mats[tint])
        bpy.context.scene.collection.objects.link(ob)


def main():
    a = R.args()
    parts = json.load(open(PARTS))
    out = os.path.join(a["out"], "ssarith")
    R.reset()
    R.setup(px=a["px"], samples=a["samples"])
    build(parts)
    leaves(json.load(open(LEAVES)))
    if a["steps"]:
        restore = R.with_material(R.clay())
        R.render(os.path.join(out, "sd-clay.png"))
        restore()
    R.render(os.path.join(out, "sd-face.png"))
    bpy.context.scene.camera = R.angle_camera()
    R.render(os.path.join(out, "sd-angle.png"))


main()
