// MapHud: the campaign map's panels, over the 3D map, as the campaign mockup v15 lays them out (its map screen). The top
// bar hangs from the top of the screen: Menu, the captain, credits, prestige, turn, moves, the Missions key with the log of
// accepted missions, standing with each faction, and the fleet's ships with their hull bars. The sector panel names the
// picked sector (or your position) and what is in it, the route there with Move here, each mission's row with its Briefing
// key, and the port's keys (Shipyard, Loadout, Supplies, Crew, and Market at a port that deals with you). The zoom and Fleet
// keys sit bottom left, End turn bottom right. The briefing shows a mission's file as the captain reads it; the port is
// its own panel (PortPanel.cs). Hovering the map names whose space the pointer is over. Every action goes to GameFlow or
// Campaign; nothing here decides anything.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Campaign;
using FleetCommand.Game.Ui;
using Godot;

namespace FleetCommand.Game.Flow;

public sealed partial class MapHud : FlowPanel
{
    private bool Narrow => Size.X < 1100;

    protected override void DrawPanel()
    {
        Campaign.Campaign? c = Camp;
        if (c == null) return;
        TopBar(c);
        Keys(c);
        // the port is its own panel over the map (PortPanel.cs); a briefing takes the sector panel's place
        if (F.Port == null && F.Brief != null) Briefing(c, F.Brief);
        if (F.Port == null && F.Brief == null) SectorPanel(c, (F.Sel ?? (c.S.AtC, c.S.AtR)).C, (F.Sel ?? (c.S.AtC, c.S.AtR)).R);
        if (LogOpen && F.Port == null) MissionLog(c);
        HoverName(c);
        if (F.Msg.Length > 0)
        {
            float w = Math.Min(Size.X - 600, X.Fonts.Width(F.Msg, 19) + 40);
            var r = new Rect2(360, Size.Y - 62, w, 38);
            Shapes.RoundRect(this, r, new Color(Sk.R.Sheet, 0.92f), 12);
            Text(F.Msg, r.Position.X + 20, r.Position.Y + 26, 19, C.Caution, HorizontalAlignment.Left, w - 30);
        }
    }

    /// <summary>The mission log is open (the top bar's Missions key).</summary>
    public bool LogOpen;

    // the top bar, as the mockup's: Menu, the captain, credits, prestige, turn, moves, the Missions key (the log of accepted
    // missions), standing with each other faction, and each ship of the fleet with its hull bar
    private void TopBar(Campaign.Campaign c)
    {
        // a panel hanging from the top of the screen (owner, 2026-10-30: "looks more like a panel hanging from top of the
        // screen"), on the skin's own ground so the stats never sit on the map
        var r = new Rect2(0, 0, Size.X, 66);
        Ui.Reg("top.block", r);
        Shapes.RoundRect(this, new Rect2(4, 0, Size.X - 8, 64), Sk.R.Sheet, 0, 0, 22, 22);
        Kit.Piece(this, "frame.panel", new Rect2(0, -10, Size.X, 76));
        float x = 22;
        Button("top.menu", new Rect2(x, 12, 104, 40), "Menu", X.Act.Menu, X.State.MenuOpen, center: true);
        x += 124;
        void Stat(string label, string value, float max = 160)
        {
            Lbl(label, x, 26, max, null, HorizontalAlignment.Left, 13);
            Text(value, x, 50, 20, C.Sky, HorizontalAlignment.Left, max, 600);
            x += Math.Min(max, Math.Max(X.Fonts.Width(label.ToUpperInvariant(), 13, 600, 1), X.Fonts.Width(value, 20))) + 22;
        }
        Stat("Captain", c.S.Captain, 190);
        Stat("Credits", $"{c.S.Credits:N0} cr");
        Stat("Prestige", c.S.Prestige.ToString());
        Stat("Turn", c.S.Turn.ToString());
        Stat("Moves", $"{c.S.Mp} / {c.Data.Rules.MovePoints}");
        int active = c.S.Missions.Count(m => c.S.Active.Contains(m.Id));
        Button("top.missions", new Rect2(x, 12, 150, 40), $"Missions {active}", () => LogOpen = !LogOpen, on: LogOpen, tip: new Tip("Mission log", new (string, string, Color?)[] { ("Shows", "the missions you have taken, and recent news", null) }));
        x += 168;
        if (Narrow) return;
        Lbl("Standing with", x, 26, 200, null, HorizontalAlignment.Left, 13);
        float cx = x;
        foreach (string f in new[] { "concord", "krai", "ssarith", "vos" }.Where(f => f != c.S.Side && c.S.Standing.ContainsKey(f)))
        {
            int st = c.Standing(f);
            string t = $"{c.Data.Factions[f].Short} {(st > 0 ? "+" : "")}{st}";
            float w = X.Fonts.Width(t, 17, 600) + 30;
            var chip = new Rect2(cx, 32, w, 26);
            Ui.Reg($"top.st.{f}", chip, null, null, new Tip(c.Data.Factions[f].Name, new (string, string, Color?)[] { ("Standing", st.ToString(), null), ("Ports", st >= 0 ? "open to you" : "closed to you", st >= 0 ? C.Accord : C.Alert) }));
            Shapes.RoundRect(this, chip, new Color(Sk.R.Plate, 0.9f), 6);
            DrawRect(new Rect2(chip.Position + new Vector2(8, 9), new Vector2(9, 9)), FacColor(f));
            Text(t, chip.Position.X + 22, chip.Position.Y + 19, 17, C.Text, HorizontalAlignment.Left, w, 600);
            cx += w + 6;
        }
        x = Math.Max(cx, x + 120) + 16;
        // the fleet: each ship's name and hull; a tap glides the view to the fleet
        foreach (CampShip sh in c.S.Fleet)
        {
            float w = Math.Min(200, X.Fonts.Width(sh.Name, 15, 600) + 70);
            if (x + w > Size.X - 10) break;
            var chip = new Rect2(x, 16, w, 34);
            Ui.Reg($"top.ship.{sh.Id}", chip, _ => MapOf()?.CentreOnFleet(), null, new Tip(sh.Name, new (string, string, Color?)[] { ("Hull", $"{sh.HullPct:0} %", sh.HullPct < 50 ? C.Caution : null), ("Tap", "go to the fleet", null) }));
            Shapes.RoundRect(this, chip, new Color(Sk.R.Plate, 0.9f), 6);
            Text(sh.Name, chip.Position.X + 8, chip.Position.Y + 22, 15, C.Text, HorizontalAlignment.Left, w - 64, 600);
            var bar = new Rect2(chip.End.X - 52, chip.Position.Y + 13, 44, 8);
            Shapes.RoundRect(this, bar, C.Deep, 4);
            Shapes.RoundRect(this, new Rect2(bar.Position, new Vector2(bar.Size.X * (float)Math.Clamp(sh.HullPct / 100, 0, 1), bar.Size.Y)), sh.HullPct < 50 ? C.Caution : C.Sky, 4);
            x += w + 6;
        }
    }

    // the zoom keys and the Fleet key (bottom left) and End turn (bottom right), as the mockup's map keys
    private void Keys(Campaign.Campaign c)
    {
        var box = new Rect2(14, Size.Y - 84, 330, 70);
        Ui.Reg("keys.block", box);
        Kit.Piece(this, "frame.panel", box);
        float y = box.Position.Y + 14, kx = box.Position.X + Sk.Pad.L + 4;
        Button("keys.in", new Rect2(kx, y, 74, 42), "+", () => MapOf()?.Map?.Zoom(0.7f), tip: new Tip("Zoom in"));
        Button("keys.out", new Rect2(kx + 82, y, 74, 42), "-", () => MapOf()?.Map?.Zoom(1.4f), tip: new Tip("Zoom out"));
        Button("keys.fleet", new Rect2(kx + 164, y, box.End.X - Sk.Pad.R - 4 - (kx + 164), 42), "Fleet", () => { F.SelectHex((c.S.AtC, c.S.AtR)); MapOf()?.CentreOnFleet(); }, tip: new Tip("Go to your fleet"));
        Button("top.end", new Rect2(Size.X - 196, Size.Y - 66, 180, 50), "End turn", F.EndTurn, on: true,
            tip: new Tip("End turn", new (string, string, Color?)[] { ("Then", "the factions move, missions change", null) }));
    }

    // the mission log (the Missions key): the accepted missions, each a key that glides to it and opens its briefing, and
    // the latest news (the mockup's missionLog)
    private void MissionLog(Campaign.Campaign c)
    {
        var act = c.S.Missions.Where(m => c.S.Active.Contains(m.Id)).ToList();
        var news = c.S.Log.TakeLast(4).Reverse().ToList();
        float h = 70 + Math.Max(1, act.Count) * 64 + (news.Count > 0 ? 40 + news.Count * 26 : 0);
        Rect2 b = Sheet(new Rect2(16, 80, Math.Min(420, Size.X - 32), h), "Mission log");
        float x = b.Position.X, y = b.Position.Y + 14, w = b.Size.X;
        if (act.Count == 0) { Text("No missions taken. Pick a banner on the map.", x, y + 20, 17, C.Lilac, HorizontalAlignment.Left, w, 600); y += 64; }
        foreach (Offer m in act)
        {
            Offer mm = m;
            var r = new Rect2(x, y, w, 56);
            HitState hs = Ui.Reg($"log.{m.Id}", r, _ => { MapOf()?.Map?.GlideTo(mm.C, mm.R); F.SelectHex((mm.C, mm.R)); F.OpenBrief(mm); });
            Kit.Piece(this, hs.Hot ? "frame.panel" : "frame.inset", r);
            Text(c.ScriptOf(m).Name, x + 12, y + 23, 18, C.Text, HorizontalAlignment.Left, w - 120, 700);
            Text($"{HexGrid.Dist(c.S.AtC, c.S.AtR, m.C, m.R)} sectors", r.End.X - 12, y + 23, 16, C.Sky, HorizontalAlignment.Right, 110, 600);
            Text(c.Objective(m), x + 12, y + 45, 15, C.Lilac, HorizontalAlignment.Left, w - 24, 500);
            y += 64;
        }
        if (news.Count == 0) return;
        Lbl("Recent", x, y + 22, w);
        y += 34;
        foreach (string n in news) { Text(n, x, y + 16, 15, C.Lilac, HorizontalAlignment.Left, w, 500); y += 26; }
    }

    private MapScreen? MapOf() => GetParent() as MapScreen;

    private Rect2 SidePanel(float h)
    {
        float w = Narrow ? Size.X - 20 : 480;
        return Narrow ? new Rect2(10, Size.Y - h - 90, w, h) : new Rect2(Size.X - w - 16, 80, w, h);
    }

    // the sector panel, as the mockup's: your position or the selected sector, its name, holder, terrain and move cost, the
    // fleets in it, the route there with Move here, each mission here with its Briefing key, and the port's keys
    private void SectorPanel(Campaign.Campaign c, int col, int row)
    {
        Sector h = c.Hex(col, row);
        var missions = c.S.Missions.Where(m => m.C == col && m.R == row).ToList();
        var groups = c.S.Groups.Where(g => g.C == col && g.R == row).ToList();
        bool here = c.S.AtC == col && c.S.AtR == row;
        Sector at = c.At;
        bool port = c.IsPort(at), market = c.IsMarket(at);
        Terrain t = c.TerrainOf(h);
        float hgt = 286 + missions.Count * 84 + (groups.Count > 0 ? 28 : 0) + (!here && !t.Impassable ? 56 : 0) + (market ? 54 : 0);
        Rect2 b = Sheet(SidePanel(hgt), here ? "Your position" : "Selected sector");
        float x = b.Position.X, y = b.Position.Y + 30, w = b.Size.X;
        Text((h.PortName ?? t.Name).ToUpperInvariant(), x, y + 6, 28, C.Text, HorizontalAlignment.Left, w, 700, 1);
        y += 34;
        DrawRect(new Rect2(x, y - 11, 11, 11), FacColor(h.Owner));
        Text($"{FacName(h.Owner)} · {t.Name}{(t.Impassable ? "" : $" · move cost {t.Cost}")}", x + 18, y, 18, C.Sky, HorizontalAlignment.Left, w - 18, 600);
        y += 28;
        if (groups.Count > 0)
        {
            Text(string.Join(", ", groups.Select(g => $"{g.Name} ({g.Size} {(g.Size > 1 ? "ships" : "ship")})")), x, y, 17, C.Text, HorizontalAlignment.Left, w, 600);
            y += 28;
        }
        if (!here && !t.Impassable)
        {
            if (F.Path is { } path)
            {
                string how = path.Turns > 1 ? $"{path.Cost} moves · {path.Turns} turns" : $"Path cost {path.Cost}";
                Text(how, x, y + 26, 18, C.Lilac, HorizontalAlignment.Left, w - 180, 600);
                Button("sec.move", new Rect2(b.End.X - 170, y, 170, 44), "Move here", F.MoveHere, on: true, dis: path.Legs[0].Count == 0 && path.Turns == 1);
            }
            else Text("No route there.", x, y + 26, 18, C.Alert, HorizontalAlignment.Left, w, 600);
            y += 56;
        }
        foreach (Offer m in missions)
        {
            // a mission here is a key that opens its briefing (owner: "make this more obvious its a button")
            MissionScript def = c.ScriptOf(m);
            var rr = new Rect2(x, y, w, 74);
            Kit.Piece(this, "frame.inset", rr);
            Color ink = def.Story ? new Color("#8fd0ff") : c.S.Active.Contains(m.Id) ? new Color("#7dffa0") : new Color("#ffb547");
            Shapes.RoundRect(this, new Rect2(rr.Position + new Vector2(10, 14), new Vector2(40, 46)), ink, 6);
            Text(def.Glyph, rr.Position.X + 30, rr.Position.Y + 47, 28, new Color(0.05f, 0.05f, 0.08f), HorizontalAlignment.Center, 40, 700);
            Text(def.Name.ToUpperInvariant(), rr.Position.X + 62, rr.Position.Y + 30, 19, C.Text, HorizontalAlignment.Left, w - 200, 700, 1);
            string terms = $"{m.Reward:0} cr · " + (m.Turns is int tl ? $"{tl} turn{(tl > 1 ? "s" : "")} left" : "story") + (c.BattleOf(m) != null ? $" · {m.Strength} opposition" : "");
            Text(terms, rr.Position.X + 62, rr.Position.Y + 56, 15, C.Lilac, HorizontalAlignment.Left, w - 200, 600);
            Offer mm = m;
            Button($"sec.brief.{m.Id}", new Rect2(rr.End.X - 128, rr.Position.Y + 15, 118, 44), "Briefing", () => F.OpenBrief(mm), on: true);
            y += 84;
        }
        // the port where the fleet stands: its keys, open only at a friendly port; a port that deals with you has a market
        string plate = port ? $"Port: {at.PortName}" : market ? $"{FacName(at.Owner)} port: {at.PortName}"
            : at.IsPort ? $"Closed to you: standing {c.Standing(at.Owner)}" : "No friendly port here";
        Lbl(plate, x, y + 16, w);
        y += 28;
        // the port's tabs (PortPanel), as the mockup's sector panel names them
        float kw = (w - 8) / 2;
        void PortKey(string id, string label, int i) =>
            Button($"sec.port.{id}", new Rect2(x + (i % 2) * (kw + 8), y + (i / 2) * 50, kw, 44), label, () => { F.Port = id; F.Brief = null; }, dis: !port);
        PortKey("yard", "Shipyard", 0);
        PortKey("outfit", "Loadout", 1);
        PortKey("supply", "Supplies", 2);
        PortKey("crew", "Crew", 3);
        y += 104;
        if (market) Button("sec.market", new Rect2(x, y, w, 44), "Market", () => { F.Port = "market"; F.Brief = null; }, on: true);
    }

    private void Briefing(Campaign.Campaign c, Offer m)
    {
        MissionScript def = c.ScriptOf(m);
        MissionBattle? bt = c.BattleOf(m);
        float w = Math.Min(720, Size.X - 20), h = Math.Min(Size.Y - 100, 720);
        Rect2 b = Sheet(new Rect2((Size.X - w) / 2, 80, w, h), "Briefing");
        float x = b.Position.X, y = b.Position.Y + 34, cw = b.Size.X;
        Text(def.Name.ToUpperInvariant(), x, y, 32, C.Sky, HorizontalAlignment.Left, cw, 700, 2);
        y += 30;
        Text($"{(def.Type.Length > 0 ? def.Type + " · " : "")}{m.Reward:0} cr · {m.Strength} · " + (m.Turns is int tl ? $"{tl} turns left" : "story"), x, y, 17, C.Lilac, HorizontalAlignment.Left, cw, 600);
        y += 34;
        var sc = new OfferScope(c, m);
        y += Para(c.Brief(m), x, y, cw, 19) + 12;
        Lbl("Objective", x, y); y += 24;
        y += Para(c.Objective(m), x, y, cw, 19, C.Sky) + 10;
        if (bt != null)
        {
            Lbl("Objectives", x, y); y += 26;
            foreach (MissionObjective o in bt.Objectives)
            {
                DrawCircle(new Vector2(x + 8, y - 6), 5, o.Primary ? C.Sky : C.Lilac);
                if (!o.Primary) DrawCircle(new Vector2(x + 8, y - 6), 3, Sk.R.Sheet);
                Text(o.Text.Eval(sc) + (o.Primary ? "" : " (bonus)"), x + 22, y, 18, o.Primary ? C.Text : C.Lilac, HorizontalAlignment.Left, cw - 22, 600);
                y += 25;
            }
            y += 8;
            string intel = bt.Intel?.Eval(sc) ?? string.Join(", ", bt.Groups.Where(gp => gp.Role == GroupRole.Enemy).Select(gp => $"{gp.Label.Eval(sc)} ({FacName(gp.Faction.Eval(sc))})"));
            Row("Opposition", intel, x, y, cw, C.Alert); y += 26;
            var wing = bt.Groups.Where(gp => gp.Role != GroupRole.Enemy).Select(gp => gp.Label.Eval(sc)).ToList();
            if (wing.Count > 0) { Row("With you", string.Join(", ", wing), x, y, cw, C.Accord); y += 26; }
        }
        else { Row("Battle", "none expected", x, y, cw); y += 26; }
        Row("Sector", $"{c.Hex(m.C, m.R).PortName ?? $"{m.C}.{m.R}"} · {c.TerrainOf(c.Hex(m.C, m.R)).Name}", x, y, cw); y += 26;
        string? need = c.Needs(m);
        bool here = c.S.AtC == m.C && c.S.AtR == m.R;
        if (need != null) { Text(need, x, y + 6, 18, C.Caution, HorizontalAlignment.Left, cw, 600); y += 30; }
        if (!here) Text(c.S.Active.Contains(m.Id) ? "Accepted: the fleet is on its way, and the briefing opens when it arrives." : "Accept it and the fleet sets course for its sector.",
            x, y + 6, 17, C.Lilac, HorizontalAlignment.Left, cw, 600);
        float by = b.End.Y - 52;
        Button("br.close", new Rect2(x, by, 150, 48), "Close", () => F.Brief = null);
        if (here) Button("br.launch", new Rect2(b.End.X - 230, by, 230, 48), bt != null ? "Launch" : "Complete", () => F.Launch(m), dis: need != null);
        else
        {
            Button("br.go", new Rect2(b.End.X - 230, by, 230, 48), "Set course", () => { F.SelectHex((m.C, m.R)); F.MoveHere(); }, dis: F.Path == null && !(F.Sel == (m.C, m.R)));
            if (!c.S.Active.Contains(m.Id) && !def.Story) Button("br.accept", new Rect2(b.End.X - 420, by, 180, 48), "Accept", () => F.Accept(m),
                tip: new Tip("Accept", new (string, string, Color?)[] { ("Then", "the fleet sets course for its sector", null) }));
        }
    }

    private void HoverName(Campaign.Campaign c)
    {
        MapScreen? ms = MapOf();
        if (ms?.HoverHex is not { } hh || F.Port != null || F.Brief != null) return;
        Sector h = c.Hex(hh.C, hh.R);
        string s = $"{FacName(h.Owner)} space" + (h.PortName != null ? $" · {h.PortName}" : "");
        float w = X.Fonts.Width(s, 17, 600) + 24;
        var r = new Rect2(ms.Mouse + new Vector2(18, 18), new Vector2(w, 30));
        Shapes.RoundRect(this, r, new Color(Sk.R.Plate, 0.92f), 8);
        Text(s, r.Position.X + 12, r.Position.Y + 21, 17, C.Text, HorizontalAlignment.Left, w, 600);
    }
}
