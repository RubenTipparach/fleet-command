// PortPanel: the port, over the dimmed campaign map (game-flow design, "Campaign ports"; owner, 2026-11-05: "improve the
// "shop" expeirience as well, AND improve the loadout, crew recruitment and supplies shopping"). One screen with four tabs:
// - Shipyard: every hull the side builds, by its schematic, with a detail pane comparing it with your flagship.
// - Loadout: the chosen ship's schematic with its hardpoints on it; pick one and every system that fits is listed with
//   what changing to it costs or refunds.
// - Supplies: each supply as a row of boxes, carried out of what the ship holds, with -, +, Fill and Empty, and a key that
//   restocks the whole fleet at once.
// - Crew: the six stations and who serves at each, the picked officer's skill tree, and the officers on offer
//   (PortPanel.Crew.cs).
// Down the left, always: your fleet, each ship by its schematic with its hull, a Repair key, and a free berth leading to
// the shipyard. At another faction's port that deals with you, the one tab is the Market (selling). Every price shown is
// the Campaign's own quote, and every key pays through Campaign (src/Core/Campaign), so what the shop says is what it charges.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Campaign;
using FleetCommand.Data;
using FleetCommand.Game.Ui;
using Godot;

namespace FleetCommand.Game.Flow;

public sealed partial class PortPanel : FlowPanel
{
    private static readonly (string Id, string Label)[] OwnTabs = { ("yard", "Shipyard"), ("outfit", "Loadout"), ("supply", "Supplies"), ("crew", "Crew") };
    private static readonly (string Id, string Label)[] MarketTabs = { ("market", "Market") };
    /// <summary>Each station's icon from the kit's list (the icon guide's subjects).</summary>
    private static readonly Dictionary<string, string> StationIcon = new()
    {
        ["Helm"] = "helm", ["Tactical"] = "tactical", ["Engineering"] = "repair", ["Science"] = "science", ["Comms"] = "comms", ["Security"] = "transport",
    };
    private static readonly Dictionary<string, string> GroupIcon = new()
    {
        ["shields"] = "shields", ["primary"] = "weapons", ["heavy"] = "fire", ["hull"] = "ship", ["bridge"] = "sensors", ["engines"] = "engines",
    };

    private const float Top = 72;
    /// <summary>The grant each tab needs at another faction's port (Campaign.Allows).</summary>
    private static readonly Dictionary<string, string> GrantOfTab = new() { ["yard"] = "ships", ["outfit"] = "weapons", ["supply"] = "parts", ["crew"] = "officers" };

    protected override void DrawPanel()
    {
        if (Camp is not { } c || F.Port == null) return;
        // the map dimmed under the port, still there; the top bar stays live above it
        DrawRect(new Rect2(0, Top, Size.X, Size.Y - Top), new Color(0, 0, 0, 0.55f));
        Ui.Reg("port.block", new Rect2(0, Top, Size.X, Size.Y - Top));
        bool own = c.IsPort(c.At);
        // a friendly faction's port opens the tabs your standing with it has earned, beside its market
        var tabs = own ? OwnTabs : MarketTabs.Concat(OwnTabs.Where(t => c.Allows(GrantOfTab[t.Id]))).ToArray();
        if (!tabs.Any(t => t.Id == F.Port)) F.Port = tabs[0].Id;
        F.PortShip = Math.Clamp(F.PortShip, 0, c.S.Fleet.Count - 1);

        Rect2 b = Sheet(new Rect2(12, Top + 8, Size.X - 24, Size.Y - Top - 20), c.At.PortName ?? "Port");
        float tw = Math.Min(170 * tabs.Length, b.Size.X - 520);
        Tabs("port.tab", new Rect2(b.Position.X, b.Position.Y + 4, tw, 42), tabs, F.Port!, id => F.Port = id);
        Text($"{c.S.Credits:N0} cr", b.End.X - 140, b.Position.Y + 34, 26, C.Sky, HorizontalAlignment.Right, 220, 700);
        Button("port.close", new Rect2(b.End.X - 124, b.Position.Y + 2, 124, 46), "Close", () => F.Port = null);

        var body = new Rect2(b.Position.X, b.Position.Y + 62, b.Size.X, b.Size.Y - 62);
        float fw = Math.Min(300, body.Size.X * 0.24f);
        FleetColumn(c, new Rect2(body.Position, new Vector2(fw, body.Size.Y)), own);
        var main = new Rect2(body.Position.X + fw + 18, body.Position.Y, body.Size.X - fw - 18, body.Size.Y);
        switch (F.Port)
        {
            case "yard": Yard(c, main); break;
            case "outfit": Loadout(c, main); break;
            case "supply": Supplies(c, main); break;
            case "crew": Crew(c, main); break;
            case "market": Market(c, main); break;
        }
    }

    // ---- The fleet down the left -----------------------------------------------------------------------------------------

    private void FleetColumn(Campaign.Campaign c, Rect2 b, bool own)
    {
        Lbl($"Your fleet · {c.S.Fleet.Count} of {c.Data.Rules.FleetMax}", b.Position.X, b.Position.Y + 14, b.Size.X);
        float y = b.Position.Y + 26, h = Math.Min(128, (b.Size.Y - 30) / c.Data.Rules.FleetMax - 8);
        for (int i = 0; i < c.Data.Rules.FleetMax; i++)
        {
            var r = new Rect2(b.Position.X, y, b.Size.X, h);
            if (i < c.S.Fleet.Count)
            {
                CampShip s = c.S.Fleet[i];
                Hull hull = c.Data.Hulls[s.Hull];
                int idx = i;
                bool on = F.PortShip == i;
                HitState st = Ui.Reg($"pf.{s.Id}", r, _ => F.PortShip = idx);
                Card(r, on, st.Hot);
                float sw = h * 0.55f;
                ShipPic(hull, new Rect2(r.Position.X + 10, r.Position.Y + 8, sw, h - 16));
                float tx = r.Position.X + sw + 20, tww = r.End.X - tx - 10;
                Text(s.Name, tx, r.Position.Y + 28, 19, on ? C.Sky : C.Text, HorizontalAlignment.Left, tww, 700);
                Text(i == 0 ? $"{hull.Cls} · flagship" : $"{hull.Cls} · {s.Behaviour}", tx, r.Position.Y + 50, 15, C.Lilac, HorizontalAlignment.Left, tww, 600);
                Bar(tx, r.Position.Y + 56, tww, $"Hull {s.HullPct:0} %", s.HullPct / 100, s.HullPct < 50, s.HullPct >= 100);
                int cost = c.RepairCost(s);
                if (c.Allows("parts") && cost > 0)
                    Button($"pf.repair.{s.Id}", new Rect2(tx, r.End.Y - 34, tww, 28), $"Repair {cost:N0} cr", () => Say(c.Repair(s), $"The {s.Name} is repaired."), dis: cost > c.S.Credits);
            }
            else
            {
                HitState st = Ui.Reg($"pf.berth.{i}", r, c.Allows("ships") ? _ => F.Port = "yard" : null);
                Kit.Piece(this, "frame.inset", r, st.Hot ? 0.7f : 0.35f);
                Text("FREE BERTH", r.GetCenter().X, r.GetCenter().Y - 2, 17, C.Lilac, HorizontalAlignment.Center, r.Size.X - 20, 600, 1);
                if (c.Allows("ships")) Text("buy a ship at the shipyard", r.GetCenter().X, r.GetCenter().Y + 20, 14, C.Lilac, HorizontalAlignment.Center, r.Size.X - 20, 500);
            }
            y += h + 8;
        }
    }

    /// <summary>A card: the kit's inset frame, outlined in the skin's light when chosen (a full panel frame's sweep would
    /// crowd a card's text), dimmer when idle and not under the pointer.</summary>
    private void Card(Rect2 r, bool on, bool hot)
    {
        Kit.Piece(this, "frame.inset", r, on || hot ? 1 : 0.75f);
        if (on) Kit.Shape(this, r.Grow(-2), 10, C.Sky, 2.5f);
        else if (hot) Kit.Shape(this, r.Grow(-2), 10, Sk.Mark.Hover, 1.5f);
    }

    private static string? Key(Hull h) => h.Schematic == "" ? null : h.Schematic;

    private void Say(string? err, string ok)
    {
        F.Msg = err ?? ok;
        F.Save();
    }

    private void Schematic(Hull h, Rect2 box, bool frame = true)
    {
        if (frame) Kit.Piece(this, "frame.inset", box);
        ShipPic(h, box.Grow(-10));
    }

    /// <summary>A hull fitted in a box, nose up: its schematic, or the battle's stand-in by its class's shape while it has
    /// none (the systems display's, so a card's text never sits beside an empty space).</summary>
    private void ShipPic(Hull h, Rect2 box)
    {
        if (Schematics.DrawFit(this, Key(h), box, C.Signal, C.Sky)) return;
        float half = Math.Min(box.Size.Y * 0.46f, box.Size.X * 0.46f / 0.62f);
        SystemsDisplay.StandIn(this, X.Data.Classes[h.BattleClass].Shape, box.GetCenter(), half, C.Sky);
    }

    // ---- Shipyard ----------------------------------------------------------------------------------------------------

    /// <summary>A hull's figures for comparing: hull points, shield points, weapon mounts, top speed, hardpoints.</summary>
    private (double Hull, double Shields, int Mounts, double Speed, int Slots) Stats(Campaign.Campaign c, Hull h)
    {
        ShipClass k = X.Data.Classes[h.BattleClass];
        Tier t = c.Data.TierOf(h);
        return (k.HullHp, k.ShieldsHp.Sum(), k.Mounts.Count, k.SpeedMaxMS, t.Slots.Values.Sum());
    }

    private void Yard(Campaign.Campaign c, Rect2 b)
    {
        var hulls = c.Data.HullsOf(c.YardSide).Where(h => h.ForSale).ToList();
        if (F.YardHull == null || hulls.All(h => h.Id != F.YardHull)) F.YardHull = hulls[Math.Min(1, hulls.Count - 1)].Id;
        float lw = Math.Min(520, b.Size.X * 0.46f), ch = Math.Min(118, (b.Size.Y - 8) / hulls.Count - 8);
        float y = b.Position.Y;
        foreach (Hull h in hulls)
        {
            var r = new Rect2(b.Position.X, y, lw, ch);
            bool on = F.YardHull == h.Id;
            string id = h.Id;
            HitState st = Ui.Reg($"yard.pick.{h.Id}", r, _ => F.YardHull = id);
            Card(r, on, st.Hot);
            ShipPic(h, new Rect2(r.Position.X + 10, r.Position.Y + 6, ch * 0.62f, ch - 12));
            float tx = r.Position.X + ch * 0.62f + 22;
            Text(h.Name.ToUpperInvariant(), tx, r.Position.Y + 32, 22, on ? C.Sky : C.Text, HorizontalAlignment.Left, lw - 280, 700, 1);
            Text($"{h.Cls} · tier {h.Tier}", tx, r.Position.Y + 56, 16, C.Lilac, HorizontalAlignment.Left, lw - 280, 600);
            int price = c.HullPrice(h.Id);
            Text($"{price:N0} cr", r.End.X - 16, r.Position.Y + 34, 22, price <= c.S.Credits ? C.Sky : C.Alert, HorizontalAlignment.Right, 160, 700);
            y += ch + 8;
        }

        // the detail pane: the hull large, its figures beside your flagship's, its stock fit, and Buy
        Hull sel = c.Data.Hulls[F.YardHull!];
        var d = new Rect2(b.Position.X + lw + 18, b.Position.Y, b.Size.X - lw - 18, b.Size.Y);
        float sw = Math.Min(d.Size.X * 0.42f, 300);
        Schematic(sel, new Rect2(d.Position.X, d.Position.Y, sw, d.Size.Y - 70));
        float x = d.Position.X + sw + 20, w = d.End.X - x, yy = d.Position.Y + 30;
        Text(sel.Name.ToUpperInvariant(), x, yy, 34, C.Sky, HorizontalAlignment.Left, w, 700, 2); yy += 28;
        Text($"{sel.Cls} · tier {sel.Tier}", x, yy, 18, C.Lilac, HorizontalAlignment.Left, w, 600); yy += 22;
        Hull flag = c.Data.Hulls[c.S.Fleet[0].Hull];
        var a = Stats(c, sel);
        var f = Stats(c, flag);
        Lbl($"Against your flagship ({flag.Name})", x, yy + 16, w); yy += 26;
        double maxHull = c.Data.Hulls.Values.Where(h => h.Side == c.S.Side).Max(h => X.Data.Classes[h.BattleClass].HullHp);
        double maxSh = c.Data.Hulls.Values.Where(h => h.Side == c.S.Side).Max(h => X.Data.Classes[h.BattleClass].ShieldsHp.Sum());
        double maxSp = c.Data.Hulls.Values.Where(h => h.Side == c.S.Side).Max(h => X.Data.Classes[h.BattleClass].SpeedMaxMS);
        yy = Compare(x, yy, w, "Hull", a.Hull, f.Hull, maxHull, "0");
        yy = Compare(x, yy, w, "Shields", a.Shields, f.Shields, maxSh, "0");
        yy = Compare(x, yy, w, "Weapon mounts", a.Mounts, f.Mounts, 12, "0");
        yy = Compare(x, yy, w, "Top speed, m/s", a.Speed, f.Speed, maxSp, "0");
        yy = Compare(x, yy, w, "Hardpoints", a.Slots, f.Slots, 17, "0");
        yy += 8;
        Lbl("Stock fit", x, yy + 14, w); yy += 26;
        Tier t = c.Data.TierOf(sel);
        foreach (var (gid, gname) in c.Data.Groups)
        {
            if (t.Slots[gid] == 0) continue;
            Text($"{gname}: {t.Slots[gid]} x {c.Data.Systems[c.Data.Stock[gid]].Name}", x, yy + 14, 16, C.Text, HorizontalAlignment.Left, w, 500);
            yy += 22;
        }
        int price2 = c.HullPrice(sel.Id);
        string? why = c.S.Fleet.Count >= c.Data.Rules.FleetMax ? $"Your fleet is full ({c.Data.Rules.FleetMax} ships): sell one first."
            : c.PrestigeShort(sel.Id) is { } ps ? ps
            : price2 > c.S.Credits ? $"You need {price2 - c.S.Credits:N0} cr more." : null;
        if (why != null) Text(why, x, d.End.Y - 66, 17, C.Caution, HorizontalAlignment.Left, w, 600);
        string hid = sel.Id;
        Button("yard.buy", new Rect2(d.End.X - 300, d.End.Y - 52, 300, 50), $"Buy for {price2:N0} cr", () =>
        {
            string? err = c.Buy(hid);
            Say(err, $"The {c.S.Fleet[^1].Name} joins the fleet. Fit her out in the Loadout.");
            if (err == null) { F.PortShip = c.S.Fleet.Count - 1; F.Port = "outfit"; }
        }, dis: why != null);
    }

    /// <summary>One figure as two bars: this hull's, and your flagship's beneath it, with the difference.</summary>
    private float Compare(float x, float y, float w, string label, double v, double mine, double max, string fmt)
    {
        float lw = 150, bw = w - lw - 70;
        Lbl(label, x, y + 16, lw - 8);
        Kit.Piece(this, "bar.track", new Rect2(x + lw, y + 4, bw, 14));
        float f1 = (float)Math.Clamp(v / Math.Max(1, max), 0, 1), f2 = (float)Math.Clamp(mine / Math.Max(1, max), 0, 1);
        if (f1 > 0) Kit.Piece(this, "bar.fill", new Rect2(x + lw + 2, y + 6, Math.Max(8, (bw - 4) * f1), 10));
        DrawRect(new Rect2(x + lw + 2, y + 21, Math.Max(2, (bw - 4) * f2), 3), new Color(C.Lilac, 0.8f));
        double dlt = v - mine;
        string ds = dlt == 0 ? "same" : (dlt > 0 ? "+" : "") + dlt.ToString(fmt);
        Text(ds, x + w, y + 17, 16, dlt > 0 ? C.Accord : dlt < 0 ? C.Alert : C.Lilac, HorizontalAlignment.Right, 64, 700);
        return y + 30;
    }

    // ---- Loadout ------------------------------------------------------------------------------------------------------

    /// <summary>Where each hardpoint sits on the hull, in 0..1 across its beam and down its length, and which way it faces:
    /// the campaign mockup's slotLayout (game-flow.html), the one hardpoint picture both draw.</summary>
    private static List<(float X, float Y)> Layout(string g, int n)
    {
        IEnumerable<(float, float)> Spread(float y0, float y1, float[] xs) =>
            Enumerable.Range(0, n).Select(i => { float t = n == 1 ? 0.5f : i / (float)(n - 1); return (xs[i % xs.Length], y0 + (y1 - y0) * t); });
        return g switch
        {
            "shields" => Spread(0.42f, 0.58f, new[] { 0.5f }).Select((p, i) => (p.Item1 + (i - (n - 1) / 2f) * 0.12f, p.Item2)).ToList(),
            "primary" => Spread(0.22f, 0.5f, new[] { 0.32f, 0.68f }).ToList(),
            "heavy" => Spread(0.12f, 0.2f, new[] { 0.5f, 0.42f, 0.58f }).ToList(),
            "hull" => Spread(0.5f, 0.7f, new[] { 0.42f, 0.58f }).ToList(),
            "bridge" => Spread(0.3f, 0.36f, new[] { 0.5f, 0.56f }).ToList(),
            _ => Spread(0.86f, 0.86f, new[] { 0.32f, 0.68f }).ToList(),
        };
    }

    private void Loadout(Campaign.Campaign c, Rect2 b)
    {
        CampShip s = c.S.Fleet[F.PortShip];
        Hull hull = c.Data.Hulls[s.Hull];
        if (!s.Fit.TryGetValue(F.LoGroup, out var cur) || cur.Count == 0) { F.LoGroup = s.Fit.First(kv => kv.Value.Count > 0).Key; F.LoSlot = 0; }
        F.LoSlot = Math.Clamp(F.LoSlot, 0, s.Fit[F.LoGroup].Count - 1);

        // the hull with its hardpoints on it: each a key in its group's icon; empty ones crossed; the picked one ringed
        float sw = Math.Min(b.Size.X * 0.3f, 330);
        var box = new Rect2(b.Position.X, b.Position.Y, sw, b.Size.Y - 60);
        Kit.Piece(this, "frame.inset", box);
        string? key = Key(hull);
        Rect2 inner = box.Grow(-14);
        // a stand-in (no schematic) is 1.1 half-lengths across, so it is held to the box's width too
        float half = Schematics.HullRect(key, inner.GetCenter(), 1) != null ? Schematics.FitHalf(key, inner) : Math.Min(inner.Size.Y * 0.46f, inner.Size.X * 0.46f / 0.62f);
        if (!Schematics.Draw(this, key, inner.GetCenter(), half, C.Signal, C.Sky, 0.75f)) SystemsDisplay.StandIn(this, X.Data.Classes[hull.BattleClass].Shape, inner.GetCenter(), half, new Color(C.Sky, 0.75f));
        Rect2 hr = Schematics.HullRect(key, inner.GetCenter(), half) ?? new Rect2(inner.GetCenter().X - half * 0.3f, inner.GetCenter().Y - half, half * 0.6f, half * 2);
        foreach (var (gid, _) in c.Data.Groups)
        {
            var slots = s.Fit[gid];
            var lay = Layout(gid, slots.Count);
            for (int i = 0; i < slots.Count; i++)
            {
                var (px, py) = lay[i];
                // across: spread over at least a narrow hull's width, so two mounts never sit on top of each other
                float bx = Math.Max(hr.Size.X, hr.Size.Y * 0.36f);
                var cpos = new Vector2(hr.GetCenter().X + (px - 0.5f) * bx, hr.Position.Y + py * hr.Size.Y);
                var r = new Rect2(cpos - new Vector2(15, 15), new Vector2(30, 30));
                bool on = gid == F.LoGroup && i == F.LoSlot;
                string g = gid;
                int ii = i;
                HitState st = Ui.Reg($"lo.hp.{gid}.{i}", r, _ => { F.LoGroup = g; F.LoSlot = ii; },
                    tip: new Tip($"{c.Data.Groups.First(x => x.Id == g).Name} {i + 1}", new (string, string, Color?)[] { ("Fitted", slots[i] != null ? c.Data.Systems[slots[i]!].Name : "empty", null) }));
                Kit.Shape(this, r, 6, slots[i] == null ? C.Deep : gid == F.LoGroup ? C.Sky : C.Signal);
                Kit.Icon(this, GroupIcon[gid], r.Grow(-5), Sk.InkOn(slots[i] == null ? C.Deep : gid == F.LoGroup ? C.Sky : C.Signal));
                if (slots[i] == null) DrawLine(r.Position + new Vector2(4, 4), r.End - new Vector2(4, 4), C.Alert, 2.5f);
                if (on || st.Hot) Kit.Shape(this, r.Grow(5), 9, on ? C.Caution : Sk.Mark.Hover, 2.5f);
            }
        }
        Text("BOW", box.GetCenter().X, box.Position.Y + 22, 14, C.Lilac, HorizontalAlignment.Center, 60, 600, 2);

        // the hardpoints listed by group
        float lx = box.End.X + 18, lw = Math.Min(330, (b.End.X - lx) * 0.45f), y = b.Position.Y;
        foreach (var (gid, gname) in c.Data.Groups)
        {
            var slots = s.Fit[gid];
            if (slots.Count == 0) continue;
            Lbl(gname, lx, y + 14, lw);
            y += 20;
            for (int i = 0; i < slots.Count; i++)
            {
                string g = gid;
                int ii = i;
                bool on = gid == F.LoGroup && i == F.LoSlot;
                Button($"lo.slot.{gid}.{i}", new Rect2(lx, y, lw, 32), slots[i] != null ? c.Data.Systems[slots[i]!].Name : "Empty", () => { F.LoGroup = g; F.LoSlot = ii; }, on, center: false);
                y += 36;
            }
            y += 4;
        }

        // what fits the picked hardpoint: fitting is free, from the stores (owner, 2026-11-06: "fitting stuff on a ship
        // should be free, have the store below"); the store below buys systems into the stores and sells them back
        float cx = lx + lw + 18, cw = b.End.X - cx;
        string? fitted = s.Fit[F.LoGroup][F.LoSlot];
        string gn = c.Data.Groups.First(x => x.Id == F.LoGroup).Name;
        var options = c.Data.Systems.Values.Where(x => x.Group == F.LoGroup).OrderBy(x => x.Cost).ToList();
        float top = b.Position.Y + 24, bottom = b.End.Y - (F.PortShip > 0 ? 120 : 60);
        float avail = bottom - top - 40 - 30 - (fitted != null ? 44 : 0);
        float oh = Math.Clamp(avail * 0.6f / Math.Max(1, options.Count) - 6, 44, 84), sh = Math.Clamp(avail * 0.4f / Math.Max(1, options.Count) - 6, 36, 48);
        Lbl($"{gn} {F.LoSlot + 1}: fit from the stores", cx, b.Position.Y + 14, cw);
        float cy = top;
        foreach (ShipSystem o in options)
        {
            var r = new Rect2(cx, cy, cw, oh);
            bool isOn = o.Id == fitted;
            int held = c.InStores(o.Id);
            Card(r, isOn, false);
            Text(o.Name, r.Position.X + 14, r.Position.Y + 24, 19, isOn ? C.Sky : C.Text, HorizontalAlignment.Left, cw - 290, 700);
            if (oh >= 60) Para(o.Desc, r.Position.X + 14, r.Position.Y + 46, cw - 350, 14, C.Lilac, 2);
            Text(held > 0 ? $"{held} in stores" : "none in stores", r.End.X - 186, r.Position.Y + oh / 2 + 6, 15, held > 0 ? C.Accord : C.Lilac, HorizontalAlignment.Right, 150, 600, 1);
            string sid = o.Id;
            if (isOn) Text("FITTED", r.End.X - 14, r.Position.Y + oh / 2 + 6, 17, C.Accord, HorizontalAlignment.Right, 160, 700, 1);
            else Button($"lo.fit.{o.Id}", new Rect2(r.End.X - 170, r.Position.Y + (oh - 40) / 2, 160, 40), "Fit",
                () => Say(c.Refit(s, F.LoGroup, F.LoSlot, sid), $"Fitted: {c.Data.Systems[sid].Name}."), dis: held == 0,
                tip: held == 0 ? new Tip(o.Name, new (string, string, Color?)[] { ("In stores", "none: buy one below", null) }) : null);
            cy += oh + 6;
        }
        if (fitted != null)
        {
            Button("lo.remove", new Rect2(cx, cy, Math.Min(cw, 300), 38), "Remove to stores", () => Say(c.Refit(s, F.LoGroup, F.LoSlot, null), "Removed to the stores."));
            cy += 44;
        }

        // the store: buy into the stores, sell from them
        Lbl($"Store: {gn.ToLowerInvariant()} systems", cx, cy + 16, cw);
        cy += 26;
        bool port = c.Allows("weapons");
        foreach (ShipSystem o in options)
        {
            var r = new Rect2(cx, cy, cw, sh);
            int held = c.InStores(o.Id), price = c.SystemPrice(o.Id), sale = c.SystemSale(o.Id);
            Kit.Piece(this, "frame.inset", r);
            Text(o.Name, r.Position.X + 14, r.Position.Y + sh / 2 + 6, 17, C.Text, HorizontalAlignment.Left, cw - 400, 600, 1);
            Text($"{held} held", r.End.X - 340, r.Position.Y + sh / 2 + 6, 15, held > 0 ? C.Accord : C.Lilac, HorizontalAlignment.Right, 80, 600, 1);
            string sid = o.Id;
            Button($"lo.buy.{o.Id}", new Rect2(r.End.X - 252, r.Position.Y + (sh - 32) / 2, 130, 32), $"Buy {price:N0} cr",
                () => Say(c.BuySystem(sid), $"{c.Data.Systems[sid].Name} bought into the stores."), dis: !port || price > c.S.Credits);
            Button($"lo.sell.{o.Id}", new Rect2(r.End.X - 116, r.Position.Y + (sh - 32) / 2, 110, 32), $"Sell +{sale:N0}",
                () => Say(c.SellSystem(sid), $"{c.Data.Systems[sid].Name} sold."), dis: !port || held == 0);
            cy += sh + 4;
        }

        // the ship's worth, its empty hardpoints, and a wingman's standing orders
        float by = b.End.Y - 48;
        Text($"{s.Name} · worth {c.Points(s) * c.Data.Rules.CreditRate:N0} cr", b.Position.X, by + 28, 18, C.Text, HorizontalAlignment.Left, sw, 600);
        int empty = c.EmptyHardpoints(s).Count;
        if (empty > 0)
        {
            int sq = c.StockQuote(s);
            Button("lo.stock", new Rect2(lx, by, lw, 44), sq > 0 ? $"Fill {empty} empty: {sq:N0} cr" : $"Fill {empty} empty from stores", () => Say(c.FitStock(s), "Empty hardpoints fitted with stock systems."), dis: sq > c.S.Credits);
        }
        if (F.PortShip > 0)
        {
            Lbl("Orders", cx, by - 8, cw);
            float bw = (cw - 8 * (c.Data.Behaviours.Count - 1)) / c.Data.Behaviours.Count;
            for (int i = 0; i < c.Data.Behaviours.Count; i++)
            {
                string be = c.Data.Behaviours[i];
                Tab($"lo.beh.{i}", new Rect2(cx + i * (bw + 8), by, bw, 44), be, () => { c.SetBehaviour(s, be); F.Save(); }, s.Behaviour == be);
            }
        }
    }

    // ---- Supplies ----------------------------------------------------------------------------------------------------

    private void Supplies(Campaign.Campaign c, Rect2 b)
    {
        CampShip s = c.S.Fleet[F.PortShip];
        Text($"{s.Name.ToUpperInvariant()} · SUPPLIES", b.Position.X, b.Position.Y + 24, 24, C.Sky, HorizontalAlignment.Left, b.Size.X, 700, 1);
        // a friendly faction's port sells the supplies your standing has earned: repair kits first, the rest later
        var sold = c.Data.Supplies.Where(u => c.Allows(Campaign.Campaign.GrantOfSupply(u.Id))).ToList();
        float y = b.Position.Y + 48, rh = Math.Min(110, (b.Size.Y - 140) / c.Data.Supplies.Count - 8), x = b.Position.X, w = b.Size.X;
        foreach (Supply u in sold)
        {
            var r = new Rect2(x, y, w, rh);
            Kit.Piece(this, "frame.inset", r);
            int max = c.SupplyMax(s, u.Id), now = s.Supplies[u.Id];
            float lx = r.Position.X + 16, lw = w * 0.3f;
            Text(u.Name, lx, r.Position.Y + 28, 20, C.Text, HorizontalAlignment.Left, lw, 700);
            Para(u.Desc, lx, r.Position.Y + 52, lw, 14, C.Lilac, 2);
            float bx = lx + lw + 16, bw = w * 0.32f;
            if (max > 0)
            {
                // discrete boxes, one a unit (CLAUDE.md section 7); a tap sets the count, paying or refunding the difference
                string id = u.Id;
                CellRow($"sup.{u.Id}.cells", new Rect2(bx, r.Position.Y + 18, bw, 24), max, now,
                    n => Say(c.Resupply(s, id, n - s.Supplies[id]), ""), null);
                Text($"{now} of {max} · {u.Cost * c.Data.Rules.CreditRate} cr each", bx, r.Position.Y + 66, 15, C.Lilac, HorizontalAlignment.Left, bw, 600);
            }
            else Text(u.PerFit > 0 ? $"Needs a {c.Data.Systems[u.Fit].Name} fitted" : "This ship carries none", bx, r.Position.Y + 38, 16, C.Lilac, HorizontalAlignment.Left, bw, 600);
            float kx = bx + bw + 16, kw = (r.End.X - kx - 16 - 3 * 8) / 4;
            string sid = u.Id;
            int fill = c.SupplyQuote(s, u.Id, max), empty = c.SupplyQuote(s, u.Id, -max);
            float ky = r.Position.Y + (rh - 40) / 2;
            Button($"sup.{u.Id}.minus", new Rect2(kx, ky, kw, 40), "-1", () => Say(c.Resupply(s, sid, -1), ""), dis: now == 0);
            Button($"sup.{u.Id}.plus", new Rect2(kx + kw + 8, ky, kw, 40), "+1", () => Say(c.Resupply(s, sid, 1), ""), dis: now >= max || c.SupplyQuote(s, sid, 1) > c.S.Credits);
            Button($"sup.{u.Id}.fill", new Rect2(kx + 2 * (kw + 8), ky, kw, 40), fill > 0 ? $"Fill {fill}" : "Full", () => Say(c.Resupply(s, sid, max), ""), dis: fill == 0 || fill > c.S.Credits,
                tip: new Tip($"Fill {u.Name}", new (string, string, Color?)[] { ("Costs", $"{fill:N0} cr", null) }));
            Button($"sup.{u.Id}.empty", new Rect2(kx + 3 * (kw + 8), ky, kw, 40), "Empty", () => Say(c.Resupply(s, sid, -max), ""), dis: now == 0,
                tip: new Tip($"Sell back {u.Name}", new (string, string, Color?)[] { ("Returns", $"{-empty:N0} cr", null) }));
            y += rh + 8;
        }
        int all = c.RestockQuote();
        Text(all > 0 ? $"Restocking every ship of the fleet to what it carries costs {all:N0} cr." : "Every ship is fully stocked.", b.Position.X, b.End.Y - 20, 17, C.Lilac, HorizontalAlignment.Left, b.Size.X - 340, 600);
        Button("sup.restock", new Rect2(b.End.X - 320, b.End.Y - 50, 320, 48), all > 0 ? $"Restock fleet: {all:N0} cr" : "Fleet stocked", () => Say(c.Restock(), "The fleet is restocked."), dis: all == 0 || all > c.S.Credits);
    }

    // ---- Market (another faction's port) ---------------------------------------------------------------------------------

    private void Market(Campaign.Campaign c, Rect2 b)
    {
        string buyer = c.At.Owner;
        CampFaction f = c.Data.Factions[buyer];
        Para($"{f.Name} buyers. They pay {(buyer == "vos" ? "less than a shipyard, and dealing with them raises your standing with them" : "a quarter of a ship's price, more for a rival's hull")}.",
            b.Position.X, b.Position.Y + 20, b.Size.X, 18, C.Lilac);
        // what their ports give you at your standing, and the next grant
        int st = c.Standing(buyer);
        var grants = c.GrantsOf(buyer);
        var next = c.Data.Rules.Privileges.FirstOrDefault(p => !grants.Contains(p.Grant));
        Text($"Standing {st}: " + (grants.Count == 0 ? "they sell you nothing yet" : "they give you " + string.Join(", ", grants))
            + (next.Grant != null ? $" · {next.Grant} at {next.Standing}" : ""), b.Position.X, b.Position.Y + 52, 17, C.Sky, HorizontalAlignment.Left, b.Size.X, 600);
        float y = b.Position.Y + 72;
        foreach (CampShip s in c.S.Fleet.ToList())
        {
            var r = new Rect2(b.Position.X, y, b.Size.X, 90);
            Kit.Piece(this, "frame.inset", r);
            Hull h = c.Data.Hulls[s.Hull];
            ShipPic(h, new Rect2(r.Position.X + 10, r.Position.Y + 6, 60, 78));
            Text(s.Name, r.Position.X + 90, r.Position.Y + 34, 20, C.Text, HorizontalAlignment.Left, 400, 700);
            Text($"{c.Data.Factions[s.Maker].Short}-built {h.Cls.ToLowerInvariant()} · hull {s.HullPct:0} %", r.Position.X + 90, r.Position.Y + 60, 16, C.Lilac, HorizontalAlignment.Left, 400, 600);
            int value = c.SaleQuote(s, buyer);
            CampShip ss = s;
            Button($"mk.sell.{s.Id}", new Rect2(r.End.X - 220, r.Position.Y + 22, 210, 46), $"Sell {value:N0} cr", () => Say(c.Sell(ss), $"The {ss.Name} is sold."), dis: c.S.Fleet.Count <= 1);
            y += 98;
        }
    }
}
