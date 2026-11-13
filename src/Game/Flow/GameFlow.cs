// GameFlow: the primary game flow in the game (mockup 1, docs/mockups/game-flow.html v14; owner, 2026-11-05: "implement game
// flow into the game, but I want you to implmenent missions as well"). The main menu, the faction screen, the skirmish list,
// the campaign map with its sector panel, briefing and port, the mission battle (the battle console, with the mission's
// objectives and a Withdraw key) and the debrief. It holds which screen is up and the campaign being played; the campaign's
// rules are src/Core/Campaign, the battle is Main's console, and the screens draw only kit pieces (FlowScreens.cs). The
// composition root (Main) wires it and hands it the battle: StartBattle plays a scenario on the console, ShowBattle swaps
// between the console and the flow's screens, UseSkin sets the console skin of the side being played.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FleetCommand.Campaign;
using FleetCommand.Data;
using FleetCommand.Game.Ui;
using Godot;

namespace FleetCommand.Game.Flow;

public enum FlowScreen { Menu, Factions, Skirmish, Map, Battle, Debrief }

public sealed partial class GameFlow : Node
{
    public ConsoleContext X { get; private set; } = null!;
    public CampaignData Data { get; private set; } = null!;
    public FlowScreen Screen { get; private set; } = FlowScreen.Menu;
    public Campaign.Campaign? Camp { get; private set; }

    // the campaign map's selection, route preview, open briefing and port tab, and a line of feedback
    public (int C, int R)? Sel;
    public Route? Path;
    /// <summary>The course set with Move here, from where it was set this turn, drawn until the fleet arrives.</summary>
    public (Route Route, (int C, int R) From)? Course;
    public Offer? Brief;
    public string? Port;
    public string Msg = "";
    /// <summary>The faction picked on the faction screen, waiting for New or Continue.</summary>
    public string? Picked;
    /// <summary>The ship chosen in the port (its loadout, supplies), the hull looked at in the shipyard, and the hardpoint
    /// picked in the loadout (group and index).</summary>
    public int PortShip;
    public string? YardHull;
    public string LoGroup = "primary";
    public int LoSlot;
    /// <summary>The crew station whose list of officers on offer is open.</summary>
    public string? CrewStation;
    /// <summary>The skill picked on the crew tab's tree (its id), shown with Learn beside the tree.</summary>
    public string? CrewSkill;

    // the battle under way: the mission and its plan, or a skirmish
    public Offer? Running { get; private set; }
    public MissionPlan? Plan { get; private set; }
    public bool Skirmish { get; private set; }
    public Debrief? Last { get; private set; }
    private double _overT;

    private Action<Scenario> _startBattle = null!;
    private Action<bool> _showBattle = null!;
    private Action<string> _useSkin = null!;
    private CanvasLayer _layer = null!;
    private MapScreen _map = null!;
    private readonly Dictionary<FlowScreen, Control> _screens = new();
    // the faction screen's keys, over its 3D stage inside FactionView
    private FactionScreen _factionScreen = null!;
    private MissionStrip _strip = null!;
    private LoadingScreen _loading = null!;
    private ColorRect _fade = null!;
    private bool _busy;
    /// <summary>Done once the boot's loading (ships, pictures, shaders) has finished and the menu is up.</summary>
    public Task Booted { get; private set; } = Task.CompletedTask;
    /// <summary>True while a screen change or a loading screen is under way.</summary>
    public bool Busy => _busy;
    /// <summary>A screen change: the screen fades to black, changes, and fades back.</summary>
    public const float FadeOutS = 0.18f, FadeInS = 0.28f;

    public void Init(ConsoleContext x, CampaignData data, Action<Scenario> startBattle, Action<bool> showBattle, Action<string> useSkin)
    {
        X = x;
        Data = data;
        _startBattle = startBattle;
        _showBattle = showBattle;
        _useSkin = useSkin;
        Name = "GameFlow";
        _layer = new CanvasLayer { Name = "Flow", Layer = 4 };
        AddChild(_layer);
        _map = new MapScreen();
        _map.Init(this);
        Add(FlowScreen.Map, _map);
        Add(FlowScreen.Menu, Panel(new MenuScreen()));
        var factions = new FactionView();
        factions.Init(_factionScreen = Panel(new FactionScreen()));
        Add(FlowScreen.Factions, factions);
        Add(FlowScreen.Skirmish, Panel(new SkirmishScreen()));
        Add(FlowScreen.Debrief, Panel(new DebriefScreen()));
        _strip = Panel(new MissionStrip());
        _strip.MouseFilter = Control.MouseFilterEnum.Stop;
        _layer.AddChild(_strip);
        // over everything: the loading screen, and the black the screens fade through
        var top = new CanvasLayer { Name = "Fade", Layer = 20 };
        AddChild(top);
        _loading = Panel(new LoadingScreen());
        _loading.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _loading.Visible = false;
        top.AddChild(_loading);
        _fade = new ColorRect { Color = new Color(0, 0, 0, 1), MouseFilter = Control.MouseFilterEnum.Stop, Name = "Black" };
        _fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        top.AddChild(_fade);
        Go(FlowScreen.Menu);
        Booted = Boot();
    }

    // the boot: the loading screen while the ships load and the shaders compile, then the menu fades in
    private async Task Boot()
    {
        ulong t0 = Time.GetTicksMsec();
        await Loading("Fleet Command", async report => await Prewarm.Run(this, X.Data, report), fadeOut: false);
        GD.Print($"FLOW_BOOT {Time.GetTicksMsec() - t0} ms (ships, pictures and shaders)");
        await FadeIn();
    }

    private async Task Tween(float to, float s)
    {
        Tween t = CreateTween();
        t.TweenProperty(_fade, "color:a", to, s).SetTrans(Godot.Tween.TransitionType.Sine).SetEase(Godot.Tween.EaseType.InOut);
        await ToSignal(t, Godot.Tween.SignalName.Finished);
    }

    private async Task FadeOut() { _fade.MouseFilter = Control.MouseFilterEnum.Stop; await Tween(1, FadeOutS); }

    private async Task FadeIn()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await Tween(0, FadeInS);
        _fade.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    /// <summary>Fades to another screen (owner, 2026-11-05: "make transitions between menus smoother"); a change already
    /// under way is not interrupted.</summary>
    public async Task Show(FlowScreen s, Action? change = null)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            await FadeOut();
            change?.Invoke();
            Go(s);
            await FadeIn();
        }
        finally { _busy = false; }
    }

    /// <summary>The loading screen over a slow change: it fades in, the work reports its progress (0 to 1) and step as it
    /// goes, and the screen stays black when it is done, for the caller to fade the next screen in.</summary>
    private async Task Loading(string title, Func<Action<float, string>, Task> work, bool fadeOut = true)
    {
        if (fadeOut) await FadeOut();
        _loading.Reset(title);
        _loading.Visible = true;
        await Tween(0, 0.15f);
        await work((p, step) => { _loading.Target = p; _loading.Step = step; });
        _loading.Target = 1;
        while (_loading.Shown < 1) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await Tween(1, FadeOutS);
        _loading.Visible = false;
    }

    // one step of a loading screen: report it, let the bar draw, then do it
    private async Task Step(Action<float, string> report, float p, string step, Action work)
    {
        report(p, step);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        work();
    }

    private T Panel<T>(T p) where T : FlowPanel { p.Bind(this); return p; }

    private void Add(FlowScreen s, Control c)
    {
        c.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _layer.AddChild(c);
        _screens[s] = c;
    }

    /// <summary>Shows a screen; the battle console shows only for Battle.</summary>
    public void Go(FlowScreen s)
    {
        // the debrief stands over what it follows (owner, 2026-11-05: "background shouldnt disappear on mission win screen"):
        // the battle as it ended, frozen, or the map for a mission done on arrival
        FlowScreen behind = s == FlowScreen.Debrief ? (Screen == FlowScreen.Battle ? FlowScreen.Battle : FlowScreen.Map) : s;
        Screen = s;
        X.State.OnMap = s == FlowScreen.Map;
        if (s != FlowScreen.Map && s != FlowScreen.Battle) X.State.MenuOpen = false;
        foreach (var (k, c) in _screens) c.Visible = k == s || (k == behind && k != FlowScreen.Battle);
        _strip.Visible = s == FlowScreen.Battle && Running != null && X.State.GoalsShown;
        _showBattle(behind == FlowScreen.Battle);
        if (s == FlowScreen.Map) _map.Refresh();
    }

    /// <summary>Where the mission strip sits: the top left of the battle view.</summary>
    public void PlaceStrip(Rect2 view)
    {
        int n = Plan?.Battle.Objectives.Count ?? 0;
        _strip.Position = view.Position + new Vector2(16, -8);
        _strip.Size = new Vector2(Math.Min(470, view.Size.X - 32), 52 + 24 * n);
    }

    // ---- Campaign ---------------------------------------------------------------------------------------------------

    /// <summary>Set by a scripted run, so a playtest's campaign saves beside the player's own and never over it.</summary>
    public static string SaveTag = "";
    public static string SavePath(string side) => $"user://campaign-{SaveTag}{side}.json";

    /// <summary>The flow's controls on screen now (the scripted captain clicks them as a player would).</summary>
    public IEnumerable<Ui.IControlSource> Sources()
    {
        var all = new List<Control>(_screens.Values) { _strip, _loading, _factionScreen };
        all.AddRange(_map.Panels);
        return all.Where(c => c.IsVisibleInTree()).OfType<Ui.IControlSource>();
    }

    /// <summary>The campaign map's 3D view (the scripted captain finds a sector's place on screen there, as a player sees it).</summary>
    public CampaignMap? MapView3D => _map.Map;
    public bool HasSave(string side) => FileAccess.FileExists(SavePath(side));

    public async Task NewCampaign(string side)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            await Loading("New campaign", async report =>
            {
                await Step(report, 0.1f, "Charting the sectors", () =>
                {
                    Camp = Campaign.Campaign.New(Data, side, unfinished: GameSettings.UnfinishedFactions);
                    Camp.S.Log.Add($"Turn 1: {Camp.S.Fleet[0].Name} takes her first command at {Camp.Home.PortName}.");
                });
                await Begin(report);
            });
            await FadeIn();
        }
        finally { _busy = false; }
    }

    public async Task ContinueCampaign(string side)
    {
        if (_busy) return;
        Campaign.Campaign loaded;
        using (var f = FileAccess.Open(SavePath(side), FileAccess.ModeFlags.Read))
            try { loaded = Campaign.Campaign.Load(Data, f.GetAsText()); }
            catch (Exception e) when (e is FormatException or System.Text.Json.JsonException)
            {
                Msg = $"The saved campaign could not be read ({e.Message}). Start a new one.";
                return;
            }
        _busy = true;
        try
        {
            await Loading("Continue campaign", async report => { Camp = loaded; await Begin(report); });
            await FadeIn();
        }
        finally { _busy = false; }
    }

    private async Task Begin(Action<float, string> report)
    {
        await Step(report, 0.35f, "Setting the console", () =>
        {
            _useSkin(Data.Factions[Camp!.S.Side].Id);
            Sel = (Camp.S.AtC, Camp.S.AtR);
            Path = null; Brief = null; Port = null; Msg = "";
            Course = Camp.OrderRoute() is { } o ? (o, (Camp.S.AtC, Camp.S.AtR)) : null;
        });
        await Step(report, 0.55f, "Placing ports, fleets and freighters", () => _map.NewMap(Camp!));
        await Step(report, 0.9f, "Saving", Save);
        Go(FlowScreen.Map);
    }

    public void Save()
    {
        if (Camp == null) return;
        using var f = FileAccess.Open(SavePath(Camp.S.Side), FileAccess.ModeFlags.Write);
        f?.StoreString(Camp.Save());
    }

    public void SelectHex((int C, int R)? at)
    {
        Sel = at;
        Brief = null;
        Path = at is { } a && Camp != null ? Camp.PathTo(a.C, a.R) : null;
        _map.Map?.Select(at);
        _map.Map?.ShowRoute(Path, Course);
    }

    /// <summary>Flies this turn's leg of the route to the selected sector; a longer route becomes the fleet's order.</summary>
    public void MoveHere()
    {
        if (Camp == null || Path == null) return;
        Course = (Path, (Camp.S.AtC, Camp.S.AtR));
        // with no moves left the course is still set: it is the fleet's order, flown from the next End turn (owner,
        // 2026-11-07: "pressing the move button if move is already exhasuted wont queue up move for the next turns")
        bool none = Path.Legs[0].Count == 0;
        var steps = Camp.Move(Path);
        Msg = none ? $"Course set: under way at End turn, {Path.Turns - 1} turn{(Path.Turns > 2 ? "s" : "")} to go."
            : Camp.S.OrderC != null ? $"Under way: {Path.Turns - 1} more turn{(Path.Turns > 2 ? "s" : "")} to go." : "";
        if (none) { SelectHex(Sel); Save(); return; }
        _map.Map?.Rebuild(null, steps);
        SelectHex(Sel);
        Arrived();
        Save();
    }

    public void EndTurn()
    {
        if (Camp == null) return;
        var moves = Camp.EndTurn();
        List<(int, int)>? own = null;
        Course = null;
        if (Camp.OrderRoute() is { } next && next.Legs[0].Count > 0) { Course = (next, (Camp.S.AtC, Camp.S.AtR)); own = Camp.Move(next); }
        _map.Map?.Rebuild(moves, own);
        Msg = $"Turn {Camp.S.Turn}.";
        SelectHex(Sel);
        if (own != null) Arrived();
        else Hunted();
        Save();
    }

    public void OpenBrief(Offer m) { Brief = m; Port = null; }

    /// <summary>Centres the map's camera on a sector (the stills; the player's own Fleet key does the same for the fleet).</summary>
    public void MapCentre(int c, int r) => _map.Map?.CentreOn(c, r);

    /// <summary>Sets the map camera's distance and opens or closes the mission log (the stills).</summary>
    public void MapView(float? dist, bool? log)
    {
        if (dist is { } d && _map.Map != null) _map.Map.Dist = d;
        if (log is { } l) _map.Hud.LogOpen = l;
    }

    /// <summary>Accepts a mission and sets course for its sector (owner, 2026-11-05: "if you accept a mission your ship auto
    /// navigates there"): the fleet flies this turn's leg, and the rest is its order, flown on after each End turn. The
    /// briefing opens again when the fleet arrives (Arrived).</summary>
    public void Accept(Offer m)
    {
        if (Camp == null) return;
        Camp.Accept(m);
        bool here = Camp.S.AtC == m.C && Camp.S.AtR == m.R;
        if (here) return;
        Brief = null;
        SelectHex((m.C, m.R));
        if (Path != null) MoveHere();
    }

    /// <summary>Draws the map again from the campaign as it stands (its fleets, freighters and missions).</summary>
    public void Redraw() => _map.Refresh();

    /// <summary>Meets a freighter in the fleet's sector: her encounter (an escort, or a raid) opens as a briefing to launch.</summary>
    public void Meet(Freighter f)
    {
        if (Camp?.Meet(f) is not { } m) return;
        Save();
        OpenBrief(m);
    }

    /// <summary>Cashes out the cargo of the freighters taken, at a port that deals with you.</summary>
    public void CashOut()
    {
        if (Camp == null) return;
        int before = Camp.S.Credits;
        Msg = Camp.CashOut() ?? $"Cargo sold for {Camp.S.Credits - before:N0} cr.";
        Save();
        Redraw();
    }

    /// <summary>Patches the most damaged ship with a repair kit.</summary>
    public void FieldRepair()
    {
        if (Camp?.S.Fleet.OrderBy(s => s.HullPct).FirstOrDefault() is not { } s) return;
        Msg = Camp.FieldRepair(s) ?? $"The {s.Name} is patched: hull {s.HullPct:0} %.";
        Save();
    }

    // an accepted mission in the sector the fleet has reached opens its briefing (as the mockup does on arrival)
    private void Arrived()
    {
        if (Camp?.MissionAt(Camp.S.AtC, Camp.S.AtR) is { } m && Camp.S.Active.Contains(m.Id)) OpenBrief(m);
        Hunted();
    }

    // hunters in the fleet's sector catch it: their fight's briefing opens (fight, or slip away by moving on)
    private void Hunted()
    {
        if (Camp?.Caught() is { Count: > 0 } caught)
        {
            OpenBrief(caught[0]);
            Msg = $"Caught by the {Camp.HuntersOf(caught[0])?.Name ?? "hunters"}!";
        }
    }

    /// <summary>Starts the briefed mission here: its battle on the console, or done on arrival.</summary>
    public void Launch(Offer m)
    {
        if (Camp == null) return;
        if (Camp.Needs(m) is { } why) { Msg = why; return; }
        Camp.Accept(m);
        MissionPlan? plan = MissionRun.Plan(Camp, m, X.Data);
        if (plan == null)
        {
            Brief = null;
            _ = Show(FlowScreen.Debrief, () => { Last = MissionRun.Finish(Camp, m, null, null); Save(); });
            return;
        }
        Running = m;
        Plan = plan;
        Skirmish = false;
        _overT = 0;
        Brief = null;
        X.State.InMission = true;
        X.State.SceneOpen = false;
        X.State.Goals = Goals;
        _ = Battle(plan.Scenario);
    }

    public void StartSkirmish(Scenario sc)
    {
        Running = null;
        Plan = null;
        Skirmish = true;
        X.State.InMission = false;
        X.State.Goals = null;
        _overT = 0;
        _ = Battle(sc);
    }

    // the battle behind the loading screen: the scenario built, its ships placed and dressed, then the console fades in
    private async Task Battle(Scenario sc)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            await Loading(sc.Name, async report =>
            {
                await Step(report, 0.2f, "Plotting the engagement", () => { });
                await Step(report, 0.6f, "Bringing the ships in", () =>
                {
                    _startBattle(sc);
                    // a mission's flagship is manned by the campaign crew: seated in its slots, and their learned skills (change crew-leveling)
                    if (Plan != null && Camp != null && Camp.S.Fleet.Count > 0 && Plan.FleetShips.FirstOrDefault(kv => kv.Value == Camp.S.Fleet[0].Id).Key is { } flag)
                    {
                        X.Battle.SeatCrew(flag, Camp.CrewSeats());
                        X.Battle.ManCrew(flag, Camp.CrewBonus());
                    }
                });
                await Step(report, 0.9f, "Manning the console", () => Go(FlowScreen.Battle));
            });
            await FadeIn();
        }
        finally { _busy = false; }
    }

    public void Withdraw() => X.Battle.Withdraw();

    /// <summary>The running mission's goals as they stand (MissionRun.MetNow, the rule the debrief grades by), for the goals
    /// over the view and the Comms station.</summary>
    private IReadOnlyList<Ui.GoalLine> Goals()
    {
        if (Plan is not { } p || Camp is not { } c) return Array.Empty<Ui.GoalLine>();
        var met = MissionRun.MetNow(c, p, X.Battle);
        return p.Battle.Objectives.Select(o => new Ui.GoalLine(o.Text.Eval(new OfferScope(c, p.Mission)), o.Primary, met.GetValueOrDefault(o.Id))).ToList();
    }

    /// <summary>The longest a finished battle waits for a ship's end to play before the debrief, seconds.</summary>
    private const double EndWaitMaxS = 10;

    /// <summary>Each frame while a battle plays: a mission whose own fleet is gone is lost; a battle that is over goes to
    /// the debrief a moment later, so the last hit is seen, and once the last ship's end has played (its breach and nova, up
    /// to <see cref="EndWaitMaxS"/>), so the battle's finale is not cut off.</summary>
    public override void _Process(double delta)
    {
        // the goals over the view: a mission's, until their X shuts them (Comms shows them again)
        _strip.Visible = Screen == FlowScreen.Battle && Running != null && X.State.GoalsShown;
        if (Screen != FlowScreen.Battle) return;
        Sim.Battle b = X.Battle;
        if (Plan != null && b.Over == null && Plan.Groups["fleet"].All(id => b.ShipById(id) is not { Alive: true })) b.Lose();
        // a battle with marker beacons ends when every primary objective is met, not when the last enemy goes
        if (Plan != null && Camp != null && b.Over == null && Plan.Scenario.EndsOnObjectives)
        {
            var met = MissionRun.MetNow(Camp, Plan, b);
            if (Plan.Battle.Objectives.Where(o => o.Primary).All(o => met.GetValueOrDefault(o.Id))) b.Win();
        }
        if (b.Over == null) return;
        _overT += delta;
        if (_overT < 2.5 || (X.State.ShipsEnding && _overT < EndWaitMaxS)) return;
        if (_busy) return;
        if (Running != null && Plan != null && Camp != null)
        {
            Last = MissionRun.Finish(Camp, Running, Plan, b);
            Running = null;
            Plan = null;
            Save();
        }
        else Last = new Debrief { Mission = b.Scenario.Name, Grade = b.Over == "won" ? "victory" : b.Over is "withdrawn" or "jumped" or "stalemate" ? "draw" : "defeat",
            Win = b.Over == "won", How = MissionRun.HowOf(b) };
        _ = Show(FlowScreen.Debrief);
    }

    /// <summary>From the debrief: back to the map (a campaign) or the skirmish list.</summary>
    public void AfterDebrief()
    {
        if (Skirmish || Camp == null) { _ = Show(FlowScreen.Skirmish, () => Skirmish = false); return; }
        _ = Show(FlowScreen.Map, () =>
        {
            _map.Map?.Rebuild();
            SelectHex((Camp.S.AtC, Camp.S.AtR));
            Arrived();
        });
    }

    public void ToMenu() => _ = Show(FlowScreen.Menu, () => { Save(); _useSkin("concord"); });
}
