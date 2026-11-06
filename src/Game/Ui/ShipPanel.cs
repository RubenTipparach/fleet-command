// ShipPanel: the ship panel (battle-console design section 2): the ship systems display (the ship from above, its six
// shield facings, its weapon mounts as solid status boxes on a grid, its subsystems in two columns) and the shield page
// (six trapezoid keys to reinforce a facing, shields up or down, the shields' energy row). The systems display is one
// component (CLAUDE.md section 6.1): SystemsDisplay.Draw serves this panel and, later, the Target box and the report.
// Sensors, tractor and craft are on the rail and are not built yet.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Game.Ui.Kit;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class ShipPanel : ConsolePanel
{
    public static readonly (string Id, string Label, string Icon)[] Pages =
    {
        ("systems", "Systems", "ship"), ("shields", "Shields", "shields"), ("sensors", "Sensors", "sensors"), ("tractor", "Tractor", "tractor"), ("craft", "Craft", "craft"),
    };

    protected override void DrawPanel()
    {
        string cur = X.State.ShipPage;
        var pg = Array.Find(Pages, p => p.Id == cur);
        Rect2 p = Frame(cur is "systems" or "shields" ? $"{pg.Label} · {Me.Name}" : pg.Label);
        Rail("sh", p.Position.X, p.Position.Y, Pages, cur, X.Act.ShipPage);
        var b = new Rect2(p.Position.X + 56, p.Position.Y, p.Size.X - 56, p.Size.Y);
        switch (cur)
        {
            case "systems": SystemsDisplay.Draw(this, Ui, X, Me, b, true, "ssd", Big); break;
            case "shields": Shields(b); break;
            default:
                // each other page in its own file (ShipPanel.<Page>.cs); a page not yet built says so
                bool drawn = false;
                if (cur == "sensors") SensorsPage(b, ref drawn);
                else if (cur == "tractor") TractorPage(b, ref drawn);
                else if (cur == "craft") CraftPage(b, ref drawn);
                if (drawn) break;
                Kit.Icon(this, pg.Icon, new Rect2(b.GetCenter().X - 30, b.Position.Y + 40, 60, 60), C.Deep);
                Text(pg.Label.ToUpperInvariant(), b.GetCenter().X, b.Position.Y + 130, 24, C.Sky, HorizontalAlignment.Center, b.Size.X, 700, 1);
                Lbl("Not yet in the game", b.GetCenter().X, b.Position.Y + 156, b.Size.X, null, HorizontalAlignment.Center);
                break;
        }
    }

    partial void SensorsPage(Rect2 b, ref bool drawn);
    partial void TractorPage(Rect2 b, ref bool drawn);
    partial void CraftPage(Rect2 b, ref bool drawn);

    // Shield keys (owner, 2026-11-01): six big trapezoid keys in a hexagonal ring round the ship, the wide side outward,
    // each filling outward by its charge in its strength's colour; a click reinforces it (again to clear).
    private void Shields(Rect2 b)
    {
        Ship s = Me;
        float ro = Big ? Math.Min(b.Size.Y * 0.4f, 300) : Math.Min(b.Size.Y / 2 - 22, 96), ri = ro * 0.55f;
        var c = new Vector2(b.Position.X + ro + 6, b.Position.Y + ro + 4);
        Ship? t = B.TargetOf(s);
        int toward = t != null ? Rules.FacingFrom(s, t.X, t.Z) : -1;
        for (int i = 0; i < 6; i++)
        {
            float a = -Mathf.Pi / 2 + i * Mathf.Pi / 3, h = Mathf.Pi / 6 - 0.04f;
            Vector2 Pt(float ang, float r) => c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            Vector2[] key = { Pt(a - h, ri), Pt(a - h, ro), Pt(a + h, ro), Pt(a + h, ri) };
            double frac = s.Shields[i] / s.Class.ShieldsHp[i];
            int idx = i;
            HitState st = Ui.Reg($"shk.{i}", Bounds(key), _ => B.Reinforce(idx), null,
                new Tip($"{Rules.Facings[i]} shield", new (string, string, Color?)[] { ("Strength", $"{s.Shields[i]:0} of {s.Class.ShieldsHp[i]:0}", null), ("Charged", Pct(frac), null), ("Reinforced", s.Reinforce == i ? "yes" : "no", s.Reinforce == i ? C.Lilac : null) }), key);
            Pen.FillAbs(this, key, st.Hot ? C.Deep.Lightened(0.2f) : C.Deep);
            float rf = ri + (ro - ri) * (float)Math.Clamp(frac, 0, 1);
            if (frac > 0.01) Pen.FillAbs(this, new[] { Pt(a - h, ri), Pt(a - h, rf), Pt(a + h, rf), Pt(a + h, ri) }, ShieldColor(frac));
            if (s.Reinforce == i) DrawPolyline(new[] { key[0], key[1], key[2], key[3], key[0] }, C.Lilac, 3, true);
            if (i == toward) DrawLine(Pt(a - h, ro + 4), Pt(a + h, ro + 4), Colors.White, 3, true);
            Vector2 lp = Pt(a, (ri + ro) / 2);
            Text(Rules.Facings[i], lp.X, lp.Y + 6, Big ? 22 : 15, frac > 0.4 ? Sk.InkOn(ShieldColor(frac)) : C.Text, HorizontalAlignment.Center, 0, 700);
        }
        Kit.Icon(this, "ship", new Rect2(c - new Vector2(ri * 0.6f, ri * 0.6f), new Vector2(ri * 1.2f, ri * 1.2f)), C.Sky);
        float rx = c.X + ro + 18, rw = b.End.X - rx;
        Button("sh.up", new Rect2(rx, b.Position.Y + 4, rw, 40), s.ShieldsUp ? "Shields up" : "Shields down", () => B.ToggleShields(), !s.ShieldsUp);
        Pairs("sh.rg", rx, b.Position.Y + 78, new (string, string, Color?)[] { ("Regen", $"{s.Power["shields"] * B.Tuning.ShieldRegenPerES * s.Brown:0.0}/s", null) }, 20);
        Pairs("sh.rf", rx, b.Position.Y + 108, new (string, string, Color?)[] { ("Reinforced", s.Reinforce >= 0 ? Rules.Facings[s.Reinforce] : "None", s.Reinforce >= 0 ? C.Lilac : null) }, 20);
        // the shields' row of energy boxes: the same control the power station draws (one implementation)
        int cur = s.Power["shields"];
        float y = b.End.Y - 30;
        Lbl("Shields", b.Position.X, y + 18, 78);
        CellRow("sh.pw", new Rect2(b.Position.X + 82, y, b.Size.X - 82 - 32, 26), Rules.BoxesPerSystem, cur, n => B.SetPower("shields", n), Math.Min(Rules.BoxesPerSystem, B.Rules.PowerRoom(s, cur)));
        Num(cur.ToString(), b.End.X, y + 21, 0, null, HorizontalAlignment.Right, 20);
    }

    private static Rect2 Bounds(Vector2[] pts)
    {
        var r = new Rect2(pts[0], Vector2.Zero);
        foreach (Vector2 p in pts) r = r.Expand(p);
        return r;
    }
}

/// <summary>The ship systems display, one drawing for every place that shows a ship's systems.</summary>
public static class SystemsDisplay
{
    private static readonly string[] Left = { "weapons", "shields", "sensors", "reactor" };
    private static readonly string[] Right = { "engines", "tractor", "transport" };
    private static readonly Dictionary<string, string> SysIcon = new()
    {
        ["weapons"] = "weapons", ["shields"] = "shields", ["sensors"] = "sensors", ["engines"] = "engines", ["tractor"] = "tractor", ["transport"] = "transport", ["reactor"] = "reactor",
    };

    /// <param name="own">True for your own ship (mounts can be selected); false for a target seen from outside.</param>
    /// <returns>Each mount's box, for callers that add to it (the expanded weapons panel's Fire keys).</returns>
    public static List<(Mount M, Rect2 R)> Draw(CanvasItem ci, Imm ui, ConsoleContext x, Ship s, Rect2 b, bool own, string idp, bool big = false, Action<Mount>? pick = null)
    {
        var c = x.Kit.C;
        // a skin with rendered keys shows a status as the light in a key's glass; the white marks take its colours
        bool glass = x.Kit.Style.RenderedKeys;
        var mark = x.Kit.Style.Mark;
        Battle bt = x.Battle;
        var boxes = new List<(Mount, Rect2)>();
        float box = big ? 36 : Math.Min(30, (b.Size.Y - 8 - 3 * 6) / 4f), col = box + 6;
        var centre = new Vector2(b.GetCenter().X, b.GetCenter().Y);
        float R = big ? Math.Min(b.Size.Y * 0.34f, (b.Size.X - 2 * col) / 2 - 40) : Math.Min(b.Size.Y / 2 - 24, (b.Size.X - 2 * col) / 2 - 24);
        centre.Y += big ? 0 : 6;
        Ship? t = bt.TargetOf(bt.Player);
        Ship? other = own ? t : bt.Player;
        int toward = other != null ? Rules.FacingFrom(s, other.X, other.Z) : -1;
        // on an enemy's display its subsystems are aims, and on the locked target's its weapons too (weapon-targeting design:
        // "I should be able to click to aim at specific systems here too"); the chopping block from you is bracketed
        bool aims = !own && s.Side != bt.Player.Side, mountAims = aims && t == s;
        Exposure? block = !own && toward >= 0 ? bt.ExposureFrom(s, bt.Player) : null;
        float tk = big ? 7 : 5, bw = big ? 2.5f : 1.6f;

        // a dark disk inside the shield ring, and the grid faintly on it, clipped to the ring
        ci.DrawCircle(centre, R - 4, new Color(c.Deep, 0.35f), true, -1, true);
        float g = big ? 34 : 26;
        for (float gv = -MathF.Floor((R - 6) / g) * g; gv <= R - 6; gv += g)
        {
            float half = MathF.Sqrt(Math.Max(0, (R - 6) * (R - 6) - gv * gv));
            ci.DrawLine(centre + new Vector2(gv, -half), centre + new Vector2(gv, half), new Color(c.Sky, 0.12f), 1);
            ci.DrawLine(centre + new Vector2(-half, gv), centre + new Vector2(half, gv), new Color(c.Sky, 0.12f), 1);
        }

        // the six facings: one band each in its strength's colour; a white arc over the facing toward the enemy
        for (int i = 0; i < 6; i++)
        {
            float a = -Mathf.Pi / 2 + i * Mathf.Pi / 3;
            double frac = s.ShieldsUp ? s.Shields[i] / s.Class.ShieldsHp[i] : 0;
            Color sc = frac > 0.01 ? ShieldColorOf(c, frac) : c.Deep;
            if (s.Flash[i] > 0) sc = sc.Lerp(Colors.White, (float)s.Flash[i] * 0.6f);
            ci.DrawArc(centre, R, a - Mathf.Pi / 6 + 0.08f, a + Mathf.Pi / 6 - 0.08f, 18, sc, frac > 0.01 ? 7 : 2, true);
            if (i == toward) ci.DrawArc(centre, R + 8, a - Mathf.Pi / 6 + 0.16f, a + Mathf.Pi / 6 - 0.16f, 14, Colors.White, 2.5f, true);
            // each facing's strength beside it
            Vector2 np = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (R + (big ? 26 : 15));
            x.Fonts.Draw(ci, Math.Round(s.ShieldsUp ? s.Shields[i] : 0).ToString(), np.X, np.Y + (big ? 7 : 5), big ? 20 : 13, c.Text, HorizontalAlignment.Center, 0, 600);
        }

        // the ship from above: its schematic (two white layers, tinted here in the skin's colours: the hull dark, the wire
        // bright), or a stand-in by hull shape for a class whose ship is not designed yet
        float L = R * 0.72f;
        if (!Schematics.Draw(ci, Schematics.KeyOfModel(s.Class.Model), centre, L, c.Signal, c.Sky)) Schematic(ci, s.Class.Shape, centre, L, c.Sky);

        // subsystems in two columns
        for (int side = 0; side < 2; side++)
        {
            string[] list = side == 0 ? Left : Right;
            float sx = side == 0 ? b.Position.X : b.End.X - box;
            for (int i = 0; i < list.Length; i++)
            {
                string k = list[i];
                float top = big ? centre.Y - (list.Length * (box + 10)) / 2 : b.Position.Y + 4;
                var r = new Rect2(sx, top + i * (box + (big ? 10 : 6)), box, box);
                SystemStatus st = bt.Rules.Status(s, k);
                Color fill = StatusColorOf(c, st);
                string aim = k;
                var rows = new List<(string, string, Color?)> { ("Health", $"{Math.Round(s.Sys[k] * 100)} %", null), ("Status", st.ToString(), fill) };
                if (aims) rows.Add(("Click", bt.Player.Aim == k ? "Aimed here" : "Aim here · shift: details", null));
                HitState hs = ui.Reg($"{idp}.sys.{k}", r, aims ? _ => Aiming.Click(x, ui, aim) : null, null, new Tip(Title(k), rows));
                if (aims && bt.Player.Aim == k) x.Kit.Shape(ci, r.Grow(3), 9, c.Sky, 2.5f);
                StatusKey(x.Kit, ci, r, 6, fill, st);
                if (block != null && block.Sys.Contains(k)) Aiming.Bracket(ci, c, r, big ? 6 : 4, tk, bw);
                Color ink = st == SystemStatus.Destroyed && !glass ? c.Alert : KeyFace.Ink(x.Kit, fill, true);
                x.Kit.Icon(ci, SysIcon[k], glass ? r.Grow(-box * 0.21f) : r.Grow(-4), ink);
                if (hs.Hot) x.Kit.Shape(ci, r.Grow(2), 8, mark.Hover, 2);
            }
        }

        // the mounts: square boxes on the grid, drawn last, above the schematic
        float mb = big ? 28 : 22;
        var taken = new HashSet<(int, int)>();
        foreach (Mount m in s.Mounts)
        {
            float px = (float)(m.Def.X * L * 0.6), py = (float)(-m.Def.Y * L);
            (int, int) cell = ((int)MathF.Round(px / g, MidpointRounding.AwayFromZero), (int)MathF.Round(py / g, MidpointRounding.AwayFromZero));
            if (taken.Contains(cell)) cell = NearestFree(cell, taken, px >= 0 ? 1 : -1);
            taken.Add(cell);
            var r = new Rect2(centre + new Vector2(cell.Item1 * g - mb / 2, cell.Item2 * g - mb / 2), new Vector2(mb, mb));
            SystemStatus st = bt.Rules.Status(s, m);
            Color sc = StatusColorOf(c, st);
            double? cd = Rules.CooldownS(m);
            var rows = new List<(string, string, Color?)> { ("Health", $"{Math.Round(m.Hp * 100)} %", null), ("Status", st.ToString(), sc), ("Arming", m.State.ToString(), null) };
            if (cd is { } secs) rows.Add(("Ready in", Rules.FormatCooldown(secs) + " s", null));
            if (own && t != null) rows.Add(("To hit " + t.Name, $"{Math.Round(bt.Rules.HitChance(s, m, t) * 100)} %", null));
            if (own && !m.Weapon.IsPointDefence) rows.Add(("Aims", m.Weapon.Precise ? "Precise" : "By arc", null));
            Ship? pinned = own ? bt.PinTarget(m) : null;
            if (pinned != null) rows.Add(("Pinned", m.Pin!.Aim == "hull" ? pinned.Name : $"{pinned.Name} · {Strike.Name(m.Pin.Aim, pinned)}", null));
            int idx = m.Index;
            string mAim = $"w:{m.Index}";
            // your own weapon: a click selects it (shift+click its hit details); the locked target's: a click aims at it
            Action<Vector2>? click = pick != null ? _ => pick(m)
                : own ? (m.Weapon.IsPointDefence ? null : _ => { if (ui.Shift) x.State.OpenHitInfo(new HitInfo(null, idx)); else bt.ToggleMount(idx); })
                : mountAims ? _ => Aiming.Click(x, ui, mAim) : null;
            HitState hs = ui.Reg($"{idp}.m{m.Index}", r.Grow(2), click, null, new Tip($"{m.Label} {m.Weapon.Name}", rows));
            if (mountAims && bt.Player.Aim == mAim) ci.DrawArc(r.GetCenter(), mb / 2 + 6, 0, Mathf.Tau, 24, c.Sky, 2.5f, true);
            boxes.Add((m, r));
            if (big) x.Fonts.Draw(ci, m.Label, r.End.X + 5, r.Position.Y + 9, 12, c.Lilac, HorizontalAlignment.Left, 0, 600);
            if (hs.Hot) x.Kit.Shape(ci, r.Grow(5), 7, mark.Hover, 2);
            if (m.Selected && own) x.Kit.Shape(ci, r.Grow(3), 6, c.Sky);
            if (cd != null && st != SystemStatus.Destroyed)
            {
                // cooling: a darker shade (on glass, the unlit key) lit from the bottom up as it charges
                float f = (float)Math.Clamp(m.Charge, 0, 1);
                if (glass && st != SystemStatus.Off)
                {
                    x.Kit.Hero(ci, "key.off", r);
                    x.Kit.HeroBottom(ci, "key." + KeyFace.StateOf(c, sc, true), r, f);
                }
                else
                {
                    StatusKey(x.Kit, ci, r, 4, glass ? sc : sc.Darkened(0.66f), st);
                    if (f > 0.02f && !glass) Shapes.RoundRect(ci, new Rect2(r.Position.X, r.End.Y - r.Size.Y * f, r.Size.X, r.Size.Y * f), sc, 4);
                }
                x.Fonts.DrawOutlined(ci, Rules.FormatCooldown(cd.Value), r.GetCenter().X, r.GetCenter().Y + (big ? 6 : 5), big ? 16 : 13, mark.Count, new Color(0, 0, 0, 0.85f), 3);
            }
            else
            {
                StatusKey(x.Kit, ci, r, 4, sc, st);
                Glyph(ci, m.Weapon.Kind, r.GetCenter(), mb / 2 - 3, st == SystemStatus.Destroyed ? c.Alert : KeyFace.Ink(x.Kit, sc, true));
            }
            // a pinned mount: a small flag on its box's corner; a mount on the chopping block from you: its bracket
            if (pinned != null)
            {
                float fs = big ? 12 : 8;
                ci.DrawColoredPolygon(new[] { new Vector2(r.End.X - fs, r.Position.Y - 2), new Vector2(r.End.X + 3, r.Position.Y - 2), new Vector2(r.End.X + 3, r.Position.Y + fs) }, c.Alert);
            }
            if (block != null && block.Mounts.Contains(m.Index)) Aiming.Bracket(ci, c, r, big ? 6 : 4, tk, bw);
        }
        return boxes;
    }

    /// <summary>A status box: the key face lit in its status colour; on rendered glass an Off box is dimmed.</summary>
    private static void StatusKey(IKit kit, CanvasItem ci, Rect2 r, float radius, Color fill, SystemStatus st) =>
        KeyFace.Draw(kit, ci, r, radius, fill, true, kit.Style.RenderedKeys && st == SystemStatus.Off ? 0.7f : 1);

    private static (int, int) NearestFree((int, int) cell, HashSet<(int, int)> taken, int side)
    {
        for (int d = 1; d < 6; d++)
            foreach (var (dx, dy) in new[] { (side * d, 0), (0, d), (0, -d), (-side * d, 0), (side * d, d), (side * d, -d) })
                if (!taken.Contains((cell.Item1 + dx, cell.Item2 + dy))) return (cell.Item1 + dx, cell.Item2 + dy);
        return cell;
    }

    /// <summary>A weapon type's glyph (battle-console design): lance, torpedo, disruptor, missile, point defence.</summary>
    public static void Glyph(CanvasItem ci, string kind, Vector2 c, float r, Color ink)
    {
        switch (kind)
        {
            case "beam": ci.DrawCircle(c, r * 0.32f, ink, true, -1, true); ci.DrawLine(c - new Vector2(r, 0), c - new Vector2(r * 0.5f, 0), ink, 2, true); ci.DrawLine(c + new Vector2(r * 0.5f, 0), c + new Vector2(r, 0), ink, 2, true); break;
            case "torpedo": ci.DrawCircle(c, r * 0.3f, ink, true, -1, true); ci.DrawArc(c, r * 0.8f, 0, Mathf.Tau, 20, ink, 2, true); break;
            case "bolt": ci.DrawCircle(c - new Vector2(r * 0.42f, 0), r * 0.3f, ink, true, -1, true); ci.DrawCircle(c + new Vector2(r * 0.42f, 0), r * 0.3f, ink, true, -1, true); break;
            case "missile": Shapes.RoundRect(ci, new Rect2(c - new Vector2(r * 0.3f, r * 0.85f), new Vector2(r * 0.6f, r * 1.7f)), ink, r * 0.3f); break;
            // the special weapons (special-weapons design: a beam with strands, a lattice ball, a spike)
            case "chorus":
                ci.DrawLine(c - new Vector2(r, 0), c + new Vector2(r, 0), ink, 2, true);
                for (int s = -1; s <= 1; s += 2)
                {
                    var pts = new Vector2[9];
                    for (int i = 0; i < 9; i++) { float u = i / 8f; pts[i] = c + new Vector2(-r + 2 * r * u, s * r * 0.45f * Mathf.Sin(u * Mathf.Tau)); }
                    ci.DrawPolyline(pts, ink, 1.5f, true);
                }
                break;
            case "quell":
            {
                var hex = new Vector2[7];
                for (int i = 0; i < 7; i++) { float a = i * Mathf.Tau / 6; hex[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.8f; }
                ci.DrawPolyline(hex, ink, 2, true);
                ci.DrawCircle(c, r * 0.25f, ink, true, -1, true);
                break;
            }
            case "slug": ci.DrawColoredPolygon(new[] { c + new Vector2(r, 0), c + new Vector2(-r * 0.7f, -r * 0.45f), c + new Vector2(-r * 0.7f, r * 0.45f) }, ink); break;
            default:
                ci.DrawCircle(c, r * 0.25f, ink, true, -1, true);
                for (int i = 0; i < 3; i++) { float a = -Mathf.Pi / 2 + i * Mathf.Tau / 3; ci.DrawArc(c, r * 0.75f, a - 0.6f, a + 0.6f, 8, ink, 2, true); }
                break;
        }
    }

    // A stand-in schematic by hull shape: saucer and nacelles, a wedge, or a claw (mockup 5 sets the real silhouettes).
    private static void Schematic(CanvasItem ci, string shape, Vector2 c, float L, Color ink)
    {
        switch (shape)
        {
            case "wedge":
                ci.DrawPolyline(new[] { c + new Vector2(0, -L), c + new Vector2(L * 0.55f, L * 0.8f), c + new Vector2(0, L * 0.55f), c + new Vector2(-L * 0.55f, L * 0.8f), c + new Vector2(0, -L) }, ink, 2.5f, true);
                break;
            case "claw":
                ci.DrawPolyline(new[] { c + new Vector2(-L * 0.25f, -L), c + new Vector2(-L * 0.5f, 0), c + new Vector2(-L * 0.2f, L * 0.8f), c + new Vector2(L * 0.2f, L * 0.8f), c + new Vector2(L * 0.5f, 0), c + new Vector2(L * 0.25f, -L) }, ink, 2.5f, true);
                break;
            default:
                ci.DrawArc(c + new Vector2(0, -L * 0.45f), L * 0.5f, 0, Mathf.Tau, 40, ink, 2.5f, true);
                Shapes.RoundRectLine(ci, new Rect2(c + new Vector2(-L * 0.1f, -L * 0.05f), new Vector2(L * 0.2f, L * 0.6f)), ink, L * 0.1f, 2.5f);
                Shapes.RoundRectLine(ci, new Rect2(c + new Vector2(-L * 0.62f, L * 0.05f), new Vector2(L * 0.16f, L * 0.8f)), ink, L * 0.08f, 2.5f);
                Shapes.RoundRectLine(ci, new Rect2(c + new Vector2(L * 0.46f, L * 0.05f), new Vector2(L * 0.16f, L * 0.8f)), ink, L * 0.08f, 2.5f);
                ci.DrawLine(c + new Vector2(-L * 0.1f, L * 0.3f), c + new Vector2(-L * 0.46f, L * 0.36f), ink, 2.5f, true);
                ci.DrawLine(c + new Vector2(L * 0.1f, L * 0.3f), c + new Vector2(L * 0.46f, L * 0.36f), ink, 2.5f, true);
                break;
        }
    }

    private static string Title(string k) => char.ToUpperInvariant(k[0]) + k[1..];

    public static Color StatusColorOf(Kit.Palette c, SystemStatus s) => s switch
    {
        SystemStatus.Active => c.Accord,
        SystemStatus.Damaged => c.Caution,
        SystemStatus.Disabled => c.Alert,
        SystemStatus.Off => c.Lilac.Darkened(0.35f),
        _ => c.Alert.Darkened(0.6f),
    };

    public static Color ShieldColorOf(Kit.Palette c, double frac) => frac > 0.66 ? c.Accord : frac > 0.33 ? c.Caution : c.Alert;
}
