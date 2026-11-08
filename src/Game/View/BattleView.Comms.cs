// BattleView.Comms: comms and transport in 3D (the battle console mockup's syncPlanets, beamFx, beamPair and beamStream):
// a scene's planets (drawn by BattleView.Planets), and the transporter effect for every beam the battle
// records (Battle.Beams): at each end a column of light filled with climbing motes (transporter.gdshader), a ring spreading
// on the plane and a flash, a planet's end wider; and a stream of motes flying from the sender to the receiver on a low arc
// (owner, 2026-11-03: "I want particles flying from one ship to the other ... so that I can tell what ship transported to
// what other ship"). Sky for crew and cargo, Caution for a mine. Sizes from data/battle/systems/comms.json's beam_fx, in
// metres; the view only reads the battle.
using System;
using System.Collections.Generic;
using FleetCommand.Data;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class BattleView
{
    private sealed class BeamFxNode
    {
        public required MeshInstance3D Column, Ring, Flash;
        public required ShaderMaterial Mat;
        public required float R;
        public float T;
    }

    private sealed class StreamFx
    {
        public required MultiMeshInstance3D Motes;
        public required Vector3 A, B;
        public required Color Col;
        public float T;
    }

    private Battle? _commsFor;
    private long _beamSeen;
    private readonly List<BeamFxNode> _beamFx = new();
    private readonly List<StreamFx> _streams = new();
    private static Shader? _tpShader;
    private static Mesh? _tpColumn, _tpRing, _tpQuad;
    private static Texture2D? _glow;

    partial void SyncComms(Battle b, double dt)
    {
        if (_commsFor != b)
        {
            // a new battle: its planets, and no effect left over from the last one
            _commsFor = b;
            _beamSeen = b.Beams.Count > 0 ? b.Beams[^1].Seq : 0;
            foreach (BeamFxNode n in _beamFx) { n.Column.QueueFree(); n.Ring.QueueFree(); n.Flash.QueueFree(); }
            foreach (StreamFx s in _streams) s.Motes.QueueFree();
            _beamFx.Clear(); _streams.Clear();
            BuildPlanets(b);
        }
        BeamFxDef fx = b.Comms.Fx;
        foreach (BeamEvent e in b.Beams)
        {
            if (e.Seq <= _beamSeen) continue;
            _beamSeen = e.Seq;
            Color col = e.Kind == "mine" ? _x.Kit.C.Caution : _x.Kit.C.Sky;
            float r = (float)(fx.ColumnRM / MPerU);
            Column(e.AX, e.AZ, col, e.ABig ? r * (float)fx.PlanetX : r, fx);
            Column(e.BX, e.BZ, col, e.BBig ? r * (float)fx.PlanetX : r, fx);
            Stream(new Vector3((float)(e.AX / MPerU), 0, (float)(e.AZ / MPerU)), new Vector3((float)(e.BX / MPerU), 0, (float)(e.BZ / MPerU)), col, fx);
        }
        StepBeams((float)dt, fx);
        StepStreams((float)dt, fx);
    }

    private static Texture2D Glow => _glow ??= new GradientTexture2D
    {
        Width = 64, Height = 64, Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f),
        Gradient = new Gradient { Offsets = new[] { 0f, 0.35f, 1f }, Colors = new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0.4f), new Color(1, 1, 1, 0) } },
    };

    private static StandardMaterial3D Additive(Color col, bool billboard) => new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, BlendMode = BaseMaterial3D.BlendModeEnum.Add,
        AlbedoColor = col, AlbedoTexture = billboard ? Glow : null, BillboardMode = billboard ? BaseMaterial3D.BillboardModeEnum.Enabled : BaseMaterial3D.BillboardModeEnum.Disabled,
        DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled, CullMode = BaseMaterial3D.CullModeEnum.Disabled, DisableReceiveShadows = true,
        VertexColorUseAsAlbedo = billboard,
    };

    // one end of a beam: the column of light, the ring on the plane and the flash
    private void Column(double xm, double zm, Color col, float r, BeamFxDef fx)
    {
        _tpShader ??= GD.Load<Shader>("res://src/Game/View/transporter.gdshader");
        _tpColumn ??= new CylinderMesh { TopRadius = 1, BottomRadius = 0.7f, Height = 1, RadialSegments = 32, Rings = 1, CapTop = false, CapBottom = false };
        _tpRing ??= RingMesh(0.82f, 1f, 48);
        _tpQuad ??= new QuadMesh { Size = Vector2.One };
        var mat = new ShaderMaterial { Shader = _tpShader };
        mat.SetShaderParameter("col", new Vector3(col.R, col.G, col.B));
        float h = (float)(fx.ColumnHM / MPerU) * Math.Max(1, r / (float)(fx.ColumnRM / MPerU) * 0.6f);
        var at = new Vector3((float)(xm / MPerU), 0, (float)(zm / MPerU));
        var column = new MeshInstance3D { Mesh = _tpColumn, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        var ring = new MeshInstance3D { Mesh = _tpRing, MaterialOverride = Additive(col, false), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        var flash = new MeshInstance3D { Mesh = _tpQuad, MaterialOverride = Additive(col, true), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        AddChild(column); AddChild(ring); AddChild(flash);
        column.Position = at + new Vector3(0, -0.2f + h / 2, 0);
        column.Scale = new Vector3(r, h, r);
        ring.Position = at + new Vector3(0, 0.02f, 0);
        flash.Position = at + new Vector3(0, 0.6f, 0);
        _beamFx.Add(new BeamFxNode { Column = column, Ring = ring, Flash = flash, Mat = mat, R = r });
    }

    private void StepBeams(float dt, BeamFxDef fx)
    {
        for (int i = _beamFx.Count - 1; i >= 0; i--)
        {
            BeamFxNode n = _beamFx[i];
            n.T += dt;
            float k = n.T / (float)fx.BeamS;
            n.Mat.SetShaderParameter("t", n.T);
            n.Mat.SetShaderParameter("k", k);
            n.Ring.Scale = Vector3.One * Math.Max(0.01f, n.R * (0.6f + k * 1.6f));
            ((StandardMaterial3D)n.Ring.MaterialOverride).AlbedoColor = new Color(((StandardMaterial3D)n.Ring.MaterialOverride).AlbedoColor, Math.Max(0, 0.8f * (1 - k)));
            n.Flash.Scale = Vector3.One * Math.Max(0.01f, n.R * 3 * Mathf.Sin(Mathf.Pi * Math.Min(1, k)));
            ((StandardMaterial3D)n.Flash.MaterialOverride).AlbedoColor = new Color(((StandardMaterial3D)n.Flash.MaterialOverride).AlbedoColor, Math.Max(0, 0.7f * (1 - k)));
            if (k < 1) continue;
            n.Column.QueueFree(); n.Ring.QueueFree(); n.Flash.QueueFree();
            _beamFx.RemoveAt(i);
        }
    }

    // the stream: motes leave the sender over spread_s, each crossing in travel_s on a low arc with its own wobble,
    // bright in the middle of its flight
    private void Stream(Vector3 a, Vector3 b, Color col, BeamFxDef fx)
    {
        _tpQuad ??= new QuadMesh { Size = Vector2.One };
        float size = (float)(fx.StreamSizeM / MPerU);
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, InstanceCount = fx.StreamCount, Mesh = new QuadMesh { Size = Vector2.One * size } };
        var node = new MultiMeshInstance3D { Multimesh = mm, MaterialOverride = Additive(Colors.White, true), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        node.ExtraCullMargin = 1000;
        AddChild(node);
        for (int i = 0; i < fx.StreamCount; i++) mm.SetInstanceColor(i, new Color(0, 0, 0, 0));
        _streams.Add(new StreamFx { Motes = node, A = a, B = b, Col = col });
    }

    private void StepStreams(float dt, BeamFxDef fx)
    {
        float spread = (float)fx.StreamSpreadS, travel = (float)fx.StreamTravelS, arc = (float)(fx.StreamArcM / MPerU), wob = (float)(fx.StreamWobbleM / MPerU);
        for (int s = _streams.Count - 1; s >= 0; s--)
        {
            StreamFx st = _streams[s];
            st.T += dt;
            Vector3 d = st.B - st.A;
            float len = Math.Max(1e-3f, new Vector2(d.X, d.Z).Length());
            var side = new Vector3(-d.Z / len, 0, d.X / len);
            MultiMesh mm = st.Motes.Multimesh;
            int n = mm.InstanceCount;
            for (int i = 0; i < n; i++)
            {
                float delay = (float)i / n * spread, ph = i * 2.399f, w = 0.4f + i * 0.618f % 1f, u = (st.T - delay) / travel;
                if (u <= 0 || u >= 1) { mm.SetInstanceColor(i, new Color(0, 0, 0, 0)); continue; }
                float sw = Mathf.Sin(Mathf.Pi * u), wobble = Mathf.Sin(u * 9 + ph) * wob * w * sw;
                Vector3 p = st.A + d * u + side * wobble + new Vector3(0, 0.6f + sw * arc, 0);
                mm.SetInstanceTransform(i, new Transform3D(Basis.Identity, p));
                float k = sw * 1.2f;
                mm.SetInstanceColor(i, new Color(st.Col.R * k, st.Col.G * k, st.Col.B * k, Math.Min(1, k)));
            }
            if (st.T <= spread + travel) continue;
            st.Motes.QueueFree();
            _streams.RemoveAt(s);
        }
    }

    // a flat ring on the plane, inner to outer radius
    private static ArrayMesh RingMesh(float r0, float r1, int seg)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < seg; i++)
        {
            float a0 = i * Mathf.Tau / seg, a1 = (i + 1) * Mathf.Tau / seg;
            Vector3 P(float a, float r) => new(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            st.AddVertex(P(a0, r0)); st.AddVertex(P(a0, r1)); st.AddVertex(P(a1, r1));
            st.AddVertex(P(a0, r0)); st.AddVertex(P(a1, r1)); st.AddVertex(P(a1, r0));
        }
        return st.Commit();
    }
}
