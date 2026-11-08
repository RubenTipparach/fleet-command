// StationPanel.Transport: the Transport station (the battle console mockup's stTransport, queueGrid, padRow, controlRow,
// partyPanel and holdBar; owner, 2026-11-01: "Transporters ui have 3 modes - capture transport mode, target hit and turn
// mode, and inventory transporter mode"). Capture and Hit and run share one layout (owner, 2026-11-02: "top half queuing
// mechanism and rolls to queue, bottom half the pads to energize, or energize all and the portrait with a control bar"):
// the role keys and the queue above, the pads and the target's portrait with the tug of war below; Hit and run adds the
// target's subsystems as keys, the weapon pick and Shoot or Bomb; expanded, Capture shows the party's marines' health and
// the boarding log. Cargo moves supplies, trade goods and the envoy to or from the friendly ship, station or planet
// selected. Mines and Security are other systems' tabs, drawn by their own files when built. Every readout calls the rule
// the battle uses (Battle.Transport), and everything is drawn with the kit, so every skin draws it; a part that does not
// fit the docked panel is left out rather than spilling (the panel is a fixed size).
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel
{
    private static readonly (string Id, string Label)[] TransportTabs =
        { ("capture", "Capture"), ("hit", "Hit & run"), ("cargo", "Cargo"), ("mines", "Mines"), ("security", "Security") };
    private static readonly Dictionary<string, Texture2D?> EmblemTex = new();

    /// <summary>The Mines tab (mines beamed out by the transporter), drawn by the mines' own file once built.</summary>
    partial void TransportMinesTab(Rect2 b, ref bool drawn);
    /// <summary>The Security tab (boarding defence), drawn by its own file once built.</summary>
    partial void TransportSecurityTab(Rect2 b, ref bool drawn);

    partial void TransportPage(Rect2 b, ref bool drawn)
    {
        drawn = true;
        float x = b.Position.X, y = b.Position.Y, w = b.Size.X, gap = Big ? 14 : 8;
        string cur = X.State.TransportTab;
        void Set(string k) { X.State.TransportTab = k; X.State.RaidMountOpen = false; }
        // one row expanded, two rows docked so every name reads in full
        if (Big) { Tabs("tr.tab", new Rect2(x, y, w, 34), TransportTabs, cur, Set); y += 34 + gap; }
        else
        {
            Tabs("tr.tab", new Rect2(x, y, w, 30), TransportTabs[..2], cur, Set);
            Tabs("tr.tab", new Rect2(x, y + 34, w, 30), TransportTabs[2..], cur, Set);
            y += 64 + gap;
        }
        var rest = new Rect2(x, y, w, b.End.Y - y);
        bool done = false;
        switch (cur)
        {
            case "security": TransportSecurityTab(rest, ref done); if (!done) NotBuilt(rest, "Security", "transport"); break;
            case "mines": TransportMinesTab(rest, ref done); if (!done) NotBuilt(rest, "Mines", "transport"); break;
            case "cargo": Cargo(rest); break;
            default: Boarding(rest, cur); break;
        }
    }

    // ---- Capture and Hit and run -------------------------------------------------------------------------------------

    private void Boarding(Rect2 b, string mode)
    {
        Ship s = Me;
        Ship? t = B.TargetOf(s);
        float x = b.Position.X, y = b.Position.Y, w = b.Size.X, gap = Big ? 14 : 8;
        Rect2? pick = null;
        if (mode == "hit")
        {
            // the target's subsystems as keys, coloured by their status; the picked one ringed in Sky
            string[] subs = Rules.Subsystems;
            float bs = Math.Min(Big ? 56 : 34, (w - (subs.Length - 1) * 6) / subs.Length);
            for (int i = 0; i < subs.Length; i++)
            {
                string key = subs[i];
                var r = new Rect2(x + i * (bs + 6), y, bs, bs);
                SystemStatus st = t != null ? B.Rules.Status(t, key) : SystemStatus.Off;
                if (s.RaidSys == key) Kit.Shape(this, r.Grow(3), bs * 0.28f + 3, C.Sky);
                Color fill = StatusColor(st);
                Ui.Reg($"tr.sys.{key}", r, _ => { B.AimRaid(key); if (key != "weapons") X.State.RaidMountOpen = false; }, null,
                    new Tip(Battle.SubsystemNames[key], t != null ? new (string, string, Color?)[] { ("Health", Pct(Math.Clamp(t.Sys[key], 0, 1)), null), ("Status", st.ToString(), fill) }
                        : new (string, string, Color?)[] { ("Target", "--", null) }));
                KeyFace.Draw(Kit, this, r, bs * 0.28f, fill, true);
                float ik = Sk.RenderedKeys ? 0.21f : 0.15f;
                Kit.Icon(this, key, r.Grow(-bs * ik), KeyFace.Ink(Kit, fill, true));
            }
            // weapons: which one (owner: "need a drop down or something to pick from weapons"): a pill showing the pick,
            // its list opening over the queue. Docked, the pill sits in the room left of the subsystem row, so picking
            // weapons never pushes the panel's content past its bottom (owner, 2026-11-08: "weapn controls can push stuff
            // off screen"); expanded, it has its own row.
            float keysW = subs.Length * bs + (subs.Length - 1) * 6, room = w - keysW - 8;
            bool inline = !Big && room >= 72;
            if (!inline) y += bs + gap;
            if (s.RaidSys == "weapons")
            {
                float ph = inline ? bs : Big ? 40 : 30, px = inline ? x + keysW + 8 : x, pw = inline ? room : w;
                Mount? m = t != null && s.RaidMount is { } mi && mi < t.Mounts.Count ? t.Mounts[mi] : null;
                string full = m != null ? $"{m.Label} · {m.Weapon.Name} · {Pct(Math.Clamp(m.Hp, 0, 1))}" : "Weapons control (any weapon)";
                var r = new Rect2(px, y, pw, ph);
                HitState st = Ui.Reg("tr.wpick", r, _ => X.State.RaidMountOpen = !X.State.RaidMountOpen, null, new Tip(inline ? full : "Pick the weapon to raid"));
                Color wf = st.Hot || X.State.RaidMountOpen ? C.Sky : C.Signal, ink = KeyFace.Ink(Kit, wf);
                KeyFace.Draw(Kit, this, r, ph / 2, wf);
                string lab = inline ? (m != null ? $"{m.Label} · {Pct(Math.Clamp(m.Hp, 0, 1))}" : "Any") : full;
                Text(lab, px + (inline ? 12 : 16), y + ph * 0.66f, Big ? 20 : 16, ink, HorizontalAlignment.Left, pw - (inline ? 34 : 48));
                float ax = px + pw - (inline ? 14 : 20), ay = y + ph / 2;
                Pen.FillAbs(this, X.State.RaidMountOpen
                    ? new[] { new Vector2(ax - 6, ay + 3), new Vector2(ax + 6, ay + 3), new Vector2(ax, ay - 4) }
                    : new[] { new Vector2(ax - 6, ay - 3), new Vector2(ax + 6, ay - 3), new Vector2(ax, ay + 4) }, ink);
                pick = new Rect2(x, y + ph + 4, w, b.End.Y - (y + ph + 4) - 4);
                if (!inline) y += ph + gap;
            }
            if (inline) y += bs + gap;
        }
        // the role keys (Hit and run: Shoot and Bomb share their row)
        string[] roles = Battle.QueueRoles[mode];
        float rs = Math.Min(Big ? 96 : 42, (w - 4 * 6) / 5);
        if (mode == "hit") Tabs("tr.kind", new Rect2(x, y + (rs - 32) / 2, w - 2 * (rs + 6) - 6, 32), new[] { ("shoot", "Shoot"), ("bomb", "Bomb") }, s.RaidKind, B.RaidKind);
        float rx = mode == "hit" ? x + w - roles.Length * (rs + 6) + 6 : x;
        Party? on = s.Party != null && t != null && s.Party.On == t.Id ? s.Party : null;
        string[] needR = mode == "capture" && on != null && Battle.Defenders(t!) <= 0 ? Battle.MissingRoles(on).Where(r => !s.Queue["capture"].Contains(r)).ToArray() : Array.Empty<string>();
        for (int i = 0; i < roles.Length; i++)
        {
            string r = roles[i];
            int have = Battle.Have(s, r);
            IconKey($"tr.q.{mode}.{r}", new Rect2(rx + i * (rs + 6), y, rs, rs), (cx, cy, rr, col) => TransportGlyphs.Role(this, r, cx, cy, rr, col), have,
                () => B.QueueRole(mode, r), needR.Contains(r), false, new Tip($"Queue {Battle.RoleOne[r]} · {have} aboard"));
        }
        y += rs + gap;
        y += QueueGrid($"tr.qg.{mode}", x, y, w, s, mode) + gap + (Big ? 10 : 4);
        // the line between the halves
        DrawRect(new Rect2(x, y - gap / 2 - (Big ? 5 : 2), w, 2), C.Deep);
        y += PadRow($"tr.pad.{mode}", x, y, w, s, mode) + gap;
        y += ControlRow($"tr.ctl.{mode}", x, y, w, b.End.Y - y, s, mode == "capture" ? B.CaptureDest(s) : t, mode) + gap;
        // expanded Capture: the party's marines' health and the boarding log
        if (Big && mode == "capture") PartyPanel(x, y, w, b.End.Y - y - 10, s);
        if (mode == "hit" && X.State.RaidMountOpen && pick is { } pk) WeaponPick(b, pk, t);
    }

    // The weapon list over the queue: weapons control, then each of the target's weapons by its id, name, health and status.
    private void WeaponPick(Rect2 b, Rect2 at, Ship? t)
    {
        float rh = Big ? 38 : 28;
        Ui.Reg("tr.wpick.off", b, _ => X.State.RaidMountOpen = false);
        var list = new List<Mount?> { null };
        if (t != null) list.AddRange(t.Mounts);
        float lh = Math.Min(list.Count * rh + 8, at.Size.Y);
        var box = new Rect2(at.Position, new Vector2(at.Size.X, lh));
        Shapes.RoundRect(this, box, new Color(0, 0, 0, 0.92f), 10);
        Shapes.RoundRectLine(this, box, C.Sky, 10, 2);
        for (int k = 0; k < list.Count; k++)
        {
            float ry = box.Position.Y + 4 + k * rh;
            if (ry + rh > box.End.Y) break;
            Mount? m = list[k];
            bool sel = m != null ? Me.RaidMount == m.Index : Me.RaidMount == null;
            var r = new Rect2(box.Position.X + 4, ry, box.Size.X - 8, rh - 2);
            HitState st = Ui.Reg($"tr.wpick.{k}", r, _ => { B.AimRaid("weapons", m?.Index); X.State.RaidMountOpen = false; });
            if (st.Hot || sel) KeyFace.Draw(Kit, this, r, (rh - 2) / 2, sel ? C.Signal : C.Deep);
            Color ink = sel ? KeyFace.Ink(Kit, C.Signal) : C.Text;
            int fs = Big ? 19 : 15;
            float by = ry + rh * 0.68f, lx = box.Position.X + 16, lw = box.Size.X;
            if (m == null) { Text("Weapons control (any weapon)", lx, by, fs, ink, HorizontalAlignment.Left, lw - 32); continue; }
            SystemStatus ms = B.Rules.Status(m);
            Text(m.Label, lx, by, fs, sel ? ink : C.Sky);
            Text(m.Weapon.Name, lx + fs * 2.2f, by, fs, ink, HorizontalAlignment.Left, lw * 0.45f);
            Text(Pct(Math.Clamp(m.Hp, 0, 1)), box.End.X - 16 - fs * 4.6f, by, fs, ink, HorizontalAlignment.Right);
            Text(ms.ToString(), box.End.X - 16, by, fs, sel ? ink : StatusColor(ms), HorizontalAlignment.Right, fs * 4.2f);
        }
    }

    /// <summary>An icon key: lit blue with its pictogram in the key's ink and a count in its corner; dark when there is
    /// none; ringed in Caution when wanted. The role keys, the cargo keys and the queue share it (6.1).</summary>
    private void IconKey(string id, Rect2 r, Action<float, float, float, Color> glyph, int count, Action click, bool ring, bool disabled, Tip tip)
    {
        bool dis = count <= 0 || disabled;
        float sz = r.Size.X;
        HitState st = dis ? default : Ui.Reg(id, r, _ => click(), null, tip);
        if (dis) Ui.Reg(id, r, null, null, tip);
        if (ring) Kit.Shape(this, r.Grow(3), sz * 0.22f + 3, C.Caution, 3);
        Color fill = dis ? C.Deep : st.Down ? C.Lilac : st.Hot ? C.Sky : C.Signal;
        KeyFace.Draw(Kit, this, r, sz * 0.22f, fill, false, dis ? 0.5f : 1);
        Color ink = dis ? Sk.R.InkDis : KeyFace.Ink(Kit, fill);
        glyph(r.Position.X + sz * 0.44f, r.Position.Y + sz * 0.44f, sz * 0.3f, ink);
        Text(count.ToString(), r.End.X - sz * 0.08f, r.End.Y - sz * 0.07f, Math.Max(9, (int)MathF.Round(sz * 0.27f)), ink, HorizontalAlignment.Right, 0, 700);
    }

    // The queue (owner: "a group of boxes with some scrolling arrows that light up if it can page up or page down on the
    // list", "10 boxes for you to queue"): two rows of five, the head in green, a box clicked takes its member off the
    // queue; with nothing queued, the default load (marines one a pad) shown darkened. Returns the height used.
    private float QueueGrid(string id, float x, float y, float w, Ship s, string mode)
    {
        List<string> q = s.Queue[mode];
        string[] dflt = Battle.DefaultLoad(s);
        const int per = 10, cols = 5;
        float gap = Big ? 10 : 5, aw = Big ? 60 : 30;
        int pages = Math.Max(1, (q.Count + per - 1) / per), pg = Math.Clamp(X.State.QueuePage[mode], 0, pages - 1);
        X.State.QueuePage[mode] = pg;
        float qs = Math.Min(Big ? 96 : 38, (w - aw - gap - (cols - 1) * gap) / cols);
        int fs = Math.Max(9, (int)MathF.Round(qs * 0.26f));
        for (int k = 0; k < per; k++)
        {
            int i = pg * per + k;
            var r = new Rect2(x + k % cols * (qs + gap), y + k / cols * (qs + gap), qs, qs);
            if (i >= q.Count)
            {
                KeyFace.Draw(Kit, this, r, qs * 0.22f, Sk.R.Night);
                if (!Sk.RenderedKeys) Kit.Shape(this, r, qs * 0.22f, C.Deep, 2);
                if (q.Count == 0 && i < dflt.Length)
                {
                    Ui.Reg($"{id}.{k}", r, null, null, new Tip($"{i + 1}. Marine · by default · queue to change"));
                    TransportGlyphs.Role(this, "marines", r.GetCenter().X, r.Position.Y + qs * 0.54f, qs * 0.3f, new Color(Sk.GlyphOnDark, 0.45f));
                    Text((i + 1).ToString(), r.Position.X + qs * 0.09f, r.Position.Y + qs * 0.3f, fs, Sk.R.Ghost, HorizontalAlignment.Left, 0, 700);
                }
                else Text((i + 1).ToString(), r.GetCenter().X, r.Position.Y + qs * 0.62f, Math.Max(9, (int)(qs * 0.36f)), Sk.R.Ghost, HorizontalAlignment.Center);
                continue;
            }
            string role = q[i];
            int idx = i;
            HitState st = Ui.Reg($"{id}.{k}", r, _ => B.Unqueue(mode, idx), null, new Tip($"{i + 1}. {Battle.RoleOne[role]}{(i == 0 ? " · next" : "")} · remove"));
            Color qf = st.Hot ? C.Lilac : i == 0 ? C.Accord : C.Signal, ink = KeyFace.Ink(Kit, qf);
            KeyFace.Draw(Kit, this, r, qs * 0.22f, qf);
            TransportGlyphs.Role(this, role, r.GetCenter().X, r.Position.Y + qs * 0.54f, qs * 0.3f, ink);
            Text((i + 1).ToString(), r.Position.X + qs * 0.09f, r.Position.Y + qs * 0.3f, fs, ink, HorizontalAlignment.Left, 0, 700);
        }
        // the page arrows: lit only when there is a page that way
        float ax = x + cols * (qs + gap);
        void Arrow(string k, float ay, bool up, bool on)
        {
            var r = new Rect2(ax, ay, aw, qs);
            HitState st = on ? Ui.Reg($"{id}.{k}", r, _ => X.State.QueuePage[mode] += up ? -1 : 1, null, new Tip(up ? "Page up" : "Page down")) : default;
            Color af = on ? (st.Hot ? C.Sky : C.Signal) : Sk.R.Night;
            KeyFace.Draw(Kit, this, r, aw * 0.3f, af);
            float c = ax + aw / 2, m = ay + qs / 2, d = Math.Min(aw, qs) * 0.22f;
            Color ink = on ? KeyFace.Ink(Kit, af) : Sk.R.Ghost;
            Pen.FillAbs(this, up ? new[] { new Vector2(c - d * 1.3f, m + d * 0.7f), new Vector2(c + d * 1.3f, m + d * 0.7f), new Vector2(c, m - d * 0.8f) }
                : new[] { new Vector2(c - d * 1.3f, m - d * 0.7f), new Vector2(c + d * 1.3f, m - d * 0.7f), new Vector2(c, m + d * 0.8f) }, ink);
        }
        Arrow("up", y, true, pg > 0);
        Arrow("dn", y + qs + gap, false, pg < pages - 1);
        return 2 * qs + gap;
    }

    // The pads (owner: "bottom half the pads to energize, or energize all"): each a key that fills as it charges, in the
    // ready colour (owner, 2026-11-02: "I prefer recharge to be same color as the standard value"), lit when ready, the
    // seconds left over it; clicked, it beams the head of the queue. Energize all beside them. Returns the height used.
    private float PadRow(string id, float x, float y, float w, Ship s, string mode)
    {
        bool q = mode != "inv";
        List<string>? queue = q ? s.Queue[mode] : null;
        int n = Math.Max(1, s.Pads.Length);
        float gap = Big ? 8 : 6;
        float ps = Math.Min(Big ? 110 : 40, (w - (q ? (Big ? 240 : 120) + gap : 0) - (n - 1) * gap) / n);
        float bw = q ? Math.Min(Big ? 300 : w, w - s.Pads.Length * (ps + gap)) : 0;
        bool glass = Sk.RenderedKeys;
        for (int i = 0; i < s.Pads.Length; i++)
        {
            var r = new Rect2(x + i * (ps + gap), y, ps, ps);
            double c = s.Pads[i];
            bool ready = c >= 1;
            double eta = B.PadEta(s, i);
            string tip = ready ? $"Pad {i + 1} · {(q ? $"energize · {(queue!.Count > 0 ? Battle.RoleOne[queue[0]] : "Marine (default)")}" : "charged")}"
                : $"Pad {i + 1} · charging{(double.IsFinite(eta) ? $" · {Rules.FormatCooldown(eta)} s" : " · no power")}";
            int pad = i;
            HitState st = Ui.Reg($"{id}.{i}", r, q ? _ => B.Energize(pad, mode) : null, null, new Tip(tip));
            Color pf = ready && st.Down ? C.Lilac : ready && st.Hot ? C.Sky : C.Signal;
            float f = (float)Math.Clamp(c, 0, 1);
            if (glass)
            {
                Kit.Hero(this, "key.off", r);
                if (f > 0.01f) Kit.HeroBottom(this, "key." + KeyFace.StateOf(C, pf, false), r, f);
            }
            else
            {
                Kit.Shape(this, r, ps * 0.22f, C.Deep);
                if (f > 0.02f) Shapes.RoundRect(this, new Rect2(r.Position.X, r.End.Y - ps * f, ps, ps * f), pf, Math.Min(ps * 0.22f, ps * f / 2));
            }
            TransportGlyphs.Pad(this, r.GetCenter().X, r.GetCenter().Y, ps, ready ? KeyFace.Ink(Kit, pf) : C.Sky);
            if (!ready)
                X.Fonts.DrawOutlined(this, double.IsFinite(eta) ? Rules.FormatCooldown(eta) : "--", r.GetCenter().X, r.GetCenter().Y + ps * 0.12f,
                    Math.Max(10, (int)MathF.Round(ps * 0.34f)), Sk.Mark.Count, new Color(0, 0, 0, 0.85f), 3);
            else if (Big && q)
            {
                // expanded: who this pad sends on Energize all (the queue dealt to the charged pads in order)
                int k = s.Pads.Take(i).Count(p => p >= 1);
                string? role = k < queue!.Count ? queue[k] : null;
                if (role != null)
                {
                    float cs = ps * 0.34f;
                    var cr = new Rect2(r.End.X - cs - 4, r.Position.Y + 4, cs, cs);
                    Color cf = k == 0 ? C.Accord : C.Deep;
                    KeyFace.Draw(Kit, this, cr, cs * 0.25f, cf);
                    TransportGlyphs.Role(this, role, cr.GetCenter().X, cr.GetCenter().Y, cs * 0.34f, k == 0 ? KeyFace.Ink(Kit, cf) : C.Sky);
                }
            }
        }
        float ex = x + s.Pads.Length * (ps + gap);
        if (q)
            Button($"{id}.all", new Rect2(ex + (Big ? 12 : 0), y + (Big ? ps * 0.2f : 0), bw - (Big ? 12 : 0), Big ? ps * 0.6f : ps), "Energize all", () => B.EnergizeAll(mode),
                dis: (queue!.Count == 0 && Battle.DefaultLoad(s).Length == 0) || !B.PadReady(s), tip: new Tip("Every charged pad, the queue in order"));
        else Lbl("Pads", ex + 4, y + ps / 2 + 6, w - (ex - x) - 4);
        return ps;
    }

    // The portrait and its control bar: who holds the ship (its emblem), the tug of war of our boarders against its
    // defenders with the round's clock, what the transporter needs, what a held deck still lacks, and Recall; for a
    // friendly ship, the troops it has against what it asked for; for a raid out, its clock and squad. Returns the height.
    private float ControlRow(string id, float x, float y, float w, float maxH, Ship s, Ship? t, string mode)
    {
        float pp = Big ? 180 : 58, range = (float)B.Comms.Transport.RangeKm;
        int ls = Big ? 17 : 14;
        if (maxH < 20) return 0;
        if (maxH < pp)
        {
            // too little room for the portrait (docked Hit and run with the weapon pick): the reach in one line
            if (t != null) Lbl(ReachText(s, t).Text, x, y + 14, w, ReachText(s, t).Col, HorizontalAlignment.Left, ls);
            return 20;
        }
        DrawRect(new Rect2(x + 2, y + 2, pp - 4, pp - 4), Colors.Black);
        Kit.Piece(this, "frame.inset", new Rect2(x, y, pp, pp));
        if (t != null)
        {
            string holder = t.Side == s.Side ? s.Faction.Id : t.Faction.Id;
            Ui.Reg($"{id}.por", new Rect2(x, y, pp, pp), null, null, new Tip($"{t.Name} · held by {B.Data.Factions[holder].Name}"));
            Emblem(holder, new Rect2(x + pp * 0.16f, y + pp * 0.12f, pp * 0.68f, pp * (Big ? 0.6f : 0.72f)));
            if (Big) Lbl(t.Name, x + pp / 2, y + pp - 12, pp - 12, C.Sky, HorizontalAlignment.Center, 16);
        }
        else Lbl("--", x + pp / 2, y + pp / 2 + 6, 0, Sk.R.Ghost, HorizontalAlignment.Center, 20);
        float bx = x + pp + 10, bw = w - pp - 10, yy = y, end = y + maxH;
        if (t == null) { Lbl(mode == "capture" ? "No target · or select a friendly for troops" : "No target", bx, yy + 20, bw); return pp; }
        double dk = Rules.DistKm(s, t);
        // a friendly ship: troops beamed over to it, its marines against what it asked for
        if (t.Side == s.Side)
        {
            int want = t.NeedMarines;
            Lbl($"Troops for {t.Name}", bx, yy + 16, bw, C.Accord, HorizontalAlignment.Left, ls); yy += 22;
            Num(want > 0 ? $"{t.Marines} of {want} marines" : $"{t.Marines} marines aboard", bx, yy + 18, bw, want > 0 && t.Marines < want ? C.Caution : C.Accord, HorizontalAlignment.Left, Big ? 22 : 17); yy += 26;
            Lbl(dk <= range ? $"In reach {dk:0.0} km" : $"Out of reach {dk:0.0} km", bx, yy + 14, bw, dk <= range ? C.Accord : C.Alert, HorizontalAlignment.Left, Big ? 17 : 15); yy += 20;
            if (Battle.PilotNeeded(t) && yy + 20 <= end) { Lbl("Pilot needed", bx, yy + 14, bw, C.Caution, HorizontalAlignment.Left, ls); yy += 20; }
            return Math.Max(pp, yy - y);
        }
        if (mode == "hit" && s.Raid is { } rd && rd.On == t.Id)
        {
            yy += Bar(bx, yy, bw, $"Raid {(rd.Kind == "bomb" ? "bombing" : "shooting")} {Battle.RaidName(rd, t)} · {Rules.FormatCooldown(Math.Max(0.05, rd.T))} s", 1 - rd.T / B.Comms.Board.RaidS, false) + 4;
            float cs = Big ? 34 : 24;
            if (yy + cs <= end) TeamChips(bx, yy, bw, new[] { ("marines", rd.Team["marines"]), ("demo", rd.Team["demo"]) }, C.Signal, cs);
            return Math.Max(pp, yy - y + cs);
        }
        Party? pt = s.Party != null && s.Party.On == t.Id ? s.Party : null;
        int ours = pt != null ? Battle.TeamSize(pt.Team) : 0, theirs = Battle.Defenders(t);
        float nh = Big ? 24 : 18;
        HitState us = Ui.Reg($"{id}.us", new Rect2(bx, yy, 70, nh + 4), null, null, new Tip(pt != null ? $"Our party aboard {t.Name}" : "No party aboard", pt != null ? PartyRows(pt) : Array.Empty<(string, string, Color?)>()));
        Num(ours.ToString(), bx, yy + nh, 0, us.Hot ? C.Text : C.Sky, HorizontalAlignment.Left, (int)nh + 2);
        Lbl(pt != null && theirs > 0 ? $"Volley in {Rules.FormatCooldown(Math.Max(0.05, pt.VolleyT))} s" : pt != null ? "Deck held" : "Control", bx + bw / 2, yy + nh - 2, bw - 140, C.Lilac, HorizontalAlignment.Center, ls);
        HitState them = Ui.Reg($"{id}.them", new Rect2(bx + bw - 70, yy, 70, nh + 4), null, null,
            new Tip($"{t.Name}'s defenders", new (string, string, Color?)[] { ("Marines", t.Marines.ToString(), null), ("Armed crew", t.Guard.ToString(), null) }));
        Num(theirs.ToString(), bx + bw, yy + nh, 0, them.Hot ? C.Text : C.Alert, HorizontalAlignment.Right, (int)nh + 2);
        yy += nh + 6;
        float th = Big ? 22 : 14;
        TugBar(bx, yy, bw, th, ours, theirs);
        yy += th + 3;
        // the round's clock, filling toward the next volley
        if (pt != null && theirs > 0)
        {
            DrawRect(new Rect2(bx, yy, bw, 3), C.Deep);
            DrawRect(new Rect2(bx, yy, bw * (float)Math.Clamp(1 - pt.VolleyT / B.Comms.Board.VolleyS, 0, 1), 3), C.Lilac);
        }
        yy += 6;
        // expanded: who is aboard on each side, as chips
        if (Big)
        {
            float hw = (bw - 12) / 2;
            float h1 = TeamChips(bx, yy, hw, pt != null ? PartyList(pt) : Array.Empty<(string, int)>(), C.Sky, 48);
            float h2 = TeamChips(bx + hw + 12, yy, hw, new[] { ("marines", t.Marines), ("guard", t.Guard) }, C.Alert, 48);
            yy += Math.Max(h1, h2) + 10;
        }
        // a deck held but not taken (owner, 2026-11-02: "boarding succeeded but control has not been changed"): what is
        // still missing, as role chips in Caution, so the captain knows whom to queue next
        string[] miss = pt != null && theirs <= 0 ? Battle.MissingRoles(pt) : Array.Empty<string>();
        float sz = Big ? 34 : 22;
        if (miss.Length > 0 && yy + sz <= end)
        {
            Lbl("To take it, send", bx, yy + sz * 0.72f, bw * 0.45f, C.Caution, HorizontalAlignment.Left, ls);
            TeamChips(bx + bw * 0.45f, yy, bw * 0.55f, miss.Select(r => (r, 1)).ToArray(), C.Caution, sz);
            yy += sz + 6;
        }
        var need = ReachText(s, t);
        float rh = Big ? 34 : 26, rw = Big ? 120 : 84;
        if (pt != null && mode == "capture" && yy + rh <= end)
        {
            Lbl(need.Text, bx, yy + 18, bw - rw - 8, need.Col, HorizontalAlignment.Left, Big ? 17 : 15);
            Button($"{id}.recall", new Rect2(bx + bw - rw, yy, rw, rh), "Recall", B.RecallParty, tip: new Tip("Beam the party back aboard"));
            yy += rh;
        }
        else if (yy + 20 <= end) { Lbl(need.Text, bx, yy + 16, bw, need.Col, HorizontalAlignment.Left, Big ? 17 : 15); yy += 20; }
        return Math.Max(pp, yy - y);
    }

    // what the transporter needs to reach t: in range, and an enemy's facing toward us down (Battle.BeamReach's rule)
    private (string Text, Color Col) ReachText(Ship s, Ship t)
    {
        double dk = Rules.DistKm(s, t), range = B.Comms.Transport.RangeKm;
        int f = Rules.FacingFrom(t, s.X, s.Z);
        if (dk > range) return ($"Out of reach {dk:0.0} km", C.Alert);
        if (t.Side != s.Side && !B.FacingDown(t, s)) return ($"{Rules.Facings[f]} shield up {Math.Round(t.Shields[f])}", C.Alert);
        return ($"In reach {dk:0.0} km", C.Accord);
    }

    private static (string, int)[] PartyList(Party p) => Battle.QueueRoles["capture"].Append("demo").Select(r => (r, p.Of(r))).ToArray();
    private static (string, string, Color?)[] PartyRows(Party p) =>
        PartyList(p).Where(e => e.Item2 > 0).Select(e => (Battle.RoleOne[e.Item1], e.Item2.ToString(), (Color?)null)).Concat(p.Of("vip") > 0 ? new (string, string, Color?)[] { ("Envoy", "aboard", null) } : Array.Empty<(string, string, Color?)>()).ToArray();

    // The tug of war for a ship (owner, 2026-11-02: "another tug of war display"): our boarders from the left in Sky, its
    // defenders from the right in red, a white line where they meet.
    private void TugBar(float x, float y, float w, float h, int ours, int theirs)
    {
        float f = ours + theirs > 0 ? (float)ours / (ours + theirs) : 0.5f, r = h / 2;
        Shapes.RoundRect(this, new Rect2(x, y, w, h), C.Deep, r);
        if (ours > 0) Shapes.RoundRect(this, new Rect2(x, y, w * f, h), C.Sky, r, Math.Min(r, w * f / 2), Math.Min(r, w * f / 2), r);
        if (theirs > 0) Shapes.RoundRect(this, new Rect2(x + w * f, y, w * (1 - f), h), C.Alert, Math.Min(r, w * (1 - f) / 2), r, r, Math.Min(r, w * (1 - f) / 2));
        DrawRect(new Rect2(x + w * f - 1.5f, y, 3, h), Colors.White);
    }

    // A team drawn as role chips: a lit square with the pictogram, the count beside it. Returns the height used.
    private float TeamChips(float x, float y, float w, (string Role, int N)[] list, Color col, float sz)
    {
        var items = list.Where(e => e.N > 0).ToArray();
        float cw = sz + sz * 1.2f;
        int per = Math.Max(1, (int)(w / cw));
        if (items.Length == 0) { Lbl("None", x, y + sz * 0.65f, w); return sz; }
        for (int i = 0; i < items.Length; i++)
        {
            float cx = x + i % per * cw, cy = y + i / per * (sz + 6);
            KeyFace.Draw(Kit, this, new Rect2(cx, cy, sz, sz), sz * 0.22f, col);
            TransportGlyphs.Role(this, items[i].Role, cx + sz / 2, cy + sz / 2, sz * 0.36f, KeyFace.Ink(Kit, col));
            Num($"×{items[i].N}", cx + sz + 6, cy + sz * 0.7f, 0, C.Text, HorizontalAlignment.Left, (int)MathF.Round(sz * 0.55f));
        }
        return (items.Length + per - 1) / per * (sz + 6) - 6;
    }

    // a faction's emblem in a box (the faction screen's art); a faction with none shows its initial
    private void Emblem(string faction, Rect2 r)
    {
        if (!EmblemTex.TryGetValue(faction, out Texture2D? tex))
            EmblemTex[faction] = tex = Flow.FactionScreen.Emblem.TryGetValue(faction, out string? path) ? GD.Load<Texture2D>(path) : null;
        if (tex == null) { Text(faction[..1].ToUpperInvariant(), r.GetCenter().X, r.GetCenter().Y + r.Size.Y * 0.2f, (int)(r.Size.Y * 0.55f), C.Alert, HorizontalAlignment.Center, 0, 700); return; }
        float k = Math.Min(r.Size.X / tex.GetWidth(), r.Size.Y / tex.GetHeight());
        var sz = new Vector2(tex.GetWidth(), tex.GetHeight()) * k;
        DrawTextureRect(tex, new Rect2(r.GetCenter() - sz / 2, sz), false);
    }

    // The boarding party aboard (expanded Capture; owner, 2026-11-04: "int he expanded view show marine health and battle
    // log for when medics heal"): each marine's health as a bar, the specialists and what they do now (hackers exposed
    // under hacker_cover marines, the pilot needed only to fly the prize), and the boarding log beside them.
    private void PartyPanel(float x, float y, float w, float h, Ship s)
    {
        if (h < 80) return;
        BoardDef bd = B.Comms.Board;
        Party? pt = s.Party;
        Ship? t = pt != null ? B.ShipById(pt.On) : null;
        float lw = w * 0.5f, yy = y;
        Lbl(pt != null && t != null ? $"Boarding party · aboard {t.Name}" : "Boarding party · none aboard", x, yy + 14, lw - 10);
        yy += 24;
        if (pt != null && t != null)
        {
            // each marine's health; one aboard without an entry yet arrived whole
            int m = pt.Of("marines");
            float bw = 16, bh = 40;
            for (int i = 0; i < m; i++)
            {
                float bx = x + i * (bw + 6);
                if (bx + bw > x + lw - 10) break;
                double v = i < pt.Hp.Count ? pt.Hp[i] : 1;
                Color col = v > 0.66 ? C.Accord : v > 0.33 ? C.Caution : C.Alert;
                Ui.Reg($"tr.hp.{i}", new Rect2(bx, yy, bw, bh), null, null, new Tip($"Marine {i + 1} · health {Pct(v)}"));
                Shapes.RoundRect(this, new Rect2(bx, yy, bw, bh), C.Deep, 4);
                Shapes.RoundRect(this, new Rect2(bx, yy + bh * (1 - (float)v), bw, bh * (float)v), col, 4);
            }
            if (m == 0) Lbl("No marines left", x, yy + 24, lw, C.Alert, HorizontalAlignment.Left, 15);
            yy += bh + 10;
            bool exposed = pt.Of("hackers") > 0 && m < bd.HackerCover;
            int wounded = Enumerable.Range(0, m).Count(i => i < pt.Hp.Count && pt.Hp[i] < 1);
            (string L, string V, Color Col)[] rows =
            {
                ("Marines", $"{m} · {wounded} wounded", C.Text),
                ("Medics", pt.Of("medics") > 0 ? $"{pt.Of("medics")} · heal {Pct(bd.Heal)} a volley each" : "None", pt.Of("medics") > 0 ? C.Accord : C.Lilac),
                ("Hackers", pt.Of("hackers") > 0 ? $"{pt.Of("hackers")} · volleys {Pct(Math.Min(bd.HackerSpeedMax, bd.HackerSpeed * pt.Of("hackers")))} faster{(exposed ? " · exposed" : "")}" : "None",
                    exposed ? C.Alert : pt.Of("hackers") > 0 ? C.Accord : C.Lilac),
                ("Engineers", pt.Of("engineers") > 0 ? pt.Of("engineers").ToString() : "None: needed to take it", pt.Of("engineers") > 0 ? C.Text : C.Caution),
                ("Pilots", pt.Of("pilots") > 0 ? pt.Of("pilots").ToString() : "None: needed to fly it after", pt.Of("pilots") > 0 ? C.Text : C.Caution),
                ("Defenders", Battle.Defenders(t).ToString(), C.Alert),
                ("Next volley", $"{Math.Max(0, pt.VolleyT):0.0} s", C.Sky),
            };
            foreach (var (l, v, c) in rows)
            {
                if (yy + 22 > y + h) break;
                Lbl(l, x, yy + 15, 110);
                Text(v, x + 116, yy + 16, 17, c, HorizontalAlignment.Left, lw - 126);
                yy += 24;
            }
        }
        else Lbl("Queue marines and specialists, then energize", x, yy + 14, lw - 10, Sk.R.Ghost, HorizontalAlignment.Left, 15);
        // the boarding log: the latest lines about parties, medics and hackers
        float lx = x + lw + 10;
        int n = Math.Max(1, (int)((h - 24) / 22));
        LogLine[] lines = B.Log.Where(l => l.Who is "Boarding" or "Transport").Reverse().Take(n).ToArray();
        Lbl("Boarding log", lx, y + 14, w - lw - 10);
        for (int i = 0; i < lines.Length; i++)
        {
            LogLine l = lines[i];
            Color col = Battle.IsWarning(l) ? C.Caution : l.Text.Contains("Medic") ? C.Accord : l.Text.Contains("shot aboard") || l.Text.Contains("lost") ? C.Alert : C.Text;
            X.Fonts.DrawRich(this, new[] { ($"{Clock(l.T)} ", C.Lilac), (l.Text, col) }, lx, y + 40 + i * 22, 15, w - lw - 10);
        }
    }

    // ---- Cargo -------------------------------------------------------------------------------------------------------

    // Supplies and Cargo (owner, 2026-11-03: "dont mix important cargo with regular cargo ... we should see how much cargo
    // the ship has vs extra cargo space"): where to (the friendly ship, station or planet selected), the hold split into
    // supplies, trade goods and free space, then a key per item, ours to send and theirs to take, and the pads.
    private void Cargo(Rect2 b)
    {
        Ship s = Me;
        float x = b.Position.X, y = b.Position.Y, w = b.Size.X, gap = Big ? 14 : 8;
        {
            float tw = (w - 6) / 2, th = Big ? 30 : 24;
            Button("tr.cv.sup", new Rect2(x, y, tw, th), "Supplies", () => X.State.CargoView = "sup", X.State.CargoView == "sup");
            Button("tr.cv.misc", new Rect2(x + tw + 6, y, tw, th), "Misc", () => X.State.CargoView = "misc", X.State.CargoView == "misc");
            y += th + 8;
        }
        TransportDest? d = B.InvPick();
        bool reach = B.InvReach(s, d);
        double range = B.Comms.Transport.RangeKm;
        float ks = Math.Min(Big ? 76 : 44, (w - 4 * 6) / 5);
        string[] keys = X.State.CargoView == "misc" ? CommsDef.CargoItems : CommsDef.SupplyItems.Append("vip").ToArray();
        ICargoHolder? dest = d?.Holder;
        string NameOf(string k) => k == "vip" ? "Envoy" : Battle.ItemNames[k];
        if (d != null)
            X.Fonts.DrawRich(this, new[] { (d.Name, C.Accord), ($" · {d.Km:0.0} km · ", C.Lilac),
                (d.Km <= range ? (B.PadReady(s) ? "in reach" : "pads charging") : $"out of reach ({range:0.0} km)", d.Km <= range ? C.Accord : C.Caution) }, x, y + 18, 18, w);
        else Lbl("Click a friendly ship, station or planet", x, y + 18, w, C.Caution);
        y += 28;
        y += HoldBar(x, y, w, s) + 6;
        void Row(string label, string who, Action<string> click, Func<string, int> count, Func<string, bool> ring)
        {
            Lbl(label, x, y + 13, w, C.Lilac, HorizontalAlignment.Left, 13);
            y += 18;
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                int need = dest != null && k != "vip" ? Battle.StillNeeds(dest, k) : 0;
                IconKey($"tr.inv.{who}.{k}", new Rect2(x + i * (ks + 6), y, ks, ks), (cx, cy, r, col) => TransportGlyphs.Item(this, k, cx, cy, r, col), count(k), () => click(k), ring(k), !reach,
                    new Tip($"{(who == "us" ? "Send" : "Take")} {NameOf(k)}{(k == "vip" ? "" : $" · {B.Comms.Inventory.PerBeam} a beam")}{(need > 0 ? $" · {d!.Name} needs {need}" : "")}"));
            }
            y += ks + 8;
        }
        Row("Aboard · click to send", "us", k => B.Transfer(k, 1), k => k == "vip" ? (s.Vip != null ? 1 : 0) : s.Hold[k],
            k => dest != null && k != "vip" && Battle.StillNeeds(dest, k) > 0 && s.Hold[k] > 0);
        if (d?.Ship is { } o) Row($"At {d.Name} · click to take", "them", k => B.Transfer(k, -1), k => k == "vip" ? (o.Vip != null ? 1 : 0) : o.Hold[k], _ => false);
        else if (d?.Planet is { } pl)
        {
            string[] want = keys.Where(k => k != "vip" && (pl.Needs?.GetValueOrDefault(k) ?? 0) > 0).ToArray();
            Lbl(want.Length > 0 ? $"{d.Name} needs" : $"{d.Name} needs nothing", x, y + 13, w, C.Lilac, HorizontalAlignment.Left, 13);
            y += 18;
            float sz2 = MathF.Round(ks * 0.7f);
            for (int i = 0; i < want.Length; i++)
            {
                string k = want[i];
                int left = Battle.StillNeeds(pl, k);
                float cx = x + i * ks * 1.9f;
                Color f = left > 0 ? C.Caution : C.Accord;
                KeyFace.Draw(Kit, this, new Rect2(cx, y, sz2, sz2), sz2 * 0.22f, f, true);
                TransportGlyphs.Item(this, k, cx + sz2 / 2, y + sz2 / 2, sz2 * 0.32f, KeyFace.Ink(Kit, f, true));
                Num(left > 0 ? left.ToString() : "done", cx + sz2 + 6, y + sz2 * 0.72f, 0, f, HorizontalAlignment.Left, (int)MathF.Round(sz2 * 0.55f));
            }
            if (want.Length > 0) y += sz2 + 8;
        }
        if (y + 40 <= b.End.Y) PadRow("tr.pad.inv", x, y, w, s, "inv");
    }

    // The hold as one bar split three ways: supplies (Sky), trade goods (Lilac) and the free space left (dark), with the counts.
    private float HoldBar(float x, float y, float w, Ship s)
    {
        int sup = CommsDef.SupplyItems.Sum(k => s.Hold[k]), car = CommsDef.CargoItems.Sum(k => s.Hold[k]), cap = Math.Max(1, s.HoldCap), free = Math.Max(0, s.HoldCap - sup - car);
        float h = Big ? 14 : 10;
        X.Fonts.DrawRich(this, new[] { ("Hold ", C.Lilac), ($"supplies {sup}", C.Sky), (" · ", C.Lilac), ($"misc {car}", C.Lilac), (" · ", C.Lilac),
            ($"free {free}", free > 0 ? C.Text : C.Caution), ($" of {s.HoldCap}", C.Lilac) }, x, y + 15, Big ? 18 : 15, w);
        float by = y + 22, a = w * Math.Min(1, (float)sup / cap), c = w * Math.Min(1 - a / w, (float)car / cap);
        Shapes.RoundRect(this, new Rect2(x, by, w, h), C.Deep, h / 2);
        if (a > 1) Shapes.RoundRect(this, new Rect2(x, by, a, h), C.Sky, h / 2);
        if (c > 1) Shapes.RoundRect(this, new Rect2(x + a, by, c, h), C.Lilac, h / 2);
        return 22 + h;
    }
}
