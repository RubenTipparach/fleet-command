"""The Ssarith Hunt's hull plating, modelled and rendered into tiling texture maps (OpenSpec change ssarith-ship-concepts,
version 8; the panel-textures skill, Star Crew's method on Fleet Command's panel kit, tools/blender/panel_kit.py).

The owner, 2026-11-04: "Take a look at the originals textures. They packed it with 3d depth and details. Use you skills
with star crew bridge command to pack in those details by rendering a high poly model into textures", then "Then applying
metal detail texture on top". The reference game's textures are not in this repository (someone else's art, CLAUDE.md
section 1); what is taken is the method, real relief rendered in. Each tile is MODELLED out of real pieces (plates with
seams, rivets, hatches, vents, pipes and clamps, louvres, cast sections, bolt circles) and rendered face-on with an
orthographic camera in Cycles; one render writes every map the mockup needs:

  <tile>_albedo.png   the Diffuse Color pass (the plates' shades, with crevice grime and edge wear)
  <tile>_normal.png   the Normal pass, tangent space (the tile faces +z): x along the ship, y across the face
  <tile>_orm.png      occlusion, roughness, metalness

Tiles, each a tradition's own way of cutting and laying plate (design rule X3), none drawn from the species' body
(Ship Design Bible C1-C5):
  A  Old Lodge, 6 x 6 m: plates of every size and age in rows, riveted along every edge, some patched over, bronze repairs
     with weld beads, bolted hatches, a conduit with clamps, small vents
  B  Trackers, 6 x 4.2 m: louvre bays between bolted ribs, every slat a ramp with a lip, sensor ports, a cable tray
  C  Field Hull, 8 x 4.4 m: two long cast sections with deep seams and rounded edges, weld lines, inspection hatches with
     bolt circles, flush service panels, lugs and grab rails
  metal  2 x 2 m: the fine metal on top of all three (owner: "Then applying metal detail texture on top"): brushed
     streaks, scratches and pits, from a procedural shader rendered the same way

Every tile repeats: plates sit on a grid whose seams fall on the tile's edges, and nothing crosses an edge.

    /opt/blender/blender -b --python tools/blender/build_hull_tiles_ssarith.py -- [--only A,B] [--samples 32] [--ppm 128]

Writes assets/textures/ssarith/ and its manifest.json (sizes in metres, mean albedo luminance, sha256 of every file).
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
import panel_kit as K  # noqa: E402  the shared panel kit: pieces, materials, render passes

ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "assets", "textures", "ssarith")
RAW = os.path.join(ROOT, "tools", "materials", "raw", "ssarith")

SEAM = 0.04  # metres between plates (half of it at each tile edge, so the wrap shows one seam)


def rivet(P, x, y, z, r=0.032, role="steel"):
    P.cyl(x, y, r, z, z + 0.014, role, seg=8, r1=r * 0.7)


def rivets_along(P, x0, x1, y, z, pitch=0.3, r=0.032):
    n = max(2, int((x1 - x0) / pitch))
    for i in range(n + 1):
        rivet(P, x0 + (x1 - x0) * i / n, y, z, r)


def base(P, w, h):
    P.box(-w / 2, -h / 2, w / 2, h / 2, -0.12, 0.0, "bezel")


# ----------------------------------------------------------------------------- A Old Lodge
def tile_A(P):
    w, h = 6.0, 6.0
    rng = random.Random(901)
    base(P, w, h)
    rows = 4
    for r in range(rows):
        y0, y1 = -h / 2 + r * h / rows, -h / 2 + (r + 1) * h / rows
        x = -w / 2
        while x < w / 2 - 1e-6:
            pw = min(rng.uniform(1.0, 2.2), w / 2 - x)
            if w / 2 - x - pw < 0.8:
                pw = w / 2 - x
            px0, px1, py0, py1 = x + SEAM / 2, x + pw - SEAM / 2, y0 + SEAM / 2, y1 - SEAM / 2
            t = 0.03 + rng.random() * 0.035
            role = rng.choices(["paint", "paint_lt", "paint_dk", "brass"], [55, 16, 25, 4])[0]
            P.box(px0, py0, px1, py1, 0.0, t, role, bevel=0.018)
            rivets_along(P, px0 + 0.09, px1 - 0.09, py0 + 0.09, t)
            rivets_along(P, px0 + 0.09, px1 - 0.09, py1 - 0.09, t)
            cx, cy, pwi, phi = (px0 + px1) / 2, (py0 + py1) / 2, px1 - px0, py1 - py0
            k = rng.random()
            if k < 0.16 and pwi > 1.2:  # a bolted access hatch with a handle
                hw, hh = min(0.9, pwi * 0.5), phi * 0.5
                P.rect(cx, cy, hw, hh, t, t + 0.022, "paint_dk", bevel=0.012)
                for sx in (-1, 1):
                    for sy in (-1, 1):
                        rivet(P, cx + sx * (hw / 2 - 0.06), cy + sy * (hh / 2 - 0.06), t + 0.022, 0.028)
                P.tube([(cx - 0.18, cy, t + 0.03), (cx - 0.18, cy, t + 0.09), (cx + 0.18, cy, t + 0.09), (cx + 0.18, cy, t + 0.03)], 0.02, "steel")
            elif k < 0.30 and pwi > 1.0:  # a small vent: a dark well with slats across it
                vw, vh = min(0.8, pwi * 0.5), phi * 0.42
                P.rect(cx, cy, vw + 0.08, vh + 0.08, t, t + 0.02, "paint_dk", bevel=0.01)
                P.rect(cx, cy, vw, vh, t + 0.001, t + 0.021, "rubber")
                for i in range(6):
                    yy = cy - vh / 2 + vh * (i + 0.5) / 6
                    P.rect(cx, yy, vw, vh / 6 * 0.45, t + 0.02, t + 0.04, "steel", bevel=0.006)
            elif k < 0.42 and pwi > 1.4:  # a conduit along the plate, in clamps
                yy = cy + phi * 0.18
                P.tube([(px0 + 0.15, yy, t + 0.05), (px1 - 0.15, yy, t + 0.05)], 0.045, "steel")
                for xx in (px0 + 0.35, cx, px1 - 0.35):
                    P.rect(xx, yy, 0.09, 0.16, t, t + 0.085, "paint_dk", bevel=0.01)
            elif k < 0.56:  # a patch plate welded over the old one, its weld bead round it
                ww, hh = pwi * rng.uniform(0.35, 0.6), phi * rng.uniform(0.35, 0.55)
                ox, oy = rng.uniform(-0.2, 0.2) * pwi, rng.uniform(-0.15, 0.15) * phi
                role2 = "brass" if rng.random() < 0.3 else "paint_lt"
                P.rect(cx + ox, cy + oy, ww, hh, t, t + 0.016, role2, bevel=0.008)
                a, b, c, d = cx + ox - ww / 2, cx + ox + ww / 2, cy + oy - hh / 2, cy + oy + hh / 2
                P.tube([(a, c, t + 0.004), (b, c, t + 0.004), (b, d, t + 0.004), (a, d, t + 0.004), (a, c, t + 0.004)], 0.012, "steel")
            x += pw


# ----------------------------------------------------------------------------- B Trackers
def tile_B(P):
    w, h = 6.0, 4.2
    rng = random.Random(902)
    base(P, w, h)
    for bx in (-w / 2, 0.0):  # two 3 m bays
        x0, x1 = bx + 0.16, bx + 3.0 - 0.16
        n = 12
        for i in range(n):
            y0 = -h / 2 + h * i / n + SEAM / 4
            y1 = -h / 2 + h * (i + 1) / n - SEAM / 4
            P.box(x0, y0, x1, y1, 0.0, 0.02, "paint_dk")                    # the slat's floor
            P.box(x0, y1 - 0.12, x1, y1, 0.0, 0.075, "paint", bevel=0.015)   # its raised lip, a ramp up to it
            P.box(x0, y1 - 0.26, x1, y1 - 0.12, 0.0, 0.045, "paint", bevel=0.012)
        if rng.random() < 0.9:  # a row of sensor ports in the bay
            for k in range(3):
                cx, cy = bx + 0.6 + k * 0.9, rng.uniform(-1.4, 1.4)
                P.cyl(cx, cy, 0.16, 0.075, 0.13, "paint_dk", seg=16, bevel=0.01)
                P.cyl(cx, cy, 0.09, 0.13, 0.17, "lens_off", seg=16, r1=0.06)
    for bx in (-w / 2, 0.0):  # ribs between bays (each centred on its bay's edge; the tile edge holds half of one)
        for xr in ((bx, bx + 0.14),) if bx == -w / 2 else ((bx - 0.14, bx + 0.14),):
            P.box(xr[0], -h / 2, xr[1], h / 2, 0.0, 0.12, "paint_dk", bevel=0.02)
        xc = bx + 0.07 if bx == -w / 2 else bx
        for i in range(10):
            rivet(P, xc, -h / 2 + h * (i + 0.5) / 10, 0.12, 0.03)
    P.box(w / 2 - 0.14, -h / 2, w / 2, h / 2, 0.0, 0.12, "paint_dk", bevel=0.02)  # the other half rib at the wrap
    for i in range(10):
        rivet(P, w / 2 - 0.07, -h / 2 + h * (i + 0.5) / 10, 0.12, 0.03)
    # a cable tray along the bay, three cables in it
    yy = h / 2 - 0.55
    P.box(-w / 2 + 0.3, yy - 0.12, -0.3, yy + 0.12, 0.075, 0.1, "steel", bevel=0.008)
    for k in range(3):
        P.tube([(-w / 2 + 0.3, yy - 0.06 + k * 0.06, 0.12), (-0.3, yy - 0.06 + k * 0.06, 0.12)], 0.022, "rubber")


# ----------------------------------------------------------------------------- C Field Hull
def tile_C(P):
    w, h = 8.0, 4.4
    rng = random.Random(903)
    base(P, w, h)
    for sx in (-w / 2, 0.0):  # two cast sections, 4 m each, thick with rounded edges
        x0, x1 = sx + SEAM, sx + 4.0 - SEAM
        P.box(x0, -h / 2 + SEAM / 2, x1, h / 2 - SEAM / 2, 0.0, 0.07, "paint", bevel=0.05)
        for yy in (-1.1, 1.1):  # weld lines across the casting
            P.tube([(x0 + 0.1, yy, 0.07), (x1 - 0.1, yy, 0.07)], 0.014, "steel")
        cx = (x0 + x1) / 2
        if sx < 0:  # an inspection hatch with its bolt circle
            P.cyl(cx, 0.0, 0.62, 0.07, 0.1, "paint_lt", seg=40, bevel=0.015)
            P.cyl(cx, 0.0, 0.42, 0.1, 0.115, "paint_dk", seg=40)
            for k in range(12):
                a = k / 12 * 2 * math.pi
                rivet(P, cx + math.cos(a) * 0.53, math.sin(a) * 0.53, 0.1, 0.03)
            P.tube([(cx - 0.2, 0.0, 0.12), (cx - 0.2, 0.0, 0.17), (cx + 0.2, 0.0, 0.17), (cx + 0.2, 0.0, 0.12)], 0.02, "steel")
        else:  # flush service panels and lugs
            for k, (px, py) in enumerate(((cx - 1.0, 0.45), (cx + 0.9, -0.5))):
                P.rect(px, py, 1.1, 0.7, 0.07, 0.078, "bezel")
                P.frame(px - 0.6, py - 0.4, px + 0.6, py + 0.4, 0.06, 0.07, 0.09, "paint_lt")
                for ex in (-0.5, 0.5):
                    for ey in (-0.3, 0.3):
                        rivet(P, px + ex, py + ey, 0.09, 0.025)
            for py in (-1.7, 1.7):
                P.rect(cx, py, 0.5, 0.22, 0.07, 0.16, "paint_dk", bevel=0.03)
        # a grab rail near the seam
        P.tube([(x1 - 0.25, -0.6, 0.07), (x1 - 0.25, -0.6, 0.14), (x1 - 0.25, 0.6, 0.14), (x1 - 0.25, 0.6, 0.07)], 0.025, "steel")


# ----------------------------------------------------------------------------- the fine metal on top
def metal_material():
    """A procedural fine metal: brushed streaks along x, scratches in two directions and pits, as a bump on a mid grey
    whose albedo varies a little with the streaks; roughness and metalness AOVs, as the kit's roles write them."""
    mat = bpy.data.materials.new("fine_metal")
    mat.use_nodes = True
    N, L = mat.node_tree.nodes, mat.node_tree.links
    for n in list(N):
        N.remove(n)
    out = N.new("ShaderNodeOutputMaterial")
    bsdf = N.new("ShaderNodeBsdfPrincipled")
    L.new(bsdf.outputs[0], out.inputs[0])
    tc = N.new("ShaderNodeTexCoord")
    mp = N.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (1.0, 40.0, 1.0)  # streaks along x
    L.new(tc.outputs["Object"], mp.inputs[0])
    streak = N.new("ShaderNodeTexNoise")
    streak.inputs["Scale"].default_value = 3.0
    streak.inputs["Detail"].default_value = 8.0
    L.new(mp.outputs[0], streak.inputs[0])
    scr = []
    for rot, sc in ((0.35, 7.0), (-0.6, 5.0)):
        m2 = N.new("ShaderNodeMapping")
        m2.inputs["Rotation"].default_value = (0.0, 0.0, rot)
        m2.inputs["Scale"].default_value = (sc, sc * 60.0, 1.0)
        L.new(tc.outputs["Object"], m2.inputs[0])
        wv = N.new("ShaderNodeTexWave")
        wv.wave_type = "BANDS"
        wv.inputs["Scale"].default_value = 1.0
        wv.inputs["Distortion"].default_value = 6.0
        wv.inputs["Detail"].default_value = 4.0
        L.new(m2.outputs[0], wv.inputs[0])
        cr = N.new("ShaderNodeMath"); cr.operation = "GREATER_THAN"; cr.inputs[1].default_value = 0.985
        L.new(wv.outputs[1], cr.inputs[0])
        scr.append(cr)
    pits = N.new("ShaderNodeTexVoronoi")
    pits.inputs["Scale"].default_value = 90.0
    pc = N.new("ShaderNodeMath"); pc.operation = "LESS_THAN"; pc.inputs[1].default_value = 0.06
    L.new(tc.outputs["Object"], pits.inputs[0]); L.new(pits.outputs["Distance"], pc.inputs[0])
    h1 = N.new("ShaderNodeMath"); h1.operation = "MULTIPLY"; h1.inputs[1].default_value = 0.4
    L.new(streak.outputs[0], h1.inputs[0])
    sub = N.new("ShaderNodeMath"); sub.operation = "SUBTRACT"
    L.new(h1.outputs[0], sub.inputs[0]); L.new(scr[0].outputs[0], sub.inputs[1])
    sub2 = N.new("ShaderNodeMath"); sub2.operation = "SUBTRACT"
    L.new(sub.outputs[0], sub2.inputs[0]); L.new(scr[1].outputs[0], sub2.inputs[1])
    sub3 = N.new("ShaderNodeMath"); sub3.operation = "SUBTRACT"
    L.new(sub2.outputs[0], sub3.inputs[0]); L.new(pc.outputs[0], sub3.inputs[1])
    bump = N.new("ShaderNodeBump"); bump.inputs["Strength"].default_value = 0.6; bump.inputs["Distance"].default_value = 0.002
    L.new(sub3.outputs[0], bump.inputs["Height"]); L.new(bump.outputs[0], bsdf.inputs["Normal"])
    ramp = N.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.16, 0.16, 0.16, 1.0)
    ramp.color_ramp.elements[1].color = (0.24, 0.24, 0.24, 1.0)
    L.new(streak.outputs[0], ramp.inputs[0])
    lift = N.new("ShaderNodeMix"); lift.data_type = "RGBA"
    L.new(scr[0].outputs[0], lift.inputs["Factor"])
    L.new(ramp.outputs[0], lift.inputs[6]); lift.inputs[7].default_value = (0.42, 0.42, 0.42, 1.0)
    L.new(lift.outputs[2], bsdf.inputs["Base Color"])
    for name, val in (("rough", None), ("metal", 1.0)):
        aov = N.new("ShaderNodeOutputAOV"); aov.aov_name = name
        if val is None:
            r = N.new("ShaderNodeMapRange"); r.inputs["To Min"].default_value = 0.32; r.inputs["To Max"].default_value = 0.6
            L.new(streak.outputs[0], r.inputs[0]); L.new(r.outputs[0], aov.inputs["Value"])
        else:
            aov.inputs["Value"].default_value = val
    return mat


def tile_metal(P):
    w = h = 2.0
    me = bpy.data.meshes.new("fine_metal_plane")
    me.from_pydata([(-w / 2, -h / 2, 0), (w / 2, -h / 2, 0), (w / 2, h / 2, 0), (-w / 2, h / 2, 0)], [], [(0, 1, 2, 3)])
    me.materials.append(metal_material())
    ob = bpy.data.objects.new("fine_metal_plane", me)
    P.coll.objects.link(ob)


TILES = {"A": (tile_A, 6.0, 6.0), "B": (tile_B, 6.0, 4.2), "C": (tile_C, 8.0, 4.4), "metal": (tile_metal, 2.0, 2.0)}


def bake(name, build, w, h, ppm, samples):
    """Model one tile and write its maps (the kit's render passes, with occlusion reaching 0.25 m: hull scale)."""
    K.reset_scene()
    w_px, h_px = int(round(w * ppm)), int(round(h * ppm))
    K.setup_render(w_px, h_px, samples)
    bpy.context.scene.world.light_settings.distance = 0.25
    P = K.Panel(name, w, h, K.make_materials(), seed=7)
    build(P)
    K.camera(w, h)
    K.compositor_outputs(RAW, name)
    bpy.ops.render.render(write_still=False)
    get = lambda p: K.read_exr(os.path.join(RAW, f"{name}_{p}0001.exr"))
    alb, nor, ao, rough, metal, beauty = (get(p) for p in ("albedo", "normal", "ao", "rough", "metal", "beauty"))
    albs = K.to_srgb(alb)
    K.save_png(os.path.join(OUT, f"{name}_albedo.png"), albs)
    n = nor / np.maximum(np.linalg.norm(nor, axis=2, keepdims=True), 1e-6)
    K.save_png(os.path.join(OUT, f"{name}_normal.png"), n * 0.5 + 0.5)
    K.save_png(os.path.join(OUT, f"{name}_orm.png"), np.dstack([ao[:, :, 0], rough[:, :, 0], metal[:, :, 0]]))
    K.save_png(os.path.join(RAW, f"{name}_beauty.png"), K.to_srgb(beauty * 1.1))
    lum = float((0.2126 * albs[:, :, 0] + 0.7152 * albs[:, :, 1] + 0.0722 * albs[:, :, 2]).mean())
    return {"u_m": w, "v_m": h, "px": [w_px, h_px], "albedo_mean_luminance": round(lum, 4)}


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    arg = lambda k, d: argv[argv.index(k) + 1] if k in argv else d
    only = arg("--only", ",".join(TILES)).split(",")
    samples, ppm = int(arg("--samples", "32")), float(arg("--ppm", "128"))
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(RAW, exist_ok=True)
    mpath = os.path.join(OUT, "manifest.json")
    man = json.load(open(mpath)) if os.path.exists(mpath) else {"tiles": {}, "pieces": {}}
    man["_doc"] = ("The Ssarith Hunt's hull plating tiles, modelled and rendered by tools/blender/build_hull_tiles_ssarith.py "
                   "(never edited by hand). u along the ship, v across the face, metres; the normal maps are tangent space "
                   "(x along u, y along v).")
    for name in only:
        build, w, h = TILES[name]
        info = bake(name, build, w, h, ppm if name != "metal" else ppm * 2, samples)
        info["samples"] = samples
        man["tiles"][name] = info
        for m in ("albedo", "normal", "orm"):
            f = f"{name}_{m}.png"
            man["pieces"][f"{name}_{m}"] = {"file": f, "sha256": hashlib.sha256(open(os.path.join(OUT, f), "rb").read()).hexdigest()}
        print(f"baked {name}: {info}", flush=True)
    man["blender"] = bpy.app.version_string
    with open(mpath, "w") as fh:
        json.dump(man, fh, indent=2, sort_keys=True)
        fh.write("\n")


if __name__ == "__main__":
    main()
