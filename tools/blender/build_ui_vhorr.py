"""Fleet Command: the Vhorr Brood console skin, modelled, shaded and rendered in Blender.

Owns every piece of the Vhorr skin of the shared console UI kit, organic throughout (owner, 2026-10-30:
"Vhor needs special treatment with organic materials and shaders, and organic looking modeling, things
like slimey leather, tendrils, claws, carapaces, fibers, could help sell the alien hive mind aesthetic").
Nothing here is cut or bolted: frames are bands of leathery hide with bioluminescence glowing through
them between dark veins, wet and slimy under a clear coat; corners are grown over by glossy iridescent
carapace claws; tendrils with subsurface flesh curl out of them; sinew fibres run along the cords; keys are
blister pods whose membranes glow from within. Rendered face on in Cycles through the shared harness
(tools/blender/ui_render.py), at twice the slot size, with a manifest of sizes and margins.

    /opt/blender/blender --background --factory-startup --python tools/blender/build_ui_vhorr.py -- \
        --out assets/ui/vhorr [--only button.idle,crest] [--samples 48]

Units: 1 px of the 1x slot is 1 mm; the builders take slot pixels with y running down (as the image does).
9-slice pieces keep their detail (claws, knuckles, bulbs) inside the margins and one cross-section along
their edges; their shaders use object-space noise, which a stretched edge draws out along its length like
the grain of a sinew, so the hide's features are kept long and soft. Deterministic: fixed seeds only.
Vhorr design language: openspec/changes/console-skins/design.md. Our own design throughout.
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import panel_kit  # noqa: E402  (srgb, camera, reset_scene)
import ui_render  # noqa: E402  (the render harness every Blender skin shares)

MM = ui_render.MM
srgb = panel_kit.srgb

# ----------------------------------------------------------------------------- materials
# Hunger, growth, the swarm: dark chitin, leather hide, flesh, bile light, slime, blood.
PAL = dict(chitin="#2b1d2a", chitin_hi="#5a3a5e", hide="#5a3424", hide_dark="#2e1814", flesh="#c4566a", bile="#a6e34a",
           bile_hot="#e4ff9a", sinew="#d9a8a0", pit="#120a10", blood="#e0342a", amber="#f0b43c")


class NodeKit:
    """A small helper for building one material's node tree."""

    def __init__(self, name):
        self.mat = bpy.data.materials.new(name)
        self.mat.use_nodes = True
        self.nt = self.mat.node_tree
        for n in list(self.nt.nodes):
            self.nt.nodes.remove(n)
        self.N, self.L = self.nt.nodes, self.nt.links
        self.out = self.N.new("ShaderNodeOutputMaterial")
        self.bsdf = self.N.new("ShaderNodeBsdfPrincipled")
        self.L.new(self.bsdf.outputs[0], self.out.inputs[0])
        self.co = self.N.new("ShaderNodeTexCoord").outputs["Object"]

    def warped(self, scale, amount):
        """Object coordinates pushed about by low-frequency noise: organic patterns stop looking tiled."""
        n = self.N.new("ShaderNodeTexNoise")
        n.inputs["Scale"].default_value = scale
        n.inputs["Detail"].default_value = 3.0
        self.L.new(self.co, n.inputs["Vector"])
        sub = self.N.new("ShaderNodeVectorMath")
        sub.operation = "SUBTRACT"
        self.L.new(n.outputs["Color"], sub.inputs[0])
        sub.inputs[1].default_value = (0.5, 0.5, 0.5)
        sc = self.N.new("ShaderNodeVectorMath")
        sc.operation = "SCALE"
        self.L.new(sub.outputs[0], sc.inputs[0])
        sc.inputs["Scale"].default_value = amount
        add = self.N.new("ShaderNodeVectorMath")
        add.operation = "ADD"
        self.L.new(self.co, add.inputs[0])
        self.L.new(sc.outputs[0], add.inputs[1])
        return add.outputs[0]

    def tex(self, kind, scale, vec=None, **kw):
        t = self.N.new(kind)
        t.inputs["Scale"].default_value = scale
        for k, v in kw.items():
            if k in t.inputs:
                t.inputs[k].default_value = v
            else:
                setattr(t, k, v)
        self.L.new(vec or self.co, t.inputs["Vector"])
        return t

    def math(self, op, a, b=None, clamp=False):
        m = self.N.new("ShaderNodeMath")
        m.operation = op
        m.use_clamp = clamp
        for i, v in enumerate((a, b)):
            if v is None:
                continue
            if isinstance(v, (int, float)):
                m.inputs[i].default_value = v
            else:
                self.L.new(v, m.inputs[i])
        return m.outputs[0]

    def ramp(self, fac, a, b, lo=0.0, hi=1.0):
        """Map fac through [lo, hi] onto the colours a to b."""
        r = self.N.new("ShaderNodeValToRGB")
        r.color_ramp.elements[0].position, r.color_ramp.elements[1].position = lo, hi
        r.color_ramp.elements[0].color = list(srgb(a)) + [1.0]
        r.color_ramp.elements[1].color = list(srgb(b)) + [1.0]
        self.L.new(fac, r.inputs[0])
        return r.outputs[0]

    def bump(self, height, strength, distance=0.0006):
        b = self.N.new("ShaderNodeBump")
        b.inputs["Strength"].default_value = strength
        b.inputs["Distance"].default_value = distance
        self.L.new(height, b.inputs["Height"])
        self.L.new(b.outputs[0], self.bsdf.inputs["Normal"])
        return b

    def set(self, **kw):
        for k, v in kw.items():
            sock = self.bsdf.inputs[k.replace("_", " ")]
            if isinstance(v, str):
                sock.default_value = list(srgb(v)) + [1.0]
            elif isinstance(v, (tuple, list)):
                sock.default_value = v
            elif isinstance(v, (int, float)):
                sock.default_value = v
            else:
                self.L.new(v, sock)


def hide_mat(name, glow, glow_col="#b6d86a"):
    """Leather lit from behind: dark mottled hide with thin wandering veins, pores and wrinkles in the
    bump, flesh scattering under the skin, a wet clear coat; light comes through only where the skin is
    thin (low-frequency patches), warming the hide toward flesh before it glows bile green."""
    k = NodeKit(name)
    wv = k.warped(60.0, 0.006)
    cells = k.tex("ShaderNodeTexVoronoi", 150.0, vec=wv, feature="DISTANCE_TO_EDGE")
    vein = k.math("POWER", k.math("SUBTRACT", 1.0, k.math("DIVIDE", cells.outputs["Distance"], 0.035, clamp=True)), 4.0)
    thin = k.tex("ShaderNodeTexNoise", 22.0, **{"Detail": 3.0, "Roughness": 0.5})
    patch = k.math("POWER", k.math("MULTIPLY", k.math("SUBTRACT", thin.outputs["Fac"], 0.5, clamp=True), 3.5, clamp=True), 1.4)
    mott = k.tex("ShaderNodeTexNoise", 70.0, **{"Detail": 8.0, "Roughness": 0.62})
    base = k.ramp(mott.outputs["Fac"], "#1e0e0a", PAL["hide"], 0.3, 0.72)
    warm = k.N.new("ShaderNodeMix")
    warm.data_type = "RGBA"
    k.L.new(patch, warm.inputs["Factor"])
    k.L.new(base, warm.inputs[6])
    warm.inputs[7].default_value = list(srgb("#9a4a30")) + [1.0]
    mixv = k.N.new("ShaderNodeMix")
    mixv.data_type = "RGBA"
    k.L.new(vein, mixv.inputs["Factor"])
    k.L.new(warm.outputs[2], mixv.inputs[6])
    mixv.inputs[7].default_value = list(srgb("#2a060c")) + [1.0]
    k.set(Base_Color=mixv.outputs[2], Roughness=0.5, Subsurface_Weight=0.3, Subsurface_Scale=0.002,
          Subsurface_Radius=(1.0, 0.35, 0.2), Coat_Weight=0.8, Coat_Roughness=0.1, Sheen_Weight=0.3)
    if glow > 0:
        k.set(Emission_Color=glow_col, Emission_Strength=k.math("MULTIPLY", k.math("MULTIPLY", patch, k.math("SUBTRACT", 1.0, vein)), glow))
    pores = k.tex("ShaderNodeTexVoronoi", 520.0, **{"Randomness": 1.0})
    wr = k.tex("ShaderNodeTexNoise", 140.0, vec=wv, **{"Detail": 10.0, "Roughness": 0.7})
    k.bump(k.math("ADD", k.math("MULTIPLY", pores.outputs["Distance"], 0.5), k.math("ADD", wr.outputs["Fac"], k.math("MULTIPLY", vein, -0.6))), 0.45)
    return k.mat


def chitin_mat(name):
    """Wet carapace: near-black purple, cracked into plates (Voronoi), a thin-film sheen that turns
    oily green and violet at an angle, glassy under its coat."""
    k = NodeKit(name)
    plates = k.tex("ShaderNodeTexVoronoi", 55.0, feature="DISTANCE_TO_EDGE")
    tone = k.tex("ShaderNodeTexNoise", 30.0, **{"Detail": 4.0})
    k.set(Base_Color=k.ramp(tone.outputs["Fac"], "#120a12", PAL["chitin_hi"], 0.35, 0.75), Roughness=0.28, Coat_Weight=1.0,
          Coat_Roughness=0.03, Thin_Film_Thickness=380.0, Thin_Film_IOR=1.45, Specular_IOR_Level=0.6)
    crack = k.math("SUBTRACT", 1.0, k.math("DIVIDE", plates.outputs["Distance"], 0.025, clamp=True))
    k.bump(k.math("SUBTRACT", tone.outputs["Fac"], k.math("MULTIPLY", crack, 0.8)), 0.45)
    return k.mat


def flesh_mat(name, col=PAL["flesh"], glow=0.0):
    """Tendril flesh: pink-red, scattering light deep under the surface, glistening."""
    k = NodeKit(name)
    n = k.tex("ShaderNodeTexNoise", 90.0, **{"Detail": 5.0})
    k.set(Base_Color=k.ramp(n.outputs["Fac"], "#6a1c2a", col, 0.25, 0.75), Roughness=0.35, Subsurface_Weight=0.75,
          Subsurface_Scale=0.004, Subsurface_Radius=(1.0, 0.3, 0.25), Coat_Weight=0.9, Coat_Roughness=0.06)
    rings = k.tex("ShaderNodeTexWave", 260.0, **{"Distortion": 4.0, "Detail": 2.0})
    k.bump(rings.outputs["Fac"], 0.2)
    if glow:
        k.set(Emission_Color=PAL["bile"], Emission_Strength=glow)
    return k.mat


def membrane_mat(name, glow, col=PAL["bile"]):
    """A blister's skin: a dark translucent membrane over a glowing core. The glow is strongest where
    the membrane faces the viewer and fades to dark at its swollen rim; veins shadow it."""
    k = NodeKit(name)
    wv = k.warped(80.0, 0.004)
    cells = k.tex("ShaderNodeTexVoronoi", 170.0, vec=wv, feature="DISTANCE_TO_EDGE")
    vein = k.math("POWER", k.math("SUBTRACT", 1.0, k.math("DIVIDE", cells.outputs["Distance"], 0.03, clamp=True)), 3.0)
    lw = k.N.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.5
    core = k.math("POWER", k.math("SUBTRACT", 1.0, lw.outputs["Facing"]), 5.0)
    mixv = k.N.new("ShaderNodeMix")
    mixv.data_type = "RGBA"
    k.L.new(vein, mixv.inputs["Factor"])
    mixv.inputs[6].default_value = list(srgb("#4a1c18")) + [1.0]
    mixv.inputs[7].default_value = list(srgb("#16040a")) + [1.0]
    k.set(Base_Color=mixv.outputs[2], Roughness=0.3, Subsurface_Weight=0.5, Subsurface_Scale=0.003, Subsurface_Radius=(1.0, 0.45, 0.3),
          Coat_Weight=1.0, Coat_Roughness=0.03, Emission_Color=col,
          Emission_Strength=k.math("MULTIPLY", k.math("MULTIPLY", core, k.math("SUBTRACT", 1.0, vein)), glow))
    n = k.tex("ShaderNodeTexNoise", 260.0, **{"Detail": 5.0})
    k.bump(k.math("SUBTRACT", n.outputs["Fac"], vein), 0.25)
    return k.mat


def wet_mat(name, col=PAL["pit"]):
    """The wet dark pit inside a frame: almost black, glossy, gently lumpy."""
    k = NodeKit(name)
    n = k.tex("ShaderNodeTexNoise", 25.0, **{"Detail": 3.0})
    k.set(Base_Color=col, Roughness=0.25, Coat_Weight=1.0, Coat_Roughness=0.04)
    k.bump(n.outputs["Fac"], 0.25, 0.002)
    return k.mat


def glow_mat(name, col, strength):
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=0.2, Coat_Weight=1.0, Coat_Roughness=0.03, Emission_Color=col, Emission_Strength=strength, Subsurface_Weight=0.3)
    return k.mat


# ----------------------------------------------------------------------------- organic modelling

class Organic:
    """One piece being grown: w x h slot pixels (1 px = 1 mm). Shapes are smooth tubes (curves with a
    round bevel and a radius per point), ellipsoids and filled curve slabs; nothing has a hard edge."""

    def __init__(self, name, w, h, hero):
        self.name, self.w, self.h, self.n = name, w, h, 0
        self.coll = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(self.coll)
        self.mats = {}

    def mat(self, key, make):
        if key not in self.mats:
            self.mats[key] = make()
        return self.mats[key]

    def P(self, x, y, z=0.0):
        return ((x - self.w / 2) * MM, (self.h / 2 - y) * MM, z * MM)

    def _link(self, data, m):
        self.n += 1
        ob = bpy.data.objects.new(f"{self.name}_{self.n}", data)
        if m is not None:
            ob.data.materials.append(m)
        self.coll.objects.link(ob)
        return ob

    def tube(self, pts, radii, m, flat=0.7, z=None, cyclic=False, res=12):
        """A smooth tube through pts (slot px), radius per point in px, flattened in z by flat."""
        cu = bpy.data.curves.new(f"t{self.n}", "CURVE")
        cu.dimensions = "3D"
        cu.bevel_depth = MM
        cu.bevel_resolution = 5
        cu.resolution_u = res
        cu.use_fill_caps = True
        sp = cu.splines.new("BEZIER")
        sp.bezier_points.add(len(pts) - 1)
        zs = z or [0.0] * len(pts)
        for bp, (x, y), r, zz in zip(sp.bezier_points, pts, radii, zs):
            bp.co = self.P(x, y, zz)
            bp.handle_left_type = bp.handle_right_type = "AUTO"
            bp.radius = r
        sp.use_cyclic_u = cyclic
        sp.use_smooth = True
        ob = self._link(cu, m)
        ob.scale.z = flat
        return ob

    def blob(self, x, y, rx, ry, rz, m, z=0.0, rot=0.0, lumps=0.0, seed=0, seg=40):
        """A smooth ellipsoid (radii in px), turned rot radians about z, optionally lumpy."""
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=seg // 2, radius=1.0)
        for f in bm.faces:
            f.smooth = True
        me = bpy.data.meshes.new(f"b{self.n}")
        bm.to_mesh(me)
        bm.free()
        ob = self._link(me, m)
        ob.location = self.P(x, y, z)
        ob.rotation_euler = (0.0, 0.0, -rot)
        ob.scale = (rx * MM, ry * MM, rz * MM)
        if lumps:
            tx = bpy.data.textures.new(f"lump{self.n}", "CLOUDS")
            tx.noise_scale = 0.6
            tx.noise_depth = 2
            d = ob.modifiers.new("lumps", "DISPLACE")
            d.texture = tx
            d.texture_coords = "LOCAL"
            d.strength = lumps
            d.mid_level = 0.5
            ob.modifiers.new("smooth", "SUBSURF").levels = 1
            tx.intensity = 1.0 + (seed % 7) * 0.02
        return ob

    def slab(self, outlines, z0, z1, m, bevel=2.0):
        """A rounded slab: closed outlines (slot px; the first is the outside, the rest holes), extruded
        between z0 and z1 mm with a round bevel on every edge."""
        cu = bpy.data.curves.new(f"s{self.n}", "CURVE")
        cu.dimensions = "2D"
        cu.fill_mode = "BOTH"
        cu.extrude = max(0.0, (z1 - z0) / 2 - bevel) * MM
        cu.bevel_depth = bevel * MM
        cu.bevel_resolution = 4
        for pts in outlines:
            sp = cu.splines.new("POLY")
            sp.points.add(len(pts) - 1)
            for p, (x, y) in zip(sp.points, pts):
                X, Y, _ = self.P(x, y)
                p.co = (X, Y, 0.0, 1.0)
            sp.use_cyclic_u = True
            sp.use_smooth = True
        ob = self._link(cu, m)
        ob.location.z = (z0 + z1) / 2 * MM
        return ob


def rrect(x0, y0, x1, y1, r, seg=10):
    """A rounded rectangle as points (slot px)."""
    out = []
    for cx, cy, a0 in ((x1 - r, y0 + r, -90), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90), (x0 + r, y0 + r, 180)):
        for i in range(seg + 1):
            a = math.radians(a0 + 90 * i / seg)
            out.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return out


def circle(cx, cy, r, seg=48):
    return [(cx + math.cos(2 * math.pi * i / seg) * r, cy + math.sin(2 * math.pi * i / seg) * r) for i in range(seg)]


def curl(cx, cy, r, a0, turns, dirn=1, n=7):
    """A tendril's curl: a tightening spiral (slot px)."""
    return [(cx + math.cos(a0 + dirn * t / (n - 1) * turns * 2 * math.pi) * r * (1 - 0.7 * t / (n - 1)),
             cy + math.sin(a0 + dirn * t / (n - 1) * turns * 2 * math.pi) * r * (1 - 0.7 * t / (n - 1))) for t in range(n)]


def taper(n, r0, r1):
    return [r0 + (r1 - r0) * i / (n - 1) for i in range(n)]


# ----------------------------------------------------------------------------- shared assemblies

def M(K, key):
    """The skin's materials by role, made once per piece."""
    makers = {
        "hide": lambda: hide_mat("hide", 1.6), "hide_dim": lambda: hide_mat("hide_dim", 0.7), "hide_hot": lambda: hide_mat("hide_hot", 3.0),
        "chitin": lambda: chitin_mat("chitin"), "flesh": lambda: flesh_mat("flesh"), "flesh_dark": lambda: flesh_mat("flesh_dark", "#5a2430"),
        "sinew": lambda: flesh_mat("sinew", PAL["sinew"]), "pit": lambda: wet_mat("pit"),
        "pod_idle": lambda: membrane_mat("pod_idle", 0.3), "pod_hover": lambda: membrane_mat("pod_hover", 1.6),
        "pod_hot": lambda: membrane_mat("pod_hot", 2.4, PAL["bile_hot"]), "pod_blood": lambda: membrane_mat("pod_blood", 2.6, PAL["blood"]),
        "bile": lambda: glow_mat("bile", PAL["bile"], 2.2), "blood": lambda: glow_mat("blood", PAL["blood"], 2.2),
        "amber": lambda: glow_mat("amber", PAL["amber"], 2.2), "vessel": lambda: wet_mat("vessel", "#1c0d10"),
    }
    return K.mat(key, makers[key])


def claw(K, x, y, sx, sy, size):
    """Carapace grown over a corner: a domed knuckle plate, two long curved horns sweeping out along
    the two edges and tapering to points, and a tendril curling out from beneath them."""
    ch = M(K, "chitin")
    K.blob(x + sx * size * 0.3, y + sy * size * 0.3, size * 0.36, size * 0.3, size * 0.16, ch, z=12.0, rot=sx * sy * 0.78, lumps=0.1)
    for along in (0, 1):
        pts = [(x + sx * size * 0.22, y + sy * size * 0.22)]
        for f in (0.55, 0.9):
            pts.append((x + sx * size * (f if along == 0 else 0.1 + 0.05 * f), y + sy * size * (0.1 + 0.05 * f if along == 0 else f)))
        K.tube(pts, [size * 0.16, size * 0.1, size * 0.015], ch, flat=0.55, z=[13.0, 12.0, 11.0])
    c = curl(x + sx * size * 0.62, y + sy * size * 0.62, size * 0.28, math.atan2(sy, sx), 0.8, 1 if sx * sy > 0 else -1)
    K.tube(c, taper(len(c), size * 0.1, size * 0.02), M(K, "flesh"), flat=0.8, z=[15.0] * len(c))


def band(K, x0, y0, x1, y1, r, width, thick, hide_key="hide"):
    """A band of hide round a rectangle: a rounded ring slab, with sinew fibres running along it."""
    outer = rrect(x0, y0, x1, y1, r)
    inner = rrect(x0 + width, y0 + width, x1 - width, y1 - width, max(2.0, r - width))
    K.slab([outer, inner], 0.0, thick, M(K, hide_key), bevel=min(thick / 2 - 0.2, width / 2.5))
    for f in (0.3, 0.7):
        d = width * f
        K.tube(rrect(x0 + d, y0 + d, x1 - d, y1 - d, max(2.0, r - d), 6), [1.0] * 28, M(K, "sinew"), flat=0.5, z=[thick + 0.4] * 28, cyclic=True)


def pod(K, x0, x1, cy, rad, key, z=3.0, flat=0.55, bulb=1.3):
    """A blister pod from x0 to x1 (px) along row cy: a membrane capsule with a swollen bulb at its
    left end, lit from within; a stalk feeds it from below the bulb."""
    m = M(K, key)
    K.tube([(x0 + rad * bulb * 0.6, cy), ((x0 + x1) / 2, cy), (x1 - rad, cy)], [rad, rad, rad], m, flat=flat, z=[z, z, z])
    K.blob(x0 + rad * bulb * 0.8, cy, rad * bulb, rad * bulb * 0.95, rad * bulb * flat, m, z=z)
    K.blob(x1 - rad * 0.95, cy, rad * 0.95, rad * 0.95, rad * flat, m, z=z)
    K.tube([(x0 - 2, cy + rad * 1.4), (x0 + 4, cy + rad * 0.9), (x0 + rad * 0.9, cy + rad * 0.3)], [rad * 0.3, rad * 0.24, rad * 0.16], M(K, "flesh_dark"), flat=0.7)


# ----------------------------------------------------------------------------- the pieces

def frame_panel(K):
    w, h = K.w, K.h
    K.slab([rrect(14, 14, w - 14, h - 14, 14)], -2.0, 0.0, M(K, "pit"), bevel=0.8)
    band(K, 3, 3, w - 3, h - 3, 22, 24, 9.0)
    for (x, y, sx, sy) in ((2, 2, 1, 1), (w - 2, 2, -1, 1), (2, h - 2, 1, -1), (w - 2, h - 2, -1, -1)):
        claw(K, x, y, sx, sy, 38)


def frame_inset(K):
    w, h = K.w, K.h
    K.slab([rrect(6, 6, w - 6, h - 6, 8)], -2.0, 0.0, M(K, "pit"), bevel=0.6)
    band(K, 2, 2, w - 2, h - 2, 12, 9, 5.0, "hide_dim")


def bar_track(K):
    w, h = K.w, K.h
    K.tube([(4, h / 2), (w / 2, h / 2), (w - 4, h / 2)], [6.5, 6.5, 6.5], M(K, "vessel"), flat=0.45)
    K.tube([(3, h / 2), (w / 2, h / 2), (w - 3, h / 2)], [7.2, 7.2, 7.2], M(K, "flesh_dark"), flat=0.12, z=[-1.5] * 3)


def bar_fill(key):
    def build(K):
        w, h = K.w, K.h
        K.tube([(4, h / 2), (w / 2, h / 2), (w - 4, h / 2)], [4.2, 4.2, 4.2], M(K, key), flat=0.5)
    return build


def button(state):
    def build(K):
        w, h = K.w, K.h
        key = {"idle": "pod_idle", "hover": "pod_hover", "pressed": "pod_hot"}[state]
        squash = 0.6 if state == "pressed" else 0.85
        pod(K, 8, w - 6, h / 2, 18 if state != "pressed" else 17, key, flat=squash)
    return build


def tab(active):
    def build(K):
        w, h = K.w, K.h
        ch = M(K, "chitin")
        K.tube([(14, h / 2), (w / 2, h / 2), (w - 14, h / 2)], [12, 12, 12], ch, flat=0.42)
        K.blob(14, h / 2, 13, 12, 5.5, ch)
        K.blob(w - 14, h / 2, 13, 12, 5.5, ch)
        K.tube([(16, h / 2 - 4), (w / 2, h / 2 - 5), (w - 16, h / 2 - 4)], [2.5, 2.5, 2.5], ch, flat=1.0, z=[5.0] * 3)
        if active:
            K.tube([(20, h / 2 + 6), (w / 2, h / 2 + 7), (w - 20, h / 2 + 6)], [2.2, 2.2, 2.2], M(K, "bile"), flat=1.0, z=[5.5] * 3)
    return build


def strip(bottom):
    def build(K):
        w, h = K.w, K.h
        y = 12 if bottom else h - 12
        K.tube([(60, y), (w / 2, y), (w - 60, y)], [8, 8, 8], M(K, "hide"), flat=0.6)
        for dy in (-3.5, 0.0, 3.5):
            K.tube([(62, y + dy), (w / 2, y + dy), (w - 62, y + dy)], [0.7] * 3, M(K, "sinew"), flat=0.6, z=[4.6] * 3)
        for x, s in ((2, 1), (w - 2, -1)):
            ch = M(K, "chitin")
            sy = 1 if bottom else -1
            for i, (dx, dyy, rx, ry, rot) in enumerate(((40, 0, 46, 14, 0.0), (68, 10, 26, 9, 0.35), (26, 18, 18, 12, 0.6))):
                K.blob(x + s * dx, y + sy * dyy, rx, ry, 7.0, ch, z=3.0 + i, rot=s * sy * rot, lumps=0.1, seed=i)
            c = curl(x + s * 74, y + sy * 26, 12, math.pi / 2 * sy, 0.8, s * sy)
            K.tube(c, taper(len(c), 3.2, 0.6), M(K, "flesh"), flat=0.8, z=[6.0] * len(c))
    return build


def divider(K):
    w, h = K.w, K.h
    K.tube([(10, h / 2), (w / 2, h / 2), (w - 10, h / 2)], [2.2, 2.2, 2.2], M(K, "flesh_dark"), flat=0.7)
    for x in (6, w - 6):
        K.blob(x, h / 2, 4.6, 4.6, 3.0, M(K, "pod_idle"))


def nameplate(K):
    w, h = K.w, K.h
    lens = [(30 + (w - 60) * i / 40, h / 2 - (h / 2 - 3) * math.sin(math.pi * i / 40) ** 0.25) for i in range(41)]
    lens += [(x, h - y) for x, y in reversed(lens)]
    K.slab([lens], 0.0, 7.0, M(K, "hide"), bevel=3.0)
    K.slab([rrect(70, 10, 390, h - 10, 18)], 6.0, 7.5, M(K, "pit"), bevel=1.0)
    for x, s in ((4, 1), (w - 4, -1)):
        for i, (dx, rx, ry, rot) in enumerate(((22, 30, 13, 0.35), (22, 30, 13, -0.35), (40, 16, 9, 0.0))):
            K.blob(x + s * dx, h / 2 + (i - 1) * 0 + (-8 if i == 0 else 8 if i == 1 else 0), rx, ry, 6.0, M(K, "chitin"), z=7.0 + i, rot=s * rot, lumps=0.1, seed=i)


def helm_pad(K):
    w, h = K.w, K.h
    c = w / 2
    K.slab([circle(c, c, 132)], 0.0, 8.0, M(K, "hide_dim"), bevel=3.5)
    for i in range(16):
        a = i * math.pi / 8
        K.blob(c + math.cos(a) * 136, c + math.sin(a) * 136, 13, 6, 5, M(K, "chitin"), z=4.0, rot=a, lumps=0.1, seed=i)
    for i in range(8):
        a = -math.pi / 2 + i * math.pi / 4
        p = [(c + math.cos(a) * 38, c + math.sin(a) * 38), (c + math.cos(a + 0.12) * 60, c + math.sin(a + 0.12) * 60), (c + math.cos(a) * 82, c + math.sin(a) * 82)]
        K.tube(p, [5, 4, 3.4], M(K, "flesh_dark"), flat=0.6, z=[8.0] * 3)
    for i in range(8):
        a = -math.pi / 2 + i * math.pi / 4
        K.blob(c + math.cos(a) * 100, c + math.sin(a) * 100, 27, 20, 11, M(K, "pod_idle"), z=9.0, rot=a, lumps=0.06, seed=i)
    K.blob(c, c, 38, 36, 16, M(K, "pod_blood"), z=10.0, lumps=0.08, seed=9)


def ssd_bezel(K):
    w, h = K.w, K.h
    K.slab([rrect(12, 12, w - 12, h - 12, 22)], -2.0, 0.0, M(K, "pit"), bevel=0.8)
    band(K, 3, 3, w - 3, h - 3, 28, 18, 8.0)
    for s in (1, -1):
        for i in range(5):
            y = 54 + i * 48
            x0 = 6 if s > 0 else w - 6
            K.tube([(x0, y - 2), (x0 + s * 22, y - 8), (x0 + s * 40, y + 2)], [6, 4.6, 1.6], M(K, "chitin"), flat=0.6, z=[8.0] * 3)
    for (x, y, sx, sy) in ((2, 2, 1, 1), (w - 2, 2, -1, 1), (2, h - 2, 1, -1), (w - 2, h - 2, -1, -1)):
        claw(K, x, y, sx, sy, 44)


def dial(K):
    w, h = K.w, K.h
    c = w / 2
    K.slab([circle(c, c, 58), circle(c, c, 44)], 0.0, 8.0, M(K, "chitin"), bevel=3.0)
    K.slab([circle(c, c, 46)], -1.0, 3.0, M(K, "hide_dim"), bevel=1.5)
    K.slab([circle(c, c, 36)], 2.5, 3.5, M(K, "pit"), bevel=0.5)


def lamp(key):
    def build(K):
        w, h = K.w, K.h
        c = w / 2
        K.blob(c, c + 14, 26, 12, 6, M(K, "chitin"), lumps=0.1)
        K.blob(c, c - 4, 19, 22, 14, M(K, key), z=4.0, lumps=0.04)
    return build


def crest(K):
    w, h = K.w, K.h
    c = w / 2
    cl = curl(c, c + 8, 36, 0.4, 1.0, 1, 9)
    K.tube(cl, taper(9, 6.5, 1.0), M(K, "flesh"), flat=0.7)
    K.blob(c, c, 13, 17, 10, M(K, "pod_hot"), z=6.0)
    for i in range(3):
        a = -math.pi / 2 + i * 2 * math.pi / 3
        p = [(c + math.cos(a) * 44, c + math.sin(a) * 44), (c + math.cos(a + 0.35) * 30, c + math.sin(a + 0.35) * 30), (c + math.cos(a + 0.55) * 16, c + math.sin(a + 0.55) * 16)]
        K.tube(p, [7, 5.5, 1.2], M(K, "chitin"), flat=0.6, z=[8.0] * 3)


# Icons: small organic sculptures, lit with bile so they read at 48 px.
def icon(name):
    def build(K):
        c = 24
        fl, bl, ch = M(K, "flesh"), M(K, "bile"), M(K, "chitin")
        if name == "helm":
            p = curl(26, 22, 18, math.pi * 0.9, 0.85, 1, 8)
            K.tube(p, taper(8, 5, 1.2), fl)
            K.blob(p[0][0], p[0][1], 4, 4, 3, bl)
        elif name == "fire":
            K.tube([(6, 10), (16, 19), (22, 24)], [4, 3, 2], fl)
            K.tube([(6, 38), (16, 29), (22, 24)], [4, 3, 2], fl)
            K.blob(32, 24, 6, 6, 4, bl)
            K.blob(42, 22, 3, 3, 2, bl)
        elif name == "shields":
            for i in range(3):
                K.blob(14 + i * 9, 30 - i * 7, 12, 8, 4, ch, z=i, rot=0.5, lumps=0.05)
        elif name == "tractor":
            K.tube([(4, 10), (16, 18), (24, 24)], [3.4, 2.6, 1.6], fl)
            K.tube([(4, 38), (16, 30), (24, 26)], [3.4, 2.6, 1.6], fl)
            K.blob(34, 24, 7, 7, 5, M(K, "pod_hover"))
        elif name == "transport":
            for i, (x, y, r) in enumerate(((14, 12, 4), (30, 10, 3), (22, 22, 5), (36, 26, 3.5), (12, 32, 3), (26, 38, 4))):
                K.blob(x, y, r, r, r * 0.8, bl if i % 2 else M(K, "pod_hover"))
        elif name == "science":
            K.tube([(24, 42), (22, 26), (12, 8)], [3, 2.2, 1.2], fl)
            K.tube([(24, 42), (26, 26), (36, 8)], [3, 2.2, 1.2], fl)
            K.blob(12, 8, 3, 3, 2, bl)
            K.blob(36, 8, 3, 3, 2, bl)
        elif name == "comms":
            K.blob(14, 24, 7, 7, 5, M(K, "pod_hover"))
            for r in (13, 20):
                p = [(14 + math.cos(a) * r, 24 + math.sin(a) * r) for a in (-0.9, 0.0, 0.9)]
                K.tube(p, [1.6] * 3, bl)
        elif name == "repair":
            K.tube([(24, 44), (24, 30), (20, 20)], [4, 3, 2.4], fl)
            K.blob(20, 14, 8, 10, 6, M(K, "pod_hover"))
        elif name == "map":
            n = [(8, 10), (24, 18), (40, 8), (14, 36), (34, 38)]
            for a, b in ((0, 1), (1, 2), (1, 3), (1, 4)):
                K.tube([n[a], ((n[a][0] + n[b][0]) / 2 + 2, (n[a][1] + n[b][1]) / 2 - 2), n[b]], [1.6, 1.4, 1.6], fl)
            for x, y in n:
                K.blob(x, y, 3.2, 3.2, 2.4, bl)
        elif name == "settings":
            K.blob(c, c, 17, 15, 7, M(K, "pod_idle"), lumps=0.05)
            K.blob(c + 2, c + 1, 6, 6, 5, M(K, "blood"), z=5.0)
    return build


ICONS = ["helm", "fire", "shields", "tractor", "transport", "science", "comms", "repair", "map", "settings"]
PIECES = {
    "frame.panel":    ("nine", (200, 150), (40, 40, 40, 40), "xy", frame_panel),
    "frame.inset":    ("nine", (80, 60), (16, 16, 16, 16), "xy", frame_inset),
    "bar.track":      ("nine", (120, 16), (0, 14, 0, 14), "x", bar_track),
    "bar.fill":       ("nine", (80, 10), (0, 6, 0, 6), "x", bar_fill("bile")),
    "bar.fill.red":   ("nine", (80, 10), (0, 6, 0, 6), "x", bar_fill("blood")),
    "button.idle":    ("nine", (160, 52), (24, 30, 24, 46), "x", button("idle")),
    "button.hover":   ("nine", (160, 52), (24, 30, 24, 46), "x", button("hover")),
    "button.pressed": ("nine", (160, 52), (24, 30, 24, 46), "x", button("pressed")),
    "tab.idle":       ("nine", (130, 40), (14, 28, 14, 28), "x", tab(False)),
    "tab.active":     ("nine", (130, 40), (14, 28, 14, 28), "x", tab(True)),
    "strip.top":      ("nine", (512, 56), (8, 110, 8, 110), "x", strip(False)),
    "strip.bottom":   ("nine", (512, 56), (8, 110, 8, 110), "x", strip(True)),
    "divider":        ("nine", (128, 10), (0, 14, 0, 14), "x", divider),
    "nameplate":      ("hero", (460, 56), None, None, nameplate),
    "helm.pad":       ("hero", (300, 300), None, None, helm_pad),
    "ssd.bezel":      ("hero", (460, 300), None, None, ssd_bezel),
    "dial":           ("hero", (120, 120), None, None, dial),
    "lamp.alert.green": ("hero", (64, 64), None, None, lamp("bile")),
    "lamp.alert.yellow": ("hero", (64, 64), None, None, lamp("amber")),
    "lamp.alert.red": ("hero", (64, 64), None, None, lamp("blood")),
    "crest":          ("hero", (96, 96), None, None, crest),
}
PIECES.update({f"icon.{n}": ("hero", (48, 48), None, None, icon(n)) for n in ICONS})

# A dim warm key from the top left, a violet rim from the bottom right, a dark purple world: the light
# that matters comes from inside the hide.
LIGHTS = [("Key", (1.0, -1.0, -1.2), 2.6, (1.0, 0.9, 0.82), 4.0), ("Rim", (-1.0, 1.0, -0.7), 1.4, (0.75, 0.55, 1.0), 8.0)]


def render_setup(w_px, h_px, samples):
    ui_render.setup(w_px, h_px, samples, LIGHTS, world=(0.02, 0.012, 0.022), bounces=dict(max_bounces=6, transmission_bounces=2))


if __name__ == "__main__":
    ui_render.run("vhorr", PIECES, Organic, render_setup, samples=48)
