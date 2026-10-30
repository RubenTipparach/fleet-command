"""Fleet Command: the Kelvane Institute console skin, modelled, lit and rendered in Blender.

Owns every piece of the Kelvane skin of the shared console UI kit: an old-school laboratory dashboard (owner,
2026-10-30: "Kelvane 100% can be modeled, let and rendered just take the osciloscope dash board for inspiration,
old school electircal dashboards, dials, switches etc", after the Nucleo NLog Winamp skin's lab-instrument
feeling). Cream enamel faceplates with a faint orange-peel finish in polished chrome bezels, screwed down;
domed chrome push buttons in bakelite sockets with amber pilot rings; bat-handle toggle switches; a needle
meter under glass; knurled chrome knobs; amber backlit displays; phosphor-green oscilloscope screens with
their graticules; faceted jewel pilot lamps; engraved placards for icons. Lit by a warm lamp above the bench.
Rendered face on in Cycles through the shared harness (tools/blender/ui_render.py), from the shared shapes and
node helpers (tools/blender/ui_shapes.py), at twice the slot size, with a manifest of sizes and margins.

    /opt/blender/blender --background --factory-startup --python tools/blender/build_ui_kelvane.py -- \
        --out assets/ui/kelvane [--only button.idle,crest] [--samples 48]

Units: 1 px of the 1x slot is 1 mm; builders take slot pixels with y running down. 9-slice pieces keep their
fittings (screws, domes, meters, knobs) inside the margins; bars are tiles repeated along their length (one tick
of a ruled scale, one lit cell). Our own design throughout: nothing taken from any skin's art.
Kelvane design language: openspec/changes/console-skins/design.md.
"""

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import ui_render  # noqa: E402  (the render harness every Blender skin shares)
from ui_shapes import NodeKit, Shaper, arc, circle, rrect  # noqa: E402  (shared modelling and shading)

# ----------------------------------------------------------------------------- materials
# Measurement, patience, precision: cream enamel, chrome, amber light, phosphor green, needle red, ink.
PAL = dict(cream="#e9dcc0", chrome="#d8dcdf", brass="#c9a050", bakelite="#1a1612", amber="#ffcf6a", phos="#4dff88",
           needle="#c8302a", paper="#f2ead8", ink="#141210")


def enamel(name, col=PAL["cream"]):
    """Baked enamel: warm cream, a slightly uneven tone, a faint orange-peel finish under a soft gloss."""
    k = NodeKit(name)
    tone = k.tex("ShaderNodeTexNoise", 40.0, **{"Detail": 3.0})
    k.set(Base_Color=k.ramp(tone.outputs["Fac"], "#e0d1b0", col, 0.2, 0.8), Roughness=0.36, Coat_Weight=0.55, Coat_Roughness=0.1)
    peel = k.tex("ShaderNodeTexNoise", 700.0, **{"Detail": 2.0})
    k.bump(peel.outputs["Fac"], 0.06)
    return k.mat


def metal(name, col, rough, knurl=False):
    """Chrome or brass; with knurl, fine diagonal ridges for grip."""
    k = NodeKit(name)
    k.set(Base_Color=col, Metallic=1.0, Roughness=rough)
    if knurl:
        w = k.tex("ShaderNodeTexWave", 900.0, **{"Distortion": 0.0})
        k.bump(w.outputs["Fac"], 0.5)
    return k.mat


def plain(name, col, rough, coat=0.0, emit=None, es=0.0):
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=rough, Coat_Weight=coat, Coat_Roughness=0.05)
    if emit:
        k.set(Emission_Color=emit, Emission_Strength=es)
    return k.mat


def glass(name, tint="#ffffff", emit=None, es=0.0):
    """Clear glass (a meter's cover, a jewel lens); a jewel glows in its own colour."""
    k = NodeKit(name)
    k.set(Base_Color=tint, Roughness=0.03, Transmission_Weight=1.0, IOR=1.5, Coat_Weight=0.0)
    if emit:
        k.set(Emission_Color=emit, Emission_Strength=es)
    return k.mat


def amber_display(name, es):
    """A backlit amber window: warm light behind frosted glass, a little brighter at its middle."""
    k = NodeKit(name)
    lw = k.N.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.3
    k.set(Base_Color="#4a2a08", Roughness=0.15, Coat_Weight=1.0, Coat_Roughness=0.02, Emission_Color="#ffb43c",
          Emission_Strength=k.math("MULTIPLY", k.math("SUBTRACT", 1.1, lw.outputs["Facing"]), es))
    return k.mat


def crt(name):
    """A phosphor tube's face: deep green-black glass, its graticule glowing faintly, a soft bloom in the middle."""
    k = NodeKit(name)
    gx = k.tex("ShaderNodeTexWave", 11.0, **{"Distortion": 0.0}, wave_type="BANDS", bands_direction="X")
    gy = k.tex("ShaderNodeTexWave", 11.0, **{"Distortion": 0.0}, wave_type="BANDS", bands_direction="Y")
    line = k.math("MAXIMUM", k.math("GREATER_THAN", gx.outputs["Fac"], 0.96), k.math("GREATER_THAN", gy.outputs["Fac"], 0.96))
    k.set(Base_Color="#041008", Roughness=0.08, Coat_Weight=1.0, Coat_Roughness=0.02, Emission_Color=PAL["phos"],
          Emission_Strength=k.math("ADD", k.math("MULTIPLY", k.math("GREATER_THAN", k.math("MAXIMUM", gx.outputs["Fac"], gy.outputs["Fac"]), 0.985), 0.35), 0.04))
    return k.mat


def M(K, key):
    makers = {
        "enamel": lambda: enamel("enamel"), "paper": lambda: enamel("paper", PAL["paper"]),
        "chrome": lambda: metal("chrome", PAL["chrome"], 0.07), "knurl": lambda: metal("knurl", PAL["chrome"], 0.12, True),
        "brass": lambda: metal("brass", PAL["brass"], 0.25), "bakelite": lambda: plain("bakelite", PAL["bakelite"], 0.25, 0.6),
        "ink": lambda: plain("ink", PAL["ink"], 0.45), "needle": lambda: plain("needle", PAL["needle"], 0.35, 0.4),
        "pilot_off": lambda: plain("pilot_off", "#4a3a20", 0.3, 0.8), "pilot_on": lambda: plain("pilot_on", "#ffd27a", 0.2, 0.8, PAL["amber"], 7.0),
        "amber": lambda: amber_display("amber", 0.55), "amber_cell": lambda: amber_display("amber_cell", 1.2),
        "red_cell": lambda: plain("red_cell", "#5a1008", 0.2, 0.8, "#ff4a32", 4.0),
        "crt": lambda: crt("crt"), "glass": lambda: glass("glass"),
        "jewel_green": lambda: plain("jewel_green", "#2a8a4a", 0.05, 1.0, "#5aff8a", 3.0), "jewel_amber": lambda: plain("jewel_amber", "#a87a20", 0.05, 1.0, PAL["amber"], 3.0),
        "jewel_red": lambda: plain("jewel_red", "#8a2010", 0.05, 1.0, "#ff4a3a", 3.0), "jewel_off": lambda: plain("jewel_off", "#5a4020", 0.05, 1.0),
    }
    return K.mat(key, makers[key])


# ----------------------------------------------------------------------------- fittings

def faceplate(K, x0, y0, x1, y1, r, z=6.0, rim=3.0, holes=()):
    """A cream enamel plate in a polished chrome bezel, with windows cut through it for displays."""
    K.slab([rrect(x0, y0, x1, y1, r)] + list(holes), 0.0, z - 2.0, M(K, "chrome"), bevel=1.6)
    K.slab([rrect(x0 + rim, y0 + rim, x1 - rim, y1 - rim, max(1.0, r - rim))] + list(holes), z - 3.0, z, M(K, "enamel"), bevel=1.0)


def screw(K, x, y, z=6.0, r=2.6):
    """A chrome cheese-head screw with its slot."""
    K.cyl(x, y, r, z, z + 1.2, M(K, "chrome"), r1=r * 0.82, seg=24)
    K.slab([[(x - r * 0.75, y - 0.35), (x + r * 0.75, y - 0.45), (x + r * 0.75, y + 0.35), (x - r * 0.75, y + 0.45)]], z + 1.0, z + 1.4, M(K, "bakelite"), bevel=0.1)


def dome(K, x, y, r, pilot=False, sunk=False, z=6.0):
    """A domed chrome push button in a bakelite socket, an amber pilot ring round it."""
    K.cyl(x, y, r + 3.5, z - 1.0, z + 1.0, M(K, "bakelite"), seg=48)
    K.slab([circle(x, y, r + 2.6), circle(x, y, r + 0.9)], z + 0.8, z + 1.6, M(K, "pilot_on" if pilot else "pilot_off"), bevel=0.3)
    K.blob(x, y, r, r, r * 0.55, M(K, "chrome"), z=z + (0.2 if sunk else 1.6), seg=48)


def knob(K, x, y, r, z=6.0):
    """A knurled chrome knob with a polished cap and a pointer line."""
    K.cyl(x, y, r, z, z + 6.0, M(K, "knurl"), seg=64)
    K.cyl(x, y, r * 0.72, z + 6.0, z + 7.0, M(K, "chrome"), r1=r * 0.66, seg=64)
    K.tube([(x, y - r * 0.15), (x, y - r * 0.6)], [0.7, 0.7], M(K, "ink"), flat=0.6, z=[z + 7.2, z + 7.2], sharp=True)


def toggle(K, x, y, up, z=4.0):
    """A bat-handle toggle on a hex nut, thrown up or down."""
    K.cyl(x, y, 6.0, z, z + 1.8, M(K, "chrome"), seg=6, smooth=False)
    K.cyl(x, y, 3.0, z + 1.8, z + 3.0, M(K, "chrome"), seg=24)
    ty = y - 13 if up else y + 13
    K.tube([(x, y), (x, (y + ty) / 2), (x, ty)], [1.7, 1.6, 1.4], M(K, "chrome"), flat=1.0, z=[z + 3.0, z + 6.0, z + 9.0])
    K.blob(x, ty, 2.6, 2.6, 2.2, M(K, "chrome"), z=z + 9.0)


def meter(K, x0, y0, x1, y1, z=6.0, reading=0.62):
    """A needle meter under glass: a bakelite case, a paper scale with ruled ticks, a red needle."""
    K.slab([rrect(x0, y0, x1, y1, 3)], z - 1.0, z + 2.0, M(K, "bakelite"), bevel=0.8)
    K.slab([rrect(x0 + 2.5, y0 + 2.5, x1 - 2.5, y1 - 2.5, 2)], z + 1.0, z + 2.2, M(K, "paper"), bevel=0.3)
    cx, cy, R = (x0 + x1) / 2, y1 - 3.0, (y1 - y0) * 0.72
    for i in range(11):
        a = math.pi * (1.2 + 0.06 * i)
        r0 = R * (0.84 if i % 5 == 0 else 0.9)
        K.tube([(cx + math.cos(a) * r0, cy + math.sin(a) * r0), (cx + math.cos(a) * R, cy + math.sin(a) * R)], [0.35, 0.35], M(K, "ink"), flat=0.4, z=[z + 2.4] * 2, sharp=True)
    a = math.pi * (1.2 + 0.6 * reading)
    K.tube([(cx, cy), (cx + math.cos(a) * R * 0.96, cy + math.sin(a) * R * 0.96)], [0.6, 0.3], M(K, "needle"), flat=0.5, z=[z + 2.8] * 2, sharp=True)
    K.slab([rrect(x0 + 2.0, y0 + 2.0, x1 - 2.0, y1 - 2.0, 2)], z + 3.2, z + 3.8, M(K, "glass"), bevel=0.2)


def jewel(K, x, y, r, key, z=4.0):
    """A faceted jewel pilot lamp in a chrome bezel."""
    K.slab([circle(x, y, r + 3.0, 32), circle(x, y, r, 32)], z - 1.0, z + 2.0, M(K, "chrome"), bevel=0.8)
    K.blob(x, y, r, r, r * 0.6, M(K, key), z=z + 0.5, seg=10)


# ----------------------------------------------------------------------------- the pieces

def frame_panel(K):
    w, h = K.w, K.h
    faceplate(K, 0.5, 0.5, w - 0.5, h - 0.5, 12, holes=[rrect(18, 18, w - 18, h - 18, 6)])
    K.slab([rrect(16, 16, w - 16, h - 16, 7), rrect(19, 19, w - 19, h - 19, 5)], 3.0, 6.8, M(K, "chrome"), bevel=0.8)
    K.slab([rrect(18, 18, w - 18, h - 18, 6)], 1.0, 3.2, M(K, "amber"), bevel=0.4)
    for x, y in ((9, 9), (w - 9, 9), (9, h - 9), (w - 9, h - 9)):
        screw(K, x, y)


def frame_inset(K):
    w, h = K.w, K.h
    K.slab([rrect(0.5, 0.5, w - 0.5, h - 0.5, 7)], 0.0, 4.0, M(K, "chrome"), bevel=1.2)
    K.slab([rrect(4, 4, w - 4, h - 4, 5)], 2.0, 4.2, M(K, "amber"), bevel=0.4)


def bar_track(K):
    """One tile of a ruled scale: a dark channel and one tick top and bottom (the tile repeats)."""
    w, h = K.w, K.h
    K.slab([[(-2, -1), (w + 2, -1), (w + 2, h + 1), (-2, h + 1)]], 0.0, 3.0, M(K, "bakelite"), bevel=0.0)
    for y0, y1 in ((1.0, 4.0), (h - 4.0, h - 1.0)):
        K.slab([[(0.0, y0), (1.1, y0), (1.1, y1), (0.0, y1)]], 3.0, 3.4, M(K, "paper"), bevel=0.0)


def bar_fill(key):
    def build(K):
        w, h = K.w, K.h
        K.slab([[(-2, 0.5), (w - 1.2, 0.5), (w - 1.2, h - 0.5), (-2, h - 0.5)]], 0.0, 2.0, M(K, key), bevel=0.0)
    return build


def button(state):
    def build(K):
        w, h = K.w, K.h
        faceplate(K, 0.5, 2.5, w - 0.5, h - 2.5, 9)
        dome(K, 22, h / 2, 11.5, pilot=state != "idle", sunk=state == "pressed")
        K.tube([(41, 11), (41, h - 11)], [0.5, 0.5], M(K, "bakelite"), flat=0.4, z=[6.2, 6.2], sharp=True)
    return build


def tab(active):
    def build(K):
        w, h = K.w, K.h
        K.slab([rrect(1, 4, w - 1, h - 4, 6)], 0.0, 4.0, M(K, "enamel"), bevel=1.0)
        toggle(K, 13, h / 2, active)
        jewel(K, w - 10, h / 2, 3.4, "jewel_amber" if active else "jewel_off")
    return build


def strip(bottom):
    def build(K):
        w, h = K.w, K.h
        faceplate(K, 0.5, 0.5, w - 0.5, h - 0.5, 8, holes=[rrect(10, 9, 80, 47, 3)])
        for y in (8, h - 8):
            K.tube([(90, y), (w - 90, y)], [0.5, 0.5], M(K, "bakelite"), flat=0.4, z=[6.2, 6.2], sharp=True)
        meter(K, 10, 9, 80, 47)
        knob(K, w - 44, h / 2, 14)
        for y in (12, h - 12):
            screw(K, w - 12, y)
    return build


def divider(K):
    w, h = K.w, K.h
    for y in (3.5, 6.5):
        K.tube([(10, y), (w - 10, y)], [0.45, 0.45], M(K, "bakelite"), flat=0.5, sharp=True)
    for x in (5, w - 5):
        screw(K, x, h / 2, z=0.0, r=2.4)


def nameplate(K):
    w, h = K.w, K.h
    faceplate(K, 0.5, 0.5, w - 0.5, h - 0.5, 10, holes=[rrect(46, 9, w - 54, h - 9, 4)])
    K.slab([rrect(44, 7, w - 52, h - 7, 5), rrect(46, 9, w - 54, h - 9, 4)], 3.0, 6.8, M(K, "chrome"), bevel=0.6)
    K.slab([rrect(46, 9, w - 54, h - 9, 4)], 1.0, 3.2, M(K, "amber"), bevel=0.3)
    K.slab([rrect(10, 12, 34, h - 12, 2)], 5.0, 6.5, M(K, "brass"), bevel=0.4)
    for y in (h / 2 - 6, h / 2, h / 2 + 6):
        K.tube([(14, y), (30, y)], [0.4, 0.4], M(K, "ink"), flat=0.4, z=[6.6, 6.6], sharp=True)
    dome(K, w - 28, h / 2, 9, pilot=True)


def helm_pad(K):
    w, h = K.w, K.h
    c = w / 2
    faceplate(K, 0.5, 0.5, w - 0.5, h - 0.5, 18, rim=4.0)
    for i in range(48):
        a = i * math.pi / 24
        r0 = 118 if i % 6 == 0 else 123
        K.tube([(c + math.cos(a) * r0, c + math.sin(a) * r0), (c + math.cos(a) * 128, c + math.sin(a) * 128)], [0.45, 0.45], M(K, "ink"), flat=0.4, z=[6.2, 6.2], sharp=True)
    for i in range(8):
        a = -math.pi / 2 + i * math.pi / 4
        dome(K, c + math.cos(a) * 92, c + math.sin(a) * 92, 19, pilot=False)
    knob(K, c, c, 38)
    for x, y in ((16, 16), (w - 16, 16), (16, h - 16), (w - 16, h - 16)):
        screw(K, x, y)


def ssd_bezel(K):
    w, h = K.w, K.h
    faceplate(K, 0.5, 0.5, w - 0.5, h - 0.5, 14, rim=4.0, holes=[rrect(22, 18, w - 22, h - 18, 14)])
    K.slab([rrect(16, 12, w - 16, h - 12, 18), rrect(22, 18, w - 22, h - 18, 14)], 4.0, 9.0, M(K, "chrome"), bevel=1.4)
    K.slab([rrect(22, 18, w - 22, h - 18, 14)], 1.0, 4.4, M(K, "crt"), bevel=0.6)
    for x, y in ((9, 9), (w - 9, 9), (9, h - 9), (w - 9, h - 9)):
        screw(K, x, y, r=2.4)


def dial(K):
    w, h = K.w, K.h
    c = w / 2
    K.slab([circle(c, c, 58, 64), circle(c, c, 47, 64)], 0.0, 6.0, M(K, "chrome"), bevel=1.6)
    K.slab([circle(c, c, 48, 64)], 0.0, 3.0, M(K, "crt"), bevel=0.5)


def lamp(key):
    def build(K):
        c = K.w / 2
        jewel(K, c, c, 19, key)
    return build


def crest(K):
    w, h = K.w, K.h
    c = w / 2
    K.slab([circle(c, c, 46, 64)], 0.0, 3.0, M(K, "chrome"), bevel=1.2)
    K.slab([circle(c, c, 40, 64)], 2.0, 5.0, M(K, "enamel"), bevel=0.8)
    fork = [(c - 9, 20), (c - 9, 46)] + [(c + math.cos(a) * 9, 46 + math.sin(a) * 9) for a in [math.pi - i * math.pi / 8 for i in range(9)]] + [(c + 9, 20)]
    K.tube(fork, [1.6] * len(fork), M(K, "ink"), flat=0.6, z=[5.6] * len(fork), sharp=True)
    K.tube([(c, 55), (c, 74)], [1.6, 1.6], M(K, "ink"), flat=0.6, z=[5.6, 5.6], sharp=True)
    wave = [(c - 30 + i * 1.5, c + 2 + math.sin(i / 40 * 4 * math.pi) * 6) for i in range(41)]
    K.tube(wave, [1.1] * 41, M(K, "needle"), flat=0.6, z=[6.0] * 41)


# Icons: engraved placards, a black glyph raised on cream enamel in a chrome rim.
GLYPHS = {
    "helm": [arc(24, 28, 13, math.pi * 1.1, math.pi * 1.9), [(24, 28), (31, 17)]],
    "fire": [[(27, 8), (17, 25), (26, 25), (20, 40)]],
    "shields": [arc(24, 22, 11, math.pi, 2 * math.pi), [(13, 22), (13, 36)], [(35, 22), (35, 36)], [(10, 36), (16, 36)], [(32, 36), (38, 36)]],
    "tractor": [arc(24, 20, 9, math.pi, 2 * math.pi), [(15, 20), (15, 28)], [(33, 20), (33, 28)], arc(24, 34, 6, 0.2 * math.pi, 0.8 * math.pi, 8), arc(24, 34, 10, 0.2 * math.pi, 0.8 * math.pi, 8)],
    "transport": [arc(24, 20, 9, math.pi, 2 * math.pi), [(15, 20), (15, 34), (33, 34), (33, 20)], [(20, 38), (20, 42)], [(28, 38), (28, 42)], [(24, 16), (24, 28)]],
    "science": [[(20, 8), (20, 20), (11, 38), (37, 38), (28, 20), (28, 8)], [(17, 8), (31, 8)], [(15, 30), (33, 30)]],
    "comms": [[(8 + i, 24 + math.sin(i / 32 * 4 * math.pi) * 9) for i in range(33)]],
    "repair": [[(14, 34), (30, 18)], arc(33, 15, 6, 0.9 * math.pi, 2.6 * math.pi, 14), arc(13, 35, 3, 0, 2 * math.pi, 10)],
    "map": [[(8, 34), (40, 34)]] + [[(8 + i * 4, 34), (8 + i * 4, 30 if i % 2 else 26)] for i in range(9)] + [[(10, 14), (38, 14)], [(10, 10), (10, 18)], [(38, 10), (38, 18)]],
    "settings": [arc(24, 24, 10, 0, 2 * math.pi, 20), [(24, 24), (24, 15)]] + [[(24 + math.cos(a) * 14, 24 + math.sin(a) * 14), (24 + math.cos(a) * 18, 24 + math.sin(a) * 18)] for a in [math.pi * 0.75 + i * math.pi * 0.1875 for i in range(9)]],
}


def icon(name):
    def build(K):
        K.slab([rrect(1, 1, 47, 47, 8)], 0.0, 3.0, M(K, "chrome"), bevel=1.0)
        K.slab([rrect(3.5, 3.5, 44.5, 44.5, 6)], 2.0, 4.5, M(K, "enamel"), bevel=0.6)
        for line in GLYPHS[name]:
            K.tube(line, [1.15] * len(line), M(K, "ink"), flat=0.5, z=[4.8] * len(line), sharp=True)
    return build


ICONS = list(GLYPHS)
PIECES = {
    "frame.panel":    ("nine", (180, 140), (28, 28, 28, 28), "xy", frame_panel),
    "frame.inset":    ("nine", (72, 56), (14, 14, 14, 14), "xy", frame_inset),
    "bar.track":      ("nine", (8, 16), (0, 0, 0, 0), "x", bar_track),
    "bar.fill":       ("nine", (8, 10), (0, 0, 0, 0), "x", bar_fill("amber_cell")),
    "bar.fill.red":   ("nine", (8, 10), (0, 0, 0, 0), "x", bar_fill("red_cell")),
    "button.idle":    ("nine", (150, 48), (20, 16, 20, 46), "x", button("idle")),
    "button.hover":   ("nine", (150, 48), (20, 16, 20, 46), "x", button("hover")),
    "button.pressed": ("nine", (150, 48), (20, 16, 20, 46), "x", button("pressed")),
    "tab.idle":       ("nine", (120, 40), (14, 18, 14, 26), "x", tab(False)),
    "tab.active":     ("nine", (120, 40), (14, 18, 14, 26), "x", tab(True)),
    "strip.top":      ("nine", (512, 56), (10, 88, 10, 90), "x", strip(False)),
    "strip.bottom":   ("nine", (512, 56), (10, 88, 10, 90), "x", strip(True)),
    "divider":        ("nine", (128, 10), (0, 12, 0, 12), "x", divider),
    "nameplate":      ("hero", (460, 56), None, None, nameplate),
    "helm.pad":       ("hero", (300, 300), None, None, helm_pad),
    "ssd.bezel":      ("hero", (460, 300), None, None, ssd_bezel),
    "dial":           ("hero", (120, 120), None, None, dial),
    "lamp.alert.green": ("hero", (64, 64), None, None, lamp("jewel_green")),
    "lamp.alert.yellow": ("hero", (64, 64), None, None, lamp("jewel_amber")),
    "lamp.alert.red": ("hero", (64, 64), None, None, lamp("jewel_red")),
    "crest":          ("hero", (96, 96), None, None, crest),
}
PIECES.update({f"icon.{n}": ("hero", (48, 48), None, None, icon(n)) for n in ICONS})

# A warm lamp above the bench, a soft cool fill from the lower right, a warm grey room for the chrome to reflect.
LIGHTS = [("Lamp", (0.35, -1.0, -1.4), 2.3, (1.0, 0.9, 0.76), 10.0), ("Fill", (-0.8, 1.0, -0.9), 0.9, (0.8, 0.88, 1.0), 14.0)]


def render_setup(w_px, h_px, samples):
    ui_render.setup(w_px, h_px, samples, LIGHTS, world=(0.16, 0.14, 0.12), bounces=dict(max_bounces=8, glossy_bounces=4, transmission_bounces=4))
    # a softbox over the bench, out of the camera's view, for the chrome to reflect: domes and knobs read as polished
    import bpy
    ld = bpy.data.lights.new("Softbox", "AREA")
    ld.shape, ld.size, ld.energy, ld.color = "DISK", 0.6, 5.0, (1.0, 0.94, 0.85)
    ob = bpy.data.objects.new("Softbox", ld)
    ob.location = (-0.25, 0.35, 0.6)
    ob.rotation_euler = (math.radians(-35), math.radians(-20), 0.0)
    bpy.context.scene.collection.objects.link(ob)


if __name__ == "__main__":
    ui_render.run("kelvane", PIECES, Shaper, render_setup, samples=48)
