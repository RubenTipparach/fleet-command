"""Measure the shots tools/mockups/console_fit.mjs took: for every leaf, its own pixels (the difference between the
parent shot with the leaf and without it) grown by PAD must lie on plain art, with no frame edge (a bevel, rail, rim
or ledge) under them. An edge is a run of strong luminance steps that lines up into a line, which texture (hatching,
leather, noise) does not do. Writes <skin>-fit.png per skin, failing leaves boxed in red, and exits 1 on a failure.

    python3 tools/mockups/console_fit.py <out dir>/index.json
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

PAD = 3            # px at the 1920 reference: the even margin a child keeps from its parent's frame
STEP = 34          # a luminance step (0-255) between neighbours that counts as an edge pixel
LINE = 14          # an edge that runs this many px straight under the grown leaf, and
                   # (calibrated 2026-10-31: the faults found, rails, bevels, seams and rims under labels, bars and
                   # titles, ran 19-517 px; fragments of scanlines and glow under text sitting well inside its lens
                   # ran 10-13 px)
FULL = 36          # this many px straight in the whole parent, is a frame line: texture (grime, hatching, leather)
                   # makes short runs, a rail, rim or bevel runs on past the leaf


def lum(path):
    a = np.asarray(Image.open(path).convert("RGB"), dtype=np.float32)
    return 0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2], a


def edges(L):
    """Horizontal and vertical edge maps: a strong step to the next pixel down (a horizontal line) or across."""
    h = np.zeros(L.shape, bool)
    v = np.zeros(L.shape, bool)
    h[:-1] = np.abs(L[1:] - L[:-1]) > STEP
    v[:, :-1] = np.abs(L[:, 1:] - L[:, :-1]) > STEP
    return h, v


def runs(mask, axis):
    """Each pixel's straight run length of True along axis (1: along rows, 0: down columns)."""
    m = mask if axis == 1 else mask.T
    out = np.zeros(m.shape, np.int32)
    for i, row in enumerate(m):
        if not row.any():
            continue
        d = np.diff(np.concatenate(([0], row.astype(np.int8), [0])))
        for a, b in zip(np.where(d == 1)[0], np.where(d == -1)[0]):
            out[i, a:b] = b - a
    return out if axis == 1 else out.T


def stripes(line, axis):
    """Lines that belong to a stripe texture (scanlines, ribs, grain): three or more parallel lines at one pitch of 2-5
    px. A frame edge stands alone, or as the pair of edges either side of a thin rim."""
    m = line if axis == 1 else line.T
    rows = m.any(1)
    tex = np.zeros(len(rows), bool)
    for p in range(2, 6):
        for r in range(len(rows)):
            if rows[r] and ((r - p >= 0 and rows[r - p] and r + p < len(rows) and rows[r + p])
                            or (r + 2 * p < len(rows) and rows[r + p] and rows[r + 2 * p])
                            or (r - 2 * p >= 0 and rows[r - p] and rows[r - 2 * p])):
                tex[r] = True
    out = np.zeros(m.shape, bool)
    out[tex] = m[tex]
    return out if axis == 1 else out.T


def frame_run(edge, grown, axis):
    """The longest run of frame-line edge pixels under the grown leaf: runs at least LINE long under it that belong to
    a line at least FULL long in the parent, and not to a stripe texture."""
    line = edge & (runs(edge, axis) >= FULL)
    line &= ~stripes(line, axis)
    under = runs(line & grown, axis)
    return int(under.max()) if under.size else 0, line & grown


def check(rec):
    Lb, _ = lum(rec["base"])
    Ls, _ = lum(rec["shot"])
    if Lb.shape != Ls.shape:
        return None
    ink = np.abs(Ls - Lb) > 18
    ink = ndimage.binary_opening(ink, np.ones((2, 2))) | (ndimage.uniform_filter(ink.astype(float), 3) > 0.6)
    if ink.sum() < 6:
        return {"empty": True}
    if rec["name"].startswith(("k-fill", "k-cells")):
        # A fill (or a symbol bar's row of cells) fills its track's groove (the kit's `inset`), so shadows inside the groove lie under it by design;
        # what it must not do is cover the groove's rim: no frame line in its own first or last row.
        ys = np.where(ink.any(1))[0]
        grown = ink.copy()
        # edges are recorded on the row above their step, so the steps inside the fill's two outer rows are on rows
        # y0, y0+1 and y1-2, y1-1; row y1 records the step just outside it, which is the rim it may meet
        grown[ys.min() + 2:ys.max() - 2] = False
        grown[ys.max()] = False
    else:
        grown = ndimage.binary_dilation(ink, np.ones((2 * PAD + 1, 2 * PAD + 1)))
    h, v = edges(Lb)
    # A horizontal frame line shows as a run of vertical steps along a row, and the other way round.
    hr, hh = frame_run(h, grown, 1)
    vr, vh = frame_run(v, grown, 0)
    ys, xs = np.where(ink)
    return {"h": hr, "v": vr, "box": [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())], "bad": max(hr, vr) >= LINE,
            "hits": hh | vh}


def sheet(bad, path):
    """A contact sheet of the failing leaves at 2x: each parent shot with the leaf shown, the edge pixels under the
    grown leaf painted red."""
    if not bad:
        return
    tiles = []
    for rec, r in bad:
        im = Image.open(rec["shot"]).convert("RGB")
        a = np.asarray(im).copy()
        if r["hits"] is not None:
            a[r["hits"]] = (255, 0, 0)
        b = r["box"]
        x0, y0 = max(0, b[0] - 40), max(0, b[1] - 30)
        x1, y1 = min(a.shape[1], b[2] + 40), min(a.shape[0], b[3] + 30)
        t = Image.fromarray(a[y0:y1, x0:x1]).resize(((x1 - x0) * 2, (y1 - y0) * 2), Image.NEAREST)
        tiles.append((rec["name"][:40], t))
    W = min(1800, max(t.size[0] for _, t in tiles))
    H = sum(min(t.size[1], 400) + 22 for _, t in tiles)
    out = Image.new("RGB", (W, H), (24, 24, 24))
    d = ImageDraw.Draw(out)
    y = 0
    for name, t in tiles:
        d.text((4, y + 4), name, fill=(255, 255, 0))
        out.paste(t.crop((0, 0, min(W, t.size[0]), min(400, t.size[1]))), (0, y + 22))
        y += min(t.size[1], 400) + 22
    out.save(path)


def overlaps(blocks):
    out = []
    for i, a in enumerate(blocks):
        for b in blocks[i + 1:]:
            ax, ay, aw, ah = a["rect"]
            bx, by, bw, bh = b["rect"]
            t = 0.6
            inside = (bx >= ax - t and by >= ay - t and bx + bw <= ax + aw + t and by + bh <= ay + ah + t) or (ax >= bx - t and ay >= by - t and ax + aw <= bx + bw + t and ay + ah <= by + bh + t)
            if inside:
                continue
            inside = (bx >= ax and by >= ay and bx + bw <= ax + aw and by + bh <= ay + ah) or (ax >= bx and ay >= by and ax + aw <= bx + bw and ay + ah <= by + bh)
            if inside:
                continue
            ix = min(ax + aw, bx + bw) - max(ax, bx)
            iy = min(ay + ah, by + bh) - max(ay, by)
            if ix > 0.5 and iy > 0.5:
                out.append((a["name"], b["name"], round(ix, 1), round(iy, 1)))
    return out


def main():
    idx_path = sys.argv[1]
    out_dir = os.path.dirname(idx_path)
    index = json.load(open(idx_path))
    fails = 0
    for skin in index:
        bad = []
        for rec in skin["leaves"]:
            r = check(rec)
            if r and r.get("empty") and rec["name"].split(" ")[0] in ("ptitle", "lbl", "num", "ovl") or (r and r.get("empty") and rec["name"].endswith(" label")):
                # text that shows no pixels was clipped or hidden: that is a fault, not a pass
                bad.append((rec, {"h": 0, "v": 0, "box": [0, 0, 1, 1], "bad": True, "hits": None, "empty": True}))
                continue
            if r and not r.get("empty") and r["bad"]:
                bad.append((rec, r))
        ov = overlaps(skin["blocks"]) + [(c, "", 0, 0) for c in skin.get("clipped", [])]
        fails += len(bad) + len(ov)
        print("%-9s %s" % (skin["skin"], "clear" if not bad and not ov else "%d leaf edge(s), %d block overlap(s)" % (len(bad), len(ov))))
        for rec, r in bad:
            if r.get("empty"):
                print("    %-46s on %-22s shows no pixels (clipped or hidden)" % (rec["name"][:46], rec["parentCls"].replace("nine", "").strip()[:22]))
            else:
                print("    %-46s on %-22s edge run h %3d v %3d" % (rec["name"][:46], rec["parentCls"].replace("nine", "").strip()[:22], r["h"], r["v"]))
        for a, b, ix, iy in ov:
            print("    overlap: %s / %s (%s x %s px)" % (a, b, ix, iy) if b else "    does not fit: %s" % a)
        sheet(bad, os.path.join(out_dir, "%s-fit.png" % skin["skin"]))
    print("console fit:", "PASS" if not fails else "FAIL (%d)" % fails)
    sys.exit(1 if fails else 0)


if __name__ == "__main__":
    main()
