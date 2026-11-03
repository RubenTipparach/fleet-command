// Imm: the console's immediate-mode input (battle-console design section 2, "Input is immediate mode"). Every control
// registers its rectangle as it is drawn; a press, a drag or a click is matched against the rectangles of the last
// frame drawn, topmost first. Hover cards come from the control under the pointer. One Imm per panel, in that panel's
// local coordinates.
using System;
using System.Collections.Generic;
using Godot;

namespace FleetCommand.Game.Ui;

/// <summary>A hover card: a title and rows of label, value and an optional value colour.</summary>
public sealed record Tip(string Title, IReadOnlyList<(string Label, string Value, Color? Col)> Rows)
{
    public Tip(string title) : this(title, Array.Empty<(string, string, Color?)>()) { }
}

public readonly record struct HitState(bool Hot, bool Down);

public sealed class Imm
{
    private sealed class Hit
    {
        public required string Id;
        public Rect2 R;
        public Action<Vector2>? Click;
        public Action<Vector2, bool>? Drag;
        public Tip? Tip;
        public Vector2[]? Poly;
    }

    private List<Hit> _cur = new(), _last = new();
    private Hit? _drag;
    public string? Hot { get; private set; }
    public string? Active { get; private set; }
    /// <summary>The pointer in the panel's coordinates, far away when it is elsewhere.</summary>
    public Vector2 Mouse { get; private set; } = new(-1e5f, -1e5f);

    public void Begin() => _cur = new List<Hit>();
    public void End() { _last = _cur; if (Hot != null && _last.Find(h => h.Id == Hot) == null) Hot = null; }

    /// <summary>Registers a control. A click fires on release over the same control; a drag gets every move while held.</summary>
    public HitState Reg(string id, Rect2 r, Action<Vector2>? click = null, Action<Vector2, bool>? drag = null, Tip? tip = null, Vector2[]? poly = null)
    {
        _cur.Add(new Hit { Id = id, R = r, Click = click, Drag = drag, Tip = tip, Poly = poly });
        return new HitState(Hot == id, Active == id && Hot == id);
    }

    /// <summary>The hover card of the control under the pointer, and that control's rectangle.</summary>
    public (Tip Tip, Rect2 R)? CurrentTip()
    {
        if (Hot == null) return null;
        Hit? h = _last.Find(x => x.Id == Hot);
        return h?.Tip != null ? (h.Tip, h.R) : null;
    }

    private Hit? TopAt(Vector2 p)
    {
        for (int i = _last.Count - 1; i >= 0; i--)
        {
            Hit h = _last[i];
            if (h.R.HasPoint(p) && (h.Poly == null || Geometry2D.IsPointInPolygon(p, h.Poly))) return h;
        }
        return null;
    }

    /// <summary>Feeds one input event; true when a control took it.</summary>
    public bool Handle(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion m:
                Mouse = m.Position;
                if (_drag != null) { _drag.Drag!(m.Position, false); return true; }
                Hot = TopAt(m.Position)?.Id;
                return Hot != null;
            case InputEventMouseButton b when b.ButtonIndex == MouseButton.Left:
                Mouse = b.Position;
                if (b.Pressed)
                {
                    Hit? h = TopAt(b.Position);
                    if (h == null) return false;
                    Active = h.Id; Hot = h.Id;
                    if (h.Drag != null) { _drag = h; h.Drag(b.Position, true); }
                    return true;
                }
                else
                {
                    if (_drag != null) { _drag = null; Active = null; return true; }
                    string? was = Active;
                    Active = null;
                    if (was == null) return false;
                    Hit? h = TopAt(b.Position);
                    if (h != null && h.Id == was) h.Click?.Invoke(b.Position);
                    return true;
                }
            default:
                return false;
        }
    }

    public void Leave() { if (_drag == null) Hot = null; Mouse = new Vector2(-1e5f, -1e5f); }
}

/// <summary>The one hover card on screen, offered by whichever panel has a control under the pointer.</summary>
public sealed class TipBoard
{
    public object? Owner { get; private set; }
    public Tip? Tip { get; private set; }
    /// <summary>The control's rectangle in the console's coordinates.</summary>
    public Rect2 Anchor { get; private set; }

    public void Offer(object owner, (Tip Tip, Rect2 R)? tip, Vector2 origin)
    {
        if (tip is { } t) { Owner = owner; Tip = t.Tip; Anchor = new Rect2(t.R.Position + origin, t.R.Size); }
        else if (Owner == owner) { Owner = null; Tip = null; }
    }
}
