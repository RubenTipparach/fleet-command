// Battle.Missiles: missiles by grade and the countermeasures against them (openspec/changes/missiles; the weapons range,
// mockup 16, approved for implementation 2026-11-05). Owner, 2026-11-04: "missiles depending on grade will have: red,
// orange, blue, green", "the missiles should move slower. Different grades have different speeds and damage values",
// "Missiles follow ships around. Can be countered with counter measures like flares, decoy shuttles, or pdc".
// - A missile takes its rack's grade (classes.json "grade", data/battle/missiles.json): its speed, damage, colour, the
//   point defence hits it takes to stop, and its fuel; it follows its target and drifts dead when the fuel is spent.
// - A ship throws flares when a missile closes inside flare.trigger_m (every cooldown_s), and launches a decoy shuttle
//   (per_ship a battle) when one closes inside decoy.trigger_km. Each missile closing on it is fooled once, with its
//   grade's chance, and chases the lure; one that reaches it spends itself there (a decoy is destroyed by the first).
//   A burnt-out flare leaves its missiles lost; a decoy gone (destroyed, out of life, or voided by its ship firing main
//   weapons) sends its missiles back for the ship (SFC2, printed pages 136-137). Point defence is PointDefenceStep.
// The rolls come from their own stream ("countermeasures"), so a battle still replays exactly from its seed.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

/// <summary>A flare or a decoy shuttle: where it is (m), how it drifts (m/s), how long it has left (s), whose it is.</summary>
public sealed class Lure
{
    public required string Kind { get; init; }   // "flare" or "decoy"
    public required string Owner { get; init; }
    public double X, Z, VX, VZ, LifeS;
    public bool Alive = true;
}

public sealed partial class Battle
{
    private readonly List<Lure> _lures = new();
    /// <summary>Every flare and decoy still out, in the order they were made.</summary>
    public IReadOnlyList<Lure> Lures => _lures;

    private Shot LaunchMissile(Ship s, Mount m, Ship t, double x, double z, int mi, bool hit)
    {
        MissileGrade g = Data.Missiles.Grade(m.Def.Grade ?? "orange");
        var sh = new Shot
        {
            Kind = "missile", X = x, Z = z, SX = x, SZ = z, TX = t.X, TZ = t.Z, To = t.Id, From = s.Id, Mount = mi, Hit = hit,
            Damage = g.Damage, SpeedMS = g.SpeedMS, Life = g.FuelS, Color = g.Color, Grade = g, PdHitsLeft = g.PdHits,
        };
        // a missile fired at a ship with a decoy out may go for the decoy from the start
        if (t.Decoy is { Alive: true } d && _cm.Next() < g.FooledByDecoyX) sh.Lure = d;
        return sh;
    }

    /// <summary>One step of a missile: it follows its lure while that lasts, else the ship it was fired at; lost, it flies
    /// on straight. It lands within shot_hit_radius_m of what it is after.</summary>
    private void MissileStep(Shot sh, double dt)
    {
        if (sh.Lure is { Alive: false } l) { if (l.Kind == "decoy") sh.Lure = null; else { sh.Lure = null; sh.Lost = true; } }
        double stepM = sh.SpeedMS * dt;
        if (sh.Lost) { sh.X += sh.DX * stepM; sh.Z += sh.DZ * stepM; return; }
        Ship? t = ShipById(sh.To);
        if (sh.Lure == null && t is not { Alive: true }) { sh.Life = Math.Min(sh.Life, 0.4); return; }
        double ax = sh.Lure?.X ?? t!.X, az = sh.Lure?.Z ?? t!.Z;
        double d = Geometry.Dist(sh.X, sh.Z, ax, az);
        if (d <= stepM + Tuning.ShotHitRadiusM)
        {
            if (sh.Lure is { } lure) { if (lure.Kind == "decoy") EndDecoy(lure); }
            else if (sh.Hit && !sh.Spent) { Ship? from = ShipById(sh.From); Damage(t!, from?.X ?? sh.X, from?.Z ?? sh.Z, from, sh.Damage, sh.Kind, 0, sh); }
            sh.Spent = true; sh.Life = 0;
            return;
        }
        sh.DX = (ax - sh.X) / d; sh.DZ = (az - sh.Z) / d;
        sh.X += sh.DX * stepM; sh.Z += sh.DZ * stepM;
    }

    // the missiles closing on a ship that are still after it (no lure, not lost), in the stable shot order
    private IEnumerable<Shot> Closing(Ship s) => _shots.Where(sh => sh.Kind == "missile" && sh.To == s.Id && !sh.Spent && sh.Lure == null && !sh.Lost);

    private void CountermeasuresStep(double dt)
    {
        MissileData md = Data.Missiles;
        foreach (Ship s in _ships)
        {
            if (!s.Alive) continue;
            var closing = Closing(s).ToList();
            if (closing.Count == 0) continue;
            double nearestM = closing.Min(sh => Geometry.Dist(sh.X, sh.Z, s.X, s.Z));
            if (nearestM <= md.Flare.TriggerM && T >= s.FlareReadyT)
            {
                s.FlareReadyT = T + md.Flare.CooldownS;
                var flares = new List<Lure>();
                for (int k = 0; k < md.Flare.Count; k++)
                {
                    double a = s.H + Math.PI / 2 + k * 2 * Math.PI / md.Flare.Count;   // thrown out round the hull
                    var f = new Lure { Kind = "flare", Owner = s.Id, X = s.X, Z = s.Z, VX = Math.Sin(a) * md.Flare.EjectMS, VZ = -Math.Cos(a) * md.Flare.EjectMS, LifeS = md.Flare.BurnS };
                    flares.Add(f); _lures.Add(f);
                }
                foreach (Shot sh in closing) if (_cm.Next() < sh.Grade!.FooledByFlareX) sh.Lure = flares[_cm.Below(flares.Count)];
                if (s == Player) Say("Tactical", "Flares away.");
                continue;
            }
            if (s.Decoy == null && s.DecoysLeft > 0 && nearestM / 1000 <= md.Decoy.TriggerKm)
            {
                s.DecoysLeft--;
                var f = Geometry.Forward(s.H);
                var d = new Lure { Kind = "decoy", Owner = s.Id, X = s.X - f.X * s.Class.FrameM * 0.5, Z = s.Z - f.Z * s.Class.FrameM * 0.5, VX = -f.X * md.Decoy.SpeedMS, VZ = -f.Z * md.Decoy.SpeedMS, LifeS = md.Decoy.LifeS };
                s.Decoy = d; _lures.Add(d);
                foreach (Shot sh in closing) if (_cm.Next() < sh.Grade!.FooledByDecoyX) sh.Lure = d;
                if (s == Player) Say("Tactical", "Decoy launched.");
            }
        }
        foreach (Lure l in _lures)
        {
            if (!l.Alive) continue;
            double k = l.Kind == "flare" ? Math.Exp(-0.6 * dt) : 1;   // flares slow as they burn; a decoy flies on
            l.VX *= k; l.VZ *= k; l.X += l.VX * dt; l.Z += l.VZ * dt;
            l.LifeS -= dt;
            if (l.LifeS <= 0) { if (l.Kind == "decoy") EndDecoy(l); else l.Alive = false; }
        }
        _lures.RemoveAll(l => !l.Alive);
    }

    // a decoy's end: destroyed by a missile, out of life, or voided; its missiles go back for the ship (MissileStep)
    private void EndDecoy(Lure d)
    {
        d.Alive = false;
        if (ShipById(d.Owner) is { } s && s.Decoy == d) s.Decoy = null;
    }

    /// <summary>Firing main weapons voids a ship's decoy (data/battle/missiles.json decoy.voided_by_firing).</summary>
    private void VoidDecoy(Ship s)
    {
        if (Data.Missiles.Decoy.VoidedByFiring && s.Decoy is { Alive: true } d) EndDecoy(d);
    }
}
