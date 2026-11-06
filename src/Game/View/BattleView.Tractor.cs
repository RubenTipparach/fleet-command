// BattleView.Tractor: the tractor-and-craft package in 3D, after the battle console mockup's view (simple geometry, as the
// mockup draws these): a tractor beam is a pale line from holder to held, brighter as the grip tightens; fighters and
// disguised shuttles are small lights (a caught shuttle goes up in a puff; a detonation's fireball comes with its hit);
// a mine is a glow, dim amber while arming and bright once armed; the scene's asteroids are lumpy grey-brown rocks at
// their radius. View only: the simulation never reads it. Sizes are scene units (MPerU: 1 u is 100 m).
using System;
using System.Collections.Generic;
using FleetCommand.Data;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class BattleView
{
    private const float BeamWideU = 0.12f, CraftU = 0.1f, ShuttleU = 0.14f, MineU = 0.16f;
    private static readonly Color TractorCol = new("#9fe8ff"), CraftCol = new("#9fd0ff"), MineArming = new("#806020"), MineArmed = new("#ffb030");
    private Battle? _trBattle;
    private Node3D? _trRoot;
    private readonly List<MeshInstance3D> _trBeams = new(), _trDots = new();
    private readonly Dictionary<Craft, Vector3> _trCraftSeen = new(ReferenceEqualityComparer.Instance);

    partial void SyncTractor(Battle b, double dt)
    {
        if (_trBattle != b) TractorRebuild(b);
        int beams = 0, dots = 0;
        foreach (Ship s in b.Ships)
        {
            if (!s.Alive || s.Beam == null || b.ShipById(s.Beam.On) is not { Alive: true } h) continue;
            MeshInstance3D m = Pooled(_trBeams, ref beams, () => new BoxMesh { Size = new Vector3(BeamWideU, BeamWideU, 1) });
            Vector3 a = U(s.X, s.Z), c = U(h.X, h.Z);
            float len = a.DistanceTo(c);
            if (len < 1e-3f) { m.Visible = false; continue; }
            m.Position = (a + c) / 2;
            m.LookAt(c, Vector3.Up);
            m.Scale = new Vector3(1, 1, len);
            Tint(m, new Color(TractorCol, 0.2f + 0.5f * (float)s.Beam.Grip));
        }
        var now = new HashSet<Craft>(ReferenceEqualityComparer.Instance);
        foreach (Craft c in b.Craft)
        {
            now.Add(c);
            MeshInstance3D m = Pooled(_trDots, ref dots, () => new SphereMesh { Radius = 1, Height = 2, RadialSegments = 8, Rings = 4 });
            m.Position = U(c.X, c.Z) + new Vector3(0, 0.1f, 0);
            m.Scale = Vector3.One * (c.Kind == "shuttle" ? ShuttleU : CraftU);
            Tint(m, CraftCol);
            _trCraftSeen[c] = m.Position;
        }
        foreach (var (c, p) in new List<KeyValuePair<Craft, Vector3>>(_trCraftSeen))
        {
            if (now.Contains(c)) continue;
            if (c.Fate == "caught") Fx.Fireball(this, p, 0.06f, Vector3.Up);
            _trCraftSeen.Remove(c);
        }
        foreach (Mine mn in b.Mines)
        {
            MeshInstance3D m = Pooled(_trDots, ref dots, () => new SphereMesh { Radius = 1, Height = 2, RadialSegments = 8, Rings = 4 });
            m.Position = U(mn.X, mn.Z);
            m.Scale = Vector3.One * MineU;
            Tint(m, mn.ArmS > 0 ? MineArming : MineArmed);
        }
        for (int i = beams; i < _trBeams.Count; i++) _trBeams[i].Visible = false;
        for (int i = dots; i < _trDots.Count; i++) _trDots[i].Visible = false;
    }

    private static Vector3 U(double xm, double zm) => new((float)(xm / MPerU), 0.05f, (float)(zm / MPerU));

    // one pooled piece of a kind: made once, shown again each frame it is used
    private MeshInstance3D Pooled(List<MeshInstance3D> pool, ref int used, Func<Mesh> make)
    {
        if (used >= pool.Count)
        {
            var m = new MeshInstance3D
            {
                Mesh = make(), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                MaterialOverride = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    BlendMode = BaseMaterial3D.BlendModeEnum.Add, NoDepthTest = false,
                },
            };
            _trRoot!.AddChild(m);
            pool.Add(m);
        }
        MeshInstance3D p = pool[used++];
        p.Visible = true;
        return p;
    }

    private static void Tint(MeshInstance3D m, Color c) => ((StandardMaterial3D)m.MaterialOverride).AlbedoColor = c;

    // A new battle: the pools are emptied and the scene's rocks made, each a squashed ball with a few lumps on it,
    // its shape fixed by its place in the list so a restart draws the same rocks.
    private void TractorRebuild(Battle b)
    {
        _trBattle = b;
        _trRoot?.QueueFree();
        _trBeams.Clear(); _trDots.Clear(); _trCraftSeen.Clear();
        _trRoot = new Node3D { Name = "TractorFx" };
        AddChild(_trRoot);
        var rock = new StandardMaterial3D { AlbedoColor = new Color("#7a6f63"), Roughness = 0.95f };
        for (int i = 0; i < b.Rocks.Count; i++)
        {
            RockDef r = b.Rocks[i];
            float rad = (float)(r.RadiusM / MPerU);
            var holder = new Node3D { Position = new Vector3((float)(r.XM / MPerU), 0, (float)(r.ZM / MPerU)), RotationDegrees = new Vector3(i * 23 % 40, i * 67 % 360, i * 31 % 30) };
            var rng = new RandomNumberGenerator { Seed = (ulong)(1000 + i) };
            holder.AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 1, Height = 2, RadialSegments = 12, Rings = 6 }, MaterialOverride = rock, Scale = new Vector3(rad, rad * 0.75f, rad * 0.9f) });
            for (int k = 0; k < 5; k++)
            {
                float a = rng.RandfRange(0, Mathf.Tau), e = rng.RandfRange(-0.6f, 0.6f), s = rad * rng.RandfRange(0.35f, 0.6f);
                var at = new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e) * 0.75f, Mathf.Sin(a) * Mathf.Cos(e)) * rad * 0.7f;
                holder.AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 1, Height = 2, RadialSegments = 8, Rings = 4 }, MaterialOverride = rock, Position = at, Scale = Vector3.One * s });
            }
            _trRoot.AddChild(holder);
        }
    }
}
