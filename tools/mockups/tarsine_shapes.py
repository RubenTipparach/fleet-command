"""Fleet Command: the Tarsine Remnant kit's shapes, fit-checked, as the one source for its render and its page module.

Owns the geometry of every piece of the Remnant skin (mockup 4, console-skins), rebuilt on the analog panel bible (owner,
2026-10-31: "I love the Remnant ui, i htink it could use some polish from our guide, but please dont remove the grimey
industrial feel, I love that story telling so much"). The ancient race's language stays (design.md: "a powerful ancient
race. rusty, with back lit buttons clogged with dirt. their ships havent been upgraded in ages, rough around the
edges"): monumental corroded iron plates whose edges roll over in a worn round, stepped capitals grown into the corners
of every frame's well (each holding a great rivet), arched-tablet keys on a wider foot with an arched window of grimy
glass, lintel tabs with a slot of light, a beam strip whose channel ends at riveted gold brackets, cracked glass on the
gauges, rust streaks running down from every rivet. The streaks are decals clipped to the flat metal, so they never
climb a roll. tools/blender/build_ui_tarsine.py renders exactly these outlines; docs/mockups/skins/tarsine.js places
its live readouts inside the measured windows recorded here.

Every piece passes tools/mockups/formed_metal.py's fit rules before anything is written (the analog panel bible,
chapter 9): K rivets on flat metal, L tips that hold the roll, R rolled edges that never meet, P live readouts and cracks
inside their windows, B films inside the plate. Gold brackets and the crest's crown are checked as raised plates. Every
corner is rounder than about twice the roll (the blender-ui-panels skill), and outlines are cleaned before they are
written. A failure writes nothing and exits 1.

    python3 tools/mockups/tarsine_shapes.py              # check, then write assets/ui/tarsine/shapes.json
    python3 tools/mockups/tarsine_shapes.py --verify assets/ui/tarsine   # rule B on the renders

Units: slot px = mm, y down, at the 1920 x 1080 reference (renders are at 2x). Needs shapely.
"""
import json
import math
import os
import sys

from shapely.geometry import LineString, Polygon, box
from shapely.ops import unary_union

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from formed_metal import INSET, behind_layer, disc, fit, rbox, rbox4, soft  # noqa: E402

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "assets", "ui", "tarsine")

ROLL = 2.6   # a worn round on monumental iron: soft, heavy, every edge rubbed smooth by ages of hands
SOFT = 5.5   # every corner at least twice the roll round, or the offset turns it inside out


def tablet(x0, y0, x1, y1, r, foot=4):
    """The arched tablet (design.md: "round top, flat foot"): round top corners of radius r, near-square foot."""
    return rbox4(x0, y0, x1, y1, r, r, foot, foot)


def crack(pts, w=0.8):
    """A crack across glass, as a thin geometry so rule P can hold it inside its window."""
    return LineString(pts).buffer(w / 2)


def pieces():
    """Every piece: kind, size, 9-slice margins (top, right, bottom, left), stretch, the cells' groove inset (bar track),
    roll and land, the plate, its wells (each with the film behind it: dim, warm, hot, hearth, gold, slot, lamp colours),
    its rivets, its raised gold, its cracks and the live readouts the page draws (for rule P)."""
    P = {}

    def add(name, kind, size, plate, wells, rivets=(), gold=(), cracks=(), live=(), margins=None, stretch=None, roll=ROLL,
            land=3.0, inset=None, dz=-7.0, pad=3.0, streaks=True):
        P[name] = dict(kind=kind, size=size, margins=margins, stretch=stretch, roll=roll, land=land, inset=inset, plate=plate,
                       wells=list(wells), rivets=list(rivets), gold=list(gold), cracks=list(cracks), live=list(live), dz=dz,
                       pad=pad, streaks=streaks)

    # Frames: a stepped iron plate round a well whose corners are filled by stepped capitals of the plate, each holding
    # a great rivet; rust runs down from every rivet. Capitals and steps stay inside the 34 px margins.
    def stepped(x0, y0, x1, y1, s):
        return box(x0, y0, x1, y1).difference(unary_union([box(x0, y0, x0 + s, y0 + s), box(x1 - s, y0, x1, y0 + s),
                                                           box(x0, y1 - s, x0 + s, y1), box(x1 - s, y1 - s, x1, y1)]))
    well = stepped(12, 12, 148, 108, 18)
    add("frame.panel", "nine", (160, 120), soft(stepped(0.5, 0.5, 159.5, 119.5, 6), SOFT), [("dim", soft(well, SOFT))],
        rivets=[(21, 21, 3.2), (139, 21, 3.2), (21, 99, 3.2), (139, 99, 3.2)], margins=(34, 34, 34, 34), stretch="xy")
    add("frame.inset", "nine", (64, 48), soft(rbox(0.5, 0.5, 63.5, 47.5, 6), SOFT), [("dim", soft(rbox(9, 9, 55, 39, 6), SOFT))],
        margins=(14, 14, 14, 14), stretch="xy", dz=-4.0, streaks=False)
    # The symbol bar's track: an iron rail with a groove of grimy glass the arched cells sit in (inset 9, 3: 14 px cells).
    add("bar.track", "nine", (96, 20), soft(box(0.5, 0.5, 95.5, 19.5), 1.4), [("slot", soft(box(7, 2.2, 89, 17.8), 1.2))],
        live=[("cells", box(9, 3, 87, 17))], margins=(0, 14, 0, 14), stretch="x", roll=0.6, land=0.4, inset=(9, 3), dz=-2.0,
        pad=0.4, streaks=False)
    # Keys: the arched tablet standing on a wider foot, an arched window of grimy glass in it, concentric with the arch.
    # Every state is the same iron and the same dirt; only the light behind the glass changes (owner, 2026-10-31: "sync up
    # dust with out and unlit button to give it that illusion"). The arch and the foot stay in the margins.
    key_plate = soft(unary_union([tablet(6, 2, 134, 46, 24, 2), rbox(1, 40, 139, 51, 3)]), SOFT)
    key_well = soft(tablet(15, 10.5, 125, 40, 15), 3)
    for state, film in (("idle", "dim"), ("hover", "warm"), ("pressed", "hot")):
        add(f"button.{state}", "nine", (140, 52), key_plate, [(film, key_well)], live=[("label", box(28, 17, 112, 35))],
            margins=(24, 30, 12, 30), stretch="x", dz=-5.0, pad=2.0, streaks=False)
    # Tabs: a lintel of iron, its ends chipped by time, a slot of glass the label shows through; the active one lit.
    lintel = soft(Polygon([(5, 2.5), (113, 1.5), (118.5, 8), (118.5, 38.5), (7, 39), (1.5, 32)]), SOFT)
    slot = soft(box(13, 11, 107, 29.5), 4)
    for state, film in (("idle", "dim"), ("active", "warm")):
        add(f"tab.{state}", "nine", (120, 40), lintel, [(film, slot)], live=[("label", box(18, 14, 102, 27))],
            margins=(12, 14, 12, 14), stretch="x", dz=-4.0, pad=1.5, streaks=False)
    # The strips: an iron beam with a long channel of grimy glass for the readouts; at each end a stepped gold bracket
    # lies on the iron, riveted down, rust running from its rivets. 72 px, the channel's open band rows 13-59.
    chan = soft(rbox(60, 10, 452, 62, 8), SOFT)
    bracket = Polygon([(8, 8), (36, 8), (36, 16), (50, 16), (50, 56), (36, 56), (36, 64), (8, 64)])
    for name in ("strip.top", "strip.bottom"):
        add(name, "nine", (512, 72), soft(rbox(0.5, 0.5, 511.5, 71.5, 8), SOFT), [("dim", chan)],
            rivets=[(16, 20, 3.0), (16, 52, 3.0), (496, 20, 3.0), (496, 52, 3.0)],
            gold=[soft(bracket, 2.0), soft(Polygon([(512 - x, y) for x, y in bracket.exterior.coords]), 2.0)],
            live=[("readouts", box(104, 14, 408, 58))], margins=(10, 100, 10, 100), stretch="x", dz=-6.0, pad=1.5)
    add("divider", "nine", (128, 10), soft(rbox(0.5, 0.5, 127.5, 9.5, 4.5), 2), [("slot", soft(box(14, 3.3, 114, 6.7), 1.2))],
        margins=(0, 16, 0, 16), stretch="x", roll=0.6, land=1.0, dz=-2.0, pad=0.5, streaks=False)
    # Heroes. The name plate: an iron plaque pointed at both ends, a worn gold field lit dimly from behind, two rivets.
    plaque = soft(Polygon([(14, 0.5), (446, 0.5), (459.5, 28), (446, 55.5), (14, 55.5), (0.5, 28)]), SOFT)
    add("nameplate", "hero", (460, 56), plaque, [("gold", soft(rbox(36, 9.5, 424, 46.5, 5), SOFT))],
        rivets=[(21, 28, 3.2), (439, 28, 3.2)], live=[("name", box(44, 13, 416, 43))], dz=-5.0)
    # The helm: a temple keyboard, nine arched tablets of grimy glass in a heavy plate, the centre one lit like a hearth;
    # great rivets where the rows cross.
    keys = []
    for r in range(3):
        for c in range(3):
            cx, cy = 64 + 86 * c, 63 + 86 * r
            keys.append(("hearth" if (r, c) == (1, 1) else "low", soft(tablet(cx - 30, cy - 31, cx + 30, cy + 31, 14), SOFT)))
    arrows = [(150, 63), (236, 63), (236, 149), (236, 235), (150, 235), (64, 235), (64, 149), (64, 63)]
    add("helm.pad", "hero", (300, 300), soft(stepped(1, 1, 299, 299, 10), SOFT), keys,
        rivets=[(107, 106, 3.4), (193, 106, 3.4), (107, 192, 3.4), (193, 192, 3.4)],
        live=[(f"key {i}", box(x - 13, y - 11, x + 13, y + 11)) for i, (x, y) in enumerate(arrows)] + [("halt", box(128, 140, 172, 157))],
        dz=-8.0)
    # The ship display: a monument frame round cracked, grimy glass, stepped capitals in its corners, a rivet in each.
    ssd = stepped(22, 22, 438, 278, 24)
    add("ssd.bezel", "hero", (460, 300), soft(stepped(0.5, 0.5, 459.5, 299.5, 8), SOFT), [("dim", soft(ssd, SOFT))],
        rivets=[(34, 34, 3.6), (426, 34, 3.6), (34, 266, 3.6), (426, 266, 3.6)],
        cracks=[crack([(70, 40), (96, 74), (90, 108), (118, 150)]), crack([(380, 262), (356, 236), (366, 214)])],
        live=[("ship and shields", disc(230, 150, 112))], dz=-9.0)
    # The gauge: a round iron housing round cracked glass, four rivets.
    add("dial", "hero", (120, 120), disc(60, 60, 59.5), [("dim", disc(60, 60, 40))],
        rivets=[(60 + math.cos(a) * 51, 60 + math.sin(a) * 51, 2.4) for a in [math.radians(45 + 90 * i) for i in range(4)]],
        cracks=[crack([(38, 36), (52, 50), (48, 66)])], live=[("needle and contacts", disc(60, 60, 36))], dz=-7.0)
    # Alert lamps: an old caged bulb: a round housing round coloured glass, the cage's bars printed dark across it.
    for c in ("green", "yellow", "red"):
        add(f"lamp.alert.{c}", "hero", (64, 64), disc(32, 32, 31.5), [(f"lamp_{c}", disc(32, 30, 17))],
            cracks=[LineString([(x, 18), (x, 42)]).buffer(0.9) for x in (26, 32, 38)], dz=-5.0, pad=2.0, streaks=False)
    # The crest (original): a stepped crown of three tiers in worn gold standing on a corroded iron disc.
    crown = Polygon([(18, 70), (18, 58), (28, 58), (28, 44), (38, 44), (38, 28), (58, 28), (58, 44), (68, 44), (68, 58), (78, 58), (78, 70)])
    add("crest", "hero", (96, 96), disc(48, 48, 46.5), [], gold=[soft(crown, 1.5)], dz=-6.0, streaks=False)
    return P


# The symbol bar's cells (not plates: little arched windows the page lays in the track's groove), one outline for all
# three, so lit and unlit differ only in their light; the dirt on them is the same.
CELL = dict(size=(10, 14), gap=2)
CELL["outline"] = [(0.6, 13.4), (0.6, 5)] + [(5 + 4.4 * math.cos(math.pi + math.pi * k / 10), 5 + 4.4 * math.sin(math.pi + math.pi * k / 10)) for k in range(11)] + [(9.4, 13.4)]


def streaks_for(p, flat):
    """Rust running down from each rivet: a tapering wedge from the rivet's foot, as long as the flat metal below it
    allows (clipped to it, so a streak never climbs a roll)."""
    out = []
    for i, (x, y, r) in enumerate(p["rivets"]):
        L = 10 + (i * 7) % 9
        drift = ((i * 5) % 7 - 3) * 0.6
        g = Polygon([(x - r * 0.7, y + r * 0.6), (x + r * 0.7, y + r * 0.6), (x + drift + 0.6, y + r + L), (x + drift - 0.6, y + r + L)])
        g = g.intersection(flat.buffer(-0.4))
        if not g.is_empty and g.geom_type == "Polygon" and g.area > 2.0:
            out.append(g)
    return out


def check_all(P):
    """Run every piece through the fit rules; return (failures, report lines, films, streaks)."""
    bad, lines, films, streaks = 0, [], {}, {}
    for name, p in P.items():
        wells = [w for _k, w in p["wells"]]
        gap = min(w.exterior.distance(p["plate"].exterior) for w in wells) if wells else 8.0
        inset = min(INSET, gap / 2)
        f = [behind_layer(w, p["plate"], grow=min(6.0, gap), inset=inset) for w in wells]
        films[name] = f
        res, flat = fit(p["plate"], wells, knobs=[(x, y, r, None) for x, y, r in p["rivets"]],
                        prints=[(f"live {role}", g) for role, g in p["live"]] + [(f"crack {i + 1}", g) for i, g in enumerate(p["cracks"])],
                        behind=[(f"film behind well {i + 1}", g) for i, g in enumerate(f)],
                        raised=[(f"gold {i + 1}", g) for i, g in enumerate(p["gold"])],
                        roll=p["roll"], land=p["land"], inset=inset, pad=p["pad"])
        streaks[name] = streaks_for(p, flat) if p["streaks"] else []
        fails = [r for r in res if not r[2]]
        bad += len(fails)
        lines.append(f"{name}: {len(res) - len(fails)} of {len(res)} pass" + "".join(f"\n  FAIL {r[0]} {r[1]}: {r[3]}" for r in fails))
    return bad, lines, films, streaks


def pts(g):
    """An outline as points for Blender: simplified within 0.02 px and with no repeated point (the shared offset mitres
    each point, and a zero-length edge sends its offset off to infinity)."""
    out = []
    for x, y in list(g.simplify(0.02, preserve_topology=True).exterior.coords)[:-1]:
        q = [round(x, 3), round(y, 3)]
        if not out or abs(q[0] - out[-1][0]) + abs(q[1] - out[-1][1]) > 0.05:
            out.append(q)
    if len(out) > 2 and abs(out[0][0] - out[-1][0]) + abs(out[0][1] - out[-1][1]) <= 0.05:
        out.pop()
    return out


def write(P, films, streaks):
    out = {"skin": "tarsine", "pieces": {}, "cell": dict(size=list(CELL["size"]), gap=CELL["gap"], outline=[[round(x, 3), round(y, 3)] for x, y in CELL["outline"]])}
    for name, p in P.items():
        out["pieces"][name] = dict(kind=p["kind"], size=list(p["size"]), margins=list(p["margins"]) if p["margins"] else None,
                                   stretch=p["stretch"], inset=list(p["inset"]) if p["inset"] else None, roll=p["roll"], dz=p["dz"],
                                   plate=pts(p["plate"]), wells=[[k, pts(w)] for k, w in p["wells"]], films=[pts(f) for f in films[name]],
                                   rivets=[[x, y, r] for x, y, r in p["rivets"]], gold=[pts(g) for g in p["gold"]],
                                   cracks=[pts(g) for g in p["cracks"]], streaks=[pts(g) for g in streaks[name]],
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
    bad, lines, films, streaks = check_all(P)
    print("\n".join(lines))
    print("failures:", bad)
    if bad:
        sys.exit(1)
    write(P, films, streaks)
    print("wrote", os.path.relpath(os.path.join(OUT, "shapes.json"), ROOT))


if __name__ == "__main__":
    main()
