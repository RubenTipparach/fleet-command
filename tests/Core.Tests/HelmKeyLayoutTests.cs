using FleetCommand.ConsoleModel;
using FleetCommand.Data;
using Xunit;

namespace FleetCommand.Tests;

public class HelmKeyLayoutTests
{
    // The Kelvane key's glass, as measured from its renders: 19-77 of 96 each way.
    private static readonly Rect Kelvane = new(19 / 96.0, 19 / 96.0, 58 / 96.0, 58 / 96.0);

    private static bool Inside(Rect glass, Rect key, double x, double y) =>
        x >= key.X + glass.X * key.W - 1e-9 && x <= key.X + (glass.X + glass.W) * key.W + 1e-9 && y >= key.Y + glass.Y * key.H - 1e-9 && y <= key.Y + (glass.Y + glass.H) * key.H + 1e-9;

    [Fact]
    public void A_turn_angle_sits_inside_the_glass_not_on_the_frame()
    {
        var key = new Rect(0, 0, 52, 52);
        foreach (bool left in new[] { true, false })
        {
            HelmKeyContent c = HelmKeyLayout.For(key, Kelvane, true, left);
            Assert.True(Inside(Kelvane, key, c.Tx, c.Ty));
            Assert.True(Inside(Kelvane, key, c.Tx + (left ? -c.Size * 1.6 : c.Size * 1.6), c.Ty - c.Size * 0.75)); // three figures of the label
        }
    }

    [Fact]
    public void The_glyph_stays_inside_the_glass_with_room_to_spare()
    {
        var key = new Rect(10, 20, 52, 52);
        foreach (bool angle in new[] { true, false })
        {
            HelmKeyContent c = HelmKeyLayout.For(key, Kelvane, angle, false);
            double glassHalf = Kelvane.W * key.W / 2;
            Assert.True(c.R <= glassHalf * 0.8);
            Assert.True(Inside(Kelvane, key, c.Cx - c.R, c.Cy - c.R) && Inside(Kelvane, key, c.Cx + c.R, c.Cy + c.R));
        }
    }

    [Fact]
    public void A_port_turn_puts_its_angle_on_the_right_and_a_starboard_turn_on_the_left()
    {
        var key = new Rect(0, 0, 52, 52);
        Assert.True(HelmKeyLayout.For(key, Kelvane, true, true).Tx > 26);
        Assert.True(HelmKeyLayout.For(key, Kelvane, true, false).Tx < 26);
    }
}
