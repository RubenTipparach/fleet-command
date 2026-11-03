"""ON HOLD, NOT YET RUN (owner, 2026-11-03: "the shapes matter before we go 3d with it"). These three 3D designs came
before the sketch-first rule and do not match the Concord sketches on the Emblem Design Guide; once the owner picks a
sketch, this file is rewritten from it. The swept-form helpers in emblem_render.py (sweep, outline_slab, cut_gem) have
not been rendered yet either.

Fleet Command: the Concord's emblem, rendered as a sculpted badge (openspec/changes/faction-emblems; the Emblem Design
Guide, docs/design/emblem-guide.html). The shared rig, materials, sculpted forms and outputs are
tools/blender/emblem_render.py; this file owns the Concord's designs.

The owner, 2026-11-03, on the first render (a ring of five domes): "I'm not a fan of the circle design here", "its too
simple and easy to make, see our rules about just slapping on primitives", "this is bad design", "make a few different
designs". So every design here is built from swept, tapering forms (a path and a section), faceted gems and inlaid
enamel, and none has a circular outline. Three directions, each from the Concord's ideology (console skins design:
a federation of many species that decides by consent; explores, mediates, defends; saucers and slender nacelles, long
smooth sweeps; LCARS's rule that a frame changes thickness at every turn):

- A "Five Sweeps": five founding worlds as five swept blades turning together round an open centre, each carrying its
  world as a cut gem; many peoples, one motion.
- B "The Sweep": the Concord's ship read as a mark: a saucer frame that thickens as it turns, two long nacelle wings
  running back thick to thin, a field of night sky inside the saucer and the Accord's star in it.
- C "The Accord": two hands of swept metal cupped together at the base, holding five worlds in an arc above them, the
  top left open for any people who join.

    /opt/blender/blender -b -P tools/blender/emblem_concord.py -- [--design A|B|C|all] [--out DIR] [--samples N] [--px N]

Writes <out>/concord/design-<X>.png (and with --steps a clay blockout of each, design-<X>-clay.png).
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import emblem_render as R  # noqa: E402

FACTION = "concord"
P = R.polar


def materials():
    return {
        "plat": R.mm_material("emblem_platinum"),
        "signal": R.mm_material("emblem_enamel_signal", coat=1.0),
        "night": R.mm_material("emblem_enamel_night", coat=1.0, emission_strength=2.5),
        "sky": R.principled("concord.gem", "#3fa9f0", rough=0.02, coat=1.0, emission="#3fa9f0", strength=0.15,
                            transmission=0.0),
        "star": R.principled("concord.star", "#eef3ff", rough=0.02, coat=1.0, emission="#cfe3ff", strength=1.2),
    }


def design_a(m):
    """Five Sweeps. Each blade roots at 9 mm from the centre and sweeps 75 degrees round to a point at 47 mm: an enamel
    bed with a platinum spine along it, swelling at its middle where its world sits."""
    for i in range(5):
        a = 90 + i * 72
        path = [P(9, a - 10), P(20, a + 6), P(33, a + 30), P(43, a + 58), P(47, a + 78)]
        taper = ((0.0, 0.55), (0.35, 1.0), (0.7, 0.7), (1.0, 0.0))
        R.sweep(f"blade.{i}.bed", path, 15.0, 3.2, "bed", m["signal"], z=0.0, taper=taper)
        R.sweep(f"blade.{i}.spine", path, 4.0, 5.4, "blade", m["plat"], z=0.0, taper=((0.0, 0.7), (0.4, 1.0), (1.0, 0.0)))
        # A rolled platinum edge along the blade's leading side, so the outline is a metal frame round the enamel.
        lead = [P(9.5, a - 22), P(19, a - 10), P(31, a + 14), P(42, a + 44), P(47, a + 78)]
        R.sweep(f"blade.{i}.edge", lead, 2.4, 4.0, "roll", m["plat"], z=0.0, taper=((0.0, 0.8), (0.5, 1.0), (1.0, 0.2)))
        gx, gy = P(25, a + 14)
        R.cut_gem(f"world.{i}", gx, gy, 4.2, 5, m["sky"], z=3.2)
    # The open centre: a pentagonal collar where the five roots meet, nothing inside it.
    collar = [P(9.5, 90 + k * 72) for k in range(5)]
    R.sweep("collar", collar, 3.0, 5.0, "roll", m["plat"], z=0.0, cyclic=True)


def design_b(m):
    """The Sweep. A saucer frame (an oval wider than tall, its rim thick at the base and thin at the top), two nacelle
    wings from its lower sides running down and out, thick to thin; night enamel inside with the Accord's star."""
    saucer = [(0, 30), (24, 22), (30, 6), (22, -10), (0, -16), (-22, -10), (-30, 6), (-24, 22)]
    R.outline_slab("saucer.field", [(x * 0.9, y * 0.9 + 0.6) for x, y in saucer], 0.0, 1.6, 0.3, m["night"])
    R.sweep("saucer.rim", saucer, 6.5, 5.0, "roll", m["plat"], z=0.0, cyclic=True,
            taper=((0.0, 0.55), (0.25, 0.8), (0.5, 1.25), (0.75, 0.8), (1.0, 0.55)))
    for s in (1, -1):
        wing = [(s * 18, -10), (s * 30, -18), (s * 40, -32), (s * 46, -46)]
        R.sweep(f"wing.{s}.bed", wing, 13.0, 3.0, "bed", m["signal"], z=0.0, taper=((0.0, 1.0), (0.5, 0.8), (1.0, 0.25)))
        R.sweep(f"wing.{s}.spine", wing, 4.2, 5.2, "blade", m["plat"], z=0.0, taper=((0.0, 1.0), (1.0, 0.1)))
        R.cut_gem(f"wing.{s}.light", s * 44.5, -43.5, 3.0, 5, m["sky"], z=2.6)
    # The Accord's star: a five-pointed cut gem in the saucer, the five founding worlds as its points.
    R.cut_gem("accord", 0, 7, 9.0, 5, m["star"], z=1.6, table=0.35, crown=0.5)
    # A short dorsal spine above the saucer: the bow, pointing the way the Concord explores.
    R.sweep("bow", [(0, 30), (0, 38), (0, 46)], 5.0, 5.6, "blade", m["plat"], z=0.0, taper=((0.0, 1.0), (1.0, 0.0)))


def design_c(m):
    """The Accord. Two swept hands cupped together at the base, their fingers flaring out at the top; five worlds in
    an arc above them, the middle one highest; the arc open at the top."""
    for s in (1, -1):
        hand = [(s * 2, -44), (s * 20, -34), (s * 34, -14), (s * 40, 8), (s * 46, 26)]
        R.sweep(f"hand.{s}.bed", hand, 15.0, 3.2, "bed", m["signal"], z=0.0,
                taper=((0.0, 0.6), (0.3, 1.0), (0.75, 0.85), (1.0, 0.0)))
        R.sweep(f"hand.{s}.spine", hand, 4.0, 5.6, "blade", m["plat"], z=0.0, taper=((0.0, 0.6), (0.35, 1.0), (1.0, 0.0)))
        inner = [(s * 2, -36), (s * 14, -30), (s * 25, -16), (s * 30, 0)]
        R.sweep(f"hand.{s}.inner", inner, 2.4, 4.2, "roll", m["plat"], z=0.0, taper=((0.0, 1.0), (1.0, 0.2)))
    R.outline_slab("palm", [(0, -33), (14, -26), (20, -14), (0, -20), (-20, -14), (-14, -26)], 0.0, 1.6, 0.3, m["night"])
    for k, (r, deg, size) in enumerate(((30, 160, 4.0), (32, 125, 4.6), (34, 90, 5.6), (32, 55, 4.6), (30, 20, 4.0))):
        x, y = P(r, deg)
        R.cut_gem(f"world.{k}", x, y - 8, size, 5, m["sky"] if k != 2 else m["star"], z=0.6)


DESIGNS = {"A": design_a, "B": design_b, "C": design_c}


def main():
    a = R.args()
    which = os.environ.get("EMBLEM_DESIGN", "all")
    for k, v in zip(sys.argv, sys.argv[1:]):
        if k == "--design":
            which = v
    out = os.path.join(a["out"], FACTION)
    for name in (DESIGNS if which == "all" else [which]):
        R.reset()
        R.setup(px=a["px"], samples=a["samples"])
        m = materials()
        DESIGNS[name](m)
        if a["steps"]:
            restore = R.with_material(R.clay())
            R.render(os.path.join(out, f"design-{name}-clay.png"))
            restore()
        R.render(os.path.join(out, f"design-{name}.png"))


main()
