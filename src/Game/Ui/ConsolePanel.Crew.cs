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
    /// <summary>Draws the roles grid of this player's bridge from (x, y), <paramref name="w"/> wide, rows <paramref name="rowH"/>
    /// tall; returns the height used.</summary>
    protected float RolesGrid(Lockstep co, float x, float y, float w, float rowH = 44)
    {
        Crew crew = co.Crew;
        var roles = crew.Roles.List;
        string bridge = crew.BridgeOf(co.Me);
        int captain = crew.CaptainOf(bridge);
        bool amCaptain = captain == co.Me;
        // the admiral is on every bridge's list: every role is theirs to work (owner, 2026-11-11: "The admiral has access to all
        // roles and all ships at all times")
        var aboard = crew.Members.Where(m => m.Id == Crew.AdmiralId || crew.BridgeOf(m.Id) == bridge && (m.Id == captain || !crew.Ships.ContainsValue(m.Id))).ToList();
        float y0 = y;
        string shipName = bridge == Crew.Flag ? (co.Flag ?? B.Player).Name : B.ShipById(bridge)?.Name ?? bridge;
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
            Text((m.Id == captain ? "★ " : "") + m.Name.ToUpperInvariant(), x, y + rowH * 0.6f, rowH >= 44 ? 17 : 14, CrewColors.Of(m.Id, C.Sky), HorizontalAlignment.Left, nameW - 6, 600, 1);
            for (int i = 0; i < roles.Count; i++)
            {
                RoleDef rd = roles[i];
                int memberId = m.Id;
                bool held = crew.HolderOf(bridge, rd.Id) == m.Id;
                var cell = new Rect2(x + nameW + i * cw + 2, y + 3, cw - 4, rowH - 6);
                // the admiral's access to a role another holds: lit, dimmed (theirs to work, the holder's to keep)
                bool access = !held && m.Id == Crew.AdmiralId;
                if (access) { Kit.Piece(this, "tab.active", cell, 0.4f); continue; }
                Tab($"crew.role.{rd.Id}.{m.Id}", cell, held && cw > 90 ? rd.Name : "", () => co.AskCrew(new JsonObject { ["op"] = "role", ["role"] = rd.Id, ["to"] = held ? captain : memberId }),
                    held, null, !amCaptain);
            }
            y += rowH;
        }
        return y - y0;
    }
}
