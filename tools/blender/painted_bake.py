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

Writes assets/textures/painted/ and its manifest.json (sizes in metres, sha256 of every file).
"""

import hashlib
import json
import math
import os
import random
import sys

import bpy
import numpy as np

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
# "tile textures are for DETAIL only", "dont forget to bake AO as well". A picked blockout comes from its page as OBJ
# (tools/mockups/export_blockouts.mjs, ship space: +x port, +y up, +z bow, imported as it is). It is joined into one mesh,
# every piece keeping a random id and a glow flag in a colour attribute, unwrapped once (Smart UV Project), and baked onto
# its own UVs: AO (reaching 1.5 m), object space normals, and one data pass (red the piece's id, green its edges, blue its
# glow). The same paint-over as a tile turns them into the ship's own colour map, with ink along the UV island borders (the
# seams fall where the faces turn, so they read as panel lines that follow the form), and the UV mesh is written out.
SHIPS_RAW = os.path.join(RAW, "ships")
SHIPS_OUT = os.path.join(ROOT, "assets", "ships", "ssarith")


def ship_material(img):
    """The one bake material: an emission whose colour is the data pass (red the piece's id, green its edges from a
    Bevel node, blue its glow flag), or, relinked by the returned `show` function, the surface position in ship space."""
    m = bpy.data.materials.new("ship_bake")
    m.use_nodes = True
    N, L = m.node_tree.nodes, m.node_tree.links
    for n in list(N):
        N.remove(n)
    out = N.new("ShaderNodeOutputMaterial")
    attr = N.new("ShaderNodeAttribute"); attr.attribute_name = "rid"
    sep = N.new("ShaderNodeSeparateColor"); L.new(attr.outputs["Color"], sep.inputs[0])
    geo = N.new("ShaderNodeNewGeometry")
    bev = N.new("ShaderNodeBevel"); bev.samples = 8; bev.inputs["Radius"].default_value = 0.3
    dot = N.new("ShaderNodeVectorMath"); dot.operation = "DOT_PRODUCT"
    L.new(bev.outputs[0], dot.inputs[0]); L.new(geo.outputs["Normal"], dot.inputs[1])
    inv = N.new("ShaderNodeMath"); inv.operation = "SUBTRACT"; inv.inputs[0].default_value = 1.0
    L.new(dot.outputs["Value"], inv.inputs[1])
    comb = N.new("ShaderNodeCombineColor")
    L.new(sep.outputs[0], comb.inputs[0]); L.new(inv.outputs[0], comb.inputs[1]); L.new(sep.outputs[1], comb.inputs[2])
    em = N.new("ShaderNodeEmission"); L.new(em.outputs[0], out.inputs[0])
    tex = N.new("ShaderNodeTexImage"); tex.image = img
    N.active = tex
    src = {"data": comb.outputs[0], "position": geo.outputs["Position"]}
    show = lambda what: L.new(src[what], em.inputs[0])
    show("data")
    return m, show


# The hull's own detail, laid in ship space over its unwrap, so it follows the form and never repeats as a tile. Plates in
# courses (long along the ship, PLATE_M), each course with its own plate length and offset so the joints never line up
# into brickwork, each plate a little lighter or darker; a groove between plates with a lit or
# shaded lip by which way the plate's edge faces the light; forward chevrons along the dorsal spine; rows of lit ports on
# the flanks. Every number is metres of hull.
PLATE_M = (11.0, 4.5)         # a plate's mean length along the ship, and a course's height across the face
CHEVRON = "#a3743d"           # the dorsal chevrons' ochre
PORT = "#ffe1a6"              # a lit port's warm white
PORT_M = (0.7, 0.28, 1.7)     # a port's length and height, and the spacing along the row


def hash01(*k):
    x = sum(np.asarray(a, np.float64) * c for a, c in zip(k, (12.9898, 78.233, 37.719, 19.311)))
    return (np.sin(x) * 43758.5453) % 1.0


def ship_courses(P, n):
    """Each texel's place in the plating: (plate id 0..1, distance to the plate's border in m, the border's outward
    direction in ship space, the face kind 0 top/bottom 1 flank 2 bow/stern, the course number)."""
    a = np.abs(n)
    top = (a[..., 1] >= a[..., 0]) & (a[..., 1] >= a[..., 2])
    flank = ~top & (a[..., 0] >= a[..., 2])
    kind = np.where(top, 0, np.where(flank, 1, 2))
    sgn = np.where(top, np.sign(n[..., 1]), np.where(flank, np.sign(n[..., 0]), np.sign(n[..., 2])))
    u = np.where(kind == 2, P[..., 0], P[..., 2])                # along the ship where the face allows
    v = np.where(kind == 0, P[..., 0], P[..., 1])                # across the face
    eu = np.where((kind == 2)[..., None], [1.0, 0, 0], [0, 0, 1.0])
    ev = np.where((kind == 0)[..., None], [1.0, 0, 0], [0, 1.0, 0])
    pu, pv = PLATE_M
    row = np.floor(v / pv)
    plen = pu * (0.6 + 0.8 * hash01(row, kind, 3.0))           # this course's plate length
    au = u / plen + hash01(row, kind, 7.0)
    ci = np.floor(au)
    fu, fv = (au - ci) * plen, (v / pv - row) * pv
    ds = np.stack([fu, plen - fu, fv, pv - fv])
    near = ds.argmin(axis=0)
    d = ds.min(axis=0)
    out = np.select([(near == 0)[..., None], (near == 1)[..., None], (near == 2)[..., None]], [-eu, eu, -ev], ev)
    pid = hash01(ci, row, kind * 2 + (sgn > 0))
    return pid, d, out, kind, row


def ship_detail(P, n, ao, rid, cover, texel_m, light):
    """The ship's own base paint (plates and markings) and, after the paint-over, its grooves, lips and ports.
    Returns (base, finish) where finish(col, glow) draws the grooves and ports onto the painted map."""
    pid, d, out, kind, row = ship_courses(P, n)
    base = piece_paint(rid) * (0.86 + 0.24 * pid)[..., None]
    base = np.where(((pid > 0.9) & (kind != 2))[..., None], base * 0.78 + hexrgb(BRONZE) * 0.22, base)
    # forward chevrons on the dorsal faces, in two bands, the first two fifths and the middle of the ship
    zs = P[..., 2][cover]
    z0, z1 = float(zs.min()), float(zs.max()); ln = z1 - z0
    zt = (P[..., 2] - z0) / ln
    chev = ((P[..., 2] + 1.6 * np.abs(P[..., 0])) / 6.0) % 1.0 < 0.4
    band = ((zt > 0.58) & (zt < 0.74)) | ((zt > 0.30) & (zt < 0.38))
    dorsal = (n[..., 1] > 0.55) & (np.abs(P[..., 0]) < 0.08 * ln)
    mark = chev & band & dorsal & (d > 0.25)
    base = np.where(mark[..., None], base * 0.12 + hexrgb(CHEVRON) * 0.88, base)
    L = np.array(light, np.float64); L /= np.linalg.norm(L)
    facing = (out * L).sum(axis=2)
    gw = max(0.09, 1.1 * texel_m)                                # the groove, at least a texel wide
    groove = d < gw
    lip = (d >= gw) & (d < gw + max(0.12, 1.2 * texel_m))
    # lit ports on the flanks, in every other course, on plates the hash picks, away from the borders and the shadows
    pl, ph, sp = PORT_M
    fz = (P[..., 2] / sp) % 1.0
    fy = (P[..., 1] / PLATE_M[1]) % 1.0
    port = (kind == 1) & (row % 2 == 0) & (pid < 0.35) & (fz < pl / sp) & (np.abs(fy - 0.5) < ph / PLATE_M[1] / 2) \
        & (d > 0.35) & (ao > 0.8)

    def finish(col, glow):
        col = np.where(groove[..., None], col * 0.38, col)
        col = np.where((lip & (facing > 0.15))[..., None], col * 0.7 + hexrgb(LIGHT_EDGE) * 0.3, col)
        col = np.where((lip & (facing < -0.15))[..., None], col * 0.72, col)
        col = np.where(port[..., None], hexrgb(PORT), col)
        glow = np.maximum(glow, hexrgb(PORT) * blur(port.astype(np.float32), 1)[..., None] * 1.2)
        return np.clip(col, 0, 1), np.clip(glow, 0, 1)
    return base, finish


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


def bake_ship(slug, px, samples):
    import bmesh
    K.reset_scene()
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = samples; sc.cycles.use_denoising = False; sc.cycles.seed = 0
    world = bpy.data.worlds.new("World"); world.use_nodes = True; sc.world = world; world.light_settings.distance = 1.5
    bpy.ops.wm.obj_import(filepath=os.path.join(SHIPS_RAW, slug + ".obj"), forward_axis="Y", up_axis="Z")
    obs = [o for o in sc.objects if o.type == "MESH"]
    rng = random.Random(4100)
    for o in sorted(obs, key=lambda o: o.name):
        me = o.data; glow = 1.0 if o.name.startswith("glow") else 0.0; r = rng.random()
        at = me.color_attributes.new("rid", "FLOAT_COLOR", "CORNER")
        for d in at.data:
            d.color = (r, glow, 0.0, 1.0)
    bpy.ops.object.select_all(action="DESELECT")
    for o in obs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    bpy.ops.object.join()
    ship = bpy.context.view_layer.objects.active
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.006, area_weight=0.0, correct_aspect=True, scale_to_bounds=False)
    # packed again by shape, rotations allowed, so the islands fill the square and the texels go on the hull
    bpy.ops.uv.select_all(action="SELECT")
    bpy.ops.uv.pack_islands(rotate=True, shape_method="CONCAVE", margin_method="FRACTION", margin=0.004)
    bpy.ops.object.mode_set(mode="OBJECT")
    img = bpy.data.images.new(slug + "_bake", px, px, alpha=True, float_buffer=True); img.colorspace_settings.name = "Non-Color"
    mat, show = ship_material(img)
    ship.data.materials.clear(); ship.data.materials.append(mat)
    def bake(kind, margin, **kw):
        img.pixels.foreach_set(np.zeros(px * px * 4, np.float32))
        bpy.ops.object.bake(type=kind, margin=margin, **kw)
        return img_array(img).copy()
    cover = bake("EMIT", 0)[:, :, 3] > 0.5           # which pixels the islands cover, with no margin: for the seam ink
    data = bake("EMIT", 8)
    show("position"); P = bake("EMIT", 8)[:, :, :3]; show("data")
    ao = bake("AO", 8)[:, :, 0]
    nor = bake("NORMAL", 8, normal_space="OBJECT")[:, :, :3] * 2.0 - 1.0
    rid, edge, glowf = data[:, :, 0], data[:, :, 1], data[:, :, 2]
    n = nor / np.maximum(np.linalg.norm(nor, axis=2, keepdims=True), 1e-6)
    texel_m = ship_uv_m(ship) / px
    light = (0.35, 0.8, 0.5)
    base, finish = ship_detail(P, n, ao, rid, cover, texel_m, light)
    col, glow = paint(nor, ao, edge, rid, np.dstack([glowf, glowf, glowf]) * 2.0, rng_seed=11, light=light, base=base)
    col, glow = finish(col, glow)
    border = cover & ~(np.roll(cover, 1, 0) & np.roll(cover, -1, 0) & np.roll(cover, 1, 1) & np.roll(cover, -1, 1))
    border = blur(border.astype(np.float32), 1) > 0.15
    col = np.where(border[..., None], col * 0.42, col)  # ink along the seams: panel lines that follow the form
    os.makedirs(SHIPS_OUT, exist_ok=True)
    K.save_png(os.path.join(SHIPS_OUT, f"{slug}_albedo.png"), col)
    K.save_png(os.path.join(SHIPS_OUT, f"{slug}_ao.png"), np.dstack([ao, ao, ao]))
    K.save_png(os.path.join(SHIPS_OUT, f"{slug}_emit.png"), glow)
    # the UV mesh, in ship space, as flat triangle lists
    bm = bmesh.new(); bm.from_mesh(ship.data); bmesh.ops.triangulate(bm, faces=bm.faces[:])
    uvl = bm.loops.layers.uv.active
    pos, uv = [], []
    for f in bm.faces:
        for l in f.loops:
            pos += [round(c, 3) for c in l.vert.co]; uv += [round(l[uvl].uv[0], 5), round(l[uvl].uv[1], 5)]
    bm.free()
    return {"tris": len(pos) // 9, "tex_px": px, "samples": samples, "uv_m": round(ship_uv_m(ship), 3), "mesh": {"pos": pos, "uv": uv}}


def ship_main(slugs, px, samples):
    mpath = os.path.join(SHIPS_OUT, "manifest.json")
    man = json.load(open(mpath)) if os.path.exists(mpath) else {"pieces": {}, "ships": {}}
    man["_doc"] = ("The Ssarith Hunt's picked blockouts, each unwrapped and baked onto its own UVs by tools/blender/painted_bake.py "
                   "--ship (never edited by hand): the UV mesh in ship space (+x port, +y up, +z bow, metres) and its painted colour, "
                   "AO and glow maps. uv_m is metres of hull per UV unit.")
    for slug in slugs:
        info = bake_ship(slug, px, samples)
        n = int(open(os.path.join(SHIPS_RAW, slug + ".obj")).readline().split()[1])
        info["n"] = n
        man["ships"][slug] = info
        for m in ("albedo", "ao", "emit"):
            f = f"{slug}_{m}.png"
            man["pieces"][f"{slug}_{m}"] = {"file": f, "sha256": hashlib.sha256(open(os.path.join(SHIPS_OUT, f), "rb").read()).hexdigest()}
        print(f"baked ship {slug}: {info['tris']} triangles, {info['uv_m']} m per uv unit", flush=True)
    man["blender"] = bpy.app.version_string
    with open(mpath, "w") as fh:
        json.dump(man, fh, separators=(",", ":"), sort_keys=True)
        fh.write("\n")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    arg = lambda k, d: argv[argv.index(k) + 1] if k in argv else d
    if "--ship" in argv:  # a picked ship of its own: --ship hooded_lance,forked_prow [--px 2048] [--samples 64]
        return ship_main(arg("--ship", "").split(","), int(arg("--px", "2048")), int(arg("--samples", "64")))
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
