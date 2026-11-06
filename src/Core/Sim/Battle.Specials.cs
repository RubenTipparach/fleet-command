// Battle.Specials: the special weapons (openspec/changes/special-weapons; the weapons range, mockup 16, approved for
// implementation 2026-11-05: "Implement weapons in game"). Their numbers are data/battle/special_weapons.json; what a hit
// is worth is Rules (Pierced, ChorusDps, RamDamage), each in one place.
// - Chorus lance (Concord, the cruiser's deflector): one long beam for burn_s. Every Concord ship within join.range_km
//   with a lance ready, in arc and in range of the target, joins it and spends that lance; each one that joins adds
//   per_joiner_x, up to max_joiners. It lands in ticks of ChorusTickS while it burns.
// - Quell torpedo (Concord, a torpedo tube's load): a seeker that does no damage; it drops the struck facing's shield to
//   shield_left_x and stuns every mount within reach_deg of that facing for stun_s.
// - Forge driver (Krai, the spinal cannon): a direct-fire slug on the live line; pierce_x of it goes through the shield,
//   and the hull burns burn.dmg_per_s for burn.s.
// - Blood charge (Krai, from the helm): a surge of surge_s at speed_x, accel_x and turn_x, the bow shield holding
//   bow_shield_x; a ram while it lasts does RamDamage by the closing speed, pierce_x through the struck shield, and
//   recoil_x of it back on the rammer's bow.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    /// <summary>A chorus lance and a forge burn land in ticks of this many seconds.</summary>
    public const double ChorusTickS = 0.5, BurnTickS = 0.5;

    private Shot FireChorus(Ship s, Mount m, Ship t, double x, double z, int mi, bool hit)
    {
        ChorusDef c = Data.Specials.Chorus;
        var joined = new List<(string, int)>();
        foreach (Ship o in _ships)
        {
            if (joined.Count >= c.MaxJoiners) break;
            if (o == s || !o.Alive || o.Side != s.Side || Rules.DistKm(o, s) > c.JoinRangeKm) continue;
            Mount? lance = o.Mounts.FirstOrDefault(q => q.Weapon.Id == c.Spends && Rules.Ready(q) && Rules.InArc(o, q, t) && Rules.InRange(o, q, t));
            if (lance == null) continue;
            lance.Charge = 0;   // it spends its lance
            joined.Add((o.Id, lance.Index));
        }
        if (s == Player) Say("Tactical", joined.Count == 0 ? "Chorus lance firing." : $"Chorus lance firing, {joined.Count} joined.");
        return new Shot
        {
            Kind = "chorus", X = x, Z = z, SX = x, SZ = z, TX = t.X, TZ = t.Z, To = t.Id, From = s.Id, Mount = mi, Hit = hit,
            Damage = Rules.ChorusDps(c, joined.Count), Life = c.BurnS, Color = m.Weapon.Color, Joined = joined,
        };
    }

    // a burning chorus: its ends follow the ships (Ends), and it lands Damage a second in ticks
    private void ChorusStep(Shot sh, double dt)
    {
        Ends(sh);
        Ship? t = ShipById(sh.To), from = ShipById(sh.From);
        if (t is not { Alive: true } || from is not { Alive: true }) { sh.Life = Math.Min(sh.Life, 0.3); return; }
        if (!sh.Hit) return;
        sh.TravelledM += dt;   // the tick clock
        while (sh.TravelledM >= ChorusTickS - 1e-9)
        {
            sh.TravelledM -= ChorusTickS;
            Damage(t, from.X, from.Z, from, sh.Damage * ChorusTickS, "chorus", 0, sh);
        }
    }

    // a quell landing: no damage; the facing's shield to shield_left_x and the mounts on that side stunned
    private void Quelled(Ship t, double sx, double sz)
    {
        QuellDef q = Data.Specials.Quell;
        int f = Rules.FacingFrom(t, sx, sz);
        if (t.ShieldsUp) t.Shields[f] *= q.ShieldLeftX;
        t.Flash[f] = 1;
        int stunned = 0;
        foreach (Mount m in t.Mounts)
        {
            double b = Math.Atan2(m.Def.X, m.Def.Y) / Geometry.Deg;   // the mount's bearing from the centre, + to starboard
            if (Math.Abs(Geometry.Wrap((b - f * 60) * Geometry.Deg)) / Geometry.Deg <= q.ReachDeg) { m.StunS = q.StunS; stunned++; }
        }
        Record(t, sx, sz, "quell", 0, 0);
        if (t == Player) Say("Engineering", $"Quelled: the {Rules.Facings[f]} shield is down and {stunned} weapons are out.");
        else if (t.Side != Player.Side) Say("Tactical", $"{t.Name} quelled: {stunned} weapons out.");
    }

    // a forge slug's burn on the hull
    private void Burn(Ship t, double sx, double sz)
    {
        ForgeDef f = Data.Specials.Forge;
        t.BurnS = f.BurnS; t.BurnDps = f.BurnDmgPerS; t.BurnFromX = sx; t.BurnFromZ = sz; t.BurnTick = 0;
        if (t == Player) Say("Engineering", "Hull burning.");
    }

    // one fixed step of a ship's special states: stunned mounts recover, a burn lands in ticks, a surge runs out
    private void SpecialsStep(Ship s, double dt)
    {
        foreach (Mount m in s.Mounts) if (m.StunS > 0) m.StunS = Math.Max(0, m.StunS - dt);
        if (s.BurnS > 0)
        {
            s.BurnS = Math.Max(0, s.BurnS - dt);
            s.BurnTick += dt;
            while (s.BurnTick >= BurnTickS - 1e-9 && s.Alive) { s.BurnTick -= BurnTickS; Damage(s, s.BurnFromX, s.BurnFromZ, null, s.BurnDps * BurnTickS, "burn", 1); }
        }
        if (s.SurgeS > 0) { s.SurgeS = Math.Max(0, s.SurgeS - dt); if (s.SurgeS <= 0) EndSurge(s); }
    }

    /// <summary>True when s can order a blood charge now: a Krai ship, ready again, with the energy for it (the capacitor
    /// and the battery together, as a weapon charges).</summary>
    public bool CanCharge(Ship s) => s.Alive && s.Faction.Id == Data.Specials.Charge.Faction && s.SurgeS <= 0 && T >= s.ChargeReadyT && s.Capacitor + s.Battery >= Data.Specials.Charge.CostE && s.Broke <= 0;

    /// <summary>Orders a blood charge: the drive surges for surge_s. Returns false (changing nothing) when it cannot.</summary>
    public bool Charge(Ship s)
    {
        if (!CanCharge(s)) return false;
        ChargeDef c = Data.Specials.Charge;
        double fromCap = Math.Min(s.Capacitor, c.CostE);
        s.Capacitor -= fromCap; s.Battery -= c.CostE - fromCap;
        s.SurgeS = c.SurgeS; s.SurgeSpeedX = c.SpeedX; s.SurgeAccelX = c.AccelX; s.SurgeTurnX = c.TurnX;
        s.ChargeReadyT = T + c.SurgeS + c.CooldownS;
        s.VOrder = Rules.MaxSpeed(s);
        if (s == Player) Say("Helm", "Blood charge!");
        return true;
    }

    private static void EndSurge(Ship s) { s.SurgeS = 0; s.SurgeSpeedX = 1; s.SurgeAccelX = 1; s.SurgeTurnX = 1; }

    // a surging ship that reaches an enemy ahead rams it with its bayonet
    private void ChargeStep(double dt)
    {
        ChargeDef c = Data.Specials.Charge;
        foreach (Ship s in _ships)
        {
            if (!s.Alive || s.SurgeS <= 0) continue;
            foreach (Ship t in _ships)
            {
                if (!t.Alive || t.Side == s.Side) continue;
                double d = Geometry.Dist(s.X, s.Z, t.X, t.Z);
                if (d > (s.Class.LengthM + t.Class.LengthM) * 0.35 || Math.Abs(Rules.Bearing(s, t.X, t.Z)) > 40 * Geometry.Deg) continue;
                var sf = Geometry.Forward(s.H); var tf = Geometry.Forward(t.H);
                double closing = s.V - (tf.X * sf.X + tf.Z * sf.Z) * t.V;
                double dmg = Rules.RamDamage(c, closing, s.Class.LengthM);
                Damage(t, s.X, s.Z, s, dmg, "ram", c.PierceX);
                Damage(s, t.X, t.Z, t, dmg * c.RecoilX, "ram");   // the bow, while the surge still holds it
                if (s == Player || t == Player) Say("Tactical", $"{s.Name} rams {t.Name}: {dmg:0} to the hull.");
                s.V = -0.12 * s.V;
                EndSurge(s);
                break;
            }
        }
    }

    // an AI Krai captain charges a target close ahead; while it surges it drives straight at it
    private void ThinkCharge(Ship s, Ship t)
    {
        if (s.SurgeS > 0) { s.HOrder = Geometry.HeadingTo(s.X, s.Z, t.X, t.Z); s.VOrder = Rules.MaxSpeed(s); return; }
        if (s.HoldFire || !CanCharge(s)) return;
        if (Rules.DistKm(s, t) <= 3 && Math.Abs(Rules.Bearing(s, t.X, t.Z)) <= 25 * Geometry.Deg && Charge(s))
        { s.HOrder = Geometry.HeadingTo(s.X, s.Z, t.X, t.Z); }
    }
}
