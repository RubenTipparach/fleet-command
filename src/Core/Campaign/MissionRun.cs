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
    public const double FleetSpacingM = 700, AllyOffsetM = 1600, GroupSpreadM = 800, EnemyRangeKm = 10, NeutralRangeKm = 5, EnemyArcDeg = 35;

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

        for (int i = 0; i < c.S.Fleet.Count; i++)
        {
            CampShip s = c.S.Fleet[i];
            string id = $"own{s.Id}";
            double x = i == 0 ? 0 : (i % 2 == 1 ? -1 : 1) * FleetSpacingM * ((i + 1) / 2), z = i == 0 ? 0 : 500;
            ships.Add(new ShipStart(id, s.Name, i == 0 ? c.S.Captain : "Captain", c.Data.Hulls[s.Hull].BattleClass, side, 0, x, z, 0, 120,
                Stance.Escort, HullFrac: Math.Clamp(s.HullPct / 100, 0.05, 1)));
            groups["fleet"].Add(id);
            fleetIds[id] = s.Id;
            labels[id] = (s.Name, null, GroupRole.Ally);
        }

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
            var hullIds = c.Data.HullsOf(fac).ToList();
            for (int i = 0; i < count; i++)
            {
                int tier = grp.Tier != null ? (int)Math.Round(grp.Tier.Num(sc)) : grp.Tag == "flagship" ? 3 : 1 + i % 2;
                Hull hull = hullIds.Count > 0 ? c.Data.HullOfTier(fac, tier) : c.Data.HullOfTier("vos", 1);
                string id = $"{grp.Id}{i + 1}";
                var names = c.Data.Factions[fac].Names;
                string name = grp.Tag is "flagship" or "prize" or "rescue" or "decoy" && i == 0 && grp.ShipName != null ? grp.ShipName.Eval(sc)
                    : names[(list.Count + grp.Id.Length + i) % names.Count];
                double x, z, heading;
                if (grp.Role == GroupRole.Ally)
                {
                    int a = allySeen++;
                    x = (a % 2 == 0 ? -1 : 1) * (AllyOffsetM + GroupSpreadM * (a / 2));
                    z = 900;
                    heading = 0;
                }
                else
                {
                    double rad = bearing * Math.PI / 180, lat = (i - (count - 1) / 2.0) * GroupSpreadM;
                    // ahead is -z; the group's line runs across its bearing
                    x = Math.Sin(rad) * rangeM + Math.Cos(rad) * lat;
                    z = -Math.Cos(rad) * rangeM + Math.Sin(rad) * lat;
                    heading = grp.Role == GroupRole.Neutral ? 180 : Geometry.Wrap(Math.Atan2(-x, z)) * 180 / Math.PI;
                }
                bool neutral = grp.Role == GroupRole.Neutral, drone = grp.Tag == "drone";
                // a drone (a shakedown's target) stands where it is put, never fires, and is a light hulk with no shields
                ships.Add(new ShipStart(id, name, label, hull.BattleClass, fac, grp.Role == GroupRole.Enemy ? 1 : 0, x, z, heading, drone ? 0 : neutral ? 60 : 140,
                    grp.Role == GroupRole.Ally ? Stance.Escort : neutral || drone ? Stance.Hold : Stance.Attack, HoldFire: neutral || drone,
                    HullFrac: drone ? DroneHullFrac : 1, ShieldsFrac: drone ? 0 : 1, Prize: grp.Tag == "prize" && i == 0));
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
            return new MarkerDef(k.Id, k.Label.Eval(sc), Math.Sin(rad) * r, -Math.Cos(rad) * r, k.RadiusM, k.After);
        }).ToList();
        var scen = new Scenario($"mission_{def.Id}_{m.Id}", b.Name?.Eval(sc) ?? def.Name, side, seed, obj, "Comms",
            intel != "" ? $"Contacts ahead. {obj}" : obj, ships, SkyFor(c.Hex(m.C, m.R)), Markers: markers.Count > 0 ? markers : null);
        return new MissionPlan(m, b, scen, groups, fleetIds, labels);
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

    /// <summary>How a battle ended, as the mission files say it: end, withdraw or lost.</summary>
    public static string HowOf(Battle b) => b.Over switch { "withdrawn" => "withdraw", "lost" => "lost", _ => "end" };

    /// <summary>Each objective's state now, as if the battle ended this moment (the mission strip's ticks, and a battle
    /// that ends on its objectives: GameFlow wins it once every primary is met).</summary>
    public static Dictionary<string, bool> MetNow(Campaign c, MissionPlan plan, Battle battle)
    {
        var ends = plan.Groups.ToDictionary(g => g.Key, g => g.Value.Select(id => battle.ShipById(id)!).Select(s => new ShipEnd(s.Id, s.Alive, s.Struck, s.Hull / s.Class.HullHp)).ToList());
        var sc = new EndScope(c, plan.Mission, ends, "end") { Reached = battle.Reached };
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
        if (plan != null && battle != null)
            foreach (var (gid, ids) in plan.Groups)
                ends[gid] = ids.Select(id => battle.ShipById(id)!).Select(s => new ShipEnd(s.Id, s.Alive, s.Struck, s.Hull / s.Class.HullHp)).ToList();
        var sc = new EndScope(c, m, ends, how) { HadBattle = plan != null, Reached = battle?.Reached ?? (IReadOnlyCollection<string>)Array.Empty<string>() };
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
            d.Log = r.Log?.Eval(sc);
        }
        c.S.Missions.Remove(m);
        c.S.Active.Remove(m.Id);
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
            if (c.S.Fleet.Count == 0)
            {
                CampShip fresh = c.NewShip(c.Data.HullsOf(c.S.Side).First().Id, c.Side.Prefix + c.Side.Names[c.S.Battles % c.Side.Names.Count]);
                c.S.Fleet.Add(fresh);
                c.S.AtC = c.S.HomeC;
                c.S.AtR = c.S.HomeR;
                d.Ships.Add((fresh.Name, "commissioned at home to replace the fleet"));
            }
        }
        return d;
    }
}
