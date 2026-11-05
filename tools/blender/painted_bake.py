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


def paint(nor, ao, edge, rid, emit, rng_seed=5):
    """Paint the passes into a drawn-on albedo (sRGB, 0..1) and a glow map."""
    h, w = ao.shape
    rng = np.random.default_rng(rng_seed)
    n = nor / np.maximum(np.linalg.norm(nor, axis=2, keepdims=True), 1e-6)
    # flat paint per piece, from the palette by its random id; a few plates in bronze
    pal = np.stack([hexrgb(c) for c in PALETTE])
    idx = np.clip((rid * len(PALETTE) * 7).astype(int) % len(PALETTE), 0, len(PALETTE) - 1)
    base = pal[idx]
    base = np.where((rid > 0.93)[..., None], hexrgb(BRONZE), base)
    # the painter's light from the upper left, a little toward the viewer, in a few soft steps
    L = np.array([-0.5, 0.55, 0.67], np.float32); L /= np.linalg.norm(L)
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


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    arg = lambda k, d: argv[argv.index(k) + 1] if k in argv else d
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
