"""Fleet Command: the Kethran Hegemony console skin, modelled and rendered in Blender.

Owns every piece of the Kethran skin of the shared console UI kit (CLAUDE.md section 7: one kit, a
skin per faction; owner, 2026-10-30: "build the other design language using 3d models in blender like
starcraft"). Each piece is MODELLED as hard-surface geometry (gunmetal plates in layered steps, heavy
45 degree chamfers, hex bolts, conduits, recessed glass, trapezoid wedge keycaps with backlit lenses),
lit by a harsh key light from the top left and a cool rim from the bottom right, and rendered face-on
with an orthographic camera in Cycles on the CPU into a transparent PNG at twice its slot size. It
lives in tools/blender because it is a Blender generator, beside the panel kit it builds on.

    /opt/blender/blender --background --factory-startup --python tools/blender/build_ui_kethran.py -- \
        --out assets/ui/kethran [--only button.idle,crest] [--samples 64]

Writes assets/ui/kethran/<piece>.png (RGBA, 2x) and manifest.json:
    {"skin": "kethran", "scale": 2, "pieces": {"<name>": {"kind": "nine"|"hero", "size": [w, h],
     "margins": [t, r, b, l], "stretch": "x"|"xy", "file": "<name>.png"}}}
Sizes and margins are in 1x pixels at the 1920 x 1080 reference. "stretch" says which axes a 9-slice
piece is drawn to stretch along: "x" pieces (buttons, bars, tabs, strips, the divider) keep their
height, because a trapezoid keycap's slanted sides only stay straight when the piece widens.

Modelling units: 1 px of the 1x slot is 1 mm in Blender, so a 160 px frame is a 0.16 m object and a
6 mm keycap travel is 6 px of relief. Coordinates in the builders below are in those pixels with the
origin at the piece's top left and y running down (as the image does); Piece.P turns them into
Blender's metres with y up.

9-slice rule: everything that is not a plain, constant cross-section lies inside the margins (corners
for bolts and brackets, end caps for the strips), the edges between corners keep one profile along
their length, the centre is a plain field (dark glass, a socket floor, a lens run), and no procedural
noise is used on a 9-slice piece, so a stretched edge does not smear. Hero pieces carry noise and
detail anywhere.

Deterministic: no randomness but fixed seeds, Cycles seed 0, fixed samples, OpenImageDenoise.
Kethran design language: openspec/changes/console-skins/design.md. Our own design throughout.
"""

import argparse
import json
import math
import os
import struct
import sys
import time

import bmesh
import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import panel_kit  # noqa: E402  (the Star Crew / Crab Breeze panel kit: Panel primitives, srgb, camera)

ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
MM = 0.001          # one 1x slot pixel, in metres
SCALE = 2           # pixels rendered per 1x slot pixel

# ----------------------------------------------------------------------------- palette and materials
# Iron, fire, blood, rank. Hex colours are sRGB; panel_kit.srgb makes them linear.
PAL = dict(
    gunmetal="#2b2f33", iron="#4a4f55", steel="#8a9096", oxide="#9e2a1e", signal="#d9483b",
    amber="#ffb547", bone="#d8cfbf", glass="#0b0d0f",
)

# c colour, r roughness, m metalness, wear (chamfers worn to bare steel), grime (crevice darkening),
# emit / es (emission colour and strength), glow (emission brighter along the piece's centre line),
# stripes (red and black hazard bands), coat (a glossy clear layer: glass, lenses, enamel)
ROLES = {
    "gunmetal":  dict(c=PAL["gunmetal"], r=0.46, m=0.3, wear=0.6, grime=0.9),
    "gunmetal2": dict(c="#24282c", r=0.5, m=0.3, wear=0.45, grime=0.9),
    "iron":      dict(c=PAL["iron"], r=0.42, m=0.45, wear=0.75, grime=0.8),
    "steel":     dict(c=PAL["steel"], r=0.34, m=0.8, wear=0.0, grime=0.5),
    "oxide":     dict(c=PAL["oxide"], r=0.62, m=0.0, wear=0.9, grime=0.8),
    "signal":    dict(c=PAL["signal"], r=0.5, m=0.0, wear=0.5, grime=0.6),
    "bone":      dict(c=PAL["bone"], r=0.6, m=0.0, wear=0.0, grime=0.4),
    "socket":    dict(c="#0d0f11", r=0.85, m=0.0, grime=0.5),
    "glass":     dict(c=PAL["glass"], r=0.07, m=0.0, coat=True),
    "hazard":    dict(c=PAL["signal"], r=0.55, m=0.0, wear=0.6, grime=0.7, stripes=True),
    "enamel":    dict(c="#b3241a", r=0.18, m=0.0, coat=True, grime=0.4),
    "lens_idle": dict(c="#3a2208", r=0.12, m=0.0, emit=PAL["amber"], es=0.32, coat=True),
    "lens_hover": dict(c="#5a3a08", r=0.12, m=0.0, emit=PAL["amber"], es=1.35, coat=True),
    "lens_hot":  dict(c="#5a1a08", r=0.12, m=0.0, emit="#ff6a2c", es=2.4, coat=True),
    "lens_off":  dict(c="#1c1408", r=0.12, m=0.0, coat=True),
    "fill_amber": dict(c="#5a3a08", r=0.15, m=0.0, emit=PAL["amber"], es=1.0, glow=True, coat=True),
    "fill_red":  dict(c="#4a0e0a", r=0.15, m=0.0, emit=PAL["signal"], es=1.1, glow=True, coat=True),
    "lamp_green": dict(c="#0c3a1a", r=0.1, m=0.0, emit="#4dff86", es=1.3, glow=True, coat=True),
    "lamp_yellow": dict(c="#4a3a08", r=0.1, m=0.0, emit="#ffd23c", es=1.3, glow=True, coat=True),
    "lamp_red":  dict(c="#4a0a08", r=0.1, m=0.0, emit="#ff3a28", es=1.5, glow=True, coat=True),
}


def make_material(name, spec, noise):
    """One role as a lit Cycles material for the final image (not a bake): Principled BSDF with true
    metalness, chamfer wear read from the geometric normal (a 45 degree chamfer is mixed toward worn
    steel, a flat face is not, so the effect is constant along a stretched edge), crevice grime from an
    AO node, and with noise=True a faint low-frequency mottle (hero pieces only)."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    N, L = nt.nodes, nt.links
    out = N.new("ShaderNodeOutputMaterial")
    bsdf = N.new("ShaderNodeBsdfPrincipled")
    L.new(bsdf.outputs[0], out.inputs[0])

    def op(kind, a, b=None):
        m = N.new("ShaderNodeMath")
        m.operation = kind
        for k, v in enumerate((a, b)):
            if v is None:
                continue
            if isinstance(v, (int, float)):
                m.inputs[k].default_value = v
            else:
                L.new(v, m.inputs[k])
        return m.outputs[0]

    def mix(fac, a, b):
        m = N.new("ShaderNodeMix")
        m.data_type = "RGBA"
        if isinstance(fac, (int, float)):
            m.inputs["Factor"].default_value = fac
        else:
            L.new(fac, m.inputs["Factor"])
        for sock, v in ((6, a), (7, b)):
            if isinstance(v, str):
                m.inputs[sock].default_value = list(panel_kit.srgb(v)) + [1.0]
            else:
                L.new(v, m.inputs[sock])
        return m.outputs[2]

    tc = N.new("ShaderNodeTexCoord")
    sep = N.new("ShaderNodeSeparateXYZ")
    L.new(tc.outputs["Object"], sep.inputs[0])
    colour = mix(0.0, spec["c"], spec["c"])
    if spec.get("stripes"):
        # 45 degree red and black bands, 7 mm pitch (14 px at 2x): our hazard marking, not yellow
        band = op("GREATER_THAN", op("FRACT", op("MULTIPLY", op("ADD", sep.outputs[0], sep.outputs[1]), 1.0 / 0.007)), 0.5)
        colour = mix(band, colour, "#111214")
    if noise:
        nz = N.new("ShaderNodeTexNoise")
        nz.inputs["Scale"].default_value = 28.0
        nz.inputs["Detail"].default_value = 3.0
        L.new(tc.outputs["Object"], nz.inputs["Vector"])
        colour = mix(op("MULTIPLY", op("MAXIMUM", op("SUBTRACT", nz.outputs["Fac"], 0.5), 0.0), 0.22), colour, "#1c1f22")
    bev = N.new("ShaderNodeBevel")
    bev.samples = 6
    bev.inputs["Radius"].default_value = 0.0004
    L.new(bev.outputs[0], bsdf.inputs["Normal"])
    if spec.get("wear"):
        geo = N.new("ShaderNodeNewGeometry")
        gz = N.new("ShaderNodeSeparateXYZ")
        L.new(geo.outputs["Normal"], gz.inputs[0])
        e = N.new("ShaderNodeMapRange")
        e.inputs["From Min"].default_value = 0.97
        e.inputs["From Max"].default_value = 0.8
        e.inputs["To Max"].default_value = spec["wear"]
        L.new(gz.outputs[2], e.inputs["Value"])
        w = e.outputs[0]
        if noise:
            n2 = N.new("ShaderNodeTexNoise")
            n2.inputs["Scale"].default_value = 160.0
            n2.inputs["Detail"].default_value = 4.0
            L.new(tc.outputs["Object"], n2.inputs["Vector"])
            br = N.new("ShaderNodeMapRange")
            br.inputs["From Min"].default_value = 0.35
            br.inputs["From Max"].default_value = 0.6
            br.inputs["To Min"].default_value = 0.45
            L.new(n2.outputs["Fac"], br.inputs["Value"])
            w = op("MULTIPLY", w, br.outputs[0])
        colour = mix(w, colour, PAL["steel"])
    if spec.get("grime"):
        ao = N.new("ShaderNodeAmbientOcclusion")
        ao.samples = 12
        ao.inputs["Distance"].default_value = 0.006
        g = N.new("ShaderNodeMapRange")
        g.inputs["From Min"].default_value = 1.0
        g.inputs["From Max"].default_value = 0.45
        g.inputs["To Max"].default_value = 0.85 * spec["grime"]
        L.new(ao.outputs["AO"], g.inputs["Value"])
        colour = mix(g.outputs[0], colour, "#08090a")
    L.new(colour, bsdf.inputs["Base Color"])
    bsdf.inputs["Metallic"].default_value = spec["m"]
    bsdf.inputs["Roughness"].default_value = spec["r"]
    if spec.get("coat"):
        bsdf.inputs["Coat Weight"].default_value = 1.0
        bsdf.inputs["Coat Roughness"].default_value = 0.05
    if spec.get("emit"):
        bsdf.inputs["Emission Color"].default_value = list(panel_kit.srgb(spec["emit"])) + [1.0]
        s = spec.get("es", 1.0)
        if spec.get("glow"):
            # brighter along the piece's centre line in y (a lit tube under glass); constant along x
            # so a stretched bar stays one bar. Object space: the piece is centred on the origin.
            half = max(1e-6, spec.get("_half", 0.005))
            f = op("SUBTRACT", 1.0, op("MINIMUM", op("DIVIDE", op("ABSOLUTE", sep.outputs[1]), half), 1.0))
            L.new(op("MULTIPLY", op("ADD", 0.45, op("MULTIPLY", f, 0.9)), s), bsdf.inputs["Emission Strength"])
        else:
            bsdf.inputs["Emission Strength"].default_value = s
    return mat


# ----------------------------------------------------------------------------- geometry helpers

def rect(x0, y0, x1, y1):
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def trap(tx0, tx1, ty, bx0, bx1, by):
    """A trapezoid: top edge tx0..tx1 at ty, bottom edge bx0..bx1 at by (y down)."""
    return [(tx0, ty), (tx1, ty), (bx1, by), (bx0, by)]


def chamfer(pts, c):
    """Cut every corner of a polygon back by c px along both edges (c a number or a per-corner
    list; 0 leaves that corner sharp). Kethran corners are cut, never rounded."""
    cs = c if isinstance(c, (list, tuple)) else [c] * len(pts)
    out = []
    n = len(pts)
    for i, v in enumerate(pts):
        if cs[i] <= 0:
            out.append(v)
            continue
        for a in (pts[i - 1], pts[(i + 1) % n]):
            dx, dy = a[0] - v[0], a[1] - v[1]
            d = math.hypot(dx, dy)
            k = min(cs[i], d * 0.49) / d
            out.append((v[0] + dx * k, v[1] + dy * k))
    return out


def mirror(pts, w=None, h=None):
    """Mirror a polygon across x = w/2 (w given) and/or y = h/2 (h given), keeping its winding."""
    out = [((w - x) if w is not None else x, (h - y) if h is not None else y) for x, y in pts]
    if (w is None) != (h is None):
        out.reverse()
    return out


class Piece:
    """One kit piece being modelled: w x h slot pixels (1 px = 1 mm), its objects in one collection.
    Builds on panel_kit.Panel for the round and faceted primitives (cylinders for bolts and lenses)."""

    def __init__(self, name, w, h, hero):
        self.name, self.w, self.h = name, w, h
        self.mats = {k: make_material(k, dict(v, _half=h * MM * 0.5), noise=hero) for k, v in ROLES.items()}
        self.kit = panel_kit.Panel(name, w * MM, h * MM, self.mats)
        self.coll = self.kit.coll
        self.n = 0

    def P(self, x, y):
        return ((x - self.w / 2) * MM, (self.h / 2 - y) * MM)

    def _mesh(self, pts, z0, z1):
        bm = bmesh.new()
        q = [self.P(x, y) for x, y in pts]
        lo = [bm.verts.new((p[0], p[1], z0 * MM)) for p in q]
        hi = [bm.verts.new((p[0], p[1], z1 * MM)) for p in q]
        bm.faces.new(list(reversed(lo)))
        bm.faces.new(hi)
        for i in range(len(q)):
            j = (i + 1) % len(q)
            bm.faces.new((lo[i], lo[j], hi[j], hi[i]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        self.n += 1
        me = bpy.data.meshes.new(f"{self.name}_{self.n}")
        bm.to_mesh(me)
        bm.free()
        ob = bpy.data.objects.new(me.name, me)
        self.coll.objects.link(ob)
        return ob

    def prism(self, pts, z0, z1, role, bevel=0.0, holes=(), cuts=()):
        """A flat profile (slot px, y down) extruded from z0 to z1 mm, with holes cut through it and
        cuts (pts, z0, z1) sunk into it by Exact booleans, then chamfered by bevel mm on every edge
        sharper than 30 degrees (one segment: a flat 45 degree chamfer that catches the light)."""
        ob = self._mesh(pts, z0, z1)
        ob.data.materials.append(self.mats[role])
        for c in [(h, z0 - 1.0, z1 + 1.0) for h in holes] + list(cuts):
            cutter = self._mesh(c[0], c[1], c[2])
            cutter.hide_render = True
            cutter.display_type = "WIRE"
            m = ob.modifiers.new("cut", "BOOLEAN")
            m.operation = "DIFFERENCE"
            m.solver = "EXACT"
            m.object = cutter
        if bevel:
            m = ob.modifiers.new("chamfer", "BEVEL")
            m.width = bevel * MM
            m.segments = 1
            m.limit_method = "ANGLE"
            m.angle_limit = math.radians(30)
            m.use_clamp_overlap = True
        return ob

    def bolt(self, x, y, z, r=3.0):
        """A hex-head bolt on a washer, with a hex socket: chamfered, never round."""
        X, Y = self.P(x, y)
        self.kit.cyl(X, Y, r * 1.3 * MM, (z - 0.5) * MM, (z + 0.7) * MM, "gunmetal2", seg=8)
        self.kit.cyl(X, Y, r * MM, (z + 0.4) * MM, (z + 2.6) * MM, "steel", seg=6, r1=r * 0.74 * MM)
        self.kit.cyl(X, Y, r * 0.34 * MM, (z + 2.2) * MM, (z + 2.75) * MM, "socket", seg=6)

    def pipe(self, x0, x1, y, r, z, role="iron", seg=8):
        """A straight conduit along x from x0 to x1 px at row y, its axis at height z mm."""
        X0, Y = self.P(x0, y)
        X1, _ = self.P(x1, y)
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r * MM, radius2=r * MM, depth=X1 - X0,
                              matrix=Matrix.Translation(((X0 + X1) / 2, Y, z * MM)) @ Matrix.Rotation(math.pi / 2, 4, "Y")
                              @ Matrix.Rotation(math.pi / seg, 4, "Z"))
        self.n += 1
        me = bpy.data.meshes.new(f"{self.name}_p{self.n}")
        bm.to_mesh(me)
        bm.free()
        me.materials.append(self.mats[role])
        ob = bpy.data.objects.new(me.name, me)
        self.coll.objects.link(ob)
        return ob

    def cone(self, x, y, r0, r1, z0, z1, role, seg=8):
        X, Y = self.P(x, y)
        return self.kit.cyl(X, Y, r0 * MM, z0 * MM, z1 * MM, role, seg=seg, r1=r1 * MM)


# ----------------------------------------------------------------------------- shared assemblies

def keycap(K, outer, key, z_top, lens_role, key_role="iron", lens_inset=8.0,
           lens_y=None, bezel_role="gunmetal"):
    """The Kethran wedge key: a trapezoid socket bezel round a recessed socket, and a raised
    trapezoid keycap (wider at the top) whose face carries a backlit glass lens strip sunk into a
    chamfered channel. outer and key are trapezoids (tx0, tx1, ty, bx0, bx1, by); z_top is the cap's
    face height in mm (9 at rest, 3 pressed: 6 mm of travel below the bezel's 6 mm face)."""
    if outer and bezel_role:
        tx0, tx1, ty, bx0, bx1, by = outer
        sock = (tx0 + 5.5, tx1 - 5.5, ty + 4.5, bx0 + 4.5, bx1 - 4.5, by - 4.5)
        K.prism(chamfer(trap(*outer), 4), 0, 6, bezel_role, bevel=1.8, holes=[chamfer(trap(*sock), 3)])
        # the socket floor: the bezel's own trapezoid pulled in, so nothing shows past the bezel
        K.prism(chamfer(trap(tx0 + 3, tx1 - 3, ty + 2.5, bx0 + 2.5, bx1 - 2.5, by - 2.5), 3), -9, -6, "socket")
    kx0, kx1, kty, kbx0, kbx1, kby = key
    ly0, ly1 = lens_y or (kty + (kby - kty) * 0.4, kty + (kby - kty) * 0.62)

    def side(y, left):
        t = (y - kty) / (kby - kty)
        return (kx0 + (kbx0 - kx0) * t) if left else (kx1 + (kbx1 - kx1) * t)

    def strip(y0, y1, inset):
        return [(side(y0, True) + inset, y0), (side(y0, False) - inset, y0),
                (side(y1, False) - inset, y1), (side(y1, True) + inset, y1)]

    chan = chamfer(strip(ly0 - 1.5, ly1 + 1.5, lens_inset - 1.5), 1.5)
    K.prism(chamfer(trap(*key), 3), -7.5, z_top, key_role, bevel=2.5, cuts=[(chan, z_top - 1.8, z_top + 1.0)])
    K.prism(chamfer(strip(ly0, ly1, lens_inset), 1.0), z_top - 3.5, z_top - 0.7, lens_role, bevel=0.6)
    # the brow: a second, raised plate along the cap's top edge, so the key's weight sits up front
    K.prism(chamfer(strip(kty + 1.5, kty + (ly0 - kty) * 0.62, 3.0), 1.5), z_top - 1.0, z_top + 1.8, key_role, bevel=1.2)


def corner_brackets(K, w, h, roles, z0=8.0, z1=13.0, bolt=True):
    """Armour brackets hugging a frame's chamfered corners, each with a hex bolt; roles per corner
    (tl, tr, br, bl). They stay inside the 28 px corner squares so a 9-slice frame stretches clean."""
    tl = [(4, 17), (17, 4), (25, 4), (25, 8), (8, 25), (4, 25)]
    corners = (tl, mirror(tl, w=w), mirror(tl, w=w, h=h), mirror(tl, h=h))
    spots = ((13.5, 13.5), (w - 13.5, 13.5), (w - 13.5, h - 13.5), (13.5, h - 13.5))
    for pts, role, (bx, by) in zip(corners, roles, spots):
        K.prism(pts, z0, z1, role, bevel=1.5)
        if bolt:
            K.bolt(bx, by, z1, r=2.8)


# ----------------------------------------------------------------------------- the pieces

def frame_panel(K):
    w = h = 160
    K.prism(chamfer(rect(0, 0, w, h), 14), 0, 6, "gunmetal", bevel=2.0, holes=[chamfer(rect(24, 24, w - 24, h - 24), 2)])
    K.prism(chamfer(rect(3, 3, w - 3, h - 3), 13), 4, 10, "iron", bevel=2.5, holes=[chamfer(rect(17, 17, w - 17, h - 17), 6)])
    K.prism(rect(20, 20, w - 20, h - 20), -3, 0.5, "glass")
    corner_brackets(K, w, h, ("oxide", "oxide", "iron", "iron"))


def frame_inset(K):
    w = h = 64
    K.prism(chamfer(rect(0, 0, w, h), 6), 0, 4, "iron", bevel=1.2, holes=[chamfer(rect(6, 6, w - 6, h - 6), 3)])
    K.prism(chamfer(rect(4, 4, w - 4, h - 4), 5), -2, 2.5, "gunmetal", bevel=2.5, holes=[chamfer(rect(10, 10, w - 10, h - 10), 2)])
    K.prism(rect(8, 8, w - 8, h - 8), -6, -1.5, "glass")


def bar_track(K):
    w, h = 96, 16
    K.prism([(0, 8), (5, 0), (w - 5, 0), (w, 8), (w - 5, h), (5, h)], 0, 5, "gunmetal", bevel=1.5,
            holes=[[(4.5, 8), (7.5, 3.5), (w - 7.5, 3.5), (w - 4.5, 8), (w - 7.5, 12.5), (7.5, 12.5)]])
    K.prism(rect(4, 2, w - 4, h - 2), -3, -1, "socket")


def bar_fill(role):
    def build(K):
        w, h = 64, 10
        K.prism([(0, 5), (4, 0.5), (w - 4, 0.5), (w, 5), (w - 4, 9.5), (4, 9.5)], 0, 2.5, role, bevel=1.0)
    return build


def button(state):
    def build(K):
        w, h = 160, 56
        z = 3.0 if state == "pressed" else 9.0      # 6 mm of travel
        lens = {"idle": "lens_idle", "hover": "lens_hover", "pressed": "lens_hot"}[state]
        # the pressed key sits in its socket's shadow: its cap reads darker, not just lower
        keycap(K, (0, w, 0, 12, w - 12, h), (9.5, w - 9.5, 8, 18.5, w - 18.5, 48), z, lens,
               key_role="gunmetal" if state == "pressed" else "iron")
    return build


def tab(active):
    def build(K):
        w, h = 120, 44
        K.prism(chamfer([(0, 0), (106, 0), (w, 14), (w, h), (0, h)], [3, 0, 0, 0, 0]), 0, 5, "gunmetal", bevel=1.5)
        step = [(5, 5), (105, 5), (115, 15), (115, h + 2), (5, h + 2)]
        top = 10.0 if active else 7.5
        pip = rect(7.5, 11.5, 13.5, 15.5)
        K.prism(chamfer(step, [2, 0, 0, 0, 0]), 4, top, "iron" if active else "gunmetal2", bevel=2.0,
                cuts=[(chamfer(pip, 1), top - 2.0, top + 1)])
        K.prism(pip, top - 4, top - 1.2, "lens_hover" if active else "lens_off")
        if active:
            K.prism([(5.5, 5.5), (104.5, 5.5), (108.5, 9.5), (5.5, 9.5)], top - 0.5, top + 0.6, "signal", bevel=0.4)
    return build


def strip(bottom):
    def build(K):
        w, h = 512, 56

        def Y(y):
            return h - y if bottom else y

        def poly(pts, flip_x=False):
            q = [(x, Y(y)) for x, y in pts]
            if bottom:
                q.reverse()
            return mirror(q, w=w) if flip_x else q

        # the rail body: a plate whose screen-side corners (bottom for the top strip) are cut as prows
        body = chamfer([(0, 0), (w, 0), (w, h), (0, h)], [0, 0, 20, 20])
        K.prism(poly(body), 0, 6, "gunmetal", bevel=2.0, holes=[poly(chamfer(rect(60, 13, w - 60, 44), 4))])
        K.prism(poly(rect(56, 11, w - 56, 46)), -3, -1, "gunmetal2")
        # the conduit rail along the outer edge, and the steel trim along the inner lip
        K.prism(poly(rect(0, 0, w, 10)), 4, 8, "iron", bevel=1.5)
        K.pipe(30, w - 30, Y(5.0), 2.6, 9.0, "steel")
        K.prism(poly(rect(40, 48.5, w - 40, 50.5)), 5, 6.6, "iron", bevel=0.5)
        # the end caps: armoured wedges pointing in toward the field, bolted, red-ranked, hazard-notched
        # (the cap's prow stops at 84 px so its shadow ends inside the 96 px margin)
        cap = chamfer([(2, 1), (66, 1), (84, 19), (84, 37), (68, 54), (22, 54), (2, 34)], [0, 3, 2, 2, 3, 2, 0])
        haz = [(12, 14), (38, 14), (32, 24), (6, 24)]
        rank = [(44, 12), (53, 12), (64, 28), (53, 44), (44, 44), (55, 28)]
        for fx in (False, True):
            K.prism(poly(cap, fx), 5, 11.5, "iron", bevel=2.2)
            K.prism(poly(chamfer(haz, 1), fx), 10.9, 12.1, "hazard", bevel=0.5)
            K.prism(poly(rank, fx), 10.9, 12.3, "signal", bevel=0.6)
            for bx, by in ((14, 38), (32, 44), (73, 28)):
                X = w - bx if fx else bx
                K.bolt(X, Y(by), 11.5, r=3.0)
            K.prism(poly(rect(16, 31, 30, 33), fx), 11.1, 11.9, "bone")
            K.prism(poly(rect(16, 35, 24, 37), fx), 11.1, 11.9, "bone")
    return build


def divider(K):
    w, h = 128, 10
    K.prism([(0, 5), (8, 0), (w - 8, 0), (w, 5), (w - 8, h), (8, h)], 0, 4, "iron", bevel=1.4,
            cuts=[(rect(14, 4, w - 14, 6), 2.6, 5)])
    for pts in ([(0.8, 5), (7.6, 1.2), (12, 1.2), (12, 8.8), (7.6, 8.8)],):
        K.prism(pts, 3.5, 5.0, "signal", bevel=0.6)
        K.prism(mirror(pts, w=w), 3.5, 5.0, "signal", bevel=0.6)


def nameplate(K):
    w, h = 460, 56
    base = chamfer([(0, 0), (436, 0), (w, 24), (w, h), (0, h)], [3, 0, 0, 3, 8])
    field = chamfer(rect(84, 11, 420, 45), 4)
    K.prism(base, 0, 6, "gunmetal", bevel=2.0, holes=[field])
    K.prism(rect(80, 8, 424, 48), -3, -0.5, "gunmetal2")
    # rank block: signal red, chevron-pointed toward the text, three bone rank chevrons
    K.prism(chamfer([(6, 6), (60, 6), (74, 28), (60, 50), (6, 50)], 2), 5, 10, "signal", bevel=1.8)
    for i in range(3):
        x = 16 + i * 13
        K.prism([(x, 14), (x + 6, 14), (x + 14, 28), (x + 6, 42), (x, 42), (x + 8, 28)], 9.4, 10.6, "bone", bevel=0.4)
    # rivets along both edges of the field, and the prow end: a hazard notch and two bolts
    for x in range(96, 420, 28):
        K.cone(x, 5.5, 2.0, 1.3, 5.5, 7.4, "steel", seg=8)
        K.cone(x, 50.5, 2.0, 1.3, 5.5, 7.4, "steel", seg=8)
    K.prism(chamfer([(428, 28), (452, 28), (452, 50), (428, 50)], [0, 4, 0, 0]), 5.5, 8, "iron", bevel=1.2)
    K.prism([(431, 32), (449, 32), (449, 38), (431, 38)], 7.6, 8.6, "hazard", bevel=0.3)
    K.bolt(436, 44, 8, r=2.6)
    K.bolt(446, 44, 8, r=2.6)


def helm_pad(K):
    w = h = 300
    # the bezel: heavier along the top (a prow band), chamfered hard at every corner
    K.prism(chamfer(rect(0, 0, w, h), [30, 30, 20, 20]), 0, 6, "gunmetal", bevel=2.0,
            holes=[chamfer(rect(20, 38, w - 20, h - 20), 6)])
    K.prism(chamfer(rect(3, 3, w - 3, h - 3), [28, 28, 18, 18]), 4, 11, "iron", bevel=2.5,
            holes=[chamfer(rect(14, 32, w - 14, h - 14), 8)])
    band = chamfer([(34, 4), (w - 34, 4), (w - 52, 26), (w / 2 + 14, 26), (w / 2, 31), (w / 2 - 14, 26), (52, 26)], 1.5)
    K.prism(band, 10, 15, "oxide", bevel=1.8)
    K.prism([(w / 2 - 22, 9), (w / 2 + 22, 9), (w / 2 + 8, 22), (w / 2 - 8, 22)], 14.4, 15.8, "signal", bevel=0.6)
    for i in range(5):
        x = 64 + i * 7
        K.prism(rect(x, 11, x + 3, 19), 14.6, 15.4, "bone")
        K.prism(rect(w - x - 3, 11, w - x, 19), 14.6, 15.4, "bone")
    for bx, by in ((20, 20), (w - 20, 20), (16, h - 16), (w - 16, h - 16)):
        K.bolt(bx, by, 11, r=3.4)
    # the key grid: one plate holding nine sockets, the well floor under it, nine wedge keys
    K.prism(rect(16, 34, w - 16, h - 16), -12, -9, "socket")
    cols, rows = (22, 102, 198, 278), (40, 114, 202, 276)
    socks, keys = [], []
    for r in range(3):
        for c in range(3):
            x0, x1, y0, y1 = cols[c] + 3, cols[c + 1] - 3, rows[r] + 3, rows[r + 1] - 3
            s = (y1 - y0) * 0.2
            socks.append((x0, x1, y0, x0 + s, x1 - s, y1))
            keys.append((c, r, (x0 + 7, x1 - 7, y0 + 7, x0 + s + 5, x1 - s - 5, y1 - 7)))
    K.prism(rect(18, 36, w - 18, h - 18), -4, 4, "gunmetal", bevel=1.8,
            holes=[chamfer(trap(*sk), 3) for sk in socks])
    for c, r, key in keys:
        centre = (c, r) == (1, 1)
        keycap(K, None, key, 10.0 if centre else 9.0, "lens_hover" if centre else "lens_idle",
               key_role="oxide" if centre else "iron", bezel_role=None, lens_inset=7.0 if centre else 6.0)


def ssd_bezel(K):
    w, h = 460, 300
    K.prism(chamfer(rect(0, 0, w, h), [30, 30, 16, 16]), 0, 6, "gunmetal", bevel=2.0,
            holes=[chamfer(rect(28, 34, w - 28, h - 30), 10)])
    K.prism(chamfer(rect(3, 3, w - 3, h - 3), [28, 28, 14, 14]), 4, 11, "iron", bevel=2.5,
            holes=[chamfer(rect(20, 26, w - 20, h - 22), 14)])
    K.prism(rect(24, 30, w - 24, h - 26), -4, -1, "glass")
    # the top band: an oxide-red armour wedge with the faction's red chevron and bone rank ticks
    K.prism(chamfer([(56, 1), (w - 56, 1), (w - 74, 20), (74, 20)], 1.5), 10, 14, "oxide", bevel=1.8)
    K.prism([(w / 2 - 26, 4), (w / 2 + 26, 4), (w / 2 + 12, 17), (w / 2 - 12, 17)], 13.4, 14.8, "signal", bevel=0.6)
    for i in range(6):
        x = 96 + i * 8
        K.prism(rect(x, 7, x + 3.5, 14), 13.6, 14.4, "bone")
        K.prism(rect(w - x - 3.5, 7, w - x, 14), 13.6, 14.4, "bone")
    # corner brackets, hazard notches at the lower corners
    corner_brackets(K, w, h, ("oxide", "oxide", "iron", "iron"), z0=10, z1=15)
    for pts in ([(6, 236), (16, 236), (16, 262), (6, 252)],):
        K.prism(pts, 10, 12.5, "hazard", bevel=0.6)
        K.prism(mirror(pts, w=w), 10, 12.5, "hazard", bevel=0.6)
    # side vents: louvres in the side bands
    for sx in (False, True):
        for i in range(9):
            y = 92 + i * 13
            pts = rect(7, y, 17, y + 7)
            K.prism(mirror(pts, w=w) if sx else pts, 10.4, 12.6, "gunmetal2", bevel=1.6)
    # the bottom conduit, clamped, and its gauge plate
    K.pipe(64, w - 64, h - 11, 3.2, 13.0, "steel")
    K.pipe(84, w - 84, h - 4.5, 2.0, 11.6, "iron")
    for x in (80, 168, w / 2, w - 168, w - 80):
        K.prism(chamfer(rect(x - 5, h - 18, x + 5, h - 2), 1.5), 10, 17, "gunmetal", bevel=1.2)
        K.bolt(x, h - 10, 17, r=2.2)


def dial(K):
    w = h = 120
    octo = chamfer(rect(0, 0, w, h), 34)
    face = chamfer(rect(22, 22, w - 22, h - 22), 22)
    K.prism(octo, 0, 6, "gunmetal", bevel=2.0, holes=[chamfer(rect(20, 20, w - 20, h - 20), 20)])
    K.prism(chamfer(rect(6, 6, w - 6, h - 6), 30), 4, 12, "iron", bevel=3.0, holes=[face])
    K.prism(rect(16, 16, w - 16, h - 16), -4, -1, "glass")
    # bolts on the four diagonal facets, a red zero wedge on the top facet
    for bx, by in ((22, 22), (w - 22, 22), (w - 22, h - 22), (22, h - 22)):
        K.bolt(bx, by, 12, r=3.0)
    K.prism([(w / 2 - 8, 7), (w / 2 + 8, 7), (w / 2, 18)], 11.4, 13, "signal", bevel=0.6)
    K.prism(rect(w / 2 - 9, h - 15, w / 2 + 9, h - 11), 11.6, 12.6, "bone")


def lamp(colour):
    def build(K):
        w = h = 64
        K.prism(chamfer(rect(0, 0, w, h), 14), 0, 5, "gunmetal", bevel=1.8, holes=[chamfer(rect(12, 12, w - 12, h - 12), 8)])
        K.prism(chamfer(rect(8, 8, w - 8, h - 8), 9), -3, 2.5, "socket")
        K.cone(w / 2, h / 2, 19, 11, 2, 13, f"lamp_{colour}", seg=8)
        # the cage: an octagonal hoop and three bars over the lens
        K.prism(chamfer(rect(9, 9, w - 9, h - 9), 12), 11, 16, "iron", bevel=1.4, holes=[chamfer(rect(14, 14, w - 14, h - 14), 9)])
        for x in (22, 32, 42):
            K.prism(rect(x - 1.8, 11, x + 1.8, h - 11), 14.5, 17.5, "iron", bevel=0.9)
        for bx, by in ((9, 9), (w - 9, 9), (w - 9, h - 9), (9, h - 9)):
            K.bolt(bx, by, 5, r=2.4)
    return build


def crest(K):
    w = h = 96
    shield = chamfer([(6, 10), (30, 2), (66, 2), (90, 10), (90, 50), (48, 94), (6, 50)], 3)
    K.prism(shield, 0, 5, "gunmetal", bevel=2.0)
    K.prism(chamfer([(10, 13), (31, 6), (65, 6), (86, 13), (86, 48), (48, 88), (10, 48)], 3), 3, 7, "iron", bevel=1.6,
            holes=[chamfer([(16, 17), (32, 12), (64, 12), (80, 17), (80, 46), (48, 80), (16, 46)], 2)])
    # two blade chevrons sweeping back from the prow
    chev1 = [(48, 36), (80, 52), (80, 61), (48, 47), (16, 61), (16, 52)]
    chev2 = [(48, 52), (70, 63), (64, 70), (48, 62), (32, 70), (26, 63)]
    K.prism(chev1, 4, 10, "iron", bevel=1.8)
    K.prism(chev2, 4, 9, "iron", bevel=1.6)
    # the prow: a forward wedge, with a red enamel inlay down its spine
    prow = [(48, 6), (60, 38), (56, 70), (48, 80), (40, 70), (36, 38)]
    inlay = [(48, 16), (54, 39), (51.5, 64), (48, 69), (44.5, 64), (42, 39)]
    K.prism(prow, 4, 14, "iron", bevel=2.6, cuts=[(chamfer(inlay, 0.8), 11.5, 16)])
    K.prism(inlay, 9, 12.6, "enamel", bevel=0.8)


# name: (kind, (w, h), margins (t, r, b, l) or None, stretch, builder)
PIECES = {
    "frame.panel":   ("nine", (160, 160), (28, 28, 28, 28), "xy", frame_panel),
    "frame.inset":   ("nine", (64, 64), (14, 14, 14, 14), "xy", frame_inset),
    "bar.track":     ("nine", (96, 16), (0, 12, 0, 12), "x", bar_track),
    "bar.fill":      ("nine", (64, 10), (0, 5, 0, 5), "x", bar_fill("fill_amber")),
    "bar.fill.red":  ("nine", (64, 10), (0, 5, 0, 5), "x", bar_fill("fill_red")),
    "button.idle":   ("nine", (160, 56), (14, 26, 14, 26), "x", button("idle")),
    "button.hover":  ("nine", (160, 56), (14, 26, 14, 26), "x", button("hover")),
    "button.pressed": ("nine", (160, 56), (14, 26, 14, 26), "x", button("pressed")),
    "tab.idle":      ("nine", (120, 44), (16, 16, 16, 16), "x", tab(False)),
    "tab.active":    ("nine", (120, 44), (16, 16, 16, 16), "x", tab(True)),
    "strip.top":     ("nine", (512, 56), (10, 96, 10, 96), "x", strip(False)),
    "strip.bottom":  ("nine", (512, 56), (10, 96, 10, 96), "x", strip(True)),
    "divider":       ("nine", (128, 10), (0, 16, 0, 16), "x", divider),
    "nameplate":     ("hero", (460, 56), None, None, nameplate),
    "helm.pad":      ("hero", (300, 300), None, None, helm_pad),
    "ssd.bezel":     ("hero", (460, 300), None, None, ssd_bezel),
    "dial":          ("hero", (120, 120), None, None, dial),
    "lamp.alert.green": ("hero", (64, 64), None, None, lamp("green")),
    "lamp.alert.yellow": ("hero", (64, 64), None, None, lamp("yellow")),
    "lamp.alert.red": ("hero", (64, 64), None, None, lamp("red")),
    "crest":         ("hero", (96, 96), None, None, crest),
}


# ----------------------------------------------------------------------------- lighting and render

def sun(name, direction, energy, colour, angle_deg):
    ld = bpy.data.lights.new(name, "SUN")
    ld.energy = energy
    ld.color = colour
    ld.angle = math.radians(angle_deg)
    ob = bpy.data.objects.new(name, ld)
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = Vector(direction).normalized().to_track_quat("-Z", "Y")
    bpy.context.scene.collection.objects.link(ob)


def setup(w_px, h_px, samples):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    cy = sc.cycles
    cy.device = "CPU"
    cy.samples = samples
    cy.use_adaptive_sampling = False
    cy.seed = 0
    cy.use_denoising = True
    cy.denoiser = "OPENIMAGEDENOISE"
    cy.max_bounces = 4
    cy.diffuse_bounces = 2
    cy.glossy_bounces = 2
    cy.transmission_bounces = 0
    cy.sample_clamp_indirect = 3.0
    cy.filter_width = 1.0
    sc.render.film_transparent = True
    sc.render.resolution_x, sc.render.resolution_y = w_px, h_px
    sc.render.resolution_percentage = 100
    sc.render.threads_mode = "AUTO"
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
    sc.view_settings.exposure = 0.0
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    sc.render.image_settings.color_depth = "8"
    sc.render.image_settings.compression = 100
    # no metadata stamps (date, render time, file name): the PNGs are byte-reproducible
    for attr in dir(sc.render):
        if attr.startswith("use_stamp"):
            setattr(sc.render, attr, False)
    world = bpy.data.worlds.new("World")
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.055, 0.06, 0.07, 1.0)
    bg.inputs["Strength"].default_value = 1.0
    sc.world = world
    # the harsh key from the top left (light travels toward +x, -y, down), a cool rim from the bottom right
    sun("Key", (1.0, -1.0, -1.25), 5.0, (1.0, 0.96, 0.9), 3.0)
    sun("Rim", (-1.0, 1.0, -0.8), 1.1, (0.62, 0.78, 1.0), 6.0)


def strip_png_metadata(path):
    """Drop every ancillary text, time and EXIF chunk from a PNG (Cycles writes its render time
    into the file), keeping the image chunks byte for byte, so a rerun writes identical files."""
    with open(path, "rb") as f:
        data = f.read()
    out, i = [data[:8]], 8
    while i < len(data):
        n = struct.unpack(">I", data[i:i + 4])[0]
        kind = data[i + 4:i + 8]
        if kind not in (b"tEXt", b"zTXt", b"iTXt", b"tIME", b"eXIf", b"pHYs"):
            out.append(data[i:i + 12 + n])
        i += 12 + n
    with open(path, "wb") as f:
        f.write(b"".join(out))


def render_piece(name, out_dir, samples):
    kind, (w, h), margins, stretch, build = PIECES[name]
    panel_kit.reset_scene()
    setup(w * SCALE, h * SCALE, samples)
    K = Piece(name, w, h, hero=(kind == "hero"))
    build(K)
    panel_kit.camera(w * MM, h * MM)
    sc = bpy.context.scene
    sc.render.filepath = os.path.join(out_dir, f"{name}.png")
    bpy.ops.render.render(write_still=True)
    strip_png_metadata(sc.render.filepath)
    entry = {"kind": kind, "size": [w, h], "file": f"{name}.png"}
    if kind == "nine":
        entry["margins"] = list(margins)
        entry["stretch"] = stretch
    return entry


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(ROOT, "assets", "ui", "kethran"))
    ap.add_argument("--only", default="")
    ap.add_argument("--samples", type=int, default=64)
    a = ap.parse_args(argv)
    out = os.path.abspath(a.out)
    os.makedirs(out, exist_ok=True)
    names = [n for n in a.only.split(",") if n] or list(PIECES)
    man_path = os.path.join(out, "manifest.json")
    man = {"skin": "kethran", "scale": SCALE, "pieces": {}}
    if a.only and os.path.exists(man_path):
        with open(man_path) as f:
            man = json.load(f)
    t0 = time.time()
    for n in names:
        t = time.time()
        man["pieces"][n] = render_piece(n, out, a.samples)
        print(f"KETHRAN {n}: {time.time() - t:.1f} s", flush=True)
    man["pieces"] = {k: man["pieces"][k] for k in PIECES if k in man["pieces"]}
    with open(man_path, "w") as f:
        json.dump(man, f, indent=2)
        f.write("\n")
    print(f"KETHRAN done: {len(names)} pieces in {time.time() - t0:.1f} s", flush=True)


if __name__ == "__main__":
    main()
