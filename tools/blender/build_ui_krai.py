"""Fleet Command: the Krai Warhost console skin, modelled, lit and rendered in Blender.

Owns every piece of the Krai skin of the shared console UI kit, rebuilt on the analog panel bible (owner, 2026-10-31:
"we need to update krai, onbrai, and hunt to use the new guildine standards ... Please make them look nice";
docs/design/ui-bible.html and the blender-ui-panels skill). The combat race's design language (design.md: "a combat
focused race, lots of triangular motifs, angry red colors and orange lit buttons that are activated. strong metal
framing, almost blade like") in the bible's two layers: blackened blade steel plates whose every edge rolls over in a
tight honed round, red enamel bezels round every well and enamel blades standing on the flat metal, honed steel
pyramids, and behind each well a film of ember glass. Keys are Blades; at rest their glass is ember dark, on hover it
glows red, engaged it burns forge orange. The light is the bible's "bottom" option: a forge glow rising from strip
lamps under each well's lower lip, because the Warhost is lit like faces round a forge, from below, never from a calm
sky; a soft main light from the upper left still shades the wells' top lips, and a weak red underlight warms the steel.

The shapes come from assets/ui/krai/shapes.json, written and fit-checked by tools/mockups/krai_shapes.py (run it
first); this script models exactly those outlines through tools/blender/ui_shapes.py's Panel and renders them face on
in Cycles through the shared harness (tools/blender/ui_render.py) at twice the slot size, with a manifest. Every state
of a key, a tab or a bar cell is the same geometry and the same materials; only the light in its glass changes.

    python3 tools/mockups/krai_shapes.py
    /opt/blender/blender --background --factory-startup --python tools/blender/build_ui_krai.py -- \\
        --out assets/ui/krai [--only button.idle,crest] [--samples 48]
    python3 tools/mockups/krai_shapes.py --verify assets/ui/krai

Units: 1 px of the 1x slot is 1 mm, y down. Design language: openspec/changes/console-skins/design.md (Krai Warhost).
"""

import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import ui_render  # noqa: E402  (the render harness every Blender skin shares)
from ui_shapes import NodeKit, Panel, film_mat, link_layers, metal, printed  # noqa: E402  (shared modelling and shading)

SHAPES = json.load(open(os.path.join(ui_render.ROOT, "assets", "ui", "krai", "shapes.json")))
CELL = SHAPES["cell"]
SHAPES = SHAPES["pieces"]

# War is the only honest test: black steel (the forge), honed edge (readiness), blood enamel (honour paid in blood),
# red (rage), forge orange (the strike), bone (trophies). The palette and its meanings are in design.md.
PAL = dict(steel="#2b2628", honed="#c9d0d8", blood="#9a1016", red="#ff2a20", forge="#ff7a1a", ember="#3a0c0a",
           ember_lo="#080202", slot="#120505", wall="#0a0606")
# The films: (colour, its shadow in the well, emission). Ember at rest, red on hover, forge orange when engaged.
FILMS = dict(ember=("#2a0806", "#050101", 0.14), red=("#ff2a20", "#3a0505", 1.1), forge=("#ff7a1a", "#5a1a04", 1.7),
             slot=("#120505", "#050101", 0.2), lamp_green=("#5aff7a", "#0a3a12", 2.4), lamp_yellow=("#ffc23a", "#3a2a05", 2.4),
             lamp_red=("#ff2a20", "#3a0505", 2.4))
FORGE = (1.0, 0.42, 0.15)                        # the forge lamps' colour
FORGE_W = float(os.environ.get("FORGE_W", 0.0003))   # forge lamp power per mm of lower lip (times the lamp scale)
MAIN = (0.8, 60.0)                               # the soft main light on the wells: strength, angular size (deg)
DISPLAY = tuple(FILMS) + ("cellglass",)


def steel(name):
    """Blackened blade steel: dark, brushed along its length, a thin clear coat; its honed rolled edges catch the light."""
    k = NodeKit(name)
    g = k.tex("ShaderNodeTexWave", 700.0, **{"Distortion": 0.6, "Detail": 3.0})
    k.set(Base_Color=PAL["steel"], Metallic=0.9, Roughness=0.3, Coat_Weight=0.25, Coat_Roughness=0.08)
    k.bump(g.outputs["Fac"], 0.03)
    return k.mat


def enamel(name, col):
    """Red enamel: deep colour under a hard gloss coat."""
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=0.2, Coat_Weight=1.0, Coat_Roughness=0.04)
    return k.mat


def glass(name, col, es):
    """A bar cell's glass: coated, glowing in its own colour at es."""
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=0.2, Coat_Weight=1.0, Coat_Roughness=0.03, Emission_Color=col, Emission_Strength=es)
    return k.mat


def lower_lips(well):
    """The edges of a well whose inward normal points up the screen: the lower lip, where a forge lamp sits. Yields
    (x0, y0, x1, y1, nx, ny) per edge longer than 3 px (the outline's orientation gives the inward side)."""
    area = sum(well[i - 1][0] * well[i][1] - well[i][0] * well[i - 1][1] for i in range(len(well))) / 2
    side = 1.0 if area > 0 else -1.0   # y-down screen: a positive area runs clockwise on screen, inward is to the right
    run = []
    for i in range(len(well)):
        a, b = well[i], well[(i + 1) % len(well)]
        run.append((a, b))
    seg, acc = [], 0.0
    for a, b in run:
        tx, ty = b[0] - a[0], b[1] - a[1]
        n = math.hypot(tx, ty) or 1.0
        nx, ny = -ty / n * side, tx / n * side
        if ny < -0.85 and n > 1.0:   # the flat lower lips only: a glow rising evenly, no beam from a corner
            seg.append((a, b, nx, ny))
            acc += n
            if acc >= 10.0:
                yield from merge(seg)
                seg, acc = [], 0.0
        elif seg:
            yield from merge(seg)
            seg, acc = [], 0.0
    if seg:
        yield from merge(seg)


def merge(seg):
    (a, _b, _nx, _ny), (_a, b, _x, _y) = seg[0], seg[-1]
    nx, ny = sum(s[2] for s in seg) / len(seg), sum(s[3] for s in seg) / len(seg)
    n = math.hypot(nx, ny) or 1.0
    if math.hypot(b[0] - a[0], b[1] - a[1]) > 3.0:
        yield a[0], a[1], b[0], b[1], nx / n, ny / n


def mats(K, s):
    ao = min(7.0, -s["dz"] * 0.6)
    K.m.update(plate=K.mat("steel", lambda: steel("steel")), knob=K.mat("honed", lambda: metal("honed", PAL["honed"], 0.14)),
               bezel=K.mat("blood", lambda: enamel("blood", "#c4161f")), groove=K.mat("wall", lambda: printed("wall", PAL["wall"])),
               inlay=K.mat("inlay", lambda: enamel("inlay", PAL["blood"])))
    for kind, (col, lo, es) in FILMS.items():
        K.m[kind] = K.mat(kind, lambda col=col, lo=lo, es=es, kind=kind: film_mat(kind, col=col, shadow=lo, es=es, ao_mm=ao))


def piece(name):
    def build(K):
        s = SHAPES[name]
        K.roll, K.dz = s["roll"], s["dz"]
        K.pt = min(8.0, 2 * s["roll"] + 2.0)
        mats(K, s)
        wells = [(kind, [tuple(p) for p in w]) for kind, w in s["wells"]]
        plate = [tuple(p) for p in s["plate"]]
        for (kind, _w), f in zip(wells, s["films"]):  # films measured to fit inside the plate (rule B)
            K.film(None, shape=[tuple(p) for p in f], m=K.m[kind])
        K.plate([plate] + [w for _k, w in wells])
        for _kind, w in wells:
            gap = min(min(math.hypot(x - px, y - py) for px, py in plate) for x, y in w)
            if gap >= 5.0:  # a red enamel bezel, and a dark well wall where the land has room for them
                K.bezel(w, grow=1.0, wall=gap >= 7.0)
            for x0, y0, x1, y1, nx, ny in lower_lips(w):
                K.edge_lamp(x0, y0, x1, y1, nx, ny, FORGE_W, spread=120.0, depth=K.dz + min(3.0, -K.dz * 0.4), colour=FORGE)
        for g in s["inlays"]:   # enamel blades standing on flat metal (checked as raised plates)
            K.slab([[tuple(p) for p in g]], K.pt - 0.3, K.pt + 0.6, K.m["inlay"], bevel=0.25)
        for x, y, r in s["studs"]:   # honed pyramids: three faces, nothing round
            K.cyl(x, y, r, K.pt - 0.2, K.pt + r * 1.1, K.m["knob"], r1=0.08, seg=3, smooth=False)
        link_layers(DISPLAY, DISPLAY, main=MAIN)
    return build


def cell(col, es):
    """A symbol bar cell: a small blade chevron of glass in a dark steel setting. Off, on and warn share the outline,
    the setting and the coat; only the glass's colour and glow differ."""
    def build(K):
        out = [tuple(p) for p in CELL["outline"]]
        cx, cy = CELL["size"][0] / 2, CELL["size"][1] / 2
        inner = [(cx + (x - cx) * 0.72, cy + (y - cy) * 0.78) for x, y in out]
        setting = K.mat("setting", lambda: steel("setting"))
        K.slab([out], 0.0, 1.0, setting, bevel=0.3)
        K.slab([inner], 1.0, 1.5, K.mat("cellglass", lambda: glass("cellglass", col, es)), bevel=0.2)
    return build


def fill(col, es):
    """A bar's plain fill (the kit still carries one): a blade of light, a dim halo round a bright core."""
    def build(K):
        w, h = K.w, K.h
        halo = K.mat("halo", lambda: glass("halo", "#3a0c0a", 0.4))
        core = K.mat("core", lambda: glass("core", col, es))
        K.slab([[(0.5, 1), (w - 7, 1), (w - 0.5, h / 2), (w - 7, h - 1), (0.5, h - 1)]], 0.0, 0.2, halo, bevel=0.0)
        K.slab([[(2, 3), (w - 8, 3), (w - 3, h / 2), (w - 8, h - 3), (2, h - 3)]], 0.2, 0.4, core, bevel=0.0)
    return build


PIECES = {}
for name, s in SHAPES.items():
    PIECES[name] = (s["kind"], tuple(s["size"]), tuple(s["margins"]) if s["margins"] else None, s["stretch"], piece(name), s["inset"])
PIECES["bar.fill"] = ("nine", (64, 10), (0, 10, 0, 4), "x", fill(PAL["forge"], 3.0))
PIECES["bar.fill.red"] = ("nine", (64, 10), (0, 10, 0, 4), "x", fill(PAL["red"], 3.0))
CS = tuple(CELL["size"])
PIECES["bar.cell.off"] = ("cell", CS, None, None, cell(PAL["ember"], 0.3), CELL["gap"])
PIECES["bar.cell.on"] = ("cell", CS, None, None, cell(PAL["forge"], 3.2), CELL["gap"])
PIECES["bar.cell.warn"] = ("cell", CS, None, None, cell(PAL["red"], 3.2), CELL["gap"])
ORDER = ["frame.panel", "frame.inset", "bar.track", "bar.fill", "bar.fill.red", "button.idle", "button.hover", "button.pressed",
         "tab.idle", "tab.active", "strip.top", "strip.bottom", "divider", "nameplate", "helm.pad", "ssd.bezel", "dial",
         "lamp.alert.green", "lamp.alert.yellow", "lamp.alert.red", "crest", "bar.cell.off", "bar.cell.on", "bar.cell.warn"]
PIECES = {k: PIECES[k] for k in ORDER}

# A soft room light from the upper left on the steel, and a weak forge-red light rising from below (link_layers adds
# the softbox, the ceiling and the main light).
LIGHTS = [("Room", (0.3, -1.0, -1.6), 0.6, (1.0, 0.96, 0.92), 8.0), ("Forge", (0.0, 1.0, -0.9), 0.35, (1.0, 0.35, 0.15), 25.0)]


def render_setup(w_px, h_px, samples):
    ui_render.setup(w_px, h_px, samples, LIGHTS, world=(0.03, 0.02, 0.02),
                    bounces=dict(max_bounces=8, glossy_bounces=4, transmission_bounces=4, diffuse_bounces=3))


if __name__ == "__main__":
    ui_render.run("krai", PIECES, Panel, render_setup, samples=48)
