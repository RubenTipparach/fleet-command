"""Fleet Command: the Aelith Conclave kit's shapes, fit-checked, as the one source for its render and its holograms.

Owns the geometry of every piece of the Aelith skin (mockup 4, console-skins): the pillowed gold shell (its plate
outline and roll), the formed wells cut through it (lozenges with drawn points, knuckled corners, scalloped rims,
embracing arms), the pearl studs on the metal, the void film behind each well (measured to fit, rule B) and the
hologram shapes the page draws in each well. The Conclave is the one hybrid skin (owner, 2026-10-30: "a hybrid of both
vector graphics for holographic buttons and icons, but 3d rendered shell around the UI panels and elements"):
tools/blender/build_ui_aelith.py renders the shell from this file, docs/mockups/skins/aelith.js draws the holograms
from it, so the two cannot disagree (CLAUDE.md section 6.1).

Every piece passes tools/mockups/formed_metal.py's fit rules before anything is written (the analog panel bible,
chapter 9): K studs on flat metal, L tips that hold the roll, R rolled edges that never meet (flat land between
every two: 3 px on most pieces, 0.3 px on the 16 px bar track and 1 px on the divider, the most their height allows), P holograms inside their wells with an even
margin (3 px on the heroes, down to 0.6 px on the 10 and 16 px bars), B films inside the shell (4 px in, or half the
land on pieces too thin for that). A failure writes nothing and exits 1.

    python3 tools/mockups/aelith_shapes.py              # check, then write assets/ui/aelith/shapes.json
    python3 tools/mockups/aelith_shapes.py --verify assets/ui/aelith   # rule B on the renders: no lit pixel
                                                                        # outside the shell's footprint

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
OUT = os.path.join(ROOT, "assets", "ui", "aelith", "shapes.json")


def lozenge(x0, x1, y0, y1, tip):
    """A pointed lozenge: flat top and bottom from x0 + tip to x1 - tip, drawn to a point at each end."""
    m = (y0 + y1) / 2
    return Polygon([(x0, m), (x0 + tip, y0), (x1 - tip, y0), (x1, m), (x1 - tip, y1), (x0 + tip, y1)])


def key(cx, cy, a, r=98.0):
    """One of the helm's eight hologram keys: a lozenge pointing outward at angle a (radians) round (cx, cy)."""
    k = Polygon([(-26, 0), (-12, -16), (4, -15), (26, 0), (4, 15), (-12, 16)])
    k = affinity.rotate(k, a, origin=(0, 0), use_radians=True)
    return affinity.translate(k, cx + math.cos(a) * r, cy + math.sin(a) * r)


def pieces():
    """Every piece: kind, size, 9-slice margins (top, right, bottom, left), stretch, the fill inset (bars), the roll and
    land the fit rule uses, the plate, its wells, its pearl studs and its holograms (role, geometry)."""
    P = {}

    def add(name, kind, size, plate, wells, studs=(), holo=(), margins=None, stretch=None, roll=3.0, land=3.0, inset=None, dz=None, pad=3.0):
        P[name] = dict(kind=kind, size=size, margins=margins, stretch=stretch, roll=roll, land=land, inset=inset, plate=plate,
                       wells=wells, studs=list(studs), holo=list(holo), dz=dz, pad=pad)

    # Frames: a gold shell, its well's corners filled by knuckles of armour (the "ear" grown into each corner), a pearl
    # set in every knuckle. Knuckles and pearls stay inside the 44 px corners, so the edges stretch clean.
    knuckles = unary_union([disc(x, y, 17) for x in (24, 156) for y in (24, 116)])
    add("frame.panel", "nine", (180, 140), rbox(0.5, 0.5, 179.5, 139.5, 22), [soft(rbox(16, 16, 164, 124, 10).difference(knuckles), 6)],
        studs=[(x, y, 5.5) for x in (24, 156) for y in (24, 116)], margins=(44, 44, 44, 44), stretch="xy")
    small = unary_union([disc(x, y, 5) for x in (8, 64) for y in (8, 48)])
    add("frame.inset", "nine", (72, 56), rbox(0.5, 0.5, 71.5, 55.5, 12), [soft(rbox(7, 7, 65, 49, 6).difference(small), 2.5)],
        margins=(14, 14, 14, 14), stretch="xy", roll=1.6)
    # The bar track: a capsule of gold with a channel the light fills; the fill sits at inset (16, 3).
    add("bar.track", "nine", (96, 16), rbox(0.5, 0.5, 95.5, 15.5, 7.5), [rbox(8, 2.2, 88, 13.8, 5.8)],
        holo=[("fill-slot", box(16, 3, 80, 13))], margins=(0, 14, 0, 14), stretch="x", roll=0.65, land=0.3, inset=(16, 3), dz=-2.5, pad=0.6)
    # Keys: a pointed lozenge of light held between two gold claws, a pearl in each claw. The points and claws stay in
    # the 42 px ends, so the middle stretches.
    btn = soft(lozenge(22, 128, 10, 38, 14), 4)
    for state in ("idle", "hover", "pressed"):
        add(f"button.{state}", "nine", (150, 48), rbox(1, 2, 149, 46, 22), [btn], studs=[(11, 24, 3.5), (139, 24, 3.5)],
            holo=[("key", btn.buffer(-2, quad_segs=8))], margins=(20, 42, 20, 42), stretch="x", roll=2.0, dz=-6.0, pad=1.8)
    # the window is as tall as rule R allows (land 3 px to the plate), so a label clears its lit rim (console fit check)
    tab = soft(rbox(14, 10.3, 106, 29.7, 9.7), 3)
    for state in ("idle", "active"):
        add(f"tab.{state}", "nine", (120, 40), rbox(1, 4, 119, 36, 16), [tab], holo=[("tab", tab.buffer(-1.5, quad_segs=8))],
            margins=(12, 30, 12, 30), stretch="x", roll=1.6, dz=-5.0, pad=1.4)
    # The strips: a long channel the readouts sit in, drawn to a point at each end where a swept gold wing holds a pearl.
    chan = soft(lozenge(60, 452, 9, 47, 30), 6)
    for name in ("strip.top", "strip.bottom"):
        add(name, "nine", (512, 56), rbox(0.5, 0.5, 511.5, 55.5, 14), [chan], studs=[(30, 28, 7), (482, 28, 7)],
            holo=[("edge", chan.buffer(-1.5, quad_segs=8))], margins=(10, 110, 10, 110), stretch="x", roll=2.4, dz=-6.0, pad=1.4)
    add("divider", "nine", (128, 10), rbox(0.5, 0.5, 127.5, 9.5, 4.5), [rbox(10, 3, 118, 7, 2)],
        holo=[("line", box(13, 4.2, 115, 5.8))], margins=(0, 16, 0, 16), stretch="x", roll=0.6, land=1.0, dz=-2.0, pad=1.0)
    # Heroes. The name plate: a lozenge window for the live name, a pearl at each end.
    plate_win = soft(Polygon([(36, 28), (58, 7), (402, 7), (424, 28), (402, 49), (58, 49)]), 8)
    add("nameplate", "hero", (460, 56), rbox(0.5, 0.5, 459.5, 55.5, 20), [plate_win], studs=[(18, 28, 8), (442, 28, 8)],
        holo=[("field", plate_win.buffer(-2, quad_segs=8)), ("text", box(64, 13, 396, 43))], roll=1.7, dz=-8.0, pad=1.8)
    # The helm: a disc of light whose rim is scalloped by eight armour lobes, a pearl in each, between the eight keys.
    lobes = unary_union([disc(150 + math.cos(a) * 140, 150 + math.sin(a) * 140, 17) for a in [math.radians(22.5 + 45 * i) for i in range(8)]])
    helm = soft(disc(150, 150, 134).difference(lobes), 5)
    keys = [(f"key {i}", key(150, 150, -math.pi / 2 + i * math.pi / 4)) for i in range(8)]
    add("helm.pad", "hero", (300, 300), Point(150, 150).buffer(149.5, quad_segs=32), [helm],
        studs=[(150 + math.cos(a) * 136, 150 + math.sin(a) * 136, 4.5) for a in [math.radians(22.5 + 45 * i) for i in range(8)]],
        holo=keys + [("crystal", Polygon([(150, 110), (180, 150), (150, 190), (120, 150)]))], dz=-10.0)
    # The ship display: two arms of armour embrace the field from the sides, a pearl in each; the field holds the live
    # ship and its shield rings (radius 118 with their glow).
    arms = unary_union([disc(22, 150, 34), box(0, 116, 22, 184), disc(438, 150, 34), box(438, 116, 460, 184)])
    ssd = soft(rbox(22, 16, 438, 284, 30).difference(arms), 8)
    add("ssd.bezel", "hero", (460, 300), rbox(0.5, 0.5, 459.5, 299.5, 40), [ssd], studs=[(24, 150, 7), (436, 150, 7)],
        holo=[("rings", disc(230, 150, 118))], dz=-10.0)
    add("dial", "hero", (120, 120), Point(60, 60).buffer(59.5, quad_segs=32), [disc(60, 60, 49)], holo=[("scope", disc(60, 60, 45))],
        roll=2.0, dz=-8.0)
    gem = soft(Polygon([(32, 8), (48, 30), (32, 54), (16, 30)]), 4)
    for c in ("green", "yellow", "red"):
        add(f"lamp.alert.{c}", "hero", (64, 64), Point(32, 32).buffer(31.5, quad_segs=32), [gem], holo=[("crystal", gem.buffer(-2.5, quad_segs=8))],
            roll=2.0, dz=-6.0, pad=2.0)
    # The crest: a disc of gold over swept wings, a round well holding a triangle of light round a crystal.
    wings = Polygon([(4, 56), (48, 94), (92, 56), (80, 56), (48, 80), (16, 56)])
    crest = soft(unary_union([disc(48, 44, 40), wings]), 4)
    tri = Polygon([(48 + math.cos(a) * 19, 46 + math.sin(a) * 19) for a in [-math.pi / 2 + i * 2 * math.pi / 3 for i in range(3)]])
    add("crest", "hero", (96, 96), crest, [disc(48, 44, 26)],
        holo=[("triangle", tri), ("crystal", Polygon([(48, 34), (55, 45), (48, 56), (41, 45)]))], roll=2.0, dz=-8.0)
    return P


def check_all(P):
    """Run every piece through the fit rules; return (failures, report lines, films)."""
    bad, lines, films = 0, [], {}
    for name, p in P.items():
        gap = min(w.exterior.distance(p["plate"].exterior) for w in p["wells"])
        inset = min(INSET, gap / 2)
        f = [behind_layer(w, p["plate"], grow=min(8.0, gap), inset=inset) for w in p["wells"]]
        films[name] = f
        res, _ = fit(p["plate"], p["wells"], knobs=[(x, y, r, None) for x, y, r in p["studs"]],
                     prints=[(f"hologram {role}", g) for role, g in p["holo"]],
                     behind=[(f"film behind well {i + 1}", g) for i, g in enumerate(f)], roll=p["roll"], land=p["land"], inset=inset,
                     pad=p["pad"])
        fails = [r for r in res if not r[2]]
        bad += len(fails)
        lines.append(f"{name}: {len(res) - len(fails)} of {len(res)} pass" + "".join(f"\n  FAIL {r[0]} {r[1]}: {r[3]}" for r in fails))
    return bad, lines, films


def pts(g):
    return [[round(x, 3), round(y, 3)] for x, y in list(g.exterior.coords)[:-1]]


def write(P, films):
    out = {"skin": "aelith", "pieces": {}}
    for name, p in P.items():
        dz = p["dz"] if p["dz"] is not None else -10.0
        out["pieces"][name] = dict(kind=p["kind"], size=list(p["size"]), margins=list(p["margins"]) if p["margins"] else None,
                                   stretch=p["stretch"], inset=list(p["inset"]) if p["inset"] else None, roll=p["roll"], dz=dz,
                                   plate=pts(p["plate"]), wells=[pts(w) for w in p["wells"]], films=[pts(f) for f in films[name]],
                                   studs=[[x, y, r] for x, y, r in p["studs"]], holo=[[role, pts(g)] for role, g in p["holo"]])
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
        print(f"{'PASS' if not stray else 'FAIL'} B {name}: {stray} lit px outside the shell")
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
