// MapHud: the campaign map's panels, over the 3D map (game-flow design, "Campaign map" and "Ports"; mockup 1 v14). The top
// bar hangs from the top of the screen (turn, credits, prestige, moves left, the fleet, Menu and End turn); the sector panel
// names the picked sector and what is in it, with a mission's row and its Briefing key, Move here (with how many moves and
// turns the route takes), and the Port key where the fleet stands at a port; the briefing shows a mission's file as the
// captain reads it; the port is the shipyard, the fleet, the outfitter, supplies and recruiting, each hull shown by its
// schematic (owner, 2026-11-05: "in store to preview the kinds of ships you are buying our outfitting"). Hovering the map
// names whose space the pointer is over. Every action goes to GameFlow or Campaign; nothing here decides anything.
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
        // the port is its own panel over the map (PortPanel.cs)
        if (F.Port == null && F.Brief != null) Briefing(c, F.Brief);
        if (F.Sel is { } s && F.Brief == null) SectorPanel(c, s.C, s.R);
        HoverName(c);
        if (F.Msg.Length > 0)
        {
            float w = Math.Min(Size.X - 40, X.Fonts.Width(F.Msg, 19) + 40);
            var r = new Rect2(20, Size.Y - 56, w, 38);
            Shapes.RoundRect(this, r, new Color(Sk.R.Sheet, 0.92f), 12);
            Text(F.Msg, r.Position.X + 20, r.Position.Y + 26, 19, C.Caution, HorizontalAlignment.Left, w - 30);
        }
    }

    private void TopBar(Campaign.Campaign c)
    {
        var r = new Rect2(0, 0, Size.X, 64);
        Ui.Reg("top.block", r);
        Kit.Piece(this, "strip.top", r);
        float x = 24, y = 40;
        Text(c.Side.Name.ToUpperInvariant(), x, y, 24, new Color(c.Side.Color).Lightened(0.25f), HorizontalAlignment.Left, 220, 700, 2);
        x += Math.Min(230, X.Fonts.Width(c.Side.Name.ToUpperInvariant(), 24, 700, 2) + 30);
        var items = new (string, string)[]
        {
            ("Turn", c.S.Turn.ToString()), ("Credits", $"{c.S.Credits} cr"), ("Prestige", c.S.Prestige.ToString()),
            ("Moves", $"{c.S.Mp} of {c.Data.Rules.MovePoints}"), ("Fleet", $"{c.S.Fleet.Count} of {c.Data.Rules.FleetMax}"),
        };
        if (Narrow) items = items.Take(3).ToArray();
        foreach (var (l, v) in items)
        {
            Lbl(l, x, y);
            x += X.Fonts.Width(l.ToUpperInvariant(), 15, 600, 1) + 8;
            Num(v, x, y + 2, 0, null, HorizontalAlignment.Left, 22);
            x += X.Fonts.Width(v, 22) + 26;
        }
        Button("top.end", new Rect2(Size.X - 190, 10, 170, 44), "End turn", F.EndTurn, tip: new Tip("End turn", new (string, string, Color?)[] { ("Then", "the factions move, missions change", null) }));
        Button("top.menu", new Rect2(Size.X - 320, 10, 120, 44), "Menu", F.ToMenu);
        Button("top.fleet", new Rect2(Size.X - 450, 10, 120, 44), "Fleet", () => { F.SelectHex((c.S.AtC, c.S.AtR)); MapOf()?.CentreOnFleet(); }, tip: new Tip("Centre on your fleet"));
    }

    private MapScreen? MapOf() => GetParent() as MapScreen;

    private Rect2 SidePanel(float h)
    {
        float w = Narrow ? Size.X - 20 : 440;
        return Narrow ? new Rect2(10, Size.Y - h - 70, w, h) : new Rect2(Size.X - w - 16, 80, w, h);
    }

    private void SectorPanel(Campaign.Campaign c, int col, int row)
    {
        Sector h = c.Hex(col, row);
        Offer? m = c.MissionAt(col, row);
        FactionFleet? g = c.GroupAt(col, row);
        bool here = c.S.AtC == col && c.S.AtR == row;
        float hgt = 250 + (m != null ? 86 : 0) + (g != null ? 28 : 0) + (h.IsPort ? 28 : 0);
        Rect2 b = Sheet(SidePanel(hgt), h.PortName ?? $"Sector {col}.{row}");
        float x = b.Position.X, y = b.Position.Y + 22, w = b.Size.X;
        Row("Space", FacName(h.Owner), x, y, w, FacColor(h.Owner).Lightened(0.3f)); y += 28;
        Terrain t = c.TerrainOf(h);
        Row("Terrain", t.Impassable ? $"{t.Name} (no entry)" : $"{t.Name} · {t.Cost} move{(t.Cost > 1 ? "s" : "")}", x, y, w); y += 28;
        if (h.IsPort) { Row(h.Planet != null ? "Planet" : "Station", h.PortName!, x, y, w); y += 28; }
        if (g != null) { Row("Fleet", $"{g.Name} · {g.Size} ship{(g.Size > 1 ? "s" : "")}", x, y, w, FacColor(g.Fac).Lightened(0.3f)); y += 28; }
        if (m != null)
        {
            // the mission row: its banner letter, its name and terms, and its Briefing key (owner: "make this more obvious its a button")
            MissionScript def = c.ScriptOf(m);
            var rr = new Rect2(x, y + 4, w, 74);
            Kit.Piece(this, "frame.inset", rr);
            Color ink = def.Story ? new Color("#8fd0ff") : c.S.Active.Contains(m.Id) ? new Color("#7dffa0") : new Color("#ffb547");
            Shapes.RoundRect(this, new Rect2(rr.Position + new Vector2(10, 14), new Vector2(40, 46)), ink, 6);
            Text(def.Glyph, rr.Position.X + 30, rr.Position.Y + 47, 28, new Color(0.05f, 0.05f, 0.08f), HorizontalAlignment.Center, 40, 700);
            Text(def.Name, rr.Position.X + 62, rr.Position.Y + 30, 20, C.Text, HorizontalAlignment.Left, w - 200, 700);
            string terms = $"{m.Reward:0} cr · " + (m.Turns is int tl ? $"{tl} turn{(tl > 1 ? "s" : "")} left" : "story") + $" · {m.Strength}";
            Text(terms, rr.Position.X + 62, rr.Position.Y + 56, 16, C.Lilac, HorizontalAlignment.Left, w - 200, 600);
            Offer mm = m;
            Button("sec.brief", new Rect2(rr.End.X - 128, rr.Position.Y + 15, 118, 44), "Briefing", () => F.OpenBrief(mm));
            y += 86;
        }
        y += 8;
        if (here)
        {
            Text("Your fleet is here.", x, y + 14, 18, C.Accord, HorizontalAlignment.Left, w, 600);
            y += 36;
            bool port = c.IsPort(h), market = c.IsMarket(h);
            if (port || market) Button("sec.port", new Rect2(x, y, w, 46), port ? "Port: shipyard and outfitter" : $"Market: sell to the {FacName(h.Owner)}", () => { F.Port = "yard"; F.Brief = null; });
        }
        else if (F.Path is { } path)
        {
            string how = $"{path.Cost} move{(path.Cost > 1 ? "s" : "")} · {path.Turns} turn{(path.Turns > 1 ? "s" : "")}";
            Text(how, x, y + 14, 18, C.Sky, HorizontalAlignment.Left, w, 600);
            y += 30;
            Button("sec.move", new Rect2(x, y, w, 46), path.Turns > 1 ? "Set course" : "Move here", F.MoveHere, dis: path.Legs[0].Count == 0 && path.Turns == 1);
        }
        else if (!t.Impassable) Text("No route from here.", x, y + 14, 18, C.Lilac, HorizontalAlignment.Left, w, 600);
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
