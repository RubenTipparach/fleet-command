// GameData: the battle's data, typed and validated (CLAUDE.md section 6.4). Weapons, ship classes, factions, the
// skirmish and the tuning numbers, each read from data/ by DataLoader. Plain records, no behaviour: the rules that use
// them live in Sim/Rules.cs.
using System.Collections.Generic;

namespace FleetCommand.Data;

/// <summary>The figures a weapon fires with in one arming state (normal, or its overload).</summary>
public sealed record WeaponMode(double RangeKm, double ChargeS, double CostE, double Damage);

public sealed record WeaponDef(string Id, string Name, string Kind, WeaponMode Normal, WeaponMode? Overload, double SpeedMS, string Color)
{
    /// <summary>True for weapons whose shot flies (torpedo, bolt, missile) rather than landing at once.</summary>
    public bool Flies => Kind is "torpedo" or "bolt" or "missile";
    public bool IsPointDefence => Kind == "pd";
}

/// <summary>A weapon mount: x to starboard and y forward in half-lengths of the hull; its arc in degrees.</summary>
public sealed record MountDef(string Weapon, double X, double Y, double ArcCentreDeg, double ArcWidthDeg);

public sealed record ShipClass(
    string Id, string Name, string Code, string Shape, double LengthM, double HullHp, double SpeedMaxMS, double AccelMS2,
    double TurnDegS, int ReactorE, double CapacitorE, double BatteryE, double[] ShieldsHp,
    IReadOnlyDictionary<string, int> PowerE, IReadOnlyList<MountDef> Mounts);

public sealed record Faction(string Id, string Name, string Hull, string Trim);

public sealed record ShipStart(string Id, string Name, string Captain, string Class, string Faction, int Side,
    double XM, double ZM, double HeadingDeg, double SpeedMS);

public sealed record Scenario(ulong Seed, IReadOnlyList<ShipStart> Ships);

/// <summary>Every tuning number of the stand-in battle (data/battle/tuning.json), units in the names.</summary>
public sealed record Tuning(
    double StepS, double SpeedPerBoxMS, double ReverseMaxMS, double DemoToughX,
    double HitChanceNear, double HitChanceFalloff, double HitChanceMin, double HitChanceMax, double HitSpeedPenaltyPer300MS, double EcmStep,
    double ShieldRegenPerES, double BatteryChargePerSpareES, double StartCapacitorFrac, double StartBatteryFrac,
    double RepairPerES, double MountRepairPerES, double AiThinkS, double AiFireMinChance,
    double AiRangeCloseKm, double AiRangeMidKm, double AiRangeLongKm, double ShotHitRadiusM, double BeamLifeS,
    double PdInterceptChance, double StatusDisabledBelow, double SubsystemHitChance, double SubsystemDamagePerHp,
    double MountHitChance, double MountDamage);

/// <summary>The console's layout in reference pixels (data/console/layout.json).</summary>
public sealed record LayoutDef(
    double ReferenceW, double ReferenceH, double MinW, double ColumnW, double StripH,
    Rect Plate, Rect Station, Rect Ship, Rect Weapons, Rect Radar,
    double ExpandedX, double ExpandedY, double ExpandedShare, double ShipScreenY, double ViewFovDeg);

/// <summary>Everything the battle needs, loaded once at startup.</summary>
public sealed record GameData(
    IReadOnlyDictionary<string, WeaponDef> Weapons, IReadOnlyDictionary<string, ShipClass> Classes,
    IReadOnlyDictionary<string, Faction> Factions, Scenario Skirmish, Tuning Tuning, LayoutDef Layout);
