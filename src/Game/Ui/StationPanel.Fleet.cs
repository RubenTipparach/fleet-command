// StationPanel.Fleet: the Fleet station (survey G3: "with ai wingmen, and behaviors configuratble for AI"; the battle console
// mockup's stFleet): a tab for each ship of your fleet (a lost one dimmed), the picked ship's hull, and for a wingman its
// standing orders: docked, its stance as keys and the rest read out, expanded, every order as a row of keys (stance, target,
// range, retreat, fire); and Take command. The orders are Battle.Fleet.cs; the ship you command takes none.
using System;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel
{
    private static readonly (string Id, string Label)[] FleetStances = { ("attack", "Attack"), ("escort", "Escort"), ("hold", "Hold"), ("hunt", "Hunt") };
    private static readonly (string Id, string Label)[] FleetRules = { ("mine", "My target"), ("nearest", "Nearest"), ("weakest", "Weakest") };
    private static readonly (string Id, string Label)[] FleetRanges = { ("close", "Close"), ("mid", "Mid"), ("long", "Long") };
    private static readonly (string Id, string Label)[] FleetRetreats = { ("never", "Never"), ("half", "50%"), ("quarter", "25%") };
    private static readonly (string Id, string Label)[] FleetFire = { ("free", "Free"), ("hold", "Hold") };

    private static string Lower<T>(T v) where T : Enum => v.ToString().ToLowerInvariant();
    private static string Named((string Id, string Label)[] list, string id) => Array.Find(list, c => c.Id == id).Label ?? id;

    partial void FleetPage(Rect2 b, ref bool drawn)
    {
        drawn = true;
        float x = b.Position.X, y = b.Position.Y;
        Ship[] own = B.FleetAll();
        Ship f = (X.State.FleetPick is { } pick ? B.ShipById(pick) : null) is { } p && p.Side == Me.Side ? p : Me;
        Tabs("fl", new Rect2(x, y, b.Size.X, 40), own.Select(o => (o.Id, o.Name)).ToArray(), f.Id, id => X.State.FleetPick = id, id => B.ShipById(id) is not { Alive: true });
        y += 46;
        double hull = f.Hull / f.Class.HullHp;
        y += Bar(x, y, b.Size.X, $"{f.Name} · {f.Class.Name} · hull", hull, hull < 0.35) + 12;
        if (f == Me)
        {
            Text("UNDER YOUR COMMAND", x, y + 18, 20, C.Text, HorizontalAlignment.Left, b.Size.X, 600, 1);
            Lbl("Choose a wingman above to set its orders", x, y + 42, b.Size.X);
            return;
        }
        string id0 = f.Id;
        string fire = f.HoldFire ? "hold" : "free";
        if (!Big)
        {
            Lbl("Stance", x, y + 13);
            y += 20;
            Tabs("fl.st", new Rect2(x, y, b.Size.X, 40), FleetStances, Lower(f.Stance), v => B.SetStance(id0, Enum.Parse<Stance>(v, true)));
            y += 64;
            Pairs("fl.s1", x, y, new (string, string, Color?)[] { ("Target", Named(FleetRules, Lower(f.Rule)), null), ("Range", Named(FleetRanges, Lower(f.Range)), null) }, 18);
            Pairs("fl.s2", x, y + 26, new (string, string, Color?)[] { ("Retreat", Named(FleetRetreats, Lower(f.Retreat)), null), ("Fire", Named(FleetFire, fire), null) }, 18);
            Button("fl.cmd", new Rect2(x, b.End.Y - 48, b.Size.X, 48), "Take command", () => B.TakeCommand(id0), dis: !f.Alive);
            return;
        }
        void Row(string key, string label, (string Id, string Label)[] list, string cur, Action<string> set)
        {
            Lbl(label, x, y + 26);
            Tabs($"fl.{key}", new Rect2(x + 110, y, b.Size.X - 110, 40), list, cur, set);
            y += 46;
        }
        Row("stance", "Stance", FleetStances, Lower(f.Stance), v => B.SetStance(id0, Enum.Parse<Stance>(v, true)));
        Row("rule", "Target", FleetRules, Lower(f.Rule), v => B.SetRule(id0, Enum.Parse<TargetRule>(v, true)));
        Row("range", "Range", FleetRanges, Lower(f.Range), v => B.SetRange(id0, Enum.Parse<AiRange>(v, true)));
        Row("retreat", "Retreat", FleetRetreats, Lower(f.Retreat), v => B.SetRetreat(id0, Enum.Parse<RetreatAt>(v, true)));
        Row("fire", "Fire", FleetFire, fire, v => B.SetHoldFire(id0, v == "hold"));
        Button("fl.cmd", new Rect2(x, y + 10, b.Size.X, 48), "Take command", () => B.TakeCommand(id0), dis: !f.Alive);
    }
}
