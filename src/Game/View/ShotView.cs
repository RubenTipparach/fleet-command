// ShotView: every shot of the battle drawn in 3D, the game's version of the battle console mockup's shotFx (v77) and the
// weapons range (mockup 16; change weapon-fx, approved for implementation 2026-11-05: "Implement weapons in game"). A beam
// (lance) is a lit strip and a point defence beam a stream of cannon tracers, both from the mount on the firing ship to
// their target as they stand this frame (Battle.Ends, the rule the simulation flies bolts by; owner, 2026-11-04: "the
// source and destination of these beams must follow the ship source and ship destination"); a disruptor bolt is a hot
// streak on that live line; a pulse torpedo flies under its six-armed star and drops a trail of beads; a missile is a
// small body with a ball of drive light behind it and a long tail in its grade's colour (a seeker flies free and leaves its
// trail in space); flares burn where they were thrown and a decoy shuttle flies off under its pulsing beacon. The special
// weapons: the chorus lance with its strands and the lances that join it, the quell (a star, a ball and beads in deep
// blue), the forge slug (a hot streak on the live line) and a blood charge's bow and drive.
// Colours are each weapon's own (data/battle/weapons.json), never white. View only: the simulation never reads it.
// Sizes are scene units (BattleView.MPerU: 1 u is 1 km, the mockup's 100 m tenfold), the mockup's SHOT_FX.
using System;
using System.Collections.Generic;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class ShotView : Node3D
{
    /// <summary>The looks' sizes, scene units and seconds (the battle console mockup's SHOT_FX, v77).</summary>
    public const float BeamWU = 0.34f, PdWU = 0.2f, BoltWU = 0.45f, BoltLenU = 1.0f, TorpedoU = 2.2f, MissileU = 0.7f, MissileLenU = 0.22f,
        TrailWU = 0.45f, TrailS = 2.4f, TrailEveryS = 1 / 30f, BeadU = 1.4f, BeadEveryS = 0.012f, BeadLifeS = 0.4f, MissOffsetU = 0.6f, FlareU = 0.6f, DecoyU = 0.5f, DecoyLenU = 0.14f,
        ChorusWU = 0.55f, JoinWU = 0.2f, SlugWU = 0.6f, SlugLenU = 1.3f, QuellU = 1.8f, ChargeBowU = 1.3f, ChargeDriveU = 1.6f;
    private const float Y = 0.05f;
    private const int BeadN = 48, TrailN = 80;

    private static Shader? _beamSh, _flareSh, _beadSh;
    private static ArrayMesh? _strip;
    private readonly Dictionary<string, List<MeshInstance3D>> _pool = new();
    private readonly Dictionary<string, int> _used = new();
    private readonly Dictionary<Shot, Trail> _trails = new(ReferenceEqualityComparer.Instance);

    /// <summary>A seeker's trail: beads for a torpedo, a ribbon tail for a missile; it outlives its shot until it fades.</summary>
    private sealed class Trail
    {
        public Node3D Node = null!;
        public MultiMesh? Beads;
        public ImmediateMesh? Tail;
        public readonly List<(Vector3 P, float T)> Points = new();
        public int Next;
        public float Last = -99, LastSeen;
        public Vector3 LastP;
        public Color Color;
        public bool Missile;
    }

    private static Shader Load(ref Shader? s, string name) => s ??= GD.Load<Shader>($"res://src/Game/View/{name}.gdshader");

    // the strip a beam is laid on: 2 x 33 vertices, UV.x across, UV.y along
    private static ArrayMesh Strip()
    {
        if (_strip != null) return _strip;
        const int n = 32;
        var verts = new Vector3[(n + 1) * 2];
        var uvs = new Vector2[(n + 1) * 2];
        var idx = new List<int>();
        for (int i = 0; i <= n; i++)
        {
            verts[i * 2] = Vector3.Zero; verts[i * 2 + 1] = Vector3.Zero;
            uvs[i * 2] = new Vector2(0, (float)i / n); uvs[i * 2 + 1] = new Vector2(1, (float)i / n);
            if (i < n) { int a = i * 2; idx.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 }); }
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Mesh.ArrayType.Index] = idx.ToArray();
        _strip = new ArrayMesh();
        _strip.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        _strip.CustomAabb = new Aabb(new Vector3(-1e5f, -1e5f, -1e5f), new Vector3(2e5f, 2e5f, 2e5f));   // laid out in the shader
        return _strip;
    }

    // one pooled piece of a kind: "beam" (a strip) or "flare" (a camera-facing quad); a slot never changes kind
    private MeshInstance3D Take(string kind)
    {
        if (!_pool.TryGetValue(kind, out List<MeshInstance3D>? list)) _pool[kind] = list = new List<MeshInstance3D>();
        int i = _used.GetValueOrDefault(kind);
        _used[kind] = i + 1;
        if (i < list.Count) { list[i].Visible = true; return list[i]; }
        MeshInstance3D m = kind == "beam"
            ? new MeshInstance3D { Mesh = Strip(), MaterialOverride = new ShaderMaterial { Shader = Load(ref _beamSh, "beam") } }
            : new MeshInstance3D { Mesh = new QuadMesh { Size = Vector2.One }, MaterialOverride = new ShaderMaterial { Shader = Load(ref _flareSh, "flare") } };
        m.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        m.ExtraCullMargin = 16384;
        AddChild(m);
        list.Add(m);
        return m;
    }

    // a decoy shuttle's body: a small blunt box in gunmetal, pooled like the rest
    private MeshInstance3D TakeBody()
    {
        if (!_pool.TryGetValue("decoy", out List<MeshInstance3D>? list)) _pool["decoy"] = list = new List<MeshInstance3D>();
        int i = _used.GetValueOrDefault("decoy");
        _used["decoy"] = i + 1;
        if (i < list.Count) { list[i].Visible = true; return list[i]; }
        var m = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(DecoyLenU * 0.45f, DecoyLenU * 0.3f, DecoyLenU) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#6a6f78"), Metallic = 0.6f, Roughness = 0.45f },
        };
        AddChild(m);
        list.Add(m);
        return m;
    }

    // a shot in flight where it is drawn this frame, between its last two steps (Battle.DrawnAt)
    private static Vector3 Drawn(Shot sh, double share) { var (x, z) = Battle.DrawnAt(sh, share); return At(x, z); }

    private static Vector3 At(double xM, double zM) => new((float)(xM / BattleView.MPerU), Y, (float)(zM / BattleView.MPerU));

    /// <summary>Draws every shot as it stands this frame.</summary>
    public void Sync(Battle b, double share = 1)
    {
        float now = (float)b.T;
        _used.Clear();
        var live = new HashSet<Shot>(ReferenceEqualityComparer.Instance);
        foreach (Shot sh in b.Shots)
        {
            var col = new Color(sh.Color);
            if (sh.Kind == "beam")
            {
                var (sx, sz, tx, tz) = b.Ends(sh);
                Vector3 a = At(sx, sz), e = At(tx, tz);
                if (!sh.Hit && !sh.Intercept && sh.To != null) { Vector3 d = (e - a).Normalized(); e += new Vector3(-d.Z, 0, d.X) * MissOffsetU; }
                Beam(a, e, sh.Intercept ? PdWU : BeamWU, sh.Intercept ? 2 : 0, col, Math.Clamp((float)sh.Life * 6, 0, 1), now);
            }
            else if (sh.Kind == "chorus")
            {
                // the chorus lance: its beam from the deflector to the target, and each joining lance into it, a third of the way
                var (sx, sz, tx, tz) = b.Ends(sh);
                Vector3 a = At(sx, sz), e = At(tx, tz), focus = a.Lerp(e, 0.35f);
                int n = sh.Joined?.Count ?? 0;
                float k = Math.Clamp((float)sh.Life / 0.35f, 0, 1);
                Beam(a, e, ChorusWU * (1 + 0.22f * n), 3, col, k, now, new Color(b.Data.Specials.Chorus.StrandColor), 2 + n);
                foreach (var (id, mi) in sh.Joined ?? new())
                    if (b.ShipById(id) is { Alive: true } o && mi < o.Mounts.Count) { var (jx, jz) = Battle.MountWorld(o, o.Mounts[mi]); Beam(At(jx, jz), focus, JoinWU, 0, new Color(o.Mounts[mi].Weapon.Color), k, now); }
            }
            else if (sh.Kind is "bolt" or "slug")
            {
                var (sx, sz, tx, tz) = b.Ends(sh);
                Vector3 s0 = At(sx, sz), t0 = At(tx, tz), head = Drawn(sh, share);
                bool slug = sh.Kind == "slug";
                float len = Math.Max(1e-4f, s0.DistanceTo(t0)), k = Math.Min(1, (slug ? SlugLenU : BoltLenU) / len);
                Beam(head - (t0 - s0) * k, head, slug ? SlugWU : BoltWU, 1, col, slug ? 1.8f : 1.4f, now);
            }
            else
            {
                // torpedoes and the quell (half torpedo, half missile) fly under the six-armed star and drop beads
                bool torpedo = sh.Kind is "torpedo" or "quell";
                Vector3 p = Drawn(sh, share);
                Flare(p, sh.Kind == "quell" ? QuellU : torpedo ? TorpedoU : MissileU, torpedo ? 1 : 2, col, torpedo ? 1.1f : 1.5f, now);
                if (sh.Kind == "quell") Flare(p, QuellU * 0.45f, 2, col, 0.9f, now);   // and the missile's ball of drive light
                if (!_trails.TryGetValue(sh, out Trail? tr)) _trails[sh] = tr = NewTrail(!torpedo, col);
                Push(tr, p, now);
                live.Add(sh);
            }
        }
        // a blood charge: the bayonet glows blood red at the bow and the drive burns forge orange astern
        foreach (Ship s in b.Ships)
        {
            if (!s.Alive || s.SurgeS <= 0) continue;
            var f = Geometry.Forward(s.H);
            double half = s.Class.FrameM / 2;
            float k = (float)Math.Clamp(s.SurgeS / b.Data.Specials.Charge.SurgeS, 0, 1);
            Flare(At(s.X + f.X * half, s.Z + f.Z * half), ChargeBowU, 0, new Color(b.Data.Specials.Charge.Color), 1.4f + 0.6f * k, now);
            Flare(At(s.X - f.X * half, s.Z - f.Z * half), ChargeDriveU, 2, new Color("#ff8a2a"), 1.2f + 0.8f * k, now);
        }
        // countermeasures: burning flares, and a decoy shuttle under its pulsing beacon (Battle.Lures)
        foreach (Lure l in b.Lures)
        {
            Vector3 p = At(l.X, l.Z);
            if (l.Kind == "flare")
            {
                float k = (float)Math.Clamp(l.LifeS / b.Data.Missiles.Flare.BurnS, 0, 1);
                Flare(p, FlareU, 0, new Color(b.Data.Missiles.Flare.Color), 1.6f * k * (0.8f + 0.4f * Mathf.Sin(now * 40 + l.X.GetHashCode() % 7)), now);
            }
            else
            {
                Flare(p, DecoyU, 0, new Color(b.Data.Missiles.Decoy.Color), 0.6f + 1.4f * Math.Max(0, Mathf.Sin(now * 9)), now);
                MeshInstance3D body = TakeBody();
                body.Position = p;
                body.Rotation = new Vector3(0, -Mathf.Atan2((float)l.VX, (float)-l.VZ), 0);
            }
        }
        // what a slot did not draw this frame is hidden
        foreach (var (kind, list) in _pool)
            for (int i = _used.GetValueOrDefault(kind); i < list.Count; i++) list[i].Visible = false;
        // a trail outlives its shot until it has faded where it flew
        var gone = new List<Shot>();
        foreach (var (sh, tr) in _trails)
        {
            if (!live.Contains(sh) && now - tr.LastSeen > Math.Max(TrailS, BeadLifeS) + 0.1f) { tr.Node.QueueFree(); gone.Add(sh); continue; }
            if (tr.Missile) { DrawTail(tr, now); if (!live.Contains(sh) && tr.Node.GetNodeOrNull<MeshInstance3D>("Body") is { } body) body.Visible = false; }
            else ((ShaderMaterial)((MultiMeshInstance3D)tr.Node).MaterialOverride).SetShaderParameter("time_s", now);
        }
        foreach (Shot sh in gone) _trails.Remove(sh);
    }

    private void Beam(Vector3 a, Vector3 b, float width, int mode, Color col, float intensity, float now, Color? col2 = null, int strands = 2)
    {
        var m = (ShaderMaterial)Take("beam").MaterialOverride;
        if (col2 is { } c2) m.SetShaderParameter("color2", c2);
        m.SetShaderParameter("strands", (float)strands);
        m.SetShaderParameter("point_a", a); m.SetShaderParameter("point_b", b); m.SetShaderParameter("width", width);
        m.SetShaderParameter("mode", mode); m.SetShaderParameter("color", col); m.SetShaderParameter("intensity", intensity);
        m.SetShaderParameter("time_s", now);
    }

    private void Flare(Vector3 p, float size, int mode, Color col, float intensity, float now)
    {
        MeshInstance3D f = Take("flare");
        f.Position = p;
        var m = (ShaderMaterial)f.MaterialOverride;
        m.SetShaderParameter("size", size); m.SetShaderParameter("mode", mode); m.SetShaderParameter("color", col);
        m.SetShaderParameter("intensity", intensity); m.SetShaderParameter("time_s", now);
    }

    private Trail NewTrail(bool missile, Color col)
    {
        var tr = new Trail { Missile = missile, Color = col };
        if (missile)
        {
            tr.Tail = new ImmediateMesh();
            var mi = new MeshInstance3D
            {
                Mesh = tr.Tail, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, ExtraCullMargin = 16384,
                MaterialOverride = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, VertexColorUseAsAlbedo = true, BlendMode = BaseMaterial3D.BlendModeEnum.Add,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha, CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                    DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
                },
            };
            // the missile's body, at the front of its ball of drive light
            var body = new MeshInstance3D
            {
                Name = "Body", Mesh = new CylinderMesh { TopRadius = MissileLenU * 0.05f, BottomRadius = MissileLenU * 0.07f, Height = MissileLenU, RadialSegments = 8 },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#5a5f66"), Metallic = 0.7f, Roughness = 0.4f },
            };
            mi.AddChild(body);
            tr.Node = mi;
        }
        else
        {
            var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseCustomData = true, Mesh = new QuadMesh { Size = Vector2.One }, InstanceCount = BeadN };
            for (int i = 0; i < BeadN; i++) { mm.SetInstanceTransform(i, Transform3D.Identity); mm.SetInstanceCustomData(i, new Color(-10, 0, 0, 0)); }
            var mat = new ShaderMaterial { Shader = Load(ref _beadSh, "beads") };
            mat.SetShaderParameter("color", col); mat.SetShaderParameter("size", BeadU); mat.SetShaderParameter("life_s", BeadLifeS);
            tr.Beads = mm;
            tr.Node = new MultiMeshInstance3D { Multimesh = mm, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, ExtraCullMargin = 16384 };
        }
        AddChild(tr.Node);
        return tr;
    }

    // a seeker's position this frame: a missile's tail keeps its points; a torpedo drops beads every BeadEveryS of
    // flight, filled in along the line between frames so the spacing holds however far it moved
    private static void Push(Trail tr, Vector3 p, float now)
    {
        tr.LastSeen = now;
        if (tr.Missile)
        {
            // a point every TrailEveryS of battle time, the newest moved along with the missile in between, so the tail's length
            // is TrailS of flight at any frame rate or speed of play (owner, 2026-11-06: "your missiles don't have much of a tail")
            if (tr.Points.Count < 2 || now - tr.Points[^2].T >= TrailEveryS) tr.Points.Add((p, now));
            else tr.Points[^1] = (p, now);
            while (tr.Points.Count > TrailN || (tr.Points.Count > 2 && now - tr.Points[0].T > TrailS)) tr.Points.RemoveAt(0);
            if (tr.Node.GetNodeOrNull<MeshInstance3D>("Body") is { } body && tr.Points.Count >= 2)
            {
                Vector3 d = p - tr.Points[^2].P;
                Vector3 fwd = d.LengthSquared() > 1e-10f ? d.Normalized() : Vector3.Forward;
                body.Visible = true;
                body.Position = p + fwd * MissileLenU * 0.6f;   // at the front of its ball of drive light
                body.LookAt(body.Position + fwd, Mathf.Abs(fwd.Y) > 0.99f ? Vector3.Right : Vector3.Up);
                body.RotateObjectLocal(Vector3.Right, -Mathf.Pi / 2);   // the cylinder's axis along the flight
            }
            return;
        }
        if (tr.Last < -50) { tr.Last = now; tr.LastP = p; return; }
        int n = Math.Min(BeadN, (int)Math.Floor((now - tr.Last) / BeadEveryS));
        if (n < 1) return;
        Vector3 q = p;
        for (int i = 1; i <= n; i++)
        {
            float k = i * BeadEveryS / (now - tr.Last);
            q = tr.LastP.Lerp(p, k);
            tr.Beads!.SetInstanceTransform(tr.Next, new Transform3D(Basis.Identity, q));
            tr.Beads.SetInstanceCustomData(tr.Next, new Color(tr.Last + i * BeadEveryS, 0, 0, 0));
            tr.Next = (tr.Next + 1) % BeadN;
        }
        tr.Last += n * BeadEveryS;
        tr.LastP = q;
    }

    // a missile's tail: a camera-facing strip of its last points, narrowing and fading with age
    private void DrawTail(Trail tr, float now)
    {
        ImmediateMesh m = tr.Tail!;
        m.ClearSurfaces();
        if (tr.Points.Count < 2) return;
        Camera3D? cam = GetViewport()?.GetCamera3D();
        Vector3 eye = cam?.GlobalPosition ?? Vector3.Up * 10;
        m.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip);
        for (int i = tr.Points.Count - 1; i >= 0; i--)
        {
            var (p, t) = tr.Points[i];
            Vector3 nb = tr.Points[Math.Max(0, i - 1)].P, pb = tr.Points[Math.Min(tr.Points.Count - 1, i + 1)].P;
            Vector3 dir = (pb - nb); if (dir.LengthSquared() < 1e-10f) dir = Vector3.Forward;
            Vector3 side = dir.Normalized().Cross((eye - p).Normalized()).Normalized();
            float a = Math.Clamp(1 - (now - t) / TrailS, 0, 1), w = TrailWU * (0.15f + 0.85f * a) * 0.5f;
            var c = new Color(tr.Color.R * a * a, tr.Color.G * a * a, tr.Color.B * a * a, 1);
            m.SurfaceSetColor(c); m.SurfaceAddVertex(p - side * w);
            m.SurfaceSetColor(c); m.SurfaceAddVertex(p + side * w);
        }
        m.SurfaceEnd();
    }
}
