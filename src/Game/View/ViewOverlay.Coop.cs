// ViewOverlay.Coop: the co-op crew over the 3D view (openspec/changes/bridge-crew-coop; owner, 2026-11-11: "someway for
// players to see and interact with crew members in game. Adding that real time strategy game layer to gameplay"). Pings:
// the Ping key, then a click on a ship or the battle plane, marks it for the whole crew, a pulsing ring in the sender's
// colour with their name, for eight seconds. The admiral's directive to a ship's captain: a banner at the top of the
// captain's view naming it, with Aye to answer. Every ping and directive is a crew event (Lockstep), drawn from there.
using System;
using System.Linq;
using FleetCommand.Coop;
using FleetCommand.Game.Ui;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class ViewOverlay
{
    private const long PingMs = 8000;

    // the ping key's click: a ship under the pointer, else the point on the plane
    private bool PingClick(Vector2 local)
    {
        if (!_x.State.PlacingPing || _x.Coop is not { } co) return false;
        _x.State.PlacingPing = false;
        if (ShipAt(local) is { } s) co.Ping(s.X, s.Z, s.Id);
        else if (_view.PlaneAt(local + Position) is { } q) co.Ping(q.X, q.Z, null);
        return true;
    }

    private void DrawCoop(Fonts f)
    {
        if (_x.Coop is not { } co) return;
        Battle b = B;
        long now = co.Clock();
        foreach (CoopEvent e in co.Events.Where(e => (e.Kind == "ping" || e.Kind == "directive" && e.Ship != null) && now - e.Received < PingMs))
        {
            double x = e.X, z = e.Z;
            if (e.Ship != null && b.ShipById(e.Ship) is { Alive: true } s) { x = s.X; z = s.Z; }
            if (Local(x, z) is not { } p) continue;
            Color col = CrewColors.Of(e.By, C.Sky);
            float t = (now - e.Received) / 1000f, pulse = (t % 1.2f) / 1.2f, fade = Math.Clamp((PingMs / 1000f - t) / 2f, 0, 1);
            DrawArc(p, 30 + 26 * pulse, 0, Mathf.Tau, 40, new Color(col, (1 - pulse) * fade), 3, true);
            DrawArc(p, 26, 0, Mathf.Tau, 40, new Color(col, 0.9f * fade), 2.5f, true);
            DrawLine(p + new Vector2(0, -26), p + new Vector2(0, -60), new Color(col, fade), 2, true);
            string lab = e.Kind == "directive" ? $"{co.Crew.NameOf(e.By)} · {e.Text}" : co.Crew.NameOf(e.By);
            f.Draw(this, lab.ToUpperInvariant(), p.X, p.Y - 66, 16, new Color(col, fade), HorizontalAlignment.Center, 0, 700, 1);
        }
        // the admiral's directive to this captain, until answered
        if (co.DirectiveFor(co.Me) is { } d)
        {
            float w = Math.Min(620, Size.X - 40), x0 = (Size.X - w) / 2, y0 = 110;
            DrawRect(new Rect2(x0, y0, w, 50), new Color(0, 0, 0, 0.7f));
            DrawRect(new Rect2(x0, y0, w, 50), C.Caution, false, 2);
            f.DrawRich(this, new[] { ($"{co.Crew.NameOf(d.By).ToUpperInvariant()}  ", CrewColors.Of(d.By, C.Sky)), (d.Text.ToUpperInvariant(), C.Caution) }, x0 + 14, y0 + 32, 19, w - 130);
            Key("v.aye", new Rect2(x0 + w - 104, y0 + 10, 92, 30), "Aye", co.Ack, false, "Answer the admiral");
        }
        if (_x.State.PlacingPing) f.Draw(this, "CLICK A SHIP OR A POINT TO MARK IT FOR THE CREW", Size.X / 2, 74, 20, C.Lilac, HorizontalAlignment.Center, 0, 600, 1);
    }

    // the Ping key, top left of the view under the objective (the Target box holds the top right)
    private void CoopKeys()
    {
        if (_x.Coop == null) return;
        Key("v.ping", new Rect2(14, 92, 90, 30), "Ping", () => _x.State.PlacingPing = !_x.State.PlacingPing, _x.State.PlacingPing, "Mark a ship or a point for the crew");
    }
}
