// BattleView.Reactor: the reactor package in 3D (the battle console mockup's syncWells and the warp-out): each gravity
// well as a white body with a ring where its field passes the override's threshold, and a ship jumping to warp drawn out
// along its heading as it streaks away, with a flash as its field comes up and as it goes. The battle decides everything;
// this only draws it.
using System.Collections.Generic;
using FleetCommand.Data;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class BattleView
{
    private Node3D? _wells;
    private Battle? _wellsFor;
    private readonly Dictionary<string, bool> _jumping = new();
    private readonly Dictionary<string, Vector3> _baseScale = new();

    partial void SyncReactor(Battle b, double dt)
    {
        if (_wellsFor != b) BuildWells(b);
        WarpDef W = b.Data.Systems.Warp;
        foreach (Ship s in b.Ships)
        {
            if (!_models.TryGetValue(s.Id, out Node3D? m)) continue;
            bool out_ = s.Warp.Out > 0;
            _jumping.TryGetValue(s.Id, out bool was);
            var at = new Vector3((float)(s.X / MPerU), 0, (float)(s.Z / MPerU));
            if (out_ && !was) { Fx.Flash(this, at, 30, 4, 1.2f); Fx.Streaks(this, at, 24, 1.5f, 12, 0.8f); }
            if (!out_ && was && s.Jumped) Fx.Flash(this, at, 40, 6, 0.8f);
            _jumping[s.Id] = out_;
            // drawn out along its heading while the jump runs (the model's bow is -Z)
            if (!_baseScale.TryGetValue(s.Id, out Vector3 sc)) _baseScale[s.Id] = sc = m.Scale;
            float k = out_ ? 1 - (float)(s.Warp.Out / W.OutS) : 0;
            m.Scale = sc * new Vector3(1 - 0.4f * k, 1 - 0.4f * k, 1 + 8 * k * k);
        }
    }

    // the scenario's gravity wells, once per battle
    private void BuildWells(Battle b)
    {
        _wellsFor = b;
        _jumping.Clear();
        _baseScale.Clear();
        _wells?.QueueFree();
        _wells = new Node3D { Name = "Wells" };
        AddChild(_wells);
        if (b.Scenario.Wells is not { } wells) return;
        double block = b.Data.Systems.Warp.QgBlock;
        foreach (WellDef w in wells)
        {
            var at = new Vector3((float)(w.XM / MPerU), 0, (float)(w.ZM / MPerU));
            var body = new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = (float)(w.RadiusM / MPerU), Height = (float)(2 * w.RadiusM / MPerU) },
                MaterialOverride = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color("#e8f0ff"), EmissionEnabled = true, Emission = new Color("#c8d8ff"), EmissionEnergyMultiplier = 3 },
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            _wells.AddChild(body);
            body.Position = at;
            _wells.AddChild(new OmniLight3D { Position = at, OmniRange = (float)(w.ReachM / MPerU), LightEnergy = 1.5f, LightColor = new Color("#dde6ff"), ShadowEnabled = false });
            // where the field passes the override's threshold
            if (w.Strength <= block) continue;
            float r = (float)(w.ReachM * (1 - block / w.Strength) / MPerU);
            var ring = new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = r - 0.15f, OuterRadius = r + 0.15f, Rings = 96, RingSegments = 6 },
                MaterialOverride = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(0.94f, 0.7f, 0.24f, 0.5f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha },
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            _wells.AddChild(ring);
            ring.Position = at + new Vector3(0, 0.02f, 0);
            ring.Scale = new Vector3(1, 0.05f, 1);
        }
    }
}
