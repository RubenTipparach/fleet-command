// StationPanel.Tactical: the Tactical station (openspec/changes/weapon-targeting; the battle console mockup's stTactical,
// aimKeys, mountKeys and tacMatrix). Docked: the Aim at keys (the hull and each subsystem, lit in the locked target's
// status for it, the aim ringed, a bracket over each one the facing toward you exposes), the contacts nearest first, and
// Pin weapons and Next target. Expanded (owner, 2026-11-04: "there should be an expand button to view viable targets to
// shoow with various weapons"; "I expect to be able to target individual weapons as well"): the target's weapons as aim
// keys too, more contacts on the left, and on the right a grid of your weapons against the contacts, each cell the chance
// to hit now, a click pinning that weapon to that contact. Every number is the simulation's (Rules, Strike, Battle).
using System;
using System.Linq;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel
{
    partial void TacticalPage(Rect2 b, ref bool drawn)
    {
        drawn = true;
        Ship s = Me;
        float x = b.Position.X, y = b.Position.Y, lw = Big ? MathF.Round(b.Size.X * 0.4f) : b.Size.X;
        Lbl("Aim at", x, y + 13);
        Lbl("Shift+click: hit details", x + lw, y + 13, lw * 0.6f, null, HorizontalAlignment.Right, 12);
        y += 20;
        AimKeys(x, y, lw, 40);
        y += 46;
        if (Big) { Lbl("Aim at a weapon", x, y + 13); y += 20; y += MountKeys(x, y, lw, 40) + 8; }
        Lbl("Contacts", x, y + 13);
        y += 20;
        // as many contacts as fit above the keys at the foot (five docked, twelve expanded at most)
        float foot = b.End.Y - 48 - 6;
        int fit = Math.Max(0, (int)((foot - y + 4) / 44));
        Ship[] foes = B.SeenFoes(s).OrderBy(o => Rules.DistKm(s, o)).ThenBy(o => o.Order).Take(Math.Min(Big ? 12 : 5, fit)).ToArray();
        foreach (Ship f in foes) { Contact(f, x, y, lw); y += 44; }
        if (foes.Length == 0) Lbl("No contacts", x, y + 14);
        if (Big) Matrix(x + lw + 24, b.Position.Y, b.Size.X - lw - 24, b.Size.Y - 60, B.SeenFoes(s).OrderBy(o => Rules.DistKm(s, o)).ThenBy(o => o.Order).Take(6).ToArray());
        float hw = (lw - 6) / 2, by = b.End.Y - 48;
        Ship? t = B.TargetOf(s);
        Button("tac.pin", new Rect2(x, by, hw, 48), "Pin weapons", () => B.PinSelected(),
            dis: t == null || t.Side == s.Side || !s.Mounts.Any(m => m.Selected && !m.Weapon.IsPointDefence), tip: new Tip("Pin the selected weapons to the locked target"));
        Button("tac.next", new Rect2(x + hw + 6, by, hw, 48), "Next target", () => B.NextTarget(), tip: new Tip("Lock the next enemy (Tab)"));
    }

    // one contact: its dot in its colour, its name (and class, expanded), its bearing and its range; a click locks it
    private void Contact(Ship f, float x, float y, float w)
    {
        Ship s = Me;
        bool on = s.Target == f.Id;
        var r = new Rect2(x, y, w, 40);
        string id = f.Id;
        HitState st = Ui.Reg($"ct.{f.Id}", r, _ => B.Lock(id), null, new Tip(Aiming.ContactName(B, f), new (string, string, Color?)[] { ("Class", Aiming.ContactClass(B, f), null), ("Range", $"{Rules.DistKm(s, f):0.0} km", null) }));
        if (on || st.Hot) Kit.Shape(this, r, 20, on ? C.Deep : new Color(C.Deep, 0.5f));
        DrawCircle(new Vector2(x + 18, y + 20), 7, Aiming.ContactColor(C, s, f), true, -1, true);
        Text(Big ? $"{Aiming.ContactName(B, f)} · {Aiming.ContactClass(B, f)}" : Aiming.ContactName(B, f), x + 34, y + 27, 20, C.Text, HorizontalAlignment.Left, w - 34 - 150);
        Lbl($"{Geometry.HeadingDeg(Rules.Bearing(s, f.X, f.Z)):000}", x + w - 96, y + 26, 0, C.Lilac, HorizontalAlignment.Right);
        Num($"{Rules.DistKm(s, f):0.0} km", x + w - 8, y + 28, 0, C.Sky, HorizontalAlignment.Right, 20);
    }

    // The Aim at keys (owner, 2026-11-04: "this aim at should be icon based", "aim at should show status of subsystems"): one
    // key for the hull and each subsystem, lit in the locked target's status for it, the aim ringed, a bracket over each one
    // the facing toward you exposes; a click aims, shift+click opens the hit details.
    private void AimKeys(float x, float y, float w, float h)
    {
        Ship s = Me;
        Ship? t = B.TargetOf(s) is { } lt && lt.Side != s.Side ? lt : null;
        var aims = Strike.Aims;
        int n = aims.Length;
        float gap = 4, bs = Math.Min(h, (w - gap * (n - 1)) / n);
        Exposure? ex = t != null ? B.ExposureFrom(t, s) : null;
        for (int i = 0; i < n; i++)
        {
            var (k, name, icon) = aims[i];
            var r = new Rect2(x + i * (bs + gap), y, bs, bs);
            double hp = t == null ? 0 : k == "hull" ? t.Hull / t.Class.HullHp : Math.Clamp(t.Sys[k], 0, 1);
            SystemStatus st = t == null ? SystemStatus.Off : k == "hull" ? Aiming.HullStatus(B, t) : B.Rules.Status(t, k);
            bool onBlock = ex != null && (k == "hull" || ex.Sys.Contains(k));
            string aim = k;
            var rows = t != null
                ? new (string, string, Color?)[] { ("Health", Pct(hp), null), ("Status", st.ToString(), StatusColor(st)),
                    ("On the block", k == "hull" ? "Always" : onBlock ? "Yes" : "Not from here", onBlock ? C.Accord : C.Caution), ("Click", "Aim · shift: details", null) }
                : new (string, string, Color?)[] { ("Target", "None locked", null), ("Click", "Aim · shift: details", null) };
            HitState hs = Ui.Reg($"aim.{k}", r, _ => Aiming.Click(X, Ui, aim), null, new Tip(name, rows));
            AimFace(r, bs, st, s.Aim == k, hs.Hot);
            Color fill = StatusColor(st);
            Kit.Icon(this, icon, r.Grow(-bs * (Sk.RenderedKeys ? 0.21f : 0.15f)), KeyFace.Ink(Kit, fill, true));
            if (onBlock && k != "hull") Aiming.Bracket(this, C, r, 5, 4, 1.6f);
        }
    }

    // an aim key's face: the ring when it is the aim, the status box, the hover sheen
    private void AimFace(Rect2 r, float bs, SystemStatus st, bool aimed, bool hot)
    {
        if (aimed) Kit.Shape(this, r.Grow(3), bs * 0.28f + 3, C.Sky);
        KeyFace.Draw(Kit, this, r, bs * 0.28f, StatusColor(st), true, Sk.RenderedKeys && st == SystemStatus.Off ? 0.7f : 1);
        if (hot) Kit.Shape(this, r, bs * 0.28f, new Color(1, 1, 1, 0.12f));
    }

    // The target's weapons as aim keys (expanded): one a mount, lit in its status, its weapon's glyph and id on it, the aim
    // ringed, a bracket over each one on the block; returns the height used.
    private float MountKeys(float x, float y, float w, float h)
    {
        Ship s = Me;
        if (B.TargetOf(s) is not { } t || t.Side == s.Side) { Lbl("Lock a target to aim at its weapons", x, y + 22, w, Sk.R.Ghost, HorizontalAlignment.Left, 15); return 30; }
        int n = t.Mounts.Count;
        float gap = 4, bs = Math.Min(h, (w - gap * (n - 1)) / Math.Max(1, n));
        Exposure ex = B.ExposureFrom(t, s);
        foreach (Mount m in t.Mounts)
        {
            var r = new Rect2(x + m.Index * (bs + gap), y, bs, bs);
            string aim = $"w:{m.Index}";
            SystemStatus st = B.Rules.Status(t, m);
            bool on = ex.Mounts.Contains(m.Index);
            HitState hs = Ui.Reg($"aim.{aim}", r, _ => Aiming.Click(X, Ui, aim), null, new Tip($"{m.Label} · {m.Weapon.Name}", new (string, string, Color?)[]
            {
                ("Health", Pct(Math.Clamp(m.Hp, 0, 1)), null), ("Status", st.ToString(), StatusColor(st)), ("Power", m.Weapon.OwnPower ? "Its own" : "Weapons control", null),
                ("On the block", on ? "Yes" : "Not from here", on ? C.Accord : C.Caution), ("Click", "Aim · shift: details", null),
            }));
            AimFace(r, bs, st, s.Aim == aim, hs.Hot);
            Color ink = KeyFace.Ink(Kit, StatusColor(st), true);
            SystemsDisplay.Glyph(this, m.Weapon.Kind, new Vector2(r.GetCenter().X, y + bs * 0.42f), bs * 0.24f, ink);
            Text(m.Label, r.GetCenter().X, y + bs - 4, 11, ink, HorizontalAlignment.Center, bs - 4);
            if (on) Aiming.Bracket(this, C, r, 5, 4, 1.6f);
        }
        return bs;
    }

    // The weapons against contacts (expanded): a row per weapon (Prec or Arc), a column per contact (six at most), each cell
    // a key showing the chance to hit now, a dot when out of arc or range; viable cells lit, pinned cells engaged. A click
    // pins that weapon to that contact, again unpins it.
    private void Matrix(float x, float y, float w, float h, Ship[] cols)
    {
        Ship s = Me;
        Mount[] ms = s.Mounts.Where(m => !m.Weapon.IsPointDefence).ToArray();
        float rh = Math.Min(48, (h - 70) / Math.Max(1, ms.Length)), cw0 = 170, cw = (w - cw0) / Math.Max(1, cols.Length);
        Lbl("Weapons against contacts · click to pin", x, y + 13, w);
        y += 22;
        for (int j = 0; j < cols.Length; j++)
            Text(cols[j].Name, x + cw0 + j * cw + cw / 2, y + 18, 16, s.Target == cols[j].Id ? C.Sky : C.Text, HorizontalAlignment.Center, cw - 8);
        y += 28;
        foreach (Mount m in ms)
        {
            Text($"{m.Label} {m.Weapon.Name}", x, y + rh * 0.62f, 16, C.Text, HorizontalAlignment.Left, cw0 - 34);
            Lbl(m.Weapon.Precise ? "Prec" : "Arc", x + cw0 - 8, y + rh * 0.62f, 0, C.Lilac, HorizontalAlignment.Right, 11);
            foreach (Ship f in cols)
            {
                int j = Array.IndexOf(cols, f);
                var r = new Rect2(x + cw0 + j * cw + 3, y + 2, cw - 6, rh - 4);
                bool ok = Rules.InArc(s, m, f) && Rules.InRange(s, m, f), pinned = m.Pin?.To == f.Id;
                double hc = ok ? B.Rules.HitChance(s, m, f, pinned ? m.Pin!.Aim : s.Aim) : 0;
                int mi = m.Index;
                string fid = f.Id;
                HitState st = Ui.Reg($"tm.{m.Index}.{f.Id}", r, _ => B.Pin(mi, fid), null,
                    new Tip($"{m.Label} on {f.Name}", new (string, string, Color?)[] { ("To hit", ok ? Pct(hc) : "out of arc or range", null), ("Click", pinned ? "pinned · unpin" : "pin", null) }));
                Color fill = pinned ? C.Lilac : st.Hot ? C.Sky : ok ? C.Signal : C.Deep;
                KeyFace.Draw(Kit, this, r, r.Size.Y * 0.22f, fill);
                Num(ok ? Pct(hc) : "·", r.GetCenter().X, y + rh * 0.66f, r.Size.X - 6, KeyFace.Ink(Kit, fill), HorizontalAlignment.Center, 17);
            }
            y += rh;
        }
        Button("tm.unpin", new Rect2(x, y + 10, 180, 36), "Unpin all", () => B.UnpinAll(), dis: !s.Mounts.Any(m => m.Pin != null));
    }
}
