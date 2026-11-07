// The reactor (openspec/changes/reactor; the battle console mockup's REACTOR): coils, coolant, heat and meltdown, the crew's
// tending and the Reactor page's orders, on the real data.
using System;
using System.Linq;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class ReactorTests
{
    private static void Run(Battle b, double s) { for (int i = 0; i < Math.Round(s / b.Tuning.StepS); i++) b.Step(); }

    [Fact]
    public void Misaligned_coils_cut_the_output_by_the_efficiency_loss_of_their_mean_offset()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player;
        Assert.Equal(s.Class.ReactorE, b.Rules.Output(s), 6);
        b.SetCoil(0, 0.5); b.SetCoil(1, -0.5); b.SetCoil(2, 0.5);
        Assert.Equal(s.Class.ReactorE * (1 - b.Data.Systems.Reactor.EfficiencyLoss * 0.5), b.Rules.Output(s), 6);
    }

    [Fact]
    public void The_coolant_pumps_draw_from_the_reactor_and_cannot_be_raised_past_what_the_output_leaves()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player;
        int used = b.Rules.Used(s);
        b.SetFlow(0, 2);
        Assert.Equal(used + 1, b.Rules.Used(s));
        b.SetFlow(0, 5); // 22 E: 19 for the systems, 1 for loop B and 2 for loop A; none left
        Assert.Equal(2, s.Reactor.Loops[0].Flow);
        b.SetFlow(0, 0); // lowering is always allowed
        Assert.Equal(0, s.Reactor.Loops[0].Flow);
    }

    [Fact]
    public void Drawing_past_the_output_lifts_the_cap_on_the_allocation()
    {
        Battle b = TestData.NewBattle();
        b.SetPower("tractor", 10);
        Assert.Equal(1, b.Player.Power["tractor"]);
        b.SetOverdraw(true);
        b.SetPower("tractor", 10);
        Assert.Equal(10, b.Player.Power["tractor"]);
    }

    [Fact]
    public void Coils_drift_only_while_the_confinement_is_damaged()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player;
        Run(b, 10);
        Assert.All(s.Reactor.Coils, c => Assert.Equal(0, c));
        s.Sys["reactor"] = 0.3;
        Run(b, 10);
        Assert.Contains(s.Reactor.Coils, c => c != 0);
    }

    [Fact]
    public void The_power_officer_takes_drift_off_the_coils_by_skill()
    {
        double Drift(bool officer)
        {
            Battle b = TestData.NewBattle();
            Ship s = b.Player;
            if (!officer) s.Officers["power"] = null;
            s.Sys["reactor"] = 0.5;
            Run(b, 2);
            return Rules.Misalignment(s);
        }
        int skill = Rules.OfficerSkill(TestData.NewBattle().Player, "power");
        Assert.True(skill > 0);
        Assert.Equal(1 - skill * TestData.Data.Systems.Officers.Ability.Drift, Drift(true) / Drift(false), 3);
    }

    [Fact]
    public void A_core_with_its_coolant_offline_melts_down_and_the_blast_hurts_the_ships_near_it()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player, near = b.ShipById("tallis")!;
        near.X = s.X + 10000; near.Z = s.Z; near.ShieldsUp = false;
        double hull = near.Hull;
        foreach (CoolantLoop l in s.Reactor.Loops) l.Flow = 0;
        Run(b, b.Data.Systems.Reactor.MeltdownS - 1);
        Assert.True(s.Alive);
        Assert.Contains(b.Log, l => l.Text == "Coolant offline. The core will melt down.");
        near.X = s.X + 10000; near.Z = s.Z;
        Run(b, 1.5);
        Assert.False(s.Alive);
        Assert.Contains(b.Log, l => l.Text.EndsWith("reactor meltdown."));
        Assert.True(near.Hull < hull);
    }

    [Fact]
    public void A_core_that_is_safe_again_counts_the_meltdown_back()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player;
        s.Reactor.Melt = 10;
        Run(b, 2);
        Assert.Equal(10 - 2 * b.Data.Systems.Reactor.MeltRecoverX, s.Reactor.Melt, 1);
    }

    [Fact]
    public void The_core_settles_where_the_loops_carry_off_the_heat_the_output_makes()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player;
        foreach (Ship o in b.Ships) if (o.Side != s.Side) o.Z -= 200000;
        Run(b, 120);
        var R = b.Data.Systems.Reactor;
        double settle = R.HeatFullPctS * b.Rules.Output(s) / s.Class.ReactorE / (R.CoolPerFlow * Rules.Cooling(s));
        Assert.Equal(Math.Max(R.TempFloorPct, settle), s.Reactor.Temp, 0);
    }

    [Fact]
    public void An_AI_crew_nudges_its_coils_back_and_moves_flow_off_a_failing_loop()
    {
        Battle b = TestData.NewBattle();
        Ship k = b.ShipById("vorrak")!;
        k.Reactor.Coils[0] = 0.3;
        k.Reactor.Loops[0].Hp = 0.2;
        Run(b, 0.1);
        Assert.Equal(0.3 - b.Data.Systems.Reactor.Nudge, k.Reactor.Coils[0], 6);
        Assert.Equal(0, k.Reactor.Loops[0].Flow);
        Assert.Equal(2, k.Reactor.Loops[1].Flow);
    }

    [Fact]
    public void The_players_coils_are_the_captains_to_align_by_hand()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player;
        b.SetCoil(1, 0.3);
        Run(b, 2);
        Assert.Equal(0.3, s.Reactor.Coils[1], 6);
        b.AlignCoil(1, -1);
        Assert.Equal(0.3 - b.Data.Systems.Reactor.Nudge, s.Reactor.Coils[1], 6);
    }

    [Fact]
    public void Stability_falls_with_the_confinement_the_coils_and_the_heat()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player;
        Assert.Equal(1, b.Rules.Stability(s), 6);
        s.Sys["reactor"] = 0.5;
        Assert.Equal(0.5, b.Rules.Stability(s), 6);
        s.Reactor.Temp = 75;
        Assert.Equal(0.25, b.Rules.Stability(s), 6);
        s.Reactor.Temp = 100;
        Assert.Equal(0, b.Rules.Stability(s), 6);
    }

    [Fact]
    public void Every_ship_carries_the_reactor_files_coils_loops_and_nacelles()
    {
        Battle b = TestData.NewBattle();
        var d = b.Data.Systems;
        Assert.All(b.Ships, s =>
        {
            Assert.Equal(d.Reactor.Coils, s.Reactor.Coils.Length);
            Assert.Equal(d.Reactor.Loops, s.Reactor.Loops.Length);
            Assert.All(s.Reactor.Loops, l => Assert.Equal(d.Reactor.FlowStart, l.Flow));
            Assert.Equal(d.Warp.Nacelles, s.Warp.Nacelles.Length);
            Assert.Equal(d.Repair.Queue, s.RepairQueue);
        });
        Assert.True(b.Ships.All(s => s.Reactor.Temp == d.Reactor.TempStartPct));
    }

    [Fact]
    public void Every_scenes_start_fits_the_reactor_files_coils_loops_and_nacelles()
    {
        var d = TestData.Data.Systems;
        foreach (var sc in TestData.Data.Scenarios)
            foreach (var o in sc.Ships)
            {
                if (o.Start is not { } st) continue;
                Assert.True(st.Coils == null || st.Coils.Length == d.Reactor.Coils, $"{sc.Id}.{o.Id}: coils");
                Assert.True(st.LoopsHp == null || st.LoopsHp.Length == d.Reactor.Loops, $"{sc.Id}.{o.Id}: loops_hp");
                Assert.True(st.NacellesHp == null || st.NacellesHp.Length == d.Warp.Nacelles, $"{sc.Id}.{o.Id}: nacelles_hp");
                Assert.True(st.BanksE == null || st.BanksE <= d.Warp.BankE, $"{sc.Id}.{o.Id}: banks_e");
            }
    }

    [Fact]
    public void The_reactor_package_plays_each_of_its_scenes_from_both_sides()
    {
        foreach (string scene in new[] { "warp", "escape" })
        {
            Assert.Equal("concord", TestData.Data.ScenarioById("concord_" + scene).Side);
            Assert.Equal("krai", TestData.Data.ScenarioById("krai_" + scene).Side);
            Assert.Equal("kelvane", TestData.Data.ScenarioById("kelvane_" + scene).Side);
        }
    }

    [Fact]
    public void Every_classes_starting_allocation_and_its_coolant_pumps_fit_its_reactor()
    {
        var r = TestData.Data.Systems.Reactor;
        foreach (var c in TestData.Data.Classes.Values)
            Assert.True(c.PowerE.Values.Sum() + r.Loops * r.FlowStart * r.FlowE <= c.ReactorE, $"{c.Id}: {c.PowerE.Values.Sum()} E and the pumps past {c.ReactorE} E");
    }
}
