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


# --------------------------------------------------------------------------------------------------------------- sculpt
class Sculpt:
    """The organic panel's one surface, built in layers. Construct it with the panel's rectangles; read .height and
    .masks after .build(). The `layers` set decides how far the sculpt goes (the study shows each layer as a step):
    "bone", "muscle", "skin", "detail" (veins, cords, creases, pustules, fibres) and "cavern" (the receding ribcage)."""

    def __init__(self, view, res_mm, win, port, keys, neigh, cradle, pustules=(), seed=7):
        self.view, self.res = view, res_mm
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
            masks["crease"] = crease
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
                     depth=np.clip((S + 62.0) / 62.0, 0.0, 1.0))
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
