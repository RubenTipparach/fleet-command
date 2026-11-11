// ViewOverlay.Tactical: the two sheets weapon targeting and the event log lay over the view (the battle console mockup's
// hitSheet and its event log). The hit details (owner: "recommend shift + click to see weapon hit stats in details"): the
// target's facing toward you and what it exposes, the rule in a line, then each weapon (or the one clicked) with how it
// aims, its arc, range, chance to hit, chance to put the hit on the aim, and its pin. The event log (owner, 2026-11-02: "I
// need an event log button in comms somewhere"; 2026-11-03: "expanded event log should have a little X to close the
// panel"): the battle's log, newest first. A click outside either sheet, or its X, shuts it. Every figure is the
// simulation's (Rules, Strike, Battle.DirectHitChance).
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Game.Ui;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class ViewOverlay
{
    partial void DrawTacticalSheets(Fonts f)
    {
        if (_x.State.HitInfo is { } hi) HitSheet(f, hi);
        else if (_x.State.LogOpen) EventLog(f);
    }

    // the sheet's box: a near-black card edged in Sky, its title, and the X that shuts it; returns the box
    private Rect2 Sheet(Fonts f, string id, float h, string title, Action close)
    {
        _ui.Reg($"v.{id}.off", new Rect2(Vector2.Zero, Size), _ => close());
        var r = new Rect2(14, 50, Math.Min(1040, Size.X - 28), h);
        _ui.Reg($"v.{id}.box", r);
        Shapes.RoundRect(this, r, new Color(0, 0, 0, 0.9f), 10);
        Shapes.RoundRectLine(this, r, C.Sky, 10, 2);
        f.Draw(this, title.ToUpperInvariant(), r.Position.X + 16, r.Position.Y + 26, 16, C.Sky, HorizontalAlignment.Left, r.Size.X - 60, 600, 1);
        var xr = new Rect2(r.End.X - 34, r.Position.Y + 8, 26, 26);
        HitState xs = _ui.Reg($"v.{id}.x", xr, _ => close(), null, new Tip("Close"));
        KeyFace.Close(_x.Kit, this, xr, xs.Hot);
        return r;
    }

    private void HitSheet(Fonts f, HitInfo hi)
    {
        Battle b = B;
        Ship s = b.Player;
        Ship? t = b.TargetOf(s) is { } lt && lt.Side != s.Side ? lt : null;
        string aim = hi.Aim ?? s.Aim;
        List<Mount> list = hi.Mount is int mi ? s.Mounts.Where(m => m.Index == mi).ToList() : s.Mounts.Where(m => !m.Weapon.IsPointDefence).ToList();
        Rect2 r = Sheet(f, "hi", 150 + list.Count * 30, $"Hit details · aimed at {Strike.Name(aim, t)} · {(t != null ? Aiming.ContactName(B, t) : "no target")}", () => _x.State.HitInfo = null);
        float lx = r.Position.X + 16, lw = r.Size.X - 32, y = r.Position.Y + 54;
        Exposure? ex = t != null ? b.ExposureFrom(t, s) : null;
        if (t != null && ex != null)
            f.DrawRich(this, new[] { ($"{Aiming.ContactName(B, t)}'s {Rules.Facings[Rules.FacingFrom(t, s.X, s.Z)]} facing is toward you. On the chopping block: ", C.Lilac),
                ($"{string.Join(", ", ex.Sys.Select(k => Strike.Name(k, t)))}; {ex.Mounts.Count} weapon mount{(ex.Mounts.Count == 1 ? "" : "s")}", C.Text) }, lx, y, 17, lw);
        else f.Draw(this, "LOCK A TARGET TO SEE WHERE YOUR HITS CAN LAND", lx, y, 15, C.Lilac, HorizontalAlignment.Left, lw, 600, 1);
        y += 26;
        f.DrawRich(this, new[] { ($"Precise weapons put a hit on the aimed system {Math.Round(b.Rules.Tactical.Strike.DirectChance * 100)} % of the time when it is on the block; the rest, and every by-arc weapon, lands anywhere on it.", C.Lilac) }, lx, y, 15, lw);
        y += 34;
        (string H, float At)[] cols = { ("Weapon", 0), ("Aims", 0.26f), ("In arc", 0.38f), ("Range", 0.48f), ("To hit", 0.6f), ($"On {Strike.Name(aim, t)}", 0.72f), ("Pinned", 0.86f) };
        foreach (var (h, at) in cols) f.Draw(this, h.ToUpperInvariant(), lx + lw * at, y, 13, C.Lilac, HorizontalAlignment.Left, lw * 0.13f, 600, 1);
        y += 26;
        foreach (Mount m in list)
        {
            Ship? pt = b.PinTarget(m), tt = pt ?? t;
            string a2 = pt != null ? m.Pin!.Aim : aim;
            bool arc = tt != null && Rules.InArc(s, m, tt), ok = arc && tt != null && b.Rules.InRange(s, m, tt);
            double hc = ok ? b.Rules.HitChance(s, m, tt, a2) : 0;
            string on = tt == null || !ok || a2 == "hull" ? "--" : !m.Weapon.Precise ? "By arc"
                : !Strike.Exposed(b.ExposureFrom(tt, s), a2) ? "Not exposed" : $"{Math.Round(b.DirectHitChance(s, m, tt, a2) * 100)} %";
            string[] cells =
            {
                $"{m.Label} {m.Weapon.Name}{(m.State != Arming.Normal ? " · " + m.State : "")}", Rules.AimsBy(m), arc ? "Yes" : "No",
                tt != null ? $"{Rules.DistKm(s, tt):0.0}/{b.Rules.RangeKm(s, m, tt):0.#} km" : "--", ok ? $"{Math.Round(hc * 100)} %" : "--", on, pt?.Name ?? "--",
            };
            for (int i = 0; i < cells.Length; i++)
            {
                bool lit = i == 5 && on is not ("--" or "By arc" or "Not exposed");
                float max = i + 1 < cols.Length ? lw * (cols[i + 1].At - cols[i].At) - 8 : lw * (1 - cols[i].At);
                f.Draw(this, cells[i], lx + lw * cols[i].At, y, 17, lit ? C.Sky : C.Text, HorizontalAlignment.Left, max, 600);
            }
            y += 30;
        }
        if (t != null && ex != null && aim != "hull" && !Strike.Exposed(ex, aim))
            f.Draw(this, $"{Strike.Name(aim, t)} is not on the block from here: manoeuvre to another facing".ToUpperInvariant(), lx, r.End.Y - 14, 13, C.Caution, HorizontalAlignment.Left, lw, 600, 1);
    }

    private void EventLog(Fonts f)
    {
        IReadOnlyList<LogLine> log = B.Log;
        int rows = Math.Max(4, (int)Math.Floor((Size.Y - 180) / 26));
        LogLine[] list = log.Skip(Math.Max(0, log.Count - rows)).Reverse().ToArray();
        Rect2 r = Sheet(f, "log", 44 + Math.Max(1, list.Length) * 26, $"Event log · {log.Count} message{(log.Count == 1 ? "" : "s")}", () => _x.State.LogOpen = false);
        float lx = r.Position.X + 16, lw = r.Size.X - 32;
        if (list.Length == 0) f.Draw(this, "NOTHING YET", lx, r.Position.Y + 56, 15, C.Lilac, HorizontalAlignment.Left, lw, 600, 1);
        for (int i = 0; i < list.Length; i++)
        {
            LogLine q = list[i];
            int sec = (int)q.T;
            bool warn = Battle.IsWarning(q);   // a refusal or a warning in the caution colour (the mockup's red lines)
            f.DrawRich(this, new[] { ($"{sec / 60:00}:{sec % 60:00}  {q.Speaker}: ", warn ? C.Caution : C.Lilac), (q.Text, warn ? C.Caution : C.Text) }, lx, r.Position.Y + 56 + i * 26, 18, lw);
        }
    }
}
