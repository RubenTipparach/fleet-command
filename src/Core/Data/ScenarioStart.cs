// ScenarioStart: what a scenario may set up beyond where its ships are, for the reactor, warp and damage-control scenes
// (the battle console mockup's Warp and Escape scenes start with the reactor, the warp drive and the systems damaged): a
// ship's damage and energy at the start, and the gravity wells whose quantum-gravitational field a jump must reckon
// with. Read by DataLoader.LoadScenario and validated there (CLAUDE.md 6.4); Battle.Reactor applies a ship's start once
// the battle is built. The counts of coils, loops and nacelles are the reactor file's, so a test checks that every
// scene's lists fit them (DataTests), since scenarios load before data/battle/systems/.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Sim;

namespace FleetCommand.Data;

/// <summary>A ship's state at the start of a scenario, past its class's: energy boxes by system (warp included),
/// subsystem health, each mount's health in mount order, the reactor's coils, loops, core temperature and coolant
/// reserve, and the warp banks and nacelles. Null fields keep the class's start.</summary>
public sealed record ShipStartState(
    IReadOnlyDictionary<string, int> Power, IReadOnlyDictionary<string, double> Sys, double[]? MountsHp,
    double[]? Coils, double[]? LoopsHp, double? TempPct, double? Coolant, double? BanksE, double[]? NacellesHp)
{
    /// <summary>Reads a ship's "start" object, checking each name and number against the ship's class.</summary>
    public static ShipStartState Read(StrictJson o, ShipClass c)
    {
        var power = new Dictionary<string, int>();
        if (o.ObjOrNull("power") is { } p)
        {
            foreach (string k in p.Keys())
            {
                if (!Rules.Systems.Contains(k) && k != "warp") throw new DataError($"{p.Where}.{k}: no system '{k}'");
                int n = p.Int(k);
                if (n is < 0 or > Rules.BoxesPerSystem) throw new DataError($"{p.Where}.{k}: 0 to {Rules.BoxesPerSystem} boxes");
                power[k] = n;
            }
            p.Done();
        }
        var sys = new Dictionary<string, double>();
        if (o.ObjOrNull("sys") is { } s)
        {
            foreach (string k in s.Keys())
            {
                if (!Rules.Subsystems.Contains(k)) throw new DataError($"{s.Where}.{k}: no subsystem '{k}'");
                sys[k] = Frac(s, k);
            }
            s.Done();
        }
        double[]? mounts = o.Has("mounts_hp") ? Fracs(o, "mounts_hp") : null;
        if (mounts != null && mounts.Length > c.Mounts.Count) throw new DataError($"{o.Where}.mounts_hp: the {c.Id} has {c.Mounts.Count} mounts");
        double[]? coils = null, loops = null;
        double? temp = null, coolant = null;
        if (o.ObjOrNull("reactor") is { } r)
        {
            if (r.Has("coils")) { coils = r.NumList("coils"); if (coils.Any(v => v is < -1 or > 1)) throw new DataError($"{r.Where}.coils: each offset between -1 and 1"); }
            if (r.Has("loops_hp")) loops = Fracs(r, "loops_hp");
            if (r.Has("temp_pct")) { temp = r.Num("temp_pct"); if (temp < 0) throw new DataError($"{r.Where}.temp_pct: 0 or more"); }
            if (r.Has("coolant")) coolant = Frac(r, "coolant");
            r.Done();
        }
        double? banks = null;
        double[]? nacelles = null;
        if (o.ObjOrNull("warp") is { } w)
        {
            if (w.Has("banks_e")) { banks = w.Num("banks_e"); if (banks < 0) throw new DataError($"{w.Where}.banks_e: 0 or more"); }
            if (w.Has("nacelles_hp")) nacelles = Fracs(w, "nacelles_hp");
            w.Done();
        }
        o.Done();
        return new ShipStartState(power, sys, mounts, coils, loops, temp, coolant, banks, nacelles);
    }

    private static double Frac(StrictJson o, string k) { double v = o.Num(k); if (v is < 0 or > 1) throw new DataError($"{o.Where}.{k}: between 0 and 1"); return v; }

    private static double[] Fracs(StrictJson o, string k)
    {
        double[] v = o.NumList(k);
        if (v.Any(x => x is < 0 or > 1)) throw new DataError($"{o.Where}.{k}: each between 0 and 1");
        return v;
    }
}

/// <summary>A gravity well (a neutron star, a black hole, an anomaly): its quantum-gravitational instability is Strength
/// at its centre, falling to 0 at ReachM; RadiusM is the body's size as the view draws it. Metres.</summary>
public sealed record WellDef(string Name, double XM, double ZM, double RadiusM, double ReachM, double Strength)
{
    /// <summary>A scenario's "wells" list.</summary>
    public static IReadOnlyList<WellDef>? ReadList(StrictJson root)
    {
        if (!root.Has("wells")) return null;
        var list = new List<WellDef>();
        foreach (StrictJson w in root.ObjList("wells"))
        {
            double r = w.Num("r_m"), reach = w.Num("reach_m"), k = w.Num("strength");
            if (r <= 0 || reach <= r) throw new DataError($"{w.Where}: r_m above 0 and reach_m past it");
            if (k is <= 0 or > 1) throw new DataError($"{w.Where}.strength: above 0 and at most 1");
            list.Add(new WellDef(w.Str("name"), w.Num("x_m"), w.Num("z_m"), r, reach, k));
            w.Done();
        }
        return list;
    }

    /// <summary>The instability this well gives at a point, 0 to Strength.</summary>
    public double At(double x, double z) => Strength * Math.Clamp(1 - Geometry.Dist(x, z, XM, ZM) / ReachM, 0, 1);
}
