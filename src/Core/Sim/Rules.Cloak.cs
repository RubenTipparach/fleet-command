// Rules.Cloak: what electronic warfare, a nebula and a cloak are worth in the chance to hit, each in one place (CLAUDE.md
// 6.1): the shot, the weapons panel, the hover cards and every readout of the chance call Rules.HitChance, which adds Ew.
// After the battle console mockup's hitChance: ECCM adds and the target's ECM takes off ecm_step a point, both counted
// ew_gain_x x the nebula's density more inside one, which also costs every shot its hit_penalty x density; a target on
// passive sensors is passive_hit harder to hit and a revealed cloaked one the cloak's hit_penalty. The comms officer's
// jamming (OFFICER.jam, taken off the target's ECM) waits for the officers package.
using System;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public sealed partial class Rules
{
    /// <summary>The cloak package's numbers, set by the battle (null: plain ECM and ECCM only).</summary>
    public CloakSystems? CloakData { get; set; }
    /// <summary>The battle's nebula, or null.</summary>
    public NebulaDef? Nebula { get; set; }

    /// <summary>The nebula's density at a point (metres): its density in the middle, thinning to the edge, 0 outside.</summary>
    public double NebulaAt(double x, double z)
    {
        if (Nebula is not { } n || CloakData is not { } c) return 0;
        double d = Geometry.Dist(x, z, n.XM, n.ZM);
        return d < n.RM ? n.Density * (1 - c.Nebula.EdgeThinFrac * d / n.RM) : 0;
    }

    /// <summary>The electronic warfare term of the chance to hit for a shot from s at t (added to it).</summary>
    public double Ew(Ship s, Ship t)
    {
        double nb = Math.Max(NebulaAt(s.X, s.Z), NebulaAt(t.X, t.Z));
        CloakSystems? c = CloakData;
        double gain = c?.Nebula.EwGainX ?? 0, pen = c?.Nebula.HitPenalty ?? 0;
        double p = (_t.EcmStep * s.Eccm - _t.EcmStep * Math.Max(0, t.Ecm)) * (1 + gain * nb) - pen * nb;
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
