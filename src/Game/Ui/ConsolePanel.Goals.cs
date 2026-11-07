// ConsolePanel.Goals: one goal of a campaign mission drawn as a line, the one implementation the goals over the view
// (MissionStrip) and the Comms station both use: a dot (Sky for a primary goal, Lilac for a bonus) or a green tick once
// met, then its words, "(bonus)" after a bonus goal's.
using Godot;

namespace FleetCommand.Game.Ui;

public abstract partial class ConsolePanel
{
    /// <summary>A goal's line at a baseline, in a width. <paramref name="shadow"/>: a dark edge under the words, for
    /// a line drawn over the battle with nothing behind it.</summary>
    protected void Goal(GoalLine g, float x, float y, float w, bool shadow)
    {
        Color ink = g.Met ? C.Accord : g.Primary ? C.Text : C.Lilac;
        var c = new Vector2(x + 6, y - 6);
        if (g.Met)
        {
            DrawLine(c + new Vector2(-4, 0), c + new Vector2(-1, 3), C.Accord, 2.5f, true);
            DrawLine(c + new Vector2(-1, 3), c + new Vector2(5, -4), C.Accord, 2.5f, true);
        }
        else DrawCircle(c, 4, g.Primary ? C.Sky : C.Lilac);
        string t = g.Text + (g.Primary ? "" : " (bonus)");
        if (shadow) Text(t, x + 19, y + 1, 17, new Color(0, 0, 0, 0.8f), HorizontalAlignment.Left, w - 18, 600);
        Text(t, x + 18, y, 17, ink, HorizontalAlignment.Left, w - 18, 600);
    }
}
