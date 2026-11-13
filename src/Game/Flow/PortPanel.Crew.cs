// PortPanel.Crew: the port's crew tab (change crew-leveling, mockup 39; owner, 2026-11-13: "lets do branches, implement
// that", "implement the tree in the game"). Across the top, the six stations, each with its officer's level, rank,
// experience and what waits on them (points to place, a promotion ready, auto level). Below, the picked officer: promote,
// auto level, retrain, transfer out, and their station's skill tree as Branches, growing down from its three roots, each
// skill an icon (SkillIcons) on the stage the mockup draws (data/campaign/crew.json layout), the lines dashed where either
// earlier skill opens the next and solid where every one is needed, lit once learned; a hover card on each skill, and a tap
// or click picks it, so its card beside the tree (what it changes, what it needs, Learn) needs no hover. Then the officers
// on offer, priced in prestige. Every rule is Campaign's (Campaign.Crew.cs); this file only draws and calls.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Campaign;
using FleetCommand.Game.Ui;
using Godot;

namespace FleetCommand.Game.Flow;

public sealed partial class PortPanel
{
    private static readonly Dictionary<string, string> RoleName = new()
    {
        ["root"] = "Root", ["step"] = "Lane skill", ["crossover"] = "Crossover", ["keystone"] = "Keystone", ["tip"] = "Lane tip", ["capstone"] = "Capstone",
    };

    private void Crew(Campaign.Campaign c, Rect2 b)
    {
        OfficerDef od = c.Data.Officers;
        if (F.CrewStation == null || !od.Stations.Contains(F.CrewStation)) F.CrewStation = od.Stations[0];
        Text($"Prestige {c.S.Prestige}", b.End.X, b.Position.Y + 16, 20, C.Caution, HorizontalAlignment.Right, 260, 700);
        Lbl("Your officers: pick a station", b.Position.X, b.Position.Y + 14, b.Size.X - 280);
        int n = od.Stations.Count;
        float tw = (b.Size.X - 10 * (n - 1)) / n, th = 112, y = b.Position.Y + 26;
        for (int i = 0; i < n; i++) StationCard(c, od.Stations[i], new Rect2(b.Position.X + i * (tw + 10), y, tw, th));

        float top = y + th + 16, lw = Math.Min(b.Size.X * 0.6f, b.Size.X - 420), gap = 18;
        var left = new Rect2(b.Position.X, top, lw, b.End.Y - top);
        var right = new Rect2(left.End.X + gap, top, b.End.X - left.End.X - gap, b.End.Y - top);
        Officer? o = c.AtStation(F.CrewStation);
        if (o == null)
        {
            Kit.Piece(this, "frame.inset", new Rect2(left.Position, new Vector2(left.Size.X, 120)));
            Text($"{F.CrewStation}: vacant", left.Position.X + 18, left.Position.Y + 40, 22, C.Lilac, HorizontalAlignment.Left, left.Size.X - 36, 700);
            Para("No officer at this station. Hire one from those on offer; an ensign costs nothing.", left.Position.X + 18, left.Position.Y + 62, left.Size.X - 36, 16, C.Lilac, 2);
        }
        else
        {
            float hh = OfficerHead(c, o, new Rect2(left.Position, new Vector2(left.Size.X, 132)));
            Tree(c, o, new Rect2(left.Position.X, left.Position.Y + hh + 10, left.Size.X, left.Size.Y - hh - 10));
        }
        float sh = o != null ? SkillCard(c, o, new Rect2(right.Position, new Vector2(right.Size.X, 230))) : 0;
        Pool(c, new Rect2(right.Position.X, right.Position.Y + sh + (sh > 0 ? 14 : 0), right.Size.X, right.Size.Y - sh - (sh > 0 ? 14 : 0)));
    }

    // a station: its icon and name, its officer, their level and rank, their experience, and what waits on them
    private void StationCard(Campaign.Campaign c, string st, Rect2 r)
    {
        Officer? o = c.AtStation(st);
        bool picked = st == F.CrewStation;
        HitState hs = Ui.Reg($"crew.station.{st}", r, _ => { F.CrewStation = st; F.CrewSkill = null; });
        Card(r, picked, hs.Hot);
        Kit.Icon(this, StationIcon.GetValueOrDefault(st, "fleet"), new Rect2(r.Position.X + 12, r.Position.Y + 12, 26, 26), o != null ? C.Sky : C.Lilac);
        Text(st.ToUpperInvariant(), r.Position.X + 46, r.Position.Y + 31, 15, picked ? C.Caution : C.Lilac, HorizontalAlignment.Left, r.Size.X - 56, 600, 1);
        if (o == null) { Text("Vacant", r.Position.X + 12, r.Position.Y + 66, 18, C.Lilac, HorizontalAlignment.Left, r.Size.X - 24, 600); return; }
        Text(o.Name, r.Position.X + 12, r.Position.Y + 62, 17, C.Text, HorizontalAlignment.Left, r.Size.X - 24, 700);
        Text($"Level {o.Level} · {c.RankOf(o)}", r.Position.X + 12, r.Position.Y + 82, 14, C.Lilac, HorizontalAlignment.Left, r.Size.X - 80, 600);
        (string chip, Color col)? ch = c.Points(o) > 0 ? ($"{c.Points(o)} PT", C.Caution) : c.NextXp(o) is int need && o.Xp >= need ? ("READY", C.Accord) : o.Auto ? ("AUTO", C.Sky) : null;
        if (ch is { } k) Text(k.chip, r.End.X - 12, r.Position.Y + 82, 14, k.col, HorizontalAlignment.Right, 70, 700, 1);
        XpBar(c, o, new Rect2(r.Position.X + 12, r.End.Y - 16, r.Size.X - 24, 6));
    }

    private void XpBar(Campaign.Campaign c, Officer o, Rect2 r)
    {
        int? need = c.NextXp(o);
        float f = need is int nd ? Math.Clamp(o.Xp / (float)nd, 0, 1) : 1;
        DrawRect(r, C.Deep);
        DrawRect(new Rect2(r.Position, new Vector2(r.Size.X * f, r.Size.Y)), need is int n2 && o.Xp >= n2 ? C.Accord : C.Caution);
    }

    // the picked officer: name, level and rank, experience, and the four keys; returns its height
    private float OfficerHead(Campaign.Campaign c, Officer o, Rect2 r)
    {
        Kit.Piece(this, "frame.inset", r);
        Text(o.Name, r.Position.X + 18, r.Position.Y + 34, 24, C.Text, HorizontalAlignment.Left, r.Size.X * 0.5f, 700);
        Text($"{o.Station} · Level {o.Level} · {c.RankOf(o)}", r.Position.X + 18, r.Position.Y + 58, 15, C.Lilac, HorizontalAlignment.Left, r.Size.X * 0.5f, 600);
        int? need = c.NextXp(o);
        Text(need is int nd ? $"{o.Xp} / {nd} experience" : $"Level {c.Data.Crew.Levels}: the highest", r.End.X - 18, r.Position.Y + 34, 15, C.Sky, HorizontalAlignment.Right, r.Size.X * 0.45f, 600);
        XpBar(c, o, new Rect2(r.Position.X + r.Size.X * 0.55f, r.Position.Y + 48, r.Size.X * 0.45f - 18, 8));
        float bw = (r.Size.X - 36 - 3 * 8) / 4, by = r.End.Y - 52;
        int pay = c.PromoteCost(o);
        Officer oo = o;
        Button("crew.promote", new Rect2(r.Position.X + 18, by, bw, 40), need == null ? "Highest level" : pay > 0 ? $"Promote: {pay} prestige" : "Promote",
            () => Say(c.Promote(oo), $"{oo.Name} reaches level {oo.Level}, {c.RankOf(oo)}: a skill point{(oo.Auto ? ", spent by auto level" : "")}."),
            dis: need == null || pay > c.S.Prestige, tip: new Tip("Promote", new (string, string, Color?)[] { ("Next level", need is int n3 ? $"{n3} experience" : "none", null), ("Paid in prestige", $"{pay}", null) }));
        Button("crew.auto", new Rect2(r.Position.X + 18 + (bw + 8), by, bw, 40), o.Auto ? "Auto level: on" : "Auto level: off",
            () => { c.SetAuto(oo, !oo.Auto); Say(null, oo.Auto ? $"{oo.Name} auto levels up the centre of the {oo.Station} tree." : $"{oo.Name}'s points are yours to place."); }, on: o.Auto);
        Button("crew.retrain", new Rect2(r.Position.X + 18 + 2 * (bw + 8), by, bw, 40), $"Retrain: {c.Data.Crew.Retrain} prestige",
            () => Say(c.Retrain(oo), $"{oo.Name} is retrained: {c.Points(oo)} points to place."), dis: o.Skills.Count == 0 || c.Data.Crew.Retrain > c.S.Prestige);
        Button("crew.transfer", new Rect2(r.Position.X + 18 + 3 * (bw + 8), by, bw, 40), $"Transfer out: +{c.Worth(o)} prestige",
            () => { int w = c.Worth(oo); Say(c.Transfer(oo), $"{oo.Name} transfers out: +{w} prestige."); });
        return r.Size.Y;
    }

    // the station's tree as Branches, on the data's stage scaled into r
    private void Tree(Campaign.Campaign c, Officer o, Rect2 r)
    {
        Kit.Piece(this, "frame.inset", r);
        CrewLayout L = c.Data.Crew.Layout;
        CrewTree t = c.TreeOf(o);
        float k = Math.Min((r.Size.X - 20) / L.StageW, (r.Size.Y - 20) / L.StageH);
        var o0 = new Vector2(r.Position.X + (r.Size.X - L.StageW * k) / 2, r.Position.Y + (r.Size.Y - L.StageH * k) / 2);
        Vector2 P(float x, float y) => o0 + new Vector2(x, y) * k;
        float Rad(CrewSkill s) => (s.Role == "capstone" ? 40 : s.Role == "keystone" ? 36 : 32) * k;
        // the lane names over the roots
        for (int i = 0; i < t.Themes.Count; i++)
            Text(t.Themes[i].ToUpperInvariant(), P(L.LaneX0 + i * L.LaneDx, 0).X, P(0, L.ThemesY).Y, Math.Max(11, (int)(13 * k)), C.Lilac, HorizontalAlignment.Center, L.LaneDx * k, 600, 1);
        // the lines: down out of the foot of the earlier skill's name into the top of the next
        foreach (CrewSkill s in t.Skills)
            foreach (var (pre, all) in s.All.Select(p => (p, true)).Concat(s.Any.Select(p => (p, false))))
            {
                CrewSkill a = t.Skill(pre)!;
                var (ax, ay) = L.At(a); var (bx, by) = L.At(s);
                Vector2 p0 = P(ax, ay) + new Vector2(0, Rad(a) + 30 * k), p1 = P(bx, by) - new Vector2(0, Rad(s));
                Curve(p0, p1, o.Skills.Contains(pre) ? C.Sky : C.Deep.Lightened(0.25f), Math.Max(1.5f, 2.5f * k), !all);
            }
        // the skills
        foreach (CrewSkill s in t.Skills)
        {
            var (sx, sy) = L.At(s);
            Vector2 at = P(sx, sy);
            float rad = Rad(s);
            bool learned = o.Skills.Contains(s.Id), open = !learned && c.CanLearn(o, s), picked = F.CrewSkill == s.Id;
            var box = new Rect2(at - new Vector2(rad, rad), new Vector2(2 * rad, 2 * rad));
            Officer oo = o;
            HitState hs = Ui.Reg($"crew.skill.{s.Id}", box.Grow(4), _ => F.CrewSkill = s.Id, tip: SkillTip(c, o, s));
            Color fill = learned ? C.Sky : C.Deep, ring = learned ? C.Sky : open ? C.Caution : C.Deep.Lightened(0.3f), ink = learned ? new Color(0.02f, 0.06f, 0.12f) : open ? C.Caution : C.Lilac;
            if (!learned && !open) ink.A = 0.55f;
            if (s.Role == "capstone")
            {
                Vector2[] dia = { at + new Vector2(0, -rad * 1.2f), at + new Vector2(rad * 1.2f, 0), at + new Vector2(0, rad * 1.2f), at + new Vector2(-rad * 1.2f, 0) };
                DrawColoredPolygon(dia, fill);
                DrawPolyline(dia.Append(dia[0]).ToArray(), ring, 3, true);
            }
            else
            {
                DrawCircle(at, rad, fill);
                DrawArc(at, rad - 1.5f, 0, Mathf.Tau, 48, ring, 3, true);
                if (s.Role == "keystone") DrawArc(at, rad + 5, 0, Mathf.Tau, 48, ring, 2, true);
            }
            if (picked || hs.Hot) DrawArc(at, rad + (s.Role == "keystone" ? 10 : 6), 0, Mathf.Tau, 48, picked ? C.Text : C.Sky, 2, true);
            SkillIcons.Draw(this, s.Icon, new Rect2(at - new Vector2(rad * 0.62f, rad * 0.62f), new Vector2(rad * 1.24f, rad * 1.24f)), ink);
            Text(s.Name.ToUpperInvariant(), at.X, at.Y + rad * (s.Role == "capstone" ? 1.2f : 1) + Math.Max(12, 14 * k) + 4, Math.Max(11, (int)(12 * k)),
                learned ? C.Text : open ? C.Caution : C.Lilac, HorizontalAlignment.Center, 104 * k, 600);
        }
    }

    // a vertical cubic from p0 down to p1, solid or dashed
    private void Curve(Vector2 p0, Vector2 p1, Color col, float w, bool dashed)
    {
        float my = (p0.Y + p1.Y) / 2;
        Vector2 c1 = new(p0.X, my), c2 = new(p1.X, my);
        const int N = 24;
        Vector2 prev = p0;
        for (int i = 1; i <= N; i++)
        {
            float u = i / (float)N, v = 1 - u;
            Vector2 q = v * v * v * p0 + 3 * v * v * u * c1 + 3 * v * u * u * c2 + u * u * u * p1;
            if (!dashed || i % 2 == 1) DrawLine(prev, q, col, w, true);
            prev = q;
        }
    }

    private static string Needs(CrewTree t, CrewSkill s)
    {
        var parts = new List<string>();
        if (s.All.Count > 0) parts.Add((s.All.Count > 1 ? "both " : "") + string.Join(" and ", s.All.Select(id => t.Skill(id)!.Name)));
        if (s.Any.Count > 0) parts.Add("either " + string.Join(" or ", s.Any.Select(id => t.Skill(id)!.Name)));
        return parts.Count == 0 ? "Nothing: a root" : string.Join(", and ", parts);
    }

    private static string StateOf(Campaign.Campaign c, Officer o, CrewSkill s) =>
        o.Skills.Contains(s.Id) ? "Learned" : c.CanLearn(o, s) ? "Open: one point" : Campaign.Campaign.Unlocked(o, s) ? "No skill point to spend" : "Locked";

    // the hover card: what a skill changes, what it needs and whether it can be learned
    private Tip SkillTip(Campaign.Campaign c, Officer o, CrewSkill s)
    {
        CrewTree t = c.TreeOf(o);
        var rows = s.Text.Split("; ").Select(x => ("Changes", x, (Color?)null)).ToList();
        rows.Add(("Needs", Needs(t, s), null));
        rows.Add(("State", StateOf(c, o, s), o.Skills.Contains(s.Id) ? C.Sky : c.CanLearn(o, s) ? C.Caution : C.Lilac));
        return new Tip($"{s.Name} · {RoleName.GetValueOrDefault(s.Role, s.Role)}", rows);
    }

    // the picked skill beside the tree, with Learn: the tap's way to everything the hover card says; returns its height
    private float SkillCard(Campaign.Campaign c, Officer o, Rect2 r)
    {
        Kit.Piece(this, "frame.inset", r);
        CrewTree t = c.TreeOf(o);
        CrewSkill? s = F.CrewSkill != null ? t.Skill(F.CrewSkill) : null;
        if (s == null)
        {
            Lbl("Skill", r.Position.X + 18, r.Position.Y + 30, r.Size.X - 36);
            Para(c.Points(o) > 0 ? $"{c.Points(o)} skill point{(c.Points(o) > 1 ? "s" : "")} to place. Pick a skill on the tree to see what it does and learn it, or turn on Auto level."
                : o.Level == 1 ? "A level 1 ensign has no skill point and brings nothing to a battle yet. Promote them for their first, or transfer them out for prestige."
                : "Pick a skill on the tree to see what it does.", r.Position.X + 18, r.Position.Y + 54, r.Size.X - 36, 16, C.Lilac, 5);
            return r.Size.Y;
        }
        Lbl($"{o.Station} · {RoleName.GetValueOrDefault(s.Role, s.Role)}", r.Position.X + 18, r.Position.Y + 26, r.Size.X - 36);
        Text(s.Name, r.Position.X + 18, r.Position.Y + 56, 22, C.Text, HorizontalAlignment.Left, r.Size.X - 36, 700);
        float y = r.Position.Y + 72;
        string[] lines = s.Text.Split("; ");
        for (int i = 0; i < s.Fx.Count && i < lines.Length; i++)
        {
            SkillIcons.Draw(this, s.Fx[i].Ability, new Rect2(r.Position.X + 18, y, 22, 22), C.Sky);
            Text(lines[i], r.Position.X + 48, y + 17, 16, C.Text, HorizontalAlignment.Left, r.Size.X - 66, 600);
            y += 28;
        }
        Para("Needs " + Needs(t, s), r.Position.X + 18, y + 14, r.Size.X - 36, 15, C.Lilac, 2);
        bool open = c.CanLearn(o, s);
        Text(StateOf(c, o, s).ToUpperInvariant(), r.Position.X + 18, r.End.Y - 22, 14, o.Skills.Contains(s.Id) ? C.Sky : open ? C.Caution : C.Lilac, HorizontalAlignment.Left, r.Size.X - 200, 700, 1);
        Officer oo = o;
        string id = s.Id, name = s.Name;
        Button("crew.learn", new Rect2(r.End.X - 178, r.End.Y - 52, 160, 40), "Learn: 1 point", () => Say(c.Learn(oo, id), $"{oo.Name} learns {name}."), dis: !open);
        return r.Size.Y;
    }

    // the officers on offer at the picked station, priced in prestige
    private void Pool(Campaign.Campaign c, Rect2 r)
    {
        string st = F.CrewStation!;
        Officer? now = c.AtStation(st);
        Lbl($"On offer at {st}", r.Position.X, r.Position.Y + 12, r.Size.X);
        if (now != null) Text($"Hiring replaces {now.Name}, who transfers out for +{c.Worth(now)} prestige.", r.Position.X, r.Position.Y + 34, 14, C.Lilac, HorizontalAlignment.Left, r.Size.X, 600);
        var offer = c.OnOffer(st).OrderByDescending(o => o.Level).ToList();
        float y = r.Position.Y + 46, rh = Math.Min(60, (r.End.Y - y) / Math.Max(1, offer.Count) - 6);
        foreach (Officer o in offer)
        {
            var row = new Rect2(r.Position.X, y, r.Size.X, rh);
            Kit.Piece(this, "frame.inset", row);
            int cost = c.HireCost(o);
            Text(o.Name, row.Position.X + 14, row.Position.Y + rh / 2 - 2, 16, C.Text, HorizontalAlignment.Left, row.Size.X - 190, 700);
            Text($"Level {o.Level} · {c.RankOf(o)} · {o.Level - 1} point{(o.Level == 2 ? "" : "s")} to place", row.Position.X + 14, row.Position.Y + rh / 2 + 16, 13, C.Lilac, HorizontalAlignment.Left, row.Size.X - 190, 600);
            Officer oo = o;
            Button($"crew.hire.{o.Name}", new Rect2(row.End.X - 168, row.Position.Y + (rh - 40) / 2, 158, 40), cost > 0 ? $"Hire: {cost} prestige" : "Hire: free",
                () => Say(c.Hire(oo), $"{oo.Name} joins at level {oo.Level}, {c.RankOf(oo)}, at {oo.Station}."),
                dis: cost > c.S.Prestige + (now != null ? c.Worth(now) : 0));
            y += rh + 6;
        }
    }
}
