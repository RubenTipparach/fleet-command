// MissionScopes: what a mission file's expressions can read, in each place they are evaluated (Scopes lists the names, and
// MissionLoader refuses any other). OfferScope: the campaign, the mission and its sector (setup, briefing, needs, battle).
// HexScope: one candidate sector (where). EndScope: the battle as it ended (an objective's done) and the outcome (grade and
// result rules). Every value is a double or a string; true is 1.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Campaign;

public class OfferScope : IScope
{
    protected readonly Campaign C;
    protected readonly Offer M;

    public OfferScope(Campaign c, Offer m) { C = c; M = m; }

    private Sector Here => C.Hex(M.C, M.R);
    private FactionFleet? Group => M.Group != null ? C.S.Groups.FirstOrDefault(g => g.Name == M.Group) : C.GroupAt(M.C, M.R);
    private string Foe => C.Hostile(Here) ? Here.Owner : C.Enemy;
    private string Name(string f) => C.Data.Factions.TryGetValue(f, out CampFaction? x) ? x.Name : f;
    private static double B(bool b) => b ? 1 : 0;

    public virtual object Get(string n)
    {
        object? v = M.Value(n);
        if (v != null) return v;
        return n switch
        {
            "side" => C.S.Side, "side_name" => C.Side.Name,
            "rival" => C.Rival ?? "", "rival_name" => C.Rival != null ? Name(C.Rival) : "",
            "enemy" => C.Enemy, "enemy_name" => Name(C.Enemy),
            "foe" => Foe, "foe_name" => Name(Foe),
            "home" => C.Home.PortName ?? "home",
            "reward" => M.Reward,
            "strength" => (double)(C.Data.Rules.Strengths.ToList().IndexOf(M.Strength) + 1),
            "fleet" => (double)C.S.Fleet.Count,
            "marines" => (double)C.Marines,
            "sector_owner" => Here.Owner, "sector_owner_name" => Name(Here.Owner),
            "sector_station" => Here.Station ?? "", "sector_planet" => Here.Planet ?? "",
            "sector_terrain" => Here.Terrain, "asteroids" => B(Here.Terrain == "asteroids"), "nebula" => B(Here.Terrain == "nebula"),
            "group" => Group?.Name ?? "", "group_fac" => Group?.Fac ?? "", "group_size" => (double)(Group?.Size ?? 0),
            "story" => B(C.ScriptOf(M).Story),
            _ => throw new DataError($"mission {M.Script}: no value '{n}' here"),
        };
    }

    public virtual object Call(string n, IReadOnlyList<object> a)
    {
        string S(int i) => i < a.Count ? Expr.Format(a[i]) : throw new DataError($"{n}(): needs {i + 1} arguments");
        int I(int i) => i < a.Count ? (int)Expr.ToNum(a[i], n) : throw new DataError($"{n}(): needs {i + 1} arguments");
        switch (n)
        {
            case "rand": return C.R.Next();
            case "name": return Name(S(0));
            case "name_of":
            {
                var names = C.Data.Factions.TryGetValue(S(0), out CampFaction? f) ? f.Names : C.Data.Factions["vos"].Names;
                return names[((I(1) % names.Count) + names.Count) % names.Count];
            }
            case "hull_of": return C.Data.HullOfTier(C.Data.HullsOf(S(0)).Any() ? S(0) : "vos", I(1)).Id;
            case "class_of": return C.Data.Hulls.TryGetValue(S(0), out Hull? h) ? h.ClassLower : "raider";
            default: throw new DataError($"mission {M.Script}: no function '{n}()' here");
        }
    }
}

/// <summary>One candidate sector, for a mission's where.</summary>
public sealed class HexScope : OfferScope
{
    private readonly Sector _h;
    public HexScope(Campaign c, Sector h, Offer probe) : base(c, probe) { _h = h; }

    public override object Get(string n)
    {
        double B(bool b) => b ? 1 : 0;
        FactionFleet? g = C.GroupAt(_h.C, _h.R);
        return n switch
        {
            "owner" => _h.Owner,
            "own" => B(C.Own(_h)),
            "hostile" => B(C.Hostile(_h)),
            "neutral" => B(_h.Owner == "neutral"),
            "plain" => B(!_h.IsPort),
            "port" => B(_h.IsPort),
            "station" => _h.Station ?? "",
            "planet" => _h.Planet ?? "",
            "terrain" => _h.Terrain,
            "dist_home" => (double)HexGrid.Dist(_h.C, _h.R, C.S.HomeC, C.S.HomeR),
            "has_group" => B(g != null),
            "group_size_here" => (double)(g?.Size ?? 0),
            "group_fac_here" => g?.Fac ?? "",
            "commander_here" => B(g?.Commander ?? false),
            "is_home" => B(_h.C == C.S.HomeC && _h.R == C.S.HomeR),
            _ => base.Get(n),
        };
    }

    public override object Call(string n, IReadOnlyList<object> a) =>
        n == "on_route" ? (C.RoutesOf(Expr.Format(a.Count > 0 ? a[0] : "")).Contains((_h.C, _h.R)) ? 1.0 : 0.0) : base.Call(n, a);
}

/// <summary>The battle as it ended: its groups' ships (by group id, plus fleet, enemies and allies), how it ended, and,
/// once the objectives are known, which were met and the grade (for grade and result rules).</summary>
public sealed class EndScope : OfferScope
{
    private readonly IReadOnlyDictionary<string, List<ShipEnd>> _groups;
    private readonly string _how;
    public IReadOnlyDictionary<string, bool>? Met { get; set; }
    public IReadOnlyList<MissionObjective> Objectives { get; set; } = Array.Empty<MissionObjective>();
    public string? Grade { get; set; }
    public double Pay { get; set; }
    public bool HadBattle { get; set; } = true;
    /// <summary>What field operations came to: planets, cargo, the time limit, who fired first.</summary>
    public FieldEnd Field { get; set; } = FieldEnd.None;
    /// <summary>The marker beacons the fleet reached in the battle.</summary>
    public IReadOnlyCollection<string> Reached { get; set; } = Array.Empty<string>();

    public EndScope(Campaign c, Offer m, IReadOnlyDictionary<string, List<ShipEnd>> groups, string how) : base(c, m) { _groups = groups; _how = how; }

    private bool Ok(MissionObjective o) => Met != null && Met.GetValueOrDefault(o.Id);

    public override object Get(string n)
    {
        if (_groups.ContainsKey(n)) return n; // a group's id names the group: alive(raiders)
        double B(bool b) => b ? 1 : 0;
        int gi = Array.IndexOf(MissionLoader.GradeIds, Grade ?? "");
        return n switch
        {
            "how" => _how,
            "time_up" => B(Field.TimeUp),
            "we_fired_first" => B(Field.FirstFire == 0),
            "they_fired_first" => B(Field.FirstFire == 1),
            "no_shots" => B(Field.FirstFire == null),
            "teams_down" => (double)Field.TeamsDown,
            "battle" => B(HadBattle),
            "all_primary" => B(Objectives.Where(o => o.Primary).All(Ok)),
            "all_bonus" => B(Objectives.Where(o => !o.Primary).All(Ok) && Objectives.Any(o => !o.Primary)),
            "bonus_count" => (double)Objectives.Count(o => !o.Primary && Ok(o)),
            "grade" => Grade ?? "",
            "win" => B(gi >= Array.IndexOf(MissionLoader.GradeIds, "victory")),
            "pay" => Pay,
            "crushing" or "defeat" or "draw" or "victory" or "outstanding" => B(Grade == n),
            _ => base.Get(n),
        };
    }

    public override object Call(string n, IReadOnlyList<object> a)
    {
        if (n == "met") return (double)a.Count(x => Met != null && Met.GetValueOrDefault(Expr.Format(x)));
        if (n == "reached") return a.Count > 0 && Reached.Contains(Expr.Format(a[0])) ? 1.0 : 0.0;
        if (n == "cargo") return (double)(a.Count > 0 ? Field.Cargo.GetValueOrDefault(Expr.Format(a[0])) : 0);
        if (Scopes.PlanetCalls.Contains(n))
        {
            PlanetEnd p = a.Count > 0 && Field.Planets.TryGetValue(Expr.Format(a[0]), out PlanetEnd? pe) ? pe : PlanetEnd.None;
            return n switch
            {
                "surveyed" => p.Surveyed ? 1.0 : 0.0,
                "secured" => (double)p.Secured,
                "sites" => (double)p.Sites,
                "relieved" => p.Relieved ? 1.0 : 0.0,
                "envoy_landed" => p.EnvoyLanded ? 1.0 : 0.0,
                _ => p.PickedUp ? 1.0 : 0.0,
            };
        }
        List<ShipEnd> G() => a.Count > 0 && _groups.TryGetValue(Expr.Format(a[0]), out var g) ? g
            : throw new DataError($"{n}(): no group '{(a.Count > 0 ? Expr.Format(a[0]) : "")}' in this battle");
        return n switch
        {
            "size" => (double)G().Count,
            "alive" => (double)G().Count(s => s.Alive && !s.Taken),
            "destroyed" => (double)G().Count(s => !s.Alive),
            "taken" => (double)G().Count(s => s.Alive && s.Taken),
            "hull_min" => G().Where(s => s.Alive).Select(s => s.HullFrac).DefaultIfEmpty(0).Min(),
            "scanned" => (double)G().Count(s => s.Scanned),
            _ => base.Call(n, a),
        };
    }
}

/// <summary>One ship as the battle ended: alive or destroyed, taken (a prize that struck), hull left (fraction), and
/// whether the player's side finished a scan of it.</summary>
public sealed record ShipEnd(string Id, bool Alive, bool Taken, double HullFrac)
{
    public bool Scanned { get; init; }
}

/// <summary>A planet as the battle ended: surveyed, its sites held of all its sites, relieved (all it asked for given, or it
/// asked for nothing), an envoy landed, and the one waiting on it picked up (or no one was).</summary>
public sealed record PlanetEnd(bool Surveyed, int Secured, int Sites, bool Relieved, bool EnvoyLanded, bool PickedUp)
{
    public static readonly PlanetEnd None = new(false, 0, 0, false, false, false);
}

/// <summary>What field operations came to: each planet by id, the fleet's cargo by item, whether the time limit was reached,
/// the side that fired first (null: no one fired), and away teams still on a surface.</summary>
public sealed record FieldEnd(IReadOnlyDictionary<string, PlanetEnd> Planets, IReadOnlyDictionary<string, int> Cargo, bool TimeUp, int? FirstFire, int TeamsDown)
{
    public static readonly FieldEnd None = new(new Dictionary<string, PlanetEnd>(), new Dictionary<string, int>(), false, null, 0);
}
