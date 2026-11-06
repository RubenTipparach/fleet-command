// StationPanel.PointDefence: the Point defence station (the battle console mockup's stPD): its mode (Auto, Missiles, Craft,
// Off), each point defence mount's charge as a bar (red when the mount is under half health), and the shots stopped and
// still inbound. Point defence keeps its own power (owner, 2026-11-04: "weapons control system dont affect PDCs or missiles
// actually"), so its bars fill whatever weapons control's state. The rules are Battle.PointDefenceStep and Battle.SetPd.
using System;
using System.Linq;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel
{
    private static readonly (string Id, string Label)[] PdModes = { ("auto", "Auto"), ("missiles", "Missiles"), ("craft", "Craft"), ("off", "Off") };

    partial void PointDefencePage(Rect2 b, ref bool drawn)
    {
        drawn = true;
        Ship s = Me;
        float x = b.Position.X, y = b.Position.Y;
        Lbl("Mode", x, y + 13);
        y += 20;
        Tabs("pd", new Rect2(x, y, b.Size.X, 40), PdModes, s.Pd.ToString().ToLowerInvariant(), id => B.SetPd(Enum.Parse<PdMode>(id, true)));
        y += 52;
        foreach (Mount m in s.Mounts.Where(m => m.Weapon.IsPointDefence))
            y += Bar(x, y, b.Size.X, $"PD mount {m.Index + 1}", m.Charge * m.Hp, m.Hp < 0.5) + 12;
        int inbound = B.Shots.Count(sh => sh.To == s.Id && sh.Kind != "beam" && !sh.Spent);
        Pairs("pd.i", x, y + 20, new (string, string, Color?)[] { ("Intercepts", s.Intercepts.ToString(), null), ("Inbound", inbound.ToString(), inbound > 0 ? C.Caution : null) });
    }
}
