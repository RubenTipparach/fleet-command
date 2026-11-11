// CrewCard: the captain's menu (openspec/changes/bridge-crew-coop; owner, 2026-11-11: "we would need to introduce the
// captain's menu. assigning players roles in the game", "It is up to the captain to assign these roles"). A sheet over
// everything: the crew on your bridge against the bridge's roles (a lit box: who holds it; the captain taps a box to give
// the role, the captain's own box takes it back). The fleet (each ship's captain, its bridge crew, ships given and the
// admiral's directives) is the Fleet station's, the one fleet manager (owner: "Why did you add fleet button when there's
// already fleet manager panel"). Between battles it is also a crew member's waiting screen (Lobby): the whole crew, their
// roles and the last battle's end. What it shows is the crew's own state (Coop.Crew); every change goes through the
// lockstep (Lockstep.AskCrew), the host applying it for everyone.
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
        float rowH = 50, w = Math.Min(1100, Size.X - 32);
        float h = 140 + (aboard.Count + 1) * rowH + (co.Battle == null ? 40 + crew.Members.Count * 30 : 0) + 80;
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

        y += RolesGrid(co, x0, y, bw, rowH);

        // the whole crew between battles (in a battle, each ship's captain and bridge crew are on the Fleet station)
        if (co.Battle == null)
        {
            y += 10;
            Text("CREW", x0, y + 16, 15, C.Lilac, HorizontalAlignment.Left, bw, 700, 1);
            y += 26;
            foreach (CrewMember m in crew.Members)
            {
                string at2 = crew.BridgeOf(m.Id) == Crew.Flag ? "the flagship" : crew.BridgeOf(m.Id);
                string held = string.Join(", ", roles.Where(r => crew.HolderOf(crew.BridgeOf(m.Id), r.Id) == m.Id).Select(r => r.Name));
                X.Fonts.DrawRich(this, new[] { (m.Name.ToUpperInvariant() + "  ", CrewColors.Of(m.Id, C.Sky)),
                    ($"{(crew.CaptainOf(crew.BridgeOf(m.Id)) == m.Id ? "captain of " : "on ")}{at2}{(held.Length > 0 ? " · " + held : "")}", C.Text) }, x0, y + 22, 16, bw);
                y += 30;
            }
        }
        if (!Lobby) Button("crew.done", new Rect2(x0 + bw - 200, b.End.Y - 50, 200, 44), "Done", () => X.CrewOpen = false);
    }
}
