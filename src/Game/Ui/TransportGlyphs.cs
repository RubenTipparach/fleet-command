// TransportGlyphs: the transport station's pictures, drawn like the helm's signage (thick strokes, filled shapes): a port
// of the battle console mockup's roleGlyph (a marine with a rifle, a bomb, a medical cross, a wrench, a chip, pilot's
// wings, a plain figure for the defenders' armed crew), itemGlyph (a strapped crate, a gear, a ration bowl with steam, a
// capsule, the envoy under a star) and the pad's ring of four lights. Drawn in one ink, the colour that reads on the key
// it sits on (KeyFace.Ink), so every skin draws them; one implementation for the role keys, the queue, the chips and the
// party panel (CLAUDE.md 6.1).
using System;
using System.Collections.Generic;
using Godot;

namespace FleetCommand.Game.Ui;

public static class TransportGlyphs
{
    private const float P = Mathf.Pi;

    /// <summary>A role's pictogram, centred, about 2r across.</summary>
    public static void Role(CanvasItem g, string role, float cx, float cy, float r, Color col)
    {
        float lw = Math.Max(2.2f, r * 0.22f);
        void Person(float ox)
        {
            g.DrawCircle(new Vector2(cx + ox, cy - r * 0.6f), r * 0.25f, col, true, -1, true);
            Shapes.RoundRect(g, new Rect2(cx + ox - r * 0.34f, cy - r * 0.28f, r * 0.68f, r * 1.12f), col, r * 0.2f);
        }
        void Line(float w, params (float X, float Y)[] pts)
        {
            var v = new Vector2[pts.Length];
            for (int i = 0; i < pts.Length; i++) v[i] = new Vector2(pts[i].X, pts[i].Y);
            g.DrawPolyline(v, col, w, true);
        }
        switch (role)
        {
            case "marines":
                Person(-r * 0.2f);
                Line(lw * 1.15f, (cx - r * 0.55f, cy + r * 0.62f), (cx + r * 0.85f, cy - r * 0.78f));
                g.DrawArc(new Vector2(cx - r * 0.2f, cy - r * 0.62f), r * 0.36f, P * 1.05f, P * 1.95f, 12, col, lw * 0.8f, true);
                break;
            case "demo":
                g.DrawCircle(new Vector2(cx - r * 0.15f, cy + r * 0.22f), r * 0.62f, col, true, -1, true);
                g.DrawPolyline(Quad(new Vector2(cx + r * 0.22f, cy - r * 0.25f), new Vector2(cx + r * 0.4f, cy - r * 0.7f), new Vector2(cx + r * 0.66f, cy - r * 0.62f)), col, lw * 0.8f, true);
                for (int k = 0; k < 4; k++)
                {
                    float a = k * P / 2 + P / 4, ox = cx + r * 0.78f, oy = cy - r * 0.76f;
                    Line(lw * 0.8f, (ox + Mathf.Cos(a) * r * 0.1f, oy + Mathf.Sin(a) * r * 0.1f), (ox + Mathf.Cos(a) * r * 0.28f, oy + Mathf.Sin(a) * r * 0.28f));
                }
                break;
            case "medics":
            {
                float a = r * 0.27f, b = r * 0.82f;
                g.DrawRect(new Rect2(cx - a, cy - b, 2 * a, 2 * b), col);
                g.DrawRect(new Rect2(cx - b, cy - a, 2 * b, 2 * a), col);
                break;
            }
            case "engineers":
                Line(lw * 1.35f, (cx - r * 0.7f, cy + r * 0.7f), (cx + r * 0.08f, cy - r * 0.08f));
                g.DrawArc(new Vector2(cx + r * 0.32f, cy - r * 0.32f), r * 0.4f, -P / 4 + 0.85f, -P / 4 - 0.85f + 2 * P, 20, col, lw * 1.1f, true);
                break;
            case "hackers":
            {
                float h = r * 0.48f, w = lw * 0.85f;
                g.DrawPolyline(new[] { new Vector2(cx - h, cy - h), new Vector2(cx + h, cy - h), new Vector2(cx + h, cy + h), new Vector2(cx - h, cy + h), new Vector2(cx - h, cy - h) }, col, w, true);
                foreach (float k in new[] { -0.55f, 0f, 0.55f })
                {
                    Line(w, (cx + k * h, cy - h), (cx + k * h, cy - h - r * 0.3f));
                    Line(w, (cx + k * h, cy + h), (cx + k * h, cy + h + r * 0.3f));
                    Line(w, (cx - h, cy + k * h), (cx - h - r * 0.3f, cy + k * h));
                    Line(w, (cx + h, cy + k * h), (cx + h + r * 0.3f, cy + k * h));
                }
                g.DrawRect(new Rect2(cx - h * 0.45f, cy - h * 0.45f, h * 0.9f, h * 0.9f), col);
                break;
            }
            case "pilots":
                g.DrawCircle(new Vector2(cx, cy), r * 0.22f, col, true, -1, true);
                foreach (int d in new[] { -1, 1 })
                {
                    Line(lw * 0.9f, (cx + d * r * 0.3f, cy - r * 0.05f), (cx + d * r * 0.98f, cy - r * 0.42f));
                    Line(lw * 0.9f, (cx + d * r * 0.3f, cy + r * 0.12f), (cx + d * r * 0.8f, cy - r * 0.04f));
                    Line(lw * 0.9f, (cx + d * r * 0.3f, cy + r * 0.28f), (cx + d * r * 0.6f, cy + r * 0.3f));
                }
                Pen.FillAbs(g, new[] { new Vector2(cx, cy - r * 0.85f), new Vector2(cx + r * 0.16f, cy - r * 0.5f), new Vector2(cx - r * 0.16f, cy - r * 0.5f) }, col);
                break;
            default: Person(0); break;
        }
    }

    /// <summary>An item's pictogram (goods, parts, rations, medicine, or "vip" the envoy), centred, about 2r across.</summary>
    public static void Item(CanvasItem g, string item, float cx, float cy, float r, Color col)
    {
        float lw = Math.Max(2.2f, r * 0.2f);
        switch (item)
        {
            case "goods":
            {
                Vector2 a = new(cx - r * 0.75f, cy - r * 0.6f), b = new(cx + r * 0.75f, cy - r * 0.6f), c = new(cx + r * 0.75f, cy + r * 0.7f), d = new(cx - r * 0.75f, cy + r * 0.7f);
                g.DrawPolyline(new[] { a, b, c, d, a }, col, lw, true);
                g.DrawLine(a, c, col, lw, true);
                g.DrawLine(b, d, col, lw, true);
                break;
            }
            case "parts":
            {
                // a gear: its teeth as one outline round an open hub (a canvas cuts the hub out; drawn as an outline here)
                var pts = new Vector2[17];
                for (int i = 0; i <= 16; i++) { float a = i % 16 / 16f * 2 * P, rr = i % 2 == 1 ? r * 0.62f : r * 0.88f; pts[i] = new Vector2(cx + Mathf.Cos(a) * rr, cy + Mathf.Sin(a) * rr); }
                g.DrawPolyline(pts, col, lw * 1.3f, true);
                g.DrawArc(new Vector2(cx, cy), r * 0.42f, 0, 2 * P, 20, col, lw * 1.1f, true);
                break;
            }
            case "rations":
            {
                var bowl = new List<Vector2> { new(cx - r * 0.85f, cy) };
                for (int i = 0; i <= 16; i++) { float a = i / 16f * P; bowl.Add(new Vector2(cx + Mathf.Cos(a) * r * 0.85f, cy + Mathf.Sin(a) * r * 0.85f)); }
                bowl.Reverse();
                Pen.FillAbs(g, bowl.ToArray(), col);
                foreach (float k in new[] { -0.4f, 0f, 0.4f })
                    g.DrawPolyline(Quad(new Vector2(cx + k * r, cy - r * 0.2f), new Vector2(cx + k * r + r * 0.18f, cy - r * 0.5f), new Vector2(cx + k * r, cy - r * 0.8f)), col, lw, true);
                break;
            }
            case "medicine":
            {
                // a capsule laid at 45 degrees, its one end filled
                Vector2 R(float x, float y) { float c = Mathf.Cos(-P / 4), s = Mathf.Sin(-P / 4); return new Vector2(cx + x * c - y * s, cy + x * s + y * c); }
                float h = r * 0.36f, l = r * 0.9f;
                var outline = new List<Vector2>();
                for (int i = 0; i <= 10; i++) { float a = -P / 2 + i / 10f * P; outline.Add(R(l - h + Mathf.Cos(a) * h, Mathf.Sin(a) * h)); }
                for (int i = 0; i <= 10; i++) { float a = P / 2 + i / 10f * P; outline.Add(R(-l + h + Mathf.Cos(a) * h, Mathf.Sin(a) * h)); }
                var half = new List<Vector2>();
                for (int i = 0; i <= 10; i++) { float a = P / 2 + i / 10f * P; half.Add(R(-l + h + Mathf.Cos(a) * h, Mathf.Sin(a) * h)); }
                half.Add(R(0, -h)); half.Add(R(0, h));
                Pen.FillAbs(g, half.ToArray(), col);
                outline.Add(outline[0]);
                g.DrawPolyline(outline.ToArray(), col, lw, true);
                break;
            }
            default:
            {
                g.DrawCircle(new Vector2(cx, cy - r * 0.32f), r * 0.22f, col, true, -1, true);
                Shapes.RoundRect(g, new Rect2(cx - r * 0.32f, cy - r * 0.04f, r * 0.64f, r * 0.92f), col, r * 0.18f);
                var star = new Vector2[10];
                for (int i = 0; i < 10; i++) { float a = -P / 2 + i * P / 5, rr = i % 2 == 1 ? r * 0.12f : r * 0.28f; star[i] = new Vector2(cx + Mathf.Cos(a) * rr, cy - r * 0.86f + Mathf.Sin(a) * rr); }
                Pen.FillAbs(g, star, col);
                break;
            }
        }
    }

    /// <summary>A transporter pad seen from above: a ring and four lights round it, in a square key of side ps.</summary>
    public static void Pad(CanvasItem g, float cx, float cy, float ps, Color col)
    {
        g.DrawArc(new Vector2(cx, cy), ps * 0.2f, 0, 2 * P, 24, col, Math.Max(2, ps * 0.06f), true);
        foreach (var (dx, dy) in new[] { (0, -1), (1, 0), (0, 1), (-1, 0) })
            g.DrawCircle(new Vector2(cx + dx * ps * 0.33f, cy + dy * ps * 0.33f), ps * 0.055f, col, true, -1, true);
    }

    // a quadratic curve as points (the canvas's quadraticCurveTo)
    private static Vector2[] Quad(Vector2 a, Vector2 c, Vector2 b)
    {
        var pts = new Vector2[13];
        for (int i = 0; i <= 12; i++) { float t = i / 12f; pts[i] = a * (1 - t) * (1 - t) + c * 2 * t * (1 - t) + b * t * t; }
        return pts;
    }
}
