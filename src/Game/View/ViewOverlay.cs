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

public sealed partial class ViewOverlay : Control, Ui.IControlSource
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

    /// <summary>The view's controls of the last frame, in screen (canvas) coordinates.</summary>
    public System.Collections.Generic.IEnumerable<(string Id, Rect2 R, bool Live)> Controls() =>
        _ui.Controls().Select(c => (c.Id, new Rect2(c.R.Position + GlobalPosition, c.R.Size), c.Live));

    /// <summary>The view rectangle, in screen coordinates.</summary>
    public Rect2 Screen => new(GlobalPosition, Size);

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
        if (_x.State.SceneOpen && _x.State.Expanded == null) SceneList();
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

        // the 3D shield display is always shown (owner, 2026-11-03: "Re move shield view toggle"): every ship's, drawn in the
        // scene by BattleView (ShieldShell)

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
                // both ends as the ships stand this frame (Battle.Ends, the rule the bolts fly by)
                var (sx, sz, tx, tz) = b.Ends(sh);
                if (Local(sx, sz) is { } a && Local(tx, tz) is { } bb)
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
        ScenePicker();
        if (!_x.State.SceneOpen) f.DrawRich(this, new[] { ("OBJECTIVE · ", C.Lilac), (b.Scenario.Objective.ToUpperInvariant(), C.Text) }, 14, 64, 16, Size.X * 0.62f);
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
            DrawRect(new Rect2(bx + 2, by + 2, w - 4, h - 4), _x.Kit.Style.R.Plate);
            _x.Kit.Piece(this, "frame.inset", new Rect2(bx, by, w, h));
            f.Draw(this, $"TARGET · {t.Name.ToUpperInvariant()}", bx + 18, by + 34, 18, C.Sky, HorizontalAlignment.Left, w - 90, 600, 1);
            var xr = new Rect2(bx + w - 34, by + 14, 24, 24);
            HitState xs = _ui.Reg("v.untarget", xr, _ => B.Unlock(), null, new Tip("Release target"));
            Color xf = xs.Hot ? C.Alert : C.Signal, xi = _x.Kit.Style.InkOn(xf);
            _x.Kit.Knob(this, xr.GetCenter(), 12, xf);
            DrawLine(xr.Position + new Vector2(7, 7), xr.Position + new Vector2(17, 17), xi, 2.6f, true);
            DrawLine(xr.Position + new Vector2(17, 7), xr.Position + new Vector2(7, 17), xi, 2.6f, true);
            SystemsDisplay.Draw(this, _ui, _x, t, new Rect2(bx + 14, by + 44, w - 28, h - 58), false, "tgt");
            // the camera's two keys under the box (battle-console design 4n): Target view, and Camera lock back on your ship
            float kw = (w - 6) / 2, ky = by + h + 6;
            Key("v.tview", new Rect2(bx, ky, kw, 30), "Target view", _view.ToggleTargetView, _view.TargetView, "The camera glides to the target and orbits it");
            Key("v.camlock", new Rect2(bx + kw + 6, ky, kw, 30), "Camera lock", _view.LockCamera, _view.CamLock && !_view.TargetView,
                "Keep the target ahead of your ship (a drag frees it)");
        }
    }

    private const float SceneX = 110, SceneW = 230, SceneRowH = 38, SceneHeadH = 26;

    // The scene selector (the battle console mockup's, owner 2026-11-02: "I would like a scene selector"): a pill naming the
    // scenario in play, with an arrow; a click opens the list. The objective sits under it while the list is shut.
    private void ScenePicker()
    {
        var r = new Rect2(SceneX, 12, SceneW, 30);
        HitState st = _ui.Reg("v.scene", r, _ => _x.State.SceneOpen = !_x.State.SceneOpen, null, new Tip("Pick a scenario"));
        Color fill = st.Hot || _x.State.SceneOpen ? C.Sky : C.Signal;
        Color ink = _x.Kit.Style.InkOn(fill);
        Shapes.RoundRect(this, r, fill, 15);
        _x.Fonts.Draw(this, "SCENE", r.Position.X + 14, r.Position.Y + 20, 13, ink, HorizontalAlignment.Left, 0, 600);
        _x.Fonts.Draw(this, B.Scenario.Name.ToUpperInvariant(), r.Position.X + 62, r.Position.Y + 21, 17, ink, HorizontalAlignment.Left, SceneW - 96, 700);
        float ax = r.End.X - 20, ay = r.Position.Y + 15;
        Vector2[] arrow = _x.State.SceneOpen
            ? new[] { new Vector2(ax - 6, ay + 3), new Vector2(ax + 6, ay + 3), new Vector2(ax, ay - 4) }
            : new[] { new Vector2(ax - 6, ay - 3), new Vector2(ax + 6, ay - 3), new Vector2(ax, ay + 4) };
        DrawColoredPolygon(arrow, ink);
    }

    // The open list, over everything in the view: the scenarios grouped by the side the player commands, each side under
    // its faction's name (picking one of the other side's swaps the console to that faction's skin). A click anywhere else
    // shuts it.
    private void SceneList()
    {
        _ui.Reg("v.scene.off", new Rect2(Vector2.Zero, Size), _ => _x.State.SceneOpen = false);
        var groups = _x.Data.Scenarios.GroupBy(s => s.Side).ToArray();
        float lx = SceneX, ly = 48, h = groups.Sum(g => SceneHeadH + g.Count() * SceneRowH) + 8;
        Shapes.RoundRect(this, new Rect2(lx - 6, ly - 6, SceneW + 12, h), new Color(0, 0, 0, 0.85f), 10);
        float y = ly;
        foreach (var g in groups)
        {
            _x.Fonts.Draw(this, _x.Data.Factions[g.Key].Name.ToUpperInvariant(), lx + 6, y + 18, 14, C.Lilac, HorizontalAlignment.Left, SceneW - 12, 600, 1);
            y += SceneHeadH;
            foreach (Data.Scenario sc in g)
            {
                string id = sc.Id;
                Key("v.scene." + id, new Rect2(lx, y, SceneW, 32), sc.Name, () => _x.Act.Scene(id), id == B.Scenario.Id, sc.Objective);
                y += SceneRowH;
            }
        }
    }

    /// <summary>A key over the view, lit when on, its label in the skin's key ink.</summary>
    private void Key(string id, Rect2 r, string label, Action click, bool on, string? tip = null)
    {
        HitState st = _ui.Reg(id, r, _ => click(), null, tip != null ? new Tip(tip) : null);
        string kst = st.Down || on ? "pressed" : st.Hot ? "hover" : "idle";
        _x.Kit.Piece(this, "button." + kst, r);
        _x.Fonts.Draw(this, label.ToUpperInvariant(), r.GetCenter().X, r.GetCenter().Y + 6, 17, _x.Kit.Style.KeyInk.For(kst), HorizontalAlignment.Center, r.Size.X - 8, 600, 1);
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

    private void Banner(string text, Color col)
    {
        float w = Math.Min(Size.X - 40, 360);
        var r = new Rect2(Size.X / 2 - w / 2, 100, w, 44);
        _x.Kit.Shape(this, r, 22, col);
        _x.Fonts.Draw(this, text.ToUpperInvariant(), r.GetCenter().X, r.GetCenter().Y + 9, 24, _x.Kit.Style.InkOn(col), HorizontalAlignment.Center, w - 20, 700, 2);
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
