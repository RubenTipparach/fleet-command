#!/usr/bin/env python3
"""Fleet Command: the organic skin's hex-cell screen, its portrait pixels and its legibility measurement.

Owns the one definition of the fine hexagonal cell filter that organic screens draw over live pixels (owner,
2026-10-31, of the organic study's portrait: "I think the portrait should be like a hex grid filter on top of pixels. Hex
grid should be pretty fine and we can read text and images on it just fine"), the officer portrait the study shows
through it (our own design, drawn here from a height field, never traced from anything), and the measurement that
proves the text still reads. tools/blender/organic_study.py (step 25) builds the same filter as a shader from HEX
below, so the render, the page recipe and the numbers share one set of values.

    python3 tools/mockups/hex_portrait.py [--out docs/design/organic-bible]

Writes portrait-pixels.png (the live content, no filter, PX_PER_MM px per mm of the portrait window), hex-measure.jpg
(the label and a 12 px test line at 1x and 2x, without and with the filter, enlarged so the cells show) and
hex-measure.json (the WCAG contrast of the glyphs against the glass, without and with the filter, and how closely the
filtered letters keep their shape). Units: mm on the panel; the kit's 1x is 1 px per mm, its renders are 2x.
"""
import argparse
import json
import math
import os

import numpy as np

# The filter (rule H1 in the bible). A triangular lattice of cell centres PITCH_MM apart: each cell is a hexagon (the
# Voronoi cell of its centre); WALL is the dark wall's width as a fraction of the pitch; WALL_LEVEL how bright a wall is
# against the cell's middle (never black, so the image stays continuous); DOME how much brighter a cell's middle is than
# its rim (a faint lens, so the cells read as living cells, not a printed mesh).
HEX = {"pitch_mm": 2.0, "wall": 0.2, "wall_level": 0.45, "dome": 0.10}
PORT_MM = (62.0, 98.0)      # the portrait window, mm (tools/blender/organic_study.py PORT)
PX_PER_MM = 8               # the content is drawn at 8 px per mm, so the filter can be applied before any resampling
ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
READOUT = (0xb8, 0xf2, 0x7a)    # the study's readout colour (PAL["readout"])
GLASS = (5, 4, 5)               # the smoked glass behind the pixels (PAL["screen"])


def hex_mask(w, h, px_per_mm, pitch_mm=HEX["pitch_mm"], wall=HEX["wall"], wall_level=HEX["wall_level"],
             dome=HEX["dome"]):
    """The filter as a float array (h, w): 1 + dome at a cell's middle, falling to 1 at its rim, wall_level on its walls.
    Pointy-top hexagons on a lattice with neighbours at 0, 60 and 120 degrees, pitch_mm apart. The same arithmetic as the
    shader in tools/blender/organic_study.py: wrap the point into two offset rectangular lattices, keep the nearer
    centre, take the hexagonal distance, threshold it into a wall with a soft edge."""
    p = pitch_mm * px_per_mm
    sx, sy = p, p * math.sqrt(3.0)
    y, x = np.mgrid[0:h, 0:w].astype(np.float64) + 0.5

    def wrap(v, s):
        return (v + s / 2) % s - s / 2
    ax, ay = wrap(x, sx), wrap(y, sy)
    bx, by = wrap(x - sx / 2, sx), wrap(y - sy / 2, sy)
    use_a = ax * ax + ay * ay <= bx * bx + by * by
    cx, cy = np.where(use_a, ax, bx), np.where(use_a, ay, by)
    s3 = math.sqrt(3.0) / 2
    d = np.maximum(np.abs(cx), np.maximum(np.abs(0.5 * cx + s3 * cy), np.abs(0.5 * cx - s3 * cy))) / (p / 2)  # 0 middle, 1 rim
    edge = 1.0 - wall                       # the wall begins here (in units of the half pitch)
    soft = 0.35 * wall
    cell = np.clip((edge - d) / soft + 0.5, 0.0, 1.0)   # 1 in the cell, 0 in the wall, a soft edge between
    lens = 1.0 + dome * np.clip(1.0 - (d / edge) ** 2, 0.0, 1.0)
    return wall_level + (lens - wall_level) * cell


def _fbm(h, w, seed, octaves=((6, 1.0), (14, 0.5), (34, 0.25), (80, 0.12))):
    """Smooth seeded noise in 0-1: random grids of a few sizes, each smoothly upsampled, summed (skin grain)."""
    from PIL import Image
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w))
    for cells, amp in octaves:
        g = rng.random((cells, max(2, int(cells * w / h))))
        im = Image.fromarray((g * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
        out += amp * np.asarray(im) / 255.0
    return out / sum(a for _c, a in octaves)


def _shade_face(w, h):
    """The officer: a Vhorr matriarch drawn from a height field and lit, our own design. A long skull narrowing to a
    rounded point at the chin, chitin plates over the crown, a heavy brow, three crest horns swept back, deep sockets
    with slit eyes glowing life green, cheek ridges, two curved mandibles, a sinewed neck and armoured shoulders falling
    into the dark. Lit from below by the console and rimmed green from above right. Returns an sRGB float image."""
    y, x = np.mgrid[0:h, 0:w].astype(np.float64)
    u = (x + 0.5) / w * 2 - 1                      # -1 left, 1 right
    v = 1 - (y + 0.5) / h * 2                      # 1 top, -1 bottom
    dome = lambda r2, p=0.6: np.clip(1 - r2, 0, 1) ** p  # noqa: E731  (a rounded cap over r2 < 1)

    # the skull: an ellipse narrowed toward the chin, so it tapers to a rounded point
    k = 1 + 1.9 * 0.12 * np.log1p(np.exp((0.15 - v) / 0.12))      # a soft knee, so the taper has no seam
    skull = dome((u * k / 0.5) ** 2 + ((v - 0.14) / 0.64) ** 2)
    inside = skull > 0
    H = 0.62 * skull
    # neck and shoulders behind it, falling into the dark
    neck = dome((u / (0.15 + 0.05 * np.clip(-v - 0.5, 0, 1))) ** 2 + ((v + 0.66) / 0.34) ** 2)
    cords = 1 + 0.12 * (np.exp(-((np.abs(u) - 0.07) / 0.025) ** 2))         # two sinew cords down the neck
    H = np.maximum(H, 0.3 * neck * cords)
    for s in (-1, 1):     # armoured shoulders: a hump each side with three ridged plates
        sh = dome(((u - s * 0.62) / 0.5) ** 2 + ((v + 1.08) / 0.38) ** 2)
        ridge = (0.5 + 0.5 * np.cos(((u - s * 0.62) * 6.0 + (v + 1.08) * 4.0 * s) * math.pi)) ** 2
        H = np.maximum(H, sh * (0.3 + 0.07 * ridge))
    g = lambda cx, cy, sx, sy: np.exp(-(((u - cx) / sx) ** 2 + ((v - cy) / sy) ** 2))  # noqa: E731
    face = np.zeros_like(H)
    face += 0.12 * g(0, 0.24, 0.4, 0.06)                                     # the brow
    face += 0.07 * (g(-0.27, -0.1, 0.1, 0.22) + g(0.27, -0.1, 0.1, 0.22))     # cheek ridges
    face -= 0.16 * (g(-0.19, 0.11, 0.11, 0.055) + g(0.19, 0.11, 0.11, 0.055)) # sockets
    face += 0.05 * g(0, -0.2, 0.05, 0.25)                                     # the nose ridge
    crown_w = np.clip((v - 0.3) / 0.1, 0, 1) ** 2                          # chitin ridges across the crown, faded in
    face += 0.03 * crown_w * (0.5 + 0.5 * np.cos(2 * math.pi * (v - 0.33) / 0.085)) ** 2
    H = np.where(inside, H + face * skull ** 0.3, H)
    # three crest horns swept back from the crown, behind the skull (lower than it where they overlap)
    horn_m = np.zeros_like(H)
    for side, length, base in ((-1, 0.58, 0.075), (0, 0.36, 0.065), (1, 0.58, 0.075)):
        for t in np.linspace(0, 1, 60):
            cx = side * (0.1 + 0.48 * t + 0.05 * t * t)
            cy = 0.66 + (0.34 if side == 0 else 0.26) * t - (0.12 * t * t if side else 0)
            r = base * (1 - 0.92 * t)
            hh = 0.4 * (1 - 0.6 * t) * dome(((u - cx) ** 2 + (v - cy) ** 2) / r ** 2, 0.5)
            horn_m = np.maximum(horn_m, (hh > 0) * 1.0)
            H = np.where(hh > H, hh, H)
    for s in (-1, 1):     # mandibles: curved tusks from the jaw corners, hooking in under the chin
        for t in np.linspace(0, 1, 60):
            cx = s * (0.27 - 0.17 * t * t)
            cy = -0.3 - 0.34 * t
            r = 0.05 * (1 - 0.85 * t)
            hh = 0.5 * dome(((u - cx) ** 2 + (v - cy) ** 2) / r ** 2, 0.5)
            horn_m = np.maximum(horn_m, (hh > H) * 1.0)
            H = np.maximum(H, hh)
    grain = _fbm(h, w, 11)
    H = H + 0.012 * (grain - 0.5) * (H > 0.02)

    gy, gx = np.gradient(H * 70.0)
    n = np.dstack([-gx, gy, np.ones_like(H)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    unit = lambda a: np.array(a) / np.linalg.norm(a)  # noqa: E731
    key, rim, fill = unit([-0.3, -0.6, 0.74]), unit([0.65, 0.6, 0.45]), unit([0.0, 0.3, 1.0])
    lam = np.clip(n @ key, 0, 1)
    rl = np.clip(n @ rim, 0, 1) ** 4
    fl = np.clip(n @ fill, 0, 1)
    spec = np.clip(n @ unit(key + np.array([0, 0, 1.0])), 0, 1) ** 60
    body = np.clip((H - 0.01) / 0.03, 0, 1)
    tone = 0.75 + 0.5 * grain
    skin = np.array([0.24, 0.085, 0.075])[None, None] * tone[..., None]      # the frame's maroon-brown, not red
    chitin = np.array([0.16, 0.08, 0.09])
    crown = (0.7 * crown_w * np.clip((H - 0.3) / 0.15, 0, 1))[..., None]
    albedo = skin * (1 - crown) + chitin * crown
    albedo = albedo * (1 - horn_m[..., None]) + np.array([0.42, 0.36, 0.28]) * horn_m[..., None]  # horn and tusk bone
    occl = np.clip(0.55 + 6.0 * (H - (0.25 * np.roll(H, 9, 0) + 0.25 * np.roll(H, -9, 0) + 0.25 * np.roll(H, 9, 1)
                                        + 0.25 * np.roll(H, -9, 1))) + 0.45, 0.35, 1.0)
    col = albedo * (0.12 * fl + 1.25 * lam)[..., None] * occl[..., None] \
        + np.array([0.3, 0.8, 0.35]) * 0.45 * rl[..., None] + 0.28 * spec[..., None] * (0.5 + grain)[..., None]
    col *= body[..., None]
    col *= (np.clip((v + 0.98) / 0.4, 0, 1) ** 1.5)[..., None]                 # the shoulders fall into the dark
    # the eyes: slits glowing life green in the sockets, with a soft glow round them
    eye = np.zeros_like(H)
    for s in (-1, 1):
        du, dv = u - s * 0.19, v - 0.11
        ang = -s * 0.32
        ru, rv = du * math.cos(ang) - dv * math.sin(ang), du * math.sin(ang) + dv * math.cos(ang)
        eye = np.maximum(eye, np.clip(1 - (ru / 0.08) ** 2 - (rv / 0.02) ** 2, 0, 1) ** 0.5)
        eye = np.maximum(eye, 0.3 * np.exp(-((ru / 0.14) ** 2 + (rv / 0.07) ** 2)))
    col = col + np.array([0.5, 1.0, 0.28]) * eye[..., None]
    bg = np.array(GLASS) / 255.0 + np.exp(-((u / 0.8) ** 2 + ((v - 0.1) / 0.9) ** 2))[..., None] * np.array([0.05, 0.015, 0.025])
    col = col + bg * (1 - body[..., None])
    return np.clip(col, 0, 1)


def portrait_pixels(px_per_mm=PX_PER_MM):
    """The live content behind the filter: the officer's portrait, the name VHAAL and the rank line MATRIARCH, on the
    dark glass, as an sRGB uint8 image the size of the portrait window."""
    from PIL import Image, ImageDraw, ImageFont
    w, h = int(PORT_MM[0] * px_per_mm), int(PORT_MM[1] * px_per_mm)
    face_h = int(70 * px_per_mm)                    # the top 70 mm is the face; the text sits under it
    face = _shade_face(w, face_h)
    img = np.zeros((h, w, 3)) + np.array(GLASS) / 255.0
    img[:face_h] = face
    im = Image.fromarray((img * 255).round().astype(np.uint8))
    d = ImageDraw.Draw(im)
    name = ImageFont.truetype(FONT, int(10 * px_per_mm))
    rank = ImageFont.truetype(FONT, int(6 * px_per_mm))
    d.text((w / 2, 79 * px_per_mm), "VHAAL", font=name, fill=READOUT, anchor="mm")
    d.text((w / 2, 90.5 * px_per_mm), "MATRIARCH", font=rank, fill=tuple(int(c * 0.8) for c in READOUT), anchor="mm")
    return im


def apply_filter(im, px_per_mm):
    """Multiply the hex filter over an sRGB image in linear light, as the page and the shader do."""
    a = np.asarray(im).astype(np.float64) / 255.0
    lin = np.where(a <= 0.04045, a / 12.92, ((a + 0.055) / 1.055) ** 2.4)
    lin *= hex_mask(a.shape[1], a.shape[0], px_per_mm)[..., None]
    out = np.where(lin <= 0.0031308, lin * 12.92, 1.055 * np.clip(lin, 0, None) ** (1 / 2.4) - 0.055)
    return np.clip(out, 0, 1)


def _down(a, f):
    """Box-filter an (h, w, 3) float array down by the integer factor f (what a display does at that size)."""
    h, w = a.shape[0] // f * f, a.shape[1] // f * f
    return a[:h, :w].reshape(h // f, f, w // f, f, -1).mean(axis=(1, 3))


def _lum(a):
    lin = np.where(a <= 0.04045, a / 12.92, ((a + 0.055) / 1.055) ** 2.4)
    return lin @ np.array([0.2126, 0.7152, 0.0722])


def measure(out_dir):
    """The legibility of text under the filter, at the kit's 1x (1 px per mm) and 2x: the name label (10 mm) and a 12 px
    test line. For each: WCAG contrast of the glyph cores against the glass, without and with the filter, and the
    correlation of the filtered letters with the plain ones (1.0 is the same shape)."""
    from PIL import Image, ImageDraw, ImageFont
    s = PX_PER_MM
    lines = [("label", "VHAAL", 10.0), ("12 px line", "HULL 82%  SHIELDS 4  RANGE 3.5", 12.0 * 0.72)]
    res, tiles = {}, []
    for key, txt, cap_mm in lines:
        font = ImageFont.truetype(FONT, int(round(cap_mm / 0.72 * s)) if key == "12 px line" else int(cap_mm * s))
        w = int((font.getlength(txt) / s + 8) * s)
        h = int((cap_mm / 0.72 + 6) * s)
        im = Image.new("RGB", (w, h), GLASS)
        ImageDraw.Draw(im).text((w / 2, h / 2), txt, font=font, fill=READOUT, anchor="mm")
        plain = np.asarray(im).astype(np.float64) / 255.0
        filt = apply_filter(im, s)
        res[key] = {}
        for scale in (1, 2):
            f = s // scale
            p, q = _down(plain, f), _down(filt, f)
            lp, lq = _lum(p), _lum(q)
            glyph = lp > np.percentile(lp, 90) * 0.85
            back = lp < np.percentile(lp, 30) + 1e-6
            cr = lambda L: (L[glyph].mean() + 0.05) / (L[back].mean() + 0.05)  # noqa: E731
            corr = float(np.corrcoef(lp.ravel(), lq.ravel())[0, 1])
            res[key][f"{scale}x"] = {"contrast_plain": round(cr(lp), 1), "contrast_filtered": round(cr(lq), 1),
                                     "shape_correlation": round(corr, 3)}
            tiles.append((f"{key}, {scale}x, plain", p, 8 // scale))
            tiles.append((f"{key}, {scale}x, hex filter", q, 8 // scale))
    # a contact sheet, each tile enlarged by nearest neighbour so a 1x px is 8 screen px: a short text's plain and
    # filtered tiles side by side on one row, a long line's stacked
    blocks = []                                      # rows of (caption, array) placed left to right
    for k in range(0, len(tiles), 2):
        (lp_, a, z), (lf_, b, _z) = tiles[k], tiles[k + 1]
        A, B = np.kron(a, np.ones((z, z, 1))), np.kron(b, np.ones((z, z, 1)))
        if A.shape[1] < 900:
            blocks.append([(lp_, A), (lf_, B)])
        else:
            blocks += [[(lp_, A)], [(lf_, B)]]
    W = max(sum(t.shape[1] + 20 for _c, t in row) for row in blocks) + 10
    Ht = sum(max(t.shape[0] for _c, t in row) + 50 for row in blocks)
    sheet = Image.new("RGB", (W, Ht), (16, 12, 14))
    d = ImageDraw.Draw(sheet)
    lab = ImageFont.truetype(FONT, 28)
    yy = 0
    for row in blocks:
        xx = 10
        for cap, t in row:
            d.text((xx, yy + 8), cap, font=lab, fill=(230, 200, 140))
            sheet.paste(Image.fromarray((t * 255).round().astype(np.uint8)), (xx, yy + 46))
            xx += t.shape[1] + 20
        yy += max(t.shape[0] for _c, t in row) + 50
    sheet.save(os.path.join(out_dir, "hex-measure.jpg"), quality=90)
    return res


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(ROOT, "docs", "design", "organic-bible"))
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    portrait_pixels().save(os.path.join(a.out, "portrait-pixels.png"))
    res = {"filter": HEX, "measure": measure(a.out)}
    with open(os.path.join(a.out, "hex-measure.json"), "w") as f:
        json.dump(res, f, indent=2)
    print(json.dumps(res, indent=2))


if __name__ == "__main__":
    main()
