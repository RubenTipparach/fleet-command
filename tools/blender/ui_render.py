"""Fleet Command: the shared render harness for every console skin modelled in Blender.

Owns what all the skin generators (tools/blender/build_ui_<skin>.py) have in common: the Cycles setup
(CPU, fixed seed and samples, OpenImageDenoise, transparent film, Standard view, no stamps), lights,
the face-on orthographic camera, rendering each kit piece at twice its slot size, stripping the PNG
metadata Cycles writes so a rerun is byte-identical, and the manifest of sizes and margins the page
and the game read (CLAUDE.md section 7: one kit, a skin per faction; section 6.1: one implementation).
A skin generator owns only its modelling, its materials and its lighting.

Units: 1 px of the 1x slot is 1 mm in Blender. A piece is modelled in the xy plane facing +z with its
centre at the origin; the camera looks down from +z.

Manifest (assets/ui/<skin>/manifest.json):
    {"skin": "<skin>", "scale": 2, "pieces": {"<name>": {"kind": "nine"|"hero", "size": [w, h],
     "margins": [t, r, b, l], "stretch": "x"|"xy", "file": "<name>.png"}}}
"""

import argparse
import json
import math
import os
import struct
import sys
import time

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import panel_kit  # noqa: E402

ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
MM = 0.001
SCALE = 2


def sun(name, direction, energy, colour, angle_deg):
    """A sun lamp shining along direction (x right, y up, z toward the camera)."""
    ld = bpy.data.lights.new(name, "SUN")
    ld.energy = energy
    ld.color = colour
    ld.angle = math.radians(angle_deg)
    ob = bpy.data.objects.new(name, ld)
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = Vector(direction).normalized().to_track_quat("-Z", "Y")
    bpy.context.scene.collection.objects.link(ob)


def setup(w_px, h_px, samples, lights, world=(0.055, 0.06, 0.07), bounces=None):
    """The render settings every skin shares; lights are (name, direction, energy, colour, angle) suns,
    bounces overrides any of Cycles' bounce limits (a skin with subsurface or glass needs more)."""
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    cy = sc.cycles
    cy.device = "CPU"
    cy.samples = samples
    cy.use_adaptive_sampling = False
    cy.seed = 0
    cy.use_denoising = True
    cy.denoiser = "OPENIMAGEDENOISE"
    b = dict(max_bounces=4, diffuse_bounces=2, glossy_bounces=2, transmission_bounces=0)
    b.update(bounces or {})
    for k, v in b.items():
        setattr(cy, k, v)
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
    for attr in dir(sc.render):
        if attr.startswith("use_stamp"):
            setattr(sc.render, attr, False)
    w = bpy.data.worlds.new("World")
    w.use_nodes = True
    bg = w.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (*world, 1.0)
    bg.inputs["Strength"].default_value = 1.0
    sc.world = w
    for l in lights:
        sun(*l)


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


def render_piece(name, spec, out_dir, samples, make_piece, render_setup):
    """Model one piece in a fresh scene, render it face on and return its manifest entry.
    spec is (kind, (w, h), margins, stretch, build), optionally with a sixth item, a bar track's fill inset (x, y);
    make_piece(name, w, h, hero) returns the modelling context build() draws into; render_setup(w_px, h_px, samples)
    sets render and lights."""
    kind, (w, h), margins, stretch, build = spec[:5]
    panel_kit.reset_scene()
    render_setup(w * SCALE, h * SCALE, samples)
    K = make_piece(name, w, h, kind == "hero")
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
        if len(spec) > 5 and spec[5]:
            entry["inset"] = list(spec[5])
    return entry


def run(skin, pieces, make_piece, render_setup, samples=64):
    """The command line every generator shares: --out, --only a,b, --samples N. Renders the pieces,
    merges them into the manifest (keeping pieces not re-rendered with --only), in kit order."""
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(ROOT, "assets", "ui", skin))
    ap.add_argument("--only", default="")
    ap.add_argument("--samples", type=int, default=samples)
    a = ap.parse_args(argv)
    out = os.path.abspath(a.out)
    os.makedirs(out, exist_ok=True)
    names = [n for n in a.only.split(",") if n] or list(pieces)
    man_path = os.path.join(out, "manifest.json")
    man = {"skin": skin, "scale": SCALE, "pieces": {}}
    if a.only and os.path.exists(man_path):
        with open(man_path) as f:
            man = json.load(f)
    t0 = time.time()
    for n in names:
        t = time.time()
        man["pieces"][n] = render_piece(n, pieces[n], out, a.samples, make_piece, render_setup)
        print(f"{skin.upper()} {n}: {time.time() - t:.1f} s", flush=True)
    man["pieces"] = {k: man["pieces"][k] for k in pieces if k in man["pieces"]}
    with open(man_path, "w") as f:
        json.dump(man, f, indent=2)
        f.write("\n")
    print(f"{skin.upper()} done: {len(names)} pieces in {time.time() - t0:.1f} s", flush=True)
