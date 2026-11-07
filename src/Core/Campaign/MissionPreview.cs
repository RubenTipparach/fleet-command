// MissionPreview: a mission file tried out on a fresh campaign, for the mission editor (tools/mission-editor) and the tests.
// It loads the file as the game does, offers it in a new campaign, briefs it, plans its battle, and plays the battle to a
// chosen end (won, withdrawn or lost, set directly rather than fought) to show what each outcome grades and pays. Everything
// goes through the game's own loader, campaign, planner and scoring: an author sees exactly what the game would do.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FleetCommand.Data;
using FleetCommand.Sim;

namespace FleetCommand.Campaign;

public sealed record PreviewResult(bool Ok, string Error, string Report);

public static class MissionPreview
{
    /// <summary>How a previewed battle is ended: every enemy destroyed (prizes struck), the fleet withdrawn, or the fleet lost.</summary>
    public enum End { Win, Withdraw, Lose }

    /// <summary>Loads <paramref name="text"/> as the mission file <paramref name="file"/> (relative to data/) beside the other
    /// files, checks it, offers it in a new campaign for <paramref name="side"/>, and reports the briefing, the battle and,
    /// when <paramref name="end"/> is given, the outcome. <paramref name="data"/> is what an offering mission would hand it.</summary>
    public static PreviewResult Run(Func<string, string> read, IEnumerable<string> missionFiles, string file, string text, string side,
        uint seed, IReadOnlyDictionary<string, object>? data, End? end)
    {
        GameData game;
        CampaignData camp;
        MissionScript def;
        try
        {
            def = MissionLoader.Load(text, "data/" + file);
            game = DataLoader.Load(read);
            var files = missionFiles.Where(f => f != file).Append(file).ToList();
            camp = CampaignData.Load(f => f == file ? text : read(f), files, game);
        }
        catch (DataError e) { return new PreviewResult(false, e.Message, ""); }

        var sb = new StringBuilder();
        try
        {
            Campaign c = Campaign.New(camp, side, seed);
            c.S.Missions.Clear();
            int where = c.Where(def).Count;
            sb.AppendLine($"{def.Name} ({def.Id}), {(def.Story ? "story" : "contract")}, {def.Reward:0} cr, offered in {where} sector{(where == 1 ? "" : "s")} of a new {camp.Factions[side].Name} campaign.");
            Offer? m = c.OfferMission(def.Id, data: data);
            if (m == null) { sb.AppendLine("It is offered nowhere: its where holds for no free sector."); return new PreviewResult(true, "", sb.ToString()); }
            Sector h = c.Hex(m.C, m.R);
            sb.AppendLine($"Offered at sector {m.C}.{m.R} ({h.PortName ?? c.TerrainOf(h).Name}, {camp.Factions.GetValueOrDefault(h.Owner)?.Name ?? h.Owner}), {m.Strength}, {m.Reward:0} cr.");
            foreach (var (k, v) in m.Text) sb.AppendLine($"  {k} = '{v}'");
            foreach (var (k, v) in m.Num) sb.AppendLine($"  {k} = {Expr.Format(v)}");
            sb.AppendLine();
            sb.AppendLine("BRIEFING");
            sb.AppendLine(c.Brief(m));
            sb.AppendLine("OBJECTIVE: " + c.Objective(m));
            c.S.Fleet[0].Supplies["marines"] = Math.Max(c.S.Fleet[0].Supplies["marines"], 2);
            if (c.Needs(m) is { } need) sb.AppendLine("NEEDS: " + need);
            MissionPlan? plan = MissionRun.Plan(c, m, game);
            sb.AppendLine();
            if (plan == null) sb.AppendLine("BATTLE: none (done on arrival).");
            else
            {
                sb.AppendLine($"BATTLE: {plan.Scenario.Name}, under the {plan.Scenario.Sky} sky");
                foreach (ShipStart s in plan.Scenario.Ships)
                {
                    var (label, tag, role) = plan.Labels[s.Id];
                    string who = plan.FleetShips.ContainsKey(s.Id) ? "your fleet" : role.ToString().ToLowerInvariant();
                    sb.AppendLine($"  {s.Name,-18} {s.Class,-9} {s.Faction,-8} {who,-10} {label}{(tag != null ? $" [{tag}]" : "")}  at {s.XM / 1000:0.0}, {s.ZM / 1000:0.0} km");
                }
                sb.AppendLine("OBJECTIVES");
                var sc = new OfferScope(c, m);
                foreach (MissionObjective o in plan.Battle.Objectives)
                    sb.AppendLine($"  {(o.Primary ? "primary" : "bonus  ")} {o.Text.Eval(sc)}   done: {o.Done.Text}{(o.ByWithdraw ? "   (met by withdrawing)" : "")}");
            }
            if (end is { } e)
            {
                Battle? b = null;
                if (plan != null)
                {
                    b = new Battle(game, plan.Scenario);
                    switch (e)
                    {
                        case End.Win:
                            foreach (Ship s in b.Ships.Where(s => s.Side == 1))
                                if (s.Prize) s.Hull = s.Class.HullHp * game.Tuning.CaptureHullFrac * 0.8; else { s.Hull = 0; s.Alive = false; }
                            b.Step();
                            break;
                        case End.Withdraw: b.EndWithdrawn(); break;
                        case End.Lose:
                            foreach (string id in plan.Groups["fleet"]) { Ship s = b.ShipById(id)!; s.Hull = 0; s.Alive = false; }
                            b.Lose();
                            break;
                    }
                }
                Debrief d = MissionRun.Finish(c, m, plan, b);
                sb.AppendLine();
                sb.AppendLine($"OUTCOME ({e}): {d.Grade}, {(d.Win ? "a win" : "not a win")}, {d.Credits:+0;-0;0} cr (now {c.S.Credits}), prestige {d.Prestige:+0;-0;0}");
                foreach (var (otext, _, met) in d.Objectives) sb.AppendLine($"  {(met ? "MET   " : "MISSED")} {otext}");
                foreach (var (fac, delta, now) in d.Standing) sb.AppendLine($"  standing {fac} {delta:+0;-0;0} (now {now})");
                if (d.Log != null) sb.AppendLine("  log: " + d.Log);
                foreach (Offer n in c.S.Missions) sb.AppendLine($"  next: {n.Script} at {n.C}.{n.R}");
            }
            return new PreviewResult(true, "", sb.ToString());
        }
        catch (DataError e) { return new PreviewResult(false, e.Message, sb.ToString()); }
    }
}
