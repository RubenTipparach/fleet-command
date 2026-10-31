"""Fleet Command: the Krai Warhost kit's shapes, fit-checked, as the one source for its render and its page module.

Owns the geometry of every piece of the Krai skin (mockup 4, console-skins), rebuilt on the analog panel bible (owner,
2026-10-31: "we need to update krai, onbrai, and hunt to use the new guildine standards ... Please make them look
nice"): blackened blade steel plates whose edges roll over in a tight honed round, formed wells cut through them (blade
keys drawn to a point, frames whose wells grow fangs at the corners, an octagonal war shield with eight arrowhead key
wells, a hexagonal radar), red enamel inlays standing on the flat metal, honed steel pyramid studs, and the ember
glass film behind every well. The Warhost's design language is in openspec/changes/console-skins/design.md (combat race:
triangles, angry red, orange lit when engaged, blade-like framing). tools/blender/build_ui_krai.py renders exactly these
outlines; docs/mockups/skins/krai.js places its live readouts inside the measured windows recorded here.

Every piece passes tools/mockups/formed_metal.py's fit rules before anything is written (the analog panel bible,
chapter 9): K studs on flat metal, L tips that hold the roll (the Krai roll is a tight 1.6 mm, so its blades stay
sharp-looking while still rounding over), R rolled edges that never meet, P live readouts inside their wells with an
even margin, B films inside the plate. Inlays are checked as raised plates (inside their parent, clear of every opening).
A failure writes nothing and exits 1.

    python3 tools/mockups/krai_shapes.py              # check, then write assets/ui/krai/shapes.json
    python3 tools/mockups/krai_shapes.py --verify assets/ui/krai   # rule B on the renders

Units: slot px = mm, y down, at the 1920 x 1080 reference (renders are at 2x). Needs shapely.
"""
import json
import math
import os
import sys

from shapely import affinity
from shapely.geometry import Point, Polygon, box

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from formed_metal import INSET, behind_layer, disc, fit, soft  # noqa: E402

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "assets", "ui", "krai")

ROLL = 1.6   # the honed edge: tight, so a blade reads sharp, but every edge still rolls over (rule L)


def cut(x0, y0, x1, y1, c):
    """A rectangle with its four corners cut off at 45 degrees by c: the Warhost's blade-cut octagon."""
    return Polygon([(x0 + c, y0), (x1 - c, y0), (x1, y0 + c), (x1, y1 - c), (x1 - c, y1), (x0 + c, y1), (x0, y1 - c), (x0, y0 + c)])


def blade(x0, x1, y0, y1, back, tip):
    """The Blade key's outline (design.md): a slanted back, a flat top and foot, drawn to a point at the right."""
    m = (y0 + y1) / 2
    return Polygon([(x0 + back, y0), (x1 - tip, y0), (x1, m), (x1 - tip, y1), (x0, y1)])


def tri(cx, cy, r, a=-math.pi / 2):
    """An equilateral triangle round (cx, cy), circumradius r, its first point at angle a."""
    return Polygon([(cx + math.cos(a + i * 2 * math.pi / 3) * r, cy + math.sin(a + i * 2 * math.pi / 3) * r) for i in range(3)])


def arrowhead(cx, cy, a, r):
    """One of the helm's eight key wells: an arrowhead pointing outward along a (radians) at radius r from (cx, cy)."""
    k = Polygon([(28, 0), (-16, -22), (-10, 0), (-16, 22)])
    k = affinity.rotate(k, a, origin=(0, 0), use_radians=True)
    return affinity.translate(k, cx + math.cos(a) * r, cy + math.sin(a) * r)


def glyph(cx, cy, a, r):
    """The live helm arrow krai.js draws at each key (its live.arrow outline), placed and turned as the page does."""
    g = Polygon([(12, 0), (-6, -6), (-2, 0), (-6, 6)])
    g = affinity.rotate(g, a, origin=(0, 0), use_radians=True)
    return affinity.translate(g, cx + math.cos(a) * r, cy + math.sin(a) * r)


def pieces():
    """Every piece: kind, size, 9-slice margins (top, right, bottom, left), stretch, the cells' groove inset (bar track),
    the roll and land, the plate, its wells (each with the film it shows: ember, red, forge, lamp colours, slot), its
    pyramid studs, its raised enamel inlays, and the live readouts the page draws (label, geometry) for rule P."""
    P = {}

    def add(name, kind, size, plate, wells, studs=(), inlays=(), live=(), margins=None, stretch=None, roll=ROLL, land=3.0,
            inset=None, dz=-8.0, pad=3.0):
        P[name] = dict(kind=kind, size=size, margins=margins, stretch=stretch, roll=roll, land=land, inset=inset, plate=plate,
                       wells=list(wells), studs=list(studs), inlays=list(inlays), live=list(live), dz=dz, pad=pad)

    # Frames: a blade-cut plate round a blade-cut well, a fang of metal grown into each corner of the well (formed with
    # the plate, the bible's ear in the Warhost's hand), a line of red enamel along the top and bottom lands. Fangs and
    # the cut corners stay inside the 42 px margins, so the edges stretch clean.
    # each fang takes the well's corner and drives a point of metal in toward its centre
    fang = Polygon([(8, 8), (8, 22), (40, 40), (22, 8)])
    fangs = [fang, affinity.scale(fang, -1, 1, origin=(85, 65)), affinity.scale(fang, 1, -1, origin=(85, 65)), affinity.scale(fang, -1, -1, origin=(85, 65))]
    well = cut(13, 13, 157, 117, 4)
    for f in fangs:
        well = well.difference(f)
    add("frame.panel", "nine", (170, 130), soft(cut(0.5, 0.5, 169.5, 129.5, 16), 3.6), [("ember", soft(well, 3.6))],
        inlays=[box(44, 5.2, 126, 8.3), box(44, 121.7, 126, 124.8)], margins=(42, 42, 42, 42), stretch="xy")
    add("frame.inset", "nine", (64, 48), soft(cut(0.5, 0.5, 63.5, 47.5, 6), 3.6), [("ember", soft(cut(7, 7, 57, 41, 5), 3.6))],
        margins=(14, 14, 14, 14), stretch="xy", dz=-5.0)
    # The symbol bar's track: a low steel rail with a dark groove the cells sit in (inset 9, 3: cells 14 px tall).
    add("bar.track", "nine", (96, 20), soft(box(0.5, 0.5, 95.5, 19.5), 1.2), [("slot", soft(box(7, 2.2, 89, 17.8), 1.2))],
        live=[("cells", box(9, 3, 87, 17))], margins=(0, 14, 0, 14), stretch="x", roll=0.6, land=0.4, inset=(9, 3), dz=-2.0, pad=0.4)
    # Keys: the Blade, a well of the same outline in a blade plate. The back and the point stay in the margins.
    key_plate = soft(blade(1, 149, 2, 46, 16, 28), 3.6)
    key_well = soft(blade(12, 136, 9, 39, 12, 20), 3.6)
    for state, film in (("idle", "ember"), ("hover", "red"), ("pressed", "forge")):
        add(f"button.{state}", "nine", (150, 48), key_plate, [(film, key_well)], live=[("label", box(28, 13, 112, 35))],
            margins=(20, 36, 20, 26), stretch="x", dz=-6.0, pad=2.0)
    # Tabs: a parallelogram plate, a slot of light under the label; the active one lit forge orange.
    tab_plate = soft(Polygon([(10, 2), (119, 2), (110, 38), (1, 38)]), 3.6)
    tab_well = soft(Polygon([(16, 9.6), (110, 9.6), (105.4, 30.4), (11.4, 30.4)]), 3.6)
    for state, film in (("idle", "ember"), ("active", "forge")):
        add(f"tab.{state}", "nine", (120, 40), tab_plate, [(film, tab_well)], live=[("label", box(24, 13, 98, 27))],
            margins=(12, 30, 12, 30), stretch="x", dz=-5.0, pad=1.5)
    # The strips: a long blade channel drawn to a point at each end, where a great blade of red enamel lies on the metal
    # and two honed pyramids hold it down.
    # 72 px tall since 2026-10-31 (owner: "we need to make parent taller"): the channel grows 16 px, its points and the
    # blades and pyramids at its ends keep their size, centred on row 36.
    chan = soft(Polygon([(64, 11), (448, 11), (474, 36), (448, 61), (64, 61), (38, 36)]), 3.6)
    for name in ("strip.top", "strip.bottom"):
        add(name, "nine", (512, 72), soft(box(0.5, 0.5, 511.5, 71.5), 3.6), [("ember", chan)],
            studs=[(14, 14, 2.6), (14, 58, 2.6), (498, 14, 2.6), (498, 58, 2.6)],
            inlays=[Polygon([(22, 26), (48, 8), (60, 8)]), Polygon([(22, 46), (48, 64), (60, 64)]),
                    Polygon([(490, 26), (464, 8), (452, 8)]), Polygon([(490, 46), (464, 64), (452, 64)])],
            live=[("readouts", box(90, 14, 422, 58))], margins=(10, 100, 10, 100), stretch="x", dz=-6.0, pad=1.5)
    add("divider", "nine", (128, 10), soft(Polygon([(6, 0.5), (122, 0.5), (127.5, 5), (122, 9.5), (6, 9.5), (0.5, 5)]), 1),
        [("red", soft(box(14, 3.3, 114, 6.7), 1.2))], margins=(0, 16, 0, 16), stretch="x", roll=0.6, land=1.0, dz=-2.0, pad=0.5)
    # Heroes. The name plate: a blade of steel, a rank triangle glowing red at its head, a long field for the name.
    field = soft(Polygon([(80, 7.5), (434, 7.5), (420.3, 48.5), (66.6, 48.5)]), 3.6)
    add("nameplate", "hero", (460, 56), soft(Polygon([(18, 0.5), (459.5, 0.5), (441.5, 55.5), (0.5, 55.5)]), 3.6),
        [("red", soft(Polygon([(26, 10), (62, 10), (44, 44)]), 3.6)), ("ember", field)],
        live=[("name", box(86, 11, 404, 45))], dz=-6.0)
    # The helm: an octagonal war shield; eight arrowhead wells point outward where the page draws the eight keys, a
    # triangle glows red at its heart round the HOLD word, honed pyramids stand between the keys.
    keys = [("ember", soft(arrowhead(150, 150, -math.pi / 2 + i * math.pi / 4, 96), 3.6)) for i in range(8)]
    heart = soft(Polygon([(150, 100), (196, 178), (104, 178)]), 4)
    add("helm.pad", "hero", (300, 300), soft(cut(1, 1, 299, 299, 86), 3.6), keys + [("red", heart)],
        studs=[(150 + math.cos(a) * 134, 150 + math.sin(a) * 134, 3.2) for a in [math.radians(22.5 + 45 * i) for i in range(8)]],
        live=[(f"key {i}", glyph(150, 150, -math.pi / 2 + i * math.pi / 4, 96)) for i in range(8)] + [("hold", box(132, 148, 168, 162))],
        dz=-10.0)
    # The ship display: a blade-cut bezel round a blade-cut well, two spikes of metal driven in from the sides, red
    # enamel blades along the top and bottom lands, a pyramid in each cut corner.
    spikes = [Polygon([(16, 126), (44, 150), (16, 174)]), Polygon([(444, 126), (416, 150), (444, 174)])]
    ssd = cut(16, 16, 444, 284, 26)
    for s in spikes:
        ssd = ssd.difference(s)
    add("ssd.bezel", "hero", (460, 300), soft(cut(0.5, 0.5, 459.5, 299.5, 30), 3.6), [("ember", soft(ssd, 3.6))],
        studs=[(22.5, 22.5, 3.0), (437.5, 22.5, 3.0), (22.5, 277.5, 3.0), (437.5, 277.5, 3.0)],
        inlays=[Polygon([(120, 5.3), (340, 5.3), (330, 10), (130, 10)]), Polygon([(130, 290), (330, 290), (340, 294.7), (120, 294.7)])],
        live=[("ship and shields", disc(230, 150, 112))], dz=-10.0)
    hexa = lambda r: Polygon([(60 + math.cos(i * math.pi / 3) * r, 60 + math.sin(i * math.pi / 3) * r) for i in range(6)])
    add("dial", "hero", (120, 120), soft(hexa(59.5), 3.6), [("ember", soft(hexa(50), 3.6))], live=[("sweep and contacts", disc(60, 60, 40))],
        dz=-8.0)
    # Alert lamps: a steel triangle round a triangle of glowing glass.
    lamp_plate = Polygon([(32, 2), (62, 58), (2, 58)])
    lamp_well = soft(Polygon([(32, 17), (50.5, 51.5), (13.5, 51.5)]), 3.6)
    for c in ("green", "yellow", "red"):
        add(f"lamp.alert.{c}", "hero", (64, 64), soft(lamp_plate, 3.6), [(f"lamp_{c}", lamp_well)], dz=-5.0, pad=2.0)
    # The crest (original): a falling blade of red light in a steel triangle, two enamel fangs beside it.
    add("crest", "hero", (96, 96), soft(Polygon([(48, 3), (93, 89), (3, 89)]), 3.6), [("red", soft(Polygon([(41, 33), (55, 33), (48, 76)]), 3.6))],
        inlays=[Polygon([(25, 62), (33, 62), (31, 80)]), Polygon([(63, 62), (71, 62), (65, 80)])], dz=-6.0)
    return P


# The symbol bar's cells (not plates: small blades of glass the page lays in the track's groove), one outline for all
# three, so lit and unlit differ only in their light.
CELL = dict(size=(10, 14), gap=2, outline=[(0.6, 0.8), (5.4, 0.8), (9.4, 7), (5.4, 13.2), (0.6, 13.2), (4.4, 7)])


def check_all(P):
    """Run every piece through the fit rules; return (failures, report lines, films)."""
    bad, lines, films = 0, [], {}
    for name, p in P.items():
        wells = [w for _k, w in p["wells"]]
        gap = min(w.exterior.distance(p["plate"].exterior) for w in wells)
        inset = min(INSET, gap / 2)
        f = [behind_layer(w, p["plate"], grow=min(6.0, gap), inset=inset) for w in wells]
        films[name] = f
        res, _ = fit(p["plate"], wells, knobs=[(x, y, r, None) for x, y, r in p["studs"]],
                     prints=[(f"live {role}", g) for role, g in p["live"]],
                     behind=[(f"film behind well {i + 1}", g) for i, g in enumerate(f)],
                     raised=[(f"enamel inlay {i + 1}", g) for i, g in enumerate(p["inlays"])],
                     roll=p["roll"], land=p["land"], inset=inset, pad=p["pad"])
        fails = [r for r in res if not r[2]]
        bad += len(fails)
        lines.append(f"{name}: {len(res) - len(fails)} of {len(res)} pass" + "".join(f"\n  FAIL {r[0]} {r[1]}: {r[3]}" for r in fails))
    return bad, lines, films


def pts(g):
    """An outline as points for Blender: simplified within 0.02 px and with no repeated point, because the shared
    offset (ui_shapes.offset_poly) mitres each point, and a zero-length edge sends its offset off to infinity."""
    out = []
    for x, y in list(g.simplify(0.02, preserve_topology=True).exterior.coords)[:-1]:
        p = [round(x, 3), round(y, 3)]
        if not out or abs(p[0] - out[-1][0]) + abs(p[1] - out[-1][1]) > 0.05:
            out.append(p)
    if len(out) > 2 and abs(out[0][0] - out[-1][0]) + abs(out[0][1] - out[-1][1]) <= 0.05:
        out.pop()
    return out


def write(P, films):
    out = {"skin": "krai", "pieces": {}, "cell": dict(size=list(CELL["size"]), gap=CELL["gap"], outline=[list(p) for p in CELL["outline"]])}
    for name, p in P.items():
        out["pieces"][name] = dict(kind=p["kind"], size=list(p["size"]), margins=list(p["margins"]) if p["margins"] else None,
                                   stretch=p["stretch"], inset=list(p["inset"]) if p["inset"] else None, roll=p["roll"], dz=p["dz"],
                                   plate=pts(p["plate"]), wells=[[k, pts(w)] for k, w in p["wells"]], films=[pts(f) for f in films[name]],
                                   studs=[[x, y, r] for x, y, r in p["studs"]], inlays=[pts(g) for g in p["inlays"]],
                                   live=[[role, list(g.bounds)] for role, g in p["live"]])
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "shapes.json"), "w") as f:
        json.dump(out, f, separators=(",", ":"))
        f.write("\n")


def verify(folder):
    """Rule B on the finished renders: no pixel with any opacity outside the piece's plate footprint (1.5 render px of
    edge antialiasing allowed)."""
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
    print("wrote", os.path.relpath(os.path.join(OUT, "shapes.json"), ROOT))


if __name__ == "__main__":
    main()
