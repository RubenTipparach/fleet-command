// ScriptedCaptain.Reactor: the scripted captain's beats for the reactor, warp and damage control (the Warp and Escape
// scenes), played through the console's own controls as a player would: a drag from one control to another (the repair
// queue's rows), setting a system's boxes on the Power page, aligning the reactor's coils with its keys, and the warp
// drive: arm it with the warp key, keep the course plotted, turn on the safety override when it must, and jump once the
// Warp page's risk is low enough (or the enemy is close). One captain: these are its beats, never a second bot.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Playtest;

public sealed partial class ScriptedCaptain
{
    // A drag from one control to another ("from>to"): pressed on the first, moved in steps, let go over the second.
    private async Task<string?> Drag(string arg)
    {
        string[] ids = arg.Split('>');
        if (ids.Length != 2) return $"a drag is 'from>to', not '{arg}'";
        await Frames(2);
        Rect2? A = FindRect(ids[0]), Bc = FindRect(ids[1]);
        if (A == null) return $"no control '{ids[0]}' on screen";
        if (Bc == null) return $"no control '{ids[1]}' on screen";
        Vector2 a = A.Value.GetCenter(), b = Bc.Value.GetCenter();
        Viewport vp = GetViewport();
        vp.PushInput(new InputEventMouseMotion { Position = a, GlobalPosition = a }, true);
        await Frames(1);
        vp.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = a, GlobalPosition = a, ButtonMask = MouseButtonMask.Left }, true);
        for (int k = 1; k <= 12; k++)
        {
            Vector2 q = a + (b - a) * k / 12f;
            vp.PushInput(new InputEventMouseMotion { Position = q, GlobalPosition = q, Relative = (b - a) / 12f, ButtonMask = MouseButtonMask.Left }, true);
            await Frames(1);
        }
        vp.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = b, GlobalPosition = b }, true);
        await Frames(3);
        if (B.Log.Count > 0) GD.Print($"PLAYTEST_DRAG {arg}: {B.Log[^1].Who}: {B.Log[^1].Text}");
        return null;
    }

    private Rect2? FindRect(string id)
    {
        foreach (var c in _sources().SelectMany(s => s.Controls()).Reverse()) if (c.Id == id) return c.R;
        return null;
    }

    // A system's boxes on the Power page's Allocation ("system=n"), by tapping its row's cells.
    private async Task<string?> SetBoxes(string arg)
    {
        string[] kv = arg.Split('=');
        if (kv.Length != 2 || !int.TryParse(kv[1], out int n)) return $"a power beat is 'system=boxes', not '{arg}'";
        string sys = kv[0];
        if (await Click("st.power") is { } s1) return s1;
        if (await Click("pw.mode.alloc") is { } s2) return s2;
        for (int i = 0; i < 12 && B.Player.Power[sys] != n; i++)
            if (await Click($"pw.{sys}.{Math.Max(0, n - 1)}") is { } s3) return s3;
        return B.Player.Power[sys] == n ? null : $"{sys} is at {B.Player.Power[sys]} boxes, not {n}";
    }

    // The reactor's coils brought back into alignment with the expanded Reactor page's keys, then the panel returned.
    private async Task<string?> Align()
    {
        if (await Click("st.power") is { } s1) return s1;
        if (await Click("pw.mode.reactor") is { } s2) return s2;
        if (_x.State.Expanded == null && await Click("Station.xp") is { } s3) return s3;
        double nudge = B.Data.Systems.Reactor.Nudge;
        double[] c = B.Player.Reactor.Coils;
        for (int i = 0; i < c.Length; i++)
            for (int k = 0; k < 40 && Math.Abs(c[i]) > nudge / 2; k++)
                if (await Click(c[i] > 0 ? $"rx.m.{i}" : $"rx.p.{i}") is { } s4) return s4;
        // a hot core: the coolant flow on every loop still working turned up to its most (the flow boxes), as the AI's crew
        // moves flow onto the loops that carry heat (Battle.Reactor's Tend)
        var R = B.Data.Systems.Reactor;
        if (B.Player.Reactor.Temp >= R.CriticalPct * 0.8)
            for (int j = 0; j < B.Player.Reactor.Loops.Length; j++)
                if (B.Player.Reactor.Loops[j].Hp > 0.2 && B.Player.Reactor.Loops[j].Flow < R.FlowMax)
                {
                    int before = B.Player.Reactor.Loops[j].Flow;
                    // every point of output spoken for: a box taken from a lesser system first (the engines last, never below one box: a slower
                    // ship rather than a melted one) (the Power page's
                    // allocation), as a player frees power for the pumps, then back to the Reactor page
                    if (B.Rules.PowerRoom(B.Player, before) <= before
                        && new[] { "tractor", "transport", "sensors", "engines" }.FirstOrDefault(k => B.Player.Power.GetValueOrDefault(k) > (k == "engines" ? 1 : 0)) is { } spare)
                    {
                        int had = B.Player.Power[spare];
                        await SetBoxes($"{spare}={had - 1}");
                        GD.Print($"PLAYTEST_REACTOR {spare} {had} -> {B.Player.Power[spare]} boxes for the coolant");
                        await Click("pw.mode.reactor");
                    }
                    int cap = Math.Min(R.FlowMax, B.Rules.PowerRoom(B.Player, before));
                    if (cap > before) await Click($"rx.f.{j}.{cap - 1}");
                    GD.Print($"PLAYTEST_REACTOR loop {j + 1} flow {before} -> {B.Player.Reactor.Loops[j].Flow} (hp {B.Player.Reactor.Loops[j].Hp:0.00})");
                }
        Press("back");
        await Frames(3);
        return null;
    }

    // The warp drive through to the jump: armed with the warp key, the course kept plotted from the Warp page, the coils
    // aligned now and then, the override on when the field blocks the jump and time is short, and the jump taken once
    // the risk is at most maxRisk (or at most 0.8 with an enemy within 9 km). Stuck if the ship is lost or never jumps.
    private async Task<string?> Warp(double maxRisk, double maxS)
    {
        double t0 = B.T, lastAlign = -1e9;
        if (!B.Player.Warp.Armed && await Click("warp.key") is { } s0) return s0;
        while (B.Over == null && B.T - t0 < maxS)
        {
            Ship me = B.Player;
            if (!me.Alive) return "the ship was lost before it jumped";
            if (me.Warp.Out > 0) { await Frames(3); continue; }
            if (Rules.Misalignment(me) > 0.12 && B.T - lastAlign > 10) { lastAlign = B.T; if (await Align() is { } sa) return sa; }
            bool close = B.EnemiesOf(me).Any(o => Rules.DistKm(me, o) < 90);
            WarpRisk r = B.WarpRiskOf(me);
            bool needOverride = r.Why.Contains("override") && !me.Warp.Override && (close || B.T - t0 > 60);
            if (!me.Warp.Plotting || needOverride)
            {
                if (await Click("st.power") is { } s1) return s1;
                if (await Click("pw.mode.warp") is { } s2) return s2;
                if (!me.Warp.Plotting && await Click("wp.plot") is { } s3) return s3;
                if (needOverride && await Click("wp.ovr") is { } s4) return s4;
            }
            r = B.WarpRiskOf(me);
            if (B.WarpReady(me) && (r.Total <= maxRisk || (close && r.Total <= 0.8)))
            {
                GD.Print($"PLAYTEST_WARP jump at t={B.T:0} s, risk {Battle.Pct(r.Total)}");
                if (await Click("warp.key") is { } s5) return s5;
            }
            await Frames(10);
        }
        for (int i = 0; i < 300 && B.Over == null; i++) await Frames(1);
        return B.Over == "jumped" ? null : $"no jump to warp in {maxS:0} s (outcome {B.Over ?? "fighting"})";
    }
}
