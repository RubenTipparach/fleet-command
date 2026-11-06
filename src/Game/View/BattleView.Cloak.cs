// BattleView.Cloak: the cloak package in 3D. A cloaking ship's model fades toward clear as it cloaks, to a shimmer of the
// cloak's own_alpha for your own ship and a revealed enemy (the battle console mockup's cloakLook): every mesh of the model
// takes the fade as its GeometryInstance3D transparency, so the hull shader, the lamps and the scars all fade together
// and no material is shared with another ship. A cloaked enemy unseen is not drawn at all (Sync reads Battle.Detected).
// A nebula is soft violet puffs of glow, seeded, filling its circle on the battle plane (the mockup's syncNebula).
using System;
using System.Collections.Generic;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class BattleView
{
    private Battle? _cloakFor;
    private readonly Dictionary<string, List<GeometryInstance3D>> _cloakMeshes = new();
    private Node3D? _nebula;

    partial void SyncCloak(Battle b, double dt)
    {
        if (_cloakFor != b) { _cloakFor = b; _cloakMeshes.Clear(); BuildNebula(b); }
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

    // The nebula's puffs, made once a battle: 70 soft glows of violet, seeded from the scenario, at its density.
    private void BuildNebula(Battle b)
    {
        _nebula?.QueueFree();
        _nebula = null;
        if (b.Scenario.Nebula is not { } n) return;
        _nebula = new Node3D { Name = "Nebula" };
        AddChild(_nebula);
        var r = Rng.Fork(b.Scenario.Seed, "nebula");
        float rU = (float)(n.RM / MPerU);
        var tex = new GradientTexture2D
        {
            Width = 64, Height = 64, Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f),
            Gradient = new Gradient { Offsets = new[] { 0f, 0.4f, 1f }, Colors = new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0.45f), new Color(1, 1, 1, 0) } },
        };
        for (int i = 0; i < 70; i++)
        {
            double a = r.Next() * Math.Tau, d = Math.Sqrt(r.Next()) * rU, sz = rU * (0.25 + r.Next() * 0.35);
            // the mockup's colour, HSL (0.76 to 0.84, 0.6, 0.45 to 0.6), as HSV
            double hue = 0.76 + r.Next() * 0.08, l = 0.45 + r.Next() * 0.15, v = l + 0.6 * Math.Min(l, 1 - l);
            Color col = Color.FromHsv((float)hue, (float)(2 * (1 - l / v)), (float)v, (float)(0.16 * n.Density));
            var mat = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha, BlendMode = BaseMaterial3D.BlendModeEnum.Add, AlbedoTexture = tex, AlbedoColor = col,
                DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled, DisableReceiveShadows = true,
            };
            var q = new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2((float)sz, (float)sz) }, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
            q.Position = new Vector3((float)(n.XM / MPerU + Math.Cos(a) * d), (float)((r.Next() - 0.5) * rU * 0.25), (float)(n.ZM / MPerU + Math.Sin(a) * d));
            _nebula.AddChild(q);
        }
    }
}
