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
    /// <summary>The bridge each member moved to themselves (owner, 2026-11-11: the fleet and targeting managers "should let
    /// you change bridge and change targets"), until they are given a ship or move again.</summary>
    public SortedDictionary<int, string> Aboard { get; } = new();
    /// <summary>The bridge the admiral stands on: the flagship's, or any other ship of the fleet they stepped onto.</summary>
    public string AdmiralAt { get; set; } = Flag;
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
        // the admiral stands on any bridge of the fleet (owner, 2026-11-11: "The admiral has access to all roles and all ships
        // at all times"), the flagship's until they step onto another
        if (member == AdmiralId) return AdmiralAt;
        foreach (var (ship, cap) in Ships) if (cap == member) return ship;
        if (Aboard.TryGetValue(member, out string? at) && (at == Flag || Ships.ContainsKey(at))) return at;
        foreach (var (k, m) in Held) if (m == member) return k[..k.IndexOf('|')];
        return Flag;
    }

    /// <summary>The captain of a bridge: the admiral's on the flagship, else the member the ship was given to.</summary>
    public int CaptainOf(string bridge) => bridge == Flag ? AdmiralId : Ships.GetValueOrDefault(bridge, AdmiralId);

    /// <summary>Whether the admiral stands on a bridge another member captains (the admiral then opens every station there,
    /// but the stations stay its captain's).</summary>
    public bool AdmiralVisiting(string bridge) => bridge != Flag && Ships.ContainsKey(bridge);

    /// <summary>Who holds a role on a bridge: the member given it, else the bridge's captain.</summary>
    public int HolderOf(string bridge, string role) => Held.TryGetValue(Key(bridge, role), out int m) ? m : CaptainOf(bridge);

    /// <summary>Whether a member's console opens a role on their bridge: they hold it, or they are its captain.</summary>
    public bool Opens(int member, string role)
    {
        if (member == AdmiralId) return true;   // every role, on every ship
        string b = BridgeOf(member);
        return CaptainOf(b) == member || HolderOf(b, role) == member;
    }

    /// <summary>Whether a member's console opens a tab ("st.helm" a station, "sh.tractor" a ship page). A tab no role names
    /// is the captain's.</summary>
    public bool OpensTab(int member, string tab)
    {
        if (member == AdmiralId) return true;
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
    public IReadOnlyList<string> LockRoles(int member, string? openTab, IReadOnlyList<string>? chosen = null)
    {
        string b = BridgeOf(member);
        // the stations the member picked on the Tactical or Fleet station's Lock for keys: those of them they open that target
        if (chosen is { Count: > 0 })
        {
            var picked = chosen.Where(r => Roles.Has(r) && Roles[r].Targets && Opens(member, r)).Distinct().OrderBy(r => r, StringComparer.Ordinal).ToArray();
            if (picked.Length > 0) return picked;
        }
        if (member == AdmiralId && AdmiralVisiting(b)) return Roles.List.Where(x => x.Targets).Select(x => x.Id).ToArray();
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
        Aboard.Remove(id);
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
        if (to != by && BridgeOf(to) != bridge) return $"{NameOf(to)} is on another bridge";
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
        Aboard.Remove(to);
        Ships[shipId] = to;
        return null;
    }

    /// <summary>Why a member may not move to a bridge (the flagship's, or a ship another member captains), or null (and they
    /// are there, holding no role until its captain gives them one). The admiral changes ship by taking command; a ship's
    /// captain stays on their own bridge.</summary>
    public string? MoveTo(int member, string bridge)
    {
        if (Member(member) == null) return "not aboard";
        if (member == AdmiralId) { AdmiralAt = bridge; return null; }   // any ship of the fleet (the lockstep checks it is one)
        if (Ships.ContainsValue(member)) return "a captain stays on their own bridge";
        if (bridge != Flag && !Ships.ContainsKey(bridge)) return "no crewed bridge there";
        if (BridgeOf(member) == bridge) return "already there";
        foreach (string k in Held.Where(kv => kv.Value == member).Select(kv => kv.Key).ToList()) Held.Remove(k);
        Aboard[member] = bridge;
        return null;
    }

    /// <summary>The bridges a member may be on: the flagship's and each ship a member captains.</summary>
    public IEnumerable<string> Bridges() => new[] { Flag }.Concat(Ships.Keys);

    /// <summary>A member's ship moved (its captain took command of another ship of the fleet): their bridge and its roles
    /// move with them.</summary>
    public void MoveShip(string from, string to)
    {
        if (!Ships.Remove(from, out int cap)) return;
        Ships[to] = cap;
        foreach (string k in Held.Keys.Where(k => k.StartsWith(from + "|")).ToList()) { int m = Held[k]; Held.Remove(k); Held[to + k[from.Length..]] = m; }
        foreach (int m in Aboard.Where(kv => kv.Value == from).Select(kv => kv.Key).ToList()) Aboard[m] = to;
    }
}
