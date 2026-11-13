// ConsolePanel: the base of every console panel. A panel is a fixed rectangle that clips what it draws (CLAUDE.md
// section 7: panels are a fixed size, and their content never changes it), redrawn every frame from the battle's state,
// with its controls registered as they are drawn (Imm). This class holds the controls every panel shares, each drawn
// only from the kit's named pieces and its style (the ink, type, padding and shapes that differ by skin): the frame with
// its title notch or placard and Expand key, keys, tabs, rails of icon tabs, rows of allocation boxes, bars and text.
// Panels redraw together at a capped rate (data/console/layout.json panel_redraw_hz), and a panel at once when it is touched (a press,
// the pointer moving over it or leaving it, a resize, shown or hidden), so its keys answer on the next frame: redrawing
// every panel every frame was half the battle's frame on a Raspberry Pi 5 (docs/playtests/pi/README.md). Godot keeps a
// canvas item's drawing until the next QueueRedraw, so a panel between redraws costs nothing, and its controls stay
// those of its last drawing.
// Docked in a skin's column (the Krai's one plate with the panels formed into it as wells), a panel draws the column's
// plate under its own rectangle, and a station, ship or weapons panel no frame of its own.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.ConsoleModel;
using FleetCommand.Game.Ui.Kit;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public abstract partial class ConsolePanel : Control, IControlSource
{
    protected ConsoleContext X = null!;
    protected readonly Imm Ui = new();
    /// <summary>The region this panel fills, which decides whether it can expand.</summary>
    public Region Region { get; private set; }
    private readonly Dictionary<string, float> _slots = new();
    /// <summary>Where the last frame's title box (or placard) starts, x in the panel: what sits left of the title on the
    /// frame's top edge (a station's officer slot) stops short of it.</summary>
    protected float TitleLeft { get; private set; }

    protected IKit Kit => X.Kit;
    protected Palette C => X.Kit.C;
    /// <summary>The skin's style: everything it draws differently besides its named pieces.</summary>
    protected SkinStyle Sk => X.Kit.Style;
    protected Battle B => X.Battle;
    protected Ship Me => X.Battle.Player;
    /// <summary>True while this panel is expanded to full screen.</summary>
    protected bool Big => X.State.Expanded == Region;

    public void Init(ConsoleContext ctx, Region region)
    {
        X = ctx;
        Region = region;
        Name = region.ToString();
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        Ui.Pressed = () => X.Sounds.Key(X.Kit.Style.KeySound);
        // the panel's text in a canvas item of its own over its shapes, one batch of glyphs (Fonts.UseLayer)
        _text = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_text, GetCanvasItem());
        Fonts.UseLayer(this, _text);
    }

    private Rid _text;

    public override void _ExitTree()
    {
        if (!_text.IsValid) return;
        Fonts.UseLayer(this, new Rid());
        RenderingServer.FreeRid(_text);
        _text = new Rid();
    }

    private bool _dirty = true;
    // One clock for every panel: they redraw on the same tick, so the console's cache (ConsoleCache) renders once a tick
    // and not on every frame some panel's turn came round.
    private static ulong _tickFrame;
    private static double _tickClock;
    private static bool _tick;

    /// <summary>Seconds between redraws of an untouched panel.</summary>
    private double RedrawEvery => 1.0 / (LowSpec.On ? X.Data.Layout.PanelRedrawHzLowSpec : X.Data.Layout.PanelRedrawHz);

    public override void _Process(double delta)
    {
        ulong frame = Engine.GetProcessFrames();
        if (frame != _tickFrame)
        {
            _tickFrame = frame;
            _tickClock += delta;
            _tick = _tickClock >= RedrawEvery;
            if (_tick) _tickClock %= RedrawEvery;
        }
        if (!_dirty && !_tick) return;
        _dirty = false;
        QueueRedraw();
        ConsoleCache.RenderFor(this);
    }

    /// <summary>Redraws the panel on the next frame (something it shows changed by a touch, not by the battle).</summary>
    public void Touch() => _dirty = true;

    /// <summary>This panel's controls of the last frame, in screen (canvas) coordinates.</summary>
    public IEnumerable<(string Id, Rect2 R, bool Live)> Controls() =>
        Visible ? Ui.Controls().Select(c => (c.Id, new Rect2(c.R.Position + GlobalPosition, c.R.Size), c.Live)) : Enumerable.Empty<(string, Rect2, bool)>();

    /// <summary>The rectangle the panel occupies, in screen coordinates.</summary>
    public Rect2 Screen => new(GlobalPosition, Size);

    public override void _Draw()
    {
        if (_text.IsValid) RenderingServer.CanvasItemClear(_text);
        DrawItem();
        Draw2D.Flush(this);   // the untextured shapes collected, sent as one draw
    }

    private void DrawItem()
    {
        Ui.Begin();
        if (Sk.Column && InColumn) Kit.Hero(this, "console.column", new Rect2(-Position, new Vector2((float)X.Data.Layout.ColumnW, (float)X.Data.Layout.ReferenceH)));
        DrawPanel();
        Ui.End();
        X.Tips.Offer(this, Ui.CurrentTip(), Position);
    }

    /// <summary>Draws the panel's content in its own coordinates (0, 0 to Size).</summary>
    protected abstract void DrawPanel();

    /// <summary>True while the panel sits docked in the left column, on the skin's column plate.</summary>
    protected virtual bool InColumn => !Big && Region is Region.Plate or Region.Station or Region.Ship or Region.Weapons or Region.Radar;

    public override void _GuiInput(InputEvent e)
    {
        _dirty = true;
        if (Ui.Handle(e)) AcceptEvent();
        else if (e is InputEventMouseButton) AcceptEvent(); // a press on the console never reaches the view
    }

    public override void _Notification(int what)
    {
        if (what is (int)NotificationMouseExit or (int)NotificationResized or (int)NotificationVisibilityChanged or (int)NotificationThemeChanged) _dirty = true;
        if (what == NotificationMouseExit) Ui.Leave();
        // a panel hidden under the pointer (the goals strip shut by its own X) draws no more, so it takes its hover card
        // away now; else the card ("Close") stays on screen with nothing under it (owner, 2026-11-07: "close button stuck
        // after I closed the goals panel")
        else if (what == NotificationVisibilityChanged && X != null && !IsVisibleInTree()) { Ui.Leave(); X.Tips.Offer(this, null, Position); }
    }

    // ---- Text ----------------------------------------------------------------------------------------------------
    protected void Text(string s, float x, float y, int size, Color col, HorizontalAlignment al = HorizontalAlignment.Left, float max = 0, int weight = 600, int spacing = 0) =>
        X.Fonts.Draw(this, s, x, y, size, col, al, max, weight, spacing);

    /// <summary>A label: upper case, Lilac, spaced; in a skin with label placards each label (not each value) sits on
    /// one, its pip in the left margin, the letters on the metal.</summary>
    protected void Lbl(string s, float x, float y, float max = 0, Color? col = null, HorizontalAlignment al = HorizontalAlignment.Left, int size = 15)
    {
        string str = s.ToUpperInvariant();
        Color c = col ?? C.Lilac;
        if (Sk.LabelPlacard && c == C.Lilac && str.Length > 0)
        {
            float w = X.Fonts.Width(str, size, 600, 1);
            if (max > 0) w = Math.Min(w, max);
            float x0 = al == HorizontalAlignment.Center ? x - w / 2 : al == HorizontalAlignment.Right ? x - w : x;
            Kit.Piece(this, "plate.label", new Rect2(x0 - 13, y - size * 0.72f - 3, w + 19, MathF.Round(size * 0.82f + 5)));
        }
        Text(str, x, y, size, c, al, max, 600, 1);
    }

    /// <summary>A number, Sky by default.</summary>
    protected void Num(string s, float x, float y, float max = 0, Color? col = null, HorizontalAlignment al = HorizontalAlignment.Left, int size = 24) =>
        Text(s, x, y, size, col ?? C.Sky, al, max);

    /// <summary>Label and value pairs in a line; each value's slot only grows, so a changing number never moves what follows.</summary>
    protected float Pairs(string id, float x, float y, (string L, string V, Color? Col)[] items, int size = 24)
    {
        float cx = x;
        for (int i = 0; i < items.Length; i++)
        {
            var (l, v, col) = items[i];
            if (l.Length > 0) { Lbl(l, cx, y); cx += X.Fonts.Width(l.ToUpperInvariant(), 15, 600, 1) + 7; }
            string k = id + "." + i;
            float vw = Math.Max(_slots.GetValueOrDefault(k), X.Fonts.Width(v, size));
            _slots[k] = vw;
            Num(v, cx, y, 0, col, HorizontalAlignment.Left, size);
            cx += vw + 16;
        }
        return cx - x;
    }

    // ---- Controls ------------------------------------------------------------------------------------------------
    /// <summary>A key: the kit's key, idle, hover or pressed (pressed also when it is on); its label in the skin's key
    /// ink. A skin whose keys end in a point (KeyCenter) centres the label in the open field between the key's caps,
    /// shrinking it to fit (down to 11 px) before it is trimmed (the Analog Panel Bible's rule T).</summary>
    protected HitState Button(string id, Rect2 r, string label, Action? click, bool on = false, bool dis = false, bool center = true, Tip? tip = null,
        string? shortLabel = null)
    {
        HitState st = dis ? default : Ui.Reg(id, r, click == null ? null : _ => click(), null, tip);
        string kst = dis ? "idle" : st.Down || on ? "pressed" : st.Hot ? "hover" : "idle";
        Kit.Piece(this, "button." + kst, r, dis ? 0.35f : 1);
        KeyLabel(this, Kit, X.Fonts, r, kst, label, dis, center, shortLabel);
        return st;
    }

    /// <summary>A key's label in the skin's key ink: one implementation for every key the console draws, the panels' and
    /// the view's (owner, 2026-11-06: the view's Target key read "ARGET" in the Kelvane skin, its label under the knob).
    /// A skin whose keys end in a point (KeyCenter) centres the label in the open field between the key's caps or its
    /// measured KeyField, shrinking it to fit (down to 11 px) before it is trimmed (the Analog Panel Bible's rule T).</summary>
    public static void KeyLabel(CanvasItem ci, IKit kit, Fonts fonts, Rect2 r, string kst, string label, bool dis = false, bool center = true,
        string? shortLabel = null)
    {
        SkinStyle sk = kit.Style;
        float h = r.Size.Y;
        int size = h >= 44 ? 22 : h >= 30 ? 17 : 15;
        float pad = h >= 44 ? 24 : 12;
        Color k = sk.KeyInk.For(kst), ink = dis ? new Color(k, k.A * 0.5f) : k;
        string lab = label.ToUpperInvariant();
        if (sk.KeyCenter)
        {
            PieceSpec p = kit.Spec("button." + kst);
            float sc = Math.Min(1, h / p.Size.Y);
            var (cl, cr) = sk.KeyField is { } f ? (f.L * sc, f.R * sc) : (p.M.L * sc, p.M.R * sc);
            float room = r.Size.X - cl - cr - 8;
            var (fl, fs, sp) = FitLabel(fonts, lab, shortLabel, size, room);
            fonts.Draw(ci, fl, r.Position.X + cl + (r.Size.X - cl - cr) / 2, r.Position.Y + h / 2 + fs * 0.36f, fs, ink, HorizontalAlignment.Center, room, 600, sp);
        }
        else if (center) fonts.Draw(ci, lab, r.Position.X + r.Size.X / 2, r.Position.Y + h / 2 + size * 0.36f, size, ink, HorizontalAlignment.Center, r.Size.X - 8, 600, 1);
        else fonts.Draw(ci, lab, r.End.X - pad, r.Position.Y + h / 2 + size * 0.36f, size, ink, HorizontalAlignment.Right, r.Size.X - pad - 12, 600, 1);
    }

    /// <summary>A label fitted to <paramref name="room"/> pixels, the mockup's rule T as drawn: at its size and 1 px
    /// spacing if it fits; else closed up to no spacing and shrunk a pixel at a time, measured as drawn (a pixel face does
    /// not scale evenly), down to 11 px; else the key's short form, fitted the same way. Only what still cannot fit at
    /// 11 px is trimmed.</summary>
    public static (string Label, int Size, int Spacing) FitLabel(Fonts fonts, string lab, string? shortLabel, int size, float room)
    {
        if (fonts.Width(lab, size, 600, 1) <= room) return (lab, size, 1);
        int fs = Shrink(fonts, lab, size, room);
        if (shortLabel != null && fonts.Width(lab, fs, 600, 0) > room)
        {
            string sl = shortLabel.ToUpperInvariant();
            if (fonts.Width(sl, size, 600, 1) <= room) return (sl, size, 1);
            return (sl, Shrink(fonts, sl, size, room), 0);
        }
        return (lab, fs, 0);
    }

    private static int Shrink(Fonts fonts, string lab, int size, float room)
    {
        int fs = size;
        while (fs > 11 && fonts.Width(lab, fs, 600, 0) > room) fs--;
        return fs;
    }

    /// <summary>A key's end caps at a drawn height: its nine-slice margins, shrunk with its height as the kit draws them.</summary>
    protected (float L, float R) Caps(string piece, float h)
    {
        PieceSpec p = Kit.Spec(piece);
        float k = Math.Min(1, h / p.Size.Y);
        return (p.M.L * k, p.M.R * k);
    }

    /// <summary>What a label keeps clear of at each end of a key or tab drawn this high: the skin's measured label field
    /// when it gives one (shrunk with the height as the piece is), else the piece's caps (the mockup's keyField, tabField
    /// and keyCaps).</summary>
    private (float L, float R) Field((float L, float R)? field, string piece, float h)
    {
        if (field is not { } f) return Caps(piece, h);
        float k = Math.Min(1, h / Kit.Spec(piece).Size.Y);
        return (f.L * k, f.R * k);
    }

    /// <summary>A tab: one cap and a flat end, lit when chosen. With an icon it shows the icon and names itself on hover.</summary>
    protected void Tab(string id, Rect2 r, string label, Action click, bool on, string? icon = null, bool dis = false)
    {
        HitState st = dis ? default : Ui.Reg(id, r, _ => click(), null, icon != null ? new Tip(label) : null);
        // an icon tab in a skin whose tab piece is a long tube or window (the Ssarith's, the Kelvane's): its square key
        // instead, the icon inside the glass; dark at rest, lit on hover, engaged when chosen (the mockup's iconKeys)
        if (icon != null && Sk.IconKeys)
        {
            float s2 = Math.Min(r.Size.X, r.Size.Y), ic = s2 * 0.56f;
            var key = new Rect2(r.Position + (r.Size - new Vector2(s2, s2)) / 2, new Vector2(s2, s2));
            Kit.Hero(this, on ? "key.on" : st.Hot ? "key.hover" : "key.idle", key, dis ? 0.35f : 1);
            Kit.Icon(this, icon, new Rect2(key.Position + new Vector2((s2 - ic) / 2, (s2 - ic) / 2), new Vector2(ic, ic)), on ? Sk.R.Ink : Sk.TabIcon);
            return;
        }
        Kit.Piece(this, on ? "tab.active" : "tab.idle", r, dis ? 0.35f : 1);
        if (!on && st.Hot) Kit.Piece(this, "tab.active", r, 0.22f);
        if (icon != null)
        {
            float s2 = Math.Min(30, r.Size.Y - 8);
            Kit.Icon(this, icon, new Rect2(r.Position + new Vector2((r.Size.X - s2) / 2 + 3, (r.Size.Y - s2) / 2), new Vector2(s2, s2)), on ? Sk.KeyInk.TabOn : Sk.TabIcon);
            return;
        }
        string lab = label.ToUpperInvariant();
        Color ink = on ? Sk.KeyInk.TabOn : Sk.KeyInk.TabOff;
        if (Sk.KeyCenter)
        {
            // centred in the open field between the tab's end caps (or the skin's measured field), shrinking to fit
            // (down to 11 px) before it is trimmed (rule T)
            var (tl, tr) = Field(Sk.TabField, on ? "tab.active" : "tab.idle", r.Size.Y);
            float room = r.Size.X - Math.Max(28, tl + tr + 4);
            var (fl, fs, sp) = FitLabel(X.Fonts, lab, null, 19, room);
            float cx = tl + tr > 20 ? r.Position.X + tl + (r.Size.X - tl - tr) / 2 : r.Position.X + r.Size.X / 2;
            Text(fl, cx, r.Position.Y + r.Size.Y / 2 + fs * 0.37f, fs, ink, HorizontalAlignment.Center, room, 600, sp);
        }
        else
        {
            // right-aligned in the open end of the tab, fitted the same way before it is trimmed
            var (fl, fs, sp) = FitLabel(X.Fonts, lab, null, 19, r.Size.X - 22);
            Text(fl, r.End.X - 8, r.Position.Y + r.Size.Y / 2 + fs * 0.37f, fs, ink, HorizontalAlignment.Right, r.Size.X - 22, 600, sp);
        }
    }

    /// <summary>A row of tabs across a width.</summary>
    protected void Tabs(string idp, Rect2 r, (string Id, string Label)[] list, string cur, Action<string> set, Func<string, bool>? dis = null)
    {
        int n = list.Length; float gap = 4, tw = (r.Size.X - gap * (n - 1)) / n;
        for (int i = 0; i < n; i++)
        {
            string id = list[i].Id;
            Tab($"{idp}.{id}", new Rect2(r.Position.X + i * (tw + gap), r.Position.Y, tw, r.Size.Y), list[i].Label, () => set(id), cur == id, null, dis != null && dis(id));
        }
    }

    /// <summary>A column of icon tabs down a panel's left side (owner: "group icons into a narrow column").</summary>
    protected void Rail(string idp, float x, float y, (string Id, string Label, string Icon)[] list, string cur, Action<string> set)
    {
        for (int i = 0; i < list.Length; i++)
        {
            string id = list[i].Id;
            // in co-op a tab of a role this player does not hold is shown, disabled (ConsoleContext.Opens)
            Tab($"{idp}.{id}", new Rect2(x, y + i * 42, 48, 40), list[i].Label, () => set(id), cur == id, list[i].Icon, !X.Opens($"{idp}.{id}"));
        }
    }

    /// <summary>The open tab, or in co-op the first this player opens if theirs is shut to them (null if none is open).</summary>
    protected string? OpenOr(string idp, (string Id, string Label, string Icon)[] list, string cur) =>
        X.Opens($"{idp}.{cur}") ? cur : list.Select(t => t.Id).FirstOrDefault(id => X.Opens($"{idp}.{id}"));

    /// <summary>A row of discrete boxes (CLAUDE.md section 7): value lit, those at or past cap locked, a tap sets the count.</summary>
    protected void CellRow(string idp, Rect2 r, int n, double value, Action<int>? set, int? cap, Func<int, bool>? warn = null, string litPiece = "cell.lit", float gap = 3)
    {
        float cw = (r.Size.X - gap * (n - 1)) / n;
        int k = cap ?? n;
        for (int i = 0; i < n; i++)
        {
            var cr = new Rect2(r.Position.X + i * (cw + gap), r.Position.Y, cw, r.Size.Y);
            bool lit = i < value, locked = !lit && i >= k;
            int idx = i;
            if (set != null && !locked) Ui.Reg($"{idp}.{i}", cr, _ => set(idx + 1 == (int)Math.Round(value) ? idx : idx + 1));
            Kit.Piece(this, lit ? (warn != null && warn(i) ? "cell.warn" : litPiece) : locked ? "cell.locked" : "cell.idle", cr);
        }
    }

    /// <summary>A bar: its label above, the track, the fill (red for a warning, green when asked); in a skin with symbol
    /// bars, a row of the skin's symbol in the track's groove, as many whole cells as fit, the first ones lit. Returns
    /// the height used.</summary>
    protected float Bar(float x, float y, float w, string label, double frac, bool warn, bool ok = false)
    {
        float yy = y;
        if (label.Length > 0) { Lbl(label, x, y + 13, w); yy = y + 19; }
        Kit.Piece(this, "bar.track", new Rect2(x, yy, w, 16));
        float v = (float)Math.Clamp(frac, 0, 1);
        if (Sk.SymbolBars)
        {
            PieceSpec tr = Kit.Spec("bar.track"), on = Kit.Spec("bar.cell.on");
            const float ch = 10;
            float ix = tr.Inset.X * 16 / tr.Size.Y, cw = on.Size.X * ch / on.Size.Y, gp = on.Gap * ch / on.Size.Y, gw = w - 2 * ix;
            int n = Math.Max(1, (int)MathF.Floor((gw + gp) / (cw + gp))), lit = (int)MathF.Round(n * v);
            float x0 = x + ix + (gw - (n * cw + (n - 1) * gp)) / 2;
            for (int i = 0; i < n; i++)
                Kit.Piece(this, i < lit ? (warn ? "bar.cell.warn" : ok ? "bar.cell.ok" : "bar.cell.on") : "bar.cell.off", new Rect2(x0 + i * (cw + gp), yy + 3, cw, ch));
        }
        else if (v > 0.005f) Kit.Piece(this, warn ? "bar.fill.red" : ok ? "bar.fill.accord" : "bar.fill", new Rect2(x + 3, yy + 3, Math.Max(10, MathF.Round((w - 6) * v)), 10));
        return yy + 16 - y;
    }

    /// <summary>The panel's frame, its title in a dark notch that breaks the top arm (or on the skin's title placard),
    /// and the Expand key. Docked in a skin's column the panel is a well of the column's plate and draws no frame of its
    /// own. Returns the content box, inside the skin's padding.</summary>
    protected Rect2 Frame(string title, bool expandable = true) => Frame(new Rect2(Vector2.Zero, Size), title, expandable);

    /// <summary>A frame at a rectangle inside the panel (the menu sheet's).</summary>
    protected Rect2 Frame(Rect2 at, string title, bool expandable)
    {
        if (!(Sk.Column && InColumn)) Kit.Piece(this, "frame.panel", at);
        Pad p = Sk.Pad;
        var content = new Rect2(at.Position.X + p.L, at.Position.Y + p.T, at.Size.X - p.R - p.L, at.Size.Y - p.T - p.B);
        string up = title.ToUpperInvariant();
        float tw = Math.Min(X.Fonts.Width(up, 22, 700, 1), content.Size.X - 160);
        // the title box ends inside the frame's corner (owner, 2026-11-06: "trim the box a bit so its corner matches the blue
        // border"): a panel without the expand key keeps it clear of the corner cap as one with the key does
        float bw = expandable ? 21 : -10, x1 = content.End.X - (expandable ? 6 : 20), total = 8 + tw + 10 + bw + 8, x0 = x1 - total, y0 = at.Position.Y;
        TitleLeft = x0 - (Sk.TitlePlacard ? 18 : 0);
        if (Sk.TitlePlacard) Kit.Piece(this, "plate.title", new Rect2(x0 - 18, y0 - 1, total + 22, 24));
        else Draw2D.Rect(this, new Rect2(x0, y0, total, 22), Sk.R.Plate);
        Text(up, x0 + 8, y0 + 19 - Sk.TitleDy, 22, Sk.R.Title, HorizontalAlignment.Left, tw, 700, 1);
        if (expandable) ExpandKey($"{Region}.xp", new Vector2(x1 - 8 - bw / 2, y0 + 11), bw / 2, Big, () => X.Act.Expand(Region));
        return content;
    }

    /// <summary>The expand key: a small knob in the skin's shape, arrows out to the corners in the ink that reads on it;
    /// on an expanded panel they point in and it returns.</summary>
    private void ExpandKey(string id, Vector2 c, float r, bool inward, Action click)
    {
        // the knob's reach, kept inside the panel: the panel is clipped, so a click past its edge could never land here
        // (the skin parity check measures every control inside what draws it)
        var reach = new Rect2(c - new Vector2(r + 4, r + 4), new Vector2(2 * r + 8, 2 * r + 8)).Intersection(new Rect2(Vector2.Zero, Size));
        HitState st = Ui.Reg(id, reach, _ => click(), null, new Tip(inward ? "Return" : "Expand"));
        Color fill = st.Down ? C.Lilac : st.Hot ? C.Sky : C.Signal, ink = Sk.InkOn(fill);
        Kit.Knob(this, c, r, fill);
        float w = Math.Max(2, r * 0.18f), a = r * 0.1f, bb = r * 0.6f, hd = r * 0.34f;
        foreach (int s in new[] { 1, -1 })
        {
            Vector2 p0 = c + new Vector2(s * a, -s * a), p1 = c + new Vector2(s * bb, -s * bb), h = inward ? p0 : p1;
            float d = inward ? 1 : -1;
            Draw2D.Line(this, p0, p1, ink, w, true);
            Draw2D.Polyline(this, new[] { h + new Vector2(s * d * hd, 0), h, h + new Vector2(0, -s * d * hd) }, ink, w, true);
        }
    }

    // the meters' needles as last drawn: the ship they read, when, and where they pointed (the mockup's NEEDLE)
    private readonly Dictionary<string, (string Ship, ulong Ms, float V)> _needles = new();

    /// <summary>A strip's needle meter, in a skin whose strip art carries a printed scale (SkinStyle.Meter; the mockup's
    /// stripMeter): the needle swings toward the reading and settles as a damped needle does, and names its reading on
    /// hover. A display only: the value is the one the strip's bar and number show. A needle not drawn for a quarter of a
    /// second, or on another ship, is set straight to its reading (after a pause, a scene change or a hop the gauge shows
    /// the reading, not a swing from the last one).</summary>
    protected void StripMeter(string id, double frac, string tip)
    {
        if (Sk.Meter is not { } m || Sk.Needle == null) return;
        ulong now = Time.GetTicksMsec();
        float v = (float)Math.Clamp(double.IsFinite(frac) ? frac : 0, 0, 1);
        if (_needles.TryGetValue(id, out var n) && n.Ship == Me.Id)
        {
            float dt = (now - n.Ms) / 1000f;
            if (dt <= 0.25f) v = n.V + (v - n.V) * Math.Min(1, dt * m.Settle);
        }
        _needles[id] = (Me.Id, now, v);
        float ky = Size.Y / m.H, r = m.R * Math.Min(1, ky);
        var pivot = new Vector2(m.Cx, m.Cy * ky);
        Sk.Needle(this, pivot, r, m.A0 + (m.A1 - m.A0) * v);
        Ui.Reg($"meter.{id}", new Rect2(pivot.X - m.R - 6, pivot.Y - m.R - 6, 2 * m.R + 12, m.R + 10), null, null, new Tip(tip));
    }

    /// <summary>The colour of a status (battle-console design, "The five statuses").</summary>
    protected Color StatusColor(SystemStatus s) => SystemsDisplay.StatusColorOf(C, s);

    /// <summary>The colour of a shield facing's strength: three bands strong, two middling, one weak.</summary>
    protected Color ShieldColor(double frac) => frac > 0.01 ? SystemsDisplay.ShieldColorOf(C, frac) : C.Deep;

    protected static string Pct(double f) => $"{Math.Round(f * 100)} %";
}
