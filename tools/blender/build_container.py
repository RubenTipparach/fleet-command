"""The freighters' container: one asset, modelled and baked once, carried by every freighter of every faction
(openspec/changes/freighters, task 2.5; owner, 2026-11-07: "you need like better cargo textures, lookem up online for cc0
options, back ao and get diffuse and normals and specular for each container").

A high-poly container is built here in Blender: corrugated panels (their ribs from the CC0 Poly Haven texture set
"Container Side", by Dimitrios Savva, its normal map; assets/containers/SOURCES.md), a frame of twelve bars round the box's
edges, eight corner castings with their sockets, and at the door end two leaves with four locking bars, their brackets,
handles and hinges. It is baked selected-to-active onto a low-poly box (6 by 6 by 12 m drafted, shown twice that on the
freighters page) whose faces are laid out once in a square atlas: ambient occlusion, tangent normals (the modelled frame
and the texture's ribs together), diffuse colour and roughness, from which the specular map is drawn. The diffuse is
grey (the texture's own green taken to its luminance), so each container's own colour tints it on the page.

    python3 tools/textures/fetch_cc0.py container_side          # the CC0 source maps, into tools/materials/raw/cc0/
    /opt/blender/blender -b --python tools/blender/build_container.py -- [--px 2048] [--samples 64]
    /opt/blender/blender -b --python tools/blender/build_container.py -- --glb   # the low box alone, for the game

--glb writes assets/containers/container.glb: the same low box (no bake), its atlas UVs and tangents, in the box's own frame
(+Y up, long along z, metres, origin at its centre), which the game draws once per container under each freighter
(src/Game/View/Cargo.cs, the places from assets/ships/manifest.json "cargo").

Writes assets/containers/container_{diffuse,normal,ao,specular,rough}.png and assets/containers/manifest.json (its maps as
"pieces", the box's
size, the atlas and every face's place in it, so the page builds the same box: docs/mockups/freighter-concepts.html).
Deterministic: no random numbers; Cycles' seed is fixed.
"""
import hashlib
import json
import math
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "tools", "materials", "raw", "cc0", "container_side")
OUT = os.path.join(ROOT, "assets", "containers")
W, H, L = 6.0, 6.0, 12.0        # across (x), up (y), along (z), drafted metres
TILE_M = 1.94                   # the texture's own size (Poly Haven: 1940 mm square)
ATLAS_M = 24.0                  # the atlas square, metres of container per UV unit
PAD = 0.25                      # metres between faces in the atlas
FRAME = 0.42                    # the frame bars' section, metres
INSET = 0.15                    # how far the panels stand behind the frame (deep enough for the AO to find)

# The atlas: each face of the box (outward normal, its two in-plane axes, its size) and its place, in metres, in the square.
# Long faces stacked in the left column, the two ends in the right.
FACES = {
    "port":   {"n": (1, 0, 0), "u": (0, 0, -1), "v": (0, 1, 0), "size": (L, H), "at": (PAD, PAD)},
    "stbd":   {"n": (-1, 0, 0), "u": (0, 0, 1), "v": (0, 1, 0), "size": (L, H), "at": (PAD, PAD * 2 + H)},
    "top":    {"n": (0, 1, 0), "u": (0, 0, -1), "v": (-1, 0, 0), "size": (L, W), "at": (PAD, PAD * 3 + H * 2)},
    "bottom": {"n": (0, -1, 0), "u": (0, 0, -1), "v": (1, 0, 0), "size": (L, W), "at": (PAD * 2 + L, PAD * 3 + H * 2)},
    "doors":  {"n": (0, 0, 1), "u": (1, 0, 0), "v": (0, 1, 0), "size": (W, H), "at": (PAD * 2 + L, PAD)},
    "back":   {"n": (0, 0, -1), "u": (-1, 0, 0), "v": (0, 1, 0), "size": (W, H), "at": (PAD * 2 + L, PAD * 2 + H)},
}


def arg(name, default):
    a = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    return a[a.index(name) + 1] if name in a else default


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.seed = 7


def box(name, size, at, mat):
    """A box of size (x, y, z) centred at `at`, with the material."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2])) + Vector(at)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    me.materials.append(mat)
    return ob


def cyl(name, r, h, at, mat, axis="y"):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=12, radius1=r, radius2=r, depth=h)
    rot = {"y": ((1, 0, 0), math.pi / 2), "z": ((0, 0, 1), 0.0), "x": ((0, 1, 0), math.pi / 2)}[axis]
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=__import__("mathutils").Matrix.Rotation(rot[1], 3, rot[0]))
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector(at))
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    me.materials.append(mat)
    return ob


def image_node(nt, path, colour):
    n = nt.nodes.new("ShaderNodeTexImage")
    n.image = bpy.data.images.load(path)
    n.image.colorspace_settings.name = "sRGB" if colour else "Non-Color"
    n.projection = "BOX"; n.projection_blend = 0.15
    return n


def cc0_material(name, tint, rough_k=1.0, ribbed=True, metal=0.0, turn=None):
    """A material from the CC0 set, box-projected in object space at the texture's own size: its luminance times tint
    for colour, its roughness, and (ribbed) its normal map. A frame part takes the same paint without the ribs."""
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    tc = nt.nodes.new("ShaderNodeTexCoord"); mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (1 / TILE_M, 1 / TILE_M, 1 / TILE_M)
    if turn:   # the texture turned a quarter so its ribs stand upright on the face: about x for the sides, about z for the ends
        mp.inputs["Rotation"].default_value = turn
    nt.links.new(tc.outputs["Object"], mp.inputs["Vector"])
    diff = image_node(nt, os.path.join(SRC, "container_side_diff_2k.jpg"), True)
    rough = image_node(nt, os.path.join(SRC, "container_side_rough_2k.jpg"), False)
    for n in (diff, rough):
        nt.links.new(mp.outputs["Vector"], n.inputs["Vector"])
    grey = nt.nodes.new("ShaderNodeRGBToBW"); nt.links.new(diff.outputs["Color"], grey.inputs["Color"])
    lift = nt.nodes.new("ShaderNodeMath"); lift.operation = "MULTIPLY"; lift.inputs[1].default_value = 2.1   # the green's luminance (~0.3) up to a light grey
    nt.links.new(grey.outputs["Val"], lift.inputs[0])
    mix = nt.nodes.new("ShaderNodeMixRGB"); mix.blend_type = "MULTIPLY"; mix.inputs["Fac"].default_value = 1.0
    mix.inputs["Color2"].default_value = (*tint, 1)
    nt.links.new(lift.outputs["Value"], mix.inputs["Color1"])
    nt.links.new(mix.outputs["Color"], bsdf.inputs["Base Color"])
    rk = nt.nodes.new("ShaderNodeMath"); rk.operation = "MULTIPLY"; rk.inputs[1].default_value = rough_k
    nt.links.new(rough.outputs["Color"], rk.inputs[0]); nt.links.new(rk.outputs["Value"], bsdf.inputs["Roughness"])
    bsdf.inputs["Metallic"].default_value = metal
    if ribbed:
        nor = image_node(nt, os.path.join(SRC, "container_side_nor_gl_2k.jpg"), False)
        nt.links.new(mp.outputs["Vector"], nor.inputs["Vector"])
        nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = 1.2
        nt.links.new(nor.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    return m


def build_high():
    """The container as modelled: panels a little behind the frame, the frame, the castings, the doors' hardware."""
    panel = cc0_material("panel", (1.0, 1.0, 1.0))
    side = cc0_material("panel_side", (1.0, 1.0, 1.0), turn=(math.pi / 2, 0, 0))
    end = cc0_material("panel_end", (1.0, 1.0, 1.0), turn=(0, 0, math.pi / 2))
    frame = cc0_material("frame", (0.78, 0.78, 0.78), 1.05, ribbed=False)
    steel = cc0_material("steel", (0.55, 0.55, 0.56), 0.7, ribbed=False, metal=0.8)
    obs = []
    # the panels: each face of the box set INSET behind the frame (the door end is the two leaves, below)
    hw, hh, hl = W / 2 - INSET, H / 2 - INSET, L / 2 - INSET
    obs += [box("side_port", (0.05, H - 2 * FRAME, L - 2 * FRAME), (hw, 0, 0), side),
            box("side_stbd", (0.05, H - 2 * FRAME, L - 2 * FRAME), (-hw, 0, 0), side),
            box("roof", (W - 2 * FRAME, 0.05, L - 2 * FRAME), (0, hh, 0), panel),
            box("floor", (W - 2 * FRAME, 0.05, L - 2 * FRAME), (0, -hh, 0), panel),
            box("back", (W - 2 * FRAME, H - 2 * FRAME, 0.05), (0, 0, -hl), end)]
    # the frame: twelve bars along the box's edges
    f = FRAME
    for sx in (1, -1):
        for sy in (1, -1):
            obs.append(box(f"rail_{sx}{sy}", (f, f, L), (sx * (W - f) / 2, sy * (H - f) / 2, 0), frame))     # along
            for sz in (1, -1):
                pass
        for sz in (1, -1):
            obs.append(box(f"post_{sx}{sz}", (f, H, f), (sx * (W - f) / 2, 0, sz * (L - f) / 2), frame))       # corner posts
    for sy in (1, -1):
        for sz in (1, -1):
            obs.append(box(f"end_rail_{sy}{sz}", (W, f, f), (0, sy * (H - f) / 2, sz * (L - f) / 2), frame))   # across the ends
    # the corner castings, standing a little proud, each with a socket cut as a dark recess (a smaller dark block set in)
    c = 0.62
    for sx in (1, -1):
        for sy in (1, -1):
            for sz in (1, -1):
                at = (sx * (W - c) / 2 + sx * 0.03, sy * (H - c) / 2 + sy * 0.03, sz * (L - c) / 2 + sz * 0.03)
                obs.append(box("casting", (c, c, c), at, steel))
                obs.append(box("socket", (0.34, 0.18, 0.06), (at[0], at[1], at[2] + sz * (c / 2 + 0.01)), frame))
                obs.append(box("socket_side", (0.06, 0.18, 0.34), (at[0] + sx * (c / 2 + 0.01), at[1], at[2]), frame))
    # the door end: two leaves, a seam between them, four locking bars with brackets and handles, hinges at the edges
    zd = L / 2 - INSET
    for sx in (1, -1):
        obs.append(box("door_leaf", ((W - 2 * f) / 2 - 0.04, H - 2 * f, 0.08), (sx * ((W - 2 * f) / 4 + 0.02), 0, zd), end))
        for bx in (0.7, 2.0):
            x = sx * bx
            obs.append(cyl("lock_bar", 0.07, H - 2 * f - 0.1, (x, 0, zd + 0.16), steel, "y"))
            for y in (H / 2 - f - 0.35, 0.0, -(H / 2 - f - 0.35)):
                obs.append(box("bar_bracket", (0.28, 0.16, 0.14), (x, y, zd + 0.1), steel))
            obs.append(box("handle", (0.08, 0.6, 0.12), (x + sx * 0.22, -0.6, zd + 0.2), steel))
        for y in (H / 2 - f - 0.7, H / 6, -H / 6, -(H / 2 - f - 0.7)):
            obs.append(box("hinge", (0.3, 0.42, 0.16), (sx * (W / 2 - f - 0.1), y, zd + 0.1), steel))
    for o in obs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    bpy.ops.object.join()
    hi = bpy.context.view_layer.objects.active; hi.name = "container_high"
    bv = hi.modifiers.new("bevel", "BEVEL"); bv.width = 0.05; bv.segments = 2; bv.limit_method = "ANGLE"   # edges that catch the light
    bpy.ops.object.modifier_apply(modifier="bevel")
    return hi


def build_low():
    """The low-poly box, each face its own quad, laid out in the atlas (FACES): the box the page draws."""
    me = bpy.data.meshes.new("container_low"); bm = bmesh.new(); uvl = bm.loops.layers.uv.new("UVMap")
    for name, F in FACES.items():
        n, u, v = Vector(F["n"]), Vector(F["u"]), Vector(F["v"]); su, sv = F["size"]
        half = Vector((W / 2, H / 2, L / 2))
        c = Vector((n.x * half.x, n.y * half.y, n.z * half.z))
        corners = [(-1, -1), (1, -1), (1, 1), (-1, 1)]
        verts = [bm.verts.new(c + u * (a * su / 2) + v * (b * sv / 2)) for a, b in corners]
        face = bm.faces.new(verts)
        if face.normal.dot(n) < 0:
            face.normal_flip()
        for loop in face.loops:
            p = loop.vert.co - c
            uu, vv = p.dot(u) / su + 0.5, p.dot(v) / sv + 0.5
            loop[uvl].uv = ((F["at"][0] + uu * su) / ATLAS_M, (F["at"][1] + vv * sv) / ATLAS_M)
    bm.to_mesh(me); bm.free()
    lo = bpy.data.objects.new("container_low", me); bpy.context.collection.objects.link(lo)
    return lo


def bake(lo, hi, kind, img, samples, **kw):
    sc = bpy.context.scene; sc.cycles.samples = samples
    mat = lo.data.materials[0]; nt = mat.node_tree
    for n in nt.nodes:
        n.select = False
    node = nt.nodes.get("target") or nt.nodes.new("ShaderNodeTexImage"); node.name = "target"; node.image = img
    nt.nodes.active = node; node.select = True
    bpy.ops.object.select_all(action="DESELECT"); hi.select_set(True); lo.select_set(True); bpy.context.view_layer.objects.active = lo
    bpy.ops.object.bake(type=kind, use_selected_to_active=True, cage_extrusion=0.45, max_ray_distance=0.9, margin=12, **kw)


def pixels(img):
    a = np.array(img.pixels[:], dtype=np.float32).reshape(img.size[1], img.size[0], 4)
    return a


def save(path, rgb):
    """Save an RGB float array (bottom-up rows, as Blender holds them) as an 8-bit PNG."""
    h, w = rgb.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=False)
    im.colorspace_settings.name = "Non-Color"   # values written as they are (the colour map is already sRGB)
    im.pixels[:] = np.dstack([np.clip(rgb, 0, 1), np.ones((h, w))]).ravel()
    im.filepath_raw = path; im.file_format = "PNG"; im.save()


def export_glb():
    reset(); lo = build_low()
    mat = bpy.data.materials.new("container"); lo.data.materials.append(mat)
    bpy.ops.object.select_all(action="DESELECT"); lo.select_set(True); bpy.context.view_layer.objects.active = lo
    path = os.path.join(OUT, "container.glb")
    # the box is authored +Y up already (FACES), so no axis conversion
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", use_selection=True, export_yup=False, export_tangents=True,
                              export_normals=True, export_materials="PLACEHOLDER", export_apply=True)
    print(f"wrote {path} sha256 {hashlib.sha256(open(path, 'rb').read()).hexdigest()}", flush=True)


def main():
    if "--glb" in sys.argv:
        export_glb(); return
    px, samples = int(arg("--px", "2048")), int(arg("--samples", "64"))
    reset()
    hi = build_high(); lo = build_low()
    lm = bpy.data.materials.new("low"); lm.use_nodes = True; lo.data.materials.append(lm)
    os.makedirs(OUT, exist_ok=True)
    imgs = {k: bpy.data.images.new(k, px, px, alpha=False, float_buffer=True) for k in ("ao", "normal", "diffuse", "rough")}
    for k in imgs:
        imgs[k].colorspace_settings.name = "Non-Color"
    bake(lo, hi, "AO", imgs["ao"], samples)
    bake(lo, hi, "NORMAL", imgs["normal"], 1, normal_space="TANGENT")
    bake(lo, hi, "DIFFUSE", imgs["diffuse"], 1, pass_filter={"COLOR"})
    bake(lo, hi, "ROUGHNESS", imgs["rough"], 1)
    ao = pixels(imgs["ao"])[..., :3]
    nor = pixels(imgs["normal"])[..., :3]
    dif = pixels(imgs["diffuse"])[..., :3]            # linear
    rough = pixels(imgs["rough"])[..., :1]
    # the colour map: the paint in sRGB, its crevices darkened a little by the AO (the AO map itself carries the rest)
    srgb = np.where(dif <= 0.0031308, dif * 12.92, 1.055 * np.power(np.clip(dif, 0, 1), 1 / 2.4) - 0.055)
    colour = srgb * (0.55 + 0.45 * ao)
    spec = np.clip((1.0 - rough) * 1.25, 0, 1) * (0.6 + 0.4 * ao[..., :1])   # bright where smooth and open, dull in the grime and crevices
    maps = {"diffuse": colour, "normal": nor, "ao": ao, "specular": np.repeat(spec, 3, axis=2), "rough": np.repeat(rough, 3, axis=2)}
    man = {"_doc": "The freighters' container (openspec/changes/freighters, task 2.5), written by tools/blender/build_container.py: a "
                   "low-poly box (size_m: across, up, along, drafted metres) whose faces sit in one square atlas (atlas_m metres a UV unit; "
                   "each face's n, u, v axes, size and place in metres), and its baked maps. The diffuse is grey: each container's own colour "
                   "tints it. Sources: assets/containers/SOURCES.md.",
           "size_m": [W, H, L], "atlas_m": ATLAS_M, "faces": FACES, "px": px, "samples": samples, "blender": bpy.app.version_string,
           "webp_quality": 90, "pieces": {}}   # "pieces": the shape tools/mockups/inline_data.py carries as data URIs
    for k, a in maps.items():
        p = os.path.join(OUT, f"container_{k}.png"); save(p, a)
        man["pieces"][k] = {"file": os.path.basename(p), "sha256": hashlib.sha256(open(p, "rb").read()).hexdigest()}
        print(f"wrote {p}", flush=True)
    with open(os.path.join(OUT, "manifest.json"), "w") as fh:
        json.dump(man, fh, indent=1, sort_keys=True); fh.write("\n")


main()
