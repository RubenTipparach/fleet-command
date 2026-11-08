// Scenes.Comms: what a scenario says beyond its ships' places, for the battle console mockup's transport and boarding test
// scenes (its SCENES' setup functions, as data): a ship's start set up (marines and specialists, cargo, what it asks for,
// the envoy, an order to capture, a distress call, its shields down, its power, damaged systems and wrecked mounts), the
// planets in the scene (Relief), and the scene's goal (a ship taken, or a ship or planet given all it asked for). Read by
// DataLoader.LoadScenario from optional keys, so the other scenarios need none of them (CLAUDE.md 6.4).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FleetCommand.Data;

/// <summary>A ship's start beyond its place (a scenario ship's "setup"; every field optional): its marines and specialists
/// (null: its class's), cargo in its hold, what it asks to be given, the envoy aboard, a ship it is ordered to capture once
/// it has the envoy or NeedMarines marines, a distress call, its shields down and unpowered, energy boxes by system,
/// subsystem health, and its mounts' health in mount order (a shorter list leaves the rest whole).</summary>
public sealed record ShipSetup(int? Marines, IReadOnlyDictionary<string, int> Team, IReadOnlyDictionary<string, int> Hold, IReadOnlyDictionary<string, int>? Needs,
    string? Vip, string? Capture, int NeedMarines, bool Distress, bool ShieldsDown, IReadOnlyDictionary<string, int> Power, IReadOnlyDictionary<string, double> Sys,
    IReadOnlyList<double> MountsHp);

/// <summary>A planet in a scene: where it is and its radius (metres), its colour, and what it asks to be given.</summary>
public sealed record PlanetDef(string Id, string Name, double XM, double ZM, double RadiusM, string Color, IReadOnlyDictionary<string, int> Needs)
{
    /// <summary>The kind of world, which picks its look in the view (PlanetLooks): terran, ocean, desert, ice, lava, barren or gas.</summary>
    public string Kind { get; init; } = "terran";
    /// <summary>What stands on its surface, to scan and send an away team to (Battle.Field).</summary>
    public IReadOnlyList<InstallationDef> Installations { get; init; } = System.Array.Empty<InstallationDef>();
    /// <summary>Whether its people will receive an envoy beamed down (a diplomat run).</summary>
    public bool TakesEnvoy { get; init; }
    /// <summary>Someone on the surface waiting to be beamed up (an envoy or a research team to extract), or null.</summary>
    public string? Waiting { get; init; }
    /// <summary>The planet kinds the view can draw.</summary>
    public static readonly string[] Kinds = { "terran", "ocean", "desert", "ice", "lava", "barren", "gas" };
}

/// <summary>Something on a planet's surface: a name, its kind (outpost, lab, ruin, mine, city, battery or wreck), where it
/// stands (latitude and longitude, degrees), whether it is hostile and the garrison an away team must beat there, and
/// whether it shows before the planet is scanned.</summary>
public sealed record InstallationDef(string Id, string Name, string Kind, double LatDeg, double LonDeg, bool Hostile, int Garrison, bool Known)
{
    public static readonly string[] Kinds = { "outpost", "lab", "ruin", "mine", "city", "battery", "wreck" };
}

/// <summary>A scene's goal: "taken", the ship Id joined to the player's side; or "relieved", the ship or planet Id given all it
/// asked for. Met, the battle is won; the ship destroyed first, it is lost.</summary>
public sealed record ScenarioGoal(string Kind, string Id);

public static class CommsScenes
{
    /// <summary>A ship's "setup", or null when it has none.</summary>
    public static ShipSetup? LoadSetup(StrictJson? o)
    {
        if (o == null) return null;
        int? marines = o.Has("marines") ? Whole(o, "marines") : MarkAbsent(o, "marines");
        var team = Counts(o.ObjOrNull("team"), CommsDef.Specialists);
        var hold = Counts(o.ObjOrNull("hold"), CommsDef.Items);
        StrictJson? nj = o.ObjOrNull("needs");
        IReadOnlyDictionary<string, int>? needs = nj == null ? null : Counts(nj, CommsDef.Items);
        string vip = o.Str("vip", ""), cap = o.Str("capture", "");
        int need = o.Has("need_marines") ? Whole(o, "need_marines") : MarkZero(o, "need_marines");
        var power = Counts(o.ObjOrNull("power_e"), Sim.Rules.Systems);
        foreach (var (k, v) in power) if (v > Sim.Rules.BoxesPerSystem) throw new DataError($"{o.Where}.power_e.{k}: at most {Sim.Rules.BoxesPerSystem} boxes");
        var sys = new Dictionary<string, double>();
        if (o.ObjOrNull("sys") is { } sj)
        {
            foreach (string k in sj.Keys())
            {
                if (!Sim.Rules.Subsystems.Contains(k)) throw new DataError($"{sj.Where}.{k}: not a subsystem (there are: {string.Join(", ", Sim.Rules.Subsystems)})");
                double v = sj.Num(k);
                if (v < 0 || v > 1) throw new DataError($"{sj.Where}.{k}: a health between 0 and 1");
                sys[k] = v;
            }
            sj.Done();
        }
        double[] hp = o.Has("mounts_hp") ? o.NumList("mounts_hp") : MarkEmpty(o, "mounts_hp");
        foreach (double v in hp) if (v < 0 || v > 1) throw new DataError($"{o.Where}.mounts_hp: each a health between 0 and 1");
        var s = new ShipSetup(marines, team, hold, needs, vip == "" ? null : vip, cap == "" ? null : cap, need, o.Bool("distress", false), o.Bool("shields_down", false), power, sys, hp);
        o.Done();
        return s;
    }

    /// <summary>A scene's planets (its "planets"), or null when it has none.</summary>
    public static IReadOnlyList<PlanetDef>? LoadPlanets(StrictJson root)
    {
        if (!root.Has("planets")) { root.ObjOrNull("planets"); return null; }
        var list = new List<PlanetDef>();
        foreach (StrictJson p in root.ObjList("planets"))
        {
            string id = p.Str("id"), col = p.Str("color");
            if (!Regex.IsMatch(col, "^#[0-9a-fA-F]{6}$")) throw new DataError($"{p.Where}.color: a colour as #rrggbb, not '{col}'");
            double r = p.Num("radius_m");
            if (r <= 0) throw new DataError($"{p.Where}.radius_m: must be above 0");
            list.Add(new PlanetDef(id, p.Str("name"), p.Num("x_m"), p.Num("z_m"), r, col, Counts(p.ObjOrNull("needs"), CommsDef.Items)));
            p.Done();
        }
        return list;
    }

    /// <summary>A scene's goal (its "goal"), or null when it has none.</summary>
    public static ScenarioGoal? LoadGoal(StrictJson root)
    {
        if (root.ObjOrNull("goal") is not { } g) return null;
        string kind = g.Str("kind");
        if (kind is not ("taken" or "relieved")) throw new DataError($"{g.Where}.kind: taken or relieved, not '{kind}'");
        var goal = new ScenarioGoal(kind, g.Str("id"));
        g.Done();
        return goal;
    }

    /// <summary>Every id a scene's setups and goal name is a ship or planet of that scene: a capture order an enemy ship, a
    /// taken goal a ship, a relieved goal a ship or planet with needs.</summary>
    public static void Check(string where, IReadOnlyList<ShipStart> ships, IReadOnlyList<PlanetDef>? planets, ScenarioGoal? goal)
    {
        var ids = new HashSet<string>(ships.Select(s => s.Id));
        foreach (PlanetDef p in planets ?? Array.Empty<PlanetDef>())
            if (!ids.Add(p.Id)) throw new DataError($"{where}.planets: '{p.Id}' is also a ship's or planet's id");
        foreach (ShipStart s in ships)
            if (s.Setup?.Capture is { } c && ships.FirstOrDefault(o => o.Id == c) is not { } t)
                throw new DataError($"{where}.ships.{s.Id}.setup.capture: no ship '{c}'");
            else if (s.Setup?.Capture is { } c2 && ships.First(o => o.Id == c2).Side == s.Side)
                throw new DataError($"{where}.ships.{s.Id}.setup.capture: '{c2}' is on its own side");
        if (goal == null) return;
        ShipStart? gs = ships.FirstOrDefault(o => o.Id == goal.Id);
        PlanetDef? gp = planets?.FirstOrDefault(o => o.Id == goal.Id);
        if (goal.Kind == "taken" && gs == null) throw new DataError($"{where}.goal.id: no ship '{goal.Id}'");
        if (goal.Kind == "relieved" && (gs?.Setup?.Needs ?? gp?.Needs) is not { Count: > 0 }) throw new DataError($"{where}.goal.id: '{goal.Id}' is no ship or planet with needs");
    }

    // counts by key, every key one of the allowed list, each a whole number 0 or more
    private static Dictionary<string, int> Counts(StrictJson? o, string[] allowed)
    {
        var d = new Dictionary<string, int>();
        if (o == null) return d;
        foreach (string k in o.Keys())
        {
            if (!allowed.Contains(k)) throw new DataError($"{o.Where}.{k}: unknown (there are: {string.Join(", ", allowed)})");
            d[k] = Whole(o, k);
        }
        o.Done();
        return d;
    }

    private static int Whole(StrictJson o, string k)
    {
        double v = o.Num(k);
        if (v < 0 || v != Math.Floor(v) || v > 100000) throw new DataError($"{o.Where}.{k}: expected a whole number, 0 or more");
        return (int)v;
    }
    private static int? MarkAbsent(StrictJson o, string k) { o.ObjOrNull(k); return null; }
    private static int MarkZero(StrictJson o, string k) { o.ObjOrNull(k); return 0; }
    private static double[] MarkEmpty(StrictJson o, string k) { o.ObjOrNull(k); return Array.Empty<double>(); }
}
