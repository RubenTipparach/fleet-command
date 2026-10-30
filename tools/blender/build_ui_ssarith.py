"""Fleet Command: the Ssarith Hunt console skin, modelled, lit and rendered in Blender.

Owns every piece of the Ssarith Hunt skin of the shared console UI kit: the console of a reptilian race that hunts for
sport (owner, 2026-10-30: "A gurilla style faction, gun metal, beveled corner, industrial retro CRT style buttons",
then "that faction is from the gorn, which are more hunter like faction, they enjoy the sport, so their ui is
reminiscent of a dense jungle akin to the Predator"). A hunter's kit carried through a dense jungle: worn gunmetal with
an oiled sheen and 45 degree chamfered corners, painted in mottled canopy camouflage where it is not bare, held by hex
bolts sunk in counterbores, grown over by vines and frond leaves; tally marks of the hunt scratched in bone white;
tracking reticles; every key a small CRT, a curved phosphor screen with scanlines set into a thick chamfered bezel,
dark at rest and lit green when the quarry is in sight. Lit by a dim light falling through the canopy, the screens
spilling green onto the steel. Rendered face on in Cycles through the shared harness (tools/blender/ui_render.py), from
the shared shapes and node helpers (tools/blender/ui_shapes.py) and the Kethran generator's corner cutter (chamfer,
rect), at twice the slot size, with a manifest of sizes and margins.

    /opt/blender/blender --background --factory-startup --python tools/blender/build_ui_ssarith.py -- \
        --out assets/ui/ssarith [--only button.idle,crest] [--samples 48]

Units: 1 px of the 1x slot is 1 mm; builders take slot pixels with y running down. 9-slice pieces keep their bolts,
vines and leaves inside the margins and carry no noise on what stretches; their screens carry only horizontal
scanlines, which survive the horizontal stretch every "x" piece gets. Our own design throughout: no other game's or
film's names, shapes, glyphs or art. Ssarith Hunt design language: openspec/changes/console-skins/design.md.
"""

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import ui_render  # noqa: E402  (the render harness every Blender skin shares)
from build_ui_kethran import chamfer, rect  # noqa: E402  (cut corners, the one implementation)
from ui_shapes import NodeKit, Shaper, circle, rrect, srgb, taper  # noqa: E402  (shared modelling and shading)

# ----------------------------------------------------------------------------- materials
# The hunter's gear, the canopy that hides it, the tracker's green, the trophies' bone, the kill's red.
PAL = dict(gun="#3a4044", gun_dark="#1c2022", canopy="#2f4a2a", moss="#5b6340", shade="#1b2a18", phos="#6dff8a",
           bone="#d8cfb4", kill="#c2412b", amber="#ffb340", vine="#3b3a24", leaf="#2c4a24", glass="#020804")


def gunmetal(name, worn):
    """Gunmetal with an oiled sheen; worn (hero pieces only) adds patchy roughness and fine scratches."""
    k = NodeKit(name)
    k.set(Base_Color=PAL["gun"], Metallic=0.85, Roughness=0.36, Coat_Weight=0.25, Coat_Roughness=0.12)
    if worn:
        patch = k.tex("ShaderNodeTexNoise", 30.0, **{"Detail": 4.0})
        k.set(Roughness=k.math("ADD", k.math("MULTIPLY", patch.outputs["Fac"], 0.35), 0.18))
        scratch = k.tex("ShaderNodeTexWave", 260.0, **{"Distortion": 6.0, "Detail": 3.0})
        k.bump(scratch.outputs["Fac"], 0.08)
    return k.mat


def camo(name):
    """Canopy camouflage: hard-edged blotches of shade, canopy green and moss over warped coordinates, matte paint."""
    k = NodeKit(name)
    n = k.tex("ShaderNodeTexNoise", 45.0, k.warped(20.0, 0.02), **{"Detail": 2.0, "Roughness": 0.4})
    r = k.N.new("ShaderNodeValToRGB")
    r.color_ramp.interpolation = "CONSTANT"
    stops = ((0.0, PAL["shade"]), (0.42, PAL["canopy"]), (0.55, PAL["moss"]), (0.66, PAL["canopy"]))
    r.color_ramp.elements[0].position, r.color_ramp.elements[0].color = stops[0][0], list(srgb(stops[0][1])) + [1.0]
    r.color_ramp.elements[1].position, r.color_ramp.elements[1].color = stops[1][0], list(srgb(stops[1][1])) + [1.0]
    for pos, col in stops[2:]:
        e = r.color_ramp.elements.new(pos)
        e.color = list(srgb(col)) + [1.0]
    k.L.new(n.outputs["Fac"], r.inputs[0])
    k.set(Base_Color=r.outputs[0], Roughness=0.74)
    return k.mat


def paint(name, col, rough, coat=0.0):
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=rough, Coat_Weight=coat, Coat_Roughness=0.2)
    return k.mat


def bark(name):
    """A vine's stem: dark woody green-brown, fibrous along its length."""
    k = NodeKit(name)
    fib = k.tex("ShaderNodeTexNoise", 400.0, **{"Detail": 3.0})
    k.set(Base_Color=k.ramp(fib.outputs["Fac"], "#24231a", PAL["vine"], 0.3, 0.7), Roughness=0.8)
    k.bump(fib.outputs["Fac"], 0.25)
    return k.mat


def leaf(name):
    """A frond leaf: waxy dark green, light through it at the thin edges."""
    k = NodeKit(name)
    k.set(Base_Color=PAL["leaf"], Roughness=0.42, Coat_Weight=0.4, Coat_Roughness=0.25, Subsurface_Weight=0.25,
          Subsurface_Radius=(0.6, 1.0, 0.3), Subsurface_Scale=0.002)
    return k.mat


def screen(name, es, lines=True):
    """A phosphor tube's face behind curved glass: green-black, glowing es strong, brightest in the middle where the
    tube faces the eye; lines adds horizontal scanlines 2 mm apart. The page draws live readouts over the dark ones."""
    k = NodeKit(name)
    lw = k.N.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.35
    glow = k.math("MULTIPLY", k.math("SUBTRACT", 1.05, lw.outputs["Facing"]), es)
    if lines:
        sl = k.tex("ShaderNodeTexWave", 500.0, **{"Distortion": 0.0}, wave_type="BANDS", bands_direction="Y")
        glow = k.math("MULTIPLY", glow, k.math("ADD", k.math("MULTIPLY", sl.outputs["Fac"], 0.6), 0.4))
    k.set(Base_Color=PAL["glass"], Roughness=0.06, Coat_Weight=1.0, Coat_Roughness=0.02, Emission_Color=PAL["phos"], Emission_Strength=glow)
    return k.mat


def lens(name, col, es):
    k = NodeKit(name)
    k.set(Base_Color=col, Roughness=0.15, Coat_Weight=1.0, Coat_Roughness=0.03, Emission_Color=col, Emission_Strength=es)
    return k.mat


def M(K, key):
    makers = {
        "gun": lambda: gunmetal("gun", False), "gun_worn": lambda: gunmetal("gun_worn", True),
        "dark": lambda: paint("dark", PAL["gun_dark"], 0.5), "canopy": lambda: paint("canopy", PAL["canopy"], 0.74),
        "camo": lambda: camo("camo"), "bone": lambda: paint("bone", PAL["bone"], 0.55, 0.2),
        "vine": lambda: bark("vine"), "leaf": lambda: leaf("leaf"),
        "scr_off": lambda: screen("scr_off", 0.012), "scr_idle": lambda: screen("scr_idle", 0.07),
        "scr_hover": lambda: screen("scr_hover", 0.32), "scr_press": lambda: screen("scr_press", 0.7),
        "scr_flat": lambda: screen("scr_flat", 0.018, lines=False), "well": lambda: screen("well", 0.006, lines=False),
        "trace": lambda: lens("trace", PAL["phos"], 2.2), "cell": lambda: lens("cell", PAL["phos"], 1.2),
        "cell_red": lambda: lens("cell_red", PAL["kill"], 1.4),
        "lamp_green": lambda: lens("lamp_green", PAL["phos"], 2.4), "lamp_amber": lambda: lens("lamp_amber", PAL["amber"], 2.4),
        "lamp_red": lambda: lens("lamp_red", PAL["kill"], 2.6),
    }
    return K.mat(key, makers[key])


# ----------------------------------------------------------------------------- fittings

def plate(K, x0, y0, x1, y1, c, z0, z1, key="gun", holes=(), bevel=1.2):
    """A plate with every corner cut back c px at 45 degrees, its edges bevelled."""
    return K.slab([chamfer(rect(x0, y0, x1, y1), c)] + list(holes), z0, z1, M(K, key), bevel=bevel)


def bolt(K, x, y, z=6.0, r=2.6):
    """A hex bolt sunk in a counterbore: a dark ring, the six-sided head, a hex socket in it."""
    K.cyl(x, y, r + 1.2, z - 1.4, z - 0.2, M(K, "dark"), seg=24)
    K.cyl(x, y, r, z - 1.2, z + 0.4, M(K, "gun"), seg=6, smooth=False)
    K.cyl(x, y, r * 0.42, z + 0.3, z + 0.6, M(K, "dark"), seg=6, smooth=False)


def crt(K, x0, y0, x1, y1, r, z, key, lines=True):
    """A curved tube face: a dark rim, then the glass domed up at its edges."""
    K.slab([rrect(x0 - 1.2, y0 - 1.2, x1 + 1.2, y1 + 1.2, r + 1)], z - 2.0, z, M(K, "dark"), bevel=0.4)
    K.slab([rrect(x0, y0, x1, y1, r)], z - 1.0, z + 1.6, M(K, key), bevel=min(1.5, (y1 - y0) / 4))


def crt_key(K, x0, y0, x1, y1, state, z=0.0):
    """The Ssarith key: a thick chamfered gunmetal bezel holding a small CRT; dark at rest, lit on hover, flaring and
    sunk 1.5 mm when pressed."""
    sunk = 1.5 if state == "pressed" else 0.0
    plate(K, x0, y0, x1, y1, 7, z, z + 6.0, holes=[chamfer(rect(x0 + 6, y0 + 6, x1 - 6, y1 - 6), 4)], bevel=1.4)
    plate(K, x0 + 5, y0 + 5, x1 - 5, y1 - 5, 4, z, z + 3.0 - sunk, key="dark", bevel=0.5)
    crt(K, x0 + 8, y0 + 8, x1 - 8, y1 - 8, 5, z + 3.0 - sunk, {"idle": "scr_idle", "hover": "scr_hover", "pressed": "scr_press"}[state])


def leaf_shape(cx, cy, length, width, angle, n=14):
    """A frond leaf's outline from its stalk at (cx, cy), pointing along angle (radians, y down)."""
    ca, sa = math.cos(angle), math.sin(angle)
    side = [(t, width * math.sin(math.pi * t) ** 0.7 * (1 - 0.25 * t)) for t in (i / n for i in range(n + 1))]
    pts = [(t * length, h) for t, h in side] + [(t * length, -h) for t, h in reversed(side[1:-1])]
    return [(cx + x * ca - y * sa, cy + x * sa + y * ca) for x, y in pts]


def frond(K, cx, cy, length, width, angle, z):
    """A leaf lying over the hardware, its midrib raised."""
    K.slab([leaf_shape(cx, cy, length, width, angle)], z, z + 0.8, M(K, "leaf"), bevel=0.3)
    tip = (cx + math.cos(angle) * length * 0.92, cy + math.sin(angle) * length * 0.92)
    K.tube([(cx, cy), tip], [0.45, 0.2], M(K, "vine"), flat=0.6, z=[z + 1.0, z + 1.0], sharp=True)


def vine(K, pts, r0, r1, z):
    """A vine stem through pts, thick at its root and thin at its tip, lying at height z (a list or one value)."""
    zs = z if isinstance(z, (list, tuple)) else [z] * len(pts)
    K.tube(pts, taper(len(pts), r0, r1), M(K, "vine"), flat=0.8, z=zs)


def tally(K, x, y, groups, z, size=10.0):
    """Tally marks scratched in bone white: groups of four strokes struck through by a fifth."""
    for g in range(groups):
        gx = x + g * size * 1.1
        for i in range(4):
            sx = gx + i * size * 0.18
            K.tube([(sx, y - size / 2), (sx + 0.6, y + size / 2)], [0.55, 0.55], M(K, "bone"), flat=0.5, z=[z, z], sharp=True)
        K.tube([(gx - 1.5, y + size * 0.3), (gx + size * 0.7, y - size * 0.3)], [0.55, 0.55], M(K, "bone"), flat=0.5, z=[z + 0.2, z + 0.2], sharp=True)


def reticle(K, cx, cy, r, z, key="bone", w=0.9):
    """A tracking reticle: four chevrons closing on a point, open at the middle."""
    for a in (0.25, 0.75, 1.25, 1.75):
        ang = a * math.pi
        tip = (cx + math.cos(ang) * r * 0.45, cy + math.sin(ang) * r * 0.45)
        left = (cx + math.cos(ang - 0.35) * r, cy + math.sin(ang - 0.35) * r)
        right = (cx + math.cos(ang + 0.35) * r, cy + math.sin(ang + 0.35) * r)
        K.tube([left, tip, right], [w] * 3, M(K, key), flat=0.5, z=[z] * 3, sharp=True)


def vents(K, x0, x1, y, n, z, h=8.0):
    """A row of n vent slots, sunk dark between x0 and x1, centred on y."""
    step = (x1 - x0) / n
    for i in range(n):
        cx = x0 + step * (i + 0.5)
        K.slab([rrect(cx - step * 0.28, y - h / 2, cx + step * 0.28, y + h / 2, 1.0)], z - 1.2, z + 0.1, M(K, "dark"), bevel=0.2)


def octagon(cx, cy, r):
    return [(cx + math.cos(math.pi / 8 + i * math.pi / 4) * r, cy + math.sin(math.pi / 8 + i * math.pi / 4) * r) for i in range(9)]


# ----------------------------------------------------------------------------- the pieces

def frame_panel(K):
    w, h = K.w, K.h
    plate(K, 0, 0, w, h, 14, 0.0, 6.0, holes=[chamfer(rect(20, 20, w - 20, h - 20), 6)], bevel=1.6)
    plate(K, 14, 14, w - 14, h - 14, 8, 0.0, 3.5, key="canopy", holes=[chamfer(rect(20, 20, w - 20, h - 20), 6)], bevel=0.8)
    plate(K, 18, 18, w - 18, h - 18, 7, -1.0, 1.0, key="well", bevel=0.3)
    for x, y in ((12, 12), (w - 12, h - 12)):
        bolt(K, x, y)
    # the jungle has the other two corners: a vine curling over the top right, a frond across the bottom left
    vine(K, [(w - 27, 2), (w - 16, 6), (w - 9, 15), (w - 6, 26)], 1.6, 0.7, [7.5, 8.2, 8.0, 7.2])
    frond(K, w - 14, 7, 15, 3.6, 0.9, 7.6)
    frond(K, 6, h - 22, 19, 4.2, 0.55, 6.6)
    frond(K, 4, h - 10, 16, 3.4, -0.15, 6.9)


def frame_inset(K):
    w, h = K.w, K.h
    plate(K, 0, 0, w, h, 6, 0.0, 4.0, holes=[chamfer(rect(6, 6, w - 6, h - 6), 3)], bevel=1.0)
    plate(K, 5, 5, w - 5, h - 5, 3, -1.0, 1.5, key="well", bevel=0.3)


def bar_track(K):
    """One tile of the bar channel: a dark slot between gunmetal lips (the tile repeats)."""
    w, h = K.w, K.h
    K.slab([[(-2, -1), (w + 2, -1), (w + 2, h + 1), (-2, h + 1)]], 0.0, 2.0, M(K, "dark"), bevel=0.0)
    for y0, y1 in ((-1.0, 2.5), (h - 2.5, h + 1.0)):
        K.slab([[(-2, y0), (w + 2, y0), (w + 2, y1), (-2, y1)]], 1.0, 4.0, M(K, "gun"), bevel=0.0)


def bar_fill(key):
    """One phosphor cell of a segmented bar, with the gap to the next one."""
    def build(K):
        w, h = K.w, K.h
        K.slab([[(0.5, 1.0), (w - 2.0, 1.0), (w - 2.0, h - 1.0), (0.5, h - 1.0)]], 0.0, 1.6, M(K, key), bevel=0.0)
    return build


def button(state):
    def build(K):
        crt_key(K, 0.5, 1.0, K.w - 0.5, K.h - 1.0, state)
    return build


def tab(active):
    def build(K):
        w, h = K.w, K.h
        plate(K, 1, 3, w - 1, h - 3, 9, 0.0, 4.0 if active else 3.0, key="gun" if active else "dark", bevel=1.0)
        crt(K, w - 30, 11, w - 12, h - 11, 3, 4.0, "scr_hover" if active else "scr_off")
        for i in range(3):
            K.tube([(9 + i * 3.2, 14), (9.6 + i * 3.2, h - 14)], [0.55, 0.55], M(K, "bone"), flat=0.5, z=[4.6, 4.6], sharp=True)
    return build


def strip(bottom):
    def build(K):
        w, h = K.w, K.h
        plate(K, 0, 0, w, h, 12, 0.0, 6.0, holes=[rrect(12, 11, 78, 45, 4)], bevel=1.6)
        crt(K, 13, 12, 77, 44, 4, 3.0, "scr_idle")
        for y in (10, h - 10):
            K.slab([[(90, y - 1.0), (w - 90, y - 1.0), (w - 90, y + 1.0), (90, y + 1.0)]], 5.2, 6.4, M(K, "dark"), bevel=0.2)
        tally(K, w - 82, h / 2, 2, 6.6, size=12.0)
        bolt(K, w - 17, 13, r=2.4)
        vine(K, [(w - 30, h + 1), (w - 22, h - 9), (w - 10, h - 14), (w - 2, h - 26)], 1.5, 0.6, [7.0, 7.8, 7.6, 7.0])
        frond(K, w - 20, h - 10, 14, 3.4, -1.9 if bottom else -2.2, 7.8)
    return build


def divider(K):
    w, h = K.w, K.h
    plate(K, 0, 1, w, h - 1, 3, 0.0, 3.0, bevel=0.8)
    K.slab([[(10, 4.4), (w - 10, 4.4), (w - 10, 5.6), (10, 5.6)]], 2.4, 3.2, M(K, "dark"), bevel=0.0)
    for x in (5, w - 5):
        bolt(K, x, h / 2, z=3.0, r=1.8)


def nameplate(K):
    w, h = K.w, K.h
    plate(K, 0, 0, w, h, 10, 0.0, 6.0, key="gun_worn", holes=[chamfer(rect(48, 9, w - 70, h - 9), 4)], bevel=1.6)
    crt(K, 49, 10, w - 71, h - 10, 4, 3.0, "scr_idle")
    plate(K, 8, 9, 40, h - 9, 4, 3.0, 7.0, key="camo", bevel=0.8)
    reticle(K, 24, h / 2, 12, 7.4)
    tally(K, w - 62, h / 2, 2, 6.6)
    bolt(K, w - 14, 14, r=2.2)
    vine(K, [(w - 40, -1), (w - 26, 8), (w - 18, 24), (w - 8, 34), (w + 1, 40)], 2.0, 0.8, [7.0, 7.8, 8.2, 7.8, 7.0])
    frond(K, w - 20, 22, 18, 4.0, 1.2, 8.4)
    frond(K, w - 30, 6, 15, 3.4, 2.6, 8.0)


def helm_pad(K):
    w, h = K.w, K.h
    c = w / 2
    plate(K, 0.5, 0.5, w - 0.5, h - 0.5, 60, 0.0, 6.0, key="gun_worn", bevel=2.0)
    plate(K, 18, 18, w - 18, h - 18, 50, 4.0, 7.0, key="camo", bevel=1.0)
    for i in range(8):
        a = -math.pi / 2 + i * math.pi / 4
        x, y = c + math.cos(a) * 94, c + math.sin(a) * 94
        crt_key(K, x - 21, y - 21, x + 21, y + 21, "idle", z=6.0)
    crt_key(K, c - 34, c - 34, c + 34, c + 34, "hover", z=6.0)
    reticle(K, c, c, 30, 11.2, key="trace", w=1.0)
    for x, y in ((40, 40), (w - 40, h - 40)):
        bolt(K, x, y, z=7.0, r=3.4)
    # roots over the lower left cut, leaves at the upper right
    vine(K, [(-2, h - 70), (16, h - 58), (34, h - 48), (52, h - 30), (64, h + 2)], 3.0, 1.0, [9.0, 10.5, 11.0, 10.0, 9.0])
    vine(K, [(4, h - 42), (22, h - 30), (34, h - 12)], 1.8, 0.7, [9.5, 10.2, 9.6])
    for (x, y, a, L) in ((w - 52, 18, 0.2, 30), (w - 40, 30, 0.9, 34), (w - 30, 48, 1.4, 26)):
        frond(K, x, y, L, 7.0, a, 12.0)
    vine(K, [(w - 70, 4), (w - 52, 18), (w - 38, 32), (w - 24, 58)], 2.4, 0.8, [11.0, 12.0, 12.2, 11.4])


def ssd_bezel(K):
    w, h = K.w, K.h
    plate(K, 0.5, 0.5, w - 0.5, h - 0.5, 22, 0.0, 9.0, key="gun_worn", holes=[rrect(26, 22, w - 26, h - 22, 16)], bevel=2.2)
    crt(K, 27, 23, w - 27, h - 23, 15, 4.0, "scr_flat")
    for x0 in (4, w - 18):
        vents(K, x0, x0 + 14, h / 2, 1, 9.0, h=90.0)
    for x, y in ((16, 14), (w - 16, h - 14)):
        bolt(K, x, y, z=9.0, r=3.0)
    tally(K, 40, h - 11, 3, 9.5, size=9.0)
    vine(K, [(w - 120, -2), (w - 80, 8), (w - 46, 6), (w - 18, 18), (w - 6, 50), (w + 2, 80)], 3.0, 1.0, [10.0, 11.4, 12.0, 11.6, 11.0, 10.2])
    vine(K, [(w - 60, 6), (w - 40, 22), (w - 34, 32)], 1.6, 0.6, [12.0, 12.6, 12.2])
    for (x, y, a, L) in ((w - 84, 8, 2.2, 30), (w - 50, 6, 0.4, 34), (w - 20, 22, 1.2, 32), (w - 8, 56, 1.9, 26)):
        frond(K, x, y, L, 7.5, a, 12.6)
    vine(K, [(-2, h - 60), (10, h - 40), (14, h - 18), (30, h + 2)], 2.4, 0.9, [10.0, 11.0, 11.0, 10.0])
    frond(K, 10, h - 40, 26, 6.5, -0.4, 11.6)


def dial(K):
    w, h = K.w, K.h
    c = w / 2
    plate(K, 1, 1, w - 1, h - 1, 26, 0.0, 6.0, key="gun_worn", holes=[circle(c, c, 47, 64)], bevel=1.6)
    K.slab([circle(c, c, 48.5, 64)], 1.0, 3.0, M(K, "dark"), bevel=0.4)
    K.slab([circle(c, c, 46, 64)], 1.6, 4.6, M(K, "scr_flat"), bevel=2.0)
    for i in range(12):
        a = i * math.pi / 6
        r0 = 49.5 if i % 3 else 48.5
        K.tube([(c + math.cos(a) * r0, c + math.sin(a) * r0), (c + math.cos(a) * 54, c + math.sin(a) * 54)], [0.6, 0.6], M(K, "bone"), flat=0.5, z=[6.2, 6.2], sharp=True)
    bolt(K, c + 40, c + 40, r=2.2)
    vine(K, [(-1, 30), (10, 18), (20, 8), (34, -1)], 1.8, 0.7, [7.0, 7.8, 7.8, 7.0])
    frond(K, 12, 16, 16, 4.0, -0.9, 8.2)


def lamp(key):
    """A caged lamp: a chamfered housing, a glowing lens, three guard bars across it, a leaf caught in the cage."""
    def build(K):
        w = K.w
        c = w / 2
        plate(K, 2, 2, w - 2, w - 2, 12, 0.0, 6.0, key="gun_worn", holes=[circle(c, c, 18, 40)], bevel=1.4)
        K.slab([circle(c, c, 19.5, 40)], 1.0, 3.0, M(K, "dark"), bevel=0.4)
        K.blob(c, c, 17, 17, 6, M(K, key), z=3.0, seg=48)
        for dx in (-8, 0, 8):
            K.tube([(c + dx, c - 22), (c + dx, c + 22)], [1.4, 1.4], M(K, "gun"), flat=1.0, z=[9.0, 9.0], sharp=True)
        frond(K, w - 8, 6, 14, 3.4, 2.4, 10.6)
    return build


def crest(K):
    """The Hunt's mark: a trophy fang hung inside a tracking reticle, on a camouflaged cut-cornered plate."""
    w = K.w
    c = w / 2
    plate(K, 2, 2, w - 2, w - 2, 22, 0.0, 4.0, key="gun_worn", bevel=1.4)
    plate(K, 10, 10, w - 10, w - 10, 17, 3.0, 6.0, key="camo", bevel=0.8)
    reticle(K, c, c, 34, 6.6, w=1.4)
    fang = [(c - 7, 30), (c + 7, 30), (c + 4, 52), (c, 68), (c - 4, 52)]
    K.slab([fang], 5.5, 8.5, M(K, "bone"), bevel=0.8)
    K.tube([(c - 9, 28), (c + 9, 28)], [1.4, 1.4], M(K, "vine"), flat=0.8, z=[8.0, 8.0], sharp=True)


# Icons: glyphs glowing on a tiny tube, drawn from the hunt: straight strokes and 45 degree turns.
GLYPHS = {
    "helm": [[(16, 36), (24, 12), (32, 36)], [(19, 28), (29, 28)], [(24, 40), (24, 42)]],          # a track mark, an arrow up the trail
    "fire": [[(10, 16), (10, 10), (16, 10)], [(32, 10), (38, 10), (38, 16)], [(38, 32), (38, 38), (32, 38)], [(16, 38), (10, 38), (10, 32)], octagon(24, 24, 2.0)],
    "shields": [[(24, 8), (36, 16), (36, 28), (24, 40), (12, 28), (12, 16), (24, 8)], [(12, 22), (36, 22)], [(18, 31), (30, 31)]],  # a scale plate
    "tractor": [[(10, 12), (24, 12)], octagon(24, 26, 9), [(24, 12), (24, 17)]],                   # a snare loop
    "transport": [[(24, 8), (24, 30)], [(17, 23), (24, 30), (31, 23)], [(12, 34), (12, 40), (36, 40), (36, 34)]],
    "science": [[(8, 26), (14, 26), (17, 18), (21, 34), (25, 12), (29, 30), (32, 22), (40, 22)]],  # the scent of the quarry
    "comms": [[(10, 24), (16, 18), (22, 30), (28, 14), (34, 30), (38, 24)], [(24, 38), (24, 42)]],  # the call
    "repair": [[(12, 38), (30, 20)], [(30, 20), (38, 10), (34, 22), (30, 20)], [(10, 34), (16, 40)]],  # a skinning blade
    "map": [[(10, 38), (16, 30), (14, 22), (22, 16), (30, 18), (34, 10)], [(30, 6), (38, 14)], [(38, 6), (30, 14)]],  # a trail to an X
    "settings": [[(12, 12), (13, 36)], [(18, 12), (19, 36)], [(24, 12), (25, 36)], [(30, 12), (31, 36)], [(8, 30), (36, 18)]],  # the tally
}


def icon(name):
    def build(K):
        plate(K, 1, 1, 47, 47, 8, 0.0, 3.5, bevel=1.0, holes=[rrect(5, 5, 43, 43, 5)])
        crt(K, 6, 6, 42, 42, 5, 1.8, "scr_off")
        for line in GLYPHS[name]:
            K.tube(line, [1.1] * len(line), M(K, "trace"), flat=0.5, z=[3.6] * len(line), sharp=True)
    return build


ICONS = list(GLYPHS)
PIECES = {
    "frame.panel":    ("nine", (180, 140), (28, 28, 28, 28), "xy", frame_panel),
    "frame.inset":    ("nine", (72, 56), (12, 12, 12, 12), "xy", frame_inset),
    "bar.track":      ("nine", (8, 16), (0, 0, 0, 0), "x", bar_track),
    "bar.fill":       ("nine", (8, 10), (0, 0, 0, 0), "x", bar_fill("cell")),
    "bar.fill.red":   ("nine", (8, 10), (0, 0, 0, 0), "x", bar_fill("cell_red")),
    "button.idle":    ("nine", (150, 48), (16, 18, 16, 18), "x", button("idle")),
    "button.hover":   ("nine", (150, 48), (16, 18, 16, 18), "x", button("hover")),
    "button.pressed": ("nine", (150, 48), (16, 18, 16, 18), "x", button("pressed")),
    "tab.idle":       ("nine", (120, 40), (14, 36, 14, 22), "x", tab(False)),
    "tab.active":     ("nine", (120, 40), (14, 36, 14, 22), "x", tab(True)),
    "strip.top":      ("nine", (512, 56), (10, 92, 10, 88), "x", strip(False)),
    "strip.bottom":   ("nine", (512, 56), (10, 92, 10, 88), "x", strip(True)),
    "divider":        ("nine", (128, 10), (0, 12, 0, 12), "x", divider),
    "nameplate":      ("hero", (460, 56), None, None, nameplate),
    "helm.pad":       ("hero", (300, 300), None, None, helm_pad),
    "ssd.bezel":      ("hero", (460, 300), None, None, ssd_bezel),
    "dial":           ("hero", (120, 120), None, None, dial),
    "lamp.alert.green": ("hero", (64, 64), None, None, lamp("lamp_green")),
    "lamp.alert.yellow": ("hero", (64, 64), None, None, lamp("lamp_amber")),
    "lamp.alert.red": ("hero", (64, 64), None, None, lamp("lamp_red")),
    "crest":          ("hero", (96, 96), None, None, crest),
}
PIECES.update({f"icon.{n}": ("hero", (48, 48), None, None, icon(n)) for n in ICONS})

# Light falling through the canopy: a warm-green key from above, a weak cool fill; the room is near black, so the
# screens light the steel around them.
LIGHTS = [("Canopy", (0.25, -1.0, -1.5), 3.0, (0.92, 1.0, 0.82), 9.0), ("Fill", (-0.7, 1.0, -0.8), 0.6, (0.75, 0.85, 1.0), 16.0)]


def render_setup(w_px, h_px, samples):
    ui_render.setup(w_px, h_px, samples, LIGHTS, world=(0.05, 0.06, 0.05), bounces=dict(max_bounces=6, glossy_bounces=3, diffuse_bounces=2))


if __name__ == "__main__":
    ui_render.run("ssarith", PIECES, Shaper, render_setup, samples=48)
