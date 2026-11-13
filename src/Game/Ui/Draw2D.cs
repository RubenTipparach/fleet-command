// Draw2D: the console's untextured drawing (lines, polylines, arcs, circles, polygons, rectangles) collected into one
// triangle buffer per canvas item and sent as one draw call (the owner, 2026-11-12: "get it down to less than 300 draw
// calls"; openspec/changes/console-draw-cost D4). Godot's canvas renderer draws each polygon-type command (an arc, a
// circle, an antialiased polyline, a filled polygon) as a draw call of its own; the console makes hundreds a frame. Each
// method here has the arguments of the CanvasItem call it stands for and tessellates the shape as Godot does, the
// antialiased ones with a feather a screen pixel wide fading to clear. The buffer is flushed (one triangle array, its
// colours per vertex) before anything textured is drawn on the same item (text, kit pieces, 9-slices, atlas icons,
// textures) and before its transform changes, and at the end of the item's _Draw (each console item's _Draw ends with
// Flush), so what is drawn over what is unchanged. Godot emits an item's draw signal before its _Draw, so that signal
// cannot be the end. Anything an item without that ending left behind is flushed just before the frame renders, so
// nothing is ever lost.
using System;
using System.Collections.Generic;
using Godot;

namespace FleetCommand.Game.Ui;

public static class Draw2D
{
    private sealed class Buffer
    {
        public readonly List<Vector2> Pts = new();
        public readonly List<Color> Cols = new();
        public readonly List<int> Idx = new();
        public float Px = 1;   // one screen pixel in the item's units, for this drawing
    }

    private static readonly Dictionary<ulong, Buffer> Buffers = new();
    private static readonly HashSet<ulong> Hooked = new();
    private static bool _safetyNet;

    // what an item left unflushed (one whose drawing does not end with Flush), sent before the frame renders
    private static void FlushAll()
    {
        foreach (ulong id in new List<ulong>(Buffers.Keys))
            if (Buffers[id].Idx.Count > 0 && GodotObject.InstanceFromId(id) is CanvasItem c && GodotObject.IsInstanceValid(c)) Flush(c);
    }

    private static Buffer Of(CanvasItem c)
    {
        ulong id = c.GetInstanceId();
        if (!Buffers.TryGetValue(id, out Buffer? b))
        {
            Buffers[id] = b = new Buffer();
            if (Hooked.Add(id)) c.TreeExiting += () => { Buffers.Remove(id); Hooked.Remove(id); };
            if (!_safetyNet) { _safetyNet = true; RenderingServer.FramePreDraw += FlushAll; }
        }
        if (b.Idx.Count == 0)
        {
            float k = (c.GetViewport().GetFinalTransform() * c.GetGlobalTransformWithCanvas()).Scale.X;
            b.Px = k > 1e-4f ? 1 / k : 1;
        }
        return b;
    }

    /// <summary>Sends what is collected for c as one draw call; called before c draws anything textured or changes its
    /// transform, and at the end of its drawing.</summary>
    public static void Flush(CanvasItem c)
    {
        if (!Buffers.TryGetValue(c.GetInstanceId(), out Buffer? b) || b.Idx.Count == 0) return;
        RenderingServer.CanvasItemAddTriangleArray(c.GetCanvasItem(), b.Idx.ToArray(), b.Pts.ToArray(), b.Cols.ToArray());
        b.Pts.Clear(); b.Cols.Clear(); b.Idx.Clear();
    }

    private static int V(Buffer b, Vector2 p, Color c) { b.Pts.Add(p); b.Cols.Add(c); return b.Pts.Count - 1; }
    private static void Tri(Buffer b, int i0, int i1, int i2) { b.Idx.Add(i0); b.Idx.Add(i1); b.Idx.Add(i2); }

    // a strip along pts, w wide, with a feather of f on each side fading to clear; closed joins the ends
    private static void Strip(Buffer b, IReadOnlyList<Vector2> pts, Color col, float w, float f, bool closed)
    {
        int n = pts.Count;
        if (n < 2) return;
        var clear = new Color(col, 0);
        float h = w / 2;
        int prevA = -1, prevB = -1, prevC = -1, prevD = -1;
        int first = -1;
        for (int i = 0; i < n; i++)
        {
            // the normal at a point: the mean of its two segments' (a miter, capped so a sharp turn does not spike)
            Vector2 dIn = i > 0 ? (pts[i] - pts[i - 1]) : closed ? (pts[i] - pts[n - 1]) : (pts[1] - pts[0]);
            Vector2 dOut = i < n - 1 ? (pts[i + 1] - pts[i]) : closed ? (pts[0] - pts[i]) : (pts[i] - pts[i - 1]);
            Vector2 nIn = dIn.LengthSquared() > 1e-12f ? new Vector2(-dIn.Y, dIn.X).Normalized() : Vector2.Zero;
            Vector2 nOut = dOut.LengthSquared() > 1e-12f ? new Vector2(-dOut.Y, dOut.X).Normalized() : nIn;
            if (nIn == Vector2.Zero) nIn = nOut;
            Vector2 nm = (nIn + nOut);
            nm = nm.LengthSquared() > 1e-8f ? nm.Normalized() : nOut;
            float scale = Math.Min(1 / Math.Max(nm.Dot(nOut), 0.25f), 4);
            Vector2 o = nm * scale;
            int a = V(b, pts[i] + o * (h + f), f > 0 ? clear : col), bb = V(b, pts[i] + o * h, col), c = V(b, pts[i] - o * h, col), d = V(b, pts[i] - o * (h + f), f > 0 ? clear : col);
            if (i == 0) first = a;
            if (prevA >= 0) Quads(b, prevA, prevB, prevC, prevD, a, bb, c, d, f > 0);
            prevA = a; prevB = bb; prevC = c; prevD = d;
        }
        if (closed && first >= 0) Quads(b, prevA, prevB, prevC, prevD, first, first + 1, first + 2, first + 3, f > 0);
    }

    private static void Quads(Buffer b, int a0, int b0, int c0, int d0, int a1, int b1, int c1, int d1, bool feather)
    {
        Tri(b, b0, c0, c1); Tri(b, b0, c1, b1);
        if (!feather) return;
        Tri(b, a0, b0, b1); Tri(b, a0, b1, a1);
        Tri(b, c0, d0, d1); Tri(b, c0, d1, c1);
    }

    /// <summary>CanvasItem.DrawLine.</summary>
    public static void Line(CanvasItem c, Vector2 from, Vector2 to, Color col, float width = -1, bool antialiased = false)
    {
        Buffer b = Of(c);
        Strip(b, new[] { from, to }, col, width <= 0 ? b.Px : width, antialiased ? b.Px : 0, false);
    }

    /// <summary>CanvasItem.DrawPolyline.</summary>
    public static void Polyline(CanvasItem c, Vector2[] points, Color col, float width = -1, bool antialiased = false)
    {
        Buffer b = Of(c);
        bool closed = points.Length > 2 && points[0].DistanceSquaredTo(points[^1]) < 1e-8f;
        Vector2[] pts = closed ? points[..^1] : points;
        Strip(b, pts, col, width <= 0 ? b.Px : width, antialiased ? b.Px : 0, closed);
    }

    /// <summary>CanvasItem.DrawArc.</summary>
    public static void Arc(CanvasItem c, Vector2 centre, float radius, float startAngle, float endAngle, int pointCount, Color col, float width = -1, bool antialiased = false)
    {
        if (pointCount < 2) return;
        var pts = new Vector2[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            float t = startAngle + (endAngle - startAngle) * i / (pointCount - 1);
            pts[i] = centre + new Vector2(MathF.Cos(t), MathF.Sin(t)) * radius;
        }
        Buffer b = Of(c);
        bool closed = MathF.Abs(endAngle - startAngle) >= Mathf.Tau - 1e-4f;
        Strip(b, closed ? pts[..^1] : pts, col, width <= 0 ? b.Px : width, antialiased ? b.Px : 0, closed);
    }

    /// <summary>CanvasItem.DrawCircle.</summary>
    public static void Circle(CanvasItem c, Vector2 position, float radius, Color col, bool filled = true, float width = -1, bool antialiased = false)
    {
        if (radius <= 0) return;
        Buffer b = Of(c);
        int n = Math.Clamp((int)(radius / b.Px * 0.9f) + 12, 16, 96);
        if (!filled) { Arc(c, position, radius, 0, Mathf.Tau, n + 1, col, width, antialiased); return; }
        int centre = V(b, position, col), start = b.Pts.Count;
        for (int i = 0; i < n; i++) { float t = Mathf.Tau * i / n; V(b, position + new Vector2(MathF.Cos(t), MathF.Sin(t)) * radius, col); }
        for (int i = 0; i < n; i++) Tri(b, centre, start + i, start + (i + 1) % n);
        if (antialiased)
        {
            var ring = new Vector2[n];
            for (int i = 0; i < n; i++) ring[i] = b.Pts[start + i];
            Strip(b, ring, col, 0, b.Px, true);
        }
    }

    /// <summary>CanvasItem.DrawColoredPolygon (untextured).</summary>
    public static void ColoredPolygon(CanvasItem c, Vector2[] points, Color col)
    {
        int[] tris = Pen.Triangles(points);
        if (tris.Length == 0) { GD.PushError($"Draw2D: a polygon of {points.Length} points cannot be filled"); return; }
        Buffer b = Of(c);
        int start = b.Pts.Count;
        foreach (Vector2 p in points) V(b, p, col);
        foreach (int t in tris) b.Idx.Add(start + t);
    }

    /// <summary>CanvasItem.DrawRect.</summary>
    public static void Rect(CanvasItem c, Rect2 rect, Color col, bool filled = true, float width = -1, bool antialiased = false)
    {
        Buffer b = Of(c);
        if (filled)
        {
            int i0 = V(b, rect.Position, col), i1 = V(b, new Vector2(rect.End.X, rect.Position.Y), col), i2 = V(b, rect.End, col), i3 = V(b, new Vector2(rect.Position.X, rect.End.Y), col);
            Tri(b, i0, i1, i2); Tri(b, i0, i2, i3);
            return;
        }
        Strip(b, new[] { rect.Position, new Vector2(rect.End.X, rect.Position.Y), rect.End, new Vector2(rect.Position.X, rect.End.Y) }, col,
            width <= 0 ? b.Px : width, antialiased ? b.Px : 0, true);
    }

    // ---- textured drawing and transforms: the buffer is flushed first, so order is kept ------------------------------
    public static void Texture(CanvasItem c, Texture2D t, Vector2 pos, Color? modulate = null) { Flush(c); c.DrawTexture(t, pos, modulate); }
    public static void TextureRect(CanvasItem c, Texture2D t, Rect2 r, bool tile, Color? modulate = null, bool transpose = false) { Flush(c); c.DrawTextureRect(t, r, tile, modulate, transpose); }
    public static void TextureRectRegion(CanvasItem c, Texture2D t, Rect2 r, Rect2 src, Color? modulate = null, bool transpose = false, bool clipUV = true) { Flush(c); c.DrawTextureRectRegion(t, r, src, modulate, transpose, clipUV); }
    public static void SetTransform(CanvasItem c, Vector2 pos, float rot = 0, Vector2? scale = null) { Flush(c); c.DrawSetTransform(pos, rot, scale); }
    public static void DashedLine(CanvasItem c, Vector2 from, Vector2 to, Color col, float width = -1, float dash = 2, bool aligned = true, bool antialiased = false) { Flush(c); c.DrawDashedLine(from, to, col, width, dash, aligned, antialiased); }
}
