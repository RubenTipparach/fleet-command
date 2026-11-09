// ScenarioExtras: the scenario fields the battle console mockup's test scenes need beyond a plain battle, read strictly
// beside DataLoader.LoadScenario (CLAUDE.md 6.4) so the loader itself only gains two calls. A scene can strip its own ship
// of weapons (armed), name the tools that are decisive in it (lethal), hold asteroids (rocks), and start a ship with its
// own craft and mines (kit), its own energy boxes (power_e) or a damaged subsystem (sys_hp): the mockup's SCENES and their
// setup() (docs/mockups/battle-console.html). A weapons test scene arms one weapon (arm_only) and plays slowed (time_scale).
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Sim;

namespace FleetCommand.Data;

public static class ScenarioExtras
{
    /// <summary>The tools a scene may make lethal.</summary>
    public static readonly string[] LethalTools = { "mine", "rock", "shuttle", "barrage" };
    /// <summary>The fighters' missions (the Craft page's tabs).</summary>
    public static readonly string[] Missions = { "attack", "defend", "defences" };

    /// <summary>A ship start's kit, energy boxes and subsystem health, read from its object (s), on top of the start.</summary>
    public static ShipStart Ship(StrictJson s, ShipStart start, ShipClass c)
    {
        ShipKit? kit = null;
        if (s.ObjOrNull("kit") is { } k)
        {
            int? N(string key) { if (!k.Has(key)) return null; int v = k.Int(key); if (v < 0) throw new DataError($"{k.Where}.{key}: 0 or more"); return v; }
            string? mission = k.Has("mission") ? k.Str("mission") : null;
            if (mission != null && !Missions.Contains(mission)) throw new DataError($"{k.Where}.mission: {string.Join(", ", Missions)}, not '{mission}'");
            kit = new ShipKit(N("mines"), N("fighters"), N("shuttles"), N("missiles"), mission);
            k.Done();
        }
        Dictionary<string, int>? power = null;
        if (s.ObjOrNull("power_e") is { } p)
        {
            power = new Dictionary<string, int>();
            foreach (string sys in p.Keys())
            {
                if (!Rules.Systems.Contains(sys)) throw new DataError($"{p.Where}.{sys}: no system '{sys}' (there are: {string.Join(", ", Rules.Systems)})");
                int v = p.Int(sys);
                if (v < 0 || v > Rules.BoxesPerSystem) throw new DataError($"{p.Where}.{sys}: 0 to {Rules.BoxesPerSystem} boxes");
                power[sys] = v;
            }
            p.Done();
        }
        Dictionary<string, double>? hp = null;
        if (s.ObjOrNull("sys_hp") is { } h)
        {
            hp = new Dictionary<string, double>();
            foreach (string sys in h.Keys())
            {
                if (!Rules.Subsystems.Contains(sys)) throw new DataError($"{h.Where}.{sys}: no subsystem '{sys}'");
                double v = h.Num(sys);
                if (v < 0 || v > 1) throw new DataError($"{h.Where}.{sys}: health between 0 and 1");
                hp[sys] = v;
            }
            h.Done();
        }
        // mounts a scene adds to its class's (the mockup's Fighters scene gives the Ironhold three point-defence mounts)
        List<MountDef>? extra = null;
        if (s.Has("extra_mounts"))
        {
            extra = new List<MountDef>();
            foreach (StrictJson m in s.ObjList("extra_mounts"))
            {
                extra.Add(new MountDef(m.Str("weapon"), m.Num("x"), m.Num("y"), m.Num("arc_centre_deg", 0), m.Num("arc_width_deg", 360)));
                m.Done();
            }
        }
        // a weapons test scene's one weapon: every id must be on one of its mounts
        string[]? armOnly = s.Has("arm_only") ? s.StrList("arm_only") : null;
        if (armOnly != null)
            foreach (string w in armOnly)
                if (!c.Mounts.Concat(extra ?? new List<MountDef>()).Any(m => m.Weapon == w)) throw new DataError($"{s.Where}.arm_only: the {c.Id} has no '{w}' mount");
        return start with { Kit = kit, PowerE = power, SysHp = hp, ExtraMounts = extra, ArmOnly = armOnly };
    }

    /// <summary>A scene's armed flag, lethal tools and rocks, read from its object (root), on top of the scenario.</summary>
    public static Scenario Scene(StrictJson root, Scenario sc)
    {
        bool armed = root.Bool("armed", true);
        string[] lethal = root.Has("lethal") ? root.StrList("lethal") : System.Array.Empty<string>();
        foreach (string t in lethal)
            if (!LethalTools.Contains(t)) throw new DataError($"{root.Where}.lethal: {string.Join(", ", LethalTools)}, not '{t}'");
        var rocks = new List<RockDef>();
        if (root.Has("rocks"))
            foreach (StrictJson r in root.ObjList("rocks"))
            {
                double rad = r.Num("radius_m");
                if (!(rad > 0)) throw new DataError($"{r.Where}.radius_m: above 0");
                rocks.Add(new RockDef(r.Num("x_m"), r.Num("z_m"), rad, r.Has("name") ? r.Str("name") : null));
                r.Done();
            }
        double timeScale = root.Num("time_scale", 1);
        if (!(timeScale > 0 && timeScale <= 1)) throw new DataError($"{root.Where}.time_scale: above 0 and at most 1, not {timeScale}");
        return sc with { Armed = armed, Lethal = lethal, Rocks = rocks, TimeScale = timeScale };
    }
}
