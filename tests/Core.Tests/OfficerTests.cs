// The officers (openspec/changes/officers; the battle console mockup's ROSTER and officerSkill): who sits in each station's
// slot, what their skill counts for in and out of their specialty, the names a faction gives them, and the abilities read
// by the rules built so far, on the real data.
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class OfficerTests
{
    [Fact]
    public void Every_station_of_the_players_ship_starts_with_the_officer_of_its_specialty()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player;
        Assert.Equal(TestData.Data.Systems.Officers.Roster.Count, s.Crew.Count);
        foreach (string st in OfficersDef.Stations) Assert.Equal(st, s.Officers[st]!.Spec);
        Assert.Equal("Sora Vance", s.Officers["helm"]!.Name);
        Assert.Equal(4, Rules.OfficerSkill(s, "repair"));
    }

    [Fact]
    public void An_officer_out_of_their_specialty_counts_half_their_skill_rounded_down()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player;
        s.Officers["helm"] = s.Crew.First(o => o.Spec == "power"); // skill 3
        Assert.Equal(1, Rules.OfficerSkill(s, "helm"));
        s.Officers["helm"] = null;
        Assert.Equal(0, b.OfficerSkill(s, "helm"));
    }

    [Fact]
    public void Enemy_ships_have_no_officers_and_wingmen_have_their_own()
    {
        Battle b = TestData.NewBattle();
        Assert.All(b.Ships.Where(o => o.Side != b.Player.Side), o => Assert.Empty(o.Crew));
        Ship tallis = b.ShipById("tallis")!;
        Assert.NotEmpty(tallis.Crew);
        Assert.All(tallis.Crew, o => Assert.StartsWith("tallis.", o.Id));
        // a second ship of the faction takes the roster's names shifted
        Assert.NotEqual(b.Player.Officers["helm"]!.Name, tallis.Officers["helm"]!.Name);
    }

    [Fact]
    public void The_helm_officer_adds_to_the_high_energy_turn_chance()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player;
        double with = b.Rules.HetChanceNow(s);
        s.Officers["helm"] = null;
        Assert.Equal(2 * b.Data.Systems.Officers.Ability.Het, with - b.Rules.HetChanceNow(s), 9);
    }

    [Fact]
    public void Next_officer_cycles_through_those_in_reserve()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player;
        StationOfficer first = s.Officers["tactical"]!;
        b.OfficerNext("tactical");
        StationOfficer next = s.Officers["tactical"]!;
        Assert.NotEqual(first.Id, next.Id);
        Assert.DoesNotContain(s.Officers.Where(kv => kv.Key != "tactical"), kv => kv.Value?.Id == next.Id);
        Assert.Contains(b.Log, l => l.Text.EndsWith("to Tactical."));
    }

    [Fact]
    public void A_Krai_ship_names_its_officers_and_ranks_the_Krai_way()
    {
        var b = new Battle(TestData.Data, TestData.Data.ScenarioById("krai_escape"));
        StationOfficer helm = b.Player.Officers["helm"]!;
        Assert.Equal("Drovek", helm.Name);
        Assert.Equal("Fang", b.RankOf(helm));
        var c = new Battle(TestData.Data, TestData.Data.ScenarioById("concord_escape"));
        Assert.Equal("Lieutenant", c.RankOf(c.Player.Officers["helm"]!));
        Assert.Equal("Lt.", c.RankOf(c.Player.Officers["helm"]!, true));
    }

    [Fact]
    public void Every_faction_the_officer_names_are_given_for_is_a_faction()
    {
        foreach (string f in TestData.Data.Systems.Officers.Factions.Keys) Assert.True(TestData.Data.Factions.ContainsKey(f), f);
    }
}
