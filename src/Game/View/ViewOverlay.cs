// ViewOverlay: what the console draws over the 3D view, and the view's own input (battle-console design sections 1 and
// 4b). It marks every contact (enemies red, friendlies green, the locked target bracketed with its name, class, range
// and speed), draws the helm ring with the turn still to make, the shots, the readout of range and weapons in arc and
// charged, the last officer's message, and the pause and end banners. Input: a left click on empty space is the helm
// (come to that bearing), a click on an enemy locks it, a drag on or near the helm ring steers, a right drag turns the
// camera, the wheel zooms. Expanded, a click on the battle brings the console back.
using System;
using System.Linq;
using FleetCommand.Game.Ui;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class ViewOverlay : Control
{
    private const double RingM = 450, RingGrabM = 160;
    private ConsoleContext _x = null!;
    private BattleView _view = null!;
    private Vector2 _press;
    private bool _leftDown, _rightDown, _steering, _moved;
    private Vector2 _mouse = new(-1e5f, -1e5f);
    private readonly Imm _ui = new();

    public void Init(ConsoleContext ctx, BattleView view)
    {
        _x = ctx; _view = view;
        Name = "ViewOverlay";
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
    }

    private Battle B => _x.Battle;
    private Ui.Kit.Palette C => _x.Kit.C;

    public override void _Process(double delta) => QueueRedraw();

    /// <summary>A screen point in this control's coordinates, from the view's console coordinates.</summary>
    private Vector2? Local(double xm, double zm) => _view.ScreenOf(xm, zm) is { } p ? p - Position : null;

    private Ship? ShipAt(Vector2 local)
    {
        Ship? best = null;
        float bestD = 30;
        foreach (Ship s in B.Ships)
        {
            if (!s.Alive || s == B.Player) continue;
            if (Local(s.X, s.Z) is not { } p) continue;
            float d = p.DistanceTo(local);
            if (d < bestD) { bestD = d; best = s; }
        }
        return best;
    }

    private bool NearRing(Vector2 local)
    {
        if (_view.PlaneAt(local + Position) is not { } q) return false;
        Ship p = B.Player;
        return Math.Abs(Geometry.Dist(p.X, p.Z, q.X, q.Z) - RingM) <= RingGrabM;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (!_leftDown && !_rightDown && _ui.Handle(e)) { AcceptEvent(); return; }
        if (e is InputEventMouseMotion mm0) _mouse = mm0.Position;
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }: _view.Zoom(0.9f); break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }: _view.Zoom(1.1f); break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } b:
                if (b.Pressed)
                {
                    _leftDown = true; _moved = false; _press = b.Position;
                    _steering = NearRing(b.Position);
                    if (_steering) SteerTo(b.Position);
                }
                else if (_leftDown)
                {
                    _leftDown = false;
                    if (_steering) { _steering = false; B.Say("Helm", $"Come to {Geometry.HeadingDeg(B.Player.HOrder):000}."); }
                    else if (!_moved) LeftClick(b.Position);
                }
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right } b:
                if (b.Pressed) { _rightDown = true; _moved = false; _press = b.Position; }
                else if (_rightDown) { _rightDown = false; if (!_moved && ShipAt(b.Position) is { } s && s.Side != B.Player.Side) B.Lock(s.Id); }
                break;
            case InputEventMouseMotion m:
                _mouse = m.Position;
                if ((_leftDown || _rightDown) && m.Position.DistanceTo(_press) > 4) _moved = true;
                if (_steering) SteerTo(m.Position);
                else if (_rightDown) _view.Orbit(m.Relative);
                break;
            default: return;
        }
        AcceptEvent();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit) { _mouse = new Vector2(-1e5f, -1e5f); _ui.Leave(); }
    }

    private void SteerTo(Vector2 local)
    {
        if (_view.PlaneAt(local + Position) is { } q) B.HelmTo(q.X, q.Z);
    }

    private void LeftClick(Vector2 local)
    {
        if (_x.State.PlacingHet)
        {
            _x.State.PlacingHet = false;
            if (_view.PlaneAt(local + Position) is { } h) B.HetTo(h.X, h.Z);
            return;
        }
        if (ShipAt(local) is { } s) { if (s.Side != B.Player.Side) B.Lock(s.Id); return; }
        if (_x.State.Expanded != null) { _x.Act.Return(); return; }
        if (_view.PlaneAt(local + Position) is { } q)
        {
            B.HelmTo(q.X, q.Z);
            B.Say("Helm", $"Come to {Geometry.HeadingDeg(B.Player.HOrder):000}.");
        }
    }

    public override void _Draw()
    {
        _ui.Begin();
        DrawView();
        _ui.End();
        _x.Tips.Offer(this, _ui.CurrentTip(), Position);
    }

    // Over the 3D view, after the mockup's uiView (v65): markers and names, the locked target's bracket and hull, your
    // own hull, the helm ring, the 3D shield display, the Target key, the objective, the readout and the
    // last message bottom left, and the Target box.
    private void DrawView()
    {
        Fonts f = _x.Fonts;
        Battle b = B;
        Ship me = b.Player;
        Ship? t = b.TargetOf(me);
        var view = new Rect2(Vector2.Zero, Size);

        // the 3D shield display is always shown (owner, 2026-11-03: "Re move shield view toggle")
        ShieldHex(me);

        // the helm ring and the turn still to make
        bool ringHot = _steering || (!_leftDown && !_rightDown && ShipAt(_mouse) == null && NearRing(_mouse));
        RingArc(me, 0, Mathf.Tau, new Color(C.Sky, ringHot ? 0.55f : 0.22f), ringHot ? 2.5f : 1.5f);
        double turn = Geometry.Wrap(me.HOrder - me.H);
        if (Math.Abs(turn) > 0.01)
        {
            float a0 = (float)me.H, a1 = (float)(me.H + turn);
            RingArc(me, Math.Min(a0, a1), Math.Max(a0, a1), C.Sky, 4);
        }
        var fo = Geometry.Forward(me.HOrder);
        if (Local(me.X + fo.X * (RingM - 60), me.Z + fo.Z * (RingM - 60)) is { } t0 && Local(me.X + fo.X * (RingM + 60), me.Z + fo.Z * (RingM + 60)) is { } t1)
            DrawLine(t0, t1, C.Lilac, 3, true);

        // shots
        foreach (Shot sh in b.Shots)
        {
            var col = new Color(sh.Color);
            if (sh.Kind == "beam")
            {
                if (Local(sh.X, sh.Z) is { } a && Local(sh.TX, sh.TZ) is { } bb)
                    DrawLine(a, sh.Hit || sh.Intercept ? bb : bb + (bb - a).Normalized().Orthogonal() * 18, new Color(col, (float)Math.Clamp(sh.Life / b.Tuning.BeamLifeS, 0.2, 1)), 2.5f, true);
            }
            else if (Local(sh.X, sh.Z) is { } p) { DrawCircle(p, 4, col, true, -1, true); DrawCircle(p, 8, new Color(col, 0.25f), true, -1, true); }
        }

        // contacts: a ring each in its faction's colour (friendlies green), names under them; the locked target
        // bracketed with its name, class, range and speed, and its hull under that
        bool big = _x.State.Expanded != null;
        foreach (Ship s in b.Ships)
        {
            if (!s.Alive || s == me) continue;
            Vector2? lp = Local(s.X, s.Z);
            bool locked = s == t;
            if (lp is not { } p || !view.Grow(-4).HasPoint(p))
            {
                if (!locked) continue;
                // off screen: a ring at the edge toward it, with its name
                Vector2 dir = ((lp ?? Size / 2) - Size / 2);
                if (lp == null) dir = -dir;
                float an = dir.Angle();
                var ep = Size / 2 + new Vector2(Mathf.Cos(an) * (Size.X / 2 - 40), Mathf.Sin(an) * (Size.Y / 2 - 40));
                DrawArc(ep, 14, 0, Mathf.Tau, 24, C.Alert, 3, true);
                f.Draw(this, s.Name.ToUpperInvariant(), ep.X, ep.Y + 32, 16, C.Alert, HorizontalAlignment.Center);
                continue;
            }
            bool foe = s.Side != me.Side, hot = p.DistanceTo(_mouse) < (locked ? 50 : 22);
            Color col = foe ? (s.Faction.Id == "vos" ? C.Caution : C.Alert) : C.Accord;
            if (locked)
            {
                float kk = hot ? 1.08f : 1, w = 92 * kk, h = 72 * kk, c0 = 20;
                Color bc = hot ? C.Alert.Lightened(0.3f) : C.Alert;
                foreach (var (sx, sy) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
                {
                    var corner = p + new Vector2(sx * w / 2, sy * h / 2);
                    DrawPolyline(new[] { corner - new Vector2(sx * c0, 0), corner, corner - new Vector2(0, sy * c0) }, bc, 3.5f + (hot ? 1 : 0), true);
                }
                string lab = big ? s.Name : $"{s.Name} · {s.Class.Code} · {Rules.DistKm(me, s):0.0} km · {Math.Round(s.V)}";
                f.Draw(this, lab.ToUpperInvariant(), p.X, p.Y + 58, 20, C.Text, HorizontalAlignment.Center);
                HullBar(p.X, p.Y + 70, s);
            }
            else
            {
                float r = 16 * (hot ? 1.18f : 1);
                DrawArc(p, r, 0, Mathf.Tau, 32, hot ? col.Lightened(0.35f) : new Color(col, 0.7f), 1.5f + (hot ? 1 : 0), true);
                f.Draw(this, s.Name.ToUpperInvariant(), p.X, p.Y + 34, 16, col, HorizontalAlignment.Center);
            }
        }
        // your own hull under your ship
        if (me.Alive && Local(me.X, me.Z) is { } mp) HullBar(mp.X, mp.Y + 74, me);

        if (big) { f.Draw(this, "LIVE · TAP TO RETURN", Size.X / 2, 28, 18, C.Sky, HorizontalAlignment.Center, 0, 600, 1); return; }

        Key("v.target", new Rect2(14, 12, 90, 30), "Target", () => _x.State.ShowTarget = !_x.State.ShowTarget, _x.State.ShowTarget, "Show or hide the Target box");
        f.DrawRich(this, new[] { ("Objective · ", C.Lilac), (b.Data.Skirmish.Objective, C.Text) }, 14, 64, 16, Size.X * 0.62f);
        if (_x.State.Paused) f.Draw(this, "PAUSED · ORDERS STILL TAKEN", Size.X / 2, 40, 28, C.Caution, HorizontalAlignment.Center, 0, 600, 1);
        if (_x.State.PlacingHet) f.Draw(this, "CLICK WHERE TO TURN", Size.X / 2, 74, 20, C.Lilac, HorizontalAlignment.Center, 0, 600, 1);

        // the readout and the last officer's message, bottom left (Rules decides what is in arc and charged)
        float ry = Size.Y - 16, mw = Size.X * 0.6f;
        if (t != null)
        {
            Mount[] arc = me.Mounts.Where(m => !m.Weapon.IsPointDefence && m.State != Arming.Offline && Rules.InArc(me, m, t) && Rules.InRange(me, m, t)).ToArray();
            f.DrawRich(this, new[] { (t.Name, C.Sky), ($" {t.Class.Name} · range ", C.Lilac), ($"{Rules.DistKm(me, t):0.0} km", C.Sky), (" · speed ", C.Lilac), (Math.Round(t.V).ToString(), C.Sky) }, 18, ry - 28, 22, mw);
            f.DrawRich(this, new[] { ("in arc ", C.Lilac), (arc.Length.ToString(), C.Sky), (" · charged ", C.Lilac), (arc.Count(Rules.Ready).ToString(), C.Sky) }, 18, ry, 22, mw);
        }
        else f.DrawRich(this, new[] { ("No target", C.Sky), (" · click a ship to select it", C.Lilac) }, 18, ry, 22, mw);
        if (b.Log.Count > 0 && b.T - b.Log[^1].T < 5)
        {
            LogLine l = b.Log[^1];
            f.DrawRich(this, new[] { ($"{l.Who}: {l.Text}", C.Lilac) }, 18, ry - 64, 22, Size.X * 0.7f);
        }
        if (b.Over is { } over) Banner(over == "won" ? "Victory" : "Fleet lost", over == "won" ? C.Accord : C.Alert);

        // the Target box: the locked target's systems display, the same drawing as the ship panel (one component)
        if (_x.State.ShowTarget && t != null)
        {
            float w = Math.Min(320, Size.X * 0.3f), h = w * 0.86f, bx = Size.X - w - 16, by = 14;
            DrawRect(new Rect2(bx + 2, by + 2, w - 4, h - 4), Colors.Black);
            _x.Kit.Piece(this, "frame.inset", new Rect2(bx, by, w, h));
            f.Draw(this, $"TARGET · {t.Name.ToUpperInvariant()}", bx + 18, by + 34, 18, C.Sky, HorizontalAlignment.Left, w - 90, 600, 1);
            var xr = new Rect2(bx + w - 34, by + 14, 24, 24);
            HitState xs = _ui.Reg("v.untarget", xr, _ => B.Unlock(), null, new Tip("Release target"));
            DrawCircle(xr.GetCenter(), 12, xs.Hot ? C.Alert : C.Signal, true, -1, true);
            DrawLine(xr.Position + new Vector2(7, 7), xr.Position + new Vector2(17, 17), Colors.Black, 2.6f, true);
            DrawLine(xr.Position + new Vector2(17, 7), xr.Position + new Vector2(7, 17), Colors.Black, 2.6f, true);
            SystemsDisplay.Draw(this, _ui, _x, t, new Rect2(bx + 14, by + 44, w - 28, h - 58), false, "tgt");
        }
    }

    /// <summary>A pill key over the view, lit when on.</summary>
    private void Key(string id, Rect2 r, string label, Action click, bool on, string? tip = null)
    {
        HitState st = _ui.Reg(id, r, _ => click(), null, tip != null ? new Tip(tip) : null);
        _x.Kit.Piece(this, st.Down || on ? "button.pressed" : st.Hot ? "button.hover" : "button.idle", r);
        _x.Fonts.Draw(this, label.ToUpperInvariant(), r.GetCenter().X, r.GetCenter().Y + 6, 17, Colors.Black, HorizontalAlignment.Center, r.Size.X - 8, 600, 1);
    }

    /// <summary>A ship's hull: a short bar and its percentage, green above 60 %, amber above 30 %, red below (the same
    /// fraction the top strip's hull bars show).</summary>
    private void HullBar(float px, float py, Ship o)
    {
        float fr = (float)Math.Clamp(o.Hull / o.Class.HullHp, 0, 1), w = 92, h = 7;
        Color col = fr > 0.6f ? C.Accord : fr > 0.3f ? C.Caution : C.Alert;
        DrawRect(new Rect2(px - w / 2 - 2, py - 2, w + 4, h + 4), new Color(0, 0, 0, 0.6f));
        DrawRect(new Rect2(px - w / 2, py, w, h), C.Deep);
        DrawRect(new Rect2(px - w / 2, py, w * fr, h), col);
        _x.Fonts.Draw(this, $"{Math.Round(fr * 100)}%", px + w / 2 + 6, py + h, 14, col, HorizontalAlignment.Left, 0, 700);
    }

    // The 3D shield display (battle-console design): a flat hexagon in the ship's plane, an edge toward the bow, each
    // facing a stack of one to three glowing bands (strong three green, middling two amber, weak one red); a hit
    // flashes the inner band; an empty or lowered facing is one dark line.
    private void ShieldHex(Ship s)
    {
        double R = s.Class.LengthM * 0.62, step = s.Class.LengthM * 0.05;
        for (int i = 0; i < 6; i++)
        {
            double frac = s.ShieldsUp ? s.Shields[i] / s.Class.ShieldsHp[i] : 0;
            int bands = frac > 0.66 ? 3 : frac > 0.33 ? 2 : 1;
            Color col = frac > 0.01 ? SystemsDisplay.ShieldColorOf(C, frac) : C.Deep;
            for (int k = 0; k < bands; k++)
            {
                double r = R + k * step;
                double a0 = s.H + (i - 0.5) * Math.PI / 3, a1 = s.H + (i + 0.5) * Math.PI / 3;
                var p0 = Geometry.Forward(a0); var p1 = Geometry.Forward(a1);
                // the corners of a hexagon of circumradius r / cos(30) put the edge's middle at r
                double cr = r / Math.Cos(Math.PI / 6);
                if (Local(s.X + p0.X * cr, s.Z + p0.Z * cr) is not { } q0 || Local(s.X + p1.X * cr, s.Z + p1.Z * cr) is not { } q1) continue;
                Vector2 inset = (q1 - q0) * 0.06f;
                Color bc = k == 0 && s.Flash[i] > 0 ? col.Lerp(Colors.White, (float)s.Flash[i]) : col;
                DrawLine(q0 + inset, q1 - inset, new Color(bc, frac > 0.01 ? 0.85f : 0.6f), frac > 0.01 ? 3 : 2, true);
            }
        }
    }

    private void Banner(string text, Color col)
    {
        float w = Math.Min(Size.X - 40, 360);
        var r = new Rect2(Size.X / 2 - w / 2, 100, w, 44);
        Shapes.RoundRect(this, r, col, 22);
        _x.Fonts.Draw(this, text.ToUpperInvariant(), r.GetCenter().X, r.GetCenter().Y + 9, 24, Colors.Black, HorizontalAlignment.Center, w - 20, 700, 2);
    }

    private void RingArc(Ship s, float a0, float a1, Color col, float width)
    {
        int n = Math.Max(6, (int)(64 * (a1 - a0) / Mathf.Tau) + 2);
        var pts = new System.Collections.Generic.List<Vector2>();
        for (int i = 0; i <= n; i++)
        {
            double a = a0 + (a1 - a0) * i / n;
            var fw = Geometry.Forward(a);
            if (Local(s.X + fw.X * RingM, s.Z + fw.Z * RingM) is { } p) pts.Add(p);
        }
        if (pts.Count >= 2) DrawPolyline(pts.ToArray(), col, width, true);
    }
}
