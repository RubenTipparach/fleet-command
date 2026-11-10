// Ship.Nebula: what an ion storm does to a ship (openspec/changes/nebula-plumes; owner, 2026-11-10: lightning "can
// strike ships, and cause a temprorary shut down of the reactor core, the ship loosing power for a few seconds
// completely"). Plain state owned by Battle; the rule that sets it is Battle.Nebula.cs, and everything that needs power
// asks CoreDown (CLAUDE.md 6.1).
namespace FleetCommand.Sim;

public sealed partial class Ship
{
    /// <summary>Seconds until a core tripped by lightning restarts; 0 while it runs.</summary>
    public double CoreDownS;
    /// <summary>Seconds until lightning can find this ship again (the trip, then the storm's grace).</summary>
    public double StormGraceS;

    /// <summary>The core is tripped: no power at all. No drive, no charging, no fire, no shields, no cloak.</summary>
    public bool CoreDown => CoreDownS > 0;
    /// <summary>The shields hold and regenerate: raised, not under a cloak, and the core running.</summary>
    public bool ShieldsHold => ShieldsUp && CloakK <= 0 && !CoreDown;
    /// <summary>The ship may not fire at all: cloaking, or its core tripped.</summary>
    public bool HoldsFire => Cloaking || CoreDown;
}
