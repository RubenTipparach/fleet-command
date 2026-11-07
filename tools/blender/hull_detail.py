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
            gap lines (cut_style 'hunt': swept chevrons on the top and bottom, raked cuts on the flanks, every station's
            spacing, sweep and spine width its own, so no two plates share an outline; 'panels': large panels between
            the room plan's stations, decks and walls, split into sub panels, and cut at the hull's chines); holes cut where a dome, fan or vent sits and where
            the data opens the armour onto the layer beneath
  greebles  machinery on the under layer (blocks, sub-armour slabs, pipe runs, ribs), only where the armour leaves it bare;
            or, with well_cell_m, built in ordered rows in the opened zones (well_modules)
  the kit   with D['kit'] (the Concord and Krai, version 6): escape hatches and access panels set into the plating, vents,
            lit slots, small ports, thin grooves, tags, thruster quads, accent modules, missile and bomb tubes, laid over
            the plating and mirrored (scatter_kit), and their painted warning marks (DECALS)
  bolts     with D['rivets']: pill-shaped bolt wells indented into a plate's corners and long edges (rivets)
  domes     smooth spherical armour sunk in a collar, held by straps that hug the sphere
  fans      round vents: a recess, a hub, tilted fins, a ring of lamps
  louvres   shaped vents (a chevron, a raked slot) cut through the armour, slanted slats inside
  lamps     small lit blocks, the eye lamp in a housing
Everything off the centreline is built once on the port side and mirrored, so the ship has one axis of symmetry. Each
piece carries a colour attribute `rid` (a hash of its mirrored position), so a plate and its mirror take the same paint.

The layout is data: data/ships/<faction>/hull_detail.json (the Ssarith Hunt's, the Concord's, the Krai's).
"""

import math
import random
import time

import bmesh
import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

KIND = {"under": 0.12, "greeble": 0.28, "sub": 0.39, "armour": 0.5, "accent": 0.57, "dome": 0.64, "slat": 0.74, "vent": 0.82, "strip": 0.89,
        "lamp": 0.95}
# the strips and green lamps a true green (owner, 2026-11-05, of the hull emission: "I wish it were more green")
LAMP_RGB = {"bay": (0.75, 0.8, 0.7), "red": (1.0, 0.12, 0.06), "amber": (1.0, 0.78, 0.3), "green": (0.1, 1.0, 0.24), "strip": (0.1, 1.0, 0.24),
            "white": (1.0, 1.0, 1.0), "flood": (1.0, 0.92, 0.78), "window": (0.86, 1.0, 0.84), "blue": (0.3, 0.78, 1.0)}
PANES = []  # every window's and bay's pane, filled by build(): kind, lit, w_m, h_m, centre c, axes u, v and normal n
FIXTURES = []  # the lights the bake lights the hull with, filled by build(): dicts of kind, pos, dir, rgb, power_w, cone_deg
MATS = []  # (material, switch) for every material made here: switch(what) relinks its output for the next bake
DECALS = []  # painted marks (painted_bake.py paints them): dicts of kind, colour, c, u, v, n, w_m, h_m
STORY = []  # what the paint wears where (painted_bake.py --ship): dicts of kind (heat, bay, muzzle, drive), pos, n, size_m

# ----------------------------------------------------------------------------- materials
GLOW_W_M2 = 6.0   # how brightly a lamp's or window's own face lights the hull next to it, in the light pass
BEVEL_M = {"armour": 0.3, "sub": 0.14, "dome": 0.05}
WEAR_BUMP = {}   # how strongly each kind's surface wear (dents, scratches) shows in the render, by kind (data: wear_bump)   # the armour's edges rolled wide, so each plate reads pillowed in the light


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
    bs = N.new("ShaderNodeBsdfDiffuse"); bs.inputs["Color"].default_value = (1, 1, 1, 1)
    nout = bev.outputs[0]
    if WEAR_BUMP.get(kind):   # the surface's own wear before the render (owner: "are you adding wear and tear textures before
        # you render? rthat might make it better"): shallow dents and fine scratches as bump over the bevelled normal, so the
        # render's light and the normal map carry them; Blender's gradient noise (no cheap hash)
        pos = N.new("ShaderNodeTexCoord")
        dent = N.new("ShaderNodeTexNoise"); dent.inputs["Scale"].default_value = 0.6; dent.inputs["Detail"].default_value = 3.0
        L.new(pos.outputs["Object"], dent.inputs["Vector"])
        mp = N.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value = (2.0, 2.0, 60.0)   # scratches run fore and aft
        L.new(pos.outputs["Object"], mp.inputs["Vector"])
        scr = N.new("ShaderNodeTexNoise"); scr.inputs["Scale"].default_value = 3.0; scr.inputs["Detail"].default_value = 1.0
        L.new(mp.outputs["Vector"], scr.inputs["Vector"])
        ramp = N.new("ShaderNodeMapRange"); ramp.inputs["From Min"].default_value = 0.47; ramp.inputs["From Max"].default_value = 0.5
        L.new(scr.outputs["Fac"], ramp.inputs["Value"])
        mix = N.new("ShaderNodeMath"); mix.operation = "MULTIPLY_ADD"; mix.inputs[1].default_value = 0.35
        L.new(ramp.outputs["Result"], mix.inputs[0]); L.new(dent.outputs["Fac"], mix.inputs[2])
        bump = N.new("ShaderNodeBump"); bump.inputs["Strength"].default_value = WEAR_BUMP[kind]; bump.inputs["Distance"].default_value = 0.02
        L.new(mix.outputs["Value"], bump.inputs["Height"]); L.new(bev.outputs[0], bump.inputs["Normal"])
        nout = bump.outputs["Normal"]
    L.new(nout, bs.inputs["Normal"])
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


def sides_of(F, loc):
    """Which sides a feature is built on (False port, True its starboard mirror): both, unless it sits on the centreline
    or its data names one side ('side': 'port' or 'starboard', as a bay's room in the plan opens to one side only)."""
    side = F.get("side", "both")
    if side == "port":
        return (False,)
    if side == "starboard":
        return (True,)
    return (False, True) if (F["face"] == "port" or abs(loc.x) > 0.3) else (False,)


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


def boolean(o, cutter, op, solver="EXACT"):
    if solver == "EXACT":
        apply_mod(o, "BOOLEAN", operation=op, object=cutter, solver="EXACT", use_self=True)
    elif solver == "EXACT_NOSELF":   # small cutters that never cross one another (the bolt wells): quicker
        apply_mod(o, "BOOLEAN", operation=op, object=cutter, solver="EXACT", use_self=False)
    else:
        apply_mod(o, "BOOLEAN", operation=op, object=cutter, solver=solver)


# ----------------------------------------------------------------------------- finding the surface
class Surface:
    """Rays against the low-poly hull, to sit a feature on it."""
    def __init__(self, lo):
        bm = bmesh.new(); bm.from_mesh(lo.data); bm.faces.ensure_lookup_table()
        self.bvh = BVHTree.FromBMesh(bm); self.bm = bm

    def hit(self, face, at):
        a, b = at
        o, d = {"top": ((a, 300, b), (0, -1, 0)), "bottom": ((a, -300, b), (0, 1, 0)),
                "port": ((300, b, a), (-1, 0, 0)), "bow": ((a, b, 400), (0, 0, -1)), "stern": ((a, b, -400), (0, 0, 1))}[face]
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


def window_rows_plan(F, surf):
    """Windows from the ship's room plan (Ship Design Bible W1: a room with people behind every window): for every room
    of the kinds named in F['rooms'] (plan kind or id: pane kind), one row at its deck's eye height along its outer wall,
    1.6 m apart, kept 0.8 m clear of its bulkheads, and only where the room meets the hull (within F['wall_m'] of the skin).
    Symmetric (owner, 2026-11-06: "these empiures SHOULD BE SYMMETRIC"): the rooms of both sides are taken together, each
    window placed once on the port side and mirrored, so a mess to port and quarters to starboard give one mirrored row.
    F['dz_m'], F['dy_m']: the plan's frame to the hull's (the export re-centres a ship). Returns [(loc, normal, lit,
    pane kind, None)], None meaning built on both sides."""
    import json as _json
    import os as _os
    plan = _json.load(open(_os.path.join(_os.path.dirname(__file__), "..", "..", F["plan"])))
    dz, dy, wall = F.get("dz_m", 0.0), F.get("dy_m", 0.0), F.get("wall_m", 3.0)
    floors, out, seen = plan["deck_floor_m"], [], set()
    for room in plan["rooms"]:
        kind = F["rooms"].get(room["id"]) or F["rooms"].get(room["kind"])
        if not kind:
            continue
        r = random.Random(sum((i + 1) * 131 * ord(c) for i, c in enumerate(room["id"])))   # stable from run to run
        x0, x1 = room["x_m"]
        x_out = max(abs(x0), abs(x1))
        z0, z1 = room["z_m"][0] - dz, room["z_m"][1] - dz
        for d in room["decks"]:
            y = floors[d] + F.get("eye_m", 1.5) - dy
            z = z1 - 0.8
            while z > z0 + 0.8:
                key = (d, round(z / 1.6))
                h = surf.hit("port", [z, y])
                if key not in seen and h and abs(h[1].x) > 0.6 and h[0].x - x_out < wall:
                    seen.add(key)
                    out.append((h[0], h[1], r.random() > F.get("dark_share", 0.2), kind, None))
                z -= 1.6
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


def plan_cuts(rng, D, zr, xmax, yr):
    """Plating laid from the ship's own structure (owner, 2026-11-06, of random plating: "still really messy on both
    ships"): a seam only where the room plan puts a bulkhead (every room's fore and aft wall, the stations closer than
    D['plan_merge_m'] taken as one), at every deck (on the flanks) and at the corridors' and outer rooms' walls (on the top
    and bottom), each running the whole hull, so every line on the skin marks something inside it and the layout is as
    ordered and as symmetric as the ship. D['plan'], D['plan_dz_m']: the plan and its frame to the hull's. Port side only;
    mirrored by the caller."""
    import json as _json
    import os as _os
    plan = _json.load(open(_os.path.join(_os.path.dirname(__file__), "..", "..", D["plan"])))
    dz, merge = D.get("plan_dz_m", 0.0), D.get("plan_merge_m", 2.0)
    z0, z1 = zr

    def stations(vals, gap):
        out = []
        for v in sorted(vals):
            if not out or v - out[-1] > gap:
                out.append(v)
        return out
    zs = stations([z - dz for r in plan["rooms"] for z in r["z_m"]] + list(D.get("rings_z_m", [])), merge)
    xs = stations([abs(x) for r in plan["rooms"] for x in r["x_m"] if abs(x) > 0.6], D.get("plan_merge_x_m", 1.5))
    floors = plan["deck_floor_m"]
    ys = stations([f - plan.get("structure_m", 0.6) / 2 for f in floors], 1.0)
    plan_cuts_, side = [], []
    for z in zs:
        if z0 < z < z1:
            plan_cuts_.append(((0.0, z), (xmax + 2, z)))
            side.append(((z, yr[0] - 2), (z, yr[1] + 2)))
    for x in xs:
        plan_cuts_.append(((x, z0 - 2), (x, z1 + 2)))
    for y in ys:
        if yr[0] < y < yr[1]:
            side.append(((z0 - 2, y), (z1 + 2, y)))
    return {"plan": plan_cuts_, "side": side}


def panel_cuts(rng, D, zr, xmax, yr):
    """Large panels and their sub panels (owner, 2026-11-06, with reference sheets: "hull plating, clean hull", "notice
    large panels, and sub panels, liveries, varying colors of armor"): the large panels between the structure's own
    stations (the plan's bulkheads, merged within D['panel_merge_m'], each station seam swept back across the top by
    D['station_sweep'] so the plating follows the form) and its decks and corridor walls, every seam of those 'major'
    (D['major_gap_m'] wide); then each large panel split into sub panels at its own places (D['sub_depth'] levels,
    D['sub_stop'] the share left whole, never under D['kit_min_m']). Port side only; mirrored by the caller."""
    base = plan_cuts(rng, {**D, "plan_merge_m": D.get("panel_merge_m", 6.0), "plan_merge_x_m": D.get("panel_merge_x_m", 3.0)}, zr, xmax, yr)
    z0, z1 = zr
    sw = D.get("station_sweep", 0.0)
    out = {"plan": [], "side": []}
    zs = sorted({round(a[1], 3) for a, b in base["plan"] if abs(a[1] - b[1]) < 1e-6})
    xs = sorted({round(a[0], 3) for a, b in base["plan"] if abs(a[0] - b[0]) < 1e-6})
    ys = sorted({round(a[1], 3) for a, b in base["side"] if abs(a[1] - b[1]) < 1e-6})
    W = xmax + 2
    for z in zs:
        out["plan"].append(((0.0, z), (W, z - sw * W), "major"))
        out["side"].append(((z, yr[0] - 2), (z, yr[1] + 2), "major"))
    for x in xs:
        out["plan"].append(((x, z0 - 2), (x, z1 + 2), "major"))
    for y in ys:
        out["side"].append(((z0 - 2, y), (z1 + 2, y), "major"))
    sub = {"kit_depth": D.get("sub_depth", 2), "kit_stop": D.get("sub_stop", 0.3), "kit_min_m": D.get("kit_min_m", 1.6),
           "kit_chamfer": D.get("kit_chamfer", 0.0), "kit_chamfer_m": D.get("kit_chamfer_m", 0.3)}
    zb = [z0 - 1] + zs + [z1 + 1]
    for i in range(len(zb) - 1):
        xb = [0.0] + [x for x in xs if 0 < x < W] + [W]
        for j in range(len(xb) - 1):   # the sweep moves a station aft as it runs out; the sub cuts stay inside its panel
            kit_split(rng, sub, out, "plan", xb[j], xb[j + 1], zb[i] - sw * xb[j + 1] + 0.3, zb[i + 1] - sw * xb[j + 1] - 0.3, 0)
        yb = [yr[0]] + [y for y in ys if yr[0] < y < yr[1]] + [yr[1]]
        for j in range(len(yb) - 1):
            kit_split(rng, sub, out, "side", zb[i], zb[i + 1], yb[j], yb[j + 1], 0)
    return out


def kit_split(rng, D, out, view, a0, a1, b0, b1, depth):
    """Split a panel a0..a1 by b0..b1 of a view again and again along its longer side, each cut at its own place, some
    panels left whole (D: kit_depth, kit_stop, kit_min_m; kit_chamfer the share given a small cut corner)."""
    w, h = a1 - a0, b1 - b0
    mn = D["kit_min_m"]
    if depth >= D["kit_depth"] or (w < mn * 2 and h < mn * 2) or (depth > 0 and rng.random() < D["kit_stop"]):
        if rng.random() < D.get("kit_chamfer", 0) and min(w, h) > 2.2:
            c = min(D.get("kit_chamfer_m", 0.6), 0.2 * min(w, h))
            ca, cb = (a0, b0) if rng.random() < 0.5 else (a1, b1)
            sa, sb = (1 if ca == a0 else -1), (1 if cb == b0 else -1)
            out[view].append(((ca + sa * c, cb), (ca, cb + sb * c)))
        return
    along_a = w > h * 1.2 or (h <= w * 1.2 and rng.random() < 0.5)
    if along_a and w >= mn * 2:
        m = a0 + w * rng.uniform(0.32, 0.68)
        out[view].append(((m, b0), (m, b1)))
        kit_split(rng, D, out, view, a0, m, b0, b1, depth + 1); kit_split(rng, D, out, view, m, a1, b0, b1, depth + 1)
    elif h >= mn * 2:
        m = b0 + h * rng.uniform(0.32, 0.68)
        out[view].append(((a0, m), (a1, m)))
        kit_split(rng, D, out, view, a0, a1, b0, m, depth + 1); kit_split(rng, D, out, view, a0, a1, m, b1, depth + 1)


def chine_slabs(lo, deg, gap):
    """Thin cutters along the hull's chines (every edge where its faces turn more than deg degrees), so the plating breaks
    where the form turns and each facet's plates catch the light on their own."""
    me = lo.data
    glow_idx = {i for i, m in enumerate(me.materials) if m and m.name.startswith("lo_glow")}
    bm = bmesh.new(); bm.from_mesh(me)
    out, lim = [], math.cos(math.radians(deg))
    for e in bm.edges:
        if len(e.link_faces) != 2 or any(f.material_index in glow_idx for f in e.link_faces):
            continue
        f0, f1 = e.link_faces
        if f0.normal.dot(f1.normal) > lim:
            continue
        a, b = e.verts[0].co, e.verts[1].co
        L = (b - a).length
        if L < 0.3:
            continue
        u = (b - a) / L
        w = (f0.normal + f1.normal).normalized()
        v = w.cross(u).normalized(); w = u.cross(v)
        M = Matrix.Identity(4)
        for i, vec in enumerate((u, v, w)):
            M[0][i], M[1][i], M[2][i] = vec.x, vec.y, vec.z
        mid = (a + b) / 2
        M[0][3], M[1][3], M[2][3] = mid.x, mid.y, mid.z
        out.append(place(bm_box(L + gap, gap, 3.0, at=(0, 0, -1.0)), M))
    bm.free()
    return out


def mirror_cuts(cuts):
    return {"plan": cuts["plan"] + [((-c[0][0], c[0][1]), (-c[1][0], c[1][1]), *c[2:]) for c in cuts["plan"]], "side": cuts["side"]}


def soften(o, width, segments=2, angle_deg=30):
    """Round an object's edges with a real bevel (owner, 2026-11-07: "very little hard angles. Chamfer/bevel avoid
    90degree in the extrustion of shapes and the shape of hull.plating"): every edge turning more than angle_deg
    degrees, width metres, clamped where the bevels would meet."""
    apply_mod(o, "BEVEL", width=width, segments=segments, limit_method="ANGLE", angle_limit=math.radians(angle_deg),
              use_clamp_overlap=True)


def softened_form(lo, width):
    """A copy of the low-poly hull with its chines rounded (D['soft']['chine_m']): the plating and the under layer are
    laid over it, so the plates bend round the hull's corners; the bake carries the rounding onto the faceted hull."""
    f = lo.copy(); f.data = lo.data.copy(); bpy.context.scene.collection.objects.link(f)
    soften(f, width, segments=3)
    return f


def build_armour(lo, D, S, rng, holes, mats, layer):
    """One layer of armour plates. layer 'primary': thick, cut by the ship's gap lines (most gaps narrow, some wide
    trenches), the data's exposed areas and the features' holes, and each plate then raised by its own amount so it
    casts its shadow on its neighbours. layer 'sub': thin, under the primary, cut by its own lines with wide gaps, so
    the trenches show a second layer of plating and, in its gaps, the machinery."""
    groups = shell_groups(lo)
    xs = [v.co.x for v in lo.data.vertices]; ys = [v.co.y for v in lo.data.vertices]; zs = [v.co.z for v in lo.data.vertices]
    style = {"hunt": armour_cuts, "plan": plan_cuts, "panels": panel_cuts}[D.get("cut_style", "hunt")]
    cuts = mirror_cuts(style(rng, D, (min(zs), max(zs)), max(xs), (min(ys), max(ys))))
    primary = layer == "primary"
    t = D["armour_t_m"] if primary else D["sub_t_m"]
    plates = []
    for g, faces in groups.items():
        if not faces:
            continue
        sh = shell_of(lo, faces, t, f"{layer}_{g}", mats["armour" if primary else "sub"])
        view = ("x", "z") if g in ("top", "bottom") else ("z", "y")
        bms = []
        for c in (cuts["plan"] if view == ("x", "z") else cuts["side"]):
            a, b = c[0], c[1]
            wide = rng.random() < (D["trench_share"] if primary else 0.35)
            gw = D["major_gap_m"] if (len(c) > 2 and c[2] == "major" and "major_gap_m" in D) else rng.uniform(*(D["trench_m"] if wide else D["gap_m"]))
            p = gap_slab(a, b, gw, view)
            if p:
                bms.append(p)
        if primary:
            for e in S.get("exposed", []):
                bms.append(view_prism(e["plan"], ("x", "z"), *e["y_m"]))
        for h in holes:
            bm = bmesh.new(); bm.from_mesh(h.data); bms.append(bm)
        if primary and D.get("chine_deg"):
            bms += chine_slabs(lo, D["chine_deg"], D.get("major_gap_m", D["gap_m"][1]))
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
    if D.get("soft"):   # every plate's edges rolled, its seams soft-shouldered
        w = D["soft"]["plate_m"] * (1.0 if primary else 0.5)
        for o in plates:
            soften(o, w)
    if primary:
        plates += [ov for o in list(plates) if (ov := overlay(o, D, mats)) is not None]
        if D.get("rivets"):   # bolt holes and slots indented into the plate (owner: "I think indents might be better")
            t0 = time.time()
            for o in plates:
                rv = rivets(o, D["rivets"])
                if rv is not None:
                    c = obj_from(rv, "bolts", mats["armour"])
                    boolean(o, c, "DIFFERENCE", "EXACT_NOSELF")
                    bpy.data.objects.remove(c)
            print(f"  bolt wells in {len(plates)} plates: {time.time() - t0:.0f} s", flush=True)
    return plates


def rivets(o, R):
    """Bolt holes and slots on a plate (owner, 2026-11-06: "tiny holes or slots for rivets and bolts", then "we dont need
    those old ww2 style rivets", "you can see these pill shaped holes here"): no rows. A pill-shaped hole lying along each
    corner of the plate's face set diagonally, pointing at the corner (R['inset_m'] in, R['pill_l_m'] long), and on an
    edge longer than R['slot_edge_m'] one standing across it at its middle, only on the plates the rid picks (R['share']).
    Built from the plate's own corners and edges, so a plate and its mirror carry the same."""
    r0 = o.data.color_attributes["rid"].data[0].color[0]
    if (r0 * 17.3) % 1.0 > R.get("share", 0.4):
        return None
    me = o.data
    nrm = sum((p.normal * p.area for p in me.polygons), Vector())
    if nrm.length == 0:
        return None
    nrm.normalize()
    bm = bmesh.new(); bm.from_mesh(me)
    top = {f for f in bm.faces if f.normal.dot(nrm) > 0.5}   # the plate's outer face (its inner face looks the other way,
    if not top:                                               # its rims across), however the plate curves
        bm.free(); return None
    out = []
    inset = R["inset_m"]

    def put(p, t, n, piece):
        M = Matrix.Identity(4)
        for i, vec in enumerate((t, n.cross(t), n)):
            M[0][i], M[1][i], M[2][i] = vec.x, vec.y, vec.z
        M[0][3], M[1][3], M[2][3] = p.x, p.y, p.z
        out.append(place(piece, M))
    for f in top:
        c = f.calc_center_median()
        bnd = [e for e in f.edges if not any(g in top for g in e.link_faces if g is not f)]
        for e in bnd:
            a, b = e.verts[0].co, e.verts[1].co
            L = (b - a).length
            if L < 0.5:
                continue
            t = (b - a) / L
            inn = f.normal.cross(t)
            if inn.dot(c - a) < 0:
                inn = -inn
            if L > R.get("slot_edge_m", 3.0) and (r0 * 7.7 + L) % 1.0 < R.get("slot_share", 0.5):
                sl = R.get("slot_l_m", 0.28)   # across the edge, standing in from it
                put((a + b) / 2 + inn * (inset + sl / 2), inn, f.normal, bm_prism(pill(sl, R["r_m"] * 1.4), -R.get("depth_m", 0.05), 0.6))
            for v in e.verts:   # a corner: where this boundary edge turns into the next by more than 30 degrees
                nxt = [g for g in bnd if g is not e and v in g.verts]
                if not nxt:
                    continue
                w = nxt[0].other_vert(v).co - v.co
                if w.length < 0.5 or abs(w.normalized().dot(t)) > 0.87 or e.index > nxt[0].index:
                    continue   # (each corner once: from the lower-numbered of its two edges)
                ww = w.normalized()
                d = (v.co - a).normalized() if (v.co - a).length > 1e-6 else (v.co - b).normalized()
                pl = R.get("pill_l_m", 0.34)   # a pill set diagonally into the corner, pointing at it
                bis = (ww - d).normalized()
                put(v.co + bis * (inset * 1.4 + pl / 2), bis, f.normal, bm_prism(pill(pl, R["r_m"] * 1.4), -R.get("depth_m", 0.05), 0.6))
    bm.free()
    return merge(out) if out else None


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


def in_exposed(q, S):
    """Whether a point lies in one of the data's exposed zones (a plan polygon and a y range: where the armour is opened)."""
    for e in S.get("exposed", []):
        y0, y1 = e["y_m"]
        if not (y0 <= q.y <= y1):
            continue
        P, inside_ = e["plan"], False
        j = len(P) - 1
        for i in range(len(P)):
            (xi, zi), (xj, zj) = P[i], P[j]
            if (zi > q.z) != (zj > q.z) and q.x < (xj - xi) * (q.z - zi) / (zj - zi + 1e-12) + xi:
                inside_ = not inside_
            j = i
        if inside_:
            return True
    return False


def greebles(lo, armour, D, rng, mats, holes=None, S=None):
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
            if D.get("greebles_exposed_only") and not in_exposed(q, S or {}):   # machinery only where the armour is opened,
                continue                                                         # never poking through a seam
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


# ----------------------------------------------------------------------------- the detail kit
# The owner, 2026-11-06, with close-up sheets of a clean hull: "vents, lights, escape hatches, missile tubes, bombe tobes",
# "warning markings too", "tiny holes or slots for rivets and bolts", "close up ship details", "more detail variety".
# Each piece is built in a feature frame (u along the ship, v across, w out of the hull, 0 at the low-poly skin; T the
# armour's thickness, so the armour's face is at about w = T) and returns its parts [(bmesh, kind, ...)] and its cutters.
def pill(l, w, seg=8):
    """A pill (a slot with round ends) l long along u, w wide, centred, counter-clockwise (u, v)."""
    r, a = w / 2, max(l / 2 - w / 2, 0.0)
    pts = [(a + r * math.cos(-math.pi / 2 + math.pi * k / seg), r * math.sin(-math.pi / 2 + math.pi * k / seg)) for k in range(seg + 1)]
    return pts + [(-a + r * math.cos(math.pi / 2 + math.pi * k / seg), r * math.sin(math.pi / 2 + math.pi * k / seg)) for k in range(seg + 1)]


def crect(w, h, c):
    """A w by h rectangle with corners cut by c, centred, counter-clockwise (u, v)."""
    a, b = w / 2, h / 2
    c = min(c, 0.45 * min(w, h))
    return [(a - c, -b), (a, -b + c), (a, b - c), (a - c, b), (-a + c, b), (-a, b - c), (-a, -b + c), (-a + c, -b)]


def ring_prisms(outer, inner, w0, w1):
    """A frame between two matching polygons, as one convex quad prism a side (so each is a clean solid)."""
    n = len(outer)
    return merge([bm_prism([outer[i], outer[(i + 1) % n], inner[(i + 1) % n], inner[i]], w0, w1) for i in range(n)])


def decal(kind, colour, M, du, dv, w, h):
    """A painted mark at (du, dv) in frame M, w along u by h along v (painted_bake.py paints DECALS)."""
    R3, o = M.to_3x3(), M.to_translation()
    u, v, n = (R3 @ Vector((1, 0, 0))).normalized(), (R3 @ Vector((0, 1, 0))).normalized(), (R3 @ Vector((0, 0, 1))).normalized()
    c = o + u * du + v * dv
    DECALS.append({"kind": kind, "colour": colour, "c": [round(x, 4) for x in c], "u": [round(x, 5) for x in u],
                   "v": [round(x, 5) for x in v], "n": [round(x, 5) for x in n], "w_m": w, "h_m": h})


def k_hatch(r, M, T, w, h, cutters):
    """An escape hatch or access panel, set into the plating (owner: "I think indents might be better"): a chamfered well
    cut through the armour, the hatch's own plate (its own shade) sunk in it with a groove all round, a recessed grip at
    one end, a warning frame and arrow painted beside it."""
    g = 0.07
    outer, inner = crect(w + 2 * g, h + 2 * g, 0.28), crect(w, h, 0.22)
    cutters.append(place(bm_prism(outer, -1.0, 2.0), M.copy()))
    sink = r.uniform(0.06, 0.1)
    parts = [(place(bm_prism(inner, T * 0.3, T - sink), M.copy()), "armour_hatch", r.random()),
             (place(bm_prism(outer, T * 0.2, T * 0.3), M.copy()), "vent")]
    parts.append((place(bm_prism(crect(0.32, h * 0.42, 0.06), T - sink - 0.04, T - sink + 0.01), M @ Matrix.Translation((w / 2 - 0.3, 0, 0))), "vent"))
    side = 1 if r.random() < 0.5 else -1
    decal("frame", "warn", M, 0, 0, w + 0.5, h + 0.5)
    decal("arrow", "warn", M, -w / 2 - 0.45, side * (h / 2 - 0.2), 0.32, 0.32)
    if r.random() < 0.5:
        decal("dashes", "ink", M, 0, -side * (h / 2 + 0.38), w * 0.6, 0.1)
    return parts


def k_vent(r, M, T, w, h, cutters):
    """A vent: a chamfered recess with a raised rim, a dark floor and slats across it."""
    rim, hole = crect(w + 0.24, h + 0.24, 0.2), crect(w, h, 0.14)
    cutters.append(place(bm_prism(hole, -1.0, 2.0), M.copy()))
    parts = [(place(ring_prisms(rim, hole, T - 0.06, T + 0.09), M.copy()), "armour_rim", 0.3 + r.random() * 0.4),
             (place(bm_prism(hole, T - 0.32, T - 0.24), M.copy()), "vent")]
    pitch = r.choice((0.14, 0.18, 0.22))
    across = w < h
    span, L = (h, w) if not across else (w, h)
    k = -L / 2 + pitch
    while k < L / 2 - pitch * 0.5:
        at = (k, 0, T - 0.26) if not across else (0, k, T - 0.26)
        sz = (pitch * 0.45, span * 0.94, 0.2) if not across else (span * 0.94, pitch * 0.45, 0.2)
        parts.append((place(bm_box(*sz, at=at), M.copy()), "slat"))
        k += pitch
    if r.random() < 0.6:
        decal("chevrons", "hazard", M, 0, (h / 2 + 0.3) * (1 if r.random() < 0.5 else -1), w, 0.22)
    return parts


def k_light(r, M, T, w, h, cutters, colour):
    """A lit slot: a recess with a lamp bar in it (blue, amber or red), sometimes two or three bars side by side."""
    n = r.choice((1, 1, 2, 3))
    hole = crect(w, h, 0.06)
    cutters.append(place(bm_prism(hole, -1.0, 2.0), M.copy()))
    parts = [(place(bm_prism(hole, T - 0.2, T - 0.14), M.copy()), "vent")]
    bw = (w - 0.06 * (n + 1)) / n
    for i in range(n):
        uu = -w / 2 + 0.06 + bw / 2 + i * (bw + 0.06)
        parts.append((place(bm_box(bw, h - 0.1, 0.1, at=(uu, 0, T - 0.15)), M.copy()), "lamp:" + colour))
    return parts


def k_tubes(r, M, T, w, h, cutters, nu, nv, rad, accent=False):
    """A launcher: a raised block with a grid of tube mouths (missile tubes nu by nv; a bomb tube one or two large),
    each a round hole with a dark floor, a warning stripe painted on the block's lip."""
    top = T + 0.18 + r.uniform(0, 0.08)
    blk = bm_prism(crect(w, h, min(w, h) * 0.12), -0.1, top)
    holes = []
    for i in range(nu):
        for j in range(nv):
            uu = -w / 2 + w * (i + 0.5) / nu; vv = -h / 2 + h * (j + 0.5) / nv
            holes.append(bm_cyl(rad, 1.0, 24, at=(uu, vv, top - 0.35)))
    parts = [(place(blk, M.copy()), "armour_holed" if not accent else "accent_holed", r.random(), place(merge(holes), M.copy()))]
    floors = [bm_cyl(rad * 1.02, 0.05, 24, at=(-w / 2 + w * (i + 0.5) / nu, -h / 2 + h * (j + 0.5) / nv, top - 0.3))
              for i in range(nu) for j in range(nv)]
    parts.append((place(merge(floors), M.copy()), "vent"))
    decal("hazard", "hazard", M, 0, -h / 2 - 0.22, w, 0.26)
    return parts


def k_module(r, M, T, w, h, cutters):
    """An accent module: a raised block in the design's accent colour, stepped once, with a small light and a mark."""
    top = T + 0.14 + r.uniform(0, 0.12)
    parts = [(place(bm_prism(crect(w, h, 0.18), -0.1, top), M.copy()), "accent")]
    parts.append((place(bm_prism(crect(w * 0.62, h * 0.7, 0.12), top - 0.02, top + 0.08), M @ Matrix.Translation((-w * 0.12, 0, 0))), "accent"))
    if r.random() < 0.5:
        parts.append((place(bm_box(0.12, h * 0.4, 0.06, at=(w * 0.38, 0, top - 0.02)), M.copy()), "lamp:" + r.choice(("blue", "amber"))))
    decal("block", "ink", M, w * 0.3, h / 2 - 0.18, 0.4, 0.12)
    return parts


def k_port(r, M, T, w, h, cutters):
    """A small port: a round or square hole a hand or two across, dark, now and then lit."""
    rnd = r.random() < 0.6
    poly = pill(w, w, 10) if rnd else crect(w, w, w * 0.2)
    cutters.append(place(bm_prism(poly, -1.0, 2.0), M.copy()))
    parts = [(place(bm_prism(poly, T - 0.2, T - 0.12), M.copy()), "vent")]
    if r.random() < 0.35:
        parts.append((place(bm_prism([(x * 0.6, y * 0.6) for x, y in poly], T - 0.13, T - 0.08), M.copy()), "lamp:" + r.choice(LIGHTS_OF)))
    return parts


def k_groove(r, M, T, w, h, cutters):
    """A thin indented line along the plating, ending in round ends, sometimes with a short second line beside it."""
    cutters.append(place(bm_prism(pill(w, 0.07), T - 0.05, 2.0), M.copy()))
    if r.random() < 0.4:
        cutters.append(place(bm_prism(pill(w * 0.35, 0.07), T - 0.05, 2.0), M @ Matrix.Translation((w * 0.2, 0.22, 0))))
    return []


def k_tag(r, M, T, w, h, cutters):
    """A small plate laid on the plating (a placard, a cover), its own shade, with a mark on it."""
    parts = [(place(bm_prism(crect(w, h, 0.05), T - 0.05, T + 0.11), M.copy()), "armour_tag", r.random())]
    decal(r.choice(("block", "box", "dashes")), r.choice(("ink", "warn", "ink")), M, 0, 0, w * 0.6, h * 0.4)
    return parts


def k_rcs(r, M, T, w, h, cutters):
    """A thruster quad: a small block with four nozzles in a square."""
    return k_tubes(r, M, T, 0.9, 0.9, cutters, 2, 2, 0.13)


KIT = {"hatch": k_hatch, "vent": k_vent, "rcs": k_rcs, "module": k_module, "port": k_port, "groove": k_groove, "tag": k_tag,
       "light": lambda r, M, T, w, h, c: k_light(r, M, T, w, h, c, r.choice(LIGHTS_OF)),
       "missiles": lambda r, M, T, w, h, c: k_tubes(r, M, T, w, h, c, max(2, int(w / 0.7)), max(1, int(h / 0.7)), 0.22),
       "bombs": lambda r, M, T, w, h, c: k_tubes(r, M, T, w, h, c, max(1, int(w / 1.6)), 1, min(0.55, h * 0.36), accent=True)}
LIGHTS_OF = ["blue"]
SIZES = {"port": ((0.22, 0.42), (0.22, 0.42)), "groove": ((1.2, 3.2), (0.1, 0.1)), "tag": ((0.5, 1.1), (0.3, 0.6)),
         "hatch": ((1.2, 2.6), (0.9, 1.6)), "vent": ((1.0, 3.2), (0.4, 1.1)), "rcs": ((0.9, 0.9), (0.9, 0.9)),
         "module": ((1.2, 2.8), (0.8, 1.6)), "light": ((0.6, 1.4), (0.2, 0.34)), "missiles": ((1.4, 2.8), (0.7, 1.4)),
         "bombs": ((1.6, 3.4), (1.0, 1.5))}


def scatter_kit(lo, S, D, surf, rng, taken):
    """The kit laid over the hull (D['kit']: per_100m2, weights by kind, gap_m): port side and centreline only, each piece
    mirrored, kept off bends, off other pieces, out of the opened zones and clear of the windows and bays (taken: centres
    and radii already used). Every piece seeded by where it is. Returns [(parts, loc)], cutters."""
    K = D["kit"]
    kinds = list(K["weights"]); wts = [K["weights"][k] for k in kinds]
    LIGHTS_OF[:] = K.get("lights", ["blue"])
    T = D["armour_t_m"]
    me = lo.data
    glow_idx = {i for i, m in enumerate(me.materials) if m and m.name.startswith("lo_glow")}
    feats, cutters = [], []
    gap = K.get("gap_m", 0.8)
    for p in sorted(me.polygons, key=lambda p: (round(p.center.z, 2), round(p.center.y, 2), round(p.center.x, 2))):
        if p.material_index in glow_idx or p.center.x < -0.01:
            continue
        nwant = p.area * K["per_100m2"] / 100.0
        k = int(nwant) + (1 if rng.random() < nwant % 1 else 0)
        vs = [me.vertices[i].co for i in p.vertices]
        for _ in range(k):
            a, b = rng.random(), rng.random()
            if a + b > 1:
                a, b = 1 - a, 1 - b
            q = vs[0] + (vs[1] - vs[0]) * a + (vs[2] - vs[0]) * b
            kind = rng.choices(kinds, wts)[0]
            (u0, u1), (v0, v1) = SIZES[kind]
            w, h = round(rng.uniform(u0, u1), 2), round(rng.uniform(v0, v1), 2)
            rad = math.hypot(w, h) / 2 + 0.35
            centre = abs(p.normal.y) > 0.8 and abs(q.x) < rad
            if centre:
                q = Vector((0.0, q.y, q.z))
                hh = surf.bvh.ray_cast(q + p.normal * 3, -p.normal, 6)
                if hh[0] is None:
                    continue
                q = hh[0]
            elif q.x < rad:
                continue
            if in_exposed(q, S) or any((q - c).length < rad + rc + gap for c, rc in taken):
                continue
            M = frame_at(q, p.normal, False)
            if not fits(surf, M, w, h, 0.25 + D.get("soft", {}).get("chine_m", 0.0)):   # clear of a rounded chine too
                continue
            taken.append((q.copy(), rad))
            if not centre:
                taken.append((Vector((-q.x, q.y, q.z)), rad))
            for m in ((False,) if centre else (False, True)):
                r = random.Random(int(abs(q.x) * 100) * 7919 + int(q.y * 100) * 104729 + int(q.z * 100) + sum(map(ord, kind)) * 31)
                cut = []
                parts = KIT[kind](r, frame_at(q, p.normal, m), T, w, h, cut)
                feats.append((parts, q))
                cutters.extend(cut)
    return feats, cutters


def well_modules(lo, S, D):
    """The machinery in the opened zones laid out in order (owner: "not messy"; the reference sheets' recessed bays): a
    grid of cells D['well_cell_m'] (along, across) over every face in a zone, one module a cell, its kind by its place in
    the grid, so a bay reads as rows of built machinery: blocks, pipe runs, slatted boxes, tanks, lit panels."""
    cu, cv = D["well_cell_m"]
    me = lo.data
    T = D["armour_t_m"]
    out = []
    for p in me.polygons:
        c = p.center
        if c.x < -0.01 or not in_exposed(c, S):
            continue
        M0 = frame_at(c, p.normal, False)
        Mi = M0.inverted()
        loc = [(Mi @ me.vertices[i].co).to_2d() for i in p.vertices]
        us = [l.x for l in loc]; vs_ = [l.y for l in loc]
        iu = math.floor(min(us) / cu)
        while iu * cu < max(us):
            iv = math.floor(min(vs_) / cv)
            while iv * cv < max(vs_):
                uu, vv = (iu + 0.5) * cu, (iv + 0.5) * cv
                q = M0 @ Vector((uu, vv, 0))
                inside_ = all(in_exposed(M0 @ Vector((uu + du, vv + dv, 0)), S) for du in (-cu * 0.45, cu * 0.45) for dv in (-cv * 0.45, cv * 0.45))
                if inside_ and (q.x > 0.3 or abs(q.x) <= 0.3):
                    sel = (int(round(q.z / cu)) * 3 + int(round(abs(q.x) / cv)) * 5 + int(round(q.y))) % 6
                    for m in ((False,) if abs(q.x) <= 0.3 else (False, True)):
                        out.append((well_module(sel, frame_at(q, p.normal, m), cu * 0.88, cv * 0.86, T), q))
                iv += 1
            iu += 1
    return out


def well_module(sel, M, w, h, T):
    top = T * 0.8
    if sel == 0:     # a machinery block, stepped
        return [(place(merge([bm_box(w, h, top * 0.6), bm_box(w * 0.6, h * 0.7, top * 0.9, at=(-w * 0.1, 0, 0))]), M), "greeble")]
    if sel == 1:     # a pipe run along the bay
        return [(place(merge([bm_cyl(h * 0.13, w * 1.02, 16, at=(0, (j - 1) * h * 0.3, h * 0.15), axis="u") for j in range(3)]), M), "greeble")]
    if sel == 2:     # a slatted box
        return [(place(bm_box(w, h, top * 0.5), M), "greeble")] + [
            (place(bm_box(0.08, h * 0.9, top * 0.18, at=(-w / 2 + (k + 0.5) * w / 7, 0, top * 0.5)), M.copy()), "slat") for k in range(7)]
    if sel == 3:     # a tank, lying along the bay
        return [(place(bm_cyl(min(h * 0.4, top * 0.9), w * 0.9, 24, at=(0, 0, min(h * 0.4, top * 0.9) * 0.9), axis="u"), M), "greeble")]
    if sel == 4:     # a lit panel
        return [(place(bm_box(w, h, top * 0.4), M), "greeble"), (place(bm_box(w * 0.5, 0.12, 0.08, at=(0, 0, top * 0.4)), M.copy()), "lamp:" + LIGHTS_OF[0])]
    return [(place(bm_box(w, h, top * 0.7), M), "accent")]   # an accent block


# ----------------------------------------------------------------------------- the whole layer
def build(lo, S, D, seed):
    """Build the hull detail for one ship over its low-poly hull `lo` (S: the ship's entry in hull_detail.json, D: the
    defaults). Returns the high-poly objects to bake from."""
    D = {**D, **S.get("detail", {})}   # a design's own plating over the faction's defaults
    LAMP_RGB.update({k: tuple(v) for k, v in D.get("lamp_rgb", {}).items()})
    BEVEL_M.update(D.get("bevel_m", {}))
    WEAR_BUMP.clear(); WEAR_BUMP.update(D.get("wear_bump", {}))
    rng = random.Random(seed)
    PANES.clear(); DECALS.clear()
    mats = {k: kind_material(k) for k in ("under", "greeble", "sub", "armour", "accent", "dome", "slat", "vent")}
    lamps = {c: kind_material("lamp", c) for c in LAMP_RGB}
    surf = Surface(lo)
    SOFT = D.get("soft")
    form = softened_form(lo, SOFT["chine_m"]) if SOFT else lo   # the shape the plating is laid over
    under = form.copy(); under.data = form.data.copy(); bpy.context.scene.collection.objects.link(under)
    strip = [form.data.materials[p.material_index].name.startswith("lo_glow") for p in form.data.polygons]
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
        for m in sides_of(F, loc):
            cut = []
            feats.append((fn(F, frame_at(loc, nor, m), cut), loc))
            holes.extend(obj_from(c, "hole", mats["armour"]) for c in cut)

    for F in S.get("domes", []):
        at_both(F, lambda F, M, cut: dome(F, M, mats, cut))
    for F in S.get("fans", []):
        at_both(F, fan)
    for F in S.get("louvres", []):
        at_both(F, louvre)
    STORY.clear()
    for kind, key, size in (("heat", "fans", "r_m"), ("heat", "louvres", "span_m"), ("bay", "bays", "w_m")):
        for F in S.get(key, []):
            h = surf.hit(F["face"], F["at"])
            if h:
                for m in sides_of(F, h[0]):
                    sx = -1 if m else 1
                    STORY.append({"kind": kind, "pos": [h[0].x * sx, h[0].y, h[0].z], "n": [h[1].x * sx, h[1].y, h[1].z],
                                  "size_m": F[size], "h_m": F.get("h_m", F.get(size))})
    for F in S.get("marks", []):     # muzzles and drives: where the guns and engines leave their scorch and soot
        h = surf.hit(F["face"], F["at"])
        if h:
            for m in sides_of(F, h[0]):
                sx = -1 if m else 1
                STORY.append({"kind": F["kind"], "pos": [h[0].x * sx, h[0].y, h[0].z], "n": [h[1].x * sx, h[1].y, h[1].z], "size_m": F["size_m"]})
    for F in S.get("lamps", []):
        at_both(F, lambda F, M, cut: lamp(F, M))
    PF = D["pane_frame_m"]
    for F in S.get("bays", []):
        at_both(F, lambda F, M, cut: bay(F, M, cut, surf, PF["bay"]))
    for F in S.get("windows", []):
        rows = [(l, n_, lt, k, None) for l, n_, lt, k in window_rows(F, surf)] if "rows_y_m" in F else window_rows_plan(F, surf)
        for loc, nor, lit_, kind, only in rows:
            if not fits(surf, frame_at(loc, nor), 1.1, 0.6, PF["window"]):
                continue   # it would cross a bend
            for m in ((False, True) if only is None else (only,)):
                cut = []
                feats.append((window(lit_, frame_at(loc, nor, m), cut, PF["window"], kind), loc))
                holes.extend(obj_from(c, "hole", mats["armour"]) for c in cut)
    lights(lo, S, surf, mats, lamps, feats, D)
    for F in S.get("decals", []):   # the data's own marks: names, numbers, warning bands
        h = surf.hit(F["face"], F["at"])
        if h:
            for m in sides_of(F, h[0]):
                M = frame_at(h[0], h[1], m)
                if F.get("turn"):
                    M = M @ Matrix.Rotation(math.radians(F["turn"]), 4, "Z")
                decal(F["kind"], F.get("colour", "ink"), M, 0, 0, F["w_m"], F["h_m"])
                if "text" in F:
                    DECALS[-1]["text"] = F["text"]
                DECALS[-1]["mirror"] = bool(m)
    if D.get("kit"):
        taken = [(loc, 1.2) for _, loc in feats]
        for P_ in PANES:
            taken.append((Vector(P_["c"]), math.hypot(P_["w_m"], P_["h_m"]) / 2 + 0.6))
        kf, kc = scatter_kit(lo, S, D, surf, rng, taken)
        feats += kf
        holes.extend(obj_from(c, "hole", mats["armour"]) for c in kc)
    t0 = time.time()
    armour = build_armour(form, D, S, rng, holes, mats, "primary")
    print(f"  primary armour: {len(armour)} plates, {time.time() - t0:.0f} s", flush=True)
    sub = build_armour(form, D, S, random.Random(seed * 31 + 7), holes, mats, "sub")
    hole_bvh = bvh_of(holes)
    for h in holes:
        bpy.data.objects.remove(h)
    high += armour + sub
    for parts, loc in feats:
        for i, part in enumerate(parts):
            bm, kind = part[0], part[1]
            if kind.startswith("armour_"):
                o = obj_from(bm, kind, mats["armour"], rid_of(loc, salt=part[2]))
                if SOFT and kind != "armour_collar" and kind != "armour_strap":
                    soften(o, SOFT["plate_m"] * 0.6)
                if kind == "armour_holed":
                    inner = obj_from(part[3], "holes_in", mats["armour"]); boolean(o, inner, "DIFFERENCE"); bpy.data.objects.remove(inner)
                if kind == "armour_collar":
                    inner = obj_from(part[3], "collar_in", mats["armour"]); boolean(o, inner, "DIFFERENCE"); bpy.data.objects.remove(inner)
                elif kind == "armour_strap":
                    slab = obj_from(part[3], "strap_slab", mats["armour"]); boolean(o, slab, "INTERSECT"); bpy.data.objects.remove(slab)
                high.append(o)
            elif kind == "accent_holed":
                o = obj_from(bm, kind, mats["accent"], rid_of(loc, salt=part[2]))
                inner = obj_from(part[3], "holes_in", mats["accent"]); boolean(o, inner, "DIFFERENCE"); bpy.data.objects.remove(inner)
                if SOFT:
                    soften(o, SOFT["plate_m"] * 0.5)
                high.append(o)
            elif kind.startswith("lamp:"):
                high.append(obj_from(bm, "lamp", lamps[kind[5:]], 0.5))
            else:
                o = obj_from(bm, kind, mats[kind], rid_of(loc, salt=i * 0.37))
                if SOFT and kind == "accent":
                    soften(o, SOFT["plate_m"] * 0.5)
                high.append(o)
    if D.get("well_cell_m"):
        for parts, loc in well_modules(lo, S, D):
            for i, part in enumerate(parts):
                bm, kind = part[0], part[1]
                mat = lamps[kind[5:]] if kind.startswith("lamp:") else mats[kind]
                high.append(obj_from(bm, kind, mat, rid_of(loc, salt=1.7 + i * 0.37)))
    gb = [] if D.get("well_cell_m") else greebles(lo, armour + sub, D, rng, mats, hole_bvh, S)
    if gb:
        high.append(obj_from(merge(gb), "greebles", mats["greeble"], 0.0))
        set_greeble_rids(high[-1])
    if form is not lo:
        bpy.data.objects.remove(form)
    return within_hull(lo, high)


def within_hull(lo, high, margin=3.0):
    """Every piece must lie within the hull's own bounds (plus margin metres): a piece reaching past them is a bug (a
    cutter left behind, a frame gone wrong) that would spoil the bake. Such a piece is named in the log and left out."""
    vs = [v.co for v in lo.data.vertices]
    lo_ = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs))) - Vector((margin,) * 3)
    hi_ = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs))) + Vector((margin,) * 3)
    keep = []
    for o in high:
        bb = [o.matrix_world @ Vector(c) for c in o.bound_box]
        if all(lo_.x <= p.x <= hi_.x and lo_.y <= p.y <= hi_.y and lo_.z <= p.z <= hi_.z for p in bb):
            keep.append(o)
        else:
            size = Vector((max(p.x for p in bb) - min(p.x for p in bb), max(p.y for p in bb) - min(p.y for p in bb), max(p.z for p in bb) - min(p.z for p in bb)))
            print(f"hull_detail: BUG: piece {o.name} ({len(o.data.polygons)} faces, {size.x:.1f} x {size.y:.1f} x {size.z:.1f} m) reaches past the hull; left out", flush=True)
            bpy.data.objects.remove(o)
    return keep


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
