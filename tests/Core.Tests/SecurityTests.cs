// Boarding defence (Battle.Security, Data.DeckPlan; openspec/changes/boarding-defence; owner, 2026-11-04: "Put a high
// security officer, transport units around the ship to setup ambushes"). The numbers are data/battle/systems/cloak.json's
// security, the plans data/ships/*_plan.json.
using System;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class SecurityTests
{
    private static CloakSystems Cs => TestData.Data.Systems.Cloak;
    private static SecurityRules Sr => Cs.Security;

    private static void Run(Battle b, double s) { for (int i = 0; i < Math.Round(s / b.Tuning.StepS) && b.Over == null; i++) b.Step(); }

    [Fact]
    public void Every_class_has_a_deck_plan_and_every_room_of_it_can_be_walked_to_from_the_bridge()
    {
        foreach (string c in TestData.Data.Classes.Keys) Assert.NotNull(Cs.PlanFor(c));
        foreach (DeckPlan p in Cs.Plans.Values)
        {
            int from = p.Room("bridge").Node;
            foreach (DeckRoom r in p.Rooms)
                if (r.Node != from) Assert.True(p.Route(from, r.Node).Count > 0, $"{p.Id}: no walk from the bridge to {r.Id}");
        }
    }

    [Fact]
    public void A_walk_ends_at_its_room_and_goes_by_way_of_the_corridors()
    {
        DeckPlan p = Cs.Plans["petrel"];
        var path = p.Route(p.Room("bridge").Node, p.Room("reactor").Node);
        Assert.Equal(p.Room("reactor").Node, path[^1]);
        Assert.Contains(path, n => p.Room(p.Nodes[n].Room).IsCorridor);
        // three decks down: a ladder between each pair of corridors
        Assert.Equal(3, path.Select(n => p.Nodes[n].Y).Distinct().Count());
    }

    [Fact]
    public void The_marines_aboard_are_teams_of_up_to_three_at_their_posts()
    {
        var b = new Battle(TestData.Data, TestData.Data.ScenarioById("concord_repel"));
        SecurityState sec = b.Player.Sec!;
        Assert.Equal(new[] { 3, 3, 3 }, sec.Teams.Select(u => u.N));
        Assert.Equal(sec.Plan.Posts.Take(3), sec.Teams.Select(u => Battle.RoomOf(sec, u).Id));
        Assert.All(sec.Teams, u => Assert.Equal("Ambush set", b.UnitState(u)));
    }

    [Fact]
    public void A_team_walks_at_its_pace_and_one_beamed_arrives_at_once()
    {
        var b = new Battle(TestData.Data, TestData.Data.ScenarioById("concord_repel"));
        SecurityState sec = b.Player.Sec!;
        Unit t1 = sec.Teams[0];
        b.SecurityGo("T1", "mess", beam: false);
        Assert.NotEmpty(t1.Path);
        Assert.Equal("Moving", b.UnitState(t1));
        double x = t1.X, y = t1.Y, z = t1.Z;
        b.Step();
        double moved = Math.Sqrt((t1.X - x) * (t1.X - x) + (t1.Y - y) * (t1.Y - y) + (t1.Z - z) * (t1.Z - z));
        Assert.True(moved <= Sr.WalkMS * b.Tuning.StepS + 1e-9);
        b.SecurityGo("T2", "shield_room", beam: true);
        Assert.Equal("shield_room", Battle.RoomOf(sec, sec.Teams[1]).Id);
        Assert.Empty(sec.Teams[1].Path);
    }

    [Fact]
    public void A_boarding_captain_beams_a_squad_into_an_empty_room_bound_for_the_bridge_first()
    {
        var b = new Battle(TestData.Data, TestData.Data.ScenarioById("concord_repel"));
        Ship v = b.ShipById("vekk")!;
        Run(b, 7);
        SecurityState sec = b.Player.Sec!;
        Assert.Single(sec.Foes);
        Unit f = sec.Foes[0];
        Assert.Equal(Sr.BoardSquad, f.N);
        Assert.Equal(15 - Sr.BoardSquad, v.Marines);
        Assert.Equal(sec.Plan.Goals[0], f.Goal);
        Assert.Contains(b.Log, l => l.Text.StartsWith("Intruder alert"));
    }

    [Fact]
    public void A_boarding_captain_cannot_board_through_a_shield_that_is_up()
    {
        var b = new Battle(TestData.Data, TestData.Data.ScenarioById("concord_repel"));
        Ship v = b.ShipById("vekk")!;
        int f = Rules.FacingFrom(b.Player, v.X, v.Z);
        Assert.True(b.CanBoard(v, b.Player));
        b.Player.Shields[f] = 10;
        Assert.False(b.CanBoard(v, b.Player));
    }

    [Fact]
    public void An_ambush_opens_a_fight_with_a_volley_the_boarders_cannot_answer()
    {
        var b = new Battle(TestData.Data, TestData.Data.ScenarioById("concord_repel"));
        SecurityState sec = b.Player.Sec!;
        Unit team = sec.Teams[0];   // on the bridge, its ambush set
        b.BoardShip(b.ShipById("vekk")!, b.Player);
        Unit squad = sec.Foes[0];
        // the squad stands on the team
        squad.X = team.X; squad.Y = team.Y; squad.Z = team.Z; squad.Path.Clear();
        b.Step();
        Assert.Contains(b.Log, l => l.Text.Contains("springs an ambush"));
        // the ambush volley went at once, and no shot came back
        Assert.Contains(b.Log, l => l.Text.StartsWith("T1 in ") && l.Text.Contains(" 0 of ours."));
        Assert.Equal(3, team.N);
    }

    [Fact]
    public void Boarders_unopposed_at_a_system_sabotage_it_and_on_the_bridge_take_the_ship()
    {
        var b = new Battle(TestData.Data, TestData.Data.ScenarioById("concord_repel"));
        SecurityState sec = b.Player.Sec!;
        sec.Teams.Clear(); b.Player.Marines = 0;
        b.ShipById("vekk")!.AiBoard = false;
        DeckRoom sh = sec.Plan.Room("shield_room");
        DeckNode n = sec.Plan.Nodes[sh.Node];
        sec.Foes.Add(new Unit { Id = "B9", Side = 1, N = 3, Node = sh.Node, X = n.X, Y = n.Y, Z = n.Z, Goal = sh.Id });
        double hp = b.Player.Sys["shields"];
        Run(b, Sr.SabotageS + 0.1);
        Assert.Equal(hp - Sr.SabotageDmg, b.Player.Sys["shields"], 9);
        DeckRoom br = sec.Plan.Room("bridge");
        DeckNode bn = sec.Plan.Nodes[br.Node];
        sec.Foes.Add(new Unit { Id = "B10", Side = 1, N = 3, Node = br.Node, X = bn.X, Y = bn.Y, Z = bn.Z, Goal = br.Id });
        Run(b, Sr.CaptureS + 0.2);
        Assert.Equal("lost", b.Over);
        Assert.Contains(b.Log, l => l.Text.StartsWith("Boarders took the bridge"));
    }

    [Fact]
    public void A_volley_lands_each_marines_shot_at_its_chance()
    {
        var r = new Rng(3);
        int hits = 0;
        for (int i = 0; i < 2000; i++) hits += Rules.Volley(3, 0.5, r);
        Assert.InRange(hits / 6000.0, 0.47, 0.53);
        Assert.Equal(0, Rules.Volley(5, 0, r));
        Assert.Equal(5, Rules.Volley(5, 1, r));
    }

    [Fact]
    public void The_repel_scenes_run_from_both_sides_to_an_end_or_a_fight_aboard()
    {
        foreach (string id in new[] { "concord_repel", "krai_repel" })
        {
            var b = new Battle(TestData.Data, TestData.Data.ScenarioById(id));
            Run(b, 60);
            Assert.True(b.Over != null || b.Player.Sec!.Foes.Count > 0 || b.Ships[1].Marines < 15, id);
            Assert.Contains(b.Log, l => l.Who == "Security");
        }
    }

    [Fact]
    public void The_same_seed_plays_the_same_boarding()
    {
        string Play()
        {
            var b = new Battle(TestData.Data, TestData.Data.ScenarioById("concord_repel"));
            Run(b, 90);
            return string.Join("|", b.Log.Select(l => l.Text));
        }
        Assert.Equal(Play(), Play());
    }
}
