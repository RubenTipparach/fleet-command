// HelmKeyLayout: where a helm key's glyph and turn angle go inside the key's glass (owner, 2026-11-06, on the Institute
// console: "the numbers arent in the buttons for turn angle, also these icons need a little more space on these
// buttons"). The glass is the part of the key the skin lights (all of a drawn key; the window inside a rendered key's
// frame). The glyph is a little smaller than the glass; a turn's angle sits in the glass's lower corner away from the
// turn, with the glyph lifted to leave it room. Pure arithmetic in Core so the game and the tests share it; the battle
// console mockup lays its keys out by the same numbers (docs/mockups/battle-console.html, helmKeyLayout).
using System;
using FleetCommand.Data;

namespace FleetCommand.ConsoleModel;

/// <summary>A helm key's content: the glyph's centre and radius, and the angle label's baseline anchor, size and side.</summary>
public readonly record struct HelmKeyContent(double Cx, double Cy, double R, bool HasAngle, double Tx, double Ty, int Size, bool AlignRight);

public static class HelmKeyLayout
{
    /// <param name="key">The key's rectangle.</param>
    /// <param name="glass">The glass as fractions of the key: X, Y, W and H from 0 to 1.</param>
    /// <param name="hasAngle">True for a turn key that shows its angle.</param>
    /// <param name="leftTurn">A turn to port: its angle goes in the lower right corner, else the lower left.</param>
    public static HelmKeyContent For(Rect key, Rect glass, bool hasAngle, bool leftTurn)
    {
        double wx = key.X + glass.X * key.W, wy = key.Y + glass.Y * key.H, ww = glass.W * key.W, wh = glass.H * key.H, m = Math.Min(ww, wh);
        if (!hasAngle) return new HelmKeyContent(wx + ww / 2, wy + wh / 2, m * 0.4, false, 0, 0, 0, false);
        int size = Math.Max(8, (int)Math.Round(m * 0.26));
        double pad = m * 0.11;
        return new HelmKeyContent(wx + ww / 2, wy + wh / 2 - m * 0.09, m * 0.34, true, leftTurn ? wx + ww - pad : wx + pad, wy + wh - pad, size, leftTurn);
    }
}
