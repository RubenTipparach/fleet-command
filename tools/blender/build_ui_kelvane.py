"""Fleet Command: the Kelvane Institute console skin, modelled, lit and rendered in Blender.

Owns every piece of the Kelvane skin of the shared console UI kit. The Institute's instruments were inspired by the
Nucleo NLog look (owner, 2026-10-31: "They were inspired by the nucleo look. So.lets update that"), so the kit is
built the way the session's NLog study was, in our own design: two layers, a pillowed champagne plate whose every edge
rolls over, over recessed displays lit from behind. Two kinds of light for two kinds of information: amber backlit
film with flat ink for readouts (names, keys, tabs, the strips' meters, icons), green phosphor glass with a faint
graticule for the scopes (the helm, the ship display, the radar). Domed keys sit in pressed dimples, the helm's eight
keys in one pressed cradle round its scope, knurled knobs and screws stand on flat metal, jewel lamps glow in dimples.
Soft lamps tucked under each amber window's top lip (10 mm, 180 degrees) and a soft main light from the upper left
shade into the wells; light linking keeps the metal and the displays apart (tools/blender/ui_shapes.py's Panel and
link_layers, the analog panel bible, docs/design/ui-bible.html).

The shapes come from assets/ui/kelvane/shapes.json, written and fit-checked by tools/mockups/kelvane_shapes.py (run it
first); this script models exactly those outlines and renders them face on in Cycles through the shared harness
(tools/blender/ui_render.py) at twice the slot size, with a manifest.

    python3 tools/mockups/kelvane_shapes.py
    /opt/blender/blender --background --factory-startup --python tools/blender/build_ui_kelvane.py -- \\
        --out assets/ui/kelvane [--only button.idle,crest] [--samples 48]
    python3 tools/mockups/kelvane_shapes.py --verify assets/ui/kelvane

Units: 1 px of the 1x slot is 1 mm, y down. Design language: openspec/changes/console-skins/design.md (Kelvane).
"""

import json
import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import ui_render  # noqa: E402  (the render harness every Blender skin shares)
from ui_shapes import MM, NodeKit, Panel, film_ink, film_mat, link_layers, metal, printed, rrect, satin  # noqa: E402

SHAPES = json.load(open(os.path.join(ui_render.ROOT, "assets", "ui", "kelvane", "shapes.json")))["pieces"]

# Truth is measured: champagne metal (the instrument), amber light (a reading), phosphor green (a trace), needle red
# (the reading that matters), ink (the record). The palette and its meanings are in design.md.
PAL = dict(champagne="#c9bfa9", knob="#d6d0c4", brass="#b0915a", amber="#f2c46e", amber_lo="#6b4a1c", dim="#b8945a",
           phos="#1c5a33", phos_lo="#03110a", grat="#4fd27a", ink="#5a3f16", unlit="#5a4520", red="#c0301f", slot="#241a10",
           groove="#2e271c", engrave="#3a3328", tray="#a89f8e")
GLOW = float(os.environ.get("FILM_GLOW", 0.78))   # the film's own backlight, as in the study
MAIN = (0.8, 60.0)                                  # the soft main light on the wells: strength, angular size (deg)
DIMPLE = 2.2                                        # a dimple's depth, mm
DISPLAY = ("amber", "hot", "dim", "phos", "slotfilm", "ink", "grat", "unlit", "redink")


def jewel(name, col, es):
    """A jewel lamp's glass: clear, coated, glowing in its own colour."""
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=0.08, Coat_Weight=1.0, Coat_Roughness=0.02, Emission_Color=col, Emission_Strength=es)
    return k.mat


def glow(name, col, es):
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=0.5, Emission_Color=col, Emission_Strength=es)
    return k.mat


def mats(K, s):
    ao = min(7.0, -s["dz"] * 0.6)
    K.m.update(plate=K.mat("satin", lambda: satin("satin", PAL["champagne"])), knob=K.mat("knob", lambda: metal("knob", PAL["knob"], 0.16)),
               bezel=K.mat("brass", lambda: metal("brass", PAL["brass"], 0.28)), groove=K.mat("groove", lambda: printed("groove", PAL["groove"])),
               pointer=K.mat("pointer", lambda: printed("pointer", "#a83020")),
               amber=K.mat("amber", lambda: film_mat("amber", col=PAL["amber"], shadow=PAL["amber_lo"], es=0.85, glow=GLOW, ao_mm=ao)),
               dim=K.mat("dim", lambda: film_mat("dim", col=PAL["dim"], shadow="#3a2810", es=0.3, glow=GLOW, ao_mm=ao)),
               hot=K.mat("hot", lambda: film_mat("hot", col=PAL["amber"], shadow=PAL["amber_lo"], es=1.35, glow=GLOW, ao_mm=ao)),
               phos=K.mat("phos", lambda: film_mat("phos", col=PAL["phos"], shadow=PAL["phos_lo"], es=0.55, glow=1.0, ao_mm=ao)),
               slot=K.mat("slotfilm", lambda: film_mat("slotfilm", col=PAL["slot"], shadow="#0c0805", es=0.2, glow=1.0, ao_mm=1.5)),
               tray=K.mat("tray", lambda: satin("tray", PAL["tray"])),
               ink=K.mat("ink", lambda: film_ink("ink", PAL["ink"], glow=GLOW)), grat=K.mat("grat", lambda: film_ink("grat", PAL["grat"], es=0.5)),
               unlit=K.mat("unlit", lambda: film_ink("unlit", PAL["unlit"], es=0.3)), red=K.mat("redink", lambda: film_ink("redink", PAL["red"])),
               engrave=K.mat("engrave", lambda: printed("engrave", PAL["engrave"])),
               knurl=K.mat("knurl", lambda: metal("knurl", PAL["knob"], 0.2, True)))


def torus_cut(K, ob, cx, cy, R, half, depth):
    """Press a ring channel into a plate: a torus cutter whose round section (radius half px) sinks depth mm below the
    top, so the channel is one smooth trough (the helm's cradle, the crest's ring)."""
    bm = bmesh.new()
    seg, mseg, rz = 96, 24, half
    zc = K.pt + rz - depth
    rows = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        row = []
        for k in range(mseg):
            b = 2 * math.pi * k / mseg
            rr_ = R + half * math.cos(b)
            x, y, z = cx + rr_ * math.cos(a), cy + rr_ * math.sin(a), zc + rz * math.sin(b)
            row.append(bm.verts.new(K.P(x, y, z)))
        rows.append(row)
    for i in range(seg):
        for k in range(mseg):
            bm.faces.new((rows[i][k], rows[(i + 1) % seg][k], rows[(i + 1) % seg][(k + 1) % mseg], rows[i][(k + 1) % mseg]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    K.cut(ob, bm)


def stud(K, x, y, r, style, sunk, press=0.0):
    """A thing standing on the metal: a domed key, a knurled knob, a screw or a jewel lamp. sunk lowers it into the
    dimple it sits in; press lowers a pressed key further (mm)."""
    z = K.pt - (DIMPLE * 0.75 if sunk else 0.0) - press
    if style == "key":
        K.knob(x, y, r, z=z, collar=False)
    elif style == "knurl":
        K.cyl(x, y, r * 1.14, z - 0.6, z + 0.7, K.m["groove"], seg=48)
        K.cyl(x, y, r, z, z + 5.0, K.m["knurl"], seg=64)
        K.cyl(x, y, r * 0.74, z + 5.0, z + 6.0, K.m["knob"], r1=r * 0.68, seg=64)
        K.tube([(x, y - r * 0.15), (x, y - r * 0.62)], [0.7, 0.7], K.m["pointer"], flat=0.6, z=[z + 6.2] * 2, sharp=True)
    elif style == "screw":
        K.cyl(x, y, r, z, z + 1.0, K.m["knob"], r1=r * 0.82, seg=32)
        K.slab([[(x - r * 0.72, y - 0.32), (x + r * 0.72, y - 0.42), (x + r * 0.72, y + 0.32), (x - r * 0.72, y + 0.42)]], z + 0.8, z + 1.15,
               K.m["groove"], bevel=0.08)
    elif style.startswith("jewel"):
        col = {"jewel": PAL["amber"], "jewel_green": "#5aff8a", "jewel_yellow": "#ffcf6a", "jewel_red": "#ff4a3a",
               "jewel_on": "#ffc35a", "jewel_off": "#ffc35a"}[style]   # a toggle's pilot lamp: one glass, lit or dark
        es = {"jewel": 1.2, "jewel_on": 3.0, "jewel_off": 0.03}.get(style, 3.0)
        m = K.mat(style, lambda: jewel(style, col, es))
        K.cyl(x, y, r + 1.6, z - 0.4, z + 0.6, K.m["bezel"], seg=48)
        K.blob(x, y, r, r, r * 0.62, m, z=z + 0.4, seg=10)


def piece(name):
    def build(K):
        s = SHAPES[name]
        K.roll, K.dz = s["roll"], s["dz"]
        K.pt = min(8.0, 2 * s["roll"] + 2.0)
        mats(K, s)
        wells = [(kind, [tuple(p) for p in w]) for kind, w in s["wells"]]
        plate = [tuple(p) for p in s["plate"]]
        for (kind, _w), f in zip(wells, s["films"]):  # films measured to fit inside the plate (rule B)
            K.film(None, shape=[tuple(p) for p in f], m=K.m[{"amber": "amber", "hot": "hot", "dim": "dim", "phos": "phos", "slot": "slot", "tray": "tray"}[kind]])
        ob = K.plate([plate] + [w for _k, w in wells])
        cuts = False
        for x, y, r in s["dimples"]:
            K.dimple(ob, x, y, r, r, K.pt, DIMPLE, rz=DIMPLE + 0.4)
            cuts = True
        if s["cradle"]:
            cx, cy, R, half, depth = s["cradle"]
            torus_cut(K, ob, cx, cy, R, half, depth)
            cuts = True
        if cuts:
            K.finish(ob, 0.6)
        for kind, w in wells:
            gap = min(min(math.hypot(x - px, y - py) for px, py in plate) for x, y in w)
            if kind != "tray" and gap >= 5.0:  # a bezel and a dark well wall where the land has room for them
                K.bezel(w, grow=1.2, wall=gap >= 7.0)
        sunk = {(round(x, 2), round(y, 2)) for x, y, _r in s["dimples"]}
        cr = s["cradle"]
        for i, (x, y, r, style) in enumerate(s["studs"]):
            in_cradle = bool(cr) and abs(math.hypot(x - cr[0], y - cr[1]) - cr[2]) < 1.0
            press = 1.0 if (i == 0 and name.endswith(".pressed")) else 0.0
            stud(K, x, y, r, style, (round(x, 2), round(y, 2)) in sunk or in_cradle, press)
        for role, lines, w, ink in s["prints"]:
            m = K.m[{"ink": "ink", "grat": "grat", "unlit": "unlit", "red": "red"}[ink]]
            for line in lines:
                K.stroke([tuple(p) for p in line], w, m)
        for line in s["engrave"]:
            K.stroke([tuple(p) for p in line], 0.8, K.m["engrave"], z=K.pt)
        for line in s["emblem"]:
            pts = [tuple(p) for p in line]
            K.tube(pts, [1.2] * len(pts), K.m["bezel"], flat=0.55, z=[K.pt + 0.5] * len(pts))
        if name.endswith(".hover") or name.endswith(".pressed"):  # the key's pilot ring, lit when the key is live
            x, y, r, _st = s["studs"][0]
            dr = s["dimples"][0][2]
            ring = K.mat("pilot", lambda: glow("pilot", PAL["amber"], 3.0 if name.endswith(".pressed") else 1.4))
            # it lies on the dimple's floor round the key: the floor's height at the ring's radius, from the cutter
            zf = K.pt + 0.4 - (DIMPLE + 0.4) * math.sqrt(max(0.0, 1 - ((r + 1.6) / dr) ** 2))
            K.slab([rrect(x - r - 2.6, y - r - 2.6, x + r + 2.6, y + r + 2.6, r + 2.6), rrect(x - r - 0.6, y - r - 0.6, x + r + 0.6, y + r + 0.6, r + 0.6)],
                   zf + 0.05, zf + 0.35, ring, bevel=0.05)
        for x, y, power in s["lamps"]:
            K.lip_light(x, y, power, depth=K.dz + min(8.0, -K.dz * 0.6))
        link_layers(DISPLAY, DISPLAY, main=MAIN)
    return build


def fill(col, es):
    """A bar's fill: the lit streak the page lays into the track's slot (a halo and a bright core, nothing else)."""
    def build(K):
        w, h = K.w, K.h
        halo = K.mat("halo", lambda: glow("halo", "#3a2a14", 0.35))
        core = K.mat("core", lambda: glow("core", col, es))
        K.slab([rrect(1.5, 2.2, w - 1.5, h - 2.2, 2.8)], 0.0, 0.2, halo, bevel=0.0)
        K.slab([rrect(3.0, 3.8, w - 3.0, h - 3.8, 1.2)], 0.2, 0.4, core, bevel=0.0)
    return build


def lamp_cell(col, es):
    """A symbol bar cell: a jewel lamp in a brass bezel. Every state is the same lamp and the same glass; only its light
    (and, on a warning, its colour) changes."""
    def build(K):
        w, h = K.w, K.h
        c = (w / 2, h / 2)
        K.cyl(c[0], c[1], 5.6, 0.0, 1.4, K.mat("brass", lambda: metal("brass", PAL["brass"], 0.28)))
        K.cyl(c[0], c[1], 4.6, 1.0, 1.6, K.mat("groove", lambda: printed("groove", PAL["groove"])))
        K.blob(c[0], c[1], 4.2, 4.2, 2.6, K.mat(f"jewel_{col}_{es}", lambda: jewel(f"jewel_{col}_{es}", col, es)), z=1.4)
    return build


PIECES = {}
for name, s in SHAPES.items():
    PIECES[name] = (s["kind"], tuple(s["size"]), tuple(s["margins"]) if s["margins"] else None, s["stretch"], piece(name), s["inset"])
PIECES["bar.fill"] = ("nine", (64, 10), (0, 6, 0, 6), "x", fill("#ffcf6a", 3.0))
PIECES["bar.fill.red"] = ("nine", (64, 10), (0, 6, 0, 6), "x", fill("#ff5a3a", 3.0))
PIECES["bar.cell.off"] = ("cell", (12, 12), None, None, lamp_cell("#ffc35a", 0.03), 1)
PIECES["bar.cell.on"] = ("cell", (12, 12), None, None, lamp_cell("#ffc35a", 2.6), 1)
PIECES["bar.cell.warn"] = ("cell", (12, 12), None, None, lamp_cell("#ff4a2a", 2.6), 1)
ORDER = ["frame.panel", "frame.inset", "bar.track", "bar.fill", "bar.fill.red", "button.idle", "button.hover", "button.pressed",
         "tab.idle", "tab.active", "strip.top", "strip.bottom", "divider", "nameplate", "helm.pad", "ssd.bezel", "dial",
         "lamp.alert.green", "lamp.alert.yellow", "lamp.alert.red", "crest"]
PIECES = {k: PIECES[k] for k in ORDER + [k for k in PIECES if k not in ORDER]}

# A soft room light from the upper left for the champagne metal; link_layers adds the softbox, the ceiling and the main
# light that also reaches the wells.
LIGHTS = [("Room", (0.3, -1.0, -1.6), 0.7, (1.0, 0.97, 0.92), 8.0)]


def render_setup(w_px, h_px, samples):
    ui_render.setup(w_px, h_px, samples, LIGHTS, world=(0.05, 0.045, 0.04),
                    bounces=dict(max_bounces=8, glossy_bounces=4, transmission_bounces=4, diffuse_bounces=3))


if __name__ == "__main__":
    ui_render.run("kelvane", PIECES, Panel, render_setup, samples=48)
