// PortPanel: the port over the campaign map (game-flow design, "Campaign ports"): the shipyard, the fleet, the outfitter,
// supplies and recruiting at your own ports, and selling at a port that deals with you. Its own panel, so the map's HUD
// (MapHud) and the port change independently. Every purchase goes through Campaign (src/Core/Campaign).
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Campaign;
using FleetCommand.Game.Ui;
using Godot;

namespace FleetCommand.Game.Flow;

public sealed partial class PortPanel : FlowPanel
{
    private static readonly (string Id, string Label)[] PortTabs =
        { ("yard", "Shipyard"), ("fleet", "Fleet"), ("outfit", "Outfitter"), ("supply", "Supplies"), ("crew", "Recruit") };

    private bool Narrow => Size.X < 1100;

    protected override void DrawPanel()
    {
        if (Camp is { } c && F.Port != null) PortPanel_(c);
    }

    private void PortPanel_(Campaign.Campaign c)
    {
        float w = Narrow ? Size.X - 20 : Math.Min(760, Size.X - 500);
        var at = new Rect2(Narrow ? 10 : 16, 80, w, Size.Y - 160);
        Rect2 b = Sheet(at, c.At.PortName ?? "Port");
        bool own = c.IsPort(c.At);
        var tabs = own ? PortTabs : PortTabs.Where(t => t.Id == "fleet").ToArray();
        if (!tabs.Any(t => t.Id == F.Port)) F.Port = tabs[0].Id;
        Tabs("port.tab", new Rect2(b.Position.X, b.Position.Y + 6, b.Size.X - 120, 40), tabs, F.Port!, id => F.Port = id);
        Button("port.close", new Rect2(b.End.X - 110, b.Position.Y + 4, 110, 44), "Close", () => F.Port = null);
        var body = new Rect2(b.Position.X, b.Position.Y + 62, b.Size.X, b.Size.Y - 62);
        switch (F.Port)
        {
            case "yard": Yard(c, body); break;
            case "fleet": FleetTab(c, body, own); break;
            case "outfit": Outfit(c, body); break;
            case "supply": Supplies(c, body); break;
            case "crew": Crew(c, body); break;
        }
    }

    private void Schematic(Hull h, Rect2 box)
    {
        Kit.Piece(this, "frame.inset", box);
        if (!Schematics.DrawFit(this, h.Schematic == "" ? null : h.Schematic, box.Grow(-8), C.Signal, C.Sky))
        {
            Text("NO SCHEMATIC", box.GetCenter().X, box.GetCenter().Y - 4, 15, C.Lilac, HorizontalAlignment.Center, box.Size.X - 8, 600, 1);
            Text("a stand-in hull", box.GetCenter().X, box.GetCenter().Y + 18, 14, C.Lilac, HorizontalAlignment.Center, box.Size.X - 8, 500);
        }
    }

    private void Yard(Campaign.Campaign c, Rect2 b)
    {
        float y = b.Position.Y, rowH = Math.Min(150, (b.Size.Y - 10) / Math.Max(1, c.Data.HullsOf(c.S.Side).Count(h => h.ForSale)));
        foreach (Hull h in c.Data.HullsOf(c.S.Side).Where(h => h.ForSale))
        {
            Tier t = c.Data.TierOf(h);
            var sch = new Rect2(b.Position.X, y, rowH * 0.62f, rowH - 10);
            Schematic(h, sch);
            float tx = sch.End.X + 16, tw = b.End.X - tx;
            Text(h.Name.ToUpperInvariant(), tx, y + 28, 24, C.Sky, HorizontalAlignment.Left, tw - 180, 700, 1);
            Text($"{h.Cls} · tier {h.Tier} · hull {t.Hp}", tx, y + 54, 17, C.Lilac, HorizontalAlignment.Left, tw - 180, 600);
            string slots = string.Join("  ", c.Data.Groups.Where(gp => t.Slots[gp.Id] > 0).Select(gp => $"{gp.Name.Split(' ')[0]} {t.Slots[gp.Id]}"));
            Text(slots, tx, y + 78, 15, C.Text, HorizontalAlignment.Left, tw - 180, 500);
            int price = c.HullPrice(h.Id);
            string hid = h.Id;
            Button($"yard.{h.Id}", new Rect2(b.End.X - 170, y + 20, 170, 46), $"Buy {price} cr", () => { F.Msg = c.Buy(hid) ?? $"The {h.Name} joins the fleet."; F.Save(); },
                dis: price > c.S.Credits || c.S.Fleet.Count >= c.Data.Rules.FleetMax);
            y += rowH;
        }
    }

    private void FleetTab(Campaign.Campaign c, Rect2 b, bool own)
    {
        float y = b.Position.Y, rowH = Math.Min(150, (b.Size.Y - 70) / Math.Max(1, c.S.Fleet.Count));
        foreach (CampShip s in c.S.Fleet.ToList())
        {
            Hull h = c.Data.Hulls[s.Hull];
            Schematic(h, new Rect2(b.Position.X, y, rowH * 0.62f, rowH - 10));
            float tx = b.Position.X + rowH * 0.62f + 16, tw = b.End.X - tx;
            Text(s.Name.ToUpperInvariant(), tx, y + 28, 22, C.Sky, HorizontalAlignment.Left, tw - 180, 700, 1);
            Text($"{h.Name} · {h.Cls} · hull {s.HullPct:0} %", tx, y + 54, 17, s.HullPct < 50 ? C.Caution : C.Lilac, HorizontalAlignment.Left, tw - 180, 600);
            Text($"worth {c.Points(s) * c.Data.Rules.CreditRate} cr new · marines {s.Supplies.GetValueOrDefault("marines")}", tx, y + 78, 15, C.Text, HorizontalAlignment.Left, tw - 180, 500);
            CampShip ss = s;
            Button($"fleet.sell.{s.Id}", new Rect2(b.End.X - 170, y + 20, 170, 46), "Sell", () => { F.Msg = c.Sell(ss) ?? $"The {ss.Name} is sold."; F.Save(); }, dis: c.S.Fleet.Count <= 1);
            y += rowH;
        }
        if (own)
        {
            int cost = c.RepairCost();
            Button("fleet.repair", new Rect2(b.Position.X, b.End.Y - 52, 300, 48), cost > 0 ? $"Repair all: {cost} cr" : "Hulls are whole", () => { F.Msg = c.Repair() ?? "Repaired."; F.Save(); }, dis: cost == 0 || cost > c.S.Credits);
        }
    }

    private CampShip PortShip(Campaign.Campaign c, Rect2 b)
    {
        F.PortShip = Math.Clamp(F.PortShip, 0, c.S.Fleet.Count - 1);
        float tw = Math.Min(180, (b.Size.X - 8 * (c.S.Fleet.Count - 1)) / c.S.Fleet.Count);
        for (int i = 0; i < c.S.Fleet.Count; i++)
        {
            int idx = i;
            Tab($"ps.{i}", new Rect2(b.Position.X + i * (tw + 8), b.Position.Y, tw, 38), c.S.Fleet[i].Name, () => F.PortShip = idx, F.PortShip == i);
        }
        return c.S.Fleet[F.PortShip];
    }

    private void Outfit(Campaign.Campaign c, Rect2 b)
    {
        CampShip s = PortShip(c, b);
        Hull h = c.Data.Hulls[s.Hull];
        float sw = Math.Min(220, b.Size.X * 0.3f);
        Schematic(h, new Rect2(b.Position.X, b.Position.Y + 50, sw, b.Size.Y - 60));
        float x = b.Position.X + sw + 16, y = b.Position.Y + 72, w = b.End.X - x;
        foreach (var (gid, gname) in c.Data.Groups)
        {
            var slots = s.Fit[gid];
            if (slots.Count == 0) continue;
            Lbl(gname, x, y); y += 8;
            var options = c.Data.Systems.Values.Where(sy => sy.Group == gid).OrderBy(sy => sy.Cost).ToList();
            for (int i = 0; i < slots.Count; i++)
            {
                string? cur = slots[i];
                int si = i;
                ShipSystem? now = cur != null ? c.Data.Systems[cur] : null;
                // a hardpoint's key steps through the systems that fit it, paying the difference
                ShipSystem next = options[(options.FindIndex(o => o.Id == cur) + 1) % options.Count];
                Button($"fit.{gid}.{i}", new Rect2(x, y + 4, w, 36), $"{now?.Name ?? "empty"}  ·  next: {next.Name}", () => { F.Msg = c.Refit(s, gid, si, next.Id) ?? $"Fitted: {next.Name}."; F.Save(); },
                    center: false, tip: new Tip(now?.Name ?? "Empty", new (string, string, Color?)[] { ("Does", now?.Desc ?? "nothing", null), ("Next", $"{next.Name}: {next.Cost * c.Data.Rules.CreditRate} cr", null) }));
                y += 42;
                if (y > b.End.Y - 40) return;
            }
            y += 22;
        }
    }

    private void Supplies(Campaign.Campaign c, Rect2 b)
    {
        CampShip s = PortShip(c, b);
        float y = b.Position.Y + 80, x = b.Position.X, w = b.Size.X;
        foreach (Supply u in c.Data.Supplies)
        {
            int max = c.SupplyMax(s, u.Id), now = s.Supplies[u.Id];
            Lbl(u.Name, x, y + 26, w * 0.4f);
            Text($"{now} of {max}", x + w * 0.42f, y + 26, 20, C.Sky, HorizontalAlignment.Left, 120, 600);
            Text($"{u.Cost * c.Data.Rules.CreditRate} cr each", x + w * 0.42f + 110, y + 26, 16, C.Lilac, HorizontalAlignment.Left, 130, 500);
            string id = u.Id;
            Button($"sup.{u.Id}.minus", new Rect2(b.End.X - 120, y, 52, 40), "-", () => { F.Msg = c.Resupply(s, id, -1) ?? ""; F.Save(); }, dis: now == 0);
            Button($"sup.{u.Id}.plus", new Rect2(b.End.X - 60, y, 52, 40), "+", () => { F.Msg = c.Resupply(s, id, 1) ?? ""; F.Save(); }, dis: now >= max);
            y += 54;
        }
    }

    private void Crew(Campaign.Campaign c, Rect2 b)
    {
        float y = b.Position.Y + 6, x = b.Position.X, w = b.Size.X;
        Lbl($"Your officers: {(c.S.Crew.Count == 0 ? "none yet" : string.Join(", ", c.S.Crew.Select(o => $"{o.Name} ({o.Station})")))}", x, y + 16, w);
        y += 40;
        foreach (Officer o in c.S.OfficerPool.ToList())
        {
            Text(o.Name, x, y + 28, 20, C.Text, HorizontalAlignment.Left, w * 0.4f, 600);
            Text($"{o.Station} · {o.Skill}", x + w * 0.4f, y + 28, 17, C.Lilac, HorizontalAlignment.Left, w * 0.3f, 600);
            Officer oo = o;
            Button($"crew.{o.Name}", new Rect2(b.End.X - 170, y, 170, 44), $"Hire {o.Cost} cr", () => { F.Msg = c.Hire(oo) ?? $"{oo.Name} joins the crew."; F.Save(); }, dis: o.Cost > c.S.Credits);
            y += 54;
        }
    }

}
