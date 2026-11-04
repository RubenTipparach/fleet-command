using FleetCommand.ConsoleModel;
using FleetCommand.Data;
using Xunit;

namespace FleetCommand.Tests;

public class LayoutTests
{
    private static LayoutDef L => TestData.Data.Layout;

    [Fact]
    public void At_the_reference_size_every_panel_sits_at_its_designed_rectangle()
    {
        ConsoleFrame f = ConsoleLayout.Compute(L, 1920, 1080, null);
        Assert.Equal(new Rect(6, 68, 468, 420), f.Regions[Region.Station]);
        Assert.Equal(new Rect(480, 0, 1440, 56), f.Regions[Region.Top]);
        Assert.Equal(new Rect(480, 1024, 1440, 56), f.Regions[Region.Bottom]);
        Assert.Equal(new Rect(480, 56, 1440, 968), f.View);
    }

    [Fact]
    public void A_wider_window_stretches_only_the_strips_and_the_view()
    {
        double s = ConsoleLayout.Scale(L, 2560, 1080);
        Assert.Equal(1, s);
        ConsoleFrame a = ConsoleLayout.Compute(L, 1920, 1080, null), b = ConsoleLayout.Compute(L, 2560 / s, 1080 / s, null);
        foreach (Region r in new[] { Region.Plate, Region.Station, Region.Ship, Region.Weapons, Region.Radar }) Assert.Equal(a.Regions[r], b.Regions[r]);
        Assert.Equal(2080, b.Regions[Region.Top].W);
        Assert.Equal(2080, b.View.W);
    }

    [Fact]
    public void A_narrow_window_scales_the_whole_console_down_past_the_minimum_width()
    {
        Assert.Equal(1000.0 / 1500.0, ConsoleLayout.Scale(L, 1000, 1080), 9);
    }

    [Fact]
    public void An_expanded_panel_takes_the_left_two_thirds_and_the_battle_the_right_third()
    {
        ConsoleFrame f = ConsoleLayout.Compute(L, 1920, 1080, Region.Station);
        Assert.Single(f.Regions);
        Assert.Equal(new Rect(6, 34, 1268, 1040), f.Regions[Region.Station]);
        Assert.Equal(new Rect(1280, 0, 640, 1080), f.View);
    }

    [Fact]
    public void A_strip_cannot_be_expanded()
    {
        ConsoleFrame f = ConsoleLayout.Compute(L, 1920, 1080, Region.Top);
        Assert.Null(f.Expanded);
        Assert.Equal(7, f.Regions.Count);
    }

    [Fact]
    public void A_skin_with_taller_strip_art_gets_taller_strips_and_the_view_gives_way()
    {
        ConsoleFrame f = ConsoleLayout.Compute(L, 1920, 1080, null, 1, 60, 80);
        Assert.Equal(new Rect(480, 0, 1440, 60), f.Regions[Region.Top]);
        Assert.Equal(new Rect(480, 1000, 1440, 80), f.Regions[Region.Bottom]);
        Assert.Equal(new Rect(480, 60, 1440, 940), f.View);
        Assert.Equal(L.Station, f.Regions[Region.Station]);
    }
}
