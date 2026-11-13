// HelmGlyphs: the helm keys' pictures, drawn like signage (owner, 2026-11-02: "please fix these icons to look like
// actual icons I'd see in an airport or something"): thick strokes, filled arrowheads, familiar shapes. A port of the
// battle console mockup's helmGlyph (v65), shape for shape. Turns are road-sign turn arrows, 180 the U-turn sign, free
// heading straight ahead, intercept an arrow into a target, trail two ships in line, orbit a circular arrow round a dot,
// hold distance an arrow stopped short of a target, hit and run a strafing pass, evasive a zigzag, stop an octagon.
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FleetCommand.Game.Ui;

public static class HelmGlyphs
{
    public static void Draw(CanvasItem g, Fonts fonts, string kind, float cx, float cy, float r, Color col, int orbitDir, Color stopInk)
    {
        float lw = Math.Max(3, r * 0.24f), P = Mathf.Pi;
        void Head(float x, float y, float a, float l, float w) =>
            Pen.FillAbs(g, new[] { new Vector2(x, y), new Vector2(x - Mathf.Cos(a) * l - Mathf.Sin(a) * w, y - Mathf.Sin(a) * l + Mathf.Cos(a) * w), new Vector2(x - Mathf.Cos(a) * l + Mathf.Sin(a) * w, y - Mathf.Sin(a) * l - Mathf.Cos(a) * w) }, col);
        void Line(float width, params (float X, float Y)[] pts) => Draw2D.Polyline(g, pts.Select(p => new Vector2(p.X, p.Y)).ToArray(), col, width, true);
        void Target(float x, float y, float rr) { Draw2D.Arc(g, new Vector2(x, y), rr, 0, Mathf.Tau, 28, col, lw * 0.6f, true); Draw2D.Circle(g, new Vector2(x, y), rr * 0.35f, col, true, -1, true); }
        // a road-sign turn: a stem up from the bottom, the bend by deg toward dir (-1 left, 1 right), a short run, a head;
        // built at the origin, measured, and centred in the key
        void Turn(float deg, int dir, float scale = 1)
        {
            float rr = r * 0.34f * scale, th = deg * P / 180, stem = r * 0.55f * scale, run = r * 0.22f * scale, hl = r * 0.4f * scale, hw = r * 0.34f * scale;
            var C = new Vector2(dir * rr, 0);
            Vector2 At(float a) => new(C.X - dir * rr * Mathf.Cos(a), C.Y - rr * Mathf.Sin(a));
            float ta = Mathf.Atan2(-Mathf.Cos(th), dir * Mathf.Sin(th));
            var u = new Vector2(Mathf.Cos(ta), Mathf.Sin(ta));
            var pts = new List<Vector2> { new(0, stem), new(0, 0) };
            for (int i = 1; i <= 24; i++) pts.Add(At(th * i / 24));
            Vector2 E = pts[^1], F = E + u * run, tip = F + u * hl;
            pts.Add(F);
            Vector2 w0 = new(F.X - u.Y * hw, F.Y + u.X * hw), w1 = new(F.X + u.Y * hw, F.Y - u.X * hw);
            var all = pts.Concat(new[] { tip, w0, w1 }).ToArray();
            float x0 = all.Min(q => q.X), x1 = all.Max(q => q.X), y0 = all.Min(q => q.Y), y1 = all.Max(q => q.Y);
            var o = new Vector2(cx - (x0 + x1) / 2, cy - (y0 + y1) / 2);
            Draw2D.Polyline(g, pts.Select(q => q + o).ToArray(), col, lw, true);
            Pen.FillAbs(g, new[] { tip + o, w0 + o, w1 + o }, col);
        }
        switch (kind)
        {
            case "het.l90": Turn(90, -1); break;
            case "het.r90": Turn(90, 1); break;
            case "het.l135": Turn(135, -1); break;
            case "het.r135": Turn(135, 1); break;
            case "het.180": Turn(180, -1); break;
            case "free": Line(lw, (cx, cy + r * 0.75f), (cx, cy - r * 0.3f)); Head(cx, cy - r * 0.75f, -P / 2, r * 0.48f, r * 0.42f); break;
            case "het.free":
            {
                Turn(70, 1, 0.85f);
                float x0 = cx + r * 0.55f, y0 = cy - r * 0.55f;
                Draw2D.Line(g, new Vector2(x0 - r * 0.3f, y0), new Vector2(x0 + r * 0.3f, y0), col, lw * 0.55f, true);
                Draw2D.Line(g, new Vector2(x0, y0 - r * 0.3f), new Vector2(x0, y0 + r * 0.3f), col, lw * 0.55f, true);
                break;
            }
            case "intercept":
                Target(cx + r * 0.35f, cy - r * 0.35f, r * 0.42f);
                Line(lw, (cx - r * 0.75f, cy + r * 0.75f), (cx - r * 0.12f, cy + r * 0.12f));
                Head(cx + r * 0.08f, cy - r * 0.08f, -P / 4, r * 0.4f, r * 0.32f);
                break;
            case "trail":
            {
                void Ship2(float y, float s2) => Pen.FillAbs(g, new[] { new Vector2(cx, y - s2), new Vector2(cx + s2 * 0.8f, y + s2 * 0.7f), new Vector2(cx, y + s2 * 0.35f), new Vector2(cx - s2 * 0.8f, y + s2 * 0.7f) }, col);
                Ship2(cy - r * 0.45f, r * 0.38f); Ship2(cy + r * 0.45f, r * 0.38f);
                Line(lw * 0.5f, (cx - r * 0.75f, cy - r * 0.02f), (cx - r * 0.45f, cy - r * 0.02f));
                Line(lw * 0.5f, (cx + r * 0.45f, cy - r * 0.02f), (cx + r * 0.75f, cy - r * 0.02f));
                break;
            }
            case "orbit":
            case "orbit.dir":
            {
                int d = orbitDir;
                float R2 = r * 0.6f, a0 = -P / 2 + d * 0.35f, a1 = a0 + d * 1.55f * P;
                if (kind == "orbit") Draw2D.Circle(g, new Vector2(cx, cy), r * 0.18f, col, true, -1, true);
                Draw2D.Arc(g, new Vector2(cx, cy), R2, Math.Min(a0, a1), Math.Max(a0, a1), 40, col, lw, true);
                Head(cx + Mathf.Cos(a1 + d * 0.32f) * R2, cy + Mathf.Sin(a1 + d * 0.32f) * R2, a1 + d * (P / 2 + 0.16f), r * 0.42f, r * 0.34f);
                break;
            }
            case "hold_range":
                Target(cx + r * 0.42f, cy, r * 0.36f);
                Line(lw, (cx - r * 0.8f, cy), (cx - r * 0.35f, cy));
                Head(cx - r * 0.12f, cy, 0, r * 0.32f, r * 0.3f);
                Line(lw * 0.7f, (cx - r * 0.02f, cy - r * 0.45f), (cx - r * 0.02f, cy + r * 0.45f));
                break;
            case "hit_run":
            {
                Target(cx + r * 0.05f, cy - r * 0.62f, r * 0.26f);
                var p0 = new Vector2(cx - r * 0.62f, cy + r * 0.8f); var ctl = new Vector2(cx + r * 0.02f, cy - r * 0.62f); var p3 = new Vector2(cx + r * 0.45f, cy + r * 0.42f);
                var curve = new PathBuilder().To(p0.X, p0.Y).Quad(ctl.X, ctl.Y, p3.X, p3.Y, 16);
                Draw2D.Polyline(g, curve.Pts.ToArray(), col, lw, true);
                float a = Mathf.Atan2(p3.Y - ctl.Y, p3.X - ctl.X);
                Head(p3.X + Mathf.Cos(a) * r * 0.36f, p3.Y + Mathf.Sin(a) * r * 0.36f, a, r * 0.4f, r * 0.32f);
                break;
            }
            case "erratic":
            {
                (float X, float Y)[] pts = { (cx - r * 0.05f, cy + r * 0.85f), (cx + r * 0.5f, cy + r * 0.38f), (cx - r * 0.5f, cy - r * 0.02f), (cx + r * 0.12f, cy - r * 0.42f) };
                float a = Mathf.Atan2(pts[3].Y - pts[2].Y, pts[3].X - pts[2].X);
                Line(lw, pts);
                Head(pts[3].X + Mathf.Cos(a) * r * 0.4f, pts[3].Y + Mathf.Sin(a) * r * 0.4f, a, r * 0.48f, r * 0.38f);
                break;
            }
            case "estop":
            {
                var oct = new Vector2[8];
                for (int i = 0; i < 8; i++) { float a = P / 8 + i * P / 4; oct[i] = new Vector2(cx + Mathf.Cos(a) * r * 0.8f, cy + Mathf.Sin(a) * r * 0.8f); }
                Pen.FillAbs(g, oct, col);
                fonts.Draw(g, "STOP", cx, cy + r * 0.17f, Math.Max(8, (int)Math.Round(r * 0.46f)), stopInk, HorizontalAlignment.Center, 0, 700);
                break;
            }
        }
    }
}
