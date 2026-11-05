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
    public Offer? Brief;
    public string? Port;
    public string Msg = "";
    /// <summary>The faction picked on the faction screen, waiting for New or Continue.</summary>
    public string? Picked;
    /// <summary>The ship chosen in the port's outfitter and supplies.</summary>
    public int PortShip;

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
    private MissionStrip _strip = null!;

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
        Add(FlowScreen.Factions, Panel(new FactionScreen()));
        Add(FlowScreen.Skirmish, Panel(new SkirmishScreen()));
        Add(FlowScreen.Debrief, Panel(new DebriefScreen()));
        _strip = Panel(new MissionStrip());
        _strip.MouseFilter = Control.MouseFilterEnum.Stop;
        _layer.AddChild(_strip);
        Go(FlowScreen.Menu);
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
        Screen = s;
        foreach (var (k, c) in _screens) c.Visible = k == s;
        _strip.Visible = s == FlowScreen.Battle && Running != null;
        _showBattle(s == FlowScreen.Battle);
        if (s == FlowScreen.Map) _map.Refresh();
    }

    /// <summary>Where the mission strip sits: the top left of the battle view.</summary>
    public void PlaceStrip(Rect2 view)
    {
        int n = Plan?.Battle.Objectives.Count ?? 0;
        _strip.Position = view.Position + new Vector2(16, -8);
        _strip.Size = new Vector2(Math.Min(470, view.Size.X - 32), 92 + 24 * n);
    }

    // ---- Campaign ---------------------------------------------------------------------------------------------------

    public static string SavePath(string side) => $"user://campaign-{side}.json";
    public bool HasSave(string side) => FileAccess.FileExists(SavePath(side));

    public void NewCampaign(string side)
    {
        Camp = Campaign.Campaign.New(Data, side);
        Camp.S.Log.Add($"Turn 1: {Camp.S.Fleet[0].Name} takes her first command at {Camp.Home.PortName}.");
        Begin();
    }

    public void ContinueCampaign(string side)
    {
        using var f = FileAccess.Open(SavePath(side), FileAccess.ModeFlags.Read);
        try { Camp = Campaign.Campaign.Load(Data, f.GetAsText()); }
        catch (Exception e) when (e is FormatException or System.Text.Json.JsonException)
        {
            Msg = $"The saved campaign could not be read ({e.Message}). Start a new one.";
            return;
        }
        Begin();
    }

    private void Begin()
    {
        _useSkin(Data.Factions[Camp!.S.Side].Id);
        Sel = (Camp.S.AtC, Camp.S.AtR);
        Path = null; Brief = null; Port = null; Msg = "";
        _map.NewMap(Camp);
        Save();
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
        _map.Map?.ShowRoute(Path);
    }

    /// <summary>Flies this turn's leg of the route to the selected sector; a longer route becomes the fleet's order.</summary>
    public void MoveHere()
    {
        if (Camp == null || Path == null) return;
        if (Path.Legs[0].Count == 0) { Msg = "No moves left this turn to make the first step. End the turn."; return; }
        var steps = Camp.Move(Path);
        Msg = Camp.S.OrderC != null ? $"Under way: {Path.Turns - 1} more turn{(Path.Turns > 2 ? "s" : "")} to go." : "";
        _map.Map?.Rebuild(null, steps);
        SelectHex(Sel);
        Save();
    }

    public void EndTurn()
    {
        if (Camp == null) return;
        var moves = Camp.EndTurn();
        List<(int, int)>? own = null;
        if (Camp.OrderRoute() is { } next && next.Legs[0].Count > 0) own = Camp.Move(next);
        _map.Map?.Rebuild(moves, own);
        Msg = $"Turn {Camp.S.Turn}.";
        SelectHex(Sel);
        Save();
    }

    public void OpenBrief(Offer m) { Brief = m; Port = null; }

    /// <summary>Starts the briefed mission here: its battle on the console, or done on arrival.</summary>
    public void Launch(Offer m)
    {
        if (Camp == null) return;
        if (Camp.Needs(m) is { } why) { Msg = why; return; }
        Camp.Accept(m);
        MissionPlan? plan = MissionRun.Plan(Camp, m, X.Data);
        if (plan == null)
        {
            Last = MissionRun.Finish(Camp, m, null, null);
            Brief = null;
            Save();
            Go(FlowScreen.Debrief);
            return;
        }
        Running = m;
        Plan = plan;
        Skirmish = false;
        _overT = 0;
        Brief = null;
        X.State.InMission = true;
        X.State.SceneOpen = false;
        _startBattle(plan.Scenario);
        Go(FlowScreen.Battle);
    }

    public void StartSkirmish(Scenario sc)
    {
        Running = null;
        Plan = null;
        Skirmish = true;
        X.State.InMission = false;
        _overT = 0;
        _startBattle(sc);
        Go(FlowScreen.Battle);
    }

    public void Withdraw() => X.Battle.Withdraw();

    /// <summary>Each frame while a battle plays: a mission whose own fleet is gone is lost; a battle that is over goes to
    /// the debrief a moment later, so the last hit is seen.</summary>
    public override void _Process(double delta)
    {
        if (Screen != FlowScreen.Battle) return;
        Sim.Battle b = X.Battle;
        if (Plan != null && b.Over == null && Plan.Groups["fleet"].All(id => b.ShipById(id) is not { Alive: true })) b.Lose();
        if (b.Over == null) return;
        _overT += delta;
        if (_overT < 2.5) return;
        if (Running != null && Plan != null && Camp != null)
        {
            Last = MissionRun.Finish(Camp, Running, Plan, b);
            Running = null;
            Plan = null;
            Save();
        }
        else Last = new Debrief { Mission = b.Scenario.Name, Grade = b.Over == "won" ? "victory" : b.Over == "withdrawn" ? "draw" : "defeat",
            Win = b.Over == "won", How = MissionRun.HowOf(b) };
        Go(FlowScreen.Debrief);
    }

    /// <summary>From the debrief: back to the map (a campaign) or the skirmish list.</summary>
    public void AfterDebrief()
    {
        if (Skirmish || Camp == null) { Skirmish = false; Go(FlowScreen.Skirmish); return; }
        _map.Map?.Rebuild();
        SelectHex((Camp.S.AtC, Camp.S.AtR));
        Go(FlowScreen.Map);
    }

    public void ToMenu()
    {
        Save();
        _useSkin("concord");
        Go(FlowScreen.Menu);
    }
}
