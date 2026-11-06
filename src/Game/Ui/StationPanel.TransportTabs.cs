// StationPanel.TransportTabs: the Transport station's Mines and Security tabs (the battle console mockup's transport
// station, "mines on their own tab"), each the one component its system draws: the mine keys calling Battle.Mines' orders
// (Place mine by transporter, Drop astern, the count, the reach and the cool-down), and the Security view
// (ConsolePanel.Security).
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel
{
    partial void TransportSecurityTab(Rect2 b, ref bool drawn)
    {
        drawn = true;
        SecurityPanel(b);
    }

    partial void TransportMinesTab(Rect2 b, ref bool drawn)
    {
        drawn = true;
        Ship s = Me;
        float x = b.Position.X, y = b.Position.Y + 6, w = b.Size.X, hw = (w - 6) / 2;
        bool placing = X.State.PlacingMine;
        bool ready = B.MineReady(s);
        Button("mn.place", new Rect2(x, y, hw, 44), placing ? "Cancel" : "Place mine", () => X.State.PlacingMine = !X.State.PlacingMine, placing, !placing && (!ready || !B.CanBeamMine(s)),
            tip: new Tip("Beam a mine out: click in the view inside the dashed ring"));
        Button("mn.drop", new Rect2(x + hw + 6, y, hw, 44), s.MineCool > 0 ? $"Mine {Rules.FormatCooldown(s.MineCool)} s" : "Drop astern", B.DropMine, false, !ready, tip: new Tip("Drop a mine astern"));
        y += 50;
        var md = B.Data.Systems.Mines;
        Pairs("mn.info", x, y + 12, new (string, string, Color?)[]
        {
            ("Mines", s.Mines.ToString(), null), ("Reach", $"{md.TransporterReachKm:0.0} km", null),
            ("Cooldown", s.MineCool > 0 ? $"{Rules.FormatCooldown(s.MineCool)} s" : "ready", s.MineCool > 0 ? C.Caution : C.Accord),
        }, 18);
    }
}
