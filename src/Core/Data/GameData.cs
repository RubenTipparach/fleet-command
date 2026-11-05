// GameData: the battle's data, typed and validated (CLAUDE.md section 6.4). Weapons, ship classes, factions, the
// scenarios and the tuning numbers, each read from data/ by DataLoader. Plain records, no behaviour: the rules that use
// them live in Sim/Rules.cs.
using System.Collections.Generic;
using System.Linq;

namespace FleetCommand.Data;

/// <summary>The figures a weapon fires with in one arming state (normal, or its overload).</summary>
public sealed record WeaponMode(double RangeKm, double ChargeS, double CostE, double Damage);

public sealed record WeaponDef(string Id, string Name, string Kind, WeaponMode Normal, WeaponMode? Overload, double SpeedMS, string Color)
{
    /// <summary>True for weapons whose shot flies (torpedo, bolt, missile, the quell and the forge slug) rather than landing at once.</summary>
    public bool Flies => Kind is "torpedo" or "bolt" or "missile" or "quell" or "slug";
    /// <summary>True for a direct-fire bolt (the disruptor): it flies, but along the live line from its mount to its target,
    /// both ends riding their ships (owner, 2026-11-04), where a seeker (torpedo, missile) flies free.</summary>
    public bool RidesLine => Kind is "bolt" or "slug";
    public bool IsPointDefence => Kind == "pd";
}

/// <summary>A weapon mount: x to starboard and y forward in half-lengths of the hull; its arc in degrees; for a missile
/// rack, the grade of missile it is loaded with (data/battle/missiles.json), else null.</summary>
public sealed record MountDef(string Weapon, double X, double Y, double ArcCentreDeg, double ArcWidthDeg, string? Grade = null);

/// <summary>A missile grade (data/battle/missiles.json; owner, 2026-11-04: "red, orange, blue, green", red the lowest): its
/// colour, speed, damage, the point defence hits it takes to stop, its fuel, and the chance a flare or a decoy fools it.</summary>
public sealed record MissileGrade(string Id, string Name, string Color, double SpeedMS, double Damage, int PdHits, double FuelS,
    double FooledByFlareX, double FooledByDecoyX);

/// <summary>Flares: how many a throw, how long they burn, how fast they leave the hull, the wait before the next throw, and
/// how close a missile comes before they are thrown.</summary>
public sealed record FlareDef(int Count, double BurnS, double EjectMS, double CooldownS, double TriggerM, string Color);

/// <summary>A decoy shuttle: its speed and life, whether firing main weapons voids it, how close a missile comes before it
/// is launched, and how many a ship carries.</summary>
public sealed record DecoyDef(double SpeedMS, double LifeS, bool VoidedByFiring, double TriggerKm, int PerShip, string Color);

/// <summary>The special weapons (data/battle/special_weapons.json; openspec/changes/special-weapons): the Concord's Chorus
/// lance and Quell torpedo, the Krai's Forge driver and Blood charge. The three that are mounts are also WeaponDefs (kinds
/// chorus, quell, slug); the charge is an order from the helm.</summary>
public sealed record ChorusDef(double BurnS, double DmgPerS, double JoinRangeKm, double PerJoinerX, int MaxJoiners, string Spends, string StrandColor, IReadOnlyList<string> Fits);
public sealed record QuellDef(double ShieldLeftX, double StunS, double ReachDeg);
public sealed record ForgeDef(double PierceX, double OverloadPierceX, double BurnDmgPerS, double BurnS);
public sealed record ChargeDef(string Name, string Faction, double SurgeS, double SpeedX, double AccelX, double TurnX, double BowShieldX, double CostE, double CooldownS,
    double DmgPer100MS, double MassRefM, double PierceX, double RecoilX, string Color);
public sealed record SpecialWeapons(ChorusDef Chorus, QuellDef Quell, ForgeDef Forge, ChargeDef Charge);

/// <summary>The missile grades and the countermeasures against them.</summary>
public sealed record MissileData(IReadOnlyList<MissileGrade> Grades, FlareDef Flare, DecoyDef Decoy)
{
    /// <summary>A grade by id; throws naming it when there is none.</summary>
    public MissileGrade Grade(string id) => Grades.FirstOrDefault(g => g.Id == id) ?? throw new KeyNotFoundException($"no missile grade '{id}' in data/battle/missiles.json");
}

/// <summary>A ship class. Shape is the systems display's top-down silhouette; Model, when set, is the approved ship's glb
/// (res:// path under assets/ships/), drawn in the battle at LengthM (the battle's framing, as the battle console mockup
/// draws its ships); without it the battle draws the stand-in for Shape (factions whose ships are not designed yet).</summary>
public sealed record ShipClass(
    string Id, string Name, string Code, string Shape, string? Model, double LengthM, double HullHp, double SpeedMaxMS, double AccelMS2,
    double TurnDegS, int ReactorE, double CapacitorE, double BatteryE, double[] ShieldsHp,
    IReadOnlyDictionary<string, int> PowerE, IReadOnlyList<MountDef> Mounts);

/// <summary>A faction. Skin is the console skin its captains play in (a UI kit id); null for a faction the player
/// cannot command yet.</summary>
public sealed record Faction(string Id, string Name, string Hull, string Trim, string? Skin);

/// <summary>An AI captain's preferred fighting range (Tuning's ai_range_*_km).</summary>
public enum AiRange { Close, Mid, Long }

/// <summary>A ship at the start of a scenario, with its captain's standing orders when an AI flies it: its stance, its
/// fighting range, whether it jinks (erratic) or holds its fire, and the hull and shields it starts with (fractions).</summary>
public sealed record ShipStart(string Id, string Name, string Captain, string Class, string Faction, int Side,
    double XM, double ZM, double HeadingDeg, double SpeedMS,
    Sim.Stance Stance = Sim.Stance.Attack, AiRange Range = AiRange.Mid, bool Erratic = false, bool HoldFire = false,
    double HullFrac = 1, double ShieldsFrac = 1);

/// <summary>A scenario: who plays it (Side, the player's faction, which picks the console skin), the seeded battle and its
/// objective, and the first officer's message. The first ship is the one the player commands.</summary>
public sealed record Scenario(string Id, string Name, string Side, ulong Seed, string Objective, string SayWho, string SayText,
    IReadOnlyList<ShipStart> Ships, string Sky);

/// <summary>Every tuning number of the stand-in battle (data/battle/tuning.json), units in the names.</summary>
public sealed record Tuning(
    double StepS, double SpeedPerBoxMS, double ReverseMaxMS, double DemoToughX,
    double HitChanceNear, double HitChanceFalloff, double HitChanceMin, double HitChanceMax, double HitSpeedPenaltyPer300MS, double EcmStep,
    double ShieldRegenPerES, double BatteryChargePerSpareES, double StartCapacitorFrac, double StartBatteryFrac,
    double RepairPerES, double MountRepairPerES, double AiThinkS, double AiFireMinChance,
    double AiRangeCloseKm, double AiRangeMidKm, double AiRangeLongKm, double ShotHitRadiusM, double BeamLifeS,
    double PdInterceptChance, double StatusDisabledBelow, double SubsystemHitChance, double SubsystemDamagePerHp,
    double MountHitChance, double MountDamage,
    double HetRateDegS, double HetCostE, double HetChanceDrop, double HetRecoverPerS, double HetErraticPenalty,
    double HetBreakdownS, double HetBreakdownEngines, double EstopDecelX, double EstopHoldS, double ErraticHit, double ErraticTurn,
    double OrbitKm, double HoldMinKm, double HoldMaxKm);

/// <summary>The console's layout in reference pixels (data/console/layout.json).</summary>
public sealed record LayoutDef(
    double ReferenceW, double ReferenceH, double MinW, double ColumnW, double StripH,
    Rect Plate, Rect Station, Rect Ship, Rect Weapons, Rect Radar,
    double ExpandedX, double ExpandedY, double ExpandedShare, double ShipScreenY, double ViewFovDeg);

/// <summary>Everything the battle needs, loaded once at startup. Scenarios are in menu order; the first is the one the
/// game starts in.</summary>
public sealed record GameData(
    IReadOnlyDictionary<string, WeaponDef> Weapons, IReadOnlyDictionary<string, ShipClass> Classes,
    IReadOnlyDictionary<string, Faction> Factions, IReadOnlyList<Scenario> Scenarios, Tuning Tuning, LayoutDef Layout, MissileData Missiles,
    SpecialWeapons Specials)
{
    /// <summary>The scenario the game starts in (the first).</summary>
    public Scenario StartScenario => Scenarios[0];

    /// <summary>A scenario by id; throws naming the id when there is none.</summary>
    public Scenario ScenarioById(string id) =>
        Scenarios.FirstOrDefault(s => s.Id == id) ?? throw new KeyNotFoundException($"no scenario '{id}' in data/battle/scenarios.json");
}
