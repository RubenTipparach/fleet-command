// KelvaneKit: the Kelvane Institute skin (console-skins design, "Kelvane Institute"; openspec/changes/kelvane-battle-console):
// the laboratory bench's calibrated instruments, every piece rendered in Blender by tools/blender/build_ui_kelvane.py
// (assets/ui/kelvane/manifest.json): pillowed champagne plates rolled round wells of smoked glass, amber windows, phosphor
// scopes, window keys and wide keys, bars of lit cells, title placards, the backing plate, and the strips' printed meter
// scales whose needles are drawn live (hull integrity on the top strip, speed on the bottom); its icons drawn as schematic
// legends (even round strokes, true circles, sine waves); its colour roles, ink, type (Share Tech Mono) and rounded drawn
// keys. A port of docs/mockups/lib/kelvane.js (mockup 20), field for field and shape for shape. Where it comes from is the
// Institute's tradition of the instrument a scientist can check by eye, never what the Kelvane look like (CLAUDE.md
// section 1). Our own design: no other game's or film's names, shapes, glyphs or art.
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static FleetCommand.Game.Ui.Kit.IconGeom;

namespace FleetCommand.Game.Ui.Kit;

public sealed class KelvaneKit : IKit
{
    private const string ShareTechMono = "res://assets/fonts/ShareTechMono-Regular.ttf";
    private const float P = Mathf.Pi;

    // the palette (kelvane.js KV): a reading patiently lit (amber), the measurement (phosphor), the record (ink)
    private static readonly Color Amber = new("#ffc865"), AmberHi = new("#ffe2a4"), Phos = new("#5df08a"), Ink = new("#4a3412"),
        InkOn = new("#2a1a06"), Parchment = new("#f3e4c2");

    public string Name => "Kelvane Institute";

    // The battle console's colour roles (kelvane.js CC): amber for a reading, brass print for a label, parchment for prose,
    // phosphor green for whole and engaged, a hotter amber-orange for damaged, needle red only for down and alarms.
    public Palette C { get; } = new(
        Deep: new Color("#4a3d26"), Signal: new Color("#4a3820"), Sky: Amber, Accord: Phos,
        Lilac: new Color("#b8a074"), Caution: new Color("#ff8f3a"), Alert: new Color("#ff4f3a"), Text: Parchment);

    public SkinStyle Style { get; }

    private readonly RenderedPieces _r = new("kelvane");
    /// <summary>Measured from the kit's own key renders (RenderedPieces.Glass).</summary>
    public FleetCommand.Data.Rect KeyGlass => _glass ??= _r.Glass("key.idle", "key.on");
    private FleetCommand.Data.Rect? _glass;
    private readonly HashSet<string> _supplies = new();
    public IReadOnlyCollection<string> Supplies => _supplies;
    private readonly Dictionary<string, Action<Pen>> _icons = new();

    /// <summary>The console's own screened pieces, standing in under the kit's names (kelvane.js CONSOLE): the plate rolled
    /// round a well of smoked glass, the strips with a long screen and their meter bays, a key and a tab unlit at rest, the
    /// name plate with a flat for the Fleet key.</summary>
    private static readonly (string Name, string Piece)[] Console =
    {
        ("frame.panel", "frame.screen"), ("frame.inset", "inset.screen"), ("strip.top", "strip.top.screen"), ("strip.bottom", "strip.bottom.screen"),
        ("button.idle", "button.idle.screen"), ("tab.idle", "tab.idle.screen"), ("nameplate", "nameplate.console"),
    };

    public KelvaneKit()
    {
        Style = new SkinStyle
        {
            R = new Roles(Ink: InkOn, InkDis: new Color("#5a4a30"), Night: new Color("#0f0b07"), Ghost: new Color("#75674f"),
                Mute: new Color("#2a2318"), Plate: new Color("#16110a"), Title: new Color("#ffd27a"), Sheet: new Color("#1c1a16")),
            // Share Tech Mono, the instrument's readout face, for every readout; it runs 1.31 times as wide as Antonio, so
            // it is set at 0.8 times the size, and the keys' fit shrinks what still runs long (rule T)
            KeySound = "ui_clunk", // the modelled metal skins' clunk (sound-effects design section 2)
            Type = new TypeSet(ShareTechMono, ShareTechMono, (size, weight) => new Face(false, (int)MathF.Round(size * 0.8f), 400)),
            // dark on the lit colours, amber on the dark glass; nothing on a key is white
            InkOn = fill => KitInk.On(fill, InkOn, Amber),
            // at rest the window is unlit smoke and the name glows on it in amber (a tab's a little dimmer); lit, dark ink
            KeyInk = new KeyInk(Idle: Amber, Hover: InkOn, Pressed: InkOn, TabOn: InkOn, TabOff: new Color("#d9aa58")),
            TabIcon = Parchment, GlyphOnDark = Amber,
            Mark = new Marks(Hover: AmberHi, Ready: Phos, Count: Parchment),
            // clear of the 12 px bezel and 12 px into the glass
            Pad = new Pad(26, 24, 24, 24),
            // nameplate.console's amber window (46-360 x 9-47), 8 px clear of its left end, in ink on the lit film
            Plate = new PlateText(54, 36, 300, new Face(false, 22, 400), Ink),
            // labels centred in each window's open field: on a key 46 px from the left (past the domed key) and 12 from the
            // right; on a tab 30 from the left (past the pilot lamp) and 10 from the right
            KeyCenter = true, KeyField = (46, 12), TabField = (30, 10),
            // the strips at their full 72 px, the content laid out for 56 px moved down to centre in the screen (9-63)
            StripTopDy = 14, StripBottomDy = 4, StripTopH = 72, StripBottomH = 72,
            // the strips' needle meters: pivot (46, 50), needle 20 px, swept 205 to 335 degrees, settling at 4 per second
            Meter = new StripMeterDef(46, 50, 72, 20, 205 * P / 180, 335 * P / 180, 4),
            Needle = DrawNeedle,
            // a panel title's baseline 2 px higher, centred in the title placard's window (y 3-21 of 24)
            TitleDy = 2,
            TitlePlacard = true, SymbolBars = true, RenderedKeys = true, IconKeys = true, Backing = true,
        };
        foreach (var (name, piece) in Console) _r.Alias(name, piece);
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
        if (!_icons.TryGetValue(icon, out var d)) throw new KeyNotFoundException($"Kelvane kit has no icon '{icon}'");
        d(new Pen(c, r.Position, r.Size.X / 48f, ink));
    }

    public PieceSpec Spec(string piece) => _r.Spec(piece);

    /// <summary>The Institute's controls are rounded, never cut: pressed plates with soft corners (kelvane.js rr).</summary>
    public void Shape(CanvasItem ci, Rect2 r, float radius, Color col, float line = 0)
    {
        if (r.Size.X <= 0 || r.Size.Y <= 0) return;
        float rad = Math.Min(radius, Math.Min(r.Size.X / 2, r.Size.Y / 2));
        if (line > 0) Shapes.RoundRectLine(ci, r, col, rad, line);
        else Shapes.RoundRect(ci, r, col, rad);
    }

    /// <summary>Round knobs stay round (kelvane.js knob).</summary>
    public void Knob(CanvasItem ci, Vector2 centre, float r, Color col) => ci.DrawCircle(centre, r, col, true, -1, true);

    /// <summary>A strip meter's needle (kelvane.js live.needle): a needle-red pointer from the pivot over the printed scale,
    /// a faint shadow on the film below it, and a dark brass hub on the pivot.</summary>
    private static void DrawNeedle(CanvasItem c, Vector2 pivot, float r, float a)
    {
        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        var sh = pivot + new Vector2(0.8f, 1.2f);
        c.DrawLine(sh, sh + dir * r, new Color(60 / 255f, 36 / 255f, 10 / 255f, 0.35f), 1.6f, true);
        c.DrawLine(pivot - dir * 3, pivot + dir * r, new Color("#c8302a"), 1.4f, true);
        c.DrawCircle(pivot, 2.6f, new Color("#5a4520"), true, -1, true);
        c.DrawCircle(pivot - new Vector2(0.5f, 0.5f), 1.2f, new Color("#b0915a"), true, -1, true);
    }

    // ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ------------------------------------
    // kelvane.js icon(), on a 48 x 48 grid: a schematic drawing, as on an instrument's legend plate. Even strokes with round
    // caps and joins, true circles and arcs, sine waves for anything given off; a solid where the thing itself is solid.

    private void DefineIcons()
    {
        // a stroke with round caps and joins (a dot of the stroke's width at every point of an open line)
        static void L(Pen p, Vector2[] pts, bool closed = false, float lw = 3)
        {
            p.Width = lw;
            p.Stroke(pts, closed);
            if (!closed) foreach (Vector2 q in pts) p.Dot(q.X, q.Y, lw / 2);
            p.Width = 3;
        }
        static void Ln(Pen p, float x0, float y0, float x1, float y1, float lw = 3) => L(p, Pts(x0, y0, x1, y1), false, lw);
        static void C(Pen p, float cx, float cy, float r, float lw = 3) { p.Width = lw; p.Circle(cx, cy, r); p.Width = 3; }
        static void A(Pen p, float cx, float cy, float r, float a0, float a1, float lw = 3) => L(p, ArcPts(cx, cy, r, a0, a1, 20), false, lw);
        static void RB(Pen p, float x0, float y0, float x1, float y1, float r, bool fill, float lw = 3)
        {
            if (fill) p.RoundRect(x0, y0, x1 - x0, y1 - y0, r);
            else L(p, RoundRectPts(x0, y0, x1, y1, r), true, lw);
        }
        static void Wave(Pen p, float x0, float x1, float y, float amp, float cycles, float lw = 2.4f) =>
            L(p, Enumerable.Range(0, 33).Select(i => new Vector2(x0 + (x1 - x0) * i / 32f, y - Mathf.Sin(i / 32f * cycles * 2 * P) * amp)).ToArray(), false, lw);
        static Vector2 At(float cx, float cy, float a, float r) => new(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);

        // the helm: a heading dial, its needle on the course and the pivot solid; ticks at the cardinal points
        _icons["helm"] = p =>
        {
            C(p, 24, 24, 18, 2.6f);
            for (int i = 0; i < 4; i++) { float a = i * P / 2; L(p, new[] { At(24, 24, a, 14), At(24, 24, a, 18) }, false, 2.4f); }
            p.Fill(Pts(24, 8, 29, 24, 24, 27, 19, 24)); p.Dot(24, 24, 3.6f);
        };
        // power: a bolt inside a meter's ring
        _icons["power"] = p => { C(p, 24, 24, 19, 2.6f); p.Fill(Pts(27, 9, 15, 27, 23, 27, 20, 40, 33, 21, 25, 21)); };
        // tactical: a turret on its ring mount, the barrel out to the upper right
        _icons["tactical"] = p => { p.Fill(ArcPts(22, 32, 13, P, 2 * P)); RB(p, 6, 34, 38, 41, 3, true); Ln(p, 26, 24, 41, 9, 4.5f); };
        // point defence: a shield with a ranging reticle on it
        _icons["pd"] = p =>
        {
            L(p, Pts(24, 5, 39, 11, 39, 25, 24, 43, 9, 25, 9, 11), true, 2.8f); C(p, 24, 22, 7, 2.4f);
            Ln(p, 24, 11, 24, 15, 2.4f); Ln(p, 24, 29, 24, 33, 2.4f); Ln(p, 13, 22, 17, 22, 2.4f); Ln(p, 31, 22, 35, 22, 2.4f);
        };
        // science: a lens over a trace, the reading under study
        _icons["science"] = p => { C(p, 20, 20, 13, 3); Ln(p, 30, 30, 42, 42, 5); Wave(p, 11, 29, 20, 4.5f, 1.5f, 2.2f); };
        // comms: a dish on its stand, two waves going out
        _icons["comms"] = p =>
        {
            p.Fill(ArcPts(18, 26, 13, P * 0.75f, P * 1.75f)); Ln(p, 18, 26, 12, 42, 3); Ln(p, 6, 42, 20, 42, 3);
            A(p, 18, 26, 21, -P * 0.45f, -P * 0.05f, 2.6f); A(p, 18, 26, 28, -P * 0.42f, -P * 0.08f, 2.6f);
        };
        // repair: a spanner, its jaw open to the upper right (the jaw's mouth cut once, here)
        Vector2[][] jaw = Geometry2D.ClipPolygons(EllipsePts(32, 16, 9, 9), RoundRectPts(32, 10, 44, 18, 1.5f)).ToArray();
        _icons["repair"] = p => { Ln(p, 9, 39, 27, 21, 5.5f); foreach (Vector2[] j in jaw) p.Fill(j); };
        // transport: a figure, its lower half breaking into lines as it beams
        _icons["transport"] = p =>
        {
            p.Dot(24, 9, 5); p.Fill(Pts(15, 30, 17, 18, 31, 18, 33, 30));
            foreach (var (y, w) in new[] { (34f, 9f), (38f, 7f), (42f, 5f) }) Ln(p, 24 - w, y, 24 + w, y, 2.4f);
        };
        // fleet: three ships in formation, the lead ahead, each a pointed hull
        _icons["fleet"] = p =>
        {
            void Sh(float cx, float cy, float k) => p.Fill(Pts(cx, cy - 10 * k, cx + 6 * k, cy - k, cx + 5 * k, cy + 9 * k, cx - 5 * k, cy + 9 * k, cx - 6 * k, cy - k));
            Sh(24, 13, 1); Sh(11, 33, 0.9f); Sh(37, 33, 0.9f);
        };
        // the ship from above: a rounded research hull, its prow forward, on two engine pods
        _icons["ship"] = p =>
        {
            p.Fill(EllipsePts(24, 19, 8, 15)); Ln(p, 17, 28, 11, 31, 2.6f); Ln(p, 31, 28, 37, 31, 2.6f);
            RB(p, 6, 26, 14, 44, 3.5f, true); RB(p, 34, 26, 42, 44, 3.5f, true);
        };
        // shields: six arcs round the hull
        _icons["shields"] = p => { for (int i = 0; i < 6; i++) { float a = -P / 2 + i * P / 3; A(p, 24, 24, 19, a - 0.4f, a + 0.4f, 3.2f); } p.Fill(EllipsePts(24, 24, 5, 9)); };
        // sensors: a scope with its graticule, a sweep and two contacts
        _icons["sensors"] = p => { C(p, 24, 24, 19, 2.6f); Ln(p, 24, 24, 37, 11, 2.6f); C(p, 24, 24, 9, 1.4f); p.Dot(31, 30, 2.8f); p.Dot(13, 17, 2.8f); };
        // tractor: an emitter, the beam widening to the hull it holds
        _icons["tractor"] = p => { RB(p, 3, 19, 12, 29, 2, true); Ln(p, 12, 21, 31, 12, 2.2f); Ln(p, 12, 27, 31, 36, 2.2f); RB(p, 32, 14, 45, 34, 4, false, 2.6f); };
        // craft: a small swept-wing craft
        _icons["craft"] = p => p.Fill(Pts(24, 4, 29, 22, 44, 30, 29, 33, 24, 44, 19, 33, 4, 30, 19, 22));
        // weapons: a beam from an emitter, ending in a strike
        _icons["weapons"] = p =>
        {
            p.Dot(9, 39, 5); Ln(p, 12, 36, 30, 18, 3.4f);
            for (int i = 0; i < 6; i++) { float a = i * P / 3; L(p, new[] { At(34, 14, a, 4), At(34, 14, a, 10) }, false, 2.2f); }
        };
        // warp: a chevron trailing speed lines
        _icons["warp"] = p => { L(p, Pts(22, 10, 38, 24, 22, 38), false, 4.5f); Ln(p, 5, 16, 17, 16, 2.6f); Ln(p, 3, 24, 21, 24, 2.6f); Ln(p, 5, 32, 17, 32, 2.6f); };
        // engines: a nozzle and the exhaust wave out of it
        _icons["engines"] = p => { p.Fill(Pts(14, 5, 34, 5, 38, 21, 10, 21)); Ln(p, 16, 27, 32, 27, 2.6f); Ln(p, 19, 34, 29, 34, 2.6f); Ln(p, 22, 41, 26, 41, 2.6f); };
        // reactor: a solid core and two crossed orbits
        _icons["reactor"] = p => { p.Dot(24, 24, 5.5f); foreach (float r in new[] { P / 4, -P / 4 }) L(p, EllipsePts(24, 24, 19, 8, r), true, 2.6f); };
        // map: a plotted course, dotted, to a mark
        _icons["map"] = p =>
        {
            foreach (var (x, y) in new[] { (8f, 40f), (12f, 33f), (12f, 26f), (17f, 20f), (24f, 18f) }) p.Dot(x, y, 2.4f);
            Ln(p, 29, 6, 41, 18, 3.4f); Ln(p, 41, 6, 29, 18, 3.4f);
        };
        _icons["pause"] = p => { Ln(p, 17, 11, 17, 37, 6.5f); Ln(p, 31, 11, 31, 37, 6.5f); };
        _icons["menu"] = p => { foreach (float y in new[] { 13f, 24f, 35f }) Ln(p, 10, y, 38, y, 4.2f); };
        // fire: four brackets closing on a solid mark
        _icons["fire"] = p =>
        {
            foreach (float[] b in new[] { new float[] { 8, 17, 8, 8, 17, 8 }, new float[] { 31, 8, 40, 8, 40, 17 }, new float[] { 40, 31, 40, 40, 31, 40 }, new float[] { 17, 40, 8, 40, 8, 31 } })
                L(p, Pts(b), false, 3.2f);
            p.Dot(24, 24, 5);
        };
        _icons["cargo"] = p => { foreach (var (cx, cy) in new[] { (15f, 15f), (33f, 15f), (15f, 33f), (33f, 33f) }) RB(p, cx - 7.5f, cy - 7.5f, cx + 7.5f, cy + 7.5f, 3, false, 2.6f); };
        // settings: a cog with round teeth round its bore
        _icons["settings"] = p =>
        {
            C(p, 24, 24, 12, 3.4f);
            for (int i = 0; i < 8; i++) { float a = i * P / 4 + P / 8; Vector2 q = At(24, 24, a, 16); p.Dot(q.X, q.Y, 3.6f); }
            C(p, 24, 24, 4.4f, 2.6f);
        };
    }
}
