// Ship.Security: a ship's marines and the security aboard it (boarding defence; the battle console mockup's s.marines,
// s.sec and ai.board). Plain state owned by Battle; Battle.Security.cs walks the teams and boarders, fights them, and
// sends a boarding captain's squads. Marines is the count of marines aboard, the one number the transporter's boarding
// parties draw on too when they come into the game (the comms package).
using System.Collections.Generic;

namespace FleetCommand.Sim;

/// <summary>A security team or a boarding squad aboard a ship: its marines, where it is on the deck plan (metres, the plan's
/// axes), the nodes it still has to walk, how long it has stood still, the room it is going to, how long it has worked at
/// its goal, and the fight it is in.</summary>
public sealed class Unit
{
    public required string Id { get; init; }
    public required int Side { get; init; }
    public int N;
    public int Node;
    public List<int> Path = new();
    public double X, Y, Z;
    public double Still;
    public string Goal = "";
    public double Sab;
    public Fight? Fight;
}

/// <summary>A team and a squad fighting aboard: the seconds to their next volley, and whether the team's ambush opens it.</summary>
public sealed class Fight
{
    public required Unit A { get; init; }
    public required Unit B { get; init; }
    public double T;
    public bool Ambush, Done;
}

/// <summary>The security aboard a ship: its teams, the boarders, the fights, and the next squad's and team's numbers.</summary>
public sealed class SecurityState
{
    public required Data.DeckPlan Plan { get; init; }
    public readonly List<Unit> Teams = new();
    public readonly List<Unit> Foes = new();
    public readonly List<Fight> Fights = new();
    public int NextSquad = 1, NextTeam = 1;
}

public sealed partial class Ship
{
    /// <summary>The marines aboard.</summary>
    public int Marines;
    /// <summary>The security aboard (teams, boarders, fights); made for the ship you command once it has a deck plan.</summary>
    public SecurityState? Sec;
    /// <summary>A boarding captain: sends squads aboard the ship it fights whenever it can (Battle.Security.cs).</summary>
    public bool AiBoard;
    /// <summary>Battle time the boarding captain last sent a squad, s.</summary>
    public double AiBoardT = double.NegativeInfinity;
}
