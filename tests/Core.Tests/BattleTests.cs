using System;
using System.Linq;
using System.Text;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class BattleTests
{
    private static string Snapshot(Battle b)
    {
        var sb = new StringBuilder();
        foreach (Ship s in b.Ships)
            sb.Append($"{s.Id}:{s.X:F3},{s.Z:F3},{s.H:F5},{s.Hull:F3},{string.Join('/', s.Shields.Select(v => v.ToString("F3")))};");
        return sb.ToString();
    }

    private static Battle Run(int steps)
    {
        Battle b = TestData.NewBattle();
        b.Lock("vorrak");
        for (int i = 0; i < steps; i++) { if (i % 30 == 0) b.Fire(); b.Step(); }
        return b;
    }

    [Fact]
    public void A_battle_replays_exactly_from_its_seed_and_orders()
    {
        Assert.Equal(Snapshot(Run(1800)), Snapshot(Run(1800)));
    }

    [Fact]
    public void The_golden_seed_battle_reaches_the_pinned_state_after_one_minute()
    {
        Battle b = Run(1800);
        Assert.Equal(60, b.T, 6);
        // Pinned from the first run of this rule set (seed 20261101). A change here means the simulation changed: if
        // that was meant, re-pin and say so in the commit.
        Assert.Equal(GoldenSeed.Hulls, string.Join(",", b.Ships.Select(s => s.Hull.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))));
    }

    [Fact]
    public void Orders_given_while_paused_change_the_orders_but_not_the_ship()
    {
        Battle b = TestData.NewBattle();
        double h = b.Player.H, x = b.Player.X;
        b.Turn(45);
        b.SetSpeed(0);
        Assert.Equal(h, b.Player.H);
        Assert.Equal(x, b.Player.X);
        Assert.Equal(45, b.Player.HOrder / Geometry.Deg, 6);
        b.Step();
        Assert.NotEqual(h, b.Player.H);
    }

    [Fact]
    public void The_ship_turns_toward_the_ordered_heading_at_its_turn_rate()
    {
        Battle b = TestData.NewBattle();
        b.Turn(90);
        for (int i = 0; i < 30; i++) b.Step();
        Assert.Equal(26, b.Player.H / Geometry.Deg, 3); // the cruiser turns 26 degrees a second
    }

    [Fact]
    public void Fire_looses_only_selected_mounts_that_are_ready_in_arc_and_in_range()
    {
        Battle b = TestData.NewBattle();
        Ship t = b.ShipById("vorrak")!;
        t.X = b.Player.X; t.Z = b.Player.Z - 5000; // dead ahead, 5 km
        b.Lock("vorrak");
        foreach (Mount m in b.Player.Mounts) m.Charge = 1;
        Mount[] would = b.WouldFire();
        // ahead in arc: both forward lances, both broadside lances (90 +- 90 reaches the bow), both torpedoes; not the aft lance, not point defence
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, would.Select(m => m.Index).ToArray());
        b.ToggleVolley();
        Assert.Single(b.WouldFire());
        Assert.Equal(1, b.Fire());
    }

    [Fact]
    public void Red_alert_raises_shields_and_arms_offline_weapons()
    {
        Battle b = TestData.NewBattle();
        b.ToggleShields();
        b.SetArming(Arming.Offline);
        b.SetAlert(AlertLevel.Red);
        Assert.True(b.Player.ShieldsUp);
        Assert.All(b.Player.Mounts, m => Assert.Equal(Arming.Normal, m.State));
    }

    [Fact]
    public void Taking_command_moves_the_player_to_that_ship()
    {
        Battle b = TestData.NewBattle();
        b.TakeCommand("tallis");
        Assert.Equal("tallis", b.Player.Id);
        b.TakeCommand("vorrak"); // an enemy: changes nothing
        Assert.Equal("tallis", b.Player.Id);
    }

    [Fact]
    public void Next_target_locks_enemies_nearest_first_and_wraps()
    {
        Battle b = TestData.NewBattle();
        string[] seen = Enumerable.Range(0, 4).Select(_ => { b.NextTarget(); return b.Player.Target!; }).ToArray();
        Assert.Equal(seen[0], seen[3]);
        Assert.Equal(3, seen.Take(3).Distinct().Count());
    }

    [Fact]
    public void Non_finite_orders_change_nothing()
    {
        Battle b = TestData.NewBattle();
        double h = b.Player.HOrder, v = b.Player.VOrder;
        b.Turn(double.NaN); b.SetSpeed(double.PositiveInfinity); b.HelmTo(double.NaN, 0);
        Assert.Equal(h, b.Player.HOrder);
        Assert.Equal(v, b.Player.VOrder);
    }
}
