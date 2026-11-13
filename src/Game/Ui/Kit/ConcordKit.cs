// ConcordKit: the Concord skin (console-skins design, "Concord"): flat back-lit vector shapes on black, sweeping frames
// that change thickness at every turn, round-capped pill buttons, at most five colours each with one meaning. A port of
// docs/mockups/lib/concord.js (battle console v65), shape for shape, so the game draws what the owner reviewed. Every
// piece is drawn whole at its rectangle in vector, which is the 9-slice contract (fixed corners, stretched edges and
// centre) with no seam between slices. Its style holds the values the console drew before skins had styles, so the
// Concord looks as it did.
using System;
using System.Collections.Generic;
using FleetCommand.Game.Ui;
using Godot;

namespace FleetCommand.Game.Ui.Kit;

public sealed class ConcordKit : IKit
{
    public string Name => "Concord";
    public Palette C { get; } = new(
        Deep: new Color("#1b2a6b"), Signal: new Color("#2f62e0"), Sky: new Color("#3fa9f0"), Accord: new Color("#2fbf5a"),
        Lilac: new Color("#b7a6f2"), Caution: new Color("#f0b43c"), Alert: new Color("#e8352b"), Text: new Color("#eef3ff"));

    private const string Antonio = "res://assets/fonts/Antonio-VariableFont_wght.ttf";

    /// <summary>The Concord's style: black ink on its lit keys, Antonio as written, its frame's own margins.</summary>
    public SkinStyle Style { get; }
    /// <summary>A drawn key is all face.</summary>
    public FleetCommand.Data.Rect KeyGlass { get; } = new(0, 0, 1, 1);

    private readonly HashSet<string> _supplies = new();
    public IReadOnlyCollection<string> Supplies => _supplies;

    // The pieces' designs (docs/mockups/lib/concord.js concordKit and ccCells): size, margins (top, right, bottom,
    // left), inset and gap.
    private static readonly Dictionary<string, PieceSpec> Specs = new()
    {
        ["frame.panel"] = new(new Vector2(160, 120), new Pad(22, 28, 22, 44), Vector2.Zero, 0),
        ["frame.inset"] = new(new Vector2(64, 48), new Pad(14, 14, 14, 14), Vector2.Zero, 0),
        ["bar.track"] = new(new Vector2(96, 16), new Pad(0, 10, 0, 10), new Vector2(3, 3), 0),
        ["bar.fill"] = new(new Vector2(64, 10), new Pad(0, 6, 0, 6), Vector2.Zero, 0),
        ["bar.fill.red"] = new(new Vector2(64, 10), new Pad(0, 6, 0, 6), Vector2.Zero, 0),
        ["bar.fill.accord"] = new(new Vector2(64, 10), new Pad(0, 6, 0, 6), Vector2.Zero, 0),
        ["button.idle"] = new(new Vector2(120, 48), new Pad(22, 24, 22, 24), Vector2.Zero, 0),
        ["button.hover"] = new(new Vector2(120, 48), new Pad(22, 24, 22, 24), Vector2.Zero, 0),
        ["button.pressed"] = new(new Vector2(120, 48), new Pad(22, 24, 22, 24), Vector2.Zero, 0),
        ["tab.idle"] = new(new Vector2(120, 40), new Pad(18, 6, 18, 20), Vector2.Zero, 0),
        ["tab.active"] = new(new Vector2(120, 40), new Pad(18, 6, 18, 20), Vector2.Zero, 0),
        ["strip.top"] = new(new Vector2(512, 72), new Pad(10, 96, 16, 96), Vector2.Zero, 0),
        ["strip.bottom"] = new(new Vector2(512, 72), new Pad(16, 96, 10, 96), Vector2.Zero, 0),
        ["divider"] = new(new Vector2(128, 10), new Pad(0, 18, 0, 18), Vector2.Zero, 0),
        ["cell.idle"] = new(new Vector2(32, 28), new Pad(10, 10, 10, 10), Vector2.Zero, 0),
        ["cell.lit"] = new(new Vector2(32, 28), new Pad(10, 10, 10, 10), Vector2.Zero, 0),
        ["cell.warn"] = new(new Vector2(32, 28), new Pad(10, 10, 10, 10), Vector2.Zero, 0),
        ["cell.accord"] = new(new Vector2(32, 28), new Pad(10, 10, 10, 10), Vector2.Zero, 0),
        ["cell.locked"] = new(new Vector2(32, 28), new Pad(10, 10, 10, 10), Vector2.Zero, 0),
    };

    private readonly Dictionary<string, Action<CanvasItem, Rect2, float>> _pieces = new();
    private readonly Dictionary<string, Action<CanvasItem, Rect2, float>> _heroes = new();
    private readonly Dictionary<string, Action<Pen>> _icons = new();

    public ConcordKit()
    {
        Palette c = C;
        Color black = Colors.Black;
        Style = new SkinStyle
        {
            R = new Roles(Ink: black, InkDis: new Color("#2a3a7a"), Night: new Color("#0d1430"), Ghost: new Color("#3a4a80"),
                Mute: new Color("#3a4152"), Plate: black, Title: c.Sky, Sheet: new Color("#070b1c")),
            KeySound = "ui_chirp", // the Concord's soft chirp (console-skins design, the Sound row: "Soft chirps")
            Type = new TypeSet(Antonio, Antonio, (size, weight) => new Face(false, size, weight)),
            InkOn = _ => black,
            KeyInk = new KeyInk(black, black, black, black, c.Text),
            TabIcon = c.Sky, GlyphOnDark = c.Text,
            Mark = new Marks(Colors.White, Colors.White, Colors.White),
            Pad = Specs["frame.panel"].M,
            Plate = new PlateText(108, 36, 250, new Face(false, 28, 700), c.Text),
        };
        // The sweep: a thick bar on the left joined by outer-rounded elbows to thin arms along the top and bottom,
        // which end in a small rounded cap of another colour. Thick to thin at every turn (the LCARS rule).
        _pieces["frame.panel"] = (ci, r, a) => Sweep(ci, r, 28, 10, 24, 10, 16, A(c.Signal, a), A(c.Sky, a));
        _pieces["frame.inset"] = (ci, r, a) =>
        {
            Shapes.RoundRectLine(ci, new Rect2(r.Position + new Vector2(1.5f, 1.5f), r.Size - new Vector2(3, 3)), A(c.Deep, a), 12, 3);
            Shapes.RoundRect(ci, new Rect2(r.Position + new Vector2(8, 0), new Vector2(14, 3)), A(c.Sky, a), 1.5f);
        };
        _pieces["bar.track"] = (ci, r, a) =>
        {
            Shapes.RoundRect(ci, r, A(c.Deep, a), 8);
            Shapes.RoundRect(ci, r.Grow(-2), A(Colors.Black, a), 6);
        };
        _pieces["bar.fill"] = (ci, r, a) => Shapes.RoundRect(ci, r, A(c.Sky, a), 5);
        _pieces["bar.fill.red"] = (ci, r, a) => Shapes.RoundRect(ci, r, A(c.Alert, a), 5);
        _pieces["bar.fill.accord"] = (ci, r, a) => Shapes.RoundRect(ci, r, A(c.Accord, a), 5);
        // The pill: round caps both ends (LCARS's button).
        _pieces["button.idle"] = (ci, r, a) => Shapes.RoundRect(ci, r, A(c.Signal, a), r.Size.Y / 2);
        _pieces["button.hover"] = (ci, r, a) => Shapes.RoundRect(ci, r, A(c.Sky, a), r.Size.Y / 2);
        _pieces["button.pressed"] = (ci, r, a) => Shapes.RoundRect(ci, r, A(c.Lilac, a), r.Size.Y / 2);
        // A tab is a button that grew a flat side: one cap on the left, square on the right.
        _pieces["tab.idle"] = (ci, r, a) => Shapes.RoundRect(ci, r, A(c.Deep, a), 20, 2, 2, 20);
        _pieces["tab.active"] = (ci, r, a) => Shapes.RoundRect(ci, r, A(c.Accord, a), 20, 2, 2, 20);
        _pieces["strip.top"] = (ci, r, a) => Strip(ci, r, false, a);
        _pieces["strip.bottom"] = (ci, r, a) => Strip(ci, r, true, a);
        _pieces["divider"] = (ci, r, a) =>
        {
            Vector2 p = r.Position;
            Shapes.RoundRect(ci, new Rect2(p + new Vector2(16, 3), new Vector2(r.Size.X - 32, 4)), A(c.Deep, a), 2);
            Shapes.RoundRect(ci, new Rect2(p + new Vector2(0, 2), new Vector2(12, 6)), A(c.Signal, a), 3);
            Shapes.RoundRect(ci, new Rect2(p + new Vector2(r.Size.X - 12, 2), new Vector2(12, 6)), A(c.Signal, a), 3);
        };
        // The allocation cell (proposed kit piece, battle-console design): a small flat pill, lit from behind.
        _pieces["cell.idle"] = (ci, r, a) => Shapes.RoundRect(ci, r, A(c.Deep, a), Math.Min(8, r.Size.Y / 3));
        _pieces["cell.lit"] = (ci, r, a) => Shapes.RoundRect(ci, r, A(c.Sky, a), Math.Min(8, r.Size.Y / 3));
        _pieces["cell.warn"] = (ci, r, a) => Shapes.RoundRect(ci, r, A(c.Alert, a), Math.Min(8, r.Size.Y / 3));
        _pieces["cell.accord"] = (ci, r, a) => Shapes.RoundRect(ci, r, A(c.Accord, a), Math.Min(8, r.Size.Y / 3));
        _pieces["cell.locked"] = (ci, r, a) => Shapes.RoundRectLine(ci, r.Grow(-1), A(c.Deep, a), Math.Min(7, r.Size.Y / 3), 2);

        _heroes["nameplate"] = (ci, r, a) =>
        {
            Vector2 p = r.Position; float w = r.Size.X, h = r.Size.Y;
            Shapes.RoundRect(ci, new Rect2(p, new Vector2(96, h)), A(c.Signal, a), 28, 4, 4, 28);
            Shapes.RoundRect(ci, new Rect2(p + new Vector2(102, h - 10), new Vector2(w - 140, 8)), A(c.Deep, a), 4);
            Shapes.RoundRect(ci, new Rect2(p + new Vector2(w - 32, h - 10), new Vector2(32, 8)), A(c.Sky, a), 4);
        };
        // Helm: a saucer. Eight arc keys round a round stop key. Drawn at 300 units.
        _heroes["helm.pad"] = (ci, r, a) =>
        {
            float k = r.Size.X / 300f; Vector2 cc = r.Position + r.Size / 2;
            for (int i = 0; i < 8; i++)
            {
                float a0 = -Mathf.Pi / 2 + (i - 0.5f) * Mathf.Pi / 4;
                Pen.FillAbs(ci, Shapes.ArcSeg(cc, 58 * k, 128 * k, a0 + 0.06f, a0 + Mathf.Pi / 4 - 0.06f), A(i % 2 == 1 ? c.Deep : c.Signal, a));
            }
            Draw2D.Circle(ci, cc, 46 * k, A(c.Signal, a), true, -1, true);
            Draw2D.Arc(ci, cc, 140 * k, 0, Mathf.Tau, 96, A(c.Deep, a), 3 * k, true);
            foreach (float ang in new[] { 0f, Mathf.Pi / 2, Mathf.Pi, 1.5f * Mathf.Pi })
            {
                Draw2D.SetTransform(ci, cc, ang, Vector2.One);
                Draw2D.Rect(ci, new Rect2(136 * k, -6 * k, 10 * k, 12 * k), A(Colors.Black, a));
            }
            Draw2D.SetTransform(ci, Vector2.Zero, 0, Vector2.One);
        };
        _heroes["dial"] = (ci, r, a) =>
        {
            float k = r.Size.X / 120f; Vector2 cc = r.Position + r.Size / 2;
            Draw2D.Arc(ci, cc, 54 * k, 0, Mathf.Tau, 72, A(c.Deep, a), 6 * k, true);
            Draw2D.Arc(ci, cc, 36 * k, 0, Mathf.Tau, 64, A(c.Deep, a), 2 * k, true);
            Draw2D.Arc(ci, cc, 18 * k, 0, Mathf.Tau, 48, A(c.Deep, a), 2 * k, true);
            for (int i = 0; i < 4; i++) Draw2D.Arc(ci, cc, 54 * k, i * Mathf.Pi / 2 - 0.12f, i * Mathf.Pi / 2 + 0.12f, 8, A(c.Signal, a), 6 * k, true);
        };
        _heroes["lamp.alert.green"] = (ci, r, a) => Lamp(ci, r, c.Accord, a);
        _heroes["lamp.alert.yellow"] = (ci, r, a) => Lamp(ci, r, c.Caution, a);
        _heroes["lamp.alert.red"] = (ci, r, a) => Lamp(ci, r, c.Alert, a);
        // The Concord mark (original): five worlds round an open circle, crossed by one orbit. Drawn at 96 units.
        _heroes["crest"] = (ci, r, a) =>
        {
            var p = new Pen(ci, r.Position, r.Size.X / 96f, A(c.Sky, a)) { Width = 4 };
            p.Circle(48, 48, 44);
            p.Col = A(c.Signal, a);
            for (int i = 0; i < 5; i++) { float an = -Mathf.Pi / 2 + i * Mathf.Tau / 5; p.Dot(48 + Mathf.Cos(an) * 27, 48 + Mathf.Sin(an) * 27, 7); }
            p.Col = A(c.Lilac, a); p.Width = 3; p.Circle(48, 48, 11);
            p.Col = A(c.Sky, a); p.Width = 2; p.Ellipse(48, 48, 40, 13, -0.5f);
        };

        DefineIcons();
        foreach (string k in _pieces.Keys) _supplies.Add(k);
        foreach (string k in _heroes.Keys) _supplies.Add(k);
        foreach (string k in _icons.Keys) _supplies.Add("icon." + k);
    }

    public void Piece(CanvasItem c, string piece, Rect2 r, float alpha = 1)
    {
        if (!_pieces.TryGetValue(piece, out var d)) throw new KeyNotFoundException($"Concord kit has no piece '{piece}'");
        d(c, r, alpha);
    }

    public void Hero(CanvasItem c, string hero, Rect2 r, float alpha = 1)
    {
        if (!_heroes.TryGetValue(hero, out var d)) throw new KeyNotFoundException($"Concord kit has no hero piece '{hero}'");
        d(c, r, alpha);
    }

    /// <summary>The Concord has no rendered keys (its style says so), so nothing asks it for part of one.</summary>
    public void HeroBottom(CanvasItem c, string hero, Rect2 r, float frac) =>
        throw new System.NotSupportedException($"the Concord kit has no rendered keys ('{hero}')");

    public void Icon(CanvasItem c, string icon, Rect2 r, Color ink)
    {
        if (!_icons.TryGetValue(icon, out var d)) throw new KeyNotFoundException($"Concord kit has no icon '{icon}'");
        IconAtlas.Draw(Name, c, icon, r, ink, d);   // drawn once into the shared atlas, then as one textured rectangle
    }

    public PieceSpec Spec(string piece) =>
        Specs.TryGetValue(piece, out PieceSpec s) ? s : throw new KeyNotFoundException($"Concord kit has no piece '{piece}'");

    /// <summary>The Concord's keys are rounded rectangles.</summary>
    public void Shape(CanvasItem c, Rect2 r, float radius, Color col, float line = 0)
    {
        if (line > 0) Shapes.RoundRectLine(c, r, col, radius, line);
        else Shapes.RoundRect(c, r, col, radius);
    }

    /// <summary>The Concord's knobs are circles.</summary>
    public void Knob(CanvasItem c, Vector2 centre, float r, Color col) => Draw2D.Circle(c, centre, r, col, true, -1, true);

    private static Color A(Color c, float a) => new(c.R, c.G, c.B, c.A * a);

    private static void Sweep(CanvasItem ci, Rect2 r, float bar, float arm, float R, float rr, float cap, Color col, Color capCol)
    {
        float X = r.Position.X, Y = r.Position.Y, W = r.Size.X, H = r.Size.Y, end = X + W - cap - 5;
        var pb = new PathBuilder();
        pb.To(X + R, Y).To(end, Y).To(end, Y + arm).To(X + bar + rr, Y + arm)
          .Corner(new Vector2(X + bar, Y + arm), new Vector2(-1, 0), new Vector2(0, 1), rr)
          .To(X + bar, Y + H - arm - rr)
          .Corner(new Vector2(X + bar, Y + H - arm), new Vector2(0, 1), new Vector2(1, 0), rr)
          .To(end, Y + H - arm).To(end, Y + H).To(X + R, Y + H)
          .Corner(new Vector2(X, Y + H), new Vector2(-1, 0), new Vector2(0, -1), R)
          .To(X, Y + R)
          .Corner(new Vector2(X, Y), new Vector2(0, -1), new Vector2(1, 0), R);
        Pen.FillAbs(ci, pb.Pts.ToArray(), col);
        if (cap > 0)
        {
            Shapes.RoundRect(ci, new Rect2(end + 5, Y, cap, arm), capCol, arm / 2);
            Shapes.RoundRect(ci, new Rect2(end + 5, Y + H - arm, cap, arm), capCol, arm / 2);
        }
    }

    // A tall block at the left, rounded on its outer side, sweeping into a thin bar along one edge with a cap at the end.
    private void Strip(CanvasItem ci, Rect2 r, bool flip, float a)
    {
        float w = r.Size.X, h = r.Size.Y, by = h - 12, bt = 8, R = Math.Min(28, (by + bt) / 2);
        var pb = new PathBuilder();
        pb.To(R, 0).To(72, 0).To(72, by - 10)
          .Corner(new Vector2(72, by), new Vector2(0, 1), new Vector2(1, 0), 10)
          .To(w - 44, by).To(w - 44, by + bt).To(R, by + bt)
          .Corner(new Vector2(0, by + bt), new Vector2(-1, 0), new Vector2(0, -1), R)
          .To(0, R)
          .Corner(new Vector2(0, 0), new Vector2(0, -1), new Vector2(1, 0), R);
        Vector2 Map(Vector2 v) => r.Position + new Vector2(v.X, flip ? h - v.Y : v.Y);
        var pts = pb.Pts.ConvertAll(Map).ToArray();
        if (flip) Array.Reverse(pts);
        Pen.FillAbs(ci, pts, A(C.Signal, a));
        float capY = flip ? h - by - bt : by;
        Shapes.RoundRect(ci, new Rect2(r.Position + new Vector2(w - 39, capY), new Vector2(22, bt)), A(C.Sky, a), 4);
        Shapes.RoundRect(ci, new Rect2(r.Position + new Vector2(w - 13, capY), new Vector2(13, bt)), A(C.Deep, a), 4);
    }

    private void Lamp(CanvasItem ci, Rect2 r, Color col, float a)
    {
        float k = r.Size.X / 64f; Vector2 cc = r.Position + r.Size / 2;
        Draw2D.Circle(ci, cc, 30 * k, A(C.Deep, a), true, -1, true);
        Draw2D.Circle(ci, cc, 25 * k, A(Colors.Black, a), true, -1, true);
        Draw2D.Circle(ci, cc, 21 * k, A(col, a), true, -1, true);
    }

    // Concord icons, 48 units square (docs/mockups/lib/concord.js ccIcon and the icon guide, docs/design/icon-guide.html):
    // pictures of their jobs, solid for the thing and lines for what it gives off.
    private void DefineIcons()
    {
        const float P = Mathf.Pi;
        _icons["helm"] = p =>
        {
            p.Circle(24, 24, 12); p.Dot(24, 24, 4);
            for (int i = 0; i < 8; i++) { float a = i * P / 4, c = Mathf.Cos(a), s = Mathf.Sin(a); p.Line(24 + c * 4, 24 + s * 4, 24 + c * 19, 24 + s * 19); p.Dot(24 + c * 20, 24 + s * 20, 2.6f); }
        };
        _icons["fire"] = p => { p.Circle(24, 24, 18); p.Circle(24, 24, 10); p.Dot(24, 24, 3.5f); };
        _icons["shields"] = p =>
        {
            p.Width = 4.5f;
            for (int i = 0; i < 6; i++) { float a = -P / 2 + i * P / 3; p.Arc(24, 24, 18, a - P / 6 + 0.16f, a + P / 6 - 0.16f); }
            p.Width = 3; p.Dot(24, 24, 5);
        };
        _icons["tractor"] = p =>
        {
            p.Rect(3, 19, 9, 10); p.Line(12, 21, 32, 10); p.Line(12, 27, 32, 38);
            p.Width = 2; p.Arc(10, 24, 12, -0.45f, 0.45f); p.Arc(10, 24, 18, -0.42f, 0.42f);
            p.Width = 3; p.Stroke(new[] { new Vector2(34, 15), new Vector2(45, 15), new Vector2(45, 33), new Vector2(34, 33) });
        };
        _icons["transport"] = p =>
        {
            // a figure solid above the waist, dissolving into thinning horizontal slices below it
            p.Dot(24, 9, 5.5f);
            p.Fill(new PathBuilder().To(14, 26).To(15, 21).Quad(24, 15, 33, 21).To(34, 26).Pts);
            float yy = 26; int k = 0;
            while (yy < 46)
            {
                float gap = 1 + k * 0.32f, top = yy + gap, bot = Math.Min(yy + 3.4f, 46);
                if (bot > top)
                {
                    if (top < 36) p.Rect(14, top, 20, Math.Min(bot, 36) - top);
                    if (bot > 35) { float t2 = Math.Max(top, 35), b2 = Math.Min(bot, 44); if (b2 > t2) { p.Rect(16, t2, 6, b2 - t2); p.Rect(26, t2, 6, b2 - t2); } }
                }
                yy += 3.4f; k++;
            }
            foreach (var (sx, sy) in new[] { (9f, 30f), (39f, 33f), (10f, 40f), (38f, 42f) }) p.Rect(sx - 3, sy - 0.9f, 6, 1.8f);
        };
        _icons["science"] = p => { foreach (float r in new[] { 0, P / 3, -P / 3 }) p.Ellipse(24, 24, 20, 7.5f, r); p.Dot(24, 24, 4); };
        _icons["comms"] = p =>
        {
            var dish = new PathBuilder().ArcPts(17, 27, 12, P * 0.75f, P * 1.75f - Mathf.Tau, 16);
            p.Stroke(dish.Pts);
            p.Line(17, 27, 12, 44); p.Line(17, 27, 22, 44); p.Line(9, 44, 25, 44);
            p.Dot(26, 18, 2.5f); p.Arc(26, 18, 8, -P * 0.5f, 0); p.Arc(26, 18, 15, -P * 0.5f, 0);
        };
        _icons["repair"] = p => { p.Width = 6; p.Line(11, 37, 26, 22); p.Width = 5; p.Arc(31, 17, 9, -P / 4 + 0.95f, -P / 4 - 0.95f + Mathf.Tau); p.Width = 3; };
        _icons["map"] = p => { p.Circle(12, 14, 6); p.Circle(36, 20, 6); p.Circle(20, 36, 6); p.Width = 2; p.Line(18, 15, 30, 19); p.Line(33, 25, 24, 32); p.Line(17, 30, 13, 20); };
        _icons["power"] = p => { p.Circle(24, 24, 19); p.Fill(new PathBuilder().To(27, 7).To(15, 26).To(23, 26).To(20, 41).To(33, 20).To(25, 20).Pts); };
        _icons["tactical"] = p =>
        {
            p.Rect(7, 39, 34, 5); p.RoundRect(12, 27, 24, 13, 7, 7, 2, 2);
            p.Width = 3.5f; p.Line(25, 31, 42, 14); p.Line(21, 29, 38, 12); p.Rect(19, 22, 10, 6); p.Width = 3;
        };
        _icons["pd"] = p =>
        {
            p.Stroke(new PathBuilder().To(24, 5).To(40, 11).Quad(40, 33, 24, 43).Quad(8, 33, 8, 11).Pts);
            var star = new PathBuilder();
            for (int i = 0; i < 12; i++) { float a = -P / 2 + i * P / 6, r = i % 2 == 1 ? 4 : 9; star.To(24 + Mathf.Cos(a) * r, 23 + Mathf.Sin(a) * r); }
            p.Fill(star.Pts);
        };
        _icons["fleet"] = p =>
        {
            void Chev(float cx, float cy, float k) => p.Fill(new PathBuilder().To(cx, cy - 8 * k).To(cx + 8 * k, cy + 6 * k).To(cx, cy + 2 * k).To(cx - 8 * k, cy + 6 * k).Pts);
            Chev(24, 13, 1.35f); Chev(11, 33, 1.15f); Chev(37, 33, 1.15f);
        };
        _icons["ship"] = p =>
        {
            p.Dot(24, 14, 10); p.Rect(22, 22, 4, 10); p.RoundRect(17, 28, 14, 8, 4); p.RoundRect(6, 24, 6, 20, 3); p.RoundRect(36, 24, 6, 20, 3);
            p.Width = 2.5f; p.Line(17, 32, 12, 30); p.Line(31, 32, 36, 30); p.Width = 3;
        };
        _icons["sensors"] = p =>
        {
            p.Circle(24, 24, 19); p.Width = 2; p.Circle(24, 24, 10); p.Width = 3;
            p.Fill(new PathBuilder().To(24, 24).ArcPts(24, 24, 19, -P / 2, -P / 2 + 1.0f, 10).Pts);
            p.Dot(34, 31, 2.6f); p.Dot(15, 33, 2.2f);
        };
        _icons["cargo"] = p => { foreach (var (cx, cy) in new[] { (15f, 15f), (33f, 15f), (15f, 33f), (33f, 33f) }) p.Circle(cx, cy, 7); };
        _icons["craft"] = p =>
        {
            p.RoundRect(21, 5, 6, 34, 3);
            p.Fill(new PathBuilder().To(21, 18).To(5, 33).To(5, 37).To(21, 30).Pts);
            p.Fill(new PathBuilder().To(27, 18).To(43, 33).To(43, 37).To(27, 30).Pts);
            p.Fill(new PathBuilder().To(21, 36).To(15, 44).To(21, 42).Pts);
            p.Fill(new PathBuilder().To(27, 36).To(33, 44).To(27, 42).Pts);
        };
        _icons["weapons"] = p =>
        {
            p.Dot(11, 36, 6); p.Width = 4; p.Line(15, 32, 33, 14); p.Width = 3;
            var burst = new PathBuilder();
            for (int i = 0; i < 12; i++) { float a = i * P / 6, r = i % 2 == 1 ? 3.5f : 8; burst.To(36 + Mathf.Cos(a) * r, 11 + Mathf.Sin(a) * r); }
            p.Fill(burst.Pts);
        };
        _icons["warp"] = p => { p.Circle(33, 24, 8); p.Dot(33, 24, 2.5f); p.Line(5, 15, 21, 15); p.Line(2, 24, 21, 24); p.Line(5, 33, 21, 33); };
        _icons["engines"] = p =>
        {
            p.Rect(19, 4, 10, 9); p.Fill(new PathBuilder().To(18, 13).To(30, 13).To(37, 28).To(11, 28).Pts);
            p.Stroke(new PathBuilder().To(13, 31).Quad(15, 41, 24, 46).Quad(33, 41, 35, 31).Pts, false);
            p.Fill(new PathBuilder().To(19, 31).Quad(20, 37, 24, 40).Quad(28, 37, 29, 31).Pts);
        };
        _icons["reactor"] = p =>
        {
            p.Dot(24, 24, 3.5f);
            for (int i = 0; i < 3; i++)
            {
                float a = -P / 2 + i * Mathf.Tau / 3;
                var blade = new PathBuilder().ArcPts(24, 24, 18, a - 0.52f, a + 0.52f, 10).ArcPts(24, 24, 7, a + 0.52f, a - 0.52f, 6);
                p.Fill(blade.Pts);
            }
        };
        _icons["pause"] = p => { p.RoundRect(13, 10, 8, 28, 4); p.RoundRect(27, 10, 8, 28, 4); };
        _icons["menu"] = p => { foreach (float y in new[] { 12f, 22f, 32f }) p.RoundRect(10, y, 28, 6, 3); };
    }
}
