using System.Linq;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class RulesTests
{
    [Fact]
    public void Engine_boxes_set_the_speed_limit()
    {
        Battle b = TestData.NewBattle();
        Assert.Equal(180, b.Rules.MaxSpeed(b.Player), 6);
        b.SetPower("engines", 4);
        Assert.Equal(120, b.Rules.MaxSpeed(b.Player), 6);
        b.SetSpeed(300);
        Assert.Equal(120, b.Player.VOrder, 6);
    }

    [Fact]
    public void Energy_is_never_allocated_past_the_reactor_output()
    {
        Battle b = TestData.NewBattle();
        Assert.Equal(21, b.Rules.Used(b.Player)); // the cruiser's start: 6 + 5 + 6 + 1 + 1, and two coolant pumps, of 22 E
        b.SetPower("tractor", 10);
        Assert.Equal(1, b.Player.Power["tractor"]);
        Assert.Equal(22, b.Rules.Used(b.Player));
        b.SetPower("transport", 1); // a box past the output is locked: tapping it changes nothing
        Assert.Equal(0, b.Player.Power["transport"]);
    }

    [Fact]
    public void A_shot_from_dead_ahead_strikes_the_forward_facing_and_one_from_astern_the_aft()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player; // heading 0, north is -z
        Assert.Equal(0, Rules.FacingFrom(s, s.X, s.Z - 1000));
        Assert.Equal(3, Rules.FacingFrom(s, s.X, s.Z + 1000));
        Assert.Equal(1, Rules.FacingFrom(s, s.X + 1000, s.Z - 600));
        Assert.Equal(5, Rules.FacingFrom(s, s.X - 1000, s.Z - 600));
    }

    [Fact]
    public void The_chance_to_hit_falls_with_range_and_is_zero_past_the_reach()
    {
        Battle b = TestData.NewBattle();
        Ship s = b.Player, t = b.ShipById("vorrak")!;
        Mount lance = s.Mounts[0];
        t.V = 0; t.Ecm = 0;
        t.X = s.X; t.Z = s.Z - 2000;
        double near = b.Rules.HitChance(s, lance, t);
        t.Z = s.Z - 10000;
        double far = b.Rules.HitChance(s, lance, t);
        t.Z = s.Z - 13000;
        Assert.True(near > far);
        Assert.Equal(0, b.Rules.HitChance(s, lance, t));
    }

    [Fact]
    public void Cooldowns_read_one_decimal_under_ten_seconds_and_never_zero_while_cooling()
    {
        Assert.Equal("0.1", Rules.FormatCooldown(0.01));
        Assert.Equal("9.9", Rules.FormatCooldown(9.85));
        Assert.Equal("10", Rules.FormatCooldown(9.99));
        Assert.Equal("12", Rules.FormatCooldown(11.2));
    }

    [Fact]
    public void A_mount_status_follows_its_health_and_arming()
    {
        Battle b = TestData.NewBattle();
        Mount m = b.Player.Mounts[0];
        Assert.Equal(SystemStatus.Active, b.Rules.Status(m));
        m.Hp = 0.8; Assert.Equal(SystemStatus.Damaged, b.Rules.Status(m));
        m.Hp = 0.2; Assert.Equal(SystemStatus.Disabled, b.Rules.Status(m));
        m.Hp = 0; Assert.Equal(SystemStatus.Destroyed, b.Rules.Status(m));
        m.Hp = 1; m.State = Arming.Offline; Assert.Equal(SystemStatus.Off, b.Rules.Status(m));
        Assert.Equal(SystemStatus.Off, b.Rules.Status(b.Player, "tractor")); // no energy
        Assert.Equal(SystemStatus.Active, b.Rules.Status(b.Player, "engines"));
    }
}
