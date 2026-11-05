"""Fleet Command: the Ssarith Hunt's emblem SD, the Bow Rites, rendered as a 3D badge (openspec/changes/faction-emblems;
the Emblem Design Guide, docs/design/emblem-guide.html). The owner, 2026-11-04 and 05: "3 bow and arrows overlapping
pointed to the right", "shade the bow parts with like wood", "Background should be like a beige, dull brown and dry green
forest", "make each arrowhead unique", then, with a photograph of ferns in deep shadow: "3d model Fern leaves. Arrow made
of wood. Arrow head made of steal. Feathera red with orange tip. Arrows dripping with bright glowing green blood", "Fern
leaves are the holographic background jungle".

The shapes are the guide's own: tools/emblems/export_parts.mjs ssarith writes assets/emblems/ssarith/sd.parts.json (every
outline as polygons, 1 grid unit = 1 mm). The ferns and the blood are tools/emblems/ssarith_ferns.py's
(assets/emblems/ssarith/sd.ferns.json: the fronds as finished triangles, the blood as polygons). The shared rig (lights,
studio, shadow catcher, Material Maker map loader and the polygon slab) is tools/blender/emblem_render.py; this file owns
the parts, their heights and their materials.

Materials, each a Material Maker graph (tools/materials/build_ptex.py) but the blood:
    laminated wood  the bows' limbs and the arrows' shafts (emblem_wood_laminate), and darkened, the strings
    steel           the arrowheads: the platinum graph, cooled
    feathers        red with an orange tip: the pearl enamel graph tinted, matte
    dry forest      the field (emblem_enamel_forest), darkened under the jungle's hologram, under a light clear coat
    pale metal      the pennon's rim: the platinum graph tinted bone
    blood           bright glowing green: an emission under a wet clear coat (light, so no graph)
    the jungle      the ferns, a hologram: light in green, brighter along every edge, seen through (light, so no graph)

Heights (mm). The jungle's ferns arch off the field up to 2.7 mm; every part of a bow stands above them, and each bow higher than
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
FIELD_DARK = "#5a5446"   # the forest in the dark under the hologram
JUNGLE_DEEP, JUNGLE_BRIGHT = "#0d6b1a", "#4cf040"
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
    forest = R.mm_material("emblem_enamel_forest", coat=0.4, tint=FIELD_DARK)
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
    """The holographic jungle: the fronds as the generator built them, drawn as light. An emission from deep to bright
    green by each vertex's light (`shade`), twice as strong along every leaflet's and stem's edge (`edge`), mixed with
    transparency so the darkened forest shows through, as the three.js badge's hologram shader does."""
    v, f, sh, ed = data["v"], data["f"], data["s"], data["e"]
    verts = [R.xy((v[i] / 100, v[i + 1] / 100)) + (v[i + 2] / 100 * R.MM,) for i in range(0, len(v), 3)]
    faces = [tuple(f[i:i + 3]) for i in range(0, len(f), 3)]
    me = bpy.data.meshes.new("ferns")
    me.from_pydata(verts, [], faces)
    for name, vals in (("shade", [x / 100 for x in sh]), ("edge", [float(x) for x in ed])):
        at = me.attributes.new(name, "FLOAT", "POINT")
        at.data.foreach_set("value", vals)
    m = bpy.data.materials.new("jungle")
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    shade, edge = nt.nodes.new("ShaderNodeAttribute"), nt.nodes.new("ShaderNodeAttribute")
    shade.attribute_name, edge.attribute_name = "shade", "edge"
    col = nt.nodes.new("ShaderNodeMix")
    col.data_type = "RGBA"
    col.inputs["A"].default_value = R.hex_rgb(JUNGLE_DEEP)
    col.inputs["B"].default_value = R.hex_rgb(JUNGLE_BRIGHT)
    nt.links.new(shade.outputs["Fac"], col.inputs["Factor"])
    strength = nt.nodes.new("ShaderNodeMapRange")
    strength.inputs["To Min"].default_value = 0.12
    strength.inputs["To Max"].default_value = 1.1
    nt.links.new(edge.outputs["Fac"], strength.inputs["Value"])
    emi = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(col.outputs["Result"], emi.inputs["Color"])
    nt.links.new(strength.outputs["Result"], emi.inputs["Strength"])
    clear = nt.nodes.new("ShaderNodeBsdfTransparent")
    add = nt.nodes.new("ShaderNodeAddShader")
    nt.links.new(clear.outputs[0], add.inputs[0])
    nt.links.new(emi.outputs[0], add.inputs[1])
    mix = nt.nodes.new("ShaderNodeMixShader")
    mix.inputs["Fac"].default_value = 0.75
    nt.links.new(clear.outputs[0], mix.inputs[1])
    nt.links.new(add.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs["Surface"])
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
