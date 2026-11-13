// LowSpec: the low-spec mode, for a small machine such as a Raspberry Pi 5 (owner, 2026-11-12: "Make a lowres simplified
// version of this game (nebula just standard fog and billboards) and see how well the game runs on a pi5", "lower resolution
// of UI down to max 512 on each texture"). Started with --lowspec (and the Compatibility renderer, --rendering-method
// gl_compatibility, which the Pi's GPU runs). One switch, read once, that each place asks of: the 3D view drawn at a share
// of the window's pixels with no MSAA, the sun's shadows off, and the nebula as fog and billboards (NebulaPuffs'
// "billboards" style, never the marched decks). The textures' 512 px cap is the import's (scripts/lowspec-prep.py on the
// small machine's checkout), so no texture is loaded large and then shrunk.
using System;
using System.Linq;
using Godot;

namespace FleetCommand.Game;

public static class LowSpec
{
    /// <summary>Whether the game runs in the low-spec mode (--lowspec).</summary>
    public static readonly bool On = OS.GetCmdlineUserArgs().Contains("--lowspec");

    /// <summary>The window the low-spec mode opens in the first time, before the player has chosen (the owner, 2026-11-13:
    /// "maintain 720p", "you can still change resolution in the settings", "its not a hard rule"): 1280 x 720, where a
    /// Raspberry Pi 5 runs a battle at about 38 fps; borderless fullscreen on its 2560 x 1080 monitor ran at 22.</summary>
    public static readonly Vector2I DefaultWindow = new(1280, 720);

    /// <summary>The view's shaders the low-spec mode never draws (it has baked planets, nebula shells and baked ships
    /// instead), which Prewarm leaves uncompiled: the full planet and volume-nebula shaders stall a Raspberry Pi 5's shader
    /// compiler for minutes (docs/playtests/pi/README.md), and with them the loading screen never reached the menu.</summary>
    public static readonly string[] UnusedShaders =
    {
        "planet.gdshader", "planet_gas.gdshader", "planet_atmosphere.gdshader", "planet_craters.gdshader", "planet_clouds.gdshader",
        "planet_rings.gdshader", "nebula_plume.gdshader", "nebula_deck.gdshader", "nebula_mesh.gdshader", "nebula_billboard.gdshader",
        "ship_bake.gdshader",
    };

    /// <summary>The share of the window's width and height the 3D view is drawn at (--lowspec-scale=1 by default: the owner,
    /// 2026-11-12, "I want 100% scale renderer too").</summary>
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

    /// <summary>In the low-spec mode, draws a stretched SubViewport's 3D at the window's pixels (times Scale3D), not at the
    /// reference size its container gives it (a 1920 x 1080 map in a 1280 x 720 window shades 2.25 times the pixels shown);
    /// call it each frame, it changes the viewport only when the window or the container did. Nothing when off.</summary>
    public static void FitToWindow(SubViewport vp)
    {
        if (!On || DisplayServer.GetName() == "headless" || vp.Size.X <= 0) return;
        float want = Scale3D * Mathf.Min(1f, (float)DisplayServer.WindowGetSize().X / vp.Size.X);
        if (Mathf.Abs(vp.Scaling3DScale - want) < 0.01f && vp.Scaling3DMode == Viewport.Scaling3DModeEnum.Bilinear) return;
        vp.Scaling3DMode = Viewport.Scaling3DModeEnum.Bilinear;
        vp.Scaling3DScale = want;
    }

    private static float ReadScale()
    {
        foreach (string a in OS.GetCmdlineUserArgs())
            if (a.StartsWith("--lowspec-scale=") && float.TryParse(a["--lowspec-scale=".Length..], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s) && s is >= 0.25f and <= 1f)
                return s;
        return 1f;
    }
}
