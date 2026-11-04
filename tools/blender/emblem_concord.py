"""Fleet Command: the Concord's emblem (B-E25, the pointed peak, by default; B-E23 and B-E24 before it) rendered as a 3D badge (openspec/changes/faction-emblems;
the Emblem Design Guide, docs/design/emblem-guide.html). The owner, 2026-11-04: "ok I'm kinda liking it, lets see how it
looks in 3d, render something in blender ... add in materials, normals, specular map, AO, and if you can make the
background of the emblem, the bell shape some kind of holographic stars and nebula shader".

The shapes are the guide's own: tools/emblems/export_parts.mjs writes assets/emblems/concord/<key>.parts.json (every
outline as polygons, 1 grid unit = 1 mm). The shared rig (lights, studio, shadow catcher, Material Maker map loader) is
tools/blender/emblem_render.py; this file owns the parts, their heights and the field's shader.

Heights (mm), lowest to highest, so no two faces ever share a plane (no z-fighting):
    field  0.3 to 1.0   the holographic night inside the bell
    star   0.5 to 1.9   the pressed star's floor, inside a hole cut through the saucer (a pocket 1.0 mm deep)
    legs   0.5 to 1.9   each nacelle's body, and over it its segments 0.5 to 2.4, so the marks between them are
                        grooves 0.5 mm deep (owner, 2026-11-04: "some segment marks on the legs"); the tail 0.5 to 2.4
    lens   0.6 to 2.3   the saucer's lower tier, and its upper tier 1.9 to 2.9, 1.6 mm inside the edge: the double
                        bevel (owner: "a double bevel on the saucer"); the star cut through both
    frame  0.0 to 3.6   the platinum rim, highest, catching the key light
    light  dome to 3.1  the green running light at the bow

    /opt/blender/blender -b -P tools/blender/emblem_concord.py -- [--out DIR] [--samples N] [--px N] [--steps]

Writes <out>/concord/<key>-face.png (face on, orthographic) and <key>-angle.png (three-quarter, perspective); with
--steps, the clay blockout <key>-clay.png too. The key is EMBLEM_KEY, b-e25 by default.
"""
import json
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import emblem_render as R  # noqa: E402

MM = R.MM
# The outline to build: the guide's picked patch, exported by tools/emblems/export_parts.mjs (EMBLEM_KEY to override).
KEY = os.environ.get("EMBLEM_KEY", "b-e25")
PARTS = os.path.join(R.ROOT, "assets", "emblems", "concord", f"{KEY}.parts.json")


def xy(p):
    """A grid point (y down, 0 to 100) to badge millimetres (y up, centred)."""
    return ((p[0] - 50.0) * MM, (50.0 - p[1]) * MM)


def poly_slab(name, rings, z0, z1, bevel, material):
    """A flat part between z0 and z1 mm whose outline is the given polygons (nested ones are holes: the curve's 2D fill
    is even-odd), its edges rounded by `bevel` mm."""
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "2D"
    cu.fill_mode = "BOTH"
    for ring in rings:
        sp = cu.splines.new("POLY")
        sp.points.add(len(ring) - 1)
        for pt, p in zip(sp.points, ring):
            x, y = xy(p)
            pt.co = (x, y, 0, 1)
        sp.use_cyclic_u = True
    h = (z1 - z0) / 2
    cu.extrude = max(0.0, h - bevel) * MM
    cu.bevel_depth = bevel * MM
    cu.bevel_resolution = 4
    ob = bpy.data.objects.new(name, cu)
    ob.location.z = (z0 + h) * MM
    bpy.context.scene.collection.objects.link(ob)
    ob.data.materials.append(material)
    return ob


def holo_field():
    """The bell's field: a night sky in deep blue with drifting nebula (violet, teal, lilac) and fine stars, under a
    clear coat with a thin film, so its sheen shifts colour as the badge turns (the holographic look). All procedural:
    two noise fields for the nebula, a Voronoi scatter for the stars, emission so it glows like a lit display."""
    m = bpy.data.materials.new("concord.holo_field")
    m.use_nodes = True
    nt = m.node_tree
    p = nt.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = R.hex_rgb("#0a1030")
    p.inputs["Roughness"].default_value = 0.35
    p.inputs["Coat Weight"].default_value = 0.3
    p.inputs["Coat Roughness"].default_value = 0.3  # soft: the key lamp glows in it rather than showing as a disc
    p.inputs["Thin Film Thickness"].default_value = 420.0
    p.inputs["Thin Film IOR"].default_value = 1.45
    tc = nt.nodes.new("ShaderNodeTexCoord")
    n1 = nt.nodes.new("ShaderNodeTexNoise")
    n1.inputs["Scale"].default_value = 22.0
    n1.inputs["Detail"].default_value = 10.0
    n1.inputs["Roughness"].default_value = 0.62
    n2 = nt.nodes.new("ShaderNodeTexNoise")
    n2.inputs["Scale"].default_value = 9.0
    n2.inputs["Detail"].default_value = 4.0
    nt.links.new(tc.outputs["Object"], n1.inputs["Vector"])
    nt.links.new(tc.outputs["Object"], n2.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    el = ramp.color_ramp.elements
    el[0].position, el[0].color = 0.3, R.hex_rgb("#03050f")
    el[1].position, el[1].color = 0.82, R.hex_rgb("#b7a6f2")
    for pos, col in ((0.46, "#1b2a6b"), (0.58, "#4b2a8a"), (0.7, "#1aa3c4")):
        e = el.new(pos)
        e.color = R.hex_rgb(col)
    nt.links.new(n1.outputs["Fac"], ramp.inputs[0])
    # The second noise thins the nebula into lanes, so dark sky shows between the clouds.
    lanes = nt.nodes.new("ShaderNodeMapRange")
    lanes.inputs["From Min"].default_value, lanes.inputs["From Max"].default_value = 0.38, 0.66
    nt.links.new(n2.outputs["Fac"], lanes.inputs["Value"])
    neb = nt.nodes.new("ShaderNodeMix")
    neb.data_type = "RGBA"
    neb.blend_type = "MULTIPLY"
    neb.inputs["Factor"].default_value = 1.0
    nt.links.new(ramp.outputs["Color"], neb.inputs["A"])
    nt.links.new(lanes.outputs["Result"], neb.inputs["B"])
    # Stars: a Voronoi scatter, the brightest few cells lit with a sharp point.
    vor = nt.nodes.new("ShaderNodeTexVoronoi")
    vor.inputs["Scale"].default_value = 700.0
    nt.links.new(tc.outputs["Object"], vor.inputs["Vector"])
    pick = nt.nodes.new("ShaderNodeMath")
    pick.operation = "GREATER_THAN"
    pick.inputs[1].default_value = 0.8
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(vor.outputs["Color"], sep.inputs["Color"])
    nt.links.new(sep.outputs["Red"], pick.inputs[0])
    point = nt.nodes.new("ShaderNodeMapRange")
    point.inputs["From Min"].default_value, point.inputs["From Max"].default_value = 0.0, 0.12
    point.inputs["To Min"].default_value, point.inputs["To Max"].default_value = 1.0, 0.0
    nt.links.new(vor.outputs["Distance"], point.inputs["Value"])
    star = nt.nodes.new("ShaderNodeMath")
    star.operation = "MULTIPLY"
    nt.links.new(pick.outputs[0], star.inputs[0])
    nt.links.new(point.outputs["Result"], star.inputs[1])
    starc = nt.nodes.new("ShaderNodeMix")
    starc.data_type = "RGBA"
    starc.blend_type = "ADD"
    nt.links.new(star.outputs[0], starc.inputs["Factor"])
    nt.links.new(neb.outputs["Result"], starc.inputs["A"])
    starc.inputs["B"].default_value = (6.0, 6.5, 8.0, 1.0)
    nt.links.new(starc.outputs["Result"], p.inputs["Emission Color"])
    p.inputs["Emission Strength"].default_value = 1.2
    return m


def pearl_film(m):
    """The pearl's own colour shift (the three.js badge does the same in its pearl shader): a pastel rainbow by facing
    ratio, drifting across the face with a slow noise, multiplied into the enamel's colour, so the ship reads iridescent
    face on as well as at a slant (the thin film colours only its reflections)."""
    nt = m.node_tree
    p = nt.nodes["Principled BSDF"]
    src = p.inputs["Base Color"].links[0].from_socket
    lw = nt.nodes.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.5
    tc = nt.nodes.new("ShaderNodeTexCoord")
    drift = nt.nodes.new("ShaderNodeTexNoise")
    drift.inputs["Scale"].default_value = 18.0
    nt.links.new(tc.outputs["Object"], drift.inputs["Vector"])
    add = nt.nodes.new("ShaderNodeMath")
    add.operation = "ADD"
    nt.links.new(lw.outputs["Facing"], add.inputs[0])
    nt.links.new(drift.outputs["Fac"], add.inputs[1])
    wrap = nt.nodes.new("ShaderNodeMath")
    wrap.operation = "FRACT"
    nt.links.new(add.outputs[0], wrap.inputs[0])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    el = ramp.color_ramp.elements
    el[0].position, el[0].color = 0.0, R.hex_rgb("#f0a8dc")
    el[1].position, el[1].color = 1.0, R.hex_rgb("#f0a8dc")
    for pos, col in ((0.25, "#8fd0ff"), (0.5, "#a6f0c6"), (0.62, "#f4e2a0"), (0.8, "#c8a8ff")):
        e = el.new(pos)
        e.color = R.hex_rgb(col)
    nt.links.new(wrap.outputs[0], ramp.inputs[0])
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs["Factor"].default_value = 0.9
    nt.links.new(src, mul.inputs["A"])
    nt.links.new(ramp.outputs["Color"], mul.inputs["B"])
    nt.links.new(mul.outputs["Result"], p.inputs["Base Color"])


def satin(m):
    """A satin finish on a Material Maker enamel: its roughness raised by 0.25 (soft, wide highlights rather than a
    gloss) and a pale blue sheen at grazing angles, the soft glow of satin cloth or satin-finished enamel."""
    nt = m.node_tree
    p = nt.nodes["Principled BSDF"]
    src = p.inputs["Roughness"].links[0].from_socket
    add = nt.nodes.new("ShaderNodeMath")
    add.operation = "ADD"
    add.use_clamp = True
    add.inputs[1].default_value = 0.25
    nt.links.new(src, add.inputs[0])
    nt.links.new(add.outputs[0], p.inputs["Roughness"])
    p.inputs["Sheen Weight"].default_value = 1.0
    p.inputs["Sheen Roughness"].default_value = 0.45
    p.inputs["Sheen Tint"].default_value = R.hex_rgb("#a9c4ff")
    return m


def build(parts):
    plat = R.mm_material("emblem_platinum")
    # The ship is iridescent pearl (owner, 2026-11-04: "Central shape should be like irridescent"): the pearl enamel with a
    # thin film over it, so its sheen runs through blue, violet and green as the badge turns.
    pearl = R.mm_material("emblem_enamel_pearl", coat=0.5, thin_film=(520.0, 1.7))
    pearl_film(pearl)
    field = holo_field()
    light = R.principled("concord.bow_light", "#2fbf5a", rough=0.05, coat=1.0, emission="#2fbf5a", strength=4.0)

    poly_slab("frame", parts["frame"], 0.0, 3.6, 0.8, plat)
    poly_slab("field", parts["field"], 0.3, 1.0, 0.15, field)
    for i, (leg, segs) in enumerate(zip(parts["legs"], parts["legSegments"])):
        poly_slab(f"leg.{i}", [leg], 0.5, 1.9, 0.35, pearl)
        for j, seg in enumerate(segs):
            poly_slab(f"leg.{i}.seg.{j}", [seg], 0.5, 2.4, 0.35, pearl)
    poly_slab("tail", [parts["tail"]], 0.5, 2.4, 0.4, pearl)
    # The saucer in two tiers (the double bevel), the star cut through both, and the star's floor set in the hole.
    poly_slab("lens", [parts["lens"], parts["star"]], 0.6, 2.3, 0.45, pearl)
    poly_slab("lens.top", [parts["lensTop"], parts["star"]], 1.9, 2.9, 0.3, pearl)
    poly_slab("star.floor", [parts["star"]], 0.5, 1.9, 0.2, satin(R.mm_material("emblem_enamel_signal")))
    lt = parts["light"]
    x, y = xy((lt["cx"], lt["cy"]))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=lt["r"] * MM, location=(x, y, 2.4 * MM))
    ob = bpy.context.active_object
    ob.name = "bow.light"
    ob.scale.z = 0.4
    bpy.ops.object.shade_smooth()
    ob.data.materials.append(light)


def angle_camera():
    """A three-quarter perspective view from the lower right, looking up at the badge's face, so its depth shows."""
    cd = bpy.data.cameras.new("emblem.angle")
    cd.lens = 70
    ob = bpy.data.objects.new("emblem.angle", cd)
    ob.location = Vector((0.085, -0.15, 0.2))
    bpy.context.scene.collection.objects.link(ob)
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = (Vector((0, -0.004, 0.0015)) - ob.location).to_track_quat("-Z", "Y")
    return ob


def main():
    a = R.args()
    parts = json.load(open(PARTS))
    out = os.path.join(a["out"], "concord")
    R.reset()
    R.setup(px=a["px"], samples=a["samples"])
    build(parts)
    if a["steps"]:
        restore = R.with_material(R.clay())
        R.render(os.path.join(out, f"{KEY}-clay.png"))
        restore()
    R.render(os.path.join(out, f"{KEY}-face.png"))
    bpy.context.scene.camera = angle_camera()
    R.render(os.path.join(out, f"{KEY}-angle.png"))


main()
