"""Fleet Command: renders the analog panel bible's 3D tiles: the formed-metal catalogue and the lighting options.

Owns the Blender side of docs/design/ui-bible.html chapters 8 and 10. Each tile is a 320 x 220 mm plate built from the
outlines tools/mockups/formed_metal.py writes (tiles.json, already fit-checked), with the same plate, bezel, backlit
film, lamps, knobs, materials and light linking as the panel study, so a tile looks exactly as a panel built that way
would. Everything it builds with is tools/blender/ui_shapes.py's Panel (one implementation for every skin).

    python3 tools/mockups/formed_metal.py --out <dir>
    TILE_DIR=<dir> TILES=formed|lighting [ONLY=a,b] blender --background --factory-startup \
        --python tools/blender/ui_tiles_tail.py -- x 32

Writes <dir>/tile_<name>.png (formed) and <dir>/tile_light_<mode>.png (lighting). Units: 1 px of a tile = 1 mm;
renders at 2x (640 x 440 px). The lighting tiles print in Tektur when its file is found (TEKTUR, default the
canvas-design fonts folder), else in DejaVu Sans Bold.
"""
import json
import math
import os
import sys

import bpy

HERE_PY = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE_PY)
import panel_kit  # noqa: E402
import ui_render  # noqa: E402
from ui_shapes import MM, Panel, circle, film_ink, film_mat, link_layers, metal, printed, rrect, satin  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAMPLES = int(argv[1]) if len(argv) > 1 else 32
HERE = os.environ["TILE_DIR"]
SPECS = json.load(open(os.path.join(HERE, "tiles.json")))
W, H = 320, 220
FILM_GLOW, MAIN, MAIN_SOFT = 0.78, 0.8, 60.0   # the study's chosen look (the bible's chapter 7 table)
TEKTUR = os.environ.get("TEKTUR", "/mnt/skills/examples/canvas-design/canvas-fonts/Tektur-Medium.ttf")
DISPLAY = ("display", "ink", "well", "redink")


def scene_base(name):
    panel_kit.reset_scene()
    ui_render.setup(W * 2, H * 2, SAMPLES, [("Room", (0.3, -1.0, -1.6), 0.7, (1.0, 0.97, 0.92), 8.0)], world=(0.05, 0.045, 0.04),
                    bounces=dict(max_bounces=8, glossy_bounces=4, transmission_bounces=4, diffuse_bounces=3))
    K = Panel(name, W, H)
    K.m.update(plate=K.mat("satin", lambda: satin("satin")), film=K.mat("display", lambda: film_mat("display", glow=FILM_GLOW)),
               ink=K.mat("ink", lambda: film_ink("ink", "#9a7f4f", glow=FILM_GLOW)), well=K.mat("well", lambda: film_ink("well", "#7d6845", glow=FILM_GLOW)),
               groove=K.mat("groove", lambda: printed("groove", "#2e271c")), knob=K.mat("knobmetal", lambda: metal("knobmetal", "#d6d0c4", 0.16)),
               pointer=K.mat("red", lambda: printed("red", "#a83020")), redink=K.mat("redink", lambda: film_ink("redink", "#a83020", glow=FILM_GLOW)),
               bezel=K.mat("brass", lambda: metal("brass", "#b0915a", 0.28)))
    K.slab([rrect(0, 0, W, H, 12)], K.dz - 6.0, K.dz - 4.0, K.m["groove"], bevel=0.5)
    return K


def build_plate(K, sp):
    for f in sp["films"]:  # each film is measured to fit its plate (rule B): the window grown 8 mm, clamped 4 mm inside
        K.film(f, shape=f)
    K.slab([sp["plate_off"]] + sp["holes_off"], 0.0, K.pt, K.m["plate"], bevel=K.roll)      # rolls grow back to the drawn line
    for outer, hole in sp["bezel"]:
        K.slab([outer, hole], K.pt - K.roll - 1.8, K.pt - K.roll + 0.2, K.m["bezel"], bevel=0.6)
    for x, y, r, p in sp["knobs"]:
        K.knob(x, y, r, pointer=p)
    for x, y, r in sp["gauges"]:
        K.flat([circle(x, y, r + 0.7, 64), circle(x, y, r - 0.7, 64)], K.m["ink"])
        for i in range(11):
            a = math.radians(200 + 14 * i)
            K.stroke([(x + math.cos(a) * r * 0.86, y + math.sin(a) * r * 0.86), (x + math.cos(a) * r * 0.97, y + math.sin(a) * r * 0.97)], 1.0, K.m["ink"])
        a = math.radians(250)
        K.stroke([(x, y), (x + math.cos(a) * r * 0.9, y + math.sin(a) * r * 0.9)], 1.2, K.m["redink"])


def finish_scene(K, name):
    link_layers(DISPLAY, DISPLAY, main=(MAIN, MAIN_SOFT))
    panel_kit.camera(W * MM, H * MM)
    sc = bpy.context.scene
    sc.render.filepath = os.path.join(HERE, f"tile_{name}.png")
    bpy.ops.render.render(write_still=True)
    print("TILE done", name, flush=True)


def lighting_tile(mode):
    """One plain display with printed text and a ruler, lit one way, to compare lighting options for legibility."""
    K = scene_base("light_" + mode)
    ink, roll, pt = K.m["ink"], K.roll, K.pt
    win = rrect(22, 22, W - 22, H - 52, 8)
    K.film(win)
    K.slab([rrect(6 + roll, 6 + roll, W - 6 - roll, H - 6 - roll, 14 - roll), rrect(22 - roll, 22 - roll, W - 22 + roll, H - 52 + roll, 8 + roll)],
           0.0, pt, K.m["plate"], bevel=roll)
    K.slab([rrect(21.2, 21.2, W - 21.2, H - 51.2, 8.8), rrect(23.5, 23.5, W - 23.5, H - 53.5, 6.5)], pt - roll - 1.8, pt - roll + 0.2, K.m["bezel"], bevel=0.6)
    font = TEKTUR if os.path.exists(TEKTUR) else None
    for i, t in enumerate(("THE LONG WATCH", "CONCORD FLEET ORCHESTRA", "SUITES FROM THE DEEP")):
        K.label(t, 42, 52 + i * 26, 13, ink, K.dz + K.fz, font=font, align="LEFT", track=0.1)
    K.stroke([(42, 140), (W - 42, 140)], 1.0, ink)
    for i in range(25):
        x = 42 + i * (W - 84) / 24
        K.stroke([(x, 140.5), (x, 134 if i % 4 else 130)], 0.8, ink)
    K.knob(W / 2, H - 29, 9)
    x0, y0, x1, y1 = 22, 22, W - 22, H - 52
    e = {"top": (x0 + 8, y0, x1 - 8, y0, 0, 1), "bottom": (x0 + 8, y1, x1 - 8, y1, 0, -1),
         "left": (x0, y0 + 8, x0, y1 - 8, 1, 0), "right": (x1, y0 + 8, x1, y1 - 8, -1, 0)}
    if mode == "spots":
        for x in (W * 0.2, W * 0.5, W * 0.8):
            K.lip_light(x, y0, 0.3)
    elif mode == "strip":
        K.edge_lamp(*e["top"], 0.0004)
    elif mode == "all_sides":
        for k in e:
            K.edge_lamp(*e[k], 0.00018)
    elif mode == "bottom":
        K.edge_lamp(*e["bottom"], 0.0004)
    elif mode == "rim":
        for k in e:
            K.edge_lamp(*e[k], 0.0004, spread=30.0, depth=K.dz + 1.5)
    elif mode == "side":
        K.edge_lamp(*e["left"], 0.0006)
    finish_scene(K, "light_" + mode)


which = os.environ.get("TILES", "formed")
names = [n for n in os.environ.get("ONLY", "").split(",") if n]
if which == "formed":
    for name in names or list(SPECS):
        K = scene_base(name)
        build_plate(K, SPECS[name])
        for x, y in SPECS[name]["lamps"]:
            K.lip_light(x, y, 0.3)
        finish_scene(K, name)
else:
    for mode in names or ["spots", "strip", "all_sides", "bottom", "rim", "side", "backlight"]:
        lighting_tile(mode)
