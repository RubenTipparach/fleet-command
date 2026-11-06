// The damage range (Battle.Range, data/battle/range.json): its shots are a battle's hits, its programs run on the battle's
// step and replay from the seed, its ships stand with shields down and no point defence, and only a range takes its orders.
using System.Linq;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class RangeTests
{
    private static Battle Range(string side = "concord") => new(TestData.Data, TestData.Data.ScenarioById(side + "_range"));

    private static void Run(Battle b, double s) { for (int i = 0; i < s / b.Tuning.StepS; i++) b.Step(); }

    [Fact]
    public void Each_side_has_a_range_with_every_class_that_has_an_approved_ship()
    {
        foreach (string side in new[] { "concord", "krai" })
        {
            Battle b = Range(side);
            var classes = b.Ships.Where(s => s.Side == 1).Select(s => s.Class.Id).ToHashSet();
            var approved = TestData.Data.Classes.Values.Where(c => c.Model != null).Select(c => c.Id).ToHashSet();
            Assert.True(approved.SetEquals(classes), $"{side}_range has {string.Join(", ", classes)}");
        }
    }

    [Fact]
    public void Range_ships_stand_with_shields_down_and_point_defence_off()
    {
        Battle b = Range();
        foreach (Ship s in b.Ships.Where(s => s.Side == 1))
        {
            Assert.False(s.ShieldsUp);
            Assert.All(s.Mounts.Where(m => m.Weapon.IsPointDefence), m => Assert.Equal(Arming.Offline, m.State));
        }
    }

    [Fact]
    public void A_range_lance_lands_on_the_hull_at_once_and_is_recorded_as_a_hit()
    {
        Battle b = Range();
        Ship t = b.ShipById("t_kca")!;
        double before = t.Hull;
        b.RangeFire("t_kca", 90, "lance");
        Assert.Equal(before - TestData.Data.Weapons["lance"].Normal.Damage, t.Hull, 6);
        HitEvent hit = b.Hits.Last();
        Assert.Equal("t_kca", hit.ShipId);
        Assert.Equal("beam", hit.Kind);
        // it came from 90 degrees to starboard of a ship heading east: from the south, 3 km out
        Assert.True(hit.FromZ > t.Z + 2900);
    }

    [Fact]
    public void A_range_torpedo_flies_in_and_lands_its_overload_damage()
    {
        Battle b = Range();
        Ship t = b.ShipById("t_ff")!;
        double before = t.Hull;
        b.RangeFire("t_ff", 0, "torpedo", overload: true);
        Assert.Equal(before, t.Hull);
        Run(b, 3);
        Assert.Equal(before - TestData.Data.Weapons["torpedo"].Overload!.Damage, t.Hull, 6);
    }

    [Fact]
    public void Raised_shields_take_a_range_shot_and_the_hull_is_untouched()
    {
        Battle b = Range();
        Ship t = b.ShipById("t_ca")!;
        b.RangeShields("t_ca", true);
        double hull = t.Hull;
        b.RangeFire("t_ca", 0, "lance");
        Assert.Equal(hull, t.Hull);
        Assert.True(b.Hits.Last().Absorbed > 0);
    }

    [Fact]
    public void The_core_breach_program_destroys_its_ship_and_the_range_does_not_end()
    {
        Battle b = Range();
        b.RangeRun("t_kcv", "breach");
        Run(b, 60);
        Assert.False(b.ShipById("t_kcv")!.Alive);
        Assert.Null(b.RangeRunning);
        Assert.Null(b.Over);
    }

    [Fact]
    public void Battle_scars_stops_while_the_ship_is_still_whole()
    {
        Battle b = Range();
        Ship t = b.ShipById("t_dd")!;
        b.RangeRun("t_dd", "scars");
        Run(b, 40);
        Assert.Null(b.RangeRunning);
        Assert.True(t.Alive);
        // above the share at which the view starts blowing parts off (ShipDamage.ChunkBelowHull, 0.6): scarred, not broken
        Assert.InRange(t.Hull / t.Class.HullHp, 0.6, 0.78);
    }

    [Fact]
    public void A_program_replays_exactly_from_the_seed()
    {
        double[] Hits() { Battle b = Range(); b.RangeRun("t_kdn", "rake"); Run(b, 5); return b.Hits.Select(h => h.FromX + h.FromZ).ToArray(); }
        Assert.Equal(Hits(), Hits());
    }

    [Fact]
    public void Only_a_damage_range_takes_range_orders()
    {
        Battle b = new(TestData.Data, TestData.Data.ScenarioById("concord_duel"));
        Ship t = b.Ships[1];
        double hull = t.Hull;
        b.RangeFire(t.Id, 0, "torpedo");
        b.RangeRun(t.Id, "breach");
        Run(b, 2);
        Assert.Equal(hull, t.Hull);
        Assert.Null(b.RangeRunning);
    }
}
