// StationPanel.Reactor: the Power station's Reactor and Warp modes and every station's officer slot (the battle console
// mockup's stReactor, drawReactor, coilGauge, stWarp and officerSlot; openspec/changes/reactor, warp, officers). The
// reactor is a diagram worked by hand: the core between its confinement rings, each coil a key dragged onto its ring, the
// two coolant loops as closed circuits through the core with their pumps' flow keys; expanded, the gauges and keys beside
// it. The Warp page shows the nacelles with their draw and damage, the banks with the jump threshold, the six risk factors
// and the total, and the plot, override and jump keys. Every figure is the simulation's own (Rules.Reactor, Battle.Warp),
// drawn only with the kit's pieces, keys and the palette, so every skin draws it.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel
{
    private static float Now => Time.GetTicksMsec() / 1000f;

    // ---- the officer slot -----------------------------------------------------------------------------------------

    // A station's officer slot (openspec/changes/officers): the officer's rank as pips and their name; on hover a card with
    // their rank, specialty, skill and what they bring to this station; a click swaps in the next officer in reserve.
    private void OfficerSlot(float x, float y, float w, string st)
    {
        if (w < 90) return;
        Ship s = Me;
        StationOfficer? o = s.Officers.GetValueOrDefault(st);
        int k = Rules.OfficerSkill(s, st);
        OfficersDef d = B.Data.Systems.Officers;
        string name = Battle.StationName(st);
        Tip tip = o != null
            ? new Tip($"{B.RankOf(o)} {o.Name}", new (string, string, Color?)[] { ("Station", name, null), ("Specialty", Battle.StationName(o.Spec) + (o.Spec == st ? "" : " · half skill"), null),
                ("Skill", d.Skills[o.Skill] + (o.Spec == st ? "" : $" (as {d.Skills[k]})"), null) }
                .Concat(Abilities(st, k)).Append(("Click", "Next officer", null)).ToArray())
            : new Tip("Vacant", new (string, string, Color?)[] { ("Station", name, null), ("Click", "Assign an officer", null) });
        HitState h = Ui.Reg($"off.{st}", new Rect2(x, y, w, 22), _ => B.OfficerNext(st), null, tip);
        if (Sk.TitlePlacard) Kit.Piece(this, "plate.title", new Rect2(x - 6, y - 1, w + 12, 24));
        else DrawRect(new Rect2(x, y, w, 22), Sk.R.Plate);
        if (h.Hot) DrawRect(new Rect2(x, y, w, 22), new Color(C.Sky, 0.18f));
        // on a rendered placard the contents start past the light at its head
        float x0 = Sk.TitlePlacard ? x + 12 : x;
        if (o == null) { Lbl("Officer vacant", x0 + 8, y + 16, w - (x0 - x) - 16, Sk.R.Ghost, HorizontalAlignment.Left, 14); return; }
        // rank pips: one for each step up from the lowest rank, in Caution
        for (int i = 0; i <= o.Rank; i++)
        {
            float px0 = x0 + 10 + i * 10;
            DrawColoredPolygon(new[] { new Vector2(px0, y + 6), new Vector2(px0 + 4, y + 11), new Vector2(px0, y + 16), new Vector2(px0 - 4, y + 11) }, C.Caution);
        }
        float px = x0 + 14 + (o.Rank + 1) * 10;
        Text($"{B.RankOf(o, true)} {o.Name}".ToUpperInvariant(), px, y + 17, 16, o.Spec == st ? C.Text : C.Caution, HorizontalAlignment.Left, w - (px - x) - 8, 600, 1);
    }

    // What a station's officer does, as the hover card says it, at skill k (the mockup's ABILITIES).
    private IEnumerable<(string, string, Color?)> Abilities(string st, int k)
    {
        OfficerAbility a = B.Data.Systems.Officers.Ability;
        string P(double v) => $"{Math.Round(k * v * 100)} %";
        (string, string)[] rows = st switch
        {
            "helm" => new[] { ("High energy turns", "+" + P(a.Het)) },
            "power" => new[] { ("Reactor coil drift", "-" + P(a.Drift)) },
            "tactical" => new[] { ("Chance to hit", "+" + P(a.Hit)) },
            "pd" => new[] { ("Intercepts", "+" + P(a.Pd)) },
            "science" => new[] { ("Scan speed", "+" + P(a.Scan)), ("Cloak sweep reach", $"+{k * a.SweepKm:0.#} km") },
            "comms" => new[] { ("Enemy ECM countered", (k * a.Jam).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)) },
            "repair" => new[] { ("Repair crews", "+" + P(a.RepairRate)) },
            "transport" => new[] { ("Ambush first volley", "+" + P(a.Ambush)), ("Hit aboard", "+" + P(a.Aboard)), ("Pad recharge", "+" + P(a.PadRate)) },
            "fleet" => new[] { ("Wingmen chance to hit", $"+{k * a.WingHit * 100:0.0} %") },
            _ => Array.Empty<(string, string)>(),
        };
        return rows.Select(r => (r.Item1, k > 0 ? r.Item2 : "None", (Color?)(k > 0 ? C.Accord : C.Lilac)));
    }

    // ---- Reactor ----------------------------------------------------------------------------------------------------

    private void ReactorMode(Rect2 c)
    {
        Ship s = Me;
        ReactorState rx = s.Reactor;
        ReactorDef R = B.Data.Systems.Reactor;
        float cx = c.Position.X, cy = c.Position.Y, cw = c.Size.X, ch = c.Size.Y;
        if (!Big)
        {
            ReactorDiagram(cx, cy, cw, ch - 46, false);
            Overdraw(cx, cy + ch - 40, cw);
            return;
        }
        Pairs("rx.o", cx, cy + 22, new (string, string, Color?)[] { ("Output", $"{B.Rules.Output(s):0.0} / {s.Class.ReactorE} E", null), ("Efficiency", Pct(B.Rules.Efficiency(s)), null) });
        float sw = 420, dw = Math.Min(820, cw - sw - 24), dh = Math.Min(dw * 0.72f, ch - 60);
        ReactorDiagram(cx, cy + 40, dw, dh, true);
        float x = cx + dw + 24, y = cy + 40;
        y += Bar(x, y, sw, rx.Melt > 0 ? $"Core {Math.Round(rx.Temp)}% · meltdown in {Math.Ceiling(R.MeltdownS - rx.Melt)} s" : $"Core temperature {Math.Round(rx.Temp)}%", rx.Temp / 120, rx.Temp >= 85 || rx.Melt > 0) + 14;
        Lbl("Magnetic confinement", x, y + 13); y += 24;
        for (int i = 0; i < rx.Coils.Length; i++)
        {
            int ii = i;
            Lbl($"Coil {i + 1}", x, y + 19);
            CoilGauge(x + 60, y, 190, 26, rx.Coils[i]);
            Button($"rx.m.{i}", new Rect2(x + 258, y - 2, 34, 30), "-", () => B.AlignCoil(ii, -1), tip: new Tip($"Coil {i + 1} toward port"));
            Button($"rx.p.{i}", new Rect2(x + 298, y - 2, 34, 30), "+", () => B.AlignCoil(ii, 1), tip: new Tip($"Coil {i + 1} toward starboard"));
            Num($"{(rx.Coils[i] >= 0 ? "+" : "")}{rx.Coils[i]:0.00}", x + sw, y + 20, 0, null, HorizontalAlignment.Right, 16);
            y += 36;
        }
        Lbl("Coolant", x, y + 13); y += 24;
        for (int j = 0; j < rx.Loops.Length; j++)
        {
            CoolantLoop l = rx.Loops[j];
            int jj = j;
            Lbl($"Loop {Battle.LoopName(j)}", x, y + 19);
            CellRow($"rx.f.{j}", new Rect2(x + 60, y, sw - 60 - 80, 28), R.FlowMax, l.Flow, n => B.SetFlow(jj, n), Math.Min(R.FlowMax, B.Rules.PowerRoom(s, l.Flow)));
            Num(l.Hp <= 0 ? "OFFLINE" : Pct(l.Hp), x + sw, y + 20, 0, LoopCol(l.Hp, C.Sky), HorizontalAlignment.Right, 16);
            y += 36;
        }
        Overdraw(x, y + 4, sw); y += 70;
        Pairs("rx.x1", x, y, new (string, string, Color?)[] { ("Confinement", Pct(Rules.Confinement(s)), null), ("Cooling", Rules.Cooling(s).ToString("0.0"), null) });
        Pairs("rx.x2", x, y + 32, new (string, string, Color?)[] { ("Battery", $"{s.Battery:0.0} E", null), ("Brownout", s.Brown < 1 ? Pct(s.Brown) : "None", s.Brown < 1 ? C.Alert : null) });
    }

    private Color LoopCol(double hp, Color ok) => hp <= 0 ? C.Alert : hp < 0.5 ? C.Caution : ok;

    // the setting that lets the allocation draw past the output (off by default)
    private void Overdraw(float x, float y, float w)
    {
        Lbl("Draw past output", x, y + 26, 160);
        Tabs("rx.od", new Rect2(x + 170, y, w - 170, 40), new[] { ("off", "Off"), ("on", "On") }, Me.Overdraw ? "on" : "off", v => B.SetOverdraw(v == "on"));
    }

    // a coil's offset on a track: the safe band in the middle, the knob coloured by how far out it is
    private void CoilGauge(float x, float y, float w, float h, double o)
    {
        float cx = x + w / 2, half = w / 2 - 10, yy = y + h / 2;
        const float safe = 0.1f;
        DrawRect(new Rect2(cx - half * safe, y + 2, half * safe * 2, h - 4), new Color(C.Accord, 0.18f));
        DrawLine(new Vector2(cx - half, yy), new Vector2(cx + half, yy), C.Deep, 4);
        DrawLine(new Vector2(cx, y + 3), new Vector2(cx, y + h - 3), C.Sky, 2);
        DrawCircle(new Vector2(cx + (float)o * half, yy), h / 2 - 3, CoilCol(o), true, -1, true);
    }

    private Color CoilCol(double o) => Math.Abs(o) < 0.1 ? C.Accord : Math.Abs(o) < 0.3 ? C.Caution : C.Alert;

    // The diagram, worked by hand: drag a coil onto its ring, the loops' + and - keys set their pumps' flow.
    private void ReactorDiagram(float ox, float oy, float w, float h, bool big)
    {
        Ship s = Me;
        ReactorState rx = s.Reactor;
        ReactorDef R = B.Data.Systems.Reactor;
        float k = big ? 1.6f : 1, t = Now;
        float Rr = Math.Min(w * 0.2f, h * 0.33f), cx = ox + Math.Max(w * 0.29f, Rr + 46 * k), cy = oy + h * 0.5f, Ri = Rr * 0.56f, mid = (Rr + Ri) / 2, chan = Rr - Ri, travel = Rr * 0.28f;
        int nc = rx.Coils.Length;
        float Dir(int i) => nc == 3 ? new[] { -Mathf.Pi / 2, Mathf.Pi, Mathf.Pi / 2 }[i] : -Mathf.Pi / 2 + i * Mathf.Tau / nc;
        Vector2 CoilAt(int i) { float a = Dir(i), d = mid + (float)rx.Coils[i] * travel; return new Vector2(cx + Mathf.Cos(a) * d, cy + Mathf.Sin(a) * d); }
        Color tc = rx.Temp >= 85 ? C.Alert : rx.Temp >= 65 ? C.Caution : C.Sky;
        // the controls first, so the drawing can show what is pointed at
        var coilHot = new bool[nc];
        for (int i = 0; i < nc; i++)
        {
            int ii = i;
            float a = Dir(i);
            Vector2 q = CoilAt(i);
            HitState st = Ui.Reg($"rx.coil.{i}", new Rect2(q.X - 20 * k, q.Y - 20 * k, 40 * k, 40 * k), null,
                (p, _) => B.SetCoil(ii, (((p.X - cx) * Mathf.Cos(a) + (p.Y - cy) * Mathf.Sin(a)) - mid) / travel), new Tip($"Coil {i + 1}: drag onto its ring"));
            coilHot[i] = st.Hot || st.Down;
        }
        // each coolant loop a closed circuit through the core, its pump and radiator on it
        for (int j = 0; j < rx.Loops.Length && j < 2; j++)
        {
            CoolantLoop l = rx.Loops[j];
            float sg = j == 0 ? -1 : 1, x0 = cx + Rr * 0.45f, x1 = ox + w - 20 * k, yO = cy + sg * h * 0.4f, yI = cy + sg * h * 0.12f, rad = 14 * k;
            Vector2[] loop = RoundLoop(x0, x1, Math.Min(yO, yI), Math.Max(yO, yI), rad);
            DrawPolyline(loop, C.Deep, 10 * k, true);
            if (l.Hp <= 0) DrawPolyline(loop, C.Alert, 2 * k, true);
            else
            {
                Color lc = new(tc, l.Hp < 0.5 ? 0.55f : 0.85f);
                DrawPolyline(loop, lc, 3 * k, true);
                int n = 2 + l.Flow * 2;
                for (int m = 0; m < n; m++) DrawCircle(Along(loop, (m / (float)n + t * l.Flow * 0.045f) % 1f), 2.6f * k, tc, true, -1, true);
            }
            // the radiator at the far end
            float qx = x1, qy = (yO + yI) / 2, rh = Math.Abs(yO - yI) * 0.62f;
            Kit.Shape(this, new Rect2(qx - 13 * k, qy - rh / 2, 26 * k, rh), 9 * k, C.Deep);
            for (int f = 0; f < 4; f++) { float fy = qy - rh / 2 + 7 * k + f * (rh - 14 * k) / 3; DrawLine(new Vector2(qx - 8 * k, fy), new Vector2(qx + 8 * k, fy), l.Hp <= 0 ? C.Alert : C.Signal, 2 * k); }
            if (big) Lbl("Radiator", qx - 18 * k, qy + 4 * k, 0, null, HorizontalAlignment.Right, 11);
            // the pump, its flow on it, and its keys
            var pump = new Vector2(ox + w * 0.62f, yO);
            DrawCircle(pump, 16 * k, Colors.Black, true, -1, true);
            DrawArc(pump, 16 * k, 0, Mathf.Tau, 32, LoopCol(l.Hp, C.Signal), 4 * k, true);
            Text(l.Flow.ToString(), pump.X, pump.Y + 5 * k, (int)(15 * k), C.Text, HorizontalAlignment.Center, 0, 700);
            Lbl($"Loop {Battle.LoopName(j)} · {(l.Hp <= 0 ? "offline" : Pct(l.Hp))}", (pump.X + qx) / 2 + 6 * k, yI + sg * 12 * k + 4 * k, 0, LoopCol(l.Hp, C.Lilac), HorizontalAlignment.Center, 11);
            float ky = yO - sg * 27 * k;
            foreach (int d in new[] { -1, 1 })
            {
                int jj = j, dd = d;
                var kc = new Vector2(pump.X + d * 16 * k, ky);
                HitState ks = Ui.Reg($"rx.key.{j}.{d}", new Rect2(kc.X - 13 * k, kc.Y - 13 * k, 26 * k, 26 * k), _ => B.SetFlow(jj, Me.Reactor.Loops[jj].Flow + dd), null,
                    new Tip($"Loop {Battle.LoopName(j)} flow {(d < 0 ? "down" : "up")}"));
                Color kf = ks.Hot ? C.Sky : C.Signal;
                Kit.Knob(this, kc, 11 * k, kf);
                Text(d < 0 ? "-" : "+", kc.X, kc.Y + 5.5f * k, (int)(16 * k), KeyFace.Ink(Kit, kf), HorizontalAlignment.Center, 0, 700);
            }
        }
        // the core between its confinement rings, its plasma ring pushed off centre by the coils
        var core = new Vector2(cx, cy);
        DrawArc(core, Rr, 0, Mathf.Tau, 64, C.Signal, 3 * k, true);
        DrawArc(core, Ri, 0, Mathf.Tau, 64, C.Signal, 3 * k, true);
        Vector2 off = Vector2.Zero;
        for (int i = 0; i < nc; i++) off += new Vector2(Mathf.Cos(Dir(i)), Mathf.Sin(Dir(i))) * (float)rx.Coils[i];
        float pulse = rx.Melt > 0 ? 0.55f + 0.45f * Mathf.Sin(t * 9) : 0.85f + 0.15f * Mathf.Sin(t * 2.3f);
        DrawArc(core + off * chan * 0.42f, mid, 0, Mathf.Tau, 64, new Color(tc, 0.95f * pulse), chan * 0.22f, true);
        Text($"{Math.Round(rx.Temp)}%", cx, cy + 6 * k, (int)((big ? 26 : 20) * k / (big ? 1.25f : 1)), rx.Temp >= 85 ? C.Alert : C.Text, HorizontalAlignment.Center, 0, 700);
        Lbl("Core", cx, cy - 18 * k, 0, null, HorizontalAlignment.Center, 11);
        for (int i = 0; i < nc; i++)
        {
            float a = Dir(i);
            Vector2 home = core + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * mid, q = CoilAt(i), tan = new Vector2(-Mathf.Sin(a), Mathf.Cos(a));
            DrawArc(home, 13 * k, 0, Mathf.Tau, 24, C.Accord, 2 * k, true);
            Color col = CoilCol(rx.Coils[i]);
            DrawLine(q - tan * 9 * k, q + tan * 9 * k, col, 16 * k);
            DrawCircle(q - tan * 9 * k, 8 * k, col, true, -1, true);
            DrawCircle(q + tan * 9 * k, 8 * k, col, true, -1, true);
            if (coilHot[i])
            {
                DrawArc(q, 19 * k, 0, Mathf.Tau, 24, C.Text, 2 * k, true);
                Lbl($"{(rx.Coils[i] >= 0 ? "+" : "")}{rx.Coils[i]:0.00}", cx + Mathf.Cos(a) * (Rr + 22 * k), cy + Mathf.Sin(a) * (Rr + 22 * k) + 4 * k, 0, col, HorizontalAlignment.Center, 12);
            }
            Text((i + 1).ToString(), q.X, q.Y + 4.5f * k, (int)(12 * k), KeyFace.Ink(Kit, col), HorizontalAlignment.Center, 0, 700);
        }
        if (rx.Melt > 0) Text($"MELTDOWN IN {Math.Ceiling(R.MeltdownS - rx.Melt)} S", ox + 6, oy + 16 * k, (int)(15 * k), new Color(C.Alert, 0.6f + 0.4f * Mathf.Sin(t * 9)), HorizontalAlignment.Left, 0, 700);
        else if (Rules.Cooling(s) < 1.5) Lbl("Cooling low", ox + 6, oy + 16 * k, 0, C.Caution, HorizontalAlignment.Left, 13);
    }

    // a closed loop: a rectangle with rounded corners, from the core's side out to the radiator and back
    private static Vector2[] RoundLoop(float x0, float x1, float y0, float y1, float r)
    {
        var pts = new List<Vector2>();
        void Arc(float ax, float ay, float a0, float a1) { for (int n = 0; n <= 6; n++) { float a = a0 + (a1 - a0) * n / 6; pts.Add(new Vector2(ax + Mathf.Cos(a) * r, ay + Mathf.Sin(a) * r)); } }
        Arc(x1 - r, y0 + r, -Mathf.Pi / 2, 0);
        Arc(x1 - r, y1 - r, 0, Mathf.Pi / 2);
        Arc(x0 + r, y1 - r, Mathf.Pi / 2, Mathf.Pi);
        Arc(x0 + r, y0 + r, Mathf.Pi, Mathf.Pi * 1.5f);
        pts.Add(pts[0]);
        return pts.ToArray();
    }

    // the point a share f of the way round a path
    private static Vector2 Along(Vector2[] pts, float f)
    {
        float tot = 0;
        for (int n = 1; n < pts.Length; n++) tot += pts[n].DistanceTo(pts[n - 1]);
        float d = ((f % 1 + 1) % 1) * tot;
        for (int n = 1; n < pts.Length; n++)
        {
            float l = pts[n].DistanceTo(pts[n - 1]);
            if (d <= l) return pts[n - 1].Lerp(pts[n], l > 0 ? d / l : 0);
            d -= l;
        }
        return pts[^1];
    }

    // ---- Warp ----------------------------------------------------------------------------------------------------------

    private Color RiskCol(double r) => r > 0.3 ? C.Alert : r > 0.1 ? C.Caution : C.Accord;

    private void WarpMode(Rect2 c)
    {
        Ship s = Me;
        WarpState w = s.Warp;
        WarpDef W = B.Data.Systems.Warp;
        WarpRisk r = B.WarpRiskOf(s);
        double[] draw = Rules.NacelleDraw(s);
        float cx = c.Position.X, cy = c.Position.Y, cw = c.Size.X, ch = c.Size.Y;
        float dh = Big ? Math.Min(ch * 0.42f, 320) : 96, nw = Big ? 48 : 22;
        // the nacelles, port and starboard, each filled from the bottom by its health in its status colour, the draw beside it
        for (int i = 0; i < w.Nacelles.Length && i < 2; i++)
        {
            float x = cx + (i == 1 ? 2 * nw + 70 : 0), y = cy + 4;
            double hp = Math.Clamp(w.Nacelles[i], 0, 1);
            SystemStatus st = B.Rules.StatusOf(hp);
            Color col = StatusColor(st);
            Kit.Shape(this, new Rect2(x, y, nw, dh), nw / 2, C.Deep);
            if (hp > 0) Kit.Shape(this, new Rect2(x, y + dh * (1 - (float)hp), nw, Math.Max(nw, dh * (float)hp)), nw / 2, col);
            if (draw[i] > 0) Kit.Shape(this, new Rect2(x - 2, y - 2, nw + 4, dh + 4), nw / 2 + 2, C.Sky, 2);
            Ui.Reg($"wp.n{i}", new Rect2(x, y, nw, dh), null, null, new Tip(i == 1 ? "Starboard nacelle" : "Port nacelle",
                new (string, string, Color?)[] { ("Health", Pct(hp), null), ("Status", st.ToString(), col), ("Draw", $"{draw[i]:0.0} E", null) }));
            Lbl(i == 1 ? "Stbd" : "Port", x + nw / 2, y + dh + 16, 0, null, HorizontalAlignment.Center, 13);
            Num($"{draw[i]:0.0} E", x + nw / 2, y + dh + 34, 0, null, HorizontalAlignment.Center, 15);
        }
        // the banks between them, with the jump threshold
        float bx = cx + nw + 22, bw = 26, by = cy + 4, fill = (float)(w.Banks / W.BankE);
        DrawRect(new Rect2(bx, by, bw, dh), C.Deep);
        DrawRect(new Rect2(bx, by + dh * (1 - fill), bw, dh * fill), w.Banks >= W.JumpE ? C.Accord : C.Sky);
        float ty = by + dh * (1 - (float)(W.JumpE / W.BankE));
        DrawLine(new Vector2(bx - 4, ty), new Vector2(bx + bw + 4, ty), C.Text, 2);
        Lbl("Banks", bx + bw / 2, by + dh + 16, 0, null, HorizontalAlignment.Center, 13);
        Num($"{Math.Floor(w.Banks)}/{W.BankE:0}", bx + bw / 2, by + dh + 34, 0, w.Banks >= W.JumpE ? C.Accord : null, HorizontalAlignment.Center, 15);
        // the risk factors, each a thin bar of its risk, and the total
        float rx = cx + 2 * nw + 70 + nw + 14, rw = cx + cw - rx, rh = Big ? Math.Min(44, dh / 7) : 17;
        for (int i = 0; i < r.Factors.Count; i++)
        {
            RiskFactor f = r.Factors[i];
            float y = cy + 2 + i * rh;
            Color rc = RiskCol(f.Risk);
            Lbl(f.Label, rx, y + rh - 5, rw * 0.5f, null, HorizontalAlignment.Left, Big ? 15 : 12);
            DrawRect(new Rect2(rx + rw * 0.52f, y + rh / 2 - 3, rw * 0.3f, 6), C.Deep);
            DrawRect(new Rect2(rx + rw * 0.52f, y + rh / 2 - 3, rw * 0.3f * (float)Math.Clamp(f.Risk, 0, 1), 6), rc);
            Num(Pct(f.Risk), rx + rw, y + rh - 4, 0, rc, HorizontalAlignment.Right, Big ? 16 : 13);
        }
        float ry = cy + 10 + 6 * rh;
        Lbl("Jump risk", rx, ry + 18, 0, C.Sky, HorizontalAlignment.Left, Big ? 18 : 15);
        Num(r.Why != "" ? "Blocked" : Pct(r.Total), rx + rw, ry + 20, 0, r.Why != "" ? C.Caution : RiskCol(r.Total), HorizontalAlignment.Right, Big ? 26 : 20);
        // plot, override, jump
        float yb = cy + dh + 46, kw = (cw - 12) / 3, kh = Big ? 44 : 34;
        Button("wp.plot", new Rect2(cx, yb, kw, kh), w.Plotting ? $"Plot {Pct(w.Plot)}" : "Plot course", B.PlotCourse, w.Plotting && w.Plot >= 1, tip: new Tip("Plot a warp course: navigation calculates it, whatever the heading"));
        Button("wp.ovr", new Rect2(cx + kw + 6, yb, kw, kh), "Override", B.ToggleOverride, w.Override, tip: new Tip("Safety override: needed to jump in a strong Q-G field"));
        Button("wp.jump", new Rect2(cx + 2 * (kw + 6), yb, kw, kh), w.Out > 0 ? "Jumping" : "Jump", B.JumpNow, r.Why == "", tip: new Tip("Jump to warp"));
        yb += kh + 6;
        Lbl(r.Why != "" ? r.Why : $"Q-G field {Pct(r.Q)} · coolant {Pct(s.Reactor.Coolant)}", cx, yb + 14, cw, r.Why != "" ? C.Caution : null, HorizontalAlignment.Left, 13);
        PowerRow("wp.pw", cx, cy + ch - 28, cw, 26, "warp", Big ? 120 : 82);
    }
}
