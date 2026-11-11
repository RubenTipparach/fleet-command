// GameSettings: the player's game options other than sound and the window (owner, 2026-11-09: "add a flag to toggle
// unfinished factions to off be default in options", "So we can play with the 4 present factions..this flag is override by
// any save"). Remembered in user://settings.cfg beside them. Unfinished factions (no territory on the campaign map yet) are
// off by default: the faction screen shows the factions in the game, and a new campaign holds standing only with the
// factions in its world. A campaign keeps the setting it was started with (CampaignState.Unfinished), so a save is never
// changed by it.
using Godot;

namespace FleetCommand.Game.Ui;

public static class GameSettings
{
    private const string Path = "user://settings.cfg", Section = "game", Key = "unfinished_factions";

    /// <summary>Whether new campaigns and the faction screen include the factions not yet finished. Off by default.</summary>
    public static bool UnfinishedFactions
    {
        get
        {
            var cfg = new ConfigFile();
            return cfg.Load(Path) == Error.Ok && cfg.GetValue(Section, Key, false).AsBool();
        }
        set
        {
            var cfg = new ConfigFile();
            cfg.Load(Path);
            cfg.SetValue(Section, Key, value);
            cfg.Save(Path);
        }
    }
}
