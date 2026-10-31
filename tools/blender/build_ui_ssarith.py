"""Fleet Command: the Ssarith Hunt console skin, modelled, lit and rendered in Blender on the analog panel bible.

Owns every piece of the Ssarith Hunt skin of the shared console UI kit: the console of a reptilian race that hunts for
sport (owner, 2026-10-30: "A gurilla style faction, gun metal, beveled corner, industrial retro CRT style buttons",
then "that faction is from the gorn, which are more hunter like faction, they enjoy the sport, so their ui is
reminiscent of a dense jungle akin to the Predator"), rebuilt on the bible's construction (owner, 2026-10-31: "we need to
update krai, onbrai, and hunt to use the new guildine standards ... Please make them look nice"). Two layers: a pillowed
plate of worn olive gunmetal, every edge rolled over, cut-cornered (the hero pieces carry the hunters' own hide-scale
pressing), over recessed tubes: each well a small CRT, phosphor film glowing from behind with its scanlines printed on
it. Hex bolts sit in counterbores on the flat, tally marks and tracking reticles are engraved in bone, frond leaves lie
on the flat of the hero pieces, and the alert lamps are caged jewels in pressed dimples.

The light is the bible's "backlight only" option: the tubes light themselves and no lamp is hidden under any lip. A
hunter shows nothing the quarry could see; the only light on the console is the tracker's own glow, plus the dim canopy
light from above on the metal and the soft main light that shades each tube's top and left rim.

The shapes come from assets/ui/ssarith/shapes.json, written and fit-checked by tools/mockups/ssarith_shapes.py (run it
first); this script models exactly those outlines and renders them face on in Cycles through the shared harness
(tools/blender/ui_render.py) at twice the slot size, with a manifest. A key's idle, hover and pressed states, a tab's
two states and the symbol bar's three cells are one model each: only the phosphor's light changes.

    python3 tools/mockups/ssarith_shapes.py
    /opt/blender/blender --background --factory-startup --python tools/blender/build_ui_ssarith.py -- \\
        --out assets/ui/ssarith [--only button.idle,crest] [--samples 48]
    python3 tools/mockups/ssarith_shapes.py --verify assets/ui/ssarith

Units: 1 px of the 1x slot is 1 mm, y down. Our own design throughout: no other game's or film's names, shapes, glyphs
or art. Ssarith Hunt design language: openspec/changes/console-skins/design.md.
"""

import json
import math
import os
import sys

import bmesh

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import ui_render  # noqa: E402  (the render harness every Blender skin shares)
from ui_shapes import NodeKit, Panel, film_ink, film_mat, link_layers, metal, printed  # noqa: E402

SHAPES = json.load(open(os.path.join(ui_render.ROOT, "assets", "ui", "ssarith", "shapes.json")))["pieces"]

# The hunter's gear (olive gunmetal), the hunting ground (canopy), the tracker (phosphor), the count of kills (bone),
# the strike (kill red). The palette and its meanings are in design.md.
PAL = dict(drab="#585e3e", drab_hi="#636a44", drab_lo="#4c5236", gun="#2b3130", bolt="#50585a", wall="#050806",
           tube="#0f2a17", tube_lo="#020604", lit="#1d6633", flare="#2f9a4c", slot="#081a0e", scan="#041009",
           grid="#2f9a4f", phos="#8affa0", clawoff="#1d4a2a", kill="#ff5a3a", bone="#d8cfb4", leaf="#2c4a24", rib="#3b3a24")
MAIN = (0.8, 60.0)    # the soft main light on the tubes: strength, angular size (deg)
DIMPLE = 2.2          # a lamp's pressed dimple, mm deep
# Display materials: every film and everything printed on one (the first is the film the print sits on).
DISPLAY = ("tube", "lit", "flare", "slotfilm", "cellglass", "scan", "grid", "phos", "clawoff", "kill")
# A tube's light by its kind and the piece's state: (film material, emission strength).
LIGHT = dict(tube=("tube", 0.32), lit=("lit", 0.6), flare=("flare", 0.9), slot=("slotfilm", 0.22), cell=("cellglass", 0.22))


def drab(name, scales):
    """Worn olive gunmetal paint over steel: a satin sheen and a fine grain. With scales (hero pieces only, which never
    stretch), the plate is pressed with the hunters' own hide pattern: small scales, each a shade apart."""
    k = NodeKit(name)
    k.set(Metallic=0.55, Roughness=0.42, Coat_Weight=0.25, Coat_Roughness=0.15)
    grain = k.tex("ShaderNodeTexWave", 900.0, **{"Distortion": 1.5, "Detail": 2.0})
    if scales:
        v = k.tex("ShaderNodeTexVoronoi", 300.0, feature="DISTANCE_TO_EDGE")
        cell = k.tex("ShaderNodeTexVoronoi", 300.0)
        k.set(Base_Color=k.ramp(cell.outputs["Color"], PAL["drab_lo"], PAL["drab_hi"], 0.25, 0.85))
        # each scale domed: its height rises smoothly from its edge (a square root of the distance), never a crack
        dome = k.math("POWER", k.math("MINIMUM", k.math("DIVIDE", v.outputs["Distance"], 0.18), 1.0), 0.5)
        k.bump(k.math("ADD", dome, k.math("MULTIPLY", grain.outputs["Fac"], 0.05)), 0.22)
    else:
        k.set(Base_Color=PAL["drab"])
        k.bump(grain.outputs["Fac"], 0.04)
    return k.mat


def jewel(name, col, es):
    """An alert lamp's glass: clear, coated, glowing in its own colour."""
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=0.08, Coat_Weight=1.0, Coat_Roughness=0.02, Emission_Color=col, Emission_Strength=es)
    return k.mat


def leafmat(name):
    """A frond leaf: waxy dark green, light through it at the thin edges."""
    k = NodeKit(name)
    k.set(Base_Color=PAL["leaf"], Roughness=0.42, Coat_Weight=0.4, Coat_Roughness=0.25, Subsurface_Weight=0.25,
          Subsurface_Radius=(0.6, 1.0, 0.3), Subsurface_Scale=0.002)
    return k.mat


def mats(K, s):
    ao = min(6.0, -s["dz"] * 0.6)
    K.m.update(plate=K.mat("hide" if s["scales"] else "drab", lambda: drab("hide" if s["scales"] else "drab", s["scales"])),
               bezel=K.mat("gun", lambda: metal("gun", PAL["gun"], 0.34)), groove=K.mat("wall", lambda: printed("wall", PAL["wall"])),
               knob=K.mat("bolt", lambda: metal("bolt", PAL["bolt"], 0.3)), pointer=K.mat("wall", lambda: printed("wall", PAL["wall"])),
               bone=K.mat("bone", lambda: printed("bone", PAL["bone"], 0.5)), leaf=K.mat("leaf", lambda: leafmat("leaf")),
               rib=K.mat("rib", lambda: printed("rib", PAL["rib"], 0.7)),
               scan=K.mat("scan", lambda: film_ink("scan", PAL["scan"], es=0.15)),
               grid=K.mat("grid", lambda: film_ink("grid", PAL["grid"], es=0.45)),
               phos=K.mat("phos", lambda: film_ink("phos", PAL["phos"], es=2.2)),
               clawoff=K.mat("clawoff", lambda: film_ink("clawoff", PAL["clawoff"], es=0.35)),
               kill=K.mat("kill", lambda: film_ink("kill", PAL["kill"], es=2.4)))
    for key, (mname, es) in LIGHT.items():
        col = {"tube": PAL["tube"], "lit": PAL["lit"], "flare": PAL["flare"], "slot": PAL["slot"], "cell": "#030a05"}[key]
        K.m[mname] = K.mat(mname, lambda mname=mname, col=col, es=es: film_mat(mname, col=col, shadow=PAL["tube_lo"], es=es, glow=1.0,
                                                                                rim=key not in ("cell",), ao_mm=ao))


def light_of(name, kind):
    """The film a well gets: a key's tube is dark at rest, lit on hover and flaring when pressed; the active tab's is
    lit; every other well keeps its kind's own light."""
    if kind == "tube":
        if name.endswith(".hover") or name == "tab.active":
            return "lit"
        if name.endswith(".pressed"):
            return "flare"
    return kind


def bolt(K, ob, x, y, r):
    """A hex bolt sunk in a counterbore: a round bore cut into the plate, the six-sided head in it, a hex socket."""
    bm = bmesh.new()
    K.cyl_bm(bm, x, y, r * 1.12, K.pt - 1.3, K.pt + 2.0, seg=32)
    K.cut(ob, bm)
    z = K.pt - 1.3
    K.cyl(x, y, r, z, z + 1.1, K.m["knob"], seg=6, smooth=False)
    K.cyl(x, y, r * 0.42, z + 1.0, z + 1.25, K.m["groove"], seg=6, smooth=False)


def caged_jewel(K, ob, x, y, r, style):
    """An alert lamp: a jewel in a pressed dimple, three gunmetal guard bars across it."""
    col = {"jewel_green": PAL["flare"], "jewel_yellow": "#ffcf6a", "jewel_red": PAL["kill"]}[style]
    K.dimple(ob, x, y, r + 5, r + 5, K.pt, DIMPLE, rz=DIMPLE + 0.4)
    m = K.mat(style, lambda: jewel(style, col, 3.0))
    z = K.pt - DIMPLE * 0.75
    K.cyl(x, y, r + 1.6, z - 0.4, z + 0.6, K.m["bezel"], seg=48)
    K.blob(x, y, r, r, r * 0.6, m, z=z + 0.4, seg=12)
    for dx in (-r * 0.55, 0.0, r * 0.55):
        h = math.sqrt(max(0.0, (r + 3.5) ** 2 - dx * dx))
        K.tube([(x + dx, y - h), (x + dx, y + h)], [1.1, 1.1], K.m["bezel"], flat=1.0, z=[K.pt + 2.6] * 2, sharp=True)


def frond(K, outline, cx, cy, length, angle):
    """A leaf lying on the flat metal (its outline checked by the shapes file), its midrib raised."""
    K.slab([[tuple(p) for p in outline]], K.pt, K.pt + 0.7, K.m["leaf"], bevel=0.25)
    tip = (cx + math.cos(angle) * length * 0.9, cy + math.sin(angle) * length * 0.9)
    K.tube([(cx, cy), tip], [0.45, 0.2], K.m["rib"], flat=0.6, z=[K.pt + 0.9] * 2, sharp=True)


def piece(name):
    def build(K):
        s = SHAPES[name]
        K.roll, K.dz = s["roll"], s["dz"]
        K.pt = min(8.0, 2 * s["roll"] + 2.0)
        mats(K, s)
        wells = [(light_of(name, kind), [tuple(p) for p in w]) for kind, w in s["wells"]]
        if s["kind"] == "cell":  # a cell is all tube: its glass, the claw marks printed on it, nothing else
            K.film(None, shape=wells[0][1], m=K.m[LIGHT["cell"][0]])
        else:
            plate = [tuple(p) for p in s["plate"]]
            for (kind, _w), f in zip(wells, s["films"]):  # films measured to fit inside the plate (rule B)
                K.film(None, shape=[tuple(p) for p in f], m=K.m[LIGHT[kind][0]])
            ob = K.plate([plate] + [w for _k, w in wells])
            jewels = [st for st in s["studs"] if st[3].startswith("jewel")]
            for x, y, r, st in s["studs"]:
                if st == "bolt":
                    bolt(K, ob, x, y, r)
            for x, y, r, st in jewels:
                caged_jewel(K, ob, x, y, r, st)
            if s["studs"]:
                K.finish(ob, 0.5)
            for _kind, w in wells:
                gap = min(min(math.hypot(x - px, y - py) for px, py in plate) for x, y in w)
                if gap >= 4.0:  # a dark bezel and well wall where the land has room for them
                    K.bezel(w, grow=1.0, wall=gap >= 6.0)
        for _role, lines, w, ink in s["prints"]:
            for line in lines:
                K.stroke([tuple(p) for p in line], w, K.m[ink])
        for line in s["engrave"]:
            K.stroke([tuple(p) for p in line], 1.1, K.m["bone"], z=K.pt)
        for (cx, cy, length, _width, ang), outline in s["leaves"]:
            frond(K, outline, cx, cy, length, ang)
        link_layers(DISPLAY, DISPLAY, main=MAIN)
    return build


PIECES = {}
for name, s in SHAPES.items():
    if s["kind"] == "cell":
        PIECES[name] = ("cell", tuple(s["size"]), None, None, piece(name), s["gap"])
    else:
        PIECES[name] = (s["kind"], tuple(s["size"]), tuple(s["margins"]) if s["margins"] else None, s["stretch"], piece(name), s["inset"])


def fill(col):
    """The bar fills the kit still lists: the Hunt draws its bars as claw cells, so these are plain phosphor runs, kept
    only so the kit is complete."""
    def build(K):
        k = NodeKit("fill")
        k.set(Base_Color=col, Roughness=0.5, Emission_Color=col, Emission_Strength=2.0)
        K.slab([[(0.5, 1.5), (K.w - 0.5, 1.5), (K.w - 0.5, K.h - 1.5), (0.5, K.h - 1.5)]], 0.0, 0.4, k.mat, bevel=0.0)
    return build


PIECES["bar.fill"] = ("nine", (64, 10), (0, 6, 0, 6), "x", fill(PAL["phos"]))
PIECES["bar.fill.red"] = ("nine", (64, 10), (0, 6, 0, 6), "x", fill(PAL["kill"]))
ORDER = ["frame.panel", "frame.inset", "bar.track", "bar.fill", "bar.fill.red", "bar.cell.off", "bar.cell.on", "bar.cell.warn",
         "button.idle", "button.hover", "button.pressed", "tab.idle", "tab.active", "strip.top", "strip.bottom", "divider",
         "nameplate", "helm.pad", "ssd.bezel", "dial", "lamp.alert.green", "lamp.alert.yellow", "lamp.alert.red", "crest"]
PIECES = {k: PIECES[k] for k in ORDER + [k for k in PIECES if k not in ORDER]}

# A dim light falling through the canopy from above onto the metal; link_layers adds the softbox, the ceiling and the
# main light that also reaches the tubes.
LIGHTS = [("Canopy", (0.25, -1.0, -1.5), 0.8, (0.92, 1.0, 0.84), 9.0)]


def render_setup(w_px, h_px, samples):
    ui_render.setup(w_px, h_px, samples, LIGHTS, world=(0.035, 0.045, 0.035),
                    bounces=dict(max_bounces=8, glossy_bounces=4, transmission_bounces=4, diffuse_bounces=3))


if __name__ == "__main__":
    ui_render.run("ssarith", PIECES, Panel, render_setup, samples=48)
