"""Measure that no layer under a plate shows outside it: (1) every film's footprint lies inside a plate's, rounded
corners included; (2) on the render, no lit pixel lies outside the union of the plates' footprints.
    python3 -I fitcheck.py <render.png> <fit.json>"""
import json, sys
import numpy as np
from PIL import Image, ImageDraw
from shapely.geometry import box

def rr(x0, y0, x1, y1, r):
    return box(x0 + r, y0 + r, x1 - r, y1 - r).buffer(r, quad_segs=16)

img, fit = sys.argv[1], json.load(open(sys.argv[2]))
plates = [rr(*p) for p in fit["plates"]]
bad = 0
for f in fit["films"]:
    ok = any(p.contains(rr(*f)) for p in plates)
    bad += not ok
    print(("PASS" if ok else "FAIL"), "film", [round(v, 1) for v in f], "inside its plate")
im = Image.open(img).convert("RGBA")
s = im.width / fit["W"]
mask = Image.new("L", im.size, 0)
d = ImageDraw.Draw(mask)
for p in fit["plates"]:
    x0, y0, x1, y1, r = p
    d.rounded_rectangle([(x0 - 1) * s, (y0 - 1) * s, (x1 + 1) * s, (y1 + 1) * s], radius=(r + 1) * s, fill=255)
a = np.asarray(im).astype(float)
lum = 0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]
outside = (np.asarray(mask) == 0) & (a[..., 3] > 0)
lit = outside & (lum > 110)
ys, xs = np.nonzero(lit)
print(f"lit pixels outside the plates: {lit.sum()}" + (f" (around x {int(xs.mean() / s)}, y {int(ys.mean() / s)} px)" if lit.sum() else ""))
sys.exit(1 if (bad or lit.sum()) else 0)
