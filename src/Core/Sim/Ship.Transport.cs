// Ship.Transport: what a ship carries for comms, transport and boarding (the battle console mockup's makeShip fields
// marines, team, guard, vip, captured, pads, bq, raid, party, hold, needs, got and the AI's capture order): its combat
// marines and specialists aboard, the transporter pads, the boarding queues, a raid or a boarding party it has out, its
// hold and what it asks to be given. Plain state, set up and changed only by Battle.Transport; its own file so the
// system packages never edit the same lines of Ship.
using System;
using System.Collections.Generic;

namespace FleetCommand.Sim;

/// <summary>A holder of cargo: a ship or a planet (one rule for what either still needs, CLAUDE.md 6.1).</summary>
public interface ICargoHolder
{
    string Id { get; }
    string Name { get; }
    /// <summary>Units of each item aboard (CommsDef.Items).</summary>
    Dictionary<string, int> Hold { get; }
    /// <summary>What it asks to be given, or null when it asks for nothing.</summary>
    Dictionary<string, int>? Needs { get; }
    /// <summary>What it has been given against its needs.</summary>
    Dictionary<string, int> Got { get; }
}

/// <summary>A planet in the battle (a scene's PlanetDef): where it is, its radius, its colour and what it asks for.</summary>
public sealed class Planet : ICargoHolder
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required double X { get; init; }
    public required double Z { get; init; }
    public required double RadiusM { get; init; }
    public required string Color { get; init; }
    public Dictionary<string, int> Hold { get; } = new();
    public Dictionary<string, int>? Needs { get; init; }
    public Dictionary<string, int> Got { get; } = new();
    /// <summary>True once every need was met (said once).</summary>
    public bool Relieved;
}

/// <summary>A hit and run raid out: the ship it is on, the subsystem (or one mount) it aims at, shoot or bomb, the squad
/// (combat marines and demolition marines) and the seconds left until it is done.</summary>
public sealed class Raid
{
    public required string On { get; init; }
    public required string Sys { get; init; }
    public int? Mount { get; init; }
    public required string Kind { get; init; }
    public readonly Dictionary<string, int> Team = new() { ["marines"] = 0, ["demo"] = 0 };
    public double T;
}

/// <summary>A boarding party aboard a ship: who is in it (every role and the envoy, "vip"), each marine's health, the
/// seconds to the next volley, whether its held deck was said, whether its log is spoken (the player's, or an AI party's
/// the player should hear), and the last volley.</summary>
public sealed class Party
{
    public required string On { get; init; }
    public readonly Dictionary<string, int> Team = new();
    public readonly List<double> Hp = new();
    public double VolleyT;
    public bool Held, Loud;
    /// <summary>The envoy's name while the envoy is in the party.</summary>
    public string? VipName;
    public Volley? Last;
    public int Of(string role) => Team.GetValueOrDefault(role);
}

/// <summary>One volley aboard: hits our marines landed, hits the defenders landed, guards locked out, a defender talked down.</summary>
public sealed record Volley(int Att, int Def, int Locked, int Talked);

public sealed partial class Ship : ICargoHolder
{
    /// <summary>Combat marines aboard (the boarding parties and the defenders).</summary>
    public int Marines;
    /// <summary>The specialists aboard by role (demo, medics, engineers, hackers, pilots).</summary>
    public readonly Dictionary<string, int> Team = new();
    /// <summary>Armed crew who defend the ship beside its marines.</summary>
    public int Guard;
    /// <summary>The envoy aboard, by name, or null.</summary>
    public string? Vip;
    /// <summary>Taken by a boarding party: it changed sides.</summary>
    public bool Captured;
    /// <summary>A prize waiting for a pilot (said when one arrives).</summary>
    public bool AwaitPilot;
    /// <summary>Each transporter pad's charge, 0 to 1; 1 is ready.</summary>
    public double[] Pads = Array.Empty<double>();
    /// <summary>The boarding queues by mode, "capture" and "hit", head first.</summary>
    public readonly Dictionary<string, List<string>> Queue = new() { ["capture"] = new(), ["hit"] = new() };
    public Raid? Raid;
    public Party? Party;
    /// <summary>What the next raid aims at: a subsystem, one mount of it (weapons only), and shoot or bomb.</summary>
    public string RaidSys = "weapons";
    public int? RaidMount;
    public string RaidKind = "shoot";
    public Dictionary<string, int> Hold { get; } = new();
    /// <summary>The hold's size in units.</summary>
    public int HoldCap;
    public Dictionary<string, int>? Needs { get; set; }
    public Dictionary<string, int> Got { get; } = new();
    /// <summary>True once every need was met (said once).</summary>
    public bool Relieved;
    /// <summary>Adrift with failed engines, calling for help; under way again once relieved.</summary>
    public bool Distress;
    /// <summary>An AI captain's order to capture this ship, once it has the envoy or NeedMarines marines; Told once it says so.</summary>
    public string? CaptureOrder;
    public int NeedMarines;
    public bool Told;
}
