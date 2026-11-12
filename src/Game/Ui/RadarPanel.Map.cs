// RadarPanel.Map: the tactical map, the radar row expanded (battle-console design; the mockup's uiMap and drawMap): its
// range as tabs (tactical.json map_scales_km), and the battle from above centred on your ship, north up: range rings every
// 5 km, the selected weapons' arcs out to their reach, the shots in flight, and every ship as a ring with its heading, the
// locked target bracketed, names beside them. A click on a ship locks an enemy, or opens a ship of your fleet on the Fleet
// station. The map draws in a child control that clips to its box, so a ring wider than the box stops at its edge.
using System;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

/// <summary>A box the tactical map draws into, clipped to its rectangle; it takes no input (the radar panel does).</summary>
public sealed partial class MapCanvas : Control
{
    public Action<Control>? Paint;
    public override void _Draw() { Paint?.Invoke(this); Draw2D.Flush(this); }
}

public sealed partial class RadarPanel
{
    private MapCanvas? _canvas;

    private double MapKm()
    {
        var scales = X.Data.Systems.Tactical.MapScalesKm;
        return scales.Contains(X.State.MapScaleKm) ? X.State.MapScaleKm : scales[scales.Count / 2];
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_canvas != null) { _canvas.Visible = Big; _canvas.QueueRedraw(); }
    }

    private void Map()
    {
        Rect2 p = Frame("Tactical map");
        var scales = X.Data.Systems.Tactical.MapScalesKm;
        double km = MapKm();
        Tabs("map.s", new Rect2(p.Position, new Vector2(p.Size.X, 40)), scales.Select(k => (k.ToString(System.Globalization.CultureInfo.InvariantCulture), $"{k:0} km")).ToArray(),
            km.ToString(System.Globalization.CultureInfo.InvariantCulture), v => X.State.MapScaleKm = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture));
        var b = new Rect2(p.Position.X, p.Position.Y + 50, p.Size.X, p.Size.Y - 50);
        if (_canvas == null)
        {
            _canvas = new MapCanvas { Name = "Map", MouseFilter = MouseFilterEnum.Ignore, ClipContents = true };
            AddChild(_canvas);
        }
        _canvas.Position = b.Position;
        _canvas.Size = b.Size;
        _canvas.Paint = c => DrawMap(c, b.Size, km);
        Ui.Reg("map", b, at => MapClick(at - b.Position, b.Size, km));
    }

    // a point of the battle plane (metres) on the map, centred on your ship
    private Vector2 MapAt(double xm, double zm, Vector2 size, double km)
    {
        double k = Math.Min(size.X, size.Y) / 2 / (km * 1000);
        return size / 2 + new Vector2((float)((xm - Me.X) * k), (float)((zm - Me.Z) * k));
    }

    private void MapClick(Vector2 at, Vector2 size, double km)
    {
        Ship? best = null;
        float bd = 30;
        foreach (Ship o in B.Ships.Where(o => o.Alive && (o == Me || B.Detected(Me, o))))
        {
            float d = MapAt(o.X, o.Z, size, km).DistanceTo(at);
            if (d < bd) { bd = d; best = o; }
        }
        if (best == null) return;
        if (best.Side != Me.Side) B.Lock(best.Id);
        else { X.State.FleetPick = best.Id; X.State.Station = "fleet"; }
    }

    // a plume's colour on the map and the dial: a hazard's caution for an ion storm, the notes' lilac for a plume
    private Color PlumeColor(NebulaDef n) => n.Storm ? C.Caution : C.Lilac;

    private void DrawMap(Control c, Vector2 size, double km)
    {
        Ship me = Me;
        float k = (float)(Math.Min(size.X, size.Y) / 2 / (km * 1000));
        Vector2 mid = size / 2;
        // range rings every 5 km, labelled
        for (int r = 50; r <= km * 1.5; r += 50)
        {
            Draw2D.Arc(c, mid, r * 1000 * k, 0, Mathf.Tau, 96, C.Deep, 1.5f, true);
            X.Fonts.Draw(c, $"{r} KM", mid.X + r * 1000 * k + 4, mid.Y - 4, 14, C.Lilac, HorizontalAlignment.Left, 0, 600);
        }
        // the nebula plumes, each the circle the rules test (Rules.NebulaAt), an ion storm in the caution colour and named
        foreach (NebulaDef n in B.Rules.Nebulae)
        {
            Vector2 np = MapAt(n.XM, n.ZM, size, km);
            float nr = (float)n.RM * k;
            Color nc = PlumeColor(n);
            Draw2D.Circle(c, np, nr, new Color(nc, 0.12f), true, -1, true);
            Draw2D.Arc(c, np, nr, 0, Mathf.Tau, 64, new Color(nc, 0.6f), 1.5f, true);
            X.Fonts.Draw(c, n.Storm ? "ION STORM" : "NEBULA", np.X, np.Y - nr - 6, 14, nc, HorizontalAlignment.Center, 200, 600);
        }
        // the selected weapons' arcs out to their reach, faint
        foreach (Mount m in me.Mounts.Where(m => m.Selected && !m.Weapon.IsPointDefence && m.Def.ArcWidthDeg < 360))
        {
            float a0 = (float)(me.H + (m.Def.ArcCentreDeg - m.Def.ArcWidthDeg / 2) * Geometry.Deg - Math.PI / 2), a1 = a0 + (float)(m.Def.ArcWidthDeg * Geometry.Deg);
            float rr = (float)(B.Rules.RangeKm(me, m) * 1000) * k;
            var pts = new Vector2[26];
            pts[0] = mid;
            for (int i = 0; i <= 24; i++) { float a = a0 + (a1 - a0) * i / 24; pts[i + 1] = mid + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr; }
            Draw2D.ColoredPolygon(c, pts, new Color(C.Sky, 0.05f));
        }
        // the shots in flight
        foreach (Shot sh in B.Shots.Where(sh => sh.Kind != "beam"))
        {
            Vector2 sp = MapAt(sh.X, sh.Z, size, km);
            Draw2D.Rect(c, new Rect2(sp - new Vector2(2, 2), new Vector2(4, 4)), Color.FromHtml(sh.Color));
        }
        // your sensors' reach, a faint ring: beyond it an enemy is an unknown contact (Battle.Identified)
        Draw2D.Arc(c, mid, (float)(B.SensorKm(me) * 1000) * k, 0, Mathf.Tau, 96, new Color(C.Sky, 0.35f), 1.5f, true);
        // every ship seen (a ship not yet arrived or cloaked unseen is not on the map): a ring with its heading, the target
        // bracketed, its name as the sensors know it
        foreach (Ship o in B.Ships.Where(o => o.Alive && (o == me || B.Detected(me, o))))
        {
            Vector2 sp = MapAt(o.X, o.Z, size, km);
            Color col = o == me ? C.Sky : Aiming.ContactColor(C, me, o);
            var f = Geometry.Forward(o.H);
            var fv = new Vector2((float)f.X, (float)f.Z);
            Draw2D.Arc(c, sp, 7, 0, Mathf.Tau, 20, col, 2, true);
            Draw2D.Line(c, sp + fv * 7, sp + fv * 20, col, 2, true);
            if (o.Id == me.Target)
                foreach (var (sx, sy) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
                {
                    var corner = sp + new Vector2(sx * 34, sy * 34);
                    Draw2D.Polyline(c, new[] { corner - new Vector2(sx * 14, 0), corner, corner - new Vector2(0, sy * 14) }, o.Side != me.Side ? C.Alert : C.Accord, 3, true);
                }
            X.Fonts.Draw(c, Aiming.ContactName(B, o).ToUpperInvariant(), sp.X + 12, sp.Y + 18, 16, C.Text, HorizontalAlignment.Left, 0, 600);
        }
    }
}
