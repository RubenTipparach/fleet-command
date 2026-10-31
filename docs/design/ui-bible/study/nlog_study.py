"""A lighting study after the Nucleo NLog Winamp skin, for the owner (2026-10-30: "this skin comprises of two layers,
the top metal layer, and the back "display" layer that looks like it was indirectly lit from the top like a dashboard
in an old car. entertain me a little and try and recrewate the nucleo nlog skin in blender").

Not committed: it studies someone else's artwork (CLAUDE.md section 10). No names, logos or text from the skin.
Two layers: a satin metal faceplate with windows cut through it, and behind it a cream display layer lit only by hidden
lamps tucked under each window's top lip, so light falls off from the top edge down, as in an old dashboard.

  /opt/blender/blender --background --factory-startup --python nlog_study.py -- <out.png> [samples]
"""
import json
import math
import os
import sys

import bpy
import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, "/home/user/fleet-command/tools/blender")
import panel_kit  # noqa: E402
import ui_render  # noqa: E402
from ui_shapes import NodeKit, Shaper, circle, rrect  # noqa: E402
import build_ui_kelvane as kv  # noqa: E402  (the Kelvane fittings: domes, knobs, toggles, screws)

W, H = 550, 700
MM = ui_render.MM
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else "/tmp/nlog_study.png"
SAMPLES = int(argv[1]) if len(argv) > 1 else 48
LIP_TILT = float(os.environ.get("LIP_TILT", 90))  # spot lamp tilt, degrees from the film's normal toward screen-down
LIP_CONE = float(os.environ.get("LIP_CONE", 180))  # the lamps' spread, degrees
LIP_LEN = float(os.environ.get("LIP_LEN", 10))  # the lamps' length, px
LIP_W = float(os.environ.get("LIP_W", 20))  # the lamps' depth, mm: wider is softer
SPILL = float(os.environ.get("SPILL", 0.2))  # the lamps' power against what they had as small spots: a 180 degree line source lights far more of the film
MAIN = float(os.environ.get("MAIN", 0.8))  # the main light's strength on the display layer
MAIN_SOFT = float(os.environ.get("MAIN_SOFT", 60))  # its angular size, degrees: bigger is a softer shadow
FILM_GLOW = float(os.environ.get("FILM_GLOW", 0.78))  # the film's own backlight; with the soft main light on it, 0.78 keeps the approved film colour (#eacf9f)
LAMPS = 0.25  # the lip lamps' scale, separate from the film, so softening the lamps keeps the film's colour
DZ = -12.0  # the display layer's surface: 12 mm behind the faceplate, room for the lip lamps to spread


def satin(name):
    k = NodeKit(name)
    w = k.tex("ShaderNodeTexWave", 900.0, **{"Distortion": 1.5, "Detail": 2.0})
    k.set(Base_Color="#bdb8ae", Metallic=0.75, Roughness=0.22, Coat_Weight=0.3, Coat_Roughness=0.1)
    k.bump(w.outputs["Fac"], 0.04)
    return k.mat


def display(name, es=0.85, rim=True):
    """The back layer: a translucent cream film with a small, flat, even backlight behind it. Where the faceplate's rim
    overhangs it (ambient occlusion within 7 mm) the film and its glow fall into a contact shadow, so it sits in a well."""
    k = NodeKit(name)
    ao = k.N.new("ShaderNodeAmbientOcclusion")
    ao.samples, ao.only_local = 16, False
    ao.inputs["Distance"].default_value = 0.007
    occ = k.math("POWER", ao.outputs["AO"], 2.2) if rim else 1.0
    k.set(Base_Color=k.ramp(occ, "#85765e", "#f4d9a9") if rim else "#f4d9a9", Roughness=0.6,
          Emission_Color="#f4d9a9", Emission_Strength=k.math("MULTIPLY", occ, es * FILM_GLOW) if rim else es * FILM_GLOW)
    return k.mat


def streak(name):
    """The EQ slot's light: a thin, even gold streak in an otherwise dark slot."""
    k = NodeKit(name)
    k.set(Base_Color="#2e271c", Roughness=0.5, Emission_Color="#e8b448", Emission_Strength=1.2 * FILM_GLOW)
    return k.mat

def printed(name, col):
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=0.55)
    return k.mat


def film_ink(name, col, es=0.9):
    """Ink printed on the backlit film: the light comes through it, so it glows faintly in its own warm tan
    (sampled from the reference) instead of reading as black paint."""
    k = NodeKit(name)
    # ink absorbs: its surface reflects only 35% of its colour, so a lamp right above it cannot wash it out
    dim = "#%02x%02x%02x" % tuple(int(int(col[i:i + 2], 16) * 0.35) for i in (1, 3, 5))
    k.set(Base_Color=dim, Roughness=0.6, Emission_Color=col, Emission_Strength=es * FILM_GLOW)
    return k.mat


def lip_light(x, y, power, depth=DZ + 8.0):
    """A short line of light, LIP_LEN px long, tucked in the well under a window's top lip at (x, y) px, 8 mm off the
    film and turned LIP_TILT degrees down the screen; it emits over a full 180 degrees, so its light spills down the
    surface in a wide soft fan from a short bar rather than from a point."""
    ld = bpy.data.lights.new("lip", "AREA")
    ld.shape, ld.size, ld.size_y = "RECTANGLE", LIP_LEN * MM, LIP_W * MM  # a broad soft source: diffused light
    ld.energy, ld.color, ld.spread = power * SPILL * LAMPS, (1.0, 0.88, 0.6), math.radians(LIP_CONE)
    ob = bpy.data.objects.new("lip", ld)
    ob.location = ((x - W / 2) * MM, (H / 2 - y - 1.0) * MM, depth * MM)  # just inside the window, in the well
    ob.rotation_mode = "QUATERNION"
    t = math.radians(LIP_TILT)  # 0 deg: straight into the film; 90 deg: straight down the screen, along the film
    ob.rotation_quaternion = Vector((0.0, -math.sin(t), -math.cos(t))).to_track_quat("-Z", "Y")
    ob.visible_camera = False
    bpy.context.scene.collection.objects.link(ob)


# ----------------------------------------------------------------------------- hard surface: primitives, cutters, bevels
# Parts are bmesh primitives; detail is carved by Exact boolean cutters (hidden from the render), then a bevel modifier
# rounds every edge sharper than 30 degrees so the light catches it (the blender-hard-surface way).

def _ob(bm, mat, name="hs"):
    bm.normal_update()
    for f in bm.faces:
        f.smooth = abs(f.normal.z) < 0.7
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    if mat is not None:
        me.materials.append(mat)
    ob = bpy.data.objects.new(name, me)
    K.coll.objects.link(ob)
    return ob


def cyl_bm(bm, x, y, r, z0, z1, seg=72):
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg, radius1=r * MM, radius2=r * MM,
                          depth=(z1 - z0) * MM, matrix=Matrix.Translation(K.P(x, y, (z0 + z1) / 2)))
    return bm


def box_bm(bm, cx, cy, w, h, z0, z1, rot=0.0):
    m = Matrix.Translation(K.P(cx, cy, (z0 + z1) / 2)) @ Matrix.Rotation(-rot, 4, "Z") @ Matrix.Diagonal((w * MM, h * MM, (z1 - z0) * MM, 1.0))
    bmesh.ops.create_cube(bm, size=1.0, matrix=m)
    return bm


def torus_bm(bm, x, y, R, rm, z, seg=72, mseg=10):
    c = Vector(K.P(x, y, z))
    rows = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        rows.append([bm.verts.new(c + Vector(((R + rm * math.cos(b)) * math.cos(a) * MM, (R + rm * math.cos(b)) * math.sin(a) * MM, rm * math.sin(b) * MM)))
                     for b in (2 * math.pi * k / mseg for k in range(mseg))])
    for i in range(seg):
        for k in range(mseg):
            bm.faces.new((rows[i][k], rows[(i + 1) % seg][k], rows[(i + 1) % seg][(k + 1) % mseg], rows[i][(k + 1) % mseg]))
    return bm


def cutter(bm):
    ob = _ob(bm, None, "cutter")
    ob.hide_render, ob.display_type = True, "WIRE"
    return ob


def cut(ob, bm):
    m = ob.modifiers.new("cut", "BOOLEAN")
    m.operation, m.solver, m.object = "DIFFERENCE", "EXACT", cutter(bm)
    m.use_hole_tolerant = True


def bevel(ob, w, seg=3, angle=30):
    m = ob.modifiers.new("bevel", "BEVEL")
    m.width, m.segments, m.limit_method, m.angle_limit, m.use_clamp_overlap = w * MM, seg, "ANGLE", math.radians(angle), True


def dial(K, x, y, r, z, pilot=False, knurl=False, socket=True):
    """A turned metal push dial in a dark socket: a dished face cut by a sphere, three concentric grooves cut by
    torus rings, knurling cut round the rim when asked, every edge bevelled."""
    if socket:
        sock = _ob(cyl_bm(bmesh.new(), x, y, r + 3.0, z - 1.0, z + 0.8), groove, "socket")
        cut(sock, cyl_bm(bmesh.new(), x, y, r + 0.8, z - 0.2, z + 3.0))
        bevel(sock, 0.4)
    if pilot:
        K.slab([circle(x, y, r + 2.6), circle(x, y, r + 1.2)], z + 0.7, z + 1.2, kv.M(K, "pilot_on"), bevel=0.2)
    top, dish = z + 3.6, 0.9
    body = _ob(cyl_bm(bmesh.new(), x, y, r, z - 0.6, top), knobmetal, "dial")
    R = 3.2 * r
    sp = bmesh.new()
    bmesh.ops.create_uvsphere(sp, u_segments=96, v_segments=48, radius=R * MM, matrix=Matrix.Translation(K.P(x, y, top - dish + R)))
    cut(body, sp)
    rings = bmesh.new()
    for f in (0.34, 0.56, 0.78):
        rho = f * r
        torus_bm(rings, x, y, rho, 0.5, top - dish + (R - math.sqrt(R * R - rho * rho)))
    cut(body, rings)
    if knurl:
        kn = bmesh.new()
        for i in range(36):
            a = 2 * math.pi * i / 36
            box_bm(kn, x + math.cos(a) * r, y + math.sin(a) * r, 2.6, 1.2, z + 0.4, top + 1.0, rot=a)
        cut(body, kn)
    bevel(body, 0.35)


def slider_cap(x, y, z):
    """A slider cap: a bevelled metal block with a groove cut across its middle."""
    cap = _ob(box_bm(bmesh.new(), x, y, 20.0, 10.0, z, z + 5.0), knobmetal, "cap")
    cut(cap, box_bm(bmesh.new(), x, y, 22.0, 1.1, z + 4.2, z + 6.0))
    bevel(cap, 1.0, 4)


def _area(pts):
    return abs(sum(pts[i - 1][0] * p[1] - p[0] * pts[i - 1][1] for i, p in enumerate(pts))) / 2


def offset_poly(pts, d):
    """Move a closed outline d px inward (negative: outward), each point along its mitred normal."""
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
    metal below flows up into it as one piece."""
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
    o = RR(rrect(x0, y0, x1, y1, r))
    o.rect = (x0, y0, x1, y1, r)
    return o


def plate(outlines, z0, z1, m, edge):
    """A pillowed metal plate: every edge, outside and round every opening, rolls over in a full round of radius
    edge mm, and the plate keeps exactly the footprint drawn (Blender's curve bevel grows a shape by its radius, so
    the outside is drawn that much smaller and the openings that much larger first). Turned into a welded mesh so
    cutters can carve it."""
    SHAPES.append(dict(outer=[list(p) for p in outlines[0]], holes=[[list(p) for p in o] for o in outlines[1:]], edge=edge, top=z1))
    offset = []
    for i, o in enumerate(outlines):
        d = edge if i == 0 else -edge
        if hasattr(o, "rect"):
            x0, y0, x1, y1, r = o.rect
            offset.append(rrect(x0 + d, y0 + d, x1 - d, y1 - d, max(0.6, r - d), seg=16))
        else:
            offset.append(offset_poly(o, d))
    cu = K.slab(offset, min(z0, z1 - 2 * edge - 0.4), z1, m, bevel=edge)
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
    K.coll.objects.link(ob)
    bpy.data.objects.remove(cu)
    return ob


def finish(ob, w, seg=6, angle=20):
    """Bake the cutters into the plate, dissolve the flat areas they split back into clean faces (so no stray
    sliver edge is left for the light to catch), then bevel the edges the cuts made."""
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
    bevel(ob, w, seg, angle)


def dimple(ob, x, y, rx, ry, top, depth, rz=7.0):
    """Press a soft dimple into a plate: an ellipsoid cutter sunk depth mm below its top face."""
    DIMPLES.append((x, y, rx, ry))
    bm = bmesh.new()
    m = Matrix.Translation(K.P(x, y, top + rz - depth)) @ Matrix.Diagonal((rx * MM, ry * MM, rz * MM, 1.0))
    bmesh.ops.create_uvsphere(bm, u_segments=72, v_segments=36, radius=1.0, matrix=m)
    cut(ob, bm)


def bezel(win, grow=1.5):
    """A thin brass frame round a display window, following its outline, just under the plate's rolled lip: it starts
    inside the metal wall and shows a thin gold edge in the opening."""
    K.slab([offset_poly(win, -0.8), offset_poly(win, grow)], PT - ROLL - 1.8, PT - ROLL + 0.2, brass, bevel=0.6)
    # the well's wall, from the plate down past the film, just outside the opening: what the film's rim shadow sees
    K.slab([offset_poly(win, -4.0), offset_poly(win, -0.3)], DZ - 1.0, 0.5, groove, bevel=0.2)

def band(win, power):
    """A thin strip lamp along a window's whole top edge, under the lip: the glowing band the spots sit in."""
    x0, x1, y = min(p[0] for p in win) + 6, max(p[0] for p in win) - 6, min(p[1] for p in win)
    ld = bpy.data.lights.new("lip", "AREA")
    ld.shape, ld.size, ld.size_y, ld.energy, ld.color = "RECTANGLE", (x1 - x0) * MM, 1.0 * MM, power * LAMPS, (1.0, 0.8, 0.38)
    ob = bpy.data.objects.new("lip", ld)
    ob.location = (((x0 + x1) / 2 - W / 2) * MM, (H / 2 - y + 1.5) * MM, (DZ + 2.0) * MM)
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = Vector((0.0, -1.0, -1.0)).normalized().to_track_quat("-Z", "Y")
    ob.visible_camera = False
    bpy.context.scene.collection.objects.link(ob)


def moulding(sec, inset=10.0):
    """A round bead running round a plate just inside its rolled edge, for plates with no window to frame."""
    x0, y0 = min(p[0] for p in sec), min(p[1] for p in sec)
    x1, y1 = max(p[0] for p in sec), max(p[1] for p in sec)
    path = rrect(x0 + inset, y0 + inset, x1 - inset, y1 - inset, 9, seg=16)
    K.tube(path, [1.7] * len(path), plate_m, flat=0.8, z=[PT - 0.3] * len(path), cyclic=True)


SHAPES, DIMPLES = [], []  # every plate's outlines and every pressed dimple, for the rolled-edge check
PLATES, FILMS = [], []  # every plate's footprint and every film's, for the fit check on the render
INSET = 4.0  # a layer behind a plate stays this far (px) inside the plate's footprint


def film(win, parent, grow=8, holes=(), r=8.0):
    """The backlit film behind one window only, so the gaps between plates stay dark; holes open it for sub-displays.
    It is grown past its window so the roll has film under it, then clamped INSET inside its parent plate, rounded
    corners included: a film grown past its plate shows as a lit sliver outside the panel."""
    x0, y0 = min(p[0] for p in win), min(p[1] for p in win)
    x1, y1 = max(p[0] for p in win), max(p[1] for p in win)
    px0, py0, px1, py1, pr = parent.rect
    bev = 0.4  # the curve bevel grows the slab by its radius: allow for it
    fx0, fy0 = max(x0 - grow, px0 + INSET + bev), max(y0 - grow, py0 + INSET + bev)
    fx1, fy1 = min(x1 + grow, px1 - INSET - bev), min(y1 + grow, py1 - INSET - bev)
    r = max(r, pr - INSET)
    assert fx0 < x0 and fy0 < y0 and fx1 > x1 and fy1 > y1, "the window is too close to its plate's edge for a film"
    FILMS.append((fx0 - bev, fy0 - bev, fx1 + bev, fy1 + bev, r + bev))
    K.slab([rrect(fx0, fy0, fx1, fy1, r)] + list(holes), DZ - 2.0, DZ, back, bevel=bev)


SUB = DZ - 1.5  # a sub-display's film: sunk 1.5 mm into the main film


def sub(x0, y0, x1, y1, power):
    """A display within the display: its own film sunk SUB below the main one through a hole, so the step throws a
    contact shadow round it, and a small lamp of its own along its top edge. Its printed frame sits on the step."""
    K.slab([rrect(x0 - 3, y0 - 3, x1 + 3, y1 + 3, 3)], SUB - 2.0, SUB, back, bevel=0.3)
    lip_light((x0 + x1) / 2, y0 + 1.0, power, depth=SUB + 5.0)


def inner(x0, y0, x1, y1):
    """The opening in the main film for a sub-display: inside its printed frame."""
    return rrect(x0 + 2, y0 + 2, x1 - 2, y1 - 2, 2)


def ball(x, y, r, z):
    """A plain polished metal ball button."""
    K.blob(x, y, r, r, r * 0.8, knobmetal, z=z, seg=48)


FZ = 0.05  # flat print sits this far (mm) above the film


def flat(outlines, mat, z=None):
    """A flat printed shape on the film: closed outlines filled with no depth and no bevel, casting no shadow."""
    ob = K.slab(outlines, DZ + FZ if z is None else z, DZ + FZ if z is None else z, mat, bevel=0.0)
    ob.visible_shadow = False
    return ob


def stroke(pts, w, mat, z=None):
    """A flat printed line on the film: a ribbon w px wide along the points, with no depth and no shadow."""
    bm = bmesh.new()
    left, right = [], []
    for i, p in enumerate(pts):
        a, b = pts[max(0, i - 1)], pts[min(len(pts) - 1, i + 1)]
        tx, ty = b[0] - a[0], b[1] - a[1]
        n = math.hypot(tx, ty) or 1.0
        nx, ny = -ty / n * w / 2, tx / n * w / 2
        left.append(bm.verts.new(K.P(p[0] + nx, p[1] + ny, (DZ if z is None else z) + FZ)))
        right.append(bm.verts.new(K.P(p[0] - nx, p[1] - ny, (DZ if z is None else z) + FZ)))
    for i in range(len(pts) - 1):
        bm.faces.new((left[i], left[i + 1], right[i + 1], right[i]))
    ob = _ob(bm, mat, "stroke")
    ob.visible_shadow = False
    return ob


def lamp_dot(x, y, r):
    """A flat printed indicator: a dark ring with an ink dot."""
    flat([circle(x, y, r + 2.0), circle(x, y, r)], dark)
    flat([circle(x, y, r - 0.8)], ink)


def marquee_ink(name, col, x0, x1, fade=10.0):
    """Film ink seen only between x0 and x1 px, fading in and out over fade px at each end: a scrolling readout's
    window, so text sliding through it appears and disappears like a radio's display."""
    mat = film_ink(name, col)
    nt = mat.node_tree
    N, Lk = nt.nodes, nt.links
    out = next(n for n in N if n.type == "OUTPUT_MATERIAL")
    bsdf = next(n for n in N if n.type == "BSDF_PRINCIPLED")
    sep = N.new("ShaderNodeSeparateXYZ")
    Lk.new(N.new("ShaderNodeNewGeometry").outputs["Position"], sep.inputs[0])
    ramps = []
    for a, b, lo, hi in ((x0, x0 + fade, 0.0, 1.0), (x1 - fade, x1, 1.0, 0.0)):
        mr = N.new("ShaderNodeMapRange")
        mr.clamp = True
        mr.inputs["From Min"].default_value, mr.inputs["From Max"].default_value = (a - W / 2) * MM, (b - W / 2) * MM
        mr.inputs["To Min"].default_value, mr.inputs["To Max"].default_value = lo, hi
        Lk.new(sep.outputs["X"], mr.inputs["Value"])
        ramps.append(mr)
    mn = N.new("ShaderNodeMath")
    mn.operation = "MINIMUM"
    Lk.new(ramps[0].outputs[0], mn.inputs[0])
    Lk.new(ramps[1].outputs[0], mn.inputs[1])
    mix = N.new("ShaderNodeMixShader")
    Lk.new(mn.outputs[0], mix.inputs[0])
    Lk.new(N.new("ShaderNodeBsdfTransparent").outputs[0], mix.inputs[1])
    Lk.new(bsdf.outputs[0], mix.inputs[2])
    Lk.new(mix.outputs[0], out.inputs[0])
    return mat


def knob(x, y, r, z, pointer=None, collar=True):
    """A polished metal knob that stands proud: a near-full sphere on a dark collar, a red pointer line on its crown
    when pointer gives an angle (degrees, 0 = right, -90 = up)."""
    if collar:
        K.cyl(x, y, r * 1.14, z - 0.6, z + 0.7, groove, seg=48)
    cz, rz = z + r * 0.3, r * 0.85
    K.blob(x, y, r, r, rz, knobmetal, z=cz, seg=64)
    if pointer is not None:
        a = math.radians(pointer)
        pts, zs = [], []
        for f in (0.2, 0.68):
            pts.append((x + math.cos(a) * r * f, y + math.sin(a) * r * f))
            zs.append(cz + rz * math.sqrt(max(0.0, 1 - f * f)) + 0.15)
        K.tube(pts, [0.9, 0.9], red, flat=0.6, z=zs, sharp=True)


FONTS = "/mnt/skills/examples/canvas-design/canvas-fonts/"


def label(text, x, y, size, mat, z, font="BigShoulders-Bold.ttf", align="CENTER", track=0.0):
    """Text laid on a surface at (x, y) px, size px tall: engraved on the metal, or printed flat on the film (below
    the plate), with no depth, like the printing on a calculator's backlit display."""
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body, cu.font, cu.size = text, bpy.data.fonts.load(font if font.startswith("/") else FONTS + font, check_existing=True), size * MM
    cu.align_x, cu.align_y, cu.space_character, cu.extrude = align, "CENTER", 1.0 + track, (0.0 if z < 0 else 0.06 * MM)
    cu.materials.append(mat)
    ob = bpy.data.objects.new("txt", cu)
    ob.location = K.P(x, y, z)
    K.coll.objects.link(ob)
    return ob


panel_kit.reset_scene()
ui_render.setup(W * 2, H * 2, SAMPLES, [("Room", (0.3, -1.0, -1.6), 0.7, (1.0, 0.97, 0.92), 8.0)], world=(0.05, 0.045, 0.04),
                bounces=dict(max_bounces=8, glossy_bounces=4, transmission_bounces=4, diffuse_bounces=3))
K = Shaper("nlog", W, H, True)
plate_m, back = K.mat("satin", lambda: satin("satin")), K.mat("display", lambda: display("display"))
ink, dark = K.mat("ink", lambda: film_ink("ink", "#9a7f4f")), K.mat("well", lambda: film_ink("well", "#7d6845"))
groove = K.mat("groove", lambda: printed("groove", "#2e271c"))
knobmetal = K.mat("knobmetal", lambda: kv.metal("knobmetal", "#d6d0c4", 0.16))
red = K.mat("red", lambda: printed("red", "#a83020"))
engrave = K.mat("engrave", lambda: printed("engrave", "#3a3328"))
listink = K.mat("listink", lambda: film_ink("listink", "#b39867"))
redink = K.mat("redink", lambda: film_ink("redink", "#a83020"))
glow = K.mat("glowline", lambda: streak("glowline"))

PT = 8.0  # the plates' top face (mm)
ROLL = 3.8  # the radius every windowed plate rolls over with: the frame round a window is one round bar
brass = K.mat("brass", lambda: kv.metal("brass", "#b0915a", 0.28))
# the chassis: dark, behind everything, so the gaps between plates read as deep slots
K.slab([rrect(0, 0, W, H, 12)], DZ - 6.0, DZ - 4.0, groove, bevel=0.5)

# ---- the top plate, one piece: the main display with a notch rising into its bottom edge for the volume and balance
# knobs, the long track-progress slot under it, the riveted transport plate, an eject dish, toggles, a perforated box
sec1 = rr(3, 3, W - 3, 262, 12)
PLATES.append(sec1.rect)
win1 = notched(14, 14, W - 14, 134, 6, 340, 74, 30, 30)
BOXES1 = ((38, 34, 200, 72), (38, 82, 200, 122), (430, 84, 512, 102))
film(rr(14, 14, W - 14, 134, 6), sec1, holes=[inner(*b) for b in BOXES1])
for b, pw in zip(BOXES1, (0.06, 0.06, 0.03)):
    sub(*b, pw)
# the track-progress slider is built like the equaliser's: a rolled slot through the plate, a gold streak in its
# dark depth, dotted ticks along both sides, the same ball knob
seek = rr(24, 169, W - 24, 183, 6)
p1 = plate([sec1, win1, seek], 0.0, PT, plate_m, ROLL)
for x in (290, 392):
    dimple(p1, x, 128, 15, 15, PT, 1.2)
dimple(p1, 296, 229, 17, 17, PT, 2.6)
finish(p1, 1.4)
bezel(win1)
for x0, y0, x1, y1 in BOXES1:
    flat([rrect(x0, y0, x1, y1, 3), rrect(x0 + 2, y0 + 2, x1 - 2, y1 - 2, 2)], dark)
stroke([(222, 70), (W - 36, 70)], 1.0, ink)
for i in range(31):
    x = 222 + i * (W - 258) / 30
    stroke([(x, 70.5), (x, 64 if i % 5 else 60)], 0.8, ink)
for x in (462, 502):
    lamp_dot(x, 118, 3.0)
label("EQ", 452, 118, 11, ink, DZ + FZ, font="Tektur-Medium.ttf", align="RIGHT")
label("PL", 512, 118, 11, ink, DZ + FZ, font="Tektur-Medium.ttf", align="LEFT")
for x, t in ((250, "320 KB/S"), (330, "44 KHZ"), (412, "MODE")):
    label(t, x, 84, 11, ink, DZ + FZ, font="Tektur-Medium.ttf")
# the volume and balance knobs stand proud of shallow dimples pressed into the notch, a divider between them
knob(290, 128, 12, PT - 1.0, pointer=-125)
knob(392, 128, 10, PT - 1.0, pointer=-90)
label("VOL", 318, 128, 13, engrave, PT, align="LEFT")
label("BAL", 364, 128, 13, engrave, PT, align="RIGHT")
K.tube([(341, 110), (341, 140)], [0.6, 0.6], groove, flat=0.4, z=[PT + 0.1] * 2, sharp=True)
if os.environ.get("NLOG_TEST"):  # legibility test: text right under the middle lamp
    label("LEGIBILITY TEST 0123", W * 0.5, 24, 11, ink, DZ + FZ, font="Tektur-Medium.ttf")
for x in (W * 0.2, W * 0.5, W * 0.8):  # three spots across the main display
    lip_light(x, 14, 0.3)
K.slab([rrect(36, 175, W - 36, 177, 1.0)], DZ + 0.0, DZ + 0.4, glow, bevel=0.2)
for i in range(int((W - 72) / 7.7) + 1):
    for dy in (-13, 13):
        K.cyl(36 + i * 7.7, 176 + dy, 0.9, PT - 0.2, PT + 0.25, groove, seg=10)

tp = plate([rr(22, 196, 270, 254, 9)], PT - 2.0, PT + 3.0, plate_m, 2.2)
for x in (69, 114, 160, 206, 248):
    cut(tp, box_bm(bmesh.new(), x, 233, 1.0, 34.0, PT + 2.4, PT + 4.0)) if x != 248 else None
bevel(tp, 0.5, 3, 30)
for x, y in ((29, 203), (263, 203), (29, 247), (263, 247)):
    K.blob(x, y, 2.2, 2.2, 1.3, knobmetal, z=PT + 3.0)
BTN = 8.0  # every push button on the console is this knob at this radius
for x in (48, 92, 137, 183, 228):
    # play, pause and stop are twice the console's push button; previous and next stay smaller, as on a deck
    knob(x, 233, BTN * (1.25 if x in (48, 228) else 1.75), PT + 3.0)
# media symbols over the buttons, engraved into the transport plate
gz = PT + 3.1
def tri(cx, cy, s, d):
    return [(cx - d * s * 0.5, cy - s * 0.6), (cx + d * s * 0.6, cy), (cx - d * s * 0.5, cy + s * 0.6)]
for shape in ([tri(51, 207, 4.5, -1)], [rrect(43, 203, 45, 211, 0.5)], [tri(93, 207, 5.5, 1)], [rrect(133, 203, 135.5, 211, 0.5)],
              [rrect(138.5, 203, 141, 211, 0.5)], [rrect(179, 203, 187, 211, 0.6)], [tri(225, 207, 4.5, 1)], [rrect(231, 203, 233, 211, 0.5)]):
    K.slab(shape, gz - 0.2, gz + 0.1, engrave, bevel=0.05)
K.slab([[(290, 210), (302, 210), (296, 203)]], PT - 0.2, PT + 0.15, engrave, bevel=0.05)
K.slab([rrect(290, 212, 302, 214, 0.4)], PT - 0.2, PT + 0.15, engrave, bevel=0.05)
knob(296, 229, BTN, PT - 2.6, collar=False)
for x, name in ((356, "SHUFFLE"), (430, "REPEAT")):
    label(name, x, 212, 11, engrave, PT)
    label("OFF", x - 17, 229, 10, engrave, PT, align="RIGHT")
    label("ON", x + 17, 229, 10, engrave, PT, align="LEFT")
for x in (356, 430):
    K.slab([rrect(x - 14, 222, x + 14, 236, 3)], PT, PT + 1.0, groove, bevel=0.4)
    K.slab([rrect(x - 12 if x == 356 else x, 224, x if x == 356 else x + 12, 234, 2)], PT + 1.0, PT + 2.0, knobmetal, bevel=0.5)
gb = plate([rr(468, 202, 534, 254, 6)], PT - 2.0, PT + 2.0, plate_m, 1.8)
holes = bmesh.new()
for gx in range(474, 530, 5):
    for gy in range(207, 252, 5):
        if not (500 < gx < 526 and 216 < gy < 242):
            cyl_bm(holes, gx, gy, 1.1, PT, PT + 3.0, seg=10)
cut(gb, holes)
dimple(gb, 512, 229, 12, 12, PT + 2.0, 1.6)
knob(512, 229, BTN * 0.8, PT + 0.6, collar=False)

# ---- section 3: the equaliser: a small curve display, a preamp and ten sliders in slots, a boost and damp dish
sec3 = rr(3, 266, W - 3, 456, 12)
PLATES.append(sec3.rect)
win3 = rr(14, 277, W - 14, 320, 6)
film(win3, sec3, holes=[inner(170, 282, 400, 312)])
sub(170, 282, 400, 312, 0.05)
xs = [42] + [160 + i * 37 for i in range(10)]
slots = [rr(x - 7, 332, x + 7, 440, 6) for x in xs]
p3 = plate([sec3, win3] + slots, 0.0, PT, plate_m, ROLL)
dimple(p3, 102, 388, 24, 24, PT, 2.8, rz=9.0)
finish(p3, 1.4)
bezel(win3)
flat([rrect(170, 282, 400, 312, 3), rrect(172, 284, 398, 310, 2)], dark)
for x in (38, 98):
    lamp_dot(x, 297, 2.6)
label("ON", 48, 297, 11, ink, DZ + FZ, font="Tektur-Medium.ttf", align="LEFT")
label("AUTO", 108, 297, 11, ink, DZ + FZ, font="Tektur-Medium.ttf", align="LEFT")
label("PRESETS", 488, 297, 11, ink, DZ + FZ, font="Tektur-Medium.ttf", align="RIGHT")
lip_light(W * 0.5, 277, 0.2)  # one in the middle panel
lamp_dot(500, 297, 2.6)
for x in xs:
    K.slab([rrect(x - 1.0, 340, x + 1.0, 432, 1.0)], DZ + 0.0, DZ + 0.4, glow, bevel=0.2)
    for j in range(13):
        for dx in (-13, 13):
            K.cyl(x + dx, 340 + j * 7.7, 0.9, PT - 0.2, PT + 0.25, groove, seg=10)
# the now-playing line scrolls like a radio's: track, artist, album, round and round
NOW = "THE LONG WATCH  ·  CONCORD FLEET ORCHESTRA  ·  SUITES FROM THE DEEP  ·  "
TITLE = dict(size=12, font="Tektur-Medium.ttf", track=0.12)
marquee = K.mat("marquee", lambda: marquee_ink("marquee", "#9a7f4f", 222, W - 36))
LOOP_S = float(os.environ.get("NLOG_FRAMES", 96)) / 12.0  # the loop's length, s


def text_w(body):
    """The width in px of a title line, measured on the real text object."""
    ws = []
    for b in (body + "X", body + body + "X"):
        ob = label(b, 0, -200, TITLE["size"], ink, DZ + FZ, font=TITLE["font"], align="LEFT", track=TITLE["track"])
        bpy.context.view_layer.update()
        ws.append(ob.dimensions.x / MM)
        bpy.data.objects.remove(ob, do_unlink=True)
    return ws[1] - ws[0]


NOW_W = text_w(NOW)
LEVELS = [0.55, 0.42, 0.28, 0.18, 0.08, 0.35, 0.5, 0.78, 0.82, 0.7, 0.25]
chrome = knobmetal


def live(t):
    """Everything that moves, at loop phase t (0 to 1; 1 is the same as 0): the waveform scrolls, the mode trace
    drifts, the play clock counts, the seek knob creeps on, the EQ knobs ride a slow wave and the EQ curve follows them."""
    ph = 2 * math.pi * t
    stroke([(44 + i * 3, 102 + math.sin(i * 0.55 - 2 * ph) * 9 * math.sin(i * 0.11 + ph)) for i in range(51)], 1.2, ink, SUB)
    stroke([(436 + i, 93 - 5 * math.sin(i / 70 * 2 * math.pi - ph)) for i in range(0, 71, 2)], 1.0, redink, SUB)
    label("07:%02d" % (42 + int(t * LOOP_S)), 119, 53, 30, ink, SUB + FZ, font="Tektur-Medium.ttf")
    label(NOW * 3, 222 - t * NOW_W, 46, TITLE["size"], marquee, DZ + FZ, font=TITLE["font"], align="LEFT", track=TITLE["track"])
    knob(150 + 5.0 * t, 176, 11.0, PT - 0.5, collar=False)
    lv = [b + (0.0 if i == 0 else 0.13 * math.sin(ph + i * 0.6)) for i, b in enumerate(LEVELS)]
    for x, l in zip(xs, lv):
        knob(x, 342 + l * 90, 11.0, PT - 0.5, collar=False)
    bands = lv[1:]  # the EQ curve: a smooth line through the ten band levels, raised where a band is boosted
    pts = []
    for i in range(51):
        u = i / 50 * (len(bands) - 1)
        k = min(int(u), len(bands) - 2)
        f = (1 - math.cos((u - k) * math.pi)) / 2
        pts.append((176 + i * 4.4, 297 + ((bands[k] * (1 - f) + bands[k + 1] * f) - 0.45) * 22))
    stroke(pts, 1.0, ink, SUB)


def tracked(fn, *a):
    """Run a builder and return the objects it made."""
    before = set(bpy.data.objects.keys())
    fn(*a)
    return [o for o in bpy.data.objects if o.name not in before]


LIVE = tracked(live, float(os.environ.get("NLOG_T", 0.0)))
for x, t in zip(xs, ["PREAMP", "60", "170", "310", "600", "1K", "3K", "6K", "12K", "14K", "16K"]):
    label(t, x, 449, 11, engrave, PT)
for y, t in ((340, "+20"), (386, "0 DB"), (432, "-20")):
    label(t, 532, y, 10, engrave, PT)
label("BOOST", 102, 356, 11, engrave, PT)
label("DAMP", 102, 421, 11, engrave, PT)
K.tube([(132, 386), (W - 28, 386)], [0.35, 0.35], K.mat("red", lambda: printed("red", "#a83020")), flat=0.4, z=[PT + 0.2] * 2, sharp=True)
knob(102, 388, BTN + 1.0, PT - 2.8, collar=False)

# ---- section 4: the list window
sec4 = rr(3, 460, W - 3, 638, 12)
PLATES.append(sec4.rect)
win4 = rr(14, 471, W - 14, 627, 7)
film(win4, sec4)
plate([sec4, win4], 0.0, PT, plate_m, ROLL)
bezel(win4)
for i, (t, d) in enumerate((("1.  Admiral's Theme", "4:12"), ("2.  Nebula Drift", "6:05"), ("3.  The Long Watch", "3:48"), ("4.  Concord March", "5:30"))):
    label(t, 40, 506 + i * 28, 15, listink, DZ + FZ, font="/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", align="LEFT", track=0.06)
    label(d, 500, 506 + i * 28, 15, listink, DZ + FZ, font="/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", align="RIGHT", track=0.06)
flat([rrect(514, 483, 522, 501, 4)], dark)  # the list's scroll marker
for x in (W * 0.2, W * 0.5, W * 0.8):  # three across the list
    lip_light(x, 471, 0.3)

# ---- section 5: four keys in oval dimples, a small time display, a list key in its own dimple
sec5 = rr(3, 642, W - 3, H - 3, 12)
PLATES.append(sec5.rect)
win5 = rr(254, 653, 442, 686, 4)
film(win5, sec5)
p5 = plate([sec5, win5], 0.0, PT, plate_m, ROLL)
for x in (46, 102, 158, 214):
    dimple(p5, x, 666, 22, 15, PT, 2.6)
dimple(p5, 498, 666, 20, 15, PT, 2.6)
finish(p5, 1.4)
bezel(win5, 2.0)
for x, t in ((46, "ADD"), (102, "REM"), (158, "SEL"), (214, "MISC")):
    knob(x, 666, BTN, PT - 2.6, collar=False)
    label(t, x, 688, 10, engrave, PT)
knob(498, 666, BTN, PT - 2.6, collar=False)
label("LIST", 498, 688, 10, engrave, PT)
label("0:00 / 31:27", 348, 669, 14, ink, DZ + FZ, font="Tektur-Medium.ttf", track=0.1)
lip_light(348, 653, 0.05)  # one in the small display, gentler: the window is only 42 px tall

for x, y in ((14, 160), (W - 14, 160), (14, 438), (W - 14, 438)):
    kv.screw(K, x, y, z=PT, r=2.4)

DISPLAY = {"display", "ink", "well", "glowline", "faint", "listink", "redink", "marquee", "jewel_off"}
EMIT = ("display", "glowline", "faint", "listink", "ink", "well", "redink", "marquee")


def layer(obs):
    """Put new objects on their layer: metal on the metal layer; display print on the display layer, shadowless, its
    glow reaching only the display."""
    for ob in obs:
        if ob.type not in ("MESH", "CURVE", "FONT") or not ob.active_material:
            continue
        m = ob.active_material.name
        if m in DISPLAY:
            display_layer.objects.link(ob)
            if m != "display":
                ob.visible_shadow = False
            if m in EMIT:
                ob.light_linking.receiver_collection = display_layer
        else:
            metal_layer.objects.link(ob)


metal_layer = bpy.data.collections.new("metal_layer")
bpy.context.scene.collection.children.link(metal_layer)
for ob in list(bpy.context.scene.objects):
    if ob.type in ("MESH", "CURVE", "FONT") and ob.active_material and ob.active_material.name not in DISPLAY:
        metal_layer.objects.link(ob)
sb = bpy.data.lights.new("Softbox", "AREA")
sb.shape, sb.size, sb.energy, sb.color = "DISK", 0.6, 1.2, (1.0, 0.94, 0.85)
sbo = bpy.data.objects.new("Softbox", sb)
sbo.location, sbo.rotation_euler = (-0.25, 0.35, 0.6), (math.radians(-35), math.radians(-20), 0.0)
bpy.context.scene.collection.objects.link(sbo)
for ob in bpy.context.scene.objects:  # the film's printing is flat ink: it casts no shadow
    if ob.type in ("MESH", "CURVE", "FONT") and ob.active_material and ob.active_material.name in DISPLAY and ob.active_material.name != "display":
        ob.visible_shadow = False
display_layer = bpy.data.collections.new("display_layer")
bpy.context.scene.collection.children.link(display_layer)
for ob in list(bpy.context.scene.objects):
    if ob.type in ("MESH", "CURVE", "FONT") and ob.active_material and ob.active_material.name in DISPLAY:
        display_layer.objects.link(ob)
# the room light and the softbox reach only the metal; the lip lamps and the glowing film itself reach only the display
# the main light reaches the displays too, from the same upper left as the room light on the metal: the plate's lip
# throws its shadow down onto each display's top edge, and a thinner one along its left, so the wells read as deep
ui_render.sun("Main", (0.3, -1.0, -1.6), MAIN, (1.0, 0.97, 0.92), MAIN_SOFT)  # a broad sun: the shadow fades out softly
cl = bpy.data.lights.new("Ceiling", "AREA")
cl.shape, cl.size, cl.energy, cl.color = "SQUARE", 1.2, 1.6, (1.0, 0.97, 0.92)
clo = bpy.data.objects.new("Ceiling", cl)
clo.location = (0.0, 0.1, 0.7)
bpy.context.scene.collection.objects.link(clo)
for ob in bpy.context.scene.objects:
    if ob.type == "LIGHT":
        ob.light_linking.receiver_collection = display_layer if ob.name.startswith(("lip", "Main")) else metal_layer
    elif ob.active_material and ob.active_material.name in EMIT:
        ob.light_linking.receiver_collection = display_layer

if os.environ.get("NLOG_GEOM"):  # write the geometry for the fit check and stop, without rendering
    json.dump(dict(W=W, H=H, plates=PLATES, films=FILMS, shapes=SHAPES, dimples=DIMPLES), open(os.environ["NLOG_GEOM"], "w"))
    sys.exit(0)
panel_kit.camera(W * MM, H * MM)
sc = bpy.context.scene
if os.environ.get("NLOG_TOP"):  # render only the main display, for quick comparisons
    sc.render.use_border, sc.render.use_crop_to_border = True, True
    sc.render.border_min_x, sc.render.border_max_x, sc.render.border_min_y, sc.render.border_max_y = 0.0, 1.0, 1.0 - 200 / H, 1.0
if os.environ.get("NLOG_SCALE"):  # render size, percent of the 2x size (274: about 3000 x 3840 px)
    sc.render.resolution_percentage = int(os.environ["NLOG_SCALE"])
if os.environ.get("NLOG_ANIM"):  # an animation loop: NLOG_FRAMES frames into the folder NLOG_ANIM
    n, out = int(os.environ.get("NLOG_FRAMES", 96)), os.environ["NLOG_ANIM"]
    os.makedirs(out, exist_ok=True)
    for f in range(n):
        for ob in LIVE:
            bpy.data.objects.remove(ob, do_unlink=True)
        LIVE = tracked(live, f / n)
        layer(LIVE)
        sc.render.filepath = os.path.join(out, "f%03d.png" % f)
        bpy.ops.render.render(write_still=True)
        print("NLOG frame", f + 1, "of", n, flush=True)
else:
    sc.render.filepath = OUT
    bpy.ops.render.render(write_still=True)
json.dump(dict(W=W, H=H, plates=PLATES, films=FILMS, shapes=SHAPES, dimples=DIMPLES), open(os.path.splitext(OUT)[0] + "_fit.json", "w"))
print("NLOG done", OUT)
