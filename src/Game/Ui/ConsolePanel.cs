// ConsolePanel: the base of every console panel. A panel is a fixed rectangle that clips what it draws (CLAUDE.md
// section 7: panels are a fixed size, and their content never changes it), redrawn every frame from the battle's state,
// with its controls registered as they are drawn (Imm). This class holds the controls every panel shares, each drawn
// only from the kit's named pieces: the frame with its title notch and Expand key, keys, tabs, rails of icon tabs, rows
// of allocation boxes, bars and text.
using System;
using System.Collections.Generic;
using FleetCommand.ConsoleModel;
using FleetCommand.Game.Ui.Kit;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public abstract partial class ConsolePanel : Control
{
    protected ConsoleContext X = null!;
    protected readonly Imm Ui = new();
    /// <summary>The region this panel fills, which decides whether it can expand.</summary>
    public Region Region { get; private set; }
    private readonly Dictionary<string, float> _slots = new();

    protected IKit Kit => X.Kit;
    protected Palette C => X.Kit.C;
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
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        Ui.Begin();
        DrawPanel();
        Ui.End();
        X.Tips.Offer(this, Ui.CurrentTip(), Position);
    }

    /// <summary>Draws the panel's content in its own coordinates (0, 0 to Size).</summary>
    protected abstract void DrawPanel();

    public override void _GuiInput(InputEvent e)
    {
        if (Ui.Handle(e)) AcceptEvent();
        else if (e is InputEventMouseButton) AcceptEvent(); // a press on the console never reaches the view
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit) Ui.Leave();
    }

    // ---- Text ----------------------------------------------------------------------------------------------------
    protected void Text(string s, float x, float y, int size, Color col, HorizontalAlignment al = HorizontalAlignment.Left, float max = 0, int weight = 600, int spacing = 0) =>
        X.Fonts.Draw(this, s, x, y, size, col, al, max, weight, spacing);

    /// <summary>A label: upper case, Lilac, spaced.</summary>
    protected void Lbl(string s, float x, float y, float max = 0, Color? col = null, HorizontalAlignment al = HorizontalAlignment.Left, int size = 15) =>
        Text(s.ToUpperInvariant(), x, y, size, col ?? C.Lilac, al, max, 600, 1);

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
    /// <summary>A key: the kit's pill, idle, hover or pressed (pressed also when it is on); its label black.</summary>
    protected HitState Button(string id, Rect2 r, string label, Action? click, bool on = false, bool dis = false, bool center = true, Tip? tip = null)
    {
        HitState st = dis ? default : Ui.Reg(id, r, click == null ? null : _ => click(), null, tip);
        Kit.Piece(this, dis ? "button.idle" : st.Down || on ? "button.pressed" : st.Hot ? "button.hover" : "button.idle", r, dis ? 0.35f : 1);
        float h = r.Size.Y;
        int size = h >= 44 ? 22 : h >= 30 ? 17 : 15;
        float pad = h >= 44 ? 24 : 12;
        Color ink = dis ? new Color(0, 0, 0, 0.5f) : Colors.Black;
        if (center) Text(label.ToUpperInvariant(), r.Position.X + r.Size.X / 2, r.Position.Y + h / 2 + size * 0.36f, size, ink, HorizontalAlignment.Center, r.Size.X - 8, 600, 1);
        else Text(label.ToUpperInvariant(), r.End.X - pad, r.Position.Y + h / 2 + size * 0.36f, size, ink, HorizontalAlignment.Right, r.Size.X - pad - 12, 600, 1);
        return st;
    }

    /// <summary>A tab: one cap and a flat end, lit when chosen. With an icon it shows the icon and names itself on hover.</summary>
    protected void Tab(string id, Rect2 r, string label, Action click, bool on, string? icon = null, bool dis = false)
    {
        HitState st = dis ? default : Ui.Reg(id, r, _ => click(), null, icon != null ? new Tip(label) : null);
        Kit.Piece(this, on ? "tab.active" : "tab.idle", r, dis ? 0.35f : 1);
        if (!on && st.Hot) Kit.Piece(this, "tab.active", r, 0.22f);
        if (icon != null)
        {
            float s2 = Math.Min(30, r.Size.Y - 8);
            Kit.Icon(this, icon, new Rect2(r.Position + new Vector2((r.Size.X - s2) / 2 + 3, (r.Size.Y - s2) / 2), new Vector2(s2, s2)), on ? Colors.Black : C.Sky);
            return;
        }
        Text(label.ToUpperInvariant(), r.End.X - 8, r.Position.Y + r.Size.Y / 2 + 7, 19, on ? Colors.Black : C.Text, HorizontalAlignment.Right, r.Size.X - 22, 600, 1);
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
            Tab($"{idp}.{id}", new Rect2(x, y + i * 42, 48, 40), list[i].Label, () => set(id), cur == id, list[i].Icon);
        }
    }

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

    /// <summary>A bar: its label above, the track, the fill (red for a warning, green when asked). Returns the height used.</summary>
    protected float Bar(float x, float y, float w, string label, double frac, bool warn, bool ok = false)
    {
        float yy = y;
        if (label.Length > 0) { Lbl(label, x, y + 13, w); yy = y + 19; }
        Kit.Piece(this, "bar.track", new Rect2(x, yy, w, 16));
        float v = (float)Math.Clamp(frac, 0, 1);
        if (v > 0.005f) Kit.Piece(this, warn ? "bar.fill.red" : ok ? "bar.fill.accord" : "bar.fill", new Rect2(x + 3, yy + 3, Math.Max(10, MathF.Round((w - 6) * v)), 10));
        return yy + 16 - y;
    }

    /// <summary>The panel's frame, its title in a black notch that breaks the top arm, and the Expand key. Returns the content box.</summary>
    protected Rect2 Frame(string title, bool expandable = true) => Frame(new Rect2(Vector2.Zero, Size), title, expandable);

    /// <summary>A frame at a rectangle inside the panel (the menu sheet's).</summary>
    protected Rect2 Frame(Rect2 at, string title, bool expandable)
    {
        Kit.Piece(this, "frame.panel", at);
        var (t, r, b, l) = Kit.Margins("frame.panel");
        var content = new Rect2(at.Position.X + l, at.Position.Y + t, at.Size.X - r - l, at.Size.Y - t - b);
        string up = title.ToUpperInvariant();
        float tw = Math.Min(X.Fonts.Width(up, 22, 700, 1), content.Size.X - 160);
        float bw = expandable ? 21 : -10, x1 = content.End.X - 6, total = 8 + tw + 10 + bw + 8, x0 = x1 - total, y0 = at.Position.Y;
        DrawRect(new Rect2(x0, y0, total, 22), Colors.Black);
        Text(up, x0 + 8, y0 + 19, 22, C.Sky, HorizontalAlignment.Left, tw, 700, 1);
        if (expandable) ExpandKey($"{Region}.xp", new Vector2(x1 - 8 - bw / 2, y0 + 11), bw / 2, Big, () => X.Act.Expand(Region));
        return content;
    }

    /// <summary>The expand key: a small round key, arrows out to the corners; on an expanded panel they point in and it returns.</summary>
    private void ExpandKey(string id, Vector2 c, float r, bool inward, Action click)
    {
        HitState st = Ui.Reg(id, new Rect2(c - new Vector2(r + 4, r + 4), new Vector2(2 * r + 8, 2 * r + 8)), _ => click(), null, new Tip(inward ? "Return" : "Expand"));
        DrawCircle(c, r, st.Down ? C.Lilac : st.Hot ? C.Sky : C.Signal, true, -1, true);
        float w = Math.Max(2, r * 0.18f), a = r * 0.1f, bb = r * 0.6f, hd = r * 0.34f;
        foreach (int s in new[] { 1, -1 })
        {
            Vector2 p0 = c + new Vector2(s * a, -s * a), p1 = c + new Vector2(s * bb, -s * bb), h = inward ? p0 : p1;
            float d = inward ? 1 : -1;
            DrawLine(p0, p1, Colors.Black, w, true);
            DrawPolyline(new[] { h + new Vector2(s * d * hd, 0), h, h + new Vector2(0, -s * d * hd) }, Colors.Black, w, true);
        }
    }

    /// <summary>The colour of a status (battle-console design, "The five statuses").</summary>
    protected Color StatusColor(SystemStatus s) => SystemsDisplay.StatusColorOf(C, s);

    /// <summary>The colour of a shield facing's strength: three bands strong, two middling, one weak.</summary>
    protected Color ShieldColor(double frac) => frac > 0.01 ? SystemsDisplay.ShieldColorOf(C, frac) : C.Deep;

    protected static string Pct(double f) => $"{Math.Round(f * 100)} %";
}
