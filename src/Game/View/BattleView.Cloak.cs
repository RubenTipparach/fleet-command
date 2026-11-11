// BattleView.Cloak: the cloak package in 3D. A cloaking ship's model fades toward clear as it cloaks, to a shimmer of the
// cloak's own_alpha for your own ship and a revealed enemy (the battle console mockup's cloakLook): every mesh of the model
// takes the fade as its GeometryInstance3D transparency, so the hull shader, the lamps and the scars all fade together
// and no material is shared with another ship. A cloaked enemy unseen is not drawn at all (Sync reads Battle.Detected).
// (The nebula plumes a cloak hides in are BattleView.Nebula's.)
using System;
using System.Collections.Generic;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class BattleView
{
    private Battle? _cloakFor;
    private readonly Dictionary<string, List<GeometryInstance3D>> _cloakMeshes = new();

    partial void SyncCloak(Battle b, double dt)
    {
        if (_cloakFor != b) { _cloakFor = b; _cloakMeshes.Clear(); }
        double own = b.Data.Systems.Cloak.Cloak.OwnAlpha, now = Time.GetTicksMsec() / 1000.0;
        foreach (Ship s in b.Ships)
        {
            if (!_models.TryGetValue(s.Id, out Node3D? m)) continue;
            bool known = _cloakMeshes.TryGetValue(s.Id, out List<GeometryInstance3D>? meshes);
            if (s.CloakK <= 0 && !known) continue;
            if (meshes == null) { meshes = new List<GeometryInstance3D>(); Collect(m, meshes); _cloakMeshes[s.Id] = meshes; }
            double a = 1 - s.CloakK * (1 - own * (0.8 + 0.2 * Math.Sin(now / 0.18)));
            float tr = (float)Math.Clamp(1 - a, 0, 1);
            foreach (GeometryInstance3D g in meshes) if (IsInstanceValid(g)) g.Transparency = tr;
        }
    }

    private static void Collect(Node n, List<GeometryInstance3D> into)
    {
        foreach (Node c in n.GetChildren())
        {
            if (c is GeometryInstance3D g) into.Add(g);
            Collect(c, into);
        }
    }
}
