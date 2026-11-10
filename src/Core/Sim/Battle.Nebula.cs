// Battle.Nebula: ion storms (openspec/changes/nebula-plumes; owner, 2026-11-10: "some nebulas are ion storms, which will
// have cool lightning", "which can strike ships, and cause a temprorary shut down of the reactor core, the ship loosing
// power for a few seconds completely"). A plume a scenario marks storm throws lightning at the ships inside it: each is
// struck with the chance Rules.StormStrikePerS gives where it is. A strike does no damage; it trips the ship's core for
// the storm's shutdown time, and while a core is down the ship has no power at all:
// - SystemsStep charges nothing (battery, capacitor, mounts with their own generators too) and its brownout is 0, so its
//   drive, warp, tractor and repair crews stop with it (each already scales by Ship.Brown);
// - its shields neither hold nor regenerate (Ship.ShieldsHold), it cannot fire, its point defence is silent
//   (Ship.HoldsFire, Ship.CoreDown), and a cloak that was on drops.
// The plume's other rules (half accuracy, half energy range, lost missiles, hidden ships) are Rules.Cloak.cs and
// Battle.Missiles.cs. Rolls come from the nebula's own fork and are drawn only for ships inside a storm, so a battle with
// no storm replays as it did (CLAUDE.md 6.3). The bolts are kept three seconds for the view to draw the bolt.
using System;
using System.Collections.Generic;
using FleetCommand.Data;

namespace FleetCommand.Sim;

/// <summary>Lightning that found a ship: which, where it was (metres) and when (battle time, s).</summary>
public sealed record LightningHit(string ShipId, double XM, double ZM, double T);

public sealed partial class Battle
{
    private readonly List<LightningHit> _bolts = new();
    /// <summary>The last three seconds' lightning that found a ship, oldest first.</summary>
    public IReadOnlyList<LightningHit> Lightning => _bolts;

    partial void NebulaStep(double dt)
    {
        _bolts.RemoveAll(k => T - k.T >= 3);
        foreach (Ship s in _ships)
        {
            if (!s.Alive || s.Absent) continue;
            bool was = s.CoreDown;
            s.CoreDownS = Math.Max(0, s.CoreDownS - dt);
            s.StormGraceS = Math.Max(0, s.StormGraceS - dt);
            if (was && !s.CoreDown && s == Player) Say("Engineering", "Core restarted. Power is back.");
            if (s.StormGraceS > 0) continue;
            double perS = Rules.StormStrikePerS(s.X, s.Z);
            if (perS > 0 && _nebRng.Next() < perS * dt) LightningStrike(s);
        }
    }

    /// <summary>Lightning finds a ship: its core trips for the storm's shutdown time, and a cloak that was on drops.</summary>
    public void LightningStrike(Ship s)
    {
        NebulaRules n = Cs.Nebula;
        s.CoreDownS = n.StormShutdownS;
        s.StormGraceS = n.StormShutdownS + n.StormGraceS;
        s.Brown = 0;
        if (s.CloakOn) SetCloak(s, false);
        _bolts.Add(new LightningHit(s.Id, s.X, s.Z, T));
        if (s == Player) Say("Engineering", $"Lightning strike! The core has tripped: no power for {n.StormShutdownS:0} seconds.");
        else if (s.Side == Player.Side) Say("Comms", $"Lightning has hit {s.Name}: its core has tripped.");
        else if (Detected(Player, s)) Say("Tactical", $"Lightning has hit {s.Name}: its core is down.");
    }
}
