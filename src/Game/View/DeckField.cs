// DeckField: what the decks of a battle's nebula are at each point of the battle plane, the one implementation of it
// (CLAUDE.md 6.1), set once when the columns are placed (owner, 2026-11-10: "color the nebula decks with vertex colors
// too", "which can be done uppon column placement"): how far the deck's face reaches down toward the plane round the root
// of each column that runs into that deck (the funnel), how whole the deck is (its edge thinning away past the outermost
// column, clear round every planet), and its colours (each column's look over its funnel and round it, their mean far from
// any, varied in broad patches toward the glow, toward the other looks, lighter and darker). The marched decks bake it into
// small textures over the field (NebulaDeck: a texture read in place of a loop over every column at every sample); the
// puffs read it where each puff is placed (NebulaPuffs). The view only.
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FleetCommand.Game.View;

public sealed class DeckField
{
    /// <summary>The columns a deck knows of: the widest first.</summary>
    public const int Columns = 16;

    public readonly DeckLook Look;
    /// <summary>The field's middle on the plane, and how far from it the deck is whole and where it is gone (scene units).</summary>
    public readonly Vector2 Centre;
    public readonly float FieldR, FadeR;
    /// <summary>Scene units a km.</summary>
    public readonly float U;
    private readonly NebulaPlume[] _cols;
    private readonly (Vector2 At, float Clear)[] _planets;
    private readonly FastNoiseLite _tintA, _tintB;
    private readonly Color _meanLit, _meanDeep, _meanGlow;

    /// <summary>The field over a battle's plumes and planets (x, z and radius, metres), at <paramref name="mPerU"/> metres to
    /// the scene's unit; seed picks its patches of colour.</summary>
    public DeckField(IReadOnlyList<NebulaPlume> plumes, IEnumerable<(double X, double Z, double R)> planets, ulong seed, float mPerU)
    {
        Look = DeckLook.Get();
        U = 1000f / mPerU;
        _cols = plumes.OrderByDescending(p => p.Def.RM).Take(Columns).ToArray();
        Centre = new Vector2((float)_cols.Average(p => p.Def.XM), (float)_cols.Average(p => p.Def.ZM)) / mPerU;
        FieldR = _cols.Max(p => new Vector2(p.Node.Position.X, p.Node.Position.Z).DistanceTo(Centre) + p.RadiusU) + Look.MarginKm * U;
        FadeR = FieldR + Look.FadeKm * U;
        _planets = planets.Select(q => (new Vector2((float)(q.X / mPerU), (float)(q.Z / mPerU)), (float)(q.R / mPerU) * Look.PlanetClearX)).ToArray();
        int s = (int)(seed % int.MaxValue);
        _tintA = new FastNoiseLite { NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex, Seed = s + 7, Frequency = 0.18f / (Look.BaseKm * U) };
        _tintB = new FastNoiseLite { NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex, Seed = s + 13, Frequency = 0.18f / (Look.BaseKm * U) };
        foreach (NebulaPlume c in _cols)
        {
            _meanLit += c.Look.Lit / _cols.Length;
            _meanDeep += c.Look.Deep / _cols.Length;
            _meanGlow += c.Look.Glow / _cols.Length;
        }
    }

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3 - 2 * t);
    }

    /// <summary>How far the deck's face reaches down at q (scene units on the plane), 0 to 1 of join_km, in the deck above
    /// (side +1) or below (-1): round the root of each column that runs into that deck.</summary>
    public float Pull(Vector2 q, float side)
    {
        float pull = 0;
        foreach (NebulaPlume c in _cols)
            if (side > 0 ? c.JoinsAbove : c.JoinsBelow)
            {
                float d = Math.Max(q.DistanceTo(new Vector2(c.Node.Position.X, c.Node.Position.Z)) - c.RadiusU, 0) / (Look.FunnelKm * U);
                pull = Math.Max(pull, Mathf.Exp(-d * d * 2.5f));
            }
        return pull;
    }

    /// <summary>The height of a deck's face toward the battle at q (scene units off the plane), in the deck above (side +1)
    /// or below (-1), where its billows (<paramref name="v"/>, 0 to 1, the deck's cover noise there) hang it lower and a
    /// column's funnel pulls it down: the one rule for the puffs and the shells.</summary>
    public float Face(Vector2 q, float side, float v) =>
        (Look.UndersideKm - Look.HangKm * (2 * v - 0.7f)) * U - Look.JoinKm * U * Pull(q, side);

    /// <summary>The deck's cover noise (the billows it hangs in and where it is cloud), 0 to 1 at q: the one noise for the
    /// puffs and the shells.</summary>
    public static FastNoiseLite CoverNoise(ulong seed, DeckLook k, float u) =>
        new() { NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex, Seed = (int)(seed % int.MaxValue), Frequency = 1f / (k.BaseKm * u) };

    /// <summary>How whole the deck is at q, 0 to 1: thinning away past the field, clear round every planet.</summary>
    public float Edge(Vector2 q)
    {
        float e = 1 - Smooth(FieldR, FadeR, q.DistanceTo(Centre));
        foreach ((Vector2 at, float clear) in _planets) e *= Smooth(0.75f, 1f, q.DistanceTo(at) / clear);
        return e;
    }

    /// <summary>The deck's colours at q: in its light, in its own shadow, and its glow.</summary>
    public (Color Lit, Color Deep, Color Glow) Colors(Vector2 q)
    {
        float wsum = 0.05f;
        Color lit = _meanLit * wsum, deep = _meanDeep * wsum, glow = _meanGlow * wsum;
        foreach (NebulaPlume c in _cols)
        {
            float d = Math.Max(q.DistanceTo(new Vector2(c.Node.Position.X, c.Node.Position.Z)) - c.RadiusU, 0) / (c.RadiusU * 1.5f + Look.FunnelKm * U);
            float w = Mathf.Exp(-d * d * 2) * 20 + Mathf.Exp(-d * 0.7f);
            lit += c.Look.Lit * w; deep += c.Look.Deep * w; glow += c.Look.Glow * w;
            wsum += w;
        }
        lit /= wsum; deep /= wsum; glow /= wsum;
        float a = (_tintA.GetNoise2D(q.X, q.Y) + 1) / 2, b = (_tintB.GetNoise2D(q.X, q.Y) + 1) / 2, v = Look.Variety;
        lit = lit.Lerp(glow, v * 0.55f * Smooth(0.3f, 0.8f, a)).Lerp(_meanLit, v * 0.5f * Smooth(0.35f, 0.85f, b));
        float shade = Mathf.Lerp(1 - v * 0.45f, 1 + v * 0.3f, Smooth(0.2f, 0.8f, (a + b) / 2));
        return (Scale(lit, shade), Scale(deep, shade), glow);
    }

    private static Color Scale(Color c, float k) => new(Math.Min(c.R * k, 1), Math.Min(c.G * k, 1), Math.Min(c.B * k, 1));

    /// <summary>The field baked over the deck's square (Centre plus or minus FadeR), <paramref name="size"/> texels a side:
    /// the face's reach down and the deck's wholeness (red and green) for the deck above (side +1) or below, and the three
    /// colours (sRGB).</summary>
    public (ImageTexture Shape, ImageTexture Lit, ImageTexture Deep, ImageTexture Glow) Bake(float side, int size)
    {
        byte[] shape = new byte[size * size * 2], lit = new byte[size * size * 3], deep = new byte[size * size * 3], glow = new byte[size * size * 3];
        static byte B(float x) => (byte)Math.Clamp(Mathf.RoundToInt(x * 255), 0, 255);
        for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                var q = Centre + new Vector2(((i + 0.5f) / size * 2 - 1) * FadeR, ((j + 0.5f) / size * 2 - 1) * FadeR);
                int o = j * size + i;
                shape[o * 2] = B(Pull(q, side));
                shape[o * 2 + 1] = B(Edge(q));
                (Color l, Color d, Color g) = Colors(q);
                lit[o * 3] = B(l.R); lit[o * 3 + 1] = B(l.G); lit[o * 3 + 2] = B(l.B);
                deep[o * 3] = B(d.R); deep[o * 3 + 1] = B(d.G); deep[o * 3 + 2] = B(d.B);
                glow[o * 3] = B(g.R); glow[o * 3 + 1] = B(g.G); glow[o * 3 + 2] = B(g.B);
            }
        ImageTexture T(byte[] px, Image.Format f) => ImageTexture.CreateFromImage(Image.CreateFromData(size, size, false, f, px));
        return (T(shape, Image.Format.Rg8), T(lit, Image.Format.Rgb8), T(deep, Image.Format.Rgb8), T(glow, Image.Format.Rgb8));
    }
}
