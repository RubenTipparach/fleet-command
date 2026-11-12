// StationPanel.Crew: co-op's strip over the Fleet and Tactical stations (openspec/changes/bridge-crew-coop; owner, 2026-11-11:
// "There's the fleet manager and a targeting manager tap both should let you change bridge and change targets"). Bridge: a
// key for each crewed bridge (the flagship's and each ship a crew member captains), the one you stand on lit; a key moves you
// there, holding no station until its captain gives you some. Lock for: a key for each targeting station you open, with
// what it has locked; the lit ones are where your next lock goes (none lit: every station you hold, as before). Both are
// orders through the lockstep (Lockstep.AskCrew, the order's "for"), so every player's battle agrees.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using FleetCommand.Coop;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel
{
    /// <summary>Co-op, on the Fleet station for the ship picked (the one fleet manager; owner, 2026-11-11: "Why did you add
    /// fleet button when there's already fleet manager panel"): its captain (the admiral gives it to a crew member, or back to
    /// the AI), who stands on its bridge with their stations and each station's lock, and the admiral's directives to a crew
    /// member's ship. Returns the height used, and whether a crew member captains it (the AI's orders then do not apply).</summary>
    private (float H, bool Crewed) FleetCrew(Ship f, float x, float y, float w)
    {
        if (X.Coop is not { Battle: not null } co) return (0, false);
        Crew crew = co.Crew;
        float y0 = y;
        bool flag = f == co.Flag;
        string key = flag ? Crew.Flag : f.Id;
        int cap = flag ? Crew.AdmiralId : crew.Ships.GetValueOrDefault(f.Id, 0);
        bool admiral = co.Me == Crew.AdmiralId;
        // the captain, read out (crew are sent to ships in the Crew view's assignments, the one place for it)
        Lbl("Captain", x, y + 24, 104);
        Text((cap == 0 ? "AI" : crew.NameOf(cap)).ToUpperInvariant(), x + 110, y + 26, 18, cap == 0 ? C.Lilac : CrewColors.Of(cap, C.Sky), HorizontalAlignment.Left, w - 110, 600, 1);
        y += 44;
        // who stands on this bridge: name, then each station they hold with its lock
        // the admiral first, on every ship, holding every station; then its own crew with the stations granted them
        foreach (CrewMember m in crew.Members.Where(m => m.Id == Crew.AdmiralId || crew.BridgeOf(m.Id) == key).OrderBy(m => m.Id))
        {
            Text((m.Id == Crew.AdmiralId ? "" : crew.RankOf(m.Id).Short + " ") + m.Name.ToUpperInvariant(), x, y + 18, 15, CrewColors.Of(m.Id, C.Sky), HorizontalAlignment.Left, 104, 600, 1);
            float xx = x + 110;
            bool adm = m.Id == Crew.AdmiralId;
            foreach (RoleDef r in crew.Roles.List.Where(r => crew.Holds(key, r.Id, m.Id)))
            {
                bool theirs = true;
                // each station's icon (dimmed: the admiral's access to a station another holds), its lock on hover
                var ir = new Rect2(xx, y + 2, 22, 22);
                Kit.Icon(this, r.Icon, ir, new Color(CrewColors.Of(m.Id, C.Sky), theirs ? 1 : 0.4f));
                string lockOn = r.Targets && f.RoleTargets != null && B.ShipById(Battle.RoleTargetId(f, r.Id)) is { Alive: true } lt ? Aiming.ContactName(B, lt) : "no lock";
                Ui.Reg($"fl.role.{f.Id}.{m.Id}.{r.Id}", ir, null, null, new Tip(r.Targets ? $"{r.Name}: {lockOn}" : r.Name));
                xx += 30;
            }
            y += 26;
        }
        // the admiral's directives to a crew member's ship
        if (admiral && cap > 1)
        {
            y += 6;
            float dw = w / Lockstep.Directives.Length;
            for (int i = 0; i < Lockstep.Directives.Length; i++)
            {
                var (id, label) = Lockstep.Directives[i];
                string shortLabel = id switch { "attack" => "Attack", "escort" => "Escort", "hold" => "Hold", _ => "Free" };
                Button($"fl.dir.{id}", new Rect2(x + i * dw, y, dw - 4, 36), shortLabel, () => co.Direct(cap, id), dis: id == "attack" && B.TargetOf(co.Flag!, "weapons") == null, tip: new Tip(label));
            }
            y += 40;
            if (co.DirectiveFor(cap) is { } d) { Text($"sent: {d.Text}", x, y + 14, 13, C.Caution, HorizontalAlignment.Left, w); y += 20; }
        }
        return (y - y0 + 6, cap > 1);
    }

    // the Fleet station's Crew view: change bridge and lock for, then the bridge's roles grid (the captain gives stations here)
    private void CrewPage(Rect2 b)
    {
        if (X.Coop is not { } co) return;
        Rect2 rest = CrewStrip(b);
        CrewManager(co, rest);
    }

    /// <summary>Draws the co-op strip at the top of <paramref name="b"/> and returns the room left under it.</summary>
    private Rect2 CrewStrip(Rect2 b)
    {
        if (X.Coop is not { Battle: not null } co) return b;
        Crew crew = co.Crew;
        float x = b.Position.X, y = b.Position.Y, lw = 86, w = b.Size.X - lw, h = 34;
        string here = crew.BridgeOf(co.Me);
        bool admiral = co.Me == Crew.AdmiralId;
        bool canMove = admiral || !crew.Ships.ContainsValue(co.Me);
        Lbl("Bridge", x, y + 22, lw - 6);
        // the admiral steps onto any ship of the fleet; a crew member onto the flagship's or a crewed ship's bridge
        string[] bridges = admiral
            ? new[] { Crew.Flag }.Concat(B.Fleet().Where(s => s != co.Flag && !s.Fixed).Select(s => s.Id)).ToArray()
            : crew.Bridges().ToArray();
        float bw = w / Math.Max(1, bridges.Length);
        for (int i = 0; i < bridges.Length; i++)
        {
            string k = bridges[i];
            Ship? s = k == Crew.Flag ? co.Flag : B.ShipById(k);
            int cap = k == Crew.Flag ? Crew.AdmiralId : crew.Ships.GetValueOrDefault(k, 0);
            string label = $"{s?.Name ?? k} · {(cap == 0 ? "AI" : crew.NameOf(cap))}";
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
