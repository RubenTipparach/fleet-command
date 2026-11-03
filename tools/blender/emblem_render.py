"""Fleet Command: the shared render harness for faction emblems (openspec/changes/faction-emblems, the Emblem Render
Guide docs/design/emblem-guide.html). Every faction's emblem is a physical badge: modelled, lit and rendered the same
way, so the ten sit together as one set. This file owns what they share (CLAUDE.md section 6.1): the scene and Cycles
settings, the light rig and the environment the metals reflect, the face-on camera, the shadow catcher, the material
library, the bezier outlines every part is extruded from, and the outputs (the full render, the console crest at
96 px and the marks at 40 px and 20 px). A faction's own file (tools/blender/emblem_<faction>.py) owns only its parts.

Units: 1 grid unit of the emblem's 100 x 100 grid (docs/mockups/lib/emblems.js) is 1 mm. The badge lies in the xy
plane, its top facing +z (the camera), centred on the origin: x = u - 50, y = 50 - v.

Heights, the rule for every badge (guide, "Build it in layers"):
    ground      z = -8      the surface the badge is pinned to (a shadow catcher: only its shadow is rendered)
    field       z 0 to 1.5  the recessed ground inside the frame: enamel, glass or stone
    device      z up to 4   the picture: domes, gems, raised metal
    frame       z up to 5   the outline: the highest metal, catching the key light on its bevel
"""

import math
import os
import struct
import subprocess
import sys
import json

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
MM = 0.001
GROUND_Z = -8.0
VIEW_MM = 112.0  # the camera's width: the 92-96 mm badge plus room for its shadow


def geo(faction):
    """The emblem's shape numbers from docs/mockups/lib/emblems.js (one source for the flat marks and the render)."""
    out = subprocess.run(["node", os.path.join(ROOT, "tools", "emblems", "geo.mjs"), faction],
                         check=True, capture_output=True, text=True).stdout
    return json.loads(out)


def hex_rgb(h, a=1.0):
    """A #rrggbb colour as linear RGBA (Blender's material colours are linear)."""
    h = h.lstrip("#")
    def lin(c):
        c = int(c, 16) / 255
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return (lin(h[0:2]), lin(h[2:4]), lin(h[4:6]), a)


# ---- scene --------------------------------------------------------------------------------------------------------
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def setup(px=1024, samples=128):
    """Cycles on the CPU, fixed seed, denoised, transparent film (the badge floats over whatever it is placed on),
    Standard view so the faction's colours stay the colours its palette names."""
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    cy = sc.cycles
    cy.device = "CPU"
    cy.samples = samples
    cy.use_adaptive_sampling = False
    cy.seed = 0
    cy.use_denoising = True
    cy.max_bounces = 12
    cy.glossy_bounces = 6
    cy.transmission_bounces = 8
    sc.render.film_transparent = True
    sc.render.resolution_x = sc.render.resolution_y = px
    sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    sc.render.use_stamp = False
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
    environment()
    camera()
    lights()
    ground()


def environment():
    """What the polished metal and the clear coats reflect (guide, "Light"): a studio whose wall facing the badge (the
    camera's side) is near black, so the badge's flat faces keep their own colour, with a bright sky above, a warm band
    at the horizon and a dark floor, so every bevel turned up reads as a bright edge and every bevel turned down as a
    dark one (the look of a cast badge under a studio light). Never seen directly: the film is transparent.

    The world's Generated coordinate is the ray's direction: x right, y up the badge's face, z toward the camera."""
    w = bpy.data.worlds.new("emblem.env")
    bpy.context.scene.world = w
    w.use_nodes = True
    nt = w.node_tree
    nt.nodes.clear()
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(tc.outputs["Generated"], sep.inputs[0])
    # Height on the face: y in -1..1 to 0..1 for the ramp.
    up = nt.nodes.new("ShaderNodeMapRange")
    up.inputs["From Min"].default_value, up.inputs["From Max"].default_value = -1.0, 1.0
    nt.links.new(sep.outputs["Y"], up.inputs["Value"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    nt.links.new(up.outputs["Result"], ramp.inputs[0])
    el = ramp.color_ramp.elements
    el[0].position, el[0].color = 0.0, (0.005, 0.006, 0.008, 1)
    el[1].position, el[1].color = 1.0, (1.0, 1.0, 1.05, 1)
    for pos, col in ((0.45, (0.02, 0.022, 0.03, 1)), (0.55, (0.6, 0.5, 0.36, 1)), (0.62, (0.08, 0.09, 0.11, 1)),
                     (0.8, (0.7, 0.74, 0.82, 1))):
        e = el.new(pos)
        e.color = col
    # The wall behind the camera is dark: scale by (1 - z) squared, so only grazing directions are lit.
    away = nt.nodes.new("ShaderNodeMath")
    away.operation = "SUBTRACT"
    away.inputs[0].default_value = 1.0
    nt.links.new(sep.outputs["Z"], away.inputs[1])
    sq = nt.nodes.new("ShaderNodeMath")
    sq.operation = "POWER"
    sq.use_clamp = True
    sq.inputs[1].default_value = 2.0
    nt.links.new(away.outputs[0], sq.inputs[0])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 1.0
    nt.links.new(ramp.outputs["Color"], mix.inputs["A"])
    nt.links.new(sq.outputs[0], mix.inputs["B"])
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Strength"].default_value = 0.9
    out = nt.nodes.new("ShaderNodeOutputWorld")
    nt.links.new(mix.outputs["Result"], bg.inputs["Color"])
    nt.links.new(bg.outputs[0], out.inputs[0])


def camera():
    cd = bpy.data.cameras.new("emblem.cam")
    cd.type = "ORTHO"
    cd.ortho_scale = VIEW_MM * MM
    ob = bpy.data.objects.new("emblem.cam", cd)
    ob.location = (0, 0, 0.5)
    bpy.context.scene.collection.objects.link(ob)
    bpy.context.scene.camera = ob


def area(name, direction, dist_mm, size_mm, energy_w, colour):
    """An area lamp at dist_mm along direction (x right, y up, z toward the camera), aimed at the badge's centre.
    Area lamps show in glossy reflections, which is what puts the hot streak on a bevel."""
    ld = bpy.data.lights.new(name, "AREA")
    ld.shape = "DISK"
    ld.size = size_mm * MM
    ld.energy = energy_w
    ld.color = colour
    ob = bpy.data.objects.new(name, ld)
    d = Vector(direction).normalized()
    ob.location = d * dist_mm * MM
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = (-d).to_track_quat("-Z", "Y")
    bpy.context.scene.collection.objects.link(ob)
    return ob


def lights():
    """The rig every emblem shares (guide, "Light"): a large warm key high at the upper left, which also throws the
    drop shadow to the lower right; a narrow cool rim from the right that outlines the far bevels; a soft top fill."""
    area("key", (-0.75, 0.85, 2.2), 260, 60, 3.0, (1.0, 0.95, 0.88))
    area("rim", (1.2, -0.25, 0.45), 220, 30, 1.5, (0.75, 0.85, 1.0))
    area("fill", (0.2, 0.4, 1.0), 300, 220, 0.6, (0.9, 0.93, 1.0))


def ground():
    """The surface the badge is pinned to, 8 mm below its back: renders only the shadow (a shadow catcher), so the
    badge can be laid over any console or page."""
    me = bpy.data.meshes.new("ground")
    s = 0.4
    me.from_pydata([(-s, -s, 0), (s, -s, 0), (s, s, 0), (-s, s, 0)], [], [(0, 1, 2, 3)])
    ob = bpy.data.objects.new("ground", me)
    ob.location.z = GROUND_Z * MM
    ob.is_shadow_catcher = True
    bpy.context.scene.collection.objects.link(ob)


# ---- materials (guide, "Materials") -------------------------------------------------------------------------------
def principled(name, base, metallic=0.0, rough=0.3, coat=0.0, emission=None, strength=0.0, transmission=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = hex_rgb(base)
    p.inputs["Metallic"].default_value = metallic
    p.inputs["Roughness"].default_value = rough
    p.inputs["Coat Weight"].default_value = coat
    p.inputs["Coat Roughness"].default_value = 0.03
    p.inputs["Transmission Weight"].default_value = transmission
    if emission:
        p.inputs["Emission Color"].default_value = hex_rgb(emission)
        p.inputs["Emission Strength"].default_value = strength
    return m


def polished_metal(name, base, rough=0.16):
    """Polished metal (platinum, gold, gunmetal): fully metallic, low roughness, so it shows the studio."""
    return principled(name, base, metallic=1.0, rough=rough)


def enamel(name, base, rough=0.25):
    """Fired enamel: a coloured body under a glassy clear coat. The coat gives the sharp highlight, the body the colour."""
    return principled(name, base, rough=rough, coat=1.0)


def glass_cabochon(name, base, glow=0.6):
    """A polished glass dome lit a little from within (clean light, the Concord's technology; any faction whose light
    is its own). Coated so the dome carries a crisp window highlight."""
    return principled(name, base, rough=0.05, coat=1.0, emission=base, strength=glow)


def star_field_enamel(name, base, fleck="#cfe3ff", density=900.0, size=0.035):
    """Enamel with a scatter of fine bright flecks: the night sky. Voronoi cells, the brightest few lit."""
    m = enamel(name, base, rough=0.2)
    nt = m.node_tree
    p = nt.nodes["Principled BSDF"]
    vor = nt.nodes.new("ShaderNodeTexVoronoi")
    vor.inputs["Scale"].default_value = density
    vor.inputs["Randomness"].default_value = 1.0
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.0
    ramp.color_ramp.elements[0].color = (1, 1, 1, 1)
    ramp.color_ramp.elements[1].position = size
    ramp.color_ramp.elements[1].color = (0, 0, 0, 1)
    nt.links.new(vor.outputs["Distance"], ramp.inputs[0])
    nt.links.new(ramp.outputs["Color"], p.inputs["Emission Strength"])
    p.inputs["Emission Color"].default_value = hex_rgb(fleck)
    return m


def mm_material(name, coat=0.0, emission_strength=0.0, tile_mm=50.0):
    """A material from a Material Maker graph's maps (assets/textures/materials/<name>/, rendered from
    tools/materials/ptex/<name>.ptex by tools/materials/export_materials.sh): albedo (sRGB), normal (OpenGL, as Blender
    reads it), ORM (R occlusion, G roughness, B metal) and an emission map when the graph has one. Projected flat onto the
    badge's face from world position, one tile every tile_mm. Material Maker owns how the surface looks; Blender adds
    only what a texture cannot: a clear coat on enamel and the light."""
    d = os.path.join(ROOT, "assets", "textures", "materials", name)
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    p = nt.nodes["Principled BSDF"]
    geo_n = nt.nodes.new("ShaderNodeNewGeometry")
    mapping = nt.nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (1.0 / (tile_mm * MM),) * 3
    nt.links.new(geo_n.outputs["Position"], mapping.inputs["Vector"])

    def tex(kind, colour):
        path = os.path.join(d, f"{kind}.png")
        if not os.path.exists(path):
            return None
        t = nt.nodes.new("ShaderNodeTexImage")
        t.image = bpy.data.images.load(path)
        t.image.colorspace_settings.name = "sRGB" if colour else "Non-Color"
        t.extension = "REPEAT"
        nt.links.new(mapping.outputs["Vector"], t.inputs["Vector"])
        return t

    alb, orm, nrm, emi = tex("albedo", True), tex("orm", False), tex("normal", False), tex("emission", True)
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(orm.outputs["Color"], sep.inputs["Color"])
    ao = nt.nodes.new("ShaderNodeMix")
    ao.data_type = "RGBA"
    ao.blend_type = "MULTIPLY"
    ao.inputs["Factor"].default_value = 1.0
    nt.links.new(alb.outputs["Color"], ao.inputs["A"])
    nt.links.new(sep.outputs["Red"], ao.inputs["B"])
    nt.links.new(ao.outputs["Result"], p.inputs["Base Color"])
    nt.links.new(sep.outputs["Green"], p.inputs["Roughness"])
    nt.links.new(sep.outputs["Blue"], p.inputs["Metallic"])
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(nrm.outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], p.inputs["Normal"])
    p.inputs["Coat Weight"].default_value = coat
    p.inputs["Coat Roughness"].default_value = 0.03
    if emi and emission_strength:
        nt.links.new(emi.outputs["Color"], p.inputs["Emission Color"])
        p.inputs["Emission Strength"].default_value = emission_strength
    return m


def clay():
    """A neutral grey for the blockout shot (guide, worked example step 1)."""
    return principled("clay", "#9a9ca0", rough=0.55)


# ---- shapes -------------------------------------------------------------------------------------------------------
def to_xy(u, v):
    """A grid point (u right, v down) to badge millimetres (x right, y up), centred."""
    return (u - 50.0, 50.0 - v)


def circle_spline(cu, cx, cy, r):
    """A closed bezier circle of radius r mm into curve data cu (four points, the standard 0.5523 handles)."""
    sp = cu.splines.new("BEZIER")
    sp.bezier_points.add(3)
    k = 0.5523 * r
    for i, bp in enumerate(sp.bezier_points):
        a = i * math.pi / 2
        c, s = math.cos(a), math.sin(a)
        bp.co = ((cx + c * r) * MM, (cy + s * r) * MM, 0)
        bp.handle_left = ((cx + c * r + s * k) * MM, (cy + s * r - c * k) * MM, 0)
        bp.handle_right = ((cx + c * r - s * k) * MM, (cy + s * r + c * k) * MM, 0)
    sp.use_cyclic_u = True
    sp.use_smooth = True


def slab(name, z0, z1, bevel, material, circles):
    """A flat part between heights z0 and z1 mm, its outline the given closed shapes: `circles` is a list of
    (cx, cy, r) in badge mm; nested outlines make holes (a ring is two circles). The edge is rounded by `bevel` mm
    and the outline is kept to its exact size (the bevel grows inward, never past the drawn edge)."""
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "2D"
    cu.fill_mode = "BOTH"
    cu.resolution_u = 48
    for cx, cy, r in (circles or []):
        circle_spline(cu, cx, cy, r)
    h = (z1 - z0) / 2
    cu.extrude = max(0.0, h - bevel) * MM
    cu.bevel_depth = bevel * MM
    cu.bevel_resolution = 6
    ob = bpy.data.objects.new(name, cu)
    ob.location.z = (z0 + h) * MM
    bpy.context.scene.collection.objects.link(ob)
    ob.data.materials.append(material)
    # A curve's bevel grows the outline by its depth, so callers pass outlines already moved in by the bevel (outer
    # circles smaller, hole circles larger): ring() and disc() do it, and the part keeps its drawn size.
    return ob


def dome(name, cx, cy, r, z_base, height, material):
    """A cabochon: a sphere of radius r mm squashed to `height` mm above z_base."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=64, ring_count=32, radius=r * MM, location=(cx * MM, cy * MM, z_base * MM))
    ob = bpy.context.active_object
    ob.name = name
    ob.scale.z = height / r
    bpy.ops.object.shade_smooth()
    ob.data.materials.append(material)
    return ob


def ring(name, cx, cy, r_in, r_out, z0, z1, bevel, material):
    """An annulus between radii r_in and r_out mm, from z0 to z1, its edges rounded by `bevel` and its drawn radii
    kept exact (the bevel is taken from inside the drawn outline)."""
    return slab(name, z0, z1, bevel, material, [(cx, cy, r_out - bevel), (cx, cy, r_in + bevel)])


def disc(name, cx, cy, r, z0, z1, bevel, material, hole=0.0):
    """A disc of radius r mm (with a central hole of radius `hole`), drawn size kept as for ring()."""
    circles = [(cx, cy, r - bevel)] + ([(cx, cy, hole + bevel)] if hole else [])
    return slab(name, z0, z1, bevel, material, circles)


# ---- output -------------------------------------------------------------------------------------------------------
def render(path):
    """Render the scene to path (PNG), with the metadata Cycles writes stripped so a rerun is byte-identical."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    strip_png_text(path)


def strip_png_text(path):
    """Drop tEXt, zTXt and iTXt chunks (Cycles stamps the date and file name into them)."""
    data = open(path, "rb").read()
    out, i = [data[:8]], 8
    while i < len(data):
        n = struct.unpack(">I", data[i:i + 4])[0]
        kind = data[i + 4:i + 8]
        chunk = data[i:i + 12 + n]
        if kind not in (b"tEXt", b"zTXt", b"iTXt"):
            out.append(chunk)
        i += 12 + n
    open(path, "wb").write(b"".join(out))


def all_parts():
    return [o for o in bpy.context.scene.objects if o.type in ("MESH", "CURVE") and o.name != "ground"]


def with_material(mat):
    """Give every part one material (the clay blockout shot); returns a function that puts the real ones back."""
    saved = {o.name: list(o.data.materials) for o in all_parts()}
    for o in all_parts():
        o.data.materials.clear()
        o.data.materials.append(mat)
    def restore():
        for o in all_parts():
            o.data.materials.clear()
            for m in saved[o.name]:
                o.data.materials.append(m)
    return restore


def args():
    """Command-line options after Blender's own `--`."""
    a = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = {"out": os.path.join(ROOT, "assets", "emblems"), "samples": 128, "px": 1024, "steps": False}
    it = iter(a)
    for k in it:
        if k == "--out":
            out["out"] = next(it)
        elif k == "--samples":
            out["samples"] = int(next(it))
        elif k == "--px":
            out["px"] = int(next(it))
        elif k == "--steps":
            out["steps"] = True
    return out


# ---- sculpted forms (guide rule: no raw primitives; owner, 2026-11-03: "its too simple and easy to make, see our rules
# about just slapping on primitives") ------------------------------------------------------------------------------
PROFILES = {
    # A blade's cross-section (x across, y up), unit width and height: a low shoulder each side rising to a ridge.
    "blade": [(-0.5, 0.0), (-0.42, 0.28), (-0.18, 0.62), (0.0, 1.0), (0.18, 0.62), (0.42, 0.28), (0.5, 0.0), (0.0, -0.05)],
    # An enamel bed: wide and nearly flat, a soft crown.
    "bed": [(-0.5, 0.0), (-0.47, 0.6), (-0.3, 0.9), (0.0, 1.0), (0.3, 0.9), (0.47, 0.6), (0.5, 0.0), (0.0, -0.05)],
    # A rolled frame: a half-round with a flat foot.
    "roll": [(-0.5, 0.0)] + [(-0.5 * math.cos(t * math.pi / 8), math.sin(t * math.pi / 8)) for t in range(1, 8)] + [(0.5, 0.0), (0.0, -0.05)],
}


def _poly_curve(name, pts, cyclic, dims="2D"):
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = dims
    sp = cu.splines.new("POLY")
    sp.points.add(len(pts) - 1)
    for p, (x, y) in zip(sp.points, pts):
        p.co = (x, y, 0, 1)
    sp.use_cyclic_u = cyclic
    ob = bpy.data.objects.new(name, cu)
    bpy.context.scene.collection.objects.link(ob)
    ob.hide_render = True
    return ob


def sweep(name, path_mm, width_mm, height_mm, profile, material, z=0.0, taper=((0, 1), (1, 1)), cyclic=False):
    """A sculpted swept form: the profile (PROFILES) drawn along a smooth path through path_mm [(x, y)] in the badge's
    plane, width_mm across and height_mm tall, scaled along its length by taper [(t 0..1, scale)] so a blade can swell
    at its root and run out to a point. Its base sits at height z mm. This is how every frame, blade and spine of an
    emblem is made: a path and a section, never a primitive."""
    prof = _poly_curve(name + ".profile", [(x * width_mm * MM, y * height_mm * MM) for x, y in PROFILES[profile]], True)
    tap = _poly_curve(name + ".taper", [(t, s) for t, s in taper], False)
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "3D"
    cu.twist_mode = "Z_UP"
    cu.resolution_u = 32
    sp = cu.splines.new("BEZIER")
    sp.bezier_points.add(len(path_mm) - 1)
    for bp, (x, y) in zip(sp.bezier_points, path_mm):
        bp.co = (x * MM, y * MM, z * MM)
        bp.handle_left_type = bp.handle_right_type = "AUTO"
    sp.use_cyclic_u = cyclic
    sp.use_smooth = True
    cu.bevel_mode = "OBJECT"
    cu.bevel_object = prof
    cu.taper_object = tap
    cu.taper_radius_mode = "OVERRIDE"
    cu.use_fill_caps = True
    ob = bpy.data.objects.new(name, cu)
    bpy.context.scene.collection.objects.link(ob)
    ob.data.materials.append(material)
    return ob


def outline_slab(name, path_mm, z0, z1, bevel, material):
    """A flat inlay whose outline is a smooth closed path through path_mm [(x, y)] (an enamel field inside a frame),
    from z0 to z1 mm with a rounded edge."""
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "2D"
    cu.fill_mode = "BOTH"
    cu.resolution_u = 32
    sp = cu.splines.new("BEZIER")
    sp.bezier_points.add(len(path_mm) - 1)
    for bp, (x, y) in zip(sp.bezier_points, path_mm):
        bp.co = (x * MM, y * MM, 0)
        bp.handle_left_type = bp.handle_right_type = "AUTO"
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


def cut_gem(name, cx, cy, r_mm, sides, material, z=0.0, table=0.55, crown=0.35, depth=0.6):
    """A faceted gem cut with `sides` sides (5 for the Concord's five worlds): a flat table, a crown of facets down to
    the girdle and a pavilion below it, flat shaded so every facet catches its own light. Built facet by facet."""
    import bmesh
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    R = r_mm * MM
    def ring(rad, zz, rot):
        return [bm.verts.new((cx * MM + math.cos(rot + i * 2 * math.pi / sides) * rad,
                               cy * MM + math.sin(rot + i * 2 * math.pi / sides) * rad, zz)) for i in range(sides)]
    rot = math.pi / 2
    top = ring(R * table, (z + r_mm * (crown + 0.25)) * MM, rot)
    mid = ring(R * 0.82, (z + r_mm * 0.25) * MM, rot + math.pi / sides)
    gird = ring(R, z * MM + r_mm * 0.12 * MM, rot)
    tip = bm.verts.new((cx * MM, cy * MM, (z - r_mm * depth) * MM))
    bm.faces.new(top)
    for i in range(sides):
        j = (i + 1) % sides
        bm.faces.new((top[i], mid[i], top[j]))
        bm.faces.new((top[j], mid[i], gird[j]))
        bm.faces.new((mid[i], gird[i], gird[j]))
        bm.faces.new((gird[i], tip, gird[j]))
    bm.normal_update()
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    ob.data.materials.append(material)
    return ob


def polar(r, deg):
    """A point r mm from the centre at deg degrees (0 right, 90 up)."""
    return (r * math.cos(math.radians(deg)), r * math.sin(math.radians(deg)))
