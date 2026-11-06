// Battle.Targeting: weapon targeting and point defence as the captain orders them (openspec/changes/weapon-targeting; the
// battle console mockup's aim, pin, pinSelected, unpinAll, fireable, pinsStep and pd). Owner, 2026-11-04: "this aim at
// should be icon based"; "the: Pin weapon fire system, should allow you to lock a weapon on a specific target even if your
// main sensors changed targets. if that target is destroyed (ship or susbsystem) then the pinning stops"; "in this expanded
// view, I expect to be able to target individual weapons as well". Where a hit lands is Strike and Battle.Damage, one rule
// for every hit; what it costs to aim is Rules.HitChance. A test scene won by disabling a ship (tactical.json objectives)
// registers its end here.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    private bool _targetingReady;

    partial void TargetingStep(double dt)
    {
        if (!_targetingReady) { _targetingReady = true; RegisterObjectives(); }
        PinsStep();
    }

    // a scene won by disabling a ship: won once each listed subsystem is under the Disabled line, failed if it is destroyed
    private void RegisterObjectives()
    {
        foreach (DisableObjective o in Data.Systems.Tactical.Objectives.Where(o => o.Scenario == Scenario.Id))
            _ends.Add(() =>
            {
                if (ShipById(o.Ship) is not { } t) return false;
                if (!t.Alive)
                {
                    Say("Tactical", $"Mission failed: the {t.Name} was destroyed.");
                    Lose();
                    return true;
                }
                if (!o.Systems.All(k => t.Sys[k] < Tuning.StatusDisabledBelow)) return false;
                Say("Tactical", $"The {t.Name} is disabled.");
                Win();
                return true;
            });
    }

    // a pin stops, and says why, when its ship or the subsystem or weapon it aims at is destroyed
    private void PinsStep()
    {
        foreach (Mount m in Player.Mounts)
        {
            if (m.Pin is not { } p) continue;
            Ship? t = ShipById(p.To);
            string? why = t is not { Alive: true } ? $"{t?.Name ?? "its target"} destroyed"
                : p.Aim != "hull" && Strike.Health(t, p.Aim) <= 0 ? $"{t.Name}'s {Strike.Name(p.Aim, t).ToLowerInvariant()} destroyed" : null;
            if (why == null) continue;
            m.Pin = null;
            Say("Tactical", $"{m.Label} unpinned: {why}.");
        }
    }

    /// <summary>Aims at the hull, a subsystem, or one of the locked target's weapons ("w:" and its mount index).</summary>
    public void Aim(string aim)
    {
        if (!Strike.Valid(aim, TargetOf(Player)) || Player.Aim == aim) return;
        Player.Aim = aim;
        Say("Tactical", $"Aiming at {Strike.Name(aim, TargetOf(Player)).ToLowerInvariant()}.");
    }

    /// <summary>A pinned mount's ship while it lives, else null.</summary>
    public Ship? PinTarget(Mount m) => m.Pin is { } p && ShipById(p.To) is { Alive: true } t ? t : null;

    /// <summary>Pins one of the commanded ship's weapons to an enemy with the captain's aim; the same pin again unpins it.</summary>
    public void Pin(int mount, string shipId)
    {
        if (mount < 0 || mount >= Player.Mounts.Count) return;
        Mount m = Player.Mounts[mount];
        if (m.Weapon.IsPointDefence || ShipById(shipId) is not { Alive: true } t || t.Side == Player.Side) return;
        if (m.Pin?.To == shipId) { m.Pin = null; Say("Tactical", $"{m.Label} unpinned."); return; }
        m.Pin = new Pin(t.Id, Player.Aim);
        Say("Tactical", $"{m.Label} pinned to {t.Name}{AimedAt(Player.Aim, t)}.");
    }

    /// <summary>Pins every selected weapon to the locked enemy with the captain's aim.</summary>
    public void PinSelected()
    {
        if (TargetOf(Player) is not { } t || t.Side == Player.Side) return;
        Mount[] sel = Player.Mounts.Where(m => m.Selected && !m.Weapon.IsPointDefence).ToArray();
        if (sel.Length == 0) return;
        foreach (Mount m in sel) m.Pin = new Pin(t.Id, Player.Aim);
        Say("Tactical", $"{string.Join(", ", sel.Select(m => m.Label))} pinned to {t.Name}{AimedAt(Player.Aim, t)}.");
    }

    private static string AimedAt(string aim, Ship t) => aim == "hull" ? "" : $", aimed at {Strike.Name(aim, t)}";

    /// <summary>Unpins every weapon.</summary>
    public void UnpinAll()
    {
        foreach (Mount m in Player.Mounts) m.Pin = null;
        Say("Tactical", "All weapons unpinned.");
    }

    /// <summary>Sets point defence's mode (the Point defence station).</summary>
    public void SetPd(PdMode mode) => Player.Pd = mode;

    /// <summary>Whether a ship's point defence fires on enemy craft (fighters, shuttles) now: on Auto or Craft. The hook for
    /// the craft package (Battle.Craft.cs) to shoot craft down with; shots in flight are PointDefenceStep's, on Auto or
    /// Missiles.</summary>
    public static bool PdTakesCraft(Ship s) => s.Alive && s.Pd is PdMode.Auto or PdMode.Craft;

    /// <summary>The commanded ship's selected weapons that can fire now, each with its target (its pinned ship, else the
    /// locked enemy) and the aim it fires with (its pin's, else the captain's): what Fire looses and its key counts.</summary>
    public List<(Mount M, Ship T, string Aim)> Fireable()
    {
        var list = new List<(Mount, Ship, string)>();
        Ship s = Player;
        if (!s.Alive) return list;
        Ship? locked = TargetOf(s) is { } lt && lt.Side != s.Side ? lt : null;
        foreach (Mount m in s.Mounts)
        {
            if (!m.Selected || m.Weapon.IsPointDefence || !Rules.Ready(m)) continue;
            Ship? pinned = PinTarget(m);
            if ((pinned ?? locked) is not { } tt || !Rules.InArc(s, m, tt) || !Rules.InRange(s, m, tt)) continue;
            list.Add((m, tt, pinned != null ? m.Pin!.Aim : s.Aim));
        }
        return list;
    }

    /// <summary>The chopping block of t from where <paramref name="from"/> stands: the facing turned toward it, at the
    /// strike's reach (or reachDeg, a proximity burst's).</summary>
    public Exposure ExposureFrom(Ship t, Ship from, double reachDeg = 0) =>
        Strike.Of(t, Rules.FacingFrom(t, from.X, from.Z), Rules.Tactical.Strike, reachDeg);

    /// <summary>The chance a shot from mount m of s puts its hit on the aimed subsystem or weapon of t: its chance to hit
    /// times the direct hit's, for a precise weapon whose block from here holds the aim; 0 otherwise (the hit details).</summary>
    public double DirectHitChance(Ship s, Mount m, Ship t, string aim) =>
        m.Weapon.Precise && aim != "hull" && Strike.Exposed(ExposureFrom(t, s, Rules.ReachDeg(m)), aim) ? Rules.HitChance(s, m, t, aim) * Rules.Tactical.Strike.DirectChance : 0;
}
