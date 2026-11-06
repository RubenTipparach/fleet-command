// ShipPanel.Craft: the ship panel's Craft page (the battle console mockup's shCraft): the craft aboard and flying, the
// fighters' mission (Attack, Defend, Defences), Launch and Recall, the missile barrage and the disguised shuttle. Below
// them the mines: the mockup keeps those on the Transport station's Mines tab, which belongs to the transporter's own
// package, so until that station is in the game the same keys sit here (Place mine by transporter, Drop astern, the
// count, the reach and the cool-down), calling the same orders (Battle.Mines).
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
        bool placing = X.State.PlacingMine;
        bool ready = B.MineReady(s);
        Button("cr.place", new Rect2(x, y, hw, 44), placing ? "Cancel" : "Place mine", () => X.State.PlacingMine = !X.State.PlacingMine, placing, !placing && (!ready || !B.CanBeamMine(s)),
            tip: new Tip("Beam a mine out: click in the view inside the dashed ring"));
        Button("cr.drop", new Rect2(x + hw + 6, y, hw, 44), s.MineCool > 0 ? $"Mine {Rules.FormatCooldown(s.MineCool)} s" : "Drop astern", B.DropMine, false, !ready, tip: new Tip("Drop a mine astern"));
        y += 50;
        var md = B.Data.Systems.Mines;
        Pairs("cr.mn", x, y + 12, new (string, string, Color?)[]
        {
            ("Mines", s.Mines.ToString(), null), ("Reach", $"{md.TransporterReachKm:0.0} km", null),
            ("Cooldown", s.MineCool > 0 ? $"{Rules.FormatCooldown(s.MineCool)} s" : "ready", s.MineCool > 0 ? C.Caution : C.Accord),
        }, 18);
    }
}
