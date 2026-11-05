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

    // The owner, 2026-11-04: "as they fire, the source and destination of these beams must follow the ship source and
    // ship destination ... a missile fired can break this rule as it leaves behind a particle trail".
    private static Shot FirstShot(Battle b, Func<Shot, bool> pick)
    {
        for (int i = 0; i < 3600; i++)
        {
            if (i % 30 == 0) b.Fire();
            b.Step();
            Shot? sh = b.Shots.FirstOrDefault(pick);
            if (sh != null) return sh;
        }
        throw new InvalidOperationException("no such shot in a minute of battle");
    }

    [Fact]
    public void A_beam_keeps_its_ends_on_the_firing_mount_and_the_target_as_both_ships_move()
    {
        Battle b = TestData.NewBattle();
        b.Lock("vorrak");
        Shot beam = FirstShot(b, s => s.Kind == "beam" && s.To != null && s.Mount >= 0);
        Ship from = b.ShipById(beam.From)!, to = b.ShipById(beam.To)!;
        double fx = from.X, tx0 = to.X;
        b.Step();
        Assert.Contains(beam, b.Shots);
        Assert.NotEqual(fx, from.X);
        Assert.NotEqual(tx0, to.X);
        var (sx, sz, tx, tz) = b.Ends(beam);
        var (mx, mz) = Battle.MountWorld(from, from.Mounts[beam.Mount]);
        Assert.Equal(mx, sx, 9);
        Assert.Equal(mz, sz, 9);
        Assert.Equal(to.X, tx, 9);
        Assert.Equal(to.Z, tz, 9);
    }

    [Fact]
    public void A_disruptor_bolt_flies_along_the_live_line_between_its_mount_and_its_target()
    {
        Battle b = TestData.NewBattle();
        b.Lock("vorrak");
        Shot bolt = FirstShot(b, s => s.Kind == "bolt" && !s.Spent);
        for (int i = 0; i < 3 && b.Shots.Contains(bolt); i++)
        {
            b.Step();
            if (!b.Shots.Contains(bolt)) break;
            Ship from = b.ShipById(bolt.From)!, to = b.ShipById(bolt.To)!;
            var (mx, mz) = Battle.MountWorld(from, from.Mounts[bolt.Mount]);
            double ax = to.X - mx, az = to.Z - mz, px = bolt.X - mx, pz = bolt.Z - mz, len = Math.Sqrt(ax * ax + az * az);
            Assert.True(Math.Abs(ax * pz - az * px) / len < 1e-6, "the bolt is off the line from its mount to its target");
            double along = (ax * px + az * pz) / len;
            Assert.InRange(along, 0, len);
        }
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
        // ahead in arc: both forward lances, both broadside lances (90 +- 90 reaches the bow), both torpedoes, the chorus lance
        // on the deflector and the missile rack (it launches any way); not the aft lance, not point defence
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6, 10 }, would.Select(m => m.Index).ToArray());
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

    [Fact]
    public void Every_hit_that_lands_is_recorded_for_the_view_with_what_the_shields_took_and_what_reached_the_hull()
    {
        Battle b = TestData.NewBattle();
        b.Lock("vorrak");
        long last = 0;
        for (int i = 0; i < 3600; i++)
        {
            if (i % 30 == 0) b.Fire();
            b.Step();
            foreach (HitEvent e in b.Hits)
            {
                if (e.Seq <= last) continue;
                Assert.Equal(last + 1, e.Seq);   // none missed, none twice
                last = e.Seq;
                // every hit does damage, but a quell, which by design does none (it drops a shield and stuns weapons)
                Assert.True(e.Absorbed >= 0 && e.HullDamage >= 0 && (e.Absorbed + e.HullDamage > 0 || e.Kind == "quell"));
                Assert.Contains(b.Ships, s => s.Id == e.ShipId);
                if (e.Destroyed) Assert.False(b.Ships.First(s => s.Id == e.ShipId).Alive);
            }
            Assert.True(b.Hits.Count <= Battle.HitMemory);
        }
        Assert.True(last > 0, "no hit landed in two minutes");
    }
}
