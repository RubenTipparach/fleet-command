// NebulaPlume: the one builder of a nebula plume in 3D (openspec/changes/nebula-plumes; owner, 2026-11-10: "something like
// a 3d noise texture would be cool? something like how Ace Combat 7 does clouds, entering a column of nebula plume can
// hide you from enemies"). A plume is one mesh, a bounding column scaled to the plume's radius and standing on its place
// on the battle plane, drawn by nebula_plume.gdshader, which marches the cloud (the column's shape, Cover, baked into a
// small 3D texture of its own) through the shared 3D noise
// (assets/space/plume_noise.bin, written by tools/space/plume_noise.py) in the plume's look (data/space/plumes.json,
// PlumeLooks). The mesh's unit is the plume's radius: the circle the rules test (Rules.NebulaAt) is radius 1 on the plane,
// so the cloud the player sees is the cloud the rules use. Sync sets the battle's sun, the battle's clock (the cloud holds
// still while the battle is paused) and an ion storm's lightning flashes each frame. Planets draw before a plume and every
// glow after it (render priority): PlanetBody's layers sit below Priority, everything else at 0. The view only.
using System;
using FleetCommand.Data;
using Godot;

namespace FleetCommand.Game.View;

public sealed class NebulaPlume
{
    /// <summary>How far a plume hangs below the battle plane, and the bounding column's radius, in the plume's radii (the
    /// shader's bottom and bound): the widest head (anvil 0.8, lobes, lean and billows) stays inside it.</summary>
    public const float Bottom = -0.45f, Bound = 2.1f;
    /// <summary>A plume's place in the draw order: after every planet layer (PlanetBody.Priority and below), before the
    /// glows, beams and shields at 0.</summary>
    public const int Priority = -1;
    /// <summary>The lightning flashes a plume can show at once (the shader's FLASHES).</summary>
    public const int Flashes = 4;
    /// <summary>The noise's size a side, in voxels of four bytes (tools/space/plume_noise.py SIZE).</summary>
    public const int NoiseSize = 64;
    /// <summary>The baked shape's size in voxels, across (x and z, over the bounding column's width) and up (bottom to
    /// top): about a fifteenth of a radius a voxel, a third of the shape's softest edge.</summary>
    public const int CoverXZ = 64, CoverY = 48;
    /// <summary>Half the band the billows stand in, either side of the column's edge, radii.</summary>
    public const float Edge = 0.2f;
    private const string NoiseFile = "res://assets/space/plume_noise.bin";

    private static Shader? _shader;
    private static ImageTexture3D? _noise;

    public readonly MeshInstance3D Node;
    public readonly NebulaDef Def;
    public readonly PlumeLook Look;
    /// <summary>The plume's radius in the scene's units (the mesh's scale).</summary>
    public readonly float RadiusU;
    private readonly ShaderMaterial _mat;

    private NebulaPlume(NebulaDef def, PlumeLook look, Vector3 seed, float mPerU)
    {
        Def = def;
        Look = look;
        RadiusU = (float)(def.RM / mPerU);
        _shader ??= GD.Load<Shader>("res://src/Game/View/nebula_plume.gdshader");
        _mat = new ShaderMaterial { Shader = _shader, RenderPriority = Priority };
        _mat.SetShaderParameter("noise_tex", Noise());
        _mat.SetShaderParameter("color_lit", look.Lit);
        _mat.SetShaderParameter("color_deep", look.Deep);
        _mat.SetShaderParameter("color_glow", look.Glow);
        _mat.SetShaderParameter("glow_strength", look.GlowStrength);
        _mat.SetShaderParameter("density", (float)def.Density);
        _mat.SetShaderParameter("top", look.HeightR);
        _mat.SetShaderParameter("bottom", Bottom);
        _mat.SetShaderParameter("bound", Bound);
        _mat.SetShaderParameter("cover_tex", CoverTexture(look.HeightR, look.Anvil, seed));
        _mat.SetShaderParameter("absorption", look.Absorption);
        _mat.SetShaderParameter("base_scale", look.BaseScale);
        _mat.SetShaderParameter("detail_scale", look.DetailScale);
        _mat.SetShaderParameter("erosion", look.Erosion);
        _mat.SetShaderParameter("drift", look.DriftPerS);
        _mat.SetShaderParameter("clear", Math.Max(look.ClearKm * 1000f / mPerU / RadiusU, 1e-3f));
        _mat.SetShaderParameter("seed", seed);
        _mat.SetShaderParameter("flash_color", look.Flash);
        _mat.SetShaderParameter("flash", new Color[Flashes]);
        // the column the shader marches: a little wider than Bound (a 32-sided column's flats sit inside its radius), as
        // far below the plane as above, so the mesh needs no offset; the shader stops at Bottom itself
        float r = Bound * 1.02f;
        Node = new MeshInstance3D
        {
            Name = "Plume", MaterialOverride = _mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Mesh = new CylinderMesh { TopRadius = r, BottomRadius = r, Height = 2 * look.HeightR, RadialSegments = 32, Rings = 1 },
            Position = new Vector3((float)(def.XM / mPerU), 0, (float)(def.ZM / mPerU)), Scale = Vector3.One * RadiusU,
        };
    }

    /// <summary>A scenario's plume in its look, with its own seed (three numbers 0 to 1: where in the noise it is cut from,
    /// which way it leans), at <paramref name="mPerU"/> metres to the scene's unit.</summary>
    public static NebulaPlume Build(NebulaDef def, Vector3 seed, float mPerU) => new(def, PlumeLooks.For(def.Look), seed, mPerU);

    /// <summary>The sun (the direction toward it, world space, and its strength), the battle's clock in seconds, and the
    /// lightning inside the plume now: up to <see cref="Flashes"/> flashes, each a place in the plume's own units (x, y, z in
    /// radii from its foot) with its brightness in the alpha.</summary>
    public void Sync(Vector3 sunDir, float sunEnergy, double battleS, Color[] flashes)
    {
        _mat.SetShaderParameter("sun_dir", sunDir);
        _mat.SetShaderParameter("sun_energy", sunEnergy);
        _mat.SetShaderParameter("plume_time", (float)battleS);
        _mat.SetShaderParameter("flash", flashes);
    }

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3 - 2 * t);
    }

    // A slow field over the plane, 0 to 1, cut from the seed: where the column's towers stand and where its foot hangs.
    private static float Lobes(float ax, float ay, Vector3 ph) =>
        0.5f + 0.25f * (Mathf.Sin(ax * 2.1f + ph.X + 1.3f * Mathf.Sin(ay * 1.7f + ph.Y)) + Mathf.Sin(ay * 2.6f + ph.Z + 1.1f * Mathf.Sin(ax * 1.9f + ph.X * 2f)));

    /// <summary>The column's shape, the one implementation of it: how far inside the column a point is (the plume's own
    /// units, radii from its foot), 0 outside, 1 in the heart, soft between over half a radius. On the plane (y = 0) the
    /// cloud's mean edge is the circle of radius 1 (the rule's), but for the lobes; the column leans as it climbs, spreads
    /// toward its head by <paramref name="anvil"/>, and ends in towers of different heights (the tallest reach
    /// <paramref name="top"/>) that round over to nothing, as its foot hangs to different depths: never cut flat.</summary>
    public static float Cover(Vector3 p, float top, float anvil, Vector3 seed)
    {
        if (p.Y <= Bottom || p.Y >= top) return 0;
        Vector3 ph = seed * Mathf.Tau;
        float u = Math.Max(p.Y, 0) / top;
        float lean = 0.06f * Math.Clamp(Math.Abs(p.Y) * 2, 0f, 1f) + 0.07f * Math.Max(p.Y, 0);
        float x = p.X + lean * Mathf.Sin(p.Y * 1.3f + ph.X), z = p.Z + lean * Mathf.Cos(p.Y * 1.1f + ph.Y);
        float len = Mathf.Sqrt(x * x + z * z);
        float r = p.Y < 0 ? Mathf.Lerp(0.5f, 1f, Smooth(Bottom, 0, p.Y)) : 1 - 0.14f * Mathf.Sin(Mathf.Pi * Math.Clamp(u / 0.55f, 0f, 1f));
        r += anvil * 0.5f * Smooth(0.4f, 0.8f, u);
        float ang = Mathf.Atan2(z, x);
        r *= 1 + 0.08f * Mathf.Sin(3 * ang + ph.Z + p.Y * 1.5f) + 0.04f * Mathf.Sin(7 * ang - ph.X - p.Y * 2.7f);
        float side = Smooth(-Edge, Edge * 1.5f, r - len);
        if (side <= 0) return 0;
        float towers = Smooth(0.15f, 0.85f, Lobes(x * 1.9f, z * 1.9f, ph));
        float head = (top - Edge) * Mathf.Lerp(0.45f, 1f, towers) * Mathf.Sqrt(Math.Max(1 - 0.42f * len * len, 0));
        float foot = Bottom * Mathf.Lerp(0.3f, 1f, Lobes(x * 2.3f, z * 2.3f, new Vector3(ph.Z, ph.X, ph.Y)));
        return side * Smooth(-Edge, Edge * 1.5f, head - p.Y) * Smooth(0, 0.3f, p.Y - foot);
    }

    // The shape over the bounding box (x and z from -Bound to Bound, y from Bottom to top) as a 3D texture of one byte a
    // voxel, a z slice an image, sampled at voxel centres; the box's outer voxels are clear, so a sample clamped past the
    // box reads none.
    private static ImageTexture3D CoverTexture(float top, float anvil, Vector3 seed)
    {
        int n = CoverXZ, h = CoverY;
        var slices = new Godot.Collections.Array<Image>();
        for (int k = 0; k < n; k++)
        {
            byte[] px = new byte[n * h];
            if (k > 0 && k < n - 1)
                for (int j = 1; j < h - 1; j++)
                    for (int i = 1; i < n - 1; i++)
                    {
                        var p = new Vector3(((i + 0.5f) / n * 2 - 1) * Bound, Bottom + (j + 0.5f) / h * (top - Bottom), ((k + 0.5f) / n * 2 - 1) * Bound);
                        px[j * n + i] = (byte)Mathf.RoundToInt(Cover(p, top, anvil, seed) * 255);
                    }
            slices.Add(Image.CreateFromData(n, h, false, Image.Format.R8, px));
        }
        var t = new ImageTexture3D();
        t.Create(Image.Format.R8, n, h, n, false, slices);
        return t;
    }

    /// <summary>A point of the scene in the plume's own units (radii from its foot on the plane).</summary>
    public Vector3 Local(Vector3 world) => (world - Node.Position) / RadiusU;

    /// <summary>A point of the plume (radii from its foot) in the scene.</summary>
    public Vector3 World(Vector3 local) => Node.Position + local * RadiusU;

    /// <summary>The shared noise as a 3D texture, one RGBA image a z slice, loaded once. A file of the wrong size is an
    /// error naming it (the generator and this must agree: NoiseSize).</summary>
    public static ImageTexture3D Noise()
    {
        if (_noise != null) return _noise;
        byte[] bytes = FileAccess.GetFileAsBytes(NoiseFile);
        int n = NoiseSize, slice = n * n * 4;
        if (bytes.Length != slice * n)
            throw new InvalidOperationException($"{NoiseFile}: {bytes.Length} bytes, not {n} x {n} x {n} voxels of four (run tools/space/plume_noise.py)");
        var slices = new Godot.Collections.Array<Image>();
        for (int z = 0; z < n; z++) slices.Add(Image.CreateFromData(n, n, false, Image.Format.Rgba8, bytes[(z * slice)..((z + 1) * slice)]));
        var t = new ImageTexture3D();
        t.Create(Image.Format.Rgba8, n, n, n, false, slices);
        return _noise = t;
    }
}
