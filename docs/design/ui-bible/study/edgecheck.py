"""Rule R, measured: no two rolled edges touch or overlap. Reads the geometry the study writes (NLOG_GEOM).
  windows and slots: to their plate's edge and to each other, at least 2 x roll + LAND
  raised plates: at least the parent's roll + LAND inside the parent's edge and clear of its openings
  pressed dimples: at least roll + RIM + LAND from the plate's edge, its openings and raised plates on it
    python3 -I edgecheck.py <geometry.json>"""
import json, sys
from shapely.geometry import Point, Polygon
from shapely import affinity

LAND, RIM = 3.0, 1.4
g = json.load(open(sys.argv[1]))
shapes = [dict(outer=Polygon(s["outer"]), holes=[Polygon(h) for h in s["holes"]], edge=s["edge"], top=s["top"]) for s in g["shapes"]]
rows = []

def row(ok, what, d, need):
    rows.append(ok)
    print(f"{'PASS' if ok else 'FAIL'} {what}: {d:.1f} mm (needs {need:.1f})")

def name(p):
    x0, y0, x1, y1 = p.bounds
    return f"[{x0:.0f},{y0:.0f} to {x1:.0f},{y1:.0f}]"

for i, s in enumerate(shapes):
    e, need = s["edge"], 2 * s["edge"] + LAND
    for h in s["holes"]:
        d = h.exterior.distance(s["outer"].exterior)
        row(d >= need, f"opening {name(h)} to its plate's edge", d, need)
    for a in range(len(s["holes"])):
        for b in range(a + 1, len(s["holes"])):
            d = s["holes"][a].distance(s["holes"][b])
            row(d >= need, f"opening {name(s['holes'][a])} to opening {name(s['holes'][b])}", d, need)
    # raised plates standing on this one
    for j, r in enumerate(shapes):
        if j != i and r["top"] > s["top"] and s["outer"].contains(r["outer"].centroid) and r["outer"].area < s["outer"].area:
            need_r = e + LAND
            d = r["outer"].exterior.distance(s["outer"].exterior)
            row(d >= need_r, f"raised plate {name(r['outer'])} to its parent's edge", d, need_r)
            for h in s["holes"]:
                d = r["outer"].distance(h)
                row(d >= need_r, f"raised plate {name(r['outer'])} to opening {name(h)}", d, need_r)
for (x, y, rx, ry) in g["dimples"]:
    ell = affinity.scale(Point(x, y).buffer(1.0, quad_segs=32), rx, ry)
    host = max((s for s in shapes if s["outer"].contains(Point(x, y))), key=lambda s: s["top"])
    need = host["edge"] + RIM + LAND
    d = ell.exterior.distance(host["outer"].exterior)
    row(d >= need, f"dimple at ({x},{y}) to its plate's edge", d, need)
    for h in host["holes"]:
        d = ell.distance(h)
        if d < 60:
            row(d >= need, f"dimple at ({x},{y}) to opening {name(h)}", d, need)
    for r in shapes:
        if r is not host and r["top"] > host["top"] and host["outer"].contains(r["outer"].centroid) and not r["outer"].contains(Point(x, y)):
            need_r = r["edge"] + RIM + LAND
            d = ell.distance(r["outer"])
            if d < 60:
                row(d >= need_r, f"dimple at ({x},{y}) to raised plate {name(r['outer'])}", d, need_r)
print(f"{sum(rows)} of {len(rows)} pass")
sys.exit(0 if all(rows) else 1)
