// The console's small panels: the name plate, the weapons panel, the radar row and the two strips (battle-console
// design sections 1 to 3). Each draws from the battle's state through Rules (one implementation of every number) and
// gives orders through Battle and GameActions. The stations panel and the ship panel are in their own files.
using System;
using System.Linq;
using FleetCommand.ConsoleModel;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

/// <summary>The captain's name plate, with the Fleet key that cycles the ship you command.</summary>
public sealed partial class PlatePanel : ConsolePanel
{
    protected override void DrawPanel()
    {
        Kit.Hero(this, "nameplate", new Rect2(0, 0, Size.X, 56));
        Text($"{Me.Name} · {Me.Captain}".ToUpperInvariant(), 108, 36, 28, C.Text, HorizontalAlignment.Left, 250, 700);
        Ship[] fleet = B.Fleet();
        int i = Array.IndexOf(fleet, Me);
        Button("fleet", new Rect2(Size.X - 90, 6, 86, 30), $"Fleet {i + 1}/{fleet.Length}", () => B.TakeCommand(fleet[(i + 1) % fleet.Length].Id),
            tip: new Tip("Take command of the next ship"));
    }
}

/// <summary>Fire, Multiple or Single, and the arming state of the selected mounts.</summary>
public sealed partial class WeaponsPanel : ConsolePanel
{
    protected override void DrawPanel()
    {
        Rect2 b = Frame("Weapons");
        int n = B.WouldFire().Length;
        Ship? t = B.TargetOf(Me);
        float half = (b.Size.X - 10) / 2;
        Button("fire", new Rect2(b.Position, new Vector2(half, 48)), n > 0 ? $"Fire {n}" : "Fire", () => B.Fire(), dis: t == null,
            tip: new Tip(t == null ? "Fire: lock a target first" : $"Fire the selected weapons at {t.Name}"));
        Button("volley", new Rect2(b.Position.X + half + 10, b.Position.Y, half, 48), Me.Volley ? "Multiple" : "Single", () => B.ToggleVolley(), Me.Volley,
            tip: new Tip(Me.Volley ? "Multiple: every ready weapon fires" : "Single: one weapon per press"));
        Mount[] sel = Me.Mounts.Where(m => m.Selected && m.Hp > 0).ToArray();
        string cur = sel.Length > 0 && sel.All(m => m.State == sel[0].State) ? sel[0].State.ToString().ToLowerInvariant() : "";
        Tabs("arm", new Rect2(b.Position.X, b.Position.Y + 56, b.Size.X, 40),
            new[] { ("offline", "Offline"), ("normal", "Normal"), ("overload", "Overload") }, cur,
            id => B.SetArming(Enum.Parse<Arming>(id, true)));
        if (Big)
        {
            float y = b.Position.Y + 130;
            Lbl("Mount", b.Position.X, y); Lbl("Weapon", b.Position.X + 70, y); Lbl("State", b.Position.X + 260, y); Lbl("Charge", b.Position.X + 380, y);
            Lbl("Status", b.Position.X + 520, y); Lbl("To hit", b.Position.X + 660, y);
            foreach (Mount m in Me.Mounts)
            {
                y += 34;
                Color sc = StatusColor(B.Rules.Status(m));
                Num(m.Label, b.Position.X, y, 0, m.Selected ? C.Sky : C.Lilac, HorizontalAlignment.Left, 20);
                Text(m.Weapon.Name.ToUpperInvariant(), b.Position.X + 70, y, 18, C.Text, HorizontalAlignment.Left, 180);
                Text(m.State.ToString().ToUpperInvariant(), b.Position.X + 260, y, 18, C.Text);
                double? cd = Rules.CooldownS(m);
                Num(cd is { } s ? Rules.FormatCooldown(s) + " S" : m.Charge >= 1 ? "READY" : "-", b.Position.X + 380, y, 0, null, HorizontalAlignment.Left, 20);
                Text(B.Rules.Status(m).ToString().ToUpperInvariant(), b.Position.X + 520, y, 18, sc);
                Num(t != null ? Pct(B.Rules.HitChance(Me, m, t)) : "-", b.Position.X + 660, y, 0, null, HorizontalAlignment.Left, 20);
            }
        }
    }
}

/// <summary>The radar dial of contacts round your heading, the contact count, Next target and the crest.</summary>
public sealed partial class RadarPanel : ConsolePanel
{
    private const double ReachKm = 25;

    protected override void DrawPanel()
    {
        float dial = Big ? Math.Min(Size.Y - 60, Size.X * 0.6f) : 120;
        var dr = new Rect2(8, Big ? 40 : 6, dial, dial);
        Kit.Hero(this, "dial", dr);
        Vector2 c = dr.GetCenter();
        float k = dial / 120f;
        DrawArc(c, 46 * k, -Mathf.Pi / 2, 0.4f, 24, C.Sky, 3 * k, true);
        Ship? t = B.TargetOf(Me);
        int count = 0;
        foreach (Ship o in B.Ships)
        {
            if (!o.Alive || o == Me) continue;
            double km = Rules.DistKm(Me, o);
            if (km > ReachKm) continue;
            if (o.Side != Me.Side) count++;
            double br = Rules.Bearing(Me, o.X, o.Z), rr = Math.Min(1, km / ReachKm) * 50 * k;
            var p = c + new Vector2((float)(Math.Sin(br) * rr), (float)(-Math.Cos(br) * rr));
            DrawCircle(p, 4 * k, o.Side == Me.Side ? C.Accord : C.Alert, true, -1, true);
            if (o == t) DrawArc(p, 8 * k, 0, Mathf.Tau, 20, C.Text, 2, true);
        }
        DrawCircle(c, 3 * k, C.Sky, true, -1, true);
        float x = dr.End.X + 18;
        float y = dr.Position.Y + 26;
        Pairs("rd", x, y, new (string, string, Color?)[] { ("Contacts", count.ToString(), count > 0 ? C.Alert : null) }, 22);
        Ship? near = B.EnemiesOf(Me).OrderBy(o => Rules.DistKm(Me, o)).FirstOrDefault();
        Pairs("rd.n", x, y + 30, new (string, string, Color?)[] { ("Nearest", near != null ? $"{Rules.DistKm(Me, near):0.0} km" : "-", null) }, 22);
        Button("rd.next", new Rect2(x, y + 46, 120, 30), "Next target", () => B.NextTarget(), tip: new Tip("Lock the next enemy, nearest first"));
        if (!Big) Kit.Hero(this, "crest", new Rect2(Size.X - 104, 18, 96, 96));
        else
        {
            Kit.Hero(this, "crest", new Rect2(Size.X - 160, 40, 140, 140));
            Button("rd.return", new Rect2(Size.X - 160, Size.Y - 60, 140, 40), "Return", () => X.Act.Return());
        }
    }
}

/// <summary>The top strip: own hull, the weapon capacitor in cells, the target's hull, the clock, Pause and Menu.</summary>
public sealed partial class TopStrip : ConsolePanel
{
    protected override void DrawPanel()
    {
        Kit.Piece(this, "strip.top", new Rect2(Vector2.Zero, Size));
        Ship s = Me;
        float cx = 96, cw = Size.X - 192, cy = (44 - 35) / 2f, fix = 70 + 100 + 80, gap = 22, u = (cw - fix - gap * 5) / 3.2f, xx = cx;
        double hull = s.Hull / s.Class.HullHp;
        Bar(xx, cy, u, "Hull integrity", hull, hull < 0.35); xx += u + gap;
        Lbl("Weapon capacitor", xx, cy + 13, u * 1.2f);
        CellRow("cap", new Rect2(xx, cy + 19, u * 1.2f, 16), 14, s.Capacitor / s.Class.CapacitorE * 14, null, null); xx += u * 1.2f + gap;
        Ship? t = B.TargetOf(s);
        Bar(xx, cy, u, t != null ? $"Target hull · {t.Name}" : "Target hull", t != null ? t.Hull / t.Class.HullHp : 0, true); xx += u + gap;
        int secs = (int)B.T;
        Num($"{secs / 60:00}:{secs % 60:00}", xx + 70, cy + 30, 0, null, HorizontalAlignment.Right); xx += 70 + gap;
        Button("pause", new Rect2(xx, cy + 4, 100, 30), X.State.Paused ? "Resume" : "Pause", X.Act.Pause, X.State.Paused, tip: new Tip("Pause (Space): orders still work")); xx += 100 + gap;
        Button("menu", new Rect2(xx, cy + 4, 80, 30), "Menu", X.Act.Menu, X.State.MenuOpen);
    }
}

/// <summary>The bottom strip: speed in boxes with a handle, Power over Systems as ticks, the battery, Next target, alert lamps.</summary>
public sealed partial class BottomStrip : ConsolePanel
{
    protected override void DrawPanel()
    {
        Kit.Piece(this, "strip.bottom", new Rect2(Vector2.Zero, Size));
        Ship s = Me;
        float cx = 96, cw = Size.X - 192, cy = 12, ch = 44, gap = 22, fix = 120 + 138, u = (cw - fix - gap * 4) / 3.45f, xx = cx;
        Lbl("Speed", xx, cy + 28);
        Num(Math.Round(s.V).ToString(), xx + 58, cy + 31, 46);
        SpeedSlider(xx + 112, cy, u * 1.5f - 112, ch); xx += u * 1.5f + gap;
        int n = s.Class.ReactorE, outN = (int)Math.Floor(B.Rules.Output(s) + 1e-6), used = B.Rules.Used(s);
        float tw = u * 1.4f - 68 - 60;
        Lbl("Power", xx, cy + 17, 0, null, HorizontalAlignment.Left, 12);
        CellRow("pt", new Rect2(xx + 68, cy + 6, tw, 12), n, outN, null, null, null, "cell.lit", 2);
        Num($"{B.Rules.Output(s):0.0} E", xx + 68 + tw + 58, cy + 17, 0, null, HorizontalAlignment.Right, 14);
        Lbl("Systems", xx, cy + 34, 0, null, HorizontalAlignment.Left, 12);
        CellRow("st", new Rect2(xx + 68, cy + 23, tw, 12), n, Math.Min(n, used), null, null, i => i >= outN, "cell.accord", 2);
        Num($"{used:0.0} E", xx + 68 + tw + 58, cy + 34, 0, used > B.Rules.Output(s) ? C.Alert : null, HorizontalAlignment.Right, 14);
        xx += u * 1.4f + gap;
        double bat = s.Battery / s.Class.BatteryE;
        Bar(xx, cy + 4, u * 0.55f, $"Battery {s.Battery:0.0} E", bat, bat < 0.25); xx += u * 0.55f + gap;
        Button("next", new Rect2(xx, cy + 7, 120, 30), "Next target", () => B.NextTarget(), tip: new Tip("Lock the next enemy (Tab)")); xx += 120 + gap;
        (AlertLevel L, string Hero, string Name)[] lamps = { (AlertLevel.Green, "lamp.alert.green", "Condition green"), (AlertLevel.Yellow, "lamp.alert.yellow", "Yellow alert"), (AlertLevel.Red, "lamp.alert.red", "Red alert") };
        for (int i = 0; i < 3; i++)
        {
            var (lv, hero, name) = lamps[i];
            var r = new Rect2(xx + i * 46, cy + 2, 40, 40);
            HitState st = Ui.Reg($"alert.{lv}", r, _ => B.SetAlert(lv), null, new Tip(name));
            Kit.Hero(this, hero, r, s.Alert == lv ? 1 : st.Hot ? 0.6f : 0.28f);
        }
    }

    // Speed: the ten engine boxes are the track, a handle on them is dragged (owner: "speed needs a slider handle").
    private void SpeedSlider(float x, float y, float w, float h)
    {
        Ship s = Me;
        double box = B.Tuning.SpeedPerBoxMS, top = box * 10;
        CellRow("spd", new Rect2(x, y + (h - 24) / 2, w, 24), 10, Math.Max(0, s.VOrder) / box, null, (int)Math.Floor(B.Rules.MaxSpeed(s) / box + 1e-6));
        Ui.Reg("spd", new Rect2(x - 8, y, w + 16, h), null, (p, _) => B.SetSpeed(Math.Round(Math.Clamp((p.X - x) / w, 0, 1) * top / 5) * 5),
            new Tip($"Speed: ordered {Math.Round(s.VOrder)} m/s, limit {Math.Round(B.Rules.MaxSpeed(s))} m/s"));
        float f = (float)Math.Clamp(s.VOrder / top, 0, 1);
        Kit.Piece(this, "button.pressed", new Rect2(x + f * w - 11, y + (h - 36) / 2, 22, 36));
    }
}

/// <summary>The in-battle menu, over the view: resume, restart the battle, quit.</summary>
public sealed partial class MenuCard : ConsolePanel
{
    protected override void DrawPanel()
    {
        Rect2 b = Frame("Menu", false);
        float y = b.Position.Y + 8, w = b.Size.X;
        Button("m.resume", new Rect2(b.Position.X, y, w, 44), "Resume", X.Act.Menu); y += 54;
        Button("m.restart", new Rect2(b.Position.X, y, w, 44), "Restart battle", X.Act.Restart); y += 54;
        Button("m.quit", new Rect2(b.Position.X, y, w, 44), "Quit", X.Act.Quit);
    }
}

/// <summary>The hover card, drawn above everything, above the control it names and kept on screen.</summary>
public sealed partial class TooltipLayer : Control
{
    private ConsoleContext _x = null!;
    public void Init(ConsoleContext ctx) { _x = ctx; MouseFilter = MouseFilterEnum.Ignore; Name = "Tooltips"; }
    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        Tip? tip = _x.Tips.Tip;
        if (tip == null) return;
        var c = _x.Kit.C;
        Fonts f = _x.Fonts;
        float w = f.Width(tip.Title.ToUpperInvariant(), 17, 600, 1) + 28;
        foreach (var row in tip.Rows) w = Math.Max(w, f.Width(row.Label.ToUpperInvariant(), 14, 600, 1) + f.Width(row.Value, 17) + 46);
        float h = 30 + tip.Rows.Count * 22 + (tip.Rows.Count > 0 ? 6 : 0);
        Rect2 a = _x.Tips.Anchor;
        float x = Math.Clamp(a.GetCenter().X - w / 2, 4, Size.X - w - 4), y = a.Position.Y - h - 6;
        if (y < 4) y = a.End.Y + 6;
        Shapes.RoundRect(this, new Rect2(x, y, w, h), Colors.Black, 10);
        Shapes.RoundRect(this, new Rect2(x, y, w, 26), c.Sky, 10, 10, 2, 2);
        f.Draw(this, tip.Title.ToUpperInvariant(), x + 12, y + 19, 17, Colors.Black, HorizontalAlignment.Left, w - 20, 600, 1);
        float yy = y + 48;
        foreach (var (l, v, col) in tip.Rows)
        {
            f.Draw(this, l.ToUpperInvariant(), x + 12, yy, 14, c.Lilac, HorizontalAlignment.Left, 0, 600, 1);
            f.Draw(this, v, x + w - 12, yy, 17, col ?? c.Sky, HorizontalAlignment.Right);
            yy += 22;
        }
    }
}
