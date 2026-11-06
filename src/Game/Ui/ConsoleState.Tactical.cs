// ConsoleState.Tactical: the console's own state for weapon targeting, the fleet, the tactical map and the event log (the
// battle console mockup's hitInfo, fleetPick, mapScale and logOpen), in its own part of ConsoleState so the other system
// packages never edit these lines. It is what the console shows, never a rule of the battle.
namespace FleetCommand.Game.Ui;

/// <summary>What the hit details sheet is open for: an aim (every weapon against it), or one of your mounts.</summary>
public sealed record HitInfo(string? Aim, int? Mount);

public sealed partial class ConsoleState
{
    /// <summary>The hit details sheet over the view (shift+click an aim or a weapon), or null when it is shut.</summary>
    public HitInfo? HitInfo;
    /// <summary>The event log over the view (owner, 2026-11-03: "expanded event log should have a little X to close the panel").</summary>
    public bool LogOpen;
    /// <summary>The ship of your fleet the Fleet station shows; null is the ship you command.</summary>
    public string? FleetPick;
    /// <summary>The tactical map's range, km; 0 is the middle of tactical.json map_scales_km.</summary>
    public double MapScaleKm;

    /// <summary>Opens the event log over the view, shutting the scene list and the hit details and returning an expanded
    /// panel so the log has the view (the mockup's Event log key, which the Comms station draws).</summary>
    public void OpenLog(GameActions act)
    {
        LogOpen = true;
        SceneOpen = false;
        HitInfo = null;
        act.Return();
    }

    /// <summary>Opens the hit details for an aim or a mount, shutting the log and the scene list.</summary>
    public void OpenHitInfo(HitInfo info)
    {
        HitInfo = info;
        LogOpen = false;
        SceneOpen = false;
    }
}
