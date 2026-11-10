using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class StackingTests
{
    // a cruiser 200 m long and 20 m high, a frigate 80 m long and 10 m high, in metres
    private static Stacking.Hull Cruiser(string id, double x, double z, bool player = false, int order = 0) => new(id, x, z, 0, 30, 100, -10, 10, player, order);
    private static Stacking.Hull Frigate(string id, double x, double z, double h = 0, bool player = false, int order = 1) => new(id, x, z, h, 12, 40, -5, 5, player, order);

    [Fact]
    public void Ships_apart_stay_on_the_plane()
    {
        var y = Stacking.Heights(new[] { Cruiser("c", 0, 0), Frigate("f", 1000, 0) });
        Assert.Equal(0, y["c"]);
        Assert.Equal(0, y["f"]);
    }

    [Fact]
    public void A_smaller_ship_over_a_larger_one_is_drawn_clear_above_it_and_the_larger_stays_down()
    {
        var y = Stacking.Heights(new[] { Frigate("f", 10, 20, 1.2, player: true), Cruiser("c", 0, 0) });
        Assert.Equal(0, y["c"]);
        // its keel above the cruiser's top, with the gap
        Assert.Equal(10 + 20 * Stacking.GapX + 5, y["f"], 9);
    }

    [Fact]
    public void Of_two_sister_ships_the_one_you_command_stays_down_whatever_the_order()
    {
        var y = Stacking.Heights(new[] { Frigate("a", 0, 0, order: 0), Frigate("me", 5, 5, player: true, order: 5) });
        Assert.Equal(0, y["me"]);
        Assert.True(y["a"] > 0);
    }

    [Fact]
    public void Of_two_sister_ships_neither_yours_the_later_in_the_battle_order_climbs()
    {
        var y = Stacking.Heights(new[] { Frigate("b", 5, 5, order: 3), Frigate("a", 0, 0, order: 2) });
        Assert.Equal(0, y["a"]);
        Assert.True(y["b"] > 0);
    }

    [Fact]
    public void A_third_ship_over_two_stacked_ones_climbs_above_both()
    {
        var y = Stacking.Heights(new[] { Cruiser("c", 0, 0), Frigate("f1", 0, 10, order: 1), Frigate("f2", 0, -10, order: 2) });
        Assert.True(y["f2"] >= y["f1"] + 10, $"f1 {y["f1"]}, f2 {y["f2"]}");
    }

    [Fact]
    public void Footprints_follow_the_heading_two_long_ships_side_by_side_do_not_overlap_until_one_turns_across()
    {
        Stacking.Hull a = Frigate("a", 0, 0), b = Frigate("b", 60, 0);
        Assert.False(Stacking.Overlap(a, b));
        Assert.True(Stacking.Overlap(a, b with { H = System.Math.PI / 2 }));
    }
}
