using System;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class HelmTests
{
    [Fact]
    public void A_high_energy_turn_turns_far_faster_than_the_turn_rate_or_breaks_the_ship_down()
    {
        Battle b = TestData.NewBattle();
        double battery = b.Player.Battery;
        b.Het(90);
        Assert.Equal(battery - b.Tuning.HetCostE, b.Player.Battery, 6);
        for (int i = 0; i < 15; i++) b.Step(); // half a second
        if (b.Player.Broke > 0) { Assert.Equal(0, b.Player.V); Assert.True(b.Player.Sys["engines"] < 1); }
        else Assert.Equal(90, Geometry.HeadingDeg(b.Player.H));
    }

    [Fact]
    public void Each_high_energy_turn_lowers_the_chance_of_the_next()
    {
        Battle b = TestData.NewBattle();
        double before = b.Rules.HetChanceNow(b.Player);
        b.Het(-90);
        Assert.Equal(before - b.Tuning.HetChanceDrop, b.Player.HetChance, 6);
        b.ToggleErratic();
        Assert.Equal(b.Player.HetChance - b.Tuning.HetErraticPenalty, b.Rules.HetChanceNow(b.Player), 6);
    }

    [Fact]
    public void A_helm_order_needs_a_target_and_the_lit_order_again_returns_to_free_heading()
    {
        Battle b = TestData.NewBattle();
        b.HelmOrder("intercept");
        Assert.False(b.Player.Engaged);
        b.Lock("vorrak");
        b.HelmOrder("intercept");
        Assert.True(b.Player.Engaged);
        // intercept steers for where the target will be when we arrive, not where it is (positions before the step)
        Ship me = b.Player, t = b.ShipById("vorrak")!;
        double tti = Geometry.Dist(me.X, me.Z, t.X, t.Z) / Math.Max(100, Math.Abs(me.V));
        var f = Geometry.Forward(t.H);
        double lead = Geometry.HeadingTo(me.X, me.Z, t.X + f.X * t.V * tti, t.Z + f.Z * t.V * tti);
        b.Step();
        Assert.True(Math.Abs(Geometry.Wrap(me.HOrder - lead)) < 0.02);
        b.HelmOrder("intercept");
        Assert.False(b.Player.Engaged);
    }

    [Fact]
    public void Turning_by_hand_takes_the_ship_off_its_helm_order()
    {
        Battle b = TestData.NewBattle();
        b.Lock("vorrak");
        b.HelmOrder("orbit");
        b.Turn(15);
        Assert.False(b.Player.Engaged);
    }

    [Fact]
    public void Emergency_stop_brings_the_ship_to_rest_then_holds_the_engines()
    {
        Battle b = TestData.NewBattle();
        b.EmergencyStop();
        for (int i = 0; i < 60 && b.Player.EstopT <= 0; i++) b.Step();
        Assert.Equal(0, b.Player.V);
        Assert.True(b.Player.EstopT > 0);
        b.SetSpeed(120);
        b.Step();
        Assert.Equal(0, b.Player.VOrder);
    }

    [Fact]
    public void Erratic_manoeuvres_make_the_ship_harder_to_hit()
    {
        Battle b = TestData.NewBattle();
        Ship t = b.ShipById("vorrak")!, me = b.Player;
        t.X = me.X; t.Z = me.Z + 3000; // dead astern, in the aft lance's arc
        double plain = b.Rules.HitChance(t, t.Mounts[3], me);
        b.ToggleErratic();
        Assert.Equal(plain * b.Tuning.ErraticHit, b.Rules.HitChance(t, t.Mounts[3], me), 6);
    }

    [Fact]
    public void Hold_and_run_ranges_snap_to_half_kilometres_inside_their_limits()
    {
        Battle b = TestData.NewBattle();
        b.SetHoldKm(6.3);
        Assert.Equal(6.5, b.Player.HoldKm);
        b.SetRunRange(5, 4);
        Assert.True(b.Player.RunMaxKm - b.Player.RunMinKm >= 0.5);
        b.SetHoldKm(99);
        Assert.Equal(b.Tuning.HoldMaxKm, b.Player.HoldKm);
    }
}
