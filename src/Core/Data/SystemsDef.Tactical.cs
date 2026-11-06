// SystemsDef.Tactical: the numbers of weapon targeting, point defence, the fleet and the tactical map, read from
// data/battle/systems/tactical.json (openspec/changes/weapon-targeting). It lives in its own part of SystemsDef so the
// other system packages never edit these lines, and it validates its file strictly (CLAUDE.md 6.4): every key read, every
// fraction between 0 and 1, every subsystem placed once. The rules that use them are Sim/Strike.cs and Battle.Targeting.cs.
using System;
using System.Collections.Generic;
using System.Linq;

namespace FleetCommand.Data;

/// <summary>Where a hit lands (the chopping block): the block's reach either side of the facing struck (degrees) and its
/// internal radius (half-lengths); the chances a precise hit lands on the aim, that another hit damages a block subsystem,
/// that a hit damages an exposed mount (and the extra for a direct hit aimed at weapons control); the share of a mount's
/// health a hit takes; a subsystem's and the reactor's loss a point through the shields; each subsystem's place in the hull
/// in half-lengths (x to starboard, y forward).</summary>
public sealed record StrikeDef(double ReachDeg, double InternalHalfLen, double DirectChance, double SpreadChance, double MountChance, double MountAimChance,
    double MountHitFrac, double SysPerDmg, double ReactorPerDmg, IReadOnlyDictionary<string, (double X, double Y)> SubsystemsHalfLen);

/// <summary>A test scene won by disabling a ship, not destroying it: its scenario, the ship, the subsystems to disable.</summary>
public sealed record DisableObjective(string Scenario, string Ship, IReadOnlyList<string> Systems);

/// <summary>Targeting, the fleet and the map: the strike, the chance to hit given up aiming at a subsystem, the hull shares
/// a wingman retreats below (by setting), the map's scales (km) and the scenes won by disabling.</summary>
public sealed record TacticalDef(StrikeDef Strike, double AimHitPenalty, IReadOnlyDictionary<string, double> RetreatBelowHullFrac, IReadOnlyList<double> MapScalesKm,
    IReadOnlyList<DisableObjective> Objectives);

public sealed partial class SystemsDef
{
    /// <summary>Weapon targeting, point defence, the fleet and the tactical map (data/battle/systems/tactical.json).</summary>
    public TacticalDef Tactical { get; private set; } = null!;

    partial void LoadTactical(StrictJson o)
    {
        static double Frac(StrictJson j, string k)
        {
            double v = j.Num(k);
            if (v is < 0 or > 1) throw new DataError($"{j.Where}.{k}: between 0 and 1, not {v}");
            return v;
        }
        StrictJson s = o.Obj("strike");
        double reach = s.Num("reach_deg");
        if (reach is <= 0 or > 180) throw new DataError($"{s.Where}.reach_deg: above 0 and at most 180");
        double internalR = s.Num("internal_half_len");
        if (internalR < 0) throw new DataError($"{s.Where}.internal_half_len: at or above 0");
        double sysPer = s.Num("sys_per_dmg"), reactorPer = s.Num("reactor_per_dmg");
        if (sysPer < 0 || reactorPer < 0) throw new DataError($"{s.Where}: sys_per_dmg and reactor_per_dmg at or above 0");
        StrictJson p = o.Obj("subsystems_half_len");
        var pos = new Dictionary<string, (double, double)>();
        foreach (string k in p.Keys())
        {
            if (Array.IndexOf(Sim.Rules.Subsystems, k) < 0) throw new DataError($"{p.Where}.{k}: no subsystem '{k}' (there are {string.Join(", ", Sim.Rules.Subsystems)})");
            double[] xy = p.NumList(k);
            if (xy.Length != 2) throw new DataError($"{p.Where}.{k}: [x, y] in half-lengths");
            pos[k] = (xy[0], xy[1]);
        }
        foreach (string k in Sim.Rules.Subsystems) if (!pos.ContainsKey(k)) throw new DataError($"{p.Where}.{k}: missing");
        p.Done();
        var strike = new StrikeDef(reach, internalR, Frac(s, "direct_chance"), Frac(s, "spread_chance"), Frac(s, "mount_chance"), Frac(s, "mount_aim_chance"),
            Frac(s, "mount_hit_frac"), sysPer, reactorPer, pos);
        s.Done();
        StrictJson r = o.Obj("retreat_below_hull_frac");
        var retreat = new Dictionary<string, double> { ["half"] = Frac(r, "half"), ["quarter"] = Frac(r, "quarter") };
        r.Done();
        double[] scales = o.NumList("map_scales_km");
        if (scales.Length == 0 || scales.Any(k => k <= 0)) throw new DataError($"{o.Where}.map_scales_km: at least one range above 0");
        var objectives = new List<DisableObjective>();
        foreach (StrictJson j in o.ObjList("objectives"))
        {
            string[] systems = j.StrList("disable");
            foreach (string k in systems)
                if (Array.IndexOf(Sim.Rules.Subsystems, k) < 0) throw new DataError($"{j.Where}.disable: no subsystem '{k}'");
            if (systems.Length == 0) throw new DataError($"{j.Where}.disable: at least one subsystem");
            objectives.Add(new DisableObjective(j.Str("scenario"), j.Str("ship"), systems));
            j.Done();
        }
        Tactical = new TacticalDef(strike, Frac(o, "aim_hit_penalty"), retreat, scales, objectives);
        o.Done();
    }
}
