// Crew: who is on the bridge in a co-op game and what each may touch (openspec/changes/bridge-crew-coop). The owner,
// 2026-11-11: "by default - captain has access to all roles", "if you have more than one ship, you can asing whole ships to
// other players", "whoeever is designated as captain remains captain the whole game", "the main captain is technically
// admiral of the fleet", "The admiral has access to all roles and all ships at all times", "When there is only one player
// assigned to a ship. That player has all roles on that ship", "Remember. Roles are not exclusive", "It's whatever the
// admiral sets up", "it persists from mission to mission until a ship is sold or destroyed".
//
// - Member 1 is the admiral: the host for the whole game, on the flagship's bridge unless they step onto another ship's;
//   they open every role on every ship.
// - A bridge is keyed "flag" for the admiral's flagship, or by the id of a ship the admiral sent a member to as its captain
//   (the first member sent to a ship the AI flew). Others sent there are its crew.
// - Roles are granted, not owned: any number of members may hold the same role on a bridge. A member sent alone to a ship
//   holds every role there; a member joining a crewed bridge holds none until the admiral (or that ship's captain) grants
//   some. A member opens only the tabs of the roles they hold; the rest are disabled.
// - Keyed by ship id, assignments and grants carry from battle to battle (a campaign ship keeps its id) until the ship is
//   gone from the fleet (Prune).
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
    /// <summary>Ships the admiral sent a member to as captain: ship id to its captain.</summary>
    public SortedDictionary<string, int> Ships { get; } = new(StringComparer.Ordinal);
    /// <summary>The bridge each other member stands on, when it is not a ship they captain (the flagship's, or a crewed ship's).</summary>
    public SortedDictionary<int, string> Aboard { get; } = new();
    /// <summary>The bridge the admiral stands on: the flagship's, or any other ship of the fleet they stepped onto.</summary>
    public string AdmiralAt { get; set; } = Flag;
    /// <summary>The roles granted on each bridge: "bridge|role|member" for each grant (a role may be granted to several).</summary>
    public SortedSet<string> Grants { get; } = new(StringComparer.Ordinal);

    public Crew(Roles roles, string admiralName)
    {
        Roles = roles;
        Members.Add(new CrewMember(AdmiralId, admiralName));
    }

    public CrewMember? Member(int id) => Members.FirstOrDefault(m => m.Id == id);
    public string NameOf(int id) => Member(id)?.Name ?? $"Player {id}";

    private static string Key(string bridge, string role, int member) => $"{bridge}|{role}|{member}";

    /// <summary>The bridge a member stands on: the admiral's choice for the admiral; the ship a member captains; the bridge
    /// they were sent to or moved to; else the flagship's.</summary>
    public string BridgeOf(int member)
    {
        if (member == AdmiralId) return AdmiralAt;
        foreach (var (ship, cap) in Ships) if (cap == member) return ship;
        if (Aboard.TryGetValue(member, out string? at) && (at == Flag || Ships.ContainsKey(at))) return at;
        return Flag;
    }

    /// <summary>The captain of a bridge: the admiral's on the flagship, else the member the ship was given to (the admiral when
    /// no member has it).</summary>
    public int CaptainOf(string bridge) => bridge == Flag ? AdmiralId : Ships.GetValueOrDefault(bridge, AdmiralId);

    /// <summary>Whether the admiral stands on a bridge a member captains.</summary>
    public bool AdmiralVisiting(string bridge) => bridge != Flag && Ships.ContainsKey(bridge);

    /// <summary>Whether a member holds a role on a bridge (the admiral holds every role everywhere).</summary>
    public bool Holds(string bridge, string role, int member) => member == AdmiralId || Grants.Contains(Key(bridge, role, member));

    /// <summary>The members who hold a role on a bridge, by id (the admiral not listed).</summary>
    public IEnumerable<int> HoldersOf(string bridge, string role) =>
        Members.Where(m => m.Id != AdmiralId && Grants.Contains(Key(bridge, role, m.Id))).Select(m => m.Id);

    /// <summary>The members on a bridge, by id (the admiral counted when standing on it).</summary>
    public IEnumerable<int> On(string bridge) => Members.Where(m => BridgeOf(m.Id) == bridge).Select(m => m.Id);

    /// <summary>Whether a member's console opens a role on their bridge: they hold it (the admiral holds every role).</summary>
    public bool Opens(int member, string role) => Holds(BridgeOf(member), role, member);

    /// <summary>Whether a member's console opens a tab ("st.helm" a station, "sh.tractor" a ship page). A tab no role names
    /// is the bridge's captain's (and the admiral's).</summary>
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
        if (member == AdmiralId) return null;
        if (Roles.AdmiralOnly.Contains(order)) return "the admiral's order";
        if (Roles.Shared.Contains(order)) return null;
        string? role = Roles.RoleOfOrder(order);
        if (role == null) return CaptainOf(BridgeOf(member)) == member ? null : "the captain's order";
        return Opens(member, role) ? null : $"the {Roles[role].Name} station's order";
    }

    /// <summary>The targeting roles a member's lock moves: those they picked (Lock for) that they hold; else every targeting
    /// role they hold. The admiral's default is the targeting roles no other member on that bridge holds (every one where
    /// none does), so the admiral's lock does not move a crew member's station unless picked.</summary>
    public IReadOnlyList<string> LockRoles(int member, string? openTab, IReadOnlyList<string>? chosen = null)
    {
        string b = BridgeOf(member);
        var targeting = Roles.List.Where(x => x.Targets).Select(x => x.Id).ToArray();
        if (chosen is { Count: > 0 })
        {
            var picked = chosen.Where(r => targeting.Contains(r) && Holds(b, r, member)).Distinct().OrderBy(r => r, StringComparer.Ordinal).ToArray();
            if (picked.Length > 0) return picked;
        }
        if (member == AdmiralId)
        {
            var free = targeting.Where(r => !HoldersOf(b, r).Any()).ToArray();
            return free.Length > 0 ? free : targeting;
        }
        return targeting.Where(r => Holds(b, r, member)).ToArray();
    }

    /// <summary>Adds a member who joined (the next free id), on the flagship's bridge holding no role.</summary>
    public CrewMember Join(string name)
    {
        int id = Members.Max(m => m.Id) + 1;
        var m = new CrewMember(id, name.Length == 0 ? $"Player {id}" : name);
        Members.Add(m);
        return m;
    }

    // every grant a member has, on every bridge, taken back
    private void Ungrant(int member)
    {
        foreach (string k in Grants.Where(k => k.EndsWith("|" + member)).ToList()) Grants.Remove(k);
    }

    // every role of a bridge granted to a member
    private void GrantAll(string bridge, int member)
    {
        foreach (RoleDef r in Roles.List) Grants.Add(Key(bridge, r.Id, member));
    }

    /// <summary>A member left: their ship goes back to the AI and their grants are taken back.</summary>
    public void Leave(int id)
    {
        if (id == AdmiralId) return;
        Members.RemoveAll(m => m.Id == id);
        Aboard.Remove(id);
        foreach (string s in Ships.Where(kv => kv.Value == id).Select(kv => kv.Key).ToList()) Ships.Remove(s);
        Ungrant(id);
    }

    /// <summary>Grants a role on a bridge to a member on it, or takes it back (<paramref name="on"/> false); returns why not,
    /// or null. The admiral grants on any bridge; a ship's captain on their own. Several members may hold the same role.</summary>
    public string? GrantRole(int by, string bridge, string role, int to, bool on)
    {
        if (CaptainOf(bridge) != by && by != AdmiralId) return "only the admiral or the ship's captain grants its roles";
        if (!Roles.Has(role)) return $"no role '{role}'";
        if (to == AdmiralId) return "the admiral holds every role";
        if (Member(to) == null) return "no such crew member";
        if (BridgeOf(to) != bridge) return $"{NameOf(to)} is on another bridge";
        if (on) Grants.Add(Key(bridge, role, to));
        else Grants.Remove(Key(bridge, role, to));
        return null;
    }

    /// <summary>The admiral sends a member to a ship: to a ship the AI flies, they take it as its captain, holding every
    /// role there; to the flagship or a crewed ship, they join its crew holding no role until one is granted. A ship they
    /// captained goes back to the AI. Returns why not, or null. <paramref name="aiShip"/>: the AI flies that ship.</summary>
    public string? Assign(int by, int member, string bridge, bool aiShip)
    {
        if (by != AdmiralId) return "only the admiral assigns crew to ships";
        if (Member(member) == null || member == AdmiralId) return "no such crew member";
        if (BridgeOf(member) == bridge) return null;
        if (!aiShip && bridge != Flag && !Ships.ContainsKey(bridge)) return "not a ship of the fleet";
        foreach (string s in Ships.Where(kv => kv.Value == member).Select(kv => kv.Key).ToList()) Ships.Remove(s);
        Ungrant(member);
        Aboard.Remove(member);
        if (aiShip) { Ships[bridge] = member; GrantAll(bridge, member); }
        else Aboard[member] = bridge;
        return null;
    }

    /// <summary>The admiral gives a ship back to the AI: its captain and crew return to the flagship's bridge, holding nothing.</summary>
    public string? Release(int by, string bridge)
    {
        if (by != AdmiralId) return "only the admiral";
        if (!Ships.ContainsKey(bridge)) return "the AI has it";
        foreach (int m in On(bridge).Where(m => m != AdmiralId).ToList()) { Ungrant(m); Aboard[m] = Flag; }
        Ships.Remove(bridge);
        if (AdmiralAt == bridge) AdmiralAt = Flag;
        return null;
    }

    /// <summary>A member moves themselves to a crewed bridge (the flagship's, or a ship another member captains), holding no
    /// role there; the admiral to any ship of the fleet (the lockstep checks it is one). A ship's captain stays on their own.</summary>
    public string? MoveTo(int member, string bridge)
    {
        if (Member(member) == null) return "not aboard";
        if (member == AdmiralId) { AdmiralAt = bridge; return null; }
        if (Ships.ContainsValue(member)) return "a captain stays on their own bridge";
        if (bridge != Flag && !Ships.ContainsKey(bridge)) return "no crewed bridge there";
        if (BridgeOf(member) == bridge) return "already there";
        Ungrant(member);
        Aboard[member] = bridge;
        return null;
    }

    /// <summary>The crewed bridges: the flagship's and each ship a member captains.</summary>
    public IEnumerable<string> Bridges() => new[] { Flag }.Concat(Ships.Keys);

    /// <summary>A member's ship moved (its captain took command of another ship of the fleet): its bridge, crew and grants move.</summary>
    public void MoveShip(string from, string to)
    {
        if (!Ships.Remove(from, out int cap)) return;
        Ships[to] = cap;
        foreach (string k in Grants.Where(k => k.StartsWith(from + "|")).ToList()) { Grants.Remove(k); Grants.Add(to + k[from.Length..]); }
        foreach (int m in Aboard.Where(kv => kv.Value == from).Select(kv => kv.Key).ToList()) Aboard[m] = to;
    }

    /// <summary>Drops every assignment to a ship no longer in the fleet (sold or destroyed): its crew back to the flagship.</summary>
    public void Prune(Func<string, bool> inFleet)
    {
        foreach (string s in Ships.Keys.Where(s => !inFleet(s)).ToList()) Release(AdmiralId, s);
        foreach (string k in Grants.Where(k => k[..k.IndexOf('|')] is var b && b != Flag && !inFleet(b)).ToList()) Grants.Remove(k);
        if (AdmiralAt != Flag && !inFleet(AdmiralAt)) AdmiralAt = Flag;
    }
}
