// Battle.Crew: what co-op (openspec/changes/bridge-crew-coop) needs of the battle itself, kept in its own part so single
// play is untouched (owner, 2026-11-11: "certain roles like science/sensors, tractor beam, transporters, weapons
// targetting, and navigation would have acces to independent targetting", "if you have more than one ship, you can asing
// whole ships to other players", "other players can be captains of their own ship, the main captain is technically
// admiral of the fleet").
//
// - Independent targets: a ship crewed by players keeps a target per targeting role (RoleTargets: helm, weapons, science,
//   tractor, transport); each system reads its own role's (TargetOf(s, role)). A ship with no RoleTargets (every ship in
//   single play, and every AI ship) has the one Target it always had, which every role reads, so nothing changes there.
// - The ship's main Target is the captain's: what the fleet, comms, the cloak and the AI's "Mine" rule follow.
// - Ships commanded by players: Player is the ship the order being applied acts on. Other players' ships (Helmed) are left
//   alone by the AI, steer to their helm's orders and fire automatically the same as Player's. The lockstep (Coop.Lockstep)
//   sets Player around every order to the ship its sender commands, and back to the admiral's around every step, the same
//   on every peer, so the battle stays one battle.
using System;
using System.Collections.Generic;
using System.Linq;

namespace FleetCommand.Sim;

public sealed partial class Ship
{
    /// <summary>The roles that keep their own target on a crewed ship (owner, 2026-11-11).</summary>
    public static readonly string[] TargetRoles = { "helm", "weapons", "science", "tractor", "transport" };

    /// <summary>A crewed ship's target per targeting role (role id to ship id, or null for none); null on every ship no
    /// player crew is on, where the one <see cref="Target"/> serves every role.</summary>
    public SortedDictionary<string, string?>? RoleTargets;
}

public sealed partial class Battle
{
    /// <summary>The ships players command besides the admiral's (Player between steps), by id, in a stable order.</summary>
    public readonly SortedSet<string> Helmed = new(StringComparer.Ordinal);

    /// <summary>The targeting roles the order being applied acts for: null in single play (a lock sets the ship's one
    /// target), else the roles its sender's lock moves (Coop.Crew.LockRoles).</summary>
    public IReadOnlyList<string>? OrderRoles { get; private set; }

    /// <summary>Whether the order being applied comes from the ship's captain (who also moves the ship's main target).</summary>
    public bool OrderFromCaptain { get; private set; } = true;

    /// <summary>A ship's target for one role: its own role target on a crewed ship, else its one target.</summary>
    public Ship? TargetOf(Ship s, string role)
    {
        if (s.RoleTargets != null && s.RoleTargets.TryGetValue(role, out string? id)) { Ship? t = ShipById(id); return t is { Alive: true } ? t : null; }
        return TargetOf(s);
    }

    /// <summary>The id a ship has locked for one role, alive or not (its role target on a crewed ship, else its one target).</summary>
    public static string? RoleTargetId(Ship s, string role) =>
        s.RoleTargets != null && s.RoleTargets.TryGetValue(role, out string? id) ? id : s.Target;

    /// <summary>Gives a ship a target per role (each starting on its one target): a ship with a player crew.</summary>
    public void CrewShip(string id)
    {
        if (ShipById(id) is not { } s || s.RoleTargets != null) return;
        s.RoleTargets = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        foreach (string r in Ship.TargetRoles) s.RoleTargets[r] = s.Target;
    }

    /// <summary>Marks a ship as commanded by a player (not the AI), or hands it back to the AI.</summary>
    public void SetHelmed(string id, bool on)
    {
        if (ShipById(id) is not { Side: 0 } s) return;
        if (on) { Helmed.Add(id); CrewShip(id); s.Engaged = false; }
        else Helmed.Remove(id);
    }

    /// <summary>Runs one order as a sender: on the ship they command, for the targeting roles their lock moves, captain or
    /// not. Returns the ship that sender commands afterwards (Take command may have moved them).</summary>
    public Ship ApplyAs(Ship ship, IReadOnlyList<string>? roles, bool captain, Action order)
    {
        Ship admiral = Player;
        bool isAdmiral = ship == admiral;
        Player = ship;
        OrderRoles = roles;
        OrderFromCaptain = captain;
        try { order(); }
        finally { OrderRoles = null; OrderFromCaptain = true; }
        Ship now = Player;
        if (!isAdmiral)
        {
            // a ship's captain who took command of another ship moves to it; the admiral's ship stays the admiral's
            if (now != ship) { Helmed.Remove(ship.Id); SetHelmed(now.Id, true); }
            Player = admiral;
        }
        return now;
    }

    /// <summary>Shows the battle as one ship's captain sees it (the console and view read Player): the lockstep puts the
    /// admiral's ship back before every order and step.</summary>
    public void ViewAs(Ship ship) => Player = ship;

    /// <summary>The ships the players command now: the admiral's first, then the others by id.</summary>
    public IEnumerable<Ship> Commanded() => new[] { Player }.Concat(Helmed.Select(ShipById).Where(s => s is { Alive: true } && s != Player)!)!;

    // the targets a lock moves: the sender's roles on a crewed ship (and the main target if the captain sent it), else the
    // ship's one target
    private void SetLock(Ship s, string? id)
    {
        if (s.RoleTargets == null || OrderRoles == null) { s.Target = id; return; }
        foreach (string r in OrderRoles) if (s.RoleTargets.ContainsKey(r)) s.RoleTargets[r] = id;
        if (OrderFromCaptain) s.Target = id;
    }

    // the target a lock order starts from: the first of the sender's roles on a crewed ship, else the one target
    private string? LockedNow(Ship s) =>
        s.RoleTargets != null && OrderRoles is { Count: > 0 } r && s.RoleTargets.TryGetValue(r[0], out string? id) ? id : s.Target;

    // every lock on a ship dropped (it was destroyed, struck, jumped, cloaked or taken): its main target and its role targets
    private void DropLocksOn(string id, Func<Ship, bool>? who = null)
    {
        foreach (Ship o in _ships)
        {
            if (who != null && !who(o)) continue;
            if (o.Target == id) o.Target = null;
            if (o.RoleTargets != null) foreach (string r in o.RoleTargets.Keys.ToList()) if (o.RoleTargets[r] == id) o.RoleTargets[r] = null;
        }
    }
}
