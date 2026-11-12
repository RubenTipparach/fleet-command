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
    public void Circle(float cx, float cy, float r) => _c.DrawArc(P(cx, cy), r * _k, 0, Mathf.Tau, Segs(r), Col, W, true);
    /// <summary>A filled circle.</summary>
    public void Dot(float cx, float cy, float r) => _c.DrawCircle(P(cx, cy), r * _k, Col, true, -1, true);
    /// <summary>An arc's outline, angles in radians clockwise from +x.</summary>
    public void Arc(float cx, float cy, float r, float a0, float a1) => _c.DrawArc(P(cx, cy), r * _k, a0, a1, Math.Max(4, (int)(Segs(r) * Math.Abs(a1 - a0) / Mathf.Tau) + 2), Col, W, true);

    public void Line(params float[] xy)
    {
        var pts = new Vector2[xy.Length / 2];
        for (int i = 0; i < pts.Length; i++) pts[i] = P(xy[2 * i], xy[2 * i + 1]);
        _c.DrawPolyline(pts, Col, W, true);
    }

    public void Fill(IList<Vector2> local) => FillAbs(Map(local), Col);

    public void Stroke(IList<Vector2> local, bool closed = true)
    {
        Vector2[] p = Map(local);
        if (closed) { Array.Resize(ref p, p.Length + 1); p[^1] = p[0]; }
        _c.DrawPolyline(p, Col, W, true);
    }

    public void Rect(float x, float y, float w, float h) => _c.DrawRect(new Rect2(P(x, y), new Vector2(w, h) * _k), Col);

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
        _c.DrawPolyline(pts, Col, W, true);
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
        RenderingServer.CanvasItemAddTriangleArray(c.GetCanvasItem(), tris, poly, new[] { col });
        clean.Add(poly[0]);
        c.DrawPolyline(clean.ToArray(), col, 1, true);
    }

    private void FillAbs(Vector2[] pts, Color col) => FillAbs(_c, pts, col);

    // A polygon's triangles, remembered by its points: the console fills the same shapes every redraw, and Godot
    // triangulated each one again on every call (twice, with the check that it can be filled at all). Empty when
    // Godot's triangulator refuses the polygon.
    private static readonly Dictionary<int, int[]> Tris = new();

    private static int[] Triangles(Vector2[] poly)
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

/// <summary>Shapes drawn in absolute coordinates: rounded rectangles (a cached antialiased mesh) and ring segments.</summary>
public static class Shapes
{
    // A rounded rectangle is a mesh built once per size and corner radii, white, its edge feathered over AaPx like
    // Godot's StyleBoxFlat (whose look it keeps: CornerSegs points a corner, an antialiased rim), drawn with the colour
    // as its modulate. Drawing a StyleBoxFlat tessellated it again on every call, about a thousand times a console
    // redraw, the console's largest single cost on a Raspberry Pi 5 (docs/playtests/pi/README.md).
    private const int CornerSegs = 10;
    private const float AaPx = 0.8f;
    private static readonly Dictionary<(int, int, int, int, int, int), ArrayMesh> Meshes = new();

    public static void RoundRect(CanvasItem c, Rect2 r, Color col, float radius) => RoundRect(c, r, col, radius, radius, radius, radius);

    public static void RoundRect(CanvasItem c, Rect2 r, Color col, float tl, float tr, float br, float bl)
    {
        if (r.Size.X <= 0 || r.Size.Y <= 0) return;
        float lim = Math.Min(r.Size.X, r.Size.Y) / 2;
        // sizes in quarter pixels, radii in whole pixels (as the style boxes were)
        var key = ((int)MathF.Round(r.Size.X * 4), (int)MathF.Round(r.Size.Y * 4),
            (int)MathF.Round(Math.Min(tl, lim)), (int)MathF.Round(Math.Min(tr, lim)), (int)MathF.Round(Math.Min(br, lim)), (int)MathF.Round(Math.Min(bl, lim)));
        if (!Meshes.TryGetValue(key, out ArrayMesh? mesh))
        {
            if (Meshes.Count > 2048) Meshes.Clear();
            Meshes[key] = mesh = RoundRectMesh(key.Item1 / 4f, key.Item2 / 4f, key.Item3, key.Item4, key.Item5, key.Item6);
        }
        c.DrawMesh(mesh, null, new Transform2D(0, r.Position), col);
    }

    /// <summary>A w by h rounded rectangle from (0, 0): a fan over its outline drawn a half feather inside the edge, and
    /// a ring out to a half feather outside it that fades to clear.</summary>
    private static ArrayMesh RoundRectMesh(float w, float h, float tl, float tr, float br, float bl)
    {
        var outline = new List<Vector2>();
        var normals = new List<Vector2>();
        void Corner(float cx, float cy, float rad, float a0)
        {
            int n = rad > 0 ? CornerSegs : 0;
            for (int i = 0; i <= n; i++)
            {
                float t = a0 + Mathf.Pi / 2 * (n == 0 ? 0.5f : (float)i / n);
                var d = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
                outline.Add(new Vector2(cx, cy) + d * rad);
                normals.Add(rad > 0 ? d : d * Mathf.Sqrt2);   // a square corner's rim goes out along its diagonal
            }
        }
        Corner(w - tr, tr, tr, -Mathf.Pi / 2);       // top right, from the top edge round to the right
        Corner(w - br, h - br, br, 0);               // bottom right
        Corner(bl, h - bl, bl, Mathf.Pi / 2);        // bottom left
        Corner(tl, tl, tl, Mathf.Pi);                // top left
        int m = outline.Count;
        var pts = new Vector2[1 + m * 2];
        var cols = new Color[pts.Length];
        pts[0] = new Vector2(w / 2, h / 2);
        cols[0] = Colors.White;
        for (int i = 0; i < m; i++)
        {
            pts[1 + i] = outline[i] - normals[i] * (AaPx / 2);
            cols[1 + i] = Colors.White;
            pts[1 + m + i] = outline[i] + normals[i] * (AaPx / 2);
            cols[1 + m + i] = new Color(1, 1, 1, 0);
        }
        var idx = new List<int>(m * 9);
        for (int i = 0; i < m; i++)
        {
            int j = (i + 1) % m;
            idx.AddRange(new[] { 0, 1 + i, 1 + j });                              // the body
            idx.AddRange(new[] { 1 + i, 1 + m + i, 1 + m + j, 1 + i, 1 + m + j, 1 + j });   // the feathered rim
        }
        var arr = new Godot.Collections.Array();
        arr.Resize((int)Mesh.ArrayType.Max);
        arr[(int)Mesh.ArrayType.Vertex] = pts;
        arr[(int)Mesh.ArrayType.Color] = cols;
        arr[(int)Mesh.ArrayType.Index] = idx.ToArray();
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arr);
        return mesh;
    }

    /// <summary>A rounded rectangle's outline.</summary>
    public static void RoundRectLine(CanvasItem c, Rect2 r, Color col, float radius, float width)
    {
        float rad = Math.Min(radius, Math.Min(r.Size.X, r.Size.Y) / 2);
        var pb = new PathBuilder();
        Vector2 p = r.Position, s = r.Size;
        pb.ArcPts(p.X + rad, p.Y + rad, rad, Mathf.Pi, 1.5f * Mathf.Pi, 6).ArcPts(p.X + s.X - rad, p.Y + rad, rad, -0.5f * Mathf.Pi, 0, 6)
          .ArcPts(p.X + s.X - rad, p.Y + s.Y - rad, rad, 0, 0.5f * Mathf.Pi, 6).ArcPts(p.X + rad, p.Y + s.Y - rad, rad, 0.5f * Mathf.Pi, Mathf.Pi, 6);
        pb.Pts.Add(pb.Pts[0]);
        c.DrawPolyline(pb.Pts.ToArray(), col, width, true);
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
