using System;
using FleetCommand.Data;
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
        double before = b.Player.HetChance, officer = Rules.OfficerSkill(b.Player, "helm") * b.Data.Systems.Officers.Ability.Het;
        b.Het(-90);
        Assert.Equal(before - b.Tuning.HetChanceDrop, b.Player.HetChance, 6);
        b.ToggleErratic();
        Assert.Equal(b.Player.HetChance - b.Tuning.HetErraticPenalty + officer, b.Rules.HetChanceNow(b.Player), 6);
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
        // intercept steers for where the target will be when we arrive, not where it is (positions before the step), but
        // never more than intercept_max_lead_deg off the target, so the forward arcs keep it
        Ship me = b.Player, t = b.ShipById("vorrak")!;
        double tti = Geometry.Dist(me.X, me.Z, t.X, t.Z) / Math.Max(1000, Math.Abs(me.V));
        var f = Geometry.Forward(t.H);
        double toT = Geometry.HeadingTo(me.X, me.Z, t.X, t.Z), lead = Geometry.HeadingTo(me.X, me.Z, t.X + f.X * t.V * tti, t.Z + f.Z * t.V * tti);
        double max = b.Tuning.InterceptMaxLeadDeg * Geometry.Deg, want = toT + Math.Clamp(Geometry.Wrap(lead - toT), -max, max);
        b.Step();
        Assert.True(Math.Abs(Geometry.Wrap(me.HOrder - want)) < 0.02, $"HOrder {me.HOrder / Geometry.Deg:0.0} want {want / Geometry.Deg:0.0} toT {toT / Geometry.Deg:0.0} lead {lead / Geometry.Deg:0.0}");
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
        b.SetSpeed(1200);
        b.Step();
        Assert.Equal(0, b.Player.VOrder);
    }

    [Fact]
    public void Erratic_manoeuvres_make_the_ship_harder_to_hit()
    {
        Battle b = TestData.NewBattle();
        Ship t = b.ShipById("vorrak")!, me = b.Player;
        t.X = me.X; t.Z = me.Z + 30000; // dead astern, in the aft lance's arc
        double plain = b.Rules.HitChance(t, t.Mounts[3], me);
        b.ToggleErratic();
        Assert.Equal(plain * b.Tuning.ErraticHit, b.Rules.HitChance(t, t.Mounts[3], me), 6);
    }

    [Fact]
    public void Hold_and_run_ranges_snap_to_five_kilometres_inside_their_limits()
    {
        Battle b = TestData.NewBattle();
        b.SetHoldKm(63);
        Assert.Equal(65, b.Player.HoldKm);
        b.SetRunRange(50, 40);
        Assert.True(b.Player.RunMaxKm - b.Player.RunMinKm >= 5);
        b.SetHoldKm(999);
        Assert.Equal(b.Tuning.HoldMaxKm, b.Player.HoldKm);
    }

    [Fact]
    public void The_range_sliders_give_their_first_half_to_five_to_fifteen_kilometres_and_step_by_five_above()
    {
        // owner, 2026-11-10: "dedicate 1/2 of the slider to be from 5km - 15km, and the other half is increments of 5km"
        Battle b = TestData.NewBattle();
        Tuning t = b.Tuning;
        Assert.Equal(5, HelmRange.FromFrac(0, t));
        Assert.Equal(15, HelmRange.FromFrac(0.5, t));
        Assert.Equal(t.HoldMaxKm, HelmRange.FromFrac(1, t));
        Assert.Equal(10, HelmRange.FromFrac(0.25, t));
        // below the split, a kilometre at a time; above it, five
        b.SetHoldKm(7.4);
        Assert.Equal(7, b.Player.HoldKm);
        b.SetHoldKm(17);
        Assert.Equal(15, b.Player.HoldKm);
        b.SetHoldKm(18);
        Assert.Equal(20, b.Player.HoldKm);
        // the slider reads back where it drew
        foreach (double km in new[] { 5.0, 9, 15, 40, t.HoldMaxKm }) Assert.Equal(km, HelmRange.FromFrac(HelmRange.ToFrac(km, t), t), 9);
        // hit and run's two ranges a step apart in the fine half too
        b.SetRunRange(8, 8);
        Assert.Equal(1, b.Player.RunMaxKm - b.Player.RunMinKm, 9);
        b.SetRunRange(1, 2);
        Assert.Equal(5, b.Player.RunMinKm);
    }
}
