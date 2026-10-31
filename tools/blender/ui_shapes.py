"""Fleet Command: the shared modelling and shading helpers for the console skins rendered in Blender.

Owns what the skin generators (tools/blender/build_ui_<skin>.py) build pieces from (CLAUDE.md section 6.1:
one implementation): NodeKit, a small builder for one Principled material's node tree (textures on object
coordinates, warped coordinates, maths, colour ramps, bump); Shaper, the modelling context for one kit
piece, whose shapes are smooth tubes (curves with a round bevel and a radius per point), ellipsoids,
cylinders and filled curve slabs with rounded edges; and Panel, a Shaper that builds the two-layer analog
panel of the Analog Panel Bible (docs/design/ui-bible.html, the blender-ui-panels skill): pillowed plates
with footprint-true rolled edges, formed-metal outlines, cutters baked and bevelled, pressed dimples, wells
with a backlit film, flat print, proud knobs, lamps under the lip, and light linking between the layers.
Units: 1 px of the 1x slot is 1 mm; builders take slot pixels with y running down; heights are mm. The
render harness is tools/blender/ui_render.py.
"""

import math
import os

import bmesh
import bpy
from mathutils import Matrix, Vector

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


# ----------------------------------------------------------------------------- outlines (pure geometry, slot px)

def _area(pts):
    return abs(sum(pts[i - 1][0] * p[1] - p[0] * pts[i - 1][1] for i, p in enumerate(pts))) / 2


def lip_runs(well, seg, faces=None, min_len=0.5):
    """Where a well's lamps go: walks the closed outline `well` (px, y down) and groups the edges `faces(nx, ny, length)`
    accepts (all of them when None) into runs about `seg` px long. Yields (x0, y0, x1, y1, nx, ny): a run's ends and the
    length-weighted inward normal of its edges; a run shorter than min_len is dropped. One implementation for every
    skin's lighting option (the bible's chapter 10): rim light takes every edge, bottom light the lower lips (ny < 0),
    side light the left lips (nx > 0). The inward side comes from the outline's winding, so any shape works."""
    n = len(well)
    area = sum(well[i - 1][0] * well[i][1] - well[i][0] * well[i - 1][1] for i in range(n)) / 2
    sgn = 1.0 if area > 0 else -1.0   # y down: a positive area runs clockwise on screen, so inward is to the right
    start, end, acc, nrm = None, None, 0.0, [0.0, 0.0]

    def flush():
        if start is not None and math.hypot(end[0] - start[0], end[1] - start[1]) > min_len:
            k = math.hypot(*nrm) or 1.0
            yield start[0], start[1], end[0], end[1], nrm[0] / k, nrm[1] / k

    for i in range(n):
        a, b = well[i], well[(i + 1) % n]
        tx, ty = b[0] - a[0], b[1] - a[1]
        ln = math.hypot(tx, ty)
        if ln == 0:
            continue
        nx, ny = -ty / ln * sgn, tx / ln * sgn
        if faces is None or faces(nx, ny, ln):
            if start is None:
                start, acc, nrm = a, 0.0, [0.0, 0.0]
            end, acc = b, acc + ln
            nrm = [nrm[0] + nx * ln, nrm[1] + ny * ln]
            if acc >= seg:
                yield from flush()
                start = None
        else:
            yield from flush()
            start = None
    yield from flush()


def offset_poly(pts, d):
    """Move a closed outline d px inward (negative: outward), each point along its mitred normal. Works on any
    smooth outline, which is what lets a plate's roll follow a formed shape."""
    def unit(x, y):
        n = math.hypot(x, y) or 1.0
        return x / n, y / n

    def go(sign):
        out = []
        for i in range(len(pts)):
            p0, p1, p2 = pts[i - 1], pts[i], pts[(i + 1) % len(pts)]
            n1 = unit(-(p1[1] - p0[1]), p1[0] - p0[0])
            n2 = unit(-(p2[1] - p1[1]), p2[0] - p1[0])
            m = unit(n1[0] + n2[0], n1[1] + n2[1])
            k = sign * d / max(0.35, m[0] * n1[0] + m[1] * n1[1])
            out.append((p1[0] + m[0] * k, p1[1] + m[1] * k))
        return out
    out = go(1.0)
    return out if (_area(out) < _area(pts)) == (d > 0) else go(-1.0)


def notched(x0, y0, x1, y1, r, cx, hw, fall, h):
    """A window outline whose bottom edge rises in a smooth notch: flat at h px above the bottom for hw px either side
    of cx, falling back to the edge over fall px with a concave foot and a rounded shoulder (a smoothstep), so the
    metal below flows up into it as one piece (the bible's N1 to N4)."""
    def corner(cx_, cy_, a0):
        return [(cx_ + r * math.cos(math.radians(a0 + 90 * t / 8)), cy_ + r * math.sin(math.radians(a0 + 90 * t / 8))) for t in range(9)]

    def lift(x):
        d = abs(x - cx)
        if d <= hw:
            return h
        if d >= hw + fall:
            return 0.0
        t = 1.0 - (d - hw) / fall
        return h * t * t * (3 - 2 * t)
    pts = corner(x0 + r, y0 + r, 180) + corner(x1 - r, y0 + r, 270) + corner(x1 - r, y1 - r, 0)
    x = x1 - r - 2.0
    while x > x0 + r + 1.0:
        pts.append((x, y1 - lift(x)))
        x -= 2.0
    return pts + corner(x0 + r, y1 - r, 90)


class RR(list):
    """A rounded rectangle's points that remember the rectangle, so a plate can offset it exactly."""


def rr(x0, y0, x1, y1, r):
    o = RR(rrect(x0, y0, x1, y1, r, seg=16))
    o.rect = (x0, y0, x1, y1, r)
    return o


# ----------------------------------------------------------------------------- materials shared by the panels

def metal(name, col, rough, knurl=False):
    """Polished metal (chrome, brass, gold); with knurl, fine diagonal ridges for grip."""
    k = NodeKit(name)
    k.set(Base_Color=col, Metallic=1.0, Roughness=rough)
    if knurl:
        w = k.tex("ShaderNodeTexWave", 900.0, **{"Distortion": 0.0})
        k.bump(w.outputs["Fac"], 0.5)
    return k.mat


def satin(name, col="#bdb8ae"):
    """Brushed satin plate metal: a fine wave grain under a light clear coat."""
    k = NodeKit(name)
    w = k.tex("ShaderNodeTexWave", 900.0, **{"Distortion": 1.5, "Detail": 2.0})
    k.set(Base_Color=col, Metallic=0.75, Roughness=0.22, Coat_Weight=0.3, Coat_Roughness=0.1)
    k.bump(w.outputs["Fac"], 0.04)
    return k.mat


def printed(name, col, rough=0.55):
    """Plain paint or enamel: engraving fill, a dark chassis, a pointer."""
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=rough)
    return k.mat


def film_mat(name, col="#f4d9a9", shadow="#85765e", es=0.85, glow=1.0, rim=True, ao_mm=7.0):
    """The backlit film in a well: its own even glow (es x glow), darkened by ambient occlusion within ao_mm where the
    plate's lip overhangs it, so it sits in a well with a thin contact line at the rim (the bible's F1, W5)."""
    k = NodeKit(name)
    if rim:
        ao = k.N.new("ShaderNodeAmbientOcclusion")
        ao.samples, ao.only_local = 16, False
        ao.inputs["Distance"].default_value = ao_mm * MM
        occ = k.math("POWER", ao.outputs["AO"], 2.2)
        k.set(Base_Color=k.ramp(occ, shadow, col), Roughness=0.6, Emission_Color=col, Emission_Strength=k.math("MULTIPLY", occ, es * glow))
    else:
        k.set(Base_Color=col, Roughness=0.6, Emission_Color=col, Emission_Strength=es * glow)
    return k.mat


def film_ink(name, col, es=0.9, glow=1.0):
    """Ink printed on a backlit film: its surface reflects 35% of its colour, so a lamp right above cannot wash it out,
    while its glow carries the full colour, so it reads as light through the film and never as black paint."""
    k = NodeKit(name)
    dim = "#%02x%02x%02x" % tuple(int(int(col[i:i + 2], 16) * 0.35) for i in (1, 3, 5))
    k.set(Base_Color=dim, Roughness=0.6, Emission_Color=col, Emission_Strength=es * glow)
    return k.mat


# ----------------------------------------------------------------------------- the two-layer analog panel

LIP = dict(length=10.0, depth=20.0, tilt=90.0, cone=180.0, spill=0.2, colour=(1.0, 0.88, 0.6))


class Panel(Shaper):
    """A Shaper that builds the bible's two-layer panel: pillowed plates over wells with a backlit film.

    pt is the plate's top face (mm), roll the radius every plate edge rolls over with, dz the film's surface (mm, below
    the plate), lamps the scale on every lamp (separate from the film's glow, so softening the lamps keeps the film's
    colour), lip the lamp settings (LIP above). mats maps roles to materials: plate, film, ink, groove, knob, pointer,
    bezel. Every method takes slot px with y down and heights in mm."""

    def __init__(self, name, w, h, hero=True, pt=8.0, roll=3.8, dz=-12.0, lamps=0.25, lip=None, mats=None):
        super().__init__(name, w, h, hero)
        self.pt, self.roll, self.dz, self.lamps = pt, roll, dz, lamps
        self.lip = dict(LIP, **(lip or {}))
        self.m = dict(mats or {})
        self.fz = 0.05  # flat print sits this far (mm) above its film

    # --- primitives and cutters
    def mesh(self, bm, mat, name="hs"):
        """A bmesh as an object in this piece: steep faces smooth, flat faces flat."""
        bm.normal_update()
        for f in bm.faces:
            f.smooth = abs(f.normal.z) < 0.7
        me = bpy.data.meshes.new(name)
        bm.to_mesh(me)
        bm.free()
        if mat is not None:
            me.materials.append(mat)
        ob = bpy.data.objects.new(name, me)
        self.coll.objects.link(ob)
        return ob

    def cyl_bm(self, bm, x, y, r, z0, z1, seg=72):
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg, radius1=r * MM, radius2=r * MM,
                              depth=(z1 - z0) * MM, matrix=Matrix.Translation(self.P(x, y, (z0 + z1) / 2)))
        return bm

    def box_bm(self, bm, cx, cy, w, h, z0, z1, rot=0.0):
        m = Matrix.Translation(self.P(cx, cy, (z0 + z1) / 2)) @ Matrix.Rotation(-rot, 4, "Z") @ Matrix.Diagonal((w * MM, h * MM, (z1 - z0) * MM, 1.0))
        bmesh.ops.create_cube(bm, size=1.0, matrix=m)
        return bm

    def cut(self, ob, bm):
        """Carve ob with the solid in bm through an Exact boolean; the cutter is hidden from the render."""
        c = self.mesh(bm, None, "cutter")
        c.hide_render, c.display_type = True, "WIRE"
        m = ob.modifiers.new("cut", "BOOLEAN")
        m.operation, m.solver, m.object = "DIFFERENCE", "EXACT", c
        m.use_hole_tolerant = True

    @staticmethod
    def bevel(ob, w, seg=3, angle=30):
        m = ob.modifiers.new("bevel", "BEVEL")
        m.width, m.segments, m.limit_method, m.angle_limit, m.use_clamp_overlap = w * MM, seg, "ANGLE", math.radians(angle), True

    # --- the metal layer
    def plate(self, outlines, z0=0.0, z1=None, m=None, edge=None):
        """A pillowed plate: outlines[0] the outside, the rest openings. Every edge rolls over in a full round of radius
        edge mm and the plate keeps exactly the footprint drawn (a curve bevel grows a shape by its radius, so the
        outside is drawn that much smaller and the openings that much larger first). Returned as a welded mesh so
        cutters can carve it (the bible's P1, P2)."""
        z1 = self.pt if z1 is None else z1
        edge = self.roll if edge is None else edge
        offset = []
        for i, o in enumerate(outlines):
            d = edge if i == 0 else -edge
            if hasattr(o, "rect"):
                x0, y0, x1, y1, r = o.rect
                offset.append(rrect(x0 + d, y0 + d, x1 - d, y1 - d, max(0.6, r - d), seg=16))
            else:
                offset.append(offset_poly(o, d))
        cu = self.slab(offset, min(z0, z1 - 2 * edge - 0.4), z1, m or self.m["plate"], bevel=edge)
        dg = bpy.context.evaluated_depsgraph_get()
        me = bpy.data.meshes.new_from_object(cu.evaluated_get(dg))
        bm = bmesh.new()
        bm.from_mesh(me)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.00002)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        for f in bm.faces:  # flat faces shade flat, so the fill's long triangles do not pick up the bevel's normals
            f.smooth = abs(f.normal.z) < 0.999
        bm.to_mesh(me)
        bm.free()
        ob = bpy.data.objects.new("plate", me)
        ob.location = cu.location
        self.coll.objects.link(ob)
        bpy.data.objects.remove(cu)
        return ob

    def finish(self, ob, w, seg=6, angle=20):
        """Bake the cutters into a plate, dissolve the flat areas they split back into clean faces (no stray sliver for
        the light to catch), then bevel the edges the cuts made."""
        dg = bpy.context.evaluated_depsgraph_get()
        me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
        bm = bmesh.new()
        bm.from_mesh(me)
        bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(0.5), verts=bm.verts, edges=bm.edges)
        for f in bm.faces:
            f.smooth = abs(f.normal.z) < 0.999
        bm.to_mesh(me)
        bm.free()
        ob.modifiers.clear()
        ob.data = me
        self.bevel(ob, w, seg, angle)

    def dimple(self, ob, x, y, rx, ry, top, depth, rz=7.0):
        """Press a soft dimple into a plate: an ellipsoid cutter sunk depth mm below its top face (then finish)."""
        bm = bmesh.new()
        m = Matrix.Translation(self.P(x, y, top + rz - depth)) @ Matrix.Diagonal((rx * MM, ry * MM, rz * MM, 1.0))
        bmesh.ops.create_uvsphere(bm, u_segments=72, v_segments=36, radius=1.0, matrix=m)
        self.cut(ob, bm)

    def bezel(self, win, grow=1.5, m=None, wall=True):
        """A thin bezel round a window, following its outline just under the plate's roll: it starts inside the metal
        wall and shows a fine line in the opening. With wall, the well's dark wall runs from the plate down past the
        film just outside the opening, which is what the film's rim shadow sees (never inside it: it would block the
        lamps)."""
        self.slab([offset_poly(win, -0.8), offset_poly(win, grow)], self.pt - self.roll - 1.8, self.pt - self.roll + 0.2,
                  m or self.m["bezel"], bevel=0.6)
        if wall:
            self.slab([offset_poly(win, -4.0), offset_poly(win, -0.3)], self.dz - 1.0, 0.5, self.m["groove"], bevel=0.2)

    def knob(self, x, y, r, z=None, pointer=None, collar=True, m=None):
        """A polished knob that stands proud: a near-full sphere on a dark collar, a pointer line on its crown when
        pointer gives an angle (degrees, 0 = right, -90 = up). Its footprint is r x 1.14 (the fit check's K rule)."""
        z = self.pt if z is None else z
        if collar:
            self.cyl(x, y, r * 1.14, z - 0.6, z + 0.7, self.m["groove"], seg=48)
        cz, rz = z + r * 0.3, r * 0.85
        self.blob(x, y, r, r, rz, m or self.m["knob"], z=cz, seg=64)
        if pointer is not None:
            a = math.radians(pointer)
            pts, zs = [], []
            for f in (0.2, 0.68):
                pts.append((x + math.cos(a) * r * f, y + math.sin(a) * r * f))
                zs.append(cz + rz * math.sqrt(max(0.0, 1 - f * f)) + 0.15)
            self.tube(pts, [0.9, 0.9], self.m["pointer"], flat=0.6, z=zs, sharp=True)

    # --- the display layer
    def film(self, win, grow=8, holes=(), m=None, z=None, inside=None, shape=None):
        """The backlit film behind one window only, so the gaps between plates stay dark; holes open it for sub-displays.

        Rule B (owner, 2026-10-31: "Some times the UI layer/plane can stick outside its bounds. It must measured
        properly to fit inside the parent panel"): a layer behind a plate never reaches past the plate's footprint. Give
        shape, an outline already measured to fit (tools/mockups/formed_metal.py behind_layer), or inside, the plate's
        rounded rectangle (x0, y0, x1, y1, r): the window's bounds grown by grow px are then clamped 4 px inside it,
        their corners rounded at least as much as the plate's less the inset."""
        z = self.dz if z is None else z
        if shape is None:
            x0, y0 = min(p[0] for p in win) - grow, min(p[1] for p in win) - grow
            x1, y1 = max(p[0] for p in win) + grow, max(p[1] for p in win) + grow
            r = 8.0
            if inside is not None:
                px0, py0, px1, py1, pr = inside
                x0, y0, x1, y1 = max(x0, px0 + 4), max(y0, py0 + 4), min(x1, px1 - 4), min(y1, py1 - 4)
                r = max(r, pr - 4)
            shape = rrect(x0, y0, x1, y1, min(r, (x1 - x0) / 2, (y1 - y0) / 2), seg=16)
        return self.slab([shape] + list(holes), z - 2.0, z, m or self.m["film"], bevel=0.4)

    def flat(self, outlines, m, z=None):
        """A flat printed shape on the film: closed outlines filled with no depth and no bevel, casting no shadow."""
        z = self.dz + self.fz if z is None else z
        ob = self.slab(outlines, z, z, m, bevel=0.0)
        ob.visible_shadow = False
        return ob

    def stroke(self, pts, w, m, z=None):
        """A flat printed line on the film: a ribbon w px wide along the points, with no depth and no shadow."""
        z = (self.dz if z is None else z) + self.fz
        bm = bmesh.new()
        left, right = [], []
        for i, p in enumerate(pts):
            a, b = pts[max(0, i - 1)], pts[min(len(pts) - 1, i + 1)]
            tx, ty = b[0] - a[0], b[1] - a[1]
            n = math.hypot(tx, ty) or 1.0
            nx, ny = -ty / n * w / 2, tx / n * w / 2
            left.append(bm.verts.new(self.P(p[0] + nx, p[1] + ny, z)))
            right.append(bm.verts.new(self.P(p[0] - nx, p[1] - ny, z)))
        for i in range(len(pts) - 1):
            bm.faces.new((left[i], left[i + 1], right[i + 1], right[i]))
        ob = self.mesh(bm, m, "stroke")
        ob.visible_shadow = False
        return ob

    def label(self, text, x, y, size, m, z, font=None, align="CENTER", track=0.0):
        """Text at (x, y) px, size px tall: engraved on the metal (z above 0, a 0.06 mm relief), or printed flat on the
        film (z below 0). font is a path, or a file name in tools/fonts (default DejaVu Sans Bold); fonts whose glyphs
        overlap their own outlines fill with holes in Blender, so print with clean ones."""
        font = font or panel_kit.FONT_SANS
        if not os.path.isabs(font):
            font = os.path.join(os.path.dirname(panel_kit.FONT_SANS), font)
        cu = bpy.data.curves.new("txt", "FONT")
        cu.body, cu.font, cu.size = text, bpy.data.fonts.load(font, check_existing=True), size * MM
        cu.align_x, cu.align_y, cu.space_character, cu.extrude = align, "CENTER", 1.0 + track, (0.0 if z < 0 else 0.06 * MM)
        cu.materials.append(m)
        ob = bpy.data.objects.new("txt", cu)
        ob.location = self.P(x, y, z)
        self.coll.objects.link(ob)
        return ob

    # --- lamps (named lip*, so the layers can tell them from the room lights)
    def lip_light(self, x, y, power, depth=None, colour=None):
        """A short line of light tucked in the well under a window's top lip at (x, y) px, lip['tilt'] degrees down the
        screen from the film's normal, spreading over lip['cone'] degrees from a lip['length'] x lip['depth'] mm source,
        so its light spills down the film in a wide soft fan (the bible's L2; the owner's "10 mm 180 degrees")."""
        L = self.lip
        ld = bpy.data.lights.new("lip", "AREA")
        ld.shape, ld.size, ld.size_y = "RECTANGLE", L["length"] * MM, L["depth"] * MM
        ld.energy, ld.color, ld.spread = power * L["spill"] * self.lamps, colour or L["colour"], math.radians(L["cone"])
        ob = bpy.data.objects.new("lip", ld)
        ob.location = ((x - self.w / 2) * MM, (self.h / 2 - y - 1.0) * MM, (self.dz + 8.0 if depth is None else depth) * MM)
        ob.rotation_mode = "QUATERNION"
        t = math.radians(L["tilt"])
        ob.rotation_quaternion = Vector((0.0, -math.sin(t), -math.cos(t))).to_track_quat("-Z", "Y")
        ob.visible_camera = False
        bpy.context.scene.collection.objects.link(ob)
        return ob

    def edge_lamp(self, x0, y0, x1, y1, nx, ny, watts_per_mm, spread=120.0, tilt=90.0, depth=None, colour=None):
        """A strip lamp along the edge (x0, y0)-(x1, y1) px under the lip, aimed inward along the film (nx, ny is the
        inward normal on screen, y down). Its power is per mm of edge, so a longer edge stays as bright. One makes a
        strip, four make all sides, four close to the film with a narrow spread make rim light (the bible's lighting
        options)."""
        ln = math.hypot(x1 - x0, y1 - y0)
        ld = bpy.data.lights.new("lip", "AREA")
        ld.shape, ld.size, ld.size_y = "RECTANGLE", ln * MM, 6.0 * MM
        ld.energy, ld.color, ld.spread = watts_per_mm * ln * self.lamps, colour or self.lip["colour"], math.radians(spread)
        ob = bpy.data.objects.new("lip", ld)
        cx, cy = (x0 + x1) / 2 + nx * 1.0, (y0 + y1) / 2 + ny * 1.0
        ob.location = ((cx - self.w / 2) * MM, (self.h / 2 - cy) * MM, (self.dz + 6.0 if depth is None else depth) * MM)
        t = math.radians(tilt)
        d = Vector((nx * math.sin(t), -ny * math.sin(t), -math.cos(t))).normalized()   # light direction, world
        ex = Vector(((x1 - x0) / ln, -(y1 - y0) / ln, 0.0))                             # along the edge, world
        z = -d
        xx = (ex - z * ex.dot(z)).normalized()
        ob.matrix_world = Matrix.Translation(ob.location) @ Matrix((xx, z.cross(xx), z)).transposed().to_4x4()
        ob.visible_camera = False
        bpy.context.scene.collection.objects.link(ob)
        return ob


def link_layers(display, emitters, main=(0.8, 60.0), sun_dir=(0.3, -1.0, -1.6), softbox=1.2, ceiling=1.6):
    """Split the scene into the bible's two layers and light each on its own (Blender 4.x light linking).

    display: material names on the display layer (film, its print, streaks); everything else with a material is the
    metal layer. Print on the film (every display material but the first) casts no shadow. Adds a softbox and a large
    ceiling light that only the metal sees (polished parts need something bright to reflect), and the main light: a
    sun from the room light's direction (sun_dir, upper left), strength main[0] and angular size main[1] degrees, that
    the display sees too, so the lips cast a soft top and left shadow into the wells. Lamps named lip* reach only the
    display; emitters (material names) light only the display. Returns (metal_layer, display_layer)."""
    import ui_render
    scene = bpy.context.scene
    metal_layer = bpy.data.collections.new("metal_layer")
    display_layer = bpy.data.collections.new("display_layer")
    for c in (metal_layer, display_layer):
        scene.collection.children.link(c)
    for ob in list(scene.objects):
        if ob.type in ("MESH", "CURVE", "FONT") and ob.active_material:
            name = ob.active_material.name
            (display_layer if name in display else metal_layer).objects.link(ob)
            if name in display and name != display[0]:
                ob.visible_shadow = False
    sb = bpy.data.lights.new("Softbox", "AREA")
    sb.shape, sb.size, sb.energy, sb.color = "DISK", 0.6, softbox, (1.0, 0.94, 0.85)
    sbo = bpy.data.objects.new("Softbox", sb)
    sbo.location, sbo.rotation_euler = (-0.25, 0.35, 0.6), (math.radians(-35), math.radians(-20), 0.0)
    scene.collection.objects.link(sbo)
    ui_render.sun("Main", sun_dir, main[0], (1.0, 0.97, 0.92), main[1])
    cl = bpy.data.lights.new("Ceiling", "AREA")
    cl.shape, cl.size, cl.energy, cl.color = "SQUARE", 1.2, ceiling, (1.0, 0.97, 0.92)
    clo = bpy.data.objects.new("Ceiling", cl)
    clo.location = (0.0, 0.1, 0.7)
    scene.collection.objects.link(clo)
    for ob in scene.objects:
        if ob.type == "LIGHT":
            ob.light_linking.receiver_collection = display_layer if ob.name.startswith(("lip", "Main")) else metal_layer
        elif ob.active_material and ob.active_material.name in emitters:
            ob.light_linking.receiver_collection = display_layer
    return metal_layer, display_layer
