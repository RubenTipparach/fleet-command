// Battle.Cloak: cloaking and what a ship can see, after the battle console mockup's cloakStep, setCloak, sweep, detected,
// canLock and seenFoes (docs/mockups/battle-console.html; openspec/changes/cloaking; owner, 2026-11-04: "we need both sides
// to cloak. And scan with eccm"). A fitted ship fades toward its order; cloaking it draws the cloak's energy (Rules.Used),
// its shields neither hold nor regenerate (Battle.Damage, SystemsStep) and it cannot fire (FireMount). Detected is the one
// rule for whether one ship sees another, and CanLock builds on it with the nebula's rule: the contacts, the markers, the
// radar, the 3D model, the AI's choice of target and every lock read them. This file also sets the package's state up from
// the scenario (CloakSetup) and gives the AI its cloak, its sweep and its ECM (ThinkSystems).
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

/// <summary>A sweep as it was made, for the view's spreading ring: where (metres), its reach (metres), when, and whose side.</summary>
public sealed record SweepRing(double XM, double ZM, double RM, double T, int Side);

public sealed partial class Battle
{
    private CloakSystems Cs => Data.Systems.Cloak;
    private readonly List<SweepRing> _sweeps = new();
    /// <summary>The sweeps of the last few seconds, oldest first.</summary>
    public IReadOnlyList<SweepRing> Sweeps => _sweeps;
    private Rng _secRng = null!, _boardRng = null!, _nebRng = null!;

    // Called once the battle is built: the rules' numbers and nebula plumes, each ship's cloak draw and marines, then the scene's
    // own settings for each ship (data/battle/scenes/cloak.json), as the mockup's scene setups make them.
    private void CloakSetup()
    {
        Rules.CloakData = Cs;
        Rules.Nebulae = Scenario.Nebulae ?? Array.Empty<NebulaDef>();
        _secRng = Rng.Fork(Scenario.Seed, "security");
        _boardRng = Rng.Fork(Scenario.Seed, "boarding");
        _nebRng = Rng.Fork(Scenario.Seed, "nebula");
        foreach (Ship s in _ships)
        {
            s.CloakCostE = Cs.Cloak.CostE;
            // the marines aboard are the class's crew (Battle.Transport sets them first); a scene may set them here
            if (Scenario.Ships[s.Order].Cloak is not { } c) continue;
            if (c.Cloaked && Fitted(s)) { s.CloakOn = true; s.CloakK = 1; }
            s.AiCloak = c.AiCloak;
            if (c.PowerE != null) foreach (var (k, v) in c.PowerE) s.Power[k] = v;
            if (c.Ecm is { } ecm) s.Ecm = ecm;
            s.Eccm = c.Eccm;
            ClampEw(s);
            if (c.Marines is { } mn) s.Marines = mn;
            s.AiBoard = c.AiBoard;
            s.AiBoardT = c.BoardFirstS - Cs.Security.BoardEveryS;
            if (c.ShieldsDown) { s.ShieldsUp = false; s.Power["shields"] = 0; for (int i = 0; i < 6; i++) s.Shields[i] = 0; }
            if (c.BatteryFrac is { } bf) s.Battery = s.Class.BatteryE * bf;
        }
        // a cloaking captain starts off knowing where your ship is (the mockup's Cloak scene)
        foreach (Ship s in _ships) if (s.AiCloak && s.Side != Player.Side) s.LastSeen = (Player.X, Player.Z);
        EnsureSecurity(Player);
    }

    /// <summary>True for a ship whose class carries a cloak.</summary>
    public bool Fitted(Ship s) => Cs.Cloak.Fitted.Contains(s.Class.Id);

    // The cloak, every step: fading toward its order, the reveals and the sweep's cool-down running down; a ship that can
    // no longer see its target loses its lock; an enemy captain remembers where its target was.
    partial void CloakStep(double dt)
    {
        CloakRules c = Cs.Cloak;
        foreach (Ship s in _ships)
        {
            if (!s.Alive) continue;
            s.CloakK = Math.Clamp(s.CloakK + (s.CloakOn ? 1 : -1) * dt / c.FadeS, 0, 1);
            for (int i = 0; i < s.SeenBy.Length; i++) s.SeenBy[i] = Math.Max(0, s.SeenBy[i] - dt);
            s.SweepT = Math.Max(0, s.SweepT - dt);
        }
        foreach (Ship s in _ships)
        {
            if (!s.Alive) continue;
            Ship? t = TargetOf(s);
            if (t != null && t.Side != s.Side && !CanLock(s, t))
            {
                s.Target = null;
                if (s == Player) Say("Science", $"Lost lock on {t.Name}: {(t.Cloaked ? "it has cloaked. Sweep to find it" : "the nebula hides it")}.");
            }
            if (t != null && s.Side != Player.Side) s.LastSeen = (t.X, t.Z);
        }
        _sweeps.RemoveAll(w => T - w.T >= 3);
    }

    /// <summary>Orders a ship's cloak on or off; refused for a ship without one. One rule for the player and the AI.</summary>
    public void SetCloak(Ship s, bool on)
    {
        if (!Fitted(s)) { if (s == Player) Say("Science", "Cannot cloak: no cloak fitted."); return; }
        if (s.CloakOn == on) return;
        s.CloakOn = on;
        if (s == Player || Detected(Player, s)) Say(s == Player ? "Science" : "Tactical", on ? $"{s.Name} cloaking." : $"{s.Name} decloaking.");
    }

    /// <summary>The Science station's Cloak key: the commanded ship's cloak on, or off again.</summary>
    public void ToggleCloak() => SetCloak(Player, !Player.CloakOn);

    /// <summary>A ship's sweep reach now, km (Rules.SweepKm with its science officer).</summary>
    public double SweepKm(Ship s) => Rules.SweepKm(s, OfficerSkill(s, "science"));

    /// <summary>True when the ship can sweep now: cooled down and the battery holding the sweep's energy.</summary>
    public bool CanSweep(Ship s) => s.Alive && s.SweepT <= 0 && s.Battery >= Cs.Cloak.SweepE;

    /// <summary>A sweep by the commanded ship (the Science station's Sweep key).</summary>
    public void Sweep() => Sweep(Player);

    /// <summary>A sweep: spends the battery and reveals, to s's side, every cloaked ship of the other side within reach.</summary>
    public void Sweep(Ship s)
    {
        CloakRules c = Cs.Cloak;
        if (!CanSweep(s))
        {
            if (s == Player) Say("Science", s.SweepT > 0 ? $"Cannot sweep yet: {Math.Ceiling(s.SweepT)} s." : "Cannot sweep: the battery is too low.");
            return;
        }
        s.Battery -= c.SweepE;
        s.SweepT = c.SweepCoolS;
        double km = SweepKm(s);
        _sweeps.Add(new SweepRing(s.X, s.Z, km * 1000, T, s.Side));
        Ship[] found = _ships.Where(o => o.Alive && o.Side != s.Side && o.CloakK > 0 && Rules.DistKm(s, o) <= km).ToArray();
        foreach (Ship o in found) o.SeenBy[s.Side] = c.RevealS;
        if (s == Player) Say("Science", found.Length > 0 ? $"Sweep: {string.Join(", ", found.Select(o => o.Name))} found inside {km:0} km." : $"Sweep: nothing cloaked inside {km:0} km.");
        else if (found.Contains(Player)) Say("Science", $"{s.Name} swept us: we are seen.");
    }

    /// <summary>Whether s sees t at all: its own side, not fully cloaked, revealed to s's side by a sweep, or within the
    /// cloak's detect range.</summary>
    public bool Detected(Ship s, Ship t) =>
        !t.Absent && (t.Side == s.Side || !t.Cloaked || (s.Side < t.SeenBy.Length && t.SeenBy[s.Side] > 0) || Rules.DistKm(s, t) <= Cs.Cloak.DetectKm);

    /// <summary>Whether s can lock t: seen, and not hidden by a nebula (in one, beyond its lock range, unless s's ECCM beats
    /// t's ECM).</summary>
    public bool CanLock(Ship s, Ship t) =>
        Detected(s, t) && !(Rules.NebulaAt(t.X, t.Z) > 0 && Rules.DistKm(s, t) > Cs.Nebula.LockKm && s.Eccm <= t.Ecm);

    /// <summary>The enemies s can see (the contact lists, the radar, the markers and the AI's choice of target).</summary>
    public IEnumerable<Ship> SeenFoes(Ship s) => EnemiesOf(s).Where(o => Detected(s, o));

    // A captain who sees no enemy: it sweeps for a cloaked one when it can, and heads for where it last saw one.
    private void NoFoeSeen(Ship s)
    {
        if (EnemiesOf(s).Any() && CanSweep(s) && s.Power["sensors"] > 0) Sweep(s);
        if (s.LastSeen is { } l) { s.HOrder = Geometry.HeadingTo(s.X, s.Z, l.X, l.Z); s.VOrder = Rules.MaxSpeed(s) * 0.6; }
        else s.VOrder = 60;
    }

    // The AI's systems, each think: a cloaking captain's cloak, a boarding captain's squads, and an enemy's ECM.
    private void ThinkSystems(Ship s, Ship t)
    {
        // a cloaking captain stays cloaked closing in, decloaks inside the strike range with most guns charged, fights a
        // while, then cloaks again to close for the next pass
        if (s.AiCloak && Fitted(s))
        {
            Mount[] guns = s.Mounts.Where(m => !m.Weapon.IsPointDefence && m.Hp > 0).ToArray();
            int charged = guns.Count(Rules.Ready);
            AiCloakRules a = Cs.AiCloak;
            if (s.CloakOn && s.CloakK >= 1 && Rules.DistKm(s, t) < a.StrikeKm && charged >= Math.Ceiling(guns.Length * a.ChargedFrac)) { SetCloak(s, false); s.AiUncloakT = T; }
            else if (!s.CloakOn && s.CloakK <= 0 && T - s.AiUncloakT > a.FightS) SetCloak(s, true);
        }
        ThinkBoard(s, t);
        if (s.Side == 1 && s.Ecm == 0 && s.Power["sensors"] > 0) s.Ecm = 1;
    }

    // A message that is said once while it would repeat (a lock refused again and again).
    private void SayOnce(string who, string text)
    {
        if (_log.Count > 0 && _log[^1].Text == text && T - _log[^1].T < 3) return;
        Say(who, text);
    }
}
