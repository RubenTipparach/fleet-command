// Rules: what a ship's numbers are worth, each computed in exactly one place (CLAUDE.md section 6.1). The chance to hit
// that the weapons panel, the hover card and the view's readout show is HitChance, the same call that rolls the shot;
// the shield facing the systems display marks is FacingFrom, the same call that takes the damage; the speed limit the
// bottom strip locks the boxes at is MaxSpeed, the same number Battle.Move obeys.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public sealed partial class Rules
{
    /// <summary>The systems energy is allocated to, in the power station's order.</summary>
    public static readonly string[] Systems = { "weapons", "shields", "engines", "sensors", "tractor", "transport", "repair" };
    /// <summary>The subsystems that take damage and show a status box on the systems display.</summary>
    public static readonly string[] Subsystems = { "weapons", "shields", "sensors", "reactor", "engines", "tractor", "transport" };
    /// <summary>Boxes in one system's allocation row.</summary>
    public const int BoxesPerSystem = 10;
    /// <summary>Facing names, clockwise from the bow.</summary>
    public static readonly string[] Facings = { "F", "FR", "AR", "A", "AL", "FL" };

    private readonly Tuning _t;
    public Rules(Tuning tuning) { _t = tuning; }
    public Tuning Tuning => _t;

    /// <summary>The reactor's output this moment, E: its rating, less what misaligned coils lose (Rules.Reactor).</summary>
    public double Output(Ship s) => s.Class.ReactorE * Efficiency(s);

    /// <summary>What the allocation draws, E: the systems' boxes, the coolant pumps and the warp coils (Rules.Reactor).</summary>
    public int Used(Ship s) => Systems.Sum(k => s.Power.GetValueOrDefault(k)) + ReactorDraw(s);

    /// <summary>How many more boxes a system holding <paramref name="current"/> may be given: up to ten, within the output,
    /// unless the captain lets the allocation draw past it (Ship.Overdraw).</summary>
    public int PowerRoom(Ship s, int current) => s.Overdraw ? NoCap : (int)Math.Floor(Output(s) + 1e-6) - Used(s) + current;

    /// <summary>The speed limit, m/s: the engines' boxes, the class's top speed, the engines' health and any brownout.</summary>
    public double MaxSpeed(Ship s) =>
        Math.Min(s.Class.SpeedMaxMS, s.Power.GetValueOrDefault("engines") * _t.SpeedPerBoxMS) * (0.4 + 0.6 * s.Sys["engines"]) * s.Brown * s.SurgeSpeedX;

    /// <summary>What a hit does to a facing (special-weapons design): pierce of it goes straight to the hull, the shield
    /// takes the rest up to what it holds, and what it cannot take goes to the hull too.</summary>
    public static (double Held, double ToHull) Pierced(double dmg, double pierce, double shield)
    {
        double held = Math.Min(Math.Max(0, shield), dmg * (1 - Math.Clamp(pierce, 0, 1)));
        return (held, dmg - held);
    }

    /// <summary>The chorus lance's damage a second with joined ships joining it.</summary>
    public static double ChorusDps(ChorusDef c, int joined) => c.DmgPerS * (1 + c.PerJoinerX * Math.Min(joined, c.MaxJoiners));

    /// <summary>A blood charge's ram: damage for the closing speed (m/s), by the rammer's battle length.</summary>
    public static double RamDamage(ChargeDef c, double closingMS, double lengthM) => Math.Max(0, closingMS) / 100 * c.DmgPer100MS * lengthM / c.MassRefM;

    /// <summary>The figures a mount fires with in its arming state.</summary>
    public static WeaponMode Mode(Mount m) => m.State == Arming.Overload && m.Weapon.Overload != null ? m.Weapon.Overload : m.Weapon.Normal;

    /// <summary>Bearing of a point from a ship, radians: 0 dead ahead, positive to starboard.</summary>
    public static double Bearing(Ship s, double x, double z)
    {
        double dx = x - s.X, dz = z - s.Z;
        var f = Geometry.Forward(s.H);
        var r = Geometry.Right(s.H);
        return Math.Atan2(dx * r.X + dz * r.Z, dx * f.X + dz * f.Z);
    }

    /// <summary>The shield facing of s that a point at (x, z) strikes: 0 F, 1 FR, 2 AR, 3 A, 4 AL, 5 FL.</summary>
    public static int FacingFrom(Ship s, double x, double z)
    {
        double b = Bearing(s, x, z) / Geometry.Deg;
        return (int)Math.Floor((((b + 30) % 360) + 360) % 360 / 60) % 6;
    }

    public static double DistKm(Ship a, Ship b) => Geometry.Dist(a.X, a.Z, b.X, b.Z) / 1000.0;

    public static bool InArc(Ship s, Mount m, Ship t)
    {
        if (m.Def.ArcWidthDeg >= 360) return true;
        double b = Bearing(s, t.X, t.Z), c = m.Def.ArcCentreDeg * Geometry.Deg;
        return Math.Abs(Geometry.Wrap(b - c)) <= m.Def.ArcWidthDeg * Geometry.Deg / 2;
    }

    public static bool InRange(Ship s, Mount m, Ship t) => DistKm(s, t) <= Mode(m).RangeKm;

    public static bool Ready(Mount m) => m.State != Arming.Offline && m.Charge >= 1 && m.Hp > 0 && m.StunS <= 0;

    /// <summary>Seconds until a mount is ready at the rate Battle charges it, or null when it is not cooling.</summary>
    public static double? CooldownS(Mount m) => m.State == Arming.Offline || m.Hp <= 0 || m.Charge >= 1 ? null : (1 - m.Charge) * Mode(m).ChargeS;

    /// <summary>Cooldown text: one decimal under 10 s, rounded up so it never reads 0.0 while cooling; whole seconds from 10 s.</summary>
    public static string FormatCooldown(double seconds)
    {
        double t = Math.Ceiling(seconds * 10) / 10;
        return t < 10 ? t.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : ((int)Math.Ceiling(seconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>The chance a shot from mount m of s hits t. Every readout of it and the shot itself call this.</summary>
    public double HitChance(Ship s, Mount m, Ship? t)
    {
        if (t == null || !t.Alive || !InRange(s, m, t)) return 0;
        WeaponMode w = Mode(m);
        double d = DistKm(s, t);
        double p = _t.HitChanceNear - _t.HitChanceFalloff * d / w.RangeKm;
        p -= _t.EcmStep * t.Ecm;
        p -= _t.HitSpeedPenaltyPer300MS * Math.Abs(t.V) / 300;
        p *= 0.5 + 0.5 * s.Sys["sensors"];
        // erratic manoeuvres: harder to hit, and harder to hit with
        if (t.Erratic) p *= _t.ErraticHit;
        if (s.Erratic) p *= _t.ErraticHit;
        return Math.Clamp(p, _t.HitChanceMin, _t.HitChanceMax);
    }

    /// <summary>A subsystem's status from its health and its energy.</summary>
    public SystemStatus Status(Ship s, string subsystem)
    {
        double hp = s.Sys[subsystem];
        if (hp <= 0) return SystemStatus.Destroyed;
        if (hp < _t.StatusDisabledBelow) return SystemStatus.Disabled;
        bool off = (Array.IndexOf(Systems, subsystem) >= 0 && s.Power.GetValueOrDefault(subsystem) <= 0) || (subsystem == "shields" && !s.ShieldsUp);
        if (off) return SystemStatus.Off;
        return hp < 1 ? SystemStatus.Damaged : SystemStatus.Active;
    }

    /// <summary>A mount's status from its health and its arming state.</summary>
    public SystemStatus Status(Mount m)
    {
        if (m.Hp <= 0) return SystemStatus.Destroyed;
        if (m.Hp < _t.StatusDisabledBelow) return SystemStatus.Disabled;
        if (m.State == Arming.Offline) return SystemStatus.Off;
        return m.Hp < 1 ? SystemStatus.Damaged : SystemStatus.Active;
    }

    /// <summary>A class's best high energy turn chance: smaller hulls turn hard more safely.</summary>
    public static double HetMax(Ship s) => Math.Clamp(0.95 - s.Class.LengthM / 2000, 0.6, 0.95);

    /// <summary>The chance the next high energy turn succeeds, as the helm shows it and Battle.Het rolls it: less erratic
    /// manoeuvres, plus the helm officer's skill (officers).</summary>
    public double HetChanceNow(Ship s) => Math.Max(0, s.HetChance - (s.Erratic ? _t.HetErraticPenalty : 0) + OfficerSkill(s, "helm") * SystemsData.Officers.Ability.Het);

    /// <summary>Range in km an AI captain closes to for its range setting.</summary>
    /// <summary>The range an AI captain fights at, km (the player's manoeuvres use the middle one).</summary>
    public double AiRangeKm(Data.AiRange r = Data.AiRange.Mid) => r switch { Data.AiRange.Close => _t.AiRangeCloseKm, Data.AiRange.Long => _t.AiRangeLongKm, _ => _t.AiRangeMidKm };
}
