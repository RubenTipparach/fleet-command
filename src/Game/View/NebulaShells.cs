// NebulaShells: a battle's nebula drawn as geometry built once when the battle loads (the owner, 2026-11-12: "Nebula
// shells. The cube marched, generate once static mesh"; mockup 40, openspec/changes/nebula-shells; nebula_decks.json style
// "shells", the low-spec mode's). Each column's shape (NebulaPlume.Cover, the volume's own shape, its edge eaten by the
// look's billows) and each deck (DeckField: its face, funnels, edge and colours, the one rule the puffs read too) is a
// density field on a grid, cut by Core's SurfaceNets at each of the shell levels into nested see-through meshes,
// thin and dark outside to dense and bright inside, and drawn by nebula_shell.gdshader. Nothing is evaluated per pixel
// or per frame but the sun. The view only.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Space;
using Godot;

namespace FleetCommand.Game.View;

public sealed class NebulaShells
{
    /// <summary>The shells' place in the draw order: with the plumes (after the planets, before the glows).</summary>
    public const int Priority = -1;

    public readonly Node3D Node;
    private readonly List<ShaderMaterial> _mats = new();
    /// <summary>Triangles in every shell, and the seconds the build took.</summary>
    public int Triangles { get; private set; }
    public double BuildS { get; private set; }

    private static float _nearU;

    private NebulaShells() => Node = new Node3D { Name = "NebulaShells" };

    /// <summary>The shells of a battle's plumes and decks.</summary>
    public static NebulaShells Build(IReadOnlyList<NebulaPlume> plumes, DeckField field, ulong seed)
    {
        _nearU = field.Look.ShellNearKm * field.U;
        ulong t0 = Time.GetTicksUsec();
        var s = new NebulaShells();
        DeckLook k = field.Look;
        for (int i = 0; i < plumes.Count; i++) s.Column(plumes[i], k, seed, i);
        s.Deck(field, seed, +1);
        s.Deck(field, seed, -1);
        s.BuildS = (Time.GetTicksUsec() - t0) / 1e6;
        GD.Print($"NEBULA_SHELLS {plumes.Count} columns and 2 decks, {k.ShellLevels.Length} shells each: {s.Triangles} triangles, built in {s.BuildS:0.00} s");
        return s;
    }

    // a column in its own units (radii), from its shape and its look's billows
    private void Column(NebulaPlume p, DeckLook k, ulong seed, int i)
    {
        float top = p.JoinsAbove ? p.TopR * 1.05f : p.CapUpR * 1.15f, bottom = p.JoinsBelow ? -p.TopR * 1.05f : -p.CapDownR * 1.15f;
        const float half = 2.4f;
        int n = k.ShellGrid, ny = Math.Max(4, (int)Math.Ceiling((top - bottom) / (2 * half / n)));
        var step = (X: 2 * half / n, Y: (top - bottom) / ny, Z: 2 * half / n);
        var billows = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex, Seed = (int)((seed + (ulong)i * 7919) % int.MaxValue),
            Frequency = 0.9f * p.Look.BaseScale, FractalType = FastNoiseLite.FractalTypeEnum.Fbm, FractalOctaves = 3,
        };
        var f = new float[(n + 1) * (ny + 1) * (n + 1)];
        for (int z = 0; z <= n; z++)
            for (int y = 0; y <= ny; y++)
                for (int x = 0; x <= n; x++)
                {
                    var at = new Vector3(-half + x * step.X, bottom + y * step.Y, -half + z * step.Z);
                    float c = NebulaPlume.Cover(at, p.TopR, p.FlareR, p.CapUpR, p.CapDownR, p.Look.Anvil, p.Seed);
                    f[x + (n + 1) * (y + (ny + 1) * z)] = c <= 0 ? 0 : c - p.Look.Erosion * 0.55f * (billows.GetNoise3Dv(at) * 0.5f + 0.5f);
                }
        var holder = new Node3D { Name = "Column" + i, Position = p.Node.Position, Scale = Vector3.One * p.RadiusU };
        Node.AddChild(holder);
        Shells(holder, f, n, ny, n, (-half, bottom, -half), step, _ => p.Look.Lit, p.Look.Glow, k);
    }

    // a deck above (side +1) or below (-1) the battle plane, in scene units
    private void Deck(DeckField fd, ulong seed, float side)
    {
        DeckLook k = fd.Look;
        float u = fd.U;
        FastNoiseLite cover = DeckField.CoverNoise(seed, k, u);
        var detail = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex, Seed = (int)((seed + 31) % int.MaxValue), Frequency = 1f / (k.DetailKm * u),
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm, FractalOctaves = 2,
        };
        float lo = (k.UndersideKm - k.HangKm * 1.3f - k.JoinKm - 1) * u, hi = (k.UndersideKm + k.ThicknessKm + 1) * u;
        int nx = k.ShellGrid * 2, ny = Math.Max(6, k.ShellGrid / 4), nz = nx;
        float span = fd.FadeR * 2;
        var step = (X: span / nx, Y: (hi - lo) / ny, Z: span / nz);
        var origin = (X: fd.Centre.X - fd.FadeR, Y: lo, Z: fd.Centre.Y - fd.FadeR);
        var f = new float[(nx + 1) * (ny + 1) * (nz + 1)];
        float soft = 0.2f * k.ThicknessKm * u;
        for (int z = 0; z <= nz; z++)
            for (int x = 0; x <= nx; x++)
            {
                var q = new Vector2(origin.X + x * step.X, origin.Z + z * step.Z);
                float e = fd.Edge(q);
                float v = (cover.GetNoise2D(q.X, q.Y) + 1) / 2;
                float where = Smooth(1 - k.Coverage - 0.1f, 1 - k.Coverage + 0.1f, v) * e;
                float face = fd.Face(q, side, v), back = face + k.ThicknessKm * u;
                for (int y = 0; y <= ny; y++)
                {
                    float h = origin.Y + y * step.Y;
                    float slab = Math.Clamp(Math.Min(h - face, back - h) / soft, 0, 1);
                    float d = slab * where;
                    f[x + (nx + 1) * (y + (ny + 1) * z)] = d <= 0 ? 0 : d - k.Erosion * 0.5f * (detail.GetNoise3D(q.X, h * side, q.Y) * 0.5f + 0.5f);
                }
            }
        // the deck below is built in heights off the plane and turned over
        var holder = new Node3D { Name = side > 0 ? "DeckAbove" : "DeckBelow", Scale = new Vector3(1, side, 1) };
        Node.AddChild(holder);
        Shells(holder, f, nx, ny, nz, origin, step, at => fd.Colors(new Vector2(at.X, at.Z)).Lit, fd.Colors(fd.Centre).Glow, k);
    }

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3 - 2 * t);
    }

    // every shell level of one field, thin to dense, each a mesh under holder coloured by lit at its vertices
    private void Shells(Node3D holder, float[] field, int nx, int ny, int nz, (float X, float Y, float Z) origin, (float X, float Y, float Z) step,
        Func<Vector3, Color> lit, Color glow, DeckLook k)
    {
        float[] levels = k.ShellLevels;
        for (int li = 0; li < levels.Length; li++)
        {
            NetMesh m = SurfaceNets.Build(field, nx, ny, nz, levels[li], origin, step);
            if (m.Triangles == 0) continue;
            int nv = m.Positions.Length / 3;
            var pos = new Vector3[nv];
            var nrm = new Vector3[nv];
            var col = new Color[nv];
            float share = (li + 1f) / levels.Length;
            for (int v = 0; v < nv; v++)
            {
                pos[v] = new Vector3(m.Positions[v * 3], m.Positions[v * 3 + 1], m.Positions[v * 3 + 2]);
                nrm[v] = new Vector3(m.Normals[v * 3], m.Normals[v * 3 + 1], m.Normals[v * 3 + 2]);
                Color c = lit(holder.Position + pos[v] * holder.Scale);
                col[v] = new Color(c.R, c.G, c.B, 0.6f + 0.8f * share);
            }
            var arr = new Godot.Collections.Array();
            arr.Resize((int)Mesh.ArrayType.Max);
            arr[(int)Mesh.ArrayType.Vertex] = pos;
            arr[(int)Mesh.ArrayType.Normal] = nrm;
            arr[(int)Mesh.ArrayType.Color] = col;
            arr[(int)Mesh.ArrayType.Index] = m.Indices;
            var mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arr);
            var mat = new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/View/nebula_shell.gdshader"), RenderPriority = Priority };
            mat.SetShaderParameter("alpha", k.ShellAlpha);
            mat.SetShaderParameter("soft", k.ShellSoft);
            mat.SetShaderParameter("glow", glow);
            mat.SetShaderParameter("near_fade", _nearU);
            _mats.Add(mat);
            holder.AddChild(new MeshInstance3D
            {
                Name = "Shell" + li, Mesh = mesh, MaterialOverride = mat,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, ExtraCullMargin = 4000,
            });
            Triangles += m.Triangles;
        }
    }

    /// <summary>The battle's sun.</summary>
    public void Sync(Vector3 sunDir, float sunEnergy)
    {
        foreach (ShaderMaterial m in _mats)
        {
            m.SetShaderParameter("sun_dir", sunDir);
            m.SetShaderParameter("sun_energy", sunEnergy);
        }
    }
}
