// Campaign and mission tests (openspec/changes/mission-scripting, game-flow-engine): the expression language, the mission
// files and the campaign data as they are in data/, a new campaign built the way the mockup builds it, moves, the factions'
// turn, every mission offered, briefed, fought and graded, and a save loaded back.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FleetCommand.Campaign;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public sealed class CampaignTests
{
    private sealed class Vars : IScope
    {
        private readonly Dictionary<string, object> _v;
        public Vars(Dictionary<string, object> v) { _v = v; }
        public object Get(string n) => _v.TryGetValue(n, out object? v) ? v : throw new DataError("no " + n);
        public object Call(string n, IReadOnlyList<object> a) => n == "twice" ? Expr.ToNum(a[0], n) * 2 : throw new DataError("no " + n);
    }

    private static readonly Vars V = new(new() { ["strength"] = 2.0, ["side"] = "concord", ["trap"] = 0.0, ["empty"] = "" });

    public static IEnumerable<string> MissionFiles =>
        Directory.GetFiles(Path.Combine(TestData.DataDir, "missions"), "*.json").Select(f => "missions/" + Path.GetFileName(f));

    private static CampaignData? _camp;
    public static CampaignData Camp => _camp ??= CampaignData.Load(TestData.Read, MissionFiles, TestData.Data);

    [Theory]
    [InlineData("1 + 2 * 3", 7)]
    [InlineData("(1 + 2) * 3", 9)]
    [InlineData("strength + 1", 3)]
    [InlineData("strength >= 2 && side == 'concord'", 1)]
    [InlineData("!trap", 1)]
    [InlineData("trap ? 5 : 6", 6)]
    [InlineData("round10(113)", 110)]
    [InlineData("twice(strength) - 1", 3)]
    [InlineData("min(3, max(1, 2))", 2)]
    [InlineData("-strength + 10 % 4", 0)]
    [InlineData("1 < 2 && 2 <= 2 && 3 > 2 && 3 != 4", 1)]
    public void An_expression_evaluates_to_its_number(string text, double want) => Assert.Equal(want, Expr.Parse(text, "test").Num(V));

    [Fact]
    public void Or_gives_the_first_value_that_is_set() => Assert.Equal("vos", Expr.Parse("empty || 'vos'", "t").Str(V));

    [Fact]
    public void A_template_fills_its_parts() =>
        Assert.Equal("Strength 2 of {3}", Template.Parse("Strength {strength} of {{{strength + 1}}}", "t").Eval(V));

    [Theory]
    [InlineData("1 +")]
    [InlineData("(1")]
    [InlineData("'open")]
    [InlineData("1 2")]
    [InlineData("a ? b")]
    public void A_bad_expression_fails_naming_where(string text) =>
        Assert.Contains("here.field", Assert.Throws<DataError>(() => Expr.Parse(text, "here.field")).Message);

    [Fact]
    public void Every_mission_file_loads() => Assert.Equal(15, Camp.Missions.Count);

    [Fact]
    public void A_mission_naming_something_the_campaign_lacks_is_refused()
    {
        string text = TestData.Read("missions/patrol.json").Replace("plain && (own || neutral)", "plain && friendly");
        var e = Assert.Throws<DataError>(() => MissionLoader.Load(text, "data/missions/patrol.json"));
        Assert.Contains("unknown name 'friendly'", e.Message);
        Assert.Contains("data/missions/patrol.json.where", e.Message);
    }

    [Fact]
    public void A_mission_with_an_unknown_key_is_refused()
    {
        string text = TestData.Read("missions/patrol.json").Replace("\"glyph\"", "\"colour\": \"red\", \"glyph\"");
        Assert.Contains("colour: unknown key", Assert.Throws<DataError>(() => MissionLoader.Load(text, "p")).Message);
    }

    [Fact]
    public void The_random_numbers_are_the_mockups()
    {
        var r = new Mulberry32(7);
        Assert.Equal(0.011704753153026104, r.Next(), 15);
        Assert.Equal(0.06195825757458806, r.Next(), 15);
        Assert.Equal(0.97690763277933, r.Next(), 15);
    }

    [Fact]
    public void A_new_campaign_draws_the_mockups_territory()
    {
        // row 7 of the map as the mockup's buildCampaign(seed 7) draws it, before ports and stars: owner initial and terrain
        const string mockup = "cococococacocococonanonononononosososnsnsososokoknkokokokoko";
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        var stars = Camp.Map.Stars.ToHashSet();
        for (int col = 0; col < 30; col++)
        {
            Sector h = c.Hex(col, 7);
            if (h.IsPort || stars.Contains((col, 7))) continue;
            Assert.Equal(mockup.Substring(col * 2, 2), $"{h.Owner[0]}{h.Terrain[0]}");
        }
    }

    [Fact]
    public void A_new_campaign_starts_at_home_with_the_smallest_hull_and_marines()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "krai");
        Assert.Equal("Vahr", c.At.PortName);
        Assert.Single(c.S.Fleet);
        Assert.Equal("krai_cv", c.S.Fleet[0].Hull);
        Assert.True(c.Marines > 0);
        Assert.Contains(c.S.Missions, m => m.Script == "shakedown");
        Assert.True(c.S.Missions.Count >= 5);
        Assert.Equal(40, c.Standing("krai"));
        Assert.Equal(-40, c.Standing("concord"));
    }

    [Fact]
    public void A_route_longer_than_a_turn_is_split_into_legs_and_flown_on_after_end_turn()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        Route r = c.PathTo(c.S.HomeC + 8, c.S.HomeR)!;
        Assert.True(r.Turns >= 3);
        Assert.True(r.Legs[0].Sum(k => c.TerrainOf(c.Hex(k.C, k.R)).Cost) <= c.S.Mp);
        c.Move(r);
        Assert.NotNull(c.S.OrderC);
        for (int t = 0; t < 10 && c.S.OrderC != null; t++)
        {
            c.EndTurn();
            if (c.OrderRoute() is { } next) c.Move(next);
        }
        Assert.Null(c.S.OrderC);
        Assert.Equal((c.S.HomeC + 8, c.S.HomeR), (c.S.AtC, c.S.AtR));
    }

    [Fact]
    public void No_path_enters_a_star()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        var (sc, sr) = Camp.Map.Stars[0];
        Assert.Null(c.PathTo(sc, sr));
    }

    [Fact]
    public void The_factions_move_their_fleets_and_freighters()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        var before = c.S.Groups.Select(g => (g.C, g.R)).ToList();
        var fr = c.S.Freighters.Select(f => f.I).ToList();
        for (int i = 0; i < 3; i++) c.EndTurn();
        Assert.NotEqual(before, c.S.Groups.Select(g => (g.C, g.R)).ToList());
        Assert.NotEqual(fr, c.S.Freighters.Select(f => f.I).ToList());
        foreach (FactionFleet g in c.S.Groups) Assert.False(c.TerrainOf(c.Hex(g.C, g.R)).Impassable);
    }

    [Fact]
    public void A_saved_campaign_loads_back_and_draws_on_the_same()
    {
        Campaign.Campaign a = Campaign.Campaign.New(Camp, "concord");
        a.EndTurn();
        Campaign.Campaign b = Campaign.Campaign.Load(Camp, a.Save());
        a.EndTurn();
        b.EndTurn();
        Assert.Equal(a.Save(), b.Save());
    }

    /// <summary>Every mission is offered in some sector of a new campaign (over a few turns), briefed, and either done on
    /// arrival or fought: its battle planned into a scenario, played to the end by the AI, graded and its result applied.</summary>
    [Fact]
    public void Every_mission_can_be_offered_briefed_fought_and_graded()
    {
        foreach (MissionScript def in Camp.Missions.Values)
        {
            foreach (string side in new[] { "concord", "krai", "ssarith" })
            {
                Campaign.Campaign c = Campaign.Campaign.New(Camp, side);
                c.S.Missions.Clear();
                var data = def.Id == "prize_home"
                    ? new Dictionary<string, object> { ["fac"] = "krai", ["tier"] = 2.0, ["hull"] = "krai_ff", ["cls"] = "frigate", ["prize_name"] = "Vekk", ["taken_grade"] = "outstanding" }
                    : null;
                Offer? m = null;
                for (int t = 0; t < 6 && m == null; t++) { m = c.OfferMission(def.Id, data: data); if (m == null) c.EndTurn(); }
                // the Hunt, playable once its console was built, has no rival yet (data/campaign/campaign.json), so the one
                // mission that needs a hostile planet is not offered to it
                if (m == null && side == "ssarith" && def.Id == "planet") continue;
                Assert.True(m != null, $"{def.Id} ({side}) is offered nowhere");
                Assert.False(string.IsNullOrWhiteSpace(c.Brief(m!)), def.Id);
                Assert.DoesNotContain("{", c.Brief(m!));
                Assert.False(string.IsNullOrWhiteSpace(c.Objective(m!)), def.Id);
                c.S.Fleet[0].Supplies["marines"] = 2;
                int credits = c.S.Credits;
                MissionPlan? plan = MissionRun.Plan(c, m!, TestData.Data);
                // every mission is flown: none is done on arrival (owner, 2026-11-06: "you just skipped the mission!")
                Assert.True(plan != null, $"{def.Id} ({side}) has no battle");
                Debrief d;
                {
                    Assert.Equal(side, plan.Scenario.Side);
                    Assert.Contains(plan.Scenario.Ships, s => s.Side == 1);
                    var b = new Battle(TestData.Data, plan.Scenario);
                    for (int i = 0; i < 20000 && b.Over == null; i++) b.Step();
                    if (b.Over == null) b.Withdraw();
                    d = MissionRun.Finish(c, m!, plan, b);
                    Assert.Equal(plan.Battle.Objectives.Count, d.Objectives.Count);
                }
                Assert.Contains(d.Grade, MissionLoader.GradeIds);
                Assert.DoesNotContain(c.S.Missions, x => x.Id == m!.Id);
                Assert.True(c.S.Fleet.Count >= 1);
                Assert.Equal(credits + d.Credits, c.S.Credits);
            }
        }
    }

    [Fact]
    public void A_prize_beaten_to_the_capture_line_strikes_and_is_taken()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        c.S.Missions.Clear();
        Offer m = c.OfferMission("prize")!;
        MissionPlan plan = MissionRun.Plan(c, m, TestData.Data)!;
        var b = new Battle(TestData.Data, plan.Scenario);
        string prize = plan.Groups["prize"][0];
        Ship p = b.ShipById(prize)!;
        Assert.True(p.Prize);
        p.Hull = p.Class.HullHp * 0.2;
        foreach (string e in plan.Groups["escorts"]) { Ship s = b.ShipById(e)!; s.Hull = 0; s.Alive = false; }
        b.Step();
        Assert.True(p.Struck);
        Assert.Equal("won", b.Over);
        Debrief d = MissionRun.Finish(c, m, plan, b);
        Assert.True(d.Win);
        Assert.Contains(c.S.Missions, x => x.Script == "prize_home" && x.C == c.S.HomeC && x.R == c.S.HomeR);
    }

    [Fact]
    public void The_shakedown_is_flown_to_its_beacon_shot_at_its_drones_and_back_and_ends_only_then()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        Offer m = c.S.Missions.First(x => x.Script == "shakedown");
        MissionPlan plan = MissionRun.Plan(c, m, TestData.Data)!;
        Assert.True(plan.Scenario.EndsOnObjectives);
        var b = new Battle(TestData.Data, plan.Scenario);
        Ship me = b.Player;
        MarkerDef beacon = plan.Scenario.Markers!.First(k => k.Id == "beacon"), home = plan.Scenario.Markers!.First(k => k.Id == "home");
        // the drones stand still and hold their fire
        foreach (string id in plan.Groups["drones"]) { Ship d0 = b.ShipById(id)!; Assert.True(d0.HoldFire); Assert.Equal(0, d0.V); }
        // the start point does not count before the beacon
        b.Step();
        Assert.DoesNotContain("home", b.Reached);
        // the drones destroyed: the battle goes on, the beacon and the way home still to fly
        foreach (string id in plan.Groups["drones"]) { Ship d0 = b.ShipById(id)!; d0.Hull = 0; d0.Alive = false; }
        b.Step();
        Assert.Null(b.Over);
        me.X = beacon.XM; me.Z = beacon.ZM; b.Step();
        Assert.Contains("beacon", b.Reached);
        Assert.False(MissionRun.MetNow(c, plan, b)["home"]);
        me.X = home.XM; me.Z = home.ZM; b.Step();
        var met = MissionRun.MetNow(c, plan, b);
        Assert.True(plan.Battle.Objectives.Where(o => o.Primary).All(o => met[o.Id]));
        b.Win();
        Debrief d = MissionRun.Finish(c, m, plan, b);
        Assert.True(d.Win);
        Assert.Contains(c.S.Missions, x => x.Script == "first_blood");
    }

    [Fact]
    public void A_mission_asking_for_a_beacon_it_does_not_place_is_refused_on_load()
    {
        string json = TestData.Read("missions/shakedown.json").Replace("reached('home')", "reached('hmoe')");
        var e = Assert.Throws<DataError>(() => MissionLoader.Load(json, "missions/shakedown.json"));
        Assert.Contains("hmoe", e.Message);
    }

    [Fact]
    public void Withdrawing_from_an_ambush_meets_the_breakout_and_is_a_victory()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        c.S.Missions.Clear();
        Offer m = c.OfferMission("ambushed")!;
        MissionPlan plan = MissionRun.Plan(c, m, TestData.Data)!;
        var b = new Battle(TestData.Data, plan.Scenario);
        b.Withdraw();
        Debrief d = MissionRun.Finish(c, m, plan, b);
        Assert.Equal("withdraw", d.How);
        Assert.True(d.Objectives.First().Met);
        Assert.Contains(d.Grade, new[] { "victory", "outstanding" });
        Assert.Equal(5, d.Prestige);
    }

    [Fact]
    public void The_shipyard_sells_hulls_at_home_for_credits()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        int price = c.HullPrice("petrel");
        Assert.Null(c.Buy("petrel"));
        Assert.Equal(Camp.Rules.StartCredits - price, c.S.Credits);
        Assert.Equal(2, c.S.Fleet.Count);
        Assert.NotNull(c.Buy("krai_cv"));
        Assert.Null(c.Sell(c.S.Fleet[1]));
        Assert.NotNull(c.Sell(c.S.Fleet[0]));
    }

    [Fact]
    public void Every_hull_with_a_schematic_has_both_layers()
    {
        string assets = Path.Combine(Path.GetDirectoryName(TestData.DataDir)!, "assets", "ships", "schematics");
        foreach (Hull h in Camp.Hulls.Values.Where(h => h.Schematic != ""))
            foreach (string layer in new[] { "hull", "wire" })
                Assert.True(File.Exists(Path.Combine(assets, h.Schematic + $".{layer}.png")), $"{h.Id}: {h.Schematic}.{layer}.png");
    }
}

public sealed class HexGridTests
{
    [Fact]
    public void A_point_of_the_map_falls_in_the_sector_drawn_there()
    {
        for (int c = 0; c < 30; c++)
            for (int r = 0; r < 22; r++)
            {
                var (x, z) = HexGrid.Pos(c, r);
                Assert.Equal((c, r), HexGrid.At(x, z));
                Assert.Equal((c, r), HexGrid.At(x + 0.6, z - 0.3));
            }
    }
}

public sealed class MissionPreviewTests
{
    [Fact]
    public void Every_mission_file_previews_and_ends_each_way()
    {
        foreach (string f in CampaignTests.MissionFiles)
            foreach (MissionPreview.End end in Enum.GetValues<MissionPreview.End>())
            {
                var data = f.EndsWith("prize_home.json")
                    ? new Dictionary<string, object> { ["fac"] = "krai", ["tier"] = 2.0, ["hull"] = "krai_ff", ["cls"] = "frigate", ["prize_name"] = "Vekk", ["taken_grade"] = "victory" }
                    : null;
                PreviewResult r = MissionPreview.Run(TestData.Read, CampaignTests.MissionFiles, f, TestData.Read(f), "concord", 7, data, end);
                Assert.True(r.Ok, $"{f} {end}: {r.Error}");
                Assert.Contains("BRIEFING", r.Report);
            }
    }

    [Fact]
    public void A_broken_file_previews_as_its_loader_error()
    {
        string f = "missions/patrol.json";
        PreviewResult r = MissionPreview.Run(TestData.Read, CampaignTests.MissionFiles, f, TestData.Read(f).Replace("alive(raiders)", "alive(raiders"), "concord", 7, null, null);
        Assert.False(r.Ok);
        Assert.Contains("data/missions/patrol.json", r.Error);
    }
}

public sealed class ShopTests
{
    private static Campaign.Campaign New() => Campaign.Campaign.New(CampaignTests.Camp, "concord");

    [Fact]
    public void A_refit_costs_exactly_its_quote()
    {
        Campaign.Campaign c = New();
        CampShip s = c.S.Fleet[0];
        int quote = c.RefitQuote(s, "primary", 0, "lance"), before = c.S.Credits;
        Assert.Null(c.Refit(s, "primary", 0, "lance"));
        Assert.Equal(before - quote, c.S.Credits);
        Assert.True(c.RefitQuote(s, "primary", 0, null) < 0);
    }

    [Fact]
    public void Empty_hardpoints_are_filled_with_stock_for_their_quote()
    {
        Campaign.Campaign c = New();
        CampShip s = c.S.Fleet[0];
        c.Refit(s, "primary", 0, null);
        c.Refit(s, "shields", 0, null);
        Assert.Equal(2, c.EmptyHardpoints(s).Count);
        int quote = c.StockQuote(s), before = c.S.Credits;
        Assert.Null(c.FitStock(s));
        Assert.Empty(c.EmptyHardpoints(s));
        Assert.Equal(before - quote, c.S.Credits);
    }

    [Fact]
    public void Restocking_fills_every_ship_for_its_quote()
    {
        Campaign.Campaign c = New();
        int quote = c.RestockQuote(), before = c.S.Credits;
        Assert.True(quote > 0);
        Assert.Null(c.Restock());
        Assert.Equal(before - quote, c.S.Credits);
        foreach (Supply u in CampaignTests.Camp.Supplies) Assert.Equal(c.SupplyMax(c.S.Fleet[0], u.Id), c.S.Fleet[0].Supplies[u.Id]);
        Assert.Equal(0, c.RestockQuote());
    }

    [Fact]
    public void One_officer_serves_each_station()
    {
        Campaign.Campaign c = New();
        c.S.Credits = 100000;
        Officer a = c.S.OfficerPool[0];
        Assert.Null(c.Hire(a));
        var b = new Officer { Name = "Test Officer", Station = a.Station, Skill = "Green", Cost = 150 };
        c.S.OfficerPool.Add(b);
        Assert.Null(c.Hire(b));
        Assert.Same(b, c.AtStation(a.Station));
        Assert.Contains(a, c.S.OfficerPool);
        c.Dismiss(b);
        Assert.Null(c.AtStation(a.Station));
    }

    [Fact]
    public void Repairs_are_priced_per_ship()
    {
        Campaign.Campaign c = New();
        c.S.Fleet[0].HullPct = 50;
        int one = c.RepairCost(c.S.Fleet[0]);
        Assert.True(one > 0);
        Assert.Equal(one, c.RepairCost());
        Assert.Null(c.Repair(c.S.Fleet[0]));
        Assert.Equal(100, c.S.Fleet[0].HullPct);
    }
}
