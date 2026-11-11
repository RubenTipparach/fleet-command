// Main.Coop: the composition root's co-op wiring (openspec/changes/bridge-crew-coop). --host[=port] makes this game the
// admiral's: crew members join it, and every battle it starts is started on theirs too. --join=address[:port] --name=...
// makes it a crew member's: it waits on the bridge crew sheet until the admiral starts a battle, builds the same battle
// (a skirmish by its id, a campaign mission from the admiral's campaign and the mission's id, MissionRun.Plan), and plays it
// in step with the admiral's (Coop.Lockstep). Every peer steps its own battle; only orders cross the network.
using System;
using System.Collections.Generic;
using FleetCommand.ConsoleModel;
using System.Linq;
using System.Text.Json.Nodes;
using FleetCommand.Campaign;
using FleetCommand.Coop;
using FleetCommand.Data;
using FleetCommand.Game.Net;
using FleetCommand.Game.Ui;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game;

public sealed partial class Main
{
    private CoopNet? _net;
    private CrewCard _crew = null!;
    private ChatBox _chat = null!;
    private CampaignData _campData = null!;
    private bool _pauseSent;
    // a crew peer: the battle's end seen, and how long ago
    private double _crewOverT;

    /// <summary>Whether this game is a co-op crew member's (it has no game flow of its own: the admiral's runs it).</summary>
    private bool IsCrew => _args.ContainsKey("join");

    // sets up the crew sheet, and the connection when --host or --join is given
    private void SetupCoop(CampaignData camp)
    {
        _campData = camp;
        var layer = new CanvasLayer { Name = "Crew", Layer = 11 };
        AddChild(layer);
        _crew = new CrewCard { Name = "CrewCard", Visible = false };
        _crew.Init(_x, Region.Station);
        _crew.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(_crew);
        _chat = new ChatBox { Name = "ChatBox", Visible = false };
        _chat.Init(_x, Region.Station);
        layer.AddChild(_chat);
        bool host = _args.TryGetValue("host", out string? port);
        if (!host && !IsCrew) return;
        string name = _args.GetValueOrDefault("name", host ? "Admiral" : "Crew")!;
        var ls = new Lockstep(new Crew(_x.Data.CoopRoles!, name), host)
        {
            Note = line => GD.Print(line),
            OpenTab = () => "st." + _x.State.Station,
            Clock = () => (long)Time.GetTicksMsec(),
        };
        // every crew event goes into the battle's log too, in its sender's name (the log is no part of the checksum)
        ls.OnEvent = e =>
        {
            if (ls.Battle == null) return;
            string who = e.Kind switch { "chat" => "Chat", "ping" => "Ping", "directive" or "ack" => "Orders", _ => "Crew" };
            ls.Battle.SayAs(ls.Crew.NameOf(e.By), who, e.Text);
        };
        // the window says whose seat it is, so a recorder (and a player with two windows) can tell them apart
        GetWindow().Title = $"Fleet Command · {(host ? "Admiral" : "Crew")} {name}";
        _net = new CoopNet { Name = "CoopNet" };
        AddChild(_net);
        _x.Coop = ls;
        if (host)
        {
            int p = int.TryParse(port, out int pp) && pp > 0 ? pp : CoopNet.DefaultPort;
            if (_net.Host(ls, p) != Error.Ok) { GD.PushError($"COOP_HOST cannot listen on {p}"); return; }
            _crew.Hosting = $"{LanAddress()}:{p}";
        }
        else
        {
            if (_net.Join(ls, _args["join"], name) != Error.Ok) { GD.PushError($"COOP_JOIN cannot reach {_args["join"]}"); return; }
            _net.OnFlow = CrewFlow;
            _net.OnLost = () => { ls.Lost = true; _crew.Status = "The connection to the admiral was lost."; Waiting(true); };
            _crew.Status = $"Joining {_args["join"]}...";
            ls.CrewChanged = () => { if (_net.Connected && ls.Battle == null) _crew.Status = "Waiting for the admiral to start a battle."; };
            Waiting(true);
        }
    }

    /// <summary>Places the chat box: over the view in a battle, over the whole screen elsewhere; shown in co-op only.</summary>
    private void PlaceChat(Rect2 view, Vector2 screen)
    {
        if (_chat == null) return;
        bool battle = !_crew.Lobby && (_flow == null || _flow.Screen == Flow.FlowScreen.Battle) && _x.Coop?.Battle != null;
        _chat.Visible = _x.Coop != null;
        // only its own corner (bottom left, above the view's message lines), so it never takes a click meant for the view
        Rect2 area = battle ? view : new Rect2(Vector2.Zero, screen);
        float w = Math.Min(660, area.Size.X), h = Math.Min(300, area.Size.Y);
        _chat.Position = new Vector2(area.Position.X, area.End.Y - h - (battle ? 90 : 10));
        _chat.Size = new Vector2(w, h);
    }

    /// <summary>Enter opens the chat (co-op); true when it took the key.</summary>
    private bool ChatKey(InputEvent e)
    {
        if (_x.Coop == null || _chat.Typing || e is not InputEventKey { Pressed: true, Keycode: Key.Enter or Key.KpEnter }) return false;
        _chat.Open();
        GetViewport().SetInputAsHandled();
        return true;
    }

    // this machine's address on the local network, to show the crew where to join
    private static string LanAddress() =>
        IP.GetLocalAddresses().FirstOrDefault(a => a.StartsWith("192.168.") || a.StartsWith("10.") || a.StartsWith("172.")) ?? "this machine";

    // a crew peer between battles: the crew sheet as the screen, the battle hidden and still
    private void Waiting(bool on)
    {
        _crew.Lobby = on;
        _view.Visible = !on;
        _console.Visible = !on;
    }

    /// <summary>A battle was just built (Start): the admiral sends it to the crew and both begin sharing it.</summary>
    private void CoopStarted(Scenario sc)
    {
        if (_x.Coop is not { } ls) return;
        if (ls.IsHost)
        {
            var m = new JsonObject { ["t"] = "start", ["title"] = sc.Name };
            if (_flow?.Running is { } offer && _flow.Camp is { } c) { m["kind"] = "mission"; m["save"] = c.Save(); m["offer"] = offer.Id; }
            else { m["kind"] = "scenario"; m["id"] = sc.Id; }
            _net!.Broadcast(m);
        }
        ls.Begin(_x.Battle);
        _crewOverT = 0;
        GD.Print($"COOP_BATTLE {sc.Id} as {ls.Crew.NameOf(ls.Me)} on {ls.OwnShip()?.Name}");
    }

    /// <summary>The admiral's battle left the screen (back to the map or the menu): the crew stops sharing it.</summary>
    private void CoopStopped()
    {
        if (_x.Coop is not { IsHost: true } ls || ls.Battle == null) return;
        _net!.Broadcast(new JsonObject { ["t"] = "stop", ["over"] = ls.Battle.Over });
        ls.End();
    }

    // a crew peer: the admiral's word on battles (start, stop, the pause)
    private void CrewFlow(JsonObject m)
    {
        switch ((string?)m["t"])
        {
            case "start":
                Scenario sc;
                _x.State.InMission = false;
                _x.State.Goals = null;
                if ((string?)m["kind"] == "mission")
                {
                    var camp = Campaign.Campaign.Load(_campData, (string)m["save"]!);
                    int id = (int)m["offer"]!;
                    Offer offer = camp.S.Missions.First(o => o.Id == id);
                    MissionPlan plan = MissionRun.Plan(camp, offer, _x.Data) ?? throw new InvalidOperationException($"mission {id} has no battle");
                    sc = plan.Scenario;
                    _x.State.InMission = true;
                    _x.State.Goals = () => plan.Battle.Objectives.Select(o => new GoalLine(o.Text.Eval(new OfferScope(camp, plan.Mission)), o.Primary,
                        MissionRun.MetNow(camp, plan, _x.Battle).GetValueOrDefault(o.Id))).ToList();
                }
                else sc = _x.Data.ScenarioById((string)m["id"]!);
                Start(sc);
                Waiting(false);
                break;
            case "stop":
                _x.Coop?.End();
                _crew.Status = $"Battle over: {(string?)m["over"] ?? "the admiral left it"}. Waiting for the admiral.";
                Waiting(true);
                break;
            case "pause":
                _x.State.Paused = (bool?)m["on"] ?? false;
                break;
        }
    }

    /// <summary>Steps a shared battle (instead of Main's own step) and returns true; false when there is none.</summary>
    private bool CoopStep(double delta)
    {
        if (_x.Coop is not { Battle: not null } ls) return false;
        double step = _x.Data.Tuning.StepS;
        if (ls.IsHost)
        {
            bool paused = _x.State.Paused || _x.State.MenuOpen;
            if (paused != _pauseSent) { _pauseSent = paused; _net!.Broadcast(new JsonObject { ["t"] = "pause", ["on"] = paused }); }
            if (paused) { ls.HostTick(false); return true; }
            _acc = Math.Min(_acc + delta, step * 6);
            while (_acc >= step) { ls.HostTick(true); _acc -= step; }
            _x.StepShare = _acc / step;
            return true;
        }
        _net!.Poll();
        _acc += delta;
        int want = (int)(_acc / step);
        _acc -= want * step;
        // a backlog (a stall on the network) is played off faster, never left to grow
        if (ls.Waiting > 6) want += ls.Waiting - 3;
        int played = ls.Play(want);
        if (played < want) _acc = 0;
        _x.StepShare = Math.Clamp(_acc / step, 0, 1);
        // the admiral's stop may have arrived while playing (the battle is no longer shared)
        if (ls.Battle is { Over: not null } && (_crewOverT += delta) > 4 && !_crew.Lobby)
        {
            _crew.Status = $"Battle over: {ls.Battle?.Over}. Waiting for the admiral.";
            Waiting(true);
        }
        return true;
    }
}
