// Main: the composition root (CLAUDE.md section 6.2). It loads and validates the data, picks the skin, builds the battle,
// the 3D view and the console, wires them together and runs the fixed step. Nothing else creates these; every other
// class gets what it needs passed in. It also runs the headless checks: --layout-test (every panel at its designed
// rectangle, clipped, at several window sizes) and --still (a picture of the console after a number of steps, under
// Xvfb in a cloud session).
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.ConsoleModel;
using FleetCommand.Data;
using FleetCommand.Game.Ui;
using FleetCommand.Game.Ui.Kit;
using FleetCommand.Game.View;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game;

public sealed partial class Main : Node
{
    private ConsoleContext _x = null!;
    private BattleView _view = null!;
    private ViewOverlay _overlay = null!;
    private Control _root = null!;
    private ColorRect _column = null!;
    private MenuCard _menu = null!;
    private readonly Dictionary<Region, ConsolePanel> _panels = new();
    private double _acc;
    private Dictionary<string, Action> _bindings = new();
    private readonly Dictionary<string, string> _args = new();

    public override void _Ready()
    {
        ParseArgs();
        GameData data;
        try { data = DataLoader.Load(ReadData); }
        catch (DataError e) { GD.PushError($"DATA_ERROR {e.Message}"); GetTree().Quit(1); return; }
        IKit kit = new ConcordKit();
        KitPieces.Verify(kit);
        GameActions act = null!;
        act = new GameActions(() => _x);
        _x = new ConsoleContext
        {
            Data = data, Kit = kit, Fonts = new Fonts("res://assets/fonts/Antonio-VariableFont_wght.ttf"), Tips = new TipBoard(),
            State = new ConsoleState(), Act = act, Battle = new Battle(data, data.Skirmish),
        };
        act.OnLayout = Relayout;
        act.OnRestart = Restart;
        act.OnQuit = () => GetTree().Quit();
        _bindings = act.Bindings.ToDictionary(kv => kv.Key, kv => kv.Value);

        _view = new BattleView();
        AddChild(_view);
        _view.Init(_x);

        var layer = new CanvasLayer { Name = "Console" };
        AddChild(layer);
        _root = new Control { Name = "Root", MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(_root);
        _column = new ColorRect { Name = "Column", Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(_column);
        _overlay = new ViewOverlay();
        _overlay.Init(_x, _view);
        _root.AddChild(_overlay);
        Add(new PlatePanel(), Region.Plate);
        Add(new StationPanel(), Region.Station);
        Add(new ShipPanel(), Region.Ship);
        Add(new WeaponsPanel(), Region.Weapons);
        Add(new RadarPanel(), Region.Radar);
        Add(new TopStrip(), Region.Top);
        Add(new BottomStrip(), Region.Bottom);
        _menu = new MenuCard();
        _menu.Init(_x, Region.Station);
        _menu.Name = "Menu";
        _root.AddChild(_menu);
        var tips = new TooltipLayer();
        tips.Init(_x);
        _root.AddChild(tips);

        GetViewport().SizeChanged += Relayout;
        Relayout();
        GD.Print("GAME_READY");
        if (_args.ContainsKey("layout-test")) CallDeferred(nameof(RunLayoutTest));
        else if (_args.TryGetValue("still", out string? still)) _ = Still(still);
    }

    private void Add(ConsolePanel p, Region r)
    {
        p.Init(_x, r);
        _root.AddChild(p);
        _panels[r] = p;
    }

    private static string ReadData(string rel)
    {
        string path = "res://data/" + rel;
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (f == null) throw new DataError($"data/{rel}: cannot be opened ({FileAccess.GetOpenError()})");
        return f.GetAsText();
    }

    private void Restart()
    {
        _x.Battle = new Battle(_x.Data, _x.Data.Skirmish);
        _view.Rebuild();
        _acc = 0;
    }

    public override void _Process(double delta)
    {
        if (_x == null) return;
        _menu.Visible = _x.State.MenuOpen;
        if (_x.State.Paused || _x.State.MenuOpen || _args.ContainsKey("still")) return;
        double step = _x.Data.Tuning.StepS;
        _acc = Math.Min(_acc + delta, step * 6);
        while (_acc >= step) { _x.Battle.Step(); _acc -= step; }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_x == null || e.IsEcho()) return;
        foreach (var (name, run) in _bindings)
        {
            if (!e.IsActionPressed(name)) continue;
            run();
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    /// <summary>Places every panel for the window and the expanded panel (ConsoleLayout, the one layout).</summary>
    public void Relayout() => Relayout(GetViewport().GetVisibleRect().Size);

    private void Relayout(Vector2 size)
    {
        if (_x == null) return;
        ConsoleFrame f = ConsoleLayout.Compute(_x.Data.Layout, size.X, size.Y, _x.State.Expanded);
        _x.State.Expanded = f.Expanded;
        _root.Position = Vector2.Zero;
        _root.Size = size;
        foreach (var (r, p) in _panels)
        {
            if (f.Regions.TryGetValue(r, out Rect rect)) { p.Visible = true; p.Position = V(rect.X, rect.Y); p.Size = V(rect.W, rect.H); }
            else p.Visible = false;
        }
        Rect v = f.View;
        _overlay.Position = V(v.X, v.Y);
        _overlay.Size = V(v.W, v.H);
        _column.Position = Vector2.Zero;
        _column.Size = V(v.X, size.Y);
        _menu.Size = new Vector2(320, 240);
        _menu.Position = V(v.X + v.W / 2 - 160, v.Y + v.H / 2 - 120);
        _view.SetViewRect(new Rect2(V(v.X, v.Y), V(v.W, v.H)), size);
        _view.Sync();
    }

    private static Vector2 V(double x, double y) => new((float)x, (float)y);

    private void ParseArgs()
    {
        foreach (string a in OS.GetCmdlineUserArgs())
        {
            string s = a.TrimStart('-');
            int eq = s.IndexOf('=');
            if (eq < 0) _args[s] = "";
            else _args[s[..eq]] = s[(eq + 1)..];
        }
    }

    // ---- Headless checks ------------------------------------------------------------------------------------------

    /// <summary>Every panel at its designed rectangle and clipped, at the reference, wider and narrower windows and expanded.</summary>
    private void RunLayoutTest()
    {
        var fails = new List<string>();
        LayoutDef l = _x.Data.Layout;
        foreach (Vector2 size in new[] { new Vector2(1920, 1080), new Vector2(2560, 1080), new Vector2(1500, 1080) })
            foreach (Region? ex in new Region?[] { null, Region.Station, Region.Ship, Region.Weapons, Region.Radar })
            {
                _x.State.Expanded = ex;
                Relayout(size);
                ConsoleFrame want = ConsoleLayout.Compute(l, size.X, size.Y, ex);
                foreach (var (r, p) in _panels)
                {
                    bool shown = want.Regions.TryGetValue(r, out Rect wr);
                    if (p.Visible != shown) fails.Add($"{size} {ex}: {r} visible {p.Visible}, want {shown}");
                    if (!shown) continue;
                    if (p.Position != V(wr.X, wr.Y) || p.Size != V(wr.W, wr.H)) fails.Add($"{size} {ex}: {r} at {p.Position} {p.Size}, want {wr}");
                    if (!p.ClipContents) fails.Add($"{r}: not clipped");
                }
                if (ex == null)
                    foreach (Region r in new[] { Region.Plate, Region.Station, Region.Ship, Region.Weapons, Region.Radar })
                        if (_panels[r].Size != V(Pick(l, r).W, Pick(l, r).H)) fails.Add($"{size}: {r} changed size");
            }
        // a long name is trimmed to its room and never widens what holds it
        float w = _x.Fonts.Line("THE EXTREMELY LONG NAME OF A KETHRAN DREADNOUGHT OF THE LINE", 18, 700, 1, 220).GetLineWidth();
        if (w > 220.5f) fails.Add($"a trimmed line is {w} px wide in a 220 px room");
        _x.State.Expanded = null;
        Relayout();
        foreach (string s in fails) GD.PrintErr("LAYOUT_FAIL " + s);
        GD.Print(fails.Count == 0 ? "LAYOUT_TEST PASS" : $"LAYOUT_TEST FAIL ({fails.Count})");
        GetTree().Quit(fails.Count == 0 ? 0 : 1);
    }

    private static Rect Pick(LayoutDef l, Region r) => r switch
    {
        Region.Plate => l.Plate, Region.Station => l.Station, Region.Ship => l.Ship, Region.Weapons => l.Weapons, _ => l.Radar,
    };

    /// <summary>Plays the battle for --steps (default 600) with the target locked, sets the console as asked, saves a PNG and
    /// quits. The window size is Godot's own --resolution flag.</summary>
    private async System.Threading.Tasks.Task Still(string path)
    {
        int steps = _args.TryGetValue("steps", out string? n) ? int.Parse(n) : 600;
        Battle b = _x.Battle;
        b.Lock(_args.GetValueOrDefault("target", "vorrak"));
        for (int i = 0; i < steps; i++) { if (i % 30 == 0) b.Fire(); b.Step(); }
        if (_args.TryGetValue("station", out string? st)) _x.State.Station = st;
        if (_args.TryGetValue("page", out string? pg)) _x.State.ShipPage = pg;
        if (_args.TryGetValue("expand", out string? ex) && Enum.TryParse(ex, true, out Region er)) _x.State.Expanded = er;
        if (_args.ContainsKey("menu")) _x.State.MenuOpen = true;
        if (_args.ContainsKey("paused")) _x.State.Paused = true;
        for (int i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Relayout();
        for (int i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Image img = GetViewport().GetTexture().GetImage();
        Error err = img.SavePng(path);
        GD.Print(err == Error.Ok ? $"STILL {path} {img.GetWidth()}x{img.GetHeight()}" : $"STILL_FAIL {err}");
        GetTree().Quit(err == Error.Ok ? 0 : 1);
    }
}
