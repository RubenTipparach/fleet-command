// Crew: who is on the bridge in a co-op game and what each may touch (openspec/changes/bridge-crew-coop). The owner,
// 2026-11-11: "It is up to the captain to assign these roles", "by default - captain has access to all roles", "if you have
// more than one ship, you can asing whole ships to other players", "whoeever is designated as captain remains captain the
// whole game", "other players can be captains of their own ship, the main captain is technically admiral of the fleet".
//
// - Member 1 is the admiral: the host, captain of the flagship (the ship the battle's Player is between steps) for the
//   whole game. Every other member joins on the flagship holding no role, until a captain gives them some.
// - A bridge is keyed "flag" for the admiral's ship (so the admiral's crew moves with the admiral) or by the ship's id for a
//   ship the admiral has given a member, who is that ship's captain.
// - Every role on a bridge is held by one member; a role no one was given is the bridge's captain's. A captain opens every
//   tab of their bridge; a member opens their roles' tabs, the others are disabled.
// Plain data and rules, the same on every peer: changes reach it only through the lockstep's ticks (Lockstep).
using System;
using System.Collections.Generic;
using System.Linq;

namespace FleetCommand.Coop;

public sealed record CrewMember(int Id, string Name);

public sealed class Crew
{
    public const int AdmiralId = 1;
    public const string Flag = "flag";

    public Roles Roles { get; }
    public List<CrewMember> Members { get; } = new();
    /// <summary>Ships the admiral has given to members: ship id to its captain.</summary>
    public SortedDictionary<string, int> Ships { get; } = new(StringComparer.Ordinal);
    /// <summary>Roles given away on each bridge: (bridge, role) to the member holding it.</summary>
    public SortedDictionary<string, int> Held { get; } = new(StringComparer.Ordinal);

    public Crew(Roles roles, string admiralName)
    {
        Roles = roles;
        Members.Add(new CrewMember(AdmiralId, admiralName));
    }

    public CrewMember? Member(int id) => Members.FirstOrDefault(m => m.Id == id);
    public string NameOf(int id) => Member(id)?.Name ?? $"Player {id}";

    private static string Key(string bridge, string role) => bridge + "|" + role;

    /// <summary>The bridge a member stands on: the ship they captain, the bridge they hold a role on, else the flagship's.</summary>
    public string BridgeOf(int member)
    {
        foreach (var (ship, cap) in Ships) if (cap == member) return ship;
        foreach (var (k, m) in Held) if (m == member) return k[..k.IndexOf('|')];
        return Flag;
    }

    /// <summary>The captain of a bridge: the admiral's on the flagship, else the member the ship was given to.</summary>
    public int CaptainOf(string bridge) => bridge == Flag ? AdmiralId : Ships.GetValueOrDefault(bridge, AdmiralId);

    /// <summary>Who holds a role on a bridge: the member given it, else the bridge's captain.</summary>
    public int HolderOf(string bridge, string role) => Held.TryGetValue(Key(bridge, role), out int m) ? m : CaptainOf(bridge);

    /// <summary>Whether a member's console opens a role on their bridge: they hold it, or they are its captain.</summary>
    public bool Opens(int member, string role)
    {
        string b = BridgeOf(member);
        return CaptainOf(b) == member || HolderOf(b, role) == member;
    }

    /// <summary>Whether a member's console opens a tab ("st.helm" a station, "sh.tractor" a ship page). A tab no role names
    /// is the captain's.</summary>
    public bool OpensTab(int member, string tab)
    {
        string? role = Roles.RoleOfTab(tab);
        return role != null ? Opens(member, role) : CaptainOf(BridgeOf(member)) == member;
    }

    /// <summary>Why a member may not give an order, or null when they may.</summary>
    public string? Refuses(int member, string order)
    {
        if (Member(member) == null) return "not aboard";
        if (Roles.AdmiralOnly.Contains(order)) return member == AdmiralId ? null : "the admiral's order";
        if (Roles.Shared.Contains(order)) return null;
        string? role = Roles.RoleOfOrder(order);
        if (role == null) return CaptainOf(BridgeOf(member)) == member ? null : "the captain's order";
        return Opens(member, role) ? null : $"the {Roles[role].Name} station's order";
    }

    /// <summary>The targeting roles a member's lock moves: the targeting roles they hold; or, from the tab of a role they open
    /// but another holds (a captain stepping in), that role alone.</summary>
    public IReadOnlyList<string> LockRoles(int member, string? openTab)
    {
        string b = BridgeOf(member);
        if (openTab != null && Roles.RoleOfTab(openTab) is { } r && Roles[r].Targets && Opens(member, r) && HolderOf(b, r) != member)
            return new[] { r };
        return Roles.List.Where(x => x.Targets && HolderOf(b, x.Id) == member).Select(x => x.Id).ToArray();
    }

    /// <summary>Adds a member who joined (the next free id), or returns the one already of that id.</summary>
    public CrewMember Join(string name)
    {
        int id = Members.Max(m => m.Id) + 1;
        var m = new CrewMember(id, name.Length == 0 ? $"Player {id}" : name);
        Members.Add(m);
        return m;
    }

    /// <summary>A member left: their ship goes back to the AI and their roles back to their captain.</summary>
    public void Leave(int id)
    {
        if (id == AdmiralId) return;
        Members.RemoveAll(m => m.Id == id);
        foreach (string s in Ships.Where(kv => kv.Value == id).Select(kv => kv.Key).ToList()) Ships.Remove(s);
        foreach (string k in Held.Where(kv => kv.Value == id).Select(kv => kv.Key).ToList()) Held.Remove(k);
    }

    /// <summary>Why the sender may not give this role to this member, or null (and the role is given). Giving a role to the
    /// bridge's captain takes it back. Only the bridge's captain gives its roles; a member on another bridge cannot be given
    /// one (they leave theirs first).</summary>
    public string? GiveRole(int by, string role, int to)
    {
        string bridge = BridgeOf(by);
        if (CaptainOf(bridge) != by) return "only the captain assigns the roles";
        if (!Roles.Has(role)) return $"no role '{role}'";
        if (Member(to) == null) return "no such crew member";
        if (to != by && BridgeOf(to) != bridge && (Ships.ContainsValue(to) || Held.ContainsValue(to))) return $"{NameOf(to)} is on another bridge";
        if (to == CaptainOf(bridge)) Held.Remove(Key(bridge, role));
        else Held[Key(bridge, role)] = to;
        return null;
    }

    /// <summary>Why the sender may not give this ship to this member, or null (and it is given; to the admiral, it goes back to
    /// the AI). Only the admiral gives ships; the member leaves any roles they held.</summary>
    public string? GiveShip(int by, string shipId, int to)
    {
        if (by != AdmiralId) return "only the admiral assigns ships";
        if (shipId == Flag) return "the flagship is the admiral's";
        if (to == AdmiralId) { Ships.Remove(shipId); foreach (string k in Held.Keys.Where(k => k.StartsWith(shipId + "|")).ToList()) Held.Remove(k); return null; }
        if (Member(to) == null) return "no such crew member";
        foreach (string s in Ships.Where(kv => kv.Value == to).Select(kv => kv.Key).ToList()) Ships.Remove(s);
        foreach (string k in Held.Where(kv => kv.Value == to).Select(kv => kv.Key).ToList()) Held.Remove(k);
        Ships[shipId] = to;
        return null;
    }

    /// <summary>A member's ship moved (its captain took command of another ship of the fleet): their bridge and its roles
    /// move with them.</summary>
    public void MoveShip(string from, string to)
    {
        if (!Ships.Remove(from, out int cap)) return;
        Ships[to] = cap;
        foreach (string k in Held.Keys.Where(k => k.StartsWith(from + "|")).ToList()) { int m = Held[k]; Held.Remove(k); Held[to + k[from.Length..]] = m; }
    }
}
