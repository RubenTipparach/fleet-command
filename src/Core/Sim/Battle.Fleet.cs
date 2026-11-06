// Battle.Fleet: the other ships of your fleet (survey G3: "with ai wingmen, and behaviors configuratble for AI"; the battle
// console mockup's Fleet station, fleetOrder and ai). Each wingman has standing orders the captain sets on the Fleet
// station: a stance (attack, escort, hold, hunt), a target rule (your target, the nearest, the weakest), a fighting range,
// when to retreat and whether to fire; and one order sets every wingman's stance at once. An AI captain's think (Battle.cs
// Think) reads them through AiTarget and Retreating here, the one place they mean anything.
using System.Linq;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    /// <summary>A stance's name as the console says it.</summary>
    public static string StanceName(Stance s) => s switch { Stance.Escort => "Escort", Stance.Hold => "Hold", Stance.Hunt => "Hunt", _ => "Attack" };

    // the enemy an AI captain fights: the weakest by hull for a hunter or a Weakest rule, its fleet captain's target for a
    // wingman on Mine (an enemy, never a friendly the captain has selected), else the nearest
    private Ship AiTarget(Ship s, Ship[] foes, Ship near)
    {
        if (s.Rule == TargetRule.Weakest || s.Stance == Stance.Hunt) return foes.OrderBy(o => o.Hull / o.Class.HullHp).ThenBy(o => o.Order).First();
        if (s.Rule == TargetRule.Mine && s.Side == Player.Side && s != Player && Player.Alive && TargetOf(Player) is { } pt && pt.Side != s.Side && !pt.Struck) return pt;
        return near;
    }

    // a captain told to retreat breaks off below that share of its hull (tactical.json retreat_below_hull_frac)
    private bool Retreating(Ship s) =>
        s.Retreat != RetreatAt.Never && s.Hull / s.Class.HullHp < Rules.Tactical.RetreatBelowHullFrac[s.Retreat == RetreatAt.Half ? "half" : "quarter"];

    /// <summary>The ships of your fleet, living or lost, in scenario order (the Fleet station's tabs).</summary>
    public Ship[] FleetAll() => _ships.Where(s => s.Side == Player.Side).ToArray();

    /// <summary>Gives every living wingman one stance (the fleet orders: all attack, form on me, all hold, hunt).</summary>
    public void FleetOrder(Stance stance)
    {
        foreach (Ship s in _ships) if (s.Side == Player.Side && s != Player && s.Alive) s.Stance = stance;
        Say("Comms", $"Fleet: {StanceName(stance)}.");
    }

    // a living wingman of the commanded ship's side, never the ship it commands
    private Ship? Wingman(string id) => ShipById(id) is { Alive: true } s && s.Side == Player.Side && s != Player ? s : null;

    /// <summary>A wingman's stance.</summary>
    public void SetStance(string id, Stance v) { if (Wingman(id) is { } s) s.Stance = v; }
    /// <summary>A wingman's target rule.</summary>
    public void SetRule(string id, TargetRule v) { if (Wingman(id) is { } s) s.Rule = v; }
    /// <summary>A wingman's fighting range.</summary>
    public void SetRange(string id, Data.AiRange v) { if (Wingman(id) is { } s) s.Range = v; }
    /// <summary>When a wingman retreats.</summary>
    public void SetRetreat(string id, RetreatAt v) { if (Wingman(id) is { } s) s.Retreat = v; }
    /// <summary>Whether a wingman holds its fire.</summary>
    public void SetHoldFire(string id, bool hold) { if (Wingman(id) is { } s) s.HoldFire = hold; }
}
