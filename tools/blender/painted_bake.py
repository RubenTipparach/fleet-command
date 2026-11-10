"""Painted bake: a high-poly hull tile modelled in Blender, its occlusion, normals, edges and plate ids rendered face-on in
Cycles, then painted over in numpy so it reads drawn on, the way late-1990s ship textures were painted over their renders
(the painted-bake skill, .claude/skills/painted-bake/SKILL.md; OpenSpec change ssarith-ship-concepts, version 10).

The owner, 2026-11-05, with the reference game's ships: "make ships like this, with this kind of texture", "might need a
new skill to make high -poly AO baked texture that looks drawn on". What is taken from the references is the method and
the look (painted light, dark crevices, lit edges, flat colour per plate, glowing strips), never their art (CLAUDE.md 1).

One render writes these passes (the shared panel kit's camera and EXR reading, tools/blender/panel_kit.py):
  normal   the Normal pass (the tile faces +z: tangent space, x along the ship, y across the face)
  ao       the AO pass, reaching 0.45 m
  edge     an AOV: how far the bevelled normal leaves the true normal, i.e. the lit and shaded edges a painter picks out
  rid      an AOV: a random number per piece, so every plate can take its own paint
  emit     the Emit pass (the light strips)
and the paint-over writes the maps a page uses:
  <tile>_albedo.png   painted colour with its light and shade in it (upper-left light, as a painter lights a sheet)
  <tile>_normal.png   the normal map, for the live light over the paint
  <tile>_emit.png     the strips' glow

    /opt/blender/blender -b --python tools/blender/painted_bake.py -- [--tile hunt_chevron] [--samples 32] [--ppm 128]
    /opt/blender/blender -b --python tools/blender/painted_bake.py -- --ship hooded_lance,forked_prow,harpoon,trident [--px 2048]
    /opt/blender/blender -b --python tools/blender/painted_bake.py -- --faction kelvane --ship kelvane_frigate [--px 2048]

Writes assets/textures/painted/ and its manifest.json (sizes in metres, sha256 of every file).
"""

import hashlib
import json
import math
import os
import random
import sys
import time

import bpy
import numpy as np
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import panel_kit as K  # noqa: E402  the shared kit: Panel pieces, the camera, EXR reading, PNG writing

ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "assets", "textures", "painted")
RAW = os.path.join(ROOT, "tools", "materials", "raw", "painted")


# ----------------------------------------------------------------------------- materials that write the paint's passes
def pass_material(name, emit=None):
    """One material for every hull piece: a mid grey diffuse (the paint-over sets the colour), and two AOVs: `edge`
    (1 - the bevelled normal against the true one) and `rid` (a random number per object). emit: an emission colour."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    N, L = m.node_tree.nodes, m.node_tree.links
    for n in list(N):
        N.remove(n)
    out = N.new("ShaderNodeOutputMaterial")
    if emit:
        em = N.new("ShaderNodeEmission"); em.inputs[0].default_value = (*emit, 1.0); em.inputs[1].default_value = 4.0
        L.new(em.outputs[0], out.inputs[0])
    else:
        bsdf = N.new("ShaderNodeBsdfDiffuse"); bsdf.inputs[0].default_value = (0.5, 0.5, 0.5, 1.0)
        L.new(bsdf.outputs[0], out.inputs[0])
    geo = N.new("ShaderNodeNewGeometry")
    bev = N.new("ShaderNodeBevel"); bev.samples = 8; bev.inputs["Radius"].default_value = 0.035
    dot = N.new("ShaderNodeVectorMath"); dot.operation = "DOT_PRODUCT"
    L.new(bev.outputs[0], dot.inputs[0]); L.new(geo.outputs["Normal"], dot.inputs[1])
    inv = N.new("ShaderNodeMath"); inv.operation = "SUBTRACT"; inv.inputs[0].default_value = 1.0
    L.new(dot.outputs["Value"], inv.inputs[1])
    a1 = N.new("ShaderNodeOutputAOV"); a1.aov_name = "edge"; L.new(inv.outputs[0], a1.inputs["Value"])
    oi = N.new("ShaderNodeObjectInfo")
    a2 = N.new("ShaderNodeOutputAOV"); a2.aov_name = "rid"; L.new(oi.outputs["Random"], a2.inputs["Value"])
    return m


def setup(w_px, h_px, samples):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    cy = sc.cycles
    cy.device, cy.samples, cy.use_adaptive_sampling, cy.use_denoising, cy.seed = "CPU", samples, False, False, 0
    cy.max_bounces, cy.diffuse_bounces = 2, 1
    sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage = w_px, h_px, 100
    sc.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.new("World"); world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.0
    world.light_settings.distance = 0.45
    sc.world = world
    vl = sc.view_layers[0]
    vl.use_pass_normal = vl.use_pass_ambient_occlusion = vl.use_pass_emit = True
    for name in ("edge", "rid"):
        a = vl.aovs.add(); a.name = name; a.type = "VALUE"
    sc.use_nodes = True
    sc.render.use_compositing = True
    nt = sc.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    rl = nt.nodes.new("CompositorNodeRLayers")
    fo = nt.nodes.new("CompositorNodeOutputFile")
    fo.base_path = RAW; fo.format.file_format = "OPEN_EXR"; fo.format.color_depth = "32"; fo.file_slots.clear()
    return rl, fo, nt


# ----------------------------------------------------------------------------- the tile: the Hunt's armour
def tile_hunt_chevron(P, w, h, rng):
    """8 m along the ship (x) by 6 m across (y): two rows of armour plates cut as chevrons pointing forward (+x), a raised
    spine between them with a light strip in its groove, conduits in clamps either side of the spine, vent wells with
    slats, bolts at the plates' corners and a seam band at each long edge (where the tile meets the next, one seam)."""
    P.box(-w / 2, -h / 2, w / 2, h / 2, -0.15, 0.0, "dark")
    for sy in (-1, 1):  # the seam bands at the tile's long edges (half a band each side, so the wrap shows one)
        P.box(-w / 2, sy * h / 2 - (0.08 if sy > 0 else -0.08), w / 2, sy * h / 2, 0.0, 0.03, "hull")
    for row, yc in enumerate((-1.6, 1.6)):
        for k in range(4):  # under-plates: a seamed band of 2 m plates under each row
            x0 = -w / 2 + k * 2.0
            P.box(x0 + 0.03, yc - 1.32, x0 + 2.0 - 0.03, yc + 1.32, 0.0, 0.05, "hull", bevel=0.03)
        off = 0.0 if row == 0 else 1.0
        for k in range(4):
            cx = -w / 2 + 1.0 + off + k * 2.0
            if cx > w / 2 - 0.2:
                cx -= w
            a, hh, t = 0.92, 1.12, 0.5
            z = 0.14 + rng.random() * 0.08
            # each chevron is two convex arms meeting on its centreline (a concave outline bevels badly)
            for sy in (1, -1):
                arm = [(cx + a, yc), (cx - a, yc + sy * hh), (cx - a + t, yc + sy * hh), (cx + a - t * 1.3, yc)]
                P.poly(arm if sy > 0 else list(reversed(arm)), 0.05, z, "hull", bevel=0.06)
                P.cyl(cx - a + 0.25, yc + sy * (hh - 0.2), 0.05, z, z + 0.03, "hull", seg=8)
    # the spine and its light strip, in segments so the strip has breaks
    P.box(-w / 2, -0.32, w / 2, 0.32, 0.0, 0.14, "hull", bevel=0.04)
    P.box(-w / 2, -0.1, w / 2, 0.1, 0.12, 0.15, "dark")
    for x0, x1 in ((-3.6, -0.4), (0.4, 3.6)):
        P.box(x0, -0.06, x1, 0.06, 0.15, 0.17, "glow")
    for sy in (-1, 1):  # conduits either side of the spine, in clamps
        P.tube([(-w / 2, sy * 0.62, 0.07), (w / 2, sy * 0.62, 0.07)], 0.06, "hull")
        for x in (-3.0, -1.0, 1.0, 3.0):
            P.box(x - 0.07, sy * 0.62 - 0.12, x + 0.07, sy * 0.62 + 0.12, 0.0, 0.15, "hull", bevel=0.015)
    for yc in (-2.75, 2.75):  # vent wells near the edges
        for x in (-2.0, 2.0):
            P.box(x - 0.55, yc - 0.12, x + 0.55, yc + 0.12, 0.0, 0.04, "hull", bevel=0.015)
            P.box(x - 0.48, yc - 0.08, x + 0.48, yc + 0.08, 0.0, 0.041, "dark")
            for i in range(6):
                xx = x - 0.4 + i * 0.16
                P.box(xx - 0.03, yc - 0.07, xx + 0.03, yc + 0.07, 0.0, 0.05, "hull")


TILES = {"hunt_chevron": (tile_hunt_chevron, 8.0, 6.0)}


# ----------------------------------------------------------------------------- the paint-over
PALETTE = ["#6b6b46", "#7d7450", "#5a5b3b", "#8a7d54", "#4f5236"]  # olive, khaki, dark olive, sand, deep olive
BRONZE = "#7a5a36"
INK = "#14160e"
LIGHT_EDGE = "#d9d2a6"
GLOW = "#46ff8a"


def hexrgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)], np.float32)


def blur(a, r):
    """A box blur of radius r pixels, wrapping (the tile repeats)."""
    if r <= 0:
        return a
    out = a.copy()
    for ax in (0, 1):
        acc = np.zeros_like(out)
        for d in range(-r, r + 1):
            acc += np.roll(out, d, axis=ax)
        out = acc / (2 * r + 1)
    return out


def piece_paint(rid):
    """Flat paint per piece, from the palette by its random id; a few pieces in bronze."""
    pal = np.stack([hexrgb(c) for c in PALETTE])
    idx = np.clip((rid * len(PALETTE) * 7).astype(int) % len(PALETTE), 0, len(PALETTE) - 1)
    return np.where((rid > 0.93)[..., None], hexrgb(BRONZE), pal[idx])


def paint(nor, ao, edge, rid, emit, rng_seed=5, light=(-0.5, 0.55, 0.67), base=None):
    """Paint the passes into a drawn-on albedo (sRGB, 0..1) and a glow map. light: the painter's light direction in the
    passes' own space (a tile's: x along, y across, z out; a ship's: +x port, +y up, +z bow). base: the flat paint, when
    the caller has laid its own (a ship's plating and markings); by default, piece_paint(rid)."""
    h, w = ao.shape
    rng = np.random.default_rng(rng_seed)
    n = nor / np.maximum(np.linalg.norm(nor, axis=2, keepdims=True), 1e-6)
    if base is None:
        base = piece_paint(rid)
    # the painter's light from the upper left, a little toward the viewer, in a few soft steps
    L = np.array(light, np.float32); L /= np.linalg.norm(L)
    lam = np.clip((n * L).sum(axis=2), 0.0, 1.0)
    steps = np.floor(lam * 4.0 + 0.5) / 4.0
    shade = 0.62 + 0.5 * (0.55 * lam + 0.45 * steps)
    col = base * shade[..., None]
    # crevices painted dark (the occlusion, curved, then inked where it is deepest)
    ao = blur(ao, 1)  # a pixel's softening takes the render's speckle out of the deepest shadows
    aoc = np.clip(ao, 0.0, 1.0) ** 1.6
    col *= (0.35 + 0.65 * aoc)[..., None]
    ink = np.clip((0.42 - ao) / 0.2, 0.0, 1.0)
    col = col * (1 - ink[..., None] * 0.8) + hexrgb(INK) * (ink[..., None] * 0.8)
    # edges: lit edges lightened toward a pale highlight, shaded edges darkened
    e = np.clip(edge * 6.0, 0.0, 1.0)
    lit = e * np.clip((lam - 0.55) / 0.3, 0.0, 1.0)
    dark = e * np.clip((0.45 - lam) / 0.3, 0.0, 1.0)
    col = col * (1 - lit[..., None] * 0.6) + hexrgb(LIGHT_EDGE) * (lit[..., None] * 0.6)
    col *= (1 - dark * 0.45)[..., None]
    # brush grain: low streaks along the ship and a faint mottle, so the flats are not machine flat
    streak = blur(rng.standard_normal((h, w)).astype(np.float32), 2)
    sx = np.zeros_like(streak)
    for d in range(-12, 13):
        sx += np.roll(streak, d, axis=1)
    sx /= 25.0
    mott = blur(rng.standard_normal((h // 8 + 1, w // 8 + 1)).astype(np.float32), 1)
    mott = np.kron(mott, np.ones((8, 8), np.float32))[:h, :w]
    col *= (1.0 + 0.06 * sx / (np.abs(sx).max() + 1e-6) + 0.05 * mott / (np.abs(mott).max() + 1e-6))[..., None]
    # the strips: their own paint, and the glow map
    g = np.clip(emit.max(axis=2) / 2.0, 0.0, 1.0)
    col = col * (1 - g[..., None]) + hexrgb(GLOW) * g[..., None]
    glow = hexrgb(GLOW) * blur(g, 2)[..., None] * 1.6
    return np.clip(col, 0, 1), np.clip(glow, 0, 1)


def bake(name, w, h, ppm, samples):
    K.reset_scene()
    w_px, h_px = int(round(w * ppm)), int(round(h * ppm))
    rl, fo, nt = setup(w_px, h_px, samples)
    mats = {"hull": pass_material("hull"), "dark": pass_material("dark"), "glow": pass_material("glow", emit=(0.27, 1.0, 0.54))}
    P = K.Panel(name, w, h, mats, seed=11)
    TILES[name][0](P, w, h, random.Random(1201))
    K.camera(w, h)
    for sock, slot in (("Normal", "normal"), ("AO", "ao"), ("Emit", "emit"), ("edge", "edge"), ("rid", "rid")):
        fo.file_slots.new(f"{name}_{slot}")
        nt.links.new(rl.outputs[sock], fo.inputs[-1])
    comp = nt.nodes.new("CompositorNodeComposite"); nt.links.new(rl.outputs["Image"], comp.inputs[0])
    bpy.ops.render.render(write_still=False)
    get = lambda p: K.read_exr(os.path.join(RAW, f"{name}_{p}0001.exr"))
    nor, ao, emit, edge, rid = get("normal"), get("ao")[:, :, 0], get("emit"), get("edge")[:, :, 0], get("rid")[:, :, 0]
    col, glow = paint(nor, ao, edge, rid, emit)
    K.save_png(os.path.join(OUT, f"{name}_albedo.png"), col)
    n = nor / np.maximum(np.linalg.norm(nor, axis=2, keepdims=True), 1e-6)
    K.save_png(os.path.join(OUT, f"{name}_normal.png"), n * 0.5 + 0.5)
    K.save_png(os.path.join(OUT, f"{name}_emit.png"), glow)
    return {"u_m": w, "v_m": h, "px": [w_px, h_px], "samples": samples}


# ----------------------------------------------------------------------------- a ship of its own (owner, 2026-11-05)
# "every ship needs custom uv Unwraped textures. From NOW on . ALL SHIPS MUST AVOID REPETTIVE TILE FOR MAIN COLOR MAP",
# "tile textures are for DETAIL only", "dont forget to bake AO as well"; then, on the plain plating: "I'd like more
# features onthe hulls ... vents, lights, and sub armor pieces, and primary armor peices", "liveries, smooth sphereical
# portions of armor, and specifically shaped vents", "no repeating elemnts, single axis symmetry, and lots of shading to
# give the armor depth". A picked hull comes from its page as OBJ (tools/mockups/export_blockouts.mjs, ship space: +x port,
# +y up, +z bow, imported as it is), is joined and unwrapped once (Smart UV Project, islands packed again by shape), and
# hull_detail.py builds its high-poly layer over it (armour plates cut apart, the bare under layer with greebles, domes,
# fans, louvres, lamps; data/ships/ssarith/hull_detail.json). That layer is baked onto the hull's own UVs, selected to
# active: a data pass (kind, plate id, edges), glow, position, object-space normals, AO, and a sun's direct light with
# its cast shadows. paint_ship turns them into the colour map, and the maps and the UV mesh are written out.
SHIPS_RAW = os.path.join(RAW, "ships")
SUN = (0.6, 0.55, 0.58)       # the painter's light, ship space: low, from port and ahead, so the relief throws shadows
FACTION = "ssarith"           # --faction: whose ships (data/ships/<faction>/hull_detail.json, assets/ships/<faction>/)
PT = {}                       # the faction's paint (hull_detail.json "paint"), set by ship_main before any ship is painted


def ships_out():
    """Where a faction's baked maps go: its data's "out" (the Concord's and Krai's own folders hold the game's models, so
    their bakes go in a baked/ folder Godot does not import), else assets/ships/<faction>/."""
    out = json.load(open(detail_path())).get("out") if os.path.exists(detail_path()) else None
    return os.path.join(ROOT, out) if out else os.path.join(ROOT, "assets", "ships", FACTION)


def mesh_of(slug):
    """The hull a ship is baked on: its entry's "mesh" (several designs of one hull share its export), else its slug."""
    return json.load(open(detail_path()))["ships"][slug].get("mesh", slug)


def detail_path():
    return os.path.join(ROOT, "data", "ships", FACTION, "hull_detail.json")


def load_detail(slug=None):
    """The detail layout for FACTION (data/ships/<faction>/hull_detail.json). A ship that names a look ("look": name, as
    the freighters do, every faction's in one file: openspec/changes/freighters) takes that look's paint, defaults and
    faction from the file's "looks"; a look of {"from": "<faction>"} is that faction's own hull_detail.json, so a faction's
    look has one copy (CLAUDE.md 6.1). The file's own "defaults" lie over the look's, and the ship's over both (for_ship)."""
    D = json.load(open(detail_path()))
    name = slug and D.get("ships", {}).get(slug, {}).get("look")
    if not name:
        return D
    L = dict(D["looks"][name])
    if "from" in L:
        src = json.load(open(os.path.join(ROOT, "data", "ships", L.pop("from"), "hull_detail.json")))
        L = {"faction": src.get("faction"), "paint": src["paint"], "defaults": src["defaults"], **L}
    return {**D, "faction": L.get("faction") or name, "paint": L["paint"], "defaults": {**L["defaults"], **D.get("defaults", {})}}


def lo_material(img, name):
    m = bpy.data.materials.new(name); m.use_nodes = True
    tex = m.node_tree.nodes.new("ShaderNodeTexImage"); tex.image = img; m.node_tree.nodes.active = tex
    return m


def ship_uv_m(ship):
    """Metres of hull per UV unit: the square root of the mesh's area over its UV area."""
    import bmesh
    bm = bmesh.new(); bm.from_mesh(ship.data); bmesh.ops.triangulate(bm, faces=bm.faces[:])
    uvl = bm.loops.layers.uv.active
    a3 = a2 = 0.0
    for f in bm.faces:
        a3 += f.calc_area()
        (u0, v0), (u1, v1), (u2, v2) = (tuple(l[uvl].uv) for l in f.loops)
        a2 += abs((u1 - u0) * (v2 - v0) - (u2 - u0) * (v1 - v0)) / 2
    bm.free()
    return math.sqrt(a3 / max(a2, 1e-9))


def img_array(img):
    w, h = img.size
    a = np.empty(w * h * 4, np.float32); img.pixels.foreach_get(a)
    return a.reshape(h, w, 4)


def bake_ship(slug, px, samples, panes_only=False):
    """Bake one ship. panes_only: build its detail and return only its panes (no bake, no maps written)."""
    import bmesh
    import hull_detail as HD
    import hull_sdf
    K.reset_scene(); HD.MATS.clear()
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = samples; sc.cycles.use_denoising = False; sc.cycles.seed = 0
    world = bpy.data.worlds.new("World"); world.use_nodes = True; sc.world = world; world.light_settings.distance = 2.2
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.0
    bpy.ops.wm.obj_import(filepath=os.path.join(SHIPS_RAW, mesh_of(slug) + ".obj"), forward_axis="Y", up_axis="Z")
    obs = sorted((o for o in sc.objects if o.type == "MESH"), key=lambda o: o.name)
    img = bpy.data.images.new(slug + "_bake", px, px, alpha=True, float_buffer=True); img.colorspace_settings.name = "Non-Color"
    lo_hull, lo_glow, lo_well = lo_material(img, "lo_hull"), lo_material(img, "lo_glow"), lo_material(img, "lo_well")
    lo_maker = {}   # hull_<maker>_* a part taken from another faction's ship (a Vos conversion): its own material, so its maker is baked
    for o in obs:   # glow_* the light strips, well_* dark glass (hull_detail keeps both bare of armour), the rest the hull
        mk = o.name.split("_")[1] if o.name.startswith("hull_") and o.name.split("_")[1] in MAKERS else None
        if mk and mk not in lo_maker:
            lo_maker[mk] = lo_material(img, "lo_hull_" + mk)
        o.data.materials.clear(); o.data.materials.append(lo_glow if o.name.startswith("glow") else lo_well if o.name.startswith("well") else lo_maker[mk] if mk else lo_hull)
    bpy.ops.object.select_all(action="DESELECT")
    for o in obs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    bpy.ops.object.join()
    ship = bpy.context.view_layer.objects.active
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=0.001)
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.006, area_weight=0.0, correct_aspect=True, scale_to_bounds=False)
    bpy.ops.uv.select_all(action="SELECT")
    bpy.ops.uv.pack_islands(rotate=True, shape_method="CONCAVE", margin_method="FRACTION", margin=0.004)
    bpy.ops.object.mode_set(mode="OBJECT")
    D = for_ship(load_detail(slug), slug)
    smooth = D["defaults"].get("smooth_deg")   # a rounded hull (the Kelvane's rolled case) is shaded smooth across faces within
    if smooth:                                 # this angle, so its tangent normals are baked against the normals the page draws
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(smooth), keep_sharp_edges=True)
    S = D["ships"][slug]
    t0 = time.time()
    high = HD.build(ship, S, D["defaults"], S["seed"])
    print(f"  built the detail in {time.time() - t0:.0f} s", flush=True)
    print(f"  {slug}: {len(high)} high-poly pieces, {sum(len(o.data.polygons) for o in high)} faces", flush=True)
    panes = [dict(P) for P in HD.PANES]   # the page draws each pane as its own quad (hull_detail.pane_box)
    pads = [dict(P) for P in HD.PADS]     # the page parks a craft on each landing pad (hull_detail.inner_machinery)
    if panes_only:
        return {"panes": panes}
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
    sun.data.energy = 1.0; sun.data.angle = math.radians(D["defaults"].get("sun_angle_deg", 3))   # a broad sun casts no hard shadow (owner, 2026-11-07: "why is there an offset shadow? the blender model shouldve been lit without hard shadows")
    sun.rotation_euler = Vector(SUN).normalized().to_track_quat("Z", "Y").to_euler()
    fixture_lights = []   # the hull's lamps, for the light pass only (named LIGHT_<kind>_<n>, the light-baking skill)
    for i, F in enumerate(HD.FIXTURES):
        kind = "SPOT" if F["cone_deg"] else "POINT"
        L = bpy.data.lights.new(f"LIGHT_{F['kind']}_{i}", kind); L.energy = F["power_w"]; L.color = F["rgb"]
        L.shadow_soft_size = F["size_m"] or 0.25
        if kind == "SPOT":
            L.spot_size = math.radians(F["cone_deg"]); L.spot_blend = 0.35
        o = bpy.data.objects.new(L.name, L); sc.collection.objects.link(o)
        o.location = F["pos"]
        o.rotation_euler = (-Vector(F["dir"])).to_track_quat("Z", "Y").to_euler()
        o.hide_render = True
        fixture_lights.append(o)
    nfaces = sum(len(o.data.polygons) for o in high)
    bpy.ops.object.select_all(action="DESELECT")   # one object to bake from: the bakes' setup is per object
    for o in high:
        o.select_set(True)
    bpy.context.view_layer.objects.active = high[0]
    bpy.ops.object.join()
    high = [high[0]]
    bpy.ops.object.select_all(action="DESELECT")
    high[0].select_set(True)
    ship.select_set(True); bpy.context.view_layer.objects.active = ship
    ship.visible_shadow = ship.visible_diffuse = ship.visible_glossy = False  # the low-poly hull only receives the bake

    cage_m = D["defaults"].get("cage_m", 1.3)   # how far out the bake's rays start: past the tallest detail (the Hunt's 1.3 m)

    def bake(kind, what, **kw):
        t0 = time.time()
        HD.show(what)
        img.pixels.foreach_set(np.zeros(px * px * 4, np.float32))
        bpy.ops.object.bake(type=kind, margin=8, use_selected_to_active=True, cage_extrusion=cage_m, max_ray_distance=cage_m + 1.5, **kw)
        print(f"  bake {kind} {what}: {time.time() - t0:.0f} s", flush=True)
        return img_array(img).copy()
    data = bake("EMIT", "data")
    cover = data[:, :, 3] > 0.5
    glowp = bake("EMIT", "glow")[:, :, :3]
    P = bake("EMIT", "position")[:, :, :3]
    nor = bake("NORMAL", "shade", normal_space="OBJECT")[:, :, :3] * 2.0 - 1.0     # for the paint's own use
    nts = bake("NORMAL", "shade", normal_space="TANGENT")[:, :, :3]                # the normal map, for the game and page
    ao = bake("AO", "shade")[:, :, 0]
    sc.cycles.samples = max(8, samples // 4)
    sunp = bake("DIFFUSE", "shade", pass_filter={"DIRECT"})[:, :, 0]
    # the hull lights: the fixtures alone, and the lamps' and windows' own faces, falling on white plating
    sun.hide_render = True
    for o in fixture_lights:
        o.hide_render = False
    sc.cycles.samples = max(32, samples)
    lightp = bake("DIFFUSE", "light", pass_filter={"DIRECT"})[:, :, :3]
    lightp = np.dstack([blur(lightp[..., c], 1) for c in range(3)])   # the emissive faces' sampling speckle out
    passes = {"data": data, "glowp": glowp, "P": P, "nor": nor, "nts": nts, "ao": ao, "sunp": sunp, "lightp": lightp}
    if lo_maker:   # which maker each texel's part came from: the low-poly's own faces, each maker's material glowing its index
        for m in ship.data.materials:
            mk = m.name[len("lo_hull_"):] if m.name.startswith("lo_hull_") else None
            bsdf = m.node_tree.nodes.get("Principled BSDF")
            bsdf.inputs["Emission Color"].default_value = ((MAKERS.index(mk) if mk else 0) / 8.0, 0.0, 0.0, 1.0)
            bsdf.inputs["Emission Strength"].default_value = 1.0
        bpy.ops.object.select_all(action="DESELECT"); ship.select_set(True); bpy.context.view_layer.objects.active = ship
        img.pixels.foreach_set(np.zeros(px * px * 4, np.float32))
        bpy.ops.object.bake(type="EMIT", margin=8, use_selected_to_active=False)
        passes["maker"] = np.rint(img_array(img)[:, :, 0] * 8.0).astype(np.int8)
        print(f"  makers: {sorted(lo_maker)}", flush=True)
    np.savez_compressed(passes_path(slug), **passes)   # kept (a build intermediate), so --repaint repaints without a bake
    paint_and_write(slug, D, S, passes)
    lref = D["defaults"]["light_ref"]
    # the UV mesh, in ship space, as flat triangle lists
    bm = bmesh.new(); bm.from_mesh(ship.data); bmesh.ops.triangulate(bm, faces=bm.faces[:])
    uvl = bm.loops.layers.uv.active
    pos, uv = [], []
    for f in bm.faces:
        for l in f.loops:
            pos += [round(c, 3) for c in l.vert.co]; uv += [round(l[uvl].uv[0], 5), round(l[uvl].uv[1], 5)]
    bm.free()
    return {"tris": len(pos) // 9, "tex_px": px, "smooth_deg": smooth, "samples": samples, "uv_m": round(ship_uv_m(ship), 3),
            "high_faces": nfaces, "light_ref": lref, "light_gamma": 1.6,
            "fixtures": [{k: F[k] for k in ("kind", "pos", "rgb")} for F in HD.FIXTURES], "panes": panes, "pads": pads, "mesh": {"pos": pos, "uv": uv},
            "sdf": hull_sdf.hull_sdf(pos)}   # the inside its breakup cuts by (lib/ship-break.js)


def passes_path(slug):
    """Where a ship's baked passes are kept (tools/materials/raw/painted/ships/, gitignored): the paint can be redone from
    them in seconds, with no bake (--repaint), so a change to a faction's paint costs no Cycles time."""
    return os.path.join(SHIPS_RAW, f"{FACTION}_{slug}_passes.npz")


def for_ship(D, slug):
    """The faction's data as one ship sees it: its own "defaults" and "paint" blocks laid over the faction's (a craft a
    tenth of a warship's size needs its plating, cells and gaps at its own scale), and the paint set for it (PT)."""
    S = D["ships"][slug]
    PT.clear(); PT.update({**D["paint"], **S.get("paint", {})})
    return {**D, "defaults": {**D["defaults"], **S.get("defaults", {})}}


def paint_and_write(slug, D, S, ps):
    """Paint a ship from its passes (ps) and write its maps: painted colour, AO, emission, tangent normals, PBR base
    colour, ORM and the hull-light map."""
    data, glowp, P, nor, nts, ao, sunp, lightp = (ps[k] for k in ("data", "glowp", "P", "nor", "nts", "ao", "sunp", "lightp"))
    cover = data[:, :, 3] > 0.5
    faction = D.get("faction", FACTION)
    col, emit = paint_ship(data, glowp, P, nor, ao, sunp, cover, S, faction)
    pbr, orm = paint_pbr(data, P, nor, ao, cover, S, faction)
    if "maker" in ps and S.get("makers"):   # a conversion's taken parts in their makers' looks, dulled (S["makers"])
        keep, mk = dict(PT), ps["maker"]
        for name, Z in S["makers"].items():
            m = mk == MAKERS.index(name)
            if not m.any():
                continue
            PT.clear(); PT.update({**keep, **look_paint(Z["look"]), **Z.get("paint", {})})
            c2, e2 = paint_ship(data, glowp, P, nor, ao, sunp, cover, S, faction)
            p2, o2 = paint_pbr(data, P, nor, ao, cover, S, faction)
            d = Z.get("dull", 0.0)
            col[m], emit[m], pbr[m], orm[m] = dull(c2, d)[m], e2[m], dull(p2, d)[m], o2[m]
        PT.clear(); PT.update(keep)
    lref = D["defaults"]["light_ref"]
    lmap = half(np.clip(lightp / lref, 0, 1) ** (1 / 1.6))   # stored with a gentle curve; the page and game undo it
    os.makedirs(ships_out(), exist_ok=True)
    K.save_png(os.path.join(ships_out(), f"{slug}_albedo.png"), col)
    K.save_png(os.path.join(ships_out(), f"{slug}_ao.png"), np.dstack([ao, ao, ao]))
    K.save_png(os.path.join(ships_out(), f"{slug}_emit.png"), emit)
    K.save_png(os.path.join(ships_out(), f"{slug}_normal.png"), np.clip(nts, 0, 1))
    K.save_png(os.path.join(ships_out(), f"{slug}_base.png"), pbr)
    K.save_png(os.path.join(ships_out(), f"{slug}_orm.png"), orm)
    K.save_png(os.path.join(ships_out(), f"{slug}_light.png"), lmap)
    if PT.get("area_mask"):   # which surface a texel is (owner, 2026-11-07: "diffrent detail maps should be used for different
        # "hull area", smooth armor plating layer vs sub hull layer"): red the plating (armour, accent), green the hull
        # beneath it (sub-armour, the under layer, machinery, greebles, vents), from the bake's own kinds, at half size
        _, _, _, is_ = classify(data, cover)
        plate = (is_("armour") | is_("accent")).astype(np.float32)
        under = np.any([is_(k) for k in ("sub", "under", "greeble", "machine", "slat", "vent")], axis=0).astype(np.float32)
        K.save_png(os.path.join(ships_out(), f"{slug}_mask.png"), half(np.dstack([plate, under, np.zeros_like(plate)])))
    print(f"  light map: 99th percentile {np.percentile(lightp[cover].max(axis=1), 99):.3f} against light_ref {lref}", flush=True)


MAKERS = ("none", "vos", "concord", "krai", "ssarith", "kelvane")   # the maker pass's indices (a hull_<maker>_* part)


def look_paint(name):
    """A faction's look by name, the freighters' list of looks (data/ships/freighters/hull_detail.json "looks"), so a taken part
    wears the same look its maker's freighters do (CLAUDE.md 6.1): its paint, from the faction's own file where the look says so."""
    L = json.load(open(os.path.join(ROOT, "data", "ships", "freighters", "hull_detail.json")))["looks"][name]
    if "from" in L:
        return json.load(open(os.path.join(ROOT, "data", "ships", L["from"], "hull_detail.json")))["paint"]
    return L["paint"]


def dull(c, d):
    """A colour dulled by d (0 to 1): toward its own grey, darkened a little and warmed, as paint goes under grime and years."""
    if not d:
        return c
    g = c[..., :3].mean(axis=-1, keepdims=True)
    out = c.copy()
    out[..., :3] = c[..., :3] * (1 - d) + (g * 0.62 + np.array([0.04, 0.025, 0.01], np.float32)) * d
    return out


def repaint_main(slugs):
    """Repaint ships from their kept passes (no bake): the maps and their hashes in the manifest; the mesh, fixtures and
    field are the bake's and stay."""
    mpath = os.path.join(ships_out(), "manifest.json")
    man = json.load(open(mpath))
    for slug in slugs:
        D = load_detail(slug)
        PT.clear(); PT.update(D["paint"])
        ps = dict(np.load(passes_path(slug)))
        Ds = for_ship(D, slug)
        paint_and_write(slug, Ds, Ds["ships"][slug], ps)
        for m in [m for m in MAPS if m != "mask" or PT.get("area_mask")]:
            f = f"{slug}_{m}.png"
            man["pieces" if m != "ao" else "kept"][f"{slug}_{m}"] = {"file": f, "sha256": hashlib.sha256(open(os.path.join(ships_out(), f), "rb").read()).hexdigest()}
        print(f"repainted {slug}", flush=True)
    with open(mpath, "w") as fh:
        json.dump(man, fh, separators=(",", ":"), sort_keys=True)
        fh.write("\n")


EMBLEMS = os.path.join(ROOT, "assets", "emblems")
MAPS = ("albedo", "ao", "emit", "normal", "base", "orm", "light", "mask")   # "mask" only where the paint has area_mask
# The paint and the PBR by what a texel is (hull_detail.KIND) are the faction's, in its hull_detail.json "paint": the
# armour's palette (by plate id) and its accent, the sub-armour's, the under layer, greebles, domes, slats, vents, glass,
# liveries, the ink in the deepest cavities, the lit edges, how a light strip is lit ("centre": a lit line strip_m wide on
# x = 0, down a keel; "whole": the strip face itself), and "pbr": (roughness, metalness) by kind, "livery_pbr" for paint.


def classify(data, cover):
    """Each texel's kind (hull_detail.KIND), its piece id and its edge amount, from the data bake."""
    from hull_detail import KIND
    codes = np.array(list(KIND.values())); names = list(KIND.keys())
    kind = np.array(names, dtype=object)[np.abs(data[:, :, 0][..., None] - codes).argmin(axis=2)]
    return kind, data[:, :, 1], data[:, :, 2], (lambda k: (kind == k) & cover)


def logo_mask(S, faction, P, n, plated):
    """Where the faction's emblem is painted (S['logo']): its field and device masks, projected from above onto the
    top-facing armour (assets/emblems/<faction>/hull_mask.png, drawn from docs/mockups/lib/emblems.js)."""
    lg = S.get("logo")
    h, w = P.shape[:2]
    none = np.zeros((h, w), bool)
    if not lg:
        return none, none, none, None
    path = os.path.join(EMBLEMS, faction, "hull_mask.png")
    img = bpy.data.images.load(path, check_existing=True)
    mw, mh = img.size
    m = np.empty(mw * mh * 4, np.float32); img.pixels.foreach_get(m); m = m.reshape(mh, mw, 4)   # bottom row first
    cx, cz = lg["at"]; size = lg["size_m"]
    u = 0.5 - (P[..., 0] - cx) / size            # seen from above with the bow up the image, port (+x) is on the left
    v = 0.5 + (P[..., 2] - cz) / size            # bottom-up, like Blender's rows: the bow at the top of the mark
    inside = (u >= 0) & (u < 1) & (v >= 0) & (v < 1) & plated & (n[..., 1] > 0.35)
    px_ = np.clip((u * mw).astype(int), 0, mw - 1); py_ = np.clip((v * mh).astype(int), 0, mh - 1)
    s = m[py_, px_]
    field = inside & (s[..., 3] > 0.5) & (s[..., 0] > 0.5)
    device = inside & (s[..., 3] > 0.5) & (s[..., 1] > 0.5)
    hull = json.load(open(os.path.join(EMBLEMS, faction, "hull_mask.json")))["hull"]
    return inside, field, device, hull


def flat_paint(kind_is, rid, P, n, cover, S, faction):
    """The paint itself, before any light: each texel's colour by its kind (armour from the palette by its plate's id,
    sub-armour greyer, the under layer near black, greebles, the dome's green, slats, vents), the liveries and the
    emblem. The painted map and the PBR base colour both start here. Returns (colour, painted-on mask)."""
    h, w = cover.shape
    ARMOUR, SUB = PT["armour"], PT["sub"]
    pal = np.stack([hexrgb(c) for c in ARMOUR])
    arm = pal[(rid * len(ARMOUR) * 11).astype(int) % len(ARMOUR)] * (0.9 + 0.2 * ((rid * 37.0) % 1.0))[..., None]
    arm = np.where((rid > 1.0 - PT.get("accent_share", 0.06))[..., None], hexrgb(PT["accent"]), arm)   # a share of plates in the accent
    base = np.zeros((h, w, 3), np.float32) + hexrgb(PT["under"])
    base = np.where(kind_is("armour")[..., None], arm, base)
    spal = np.stack([hexrgb(c) for c in SUB])
    base = np.where(kind_is("sub")[..., None], spal[(rid * len(SUB) * 7).astype(int) % len(SUB)], base)
    g0, g1 = hexrgb(PT["greeble"][0]), hexrgb(PT["greeble"][1])
    base = np.where(kind_is("greeble")[..., None], g0 + (g1 - g0) * ((rid * 13.0) % 1.0)[..., None], base)
    m0, m1 = (hexrgb(c) for c in PT.get("machine", PT["greeble"]))   # the machines' tanks, coils and pads (the greebles' unless given)
    base = np.where(kind_is("machine")[..., None], m0 + (m1 - m0) * ((rid * 7.0) % 1.0)[..., None], base)
    for k in ("dome", "slat", "vent", "glass"):
        base = np.where(kind_is(k)[..., None], hexrgb(PT[k]), base)
    liv = np.zeros((h, w), bool)
    for L in S.get("liveries", []):
        if "band_z_m" in L:
            z0, z1 = L["band_z_m"]; liv |= (P[..., 2] > z0) & (P[..., 2] < z1)
        if "chevron" in L:
            c = L["chevron"]; liv |= np.abs(P[..., 2] - (c["z_m"] - c["sweep"] * np.abs(P[..., 0]))) < c["w_m"] / 2
    liv &= kind_is("armour")
    base = np.where(liv[..., None], hexrgb(PT["livery"]) * (0.92 + 0.16 * rid)[..., None], base)
    # an engraved measuring scale (the Kelvane: the Institute marks every surface it works on): ticks every tick_m along
    # the ship in a band of the flank, a long tick every major_m, inked into the plating
    for L in S.get("liveries", []):
        if "scale" in L:
            c = L["scale"]; y0, y1 = c["y_m"]; z, y = P[..., 2], P[..., 1]; w2 = c["w_m"] / 2
            za, zb = c.get("z_m", [-1e9, 1e9])
            near = lambda step: np.abs(((z / step) + 0.5) % 1.0 - 0.5) * step < w2   # within w2 of a multiple of step
            top = np.where(near(c["major_m"]), y1, y0 + (y1 - y0) * 0.5)          # a long tick every major_m
            tick = (near(c["tick_m"]) & (y > y0) & (y < top)) | (np.abs(y - y0) < w2)   # the ticks and their base line
            tick &= (z > za) & (z < zb) & (np.abs(n[..., 0]) > 0.5) & (kind_is("armour") | kind_is("sub"))
            base = np.where(tick[..., None], hexrgb(PT["ink"]) * 1.4, base)
            liv |= tick
    if PT.get("plate_marks"):
        base, marked = plate_marks(base, P, n, rid, kind_is("armour"), PT["plate_marks"])
        liv |= marked
    patch, field, device, hull = logo_mask(S, faction, P, n, kind_is("armour") | kind_is("sub"))
    if hull:   # the emblem's own hull colours: its plate painted as a patch, the field and device on it
        base = np.where(patch[..., None], hexrgb(hull["plate"]), base)
        base = np.where(field[..., None], hexrgb(hull["field"]), base)
        base = np.where(device[..., None], hexrgb(hull["device"]), base)
    return base, liv | patch


def grain(h, w, seed=17):
    """Brush grain: streaks along the plates and a coarse mottle, as a multiplier about 1."""
    rng = np.random.default_rng(seed)
    streak = blur(rng.standard_normal((h, w)).astype(np.float32), 2)
    sx = sum(np.roll(streak, d, axis=1) for d in range(-10, 11)) / 21.0
    mott = blur(rng.standard_normal((h // 8 + 1, w // 8 + 1)).astype(np.float32), 1)
    mott = np.kron(mott, np.ones((8, 8), np.float32))[:h, :w]
    return 1.0 + 0.07 * sx / (np.abs(sx).max() + 1e-6) + 0.06 * mott / (np.abs(mott).max() + 1e-6)


def cell_hash(i, j, k, seed=0):
    """A well-mixed 64-bit hash of three integer arrays (SplitMix64's finaliser, CLAUDE.md section 10: never a product
    modulo a constant), as floats in [0, 1)."""
    with np.errstate(over="ignore"):
        h = (i.astype(np.int64).astype(np.uint64) * np.uint64(0x9E3779B97F4A7C15)
             ^ j.astype(np.int64).astype(np.uint64) * np.uint64(0xC2B2AE3D27D4EB4F)
             ^ k.astype(np.int64).astype(np.uint64) * np.uint64(0x165667B19E3779F9) ^ np.uint64(seed))
        h = (h ^ (h >> np.uint64(30))) * np.uint64(0xBF58476D1CE4E5B9)
        h = (h ^ (h >> np.uint64(27))) * np.uint64(0x94D049BB133111EB)
        h = h ^ (h >> np.uint64(31))
    return (h >> np.uint64(11)).astype(np.float64) / float(1 << 53)


def detector_cells(P, n, cell_m=None):
    """The middle of a wide lit face as an instrument's array (owner, 2026-11-06, of a Kelvane nose aperture: "missing
    texture on the nose"): square cells strip_cell_m apart on the face's own plane, a dark gap round each, about
    strip_cell_on of them lit, each at its own level. Mirrored (|x|), so port and starboard match. Returns the glow level."""
    c, gap = cell_m or PT["strip_cell_m"], PT.get("strip_cell_gap", 0.14)
    ax = np.abs(n).argmax(axis=2)
    x = np.abs(P[..., 0])
    u = np.where(ax == 0, P[..., 2], x) / c
    v = np.where(ax == 1, P[..., 2], P[..., 1]) / c
    iu, iv = np.floor(u), np.floor(v)
    fu, fv = u - iu, v - iv
    sq = (fu > gap) & (fu < 1 - gap) & (fv > gap) & (fv < 1 - gap)
    h = cell_hash(iu, iv, ax, 0x6B)
    on = h < PT.get("strip_cell_on", 0.7)
    lvl = 0.45 + 0.55 * cell_hash(iu, iv, ax, 0x1F)
    return np.where(sq & on, lvl, 0.0).astype(np.float32)


def plate_marks(base, P, n, rid, plate, M):
    """Each armour plate as a panel within a panel, with the small marks a worked hull carries (owner, 2026-11-06, "some
    hull armor examples", "details idea"): a groove inset M['groove_inset_m'] from every plate edge, and, on some plates
    (by the plate's id, so a plate and its mirror match), a small red triangle near one corner, an outlined stencil box
    near another, and a short row of small squares. Each mark is placed in the plate's own plane (the axes of its
    facing, |x| for port and starboard alike) from the plate's extent there. Returns (colour, marked mask)."""
    h, w = plate.shape
    d = np.linalg.norm(np.diff(P, axis=1), axis=2)[:, :-1][plate[:, 1:-1]] if plate.any() else np.array([0.1])
    texel = float(np.median(d[(d > 1e-4) & (d < 1.0)])) if d.size else 0.1   # metres per texel on the plating
    q = np.round(rid * 4096).astype(np.int64)
    edge = np.zeros_like(plate)
    for ax in (0, 1):
        for sh in (1, -1):
            edge |= plate & ((np.roll(q, sh, axis=ax) != q) | ~np.roll(plate, sh, axis=ax))
    r1 = max(1, int(round(M.get("groove_inset_m", 0.3) / texel)))
    near = lambda r: blur(edge.astype(np.float32), r) > 1e-6
    groove = plate & near(r1 + max(1, int(round(M.get("groove_w_m", 0.08) / texel)))) & ~near(r1)
    if M.get("groove", True):   # the inset line (off where the owner asked: "I'd probably get rid of the inner lines on the panel")
        base = np.where(groove[..., None], base * M.get("groove_k", 0.62), base)
    # the plate's own plane: (along, across) by its facing, the same on both sides
    ax = np.abs(n).argmax(axis=2); x = np.abs(P[..., 0])
    a = np.where(ax == 0, P[..., 2], x); b = np.where(ax == 1, P[..., 2], P[..., 1])
    lab = np.where(plate, q, -1); ids, inv = np.unique(lab, return_inverse=True); inv = inv.reshape(lab.shape)
    big = 1e9
    amin = np.full(len(ids), big); amax = np.full(len(ids), -big); bmin = np.full(len(ids), big); bmax = np.full(len(ids), -big)
    np.minimum.at(amin, inv[plate], a[plate]); np.maximum.at(amax, inv[plate], a[plate])
    np.minimum.at(bmin, inv[plate], b[plate]); np.maximum.at(bmax, inv[plate], b[plate])
    zero = np.zeros(len(ids), np.int64)
    hsh = lambda salt: cell_hash(ids, zero, zero, salt)[inv]       # one draw a plate, by its id
    A0, A1, B0, B1 = amin[inv], amax[inv], bmin[inv], bmax[inv]
    room = ((A1 - A0) > 3.0) & ((B1 - B0) > 2.0) & plate               # a plate big enough to carry marks
    ins = r1 * texel + 0.35                                            # inside the groove
    marked = np.zeros_like(plate)
    red = hexrgb(M.get("red", "#9a1c1c")); ink = hexrgb(M.get("ink", "#3a3022")); dots = [hexrgb(c) for c in M.get("dots", ["#9a1c1c", "#8a6a3a", "#3a3022"])]
    # a small red triangle, pointing in, near the plate's top corner
    tri = room & (hsh(0x11) < M.get("triangle_share", 0.45))
    da, db = a - (A0 + ins + 0.45), (B1 - ins) - b
    tri &= (db > 0) & (db < 0.7) & (np.abs(da) < 0.42 * (1 - db / 0.7))
    base = np.where(tri[..., None], red, base); marked |= tri
    # an outlined stencil box near the opposite corner
    fr = room & (hsh(0x23) < M.get("frame_share", 0.4))
    ua, vb = (A1 - ins) - a, b - (B0 + ins)
    box = (ua > 0) & (ua < 1.8) & (vb > 0) & (vb < 0.8)
    lw = max(texel * 1.2, 0.06)
    inner = (ua > lw) & (ua < 1.8 - lw) & (vb > lw) & (vb < 0.8 - lw)
    fr &= box & ~inner
    base = np.where(fr[..., None], ink, base); marked |= fr
    # a short row of small squares along the bottom edge, middle of the plate
    sq = room & (hsh(0x37) < M.get("dots_share", 0.3))
    k = np.floor((a - (A0 + A1) / 2) / 0.55 + 2)
    fa = (a - (A0 + A1) / 2) / 0.55 + 2 - k
    sq &= (k >= 0) & (k < 4) & (fa > 0.2) & (fa < 0.8) & ((b - (B0 + ins)) > 1.0) & ((b - (B0 + ins)) < 1.35)
    pick = (hsh(0x41) * 3).astype(int)
    for i, c in enumerate(dots):
        m = sq & (((pick + k.astype(int)) % len(dots)) == i)
        base = np.where(m[..., None], c, base)
    marked |= sq
    return base, marked


def strips_and_glow(kind_is, P, glowp, cover, n=None):
    """The light strips: a dark channel with a lit line strip_m wide down the middle (paint "strip": "centre"), or the strip
    face lit whole ("whole"). Lit whole, a wide face (a loop's inner wall) burns brightest along its rims and dims to
    strip_inner across its middle, the rim's width set by strip_rim_px, so a wall reads as a lit aperture with form rather
    than a flat fill; a narrow strip is all rim and burns whole. Returns (channel mask, the glow pass with the channel's sides put out, the emission
    map: lamps, windows and lines, each with a soft halo)."""
    chan = kind_is("strip")
    line = chan & (np.abs(P[..., 0]) < PT["strip_m"] / 2) if PT["strip"] == "centre" else chan
    side = chan & ~line
    g = np.where(side[..., None], 0.0, np.clip(glowp, 0, 1)) * cover[..., None]
    g = g * np.where(chan, PT.get("strip_k", 1.0), 1.0)[..., None]   # how bright a strip glows (a wide wall lit whole wants less)
    if PT["strip"] == "whole":
        r = PT.get("strip_rim_px", 6)
        b2 = lambda a: blur(blur(a.astype(np.float32), r), r)
        mach = np.any([kind_is(k) for k in PT.get("machine_kinds", ["greeble"])], axis=0)
        m = b2(chan | mach)   # 1 deep inside a wide face (machinery on it counts as the face), falling to its edges
        rim = np.clip((0.95 - m) / 0.3, 0, 1)
        if "strip_rim_k" in PT:   # how bright a wide face's rim burns (the Institute: not at all; owner, 2026-11-07, of
            # a loop wall's lit edge: "i dont like this light line, get rid"); a narrow strip, far from any wide face, keeps its light
            wide = blur(blur((m > 0.95).astype(np.float32), 2 * r), 2 * r) > 1e-3
            rim = np.where(wide, rim * PT["strip_rim_k"], rim)
            edge = wide & ~chan & ~kind_is("lamp")   # the face's own edge texels, blended with the hull beside it
            g = g * np.where(edge, PT["strip_rim_k"], 1.0)[..., None]
        inner = PT.get("strip_inner", 1.0)
        k = inner + (1 - inner) * rim
        if PT.get("strip_cell_m") and n is not None:   # the open middle an instrument's array, not blank glass
            open_ = np.clip((b2(chan) - 0.75) / 0.2, 0, 1)   # not in the gaps between machinery, where it reads as noise
            k = k + (1 - rim) * open_ * detector_cells(P, n) * PT.get("strip_cell_k", 0.6)
        g = g * np.where(chan, k, 1.0)[..., None]
    g = g + kind_is("dome")[..., None] * hexrgb(PT["dome"]) * PT["dome_glow"]   # the domes' faint inner light
    emit = np.clip(g * 1.2 + np.dstack([blur(g[..., c], 2) for c in range(3)]) * 0.8, 0, 1)
    return side, g, emit


def paint_ship(data, glowp, P, nor, ao, sunp, cover, S, faction):
    """The painted colour map and the emission map. The flat paint (flat_paint), lit as a painter lights it: the sun's
    direct light with its cast shadows in soft steps, the AO curved and inked where it is deepest, each plate pillowed
    darker toward its gaps, lit and shaded edges, the dome's highlight, brush grain, and the lamps over it all."""
    kind, rid, edge, is_ = classify(data, cover)
    h, w = cover.shape
    n = nor / np.maximum(np.linalg.norm(nor, axis=2, keepdims=True), 1e-6)
    base, _ = flat_paint(is_, rid, P, n, cover, S, faction)
    sh = np.clip(sunp / max(np.percentile(sunp[cover], 99), 1e-6), 0, 1)
    sh = blur(sh, 1)
    st = np.floor(sh * 4 + 0.5) / 4
    aoc = np.clip(blur(ao, 1), 0, 1)
    if PT.get("greeble_ao_floor"):   # machinery deep in an opening still reads, as if lit by the work lamps inside it
        mach = np.any([is_(k) for k in PT.get("machine_kinds", ["greeble"])], axis=0)
        aoc = np.where(mach, np.maximum(aoc, PT["greeble_ao_floor"]), aoc)
    light = 0.12 + 0.26 * aoc ** 1.4 + 0.7 * (0.55 * sh + 0.45 * st)
    if PT.get("machine_light"):   # machinery deep in an opening, out of the sun, lit by the opening's own work lamps
        mach = np.any([is_(k) for k in PT.get("machine_kinds", ["greeble"])], axis=0)
        light = np.where(mach, np.maximum(light, PT["machine_light"] * (0.75 + 0.25 * st)), light)
    col = base * light[..., None]
    af = PT.get("ao_floor", 0.22)   # how dark the deepest cavity goes (a plate's rim shadow: owner, of the Krai, "the darker color is too tark")
    col *= (af + (1 - af) * aoc ** 1.7)[..., None]
    ink = np.clip((0.4 - aoc) / 0.25, 0, 1)
    ik = PT.get("ink_k", 0.75)   # how hard the deepest crevices are inked (the Hunt 0.75; a lighter hand for a clean faction)
    col = col * (1 - ik * ink[..., None]) + hexrgb(PT["ink"]) * (ik * ink[..., None])
    m = (is_("armour") | is_("sub")).astype(np.float32)
    pb = blur(blur(m, 4), 4)
    col *= np.where(m > 0, 0.7 + 0.3 * np.clip((pb - 0.45) / 0.5, 0, 1), 1.0)[..., None]
    e = np.clip(edge * 5.0, 0, 1)
    lit = e * np.clip((sh - 0.45) / 0.35, 0, 1); dark = e * np.clip((0.5 - sh) / 0.4, 0, 1)
    col = col * (1 - 0.55 * lit[..., None]) + hexrgb(PT["light_edge"]) * (0.55 * lit[..., None])
    col *= (1 - 0.45 * dark)[..., None]
    Lv = np.array(SUN, np.float32); Lv /= np.linalg.norm(Lv)
    Hv = Lv + np.array([0.0, 0.25, 1.0], np.float32); Hv /= np.linalg.norm(Hv)
    spec = np.clip((n * Hv).sum(axis=2), 0, 1) ** 28 * is_("dome")
    col = col + (np.array([0.75, 0.95, 0.8]) * spec[..., None] * 0.7)
    col *= grain(h, w)[..., None]
    side, g, emit = strips_and_glow(is_, P, glowp, cover, n)
    if PT.get("glass_cells_k"):   # glass that looks ahead (a sensor nose): an instrument's array glowing dimly behind it
        fwd = is_("glass") & (np.abs(n[..., 2]) > 0.7)
        rim = np.clip((0.95 - blur(blur(fwd.astype(np.float32), 6), 6)) / 0.3, 0, 1)
        gc = (detector_cells(P, n, PT.get("glass_cell_m")) * (1 - rim) * fwd * PT["glass_cells_k"])[..., None] * np.array(PT.get("glass_cells_rgb", [1.0, 0.78, 0.4]), np.float32)
        emit = np.clip(emit + gc, 0, 1)
        g = g + gc
    col = np.where(side[..., None], hexrgb(PT["vent"]) * (0.6 + 0.6 * sh)[..., None], col)
    if PT["strip"] == "whole":   # a wall lit whole: its own shaded paint with the glow over it, not the glow's flat colour
        col = np.where(is_("strip")[..., None], np.clip(col + g * 0.6, 0, 1), col)
        col = np.where((~is_("strip") & (g.max(axis=2) > 0.05))[..., None], np.maximum(col, g), col)
    else:
        col = np.where((g.max(axis=2) > 0.05)[..., None], np.maximum(col, g), col)
    return np.clip(col, 0, 1), emit


def paint_pbr(data, P, nor, ao, cover, S, faction):
    """The PBR set: a base colour with no light in it (the flat paint, darkened in its cavities, its plates a little
    darker toward their gaps, edges worn lighter, grain), and ORM (red the AO, green the roughness, blue the metalness,
    by kind; worn edges smoother and barer, paint less metallic)."""
    kind, rid, edge, is_ = classify(data, cover)
    h, w = cover.shape
    n = nor / np.maximum(np.linalg.norm(nor, axis=2, keepdims=True), 1e-6)
    base, painted = flat_paint(is_, rid, P, n, cover, S, faction)
    aoc = np.clip(blur(ao, 1), 0, 1)
    col = base * (0.45 + 0.55 * aoc ** 1.2)[..., None]                 # cavities keep their dark: the paint's own depth
    m = (is_("armour") | is_("sub")).astype(np.float32)
    pb = blur(blur(m, 4), 4)
    col *= np.where(m > 0, 0.82 + 0.18 * np.clip((pb - 0.45) / 0.5, 0, 1), 1.0)[..., None]
    e = np.clip(edge * 5.0, 0, 1) * (m > 0)
    col = col * (1 - 0.3 * e[..., None]) + hexrgb(PT["light_edge"]) * (0.3 * e[..., None])
    gr = grain(h, w)
    col *= gr[..., None]
    PBR = PT["pbr"]
    rough = np.full((h, w), PBR["under"][0], np.float32); metal = np.full((h, w), PBR["under"][1], np.float32)
    for k, (r, mm) in PBR.items():
        rough = np.where(is_(k), r, rough); metal = np.where(is_(k), mm, metal)
    rough = np.where(painted, PT["livery_pbr"][0], rough); metal = np.where(painted, PT["livery_pbr"][1], metal)
    rough = np.clip(rough - 0.22 * e + 0.6 * (gr - 1.0), 0.05, 1.0)
    metal = np.clip(metal + 0.35 * e, 0.0, 1.0)
    orm = np.dstack([aoc, rough, metal])
    side, _, _ = strips_and_glow(is_, P, np.zeros_like(P), cover)
    col = np.where(side[..., None], hexrgb(PT["vent"]), col)
    return np.clip(col, 0, 1), orm


def half(a):
    """Half the size, by 2 x 2 means (the light map is smooth)."""
    h, w = a.shape[0] // 2 * 2, a.shape[1] // 2 * 2
    a = a[:h, :w]
    return (a[0::2, 0::2] + a[1::2, 0::2] + a[0::2, 1::2] + a[1::2, 1::2]) / 4


def ship_main(slugs, px, samples, panes_only=False):
    mpath = os.path.join(ships_out(), "manifest.json")
    man = json.load(open(mpath)) if os.path.exists(mpath) else {"pieces": {}, "ships": {}}
    man.setdefault("kept", {})
    for k in [k for k in man["pieces"] if k.endswith("_ao")]:
        del man["pieces"][k]
    man["_doc"] = (load_detail().get("manifest_doc") or "") + (
        " Each unwrapped and baked onto its own UVs by tools/blender/painted_bake.py --ship (never edited by hand): the UV mesh "
        "in ship space (+x port, +y up, +z bow, metres) and its painted colour, AO and glow maps. uv_m is metres of hull per UV unit.")
    for k in [k for k in man["pieces"] if k.endswith("_pane")]:   # panes are drawn from their data now, not a map
        del man["pieces"][k]
    for slug in slugs:
        PT.clear(); PT.update(load_detail(slug)["paint"])   # each ship's own faction's paint (a freighter's look)
        if panes_only:
            man["ships"][slug]["panes"] = bake_ship(slug, px, samples, True)["panes"]
            print(f"panes for {slug}: {len(man['ships'][slug]['panes'])}", flush=True)
            continue
        info = bake_ship(slug, px, samples)
        n = int(open(os.path.join(SHIPS_RAW, mesh_of(slug) + ".obj")).readline().split()[1])
        info["n"] = n
        man["ships"][slug] = info
        for m in [m for m in MAPS if m != "mask" or PT.get("area_mask")]:   # the AO is kept beside, not inlined (it is in the ORM)
            f = f"{slug}_{m}.png"
            man["pieces" if m != "ao" else "kept"][f"{slug}_{m}"] = {"file": f, "sha256": hashlib.sha256(open(os.path.join(ships_out(), f), "rb").read()).hexdigest()}
        print(f"baked ship {slug}: {info['tris']} triangles, {info['uv_m']} m per uv unit", flush=True)
    man["blender"] = bpy.app.version_string
    man["webp_quality"] = 84   # the page's copy of the maps (tools/mockups/inline_data.py); the PNGs here stay exact
    D_ = json.load(open(detail_path()))
    if D_.get("maps_dir"):   # the page's maps written beside it as files (inline_data.py), for a page of many ships
        man["maps_dir"] = D_["maps_dir"]
    with open(mpath, "w") as fh:
        json.dump(man, fh, separators=(",", ":"), sort_keys=True)
        fh.write("\n")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    arg = lambda k, d: argv[argv.index(k) + 1] if k in argv else d
    if "--ship" in argv:  # a picked ship of its own: --ship hooded_lance,forked_prow [--px 2048] [--samples 64] [--panes-only] [--faction ssarith] [--repaint]
        global FACTION
        FACTION = arg("--faction", "ssarith")
        if "--repaint" in argv:
            return repaint_main(arg("--ship", "").split(","))
        return ship_main(arg("--ship", "").split(","), int(arg("--px", "2048")), int(arg("--samples", "64")), "--panes-only" in argv)
    names = arg("--tile", ",".join(TILES)).split(",")
    samples, ppm = int(arg("--samples", "32")), float(arg("--ppm", "128"))
    os.makedirs(OUT, exist_ok=True); os.makedirs(RAW, exist_ok=True)
    mpath = os.path.join(OUT, "manifest.json")
    man = json.load(open(mpath)) if os.path.exists(mpath) else {"tiles": {}, "pieces": {}}
    man["_doc"] = ("Painted bake tiles, written by tools/blender/painted_bake.py (never edited by hand): a high-poly tile's "
                   "passes painted over so it reads drawn on. u along the ship (x), v across the face (y), metres.")
    for name in names:
        build, w, h = TILES[name]
        info = bake(name, w, h, ppm, samples)
        man["tiles"][name] = info
        for m in ("albedo", "normal", "emit"):
            f = f"{name}_{m}.png"
            man["pieces"][f"{name}_{m}"] = {"file": f, "sha256": hashlib.sha256(open(os.path.join(OUT, f), "rb").read()).hexdigest()}
        print(f"baked {name}: {info}", flush=True)
    man["blender"] = bpy.app.version_string
    with open(mpath, "w") as fh:
        json.dump(man, fh, indent=2, sort_keys=True)
        fh.write("\n")


if __name__ == "__main__":
    main()
