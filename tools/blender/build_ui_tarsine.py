"""Fleet Command: the Tarsine Remnant console skin, modelled, lit and rendered in Blender.

Owns every piece of the Remnant skin of the shared console UI kit, rebuilt on the analog panel bible (owner, 2026-10-31:
"I love the Remnant ui, i htink it could use some polish from our guide, but please dont remove the grimey industrial
feel, I love that story telling so much"; docs/design/ui-bible.html and the blender-ui-panels skill). The ancient race's
language (design.md: "a powerful ancient race. rusty, with back lit buttons clogged with dirt. their ships havent been
upgraded in ages, rough around the edges") in the bible's two layers:

- the metal: monumental wrought iron whose every edge rolls over in a worn round, its grain drawn out along the plate,
  hammer facets catching the light, edges rubbed bright, soot pooled in every crevice (the plate's own ambient
  occlusion decides where grime collects) with rust and verdigris there and in a few small blooms, great domed rivets
  with rust streaks running down from them onto the flat metal, worn gold bezels, brackets and crown. A frame's well
  holds a floor of the same iron, darker: the panel's face, which the readouts are set on (version 2, owner,
  2026-11-08: "tarsine background is terrible, it should be like a metal texture, but here it just looks like sludge
  stretched out");
- the display: grimy glass lit from behind. A layer of caked dirt lies on every window's glass, the same dirt in every
  state of a key, a tab and a bar cell (owner: "sync up dust with out and unlit button to give it that illusion"): only
  the backlight behind it changes. The gauges' glass is cracked.

The light is the bible's "spots" option, dimmed and uneven: one or two old lamps under each window's top lip, some
weaker than others (deterministic per piece), because the Remnant's fixtures were never replaced and not all of them
still burn; a weak warm room light from the upper left shows the corrosion, and the soft main light shades the lips.

The shapes come from assets/ui/tarsine/shapes.json, written and fit-checked by tools/mockups/tarsine_shapes.py (run it
first); this script models exactly those outlines through tools/blender/ui_shapes.py's Panel and renders them face on in
Cycles through the shared harness (tools/blender/ui_render.py) at twice the slot size, with a manifest.

Nothing is a small texture stretched over a large area. The pieces the battle console draws large are rendered at the
size it draws them (frame.panel at the expanded panel's 1268 x 1040, the strips at 1440 px, the column whole at 480 x
1080, its ground at 480 x 1080); every other nine-sliced piece samples its textures through nine_co(), which folds each
stretched band of the slot to a line, so a stretch of it shows no smear.

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
PAL = dict(iron="#4a443d", iron_lo="#2b2723", rust="#6e3418", rust_hi="#a5562a", verd="#4b6656", soot="#15110d",
           gold="#a88a46", light="#d6c76a", ember="#d2482a", bone="#e8dcc0",
           # the worked metal (version 2, 2026-11-08): the iron's rubbed high points, its bare worn edges, a panel's face
           iron_hi="#6e685e", bright="#a49a88", floor="#2c2824", floor_lo="#181512")
# The films behind the glass: (colour, its shadow at the rim, emission). Dark glass on frames; keys glow dimly at rest
# (their ink must read), brighter on hover, brightest pressed; the helm's centre is a hearth.
FILMS = dict(dim=("#3a3322", "#0a0806", 0.22), low=(PAL["light"], "#2a2412", 0.5), warm=(PAL["light"], "#3a3216", 0.9),
             hot=("#ffe08a", "#4a3a14", 1.45), hearth=("#ffd36a", "#4a3410", 1.25), gold=("#e2c67a", "#3a2c10", 1.05),
             slot=("#3a3020", "#0c0a08", 0.12), lamp_green=("#8ad46a", "#1a3010", 1.8), lamp_yellow=(PAL["light"], "#3a3010", 1.8),
             lamp_red=(PAL["ember"], "#3a1006", 1.8), accord=("#a9b98a", "#1c2416", 1.0), seam=("#0c0a08", "#050403", 0.0))
PRESS_MM = 3.0                # how far a pressed key's glass travels, down and in (3 px at the kit's 1x)
KEY_FILMS = {"dim": "low"}   # a key at rest glows a little (its window is never dark, or its label would not read)
LAMP = (1.0, 0.78, 0.48)                         # the old lamps' colour
LAMP_P = float(os.environ.get("LAMP_P", 0.22))   # their power (times the lamp scale), before each lamp's own age
MAIN = (0.7, 60.0)                               # the soft main light on the wells: strength, angular size (deg)
INKS = ("dirt", "crack", "cage")
DISPLAY = tuple(FILMS) + INKS + ("cell_off", "cell_on", "cell_warn", "fill_light", "fill_ember")
# Pieces rendered at the size the battle console draws them (1920 x 1080 reference): their textures are not folded.
NATIVE = ("frame.panel", "strip.top", "strip.bottom")
FLOOR = "floor"   # a well holding a panel's face of worked iron (metal layer, no glass, no lamps), not a lit film


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
    n.samples, n.only_local = 6, True
    n.inputs["Distance"].default_value = mm * MM
    return n.outputs["AO"]


# The piece being built: (w, h, margins, stretch) of a nine-sliced piece, or None for a hero. Set by piece() and cell()
# before their materials are made, so every texture knows where the page will stretch it.
SLICE = None


def nine_co(k):
    """The texture coordinates of a nine-sliced piece, safe to stretch (owner, 2026-11-08: "it just looks like sludge
    stretched out"): the object coordinates with each stretched band of the slot folded to a single line, so a texture
    is constant across the band and a stretch of it shows no smear, and the far margin carries on from the near one
    with no seam. A hero (drawn at its size) keeps its coordinates as they are."""
    if not SLICE or SLICE[3] is None:
        return k.co
    w, h, (mt, mr, mb, ml), stretch = SLICE
    sep = k.N.new("ShaderNodeSeparateXYZ")
    k.L.new(k.co, sep.inputs[0])
    out = k.N.new("ShaderNodeCombineXYZ")

    def fold(v, lo, hi):   # v - clamp(v - lo, 0, hi - lo): constant on [lo, hi], continuous either side
        return k.math("SUBTRACT", v, k.math("MINIMUM", k.math("MAXIMUM", k.math("SUBTRACT", v, lo), 0.0), hi - lo))
    x, y = sep.outputs["X"], sep.outputs["Y"]
    if "x" in stretch:
        x = fold(x, (ml - w / 2) * MM, (w / 2 - mr) * MM)
    if "y" in stretch:
        y = fold(y, (mb - h / 2) * MM, (h / 2 - mt) * MM)
    k.L.new(x, out.inputs[0])
    k.L.new(y, out.inputs[1])
    k.L.new(sep.outputs["Z"], out.inputs[2])
    return out.outputs[0]


def kit(name):
    """A NodeKit whose textures sample the stretch-safe coordinates (nine_co)."""
    k = NodeKit(name)
    k.co = nine_co(k)
    return k


def edges(k, mm):
    """1 on a convex edge (a roll's crown, a rivet's rim), 0 on flat metal: the occlusion seen from inside the solid."""
    n = k.N.new("ShaderNodeAmbientOcclusion")
    n.samples, n.only_local, n.inside = 4, True, True
    n.inputs["Distance"].default_value = mm * MM
    return k.math("SUBTRACT", 1.0, n.outputs["AO"], clamp=True)


def worked(k):
    """The worked surface every Remnant iron shares (owner, 2026-11-08: "it should be like a metal texture"). Wrought iron:
    its slag fibres, drawn out along the plate when it was rolled, show through the weathered face as a fine grain of
    long streaks, lighter and darker; ages of hands and cloths have rubbed that grain smooth in places and dulled it in
    others; a smith's hammer left shallow facets that catch the light; the tone of the metal drifts slowly across a big
    plate. Returns (grain, rub, facets, tone), each 0-1."""
    along = k.N.new("ShaderNodeMapping")   # long in x, fine in y: the grain runs along the plate
    along.inputs["Scale"].default_value = (0.012, 1.0, 1.0)
    k.L.new(k.warped(12.0, 0.0006), along.inputs["Vector"])
    grain = k.tex("ShaderNodeTexNoise", 800.0, vec=along.outputs[0], **{"Detail": 8.0, "Roughness": 0.6}).outputs["Fac"]
    broad = k.N.new("ShaderNodeMapping")
    broad.inputs["Scale"].default_value = (0.05, 1.0, 1.0)
    k.L.new(k.co, broad.inputs["Vector"])
    rub = k.tex("ShaderNodeTexNoise", 60.0, vec=broad.outputs[0], **{"Detail": 3.0}).outputs["Fac"]
    vor = k.tex("ShaderNodeTexVoronoi", 55.0, vec=k.warped(12.0, 0.003), **{"Randomness": 1.0})
    vor.feature = "SMOOTH_F1"
    facets = k.math("MINIMUM", k.math("MULTIPLY", vor.outputs["Distance"], 1.4), 1.0)
    tone = k.tex("ShaderNodeTexNoise", 4.0, vec=k.warped(3.0, 0.02), **{"Detail": 2.0}).outputs["Fac"]
    return grain, rub, facets, tone


def relief(k, layers):
    """Chain bump nodes (height socket, strength, distance in mm) into the shader's normal."""
    prev = None
    for h, st, mm in layers:
        b = k.N.new("ShaderNodeBump")
        b.inputs["Strength"].default_value = st
        b.inputs["Distance"].default_value = mm * MM
        k.L.new(h, b.inputs["Height"])
        if prev is not None:
            k.L.new(prev, b.inputs["Normal"])
        prev = b.outputs[0]
    k.L.new(prev, k.bsdf.inputs["Normal"])


def iron(name, tone_bias=0.0):
    """Monumental wrought iron, worn (owner, 2026-11-08: "it should be like a metal texture"): dark metal with a sheen and
    a grain (worked()), its rolled edges rubbed bright by ages of hands; soot pooled wherever the plate's own occlusion
    says grime collects, and rust only there and in a few small blooms (the decline they will not admit shows in the
    crevices and under the rivets, not as a stain across the plate); a little verdigris in the deepest corners."""
    k = kit(name)
    grain, rub, facets, tone = worked(k)
    occ = ao(k, 4.0)
    grime = k.math("SUBTRACT", 1.0, k.math("POWER", occ, 1.6), clamp=True)
    rust_n = k.tex("ShaderNodeTexNoise", 60.0, vec=k.warped(40.0, 0.004), **{"Detail": 8.0, "Roughness": 0.62}).outputs["Fac"]
    bloom = band(k, rust_n, 0.66 - tone_bias, 0.76 - tone_bias)
    rust = k.math("MAXIMUM", k.math("MULTIPLY", band(k, grime, 0.25, 0.7), band(k, rust_n, 0.42, 0.6)), k.math("MULTIPLY", bloom, 0.7))
    verd = k.math("MULTIPLY", band(k, grime, 0.55, 0.9), band(k, k.tex("ShaderNodeTexNoise", 30.0, **{"Detail": 4.0}).outputs["Fac"], 0.55, 0.65))
    wear = band(k, edges(k, 1.2), 0.08, 0.35)
    c = mix(k, tone, PAL["iron_lo"], PAL["iron"])
    c = mix(k, band(k, grain, 0.3, 0.75), mix(k, 0.0, PAL["iron_lo"], PAL["iron_lo"]), c)
    c = mix(k, k.math("MULTIPLY", band(k, grain, 0.55, 0.8), 0.6), c, PAL["iron_hi"])
    c = mix(k, k.math("MULTIPLY", wear, 0.85), c, PAL["bright"])
    c = mix(k, k.math("MULTIPLY", grime, 0.85), c, PAL["soot"])
    c = mix(k, rust, c, PAL["rust"])
    c = mix(k, k.math("MULTIPLY", rust, band(k, grain, 0.6, 0.8)), c, PAL["rust_hi"])
    c = mix(k, k.math("MULTIPLY", verd, 0.6), c, PAL["verd"])
    bare = k.math("SUBTRACT", 1.0, k.math("MAXIMUM", rust, k.math("MULTIPLY", grime, 0.7)))
    k.set(Base_Color=c, Metallic=k.math("ADD", 0.2, k.math("MULTIPLY", bare, 0.7)),
          Roughness=k.math("ADD", 0.3, k.math("ADD", k.math("MULTIPLY", rust, 0.4),
                                                  k.math("ADD", k.math("MULTIPLY", rub, 0.22), k.math("MULTIPLY", k.math("SUBTRACT", 1.0, wear), 0.08)))))
    relief(k, [(facets, 0.6, 2.0), (grain, 0.25, 0.25), (rust_n, 0.2, 0.3)])
    return k.mat


def floor(name):
    """A panel's face: the floor of a frame's well, dark worked iron the readouts are set on. The same grain and facets
    as the frame (worked()), darker and calmer so bone and gold letters read on it at a glance; soot pooled at the foot of
    the well's lip and rust in its corners; faint grime run down the face in streaks (an old ship's damp)."""
    k = kit(name)
    grain, rub, facets, tone = worked(k)
    occ = ao(k, 10.0)
    grime = k.math("SUBTRACT", 1.0, k.math("POWER", occ, 1.4), clamp=True)
    down = k.N.new("ShaderNodeMapping")   # damp runs down the face: long in y
    down.inputs["Scale"].default_value = (1.0, 0.06, 1.0)
    k.L.new(k.co, down.inputs["Vector"])
    runs = k.tex("ShaderNodeTexNoise", 70.0, vec=down.outputs[0], **{"Detail": 5.0, "Roughness": 0.6}).outputs["Fac"]
    run = k.math("MULTIPLY", band(k, runs, 0.55, 0.75), 0.35)
    rust_n = k.tex("ShaderNodeTexNoise", 60.0, vec=k.warped(40.0, 0.004), **{"Detail": 8.0}).outputs["Fac"]
    rust = k.math("MAXIMUM", k.math("MULTIPLY", band(k, grime, 0.2, 0.7), band(k, rust_n, 0.4, 0.58)),
                  k.math("MULTIPLY", band(k, rust_n, 0.72, 0.8), 0.5))
    c = mix(k, tone, PAL["floor_lo"], PAL["floor"])
    c = mix(k, band(k, grain, 0.3, 0.75), PAL["floor_lo"], c)
    c = mix(k, k.math("MULTIPLY", band(k, grain, 0.55, 0.8), 0.5), c, PAL["iron"])
    c = mix(k, run, c, PAL["soot"])
    c = mix(k, k.math("MULTIPLY", grime, 0.8), c, PAL["soot"])
    c = mix(k, k.math("MULTIPLY", rust, 0.85), c, PAL["rust"])
    k.set(Base_Color=c, Metallic=k.math("SUBTRACT", 0.88, k.math("MULTIPLY", k.math("ADD", rust, run), 0.5)),
          Roughness=k.math("ADD", 0.32, k.math("ADD", k.math("MULTIPLY", rub, 0.2), k.math("MULTIPLY", k.math("ADD", rust, run), 0.3))))
    relief(k, [(facets, 0.5, 2.0), (grain, 0.22, 0.25)])
    return k.mat


def gold(name):
    """Old gold worn through: polished where hands rubbed it, sooty in its hollows, flecked with rust from the iron."""
    k = kit(name)
    n = k.tex("ShaderNodeTexNoise", 70.0, **{"Detail": 6.0}).outputs["Fac"]
    grime = k.math("SUBTRACT", 1.0, k.math("POWER", ao(k, 2.5), 1.4), clamp=True)
    c = mix(k, band(k, n, 0.58, 0.7), PAL["gold"], PAL["rust"])
    c = mix(k, k.math("MULTIPLY", grime, 0.85), c, PAL["soot"])
    k.set(Base_Color=c, Metallic=0.9, Roughness=k.math("ADD", 0.32, k.math("MULTIPLY", n, 0.25)))
    k.bump(n, 0.15)
    return k.mat


def dirt(name, rim=None):
    """The caked dirt on every window's glass: patches, specks and a smear, translucent; one material, one place, so a
    window's dirt is the same lit or unlit (only the backlight behind it changes). With rim (x0, y0, x1, y1) px, the
    window of a display that stretches to any size (an inset), the dirt lies only in a band rim[4] px wide along its
    edges, so its open middle is clean dark glass that stretches with no smear."""
    k = kit(name)
    caked = k.tex("ShaderNodeTexNoise", 48.0, vec=k.warped(20.0, 0.003), **{"Detail": 6.0}).outputs["Fac"]
    specks = k.tex("ShaderNodeTexNoise", 650.0, **{"Detail": 1.0}).outputs["Fac"]
    a = k.math("MAXIMUM", k.math("MULTIPLY", band(k, caked, 0.46, 0.7), 0.78), k.math("MULTIPLY", band(k, specks, 0.62, 0.7), 0.85))
    if rim:
        x0, y0, x1, y1, wd = rim
        sep = k.N.new("ShaderNodeSeparateXYZ")
        k.L.new(k.N.new("ShaderNodeTexCoord").outputs["Object"], sep.inputs[0])
        px = k.math("ADD", k.math("DIVIDE", sep.outputs["X"], MM), SLICE[0] / 2 if SLICE else 0.0)
        py = k.math("SUBTRACT", SLICE[1] / 2 if SLICE else 0.0, k.math("DIVIDE", sep.outputs["Y"], MM))
        d = k.math("MINIMUM", k.math("MINIMUM", k.math("SUBTRACT", px, x0), k.math("SUBTRACT", x1, px)),
                   k.math("MINIMUM", k.math("SUBTRACT", py, y0), k.math("SUBTRACT", y1, py)))
        a = k.math("MULTIPLY", a, k.math("SUBTRACT", 1.0, band(k, d, wd * 0.4, wd)))
    k.set(Base_Color="#2a1e12", Roughness=0.95, Alpha=a)
    return k.mat


def dirt_key(name, rx_mm=50.0, ry_mm=13.5):
    """The same caked dirt on a key's glass, wiped from its middle by fingers (owner, 2026-10-31: "make the middle area
    of the button kinda clear because fingers have pressed on it. so the grime should mostly be along the edges"): an
    oval rx_mm by ry_mm round the key's middle, ragged at its rim, where only a faint film of dirt is left (owner: "I need like a clean area where people mush their fingers on
    the dust, also makes text more legible"). The mask runs
    in the dirt slab's object coordinates (origin at the slot's middle), so it moves with the glass when a key is
    pressed."""
    k = kit(name)
    caked = k.tex("ShaderNodeTexNoise", 48.0, vec=k.warped(20.0, 0.003), **{"Detail": 6.0}).outputs["Fac"]
    specks = k.tex("ShaderNodeTexNoise", 650.0, **{"Detail": 1.0}).outputs["Fac"]
    a = k.math("MAXIMUM", k.math("MULTIPLY", band(k, caked, 0.40, 0.66), 0.85), k.math("MULTIPLY", band(k, specks, 0.6, 0.7), 0.85))
    sep = k.N.new("ShaderNodeSeparateXYZ")   # the oval in the slab's own coordinates (not folded: it is smooth)
    k.L.new(k.N.new("ShaderNodeTexCoord").outputs["Object"], sep.inputs[0])
    dx = k.math("DIVIDE", sep.outputs["X"], rx_mm * MM)
    dy = k.math("DIVIDE", sep.outputs["Y"], ry_mm * MM)
    d = k.math("SQRT", k.math("ADD", k.math("MULTIPLY", dx, dx), k.math("MULTIPLY", dy, dy)))
    rag = k.tex("ShaderNodeTexNoise", 30.0, **{"Detail": 4.0}).outputs["Fac"]
    edge = band(k, k.math("ADD", d, k.math("MULTIPLY", k.math("SUBTRACT", rag, 0.5), 0.35)), 0.74, 1.02)
    k.set(Base_Color="#2a1e12", Roughness=0.95, Alpha=k.math("MULTIPLY", a, k.math("ADD", 0.02, k.math("MULTIPLY", edge, 0.98))))
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
               floor=K.mat("floor", lambda: floor("floor")),
               dirt=K.mat("dirt", lambda: dirt("dirt")), dirt_key=K.mat("dirt_key", lambda: dirt_key("dirt_key")), streak=K.mat("streak", lambda: streak("streak")),
               crack=K.mat("crack", lambda: glow("crack", PAL["bone"], 0.35)), cage=K.mat("cage", lambda: printed("cage", "#1a140e")))
    for kind, (col, lo, es) in FILMS.items():
        K.m[kind] = K.mat(kind, lambda col=col, lo=lo, es=es, kind=kind: film_mat(kind, col=col, shadow=lo, es=es, ao_mm=a))


def age(name, i):
    """How much an old lamp still gives, 0.35-1: fixed per piece (keys share one across their states) and lamp."""
    base = name.split(".")[0] + (name.split(".")[1] if name.startswith(("strip", "lamp", "frame")) else "")
    return 0.35 + 0.65 * ((zlib.crc32(f"{base}{i}".encode()) % 1000) / 1000)


def piece(name):
    def build(K):
        global SLICE
        s = SHAPES[name]
        # pieces rendered at the size the console draws them (NATIVE) keep their texture whole; the rest fold theirs
        SLICE = (s["size"][0], s["size"][1], s["margins"], None if name in NATIVE else s["stretch"]) if s["kind"] == "nine" else None
        K.roll, K.dz = s["roll"], s["dz"]
        K.pt = min(8.0, 2 * s["roll"] + 2.0)
        mats(K, s)
        is_key = name.startswith("button.")
        # a display that stretches to any size (an inset, a cell) keeps its dirt to a band along its rim and has no lamp
        # whose spot the stretch would smear (owner, 2026-11-08)
        stretchy = name in ("frame.inset",)
        wells = [(KEY_FILMS.get(kind, kind) if is_key else kind, [tuple(p) for p in w]) for kind, w in s["wells"]]
        plate = [tuple(p) for p in s["plate"]]
        # A pressed key's glass is pushed in: down PRESS_MM and back PRESS_MM, its dirt with it, and the page's label
        # sinks the same PRESS_MM (skins/tarsine.js --btn-sink), so glass, dirt and letters move as one and the iron stays.
        press = PRESS_MM if name == "button.pressed" else 0.0
        for (kind, w), f in zip(wells, s["films"]):  # films measured to fit inside the plate (rule B), and their dirt
            shape = [tuple(p) for p in f]
            if kind == FLOOR:   # a panel's face: worked iron on the well's floor, lit by the room like the frame
                K.film(None, shape=shape, m=K.m["floor"], z=K.dz)
                continue
            if kind == "seam":
                K.film(None, shape=shape, m=K.m[kind], z=K.dz)
                continue
            dm = K.m["dirt_key"] if name.startswith("button.") else K.m["dirt"]
            if stretchy:
                xs, ys = [p[0] for p in w], [p[1] for p in w]
                dm = K.mat("dirt_rim", lambda: dirt("dirt_rim", rim=(min(xs), min(ys), max(xs), max(ys), 7.0)))
            glass = [K.film(None, shape=shape, m=K.m[kind], z=K.dz - press),
                     K.slab([shape], K.dz - press + 0.12, K.dz - press + 0.16, dm, bevel=0.0)]
            glass[1].visible_shadow = False
            for ob in glass:
                ob.location.y -= press * MM
        K.plate([plate] + [w for _k, w in wells])
        lamp = 0
        for kind, w in wells:
            gap = min(min(math.hypot(x - px, y - py) for px, py in plate) for x, y in w)
            if gap >= 5.0 and kind != "seam":  # a worn gold bezel, and a dark well wall where the land has room for them
                K.bezel(w, grow=1.0, wall=gap >= 7.0)
            if kind.startswith("lamp_") or kind in ("slot", "seam", FLOOR) or stretchy or name.startswith("cell."):
                continue
            xs, ys = [p[0] for p in w], [p[1] for p in w]
            x0, x1, y0 = min(xs), max(xs), min(ys)
            n = max(1, round((x1 - x0) / 240)) if x1 - x0 > 400 else (2 if x1 - x0 > 140 else 1)
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
        global SLICE
        SLICE = None
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
        global SLICE
        w, h = K.w, K.h
        SLICE = (w, h, (0, 6, 0, 6), "x")
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
         "lamp.alert.green", "lamp.alert.yellow", "lamp.alert.red", "crest", "bar.cell.off", "bar.cell.on", "bar.cell.warn",
         # the battle console's own pieces (version 2, 2026-11-08): the column, its ground, the title lintel, the cells
         "console.column", "console.backing", "plate.title", "cell.idle", "cell.lit", "cell.warn", "cell.accord", "cell.locked"]
PIECES = {k: PIECES[k] for k in ORDER}

# A weak warm room light from the upper left, enough to show the corrosion; link_layers adds the softbox, the ceiling and
# the main light. The world is dim: these consoles sit in the dark of an old ship.
LIGHTS = [("Room", (0.3, -1.0, -1.6), 0.55, (1.0, 0.9, 0.76), 10.0)]


def render_setup(w_px, h_px, samples):
    ui_render.setup(w_px, h_px, samples, LIGHTS, world=(0.025, 0.02, 0.016),
                    bounces=dict(max_bounces=8, glossy_bounces=4, transmission_bounces=4, diffuse_bounces=3))


if __name__ == "__main__":
    ui_render.run("tarsine", PIECES, Panel, render_setup, samples=48)
