// The fleet (survey G3: "with ai wingmen, and behaviors configuratble for AI"): fleet orders set every wingman's stance;
// each wingman's standing orders (stance, target rule, range, retreat, fire) change how it fights; orders never reach the
// ship you command.
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class FleetTests
{
    [Fact]
    public void A_fleet_order_sets_every_living_wingmans_stance_but_not_the_ship_you_command()
    {
        Battle b = TestData.NewBattle();
        b.FleetOrder(Stance.Hunt);
        Assert.All(b.Ships.Where(s => s.Side == 0 && s != b.Player), s => Assert.Equal(Stance.Hunt, s.Stance));
        Assert.Equal(Stance.Attack, b.Player.Stance);
        Assert.Equal("Fleet: Hunt.", b.Log[^1].Text);
    }

    [Fact]
    public void A_hunter_goes_for_the_weakest_enemy_and_a_wingman_on_nearest_for_the_nearest()
    {
        Battle b = TestData.NewBattle();
        Ship w = b.ShipById("tallis")!, weak = b.ShipById("rattlejaw")!;
        weak.Hull = weak.Class.HullHp * 0.2;
        b.SetStance("tallis", Stance.Hunt);
        b.Step();
        Assert.Equal("rattlejaw", w.Target);
        b.SetStance("tallis", Stance.Attack);
        b.SetRule("tallis", TargetRule.Nearest);
        for (int i = 0; i < 20; i++) b.Step();
        Ship nearest = b.EnemiesOf(w).OrderBy(o => Rules.DistKm(w, o)).First();
        Assert.Equal(nearest.Id, w.Target);
    }

    [Fact]
    public void A_wingman_on_mine_fights_your_target_but_never_a_friendly_you_selected()
    {
        Battle b = TestData.NewBattle();
        Ship w = b.ShipById("tallis")!;
        b.Lock("ashkar");
        b.Step();
        Assert.Equal("ashkar", w.Target);
        b.Lock("meridian");
        for (int i = 0; i < 20; i++) b.Step();
        Assert.NotEqual("meridian", w.Target);
        Assert.Equal(1, b.ShipById(w.Target)!.Side);
    }

    [Fact]
    public void A_wingman_told_to_retreat_at_half_turns_away_below_half_its_hull()
    {
        Battle b = TestData.NewBattle();
        Ship w = b.ShipById("tallis")!;
        b.SetRetreat("tallis", RetreatAt.Half);
        w.Hull = w.Class.HullHp * 0.4;
        b.Step();
        Ship near = b.EnemiesOf(w).OrderBy(o => Geometry.Dist(w.X, w.Z, o.X, o.Z)).First();
        double away = Geometry.Wrap(Geometry.HeadingTo(w.X, w.Z, near.X, near.Z) + System.Math.PI);
        Assert.Equal(0, Geometry.Wrap(w.HOrder - away), 3);   // ordered before the step's move, read after it
        Assert.Equal(b.Rules.MaxSpeed(w), w.VOrder, 6);
    }

    [Fact]
    public void A_wingman_set_to_hold_fire_never_fires()
    {
        Battle b = TestData.NewBattle();
        b.SetHoldFire("tallis", true);
        b.SetHoldFire("meridian", true);
        bool fired = false;
        for (int i = 0; i < 1800; i++) { b.Step(); fired |= b.Shots.Any(sh => sh.From is "tallis" or "meridian" && !sh.Intercept); }
        Assert.False(fired);
    }

    [Fact]
    public void Standing_orders_never_reach_the_ship_you_command_or_an_enemy()
    {
        Battle b = TestData.NewBattle();
        b.SetStance(b.Player.Id, Stance.Hold);
        b.SetRange("vorrak", AiRange.Long);
        b.SetRetreat("vorrak", RetreatAt.Quarter);
        Assert.Equal(Stance.Attack, b.Player.Stance);
        Assert.Equal(AiRange.Mid, b.ShipById("vorrak")!.Range);
        Assert.Equal(RetreatAt.Never, b.ShipById("vorrak")!.Retreat);
    }

    [Fact]
    public void The_fleet_lists_your_ships_living_or_lost_and_taking_command_hands_the_old_ship_your_target()
    {
        Battle b = TestData.NewBattle();
        b.ShipById("meridian")!.Alive = false;
        Assert.Equal(new[] { "resolute", "tallis", "meridian" }, b.FleetAll().Select(s => s.Id));
        b.Player.Rule = TargetRule.Weakest;
        Ship old = b.Player;
        b.TakeCommand("tallis");
        Assert.Equal(TargetRule.Mine, old.Rule);
    }
}
