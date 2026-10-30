"""Fleet Command: the diagrams of the analog panel bible (docs/design/ui-bible.html).

Owns every inline SVG in the bible. Each diagram is drawn from the same outline functions the Blender panels use
(a rounded rectangle, a mitred outline offset, a window whose edges carry profiles such as the notch), so a formed
shape in the catalogue is the shape the plate would be built from, not a sketch of it. The SVGs carry classes only;
the page's CSS colours them from its theme tokens, so they read in light and dark.

    python3 tools/mockups/ui_bible_svgs.py            # rewrite the SVG blocks between <!-- SVG:name --> markers
    python3 tools/mockups/ui_bible_svgs.py --check    # fail if the page is out of date

Units: diagram pixels. The cross-section is drawn at a stated scale (mm to px) and labelled in mm.
"""

import math
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PAGE = os.path.join(HERE, "..", "..", "docs", "design", "ui-bible.html")


# ----------------------------------------------------------------------------- outlines (after the study's helpers)

def rrect(x0, y0, x1, y1, r, seg=8):
    """A rounded rectangle as points, clockwise on screen (y down)."""
    pts = []
    for cx, cy, a0 in ((x0 + r, y0 + r, 180), (x1 - r, y0 + r, 270), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90)):
        for i in range(seg + 1):
            a = math.radians(a0 + 90 * i / seg)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def circle(cx, cy, r, seg=40):
    return [(cx + r * math.cos(2 * math.pi * i / seg), cy + r * math.sin(2 * math.pi * i / seg)) for i in range(seg)]


def _area(pts):
    return abs(sum(pts[i - 1][0] * p[1] - p[0] * pts[i - 1][1] for i, p in enumerate(pts))) / 2


def offset_poly(pts, d):
    """Move a closed outline d px inward (negative: outward), each point along its mitred normal."""
    def unit(x, y):
        n = math.hypot(x, y) or 1.0
        return x / n, y / n

    def go(sign):
        out = []
        for i in range(len(pts)):
            p0, p1, p2 = pts[i - 1], pts[i], pts[(i + 1) % len(pts)]
            n1 = unit(-(p1[1] - p0[1]), p1[0] - p0[0])
            n2 = unit(-(p2[1] - p1[1]), p2[0] - p1[0])
            m = unit(n1[0] + n2[0], n1[1] + n2[1])
            k = sign * d / max(0.35, m[0] * n1[0] + m[1] * n1[1])
            out.append((p1[0] + m[0] * k, p1[1] + m[1] * k))
        return out
    out = go(1.0)
    return out if (_area(out) < _area(pts)) == (d > 0) else go(-1.0)


def smooth(t):
    t = min(1.0, max(0.0, t))
    return t * t * (3 - 2 * t)


# Edge profiles: how far (px) an edge is pushed into the window at position s along it. Positive is metal growing in.
def flat(c, hw, fall, h):
    """The notch: flat at h within hw of c, back to 0 over fall px through a smoothstep (foot fillet and shoulder)."""
    return lambda s: h * smooth(1.0 - (abs(s - c) - hw) / fall) if abs(s - c) > hw else h


def bell(c, w, h):
    """A lobe: a cos-squared bump, round on top with concave feet, w px either side of c."""
    return lambda s: h * math.cos(math.pi * (s - c) / (2 * w)) ** 2 if abs(s - c) < w else 0.0


def waves(s0, s1, pitch, depth):
    """Scallops: a row of shallow round bumps between s0 and s1."""
    return lambda s: depth * (0.5 - 0.5 * math.cos(2 * math.pi * (s - s0) / pitch)) if s0 <= s <= s1 else 0.0


def steps(*stairs):
    """A terrace: a sum of smoothstep risers, each (start, run, rise)."""
    return lambda s: sum(rise * smooth((s - a) / run) for a, run, rise in stairs)


def flare(s0, s1, fall, h):
    """A bridge's side: h at both ends of the edge, falling to 0 within fall px, so the bar flares into the frame."""
    return lambda s: h * (smooth(1 - (s - s0) / fall) + smooth(1 - (s1 - s) / fall))


def shaped(x0, y0, x1, y1, r, top=None, right=None, bottom=None, left=None, step=2.0):
    """A window outline with rounded corners whose four edges each follow a profile (None: straight)."""
    z = lambda s: 0.0
    top, right, bottom, left = top or z, right or z, bottom or z, left or z
    pts = []

    def corner(cx, cy, a0):
        for i in range(9):
            a = math.radians(a0 + 90 * i / 8)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    corner(x0 + r, y0 + r, 180)
    x = x0 + r + step
    while x < x1 - r:
        pts.append((x, y0 + top(x)))
        x += step
    corner(x1 - r, y0 + r, 270)
    y = y0 + r + step
    while y < y1 - r:
        pts.append((x1 - right(y), y))
        y += step
    corner(x1 - r, y1 - r, 0)
    x = x1 - r - step
    while x > x0 + r:
        pts.append((x, y1 - bottom(x)))
        x -= step
    corner(x0 + r, y1 - r, 90)
    y = y1 - r - step
    while y > y0 + r:
        pts.append((x0 + left(y), y))
        y -= step
    return pts


# ----------------------------------------------------------------------------- SVG writing

def path(pts, cls, extra=""):
    d = "M" + " L".join(f"{x:.1f},{y:.1f}" for x, y in pts) + " Z"
    return f'<path class="{cls}" d="{d}"{extra}/>'


def line(x0, y0, x1, y1, cls, extra=""):
    return f'<line class="{cls}" x1="{x0:.1f}" y1="{y0:.1f}" x2="{x1:.1f}" y2="{y1:.1f}"{extra}/>'


def text(x, y, s, cls="lbl", anchor="start"):
    s = s.replace("&", "&amp;").replace("<", "&lt;")
    return f'<text class="{cls}" x="{x:.1f}" y="{y:.1f}" text-anchor="{anchor}">{s}</text>'


def tag(x, y, code, anchor="start"):
    """A part code in a small box, e.g. N2."""
    w = 7 + 6.6 * len(code)
    x0 = x if anchor == "start" else x - w if anchor == "end" else x - w / 2
    return (f'<rect class="tagbox" x="{x0:.1f}" y="{y - 10:.1f}" width="{w:.1f}" height="14" rx="3"/>'
            + text(x0 + w / 2, y + 0.5, code, "tag", "middle"))


def callout(x, y, lx, ly, code, label="", anchor="start"):
    out = line(x, y, lx, ly, "leader") + f'<circle class="dot" cx="{x:.1f}" cy="{y:.1f}" r="2.2"/>'
    out += tag(lx, ly + 4, code, anchor)
    if label:
        w = 7 + 6.6 * len(code)
        if anchor == "middle":
            out += text(lx, ly + 20, label, "lbl small", "middle")
        else:
            tx = lx + w + 5 if anchor == "start" else lx - w - 5
            out += text(tx, ly + 4, label, "lbl small", "start" if anchor == "start" else "end")
    return out


def knob(x, y, r, pointer=None):
    out = f'<circle class="collar" cx="{x:.1f}" cy="{y:.1f}" r="{r * 1.18:.1f}"/>'
    out += f'<circle class="kn" cx="{x:.1f}" cy="{y:.1f}" r="{r:.1f}"/>'
    out += f'<circle class="knh" cx="{x - r * 0.35:.1f}" cy="{y - r * 0.35:.1f}" r="{r * 0.3:.1f}"/>'
    if pointer is not None:
        a = math.radians(pointer)
        out += line(x + math.cos(a) * r * 0.2, y + math.sin(a) * r * 0.2, x + math.cos(a) * r * 0.75, y + math.sin(a) * r * 0.75, "pointer")
    return out


def panel(plate, windows, roll=4.0, bezel=1.2, lamps=()):
    """A plate drawn top-down: metal with its rolled edge, each window with its frame-bar highlight, brass bezel and
    lit film (lamps as small amber bars on the top lip)."""
    out = path(plate, "mt") + path(offset_poly(plate, roll), "mhi")
    for w in windows:
        out += path(offset_poly(w, -roll), "mhi")
        out += path(w, "fl")
        out += path(offset_poly(w, bezel * 0.5), "br")
    for x, y in lamps:
        out += f'<rect class="lampbar" x="{x - 8:.1f}" y="{y + 1:.1f}" width="16" height="2.4" rx="1.2"/>'
    return out


def svg(name, w, h, body, title):
    return (f'<svg class="dia" viewBox="0 0 {w} {h}" role="img" aria-labelledby="t-{name}" xmlns="http://www.w3.org/2000/svg">'
            f'<title id="t-{name}">{title}</title>{body}</svg>')


# ----------------------------------------------------------------------------- the diagrams

def section():
    """D1: the two layers cut through one window, top edge on the left, screen-down to the right. 7 px per mm."""
    s = 7.0
    ox, oy = 190, 92  # x of the window's top edge; y of the plate's top face (z = 8 mm)
    Z = lambda z: oy + (8 - z) * s
    X = lambda mm: ox + mm * s
    roll, dz = 3.8, -12.0
    b = ""
    # chassis
    b += f'<rect class="ch" x="{X(-8):.1f}" y="{Z(dz - 4):.1f}" width="{X(70) - X(-8):.1f}" height="{2 * s:.1f}"/>'
    # film (main), sub-display step on the right part
    b += f'<rect class="fl" x="{X(-6):.1f}" y="{Z(dz):.1f}" width="{X(38) - X(-6):.1f}" height="{2 * s:.1f}"/>'
    b += f'<rect class="fl" x="{X(38):.1f}" y="{Z(dz - 1.5):.1f}" width="{X(60) - X(38):.1f}" height="{2 * s:.1f}"/>'
    b += f'<rect class="fl" x="{X(60):.1f}" y="{Z(dz):.1f}" width="{X(68) - X(60):.1f}" height="{2 * s:.1f}"/>'
    # shadow wedge on the film under the top lip (main light from upper left: 1 down the screen per 1.6 in)
    sh = 12.0 / 1.6
    b += f'<path class="shd" d="M{X(0):.1f},{Z(dz):.1f} L{X(sh):.1f},{Z(dz):.1f} L{X(0):.1f},{Z(0):.1f} Z"/>'
    # plate: rounded profile, window edge at x = 0, plate continues left
    top_round = [(X(-roll + roll * math.cos(math.radians(a))), Z(8 - roll + roll * math.sin(math.radians(a)))) for a in range(0, 91, 8)]
    bot_round = [(X(-roll + roll * math.cos(math.radians(a))), Z(roll + roll * math.sin(math.radians(a)))) for a in range(-90, 1, 8)]
    plate = [(X(-10), Z(8))] + list(reversed(top_round)) + list(reversed(bot_round)) + [(X(-10), Z(0))]
    b += f'<path class="mt mte" d="M{plate[0][0]:.1f},{plate[0][1]:.1f} ' + " ".join(f"L{x:.1f},{y:.1f}" for x, y in plate[1:]) + ' Z"/>'
    b += f'<path class="mhi" d="M{X(-9):.1f},{Z(7.6):.1f} L{X(-roll):.1f},{Z(7.6):.1f}"/>'
    # far lip on the right (bottom edge of the window)
    far = [(X(72), Z(8)), (X(72), Z(0))] + [(X(66 + roll - roll * math.cos(math.radians(a))), Z(roll + roll * math.sin(math.radians(a)))) for a in range(-90, 1, 8)] + \
          [(X(66 + roll - roll * math.cos(math.radians(a))), Z(8 - roll + roll * math.sin(math.radians(a)))) for a in range(0, 91, 8)]
    b += f'<path class="mt mte" d="M{far[0][0]:.1f},{far[0][1]:.1f} ' + " ".join(f"L{x:.1f},{y:.1f}" for x, y in far[1:]) + ' Z"/>'
    # brass bezel under the roll
    b += f'<rect class="brfill" x="{X(-0.8):.1f}" y="{Z(8 - roll + 0.2):.1f}" width="{2.3 * s:.1f}" height="{2.0 * s:.1f}" rx="2"/>'
    # lamp just inside the window, 8 mm off the film, aimed down the screen with a 120 degree fan
    lx, lz = X(1.0), Z(dz + 8)
    b += f'<rect class="lampbar" x="{lx - 3:.1f}" y="{lz - 6:.1f}" width="6" height="12" rx="2"/>'
    for a in (-60, -30, 0, 30, 60):
        r = math.radians(a)
        b += line(lx + 4, lz, lx + 4 + 70 * math.cos(r), lz + 70 * math.sin(r), "ray", ' marker-end="url(#ah)"')
    # main light arrows from the upper left
    for i in range(3):
        x0, y0 = X(-6 + i * 5), Z(24)
        b += line(x0, y0, x0 + 8 * s * 0.62, y0 + 8 * s, "sun", ' marker-end="url(#ahs)"')
    # backlight arrows from behind the film
    for mm in (20, 30, 50, 64):
        z0 = dz - 1.5 if 38 <= mm < 60 else dz
        b += line(X(mm), Z(dz - 3.6), X(mm), Z(z0) + 2, "glow", ' marker-end="url(#ahg)"')
    # dimensions
    b += line(X(-20), Z(8), X(-20), Z(0), "dim") + text(X(-20) + 5, Z(4) + 4, "8 mm", "lbl small")
    b += line(X(-26), Z(8), X(-26), Z(dz), "dim") + text(X(-26) + 5, Z(-5) + 4, "12 mm", "lbl small")
    b += line(X(4), Z(dz + 8), X(4), Z(dz), "dim") + text(X(4) + 6, Z(dz + 3) + 4, "8 mm", "lbl small")
    b += text(X(-roll) - 6, Z(8) - 8, "r 3.8 mm", "lbl small", "end")
    b += line(X(38), Z(dz) - 2, X(38), Z(dz - 1.5) + 2, "dim") + text(X(38) - 4, Z(dz) - 8, "1.5 mm step", "lbl small", "end")
    # callouts
    b += callout(X(-8), Z(7), X(-14), Z(13), "P1", "plate", "end")
    b += callout(X(-1.2), Z(6.8), X(2), Z(14), "P2", "rolled edge")
    b += callout(X(0.6), Z(3.3), X(12), Z(9.5), "W4", "bezel")
    b += callout(X(1.0), Z(dz + 8) + 6, X(10), Z(dz + 6.2), "F3", "lamp")
    b += callout(X(26), Z(dz + 2.2), X(26), Z(dz + 5.6), "F4", "spill")
    b += callout(X(sh * 0.4), Z(dz) + 3, X(10), Z(dz - 6.2), "W6", "main shadow")
    b += callout(X(30), Z(dz) + 7, X(24), Z(dz - 8), "F1", "film")
    b += callout(X(49), Z(dz - 1.5) + 7, X(44), Z(dz - 10.5), "F6", "sub-display")
    b += callout(X(66), Z(dz - 4) + 6, X(62), Z(dz - 8), "P7", "chassis")
    b += callout(X(-2), Z(-1), X(-12), Z(-8), "W2", "well", "end")
    b += text(X(-4), Z(24) - 6, "main light", "lbl small sunlbl")
    defs = ('<defs><marker id="ah" viewBox="0 0 6 6" refX="5" refY="3" markerWidth="6" markerHeight="6" orient="auto">'
            '<path class="raytip" d="M0,0 L6,3 L0,6 Z"/></marker>'
            '<marker id="ahs" viewBox="0 0 6 6" refX="5" refY="3" markerWidth="6" markerHeight="6" orient="auto">'
            '<path class="suntip" d="M0,0 L6,3 L0,6 Z"/></marker>'
            '<marker id="ahg" viewBox="0 0 6 6" refX="5" refY="3" markerWidth="6" markerHeight="6" orient="auto">'
            '<path class="glowtip" d="M0,0 L6,3 L0,6 Z"/></marker></defs>')
    return svg("section", 720, 330, defs + b, "Cross-section through one window: plate, well, film, lamp and lights")


def anatomy():
    """D2: a top view of an original example panel with every part named."""
    plate = rrect(20, 20, 600, 330, 18)
    win = shaped(44, 44, 576, 196, 8, bottom=flat(310, 62, 34, 40))
    sub1 = rrect(78, 70, 220, 120, 4)
    slot = rrect(60, 232, 560, 248, 8)
    b = panel(plate, [win, slot], roll=5, lamps=((160, 44), (310, 44), (460, 44)))
    # sub-display: a printed frame and a deeper film
    b += path(sub1, "subfl") + path(sub1, "inkline")
    b += text(149, 102, "12:40", "inktext big", "middle")
    b += f'<path class="inkline" d="M250,88 L540,88"/>' + "".join(line(250 + i * 14.5, 88, 250 + i * 14.5, 82 if i % 5 else 78, "inkline") for i in range(21))
    b += text(250, 112, "TRACK 04  ·  ASTERN", "inktext", "start")
    # lamp spill fans
    for x in (160, 310, 460):
        b += f'<path class="spill" d="M{x - 8},{47} L{x + 8},{47} L{x + 46},{120} L{x - 46},{120} Z"/>'
    # knobs in the notch, dimples
    for x in (282, 338):
        b += f'<ellipse class="dimple" cx="{x}" cy="{176}" rx="20" ry="20"/>' + knob(x, 176, 12, -125)
    # slot streak, thumb, ticks
    b += f'<rect class="streak" x="72" y="239" width="476" height="2.4" rx="1.2"/>'
    b += "".join(f'<circle class="tick" cx="{64 + i * 12.3:.1f}" cy="{c}" r="1.3"/>' for i in range(41) for c in (224, 256))
    b += knob(210, 240, 11)
    # keys in pressed dimples, rivets, bead
    for x in (80, 140, 200):
        b += f'<ellipse class="dimple" cx="{x}" cy="{298}" rx="24" ry="17"/>' + knob(x, 296, 9)
    for x, y in ((34, 34), (586, 34), (34, 316), (586, 316)):
        b += f'<circle class="rivet" cx="{x}" cy="{y}" r="4"/>'
    b += path(rrect(250, 276, 584, 318, 9), "bead")
    b += f'<rect class="subfl" x="268" y="284" width="298" height="26" rx="4"/>' + text(417, 302, "00:43 / 31:27", "inktext", "middle")
    # callouts (lines from part to labels outside the plate)
    C = [
        (30, 120, 2, 128, "P1", "plate"), (24, 220, 2, 214, "P2", "rolled edge"), (46, 60, 2, 60, "P3", "frame bar"),
        (34, 316, 2, 344, "P5", "rivet"), (258, 297, 236, 344, "P4", "bead"),
        (300, 46, 300, 12, "F3", "lamp"), (340, 92, 360, 12, "F4", "spill"), (520, 150, 640, 120, "F1", "film"),
        (220, 100, 640, 70, "F6", "sub-display"), (330, 110, 640, 96, "F5", "ink"),
        (576, 120, 640, 146, "W1", "window"), (575, 60, 640, 44, "W4", "bezel"),
        (282, 160, 640, 172, "N4", "flat"), (240, 190, 640, 198, "N3", "foot fillet"), (262, 158, 640, 224, "N2", "shoulder"),
        (338, 176, 640, 250, "C3", "knob"), (343, 169, 640, 276, "C5", "pointer"), (300, 176, 640, 302, "C1", "dimple"),
        (420, 240, 640, 328, "C7", "streak"), (210, 240, 236, 366, "C9", "thumb"), (100, 224, 120, 366, "C8", "tick column"),
        (540, 240, 560, 366, "C6", "slot"),
    ]
    D = 110
    body = f'<g transform="translate({D},0)">' + b + "</g>"
    labels = ""
    bottom = {"P5": 40, "C8": 200, "C9": 330, "P4": 470, "C6": 620}
    top = {"F3": 300, "F4": 470}
    for x, y, lx, ly, code, lab in C:
        if code in bottom:
            labels += callout(x + D, y, bottom[code] + D, 356, code, lab, "middle")
        elif code in top:
            labels += callout(x + D, y, top[code], 8, code, lab)
        elif lx >= 640:
            labels += callout(x + D, y, 640 + D + 12, ly, code, lab)
        else:
            labels += callout(x + D, y, D - 8, ly, code, lab, "end")
    return svg("anatomy", 900, 392, body + labels, "Top view of an example panel with every part named")


def notch_profile():
    """D3: the notch's bottom-edge profile with its parameters."""
    S = 4.0
    ox, oy = 170, 190
    h, hw, fall, c = 30, 74, 30, 0
    f = flat(c, hw, fall, h)
    pts = [(ox + (s + 130) * S / 2, oy - f(s) * S / 1.6) for s in range(-130, 131, 2)]
    plate = [(ox, oy + 50)] + pts + [(ox + 260 * S / 2, oy + 50)]
    b = path([(ox, 30)] + [(ox + 260 * S / 2, 30)] + list(reversed(pts)), "fl")
    b += path(plate, "mt")
    rolled = [(x, y + 0) for x, y in pts]
    b += f'<path class="mhi" d="M' + " L".join(f"{x:.1f},{y + 6:.1f}" for x, y in rolled) + '"/>'
    b += f'<path class="br" d="M' + " L".join(f"{x:.1f},{y - 1.5:.1f}" for x, y in rolled) + '"/>'
    X = lambda s: ox + (s + 130) * S / 2
    Y = lambda v: oy - v * S / 1.6
    b += line(X(-hw), Y(h) - 18, X(hw), Y(h) - 18, "dim") + text(X(0), Y(h) - 24, "flat: 2 × hw = 148 px", "lbl small", "middle")
    b += line(X(hw), Y(0) + 26, X(hw + fall), Y(0) + 26, "dim") + text(X(hw + fall / 2), Y(0) + 40, "fall 30 px", "lbl small", "middle")
    b += line(X(hw + fall + 12), Y(0), X(hw + fall + 12), Y(h), "dim") + text(X(hw + fall + 16), Y(h / 2) + 4, "rise h 30 px", "lbl small")
    b += callout(X(-hw - fall * 0.82), Y(h * 0.07), X(-hw - fall - 22), Y(-14), "N3", "foot fillet (concave)", "end")
    b += callout(X(-hw - fall * 0.15), Y(h * 0.94), X(-hw - fall - 22), Y(h + 10), "N2", "shoulder (convex)", "end")
    b += callout(X(0), Y(h), X(30), Y(h + 18), "N4", "flat")
    b += callout(X(hw + fall * 0.5), Y(h * 0.5), X(hw + fall + 40), Y(h * 0.5 - 22), "N5", "fall")
    b += callout(X(100), Y(0) + 6, X(112), Y(-12), "P3", "rolled edge follows")
    b += text(X(0), 22, "display (film, F1)", "lbl small", "middle") + text(X(0), oy + 42, "plate (P1), one piece", "lbl small", "middle")
    b += text(X(0), oy + 70, "lift(s) = h · smoothstep(1 − (|s − c| − hw) / fall)", "mono small", "middle")
    return svg("notch", 860, 282, b, "The notch profile: rise, flat, fall, foot fillet and shoulder")


def lights():
    """D4: the three lights on the film, each in its own small section."""
    out = ""
    titles = (("Backlight", "F2 · FILM_GLOW", ("even glow from behind:", "the film's colour")),
              ("Lamp spill", "F3/F4 · LAMPS, SPILL", ("a fan down the film from", "under the top lip")),
              ("Main light", "W6 · MAIN", ("the room light: the lip casts", "the top and left shadow")))
    for i, (t, code, note) in enumerate(titles):
        ox = 20 + i * 250
        b = f'<rect class="fl" x="{ox + 10}" y="120" width="180" height="14"/>'
        b += f'<path class="mt mte" d="M{ox},40 L{ox + 34},40 Q{ox + 44},40 {ox + 44},50 L{ox + 44},60 Q{ox + 44},70 {ox + 34},70 L{ox},70 Z"/>'
        b += f'<path class="mt mte" d="M{ox + 200},40 L{ox + 176},40 Q{ox + 166},40 {ox + 166},50 L{ox + 166},60 Q{ox + 166},70 {ox + 176},70 L{ox + 200},70 Z"/>'
        if i == 0:
            b += f'<rect class="glowfill" x="{ox + 10}" y="120" width="180" height="14"/>'
            for x in range(ox + 30, ox + 180, 30):
                b += line(x, 158, x, 138, "glow", ' marker-end="url(#ahg2)"')
        elif i == 1:
            b += f'<rect class="lampbar" x="{ox + 46}" y="86" width="5" height="12" rx="2"/>'
            b += f'<path class="spill" d="M{ox + 52},88 L{ox + 52},104 L{ox + 160},120 L{ox + 70},120 Z"/>'
            for a in (-40, 0, 40):
                r = math.radians(a + 20)
                b += line(ox + 54, 92, ox + 54 + 60 * math.cos(r), 92 + 28 * math.sin(r), "ray", ' marker-end="url(#ah2)"')
        else:
            b += f'<path class="shd" d="M{ox + 44},120 L{ox + 64},120 L{ox + 44},70 Z"/>'
            for k in range(3):
                b += line(ox + 20 + k * 22, 6, ox + 40 + k * 22, 52, "sun", ' marker-end="url(#ahs2)"')
        b += text(ox + 100, 178, t, "lbl", "middle") + text(ox + 100, 194, code, "mono small", "middle") + text(ox + 100, 210, note[0], "lbl small", "middle") + text(ox + 100, 224, note[1], "lbl small", "middle")
        out += b
    defs = ('<defs><marker id="ah2" viewBox="0 0 6 6" refX="5" refY="3" markerWidth="6" markerHeight="6" orient="auto"><path class="raytip" d="M0,0 L6,3 L0,6 Z"/></marker>'
            '<marker id="ahs2" viewBox="0 0 6 6" refX="5" refY="3" markerWidth="6" markerHeight="6" orient="auto"><path class="suntip" d="M0,0 L6,3 L0,6 Z"/></marker>'
            '<marker id="ahg2" viewBox="0 0 6 6" refX="5" refY="3" markerWidth="6" markerHeight="6" orient="auto"><path class="glowtip" d="M0,0 L6,3 L0,6 Z"/></marker></defs>')
    return svg("lights", 760, 234, defs + out, "The three lights on the film: backlight, lamp spill and main light")


def legibility():
    """D5: contrast under a lamp in the legibility test, drawn to scale against the 0.5 floor."""
    rows = (("Point spot, 1 mm source", 0.14), ("Line source 40 px × 4 mm, power ×0.8", 0.34),
            ("Line 20 mm deep, ×0.3, ink that absorbs", 0.59), ("Line 20 mm deep, ×0.2, ink that absorbs (chosen)", 0.62))
    x0, w = 250, 330
    X = lambda v: x0 + v / 0.7 * w
    b = ""
    for i, (lab, v) in enumerate(rows):
        y = 18 + i * 34
        b += text(x0 - 10, y + 15, lab, "lbl small", "end")
        b += f'<rect class="{"barok" if v >= 0.5 else "barbad"}" x="{x0}" y="{y}" width="{X(v) - x0:.1f}" height="20" rx="3"/>'
        b += text(X(v) + 6, y + 15, f"{v:.2f}", "mono small")
    b += line(X(0.5), 8, X(0.5), 150, "floor") + text(X(0.5) + 4, 162, "floor 0.50", "mono small")
    for v in (0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7):
        b += line(X(v), 150, X(v), 154, "dim") + text(X(v), 168, f"{v:.1f}", "mono tiny", "middle")
    b += text(x0 + w / 2, 186, "contrast under the lamp: (film − ink) / film", "lbl small", "middle")
    return svg("legibility", 640, 194, b, "Legibility test results: contrast under a lamp for four lamp settings")


def fit():
    """D6: children inside their parent with even margins, against children hanging over its edge."""
    b = ""
    for i, ok in enumerate((False, True)):
        ox = 10 + i * 320
        b += f'<rect class="mt mte" x="{ox}" y="30" width="300" height="34" rx="8"/>'
        for k, frac in enumerate((0.8, 0.6, 0.35)):
            x = ox + 12 + k * 96
            if ok:
                b += f'<rect class="trackin" x="{x}" y="46" width="84" height="8" rx="4"/><rect class="fillin" x="{x}" y="46" width="{84 * frac:.0f}" height="8" rx="4"/>'
                b += text(x, 41, ("HULL", "CAPACITOR", "TARGET")[k], "mono tiny")
            else:
                b += f'<rect class="trackin" x="{x}" y="56" width="84" height="12" rx="6"/><rect class="fillbad" x="{x}" y="56" width="{84 * frac:.0f}" height="12" rx="6"/>'
                b += text(x, 46, ("HULL", "CAPACITOR", "TARGET")[k], "mono tiny")
        if ok:
            b += line(ox, 74, ox + 12, 74, "dim") + text(ox + 6, 88, "even margin", "lbl small", "start")
        b += text(ox + 150, 112, "Don't: bars hang over the strip's edge" if not ok else "Do: every bar sits inside the strip", "lbl", "middle")
    return svg("fit", 640, 122, b, "Children inside their parent, against children hanging over its edge")


def dont_pairs():
    """D7: six do and don't pairs, each a small top view."""
    cells = []

    def cell(body, cap, good):
        cells.append((body, cap, good))

    # 1 strip lamp vs line lamps
    win = rrect(10, 14, 150, 74, 6)
    cell(panel(rrect(2, 6, 158, 82, 10), [win]) + '<rect class="glare" x="14" y="16" width="132" height="10" rx="5"/>', "One strip lamp: a highlight across the glass", False)
    cell(panel(rrect(2, 6, 158, 82, 10), [win], lamps=((40, 14), (80, 14), (120, 14))) + "".join(
        f'<path class="spill" d="M{x - 6},17 L{x + 6},17 L{x + 22},52 L{x - 22},52 Z"/>' for x in (40, 80, 120)), "Short line lamps: soft fans, flat glass", True)
    # 2 primitive bump vs formed notch
    cell(panel(rrect(2, 6, 158, 82, 10), [rrect(10, 14, 150, 66, 6)]) + '<rect class="mt mte" x="56" y="50" width="48" height="26" rx="6"/>' + knob(80, 62, 7), "A block stuck on top", False)
    cell(panel(rrect(2, 6, 158, 82, 10), [shaped(10, 14, 150, 66, 6, bottom=flat(80, 16, 12, 16))]) + knob(80, 62, 7), "The window's edge rises: one piece", True)
    # 3 black ink vs tan ink
    cell(panel(rrect(2, 6, 158, 82, 10), [win]) + text(80, 52, "07:42", "blackink", "middle"), "Black paint on the film", False)
    cell(panel(rrect(2, 6, 158, 82, 10), [win]) + text(80, 52, "07:42", "inktext big", "middle"), "Tan ink that glows through", True)
    # 4 raised print vs flat print
    cell(panel(rrect(2, 6, 158, 82, 10), [win]) + text(82, 54, "07:42", "printshadow", "middle") + text(80, 52, "07:42", "inktext big", "middle"), "Raised print casting shadows", False)
    cell(panel(rrect(2, 6, 158, 82, 10), [win]) + text(80, 52, "07:42", "inktext big", "middle"), "Flat print, no shadow", True)
    # 5 point dots vs spill
    cell(panel(rrect(2, 6, 158, 82, 10), [win], lamps=((40, 14), (80, 14), (120, 14))) + "".join(
        f'<circle class="hot" cx="{x}" cy="19" r="5"/>' for x in (40, 80, 120)), "Lamps aimed into the film: dots", False)
    cell(panel(rrect(2, 6, 158, 82, 10), [win], lamps=((40, 14), (80, 14), (120, 14))) + "".join(
        f'<path class="spill" d="M{x - 6},17 L{x + 6},17 L{x + 26},62 L{x - 26},62 Z"/>' for x in (40, 80, 120)), "Lamps aimed down the screen: spill", True)
    # 6 polished knob black vs lit
    cell(panel(rrect(2, 6, 158, 82, 10), [rrect(10, 14, 150, 40, 6)]) + '<circle class="collar" cx="80" cy="62" r="11"/><circle class="blackknob" cx="80" cy="62" r="9"/>', "Polished knob mirroring a dark room", False)
    cell(panel(rrect(2, 6, 158, 82, 10), [rrect(10, 14, 150, 40, 6)]) + knob(80, 62, 9, -125), "A soft overhead light for it to reflect", True)
    out = []
    for i in range(0, len(cells), 2):
        pair = ""
        for j in (0, 1):
            body, cap, good = cells[i + j]
            pair += f'<figure class="dd {"do" if good else "dont"}">' + svg(f"dd{i + j}", 160, 88, body, cap) + f'<figcaption><b>{"Do" if good else "Don&#39;t"}</b> {cap}</figcaption></figure>'
        out.append(f'<div class="ddpair">{pair}</div>')
    return "".join(out)


def catalogue():
    """D8: the formed-metal catalogue, each shape drawn from its outline function."""
    P = rrect(4, 4, 196, 136, 12)
    items = []
    # 1 notch
    w = shaped(14, 14, 186, 100, 6, bottom=flat(100, 30, 18, 24))
    items.append(("notch", panel(P, [w]) + knob(84, 92, 7, -125) + knob(116, 92, 7, -90)))
    # 2 bridge: two openings whose inner sides flare into the frame
    l = shaped(14, 14, 92, 100, 6, right=flare(14, 100, 26, 8))
    r = shaped(108, 14, 186, 100, 6, left=flare(14, 100, 26, 8))
    items.append(("bridge", panel(P, [l, r]) + knob(100, 40, 5) + knob(100, 66, 5)))
    # 3 ear: a lobe from the right edge carrying one knob
    w = shaped(14, 14, 186, 100, 6, right=bell(57, 34, 32))
    items.append(("ear", panel(P, [w]) + knob(170, 57, 9, -150)))
    # 4 saddle: the bar between two windows bulges at its middle for a knob
    l = shaped(14, 14, 92, 100, 6, right=bell(57, 34, 12))
    r = shaped(108, 14, 186, 100, 6, left=bell(57, 34, 12))
    items.append(("saddle", panel(P, [l, r]) + knob(100, 57, 8)))
    # 5 scallop: the bottom edge rises in a row of bumps, a key on each
    w = shaped(14, 14, 186, 100, 6, bottom=waves(30, 170, 35, 18))
    items.append(("scallop", panel(P, [w]) + "".join(knob(30 + 17.5 + k * 35, 100, 5.5) for k in range(4))))
    # 6 cradle: a notch holding its own round meter
    w = shaped(14, 14, 186, 100, 6, bottom=flat(100, 26, 18, 40))
    m = circle(100, 86, 14)
    items.append(("cradle", panel(P, [w, m]) + line(100, 86, 108, 78, "pointer")))
    # 7 peninsula: a long tongue of metal carrying a row of keys into the film
    w = shaped(14, 14, 186, 100, 6, bottom=flat(100, 8, 12, 64))
    items.append(("peninsula", panel(P, [w]) + knob(100, 50, 5) + knob(100, 70, 5) + knob(100, 90, 5)))
    # 8 terrace: the bottom edge climbs in steps, a shelf for a key or a readout on each
    w = shaped(14, 14, 186, 100, 6, bottom=steps((60, 14, 16), (106, 14, 16), (156, 14, -32)))
    items.append(("terrace", panel(P, [w]) + knob(40, 108, 5) + knob(90, 92, 5) + knob(140, 76, 5)))
    # 9 bay: the window bulges outward into the metal to seat a round gauge
    w = shaped(14, 24, 186, 100, 6, top=lambda s: -bell(140, 30, 14)(s))
    items.append(("bay", panel(P, [w]) + path(circle(140, 34, 11), "subfl") + line(140, 34, 146, 26, "inkline")))
    # 10 swept edge: the top edge sweeps down across the window, a tapering display
    w = shaped(14, 14, 186, 100, 6, top=steps((24, 120, 40), (160, 16, -40)))
    items.append(("swept edge", panel(P, [w]) + knob(120, 30, 6)))
    out = {}
    for name, body in items:
        out[name] = svg("cat-" + name.replace(" ", "-"), 200, 140, body, f"Formed metal: the {name}")
    return out


# ----------------------------------------------------------------------------- write into the page

def blocks():
    b = {"section": section(), "anatomy": anatomy(), "notch": notch_profile(), "lights": lights(),
         "legibility": legibility(), "fit": fit(), "donts": dont_pairs()}
    for name, s in catalogue().items():
        b["cat-" + name.replace(" ", "-")] = s
    return b


def main():
    check = "--check" in sys.argv
    page = open(PAGE, encoding="utf-8").read()
    new = page
    for name, s in blocks().items():
        pat = re.compile(r"(<!-- SVG:%s -->).*?(<!-- /SVG:%s -->)" % (re.escape(name), re.escape(name)), re.S)
        if not pat.search(new):
            sys.exit(f"missing marker SVG:{name} in {PAGE}")
        new = pat.sub(lambda m: m.group(1) + s + m.group(2), new)
    if check:
        if new != page:
            sys.exit("ui-bible.html diagrams are out of date: run tools/mockups/ui_bible_svgs.py")
        print("ui-bible diagrams up to date")
        return
    open(PAGE, "w", encoding="utf-8").write(new)
    print("wrote", len(blocks()), "diagrams")


if __name__ == "__main__":
    main()
