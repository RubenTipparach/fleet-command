// SystemsDef.Tractor: the tractor-and-craft package's numbers (the tractor beam's contest, mines, asteroids, fighters,
// disguised shuttles and the missile barrage), read from data/battle/systems/tractor.json, strictly (CLAUDE.md 6.4: units
// in the keys, every number validated, an unknown key an error). It lives beside the other packages' parts of SystemsDef
// so each package reads only its own file. The rules that use these numbers are Battle.Tractor, Battle.Mines and
// Battle.Craft.
using System.Collections.Generic;

namespace FleetCommand.Data;

/// <summary>The tractor contest (design 4a): reach, the power-up, the start grip, the round, the grip a point of
/// difference moves, when the beam turns, the damping and the tether, and the AI crew's pick.</summary>
public sealed record TractorDef(double ReachKm, double ReachSlackKm, double PowerupS, double GripStartX, double RoundS, double StepPerE,
    double ReverseRatioX, double HoldDampPerS, double RelockS, double ReelMS, double ReelStepM, double MinTetherM, int AiPickMinE, int AiPickMaxE);

/// <summary>Mines: the wait between two, the time to arm, how near an enemy sets one off, its damage, how far astern one
/// is dropped and how far the transporter places one.</summary>
public sealed record MineDef(double CooldownS, double ArmS, double TriggerM, double DmgHp, double DropAsternM, double TransporterReachKm);

/// <summary>Asteroids: a collision's damage and its speed scaling, the speed kept, the cool-down between two, and the
/// micrometeor cloud round each rock; a ship's collision radius as a share of its length.</summary>
public sealed record RockRules(double DmgHp, double SpeedRefMS, double SpeedFloorX, double SpeedKeepX, double CooldownS, double HaloX,
    double GrindDps, double GrindFloorX, double GritWholeHp, double GrindAheadM, double HullRadiusX);

/// <summary>Fighters: their speed, their circle round what they are after, their strikes, Defend's shooting and recovery.</summary>
public sealed record FighterDef(double SpeedMS, double OrbitM, double OrbitRadS, double RecoverM, double StrikeM, double StrikeEveryS, double StrikeHitX,
    double StrikeDmgHp, double StrafeMountHp, double DefendReachM, double DefendKillX, double LaunchWaitS, double LaunchStaggerS);

/// <summary>A disguised shuttle: its speed, its charge (times the demo's toughness), where the target's crew may see
/// through it and how often, and what it leaves of one subsystem.</summary>
public sealed record ShuttleDef(double SpeedMS, double DmgHp, double SuspectKm, double CatchPerS, double SysLeftX);

/// <summary>A missile barrage: missiles a salvo, the interval, the chance each hits, their grade, the racks' offset and
/// a missile's life.</summary>
public sealed record BarrageDef(int Salvo, double IntervalS, double HitX, string Grade, double SideOffsetM, double LifeS);

/// <summary>What a class carries into battle: mines, fighters, disguised shuttles and barrage missiles.</summary>
public sealed record Loadout(int Mines, int Fighters, int Shuttles, int Missiles)
{
    public static readonly Loadout None = new(0, 0, 0, 0);
}

public sealed partial class SystemsDef
{
    public TractorDef Tractor { get; private set; } = new(3, 0.2, 10, 0.5, 10, 0.1, 0.5, 0.8, 10, 150, 200, 300, 1, 6);
    public MineDef Mines { get; private set; } = new(3, 3, 1200, 30, 300, 1.5);
    public RockRules Rocks { get; private set; } = new(14, 150, 0.35, 0.3, 1.5, 3, 6, 0.2, 2, 500, 0.35);
    public FighterDef Fighters { get; private set; } = new(900, 400, 1.4, 100, 300, 2, 0.5, 1.5, 0.25, 600, 0.3, 1, 0.5);
    public ShuttleDef Shuttle { get; private set; } = new(500, 110, 1.5, 0.25, 0.4);
    public BarrageDef Barrage { get; private set; } = new(6, 0.2, 0.85, "orange", 100, 30);
    /// <summary>How far past what it runs at a ship on a lane run turns back for another pass, km.</summary>
    public double LaneTurnKm { get; private set; } = 12;
    /// <summary>What a lethal tool deals past the target's hull, hit points.</summary>
    public double LethalOverkillHp { get; private set; } = 400;
    /// <summary>A lethal barrage missile's damage as a share of its target class's hull.</summary>
    public double LethalHullShareX { get; private set; } = 1.0 / 3;
    private readonly Dictionary<string, Loadout> _loadouts = new();
    /// <summary>Each class's loadout by class id (a class not listed carries none).</summary>
    public IReadOnlyDictionary<string, Loadout> Loadouts => _loadouts;

    /// <summary>A class's loadout, or none.</summary>
    public Loadout LoadoutOf(string classId) => _loadouts.GetValueOrDefault(classId) ?? Loadout.None;

    partial void LoadTractor(StrictJson o)
    {
        static double Pos(StrictJson j, string k) { double v = j.Num(k); if (!(v > 0)) throw new DataError($"{j.Where}.{k}: must be above 0, got {v}"); return v; }
        static double Non(StrictJson j, string k) { double v = j.Num(k); if (v < 0) throw new DataError($"{j.Where}.{k}: must be 0 or more, got {v}"); return v; }
        static double Frac(StrictJson j, string k) { double v = j.Num(k); if (v < 0 || v > 1) throw new DataError($"{j.Where}.{k}: between 0 and 1, got {v}"); return v; }
        static int Count(StrictJson j, string k, int fallback) { int v = j.Has(k) ? j.Int(k) : fallback; if (v < 0) throw new DataError($"{j.Where}.{k}: 0 or more, got {v}"); return v; }

        StrictJson t = o.Obj("tractor");
        int pickMin = t.Int("ai_pick_min_e"), pickMax = t.Int("ai_pick_max_e");
        if (pickMin < 0 || pickMax < pickMin) throw new DataError($"{t.Where}.ai_pick_max_e: at least ai_pick_min_e, and both 0 or more");
        Tractor = new TractorDef(Pos(t, "reach_km"), Non(t, "reach_slack_km"), Pos(t, "powerup_s"), Frac(t, "grip_start_x"), Pos(t, "round_s"), Pos(t, "step_per_e"),
            Frac(t, "reverse_ratio_x"), Non(t, "hold_damp_per_s"), Non(t, "relock_s"), Non(t, "reel_m_s"), Pos(t, "reel_step_m"), Pos(t, "min_tether_m"), pickMin, pickMax);
        t.Done();
        StrictJson m = o.Obj("mines");
        Mines = new MineDef(Pos(m, "cooldown_s"), Non(m, "arm_s"), Pos(m, "trigger_m"), Non(m, "dmg_hp"), Non(m, "drop_astern_m"), Pos(m, "transporter_reach_km"));
        m.Done();
        StrictJson r = o.Obj("rocks");
        Rocks = new RockRules(Non(r, "dmg_hp"), Pos(r, "speed_ref_m_s"), Frac(r, "speed_floor_x"), Frac(r, "speed_keep_x"), Pos(r, "cooldown_s"), Pos(r, "halo_x"),
            Non(r, "grind_dps"), Frac(r, "grind_floor_x"), Pos(r, "grit_whole_hp"), Non(r, "grind_ahead_m"), Pos(r, "hull_radius_x"));
        if (Rocks.HaloX <= 1) throw new DataError($"{r.Where}.halo_x: the cloud reaches past the rock, so above 1");
        r.Done();
        StrictJson f = o.Obj("fighters");
        Fighters = new FighterDef(Pos(f, "speed_m_s"), Non(f, "orbit_m"), Non(f, "orbit_rad_s"), Pos(f, "recover_m"), Pos(f, "strike_m"), Pos(f, "strike_every_s"), Frac(f, "strike_hit_x"),
            Non(f, "strike_dmg_hp"), Frac(f, "strafe_mount_hp"), Pos(f, "defend_reach_m"), Frac(f, "defend_kill_x"), Non(f, "launch_wait_s"), Non(f, "launch_stagger_s"));
        f.Done();
        StrictJson s = o.Obj("shuttle");
        Shuttle = new ShuttleDef(Pos(s, "speed_m_s"), Non(s, "dmg_hp"), Non(s, "suspect_km"), Non(s, "catch_per_s"), Frac(s, "sys_left_x"));
        s.Done();
        StrictJson b = o.Obj("barrage");
        int salvo = b.Int("salvo");
        if (salvo < 1) throw new DataError($"{b.Where}.salvo: at least one missile");
        Barrage = new BarrageDef(salvo, Pos(b, "interval_s"), Frac(b, "hit_x"), b.Str("grade"), Non(b, "side_offset_m"), Pos(b, "life_s"));
        b.Done();
        LaneTurnKm = Pos(o, "lane_turn_km");
        LethalOverkillHp = Non(o, "lethal_overkill_hp");
        LethalHullShareX = Frac(o, "lethal_hull_share_x");
        StrictJson l = o.Obj("loadouts");
        foreach (string cls in l.Keys())
        {
            StrictJson c = l.Child(cls);
            _loadouts[cls] = new Loadout(Count(c, "mines", 0), Count(c, "fighters", 0), Count(c, "shuttles", 0), Count(c, "missiles", 0));
            c.Done();
        }
        l.Done();
        o.Done();
    }
}
