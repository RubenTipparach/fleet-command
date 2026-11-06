// ShipPanel.Craft: the ship panel's Craft page (the battle console mockup's shCraft): the craft aboard and flying, the
// fighters' mission (Attack, Defend, Defences), Launch and Recall, the missile barrage and the disguised shuttle. Below
// them the mines, which are on the Transport station's Mines tab (StationPanel.TransportTabs), where the mockup has them.
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class ShipPanel
{
    private static readonly (string Id, string Label)[] Missions = { ("attack", "Attack"), ("defend", "Defend"), ("defences", "Defences") };

    partial void CraftPage(Rect2 b, ref bool drawn)
    {
        drawn = true;
        Ship s = Me;
        Ship? t = B.TargetOf(s);
        float x = b.Position.X, y = b.Position.Y, w = b.Size.X, hw = (w - 6) / 2;
        int flying = B.FightersOut(s);
        Pairs("cr.1", x, y + 16, new (string, string, Color?)[]
        {
            ("In bay", s.Fighters.ToString(), null), ("Flying", flying.ToString(), null), ("Missiles", s.Missiles.ToString(), null), ("Shuttles", s.Shuttles.ToString(), null),
        }, 18);
        y += 26;
        Tabs("cr.m", new Rect2(x, y, w, 36), Missions, s.Mission, B.SetMission);
        y += 42;
        Button("cr.l", new Rect2(x, y, hw, 44), "Launch", B.LaunchFighters, false, s.Fighters <= 0 || !s.Alive, tip: new Tip("Launch every fighter in the bay"));
        Button("cr.r", new Rect2(x + hw + 6, y, hw, 44), "Recall", B.RecallFighters, false, flying == 0, tip: new Tip("Bring the fighters home"));
        y += 48;
        Button("cr.b", new Rect2(x, y, hw, 44), s.Barrage > 0 ? $"Barrage · {s.Barrage}" : "Barrage", B.FireBarrage, s.Barrage > 0, t == null || s.Missiles <= 0,
            tip: new Tip("Missile barrage", new (string, string, Color?)[] { ("Salvo", B.Data.Systems.Barrage.Salvo.ToString(), null) }));
        Button("cr.s", new Rect2(x + hw + 6, y, hw, 44), "Shuttle", B.LaunchShuttle, false, t == null || s.Shuttles <= 0, tip: new Tip("A disguised shuttle at the target"));
        y += 48;
    }
}
