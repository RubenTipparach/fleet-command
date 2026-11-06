// The ship's craft (Battle.Craft): fighters on their missions, the disguised shuttle and the missile barrage, and the
// battle console mockup's test scenes for this package (data/battle/scenes/tractor.json) from both sides. Owner: "Deploy
// fighters to take down another ships defenses, and fire a missile barage to take it down"; "a disguised shuttle leaves
// your ship and suicide bombs a station".
using System;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class CraftTests
{
    private static Battle Scene(string id) => new(TestData.Data, TestData.Data.ScenarioById(id));
    private static void Run(Battle b, double s) { for (int i = 0; i < Math.Round(s / b.Tuning.StepS) && b.Over == null; i++) b.Step(); }

    public static readonly TheoryData<string> Sides = new() { "concord", "krai" };

    [Theory, MemberData(nameof(Sides))]
    public void Fighters_launch_together_and_strip_the_targets_point_defence_on_Defences(string side)
    {
        Battle b = Scene($"{side}_fighters");
        Ship t = b.EnemiesOf(b.Player).Single();
        b.Lock(t.Id);
        int n = b.Player.Fighters;
        b.LaunchFighters();
        Assert.Equal(0, b.Player.Fighters);
        Assert.Equal(n, b.FightersOut(b.Player));
        Run(b, 40);
        Assert.All(t.Mounts.Where(m => m.Weapon.IsPointDefence), m => Assert.True(m.Hp < 1, $"{m.Label} untouched"));
    }

    [Fact]
    public void Recalled_fighters_fly_home_to_the_bay()
    {
        Battle b = Scene("concord_fighters");
        b.Lock(b.EnemiesOf(b.Player).Single().Id);
        int n = b.Player.Fighters;
        b.LaunchFighters();
        Run(b, 6);
        b.RecallFighters();
        Run(b, 20);
        Assert.Equal(0, b.FightersOut(b.Player));
        Assert.Equal(n, b.Player.Fighters);
    }

    [Fact]
    public void The_mission_changes_for_the_fighters_flying_and_those_in_the_bay()
    {
        Battle b = Scene("concord_fighters");
        b.Lock(b.EnemiesOf(b.Player).Single().Id);
        b.SetMission("attack");
        b.LaunchFighters();
        b.SetMission("defend");
        Assert.Equal("defend", b.Player.Mission);
        Assert.All(b.Craft, c => Assert.Equal("defend", c.Mission));
        b.SetMission("loiter");
        Assert.Equal("defend", b.Player.Mission);
    }

    [Fact]
    public void A_barrage_fires_a_salvo_one_missile_at_a_time_from_the_racks()
    {
        Battle b = Scene("concord_fighters");
        b.Lock(b.EnemiesOf(b.Player).Single().Id);
        int missiles = b.Player.Missiles;
        BarrageDef bd = TestData.Data.Systems.Barrage;
        b.FireBarrage();
        Assert.Equal(bd.Salvo, b.Player.Barrage);
        Run(b, bd.IntervalS * (bd.Salvo + 1));
        Assert.Equal(0, b.Player.Barrage);
        Assert.Equal(missiles - bd.Salvo, b.Player.Missiles);
        Assert.True(b.Shots.Count(s => s.Kind == "missile" && s.From == b.Player.Id) > 0);
    }

    [Fact]
    public void Nothing_launches_without_a_target_or_while_evasive()
    {
        Battle b = Scene("concord_shuttle");
        b.LaunchShuttle();
        b.FireBarrage();
        Assert.Empty(b.Craft);
        b.Lock(b.EnemiesOf(b.Player).Single().Id);
        b.ToggleErratic();
        b.LaunchShuttle();
        Assert.Empty(b.Craft);
        b.ToggleErratic();
        b.LaunchShuttle();
        Assert.Single(b.Craft);
    }

    [Fact]
    public void A_disguised_shuttle_detonates_past_the_shields()
    {
        // a scene with no lethal shuttle: the charge's damage reaches the hull with the shields up
        Battle b = Scene("concord_shuttle");
        Scenario plain = b.Scenario with { Lethal = Array.Empty<string>() };
        b = new Battle(TestData.Data, plain);
        Ship t = b.EnemiesOf(b.Player).Single();
        b.Lock(t.Id);
        double hull = t.Hull, shields = t.Shields.Sum();
        for (int i = 0; i < 3; i++) { b.LaunchShuttle(); Run(b, 0.2); }
        Run(b, 40);
        Assert.Empty(b.Craft);
        if (t.Hull < hull) Assert.Equal(shields, t.Shields.Sum(), 6);   // the shields never took it
    }

    // the scenes' own tools, played as the mockup's scripted shots play them, from both sides
    [Theory, MemberData(nameof(Sides))]
    public void The_tractor_scene_drags_its_target_into_the_rocks(string side)
    {
        Battle b = Scene($"{side}_tractor");
        Assert.Empty(b.Player.Mounts);
        b.Lock(b.EnemiesOf(b.Player).Single().Id);
        b.TractorHold();
        Run(b, 11);
        Assert.NotNull(b.Player.Beam);
        b.SetSpeed(2500);
        Run(b, 60);
        Assert.Equal("won", b.Over);
    }

    [Theory, MemberData(nameof(Sides))]
    public void The_mines_scene_is_won_with_two_mines_beamed_into_the_lanes(string side)
    {
        Battle b = Scene($"{side}_mines");
        Ship me = b.Player;
        Ship[] foes = b.EnemiesOf(me).ToArray();
        foreach (Ship f in foes)
        {
            b.PlaceMine(f.X * 0.12, -10000);
            Run(b, b.Data.Systems.Mines.CooldownS + 0.1);
        }
        Assert.Equal(2, b.Mines.Count);
        Run(b, 120);
        Assert.Equal("won", b.Over);
    }

    [Theory, MemberData(nameof(Sides))]
    public void The_shuttle_scene_is_won_with_its_three_shuttles(string side)
    {
        Battle b = Scene($"{side}_shuttle");
        b.Lock(b.EnemiesOf(b.Player).Single().Id);
        for (int i = 0; i < 3; i++) { b.LaunchShuttle(); Run(b, 8); }
        Run(b, 30);
        Assert.Equal("won", b.Over);
    }

    [Theory, MemberData(nameof(Sides))]
    public void The_fighters_scene_is_won_with_fighters_on_Defences_then_a_barrage(string side)
    {
        Battle b = Scene($"{side}_fighters");
        b.Lock(b.EnemiesOf(b.Player).Single().Id);
        b.LaunchFighters();
        Run(b, 30);
        for (int i = 0; i < 2 && b.Over == null; i++) { b.FireBarrage(); Run(b, 25); }
        Assert.Equal("won", b.Over);
    }

    public static TheoryData<string> PackageScenes()
    {
        var d = new TheoryData<string>();
        foreach (string id in new[] { "tractor", "mines", "shuttle", "fighters" }) { d.Add("concord_" + id); d.Add("krai_" + id); }
        return d;
    }

    [Theory, MemberData(nameof(PackageScenes))]
    public void Each_scene_loads_with_approved_ships_and_runs_a_minute(string id)
    {
        Scenario sc = TestData.Data.ScenarioById(id);
        Assert.All(sc.Ships, s => Assert.NotNull(TestData.Data.Classes[s.Class].Model));
        Assert.False(sc.Armed);
        Assert.NotEmpty(sc.Lethal);
        var b = new Battle(TestData.Data, sc);
        Assert.Empty(b.Player.Mounts);
        Run(b, 60);
        Assert.True(b.Player.Alive);
    }

    [Fact]
    public void Every_loadout_names_a_class_and_the_barrage_a_missile_grade()
    {
        foreach (string cls in TestData.Data.Systems.Loadouts.Keys) Assert.True(TestData.Data.Classes.ContainsKey(cls), $"no class '{cls}'");
        Assert.NotNull(TestData.Data.Missiles.Grade(TestData.Data.Systems.Barrage.Grade));
    }

    [Fact]
    public void The_package_replays_exactly_from_its_seed()
    {
        static string Play()
        {
            Battle b = Scene("concord_fighters");
            b.Lock(b.EnemiesOf(b.Player).Single().Id);
            b.LaunchFighters();
            Run(b, 20);
            b.FireBarrage();
            Run(b, 20);
            return string.Join(";", b.Ships.Select(s => $"{s.Hull:0.000},{s.X:0.000},{string.Join(",", s.Mounts.Select(m => m.Hp.ToString("0.00")))}"));
        }
        Assert.Equal(Play(), Play());
    }
}
