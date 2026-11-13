// DisplaySettings: the window's mode, the player's to choose (owner, 2026-11-04: "need a fullscreen /window option since
// right now its not prioritizing the borderless fullscreen"). Borderless fullscreen by default, a plain window on request
// (the menu's Display key, F11 or Alt+Enter), remembered between runs in user://settings.cfg. A scripted run (a playtest,
// a still, the layout test) keeps the window it was started in, so a recording or a test is never resized under it.
using Godot;

namespace FleetCommand.Game.Ui;

public static class DisplaySettings
{
    private const string Path = "user://settings.cfg", Section = "display", Key = "fullscreen";

    /// <summary>Whether the game is in borderless fullscreen now.</summary>
    public static bool Fullscreen => DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;

    /// <summary>Puts the window in the mode the player last chose (borderless fullscreen the first time; in the low-spec
    /// mode a 1280 x 720 window the first time, LowSpec.DefaultWindow).</summary>
    public static void ApplySaved()
    {
        var cfg = new ConfigFile();
        bool chosen = cfg.Load(Path) == Error.Ok && cfg.HasSectionKey(Section, Key);
        bool full = chosen ? cfg.GetValue(Section, Key, true).AsBool() : !LowSpec.On;
        Set(full);
        if (!full && !chosen && LowSpec.On)
        {
            DisplayServer.WindowSetSize(LowSpec.DefaultWindow);
            Vector2I screen = DisplayServer.ScreenGetSize();
            DisplayServer.WindowSetPosition((screen - LowSpec.DefaultWindow) / 2);
        }
    }

    /// <summary>Switches between borderless fullscreen and a window, and remembers the choice.</summary>
    public static void Toggle()
    {
        bool full = !Fullscreen;
        Set(full);
        var cfg = new ConfigFile();
        cfg.Load(Path);
        cfg.SetValue(Section, Key, full);
        cfg.Save(Path);
    }

    private static void Set(bool full) =>
        DisplayServer.WindowSetMode(full ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
}
