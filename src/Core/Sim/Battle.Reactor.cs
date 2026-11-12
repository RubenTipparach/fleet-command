// Battle.Reactor: the reactor every step (openspec/changes/reactor; the battle console mockup's reactorStep, meltdown,
// the crew's tending in think, the reactor's share of damage, and the Reactor page's orders). The coils drift while the
// confinement is damaged (less with a power officer); heat comes from the output and from misalignment, and the coolant
// loops carry it off in proportion to the core temperature; coolant offline, or the core at its limit, counts toward a
// meltdown, and a safe core counts back. An AI ship's crew nudges its coils back and moves flow off a failing loop.
// Also the one set-up of this package's state for every ship (ReactorSetup: the reactor, the warp drive, the repair
// queue, the officers and the scenario's start). Randomness from its own streams, so it never shifts another's.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

/// <summary>A notice in the middle of the view (a jump refused, made or failed): a title, its reason, its tone (accord,
/// caution or alert) and the battle time it was given.</summary>
public sealed record BattleNotice(string Title, string Text, string Tone, double T);

public sealed partial class Battle
{
    private Rng _reactorRng = null!, _reactorHitRng = null!, _warpRng = null!;

    /// <summary>The last notice given, or null.</summary>
    public BattleNotice? Notice { get; private set; }

    private void Notify(string title, string text, string tone) => Notice = new BattleNotice(title, text, tone, T);

    /// <summary>A share as the console shows it: whole percent.</summary>
    public static string Pct(double f) => $"{Math.Floor(f * 100 + 0.5)} %";

    // Every ship's reactor, warp drive, repair queue and officers from the reactor file, then each ship's start as the
    // scenario sets it (ScenarioStart).
    private void ReactorSetup()
    {
        _reactorRng = Rng.Fork(Scenario.Seed, "reactor");
        _reactorHitRng = Rng.Fork(Scenario.Seed, "reactor.hits");
        _warpRng = Rng.Fork(Scenario.Seed, "warp");
        SystemsDef d = Data.Systems;
        foreach (Ship s in _ships)
        {
            s.Reactor.Coils = new double[d.Reactor.Coils];
            s.Reactor.Loops = Enumerable.Range(0, d.Reactor.Loops).Select(_ => new CoolantLoop { Flow = d.Reactor.FlowStart }).ToArray();
            s.Reactor.Temp = d.Reactor.TempStartPct;
            s.Reactor.Coolant = 1;
            s.Warp.Nacelles = Enumerable.Repeat(1.0, d.Warp.Nacelles).ToArray();
            s.Power["warp"] = 0;
            s.RepairQueue.AddRange(d.Repair.Queue);
        }
        for (int i = 0; i < _ships.Count; i++) if (Scenario.Ships[i].Start is { } st) ApplyStart(_ships[i], st);
        OfficersSetup();
    }

    private static void ApplyStart(Ship s, ShipStartState st)
    {
        foreach (var (k, n) in st.Power) s.Power[k] = n;
        if (st.Power.TryGetValue("warp", out int wp) && wp > 0) { s.Warp.Armed = true; s.Warp.Want = wp; }
        foreach (var (k, v) in st.Sys) s.Sys[k] = v;
        if (st.MountsHp is { } mh)
            for (int i = 0; i < mh.Length && i < s.Mounts.Count; i++) { s.Mounts[i].Hp = mh[i]; if (mh[i] <= 0) s.Mounts[i].Charge = 0; }
        ReactorState r = s.Reactor;
        if (st.Coils is { } c) for (int i = 0; i < c.Length && i < r.Coils.Length; i++) r.Coils[i] = c[i];
        if (st.LoopsHp is { } l) for (int i = 0; i < l.Length && i < r.Loops.Length; i++) r.Loops[i].Hp = l[i];
        if (st.TempPct is { } t) r.Temp = t;
        if (st.Coolant is { } k2) r.Coolant = k2;
        if (st.BanksE is { } b) s.Warp.Banks = b;
        if (st.NacellesHp is { } n2) for (int i = 0; i < n2.Length && i < s.Warp.Nacelles.Length; i++) s.Warp.Nacelles[i] = n2[i];
    }

    partial void ReactorStep(double dt)
    {
        ReactorDef R = Data.Systems.Reactor;
        double drift = Data.Systems.Officers.Ability.Drift;
        foreach (Ship s in _ships)
        {
            if (!s.Alive) continue;
            ReactorState rx = s.Reactor;
            // a ship that has struck has her core shut down by the boarding party: it neither heats nor melts down
            if (s.Struck) { rx.Melt = 0; rx.Temp = Math.Max(R.TempFloorPct, rx.Temp - R.CoolPerFlow * rx.Temp * dt); continue; }
            double conf = Rules.Confinement(s);
            if (conf < 1)
                for (int i = 0; i < rx.Coils.Length; i++)
                    rx.Coils[i] = Math.Clamp(rx.Coils[i] + (_reactorRng.Next() - 0.5) * 2 * R.DriftPerS * (1 - Rules.OfficerSkill(s, "power") * drift) * (1 - conf) * dt * R.DriftX, -1, 1);
            double heatIn = R.HeatFullPctS * Rules.Output(s) / s.Class.ReactorE + R.HeatMisalignPctS * Rules.Misalignment(s);
            rx.Temp = Math.Max(R.TempFloorPct, rx.Temp + (heatIn - R.CoolPerFlow * Rules.Cooling(s) * rx.Temp) * dt);
            bool offline = Rules.CoolantOffline(s), critical = offline || rx.Temp >= R.CriticalPct;
            rx.Melt = critical ? rx.Melt + dt : Math.Max(0, rx.Melt - dt * R.MeltRecoverX);
            if (s == Player && critical && !rx.Warned) { rx.Warned = true; Say("Engineering", offline ? "Coolant offline. The core will melt down." : "Core temperature critical."); }
            if (!critical && rx.Melt == 0) rx.Warned = false;
            if (rx.Melt >= R.MeltdownS) { Meltdown(s); continue; }
            // an AI ship's crew tends its reactor each time its captain thinks (the mockup's think)
            if (s == Player || s.Struck) continue;
            rx.TendT -= dt;
            if (rx.TendT > 0) continue;
            rx.TendT = Tuning.AiThinkS;
            Tend(s, R);
        }
    }

    // The crew tends the reactor: coils nudged back toward centre, flow moved off a failing loop.
    private static void Tend(Ship s, ReactorDef R)
    {
        double[] c = s.Reactor.Coils;
        for (int i = 0; i < c.Length; i++) c[i] -= Math.Clamp(c[i], -R.Nudge, R.Nudge);
        if (s.Reactor.Loops.Length < 2) return;
        CoolantLoop a = s.Reactor.Loops[0], b = s.Reactor.Loops[1];
        if (a.Hp < R.CrewLoopBelow && b.Flow < R.FlowMax) { b.Flow++; a.Flow = Math.Max(0, a.Flow - 1); }
        else if (b.Hp < R.CrewLoopBelow && a.Flow < R.FlowMax) { a.Flow++; b.Flow = Math.Max(0, b.Flow - 1); }
    }

    // A hit through the shields (Damage): it may knock a confinement coil out of alignment, damage a coolant loop or a
    // warp nacelle, each by its own roll.
    private void ReactorHit(Ship t, double rest)
    {
        ReactorHitDef H = Data.Systems.Reactor.Hit;
        Rng r = _reactorHitRng;
        ReactorState rx = t.Reactor;
        if (rx.Coils.Length > 0 && r.Next() < H.CoilP)
        {
            int i = r.Below(rx.Coils.Length);
            rx.Coils[i] = Math.Clamp(rx.Coils[i] + (r.Next() - 0.5) * H.CoilKnock, -1, 1);
            t.Sys["reactor"] = Math.Max(0, Rules.Confinement(t) - rest * H.ConfPerDmg);
            if (t == Player) Say("Engineering", $"Confinement coil {i + 1} out of alignment.");
        }
        if (rx.Loops.Length > 0 && r.Next() < H.LoopP)
        {
            int j = r.Below(rx.Loops.Length);
            CoolantLoop l = rx.Loops[j];
            l.Hp = Math.Max(0, l.Hp - H.LoopDmg - r.Next() * H.LoopDmgRand);
            if (t == Player) Say("Engineering", l.Hp <= 0 ? $"Coolant loop {LoopName(j)} offline." : $"Coolant loop {LoopName(j)} damaged.");
        }
        if (t.Warp.Nacelles.Length > 0 && r.Next() < H.NacelleP)
        {
            int n = r.Below(t.Warp.Nacelles.Length);
            t.Warp.Nacelles[n] = Math.Max(0, t.Warp.Nacelles[n] - H.NacelleDmg);
            if (t == Player) Say("Engineering", t.Warp.Nacelles[n] <= 0 ? "A warp nacelle is out." : "Warp nacelle damaged.");
        }
    }

    /// <summary>A coolant loop's name: A, B and on.</summary>
    public static string LoopName(int j) => ((char)('A' + j)).ToString();

    /// <summary>The reactor explodes: the ship is destroyed and every ship within blast_km takes blast_dmg, falling to 0 at
    /// the edge (a warp core breach too).</summary>
    private void Meltdown(Ship s)
    {
        if (!s.Alive) return;
        ReactorDef R = Data.Systems.Reactor;
        double hull = s.Hull;
        s.Hull = 0; s.Alive = false; s.V = 0;
        Record(s, s.X, s.Z, "meltdown", 0, hull);
        _hits[^1] = _hits[^1] with { Destroyed = true };
        Say("Engineering", $"{s.Name}: reactor meltdown.");
        foreach (Ship t in _ships)
        {
            if (!t.Alive) continue;
            double km = Rules.DistKm(s, t);
            if (km <= R.BlastKm) Damage(t, s.X, s.Z, s, R.BlastDmg * (1 - km / R.BlastKm), "blast");
        }
        DropLocksOn(s.Id);
        if (s == Player) PromoteNextCommand();
    }

    // ---- The Reactor page's orders (the player's ship) -------------------------------------------------------------

    /// <summary>Sets a coil's offset by hand (the diagram's drag), -1 to 1.</summary>
    public void SetCoil(int i, double offset)
    {
        if (Sent(nameof(SetCoil), i, offset)) return;
        double[] c = Player.Reactor.Coils;
        if (i < 0 || i >= c.Length || !double.IsFinite(offset)) return;
        c[i] = Math.Clamp(offset, -1, 1);
    }

    /// <summary>Nudges a coil one step toward (or away from) alignment: dir is -1 or 1.</summary>
    public void AlignCoil(int i, int dir)
    {
        if (Sent(nameof(AlignCoil), i, dir)) return;
        double[] c = Player.Reactor.Coils;
        if (i < 0 || i >= c.Length || dir is not (-1 or 1)) return;
        c[i] = Math.Clamp(c[i] + dir * Data.Systems.Reactor.Nudge, -1, 1);
    }

    /// <summary>Sets a coolant loop's pump flow, within flow_max and what the output leaves (lowering it is always allowed).</summary>
    public void SetFlow(int j, int n)
    {
        if (Sent(nameof(SetFlow), j, n)) return;
        CoolantLoop[] loops = Player.Reactor.Loops;
        if (j < 0 || j >= loops.Length) return;
        CoolantLoop l = loops[j];
        int cap = Math.Max(Math.Min(l.Flow, n), Math.Min(Data.Systems.Reactor.FlowMax, Rules.PowerRoom(Player, l.Flow)));
        l.Flow = Math.Clamp(n, 0, Math.Max(0, cap));
    }

    /// <summary>Lets the allocation draw past the reactor's output, or not (off by default).</summary>
    public void SetOverdraw(bool on) { if (Sent(nameof(SetOverdraw), on)) return; Player.Overdraw = on; }
}
