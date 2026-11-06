// The tractor beam's contest of rounds and the asteroids it drags ships into (Battle.Tractor; design 4a). Owner,
// 2026-11-01: "Hold and break should be only two options", "Tractor beam does take 10 seconds to power up", "If enemy
// tractor beam module is down or is less than .5x of the breaking ships strength. The ship breaking the beam then
// becomes the holder".
using System;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class TractorTests
{
    private static TractorDef T => TestData.Data.Systems.Tractor;

    // a Concord cruiser and a Krai frigate 2 km apart, both still and holding fire; the cruiser has 5 E in its tractor
    private static Battle Pair(string extra = "", string rocks = "")
    {
        string json = "{\"id\": \"t\", \"name\": \"T\", \"side\": \"concord\", \"sky\": \"quiet\", \"seed\": 5, \"objective\": \"O\", \"say\": [\"Comms\", \"Hi.\"]," + rocks + " \"ships\": ["
            + "{\"id\": \"r\", \"name\": \"R\", \"captain\": \"C\", \"class\": \"ca\", \"faction\": \"concord\", \"side\": 0, \"x_m\": 0, \"z_m\": 0, \"heading_deg\": 180, \"speed_m_s\": 0, \"power_e\": {\"weapons\": 1, \"tractor\": 5}},"
            + "{\"id\": \"v\", \"name\": \"V\", \"captain\": \"C\", \"class\": \"krai_ff\", \"faction\": \"krai\", \"side\": 1, \"x_m\": 0, \"z_m\": -20000, \"heading_deg\": 90, \"speed_m_s\": 0, \"stance\": \"hold\", \"hold_fire\": true" + extra + "}]}";
        Scenario sc = DataLoader.LoadScenario(StrictJson.Parse(json, "test"), TestData.Data.Classes, TestData.Data.Factions);
        var b = new Battle(TestData.Data, sc);
        b.Lock("v");
        return b;
    }

    private static void Run(Battle b, double s) { for (int i = 0; i < Math.Round(s / b.Tuning.StepS); i++) b.Step(); }

    [Fact]
    public void Hold_powers_the_beam_up_for_ten_seconds_then_takes_the_target_at_half_grip()
    {
        Battle b = Pair();
        b.TractorHold();
        Assert.Equal(TractorMode.Power, b.Player.Tractor);
        Run(b, T.PowerupS - 0.5);
        Assert.Null(b.Player.Beam);
        Run(b, 1);
        Assert.NotNull(b.Player.Beam);
        Assert.Equal("v", b.Player.Beam!.On);
        Assert.Equal(T.GripStartX, b.Player.Beam.Grip, 6);
        Assert.Same(b.Player, b.HeldBy(b.ShipById("v")!));
    }

    [Fact]
    public void The_power_up_pauses_while_the_tractor_has_no_energy()
    {
        Battle b = Pair();
        b.TractorHold();
        b.SetPower("tractor", 0);
        Run(b, T.PowerupS + 2);
        Assert.Equal(TractorMode.Power, b.Player.Tractor);
        Assert.Equal(T.PowerupS, b.Player.PowerT, 6);
    }

    [Fact]
    public void Hold_does_nothing_with_no_tractor_energy_or_while_evasive()
    {
        Battle b = Pair();
        b.SetPower("tractor", 0);
        b.TractorHold();
        Assert.Equal(TractorMode.Off, b.Player.Tractor);
        b.SetPower("tractor", 5);
        b.ToggleErratic();
        b.TractorHold();
        Assert.Equal(TractorMode.Off, b.Player.Tractor);
    }

    [Fact]
    public void Break_lets_go_at_any_time_even_while_powering_up()
    {
        Battle b = Pair();
        b.TractorHold();
        Run(b, 3);
        b.TractorBreak();
        Assert.Equal(TractorMode.Off, b.Player.Tractor);
        b.TractorHold();
        Run(b, T.PowerupS + 1);
        Assert.NotNull(b.Player.Beam);
        b.TractorBreak();
        Assert.Null(b.Player.Beam);
        Assert.Null(b.HeldBy(b.ShipById("v")!));
    }

    [Fact]
    public void A_round_moves_the_grip_ten_percent_a_point_toward_the_higher_pick()
    {
        Battle b = Pair();
        b.TractorHold();
        Run(b, T.PowerupS + 0.5);
        Ship v = b.ShipById("v")!;
        TractorBeam beam = b.Player.Beam!;
        Run(b, T.RoundS);
        Assert.NotNull(beam.Last);
        double expect = Math.Clamp(T.GripStartX + (beam.Last!.Holder - beam.Last.Held) * T.StepPerE, 0, 1);
        Assert.Equal(expect, beam.Grip, 6);
        Assert.Equal(Battle.BeamValue(b.Player), beam.Last.Holder, 6);
        Assert.Equal(2, beam.Round);
        // the AI crew picked a whole number of boxes, in its range, from its spare energy and its weapons
        Assert.InRange(v.Power["tractor"], T.AiPickMinE, T.AiPickMaxE);
    }

    [Fact]
    public void The_held_ship_breaks_free_at_zero_grip_and_turns_the_beam_when_the_holder_held_with_under_half()
    {
        Battle b = Pair();
        b.TractorHold();
        Run(b, T.PowerupS + 0.5);
        // the holder drops to 1 E in its tractor; the held crew picks at least 1, and grip falls until it breaks
        b.SetPower("tractor", 1);
        Ship v = b.ShipById("v")!;
        for (int i = 0; i < 40 && b.Player.Beam != null; i++) Run(b, T.RoundS);
        Assert.Null(b.Player.Beam);
        Assert.Equal("v", b.Player.RelockOn);
        // whether the beam turned follows the last round's picks, by the rule
        if (v.Beam != null) Assert.Equal(b.Player.Id, v.Beam.On);
    }

    [Fact]
    public void The_holders_tractor_going_down_frees_the_held_ship_which_takes_hold_of_the_holder()
    {
        Battle b = Pair();
        b.TractorHold();
        Run(b, T.PowerupS + 0.5);
        Ship v = b.ShipById("v")!;
        v.Power["weapons"] -= 3; v.Power["tractor"] = 3;   // the held ship's tractor is up
        b.Player.Sys["tractor"] = 0.2;                      // the holder's is disabled
        Run(b, 0.1);
        Assert.Null(b.Player.Beam);
        Assert.NotNull(v.Beam);
        Assert.Equal(b.Player.Id, v.Beam!.On);
        Assert.Same(v, b.HeldBy(b.Player));
    }

    [Fact]
    public void A_ship_that_broke_free_cannot_be_taken_by_the_same_holder_until_the_relock_runs_out()
    {
        Battle b = Pair();
        b.Player.RelockOn = "v"; b.Player.RelockT = T.PowerupS + T.RelockS;
        b.TractorHold();
        Run(b, T.PowerupS + 0.5);
        Assert.Null(b.Player.Beam);
        Run(b, T.RelockS);
        Assert.NotNull(b.Player.Beam);
    }

    [Fact]
    public void The_beam_is_lost_past_reach_and_never_takes_a_target_beyond_it()
    {
        Battle b = Pair();
        Ship v = b.ShipById("v")!;
        v.Z = -(T.ReachKm + 5) * 1000;
        b.TractorHold();
        Run(b, T.PowerupS + 1);
        Assert.Null(b.Player.Beam);
        Assert.Equal(TractorMode.Hold, b.Player.Tractor);
        v.Z = -20000;
        Run(b, 0.1);
        Assert.NotNull(b.Player.Beam);
        v.Z = -(T.ReachKm + T.ReachSlackKm + 3) * 1000;
        Run(b, 0.1);
        Assert.Null(b.Player.Beam);
        Assert.Equal(TractorMode.Off, b.Player.Tractor);
    }

    [Fact]
    public void The_holder_drags_the_held_ship_along_on_its_tether()
    {
        Battle b = Pair();
        b.TractorHold();
        Run(b, T.PowerupS + 0.5);
        Ship v = b.ShipById("v")!;
        double z0 = v.Z;
        b.SetSpeed(1500);
        Run(b, 15);
        Assert.True(v.Z > z0 + 500, $"the held ship moved from {z0:0} to {v.Z:0}");
        Assert.True(Rules.DistKm(b.Player, v) < T.ReachKm, "the tether keeps it inside reach");
    }

    [Fact]
    public void Reeling_moves_the_tether_by_steps_within_its_shortest_and_the_reach()
    {
        Battle b = Pair();
        b.TractorHold();
        Run(b, T.PowerupS + 0.5);
        TractorBeam beam = b.Player.Beam!;
        double want = beam.WantM;
        b.Reel(-1);
        Assert.Equal(want - T.ReelStepM, beam.WantM, 6);
        for (int i = 0; i < 40; i++) b.Reel(-1);
        Assert.Equal(T.MinTetherM, beam.WantM, 6);
        for (int i = 0; i < 40; i++) b.Reel(1);
        Assert.Equal(T.ReachKm * 1000, beam.WantM, 6);
    }

    [Fact]
    public void A_ship_dragged_into_a_rock_where_rocks_are_lethal_is_destroyed()
    {
        Battle b = Pair(rocks: " \"lethal\": [\"rock\"], \"rocks\": [{\"x_m\": 0, \"z_m\": -10000, \"radius_m\": 2400}],");
        b.TractorHold();
        Run(b, T.PowerupS + 0.5);
        Ship v = b.ShipById("v")!;
        b.SetSpeed(2000);
        Run(b, 40);
        Assert.False(v.Alive);
    }

    [Fact]
    public void A_ship_that_hits_a_rock_is_set_back_on_its_surface_and_takes_damage_scaled_by_speed()
    {
        Battle b = Pair(rocks: " \"rocks\": [{\"x_m\": 0, \"z_m\": 20000, \"radius_m\": 3000}],");
        b.Player.ShieldsUp = false;
        double hull = b.Player.Hull;
        b.SetSpeed(2000);
        Run(b, 20);
        Assert.True(b.Player.Hull < hull, "the hit and the micrometeor grit cost hull");
        double reach = 300 + b.HullRadiusM(b.Player);
        Assert.True(Geometry.Dist(b.Player.X, b.Player.Z, 0, 20000) >= reach - 1, "never inside the rock");
    }
}
