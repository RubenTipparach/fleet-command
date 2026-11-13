// MissionRun: a mission between the campaign and the battle (openspec/changes/mission-scripting). Plan turns an offered
// mission's battle into a Scenario the simulation plays (the player's fleet, wingmen, ships to protect and the enemy groups,
// placed by bearing and range), remembering which ship is in which group. Finish reads the battle as it ended, checks each
// objective's done, grades the outcome (the script's rules, or the default), finds the script's result rule and applies it
// to the campaign: credits, prestige, standing, a prize, the next missions, losses and damage. It returns the debrief the
// results screen shows. The five grades and what they pay are data (data/campaign/campaign.json, rules.pay and prestige).
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;

namespace FleetCommand.Campaign;

/// <summary>A planned mission battle: the scenario, the ships of each group by id, and each campaign ship's battle id.</summary>
public sealed record MissionPlan(Offer Mission, MissionBattle Battle, Scenario Scenario, IReadOnlyDictionary<string, List<string>> Groups,
    IReadOnlyDictionary<string, int> FleetShips, IReadOnlyDictionary<string, (string Label, string? Tag, GroupRole Role)> Labels);

public static class MissionRun
{
    /// <summary>A target drone is a light hulk: no shields, this share of its class's hull.</summary>
    public const double DroneHullFrac = 0.3;
    /// <summary>How far above a planet's surface a group placed at it orbits, m.</summary>
    public const double OrbitM = 4000;
    public const double FleetSpacingM = 7000, AllyOffsetM = 16000, GroupSpreadM = 8000, EnemyRangeKm = 100, NeutralRangeKm = 50, EnemyArcDeg = 35;

    /// <summary>The battle for an offered mission, or null when the mission has none (done on arrival).</summary>
    public static MissionPlan? Plan(Campaign c, Offer m, GameData g)
    {
        MissionBattle? b = c.BattleOf(m);
        if (b == null) return null;
        var sc = new OfferScope(c, m);
        var ships = new List<ShipStart>();
        var groups = new Dictionary<string, List<string>> { ["fleet"] = new(), ["enemies"] = new(), ["allies"] = new() };
        var fleetIds = new Dictionary<string, int>();
        var labels = new Dictionary<string, (string, string?, GroupRole)>();
        string side = c.S.Side;

        // a freighter taken with her cargo sails with the fleet but stays out of its battles (unless she is all it has)
        var fighting = c.S.Fleet.Where(x => x.Haul == 0).ToList();
        if (fighting.Count == 0) fighting = c.S.Fleet.ToList();
        for (int i = 0; i < fighting.Count; i++)
        {
            CampShip s = fighting[i];
            string id = $"own{s.Id}";
            double x = i == 0 ? 0 : (i % 2 == 1 ? -1 : 1) * FleetSpacingM * ((i + 1) / 2), z = i == 0 ? 0 : 5000;
            ShipSetup? kit = i == 0 && (b.Cargo.Count > 0 || b.Envoy != null)
                ? new ShipSetup(null, new Dictionary<string, int>(), b.Cargo, null, b.Envoy?.Eval(sc), null, 0, false, false, new Dictionary<string, int>(), new Dictionary<string, double>(), Array.Empty<double>())
                : null;
            ships.Add(new ShipStart(id, s.Name, i == 0 ? c.S.Captain : "Captain", c.Data.Hulls[s.Hull].BattleClass, side, 0, x, z, 0, 1200,
                Stance.Escort, HullFrac: Math.Clamp(s.HullPct / 100, 0.05, 1), Setup: kit));
            groups["fleet"].Add(id);
            fleetIds[id] = s.Id;
            labels[id] = (s.Name, null, GroupRole.Ally);
        }

        // the planets, by bearing (clockwise from ahead, which is -z) and range to their centre from the fleet's start
        var planets = b.Planets.Select(p =>
        {
            double rad = p.BearingDeg * Math.PI / 180, r = p.RangeKm * 1000;
            return new PlanetDef(p.Id, p.Name.Eval(sc), DMath.Sin(rad) * r, -DMath.Cos(rad) * r, p.RadiusKm * 1000, p.Color, p.Needs)
                { Kind = p.Kind, Installations = p.Sites, TakesEnvoy = p.TakesEnvoy, Waiting = p.Waiting?.Eval(sc) };
        }).ToList();

        var enemyGroups = b.Groups.Where(x => x.Role == GroupRole.Enemy).ToList();
        int allySeen = 0;
        foreach (MissionGroup grp in b.Groups)
        {
            string fac = grp.Faction.Eval(sc);
            if (!g.Factions.ContainsKey(fac)) throw new DataError($"{c.ScriptOf(m).File}: group '{grp.Id}' is of faction '{fac}', which the battle has no data for");
            int count = Math.Clamp((int)Math.Round(grp.Count.Num(sc)), 1, 8);
            string label = grp.Label.Eval(sc);
            var list = groups[grp.Id] = new List<string>();
            double bearing, rangeM;
            if (grp.Role == GroupRole.Ally) { bearing = 0; rangeM = 0; }
            else if (grp.Role == GroupRole.Neutral) { bearing = 0; rangeM = NeutralRangeKm * 1000; }
            else
            {
                int k = enemyGroups.IndexOf(grp), n = enemyGroups.Count;
                bearing = grp.BearingDeg ?? (k - (n - 1) / 2.0) * EnemyArcDeg;
                rangeM = (grp.RangeKm ?? EnemyRangeKm) * 1000;
            }
            var hullIds = c.Data.Factions.ContainsKey(fac) ? c.Data.HullsOf(fac).ToList() : new List<Hull>();
            string? fixedClass = grp.Hull != null ? MissionGroup.HullClasses[grp.Hull] : null;
            PlanetDef? orbit = grp.AtPlanet != null ? planets.First(p => p.Id == grp.AtPlanet) : null;
            for (int i = 0; i < count; i++)
            {
                int tier = grp.Tier != null ? (int)Math.Round(grp.Tier.Num(sc)) : grp.Tag == "flagship" ? 3 : 1 + i % 2;
                string cls = fixedClass ?? (grp.Hull == "freighter" ? c.Data.FreighterOf(fac) : hullIds.Count > 0 ? c.Data.HullOfTier(fac, tier) : c.Data.HullOfTier("vos", 1)).BattleClass;
                string id = $"{grp.Id}{i + 1}";
                // a faction the campaign has no names for (the fauna) goes by its label
                var names = c.Data.Factions.TryGetValue(fac, out CampFaction? cf) ? cf.Names : (IReadOnlyList<string>)new[] { label };
                string name = grp.Tag is "flagship" or "prize" or "rescue" or "decoy" or "quarry" or "scout" && i == 0 && grp.ShipName != null ? grp.ShipName.Eval(sc)
                    : fixedClass != null && grp.Hull != "monster" ? (count > 1 ? $"{label} {i + 1}" : label)
                    : names.Count == 1 && count > 1 ? $"{names[0]} {i + 1}" : names[(list.Count + grp.Id.Length + i) % names.Count];
                double x, z, heading;
                if (grp.Role == GroupRole.Ally)
                {
                    int a = allySeen++;
                    x = (a % 2 == 0 ? -1 : 1) * (AllyOffsetM + GroupSpreadM * (a / 2));
                    z = 9000;
                    heading = 0;
                }
                else
                {
                    double rad = bearing * Math.PI / 180, lat = (i - (count - 1) / 2.0) * GroupSpreadM;
                    // ahead is -z; the group's line runs across its bearing
                    x = DMath.Sin(rad) * rangeM + DMath.Cos(rad) * lat;
                    z = -DMath.Cos(rad) * rangeM + DMath.Sin(rad) * lat;
                    heading = grp.Role == GroupRole.Neutral ? 180 : Geometry.Wrap(DMath.Atan2(-x, z)) * 180 / Math.PI;
                }
                if (orbit != null)
                {
                    // in orbit: round the planet, spread about its near side
                    double a0 = DMath.Atan2(-orbit.XM, orbit.ZM) + (i - (count - 1) / 2.0) * 0.5, rr = orbit.RadiusM + OrbitM;
                    x = orbit.XM + DMath.Sin(a0) * rr;
                    z = orbit.ZM - DMath.Cos(a0) * rr;
                }
                // a freighter keeps her course, slow, whichever side she is on: she never closes to fight
                bool neutral = grp.Role == GroupRole.Neutral, drone = grp.Tag == "drone", freighter = grp.Hull == "freighter", still = grp.Fixed;
                // a scout runs: it keeps its course away from the fleet at full speed and never turns to fight
                bool scout = grp.Tag == "scout";
                if (scout) heading = Geometry.Wrap(DMath.Atan2(x, -z)) * 180 / Math.PI;
                ShipSetup? load = grp.Hold.Count > 0
                    ? new ShipSetup(null, new Dictionary<string, int>(), grp.Hold, null, null, null, 0, false, false, new Dictionary<string, int>(), new Dictionary<string, double>(), Array.Empty<double>())
                    : null;
                // a drone (a shakedown's target) stands where it is put and is a light hulk with no shields; it fires training rounds,
                // which drop shields and hurt nothing past them
                ships.Add(new ShipStart(id, name, label, cls, fac, grp.Role == GroupRole.Enemy ? 1 : 0, x, z, heading,
                    drone || still ? 0 : scout ? 2600 : neutral || freighter ? 600 : 1400,
                    grp.Role == GroupRole.Ally && !freighter && !still ? Stance.Escort : neutral || drone || freighter || still || scout ? Stance.Hold : Stance.Attack,
                    HoldFire: neutral || scout || grp.Hull == "pod",
                    HullFrac: drone ? DroneHullFrac : 1, ShieldsFrac: drone || grp.Hull == "pod" ? 0 : 1, Prize: grp.Tag == "prize" && i == 0, Setup: load)
                    { ArriveS = grp.ArriveS, WakeS = grp.WakeS, Fixed = still, Runs = scout, Truce = grp.Tag == "truce", Training = drone });
                list.Add(id);
                groups[grp.Role == GroupRole.Enemy ? "enemies" : "allies"].Add(id);
                labels[id] = (label, i == 0 ? grp.Tag : null, grp.Role);
            }
        }
        MissionScript def = c.ScriptOf(m);
        string intel = b.Intel?.Eval(sc) ?? "";
        string obj = b.Objectives.First(o => o.Primary).Text.Eval(sc);
        ulong seed = c.S.Seed * 1_000_003UL + (ulong)(c.S.Turn * 97 + m.Id);
        // the marker beacons, by bearing (clockwise from ahead, which is -z) and range from the fleet's start, or at it
        var markers = b.Markers.Select(k =>
        {
            double rad = k.BearingDeg * Math.PI / 180, r = k.AtStart ? 0 : k.RangeKm * 1000;
            return new MarkerDef(k.Id, k.Label.Eval(sc), DMath.Sin(rad) * r, -DMath.Cos(rad) * r, k.RadiusM, k.After);
        }).ToList();
        var scen = new Scenario($"mission_{def.Id}_{m.Id}", b.Name?.Eval(sc) ?? def.Name, side, seed, obj, "Comms",
            intel != "" ? $"Contacts ahead. {obj}" : obj, ships, SkyFor(c.Hex(m.C, m.R)), Markers: markers.Count > 0 ? markers : null)
            { CanWithdraw = def.CanWithdraw, Planets = planets.Count > 0 ? planets : null, TimeLimitS = b.TimeLimitS, DeadEngines = b.DeadEngines, ObjectiveEnd = b.EndsOnGoals };
        return new MissionPlan(m, b, scen, groups, fleetIds, labels);
    }

    // the freighter of a battle (its first freighter-hull group's first ship) as the battle ended, or null
    private static ShipEnd? FreighterEnd(MissionPlan? plan, Battle? battle)
    {
        if (plan == null || battle == null || plan.Battle.Groups.FirstOrDefault(g => g.Hull == "freighter") is not { } grp) return null;
        if (!plan.Groups.TryGetValue(grp.Id, out var ids) || ids.Count == 0 || battle.ShipById(ids[0]) is not { } s) return null;
        return new ShipEnd(s.Id, s.Alive, s.Struck || s.Captured, s.Hull / s.Class.HullHp);
    }

    // every group's ships as they stand: alive, taken, hull left, and scanned by the player's side
    private static Dictionary<string, List<ShipEnd>> Ends(MissionPlan plan, Battle battle) =>
        plan.Groups.ToDictionary(g => g.Key, g => g.Value.Select(id => battle.ShipById(id)!)
            .Select(s => new ShipEnd(s.Id, s.Alive, s.Struck || s.Captured, s.Hull / s.Class.HullHp) { Scanned = battle.ScannedIds.Contains(s.Id) }).ToList());

    /// <summary>What field operations came to (Battle.Field and Battle.Transport): each planet surveyed, its sites held,
    /// relieved, the envoy landed, the one waiting picked up; the fleet's cargo; the time limit; who fired first.</summary>
    public static FieldEnd FieldOf(MissionPlan plan, Battle battle)
    {
        var planets = battle.Planets.ToDictionary(p => p.Id, p => new PlanetEnd(p.Scanned, p.Installations.Count(i => i.Secured), p.Installations.Count,
            p.Relieved || (p.Needs == null), p.EnvoyLanded != null, p.Waiting == null));
        var cargo = new Dictionary<string, int>();
        foreach (string id in plan.Groups["fleet"])
            if (battle.ShipById(id) is { Alive: true } s)
                foreach (var (k, v) in s.Hold) cargo[k] = cargo.GetValueOrDefault(k) + v;
        // the envoy or anyone picked up from a planet counts as aboard while a fleet ship carries them
        return new FieldEnd(planets, cargo, battle.TimeUp, battle.FirstFire, battle.AwayTeams.Count);
    }

    /// <summary>The nebula sky a sector's battle is fought under (data/space/skies/).</summary>
    public static string SkyFor(Sector h) => h.Terrain == "nebula" ? "crimson_reach" : "deep_indigo";

    /// <summary>Default grading: losing the fleet is a crushing defeat; every primary met a victory, and every bonus too
    /// outstanding; a withdrawal short of the primary a draw, staying and failing it a defeat.</summary>
    public static string DefaultGrade(IReadOnlyList<MissionObjective> objs, IReadOnlyDictionary<string, bool> met, string how)
    {
        if (how == "lost") return "crushing";
        bool Ok(MissionObjective o) => met.GetValueOrDefault(o.Id);
        var bonus = objs.Where(o => !o.Primary).ToList();
        if (objs.Where(o => o.Primary).All(Ok)) return bonus.Count > 0 && bonus.All(Ok) ? "outstanding" : "victory";
        return how == "withdraw" ? "draw" : "defeat";
    }

    /// <summary>How a battle ended, as the mission files say it: end, withdraw or lost (a jump to warp is an escape: a withdrawal).</summary>
    public static string HowOf(Battle b) => b.Over switch { "withdrawn" or "jumped" or "stalemate" => "withdraw", "lost" => "lost", _ => "end" };

    /// <summary>Each objective's state now, as if the battle ended this moment (the mission strip's ticks, and a battle
    /// that ends on its objectives: GameFlow wins it once every primary is met). Asked of another battle (the last one,
    /// still standing while the mission's loads), none is met.</summary>
    public static Dictionary<string, bool> MetNow(Campaign c, MissionPlan plan, Battle battle)
    {
        // a battle not the plan's (the one before it, while the mission's battle loads) has met none of its objectives
        if (battle.Scenario != plan.Scenario) return plan.Battle.Objectives.ToDictionary(o => o.Id, _ => false);
        var ends = Ends(plan, battle);
        var sc = new EndScope(c, plan.Mission, ends, "end") { Reached = battle.Reached, Field = FieldOf(plan, battle) };
        return plan.Battle.Objectives.ToDictionary(o => o.Id, o => o.Done.Bool(sc));
    }

    /// <summary>Ends a mission: with its battle as it ended (plan and battle), or done on arrival (both null). Applies the
    /// result to the campaign and returns the debrief.</summary>
    public static Debrief Finish(Campaign c, Offer m, MissionPlan? plan, Battle? battle)
    {
        MissionScript def = c.ScriptOf(m);
        CampRules ru = c.Data.Rules;
        string how = battle != null ? HowOf(battle) : "done";
        var ends = new Dictionary<string, List<ShipEnd>>();
        if (plan != null && battle != null) ends = Ends(plan, battle);
        var sc = new EndScope(c, m, ends, how)
        {
            HadBattle = plan != null, Reached = battle?.Reached ?? (IReadOnlyCollection<string>)Array.Empty<string>(),
            Field = plan != null && battle != null ? FieldOf(plan, battle) : FieldEnd.None,
        };
        var objs = plan?.Battle.Objectives ?? (IReadOnlyList<MissionObjective>)Array.Empty<MissionObjective>();
        var met = new Dictionary<string, bool>();
        foreach (MissionObjective o in objs) met[o.Id] = (how == "withdraw" && o.ByWithdraw) || (how != "lost" && o.Done.Bool(sc));
        sc.Met = met;
        sc.Objectives = objs;
        string grade = plan == null ? "victory" : def.Grades.FirstOrDefault(r => r.When.Bool(sc))?.Grade ?? DefaultGrade(objs, met, how);
        sc.Grade = grade;
        sc.Pay = Math.Round(m.Reward * ru.Pay[grade] / 10, MidpointRounding.AwayFromZero) * 10;
        bool win = Array.IndexOf(MissionLoader.GradeIds, grade) >= Array.IndexOf(MissionLoader.GradeIds, "victory");

        var d = new Debrief { Mission = def.Name, Grade = grade, Win = win, How = how };
        foreach (MissionObjective o in objs) d.Objectives.Add((o.Text.Eval(sc), o.Primary, met[o.Id]));
        MissionOutcome? r = def.Results.FirstOrDefault(x => x.When == null || x.When.Bool(sc));
        int credits = r?.Credits != null ? (int)Math.Round(r.Credits.Num(sc)) : 0;
        int prestige = r?.Prestige != null ? (int)Math.Round(r.Prestige.Num(sc)) : (int)Math.Round(ru.Prestige[grade] * (def.Story ? ru.StoryPrestigeX : 1));
        c.S.Credits += credits;
        c.S.Prestige += prestige;
        d.Credits = credits;
        d.Prestige = prestige;
        d.PrestigeNow = c.S.Prestige;
        if (r != null)
        {
            foreach (var (ft, ex) in r.Standing)
            {
                string fac = ft.Eval(sc);
                int delta = (int)Math.Round(ex.Num(sc));
                if (fac == "" || delta == 0 || !c.S.Standing.ContainsKey(fac)) continue;
                c.ChangeStanding(fac, delta);
                d.Standing.Add((fac, delta, c.Standing(fac)));
            }
            if (r.Prize?.Eval(sc) is { Length: > 0 } pf && c.S.Fleet.Count < ru.FleetMax && c.Data.HullsOf(pf).Any(h => h.ForSale))
            {
                CampShip prize = c.NewShip(c.Data.HullsOf(pf).First(h => h.ForSale).Id);
                prize.HullPct = 50;
                c.S.Fleet.Add(prize);
                d.Ships.Add((prize.Name, "taken as a prize"));
            }
            // a freighter taken with her cargo joins the fleet, to be cashed out at a port; with no room in the fleet her
            // cargo is moved across and paid at half
            if (r.Haul?.Eval(sc) is { Length: > 0 } hf)
            {
                int cr = (int)Math.Round(r.HaulCr!.Num(sc));
                string name = m.Value("ship") as string ?? c.Data.FreighterOf(hf).Name;
                if (c.S.Fleet.Count < ru.FleetMax)
                {
                    CampShip haul = c.NewShip(c.Data.FreighterOf(hf).Id, name);
                    haul.Maker = hf;
                    haul.Haul = cr;
                    haul.HullPct = Math.Round(Math.Max(1, FreighterEnd(plan, battle)?.HullFrac * 100 ?? 50));
                    c.S.Fleet.Add(haul);
                    d.Ships.Add((name, $"taken with her cargo, {cr} cr when cashed out at a port"));
                }
                else
                {
                    c.S.Credits += cr / 2;
                    d.Credits += cr / 2;
                    d.Ships.Add((name, $"no room in the fleet: her cargo moved across, {cr / 2} cr"));
                }
            }
            foreach (NextMission n in r.Next)
            {
                (int, int)? at = n.At switch { "home" => (c.S.HomeC, c.S.HomeR), "fleet" => (c.S.AtC, c.S.AtR), "here" => (m.C, m.R), _ => null };
                var carry = new Dictionary<string, object>();
                foreach (var (k, v) in m.Text) carry[k] = v;
                foreach (var (k, v) in m.Num) carry[k] = v;
                foreach (var (k, t) in n.Data) carry[k] = t.Eval(sc);
                // the mission that ends frees its sector for the next one
                c.S.Missions.Remove(m);
                c.OfferMission(n.Mission, at, carry);
            }
            if (r.Disband && m.Group != null && c.S.Groups.FirstOrDefault(g => g.Name == m.Group) is { } beaten)
            {
                c.S.Groups.Remove(beaten);
                d.Ships.Add((beaten.Name, "broken up"));
            }
            d.Log = r.Log?.Eval(sc);
        }
        c.S.Missions.Remove(m);
        c.S.Active.Remove(m.Id);
        // the freighter an encounter was met with, destroyed or taken, is off her route while she is replaced
        if (def.Encounter && FreighterEnd(plan, battle) is { } fe && (!fe.Alive || fe.Taken) && c.FreighterOf(m) is { } metF) metF.Out = ru.FreighterRefitTurns;
        if (d.Log != null) c.S.Log.Add($"Turn {c.S.Turn}: {d.Log}");

        // the fleet as it came out: damage carried, losses struck off; a fleet lost entirely is given a new smallest hull
        if (plan != null && battle != null)
        {
            c.S.Battles++;
            foreach (var (bid, campId) in plan.FleetShips)
            {
                Ship s = battle.ShipById(bid)!;
                CampShip cs = c.S.Fleet.First(x => x.Id == campId);
                if (!s.Alive) { c.S.Fleet.Remove(cs); d.Ships.Add((cs.Name, "destroyed")); }
                else
                {
                    cs.HullPct = Math.Round(Math.Max(1, s.Hull / s.Class.HullHp * 100));
                    d.Ships.Add((cs.Name, $"hull {cs.HullPct:0} %"));
                }
            }
            if (c.Recommission() is { } fresh) d.Ships.Add((fresh.Name, "commissioned at home to replace the fleet"));
        }
        return d;
    }
}
