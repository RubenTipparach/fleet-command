// ShipPanel.Tractor: the ship panel's Tractor page (the battle console mockup's shTractor; design 4a): Hold and Break,
// the power-up on the Hold key, then the contest whichever end of the beam the ship is at (the grip, green when ours and
// red when theirs, the round counting down and the last round's picks), the tether with Reel in and Let out for the
// holder, the target's range against the reach, and the pick itself: the tractor's row of energy boxes, the same row
// the power station draws. Every figure is Battle.Tractor's own state and BeamValue (CLAUDE.md 6.1).
using System;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class ShipPanel
{
    partial void TractorPage(Rect2 b, ref bool drawn)
    {
        drawn = true;
        Ship s = Me;
        Ship? t = B.TargetOf(s);
        Ship? heldBy = B.HeldBy(s);
        var contest = B.Contest(s);
        var td = B.Data.Systems.Tractor;
        float x = b.Position.X, y = b.Position.Y, w = b.Size.X, hw = (w - 8) / 2;
        bool off = s.Tractor == TractorMode.Off;
        string hold = s.Tractor == TractorMode.Power ? $"Powering {Rules.FormatCooldown(s.PowerT)} s" : "Hold";
        Button("trc.hold", new Rect2(x, y, hw, 44), hold, B.TractorHold, !off, off && (s.Power["tractor"] <= 0 || heldBy != null),
            tip: new Tip("Hold", new (string, string, Color?)[] { ("Powers up", $"{td.PowerupS:0} s", null), ("Reach", $"{td.ReachKm:0.0} km", null) }));
        Button("trc.break", new Rect2(x + hw + 8, y, hw, 44), "Break", B.TractorBreak, false, off, tip: new Tip("Let go of the beam"));
        y += 52;
        if (contest is { } c)
        {
            TractorBeam bm = c.Beam;
            string grip = Pct(bm.Grip);
            y += Bar(x, y, w, c.Holding ? $"Grip on {c.Other.Name} · {grip}" : $"Held by {c.Other.Name} · {grip}", bm.Grip, !c.Holding || bm.Grip < 0.25, c.Holding) + 8;
            if (c.Holding)
            {
                float w3 = (w - 12) / 3;
                string tether = $"Tether {bm.LenM / 1000:0.0} km" + (Math.Abs(bm.WantM - bm.LenM) > 5 ? $" to {bm.WantM / 1000:0.0}" : "");
                Lbl(tether, x, y + 26, w3 + 4);
                Button("trc.in", new Rect2(x + w3 + 6, y, w3, 44), "Reel in", () => B.Reel(-1), tip: new Tip("Shorten the tether"));
                Button("trc.out", new Rect2(x + 2 * (w3 + 6), y, w3, 44), "Let out", () => B.Reel(1), tip: new Tip("Lengthen the tether"));
                y += 50;
            }
            TractorRound? last = bm.Last;
            double ours = last == null ? 0 : c.Holding ? last.Holder : last.Held, theirs = last == null ? 0 : c.Holding ? last.Held : last.Holder;
            double gain = last == null ? 0 : c.Holding ? last.Moved : -last.Moved;
            Pairs("trc.r", x, y + 16, new (string, string, Color?)[] { ($"Round {bm.Round} ends in", $"{Rules.FormatCooldown(Math.Max(0.05, bm.RoundT))} s", null) }, 20);
            Pairs("trc.last", x, y + 38, new (string, string, Color?)[]
            {
                ("Last", last == null ? "--" : $"{ours:0.0} vs {theirs:0.0} E · {(gain > 0 ? "+" : "")}{Math.Round(gain * 100)}%", last == null ? null : gain > 0 ? C.Accord : gain < 0 ? C.Alert : null),
            }, 20);
        }
        else if (s.Power["tractor"] <= 0) Lbl("No tractor power", x, y + 16, w);
        else if (t != null && s.RelockOn == t.Id)
            Pairs("trc.l", x, y + 16, new (string, string, Color?)[] { ($"{t.Name} broke free", "", null), ("Beam again in", $"{Rules.FormatCooldown(s.RelockT)} s", C.Caution) }, 20);
        else if (t == null) Lbl("No target", x, y + 16, w);
        else
        {
            double d = Rules.DistKm(s, t);
            bool inReach = d <= td.ReachKm;
            Pairs("trc.d", x, y + 16, new (string, string, Color?)[] { (t.Name, $"{d:0.0} km", inReach ? C.Accord : C.Alert), (inReach ? "in reach" : $"reach {td.ReachKm:0.0} km", "", null) }, 20);
        }
        // the pick for the round, to hold or to resist: the energy in the tractor, the power station's own row
        PowerRow("trc.pw", x, b.End.Y - 30, w, 26, "tractor", 82);
    }
}
