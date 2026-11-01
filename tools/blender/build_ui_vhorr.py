"""Fleet Command: the Vhorr Brood console skin, modelled, shaded and rendered in Blender.

Owns every piece of the Brood skin of the shared console UI kit, rebuilt as assimilated tech (owner, 2026-10-31: "The
brood still needs work", "I like that some of the tech in zerg is human tech with an organic layer on top"; design.md,
"The Vhorr Brood: assimilated tech"). The brood grows, consumes and adapts; it does not build consoles, it takes them.
Two layers:

- a captured machine on the analog panel bible (docs/design/ui-bible.html, the blender-ui-panels skill): gunmetal
  steel plates whose every edge rolls over, machined wells with dark glass, keycaps, bolts, a lamp under each window's
  top lip, fully legible;
- the brood's growth over it, in the skin's organic materials (owner, 2026-10-30: "slimey leather, tendrils, claws,
  carapaces, fibers"): creep spreading from the corners and seams (a thick core of hide lit from behind, a thin wet
  membrane at its edge fogging the bezels), bolts swallowed into glowing pustules that serve as status lights, chitin
  ridges and horns growing out past the plate's corners, captured conduits wrapped in sinew, a fleshy pocket on the
  name plate holding the brood's eye as the captain's portrait, and a large glowing egg as the crest.

Growth never covers a readout, a label or a key's face, and on a 9-slice piece it lies only in the cells that never
stretch (rule G). The shapes come from assets/ui/vhorr/shapes.json, written and fit-checked by
tools/mockups/vhorr_shapes.py (run it first); this script models exactly those outlines through tools/blender/ui_shapes.py's
Panel and renders them face on in Cycles through the shared harness (tools/blender/ui_render.py) at twice the slot
size, with a manifest. The icons are the skin's organic sculptures, unchanged.

    python3 tools/mockups/vhorr_shapes.py
    /opt/blender/blender --background --factory-startup --python tools/blender/build_ui_vhorr.py -- \\
        --out assets/ui/vhorr [--only button.idle,crest] [--samples 48]
    python3 tools/mockups/vhorr_shapes.py --verify assets/ui/vhorr

Units: 1 px of the 1x slot is 1 mm, y down. Geometry and shaders use fixed seeds only; subsurface renders vary by a
few colour levels between runs, so a rerun is visually the same but not bit-identical. Our own design throughout.
"""

import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import panel_kit  # noqa: E402  (srgb)
import ui_render  # noqa: E402  (the render harness every Blender skin shares)
from ui_shapes import NodeKit, Panel, curl, film_mat, link_layers, printed, taper  # noqa: E402  (shared modelling and shading)

MM = ui_render.MM
srgb = panel_kit.srgb
SHAPES = json.load(open(os.path.join(ui_render.ROOT, "assets", "ui", "vhorr", "shapes.json")))
CELL, EGG = SHAPES["cell"], SHAPES["egg"]
SHAPES = SHAPES["pieces"]

# ----------------------------------------------------------------------------- materials
# Hunger, growth, the swarm: dark chitin, leather hide, flesh, bile light, slime, blood; and the steel they took.
PAL = dict(chitin="#2b1d2a", chitin_hi="#5a3a5e", hide="#5a3424", hide_dark="#2e1814", flesh="#c4566a", bile="#a6e34a",
           bile_hot="#e4ff9a", sinew="#d9a8a0", pit="#120a10", blood="#e0342a", amber="#f0b43c", steel="#6a737a", steel_lo="#4a5258")


def hide_mat(name, glow, glow_col="#b6d86a"):
    """Leather lit from behind: dark mottled hide with thin wandering veins, pores and wrinkles in the
    bump, flesh scattering under the skin, a wet clear coat; light comes through only where the skin is
    thin (low-frequency patches), warming the hide toward flesh before it glows bile green."""
    k = NodeKit(name)
    wv = k.warped(60.0, 0.006)
    cells = k.tex("ShaderNodeTexVoronoi", 150.0, vec=wv, feature="DISTANCE_TO_EDGE")
    vein = k.math("POWER", k.math("SUBTRACT", 1.0, k.math("DIVIDE", cells.outputs["Distance"], 0.035, clamp=True)), 4.0)
    thin = k.tex("ShaderNodeTexNoise", 22.0, **{"Detail": 3.0, "Roughness": 0.5})
    patch = k.math("POWER", k.math("MULTIPLY", k.math("SUBTRACT", thin.outputs["Fac"], 0.5, clamp=True), 3.5, clamp=True), 1.4)
    mott = k.tex("ShaderNodeTexNoise", 70.0, **{"Detail": 8.0, "Roughness": 0.62})
    base = k.ramp(mott.outputs["Fac"], "#1e0e0a", PAL["hide"], 0.3, 0.72)
    warm = k.N.new("ShaderNodeMix")
    warm.data_type = "RGBA"
    k.L.new(patch, warm.inputs["Factor"])
    k.L.new(base, warm.inputs[6])
    warm.inputs[7].default_value = list(srgb("#9a4a30")) + [1.0]
    mixv = k.N.new("ShaderNodeMix")
    mixv.data_type = "RGBA"
    k.L.new(vein, mixv.inputs["Factor"])
    k.L.new(warm.outputs[2], mixv.inputs[6])
    mixv.inputs[7].default_value = list(srgb("#2a060c")) + [1.0]
    k.set(Base_Color=mixv.outputs[2], Roughness=0.5, Subsurface_Weight=0.3, Subsurface_Scale=0.002,
          Subsurface_Radius=(1.0, 0.35, 0.2), Coat_Weight=0.8, Coat_Roughness=0.1, Sheen_Weight=0.3)
    if glow > 0:
        k.set(Emission_Color=glow_col, Emission_Strength=k.math("MULTIPLY", k.math("MULTIPLY", patch, k.math("SUBTRACT", 1.0, vein)), glow))
    pores = k.tex("ShaderNodeTexVoronoi", 520.0, **{"Randomness": 1.0})
    wr = k.tex("ShaderNodeTexNoise", 140.0, vec=wv, **{"Detail": 10.0, "Roughness": 0.7})
    k.bump(k.math("ADD", k.math("MULTIPLY", pores.outputs["Distance"], 0.5), k.math("ADD", wr.outputs["Fac"], k.math("MULTIPLY", vein, -0.6))), 0.45)
    return k.mat


def chitin_mat(name):
    """Wet carapace: near-black purple, cracked into plates (Voronoi), a thin-film sheen that turns
    oily green and violet at an angle, glassy under its coat."""
    k = NodeKit(name)
    plates = k.tex("ShaderNodeTexVoronoi", 55.0, feature="DISTANCE_TO_EDGE")
    tone = k.tex("ShaderNodeTexNoise", 30.0, **{"Detail": 4.0})
    k.set(Base_Color=k.ramp(tone.outputs["Fac"], "#120a12", PAL["chitin_hi"], 0.35, 0.75), Roughness=0.28, Coat_Weight=1.0,
          Coat_Roughness=0.03, Thin_Film_Thickness=380.0, Thin_Film_IOR=1.45, Specular_IOR_Level=0.6)
    crack = k.math("SUBTRACT", 1.0, k.math("DIVIDE", plates.outputs["Distance"], 0.025, clamp=True))
    k.bump(k.math("SUBTRACT", tone.outputs["Fac"], k.math("MULTIPLY", crack, 0.8)), 0.45)
    return k.mat


def flesh_mat(name, col=PAL["flesh"], glow=0.0):
    """Tendril flesh: pink-red, scattering light deep under the surface, glistening."""
    k = NodeKit(name)
    n = k.tex("ShaderNodeTexNoise", 90.0, **{"Detail": 5.0})
    k.set(Base_Color=k.ramp(n.outputs["Fac"], "#6a1c2a", col, 0.25, 0.75), Roughness=0.35, Subsurface_Weight=0.75,
          Subsurface_Scale=0.004, Subsurface_Radius=(1.0, 0.3, 0.25), Coat_Weight=0.9, Coat_Roughness=0.06)
    rings = k.tex("ShaderNodeTexWave", 260.0, **{"Distortion": 4.0, "Detail": 2.0})
    k.bump(rings.outputs["Fac"], 0.2)
    if glow:
        k.set(Emission_Color=PAL["bile"], Emission_Strength=glow)
    return k.mat


def membrane_mat(name, glow, col=PAL["bile"], plain=False):
    """A blister's skin: a dark translucent membrane over a glowing core. The glow is strongest where
    the membrane faces the viewer and fades to dark at its swollen rim; veins shadow it. plain: no veins
    and no bump, for a 9-slice key whose centre is stretched (a vein pattern stretched leaves seams at
    the slice lines; the console fit check found them across the pressed key's label)."""
    k = NodeKit(name)
    if plain:
        lw = k.N.new("ShaderNodeLayerWeight")
        lw.inputs["Blend"].default_value = 0.5
        core = k.math("POWER", k.math("SUBTRACT", 1.0, lw.outputs["Facing"]), 5.0)
        k.set(Base_Color=list(srgb("#3a1614")) + [1.0], Roughness=0.3, Subsurface_Weight=0.5, Subsurface_Scale=0.003, Subsurface_Radius=(1.0, 0.45, 0.3),
              Coat_Weight=1.0, Coat_Roughness=0.03, Emission_Color=col, Emission_Strength=k.math("MULTIPLY", core, glow))
        return k.mat
    wv = k.warped(80.0, 0.004)
    cells = k.tex("ShaderNodeTexVoronoi", 170.0, vec=wv, feature="DISTANCE_TO_EDGE")
    vein = k.math("POWER", k.math("SUBTRACT", 1.0, k.math("DIVIDE", cells.outputs["Distance"], 0.03, clamp=True)), 3.0)
    lw = k.N.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.5
    core = k.math("POWER", k.math("SUBTRACT", 1.0, lw.outputs["Facing"]), 5.0)
    mixv = k.N.new("ShaderNodeMix")
    mixv.data_type = "RGBA"
    k.L.new(vein, mixv.inputs["Factor"])
    mixv.inputs[6].default_value = list(srgb("#4a1c18")) + [1.0]
    mixv.inputs[7].default_value = list(srgb("#16040a")) + [1.0]
    k.set(Base_Color=mixv.outputs[2], Roughness=0.3, Subsurface_Weight=0.5, Subsurface_Scale=0.003, Subsurface_Radius=(1.0, 0.45, 0.3),
          Coat_Weight=1.0, Coat_Roughness=0.03, Emission_Color=col,
          Emission_Strength=k.math("MULTIPLY", k.math("MULTIPLY", core, k.math("SUBTRACT", 1.0, vein)), glow))
    n = k.tex("ShaderNodeTexNoise", 260.0, **{"Detail": 5.0})
    k.bump(k.math("SUBTRACT", n.outputs["Fac"], vein), 0.25)
    return k.mat


def wet_mat(name, col=PAL["pit"]):
    """The wet dark pit inside a frame: almost black, glossy, gently lumpy."""
    k = NodeKit(name)
    n = k.tex("ShaderNodeTexNoise", 25.0, **{"Detail": 3.0})
    k.set(Base_Color=col, Roughness=0.25, Coat_Weight=1.0, Coat_Roughness=0.04)
    k.bump(n.outputs["Fac"], 0.25, 0.002)
    return k.mat


def glow_mat(name, col, strength):
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=0.2, Coat_Weight=1.0, Coat_Roughness=0.03, Emission_Color=col, Emission_Strength=strength, Subsurface_Weight=0.3)
    return k.mat




def steel_mat(name):
    """The captured machine's gunmetal: brushed along x, worn bright on its edges, grime pooled in its crevices (its
    own occlusion), and a faint bile stain where the brood has fed on it."""
    k = NodeKit(name)
    brush = k.tex("ShaderNodeTexNoise", 140.0, **{"Detail": 3.0})
    v = k.warped(30.0, 0.004)
    wear = k.tex("ShaderNodeTexNoise", 45.0, vec=v, **{"Detail": 6.0, "Roughness": 0.6}).outputs["Fac"]
    ao = k.N.new("ShaderNodeAmbientOcclusion")
    ao.samples, ao.only_local = 12, True
    ao.inputs["Distance"].default_value = 3.5 * MM
    grime = k.math("SUBTRACT", 1.0, k.math("POWER", ao.outputs["AO"], 1.5), clamp=True)
    c = k.ramp(brush.outputs["Fac"], PAL["steel_lo"], PAL["steel"], 0.2, 0.8)
    m = k.N.new("ShaderNodeMix")
    m.data_type = "RGBA"
    k.L.new(k.math("MULTIPLY", k.math("DIVIDE", k.math("SUBTRACT", wear, 0.58), 0.12, clamp=True), 0.45), m.inputs["Factor"])
    k.L.new(c, m.inputs[6])
    m.inputs[7].default_value = list(srgb("#3a4a22")) + [1.0]
    g = k.N.new("ShaderNodeMix")
    g.data_type = "RGBA"
    k.L.new(k.math("MULTIPLY", grime, 0.9), g.inputs["Factor"])
    k.L.new(m.outputs[2], g.inputs[6])
    g.inputs[7].default_value = list(srgb("#0c0a0c")) + [1.0]
    k.set(Base_Color=g.outputs[2], Metallic=0.85, Roughness=k.math("ADD", 0.34, k.math("MULTIPLY", wear, 0.2)))
    k.bump(k.math("MULTIPLY", wear, 0.4), 0.06)
    return k.mat


def creep_mat(name):
    """The creep's thick core: dark wet hide, its light running through a web of thin veins (not patches: mottled hide
    read as camouflage), brighter where the veins knot, under a glistening coat."""
    k = NodeKit(name)
    wv = k.warped(50.0, 0.006)
    cells = k.tex("ShaderNodeTexVoronoi", 90.0, vec=wv, feature="DISTANCE_TO_EDGE")
    vein = k.math("POWER", k.math("SUBTRACT", 1.0, k.math("DIVIDE", cells.outputs["Distance"], 0.05, clamp=True)), 3.0)
    knot = k.tex("ShaderNodeTexNoise", 18.0, **{"Detail": 2.0}).outputs["Fac"]
    mott = k.tex("ShaderNodeTexNoise", 60.0, vec=wv, **{"Detail": 6.0, "Roughness": 0.6}).outputs["Fac"]
    k.set(Base_Color=k.ramp(mott, "#1a0a0e", "#4a2024", 0.3, 0.75), Roughness=0.32, Coat_Weight=0.9, Coat_Roughness=0.06,
          Subsurface_Weight=0.35, Subsurface_Scale=0.002, Subsurface_Radius=(1.0, 0.35, 0.25), Emission_Color=PAL["bile"],
          Emission_Strength=k.math("MULTIPLY", vein, k.math("ADD", 0.6, k.math("MULTIPLY", k.math("SUBTRACT", knot, 0.45, clamp=True), 6.0))))
    k.bump(k.math("ADD", k.math("MULTIPLY", vein, 0.7), mott), 0.4)
    return k.mat


def fog_mat(name):
    """The thin wet membrane at the creep's edge: dark red-brown, translucent, so the glass and steel under it still
    show, glistening under a clear coat."""
    k = NodeKit(name)
    wv = k.warped(70.0, 0.004)
    cells = k.tex("ShaderNodeTexVoronoi", 140.0, vec=wv, feature="DISTANCE_TO_EDGE")
    vein = k.math("POWER", k.math("SUBTRACT", 1.0, k.math("DIVIDE", cells.outputs["Distance"], 0.03, clamp=True)), 3.0)
    k.set(Base_Color="#4a1a16", Roughness=0.3, Coat_Weight=1.0, Coat_Roughness=0.04, Subsurface_Weight=0.4, Subsurface_Scale=0.002,
          Subsurface_Radius=(1.0, 0.4, 0.3), Alpha=k.math("ADD", 0.62, k.math("MULTIPLY", vein, 0.3)))
    k.bump(k.math("SUBTRACT", 0.5, vein), 0.3)
    return k.mat


def M(K, key):
    """The skin's materials by role, made once per piece."""
    makers = {
        "steel": lambda: steel_mat("steel"), "bolt": lambda: steel_mat("bolt"),
        "hide": lambda: hide_mat("hide", 0.8), "creep": lambda: creep_mat("creep"), "fog": lambda: fog_mat("fog"), "chitin": lambda: chitin_mat("chitin"),
        "flesh": lambda: flesh_mat("flesh"), "sinew": lambda: flesh_mat("sinew", PAL["sinew"]), "pit": lambda: wet_mat("pit"),
        "pod_idle": lambda: membrane_mat("pod_idle", 0.3), "pod_hover": lambda: membrane_mat("pod_hover", 1.6),
        "pod_hot": lambda: membrane_mat("pod_hot", 2.4, PAL["bile_hot"]), "pustule": lambda: membrane_mat("pustule", 2.0),
        # the symbol bar's spore eggs: one membrane and one vein pattern, only the glow differs (lit and unlit match)
        "egg_off": lambda: membrane_mat("egg_off", 0.12), "egg_on": lambda: membrane_mat("egg_on", 2.2),
        "egg_warn": lambda: membrane_mat("egg_warn", 2.4, PAL["blood"]),
        "bile": lambda: glow_mat("bile", PAL["bile"], 2.2), "blood": lambda: glow_mat("blood", PAL["blood"], 2.2),
        "amber": lambda: glow_mat("amber", PAL["amber"], 2.2), "groove": lambda: printed("groove", "#060406"),
    }
    return K.mat(key, makers[key])


# The films behind the captured glass: (colour, its shadow at the rim, emission). The brood's light is bile: dark glass
# on frames and screens; keys glow dimly at rest (their label must read), brighter on hover, brightest pressed; the
# helm's centre and the name window glow warm; the pocket holds the eye.
FILMS = dict(dim=("#16261a", "#040806", 0.3), low=("#7fb04a", "#14200c", 0.6), warm=(PAL["bile"], "#1a2a0c", 1.0),
             hot=(PAL["bile_hot"], "#2a3a10", 1.5), hearth=(PAL["bile_hot"], "#24300c", 1.2), gold=("#4e7a2c", "#0a1406", 0.45),
             pocket=("#c4566a", "#2a0a10", 0.9), slot=("#14201a", "#040606", 0.15), lamp_green=(PAL["bile"], "#1a3010", 1.8),
             lamp_yellow=(PAL["amber"], "#3a2a08", 1.8), lamp_red=(PAL["blood"], "#3a0806", 1.8))
INKS = ("iris", "pupil")
DISPLAY = tuple(FILMS) + INKS
LAMP = (0.78, 1.0, 0.55)                         # the lamps under the lips, turned bile by the brood
LAMP_P = float(os.environ.get("LAMP_P", 0.3))
MAIN = (0.7, 60.0)


def glass(K, s):
    a = min(7.0, -s["dz"] * 0.6)
    K.m.update(plate=M(K, "steel"), knob=M(K, "bolt"), bezel=K.mat("bezel", lambda: printed("bezel", "#1a1c1e", 0.35)), groove=M(K, "groove"))
    for kind, (col, lo, es) in FILMS.items():
        K.m[kind] = K.mat(kind, lambda col=col, lo=lo, es=es, kind=kind: film_mat(kind, col=col, shadow=lo, es=es, ao_mm=a))
    K.m["iris"] = K.mat("iris", lambda: glow_mat("iris", PAL["amber"], 1.6))
    K.m["pupil"] = K.mat("pupil", lambda: printed("pupil", "#0a0306"))


def P(pts):
    return [tuple(p) for p in pts]


def growth(K, s):
    """The brood's layer over the captured machine: creep (a thick core of lit hide inside a thin membrane), pustules
    over the bolts, chitin ridges and horns, sinew-wrapped conduits and the portrait pocket's lip."""
    top = K.pt
    for c in s["conduits"]:
        K.tube(P(c["pts"]), [c["r"]] * len(c["pts"]), M(K, "steel"), flat=0.8, z=[top + c["r"] * 0.6] * len(c["pts"]))
        for w in c["wraps"]:
            K.tube(P(w), [1.1, 1.3, 1.1], M(K, "sinew"), flat=0.8, z=[top + c["r"] * 0.9] * 3)
    for outline, core in (zip(s["creep"], s["core"])):
        ob = K.slab([P(outline)], top - 0.6, top + 0.9, M(K, "fog"), bevel=0.7)   # outlines come drawn in by their bevel
        ob.visible_shadow = False
        if core:
            K.slab([P(core)], top - 0.4, top + 4.4, M(K, "creep"), bevel=2.3)
            xs, ys = [p[0] for p in core], [p[1] for p in core]
            span = min(max(xs) - min(xs), max(ys) - min(ys))
            if span > 10:   # nodules swelling on the creep, inside its core: thick knots beside the thin membrane
                cx, cy = sum(xs) / len(xs), sum(ys) / len(ys)
                for j in range(3 if span > 24 else 1):
                    a = 2.1 * j + len(core) * 0.37
                    r = span * (0.16 if j else 0.22)
                    K.blob(cx + math.cos(a) * span * 0.18 * (j > 0), cy + math.sin(a) * span * 0.18 * (j > 0), r, r * 0.9, r * 0.55,
                           M(K, "creep"), z=top + 4.0, lumps=0.0)
    if s["pocket"]:
        ring = [P(r) for r in s["pocket"]]
        K.slab(ring, top - 0.5, top + 3.4, M(K, "flesh"), bevel=1.6)
    for x, y, r in s["pustules"]:
        K.blob(x, y, r * 1.2, r * 1.2, r * 0.45, M(K, "hide"), z=top + 3.0, lumps=0.0)
        K.blob(x, y, r * 0.85, r * 0.85, r * 0.75, M(K, "pustule"), z=top + 3.6)
    for rg in s["ridges"]:
        z = [top + r * 0.55 for r in rg["radii"]]
        K.tube(P(rg["pts"]), rg["radii"], M(K, "chitin"), flat=0.75, z=z)
        if len(rg["pts"]) >= 5:   # a spine: vertebral knobs along it, thick and thin
            for (x, y), r in zip(rg["pts"][1:-1], rg["radii"][1:-1]):
                K.blob(x, y, r * 1.25, r * 1.1, r * 0.9, M(K, "chitin"), z=top + r * 0.9)


def piece(name):
    def build(K):
        s = SHAPES[name]
        K.roll, K.dz = s["roll"], s["dz"]
        K.pt = min(8.0, 2 * s["roll"] + 2.0)
        glass(K, s)
        wells = [(kind, P(w)) for kind, w in s["wells"]]
        plate = P(s["plate"])
        for (kind, _w), f in zip(wells, s["films"]):   # films measured to fit inside the plate (rule B)
            K.film(None, shape=P(f), m=K.m[kind])
        K.plate([plate] + [w for _k, w in wells])
        for kind, w in wells:
            gap = min(min(math.hypot(x - px, y - py) for px, py in plate) for x, y in w)
            if gap >= 5.0:
                K.bezel(w, grow=1.0, wall=gap >= 7.0)
            if kind.startswith("lamp_") or kind in ("slot", "pocket") or name.startswith(("button.", "tab.")):
                continue   # keys and tabs are backlit only: a lamp's hot spot under the lip would cross their label
            xs, ys = [p[0] for p in w], [p[1] for p in w]
            x0, x1, y0 = min(xs), max(xs), min(ys)
            n = 2 if x1 - x0 > 140 else 1
            for j in range(n):   # the machine's lamps under the top lip
                K.lip_light(x0 + (x1 - x0) * (j + 1) / (n + 1), y0 + 1.5, LAMP_P * (0.4 if kind == "dim" else 1.0), colour=LAMP)
        if name == "nameplate":   # the brood's eye looks out of the pocket: the captain's portrait
            cx, cy = 30, 28
            K.flat([[(cx + 10 * math.cos(a), cy + 9 * math.sin(a)) for a in [i * math.pi / 24 for i in range(48)]]], K.m["iris"])
            K.flat([[(cx + 2.4 * math.cos(a), cy + 8 * math.sin(a)) for a in [i * math.pi / 16 for i in range(32)]]], K.m["pupil"], z=K.dz + 0.1)
        for x, y, r in s["bolts"]:   # the machine's hex bolts (most now under pustules)
            K.cyl(x, y, r, K.pt - 0.2, K.pt + 0.9, K.m["knob"], seg=6, smooth=False)
        growth(K, s)
        if name == "crest":
            x, y, rx, ry = EGG
            K.blob(x, y + 2, rx * 1.15, ry * 1.08, 6.0, M(K, "hide"), z=K.pt + 1.0)
            K.blob(x, y, rx, ry, rx * 0.9, M(K, "pod_hot"), z=K.pt + 4.0, lumps=0.04)
        link_layers(DISPLAY, tuple(FILMS) + ("iris",), main=MAIN)
    return build


def egg(key):
    """A spore egg, the Vhorr bar's symbol, in a ring of hide: the same membrane in every state, only its glow differs."""
    def build(K):
        out = P(CELL["outline"])
        K.slab([out], 0.0, 1.0, M(K, "hide"), bevel=0.4)
        w, h = CELL["size"]
        K.blob(w / 2, h / 2 + 0.2, w / 2 - 1.3, h / 2 - 1.4, 2.4, M(K, key), z=1.2)
    return build


def bar_fill(key):
    def build(K):
        w, h = K.w, K.h
        K.tube([(4, h / 2), (w / 2, h / 2), (w - 4, h / 2)], [4.2, 4.2, 4.2], M(K, key), flat=0.5)
    return build


# Icons: small organic sculptures, lit with bile so they read at 48 px.
def icon(name):
    def build(K):
        c = 24
        fl, bl, ch = M(K, "flesh"), M(K, "bile"), M(K, "chitin")
        if name == "helm":
            p = curl(26, 22, 18, math.pi * 0.9, 0.85, 1, 8)
            K.tube(p, taper(8, 5, 1.2), fl)
            K.blob(p[0][0], p[0][1], 4, 4, 3, bl)
        elif name == "fire":
            K.tube([(6, 10), (16, 19), (22, 24)], [4, 3, 2], fl)
            K.tube([(6, 38), (16, 29), (22, 24)], [4, 3, 2], fl)
            K.blob(32, 24, 6, 6, 4, bl)
            K.blob(42, 22, 3, 3, 2, bl)
        elif name == "shields":
            for i in range(3):
                K.blob(14 + i * 9, 30 - i * 7, 12, 8, 4, ch, z=i, rot=0.5, lumps=0.05)
        elif name == "tractor":
            K.tube([(4, 10), (16, 18), (24, 24)], [3.4, 2.6, 1.6], fl)
            K.tube([(4, 38), (16, 30), (24, 26)], [3.4, 2.6, 1.6], fl)
            K.blob(34, 24, 7, 7, 5, M(K, "pod_hover"))
        elif name == "transport":
            for i, (x, y, r) in enumerate(((14, 12, 4), (30, 10, 3), (22, 22, 5), (36, 26, 3.5), (12, 32, 3), (26, 38, 4))):
                K.blob(x, y, r, r, r * 0.8, bl if i % 2 else M(K, "pod_hover"))
        elif name == "science":
            K.tube([(24, 42), (22, 26), (12, 8)], [3, 2.2, 1.2], fl)
            K.tube([(24, 42), (26, 26), (36, 8)], [3, 2.2, 1.2], fl)
            K.blob(12, 8, 3, 3, 2, bl)
            K.blob(36, 8, 3, 3, 2, bl)
        elif name == "comms":
            K.blob(14, 24, 7, 7, 5, M(K, "pod_hover"))
            for r in (13, 20):
                p = [(14 + math.cos(a) * r, 24 + math.sin(a) * r) for a in (-0.9, 0.0, 0.9)]
                K.tube(p, [1.6] * 3, bl)
        elif name == "repair":
            K.tube([(24, 44), (24, 30), (20, 20)], [4, 3, 2.4], fl)
            K.blob(20, 14, 8, 10, 6, M(K, "pod_hover"))
        elif name == "map":
            n = [(8, 10), (24, 18), (40, 8), (14, 36), (34, 38)]
            for a, b in ((0, 1), (1, 2), (1, 3), (1, 4)):
                K.tube([n[a], ((n[a][0] + n[b][0]) / 2 + 2, (n[a][1] + n[b][1]) / 2 - 2), n[b]], [1.6, 1.4, 1.6], fl)
            for x, y in n:
                K.blob(x, y, 3.2, 3.2, 2.4, bl)
        elif name == "settings":
            K.blob(c, c, 17, 15, 7, M(K, "pod_idle"), lumps=0.05)
            K.blob(c + 2, c + 1, 6, 6, 5, M(K, "blood"), z=5.0)
    return build




ICONS = ["helm", "fire", "shields", "tractor", "transport", "science", "comms", "repair", "map", "settings"]
PIECES = {}
for _n, _s in SHAPES.items():
    PIECES[_n] = (_s["kind"], tuple(_s["size"]), tuple(_s["margins"]) if _s["margins"] else None, _s["stretch"], piece(_n), _s["inset"])
PIECES["bar.fill"] = ("nine", (64, 10), (0, 6, 0, 6), "x", bar_fill("bile"))
PIECES["bar.fill.red"] = ("nine", (64, 10), (0, 6, 0, 6), "x", bar_fill("blood"))
for _st, _key in (("off", "egg_off"), ("on", "egg_on"), ("warn", "egg_warn")):
    PIECES[f"bar.cell.{_st}"] = ("cell", tuple(CELL["size"]), None, None, egg(_key), CELL["gap"])
ORDER = ["frame.panel", "frame.inset", "bar.track", "bar.fill", "bar.fill.red", "bar.cell.off", "bar.cell.on", "bar.cell.warn",
         "button.idle", "button.hover", "button.pressed", "tab.idle", "tab.active", "strip.top", "strip.bottom", "divider", "nameplate",
         "helm.pad", "ssd.bezel", "dial", "lamp.alert.green", "lamp.alert.yellow", "lamp.alert.red", "crest"]
PIECES = {k: PIECES[k] for k in ORDER}
PIECES.update({f"icon.{n}": ("hero", (48, 48), None, None, icon(n)) for n in ICONS})

# A cool room light from the upper left shows the steel and the wet growth; a violet rim from the lower right catches
# the chitin; link_layers adds the softbox, the ceiling and the main light. The world is dark: the light that matters
# comes from inside the brood.
LIGHTS = [("Room", (0.3, -1.0, -1.6), 1.1, (0.86, 0.9, 1.0), 10.0), ("Rim", (-1.0, 1.0, -0.7), 0.6, (0.75, 0.55, 1.0), 8.0)]


def render_setup(w_px, h_px, samples):
    ui_render.setup(w_px, h_px, samples, LIGHTS, world=(0.018, 0.012, 0.02),
                    bounces=dict(max_bounces=8, glossy_bounces=4, transmission_bounces=4, diffuse_bounces=3))


if __name__ == "__main__":
    ui_render.run("vhorr", PIECES, Panel, render_setup, samples=48)
