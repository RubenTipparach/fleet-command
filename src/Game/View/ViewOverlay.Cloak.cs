// ViewOverlay.Cloak: the cloak package over the view: each sweep as a ring spreading on the battle plane from the sweeper
// to its reach over the cloak's sweep_fx_s (the battle console mockup's sweepsStep), in Sky for your side's and Alert for
// the other's. A cloaked ship unseen is simply not marked (DrawView reads Battle.Detected); the cloak's fade itself is the
// 3D view's (BattleView.Cloak.cs).
using System;
using FleetCommand.Game.Ui;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class ViewOverlay
{
    partial void DrawCloak(Fonts f)
    {
        Battle b = B;
        double fx = b.Data.Systems.Cloak.Cloak.SweepFxS;
        foreach (SweepRing w in b.Sweeps)
        {
            double k = (b.T - w.T) / fx;
            if (k < 0 || k >= 1) continue;
            Color col = w.Side == b.Player.Side ? C.Sky : C.Alert;
            var pts = new System.Collections.Generic.List<Vector2>();
            double r = Math.Max(1, w.RM * k);
            for (int i = 0; i <= 96; i++)
            {
                double a = i * Math.PI * 2 / 96;
                if (Local(w.XM + Math.Sin(a) * r, w.ZM - Math.Cos(a) * r) is { } p) pts.Add(p);
                else if (pts.Count >= 2) { Draw2D.Polyline(this, pts.ToArray(), new Color(col, (float)(1 - k)), 3, true); pts.Clear(); }
                else pts.Clear();
            }
            if (pts.Count >= 2) Draw2D.Polyline(this, pts.ToArray(), new Color(col, (float)(1 - k)), 3, true);
        }
    }
}
