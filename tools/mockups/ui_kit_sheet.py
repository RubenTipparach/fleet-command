#!/usr/bin/env python3
"""Contact sheet for one console skin's UI kit: every piece of its manifest at its rendered size over
the console's dark grey, and every 9-slice piece also drawn stretched to three sizes by its own
margins, so the slicing is proven on the real images (CLAUDE.md section 7: one kit, a skin per
faction). Works for any skin whose folder holds a manifest.json in the kit's format; it lives in
tools/mockups because the sheet is a mockup shot, not a game asset.

    python3 tools/mockups/ui_kit_sheet.py assets/ui/kethran docs/screenshots/mockups/console-skins-kethran-kit.png

A piece whose manifest "stretch" is "x" keeps its height and is shown narrow, wide and wider; "xy"
(the default) is shown small, wide and tall.
"""
import json
import pathlib
import sys

from PIL import Image, ImageDraw, ImageFont

BG = (0x1a, 0x1d, 0x21)
INK = (0xb8, 0xbe, 0xc6)
GAP = 24
WIDTH = 2200
FONT = pathlib.Path(__file__).resolve().parents[2] / "tools" / "fonts" / "DejaVuSansMono-Bold.ttf"


def nine(im, margins, w, h):
    """Draw im (RGBA) at w x h px by 9-slicing with margins (t, r, b, l) in its own pixels: corners
    kept, edges stretched along one axis, the centre stretched along both (CSS border-image stretch)."""
    t, r, b, l = margins
    W, H = im.size
    xs, ys = (0, l, W - r, W), (0, t, H - b, H)
    xd, yd = (0, l, w - r, w), (0, t, h - b, h)
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for i in range(3):
        for j in range(3):
            sw, sh = xs[i + 1] - xs[i], ys[j + 1] - ys[j]
            dw, dh = xd[i + 1] - xd[i], yd[j + 1] - yd[j]
            if sw <= 0 or sh <= 0 or dw <= 0 or dh <= 0:
                continue
            part = im.crop((xs[i], ys[j], xs[i + 1], ys[j + 1]))
            if (dw, dh) != (sw, sh):
                part = part.resize((dw, dh), Image.Resampling.BILINEAR)
            out.alpha_composite(part, (xd[i], yd[j]))
    return out


def main():
    src, dst = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
    man = json.loads((src / "manifest.json").read_text())
    s = man.get("scale", 1)
    font = ImageFont.truetype(str(FONT), 18)
    rows = []          # each row: list of (label, image)
    heroes = []
    for name, p in man["pieces"].items():
        im = Image.open(src / p["file"]).convert("RGBA")
        if p["kind"] != "nine":
            heroes.append((name, im))
            continue
        m = [v * s for v in p["margins"]]
        W, H = im.size
        mw, mh = m[1] + m[3], m[0] + m[2]
        if p.get("stretch", "xy") == "x":
            sizes = [(max(mw + 8, int(W * 0.7)), H), (int(W * 2.2), H), (int(W * 3.4), H)]
            tags = ("narrow", "wide", "wider")
        else:
            sizes = [(max(mw + 8, int(W * 0.75)), max(mh + 8, int(H * 0.75))), (int(W * 2.6), H), (W, int(H * 2.2))]
            tags = ("small", "wide", "tall")
        row = [(f"{name}  {p['size'][0]}x{p['size'][1]}  m{p['margins']}", im)]
        for (w, h), tag in zip(sizes, tags):
            row.append((tag, nine(im, m, min(w, WIDTH - 400), h)))
        rows.append(row)
    # flow heroes into rows that fit the sheet width
    cur, cw = [], GAP
    for name, im in heroes:
        if cur and cw + im.width + GAP > WIDTH:
            rows.append(cur)
            cur, cw = [], GAP
        cur.append((f"{name}  {im.width // s}x{im.height // s}", im))
        cw += im.width + GAP
    if cur:
        rows.append(cur)
    # lay out: rows of labelled images, wrapping a row that is too wide
    placed, y = [], GAP
    for row in rows:
        x, rh = GAP, 0
        for label, im in row:
            cw = max(im.width, int(font.getlength(label)))
            if x + cw > WIDTH - GAP and x > GAP:
                x, y = GAP, y + rh + GAP + 26
                rh = 0
            placed.append((x, y, label, im))
            x += cw + GAP
            rh = max(rh, im.height)
        y += rh + GAP + 26
    sheet = Image.new("RGBA", (WIDTH, y + GAP), BG + (255,))
    d = ImageDraw.Draw(sheet)
    for x, yy, label, im in placed:
        d.text((x, yy), label, fill=INK, font=font)
        sheet.alpha_composite(im, (x, yy + 24))
    dst.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(dst, optimize=True)
    print(f"{dst}: {sheet.width} x {sheet.height}")


if __name__ == "__main__":
    main()
