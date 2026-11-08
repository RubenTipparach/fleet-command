// Regression tests for the Mac playtest of 2026-11-08 (docs/playtests/2026-11-08-mission-types.md): a withdrawal whose drive
// is wrecked no longer locks the battle, a withdrawal holds over its risk and can be called off, a story battle nobody can
// win ends as a stalemate, and an AI captain who has not fired closes in rather than kiting out of reach.
using System;
using System.Linq;
using FleetCommand.Campaign;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public sealed class StandoffTests
{
    private static void Run(Battle b, double s) { for (int i = 0; i < Math.Round(s / b.Tuning.StepS) && b.Over == null; i++) b.Step(); }

    // a mission's battle on a new campaign
    private static (Campaign.Campaign C, Offer M, MissionPlan P, Battle B) Fly(string id, string side)
    {
        Campaign.Campaign c = Campaign.Campaign.New(CampaignTests.Camp, side);
        c.S.Missions.Clear();
        Offer? m = null;
        for (int t = 0; t < 6 && m == null; t++) { m = c.OfferMission(id); if (m == null) c.EndTurn(); }
        Assert.NotNull(m);
        MissionPlan plan = MissionRun.Plan(c, m!, TestData.Data)!;
        return (c, m!, plan, new Battle(TestData.Data, plan.Scenario));
    }

    // every enemy put out of the way and holding fire, so only the rule under test acts
    private static void Quiet(Battle b)
    {
        foreach (Ship e in b.Ships.Where(s => s.Side == 1)) { e.X += 3_000_000; e.HoldFire = true; e.VOrder = 0; e.V = 0; }
    }

    [Fact]
    public void A_withdrawal_whose_drive_can_no_longer_charge_is_called_off()
    {
        var (_, _, _, b) = Fly("patrol", "concord");
        Quiet(b);
        Assert.True(b.Withdraw());
        Array.Fill(b.Player.Warp.Nacelles, 0);
        b.Player.Warp.Banks = 0;
        Run(b, 1);
        Assert.False(b.Withdrawing);
        Assert.Null(b.Over);
        Assert.Contains(b.Log, l => l.Text.Contains("called off"));
    }

    [Fact]
    public void A_withdrawal_over_its_risk_holds_the_jump_and_pressing_again_calls_it_off()
    {
        var (_, _, _, b) = Fly("patrol", "concord");
        Quiet(b);
        Assert.True(b.Withdraw());
        // a drive charged and plotted, its nacelles badly hurt: ready, with a jump risk over the withdrawal's limit
        WarpState w = b.Player.Warp;
        for (int i = 0; i < w.Nacelles.Length; i++) w.Nacelles[i] = 0.05;
        w.Banks = TestData.Data.Systems.Warp.BankE; w.Plot = 1; w.Plotting = true;
        Run(b, 2);
        Assert.True(b.WarpReady(b.Player));
        Assert.True(b.WarpRiskOf(b.Player).Total > TestData.Data.Systems.Warp.WithdrawMaxRisk);
        Assert.Null(b.Over);
        Assert.True(b.Withdrawing);
        Assert.Contains(b.Log, l => l.Text.StartsWith("Holding the jump"));
        b.ToggleWithdraw();
        Assert.False(b.Withdrawing);
    }

    [Fact]
    public void A_clean_withdrawal_still_jumps_out()
    {
        var (_, _, _, b) = Fly("patrol", "concord");
        Quiet(b);
        Assert.True(b.Withdraw());
        Run(b, 180);
        Assert.Equal("jumped", b.Over);
    }

    [Fact]
    public void A_story_battle_where_no_ship_is_hurt_ends_as_a_stalemate_graded_a_draw()
    {
        var (c, m, plan, b) = Fly("first_blood", "krai");
        Assert.False(plan.Scenario.CanWithdraw);
        Quiet(b);
        Run(b, b.Tuning.StalemateS + 5);
        Assert.Equal("stalemate", b.Over);
        Debrief d = MissionRun.Finish(c, m, plan, b);
        Assert.Equal("draw", d.Grade);
    }

    [Fact]
    public void A_contract_battle_never_ends_as_a_stalemate()
    {
        var (_, _, _, b) = Fly("patrol", "krai");
        Quiet(b);
        Run(b, b.Tuning.StalemateS + 5);
        Assert.NotEqual("stalemate", b.Over);
    }

    [Fact]
    public void A_chase_between_ships_of_equal_speed_comes_to_blows()
    {
        // the Mac playtest's stall: a Krai corvette intercepting a Vos corsair at the same top speed, 49 km apart, and no
        // hull changing for the rest of the battle
        var (_, _, plan, b) = Fly("patrol", "krai");
        Ship me = b.Player, vos = b.ShipById(plan.Groups["raiders"][0])!;
        foreach (Ship e in b.Ships.Where(s => s.Side == 1 && s != vos)) { e.X += 3_000_000; e.HoldFire = true; }
        vos.X = me.X; vos.Z = me.Z - 49_000;
        b.Lock(vos.Id);
        b.HelmOrder("intercept");
        double hulls = me.Hull + vos.Hull;
        for (int i = 0; i < Math.Round(150 / b.Tuning.StepS) && b.Over == null; i++)
        {
            me.VOrder = b.Rules.MaxSpeed(me);
            if (b.WouldFire().Length > 0) b.Fire();
            b.Step();
        }
        Assert.True(me.Hull + vos.Hull < hulls, $"no hull changed in 150 s; {Rules.DistKm(me, vos):0} km apart");
    }

    [Fact]
    public void An_ai_captain_whose_fire_never_lands_keeps_closing_in()
    {
        // a corsair whose shots land on nothing (held here) closes from its 80 km fighting range, half again every 20 s
        var (_, _, plan, b) = Fly("patrol", "krai");
        Ship me = b.Player, vos = b.ShipById(plan.Groups["raiders"][0])!;
        foreach (Ship e in b.Ships.Where(s => s.Side == 1 && s != vos)) { e.X += 3_000_000; e.HoldFire = true; }
        vos.X = me.X; vos.Z = me.Z - 80_000;
        vos.HoldFire = true;
        double nearest = double.MaxValue;
        for (int i = 0; i < Math.Round(120 / b.Tuning.StepS) && b.Over == null; i++)
        {
            me.VOrder = 0;
            b.Step();
            nearest = Math.Min(nearest, Rules.DistKm(me, vos));
        }
        Assert.True(nearest < 20, $"the corsair stayed {nearest:0} km out");
    }
}
