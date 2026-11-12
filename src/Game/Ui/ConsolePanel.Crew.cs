// ConsolePanel.Crew: the bridge's roles grid, the one drawing of it (openspec/changes/bridge-crew-coop), on the Comms station's
// Crew page in a battle and on the captain's menu sheet: a column per role (its icon), a row per crew member on your bridge,
// a lit box where they hold the role. The bridge's captain taps a box to give the role; the captain's own box takes it back.
using System;
using System.Linq;
using System.Text.Json.Nodes;
using FleetCommand.Coop;
using Godot;

namespace FleetCommand.Game.Ui;

public abstract partial class ConsolePanel
{
    /// <summary>The crew manager, one drawing for the Fleet station's Crew view and the captain's menu on the campaign map
    /// (owner, 2026-11-11: "This can be set up in the campaign map before missions too at any point"): Assign to ship, a row
    /// per crew member with a key for each ship of the fleet (the admiral's to press: an AI ship makes them its captain
    /// holding every role, a crewed one puts them in its crew); then a tab per crewed bridge and that bridge's roles grid.
    /// The ships are the battle's, or off a battle the campaign's fleet (Lockstep.Fleet).</summary>
    protected void CrewManager(Lockstep co, Rect2 r)
    {
        Crew crew = co.Crew;
        bool admiral = co.Me == Crew.AdmiralId;
        var members = crew.Members.Where(m => m.Id != Crew.AdmiralId).ToList();
        var ships = co.Fleet();
        string here = crew.BridgeOf(co.Me);
        string[] bridges = crew.Bridges().ToArray();
        string pick = X.State.CrewBridge is { } cb && bridges.Contains(cb) ? cb : here;
        int rows = crew.Members.Count(m => m.Id == Crew.AdmiralId || crew.BridgeOf(m.Id) == pick);
        float assignH = members.Count > 0 && ships.Count > 0 ? 24 + members.Count * 32 : 0;
        float rowH = Math.Clamp((r.Size.Y - 70 - assignH - (bridges.Length > 1 ? 36 : 0)) / Math.Max(1, rows), 24, 44);
        float y = r.Position.Y, x = r.Position.X, w = r.Size.X;
        if (assignH > 0)
        {
            Lbl(admiral ? "Assign to ship" : "Assignments", x, y + 14, w);
            y += 22;
            float nw = Math.Min(110, w * 0.26f), tw = (w - nw) / ships.Count;
            foreach (CrewMember m in members)
            {
                Text(m.Name.ToUpperInvariant(), x, y + 20, 15, CrewColors.Of(m.Id, C.Sky), HorizontalAlignment.Left, nw - 6, 600, 1);
                string at = crew.BridgeOf(m.Id);
                for (int i = 0; i < ships.Count; i++)
                {
                    var (key, name) = ships[i];
                    int mid = m.Id;
                    bool captain = key != Crew.Flag && crew.CaptainOf(key) == m.Id;
                    Tab($"fl.as.{m.Id}.{key}", new Rect2(x + nw + i * tw, y, tw - 4, 28), captain ? $"★ {name}" : name,
                        () => co.AskCrew(new JsonObject { ["op"] = "assign", ["member"] = mid, ["bridge"] = key }), at == key, null, !admiral);
                }
                y += 32;
            }
            y += 4;
        }
        if (bridges.Length > 1)
        {
            float bw2 = w / bridges.Length;
            for (int i = 0; i < bridges.Length; i++)
            {
                string k = bridges[i];
                Tab($"fl.rb.{k}", new Rect2(x + i * bw2, y, bw2 - 4, 30), $"Roles · {co.NameOfBridge(k)}", () => X.State.CrewBridge = k, k == pick);
            }
            y += 36;
        }
        RolesGrid(co, x, y, w, rowH, pick);
    }

    /// <summary>Draws the roles grid of this player's bridge from (x, y), <paramref name="w"/> wide, rows <paramref name="rowH"/>
    /// tall; returns the height used.</summary>
    protected float RolesGrid(Lockstep co, float x, float y, float w, float rowH = 44, string? onBridge = null)
    {
        Crew crew = co.Crew;
        var roles = crew.Roles.List;
        string bridge = onBridge ?? crew.BridgeOf(co.Me);
        int captain = crew.CaptainOf(bridge);
        // the bridge's captain gives its roles, and the admiral on any bridge
        bool amCaptain = captain == co.Me || co.Me == Crew.AdmiralId;
        // the admiral is on every bridge's list: every role is theirs to work (owner, 2026-11-11: "The admiral has access to all
        // roles and all ships at all times")
        var aboard = crew.Members.Where(m => m.Id == Crew.AdmiralId || crew.BridgeOf(m.Id) == bridge).ToList();
        float y0 = y;
        string shipName = co.NameOfBridge(bridge);
        Text($"BRIDGE · {shipName.ToUpperInvariant()}", x, y + 16, 15, C.Lilac, HorizontalAlignment.Left, w, 700, 1);
        y += 24;
        float nameW = Math.Min(220, w * 0.3f), cw = (w - nameW) / roles.Count, ic = Math.Min(26, cw - 8);
        for (int i = 0; i < roles.Count; i++)
        {
            var r = new Rect2(x + nameW + i * cw + (cw - ic) / 2, y, ic, ic);
            Kit.Icon(this, roles[i].Icon, r, C.Sky);
            Ui.Reg($"crew.head.{roles[i].Id}", r, null, null, new Tip(roles[i].Name));
        }
        y += ic + 6;
        foreach (CrewMember m in aboard)
        {
            // the member's rank before their name (Ens., Lt., ...; the admiral is the admiral)
            string rank = m.Id == Crew.AdmiralId ? "" : crew.RankOf(m.Id).Short + " ";
            Text((m.Id == captain ? "★ " : "") + rank + m.Name.ToUpperInvariant(), x, y + rowH * 0.6f, rowH >= 44 ? 17 : 14, CrewColors.Of(m.Id, C.Sky), HorizontalAlignment.Left, nameW - 6, 600, 1);
            for (int i = 0; i < roles.Count; i++)
            {
                RoleDef rd = roles[i];
                int memberId = m.Id;
                bool held = crew.Holds(bridge, rd.Id, m.Id);
                var cell = new Rect2(x + nameW + i * cw + 2, y + 3, cw - 4, rowH - 6);
                // the admiral holds every role, always (lit, not a key); a crew member's box toggles their grant (roles are not
                // exclusive: owner, 2026-11-11: "Remember. Roles are not exclusive", "It's whatever the admiral sets up")
                if (m.Id == Crew.AdmiralId) { Kit.Piece(this, "tab.active", cell, 0.55f); continue; }
                Tab($"crew.role.{rd.Id}.{m.Id}", cell, held && cw > 90 ? rd.Name : "", () => co.AskCrew(new JsonObject { ["op"] = "role", ["role"] = rd.Id, ["to"] = memberId, ["bridge"] = bridge, ["on"] = !held }),
                    held, null, !amCaptain);
            }
            y += rowH;
        }
        return y - y0;
    }
}
