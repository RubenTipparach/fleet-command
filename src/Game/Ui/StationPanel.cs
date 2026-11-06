// StationPanel: the stations panel (battle-console design section 1): one panel whose page is picked by a narrow column
// of nine icon tabs, each with its officer's slot on the frame's top edge. Built here: Helm (orders toward the target, high
// energy turns) and Power (energy in boxes; its Reactor and Warp modes in StationPanel.Reactor.cs). The other stations
// each live in their own file (StationPanel.<Station>.cs); one not built yet says so.
using System;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel : ConsolePanel
{
    public static readonly (string Id, string Label, string Icon)[] Stations =
    {
        ("helm", "Helm", "helm"), ("power", "Power", "power"), ("tactical", "Tactical", "tactical"), ("pd", "Point defence", "pd"),
        ("science", "Science", "science"), ("comms", "Comms", "comms"), ("repair", "Repair", "repair"), ("transport", "Transport", "transport"),
        ("fleet", "Fleet", "fleet"),
    };

    private static readonly string[] SystemIcons = { "weapons", "shields", "engines", "sensors", "tractor", "transport", "repair" };

    protected override void DrawPanel()
    {
        string cur = X.State.Station;
        var st = Array.Find(Stations, s => s.Id == cur);
        Rect2 p = Frame(st.Label);
        Rail("st", p.Position.X, p.Position.Y, Stations, cur, X.Act.Station);
        // the station's officer slot, on the frame's top edge left of its title (StationPanel.Reactor.cs)
        OfficerSlot(p.Position.X + 56, 0, Math.Min(Big ? 340 : 230, TitleLeft - 14 - (p.Position.X + 56)), cur);
        var b = new Rect2(p.Position.X + 56, p.Position.Y, p.Size.X - 56, p.Size.Y);
        bool drawn = false;
        switch (cur)
        {
            case "helm": Helm(b); drawn = true; break;
            case "power": Power(b); drawn = true; break;
            // each other station in its own file (StationPanel.<Station>.cs); a station not yet built says so
            case "tactical": TacticalPage(b, ref drawn); break;
            case "pd": PointDefencePage(b, ref drawn); break;
            case "science": SciencePage(b, ref drawn); break;
            case "comms": CommsPage(b, ref drawn); break;
            case "repair": RepairPage(b, ref drawn); break;
            case "transport": TransportPage(b, ref drawn); break;
            case "fleet": FleetPage(b, ref drawn); break;
        }
        if (!drawn) NotBuilt(b, st.Label, st.Icon);
    }

    partial void TacticalPage(Rect2 b, ref bool drawn);
    partial void PointDefencePage(Rect2 b, ref bool drawn);
    partial void SciencePage(Rect2 b, ref bool drawn);
    partial void CommsPage(Rect2 b, ref bool drawn);
    partial void RepairPage(Rect2 b, ref bool drawn);
    partial void TransportPage(Rect2 b, ref bool drawn);
    partial void FleetPage(Rect2 b, ref bool drawn);

    private void NotBuilt(Rect2 b, string label, string icon)
    {
        Kit.Icon(this, icon, new Rect2(b.Position.X + b.Size.X / 2 - 36, b.Position.Y + 60, 72, 72), C.Deep);
        Text(label.ToUpperInvariant(), b.Position.X + b.Size.X / 2, b.Position.Y + 170, 24, C.Sky, HorizontalAlignment.Center, b.Size.X - 10, 700, 1);
        Lbl("Not yet in the game", b.Position.X + b.Size.X / 2, b.Position.Y + 198, b.Size.X - 10, null, HorizontalAlignment.Center);
    }

    // The helm (battle-console design 4b and the helm page, v65; the Starfleet Command 2 manual's Helm Officer display,
    // printed pages 117-120, taken in shape, not art): orders toward the target in the left half (intercept, trail,
    // hold distance with its slider, orbit with its direction), hit and run with its two-handled range slider and
    // evasive in the right, then a 3 x 3 pad of high energy turns round free heading with emergency stop and a free-angle
    // turn; the HET chance, heading and ordered speed beside it.
    private void Helm(Rect2 b)
    {
        Ship s = Me;
        Ship? t = B.NavTarget(s);
        float k = Big ? 1.5f : 1, gap = 8 * k, row = (b.Size.X - 3 * gap) / 4, ks = Math.Min(row, Big ? 84 : 52);
        float x0 = b.Position.X, y = b.Position.Y, ts = MathF.Round(34 * k), tg = 5 * k, half = (b.Size.X - 12 * k) / 2, xr = x0 + half + 12 * k;
        int lbl = (int)(13 * k);
        Func<bool> ordOn(string o) => () => s.Engaged && s.Manoeuvre == o;
        bool noT = t == null;
        Lbl("Target", x0, y + 12, 0, null, HorizontalAlignment.Left, lbl);
        Lbl("Runs and evasion", xr, y + 12, 0, null, HorizontalAlignment.Left, lbl);
        y += 18 * k;
        float y0 = y;
        double span = B.Tuning.HoldMaxKm - B.Tuning.HoldMinKm;
        float Frac(double v) => (float)((v - B.Tuning.HoldMinKm) / span);
        double Val(float f) => B.Tuning.HoldMinKm + f * span;
        HelmKey("helm.int", x0, y, ts, "intercept", "Intercept", () => B.HelmOrder("intercept"), ordOn("intercept")(), noT);
        HelmKey("helm.trail", x0 + ts + tg, y, ts, "trail", "Trail and match speed", () => B.HelmOrder("trail"), ordOn("trail")(), noT);
        y += ts + tg;
        HelmKey("helm.hold", x0, y, ts, "hold_range", "Approach and hold distance", () => B.HelmOrder("hold_range"), ordOn("hold_range")(), noT);
        {
            float sx = x0 + ts + tg + 8, sw = half - ts - tg - 16;
            Slider("helm.holdkm", sx, y, sw, ts * 0.6f, Frac(s.HoldKm), null, f => B.SetHoldKm(Val(f)), "Distance to hold");
            Lbl($"{s.HoldKm:0.0} km", sx + sw, y + ts - 2, 0, C.Sky, HorizontalAlignment.Right, lbl);
        }
        y += ts + tg;
        HelmKey("helm.orbit", x0, y, ts, "orbit", $"Orbit ({(s.OrbitDir > 0 ? "clockwise" : "anticlockwise")})", () => B.HelmOrder("orbit"), ordOn("orbit")(), noT);
        {
            // the direction switch: a two-way toggle, the lit half the way the orbit goes
            float sx = x0 + ts + tg + 4, sw = half - ts - tg - 4, sh = ts - 8, sy = y + 4, hh = sw / 2;
            bool cw = s.OrbitDir > 0;
            Ui.Reg("helm.odir", new Rect2(sx, sy, sw, sh), _ => B.ToggleOrbitDir(), null, new Tip("Orbit direction"));
            Kit.Shape(this, new Rect2(sx, sy, sw, sh), sh / 2, C.Deep);
            KeyFace.Draw(Kit, this, new Rect2(sx + (cw ? 0 : hh), sy, hh, sh), sh / 2, C.Lilac);
            Color lit = KeyFace.Ink(Kit, C.Lilac);
            Text("CW", sx + hh / 2, sy + sh / 2 + 5 * k, lbl, cw ? lit : C.Sky, HorizontalAlignment.Center, 0, 700);
            Text("CCW", sx + hh * 1.5f, sy + sh / 2 + 5 * k, lbl, cw ? C.Sky : lit, HorizontalAlignment.Center, 0, 700);
        }
        float yr = y0;
        HelmKey("helm.run", xr, yr, ts, "hit_run", $"Hit and run ({(s.RunPhase == "in" ? "running in" : "pulling away")})", () => B.HelmOrder("hit_run"), ordOn("hit_run")(), B.TargetOf(s) == null);
        Lbl($"{s.RunMinKm:0.0}-{s.RunMaxKm:0.0} km", xr + ts + tg + 4, yr + ts * 0.62f, half - ts - tg, C.Sky, HorizontalAlignment.Left, (int)(14 * k));
        yr += ts + tg;
        {
            float sx = xr + 8, sw = half - 16, f0 = Frac(s.RunMinKm), f1 = Frac(s.RunMaxKm);
            Slider("helm.runkm", sx, yr, sw, ts * 0.7f, f0, f1, f =>
            {
                double v = Val(f);
                if (Math.Abs(f - f0) <= Math.Abs(f - f1)) B.SetRunRange(v, s.RunMaxKm); else B.SetRunRange(s.RunMinKm, v);
            }, "Hit and run: nearest and farthest range");
            Lbl("Min", sx, yr + ts - 2, 0, null, HorizontalAlignment.Left, (int)(12 * k));
            Lbl("Max", sx + sw, yr + ts - 2, 0, null, HorizontalAlignment.Right, (int)(12 * k));
        }
        yr += ts + tg;
        HelmKey("helm.err", xr, yr, ts, "erratic", "Evasive manoeuvres", () => B.ToggleErratic(), s.Erratic, false, C.Lilac);
        Lbl(s.Erratic ? "Evasive: on" : "Evasive", xr + ts + tg + 4, yr + ts * 0.62f, half - ts - tg, s.Erratic ? C.Lilac : C.Sky, HorizontalAlignment.Left, (int)(14 * k));
        y += ts + 12 * k;
        Lbl("High energy turns", x0, y + 12, 0, null, HorizontalAlignment.Left, lbl);
        y += 18 * k;
        bool busy = B.HetBusy(s);
        (string Kind, string Name, int Deg)[,] pad =
        {
            { ("het.l135", "HET hard left 135", -135), ("het.180", "HET 180", 180), ("het.r135", "HET hard right 135", 135) },
            { ("het.l90", "HET left 90", -90), ("free", "Free heading", 0), ("het.r90", "HET right 90", 90) },
            { ("none", "", 0), ("estop", "Emergency stop", 0), ("het.free", "HET free angle: click the view", 0) },
        };
        for (int ri = 0; ri < 3; ri++)
            for (int ci = 0; ci < 3; ci++)
            {
                var (kind, name, deg) = pad[ri, ci];
                float x = x0 + ci * (ks + gap), yy = y + ri * (ks + gap);
                switch (kind)
                {
                    // a blank grey key fills the corner, so the pad reads as a full 3 x 3; it does nothing
                    case "none": KeyFace.Draw(Kit, this, new Rect2(x, yy, ks, ks), ks * 0.22f, Sk.R.Mute); break;
                    case "free": HelmKey("helm.free", x, yy, ks, "free", name, () => B.HelmOrder("free"), !s.Engaged, false, C.Sky); break;
                    case "estop": HelmKey("helm.stop", x, yy, ks, "estop", s.EstopT > 0 ? $"Engines holding {Rules.FormatCooldown(s.EstopT)} s" : name, () => B.EmergencyStop(), s.Estop || s.EstopT > 0, false, C.Caution); break;
                    case "het.free": HelmKey("helm.hetf", x, yy, ks, "het.free", name, () => X.State.PlacingHet = !X.State.PlacingHet, X.State.PlacingHet, busy, C.Lilac); break;
                    default: int d = deg; HelmKey($"helm.{kind}", x, yy, ks, kind, name, () => B.Het(d), false, busy); break;
                }
            }
        float rx = x0 + 3 * (ks + gap), rw = b.End.X - rx;
        double hc = B.Rules.HetChanceNow(s);
        (string L, string V, Color Col)[] rows =
        {
            ("HET", s.Broke > 0 ? $"Out {Rules.FormatCooldown(s.Broke)} s" : Pct(hc), s.Broke > 0 ? C.Alert : hc < 0.5 ? C.Caution : C.Accord),
            ("Heading", $"{Geometry.HeadingDeg(s.H):000}°", C.Sky),
            ("Speed", $"{Math.Round(s.VOrder)} M/S", C.Sky),
        };
        for (int i = 0; i < rows.Length; i++)
        {
            float yy = y + i * (3 * ks + 2 * gap) / 3;
            Lbl(rows[i].L, rx, yy + 14 * k, 0, null, HorizontalAlignment.Left, lbl);
            Num(rows[i].V, rx, yy + 36 * k, rw, rows[i].Col, HorizontalAlignment.Left, (int)(22 * k));
        }
        if (!Big) return;
        y += 3 * (ks + gap) + 20;
        if (t != null) Pairs("helm.t", x0, y, new (string, string, Color?)[] { ("Target", t.Name, null), ("Range", $"{Rules.DistKm(s, t):0.0} km", null), ("Speed", $"{Math.Round(t.V)} M/S", null) });
        else Lbl("No target", x0, y);
        Pairs("helm.m", x0, y + 34, new (string, string, Color?)[] { ("Max speed", $"{Math.Round(B.Rules.MaxSpeed(s))} M/S", null), ("Engine boxes", s.Power["engines"].ToString(), null) });
    }

    /// <summary>One helm key: the skin's key face (lit when its order or toggle is on), its glyph in the ink that reads on
    /// it, its name on hover.</summary>
    private void HelmKey(string id, float x, float y, float sz, string kind, string name, Action click, bool on, bool dis, Color? onCol = null)
    {
        var r = new Rect2(x, y, sz, sz);
        HitState st = dis ? default : Ui.Reg(id, r, _ => click(), null, new Tip(name));
        Color fill = dis ? C.Deep : st.Down ? C.Lilac : on ? onCol ?? C.Accord : st.Hot ? C.Sky : C.Signal;
        KeyFace.Draw(Kit, this, r, sz * 0.22f, fill, false, dis ? 0.5f : 1);
        Color ink = dis ? Sk.R.InkDis : KeyFace.Ink(Kit, fill);
        HelmGlyphs.Draw(this, X.Fonts, kind, x + sz / 2, y + sz / 2, sz * 0.42f, ink, Me.OrbitDir, C.Signal);
        // the turn's angle, small in the corner, so 90 and 135 never read alike
        string? ang = kind switch { "het.l90" or "het.r90" => "90", "het.l135" or "het.r135" => "135", "het.180" => "180", _ => null };
        if (ang != null)
        {
            bool left = kind.Contains(".l");
            Text(ang, left ? x + sz - sz * 0.1f : x + sz * 0.1f, y + sz - sz * 0.1f, Math.Max(8, (int)Math.Round(sz * 0.2f)), ink, left ? HorizontalAlignment.Right : HorizontalAlignment.Left, 0, 700);
        }
    }

    /// <summary>A slider: one handle, or two (min and max) when f1 is given.</summary>
    private void Slider(string id, float x, float y, float w, float h, float f0, float? f1, Action<float> set, string label)
    {
        float sy = y + h / 2;
        Ui.Reg(id, new Rect2(x - 8, y, w + 16, h), p => set(Math.Clamp((p.X - x) / w, 0, 1)), (p, _) => set(Math.Clamp((p.X - x) / w, 0, 1)), new Tip(label));
        Kit.Shape(this, new Rect2(x, sy - 3, w, 6), 3, C.Deep);
        float a = f1 != null ? f0 : 0, bb = f1 ?? f0;
        Kit.Shape(this, new Rect2(x + w * a, sy - 3, w * (bb - a), 6), 3, C.Sky);
        foreach (float f in f1 != null ? new[] { f0, f1.Value } : new[] { f0 })
            KeyFace.Draw(Kit, this, new Rect2(x + w * f - 7, sy - 11, 14, 22), 6, C.Lilac);
    }

    private void Power(Rect2 b)
    {
        Ship s = Me;
        // the power station's modes: Allocation (the reference game's energy page), Reactor and Warp (StationPanel.Reactor.cs)
        Tabs("pw.mode", new Rect2(b.Position, new Vector2(b.Size.X, 40)), new[] { ("alloc", "Allocation"), ("reactor", "Reactor"), ("warp", "Warp") }, X.State.PowerMode, m => X.State.PowerMode = m);
        b = new Rect2(b.Position.X, b.Position.Y + 46, b.Size.X, b.Size.Y - 46);
        if (X.State.PowerMode == "reactor") { ReactorMode(b); return; }
        if (X.State.PowerMode == "warp") { WarpMode(b); return; }
        double outE = B.Rules.Output(s);
        int used = B.Rules.Used(s);
        double spare = outE - used;
        Pairs("pw.h", b.Position.X, b.Position.Y + 22, new (string, string, Color?)[] { ("Out", outE.ToString("0.0"), null), ("Used", used.ToString(), null), ("Spare", spare.ToString("0.0"), spare >= 0 ? C.Accord : C.Alert) });
        float y = b.Position.Y + 34, rowH = Big ? 40 : 24, gap = Big ? 10 : 5, lw = Big ? 160 : 112, io = Big ? 36 : 26, isz = Big ? 26 : 20;
        for (int i = 0; i < Rules.Systems.Length; i++)
        {
            string k = Rules.Systems[i];
            Kit.Icon(this, SystemIcons[i], new Rect2(b.Position.X, y + (rowH - isz) / 2, isz, isz), C.Sky);
            PowerRow("pw", b.Position.X + io, y, b.Size.X - io, rowH, k, lw - io);
            y += rowH + gap;
        }
        // warp set by hand (the mockup's allocation: "warp should be in here to allocate power for"), and the pumps' draw
        Kit.Icon(this, "warp", new Rect2(b.Position.X, y + (rowH - isz) / 2, isz, isz), C.Sky);
        PowerRow("pw", b.Position.X + io, y, b.Size.X - io, rowH, "warp", lw - io);
        y += rowH + gap;
        Pairs("pw.c", b.Position.X, y + 16, new (string, string, Color?)[] { ("Coolant pumps", $"{B.Rules.CoolantDraw(s)} E", null) }, 18);
        Lbl("set in Reactor", b.End.X, y + 16, 0, null, HorizontalAlignment.Right, 13);
        y += 22;
        double bat = s.Battery / s.Class.BatteryE;
        Bar(b.Position.X, y, b.Size.X, $"Battery {s.Battery:0.0} / {s.Class.BatteryE:0} E", bat, bat < 0.25);
        if (Big)
            Pairs("pw.x", b.Position.X, y + 70, new (string, string, Color?)[] { ("Speed limit", $"{Math.Round(B.Rules.MaxSpeed(s))} m/s", null), ("Capacitor", $"{s.Capacitor:0.0} E", null),
                ("Shield regen", $"{s.Power["shields"] * B.Tuning.ShieldRegenPerES:0.0}/s", null) });
    }

    /// <summary>One system's row of ten boxes: lit to its allocation, locked past what the output leaves.</summary>
    public void PowerRow(string idp, float x, float y, float w, float h, string key, float lw)
    {
        int cur = Me.Power[key];
        Lbl(key, x, y + h / 2 + 5, lw - 4);
        CellRow($"{idp}.{key}", new Rect2(x + lw, y, w - lw - 32, h), Rules.BoxesPerSystem, cur, n => B.SetPower(key, n), Math.Min(Rules.BoxesPerSystem, B.Rules.PowerRoom(Me, cur)));
        Num(cur.ToString(), x + w, y + h / 2 + 8, 0, null, HorizontalAlignment.Right, 20);
    }
}
