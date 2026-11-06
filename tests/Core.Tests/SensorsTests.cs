// Sensors, ECM and ECCM, and nebulae (Battle.Sensors, Rules.Ew, Battle.CanLock; the battle console mockup's Science station
// and SCENES.nebula; owner, 2026-11-04: "We also need a nebula scenario to test out the ECM/ECCM systems"). The numbers are
// data/battle/systems/cloak.json and data/battle/tuning.json.
using System;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class SensorsTests
{
    private static CloakSystems Cs => TestData.Data.Systems.Cloak;
    private static Tuning Tu => TestData.Data.Tuning;
    private const string Nebula = "\"nebula\": {\"x_m\": 0, \"z_m\": -70000, \"r_m\": 48000, \"density\": 0.85}, ";

    private static void Run(Battle b, double s) { for (int i = 0; i < Math.Round(s / b.Tuning.StepS); i++) b.Step(); }

    [Fact]
    public void Ecm_and_eccm_share_the_sensors_energy_and_never_pass_it()
    {
        Battle b = CloakTests.Pair("ca", "concord", "krai_ff", "krai", -90000, extraA: ", \"power_e\": {\"sensors\": 4}");
        b.SetEw(false, 3);
        Assert.Equal(3, b.Player.Ecm);
        b.SetEw(true, 3);
        Assert.Equal(1, b.Player.Eccm);
        b.SetEw(true, 9);
        Assert.Equal(1, b.Player.Eccm);
        b.SetPower("sensors", 2);
        Assert.Equal(2, b.Player.Ecm);
        Assert.Equal(0, b.Player.Eccm);
    }

    [Fact]
    public void Eccm_adds_and_the_targets_ecm_takes_off_one_step_a_point_in_the_chance_to_hit()
    {
        Battle b = CloakTests.Pair("ca", "concord", "krai_ff", "krai", -30000, extraA: ", \"power_e\": {\"sensors\": 4}", extraB: ", \"hold_fire\": true");
        Ship t = b.ShipById("b")!;
        Mount m = b.Player.Mounts.First(x => x.Weapon.Id == "lance");
        t.Ecm = 0;
        double p0 = b.Rules.HitChance(b.Player, m, t);
        b.SetEw(true, 2);
        Assert.Equal(p0 + 2 * Tu.EcmStep * (0.5 + 0.5 * b.Player.Sys["sensors"]), b.Rules.HitChance(b.Player, m, t), 9);
        t.Ecm = 3;
        Assert.Equal(p0 - 1 * Tu.EcmStep * (0.5 + 0.5 * b.Player.Sys["sensors"]), b.Rules.HitChance(b.Player, m, t), 9);
    }

    [Fact]
    public void A_nebula_is_densest_in_its_middle_thinning_to_its_edge_and_nothing_outside()
    {
        Battle b = CloakTests.Pair("ca", "concord", "krai_ff", "krai", -70000, scene: Nebula);
        Assert.Equal(0.85, b.Rules.NebulaAt(0, -70000), 9);
        Assert.Equal(0.85 * (1 - Cs.Nebula.EdgeThinFrac * 0.5), b.Rules.NebulaAt(24000, -70000), 9);
        Assert.Equal(0, b.Rules.NebulaAt(0, 0));
    }

    [Fact]
    public void Inside_a_nebula_every_shot_loses_its_penalty_and_ecm_counts_its_gain_more()
    {
        Battle b = CloakTests.Pair("ca", "concord", "krai_ff", "krai", -70000, scene: Nebula, extraB: ", \"hold_fire\": true");
        Ship t = b.ShipById("b")!;
        t.Ecm = 2;
        double nb = b.Rules.NebulaAt(t.X, t.Z);
        double want = -Tu.EcmStep * 2 * (1 + Cs.Nebula.EwGainX * nb) - Cs.Nebula.HitPenalty * nb;
        Assert.Equal(want, b.Rules.Ew(b.Player, t), 9);
    }

    [Fact]
    public void A_ship_in_a_nebula_cannot_be_locked_from_beyond_its_lock_range_unless_eccm_beats_its_ecm()
    {
        Battle b = CloakTests.Pair("ca", "concord", "krai_cv", "krai", -78000, scene: Nebula, extraA: ", \"power_e\": {\"sensors\": 4}", extraB: ", \"ecm\": 3, \"power_e\": {\"sensors\": 4}, \"hold_fire\": true");
        Ship t = b.ShipById("b")!;
        Assert.False(b.CanLock(b.Player, t));
        b.Lock("b");
        Assert.Null(b.Player.Target);
        Assert.Contains(b.Log, l => l.Text.StartsWith("No lock: the nebula hides it"));
        b.SetEw(true, 3);
        Assert.False(b.CanLock(b.Player, t));   // equal is not enough
        b.SetEw(true, 4);
        Assert.True(b.CanLock(b.Player, t));
        b.SetEw(true, 0);
        t.Z = -(Cs.Nebula.LockKm * 1000 - 2000);
        Assert.True(b.CanLock(b.Player, t));
    }

    [Fact]
    public void A_scan_runs_on_the_sensor_points_left_and_stops_when_the_target_changes()
    {
        Battle b = CloakTests.Pair("ca", "concord", "krai_ff", "krai", -50000, extraA: ", \"power_e\": {\"sensors\": 3}", extraB: ", \"hold_fire\": true");
        b.Player.Officers.Clear();   // the base rule, without a science officer's speed
        b.Lock("b");
        b.Scan();
        Run(b, 1);
        double rate = Cs.Sensors.ScanBasePerS + Cs.Sensors.ScanPerES * 3;
        Assert.Equal(rate, b.Player.ScanP, 3);
        b.SetPassive(true);
        double p = b.Player.ScanP;
        Run(b, 1);
        Assert.Equal(rate * Cs.Sensors.PassiveScanX, b.Player.ScanP - p, 3);
        b.Unlock();
        b.Step();
        Assert.Null(b.Player.ScanId);
        Assert.Equal(0, b.Player.ScanP);
    }

    [Fact]
    public void A_ship_on_passive_sensors_is_harder_to_hit()
    {
        Battle b = CloakTests.Pair("ca", "concord", "krai_ff", "krai", -30000, extraB: ", \"hold_fire\": true");
        Ship t = b.ShipById("b")!;
        double a = b.Rules.Ew(b.Player, t);
        t.Passive = true;
        Assert.Equal(a - Cs.Sensors.PassiveHit, b.Rules.Ew(b.Player, t), 9);
    }

    [Fact]
    public void The_nebula_scenes_raider_jams_with_three_points_of_ecm_and_is_found_with_four_of_eccm()
    {
        foreach (string id in new[] { "concord_nebula", "krai_nebula" })
        {
            var b = new Battle(TestData.Data, TestData.Data.ScenarioById(id));
            Ship t = b.Ships[1];
            Assert.Equal(3, t.Ecm);
            Assert.False(b.CanLock(b.Player, t));
            b.SetEw(true, 4);
            Assert.True(b.CanLock(b.Player, t), id);
        }
    }
}
