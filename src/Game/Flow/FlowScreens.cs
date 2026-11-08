// FlowScreens: the game flow's screens and panels (mockup 1), each drawn only from the console skin's kit pieces (CLAUDE.md
// section 7: "Every screen (the menus, the campaign map's panels and the battle console) draws only kit pieces by name"),
// on the console's own immediate-mode controls (ConsolePanel, Imm). FlowPanel is their base: a full-screen panel whose
// empty space lets the pointer through (the map's HUD over the map). The screens: the main menu, the faction screen, the
// skirmish list, the map's HUD (top bar, sector panel, briefing, port), the debrief, and the mission strip over the battle.
// What they show is read from the campaign and the mission files (src/Core/Campaign); none holds a rule.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Campaign;
using FleetCommand.ConsoleModel;
using FleetCommand.Data;
using FleetCommand.Game.Ui;
using Godot;

namespace FleetCommand.Game.Flow;

public abstract partial class FlowPanel : ConsolePanel
{
    protected GameFlow F = null!;
    protected Campaign.Campaign? Camp => F.Camp;

    public void Bind(GameFlow f)
    {
        F = f;
        Init(f.X, Region.Station);
        Name = GetType().Name;
        MouseFilter = MouseFilterEnum.Pass;
    }

    protected override bool InColumn => false;

    // a press no control takes passes to what is under the panel (the map, under its HUD)
    public override void _GuiInput(InputEvent e) { if (Ui.Handle(e)) AcceptEvent(); }

    /// <summary>A solid sheet the panel's controls sit on, so the press on it stays with the panel.</summary>
    protected Rect2 Sheet(Rect2 r, string title)
    {
        Ui.Reg($"sheet.{title}", r);
        Shapes.RoundRect(this, new Rect2(r.Position, r.Size), new Color(Sk.R.Sheet, 0.96f), 18);
        return Frame(r, title, false);
    }

    /// <summary>Wrapped text in a width; returns the height used.</summary>
    protected float Para(string s, float x, float y, float w, int size = 19, Color? col = null, int maxLines = 99)
    {
        var words = s.Split(' ');
        var line = "";
        int n = 0;
        float lh = size * 1.32f;
        foreach (string word in words)
        {
            string t = line.Length == 0 ? word : line + " " + word;
            if (X.Fonts.Width(t, size, 500) > w && line.Length > 0)
            {
                if (n < maxLines) Text(line, x, y + n * lh, size, col ?? C.Text, HorizontalAlignment.Left, w, 500);
                n++;
                line = word;
            }
            else line = t;
        }
        if (line.Length > 0 && n < maxLines) { Text(line, x, y + n * lh, size, col ?? C.Text, HorizontalAlignment.Left, w, 500); n++; }
        return Math.Min(n, maxLines) * lh;
    }

    /// <summary>A label and a value on one line, the value right-aligned.</summary>
    protected void Row(string label, string value, float x, float y, float w, Color? col = null)
    {
        Lbl(label, x, y, w * 0.5f);
        Text(value, x + w, y, 19, col ?? C.Sky, HorizontalAlignment.Right, w * 0.6f, 600);
    }

    protected Color FacColor(string f) => F.Data.Factions.TryGetValue(f, out CampFaction? x) ? new Color(x.Color) : new Color("#7c8794");

    protected string FacName(string f) => F.Data.Factions.TryGetValue(f, out CampFaction? x) ? x.Name : f;

    protected Color GradeInk(string g) => g is "outstanding" or "victory" ? C.Accord : g == "draw" ? C.Sky : C.Alert;

    protected static string GradeName(string g) => g switch
    {
        "crushing" => "Crushing defeat", "defeat" => "Defeat", "draw" => "Draw", "victory" => "Victory", "outstanding" => "Outstanding", _ => g,
    };
}

/// <summary>The main menu: Campaign, Skirmish, Options, Quit.</summary>
// Its backdrop is Starfield2D's stars (src/Core/Space), made once for the window's size: faint stars as fine points, bright
// ones with a soft halo, the few brilliant ones with faint spikes, and only those twinkle, slowly.
public sealed partial class MenuScreen : FlowPanel
{
    private static System.Collections.Generic.List<FleetCommand.Space.Star> _stars = new();
    private static Vector2 _starsFor;
    private static (System.Collections.Generic.List<FleetCommand.Space.StarClass> Classes, System.Collections.Generic.List<FleetCommand.Space.StarColour> Colours)? _spec;

    /// <summary>Draws the backdrop's stars over a control of this size (remade when the size changes).</summary>
    public static void DrawStars(CanvasItem c, Vector2 size)
    {
        _spec ??= FleetCommand.Space.Starfield2D.LoadSpec(FileAccess.GetFileAsString("res://data/space/starfield.json"));
        if (_starsFor != size) { _stars = FleetCommand.Space.Starfield2D.Make((int)size.X, (int)size.Y, 20261105, _spec.Value.Classes, _spec.Value.Colours); _starsFor = size; }
        float t = (float)(Time.GetTicksMsec() / 1000.0);
        foreach (var st in _stars)
        {
            var k = _spec.Value.Classes[st.Class];
            var co = _spec.Value.Colours[st.Colour];
            float b = st.Brightness;
            if (st.Class >= 2) b *= 0.88f + 0.12f * Mathf.Sin(t * 0.9f + st.Phase);
            // a colour mixed 40 % to white, as the battle's stars are
            var col = new Color(Mathf.Lerp((float)co.R, 1, 0.4f), Mathf.Lerp((float)co.G, 1, 0.4f), Mathf.Lerp((float)co.B, 1, 0.4f));
            var p = new Vector2(st.X, st.Y);
            float r = (float)k.SizePx * 0.5f + 0.25f;
            if (st.Class >= 2) c.DrawCircle(p, r * 4.5f, new Color(col, 0.08f * b), true, -1, true);
            c.DrawCircle(p, r, new Color(col, 0.25f + 0.75f * b), true, -1, true);
            if (st.Class == 3)
            {
                float len = 9 * b;
                c.DrawLine(p - new Vector2(len, 0), p + new Vector2(len, 0), new Color(col, 0.22f * b), 1, true);
                c.DrawLine(p - new Vector2(0, len), p + new Vector2(0, len), new Color(col, 0.22f * b), 1, true);
            }
        }
    }

    protected override void DrawPanel()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.01f, 0.014f, 0.03f));
        DrawStars(this, Size);
        Ui.Reg("menu.block", new Rect2(Vector2.Zero, Size));
        float cx = Size.X / 2;
        Text("FLEET COMMAND", cx, Size.Y * 0.28f, 72, C.Sky, HorizontalAlignment.Center, Size.X - 40, 700, 6);
        Lbl("A captain's war among the stars", cx, Size.Y * 0.28f + 40, 0, null, HorizontalAlignment.Center, 17);
        float w = Math.Min(420, Size.X - 40), y = Size.Y * 0.42f;
        var r = new Rect2(cx - w / 2, y, w, 52);
        Button("menu.campaign", r, "Campaign", () => _ = F.Show(FlowScreen.Factions)); r.Position += new Vector2(0, 66);
        Button("menu.skirmish", r, "Skirmish", () => _ = F.Show(FlowScreen.Skirmish)); r.Position += new Vector2(0, 66);
        Button("menu.options", r, "Options", X.Act.Options); r.Position += new Vector2(0, 66);
        Button("menu.quit", r, "Quit", () => GetTree().Quit());
    }
}

/// <summary>The faction screen's 3D stage (FactionStage) in its own world, with the faction screen's keys over it.</summary>
public sealed partial class FactionView : Control
{
    public FactionStage Stage { get; } = new();
    private bool _built;

    // the stage is built the first time the screen shows, after the boot has loaded its glbs (Prewarm)
    public override void _Process(double delta)
    {
        if (_built || !IsVisibleInTree()) return;
        _built = true;
        Stage.Build();
    }

    public void Init(FactionScreen screen)
    {
        Name = "FactionView";
        MouseFilter = MouseFilterEnum.Ignore;
        var box = new SubViewportContainer { Stretch = true, MouseFilter = MouseFilterEnum.Ignore, Name = "View" };
        box.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(box);
        var vp = new SubViewport { OwnWorld3D = true, Msaa3D = Viewport.Msaa.Msaa4X, HandleInputLocally = false, Name = "FactionWorld" };
        box.AddChild(vp);
        vp.AddChild(Stage);
        screen.Stage = Stage;
        screen.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(screen);
    }
}

/// <summary>The faction screen (mockup 1's, docs/mockups/game-flow.html): the playable factions' badges floating over a walnut
/// table (FactionStage, under this panel), each named on the table's edge; the one under the pointer half lit, the picked
/// one lit full, with New campaign and Continue in a sheet below.</summary>
public sealed partial class FactionScreen : FlowPanel
{
    public FactionStage? Stage;
    /// <summary>Each playable faction's emblem (also the transport station's portrait of who holds a ship).</summary>
    internal static readonly Dictionary<string, string> Emblem = new()
    {
        ["concord"] = "res://assets/emblems/concord/b-e27-angle.png", ["krai"] = "res://assets/emblems/krai/kc-angle.png", ["ssarith"] = "res://assets/emblems/ssarith/sd-angle.png",
        ["kelvane"] = "res://assets/emblems/kelvane/ka-angle.png",
    };

    protected override void DrawPanel()
    {
        Ui.Reg("fac.block", new Rect2(Vector2.Zero, Size));
        Text("CHOOSE YOUR FACTION", 36, 64, 34, C.Sky, HorizontalAlignment.Left, Size.X - 300, 700, 3);
        var playable = F.Data.Factions.Values.Where(f => f.Playable).ToList();
        int hover = -1;
        if (Stage != null)
        {
            Vector2 mp = GetLocalMousePosition();
            Stage.Pointer = new Vector2(mp.X / Math.Max(1, Size.X) * 2 - 1, 1 - mp.Y / Math.Max(1, Size.Y) * 2);
            Stage.Picked = F.Picked;
            Stage.ScreenSize = Size;
            for (int i = 0; i < Stage.Factions.Count; i++)
            {
                string id = Stage.Factions[i];
                CampFaction? fac = playable.FirstOrDefault(f => f.Id == id);
                if (fac == null || Stage.ScreenRect(i, Size) is not { } r) continue;
                bool skin = X.Data.Factions.TryGetValue(fac.Id, out Faction? bf) && bf.Skin != null;
                HitState st = Ui.Reg($"fac.{fac.Id}", r, skin ? _ => F.Picked = fac.Id : null, null, skin ? null : new Tip(fac.Name, new (string, string, Color?)[] { ("In the game", "not yet: its console skin is to build", null) }));
                if (st.Hot) hover = i;
                bool picked = F.Picked == fac.Id;
                // the name on the table's edge, under its badge
                float ly = r.End.Y + r.Size.Y * 0.62f;
                Text(fac.Name.ToUpperInvariant(), r.GetCenter().X, ly, 24, picked ? new Color(fac.Color).Lightened(0.35f) : C.Text, HorizontalAlignment.Center, r.Size.X * 1.6f, 700, 2);
                if (!skin) Lbl("Not in the game yet", r.GetCenter().X, ly + 24, r.Size.X * 1.6f, null, HorizontalAlignment.Center);
            }
            Stage.Hover = hover;
        }
        // the picked faction's keys, in a sheet below the table
        if (playable.FirstOrDefault(f => f.Id == F.Picked) is { } pf)
        {
            float w = Math.Min(560, Size.X - 40), h = 190;
            Rect2 b = Sheet(new Rect2((Size.X - w) / 2, Size.Y - h - 24, w, h), pf.Name);
            Text(F.HasSave(pf.Id) ? "A campaign is saved for this faction." : "No campaign saved for this faction yet.", b.Position.X, b.Position.Y + 22, 17, C.Lilac, HorizontalAlignment.Left, b.Size.X, 600);
            float bw = (b.Size.X - 12) / 2, by = b.End.Y - 52;
            Button($"fac.{pf.Id}.cont", new Rect2(b.Position.X, by, bw, 48), "Continue", () => _ = F.ContinueCampaign(pf.Id), dis: !F.HasSave(pf.Id));
            Button($"fac.{pf.Id}.new", new Rect2(b.Position.X + bw + 12, by, bw, 48), "New campaign", () => _ = F.NewCampaign(pf.Id));
        }
        Button("fac.back", new Rect2(Size.X - 230, 30, 200, 48), "Back", () => _ = F.Show(FlowScreen.Menu));
        if (F.Msg.Length > 0) Text(F.Msg, Size.X / 2, 110, 19, C.Caution, HorizontalAlignment.Center, Size.X - 500);
    }
}

/// <summary>Skirmish (owner, 2026-11-06: "I only need the scenario type, then when I select a scenario, I need to see what
/// faction I can play that scenario AS", "Also need a scroll bar"): the scenario types down the left, each once, in menu
/// order, scrolled by the wheel or the bar; the type picked on the right with its objective and a key for each faction it
/// can be played as, which starts it.</summary>
public sealed partial class SkirmishScreen : FlowPanel
{
    private const float RowH = 52, Gap = 12;
    private string? _type;
    private float _scroll;
    private Rect2 _list;

    // the scenario types in menu order, each with the scenarios that play it (one a side)
    private List<(string Name, List<Scenario> Sides)> Types() =>
        X.Data.Scenarios.GroupBy(sc => sc.Name).Select(g => (g.Key, g.ToList())).ToList();

    private float MaxScroll(int n) => Math.Max(0, n * (RowH + Gap) - Gap - _list.Size.Y);

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } w && _list.HasPoint(w.Position))
        {
            _scroll = Math.Clamp(_scroll + (w.ButtonIndex == MouseButton.WheelDown ? 1 : -1) * (RowH + Gap), 0, MaxScroll(Types().Count));
            AcceptEvent();
            return;
        }
        base._GuiInput(e);
    }

    protected override void DrawPanel()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.012f, 0.016f, 0.03f));
        var types = Types();
        _type ??= types.FirstOrDefault().Name;
        float w = Math.Min(1100, Size.X - 40), h = Size.Y - 80;
        Rect2 b = Sheet(new Rect2((Size.X - w) / 2, 40, w, h), "Skirmish");
        float lw = Math.Min(380, b.Size.X * 0.42f);
        _list = new Rect2(b.Position.X, b.Position.Y + 34, lw - 26, b.Size.Y - 34 - 64);
        Lbl("SCENARIO", b.Position.X, b.Position.Y + 18);
        _scroll = Math.Clamp(_scroll, 0, MaxScroll(types.Count));
        // the rows that fit wholly in the list, the rest reached by scrolling
        for (int i = 0; i < types.Count; i++)
        {
            float y = _list.Position.Y + i * (RowH + Gap) - _scroll;
            if (y < _list.Position.Y - 0.5f || y + RowH > _list.End.Y + 0.5f) continue;
            string name = types[i].Name;
            Button($"sk.type.{Slug(name)}", new Rect2(_list.Position.X, y, _list.Size.X, RowH), name, () => _type = name, on: name == _type, center: false);
        }
        // the scroll bar: a track beside the list and a thumb the size of what shows, dragged or clicked to a place
        float max = MaxScroll(types.Count);
        if (max > 0)
        {
            var track = new Rect2(_list.End.X + 10, _list.Position.Y, 12, _list.Size.Y);
            float th = Math.Max(44, track.Size.Y * _list.Size.Y / (_list.Size.Y + max)), ty = track.Position.Y + (track.Size.Y - th) * _scroll / max;
            HitState st = Ui.Reg("sk.scroll", new Rect2(track.Position.X - 8, track.Position.Y, track.Size.X + 16, track.Size.Y), at => ScrollTo(at.Y, track, th, max),
                (at, _) => ScrollTo(at.Y, track, th, max), new Tip("Scroll the scenarios"));
            Shapes.RoundRect(this, track, new Color(Sk.R.Plate, 0.9f), 6);
            Shapes.RoundRect(this, new Rect2(track.Position.X, ty, track.Size.X, th), st.Hot || st.Down ? C.Sky : C.Signal, 6);
        }
        // the type picked: its objective, and a key for each faction it can be played as
        var pick = types.FirstOrDefault(t => t.Name == _type);
        if (pick.Sides != null)
        {
            float rx = b.Position.X + lw + 20, rw = b.End.X - rx, y = b.Position.Y + 34;
            Text(pick.Name.ToUpperInvariant(), rx, y + 26, 34, C.Sky, HorizontalAlignment.Left, rw, 700, 1);
            y += 52;
            y += Para(pick.Sides[0].Objective, rx, y + 20, rw, 19, C.Text) + 24;
            Lbl("PLAY AS", rx, y + 14);
            y += 28;
            foreach (Scenario sc in pick.Sides)
            {
                Scenario s = sc;
                var key = new Rect2(rx, y, Math.Min(rw, 420), RowH);
                Button($"sk.{sc.Id}", key, FacName(sc.Side), () => F.StartSkirmish(s), center: false,
                    tip: new Tip(sc.Name, new (string, string, Color?)[] { ("As", FacName(sc.Side), FacColor(sc.Side)), ("Ships", sc.Ships.Count.ToString(), null) }));
                DrawRect(new Rect2(key.End.X + 12, y + 14, 24, 24), FacColor(sc.Side));
                y += RowH + Gap;
            }
        }
        Button("sk.back", new Rect2(b.Position.X, b.End.Y - 50, 200, 48), "Back", () => _ = F.Show(FlowScreen.Menu));
    }

    private void ScrollTo(float y, Rect2 track, float thumb, float max) =>
        _scroll = Math.Clamp((y - track.Position.Y - thumb / 2) / Math.Max(1, track.Size.Y - thumb) * max, 0, max);

    private static string Slug(string s) => new string(s.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
}

/// <summary>The mission strip over the battle: the mission's name and its goals, each ticked once it is met (the console's
/// Goals, read live by MissionRun.MetNow, the same rule the debrief grades by). Its frame stands over the battle with nothing
/// filled behind the goals (owner, 2026-11-06: "make background transparent on goal"); Withdraw is in the battle's menu. Its X
/// shuts it and Comms shows it again (owner, 2026-11-06: "add close button to goals, and put them in comms panel").</summary>
public sealed partial class MissionStrip : FlowPanel
{
    protected override void DrawPanel()
    {
        MissionPlan? p = F.Plan;
        if (p == null || X.State.Goals is not { } goals) return;
        Rect2 b = Frame(new Rect2(Vector2.Zero, Size), p.Scenario.Name, false);
        var xr = new Rect2(b.End.X - 26, b.Position.Y + 2, 26, 26);
        KeyFace.Close(Kit, this, xr, Ui.Reg("goals.x", xr, _ => X.State.GoalsShown = false, null, new Tip("Close")).Hot);
        float y = b.Position.Y + 20;
        foreach (GoalLine g in goals())
        {
            Goal(g, b.Position.X, y, b.Size.X - 36, true);
            y += 24;
        }
    }
}

/// <summary>The debrief: the verdict, the grade, credits, prestige and reputation won or lost, the objectives and the ships.</summary>
public sealed partial class DebriefScreen : FlowPanel
{
    protected override void DrawPanel()
    {
        // on black: the battle fades to black as it ends, so the debrief stays on the black it faded to (owner, 2026-11-06:
        // "I think this background should be black. Because you faded to black")
        DrawRect(new Rect2(Vector2.Zero, Size), Colors.Black);
        Ui.Reg("db.block", new Rect2(Vector2.Zero, Size));
        Debrief? d = F.Last;
        if (d == null) return;
        float w = Math.Min(860, Size.X - 30), h = Math.Min(Size.Y - 40, 760);
        float need = 300 + (F.Skirmish || F.Camp == null ? 0 : 120) + (d.Objectives.Count > 0 ? 38 + 28 * d.Objectives.Count : 0)
            + (d.Ships.Count > 0 ? 38 + 26 * d.Ships.Count : 0) + (d.Log != null ? 60 : 0);
        h = Math.Min(h, need);
        Rect2 b = Sheet(new Rect2((Size.X - w) / 2, (Size.Y - h) / 2, w, h), d.Mission);
        float x = b.Position.X, y = b.Position.Y + 60, cw = b.Size.X;
        string verdict = d.Win ? "MISSION ACCOMPLISHED" : d.How == "withdraw" ? "WITHDRAWN" : "MISSION FAILED";
        Text(verdict, x + cw / 2, y, 46, GradeInk(d.Grade), HorizontalAlignment.Center, cw, 700, 3);
        Text(GradeName(d.Grade).ToUpperInvariant(), x + cw / 2, y + 40, 24, C.Lilac, HorizontalAlignment.Center, cw, 600, 2);
        y += 72;
        if (!F.Skirmish && F.Camp != null)
        {
            float tw = (cw - 24) / 3;
            Tile(new Rect2(x, y, tw, 96), "Credits", (d.Credits >= 0 ? "+" : "") + d.Credits + " cr", $"now {F.Camp.S.Credits} cr");
            Tile(new Rect2(x + tw + 12, y, tw, 96), "Prestige", (d.Prestige >= 0 ? "+" : "") + d.Prestige, $"now {d.PrestigeNow}");
            string rep = d.Standing.Count == 0 ? "no change" : string.Join("  ", d.Standing.Select(s => $"{F.Data.Factions[s.Fac].Short} {(s.Delta > 0 ? "+" : "")}{s.Delta}"));
            string now = d.Standing.Count == 0 ? "" : string.Join("  ", d.Standing.Select(s => $"{F.Data.Factions[s.Fac].Short} {s.Now}"));
            Tile(new Rect2(x + 2 * (tw + 12), y, tw, 96), "Reputation", rep, now);
            y += 120;
        }
        if (d.Objectives.Count > 0)
        {
            Lbl("Objectives", x, y); y += 28;
            foreach (var (text, primary, met) in d.Objectives)
            {
                Text(text, x + 12, y, 19, primary ? C.Text : C.Lilac, HorizontalAlignment.Left, cw - 140, 600);
                Text(met ? "MET" : "MISSED", x + cw, y, 19, met ? C.Accord : C.Alert, HorizontalAlignment.Right, 120, 700);
                y += 28;
            }
            y += 10;
        }
        if (d.Ships.Count > 0)
        {
            Lbl("Your ships", x, y); y += 28;
            foreach (var (ship, fate) in d.Ships) { Row(ship, fate, x + 12, y, cw - 12, fate == "destroyed" ? C.Alert : C.Sky); y += 26; }
            y += 10;
        }
        if (d.Log != null) Para(d.Log, x, y, cw, 18, C.Lilac, 3);
        Button("db.continue", new Rect2(b.End.X - 260, b.End.Y - 52, 260, 50), "Continue", F.AfterDebrief);
    }

    private void Tile(Rect2 r, string label, string value, string sub)
    {
        Kit.Piece(this, "frame.inset", r);
        Lbl(label, r.Position.X + 14, r.Position.Y + 26, r.Size.X - 20);
        Text(value, r.Position.X + 14, r.Position.Y + 58, 24, C.Sky, HorizontalAlignment.Left, r.Size.X - 24, 700);
        Text(sub, r.Position.X + 14, r.Position.Y + 84, 15, C.Lilac, HorizontalAlignment.Left, r.Size.X - 24, 500);
    }
}

/// <summary>The loading screen: what is being done and a bar of how far along (the boot's shader prewarm, building the
/// campaign map, preparing a battle). The bar eases toward the progress it is given, so it never jumps.</summary>
public sealed partial class LoadingScreen : FlowPanel
{
    public string Title = "Fleet Command";
    public string Step = "";
    public float Target;
    private float _shown;

    public void Reset(string title) { Title = title; Step = ""; Target = 0; _shown = 0; }

    protected override void DrawPanel()
    {
        _shown += (Target - _shown) * Math.Min(1, (float)GetProcessDeltaTime() * 10);
        if (Target >= 1 && _shown > 0.995f) _shown = 1;
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.01f, 0.014f, 0.03f));
        Ui.Reg("load.block", new Rect2(Vector2.Zero, Size));
        float w = Math.Min(720, Size.X - 60), x = (Size.X - w) / 2, y = Size.Y * 0.5f;
        Text(Title.ToUpperInvariant(), Size.X / 2, y - 60, 40, C.Sky, HorizontalAlignment.Center, Size.X - 40, 700, 4);
        Bar(x, y, w, Step, _shown, false);
        Text($"{Math.Round(_shown * 100)} %", x + w, y + 60, 18, C.Lilac, HorizontalAlignment.Right, 100, 600);
    }

    public float Shown => _shown;
}
