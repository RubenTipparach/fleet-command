// BattleView.Stack: ships drawn one above another where they overlap (owner, 2026-11-09: "in SFC smaller ships would change
// their height and fly above larger ships so that ships wouldnt clip inside of each other"). The rule is the Core's
// (Sim/Stacking.cs, tested headless); this measures each model's box once, asks the rule for each frame's heights, and
// eases every lift toward its height over StackEaseS (owner, 2026-11-10: "Ships move up and down too abruptly. They should
// take like 5 seconds or something to move up or down to avoid clipping though"): the heights are the ones each ship needs
// now or within the next StackAheadS (Stacking.HeightsAhead), so a slow climb starts before the hulls would meet. Shots, the target bracket and the hull bars follow a ship's lift
// (ShotView.Lift, LiftOf). The battle itself stays flat.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class BattleView
{
    /// <summary>The time a lift takes to settle, seconds, and how far ahead the heights look (so the climb is done before the
    /// hulls meet), and in how many moments.</summary>
    public const float StackEaseS = 5f, StackAheadS = 5f;
    public const int StackAheadSamples = 5;

    // each model's box in its own frame, scene units: half its width (x) and length (z), its lowest and highest points
    private readonly Dictionary<string, (float HalfW, float HalfL, float Lo, float Hi)> _extent = new();
    private readonly Dictionary<string, float> _lift = new();

    /// <summary>How high the ship is drawn above the battle's plane, scene units (0 for a ship not lifted, or not drawn).</summary>
    public float LiftOf(string? id) => id != null && _lift.TryGetValue(id, out float y) ? y : 0;

    private void Measure(Ship s, Node3D model)
    {
        Aabb a = Bounds(model, Transform3D.Identity);
        float len = (float)(s.Class.FrameM / MPerU);
        // a stand-in with no mesh under it still takes room: a box its class's length, a third as wide and a tenth as high
        _extent[s.Id] = a.Size == Vector3.Zero ? (len / 6, len / 2, -len / 20, len / 20)
            : (Math.Max(Math.Abs(a.Position.X), Math.Abs(a.End.X)), Math.Max(Math.Abs(a.Position.Z), Math.Abs(a.End.Z)), a.Position.Y, a.End.Y);
        _lift[s.Id] = 0;
    }

    /// <summary>Each ship's height this frame (Stacking.Heights), each lift eased toward it by dt seconds.</summary>
    private void Stack(Battle b, double dt)
    {
        var hulls = b.Ships.Where(s => s.Alive && !s.Jumped && _extent.ContainsKey(s.Id)).Select(s =>
        {
            var (w, l, lo, hi) = _extent[s.Id];
            var (fx, fz) = Geometry.Forward(s.H);
            return new Stacking.Hull(s.Id, s.X / MPerU, s.Z / MPerU, s.H, w, l, lo, hi, s == b.Player, s.Order, fx * s.V / MPerU, fz * s.V / MPerU);
        }).ToList();
        // eased so it is most of the way there by StackEaseS (95 %), smoothly, never a jump
        float k = dt > 0 ? 1 - Mathf.Exp(-(float)dt * 3 / StackEaseS) : 0;
        foreach (var (id, y) in Stacking.HeightsAhead(hulls, StackAheadS, StackAheadSamples))
        {
            float now = _lift.TryGetValue(id, out float l) ? l : 0;
            _lift[id] = now + ((float)y - now) * k;
        }
    }
}
