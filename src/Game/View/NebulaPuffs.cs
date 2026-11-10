// NebulaPuffs: a battle's nebula as puffs, for the two styles that are not marched (owner, 2026-11-10: "either the deck
// would have to be clouds too or everything is geometry. lets try both", "third technique to try out: crab-breezes cloud
// system", "cheap 3d billboards with lighting"; data/space/nebula_decks.json style). The one placement of the puffs for
// both styles: each plume's column filled with puffs inside its own shape (NebulaPlume.Cover, the volume's shape), and
// the two decks laid as a field of puffs over the same ground as the marched decks (NebulaDeck's numbers: the face, its
// hang, the funnel round each column that reaches it, whole to a margin past the outermost column and thinning to its
// edge, clear round every planet), every puff's colour set when it is placed from the columns round it (the looks of the
// columns placed there, varied in broad patches). The styles differ only in how a puff is drawn: a camera-facing quad
// (nebula_billboard.gdshader, Crab Breeze's) or a displaced ball (nebula_mesh.gdshader). One MultiMesh, sorted far to
// near on the CPU when the camera has moved, one draw. The view only.
//
// While the owner compares the styles, the deck's colour rule is written twice: here for a puff, and in
// nebula_deck.gdshader for the marched deck (deck_colors). Once a style is chosen the other goes, and the copy with it.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed class NebulaPuffs
{
    /// <summary>One puff: its middle and radius (scene units), its colours in the light and in its own shadow, how much of
    /// it shows (its deck's edge), and which deck it lies in (+1 above, -1 below, 0 a column's).</summary>
    public readonly record struct Puff(Vector3 At, float R, Color Lit, Color Deep, float Alpha, float Side);

    /// <summary>The puffs' place in the draw order: with the plumes (after the planets, before the glows).</summary>
    public const int Priority = -1;
    /// <summary>The camera moves this far (scene units) before the puffs are sorted again.</summary>
    public const float ResortU = 1.5f;

    public readonly MultiMeshInstance3D Node;
    private readonly ShaderMaterial _mat;
    private readonly Puff[] _puffs;
    private readonly float[] _buffer;
    private Vector3 _sortedAt = new(float.NaN, 0, 0);

    private NebulaPuffs(MultiMeshInstance3D node, ShaderMaterial mat, Puff[] puffs)
    {
        Node = node;
        _mat = mat;
        _puffs = puffs;
        _buffer = new float[puffs.Length * 20];
    }

    /// <summary>The puffs of a battle's plumes and decks, drawn as billboards or as meshes (DeckLook.Style).</summary>
    public static NebulaPuffs Build(IReadOnlyList<NebulaPlume> plumes, IEnumerable<(double X, double Z, double R)> planets, ulong seed, float mPerU, bool billboards)
    {
        Puff[] puffs = Place(plumes, planets, seed, mPerU).ToArray();
        DeckLook k = DeckLook.Get();
        var mat = new ShaderMaterial
        {
            Shader = GD.Load<Shader>(billboards ? "res://src/Game/View/nebula_billboard.gdshader" : "res://src/Game/View/nebula_mesh.gdshader"),
            RenderPriority = Priority,
        };
        mat.SetShaderParameter("noise_tex", NebulaPlume.Noise());
        mat.SetShaderParameter("underside", k.UndersideKm * 1000f / mPerU);
        var mm = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, UseCustomData = true,
            Mesh = billboards ? new QuadMesh { Size = new Vector2(2, 2) } : new SphereMesh { Radius = 1, Height = 2, RadialSegments = 40, Rings = 20 },
            InstanceCount = puffs.Length,
        };
        var node = new MultiMeshInstance3D
        {
            Name = billboards ? "PuffBillboards" : "PuffMeshes", Multimesh = mm, MaterialOverride = mat,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, ExtraCullMargin = 4000,
        };
        return new NebulaPuffs(node, mat, puffs);
    }

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3 - 2 * t);
    }

    // Every puff: the columns', then the decks'.
    private static IEnumerable<Puff> Place(IReadOnlyList<NebulaPlume> plumes, IEnumerable<(double X, double Z, double R)> planetsM, ulong seed, float mPerU)
    {
        DeckLook k = DeckLook.Get();
        float u = 1000f / mPerU;
        // each column: puffs inside its own shape, most in its outer shell (the inside is hidden), from its own fork
        for (int i = 0; i < plumes.Count; i++)
        {
            NebulaPlume p = plumes[i];
            Rng r = Rng.Fork(seed, "puffs-column-" + i);
            float top = p.JoinsAbove ? p.FlareR * 1.15f : p.CapUpR * 1.05f, bottom = p.JoinsBelow ? -p.FlareR * 1.15f : -p.CapDownR * 1.05f;
            int n = Math.Clamp((int)Math.Round((top - bottom) * 22 + 12), 14, 160);
            for (int tries = 0, made = 0; made < n && tries < n * 40; tries++)
            {
                var at = new Vector3((float)(r.Next() * 2 - 1) * 2.2f, bottom + (float)r.Next() * (top - bottom), (float)(r.Next() * 2 - 1) * 2.2f);
                float c = NebulaPlume.Cover(at, p.TopR, p.FlareR, p.CapUpR, p.CapDownR, p.Look.Anvil, p.Seed);
                if (c < 0.35f || (c > 0.9f && r.Next() > 0.25)) continue;
                float rad = Math.Clamp(Mathf.Lerp(0.3f, 0.55f, (float)r.Next()) * p.RadiusU, 1.2f * u, 10f * u);
                yield return new Puff(p.World(at), rad, p.Look.Lit, p.Look.Deep, 1, 0);
                made++;
            }
        }

        // the decks: a jittered field of puffs, where the marched deck's cloud would be
        NebulaPlume[] cols = plumes.OrderByDescending(p => p.Def.RM).Take(NebulaDeck.Columns).ToArray();
        var centre = new Vector2((float)cols.Average(p => p.Def.XM), (float)cols.Average(p => p.Def.ZM)) / mPerU;
        float fieldR = cols.Max(p => new Vector2((float)p.Def.XM, (float)p.Def.ZM).DistanceTo(centre * mPerU) + (float)p.Def.RM) / mPerU + k.MarginKm * u;
        float fadeR = fieldR + k.FadeKm * u, step = k.DeckPuffKm * u;
        (Vector2 At, float Clear)[] planets = planetsM.Select(q => (new Vector2((float)(q.X / mPerU), (float)(q.Z / mPerU)), (float)(q.R / mPerU) * k.PlanetClearX)).ToArray();
        var field = new FastNoiseLite { NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex, Seed = (int)(seed % int.MaxValue), Frequency = 1f / (k.BaseKm * u) };
        var tint = new FastNoiseLite { NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex, Seed = (int)(seed % int.MaxValue) + 7, Frequency = 0.18f / (k.BaseKm * u) };
        Rng rd = Rng.Fork(seed, "puffs-decks");
        int cells = (int)Math.Ceiling(fadeR / step);
        for (int gz = -cells; gz <= cells; gz++)
            for (int gx = -cells; gx <= cells; gx++)
            {
                var q = centre + new Vector2(gx + (float)rd.Next() - 0.5f, gz + (float)rd.Next() - 0.5f) * step;
                float e = 1 - Smooth(fieldR, fadeR, q.DistanceTo(centre));
                foreach ((Vector2 at, float clear) in planets) e *= Smooth(0.75f, 1f, q.DistanceTo(at) / clear);
                float v = (field.GetNoise2D(q.X, q.Y) + 1) / 2;
                if (e <= 0.02f || v < 1 - k.Coverage) continue;
                (Color lit, Color deep) = DeckColor(cols, q, k, u, tint.GetNoise2D(q.X, q.Y), tint.GetNoise2D(q.Y + 913, q.X));
                for (int s = 0; s < 2; s++)
                {
                    float side = s == 0 ? 1 : -1;
                    float pull = 0;
                    foreach (NebulaPlume c in cols)
                        if (side > 0 ? c.JoinsAbove : c.JoinsBelow)
                        {
                            float dd = Math.Max(q.DistanceTo(new Vector2((float)(c.Def.XM / mPerU), (float)(c.Def.ZM / mPerU))) - c.RadiusU, 0) / (k.FunnelKm * u);
                            pull = Math.Max(pull, Mathf.Exp(-dd * dd * 2.5f));
                        }
                    float face = (k.UndersideKm - k.HangKm * (2 * v - 0.7f)) * u - k.JoinKm * u * pull;
                    // one big puff at the face and one deeper behind it
                    for (int layer = 0; layer < 2; layer++)
                    {
                        float y = face + k.ThicknessKm * u * (layer == 0 ? 0.12f + 0.12f * (float)rd.Next() : 0.4f + 0.2f * (float)rd.Next());
                        float rad = step * Mathf.Lerp(0.7f, 1.1f, (float)rd.Next()) * (layer == 0 ? 0.85f : 1.2f) * (0.6f + 0.4f * e);
                        yield return new Puff(new Vector3(q.X, side * y, q.Y), rad, lit, deep, e, side);
                    }
                }
            }
    }

    // A deck puff's colour at q (the plane, scene units): each column's look over its funnel and round it, their mean far
    // from any, varied in broad patches toward the glow, toward the other looks, lighter and darker (nebula_deck.gdshader's
    // deck_colors and its variety, for a puff).
    private static (Color Lit, Color Deep) DeckColor(NebulaPlume[] cols, Vector2 q, DeckLook k, float u, float t1, float t2)
    {
        float wsum = 0.05f;
        Color mean = new(0, 0, 0), meanDeep = new(0, 0, 0), lit = new(0, 0, 0), deep = new(0, 0, 0), glow = new(0, 0, 0);
        foreach (NebulaPlume c in cols) { mean += c.Look.Lit / cols.Length; meanDeep += c.Look.Deep / cols.Length; }
        lit += mean * wsum; deep += meanDeep * wsum;
        Color meanGlow = new(0, 0, 0);
        foreach (NebulaPlume c in cols) meanGlow += c.Look.Glow / cols.Length;
        glow += meanGlow * wsum;
        foreach (NebulaPlume c in cols)
        {
            float d = Math.Max(q.DistanceTo(new Vector2(c.Node.Position.X, c.Node.Position.Z)) - c.RadiusU, 0) / (c.RadiusU * 1.5f + k.FunnelKm * u);
            float w = Mathf.Exp(-d * d * 2) * 20 + Mathf.Exp(-d * 0.7f);
            lit += c.Look.Lit * w; deep += c.Look.Deep * w; glow += c.Look.Glow * w;
            wsum += w;
        }
        lit /= wsum; deep /= wsum; glow /= wsum;
        float a = (t1 + 1) / 2, b = (t2 + 1) / 2;
        lit = lit.Lerp(glow, k.Variety * 0.55f * Smooth(0.3f, 0.8f, a)).Lerp(mean, k.Variety * 0.5f * Smooth(0.35f, 0.85f, b));
        float shade = Mathf.Lerp(1 - k.Variety * 0.45f, 1 + k.Variety * 0.3f, Smooth(0.2f, 0.8f, (a + b) / 2));
        return (new Color(lit.R * shade, lit.G * shade, lit.B * shade), new Color(deep.R * shade, deep.G * shade, deep.B * shade));
    }

    /// <summary>The sun and the battle's clock; the puffs sorted far to near again once the camera has moved.</summary>
    public void Sync(Vector3 sunDir, float sunEnergy, double battleS, Vector3 cam)
    {
        _mat.SetShaderParameter("sun_dir", sunDir);
        _mat.SetShaderParameter("sun_energy", sunEnergy);
        _mat.SetShaderParameter("puff_time", (float)battleS);
        if (!float.IsNaN(_sortedAt.X) && _sortedAt.DistanceTo(cam) < ResortU) return;
        _sortedAt = cam;
        int[] order = Enumerable.Range(0, _puffs.Length).OrderByDescending(i => _puffs[i].At.DistanceSquaredTo(cam)).ToArray();
        for (int j = 0; j < order.Length; j++)
        {
            Puff p = _puffs[order[j]];
            // (the buffer is the shader's own numbers: linear light, as ALBEDO wants it)
            Color lit = p.Lit.SrgbToLinear(), deep = p.Deep.SrgbToLinear();
            int o = j * 20;
            // a 3 x 4 transform by rows (basis x, y, z then origin per row), the colour, the custom data
            _buffer[o] = p.R; _buffer[o + 1] = 0; _buffer[o + 2] = 0; _buffer[o + 3] = p.At.X;
            _buffer[o + 4] = 0; _buffer[o + 5] = p.R; _buffer[o + 6] = 0; _buffer[o + 7] = p.At.Y;
            _buffer[o + 8] = 0; _buffer[o + 9] = 0; _buffer[o + 10] = p.R; _buffer[o + 11] = p.At.Z;
            _buffer[o + 12] = lit.R; _buffer[o + 13] = lit.G; _buffer[o + 14] = lit.B; _buffer[o + 15] = p.Alpha;
            _buffer[o + 16] = deep.R; _buffer[o + 17] = deep.G; _buffer[o + 18] = deep.B; _buffer[o + 19] = p.Side;
        }
        Node.Multimesh.Buffer = _buffer;
    }
}
