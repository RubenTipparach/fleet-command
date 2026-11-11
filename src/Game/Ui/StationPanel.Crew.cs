// StationPanel.Crew: co-op's strip over the Fleet and Tactical stations (openspec/changes/bridge-crew-coop; owner, 2026-11-11:
// "There's the fleet manager and a targeting manager tap both should let you change bridge and change targets"). Bridge: a
// key for each crewed bridge (the flagship's and each ship a crew member captains), the one you stand on lit; a key moves you
// there, holding no station until its captain gives you some. Lock for: a key for each targeting station you open, with
// what it has locked; the lit ones are where your next lock goes (none lit: every station you hold, as before). Both are
// orders through the lockstep (Lockstep.AskCrew, the order's "for"), so every player's battle agrees.
using System;
using System.Linq;
using System.Text.Json.Nodes;
using FleetCommand.Coop;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel
{
    /// <summary>Draws the co-op strip at the top of <paramref name="b"/> and returns the room left under it.</summary>
    private Rect2 CrewStrip(Rect2 b)
    {
        if (X.Coop is not { Battle: not null } co) return b;
        Crew crew = co.Crew;
        float x = b.Position.X, y = b.Position.Y, lw = 86, w = b.Size.X - lw, h = 34;
        string here = crew.BridgeOf(co.Me);
        bool canMove = co.Me != Crew.AdmiralId && !crew.Ships.ContainsValue(co.Me);
        Lbl("Bridge", x, y + 22, lw - 6);
        string[] bridges = crew.Bridges().ToArray();
        float bw = w / Math.Max(1, bridges.Length);
        for (int i = 0; i < bridges.Length; i++)
        {
            string k = bridges[i];
            Ship? s = k == Crew.Flag ? co.Flag : B.ShipById(k);
            string label = $"{s?.Name ?? k} · {crew.NameOf(crew.CaptainOf(k))}";
            Tab($"cs.br.{k}", new Rect2(x + lw + i * bw, y, bw - 4, h), label,
                () => co.AskCrew(new JsonObject { ["op"] = "bridge", ["bridge"] = k }), k == here, null, !canMove || k == here || s is not { Alive: true });
        }
        y += h + 6;
        Lbl("Lock for", x, y + 22, lw - 6);
        var mine = crew.Roles.List.Where(r => r.Targets && crew.Opens(co.Me, r.Id)).ToArray();
        float rw = w / Math.Max(1, mine.Length);
        Ship me = Me;
        for (int i = 0; i < mine.Length; i++)
        {
            RoleDef r = mine[i];
            string on = me.RoleTargets != null && B.ShipById(Battle.RoleTargetId(me, r.Id)) is { Alive: true } t ? Aiming.ContactName(B, t) : "none";
            bool lit = X.State.LockFor.Contains(r.Id);
            var key = new Rect2(x + lw + i * rw + (rw - 4 - 40) / 2, y, 40, h);
            // the station's icon as the key (its name on hover), what it has locked under it
            Tab($"cs.for.{r.Id}", key, $"{r.Name}: {on}", () => { if (!X.State.LockFor.Remove(r.Id)) X.State.LockFor.Add(r.Id); }, lit, r.Icon);
            Text(on.ToUpperInvariant(), x + lw + i * rw + (rw - 4) / 2, y + h + 13, 12, lit ? C.Sky : C.Lilac, HorizontalAlignment.Center, rw - 6, 600);
        }
        y += 16;
        y += h + 10;
        return new Rect2(b.Position.X, y, b.Size.X, b.End.Y - y);
    }
}
