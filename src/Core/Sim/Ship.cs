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
    /// <summary>Multiple fire: Fire looses every selected ready mount; off, one at a time.</summary>
    public bool Volley = true;
    public int Ecm;
    public Stance Stance = Stance.Attack;
    public double ThinkT;
}
