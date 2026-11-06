// ViewOverlay.Comms: comms and transport over the battle view (the battle console mockup's uiView): the planets of a
// scene, each a ring and its name in green, a click selecting it for transport and the helm (bracketed once selected);
// what a friendly ship or planet still asks for under its name, and PILOT NEEDED under a prize with no pilot; the
// transporter's reach as a dashed ring round your ship named on itself (owner, 2026-11-03: "If the transport tab is open,
// show radius as a dotted line with the label "transport range" drawn on the radius"), green while the destination is
// inside it; and the event log over the view, opened from Comms, closed by its X or a click anywhere else.
using System;
using System.Linq;
using FleetCommand.Game.Ui;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class ViewOverlay
{
    private float? _reachLabelA;

    /// <summary>Where a ship or planet of the battle is on screen (global coordinates), or null off the view: what a
    /// captain clicks to select it (the scripted captain's select beat).</summary>
    public Vector2? ScreenPointOf(string id)
    {
        double x, z;
        if (B.ShipById(id) is { Alive: true } s) (x, z) = (s.X, s.Z);
        else if (B.Planets.FirstOrDefault(p => p.Id == id) is { } p) (x, z) = (p.X, p.Z);
        else return null;
        return Local(x, z) is { } l && new Rect2(Vector2.Zero, Size).Grow(-8).HasPoint(l) ? l + GlobalPosition : null;
    }

    partial void DrawComms(Fonts f)
    {
        Battle b = B;
        Ship me = b.Player;
        var view = new Rect2(Vector2.Zero, Size);
        TransportDest? dest = b.InvPick();
        // the planets: a ring and the name, a click selects it; selected, green corners, its range and what it still needs
        foreach (Planet p in b.Planets)
        {
            if (Local(p.X, p.Z) is not { } lp || !view.Grow(-30).HasPoint(lp)) continue;
            bool sel = b.PlanetDest == p.Id;
            float rr = Math.Max(30, Local(p.X + p.RadiusM, p.Z) is { } e ? e.DistanceTo(lp) : 30);
            var hit = new Rect2(lp - new Vector2(rr, rr), new Vector2(2 * rr, 2 * rr)).Intersection(view.Grow(-2));
            string id = p.Id;
            HitState hs = _ui.Reg("v.planet." + id, hit, _ => B.SelectPlanet(id), null,
                new Tip(p.Name, new (string, string, Color?)[] { ("Range", $"{Battle.PlanetKm(me, p):0.0} km", null), ("Click", "select for transport", null) }));
            Mark(f, lp, p.Name, Battle.PlanetKm(me, p), p, sel, hs.Hot);
        }
        // a friendly ship still asking for something says so under its name
        foreach (Ship s in b.Ships)
        {
            if (!s.Alive || s == me || s.Side != me.Side || Local(s.X, s.Z) is not { } lp || !view.Grow(-4).HasPoint(lp)) continue;
            bool locked = me.Target == s.Id;
            float ny = locked ? 58 + 36 : 34 + 18;
            if (Battle.PilotNeeded(s)) { f.Draw(this, "PILOT NEEDED", lp.X, lp.Y + ny, 16, C.Caution, HorizontalAlignment.Center, 0, 700); ny += 18; }
            string need = Battle.NeedsText(s);
            if (need != "") f.Draw(this, $"NEEDS {need.ToUpperInvariant()}", lp.X, lp.Y + ny, 15, C.Caution, HorizontalAlignment.Center);
        }
        // the transporter's reach while the Transport station is open
        if (_x.State.Station == "transport" && me.Alive)
        {
            string tab = _x.State.TransportTab;
            bool inv = tab is "cargo" or "mines";
            double dk = inv || (tab == "capture" && b.TargetOf(me) == null) ? dest?.Km ?? double.PositiveInfinity
                : b.TargetOf(me) is { } t ? Rules.DistKm(me, t) : double.PositiveInfinity;
            bool inR = dk <= b.Comms.Transport.RangeKm;
            ReachRing(f, inR ? C.Accord : C.Sky, inR ? "TRANSPORT RANGE · IN REACH" : "TRANSPORT RANGE");
        }
        // the event log itself is drawn by ViewOverlay.Tactical (one log, opened from Comms)
    }

    // a friendly mark (a planet's): its ring in green, or green corners once selected, its name, range and needs
    private void Mark(Fonts f, Vector2 p, string name, double km, ICargoHolder o, bool sel, bool hot)
    {
        float k = hot ? 1.1f : 1;
        Color col = hot ? C.Accord.Lightened(0.3f) : C.Accord;
        if (sel)
            foreach (var (sx, sy) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
            {
                var c0 = p + new Vector2(sx * 40 * k, sy * 30 * k);
                DrawPolyline(new[] { c0 - new Vector2(sx * 16, 0), c0, c0 - new Vector2(0, sy * 16) }, col, hot ? 4 : 3, true);
            }
        else DrawArc(p, 16 * (hot ? 1.18f : 1), 0, Mathf.Tau, 32, hot ? col : new Color(col, 0.7f), 1.5f + (hot ? 1 : 0), true);
        float ny = sel ? 50 : 16 * 1.18f + 6 + 14;
        bool reach = km <= B.Comms.Transport.RangeKm;
        f.Draw(this, (sel ? $"{name} · {km:0.0} km{(reach ? " · in reach" : "")}" : name).ToUpperInvariant(), p.X, p.Y + ny, sel ? 18 : 14, C.Accord, HorizontalAlignment.Center);
        string need = Battle.NeedsText(o);
        if (need != "") f.Draw(this, $"NEEDS {need.ToUpperInvariant()}", p.X, p.Y + ny + (sel ? 20 : 18), 15, C.Caution, HorizontalAlignment.Center);
    }

    // The dashed ring of the transporter's reach round your ship, its name on the ring at the point highest on screen,
    // turned along the ring and eased toward that point so it glides as the camera turns (owner, 2026-11-03: "can we make
    // this text move smoother?").
    private void ReachRing(Fonts f, Color col, string label)
    {
        Ship s = B.Player;
        double rm = B.Comms.Transport.RangeKm * 1000;
        Vector2? At(float a) => Local(s.X + Math.Sin(a) * rm, s.Z - Math.Cos(a) * rm);
        const int n = 96;
        for (int i = 0; i < n; i += 2)
            if (At(i * Mathf.Tau / n) is { } p0 && At((i + 1) * Mathf.Tau / n) is { } p1) DrawLine(p0, p1, col, 2, true);
        float? best = null, by = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.Tau / n;
            if (At(a) is { } q && q.X > 60 && q.X < Size.X - 60 && q.Y > 110 && q.Y < by) { by = q.Y; best = a; }
        }
        if (best is not { } ta) return;
        _reachLabelA = _reachLabelA is { } la && Math.Abs(Mathf.Wrap(ta - la, -Mathf.Pi, Mathf.Pi)) < 1 ? la + Mathf.Wrap(ta - la, -Mathf.Pi, Mathf.Pi) * 0.15f : ta;
        float at = _reachLabelA.Value;
        if (At(at) is not { } top || At(at - 0.08f) is not { } q0 || At(at + 0.08f) is not { } q1) return;
        float ang = (q1 - q0).Angle();
        if (ang > Mathf.Pi / 2 || ang < -Mathf.Pi / 2) ang += Mathf.Pi;
        float tw = f.Width(label, 15, 700, 2) + 16;
        DrawSetTransform(top, ang, Vector2.One);
        DrawRect(new Rect2(-tw / 2, -12, tw, 18), Colors.Black);
        f.Draw(this, label, 0, 2, 15, col, HorizontalAlignment.Center, 0, 700, 2);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    // The battle log over the view, newest first, refusals in Caution; its X and a click anywhere else close it.
}
