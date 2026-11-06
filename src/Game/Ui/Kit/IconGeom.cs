// IconGeom: the small point-list helpers the skins' icon painters share (each skin's icons are drawn on a 48 x 48 grid
// from lists of points, as the mockups' icon() functions write them). One copy for every kit (CLAUDE.md section 6.1).
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FleetCommand.Game.Ui.Kit;

public static class IconGeom
{
    /// <summary>Points from pairs of numbers: Pts(x0, y0, x1, y1, ...).</summary>
    public static Vector2[] Pts(params float[] xy)
    {
        var p = new Vector2[xy.Length / 2];
        for (int i = 0; i < p.Length; i++) p[i] = new Vector2(xy[2 * i], xy[2 * i + 1]);
        return p;
    }

    /// <summary>The points of an arc round (cx, cy), radius r, from angle a0 to a1 (radians, clockwise from +x).</summary>
    public static Vector2[] ArcPts(float cx, float cy, float r, float a0, float a1, int n = 24) =>
        Enumerable.Range(0, n + 1).Select(i => { float a = a0 + (a1 - a0) * i / n; return new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r); }).ToArray();

    /// <summary>An ellipse's outline round (cx, cy), radii rx and ry, turned by rot radians.</summary>
    public static Vector2[] EllipsePts(float cx, float cy, float rx, float ry, float rot = 0, int n = 40) =>
        Enumerable.Range(0, n).Select(i =>
        {
            float a = i * Mathf.Tau / n, ex = rx * Mathf.Cos(a), ey = ry * Mathf.Sin(a);
            return new Vector2(cx + ex * Mathf.Cos(rot) - ey * Mathf.Sin(rot), cy + ex * Mathf.Sin(rot) + ey * Mathf.Cos(rot));
        }).ToArray();

    /// <summary>A rounded rectangle's outline from (x0, y0) to (x1, y1), corner radius r (a canvas roundRect).</summary>
    public static Vector2[] RoundRectPts(float x0, float y0, float x1, float y1, float r)
    {
        float rad = Mathf.Min(r, Mathf.Min(x1 - x0, y1 - y0) / 2), P = Mathf.Pi;
        var pts = new List<Vector2>();
        pts.AddRange(ArcPts(x0 + rad, y0 + rad, rad, P, 1.5f * P, 6));
        pts.AddRange(ArcPts(x1 - rad, y0 + rad, rad, -0.5f * P, 0, 6));
        pts.AddRange(ArcPts(x1 - rad, y1 - rad, rad, 0, 0.5f * P, 6));
        pts.AddRange(ArcPts(x0 + rad, y1 - rad, rad, 0.5f * P, P, 6));
        return pts.ToArray();
    }
}
