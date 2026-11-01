"""Fleet Command: the Aelith Conclave console skin's shell, modelled, lit and rendered in Blender.

Owns the rendered half of the Conclave's hybrid skin (owner, 2026-10-30: "the conclave is one exception. They have a
hybrid of both vector graphics for holographic buttons and icons, but 3d rendered shell around the UI panels and
elements"; 2026-10-31: "Using these design principles. Update conclave UI", the principles being the analog panel
bible, docs/design/ui-bible.html, and the blender-ui-panels skill). Every piece is a pillowed shell of satin gold
whose edges roll over (footprint-true), with formed wells cut through it (lozenges drawn to a point, knuckled corners,
a scalloped rim, embracing arms) and pearls set in the metal. Behind each well is a void: dark teal glass glowing
faintly, rim-lit by cyan lamps tucked under its lip all the way round, and shadowed softly from the upper left by the
main light. The holograms (keys, fields, icons) are not here: docs/mockups/skins/aelith.js draws them as vectors into
the wells, from the same shapes.

The shapes come from assets/ui/aelith/shapes.json, written and fit-checked by tools/mockups/aelith_shapes.py (run it
first); this script models exactly those outlines, through tools/blender/ui_shapes.py's Panel, and renders face on in
Cycles through the shared harness (tools/blender/ui_render.py) at twice the slot size, with a manifest.

    python3 tools/mockups/aelith_shapes.py
    /opt/blender/blender --background --factory-startup --python tools/blender/build_ui_aelith.py -- \\
        --out assets/ui/aelith [--only button.idle,crest] [--samples 48]
    python3 tools/mockups/aelith_shapes.py --verify assets/ui/aelith

Units: 1 px of the 1x slot is 1 mm, y down. Design language: openspec/changes/console-skins/design.md (Aelith Conclave).
"""

import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import ui_render  # noqa: E402  (the render harness every Blender skin shares)
from ui_shapes import NodeKit, Panel, film_mat, link_layers, lip_runs, metal, printed  # noqa: E402  (shared modelling and shading)

SHAPES = json.load(open(os.path.join(ui_render.ROOT, "assets", "ui", "aelith", "shapes.json")))["pieces"]

# Wisdom, permanence, serenity, thought made visible: pearl, gold, void, holo (the palette and its meanings are in
# design.md). The shell is gold and pearl; the wells are void; the light in them is holo cyan.
PAL = dict(gold="#c9a646", gold_hi="#f0d58a", pearl="#ece6da", void="#0c3340", void_deep="#010a0d", wall="#03100f")
RIM = (0.42, 0.95, 1.0)   # the rim lamps' colour: holo cyan
RIM_W = float(os.environ.get("RIM_W", 0.0004))       # rim lamp power per mm of well edge (times the lamp scale); 0.0008
# was tuned while most lamps were aimed out of their wells (fixed by ui_shapes.lip_runs, 2026-10-31)
RIM_SPREAD = float(os.environ.get("RIM_SPREAD", 120))  # their spread, deg: wide enough that the lamps blend into one glow
MAIN = (0.8, 60.0)        # the main light on the wells: strength and angular size (deg), a soft upper-left shadow


def gold(name):
    """Satin gold armour: warm, a fine polished grain, a thin clear coat; bright where the rolled edges catch light."""
    k = NodeKit(name)
    g = k.tex("ShaderNodeTexWave", 900.0, **{"Distortion": 1.5, "Detail": 2.0})
    k.set(Base_Color=PAL["gold"], Metallic=0.95, Roughness=0.24, Coat_Weight=0.4, Coat_Roughness=0.06)
    k.bump(g.outputs["Fac"], 0.03)
    return k.mat


def pearl(name):
    """Pearl inlay: soft white under a deep coat, a thin-film sheen that shifts toward cyan and rose."""
    k = NodeKit(name)
    k.set(Base_Color=PAL["pearl"], Roughness=0.3, Coat_Weight=1.0, Coat_Roughness=0.05, Thin_Film_Thickness=320.0,
          Thin_Film_IOR=1.45, Sheen_Weight=0.3)
    return k.mat


def rim(K, well, seg):
    """Rim light: short strip lamps all the way round the well's edge (ui_shapes.lip_runs, every edge), under the lip,
    aimed inward along the void with a narrow spread, so the void glows at its rim and stays dark in the middle where
    the holograms are drawn."""
    for x0, y0, x1, y1, nx, ny in lip_runs(well, seg):
        K.edge_lamp(x0, y0, x1, y1, nx, ny, RIM_W, spread=RIM_SPREAD, depth=K.dz + min(3.0, -K.dz * 0.4), colour=RIM)


def piece(name):
    def build(K):
        s = SHAPES[name]
        K.roll, K.dz = s["roll"], s["dz"]
        K.pt = min(8.0, 2 * s["roll"] + 2.0)
        K.m.update(plate=K.mat("gold", lambda: gold("gold")), knob=K.mat("pearl", lambda: pearl("pearl")),
                   bezel=K.mat("gold_hi", lambda: metal("gold_hi", PAL["gold_hi"], 0.2)),
                   groove=K.mat("wall", lambda: printed("wall", PAL["wall"])),
                   film=K.mat("void", lambda: film_mat("void", col=PAL["void"], shadow=PAL["void_deep"], es=float(os.environ.get("VOID_ES", 0.12)), ao_mm=min(7.0, -s["dz"] * 0.6))))
        for f in s["films"]:  # measured to fit inside the shell (rule B)
            K.film(None, shape=[tuple(p) for p in f])
        wells = [[tuple(p) for p in w] for w in s["wells"]]
        K.plate([[tuple(p) for p in s["plate"]]] + wells)
        for w in wells:
            gap = min(min(math.hypot(x - px, y - py) for px, py in s["plate"]) for x, y in w)
            if gap >= 5.0:  # a bezel and a well wall only where the land has room for them inside the shell
                K.bezel(w, grow=1.2, wall=gap >= 7.0)
            rim(K, w, 12.0 if min(s["size"]) > 40 else 8.0)
        for x, y, r in s["studs"]:
            K.knob(x, y, r, collar=False)
        link_layers(("void",), ("void",), main=MAIN)
    return build


PIECES = {}
for name, s in SHAPES.items():
    PIECES[name] = (s["kind"], tuple(s["size"]), tuple(s["margins"]) if s["margins"] else None, s["stretch"], piece(name), s["inset"])

# A soft room light from the upper left for the gold; link_layers adds the softbox, the ceiling and the main light.
LIGHTS = [("Room", (0.3, -1.0, -1.6), 0.7, (1.0, 0.97, 0.92), 8.0)]


def render_setup(w_px, h_px, samples):
    ui_render.setup(w_px, h_px, samples, LIGHTS, world=(0.03, 0.035, 0.04),
                    bounces=dict(max_bounces=8, glossy_bounces=4, transmission_bounces=4, diffuse_bounces=3))


if __name__ == "__main__":
    ui_render.run("aelith", PIECES, Panel, render_setup, samples=48)
