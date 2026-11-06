// Ship.Tractor: a ship's state for the tractor beam, mines, asteroids and craft (the tractor-and-craft package), plain
// data owned by Battle and changed only by Battle.Tractor, Battle.Mines and Battle.Craft. It sits in its own part of Ship
// so the packages never edit the same lines.
namespace FleetCommand.Sim;

/// <summary>What a tractor beam is doing: off, powering up after Hold, or powered (holding, or waiting for its target to
/// come inside reach).</summary>
public enum TractorMode { Off, Power, Hold }

/// <summary>A round's picks as they were revealed: the holder's value, the held ship's, and the grip it moved.</summary>
public sealed record TractorRound(double Holder, double Held, double Moved);

/// <summary>A tractor beam holding a ship (the holder's): whom, the grip (0 to 1), the round under way and the time left in
/// it, the last round's picks, and the tether's length and the length asked for (metres).</summary>
public sealed class TractorBeam
{
    public required string On { get; init; }
    public double Grip, RoundT, LenM, WantM;
    public int Round = 1;
    public TractorRound? Last;
}

public sealed partial class Ship
{
    /// <summary>The tractor's mode, and the power-up's seconds left.</summary>
    public TractorMode Tractor = TractorMode.Off;
    public double PowerT;
    /// <summary>The beam this ship holds, or null.</summary>
    public TractorBeam? Beam;
    /// <summary>A ship that broke free of this one, which cannot take it again until RelockT runs out (s).</summary>
    public string? RelockOn;
    public double RelockT;
    /// <summary>The contest round an AI crew last picked its beam value in (0: none).</summary>
    public int PickRound;

    /// <summary>Mines aboard, and the seconds until the next can be laid.</summary>
    public int Mines;
    public double MineCool;

    /// <summary>Fighters in the bay, disguised shuttles and barrage missiles aboard, and the fighters' mission.</summary>
    public int Fighters, Shuttles, Missiles;
    public string Mission = "attack";
    /// <summary>A barrage under way: missiles still to fire, and the seconds to the next.</summary>
    public int Barrage;
    public double BarrageT;

    /// <summary>Asteroids: the seconds until another collision counts, the grit of the micrometeor cloud gathered toward
    /// a whole point, whether it is in a cloud, and where it was at the end of the last step (m), for the speed of a tow.</summary>
    public double BumpT, Grit, PrevX, PrevZ;
    public bool InCloud;
}
