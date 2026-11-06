// KitInk: the colour arithmetic every skin's ink rule uses (the mockups' lum(), one copy in each skin library there; one
// copy here, CLAUDE.md section 6.1): the relative luminance that decides whether dark ink or the skin's glow reads on a fill.
using System;
using Godot;

namespace FleetCommand.Game.Ui.Kit;

public static class KitInk
{
    /// <summary>Relative luminance of a colour (WCAG), for picking the ink that reads on a fill.</summary>
    public static float Luminance(Color col)
    {
        static float Lin(float v) => v <= 0.03928f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
        return 0.2126f * Lin(col.R) + 0.7152f * Lin(col.G) + 0.0722f * Lin(col.B);
    }

    /// <summary>The skins' ink rule: <paramref name="dark"/> on a lit fill (luminance above 0.2), <paramref name="glow"/> on
    /// a dark one.</summary>
    public static Color On(Color fill, Color dark, Color glow) => Luminance(fill) > 0.2f ? dark : glow;
}
