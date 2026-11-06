// ScriptedCaptain: the playtest bot (CLAUDE.md section 9; the playtest skill). It plays the real game through the same
// inputs a player gives: key actions from the InputMap (fire, next target, speed, pause, back) and pointer clicks on the
// console's controls, found by id where they were last drawn (IControlSource) and pushed into the viewport as mouse
// events. It decides nothing a player could not see on the console. A script is a list of beats in
// data/playtests/<name>.json; the captain prints GAME_READY (from Main), PLAYTEST_START, PLAYTEST_BEAT, PLAYTEST_RESULT
// and PLAYTEST_END, then quits with 0 for ok and 1 for stuck or crash. One captain: a new beat is added here, never a
// second bot.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FleetCommand.Campaign;
using FleetCommand.Data;
using FleetCommand.Game.Flow;
using FleetCommand.Game.Ui;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Playtest;

/// <summary>One step of a playtest script.</summary>
/// <summary>One step of a playtest script. LingerS (fight only): seconds to stay on a kill before locking the next target.</summary>
public sealed record Beat(string Do, string Arg, double Seconds, double LingerS = 0);

public sealed partial class ScriptedCaptain : Node
{
    /// <summary>The beats a script may use; an unknown one is a data error naming it.</summary>
    public static readonly string[] Kinds =
        { "wait_ready", "pick_scene", "click", "press", "station", "expand", "collapse", "arm", "helm", "fight", "hold", "wheel", "snap",
          "wait_screen", "end_turn", "move", "contract", "mine_at", "shift_click", "pin_grid", "fleet_wingman", "drag", "power", "align", "warp" };

    private ConsoleContext _x = null!;
    private Func<IEnumerable<IControlSource>> _sources = null!;
    private string _name = "";
    private string _scene = "";
    private List<Beat> _beats = new();
    private bool _done;
    private GameFlow? _flow;
    private int _snapN;
    private readonly List<string> _missions = new();

    /// <summary>Loads a script (validated, CLAUDE.md section 6.4) and the scenario it is to play.</summary>
    public void Init(ConsoleContext ctx, Func<IEnumerable<IControlSource>> sources, string name, string json, string scene, GameFlow? flow = null)
    {
        _x = ctx; _sources = sources; _name = name; _scene = scene; _flow = flow;
        Name = "ScriptedCaptain";
        StrictJson root = StrictJson.Parse(json, $"data/playtests/{name}.json");
        foreach (StrictJson b in root.ObjList("beats"))
        {
            string kind = b.Str("do");
            if (!Kinds.Contains(kind)) throw new DataError($"{b.Where}.do: unknown beat '{kind}' (there are: {string.Join(", ", Kinds)})");
            _beats.Add(new Beat(kind, b.Str("arg", ""), b.Num("s", 0), b.Num("linger_s", 0)));
            b.Done();
        }
        root.Bool("flow", false);
        root.Done();
    }

    private Battle B => _x.Battle;

    public override void _Ready() => _ = Run();

    private async Task Run()
    {
        string result = "ok", detail = "";
        try
        {
            GD.Print($"PLAYTEST_START {_name} {_scene}");
            for (int i = 0; i < _beats.Count && !_done; i++)
            {
                Beat b = _beats[i];
                GD.Print($"PLAYTEST_BEAT {i} {b.Do}{(b.Arg != "" ? " " + b.Arg : "")}");
                string? stuck = await Play(b);
                if (stuck != null) { result = "stuck"; detail = $"beat {i} {b.Do}: {stuck}"; break; }
            }
            if (result == "ok" && _flow?.Camp is { } camp)
                detail = $"campaign side={camp.S.Side} turn={camp.S.Turn} credits={camp.S.Credits:0} prestige={camp.S.Prestige:0} ships={camp.S.Fleet.Count} missions=[{string.Join("; ", _missions)}]";
            else if (result == "ok")
            {
                Battle bt = B;
                int foes = bt.Ships.Count(s => s.Side != bt.Player.Side && s.Alive);
                detail = $"{bt.Scenario.Id} side={bt.Scenario.Side} skin={_x.Kit.Name} outcome={bt.Over ?? "fighting"} t={bt.T:0}s foes_left={foes} hull={Math.Round(100 * bt.Player.Hull / bt.Player.Class.HullHp)}%";
            }
        }
        catch (Exception e) { result = "crash"; detail = e.GetType().Name + ": " + e.Message.Replace('\n', ' '); }
        GD.Print($"PLAYTEST_RESULT {result} {detail}");
        await Seconds(2);
        GD.Print($"PLAYTEST_END {_name} {_scene}");
        GetTree().Quit(result == "ok" ? 0 : 1);
    }

    // ---- The beats -----------------------------------------------------------------------------------------------

    /// <summary>Plays one beat; null when it finished, else why it is stuck.</summary>
    private async Task<string?> Play(Beat b)
    {
        switch (b.Do)
        {
            case "wait_ready": await Frames(10); return null;
            case "hold": await Seconds(b.Seconds); return null;
            case "press": Press(b.Arg); await Frames(2); return null;
            case "click": return await Click(b.Arg);
            case "collapse": Press("back"); await Frames(3); return _x.State.Expanded == null ? null : "the panel is still expanded";
            case "station": return await Click("st." + b.Arg) ?? (_x.State.Station == b.Arg ? null : $"station is {_x.State.Station}");
            case "expand": return await Click(b.Arg + ".xp") ?? (_x.State.Expanded?.ToString() == b.Arg ? null : "the panel did not expand");
            case "arm": return await Click("arm." + b.Arg);
            case "helm": return await Click("helm." + b.Arg);
            case "pick_scene":
            {
                string id = b.Arg == "" ? _scene : b.Arg;
                if (B.Scenario.Id == id) return null;
                // the list by scenario type, then the faction to play it as: open it, pick the type (scrolled into view as a
                // player scrolls), then the side
                string type = _x.Data.ScenarioById(id).Name, slug = new string(type.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
                if (await Click("v.scene") is { } s0) return s0;
                for (int k = 0; k < 40 && await Find("v.scene.type." + slug) == null; k++) await WheelAt("v.scene.type.", true);
                return await Click("v.scene.type." + slug) ?? await Click("v.scene." + id) ?? (B.Scenario.Id == id ? null : $"scenario is {B.Scenario.Id}");
            }
            case "wheel": return await Wheel(b.Arg == "in", (int)Math.Max(1, b.Seconds));
            case "fight": return await Fight(b.Seconds > 0 ? b.Seconds : 600, b.LingerS, b.Arg);
            case "shift_click": return await Click(b.Arg, true);
            case "pin_grid": return await PinGrid();
            case "fleet_wingman": return await FleetWingman();
            case "snap": return await Snap(b.Arg);
            case "wait_screen": return await WaitScreen(b.Arg, b.Seconds > 0 ? b.Seconds : 60);
            case "end_turn": return await EndTurn();
            case "move": return await Move((int)Math.Max(1, b.Seconds));
            case "contract": return await Contract(b.Seconds > 0 ? b.Seconds : 600, b.Arg);
            case "mine_at": return await MineAt(b.Arg);   // ScriptedCaptain.Tractor.cs
            // the reactor package's beats (ScriptedCaptain.Reactor.cs)
            case "drag": return await Drag(b.Arg);
            case "power": return await SetBoxes(b.Arg);
            case "align": return await Align();
            case "warp": return await Warp(b.Arg == "" ? 0.35 : double.Parse(b.Arg, System.Globalization.CultureInfo.InvariantCulture), b.Seconds > 0 ? b.Seconds : 300);
            default: return $"no beat '{b.Do}'";
        }
    }

    // Fights the scenario out as a captain at the console would: lock the nearest enemy (Tab), order Intercept on the helm,
    // and fire (F) whenever the weapons panel shows a charged weapon in arc. Stuck when nobody's hull has changed for 60 s
    // of battle time. With lingerS, a captain watching a kill: it stays on the wreck that long before locking the next. With
    // aims (a comma list, the Disable scene's "engines,weapons"), a captain disabling a ship: it aims, on the Tactical
    // station's Aim at keys, at the first of them whose key is not yet lit Disabled or Destroyed on the locked target.
    private async Task<string?> Fight(double maxS, double lingerS = 0, string aims = "")
    {
        double t0 = B.T, lastChange = B.T, sum = HullSum();
        bool ordered = false, hadTarget = false;
        string? steering = null;
        while (B.Over == null && B.T - t0 < maxS)
        {
            Ship me = B.Player;
            if (!me.Alive) break;
            // a battle with marker beacons and no enemy left: steer for the next beacon shown, by clicking it, as a player would
            if (B.Scenario.Markers != null && !B.EnemiesOf(me).Any())
            {
                if (B.Scenario.Markers.FirstOrDefault(k => B.MarkerActive(k) && !B.Reached.Contains(k.Id)) is { } mk && steering != mk.Id)
                {
                    if (await Click("v.marker." + mk.Id) == null) steering = mk.Id;
                }
                await Frames(3);
                lastChange = B.T;   // flying to a beacon changes no hull
                continue;
            }
            if (B.TargetOf(me) == null)
            {
                if (hadTarget && lingerS > 0) { await Seconds(lingerS); lastChange = B.T; if (B.Over != null) break; }
                Press("next_target"); ordered = false;
            }
            hadTarget = B.TargetOf(me) != null;
            // the Intercept key lit is Intercept on: pressing it again would turn it off (free heading), so only press it unlit
            if (!ordered && B.TargetOf(me) != null)
            {
                if (me.Engaged && me.Manoeuvre == "intercept") ordered = true;
                else
                {
                    if (_x.State.Station != "helm") await Click("st.helm");
                    ordered = await Click("helm.int") == null;
                }
            }
            if (aims != "" && B.TargetOf(me) is { } at)
            {
                string[] list = aims.Split(',');
                string want = list.FirstOrDefault(a => a == "hull" || B.Rules.Status(at, a) is not (SystemStatus.Disabled or SystemStatus.Destroyed)) ?? list[^1];
                if (me.Aim != want)
                {
                    if (_x.State.Station != "tactical") await Click("st.tactical");
                    await Click("aim." + want);
                }
            }
            if (B.WouldFire().Length > 0) Press("fire");
            await Frames(3);
            double now = HullSum();
            if (Math.Abs(now - sum) > 1e-6) { sum = now; lastChange = B.T; }
            if (B.T - lastChange > 60) return $"no hull changed for 60 s (t={B.T:0} s)";
        }
        return null;
    }

    // ---- The campaign (a script with "flow": true) -----------------------------------------------------------------

    private Campaign.Campaign Camp => _flow?.Camp ?? throw new InvalidOperationException("no campaign is open");

    // Waits for the flow to show a screen and settle (the boot, a fade or a loading screen finished); real seconds.
    private async Task<string?> WaitScreen(string name, double maxS)
    {
        if (_flow == null) return "not a campaign script (\"flow\": true)";
        if (!Enum.TryParse(name, true, out FlowScreen want)) return $"no screen '{name}'";
        ulong end = Time.GetTicksMsec() + (ulong)(maxS * 1000);
        while (Time.GetTicksMsec() < end)
        {
            if (_flow.Booted.IsCompleted && !_flow.Busy && _flow.Screen == want) { await Frames(20); return null; }
            await Frames(2);
        }
        return $"the screen is {_flow.Screen}, not {want}";
    }

    // The End turn key; null once the turn has moved on.
    private async Task<string?> EndTurn()
    {
        int t = Camp.S.Turn;
        string? stuck = await Click("top.end");
        await Frames(10);
        return stuck ?? (Camp.S.Turn > t ? null : "the turn did not end");
    }

    // A click on a sector of the map where the player sees it; a sector off screen is dragged into view first, a drag of
    // the map at a time (left button held, as a player pans), toward the middle of the view.
    private async Task<string?> ClickHex(int c, int r)
    {
        Vector2 sz = GetViewport().GetVisibleRect().Size;
        var safe = new Rect2(sz.X * 0.08f, sz.Y * 0.16f, sz.X * 0.6f, sz.Y * 0.7f);   // clear of the top bar and the sector panel
        Viewport vp = GetViewport();
        for (int tries = 0; tries < 12; tries++)
        {
            if (_flow!.MapView3D?.ScreenOf(c, r) is not { } p) { await Click("keys.fleet"); await Frames(40); continue; }
            if (safe.HasPoint(p))
            {
                vp.PushInput(new InputEventMouseMotion { Position = p, GlobalPosition = p }, true);
                await Frames(1);
                vp.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = p, GlobalPosition = p }, true);
                vp.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = p, GlobalPosition = p }, true);
                await Frames(4);
                return _flow.Sel == (c, r) ? null : $"the click at sector {c},{r} selected {_flow.Sel}";
            }
            // drag from a quiet spot of the map toward the sector's way, so the sector comes toward the middle
            Vector2 mid = safe.GetCenter(), move = (mid - p).LimitLength(Math.Min(sz.X, sz.Y) * 0.35f), from = mid - move / 2, to = mid + move / 2;
            vp.PushInput(new InputEventMouseMotion { Position = from, GlobalPosition = from }, true);
            await Frames(1);
            vp.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = from, GlobalPosition = from, ButtonMask = MouseButtonMask.Left }, true);
            for (int k = 1; k <= 12; k++)
            {
                Vector2 q = from + move * k / 12f;
                vp.PushInput(new InputEventMouseMotion { Position = q, GlobalPosition = q, Relative = move / 12f, ButtonMask = MouseButtonMask.Left }, true);
                await Frames(1);
            }
            vp.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = to, GlobalPosition = to }, true);
            await Frames(20);
        }
        return $"sector {c},{r} never came on screen";
    }

    // Moves the fleet: picks the sector `steps` away that it can reach this turn (the first in a turn of the compass), clicks
    // it on the map and presses Move here.
    private async Task<string?> Move(int steps)
    {
        var c = Camp;
        var at = (c.S.AtC, c.S.AtR);
        var ring = new List<(int C, int R)>();
        for (int cc = 0; cc < c.S.Cols; cc++)
            for (int rr = 0; rr < c.S.Rows; rr++)
                if (HexGrid.Dist(at.AtC, at.AtR, cc, rr) == steps && c.PathTo(cc, rr) is { } p && p.Turns == 1 && p.Legs[0].Count > 0) ring.Add((cc, rr));
        if (ring.Count == 0) return $"no sector {steps} away to move to this turn";
        var to = ring[(c.S.Turn * 3 + steps) % ring.Count];
        string? stuck = await ClickHex(to.C, to.R);
        stuck ??= await Click("sec.move");
        await Seconds(1.5);
        return stuck ?? ((c.S.AtC, c.S.AtR) == to ? null : $"the fleet is at {c.S.AtC},{c.S.AtR}, not {to.C},{to.R}");
    }

    // One contract, start to end, as a player plays it: the nearest contract the fleet can take (End turn until one is
    // offered), its sector clicked on the map and its Briefing opened, Accept (the fleet sets course), End turn until it
    // arrives and the briefing opens again, Launch, the battle fought out (Fight; a battle that stalls is withdrawn from
    // with the menu's Withdraw), the debrief read and Continue back to the map.
    private async Task<string?> Contract(double maxBattleS, string only = "")
    {
        var c = Camp;
        Campaign.Offer? m = null;
        for (int t = 0; t < 4 && m == null; t++)
        {
            m = c.S.Missions.Where(o => (only != "" ? o.Script == only : !c.Data.Missions[o.Script].Story) && c.Needs(o) == null)
                .OrderBy(o => HexGrid.Dist(c.S.AtC, c.S.AtR, o.C, o.R)).ThenBy(o => o.Id).FirstOrDefault();
            if (m == null && await EndTurn() is { } st) return st;
        }
        if (m == null) return only != "" ? $"no '{only}' mission offered" : "no contract offered in four turns";
        string name = c.Data.Missions[m.Script].Name;
        GD.Print($"PLAYTEST_MISSION take {name} #{m.Id} at {m.C},{m.R} from {c.S.AtC},{c.S.AtR}, reward {m.Reward:0} cr");
        if (await ClickHex(m.C, m.R) is { } s1) return s1;
        if (await Click($"sec.brief.{m.Id}") is { } s2) return s2;
        await Snap($"brief-{m.Script}");
        bool here = (c.S.AtC, c.S.AtR) == (m.C, m.R);
        // a contract is accepted (which sets course); a story mission, waiting until it is done, has only Set course
        if (!here && await Click(c.Data.Missions[m.Script].Story ? "br.go" : "br.accept") is { } s3) return s3;
        for (int t = 0; t < 16 && (c.S.AtC, c.S.AtR) != (m.C, m.R); t++)
        {
            await Seconds(1.2);
            if ((c.S.AtC, c.S.AtR) == (m.C, m.R)) break;
            if (!c.S.Missions.Contains(m)) return $"{name} expired before the fleet arrived";
            // a mission that goes with a moving fleet has moved on from where the course was set: set course again from
            // its briefing, opened from the mission log
            if (c.S.OrderC == null || (c.S.OrderC, c.S.OrderR) != (m.C, m.R))
            {
                GD.Print($"PLAYTEST_MISSION course again for {name}, now at {m.C},{m.R}");
                string? sc = await Click("top.missions") ?? await Click($"log.{m.Id}") ?? await Click("br.go");
                if (sc != null) return sc;
                await Click("top.missions");
                await Seconds(1.2);
                if ((c.S.AtC, c.S.AtR) == (m.C, m.R)) break;
            }
            if (await EndTurn() is { } st) return st;
        }
        if ((c.S.AtC, c.S.AtR) != (m.C, m.R)) return $"the fleet never reached {m.C},{m.R}";
        if (!c.S.Missions.Contains(m)) return $"{name} was gone when the fleet arrived";
        await Seconds(1.5);
        if (_flow!.Brief?.Id != m.Id && await Click($"sec.brief.{m.Id}") is { } s4) return s4;
        if (await Click("br.launch") is { } s5) return s5;
        await Frames(10);
        ulong end = Time.GetTicksMsec() + 30000;
        while (Time.GetTicksMsec() < end && (_flow.Busy || _flow.Screen == FlowScreen.Map)) await Frames(2);
        string how = "done on arrival";
        if (_flow.Screen == FlowScreen.Battle)
        {
            await Frames(30);
            await Snap($"battle-{m.Script}");
            string? stalled = await Fight(maxBattleS);
            if (stalled != null || B.Over == null) { how = "withdrew: " + (stalled ?? "out of time"); await Click("menu"); await Click("m.withdraw"); }
            else how = B.Over;
            await Snap($"end-{m.Script}");
        }
        if (await WaitScreen("Debrief", 30) is { } s6) return s6;
        await Snap($"debrief-{m.Script}");
        var d = _flow.Last;
        string line = $"{name}: {how}, {d?.Grade ?? "?"}";
        _missions.Add(line);
        GD.Print($"PLAYTEST_MISSION done {line}, credits {c.S.Credits:0}, prestige {c.S.Prestige:0}");
        return await Click("db.continue") ?? await WaitScreen("Map", 30);
    }

    // A still of the screen as the player sees it, into docs/screenshots/game/<script>/<scenario>-<name>.png (a windowed
    // run only: headless renders nothing, so there it does nothing).
    private async Task<string?> Snap(string name)
    {
        if (DisplayServer.GetName() == "headless") return null;
        // the pointer off the keys, so no hover card covers the still
        Vector2 sz = GetViewport().GetVisibleRect().Size, away = new(sz.X * 0.97f, sz.Y * 0.6f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = away, GlobalPosition = away }, true);
        await Frames(2);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        string dir = ProjectSettings.GlobalizePath($"res://docs/screenshots/game/{_name}");
        DirAccess.MakeDirRecursiveAbsolute(dir);
        string file = _flow != null ? $"{dir}/{++_snapN:00}-{name}.png" : $"{dir}/{_scene}-{name}.png";
        Error e = GetViewport().GetTexture().GetImage().SavePng(file);
        return e == Error.Ok ? null : $"could not save the still ({e})";
    }

    private double HullSum() => B.Ships.Sum(s => s.Alive ? s.Hull : 0);

    // ---- Input as a player gives it ------------------------------------------------------------------------------

    /// <summary>Presses and releases an InputMap action.</summary>
    private void Press(string action)
    {
        if (!InputMap.HasAction(action)) throw new InvalidOperationException($"no input action '{action}'");
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true });
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
    }

    // Pins one weapon to each contact on the expanded Tactical station's grid, as a captain reads it: for each contact's
    // column, the first cell lit (in arc and range, so it shows a chance to hit) whose weapon is not pinned yet, else the
    // first weapon not pinned yet; null once every contact has a weapon pinned to it.
    private async Task<string?> PinGrid()
    {
        await Frames(2);
        var cells = _sources().SelectMany(s => s.Controls()).Where(c => c.Id.StartsWith("tm.") && c.Id != "tm.unpin")
            .Select(c => c.Id.Split('.')).Select(p => (Mount: int.Parse(p[1]), Ship: p[2])).ToList();
        if (cells.Count == 0) return "no weapons-against-contacts grid on screen (expand the Tactical station)";
        Ship me = B.Player;
        foreach (string ship in cells.Select(c => c.Ship).Distinct().ToList())
        {
            if (B.ShipById(ship) is not { } f) continue;
            var free = cells.Where(c => c.Ship == ship && me.Mounts[c.Mount].Pin == null).ToList();
            if (free.Count == 0) continue;
            var pick = free.FirstOrDefault(c => Rules.InArc(me, me.Mounts[c.Mount], f) && Rules.InRange(me, me.Mounts[c.Mount], f));
            if (pick == default) pick = free[0];
            if (await Click($"tm.{pick.Mount}.{ship}") is { } stuck) return stuck;
        }
        int pinned = cells.Select(c => c.Ship).Distinct().Count(s => me.Mounts.Any(m => m.Pin?.To == s));
        return pinned == cells.Select(c => c.Ship).Distinct().Count() ? null : $"{pinned} contacts pinned of {cells.Select(c => c.Ship).Distinct().Count()}";
    }

    // Opens a wingman on the Fleet station (the first tab of a living ship you do not command); null once it is shown.
    private async Task<string?> FleetWingman()
    {
        if (_x.State.Station != "fleet" && await Click("st.fleet") is { } s0) return s0;
        Ship? w = B.FleetAll().FirstOrDefault(s => s != B.Player && s.Alive);
        if (w == null) return "no wingman in this battle";
        return await Click("fl." + w.Id) ?? (_x.State.FleetPick == w.Id ? null : $"the Fleet station shows {_x.State.FleetPick ?? B.Player.Id}");
    }

    /// <summary>A control on screen by id, as last drawn, or null.</summary>
    private async Task<(string Id, Rect2 R, bool Live)?> Find(string id)
    {
        await Frames(2);
        var c = _sources().SelectMany(s => s.Controls()).Reverse().FirstOrDefault(k => k.Id == id);
        return c.Id == null ? null : c;
    }

    /// <summary>One wheel notch over the first control whose id starts with a prefix (a list to scroll), down or up.</summary>
    private async Task WheelAt(string prefix, bool down)
    {
        var c = _sources().SelectMany(s => s.Controls()).FirstOrDefault(k => k.Id != null && k.Id.StartsWith(prefix));
        if (c.Id == null) return;
        Vector2 p = c.R.GetCenter();
        Viewport vp = GetViewport();
        vp.PushInput(new InputEventMouseMotion { Position = p, GlobalPosition = p }, true);
        var bi = down ? MouseButton.WheelDown : MouseButton.WheelUp;
        vp.PushInput(new InputEventMouseButton { ButtonIndex = bi, Pressed = true, Position = p, GlobalPosition = p }, true);
        vp.PushInput(new InputEventMouseButton { ButtonIndex = bi, Pressed = false, Position = p, GlobalPosition = p }, true);
        await Frames(2);
    }

    /// <summary>Points at a control where it was last drawn and clicks it (with Shift held when asked: shift+click opens
    /// the hit details); null when it was there to click.</summary>
    private async Task<string?> Click(string id, bool shift = false)
    {
        await Frames(2);
        (string Id, Rect2 R, bool Live)? c = _sources().SelectMany(s => s.Controls()).Reverse().FirstOrDefault(k => k.Id == id);
        if (c is not { } hit || hit.Id == null) return $"no control '{id}' on screen";
        if (!hit.Live) return $"control '{id}' does nothing";
        Vector2 p = hit.R.GetCenter();
        Viewport vp = GetViewport();
        vp.PushInput(new InputEventMouseMotion { Position = p, GlobalPosition = p }, true);
        await Frames(1);
        vp.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = p, GlobalPosition = p, ShiftPressed = shift }, true);
        vp.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = p, GlobalPosition = p, ShiftPressed = shift }, true);
        await Frames(2);
        return null;
    }

    /// <summary>Turns the mouse wheel over the 3D view (to the right of the console's column), n notches in or out.</summary>
    private async Task<string?> Wheel(bool zoomIn, int n)
    {
        Viewport vp = GetViewport();
        Vector2 sz = vp.GetVisibleRect().Size, p = new(sz.X * 0.62f, sz.Y * 0.45f);
        vp.PushInput(new InputEventMouseMotion { Position = p, GlobalPosition = p }, true);
        for (int i = 0; i < n; i++)
        {
            var b = zoomIn ? MouseButton.WheelUp : MouseButton.WheelDown;
            vp.PushInput(new InputEventMouseButton { ButtonIndex = b, Pressed = true, Position = p, GlobalPosition = p }, true);
            vp.PushInput(new InputEventMouseButton { ButtonIndex = b, Pressed = false, Position = p, GlobalPosition = p }, true);
            await Frames(2);
        }
        return null;
    }

    private async Task Frames(int n)
    {
        for (int i = 0; i < n; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task Seconds(double s)
    {
        // once the battle is over its clock stops, and off the battle screen (the campaign's menus and map) it never runs:
        // wait real time there (the view still plays, a wreck still comes apart)
        if (B.Over != null || (_flow != null && _flow.Screen != FlowScreen.Battle)) { await ToSignal(GetTree().CreateTimer(s), SceneTreeTimer.SignalName.Timeout); return; }
        double end = B.T + s;
        int guard = 0;
        while (B.T < end && B.Over == null && guard++ < 100000) await Frames(1);
    }
}
