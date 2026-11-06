// KelvaneKit: the Kelvane Institute skin (console-skins design, "Kelvane Institute"; openspec/changes/kelvane-battle-console,
// kelvane-console-engine): the laboratory bench, pillowed champagne plates with readouts lit amber behind smoked glass,
// phosphor green for a measurement taken, needle red for the reading that matters. Every piece is rendered in Blender by
// tools/blender/build_ui_kelvane.py (assets/ui/kelvane/manifest.json), the console's screened pieces standing in under the
// kit's names as the mockup's CONSOLE map has them; its icons drawn as schematic legend-plate drawings (even round strokes,
// true circles, sine waves for anything given off); its colour roles, ink, type (Share Tech Mono) and rounded keys; the
// strips' needle meters. A port of docs/mockups/lib/kelvane.js (mockup 20), field for field and shape for shape, so the
// game draws what the owner reviewed.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;
using Godot;

namespace FleetCommand.Game.Ui.Kit;

public sealed class KelvaneKit : IKit
{
    private const string ShareTechMono = "res://assets/fonts/ShareTechMono-Regular.ttf";
    private const string CourierPrime = "res://assets/fonts/CourierPrime-Bold.ttf";
    private const float P = Mathf.Pi;

    // The palette (kelvane.js KV): the instrument, a reading patiently lit, the measurement itself, the reading that
    // matters, the record, craft and calibration.
    private static readonly Color Amber = new("#ffc865"), AmberHi = new("#ffe2a4"), Phos = new("#5df08a"), Needle = new("#ff4f3a");
    private static readonly Color InkPrint = new("#4a3412"), InkDark = new("#2a1a06"), Parchment = new("#f3e4c2");

    /// <summary>The console's names for the kit pieces drawn by the Institute's screened renders (kelvane.js CONSOLE): the
    /// plate rolled round a well of smoked glass, the strips with a long screen, a key and a tab unlit at rest, the name
    /// plate with a flat for the Fleet key.</summary>
    private static readonly Dictionary<string, string> Screened = new()
    {
        ["frame.panel"] = "frame.screen", ["frame.inset"] = "inset.screen", ["strip.top"] = "strip.top.screen", ["strip.bottom"] = "strip.bottom.screen",
        ["button.idle"] = "button.idle.screen", ["tab.idle"] = "tab.idle.screen", ["nameplate"] = "nameplate.console",
    };

    /// <inheritdoc/>
    public string Name => "Kelvane Institute";

    /// <summary>The battle console's colour roles (kelvane.js CC): amber for a reading, brass print for a label, parchment
    /// for prose and titles, phosphor for whole and engaged, a hotter amber-orange for damaged, needle red for down.</summary>
    public Palette C { get; } = new(
        Deep: new Color("#4a3d26"), Signal: new Color("#4a3820"), Sky: Amber, Accord: Phos,
        Lilac: new Color("#b8a074"), Caution: new Color("#ff8f3a"), Alert: Needle, Text: Parchment);

    /// <inheritdoc/>
    public SkinStyle Style { get; }

    private readonly RenderedPieces _r = new("kelvane");
    /// <summary>Measured from the kit's own key renders (RenderedPieces.Glass).</summary>
    public Rect KeyGlass => _glass ??= _r.Glass("key.idle", "key.on");
    private Rect? _glass;
    private readonly HashSet<string> _supplies = new();
    /// <inheritdoc/>
    public IReadOnlyCollection<string> Supplies => _supplies;
    private readonly Dictionary<string, Action<Pen>> _icons = new();

    public KelvaneKit()
    {
        Palette c = C;
        Style = new SkinStyle
        {
            R = new Roles(Ink: InkDark, InkDis: new Color("#5a4a30"), Night: new Color("#0f0b07"), Ghost: new Color("#75674f"),
                Mute: new Color("#2a2318"), Plate: new Color("#16110a"), Title: new Color("#ffd27a"), Sheet: new Color("#16110a")),
            // Share Tech Mono, the instrument's readout face, for every readout and title (kelvane.js font: every Antonio
            // size at 0.8 times, weight 400; the console's labels are all lit on glass). Courier Prime, the pair's face for
            // labels engraved on metal, is the heading face, but the console asks for none (the mockup sets titles in Share
            // Tech Mono too).
            Type = new TypeSet(ShareTechMono, CourierPrime, (size, weight) => new Face(false, Math.Max(1, (int)MathF.Round(size * 0.8f)), 400)),
            // dark ink on the lit colours, amber on the dark glass; nothing on a key is white (kelvane.js ink)
            InkOn = fill => Luminance(fill) > 0.2f ? InkDark : Amber,
            // at rest the window is unlit smoke and the name glows on it in amber; lit, the name is dark ink on the film
            KeyInk = new KeyInk(Idle: Amber, Hover: InkDark, Pressed: InkDark, TabOn: InkDark, TabOff: new Color("#d9aa58")),
            TabIcon = Parchment, GlyphOnDark = Amber,
            Mark = new Marks(Hover: AmberHi, Ready: Phos, Count: Parchment),
            // clear of the 12 px bezel and 12 px into the glass (frame.screen's well)
            Pad = new Pad(26, 24, 24, 24),
            // the name plate's amber window (nameplate.console, 46-360 x 9-47), in ink on the lit film
            Plate = new PlateText(54, 36, 300, new Face(false, 22, 400), InkPrint),
            KeyCenter = true,
            KeyField = (46, 12), TabField = (30, 10), IconKeys = true, TitleDy = 2,
            // the strips at their full 72 px, the content laid out for 56 px moved down to centre in the screen
            // (kelvane.js stripTop, stripBottom)
            StripTopDy = 14, StripBottomDy = 4, StripTopH = 72, StripBottomH = 72,
            // the strips' needle meter: pivot (46, 50), needle 20 px in the 72 px strip, swept 205 to 335 degrees over the
            // printed scale, settling at 4 a second (kelvane.js meter)
            Meter = new StripMeter(46, 50, 72, 20, 205 * P / 180, 335 * P / 180, 4),
            Needle = DrawNeedle,
            TitlePlacard = true, SymbolBars = true, RenderedKeys = true,
        };
        // The Blender kit has no green bar fill (the Institute's bars are symbol rows, so the console never draws one);
        // the kit's list still names it, so it is the amber fill with its light turned to the state colour, as the Krai
        // make their ok symbol.
        if (!_r.Has("bar.fill.accord")) _r.AddHueTurned("bar.fill.accord", "bar.fill", c.Accord);
        DefineIcons();
        foreach (string k in _r.Names) _supplies.Add(k);
        foreach (string k in Screened.Keys) if (_r.Has(Screened[k])) _supplies.Add(k);
        foreach (string k in _icons.Keys) _supplies.Add("icon." + k);
    }

    /// <summary>The rendered piece that draws a kit name: the console's screened piece where there is one.</summary>
    private static string Alias(string id) => Screened.TryGetValue(id, out string? s) ? s : id;

    /// <summary>Relative luminance of a colour (WCAG), for picking the ink that reads on a fill.</summary>
    private static float Luminance(Color col)
    {
        static float Lin(float v) => v <= 0.03928f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
        return 0.2126f * Lin(col.R) + 0.7152f * Lin(col.G) + 0.0722f * Lin(col.B);
    }

    /// <inheritdoc/>
    public void Piece(CanvasItem c, string piece, Rect2 r, float alpha = 1) => _r.Draw(c, Alias(piece), r, alpha);

    /// <inheritdoc/>
    public void Hero(CanvasItem c, string hero, Rect2 r, float alpha = 1) => _r.Draw(c, Alias(hero), r, alpha);

    /// <inheritdoc/>
    public void HeroBottom(CanvasItem c, string hero, Rect2 r, float frac) => _r.DrawBottom(c, Alias(hero), r, frac);

    /// <inheritdoc/>
    public void Icon(CanvasItem c, string icon, Rect2 r, Color ink)
    {
        if (!_icons.TryGetValue(icon, out var d)) throw new KeyNotFoundException($"Kelvane kit has no icon '{icon}'");
        d(new Pen(c, r.Position, r.Size.X / 48f, ink));
    }

    /// <inheritdoc/>
    public PieceSpec Spec(string piece) => _r.Spec(Alias(piece));

    /// <summary>The Institute's controls are rounded, never cut: pressed plates with soft corners (kelvane.js rr).</summary>
    public void Shape(CanvasItem c, Rect2 r, float radius, Color col, float line = 0)
    {
        if (r.Size.X <= 0 || r.Size.Y <= 0) return;
        float rad = Math.Min(radius, Math.Min(r.Size.X / 2, r.Size.Y / 2));
        if (line > 0) Shapes.RoundRectLine(c, r, col, rad, line);
        else Shapes.RoundRect(c, r, col, rad);
    }

    /// <summary>Round knobs (kelvane.js knob).</summary>
    public void Knob(CanvasItem c, Vector2 centre, float r, Color col) => c.DrawCircle(centre, r, col, true, -1, true);

    /// <summary>A strip meter's needle (kelvane.js live.needle): a needle-red pointer from the pivot over the printed scale,
    /// angle <paramref name="a"/> in radians, length <paramref name="r"/>, with a dark brass hub on the pivot and a faint
    /// shadow on the film below it.</summary>
    private static void DrawNeedle(CanvasItem ci, Vector2 pivot, float r, float a)
    {
        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        var sh = pivot + new Vector2(0.8f, 1.2f);
        ci.DrawLine(sh, sh + dir * r, new Color(60 / 255f, 36 / 255f, 10 / 255f, 0.35f), 1.6f, true);
        ci.DrawLine(pivot - dir * 3, pivot + dir * r, new Color("#c8302a"), 1.4f, true);
        ci.DrawCircle(pivot, 2.6f, new Color("#5a4520"), true, -1, true);
        ci.DrawCircle(pivot - new Vector2(0.5f, 0.5f), 1.2f, new Color("#b0915a"), true, -1, true);
    }

    // ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ------------------------------------
    // kelvane.js icon(), on a 48 x 48 grid with 4 px clear: a schematic drawing, as on an instrument's legend plate. Even
    // strokes with round caps and joins, true circles and arcs, sine waves for anything given off; a solid where the thing
    // itself is solid. (The mockup's soft glow on a bright ink is not drawn here.)

    private static Vector2[] Pts(params float[] xy)
    {
        var p = new Vector2[xy.Length / 2];
        for (int i = 0; i < p.Length; i++) p[i] = new Vector2(xy[2 * i], xy[2 * i + 1]);
        return p;
    }

    private static Vector2[] EllipsePts(float cx, float cy, float rx, float ry, float rot = 0, int n = 40) =>
        Enumerable.Range(0, n).Select(i =>
        {
            float t = i * 2 * P / n, ex = rx * Mathf.Cos(t), ey = ry * Mathf.Sin(t);
            return new Vector2(cx + ex * Mathf.Cos(rot) - ey * Mathf.Sin(rot), cy + ex * Mathf.Sin(rot) + ey * Mathf.Cos(rot));
        }).ToArray();

    /// <summary>A rounded rectangle's outline as points, x0 to x1 and y0 to y1 (the mockup's roundRect).</summary>
    private static Vector2[] RoundBox(float x0, float y0, float x1, float y1, float r)
    {
        r = Math.Min(r, Math.Min((x1 - x0) / 2, (y1 - y0) / 2));
        return new PathBuilder().ArcPts(x0 + r, y0 + r, r, P, 1.5f * P, 6).ArcPts(x1 - r, y0 + r, r, -0.5f * P, 0, 6)
            .ArcPts(x1 - r, y1 - r, r, 0, 0.5f * P, 6).ArcPts(x0 + r, y1 - r, r, 0.5f * P, P, 6).Pts.ToArray();
    }

    private void DefineIcons()
    {
        static void L(Pen p, Vector2[] pts, bool closed = false, float lw = 3) { p.Width = lw; p.Stroke(pts, closed); p.Width = 3; }
        static void C(Pen p, float cx, float cy, float r, float lw = 3) { p.Width = lw; p.Circle(cx, cy, r); p.Width = 3; }
        static void A(Pen p, float cx, float cy, float r, float a0, float a1, float lw = 3) { p.Width = lw; p.Arc(cx, cy, r, a0, a1); p.Width = 3; }
        static void RB(Pen p, float x0, float y0, float x1, float y1, float r, bool fill, float lw = 3)
        {
            if (fill) p.Fill(RoundBox(x0, y0, x1, y1, r));
            else L(p, RoundBox(x0, y0, x1, y1, r), true, lw);
        }
        // a sine wave from x0 to x1 about y, amp px high, cycles whole periods
        static void Wave(Pen p, float x0, float x1, float y, float amp, float cycles, float lw = 2.4f) =>
            L(p, Enumerable.Range(0, 33).Select(i => new Vector2(x0 + (x1 - x0) * i / 32f, y - Mathf.Sin(i / 32f * cycles * 2 * P) * amp)).ToArray(), false, lw);

        // the helm: a heading dial, its needle on the course and the pivot solid; ticks at the cardinal points
        _icons["helm"] = p =>
        {
            C(p, 24, 24, 18, 2.6f);
            for (int i = 0; i < 4; i++) { float a = i * P / 2; L(p, Pts(24 + Mathf.Cos(a) * 14, 24 + Mathf.Sin(a) * 14, 24 + Mathf.Cos(a) * 18, 24 + Mathf.Sin(a) * 18), false, 2.4f); }
            p.Fill(Pts(24, 8, 29, 24, 24, 27, 19, 24)); p.Dot(24, 24, 3.6f);
        };
        // power: a bolt inside a meter's ring
        _icons["power"] = p => { C(p, 24, 24, 19, 2.6f); p.Fill(Pts(27, 9, 15, 27, 23, 27, 20, 40, 33, 21, 25, 21)); };
        // tactical: a turret on its ring mount, the barrel out to the upper right
        _icons["tactical"] = p =>
        {
            p.Fill(new PathBuilder().ArcPts(22, 32, 13, P, 2 * P, 16).Pts);
            RB(p, 6, 34, 38, 41, 3, true); L(p, Pts(26, 24, 41, 9), false, 4.5f);
        };
        // point defence: a shield with a ranging reticle on it
        _icons["pd"] = p =>
        {
            L(p, Pts(24, 5, 39, 11, 39, 25, 24, 43, 9, 25, 9, 11), true, 2.8f); C(p, 24, 22, 7, 2.4f);
            L(p, Pts(24, 11, 24, 15), false, 2.4f); L(p, Pts(24, 29, 24, 33), false, 2.4f); L(p, Pts(13, 22, 17, 22), false, 2.4f); L(p, Pts(31, 22, 35, 22), false, 2.4f);
        };
        // science: a lens over a trace, the reading under study
        _icons["science"] = p => { C(p, 20, 20, 13, 3); L(p, Pts(30, 30, 42, 42), false, 5); Wave(p, 11, 29, 20, 4.5f, 1.5f, 2.2f); };
        // comms: a dish on its stand, two waves going out
        _icons["comms"] = p =>
        {
            p.Fill(new PathBuilder().ArcPts(18, 26, 13, P * 0.75f, P * 1.75f, 16).Pts);
            L(p, Pts(18, 26, 12, 42), false, 3); L(p, Pts(6, 42, 20, 42), false, 3);
            A(p, 18, 26, 21, -P * 0.45f, -P * 0.05f, 2.6f); A(p, 18, 26, 28, -P * 0.42f, -P * 0.08f, 2.6f);
        };
        // repair: a spanner, its jaw open to the upper right (the jaw's mouth cut once, here)
        Vector2[][] jaw = Geometry2D.ClipPolygons(EllipsePts(32, 16, 9, 9), RoundBox(32, 10, 44, 18, 1.5f)).ToArray();
        _icons["repair"] = p => { L(p, Pts(9, 39, 27, 21), false, 5.5f); foreach (Vector2[] j in jaw) p.Fill(j); };
        // transport: a figure, its lower half breaking into lines as it beams
        _icons["transport"] = p =>
        {
            p.Dot(24, 9, 5); p.Fill(Pts(15, 30, 17, 18, 31, 18, 33, 30));
            foreach (var (y, w) in new[] { (34f, 9f), (38f, 7f), (42f, 5f) }) L(p, Pts(24 - w, y, 24 + w, y), false, 2.4f);
        };
        // fleet: three ships in formation, the lead ahead, each a pointed hull
        _icons["fleet"] = p =>
        {
            void Sh(float cx, float cy, float k) => p.Fill(Pts(cx, cy - 10 * k, cx + 6 * k, cy - 1 * k, cx + 5 * k, cy + 9 * k, cx - 5 * k, cy + 9 * k, cx - 6 * k, cy - 1 * k));
            Sh(24, 13, 1); Sh(11, 33, 0.9f); Sh(37, 33, 0.9f);
        };
        // the ship from above: a rounded research hull, its prow forward, on two engine pods
        _icons["ship"] = p =>
        {
            p.Fill(EllipsePts(24, 19, 8, 15));
            L(p, Pts(17, 28, 11, 31), false, 2.6f); L(p, Pts(31, 28, 37, 31), false, 2.6f);
            RB(p, 6, 26, 14, 44, 3.5f, true); RB(p, 34, 26, 42, 44, 3.5f, true);
        };
        // shields: six arcs round the hull
        _icons["shields"] = p =>
        {
            for (int i = 0; i < 6; i++) { float a = -P / 2 + i * P / 3; A(p, 24, 24, 19, a - 0.4f, a + 0.4f, 3.2f); }
            p.Fill(EllipsePts(24, 24, 5, 9));
        };
        // sensors: a scope with its graticule, a sweep and two contacts
        _icons["sensors"] = p => { C(p, 24, 24, 19, 2.6f); L(p, Pts(24, 24, 37, 11), false, 2.6f); C(p, 24, 24, 9, 1.4f); p.Dot(31, 30, 2.8f); p.Dot(13, 17, 2.8f); };
        // tractor: an emitter, the beam widening to the hull it holds
        _icons["tractor"] = p =>
        {
            RB(p, 3, 19, 12, 29, 2, true); L(p, Pts(12, 21, 31, 12), false, 2.2f); L(p, Pts(12, 27, 31, 36), false, 2.2f);
            RB(p, 32, 14, 45, 34, 4, false, 2.6f);
        };
        // craft: a small swept-wing craft
        _icons["craft"] = p => p.Fill(Pts(24, 4, 29, 22, 44, 30, 29, 33, 24, 44, 19, 33, 4, 30, 19, 22));
        // weapons: a beam from an emitter, ending in a strike
        _icons["weapons"] = p =>
        {
            p.Dot(9, 39, 5); L(p, Pts(12, 36, 30, 18), false, 3.4f);
            for (int i = 0; i < 6; i++) { float a = i * P / 3; L(p, Pts(34 + Mathf.Cos(a) * 4, 14 + Mathf.Sin(a) * 4, 34 + Mathf.Cos(a) * 10, 14 + Mathf.Sin(a) * 10), false, 2.2f); }
        };
        // warp: a chevron trailing speed lines
        _icons["warp"] = p =>
        {
            L(p, Pts(22, 10, 38, 24, 22, 38), false, 4.5f);
            L(p, Pts(5, 16, 17, 16), false, 2.6f); L(p, Pts(3, 24, 21, 24), false, 2.6f); L(p, Pts(5, 32, 17, 32), false, 2.6f);
        };
        // engines: a nozzle and the exhaust wave out of it
        _icons["engines"] = p =>
        {
            p.Fill(Pts(14, 5, 34, 5, 38, 21, 10, 21));
            L(p, Pts(16, 27, 32, 27), false, 2.6f); L(p, Pts(19, 34, 29, 34), false, 2.6f); L(p, Pts(22, 41, 26, 41), false, 2.6f);
        };
        // reactor: a solid core and two crossed orbits
        _icons["reactor"] = p => { p.Dot(24, 24, 5.5f); p.Width = 2.6f; p.Ellipse(24, 24, 19, 8, P / 4); p.Ellipse(24, 24, 19, 8, -P / 4); p.Width = 3; };
        // map: a plotted course, dotted, to a mark
        _icons["map"] = p =>
        {
            foreach (var (px, py) in new[] { (8f, 40f), (12f, 33f), (12f, 26f), (17f, 20f), (24f, 18f) }) p.Dot(px, py, 2.4f);
            L(p, Pts(29, 6, 41, 18), false, 3.4f); L(p, Pts(41, 6, 29, 18), false, 3.4f);
        };
        _icons["pause"] = p => { L(p, Pts(17, 11, 17, 37), false, 6.5f); L(p, Pts(31, 11, 31, 37), false, 6.5f); };
        _icons["menu"] = p => { foreach (float y in new[] { 13f, 24f, 35f }) L(p, Pts(10, y, 38, y), false, 4.2f); };
        // fire: four brackets closing on a solid mark
        _icons["fire"] = p =>
        {
            foreach (Vector2[] b in new[] { Pts(8, 17, 8, 8, 17, 8), Pts(31, 8, 40, 8, 40, 17), Pts(40, 31, 40, 40, 31, 40), Pts(17, 40, 8, 40, 8, 31) }) L(p, b, false, 3.2f);
            p.Dot(24, 24, 5);
        };
        _icons["cargo"] = p => { foreach (var (cx, cy) in new[] { (15f, 15f), (33f, 15f), (15f, 33f), (33f, 33f) }) RB(p, cx - 7.5f, cy - 7.5f, cx + 7.5f, cy + 7.5f, 3, false, 2.6f); };
        // settings: a cog with round teeth round its bore
        _icons["settings"] = p =>
        {
            C(p, 24, 24, 12, 3.4f);
            for (int i = 0; i < 8; i++) { float a = i * P / 4 + P / 8; p.Dot(24 + Mathf.Cos(a) * 16, 24 + Mathf.Sin(a) * 16, 3.6f); }
            C(p, 24, 24, 4.4f, 2.6f);
        };
    }
}
