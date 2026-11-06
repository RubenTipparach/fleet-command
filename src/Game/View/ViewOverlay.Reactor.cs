// ViewOverlay.Reactor: what the reactor package draws over the 3D view (the battle console mockup's uiView): each gravity
// well's name with the quantum-gravitational field where you are, and the notice in the middle of the view that says why
// a jump was refused, made or failed (Battle.Notice), for notice_s seconds.
using System;
using System.Collections.Generic;
using FleetCommand.Data;
using FleetCommand.Game.Ui;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class ViewOverlay
{
    /// <summary>How long a notice stays in the middle of the view, seconds of battle time.</summary>
    private const double NoticeS = 6;

    partial void DrawReactor(Fonts f)
    {
        Battle b = B;
        Ship me = b.Player;
        if (b.Scenario.Wells is { } wells)
            foreach (WellDef w in wells)
                if (Local(w.XM, w.ZM) is { } p)
                    f.Draw(this, $"{w.Name} · Q-G field here {Battle.Pct(b.QgAt(me.X, me.Z))}".ToUpperInvariant(), p.X, p.Y - 30, 16, C.Caution, HorizontalAlignment.Center, 0, 600, 1);
        if (b.Notice is not { } n || b.T - n.T >= NoticeS || _x.State.Expanded != null) return;
        Color col = n.Tone switch { "accord" => C.Accord, "alert" => C.Alert, _ => C.Caution };
        float nw = Math.Min(620, Size.X - 60), nx = (Size.X - nw) / 2, ny = 120;
        // the reason, wrapped to the notice's width
        var lines = new List<string>();
        string cur = "";
        foreach (string wd in n.Text.ToUpperInvariant().Split(' '))
        {
            string tr = cur.Length > 0 ? cur + " " + wd : wd;
            if (f.Width(tr, 20, 600, 1) > nw - 40 && cur.Length > 0) { lines.Add(cur); cur = wd; } else cur = tr;
        }
        if (cur.Length > 0) lines.Add(cur);
        float nh = 60 + lines.Count * 26;
        _x.Kit.Shape(this, new Rect2(nx, ny, nw, nh), 12, new Color(0, 0, 0, 0.88f));
        _x.Kit.Shape(this, new Rect2(nx, ny, nw, nh), 12, col, 3);
        f.Draw(this, n.Title.ToUpperInvariant(), nx + nw / 2, ny + 38, 30, col, HorizontalAlignment.Center, nw - 40, 700, 2);
        for (int i = 0; i < lines.Count; i++) f.Draw(this, lines[i], nx + nw / 2, ny + 68 + i * 26, 20, C.Text, HorizontalAlignment.Center, nw - 40, 600, 1);
    }
}
