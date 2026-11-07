// Damage control (openspec/changes/damage-control; the battle console mockup's REPAIR): the crews on the repair queue, the
// jobs' health and time to whole, the queue's orders, and the Escape scene's start, on the real data.
using System;
using System.Linq;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class RepairTests
{
    private static void Run(Battle b, double s) { for (int i = 0; i < Math.Round(s / b.Tuning.StepS); i++) b.Step(); }

    private static Battle Quiet()
    {
        Battle b = TestData.NewBattle();
        foreach (Ship o in b.Ships) if (o.Side != b.Player.Side) o.Z -= 200000;
        return b;
    }

    [Fact]
    public void The_crews_work_down_the_queue_at_most_three_on_a_job_and_skip_the_jobs_that_are_whole()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        s.Sys["sensors"] = 0.5; s.Sys["shields"] = 0.5; s.Sys["weapons"] = 0.5;
        b.SetPower("tractor", 0);
        b.SetPower("weapons", 2);
        b.SetPower("repair", 5);
        // engines, reactor and warp are whole: the first five crews go to sensors (three) and shields (two)
        Assert.Equal(new[] { ("sensors", 3), ("shields", 2) }, b.RepairPlan(s).ToArray());
        Assert.Equal(0, b.CrewsOn(s, "weapons"));
    }

    [Fact]
    public void A_crew_mends_its_rate_a_second_faster_with_a_chief_engineer()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        s.Sys["engines"] = 0.5;
        Run(b, 10);
        double rate = b.Tuning.RepairPerES * (1 + Rules.OfficerSkill(s, "repair") * b.Data.Systems.Officers.Ability.RepairRate);
        Assert.Equal(0.5 + 10 * rate, s.Sys["engines"], 3);
        Assert.True(Rules.OfficerSkill(s, "repair") > 0);
    }

    [Fact]
    public void Repair_energy_mends_nothing_that_is_not_on_the_crews_plan()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        b.SetPower("repair", 0);
        s.Sys["engines"] = 0.5; s.Mounts[0].Hp = 0.5;
        Run(b, 5);
        Assert.Equal(0.5, s.Sys["engines"]);
        Assert.Equal(0.5, s.Mounts[0].Hp);
    }

    [Fact]
    public void The_reactor_job_mends_its_worst_coolant_loop_before_its_confinement()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        s.Sys["reactor"] = 0.6; s.Reactor.Loops[1].Hp = 0.4;
        b.RepairFirst("reactor");
        Run(b, 2);
        Assert.True(s.Reactor.Loops[1].Hp > 0.4);
        Assert.Equal(0.6, s.Sys["reactor"]);
        Assert.Equal(0.4, Battle.JobHealth(s, "reactor"), 1);
    }

    [Fact]
    public void The_warp_and_mounts_jobs_mend_their_worst_part_one_at_a_time()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        s.Warp.Nacelles[0] = 0.8; s.Warp.Nacelles[1] = 0.3;
        b.RepairFirst("warp");
        Run(b, 2);
        Assert.Equal(0.8, s.Warp.Nacelles[0]);
        Assert.True(s.Warp.Nacelles[1] > 0.3);
        s.Mounts[2].Hp = 0.2;
        b.RepairFirst("mounts");
        Run(b, 2);
        Assert.Equal(0.2 + 2 * b.Tuning.MountRepairPerES * (1 + Rules.OfficerSkill(s, "repair") * b.Data.Systems.Officers.Ability.RepairRate), s.Mounts[2].Hp, 3);
    }

    [Fact]
    public void The_time_to_whole_counts_every_damaged_part_of_a_job_at_the_crews_on_it()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        s.Sys["reactor"] = 0.5; s.Reactor.Loops[0].Hp = 0.5;
        b.RepairFirst("reactor");
        double rate = b.Tuning.RepairPerES * (1 + Rules.OfficerSkill(s, "repair") * b.Data.Systems.Officers.Ability.RepairRate);
        Assert.Equal(1.0 / rate, b.JobEta(s, "reactor"), 3);
        Assert.Equal(0, b.JobEta(s, "shields"));
        s.Sys["shields"] = 0.9; // damaged, but the only crew is on the reactor
        Assert.Equal(double.PositiveInfinity, b.JobEta(s, "shields"));
    }

    [Fact]
    public void Dragging_a_job_reorders_the_queue_quietly_and_the_drop_says_where_it_landed()
    {
        Battle b = Quiet();
        Ship s = b.Player;
        int log = b.Log.Count;
        b.RepairMove("warp", 0, true);
        Assert.Equal("warp", s.RepairQueue[0]);
        Assert.Equal(log, b.Log.Count);
        b.RepairDropped("warp", 2);
        Assert.Equal("Warp nacelles: first in the repair queue.", b.Log[^1].Text);
        Assert.Equal(b.Data.Systems.Repair.Jobs.Count, s.RepairQueue.Distinct().Count());
    }

    [Fact]
    public void The_escape_scene_starts_with_the_battles_damage_from_both_sides()
    {
        foreach (string id in new[] { "concord_escape", "krai_escape", "kelvane_escape" })
        {
            var b = new Battle(TestData.Data, TestData.Data.ScenarioById(id));
            Ship s = b.Player;
            Assert.Equal(0.4, Rules.Confinement(s));
            Assert.Equal(new[] { 0.3, -0.22, 0.08 }, s.Reactor.Coils);
            Assert.Equal(0.25, s.Reactor.Loops[0].Hp);
            Assert.Equal(72, s.Reactor.Temp);
            Assert.Equal(new[] { 0.15, 0.35 }, s.Warp.Nacelles);
            Assert.Equal(22, s.Warp.Banks);
            Assert.Equal(4, s.Power["repair"]);
            Assert.Equal(2, s.Power["weapons"]);
            Assert.Equal(0, s.Mounts[4].Hp);
            Assert.Equal(0.3, s.Mounts[1].Hp);
            Assert.Equal(s.Class.HullHp * 0.35, s.Hull, 6);
            // four crews: three on the first damaged job (engines), one on the reactor
            Assert.Equal(new[] { ("engines", 3), ("reactor", 1) }, b.RepairPlan(s).ToArray());
        }
    }
}
