// StationPanel: the stations panel (battle-console design section 1): one panel whose page is picked by a narrow column
// of nine icon tabs. Built in the game so far: Helm (the pad of turn and speed keys) and Power (energy in boxes). The
// other seven stations are on the rail and show that they are not built yet; each comes in its own change.
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

    // The pad's eight keys clockwise from the top (the mockup's HELM_KEYS): speed up, starboard 15, 45, 90, speed down,
    // port 90, 45, 15.
    private static readonly (string Label, Action<Battle> Do)[] HelmKeys =
    {
        ("+30", b => b.SpeedStep(1)), ("15°", b => b.Turn(15)), ("45°", b => b.Turn(45)), ("90°", b => b.Turn(90)),
        ("-30", b => b.SpeedStep(-1)), ("90°", b => b.Turn(-90)), ("45°", b => b.Turn(-45)), ("15°", b => b.Turn(-15)),
    };
    private int _lit = -1;
    private ulong _litAt;

    protected override void DrawPanel()
    {
        string cur = X.State.Station;
        var st = Array.Find(Stations, s => s.Id == cur);
        Rect2 p = Frame(st.Label);
        Rail("st", p.Position.X, p.Position.Y, Stations, cur, X.Act.Station);
        var b = new Rect2(p.Position.X + 56, p.Position.Y, p.Size.X - 56, p.Size.Y);
        switch (cur)
        {
            case "helm": Helm(b); break;
            case "power": Power(b); break;
            default: NotBuilt(b, st.Label, st.Icon); break;
        }
    }

    private void NotBuilt(Rect2 b, string label, string icon)
    {
        Kit.Icon(this, icon, new Rect2(b.Position.X + b.Size.X / 2 - 36, b.Position.Y + 60, 72, 72), C.Deep);
        Text(label.ToUpperInvariant(), b.Position.X + b.Size.X / 2, b.Position.Y + 170, 24, C.Sky, HorizontalAlignment.Center, b.Size.X - 10, 700, 1);
        Lbl("Not yet in the game", b.Position.X + b.Size.X / 2, b.Position.Y + 198, b.Size.X - 10, null, HorizontalAlignment.Center);
    }

    private void Helm(Rect2 b)
    {
        Ship s = Me;
        float size = Math.Min(b.Size.X, b.Size.Y - 70);
        if (Big) size = Math.Min(b.Size.X * 0.6f, b.Size.Y - 80);
        var pad = new Rect2(b.Position.X + (Big ? 0 : (b.Size.X - size) / 2), b.Position.Y, size, size);
        Kit.Hero(this, "helm.pad", pad);
        int? Key(Vector2 q)
        {
            Vector2 d = q - pad.GetCenter();
            float r = d.Length() / size * 300;
            if (r < 50) return 8;
            if (r > 135) return null;
            return (int)Math.Round((Mathf.Atan2(d.Y, d.X) + Mathf.Pi / 2) / (Mathf.Pi / 4) + 8) % 8;
        }
        HitState hs = Ui.Reg("helm", pad, q =>
        {
            if (Key(q) is not { } k) return;
            _lit = k; _litAt = Time.GetTicksMsec();
            if (k == 8) B.AllStop(); else HelmKeys[k].Do(B);
        });
        float sc = size / 300f;
        Vector2 c = pad.GetCenter();
        void Light(int key, float alpha)
        {
            var col = new Color(C.Sky, alpha);
            if (key == 8) DrawCircle(c, 46 * sc, col, true, -1, true);
            else { float a = -Mathf.Pi / 2 + (key - 0.5f) * Mathf.Pi / 4; Pen.FillAbs(this, Shapes.ArcSeg(c, 58 * sc, 128 * sc, a + 0.06f, a + Mathf.Pi / 4 - 0.06f), col); }
        }
        if (hs.Hot && Key(Ui.Mouse) is { } hk) Light(hk, 0.35f);
        if (_lit >= 0 && Time.GetTicksMsec() - _litAt < 160) Light(_lit, 1);
        for (int i = 0; i < 8; i++)
        {
            float a = -Mathf.Pi / 2 + i * Mathf.Pi / 4;
            Vector2 q = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 93 * sc;
            // the arrow: a dot with a tail, pointing outward along the key
            Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 tip = q + dir * (-2) * sc;
            DrawCircle(tip, 6 * sc, Colors.Black, true, -1, true);
            DrawLine(tip - dir * 12 * sc, tip - dir * 6 * sc, Colors.Black, 3 * sc, true);
            Text(HelmKeys[i].Label, q.X, q.Y + (Mathf.Sin(a) > 0.5f ? -14 : 24) * sc, Math.Max(12, (int)(17 * sc)), Colors.Black, HorizontalAlignment.Center, 0, 700);
        }
        Text("STOP", c.X, c.Y + 8 * sc, Math.Max(12, (int)(22 * sc)), Colors.Black, HorizontalAlignment.Center, 0, 700);
        // readouts: heading, ordered heading, speed and its limit
        float y = Big ? pad.Position.Y + 30 : pad.End.Y + 26, x = Big ? pad.End.X + 30 : b.Position.X;
        Pairs("hd", x, y, new (string, string, Color?)[] { ("Heading", Geometry.HeadingDeg(s.H).ToString("000"), null), ("Ordered", Geometry.HeadingDeg(s.HOrder).ToString("000"), C.Lilac) }, 22);
        Pairs("sp", x, y + 30, new (string, string, Color?)[] { ("Speed", $"{Math.Round(s.V)}", null), ("Of", $"{Math.Round(B.Rules.MaxSpeed(s))} m/s", null) }, 22);
        if (Big)
        {
            Lbl("Keys: A D turn 15, Q E turn 45, W S speed, X all stop", x, y + 70, b.End.X - x);
            Lbl("Click in the battle to come to that bearing", x, y + 94, b.End.X - x);
        }
    }

    private void Power(Rect2 b)
    {
        Ship s = Me;
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
        y += 4;
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
