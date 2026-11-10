// NebulaPlume: the one builder of a nebula plume in 3D (openspec/changes/nebula-plumes; owner, 2026-11-10: "something like
// a 3d noise texture would be cool? something like how Ace Combat 7 does clouds, entering a column of nebula plume can
// hide you from enemies", "The clouds need to stretch up in columns of varying widths across the map"). A plume is one mesh,
// a bounding column scaled to the plume's radius and standing on its place on the battle plane, reaching from the deck of
// cloud below the battle into the deck above (NebulaDeck; their numbers are DeckLook's), drawn by nebula_plume.gdshader, which marches the cloud (the column's shape, Cover, baked into a
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
    /// <summary>The bounding column's radius, in the plume's radii (the shader's bound): the widest flare (anvil 0.8, lobes,
    /// wander and billows) stays inside it.</summary>
    public const float Bound = 2.9f;
    /// <summary>A plume's place in the draw order: after every planet layer (PlanetBody.Priority and below), before the
    /// glows, beams and shields at 0.</summary>
    public const int Priority = -1;
    /// <summary>The lightning flashes a plume can show at once (the shader's FLASHES).</summary>
    public const int Flashes = 4;
    /// <summary>The noise's size a side, in voxels of four bytes (tools/space/plume_noise.py SIZE).</summary>
    public const int NoiseSize = 64;
    /// <summary>The baked shape's size in voxels, across (x and z, over the bounding column's width) and up (foot to top):
    /// about a tenth of a radius a voxel across, a fifth of the shape's softest edge.</summary>
    public const int CoverXZ = 64, CoverY = 64;
    /// <summary>The baked shadow's size in voxels a side (ShadowTexture): about a fifth of a radius a voxel across, finer
    /// than the shadow of anything but a wisp, which the shader's one near step through the noise still gives.</summary>
    public const int ShadowN = 32;
    /// <summary>The most cloud the baked shadow holds toward the sun, radii of full-density cloud (its texture's 1); more
    /// is as dark (the shader's absorption makes 2 radii of it about one fiftieth of the sun).</summary>
    public const float ShadowMaxR = 2f;
    /// <summary>The billows' mean share of the shape's density (the noise cuts the shape about in half), for the baked
    /// shadow, which knows the shape and not the noise.</summary>
    private const float ShadowMean = 0.5f;
    /// <summary>The baked shadow's step toward the sun and how far it looks, radii; it starts past the shader's near step
    /// (nebula_plume.gdshader LIGHT_REACH), which covers the first stretch with the noise.</summary>
    private const float ShadowStep = 0.08f, ShadowReach = 3.5f, ShadowStart = 0.1f;

    /// <summary>Half the band the billows stand in, either side of the column's edge, radii.</summary>
    public const float Edge = 0.2f;
    private const string NoiseFile = "res://assets/space/plume_noise.bin";

    private static Shader? _shader;
    private static ImageTexture3D? _noise;

    public readonly MeshInstance3D Node;
    public readonly NebulaDef Def;
    /// <summary>Where in the noise the plume is cut from and which way it leans, three numbers 0 to 1.</summary>
    public readonly Vector3 Seed;
    public readonly PlumeLook Look;
    /// <summary>The plume's radius in the scene's units (the mesh's scale).</summary>
    public readonly float RadiusU;
    /// <summary>How far the column reaches above the plane (and below it), and where it flares into each deck, in its radii
    /// (DeckLook.ColumnFlareKm): a narrow column stands many of its radii tall, a wide one few. It is gone a little past the
    /// flare, where the deck's funnel has come down to meet it: drawn over the deck, any of it inside would show.</summary>
    public readonly float TopR, FlareR;
    /// <summary>Where the column's head ends above the plane, and its foot below it, in its radii, for a form that does not
    /// reach that deck (NebulaDef.Forms); 0 where it runs into the deck.</summary>
    public readonly float CapUpR, CapDownR;
    /// <summary>How high the column's cloud stands above the plane, radii: its head, or its flare into the deck above.</summary>
    public float HeadR => CapUpR > 0 ? CapUpR : FlareR;
    /// <summary>Whether the column runs into the deck above, and into the deck below (where a deck reaches down to meet it:
    /// NebulaDeck).</summary>
    public bool JoinsAbove => CapUpR <= 0;
    public bool JoinsBelow => CapDownR <= 0;
    private readonly ShaderMaterial _mat;

    private NebulaPlume(NebulaDef def, PlumeLook look, DeckLook deck, Vector3 seed, float mPerU, Vector3 toSun)
    {
        Def = def;
        Look = look;
        Seed = seed;
        RadiusU = (float)(def.RM / mPerU);
        FlareR = deck.ColumnFlareKm * 1000 / (float)def.RM;
        TopR = Math.Max(FlareR * 1.3f, FlareR + 0.15f);
        // a form short of a deck ends in a head (or a foot) a share of the way there, by the plume's own seed; a puff is a
        // radius or so either way, never reaching the flare
        float v = Frac(seed.X * 7.31f + seed.Z * 2.17f), w = Frac(seed.Y * 5.77f + seed.X * 3.41f);
        (CapUpR, CapDownR) = def.Form switch
        {
            "rising" => (FlareR * Mathf.Lerp(0.4f, 0.75f, v), 0f),
            "hanging" => (0f, FlareR * Mathf.Lerp(0.3f, 0.6f, v)),
            "puff" => (Math.Clamp(Mathf.Lerp(0.55f, 1.15f, v), 0.35f, FlareR * 0.7f), Math.Clamp(Mathf.Lerp(0.35f, 0.8f, w), 0.3f, FlareR * 0.6f)),
            _ => (0f, 0f),
        };
        _shader ??= GD.Load<Shader>("res://src/Game/View/nebula_plume.gdshader");
        _mat = new ShaderMaterial { Shader = _shader, RenderPriority = Priority };
        _mat.SetShaderParameter("noise_tex", Noise());
        _mat.SetShaderParameter("color_lit", look.Lit);
        _mat.SetShaderParameter("color_deep", look.Deep);
        _mat.SetShaderParameter("color_glow", look.Glow);
        _mat.SetShaderParameter("glow_strength", look.GlowStrength);
        _mat.SetShaderParameter("density", (float)def.Density);
        _mat.SetShaderParameter("top", TopR);
        _mat.SetShaderParameter("bottom", -TopR);
        _mat.SetShaderParameter("flare", FlareR);
        _mat.SetShaderParameter("deck_fill", deck.Fill);
        _mat.SetShaderParameter("bound", Bound);
        (ImageTexture3D cover, ImageTexture3D shadow) = CoverTexture(TopR, FlareR, CapUpR, CapDownR, look.Anvil, seed, toSun.Normalized(), (float)def.Density);
        _mat.SetShaderParameter("cover_tex", cover);
        _mat.SetShaderParameter("shadow_tex", shadow);
        _mat.SetShaderParameter("shadow_max", ShadowMaxR);
        _mat.SetShaderParameter("absorption", look.Absorption);
        _mat.SetShaderParameter("contrast", look.Contrast);
        _mat.SetShaderParameter("base_scale", look.BaseScale);
        _mat.SetShaderParameter("detail_scale", look.DetailScale);
        _mat.SetShaderParameter("erosion", look.Erosion);
        _mat.SetShaderParameter("drift", look.DriftPerS);
        _mat.SetShaderParameter("clear", Math.Max(look.ClearKm * 1000f / mPerU / RadiusU, 1e-3f));
        _mat.SetShaderParameter("seed", seed);
        _mat.SetShaderParameter("flash_color", look.Flash);
        _mat.SetShaderParameter("flash", new Vector4[Flashes]);
        // the column the shader marches: a little wider than Bound (a 32-sided column's flats sit inside its radius), as
        // far below the plane as above
        float r = Bound * 1.02f;
        Node = new MeshInstance3D
        {
            Name = "Plume", MaterialOverride = _mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Mesh = new CylinderMesh { TopRadius = r, BottomRadius = r, Height = 2 * TopR, RadialSegments = 32, Rings = 1 },
            Position = new Vector3((float)(def.XM / mPerU), 0, (float)(def.ZM / mPerU)), Scale = Vector3.One * RadiusU,
        };
    }

    /// <summary>A scenario's plume in its look, with its own seed (three numbers 0 to 1: where in the noise it is cut from,
    /// which way it leans), at <paramref name="mPerU"/> metres to the scene's unit, its shadow baked for the battle's sun
    /// (<paramref name="toSun"/>, world space: the sun holds still through a battle).</summary>
    public static NebulaPlume Build(NebulaDef def, Vector3 seed, float mPerU, Vector3 toSun) => new(def, PlumeLooks.For(def.Look), DeckLook.Get(), seed, mPerU, toSun);

    /// <summary>The sun (the direction toward it, world space, and its strength), the battle's clock in seconds, and the
    /// lightning inside the plume now: up to <see cref="Flashes"/> flashes, each a place in the plume's own units (x, y, z in
    /// radii from its foot) with its brightness in the alpha.</summary>
    public void Sync(Vector3 sunDir, float sunEnergy, double battleS, Vector4[] flashes)
    {
        _mat.SetShaderParameter("sun_dir", sunDir);
        _mat.SetShaderParameter("sun_energy", sunEnergy);
        _mat.SetShaderParameter("plume_time", (float)battleS);
        _mat.SetShaderParameter("flash", flashes);
    }

    /// <summary>The battle's fog (its colour, and its density a scene unit), laid over the cloud at its own depth.</summary>
    public void SetFog(Color color, float densityPerU)
    {
        _mat.SetShaderParameter("fog_color", color);
        _mat.SetShaderParameter("fog_density", densityPerU);
    }

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3 - 2 * t);
    }

    private static float Frac(float x) => x - Mathf.Floor(x);

    // A slow field over the plane, 0 to 1, cut from the seed: where a head's towers stand.
    private static float Lobes(float ax, float ay, Vector3 ph) =>
        0.5f + 0.25f * (Mathf.Sin(ax * 2.1f + ph.X + 1.3f * Mathf.Sin(ay * 1.7f + ph.Y)) + Mathf.Sin(ay * 2.6f + ph.Z + 1.1f * Mathf.Sin(ax * 1.9f + ph.X * 2f)));

    /// <summary>The column's shape, the one implementation of it: how far inside the column a point is (the plume's own
    /// units, radii from its foot), 0 outside, 1 in the heart, soft between over half a radius. On the plane (y = 0) the
    /// cloud's mean edge is the circle of radius 1 (the rule's), but for the lobes. Above and below it the column wanders,
    /// bulges and narrows at its waist as it climbs, each by its own amount drawn from the seed (owner, 2026-11-10: "we also
    /// dont want all the columns to have the same shape"). An end that reaches a deck (cap 0) flares by
    /// <paramref name="anvil"/> of its radius from <paramref name="flare"/> on (less in a column that flares within a radius
    /// of the plane, so a wide one never spreads into a table) and thins away by <paramref name="top"/>, where the deck's
    /// funnel has come down to meet it (NebulaDeck); an end short of its deck (<paramref name="capUp"/>,
    /// <paramref name="capDown"/>, radii) rounds over into towers of different heights.</summary>
    public static float Cover(Vector3 p, float top, float flare, float capUp, float capDown, float anvil, Vector3 seed)
    {
        float ay = Math.Abs(p.Y);
        float cap = p.Y >= 0 ? capUp : capDown;
        if (ay >= top || (cap > 0 && ay >= cap * 1.1f)) return 0;
        Vector3 ph = seed * Mathf.Tau;
        // this column's own proportions
        float wander = Mathf.Lerp(0.06f, 0.32f, Frac(seed.X * 3.7f + seed.Y * 1.3f));
        float waist = Mathf.Lerp(0.0f, 0.28f, Frac(seed.Y * 4.1f + seed.Z * 2.9f));
        float bulge = Mathf.Lerp(0.03f, 0.16f, Frac(seed.Z * 5.3f + seed.X * 1.7f));
        float lobe = Mathf.Lerp(0.04f, 0.16f, Frac(seed.X * 6.1f + seed.Z * 3.3f));
        int k1 = 2 + (int)(Frac(seed.Y * 8.3f) * 3), k2 = 5 + (int)(Frac(seed.Z * 9.7f) * 4);
        // none of the wander or bulge on the plane, where the column is the rule's circle
        float rise = Smooth(0, 1.5f, ay);
        float x = p.X + rise * wander * (Mathf.Sin(p.Y * 0.42f + ph.X) + 0.4f * Mathf.Sin(p.Y * 1.3f + ph.Y));
        float z = p.Z + rise * wander * (Mathf.Cos(p.Y * 0.37f + ph.Y) + 0.4f * Mathf.Cos(p.Y * 1.1f + ph.Z));
        float len = Mathf.Sqrt(x * x + z * z);
        float h = ay / (cap > 0 ? cap : flare);
        float r = 1 + rise * (-waist * Mathf.Sin(Mathf.Pi * Math.Clamp(h, 0f, 1f)) + bulge * Mathf.Sin(p.Y * 0.9f + ph.Z));
        if (cap <= 0)
        {
            float spread = anvil * Math.Min(1f, flare);
            r += spread * Smooth(0.6f, 1f, h) + spread * 0.5f * Smooth(flare, top, ay);
        }
        float ang = Mathf.Atan2(z, x);
        r *= 1 + lobe * Mathf.Sin(k1 * ang + ph.Z + p.Y * 1.5f) + lobe * 0.5f * Mathf.Sin(k2 * ang - ph.X - p.Y * 2.7f);
        float side = Smooth(-Edge, Edge * 1.5f, r - len);
        if (side <= 0) return 0;
        if (cap <= 0) return side * (1 - Smooth(flare * 0.85f, top, ay));
        // a head (or a foot) of towers of different heights that round over
        float towers = Smooth(0.15f, 0.85f, Lobes(x * 1.9f, z * 1.9f, p.Y >= 0 ? ph : new Vector3(ph.Z, ph.X, ph.Y)));
        float head = cap * Mathf.Lerp(0.55f, 1f, towers) * Mathf.Sqrt(Math.Max(1 - 0.42f * len * len, 0));
        return side * Smooth(-Edge, Edge * 1.5f, head - ay);
    }

    // The shape over the bounding box (x and z from -Bound to Bound, y from -top to top) as a 3D texture of one byte a
    // voxel, a z slice an image, sampled at voxel centres; the box's outer voxels are clear, so a sample clamped past the
    // box reads none. And the shadow (ShadowTexture), from the same shape.
    private static (ImageTexture3D Cover, ImageTexture3D Shadow) CoverTexture(float top, float flare, float capUp, float capDown, float anvil, Vector3 seed, Vector3 toSun, float density)
    {
        int n = CoverXZ, h = CoverY;
        float[] cover = new float[n * h * n];
        var slices = new Godot.Collections.Array<Image>();
        for (int k = 0; k < n; k++)
        {
            byte[] px = new byte[n * h];
            if (k > 0 && k < n - 1)
                for (int j = 1; j < h - 1; j++)
                    for (int i = 1; i < n - 1; i++)
                    {
                        var p = new Vector3(((i + 0.5f) / n * 2 - 1) * Bound, -top + (j + 0.5f) / h * 2 * top, ((k + 0.5f) / n * 2 - 1) * Bound);
                        byte b = (byte)Mathf.RoundToInt(Cover(p, top, flare, capUp, capDown, anvil, seed) * 255);
                        px[j * n + i] = b;
                        cover[(k * h + j) * n + i] = b / 255f;
                    }
            slices.Add(Image.CreateFromData(n, h, false, Image.Format.R8, px));
        }
        var t = new ImageTexture3D();
        t.Create(Image.Format.R8, n, h, n, false, slices);
        return (t, ShadowTexture(cover, top, toSun, density));
    }

    // The column's own shadow over the same box, ShadowN voxels a side: from each voxel's middle, the cloud of the shape
    // toward the sun (the baked cover, read between its voxels, times the billows' mean share and the plume's density),
    // in radii of full-density cloud over ShadowMaxR. The sun holds still through a battle, so this is the light march
    // toward it done once, and the shader reads it in one fetch (owner, 2026-11-10: "do 1 first").
    private static ImageTexture3D ShadowTexture(float[] cover, float top, Vector3 toSun, float density)
    {
        int n = CoverXZ, h = CoverY, m = ShadowN;
        float Sample(Vector3 p)
        {
            // trilinear between the cover's voxel centres; nothing outside the box
            float fx = (p.X / (2 * Bound) + 0.5f) * n - 0.5f, fy = ((p.Y + top) / (2 * top)) * h - 0.5f, fz = (p.Z / (2 * Bound) + 0.5f) * n - 0.5f;
            if (fx < 0 || fy < 0 || fz < 0 || fx >= n - 1 || fy >= h - 1 || fz >= n - 1) return 0;
            int x0 = (int)fx, y0 = (int)fy, z0 = (int)fz;
            float ax = fx - x0, ay = fy - y0, az = fz - z0;
            float V(int x, int y, int z) => cover[(z * h + y) * n + x];
            float c00 = Mathf.Lerp(V(x0, y0, z0), V(x0 + 1, y0, z0), ax), c10 = Mathf.Lerp(V(x0, y0 + 1, z0), V(x0 + 1, y0 + 1, z0), ax);
            float c01 = Mathf.Lerp(V(x0, y0, z0 + 1), V(x0 + 1, y0, z0 + 1), ax), c11 = Mathf.Lerp(V(x0, y0 + 1, z0 + 1), V(x0 + 1, y0 + 1, z0 + 1), ax);
            return Mathf.Lerp(Mathf.Lerp(c00, c10, ay), Mathf.Lerp(c01, c11, ay), az);
        }
        var slices = new Godot.Collections.Array<Image>();
        for (int k = 0; k < m; k++)
        {
            byte[] px = new byte[m * m];
            for (int j = 0; j < m; j++)
                for (int i = 0; i < m; i++)
                {
                    var p = new Vector3(((i + 0.5f) / m * 2 - 1) * Bound, -top + (j + 0.5f) / m * 2 * top, ((k + 0.5f) / m * 2 - 1) * Bound);
                    float depth = 0;
                    for (float d = ShadowStart; d < ShadowReach; d += ShadowStep)
                    {
                        Vector3 q = p + toSun * d;
                        if (Math.Abs(q.X) > Bound || Math.Abs(q.Z) > Bound || Math.Abs(q.Y) > top) break;
                        depth += Sample(q) * ShadowStep;
                    }
                    px[j * m + i] = (byte)Math.Clamp(Mathf.RoundToInt(depth * ShadowMean * density / ShadowMaxR * 255), 0, 255);
                }
            slices.Add(Image.CreateFromData(m, m, false, Image.Format.R8, px));
        }
        var t = new ImageTexture3D();
        t.Create(Image.Format.R8, m, m, m, false, slices);
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
