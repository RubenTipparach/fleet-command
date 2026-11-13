// KraiKit: the Krai Warhost skin (console-skins design, "Krai: cast volcanic gunmetal"; openspec/changes/krai-battle-console):
// every piece rendered in Blender by tools/blender/build_ui_krai.py (assets/ui/krai/manifest.json), the joined column,
// placards, joined strips, symbol bars and glass keys included; its icons in the Krai's own language (blades, wedges,
// triangles and facets, nothing round); its colour roles, ink, type (Teko and Russo One) and blade-cut drawn keys. A
// port of docs/mockups/lib/krai.js (mockup 8), field for field and shape for shape, so the game draws what the owner
// reviewed.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Game.Ui;
using Godot;
using static FleetCommand.Game.Ui.Kit.IconGeom;

namespace FleetCommand.Game.Ui.Kit;

public sealed class KraiKit : IKit
{
    private const string Teko = "res://assets/fonts/Teko-VariableFont_wght.ttf";
    private const string RussoOne = "res://assets/fonts/RussoOne-Regular.ttf";
    private const float P = Mathf.Pi;

    public string Name => "Krai Warhost";

    // The battle console's colour roles (krai.js CC): blood keys at rest, the forge orange for readouts and hover, ash
    // labels, bone text; the state colours from the Krai palette (whole in forge orange, damaged in gold, down in red).
    public Palette C { get; } = new(
        Deep: new Color("#3b1714"), Signal: new Color("#a3161a"), Sky: new Color("#ff8a2a"), Accord: new Color("#ff7a1a"),
        Lilac: new Color("#b8a097"), Caution: new Color("#ffc23a"), Alert: new Color("#ff2a2a"), Text: new Color("#efe2d0"));

    public SkinStyle Style { get; }

    private readonly RenderedPieces _r = new("krai");
    /// <summary>Measured from the kit's own key renders (RenderedPieces.Glass).</summary>
    public FleetCommand.Data.Rect KeyGlass => _glass ??= _r.Glass("key.idle", "key.on");
    private FleetCommand.Data.Rect? _glass;
    private readonly HashSet<string> _supplies = new();
    public IReadOnlyCollection<string> Supplies => _supplies;
    private readonly Dictionary<string, Action<Pen>> _icons = new();

    public KraiKit()
    {
        Palette c = C;
        // the ink on a key: forge orange on the dark ember (krai.js GLYPH), never white
        var glyph = new Color("#ff8a2a");
        var ink = new Color("#160806");
        var red = new Color("#ff2a2a");
        Style = new SkinStyle
        {
            R = new Roles(Ink: ink, InkDis: new Color("#6a3a32"), Night: new Color("#1a0b09"), Ghost: new Color("#6a4038"),
                Mute: new Color("#2e2220"), Plate: new Color("#0a0707"), Title: red, Sheet: new Color("#0a0707")),
            // "Russo One" for titles and the name plate, Teko for everything else. Teko 500 at 1.1x is as wide as Antonio
            // 600, so every slot that fits a Concord label fits a Krai one; weight 700 at 22 px and up is a heading.
            KeySound = "ui_clunk", // the modelled metal skins' clunk (sound-effects design section 2)
            Type = new TypeSet(Teko, RussoOne, (size, weight) =>
                weight >= 700 && size >= 22 ? new Face(true, (int)MathF.Round(size * 0.75f), 400)
                : weight >= 700 ? new Face(false, (int)MathF.Round(size * 1.05f), 600)
                : new Face(false, (int)MathF.Round(size * 1.1f), 500)),
            // dark on the lit colours, forge orange on blood and steel (krai.js ink)
            InkOn = fill => KitInk.On(fill, ink, glyph),
            KeyInk = new KeyInk(Idle: glyph, Hover: ink, Pressed: ink, TabOn: ink, TabOff: new Color("#ff4a3a")),
            TabIcon = red, GlyphOnDark = glyph,
            Mark = new Marks(Hover: new Color("#ffb070"), Ready: red, Count: new Color("#ffd9b0")),
            // clear of the honed rim and the corner fangs
            Pad = new Pad(26, 20, 20, 22),
            Plate = new PlateText(92, 35, 270, new Face(true, 19, 400), new Color("#e8dccb")),
            KeyCenter = true,
            // the joined strips are drawn 60 and 80 px tall in the mockup; at the console's 56 px strips their content
            // moves down 6 px in the top strip's channel and up 5 px in the bottom one's
            StripTopDy = 8, StripBottomDy = 12, StripTopH = 60, StripBottomH = 80, // krai.js stripTop, stripBottom
            Column = true, LabelPlacard = true, TitlePlacard = true, JoinedStrips = true, SymbolBars = true, RenderedKeys = true,
            // its backing plate behind an expanded panel (the mockup's consoleBacking; no black behind metal, CLAUDE.md section 7)
            Backing = true,
        };
        // the ok symbol: the lit chevron's light turned to the state colour (krai.js load)
        _r.AddHueTurned("bar.cell.ok", "bar.cell.on", c.Accord);
        DefineIcons();
        foreach (string k in _r.Names) _supplies.Add(k);
        foreach (string k in _icons.Keys) _supplies.Add("icon." + k);
    }

    public void Piece(CanvasItem c, string piece, Rect2 r, float alpha = 1) => _r.Draw(c, piece, r, alpha);

    public void Hero(CanvasItem c, string hero, Rect2 r, float alpha = 1) => _r.Draw(c, hero, r, alpha);

    public void HeroBottom(CanvasItem c, string hero, Rect2 r, float frac) => _r.DrawBottom(c, hero, r, frac);

    public void HeroPart(CanvasItem c, string hero, Rect2 dest, Rect2 src, bool mirror = false) => _r.DrawPart(c, hero, dest, src, mirror);

    public void Icon(CanvasItem c, string icon, Rect2 r, Color ink)
    {
        if (!_icons.TryGetValue(icon, out var d)) throw new KeyNotFoundException($"Krai kit has no icon '{icon}'");
        IconAtlas.Draw(Name, c, icon, r, ink, d);   // drawn once into the shared atlas, then as one textured rectangle
    }

    public PieceSpec Spec(string piece) => _r.Spec(piece);

    /// <summary>The Krai cut their drawn keys as blades (nothing round survives the forge): the top-left and bottom-right
    /// corners cut deep, the other two nicked, the cut as big as the rounding asked for (krai.js rr).</summary>
    public void Shape(CanvasItem ci, Rect2 r, float radius, Color col, float line = 0)
    {
        float X = r.Position.X, Y = r.Position.Y, W = r.Size.X, H = r.Size.Y;
        if (W <= 0 || H <= 0) return;
        float cut = Math.Max(0, Math.Min(radius * 0.8f, Math.Min(W / 2, H / 2))), s = cut * 0.3f;
        Vector2[] pts =
        {
            new(X + cut, Y), new(X + W - s, Y), new(X + W, Y + s), new(X + W, Y + H - cut),
            new(X + W - cut, Y + H), new(X + s, Y + H), new(X, Y + H - s), new(X, Y + cut),
        };
        if (line > 0) Draw2D.Polyline(ci, pts.Append(pts[0]).ToArray(), col, line, true);
        else Pen.FillAbs(ci, pts, col);
    }

    /// <summary>Round knobs become hexagons (krai.js knob).</summary>
    public void Knob(CanvasItem ci, Vector2 centre, float r, Color col) =>
        Pen.FillAbs(ci, Enumerable.Range(0, 6).Select(i => centre + new Vector2(Mathf.Cos(i * P / 3), Mathf.Sin(i * P / 3)) * r * 1.08f).ToArray(), col);

    // ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ------------------------------------
    // krai.js icon(), on a 48 x 48 grid: solid for the thing, thinner lines for what it gives off, one ink.

    private static Vector2[] Ngon(float cx, float cy, float r, int n, float a0 = -P / 2) =>
        Enumerable.Range(0, n).Select(i => new Vector2(cx + Mathf.Cos(a0 + i * 2 * P / n) * r, cy + Mathf.Sin(a0 + i * 2 * P / n) * r)).ToArray();

    private static Vector2[] Star(float cx, float cy, float r0, float r1, int n, float a0 = -P / 2) =>
        Enumerable.Range(0, 2 * n).Select(i => { float r = i % 2 == 1 ? r1 : r0, a = a0 + i * P / n; return new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r); }).ToArray();

    private static Vector2[] Rot(Vector2[] pts, float cx, float cy, float a) =>
        pts.Select(p => new Vector2(cx + p.X * Mathf.Cos(a) - p.Y * Mathf.Sin(a), cy + p.X * Mathf.Sin(a) + p.Y * Mathf.Cos(a))).ToArray();

    private static Vector2[] Box(float x, float y, float w, float h) => Pts(x, y, x + w, y, x + w, y + h, x, y + h);

    /// <summary>The shapes left once a cut is taken out of them (krai.js cut, a destination-out fill). A cut wholly
    /// inside a shape would leave a hole, which a fill cannot show, so that shape is first parted in four round the
    /// cut's centre, and each quarter is cut on its own.</summary>
    private static List<Vector2[]> Minus(IEnumerable<Vector2[]> shapes, Vector2[] cut)
    {
        var outp = new List<Vector2[]>();
        Vector2 c = cut.Aggregate(Vector2.Zero, (a, b) => a + b) / cut.Length;
        foreach (Vector2[] s in shapes)
        {
            var parts = Geometry2D.ClipPolygons(s, cut).ToList();
            bool hole = parts.Any(p => parts.Any(o => !ReferenceEquals(o, p) && Geometry2D.IsPointInPolygon(p[0], o)));
            if (!hole) { outp.AddRange(parts); continue; }
            foreach (var (dx, dy) in new[] { (1, 1), (-1, 1), (-1, -1), (1, -1) })
                foreach (Vector2[] q in Geometry2D.IntersectPolygons(s, Pts(c.X, c.Y, c.X + 100 * dx, c.Y, c.X + 100 * dx, c.Y + 100 * dy, c.X, c.Y + 100 * dy)))
                    outp.AddRange(Geometry2D.ClipPolygons(q, cut));
        }
        return outp;
    }

    private void DefineIcons()
    {
        static void S(Pen p, Vector2[] pts, bool closed = true, float lw = 3) { p.Width = lw; p.Stroke(pts, closed); p.Width = 3; }
        static void L(Pen p, float x0, float y0, float x1, float y1, float lw = 3) { p.Width = lw; p.Line(x0, y0, x1, y1); p.Width = 3; }

        // a war wheel: an eight-sided rim, eight spokes ending in spikes, a diamond hub
        _icons["helm"] = p =>
        {
            S(p, Ngon(24, 24, 13, 8, -P / 2 + P / 8), true, 3.5f);
            for (int i = 0; i < 8; i++)
            {
                float a = -P / 2 + i * P / 4, c = Mathf.Cos(a), s = Mathf.Sin(a);
                L(p, 24 + c * 4, 24 + s * 4, 24 + c * 17, 24 + s * 17, 2.6f);
                p.Fill(Pts(24 + c * 22, 24 + s * 22, 24 + c * 16 - s * 3.2f, 24 + s * 16 + c * 3.2f, 24 + c * 16 + s * 3.2f, 24 + s * 16 - c * 3.2f));
            }
            p.Fill(Ngon(24, 24, 5.5f, 4));
        };
        // a jagged bolt in a hexagon
        _icons["power"] = p => { S(p, Ngon(24, 24, 20, 6), true, 2.6f); p.Fill(Pts(28, 6, 15, 26, 23, 26, 19, 42, 33, 20, 25, 20)); };
        // a turret: a stepped base, a wedge housing, twin blade barrels
        _icons["tactical"] = p =>
        {
            p.Fill(Pts(5, 44, 43, 44, 39, 38, 9, 38)); p.Fill(Pts(11, 37, 37, 37, 33, 27, 24, 23, 15, 27));
            p.Fill(Pts(23, 30, 41, 11, 44, 13, 27, 32)); p.Fill(Pts(19, 27, 36, 8, 39, 10, 23, 30));
        };
        // point defence: a pointed shield with a burst on it
        _icons["pd"] = p => { S(p, Pts(24, 4, 41, 10, 38, 30, 24, 44, 10, 30, 7, 10)); p.Fill(Star(24, 23, 10, 4, 6)); };
        // science: three bladed orbits, a diamond nucleus
        _icons["science"] = p => { foreach (float a in new[] { 0, P / 3, -P / 3 }) S(p, Rot(Pts(-21, 0, 0, -7.5f, 21, 0, 0, 7.5f), 24, 24, a), true, 2.4f); p.Fill(Ngon(24, 24, 5, 4)); };
        // comms: a wedge dish on a stand, its feed spike, two angled waves
        _icons["comms"] = p =>
        {
            p.Fill(Pts(4, 18, 30, 42, 20, 37, 11, 30, 6, 24)); L(p, 16, 31, 26, 20, 2.6f); p.Fill(Ngon(27, 19, 3.5f, 4));
            L(p, 15, 34, 11, 45); L(p, 5, 45, 22, 45);
            S(p, Pts(30, 11, 36, 13, 38, 19), false, 2.4f); S(p, Pts(31, 3, 42, 7, 46, 18), false, 2.4f);
        };
        // repair: a wrench with a hexagonal jaw (the jaw's slot cut once, here)
        List<Vector2[]> jaw = Minus(new[] { Ngon(32, 16, 10, 6, 0) }, Rot(Pts(-4, -6, 4, -6, 4, 8, -4, 8), 37, 11, P / 4));
        _icons["repair"] = p =>
        {
            p.Fill(Pts(7, 37, 26, 18, 31, 23, 12, 42));
            foreach (Vector2[] j in jaw) p.Fill(j);
            p.Fill(Pts(7, 37, 12, 42, 4, 45));
        };
        // transport: a figure, solid above the waist, sliced into bars below it (the slices cut once, here)
        List<Vector2[]> body = new() { Pts(13, 37, 15, 21, 24, 16, 33, 21, 35, 37), Pts(15, 36, 22, 36, 21, 45, 15, 45), Pts(26, 36, 33, 36, 33, 45, 27, 45) };
        int slice = 0;
        for (float yy = 26; yy < 46; yy += 3.4f, slice++) body = Minus(body, Box(0, yy, 48, 1 + slice * 0.32f));
        _icons["transport"] = p =>
        {
            p.Fill(Ngon(24, 9, 6, 4));
            foreach (Vector2[] b in body) p.Fill(b);
            foreach (var (sx, sy) in new[] { (8f, 30f), (40f, 33f), (9f, 40f), (39f, 42f) }) p.Rect(sx - 3, sy - 0.9f, 6, 1.8f);
        };
        // fleet: three swept blades in formation
        _icons["fleet"] = p =>
        {
            void Chev(float cx, float cy, float k) => p.Fill(Pts(cx, cy - 10 * k, cx + 7.5f * k, cy + 7 * k, cx, cy + 1.5f * k, cx - 7.5f * k, cy + 7 * k));
            Chev(24, 13, 1.25f); Chev(11, 33, 1.1f); Chev(37, 33, 1.1f);
        };
        // the ship from above: a knife-blade hull with swept wings
        _icons["ship"] = p => p.Fill(Pts(24, 3, 28, 20, 44, 37, 42, 41, 29, 35, 27, 45, 21, 45, 19, 35, 6, 41, 4, 37, 20, 20));
        // shields: six armour plates round a triangle core
        _icons["shields"] = p =>
        {
            for (int i = 0; i < 6; i++)
            {
                float a = -P / 2 + i * P / 3, d = 0.4f;
                Vector2 At(float an, float r) => new(24 + Mathf.Cos(an) * r, 24 + Mathf.Sin(an) * r);
                p.Fill(new[] { At(a - d, 13), At(a - d, 21), At(a + d, 21), At(a + d, 13) });
            }
            p.Fill(Ngon(24, 25, 6, 3));
        };
        // sensors: a hexagonal scope, a wedge sweep, two triangle contacts
        _icons["sensors"] = p =>
        {
            S(p, Ngon(24, 24, 20, 6), true, 2.6f); S(p, Ngon(24, 24, 9, 6), true, 1.8f);
            p.Fill(Pts(24, 24, 24, 4, 41, 14)); p.Fill(Ngon(34, 31, 3.4f, 3)); p.Fill(Ngon(14, 33, 3, 3));
        };
        // tractor: an emitter wedge, a widening beam with chevron waves, the held hull
        _icons["tractor"] = p =>
        {
            p.Fill(Pts(2, 16, 13, 21, 13, 27, 2, 32)); L(p, 13, 21, 33, 10, 2.4f); L(p, 13, 27, 33, 38, 2.4f);
            S(p, Pts(18, 17, 22, 24, 18, 31), false, 2); S(p, Pts(25, 13, 30, 24, 25, 35), false, 2); S(p, Pts(35, 24, 46, 14, 46, 34), true, 2.6f);
        };
        // craft: a fighter, a thin blade fuselage with forward-swept wings
        _icons["craft"] = p => { p.Fill(Pts(24, 3, 27.5f, 24, 24, 45, 20.5f, 24)); p.Fill(Pts(22, 21, 4, 13, 8, 26, 22, 31)); p.Fill(Pts(26, 21, 44, 13, 40, 26, 26, 31)); };
        // weapons: an emitter's blade beam ending in a burst
        _icons["weapons"] = p => { p.Fill(Pts(3, 45, 15, 41, 7, 33)); L(p, 9, 39, 30, 18, 4); p.Fill(Star(36, 12, 10, 3.5f, 6, -P / 4)); };
        // warp: a forward chevron trailing speed lines
        _icons["warp"] = p => { p.Fill(Pts(22, 9, 42, 24, 22, 39, 29, 24)); L(p, 4, 15, 19, 15); L(p, 2, 24, 22, 24); L(p, 4, 33, 19, 33); };
        // engines: a nozzle and its flame
        _icons["engines"] = p => { p.Fill(Pts(16, 4, 32, 4, 37, 23, 11, 23)); S(p, Pts(11, 27, 37, 27, 24, 46), true, 2.4f); p.Fill(Pts(18, 27, 30, 27, 24, 38)); };
        // reactor: three blades round a core
        _icons["reactor"] = p =>
        {
            p.Fill(Ngon(24, 24, 4, 3));
            for (int i = 0; i < 3; i++)
            {
                float a = -P / 2 + i * 2 * P / 3, d = 0.55f;
                Vector2 At(float an, float r) => new(24 + Mathf.Cos(an) * r, 24 + Mathf.Sin(an) * r);
                p.Fill(new[] { At(a - d * 0.6f, 8), At(a - d, 20), At(a + d, 20), At(a + d * 0.6f, 8) });
            }
        };
        // map: three triangle waypoints joined by a course
        _icons["map"] = p =>
        {
            L(p, 13, 16, 34, 21, 2); L(p, 33, 25, 22, 34, 2); L(p, 18, 32, 13, 19, 2);
            p.Fill(Ngon(12, 14, 6.5f, 3)); p.Fill(Ngon(36, 21, 6.5f, 3)); p.Fill(Ngon(20, 36, 6.5f, 3));
        };
        _icons["pause"] = p => { p.Fill(Pts(15, 10, 23, 10, 19, 38, 11, 38)); p.Fill(Pts(29, 10, 37, 10, 33, 38, 25, 38)); };
        _icons["menu"] = p => { foreach (float y in new[] { 12f, 22f, 32f }) p.Fill(Pts(12, y, 40, y, 36, y + 6, 8, y + 6)); };
        // fire: three blades closing on a diamond
        _icons["fire"] = p =>
        {
            foreach (float a in new[] { -P / 2, P / 6, 5 * P / 6 })
            {
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                p.Fill(Pts(24 + c * 9, 24 + s * 9, 24 + c * 20 - s * 6, 24 + s * 20 + c * 6, 24 + c * 20 + s * 6, 24 + s * 20 - c * 6));
            }
            p.Fill(Ngon(24, 24, 3.5f, 4));
        };
        _icons["cargo"] = p => { foreach (var (cx, cy) in new[] { (15f, 15f), (33f, 15f), (15f, 33f), (33f, 33f) }) S(p, Ngon(cx, cy, 8, 4), true, 2.6f); };
        // settings: a cog with square teeth and a diamond bore (the bore cut once, here)
        var cog = new List<Vector2>();
        for (int i = 0; i < 8; i++)
        {
            float a = -P / 2 + i * P / 4;
            foreach (var (r, d) in new[] { (14f, -0.32f), (20f, -0.2f), (20f, 0.2f), (14f, 0.32f) }) cog.Add(new Vector2(24 + Mathf.Cos(a + d) * r, 24 + Mathf.Sin(a + d) * r));
        }
        List<Vector2[]> bored = Minus(new[] { cog.ToArray() }, Ngon(24, 24, 6, 4));
        _icons["settings"] = p => { foreach (Vector2[] piece in bored) p.Fill(piece); };
    }
}
