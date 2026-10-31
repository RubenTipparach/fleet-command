"""Fleet Command: the Ombrai kit's shapes, fit-checked, as the one source for its render and its live drawing.

Owns the geometry of every piece of the Ombrai skin (mockup 4, console-skins), rebuilt on the analog panel bible
(owner, 2026-10-31: "we need to update krai, onbrai, and hunt to use the new guildine standards ... Please make them
look nice"): pillowed plates of anodised plum metal whose every edge rolls over, with formed wells cut through them
in the Ombrai's own shapes (crescent-bitten keys, petal wells round the helm's pupil, wells whose corners are bitten
like a waning moon, an eye-shaped ship display under two pressed lids), lilac-rimmed eye jewels set on flat metal,
the violet film behind each well, and the eyes printed on the film (iris rings and a slit pupil), lit or unlit.
tools/blender/build_ui_ombrai.py renders exactly these outlines; docs/mockups/skins/ombrai.js draws the live readouts
inside the same wells, so the two cannot disagree (CLAUDE.md section 6.1).

Every piece passes tools/mockups/formed_metal.py's fit rules before anything is written (the analog panel bible,
chapter 9): K jewels on flat metal, L tips that hold the roll, R rolled edges that never meet, P every printed eye
inside its well with an even margin, B films inside the plate. A failure writes nothing and exits 1.

    python3 tools/mockups/ombrai_shapes.py              # check, then write assets/ui/ombrai/shapes.json
    python3 tools/mockups/ombrai_shapes.py --verify assets/ui/ombrai   # rule B on the renders

States of one piece (a key idle, hovered and pressed; a tab; an unlit and a lit bar cell) share one outline, one
film and one print: only their light level differs (owner, 2026-10-31, on the Tarsine keys: "sync up dust with out
and unlit button to give it that illusion").

Units: slot px = mm, y down, at the 1920 x 1080 reference (renders are at 2x). Needs shapely.
"""
import json
import math
import os
import sys

from shapely import affinity
from shapely.geometry import Point, Polygon, box
from shapely.ops import unary_union

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from formed_metal import INSET, behind_layer, disc, fit, rbox, soft  # noqa: E402

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "assets", "ui", "ombrai", "shapes.json")


def almond(cx, cy, rx, ry):
    """An eye outline: the lens where two discs overlap, rx wide and ry tall from its centre, points left and right."""
    r = (rx * rx + ry * ry) / (2 * ry)
    return disc(cx, cy + r - ry, r).intersection(disc(cx, cy - r + ry, r))


def petal(cx, cy, a, r, length, width):
    """A helm petal: an almond length long and width wide, its long axis pointing out from (cx, cy) at angle a, its
    middle r from the centre."""
    p = almond(0, 0, length / 2, width / 2)
    p = affinity.rotate(p, a, origin=(0, 0), use_radians=True)
    return affinity.translate(p, cx + math.cos(a) * r, cy + math.sin(a) * r)


def bitten(x0, y0, x1, y1, r, bite):
    """A rounded well whose four corners are bitten like a waning moon: a disc of radius bite centred on each corner."""
    w = rbox(x0, y0, x1, y1, r)
    return w.difference(unary_union([disc(x, y, bite) for x in (x0, x1) for y in (y0, y1)]))


def eye_print(cx, cy, rx, ry):
    """What is printed for one eye on its film: the almond outline, the iris ring and the slit pupil, as (role,
    geometry, width). The page's cells and the render draw the same lines."""
    iris = ry * 0.82
    return [("lid", almond(cx, cy, rx, ry).exterior, 1.0), ("iris", Point(cx, cy).buffer(iris).exterior, 1.1),
            ("pupil", affinity.rotate(almond(cx, cy, iris * 0.78, iris * 0.24), 90, origin=(cx, cy)).exterior, 0.9)]


def pieces():
    """Every piece: kind, size, 9-slice margins, stretch, the inset (bars), the roll and land the fit rule uses, the
    plate, its wells (each with its film kind), jewels (x, y, r), printed eyes, the light level and the cell gap."""
    P = {}

    def add(name, kind, size, plate, wells, jewels=(), eyes=(), margins=None, stretch=None, roll=3.0, land=3.0,
            inset=None, dz=-8.0, pad=2.0, level=0.0, gap=None, lamps=True):
        P[name] = dict(kind=kind, size=size, margins=margins, stretch=stretch, roll=roll, land=land, inset=inset,
                       plate=plate, wells=wells, jewels=list(jewels), eyes=list(eyes), dz=dz, pad=pad, level=level,
                       gap=gap, lamps=lamps)

    # Frames: a plum plate, its well's corners bitten like a waning moon; an eye jewel in each corner's metal.
    add("frame.panel", "nine", (160, 120), rbox(0.5, 0.5, 159.5, 119.5, 24), [("void", soft(bitten(18, 18, 142, 102, 12, 13), 3))],
        jewels=[(x, y, 3.6) for x in (14.5, 145.5) for y in (14.5, 105.5)], margins=(36, 36, 36, 36), stretch="xy")
    add("frame.inset", "nine", (64, 48), rbox(0.5, 0.5, 63.5, 47.5, 12), [("void", rbox(7, 7, 57, 41, 7))],
        margins=(14, 14, 14, 14), stretch="xy", roll=1.6, land=2.0, dz=-5.0)
    # The symbol bar: a slim plum rail round a dark groove; the eye cells sit in the groove at inset (8, 3).
    add("bar.track", "nine", (96, 20), rbox(0.5, 0.5, 95.5, 19.5, 9.5), [("void", rbox(5.5, 2.75, 90.5, 17.25, 7.25))],
        margins=(0, 14, 0, 14), stretch="x", roll=0.8, land=0.6, inset=(8, 3), dz=-2.5, pad=0.6, lamps=False)
    # One eye cell: a lidded almond of plum metal round an eye of film. Unlit, lit and warning share it all.
    eye_well = almond(8, 7, 6.6, 4.6)
    for state, lv in (("off", 0.0), ("on", 1.0), ("warn", 1.0)):
        add(f"bar.cell.{state}", "cell", (16, 14), almond(8, 7, 7.9, 6.6), [("eye", eye_well)], eyes=[(8, 7, 5.0, 3.3)],
            roll=0.45, land=0.3, dz=-1.6, pad=0.3, level=lv, gap=2, lamps=False)
    # Keys: a capsule of plum metal; its well, a crescent bar, bitten concave at the left where a moon jewel rests,
    # round at the right. The bite and the moon stay in the 46 px left end, the round end in the 26 px right end.
    key = soft(rbox(30, 9.4, 140, 38.6, 14.6).difference(disc(22, 24, 15)), 2.5)
    for state, lv in (("idle", 0.12), ("hover", 0.5), ("pressed", 1.0)):
        add(f"button.{state}", "nine", (150, 48), rbox(1, 2, 149, 46, 22), [("key", key)], jewels=[(14, 24, 4.2)],
            margins=(0, 26, 0, 46), stretch="x", roll=2.0, land=3.0, dz=-6.0, pad=1.6, level=lv)
    tab = rbox(13, 10.5, 107, 29.5, 9.5)
    for state, lv in (("idle", 0.1), ("active", 0.75)):
        add(f"tab.{state}", "nine", (120, 40), rbox(1, 4, 119, 36, 16), [("key", tab)], margins=(0, 26, 0, 26),
            stretch="x", roll=1.6, land=3.0, dz=-5.0, pad=1.4, level=lv)
    # The strips: a long channel the readouts sit in, and at each end a round bay holding a watching eye.
    chan = rbox(64, 9, 448, 47, 19)
    for name in ("strip.top", "strip.bottom"):
        add(name, "nine", (512, 56), rbox(0.5, 0.5, 511.5, 55.5, 20), [("void", chan), ("eye", disc(31, 28, 17)), ("eye", disc(481, 28, 17))],
            eyes=[(31, 28, 12, 8.5), (481, 28, 12, 8.5)], margins=(0, 76, 0, 76), stretch="x", roll=2.4, land=3.0, dz=-6.0, pad=1.4, level=0.7)
    add("divider", "nine", (128, 10), rbox(0.5, 0.5, 127.5, 9.5, 4.5), [("line", rbox(12, 3, 116, 7, 2))],
        margins=(0, 16, 0, 16), stretch="x", roll=0.6, land=1.0, dz=-2.0, pad=0.5, level=0.6, lamps=False)
    # Heroes. The name plate: a capsule with a round eye bay at its head and a long field for the live name.
    add("nameplate", "hero", (460, 56), rbox(0.5, 0.5, 459.5, 55.5, 26),
        [("eye", disc(30, 28, 19)), ("void", rbox(62, 9, 446, 47, 19))], eyes=[(30, 28, 13.5, 9.5)],
        roll=2.0, land=3.0, dz=-7.0, pad=1.8, level=0.8)
    # The helm: an iris of eight petal wells round a round pupil well (the live comets ride the petals at radius 98).
    petals = [petal(150, 150, -math.pi / 2 + i * math.pi / 4, 98, 64, 40) for i in range(8)]
    add("helm.pad", "hero", (300, 300), Point(150, 150).buffer(149.5, quad_segs=32),
        [("eye", disc(150, 150, 50))] + [("void", p) for p in petals],
        jewels=[(150 + math.cos(a) * 128, 150 + math.sin(a) * 128, 4.2) for a in [math.radians(22.5 + 45 * i) for i in range(8)]],
        eyes=[(150, 150, 40, 26)], roll=2.6, land=3.0, dz=-10.0, pad=3.0, level=0.8)
    # The ship display: an eye-shaped field under two pressed lids (lobes of plate reaching in at top and bottom
    # centre); the live ship and its shield rings (radius 100, glow to 108) keep 3 mm clear of them.
    lids = unary_union([affinity.scale(Point(230, y).buffer(1, quad_segs=32), 96, 22) for y in (4, 296)])
    ssd = soft(rbox(20, 14, 440, 286, 58).difference(lids), 6)
    add("ssd.bezel", "hero", (460, 300), rbox(0.5, 0.5, 459.5, 299.5, 44), [("void", ssd)],
        jewels=[(22, 22, 4.5), (438, 22, 4.5), (22, 278, 4.5), (438, 278, 4.5)], roll=2.8, land=3.0, dz=-10.0, pad=3.0)
    add("dial", "hero", (120, 120), Point(60, 60).buffer(59.5, quad_segs=32), [("void", disc(60, 60, 49))], roll=2.0, dz=-8.0)
    # Alert lamps: a moon of light in a round well (the page draws nothing over them).
    for c in ("green", "yellow", "red"):
        add(f"lamp.alert.{c}", "hero", (64, 64), Point(32, 32).buffer(31.5, quad_segs=32), [(f"moon_{c}", disc(32, 32, 21))],
            roll=2.0, dz=-6.0, pad=2.0, level=1.0)
        # the moon's shadowed part, printed dark on its film: what is left of the moon is a waxing crescent
        P[f"lamp.alert.{c}"]["shade"] = disc(41, 26, 17).intersection(disc(32, 32, 18.5))
    # The crest: a crescent moon cradling a raised boss whose well holds an open eye.
    cres = disc(48, 54, 38).difference(disc(48, 38, 33))
    crest = soft(unary_union([cres, disc(48, 42, 24)]), 3)
    add("crest", "hero", (96, 96), crest, [("eye", disc(48, 42, 15))], eyes=[(48, 42, 11, 7.5)], roll=2.0, dz=-8.0, pad=1.5, level=0.9)
    return P


def prints(p):
    """The printed eyes as (label, geometry) for rule P: each eye's almond grown by half its line width."""
    return [(f"eye at ({cx:g}, {cy:g})", almond(cx, cy, rx, ry).buffer(0.6)) for cx, cy, rx, ry in p["eyes"]]


def check_all(P):
    """Run every piece through the fit rules; return (failures, report lines, films)."""
    bad, lines, films = 0, [], {}
    for name, p in P.items():
        wells = [w for _k, w in p["wells"]]
        gap = min(w.exterior.distance(p["plate"].exterior) for w in wells)
        inset = min(INSET, gap / 2)
        f = [behind_layer(w, p["plate"], grow=min(8.0, gap), inset=inset) for w in wells]
        films[name] = f
        res, _ = fit(p["plate"], wells, knobs=[(x, y, r, None) for x, y, r in p["jewels"]], prints=prints(p),
                     behind=[(f"film behind well {i + 1}", g) for i, g in enumerate(f)], roll=p["roll"], land=p["land"],
                     inset=inset, pad=p["pad"])
        fails = [r for r in res if not r[2]]
        bad += len(fails)
        lines.append(f"{name}: {len(res) - len(fails)} of {len(res)} pass" + "".join(f"\n  FAIL {r[0]} {r[1]}: {r[3]}" for r in fails))
    return bad, lines, films


def pts(g):
    """An outline's points, without the near-duplicates a buffer leaves (they trip the bezel's offset in Blender)."""
    g = getattr(g, "exterior", g).simplify(0.03, preserve_topology=True)
    out = []
    for x, y in list(g.coords)[:-1]:
        if not out or abs(x - out[-1][0]) + abs(y - out[-1][1]) > 0.05:
            out.append([round(x, 3), round(y, 3)])
    return out


def write(P, films):
    out = {"skin": "ombrai", "pieces": {}}
    for name, p in P.items():
        out["pieces"][name] = dict(kind=p["kind"], size=list(p["size"]), margins=list(p["margins"]) if p["margins"] else None,
                                   stretch=p["stretch"], inset=list(p["inset"]) if p["inset"] else None, roll=p["roll"], dz=p["dz"],
                                   level=p["level"], gap=p["gap"], lamps=p["lamps"], plate=pts(p["plate"]),
                                   wells=[[k, pts(w)] for k, w in p["wells"]], films=[pts(f) for f in films[name]],
                                   jewels=[[x, y, r] for x, y, r in p["jewels"]],
                                   eyes=[[[role, pts(g), wd] for role, g, wd in eye_print(*e)] for e in p["eyes"]],
                                   shade=pts(p["shade"]) if p.get("shade") is not None else None)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w") as f:
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
    print("wrote", os.path.relpath(OUT, ROOT))


if __name__ == "__main__":
    main()
