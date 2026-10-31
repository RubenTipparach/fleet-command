"""Fleet Command: the Vhorr Brood kit's shapes, fit-checked, as the one source for its render and its page module.

Owns the geometry of every piece of the Brood skin (mockup 4, console-skins), rebuilt as assimilated tech (design.md,
"The Vhorr Brood: assimilated tech"; owner, 2026-10-31: "I like that some of the tech in zerg is human tech with an
organic layer on top"). Two layers, as the design says:

1. A captured machine on the analog panel bible: steel plates whose edges roll over, bolts, screens and real keycaps,
   fully legible. Its plates, wells and live areas are checked by tools/mockups/formed_metal.py's rules (K bolts on
   flat metal, L tips that hold the roll, R rolled edges that never meet, P live readouts inside their windows, B films
   inside the plate).
2. The brood's growth over it: creep (membrane and hide) spreading from the corners and seams and fogging the bezels,
   bolts swallowed into glowing pustules, chitin ridges and horns growing out of the metal past the plate's rounded
   corners (breaking the rectangle), conduits wrapped in sinew, a fleshy portrait pocket on the name plate, a glowing
   egg as the crest.

The growth has its own rule, G, measured like the others:
  G1  every growth stays inside the piece's image (so an overhang never reaches another panel's content);
  G2  no growth comes within CLEAR px of a live readout, a label or a key's face;
  G3  on a 9-slice piece, growth lies only in the cells that never stretch (the four corners of an xy piece, the end
      columns of an x piece), so the slicing never draws it out.
Creep is grown from a seed at its anchor and shrunk toward the anchor until it fits, never clipped (a clip leaves a
straight cut edge), so G holds by construction and the check proves it.

tools/blender/build_ui_vhorr.py renders exactly these outlines; docs/mockups/skins/vhorr.js places its live readouts
inside the measured windows recorded here.

    python3 tools/mockups/vhorr_shapes.py              # check, then write assets/ui/vhorr/shapes.json
    python3 tools/mockups/vhorr_shapes.py --verify assets/ui/vhorr   # rule B on the renders

Units: slot px = mm, y down, at the 1920 x 1080 reference (renders are at 2x). Needs shapely.
"""
import json
import math
import os
import random
import sys

from shapely import affinity
from shapely.geometry import LineString, Point, Polygon, box
from shapely.ops import unary_union

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from formed_metal import INSET, behind_layer, disc, fit, rbox, soft  # noqa: E402

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "assets", "ui", "vhorr")

ROLL = 3.0    # the captured machine's steel: a firm machined round
SOFT = 6.5    # every corner at least twice the roll round, or the offset turns it inside out
CLEAR = 3.0   # rule G2: growth keeps this far from any live readout, label or key face
# Blender's curve bevel grows a slab by its radius, so the outlines written for the creep's membrane, its core and the
# pocket's lip are drawn in by their slab's bevel here (shapely offsets cleanly; a mitred offset spikes at tight bends).
FOG_BEVEL, CORE_BEVEL, POCKET_BEVEL = 0.7, 2.3, 1.6


def chamfered(x0, y0, x1, y1, c):
    """A machined well with its corners cut at 45 degrees by c px: the corner land it leaves carries a bolt."""
    return Polygon([(x0 + c, y0), (x1 - c, y0), (x1, y0 + c), (x1, y1 - c), (x1 - c, y1), (x0 + c, y1), (x0, y1 - c), (x0, y0 + c)])


def lump(cx, cy, rx, ry, seed, amp=0.22, rot=0.0, n=56):
    """An organic outline: an ellipse whose radius wanders by three seeded harmonics (no corners, nothing cut)."""
    R = random.Random(seed)
    h = [(k, R.uniform(0, 2 * math.pi), R.uniform(0.4, 1.0) * amp / k ** 0.6) for k in (2, 3, 5)]
    pts = []
    for i in range(n):
        a = 2 * math.pi * i / n
        f = 1 + sum(w * math.sin(k * a + ph) for k, ph, w in h)
        pts.append((rx * f * math.cos(a), ry * f * math.sin(a)))
    return affinity.translate(affinity.rotate(Polygon(pts), rot, origin=(0, 0), use_radians=True), cx, cy).buffer(0)


def grown(anchor, rx, ry, seed, zone, amp=0.22, rot=0.0, toward=None):
    """Creep grown from anchor and fitted inside zone (rule G) without clipping: it may slide from anchor toward the
    point toward (default: the zone's centre) and shrink about its centre; the placement that keeps it largest wins,
    with a small preference for staying near its anchor (creep spreads from corners and seams)."""
    ax, ay = anchor
    tx, ty = toward if toward else (zone.centroid.x, zone.centroid.y)
    shape = lump(0, 0, rx, ry, seed, amp, rot)
    best = None
    for i in range(13):
        t = i / 12
        cx, cy = ax + (tx - ax) * t, ay + (ty - ay) * t
        lo, hi = 0.0, 1.0
        for _ in range(14):   # the largest scale that fits here
            s = (lo + hi) / 2
            if zone.contains(affinity.translate(affinity.scale(shape, s, s, origin=(0, 0)), cx, cy)):
                lo = s
            else:
                hi = s
        score = lo * lo * (1 - 0.35 * t)
        if lo > 0.05 and (best is None or score > best[0]):
            best = (score, affinity.translate(affinity.scale(shape, lo, lo, origin=(0, 0)), cx, cy))
    if best is None:
        raise SystemExit(f"creep at {anchor} cannot fit its zone")
    g = best[1]
    return soft(g, 1.5) if zone.contains(soft(g, 1.5)) else g


def ridge(pts, radii):
    """A chitin ridge or horn: a tube through pts with a radius per point (its geometry for rule G is the swept tube)."""
    return dict(pts=[list(p) for p in pts], radii=list(radii))


def ridge_geom(r):
    line = LineString(r["pts"])
    return unary_union([Point(p).buffer(rad) for p, rad in zip(r["pts"], r["radii"])] +
                       [LineString([a, b]).buffer(max(ra, rb)) for a, b, ra, rb in zip(r["pts"], r["pts"][1:], r["radii"], r["radii"][1:])]) if line.length else Point(r["pts"][0]).buffer(r["radii"][0])


def conduit(pts, r, wraps):
    """A captured conduit pipe of radius r along pts, wrapped in sinew bands at the fractions wraps along it."""
    line = LineString(pts)
    bands = []
    for f in wraps:
        p = line.interpolate(f, normalized=True)
        q = line.interpolate(min(1.0, f + 0.01), normalized=True)
        dx, dy = q.x - p.x, q.y - p.y
        d = math.hypot(dx, dy) or 1.0
        nx, ny = -dy / d, dx / d
        bands.append([[round(p.x + nx * (r + 0.6), 3), round(p.y + ny * (r + 0.6), 3)], [round(p.x + dx / d * 1.2, 3), round(p.y + dy / d * 1.2, 3)],
                      [round(p.x - nx * (r + 0.6), 3), round(p.y - ny * (r + 0.6), 3)]])
    return dict(pts=[list(p) for p in pts], r=r, wraps=bands)


def pieces():
    """Every piece: kind, size, 9-slice margins (top, right, bottom, left), stretch, the cells' groove inset (bar track),
    roll and land, the plate, its wells (each with the film behind it), its bolts, the live readouts the page draws (for
    rules P and G), and the growth: creep outlines, pustules (x, y, r), ridges, conduits and the portrait pocket."""
    P = {}

    def add(name, kind, size, plate, wells, bolts=(), live=(), margins=None, stretch=None, roll=ROLL, land=3.0, inset=None,
            dz=-7.0, pad=3.0, creep=(), pustules=(), ridges=(), conduits=(), pocket=None, prints=(), press=0.0):
        P[name] = dict(kind=kind, size=size, margins=margins, stretch=stretch, roll=roll, land=land, inset=inset, plate=plate,
                       wells=list(wells), bolts=list(bolts), live=list(live), dz=dz, pad=pad, creep=list(creep),
                       pustules=list(pustules), ridges=list(ridges), conduits=list(conduits), pocket=pocket, prints=list(prints),
                       press=press)

    def zone(name_size, margins, stretch, live, corner=None):
        """Where growth may lie (rule G): inside the image, CLEAR from the live areas, and on a 9-slice piece only in
        its unstretched cells (one corner cell when corner names it: 'tl', 'tr', 'bl', 'br', 'l', 'r')."""
        w, h = name_size
        z = box(0.5, 0.5, w - 0.5, h - 0.5)
        if margins:
            t, r, b, l = margins
            cells = {"tl": box(0, 0, l, t), "tr": box(w - r, 0, w, t), "bl": box(0, h - b, l, h), "br": box(w - r, h - b, w, h),
                     "l": box(0, 0, l, h), "r": box(w - r, 0, w, h)}
            if stretch == "x":
                allowed = unary_union([cells["l"], cells["r"]])
            else:
                allowed = unary_union([cells[k] for k in ("tl", "tr", "bl", "br")])
            if corner:
                allowed = allowed.intersection(cells[corner])
            z = z.intersection(allowed.buffer(-0.5))
        elif corner:
            pass
        for _role, g in live:
            z = z.difference(g.buffer(CLEAR + 0.5))
        return z

    def corners4(size, margins, stretch, live, reach, seed, inset=4.0, skip=()):
        """Creep from each corner of the piece, reach px across, grown and shrunk to fit its corner cell."""
        w, h = size
        out = []
        for i, (k, ax, ay) in enumerate((("tl", inset, inset), ("tr", w - inset, inset), ("bl", inset, h - inset), ("br", w - inset, h - inset))):
            if k in skip:
                continue
            z = zone(size, margins, stretch, live, k if margins and stretch == "xy" else None)
            if margins and stretch == "x":
                z = z.intersection(box(0, 0, w / 2, h) if ax < w / 2 else box(w / 2, 0, w, h))
            out.append(grown((ax, ay), reach * (0.9 + 0.25 * ((seed + i) % 3) / 2), reach * (0.75 + 0.2 * ((seed + 2 * i) % 3) / 2),
                             seed * 31 + i, z, rot=(0.6 if k in ("tl", "br") else -0.6)))
        return out

    # Frames: a steel plate round a machined well with chamfered corners; a bolt sits in each corner land. The brood
    # creeps in from every corner over the bolt (now a glowing pustule) and fogs the chamfer; a short chitin horn grows
    # out of the plate's rounded top corners. All inside the 34 px corner cells.
    m = (34, 34, 34, 34)
    add("frame.panel", "nine", (160, 120), soft(rbox(0.5, 0.5, 159.5, 119.5, 14), SOFT), [("dim", soft(chamfered(12, 12, 148, 108, 22), SOFT))],
        bolts=[(17.5, 17.5, 1.9), (142.5, 17.5, 1.9), (17.5, 102.5, 1.9), (142.5, 102.5, 1.9)], margins=m, stretch="xy",
        creep=corners4((160, 120), m, "xy", [], 17, 3), pustules=[(17.5, 17.5, 4.2), (142.5, 17.5, 4.2), (17.5, 102.5, 3.6), (142.5, 102.5, 3.6)],
        ridges=[ridge([(26, 7), (14, 5.5), (5, 2.5)], [3.0, 2.0, 0.5]), ridge([(134, 7), (146, 5.5), (155, 2.5)], [3.0, 2.0, 0.5])])
    m = (14, 14, 14, 14)
    add("frame.inset", "nine", (64, 48), soft(rbox(0.5, 0.5, 63.5, 47.5, 7), SOFT), [("dim", soft(rbox(9.5, 9.5, 54.5, 38.5, 6), SOFT))],
        margins=m, stretch="xy", dz=-4.0, creep=corners4((64, 48), m, "xy", [], 8, 5, inset=3.0, skip=("tr", "bl")))
    # The symbol bar's track: a steel rail with a groove of dark glass the spore eggs sit in (inset 9, 3: 14 px cells).
    add("bar.track", "nine", (96, 20), soft(box(0.5, 0.5, 95.5, 19.5), 1.4), [("slot", soft(box(7, 2.2, 89, 17.8), 1.2))],
        live=[("cells", box(9, 3, 87, 17))], margins=(0, 14, 0, 14), stretch="x", roll=0.6, land=0.4, inset=(9, 3), dz=-2.0, pad=0.4)
    # Keys: a machined keycap (a rounded steel cap round a lit window); membrane has grown over both ends and onto the
    # window's ends, never over the label. Every state is the same steel and the same membrane; only the light behind
    # the window changes, and the cap never moves.
    key_live = [("label", box(28, 17, 112, 35))]
    key_m = (24, 30, 12, 30)
    key_creep = [grown((3, 26), 21, 23, 71, zone((140, 52), key_m, "x", key_live).intersection(box(0, 0, 70, 52)), rot=0.2),
                 grown((137, 22), 20, 22, 72, zone((140, 52), key_m, "x", key_live).intersection(box(70, 0, 140, 52)), rot=-0.3)]
    # Pressed, the key's inner part (its lit glass) is pushed in: 3 mm deeper and 3 px down the screen, the frame and the
    # growth unchanged (owner, 2026-10-31: "I actually prefer the more animated versions on click"); the page's
    # --btn-sink moves the label the same 3 px, so its live box covers both places.
    for state, film, press in (("idle", "low", 0.0), ("hover", "warm", 0.0), ("pressed", "hot", 3.0)):
        live = [("label", box(28, 17, 112, 35 + press))]
        add(f"button.{state}", "nine", (140, 52), soft(rbox(2, 2, 138, 50, 9), SOFT), [(film, soft(rbox(14, 11, 126, 41, 6), 3))],
            live=live, margins=key_m, stretch="x", roll=2.4, dz=-5.0 - press, pad=2.0, creep=key_creep,
            pustules=[(8.5, 13, 2.6), (131, 39, 2.4)], press=press)
    # Tabs: a low steel cap with a slot of light; a little membrane at each end.
    tab_live = [("label", box(18, 14, 102, 27))]
    tab_m = (12, 14, 12, 14)
    tab_creep = [grown((2.5, 30), 10, 9, 81, zone((120, 40), tab_m, "x", tab_live).intersection(box(0, 0, 60, 40))),
                 grown((117.5, 10), 10, 9, 82, zone((120, 40), tab_m, "x", tab_live).intersection(box(60, 0, 120, 40)))]
    for state, film in (("idle", "dim"), ("active", "warm")):
        add(f"tab.{state}", "nine", (120, 40), soft(rbox(1.5, 1.5, 118.5, 38.5, 7), SOFT), [(film, soft(rbox(12, 8.6, 108, 31.4, 5), 3))],
            live=tab_live, margins=tab_m, stretch="x", roll=2.0, dz=-4.0, pad=1.5, creep=tab_creep)
    # The strips: a steel beam with a long channel of dark glass for the readouts; at each end a block with two bolts.
    # The brood has taken both ends: creep over the bolts (pustules), a sinew-wrapped conduit diving into the creep,
    # a chitin horn. 72 px, the channel's open band rows 13-59, as the Remnant's (the page's strip padding).
    s_live = [("readouts", box(104, 14, 408, 58))]
    s_m = (10, 100, 10, 100)
    for name, seed in (("strip.top", 91), ("strip.bottom", 93)):
        zl = zone((512, 72), s_m, "x", s_live).intersection(box(0, 0, 256, 72))
        zr = zone((512, 72), s_m, "x", s_live).intersection(box(256, 0, 512, 72))
        add(name, "nine", (512, 72), soft(rbox(0.5, 0.5, 511.5, 71.5, 12), SOFT), [("dim", soft(rbox(60, 10, 452, 62, 8), SOFT))],
            bolts=[(20, 22, 2.4), (20, 50, 2.4), (492, 22, 2.4), (492, 50, 2.4)], live=s_live, margins=s_m, stretch="x", dz=-6.0, pad=1.5,
            creep=[grown((6, 30), 34, 30, seed, zl, rot=0.3), grown((506, 42), 32, 30, seed + 1, zr, rot=-0.3)],
            pustules=[(20, 22, 4.4), (20, 50, 3.8), (492, 22, 3.8), (492, 50, 4.4), (44, 64, 2.6), (470, 8, 2.6)],
            ridges=[ridge([(36, 8), (20, 5), (6, 2.5)], [3.2, 2.0, 0.5]), ridge([(476, 64), (492, 67), (506, 69.5)], [3.2, 2.0, 0.5])],
            conduits=[conduit([(4, 62), (30, 60), (56, 66), (80, 67)], 2.6, (0.25, 0.55, 0.85))])
    add("divider", "nine", (128, 10), soft(rbox(0.5, 0.5, 127.5, 9.5, 4.5), 2), [("slot", soft(box(14, 3.3, 114, 6.7), 1.2))],
        margins=(0, 16, 0, 16), stretch="x", roll=0.6, land=1.0, dz=-2.0, pad=0.5, pustules=[(7, 5, 2.4), (121, 5, 2.4)])
    # Heroes. The name plate: a steel plaque with the name window; the brood has grown a fleshy pocket into its left end
    # round a round well where the captain's portrait lives, and creep over the right end.
    np_live = [("name", box(76, 13, 428, 43)), ("portrait", disc(30, 28, 13))]
    np_zone = zone((460, 56), None, None, [np_live[0]])
    add("nameplate", "hero", (460, 56), soft(rbox(0.5, 0.5, 459.5, 55.5, 14), SOFT),
        [("pocket", disc(30, 28, 18.5)), ("gold", soft(rbox(66, 9.5, 440, 46.5, 7), SOFT))],
        bolts=[(449.8, 28, 2.0)], live=np_live, dz=-5.0,
        creep=[grown((455, 28), 15, 22, 101, np_zone.intersection(box(441, 0, 460, 56)))],
        pustules=[(449.8, 28, 3.6), (56, 49, 2.4), (57, 7, 2.0)],
        pocket=dict(ring=disc(30, 28, 18.5).buffer(5.5).difference(disc(30, 28, 18.5).buffer(-2.0)).intersection(np_zone).buffer(0)),
        ridges=[ridge([(8, 45), (5, 50), (3.5, 53)], [2.4, 1.4, 0.4])])
    # The helm: a captured keyboard, nine machined key wells in a heavy plate, the centre one lit; bolts where the rows
    # cross. Creep from the outer corners fogs the corner wells' outer rims, chitin ridges grow along the top edge and a
    # sinew-wrapped conduit runs down the left side.
    keys, arrows = [], [(150, 63), (236, 63), (236, 149), (236, 235), (150, 235), (64, 235), (64, 149), (64, 63)]
    for r in range(3):
        for c in range(3):
            cx, cy = 64 + 86 * c, 63 + 86 * r
            keys.append(("hearth" if (r, c) == (1, 1) else "low", soft(rbox(cx - 31, cy - 31, cx + 31, cy + 31, 8), SOFT)))
    h_live = [(f"key {i}", box(x - 13, y - 11, x + 13, y + 11)) for i, (x, y) in enumerate(arrows)] + [("halt", box(128, 140, 172, 157))]
    hz = zone((300, 300), None, None, h_live)
    add("helm.pad", "hero", (300, 300), soft(rbox(1, 1, 299, 299, 18), SOFT), keys,
        bolts=[(107, 106, 3.0), (193, 106, 3.0), (107, 192, 3.0), (193, 192, 3.0)], live=h_live, dz=-8.0,
        creep=[grown((6, 6), 42, 38, 111, hz, rot=0.7, toward=(30, 28)), grown((294, 294), 44, 36, 112, hz, rot=0.7, toward=(270, 272)), grown((294, 8), 26, 22, 113, hz, rot=-0.7, toward=(276, 22))],
        pustules=[(107, 106, 5.0), (193, 192, 4.6), (107, 192, 3.4), (193, 106, 3.4), (20, 40, 3.0), (276, 270, 3.2)],
        ridges=[ridge([(60, 7), (100, 5.6), (150, 5.4), (200, 5.6), (240, 7)], [2.0, 4.0, 4.6, 4.0, 2.0]),
                ridge([(258, 8), (282, 4.6), (295, 3)], [3.0, 1.8, 0.4])],
        conduits=[conduit([(8, 90), (6, 150), (8, 210), (14, 262)], 2.6, (0.2, 0.5, 0.8))])
    # The ship display: a heavy steel bezel round the screen. The screen's corners are fogged by membrane that leaves the
    # middle clear (the live ship and shields); creep spreads over the bezel from the top-left and bottom-right; a spine
    # of chitin rises along the top edge, horns at its ends breaking the corners; a conduit wrapped in sinew runs along
    # the bottom land.
    ssd_live = [("ship and shields", disc(230, 150, 112))]
    sz = zone((460, 300), None, None, ssd_live)
    add("ssd.bezel", "hero", (460, 300), soft(rbox(0.5, 0.5, 459.5, 299.5, 16), SOFT), [("dim", soft(rbox(22, 22, 438, 278, 14), SOFT))],
        bolts=[(11.5, 11.5, 2.3), (448.5, 11.5, 2.3), (11.5, 288.5, 2.3), (448.5, 288.5, 2.3), (230, 11, 2.3)], live=ssd_live, dz=-9.0,
        creep=[grown((10, 10), 78, 62, 121, sz, rot=0.55, toward=(60, 50)), grown((450, 290), 82, 64, 122, sz, rot=0.6, toward=(400, 250)), grown((450, 12), 40, 34, 123, sz, rot=-0.6, toward=(420, 36)),
               grown((12, 288), 36, 30, 124, sz, rot=-0.5, toward=(40, 264))],
        pustules=[(11.5, 11.5, 4.6), (448.5, 11.5, 4.0), (11.5, 288.5, 4.0), (448.5, 288.5, 4.6), (230, 11, 4.2), (60, 40, 3.2), (404, 262, 3.0)],
        ridges=[ridge([(140, 7.5), (185, 6.4), (230, 6.2), (275, 6.4), (320, 7.5)], [2.0, 4.6, 5.4, 4.6, 2.0]),
                ridge([(46, 10), (24, 6.5), (6, 3.6)], [5.0, 3.0, 0.6]), ridge([(414, 10), (436, 6.5), (454, 3.6)], [5.0, 3.0, 0.6])],
        conduits=[conduit([(90, 289), (180, 287), (280, 290), (370, 288)], 3.0, (0.12, 0.32, 0.5, 0.68, 0.88))])
    # The gauge: a round steel housing round dark glass; four bolts, two swallowed; creep over the lower right.
    d_live = [("needle and contacts", disc(60, 60, 36))]
    add("dial", "hero", (120, 120), disc(60, 60, 59.5), [("dim", disc(60, 60, 40))],
        bolts=[(60 + math.cos(a) * 51, 60 + math.sin(a) * 51, 2.2) for a in [math.radians(45 + 90 * i) for i in range(4)]], live=d_live, dz=-7.0,
        creep=[grown((96, 94), 26, 18, 131, zone((120, 120), None, None, d_live).intersection(disc(60, 60, 59)), rot=-0.8)],
        pustules=[(60 + math.cos(math.radians(45)) * 51, 60 + math.sin(math.radians(45)) * 51, 4.0), (60 + math.cos(math.radians(225)) * 51, 60 + math.sin(math.radians(225)) * 51, 3.4)])
    # Alert lamps: a machined lamp housing, its lens now a membrane lit from within; creep on one side, the same in every
    # state.
    for c in ("green", "yellow", "red"):
        add(f"lamp.alert.{c}", "hero", (64, 64), disc(32, 32, 31.5), [(f"lamp_{c}", disc(32, 31, 17))], dz=-5.0, pad=2.0,
            creep=[grown((54, 52), 14, 11, 141, disc(32, 32, 31).difference(disc(32, 31, 17).buffer(-6)), rot=-0.8)],
            pustules=[(9, 22, 2.4)])
    # The crest (original): the brood's heart, a large glowing egg cradled by chitin ribs on a captured steel disc.
    add("crest", "hero", (96, 96), disc(48, 48, 46.5), [], dz=-6.0,
        ridges=[ridge([(20, 80), (17, 56), (24, 30), (36, 14)], [4.0, 3.4, 2.4, 0.6]), ridge([(76, 80), (79, 56), (72, 30), (60, 14)], [4.0, 3.4, 2.4, 0.6]),
                ridge([(30, 86), (48, 90), (66, 86)], [3.0, 3.6, 3.0])])
    return P


# The spore egg of the symbol bars (not a plate: the page lays the eggs in the track's groove): one outline and one
# membrane for all three states, so lit and unlit differ only in their light.
CELL = dict(size=(10, 14), gap=2)
CELL["outline"] = [(5 + 4.4 * math.cos(2 * math.pi * k / 24), 7 + 6.4 * math.sin(2 * math.pi * k / 24) * (1 - 0.08 * math.sin(2 * math.pi * k / 24))) for k in range(24)]

# The crest's egg (x, y, rx, ry), lit from within; checked to sit inside the disc.
EGG = (48, 50, 19, 27)


def growth_geoms(p):
    """Every piece of growth as (what, geometry), for rule G."""
    out = [(f"creep {i + 1}", g) for i, g in enumerate(p["creep"])]
    out += [(f"pustule {i + 1}", disc(x, y, r * 1.2)) for i, (x, y, r) in enumerate(p["pustules"])]   # with its ring of hide
    out += [(f"ridge {i + 1}", ridge_geom(r)) for i, r in enumerate(p["ridges"])]
    out += [(f"conduit {i + 1}", LineString(c["pts"]).buffer(c["r"] + 1.0)) for i, c in enumerate(p["conduits"])]
    if p["pocket"]:
        out.append(("pocket", p["pocket"]["ring"]))
    return out


def growth_fit(name, p):
    """Rule G: G1 inside the image, G2 clear of every live area, G3 on a 9-slice piece only in its unstretched cells."""
    w, h = p["size"]
    img = box(0, 0, w, h)
    res = []
    unstretched = None
    if p["margins"]:
        t, r, b, l = p["margins"]
        if p["stretch"] == "x":
            unstretched = unary_union([box(0, 0, l, h), box(w - r, 0, w, h)])
        else:
            unstretched = unary_union([box(0, 0, l, t), box(w - r, 0, w, t), box(0, h - b, l, h), box(w - r, h - b, w, h)])
    for what, g in growth_geoms(p):
        res.append(("G1", what, img.contains(g), "stays inside the piece's image"))
        if what == "pocket":   # the pocket is the portrait's own lip: it may touch the portrait, never the name
            near = min([g.distance(lg) for role, lg in p["live"] if role != "portrait"] or [99])
        else:
            near = min([g.distance(lg) for _r, lg in p["live"]] or [99])
        res.append(("G2", what, near >= CLEAR, f"{near:.1f} px from the nearest live area (needs {CLEAR:g})"))
        if unstretched is not None:
            res.append(("G3", what, unstretched.buffer(0.01).contains(g), "lies only in cells the slicing never stretches"))
    if name == "crest":
        egg = affinity.scale(Point(EGG[0], EGG[1]).buffer(1.0, quad_segs=24), EGG[2], EGG[3])
        res.append(("G1", "egg", p["plate"].buffer(-3).contains(egg), "the egg sits inside the disc"))
    return res


def check_all(P):
    """Run every piece through the fit rules; return (failures, report lines, films)."""
    bad, lines, films = 0, [], {}
    for name, p in P.items():
        wells = [w for _k, w in p["wells"]]
        gap = min(w.exterior.distance(p["plate"].exterior) for w in wells) if wells else 8.0
        inset = min(INSET, gap / 2)
        f = [behind_layer(w, p["plate"], grow=min(6.0, gap), inset=inset) for w in wells]
        if p["press"]:   # a pressed key's glass slides down, still clamped inside the plate (rule B)
            f = [affinity.translate(g, 0, p["press"]).intersection(p["plate"].buffer(-inset, quad_segs=16)) for g in f]
        films[name] = f
        res, _flat = fit(p["plate"], wells, knobs=[(x, y, r, None) for x, y, r in p["bolts"]],
                         prints=[(f"live {role}", g) for role, g in p["live"]],
                         behind=[(f"film behind well {i + 1}", g) for i, g in enumerate(f)],
                         roll=p["roll"], land=p["land"], inset=inset, pad=p["pad"])
        res = res + growth_fit(name, p)
        fails = [r for r in res if not r[2]]
        bad += len(fails)
        lines.append(f"{name}: {len(res) - len(fails)} of {len(res)} pass" + "".join(f"\n  FAIL {r[0]} {r[1]}: {r[3]}" for r in fails))
    return bad, lines, films


def pts(g):
    """An outline as points for Blender: simplified within 0.02 px and with no repeated point (the shared offset mitres
    each point, and a zero-length edge sends its offset off to infinity)."""
    if g.geom_type == "MultiPolygon":
        g = max(g.geoms, key=lambda q: q.area)
    out = []
    for x, y in list(g.simplify(0.02, preserve_topology=True).exterior.coords)[:-1]:
        q = [round(x, 3), round(y, 3)]
        if not out or abs(q[0] - out[-1][0]) + abs(q[1] - out[-1][1]) > 0.05:
            out.append(q)
    if len(out) > 2 and abs(out[0][0] - out[-1][0]) + abs(out[0][1] - out[-1][1]) <= 0.05:
        out.pop()
    return out


def rings(g):
    """A polygon with holes as [outside, hole, ...] point lists (the pocket's lip is a ring)."""
    if g.geom_type == "MultiPolygon":
        g = max(g.geoms, key=lambda q: q.area)
    return [pts(Polygon(g.exterior))] + [pts(Polygon(i)) for i in g.interiors]


def core_of(g):
    """The creep's thick core: the outline drawn in by a fifth of its size (3 to 8 px), leaving a thin membrane round it."""
    c = g.buffer(-min(8.0, max(3.0, math.sqrt(g.area) * 0.2)))
    return pts(c.buffer(-CORE_BEVEL)) if c.buffer(-CORE_BEVEL).area > 12 else None


def write(P, films):
    out = {"skin": "vhorr", "egg": list(EGG), "pieces": {},
           "cell": dict(size=list(CELL["size"]), gap=CELL["gap"], outline=[[round(x, 3), round(y, 3)] for x, y in CELL["outline"]])}
    for name, p in P.items():
        out["pieces"][name] = dict(kind=p["kind"], size=list(p["size"]), margins=list(p["margins"]) if p["margins"] else None,
                                   stretch=p["stretch"], inset=list(p["inset"]) if p["inset"] else None, roll=p["roll"], dz=p["dz"],
                                   plate=pts(p["plate"]), wells=[[k, pts(w)] for k, w in p["wells"]], films=[pts(f) for f in films[name]],
                                   bolts=[[round(x, 3), round(y, 3), r] for x, y, r in p["bolts"]], creep=[pts(g.buffer(-FOG_BEVEL)) for g in p["creep"]],
                                   core=[core_of(g) for g in p["creep"]],
                                   pustules=[[round(x, 3), round(y, 3), r] for x, y, r in p["pustules"]], ridges=p["ridges"],
                                   conduits=p["conduits"], pocket=rings(p["pocket"]["ring"].buffer(-POCKET_BEVEL)) if p["pocket"] else None,
                                   live=[[role, [round(v, 3) for v in g.bounds]] for role, g in p["live"]])
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "shapes.json"), "w") as f:
        json.dump(out, f, separators=(",", ":"))
        f.write("\n")


def footprint(p):
    """The piece's footprint on the render: the plate and every growth over it (growth may break the plate's outline)."""
    return unary_union([p["plate"]] + [g for _w, g in growth_geoms(p)])


def verify(folder):
    """Rule B on the finished renders: no pixel with any opacity outside the piece's footprint, plate and growth (1.5
    render px of edge antialiasing allowed)."""
    from PIL import Image, ImageDraw
    P, bad = pieces(), 0
    for name, p in P.items():
        path = os.path.join(folder, f"{name}.png")
        if not os.path.exists(path):
            continue
        im = Image.open(path)
        s = im.size[0] / p["size"][0]
        mask = Image.new("L", im.size, 0)
        fp = footprint(p).buffer(0.75)
        for g in (fp.geoms if fp.geom_type == "MultiPolygon" else [fp]):
            ImageDraw.Draw(mask).polygon([(x * s, y * s) for x, y in g.exterior.coords], fill=255)
        a, m = im.getchannel("A").load(), mask.load()
        stray = sum(1 for y in range(im.size[1]) for x in range(im.size[0]) if a[x, y] > 8 and not m[x, y])
        bad += stray > 0
        print(f"{'PASS' if not stray else 'FAIL'} B {name}: {stray} lit px outside the footprint")
    return bad


def main():
    if "--verify" in sys.argv:
        sys.exit(1 if verify(sys.argv[sys.argv.index("--verify") + 1]) else 0)
    P = pieces()
    bad, lines, films = check_all(P)
    print("\n".join(lines))
    print("failures:", bad)
    if bad:
        sys.exit(1)
    write(P, films)
    print("wrote", os.path.relpath(os.path.join(OUT, "shapes.json"), ROOT))


if __name__ == "__main__":
    main()
