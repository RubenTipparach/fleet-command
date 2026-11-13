// Ship.Reactor: a ship's state for the reactor, the warp drive, damage control and its officers (the battle console
// mockup's s.reactor, s.warp, s.repairQ and s.officers). Plain data, set up by Battle.Reactor from the reactor file
// (data/battle/systems/reactor.json) and the scenario, changed only by Battle's steps and orders; what the numbers are
// worth is in Rules.Reactor. The reactor's magnetic confinement is the reactor subsystem's health, Sys["reactor"], so a
// hit on the reactor, a repair and the systems display all read the one number (CLAUDE.md 6.1).
using System.Collections.Generic;

namespace FleetCommand.Sim;

/// <summary>A coolant loop: its pump's flow (whole points, each drawing flow_e of the reactor) and its health, 0 to 1.</summary>
public sealed class CoolantLoop
{
    public int Flow;
    public double Hp = 1;
}

/// <summary>The reactor: each coil's offset from alignment (-1 to 1, 0 aligned), the coolant loops, the core temperature
/// (percent of its limit), seconds counted toward a meltdown, whether the crew has warned of it, and the coolant reserve
/// (0 to 1) that loops leak and jumps spend.</summary>
public sealed class ReactorState
{
    public double[] Coils = System.Array.Empty<double>();
    public CoolantLoop[] Loops = System.Array.Empty<CoolantLoop>();
    public double Temp, Melt, Coolant = 1;
    public bool Warned;
    /// <summary>Seconds until an AI crew next tends the reactor.</summary>
    public double TendT;
}

/// <summary>The warp drive: armed (the coils take free energy), the energy the coils want (E), the banks (E), the course
/// plot (0 to 1) and whether one is being plotted, the safety override, each nacelle's health, and the seconds of a jump
/// still to run (0 when none).</summary>
public sealed class WarpState
{
    public bool Armed, Plotting, Override;
    public int Want;
    public double Banks, Plot, Out;
    public double[] Nacelles = System.Array.Empty<double>();
}

/// <summary>An officer aboard a ship: their id (ship.roster id), name, rank (0-3), specialty (a station) and skill (1-4).</summary>
public sealed record StationOfficer(string Id, string Ship, string Name, int Rank, string Spec, int Skill)
{
    /// <summary>A campaign officer's record (change crew-leveling), for the hover card; null for the roster's officers.</summary>
    public OfficerCard? Card { get; init; }
}

/// <summary>A campaign officer as the battle shows them (Battle.SeatCrew; Campaign.CrewSeats): their campaign station, rank
/// and level, experience banked and needed for the next level (null at the highest), and the skills they have learned.</summary>
public sealed record OfficerCard(string Station, string Rank, int Level, int Xp, int? NextXp, IReadOnlyList<OfficerCardSkill> Skills);

/// <summary>A learned skill on a crew card: its name and what it does.</summary>
public sealed record OfficerCardSkill(string Name, string Text);

/// <summary>A campaign officer to seat on the flagship: the battle stations they take, their name, rank index and card.</summary>
public sealed record CrewSeat(IReadOnlyList<string> Stations, string Name, int Rank, OfficerCard Card);

public sealed partial class Ship
{
    /// <summary>What the campaign crew's learned skills add to this ship's abilities, by the ability's key (data/campaign/crew.json
    /// fx; Campaign.CrewBonus, set on the flagship by Battle.ManCrew). Each rule adds its own ability's share; empty on every
    /// other ship and in a skirmish.</summary>
    public readonly Dictionary<string, double> SkillBonus = new();
    /// <summary>The crew's skills' total for one ability (0 when none).</summary>
    public double Skill(string ability) => SkillBonus.GetValueOrDefault(ability);

    public readonly ReactorState Reactor = new();
    public readonly WarpState Warp = new();
    /// <summary>The repair queue, job ids in the order the crews take them.</summary>
    public readonly List<string> RepairQueue = new();
    /// <summary>The officer in each station's slot (null: vacant); empty for a ship without officers (every AI enemy).</summary>
    public readonly Dictionary<string, StationOfficer?> Officers = new();
    /// <summary>This ship's officers, the ones in slots and the reserve, in roster order.</summary>
    public readonly List<StationOfficer> Crew = new();
    /// <summary>The captain lets the allocation draw past the reactor's output (Power, Reactor: "Draw past output").</summary>
    public bool Overdraw;
    /// <summary>The ship has jumped to warp: gone from the battle, not destroyed.</summary>
    public bool Jumped;
}
