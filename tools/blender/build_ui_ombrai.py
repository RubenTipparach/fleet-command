"""Fleet Command: the Ombrai console skin, modelled, lit and rendered in Blender.

Owns every piece of the Ombrai skin of the shared console UI kit, rebuilt on the analog panel bible (owner, 2026-10-31:
"we need to update krai, onbrai, and hunt to use the new guildine standards ... Please make them look nice"; the
Ombrai's own words, 2026-10-30: "an enigmatic alien race with cool dark purple colors and pink buttons, progress bars
are curcular, almost organic like eyes"). Every piece is the bible's two-layer panel in the Ombrai's materials: a
pillowed plate of anodised plum metal with a thin-film sheen, every edge rolled over (footprint-true), lilac bezels
under the rolls, pink eye jewels set on flat metal, and formed wells cut through it: keys bitten like a crescent moon,
petals round the helm's pupil, frame wells bitten at the corners, an eye-shaped ship display under two pressed lids.
Behind each well is a violet film; eyes are printed on it (an almond lid, an iris ring, a slit pupil) and glow pink
when lit.

The light is the bible's **side** option: strip lamps tucked under the left lip of every well, aimed across the film.
The Ombrai watch and are seldom seen: each well is lit the way a crescent moon is, from one side, its far side left in
shadow, so every display is half veiled. No other skin is side-lit (the Aelith are rim-lit, the Kelvane spot-lit from
above, the Krai lit from below).

The shapes come from assets/ui/ombrai/shapes.json, written and fit-checked by tools/mockups/ombrai_shapes.py (run it
first); this script models exactly those outlines through tools/blender/ui_shapes.py's Panel and renders face on in
Cycles through the shared harness (tools/blender/ui_render.py) at twice the slot size, with a manifest. The states of
one piece share every outline and material; only the light level differs.

    python3 tools/mockups/ombrai_shapes.py
    /opt/blender/blender --background --factory-startup --python tools/blender/build_ui_ombrai.py -- \\
        --out assets/ui/ombrai [--only button.idle,crest] [--samples 48]
    python3 tools/mockups/ombrai_shapes.py --verify assets/ui/ombrai

Units: 1 px of the 1x slot is 1 mm, y down. Design language: openspec/changes/console-skins/design.md (Ombrai).
"""

import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import ui_render  # noqa: E402  (the render harness every Blender skin shares)
from ui_shapes import NodeKit, Panel, film_ink, film_mat, link_layers, lip_runs, metal, printed, rrect  # noqa: E402

SHAPES = json.load(open(os.path.join(ui_render.ROOT, "assets", "ui", "ombrai", "shapes.json")))["pieces"]

# Secrecy, the dream, veils, the watching gaze, memory, hostility (design.md): void, plum, smoke, pink, lilac, crimson.
PAL = dict(plum="#3b2a5c", lilac="#b99ce8", void="#1d0d33", void_lo="#05010b", wall="#0a0414", pink="#ff4fb0",
           pale="#ffd9f0", crimson="#ff2f5a", key="#e2479f", key_hot="#ec6ab8")
MOON = {"green": "#7affc8", "yellow": "#ffd36a", "red": "#ff2f5a"}
SIDE = (1.0, 0.42, 0.82)                                    # the side lamps: pink-violet
SIDE_W = float(os.environ.get("SIDE_W", 0.0045))            # their power per mm of left lip (times the lamp scale)
MAIN = (float(os.environ.get("MAIN_S", 0.4)), 60.0)                                          # the soft main light on the wells: strength, size (deg)
DISPLAY = ("void", "eyefilm", "keyfilm", "line", "moon_green", "moon_yellow", "moon_red", "lid", "iris", "pupil", "shade")


def plum(name):
    """Anodised plum metal: dark violet, a fine satin grain, a clear coat with a thin-film sheen that turns rose and
    teal as the rolled edges catch the light (the faint iridescence the Ombrai's glass had)."""
    k = NodeKit(name)
    g = k.tex("ShaderNodeTexWave", 900.0, **{"Distortion": 1.5, "Detail": 2.0})
    k.set(Base_Color=PAL["plum"], Metallic=0.85, Roughness=0.3, Coat_Weight=0.6, Coat_Roughness=0.08,
          Thin_Film_Thickness=420.0, Thin_Film_IOR=1.5)
    k.bump(g.outputs["Fac"], 0.03)
    return k.mat


def glass(name, base, col, es, ao_mm):
    """A key's or a tab's film: dark violet glass (the same in every state) that glows col at es from behind, darkened
    by occlusion at its rim like the bible's film. Only es changes between idle, hover and pressed."""
    k = NodeKit(name)
    ao = k.N.new("ShaderNodeAmbientOcclusion")
    ao.samples, ao.only_local = 16, False
    ao.inputs["Distance"].default_value = ao_mm * 0.001
    occ = k.math("POWER", ao.outputs["AO"], 2.2)
    k.set(Base_Color=k.ramp(occ, "#07020c", base), Roughness=0.55, Emission_Color=col, Emission_Strength=k.math("MULTIPLY", occ, es))
    return k.mat


def jewel(name, col, es):
    """An eye jewel: deep pink glass, coated, glowing from within."""
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=0.06, Coat_Weight=1.0, Coat_Roughness=0.02, Emission_Color=col, Emission_Strength=es)
    return k.mat


def glow(name, col, es):
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=0.5, Emission_Color=col, Emission_Strength=es)
    return k.mat


def side(K, well, seg, colour=SIDE, power=1.0):
    """Side light: short strip lamps along the well's left lip only (ui_shapes.lip_runs: edges whose inward normal is
    within 37 degrees of straight across), under the lip, aimed across the film, so light enters each well from the left
    like the lit side of a crescent moon and the right stays in shadow; a slanted lip (a bitten corner, a petal's tip)
    gets none, or its cone would be thrown across the well. Each lamp is set 2.5 px in from the lip, clear of the well
    wall, so the wall does not swallow half of it."""
    d = 2.5
    for x0, y0, x1, y1, nx, ny in lip_runs(well, seg, faces=lambda nx, ny, ln: nx > 0.8):
        K.edge_lamp(x0 + nx * d, y0 + ny * d, x1 + nx * d, y1 + ny * d, nx, ny, SIDE_W * power, spread=120.0,
                    depth=K.dz + min(3.0, -K.dz * 0.4), colour=colour)


def mats(K, s, name):
    lv = s["level"]
    ao = min(6.0, -s["dz"] * 0.6)
    hot = name.endswith(".pressed")
    warn = name.endswith(".warn")
    eye_col = PAL["crimson"] if warn else PAL["pink"]
    K.m.update(plate=K.mat("plum", lambda: plum("plum")), bezel=K.mat("lilac", lambda: metal("lilac", PAL["lilac"], 0.26)),
               groove=K.mat("wall", lambda: printed("wall", PAL["wall"])),
               void=K.mat("void", lambda: film_mat("void", col=PAL["void"], shadow=PAL["void_lo"], es=0.35, ao_mm=ao)),
               eyefilm=K.mat("eyefilm", lambda: film_mat("eyefilm", col="#2a0f3e", shadow=PAL["void_lo"], es=0.25 + 0.9 * lv, ao_mm=ao)),
               keyfilm=K.mat("keyfilm", lambda: glass("keyfilm", "#2b1240", PAL["key_hot"] if hot else PAL["key"], 0.03 + 0.55 * lv, ao)),
               line=K.mat("line", lambda: film_mat("line", col=PAL["pink"], shadow="#12041c", es=0.9, rim=False)),
               lid=K.mat("lid", lambda: film_ink("lid", PAL["lilac"], es=0.18 + 0.55 * lv)),
               iris=K.mat("iris", lambda: film_ink("iris", eye_col, es=0.12 + 1.6 * lv)),
               pupil=K.mat("pupil", lambda: film_ink("pupil", PAL["pale"] if not warn else "#ffd0dc", es=0.1 + 2.4 * lv)),
               shade=K.mat("shade", lambda: printed("shade", "#14082a")),
               jewel=K.mat("jewel", lambda: jewel("jewel", PAL["pink"], 0.5 + 2.5 * (lv if name.startswith("button") else 0.4))))
    for c, col in MOON.items():
        K.m[f"moon_{c}"] = K.mat(f"moon_{c}", lambda col=col, c=c: film_mat(f"moon_{c}", col=col, shadow="#10081a", es=1.6, ao_mm=ao))


def piece(name):
    def build(K):
        s = SHAPES[name]
        K.roll, K.dz = s["roll"], s["dz"]
        K.pt = min(8.0, 2 * s["roll"] + 2.0)
        mats(K, s, name)
        wells = [(kind, [tuple(p) for p in w]) for kind, w in s["wells"]]
        plate = [tuple(p) for p in s["plate"]]
        for (kind, _w), f in zip(wells, s["films"]):   # films measured to fit inside the plate (rule B)
            fm = {"void": "void", "eye": "eyefilm", "key": "keyfilm", "line": "line"}.get(kind, kind)
            K.film(None, shape=[tuple(p) for p in f], m=K.m[fm])
        K.plate([plate] + [w for _k, w in wells])
        for kind, w in wells:
            gap = min(min(math.hypot(x - px, y - py) for px, py in plate) for x, y in w)
            if gap >= 5.0:   # a lilac bezel and a dark well wall where the land has room for them
                K.bezel(w, grow=1.2, wall=gap >= 7.0)
            if s["lamps"]:
                col = SIDE
                if kind.startswith("moon_"):
                    col = tuple(int(MOON[kind[5:]][i:i + 2], 16) / 255 for i in (1, 3, 5))
                # keys brighten with their state; a small inset gets a third of the light, or it floods (it holds
                # readouts that must stay dark behind their ink)
                power = 0.3 + 3.2 * s["level"] if kind == "key" else (0.35 if name == "frame.inset" else 1.0)
                side(K, w, 12.0 if min(s["size"]) > 40 else 6.0, colour=col, power=power)
        for x, y, r in s["jewels"]:
            K.knob(x, y, r, collar=False, m=K.m["jewel"])
        for eye in s["eyes"]:
            for role, line, wd in eye:
                pts = [tuple(p) for p in line] + [tuple(line[0])]
                if role == "pupil":
                    K.flat([[tuple(p) for p in line]], K.m["pupil"])
                else:
                    K.stroke(pts, wd, K.m[role])
        if s.get("shade"):
            K.flat([[tuple(p) for p in s["shade"]]], K.m["shade"])
        link_layers(DISPLAY, DISPLAY, main=MAIN)
    return build


def fill(col, es):
    """A bar's plain fill (the kit still lists it; the Ombrai's bars are eye cells, so the page never shows it)."""
    def build(K):
        w, h = K.w, K.h
        core = K.mat("core", lambda: glow("core", col, es))
        K.slab([rrect(1.5, 2.0, w - 1.5, h - 2.0, 3.0)], 0.0, 0.3, core, bevel=0.0)
    return build


PIECES = {}
for name, s in SHAPES.items():
    if s["kind"] == "cell":
        PIECES[name] = ("cell", tuple(s["size"]), None, None, piece(name), s["gap"])
    else:
        PIECES[name] = (s["kind"], tuple(s["size"]), tuple(s["margins"]) if s["margins"] else None, s["stretch"], piece(name), s["inset"])
PIECES["bar.fill"] = ("nine", (64, 10), (0, 6, 0, 6), "x", fill(PAL["pink"], 2.5))
PIECES["bar.fill.red"] = ("nine", (64, 10), (0, 6, 0, 6), "x", fill(PAL["crimson"], 2.5))
ORDER = ["frame.panel", "frame.inset", "bar.track", "bar.fill", "bar.fill.red", "bar.cell.off", "bar.cell.on", "bar.cell.warn",
         "button.idle", "button.hover", "button.pressed", "tab.idle", "tab.active", "strip.top", "strip.bottom", "divider",
         "nameplate", "helm.pad", "ssd.bezel", "dial", "lamp.alert.green", "lamp.alert.yellow", "lamp.alert.red", "crest"]
PIECES = {k: PIECES[k] for k in ORDER + [k for k in PIECES if k not in ORDER]}

# A soft cool room light from the upper left for the plum metal; link_layers adds the softbox, the ceiling and the main
# light that also reaches the wells.
LIGHTS = [("Room", (0.3, -1.0, -1.6), 0.6, (0.92, 0.9, 1.0), 8.0)]


def render_setup(w_px, h_px, samples):
    ui_render.setup(w_px, h_px, samples, LIGHTS, world=(0.035, 0.025, 0.05),
                    bounces=dict(max_bounces=8, glossy_bounces=4, transmission_bounces=4, diffuse_bounces=3))


if __name__ == "__main__":
    ui_render.run("ombrai", PIECES, Panel, render_setup, samples=48)
