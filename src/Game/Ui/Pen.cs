// Pen: vector drawing on a CanvasItem in a piece's own units (CLAUDE.md section 7: the Concord skin is flat vector).
// It mirrors the handful of 2D canvas calls the mockup's kit painters use (docs/mockups/lib/concord.js): circles, arcs,
// lines, polygons, rounded rectangles, ellipses and quadratic curves, so a kit piece ports call for call. Polygons get a
// one-pixel antialiased outline in their own colour, since Godot fills polygons without antialiasing.
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed class Pen
{
    private readonly CanvasItem _c;
    private readonly Vector2 _o;
    private readonly float _k;
    public Color Col;
    /// <summary>Stroke width in the piece's units.</summary>
    public float Width = 3;

    /// <param name="origin">Where the piece's (0, 0) lands.</param>
    /// <param name="scale">Pixels per piece unit.</param>
    public Pen(CanvasItem c, Vector2 origin, float scale, Color col) { _c = c; _o = origin; _k = scale; Col = col; }

    private Vector2 P(float x, float y) => _o + new Vector2(x, y) * _k;
    private float W => Math.Max(1f, Width * _k);

    /// <summary>A circle's outline.</summary>
    public void Circle(float cx, float cy, float r) => Draw2D.Arc(_c, P(cx, cy), r * _k, 0, Mathf.Tau, Segs(r), Col, W, true);
    /// <summary>A filled circle.</summary>
    public void Dot(float cx, float cy, float r) => Draw2D.Circle(_c, P(cx, cy), r * _k, Col, true, -1, true);
    /// <summary>An arc's outline, angles in radians clockwise from +x.</summary>
    public void Arc(float cx, float cy, float r, float a0, float a1) => Draw2D.Arc(_c, P(cx, cy), r * _k, a0, a1, Math.Max(4, (int)(Segs(r) * Math.Abs(a1 - a0) / Mathf.Tau) + 2), Col, W, true);

    public void Line(params float[] xy)
    {
        var pts = new Vector2[xy.Length / 2];
        for (int i = 0; i < pts.Length; i++) pts[i] = P(xy[2 * i], xy[2 * i + 1]);
        Draw2D.Polyline(_c, pts, Col, W, true);
    }

    public void Fill(IList<Vector2> local) => FillAbs(Map(local), Col);

    public void Stroke(IList<Vector2> local, bool closed = true)
    {
        Vector2[] p = Map(local);
        if (closed) { Array.Resize(ref p, p.Length + 1); p[^1] = p[0]; }
        Draw2D.Polyline(_c, p, Col, W, true);
    }

    public void Rect(float x, float y, float w, float h) => Draw2D.Rect(_c, new Rect2(P(x, y), new Vector2(w, h) * _k), Col);

    public void RoundRect(float x, float y, float w, float h, float r) => Shapes.RoundRect(_c, new Rect2(P(x, y), new Vector2(w, h) * _k), Col, r * _k);

    public void RoundRect(float x, float y, float w, float h, float tl, float tr, float br, float bl) =>
        Shapes.RoundRect(_c, new Rect2(P(x, y), new Vector2(w, h) * _k), Col, tl * _k, tr * _k, br * _k, bl * _k);

    public void Ellipse(float cx, float cy, float rx, float ry, float rot)
    {
        var pts = new Vector2[49];
        for (int i = 0; i <= 48; i++)
        {
            float a = i * Mathf.Tau / 48, ex = rx * Mathf.Cos(a), ey = ry * Mathf.Sin(a);
            pts[i] = P(cx + ex * Mathf.Cos(rot) - ey * Mathf.Sin(rot), cy + ex * Mathf.Sin(rot) + ey * Mathf.Cos(rot));
        }
        Draw2D.Polyline(_c, pts, Col, W, true);
    }

    private Vector2[] Map(IList<Vector2> local)
    {
        var p = new Vector2[local.Count];
        for (int i = 0; i < p.Length; i++) p[i] = _o + local[i] * _k;
        return p;
    }

    private int Segs(float r) => Math.Clamp((int)(r * _k * 0.9f) + 12, 16, 96);

    /// <summary>A filled polygon in absolute coordinates, with an antialiased edge. Repeated points (where a rounded
    /// corner meets a straight run) are dropped first, since Godot's triangulator refuses some polygons that have them.</summary>
    public static void FillAbs(CanvasItem c, Vector2[] pts, Color col)
    {
        var clean = new List<Vector2>(pts.Length);
        foreach (Vector2 p in pts) if (clean.Count == 0 || clean[^1].DistanceSquaredTo(p) > 1e-4f) clean.Add(p);
        while (clean.Count > 1 && clean[0].DistanceSquaredTo(clean[^1]) <= 1e-4f) clean.RemoveAt(clean.Count - 1);
        if (clean.Count < 3) return;
        Vector2[] poly = clean.ToArray();
        int[] tris = Triangles(poly);
        if (tris.Length == 0) { GD.PushError($"Pen: a polygon of {poly.Length} points cannot be filled"); return; }
        Draw2D.ColoredPolygon(c, poly, col);   // into the item's one batch of untextured drawing
        clean.Add(poly[0]);
        Draw2D.Polyline(c, clean.ToArray(), col, 1, true);
    }

    private void FillAbs(Vector2[] pts, Color col) => FillAbs(_c, pts, col);

    // A polygon's triangles, remembered by its points: the console fills the same shapes every redraw, and Godot
    // triangulated each one again on every call (twice, with the check that it can be filled at all). Empty when
    // Godot's triangulator refuses the polygon.
    private static readonly Dictionary<int, int[]> Tris = new();

    public static int[] Triangles(Vector2[] poly)
    {
        var h = new HashCode();
        foreach (Vector2 p in poly) h.Add(p);
        int key = h.ToHashCode();
        if (Tris.TryGetValue(key, out int[]? t) && t.Length % 3 == 0 && (t.Length == 0 || t.Max() < poly.Length)) return t;
        if (Tris.Count > 4096) Tris.Clear();
        return Tris[key] = Geometry2D.TriangulatePolygon(poly);
    }
}

/// <summary>Builds outlines point by point: lines, rounded corners and quadratic curves (a canvas path's moveTo and friends).</summary>
public sealed class PathBuilder
{
    public readonly List<Vector2> Pts = new();
    public PathBuilder To(float x, float y) { Pts.Add(new Vector2(x, y)); return this; }

    /// <summary>A quadratic curve from the last point through control (cx, cy) to (x, y).</summary>
    public PathBuilder Quad(float cx, float cy, float x, float y, int n = 10)
    {
        Vector2 p0 = Pts[^1], c = new(cx, cy), p1 = new(x, y);
        for (int i = 1; i <= n; i++) { float t = i / (float)n; Pts.Add((1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * c + t * t * p1); }
        return this;
    }

    /// <summary>A rounded corner at <paramref name="corner"/>, arriving along dirIn and leaving along dirOut (a canvas arcTo).</summary>
    public PathBuilder Corner(Vector2 corner, Vector2 dirIn, Vector2 dirOut, float r, int n = 8)
    {
        Vector2 a = corner - dirIn * r, b = corner + dirOut * r, centre = a + dirOut * r;
        float a0 = (a - centre).Angle(), a1 = (b - centre).Angle(), d = Mathf.Wrap(a1 - a0, -Mathf.Pi, Mathf.Pi);
        for (int i = 0; i <= n; i++) { float t = a0 + d * i / n; Pts.Add(centre + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r); }
        return this;
    }

    /// <summary>Points of an arc round (cx, cy), angles in radians clockwise from +x.</summary>
    public PathBuilder ArcPts(float cx, float cy, float r, float a0, float a1, int n = 16)
    {
        for (int i = 0; i <= n; i++) { float t = a0 + (a1 - a0) * i / n; Pts.Add(new Vector2(cx + Mathf.Cos(t) * r, cy + Mathf.Sin(t) * r)); }
        return this;
    }
}

/// <summary>Shapes drawn in absolute coordinates: rounded rectangles and their outlines (9-slices from a corner atlas)
/// and ring segments.</summary>
public static class Shapes
{
    // A rounded rectangle, filled or outlined, is a 9-slice drawn from one shared atlas of white corner shapes, tinted by
    // its colour (the owner, 2026-11-12: "Rounded rectangles? shouldnt that just be texture-fied", "we talked about
    // sliing stuff in the ui document"). Each atlas cell is one set of corner radii (and line width), drawn once by its
    // signed distance with the edge feathered over AaPx as Godot's StyleBoxFlat was, a pixel of clear round it. Drawing
    // a StyleBoxFlat tessellated it again on every call, about a thousand times a console redraw, the console's largest
    // single cost on a Raspberry Pi 5 (docs/playtests/pi/README.md); a 9-slice from one texture batches with its
    // neighbours instead.
    private const float AaPx = 0.8f;
    private const int AtlasPx = 1024;
    private static readonly Dictionary<(int, int, int, int, int), (int Page, Rect2 Src, int L, int T, int R, int B)> Cells = new();
    private static readonly List<(Image Img, ImageTexture Tex)> Pages = new();
    private static int _cx, _cy, _rowH;

    public static void RoundRect(CanvasItem c, Rect2 r, Color col, float radius) => RoundRect(c, r, col, radius, radius, radius, radius);

    public static void RoundRect(CanvasItem c, Rect2 r, Color col, float tl, float tr, float br, float bl)
    {
        if (r.Size.X <= 0 || r.Size.Y <= 0) return;
        float lim = Math.Min(r.Size.X, r.Size.Y) / 2;
        Slice(c, r, col, (int)MathF.Round(Math.Min(tl, lim)), (int)MathF.Round(Math.Min(tr, lim)), (int)MathF.Round(Math.Min(br, lim)), (int)MathF.Round(Math.Min(bl, lim)), 0);
    }

    /// <summary>A rounded rectangle's outline, <paramref name="width"/> wide, centred on the rectangle's edge.</summary>
    public static void RoundRectLine(CanvasItem c, Rect2 r, Color col, float radius, float width)
    {
        if (r.Size.X <= 0 || r.Size.Y <= 0 || width <= 0) return;
        int rad = (int)MathF.Round(Math.Min(radius, Math.Min(r.Size.X, r.Size.Y) / 2));
        Slice(c, r, col, rad, rad, rad, rad, (int)MathF.Round(width * 4));
    }

    // Draws the cell for these radii (and line width in quarter pixels, 0 for a fill) stretched over r.
    private static void Slice(CanvasItem c, Rect2 r, Color col, int tl, int tr, int br, int bl, int line4)
    {
        var key = (tl, tr, br, bl, line4);
        if (!Cells.TryGetValue(key, out var cell)) Cells[key] = cell = Bake(tl, tr, br, bl, line4 / 4f);
        float pad = 1 + (line4 > 0 ? MathF.Ceiling(line4 / 8f) : 0);
        Draw2D.Flush(c);   // drawn over the untextured shapes before it
        RenderingServer.CanvasItemAddNinePatch(c.GetCanvasItem(), r.Grow(pad), cell.Src, Pages[cell.Page].Tex.GetRid(),
            new Vector2(cell.L, cell.T), new Vector2(cell.R, cell.B), RenderingServer.NinePatchAxisMode.Stretch, RenderingServer.NinePatchAxisMode.Stretch, true, col);
    }

    // One cell: the shape's corners at their radii, two pixels of straight edge between, and a clear border wide enough
    // for the feather (and half the line) outside the rectangle. Its margins are each corner side plus that border.
    private static (int, Rect2, int, int, int, int) Bake(int tl, int tr, int br, int bl, float line)
    {
        int pad = 1 + (line > 0 ? (int)MathF.Ceiling(line / 2) : 0);
        int L = Math.Max(tl, bl), R = Math.Max(tr, br), T = Math.Max(tl, tr), B = Math.Max(bl, br);
        int w = L + R + 2, h = T + B + 2, cw = w + pad * 2, ch = h + pad * 2;
        if (Pages.Count == 0 || _cx + cw > AtlasPx) { _cx = 0; _cy += _rowH; _rowH = 0; }
        if (Pages.Count == 0 || _cy + ch > AtlasPx)
        {
            var img = Image.CreateEmpty(AtlasPx, AtlasPx, false, Image.Format.Rgba8);
            Pages.Add((img, ImageTexture.CreateFromImage(img)));
            _cx = 0; _cy = 0; _rowH = 0;
        }
        int page = Pages.Count - 1, ox = _cx, oy = _cy;
        Image im = Pages[page].Img;
        for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
            {
                float px = x + 0.5f - pad, py = y + 0.5f - pad;
                float d = Sd(px, py, w, h, tl, tr, br, bl);
                float cov = line > 0 ? Math.Clamp(0.5f - (MathF.Abs(d) - line / 2) / AaPx, 0, 1) : Math.Clamp(0.5f - d / AaPx, 0, 1);
                im.SetPixel(ox + x, oy + y, new Color(1, 1, 1, cov));
            }
        Pages[page].Tex.Update(im);
        _cx += cw + 1; _rowH = Math.Max(_rowH, ch + 1);
        return (page, new Rect2(ox, oy, cw, ch), L + pad, T + pad, R + pad, B + pad);
    }

    // Signed distance from (x, y) to a w by h rectangle from (0, 0) with these corner radii; negative inside.
    private static float Sd(float x, float y, float w, float h, int tl, int tr, int br, int bl)
    {
        bool left = x < w / 2, top = y < h / 2;
        float rad = top ? (left ? tl : tr) : (left ? bl : br);
        float cx = left ? rad : w - rad, cy = top ? rad : h - rad;
        float qx = left ? cx - x : x - cx, qy = top ? cy - y : y - cy;   // how far past the corner's centre, outward
        if (qx > 0 && qy > 0) return MathF.Sqrt(qx * qx + qy * qy) - rad;
        float dx = Math.Max(-x, x - w), dy = Math.Max(-y, y - h);
        return Math.Max(dx, dy);
    }

    /// <summary>A segment of a ring between radii r0 and r1, angles a0 to a1 (the helm pad's keys).</summary>
    public static Vector2[] ArcSeg(Vector2 centre, float r0, float r1, float a0, float a1, int n = 14)
    {
        var pts = new Vector2[2 * (n + 1)];
        for (int i = 0; i <= n; i++)
        {
            float t = a0 + (a1 - a0) * i / n;
            pts[i] = centre + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r1;
            float u = a1 - (a1 - a0) * i / n;
            pts[n + 1 + i] = centre + new Vector2(Mathf.Cos(u), Mathf.Sin(u)) * r0;
        }
        return pts;
    }
}
