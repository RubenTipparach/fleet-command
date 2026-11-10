// Rules.Cloak: what electronic warfare, a nebula plume and a cloak are worth to a shot, each in one place (CLAUDE.md
// 6.1): the shot, the weapons panel, the hover cards and every readout of the chance call Rules.HitChance, which adds Ew
// and multiplies by NebulaHitX; everything that asks how far a weapon reaches calls Rules.RangeKm. After the battle
// console mockup's hitChance: ECCM adds and the target's ECM takes off ecm_step a point, both counted ew_gain_x x the
// plume's density more inside one; a target on passive sensors is passive_hit harder to hit and a revealed cloaked one
// the cloak's hit_penalty. A plume (openspec/changes/nebula-plumes) also halves the chance to hit and an energy weapon's
// range at full density, and loses missiles their target (Battle.Missiles.cs). The comms officer's jamming (OFFICER.jam,
// taken off the target's ECM) waits for the officers package.
using System;
using System.Collections.Generic;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public sealed partial class Rules
{
    /// <summary>The cloak package's numbers, set by the battle (null: plain ECM and ECCM only).</summary>
    public CloakSystems? CloakData { get; set; }
    /// <summary>The battle's nebula plumes (none: an empty list).</summary>
    public IReadOnlyList<NebulaDef> Nebulae { get; set; } = Array.Empty<NebulaDef>();

    /// <summary>The nebula's density at a point (metres): a plume's density in its middle, thinning to its edge, 0 outside;
    /// where plumes overlap, the densest.</summary>
    public double NebulaAt(double x, double z) => PlumeAt(x, z, stormsOnly: false);

    /// <summary>The ion storm's density at a point (metres): NebulaAt counting only the plumes that are storms.</summary>
    public double StormAt(double x, double z) => PlumeAt(x, z, stormsOnly: true);

    private double PlumeAt(double x, double z, bool stormsOnly)
    {
        if (CloakData is not { } c) return 0;
        double best = 0;
        foreach (NebulaDef n in Nebulae)
        {
            if (stormsOnly && !n.Storm) continue;
            double d = Geometry.Dist(x, z, n.XM, n.ZM);
            if (d < n.RM) best = Math.Max(best, n.Density * (1 - c.Nebula.EdgeThinFrac * d / n.RM));
        }
        return best;
    }

    /// <summary>The chance a second that lightning finds a ship at (x, z): storm_strikes_per_min x the storm's density there.</summary>
    public double StormStrikePerS(double x, double z) => CloakData is { } c ? c.Nebula.StormStrikesPerMin / 60 * StormAt(x, z) : 0;

    /// <summary>The nebula between two ships: the denser of where each is.</summary>
    public double NebulaBetween(Ship s, Ship t) => Math.Max(NebulaAt(s.X, s.Z), NebulaAt(t.X, t.Z));

    // a rule's multiplier at a density: 1 outside every plume, the rule's own number at full density
    private static double Thinned(double x, double nb) => 1 - (1 - x) * nb;

    /// <summary>What a plume leaves of the chance to hit for a shot from s at t (multiplies it): hit_x at full density.
    /// A missile barrage's hits take it too.</summary>
    public double NebulaHitX(Ship s, Ship t) => CloakData is { } c ? Thinned(c.Nebula.HitX, NebulaBetween(s, t)) : 1;

    /// <summary>How far mount m reaches from s, km: its mode's range, and for an energy weapon (the nebula's energy_kinds)
    /// with s in a plume, or its target t in one, energy_range_x of that at full density. With no target given, by where s is
    /// alone (the radar's range ring).</summary>
    public double RangeKm(Ship s, Mount m, Ship? t = null)
    {
        double km = Mode(m).RangeKm;
        if (CloakData is not { } c || !c.Nebula.EnergyKinds.Contains(m.Weapon.Kind)) return km;
        return km * Thinned(c.Nebula.EnergyRangeX, t == null ? NebulaAt(s.X, s.Z) : NebulaBetween(s, t));
    }

    /// <summary>True when the target is within mount m's reach from s (RangeKm).</summary>
    public bool InRange(Ship s, Mount m, Ship t) => DistKm(s, t) <= RangeKm(s, m, t);

    /// <summary>The chance a second that a missile at (x, z) after t loses its target to a plume: missile_lost_per_s x the
    /// denser of where it flies and where its target is.</summary>
    public double MissileLostPerS(double x, double z, Ship t) =>
        CloakData is { } c ? c.Nebula.MissileLostPerS * Math.Max(NebulaAt(x, z), NebulaAt(t.X, t.Z)) : 0;

    /// <summary>The electronic warfare term of the chance to hit for a shot from s at t (added to it).</summary>
    public double Ew(Ship s, Ship t)
    {
        CloakSystems? c = CloakData;
        double p = (_t.EcmStep * s.Eccm - _t.EcmStep * Math.Max(0, t.Ecm)) * (1 + (c?.Nebula.EwGainX ?? 0) * NebulaBetween(s, t));
        if (t.Passive) p -= c?.Sensors.PassiveHit ?? 0;
        if (t.Cloaked) p -= c?.Cloak.HitPenalty ?? 0;
        return p;
    }

    /// <summary>A sweep's reach, km: the base, a reach for each point of ECCM, and the science officer's levels.</summary>
    public double SweepKm(Ship s, int scienceOfficer = 0)
    {
        CloakRules c = (CloakData ?? throw new InvalidOperationException("the cloak's numbers are not set")).Cloak;
        return c.BaseKm + c.PerEccmKm * s.Eccm + scienceOfficer * c.OfficerSweepKm;
    }

    /// <summary>One side's shots in a volley aboard a ship: each of n marines fires once at p; the hits. The one rule for
    /// fights inside a ship (and for the boarding party's volleys when they come in).</summary>
    public static int Volley(int n, double p, Rng r)
    {
        int h = 0;
        for (int i = 0; i < n; i++) if (r.Next() < p) h++;
        return h;
    }
}
