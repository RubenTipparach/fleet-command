// SystemsDef.Comms: the numbers of comms, transport and boarding (data/battle/systems/comms.json), the battle console
// mockup's HAIL replies, TRANSPORT, BOARD, INV, TEAM_DEFAULT, each class's pads, hold, marines and specialists, and the
// transporter effect's BEAM_FX and STREAM (CLAUDE.md 6.4: tuning lives in data, units in the keys). This package's part
// of SystemsDef, in its own file so the system packages never edit the same lines. Every key is read and checked; a
// missing key takes the mockup's value (written once, here), a present zero is zero, an unknown key is an error.
using System;
using System.Collections.Generic;
using System.Linq;

namespace FleetCommand.Data;

/// <summary>What a hailed ship answers: an enemy by its faction (empty: it does not answer, as most do not), and a friendly
/// one by the templates, {name} and {captain} its own, {needs} what it still asks for.</summary>
public sealed record HailDef(string NoAnswer, IReadOnlyDictionary<string, string> EnemyReply, string FriendReply, string FriendNeeds, string PilotNeeded);

/// <summary>The transporter (owner, 2026-11-03: "transport range is too wide! 1.5 km only!"): its reach, the pads a class
/// without its own count has, the seconds a pad takes to charge at one energy point when it charges alone, the longest
/// boarding queue, and the strength at or under which a shield facing counts as down.</summary>
public sealed record TransportDef(double RangeKm, int PadsDefault, double PadChargeS, int QueueMax, double ShieldDownHp);

/// <summary>Boarding (the mockup's BOARD): raids, the fight aboard in rounds, the medics, hackers and the envoy.</summary>
public sealed record BoardDef(double RaidS, double ShootPerMarine, double ShootMax, double BombDmg, double DemoLoss, double MarineLoss,
    double VolleyS, double HitChance, int GuardCrew, double HackerLock, double VipPersuade, double Wound, double Heal,
    double HackerSpeed, double HackerSpeedMax, int HackerCover, double HackerExposedP);

/// <summary>The inventory transporter: crates moved by one beam, and the hold of a class with none of its own.</summary>
public sealed record InventoryDef(int PerBeam, int HoldDefault);

/// <summary>A class's transporter pads, cargo hold (units), combat marines and specialists (the mockup's pads, hold_t,
/// marines and team; the team over TEAM_DEFAULT).</summary>
public sealed record CrewDef(int Pads, int HoldT, int Marines, IReadOnlyDictionary<string, int> Team);

/// <summary>The transporter effect (the mockup's BEAM_FX and STREAM), in metres and seconds: the column at each end, a
/// planet's this many times wider, and the stream of motes from the sender to the receiver.</summary>
public sealed record BeamFxDef(double BeamS, double ColumnRM, double ColumnHM, double PlanetX, int StreamCount, double StreamSpreadS,
    double StreamTravelS, double StreamArcM, double StreamWobbleM, double StreamSizeM);

/// <summary>Everything comms, transport and boarding run on.</summary>
public sealed record CommsDef(HailDef Hail, TransportDef Transport, BoardDef Board, InventoryDef Inventory, IReadOnlyDictionary<string, int> TeamDefault,
    IReadOnlyDictionary<string, CrewDef> Classes, BeamFxDef Fx, int LogLinesDocked, int LogLinesExpanded, double DistressSpeedMS)
{
    /// <summary>The specialists besides combat marines, in the order the console lists them (owner, survey B14 and B15).</summary>
    public static readonly string[] Specialists = { "demo", "medics", "engineers", "hackers", "pilots" };
    /// <summary>Every role aboard, combat marines first (the mockup's ROLES).</summary>
    public static readonly string[] Roles = { "marines", "demo", "medics", "engineers", "hackers", "pilots" };
    /// <summary>What a hold carries (owner, 2026-11-03), each a count of units: trade goods and the three supplies.</summary>
    public static readonly string[] Items = { "goods", "parts", "rations", "medicine" };
    public static readonly string[] SupplyItems = { "parts", "rations", "medicine" };
    public static readonly string[] CargoItems = { "goods" };

    /// <summary>A class's crew: its own entry, else the defaults (the transporter's default pads, the default hold).</summary>
    public CrewDef CrewOf(string classId) => Classes.TryGetValue(classId, out CrewDef? c) ? c
        : new CrewDef(Transport.PadsDefault, Inventory.HoldDefault, 0, TeamDefault);
}

public sealed partial class SystemsDef
{
    /// <summary>Comms, transport and boarding (data/battle/systems/comms.json); the mockup's numbers until loaded.</summary>
    public CommsDef Comms { get; private set; } = ReadComms(StrictJson.Parse("{}", "(defaults)"));

    partial void LoadComms(StrictJson o) => Comms = ReadComms(o);

    /// <summary>Checks the per-class table against the ship classes: every class has its entry and every entry a class.</summary>
    public void CheckComms(IReadOnlyDictionary<string, ShipClass> classes)
    {
        foreach (string id in Comms.Classes.Keys)
            if (!classes.ContainsKey(id)) throw new DataError($"data/battle/systems/comms.json.classes.{id}: no class '{id}' in data/battle/classes.json");
        foreach (string id in classes.Keys)
            if (!Comms.Classes.ContainsKey(id)) throw new DataError($"data/battle/systems/comms.json.classes: no entry for class '{id}'");
    }

    private static CommsDef ReadComms(StrictJson o)
    {
        o.Str("_doc", "");
        StrictJson h = Obj(o, "hail");
        h.Str("_doc", "");
        var enemy = new Dictionary<string, string>();
        if (h.ObjOrNull("enemy_reply") is { } er) { foreach (string k in er.Keys()) enemy[k] = er.Str(k); er.Done(); }
        else foreach (var (k, v) in new[] { ("kethran", "The Hegemony does not parley with the weak."), ("vos", "Your cargo or your hull. Choose quickly.") }) enemy[k] = v;
        var hail = new HailDef(h.Str("no_answer", "No answer on any channel."), enemy, h.Str("friend_reply", "{name} here. Standing by."),
            h.Str("friend_needs", "{name} here. We still need {needs}."), h.Str("pilot_needed", "{name}: ours, but dead in space. Pilot needed."));
        h.Done();

        StrictJson t = Obj(o, "transport");
        t.Str("_doc", "");
        var tr = new TransportDef(Pos(t, "range_km", 1.5), Count(t, "pads_default", 2), Pos(t, "pad_charge_s", 8), Count(t, "queue_max", 30), Non(t, "shield_down_hp", 0.5));
        t.Done();

        StrictJson b = Obj(o, "boarding");
        b.Str("_doc", "");
        var board = new BoardDef(Pos(b, "raid_s", 6), Non(b, "shoot_per_marine", 0.2), Fr(b, "shoot_max", 0.6), Fr(b, "bomb_dmg", 0.8), Fr(b, "demo_loss", 0.6),
            Fr(b, "marine_loss", 0.2), Pos(b, "volley_s", 10), Fr(b, "hit_chance", 0.5), Whole(b, "guard_crew", 2), Fr(b, "hacker_lock", 0.5),
            Fr(b, "vip_persuade", 0.5), Fr(b, "wound", 0.5), Fr(b, "heal", 0.35), Fr(b, "hacker_speed", 0.15), Fr(b, "hacker_speed_max", 0.5),
            Whole(b, "hacker_cover", 5), Fr(b, "hacker_exposed_p", 0.5));
        b.Done();

        StrictJson iv = Obj(o, "inventory");
        iv.Str("_doc", "");
        var inv = new InventoryDef(Count(iv, "per_beam", 5), Whole(iv, "hold_default_t", 10));
        iv.Done();

        var teamDefault = Team(o.ObjOrNull("team_default"), new Dictionary<string, int> { ["demo"] = 2, ["medics"] = 1, ["engineers"] = 1, ["hackers"] = 1, ["pilots"] = 1 });
        var classes = new Dictionary<string, CrewDef>();
        if (o.ObjOrNull("classes") is { } cs)
        {
            foreach (string id in cs.Keys())
            {
                StrictJson c = cs.Child(id);
                var team = new Dictionary<string, int>(teamDefault);
                foreach (var (k, v) in Team(c.ObjOrNull("team"), new Dictionary<string, int>())) team[k] = v;
                classes[id] = new CrewDef(Count(c, "pads", tr.PadsDefault), Whole(c, "hold_t", inv.HoldDefault), Whole(c, "marines", 0), team);
                if (classes[id].Pads > 8) throw new DataError($"{c.Where}.pads: at most 8 (owner, 2026-11-02: \"1,2,3,4,5 up to 8 depending on ship size\")");
                c.Done();
            }
            cs.Done();
        }

        StrictJson f = Obj(o, "beam_fx");
        f.Str("_doc", "");
        var fx = new BeamFxDef(Pos(f, "beam_s", 1.6), Pos(f, "column_r_m", 90), Pos(f, "column_h_m", 320), Pos(f, "planet_x", 2.2), Count(f, "stream_count", 56),
            Pos(f, "stream_spread_s", 0.9), Pos(f, "stream_travel_s", 1.1), Non(f, "stream_arc_m", 140), Non(f, "stream_wobble_m", 50), Pos(f, "stream_size_m", 80));
        f.Done();
        var def = new CommsDef(hail, tr, board, inv, teamDefault, classes, fx, Count(o, "log_lines_docked", 4), Count(o, "log_lines_expanded", 22), Non(o, "distress_under_way_m_s", 120));
        o.Done();
        return def;
    }

    // a nested object, or an empty one (every key then takes its default) when absent
    private static StrictJson Obj(StrictJson o, string k) => o.ObjOrNull(k) ?? StrictJson.Parse("{}", $"{o.Where}.{k}");

    private static Dictionary<string, int> Team(StrictJson? o, Dictionary<string, int> fallback)
    {
        if (o == null) return fallback;
        var d = new Dictionary<string, int>();
        foreach (string k in o.Keys())
        {
            if (!CommsDef.Specialists.Contains(k)) throw new DataError($"{o.Where}.{k}: not a specialist (there are: {string.Join(", ", CommsDef.Specialists)})");
            d[k] = Whole(o, k, 0);
        }
        o.Done();
        return d;
    }

    private static double Pos(StrictJson o, string k, double d) { double v = o.Num(k, d); if (v <= 0) throw new DataError($"{o.Where}.{k}: must be above 0"); return v; }
    private static double Non(StrictJson o, string k, double d) { double v = o.Num(k, d); if (v < 0) throw new DataError($"{o.Where}.{k}: must be 0 or more"); return v; }
    private static double Fr(StrictJson o, string k, double d) { double v = o.Num(k, d); if (v < 0 || v > 1) throw new DataError($"{o.Where}.{k}: must be between 0 and 1"); return v; }
    private static int Whole(StrictJson o, string k, int d)
    {
        double v = o.Num(k, d);
        if (v < 0 || v != Math.Floor(v) || v > 10000) throw new DataError($"{o.Where}.{k}: expected a whole number, 0 or more");
        return (int)v;
    }
    private static int Count(StrictJson o, string k, int d) { int v = Whole(o, k, d); if (v < 1) throw new DataError($"{o.Where}.{k}: at least 1"); return v; }
}
