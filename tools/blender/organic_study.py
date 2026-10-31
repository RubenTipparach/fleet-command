"""Fleet Command: the organic panel study, the Organic Panel Bible's worked example.

Owns one organic console panel (a frame holding a backlit membrane window with a readout, one key, nodule lamps and a
tendril reaching a neighbour panel), built up in steps, each step changing one thing, so the bible
(docs/design/organic-bible.html) can show what each change teaches. It is the organic counterpart of the Nucleo NLog
study behind the Analog Panel Bible (owner, 2026-10-31, of the Vhorr Brood's version 23: "Nah that's bad lol. Let's
make a new design system for organic", after promising "if not we're gonna repeat the same exercise we did with
winamp"). The direction is the owner's ("slimey leather, tendrils, claws, carapaces, fibers ... the alien hive mind
aesthetic"); every shape, word and colour here is our own.

A study, not a kit: the console skins are built by tools/blender/build_ui_<skin>.py; the Vhorr Brood is rebuilt on what
this study teaches (the blender-organic-panels skill holds the method).

    /opt/blender/blender --background --factory-startup --python tools/blender/organic_study.py -- \\
        [--steps 1,2,14] [--samples 48] [--out docs/design/organic-bible]

Writes NN-<name>.png per step (converted to JPEG by the caller, see the skill) and fit.json, the fit measurement of the
final step (growth against the readout, the label and the key face). Units: 1 mm is 0.001 Blender units; x right, y up,
z toward the camera, which looks straight down -z (orthographic, as every kit piece is rendered).
"""

import argparse
import json
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import organic_anatomy as oa  # noqa: E402  (the layered sculpt: bone, muscle, skin, as one height field)

MM = 0.001
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))

# The panel, in mm: a corner of a console, a portrait pocket, a screen, a row of three keys, a neighbour panel and a hero
# crystal, all grown from one body. The screen, the key faces and the portrait window are the protected rectangles:
# growth may cover the frames, never these (the bible's rule G1).
WIN = (-105.0, -6.0, 105.0, 70.0)                                          # the screen
NEIGH = (236.0, -70.0, 330.0, 46.0)                                        # the neighbour panel's screen (cut by the view)
KEYS = [(cx - 16.0, -74.0, cx + 16.0, -42.0) for cx in (-52.0, 0.0, 52.0)]  # the key faces
PORT = (-200.0, -40.0, -138.0, 58.0)                                       # the portrait window
SCREEN_Z = -6.0                                                            # the smoked glass, 6 mm into the body
VIEW = (-245.0, -125.0, 300.0, 175.0)                                      # what the camera sees
PX_PER_MM = 2.0
KEY_W = 7.0                                                                # the warm key light's power, W

# The steps: (number, file name, title). Each step turns on one change; a step includes every change before it. The
# order follows the owner's notes on version 23 (coordinator's comparison, 2026-10-31), one fix per step.
STEPS = [
    (1, "boxes", "Version 23's way: metal with growth on top"),
    (2, "one-body", "One sculpted body"),
    (3, "flesh", "Flesh and chitin, not stone"),
    (4, "ribs", "A ribcage rhythm"),
    (5, "light", "Warm light from above, deep hollows"),
    (6, "screen", "Screens are black smoked glass"),
    (7, "keys", "Crisp keys set into the flesh"),
    (8, "portrait", "A portrait pocket under an arch"),
    (9, "spines", "Bone spines arching over"),
    (10, "wall", "One wall of tissue behind it all"),
    (11, "nodules", "Green for life only"),
    (12, "crystal", "A crystal heart"),
    (13, "pressed", "Pressed: the cap sinks, the flesh stays"),
    (14, "final", "Final, with the fit measured"),
    # the second round (owner, 2026-10-31: "Your approach used discrete shapes. The zerg have a couple layers. There's a
    # bone underneath. The leather stretches over muscle and bone with veins underneath ... Try sculpting these shapes",
    # and of the beads, the white spine and the glossy pustules: "These look like low effort shapes. Whereas the organic
    # leather frame looks a little more purposeful and organic"): the frame is sculpted as one surface in layers
    # (tools/blender/organic_anatomy.py) and nothing is placed on it.
    (15, "no-primitives", "Nothing placed on the body"),
    (16, "bone", "Sculpt the bone first"),
    (17, "muscle", "Muscle over the bone"),
    (18, "skin", "Skin stretched over both"),
    (19, "under-skin", "Veins, cords and sacs under the skin"),
    (20, "blend", "Blend the materials by the sculpt"),
    (21, "wet", "Wet where flesh is wet"),
    (22, "cavern", "A cavern behind, not a wall"),
    (23, "values", "Dark, so the instruments carry the light"),
    (24, "final-layered", "Final, layered and measured"),
]

# Version 23's colours (step 1): grey metal, lime everywhere. The fix: dark maroon-brown flesh, near-black chitin,
# ivory bone, black screens, one hot accent for keys (our amber), green only for life, violet only for the hero.
V23 = {"metal": "#6d6f72", "lime": "#a6e34a", "moss": "#7d9a3a", "egg": "#c9d78a", "ink": "#1d2410"}
PAL = {"pit": "#080303", "flesh": "#33110f", "flesh_lit": "#6a2620", "hide": "#1a0908", "chitin": "#140c0e",
       "chitin_hi": "#4a3236", "bone": "#e2d6bb", "bone_dk": "#8a7a5e", "bronze": "#9a5a34", "screen": "#050405",
       "accent": "#ffb03a", "accent_sat": "#ff8400", "accent_hot": "#ffe3a0", "life": "#6fd82a", "hero": "#b35cff", "hero_hot": "#e7c8ff",
       "readout": "#b8f27a"}


def srgb(h):
    """A hex colour as linear RGBA."""
    h = h.lstrip("#")
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c) + (1.0,)


# ---------------------------------------------------------------------------------------------------------- materials
def principled(name, base, rough=0.5, metal=0.0, coat=0.0, coat_rough=0.05, sss=0.0, sss_scale_mm=3.0, emit=None,
               emit_strength=0.0, alpha=1.0, transmission=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = srgb(base)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    b.inputs["Coat Weight"].default_value = coat
    b.inputs["Coat Roughness"].default_value = coat_rough
    b.inputs["Transmission Weight"].default_value = transmission
    if sss:
        b.subsurface_method = "RANDOM_WALK"
        b.inputs["Subsurface Weight"].default_value = sss
        b.inputs["Subsurface Radius"].default_value = (1.0, 0.3, 0.2)
        b.inputs["Subsurface Scale"].default_value = sss_scale_mm * MM
    if emit:
        b.inputs["Emission Color"].default_value = srgb(emit)
        b.inputs["Emission Strength"].default_value = emit_strength
    b.inputs["Alpha"].default_value = alpha
    return m


def nt(m):
    return m.node_tree.nodes, m.node_tree.links


def obj_co(m):
    return nt(m)[0].new("ShaderNodeTexCoord").outputs["Object"]


def noise(m, scale, detail=4.0, vec=None):
    n, l = nt(m)
    t = n.new("ShaderNodeTexNoise")
    t.inputs["Scale"].default_value = scale
    t.inputs["Detail"].default_value = detail
    l.new(vec or obj_co(m), t.inputs["Vector"])
    return t


def ramp(m, fac, stops):
    """A colour ramp through (position, hex) stops, fed by fac."""
    n, l = nt(m)
    r = n.new("ShaderNodeValToRGB")
    els = r.color_ramp.elements
    els[0].position, els[0].color = stops[0][0], srgb(stops[0][1])
    els[1].position, els[1].color = stops[-1][0], srgb(stops[-1][1])
    for pos, col in stops[1:-1]:
        e = els.new(pos)
        e.color = srgb(col)
    l.new(fac, r.inputs["Fac"])
    return r.outputs["Color"]


def grey_metal(name):
    """Version 23's base: grey machined metal."""
    return principled(name, V23["metal"], rough=0.45, metal=0.6)


def moss(name):
    """Version 23's growth: lime moss blotches, the same value as the metal under it."""
    m = principled(name, V23["moss"], rough=0.8)
    t = noise(m, 90.0, 6.0)
    c = ramp(m, t.outputs["Fac"], [(0.4, V23["moss"]), (0.6, V23["lime"])])
    nt(m)[1].new(c, m.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    return m


def flesh(name, grey=False, dark=False):
    """Leathery flesh: dark maroon-brown in the folds, lit brown on the swells (one hue family, a value ladder driven by
    the body's own occlusion), pores and wrinkles in the bump, subsurface so thin edges glow red, and a wet clear coat."""
    if grey:
        return grey_metal(name)
    m = principled(name, PAL["flesh"], rough=0.62, coat=0.55, coat_rough=0.22, sss=0.08, sss_scale_mm=1.5)
    n, l = nt(m)
    ao = n.new("ShaderNodeAmbientOcclusion")
    ao.samples = 16
    ao.inputs["Distance"].default_value = 14 * MM
    wr = noise(m, 260.0, 10.0)
    wrinkle = nt(m)[0].new("ShaderNodeTexWave")      # leathery creases across the swell
    wrinkle.wave_type = "BANDS"
    wrinkle.bands_direction = "DIAGONAL"
    wrinkle.inputs["Scale"].default_value = 90.0
    wrinkle.inputs["Distortion"].default_value = 18.0
    wrinkle.inputs["Detail"].default_value = 4.0
    nt(m)[1].new(obj_co(m), wrinkle.inputs["Vector"])
    mix = n.new("ShaderNodeMath")
    mix.operation = "MULTIPLY_ADD"
    l.new(wr.outputs["Fac"], mix.inputs[0])
    mix.inputs[1].default_value = 0.3
    l.new(ao.outputs["AO"], mix.inputs[2])
    b = n["Principled BSDF"]
    lit = PAL["flesh"] if dark else PAL["flesh_lit"]
    l.new(ramp(m, mix.outputs[0], [(0.3, PAL["pit"]), (0.75, PAL["hide"] if dark else PAL["flesh"]), (1.15, lit)]), b.inputs["Base Color"])
    bump = n.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.5
    bump.inputs["Distance"].default_value = 0.6 * MM
    hsum = n.new("ShaderNodeMath")
    hsum.operation = "MULTIPLY_ADD"
    l.new(wrinkle.outputs["Fac"], hsum.inputs[0])
    hsum.inputs[1].default_value = 0.6
    l.new(wr.outputs["Fac"], hsum.inputs[2])
    l.new(hsum.outputs[0], bump.inputs["Height"])
    l.new(bump.outputs["Normal"], b.inputs["Normal"])
    return m


def chitin(name):
    """Chitin: near black, hard, glossy under a clear coat, a faint thin-film sheen on its ridges."""
    m = principled(name, PAL["chitin"], rough=0.3, coat=1.0, coat_rough=0.03)
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Thin Film Thickness"].default_value = 380.0
    b.inputs["Thin Film IOR"].default_value = 1.45
    t = noise(m, 140.0, 5.0)
    nt(m)[1].new(ramp(m, t.outputs["Fac"], [(0.35, PAL["chitin"]), (0.7, PAL["chitin_hi"])]), b.inputs["Base Color"])
    return m


def bone(name):
    """Ivory bone: pale and waxy, darker toward its roots where it grows from the flesh."""
    m = principled(name, PAL["bone"], rough=0.38, coat=0.5, coat_rough=0.2, sss=0.15, sss_scale_mm=2.0)
    n, l = nt(m)
    ao = n.new("ShaderNodeAmbientOcclusion")
    ao.inputs["Distance"].default_value = 10 * MM
    l.new(ramp(m, ao.outputs["AO"], [(0.2, PAL["bone_dk"]), (0.9, PAL["bone"])]), n["Principled BSDF"].inputs["Base Color"])
    return m


def smoked(name, veins=True):
    """The screen: near-black smoked glass, glossy, with faint dark-red veins only at its edges (its middle stays calm,
    so the glowing content reads on black)."""
    m = principled(name, PAL["screen"], rough=0.12, coat=1.0, coat_rough=0.02)
    if not veins:
        return m
    n, l = nt(m)
    co = obj_co(m)
    vor = n.new("ShaderNodeTexVoronoi")
    vor.feature = "DISTANCE_TO_EDGE"
    vor.inputs["Scale"].default_value = 30.0
    l.new(co, vor.inputs["Vector"])
    vein = n.new("ShaderNodeMapRange")
    vein.inputs["From Max"].default_value = 0.03
    vein.inputs["To Min"].default_value = 1.0
    vein.inputs["To Max"].default_value = 0.0
    l.new(vor.outputs["Distance"], vein.inputs["Value"])
    sep = n.new("ShaderNodeSeparateXYZ")
    l.new(co, sep.inputs[0])
    cx, cy = (WIN[0] + WIN[2]) / 2 * MM, (WIN[1] + WIN[3]) / 2 * MM
    rx, ry = (WIN[2] - WIN[0]) / 2 * MM, (WIN[3] - WIN[1]) / 2 * MM
    terms = []
    for axis, c, r in (("X", cx, rx), ("Y", cy, ry)):
        a = n.new("ShaderNodeMath")
        a.operation = "SUBTRACT"
        l.new(sep.outputs[axis], a.inputs[0])
        a.inputs[1].default_value = c
        d = n.new("ShaderNodeMath")
        d.operation = "DIVIDE"
        l.new(a.outputs[0], d.inputs[0])
        d.inputs[1].default_value = r
        p = n.new("ShaderNodeMath")
        p.operation = "POWER"
        l.new(d.outputs[0], p.inputs[0])
        p.inputs[1].default_value = 4.0       # a squarish oval: calm almost to the corners
        terms.append(p.outputs[0])
    add = n.new("ShaderNodeMath")
    add.operation = "ADD"
    l.new(terms[0], add.inputs[0])
    l.new(terms[1], add.inputs[1])
    edge = n.new("ShaderNodeMapRange")
    edge.inputs["From Min"].default_value = 0.45
    edge.inputs["From Max"].default_value = 1.0
    l.new(add.outputs[0], edge.inputs["Value"])
    mul = n.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    l.new(vein.outputs[0], mul.inputs[0])
    l.new(edge.outputs[0], mul.inputs[1])
    mx = n.new("ShaderNodeMix")
    mx.data_type = "RGBA"
    l.new(mul.outputs[0], mx.inputs["Factor"])
    mx.inputs[6].default_value = srgb(PAL["screen"])
    mx.inputs[7].default_value = srgb("#3a0d0c")
    l.new(mx.outputs[2], n["Principled BSDF"].inputs["Base Color"])
    return m


def glow(name, col, strength, sss=0.0):
    return principled(name, col, rough=0.25, coat=1.0, coat_rough=0.02, emit=col, emit_strength=strength, sss=sss,
                      sss_scale_mm=2.0)


def flat(name, col, rough=0.6, metal=0.0):
    return principled(name, col, rough=rough, metal=metal)


# ---------------------------------------------------------------------------------------------------------- modelling
def link(ob):
    bpy.context.scene.collection.objects.link(ob)
    return ob


def assign(ob, m):
    ob.data.materials.clear()
    ob.data.materials.append(m)
    return ob


def smooth(ob):
    for p in ob.data.polygons:
        p.use_smooth = True
    return ob


def box(name, x0, y0, x1, y1, z0, z1, bevel=1.0, segs=3):
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = ((x1 - x0) * MM, (y1 - y0) * MM, (z1 - z0) * MM)
    ob.location = ((x0 + x1) / 2 * MM, (y0 + y1) / 2 * MM, (z0 + z1) / 2 * MM)
    bpy.ops.object.transform_apply(scale=True)
    if bevel:
        mod = ob.modifiers.new("bevel", "BEVEL")
        mod.width = bevel * MM
        mod.segments = segs
    return ob


def boolean(ob, cutter):
    mod = ob.modifiers.new("cut", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.object = cutter
    mod.solver = "EXACT"
    cutter.hide_render = True


def sphere(name, x, y, z, rx, ry=None, rz=None, seg=40):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=seg // 2, radius=1.0)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = (rx * MM, (ry or rx) * MM, (rz or rx) * MM)
    ob.location = (x * MM, y * MM, z * MM)
    return smooth(ob)


def tube(name, pts, radius_mm, radii, m, res=20):
    """A tapering tube along points (mm), each point's radius a factor of radius_mm: tendrils, spines, ribs, veins."""
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "3D"
    cu.bevel_depth = radius_mm * MM
    cu.bevel_resolution = 6
    cu.resolution_u = res
    cu.use_fill_caps = True
    sp = cu.splines.new("BEZIER")
    sp.bezier_points.add(len(pts) - 1)
    for p, (x, y, z), r in zip(sp.bezier_points, pts, radii):
        p.co = (x * MM, y * MM, z * MM)
        p.handle_left_type = p.handle_right_type = "AUTO"
        p.radius = r
    ob = link(bpy.data.objects.new(name, cu))
    ob.data.materials.append(m)
    return ob


def rect_ring(name, r, z, rad_mm, m, inset=0.0, corner=4.0):
    """A thin rounded rectangle of tube round r (x0, y0, x1, y1): the screen's bronze bevel, a key's lit rim."""
    x0, y0, x1, y1 = r[0] - inset, r[1] - inset, r[2] + inset, r[3] + inset
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "3D"
    cu.bevel_depth = rad_mm * MM
    cu.bevel_resolution = 4
    sp = cu.splines.new("POLY")
    pts = []
    for cx, cy, a0 in ((x1 - corner, y1 - corner, 0), (x0 + corner, y1 - corner, 90), (x0 + corner, y0 + corner, 180),
                       (x1 - corner, y0 + corner, 270)):
        for k in range(7):
            a = math.radians(a0 + k * 15)
            pts.append((cx + corner * math.cos(a), cy + corner * math.sin(a)))
    sp.points.add(len(pts) - 1)
    for p, (x, y) in zip(sp.points, pts):
        p.co = (x * MM, y * MM, z * MM, 1.0)
    sp.use_cyclic_u = True
    ob = link(bpy.data.objects.new(name, cu))
    ob.data.materials.append(m)
    return ob


def text(name, s, x, y, z, size_mm, m, font="DejaVuSans-Bold.ttf", align="CENTER", spacing=1.0):
    cu = bpy.data.curves.new(name, "FONT")
    cu.body = s
    path = os.path.join("/usr/share/fonts/truetype/dejavu", font)
    if os.path.exists(path):
        cu.font = bpy.data.fonts.load(path)
    cu.size = size_mm * MM
    cu.align_x = align
    cu.align_y = "CENTER"
    cu.space_character = spacing
    ob = link(bpy.data.objects.new(name, cu))
    ob.location = (x * MM, y * MM, z * MM)
    ob.data.materials.append(m)
    return ob


def poly_flat(name, pts, z, m):
    """A flat filled polygon (mm) at depth z: icons and the portrait's silhouette."""
    me = bpy.data.meshes.new(name)
    me.from_pydata([(x * MM, y * MM, z * MM) for x, y in pts], [], [list(range(len(pts)))])
    ob = link(bpy.data.objects.new(name, me))
    return assign(ob, m)


class Body:
    """The frame as ONE grown body: metaball elements hugging every opening, carved by negative elements, then meshed,
    voxel-remeshed, lumped by displacement and smoothed. Nothing is a box; every part flows into its neighbour."""

    def __init__(self, name):
        self.mb = bpy.data.metaballs.new(name)
        self.mb.resolution = 2.4 * MM
        self.mb.render_resolution = 1.8 * MM
        self.mb.threshold = 0.6
        self.ob = link(bpy.data.objects.new(name, self.mb))
        self.name = name

    def el(self, kind, x, y, r, sx=0.0, sy=0.0, sz=0.0, rot=0.0, neg=False, z=0.0, stiff=2.0):
        e = self.mb.elements.new(type=kind)
        e.co = (x * MM, y * MM, z * MM)
        e.radius = r * MM
        e.stiffness = stiff
        e.use_negative = neg
        if kind in ("CAPSULE", "ELLIPSOID", "CUBE"):
            e.size_x = sx * MM
        if kind in ("ELLIPSOID", "CUBE"):
            e.size_y = sy * MM
            e.size_z = sz * MM
        if rot:
            from mathutils import Quaternion
            e.rotation = Quaternion((0, 0, 1), math.radians(rot))
        return e

    def ring(self, r, gap, rad, top=1.0, bottom=0.7, knots=True):
        """Swollen spine round an opening: thicker on top, thinner below, knots at the corners."""
        x0, y0, x1, y1 = r
        cx, cy, hw, hh = (x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) / 2, (y1 - y0) / 2
        self.el("CAPSULE", cx, y1 + gap, rad * top, sx=hw)
        self.el("CAPSULE", cx, y0 - gap, rad * bottom, sx=hw)
        self.el("CAPSULE", x0 - gap, cy, rad * 0.9, sx=hh, rot=90)
        self.el("CAPSULE", x1 + gap, cy, rad * 0.9, sx=hh, rot=90)
        if knots:
            for kx, ky in ((x0 - gap, y1 + gap), (x1 + gap, y1 + gap), (x0 - gap, y0 - gap), (x1 + gap, y0 - gap)):
                self.el("BALL", kx, ky, rad * 1.25)

    def carve(self, r, grow=2.0):
        x0, y0, x1, y1 = r
        self.el("CUBE", (x0 + x1) / 2, (y0 + y1) / 2, 4, sx=(x1 - x0) / 2 + grow - 4, sy=(y1 - y0) / 2 + grow - 4, sz=60,
                neg=True, stiff=10.0)

    def mesh(self, lumps_mm=2.2, voxel_mm=1.4):
        bpy.context.view_layer.objects.active = self.ob
        self.ob.select_set(True)
        bpy.ops.object.convert(target="MESH")
        me = bpy.context.active_object
        me.name = self.name
        for v in me.data.vertices:     # sits on the console: squash the back
            if v.co.z < -3 * MM:
                v.co.z = -3 * MM + (v.co.z + 3 * MM) * 0.2
        rem = me.modifiers.new("remesh", "REMESH")
        rem.mode = "VOXEL"
        rem.voxel_size = voxel_mm * MM
        tex = bpy.data.textures.new(self.name + "_lumps", "CLOUDS")
        tex.noise_scale = 0.018
        dsp = me.modifiers.new("lumps", "DISPLACE")
        dsp.texture = tex
        dsp.texture_coords = "OBJECT"
        dsp.strength = lumps_mm * MM
        sm = me.modifiers.new("smooth", "CORRECTIVE_SMOOTH")
        sm.iterations = 5
        sm.use_only_smooth = True
        return smooth(me)


# ------------------------------------------------------------------------------------------------------------- lights
def area(name, loc_mm, target_mm, power_w, size_mm, colour):
    ld = bpy.data.lights.new(name, "AREA")
    ld.energy = power_w
    ld.size = size_mm * MM
    ld.color = colour
    ob = link(bpy.data.objects.new(name, ld))
    ob.location = Vector(loc_mm) * MM
    d = (Vector(target_mm) - Vector(loc_mm)) * MM
    ob.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    return ob


def point(name, loc_mm, power_w, radius_mm, colour):
    ld = bpy.data.lights.new(name, "POINT")
    ld.energy = power_w
    ld.shadow_soft_size = radius_mm * MM
    ld.color = colour
    ob = link(bpy.data.objects.new(name, ld))
    ob.location = Vector(loc_mm) * MM
    return ob


# -------------------------------------------------------------------------------------------------------------- scene
def reset(samples):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = samples
    sc.cycles.seed = 0
    sc.cycles.use_denoising = True
    sc.cycles.max_bounces = 10
    sc.view_settings.view_transform = "AgX"
    sc.view_settings.look = "AgX - Medium High Contrast"
    sc.render.resolution_x = int((VIEW[2] - VIEW[0]) * PX_PER_MM)
    sc.render.resolution_y = int((VIEW[3] - VIEW[1]) * PX_PER_MM)
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGB"
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs["Color"].default_value = (0.004, 0.003, 0.004, 1)
    sc.world = w
    cam = bpy.data.cameras.new("cam")
    cam.type = "ORTHO"
    cam.ortho_scale = (VIEW[2] - VIEW[0]) * MM
    cam.clip_start, cam.clip_end = 0.001, 2.0
    co = link(bpy.data.objects.new("cam", cam))
    co.location = ((VIEW[0] + VIEW[2]) / 2 * MM, (VIEW[1] + VIEW[3]) / 2 * MM, 0.5)
    sc.camera = co
    return sc


ICONS = {   # the keys' icons, our own: forward chevron, a target ring's four ticks, and a stop bar (mm, about the key)
    "go": [(-7, -6), (0, -6), (7, 0), (0, 6), (-7, 6), (0, 0)],
    "stop": [(-6, -6), (6, -6), (6, 6), (-6, 6)],
}


def key_icon(name, kind, cx, cy, z, m):
    if kind == "aim":
        out = []
        for i, (dx, dy, w, h) in enumerate([(-9, -1.2, 6, 2.4), (3, -1.2, 6, 2.4), (-1.2, -9, 2.4, 6), (-1.2, 3, 2.4, 6)]):
            out.append(poly_flat(f"{name}{i}", [(cx + dx, cy + dy), (cx + dx + w, cy + dy), (cx + dx + w, cy + dy + h),
                                                (cx + dx, cy + dy + h)], z, m))
        return out
    return [poly_flat(name, [(cx + x, cy + y) for x, y in ICONS[kind]], z, m)]


# ------------------------------------------------------------------------------------------- the sculpt (step 16 on)
SCULPT_RES_MM = 0.5     # one height sample every 0.5 mm: one per pixel at the bible's 2 px/mm, so no facet shows
PUSTULES = [(-124.0, -32.0, 9.0), (140.0, -62.0, 11.0), (156.0, -44.0, 7.0), (224.0, -88.0, 8.0)]
CRADLE = (200.0, 84.0, 30.0)


def attr(m, name):
    """A colour attribute's channels in a material: (r, g, b, a) sockets."""
    n, l = nt(m)
    a = n.new("ShaderNodeAttribute")
    a.attribute_type = "GEOMETRY"
    a.attribute_name = name
    sep = n.new("ShaderNodeSeparateColor")
    l.new(a.outputs["Color"], sep.inputs[0])
    return sep.outputs[0], sep.outputs[1], sep.outputs[2], a.outputs["Alpha"]


def mathn(m, op, a, b=None, clamp=False):
    n, l = nt(m)
    x = n.new("ShaderNodeMath")
    x.operation = op
    x.use_clamp = clamp
    for i, v in enumerate((a, b)):
        if v is None:
            continue
        if isinstance(v, (int, float)):
            x.inputs[i].default_value = v
        else:
            l.new(v, x.inputs[i])
    return x.outputs[0]


def mixc(m, fac, a, b):
    """Mix two colours (hex or socket) by fac (number or socket)."""
    n, l = nt(m)
    x = n.new("ShaderNodeMix")
    x.data_type = "RGBA"
    for sock, v in ((x.inputs["Factor"], fac), (x.inputs[6], a), (x.inputs[7], b)):
        if isinstance(v, str):
            sock.default_value = srgb(v)
        elif isinstance(v, (int, float)):
            sock.default_value = v
        else:
            l.new(v, sock)
    return x.outputs[2]


def clay(name, muscle):
    """A sculptor's clay pass: the bone layer pale, the muscle layer red clay, so each layer reads as itself."""
    m = principled(name, "#2a1c18", rough=0.75)
    bone_m, musc, _v, _ = attr(m, "m1")
    c = mixc(m, mathn(m, "POWER", bone_m, 0.7), "#2a1c18", "#b9ab90")
    if muscle:
        c = mixc(m, mathn(m, "MULTIPLY", musc, 0.9), c, "#6e2a22")
    nt(m)[1].new(c, nt(m)[0]["Principled BSDF"].inputs["Base Color"])
    return m


def anatomy(name, step):
    """The sculpted body's one material, its layers blended by masks the sculpt wrote (m1: bone showing through thin
    skin, raw muscle in the hollows, veins; m2: creases, webs in tension, pustules, depth). Before step 20 it is plain
    leather (only the veins' and sacs' light show); step 20 blends; step 21 makes it wet where flesh is wet; step 22
    sinks the cavern into shadow; step 23 darkens the whole ladder."""
    on = lambda k: step >= k  # noqa: E731
    dark = on(23)
    m = principled(name, PAL["flesh"], rough=0.6, coat=0.4, coat_rough=0.25, sss=0.06, sss_scale_mm=1.5)
    n, l = nt(m)
    b = n["Principled BSDF"]
    bone_m, musc, vein, _ = attr(m, "m1")
    crease, web, pust, depth = attr(m, "m2")
    ao = n.new("ShaderNodeAmbientOcclusion")
    ao.samples = 16
    ao.inputs["Distance"].default_value = 10 * MM
    pores = n.new("ShaderNodeTexVoronoi")
    pores.inputs["Scale"].default_value = 520.0
    l.new(obj_co(m), pores.inputs["Vector"])
    grain = noise(m, 220.0, 8.0).outputs["Fac"]
    blot = noise(m, 40.0, 3.0).outputs["Fac"]
    wr = n.new("ShaderNodeTexWave")            # leather wrinkles, strongest where the skin is stretched over a gap
    wr.wave_type = "BANDS"
    wr.bands_direction = "DIAGONAL"
    wr.inputs["Scale"].default_value = 120.0
    wr.inputs["Distortion"].default_value = 22.0
    wr.inputs["Detail"].default_value = 5.0
    l.new(obj_co(m), wr.inputs["Vector"])
    # leather: a value ladder on the body's own occlusion, mottled by a broad blotch
    lit = "#4a2416" if dark else "#55281a"     # brown-maroon, not orange: the warm key light adds the warmth
    mid = "#22100a" if dark else "#2e140d"
    t = mathn(m, "ADD", mathn(m, "MULTIPLY", ao.outputs["AO"], 0.85), mathn(m, "MULTIPLY", blot, 0.3))
    c = ramp(m, t, [(0.25, PAL["pit"]), (0.7, mid), (1.1, lit)])
    if on(20):
        # raw muscle where the skin parts in the hollows: redder, darker
        c = mixc(m, mathn(m, "MULTIPLY", musc, 0.75), c, "#4a0f0c" if dark else "#62140f")
        # veins under the skin: dark purple-blue through it
        c = mixc(m, mathn(m, "MULTIPLY", vein, 0.8), c, "#1c1028")
        # bone where the skin thins over a crest: stained ivory, worn brighter on the very top, blended through the leather
        bonec = mixc(m, mathn(m, "MULTIPLY", grain, 1.2, clamp=True), "#7d6b4c", "#cfc2a2" if not dark else "#b3a585")
        bm = mathn(m, "POWER", bone_m, 1.6)
        patch = mathn(m, "MULTIPLY", mathn(m, "SUBTRACT", blot, 0.3), 2.2, clamp=True)   # leather still clings in patches
        c = mixc(m, mathn(m, "MULTIPLY", bm, patch, clamp=True), c, bonec)
        # grime and dried fluid in the creases
        c = mixc(m, mathn(m, "MULTIPLY", crease, 0.75), c, "#080303")
        dried = mathn(m, "MULTIPLY", crease, mathn(m, "GREATER_THAN", blot, 0.55))
        c = mixc(m, mathn(m, "MULTIPLY", dried, 0.45), c, "#3a2c14")
    else:
        c = mixc(m, mathn(m, "MULTIPLY", vein, 0.45), c, "#1c1028")
    if on(19):   # sacs of light: the skin stretched thin over them glows green from inside
        pg = mathn(m, "POWER", pust, 1.5)
        c = mixc(m, mathn(m, "MULTIPLY", pg, 0.55), c, "#25500f")
        b.inputs["Emission Color"].default_value = srgb(PAL["life"])
        # the light spreads through the skin, strongest at the sac's middle, broken by the veins over it
        glowf = mathn(m, "MULTIPLY", mathn(m, "POWER", pust, 0.9), mathn(m, "SUBTRACT", 1.0, mathn(m, "MULTIPLY", vein, 0.8)))
        l.new(mathn(m, "MULTIPLY", glowf, mathn(m, "ADD", 0.25, mathn(m, "MULTIPLY", grain, 0.5))), b.inputs["Emission Strength"])
    # whatever lies deeper falls into shadow: a little before the cavern (step 22), steeply after it
    fall = mathn(m, "POWER", mathn(m, "SUBTRACT", 1.0, depth, clamp=True), 0.7 if on(22) else 1.0)
    c = mixc(m, mathn(m, "MULTIPLY", fall, 1.0 if on(22) else 0.8), c, "#000000")
    l.new(c, b.inputs["Base Color"])
    # wetness
    if on(21):
        wet = mathn(m, "ADD", mathn(m, "ADD", mathn(m, "MULTIPLY", musc, 0.6), mathn(m, "MULTIPLY", crease, 0.8)), 0.12)
        wet = mathn(m, "SUBTRACT", wet, mathn(m, "MULTIPLY", bone_m, 0.5), clamp=True)
        l.new(wet, b.inputs["Coat Weight"])
        # broken by the micro detail so it never reads as a glossy shell
        l.new(mathn(m, "ADD", 0.08, mathn(m, "MULTIPLY", grain, 0.4)), b.inputs["Coat Roughness"])
        l.new(mathn(m, "ADD", 0.48, mathn(m, "MULTIPLY", bone_m, 0.25)), b.inputs["Roughness"])
        b.inputs["Subsurface Weight"].default_value = 0.12
    # bump: pores everywhere, wrinkles where stretched, a fine grain
    h = mathn(m, "MULTIPLY", pores.outputs["Distance"], 0.6)
    h = mathn(m, "ADD", h, mathn(m, "MULTIPLY", wr.outputs["Fac"], mathn(m, "ADD", 0.25, mathn(m, "MULTIPLY", web, 1.2))))
    h = mathn(m, "ADD", h, mathn(m, "MULTIPLY", grain, 0.4))
    bump = n.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.8
    bump.inputs["Distance"].default_value = 0.7 * MM
    l.new(h, bump.inputs["Height"])
    l.new(bump.outputs["Normal"], b.inputs["Normal"])
    return m


def sculpted_frame(step):
    """The frame as one sculpted surface, as far as `step` takes it: bone (16), muscle (17), skin (18), what lies under
    the skin (19), the cavern behind (22). Returns (the sculpt, its mesh object)."""
    layers = {"bone"} | ({"muscle"} if step >= 17 else set()) | ({"skin"} if step >= 18 else set()) | \
             ({"detail"} if step >= 19 else set()) | ({"cavern"} if step >= 22 else set())
    sc = oa.Sculpt((VIEW[0] - 10, VIEW[1] - 10, VIEW[2] + 10, VIEW[3] + 10), SCULPT_RES_MM, WIN, PORT, KEYS, NEIGH,
                        CRADLE, pustules=PUSTULES if step >= 19 else ())
    S, masks = sc.build(layers)
    ny, nx = S.shape
    import numpy as np
    co = np.zeros((ny * nx, 3), dtype=np.float32)
    co[:, 0] = sc.X.ravel() * MM
    co[:, 1] = sc.Y.ravel() * MM
    co[:, 2] = S.ravel() * MM
    idx = np.arange(ny * nx).reshape(ny, nx)
    quads = np.stack([idx[:-1, :-1].ravel(), idx[:-1, 1:].ravel(), idx[1:, 1:].ravel(), idx[1:, :-1].ravel()], axis=1)
    me = bpy.data.meshes.new("sculpt")
    me.vertices.add(len(co))
    me.vertices.foreach_set("co", co.ravel())
    me.loops.add(quads.size)
    me.loops.foreach_set("vertex_index", quads.ravel().astype(np.int32))
    me.polygons.add(len(quads))
    me.polygons.foreach_set("loop_start", (np.arange(len(quads)) * 4).astype(np.int32))
    me.update(calc_edges=True)
    me.validate()
    for nm, chans in (("m1", ("bone", "muscle", "vein", None)), ("m2", ("crease", "web", "pustule", "depth"))):
        a = me.color_attributes.new(nm, "FLOAT_COLOR", "POINT")
        col = np.ones((ny * nx, 4), dtype=np.float32)
        for i, ch in enumerate(chans):
            if ch:
                col[:, i] = masks[ch].ravel()
        a.data.foreach_set("color", col.ravel())
    me.polygons.foreach_set("use_smooth", np.ones(len(quads), dtype=bool))
    ob = link(bpy.data.objects.new("sculpt", me))
    mat = clay("clay", step >= 17) if step < 18 else anatomy("anatomy", step)
    assign(ob, mat)
    # (no lamp over a sac: a point light that close burns a hot dot into the skin; the skin's own emission lights it)
    return sc, ob


def measure_field(sc):
    """Rule G1 on the sculpt: the nearest any part of the surface standing above the screen glass comes to each
    protected rectangle, in mm (negative: it covers it)."""
    import numpy as np
    up = sc.height > SCREEN_Z + 0.5
    out = {}
    for label, r in [("screen", WIN), ("portrait window", PORT)] + [(f"key {i + 1} face", k) for i, k in enumerate(KEYS)]:
        d = oa.sd_box(sc.X, sc.Y, r)[up]
        out[label] = round(float(d.min()), 2)
    return out


LAST = {}     # the last build's sculpt, for the fit measure


def build(step):
    """Build the corner of console as it stands after `step`; return (growth objects, protected objects)."""
    on = lambda n: step >= n  # noqa: E731
    growth, protected = [], []
    x0, y0, x1, y1 = WIN
    fl = flesh("flesh", grey=not on(3))
    ch = chitin("chitin") if on(3) else grey_metal("chitin")
    bn = bone("bone")

    # ---------------------------------------------------------------- the frame
    if not on(2):     # step 1: version 23's way, a grey plate with openings cut, moss blobs and rivets stuck on
        plate = assign(box("plate", -225, -100, 135, 105, -3, 7, bevel=2.5), grey_metal("metal"))
        for i, r in enumerate([WIN, PORT] + KEYS):
            boolean(plate, box(f"cut{i}", r[0] - 3, r[1] - 3, r[2] + 3, r[3] + 3, -20, 20, bevel=0))
        growth.append(plate)
        growth.append(assign(box("stub", 205, -100, 290, 120, -3, 7, bevel=2.5), grey_metal("metal2")))
        mo = moss("moss")
        for i, (cx, cy, r) in enumerate([(-222, 100, 22), (128, 98, 18), (128, -96, 16), (-150, -96, 14), (210, 112, 16)]):
            growth.append(assign(sphere(f"moss{i}", cx, cy, 7, r, r * 0.85, 3.0), mo))
        rv = flat("rivet", "#a3a5a8", rough=0.3, metal=1.0)
        for i, (rx, ry) in enumerate([(-215, -90), (125, -90), (-215, 92), (120, 92), (-120, -90), (-120, 92)]):
            growth.append(assign(sphere(f"rivet{i}", rx, ry, 8, 4, 4, 2.5), rv))
    elif on(16):   # the second round: bone, muscle and skin sculpted as one surface, nothing placed on it
        sculpt, field = sculpted_frame(step)
        growth.append(field)
        LAST["sculpt"] = sculpt
    else:
        b = Body("body")
        b.ring(WIN, 19, 22, top=1.25, bottom=0.8)
        b.ring(PORT, 16, 19, top=1.0, bottom=0.9)
        for r in KEYS:
            b.ring(r, 9, 9, top=0.9, bottom=0.9, knots=False)
        b.el("CAPSULE", 0, -84, 14, sx=88)                         # the shelf the keys grow from
        b.el("CAPSULE", -122, 16, 15, sx=0)                        # the neck joining portrait and screen
        b.el("BALL", -122, -40, 18)
        b.el("BALL", 140, -60, 22)                                 # the root of the tendrils
        b.ring(NEIGH, 14, 16, top=1.2, bottom=0.9)                 # the neighbour panel's frame, same body
        b.el("BALL", 210, -74, 20)
        if on(4):   # a ribcage rhythm: transverse ribs across the screen's top run and the sides, a vertebral ridge
            for i in range(7):      # ribs wrapping the screen frame's sides, ribcage-fashion, longest in the middle
                yy = y0 + 2 + i * (y1 - y0 - 4) / 6
                ln = 14 + 8 * math.sin(math.pi * i / 6)
                for sx_, sg in ((x0 - 22, -1), (x1 + 22, 1)):
                    b.el("CAPSULE", sx_ + sg * 4, yy, 7.5, sx=ln, rot=sg * 12, z=6)
            for i in range(9):      # and down the portrait pocket's outer side
                yy = PORT[1] + i * (PORT[3] - PORT[1]) / 8
                b.el("CAPSULE", PORT[0] - 20, yy, 7.0, sx=12 + 4 * math.sin(math.pi * i / 8), rot=-15, z=5)
            for i in range(10):     # the key shelf's underside, a row of short ribs
                b.el("CAPSULE", -81 + i * 18, -92, 6.0, sx=8, rot=90, z=4)
        if on(8):   # the portrait's arch: a swollen bow of flesh over the pocket, rising past the frame
            pcx, top = (PORT[0] + PORT[2]) / 2, PORT[3] + 12
            for k in range(13):
                a = math.pi * k / 12
                b.el("BALL", pcx - math.cos(a) * 42, top + math.sin(a) * 34, 13 - 3 * math.sin(a))
        for r in [WIN, PORT, NEIGH] + KEYS:
            b.carve(r, grow=3.0)
        body = assign(b.mesh(), fl)
        growth.append(body)

    # ---------------------------------------------------------------- chitin carapace over the flesh (step 3)
    if on(3) and not on(15):   # (step 15 removes every placed form: beads, plates, spines, spheres)
        # a segmented band of chitin along the screen's top run, overlapping like vertebrae, sunk into the flesh so the
        # flesh swells round each plate (never a plate sitting on top of the frame)
        n_seg = 13
        for k in range(n_seg):
            t = k / (n_seg - 1)
            cx = x0 - 26 + (x1 - x0 + 52) * t
            arch = math.sin(t * math.pi)
            size = 1.0 + 0.18 * arch + 0.06 * math.sin(k * 2.3)      # swelling to the middle, never two alike
            plate = sphere(f"vert{k}", cx, y1 + 22 + 6 * arch, 11 + 3 * arch, 15.5 * size, 13 * size, 6.5)
            plate.rotation_euler = (math.radians(-22), math.radians(-18), math.radians((t - 0.5) * -30))
            growth.append(assign(plate, ch))
        for k in range(7):          # and over the portrait's shoulder, down its outer side
            yy = PORT[3] - k * 15
            plate = sphere(f"shoulder{k}", PORT[0] - 22 - 2 * math.sin(k / 6 * math.pi), yy, 12, 15, 10, 8)
            plate.rotation_euler = (0, math.radians(14), math.radians(-12))
            growth.append(assign(plate, ch))

    # ---------------------------------------------------------------- the screen
    if on(6):    # near-black smoked glass, a thin bronze bevel rim, the content glowing on black
        assign(box("screen", x0 - 8, y0 - 8, x1 + 8, y1 + 8, SCREEN_Z - 2, SCREEN_Z, bevel=0), smoked("smoked"))
        assign(box("neighscreen", NEIGH[0] - 8, NEIGH[1] - 8, NEIGH[2] + 8, NEIGH[3] + 8, SCREEN_Z - 2, SCREEN_Z, bevel=0),
               smoked("smoked2", veins=False))
        growth.append(rect_ring("bezel2", NEIGH, SCREEN_Z + 2.5, 1.6, principled("bronze2", PAL["bronze"], rough=0.3, metal=1.0),
                                inset=4.5, corner=4.0))
        for k in range(5):   # the neighbour's own readout, a radar sweep's arcs
            protected.append(rect_ring(f"blip{k}", (262 + k * 4, -30 + k * 4, 300 - k * 4, 8 - k * 4), SCREEN_Z + 0.3, 0.5,
                                       glow(f"blipg{k}", PAL["readout"], 2.0 - k * 0.3), corner=10.0 - k * 2))
        growth.append(rect_ring("bezel", WIN, SCREEN_Z + 2.5, 1.6, principled("bronze", PAL["bronze"], rough=0.3, metal=1.0),
                                inset=4.5, corner=4.0))
        ink = glow("readout", PAL["readout"], 4.0)
        dim = glow("readout_dim", "#24331a", 1.0)
        z = SCREEN_Z + 0.3
    else:        # version 23: a lit lime field behind dark letters
        assign(box("screen", x0 - 8, y0 - 8, x1 + 8, y1 + 8, SCREEN_Z - 2, SCREEN_Z, bevel=0),
               glow("limefield", V23["lime"], 1.3))
        ink = flat("ink", V23["ink"])
        dim = flat("inkdim", "#6f8a3a")
        z = SCREEN_Z + 0.3
    protected.append(text("readout", "HULL 82%", -30, (y0 + y1) / 2 + 6, z, 28, ink, font="DejaVuSansMono-Bold.ttf"))
    protected.append(text("label", "VITALS", x0 + 14, y1 - 11, z, 9, ink, align="LEFT", spacing=1.3))
    for i in range(16):
        bx = x0 + 14 + i * 11.6
        protected.append(assign(box(f"pip{i}", bx, y0 + 9, bx + 8, y0 + 16, z - 0.2, z + 0.2, bevel=0),
                                ink if i < 12 else dim))

    # ---------------------------------------------------------------- the keys
    kinds = ["go", "aim", "stop"]
    for i, r in enumerate(KEYS):
        cx, cy = (r[0] + r[2]) / 2, (r[1] + r[3]) / 2
        if not on(7):   # version 23: soft green pillows
            cap = assign(sphere(f"pillow{i}", cx, cy, 2, 16, 15, 7), glow(f"pillow{i}", V23["lime"], 0.5))
            protected.append(cap)
            protected += key_icon(f"icon{i}", kinds[i], cx, cy, 9.2, flat(f"iconink{i}", V23["ink"]))
            continue
        pressed = step == 13 and i == 1
        sink, drop = (3.0, 2.0) if pressed else (0.0, 0.0)
        cap = box(f"cap{i}", r[0] + 1, r[1] + 1 - drop, r[2] - 1, r[3] - 1 - drop, -6 - sink, 6 - sink, bevel=2.0, segs=4)
        protected.append(assign(cap, principled(f"keycap{i}", "#0e0a0b", rough=0.28, coat=1.0, coat_rough=0.03)))
        if on(23):   # a hot accent: a saturated orange driven hard, so AgX keeps it orange at the edges and hot inside
            lit = glow(f"rim{i}", PAL["accent_sat"], 1.8)
        else:
            hot = PAL["accent_hot"] if pressed else PAL["accent"]
            lit = glow(f"rim{i}", hot, 3.2 if pressed else 1.6)
        protected.append(rect_ring(f"rim{i}", (r[0] + 2.5, r[1] + 2.5 - drop, r[2] - 2.5, r[3] - 2.5 - drop), 6.2 - sink, 0.7,
                                   lit, corner=3.0))
        protected += key_icon(f"icon{i}", kinds[i], cx, cy - drop, 6.3 - sink, lit)
        point(f"keyspill{i}", (cx, cy - drop, 12 - sink), 0.03 if pressed else 0.012, 12, (1.0, 0.7, 0.3))
        assign(box(f"socket{i}", r[0] - 6, r[1] - 6, r[2] + 6, r[3] + 6, -12, -10, bevel=0), flat(f"pit{i}", PAL["pit"]))

    # ---------------------------------------------------------------- the name plate
    if on(8):      # a tall fleshy pocket under an arch, a live portrait glowing in its dark
        assign(box("portback", PORT[0] - 6, PORT[1] - 6, PORT[2] + 6, PORT[3] + 6, -14, -12, bevel=0),
               principled("pocket", "#020101", rough=0.9))
        pcx = (PORT[0] + PORT[2]) / 2
        head = sphere("head", pcx, 18, -9, 15, 24, 8)
        assign(head, principled("headskin", "#2c1420", rough=0.4, coat=1.0, sss=0.4, sss_scale_mm=3.0))
        protected.append(head)
        for sx_ in (-6, 6):
            protected.append(assign(sphere(f"eye{sx_}", pcx + sx_, 22, -1.5, 3.2, 1.8, 1.5), glow(f"eye{sx_}", PAL["life"], 8.0)))
        for k, (dx, ang) in enumerate([(-10, 30), (10, -30)]):
            protected.append(tube(f"crest{k}", [(pcx + dx, 34, -7), (pcx + dx * 1.6, 46, -5), (pcx + dx * 2.0, 54, -4)], 2.4,
                                  [1.0, 0.6, 0.1], principled("crestmat", "#3a2030", rough=0.35, coat=1.0)))
        point("portlight", (pcx, -30, 4), 0.03, 10, (0.5, 1.0, 0.4))    # lit from below, out of the dark
        protected.append(text("name", "VHAAL", pcx, PORT[1] + 9, -9, 8, glow("namegl", PAL["readout"], 3.0), spacing=1.3))
    else:          # version 23: a flat green bar
        assign(box("portback", PORT[0] - 6, PORT[1] - 6, PORT[2] + 6, PORT[3] + 6, -14, -12, bevel=0),
               glow("namebar", V23["lime"], 0.8))
        protected.append(text("name", "VHAAL", (PORT[0] + PORT[2]) / 2, 8, -10, 9, flat("nameink", V23["ink"]), spacing=1.2))

    # ---------------------------------------------------------------- bone spines arching over (step 9)
    if on(9) and not on(15):
        pcx = (PORT[0] + PORT[2]) / 2
        # the canopy: two long spines rising from behind the portrait's arch and curving over it and the screen's corner
        growth.append(tube("canopy0", [(PORT[0] - 14, PORT[3] - 10, 6), (pcx - 10, PORT[3] + 40, 22),
                                       (-60, y1 + 58, 26), (40, y1 + 66, 24), (95, y1 + 84, 20)], 8.0,
                           [1.0, 0.85, 0.6, 0.35, 0.05], bn))
        growth.append(tube("canopy1", [(PORT[2] + 8, PORT[3] + 6, 6), (-110, y1 + 36, 18), (-70, y1 + 44, 20),
                                       (-30, y1 + 56, 18)], 5.0, [1.0, 0.7, 0.4, 0.05], bn))
        # spines off the screen's right corner, swept back (asymmetry: the right side is lighter than the left)
        for k, (dx, dy) in enumerate([(30, 50), (46, 34), (54, 14)]):
            growth.append(tube(f"spine{k}", [(x1 + 16, y1 + 8 - k * 18, 6), (x1 + 16 + dx * 0.55, y1 + 8 - k * 18 + dy * 0.5, 10),
                                             (x1 + 16 + dx, y1 + 8 - k * 18 + dy, 12)], 5.5 - k, [1.0, 0.55, 0.05], bn))
        # claws curling under the key shelf
        for k, cx in enumerate((-110, 108)):
            sgn = -1 if k == 0 else 1
            growth.append(tube(f"claw{k}", [(cx, -84, 6), (cx + 14 * sgn, -100, 10), (cx + 8 * sgn, -116, 12)], 6.0,
                               [1.0, 0.55, 0.05], bn))

    # ---------------------------------------------------------------- the wall of tissue (step 10)
    if on(15) and not on(16):   # step 15: the wall without its placed ribs, tubes and tendril
        bpy.ops.mesh.primitive_plane_add(size=1.0)
        wall = bpy.context.active_object
        wall.scale = ((VIEW[2] - VIEW[0] + 40) * MM, (VIEW[3] - VIEW[1] + 40) * MM, 1.0)
        wall.location = ((VIEW[0] + VIEW[2]) / 2 * MM, (VIEW[1] + VIEW[3]) / 2 * MM, -30 * MM)
        assign(wall, flesh("wallflesh", dark=True))
    if on(10) and not on(15):
        bpy.ops.mesh.primitive_plane_add(size=1.0)
        wall = bpy.context.active_object
        wall.name = "wall"
        wall.scale = ((VIEW[2] - VIEW[0] + 40) * MM, (VIEW[3] - VIEW[1] + 40) * MM, 1.0)
        wall.location = ((VIEW[0] + VIEW[2]) / 2 * MM, (VIEW[1] + VIEW[3]) / 2 * MM, -30 * MM)
        wf = flesh("wallflesh", dark=True)
        assign(wall, wf)
        # the wall is a ribcage: ribs sweeping across behind every panel, every 24 mm, bowed and uneven, thick and thin
        for k in range(14):
            yy = VIEW[1] - 6 + k * 24
            pts, rad = [], []
            for j2 in range(9):
                xx = VIEW[0] - 20 + j2 * (VIEW[2] - VIEW[0] + 40) / 8
                pts.append((xx, yy + 7 * math.sin(j2 * 0.9 + k * 0.7) - 0.04 * (xx - 30), -22))
                rad.append(0.75 + 0.25 * math.sin(j2 * 1.7 + k))
            tube(f"rib{k}", pts, 10.0, rad, wf)
        tube("spinecol", [(-232, VIEW[1] - 10, -16), (-236, 20, -14), (-228, VIEW[3] + 10, -16)], 12.0, [1.0, 1.1, 1.0], wf)
        hide = principled("vein", "#2a0c0c", rough=0.35, coat=1.0, sss=0.3)
        # tubes and veins running between the panels, half sunk in the wall
        for k, pts in enumerate([
            [(-240, 120, -14), (-200, 96, -10), (-160, 110, -12), (-120, 96, -8)],
            [(-160, -110, -12), (-120, -96, -8), (-60, -112, -12), (20, -104, -10), (120, -116, -12), (200, -100, -10)],
            [(135, 100, -10), (170, 120, -12), (215, 112, -12), (260, 130, -12)],
            [(140, -40, -8), (175, -55, -10), (205, -50, -8)],
        ]):
            growth.append(tube(f"pipe{k}", pts, 5.0 - k * 0.6, [1.0] * len(pts), hide))
        growth.append(tube("tendril", [(x1 + 22, 10, 4), (150, 0, 10), (175, -14, 12), (200, -6, 9), (228, 8, 6)], 6.0,
                           [1.0, 0.45, 0.8, 0.35, 0.95], fl))

    # ---------------------------------------------------------------- life: green pustules (step 11)
    if on(11) and not on(15):
        nod = glow("pustule", PAL["life"], 0.8, sss=0.5)
        for k, (nx, ny, r) in enumerate([(x0 - 22, y0 - 8, 6.5), (x0 - 21, y0 + 12, 4.5), (-122, -40, 7),
                                         (140, -60, 7.5), (152, -48, 4.5), (238, -78, 6)]):
            growth.append(assign(sphere(f"pustule{k}", nx, ny, 15, r, r, r * 0.8), nod))
            point(f"pusl{k}", (nx, ny, 16 + r + 3), 0.012 * r, r * 0.5, (0.6, 1.0, 0.3))
    elif not on(2):   # version 23: a small egg in a grey disc for its crest
        growth.append(assign(sphere("disc", 250, 95, 2, 26, 26, 3), grey_metal("disc")))
        growth.append(assign(sphere("egg", 250, 95, 8, 9, 12, 7), glow("egg", V23["egg"], 0.6)))

    # ---------------------------------------------------------------- the hero: a crystal heart (step 12)
    if on(12):
        cz = sculpt.at(200, 92) - 4.0 if on(16) else 18.0     # grown from the sculpted cradle, its root sunk in it
        if on(23):   # frosted, lit from a core inside, a breath of violet mist round it: never a plastic toy
            cr = principled("crystal", "#4c2490", rough=0.5, transmission=1.0, coat=0.5, coat_rough=0.35,
                            emit=PAL["hero"], emit_strength=0.12)
            frost = noise(cr, 900.0, 6.0)                   # frosted, pitted facets
            fb = nt(cr)[0].new("ShaderNodeBump")
            fb.inputs["Strength"].default_value = 0.35
            fb.inputs["Distance"].default_value = 0.3 * MM
            nt(cr)[1].new(frost.outputs["Fac"], fb.inputs["Height"])
            nt(cr)[1].new(fb.outputs["Normal"], nt(cr)[0]["Principled BSDF"].inputs["Normal"])
            core = principled("crystalcore", PAL["hero_hot"], rough=0.5, emit=PAL["hero"], emit_strength=6.0)
        else:
            cr = principled("crystal", PAL["hero"], rough=0.05, transmission=0.6, coat=1.0, emit=PAL["hero"], emit_strength=0.6)
            core = None
        for k, (dx, dy, h, w, rot) in enumerate([(0, 0, 72, 17, 0), (-22, -8, 44, 12, 26), (21, -10, 50, 13, -22),
                                                 (8, -20, 28, 9, -55), (-12, -18, 26, 8, 50)]):
            a = math.radians(rot)
            ux, uy = -math.sin(a), math.cos(a)            # the shard's axis in the view, tilted by rot
            for part, (frac, flip) in (("tip", (1.0, 0)), ("root", (0.35, 1))):
                ln = h * frac
                bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=w * MM, radius2=0, depth=ln * MM)
                c = bpy.context.active_object
                c.name = f"crystal{k}{part}"
                if on(23):   # rule 1: no raw cone. Cut it into an uneven shard: facets of different widths, a broken tip
                    import random
                    rr = random.Random(k * 7 + (part == "root"))
                    bpy.ops.object.mode_set(mode="EDIT")
                    bpy.ops.mesh.subdivide(number_cuts=2)
                    bpy.ops.object.mode_set(mode="OBJECT")
                    for v in c.data.vertices:
                        v.co.x *= rr.uniform(0.7, 1.25)
                        v.co.y *= rr.uniform(0.7, 1.25)
                        v.co.z += rr.uniform(-0.04, 0.04) * ln * MM
                    for f in c.data.polygons:
                        f.use_smooth = False
                off = ln / 2 * (-1 if flip else 1)
                c.location = ((200 + dx + ux * off) * MM, (96 + dy + uy * off) * MM, cz * MM)
                c.rotation_euler = (math.radians(90 if flip else -90), 0, a)
                growth.append(assign(c, cr))
                if core is not None and part == "tip":     # a core of light inside each shard, a third its size
                    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=w * 0.35 * MM, radius2=0, depth=ln * 0.7 * MM)
                    ci = bpy.context.active_object
                    ci.name = f"crystalcore{k}"
                    ci.location = ((200 + dx + ux * off * 0.8) * MM, (96 + dy + uy * off * 0.8) * MM, cz * MM)
                    ci.rotation_euler = c.rotation_euler
                    assign(ci, core)
        if on(23):
            bpy.ops.mesh.primitive_uv_sphere_add(radius=46 * MM, location=(200 * MM, 112 * MM, (cz + 6) * MM))
            mist = bpy.context.active_object
            mist.name = "mist"
            mm = bpy.data.materials.new("mist")
            mm.use_nodes = True
            mn = mm.node_tree.nodes
            mn.remove(mn["Principled BSDF"])
            vol = mn.new("ShaderNodeVolumePrincipled")
            vol.inputs["Color"].default_value = srgb("#b07cff")
            vol.inputs["Density"].default_value = 0.6
            mm.node_tree.links.new(vol.outputs[0], mn["Material Output"].inputs["Volume"])
            assign(mist, mm)
        kp = 0.55 if on(23) else 1.0
        point("heart", (200, 120, cz + 8), 0.3 * kp, 6, (0.7, 0.4, 1.0))
        point("heartspill", (200, 80, cz + 12), 0.25 * kp, 30, (0.75, 0.45, 1.0))
        if not on(16):
            cb = Body("cradle")
            cb.el("BALL", 200, 84, 22)
            cb.el("CAPSULE", 200, 80, 13, sx=34)
            cb.el("BALL", 230, 70, 15)
            growth.append(assign(cb.mesh(), fl))

    # ---------------------------------------------------------------- light
    if on(5):
        kw = KEY_W * (0.7 if on(23) else 1.0)   # step 23: dark, so the screens and keys carry the light
        area("key", (-40, 420, 260), (0, 0, 0), kw, 180, (1.0, 0.82, 0.62))       # warm, from above
        area("rimL", (-520, 60, -20), (-150, 20, 10), kw * 0.3, 200, (1.0, 0.72, 0.55))  # grazes the wet ridges
        area("rimR", (560, 40, -10), (200, 20, 10), kw * 0.2, 200, (0.85, 0.75, 1.0))
        area("fill", (100, -420, 300), (0, 0, 0), kw * 0.03, 600, (0.45, 0.55, 1.0))  # dim and cool, hollows stay deep
    else:
        sun = bpy.data.lights.new("front", "SUN")
        sun.energy = 1.4
        sun.angle = math.radians(20)
        so = link(bpy.data.objects.new("front", sun))
        so.rotation_euler = (math.radians(8), math.radians(-6), 0)
    return growth, protected


def measure(growth):
    """The fit measure (rule G1): the nearest any growth in front of the screen glass comes to each protected
    rectangle (the screen, each key face, the portrait window), in mm; negative means it covers it."""
    dg = bpy.context.evaluated_depsgraph_get()
    pts = []
    for ob in growth:
        ev = ob.evaluated_get(dg)
        me = ev.to_mesh()
        mw = ob.matrix_world
        pts += [(ob.name, mw @ v.co) for v in me.vertices]
        ev.to_mesh_clear()
    out = {}
    for label, (rx0, ry0, rx1, ry1) in [("screen", WIN), ("portrait window", PORT)] + [(f"key {i + 1} face", k) for i, k in enumerate(KEYS)]:
        best, who = 1e9, None
        for name, p in pts:
            if p.z / MM <= SCREEN_Z + 0.5:
                continue
            x, y = p.x / MM, p.y / MM
            dx, dy = max(rx0 - x, 0, x - rx1), max(ry0 - y, 0, y - ry1)
            d = math.hypot(dx, dy) if (dx or dy) else -min(x - rx0, rx1 - x, y - ry0, ry1 - y)
            if d < best:
                best, who = d, name
        out[label] = {"clearance_mm": round(best, 2), "nearest": who, "pass": best >= 0.0}
    return out


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--steps", default=",".join(str(s[0]) for s in STEPS))
    ap.add_argument("--samples", type=int, default=48)
    ap.add_argument("--out", default=os.path.join(ROOT, "docs", "design", "organic-bible"))
    a = ap.parse_args(argv)
    os.makedirs(a.out, exist_ok=True)
    want = {int(s) for s in a.steps.split(",")}
    for n, fname, title in STEPS:
        if n not in want:
            continue
        sc = reset(a.samples)
        LAST.clear()
        growth, _protected = build(n)
        sc.render.filepath = os.path.join(a.out, f"{n:02d}-{fname}.png")
        bpy.ops.render.render(write_still=True)
        print(f"STUDY step {n} {title}: {sc.render.filepath}", flush=True)
        if n == STEPS[-1][0]:
            fit = measure([g for g in growth if g.name != "sculpt"])
            if LAST.get("sculpt") is not None:   # the sculpted surface, measured on its height field
                for label, d in measure_field(LAST["sculpt"]).items():
                    if d < fit[label]["clearance_mm"]:
                        fit[label] = {"clearance_mm": d, "nearest": "sculpt", "pass": d >= 0.0}
            with open(os.path.join(a.out, "fit.json"), "w") as f:
                json.dump(fit, f, indent=2)
            print("STUDY fit", json.dumps(fit), flush=True)


if __name__ == "__main__":
    main()
