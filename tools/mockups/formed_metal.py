"""Fleet Command: the formed-metal catalogue as geometry, with the fit rules checked before anything is rendered.

Owns the outlines of the analog panel bible's formed-metal shapes (docs/design/ui-bible.html, chapters 8 and 9) and
the fit check every formed shape must pass before it is rendered (owner, 2026-10-31: "implement these as 3d renderings
and verify the overlap rule"). It lives in tools/mockups because it serves the bible; the Blender side that renders
the tiles is tools/blender/ui_tiles_tail.py.

Each tile is a 320 x 220 px plate (1 px = 1 mm) with display windows cut through it. Outlines are built as smoothed
solids (shapely), then checked:

  K  every knob's footprint (radius x 1.14 for its collar, plus MARGIN) lies wholly on FLAT metal: inside the plate,
     clear of every rolled edge (ROLL from any edge)
  L  every metal tip is rounder than the roll: the metal, opened by ROLL, differs from itself by at most 0.6 mm (else
     the roll pinches or eats it)
  R  rolled edges never touch or overlap (owner, 2026-10-31: "Bevels should never intersect", "You must measure elements
     like this carefully", "Avoid two rolled edges from touching or overlap"). Measured, never eyeballed: a window or
     slot is 2 x ROLL + LAND (10.6 mm) from its plate's edge and from every other window; a pressed dimple is ROLL +
     DIMPLE_RIM + LAND (8.2 mm) from the plate's edge, a window or a raised plate; a raised plate stays ROLL + LAND
     (6.8 mm) inside its parent's edge and clear of its openings
  P  anything printed on a display (a gauge) stays PAD inside its window
  B  every layer behind the plate (a film, a well, a glow plane) stays INSET inside the plate's footprint, rounded
     corners included (owner, 2026-10-31: "Some times the UI layer/plane can stick outside its bounds. It must
     measured properly to fit inside the parent panel"); behind_layer() builds one already clamped

Writes tiles.json for the Blender renderer and check_<name>.png maps (flat metal, rolls, footprints, pass or fail)
into --out, and exits with an error if any rule fails. --selftest feeds the checker four known faults (the first
drawings' mistakes) and fails if one slips through. Needs shapely (pip install shapely).

    python3 tools/mockups/formed_metal.py --out <folder>
    python3 tools/mockups/formed_metal.py --selftest

Units: mm, with 1 px of a 320 x 220 tile = 1 mm, y down.
"""
import argparse
import json
import math
import os
import sys

from PIL import Image, ImageDraw
from shapely import affinity
from shapely.geometry import LineString, Point, Polygon, box
from shapely.ops import unary_union

ROLL, LAND, MARGIN, PAD, F, INSET, DIMPLE_RIM = 3.8, 3.0, 2.5, 3.0, 10.0, 4.0, 1.4
W, H = 320, 220


def rbox(x0, y0, x1, y1, r):
    return box(x0 + r, y0 + r, x1 - r, y1 - r).buffer(r, quad_segs=16) if r else box(x0, y0, x1, y1)


def rbox4(x0, y0, x1, y1, tl, tr, br, bl):
    """A rectangle with its own radius at each corner (top left, top right, bottom right, bottom left)."""
    pts = []
    for (cx, cy, r, a0) in ((x0 + tl, y0 + tl, tl, 180), (x1 - tr, y0 + tr, tr, 270), (x1 - br, y1 - br, br, 0), (x0 + bl, y1 - bl, bl, 90)):
        for k in range(17):
            a = math.radians(a0 + 90 * k / 16)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return Polygon(pts)


def soft(p, f=F):
    """Round every corner of a window, convex and concave, to radius f: a pressed shape, never a cut one."""
    return p.buffer(f, quad_segs=16).buffer(-2 * f, quad_segs=16).buffer(f, quad_segs=16)


def disc(x, y, r):
    return Point(x, y).buffer(r, quad_segs=24)


PLATE = rbox(6, 6, W - 6, H - 6, 14)


def tiles():
    t = {}
    # 1 NOTCH: the display's bottom edge rises in a smooth bump; the metal flows up into it to carry two knobs
    notch = Polygon([(78, 172), (112, 118), (208, 118), (242, 172)])
    t["notch"] = dict(windows=[soft(rbox(22, 22, 298, 160, 8).difference(notch))],
                      knobs=[(135, 142, 11, -125), (185, 142, 9, -90)])
    # 2 BRIDGE: a bar of plate spans the display top to bottom, flaring into the frame at both ends; keys ride it
    t["bridge"] = dict(windows=[rbox4(22, 22, 140, 166, 8, 34, 34, 8), rbox4(180, 22, 298, 166, 34, 8, 8, 34)],
                       knobs=[(160, 66, 10, None), (160, 122, 10, None)])
    # 3 EAR: a round lobe grows in from the right edge to hold one dial; the display wraps round it
    ear = unary_union([disc(256, 91, 24), box(256, 67, 310, 115)])
    t["ear"] = dict(windows=[soft(rbox(22, 22, 298, 160, 8).difference(ear))], knobs=[(256, 91, 11, -125)])
    # 4 SADDLE: the land between two displays swells into a round boss that carries a knob
    boss = disc(160, 91, 26)
    t["saddle"] = dict(windows=[soft(rbox(22, 22, 148, 160, 8).difference(boss)), soft(rbox(172, 22, 298, 160, 8).difference(boss))],
                       knobs=[(160, 91, 11, None)])
    # 5 SCALLOP: the bottom edge is bitten by round bays, each a seat for a small key
    bites = unary_union([disc(x, 162, 21) for x in (70, 130, 190, 250)])
    t["scallop"] = dict(windows=[soft(rbox(22, 22, 298, 160, 8).difference(bites), 8)],
                        knobs=[(x, 160, 7, None) for x in (70, 130, 190, 250)])
    # 6 CRADLE: a round meter window hangs below a strip display; the metal cups it, a knob on each shoulder
    t["cradle"] = dict(windows=[soft(unary_union([rbox(22, 22, 298, 76, 8), disc(160, 124, 50)]), 12)],
                       knobs=[(76, 140, 11, -125), (244, 140, 11, -55)], gauges=[(160, 124, 36)])
    # 7 PENINSULA: a tongue of metal reaches up into the display from below, carrying a column of keys
    tongue = unary_union([disc(160, 74, 25), box(135, 74, 185, 230)])
    t["peninsula"] = dict(windows=[soft(rbox(22, 22, 298, 168, 8).difference(tongue))],
                          knobs=[(160, 80, 8, None), (160, 118, 8, None), (160, 156, 8, None)])
    # 8 TERRACE: the bottom edge climbs in soft steps; each shelf holds a key
    steps = Polygon([(112, 230), (112, 136), (198, 136), (198, 102), (310, 102), (310, 230)])
    t["terrace"] = dict(windows=[soft(rbox(22, 22, 298, 168, 8).difference(steps))],
                        knobs=[(68, 192, 9, None), (156, 160, 9, None), (254, 134, 9, None)])
    # 9 BAY: the top edge bulges out into the metal to seat a round gauge; the gauge is printed on the display
    t["bay"] = dict(windows=[soft(unary_union([rbox(22, 62, 298, 170, 8), disc(212, 76, 40)]), 12)],
                    knobs=[(70, 36, 9, None), (120, 36, 9, None)], gauges=[(212, 82, 30)])
    # 10 SWEPT EDGE: the top edge sweeps down across the display and hooks back up; a knob rides the high metal
    top = [(x, 34 + 70 * (lambda u: u * u * (3 - 2 * u))(min(1.0, max(0.0, (x - 40) / 190)))) for x in range(22, 241, 4)]
    swept = Polygon(top + [(262, 104), (298, 34), (298, 170), (22, 170)])
    t["swept"] = dict(windows=[soft(swept, 12)], knobs=[(196, 46, 10, -125), (246, 44, 7, None)])
    return t


def fit(plate, windows, knobs=(), prints=(), behind=(), roll=ROLL, land=LAND, margin=MARGIN, pad=PAD, inset=INSET,
        dimples=(), raised=()):
    """The fit rules for any plate (a shapely polygon) and its windows; returns (results, flat metal).

    knobs are (x, y, r, pointer) on the metal, prints are (label, geometry) drawn on a display, behind are (label,
    geometry) layers under the plate (film, well, glow plane), dimples are (x, y, rx, ry) pressed into it, raised are
    (label, geometry) plates standing on it. Each result is (rule, what, passed, why). Everything is measured on the
    geometry, never judged by eye."""
    holes = unary_union(windows) if windows else Polygon()
    metal = plate.difference(holes)
    flat = plate.buffer(-roll).difference(holes.buffer(roll))
    two = 2 * roll + land
    out = []
    for (x, y, r, _p) in knobs:
        foot = disc(x, y, r * 1.14 + margin)
        out.append(("K", f"knob at ({x:g}, {y:g}) r {r:g} mm", flat.contains(foot), f"needs {r * 1.14 + margin:.1f} mm of flat round it"))
    for what, g in prints:
        ok = any(w.buffer(-pad).contains(g) for w in windows)
        out.append(("P", what, ok, f"stays {pad:g} mm inside its window"))
    for what, g in behind:
        ok = plate.buffer(-inset, quad_segs=16).buffer(0.05).contains(g)
        out.append(("B", what, ok, f"stays {inset:g} mm inside the plate, corners included"))
    opened = metal.buffer(-roll, quad_segs=16).buffer(roll, quad_segs=16)
    dev = metal.hausdorff_distance(opened)
    out.append(("L", "every metal tip holds its roll", dev <= 0.6, f"metal changes by {dev:.2f} mm when opened by the roll {roll:g} mm"))
    # R: rolled edges never touch or overlap (owner, 2026-10-31: "Bevels should never intersect", "Avoid two rolled
    # edges from touching or overlap"): between any two rolled edges there is flat land.
    for i, a in enumerate(windows):
        d = a.exterior.distance(plate.exterior)
        out.append(("R", f"window {i + 1} to the plate edge", d >= two, f"{d:.1f} mm of metal (two rolls and the land need {two:.1f})"))
        for j, b in enumerate(windows[i + 1:], i + 1):
            d = a.distance(b)
            out.append(("R", f"window {i + 1} to window {j + 1}", d >= two, f"{d:.1f} mm of metal (needs {two:.1f})"))
    need_d = roll + DIMPLE_RIM + land
    for (x, y, rx, ry) in dimples:
        e = affinity.scale(Point(x, y).buffer(1.0, quad_segs=24), rx, ry)
        d = min([e.exterior.distance(plate.exterior)] + [e.distance(w) for w in windows] + [e.distance(g) for _, g in raised])
        out.append(("R", f"dimple at ({x:g}, {y:g})", d >= need_d, f"{d:.1f} mm to the nearest edge (roll, rim and land need {need_d:.1f})"))
    for what, g in raised:
        ok = plate.buffer(-(roll + land)).contains(g) and all(g.distance(w) >= roll + land for w in windows)
        out.append(("R", f"raised {what}", ok, f"stays {roll + land:.1f} mm inside its parent and clear of its openings"))
    return out, flat


def behind_layer(window, plate, grow=8.0, inset=None):
    """A layer under a plate (a film, a well), measured to fit: the window grown by grow mm, clamped inside the plate's
    footprint by inset mm with its corners rounded at least as much as the plate's (rule B)."""
    inset = INSET if inset is None else inset
    return window.buffer(grow, quad_segs=16).intersection(plate.buffer(-inset, quad_segs=16))


def check(name, spec, plate=None):
    """The catalogue tiles' fit (plate defaults to the 320 x 220 tile; its films are the windows grown 8 mm)."""
    plate = plate or PLATE
    wins = spec["windows"]
    films = [(f"film behind window {i + 1}", behind_layer(w, plate)) for i, w in enumerate(wins)]
    prints = [(f"gauge at ({x}, {y}) r {r} mm", disc(x, y, r)) for (x, y, r) in spec.get("gauges", [])]
    return fit(plate, wins, spec["knobs"], prints, films)


def draw(out_dir, name, spec, flat, results, scale=2):
    """The check map: rolls dark, flat metal light, displays tan, footprints green (pass) or red (fail)."""
    im = Image.new("RGB", (W * scale, H * scale), (24, 22, 20))
    d = ImageDraw.Draw(im)

    def poly(g, fill, outline=None):
        for p in (g.geoms if hasattr(g, "geoms") else [g]):
            if p.is_empty:
                continue
            d.polygon([(x * scale, y * scale) for x, y in p.exterior.coords], fill=fill, outline=outline)
            for hole in p.interiors:
                d.polygon([(x * scale, y * scale) for x, y in hole.coords], fill=(24, 22, 20))
    poly(PLATE, (110, 104, 94))
    poly(flat, (176, 170, 158))
    for w in spec["windows"]:
        poly(w.buffer(ROLL), (110, 104, 94))
        poly(w, (226, 204, 156))
    for (x, y, r) in spec.get("gauges", []):
        d.ellipse([(x - r) * scale, (y - r) * scale, (x + r) * scale, (y + r) * scale], outline=(120, 96, 60), width=2)
    for (x, y, r, _p), res in zip(spec["knobs"], results):
        f = r * 1.14 + MARGIN
        col = (40, 170, 70) if res[2] else (220, 50, 40)
        d.ellipse([(x - f) * scale, (y - f) * scale, (x + f) * scale, (y + f) * scale], outline=col, width=3)
        d.ellipse([(x - r) * scale, (y - r) * scale, (x + r) * scale, (y + r) * scale], fill=(210, 206, 196), outline=(60, 56, 50))
    im.save(os.path.join(out_dir, f"check_{name}.png"))


def build(out_dir):
    """Check every shape, write its check map and the outlines for Blender; return the number of failed rules."""
    os.makedirs(out_dir, exist_ok=True)
    specs, report, bad = {}, [], 0
    for name, spec in tiles().items():
        res, flat = check(name, spec)
        draw(out_dir, name, spec, flat, res)
        bad += sum(1 for r in res if not r[2])
        report.append((name, res))
        specs[name] = dict(windows=[list(w.exterior.coords)[:-1] for w in spec["windows"]], knobs=spec["knobs"],
                           gauges=spec.get("gauges", []), plate=list(PLATE.exterior.coords)[:-1],
                           plate_off=list(PLATE.buffer(-ROLL, quad_segs=16).exterior.coords)[:-1],
                           holes_off=[list(w.buffer(ROLL, quad_segs=16).exterior.coords)[:-1] for w in spec["windows"]],
                           bezel=[[list(w.buffer(0.8, quad_segs=16).exterior.coords)[:-1], list(w.buffer(-1.5, quad_segs=16).exterior.coords)[:-1]]
                                  for w in spec["windows"]],
                           films=[list(behind_layer(w, PLATE).exterior.coords)[:-1] for w in spec["windows"]],
                           lamps=lamps(spec["windows"]))
    for name, res in report:
        print(f"{name}:")
        for kind, what, ok, why in res:
            print(f"  {'PASS' if ok else 'FAIL'} {kind} {what}: {why}")
    with open(os.path.join(out_dir, "tiles.json"), "w") as f:
        json.dump(specs, f)
    print("failures:", bad)
    return bad


def selftest():
    """The first drawings' faults, each fed to the checker on purpose: every one must be caught."""
    t = tiles()
    cases = {
        "ear, knob on the lobe's edge": dict(t["ear"], knobs=[(242, 91, 11, None)]),
        "bay, two windows 4 mm apart (rolls overlap)": dict(windows=[rbox(22, 62, 150, 170, 8), rbox(154, 62, 298, 170, 8)], knobs=[]),
        "terrace, key on a 12 mm shelf": dict(t["terrace"], knobs=[(156, 142, 9, None)]),
        "bridge, a bar pinched to 10 mm": dict(windows=[rbox(22, 22, 155, 160, 8), rbox(165, 22, 298, 160, 8)], knobs=[]),
    }
    missed = 0
    # rule B, the study's own fault: a time display 8 mm above the plate's bottom edge, its film grown 8 mm unclamped
    win = rbox(122, 150, 298, 206, 4)
    res, _ = fit(PLATE, [win], behind=[("film grown 8 mm, unclamped", win.buffer(8, quad_segs=16))])
    fails = [f"{r[1]}: {r[3]}" for r in res if not r[2] and r[0] == "B"]
    missed += not fails
    print(f"{'CAUGHT' if fails else 'MISSED'} time display, film past the plate's edge: {'; '.join(fails[:1])}")
    for name, (spec, rule) in {
        "time display 5 mm from the plate's bottom edge (rolls meet)": (dict(windows=[rbox(122, 150, 298, 209, 4)]), "R"),
        "dimple 8 mm from a window (8.2 needed)": (dict(windows=[rbox(22, 22, 298, 120, 8)], dimples=[(160, 140, 12, 12)]), "R"),
        "raised plate 2 mm from its parent's edge": (dict(windows=[], raised=[("transport plate", rbox(8, 150, 200, 212, 6))]), "R"),
    }.items():
        res, _ = fit(PLATE, spec["windows"], dimples=spec.get("dimples", ()), raised=spec.get("raised", ()))
        fails = [f"{r[1]}: {r[3]}" for r in res if not r[2] and r[0] == rule]
        missed += not fails
        print(f"{'CAUGHT' if fails else 'MISSED'} {name}: {'; '.join(fails[:1])}")
    for name, spec in cases.items():
        res, _ = check(name, spec)
        fails = [f"{r[1]}: {r[3]}" for r in res if not r[2]]
        missed += not fails
        print(f"{'CAUGHT' if fails else 'MISSED'} {name}: {'; '.join(fails[:2])}")
    return missed


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--out", help="folder for tiles.json and the check maps")
    ap.add_argument("--selftest", action="store_true", help="prove the checker catches known faults")
    a = ap.parse_args()
    if a.selftest:
        sys.exit(1 if selftest() else 0)
    if not a.out:
        ap.error("--out is required")
    sys.exit(1 if build(a.out) else 0)


def lamps(wins, spacing=96):
    """Lamp positions along each window's top edge, about spacing mm apart."""
    out = []
    for w in wins:
        x0, y0, x1, y1 = w.bounds
        n = max(1, round((x1 - x0) / spacing))
        for k in range(n):
            x = x0 + (k + 0.5) * (x1 - x0) / n
            hit = w.intersection(LineString([(x, -10), (x, H + 10)]))
            if not hit.is_empty:
                out.append((x, hit.bounds[1]))
    return out


if __name__ == "__main__":
    main()
