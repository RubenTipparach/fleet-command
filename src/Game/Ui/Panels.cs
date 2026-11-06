// The console's small panels: the name plate, the weapons panel, the radar row and the two strips (battle-console
// design sections 1 to 3). Each draws from the battle's state through Rules (one implementation of every number) and
// gives orders through Battle and GameActions. The stations panel and the ship panel are in their own files.
using System;
using System.Linq;
using FleetCommand.ConsoleModel;
using FleetCommand.Data;
using FleetCommand.Game.Ui.Kit;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

/// <summary>The captain's name plate, with the Fleet key that cycles the ship you command.</summary>
public sealed partial class PlatePanel : ConsolePanel
{
    protected override void DrawPanel()
    {
        Kit.Hero(this, "nameplate", new Rect2(0, 0, Size.X, 56));
        // the plate's live field, in the skin's own face, clear of its mark (the Krai's war-shield triangle)
        PlateText pt = Sk.Plate;
        X.Fonts.Draw(this, $"{Me.Name} · {Me.Captain}".ToUpperInvariant(), pt.X, pt.Y, pt.Face, pt.Col, HorizontalAlignment.Left, pt.Max);
        Ship[] fleet = B.Fleet();
        int i = Array.IndexOf(fleet, Me);
        Button("fleet", new Rect2(Size.X - 90, 6, 86, 30), $"Fleet {i + 1}/{fleet.Length}", () => B.TakeCommand(fleet[(i + 1) % fleet.Length].Id),
            tip: new Tip("Take command of the next ship"));
    }
}

/// <summary>Fire, Multiple or Single, and the arming state of the selected mounts. Expanded (owner, 2026-11-02: "show
/// weapons as they are on the ship in the expanded menu, then when you hover over them and click on them, detail menu
/// appears on the lower half. Also add a fire button underneath each weapon"): groups to select, the systems display
/// with a Fire key under each mount, and the picked mount's details.</summary>
public sealed partial class WeaponsPanel : ConsolePanel
{
    private int _detail;
    private static readonly (string Id, string Label)[] ArmModes = { ("offline", "Offline"), ("normal", "Normal"), ("overload", "Overload"), ("proximity", "Proximity") };

    protected override void DrawPanel()
    {
        Rect2 b = Frame("Weapons");
        Ship s = Me;
        Ship? t = B.TargetOf(s);
        Mount[] sel = s.Mounts.Where(m => m.Selected && !m.Weapon.IsPointDefence).ToArray();
        string cur = sel.Length > 0 && sel.All(m => m.State == sel[0].State) ? sel[0].State.ToString().ToLowerInvariant() : "";
        float w1 = (b.Size.X - 6) * 0.62f, x0 = b.Position.X, y0 = b.Position.Y;
        Button("fire", new Rect2(x0, y0, w1, 48), $"Fire · {B.WouldFire().Length}", () => B.Fire(), center: false,
            tip: new Tip(t == null ? "Fire: lock a target first" : $"Fire the selected weapons at {t.Name} (F)"));
        Button("volley", new Rect2(x0 + w1 + 6, y0, b.Size.X - w1 - 6, 48), s.Volley ? "Multiple" : "Single", () => B.ToggleVolley(), s.Volley, center: false,
            tip: new Tip(s.Volley ? "Multiple: every ready weapon fires (M)" : "Single: one weapon per press (M)"));
        // every arming mode; one no selected weapon has is dimmed (weapon-arming design)
        Tabs("arm", new Rect2(x0, y0 + 54, b.Size.X, 40), ArmModes, cur,
            id => B.SetArming(Enum.Parse<Arming>(id, true)), id => !sel.Any(m => Rules.CanArm(m.Weapon, Enum.Parse<Arming>(id, true))));
        if (!Big) return;
        float y = y0 + 106;
        Lbl("Select", x0, y + 13);
        y += 20;
        Tabs("grp", new Rect2(x0, y, b.Size.X, 40), new[] { ("all", "All"), ("lance", "Lances"), ("torpedo", "Torpedoes") }, "", id => B.SelectGroup(id == "all" ? null : id));
        y += 52;
        var top = new Rect2(x0, y, b.Size.X, (b.End.Y - y) * 0.55f);
        foreach (var (m, r) in SystemsDisplay.Draw(this, Ui, X, s, top, true, "wx", true, mm => _detail = mm.Index))
        {
            if (m.Weapon.IsPointDefence) continue;
            int i = m.Index;
            Button($"wx.f.{i}", new Rect2(r.GetCenter().X - 24, r.End.Y + 3, 48, 20), "Fire", () => B.FireOne(i), dis: !B.CanFire(m));
        }
        y = top.End.Y + 8;
        Kit.Piece(this, "divider", new Rect2(x0, y, b.Size.X, 10));
        y += 14;
        Detail(new Rect2(x0, y, b.Size.X, b.End.Y - y), s, t);
    }

    // The picked mount's details: every figure from the rules the shot uses.
    private void Detail(Rect2 r, Ship s, Ship? t)
    {
        if (s.Mounts.Count == 0) { Lbl("No weapons fitted", r.Position.X, r.Position.Y + 26, 0, null, HorizontalAlignment.Left, 18); return; }
        Mount m = s.Mounts[Math.Clamp(_detail, 0, s.Mounts.Count - 1)];
        WeaponMode w = Rules.Mode(m);
        SystemStatus st = B.Rules.Status(s, m);
        double? cool = Rules.CooldownS(m);
        float x0 = r.Position.X, y = r.Position.Y, cw = r.Size.X / 2 - 12;
        Text($"{m.Label} · {m.Weapon.Name}".ToUpperInvariant(), x0, y + 26, 26, C.Sky, HorizontalAlignment.Left, cw, 700, 1);
        Pairs("wd.a", x0, y + 62, new (string, string, Color?)[] { ("Status", st.ToString(), StatusColor(st)), ("Health", Pct(Math.Clamp(m.Hp, 0, 1)), null) }, 20);
        Pairs("wd.b", x0, y + 92, new (string, string, Color?)[] { ("Arc", m.Def.ArcWidthDeg >= 360 ? "All round" : $"{m.Def.ArcCentreDeg:0}° ± {m.Def.ArcWidthDeg / 2:0}°", null), ("Range", $"{w.RangeKm:0.0} km", null), ("Damage", w.Damage.ToString("0"), null) }, 20);
        Pairs("wd.c", x0, y + 122, new (string, string, Color?)[] { ("Charge", m.Hp <= 0 || m.State == Arming.Offline ? "--" : Pct(m.Charge), null),
            ("Ready in", cool is { } c ? $"{Rules.FormatCooldown(c)} s" : Rules.Ready(m) ? "Ready" : "--", null), ("Cost", $"{w.CostE:0.#} E", null) }, 20);
        float rx = x0 + r.Size.X / 2 + 12, rw = r.Size.X / 2 - 12;
        if (!m.Weapon.IsPointDefence)
            Tabs("wd.arm", new Rect2(rx, y + 6, rw, 36), ArmModes, m.State.ToString().ToLowerInvariant(),
                id => B.ArmOne(m.Index, Enum.Parse<Arming>(id, true)), id => !Rules.CanArm(m.Weapon, Enum.Parse<Arming>(id, true)));
        if (t != null)
        {
            bool arc = Rules.InArc(s, m, t), rng = Rules.InRange(s, m, t);
            Pairs("wd.t", rx, y + 62, new (string, string, Color?)[] { ("Target", t.Name, null), ("To hit", Pct(B.Rules.HitChance(s, m, t)), null),
                ("In arc", arc ? "Yes" : "No", arc ? C.Accord : C.Alert), ("In range", rng ? "Yes" : "No", rng ? C.Accord : C.Alert) }, 20);
        }
        else Lbl("No target", rx, y + 62);
        if (!m.Weapon.IsPointDefence)
        {
            int i = m.Index;
            Tab("wd.sel", new Rect2(rx, y + 84, rw * 0.45f, 40), m.Selected ? "In the volley" : "Not in volley", () => B.ToggleMount(i), m.Selected);
            Button("wd.fire", new Rect2(rx + rw * 0.45f + 8, y + 84, rw * 0.55f - 8, 40), "Fire", () => B.FireOne(i), dis: !B.CanFire(m));
        }
    }
}

/// <summary>The radar row: the dial of contacts round your heading (a click opens the tactical map), the hostile count, Map
/// and Next target, and the crest. Expanded, it is the tactical map (RadarPanel.Map.cs).</summary>
public sealed partial class RadarPanel : ConsolePanel
{
    private const double ScaleKm = 15;

    protected override void DrawPanel()
    {
        if (Big) { Map(); return; }
        float x = 0, y = 0;
        var dr = new Rect2(x, y + 6, 120, 120);
        Kit.Hero(this, "dial", dr);
        Ui.Reg("radar", dr, _ => X.Act.Expand(Region), null, new Tip("Tactical map"));
        Vector2 c = dr.GetCenter();
        float rr = 120 * 0.4f;
        foreach (float f in new[] { 0.5f, 1f }) DrawArc(c, rr * f, 0, Mathf.Tau, 48, C.Deep, 1.5f, true);
        foreach (Ship o in B.Ships)
        {
            if (!o.Alive || o == Me) continue;
            double km = Rules.DistKm(Me, o), br = Rules.Bearing(Me, o.X, o.Z), r = Math.Min(1.08, km / ScaleKm) * rr;
            var p = c + new Vector2((float)(Math.Sin(br) * r), (float)(-Math.Cos(br) * r));
            DrawCircle(p, 120 * 0.035f, Aiming.ContactColor(C, Me, o), true, -1, true);
            if (o.Id == Me.Target) DrawArc(p, 120 * 0.07f, 0, Mathf.Tau, 20, C.Sky, 2, true);
        }
        DrawCircle(c, 120 * 0.03f, C.Sky, true, -1, true);
        float bx = x + 128, bw = Size.X - 128 - 80;
        Kit.Piece(this, "frame.inset", new Rect2(bx, y + 6, bw, 120));
        Pairs("rr", bx + 14, y + 52, new (string, string, Color?)[] { ("", B.EnemiesOf(Me).Count().ToString(), null), ($"hostile · {ScaleKm:0} km", "", null) });
        Button("rr.map", new Rect2(bx + 14, y + 72, 70, 30), "Map", () => X.Act.Expand(Region), tip: new Tip("Tactical map"));
        Button("rr.next", new Rect2(bx + 92, y + 72, 120, 30), "Next target", () => B.NextTarget(), tip: new Tip("Lock the next enemy (Tab)"));
        Kit.Hero(this, "crest", new Rect2(Size.X - 74, y + 30, 72, 72));
    }
}

/// <summary>The top strip: own hull, the weapon capacitor in cells, the target's hull, the clock, Pause and Menu. A skin
/// whose strips join its column draws the joined strip, its content moved into the strip's channel.</summary>
public sealed partial class TopStrip : ConsolePanel
{
    protected override void DrawPanel()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), Colors.Black); // the strip stands on black, never over the battle (owner, 2026-11-04)
        Kit.Piece(this, Sk.JoinedStrips ? "strip.top.join" : "strip.top", new Rect2(Vector2.Zero, Size));
        Ship s = Me;
        double hullFrac = s.Hull / s.Class.HullHp;
        StripMeter("top", hullFrac, $"Hull integrity {Pct(Math.Clamp(hullFrac, 0, 1))}");
        float cx = 96, cw = Size.X - 192, cy = (44 - 35) / 2f + Sk.StripTopDy, fix = 70 + 100 + 80, gap = 22, u = (cw - fix - gap * 5) / 3.2f, xx = cx;
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
        DrawRect(new Rect2(Vector2.Zero, Size), Colors.Black); // the strip stands on black, never over the battle (owner, 2026-11-04)
        Kit.Piece(this, Sk.JoinedStrips ? "strip.bottom.join" : "strip.bottom", new Rect2(Vector2.Zero, Size));
        Ship s = Me;
        StripMeter("bottom", s.V / (B.Tuning.SpeedPerBoxMS * 10), $"Speed {Math.Round(s.V)} m/s");
        float cx = 96, cw = Size.X - 192, cy = 12 + Sk.StripBottomDy, ch = 44, gap = 22, fix = 120 + 138, u = (cw - fix - gap * 4) / 3.45f, xx = cx;
        Lbl("Speed", xx, cy + 28);
        Num(Math.Round(s.V).ToString(), xx + 58, cy + 31, 46);
        const float wk = 96;
        SpeedSlider(xx + 112, cy, u * 1.5f - 112 - wk - 10, ch);
        WarpKey(new Rect2(xx + u * 1.5f - wk, cy + 7, wk, 30));
        xx += u * 1.5f + gap;
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

    // The warp key at the end of the speed slider (the battle console mockup's warpKey; owner, 2026-11-02: "it should start
    // out as disabled ... then it begins charging and glows, pulsating when its ready. it wont glow in a gravitational
    // anomaly however"). Offline: dark, its name dim. Armed with no free energy: "Need power" in Caution. Charging: filling
    // toward the energy a jump needs, a soft glow. Ready: green and pulsing; inside a Q-G field strong enough to need the
    // override it neither glows nor pulses. A click is Battle.WarpKey (it arms, reports or jumps; it never cuts power), and
    // its hover card gives the banks, the coils' draw, the course and the risk from the same Battle.WarpRiskOf.
    private void WarpKey(Rect2 r)
    {
        Ship s = Me;
        WarpState wp = s.Warp;
        var W = B.Data.Systems.Warp;
        WarpRisk risk = B.WarpRiskOf(s);
        float f = (float)Math.Clamp(wp.Banks / W.JumpE, 0, 1), h = r.Size.Y;
        bool anomaly = risk.Q > W.QgBlock, ready = B.WarpReady(s), starved = wp.Armed && !ready && s.Power["warp"] <= 0 && wp.Banks < W.JumpE;
        bool plotting = wp.Armed && wp.Plotting && wp.Plot < 1, blocked = wp.Armed && !ready && !plotting && wp.Banks >= W.JumpE && risk.Why != "";
        string title = !wp.Armed ? "Warp off · click to start charging" : ready ? "Warp ready · click to jump" : starved ? "Warp: no free reactor power · lower another system"
            : plotting ? "Warp: plotting the course" : blocked ? $"Warp: {risk.Why}" : "Warp charging";
        var rows = new System.Collections.Generic.List<(string, string, Color?)>
        {
            ("Stored for the jump", $"{Math.Floor(wp.Banks)}/{W.JumpE:0} E", null), ("Reactor power in", $"{s.Power["warp"]} of {W.CoilE} E", null), ("Course", Battle.CourseText(s), null),
            risk.Why != "" ? ("Jump", "blocked", C.Caution) : ("Jump risk", Pct(risk.Total), risk.Total > 0.3 ? C.Alert : risk.Total > 0.1 ? C.Caution : C.Accord),
        };
        if (risk.Why != "") rows.Add(("Needs", risk.Why, C.Caution));
        HitState st = Ui.Reg("warp.key", r, _ => B.WarpKey(), null, new Tip(title, rows));
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.GetTicksMsec() / 1000f * Mathf.Tau * 0.9f);
        // a glow: steady while charging on a plotted course, a pulse when ready; none while plotting or in a Q-G field
        if (wp.Armed && !starved && !plotting && !blocked && !anomaly)
            Kit.Shape(this, r.Grow(ready ? 2 + 4 * pulse : 3), h / 2 + 3, new Color(ready ? C.Accord : C.Sky, ready ? 0.25f + 0.25f * pulse : 0.25f));
        Kit.Shape(this, r, h / 2, C.Deep);
        if (!wp.Armed) Kit.Shape(this, r.Grow(-1), h / 2, Sk.R.Night);
        else
        {
            if (f > 0) Kit.Shape(this, new Rect2(r.Position, new Vector2(Math.Max(h, r.Size.X * (ready ? 1 : f)), h)), h / 2, ready ? (st.Down ? C.Lilac : C.Accord) : C.Signal);
            if (ready && !anomaly) Kit.Shape(this, r, h / 2, new Color(1, 1, 1, 0.28f * pulse));
        }
        // the plot's progress, a thin line along the key's foot
        if (plotting) DrawRect(new Rect2(r.Position.X + h / 2, r.End.Y - 4, (r.Size.X - h) * (float)wp.Plot, 4), C.Lilac);
        if (st.Hot) Kit.Shape(this, r.Grow(1), h / 2 + 1, C.Sky, 2);
        string lab = wp.Out > 0 ? "JUMPING" : starved ? "NEED POWER" : blocked ? (anomaly ? "Q-G FIELD" : "BLOCKED") : plotting && f >= 1 ? $"PLOT {Math.Floor(wp.Plot * 100)}%" : "WARP";
        Color col = !wp.Armed ? Sk.R.Ghost : starved || blocked ? C.Caution : ready || f > 0.55f ? KeyFace.Ink(Kit, ready ? C.Accord : C.Signal) : C.Sky;
        Text(lab, r.GetCenter().X, r.Position.Y + h * 0.68f, starved || blocked ? 14 : 17, col, HorizontalAlignment.Center, r.Size.X - 10, 700, 1);
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

/// <summary>The in-battle menu: a sheet over everything that dims the console and takes all input while it is up
/// (the mockup's uiSheet): resume, restart the battle, fullscreen or window, quit.</summary>
public sealed partial class MenuCard : ConsolePanel
{
    /// <summary>The sheet is over everything, never a panel in the column.</summary>
    protected override bool InColumn => false;

    protected override void DrawPanel()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0, 0, 0, 0.78f));
        Ui.Reg("sheet.block", new Rect2(Vector2.Zero, Size));
        float w = 600, h = 356;
        var at = new Rect2((Size.X - w) / 2, (Size.Y - h) / 2, w, h);
        // a solid background inside the frame (owner, 2026-11-03: "Pause menu should have a background"), so the dimmed
        // console does not show through the keys; it stops short of the frame's end caps (near black with the skin's dark
        // in it, so it reads as a panel over the dim)
        Shapes.RoundRect(this, new Rect2(at.Position, new Vector2(w - 21, h)), Sk.R.Sheet, 24, 0, 0, 24);
        Rect2 b = Frame(at, "Menu", false);
        float y = b.Position.Y + 8, bw = b.Size.X;
        Button("m.resume", new Rect2(b.Position.X, y, bw, 44), "Resume", X.Act.Menu, center: false); y += 56;
        // in a campaign mission the menu holds Withdraw (owner, 2026-11-06: "withdraw button should be in main meny"), and a
        // mission's battle is not restarted
        if (X.State.InMission)
            Button("m.withdraw", new Rect2(b.Position.X, y, bw, 44), X.Battle.Over != null ? "Battle over" : "Withdraw",
                X.Battle.Over != null ? null : () => { X.State.MenuOpen = false; X.Battle.Withdraw(); }, center: false, dis: X.Battle.Over != null,
                tip: new Tip("Withdraw", new (string, string, Color?)[] { ("Ends", "the battle where it stands", null), ("Grade", "a draw, unless an objective says otherwise", null) }));
        else Button("m.restart", new Rect2(b.Position.X, y, bw, 44), "Restart battle", X.Act.Restart, center: false);
        y += 56;
        Button("m.display", new Rect2(b.Position.X, y, bw, 44), DisplaySettings.Fullscreen ? "Display: fullscreen" : "Display: window", X.Act.Fullscreen,
            center: false, tip: new Tip("Fullscreen or window (F11)")); y += 56;
        Button("m.quit", new Rect2(b.Position.X, y, bw, 44), "Quit", X.Act.Quit, center: false);
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
        // kept on the screen (the layer itself has no size: it draws over everything), and never a clamp whose bounds cross
        // when the card is wider than the screen (found by the scripted captain, 2026-11-04: every hover card threw)
        float sw = GetViewportRect().Size.X;
        float x = Math.Clamp(a.GetCenter().X - w / 2, 4, Math.Max(4, sw - w - 4)), y = a.Position.Y - h - 6;
        if (y < 4) y = a.End.Y + 6;
        SkinStyle sk = _x.Kit.Style;
        Shapes.RoundRect(this, new Rect2(x, y, w, h), sk.R.Plate, 10);
        Shapes.RoundRect(this, new Rect2(x, y, w, 26), c.Sky, 10, 10, 2, 2);
        f.Draw(this, tip.Title.ToUpperInvariant(), x + 12, y + 19, 17, sk.InkOn(c.Sky), HorizontalAlignment.Left, w - 20, 600, 1);
        float yy = y + 48;
        foreach (var (l, v, col) in tip.Rows)
        {
            f.Draw(this, l.ToUpperInvariant(), x + 12, yy, 14, c.Lilac, HorizontalAlignment.Left, 0, 600, 1);
            f.Draw(this, v, x + w - 12, yy, 17, col ?? c.Sky, HorizontalAlignment.Right);
            yy += 22;
        }
    }
}
