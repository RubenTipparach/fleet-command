// StationPanel.Comms: the Comms station (the battle console mockup's stComms): Hail target, the fleet orders, the Event log
// key that opens the whole log over the view (owner, 2026-11-02: "I need an event log button in comms somewhere"), and the
// latest lines listed under it, more of them expanded. Refusals are in Caution (Battle.IsWarning). Drawn only with the
// kit, so every faction's skin draws it.
using System;
using System.Linq;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel
{
    partial void CommsPage(Rect2 b, ref bool drawn)
    {
        drawn = true;
        Ship s = Me;
        float x = b.Position.X, y = b.Position.Y, w = b.Size.X;
        Ship? t = B.TargetOf(s);
        Button("com.hail", new Rect2(x, y, w, 48), "Hail target", B.Hail, dis: t == null, center: false,
            tip: new Tip(t == null ? "Lock a ship to hail it" : $"Open a channel to {t.Name}"));
        y += 56;
        Lbl("Fleet orders", x, y + 13);
        y += 20;
        float hw = (w - 6) / 2;
        (string Label, string Id, Stance? St)[] orders = { ("All attack", "attack", Stance.Attack), ("Form on me", "escort", Stance.Escort), ("All hold", "hold", Stance.Hold), ("Hunt", "hunt", null) };
        bool wing = B.Ships.Any(o => o.Alive && o.Side == s.Side && o != s);
        for (int i = 0; i < orders.Length; i++)
        {
            var (label, id, st) = orders[i];
            // the Hunt stance (the weakest enemy first) comes with the Fleet station
            Button($"com.{id}", new Rect2(x + (i % 2) * (hw + 6), y + i / 2 * 36, hw, 30), label, st is { } v ? () => B.OrderFleet(v) : null, dis: st == null || !wing,
                tip: new Tip(st == null ? "Hunt: not yet in the game" : wing ? $"Every wingman: {label.ToLowerInvariant()}" : "No wingmen"));
        }
        y += 76;
        Button("com.log", new Rect2(x, y, w, 36), "Event log", OpenLog, X.State.LogOpen, tip: new Tip("The whole log, over the view"));
        y += 44;
        Lbl("Latest", x, y + 13);
        y += 22;
        int n = Big ? B.Comms.LogLinesExpanded : B.Comms.LogLinesDocked;
        foreach (LogLine l in B.Log.Reverse().Take(n))
        {
            if (y + 24 > b.End.Y) break;
            bool warn = Battle.IsWarning(l);
            X.Fonts.DrawRich(this, new[] { ($"{Clock(l.T)} {l.Who}: ", warn ? C.Caution : C.Lilac), (l.Text, warn ? C.Caution : C.Text) }, x, y + 16, 17, w);
            y += 24;
        }
    }

    // the log opens over the view, so an expanded panel returns and the scene list shuts
    private void OpenLog()
    {
        if (X.State.LogOpen) X.State.LogOpen = false;
        else X.State.OpenLog(X.Act);
    }

    /// <summary>Battle time as minutes and seconds (the log's clock).</summary>
    public static string Clock(double t) => $"{(int)(t / 60):00}:{(int)(t % 60):00}";
}
