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
    public void Every_mission_file_loads() => Assert.Equal(20, Camp.Missions.Count);

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
        // row 7 of the map as the mockup's buildCampaign(seed 7) draws it, before ports and stars: owner initial and terrain.
        // The terrain is now laid out by star systems (Campaign.MakeSpace; mockup 1 to follow, campaign-space 2.1), so
        // only the owners, the territory, are compared
        const string mockup = "cococococacocococonanonononononosososnsnsososokoknkokokokoko";
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        var stars = Camp.Map.Stars.ToHashSet();
        for (int col = 0; col < 30; col++)
        {
            Sector h = c.Hex(col, 7);
            if (h.IsPort || stars.Contains((col, 7))) continue;
            Assert.Equal(mockup[col * 2], h.Owner[0]);
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
        var (sc, sr) = Camp.Map.Stars.First();
        Assert.Null(c.PathTo(sc, sr));
    }

    [Fact]
    public void Space_is_star_systems_with_their_belts_and_nebulae_and_a_deep_between()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        var stars = Camp.Map.Stars.ToList();
        foreach (var (sc, sr) in stars) Assert.Equal("star", c.Hex(sc, sr).Terrain);
        // a system with a belt has asteroids on its ring, and a system with a nebula has one within four sectors
        foreach (StarSystem y in Camp.Map.Systems)
        {
            if (y.BeltR > 0 && y.BeltShare > 0)
                Assert.Contains(c.S.Sectors, h => h.Terrain == "asteroids" && HexGrid.Dist(h.C, h.R, y.C, y.R) == y.BeltR);
            if (y.Nebula > 0)
                Assert.Contains(c.S.Sectors, h => h.Terrain == "nebula" && HexGrid.Dist(h.C, h.R, y.C, y.R) <= 4);
        }
        // dust clouds and asteroid fields with no star: some of each terrain far from every star
        int Far(string t) => c.S.Sectors.Count(h => h.Terrain == t && stars.All(s => HexGrid.Dist(h.C, h.R, s.C, s.R) >= Camp.Map.Deep.ClearOfStars));
        Assert.True(Far("nebula") >= Camp.Map.Deep.DustClouds * Camp.Map.Deep.DustMin / 2, "dust clouds in the deep");
        Assert.True(Far("asteroids") >= Camp.Map.Deep.Fields, "asteroid fields in the deep");
        // every settled world but the rogues lies in a system; Ouro and Sabine are rogues
        Assert.True(c.IsRogue(c.S.Sectors.First(h => h.Planet == "Ouro")));
        Assert.False(c.IsRogue(c.S.Sectors.First(h => h.Planet == "Halcyon")));
        // the same seed lays out the same space
        Campaign.Campaign d = Campaign.Campaign.New(Camp, "concord");
        Assert.Equal(c.S.Sectors.Select(h => h.Terrain), d.S.Sectors.Select(h => h.Terrain));
    }

    [Fact]
    public void The_fog_of_war_has_unexplored_revealed_and_visible_sectors()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        var seen = c.Visible();
        // your own empire is seen, and the far side of the map is unexplored
        Assert.All(c.S.Sectors.Where(h => h.Owner == "concord"), h => Assert.Contains((h.C, h.R), seen));
        Sector far = c.S.Sectors.Where(h => h.Owner == "krai" && c.MissionAt(h.C, h.R) == null).OrderByDescending(h => HexGrid.Dist(h.C, h.R, c.S.AtC, c.S.AtR)).First();
        Assert.False(far.Known);
        Assert.DoesNotContain((far.C, far.R), seen);
        // everything seen is revealed with the empire that holds it
        Assert.All(seen, k => { Sector h = c.Hex(k.C, k.R); Assert.True(h.Known); Assert.Equal(h.Owner, h.KnownOwner); });
        // flying out reveals what the fleet passes; once it has gone on, those sectors stay revealed but are no longer seen
        Sector edge = c.S.Sectors.Where(h => !h.Known && !c.TerrainOf(h).Impassable).OrderBy(h => HexGrid.Dist(h.C, h.R, c.S.AtC, c.S.AtR)).First();
        for (int t = 0; t < 8 && (c.S.AtC, c.S.AtR) != (edge.C, edge.R); t++)
        {
            if (c.PathTo(edge.C, edge.R) is { } r && r.Legs[0].Count > 0) c.Move(r);
            c.EndTurn();
        }
        Assert.True(edge.Known);
        Assert.Contains((edge.C, edge.R), c.Visible());
        // an offered mission's sector is revealed by its intel
        Assert.All(c.S.Missions, m => Assert.True(c.Hex(m.C, m.R).Known));
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
                    : def.Encounter ? new Dictionary<string, object> { ["ship"] = "Larkspur", ["freighter_fac"] = def.Id == "escort" ? side : "vos" }
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
                    if (b.Over == null) b.EndWithdrawn();
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
    public void Each_faction_flies_its_own_approved_freighter_and_one_with_none_the_stand_in()
    {
        Assert.Equal("fr", Camp.FreighterOf("concord").BattleClass);
        Assert.Equal("krai_fr", Camp.FreighterOf("krai").BattleClass);
        Assert.Equal("hunt_fr", Camp.FreighterOf("ssarith").BattleClass);
        Assert.Equal("freighter", Camp.FreighterOf("vos").Id);
        Assert.All(new[] { "concord", "krai", "ssarith" }, f => Assert.False(Camp.FreighterOf(f).ForSale));
        // the convoy missions field them, not warships
        foreach (string id in new[] { "defend", "ambush", "distress", "escort", "raid" })
            Assert.Contains(Camp.Missions[id].Battles.SelectMany(b => b.Groups), g => g.Hull == "freighter");
    }

    [Fact]
    public void An_encounter_is_never_offered_at_random_only_on_meeting_a_freighter()
    {
        Assert.DoesNotContain(Camp.Contracts, m => m.Encounter);
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        for (int t = 0; t < 12; t++) { c.EndTurn(); Assert.DoesNotContain(c.S.Missions, m => c.ScriptOf(m).Encounter); }
    }

    // the fleet put in a freighter's sector (as if it had flown there)
    private static Freighter Beside(Campaign.Campaign c, string fac)
    {
        Freighter f = c.S.Freighters.First(x => x.Fac == fac);
        (c.S.AtC, c.S.AtR) = f.Here();
        return f;
    }

    [Fact]
    public void Sharing_a_sector_with_a_freighter_offers_her_escort_if_she_is_friendly_and_a_raid_if_not()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        Freighter own = Beside(c, "concord");
        Assert.Contains(own, c.FreightersAt(c.S.AtC, c.S.AtR));
        Offer e = c.Meet(own)!;
        Assert.Equal("escort", e.Script);
        Assert.Equal((own.Name, "concord"), (e.Value("ship"), e.Value("freighter_fac")));
        Assert.Same(e, c.Meet(own));   // one encounter a freighter a turn
        Freighter rival = Beside(c, "krai");
        Assert.Null(c.Meet(own));      // she is not here
        Assert.Equal("raid", c.Meet(rival)!.Script);
        // gone at the end of the turn, when she moves on
        c.EndTurn();
        Assert.DoesNotContain(c.S.Missions, m => c.ScriptOf(m).Encounter);
    }

    [Fact]
    public void A_raided_freighter_that_strikes_joins_the_fleet_with_her_cargo_and_is_cashed_out_at_a_port()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        Freighter f = Beside(c, "krai");
        Offer m = c.Meet(f)!;
        MissionPlan plan = MissionRun.Plan(c, m, TestData.Data)!;
        var b = new Battle(TestData.Data, plan.Scenario);
        Ship fr = b.ShipById(plan.Groups["freighter"][0])!;
        Assert.Equal("krai_fr", fr.Class.Id);
        Assert.True(fr.Prize);
        Assert.Equal(Stance.Hold, fr.Stance);   // she keeps her course, never closing to fight
        fr.Hull = fr.Class.HullHp * 0.2;
        foreach (string id in plan.Groups["escorts"]) { Ship s = b.ShipById(id)!; s.Hull = 0; s.Alive = false; }
        b.Step();
        Assert.True(fr.Struck);
        int fleet = c.S.Fleet.Count;
        Debrief d = MissionRun.Finish(c, m, plan, b);
        Assert.True(d.Win);
        CampShip haul = c.S.Fleet.Single(s => s.Haul > 0);
        Assert.Equal(fleet + 1, c.S.Fleet.Count);
        Assert.Equal(f.Name, haul.Name);
        Assert.Equal("freighter_krai", haul.Hull);
        Assert.Equal((int)Math.Round(m.Reward * 3), haul.Haul);
        Assert.Equal(c.Data.Rules.FreighterRefitTurns, f.Out);   // off her route while she is replaced
        Assert.DoesNotContain(f, c.FreightersAt(c.S.AtC, c.S.AtR));
        // she sails with the fleet but stays out of its battles
        c.S.Missions.Clear();
        MissionPlan next = MissionRun.Plan(c, c.OfferMission("patrol")!, TestData.Data)!;
        Assert.Equal(fleet, next.FleetShips.Count);
        Assert.NotNull(c.Sell(haul));   // not sold as a hull: cashed out
        // cashed out at a port of your own
        Assert.NotNull(c.CashOut());   // not here, at sea
        (c.S.AtC, c.S.AtR) = (c.S.HomeC, c.S.HomeR);
        int credits = c.S.Credits;
        Assert.Null(c.CashOut());
        Assert.Equal(credits + haul.Haul, c.S.Credits);
        Assert.DoesNotContain(haul, c.S.Fleet);
    }

    [Fact]
    public void Cashing_out_freighters_that_were_all_the_fleet_had_commissions_a_new_ship_at_home()
    {
        // found in the Krai playtest (2026-11-08): the warship lost taking her, the sale left no fleet and the map crashed
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        CampShip prize = c.NewShip("freighter_krai", "Ember Reach");
        prize.Maker = "krai";
        prize.Haul = 900;
        c.S.Fleet.Clear();
        c.S.Fleet.Add(prize);
        Sector port = c.S.Sectors.First(h => c.IsPort(h) && (h.C, h.R) != (c.S.HomeC, c.S.HomeR));
        (c.S.AtC, c.S.AtR) = (port.C, port.R);
        Assert.Null(c.CashOut());
        CampShip fresh = Assert.Single(c.S.Fleet);
        Assert.Equal(0, fresh.Haul);
        Assert.Equal((c.S.HomeC, c.S.HomeR), (c.S.AtC, c.S.AtR));
    }

    [Fact]
    public void A_boarded_and_captured_ship_counts_as_taken()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        Offer m = c.Meet(Beside(c, "krai"))!;
        MissionPlan plan = MissionRun.Plan(c, m, TestData.Data)!;
        var b = new Battle(TestData.Data, plan.Scenario);
        Ship fr = b.ShipById(plan.Groups["freighter"][0])!;
        fr.Captured = true;
        fr.Side = 0;
        Assert.True(MissionRun.MetNow(c, plan, b)["cargo"]);
    }

    [Fact]
    public void Ships_do_not_mend_between_battles_but_at_a_port_or_with_a_repair_kit()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        CampShip s = c.S.Fleet[0];
        s.HullPct = 40;
        for (int t = 0; t < 5; t++) c.EndTurn();
        Assert.Equal(40, s.HullPct);
        // a kit mends a quarter, anywhere; none left, none mended
        s.Supplies[Campaign.Campaign.KitSupply] = 1;
        Assert.Null(c.FieldRepair(s));
        Assert.Equal(40 + c.Data.Rules.KitRepairPct, s.HullPct);
        Assert.Equal(0, c.Kits);
        Assert.NotNull(c.FieldRepair(s));
        Assert.Equal(40 + c.Data.Rules.KitRepairPct, s.HullPct);
        // never past whole
        s.HullPct = 90;
        s.Supplies[Campaign.Campaign.KitSupply] = 1;
        Assert.Null(c.FieldRepair(s));
        Assert.Equal(100, s.HullPct);
        Assert.NotNull(c.FieldRepair(s));
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
    public void Patrols_are_offered_often_and_in_nebulae_belts_and_orbits()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        var patrols = new[] { "patrol", "nebula_sweep", "belt_patrol", "orbital_patrol" };
        int n = 0, p = 0;
        for (int i = 0; i < 300; i++)
        {
            c.S.Missions.Clear();
            if (c.OfferMission() is not { } m) continue;
            n++;
            if (patrols.Contains(m.Script)) p++;
            Sector h = c.Hex(m.C, m.R);
            if (m.Script == "nebula_sweep") Assert.Equal("nebula", h.Terrain);
            if (m.Script == "belt_patrol") Assert.Equal("asteroids", h.Terrain);
            if (m.Script == "orbital_patrol") Assert.NotNull(h.Planet);
        }
        // the four patrols weigh 10 of the contracts' 21: close to half of what is offered
        Assert.InRange(p / (double)n, 0.35, 0.65);
    }

    [Fact]
    public void A_story_mission_refuses_the_withdraw_and_fights_on()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        c.S.Missions.Clear();
        Offer m = c.OfferMission("shakedown")!;
        MissionPlan plan = MissionRun.Plan(c, m, TestData.Data)!;
        Assert.False(plan.Scenario.CanWithdraw);
        var b = new Battle(TestData.Data, plan.Scenario);
        Assert.False(b.Withdraw());
        Assert.False(b.Withdrawing);
        Assert.False(b.Player.Warp.Armed);
        Assert.Null(b.Over);
    }

    [Fact]
    public void A_contract_may_be_withdrawn_from_and_a_story_mission_may_not()
    {
        foreach (MissionScript d in Camp.Missions.Values) Assert.Equal(!d.Story, d.CanWithdraw);
    }

    [Fact]
    public void Withdrawing_from_an_ambush_meets_the_breakout_and_is_a_victory()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        c.S.Missions.Clear();
        Offer m = c.OfferMission("ambushed")!;
        MissionPlan plan = MissionRun.Plan(c, m, TestData.Data)!;
        var b = new Battle(TestData.Data, plan.Scenario);
        // the ambushers held far off, so the warp drive charges, plots and jumps the ship out
        foreach (Ship e in b.Ships.Where(x => x.Side != b.Player.Side)) { e.X += 1e7; e.Z += 1e7; }
        Assert.True(b.Withdraw());
        Assert.True(b.Player.Warp.Armed && b.Player.Warp.Plotting);
        for (int i = 0; i < 60000 && b.Over == null; i++) b.Step();
        Assert.True(b.Over == "jumped", $"banks {b.Player.Warp.Banks} plot {b.Player.Warp.Plot} warp power {b.Player.Power["warp"]} why '{b.WarpRiskOf(b.Player).Why}' t {b.T}");
        // unopposed, the drive charges, plots and jumps within a minute (about 21 s)
        Assert.True(b.T < 60, $"warped out after {b.T:0.0} s");
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
                    : f.EndsWith("escort.json") || f.EndsWith("raid.json") ? new Dictionary<string, object> { ["ship"] = "Larkspur", ["freighter_fac"] = "krai" }
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
    public void Fitting_is_free_and_takes_from_the_stores()
    {
        Campaign.Campaign c = New();
        CampShip s = c.S.Fleet[0];
        string old = s.Fit["primary"][0]!;
        Assert.NotNull(c.Refit(s, "primary", 0, "lance"));
        int before = c.S.Credits;
        Assert.Null(c.BuySystem("lance"));
        Assert.Equal(before - c.SystemPrice("lance"), c.S.Credits);
        before = c.S.Credits;
        Assert.Null(c.Refit(s, "primary", 0, "lance"));
        Assert.Equal(before, c.S.Credits);
        Assert.Equal("lance", s.Fit["primary"][0]);
        Assert.Equal(0, c.InStores("lance"));
        Assert.Equal(1, c.InStores(old));
    }

    [Fact]
    public void Removing_a_system_stows_it_and_selling_it_pays_the_yard_share()
    {
        Campaign.Campaign c = New();
        CampShip s = c.S.Fleet[0];
        string old = s.Fit["primary"][0]!;
        int before = c.S.Credits;
        Assert.Null(c.Refit(s, "primary", 0, null));
        Assert.Equal(before, c.S.Credits);
        Assert.Equal(1, c.InStores(old));
        Assert.Null(c.SellSystem(old));
        Assert.Equal(before + c.SystemSale(old), c.S.Credits);
        Assert.Equal(0, c.InStores(old));
        Assert.NotNull(c.SellSystem(old));
    }

    [Fact]
    public void Empty_hardpoints_take_stock_from_the_stores_and_buy_the_rest()
    {
        Campaign.Campaign c = New();
        CampShip s = c.S.Fleet[0];
        c.Refit(s, "primary", 0, null);
        c.Refit(s, "shields", 0, null);
        c.SellSystem(c.Data.Stock["shields"]);
        Assert.Equal(2, c.EmptyHardpoints(s).Count);
        int quote = c.StockQuote(s), before = c.S.Credits;
        Assert.Equal(c.SystemPrice(c.Data.Stock["shields"]), quote);
        Assert.Null(c.FitStock(s));
        Assert.Empty(c.EmptyHardpoints(s));
        Assert.Equal(before - quote, c.S.Credits);
        Assert.Empty(c.S.Stores);
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
    public void Every_station_offers_three_experienced_officers_at_different_levels_and_five_cadets()
    {
        Campaign.Campaign c = New();
        foreach (string st in c.Data.Officers.Stations)
        {
            var offer = c.OnOffer(st);
            Assert.Equal(5, offer.Count(c.IsCadet));
            var exp = offer.Where(o => !c.IsCadet(o)).ToList();
            Assert.Equal(3, exp.Count);
            Assert.Equal(3, exp.Select(o => o.Skill).Distinct().Count());
        }
        var names = c.S.OfficerPool.Select(o => o.Name).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void A_station_keeps_its_list_after_a_hire_and_one_officer_serves_it()
    {
        Campaign.Campaign c = New();
        c.S.Credits = 100000;
        string st = c.Data.Officers.Stations[0];
        Officer a = c.OnOffer(st).First(o => !c.IsCadet(o));
        Assert.Null(c.Hire(a));
        Assert.Same(a, c.AtStation(st));
        Assert.DoesNotContain(a, c.S.OfficerPool);
        Assert.Equal(3, c.OnOffer(st).Count(o => !c.IsCadet(o)));
        Assert.Equal(5, c.OnOffer(st).Count(c.IsCadet));
        Officer b = c.OnOffer(st).First(c.IsCadet);
        Assert.Null(c.Hire(b));
        Assert.Same(b, c.AtStation(st));
        Assert.Contains(a, c.S.OfficerPool);
        Assert.Equal(3, c.OnOffer(st).Count(o => !c.IsCadet(o)));
        Assert.Equal(5, c.OnOffer(st).Count(c.IsCadet));
        c.Dismiss(b);
        Assert.Null(c.AtStation(st));
        Assert.Contains(b, c.S.OfficerPool);
        Assert.Equal(8, c.OnOffer(st).Count);
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
