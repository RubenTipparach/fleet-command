// Roles: the bridge roles of a co-op crew, read from data/coop/roles.json (openspec/changes/bridge-crew-coop). Each role
// names the console tabs it opens and the battle orders it may give; an order no role lists is the ship's captain's, and
// a few are the admiral's alone. Read once, validated like every data file (CLAUDE.md section 6.4): every order named must
// be one the battle relays (Battle.Crew.cs), and none is listed twice.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Coop;

/// <summary>One bridge role: its id, name and icon, whether it keeps its own target, the station tabs and ship pages it
/// opens, and the orders it may give.</summary>
public sealed record RoleDef(string Id, string Name, string Icon, bool Targets, IReadOnlyList<string> Stations, IReadOnlyList<string> Pages, IReadOnlyList<string> Orders);

/// <summary>A crew rank: its name and short form, the XP it is reached at, and the skill it adds to the stations of every role
/// the member holds.</summary>
public sealed record RankDef(string Name, string Short, int Xp, int Skill);

public sealed class Roles
{
    /// <summary>The crew's ranks, lowest first (the first at 0 XP).</summary>
    public IReadOnlyList<RankDef> Ranks { get; private set; } = new[] { new RankDef("Ensign", "Ens.", 0, 0) };
    /// <summary>XP a crew member earns for each second of a battle they were aboard for, and on top for a battle won.</summary>
    public double XpPerBattleS { get; private set; }
    public int XpVictory { get; private set; }

    /// <summary>The rank a member's XP has reached.</summary>
    public RankDef RankAt(double xp) => Ranks.Last(r => xp >= r.Xp);
    public IReadOnlyList<RoleDef> List { get; }
    /// <summary>The station tabs and ship pages only a ship's captain opens (comms, the fleet, the ship's systems).</summary>
    public IReadOnlyList<string> CaptainStations { get; }
    public IReadOnlyList<string> CaptainPages { get; }
    /// <summary>Orders any crew member may give (a lock moves that member's targets).</summary>
    public IReadOnlySet<string> Shared { get; }
    /// <summary>Orders only the admiral may give (the fleet's, command of a ship, withdrawing, ending the battle).</summary>
    public IReadOnlySet<string> AdmiralOnly { get; }

    private readonly Dictionary<string, string> _orderRole = new();
    private readonly Dictionary<string, string> _tabRole = new();

    private Roles(List<RoleDef> list, string[] cs, string[] cp, string[] shared, string[] admiral)
    {
        List = list; CaptainStations = cs; CaptainPages = cp;
        Shared = shared.ToHashSet(); AdmiralOnly = admiral.ToHashSet();
        foreach (RoleDef r in list)
        {
            foreach (string o in r.Orders) _orderRole[o] = r.Id;
            foreach (string t in r.Stations) _tabRole["st." + t] = r.Id;
            foreach (string t in r.Pages) _tabRole["sh." + t] = r.Id;
        }
    }

    public RoleDef this[string id] => List.First(r => r.Id == id);
    public bool Has(string id) => List.Any(r => r.Id == id);

    /// <summary>The role an order belongs to, or null for the captain's (and the shared and admiral's orders).</summary>
    public string? RoleOfOrder(string order) => _orderRole.GetValueOrDefault(order);

    /// <summary>The role a console tab belongs to ("st.helm" a station, "sh.tractor" a ship page), or null for a captain's
    /// tab.</summary>
    public string? RoleOfTab(string tab) => _tabRole.GetValueOrDefault(tab);

    /// <summary>Reads data/coop/roles.json; <paramref name="orders"/> is every order the battle relays (a name not among
    /// them is an error).</summary>
    public static Roles Load(StrictJson root, IReadOnlySet<string> orders)
    {
        root.Str("_doc", "");
        var seen = new HashSet<string>();
        void Check(string where, string o)
        {
            if (!orders.Contains(o)) throw new DataError($"{where}: '{o}' is not an order the battle relays");
            if (!seen.Add(o)) throw new DataError($"{where}: '{o}' is listed twice");
        }
        var list = new List<RoleDef>();
        foreach (StrictJson r in root.ObjList("roles"))
        {
            var def = new RoleDef(r.Str("id"), r.Str("name"), r.Str("icon"), r.Bool("targets", false), r.StrList("stations"), r.StrList("pages"), r.StrList("orders"));
            foreach (string o in def.Orders) Check($"{r.Where}.orders", o);
            if (list.Any(x => x.Id == def.Id)) throw new DataError($"{r.Where}.id: '{def.Id}' twice");
            r.Done();
            list.Add(def);
        }
        string[] shared = root.StrList("shared_orders"), admiral = root.StrList("admiral_orders");
        foreach (string o in shared) Check($"{root.Where}.shared_orders", o);
        foreach (string o in admiral) Check($"{root.Where}.admiral_orders", o);
        var roles = new Roles(list, root.StrList("captain_stations"), root.StrList("captain_pages"), shared, admiral);
        var ranks = root.ObjList("ranks").Select(r => { var d = new RankDef(r.Str("name"), r.Str("short"), r.Int("xp"), r.Int("skill")); r.Done(); return d; }).ToList();
        if (ranks.Count == 0 || ranks[0].Xp != 0) throw new DataError($"{root.Where}.ranks: the first rank must start at 0 XP");
        for (int i = 1; i < ranks.Count; i++)
            if (ranks[i].Xp <= ranks[i - 1].Xp || ranks[i].Skill < ranks[i - 1].Skill) throw new DataError($"{root.Where}.ranks[{i}]: each rank needs more XP than the last, and no less skill");
        roles.Ranks = ranks;
        roles.XpPerBattleS = root.Num("xp_per_battle_s");
        roles.XpVictory = root.Int("xp_victory");
        if (!(roles.XpPerBattleS >= 0) || roles.XpVictory < 0) throw new DataError($"{root.Where}: XP rates must not be negative");
        root.Done();
        foreach (string t in Sim.Ship.TargetRoles)
            if (!roles.Has(t) || !roles[t].Targets) throw new DataError($"{root.Where}.roles: '{t}' must be a role that targets");
        return roles;
    }
}
