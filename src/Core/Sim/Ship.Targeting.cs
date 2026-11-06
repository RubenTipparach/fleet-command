// Ship.Targeting: a ship's state for weapon targeting, point defence and the fleet (openspec/changes/weapon-targeting; the
// battle console mockup's aim, pd, intercepts, pin and ai settings), in its own part of Ship and Mount so the other system
// packages never edit these lines. Plain data: the rules that read and change it are Strike, Rules and Battle.Targeting.cs
// and Battle.Fleet.cs.
namespace FleetCommand.Sim;

/// <summary>Point defence's mode (the Point defence station): Auto fires on anything inbound, Missiles on missiles only,
/// Craft is kept for craft, Off holds fire.</summary>
public enum PdMode { Auto, Missiles, Craft, Off }

/// <summary>Which enemy an AI captain fights: its fleet captain's target (Mine), the nearest, or the weakest by hull.</summary>
public enum TargetRule { Mine, Nearest, Weakest }

/// <summary>When an AI captain breaks off and runs: never, below half its hull, or below a quarter
/// (tactical.json retreat_below_hull_frac).</summary>
public enum RetreatAt { Never, Half, Quarter }

/// <summary>A mount pinned to a ship (pinned fire): Fire looses it at that ship with that aim whatever is locked, until the
/// ship or the aimed subsystem is destroyed.</summary>
public sealed record Pin(string To, string Aim);

public sealed partial class Ship
{
    /// <summary>What this ship's captain aims at (Strike): "hull", a subsystem, or "w:" and a target's mount index.</summary>
    public string Aim = "hull";
    /// <summary>Point defence's mode, and how many shots it has stopped this battle.</summary>
    public PdMode Pd = PdMode.Auto;
    public int Intercepts;
    private TargetRule? _rule;
    /// <summary>An AI captain's target rule: unless set, its fleet captain's target on side 0 and the nearest enemy on side 1.</summary>
    public TargetRule Rule { get => _rule ?? (Side == 0 ? TargetRule.Mine : TargetRule.Nearest); set => _rule = value; }
    /// <summary>When an AI captain breaks off.</summary>
    public RetreatAt Retreat = RetreatAt.Never;
}

public sealed partial class Mount
{
    /// <summary>The ship and aim this mount is pinned to, or null.</summary>
    public Pin? Pin { get; set; }
}
