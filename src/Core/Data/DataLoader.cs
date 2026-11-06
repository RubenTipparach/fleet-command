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
    /// <summary>The system packages, each with its scenes file (data/battle/scenes/) and numbers (data/battle/systems/).</summary>
    public static readonly string[] ScenePackages = { "comms", "tractor", "reactor", "cloak", "tactical" };

    /// <summary>The files GameData is built from, relative to data/.</summary>
    public static readonly string[] Files =
    {
        "battle/weapons.json", "battle/classes.json", "battle/factions.json", "battle/scenarios.json", "battle/tuning.json", "console/layout.json",
        "battle/missiles.json", "battle/special_weapons.json", "battle/range.json",
        "battle/scenes/comms.json", "battle/scenes/tractor.json", "battle/scenes/reactor.json", "battle/scenes/cloak.json", "battle/scenes/tactical.json",
        "battle/systems/comms.json", "battle/systems/tractor.json", "battle/systems/reactor.json", "battle/systems/cloak.json", "battle/systems/tactical.json",
    };

    /// <summary>Loads everything; <paramref name="read"/> returns a file's text given its path relative to data/.</summary>
    public static GameData Load(Func<string, string> read)
    {
        StrictJson J(string f) => StrictJson.Parse(read(f), "data/" + f);
        Tuning tuning = LoadTuning(J("battle/tuning.json"));
        var weapons = LoadWeapons(J("battle/weapons.json"));
        MissileData missiles = LoadMissiles(J("battle/missiles.json"));
        SpecialWeapons specials = LoadSpecials(J("battle/special_weapons.json"), weapons);
        var classes = LoadClasses(J("battle/classes.json"), weapons, tuning, missiles);
        foreach (ShipClass c in classes.Values)
            if (c.Mounts.Any(m => m.Weapon == "chorus") && !specials.Chorus.Fits.Contains(c.Id))
                throw new DataError($"data/battle/classes.json.{c.Id}: the chorus lance fits only {string.Join(", ", specials.Chorus.Fits)}");
        var factions = LoadFactions(J("battle/factions.json"));
        var scenarios = LoadScenarios(J("battle/scenarios.json"), classes, factions);
        // each system package's test scenes (data/battle/scenes/<package>.json), after the main list, ids unique across all
        foreach (string pkg in ScenePackages)
        {
            StrictJson sf = J($"battle/scenes/{pkg}.json");
            sf.Str("_doc", "");
            foreach (StrictJson o in sf.ObjList("scenarios"))
            {
                Scenario sc = LoadScenario(o, classes, factions);
                if (scenarios.Any(x => x.Id == sc.Id)) throw new DataError($"{o.Where}.id: '{sc.Id}' used twice");
                scenarios.Add(sc);
            }
            sf.Done();
        }
        LayoutDef layout = LoadLayout(J("console/layout.json"));
        RangeDef range = LoadRange(J("battle/range.json"), weapons);
        return new GameData(weapons, classes, factions, scenarios, tuning, layout, missiles, specials, range) { Systems = SystemsDef.Load(J) };
    }

    /// <summary>The damage range (data/battle/range.json): every program's weapons exist, and its numbers are in range.</summary>
    public static RangeDef LoadRange(StrictJson root, IReadOnlyDictionary<string, WeaponDef> weapons)
    {
        root.Str("_doc", "");
        double from = Positive(root, "from_km");
        var list = new List<RangeProgram>();
        var ids = new HashSet<string>();
        foreach (StrictJson o in root.ObjList("programs"))
        {
            string id = o.Str("id");
            if (!ids.Add(id)) throw new DataError($"{o.Where}.id: '{id}' used twice");
            string[] ws = o.StrList("weapons");
            if (ws.Length == 0) throw new DataError($"{o.Where}.weapons: at least one weapon");
            foreach (string w in ws)
                if (!weapons.TryGetValue(w, out WeaponDef? wd) || wd.IsPointDefence) throw new DataError($"{o.Where}.weapons: no weapon '{w}' that can strike a ship");
            int count = o.Int("count");
            if (count < 1) throw new DataError($"{o.Where}.count: at least 1");
            double? bearing = o.Has("bearing_deg") ? o.Num("bearing_deg") : null;
            double stop = o.Num("stop_below_hull", 0);
            if (stop is < 0 or >= 1) throw new DataError($"{o.Where}.stop_below_hull: 0 (never) up to below 1");
            list.Add(new RangeProgram(id, o.Str("name"), ws, o.Bool("overload", false), Positive(o, "every_s"), count, bearing, stop));
            o.Done();
        }
        if (list.Count == 0) throw new DataError($"{root.Where}.programs: at least one program");
        root.Done();
        return new RangeDef(from, list);
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

    /// <summary>The special weapons (data/battle/special_weapons.json). The chorus, the quell and the forge are added to
    /// weapons as mounts (kinds chorus, quell, slug), so classes can carry them; every number is validated.</summary>
    public static SpecialWeapons LoadSpecials(StrictJson root, Dictionary<string, WeaponDef> weapons)
    {
        void Fac(StrictJson o, string want) { if (o.Str("faction") != want) throw new DataError($"{o.Where}.faction: expected {want}"); o.Str("kind"); o.Str("aim"); }
        StrictJson c = root.Obj("chorus"), q = root.Obj("quell"), f = root.Obj("forge"), h = root.Obj("charge");
        Fac(c, "concord"); Fac(q, "concord"); Fac(f, "krai"); Fac(h, "krai");
        // the chorus: a beam whose damage is its burn; its WeaponMode's damage is what one alone does over the burn
        StrictJson j = c.Obj("join");
        var chorus = new ChorusDef(Positive(c, "burn_s"), Positive(c, "dmg_per_s"), Positive(j, "range_km"), NonNeg(j, "per_joiner_x"), j.Int("max_joiners"), j.Str("spends"), c.Str("strand_color"), c.StrList("fits"));
        j.Done();
        c.Str("fires_from");
        weapons["chorus"] = new WeaponDef("chorus", c.Str("name"), "chorus", new WeaponMode(Positive(c, "range_km"), Positive(c, "charge_s"), NonNeg(c, "cost_e"), chorus.DmgPerS * chorus.BurnS), null, 0, c.Str("color"));
        c.Done();
        q.Str("fits");
        double shieldLeft = NonNeg(q, "shield_left_x");
        if (shieldLeft > 1) throw new DataError($"{q.Where}.shield_left_x: at most 1");
        var quell = new QuellDef(shieldLeft, Positive(q, "stun_s"), Positive(q, "reach_deg"));
        q.Bool("point_defence_hits", true);
        weapons["quell"] = new WeaponDef("quell", q.Str("name"), "quell", new WeaponMode(Positive(q, "range_km"), Positive(q, "charge_s"), NonNeg(q, "cost_e"), NonNeg(q, "dmg")), null, Positive(q, "speed_m_s"), q.Str("color"));
        q.Done();
        f.Str("fits");
        StrictJson fb = f.Obj("burn"), fo = f.Obj("overload");
        var forge = new ForgeDef(NonNeg(f, "pierce_x"), NonNeg(fo, "pierce_x"), NonNeg(fb, "dmg_per_s"), Positive(fb, "s"));
        fb.Done();
        WeaponMode over = new(Positive(fo, "range_km"), Positive(fo, "charge_s"), NonNeg(fo, "cost_e"), NonNeg(fo, "dmg"));
        fo.Done();
        weapons["forge"] = new WeaponDef("forge", f.Str("name"), "slug", new WeaponMode(Positive(f, "range_km"), Positive(f, "charge_s"), NonNeg(f, "cost_e"), NonNeg(f, "dmg")), over, Positive(f, "speed_m_s"), f.Str("color"));
        f.Done();
        h.Str("fits");
        var charge = new ChargeDef(h.Str("name"), "krai", Positive(h, "surge_s"), Positive(h, "speed_x"), Positive(h, "accel_x"), Positive(h, "turn_x"), Positive(h, "bow_shield_x"),
            NonNeg(h, "cost_e"), Positive(h, "cooldown_s"), NonNeg(h, "dmg_per_100_m_s"), Positive(h, "mass_ref_m"), NonNeg(h, "pierce_x"), NonNeg(h, "recoil_x"), h.Str("color"));
        h.Done();
        root.Done();
        return new SpecialWeapons(chorus, quell, forge, charge);
    }

    /// <summary>The missile grades and countermeasures (data/battle/missiles.json): grades unique, each fooled-by chance a
    /// fraction, speeds and fuel above 0.</summary>
    public static MissileData LoadMissiles(StrictJson root)
    {
        StrictJson cm = root.Obj("countermeasures"), f = cm.Obj("flare"), d = cm.Obj("decoy");
        var flare = new FlareDef(f.Int("count"), Positive(f, "burn_s"), Positive(f, "eject_m_s"), Positive(f, "cooldown_s"), Positive(f, "trigger_m"), f.Str("color"));
        if (flare.Count < 1) throw new DataError($"{f.Where}.count: at least one flare a throw");
        f.Done();
        var decoy = new DecoyDef(Positive(d, "speed_m_s"), Positive(d, "life_s"), d.Bool("voided_by_firing", true), Positive(d, "trigger_km"), d.Int("per_ship"), d.Str("color"));
        d.Done();
        cm.Done();
        var grades = new List<MissileGrade>();
        foreach (StrictJson g in root.ObjList("grades"))
        {
            double Frac(string k) { double v = g.Num(k); if (v < 0 || v > 1) throw new DataError($"{g.Where}.{k}: a chance between 0 and 1, got {v}"); return v; }
            var grade = new MissileGrade(g.Str("id"), g.Str("name"), g.Str("color"), Positive(g, "speed_m_s"), NonNeg(g, "damage"), g.Int("pd_hits"),
                Positive(g, "fuel_s"), Frac("fooled_by_flare_x"), Frac("fooled_by_decoy_x"));
            if (grade.PdHits < 1) throw new DataError($"{g.Where}.pd_hits: at least one");
            if (grades.Any(o => o.Id == grade.Id)) throw new DataError($"{g.Where}.id: '{grade.Id}' is listed twice");
            grades.Add(grade);
            g.Done();
        }
        if (grades.Count == 0) throw new DataError($"{root.Where}.grades: no grades");
        root.Done();
        return new MissileData(grades, flare, decoy);
    }

    public static Dictionary<string, ShipClass> LoadClasses(StrictJson root, IReadOnlyDictionary<string, WeaponDef> weapons, Tuning tuning, MissileData missiles)
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
                string? grade = null;
                if (weapons[w].Kind == "missile")
                {
                    grade = m.Str("grade", "orange");
                    if (!missiles.Grades.Any(g => g.Id == grade)) throw new DataError($"{m.Where}.grade: no missile grade '{grade}' in data/battle/missiles.json");
                }
                else if (m.Has("grade")) throw new DataError($"{m.Where}.grade: only a missile rack has a grade");
                mounts.Add(new MountDef(w, m.Num("x"), m.Num("y"), m.Num("arc_centre_deg"), m.Num("arc_width_deg"), grade));
                m.Done();
            }
            int reactor = o.Int("reactor_e");
            if (reactor <= 0) throw new DataError($"{o.Where}.reactor_e: must be above 0");
            string modelPath = o.Str("model", "");
            string? model = modelPath == "" ? null : modelPath;
            if (model != null && !(model.StartsWith("assets/ships/") && model.EndsWith(".glb")))
                throw new DataError($"{o.Where}.model: expected a glb under assets/ships/, got '{model}'");
            d[id] = new ShipClass(id, o.Str("name"), o.Str("code"), shape, model, Positive(o, "length_m"), Positive(o, "hull_hp") * tuning.DemoToughX,
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
            string skin = o.Str("skin", "");
            d[id] = new Faction(id, o.Str("name"), o.Str("hull"), o.Str("trim"), skin == "" ? null : skin);
            o.Done();
        }
        root.Done();
        return d;
    }

    private static readonly Dictionary<string, Stance> Stances = new() { ["attack"] = Stance.Attack, ["escort"] = Stance.Escort, ["hold"] = Stance.Hold, ["run"] = Stance.Run };
    private static readonly Dictionary<string, AiRange> Ranges = new() { ["close"] = AiRange.Close, ["mid"] = AiRange.Mid, ["long"] = AiRange.Long };

    /// <summary>The scenarios (data/battle/scenarios.json), in menu order. Ids are unique, every scenario's player side is a
    /// faction with a console skin, and its first ship is the player's: on side 0 and of that faction.</summary>
    public static List<Scenario> LoadScenarios(StrictJson root, IReadOnlyDictionary<string, ShipClass> classes, IReadOnlyDictionary<string, Faction> factions)
    {
        var list = new List<Scenario>();
        var ids = new HashSet<string>();
        foreach (StrictJson o in root.ObjList("scenarios"))
        {
            Scenario sc = LoadScenario(o, classes, factions);
            if (!ids.Add(sc.Id)) throw new DataError($"{o.Where}.id: '{sc.Id}' used twice");
            list.Add(sc);
        }
        if (list.Count == 0) throw new DataError($"{root.Where}.scenarios: needs at least one scenario");
        root.Done();
        return list;
    }

    public static Scenario LoadScenario(StrictJson root, IReadOnlyDictionary<string, ShipClass> classes, IReadOnlyDictionary<string, Faction> factions)
    {
        string scId = root.Str("id"), side = root.Str("side");
        if (!factions.TryGetValue(side, out Faction? sideFaction)) throw new DataError($"{root.Where}.side: no faction '{side}'");
        if (sideFaction.Skin == null) throw new DataError($"{root.Where}.side: '{side}' has no console skin, so the player cannot command it");
        double seed = root.Num("seed");
        if (seed < 0 || seed != Math.Floor(seed)) throw new DataError($"{root.Where}.seed: expected a whole number at or above 0");
        string objective = root.Str("objective");
        string[] say = root.StrList("say");
        if (say.Length != 2) throw new DataError($"{root.Where}.say: expected [who, text]");
        var ships = new List<ShipStart>();
        var ids = new HashSet<string>();
        foreach (StrictJson s in root.ObjList("ships"))
        {
            string id = s.Str("id"), cls = s.Str("class"), fac = s.Str("faction");
            if (!ids.Add(id)) throw new DataError($"{s.Where}.id: '{id}' used twice");
            if (!classes.ContainsKey(cls)) throw new DataError($"{s.Where}.class: no class '{cls}'");
            if (!factions.ContainsKey(fac)) throw new DataError($"{s.Where}.faction: no faction '{fac}'");
            int sideNo = s.Int("side");
            if (sideNo is not (0 or 1)) throw new DataError($"{s.Where}.side: 0 (the player's) or 1");
            string stance = s.Str("stance", "attack"), range = s.Str("range", "mid");
            if (!Stances.TryGetValue(stance, out Stance st)) throw new DataError($"{s.Where}.stance: attack, escort, hold or run, not '{stance}'");
            if (!Ranges.TryGetValue(range, out AiRange rg)) throw new DataError($"{s.Where}.range: close, mid or long, not '{range}'");
            double hullFrac = s.Num("hull_frac", 1), shieldsFrac = s.Num("shields_frac", 1);
            if (hullFrac is <= 0 or > 1) throw new DataError($"{s.Where}.hull_frac: above 0 and at most 1");
            if (shieldsFrac is < 0 or > 1) throw new DataError($"{s.Where}.shields_frac: between 0 and 1");
            ships.Add(new ShipStart(id, s.Str("name"), s.Str("captain"), cls, fac, sideNo, s.Num("x_m"), s.Num("z_m"), s.Num("heading_deg"), s.Num("speed_m_s"),
                st, rg, s.Bool("erratic", false), s.Bool("hold_fire", false), hullFrac, shieldsFrac));
            ships[^1] = ScenarioExtras.Ship(s, ships[^1]);
            s.Done();
        }
        if (ships.Count == 0 || ships[0].Side != 0) throw new DataError($"{root.Where}.ships: the first ship is the player's and must be on side 0");
        if (ships[0].Faction != side) throw new DataError($"{root.Where}.ships[0].faction: the player's ship must be of the side '{side}', not '{ships[0].Faction}'");
        // the nebula sky it is fought under: a file data/space/skies/<sky>.json (its existence is checked by a test,
        // since the loader reads only data/battle/)
        string sky = root.Str("sky");
        if (!System.Text.RegularExpressions.Regex.IsMatch(sky, "^[a-z][a-z0-9_]*$")) throw new DataError($"{root.Where}.sky: a sky id (lower case and underscores), not '{sky}'");
        // the sun's strength there: deep space by default, more by a warm star
        double sun = root.Num("sun_energy", Scenario.DeepSpaceSun);
        if (!(sun > 0 && sun <= 4)) throw new DataError($"{root.Where}.sun_energy: above 0 and at most 4, not {sun}");
        // a damage range (data/battle/range.json): its ships stand to be fired at, and it never ends
        bool isRange = root.Bool("damage_range", false);
        var scenario = ScenarioExtras.Scene(root, new Scenario(scId, root.Str("name"), side, (ulong)seed, objective, say[0], say[1], ships, sky, sun, isRange));
        root.Done();
        return scenario;
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
            Frac(o, "mount_hit_chance"), Frac(o, "mount_damage"),
            Positive(o, "het_rate_deg_s"), NonNeg(o, "het_cost_e"), Frac(o, "het_chance_drop"), NonNeg(o, "het_recover_per_s"), Frac(o, "het_erratic_penalty"),
            NonNeg(o, "het_breakdown_s"), Frac(o, "het_breakdown_engines"), Positive(o, "estop_decel_x"), NonNeg(o, "estop_hold_s"), Frac(o, "erratic_hit"), Frac(o, "erratic_turn"),
            Positive(o, "orbit_km"), Positive(o, "hold_min_km"), Positive(o, "hold_max_km"), Frac(o, "capture_hull_frac"));
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
