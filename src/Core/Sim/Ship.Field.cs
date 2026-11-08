// Ship.Field: a ship's share of field operations (Battle.Field): a late arrival waiting off the field, a sleeping ship, and
// a fixed installation that never moves.
namespace FleetCommand.Sim;

public sealed partial class Ship
{
    /// <summary>Not yet on the field: it arrives at ArriveS and is unseen and untouchable until then.</summary>
    public bool Absent;
    public double ArriveS;
    /// <summary>Where it warps in.</summary>
    public double ArriveX, ArriveZ;
    /// <summary>Asleep until WakeS (or hit): no orders, no fire.</summary>
    public bool Dormant;
    public double WakeS;
    /// <summary>A station, yard or battery: it never moves.</summary>
    public bool Fixed;
    /// <summary>A runner: holds its course at full speed, no orders, no fire.</summary>
    public bool Runs;
    /// <summary>Under a truce until the player's side fires first.</summary>
    public bool Truce;
    /// <summary>Battle time its fire last landed (AI captains close in when theirs has not landed for a while).</summary>
    public double LastLandedT;
    /// <summary>What it is asked to do once it arrives or wakes.</summary>
    public Stance WakeStance;
    public bool WakeHoldFire;
    /// <summary>The hull it had when it fell asleep: hit, it wakes.</summary>
    public double DormantHull;
}
