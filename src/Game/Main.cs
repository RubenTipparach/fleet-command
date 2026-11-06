// Main: the composition root (CLAUDE.md section 6.2). It loads and validates the data, starts the scenario (--scenario=id,
// or the first) with its side's skin, builds the battle, the 3D view and the console, wires them together and runs the
// fixed step. Starting another scenario (the view's scene selector) rebuilds the battle and, for the other side, the skin. Nothing else creates these; every other
// class gets what it needs passed in. It also runs the headless checks: --layout-test (every panel at its designed
// rectangle, clipped, at several window sizes) and --still (a picture of the console after a number of steps, under
// Xvfb in a cloud session).
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Campaign;
using FleetCommand.ConsoleModel;
using FleetCommand.Data;
using FleetCommand.Game.Flow;
using FleetCommand.Game.Ui;
using FleetCommand.Game.Playtest;
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
    private ColumnBacking _column = null!;
    private MenuCard _menu = null!;
    private OptionsCard _options = null!;
    private MusicPlayer _music = null!;
    private readonly Dictionary<Region, ConsolePanel> _panels = new();
    private double _acc;
    private Dictionary<string, Action> _bindings = new();
    private readonly Dictionary<string, string> _args = new();
    private GameFlow? _flow;
    private CanvasLayer _console = null!;

    public override void _Ready()
    {
        ParseArgs();
        // the player's window mode (borderless fullscreen unless they chose a window); a scripted run keeps the window it
        // was started in
        if (!_args.ContainsKey("playtest") && !_args.ContainsKey("still") && !_args.ContainsKey("layout-test")) DisplaySettings.ApplySaved();
        AudioSettings.ApplySaved();
        GameData data;
        CampaignData camp;
        try
        {
            data = DataLoader.Load(ReadData);
            camp = CampaignData.Load(ReadData, MissionFiles(), data);
        }
        catch (DataError e) { GD.PushError($"DATA_ERROR {e.Message}"); GetTree().Quit(1); return; }
        Scenario start = StartScenario(data);
        IKit kit = KitFor(data, start);
        GameActions act = null!;
        act = new GameActions(() => _x);
        _x = new ConsoleContext
        {
            Data = data, Kit = kit, Fonts = new Fonts("res://assets/fonts/Antonio-VariableFont_wght.ttf"), Tips = new TipBoard(),
            State = new ConsoleState(), Act = act, Battle = new Battle(data, start),
        };
        act.OnLayout = Relayout;
        act.OnRestart = Restart;
        act.OnScene = id => Start(_x.Data.ScenarioById(id));
        act.OnQuit = () => GetTree().Quit();
        _bindings = act.Bindings.ToDictionary(kv => kv.Key, kv => kv.Value);

        _view = new BattleView();
        AddChild(_view);
        _view.Init(_x);

        var layer = _console = new CanvasLayer { Name = "Console" };
        AddChild(layer);
        _root = new Control { Name = "Root", MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(_root);
        _column = new ColumnBacking { Kit = kit };
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
        // the Options sheet over every screen (the menus and the battle), under the loading screen's fade
        var optLayer = new CanvasLayer { Name = "Options", Layer = 12 };
        AddChild(optLayer);
        _options = new OptionsCard();
        _options.Init(_x, Region.Station);
        _options.Name = "Options";
        _options.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _options.Visible = false;
        optLayer.AddChild(_options);
        _music = new MusicPlayer();
        AddChild(_music);

        // the game flow (menus, campaign, missions) unless the run is one of the battle's own checks or a scenario asked for
        // a playtest whose script plays the campaign ("flow": true) runs the flow too, saving apart from the player's campaign
        bool flowTest = _args.TryGetValue("playtest", out string? pt) && StrictJson.Parse(ReadData($"playtests/{pt}.json"), pt).Bool("flow", false);
        if (flowTest) GameFlow.SaveTag = "playtest-";
        bool battleOnly = new[] { "scenario", "playtest", "still", "layout-test", "parity-test" }.Any(_args.ContainsKey) && !_args.ContainsKey("flow") && !flowTest;
        if (!battleOnly)
        {
            _flow = new GameFlow();
            AddChild(_flow);
            _flow.Init(_x, camp, Start, ShowBattle, UseSkin);
            act.OnQuit = () => { if (_flow.Screen == FlowScreen.Battle) { _x.State.MenuOpen = false; _flow.ToMenu(); } else GetTree().Quit(); };
        }

        GetViewport().SizeChanged += Relayout;
        Relayout();
        GD.Print("GAME_READY");
        if (_flow != null && _args.TryGetValue("flow", out string? fs)) { _ = FlowStill(fs); return; }
        if (_args.ContainsKey("layout-test")) CallDeferred(nameof(RunLayoutTest));
        else if (_args.ContainsKey("parity-test")) _ = RunParityTest();
        else if (_args.TryGetValue("still", out string? still)) _ = Still(still);
        else if (_args.TryGetValue("playtest", out string? script)) StartPlaytest(script);
    }

    /// <summary>The scenario the game opens in: --scenario=id, or the first in data/battle/scenarios.json.</summary>
    private Scenario StartScenario(GameData data) =>
        _args.TryGetValue("scenario", out string? id) ? data.ScenarioById(id) : data.StartScenario;

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

    private void Restart() => Start(_x.Battle.Scenario);

    /// <summary>The mission files: every .json under data/missions/, as paths relative to data/.</summary>
    private static IEnumerable<string> MissionFiles()
    {
        var list = new List<string>();
        foreach (string f in DirAccess.GetFilesAt("res://data/missions"))
            if (f.EndsWith(".json")) list.Add("missions/" + f);
        return list;
    }

    /// <summary>Shows the battle (its 3D view and console) or hides it under the game flow's screens; hidden, it does not step.</summary>
    private void ShowBattle(bool on)
    {
        _view.Visible = on;
        _console.Visible = on;
        if (on) { _x.State.MenuOpen = false; _x.State.Paused = false; Relayout(); }
    }

    /// <summary>The console skin of a faction (the side a campaign is played from).</summary>
    private void UseSkin(string faction)
    {
        string id = _x.Data.Factions[faction].Skin ?? "concord";
        if (!_kits.TryGetValue(id, out IKit? kit)) { kit = Kits.For(id); KitPieces.Verify(kit); _kits[id] = kit; }
        _x.Kit = kit;
        _column.Kit = kit;
    }

    /// <summary>Starts a scenario from its beginning, in the skin of the side it is played from.</summary>
    private void Start(Scenario sc)
    {
        _x.Battle = new Battle(_x.Data, sc);
        Engine.TimeScale = 1;   // the damage range's slow motion ends with its scene
        _x.Kit = KitFor(_x.Data, sc);
        _column.Kit = _x.Kit;
        _x.State.Expanded = null;
        _view.Rebuild();
        _acc = 0;
        Relayout();
    }

    private readonly Dictionary<string, IKit> _kits = new();

    /// <summary>The console skin of the side a scenario is played from (its faction's skin; --skin=id overrides it, for
    /// the layout test), made and verified once per skin.</summary>
    private IKit KitFor(GameData data, Scenario sc)
    {
        string id = _args.GetValueOrDefault("skin") ?? data.Factions[sc.Side].Skin!;
        if (_kits.TryGetValue(id, out IKit? kit)) return kit;
        kit = Kits.For(id);
        KitPieces.Verify(kit);
        return _kits[id] = kit;
    }

    public override void _Process(double delta)
    {
        if (_x == null) return;
        _menu.Visible = _x.State.MenuOpen;
        _options.Visible = _x.State.OptionsOpen;
        _music.Play(_flow == null || _flow.Screen == FlowScreen.Battle ? "battle" : "main_theme");
        if (_x.State.Paused || _x.State.MenuOpen || _args.ContainsKey("still") || (_flow != null && _flow.Screen != FlowScreen.Battle)) return;
        double step = _x.Data.Tuning.StepS;
        _acc = Math.Min(_acc + delta, step * 6);
        while (_acc >= step) { _x.Battle.Step(); _acc -= step; }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_x == null || e.IsEcho() || (_flow != null && _flow.Screen != FlowScreen.Battle)) return;
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
        ConsoleFrame f = ConsoleLayout.Compute(_x.Data.Layout, size.X, size.Y, _x.State.Expanded, 1, _x.Kit.Style.StripTopH, _x.Kit.Style.StripBottomH);
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
        _menu.Position = Vector2.Zero;
        _menu.Size = size;
        _view.SetViewRect(new Rect2(V(v.X, v.Y), V(v.W, v.H)), size);
        _flow?.PlaceStrip(new Rect2(V(v.X, v.Y + 64), V(v.W, v.H)));
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

    /// <summary>Everything on the console that registers controls: the panels, the strips, the view and the menu.</summary>
    private IEnumerable<IControlSource> Sources() =>
        _panels.Values.Cast<IControlSource>().Append(_overlay).Append(_menu).Concat(_flow?.Sources() ?? Enumerable.Empty<IControlSource>());

    /// <summary>Starts the scripted captain on data/playtests/&lt;name&gt;.json; --play=&lt;scenario id&gt; is the scenario it picks
    /// from the scene selector (the playtest skill).</summary>
    private void StartPlaytest(string name)
    {
        var captain = new ScriptedCaptain();
        captain.Init(_x, Sources, name, ReadData($"playtests/{name}.json"), _args.GetValueOrDefault("play", _x.Battle.Scenario.Id), _flow);
        AddChild(captain);
    }

    /// <summary>The skin parity check (owner, 2026-11-04: "we need to make sure all UI elements are at parrity"; after
    /// tools/mockups/skin_parity.mjs for the mockups): one battle, played the same in every skin, through the same
    /// console states (every station, every ship page, each panel expanded, the menu, the scene list). In each state
    /// every skin must register the same controls, and every control must lie inside what draws it and inside the
    /// window. Prints PARITY_FAIL lines and PARITY_TEST PASS or FAIL.</summary>
    private async System.Threading.Tasks.Task RunParityTest()
    {
        var states = new List<(string Name, Action Set)> { ("battle", () => { }) };
        foreach (var st in StationPanel.Stations) { string id = st.Id; states.Add(("station." + id, () => _x.State.Station = id)); }
        foreach (var pg in ShipPanel.Pages) { string id = pg.Id; states.Add(("page." + id, () => _x.State.ShipPage = id)); }
        foreach (Region r in ConsoleLayout.Expandable) { Region e = r; states.Add(("expand." + e, () => _x.State.Expanded = e)); }
        states.Add(("menu", () => _x.State.MenuOpen = true));
        states.Add(("scene-list", () => _x.State.SceneOpen = true));
        string[] skins = { "concord", "krai" };
        var seen = new Dictionary<string, Dictionary<string, HashSet<string>>>();
        var fails = new List<string>();
        Scenario sc = _x.Battle.Scenario;
        foreach (string skin in skins)
        {
            _x.Kit = Kits.For(skin);
            KitPieces.Verify(_x.Kit);
            _column.Kit = _x.Kit;
            _x.Battle = new Battle(_x.Data, sc);
            _view.Rebuild();
            Battle b = _x.Battle;
            b.Lock(b.EnemiesOf(b.Player).First().Id);
            for (int i = 0; i < 600; i++) { b.Step(); if (i == 594) b.Fire(); }
            seen[skin] = new Dictionary<string, HashSet<string>>();
            foreach (var (name, set) in states)
            {
                _x.State.Station = "helm"; _x.State.ShipPage = "systems"; _x.State.Expanded = null; _x.State.MenuOpen = false; _x.State.SceneOpen = false;
                set();
                Relayout();
                _menu.Visible = _x.State.MenuOpen;
                for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var ids = new HashSet<string>();
                Rect2 window = GetViewport().GetVisibleRect().Grow(1);
                foreach (IControlSource src in Sources())
                {
                    Rect2 home = src.Screen.Grow(1);
                    foreach (var (id, r, _) in src.Controls())
                    {
                        ids.Add(id);
                        if (!home.Encloses(r)) fails.Add($"{skin} {name}: {id} at {r} spills out of its panel {src.Screen}");
                        else if (!window.Encloses(r)) fails.Add($"{skin} {name}: {id} at {r} is off the window");
                    }
                }
                seen[skin][name] = ids;
            }
        }
        foreach (var (name, _) in states)
        {
            HashSet<string> a = seen[skins[0]][name], k = seen[skins[1]][name];
            foreach (string id in a.Except(k).OrderBy(x => x)) fails.Add($"{name}: '{id}' in {skins[0]} but not in {skins[1]}");
            foreach (string id in k.Except(a).OrderBy(x => x)) fails.Add($"{name}: '{id}' in {skins[1]} but not in {skins[0]}");
        }
        int controls = seen.Values.Sum(d => d.Values.Sum(h => h.Count));
        foreach (string f in fails.Distinct()) GD.PrintErr("PARITY_FAIL " + f);
        GD.Print(fails.Count == 0 ? $"PARITY_TEST PASS ({states.Count} states, {controls} controls, {string.Join(" and ", skins)})" : $"PARITY_TEST FAIL ({fails.Distinct().Count()})");
        GetTree().Quit(fails.Count == 0 ? 0 : 1);
    }

    /// <summary>Every panel at its designed rectangle and clipped, at the reference, wider and narrower windows and expanded.</summary>
    private void RunLayoutTest()
    {
        var fails = new List<string>();
        LayoutDef l = _x.Data.Layout;
        foreach (Vector2 size in new[] { new Vector2(1920, 1080), new Vector2(2560, 1080), new Vector2(1500, 1080) })
            foreach (Region? ex in new Region?[] { null, Region.Station, Region.Ship, Region.Weapons })
            {
                _x.State.Expanded = ex;
                Relayout(size);
                ConsoleFrame want = ConsoleLayout.Compute(l, size.X, size.Y, ex, 1, _x.Kit.Style.StripTopH, _x.Kit.Style.StripBottomH);
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
        // a click lands where the player pointed: points of the battle plane drawn by ScreenOf come back through PlaneAt
        _view.Sync();
        double miss = _view.RayMissM();
        if (!(miss < 1.0)) fails.Add($"a click cast back onto the battle plane misses by {miss:0.0} m");
        foreach (string s in fails) GD.PrintErr("LAYOUT_FAIL " + s);
        GD.Print(fails.Count == 0 ? "LAYOUT_TEST PASS" : $"LAYOUT_TEST FAIL ({fails.Count})");
        GetTree().Quit(fails.Count == 0 ? 0 : 1);
    }

    private static Rect Pick(LayoutDef l, Region r) => r switch
    {
        Region.Plate => l.Plate, Region.Station => l.Station, Region.Ship => l.Ship, Region.Weapons => l.Weapons, _ => l.Radar,
    };

    /// <summary>Plays the battle for --steps (default 600) as the mockup's scripted shots do (target locked, intercepting), sets the console as asked
    /// (--station, --page, --expand, --reinforce, --menu, --paused, --scene-list, --zoom=factor, --target-view: the camera glided
    /// to the target), saves a PNG and quits. The window size is
    /// Godot's own --resolution flag.</summary>
    private async System.Threading.Tasks.Task Still(string path)
    {
        int steps = _args.TryGetValue("steps", out string? n) ? int.Parse(n) : 600;
        // the same set-up as the mockup's scripted shots: target locked, intercepting at full speed, a volley six
        // steps before the end
        Battle b = _x.Battle;
        b.Lock(_args.GetValueOrDefault("target", b.EnemiesOf(b.Player).First().Id));
        b.HelmOrder("intercept");
        b.SetSpeed(b.Rules.MaxSpeed(b.Player));
        for (int i = 0; i < steps; i++) { b.Step(); if (i == steps - 6) b.Fire(); }
        if (_args.TryGetValue("station", out string? st)) _x.State.Station = st;
        if (_args.TryGetValue("page", out string? pg)) _x.State.ShipPage = pg;
        if (_args.TryGetValue("expand", out string? ex) && Enum.TryParse(ex, true, out Region er)) _x.State.Expanded = er;
        if (_args.TryGetValue("reinforce", out string? rf)) b.Reinforce(int.Parse(rf));
        if (_args.ContainsKey("menu")) _x.State.MenuOpen = true;
        if (_args.ContainsKey("paused")) _x.State.Paused = true;
        if (_args.ContainsKey("scene-list")) _x.State.SceneOpen = true;
        if (_args.TryGetValue("zoom", out string? zm)) _view.Zoom(float.Parse(zm, System.Globalization.CultureInfo.InvariantCulture));
        bool tview = _args.ContainsKey("target-view");
        if (tview) _view.ToggleTargetView();
        // a glide takes BattleView.GlideS of real time: wait it out (and a little more) before the shot
        for (int i = 0; i < (tview ? 120 : 4); i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Relayout();
        for (int i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Image img = GetViewport().GetTexture().GetImage();
        Error err = img.SavePng(path);
        GD.Print(err == Error.Ok ? $"STILL {path} {img.GetWidth()}x{img.GetHeight()}" : $"STILL_FAIL {err}");
        GetTree().Quit(err == Error.Ok ? 0 : 1);
    }

    /// <summary>A still of a game flow screen (--flow=loading, menu, factions, skirmish, map, route, brief, accept, port, outfit, battle,
    /// debrief or debrief-battle; --side=concord or krai; --out=path.png), for the mockup comparison (CLAUDE.md section 12). It sets the
    /// screen up as a player would reach it, through the flow's own actions, waits for it to draw, saves and quits.</summary>
    private async System.Threading.Tasks.Task FlowStill(string what)
    {
        GameFlow f = _flow!;
        string side = _args.GetValueOrDefault("side", "concord"), path = _args.GetValueOrDefault("out", $"user://flow-{what}.png");
        async System.Threading.Tasks.Task Frames(int n) { for (int i = 0; i < n; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
        async System.Threading.Tasks.Task Settle() { do await Frames(5); while (f.Busy); }
        // the loading screen is shot part way through the boot; everything else once the menu is up
        if (what == "loading") await Frames(12); else await f.Booted;
        Offer? Find(string script) => f.Camp!.S.Missions.FirstOrDefault(m => m.Script == script) ?? f.Camp.OfferMission(script);
        void FlyTo(Offer m) { f.Camp!.S.AtC = m.C; f.Camp.S.AtR = m.R; f.SelectHex((m.C, m.R)); }
        switch (what)
        {
            case "menu": break;
            case "options": _x.State.OptionsOpen = true; break;
            case "factions": await f.Show(FlowScreen.Factions); f.Picked = side; await Frames(90); break;
            case "skirmish": await f.Show(FlowScreen.Skirmish); break;
            case "loading": break;
            default:
                await f.NewCampaign(side);
                Campaign.Campaign c = f.Camp!;
                if (what == "route") f.SelectHex((c.S.AtC + 7, c.S.AtR + 1));
                // a course set three turns out: its first leg flown, the rest dotted, the line kept
                if (what == "course") { f.SelectHex((c.S.AtC + 9, c.S.AtR + 1)); f.MoveHere(); await Frames(200); }
                if (what == "accept")
                {
                    Offer m = c.S.Missions.Where(x => !c.ScriptOf(x).Story && c.PathTo(x.C, x.R) is { Turns: > 1 }).OrderBy(x => x.Id).FirstOrDefault() ?? Find("prize")!;
                    f.SelectHex((m.C, m.R));
                    f.OpenBrief(m);
                    f.Accept(m);
                    _flow!.MapCentre(m.C, m.R);
                }
                if (what == "map-far") { f.MapCentre(15, 11); f.MapView(64, null); }
                if (what == "map-close") { f.MapCentre(c.S.AtC + 1, c.S.AtR - 1); f.MapView(6.5f, null); }
                if (what == "log") { Offer m = Find("shakedown")!; c.Accept(m); f.MapView(null, true); }
                if (what == "turns") { for (int k = 0; k < 3; k++) f.EndTurn(); f.MapCentre(16, 10); f.MapView(20, null); }
                if (what == "brief") { Offer m = Find("prize")!; f.SelectHex((m.C, m.R)); f.OpenBrief(m); }
                if (what is "port" or "outfit" or "supply" or "crew")
                {
                    c.S.Credits += 6000;
                    c.Buy(c.Data.HullsOf(side).Skip(1).First().Id);
                    c.Refit(c.S.Fleet[0], "primary", 0, null);
                    c.S.Fleet[0].HullPct = 72;
                    if (what == "crew") c.Hire(c.S.OfficerPool[0]);
                    f.Port = what switch { "port" => "yard", "outfit" => "outfit", "supply" => "supply", _ => "crew" };
                }
                if (what == "market")
                {
                    Sector cal = c.PortNamed("Calder")!;
                    c.S.AtC = cal.C; c.S.AtR = cal.R;
                    c.S.Credits += 6000;
                    f.SelectHex((cal.C, cal.R));
                    f.Port = "market";
                }
                if (what == "debrief") { Offer m = Find("shakedown")!; FlyTo(m); f.Launch(m); await Settle(); }
                if (what is "battle" or "debrief-battle")
                {
                    Offer m = Find("patrol")!;
                    FlyTo(m);
                    f.Launch(m);
                    await Settle();
                    Battle b = _x.Battle;
                    b.Lock(b.EnemiesOf(b.Player).First().Id);
                    b.HelmOrder("intercept");
                    b.SetSpeed(b.Rules.MaxSpeed(b.Player));
                    for (int i = 0; i < 600; i++) b.Step();
                    if (what == "debrief-battle") { b.Withdraw(); while (f.Screen == FlowScreen.Battle) await Frames(10); await Settle(); }
                }
                break;
        }
        await Frames(30);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Image img = GetViewport().GetTexture().GetImage();
        Error err = img.SavePng(path);
        GD.Print(err == Error.Ok ? $"STILL {path} {img.GetWidth()}x{img.GetHeight()}" : $"STILL_FAIL {err}");
        GetTree().Quit(err == Error.Ok ? 0 : 1);
    }
}
