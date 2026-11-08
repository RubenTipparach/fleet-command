// GameData: the battle's data, typed and validated (CLAUDE.md section 6.4). Weapons, ship classes, factions, the
// scenarios and the tuning numbers, each read from data/ by DataLoader. Plain records, no behaviour: the rules that use
// them live in Sim/Rules.cs.
using System.Collections.Generic;
using System.Linq;

namespace FleetCommand.Data;

/// <summary>The figures a weapon fires with in one arming state (normal, its overload or its proximity fuse): HitBonus is
/// added to the chance to hit, IgnoreSpeed drops the target's speed from it, ReachDeg (above 0) is the chopping block's
/// reach either side of the facing struck in place of the strike's own, Color (when set) the shot's colour.</summary>
public sealed record WeaponMode(double RangeKm, double ChargeS, double CostE, double Damage, double HitBonus = 0, bool IgnoreSpeed = false, double ReachDeg = 0, string? Color = null);

/// <summary>A weapon type. Aim is "precise" (a hit can land on the aimed subsystem) or "arc" (anywhere the facing struck
/// exposes); OwnPower charges it from its own generator, whatever weapons control's state (weapon-targeting design).</summary>
public sealed record WeaponDef(string Id, string Name, string Kind, WeaponMode Normal, WeaponMode? Overload, double SpeedMS, string Color,
    WeaponMode? Proximity = null, string Aim = "arc", bool OwnPower = false)
{
    public bool Precise => Aim == "precise";
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
/// draws its ships); without it the battle draws the stand-in for Shape (factions whose ships are not designed yet).
/// ShieldLayers is how many bands its shield display shows at full strength: a frigate one, larger ships more (owner,
/// 2026-11-06: "frigates have only 1 layer of shields as opposed to larger ships").</summary>
public sealed record ShipClass(
    string Id, string Name, string Code, string Shape, string? Model, double LengthM, double HullHp, double SpeedMaxMS, double AccelMS2,
    double TurnDegS, int ReactorE, double CapacitorE, double BatteryE, double[] ShieldsHp,
    IReadOnlyDictionary<string, int> PowerE, IReadOnlyList<MountDef> Mounts, int ShieldLayers = 3)
{
    private readonly double? _frameM;
    /// <summary>The ship's length as the battle frames it, metres: LengthM times Tuning.ShipFrameX (LengthM when unset).
    /// Its model is drawn this long, and every geometry of the hull in the battle (mounts, ramming, rocks) is at it.</summary>
    public double FrameM { get => _frameM ?? LengthM; init => _frameM = value; }
}

/// <summary>A faction. Skin is the console skin its captains play in (a UI kit id); null for a faction the player
/// cannot command yet.</summary>
public sealed record Faction(string Id, string Name, string Hull, string Trim, string? Skin);

/// <summary>An AI captain's preferred fighting range (Tuning's ai_range_*_km).</summary>
public enum AiRange { Close, Mid, Long }

/// <summary>A ship at the start of a scenario, with its captain's standing orders when an AI flies it: its stance, its
/// fighting range, whether it jinks (erratic) or holds its fire, and the hull and shields it starts with (fractions); Start,
/// its damage and energy past its class's (ScenarioStart), or null.</summary>
public sealed record ShipStart(string Id, string Name, string Captain, string Class, string Faction, int Side,
    double XM, double ZM, double HeadingDeg, double SpeedMS,
    Sim.Stance Stance = Sim.Stance.Attack, AiRange Range = AiRange.Mid, bool Erratic = false, bool HoldFire = false,
    double HullFrac = 1, double ShieldsFrac = 1, bool Prize = false, ShipStartState? Start = null, CloakStart? Cloak = null, ShipSetup? Setup = null)
{
    /// <summary>What it carries in place of its class's loadout (mines, fighters, shuttles, barrage missiles), or null.</summary>
    public ShipKit? Kit { get; init; }
    /// <summary>Energy boxes it starts with in place of its class's, by system (data/battle/classes.json power_e), or null.</summary>
    public IReadOnlyDictionary<string, int>? PowerE { get; init; }
    /// <summary>The health it starts with in a subsystem (Rules.Subsystems), 0 to 1, or null for all whole.</summary>
    public IReadOnlyDictionary<string, double>? SysHp { get; init; }
    /// <summary>Mounts the scene adds after its class's (weapon ids checked against data/battle/weapons.json), or null.</summary>
    public IReadOnlyList<MountDef>? ExtraMounts { get; init; }
    /// <summary>Seconds into the battle it arrives (warps in at its place), or 0 from the start (Battle.Field).</summary>
    public double ArriveS { get; init; }
    /// <summary>Seconds it lies dormant (a sleeping giant: no orders, no fire, shields down) unless hit first, or 0 awake.</summary>
    public double WakeS { get; init; }
    /// <summary>A stationary installation (a station, yard, battery): it never moves.</summary>
    public bool Fixed { get; init; }
    /// <summary>A runner (an enemy scout making for home): it holds its course at full speed and never turns to fight.</summary>
    public bool Runs { get; init; }
}

/// <summary>A ship's craft and mines in a scenario, each null for its class's loadout; the fighters' first mission.</summary>
public sealed record ShipKit(int? Mines, int? Fighters, int? Shuttles, int? Missiles, string? Mission);

/// <summary>An asteroid: where it is and its radius (metres), and its name in the log (null: "an asteroid").</summary>
public sealed record RockDef(double XM, double ZM, double RadiusM, string? Name);

/// <summary>A scenario: who plays it (Side, the player's faction, which picks the console skin), the seeded battle and its
/// objective, and the first officer's message. The first ship is the one the player commands. Sky is the nebula it is fought
/// under; SunEnergy the sun's strength there (deep space is dim and moody; a scene by a warm star turns it up); Wells, the
/// gravity wells whose field a warp jump must reckon with (ScenarioStart), or null.</summary>
public sealed record Scenario(string Id, string Name, string Side, ulong Seed, string Objective, string SayWho, string SayText,
    IReadOnlyList<ShipStart> Ships, string Sky, double SunEnergy = Scenario.DeepSpaceSun, bool DamageRange = false,
    IReadOnlyList<MarkerDef>? Markers = null, IReadOnlyList<WellDef>? Wells = null, NebulaDef? Nebula = null, bool Repel = false)
{
    /// <summary>False for a test scene whose own ship carries no weapons (the mockup's armed: false: the scene's tool is
    /// what destroys the target).</summary>
    public bool Armed { get; init; } = true;
    /// <summary>Whether the captain may withdraw (warp out) from this battle: false for a story or special mission.</summary>
    public bool CanWithdraw { get; init; } = true;
    /// <summary>The tools decisive in a test scene ("mine", "rock", "shuttle", "barrage"): one destroys its target.</summary>
    public IReadOnlyList<string> Lethal { get; init; } = System.Array.Empty<string>();
    /// <summary>The asteroids of the scene.</summary>
    public IReadOnlyList<RockDef> Rocks { get; init; } = System.Array.Empty<RockDef>();
    /// <summary>A test scene's planets (data/battle/scenes/comms.json), or null.</summary>
    public IReadOnlyList<PlanetDef>? Planets { get; init; }
    /// <summary>A test scene's goal, met to win whatever enemies are left (Scenes.Comms), or null.</summary>
    public ScenarioGoal? Goal { get; init; }
    /// <summary>A battle with marker beacons ends when its mission's objectives are met (GameFlow checks them), not when
    /// the last enemy goes: a shakedown's target drones can be shot before its beacon is reached.</summary>
    public bool EndsOnObjectives => Markers is { Count: > 0 } || ObjectiveEnd;
    /// <summary>A mission battle whose objectives are not all about destroying ships (a scan, a survey, an away team, a
    /// delivery): it ends when its primary objectives are met, as one with markers does.</summary>
    public bool ObjectiveEnd { get; init; }
    /// <summary>Seconds the battle lasts at most (a holding action, a scout's run home), or 0 for no limit: at the limit it ends
    /// with Battle.TimeUp set.</summary>
    public double TimeLimitS { get; init; }
    /// <summary>A world condition that cuts every ship's engines for ForS seconds in every EveryS (Dead engines), or null.</summary>
    public EngineOutage? DeadEngines { get; init; }
    /// <summary>The sun's strength in deep space, where most battles are (owner, 2026-11-04: "tune down sun brightness by 50%
    /// most of the game is set in deep space where lighting is more moody, if there is a warmer planet, then we can crank up
    /// solar strength"): half the 1.3 the view first used.</summary>
    public const double DeepSpaceSun = 0.65;
}

/// <summary>Every ship's engines cut for ForS seconds in every EveryS, first at EveryS (Battle.Field).</summary>
public sealed record EngineOutage(double EveryS, double ForS);

/// <summary>A marker beacon in a mission battle: where it is (metres), how near counts as reached, and the marker that must
/// be reached before it shows (null: from the start).</summary>
public sealed record MarkerDef(string Id, string Label, double XM, double ZM, double RadiusM, string? After);

/// <summary>One program of the damage range (data/battle/range.json): Weapons fired in turn, one every EveryS seconds, Count
/// at most, from BearingDeg off the target's bow (null: a seeded random bearing each shot), overloaded or not; it stops once
/// the hull is below StopBelowHull of the class's (0: never) or the ship is destroyed.</summary>
public sealed record RangeProgram(string Id, string Name, IReadOnlyList<string> Weapons, bool Overload, double EveryS, int Count,
    double? BearingDeg, double StopBelowHull);

/// <summary>The damage range: how far out its shots are fired from, and its programs in menu order.</summary>
public sealed record RangeDef(double FromKm, IReadOnlyList<RangeProgram> Programs);

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
    double OrbitKm, double HoldMinKm, double HoldMaxKm, double CaptureHullFrac)
{
    /// <summary>How many times its length a ship is framed in the battle: drawn that size and fought at it (mount positions,
    /// ramming contact, the hull a rock strikes), as the reference games draw their ships large against the space between
    /// them (owner, 2026-11-06: "multiply all distance by 10", "1km -> 10km": every distance grew tenfold, the ships did not).</summary>
    public double ShipFrameX { get; init; } = 1;
}

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
    SpecialWeapons Specials, RangeDef Range)
{
    /// <summary>The scenario the game starts in (the first).</summary>
    public Scenario StartScenario => Scenarios[0];

    /// <summary>The numbers of the mockup's other systems (data/battle/systems/*.json).</summary>
    public SystemsDef Systems { get; init; } = new();

    /// <summary>A scenario by id; throws naming the id when there is none.</summary>
    public Scenario ScenarioById(string id) =>
        Scenarios.FirstOrDefault(s => s.Id == id) ?? throw new KeyNotFoundException($"no scenario '{id}' in data/battle/scenarios.json");
}
