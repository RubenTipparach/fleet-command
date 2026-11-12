using System.Collections.Generic;
using System.Linq;
using FleetCommand.Campaign;
using FleetCommand.Sim;
using Xunit;
using Xunit.Abstractions;
namespace FleetCommand.Tests;
public class ZzAiProbe
{
    private readonly ITestOutputHelper _o;
    public ZzAiProbe(ITestOutputHelper o) { _o = o; }
    private void Run(string label, Battle b)
    {
        var seen = new HashSet<Shot>();
        int[] shots = new int[2];
        var kinds = new Dictionary<string, int>();
        long lastSeq = 0; int[] nHits = new int[2]; double[] absorbed = new double[2], hulld = new double[2];
        double[] hull0 = b.Ships.GroupBy(s => s.Side).OrderBy(g => g.Key).Select(g => g.Sum(s => s.Hull)).ToArray();
        for (int i = 1; i <= 30 * 120; i++)
        {
            if (i % 90 == 1) { b.NextTarget(); b.HelmOrder("intercept"); }
            if (i % 15 == 0) b.Fire();
            b.Step();
            foreach (Shot sh in b.Shots) if (seen.Add(sh) && sh.From != null && b.ShipById(sh.From) is { } f) { shots[System.Math.Min(f.Side, 1)]++; if (f.Side == 1) kinds[sh.Kind] = kinds.GetValueOrDefault(sh.Kind) + 1; }
            foreach (var h in b.Hits) if (h.Seq > lastSeq) { lastSeq = h.Seq; int sd = b.ShipById(h.ShipId)?.Side ?? 0; nHits[System.Math.Min(sd, 1)]++; absorbed[System.Math.Min(sd, 1)] += h.Absorbed; hulld[System.Math.Min(sd, 1)] += h.HullDamage; }
            if (b.Over != null) break;
        }
        double[] hull1 = b.Ships.GroupBy(s => s.Side).OrderBy(g => g.Key).Select(g => g.Sum(s => System.Math.Max(0, s.Hull))).ToArray();
        _o.WriteLine($"{label} ALL hits on us {nHits[0]} (shield {absorbed[0]:0}, hull {hulld[0]:0}) of their {shots[1]} shots; on them {nHits[1]} (shield {absorbed[1]:0}, hull {hulld[1]:0}) of our {shots[0]}");
        var hits = b.Hits.ToList();
        _o.WriteLine($"{label} hits on us: {hits.Count(h => b.ShipById(h.ShipId)?.Side == 0)} absorbed {hits.Where(h => b.ShipById(h.ShipId)?.Side == 0).Sum(h => h.Absorbed):0} hull {hits.Where(h => b.ShipById(h.ShipId)?.Side == 0).Sum(h => h.HullDamage):0}; on them {hits.Count(h => b.ShipById(h.ShipId)?.Side == 1)} absorbed {hits.Where(h => b.ShipById(h.ShipId)?.Side == 1).Sum(h => h.Absorbed):0} hull {hits.Where(h => b.ShipById(h.ShipId)?.Side == 1).Sum(h => h.HullDamage):0} (last {Battle.HitMemory} hits)");
        _o.WriteLine($"{label} enemy shot kinds: {string.Join(", ", kinds.Select(k => k.Key + "=" + k.Value))}; our shields {string.Join(" ", b.Ships.Where(s => s.Side == 0).Select(s => s.Name + ":" + string.Join("/", s.Shields.Select(v => v.ToString("0")))))}");
        _o.WriteLine($"{label}: t={b.T:0}s over={b.Over} shots ours {shots[0]} theirs {shots[1]}; hull lost ours {hull0[0] - hull1[0]:0} theirs {hull0[1] - hull1[1]:0}; enemies {string.Join(", ", b.Ships.Where(s => s.Side == 1).Select(s => $"{s.Name} {s.Class.Id} {s.Stance} hold={s.HoldFire} mounts={s.Mounts.Count}"))}");
    }
    [Fact]
    public void Probe()
    {
        Run("skirmish concord_battle", new Battle(TestData.Data, TestData.Data.ScenarioById("concord_battle")));
        var c = Campaign.Campaign.New(CampaignTests.Camp, "concord");
        Offer? m = null;
        for (int t = 0; t < 6 && m == null; t++) { m = c.OfferMission("patrol"); if (m == null) c.EndTurn(); }
        Run("campaign patrol", new Battle(TestData.Data, MissionRun.Plan(c, m!, TestData.Data)!.Scenario));
    }
}
