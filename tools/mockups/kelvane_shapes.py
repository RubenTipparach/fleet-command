"""Fleet Command: the Kelvane Institute kit's shapes, fit-checked, as the one source for its render and its page.

Owns the geometry of every piece of the Kelvane skin (mockup 4, console-skins): the pillowed champagne plate of each
piece (its outline and roll), the display wells cut through it, which kind of light each well holds (amber backlit film
for readouts, green phosphor glass for scopes, a metal tray for the contacts panel), the studs on the metal (domed keys,
knurled knobs, screws, jewel lamps), the pressed dimples and the helm's pressed cradle, the print on each display
(graticules, meter scales, icon glyphs), and the film behind each well (measured to fit, rule B). The Institute was
inspired by the Nucleo NLog look (owner, 2026-10-31: "They were inspired by the nucleo look. So.lets update that"), so
it is built the way the session's NLog study was: two layers, a pillowed metal plate over recessed lit displays.
tools/blender/build_ui_kelvane.py renders exactly these shapes; docs/mockups/skins/kelvane.js places its live readouts
inside the same wells (CLAUDE.md section 6.1).

Every piece passes tools/mockups/formed_metal.py's fit rules before anything is written (the analog panel bible,
chapter 9): K studs on flat metal, L tips that hold the roll, R rolled edges that never meet (dimples, the cradle and
every well keep their land), P print and live readouts inside their wells with an even margin, B films inside the plate.
A failure writes nothing and exits 1.

    python3 tools/mockups/kelvane_shapes.py              # check, then write assets/ui/kelvane/shapes.json
    python3 tools/mockups/kelvane_shapes.py --verify assets/ui/kelvane   # rule B on the renders: no lit pixel
                                                                         # outside the plate's footprint

Units: slot px = mm, y down, at the 1920 x 1080 reference (renders are at 2x). Needs shapely.
"""
import json
import math
import os
import sys

from shapely import affinity
from shapely.geometry import LineString, Point, Polygon, box
from shapely.ops import unary_union

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from formed_metal import DIMPLE_RIM, INSET, behind_layer, disc, fit, rbox, soft  # noqa: E402

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "assets", "ui", "kelvane", "shapes.json")

# Icon glyphs (48 px icons), drawn for the Institute: instruments and lab glassware, a line each. They are printed in
# ink on a small amber window, so they are scaled into it about the icon's centre.
GLYPHS = {
    "helm": [[(24 + 13 * math.cos(a), 28 + 13 * math.sin(a)) for a in [math.pi * (1.1 + 0.8 * i / 12) for i in range(13)]], [(24, 28), (31, 17)]],
    "fire": [[(27, 8), (17, 25), (26, 25), (20, 40)]],
    "shields": [[(24 + 11 * math.cos(a), 22 + 11 * math.sin(a)) for a in [math.pi * (1 + i / 12) for i in range(13)]], [(13, 22), (13, 36)], [(35, 22), (35, 36)], [(10, 36), (16, 36)], [(32, 36), (38, 36)]],
    "tractor": [[(24 + 9 * math.cos(a), 20 + 9 * math.sin(a)) for a in [math.pi * (1 + i / 12) for i in range(13)]], [(15, 20), (15, 28)], [(33, 20), (33, 28)],
                [(24 + 6 * math.cos(a), 34 + 6 * math.sin(a)) for a in [math.pi * (0.2 + 0.6 * i / 8) for i in range(9)]],
                [(24 + 10 * math.cos(a), 34 + 10 * math.sin(a)) for a in [math.pi * (0.2 + 0.6 * i / 8) for i in range(9)]]],
    "transport": [[(24 + 9 * math.cos(a), 20 + 9 * math.sin(a)) for a in [math.pi * (1 + i / 12) for i in range(13)]], [(15, 20), (15, 34), (33, 34), (33, 20)], [(20, 38), (20, 42)], [(28, 38), (28, 42)], [(24, 16), (24, 28)]],
    "science": [[(20, 8), (20, 20), (11, 38), (37, 38), (28, 20), (28, 8)], [(17, 8), (31, 8)], [(15, 30), (33, 30)]],
    "comms": [[(8 + i, 24 + math.sin(i / 32 * 4 * math.pi) * 9) for i in range(33)]],
    "repair": [[(14, 34), (30, 18)], [(33 + 6 * math.cos(a), 15 + 6 * math.sin(a)) for a in [math.pi * (0.9 + 1.7 * i / 14) for i in range(15)]],
               [(13 + 3 * math.cos(a), 35 + 3 * math.sin(a)) for a in [2 * math.pi * i / 10 for i in range(11)]]],
    "map": [[(8, 34), (40, 34)]] + [[(8 + i * 4, 34), (8 + i * 4, 30 if i % 2 else 26)] for i in range(9)] + [[(10, 14), (38, 14)], [(10, 10), (10, 18)], [(38, 10), (38, 18)]],
    "settings": [[(24 + 10 * math.cos(a), 24 + 10 * math.sin(a)) for a in [2 * math.pi * i / 20 for i in range(21)]], [(24, 24), (24, 15)]]
    + [[(24 + math.cos(a) * 14, 24 + math.sin(a) * 14), (24 + math.cos(a) * 18, 24 + math.sin(a) * 18)] for a in [math.pi * 0.75 + i * math.pi * 0.1875 for i in range(9)]],
}


def lines_geom(lines, w):
    """The ink a set of polylines leaves, w px wide, as one shape (for rule P)."""
    return unary_union([LineString(l).buffer(w / 2, quad_segs=6) for l in lines if len(l) > 1])


def arc_pts(cx, cy, r, a0, a1, n=24):
    return [(cx + r * math.cos(a0 + (a1 - a0) * i / n), cy + r * math.sin(a0 + (a1 - a0) * i / n)) for i in range(n + 1)]


def graticule(cx, cy, r, rings, spokes=4):
    """A scope's faint graticule: rings and a cross, as polylines."""
    out = [arc_pts(cx, cy, rr, 0, 2 * math.pi, 48) for rr in rings]
    for i in range(spokes):
        a = i * math.pi / spokes
        out.append([(cx - r * math.cos(a), cy - r * math.sin(a)), (cx + r * math.cos(a), cy + r * math.sin(a))])
    return out


def pieces():
    """Every piece: kind, size, 9-slice margins (top, right, bottom, left), stretch, the fill inset (bars), roll and land
    for the fit rule, the plate, its wells (each with its light: amber, phos or tray), studs (x, y, r, style), dimples
    (x, y, r), the helm cradle, print on the displays (role, polylines, width, ink), live readouts (role, box) the page
    draws inside a well, engraving on the metal (polylines), and its lamps."""
    P = {}

    def add(name, kind, size, plate, wells, studs=(), dimples=(), prints=(), live=(), engrave=(), margins=None, stretch=None,
            roll=3.0, land=3.0, inset=None, dz=-8.0, pad=3.0, lamps=(), cradle=None, emblem=()):
        P[name] = dict(kind=kind, size=size, margins=margins, stretch=stretch, roll=roll, land=land, inset=inset, plate=plate,
                       wells=list(wells), studs=list(studs), dimples=list(dimples), prints=list(prints), live=list(live),
                       engrave=list(engrave), dz=dz, pad=pad, lamps=list(lamps), cradle=cradle, emblem=list(emblem))

    # Frames. The panel is a champagne plate with a fine engraved line inside its roll and a screw in each corner;
    # its middle is plain flat metal that stretches. The inset is a pressed tray: an opening onto a sunken metal floor
    # (no light), where the contacts readout and its icon keys sit.
    add("frame.panel", "nine", (180, 140), rbox(0.5, 0.5, 179.5, 139.5, 14), [],
        studs=[(x, y, 3.0, "screw") for x in (18, 162) for y in (18, 122)],
        engrave=[[(x, y) for x, y in list(rbox(9, 9, 171, 131, 8).exterior.coords)]], margins=(28, 28, 28, 28), stretch="xy", roll=3.5)
    add("frame.inset", "nine", (72, 56), rbox(0.5, 0.5, 71.5, 55.5, 10), [("tray", rbox(7.5, 7.5, 64.5, 48.5, 5))],
        margins=(14, 14, 14, 14), stretch="xy", roll=2.0, dz=-3.0)
    # The bar: a slot rolled through the plate holding a row of jewel lamps, the instrument's indicator row (owner,
    # 2026-10-31: "have different repeated symbols for them"); the page lays the lamps (bar.cell.*) at inset (10, 4).
    slot = rbox(5, 3, 91, 17, 6)
    add("bar.track", "nine", (96, 20), rbox(0.5, 0.5, 95.5, 19.5, 9.5), [("slot", slot)],
        live=[("cells", box(10, 4, 86, 16))],
        margins=(0, 14, 0, 14), stretch="x", roll=0.8, land=0.9, inset=(10, 4), dz=-2.5, pad=0.8)
    # Keys: a domed key in a pressed dimple beside a small amber window that holds the key's name (never a knob jammed
    # at a frame's edge). The key and dimple stay in the 46 px left end, the window's ends in the margins.
    # The window is the key's state (owner, 2026-10-31: "these need to switch back light on off based on hover/click
    # state"): unlit film and no lamp at rest, backlit amber on hover, brighter with a stronger lamp when pressed. The
    # plate, glass and dimple are the same in every state; only the light changes.
    win = rbox(44, 9, 140, 39, 5)
    for state, film, lamps in (("idle", "dim", []), ("hover", "amber", [(92, 9, 0.05)]), ("pressed", "hot", [(92, 9, 0.08)])):
        add(f"button.{state}", "nine", (150, 48), rbox(0.5, 2.5, 149.5, 45.5, 10), [(film, win)],
            studs=[(22, 24, 9.0, "key")], dimples=[(22, 24, 13.0)], live=[("label", box(48, 15, 136, 33))],
            margins=(20, 16, 20, 46), stretch="x", roll=1.6, dz=-5.0, lamps=lamps, pad=2.5)
    # Tabs are the console's toggles: a plate with a jewel pilot lamp in a dimple at its left (owner, 2026-10-31: "some
    # of the togle buttons for instutues having lights on would be cool") and one long amber window. The selected tab's
    # lamp glows and its window is lit; the others' lamps are dark glass and their windows dim. Same glass in both.
    for state in ("idle", "active"):
        add(f"tab.{state}", "nine", (120, 40), rbox(1, 4, 119, 36, 8), [("amber" if state == "active" else "dim", rbox(28, 10, 112, 30, 5))],
            studs=[(15, 20, 3.6, "jewel_on" if state == "active" else "jewel_off")], dimples=[(15, 20, 5.0)],
            live=[("label", box(32, 13, 106, 27))], margins=(12, 14, 12, 28), stretch="x", roll=1.4, dz=-4.0, pad=2.5,
            lamps=[(70, 10, 0.05)] if state == "active" else [])
    # Strips: a long plate; at its left end a bay holds a small needle meter on amber film (an arched window grown up
    # out of a box), at its right end a knurled knob in a dimple and two screws. The middle is plain metal that stretches.
    # 72 px tall since 2026-10-31 (owner: "we need to make parent taller"): the bay, knob and screws keep their size,
    # moved down 8 px to centre on row 36; the plain middle the readouts sit on grows.
    bay = soft(unary_union([rbox(14, 32, 78, 54, 5), disc(46, 42, 24)]).intersection(box(0, 0, 512, 54)), 4)
    meter = [arc_pts(46, 50, 22, math.radians(205), math.radians(335), 20)] + \
        [[(46 + math.cos(a) * 18, 50 + math.sin(a) * 18), (46 + math.cos(a) * 22, 50 + math.sin(a) * 22)] for a in [math.radians(205 + 13 * i) for i in range(11)]]
    for name in ("strip.top", "strip.bottom"):
        add(name, "nine", (512, 72), rbox(0.5, 0.5, 511.5, 71.5, 12), [("amber", bay)],
            studs=[(468, 36, 12.0, "knurl"), (497, 15, 2.5, "screw"), (497, 57, 2.5, "screw")], dimples=[(468, 36, 15.0)],
            prints=[("scale", meter, 1.0, "ink"), ("needle", [[(46, 50), (46 + math.cos(math.radians(250)) * 20, 50 + math.sin(math.radians(250)) * 20)]], 1.2, "red")],
            margins=(10, 88, 10, 90), stretch="x", roll=2.5, dz=-6.0, lamps=[(46, 18, 0.06)], pad=2.0)
    add("divider", "nine", (128, 10), rbox(0.5, 1.5, 127.5, 8.5, 3.5), [], margins=(0, 12, 0, 12), stretch="x", roll=3.0)
    # Heroes. The name plate: an amber window for the live name, an engraved fork-and-wave mark at its left, a jewel and
    # a domed key in dimples at its right.
    mark = [[(16, 16), (16, 30)] + [(22 + 6 * math.cos(a), 30 + 6 * math.sin(a)) for a in [math.pi - i * math.pi / 8 for i in range(9)]] + [(28, 16)],
            [(22, 36), (22, 44)], [(10 + i, 28 + math.sin(i / 24 * 4 * math.pi) * 4) for i in range(25)]]
    add("nameplate", "hero", (460, 56), rbox(0.5, 0.5, 459.5, 55.5, 12), [("amber", rbox(46, 9, 360, 47, 6))],
        studs=[(386, 28, 5.5, "jewel"), (424, 28, 10.0, "key")], dimples=[(386, 28, 9.0), (424, 28, 15.0)],
        live=[("name", box(54, 12, 344, 44))], engrave=mark, roll=2.5, dz=-8.0, lamps=[(120, 9, 0.1), (280, 9, 0.1)], pad=2.9)
    # The helm: a round phosphor scope in the middle (the live STOP and the scope's graticule), and round it a pressed
    # cradle, one annular channel, in which eight domed keys sit on a ring of radius 92 (the page's helmR).
    keys = [(150 + math.cos(a) * 92, 150 + math.sin(a) * 92, 17.0, "key") for a in [-math.pi / 2 + i * math.pi / 4 for i in range(8)]]
    screws = [(150 + math.cos(a) * 132, 150 + math.sin(a) * 132, 2.5, "screw") for a in [math.pi / 8 + i * math.pi / 2 for i in range(4)]]
    add("helm.pad", "hero", (300, 300), Point(150, 150).buffer(149.5, quad_segs=48), [("phos", disc(150, 150, 50))],
        studs=keys + screws, cradle=(150, 150, 92.0, 24.0, 2.5), prints=[("graticule", graticule(150, 150, 44, [18, 34]), 0.8, "grat")],
        live=[("stop", box(126, 140, 174, 160))], roll=3.5, dz=-10.0)
    # The ship display: one big phosphor screen, an oscilloscope's rounded-rectangle CRT, filling the panel (owner,
    # 2026-10-31: "ship display should take up 90% of the panel or more"): 443 x 283 of 460 x 300, 91% of its area, in a
    # thin rolled rim (roll 2.5 mm, the most rule R allows at this land). The live shield rings fill radius 137.
    scope = rbox(8.5, 8.5, 451.5, 291.5, 20)
    add("ssd.bezel", "hero", (460, 300), rbox(0.5, 0.5, 459.5, 299.5, 24), [("phos", scope)],
        prints=[("graticule", graticule(230, 150, 135, [45, 90, 135], 6), 0.8, "grat")], live=[("rings", disc(230, 150, 137))],
        roll=2.5, dz=-10.0)
    add("dial", "hero", (120, 120), Point(60, 60).buffer(59.5, quad_segs=48), [("phos", disc(60, 60, 51.5))],
        prints=[("graticule", graticule(60, 60, 46, [16, 32]), 0.7, "grat")], live=[("scope", disc(60, 60, 47))], roll=2.2, dz=-8.0)
    for c in ("green", "yellow", "red"):
        add(f"lamp.alert.{c}", "hero", (64, 64), Point(32, 32).buffer(31.5, quad_segs=48), [],
            studs=[(32, 32, 13.0, f"jewel_{c}")], dimples=[(32, 32, 20.0)], roll=2.0, dz=-4.0)
    # The crest: a round plate with a pressed ring channel and the Institute's mark (a tuning fork over a sine wave)
    # inlaid in brass on the flat inside it.
    fork = [[(40, 26), (40, 48)] + [(48 + 8 * math.cos(a), 48 + 8 * math.sin(a)) for a in [math.pi - i * math.pi / 8 for i in range(9)]] + [(56, 26)],
            [(48, 56), (48, 70)], [(28 + i * 1.6, 60 + math.sin(i / 25 * 4 * math.pi) * 5) for i in range(26)]]
    add("crest", "hero", (96, 96), Point(48, 48).buffer(47.5, quad_segs=48), [], cradle=(48, 48, 34.0, 4.0, 1.6), emblem=fork, roll=3.0, dz=-4.0)
    for n, g in GLYPHS.items():
        s = 0.62
        lines = [[(24 + (x - 24) * s, 24 + (y - 24) * s) for x, y in l] for l in g]
        add(f"icon.{n}", "hero", (48, 48), rbox(0.5, 0.5, 47.5, 47.5, 9), [("amber", rbox(8, 8, 40, 40, 6))],
            prints=[("glyph", lines, 1.4, "ink")], roll=2.0, dz=-4.0, lamps=[(24, 8, 0.02)], pad=2.5)
    return P


def check_all(P):
    """Run every piece through the fit rules; return (failures, report lines, films)."""
    bad, lines, films = 0, [], {}
    for name, p in P.items():
        wells = [g for _, g in p["wells"]]
        gap = min([w.exterior.distance(p["plate"].exterior) for w in wells] or [8.0])
        inset = min(INSET, gap / 2)
        f = [behind_layer(w, p["plate"], grow=min(8.0, gap), inset=inset) for w in wells]
        films[name] = f
        prints = [(f"print {role}", lines_geom(ls, w)) for role, ls, w, _ in p["prints"]] + [(f"live {role}", g) for role, g in p["live"]]
        res, _ = fit(p["plate"], wells, knobs=[(x, y, r * (1.0 if st != "screw" else 1.0), None) for x, y, r, st in p["studs"]],
                     prints=prints, behind=[(f"film behind well {i + 1}", g) for i, g in enumerate(f)],
                     dimples=[(x, y, r, r) for x, y, r in p["dimples"]], roll=p["roll"], land=p["land"], inset=inset, pad=p["pad"])
        if p["cradle"]:  # the pressed channel keeps a dimple's distance from every rolled edge (rule R)
            cx, cy, R, half, _d = p["cradle"]
            ring = Point(cx, cy).buffer(R + half, quad_segs=48).difference(Point(cx, cy).buffer(R - half, quad_segs=48))
            need = p["roll"] + DIMPLE_RIM + p["land"]
            d = min([ring.exterior.distance(p["plate"].exterior)] + [ring.distance(w) for w in wells])
            res.append(("R", "the pressed cradle", d >= need, f"{d:.1f} mm to the nearest edge (needs {need:.1f})"))
        if p["emblem"]:  # the inlaid mark sits on the flat inside the cradle
            cx, cy, R, half, _d = p["cradle"]
            g = lines_geom(p["emblem"], 2.4)
            ok = Point(cx, cy).buffer(R - half - 2.0).contains(g)
            res.append(("K", "the inlaid mark", ok, "stays on the flat inside the channel"))
        fails = [r for r in res if not r[2]]
        bad += len(fails)
        lines.append(f"{name}: {len(res) - len(fails)} of {len(res)} pass" + "".join(f"\n  FAIL {r[0]} {r[1]}: {r[3]}" for r in fails))
    return bad, lines, films


def pts(g):
    return [[round(x, 3), round(y, 3)] for x, y in list(g.exterior.coords)[:-1]]


def write(P, films):
    out = {"skin": "kelvane", "pieces": {}}
    for name, p in P.items():
        out["pieces"][name] = dict(kind=p["kind"], size=list(p["size"]), margins=list(p["margins"]) if p["margins"] else None,
                                   stretch=p["stretch"], inset=list(p["inset"]) if p["inset"] else None, roll=p["roll"], dz=p["dz"],
                                   plate=pts(p["plate"]), wells=[[kind, pts(g)] for kind, g in p["wells"]],
                                   films=[pts(f) for f in films[name]], studs=[list(s) for s in p["studs"]],
                                   dimples=[list(d) for d in p["dimples"]], cradle=list(p["cradle"]) if p["cradle"] else None,
                                   prints=[[role, [[[round(x, 3), round(y, 3)] for x, y in l] for l in ls], w, ink] for role, ls, w, ink in p["prints"]],
                                   engrave=[[[round(x, 3), round(y, 3)] for x, y in l] for l in p["engrave"]],
                                   emblem=[[[round(x, 3), round(y, 3)] for x, y in l] for l in p["emblem"]],
                                   live={role: [round(v, 2) for v in g.bounds] for role, g in p["live"]}, lamps=[list(l) for l in p["lamps"]])
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w") as f:
        json.dump(out, f, separators=(",", ":"))
        f.write("\n")


def verify(folder):
    """Rule B on the finished renders: no pixel with any opacity outside the piece's plate footprint (0.75 px of edge
    antialiasing allowed). The bar fills have no plate and are skipped."""
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
