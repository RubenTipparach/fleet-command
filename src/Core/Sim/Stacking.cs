// Stacking: how high each ship is drawn so that hulls sharing a patch of the plane never pass through each other (owner,
// 2026-11-09: "in SFC smaller ships would change their height and fly above larger ships so that ships wouldnt clip inside
// of each other", "health/speed? ... something must be done to sort them out"). The battle is flat and this changes
// nothing in it: the 3D view (BattleView.Stack) reads the heights to place its models, shots and brackets. It lives here,
// with no Godot reference, so the rule is tested headless.
//
// The ships are laid in rank, the longest first. A ship stays on the plane unless its footprint (its hull's box turned to
// its heading, grown by a margin so the climb starts before they touch) overlaps one laid before it; then it is placed
// clear above that one, and again above any it now meets. Ships of one length rank by a key that never changes in a
// battle, so two sister ships never swap layers mid-pass (a rank by hull or speed would, and each swap would drop one
// through the other): the ship you command stays on the plane, and the rest keep the battle's order.
using System;
using System.Collections.Generic;
using System.Linq;

namespace FleetCommand.Sim;

public static class Stacking
{
    /// <summary>The clear air between a lifted hull and the one under it, as a share of the lower hull's height; the margin
    /// round a footprint, as a share of its own length.</summary>
    public const double GapX = 0.35, MarginX = 0.2;

    /// <summary>A ship as the stacking sees it: its place (any one unit, the same for all), heading (radians), half its
    /// width and length, its lowest and highest points about its origin, and its rank keys.</summary>
    public readonly record struct Hull(string Id, double X, double Z, double H, double HalfW, double HalfL, double Lo, double Hi, bool Player, int Order);

    /// <summary>The height each ship is drawn at, in the hulls' unit.</summary>
    public static Dictionary<string, double> Heights(IEnumerable<Hull> hulls)
    {
        var laid = new List<(Hull S, double Y)>();
        foreach (Hull s in hulls.OrderByDescending(s => s.HalfL).ThenBy(s => s.Player ? 0 : 1).ThenBy(s => s.Order))
        {
            double y = 0;
            // climb over every ship laid before it that it overlaps, until it is clear of all of them
            for (bool moved = true; moved;)
            {
                moved = false;
                foreach (var (o, oy) in laid)
                {
                    if (!Overlap(s, o)) continue;
                    double gap = (o.Hi - o.Lo) * GapX;
                    if (y + s.Lo >= oy + o.Hi + gap || y + s.Hi <= oy + o.Lo - gap) continue;
                    y = oy + o.Hi + gap - s.Lo;
                    moved = true;
                }
            }
            laid.Add((s, y));
        }
        return laid.ToDictionary(l => l.S.Id, l => l.Y);
    }

    /// <summary>Whether two footprints (boxes turned to their headings, each grown by its margin) overlap on the plane: the
    /// separating axis test on the four axes of the two boxes.</summary>
    public static bool Overlap(Hull a, Hull b)
    {
        double aw = a.HalfW + a.HalfL * MarginX, al = a.HalfL * (1 + MarginX), bw = b.HalfW + b.HalfL * MarginX, bl = b.HalfL * (1 + MarginX);
        double dx = b.X - a.X, dz = b.Z - a.Z;
        if (Math.Sqrt(dx * dx + dz * dz) > Math.Sqrt(aw * aw + al * al) + Math.Sqrt(bw * bw + bl * bl)) return false;
        var (afx, afz) = Geometry.Forward(a.H);
        var (bfx, bfz) = Geometry.Forward(b.H);
        foreach (var (x, z) in new[] { (afx, afz), (-afz, afx), (bfx, bfz), (-bfz, bfx) })
        {
            double ra = aw * Math.Abs(-afz * x + afx * z) + al * Math.Abs(afx * x + afz * z);
            double rb = bw * Math.Abs(-bfz * x + bfx * z) + bl * Math.Abs(bfx * x + bfz * z);
            if (Math.Abs(dx * x + dz * z) > ra + rb) return false;
        }
        return true;
    }
}
