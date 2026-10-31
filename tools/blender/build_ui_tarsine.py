"""Fleet Command: the Tarsine Remnant console skin, modelled, lit and rendered in Blender.

Owns every piece of the Remnant skin of the shared console UI kit, rebuilt on the analog panel bible (owner, 2026-10-31:
"I love the Remnant ui, i htink it could use some polish from our guide, but please dont remove the grimey industrial
feel, I love that story telling so much"; docs/design/ui-bible.html and the blender-ui-panels skill). The ancient race's
language (design.md: "a powerful ancient race. rusty, with back lit buttons clogged with dirt. their ships havent been
upgraded in ages, rough around the edges") in the bible's two layers:

- the metal: monumental corroded iron whose every edge rolls over in a worn round, rust blooming through it, verdigris
  in patches, pitting, soot pooled in every crevice (the plate's own ambient occlusion decides where grime collects),
  great domed rivets with rust streaks running down from them onto the flat metal, worn gold bezels, brackets and crown;
- the display: grimy glass lit from behind. A layer of caked dirt lies on every window's glass, the same dirt in every
  state of a key, a tab and a bar cell (owner: "sync up dust with out and unlit button to give it that illusion"): only
  the backlight behind it changes. The gauges' glass is cracked.

The light is the bible's "spots" option, dimmed and uneven: one or two old lamps under each window's top lip, some
weaker than others (deterministic per piece), because the Remnant's fixtures were never replaced and not all of them
still burn; a weak warm room light from the upper left shows the corrosion, and the soft main light shades the lips.

The shapes come from assets/ui/tarsine/shapes.json, written and fit-checked by tools/mockups/tarsine_shapes.py (run it
first); this script models exactly those outlines through tools/blender/ui_shapes.py's Panel and renders them face on in
Cycles through the shared harness (tools/blender/ui_render.py) at twice the slot size, with a manifest.

    python3 tools/mockups/tarsine_shapes.py
    /opt/blender/blender --background --factory-startup --python tools/blender/build_ui_tarsine.py -- \\
        --out assets/ui/tarsine [--only button.idle,crest] [--samples 48]
    python3 tools/mockups/tarsine_shapes.py --verify assets/ui/tarsine

Units: 1 px of the 1x slot is 1 mm, y down. Design language: openspec/changes/console-skins/design.md (Tarsine Remnant).
"""

import json
import math
import os
import sys
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import ui_render  # noqa: E402  (the render harness every Blender skin shares)
from panel_kit import srgb  # noqa: E402
from ui_shapes import MM, NodeKit, Panel, film_mat, link_layers, printed  # noqa: E402  (shared modelling and shading)

SHAPES = json.load(open(os.path.join(ui_render.ROOT, "assets", "ui", "tarsine", "shapes.json")))
CELL = SHAPES["cell"]
SHAPES = SHAPES["pieces"]

# A fallen empire that will not change: soot (ages of use), rust (decline they will not admit), verdigris (time), old
# gold (old majesty), backlight (the fire still burning), ember (anger, slow to wake). Palette and meanings: design.md.
PAL = dict(iron="#40372f", iron_lo="#2a241e", rust="#6e3418", rust_hi="#a5562a", verd="#4b6656", soot="#15110d",
           gold="#a88a46", light="#d6c76a", ember="#d2482a", bone="#e8dcc0")
# The films behind the glass: (colour, its shadow at the rim, emission). Dark glass on frames; keys glow dimly at rest
# (their ink must read), brighter on hover, brightest pressed; the helm's centre is a hearth.
FILMS = dict(dim=("#3a3322", "#0a0806", 0.22), low=(PAL["light"], "#2a2412", 0.5), warm=(PAL["light"], "#3a3216", 0.9),
             hot=("#ffe08a", "#4a3a14", 1.45), hearth=("#ffd36a", "#4a3410", 1.25), gold=("#e2c67a", "#3a2c10", 1.05),
             slot=("#3a3020", "#0c0a08", 0.12), lamp_green=("#8ad46a", "#1a3010", 1.8), lamp_yellow=(PAL["light"], "#3a3010", 1.8),
             lamp_red=(PAL["ember"], "#3a1006", 1.8))
KEY_FILMS = {"dim": "low"}   # a key at rest glows a little (its window is never dark, or its label would not read)
LAMP = (1.0, 0.78, 0.48)                         # the old lamps' colour
LAMP_P = float(os.environ.get("LAMP_P", 0.22))   # their power (times the lamp scale), before each lamp's own age
MAIN = (0.7, 60.0)                               # the soft main light on the wells: strength, angular size (deg)
INKS = ("dirt", "crack", "cage")
DISPLAY = tuple(FILMS) + INKS + ("cell_off", "cell_on", "cell_warn", "fill_light", "fill_ember")


def mix(k, fac, a, b):
    """Mix two colours (hex or sockets) by fac (a socket or a number)."""
    m = k.N.new("ShaderNodeMix")
    m.data_type = "RGBA"
    for i, v in ((6, a), (7, b)):
        if isinstance(v, str):
            m.inputs[i].default_value = list(srgb(v)) + [1.0]
        else:
            k.L.new(v, m.inputs[i])
    if isinstance(fac, (int, float)):
        m.inputs["Factor"].default_value = fac
    else:
        k.L.new(fac, m.inputs["Factor"])
    return m.outputs[2]


def band(k, v, lo, hi):
    """0 below lo, 1 above hi, linear between: a soft threshold on a texture value."""
    return k.math("DIVIDE", k.math("SUBTRACT", v, lo), hi - lo, clamp=True)


def ao(k, mm):
    n = k.N.new("ShaderNodeAmbientOcclusion")
    n.samples, n.only_local = 12, True
    n.inputs["Distance"].default_value = mm * MM
    return n.outputs["AO"]


def iron(name, tone=0.0):
    """Corroded monumental iron: rust blooming through dark iron, verdigris in patches, fine pitting, and soot pooled
    wherever the plate's own occlusion says grime would collect (crevices, the foot of a lip, round rivets)."""
    k = NodeKit(name)
    v = k.warped(40.0, 0.004)
    rust_n = k.tex("ShaderNodeTexNoise", 55.0, vec=v, **{"Detail": 8.0, "Roughness": 0.62}).outputs["Fac"]
    verd_n = k.tex("ShaderNodeTexNoise", 28.0, vec=v, **{"Detail": 4.0}).outputs["Fac"]
    grain = k.tex("ShaderNodeTexNoise", 420.0, **{"Detail": 2.0}).outputs["Fac"]
    pits = k.tex("ShaderNodeTexVoronoi", 320.0).outputs["Distance"]
    rust = band(k, rust_n, 0.50 - tone, 0.62 - tone)
    verd = k.math("MULTIPLY", band(k, verd_n, 0.60, 0.68), 0.7)
    pit = band(k, k.math("SUBTRACT", 0.16, pits), 0.0, 0.12)
    grime = k.math("SUBTRACT", 1.0, k.math("POWER", ao(k, 4.0), 1.6), clamp=True)
    c = mix(k, grain, PAL["iron_lo"], PAL["iron"])
    c = mix(k, rust, c, PAL["rust"])
    c = mix(k, k.math("MULTIPLY", rust, band(k, grain, 0.55, 0.7)), c, PAL["rust_hi"])
    c = mix(k, verd, c, PAL["verd"])
    c = mix(k, k.math("MULTIPLY", grime, 0.9), c, PAL["soot"])
    c = mix(k, k.math("MULTIPLY", pit, 0.7), c, "#0e0b08")
    k.set(Base_Color=c, Metallic=k.math("MULTIPLY", k.math("SUBTRACT", 1.0, rust), 0.6),
          Roughness=k.math("ADD", 0.48, k.math("MULTIPLY", rust, 0.4)))
    k.bump(k.math("ADD", k.math("MULTIPLY", rust_n, 0.6), k.math("MULTIPLY", pit, -0.5)), 0.35)
    return k.mat


def gold(name):
    """Old gold worn through: polished where hands rubbed it, sooty in its hollows, flecked with rust from the iron."""
    k = NodeKit(name)
    n = k.tex("ShaderNodeTexNoise", 70.0, **{"Detail": 6.0}).outputs["Fac"]
    grime = k.math("SUBTRACT", 1.0, k.math("POWER", ao(k, 2.5), 1.4), clamp=True)
    c = mix(k, band(k, n, 0.58, 0.7), PAL["gold"], PAL["rust"])
    c = mix(k, k.math("MULTIPLY", grime, 0.85), c, PAL["soot"])
    k.set(Base_Color=c, Metallic=0.9, Roughness=k.math("ADD", 0.32, k.math("MULTIPLY", n, 0.25)))
    k.bump(n, 0.15)
    return k.mat


def dirt(name):
    """The caked dirt on every window's glass: patches, specks and a smear, translucent; one material, one place, so a
    window's dirt is the same lit or unlit (only the backlight behind it changes)."""
    k = NodeKit(name)
    caked = k.tex("ShaderNodeTexNoise", 48.0, vec=k.warped(20.0, 0.003), **{"Detail": 6.0}).outputs["Fac"]
    specks = k.tex("ShaderNodeTexNoise", 650.0, **{"Detail": 1.0}).outputs["Fac"]
    a = k.math("MAXIMUM", k.math("MULTIPLY", band(k, caked, 0.46, 0.7), 0.78), k.math("MULTIPLY", band(k, specks, 0.62, 0.7), 0.85))
    k.set(Base_Color="#2a1e12", Roughness=0.95, Alpha=a)
    return k.mat


def streak(name):
    """A rust streak: rust colour, strongest at its top (the rivet) and fading down, broken by noise."""
    k = NodeKit(name)
    gen = k.N.new("ShaderNodeTexCoord").outputs["Generated"]
    sep = k.N.new("ShaderNodeSeparateXYZ")
    k.L.new(gen, sep.inputs[0])
    n = k.tex("ShaderNodeTexNoise", 160.0, **{"Detail": 3.0}).outputs["Fac"]
    a = k.math("MULTIPLY", k.math("POWER", sep.outputs["Y"], 1.4), k.math("MULTIPLY", band(k, n, 0.35, 0.65), 0.75))
    k.set(Base_Color=PAL["rust_hi"], Roughness=0.85, Alpha=a)
    return k.mat


def glow(name, col, es):
    """Plain light: a bar fill or a cell's glass, glowing in its own colour."""
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=0.5, Coat_Weight=0.6, Coat_Roughness=0.1, Emission_Color=col, Emission_Strength=es)
    return k.mat


def mats(K, s):
    a = min(7.0, -s["dz"] * 0.6)
    K.m.update(plate=K.mat("iron", lambda: iron("iron")), knob=K.mat("rivet", lambda: iron("rivet", 0.06)),
               bezel=K.mat("gold", lambda: gold("gold")), groove=K.mat("wall", lambda: printed("wall", "#0c0907")),
               dirt=K.mat("dirt", lambda: dirt("dirt")), streak=K.mat("streak", lambda: streak("streak")),
               crack=K.mat("crack", lambda: glow("crack", PAL["bone"], 0.35)), cage=K.mat("cage", lambda: printed("cage", "#1a140e")))
    for kind, (col, lo, es) in FILMS.items():
        K.m[kind] = K.mat(kind, lambda col=col, lo=lo, es=es, kind=kind: film_mat(kind, col=col, shadow=lo, es=es, ao_mm=a))


def age(name, i):
    """How much an old lamp still gives, 0.35-1: fixed per piece (keys share one across their states) and lamp."""
    base = name.split(".")[0] + (name.split(".")[1] if name.startswith(("strip", "lamp", "frame")) else "")
    return 0.35 + 0.65 * ((zlib.crc32(f"{base}{i}".encode()) % 1000) / 1000)


def piece(name):
    def build(K):
        s = SHAPES[name]
        K.roll, K.dz = s["roll"], s["dz"]
        K.pt = min(8.0, 2 * s["roll"] + 2.0)
        mats(K, s)
        is_key = name.startswith("button.")
        wells = [(KEY_FILMS.get(kind, kind) if is_key else kind, [tuple(p) for p in w]) for kind, w in s["wells"]]
        plate = [tuple(p) for p in s["plate"]]
        for (kind, _w), f in zip(wells, s["films"]):  # films measured to fit inside the plate (rule B), and their dirt
            shape = [tuple(p) for p in f]
            K.film(None, shape=shape, m=K.m[kind])
            K.slab([shape], K.dz + 0.12, K.dz + 0.16, K.m["dirt"], bevel=0.0).visible_shadow = False
        K.plate([plate] + [w for _k, w in wells])
        lamp = 0
        for kind, w in wells:
            gap = min(min(math.hypot(x - px, y - py) for px, py in plate) for x, y in w)
            if gap >= 5.0:  # a worn gold bezel, and a dark well wall where the land has room for them
                K.bezel(w, grow=1.0, wall=gap >= 7.0)
            if kind.startswith("lamp_") or kind == "slot":
                continue
            xs, ys = [p[0] for p in w], [p[1] for p in w]
            x0, x1, y0 = min(xs), max(xs), min(ys)
            n = 2 if x1 - x0 > 140 else 1
            for j in range(n):   # old lamps under the top lip; not all of them still give their full light
                dark = 0.35 if kind in ("dim", "slot") else 1.0   # dark glass takes only a glimmer
                K.lip_light(x0 + (x1 - x0) * (j + 1) / (n + 1), y0 + 1.5, LAMP_P * age(name, lamp) * dark, colour=LAMP)
                lamp += 1
        for g in s["cracks"]:   # cracks in the glass (and a lamp's cage bars), printed on the film
            K.flat([[tuple(p) for p in g]], K.m["cage" if name.startswith("lamp") else "crack"], z=K.dz + 0.2)
        for g in s["gold"]:      # worn gold brackets and the crown, standing on the flat iron
            K.slab([[tuple(p) for p in g]], K.pt - 0.3, K.pt + 1.0, K.m["bezel"], bevel=0.45)
        for g in s["streaks"]:   # rust running down from the rivets, on the flat metal only
            K.slab([[tuple(p) for p in g]], K.pt + 0.01, K.pt + 0.03, K.m["streak"], bevel=0.0).visible_shadow = False
        for x, y, r in s["rivets"]:   # great domed rivets
            K.blob(x, y, r, r, r * 0.55, K.m["knob"], z=K.pt - 0.2)
        link_layers(DISPLAY, tuple(FILMS) + ("crack",), main=MAIN)
    return build


def cell(film):
    """A symbol bar cell: a little arched window of glass in an iron setting, the same caked dirt on its glass in every
    state; off, on and warn differ only in the light behind the glass."""
    def build(K):
        out = [tuple(p) for p in CELL["outline"]]
        cx, cy = CELL["size"][0] / 2, CELL["size"][1] / 2
        inner = [(cx + (x - cx) * 0.74, cy + (y - cy) * 0.82) for x, y in out]
        col, es = {"off": (PAL["light"], 0.06), "on": (PAL["light"], 2.2), "warn": (PAL["ember"], 2.2)}[film]
        K.slab([out], 0.0, 1.0, K.mat("iron", lambda: iron("iron")), bevel=0.3)
        K.slab([inner], 1.0, 1.4, K.mat(f"cell_{film}", lambda: glow(f"cell_{film}", col, es)), bevel=0.15)
        K.slab([inner], 1.45, 1.5, K.mat("dirt", lambda: dirt("dirt")), bevel=0.0).visible_shadow = False
    return build


def fill(col, es, key):
    """A bar's plain fill (the kit still carries one): light through grimy glass."""
    def build(K):
        w, h = K.w, K.h
        K.slab([[(0.5, 1), (w - 0.5, 1), (w - 0.5, h - 1), (0.5, h - 1)]], 0.0, 0.4, K.mat(key, lambda: glow(key, col, es)), bevel=0.0)
        K.slab([[(0.5, 1), (w - 0.5, 1), (w - 0.5, h - 1), (0.5, h - 1)]], 0.45, 0.5, K.mat("dirt", lambda: dirt("dirt")), bevel=0.0)
    return build


PIECES = {}
for name, s in SHAPES.items():
    PIECES[name] = (s["kind"], tuple(s["size"]), tuple(s["margins"]) if s["margins"] else None, s["stretch"], piece(name), s["inset"])
PIECES["bar.fill"] = ("nine", (64, 10), (0, 6, 0, 6), "x", fill(PAL["light"], 2.0, "fill_light"))
PIECES["bar.fill.red"] = ("nine", (64, 10), (0, 6, 0, 6), "x", fill(PAL["ember"], 2.0, "fill_ember"))
CS = tuple(CELL["size"])
for st in ("off", "on", "warn"):
    PIECES[f"bar.cell.{st}"] = ("cell", CS, None, None, cell(st), CELL["gap"])
ORDER = ["frame.panel", "frame.inset", "bar.track", "bar.fill", "bar.fill.red", "button.idle", "button.hover", "button.pressed",
         "tab.idle", "tab.active", "strip.top", "strip.bottom", "divider", "nameplate", "helm.pad", "ssd.bezel", "dial",
         "lamp.alert.green", "lamp.alert.yellow", "lamp.alert.red", "crest", "bar.cell.off", "bar.cell.on", "bar.cell.warn"]
PIECES = {k: PIECES[k] for k in ORDER}

# A weak warm room light from the upper left, enough to show the corrosion; link_layers adds the softbox, the ceiling and
# the main light. The world is dim: these consoles sit in the dark of an old ship.
LIGHTS = [("Room", (0.3, -1.0, -1.6), 0.55, (1.0, 0.9, 0.76), 10.0)]


def render_setup(w_px, h_px, samples):
    ui_render.setup(w_px, h_px, samples, LIGHTS, world=(0.025, 0.02, 0.016),
                    bounces=dict(max_bounces=8, glossy_bounces=4, transmission_bounces=4, diffuse_bounces=3))


if __name__ == "__main__":
    ui_render.run("tarsine", PIECES, Panel, render_setup, samples=48)
