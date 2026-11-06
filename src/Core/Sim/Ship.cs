// Ship and Mount: the state of one ship in the battle. Plain data owned by Battle; the rules that read and change it
// are in BattleRules (what things are worth) and Battle (what happens each step and on each order).
using System.Collections.Generic;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public enum Arming { Offline, Normal, Overload }

/// <summary>The five statuses of a system or mount (battle-console design, "The five statuses").</summary>
public enum SystemStatus { Active, Damaged, Disabled, Off, Destroyed }

public enum AlertLevel { Green, Yellow, Red }

/// <summary>An AI captain's standing orders (survey G3); the Fleet station will set these for wingmen.</summary>
public enum Stance { Attack, Escort, Hold }

public sealed class Mount
{
    public required int Index { get; init; }
    public required MountDef Def { get; init; }
    public required WeaponDef Weapon { get; init; }
    public Arming State { get; set; } = Arming.Normal;
    /// <summary>0 to 1; 1 is ready to fire.</summary>
    public double Charge { get; set; }
    public bool Selected { get; set; }
    /// <summary>Health, 0 (destroyed) to 1.</summary>
    public double Hp { get; set; } = 1;
    /// <summary>Seconds it stays stunned by a quell torpedo: it neither charges nor fires.</summary>
    public double StunS { get; set; }
    /// <summary>The mount's id as the console shows it: W and its place in the ship's list.</summary>
    public string Label => $"W{Index + 1}";
}

public sealed class Ship
{
    public required string Id { get; init; }
    /// <summary>The ship's place in the scenario: the stable order every step iterates in (CLAUDE.md 6.3).</summary>
    public required int Order { get; init; }
    public required string Name { get; init; }
    public required string Captain { get; init; }
    public required ShipClass Class { get; init; }
    public required Faction Faction { get; init; }
    public required int Side { get; init; }

    public double X, Z, H, HOrder, V, VOrder;
    public double Hull;
    public double[] Shields = new double[6];
    public double[] Flash = new double[6];
    public bool ShieldsUp = true;
    /// <summary>The reinforced facing, or -1.</summary>
    public int Reinforce = -1;
    /// <summary>Energy boxes given to each system (BattleRules.Systems), whole points.</summary>
    public readonly Dictionary<string, int> Power = new();
    public double Capacitor, Battery;
    /// <summary>The share of its allocation each system gets when the battery is empty and the draw is past the output.</summary>
    public double Brown = 1;
    public readonly List<Mount> Mounts = new();
    /// <summary>Health of each subsystem (BattleRules.Subsystems), 0 to 1.</summary>
    public readonly Dictionary<string, double> Sys = new();
    public string? Target;
    public AlertLevel Alert = AlertLevel.Yellow;
    public bool Alive = true;
    /// <summary>A prize: a ship a mission wants taken, not destroyed (ShipStart.Prize).</summary>
    public bool Prize;
    /// <summary>A prize beaten down to the capture line has struck: boarded and taken, it no longer fights or moves, and the
    /// battle no longer counts it as an enemy. It can still be destroyed by a careless shot.</summary>
    public bool Struck;
    /// <summary>Multiple fire: Fire looses every selected ready mount; off, one at a time.</summary>
    public bool Volley = true;
    public int Ecm;
    public Stance Stance = Stance.Attack;
    public double ThinkT;
    /// <summary>Countermeasures: battle time its flares can be thrown again (s), decoy shuttles left, and the decoy it has out.</summary>
    public double FlareReadyT;
    /// <summary>A forge burn: seconds left, damage a second and where it struck from (m).</summary>
    public double BurnS, BurnDps, BurnFromX, BurnFromZ, BurnTick;
    /// <summary>A blood charge: seconds of surge left, its speed, thrust and turn factors (1 when not surging), and the
    /// battle time it can be ordered again (s).</summary>
    public double SurgeS, SurgeSpeedX = 1, SurgeAccelX = 1, SurgeTurnX = 1, ChargeReadyT;
    public int DecoysLeft;
    public Lure? Decoy;

    // The helm officer (battle-console design: helm orders, high energy turns, evasive, emergency stop).
    /// <summary>True while an order toward the target steers the ship (the autopilot); false is free heading.</summary>
    public bool Engaged;
    /// <summary>intercept, trail, hold_range, orbit or hit_run.</summary>
    public string Manoeuvre = "intercept";
    /// <summary>1 clockwise, -1 anticlockwise.</summary>
    public int OrbitDir = 1;
    public double HoldKm = 6, RunMinKm = 2, RunMaxKm = 8;
    /// <summary>Hit and run: "in" closing to RunMinKm, "out" opening to RunMaxKm.</summary>
    public string RunPhase = "in";
    public bool Erratic;
    /// <summary>An AI captain told to hold its fire (a scenario's standing order): it manoeuvres but never shoots.</summary>
    public bool HoldFire;
    /// <summary>The range an AI captain fights at.</summary>
    public Data.AiRange Range = Data.AiRange.Mid;
    /// <summary>Emergency deceleration under way; then the engines hold for EstopT seconds.</summary>
    public bool Estop;
    public double EstopT;
    /// <summary>The turn still to make in a high energy turn, radians; null when none.</summary>
    public double? Het;
    /// <summary>The chance the next high energy turn succeeds.</summary>
    public double HetChance;
    /// <summary>Seconds left dead in space after a failed high energy turn.</summary>
    public double Broke;
}
