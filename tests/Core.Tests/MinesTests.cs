// Mines, dropped astern or beamed out by transporter, arming and going off on an enemy (Battle.Mines; design 4a). Owner,
// 2026-11-01: "Mine laying should have a cool down system of 3 seconds".
using System;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class MinesTests
{
    private static MineDef M => TestData.Data.Systems.Mines;

    // a Concord cruiser with mines aboard and its transporter powered, and a Krai corvette far off holding still
    private static Battle Field(string lethal = "", double foeZ = -9000)
    {
        string json = "{\"id\": \"m\", \"name\": \"M\", \"side\": \"concord\", \"sky\": \"quiet\", \"seed\": 3, \"objective\": \"O\", \"say\": [\"Comms\", \"Hi.\"]," + lethal + " \"ships\": ["
            + "{\"id\": \"r\", \"name\": \"R\", \"captain\": \"C\", \"class\": \"ca\", \"faction\": \"concord\", \"side\": 0, \"x_m\": 0, \"z_m\": 0, \"heading_deg\": 0, \"speed_m_s\": 0, \"kit\": {\"mines\": 6}, \"power_e\": {\"weapons\": 3, \"transport\": 3}},"
            + "{\"id\": \"k\", \"name\": \"K\", \"captain\": \"C\", \"class\": \"krai_cv\", \"faction\": \"krai\", \"side\": 1, \"x_m\": 0, \"z_m\": " + foeZ + ", \"heading_deg\": 180, \"speed_m_s\": 0, \"stance\": \"hold\", \"hold_fire\": true}]}";
        Scenario sc = DataLoader.LoadScenario(StrictJson.Parse(json, "test"), TestData.Data.Classes, TestData.Data.Factions);
        return new Battle(TestData.Data, sc);
    }

    private static void Run(Battle b, double s) { for (int i = 0; i < Math.Round(s / b.Tuning.StepS); i++) b.Step(); }

    [Fact]
    public void A_ship_carries_its_class_loadout_of_mines_unless_the_scene_gives_it_its_own()
    {
        Battle b = new(TestData.Data, TestData.Data.ScenarioById("concord_battle"));
        Assert.Equal(TestData.Data.Systems.LoadoutOf("ca").Mines, b.Player.Mines);
        Assert.Equal(6, Field().Player.Mines);
    }

    [Fact]
    public void A_mine_is_dropped_astern_and_the_next_waits_three_seconds()
    {
        Battle b = Field();
        b.DropMine();
        Mine mn = Assert.Single(b.Mines);
        Assert.Equal(M.DropAsternM, mn.Z, 6);   // heading north, astern is south
        Assert.Equal(5, b.Player.Mines);
        Assert.Equal(M.CooldownS, b.Player.MineCool, 6);
        b.DropMine();
        Assert.Single(b.Mines);
        Run(b, M.CooldownS + 0.1);
        b.DropMine();
        Assert.Equal(2, b.Mines.Count);
    }

    [Fact]
    public void A_mine_is_beamed_only_inside_the_transporters_reach_and_with_it_powered()
    {
        Battle b = Field();
        b.PlaceMine(0, -(M.TransporterReachKm + 0.2) * 1000);
        Assert.Empty(b.Mines);
        b.PlaceMine(400, -900);
        Mine mn = Assert.Single(b.Mines);
        Assert.Equal((400.0, -900.0), (mn.X, mn.Z));
        Run(b, M.CooldownS + 0.1);
        b.SetPower("transport", 0);
        b.PlaceMine(0, -500);
        Assert.Single(b.Mines);
        Assert.False(b.CanBeamMine(b.Player));
    }

    [Fact]
    public void A_mine_arms_before_it_can_go_off_and_then_goes_off_on_the_first_enemy_inside_its_range()
    {
        Battle b = Field(foeZ: -1500);
        Ship k = b.ShipById("k")!;
        double hull = k.Hull;
        b.PlaceMine(0, -1000);
        Run(b, M.ArmS - 0.5);
        Assert.Single(b.Mines);
        Run(b, 1);
        Assert.Empty(b.Mines);
        Assert.True(k.Hull < hull || k.Shields.Sum() < k.Class.ShieldsHp.Sum(), "the mine struck it");
    }

    [Fact]
    public void A_mine_never_goes_off_on_its_own_side()
    {
        Battle b = Field();
        b.PlaceMine(0, -300);
        Run(b, M.ArmS + 2);
        Assert.Single(b.Mines);
    }

    [Fact]
    public void One_mine_destroys_its_target_where_mines_are_lethal()
    {
        Battle b = Field(" \"lethal\": [\"mine\"],", -1500);
        b.PlaceMine(0, -1000);
        Run(b, M.ArmS + 1);
        Assert.False(b.ShipById("k")!.Alive);
    }

    [Fact]
    public void The_placement_preview_turns_red_with_a_ship_inside_the_mine_range()
    {
        Battle b = Field(foeZ: -1500);
        Assert.True(b.ShipInMineRange(0, -1000));
        Assert.False(b.ShipInMineRange(3000, 3000));
        Assert.True(b.InMineReach(b.Player, 0, -1000));
        Assert.False(b.InMineReach(b.Player, 0, -2000));
    }

    [Fact]
    public void Ships_on_a_lane_run_hold_their_course_at_full_speed()
    {
        Battle b = new(TestData.Data, TestData.Data.ScenarioById("concord_mines"));
        Ship a = b.ShipById("ashkar")!;
        double h = a.H;
        Run(b, 10);
        Assert.Equal(h, a.H, 3);
        Assert.Equal(b.Rules.MaxSpeed(a), a.V, 0);
    }
}
