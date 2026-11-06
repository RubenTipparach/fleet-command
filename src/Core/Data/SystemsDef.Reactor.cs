// SystemsDef.Reactor: the numbers of the reactor, the warp drive, damage control and the officers (the battle console
// mockup's REACTOR, WARP, REPAIR, REPAIR_JOBS, OFFICER and ROSTER), read from data/battle/systems/reactor.json. This
// package's part of SystemsDef lives in its own file so the other packages never edit its lines. Every number has the
// mockup's value as its code default (CLAUDE.md 6.4: a missing field takes the code default, a present zero is zero, an
// unknown key is an error); the lists (the repair jobs, the queue, the roster) are required.
using System;
using System.Collections.Generic;
using System.Linq;

namespace FleetCommand.Data;

/// <summary>What a hit through the shields may do to the reactor and the warp drive (chances and amounts).</summary>
public sealed record ReactorHitDef
{
    public double CoilP { get; init; } = 0.3;
    public double CoilKnock { get; init; } = 0.8;
    public double ConfPerDmg { get; init; } = 0.012;
    public double LoopP { get; init; } = 0.18;
    public double LoopDmg { get; init; } = 0.35;
    public double LoopDmgRand { get; init; } = 0.35;
    public double NacelleP { get; init; } = 0.15;
    public double NacelleDmg { get; init; } = 0.25;
}

/// <summary>The reactor (openspec/changes/reactor): coils, coolant loops, heat and meltdown. Temperatures in percent of
/// the core's limit; energy in E.</summary>
public sealed record ReactorDef
{
    public int Coils { get; init; } = 3;
    public int Loops { get; init; } = 2;
    public int FlowMax { get; init; } = 5;
    public int FlowStart { get; init; } = 1;
    /// <summary>What one point of a loop's flow draws from the reactor, whole E.</summary>
    public int FlowE { get; init; } = 1;
    public double EfficiencyLoss { get; init; } = 0.6;
    public double HeatFullPctS { get; init; } = 4;
    public double HeatMisalignPctS { get; init; } = 12;
    public double CoolPerFlow { get; init; } = 0.04;
    public double TempFloorPct { get; init; } = 20;
    public double TempStartPct { get; init; } = 50;
    /// <summary>The core temperature at which the reactor's stability starts to fall, to 0 at CriticalPct.</summary>
    public double StablePct { get; init; } = 50;
    public double CriticalPct { get; init; } = 100;
    /// <summary>Cooling (flow times loop health) at or below this counts as coolant offline.</summary>
    public double CoolingOfflineBelow { get; init; } = 0.01;
    public double MeltdownS { get; init; } = 20;
    /// <summary>How fast the meltdown count runs back once the core is safe, a share of a second a second.</summary>
    public double MeltRecoverX { get; init; } = 0.5;
    public double DriftPerS { get; init; } = 0.05;
    /// <summary>The drift's scale per step (the mockup's walk: drift_per_s x (1 - confinement) x dt x drift_x).</summary>
    public double DriftX { get; init; } = 10;
    public double Nudge { get; init; } = 0.05;
    /// <summary>The crew moves flow off a loop whose health is below this.</summary>
    public double CrewLoopBelow { get; init; } = 0.3;
    public double BlastKm { get; init; } = 3;
    public double BlastDmg { get; init; } = 45;
    public ReactorHitDef Hit { get; init; } = new();
}

/// <summary>The weights of the jump risk's factors.</summary>
public sealed record WarpRiskDef
{
    public double Nacelles { get; init; } = 0.8;
    /// <summary>The nacelles' risk when one is out, whatever the rest.</summary>
    public double NacelleOut { get; init; } = 0.95;
    public double Coolant { get; init; } = 1.2;
    public double CoolantOk { get; init; } = 0.5;
    public double Banks { get; init; } = 0.3;
    public double Nav { get; init; } = 0.6;
    public double Reactor { get; init; } = 0.7;
    public double Qg { get; init; } = 0.3;
    public double QgOverride { get; init; } = 0.9;
}

/// <summary>A failed jump's harm, scaled by the risk and a severity roll.</summary>
public sealed record WarpMishapDef
{
    public double Nacelle { get; init; } = 0.2;
    public double NacelleSev { get; init; } = 0.4;
    public double NacelleX { get; init; } = 1.5;
    public double CoolantSev { get; init; } = 0.3;
    public double HullDmg { get; init; } = 10;
    public double HullDmgSev { get; init; } = 30;
    public double BreachRisk { get; init; } = 0.8;
    public double BreachSev { get; init; } = 0.6;
}

/// <summary>The warp drive (openspec/changes/warp): nacelles, banks, the course plot and the jump.</summary>
public sealed record WarpDef
{
    public int Nacelles { get; init; } = 2;
    public int CoilE { get; init; } = 8;
    public double BankE { get; init; } = 60;
    public double JumpE { get; init; } = 40;
    public double ChargeK { get; init; } = 0.5;
    public double PlotS { get; init; } = 20;
    public double CoolantJump { get; init; } = 0.25;
    public double LeakPerS { get; init; } = 0.01;
    public double QgBlock { get; init; } = 0.5;
    public double OutS { get; init; } = 1.2;
    public double OutSpeedMS { get; init; } = 6000;
    public WarpRiskDef Risk { get; init; } = new();
    public WarpMishapDef Mishap { get; init; } = new();
}

/// <summary>A repair job: its id (a subsystem, or reactor, warp, mounts), its name, its icon and the short name the docked
/// Repair page uses.</summary>
public sealed record RepairJob(string Id, string Name, string Icon, string Short);

/// <summary>Damage control (openspec/changes/damage-control): the crews' cap on one job, the jobs and the starting queue.</summary>
public sealed record RepairDef
{
    public int MaxCrews { get; init; } = 3;
    public IReadOnlyList<RepairJob> Jobs { get; init; } = Array.Empty<RepairJob>();
    public IReadOnlyList<string> Queue { get; init; } = Array.Empty<string>();

    /// <summary>A job by id; throws naming it when there is none.</summary>
    public RepairJob Job(string id) => Jobs.FirstOrDefault(j => j.Id == id) ?? throw new KeyNotFoundException($"no repair job '{id}' in data/battle/systems/reactor.json");
}

/// <summary>Each station officer's ability per skill level, each read by its own rule.</summary>
public sealed record OfficerAbility
{
    public double Het { get; init; } = 0.04;
    public double Drift { get; init; } = 0.15;
    public double Hit { get; init; } = 0.02;
    public double Pd { get; init; } = 0.05;
    public double Scan { get; init; } = 0.25;
    public double SweepKm { get; init; } = 1;
    public double Jam { get; init; } = 0.25;
    public double RepairRate { get; init; } = 0.2;
    public double Ambush { get; init; } = 0.1;
    public double Aboard { get; init; } = 0.04;
    public double PadRate { get; init; } = 0.1;
    public double WingHit { get; init; } = 0.015;
}

/// <summary>One officer of the roster: id, name, rank (0-3), specialty (a station id) and skill (1-4).</summary>
public sealed record RosterOfficer(string Id, string Name, int Rank, string Spec, int Skill);

/// <summary>A faction's own rank names and officers' names (by roster id).</summary>
public sealed record OfficerNames(IReadOnlyList<string> Ranks, IReadOnlyDictionary<string, string> Names);

/// <summary>The officers (openspec/changes/officers).</summary>
public sealed record OfficersDef
{
    /// <summary>The stations, each with one officer's slot (the stations panel's order).</summary>
    public static readonly string[] Stations = { "helm", "power", "tactical", "pd", "science", "comms", "repair", "transport", "fleet" };
    public OfficerAbility Ability { get; init; } = new();
    public IReadOnlyList<string> Skills { get; init; } = new[] { "Untrained", "Green", "Seasoned", "Veteran", "Legend" };
    public IReadOnlyList<string> Ranks { get; init; } = new[] { "Ensign", "Lieutenant", "Lt. Commander", "Commander" };
    public IReadOnlyList<string> RanksShort { get; init; } = new[] { "Ens.", "Lt.", "Lt. Cdr.", "Cdr." };
    public int NameShift { get; init; } = 5;
    public IReadOnlyList<RosterOfficer> Roster { get; init; } = Array.Empty<RosterOfficer>();
    /// <summary>Rank names and officers' names by faction id; a faction not listed uses the roster's own.</summary>
    public IReadOnlyDictionary<string, OfficerNames> Factions { get; init; } = new Dictionary<string, OfficerNames>();

    /// <summary>A rank's name for a faction, full or short.</summary>
    public string RankName(string faction, int rank, bool brief = false)
    {
        IReadOnlyList<string> list = Factions.TryGetValue(faction, out OfficerNames? n) ? n.Ranks : brief ? RanksShort : Ranks;
        return list[Math.Clamp(rank, 0, list.Count - 1)];
    }
}

public sealed partial class SystemsDef
{
    public ReactorDef Reactor { get; private set; } = new();
    public WarpDef Warp { get; private set; } = new();
    public RepairDef Repair { get; private set; } = new();
    public OfficersDef Officers { get; private set; } = new();

    partial void LoadReactor(StrictJson o)
    {
        o.Str("_doc", "");
        if (o.ObjOrNull("reactor") is { } r) { Reactor = ReadReactor(r); r.Done(); }
        if (o.ObjOrNull("warp") is { } w) { Warp = ReadWarp(w); w.Done(); }
        StrictJson rp = o.Obj("repair");
        Repair = ReadRepair(rp);
        rp.Done();
        StrictJson of = o.Obj("officers");
        Officers = ReadOfficers(of);
        of.Done();
        o.Done();
    }

    // ---- readers: each number in its range, the code default when absent ------------------------------------------

    private static double Pos(StrictJson o, string k, double d) { double v = o.Num(k, d); if (v <= 0) throw new DataError($"{o.Where}.{k}: must be above 0"); return v; }
    private static double Non(StrictJson o, string k, double d) { double v = o.Num(k, d); if (v < 0) throw new DataError($"{o.Where}.{k}: must be 0 or more"); return v; }
    private static double Share(StrictJson o, string k, double d) { double v = o.Num(k, d); if (v is < 0 or > 1) throw new DataError($"{o.Where}.{k}: must be between 0 and 1"); return v; }
    private static int Whole(StrictJson o, string k, int d, int min)
    {
        int v = o.Has(k) ? o.Int(k) : d;
        if (v < min) throw new DataError($"{o.Where}.{k}: at least {min}");
        return v;
    }

    private static ReactorDef ReadReactor(StrictJson o)
    {
        var d = new ReactorDef();
        o.Str("_doc", "");
        ReactorHitDef hit = d.Hit;
        if (o.ObjOrNull("hit") is { } h)
        {
            h.Str("_doc", "");
            hit = new ReactorHitDef
            {
                CoilP = Share(h, "coil_p", hit.CoilP), CoilKnock = Non(h, "coil_knock", hit.CoilKnock), ConfPerDmg = Non(h, "conf_per_dmg", hit.ConfPerDmg),
                LoopP = Share(h, "loop_p", hit.LoopP), LoopDmg = Non(h, "loop_dmg", hit.LoopDmg), LoopDmgRand = Non(h, "loop_dmg_rand", hit.LoopDmgRand),
                NacelleP = Share(h, "nacelle_p", hit.NacelleP), NacelleDmg = Non(h, "nacelle_dmg", hit.NacelleDmg),
            };
            h.Done();
        }
        var r = new ReactorDef
        {
            Coils = Whole(o, "coils", d.Coils, 1), Loops = Whole(o, "loops", d.Loops, 1), FlowMax = Whole(o, "flow_max", d.FlowMax, 1),
            FlowStart = Whole(o, "flow_start", d.FlowStart, 0), FlowE = Whole(o, "flow_e", d.FlowE, 0),
            EfficiencyLoss = Share(o, "efficiency_loss", d.EfficiencyLoss), HeatFullPctS = Non(o, "heat_full_pct_s", d.HeatFullPctS),
            HeatMisalignPctS = Non(o, "heat_misalign_pct_s", d.HeatMisalignPctS), CoolPerFlow = Non(o, "cool_per_flow", d.CoolPerFlow),
            TempFloorPct = Non(o, "temp_floor_pct", d.TempFloorPct), TempStartPct = Non(o, "temp_start_pct", d.TempStartPct),
            StablePct = Non(o, "stable_pct", d.StablePct), CriticalPct = Pos(o, "critical_pct", d.CriticalPct),
            CoolingOfflineBelow = Non(o, "cooling_offline_below", d.CoolingOfflineBelow), MeltdownS = Pos(o, "meltdown_s", d.MeltdownS),
            MeltRecoverX = Non(o, "melt_recover_x", d.MeltRecoverX), DriftPerS = Non(o, "drift_per_s", d.DriftPerS), DriftX = Non(o, "drift_x", d.DriftX),
            Nudge = Share(o, "nudge", d.Nudge), CrewLoopBelow = Share(o, "crew_loop_below", d.CrewLoopBelow),
            BlastKm = Pos(o, "blast_km", d.BlastKm), BlastDmg = Non(o, "blast_dmg", d.BlastDmg), Hit = hit,
        };
        if (r.FlowStart > r.FlowMax) throw new DataError($"{o.Where}.flow_start: at most flow_max ({r.FlowMax})");
        if (r.StablePct >= r.CriticalPct) throw new DataError($"{o.Where}.stable_pct: below critical_pct ({r.CriticalPct})");
        if (r.TempStartPct >= r.CriticalPct) throw new DataError($"{o.Where}.temp_start_pct: below critical_pct ({r.CriticalPct})");
        return r;
    }

    private static WarpDef ReadWarp(StrictJson o)
    {
        var d = new WarpDef();
        o.Str("_doc", "");
        WarpRiskDef risk = d.Risk;
        if (o.ObjOrNull("risk") is { } k)
        {
            risk = new WarpRiskDef
            {
                Nacelles = Non(k, "nacelles", risk.Nacelles), NacelleOut = Share(k, "nacelle_out", risk.NacelleOut), Coolant = Non(k, "coolant", risk.Coolant),
                CoolantOk = Share(k, "coolant_ok", risk.CoolantOk), Banks = Non(k, "banks", risk.Banks), Nav = Non(k, "nav", risk.Nav),
                Reactor = Non(k, "reactor", risk.Reactor), Qg = Non(k, "qg", risk.Qg), QgOverride = Non(k, "qg_override", risk.QgOverride),
            };
            k.Done();
        }
        WarpMishapDef mis = d.Mishap;
        if (o.ObjOrNull("mishap") is { } m)
        {
            m.Str("_doc", "");
            mis = new WarpMishapDef
            {
                Nacelle = Non(m, "nacelle", mis.Nacelle), NacelleSev = Non(m, "nacelle_sev", mis.NacelleSev), NacelleX = Non(m, "nacelle_x", mis.NacelleX),
                CoolantSev = Share(m, "coolant_sev", mis.CoolantSev), HullDmg = Non(m, "hull_dmg", mis.HullDmg), HullDmgSev = Non(m, "hull_dmg_sev", mis.HullDmgSev),
                BreachRisk = Share(m, "breach_risk", mis.BreachRisk), BreachSev = Share(m, "breach_sev", mis.BreachSev),
            };
            m.Done();
        }
        var w = new WarpDef
        {
            Nacelles = Whole(o, "nacelles", d.Nacelles, 1), CoilE = Whole(o, "coil_e", d.CoilE, 1), BankE = Pos(o, "bank_e", d.BankE), JumpE = Pos(o, "jump_e", d.JumpE),
            ChargeK = Non(o, "charge_k", d.ChargeK), PlotS = Pos(o, "plot_s", d.PlotS), CoolantJump = Share(o, "coolant_jump", d.CoolantJump),
            LeakPerS = Non(o, "leak_per_s", d.LeakPerS), QgBlock = Share(o, "qg_block", d.QgBlock), OutS = Pos(o, "out_s", d.OutS),
            OutSpeedMS = Non(o, "out_speed_m_s", d.OutSpeedMS), Risk = risk, Mishap = mis,
        };
        if (w.JumpE > w.BankE) throw new DataError($"{o.Where}.jump_e: at most bank_e ({w.BankE})");
        if (w.CoilE > 10) throw new DataError($"{o.Where}.coil_e: at most 10 (one allocation row)");
        return w;
    }

    private static readonly string[] JobIds = { "reactor", "warp", "mounts", "weapons", "shields", "sensors", "engines", "tractor", "transport" };

    private static RepairDef ReadRepair(StrictJson o)
    {
        o.Str("_doc", "");
        var jobs = new List<RepairJob>();
        foreach (object[] row in o.RawArray("jobs"))
        {
            if (row.Length != 4 || row.Any(c => c is not string)) throw new DataError($"{o.Where}.jobs: each job is [id, name, icon, short name]");
            string id = (string)row[0];
            if (!JobIds.Contains(id)) throw new DataError($"{o.Where}.jobs: no job '{id}' (there are: {string.Join(", ", JobIds)})");
            if (jobs.Any(j => j.Id == id)) throw new DataError($"{o.Where}.jobs: '{id}' twice");
            jobs.Add(new RepairJob(id, (string)row[1], (string)row[2], (string)row[3]));
        }
        string[] queue = o.StrList("queue");
        if (queue.Length != jobs.Count || queue.Distinct().Count() != queue.Length || queue.Any(q => jobs.All(j => j.Id != q)))
            throw new DataError($"{o.Where}.queue: every job once");
        return new RepairDef { MaxCrews = Whole(o, "max_crews", new RepairDef().MaxCrews, 1), Jobs = jobs, Queue = queue };
    }

    private static OfficersDef ReadOfficers(StrictJson o)
    {
        var d = new OfficersDef();
        o.Str("_doc", "");
        OfficerAbility a = d.Ability;
        if (o.ObjOrNull("ability") is { } k)
        {
            a = new OfficerAbility
            {
                Het = Non(k, "het", a.Het), Drift = Share(k, "drift", a.Drift), Hit = Non(k, "hit", a.Hit), Pd = Non(k, "pd", a.Pd), Scan = Non(k, "scan", a.Scan),
                SweepKm = Non(k, "sweep_km", a.SweepKm), Jam = Non(k, "jam", a.Jam), RepairRate = Non(k, "repair_rate", a.RepairRate), Ambush = Non(k, "ambush", a.Ambush),
                Aboard = Non(k, "aboard", a.Aboard), PadRate = Non(k, "pad_rate", a.PadRate), WingHit = Non(k, "wing_hit", a.WingHit),
            };
            k.Done();
        }
        // the power officer's drift cut is a share per skill level: at the top skill it must leave some drift
        if (a.Drift * 4 >= 1) throw new DataError($"{o.Where}.ability.drift: four levels of it must stay below 1");
        string[] skills = o.Has("skills") ? o.StrList("skills") : d.Skills.ToArray();
        if (skills.Length != 5) throw new DataError($"{o.Where}.skills: five names, untrained to the top skill");
        string[] ranks = o.Has("ranks") ? o.StrList("ranks") : d.Ranks.ToArray(), brief = o.Has("ranks_short") ? o.StrList("ranks_short") : d.RanksShort.ToArray();
        if (ranks.Length != 4 || brief.Length != 4) throw new DataError($"{o.Where}.ranks: four ranks, full and short");
        var roster = new List<RosterOfficer>();
        foreach (object[] row in o.RawArray("roster"))
        {
            if (row.Length != 5 || row[0] is not string id || row[1] is not string name || row[2] is not int rank || row[3] is not string spec || row[4] is not int skill)
                throw new DataError($"{o.Where}.roster: each officer is [id, name, rank, specialty, skill]");
            if (roster.Any(r => r.Id == id)) throw new DataError($"{o.Where}.roster: '{id}' twice");
            if (rank is < 0 or > 3) throw new DataError($"{o.Where}.roster.{id}: rank 0 to 3");
            if (skill is < 1 or > 4) throw new DataError($"{o.Where}.roster.{id}: skill 1 to 4");
            if (!OfficersDef.Stations.Contains(spec)) throw new DataError($"{o.Where}.roster.{id}: no station '{spec}'");
            roster.Add(new RosterOfficer(id, name, rank, spec, skill));
        }
        if (roster.Count == 0) throw new DataError($"{o.Where}.roster: at least one officer");
        var factions = new Dictionary<string, OfficerNames>();
        if (o.ObjOrNull("factions") is { } fs)
        {
            foreach (string f in fs.Keys())
            {
                StrictJson fo = fs.Child(f);
                string[] fr = fo.StrList("ranks");
                if (fr.Length != 4) throw new DataError($"{fo.Where}.ranks: four ranks");
                StrictJson nm = fo.Obj("names");
                var names = new Dictionary<string, string>();
                foreach (string id in nm.Keys())
                {
                    if (roster.All(r => r.Id != id)) throw new DataError($"{nm.Where}.{id}: no officer '{id}' in the roster");
                    names[id] = nm.Str(id);
                }
                nm.Done();
                fo.Done();
                factions[f] = new OfficerNames(fr, names);
            }
            fs.Done();
        }
        return new OfficersDef { Ability = a, Skills = skills, Ranks = ranks, RanksShort = brief, NameShift = Whole(o, "name_shift", d.NameShift, 0), Roster = roster, Factions = factions };
    }
}
