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
        Button("menu.options", r, DisplaySettings.Fullscreen ? "Display: fullscreen" : "Display: window", X.Act.Fullscreen); r.Position += new Vector2(0, 66);
        Button("menu.quit", r, "Quit", () => GetTree().Quit());
    }
}

/// <summary>The faction screen: each playable faction's emblem; picked, New campaign or Continue.</summary>
public sealed partial class FactionScreen : FlowPanel
{
    private static readonly Dictionary<string, string> Emblem = new()
    {
        ["concord"] = "res://assets/emblems/concord/b-e27-angle.png", ["krai"] = "res://assets/emblems/krai/kc-angle.png", ["ssarith"] = "res://assets/emblems/ssarith/sd-angle.png",
    };
    private readonly Dictionary<string, Texture2D?> _tex = new();

    protected override void DrawPanel()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.012f, 0.016f, 0.03f));
        Ui.Reg("fac.block", new Rect2(Vector2.Zero, Size));
        Text("CHOOSE YOUR FACTION", Size.X / 2, 90, 40, C.Sky, HorizontalAlignment.Center, Size.X - 40, 700, 3);
        var playable = F.Data.Factions.Values.Where(f => f.Playable).ToList();
        int n = playable.Count;
        float gap = 26, cw = Math.Min(380, (Size.X - 60 - gap * (n - 1)) / n), ch = Math.Min(560, Size.Y - 230), x0 = (Size.X - (n * cw + (n - 1) * gap)) / 2, y0 = 140;
        for (int i = 0; i < n; i++)
        {
            CampFaction fac = playable[i];
            bool skin = X.Data.Factions.TryGetValue(fac.Id, out Faction? bf) && bf.Skin != null;
            var r = new Rect2(x0 + i * (cw + gap), y0, cw, ch);
            HitState st = Ui.Reg($"fac.{fac.Id}", r, skin ? _ => F.Picked = fac.Id : null, null, skin ? null : new Tip(fac.Name, new (string, string, Color?)[] { ("In the game", "not yet: its console skin is to build", null) }));
            bool picked = F.Picked == fac.Id;
            Kit.Piece(this, picked ? "frame.panel" : "frame.inset", r, skin ? 1 : 0.4f);
            if (!_tex.TryGetValue(fac.Id, out Texture2D? t)) _tex[fac.Id] = t = Emblem.TryGetValue(fac.Id, out string? p) && ResourceLoader.Exists(p) ? GD.Load<Texture2D>(p) : null;
            float es = Math.Min(cw - 60, ch - 220) * (st.Hot || picked ? 1.04f : 1);
            if (t != null) DrawTextureRect(t, new Rect2(r.GetCenter().X - es / 2, r.Position.Y + 30, es, es), false, skin ? Colors.White : new Color(1, 1, 1, 0.35f));
            Text(fac.Name.ToUpperInvariant(), r.GetCenter().X, r.Position.Y + 60 + es, 28, new Color(fac.Color).Lightened(0.25f), HorizontalAlignment.Center, cw - 20, 700, 2);
            if (!skin) { Lbl("Not in the game yet", r.GetCenter().X, r.Position.Y + 92 + es, cw - 20, null, HorizontalAlignment.Center); continue; }
            if (!picked) { Lbl("Choose", r.GetCenter().X, r.Position.Y + 92 + es, cw - 20, null, HorizontalAlignment.Center); continue; }
            float by = r.End.Y - 128;
            // inside the frame's own padding (a Concord sweep's arm is wide)
            float bx = r.Position.X + Sk.Pad.L + 8, bw = cw - Sk.Pad.L - Sk.Pad.R - 16;
            Button($"fac.{fac.Id}.new", new Rect2(bx, by, bw, 48), "New campaign", () => _ = F.NewCampaign(fac.Id));
            Button($"fac.{fac.Id}.cont", new Rect2(bx, by + 60, bw, 48), "Continue", () => _ = F.ContinueCampaign(fac.Id), dis: !F.HasSave(fac.Id));
        }
        Button("fac.back", new Rect2(30, Size.Y - 76, 200, 48), "Back", () => _ = F.Show(FlowScreen.Menu));
        if (F.Msg.Length > 0) Text(F.Msg, Size.X / 2, Size.Y - 44, 19, C.Caution, HorizontalAlignment.Center, Size.X - 500);
    }
}

/// <summary>The skirmish list: the game's scenarios (data/battle/scenarios.json), each played from the side it was written for.</summary>
public sealed partial class SkirmishScreen : FlowPanel
{
    protected override void DrawPanel()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.012f, 0.016f, 0.03f));
        float w = Math.Min(760, Size.X - 40), h = Math.Min(Size.Y - 80, 110 + 66 * X.Data.Scenarios.Count);
        Rect2 b = Sheet(new Rect2((Size.X - w) / 2, (Size.Y - h) / 2, w, h), "Skirmish");
        float y = b.Position.Y + 10;
        foreach (Scenario sc in X.Data.Scenarios)
        {
            Scenario s = sc;
            Button($"sk.{sc.Id}", new Rect2(b.Position.X, y, b.Size.X, 52), $"{sc.Name}  ·  {FacName(sc.Side)}", () => F.StartSkirmish(s), center: false,
                tip: new Tip(sc.Name, new (string, string, Color?)[] { ("Objective", sc.Objective, null), ("Ships", sc.Ships.Count.ToString(), null) }));
            y += 66;
        }
        Button("sk.back", new Rect2(b.Position.X, b.End.Y - 50, 200, 48), "Back", () => _ = F.Show(FlowScreen.Menu));
    }
}

/// <summary>The mission strip over the battle: the mission, its objectives, and the Withdraw key.</summary>
public sealed partial class MissionStrip : FlowPanel
{
    protected override void DrawPanel()
    {
        MissionPlan? p = F.Plan;
        if (p == null) return;
        Rect2 b = Sheet(new Rect2(Vector2.Zero, Size), p.Scenario.Name);
        float y = b.Position.Y + 20;
        foreach (MissionObjective o in p.Battle.Objectives)
        {
            DrawCircle(new Vector2(b.Position.X + 6, y - 6), 4, o.Primary ? C.Sky : C.Lilac);
            Text(o.Text.Eval(new OfferScope(Camp!, p.Mission)) + (o.Primary ? "" : " (bonus)"), b.Position.X + 18, y, 17, o.Primary ? C.Text : C.Lilac, HorizontalAlignment.Left, b.Size.X - 18, 600);
            y += 24;
        }
        bool over = X.Battle.Over != null;
        Button("ms.withdraw", new Rect2(b.Position.X, b.End.Y - 38, 170, 36), over ? "Battle over" : "Withdraw", over ? null : F.Withdraw, dis: over,
            tip: new Tip("Withdraw", new (string, string, Color?)[] { ("Ends", "the battle where it stands", null), ("Grade", "a draw, unless an objective says otherwise", null) }));
    }
}

/// <summary>The debrief: the verdict, the grade, credits, prestige and reputation won or lost, the objectives and the ships.</summary>
public sealed partial class DebriefScreen : FlowPanel
{
    protected override void DrawPanel()
    {
        // dimmed, not hidden: the battle (or the map) stays behind the debrief
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0, 0, 0, 0.5f));
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
