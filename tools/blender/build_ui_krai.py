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
from ui_shapes import MM, NodeKit, Panel, film_mat, link_layers, lip_runs, metal, printed  # noqa: E402  (shared modelling and shading)

SHAPES = json.load(open(os.path.join(ui_render.ROOT, "assets", "ui", "krai", "shapes.json")))
CELL = SHAPES["cell"]
SHAPES = SHAPES["pieces"]

# War is the only honest test: black steel (the forge), honed edge (readiness), blood enamel (honour paid in blood),
# red (rage), forge orange (the strike), bone (trophies). The palette and its meanings are in design.md.
PAL = dict(steel="#2b2628", basalt="#1a1718", gunmetal="#55504f", magma="#ff3a0c", honed="#c9d0d8", blood="#9a1016", red="#ff2a20", forge="#ff7a1a", ember="#3a0c0a",
           ember_lo="#080202", slot="#120505", wall="#0a0606")
# The films: (colour, its shadow in the well, emission). Ember at rest, red on hover, forge orange when engaged.
FILMS = dict(ember=("#2a0806", "#050101", 0.14), red=("#ff2a20", "#3a0505", 1.1), forge=("#ff7a1a", "#5a1a04", 1.7),
             slot=("#120505", "#050101", 0.2), lamp_green=("#5aff7a", "#0a3a12", 2.4), lamp_yellow=("#ffc23a", "#3a2a05", 2.4),
             lamp_red=("#ff2a20", "#3a0505", 2.4))
FORGE = (1.0, 0.42, 0.15)                        # the forge lamps' colour
FORGE_W = float(os.environ.get("FORGE_W", 0.0003))   # forge lamp power per mm of lower lip (times the lamp scale)
MAGMA_ES = float(os.environ.get("MAGMA_ES", 0.7))   # the fissures' ember glow at full (a dim seam, never a readout)
PRESS_MM = 3.0   # how far an engaged key's glass is pushed in (owner: "the inner part is pushed in and the frame stays put");
# krai.js sinks the label 2 x --btn-sink = 3 px to match
MAIN = (0.8, 60.0)                               # the soft main light on the wells: strength, angular size (deg)
DISPLAY = tuple(FILMS) + ("cellglass",)


def steel(name):
    """Cast volcanic gunmetal (owner, 2026-10-31: "something like a rough gun metal, maybe sort of akin to volcanic magma
    type metal"): near-black basalt-grey metal poured like lava and left to cool, its face a crust of cooled plates
    split by fissures, coarse pitting all over, roughness that wanders from satin to raw. Deep in the fissures and the
    crevices the metal is still hot: a thin, dim ember glow (emission from the fissure mask times the plate's own
    occlusion, kept far below the readouts). Its rolled edges are worn bright where hands and blades rub them: the
    geometry's own tilt (its normal leaving the face) picks out the top of every roll as a band of honed bare steel."""
    k = NodeKit(name)
    v = k.warped(30.0, 0.004)
    crust = k.tex("ShaderNodeTexVoronoi", 50.0, vec=v, feature="DISTANCE_TO_EDGE")
    fiss = k.math("SUBTRACT", 1.0, k.math("DIVIDE", crust.outputs["Distance"], 0.03, clamp=True), clamp=True)
    tone = k.tex("ShaderNodeTexNoise", 60.0, vec=v, **{"Detail": 8.0, "Roughness": 0.65}).outputs["Fac"]
    pits = k.math("SUBTRACT", 1.0, k.math("DIVIDE", k.tex("ShaderNodeTexVoronoi", 520.0).outputs["Distance"], 0.35, clamp=True), clamp=True)
    grain = k.tex("ShaderNodeTexNoise", 900.0, **{"Detail": 2.0}).outputs["Fac"]
    occ = k.N.new("ShaderNodeAmbientOcclusion")
    occ.samples, occ.only_local = 12, True
    occ.inputs["Distance"].default_value = 3.0 * 0.001
    cavity = k.math("SUBTRACT", 1.0, k.math("POWER", occ.outputs["AO"], 1.5), clamp=True)
    geo = k.N.new("ShaderNodeNewGeometry")
    sep = k.N.new("ShaderNodeSeparateXYZ")
    k.L.new(geo.outputs["Normal"], sep.inputs[0])
    tilt = k.math("SUBTRACT", 1.0, sep.outputs["Z"], clamp=True)          # 0 on the face, rising round the roll
    wear = k.math("MULTIPLY", k.math("DIVIDE", k.math("SUBTRACT", tilt, 0.01), 0.1, clamp=True),
                  k.math("SUBTRACT", 1.0, k.math("DIVIDE", k.math("SUBTRACT", tilt, 0.7), 0.3, clamp=True), clamp=True))
    wear = k.math("MULTIPLY", wear, k.math("ADD", 0.55, k.math("MULTIPLY", tone, 0.6)), clamp=True)
    base = k.ramp(k.math("ADD", k.math("MULTIPLY", tone, 0.8), k.math("MULTIPLY", grain, 0.2)), PAL["basalt"], PAL["gunmetal"], 0.3, 0.75)
    m = k.N.new("ShaderNodeMix")
    m.data_type = "RGBA"
    k.L.new(wear, m.inputs["Factor"])
    k.L.new(base, m.inputs[6])
    m.inputs[7].default_value = (0.75, 0.74, 0.76, 1.0)
    dark = k.N.new("ShaderNodeMix")    # soot in the fissures and pits
    dark.data_type = "RGBA"
    k.L.new(k.math("MAXIMUM", k.math("MULTIPLY", fiss, 0.9), k.math("MULTIPLY", pits, 0.6)), dark.inputs["Factor"])
    k.L.new(m.outputs[2], dark.inputs[6])
    dark.inputs[7].default_value = (0.004, 0.003, 0.003, 1.0)
    glow = k.math("MULTIPLY", k.math("MULTIPLY", fiss, k.math("ADD", 0.25, cavity)),
                  k.math("DIVIDE", k.math("SUBTRACT", tone, 0.45), 0.25, clamp=True))
    rough = k.math("ADD", 0.5, k.math("MULTIPLY", tone, 0.42))
    rough = k.math("SUBTRACT", rough, k.math("MULTIPLY", wear, 0.45))
    k.set(Base_Color=dark.outputs[2], Metallic=0.85, Roughness=rough, Emission_Color=PAL["magma"],
          Emission_Strength=k.math("MULTIPLY", glow, MAGMA_ES))
    k.bump(k.math("ADD", k.math("MULTIPLY", fiss, -0.6), k.math("ADD", k.math("MULTIPLY", pits, -0.5), k.math("MULTIPLY", tone, 0.4))), 0.6)
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
    """The well's flat lower lips, where a forge lamp sits (ui_shapes.lip_runs): edges longer than 1 px whose inward
    normal points up the screen, in runs of about 10 px, a glow rising evenly with no beam from a corner."""
    return lip_runs(well, 10.0, faces=lambda nx, ny, ln: ny < -0.85 and ln > 1.0, min_len=3.0)


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
        sink = PRESS_MM if name == "button.pressed" else 0.0
        for (kind, _w), f in zip(wells, s["films"]):  # films measured to fit inside the plate (rule B), even pressed in
            glass = K.film(None, shape=[tuple(p) for p in f], m=K.m[kind])
            glass.location.y -= sink * MM   # an engaged key's glass is pushed in: down the screen and back, frame still
            glass.location.z -= sink * MM
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
