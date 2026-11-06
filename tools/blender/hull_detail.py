"""Hull detail: the high-poly layer a ship's own texture is baked from (the painted-bake skill, ship mode; Ship Design
Bible chapters 14 and 15). painted_bake.py --ship builds a ship's low-poly hull, then calls build() here, and bakes what
this builds onto the low-poly hull's own UVs (selected to active).

The owner, 2026-11-05, with the reference game's orthographic sheet of a ship: "the gorn ships have things like vents,
lights, and sub armor pieces, and primary armor peices. Armor pieces are carefully craftyer to not look the same, in your
mockup armor pieces are square, and there are no distinct features. like under armor area with greebles", "notice stuff
like liveries, smooth sphereical portions of armor, and specifically shaped vents", "no repeating elemnts, single axis
symmetry, and lots of shading to give the armor depth". What is taken is how such a hull is layered, never its art.

What is built, in ship space (+x port, +y up, +z bow, metres; Blender's object space, imported as it is):
  under     a copy of the hull: the under-armour layer, dark, its light strips kept as lamps
  armour    the primary armour: the hull's own faces, grouped by which way they face, thickened outward and cut apart along
            gap lines (swept chevrons on the top and bottom, raked cuts on the flanks, every station's spacing, sweep and
            spine width its own), so no two plates share an outline; holes cut where a dome, fan or vent sits and where
            the data opens the armour onto the layer beneath
  greebles  machinery on the under layer (blocks, sub-armour slabs, pipe runs, ribs), only where the armour leaves it bare
  domes     smooth spherical armour sunk in a collar, held by straps that hug the sphere
  fans      round vents: a recess, a hub, tilted fins, a ring of lamps
  louvres   shaped vents (a chevron, a raked slot) cut through the armour, slanted slats inside
  lamps     small lit blocks, the eye lamp in a housing
Everything off the centreline is built once on the port side and mirrored, so the ship has one axis of symmetry. Each
piece carries a colour attribute `rid` (a hash of its mirrored position), so a plate and its mirror take the same paint.

The layout is data: data/ships/ssarith/hull_detail.json.
"""

import math
import random

import bmesh
import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

KIND = {"under": 0.12, "greeble": 0.28, "sub": 0.39, "armour": 0.5, "dome": 0.64, "slat": 0.74, "vent": 0.82, "strip": 0.89,
        "lamp": 0.95}
# the strips and green lamps a true green (owner, 2026-11-05, of the hull emission: "I wish it were more green")
LAMP_RGB = {"bay": (0.75, 0.8, 0.7), "red": (1.0, 0.12, 0.06), "amber": (1.0, 0.78, 0.3), "green": (0.1, 1.0, 0.24), "strip": (0.1, 1.0, 0.24),
            "white": (1.0, 1.0, 1.0), "flood": (1.0, 0.92, 0.78), "window": (0.86, 1.0, 0.84)}
PANES = []  # every window's and bay's pane, filled by build(): kind, lit, w_m, h_m, centre c, axes u, v and normal n
FIXTURES = []  # the lights the bake lights the hull with, filled by build(): dicts of kind, pos, dir, rgb, power_w, cone_deg
MATS = []  # (material, switch) for every material made here: switch(what) relinks its output for the next bake


# ----------------------------------------------------------------------------- materials
GLOW_W_M2 = 6.0   # how brightly a lamp's or window's own face lights the hull next to it, in the light pass
BEVEL_M = {"armour": 0.3, "sub": 0.14, "dome": 0.05}   # the armour's edges rolled wide, so each plate reads pillowed in the light


def kind_material(kind, glow=None):
    """A material that can show, by switch(what): 'data' (red the kind, green the piece's rid, blue its edges), 'glow'
    (its light, black unless a lamp), 'position' (ship space) or 'shade' (white diffuse, bevelled normals, for the sun
    and normal bakes)."""
    m = bpy.data.materials.new(f"hd_{kind}" + ("_" + glow if glow else ""))
    m.use_nodes = True
    N, L = m.node_tree.nodes, m.node_tree.links
    for n in list(N):
        N.remove(n)
    out = N.new("ShaderNodeOutputMaterial")
    attr = N.new("ShaderNodeAttribute"); attr.attribute_name = "rid"
    sep = N.new("ShaderNodeSeparateColor"); L.new(attr.outputs["Color"], sep.inputs[0])
    geo = N.new("ShaderNodeNewGeometry")
    bev = N.new("ShaderNodeBevel"); bev.samples = 8; bev.inputs["Radius"].default_value = BEVEL_M.get(kind, 0.08)
    dot = N.new("ShaderNodeVectorMath"); dot.operation = "DOT_PRODUCT"
    L.new(bev.outputs[0], dot.inputs[0]); L.new(geo.outputs["Normal"], dot.inputs[1])
    inv = N.new("ShaderNodeMath"); inv.operation = "SUBTRACT"; inv.inputs[0].default_value = 1.0
    L.new(dot.outputs["Value"], inv.inputs[1])
    comb = N.new("ShaderNodeCombineColor"); comb.inputs[0].default_value = KIND[kind]
    L.new(sep.outputs[0], comb.inputs[1]); L.new(inv.outputs[0], comb.inputs[2])
    rgb = N.new("ShaderNodeRGB"); rgb.outputs[0].default_value = (*(LAMP_RGB[glow] if glow else (0, 0, 0)), 1.0)
    em = N.new("ShaderNodeEmission")
    lit = N.new("ShaderNodeEmission"); L.new(rgb.outputs[0], lit.inputs[0]); lit.inputs[1].default_value = GLOW_W_M2
    bs = N.new("ShaderNodeBsdfDiffuse"); bs.inputs["Color"].default_value = (1, 1, 1, 1); L.new(bev.outputs[0], bs.inputs["Normal"])
    src = {"data": comb.outputs[0], "glow": rgb.outputs[0], "position": geo.outputs["Position"]}

    def switch(what):
        if what == "light" and glow:      # the light pass: lamps and windows shine on the hull around them
            L.new(lit.outputs[0], out.inputs[0])
        elif what in ("shade", "light"):
            L.new(bs.outputs[0], out.inputs[0])
        else:
            L.new(src[what], em.inputs[0]); L.new(em.outputs[0], out.inputs[0])
    switch("data")
    MATS.append((m, switch))
    return m


def show(what):
    for _, sw in MATS:
        sw(what)


# ----------------------------------------------------------------------------- objects and frames
def obj_from(bm, name, mat, rid=0.0):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o)
    me.materials.append(mat)
    set_rid(o, rid)
    return o


def set_rid(o, rid):
    me = o.data
    at = me.color_attributes.get("rid") or me.color_attributes.new("rid", "FLOAT_COLOR", "POINT")
    for d in at.data:
        d.color = (rid, 0.0, 0.0, 1.0)


def rid_of(p, salt=0.0):
    """A number 0..1 from a position, the same for a point and its mirror."""
    x = round(abs(p.x), 1) * 12.9898 + round(p.y, 1) * 78.233 + round(p.z, 1) * 37.719 + salt * 19.31
    return (math.sin(x) * 43758.5453) % 1.0


def frame_at(p, n, mirror=False):
    """A 4x4 matrix: local u along the ship's axis on the surface, v across, w out along the normal. mirror gives the
    port frame reflected through x = 0 (left-handed), so a built piece comes out as the exact mirror image."""
    n = n.normalized()
    t = Vector((0, 0, 1)) - n * n.z
    if t.length < 0.2:
        t = Vector((0, 1, 0)) - n * n.y
    t.normalize()
    b = n.cross(t)
    if mirror:
        R = Matrix.Diagonal((-1, 1, 1))
        p, t, b, n = R @ p, R @ t, R @ b, R @ n
    M = Matrix.Identity(4)
    for i, v in enumerate((t, b, n)):
        M[0][i], M[1][i], M[2][i] = v.x, v.y, v.z
    M[0][3], M[1][3], M[2][3] = p.x, p.y, p.z
    return M


def place(bm, M):
    bmesh.ops.transform(bm, matrix=M, verts=bm.verts)
    if M.to_3x3().determinant() < 0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
    return bm


def bm_box(sx, sy, sz, at=(0, 0, 0), rot=None):
    """A box in local coordinates, sz along w, sitting on w = at[2] (its base at at[2])."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    S = Matrix.Diagonal((sx, sy, sz, 1.0))
    T = Matrix.Translation((at[0], at[1], at[2] + sz / 2))
    bmesh.ops.transform(bm, matrix=(T @ (rot or Matrix.Identity(4)) @ S), verts=bm.verts)
    return bm


def bm_cyl(r, h, seg=24, at=(0, 0, 0), r2=None, axis="w"):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=r if r2 is None else r2, depth=h)
    R = Matrix.Identity(4) if axis == "w" else Matrix.Rotation(math.pi / 2, 4, "Y")  # 'u': along the ship
    bmesh.ops.transform(bm, matrix=Matrix.Translation(at) @ R @ Matrix.Translation((0, 0, h / 2 if axis == "w" else 0)), verts=bm.verts)
    return bm


def bm_prism(poly, w0, w1):
    """A prism from a convex polygon [(u, v), ...] in local coordinates, from w0 to w1."""
    bm = bmesh.new()
    lo = [bm.verts.new((u, v, w0)) for u, v in poly]
    hi = [bm.verts.new((u, v, w1)) for u, v in poly]
    n = len(poly)
    bm.faces.new(lo[::-1]); bm.faces.new(hi)
    for i in range(n):
        bm.faces.new((lo[i], lo[(i + 1) % n], hi[(i + 1) % n], hi[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def merge(bms):
    out = bmesh.new()
    for bm in bms:
        me = bpy.data.meshes.new("_tmp"); bm.to_mesh(me); out.from_mesh(me); bpy.data.meshes.remove(me); bm.free()
    return out


def apply_mod(o, kind, **kw):
    m = o.modifiers.new(kind.lower(), kind)
    for k, v in kw.items():
        setattr(m, k, v)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.modifier_apply(modifier=m.name)


def boolean(o, cutter, op):
    apply_mod(o, "BOOLEAN", operation=op, object=cutter, solver="EXACT", use_self=True)


# ----------------------------------------------------------------------------- finding the surface
class Surface:
    """Rays against the low-poly hull, to sit a feature on it."""
    def __init__(self, lo):
        bm = bmesh.new(); bm.from_mesh(lo.data); bm.faces.ensure_lookup_table()
        self.bvh = BVHTree.FromBMesh(bm); self.bm = bm

    def hit(self, face, at):
        a, b = at
        o, d = {"top": ((a, 300, b), (0, -1, 0)), "bottom": ((a, -300, b), (0, 1, 0)),
                "port": ((300, b, a), (-1, 0, 0)), "bow": ((a, b, 400), (0, 0, -1))}[face]
        loc, nor, _, _ = self.bvh.ray_cast(Vector(o), Vector(d))
        if loc is None:
            print(f"hull_detail: no hull under {face} {at}", flush=True)
            return None
        if nor.dot(Vector(d)) > 0:
            nor = -nor
        return loc, nor


# ----------------------------------------------------------------------------- the features
def dome(F, M, mats, cutters):
    """A smooth spherical piece of armour: a sphere sunk so its cap stands h above the hull, in a collar ring, with
    straps that hug it. Returns its hole in the armour."""
    r, h = F["r_m"], F["h_m"]
    foot = math.sqrt(max(r * r - (r - h) ** 2, 0.01))
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=48, v_segments=24, radius=r)
    bmesh.ops.transform(bm, matrix=Matrix.Translation((0, 0, h - r)), verts=bm.verts)
    out = [(place(bm, M), "dome", 0.3)]
    ring = bm_cyl(foot * 1.16, 0.62, 48); inner = bm_cyl(foot * 1.0, 0.8, 48, at=(0, 0, -0.1))
    out.append((place(ring, M), "armour_collar", 0.71, place(inner, M)))
    for ang, w in F.get("straps", []):
        R = Matrix.Rotation(math.radians(ang), 4, "Z")
        sh = bmesh.new(); bmesh.ops.create_uvsphere(sh, u_segments=48, v_segments=24, radius=r + 0.16)
        bmesh.ops.transform(sh, matrix=Matrix.Translation((0, 0, h - r)), verts=sh.verts)
        out.append((place(sh, M), "armour_strap", 0.2 + ang / 400.0,
                    place(bm_box(foot * 2.4, w, r + 1.0, at=(0, 0, -0.2), rot=R), M), (r, h)))
    cutters.append(place(bm_cyl(foot * 1.16, 3.0, 48, at=(0, 0, -1.0)), M))
    return out


def fan(F, M, cutters):
    """A round vent: a recess through the armour, a hub, fins tilted like a turbine's, a ring of lamps round the hub."""
    R, n = F["r_m"], F["fins"]
    parts = [(place(bm_cyl(R * 1.02, 0.12, 48, at=(0, 0, -0.02)), M), "vent"),
             (place(bm_cyl(R * 0.3, 0.38, 32, r2=R * 0.24), M), "greeble")]
    tilt = math.radians(F.get("tilt_deg", 25))
    for i in range(n):
        a = 2 * math.pi * i / n
        rot = Matrix.Rotation(a, 4, "Z") @ Matrix.Translation((R * 0.63, 0, 0)) @ Matrix.Rotation(tilt, 4, "X")
        parts.append((place(bm_box(R * 0.66, 0.1, 0.34, rot=rot), M), "slat"))
    for i in range(F.get("lamps", 0)):
        a = 2 * math.pi * (i + 0.5) / F["lamps"]
        parts.append((place(bm_box(0.2, 0.2, 0.46, at=(math.cos(a) * R * 0.38, math.sin(a) * R * 0.38, 0)), M), "lamp:" + F.get("lamp", "amber")))
    cutters.append(place(bm_cyl(R * 1.04, 3.0, 48, at=(0, 0, -1.0)), M))
    return parts


def louvre_polys(F):
    """The vent's outline as convex polygons (u forward, v across)."""
    L, S = F["len_m"], F["span_m"]
    if F["shape"] == "chevron":  # a V pointing forward, two arms of width w
        w = F["w_m"]
        arm = [(L / 2, 0.0), (L / 2 - w, 0.0), (-L / 2, S / 2 - w * 0.6), (-L / 2 + w * 0.9, S / 2)]
        return [arm, [(u, -v) for u, v in arm[::-1]]]
    k = F.get("rake_m", 0.0)  # a raked slot: a parallelogram
    return [[(-L / 2 + k / 2, -S / 2), (L / 2 + k / 2, -S / 2), (L / 2 - k / 2, S / 2), (-L / 2 - k / 2, S / 2)]]


def louvre(F, M, cutters):
    """A shaped vent cut through the armour, with slanted slats across it, clipped to its outline."""
    parts = []
    polys = louvre_polys(F)
    slant = math.radians(F.get("slant_deg", 35))
    for poly in polys:
        cutters.append(place(bm_prism(poly, -1.0, 2.0), M))
        parts.append((place(bm_prism(poly, -0.05, 0.08), M), "vent"))
        us = [u for u, _ in poly]; vs = [v for _, v in poly]
        u = min(us) + F["pitch_m"] / 2
        while u < max(us):
            bm = bm_box(F["pitch_m"] * 0.55, (max(vs) - min(vs)) * 1.4, 0.36, at=(u, (max(vs) + min(vs)) / 2, 0.02),
                        rot=Matrix.Rotation(slant, 4, "Y"))
            clip_convex(bm, poly)
            parts.append((place(bm, M), "slat"))
            u += F["pitch_m"]
    return parts


def clip_convex(bm, poly):
    """Cut a local mesh down to the inside of a convex polygon's prism."""
    n = len(poly)
    area = sum(poly[i][0] * poly[(i + 1) % n][1] - poly[(i + 1) % n][0] * poly[i][1] for i in range(n))
    for i in range(n):
        (u0, v0), (u1, v1) = poly[i], poly[(i + 1) % n]
        e = Vector((u1 - u0, v1 - v0, 0)); out = Vector((e.y, -e.x, 0)) * (1 if area > 0 else -1)
        geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=(u0, v0, 0), plane_no=out.normalized(), clear_outer=True)


def lamp(F, M):
    s = F["size_m"]
    parts = [(place(bm_box(s, s * 0.7, s * 0.55), M), "lamp:" + F["colour"])]
    if F.get("housing"):
        parts.append((place(bm_box(s * 2.6, s * 1.8, s * 0.3, at=(-s * 0.2, 0, 0)), M), "armour_housing", 0.41))
        parts.append((place(bm_box(s * 1.2, s * 2.4, s * 0.42, at=(-s * 1.5, 0, 0)), M), "armour_housing", 0.53))
    return parts


def window_rows(F, surf):
    """Person-sized windows (Ship Design Bible W1-W3: 1.1 m by 0.6 m, one row a deck, 3.5 m apart, in groups of 4 to 12
    with gaps, about one in five dark) along a stretch of the port flank: [(loc, normal, lit, room kind)]. Placed from data until
    the ship's room plan places them (provisional: W1 wants a room with people behind every one)."""
    out = []
    z0, z1 = F["z_m"]
    for y in F["rows_y_m"]:
        r = random.Random(int(y * 100) * 31 + int(z0 * 10))
        z = z1
        while z > z0:
            n = r.randint(4, 12)
            kind = r.choice(ROOMS[:4])                   # one room kind a group (provisional until the plan says)
            for k in range(n):
                zk = z - k * 1.6
                if zk < z0:
                    break
                h = surf.hit("port", [zk, y])
                if h and abs(h[1].x) > 0.6:     # only on faces that look out to the side
                    out.append((h[0], h[1], r.random() > 0.2, kind))
            z -= n * 1.6 + r.uniform(2.0, 4.5)
    return out


ROOMS = ("quarters", "mess", "workshop", "hold", "bay")   # the room kinds a pane can show (the page's interior mapping)


def fits(surf, M, w, h, frame):
    """Whether a pane w by h with its frame lies flat on the hull at frame M: every corner and edge middle of the framed
    rectangle within 0.12 m of the face's plane. A pane that would cross a bend is left out (Ship Design Bible W2), or,
    for a bay, the build stops (the data must move it)."""
    R3, o = M.to_3x3(), M.to_translation()
    u, v, n = R3 @ Vector((1, 0, 0)), R3 @ Vector((0, 1, 0)), (R3 @ Vector((0, 0, 1))).normalized()
    for a in (-1, 0, 1):
        for b in (-1, 0, 1):
            p = o + u * (a * (w / 2 + frame)) + v * (b * (h / 2 + frame))
            hit = surf.bvh.ray_cast(p + n * 2.0, -n, 4.0)
            if hit[0] is None or abs((hit[0] - p).dot(n)) > 0.12:
                return False
    return True


def pane_box(w, h, kind, lit, M, frame):
    """A pane w by h at frame M: its entry in PANES, from which the page draws the pane itself as its own quad, with a
    clean frame and the room or hangar behind it. Nothing of the pane is baked: the bake keeps only the recess cut for it,
    so no glowing dot in the hull's maps sits under the page's window (owner, 2026-11-06: "I would remove window dots from
    the ships texture since it overlaps with our window cutouts") (owner, 2026-11-05: "windows and hangar bay are kinda messed up, mmake them clean, make the
    border clean"). A baked map of the panes was too coarse for that: a window is a few texels across."""
    R3 = M.to_3x3()
    PANES.append({"kind": kind, "lit": bool(lit), "w_m": w, "h_m": h, "frame_m": frame,
                  "c": [round(c, 4) for c in M.to_translation()],
                  "u": [round(c, 5) for c in (R3 @ Vector((1, 0, 0))).normalized()],
                  "v": [round(c, 5) for c in (R3 @ Vector((0, 1, 0))).normalized()],
                  "n": [round(c, 5) for c in (R3 @ Vector((0, 0, 1))).normalized()]})


def window(lit, M, cutters, frame, kind="quarters"):
    cutters.append(place(bm_box(1.1, 0.6, 3.0, at=(0, 0, -1.0)), M))
    pane_box(1.1, 0.6, kind, lit, M, frame)
    return []


def bay(F, M, cutters, surf, frame):
    """A launch bay: an opening cut through the armour, its mouth a pane onto the hangar behind (drawn by the page), a
    row of amber lamps along its lintel. Its width runs level with the decks: on a face whose frame runs up the ship (a bow
    face), the frame turns a quarter about the normal, so the mouth is w wide and h tall there too."""
    w, h = F["w_m"], F["h_m"]
    if abs(M.col[0].y) > 0.7:
        M = M @ Matrix.Rotation(math.pi / 2, 4, "Z")
    if not fits(surf, M, w, h, frame):
        raise ValueError(f"hull_detail: the bay at {F['face']} {F['at']} ({w} by {h} m) does not lie flat on its face; move it or make it smaller")
    cutters.append(place(bm_box(w, h, 3.0, at=(0, 0, -1.0)), M))
    pane_box(w, h, "bay", F.get("lit", True), M, frame)
    parts = []
    n = max(3, int(w / 2.2))
    up = 1.0 if M.col[1].y >= 0 else -1.0   # the lintel is the edge toward the ship's up, whichever way the frame runs
    for i in range(n):
        parts.append((place(bm_box(0.3, 0.3, 0.3, at=(-w / 2 + (i + 0.5) * w / n, up * (h / 2 + 0.45), 0)), M), "lamp:amber"))
    return parts


def fixture(kind, pos, d, rgb, power, cone=None, size=0.0):
    FIXTURES.append({"kind": kind, "pos": [round(c, 3) for c in pos], "dir": [round(c, 4) for c in d.normalized()],
                     "rgb": list(rgb), "power_w": power, "cone_deg": cone, "size_m": size})


def tangent_aim(n, toward, tilt_deg):
    """A direction along the surface toward `toward`, tilted tilt_deg into it, for a lamp that grazes the plating."""
    t = toward - n * n.dot(toward)
    t = t.normalized() if t.length > 1e-3 else n.orthogonal().normalized()
    a = math.radians(tilt_deg)
    return (t * math.cos(a) - n * math.sin(a)).normalized()


def lights(lo, S, surf, mats, lamps, feats, defaults=None):
    """The hull's light fixtures (Ship Design Bible chapter 9, aircraft at night): the data's floods and logo flood,
    a belly pair, position lights (port red, starboard green, white aft), red beacons top and bottom, white strobes at
    the bow's tips. Each gets a small lit lamp on the hull (so it shows in the emission) and a light for the bake."""
    FIXTURES.clear()
    vs = [v.co for v in lo.data.vertices]
    zmin, zmax = min(v.z for v in vs), max(v.z for v in vs)
    zmid = (zmin + zmax) / 2

    def lamp_at(loc, n, colour, s, mirror=False):
        feats.append(([(place(bm_box(s, s, s * 0.6), frame_at(loc, n, mirror)), "lamp:" + colour)], loc))

    def spot(loc, n, aim, rgb, power, cone, standoff, name):
        p = loc + n * standoff
        fixture(name, p, aim, rgb, power, cone)
        lamp_at(loc, n, "flood", 0.45)
    D = S.get("lighting", {})
    # every flood's throw (owner, 2026-11-06: "lights are kinda small here, can we get bigger cones that illuminate more of
    # the ship?"): stood off the plating, tilted into it, a wide cone, so each lights a broad stretch of hull; a ship's own
    # keys over the defaults', a flood's own keys over both
    FL = {"standoff_m": 0.9, "cone_deg": 90, "tilt_deg": 14, "power_w": 2600, **(defaults or {}).get("flood", {}), **D.get("flood", {})}
    for F in D.get("floods", []):
        h = surf.hit(F["face"], F["at"])
        if not h:
            continue
        loc, n = h
        toward = Vector({"aft": (0, 0, -1), "fwd": (0, 0, 1), "out": (1, 0, 0), "in": (-1, 0, 0), "up": (0, 1, 0), "down": (0, -1, 0)}[F["aim"]])
        for m in ((False, True) if (F["face"] == "port" or abs(loc.x) > 0.3) else (False,)):
            R = Matrix.Diagonal((-1, 1, 1)) if m else Matrix.Identity(3)
            ll, nn, tw = R @ loc, R @ n, R @ toward
            spot(ll, nn, tangent_aim(nn, tw, F.get("tilt_deg", FL["tilt_deg"])), LAMP_RGB["flood"], F.get("power_w", FL["power_w"]), F.get("cone_deg", FL["cone_deg"]), FL["standoff_m"], "hull_flood")
    # rows of floods aimed forward along the flanks, the top and the belly, every step_m (owner, 2026-11-05: "we need some
    # more lights pointed forward to paint more of the hull"); one column down the top and one down the belly, on the
    # centreline (owner, 2026-11-06: "only need 1 column of ligtts on top and bottom of huull"); the defaults' rows, the
    # ship's own keys over them
    A = {**(defaults or {}).get("auto_floods", {}), **D.get("auto_floods", {})}
    if A.get("step_m"):
        body = [v for v in vs if v.z < A.get("to_z_m", zmax)]
        z = zmin + A.get("from_stern_m", 6)
        while z < A.get("to_z_m", zmax) - 4:
            near = [v for v in body if abs(v.z - z) < 3] or body
            ym = (min(v.y for v in near) + max(v.y for v in near)) / 2
            xw = max(v.x for v in near)
            for face, at in (("port", [z, ym]), ("top", [xw * A.get("top_x_frac", 0.4), z]), ("bottom", [xw * A.get("top_x_frac", 0.4), z])):
                h = surf.hit(face, at)
                if not h:
                    continue
                loc, n = h
                for m in ((False, True) if abs(loc.x) > 0.3 else (False,)):   # one on the centreline, a pair off it
                    R_ = Matrix.Diagonal((-1, 1, 1)) if m else Matrix.Identity(3)
                    spot(R_ @ loc, R_ @ n, tangent_aim(R_ @ n, Vector((0, 0, 1)), A.get("tilt_deg", FL["tilt_deg"])), LAMP_RGB["flood"],
                         A.get("power_w", FL["power_w"]), A.get("cone_deg", FL["cone_deg"]), FL["standoff_m"], "hull_flood")
            z += A["step_m"]
    lg = S.get("logo")
    if lg:
        h = surf.hit("top", lg["at"])
        if h:
            c, n = h
            hh = surf.hit("top", [lg["at"][0], lg["at"][1] + lg["size_m"] * 0.85])
            if hh:
                p = hh[0] + hh[1] * 2.2
                fixture("logo_flood", p, c - p, LAMP_RGB["flood"], lg.get("flood_w", 3200), 60)
                lamp_at(hh[0], hh[1], "flood", 0.5)
    for zz, aim in ((zmid + 6, (0, 0, -1)), (zmid - 6, (0, 0, 1))):   # the belly pair, one forward, one aft, pools apart
        h = surf.hit("bottom", [0, zz])
        if h:
            spot(h[0], h[1], tangent_aim(h[1], Vector(aim), FL["tilt_deg"]), LAMP_RGB["flood"], FL["power_w"], FL["cone_deg"], FL["standoff_m"], "belly_flood")
    xmax = max(vs, key=lambda v: v.x)                                  # the widest frame: position lights
    for sign, colour in ((1, "red"), (-1, "green")):
        p = Vector((sign * xmax.x, xmax.y, xmax.z)); n = Vector((sign, 0, 0))
        fixture("position", p + n * 0.6, n, LAMP_RGB[colour], 60, None, 0.4); lamp_at(p, n, colour, 0.5)
    aft = min(vs, key=lambda v: v.z + abs(v.x) * 0.5)
    fixture("position", aft + Vector((0, 0, -0.6)), Vector((0, 0, -1)), LAMP_RGB["white"], 60, None, 0.4)
    lamp_at(aft, Vector((0, 0, -1)), "white", 0.5)
    for face in ("top", "bottom"):                                    # red beacons, mid-ship on the centreline
        h = surf.hit(face, [0, zmid])
        if h:
            fixture("beacon", h[0] + h[1] * 0.6, h[1], LAMP_RGB["red"], 80, None, 0.5); lamp_at(h[0], h[1], "red", 0.6)
    tips = []                                                          # white strobes at the bow's tips
    for v in sorted(vs, key=lambda v: -v.z):
        if v.z < zmax - 18 or v.x < -0.01:
            continue
        if all((v - t).length > 8 for t in tips):
            tips.append(v.copy())
        if len(tips) >= 3:
            break
    for t in tips:
        for sx in ((1, -1) if t.x > 0.3 else (1,)):
            p = Vector((t.x * sx, t.y, t.z))
            fixture("strobe", p + Vector((0, 0, 0.5)), Vector((0, 0, 1)), LAMP_RGB["white"], 40, None, 0.35)
            lamp_at(p, Vector((0, 0, 1)), "white", 0.4)


# ----------------------------------------------------------------------------- the armour
def shell_groups(lo):
    """The hull's faces by which way they face: top, bottom, port, starboard; the rest (bow, stern, the crotches between
    branches) and the light strips carry no armour."""
    me = lo.data
    groups = {"top": [], "bottom": [], "port": [], "starboard": []}
    glow_idx = {i for i, m in enumerate(me.materials) if m and m.name.startswith("lo_glow")}
    for p in me.polygons:
        if p.material_index in glow_idx:
            continue
        n = p.normal
        if n.y > 0.55:
            groups["top"].append(p.index)
        elif n.y < -0.55:
            groups["bottom"].append(p.index)
        elif n.x > 0.55:
            groups["port"].append(p.index)
        elif n.x < -0.55:
            groups["starboard"].append(p.index)
    return groups


def shell_of(lo, faces, t, name, mat):
    bm = bmesh.new(); bm.from_mesh(lo.data)
    keep = set(faces)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.index not in keep], context="FACES")
    for f in bm.faces:
        f.material_index = 0
    o = obj_from(bm, name, mat)
    apply_mod(o, "SOLIDIFY", thickness=t, offset=1.0, use_even_offset=True, use_rim=True)
    return o


def gap_slab(a, b, gap, axes, extent=200.0):
    """A thin prism round the segment a-b drawn in a view's plane, through the whole ship along the view's axis.
    axes: ('x', 'z') for the top and bottom, ('z', 'y') for the flanks."""
    (a0, a1), (b0, b1) = a, b
    d = Vector((b0 - a0, b1 - a1)); L = d.length
    if L < 1e-3:
        return None
    d /= L; nn = Vector((-d.y, d.x)) * (gap / 2)
    ext = d * (gap * 0.6)  # a little past each end, so cuts meet cleanly
    quad = [(a0 - ext.x + nn.x, a1 - ext.y + nn.y), (b0 + ext.x + nn.x, b1 + ext.y + nn.y),
            (b0 + ext.x - nn.x, b1 + ext.y - nn.y), (a0 - ext.x - nn.x, a1 - ext.y - nn.y)]
    return view_prism(quad, axes, -extent, extent)


def view_prism(poly, axes, lo, hi):
    """A prism from a polygon drawn in a view (plan: (x, z), extruded in y; side: (z, y), extruded in x)."""
    bm = bm_prism(poly, lo, hi)
    if axes == ("x", "z"):   # local (u, v, w) = (x, z, y)
        M = Matrix(((1, 0, 0, 0), (0, 0, 1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))
    else:                    # local (u, v, w) = (z, y, x)
        M = Matrix(((0, 0, 1, 0), (0, 1, 0, 0), (1, 0, 0, 0), (0, 0, 0, 1)))
    return place(bm, M)


def armour_cuts(rng, D, zr, xmax, yr):
    """The gap lines: on the top and bottom, a kinked spine and swept chevrons, every station its own; on the flanks,
    raked cuts and a chine line in some bays. Port side only; mirrored by the caller. Returns {view: [(a, b), ...]}."""
    z0, z1 = zr
    plan, side = [], []
    z = z1 - rng.uniform(2, 5)
    st = []
    while z > z0 + 2:
        st.append((z, rng.uniform(*D["spine_half_m"]), rng.uniform(*D["top_sweep"])))
        z -= rng.uniform(*D["top_station_m"])
    for i, (z, s, k) in enumerate(st):
        plan.append(((s, z), (xmax + 2, z - k * (xmax + 2 - s))))       # the swept cut across the side band
        if i + 1 < len(st):
            zn, sn, _ = st[i + 1]
            plan.append(((s, z), (sn, zn)))                                # the spine's edge, kinked at every station
            if rng.random() < 0.55:                                        # an outer seam in some bays only
                o = rng.uniform(0.45, 0.75)
                xa, xb = s + (xmax - s) * o, sn + (xmax - sn) * o
                plan.append(((xa, z - k * (xa - s)), (xb, zn - st[i + 1][2] * (xb - sn))))
        if rng.random() < 0.5:                                             # the spine cut across, a shallow forward V
            plan.append(((0.0, z + 0.9), (s + 0.2, z - 0.5)))
    z = z1 - rng.uniform(3, 8)
    ylo, yhi = yr
    ym = (ylo + yhi) / 2 + rng.uniform(-1, 1)
    prev = None
    while z > z0 + 2:
        r = rng.uniform(*D["flank_rake"])
        side.append(((z, ylo - 2), (z - r * (yhi - ylo + 4), yhi + 2)))
        if prev is not None and rng.random() < 0.5:
            side.append(((prev, ym), (z, ym + rng.uniform(-0.8, 0.8))))
        prev = z
        z -= rng.uniform(*D["flank_station_m"])
    return {"plan": plan, "side": side}


def mirror_cuts(cuts):
    return {"plan": cuts["plan"] + [((-a[0], a[1]), (-b[0], b[1])) for a, b in cuts["plan"]], "side": cuts["side"]}


def build_armour(lo, D, S, rng, holes, mats, layer):
    """One layer of armour plates. layer 'primary': thick, cut by the ship's gap lines (most gaps narrow, some wide
    trenches), the data's exposed areas and the features' holes, and each plate then raised by its own amount so it
    casts its shadow on its neighbours. layer 'sub': thin, under the primary, cut by its own lines with wide gaps, so
    the trenches show a second layer of plating and, in its gaps, the machinery."""
    groups = shell_groups(lo)
    xs = [v.co.x for v in lo.data.vertices]; ys = [v.co.y for v in lo.data.vertices]; zs = [v.co.z for v in lo.data.vertices]
    cuts = mirror_cuts(armour_cuts(rng, D, (min(zs), max(zs)), max(xs), (min(ys), max(ys))))
    primary = layer == "primary"
    t = D["armour_t_m"] if primary else D["sub_t_m"]
    plates = []
    for g, faces in groups.items():
        if not faces:
            continue
        sh = shell_of(lo, faces, t, f"{layer}_{g}", mats["armour" if primary else "sub"])
        view = ("x", "z") if g in ("top", "bottom") else ("z", "y")
        bms = []
        for a, b in (cuts["plan"] if view == ("x", "z") else cuts["side"]):
            wide = rng.random() < (D["trench_share"] if primary else 0.35)
            p = gap_slab(a, b, rng.uniform(*(D["trench_m"] if wide else D["gap_m"])), view)
            if p:
                bms.append(p)
        if primary:
            for e in S.get("exposed", []):
                bms.append(view_prism(e["plan"], ("x", "z"), *e["y_m"]))
        for h in holes:
            bm = bmesh.new(); bm.from_mesh(h.data); bms.append(bm)
        cut = obj_from(merge(bms), "cut_" + g, mats["armour"])
        boolean(sh, cut, "DIFFERENCE")
        bpy.data.objects.remove(cut)
        plates += split_plates(sh)
    for o in plates:
        c = sum((v.co for v in o.data.vertices), Vector()) / max(len(o.data.vertices), 1)
        r = rid_of(c, 0.0 if primary else 5.0)
        set_rid(o, r)
        if primary:   # raised by its own amount along its facing: plates stand proud of one another
            nrm = sum((p.normal * p.area for p in o.data.polygons), Vector())
            if nrm.length > 0:
                lift = nrm.normalized() * (D["lift_m"] * ((r * 7.0) % 1.0))
                for v in o.data.vertices:
                    v.co += lift
    if primary:
        plates += [ov for o in list(plates) if (ov := overlay(o, D, mats)) is not None]
    return plates


def overlay(o, D, mats):
    """A large plate gets a second, smaller plate laid over part of it: the plate copied, cut by a raked line across
    it, the cut closed, drawn in from its edges and lifted, so the armour steps up in layers. Which part, how much and
    at what angle come from the plate's own rid, so no two are alike and a plate and its mirror match."""
    area = sum(p.area for p in o.data.polygons) / 2
    if area < D["overlay_min_m2"]:
        return None
    r = o.data.color_attributes["rid"].data[0].color[0]
    bm = bmesh.new(); bm.from_mesh(o.data)
    c = sum((v.co for v in bm.verts), Vector()) / len(bm.verts)
    nrm = sum((f.normal * f.calc_area() for f in bm.faces), Vector()).normalized()
    t = Vector((0, 0, 1)) - nrm * nrm.z
    t = t.normalized() if t.length > 0.2 else Vector((1, 0, 0))
    b = nrm.cross(t)
    ang = (r * 5.3 % 1.0 - 0.5) * 2.2                       # the cut's rake, up to about 63 degrees either way
    side = 1 if r * 9.1 % 1.0 > 0.5 else -1                 # which side of the cut is kept
    if c.x < 0:
        ang, b = -ang, b                                     # the mirror's rake mirrors
    d = (t * math.cos(ang) + b * math.sin(ang)).normalized() * side
    off = (r * 3.7 % 1.0 - 0.5) * math.sqrt(area) * 0.5
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    res = bmesh.ops.bisect_plane(bm, geom=geom, plane_co=c + d * off, plane_no=d, clear_outer=True)
    edges = [e for e in bm.edges if e.is_boundary]
    if edges:
        bmesh.ops.holes_fill(bm, edges=edges)
    if len(bm.faces) < 4:
        bm.free(); return None
    cc = sum((v.co for v in bm.verts), Vector()) / len(bm.verts)
    k = D["overlay_inset"]
    for v in bm.verts:   # drawn in toward its centre along the surface, then lifted
        dv = v.co - cc
        dv_t = dv - nrm * dv.dot(nrm)
        v.co = cc + nrm * dv.dot(nrm) + dv_t * k + nrm * D["overlay_lift_m"]
    ov = obj_from(bm, "overlay", mats["armour"], (r * 1.618 + 0.31) % 1.0)
    return ov


def split_plates(o):
    bpy.ops.object.select_all(action="DESELECT"); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.separate(type="LOOSE"); bpy.ops.object.mode_set(mode="OBJECT")
    out = []
    for p in list(bpy.context.selected_objects):
        if len(p.data.polygons) < 6 or sum(f.area for f in p.data.polygons) < 1.5:   # slivers the cuts leave
            bpy.data.objects.remove(p)
        else:
            out.append(p)
    return out


# ----------------------------------------------------------------------------- greebles on the bare under layer
def bvh_of(objs):
    """One BVH over the objects' meshes, in world space."""
    bm = bmesh.new()
    for o in objs:
        m = o.data.copy(); m.transform(o.matrix_world); bm.from_mesh(m); bpy.data.meshes.remove(m)
    return BVHTree.FromBMesh(bm) if bm.faces else None


def inside(bvh, p):
    """Whether p is inside the closed meshes of bvh: the first face a ray meets faces away from where it started."""
    if bvh is None:
        return False
    for d in (Vector((0.0, 0.0, 1.0)), Vector((0.31, 0.93, 0.19)).normalized()):
        h = bvh.ray_cast(p, d, 20.0)
        if h[0] is None or h[1].dot(d) <= 0:
            return False
    return True


def greebles(lo, armour, D, rng, mats, holes=None):
    """Machinery in the gaps and trenches the armour leaves. Never inside an opening (a window, a bay, a fan's or a
    dome's well): a greeble there would stand in front of the pane and hide the room behind it."""
    bm = bmesh.new()
    for o in armour:
        bm.from_mesh(o.data)
    abvh = BVHTree.FromBMesh(bm)
    me = lo.data
    glow_idx = {i for i, m in enumerate(me.materials) if m and m.name.startswith("lo_glow")}
    parts = []
    for p in me.polygons:
        if p.material_index in glow_idx or p.center.x < 0.25:
            continue
        nwant = p.area * D["greebles_per_m2"]
        k = int(nwant) + (1 if rng.random() < nwant % 1 else 0)
        vs = [me.vertices[i].co for i in p.vertices]
        for _ in range(k):
            a, b = rng.random(), rng.random()
            if a + b > 1:
                a, b = 1 - a, 1 - b
            q = vs[0] + (vs[1] - vs[0]) * a + (vs[2] - vs[0]) * b
            n = p.normal
            if abvh.ray_cast(q + n * 0.02, n, 0.9)[0] is not None:   # under armour: nobody sees it
                continue
            g = greeble(frame_at(q, n, False), q)
            if holes is not None and any(inside(holes, v.co) for v in g.verts):
                g.free()
                continue
            parts.append(g)   # the port greeble and its mirror, one shape from one seed (the openings are mirrored too)
            parts.append(greeble(frame_at(q, n, True), q))
    return parts


def greeble(M, q):
    """One piece of machinery at q, its shape seeded by where it is, so its mirror is the same piece."""
    r = random.Random(int(abs(q.x) * 1000) * 7919 + int(q.y * 1000) * 104729 + int(q.z * 1000))
    kind = r.random()
    if kind < 0.45:      # a machinery block
        bm = bm_box(r.uniform(0.5, 2.6), r.uniform(0.4, 1.6), r.uniform(0.12, 0.4), at=(0, 0, -0.02))
    elif kind < 0.65:    # a sub-armour slab, thin and wide
        bm = bm_box(r.uniform(1.2, 3.2), r.uniform(0.8, 2.0), r.uniform(0.08, 0.16), at=(0, 0, -0.02),
                    rot=Matrix.Rotation(r.uniform(-0.3, 0.3), 4, "Z"))
    elif kind < 0.85:    # a pipe run along the ship
        bm = merge([bm_cyl(r.uniform(0.1, 0.2), r.uniform(2.0, 6.0), 12, at=(0, j * 0.45, 0.2), axis="u") for j in range(r.randint(1, 3))])
    else:                # ribs across
        bm = merge([bm_box(0.12, r.uniform(0.8, 1.6), 0.26, at=(j * 0.42, 0, -0.02)) for j in range(r.randint(3, 6))])
    return place(bm, M)


# ----------------------------------------------------------------------------- the whole layer
def build(lo, S, D, seed):
    """Build the hull detail for one ship over its low-poly hull `lo` (S: the ship's entry in hull_detail.json, D: the
    defaults). Returns the high-poly objects to bake from."""
    rng = random.Random(seed)
    PANES.clear()
    mats = {k: kind_material(k) for k in ("under", "greeble", "sub", "armour", "dome", "slat", "vent")}
    lamps = {c: kind_material("lamp", c) for c in LAMP_RGB}
    surf = Surface(lo)
    under = lo.copy(); under.data = lo.data.copy(); bpy.context.scene.collection.objects.link(under)
    strip = [lo.data.materials[p.material_index].name.startswith("lo_glow") for p in lo.data.polygons]
    under.data.materials.clear()  # (this zeroes every face's material index, so the strips were read off the hull first)
    under.data.materials.append(mats["under"]); under.data.materials.append(kind_material("strip", "strip"))
    for p, st in zip(under.data.polygons, strip):
        p.material_index = 1 if st else 0
    set_rid(under, 0.0)
    high = [under]
    feats, holes = [], []

    def at_both(F, fn):
        h = surf.hit(F["face"], F["at"])
        if not h:
            return
        loc, nor = h
        mir = (F["face"] == "port") or abs(loc.x) > 0.3
        for m in ((False, True) if mir else (False,)):
            cut = []
            feats.append((fn(F, frame_at(loc, nor, m), cut), loc))
            holes.extend(obj_from(c, "hole", mats["armour"]) for c in cut)

    for F in S.get("domes", []):
        at_both(F, lambda F, M, cut: dome(F, M, mats, cut))
    for F in S.get("fans", []):
        at_both(F, fan)
    for F in S.get("louvres", []):
        at_both(F, louvre)
    for F in S.get("lamps", []):
        at_both(F, lambda F, M, cut: lamp(F, M))
    PF = D["pane_frame_m"]
    for F in S.get("bays", []):
        at_both(F, lambda F, M, cut: bay(F, M, cut, surf, PF["bay"]))
    for F in S.get("windows", []):
        for loc, nor, lit_, kind in window_rows(F, surf):
            if not fits(surf, frame_at(loc, nor), 1.1, 0.6, PF["window"]):
                continue   # it would cross a bend
            for m in (False, True):
                cut = []
                feats.append((window(lit_, frame_at(loc, nor, m), cut, PF["window"], kind), loc))
                holes.extend(obj_from(c, "hole", mats["armour"]) for c in cut)
    lights(lo, S, surf, mats, lamps, feats, D)
    armour = build_armour(lo, D, S, rng, holes, mats, "primary")
    sub = build_armour(lo, D, S, random.Random(seed * 31 + 7), holes, mats, "sub")
    hole_bvh = bvh_of(holes)
    for h in holes:
        bpy.data.objects.remove(h)
    high += armour + sub
    for parts, loc in feats:
        for i, part in enumerate(parts):
            bm, kind = part[0], part[1]
            if kind.startswith("armour_"):
                o = obj_from(bm, kind, mats["armour"], rid_of(loc, salt=part[2]))
                if kind == "armour_collar":
                    inner = obj_from(part[3], "collar_in", mats["armour"]); boolean(o, inner, "DIFFERENCE"); bpy.data.objects.remove(inner)
                elif kind == "armour_strap":
                    slab = obj_from(part[3], "strap_slab", mats["armour"]); boolean(o, slab, "INTERSECT"); bpy.data.objects.remove(slab)
                high.append(o)
            elif kind.startswith("lamp:"):
                high.append(obj_from(bm, "lamp", lamps[kind[5:]], 0.5))
            else:
                high.append(obj_from(bm, kind, mats[kind], rid_of(loc, salt=i * 0.37)))
    gb = greebles(lo, armour + sub, D, rng, mats, hole_bvh)
    if gb:
        high.append(obj_from(merge(gb), "greebles", mats["greeble"], 0.0))
        set_greeble_rids(high[-1])
    return high


def set_greeble_rids(o):
    """Each greeble its own shade: rid per loose part, by its mirrored position."""
    bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table()
    at = o.data.color_attributes["rid"]
    seen = set()
    for v in bm.verts:
        if v.index in seen:
            continue
        island, stack = [], [v]
        while stack:
            w = stack.pop()
            if w.index in seen:
                continue
            seen.add(w.index); island.append(w.index)
            stack.extend(e.other_vert(w) for e in w.link_edges)
        c = sum((bm.verts[i].co for i in island), Vector()) / len(island)
        r = rid_of(c, 3.0)
        for i in island:
            at.data[i].color = (r, 0, 0, 1)
    bm.free()
