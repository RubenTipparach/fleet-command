// MissionScript: one mission file (data/missions/<id>.json), typed and validated (openspec/changes/mission-scripting). The
// shape is the campaign mockup's mission scripts (docs/mockups/lib/campaign-missions.js) written as data: what the mission
// is, where it is offered, what it rolls when offered, the briefing, the battle it starts (its groups and objectives), how
// it is graded and what each outcome pays. Its rules are expressions (Expr.cs) checked when the file loads: a name the
// scope will not have, an unknown key or a bad expression stops the load naming the file and the field, so a file written
// in the mission editor or by hand fails in the editor, never in the middle of a campaign.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Campaign;

/// <summary>Text that may change with the mission: parts joined in order, each kept only when its condition holds.</summary>
public sealed class MissionText
{
    public IReadOnlyList<(Expr? When, Template Text)> Parts { get; }
    public MissionText(IReadOnlyList<(Expr? When, Template Text)> parts) { Parts = parts; }

    public string Eval(IScope s) => string.Concat(Parts.Where(p => p.When == null || p.When.Bool(s)).Select(p => p.Text.Eval(s)));

    public void Names(ICollection<string> vars, ICollection<string> calls)
    {
        foreach (var (w, t) in Parts) { w?.Names(vars, calls); t.Names(vars, calls); }
    }
}

/// <summary>Who a group fights for: the enemy, wingmen beside the fleet, or ships to protect.</summary>
public enum GroupRole { Enemy, Ally, Neutral }

/// <summary>A group of ships in a mission's battle. Tag marks the ship an objective is about: flagship, prize (to take,
/// not destroy), rescue (to keep alive) or decoy.
/// BearingDeg and RangeKm place an enemy group (clockwise from ahead; default spread ahead at 10 km); ShipName names its
/// tagged ship (a prize, a flagship). Hull "freighter" fields the faction's freighter (CampaignData.FreighterOf) in place of a
/// warship of its tier; a freighter keeps her course and never closes to fight, whatever her side.</summary>
public sealed record MissionGroup(string Id, Template Faction, Expr Count, GroupRole Role, Template Label, string? Tag, Expr? Tier,
    double? BearingDeg, double? RangeKm, Template? ShipName, string? Hull = null)
{
    /// <summary>Seconds into the battle it warps in (0 from the start); seconds it sleeps before waking (0 awake).</summary>
    public double ArriveS { get; init; }
    public double WakeS { get; init; }
    /// <summary>A planet of the battle it is placed beside (in orbit), or null.</summary>
    public string? AtPlanet { get; init; }
    /// <summary>Cargo in each ship's hold (goods, parts, rations, medicine): a trader's goods, a pod's salvage.</summary>
    public IReadOnlyDictionary<string, int> Hold { get; init; } = new Dictionary<string, int>();
    /// <summary>The hulls a group may field in place of its faction's warships: the faction's freighter, or a placeholder
    /// station or creature (openspec/changes/mission-systems), each a battle class that is fixed in place but the monster.</summary>
    public static readonly IReadOnlyDictionary<string, string?> HullClasses = new Dictionary<string, string?>
    {
        ["freighter"] = null, ["station"] = "sb", ["yard"] = "yd", ["asteroid_base"] = "ab", ["outpost"] = "op", ["battery"] = "pb",
        ["monster"] = "mon", ["pod"] = "pod",
    };
    /// <summary>Whether this group's ships never move (a station, yard, base, outpost, battery or pod).</summary>
    public bool Fixed => Hull is "station" or "yard" or "asteroid_base" or "outpost" or "battery" or "pod";
}

/// <summary>A planet in a mission's battle: placed by bearing and range from the fleet's start, its radius, look and what it
/// asks for, what stands on it, whether it receives an envoy, and who waits on its surface to be beamed up.</summary>
public sealed record MissionPlanet(string Id, Template Name, string Kind, double BearingDeg, double RangeKm, double RadiusKm, string Color,
    IReadOnlyDictionary<string, int> Needs, IReadOnlyList<InstallationDef> Sites, bool TakesEnvoy, Template? Waiting);

/// <summary>An objective: Primary ones decide win or lose, the rest lift the grade. Done is checked when the battle ends;
/// ByWithdraw: withdrawing meets it, as breaking out of an ambush does.</summary>
public sealed record MissionObjective(string Id, Template Text, bool Primary, bool ByWithdraw, Expr Done);

/// <summary>The battle a mission starts, under its condition (a mission may have several, the first whose When holds).</summary>
public sealed record MissionBattle(Expr? When, Template? Name, Template? Intel, IReadOnlyList<MissionGroup> Groups, IReadOnlyList<MissionObjective> Objectives,
    IReadOnlyList<MissionMarker> Markers)
{
    /// <summary>The planets of the battle.</summary>
    public IReadOnlyList<MissionPlanet> Planets { get; init; } = Array.Empty<MissionPlanet>();
    /// <summary>Seconds the battle lasts at most (0: no limit); engines cut for dead_engines' for_s in every every_s, or null.</summary>
    public double TimeLimitS { get; init; }
    public EngineOutage? DeadEngines { get; init; }
    /// <summary>Cargo loaded in the flagship's hold at the start (relief supplies), and the envoy aboard, or null.</summary>
    public IReadOnlyDictionary<string, int> Cargo { get; init; } = new Dictionary<string, int>();
    public Template? Envoy { get; init; }
    /// <summary>Whether the battle ends when its primary objectives are met (not only when the last enemy goes): a battle
    /// with markers, planets, a time limit or objectives about scans, sites or cargo.</summary>
    public bool EndsOnGoals { get; init; }
}

/// <summary>A marker beacon to fly to: at a bearing (clockwise from ahead) and range from the fleet's start, or at the start
/// itself; shown once the marker it follows (After) is reached; reached within RadiusM metres.</summary>
public sealed record MissionMarker(string Id, Template Label, double BearingDeg, double RangeKm, bool AtStart, string? After, double RadiusM);

/// <summary>A next mission offered when this one ends: the script, where (its own where, home, here, or the fleet's sector), and data it carries.</summary>
public sealed record NextMission(string Mission, string At, IReadOnlyDictionary<string, Template> Data);

/// <summary>One outcome rule: when it holds, what it pays. Credits and prestige are expressions; null prestige is the grade's.
/// Haul (a faction) brings the battle's freighter into the fleet with her cargo, worth HaulCr credits when she is cashed out at
/// a port (Campaign.CashOut).</summary>
public sealed record MissionOutcome(Expr? When, Expr? Credits, Expr? Prestige, IReadOnlyDictionary<Template, Expr> Standing, Template? Prize,
    IReadOnlyList<NextMission> Next, Template? Log, Template? Haul = null, Expr? HaulCr = null)
{
    /// <summary>The faction fleet this mission follows is broken up and leaves the map (hunters beaten).</summary>
    public bool Disband { get; init; }
}

/// <summary>A grading rule: when it holds, the grade.</summary>
public sealed record GradeRule(Expr When, string Grade);

/// <summary>A condition that keeps the fleet from taking a mission, and what the briefing says about it.</summary>
public sealed record MissionNeed(Expr When, Template Say);

public sealed record SetupStep(string Set, Expr? Value, IReadOnlyList<string>? Pick);

public sealed record MissionScript(
    string Id, string Name, string Glyph, bool Story, string Type, double Reward, int Turns, Expr Where, IReadOnlyList<SetupStep> Setup,
    MissionText Brief, MissionText Objective, IReadOnlyList<MissionNeed> Needs, IReadOnlyList<MissionBattle> Battles,
    IReadOnlyList<GradeRule> Grades, IReadOnlyList<MissionOutcome> Results, bool FollowGroup, string File)
{
    /// <summary>Whether the fleet may withdraw from its battle: a contract yes, a story or special mission no.</summary>
    public bool CanWithdraw { get; init; } = true;
    /// <summary>How often a contract is offered against the others: a weight of 4 is picked four times as often as 1.</summary>
    public double Weight { get; init; } = 1;
    /// <summary>An encounter: never offered at random, only when the fleet shares a sector with a freighter (Campaign.Meet),
    /// and gone at the end of the turn, when she moves on (owner, 2026-11-07: "no mision flag needed. Just whenever you are
    /// on the same tile as freighter, you get a mission option").</summary>
    public bool Encounter { get; init; }
    /// <summary>An event: never offered at random, only by the campaign itself (hunters catching the fleet, Campaign.Caught),
    /// and gone at the end of the turn; it may be withdrawn from.</summary>
    public bool Event { get; init; }
    /// <summary>Whether its offer lapses after <see cref="Turns"/>: a contract always, a story mission only when its file gives
    /// turns (owner, 2026-11-11, of the shakedown: "there should only be one mission of that type, and it is optional, should
    /// go away after 5 turns or so").</summary>
    public bool Expires { get; init; } = true;
    /// <summary>Missions offered when its offer lapses unflown, so a story goes on past a mission the captain passed by.</summary>
    public IReadOnlyList<string> Lapse { get; init; } = Array.Empty<string>();
}

/// <summary>Reads mission files. Each is checked against the names its scopes provide (Scopes below).</summary>
public static class MissionLoader
{
    /// <summary>The five grades, worst to best.</summary>
    public static readonly string[] GradeIds = { "crushing", "defeat", "draw", "victory", "outstanding" };
    private static readonly string[] Roles = { "enemy", "ally", "neutral" };
    private static readonly string[] Tags = { "flagship", "prize", "rescue", "decoy", "drone", "quarry", "scout", "truce" };
    private static readonly string[] Places = { "where", "home", "here", "fleet" };

    /// <summary>Loads every mission; <paramref name="files"/> are paths relative to data/, <paramref name="read"/> returns text.</summary>
    public static Dictionary<string, MissionScript> LoadAll(IEnumerable<string> files, Func<string, string> read)
    {
        var all = new Dictionary<string, MissionScript>();
        foreach (string f in files.OrderBy(x => x, StringComparer.Ordinal))
        {
            MissionScript m = Load(read(f), "data/" + f);
            if (all.ContainsKey(m.Id)) throw new DataError($"data/{f}.id: '{m.Id}' is already the id of {all[m.Id].File}");
            all[m.Id] = m;
        }
        foreach (MissionScript m in all.Values)
            foreach (MissionOutcome o in m.Results)
                foreach (NextMission n in o.Next)
                    if (!all.ContainsKey(n.Mission)) throw new DataError($"{m.File}.result: next mission '{n.Mission}' is no mission file");
        foreach (MissionScript m in all.Values)
            foreach (string n in m.Lapse)
                if (!all.ContainsKey(n)) throw new DataError($"{m.File}.lapse_next: '{n}' is no mission file");
        return all;
    }

    public static MissionScript Load(string text, string file)
    {
        StrictJson o = StrictJson.Parse(text, file);
        string id = o.Str("id");
        if (id.Length == 0 || !id.All(c => char.IsLower(c) || char.IsDigit(c) || c == '_')) throw new DataError($"{file}.id: lower case letters, digits and _ only");
        // a contract may be withdrawn from; a story or special mission is seen through (owner, 2026-11-07: "some missions
        // withdraw is not possible (main story or special missions), general missions is fine to withdrawl")
        string kind = o.Str("kind", "contract");
        if (kind is not ("contract" or "story" or "special" or "encounter" or "event")) throw new DataError($"{file}.kind: 'contract', 'story', 'special', 'encounter' or 'event', not '{kind}'");
        string glyph = o.Str("glyph");
        if (glyph.Length != 1) throw new DataError($"{file}.glyph: one character");
        double reward = o.Num("reward_cr");
        if (reward < 0) throw new DataError($"{file}.reward_cr: not below 0");
        bool expires = kind != "story" || o.Has("turns");
        int turns = (int)o.Num("turns", 3);
        string[] lapse = o.Has("lapse_next") ? o.StrList("lapse_next") : Array.Empty<string>();
        if (lapse.Length > 0 && !expires) throw new DataError($"{file}.lapse_next: a story mission lapses only when it gives turns");
        // how often a contract is offered against the others (owner, 2026-11-07: "provide a healthy supply of patrol missions")
        double weight = o.Num("weight", 1);
        if (weight <= 0) throw new DataError($"{file}.weight: above 0");
        if (turns < 1) throw new DataError($"{file}.turns: at least 1");

        // names this mission is handed by the mission that offered it (a prize's name, its grade), and set by its setup
        var setupNames = o.Has("takes") ? o.StrList("takes").ToList() : new List<string>();
        foreach (string t in setupNames)
            if (Scopes.Offer.Contains(t) || Scopes.Hex.Contains(t)) throw new DataError($"{file}.takes: '{t}' is a name the campaign already has");
        bool follow = o.Bool("follow_group", false);
        var setup = new List<SetupStep>();
        if (o.Has("setup"))
            foreach (StrictJson s in o.ObjList("setup"))
            {
                string set = s.Str("set");
                if (Scopes.Offer.Contains(set) || Scopes.Hex.Contains(set)) throw new DataError($"{s.Where}.set: '{set}' is a name the campaign already has");
                Expr? value = s.Has("expr") ? Ex(s, "expr") : null;
                string[]? pick = s.Has("pick") ? s.StrList("pick") : null;
                if ((value == null) == (pick == null)) throw new DataError($"{s.Where}: give one of expr or pick");
                if (pick is { Length: 0 }) throw new DataError($"{s.Where}.pick: at least one value");
                if (value != null) Check(value.Names, Scopes.Offer.Concat(setupNames), Scopes.OfferCalls, $"{s.Where}.expr");
                setupNames.Add(set);
                setup.Add(new SetupStep(set, value, pick));
                s.Done();
            }
        IEnumerable<string> offerVars = Scopes.Offer.Concat(setupNames);

        Expr where = Ex(o, "where");
        Check(where.Names, Scopes.Hex.Concat(Scopes.Offer), Scopes.HexCalls.Concat(Scopes.OfferCalls), $"{file}.where");
        MissionText brief = Text(o, "brief", offerVars), objective = Text(o, "objective", offerVars);

        var needs = new List<MissionNeed>();
        if (o.Has("needs"))
            foreach (StrictJson n in o.ObjList("needs"))
            {
                var need = new MissionNeed(Ex(n, "if"), Tpl(n, "say"));
                Check(need.When.Names, offerVars, Scopes.OfferCalls, $"{n.Where}.if");
                n.Done();
                needs.Add(need);
            }

        var battles = new List<MissionBattle>();
        if (o.Has("battle"))
            foreach (StrictJson b in o.ObjList("battle")) battles.Add(Battle(b, offerVars));

        var grades = new List<GradeRule>();
        if (o.Has("grade"))
            foreach (StrictJson g in o.ObjList("grade"))
            {
                Expr when = Ex(g, "when");
                string grade = g.Str("is");
                if (!GradeIds.Contains(grade)) throw new DataError($"{g.Where}.is: one of {string.Join(", ", GradeIds)}");
                Check(when.Names, offerVars.Concat(Scopes.Grade), Scopes.OfferCalls.Concat(Scopes.GradeCalls), $"{g.Where}.when");
                g.Done();
                grades.Add(new GradeRule(when, grade));
            }

        var results = new List<MissionOutcome>();
        if (o.Has("result"))
            foreach (StrictJson r in o.ObjList("result")) results.Add(Outcome(r, offerVars.Concat(Scopes.Grade).Concat(Scopes.Result)));

        var m = new MissionScript(id, o.Str("name"), glyph, kind == "story", o.Str("type", ""), reward, turns, where, setup, brief, objective,
            needs, battles, grades, results, follow, file) { CanWithdraw = kind is "contract" or "encounter" or "event", Weight = weight, Encounter = kind == "encounter", Event = kind == "event", Expires = expires, Lapse = lapse };
        if (m.Encounter && !battles.Any(b => b.Groups.Any(g => g.Hull == "freighter")))
            throw new DataError($"{file}.battle: an encounter is fought round the freighter met, so a group needs hull 'freighter'");
        o.Done();
        return m;
    }

    private static MissionBattle Battle(StrictJson b, IEnumerable<string> vars)
    {
        Expr? when = b.Has("when") ? Ex(b, "when") : null;
        if (when != null) Check(when.Names, vars, Scopes.OfferCalls, $"{b.Where}.when");
        Template? name = b.Has("name") ? Tpl(b, "name") : null, intel = b.Has("intel") ? Tpl(b, "intel") : null;
        foreach (Template? t in new[] { name, intel }) if (t != null) Check(t.Names, vars, Scopes.OfferCalls, b.Where);
        var groups = new List<MissionGroup>();
        foreach (StrictJson g in b.ObjList("groups"))
        {
            string gid = g.Str("id");
            if (gid is "fleet" or "enemies" or "allies" || groups.Any(x => x.Id == gid)) throw new DataError($"{g.Where}.id: '{gid}' is taken");
            string role = g.Str("role");
            if (!Roles.Contains(role)) throw new DataError($"{g.Where}.role: one of {string.Join(", ", Roles)}");
            string? tag = g.Has("tag") ? g.Str("tag") : null;
            if (tag != null && !Tags.Contains(tag)) throw new DataError($"{g.Where}.tag: one of {string.Join(", ", Tags)}");
            var grp = new MissionGroup(gid, Tpl(g, "faction"), NumEx(g, "count"), Enum.Parse<GroupRole>(role, true),
                g.Has("label") ? Tpl(g, "label") : Template.Parse("", g.Where), tag, g.Has("tier") ? NumEx(g, "tier") : null,
                g.Has("bearing_deg") ? g.Num("bearing_deg") : null, g.Has("range_km") ? g.Num("range_km") : null, g.Has("ship_name") ? Tpl(g, "ship_name") : null,
                g.Has("hull") ? g.Str("hull") : null);
            if (grp.Hull != null && !MissionGroup.HullClasses.ContainsKey(grp.Hull))
                throw new DataError($"{g.Where}.hull: one of {string.Join(", ", MissionGroup.HullClasses.Keys)}, or left out, not '{grp.Hull}'");
            grp = grp with
            {
                ArriveS = Range(g, "arrive_s", 0, 3600), WakeS = Range(g, "wake_s", 0, 3600), AtPlanet = g.Has("at_planet") ? g.Str("at_planet") : null,
                Hold = Counts(g, "hold"),
            };
            if (grp.RangeKm is <= 0 or > 400) throw new DataError($"{g.Where}.range_km: above 0 and at most 400");
            if (grp.ShipName != null) Check(grp.ShipName.Names, vars, Scopes.OfferCalls, $"{g.Where}.ship_name");
            Check(grp.Faction.Names, vars, Scopes.OfferCalls, $"{g.Where}.faction");
            Check(grp.Count.Names, vars, Scopes.OfferCalls, $"{g.Where}.count");
            Check(grp.Label.Names, vars, Scopes.OfferCalls, $"{g.Where}.label");
            if (grp.Tier != null) Check(grp.Tier.Names, vars, Scopes.OfferCalls, $"{g.Where}.tier");
            g.Done();
            groups.Add(grp);
        }
        if (!groups.Any(g => g.Role == GroupRole.Enemy)) throw new DataError($"{b.Where}.groups: a battle needs at least one enemy group");
        var planets = new List<MissionPlanet>();
        if (b.Has("planets"))
            foreach (StrictJson p in b.ObjList("planets"))
            {
                string pid = p.Str("id");
                if (planets.Any(x => x.Id == pid)) throw new DataError($"{p.Where}.id: '{pid}' twice");
                string kind = p.Str("kind", "terran");
                if (!PlanetDef.Kinds.Contains(kind)) throw new DataError($"{p.Where}.kind: one of {string.Join(", ", PlanetDef.Kinds)}");
                var sites = new List<InstallationDef>();
                if (p.Has("sites"))
                    foreach (StrictJson q in p.ObjList("sites"))
                    {
                        string sk = q.Str("kind");
                        if (!InstallationDef.Kinds.Contains(sk)) throw new DataError($"{q.Where}.kind: one of {string.Join(", ", InstallationDef.Kinds)}");
                        string sid = q.Str("id");
                        if (sites.Any(x => x.Id == sid)) throw new DataError($"{q.Where}.id: '{sid}' twice");
                        sites.Add(new InstallationDef(sid, q.Str("name"), sk, Range(q, "lat_deg", -90, 90), Range(q, "lon_deg", -180, 180),
                            q.Bool("hostile", false), (int)Range(q, "garrison", 0, 30), q.Bool("known", false)));
                        q.Done();
                    }
                var mp = new MissionPlanet(pid, Tpl(p, "name"), kind, p.Num("bearing_deg", 0), Range(p, "range_km", 1, 400, 60), Range(p, "radius_km", 2, 40, 12),
                    p.Str("color", "#6b8fb0"), Counts(p, "needs"), sites, p.Bool("takes_envoy", false), p.Has("waiting") ? Tpl(p, "waiting") : null);
                Check(mp.Name.Names, vars, Scopes.OfferCalls, $"{p.Where}.name");
                if (mp.Waiting != null) Check(mp.Waiting.Names, vars, Scopes.OfferCalls, $"{p.Where}.waiting");
                p.Done();
                planets.Add(mp);
            }
        foreach (MissionGroup g in groups)
            if (g.AtPlanet != null && !planets.Any(x => x.Id == g.AtPlanet)) throw new DataError($"{b.Where}.groups: '{g.Id}' is at_planet '{g.AtPlanet}', which the battle does not place");
        double limit = Range(b, "time_limit_s", 0, 3600);
        EngineOutage? dead = null;
        if (b.Has("dead_engines"))
        {
            StrictJson de = b.Obj("dead_engines");
            dead = new EngineOutage(Range(de, "every_s", 10, 600), Range(de, "for_s", 1, 300));
            if (dead.ForS >= dead.EveryS) throw new DataError($"{de.Where}.for_s: shorter than every_s");
            de.Done();
        }
        var cargo = Counts(b, "cargo");
        Template? envoy = b.Has("envoy") ? Tpl(b, "envoy") : null;
        if (envoy != null) Check(envoy.Names, vars, Scopes.OfferCalls, $"{b.Where}.envoy");
        foreach (MissionGroup g in groups)
            if (g.Tag == "drone" && g.Role != GroupRole.Enemy) throw new DataError($"{b.Where}.groups: '{g.Id}' is tagged drone, so its role is enemy");
        var markers = new List<MissionMarker>();
        if (b.Has("markers"))
            foreach (StrictJson k in b.ObjList("markers"))
            {
                string mid = k.Str("id");
                if (markers.Any(x => x.Id == mid)) throw new DataError($"{k.Where}.id: '{mid}' twice");
                string at = k.Str("at", "bearing");
                if (at is not ("bearing" or "start")) throw new DataError($"{k.Where}.at: bearing or start");
                string? after = k.Has("after") ? k.Str("after") : null;
                if (after != null && !markers.Any(x => x.Id == after)) throw new DataError($"{k.Where}.after: no marker '{after}' before it");
                double bearing = k.Num("bearing_deg", 0), range = k.Num("range_km", at == "start" ? 0 : 80), radius = k.Num("radius_m", 8000);
                if (at == "bearing" && range is <= 0 or > 400) throw new DataError($"{k.Where}.range_km: above 0 and at most 400");
                if (radius is < 1000 or > 50000) throw new DataError($"{k.Where}.radius_m: 1000 to 50000");
                var mk = new MissionMarker(mid, Tpl(k, "label"), bearing, range, at == "start", after, radius);
                Check(mk.Label.Names, vars, Scopes.OfferCalls, $"{k.Where}.label");
                k.Done();
                markers.Add(mk);
            }
        var groupIds = groups.Select(g => g.Id).Concat(new[] { "fleet", "enemies", "allies" }).ToHashSet();
        var objs = new List<MissionObjective>();
        foreach (StrictJson j in b.ObjList("objectives"))
        {
            var ob = new MissionObjective(j.Str("id"), Tpl(j, "text"), j.Bool("primary", false), j.Bool("by_withdraw", false), Ex(j, "done"));
            if (objs.Any(x => x.Id == ob.Id)) throw new DataError($"{j.Where}.id: '{ob.Id}' twice");
            Check(ob.Text.Names, vars, Scopes.OfferCalls, $"{j.Where}.text");
            Check(ob.Done.Names, vars.Concat(Scopes.BattleEnd).Concat(groupIds), Scopes.BattleCalls, $"{j.Where}.done");
            j.Done();
            objs.Add(ob);
        }
        if (!objs.Any(x => x.Primary)) throw new DataError($"{b.Where}.objectives: at least one primary objective");
        b.Done();
        foreach (MissionObjective ob in objs)
        {
            foreach (string r in ob.Done.Calls("reached"))
                if (!markers.Any(x => x.Id == r)) throw new DataError($"{b.Where}.objectives: '{ob.Id}' asks reached('{r}'), and there is no such marker");
            foreach (string fn in Scopes.PlanetCalls)
                foreach (string r in ob.Done.Calls(fn))
                    if (!planets.Any(x => x.Id == r)) throw new DataError($"{b.Where}.objectives: '{ob.Id}' asks {fn}('{r}'), and the battle places no such planet");
        }
        // a battle whose goals are not all about the enemy ends when its primary objectives are met
        string[] goalCalls = Scopes.PlanetCalls.Concat(new[] { "scanned", "cargo", "reached" }).ToArray();
        bool goals = markers.Count > 0 || planets.Count > 0 || limit > 0
            || objs.Any(o => o.Primary && goalCalls.Any(fn => Uses(o.Done, fn)));
        return new MissionBattle(when, name, intel, groups, objs, markers)
            { Planets = planets, TimeLimitS = limit, DeadEngines = dead, Cargo = cargo, Envoy = envoy, EndsOnGoals = goals };
    }

    private static MissionOutcome Outcome(StrictJson r, IEnumerable<string> vars)
    {
        var calls = Scopes.OfferCalls.Concat(Scopes.GradeCalls).ToList();
        Expr? when = r.Has("when") ? Ex(r, "when") : null;
        Expr? credits = r.Has("credits") ? NumEx(r, "credits") : null, prestige = r.Has("prestige") ? NumEx(r, "prestige") : null;
        foreach (Expr? e in new[] { when, credits, prestige }) if (e != null) Check(e.Names, vars, calls, r.Where);
        var standing = new Dictionary<Template, Expr>();
        if (r.Has("standing"))
        {
            StrictJson s = r.Obj("standing");
            foreach (string k in s.Keys())
            {
                Template t = Template.Parse(k, $"{s.Where}.{k}");
                Expr v = NumEx(s, k);
                Check(t.Names, vars, calls, $"{s.Where}.{k}");
                Check(v.Names, vars, calls, $"{s.Where}.{k}");
                standing[t] = v;
            }
            s.Done();
        }
        Template? prize = r.Has("prize") ? Tpl(r, "prize") : null, log = r.Has("log") ? Tpl(r, "log") : null, haul = r.Has("haul") ? Tpl(r, "haul") : null;
        foreach (Template? t in new[] { prize, log, haul }) if (t != null) Check(t.Names, vars, calls, r.Where);
        Expr? haulCr = r.Has("haul_cr") ? NumEx(r, "haul_cr") : null;
        if ((haul == null) != (haulCr == null)) throw new DataError($"{r.Where}: haul and haul_cr go together");
        if (haulCr != null) Check(haulCr.Names, vars, calls, $"{r.Where}.haul_cr");
        var next = new List<NextMission>();
        if (r.Has("next"))
            foreach (StrictJson n in r.ObjList("next"))
            {
                string at = n.Str("at", "where");
                if (!Places.Contains(at)) throw new DataError($"{n.Where}.at: one of {string.Join(", ", Places)}");
                var data = new Dictionary<string, Template>();
                if (n.Has("data"))
                {
                    StrictJson d = n.Obj("data");
                    foreach (string k in d.Keys()) { data[k] = Tpl(d, k); Check(data[k].Names, vars, calls, $"{d.Where}.{k}"); }
                    d.Done();
                }
                next.Add(new NextMission(n.Str("mission"), at, data));
                n.Done();
            }
        bool disband = r.Bool("disband", false);
        r.Done();
        return new MissionOutcome(when, credits, prestige, standing, prize, next, log, haul, haulCr) { Disband = disband };
    }

    private static MissionText Text(StrictJson o, string key, IEnumerable<string> vars)
    {
        var parts = new List<(Expr?, Template)>();
        if (o.Has(key) && o.IsString(key)) parts.Add((null, Tpl(o, key)));
        else
            foreach (StrictJson p in o.ObjList(key))
            {
                Expr? when = p.Has("when") ? Ex(p, "when") : null;
                parts.Add((when, Tpl(p, "text")));
                p.Done();
            }
        var mt = new MissionText(parts);
        Check(mt.Names, vars, Scopes.OfferCalls, $"{o.Where}.{key}");
        return mt;
    }

    // whether an expression calls a function
    private static bool Uses(Expr e, string fn)
    {
        var v = new List<string>();
        var c = new List<string>();
        e.Names(v, c);
        return c.Contains(fn);
    }

    // a number in [lo, hi] (default d when absent)
    private static double Range(StrictJson o, string k, double lo, double hi, double d = 0)
    {
        double v = o.Num(k, d);
        if (v < lo || v > hi) throw new DataError($"{o.Where}.{k}: {lo} to {hi}");
        return v;
    }

    // a count of each hold item (goods, parts, rations, medicine), whole and 0 or more
    private static IReadOnlyDictionary<string, int> Counts(StrictJson o, string k)
    {
        var d = new Dictionary<string, int>();
        if (!o.Has(k)) return d;
        StrictJson c = o.Obj(k);
        foreach (string item in c.Keys())
        {
            if (!CommsDef.Items.Contains(item)) throw new DataError($"{c.Where}.{item}: one of {string.Join(", ", CommsDef.Items)}");
            double v = c.Num(item);
            if (v < 0 || v != Math.Floor(v) || v > 1000) throw new DataError($"{c.Where}.{item}: a whole number, 0 to 1000");
            d[item] = (int)v;
        }
        c.Done();
        return d;
    }

    private static Expr Ex(StrictJson o, string key) => Expr.Parse(o.Str(key), $"{o.Where}.{key}");
    private static Template Tpl(StrictJson o, string key) => Template.Parse(o.Str(key), $"{o.Where}.{key}");
    /// <summary>A number written as a JSON number or as an expression in a string.</summary>
    private static Expr NumEx(StrictJson o, string key) => o.IsString(key) ? Ex(o, key) : Expr.Const(o.Num(key));

    private static void Check(Action<ICollection<string>, ICollection<string>> names, IEnumerable<string> vars, IEnumerable<string> calls, string where)
    {
        var v = new List<string>();
        var c = new List<string>();
        names(v, c);
        var okV = vars.ToHashSet();
        var okC = calls.ToHashSet();
        foreach (string n in v) if (!okV.Contains(n)) throw new DataError($"{where}: unknown name '{n}' (known here: {string.Join(", ", okV.OrderBy(x => x))})");
        foreach (string n in c) if (!okC.Contains(n)) throw new DataError($"{where}: unknown function '{n}()' (known here: {string.Join(", ", okC.OrderBy(x => x))})");
    }
}

/// <summary>The names each scope gives a mission's expressions; the loader checks against these, and the scopes in
/// MissionScopes.cs provide exactly these.</summary>
public static class Scopes
{
    /// <summary>When a mission is offered and briefed: the campaign, the mission and its sector.</summary>
    public static readonly string[] Offer =
    {
        "side", "side_name", "rival", "rival_name", "enemy", "enemy_name", "foe", "foe_name", "home", "reward", "strength", "fleet", "marines",
        "sector_owner", "sector_owner_name", "sector_station", "sector_planet", "sector_terrain", "asteroids", "nebula", "group", "group_fac",
        "group_size", "story",
    };
    public static readonly string[] OfferCalls = { "rand", "name", "name_of", "hull_of", "class_of" };
    /// <summary>Per sector, for where: what the sector is.</summary>
    public static readonly string[] Hex =
    {
        "owner", "own", "hostile", "neutral", "plain", "port", "station", "planet", "terrain", "dist_home", "has_group", "group_size_here",
        "group_fac_here", "commander_here", "is_home",
    };
    public static readonly string[] HexCalls = { "on_route" };
    /// <summary>When the battle ends, for an objective's done.</summary>
    public static readonly string[] BattleEnd = { "how", "time_up", "we_fired_first", "they_fired_first", "no_shots", "teams_down" };
    public static readonly string[] BattleCalls =
    {
        "size", "alive", "destroyed", "taken", "hull_min", "reached", "scanned", "cargo", "surveyed", "secured", "sites", "relieved", "envoy_landed",
        "picked_up", "name", "rand", "name_of", "hull_of", "class_of",
    };
    /// <summary>The calls whose argument names a planet of the battle (checked when the file loads).</summary>
    public static readonly string[] PlanetCalls = { "surveyed", "secured", "sites", "relieved", "envoy_landed", "picked_up" };
    /// <summary>After the battle, for grade and result rules.</summary>
    public static readonly string[] Grade = { "how", "battle", "all_primary", "all_bonus", "bonus_count" };
    public static readonly string[] GradeCalls = { "met" };
    public static readonly string[] Result = { "grade", "win", "pay", "crushing", "defeat", "draw", "victory", "outstanding" };
}
