// DataLoader: turns the text of the files in data/ into GameData, validating every field (CLAUDE.md section 6.4). It
// takes file text, not paths, so the game reads through Godot's res:// file access and the tests read from disk, and
// both go through this one loader. A file that fails validation throws a DataError naming its path and field, which
// stops startup.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Sim;

namespace FleetCommand.Data;

public static class DataLoader
{
    /// <summary>The files GameData is built from, relative to data/.</summary>
    public static readonly string[] Files =
    {
        "battle/weapons.json", "battle/classes.json", "battle/factions.json", "battle/skirmish.json", "battle/tuning.json", "console/layout.json",
    };

    /// <summary>Loads everything; <paramref name="read"/> returns a file's text given its path relative to data/.</summary>
    public static GameData Load(Func<string, string> read)
    {
        StrictJson J(string f) => StrictJson.Parse(read(f), "data/" + f);
        Tuning tuning = LoadTuning(J("battle/tuning.json"));
        var weapons = LoadWeapons(J("battle/weapons.json"));
        var classes = LoadClasses(J("battle/classes.json"), weapons, tuning);
        var factions = LoadFactions(J("battle/factions.json"));
        Scenario skirmish = LoadScenario(J("battle/skirmish.json"), classes, factions);
        LayoutDef layout = LoadLayout(J("console/layout.json"));
        return new GameData(weapons, classes, factions, skirmish, tuning, layout);
    }

    private static WeaponMode Mode(StrictJson o) => new(Positive(o, "range_km"), Positive(o, "charge_s"), NonNeg(o, "cost_e"), NonNeg(o, "damage"));

    public static Dictionary<string, WeaponDef> LoadWeapons(StrictJson root)
    {
        var d = new Dictionary<string, WeaponDef>();
        foreach (string id in root.Keys())
        {
            StrictJson o = root.Child(id);
            string kind = o.Str("kind");
            if (kind is not ("beam" or "torpedo" or "bolt" or "missile" or "pd")) throw new DataError($"{o.Where}.kind: unknown kind '{kind}'");
            WeaponMode normal = Mode(o);
            StrictJson? ov = o.ObjOrNull("overload");
            WeaponMode? over = null;
            if (ov != null) { over = Mode(ov); ov.Done(); }
            double speed = o.Num("speed_m_s", 0);
            if (kind is "torpedo" or "bolt" or "missile" && speed <= 0) throw new DataError($"{o.Where}.speed_m_s: a {kind} needs a speed above 0");
            d[id] = new WeaponDef(id, o.Str("name"), kind, normal, over, speed, o.Str("color"));
            o.Done();
        }
        root.Done();
        return d;
    }

    private static readonly string[] Shapes = { "saucer", "wedge", "claw", "station" };

    public static Dictionary<string, ShipClass> LoadClasses(StrictJson root, IReadOnlyDictionary<string, WeaponDef> weapons, Tuning tuning)
    {
        var d = new Dictionary<string, ShipClass>();
        foreach (string id in root.Keys())
        {
            StrictJson o = root.Child(id);
            string shape = o.Str("shape");
            if (!Shapes.Contains(shape)) throw new DataError($"{o.Where}.shape: unknown shape '{shape}'");
            double[] shields = o.NumList("shields_hp");
            if (shields.Length != 6) throw new DataError($"{o.Where}.shields_hp: needs six facings, has {shields.Length}");
            StrictJson p = o.Obj("power_e");
            var power = new Dictionary<string, int>();
            foreach (string sys in Rules.Systems) power[sys] = Math.Clamp(p.Has(sys) ? p.Int(sys) : 0, 0, Rules.BoxesPerSystem);
            p.Done();
            var mounts = new List<MountDef>();
            foreach (StrictJson m in o.ObjList("mounts"))
            {
                string w = m.Str("weapon");
                if (!weapons.ContainsKey(w)) throw new DataError($"{m.Where}.weapon: no weapon '{w}' in data/battle/weapons.json");
                mounts.Add(new MountDef(w, m.Num("x"), m.Num("y"), m.Num("arc_centre_deg"), m.Num("arc_width_deg")));
                m.Done();
            }
            int reactor = o.Int("reactor_e");
            if (reactor <= 0) throw new DataError($"{o.Where}.reactor_e: must be above 0");
            d[id] = new ShipClass(id, o.Str("name"), o.Str("code"), shape, Positive(o, "length_m"), Positive(o, "hull_hp") * tuning.DemoToughX,
                NonNeg(o, "speed_max_m_s"), NonNeg(o, "accel_m_s2"), NonNeg(o, "turn_deg_s"), reactor, NonNeg(o, "capacitor_e"), NonNeg(o, "battery_e"),
                shields, power, mounts);
            o.Done();
        }
        root.Done();
        return d;
    }

    public static Dictionary<string, Faction> LoadFactions(StrictJson root)
    {
        var d = new Dictionary<string, Faction>();
        foreach (string id in root.Keys())
        {
            StrictJson o = root.Child(id);
            d[id] = new Faction(id, o.Str("name"), o.Str("hull"), o.Str("trim"));
            o.Done();
        }
        root.Done();
        return d;
    }

    public static Scenario LoadScenario(StrictJson root, IReadOnlyDictionary<string, ShipClass> classes, IReadOnlyDictionary<string, Faction> factions)
    {
        double seed = root.Num("seed");
        if (seed < 0 || seed != Math.Floor(seed)) throw new DataError($"{root.Where}.seed: expected a whole number at or above 0");
        var ships = new List<ShipStart>();
        var ids = new HashSet<string>();
        foreach (StrictJson s in root.ObjList("ships"))
        {
            string id = s.Str("id"), cls = s.Str("class"), fac = s.Str("faction");
            if (!ids.Add(id)) throw new DataError($"{s.Where}.id: '{id}' used twice");
            if (!classes.ContainsKey(cls)) throw new DataError($"{s.Where}.class: no class '{cls}'");
            if (!factions.ContainsKey(fac)) throw new DataError($"{s.Where}.faction: no faction '{fac}'");
            int side = s.Int("side");
            if (side is not (0 or 1)) throw new DataError($"{s.Where}.side: 0 (the player's) or 1");
            ships.Add(new ShipStart(id, s.Str("name"), s.Str("captain"), cls, fac, side, s.Num("x_m"), s.Num("z_m"), s.Num("heading_deg"), s.Num("speed_m_s")));
            s.Done();
        }
        if (ships.Count == 0 || ships[0].Side != 0) throw new DataError($"{root.Where}.ships: the first ship is the player's and must be on side 0");
        root.Done();
        return new Scenario((ulong)seed, ships);
    }

    public static Tuning LoadTuning(StrictJson o)
    {
        var t = new Tuning(
            Positive(o, "step_s"), Positive(o, "speed_per_box_m_s"), NonNeg(o, "reverse_max_m_s"), Positive(o, "demo_tough_x"),
            Frac(o, "hit_chance_near"), NonNeg(o, "hit_chance_falloff"), Frac(o, "hit_chance_min"), Frac(o, "hit_chance_max"), NonNeg(o, "hit_speed_penalty_per_300_m_s"), NonNeg(o, "ecm_step"),
            NonNeg(o, "shield_regen_per_e_s"), NonNeg(o, "battery_charge_per_spare_e_s"), Frac(o, "start_capacitor_frac"), Frac(o, "start_battery_frac"),
            NonNeg(o, "repair_per_e_s"), NonNeg(o, "mount_repair_per_e_s"), Positive(o, "ai_think_s"), Frac(o, "ai_fire_min_chance"),
            Positive(o, "ai_range_close_km"), Positive(o, "ai_range_mid_km"), Positive(o, "ai_range_long_km"), Positive(o, "shot_hit_radius_m"), Positive(o, "beam_life_s"),
            Frac(o, "pd_intercept_chance"), Frac(o, "status_disabled_below"), Frac(o, "subsystem_hit_chance"), NonNeg(o, "subsystem_damage_per_hp"),
            Frac(o, "mount_hit_chance"), Frac(o, "mount_damage"));
        o.Done();
        return t;
    }

    public static LayoutDef LoadLayout(StrictJson o)
    {
        Rect R(string k)
        {
            double[] a = o.NumList(k);
            if (a.Length != 4 || a[2] <= 0 || a[3] <= 0) throw new DataError($"{o.Where}.{k}: expected [x, y, w, h] with w and h above 0");
            return new Rect(a[0], a[1], a[2], a[3]);
        }
        var l = new LayoutDef(Positive(o, "reference_w_px"), Positive(o, "reference_h_px"), Positive(o, "min_w_px"), Positive(o, "column_w_px"), Positive(o, "strip_h_px"),
            R("plate"), R("station"), R("ship"), R("weapons"), R("radar"),
            NonNeg(o, "expanded_x_px"), NonNeg(o, "expanded_y_px"), Frac(o, "expanded_share"), Frac(o, "ship_screen_y"), Positive(o, "view_fov_deg"));
        o.Done();
        return l;
    }

    private static double Positive(StrictJson o, string k) { double v = o.Num(k); if (v <= 0) throw new DataError($"{o.Where}.{k}: must be above 0"); return v; }
    private static double NonNeg(StrictJson o, string k) { double v = o.Num(k); if (v < 0) throw new DataError($"{o.Where}.{k}: must be 0 or more"); return v; }
    private static double Frac(StrictJson o, string k) { double v = o.Num(k); if (v < 0 || v > 1) throw new DataError($"{o.Where}.{k}: must be between 0 and 1"); return v; }
}
