// ShipPanel.Sensors: the ship panel's Sensors page, as the battle console mockup's shSensors draws it: the sensors' health,
// their mode and the ECM and ECCM set on the Science station, the contacts (hostile ships seen, Battle.SeenFoes, and the
// fleet), and the locked target's ECM. Read-only: the Science station sets them.
using System.Linq;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class ShipPanel
{
    partial void SensorsPage(Rect2 b, ref bool drawn)
    {
        drawn = true;
        Ship s = Me;
        Ship? t = B.TargetOf(s);
        float x = b.Position.X, w = b.Size.X, y = b.Position.Y;
        double hp = s.Sys["sensors"];
        y += Bar(x, y, w, "Sensor health", hp, hp < 0.5) + 28;
        Pairs("se.1", x, y, new (string, string, Color?)[] { ("Mode", s.Passive ? "PASSIVE" : "ACTIVE", null), ("ECM", s.Ecm.ToString(), null), ("ECCM", s.Eccm.ToString(), null) });
        y += 34;
        int friendly = B.Ships.Count(o => o.Alive && o.Side == s.Side) - 1;
        Pairs("se.2", x, y, new (string, string, Color?)[] { ("Hostile", B.SeenFoes(s).Count().ToString(), null), ("Friendly", friendly.ToString(), null) });
        y += 34;
        if (t != null && t.Side != s.Side) Pairs("se.3", x, y, new (string, string, Color?)[] { ("Lock", Aiming.ContactName(B, t), null), ("Their ECM", t.Ecm.ToString(), null) });
        else Lbl("No lock", x, y);
    }
}
