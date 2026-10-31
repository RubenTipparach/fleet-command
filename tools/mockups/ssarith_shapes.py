"""Fleet Command: the Ssarith Hunt kit's shapes, fit-checked, as the one source for its render and its page.

Owns the geometry of every piece of the Ssarith Hunt skin (mockup 4, console-skins), rebuilt on the analog panel bible
(owner, 2026-10-31: "we need to update krai, onbrai, and hunt to use the new guildine standards"): the pillowed plate of
each piece (worn olive gunmetal, every edge rolled over), the tube wells cut through it (each a small CRT: phosphor film
behind the plate, with scanlines printed on it), the hex bolts and jewel lamps standing on the flat metal, the tally
marks and tracking reticles engraved in bone on the flat, the frond leaves lying on the flat of the hero pieces, the
print on the tubes (scanlines, the tracker's grid, icon glyphs) and the film behind each well (measured to fit, rule B).
It also owns the three cells of the Hunt's symbol bar (claw marks on a tiny tube, lit or unlit).
tools/blender/build_ui_ssarith.py renders exactly these shapes; docs/mockups/skins/ssarith.js places its live readouts
inside the same wells (CLAUDE.md section 6.1).

Every piece passes tools/mockups/formed_metal.py's fit rules before anything is written (the analog panel bible,
chapter 9): K studs on flat metal, L tips that hold the roll, R rolled edges that never meet, P print and live readouts
inside their wells with an even margin, B films inside the plate; and two of the Hunt's own: engraving and leaves lie on
the flat metal, never over a roll or a well. A failure writes nothing and exits 1.

    python3 tools/mockups/ssarith_shapes.py              # check, then write assets/ui/ssarith/shapes.json
    python3 tools/mockups/ssarith_shapes.py --verify assets/ui/ssarith   # rule B on the renders: no lit pixel
                                                                         # outside the plate's footprint

Units: slot px = mm, y down, at the 1920 x 1080 reference (renders are at 2x). Needs shapely. Our own design throughout:
no other game's or film's names, shapes, glyphs or art.
"""
import json
import math
import os
import sys

from shapely.geometry import LineString, Point, Polygon, box

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from formed_metal import INSET, behind_layer, disc, fit, rbox  # noqa: E402

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "assets", "ui", "ssarith", "shapes.json")


def cut(x0, y0, x1, y1, c):
    """A rectangle with every corner cut back c px at 45 degrees (the hunter's kit is cut-cornered, never round)."""
    return Polygon([(x0 + c, y0), (x1 - c, y0), (x1, y0 + c), (x1, y1 - c), (x1 - c, y1), (x0 + c, y1), (x0, y1 - c), (x0, y0 + c)])


def soft_cut(x0, y0, x1, y1, c, f=None):
    """A cut-cornered outline whose corners are eased rounder than any roll it gets (rule L): a plate's outline is
    offset inward by its roll before it is bevelled, and a corner tighter than that folds over into a spike."""
    f = f if f is not None else min(4.0, c / 2.0, (x1 - x0) / 4.0, (y1 - y0) / 4.0)
    return cut(x0, y0, x1, y1, c).buffer(f, join_style=1, quad_segs=6).buffer(-2 * f, join_style=1, quad_segs=6) \
        .buffer(f, join_style=1, quad_segs=6).simplify(0.02)


def octagon(cx, cy, r):
    return [(cx + math.cos(math.pi / 8 + i * math.pi / 4) * r, cy + math.sin(math.pi / 8 + i * math.pi / 4) * r) for i in range(9)]


def lines_geom(lines, w):
    """The ink a set of polylines leaves, w px wide, as one shape."""
    from shapely.ops import unary_union
    return unary_union([LineString(l).buffer(w / 2, quad_segs=6) for l in lines if len(l) > 1])


def clip(lines, well, keep):
    """The parts of straight 2-point lines that lie keep px inside the well (so print never meets the rim, rule P)."""
    area, out = well.buffer(-keep), []
    for a, b in lines:
        g = LineString([a, b]).intersection(area)
        for seg in getattr(g, "geoms", [g]):
            if seg.length > 1.0:
                out.append([tuple(round(v, 2) for v in c) for c in seg.coords])
    return out


def scan(well, pitch=2.6, keep=3.4):
    """A tube's scanlines: horizontal rules pitch px apart across the well (they survive the "x" stretch)."""
    x0, y0, x1, y1 = well.bounds
    lines, y = [], y0 + pitch / 2
    while y < y1:
        lines.append([(x0, y), (x1, y)])
        y += pitch
    return clip(lines, well, keep)


def tally(x, y, groups, size=10.0):
    """Tally marks of the hunt: groups of four strokes struck through by a fifth, as polylines."""
    out = []
    for g in range(groups):
        gx = x + g * size * 1.15
        out += [[(gx + i * size * 0.18, y - size / 2), (gx + i * size * 0.18 + 0.6, y + size / 2)] for i in range(4)]
        out.append([(gx - 1.5, y + size * 0.3), (gx + size * 0.7, y - size * 0.3)])
    return out


def reticle(cx, cy, r):
    """A tracking reticle: four chevrons closing on a point, open at the middle."""
    out = []
    for a in (0.25, 0.75, 1.25, 1.75):
        ang = a * math.pi
        tip = (cx + math.cos(ang) * r * 0.45, cy + math.sin(ang) * r * 0.45)
        out.append([(cx + math.cos(ang - 0.35) * r, cy + math.sin(ang - 0.35) * r), tip, (cx + math.cos(ang + 0.35) * r, cy + math.sin(ang + 0.35) * r)])
    return out


def grid(well, step, keep=3.4):
    """The tracker's faint grid: vertical and horizontal rules step px apart, clipped inside the well."""
    x0, y0, x1, y1 = well.bounds
    lines = [[(x0 + i * step, y0), (x0 + i * step, y1)] for i in range(1, int((x1 - x0) / step) + 1)]
    lines += [[(x0, y0 + i * step), (x1, y0 + i * step)] for i in range(1, int((y1 - y0) / step) + 1)]
    return clip(lines, well, keep)


def leaf(cx, cy, length, width, angle, n=14):
    """A frond leaf's outline from its stalk at (cx, cy), pointing along angle (radians, y down)."""
    ca, sa = math.cos(angle), math.sin(angle)
    side = [(t, width * math.sin(math.pi * t) ** 0.7 * (1 - 0.25 * t)) for t in (i / n for i in range(n + 1))]
    pts = [(t * length, h) for t, h in side] + [(t * length, -h) for t, h in reversed(side[1:-1])]
    return Polygon([(cx + x * ca - y * sa, cy + x * sa + y * ca) for x, y in pts])


# Icon glyphs (48 px icons), drawn from the hunt: straight strokes and 45 degree turns, glowing on a tiny tube.
GLYPHS = {
    "helm": [[(16, 36), (24, 12), (32, 36)], [(19, 28), (29, 28)]],                                      # a track mark
    "fire": [[(10, 16), (10, 10), (16, 10)], [(32, 10), (38, 10), (38, 16)], [(38, 32), (38, 38), (32, 38)],
             [(16, 38), (10, 38), (10, 32)], octagon(24, 24, 2.5)],                                        # brackets closing
    "shields": [[(24, 8), (36, 16), (36, 28), (24, 40), (12, 28), (12, 16), (24, 8)], [(12, 22), (36, 22)], [(18, 31), (30, 31)]],  # a scale plate
    "tractor": [[(10, 12), (24, 12)], octagon(24, 26, 9), [(24, 12), (24, 17)]],                         # a snare loop
    "transport": [[(24, 8), (24, 30)], [(17, 23), (24, 30), (31, 23)], [(12, 34), (12, 40), (36, 40), (36, 34)]],
    "science": [[(8, 26), (14, 26), (17, 18), (21, 34), (25, 12), (29, 30), (32, 22), (40, 22)]],        # the quarry's scent
    "comms": [[(10, 24), (16, 18), (22, 30), (28, 14), (34, 30), (38, 24)]],                              # the call
    "repair": [[(12, 38), (30, 20)], [(30, 20), (38, 10), (34, 22), (30, 20)], [(10, 34), (16, 40)]],     # a skinning blade
    "map": [[(10, 38), (16, 30), (14, 22), (22, 16), (30, 18), (34, 10)], [(30, 6), (38, 14)], [(38, 6), (30, 14)]],  # a trail to an X
    "settings": [[(12, 12), (13, 36)], [(18, 12), (19, 36)], [(24, 12), (25, 36)], [(30, 12), (31, 36)], [(8, 30), (36, 18)]],  # the tally
}

# The symbol bar's cell: three claw marks raked across a tiny tube (10 x 14 px), the Hunt's count of the quarry's
# strength left. Lit and unlit share every stroke; only the phosphor's light changes.
CLAW = [[(2.6, 3.0), (4.4, 11.0)], [(5.0, 2.4), (6.6, 11.6)], [(7.4, 3.0), (8.6, 10.4)]]


def pieces():
    """Every piece: kind, size, 9-slice margins (top, right, bottom, left), stretch, the cell inset (bars), roll and
    land, the plate, its wells (each with its light: tube, lit or slot), studs (x, y, r, style), print on the tubes
    (role, polylines, width, ink), live readouts (role, box) the page draws inside a well, engraving on the flat metal,
    frond leaves on the flat, and whether the plate carries the hide-scale texture (hero pieces only: a 9-slice piece
    stretches, and texture on what stretches smears)."""
    P = {}

    def add(name, kind, size, plate, wells, studs=(), prints=(), live=(), engrave=(), leaves=(), margins=None, stretch=None,
            roll=3.0, land=3.0, inset=None, dz=-8.0, pad=3.0, scales=False, gap=None):
        P[name] = dict(kind=kind, size=size, margins=margins, stretch=stretch, roll=roll, land=land, inset=inset, plate=plate,
                       wells=list(wells), studs=list(studs), prints=list(prints), live=list(live), engrave=list(engrave),
                       leaves=list(leaves), dz=dz, pad=pad, scales=scales, gap=gap)

    # Frames. The panel is a thick cut-cornered gunmetal bezel round one dark tube (the panel's content sits on the
    # glass), a hex bolt sunk in each corner of the bezel. The inset is a smaller tube in a thinner bezel.
    w = soft_cut(20, 20, 160, 120, 7)
    add("frame.panel", "nine", (180, 140), soft_cut(0.5, 0.5, 179.5, 139.5, 14), [("tube", w)],
        studs=[(x, y, 2.2, "bolt") for x, y in ((22, 10), (158, 10), (22, 130), (158, 130))],
        prints=[("scan", scan(w, 3.0), 0.5, "scan")], margins=(28, 28, 28, 28), stretch="xy", roll=3.0, dz=-6.0)
    w = soft_cut(8, 8, 64, 48, 4)
    add("frame.inset", "nine", (72, 56), soft_cut(0.5, 0.5, 71.5, 55.5, 7), [("tube", w)],
        prints=[("scan", scan(w, 3.0, 2.6), 0.5, "scan")], margins=(12, 12, 12, 12), stretch="xy", roll=2.0, dz=-4.0, pad=2.0)
    # The symbol bar's track: a dark tube slot rolled through a thin plate; the page lays the claw cells in it at
    # inset (9, 3), 14 px tall.
    add("bar.track", "nine", (96, 20), soft_cut(0.5, 0.5, 95.5, 19.5, 4), [("slot", rbox(4, 3, 92, 17, 3))],
        live=[("cells", box(9, 3.6, 87, 16.4))], margins=(0, 16, 0, 16), stretch="x", roll=0.8, land=0.9, inset=(9, 3),
        dz=-2.0, pad=0.6)
    # Keys: the Tube key, a small CRT screen in a cut-cornered gunmetal bezel; dark at rest, lit on hover, flaring when
    # pressed. One shape for every state, so only the tube's light changes.
    w = soft_cut(11, 10, 139, 38, 5)
    for state in ("idle", "hover", "pressed"):
        add(f"button.{state}", "nine", (150, 48), soft_cut(0.5, 1.5, 149.5, 46.5, 9), [("tube", w)],
            prints=[("scan", scan(w, 2.6), 0.6, "scan")], live=[("label", box(20, 14, 130, 34))],
            margins=(0, 22, 0, 22), stretch="x", roll=1.6, land=2.0, dz=-4.0, pad=2.5)
    # Tabs: a plate with a long tube for the arming state, three bone tally strokes engraved at its left.
    w = soft_cut(22, 9, 111, 31, 4)
    for state in ("idle", "active"):
        add(f"tab.{state}", "nine", (120, 40), soft_cut(0.5, 2.5, 119.5, 37.5, 7), [("tube", w)],
            prints=[("scan", scan(w, 2.6), 0.5, "scan")], live=[("label", box(28, 13, 105, 27))],
            engrave=[[(8 + i * 3.4, 14), (8.6 + i * 3.4, 26)] for i in range(3)], margins=(0, 16, 0, 28), stretch="x",
            roll=1.6, land=2.0, dz=-4.0, pad=2.5)
    # Strips: a long gunmetal rail. At its left end a bay holds a small tracker tube (a sweep and its grid); at its right
    # end the hunt's tally is engraved beside a sunk bolt. The middle is plain flat metal, where the readouts sit.
    # 72 px tall since 2026-10-31 (owner: "we need to make parent taller"): the tracker tube, bolts and tally keep their
    # size, centred on row 36; the flat middle grows.
    w = soft_cut(14, 19, 76, 53, 6)
    for name in ("strip.top", "strip.bottom"):
        add(name, "nine", (512, 72), soft_cut(0.5, 0.5, 511.5, 71.5, 12), [("tube", w)],
            studs=[(492, 23, 2.4, "bolt"), (492, 49, 2.4, "bolt")],
            prints=[("grid", grid(w, 7.0, 2.8), 0.5, "grid"), ("sweep", [[(45, 36), (64, 27)]], 1.2, "phos")],
            engrave=tally(440, 36, 2, 12.0), margins=(10, 92, 10, 88), stretch="x", roll=2.6, dz=-6.0, pad=2.0)
    add("divider", "nine", (128, 10), soft_cut(0.5, 1.5, 127.5, 8.5, 3), [], margins=(0, 12, 0, 12), stretch="x", roll=1.2)
    # Heroes carry the hide-scale texture. The name plate: a tube for the live name; a reticle engraved at its left, the
    # tally of kills at its right, a sunk bolt, and a frond lying on the flat.
    w = soft_cut(54, 9, 372, 47, 6)
    add("nameplate", "hero", (460, 56), soft_cut(0.5, 0.5, 459.5, 55.5, 10), [("tube", w)],
        studs=[(444, 15, 2.4, "bolt"), (444, 41, 2.4, "bolt")],
        prints=[("scan", scan(w, 2.6), 0.5, "scan")], live=[("name", box(62, 13, 364, 43))],
        engrave=reticle(28, 28, 13) + tally(386, 28, 2, 12.0), leaves=[(426, 40, 20, 4.0, -2.75)],
        roll=2.6, dz=-7.0, scales=True)
    # The helm: a cut-cornered plate with eight small tubes round a ring of radius 94 (the page's helmR), one for each
    # direction key (the page draws its arrow in it), and the tracker's round scope in the middle with a reticle; bolts
    # in the four cut corners, fronds over the upper right.
    keys = [(150 + math.cos(a) * 94, 150 + math.sin(a) * 94) for a in [-math.pi / 2 + i * math.pi / 4 for i in range(8)]]
    add("helm.pad", "hero", (300, 300), soft_cut(0.5, 0.5, 299.5, 299.5, 62),
        [("tube", soft_cut(x - 21, y - 21, x + 21, y + 21, 6)) for x, y in keys] + [("lit", Point(150, 150).buffer(46, quad_segs=48))],
        studs=[(x, y, 3.2, "bolt") for x, y in ((40, 40), (260, 40), (40, 260), (260, 260))],
        prints=[("reticle", reticle(150, 150, 34), 1.2, "phos"), ("ring", [[(150 + math.cos(a) * 18, 150 + math.sin(a) * 18) for a in
                [2 * math.pi * i / 48 for i in range(49)]]], 0.6, "grid")]
        + [("scan", scan(soft_cut(x - 21, y - 21, x + 21, y + 21, 6), 2.6), 0.5, "scan") for x, y in keys],
        live=[(f"key {i}", box(x - 12, y - 12, x + 12, y + 12)) for i, (x, y) in enumerate(keys)],
        leaves=[(196, 10, 30, 6.0, 0.15), (214, 14, 28, 5.6, 0.5), (228, 20, 26, 5.4, 0.75)], roll=3.2, dz=-9.0, scales=True)
    # The ship display: one big tube with the tracker's grid in a heavy plate; vents in the side bands, bolts in the
    # corners, the tally along the bottom band, fronds over the upper right corner. The live shield rings fill r 101.
    w = soft_cut(28, 22, 432, 278, 14)
    add("ssd.bezel", "hero", (460, 300), soft_cut(0.5, 0.5, 459.5, 299.5, 24), [("tube", w)],
        studs=[(52, 11, 3.0, "bolt"), (408, 11, 3.0, "bolt"), (52, 289, 3.0, "bolt"), (408, 289, 3.0, "bolt")],
        prints=[("grid", grid(w, 24.0), 0.5, "grid"), ("scan", scan(w, 3.0), 0.4, "scan")],
        live=[("rings", disc(230, 150, 103))],
        engrave=tally(80, 289, 4, 9.0) + [[(8, y), (20, y)] for y in range(120, 184, 8)] + [[(440, y), (452, y)] for y in range(120, 184, 8)],
        leaves=[(410, 9, 30, 6.0, 0.2), (440, 30, 28, 5.6, 1.45)], roll=3.2, dz=-9.0, scales=True)
    add("dial", "hero", (120, 120), Polygon(octagon(60, 60, 64.5)).intersection(box(0.5, 0.5, 119.5, 119.5)),
        [("tube", Point(60, 60).buffer(46, quad_segs=48))],
        prints=[("ring", [[(60 + math.cos(a) * 22, 60 + math.sin(a) * 22) for a in [2 * math.pi * i / 48 for i in range(49)]]], 0.6, "grid")],
        live=[("scope", disc(60, 60, 42))],
        engrave=[[(60 + math.cos(a) * 52, 60 + math.sin(a) * 52), (60 + math.cos(a) * (56 if i % 3 == 0 else 54.5), 60 + math.sin(a) * (56 if i % 3 == 0 else 54.5))]
                 for i, a in enumerate([j * math.pi / 6 for j in range(12)])],
        roll=2.2, dz=-7.0, scales=True)
    for c in ("green", "yellow", "red"):
        add(f"lamp.alert.{c}", "hero", (64, 64), soft_cut(0.5, 0.5, 63.5, 63.5, 12), [],
            studs=[(32, 32, 13.0, f"jewel_{c}")], roll=2.0, dz=-4.0, scales=True)
    # The crest: the Hunt's mark, a trophy fang hung inside a tracking reticle, inlaid in bone on the flat of a
    # cut-cornered plate.
    add("crest", "hero", (96, 96), soft_cut(0.5, 0.5, 95.5, 95.5, 22), [],
        engrave=reticle(48, 48, 34) + [[(41, 30), (55, 30), (52, 52), (48, 68), (44, 52), (41, 30)], [(39, 27), (57, 27)]],
        roll=3.0, dz=-4.0, scales=True)
    for n, g in GLYPHS.items():
        s = 0.66
        lines = [[(round(24 + (x - 24) * s, 2), round(24 + (y - 24) * s, 2)) for x, y in l] for l in g]
        add(f"icon.{n}", "hero", (48, 48), soft_cut(0.5, 0.5, 47.5, 47.5, 9), [("tube", soft_cut(8, 8, 40, 40, 6))],
            prints=[("glyph", lines, 1.5, "phos")], roll=1.8, dz=-4.0, pad=2.5)
    # The symbol bar's three cells: one tiny tube, the same claw marks in each; only the light differs.
    for state in ("off", "on", "warn"):
        add(f"bar.cell.{state}", "cell", (10, 14), rbox(0.3, 0.3, 9.7, 13.7, 1.6), [("cell", rbox(0.3, 0.3, 9.7, 13.7, 1.6))],
            prints=[("claw", CLAW, 1.3, {"off": "clawoff", "on": "phos", "warn": "kill"}[state])], roll=0.0, land=0.0, dz=-0.5,
            pad=0.4, gap=2)
    return P


def check_all(P):
    """Run every piece through the fit rules; return (failures, report lines, films)."""
    bad, lines, films = 0, [], {}
    for name, p in P.items():
        wells = [g for _, g in p["wells"]]
        if name.startswith("bar.cell"):  # a cell is all tube: its glass is its plate; only its print is checked (P)
            g = lines_geom(p["prints"][0][1], p["prints"][0][2])
            ok = wells[0].buffer(-p["pad"]).contains(g)
            films[name] = []
            bad += not ok
            lines.append(f"{name}: {'1 of 1 pass' if ok else '0 of 1 pass'}" + ("" if ok else "\n  FAIL P claw marks leave the cell"))
            continue
        gap = min([w.exterior.distance(p["plate"].exterior) for w in wells] or [8.0])
        inset = min(INSET, gap / 2)
        f = [behind_layer(w, p["plate"], grow=min(8.0, gap), inset=inset) for w in wells]
        films[name] = f
        prints = [(f"print {role}", lines_geom(ls, w)) for role, ls, w, _ in p["prints"]] + [(f"live {role}", g) for role, g in p["live"]]
        res, flat = fit(p["plate"], wells, knobs=[(x, y, r, None) for x, y, r, _st in p["studs"]], prints=prints,
                        behind=[(f"film behind well {i + 1}", g) for i, g in enumerate(f)], roll=p["roll"], land=p["land"],
                        inset=inset, pad=p["pad"])
        if p["engrave"]:  # engraving lies on the flat metal, never over a roll or a well
            ok = flat.buffer(0.05).contains(lines_geom(p["engrave"], 1.0))
            res.append(("K", "the engraving", ok, "lies on flat metal"))
        for i, (cx, cy, length, width, ang) in enumerate(p["leaves"]):  # a leaf lies on the flat, clear of every roll
            ok = flat.buffer(0.05).contains(leaf(cx, cy, length, width, ang))
            res.append(("K", f"leaf {i + 1}", ok, "lies on flat metal"))
        fails = [r for r in res if not r[2]]
        bad += len(fails)
        lines.append(f"{name}: {len(res) - len(fails)} of {len(res)} pass" + "".join(f"\n  FAIL {r[0]} {r[1]}: {r[3]}" for r in fails))
    return bad, lines, films


def pts(g):
    return [[round(x, 3), round(y, 3)] for x, y in list(g.exterior.coords)[:-1]]


def poly_lines(ls):
    return [[[round(x, 3), round(y, 3)] for x, y in l] for l in ls]


def write(P, films):
    out = {"skin": "ssarith", "pieces": {}}
    for name, p in P.items():
        out["pieces"][name] = dict(kind=p["kind"], size=list(p["size"]), margins=list(p["margins"]) if p["margins"] else None,
                                   stretch=p["stretch"], inset=list(p["inset"]) if p["inset"] else None, roll=p["roll"], dz=p["dz"],
                                   plate=pts(p["plate"]), wells=[[kind, pts(g)] for kind, g in p["wells"]],
                                   films=[pts(f) for f in films[name]], studs=[list(s) for s in p["studs"]],
                                   prints=[[role, poly_lines(ls), w, ink] for role, ls, w, ink in p["prints"]],
                                   engrave=poly_lines(p["engrave"]), leaves=[[list(l), pts(leaf(*l))] for l in p["leaves"]], scales=p["scales"],
                                   live={role: [round(v, 2) for v in g.bounds] for role, g in p["live"]}, gap=p["gap"])
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w") as f:
        json.dump(out, f, separators=(",", ":"))
        f.write("\n")


def verify(folder):
    """Rule B on the finished renders: no pixel with any opacity outside the piece's plate footprint (0.75 px of edge
    antialiasing allowed)."""
    from PIL import Image, ImageDraw
    P, bad = pieces(), 0
    for name, p in P.items():
        path = os.path.join(folder, f"{name}.png")
        if not os.path.exists(path):
            continue
        im = Image.open(path)
        s = im.size[0] / p["size"][0]
        mask = Image.new("L", im.size, 0)
        ImageDraw.Draw(mask).polygon([(x * s, y * s) for x, y in p["plate"].buffer(0.75).exterior.coords], fill=255)
        a, m = im.getchannel("A").load(), mask.load()
        stray = sum(1 for y in range(im.size[1]) for x in range(im.size[0]) if a[x, y] > 8 and not m[x, y])
        bad += stray > 0
        print(f"{'PASS' if not stray else 'FAIL'} B {name}: {stray} lit px outside the plate")
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
    print("wrote", os.path.relpath(OUT, ROOT))


if __name__ == "__main__":
    main()
