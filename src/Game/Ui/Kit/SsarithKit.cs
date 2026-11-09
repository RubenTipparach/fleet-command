// SsarithKit: the Ssarith Hunt skin (console-skins design, "Ssarith Hunt"; openspec/changes/ssarith-battle-console): a
// hunter's kit, every piece rendered in Blender by tools/blender/build_ui_ssarith.py (assets/ui/ssarith/manifest.json):
// pillowed, cut-cornered olive gunmetal over CRT tubes that light themselves, tube keys and wide keys, claw-mark bars,
// title placards and the backing plate behind the column; its icons in the Hunt's own hand (straight strokes and 45 degree
// turns, cut corners, phosphor traces); its colour roles, ink, type (VT323 and Black Ops One) and cut-cornered drawn keys.
// A port of docs/mockups/lib/ssarith.js (mockup 15), field for field and shape for shape, so the game draws what the owner
// reviewed. Our own design: no other game's or film's names, shapes, glyphs or art.
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static FleetCommand.Game.Ui.Kit.IconGeom;

namespace FleetCommand.Game.Ui.Kit;

public sealed class SsarithKit : IKit
{
    private const string Vt323 = "res://assets/fonts/VT323-Regular.ttf";
    private const string BlackOps = "res://assets/fonts/BlackOpsOne-Regular.ttf";
    private const float P = Mathf.Pi;

    // the palette (ssarith.js SS): the tracker's phosphor, the count of kills in bone, the strike in kill red
    private static readonly Color Phos = new("#6dff8a"), Bone = new("#d8cfb4"), Dim = new("#7f8f6e");

    public string Name => "Ssarith Hunt";

    // The battle console's colour roles (ssarith.js CC): dark tubes at rest, phosphor for readouts, hover and the whole,
    // amber for the damaged, kill red only for the down and the warnings ("Alerts only"), moss labels, bone text.
    public Palette C { get; } = new(
        Deep: new Color("#163020"), Signal: new Color("#1d4a2a"), Sky: new Color("#6dff8a"), Accord: new Color("#6dff8a"),
        Lilac: new Color("#9aa886"), Caution: new Color("#ffb340"), Alert: new Color("#e0533c"), Text: new Color("#d8cfb4"));

    public SkinStyle Style { get; }

    private readonly RenderedPieces _r = new("ssarith");
    /// <summary>Measured from the kit's own key renders (RenderedPieces.Glass).</summary>
    public FleetCommand.Data.Rect KeyGlass => _glass ??= _r.Glass("key.idle", "key.on");
    private FleetCommand.Data.Rect? _glass;
    private readonly HashSet<string> _supplies = new();
    public IReadOnlyCollection<string> Supplies => _supplies;
    private readonly Dictionary<string, Action<Pen>> _icons = new();

    public SsarithKit()
    {
        var ink = new Color("#06120a");
        Style = new SkinStyle
        {
            R = new Roles(Ink: ink, InkDis: new Color("#3a4a3a"), Night: new Color("#0a140d"), Ghost: new Color("#4f5e4a"),
                Mute: new Color("#1c241e"), Plate: new Color("#141b16"), Title: Bone, Sheet: new Color("#0c120d")),
            // "Black Ops One" stencilled for headings, "VT323" phosphor for everything else. VT323 at 1.2x Antonio's size
            // measures about as wide (a pixel face is short), so every slot that fits a Concord label fits a Hunt one;
            // weight 700 at 22 px and up is a heading, Black Ops One at 0.8x.
            KeySound = "ui_clunk", // the modelled metal skins' clunk (sound-effects design section 2)
            Type = new TypeSet(Vt323, BlackOps, (size, weight) =>
                weight >= 700 && size >= 22 ? new Face(true, (int)MathF.Round(size * 0.8f), 400)
                : new Face(false, (int)MathF.Round(size * 1.2f), 400)),
            // dark on the lit colours, phosphor on the dark tubes; nothing on a key is white
            InkOn = fill => KitInk.On(fill, ink, Phos),
            // phosphor on the dark tube at rest, bone on the lit tube, dark on the flaring engaged tube; a tab at rest in moss
            KeyInk = new KeyInk(Idle: Phos, Hover: Bone, Pressed: ink, TabOn: Bone, TabOff: Dim),
            TabIcon = Bone, GlyphOnDark = Phos,
            Mark = new Marks(Hover: new Color("#b8ffc8"), Ready: Phos, Count: Bone),
            // clear of the bezel (28 px margins, the tube's own rim 4 px inside them)
            Pad = new Pad(26, 24, 24, 24),
            // the name plate's tube (54-372 x 9-47), 8 px clear of its ends
            Plate = new PlateText(64, 37, 296, new Face(false, 26, 400), Phos),
            // labels centred on the tube keys; on a tab, in its tube (x 9-111 of 120) with 6 px of glass round the label
            KeyCenter = true, TabField = (15, 15),
            // the strips (72 px rendered) drawn 64 px tall, their readouts on the flat middle (ssarith.js stripTop, stripBottom)
            StripTopDy = 4, StripBottomDy = 4, StripTopH = 64, StripBottomH = 64,
            TitlePlacard = true, SymbolBars = true, RenderedKeys = true, IconKeys = true, Backing = true,
        };
        // the ok symbol: the lit claw marks' light turned to bone, the count of the whole (ssarith.js load)
        _r.AddColorTurned("bar.cell.ok", "bar.cell.on", Bone);
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
        if (!_icons.TryGetValue(icon, out var d)) throw new KeyNotFoundException($"Ssarith kit has no icon '{icon}'");
        d(new Pen(c, r.Position, r.Size.X / 48f, ink));
    }

    public PieceSpec Spec(string piece) => _r.Spec(piece);

    /// <summary>The hunter's kit is cut-cornered, never round: every corner of a drawn key cut at 45 degrees, the cut as
    /// big as the rounding asked for (ssarith.js rr).</summary>
    public void Shape(CanvasItem ci, Rect2 r, float radius, Color col, float line = 0)
    {
        float X = r.Position.X, Y = r.Position.Y, W = r.Size.X, H = r.Size.Y;
        if (W <= 0 || H <= 0) return;
        Vector2[] pts = CutBox(X, Y, X + W, Y + H, Math.Max(0, Math.Min(radius * 0.7f, Math.Min(W / 2, H / 2))));
        if (line > 0) ci.DrawPolyline(pts.Append(pts[0]).ToArray(), col, line, true);
        else Pen.FillAbs(ci, pts, col);
    }

    /// <summary>Round knobs become octagons (ssarith.js knob).</summary>
    public void Knob(CanvasItem ci, Vector2 centre, float r, Color col) => Pen.FillAbs(ci, Oct(centre.X, centre.Y, r * 1.05f), col);

    private static Vector2[] Oct(float cx, float cy, float r) =>
        Enumerable.Range(0, 8).Select(i => new Vector2(cx + Mathf.Cos(P / 8 + i * P / 4) * r, cy + Mathf.Sin(P / 8 + i * P / 4) * r)).ToArray();

    private static Vector2[] CutBox(float x0, float y0, float x1, float y1, float c) =>
        Pts(x0 + c, y0, x1 - c, y0, x1, y0 + c, x1, y1 - c, x1 - c, y1, x0 + c, y1, x0, y1 - c, x0, y0 + c);

    private static Vector2 At(float a, float r) => new(24 + Mathf.Cos(a) * r, 24 + Mathf.Sin(a) * r);

    // ---- Icons: one subject list for every skin (docs/design/icon-guide.html) ------------------------------------
    // ssarith.js icon(), on a 48 x 48 grid: straight strokes and 45 degree turns, cut corners, phosphor traces (lines only,
    // a hunter's field marks), a small solid only where the thing itself is solid; one ink.

    private void DefineIcons()
    {
        // a trace: square caps, as the mockup's lineCap 'square' (an open stroke runs on half its width past each end)
        static void S(Pen p, Vector2[] pts, bool closed = false, float lw = 3)
        {
            p.Width = lw;
            if (!closed && pts.Length >= 2)
            {
                pts = (Vector2[])pts.Clone();
                pts[0] += (pts[0] - pts[1]).Normalized() * lw / 2;
                pts[^1] += (pts[^1] - pts[^2]).Normalized() * lw / 2;
            }
            p.Stroke(pts, closed);
            p.Width = 3;
        }
        static void L(Pen p, float x0, float y0, float x1, float y1, float lw = 3) => S(p, Pts(x0, y0, x1, y1), false, lw);

        // a wheel: an octagonal rim, four spokes, a square hub
        _icons["helm"] = p => { S(p, Oct(24, 24, 17), true); L(p, 24, 7, 24, 41, 2.4f); L(p, 7, 24, 41, 24, 2.4f); p.Fill(CutBox(19, 19, 29, 29, 3)); };
        // power: a jagged bolt inside a cut-cornered cell
        _icons["power"] = p => { S(p, CutBox(6, 6, 42, 42, 9), true, 2.4f); p.Fill(Pts(27, 9, 15, 26, 23, 26, 20, 39, 33, 21, 25, 21)); };
        // tactical: a gun on its mount: a stepped base, a housing, one long barrel
        _icons["tactical"] = p => { p.Fill(Pts(6, 42, 42, 42, 38, 36, 10, 36)); S(p, Pts(13, 35, 13, 27, 21, 21, 31, 21, 35, 27, 35, 35)); L(p, 26, 24, 42, 8, 4); };
        // point defence: a cut-cornered shield with a crosshair
        _icons["pd"] = p => { S(p, Pts(24, 5, 39, 11, 39, 27, 24, 43, 9, 27, 9, 11), true); L(p, 24, 15, 24, 33, 2.2f); L(p, 15, 24, 33, 24, 2.2f); };
        // science: a lens with a reading in it, the tracker's scope
        _icons["science"] = p => { S(p, Oct(20, 20, 13), true); L(p, 30, 30, 42, 42, 4); S(p, Pts(12, 22, 16, 22, 19, 15, 22, 25, 25, 19, 28, 19), false, 2); };
        // comms: a dish on a stand and two waves going out
        _icons["comms"] = p =>
        {
            S(p, Pts(6, 18, 26, 38, 12, 34), true, 2.6f); L(p, 16, 28, 26, 18, 2.4f); L(p, 14, 36, 10, 44); L(p, 4, 44, 20, 44);
            L(p, 30, 12, 36, 18, 2.4f); L(p, 32, 4, 44, 16, 2.4f);
        };
        // repair: a wrench, its jaw open
        _icons["repair"] = p => { L(p, 8, 40, 26, 22, 4.5f); S(p, Pts(24, 16, 30, 10, 38, 10, 32, 16, 32, 20, 38, 20, 38, 28, 30, 28, 24, 22), true, 2.6f); };
        // transport: a figure, its lower half breaking into bars as it beams
        _icons["transport"] = p =>
        {
            p.Fill(CutBox(19, 4, 29, 14, 3)); S(p, Pts(14, 30, 16, 19, 32, 19, 34, 30));
            foreach (var (y, w) in new[] { (33f, 16f), (37f, 12f), (41f, 8f), (45f, 5f) }) L(p, 24 - w, y, 24 + w, y, 2);
        };
        // fleet: three ships in formation, broad prows forward
        _icons["fleet"] = p =>
        {
            void Sh(float cx, float cy, float k) => S(p, Pts(cx - 6 * k, cy - 3 * k, cx, cy - 8 * k, cx + 6 * k, cy - 3 * k, cx + 5 * k, cy + 7 * k, cx - 5 * k, cy + 7 * k), true, 2.4f);
            Sh(24, 13, 1.1f); Sh(11, 33, 1); Sh(37, 33, 1);
        };
        // the ship from above: a squat armoured hull with a broad prow
        _icons["ship"] = p => { S(p, Pts(12, 12, 24, 5, 36, 12, 40, 28, 33, 43, 15, 43, 8, 28), true); L(p, 16, 20, 32, 20, 2); L(p, 14, 31, 34, 31, 2); };
        // shields: a ring of six plates round the hull
        _icons["shields"] = p =>
        {
            for (int i = 0; i < 6; i++) { float a = -P / 2 + i * P / 3; S(p, new[] { At(a - 0.38f, 19), At(a + 0.38f, 19) }, false, 3.4f); }
            p.Fill(CutBox(19, 18, 29, 30, 3));
        };
        // sensors: a scope with a sweep and two contacts
        _icons["sensors"] = p =>
        {
            S(p, Oct(24, 24, 19), true, 2.4f); L(p, 24, 24, 24, 6, 2.4f); L(p, 24, 24, 37, 11, 2.4f);
            p.Fill(CutBox(30, 27, 36, 33, 1.5f)); p.Fill(CutBox(12, 29, 17, 34, 1.5f));
        };
        // tractor: an emitter, a widening beam, the held hull boxed at its end
        _icons["tractor"] = p =>
        {
            p.Fill(Pts(3, 19, 12, 21, 12, 27, 3, 29)); L(p, 12, 21, 32, 12, 2.2f); L(p, 12, 27, 32, 36, 2.2f); S(p, CutBox(33, 15, 45, 33, 3), true, 2.6f);
        };
        // craft: a small hunter, swept wings
        _icons["craft"] = p => S(p, Pts(24, 4, 28, 22, 44, 30, 28, 32, 24, 44, 20, 32, 4, 30, 20, 22), true, 2.6f);
        // weapons: a beam from an emitter ending in a strike
        _icons["weapons"] = p =>
        {
            p.Fill(Pts(4, 44, 14, 41, 7, 34)); L(p, 10, 38, 30, 18, 3.6f); L(p, 30, 8, 30, 28, 2.4f); L(p, 20, 18, 40, 18, 2.4f);
            L(p, 23, 11, 37, 25, 2); L(p, 37, 11, 23, 25, 2);
        };
        // warp: a chevron trailing speed lines
        _icons["warp"] = p => { S(p, Pts(22, 10, 38, 24, 22, 38), false, 4); L(p, 4, 16, 18, 16); L(p, 2, 24, 22, 24); L(p, 4, 32, 18, 32); };
        // engines: a nozzle and its exhaust
        _icons["engines"] = p => { S(p, Pts(15, 4, 33, 4, 37, 22, 11, 22), true, 2.8f); S(p, Pts(14, 27, 34, 27, 24, 44), true, 2.4f); L(p, 24, 30, 24, 38, 2); };
        // reactor: an octagonal core with three vanes
        _icons["reactor"] = p =>
        {
            p.Fill(Oct(24, 24, 5));
            for (int i = 0; i < 3; i++) { float a = -P / 2 + i * 2 * P / 3; S(p, new[] { At(a, 9), At(a, 20), At(a + 0.6f, 20) }, false, 3); }
        };
        // map: a trail to an X
        _icons["map"] = p => { S(p, Pts(8, 40, 14, 32, 12, 24, 20, 18, 28, 20, 32, 13), false, 2.4f); L(p, 30, 4, 42, 16); L(p, 42, 4, 30, 16); };
        _icons["pause"] = p => { L(p, 17, 10, 17, 38, 6); L(p, 31, 10, 31, 38, 6); };
        _icons["menu"] = p => { foreach (float y in new[] { 13f, 24f, 35f }) L(p, 10, y, 38, y, 4); };
        // fire: brackets closing on a mark
        _icons["fire"] = p =>
        {
            foreach (float[] b in new[] { new float[] { 8, 16, 8, 8, 16, 8 }, new float[] { 32, 8, 40, 8, 40, 16 }, new float[] { 40, 32, 40, 40, 32, 40 }, new float[] { 16, 40, 8, 40, 8, 32 } })
                S(p, Pts(b), false, 3);
            p.Fill(CutBox(20, 20, 28, 28, 2.5f));
        };
        _icons["cargo"] = p => { foreach (var (cx, cy) in new[] { (15f, 15f), (33f, 15f), (15f, 33f), (33f, 33f) }) S(p, CutBox(cx - 8, cy - 8, cx + 8, cy + 8, 3), true, 2.6f); };
        // settings: a cog with square teeth round an octagonal bore
        _icons["settings"] = p =>
        {
            S(p, Oct(24, 24, 13), true, 3);
            for (int i = 0; i < 8; i++) { float a = i * P / 4; S(p, new[] { At(a, 15), At(a, 21) }, false, 4.5f); }
            S(p, Oct(24, 24, 5), true, 2.4f);
        };
    }
}
