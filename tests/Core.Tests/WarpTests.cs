// The warp drive (openspec/changes/warp; the battle console mockup's WARP): the coils' energy, the banks, the course plot,
// the jump risk and the jump, the warp key, and the Warp scene, on the real data.
using System;
using System.Linq;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class WarpTests
{
    private static void Run(Battle b, double s) { for (int i = 0; i < Math.Round(s / b.Tuning.StepS); i++) b.Step(); }

    // a battle with the enemies far away and no repair crews, so nothing but the warp drive changes the ship
    private static Battle Quiet()
    {
        Battle b = TestData.NewBattle();
        foreach (Ship o in b.Ships) if (o.Side != b.Player.Side) o.Z -= 200000;
        b.SetPower("repair", 0);
        return b;
    }

    // the drive ready: armed, the banks full, the course plotted
    private static void Ready(Battle b)
    {
        WarpState w = b.Player.Warp;
        w.Armed = true; w.Banks = b.Data.Systems.Warp.BankE; w.Plot = 1; w.Plotting = true;
    }

    [Fact]
    public void An_armed_drive_takes_the_free_energy_and_gives_way_when_the_systems_need_it()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        b.WarpKey();
        Assert.True(s.Warp.Armed);
        Assert.Equal(b.Data.Systems.Warp.CoilE, s.Warp.Want);
        Run(b, 0.1);
        Assert.Equal(2, s.Power["warp"]); // 22 E less the 20 the systems and pumps use
        b.SetPower("engines", 2);
        Run(b, 0.1);
        Assert.Equal(6, s.Power["warp"]);
        b.SetOverdraw(true);
        b.SetPower("engines", 10);
        Run(b, 0.1);
        Assert.Equal(0, s.Power["warp"]);
    }

    [Fact]
    public void Warp_set_by_hand_arms_the_coils_and_zero_powers_them_down()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        b.SetPower("engines", 2);
        b.SetPower("warp", 3);
        Assert.Equal(3, s.Power["warp"]);
        Assert.True(s.Warp.Armed);
        b.SetPower("warp", 0);
        Assert.False(s.Warp.Armed);
        Run(b, 1);
        Assert.Equal(0, s.Power["warp"]);
    }

    [Fact]
    public void The_nacelles_still_working_split_the_warp_energy_and_charge_the_banks_by_their_health()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        b.SetPower("engines", 2);
        b.SetPower("warp", 4);
        s.Warp.Nacelles[0] = 0.5;
        Assert.Equal(new[] { 2.0, 2.0 }, Rules.NacelleDraw(s));
        Run(b, 1);
        Assert.Equal((2 * 0.5 + 2 * 1) * b.Data.Systems.Warp.ChargeK, s.Warp.Banks, 3);
        s.Warp.Nacelles[0] = 0;
        Assert.Equal(new[] { 0.0, 4.0 }, Rules.NacelleDraw(s));
    }

    [Fact]
    public void The_course_plot_is_slowed_by_damaged_sensors_and_stopped_when_they_are_out()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        b.PlotCourse();
        Run(b, 5);
        Assert.Equal(5 / b.Data.Systems.Warp.PlotS, s.Warp.Plot, 3);
        s.Sys["sensors"] = 0.5;
        Run(b, 4);
        Assert.Equal(7 / b.Data.Systems.Warp.PlotS, s.Warp.Plot, 3);
        s.Sys["sensors"] = 0;
        Run(b, 4);
        Assert.Equal(7 / b.Data.Systems.Warp.PlotS, s.Warp.Plot, 3);
        Assert.Equal("stopped: sensors out", Battle.CourseText(s));
    }

    [Fact]
    public void The_jump_risk_is_one_less_the_product_of_one_less_each_factor()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        s.Warp.Nacelles[0] = 0.5; s.Reactor.Coolant = 0.3;
        WarpRisk r = b.WarpRiskOf(s);
        Assert.Equal(6, r.Factors.Count);
        Assert.Equal(1 - r.Factors.Aggregate(1.0, (a, f) => a * (1 - f.Risk)), r.Total, 9);
        Assert.Equal(0.25 * 0.8, r.Factors.First(f => f.Key == "nacelles").Risk, 9);
        Assert.Equal(0.2 * 1.2, r.Factors.First(f => f.Key == "coolant").Risk, 9);
        s.Warp.Nacelles[1] = 0;
        Assert.Equal(b.Data.Systems.Warp.Risk.NacelleOut, b.WarpRiskOf(s).Factors.First(f => f.Key == "nacelles").Risk, 9);
    }

    [Fact]
    public void A_jump_is_refused_without_the_energy_stored_and_says_why()
    {
        Battle b = Quiet();
        b.JumpNow();
        Assert.True(b.Player.Alive);
        Assert.Equal("Jump refused", b.Notice?.Title);
        Assert.StartsWith("not enough energy stored", b.WarpRiskOf(b.Player).Why);
    }

    [Fact]
    public void A_ready_jump_streaks_the_ship_away_and_ends_the_battle_as_an_escape()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        Ready(b);
        Assert.True(b.WarpReady(s));
        Assert.Equal(0, b.WarpRiskOf(s).Total, 9);
        double z = s.Z;
        b.WarpKey();
        Assert.Equal("Jumping", b.Notice?.Title);
        Assert.Equal(b.Data.Systems.Warp.BankE - b.Data.Systems.Warp.JumpE, s.Warp.Banks, 6);
        Run(b, b.Data.Systems.Warp.OutS + 0.1);
        Assert.True(s.Jumped);
        Assert.False(s.Alive);
        Assert.True(s.Z < z - 5000);
        Assert.Equal("jumped", b.Over);
    }

    [Fact]
    public void A_failed_jump_damages_the_nacelles_drains_the_banks_and_loses_the_course()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        Ready(b);
        s.Warp.Nacelles[0] = 0; s.Reactor.Coolant = 0; s.Sys["reactor"] = 0.05;
        Assert.True(b.WarpRiskOf(s).Total > 0.99);
        double hull = s.Hull;
        b.WarpKey();
        Assert.Equal("Jump failed", b.Notice?.Title);
        Assert.Equal(0, s.Warp.Banks);
        Assert.Equal(0, s.Warp.Plot);
        Assert.False(s.Warp.Plotting);
        Assert.True(s.Hull < hull || !s.Alive);
        Assert.Null(b.Over);
    }

    [Fact]
    public void The_warp_key_arms_the_coils_then_reports_and_never_cuts_power()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        b.WarpKey();
        Assert.True(s.Warp.Armed && s.Warp.Plotting);
        b.SetPower("engines", 2);
        Run(b, 1);
        int pw = s.Power["warp"];
        Assert.True(pw > 0);
        b.WarpKey();
        Assert.True(s.Warp.Armed);
        Assert.Equal(pw, s.Power["warp"]);
        Assert.StartsWith("Warp banks", b.Log[^1].Text);
    }

    [Fact]
    public void The_warp_scene_starts_in_a_field_strong_enough_to_need_the_override()
    {
        foreach (string id in new[] { "concord_warp", "krai_warp" })
        {
            var b = new Battle(TestData.Data, TestData.Data.ScenarioById(id));
            Ship s = b.Player;
            Assert.Equal(12, s.Warp.Banks);
            Assert.Equal(0.7, s.Warp.Nacelles[0]);
            Assert.Equal(0.8, s.Reactor.Coolant);
            Assert.True(b.QgAt(s.X, s.Z) > b.Data.Systems.Warp.QgBlock);
            Ready(b);
            Assert.StartsWith("gravity field too strong", b.WarpRiskOf(s).Why);
            b.ToggleOverride();
            Assert.Equal("", b.WarpRiskOf(s).Why);
            Assert.Equal(b.Data.Systems.Warp.Risk.QgOverride * b.QgAt(s.X, s.Z), b.WarpRiskOf(s).Factors.First(f => f.Key == "qg").Risk, 9);
        }
    }
}
