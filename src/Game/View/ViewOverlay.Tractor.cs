// ViewOverlay.Tractor: what the tractor-and-craft package draws over the 3D view, and its click (the battle console
// mockup's bullseye, helmArc's mine rings and reach ring, and its shuttle marks). Owner, 2026-11-02: "need to draw radius
// of mine when mouse hovers over the battle field, mine placement will be yellow, but turns red when it is in radius of
// any ship. It will grey if unable to be placed, and the letters 'invalid position' will show ... the mine range display
// is like an animated bullseye radiating outwards"; "mine needs range animation still here"; "mine range is different
// than mine transport range". So: every mine your side has laid keeps its bullseye (grey while arming, yellow armed, red
// with a ship inside it); while Place mine is armed the transporter's reach is a dashed ring and the bullseye follows
// the pointer, and a click beams the mine there (Battle.PlaceMine, which checks the same reach); your own disguised
// shuttles are marked for what they are.
using System;
using System.Collections.Generic;
using FleetCommand.Game.Ui;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class ViewOverlay
{
    private static readonly Color Grey = new("#8a8f9c");

    /// <summary>A point on the battle plane (m) on the screen, in the viewport's coordinates (the scripted captain clicks there).</summary>
    public Vector2? ScreenOfPlane(double xm, double zm) => _view.ScreenOf(xm, zm);

    partial void DrawTractor(Fonts f)
    {
        Ship me = B.Player;
        double trig = B.Data.Systems.Mines.TriggerM;
        foreach (Mine mn in B.Mines)
        {
            if (mn.Side != me.Side) continue;
            Bullseye(mn.X, mn.Z, mn.ArmS > 0 ? Grey : B.ShipInMineRange(mn.X, mn.Z) ? C.Alert : C.Caution, 0.8f);
        }
        foreach (Craft c in B.Craft)
        {
            if (c.Kind != "shuttle" || c.Side != me.Side || Local(c.X, c.Z) is not { } p) continue;
            Draw2D.Arc(this, p, 9, 0, Mathf.Tau, 24, C.Caution, 2, true);
            f.Draw(this, "SHUTTLE · DISGUISED", p.X, p.Y - 16, 14, C.Caution, HorizontalAlignment.Center);
        }
        if (!_x.State.PlacingMine || !me.Alive) return;
        double reachM = B.Data.Systems.Mines.TransporterReachKm * 1000;
        DashedRing(me.X, me.Z, reachM, C.Caution);
        if (Local(me.X, me.Z - reachM) is { } top)
            f.Draw(this, "TRANSPORT RANGE", top.X, top.Y - 8, 15, C.Caution, HorizontalAlignment.Center, 0, 700, 2);
        f.Draw(this, $"PLACE A MINE: TRANSPORTER REACH {reachM / 1000:0.0} KM (DASHED) · MINE RANGE {trig / 1000:0.0} KM (BULLSEYE)", Size.X / 2, 92, 18, C.Caution, HorizontalAlignment.Center, Size.X - 40);
        if (_mouse.X < 0 || _view.PlaneAt(_mouse + Position) is not { } q) return;
        bool valid = B.InMineReach(me, q.X, q.Z), hot = B.ShipInMineRange(q.X, q.Z);
        Color col = !valid ? Grey : hot ? C.Alert : C.Caution;
        Bullseye(q.X, q.Z, col, 1);
        if (Local(q.X, q.Z) is { } c0)
            f.Draw(this, (!valid ? "Invalid position" : hot ? "Ship in mine range" : $"Mine range {trig / 1000:0.0} km").ToUpperInvariant(), c0.X, c0.Y - 16, 15, col, HorizontalAlignment.Center);
    }

    // A click while Place mine is armed beams a mine to the point clicked (the order checks the reach and says so).
    private bool MineClick(Vector2 local)
    {
        if (!_x.State.PlacingMine) return false;
        _x.State.PlacingMine = false;
        if (_view.PlaneAt(local + Position) is { } q) B.PlaceMine(q.X, q.Z);
        return true;
    }

    // A mine's range on the plane as a bullseye: its edge, and three rings radiating out from the centre to it. One drawing
    // for the placement preview and the mines already laid.
    private void Bullseye(double xm, double zm, Color col, float alpha)
    {
        double r0 = B.Data.Systems.Mines.TriggerM, ph = Time.GetTicksMsec() / 1400.0 % 1;
        PlaneRing(xm, zm, r0, new Color(col, 0.9f * alpha), 2.5f, false);
        for (int k = 0; k < 3; k++)
        {
            double fr = (ph + k / 3.0) % 1;
            PlaneRing(xm, zm, r0 * fr, new Color(col, (float)(0.85 * (1 - fr)) * alpha), 2, false);
        }
        if (Local(xm, zm) is { } c) Draw2D.Circle(this, c, 4, new Color(col, alpha), true, -1, true);
    }

    private void DashedRing(double xm, double zm, double rM, Color col) => PlaneRing(xm, zm, rM, col, 2, true);

    // a ring on the plane, radius in metres; dashed, every other segment
    private void PlaneRing(double xm, double zm, double rM, Color col, float width, bool dashed)
    {
        if (rM <= 1) return;
        const int n = 96;
        var pts = new List<Vector2>();
        for (int i = 0; i <= n; i++)
        {
            double a = i * Math.PI * 2 / n;
            Vector2? p = Local(xm + Math.Sin(a) * rM, zm - Math.Cos(a) * rM);
            if (dashed)
            {
                if (p is { } pp && i % 2 == 0 && i < n && Local(xm + Math.Sin(a + Math.PI * 2 / n) * rM, zm - Math.Cos(a + Math.PI * 2 / n) * rM) is { } p2)
                    Draw2D.Line(this, pp, p2, col, width, true);
            }
            else if (p is { } pp2) pts.Add(pp2);
        }
        if (pts.Count >= 2) Draw2D.Polyline(this, pts.ToArray(), col, width, true);
    }
}
