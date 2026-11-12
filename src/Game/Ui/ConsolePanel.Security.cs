// ConsolePanel.Security: the Security view, on the Transport station's Security tab (boarding defence; the battle console mockup's stSecurity and secView,
// openspec/changes/boarding-defence). The mockup puts it on the Transport station's Security tab; the Transport station is
// the comms package's page, so here it is its own page on the ship panel's rail, the ship's interior beside its systems.
// The teams as keys (pick one, then click a room to send it there), Walk or Beam, the deck plan in 3D, an exploded view
// of its decks turned by dragging, with the teams and boarders as figures, one for each marine, walking the corridors and
// their paths still to walk; expanded, beside it, every team and squad, where it is and what it is doing, the security
// chief and the ambush rule. The 3D is drawn by projection into the panel (not a second renderer), the mockup's camera
// in shape: the rules it shows are the battle's (Battle.Security.cs), and a click sends the order through Battle.SecurityGo.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public abstract partial class ConsolePanel
{
    /// <summary>How far apart the decks are drawn, metres (an exploded view, so every deck shows), and the view's pitch.</summary>
    private const float DeckGapM = 13f, SecPitch = 0.5f;

    /// <summary>The Security view in a rectangle: the teams, Walk or Beam, the deck plan and the expanded lists. Drawn by the
    /// Transport station's Security tab, where the mockup has it (one component).</summary>
    protected void SecurityPanel(Rect2 b)
    {
        Ship s = Me;
        if (s.Sec is not { } sec) { Lbl("No security teams aboard", b.Position.X, b.Position.Y + 20, b.Size.X); return; }
        ConsoleState st = X.State;
        bool big = Big;
        float x = b.Position.X, y = b.Position.Y, w = b.Size.X;
        float kh = big ? 40 : 30, mw = big ? 220 : 146, kw = Math.Min(big ? 110 : 62, (w - mw - 10) / Math.Max(1, sec.Teams.Count) - 4);
        for (int i = 0; i < sec.Teams.Count; i++)
        {
            Unit u = sec.Teams[i];
            string id = u.Id;
            Button($"sec.t.{id}", new Rect2(x + i * (kw + 4), y, kw, kh), $"{id} · {u.N}", () => st.SecSel = id, on: st.SecSel == id,
                tip: new Tip($"Team {id}", new (string, string, Color?)[] { ("Marines", u.N.ToString(), null), ("Where", Battle.RoomOf(sec, u).Name, null), ("State", B.UnitState(u), null) }));
        }
        Tabs("sec.mode", new Rect2(x + w - mw, y, mw, kh), new[] { ("walk", "Walk"), ("beam", "Beam") }, st.SecBeam ? "beam" : "walk", m => st.SecBeam = m == "beam");
        y += kh + 6;
        float vw = big ? MathF.Round(w * 0.66f) : w, vh = b.End.Y - y - (big ? 0 : 24);
        SecView(sec, new Rect2(x, y, vw, vh), big);
        int foes = sec.Foes.Sum(u => u.N), set = sec.Teams.Count(u => B.UnitState(u) == "Ambush set");
        if (!big)
        {
            Lbl($"{(foes > 0 ? $"{foes} boarders aboard" : "No boarders aboard")} · {set} ambush{(set == 1 ? "" : "es")} set", x, b.End.Y - 6, w, foes > 0 ? C.Caution : C.Lilac, HorizontalAlignment.Left, 13);
            return;
        }
        // expanded: every team and squad, where it is, what it is doing
        float cx = x + vw + 20, yy = y, cw = b.End.X - cx;
        Lbl("Teams", cx, yy + 14);
        yy += 22;
        foreach (Unit u in sec.Teams)
        {
            string id = u.Id;
            HitState hs = Ui.Reg($"sec.row.{id}", new Rect2(cx, yy, cw, 34), _ => st.SecSel = id);
            if (st.SecSel == id || hs.Hot) Kit.Shape(this, new Rect2(cx, yy, cw, 32), 16, new Color(C.Deep, st.SecSel == id ? 0.8f : 0.4f));
            string state = B.UnitState(u);
            Text($"{id} · {u.N}", cx + 12, yy + 23, 18, C.Sky, HorizontalAlignment.Left, 70);
            Text(Battle.RoomOf(sec, u).Name, cx + 86, yy + 23, 17, C.Text, HorizontalAlignment.Left, cw - 230);
            Lbl(state, cx + cw - 8, yy + 22, 140, state == "Ambush set" ? C.Accord : u.Fight != null ? C.Alert : C.Lilac, HorizontalAlignment.Right, 14);
            yy += 36;
        }
        yy += 10;
        Lbl("Boarders", cx, yy + 14);
        yy += 22;
        if (sec.Foes.Count == 0) { Lbl("None aboard", cx + 12, yy + 18, cw, C.Deep, HorizontalAlignment.Left, 16); yy += 30; }
        var sr = B.Data.Systems.Cloak.Security;
        foreach (Unit u in sec.Foes)
        {
            Text($"{u.Id} · {u.N}", cx + 12, yy + 23, 18, C.Alert, HorizontalAlignment.Left, 70);
            Text($"{Battle.RoomOf(sec, u).Name} → {sec.Plan.Room(u.Goal).Name}", cx + 86, yy + 23, 17, C.Text, HorizontalAlignment.Left, cw - 230);
            bool bridge = sr.RoomSystem.GetValueOrDefault(u.Goal) == "bridge" && u.Sab > 0;
            Lbl(bridge ? $"Bridge {Pct(u.Sab / sr.CaptureS)}" : B.UnitState(u), cx + cw - 8, yy + 22, 140, u.Sab > 0 ? C.Alert : C.Lilac, HorizontalAlignment.Right, 14);
            yy += 36;
        }
        yy += 10;
        // the security chief: the Transport station's officer (the officers package); without one the slot is vacant
        int chief = B.OfficerSkill(s, "transport");
        Lbl("Security chief", cx, yy + 14);
        yy += 22;
        Text(chief > 0 ? $"Skill {chief}" : "Vacant", cx + 12, yy + 20, 18, chief > 0 ? C.Text : C.Deep, HorizontalAlignment.Left, cw - 24);
        yy += 30;
        foreach (var (l, v) in new (string, double)[] { ("Ambush first volley", sr.OfficerAmbush), ("Hit aboard", sr.OfficerAboard) })
        {
            Lbl(l, cx + 12, yy + 14, cw * 0.6f);
            Num(chief > 0 ? $"+{Math.Round(chief * v * 100)} %" : "None", cx + cw - 8, yy + 15, 0, chief > 0 ? C.Accord : C.Lilac, HorizontalAlignment.Right, 17);
            yy += 24;
        }
        yy += 10;
        Lbl($"Hold a room {sr.AmbushS:0} s to set an ambush:", cx, yy + 14, cw, C.Lilac, HorizontalAlignment.Left, 14);
        Lbl("first volley harder, boarders get no shot", cx, yy + 34, cw, C.Lilac, HorizontalAlignment.Left, 14);
    }

    // The deck plan in 3D in its window: rooms as floors and outlines coloured by kind (a boarder's goal lit in Alert), each a
    // target for the picked team; the units as figures with their tags; the paths still to walk.
    private void SecView(SecurityState sec, Rect2 v, bool big)
    {
        if (v.Size.X < 40 || v.Size.Y < 40) return;
        DeckPlan plan = sec.Plan;
        ConsoleState st = X.State;
        Draw2D.Rect(this, v.Grow(-2), Sk.R.Plate);
        Kit.Piece(this, "frame.inset", v.Grow(2));
        var inner = v.Grow(-4);
        // drag to turn the ship (under the rooms, so a click on a room sends the team)
        float yaw0 = st.SecYaw, x0 = 0;
        Ui.Reg("sec.view", inner, null, (p, start) => { if (start) { x0 = p.X; yaw0 = st.SecYaw; } else st.SecYaw = yaw0 - (p.X - x0) * 0.012f; },
            new Tip("Deck plan", new (string, string, Color?)[] { ("Drag", "turn the ship", null) }));
        float gap = plan.DeckFloorM.Length > 1 ? (float)(plan.DeckFloorM[0] - plan.DeckFloorM[1]) : 3.5f;
        float Iy(double yy) => (float)yy / gap * DeckGapM;
        double z0 = plan.Rooms.Min(r => r.Z0), z1 = plan.Rooms.Max(r => r.Z1), xm = plan.Rooms.Max(r => Math.Max(Math.Abs(r.X0), Math.Abs(r.X1)));
        float cy = Iy(plan.DeckFloorM.Average()), cz = (float)(z0 + z1) / 2;
        float sy = MathF.Sin(st.SecYaw), cyw = MathF.Cos(st.SecYaw), sp = MathF.Sin(SecPitch), cp = MathF.Cos(SecPitch);
        // the camera's right and up axes (it orbits the plan's middle at SecYaw, looking down at SecPitch)
        (float X, float Y) Rot(double px, float py, double pz)
        {
            float dx = (float)px, dy = py - cy, dz = (float)pz - cz;
            return (dx * cyw - dz * sy, dx * (-sp * sy) + dy * cp + dz * (-sp * cyw));
        }
        // fitted: the plan's corners scaled until the widest reaches 92 % of the view
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        foreach (double px in new[] { -xm, xm })
            foreach (double pz in new[] { z0, z1 })
                foreach (float py in new[] { Iy(plan.DeckFloorM.Min()), Iy(plan.DeckFloorM.Max()) + 3 })
                {
                    var (rx, ry) = Rot(px, py, pz);
                    minX = Math.Min(minX, rx); maxX = Math.Max(maxX, rx); minY = Math.Min(minY, ry); maxY = Math.Max(maxY, ry);
                }
        float k = 0.92f * Math.Min(inner.Size.X / Math.Max(1, maxX - minX), inner.Size.Y / Math.Max(1, maxY - minY));
        Vector2 o = inner.GetCenter() - new Vector2((minX + maxX) / 2 * k, -(minY + maxY) / 2 * k);
        Vector2 P(double px, double py, double pz, float dy = 0) { var (rx, ry) = Rot(px, Iy(py) + dy, pz); return o + new Vector2(rx * k, -ry * k); }

        var goals = new HashSet<string>(sec.Foes.Select(u => u.Goal));
        Unit? u0 = sec.Teams.FirstOrDefault(q => q.Id == st.SecSel);
        var sr = B.Data.Systems.Cloak.Security;
        string? hot = Ui.Hot != null && Ui.Hot.StartsWith("sec.room.") ? Ui.Hot["sec.room.".Length..] : null;
        // rooms, lowest deck first, so a higher deck's room is the one picked where they overlap on screen
        foreach (DeckRoom r in plan.Rooms.OrderBy(r => r.Y))
        {
            Vector2[] floor = { P(r.X0, r.Y, r.Z0), P(r.X1, r.Y, r.Z0), P(r.X1, r.Y, r.Z1), P(r.X0, r.Y, r.Z1) };
            float h = r.IsCorridor ? 0.4f : (float)plan.HeadroomM;
            Vector2[] top = { P(r.X0, r.Y, r.Z0, h), P(r.X1, r.Y, r.Z0, h), P(r.X1, r.Y, r.Z1, h), P(r.X0, r.Y, r.Z1, h) };
            bool goal = goals.Contains(r.Id);
            Color kind = goal && r.Id != hot ? C.Alert : RoomColour(r.Kind);
            Draw2D.ColoredPolygon(this, floor, new Color(kind, r.Id == hot ? 0.7f : goal ? 0.5f : 0.28f));
            Color edge = new(r.IsCorridor ? C.Lilac : C.Sky, r.IsCorridor ? 0.25f : 0.55f);
            Draw2D.Polyline(this, new[] { floor[0], floor[1], floor[2], floor[3], floor[0] }, edge, 1, true);
            Draw2D.Polyline(this, new[] { top[0], top[1], top[2], top[3], top[0] }, edge, 1, true);
            for (int i = 0; i < 4; i++) Draw2D.Line(this, floor[i], top[i], edge, 1, true);
            // its target for the picked team, kept inside the view
            var bb = new Rect2(floor[0], Vector2.Zero);
            foreach (Vector2 q in floor) bb = bb.Expand(q);
            bb = bb.Intersection(inner);
            if (bb.Size.X < 2 || bb.Size.Y < 2) continue;
            string rid = r.Id, sys = sr.RoomSystem.GetValueOrDefault(r.Id, "");
            var rows = new List<(string, string, Color?)> { ("Deck", (r.Deck + 1).ToString(), null) };
            if (sys != "") rows.Add(("Boarders here hit", sys == "bridge" ? "Command: they take the ship" : sys, null));
            rows.Add(("Click", u0 != null ? $"{(st.SecBeam ? "Beam" : "Send")} {u0.Id} here" : "Pick a team first", null));
            Ui.Reg($"sec.room.{rid}", bb, _ => { if (X.State.SecSel is { } sel) B.SecurityGo(sel, rid, X.State.SecBeam); }, null, new Tip(r.Name, rows), floor);
            // the rooms boarders go for, named (expanded)
            if (big && sys != "") { Vector2 lp = P(r.Cx, r.Y, r.Cz); Lbl(r.Name, lp.X, lp.Y + 4, 130, C.Lilac, HorizontalAlignment.Center, 12); }
        }
        // paths still to walk: the picked team's in Sky, the boarders' in Alert
        void PathOf(Unit u, Color col)
        {
            Vector2 a = P(u.X, u.Y, u.Z);
            foreach (int n in u.Path) { DeckNode nd = plan.Nodes[n]; Vector2 c = P(nd.X, nd.Y, nd.Z); Draw2D.DashedLine(this, a, c, col, 2, 6, true); a = c; }
        }
        foreach (Unit u in sec.Foes) PathOf(u, C.Alert);
        if (u0 != null) PathOf(u0, C.Sky);
        // the units: a figure for each marine round its place, its tag over its head (a team's tag picks it)
        double now = Time.GetTicksMsec() / 1000.0;
        foreach (Unit u in sec.Teams.Concat(sec.Foes))
        {
            bool own = u.Side == Me.Side;
            string state = B.UnitState(u);
            Color ring = !own ? C.Alert : st.SecSel == u.Id ? C.Caution : state == "Ambush set" ? C.Accord : C.Sky, body = own ? C.Sky : C.Alert;
            int figs = Math.Min(6, u.N);
            for (int i = 0; i < figs; i++)
            {
                double a = (double)i / Math.Max(1, figs) * Math.Tau, rr = u.N > 1 ? 0.9 : 0, bob = u.Path.Count > 0 ? Math.Abs(Math.Sin(now * 8 + i)) * 0.25 : 0;
                double fx = u.X + Math.Cos(a) * rr, fz = u.Z + Math.Sin(a) * rr;
                Vector2 feet = P(fx, u.Y, fz, (float)bob), head = P(fx, u.Y, fz, (float)bob + 1.85f);
                Draw2D.Line(this, feet, head, body, Math.Max(2, k * 0.8f), true);
                Draw2D.Circle(this, head, Math.Max(2, k * 0.34f), body, true, -1, true);
            }
            Vector2 c0 = P(u.X, u.Y, u.Z, 0.05f);
            Draw2D.Arc(this, c0, Math.Max(5, k * 1.7f), 0, Mathf.Tau, 24, ring, 2, true);
            Vector2 tp = P(u.X, u.Y, u.Z, 3);
            string tag = $"{u.Id} {u.N}";
            float tw = 14 + tag.Length * 8;
            var tr = new Rect2(tp.X - tw / 2, tp.Y - 20, tw, 18);
            if (own && inner.Encloses(tr.Grow(-1)))
            {
                string id = u.Id;
                Ui.Reg($"sec.u.{id}", tr, _ => st.SecSel = id, null,
                    new Tip($"Team {id}", new (string, string, Color?)[] { ("Marines", u.N.ToString(), null), ("Where", Battle.RoomOf(sec, u).Name, null), ("State", state, null) }));
            }
            if (!inner.Encloses(tr)) continue;
            Color fill = own ? (st.SecSel == u.Id ? C.Caution : C.Signal) : C.Alert;
            Kit.Shape(this, tr, 9, fill);
            Text(tag, tp.X, tp.Y - 6, 13, Sk.InkOn(fill), HorizontalAlignment.Center, tw - 6);
            if (u.Fight != null) Draw2D.Arc(this, P(u.X, u.Y, u.Z), 16 + 3 * MathF.Sin((float)now * 8.3f), 0, Mathf.Tau, 24, C.Alert, 2, true);
        }
        if (big) Lbl("Drag to turn the ship · click a room to send the picked team", inner.Position.X + 10, inner.End.Y - 10, inner.Size.X - 20, C.Lilac, HorizontalAlignment.Left, 13);
    }

    // A room's colour by its kind, as the mockup's roomCol.
    private Color RoomColour(string kind) => kind switch
    {
        "command" => C.Sky, "systems" => C.Lilac, "crew" => C.Signal, "engineering" => C.Caution, "weapons" => C.Alert, "corridor" => C.Deep, "bay" => C.Lilac, _ => C.Lilac,
    };
}
