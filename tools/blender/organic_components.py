"""Fleet Command: parametric sculptors for organic console pieces, at any size (numpy only, no Blender).

Owns how every organic kit piece is grown from its size. The owner, 2026-10-31: "due to the nature of this ui, nine
slice is not possible and the UI will have to be bocked out using placeholders then a full detail agent will go in and
sculpt components manually", then "If you structured your sculpting tools well. Then we should be able to sculpt panels,
buttons and elements of any size". So nothing here is drawn for one slot: each sculptor takes a rectangle and the
protected rectangles inside it and lays out its anatomy in millimetres from them (how many vertebrae a spine has, how far
apart the ribs are, how tall a ridge stands, where a sac of light sits), with variation seeded from the slot's id, so two
keys of one size are siblings, not clones, and a panel twice as long has twice the vertebrae, not stretched ones. A new
size, a new panel or a new screen shape is a re-run of tools/blender/build_ui_vhorr_organic.py, never new sculpting code.

The sculpt follows the Organic Panel Bible (docs/design/organic-bible.html): rule 1, no raw primitive is ever visible
(every form rises out of the one height field, and only the instruments, the key caps, the glass and the crystal, are
separate objects, placed by the Blender script from the `objects` this module lists); rule 2, layered anatomy (bone, then
muscle, then one skin stretched over both, veins and sacs of light under it: tools/blender/organic_anatomy.py's
primitives); rule 4, sculpted at the exact size; rule 5, an overlay: outside the pieces' bodies the field drops to the
floor, which the renderer cuts away, so the game shows between them. Every protected rectangle (a readout, a label, a
key face, the portrait window, a drawing) becomes an opening the skin dives into, and rule G1 (growth at least 2 mm clear)
is measured on the finished field by `clearance`.

Units: mm, and 1 mm is 1 CSS px of the 1920 x 1080 console. Page coordinates: x right, y DOWN (the layout's own), z
toward the viewer. Rectangles are (x, y, w, h) at the API and (x0, y0, x1, y1) inside.

    f = Field((0, 0, 472, 1080), res_mm=0.5, seed=7)
    panel(f, (6, 644, 460, 174), windows=[...], seed="panel.weapons")
    key(f, (40, 688, 192, 48), seed="key.fire", state="hover")
    S, masks = f.finish()
"""

import math
import zlib

import numpy as np

import organic_anatomy as oa

FLOOR_MM = -30.0          # the floor: cut away by the renderer (rule 5), so nothing stands behind the pieces
GLASS_Z = -6.0            # smoked glass sits 6 mm into the body
KEY_TOP_Z = 1.5           # a key cap's face at rest
DENSE_SCALE = 1.5         # the dense anatomy's feature size against the study's (step 27), for the console's page scale
FEATURE = 1.5             # anatomy drawn this many times the study's size where the room allows (vertebrae, horns, ribs,
                          # claws, lobes, veins), so it reads at the console's page scale (about 0.65 px per mm)
G1_MM = 2.0               # growth keeps this far from every protected rectangle (the bible's rule G1)


def resample(pts, step):
    """A Catmull-Rom curve through pts, sampled about every step mm (a smooth path for a tentacle)."""
    if len(pts) < 2:
        return list(pts)
    P = [pts[0]] + list(pts) + [pts[-1]]
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        L = math.hypot(p2[0] - p1[0], p2[1] - p1[1])
        n = max(2, int(L / step))
        for k in range(n):
            t = k / n
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2
                                    + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3) for j in (0, 1)))
    out.append(tuple(pts[-1]))
    return out


def curl(start, angle, length, bend, rng, n=7):
    """A curling path: from start, heading angle (radians, y down), length mm, turning more and more by bend (radians
    over the length, its sign the way it curls), with a little seeded wander."""
    x, y = start
    pts = [(x, y)]
    a = angle
    step = length / n
    for k in range(n):
        a += bend * (k + 1) / (n * (n + 1) / 2) + rng.uniform(-0.08, 0.08)
        x, y = x + step * math.cos(a), y + step * math.sin(a)
        pts.append((x, y))
    return pts


def sid(s):
    """A stable integer seed from a slot id (or any string)."""
    return zlib.crc32(str(s).encode()) & 0x7FFFFFFF


def xyxy(r):
    x, y, w, h = r
    return (x, y, x + w, y + h)


def grow(r, d):
    x0, y0, x1, y1 = r
    return (x0 - d, y0 - d, x1 + d, y1 + d)


def merge(rects, gap_mm):
    """Rectangles (x0, y0, x1, y1) closer than gap_mm merged into their bounding boxes: a label over its bar is one window."""
    out = [list(r) for r in rects]
    changed = True
    while changed:
        changed = False
        for i in range(len(out)):
            for j in range(i + 1, len(out)):
                a, b = out[i], out[j]
                if a[0] - gap_mm < b[2] and b[0] - gap_mm < a[2] and a[1] - gap_mm < b[3] and b[1] - gap_mm < a[3]:
                    out[i] = [min(a[0], b[0]), min(a[1], b[1]), max(a[2], b[2]), max(a[3], b[3])]
                    del out[j]
                    changed = True
                    break
            if changed:
                break
    return [tuple(r) for r in out]


class Field:
    """One layout's sculpt: the bone, muscle and skin height fields over the bounds (x0, y0, x1, y1), the openings the
    skin dives into, the sacs of light and their colours, and the instruments the renderer places (`objects`)."""

    def __init__(self, bounds, res_mm=0.5, seed=7):
        self.bounds, self.res, self.seed = bounds, res_mm, seed
        x0, y0, x1, y1 = bounds
        xs = np.arange(x0, x1 + res_mm * 0.5, res_mm)
        ys = np.arange(y0, y1 + res_mm * 0.5, res_mm)
        self.X, self.Y = np.meshgrid(xs, ys)
        self.px = 1.0 / res_mm
        self.B = np.full(self.X.shape, FLOOR_MM)
        self.M = np.full(self.X.shape, -99.0)
        self.mass = np.zeros(self.X.shape)
        self.pust = np.zeros(self.X.shape)
        self.glowc = np.zeros(self.X.shape + (3,))   # the colour of the light in each sac (linear rgb)
        self.openings = []      # (x0, y0, x1, y1, floor_z): the skin dives in to floor_z
        self.protected = []     # (label, x0, y0, x1, y1): measured by clearance()
        self.objects = []       # instruments for the renderer: glass, key caps, the crystal, the portrait
        self.keepouts = []      # (x0, y0, x1, y1): a neighbour's protected rectangle, cut back to the floor
        self.bleed = 0.0        # mm a piece's features may grow past its own rectangle
        self.headers = []       # (points, radius): the vertebral headers, left as they are by the dense anatomy
        self.frames = []        # rectangles a frame body wraps (windows and held pieces): the dense anatomy follows them
        rng = np.random.default_rng(seed + 9)
        wn1 = oa.value_noise(self.X, self.Y, 34.0, rng) - 0.5
        wn2 = oa.value_noise(self.X, self.Y, 11.0, rng) - 0.5
        self.warp = (2.6 * wn1 + 1.0 * wn2, 2.6 * (oa.value_noise(self.X, self.Y, 34.0, rng) - 0.5) + 1.0 * wn2)
        self.swell = oa.value_noise(self.X, self.Y, 26.0, rng)
        self.lobes = oa.value_noise(self.X, self.Y, 13.0, rng)

    # ------------------------------------------------------------------ windows of evaluation
    def win(self, r, pad):
        """Index slices of the samples within the rectangle r (x0, y0, x1, y1) grown by pad mm."""
        x0, y0, x1, y1 = grow(r, pad)
        bx0, by0 = self.bounds[0], self.bounds[1]
        i0 = max(int((x0 - bx0) * self.px), 0)
        i1 = min(int((x1 - bx0) * self.px) + 2, self.X.shape[1])
        j0 = max(int((y0 - by0) * self.px), 0)
        j1 = min(int((y1 - by0) * self.px) + 2, self.X.shape[0])
        return (slice(j0, max(j0, j1)), slice(i0, max(i0, i1)))

    def local(self, r, pad):
        s = self.win(r, pad)
        return s, self.X[s], self.Y[s], self.X[s] + self.warp[0][s], self.Y[s] + self.warp[1][s]

    # ------------------------------------------------------------------ the layers' building blocks
    def body(self, r, round_mm, height_mm, edge_mm=6.0, inset_mm=2.0):
        """A piece's flesh mass: a plateau filling r (inset by inset_mm, its edge lumpy, never straight), rising from the
        floor over edge_mm (a rolled fleshy edge), swelling and lobed on top. The bone under it rises too, so the
        renderer's floor cut never holes a piece."""
        s, X, Y, Xw, Yw = self.local(r, 4.0)
        d = oa.sd_box(Xw, Yw, grow(r, -inset_mm - 2.0), round_mm)
        prof = np.clip(-d / edge_mm, 0.0, 1.0)
        self.mass[s] = np.maximum(self.mass[s], prof)
        top = height_mm * (0.75 + 0.35 * self.swell[s] + 0.2 * self.lobes[s])
        self.B[s] = oa.rise(self.B[s], -6.0, np.clip(-d / 2.5, 0.0, 1.0))
        self.M[s] = np.maximum(self.M[s], oa.rise(self.B[s], top, prof ** 0.55))

    def ridge(self, pts, radius, height, radii=None, bone=True):
        """A ridge along a polyline (mm): bone (an armature, a rib, a spine) or muscle (a bundle, a cord)."""
        xs, ys = [p[0] for p in pts], [p[1] for p in pts]
        s = self.win((min(xs), min(ys), max(xs), max(ys)), radius * 1.5 + 2)
        if self.X[s].shape[0] < 2 or self.X[s].shape[1] < 2:
            return
        prof = oa.path_field(self.X[s], self.Y[s], pts, radius, radii)
        self.mass[s] = np.maximum(self.mass[s], np.clip(prof * 3.0, 0.0, 1.0))   # a ridge is body: never cut by the floor
        if bone:
            self.B[s] = oa.rise(self.B[s], height, prof ** 0.8)
        else:
            self.M[s] = np.maximum(self.M[s], oa.rise(np.maximum(self.B[s], self.M[s]), height, prof))

    def ring(self, r, gap, width, height, round_mm=4.0, bone=True, lumpy=True):
        """A ridge hugging the rectangle r at gap mm outside it."""
        s, X, Y, Xw, Yw = self.local(r, gap + width + 3)
        Xa, Ya = (Xw, Yw) if lumpy else (X, Y)
        prof = oa.bell(oa.sd_box(Xa, Ya, r, round_mm) - gap, width)
        if bone:
            self.B[s] = oa.rise(self.B[s], height, prof ** 0.8)
        else:
            sw = 0.55 + 0.6 * self.swell[s] + 0.4 * self.lobes[s]
            fib = 0.3 * np.sin(2 * math.pi * oa.sd_box(Xw, Yw, r, round_mm) / 2.3)
            self.M[s] = np.maximum(self.M[s], oa.rise(np.maximum(self.B[s], self.M[s]), height + fib, np.clip(prof * sw, 0, 1)))

    def vertebrae(self, pts, size_lo, size_hi, band, rng, lean=(0.0, -1.0)):
        """Vertebrae riding a spine: each a body turned its own way with a blade crest, sized to fit the band (the room
        in mm across the spine), a shadowed hollow between each pair. Sizes and turns are seeded."""
        for (cx, cy) in pts:
            sz = rng.uniform(size_lo, size_hi)
            a = math.radians(rng.uniform(-24.0, 24.0))
            ax = min(rng.uniform(9.0, 12.5) * sz, band * 0.9)
            ay = min(rng.uniform(5.5, 7.5) * sz, band * 0.6)
            s = self.win((cx - ax - 2, cy - ax - 2, cx + ax + 2, cy + ax + 2), 1)
            X, Y = self.X[s], self.Y[s]
            if X.size == 0:
                continue
            u = (X - cx) * math.cos(a) + (Y - cy) * math.sin(a)
            v = -(X - cx) * math.sin(a) + (Y - cy) * math.cos(a)
            body = (u / ax) ** 2 + (v / ay) ** 2
            h = 9.0 + 4.5 * sz
            self.B[s] = oa.rise(self.B[s], h, np.clip(1 - body, 0, 1) ** 0.8)
            sx, sy = cx + lean[0] * ay * 0.5, cy + lean[1] * ay * 0.5
            cu = (X - sx) * math.cos(a) + (Y - sy) * math.sin(a)
            cv = -(X - sx) * math.sin(a) + (Y - sy) * math.cos(a)
            crest = (cu / (2.8 * sz)) ** 2 + (cv / (ay * 0.85)) ** 2
            self.B[s] = oa.rise(self.B[s], h + 3.5 * sz, np.clip(1 - crest, 0, 1) ** 1.2)

    def muscles(self, r, keepout, rng, size_mm, height_mm, axis=None):
        """The body's relief: muscle lobes scattered over r (x0, y0, x1, y1), none centred within keepout (rectangles
        grown by their own room), each an elongated swell size_mm long, striated along its length, its axis following
        the piece's long side (or axis, radians) turned a little each; where two lobes meet, a crease. The count comes
        from the area, so a larger piece has more lobes, not larger ones."""
        x0, y0, x1, y1 = r
        w, h = x1 - x0, y1 - y0
        if axis is None:
            axis = 0.0 if w >= h else math.pi / 2
        n = int(w * h / (size_mm * size_mm * 0.35)) + 1
        placed = 0
        for _k in range(n * 6):
            if placed >= n:
                break
            cx, cy = rng.uniform(x0 + size_mm * 0.3, x1 - size_mm * 0.3), rng.uniform(y0 + size_mm * 0.25, y1 - size_mm * 0.25)
            if any(k[0] < cx < k[2] and k[1] < cy < k[3] for k in keepout):
                continue
            placed += 1
            a = axis + rng.normal(0, 0.35)
            ax = size_mm * rng.uniform(0.55, 0.85)
            ay = ax * rng.uniform(0.26, 0.38)
            s = self.win((cx - ax - 1, cy - ax - 1, cx + ax + 1, cy + ax + 1), 1)
            X, Y = self.X[s], self.Y[s]
            if X.size == 0:
                continue
            u = (X - cx) * math.cos(a) + (Y - cy) * math.sin(a)
            v = -(X - cx) * math.sin(a) + (Y - cy) * math.cos(a)
            prof = np.clip(1 - (u / ax) ** 2 - (v / ay) ** 2, 0, 1) ** 0.9
            fib = 0.35 * np.sin(2 * math.pi * v / 1.5 + rng.uniform(0, 6.28)) * prof
            top = height_mm * rng.uniform(0.75, 1.15)
            self.M[s] = np.maximum(self.M[s], oa.rise(np.maximum(self.B[s], self.M[s]), top + fib, prof))

    def opening(self, r, floor_z=GLASS_Z - 6.0, label=None, lip=(1.0, 6.0)):
        """A protected rectangle: the skin dives into it (so growth stays G1 clear), and it is measured. lip: where the
        skin starts to climb out of the opening (mm from its edge) and how steeply (mm per mm)."""
        self.openings.append(tuple(r) + (floor_z, lip))
        if label:
            self.protected.append((label,) + tuple(r))

    def tentacle(self, pts, r0, r1, height=None, rings=True, suckers=True):
        """A tentacle sculpted into the body (never a tube laid on it): a muscular limb along the curve pts (mm), its
        radius tapering from r0 to r1, ringed along its length, a row of suckers dimpled along its underside (the side
        the curve turns away from), rising from whatever lies beneath it, so it reads wet and muscular in the anatomy
        material (muscle-deep flesh). The owner, 2026-10-31: "Some should have like a tentacle kind of thing. I want to see
        more tentacles"."""
        P = resample(pts, 1.0)
        if len(P) < 2:
            return
        xs, ys = [p[0] for p in P], [p[1] for p in P]
        s = self.win((min(xs), min(ys), max(xs), max(ys)), r0 + 3)
        X, Y = self.X[s], self.Y[s]
        if X.size == 0:
            return
        best = np.full(X.shape, 1e9)
        arc = np.zeros(X.shape)
        side = np.zeros(X.shape)
        cum = 0.0
        for a, b in zip(P, P[1:]):
            d, t = oa.sd_segment(X, Y, a, b)
            L = math.hypot(b[0] - a[0], b[1] - a[1])
            m = d < best
            best = np.where(m, d, best)
            arc = np.where(m, cum + t * L, arc)
            cr = (b[0] - a[0]) * (Y - a[1]) - (b[1] - a[1]) * (X - a[0])
            side = np.where(m, np.sign(cr), side)
            cum += L
        u = arc / max(cum, 1e-6)
        r = r0 + (r1 - r0) * u ** 1.5
        prof = np.sqrt(np.clip(1.0 - (best / np.maximum(r, 1e-6)) ** 2, 0.0, 1.0))
        h = (height if height is not None else 3.0 + 1.7 * r0) * (r / r0) ** 0.6
        top = h * (0.55 + 0.45 * prof)
        if rings:
            top = top - 0.12 * r * (0.5 + 0.5 * np.cos(2 * math.pi * arc / np.maximum(1.1 * r, 1.5))) * prof
        if suckers:
            sp = np.maximum(2.4 * r, 3.0)
            ph = (arc % sp) / sp - 0.5
            suck = oa.bell(np.hypot(ph * sp, best - 0.55 * r), 0.42 * r) * (side < 0) * (u < 0.92)
            top = top - 0.45 * r * suck
        self.mass[s] = np.maximum(self.mass[s], np.clip(prof * 3.0, 0.0, 1.0))
        under = np.maximum(self.B[s], self.M[s])
        self.M[s] = np.maximum(self.M[s], oa.rise(under, np.maximum(under, 0.0) * 0.6 + top, prof))

    def keepout(self, r, label):
        """A rectangle nothing of this field may cover (a neighbour's readout, label or key): cut back to the floor with
        G1's margin, and measured by clearance()."""
        self.keepouts.append(tuple(r))
        self.protected.append((label,) + tuple(r))

    def sac(self, cx, cy, r, colour):
        """A sac of light under the skin, colour (linear rgb) glowing through it."""
        s = self.win((cx - r, cy - r, cx + r, cy + r), 1)
        p = oa.bell(np.hypot(self.X[s] - cx, self.Y[s] - cy), r)
        self.pust[s] = np.maximum(self.pust[s], p)
        w = (p > 0.02)[..., None]
        self.glowc[s] = np.where(w, np.array(colour)[None, None, :], self.glowc[s])

    # ------------------------------------------------------------------ the finish: skin over everything
    def finish(self, veins=True):
        """Stretch one skin over bone and muscle, raise veins and sacs under it, crease it, sink every opening; return
        (height, masks) and keep them on the field."""
        X, Y, px = self.X, self.Y, self.px
        B = self.B
        M = np.maximum(self.M, B - 0.5)
        core = oa.smax(B, M, 2.5)
        drape = oa.blur(core, 5.0 * px)
        S = np.maximum(core + 0.9, drape - 0.6)
        S = oa.blur(S, 0.5 * px)
        web = np.clip((drape - 0.6 - core - 0.9) / 3.0, 0.0, 1.0)
        thin = np.clip(1.0 - (S - B - 0.9) / 1.4, 0.0, 1.0) * np.clip(1.0 - (M - B) / 2.0, 0.0, 1.0)
        thin *= np.clip((B + 4.0) / 6.0, 0.0, 1.0)
        thin *= np.clip((B - oa.blur(B, 4.0 * px)) / 2.0, 0.0, 1.0)
        musc = np.clip((M - B) / 3.0, 0.0, 1.0) * np.clip(1.0 - (S - M) / 2.5, 0.0, 1.0)
        # the frame body's dense anatomy (the study's step 27): bundles, fibres, ribs, knuckles and folds following the
        # frames, the vertebral headers left as they are
        quiet = None
        for pts, rad in self.headers:
            q = np.clip(oa.path_field(X, Y, pts, rad) * 1.6, 0.0, 1.0)
            quiet = q if quiet is None else np.maximum(quiet, q)
        dS, rib, dfold, _fib = oa.dense_anatomy(X, Y, px, self.mass, self.frames, np.random.default_rng(self.seed + 5),
                                                DENSE_SCALE, quiet)
        S = S + dS
        thin = np.maximum(thin, 0.75 * rib)
        vein = np.zeros_like(X)
        if veins:
            rng = np.random.default_rng(self.seed + 2)
            x0, y0, x1, y1 = self.bounds
            n = int((x1 - x0) * (y1 - y0) * float(self.mass.mean() + 1e-6) / 2600.0) + 4
            for _v in range(n):
                for _try in range(12):     # start on flesh
                    px_, py_ = rng.uniform(x0 + 4, x1 - 4), rng.uniform(y0 + 4, y1 - 4)
                    j, i = int((py_ - y0) * px), int((px_ - x0) * px)
                    if self.mass[j, i] > 0.5:
                        break
                ang = rng.uniform(0, 2 * math.pi)
                pts = [(px_, py_)]
                for _s in range(int(rng.uniform(6, 16))):
                    ang += rng.normal(0, 0.45)
                    px_, py_ = px_ + 3.2 * FEATURE * math.cos(ang), py_ + 3.2 * FEATURE * math.sin(ang)
                    pts.append((px_, py_))
                s = self.win((min(p[0] for p in pts), min(p[1] for p in pts), max(p[0] for p in pts), max(p[1] for p in pts)), 3)
                if X[s].shape[0] < 2 or X[s].shape[1] < 2:
                    continue
                rad = rng.uniform(0.55, 1.1) * FEATURE
                vein[s] = np.maximum(vein[s], oa.path_field(X[s], Y[s], pts, rad, [1.0 - 0.6 * k / len(pts) for k in range(len(pts))]))
            vein *= np.clip(self.mass * 1.5 + web, 0.0, 1.0)
            S = S + 0.6 * vein
        S = S + 5.0 * self.pust ** 1.3
        lap = oa.blur(S, 1.0 * px)
        lap = (np.roll(lap, 1, 0) + np.roll(lap, -1, 0) + np.roll(lap, 1, 1) + np.roll(lap, -1, 1) - 4 * lap) * px ** 2
        crease = np.clip(lap / 0.25, 0.0, 1.0)
        S = S - 0.45 * crease
        pust = self.pust.copy()
        for (ox0, oy0, ox1, oy1, fz, (d0, slope)) in self.openings:
            s = self.win((ox0, oy0, ox1, oy1), 14)
            d = oa.sd_box(X[s], Y[s], (ox0, oy0, ox1, oy1), 2.0)
            lip = fz + slope * np.maximum(d - d0, 0.0)
            S[s] = np.minimum(S[s], lip)
            k = np.clip((d - 1.5) / 2.0, 0.0, 1.0)
            for m in (thin, musc, vein, pust):
                m[s] *= k
        for r in self.keepouts:   # a neighbour's protected rectangles: cut back, G1 clear
            s = self.win(r, G1_MM + 6)
            d = oa.sd_box(X[s], Y[s], r)
            S[s] = np.where(d < G1_MM + 0.5, np.minimum(S[s], FLOOR_MM), S[s])
        # outside every body the field falls to the floor (rule 5: cut away by the renderer)
        S = np.where(self.mass > 0.002, S, np.minimum(S, FLOOR_MM))
        self.height = S
        self.masks = dict(bone=thin, muscle=musc, vein=vein, web=web, pustule=pust, crease=np.maximum(crease, dfold),
                          depth=oa.depth_mask(S))
        return S, self.masks

    def clearance(self):
        """Rule G1 on the finished field: for each protected rectangle, the nearest distance (mm) from it to any surface
        standing above the glass (negative: growth covers it)."""
        out = {}
        up = self.height > GLASS_Z + 0.5
        for label, x0, y0, x1, y1 in self.protected:
            s = self.win((x0, y0, x1, y1), 12)
            u = up[s]
            if not u.any():
                out[label] = 12.0
                continue
            d = oa.sd_box(self.X[s], self.Y[s], (x0, y0, x1, y1))[u]
            out[label] = round(float(d.min()), 2)
        return out


# ====================================================================================================== the sculptors
# Each takes the field, the piece's rectangle (x, y, w, h) and its protected rectangles (x, y, w, h), and grows the piece
# from its size. All lengths are mm laid out from the size; the seed (a slot id) varies the details, never the plan.
LIFE = (0.16, 0.69, 0.024)      # linear rgb of the life green, the alert amber and the alert red
AMBER = (1.0, 0.43, 0.02)
RED = (0.8, 0.03, 0.02)


def _band(r, windows):
    """The room (mm) between the piece's top edge and its highest window, its bottom edge and its lowest window, and on
    its left and right."""
    x0, y0, x1, y1 = r
    if not windows:
        return (y1 - y0) / 2, (y1 - y0) / 2, (x1 - x0) / 2, (x1 - x0) / 2
    return (min(w[1] for w in windows) - y0, y1 - max(w[3] for w in windows),
            min(w[0] for w in windows) - x0, x1 - max(w[2] for w in windows))


def smin(a, b, k):
    """Smooth minimum of two signed distances (a smooth union), blend radius k mm."""
    return -oa.smax(-a, -b, k)


FLOOR_Z = -21.0   # a panel floor's depth (mm): deep enough that the anatomy material sinks it into shadow


def rib_floor(f, r, rng, keep=()):
    """A panel's recessed floor: a mesh of bone bars receding into shadow (the study's rib cavern, steps 22-24, which the
    owner asked for as the background inside panels), laid in rectangle r (x0, y0, x1, y1). Bars run across the panel
    at an uneven pitch, each bowed and tilted its own way and thicker in the middle, tied by a few bars running the
    other way; between them the floor falls deep. Everything stays well under the keys, tendrils and readouts set over
    it, and dark (the depth mask), so labels over it keep their contrast. keep: rectangles left plain (a label's glass)."""
    x0, y0, x1, y1 = r
    w, h = x1 - x0, y1 - y0
    if w < 10 or h < 10:
        return
    across_x = w >= h
    L0, L1, M0, M1 = (x0, x1, y0, y1) if across_x else (y0, y1, x0, x1)
    pos = M0 + rng.uniform(2.0, 6.0)
    while pos < M1 - 2.0:
        bow = rng.uniform(-3.0, 3.0)
        n = max(3, int((L1 - L0) / 24.0) + 2)
        pts = []
        for k in range(n):
            t = k / (n - 1)
            a = L0 + (L1 - L0) * t
            b = pos + bow * math.sin(math.pi * t) + rng.uniform(-0.8, 0.8) + (t - 0.5) * rng.uniform(-3, 3)
            pts.append((a, b) if across_x else (b, a))
        rad = [0.6 + 0.5 * math.sin(math.pi * k / (n - 1)) for k in range(n)]
        f.ridge(pts, 3.0 * FEATURE * rng.uniform(0.8, 1.2), FLOOR_Z + 9.0 + rng.uniform(-2, 2), rad)
        pos += rng.uniform(9.0, 15.0) * FEATURE
    for _k in range(max(1, int((L1 - L0) / 140.0))):   # a few tie bars the other way, deeper still
        a = rng.uniform(L0 + 10, L1 - 10)
        pts = [(a + rng.uniform(-4, 4), M0 + 2), (a + rng.uniform(-6, 6), (M0 + M1) / 2), (a + rng.uniform(-4, 4), M1 - 2)]
        f.ridge([p if across_x else (p[1], p[0]) for p in pts], 2.4 * FEATURE, FLOOR_Z + 6.0, [0.8, 1.0, 0.8])


def hang(f, held, rim, rng, tentacles=False):
    """Tendrils holding each key over its panel's floor: from each end of the key to the rim (rim: the inner edge of the
    rim, x0, y0, x1, y1), and from the key's top and bottom to the rim or to its neighbour, each a knotted cord of sinew
    tapering as it goes, sagging a little. Cords sit above the floor and below the keys."""
    x0, y0, x1, y1 = rim
    for q in held:
        cx, cy = (q[0] + q[2]) / 2, (q[1] + q[3]) / 2
        ends = [((q[0] + 3, cy + rng.uniform(-4, 4)), (x0, cy + rng.uniform(-10, 10))),
                ((q[2] - 3, cy + rng.uniform(-4, 4)), (x1, cy + rng.uniform(-10, 10)))]
        others = [o for o in held if o is not q]
        for sy, ey in ((q[1] + 2, y0), (q[3] - 2, y1)):   # up and down: to a neighbour if one is in the way, else the rim
            sx = cx + rng.uniform(-0.25, 0.25) * (q[2] - q[0])
            hit = [o for o in others if o[0] < sx < o[2] and ((o[3] <= q[1]) if sy < cy else (o[1] >= q[3]))]
            if hit:
                o = min(hit, key=lambda o: abs((o[1] + o[3]) / 2 - cy))
                ey = o[3] - 2 if sy < cy else o[1] + 2
            ends.append(((sx, sy), (sx + rng.uniform(-8, 8), ey)))
        for (a, b) in ends:
            if abs(a[0] - b[0]) + abs(a[1] - b[1]) < 4:
                continue
            mid = ((a[0] + b[0]) / 2 + rng.uniform(-3, 3), (a[1] + b[1]) / 2 + rng.uniform(1, 4))
            pts = [a, ((a[0] + mid[0]) / 2, (a[1] + mid[1]) / 2), mid, b]
            if tentacles:   # style "web": the keys held by tentacles, thick at the rim, thinning to the key
                f.tentacle(pts[::-1], 9.0 * rng.uniform(0.85, 1.1), 2.6, height=10.0)   # thick enough to read as tentacles
            else:
                f.ridge(pts, 2.4 * FEATURE * rng.uniform(0.75, 1.1), 1.0, [1.0, 0.65, 0.9, 1.1], bone=False)


def larva(f, body, cap, rng, state="idle", height=10.0, segments=True):
    """A key's soft body: a swollen larva (segmented along its length) or, for a short body, a wart of skin, filling body
    (x0, y0, x1, y1), with a seat for the metal button on its back (cap). Glossy and wet: its flesh is muscle-deep, so
    the anatomy material shows it redder and wetter. Pressed: the button pushes in and the body squashes, bulging up
    round it. (The owner: "The buttons should look like skin warts or squishy larva with metal buttons on its back".)"""
    x0, y0, x1, y1 = body
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    ax, ay = max((x1 - x0) / 2, 1.0), max((y1 - y0) / 2, 1.0)
    s, X, Y, Xw, Yw = f.local(body, 3)
    u, v = (Xw - cx) / ax, (Yw - cy) / ay
    e = np.abs(u) ** 2.6 + np.abs(v) ** 2.2
    prof = np.clip(1.0 - e, 0.0, 1.0) ** 0.45
    top = np.full(X.shape, height)
    if segments and ax > ay * 1.6:   # a larva: folds across its length, the head end a little fuller
        pitch = max(9.0, 2 * ay * 0.55) * rng.uniform(0.9, 1.1)
        seg = 0.5 + 0.5 * np.cos(2 * math.pi * (X - x0) / pitch)
        head = 1.0 + 0.12 * np.clip((X - cx) / ax * (1 if rng.random() > 0.5 else -1), 0, 1)
        top = height * head * (0.86 + 0.14 * seg)
    if state == "pressed":   # squashed: flatter, bulging round the button
        cq = cap
        bul = oa.bell(oa.sd_box(X, Y, cq, 3.0) - 3.5, 5.0)
        top = top * 0.9 + 2.0 * bul
    f.mass[s] = np.maximum(f.mass[s], np.clip(prof * 4.0, 0.0, 1.0))
    f.B[s] = oa.rise(f.B[s], -4.0, np.clip(prof * 3.0, 0.0, 1.0))
    f.M[s] = np.maximum(f.M[s], oa.rise(np.maximum(f.B[s], f.M[s]), top, prof))


def frame_anatomy(f, q, T, rng, r):
    """The accepted widget's frame round one window (tools/blender/organic_anatomy.py Sculpt.bone and .muscle, step 26),
    scaled from the room T (mm) the frame has round it instead of the widget's 38 mm: a bone ring under the skin, two
    swollen muscle bundles (inner and outer) striated along their run, ribs off the side with the most room curving out
    and back, and claws curling under the bottom corners. Each feature keeps a minimum size, so it still reads when the
    console is drawn small. (Version 25 grew one even bundle and nothing else: the frames read as a plain brown lip.)"""
    k = float(np.clip(T / 26.0, 0.55, 1.3))
    x0, y0, x1, y1 = r
    f.ring(q, max(2.4, 0.40 * T), max(2.2, 0.18 * T), 6.0 + 6.0 * k)                                   # the bone ring
    f.ring(q, max(1.6, 0.30 * T), max(2.6, 0.36 * T), 8.0 + 5.0 * k, round_mm=6.0, bone=False)         # inner bundle
    if T > 11.0:
        f.ring(q, 0.80 * T, max(2.0, 0.24 * T), 7.0 + 5.0 * k, round_mm=8.0, bone=False)               # outer bundle
    qw, qh = q[2] - q[0], q[3] - q[1]
    lroom, rroom = q[0] - x0, x1 - q[2]
    side = 1 if rroom >= lroom else -1
    room = max(lroom, rroom) + f.bleed
    if room > 10.0 and qh > 24.0:     # ribs off the roomier side, curving down and back (asymmetry: one side only)
        n = max(2, int(qh / (24.0 * FEATURE)))
        xe = q[2] if side > 0 else q[0]
        reach = min(room - 3.0, 1.1 * T + 12.0)
        for kk in range(n):
            y = q[1] + qh * (kk + 0.5) / n + rng.uniform(-2.0, 2.0)
            pts = [(xe + side * 0.1 * reach, y), (xe + side * 0.5 * reach, y + 1.0 + kk * 0.5),
                   (xe + side * 0.8 * reach, y + 6.0 + kk), (xe + side * 0.95 * reach, y + 13.0 + kk)]
            f.ridge(pts, max(2.8, 4.2 * k) * FEATURE, 8.0 + 3.0 * k, [1.0, 0.85, 0.55, 0.2])
    broom = y1 - q[3] + f.bleed * 0.6
    if broom > 10.0 and qw > 60.0:   # claws under the bottom corners, curled in
        cl = min(broom - 2.5, 14.0 + 10.0 * k)
        for sgn, cx in ((-1, q[0] + 0.12 * qw), (1, q[2] - 0.12 * qw)):
            pts = [(cx, q[3] + 0.3 * cl), (cx + 0.5 * cl * sgn, q[3] + 0.65 * cl), (cx + 0.15 * cl * sgn, q[3] + cl)]
            f.ridge(pts, max(2.0, 4.0 * k) * FEATURE, 8.0 + 3.0 * k, [1.0, 0.6, 0.12])


STYLES = ("ridge", "plain", "crown", "wrap", "hang", "web", "feelers", "mixed")


def tentacle_styles(f, style, r, hugs, wins, rng):
    """A panel's tentacles, by style: "crown", tentacles rising from its top edge and curling back; "hang", tentacles
    dripping from its bottom edge, curling at their tips; "feelers", thin whips swept back from its top corners;
    "wrap", tentacles coiled round the frame of its main window, overlapping, forming its border; "mixed", a crown on
    the half the ridge leaves. Every size comes from the panel's own size and the room past it (the bleed)."""
    x0, y0, x1, y1 = r
    w, h = x1 - x0, y1 - y0
    small = min(w, h)
    r0 = float(np.clip(small * 0.075, 4.5, 13.0))
    if style in ("crown", "mixed", "hang"):
        top = style != "hang"
        # rooted on the body itself (the hugs' outer edge), not the slot's edge, so no tentacle floats over empty slot
        bx0 = min([q[0] - T for q, T in hugs], default=x0)
        bx1 = max([q[2] + T for q, T in hugs], default=x1)
        root = (min([q[1] - T * 0.6 for q, T in hugs], default=y0 + r0 * 1.2) if top
                else max([q[3] + T * 0.6 for q, T in hugs], default=y1 - r0 * 1.2))
        bw = bx1 - bx0
        xa = bx0 + (bw * 0.55 if style == "mixed" else bw * 0.08)
        n = max(2, int((bx1 - bw * 0.08 - xa) / max(30.0, r0 * 4.0)) + 1)
        room = ((root - y0) if top else (y1 - root)) + f.bleed - 3.0   # within the region rendered, never cut off
        for k in range(n):
            x = xa + (bx1 - bw * 0.08 - xa) * (k + 0.5) / n + rng.uniform(-6, 6)
            y = root
            ang = (-math.pi / 2 if top else math.pi / 2) + rng.uniform(-0.45, 0.45)
            L = float(min(small * 0.7, 40 + f.bleed * 3.0) * rng.uniform(0.75, 1.15))
            L = min(L, max(12.0, room * 1.4))   # a curl reaches about 0.7 of its length
            bend = rng.choice([-1, 1]) * rng.uniform(1.6, 3.2)
            pts = curl((x, y), ang, L, bend, rng)
            f.tentacle(pts, r0 * rng.uniform(0.8, 1.1), 0.9)
    if style == "feelers":
        for sgn, x in ((1, x0 + 10), (-1, x1 - 10)):
            for k in range(2):
                ang = -math.pi / 2 - sgn * rng.uniform(0.5, 0.9)
                pts = curl((x, y0 + 8 + 6 * k), ang, float(min(small * 0.7, 40 + f.bleed * 2.5)) * rng.uniform(0.8, 1.1),
                           -sgn * rng.uniform(0.8, 1.8), rng, n=9)
                f.tentacle(pts, 3.4 * rng.uniform(0.8, 1.0), 0.7, height=7.0, suckers=False)
    if style == "wrap":
        T = float(np.clip(small * 0.07, 7.0, 13.0))
        g = grow(r, -2.5 - T * 0.45)      # coiled round the panel's own rim
        per = []   # the loop round the window, as points
        gx0, gy0, gx1, gy1 = g
        rr = min(10.0, (gx1 - gx0) / 4, (gy1 - gy0) / 4)
        for (ax, ay, bx, by) in ((gx0 + rr, gy0, gx1 - rr, gy0), (gx1, gy0 + rr, gx1, gy1 - rr),
                                 (gx1 - rr, gy1, gx0 + rr, gy1), (gx0, gy1 - rr, gx0, gy0 + rr)):
            m = max(2, int(math.hypot(bx - ax, by - ay) / 18))
            per += [(ax + (bx - ax) * i / m, ay + (by - ay) * i / m) for i in range(m)]
        N = len(per)
        cx, cy = (gx0 + gx1) / 2, (gy0 + gy1) / 2
        for k in range(2):   # two tentacles coiled round it, each over most of the loop, overlapping, thick at the root,
            st_ = int(N * k / 2 + rng.integers(0, 3))   # their free tips peeling off the frame and curling out (so the
            seg = [per[(st_ + i) % N] for i in range(int(N * 0.7))]   # wrap reads as limbs, not a beaded border)
            ph = rng.uniform(0, 6.3)
            wav = []
            for i, (px, py) in enumerate(seg):   # it meanders on and off the rim, so it reads as a limb, not a border
                dx, dy = px - cx, py - cy
                dn = math.hypot(dx, dy) or 1.0
                o_ = T * 0.75 * math.sin(i * 0.55 + ph + k * 1.7)
                wav.append((px + dx / dn * o_, py + dy / dn * o_))
            seg = wav
            ex, ey = seg[-1]
            ang = math.atan2(ey - cy, ex - cx) + rng.uniform(-0.3, 0.3)
            tip = curl((ex, ey), ang, float(18.0 + f.bleed * 1.2), rng.choice([-1, 1]) * rng.uniform(2.0, 3.0), rng)
            f.tentacle(seg + tip[1:], float(np.clip(T * 1.15, 8.0, 15.0)), 1.6, height=14.0 + T * 0.5)


def panel(f, rect, windows=(), seed="panel", spine="top", glass=True, labels=(), deep=0.0, avoid=(), style="ridge"):
    """A frame holding windows, grown round what it holds (the bible's accepted step: a thick rolled lip hugging every
    window, a vertebral ridge over the top, the body following its openings rather than filling a rectangle; between
    them the game shows, rule 5). The body is the smooth union of a hug round every window and every piece set into it
    (avoid: its keys and tabs), each as thick as its room allows, and a spine along the edge with the most room whose
    ends curl down into horns; all of it inside the slot. Every size comes from the room: hug thickness, lip, spine
    radius, vertebra count (one about every 19 mm of length) and size. windows: glass (readouts, drawings); labels: live
    text set on glass; deep: extra depth (mm) of the glass for a pocket."""
    rng = np.random.default_rng(sid(seed))
    r = xyxy(rect)
    x0, y0, x1, y1 = r
    w, h = x1 - x0, y1 - y0
    small = min(w, h)
    wins = merge([grow(xyxy(q), 1.5) for q in list(windows) + list(labels)], 5.0)
    held = [grow(xyxy(q), 1.0) for q in avoid]
    top, bot, left, right = _band(r, wins + held)
    edges = {"top": top, "bottom": bot}
    side = spine if spine in edges else max(edges, key=edges.get)
    room = edges[side]
    inner = grow(r, -2.5)
    s, X, Y, Xw, Yw = f.local(r, 2)
    body = np.full(X.shape, 1e9)
    hugs = []
    for q in wins + held:   # a hug as thick as the window is large, as far as the room allows
        rmin = min(q[0] - x0, q[1] - y0, x1 - q[2], y1 - q[3])
        # the body keeps a third of its room free, so the spine, the horns, the ribs and the claws can grow past its
        # edge and break the silhouette (the accepted widget's outline), still inside the slot
        T = float(min(rmin - 2.0, 0.66 * rmin + 2.0, max(9.0, 0.18 * min(q[2] - q[0], q[3] - q[1]) + 8.0)))
        T = max(T, 4.0)
        hugs.append((q, T))
        if not (held and q in held):   # a panel holding keys hangs them in tendrils over its floor: no hug round a key
            body = smin(body, oa.sd_box(Xw, Yw, grow(q, T), T * 0.8), 7.0)
    hx0 = max(x0 + 4, min(q[0] - T for q, T in hugs) - 12.0) if hugs else x0 + 4
    hx1 = min(x1 - 4, max(q[2] + T for q, T in hugs) + 12.0) if hugs else x1 - 4
    sp = None
    if room >= 8.0 and w >= 40 and style in ("ridge", "mixed"):
        if style == "mixed":   # the ridge on one side only; tentacles on the other (below)
            hx1 = hx0 + (hx1 - hx0) * 0.5
        rs = float(np.clip((room + 0.5 * f.bleed) * 0.40, 4.5, 13.0))
        # the spine rides the body's own edge (where the hugs end), its vertebrae standing past it into the free room
        edge = min(q[1] - T for q, T in hugs) if (hugs and side == "top") else max(q[3] + T for q, T in hugs) if hugs else None
        if edge is None:
            yy = (y0 + 2.5 + rs) if side == "top" else (y1 - 2.5 - rs)
        elif side == "top":
            yy = max(edge + rs * 0.35, y0 + 2.5 + rs * 1.15)
        else:
            yy = min(edge - rs * 0.35, y1 - 2.5 - rs * 1.15)
        sp = (yy, rs, hx0 + rs, hx1 - rs)
        d, _ = oa.sd_segment(Xw, Yw, (hx0 + rs, yy), (hx1 - rs, yy))
        body = smin(body, d - rs * 0.8, 6.0)
        if hugs:   # the spine grows out of the body, never floats over it: flesh fills from the spine to what it holds
            near = min(q[1] for q, _T in hugs) if side == "top" else max(q[3] for q, _T in hugs)
            band = (hx0 + rs, min(yy, near), hx1 - rs, max(yy, near))
            body = smin(body, oa.sd_box(Xw, Yw, band, 4.0), 6.0)
    body = np.maximum(body, oa.sd_box(X, Y, inner, 5.0))      # never past the slot
    if held:
        # a panel that holds keys: a rolled rim round the slot over a recessed floor of ribs receding into shadow (the
        # owner, of the study's rib cavern: "This weird boney mesh thing looks cool..and I think it would make really
        # good background for panels"), its keys hung over it in tendrils ("It should be like tendrils stuff")
        trim = float(np.clip(small * 0.07, 7.0, 13.0)) * (0.55 if style == "web" else 1.0)
        dI = oa.sd_box(Xw, Yw, inner, 8.0)
        rimp = np.clip(-dI / 3.0, 0.0, 1.0) * np.clip((dI + trim) / (trim * 0.55), 0.0, 1.0)
        floor = np.clip(-dI / 3.0, 0.0, 1.0)
        prof = np.maximum(np.clip(-body / 5.0, 0.0, 1.0), floor)
        f.mass[s] = np.maximum(f.mass[s], prof)
        f.B[s] = oa.rise(f.B[s], FLOOR_Z, floor)
        winb = np.clip(-body / 5.0, 0.0, 1.0)
        f.M[s] = np.maximum(f.M[s], oa.rise(f.B[s], 6.5 * (0.8 + 0.4 * f.swell[s]), np.maximum(rimp, winb) ** 0.6))
        floor_r = grow(inner, -trim * 0.8)
        rib_floor(f, floor_r, rng, keep=[grow(q, 1.0) for q in wins])
        f.frames += [grow(inner, -trim)] + [q for q, _T in hugs[:len(wins)]]
    else:
        prof = np.clip(-body / 5.0, 0.0, 1.0)
        f.mass[s] = np.maximum(f.mass[s], prof)
        f.B[s] = oa.rise(f.B[s], -6.0, np.clip(-body / 2.0, 0, 1))
        f.M[s] = np.maximum(f.M[s], oa.rise(f.B[s], 3.5 * (0.8 + 0.4 * f.swell[s]), prof ** 0.6))
        f.frames += [q for q, _T in hugs]
    # the rolled lip round every window: a swollen bundle, thick as its hug, striated, lobed along its run
    for q, T in hugs[:len(wins)]:
        frame_anatomy(f, q, T, rng, r)
        f.ring(q, 1.4, 1.6, 9.0, round_mm=5.0)           # a bone lip under the skin at the glass
        f.opening(q, floor_z=GLASS_Z - 6.0 - deep)
        if glass:
            f.objects.append({"kind": "glass", "rect": q, "z": GLASS_Z - deep, "veins": min(q[2] - q[0], q[3] - q[1]) > 60})
    if held:   # each key hangs in tendrils strung to the rim and to its neighbours
        hang(f, held, grow(inner, -trim * 0.5), rng, tentacles=(style == "web"))
    else:      # long muscle lobes over the body between what it holds, so it is never a flat tray
        f.muscles(grow(r, -6.0), [grow(q, 2.5) for q in wins + held], rng, float(np.clip(small * 0.35 * FEATURE, 26.0, 80.0)), 9.0)
    for q in windows:
        f.protected.append((f"{seed} window",) + xyxy(q))
    for q in labels:
        f.protected.append((f"{seed} label",) + xyxy(q))
    if sp:   # the vertebral ridge: a spine of bone under the skin, vertebrae riding it, horns at its ends
        yy, rs, sx0, sx1 = sp
        n = max(3, int(round((sx1 - sx0) / (19.0 * FEATURE))))
        pts = []
        for k in range(n):
            t = k / max(1, n - 1)
            sag = rs * 0.3 * math.sin(math.pi * t) * (1 if side == "top" else -1)
            pts.append((sx0 + (sx1 - sx0) * t + rng.uniform(-2.5, 2.5), yy + sag + rng.uniform(-0.8, 0.8)))
        sz = float(np.clip(FEATURE * rs / 7.0, 0.8, 2.2))
        f.ridge(pts, rs * 0.4, 6.0 + 2.0 * sz)                   # the spine: bone, low, sheathed
        f.ridge(pts, rs * 0.95, 8.5 + 2.2 * sz, bone=False)      # in muscle; only the vertebrae's crests break through
        f.vertebrae(pts, 0.75 * sz, 1.15 * sz, rs * 2.0 + f.bleed * 0.8, rng, lean=(0.0, -1.0 if side == "top" else 1.0))
        f.headers.append((pts, rs * 2.2))
        # horns: each end of the spine sweeps out and back past the body (the widget's horn crests), as long as the
        # slot has room for, tapering to a point; bone in the one surface, never an object laid on it
        dirn = -1 if side == "top" else 1
        for sgn, xe in ((1, sx0), (-1, sx1)):
            out_room = ((xe - x0) if sgn > 0 else (x1 - xe)) + f.bleed
            up_room = ((yy - y0) if side == "top" else (y1 - yy)) + f.bleed
            L = float(min(out_room - 4.0, max(18.0, 0.22 * w), 70.0))
            if L < 10.0:
                continue
            lift = min(up_room - 3.0, L * 0.45)
            pts = [(xe, yy), (xe - sgn * L * 0.4, yy + dirn * lift * 0.35), (xe - sgn * L * 0.75, yy + dirn * lift * 0.8),
                   (xe - sgn * L, yy + dirn * lift)]
            f.ridge(pts, max(3.2, rs * 0.55 * FEATURE), 12.0 + rs * 0.4, [1.0, 0.75, 0.45, 0.12])
    # claws under the shelf of pieces it holds (the widget's clawed feet under its key shelf), past the slot if need be
    if held:
        hb = max(q[3] for q in held)
        hx0_, hx1_ = min(q[0] for q in held), max(q[2] for q in held)
        cl = min(y1 - hb + f.bleed * 0.7 - 2.0, 12.0 + 0.04 * w)
        if cl > 8.0:
            for sgn, cx in ((-1, hx0_ + 6.0), (1, hx1_ - 6.0)):
                pts = [(cx, hb + 0.2 * cl), (cx + 0.45 * cl * sgn, hb + 0.6 * cl), (cx + 0.1 * cl * sgn, hb + cl)]
                f.ridge(pts, max(2.4, 0.22 * cl), 9.0, [1.0, 0.6, 0.12])
    # a sinew tendril from the bottom edge, reaching across the gutter toward the panel below (the widget's tendril to
    # its neighbour), when the field has bleed to reach into
    if f.bleed > 4.0 and w > 120:
        tx = x0 + w * rng.uniform(0.62, 0.82)
        pts = [(tx, y1 - 14.0), (tx + rng.uniform(-5, 5), y1 - 2.0), (tx + rng.uniform(-8, 8), y1 + 0.45 * f.bleed),
               (tx + rng.uniform(-8, 8), y1 + 0.85 * f.bleed)]
        f.ridge(pts, 5.0, 9.0, [1.0, 0.55, 0.85, 0.5], bone=False)
    tentacle_styles(f, style, r, hugs, wins, rng)
    # a sac of light where the body has room for one
    best = None
    for _k in range(40):
        cx, cy = rng.uniform(x0 + 6, x1 - 6), rng.uniform(y0 + 6, y1 - 6)
        j, i = int((cy - f.bounds[1]) * f.px), int((cx - f.bounds[0]) * f.px)
        if not (0 <= j < f.mass.shape[0] and 0 <= i < f.mass.shape[1]) or f.mass[j, i] < 0.9:
            continue
        clear = min([min(abs(cx - q[0]), abs(cx - q[2]), abs(cy - q[1]), abs(cy - q[3])) if not (q[0] - 4 < cx < q[2] + 4 and q[1] - 4 < cy < q[3] + 4) else 0
                     for q in wins + held] or [99])
        if clear > 9 and (best is None or clear > best[2]):
            best = (cx, cy, clear)
    if best:
        f.sac(best[0], best[1], min(5.0, best[2] * 0.35), LIFE)


def key(f, rect, seed="key", state="idle", face=None, accent="amber", label=None):
    """A key: a collar of flesh round a socket, a bone lip round the socket, and a hard dark cap with a lit rim set in it
    (the cap is an instrument object for the renderer). state: idle, hover (the rim brighter), pressed (the cap sinks
    3 mm and drops 2 mm, the flesh stays). face: the cap's rectangle (x, y, w, h); else, given the label's rectangle,
    the cap is the label with room round it, so the key keeps a collar of flesh; else inset from the key."""
    rng = np.random.default_rng(sid(seed))
    r = xyxy(rect)
    x0, y0, x1, y1 = r
    w, h = x1 - x0, y1 - y0
    small = min(w, h)
    inset = max(4.0, small * 0.15)
    if face:
        cap = xyxy(face)
    elif label:
        lx0, ly0, lx1, ly1 = xyxy(label)
        px_ = max(10.0, (lx1 - lx0) * 0.18)
        cap = (max(x0 + inset, lx0 - px_), max(y0 + inset, ly0 - 6.0), min(x1 - inset, lx1 + px_), min(y1 - inset, ly1 + 6.0))
    else:
        cap = (x0 + max(inset, w * 0.18), y0 + inset, x1 - max(inset, w * 0.18), y1 - inset)
    height = float(min(11.0, 6.0 + small * 0.1))
    larva(f, grow(r, -2.5), cap, rng, state=state, height=height, segments=True)
    cap_top = 0.8 * height
    f.opening(cap, floor_z=cap_top - 4.0, label=None, lip=(0.4, 3.0))
    f.objects.append({"kind": "key", "rect": cap, "state": state, "accent": accent, "slot": seed, "top": cap_top})


def tab(f, rect, seed="tab", state="idle"):
    """A tab: a lower, flatter key whose rim lights green when it is the active choice."""
    r = xyxy(rect)
    x0, y0, x1, y1 = r
    small = min(x1 - x0, y1 - y0)
    inset = max(3.5, small * 0.12)
    cap = (x0 + inset + small * 0.2, y0 + inset, x1 - inset - small * 0.2, y1 - inset)
    height = 6.5     # a smaller wart of skin with a metal plate on it
    larva(f, grow(r, -2.0), cap, np.random.default_rng(sid(seed)), state="idle", height=height, segments=False)
    f.opening(cap, floor_z=0.8 * height - 3.5, lip=(0.4, 3.0))
    f.objects.append({"kind": "tab", "rect": cap, "state": state, "slot": seed, "top": 0.8 * height})


def strip(f, rect, windows=(), labels=(), seed="strip", outer="top", ends=True, spine=True):
    """A strip: a long vertebral body carrying its readouts. The windows (bars, labels, the clock) sit in channels of
    glass; the spine runs along the outer edge (its vertebrae counted from the length), and each end swells into a knot
    of bone and muscle with ribs, so the strip resolves at any length."""
    rng = np.random.default_rng(sid(seed))
    r = xyxy(rect)
    x0, y0, x1, y1 = r
    w, h = x1 - x0, y1 - y0
    wins = merge([grow(xyxy(q), 1.5) for q in list(windows) + list(labels)], 6.0)
    f.body(r, round_mm=min(14.0, h * 0.3), height_mm=3.5, edge_mm=5.0)
    for q in wins:
        f.ring(q, 5.0, 3.6, 9.0, round_mm=7.0, bone=False)
    f.frames += list(wins)
    for q in wins:
        rw = min(q[2] - q[0], q[3] - q[1])
        f.ring(q, 2.0, 2.0, 10.0)
        f.ring(q, 5.6, 2.8, 11.0, bone=False)
        f.opening(q)
        f.objects.append({"kind": "glass", "rect": q, "z": GLASS_Z, "veins": rw > 60})
    for q in list(windows):
        f.protected.append((f"{seed} window",) + xyxy(q))
    for q in labels:
        f.protected.append((f"{seed} label",) + xyxy(q))
    top, bot, left, right = _band(r, wins)
    room = top if outer == "top" else bot
    yy = (y0 + max(2.5, room * 0.45)) if outer == "top" else (y1 - max(2.5, room * 0.45))
    if room >= 4.0 and spine:
        pts, xx = [], x0 + 14
        while xx < x1 - 14:
            pts.append((xx, yy + rng.uniform(-0.6, 0.6)))
            xx += 15.0 + rng.uniform(-3.0, 4.0)
        sz = min(1.2, room / 9.0)
        f.ridge(pts, max(1.8, room * 0.32), 7.0 + 3.0 * sz)
        f.vertebrae(pts, 0.5 * sz + 0.2, 0.9 * sz + 0.25, room, rng, lean=(0.0, -1.0 if outer == "top" else 1.0))
        f.headers.append((pts, max(4.0, room * 0.9)))
    # the far edge: a sinew bundle running the length, so the strip is muscle under its spine
    inner = (y1 - max(2.0, bot * 0.5)) if outer == "top" else (y0 + max(2.0, top * 0.5))
    f.ridge([(x0 + 10, inner), (x0 + w * 0.5, inner + rng.uniform(-0.8, 0.8)), (x1 - 10, inner)],
            max(1.6, min(top, bot) * 0.3), 4.5, bone=False)
    if ends:   # each end swells into a knot with ribs, sized by the room at that end
        for sgn, room_e, xe in ((1, left, x0), (-1, right, x1)):
            if room_e < 14:
                continue
            kx = xe + sgn * min(room_e * 0.45, 30.0)
            kw = min(room_e * 0.35, 26.0)
            s, X, Y, Xw, Yw = f.local((kx - kw, y0, kx + kw, y1), 2)
            knot = np.clip(1 - ((Xw - kx) / kw) ** 2 - ((Yw - (y0 + y1) / 2) / (h * 0.42)) ** 2, 0, 1)
            f.M[s] = np.maximum(f.M[s], oa.rise(np.maximum(f.B[s], f.M[s]), 9.0, knot ** 0.7))
            for k in range(3):
                yk = y0 + h * (0.25 + 0.25 * k)
                f.ridge([(kx - sgn * kw * 0.2, yk), (kx + sgn * kw * 0.5, yk + 2), (kx + sgn * kw * 0.95, yk + 4)], 2.2, 8.5,
                        [1.0, 0.7, 0.25])
            if kw > 10:
                f.sac(kx - sgn * kw * 0.15, (y0 + y1) / 2 + rng.uniform(-3, 3), min(5.0, kw * 0.28), LIFE)


def strip_crown(f, rect, windows, outer, seed, scale=1.0, bend=(0.9, 1.7), half=False):
    """A strip's tentacle crown (style C on a strip): tentacles curling off the strip's inner edge (the edge facing the
    battle; its outer edge is the screen's) in the gaps between its readouts and at its ends, short (scale), so they
    reach a little into the view and never over a readout or its centre."""
    rng = np.random.default_rng(sid(seed) + 7)
    x0, y0, x1, y1 = xyxy(rect)
    ws = sorted(grow(xyxy(q), 2.0) for q in windows)
    gaps = [(x0 + 12, ws[0][0] - 4)] if ws else [(x0 + 12, x1 - 12)]
    for a, b in zip(ws, ws[1:]):
        if b[0] - a[2] > 14:
            gaps.append((a[2] + 3, b[0] - 3))
    if ws:
        gaps.append((ws[-1][2] + 4, x1 - 12))
    down = outer == "top"            # the top strip's tentacles hang down into the view; the bottom strip's rise
    y = (y1 - 7.0) if down else (y0 + 7.0)
    for ga, gb in gaps:
        if gb - ga < 10:
            continue
        n = max(1, int((gb - ga) / 45))
        for k in range(n):
            x = ga + (gb - ga) * (k + 0.5) / n + rng.uniform(-4, 4)
            ang = (math.pi / 2 if down else -math.pi / 2) + rng.uniform(-0.35, 0.35)
            L = min(f.bleed * 0.9, f.bleed * 0.7 * scale) * rng.uniform(0.85, 1.05)   # within the bleed, so never cut off
            if half and x < (x0 + x1) / 2:
                continue
            pts = curl((x, y), ang, L, rng.choice([-1, 1]) * rng.uniform(*bend), rng)
            f.tentacle(pts, 7.0 * min(scale, 1.2) * rng.uniform(0.85, 1.1), 1.0)


def bezel(f, rect, window, seed="bezel", labels=(), style="ridge"):
    """A deep pocket of black glass for a live drawing (the ship systems display): a heavy body, a thick bone rim and a
    ribcage round the window, the glass 4 mm deeper than a readout's."""
    panel(f, rect, windows=[window], labels=labels, seed=seed, spine="top", deep=4.0, style=style)
    rng = np.random.default_rng(sid(seed) + 1)
    q = grow(xyxy(window), 3.0)
    cx, cy = (q[0] + q[2]) / 2, (q[1] + q[3]) / 2
    rx, ry = (q[2] - q[0]) / 2 + 12.0, (q[3] - q[1]) / 2 + 12.0
    r = xyxy(rect)
    n = max(8, int(2 * math.pi * max(rx, ry) / 26.0))
    for k in range(n):
        a = 2 * math.pi * (k + rng.uniform(-0.2, 0.2)) / n
        ca, sa = math.cos(a), math.sin(a)
        reach = rng.uniform(10.0, 18.0)
        p0 = (cx + ca * rx, cy + sa * ry)
        p1 = (cx + ca * (rx + reach), cy + sa * (ry + reach))
        if not (r[0] + 6 < p1[0] < r[2] - 6 and r[1] + 6 < p1[1] < r[3] - 6):
            continue
        f.ridge([p0, ((p0[0] + p1[0]) / 2 + sa * 2, (p0[1] + p1[1]) / 2 - ca * 2), p1], 2.4, 7.5, [1.0, 0.8, 0.3])


def round_pocket(f, rect, window, seed="dial"):
    """A round eye of glass in a knot of flesh (the radar dial): a round body, a bone ring, short ribs radiating."""
    rng = np.random.default_rng(sid(seed))
    x0, y0, x1, y1 = xyxy(rect)
    cx, cy, R = (x0 + x1) / 2, (y0 + y1) / 2, min(x1 - x0, y1 - y0) / 2 - 2.0
    q = xyxy(window)
    rw = max(q[2] - q[0], q[3] - q[1]) / 2 + 3.0
    s, X, Y, Xw, Yw = f.local((x0, y0, x1, y1), 2)
    d = np.hypot(Xw - cx, Yw - cy) - R
    prof = np.clip(-d / 6.0, 0, 1)
    f.mass[s] = np.maximum(f.mass[s], prof)
    f.B[s] = oa.rise(f.B[s], -6.0, np.clip(-d / 2.5, 0, 1))
    f.M[s] = np.maximum(f.M[s], oa.rise(f.B[s], 5.0 * (0.8 + 0.4 * f.swell[s]), prof ** 0.55))
    dr = np.hypot(X - cx, Y - cy) - rw
    f.B[s] = oa.rise(f.B[s], 8.0, oa.bell(dr - 3.0, 2.6) ** 0.8)
    n = max(6, int(2 * math.pi * (rw + 8) / 14.0))
    for k in range(n):
        a = 2 * math.pi * (k + rng.uniform(-0.25, 0.25)) / n
        p0 = (cx + math.cos(a) * (rw + 6), cy + math.sin(a) * (rw + 6))
        p1 = (cx + math.cos(a + 0.12) * (R - 3), cy + math.sin(a + 0.12) * (R - 3))
        f.ridge([p0, p1], 1.9, 6.5, [1.0, 0.35])
    f.opening((cx - rw, cy - rw, cx + rw, cy + rw), floor_z=GLASS_Z - 6.0)
    f.protected.append((f"{seed} window",) + q)
    f.objects.append({"kind": "glass_round", "c": (cx, cy), "r": rw, "z": GLASS_Z})


def crest(f, rect, seed="crest"):
    """The crest: a cradle of flesh with the crystal heart grown out of it (the crystal is an instrument object)."""
    rng = np.random.default_rng(sid(seed))
    x0, y0, x1, y1 = xyxy(rect)
    cx, cy, R = (x0 + x1) / 2, (y0 + y1) / 2, min(x1 - x0, y1 - y0) / 2
    s, X, Y, Xw, Yw = f.local((x0, y0, x1, y1), 2)
    d = np.hypot((Xw - cx) / 1.0, (Yw - cy - R * 0.25) / 0.85) - R * 0.82
    prof = np.clip(-d / 7.0, 0, 1)
    f.mass[s] = np.maximum(f.mass[s], prof)
    f.B[s] = oa.rise(f.B[s], -6.0, np.clip(-d / 2.5, 0, 1))
    f.M[s] = np.maximum(f.M[s], oa.rise(f.B[s], 12.0, prof ** 0.6))
    for k in range(5):   # the cradle's claws, curling up round the crystal's root
        a = math.radians(200 + k * 35 + rng.uniform(-8, 8))
        p0 = (cx + math.cos(a) * R * 0.75, cy + R * 0.3 - math.sin(a) * R * 0.55)
        p1 = (cx + math.cos(a) * R * 0.35, cy + R * 0.1 - math.sin(a) * R * 0.25)
        f.ridge([p0, p1], 2.6, 13.0, [1.0, 0.3])
    f.objects.append({"kind": "crystal", "c": (cx, cy - R * 0.05), "scale": R / 48.0})


def lamp(f, rect, state="yellow", seed="lamp"):
    """An alert lamp: a swollen sac of light under stretched skin, its light green, amber or red by state."""
    x0, y0, x1, y1 = xyxy(rect)
    cx, cy, R = (x0 + x1) / 2, (y0 + y1) / 2, min(x1 - x0, y1 - y0) / 2
    f.body((x0, y0, x1, y1), round_mm=R * 0.8, height_mm=6.0, edge_mm=4.0, inset_mm=0.5)
    f.ring((cx - R * 0.5, cy - R * 0.5, cx + R * 0.5, cy + R * 0.5), 1.0, 1.6, 7.0, round_mm=R * 0.5)
    f.sac(cx, cy, R * 0.62, {"green": LIFE, "yellow": AMBER, "red": RED}[state])


def cord(f, rect, seed="divider"):
    """A divider: one sinew cord across the rectangle, knotted, with a fleshy root at each end."""
    rng = np.random.default_rng(sid(seed))
    x0, y0, x1, y1 = xyxy(rect)
    cy = (y0 + y1) / 2
    h = y1 - y0
    f.body((x0, y0, x1, y1), round_mm=h * 0.45, height_mm=1.5, edge_mm=2.5, inset_mm=0.5)
    n = max(4, int((x1 - x0) / 18))
    pts = [(x0 + 4 + (x1 - x0 - 8) * k / (n - 1), cy + rng.uniform(-0.8, 0.8)) for k in range(n)]
    rad = [0.8 + 0.35 * math.sin(k * 1.7 + rng.uniform(0, 3)) ** 2 for k in range(n)]
    f.ridge(pts, h * 0.3, 4.0, rad, bone=False)


def helm(f, rect, faces, drawing=None, seed="helm.pad", style="ridge"):
    """The helm pad: the panel sculptor's body grown round its nine key faces (held pieces), each face a pit holding a
    dark cap with a lit rim and a collar of flesh; so the pad has the same spine, lip and muscle as every frame, at its
    own size, never a waffle of even ridges."""
    panel(f, rect, windows=(), seed=seed, avoid=faces, style=style)
    rng = np.random.default_rng(sid(seed) + 3)
    for k, q in enumerate(faces):   # each key a wart of skin swollen up from the floor, a metal button on its back
        c = xyxy(q)
        larva(f, grow(c, 6.0), c, rng, state="idle", height=9.0, segments=False)
        f.opening(grow(c, 1.0), floor_z=0.8 * 9.0 - 4.0, lip=(0.4, 3.0))
        f.protected.append((f"{seed} key {k}",) + c)
        f.objects.append({"kind": "key", "rect": (c[0] + 2.0, c[1] + 2.0, c[2] - 2.0, c[3] - 2.0), "state": "idle",
                          "accent": "amber", "slot": f"{seed}.{k}", "top": 0.8 * 9.0})


SCULPTORS = {"panel": panel, "key": key, "tab": tab, "strip": strip, "bezel": bezel, "round": round_pocket,
             "crest": crest, "lamp": lamp, "cord": cord, "helm": helm}
