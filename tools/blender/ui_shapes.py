"""Fleet Command: the shared modelling and shading helpers for the console skins rendered in Blender.

Owns what the skin generators (tools/blender/build_ui_<skin>.py) build pieces from (CLAUDE.md section 6.1:
one implementation): NodeKit, a small builder for one Principled material's node tree (textures on object
coordinates, warped coordinates, maths, colour ramps, bump); and Shaper, the modelling context for one kit
piece, whose shapes are smooth tubes (curves with a round bevel and a radius per point), ellipsoids,
cylinders and filled curve slabs with rounded edges. Units: 1 px of the 1x slot is 1 mm; builders take
slot pixels with y running down. The render harness is tools/blender/ui_render.py.
"""

import math

import bmesh
import bpy
from mathutils import Matrix

import panel_kit

MM = 0.001
srgb = panel_kit.srgb


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


# ----------------------------------------------------------------------------- modelling

class Shaper:
    """One piece being modelled: w x h slot pixels (1 px = 1 mm). Shapes are smooth tubes (curves with a
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

    def tube(self, pts, radii, m, flat=0.7, z=None, cyclic=False, res=12, sharp=False):
        """A tube through pts (slot px) at heights z (mm), radius per point in px, flattened in z by flat (about its
        own heights, so z stays where it is asked to be). Smooth through
        its points, or with sharp=True a polyline keeping its corners (engraved lines, glyphs)."""
        cu = bpy.data.curves.new(f"t{self.n}", "CURVE")
        cu.dimensions = "3D"
        cu.bevel_depth = MM
        cu.bevel_resolution = 5
        cu.resolution_u = res
        cu.use_fill_caps = True
        zs = z or [0.0] * len(pts)
        if sharp:
            sp = cu.splines.new("POLY")
            sp.points.add(len(pts) - 1)
            for p, (x, y), r, zz in zip(sp.points, pts, radii, zs):
                p.co = (*self.P(x, y, zz / flat), 1.0)
                p.radius = r
        else:
            sp = cu.splines.new("BEZIER")
            sp.bezier_points.add(len(pts) - 1)
            for bp, (x, y), r, zz in zip(sp.bezier_points, pts, radii, zs):
                bp.co = self.P(x, y, zz / flat)
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

    def cyl(self, x, y, r, z0, z1, m, r1=None, seg=48, smooth=True):
        """A cylinder (or a cone with top radius r1) standing on the piece, radii and heights in px/mm."""
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r * MM, radius2=(r if r1 is None else r1) * MM, depth=(z1 - z0) * MM,
                              matrix=Matrix.Translation((0.0, 0.0, 0.0)))
        for f in bm.faces:
            f.smooth = smooth
        me = bpy.data.meshes.new(f"c{self.n}")
        bm.to_mesh(me)
        bm.free()
        ob = self._link(me, m)
        X, Y, _ = self.P(x, y)
        ob.location = (X, Y, (z0 + z1) / 2 * MM)
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


def arc(cx, cy, r, a0, a1, n=16):
    """Points along a circular arc (slot px; angles in radians, y down as in the image)."""
    return [(cx + math.cos(a0 + (a1 - a0) * i / n) * r, cy + math.sin(a0 + (a1 - a0) * i / n) * r) for i in range(n + 1)]


def taper(n, r0, r1):
    return [r0 + (r1 - r0) * i / (n - 1) for i in range(n)]
