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
        if (what == NotificationMouseExit) _mouse = new Vector2(-1e5f, -1e5f);
    }

    private void SteerTo(Vector2 local)
    {
        if (_view.PlaneAt(local + Position) is { } q) B.HelmTo(q.X, q.Z);
    }

    private void LeftClick(Vector2 local)
    {
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
        Fonts f = _x.Fonts;
        Battle b = B;
        Ship me = b.Player;
        Ship? t = b.TargetOf(me);

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

        // contacts: a ring each, red for enemies, green for friendlies; the locked target bracketed with its details
        foreach (Ship s in b.Ships)
        {
            if (!s.Alive || s == me) continue;
            if (Local(s.X, s.Z) is not { } p || !new Rect2(Vector2.Zero, Size).Grow(-4).HasPoint(p)) continue;
            bool hot = p.DistanceTo(_mouse) < 30, foe = s.Side != me.Side;
            Color col = foe ? C.Alert : C.Accord;
            float r = 16 * (hot ? 1.18f : 1);
            DrawArc(p, r, 0, Mathf.Tau, 32, hot ? col.Lightened(0.25f) : new Color(col, 0.8f), hot ? 3 : 2, true);
            if (s == t)
            {
                float br = 30 * (hot ? 1.08f : 1), arm = 10;
                foreach (var (sx, sy) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
                {
                    var c0 = p + new Vector2(sx * br, sy * br);
                    DrawLine(c0, c0 - new Vector2(sx * arm, 0), C.Alert, 3, true);
                    DrawLine(c0, c0 - new Vector2(0, sy * arm), C.Alert, 3, true);
                }
                f.Draw(this, s.Name.ToUpperInvariant(), p.X + br + 8, p.Y - 8, 18, C.Text, HorizontalAlignment.Left, 220, 700, 1);
                f.Draw(this, $"{s.Class.Name.ToUpperInvariant()} · {Rules.DistKm(me, s):0.0} KM · {Math.Round(s.V)} M/S", p.X + br + 8, p.Y + 12, 14, C.Lilac, HorizontalAlignment.Left, 260, 600, 1);
            }
            else if (hot || foe)
                f.Draw(this, s.Name.ToUpperInvariant(), p.X + r + 6, p.Y + 5, 14, new Color(col, hot ? 1 : 0.75f), HorizontalAlignment.Left, 160, 600, 1);
        }

        // the readout: range, weapons in arc and charged, the best chance to hit (all from Rules)
        if (t != null)
        {
            int inArc = me.Mounts.Count(m => !m.Weapon.IsPointDefence && m.Hp > 0 && Rules.InArc(me, m, t) && Rules.InRange(me, m, t));
            int charged = me.Mounts.Count(m => !m.Weapon.IsPointDefence && Rules.Ready(m));
            double best = me.Mounts.Where(m => !m.Weapon.IsPointDefence).Select(m => b.Rules.HitChance(me, m, t)).DefaultIfEmpty(0).Max();
            Shapes.RoundRect(this, new Rect2(14, 14, 300, 84), new Color(0, 0, 0, 0.65f), 10);
            f.Draw(this, $"TARGET {t.Name.ToUpperInvariant()}", 26, 38, 18, C.Sky, HorizontalAlignment.Left, 280, 700, 1);
            f.Draw(this, $"RANGE {Rules.DistKm(me, t):0.0} KM   IN ARC {inArc}   CHARGED {charged}", 26, 62, 15, C.Text, HorizontalAlignment.Left, 280, 600, 1);
            f.Draw(this, $"BEST TO HIT {Math.Round(best * 100)} %", 26, 86, 15, C.Lilac, HorizontalAlignment.Left, 280, 600, 1);
        }

        // the last officer's message, held 8 s
        if (b.Log.Count > 0 && b.T - b.Log[^1].T < 8)
        {
            LogLine l = b.Log[^1];
            f.Draw(this, $"{l.Who.ToUpperInvariant()}: {l.Text}", 18, Size.Y - 22, 17, C.Text, HorizontalAlignment.Left, Size.X - 36, 600);
        }

        if (_x.State.Paused) Banner("Paused", C.Lilac);
        if (b.Over is { } over) Banner(over == "won" ? "Victory" : "Fleet lost", over == "won" ? C.Accord : C.Alert);
        if (_x.State.Expanded != null) f.Draw(this, "CLICK THE BATTLE TO RETURN", Size.X / 2, Size.Y - 56, 15, C.Lilac, HorizontalAlignment.Center, Size.X - 20, 600, 1);
    }

    private void Banner(string text, Color col)
    {
        float w = Math.Min(Size.X - 40, 360);
        var r = new Rect2(Size.X / 2 - w / 2, 56, w, 44);
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
