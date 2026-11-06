// StationPanel.Repair: the Repair station (openspec/changes/damage-control; the battle console mockup's stRepair): the
// repair queue, ordered by drag and drop (owner, 2026-11-04: "Drag and drop interface for repairs"). Each job is a row in
// the order the crews take it, with a grip; drag a row up or down and the queue reorders as it passes the others, the
// row riding under the pointer, and the drop says where it landed. Each row shows its health, the crews on it now and,
// expanded, how long until it is whole; the crews (repair energy) at the foot. Every figure is Battle.Repair's.
using System;
using FleetCommand.Data;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class StationPanel
{
    partial void RepairPage(Rect2 b, ref bool drawn)
    {
        drawn = true;
        Ship s = Me;
        var q = s.RepairQueue;
        float rh = Big ? 52 : 34, top = b.Position.Y + 22, rw = b.Size.X, x = b.Position.X;
        _rowX = x; _rowW = rw;
        // a drag that has ended (the button let go): the drop says where the job landed
        ConsoleState cs = X.State;
        if (cs.RepairDrag is { } dk && Ui.Active != $"rep.row.{dk}") { B.RepairDropped(dk, cs.RepairDragFrom); cs.RepairDrag = null; }
        Lbl("Repair queue · drag to reorder", x, b.Position.Y + 13, rw);
        string? dragging = cs.RepairDrag;
        for (int i = 0; i < q.Count; i++)
        {
            float y = top + i * rh;
            if (q[i] == dragging) { Kit.Shape(this, new Rect2(x, y, rw, rh - 4), (rh - 4) / 2, new Color(C.Sky, 0.6f), 1.5f); RegRow(q[i], y, rh, top); }
            else Row(q[i], i, y, rh, top, false);
        }
        // the dragged row last, riding under the pointer inside the queue
        if (dragging != null && q.IndexOf(dragging) is var di and >= 0)
            Row(dragging, di, Math.Clamp(Ui.Mouse.Y - cs.RepairDragDy, top - 4, top + (q.Count - 1) * rh + 4), rh, top, true);
        PowerRow("rep.pw", x, Math.Min(b.End.Y - 28, top + q.Count * rh + 6), rw, 26, "repair", Big ? 120 : 82);
    }

    // a row's control: a drag that moves its job through the queue as it passes the others
    private void RegRow(string k, float y, float rh, float top)
    {
        Ship s = Me;
        RepairJob job = B.Data.Systems.Repair.Job(k);
        double h = Math.Clamp(Battle.JobHealth(s, k), 0, 1), eta = B.JobEta(s, k);
        SystemStatus st = B.Rules.StatusOf(h);
        var tip = new Tip(job.Name, new (string, string, Color?)[]
        {
            ("Health", Pct(h), null), ("Status", st.ToString(), StatusColor(st)), ("Crews", B.CrewsOn(s, k).ToString(), null),
            ("Whole in", eta == 0 ? "Whole" : double.IsInfinity(eta) ? "No crews" : $"{Math.Ceiling(eta)} s", null), ("Drag", "Reorder the queue", null),
        });
        Ui.Reg($"rep.row.{k}", new Rect2(_rowX, y, _rowW, rh - 4), null, (p, first) =>
        {
            ConsoleState cs = X.State;
            if (first) { cs.RepairDrag = k; cs.RepairDragFrom = Me.RepairQueue.IndexOf(k); cs.RepairDragDy = p.Y - y; return; }
            int n = Me.RepairQueue.Count;
            B.RepairMove(k, Math.Clamp((int)Math.Floor((p.Y - cs.RepairDragDy + rh / 2 - top) / rh), 0, n - 1), true);
        }, tip);
    }

    // the queue's x and width this frame (the station's content box)
    private float _rowX, _rowW;

    private void Row(string k, int i, float y, float rh, float top, bool lifted)
    {
        Ship s = Me;
        RepairJob job = B.Data.Systems.Repair.Job(k);
        int crews = B.CrewsOn(s, k), max = B.Data.Systems.Repair.MaxCrews;
        double h = Math.Clamp(Battle.JobHealth(s, k), 0, 1), eta = B.JobEta(s, k);
        SystemStatus st = B.Rules.StatusOf(h);
        float x = _rowX, rw = _rowW, mid = y + (rh - 4) / 2;
        bool hot = Ui.Hot == $"rep.row.{k}";
        if (!lifted) RegRow(k, y, rh, top);
        if (lifted) { Kit.Shape(this, new Rect2(x, y, rw, rh - 4), (rh - 4) / 2, new Color(C.Sky, 0.25f)); Kit.Shape(this, new Rect2(x, y, rw, rh - 4), (rh - 4) / 2, C.Sky, 2); }
        else if (crews > 0 || hot) Kit.Shape(this, new Rect2(x, y, rw, rh - 4), (rh - 4) / 2, new Color(C.Deep, crews > 0 ? 0.7f : 0.4f));
        // the grip: three short rules
        for (int r2 = -1; r2 <= 1; r2++) DrawLine(new Vector2(x + 8, mid + r2 * 5), new Vector2(x + 18, mid + r2 * 5), lifted || hot ? C.Sky : C.Lilac, 2);
        Num((i + 1).ToString(), x + 32, y + rh * 0.62f, 0, C.Lilac, HorizontalAlignment.Center, Big ? 20 : 16);
        float isz = Big ? 30 : 22;
        Kit.Icon(this, job.Icon, new Rect2(x + 44, y + (rh - 4 - isz) / 2, isz, isz), C.Sky);
        float nx = x + 48 + isz, nw = Big ? 230 : 104, ws = Big ? isz * 0.8f : 13;
        Text(Big ? job.Name : job.Short, nx, y + rh * 0.62f, Big ? 19 : 16, h >= 1 ? Sk.R.Ghost : C.Text, HorizontalAlignment.Left, nw - 8);
        // the crews at work: a wrench each, bobbing while it works
        for (int c = 0; c < crews; c++)
        {
            float bob = Mathf.Sin(Now * 6 + c * 1.7f) * 2;
            Kit.Icon(this, "repair", new Rect2(nx + nw + c * ws - ws * 0.1f, mid - ws * 0.6f + bob, ws * 1.2f, ws * 1.2f), C.Accord);
        }
        float hx = nx + nw + max * ws + 6, tail = Big ? 170 : 52, bw = x + rw - tail - hx - 8;
        if (bw > 30)
        {
            Kit.Piece(this, "bar.track", new Rect2(hx, mid - 8, bw, 16));
            if (h > 0.005) Kit.Piece(this, h < 0.35 ? "bar.fill.red" : "bar.fill", new Rect2(hx + 3, mid - 5, Math.Max(10, (bw - 6) * (float)h), 10));
        }
        Color sc = StatusColor(st);
        Num(Pct(h), Big ? x + rw - 100 : x + rw - 6, y + rh * 0.62f, 0, h >= 1 ? C.Accord : sc, HorizontalAlignment.Right, Big ? 20 : 16);
        if (Big) Num(eta == 0 ? "--" : double.IsInfinity(eta) ? "Idle" : $"{Math.Ceiling(eta)} S", x + rw - 8, y + rh * 0.62f, 0, crews > 0 ? C.Sky : C.Lilac, HorizontalAlignment.Right, 18);
    }
}
