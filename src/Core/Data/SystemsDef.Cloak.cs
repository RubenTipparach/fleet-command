// SystemsDef.Cloak: the cloak-and-sensors package's numbers (data/battle/systems/cloak.json): nebulae, the cloak and its
// sweep, the cloaking captain, the sensors' ECM, ECCM and scan, and boarding defence with each class's deck plan. They
// are the battle console mockup's NEBULA, CLOAK, AI_CLOAK, SECURITY and ROOM_SYSTEM (docs/mockups/battle-console.html),
// read strictly here so a typo stops startup naming its path (CLAUDE.md 6.4). The deck plans themselves are the Ship
// Design Bible's room files under data/ships/, read once and turned into a walkable graph (DeckPlan).
using System;
using System.Collections.Generic;
using System.Linq;

namespace FleetCommand.Data;

/// <summary>Inside a nebula plume (openspec/changes/nebula-plumes; owner, 2026-11-10: "entering a column of nebula plume
/// can hide you from enemies. weapons have difficulty targetting, missiles dont quite work, energy weapons 1/2 range",
/// "and 1/2 acuracy to hit"). At full density a shot from or at a ship in one keeps HitX of its chance to hit, a weapon of
/// one of EnergyKinds keeps EnergyRangeX of its range, and a missile after a ship in one (or flying through one) loses
/// its target with a chance of MissileLostPerS a second; at a lesser density each is that much nearer to no effect. ECM
/// and ECCM count EwGainX x density more, and a ship cannot lock another through a plume (any plume on the sight line
/// between them, Rules.NebulaOnLine; owner, 2026-11-10: "ships whose targeting raycasts goes through a cloud looses target
/// locks") from beyond LockKm unless its ECCM beats the other's ECM. The density thins by EdgeThinFrac from the middle to the edge.
/// <para>An ion storm is a plume with lightning (owner, 2026-11-10: "some nebulas are ion storms, which will have cool
/// lightning", "which can strike ships, and cause a temprorary shut down of the reactor core, the ship loosing power for a
/// few seconds completely"). A ship in one is struck StormStrikesPerMin times a minute at full density; a strike trips its
/// core for StormShutdownS seconds, and no bolt finds it again for StormGraceS seconds after the core restarts.</para></summary>
public sealed record NebulaRules(double HitX, double EnergyRangeX, IReadOnlySet<string> EnergyKinds, double MissileLostPerS, double EwGainX, double LockKm, double EdgeThinFrac,
    double StormStrikesPerMin, double StormShutdownS, double StormGraceS);

/// <summary>A nebula plume in a scenario: its middle and radius on the battle plane (metres), its density at the middle, and
/// which of <see cref="Looks"/> the view draws it in (data/space/plumes.json), whether it is an ion storm (lightning that
/// trips a ship's core, NebulaRules), and which of <see cref="Forms"/> its cloud takes in the view (the rules test only its
/// circle on the plane, which every form covers).</summary>
public sealed record NebulaDef(double XM, double ZM, double RM, double Density, string Look = "violet", bool Storm = false, string Form = "column")
{
    /// <summary>The forms a plume's cloud takes (owner, 2026-11-10: "some nebula clouds can just be scattered puffs, or
    /// columns that dont quite connect, to give it a more varied look"): a column from the deck below into the deck above,
    /// one rising from the deck below to a head short of the deck above, one hanging from the deck above to a foot short of
    /// the deck below, and a puff joined to neither.</summary>
    public static readonly string[] Forms = { "column", "rising", "hanging", "puff" };
    /// <summary>Every look a plume may have; data/space/plumes.json holds one for each and no other. The last two are the
    /// storm heads: a storm with no look named takes the first of them.</summary>
    public static readonly string[] Looks = { "violet", "garnet", "brass", "teal", "orchid", "slate", "cinder" };
    /// <summary>The look of a storm that names none.</summary>
    public const string StormLook = "slate";
}

/// <summary>The cloak and the sweep (the mockup's CLOAK; openspec/changes/cloaking).</summary>
public sealed record CloakRules(double CostE, double FadeS, double DetectKm, double SweepE, double BaseKm, double PerEccmKm, double OfficerSweepKm,
    double RevealS, double SweepCoolS, double HitPenalty, double OwnAlpha, double SweepFxS, IReadOnlySet<string> Fitted);

/// <summary>A cloaking captain's strike (the mockup's AI_CLOAK).</summary>
public sealed record AiCloakRules(double StrikeKm, double FightS, double ChargedFrac);

/// <summary>ECM, ECCM and the scan (the mockup's Science station and systemsStep).</summary>
public sealed record SensorRules(int EwCells, double ScanBasePerS, double ScanPerES, double PassiveScanX, double PassiveHit, double OfficerScan);

/// <summary>Boarding defence (the mockup's SECURITY and ROOM_SYSTEM; openspec/changes/boarding-defence).</summary>
public sealed record SecurityRules(double WalkMS, int Team, double EngageM, double VolleyS, double HitChance, double OfficerAboard, double OfficerAmbush,
    double AmbushS, double AmbushBonus, double BoardEveryS, int BoardSquad, double BoardRangeKm, double ShieldDownHp, double SabotageS, double SabotageDmg,
    double CaptureS, IReadOnlyDictionary<string, string> RoomSystem, IReadOnlyDictionary<string, int> Marines);

/// <summary>Everything the package reads, together.</summary>
public sealed record CloakSystems(NebulaRules Nebula, CloakRules Cloak, AiCloakRules AiCloak, SensorRules Sensors, SecurityRules Security,
    IReadOnlyDictionary<string, DeckPlan> Plans, IReadOnlyDictionary<string, string> PlanByClass)
{
    /// <summary>A class's deck plan, or null for a class with none.</summary>
    public DeckPlan? PlanFor(string classId) => PlanByClass.TryGetValue(classId, out string? p) ? Plans[p] : null;
}

public sealed partial class SystemsDef
{
    private CloakSystems? _cloak;
    private StrictJson? _plansJson;

    /// <summary>The cloak-and-sensors package's numbers; throws when data/battle/systems/cloak.json was never loaded.</summary>
    public CloakSystems Cloak => _cloak ?? throw new InvalidOperationException("data/battle/systems/cloak.json was not loaded");

    private static readonly string[] RoomSystems = { "bridge", "reactor", "engines", "shields", "sensors", "weapons", "tractor", "transport" };

    partial void LoadCloak(StrictJson o)
    {
        double Pos(StrictJson j, string k) { double v = j.Num(k); if (v <= 0) throw new DataError($"{j.Where}.{k}: must be above 0"); return v; }
        double Non(StrictJson j, string k) { double v = j.Num(k); if (v < 0) throw new DataError($"{j.Where}.{k}: must be 0 or more"); return v; }
        double Frac(StrictJson j, string k) { double v = j.Num(k); if (v < 0 || v > 1) throw new DataError($"{j.Where}.{k}: must be between 0 and 1"); return v; }
        int Whole(StrictJson j, string k) { int v = j.Int(k); if (v < 1) throw new DataError($"{j.Where}.{k}: at least 1"); return v; }

        StrictJson n = o.Obj("nebula");
        var nebula = new NebulaRules(Frac(n, "hit_x"), Frac(n, "energy_range_x"), new HashSet<string>(n.StrList("energy_kinds")), Non(n, "missile_lost_per_s"), Non(n, "ew_gain_x"), Pos(n, "lock_km"),
            Frac(n, "edge_thin_frac"), Non(n, "storm_strikes_per_min"), Pos(n, "storm_shutdown_s"), Non(n, "storm_grace_s"));
        n.Done();

        StrictJson c = o.Obj("cloak");
        var cloak = new CloakRules(Non(c, "cost_e"), Pos(c, "fade_s"), Non(c, "detect_km"), Non(c, "sweep_e"), Non(c, "base_km"), Non(c, "per_eccm_km"), Non(c, "officer_sweep_km"),
            Pos(c, "reveal_s"), Non(c, "sweep_cool_s"), Frac(c, "hit_penalty"), Frac(c, "own_alpha"), Pos(c, "sweep_fx_s"), new HashSet<string>(c.StrList("fitted")));
        c.Done();

        StrictJson a = o.Obj("ai_cloak");
        var ai = new AiCloakRules(Pos(a, "strike_km"), Non(a, "fight_s"), Frac(a, "charged_frac"));
        a.Done();

        StrictJson s = o.Obj("sensors");
        var sensors = new SensorRules(Whole(s, "ew_cells"), Non(s, "scan_base_per_s"), Non(s, "scan_per_e_s"), Frac(s, "passive_scan_x"), Frac(s, "passive_hit"), Non(s, "officer_scan"));
        s.Done();

        StrictJson q = o.Obj("security");
        StrictJson rs = q.Obj("room_system");
        var roomSystem = new Dictionary<string, string>();
        foreach (string k in rs.Keys())
        {
            string v = rs.Str(k);
            if (!RoomSystems.Contains(v)) throw new DataError($"{rs.Where}.{k}: '{v}' is not one of {string.Join(", ", RoomSystems)}");
            roomSystem[k] = v;
        }
        rs.Done();
        StrictJson mj = q.Obj("marines");
        var marines = new Dictionary<string, int>();
        foreach (string k in mj.Keys()) { int v = mj.Int(k); if (v < 0) throw new DataError($"{mj.Where}.{k}: must be 0 or more"); marines[k] = v; }
        mj.Done();
        var security = new SecurityRules(Pos(q, "walk_m_s"), Whole(q, "team"), Pos(q, "engage_m"), Pos(q, "volley_s"), Frac(q, "hit_chance"), Non(q, "officer_aboard"), Non(q, "officer_ambush"),
            Non(q, "ambush_s"), Frac(q, "ambush_bonus"), Pos(q, "board_every_s"), Whole(q, "board_squad"), Pos(q, "board_range_km"), Non(q, "shield_down_hp"), Pos(q, "sabotage_s"),
            Frac(q, "sabotage_dmg"), Pos(q, "capture_s"), roomSystem, marines);
        q.Done();

        // the deck plans are read once every package's file is in (LoadCloakPlans), from the files they name
        _plansJson = o.Obj("deck_plans");
        o.Done();
        _cloak = new CloakSystems(nebula, cloak, ai, sensors, security, new Dictionary<string, DeckPlan>(), new Dictionary<string, string>());
    }

    // The deck plans of cloak.json's deck_plans, each from its own file under data/, and which class uses which.
    partial void LoadCloakPlans(Func<string, StrictJson> json)
    {
        if (_plansJson is not { } d || _cloak is not { } cs) return;
        StrictJson ps = d.Obj("plans");
        var plans = new Dictionary<string, DeckPlan>();
        foreach (string id in ps.Keys())
        {
            StrictJson p = ps.Child(id);
            string file = p.Str("file");
            DeckPlan plan = DeckPlan.Load(id, json(file), p.StrList("posts"), p.StrList("goals"), p.Str("pad_room"), p.Where);
            plans[id] = plan;
            p.Done();
        }
        ps.Done();
        StrictJson cj = d.Obj("classes");
        var byClass = new Dictionary<string, string>();
        foreach (string k in cj.Keys())
        {
            string v = cj.Str(k);
            if (!plans.ContainsKey(v)) throw new DataError($"{cj.Where}.{k}: no deck plan '{v}' in deck_plans.plans");
            byClass[k] = v;
        }
        cj.Done();
        d.Done();
        _cloak = cs with { Plans = plans, PlanByClass = byClass };
        _plansJson = null;
    }
}
