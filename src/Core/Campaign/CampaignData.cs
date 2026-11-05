// CampaignData: the campaign's data (data/campaign/campaign.json), typed and validated (CLAUDE.md section 6.4), and the
// missions (data/missions/*.json, MissionLoader). Plain records, no behaviour: the rules that use them are in Campaign.cs.
// The campaign mockup (docs/mockups/game-flow.html) reads the same file, so the map, the hulls and the prices are one copy.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Campaign;

public sealed record CampFaction(string Id, string Name, string Short, string Color, string? Rival, bool Playable, string Prefix, string Home,
    IReadOnlyList<string> Names);

/// <summary>A sector's terrain: what it costs to enter in move points; a star is never entered.</summary>
public sealed record Terrain(string Id, string Name, int Cost, bool Impassable);

public sealed record Tier(int Cost, IReadOnlyDictionary<string, int> Slots, int Hp);

/// <summary>A hull the shipyard builds: its faction, tier, name, class name, the battle class it fights as, and its
/// top-down schematic (faction/id under assets/ships/schematics/, empty for a stand-in).</summary>
public sealed record Hull(string Id, string Side, int Tier, string Name, string Cls, string BattleClass, string Schematic, bool ForSale)
{
    public string ClassLower => Cls.Replace(" (stand-in)", "").ToLowerInvariant();
}

public sealed record ShipSystem(string Id, string Group, string Name, int Cost, string Desc);

/// <summary>A supply a ship carries: at most one per HpPer hull points, PerFit per fitted system Fit, or Fixed.</summary>
public sealed record Supply(string Id, string Name, int Cost, int HpPer, int PerFit, string Fit, int Fixed);

public sealed record Place(string Kind, int C, int R, string Name, string Owner);

public sealed record FleetDef(string Name, string Fac, int C, int R, int Size, bool Commander);

public sealed record CampRules(int Cols, int Rows, ulong Seed, int StartCredits, int StartMarines, int MovePoints, int FleetMax, int CreditRate,
    double SaleShare, double RivalPays, double PiratesPay, int SaleStanding, int PirateSaleStanding, int AllyStanding, int StandingOwn,
    int StandingRival, int MissionsMax, int MissionsStart, IReadOnlyDictionary<string, double> Pay, IReadOnlyDictionary<string, double> Prestige,
    double StoryPrestigeX, IReadOnlyList<string> Strengths, int RewardSteps, int RewardStep);

public sealed record MapDef(IReadOnlyList<(string Fac, int C, int R)> Seeds, double SeedJitter, double NebulaBelow, double AsteroidsBelow,
    IReadOnlyList<(int C, int R)> Stars, IReadOnlyList<Place> Places, IReadOnlyList<FleetDef> Fleets, int RangePirate, int RangeLone, int RangeGroup,
    IReadOnlyList<(string Fac, string A, string B)> FreightRoutes, IReadOnlyList<string> FreighterNames);

public sealed record OfficerDef(IReadOnlyList<string> Stations, IReadOnlyList<(string Skill, int Cost)> Skills, IReadOnlyList<string> Names, int Pool);

public sealed record CampaignData(CampRules Rules, IReadOnlyDictionary<string, CampFaction> Factions, IReadOnlyDictionary<string, Terrain> Terrain,
    IReadOnlyList<(string Id, string Name)> Groups, IReadOnlyList<Tier> Tiers, IReadOnlyDictionary<string, Hull> Hulls,
    IReadOnlyDictionary<string, ShipSystem> Systems, IReadOnlyDictionary<string, string> Stock, IReadOnlyList<Supply> Supplies, MapDef Map,
    OfficerDef Officers, IReadOnlyDictionary<string, MissionScript> Missions)
{
    public IEnumerable<Hull> HullsOf(string side) => Hulls.Values.Where(h => h.Side == side).OrderBy(h => h.Tier);
    /// <summary>The side's hull of a tier, or its smallest when it has none of that tier.</summary>
    public Hull HullOfTier(string side, int tier) => HullsOf(side).FirstOrDefault(h => h.Tier == tier) ?? HullsOf(side).First();
    public Tier TierOf(Hull h) => Tiers[h.Tier - 1];
    public IEnumerable<MissionScript> Contracts => Missions.Values.Where(m => !m.Story).OrderBy(m => m.Id, StringComparer.Ordinal);

    /// <summary>The paths, relative to data/, of the campaign's file; missions are every .json under data/missions/.</summary>
    public const string File = "campaign/campaign.json";

    /// <summary>Loads and checks the campaign against the battle's data (every battle class and faction must exist).</summary>
    public static CampaignData Load(Func<string, string> read, IEnumerable<string> missionFiles, GameData battle)
    {
        StrictJson root = StrictJson.Parse(read(File), "data/" + File);
        StrictJson ru = root.Obj("rules");
        Dictionary<string, double> Table(StrictJson o, string key)
        {
            StrictJson t = o.Obj(key);
            var d = MissionLoader.GradeIds.ToDictionary(g => g, g => t.Num(g));
            t.Done();
            return d;
        }
        var rules = new CampRules(ru.Int("cols"), ru.Int("rows"), (ulong)ru.Int("seed"), ru.Int("start_credits_cr"), ru.Int("start_marines"),
            ru.Int("move_points"), ru.Int("fleet_max"), ru.Int("credit_rate_cr"), ru.Num("sale_share"), ru.Num("rival_pays"), ru.Num("pirates_pay"),
            ru.Int("sale_standing"), ru.Int("pirate_sale_standing"), ru.Int("ally_standing"), ru.Int("standing_own"), ru.Int("standing_rival"),
            ru.Int("missions_max"), ru.Int("missions_start"), Table(ru, "pay"), Table(ru, "prestige"), ru.Num("story_prestige_x"),
            ru.StrList("strengths"), ru.Int("reward_steps"), ru.Int("reward_step_cr"));
        ru.Done();
        if (rules.Cols < 2 || rules.Rows < 2) throw new DataError("data/campaign/campaign.json.rules: the map needs at least 2 x 2 sectors");

        StrictJson fo = root.Obj("factions");
        var factions = new Dictionary<string, CampFaction>();
        foreach (string id in fo.Keys())
        {
            StrictJson f = fo.Child(id);
            string rival = f.Str("rival");
            factions[id] = new CampFaction(id, f.Str("name"), f.Str("short"), f.Str("color"), rival == "" ? null : rival, f.Bool("playable", false),
                f.Str("prefix"), f.Str("home"), f.StrList("names"));
            if (!battle.Factions.ContainsKey(id)) throw new DataError($"{f.Where}: no faction '{id}' in data/battle/factions.json");
            f.Done();
        }
        fo.Done();
        foreach (CampFaction f in factions.Values)
            if (f.Rival != null && !factions.ContainsKey(f.Rival)) throw new DataError($"data/campaign/campaign.json.factions.{f.Id}.rival: no faction '{f.Rival}'");

        StrictJson to = root.Obj("terrain");
        var terrain = new Dictionary<string, Terrain>();
        foreach (string id in to.Keys())
        {
            StrictJson t = to.Child(id);
            terrain[id] = new Terrain(id, t.Str("name"), t.Int("cost"), t.Bool("impassable", false));
            if (!terrain[id].Impassable && terrain[id].Cost < 1) throw new DataError($"{t.Where}.cost: at least 1");
            t.Done();
        }
        to.Done();
        foreach (string need in new[] { "open", "nebula", "asteroids", "star" })
            if (!terrain.ContainsKey(need)) throw new DataError($"data/campaign/campaign.json.terrain: needs '{need}'");

        var groups = new List<(string, string)>();
        foreach (string[] g in StrPairs(root, "groups", 2)) groups.Add((g[0], g[1]));
        var groupIds = groups.Select(g => g.Item1).ToList();

        var tiers = new List<Tier>();
        foreach (StrictJson t in root.ObjList("tiers"))
        {
            StrictJson s = t.Obj("slots");
            var slots = groupIds.ToDictionary(g => g, g => s.Int(g));
            s.Done();
            tiers.Add(new Tier(t.Int("cost"), slots, t.Int("hp")));
            t.Done();
        }

        StrictJson ho = root.Obj("hulls");
        var hulls = new Dictionary<string, Hull>();
        foreach (string id in ho.Keys())
        {
            StrictJson h = ho.Child(id);
            var hull = new Hull(id, h.Str("side"), h.Int("tier"), h.Str("name"), h.Str("cls"), h.Str("battle_class"), h.Str("schematic"), h.Bool("for_sale", true));
            if (!factions.ContainsKey(hull.Side)) throw new DataError($"{h.Where}.side: no faction '{hull.Side}'");
            if (hull.Tier < 1 || hull.Tier > tiers.Count) throw new DataError($"{h.Where}.tier: 1 to {tiers.Count}");
            if (!battle.Classes.ContainsKey(hull.BattleClass)) throw new DataError($"{h.Where}.battle_class: no class '{hull.BattleClass}' in data/battle/classes.json");
            h.Done();
            hulls[id] = hull;
        }
        ho.Done();
        foreach (CampFaction f in factions.Values)
            if (!hulls.Values.Any(h => h.Side == f.Id)) throw new DataError($"data/campaign/campaign.json.hulls: faction '{f.Id}' has no hull");

        StrictJson so = root.Obj("systems");
        var systems = new Dictionary<string, ShipSystem>();
        foreach (string id in so.Keys())
        {
            StrictJson s = so.Child(id);
            systems[id] = new ShipSystem(id, s.Str("g"), s.Str("name"), s.Int("cost"), s.Str("d"));
            if (!groupIds.Contains(systems[id].Group)) throw new DataError($"{s.Where}.g: no hardpoint group '{systems[id].Group}'");
            s.Done();
        }
        so.Done();
        StrictJson st = root.Obj("stock");
        var stock = groupIds.ToDictionary(g => g, g => st.Str(g));
        st.Done();
        foreach (var (g, sys) in stock)
            if (!systems.TryGetValue(sys, out ShipSystem? s) || s.Group != g) throw new DataError($"data/campaign/campaign.json.stock.{g}: '{sys}' is no {g} system");

        var supplies = new List<Supply>();
        foreach (StrictJson u in root.ObjList("supplies"))
        {
            supplies.Add(new Supply(u.Str("id"), u.Str("name"), u.Int("cost"), (int)u.Num("hp_per", 0), (int)u.Num("per_fit", 0), u.Str("fit", ""), (int)u.Num("fixed", 0)));
            u.Done();
        }
        if (supplies.All(s => s.Id != "marines")) throw new DataError("data/campaign/campaign.json.supplies: needs 'marines'");

        StrictJson mo = root.Obj("map");
        var seeds = Rows(mo, "seeds", 3).Select(a => (a[0].ToString()!, Convert.ToInt32(a[1]), Convert.ToInt32(a[2]))).ToList();
        var stars = Rows(mo, "stars", 2).Select(a => (Convert.ToInt32(a[0]), Convert.ToInt32(a[1]))).ToList();
        var places = Rows(mo, "places", 5).Select(a => new Place(a[0].ToString()!, Convert.ToInt32(a[1]), Convert.ToInt32(a[2]), a[3].ToString()!, a[4].ToString()!)).ToList();
        var fleets = new List<FleetDef>();
        foreach (StrictJson f in mo.ObjList("fleets"))
        {
            fleets.Add(new FleetDef(f.Str("name"), f.Str("fac"), f.Int("c"), f.Int("r"), f.Int("size"), f.Bool("commander", false)));
            f.Done();
        }
        StrictJson fr = mo.Obj("fleet_range");
        int rp = fr.Int("pirate"), rl = fr.Int("lone"), rg = fr.Int("group");
        fr.Done();
        var routes = StrPairs(mo, "freight_routes", 3).Select(a => (a[0], a[1], a[2])).ToList();
        var map = new MapDef(seeds, mo.Num("seed_jitter"), mo.Num("nebula_below"), mo.Num("asteroids_below"), stars, places, fleets, rp, rl, rg,
            routes, mo.StrList("freighter_names"));
        mo.Done();
        foreach (var (c, r) in stars.Concat(places.Select(p => (p.C, p.R))).Concat(fleets.Select(f => (f.C, f.R))))
            if (c < 0 || r < 0 || c >= rules.Cols || r >= rules.Rows) throw new DataError($"data/campaign/campaign.json.map: sector {c},{r} is off the map");
        foreach (var (fac, a, b) in routes)
            if (!places.Any(p => p.Name == a) || !places.Any(p => p.Name == b)) throw new DataError($"data/campaign/campaign.json.map.freight_routes: no port '{a}' or '{b}'");
        foreach (CampFaction f in factions.Values.Where(f => f.Playable))
            if (!places.Any(p => p.Name == f.Home && p.Owner == f.Id)) throw new DataError($"data/campaign/campaign.json.factions.{f.Id}.home: no port '{f.Home}' of its own");

        StrictJson oo = root.Obj("officers");
        var skills = Rows(oo, "skills", 2).Select(a => (a[0].ToString()!, Convert.ToInt32(a[1]))).ToList();
        var officers = new OfficerDef(oo.StrList("stations"), skills, oo.StrList("names"), oo.Int("pool"));
        oo.Done();
        root.Done();

        var missions = MissionLoader.LoadAll(missionFiles, read);
        return new CampaignData(rules, factions, terrain, groups, tiers, hulls, systems, stock, supplies, map, officers, missions);
    }

    // An array of arrays of strings and whole numbers, as the map's compact rows are written.
    private static List<object[]> Rows(StrictJson o, string key, int n)
    {
        var raw = o.RawArray(key);
        for (int i = 0; i < raw.Count; i++)
            if (raw[i].Length != n) throw new DataError($"{o.Where}.{key}[{i}]: expected {n} values");
        return raw;
    }

    private static List<string[]> StrPairs(StrictJson o, string key, int n) => Rows(o, key, n).Select(a => a.Select(x => x.ToString()!).ToArray()).ToList();
}
