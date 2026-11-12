// LowSpec: the low-spec mode, for a small machine such as a Raspberry Pi 5 (owner, 2026-11-12: "Make a lowres simplified
// version of this game (nebula just standard fog and billboards) and see how well the game runs on a pi5", "lower resolution
// of UI down to max 512 on each texture"). Started with --lowspec (and the Compatibility renderer, --rendering-method
// gl_compatibility, which the Pi's GPU runs). One switch, read once, that each place asks of: the 3D view drawn at a share
// of the window's pixels with no MSAA, the sun's shadows off, and the nebula as fog and billboards (NebulaPuffs'
// "billboards" style, never the marched decks). The textures' 512 px cap is the import's (scripts/lowspec-prep.py on the
// small machine's checkout), so no texture is loaded large and then shrunk.
using System.Linq;
using Godot;

namespace FleetCommand.Game;

public static class LowSpec
{
    /// <summary>Whether the game runs in the low-spec mode (--lowspec).</summary>
    public static readonly bool On = OS.GetCmdlineUserArgs().Contains("--lowspec");

    /// <summary>The share of the window's width and height the 3D view is drawn at (--lowspec-scale=0.5 by default).</summary>
    public static readonly float Scale3D = ReadScale();

    /// <summary>The MSAA a 3D viewport uses: its own, or none in the low-spec mode.</summary>
    public static Viewport.Msaa Msaa(Viewport.Msaa normal) => On ? Viewport.Msaa.Disabled : normal;

    /// <summary>Sets a viewport's 3D drawing to the low-spec mode's (no MSAA, a share of its pixels); nothing when off.</summary>
    public static void Apply(Viewport vp)
    {
        if (!On) return;
        vp.Msaa3D = Viewport.Msaa.Disabled;
        vp.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
        vp.Scaling3DMode = Viewport.Scaling3DModeEnum.Bilinear;
        vp.Scaling3DScale = Scale3D;
    }

    private static float ReadScale()
    {
        foreach (string a in OS.GetCmdlineUserArgs())
            if (a.StartsWith("--lowspec-scale=") && float.TryParse(a["--lowspec-scale=".Length..], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s) && s is >= 0.25f and <= 1f)
                return s;
        return 0.5f;
    }
}
