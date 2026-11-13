// Crew leveling (change crew-leveling, mockup 39): the rules of Campaign.Crew and the battle's reading of the crew's skills,
// on the real data (data/campaign/crew.json), as sentences.
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Campaign;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public sealed class CrewTests
{
    private static Campaign.Campaign New() { var c = Campaign.Campaign.New(CampaignTests.Camp, "concord"); c.S.Prestige = 0; return c; }

    private static Officer Seat(Campaign.Campaign c, string station, int level)
    {
        var o = new Officer { Name = "Test " + station, Station = station, Level = level };
        if (c.AtStation(station) is { } was) c.S.Crew.Remove(was);
        c.S.Crew.Add(o);
        return o;
    }

    [Fact]
    public void Every_station_has_a_tree_of_fourteen_skills_whose_prerequisites_are_its_own()
    {
        CrewDef cd = CampaignTests.Camp.Crew;
        Assert.Equal(CampaignTests.Camp.Officers.Stations.OrderBy(s => s), cd.Trees.Keys.OrderBy(s => s));
        foreach (CrewTree t in cd.Trees.Values)
        {
            Assert.Equal(14, t.Skills.Count);
            Assert.Equal(3, t.Skills.Count(s => s.Depth == 1));
            Assert.All(t.Skills.SelectMany(s => s.All.Concat(s.Any)), id => Assert.NotNull(t.Skill(id)));
        }
    }

    [Fact]
    public void An_ensign_has_no_skill_point_and_brings_nothing()
    {
        Campaign.Campaign c = New();
        Officer o = Seat(c, "Helm", 1);
        Assert.Equal(0, c.Points(o));
        Assert.Equal("Ensign", c.RankOf(o));
        Assert.NotNull(c.Learn(o, "helm.b1"));
        Assert.Empty(c.CrewBonus());
    }

    [Fact]
    public void Three_victories_bring_an_ensign_to_the_next_level_for_no_prestige_and_one_point()
    {
        Campaign.Campaign c = New();
        Officer o = Seat(c, "Helm", 1);
        for (int i = 0; i < 3; i++) c.CrewGains(c.BattleXp("victory"));
        Assert.Equal(0, c.PromoteCost(o));
        Assert.Null(c.Promote(o));
        Assert.Equal(2, o.Level);
        Assert.Equal("Lieutenant", c.RankOf(o));
        Assert.Equal(1, c.Points(o));
        Assert.Equal(0, c.S.Prestige);
    }

    [Fact]
    public void A_promotion_short_of_experience_is_paid_in_prestige_at_the_data_rate()
    {
        Campaign.Campaign c = New();
        Officer o = Seat(c, "Helm", 1);
        o.Xp = 200;   // 100 short of 300
        int rate = c.Data.Crew.XpPerPrestige;
        Assert.Equal((int)System.Math.Ceiling(100.0 / rate), c.PromoteCost(o));
        Assert.NotNull(c.Promote(o));   // no prestige yet
        c.S.Prestige = 50;
        Assert.Null(c.Promote(o));
        Assert.Equal(50 - (int)System.Math.Ceiling(100.0 / rate), c.S.Prestige);
        Assert.Equal(0, o.Xp);
    }

    [Fact]
    public void A_crossover_opens_from_either_lane_and_the_keystone_needs_both_crossovers()
    {
        Campaign.Campaign c = New();
        Officer o = Seat(c, "Helm", 10);
        CrewTree t = c.TreeOf(o);
        Assert.Null(c.Learn(o, "helm.a1"));
        Assert.Null(c.Learn(o, "helm.a2"));
        Assert.True(Campaign.Campaign.Unlocked(o, t.Skill("helm.x1")!));    // from the left lane alone
        Assert.False(Campaign.Campaign.Unlocked(o, t.Skill("helm.x2")!));
        Assert.Null(c.Learn(o, "helm.x1"));
        Assert.NotNull(c.Learn(o, "helm.m"));                              // the keystone needs the other crossover too
        Assert.Null(c.Learn(o, "helm.b1"));
        Assert.Null(c.Learn(o, "helm.b2"));
        Assert.Null(c.Learn(o, "helm.x2"));
        Assert.Null(c.Learn(o, "helm.m"));
    }

    [Fact]
    public void Auto_level_spends_up_the_centre_to_the_capstone_in_six_points()
    {
        Campaign.Campaign c = New();
        Officer o = Seat(c, "Engineering", 7);   // six points
        c.SetAuto(o, true);
        Assert.Equal(0, c.Points(o));
        Assert.Contains("eng.k", o.Skills);
        Assert.Equal(new[] { "eng.b1", "eng.b2", "eng.x1", "eng.x2", "eng.m", "eng.k" }, o.Skills);
    }

    [Fact]
    public void Picking_a_skill_by_hand_turns_auto_level_off_and_retraining_gives_the_points_back()
    {
        Campaign.Campaign c = New();
        Officer o = Seat(c, "Science", 3);
        c.SetAuto(o, true);
        Assert.Equal(2, o.Skills.Count);
        o.Level = 4;
        Assert.Null(c.Learn(o, "sci.c1"));
        Assert.False(o.Auto);
        c.S.Prestige = c.Data.Crew.Retrain;
        Assert.Null(c.Retrain(o));
        Assert.Empty(o.Skills);
        Assert.Equal(3, c.Points(o));
        Assert.Equal(0, c.S.Prestige);
    }

    [Fact]
    public void An_officer_transferred_out_is_worth_their_level_and_banked_experience()
    {
        Campaign.Campaign c = New();
        Officer o = Seat(c, "Comms", 1);
        o.Xp = 300;
        Assert.Null(c.Transfer(o));
        Assert.Equal(300 / c.Data.Crew.XpPerPrestige, c.S.Prestige);
        Assert.Null(c.AtStation("Comms"));
    }

    [Fact]
    public void A_turn_served_gives_every_officer_its_experience()
    {
        Campaign.Campaign c = New();
        Officer o = Seat(c, "Security", 1);
        c.EndTurn();
        Assert.Equal(c.Data.Crew.XpTurn, o.Xp);
    }

    [Fact]
    public void The_crews_skills_reach_the_flagship_and_its_wingmen_in_battle()
    {
        Campaign.Campaign c = New();
        Officer helm = Seat(c, "Helm", 3);
        Assert.Null(c.Learn(helm, "helm.b1"));
        Assert.Null(c.Learn(helm, "helm.b2"));
        Officer comms = Seat(c, "Comms", 3);
        Assert.Null(c.Learn(comms, "com.b1"));
        Assert.Null(c.Learn(comms, "com.b2"));
        Dictionary<string, double> bonus = c.CrewBonus();
        Assert.Equal(0.07, bonus["het"], 6);

        var b = new Battle(TestData.Data, TestData.Data.ScenarioById("concord_battle"));
        double hetBefore = b.Rules.HetChanceNow(b.Player);
        Ship wing = b.Ships.First(s => s != b.Player && s.Side == b.Player.Side);
        b.ManCrew(b.Player.Id, bonus);
        Assert.Equal(hetBefore + 0.07, b.Rules.HetChanceNow(b.Player), 6);
        Assert.Equal(0.025, wing.Skill("hit"), 6);   // Coordination: the wingmen hit more
        Assert.Equal(0, b.Player.Skill("wing_hit"));
    }

    [Fact]
    public void The_flagship_seats_the_campaign_crew_with_their_level_experience_and_skills_and_vacant_stations_stay_empty()
    {
        Campaign.Campaign c = New();
        foreach (Officer was in c.S.Crew.ToList()) c.S.Crew.Remove(was);
        Officer eng = Seat(c, "Engineering", 4);
        eng.Xp = 120;
        Assert.Null(c.Learn(eng, "eng.b1"));
        var b = new Battle(TestData.Data, TestData.Data.ScenarioById("concord_battle"));
        b.SeatCrew(b.Player.Id, c.CrewSeats());
        StationOfficer power = b.Player.Officers["power"]!, repair = b.Player.Officers["repair"]!;
        Assert.Same(power, repair);   // Engineering covers power and repair
        Assert.Equal(eng.Name, power.Name);
        Assert.Equal(4, power.Card!.Level);
        Assert.Equal(120, power.Card.Xp);
        Assert.Equal(c.NextXp(eng), power.Card.NextXp);
        Assert.Equal(1, power.Card.Skills);
        var (ab, v) = Assert.Single(c.TreeOf(eng).Skill("eng.b1")!.Fx);
        Assert.Equal(c.Data.Crew.Bonus(ab, v), Assert.Single(power.Card.Bonuses));
        Assert.Equal(c.RankOf(eng), b.RankOf(power));
        Assert.Null(b.Player.Officers["helm"]);
        Assert.Single(b.Player.Crew);
        Assert.Equal(0, b.OfficerSkill(b.Player, "power"));   // what they bring is their skills (ManCrew), not a roster skill
    }

    [Fact]
    public void An_officers_skills_sum_into_bonuses_by_ability_written_as_the_data_says()
    {
        CrewDef cd = CampaignTests.Camp.Crew;
        var sum = cd.Sum("Helm", new[] { "helm.b1", "helm.b2" });
        Assert.Equal(0.07, Assert.Single(sum, e => e.Ability == "het").Value, 6);
        Assert.Equal(("HET chance", "+7 %"), cd.Bonus("het", 0.07));
        Assert.Equal(("Coil drift", "-30 %"), cd.Bonus("drift", 0.3));
        Assert.Equal(("Safe HETs", "yes"), cd.Bonus("het_safe", 1));
    }
}
