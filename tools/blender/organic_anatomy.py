"""Fleet Command: layered anatomy for organic panels, sculpted as one surface (numpy only, no Blender).

Owns the organic study's sculpt from step 15 on (tools/blender/organic_study.py; docs/design/organic-bible.html). The owner,
2026-10-31, on the first study: "Your approach used discrete shapes. The zerg have a couple layers. There's a bone
underneath. The leather stretches over muscle and bone with veins underneath. One thing you could try is sculpting", then
of the bead row, the white spine and the glossy pustules: "These look like low effort shapes. Whereas the organic leather
frame looks a little more purposeful and organic".

So nothing is placed on the body. The frame is ONE height field z = h(x, y), built the way a sculptor builds anatomy:

1. bone: an armature of ridges round every opening, a vertebral ridge of varied vertebrae with shadowed hollows between,
   ribs, an arch over the portrait, the shelf under the keys, and (optionally) the cavern of ribs behind it all;
2. muscle: swollen bundles beside and between the bones, following the frame's direction, striated along it;
3. skin: one surface stretched over both (a smooth maximum, then a blur that bridges the gaps between crests as webs in
   tension, tight over every crest so the skin thins there), with veins and cords raised under it, pustules swelling it,
   creases at the foot of every ridge, and the openings sunk into it.

Besides the height, it returns the masks the material blends by (all 0-1, per sample): bone showing through thin skin,
muscle (raw sinew in the hollows), veins, creases (concavity), web (skin bridging a gap, so in tension), pustules, and a
normalised depth for the cavern's falloff into shadow. A sculpt is a pure function of its inputs: the same arguments give
the same field, bit for bit (no global random state; every jitter comes from a seeded generator).

Units: mm; x right, y up, z toward the camera. Rectangles are (x0, y0, x1, y1).
"""

import math

import numpy as np


# ---------------------------------------------------------------------------------------------------------- primitives
def sd_box(X, Y, r, round_mm=0.0):
    """Signed distance (mm) from each sample to the rectangle r, its corners rounded by round_mm; negative inside."""
    x0, y0, x1, y1 = r
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    hx, hy = (x1 - x0) / 2 - round_mm, (y1 - y0) / 2 - round_mm
    qx, qy = np.abs(X - cx) - hx, np.abs(Y - cy) - hy
    out = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0))
    return out + np.minimum(np.maximum(qx, qy), 0) - round_mm


def sd_segment(X, Y, a, b):
    """Distance (mm) from each sample to the segment a-b, and the parameter t (0 at a, 1 at b) of the nearest point."""
    ax, ay = a
    bx, by = b
    dx, dy = bx - ax, by - ay
    t = np.clip(((X - ax) * dx + (Y - ay) * dy) / max(dx * dx + dy * dy, 1e-9), 0.0, 1.0)
    return np.hypot(X - ax - t * dx, Y - ay - t * dy), t


def bell(d, w):
    """A smooth round bump profile: 1 at d = 0, falling to 0 at |d| = w (a raised-cosine)."""
    u = np.clip(np.abs(d) / w, 0.0, 1.0)
    return 0.5 * (1.0 + np.cos(math.pi * u))


def rise(under, top, prof):
    """Grow a form out of the surface beneath it: where prof (0-1) is 1 the surface reaches top (mm), where it is 0 it
    stays as it was, so every ridge, bundle and cord rises from whatever lies under it and never stands on a cliff."""
    return np.maximum(under, under + (top - under) * prof)


def smax(a, b, k):
    """Smooth maximum of two fields with blend radius k (mm): where they meet, they flow into each other."""
    h = np.clip(0.5 + 0.5 * (a - b) / k, 0.0, 1.0)
    return b + (a - b) * h + k * h * (1.0 - h)


def blur(f, sigma_px):
    """A Gaussian blur (sigma in samples), by FFT with edge padding, so the field's borders are not pulled down."""
    if sigma_px <= 0:
        return f
    p = int(math.ceil(3 * sigma_px))
    g = np.pad(f, p, mode="edge")
    ny, nx = g.shape
    ky = np.fft.fftfreq(ny)[:, None]
    kx = np.fft.fftfreq(nx)[None, :]
    filt = np.exp(-2.0 * (math.pi ** 2) * (sigma_px ** 2) * (kx ** 2 + ky ** 2))
    out = np.real(np.fft.ifft2(np.fft.fft2(g) * filt))
    return out[p:-p, p:-p]


DEPTH_MM = 62.0   # how deep the anatomy material's shadow ramp reaches: the depth mask is 0 at -62 mm, 1 at the face


def depth_mask(S):
    """The depth mask the anatomy material darkens by (tools/blender/organic_study.py anatomy()): one ramp for the study and
    every organic kit, so a hollow of a given depth is as dark in a kit piece as in the accepted widget. (Version 25's kit
    used a 30 mm ramp: its hollows fell twice as dark, and the body read as one dark brown.)"""
    return np.clip((S + DEPTH_MM) / DEPTH_MM, 0.0, 1.0)


def value_noise(X, Y, scale_mm, rng):
    """Smooth value noise (0-1) with features about scale_mm across, from a seeded generator."""
    gx = int((X.max() - X.min()) / scale_mm) + 3
    gy = int((Y.max() - Y.min()) / scale_mm) + 3
    grid = rng.random((gy, gx))
    u = (X - X.min()) / scale_mm
    v = (Y - Y.min()) / scale_mm
    i, j = np.floor(u).astype(int), np.floor(v).astype(int)
    fu, fv = u - i, v - j
    fu, fv = fu * fu * (3 - 2 * fu), fv * fv * (3 - 2 * fv)
    a, b = grid[j, i], grid[j, i + 1]
    c, d = grid[j + 1, i], grid[j + 1, i + 1]
    return (a * (1 - fu) + b * fu) * (1 - fv) + (c * (1 - fu) + d * fu) * fv


def path_field(X, Y, pts, radius_mm, radii=None):
    """A tube's cross-section raised along the polyline pts: its height profile (0-1) at each sample, the tube's own
    radius tapering by radii (one factor per point). Evaluated only near each segment, so long paths stay cheap."""
    out = np.zeros_like(X)
    radii = radii or [1.0] * len(pts)
    dxs = X[0, 1] - X[0, 0]
    dys = Y[1, 0] - Y[0, 0]
    for (a, ra), (b, rb) in zip(zip(pts, radii), zip(pts[1:], radii[1:])):
        rmax = radius_mm * max(ra, rb)
        x0, x1 = min(a[0], b[0]) - rmax, max(a[0], b[0]) + rmax
        y0, y1 = min(a[1], b[1]) - rmax, max(a[1], b[1]) + rmax
        i0 = max(int((x0 - X[0, 0]) / dxs), 0)
        i1 = min(int((x1 - X[0, 0]) / dxs) + 2, X.shape[1])
        j0 = max(int((y0 - Y[0, 0]) / dys), 0)
        j1 = min(int((y1 - Y[0, 0]) / dys) + 2, X.shape[0])
        if i0 >= i1 or j0 >= j1:
            continue
        xs, ys = X[j0:j1, i0:i1], Y[j0:j1, i0:i1]
        d, t = sd_segment(xs, ys, a, b)
        r = radius_mm * (ra + (rb - ra) * t)
        prof = np.sqrt(np.clip(1.0 - (d / np.maximum(r, 1e-6)) ** 2, 0.0, 1.0)) * (r / radius_mm)
        out[j0:j1, i0:i1] = np.maximum(out[j0:j1, i0:i1], prof)
    return out


def ring(X, Y, r, gap, width, round_mm=8.0):
    """A ridge running round the rectangle r at gap mm outside it, width mm wide: its 0-1 profile."""
    return bell(sd_box(X, Y, r, round_mm) - gap, width)


def dense_anatomy(X, Y, px, mass, openings, rng, scale=1.0, quiet=None):
    """The frame body's dense anatomy, as a height to add to the finished skin (mm) and the masks it implies. The owner,
    2026-10-31, of the accepted widget (step 26): "The header looks better but the body needs work. The body looks soft,
    and has very little details. Look at how the dividers work in the example. It has lots of details. Lots of muscle
    features, underlying bone, folded wrinkly skins", then "And detail that look like pores or goosebumps?". So the body
    gets oriented anatomy that follows the frame, never an even all-over bump:

    - muscle bundles running along the frame (concentric to the openings it holds), separated by grooves, merging and
      splitting (their spacing warped by noise), each striated with fine fibres along its run;
    - bone under taut skin: rib-like transverse ridges at a short pitch in a band round each opening, and knuckle knobs at
      the openings' corners;
    - folded skin: compression wrinkles across the bundles (short, perpendicular to the frame), and concentric folds
      close round each opening.

    X, Y: the sample grid (mm); px: samples per mm; mass: 0-1 where the frame's flesh is; openings: rectangles (x0, y0,
    x1, y1) the frame wraps; scale: feature size against the study's (a console kit seen at page scale draws them larger);
    quiet: 0-1 where nothing is added (the vertebral header, which the owner liked as it is).
    Returns (dS, rib, fold, fibre): the height to add, where bone shows through (rib ridges, knuckles), where the skin
    folds (a crease mask), and the fibre stripes (for the material's own direction)."""
    if not openings:
        z = np.zeros_like(X)
        return z, z, z, z
    d = np.full_like(X, 1e9)
    for r in openings:
        d = np.minimum(d, sd_box(X, Y, r, 4.0))
    ds = blur(d, 2.0 * px)
    gy, gx = np.gradient(ds, 1.0 / px)
    along_x = np.abs(gy) > np.abs(gx)                       # the frame runs along x here (above or below an opening)
    t = np.where(along_x, X, Y)                              # the coordinate along the frame
    # the frame band only (a cradle or a far lobe of flesh is left as it is)
    env = np.clip(mass, 0.0, 1.0) * np.clip((d - 2.0) / 2.0, 0.0, 1.0) * np.clip((75.0 * scale - d) / (12.0 * scale), 0, 1)
    if quiet is not None:
        env = env * np.clip(1.0 - quiet, 0.0, 1.0)
    w1 = value_noise(X, Y, 14.0 * scale, rng) - 0.5          # slow warp: bundles merge and split
    w2 = value_noise(X, Y, 4.0 * scale, rng) - 0.5           # quick wobble
    wt = value_noise(X, Y, 22.0 * scale, rng) - 0.5          # irregular spacing along the frame
    amp = 0.45 + 0.9 * value_noise(X, Y, 16.0 * scale, rng)  # bundles swell and thin along their run
    # muscle bundles along the frame, of uneven width, grooves between them
    pitch_b = 11.0 * scale
    ph = (d + 5.0 * scale * w1 + 1.2 * scale * w2) / pitch_b
    band = 0.5 + 0.5 * np.cos(2 * math.pi * ph)
    bundles = band ** 0.45                                  # rounded tops, narrow deep grooves
    dS = 3.2 * scale * (bundles - 0.6) * amp * env
    # fibres along each bundle (striations), faint, on the bundles' crowns only
    fibre = np.sin(2 * math.pi * (d + 1.0 * scale * w2) / (2.2 * scale)) * band ** 2
    dS += 0.22 * scale * fibre * env
    # coarser striations (tendons) along the bundles: the skin over muscle is never a smooth shell
    tend = np.sin(2 * math.pi * (d + 2.5 * scale * w1) / (3.6 * scale)) * (0.4 + 0.6 * band)
    dS += 0.45 * scale * tend * env
    # bone under taut skin: transverse ribs in patches round each opening, irregularly spaced along the frame
    rb = np.clip((d - 3.0 * scale) / (2.0 * scale), 0, 1) * (0.55 + 0.45 * np.clip((18.0 * scale - d) / (6.0 * scale), 0, 1))
    patch = np.clip((value_noise(X, Y, 34.0 * scale, rng) - 0.46) / 0.16, 0, 1)
    rib = (0.5 + 0.5 * np.cos(2 * math.pi * (t + 22.0 * scale * wt + 3.0 * scale * w2) / (17.0 * scale))) ** 4
    rib = rib * rb * patch * env * (0.45 + 0.9 * value_noise(X, Y, 7.0 * scale, rng))   # each rib its own size
    dS += 3.4 * scale * rib
    # knuckles of bone at every opening's corners, sized each their own way
    knuck = np.zeros_like(X)
    for (x0, y0, x1, y1) in openings:
        off = 5.5 * scale
        for cx, cy in ((x0 - off, y0 - off), (x1 + off, y0 - off), (x0 - off, y1 + off), (x1 + off, y1 + off)):
            knuck = np.maximum(knuck, bell(np.hypot(X - cx, Y - cy), rng.uniform(3.5, 6.0) * scale) * rng.uniform(0.6, 1.0))
    knuck *= np.clip(mass, 0, 1) * (1.0 if quiet is None else np.clip(1.0 - quiet, 0, 1))
    dS += 3.8 * scale * knuck
    # folded skin: compression wrinkles where the frame bends (its corners), short and across the frame ...
    bend = 1.0 - np.abs(np.abs(gx) - np.abs(gy)) / np.maximum(np.abs(gx) + np.abs(gy), 1e-6)
    bend = np.clip(blur(bend, 3.0 * px) * 1.6 - 0.2, 0, 1)
    nW = value_noise(t / 0.3, d, 7.0 * scale / 0.3, rng)
    wrink = np.clip((1.0 - np.abs(2.0 * nW - 1.0) - 0.84) / 0.16, 0.0, 1.0) * np.clip(bend + 0.25 * patch, 0, 1)
    # ... and one or two folds close round every opening, where the skin dives in
    near = np.clip(1.0 - (d - 2.0) / (5.0 * scale), 0.0, 1.0)
    conc = np.clip(np.cos(2 * math.pi * (d + 1.0 * scale * w2) / (3.2 * scale)), 0.0, 1.0) ** 8 * near * (0.4 + 0.6 * amp)
    fold = np.clip(wrink + conc, 0.0, 1.0) * env
    dS -= 1.6 * scale * fold
    return dS, np.clip(rib * 1.2 + knuck, 0, 1), fold, fibre * env


# --------------------------------------------------------------------------------------------------------------- sculpt
class Sculpt:
    """The organic panel's one surface, built in layers. Construct it with the panel's rectangles; read .height and
    .masks after .build(). The `layers` set decides how far the sculpt goes (the study shows each layer as a step):
    "bone", "muscle", "skin", "detail" (veins, cords, creases, pustules, fibres) and "cavern" (the receding ribcage)."""

    def __init__(self, view, res_mm, win, port, keys, neigh, cradle, pustules=(), seed=7, overlay=False):
        self.view, self.res = view, res_mm
        self.overlay = overlay   # an overlay over gameplay: nothing runs off into a wall that is not there
        self.pustules = list(pustules)   # (x, y, radius mm): sacs of light under the skin
        self.win, self.port, self.keys, self.neigh, self.cradle = win, port, keys, neigh, cradle
        self.seed = seed
        x0, y0, x1, y1 = view
        xs = np.arange(x0, x1 + res_mm * 0.5, res_mm)
        ys = np.arange(y0, y1 + res_mm * 0.5, res_mm)
        self.X, self.Y = np.meshgrid(xs, ys)
        self.px = 1.0 / res_mm  # samples per mm
        # a warped copy of the coordinates, for every frame form (never the openings): nothing runs parallel to a
        # rectangle's edge for long, as nothing grown does
        wr = np.random.default_rng(seed + 9)
        self.Xw = self.X + 9.0 * (value_noise(self.X, self.Y, 34.0, wr) - 0.5) + 3.0 * (value_noise(self.X, self.Y, 11.0, wr) - 0.5)
        self.Yw = self.Y + 9.0 * (value_noise(self.X, self.Y, 34.0, wr) - 0.5) + 3.0 * (value_noise(self.X, self.Y, 11.0, wr) - 0.5)

    # ------------------------------------------------------------------ bone
    def bone(self, cavern):
        X, Y, rng = self.X, self.Y, np.random.default_rng(self.seed)
        w0, w1 = self.win, self.port
        floor = -30.0
        B = np.full_like(X, floor)
        if cavern:   # the ribcage the panels are set into, receding into the dark: deep between its ribs
            B = np.full_like(X, -62.0)
            for k in range(16):
                yy = self.view[1] - 10 + k * 22 + rng.uniform(-3, 3)
                bow = rng.uniform(5, 11)
                pts = [(self.view[0] - 30 + j * 40, yy + bow * math.sin(j * 0.55 + k) - 0.05 * (j * 40)) for j in range(17)]
                rad = [0.6 + 0.4 * math.sin(j * 0.9 + k * 1.3) ** 2 for j in range(17)]
                B = rise(B, -30.0, path_field(X, Y, pts, 9.0, rad))
            B = rise(B, -22.0, path_field(X, Y, [(-236, self.view[1] - 20), (-238, 30), (-232, self.view[3] + 20)], 13.0))
        # the armature: a ridge round every opening, the screen's the heaviest
        for r, gap, width, h in ((w0, 15.0, 7.0, 13.0), (w1, 13.0, 6.0, 11.0), (self.neigh, 13.0, 6.0, 10.0)):
            B = rise(B, h - 2.0, ring(self.Xw, self.Yw, r, gap, width) ** 0.8)
        # the shelf under the keys
        d, _ = sd_segment(X, Y, (self.keys[0][0] - 14, self.keys[0][1] - 14), (self.keys[-1][2] + 14, self.keys[0][1] - 14))
        B = rise(B, 8.0, bell(d, 7.0) ** 0.8)
        # the vertebral ridge along the screen's top, and down the portrait's outer side: vertebrae of varied size, each a
        # body with a sharper spinous crest, a shadowed hollow between each pair
        def vertebrae(under, points, size_lo, size_hi, lean):
            out = under
            for (cx, cy) in points:
                s = rng.uniform(size_lo, size_hi)
                a = math.radians(rng.uniform(-28.0, 28.0))        # each vertebra turned its own way, its own shape
                ax, ay = rng.uniform(13.0, 18.0) * s, rng.uniform(8.0, 12.0) * s
                u = (X - cx) * math.cos(a) + (Y - cy) * math.sin(a)
                v = -(X - cx) * math.sin(a) + (Y - cy) * math.cos(a)
                body = (u / ax) ** 2 + (v / ay) ** 2
                out = rise(out, 12.0 + 6.0 * s, np.clip(1 - body, 0, 1) ** 0.8)
                sx, sy = cx + lean[0] * s, cy + lean[1] * s
                # the spinous crest: a blade along the ridge, not a stud
                cu = (X - sx) * math.cos(a) + (Y - sy) * math.sin(a)
                cv = -(X - sx) * math.sin(a) + (Y - sy) * math.cos(a)
                crest = (cu / (3.0 * s)) ** 2 + (cv / (9.0 * s)) ** 2
                out = rise(out, 13.0 + 8.0 * s, np.clip(1 - crest, 0, 1) ** 1.2)
            return out
        top = []
        xx = w0[0] - 34.0
        while xx < w0[2] + 34.0:
            t = (xx - w0[0] + 34) / (w0[2] - w0[0] + 68)
            top.append((xx, w0[3] + 25.0 + 7.0 * math.sin(math.pi * t)))
            xx += 23.0 + rng.uniform(-5.0, 6.0)
        self.top = top
        B = rise(B, 14.0, path_field(X, Y, top, 12.0))    # the spine the vertebrae ride on: one ridge, not a row
        B = vertebrae(B, top, 0.7, 1.4, (0.0, 7.0))
        side = [(w1[0] - 23.0, w1[3] + 2.0 - k * 22.0) for k in range(5)]
        B = rise(B, 10.0, path_field(X, Y, side, 9.0))
        B = vertebrae(B, side, 0.65, 1.0, (-6.0, 0.0))
        # ribs off the screen's right side, curving down and back (the right is lighter than the left: asymmetry)
        for k in range(6):
            y = w0[3] - 4 - k * 14.0
            pts = [(w0[2] + 12, y), (w0[2] + 26, y - 3), (w0[2] + 38 + 3 * k, y - 10 - k)]
            B = rise(B, 7.0, path_field(X, Y, pts, 3.2, [1.0, 0.85, 0.35]))
        # the arch over the portrait: a bow of bone, thickest at its crown, its two feet sunk beside the pocket
        pcx = (w1[0] + w1[2]) / 2
        arch = [(pcx + 44 * math.cos(a), w1[3] + 8 + 34 * math.sin(a)) for a in np.linspace(0.05, math.pi - 0.05, 15)]
        B = rise(B, 15.0, path_field(X, Y, arch, 6.5, [0.6 + 0.5 * math.sin(math.pi * k / 14) for k in range(15)]))
        # horns: the arch's crown and the screen's right corner grow crests that reach past the frame (the silhouette
        # breaks the rectangle; the horns are bone crests in the one surface, never objects laid on it)
        for pts, rad, h in (([(pcx + 10, w1[3] + 42), (pcx + 40, w1[3] + 64), (pcx + 90, w0[3] + 70), (pcx + 140, w0[3] + 78)], 7.0, 14.0),
                            ([(w0[2] + 26, w0[3] + 20), (w0[2] + 52, w0[3] + 40), (w0[2] + 70, w0[3] + 62)], 6.0, 12.0)):
            B = rise(B, 2.0 + h, path_field(X, Y, pts, rad, [1.0, 0.8, 0.5, 0.15][:len(pts)]))
        # claws under the key shelf, curled in
        for sgn, cx in ((-1, self.keys[0][0] - 30), (1, self.keys[-1][2] + 30)):
            pts = [(cx, self.keys[0][1] - 12), (cx + 12 * sgn, self.keys[0][1] - 28), (cx + 4 * sgn, self.keys[0][1] - 42)]
            B = rise(B, 10.0, path_field(X, Y, pts, 5.5, [1.0, 0.6, 0.12]))
        return B

    # ---------------------------------------------------------------- muscle
    def muscle(self, B):
        X, Y = self.X, self.Y
        rng = np.random.default_rng(self.seed + 1)
        swell = value_noise(X, Y, 26.0, rng)
        lobes = value_noise(X, Y, 13.0, rng)
        Xw, Yw = self.Xw, self.Yw
        M = B.copy()
        # the flesh mass: a plateau round every opening and over the shelf, the neck and the cradle
        mass = np.full_like(X, 1e9)
        for r, grow in ((self.win, 38.0), (self.port, 30.0), (self.neigh, 28.0)):
            mass = np.minimum(mass, sd_box(Xw, Yw, r, 10.0) - grow)
        k0, k1 = self.keys[0], self.keys[-1]
        mass = np.minimum(mass, sd_box(X, Y, (k0[0] - 18, k0[1] - 26, k1[2] + 18, k0[3] + 10), 10.0))
        mass = np.minimum(mass, np.hypot(X - self.cradle[0], Y - self.cradle[1]) - self.cradle[2])
        self.mass = np.clip(-mass / 12.0, 0.0, 1.0)
        M = rise(M, 3.0 + 8.0 * swell, self.mass ** 0.6)
        # bundles beside the bone ring, swelling and thinning along their run, striated along it
        for r, gaps in ((self.win, ((12.0, 14.0), (31.0, 9.0))), (self.port, ((11.0, 12.0), (27.0, 8.0))),
                        (self.neigh, ((11.0, 12.0), (27.0, 8.0)))):
            d = sd_box(Xw, Yw, r, 8.0)
            for g, wdt in gaps:
                bundle = bell(d - g, wdt) * (0.45 + 0.6 * swell + 0.5 * lobes)
                fib = 0.35 * np.sin(2 * math.pi * d / 2.3)
                M = rise(M, 12.0 + fib, np.clip(bundle, 0.0, 1.0) * self.mass)
        # the sinew cords between panels: the tendril to the neighbour, knotted, and the cords running to the edges
        cords = [([(self.win[2] + 20, 10), (150, 0), (175, -14), (200, -6), (self.neigh[0] - 12, 8)], 6.0, [1.0, 0.45, 0.85, 0.4, 0.95]),
                 ([(-245, 120), (-200, 96), (-160, 112), (-118, 98)], 4.5, None),
                 ([(-170, -112), (-120, -98), (-60, -114), (20, -106), (120, -118), (205, -102)], 4.0, None),
                 ([(self.win[2] + 30, 100), (170, 122), (215, 116), (265, 132)], 4.0, None)]
        if self.overlay:   # only the tendril that joins two panels stays; the cords that ran into the wall go with it
            cords = cords[:1]
        for pts, rad, radii in cords:
            M = M + 2.0 * rad * path_field(X, Y, pts, rad, radii)
        # the cradle the crystal grows from
        cx, cy, cr = self.cradle
        M = rise(M, 16.0, bell(np.hypot((X - cx) / 1.3, Y - cy), cr))
        return M

    # ------------------------------------------------------------------ build
    def build(self, layers):
        X, Y = self.X, self.Y
        cavern = "cavern" in layers
        B = self.bone(cavern)
        if "muscle" in layers:
            M = self.muscle(B)
            core = smax(B, M, 2.5)
        else:
            M = np.full_like(X, -99.0)
            self.mass = np.zeros_like(X)
            core = B
        masks = {}
        if "skin" in layers:
            drape = blur(core, 6.0 * self.px)           # the skin bridges the gaps between crests: webs in tension
            S = np.maximum(core + 0.9, drape - 0.6)     # tight over every crest (+0.9 mm of skin), webbed between them
            S = blur(S, 0.6 * self.px)
            web = np.clip((drape - 0.6 - core - 0.9) / 3.0, 0.0, 1.0)
        else:
            S, web = core.copy(), np.zeros_like(X)
        thin = np.clip(1.0 - (S - B - 0.9) / 1.4, 0.0, 1.0) * np.clip(1.0 - (M - B) / 2.0, 0.0, 1.0)
        thin *= np.clip((B + 4.0) / 6.0, 0.0, 1.0)       # bone shows only where it stands proud, not in the cavern's dark
        thin *= np.clip((B - blur(B, 5.0 * self.px)) / 2.5, 0.0, 1.0)   # and only on its crests, where it stands proud locally
        musc = np.clip((M - B) / 3.0, 0.0, 1.0) * np.clip(1.0 - (S - M) / 2.5, 0.0, 1.0)
        dense_fold = np.zeros_like(X)
        if "dense" in layers:   # step 27: the body's dense anatomy (bundles, fibres, ribs, knuckles, folds); the header stays
            header = np.clip(path_field(X, Y, self.top, 22.0) * 1.6, 0.0, 1.0) if getattr(self, "top", None) else None
            dS, rib, dense_fold, _fib = dense_anatomy(X, Y, self.px, self.mass, [self.win, self.port, self.neigh] + list(self.keys),
                                                       np.random.default_rng(self.seed + 5), 1.0, header)
            S = S + dS
            thin = np.maximum(thin, 0.75 * rib)
        veins = np.zeros_like(X)
        pust = np.zeros_like(X)
        if "detail" in layers:
            rng = np.random.default_rng(self.seed + 2)
            # veins under the skin: seeded random walks over the flesh mass, branching, raised a little and dark
            for v in range(26):
                px, py = rng.uniform(self.view[0] + 10, self.view[2] - 10), rng.uniform(self.view[1] + 10, self.view[3] - 10)
                ang = rng.uniform(0, 2 * math.pi)
                pts = [(px, py)]
                for _s in range(int(rng.uniform(8, 20))):
                    ang += rng.normal(0, 0.45)
                    px, py = px + 4.0 * math.cos(ang), py + 4.0 * math.sin(ang)
                    pts.append((px, py))
                veins = np.maximum(veins, path_field(X, Y, pts, rng.uniform(0.7, 1.3), [1.0 - 0.6 * k / len(pts) for k in range(len(pts))]))
            veins *= np.clip(self.mass * 1.5 + web, 0.0, 1.0)
            S = S + 0.7 * veins
            # pustules: the skin swelling over a sac of light, at the joints
            for (cx, cy, r) in self.pustules:
                p = bell(np.hypot(X - cx, Y - cy), r)
                pust = np.maximum(pust, p)
                S = S + 7.0 * p ** 1.3
            # creases at the foot of every ridge: where the surface is concave, sink it a little
            lap = blur(S, 1.2 * self.px)
            lap = (np.roll(lap, 1, 0) + np.roll(lap, -1, 0) + np.roll(lap, 1, 1) + np.roll(lap, -1, 1) - 4 * lap) * self.px ** 2
            crease = np.clip(lap / 0.25, 0.0, 1.0)
            S = S - 0.5 * crease
            masks["crease"] = np.maximum(crease, dense_fold)
        else:
            masks["crease"] = np.zeros_like(X)
        # the openings sink into the surface: the skin dives into each hole, steeply, so nothing overhangs a readout
        for r in [self.win, self.port, self.neigh] + list(self.keys):
            d = sd_box(X, Y, r, 4.0)
            lip = -14.0 + 5.0 * np.maximum(d - 2.6, 0.0)
            S = np.minimum(S, lip)
            for m in (thin, musc, veins, pust):
                m *= np.clip((d - 1.5) / 2.0, 0.0, 1.0)
        masks.update(bone=thin, muscle=musc, vein=veins, web=web, pustule=pust,
                     depth=depth_mask(S))
        self.height, self.masks = S, masks
        return S, masks


    def at(self, x, y):
        """The sculpted height (mm) at (x, y), bilinear."""
        i = (x - self.view[0]) / self.res
        j = (y - self.view[1]) / self.res
        i0, j0 = int(math.floor(i)), int(math.floor(j))
        fi, fj = i - i0, j - j0
        h = self.height
        return float((h[j0, i0] * (1 - fi) + h[j0, i0 + 1] * fi) * (1 - fj) + (h[j0 + 1, i0] * (1 - fi) + h[j0 + 1, i0 + 1] * fi) * fj)
