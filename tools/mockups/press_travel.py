#!/usr/bin/env python3
"""Measure how far a rendered key's cap travels down the image when it is pressed, and check that its frame did not
move, so the page's label can follow exactly that travel from one measured number (owner, 2026-10-31: "if button is
3d, render the 3d state of the button being pressed with the frame unmoved, and the label should sync with the amount
of travel that button took").

For a kit's button.idle and button.pressed renders (RGBA, at the manifest's scale):
- the moving part is where the two images differ; within it, only the edges that moved (where the vertical-edge maps
  of idle and pressed differ, so a fixed rim drops out) are cross-correlated (Sobel y of luminance, normalised) over
  downward shifts, and the best shift (refined to a tenth of a pixel) is the travel;
- the frame is every opaque pixel outside that region, grown by a margin; its mean and 99th-percentile difference
  between idle and pressed say whether it stayed put.

    python3 tools/mockups/press_travel.py assets/ui/kethran [--write]

prints {"press_px": <travel at 1x>, "frame_mean": ..., "frame_p99": ...}. With --write it records press_px on the
manifest's button.pressed piece, which is the one number the page's label sink reads.
"""
import json
import pathlib
import sys

import numpy as np
from PIL import Image


def lum(path):
    a = np.asarray(Image.open(path).convert("RGBA"), float) / 255.0
    rgb, al = a[..., :3], a[..., 3:]
    over = rgb * al + 0.5 * (1 - al)          # over mid grey, so transparent edges do not read as edges
    return over @ np.array([0.2126, 0.7152, 0.0722]), a[..., 3]


def sobel_y(l):
    p = np.pad(l, 1, mode="edge")
    return (p[2:, :-2] + 2 * p[2:, 1:-1] + p[2:, 2:]) - (p[:-2, :-2] + 2 * p[:-2, 1:-1] + p[:-2, 2:])


def measure(kit, scale=None):
    """The travel of kit's pressed key at 1x (px), and how much its frame changed."""
    kit = pathlib.Path(kit)
    man = json.loads((kit / "manifest.json").read_text())
    s = scale or man.get("scale", 2)
    li, ai = lum(kit / "button.idle.png")
    lp, _ = lum(kit / "button.pressed.png")
    return measure_images(li, lp, ai, s)


def measure_images(li, lp, ai, s):
    """The travel between an idle and a pressed image (luminance arrays, alpha of the idle one), in px at 1/s."""
    diff = np.abs(li - lp) > 0.04
    ys, xs = np.nonzero(diff)
    if len(ys) == 0:
        return {"press_px": 0.0, "frame_mean": 0.0, "frame_p99": 0.0, "region": None}
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    gi, gp = sobel_y(li), sobel_y(lp)
    # Only the edges that moved count: where the vertical-edge maps of idle and pressed differ. A fixed rim has the same
    # edge in both and drops out; a moving cap edge, the sunken glass's top edge or moving texture stays in.
    moved = np.abs(gi - gp)
    moved = moved > 0.15 * moved[y0:y1, x0:x1].max()
    k = 3 * s // 2                          # grown a little, so an edge and where it went are both inside
    mv = np.zeros_like(moved)
    for dy in range(-k, k + 1):
        mv |= np.roll(moved, dy, axis=0)
    best, scores = None, {}
    for d in np.arange(0, 12 * s // 2 + 1):  # downward shifts, in render px
        w = mv[y0:y1 - d, x0:x1] & mv[y0 + d:y1, x0:x1]
        a = gi[y0:y1 - d, x0:x1][w]
        b = gp[y0 + d:y1, x0:x1][w]
        if a.size < 20:
            continue
        a, b = a - a.mean(), b - b.mean()
        c = float((a * b).sum() / (np.sqrt((a * a).sum() * (b * b).sum()) + 1e-9))
        scores[int(d)] = c
        if best is None or c > scores[best]:
            best = int(d)
    d = float(best)
    if best - 1 in scores and best + 1 in scores:   # parabola through the peak and its neighbours: a tenth of a pixel
        cm, c0, cp = scores[best - 1], scores[best], scores[best + 1]
        den = cm - 2 * c0 + cp
        if den < 0:
            d = best + 0.5 * (cm - cp) / den
    # Flat glass has no texture to follow: what shows its travel is the dark inner wall it uncovers under the top lip.
    # Measure that band: rows in the top third of the moving region, down the middle half of its width, that are
    # darker pressed than idle although the pressed glass is lit brighter. Its height is the travel the eye reads.
    cx0, cx1 = x0 + (x1 - x0) // 4, x1 - (x1 - x0) // 4
    pi_, pp_ = np.median(li[y0:y1, cx0:cx1], axis=1), np.median(lp[y0:y1, cx0:cx1], axis=1)
    dark = (pp_ < pi_ - 0.08)[: max(1, (y1 - y0) // 3)]
    run = cur = 0
    for v in dark:
        cur = cur + 1 if v else 0
        run = max(run, cur)
    wall = run / s
    method = "edges"
    if wall > d / s + 0.5:            # the wall band shows more travel than the edges could: flat glass
        d, method = run, "wall band"
    grow = 4 * s // 2
    mask = ai > 0.5
    mask[max(0, y0 - grow):y1 + grow, max(0, x0 - grow):x1 + grow] = False
    fd = np.abs(li - lp)[mask]
    if fd.size == 0:
        fd = np.zeros(1)
    return {"press_px": round(d / s, 2), "method": method, "edges_px": round(best / s, 2), "wall_px": round(wall, 2), "frame_mean": round(float(fd.mean()), 4),
            "frame_p99": round(float(np.percentile(fd, 99)), 4), "region": [int(x0), int(y0), int(x1), int(y1)]}


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    for kit in args:
        r = measure(kit)
        print(pathlib.Path(kit).name, json.dumps(r))
        if "--write" in sys.argv:
            p = pathlib.Path(kit) / "manifest.json"
            man = json.loads(p.read_text())
            man["pieces"]["button.pressed"]["press_px"] = r["press_px"]
            p.write_text(json.dumps(man, indent=2) + "\n")


if __name__ == "__main__":
    main()
