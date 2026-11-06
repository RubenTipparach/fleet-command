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
using FleetCommand.Data;
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
        { "wait_ready", "pick_scene", "click", "press", "station", "expand", "collapse", "arm", "helm", "fight", "hold", "wheel", "snap" };

    private ConsoleContext _x = null!;
    private Func<IEnumerable<IControlSource>> _sources = null!;
    private string _name = "";
    private string _scene = "";
    private List<Beat> _beats = new();
    private bool _done;

    /// <summary>Loads a script (validated, CLAUDE.md section 6.4) and the scenario it is to play.</summary>
    public void Init(ConsoleContext ctx, Func<IEnumerable<IControlSource>> sources, string name, string json, string scene)
    {
        _x = ctx; _sources = sources; _name = name; _scene = scene;
        Name = "ScriptedCaptain";
        StrictJson root = StrictJson.Parse(json, $"data/playtests/{name}.json");
        foreach (StrictJson b in root.ObjList("beats"))
        {
            string kind = b.Str("do");
            if (!Kinds.Contains(kind)) throw new DataError($"{b.Where}.do: unknown beat '{kind}' (there are: {string.Join(", ", Kinds)})");
            _beats.Add(new Beat(kind, b.Str("arg", ""), b.Num("s", 0), b.Num("linger_s", 0)));
            b.Done();
        }
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
            if (result == "ok")
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
                return await Click("v.scene") ?? await Click("v.scene." + id) ?? (B.Scenario.Id == id ? null : $"scenario is {B.Scenario.Id}");
            }
            case "wheel": return await Wheel(b.Arg == "in", (int)Math.Max(1, b.Seconds));
            case "fight": return await Fight(b.Seconds > 0 ? b.Seconds : 600, b.LingerS);
            case "snap": return await Snap(b.Arg);
            default: return $"no beat '{b.Do}'";
        }
    }

    // Fights the scenario out as a captain at the console would: lock the nearest enemy (Tab), order Intercept on the helm,
    // and fire (F) whenever the weapons panel shows a charged weapon in arc. Stuck when nobody's hull has changed for 60 s
    // of battle time. With lingerS, a captain watching a kill: it stays on the wreck that long before locking the next.
    private async Task<string?> Fight(double maxS, double lingerS = 0)
    {
        double t0 = B.T, lastChange = B.T, sum = HullSum();
        bool ordered = false, hadTarget = false;
        while (B.Over == null && B.T - t0 < maxS)
        {
            Ship me = B.Player;
            if (!me.Alive) break;
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
            if (B.WouldFire().Length > 0) Press("fire");
            await Frames(3);
            double now = HullSum();
            if (Math.Abs(now - sum) > 1e-6) { sum = now; lastChange = B.T; }
            if (B.T - lastChange > 60) return $"no hull changed for 60 s (t={B.T:0} s)";
        }
        return null;
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
        Error e = GetViewport().GetTexture().GetImage().SavePng($"{dir}/{_scene}-{name}.png");
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

    /// <summary>Points at a control where it was last drawn and clicks it; null when it was there to click.</summary>
    private async Task<string?> Click(string id)
    {
        await Frames(2);
        (string Id, Rect2 R, bool Live)? c = _sources().SelectMany(s => s.Controls()).Reverse().FirstOrDefault(k => k.Id == id);
        if (c is not { } hit || hit.Id == null) return $"no control '{id}' on screen";
        if (!hit.Live) return $"control '{id}' does nothing";
        Vector2 p = hit.R.GetCenter();
        Viewport vp = GetViewport();
        vp.PushInput(new InputEventMouseMotion { Position = p, GlobalPosition = p }, true);
        await Frames(1);
        vp.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = p, GlobalPosition = p }, true);
        vp.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = p, GlobalPosition = p }, true);
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
        // once the battle is over its clock stops: wait real time (the view still plays, a wreck still comes apart)
        if (B.Over != null) { await ToSignal(GetTree().CreateTimer(s), SceneTreeTimer.SignalName.Timeout); return; }
        double end = B.T + s;
        int guard = 0;
        while (B.T < end && B.Over == null && guard++ < 100000) await Frames(1);
    }
}
