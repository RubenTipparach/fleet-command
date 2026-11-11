// CrewCard: the captain's menu (openspec/changes/bridge-crew-coop; owner, 2026-11-11: "we would need to introduce the
// captain's menu. assigning players roles in the game", "It is up to the captain to assign these roles", "if you have more
// than one ship, you can asing whole ships to other players"). A sheet over everything: the crew on your bridge against the
// bridge's roles (a lit box: who holds it; the captain taps a box to give the role, the captain's own box takes it back),
// and for the admiral the fleet's other ships, each given to a crew member or left to the AI. Between battles it is also a
// crew member's waiting screen (Lobby): the roster, their roles and the last battle's end. What it shows is the crew's own
// state (Coop.Crew); every change goes through the lockstep (Lockstep.AskCrew), the host applying it for everyone.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using FleetCommand.Coop;
using FleetCommand.Game.Ui.Kit;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class CrewCard : ConsolePanel
{
    protected override bool InColumn => false;

    /// <summary>Shown as the crew's waiting screen (no battle on this peer), not over the console.</summary>
    public bool Lobby;
    /// <summary>The waiting screen's line on the last battle (its end), or the connection's state.</summary>
    public string Status = "";
    /// <summary>The address the admiral hosts on, shown on the sheet so the crew can join.</summary>
    public string Hosting = "";

    protected override void DrawPanel()
    {
        if (X.Coop is not { } co) return;
        Crew crew = co.Crew;
        DrawRect(new Rect2(Vector2.Zero, Size), Lobby ? new Color(0.01f, 0.012f, 0.03f, 1) : new Color(0, 0, 0, 0.78f));
        Ui.Reg("crew.block", new Rect2(Vector2.Zero, Size));
        var roles = crew.Roles.List;
        string bridge = crew.BridgeOf(co.Me);
        int captain = crew.CaptainOf(bridge);
        bool amCaptain = captain == co.Me, admiral = co.Me == Crew.AdmiralId;
        // the crew on this bridge: its captain, those holding a role on it, and (for its captain) everyone on no other bridge
        var aboard = crew.Members.Where(m => crew.BridgeOf(m.Id) == bridge && (m.Id == captain || !crew.Ships.ContainsValue(m.Id))).ToList();
        Ship[] others = co.Battle == null ? Array.Empty<Ship>() : B.Fleet().Where(s => s != co.Flag).ToArray();
        float rowH = 50, w = Math.Min(1100, Size.X - 32);
        int fleet = co.Battle == null ? crew.Members.Count : others.Length + 1;
        float h = 140 + (aboard.Count + 1) * rowH + 40 + fleet * 40 + crew.Members.Count * 28 + crew.Ships.Count * 40 + 80;
        var at = new Rect2((Size.X - w) / 2, Math.Max(16, (Size.Y - h) / 2), w, Math.Min(h, Size.Y - 32));
        Shapes.RoundRect(this, new Rect2(at.Position, new Vector2(w - 21, at.Size.Y)), Sk.R.Sheet, 24, 0, 0, 24);
        Rect2 b = Frame(at, Lobby ? "Bridge crew" : "Captain's menu", false);
        float x0 = b.Position.X, y = b.Position.Y + 6, bw = b.Size.X;

        string who = $"{crew.NameOf(co.Me)}{(admiral ? " · admiral of the fleet" : amCaptain ? " · captain" : "")}";
        string link = co.IsHost ? (Hosting.Length > 0 ? $"Hosting · {Hosting}" : "Hosting") : $"Aboard {crew.NameOf(Crew.AdmiralId)}'s fleet";
        Text(who.ToUpperInvariant(), x0, y + 22, 20, C.Sky, HorizontalAlignment.Left, bw * 0.5f, 700, 1);
        Text($"{link} · crew {crew.Members.Count} · in sync {co.Synced} · desync {co.Desyncs}", x0 + bw, y + 22, 15, co.Desyncs > 0 ? C.Alert : C.Lilac, HorizontalAlignment.Right, bw * 0.5f);
        y += 34;
        if (Status.Length > 0) { Text(Status, x0, y + 18, 15, C.Text, HorizontalAlignment.Left, bw); }
        y += 30;

        // the roles grid: a column per role, a row per crew member on this bridge
        string shipName = bridge == Crew.Flag ? co.Flag?.Name ?? "the flagship" : B.ShipById(bridge)?.Name ?? bridge;
        Text($"BRIDGE · {shipName.ToUpperInvariant()}", x0, y + 16, 15, C.Lilac, HorizontalAlignment.Left, bw, 700, 1);
        y += 26;
        float nameW = Math.Min(220, bw * 0.24f), cw = (bw - nameW) / roles.Count;
        for (int i = 0; i < roles.Count; i++)
        {
            var r = new Rect2(x0 + nameW + i * cw + (cw - 26) / 2, y, 26, 26);
            Kit.Icon(this, roles[i].Icon, r, C.Sky);
            Ui.Reg($"crew.head.{roles[i].Id}", r, null, null, new Tip(roles[i].Name));
        }
        y += 32;
        foreach (CrewMember m in aboard)
        {
            Text((m.Id == captain ? "★ " : "") + m.Name.ToUpperInvariant(), x0, y + 28, 17, m.Id == co.Me ? C.Sky : C.Text, HorizontalAlignment.Left, nameW - 8, 600, 1);
            for (int i = 0; i < roles.Count; i++)
            {
                RoleDef rd = roles[i];
                int memberId = m.Id;
                bool held = crew.HolderOf(bridge, rd.Id) == m.Id;
                var cell = new Rect2(x0 + nameW + i * cw + 3, y + 4, cw - 6, rowH - 10);
                Tab($"crew.role.{rd.Id}.{m.Id}", cell, held ? rd.Name : "", () => co.AskCrew(new JsonObject { ["op"] = "role", ["role"] = rd.Id, ["to"] = held ? captain : memberId }),
                    held, null, !amCaptain);
            }
            y += rowH;
        }

        // the fleet manifest, for everyone: every ship of the fleet, its captain (the admiral, a crew member, or the AI), its
        // hull, who stands on its bridge with which stations, and what each station has locked; the admiral gives ships here
        // and sends a directive to each crew member's ship (owner, 2026-11-11: "Crew manifest", "someway for players to see
        // and interact with crew members in game")
        y += 10;
        Text(admiral ? "FLEET MANIFEST · GIVE A SHIP, SEND A DIRECTIVE" : "FLEET MANIFEST", x0, y + 16, 15, C.Lilac, HorizontalAlignment.Left, bw, 700, 1);
        y += 26;
        if (co.Battle == null)
        {
            foreach (CrewMember m in crew.Members)
            {
                string at2 = crew.BridgeOf(m.Id) == Crew.Flag ? "the flagship" : crew.BridgeOf(m.Id);
                string held = string.Join(", ", roles.Where(r => crew.HolderOf(crew.BridgeOf(m.Id), r.Id) == m.Id).Select(r => r.Name));
                X.Fonts.DrawRich(this, new[] { (m.Name.ToUpperInvariant() + "  ", CrewColors.Of(m.Id, C.Sky)),
                    ($"{(crew.CaptainOf(crew.BridgeOf(m.Id)) == m.Id ? "captain of " : "on ")}{at2}{(held.Length > 0 ? " · " + held : "")}", C.Text) }, x0, y + 22, 16, bw);
                y += 30;
            }
        }
        else
        {
            var choices = new[] { (Id: Crew.AdmiralId, Label: "AI") }.Concat(crew.Members.Where(m => m.Id != Crew.AdmiralId).Select(m => (Id: m.Id, Label: m.Name))).ToArray();
            float tw = Math.Min(150, (bw - nameW - 200) / Math.Max(1, choices.Length));
            foreach (Ship s in B.Fleet().Prepend(co.Flag!).Where(s => s != null).Distinct())
            {
                bool flag = s == co.Flag;
                string key = flag ? Crew.Flag : s.Id;
                int cap = flag ? Crew.AdmiralId : crew.Ships.GetValueOrDefault(s.Id, 0);
                string capName = cap == 0 ? "AI" : crew.NameOf(cap);
                Text(s.Name.ToUpperInvariant(), x0, y + 22, 17, C.Text, HorizontalAlignment.Left, nameW - 8, 600, 1);
                X.Fonts.DrawRich(this, new[] { ("captain ", C.Lilac), (capName.ToUpperInvariant(), cap == 0 ? C.Lilac : CrewColors.Of(cap, C.Sky)),
                    ($" · hull {100 * s.Hull / s.Class.HullHp:0}%", s.Hull < s.Class.HullHp * 0.35 ? C.Alert : C.Lilac) }, x0 + nameW, y + 22, 16, 230);
                if (admiral && !flag)
                    for (int i = 0; i < choices.Length; i++)
                    {
                        int to = choices[i].Id;
                        string sid = s.Id;
                        Tab($"crew.ship.{s.Id}.{to}", new Rect2(x0 + nameW + 236 + i * tw, y + 2, tw - 6, 34), choices[i].Label,
                            () => co.AskCrew(new JsonObject { ["op"] = "ship", ["ship"] = sid, ["to"] = to }), (cap == 0 ? Crew.AdmiralId : cap) == to);
                    }
                y += 40;
                // who stands on this bridge, with each station's lock
                foreach (CrewMember m in crew.Members.Where(m => crew.BridgeOf(m.Id) == key && (m.Id == cap || crew.Held.Any(kv => kv.Key.StartsWith(key + "|") && kv.Value == m.Id))))
                {
                    float xx = x0 + 24;
                    Text(m.Name.ToUpperInvariant(), xx, y + 20, 15, CrewColors.Of(m.Id, C.Sky), HorizontalAlignment.Left, nameW - 30, 600, 1);
                    xx = x0 + nameW;
                    foreach (RoleDef r in roles.Where(r => crew.HolderOf(key, r.Id) == m.Id))
                    {
                        Kit.Icon(this, r.Icon, new Rect2(xx, y + 4, 20, 20), CrewColors.Of(m.Id, C.Sky));
                        string lockOn = r.Targets && s.RoleTargets != null && B.ShipById(Battle.RoleTargetId(s, r.Id)) is { Alive: true } lt ? lt.Name : "";
                        Text(lockOn, xx + 24, y + 19, 13, C.Lilac, HorizontalAlignment.Left, 96, 500);
                        xx += lockOn.Length > 0 ? 124 : 28;
                        if (xx > x0 + bw - 130) break;
                    }
                    y += 28;
                }
                // the admiral's directives to a crew member's ship
                if (admiral && cap > 1)
                {
                    float dx = x0 + 24;
                    foreach (var (id, label) in Lockstep.Directives)
                    {
                        string order = id;
                        Button($"crew.dir.{cap}.{id}", new Rect2(dx, y + 2, 180, 32), label, () => co.Direct(cap, order), dis: id == "attack" && B.TargetOf(co.Flag!, "weapons") == null);
                        dx += 188;
                    }
                    if (co.DirectiveFor(cap) is { } d) Text($"sent: {d.Text}", dx + 6, y + 23, 14, C.Caution, HorizontalAlignment.Left, x0 + bw - dx - 10);
                    y += 40;
                }
            }
        }
        if (!Lobby) Button("crew.done", new Rect2(x0 + bw - 200, b.End.Y - 50, 200, 44), "Done", () => X.CrewOpen = false);
    }
}
