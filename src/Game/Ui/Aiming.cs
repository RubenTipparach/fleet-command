// Aiming: what every aim control on the console shares (the Aim at keys, the target's weapons as keys, the Target box's
// subsystems and mounts; openspec/changes/weapon-targeting): a click aims, shift+click opens the hit details; the bracket
// drawn over whatever the chopping block exposes; the hull's status; a contact's colour on the lists and the map. One
// implementation for each, so every place that aims behaves alike (CLAUDE.md 6.1).
using FleetCommand.Game.Ui.Kit;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public static class Aiming
{
    /// <summary>A click on an aim: shift+click opens its hit details, a plain click aims there.</summary>
    public static void Click(ConsoleContext x, Imm ui, string aim)
    {
        if (ui.Shift) x.State.OpenHitInfo(new HitInfo(aim, null));
        else x.Battle.Aim(aim);
    }

    /// <summary>The hull's status from its share left, by the same lines a subsystem's takes (Rules.Status).</summary>
    public static SystemStatus HullStatus(Battle b, Ship t)
    {
        double hp = t.Hull / t.Class.HullHp;
        return hp <= 0 ? SystemStatus.Destroyed : hp < b.Tuning.StatusDisabledBelow ? SystemStatus.Disabled : hp < 1 ? SystemStatus.Damaged : SystemStatus.Active;
    }

    /// <summary>A ship's name as the player's side knows it: its own, or "Unknown contact" while it is beyond every friendly
    /// ship's sensors and unscanned (Battle.Identified; owner, 2026-11-08: "The one big thing we need is sensor systems").
    /// Every place that names an enemy asks this, so none gives away what the sensors have not found.</summary>
    public static string ContactName(Battle b, Ship o) => b.Identified(o) ? o.Name : "Unknown contact";

    /// <summary>A ship's class as the player's side knows it ("Unknown" until identified).</summary>
    public static string ContactClass(Battle b, Ship o) => b.Identified(o) ? o.Class.Name : "Unknown";

    /// <summary>A ship's class code as the player's side knows it ("?" until identified).</summary>
    public static string ContactCode(Battle b, Ship o) => b.Identified(o) ? o.Class.Code : "?";

    /// <summary>A contact's colour: your side's ships Accord, the Vos Caution, every other enemy Alert.</summary>
    public static Color ContactColor(Palette c, Ship me, Ship o) => o.Side == me.Side ? c.Accord : o.Faction.Id == "vos" ? c.Caution : c.Alert;

    /// <summary>The chopping block's bracket over a box: a short line above it with ticks down at each end, in Alert.</summary>
    public static void Bracket(CanvasItem ci, Palette c, Rect2 r, float lift, float tick, float width)
    {
        float y = r.Position.Y - lift, x0 = r.Position.X + 2, x1 = r.End.X - 2;
        Draw2D.Polyline(ci, new[] { new Vector2(x0, y + tick), new Vector2(x0, y), new Vector2(x1, y), new Vector2(x1, y + tick) }, c.Alert, width, true);
    }
}
