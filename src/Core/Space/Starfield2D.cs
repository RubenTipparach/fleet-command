// Starfield2D: a flat field of stars for a backdrop drawn in 2D (the menus), made the way a sky is (owner, 2026-11-05: "never
// to use poor RNG stuff for stars", "can you make a better proc gen starfield?"). Every number comes from the seeded SplitMix64
// generator (Rng), never a cheap hash. Three layers, each placed on purpose:
//   * the field: Poisson-disc points (Bridson's method), so the stars cover the frame evenly with no clumps, lines or grid;
//   * the band: a soft stripe across the frame, the galaxy's disc, where extra stars are thinned in by a density that
//     falls off from its middle line and wanders along it;
//   * clusters: a few knots of stars, each scattered round its centre by a Gaussian.
// A star's class (faint to brilliant), brightness and colour come by the shares in data/space/starfield.json, so most stars
// are faint and a few are bright, as in the battle's sky and on the campaign map. Plain C#: tested headless.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Sim;

namespace FleetCommand.Space;

/// <summary>A class of star (data/space/starfield.json's classes): its share of all stars, brightness range and head size.</summary>
public sealed record StarClass(string Name, double Share, double B0, double B1, double SizePx);

/// <summary>A star colour (starfield.json's colours): its share and sRGB colour, each 0 to 1.</summary>
public sealed record StarColour(string Name, double Share, double R, double G, double B);

/// <summary>One star: where (pixels), how bright (0 to 1), its class and colour (indices), and a twinkle phase (radians).</summary>
public readonly record struct Star(float X, float Y, float Brightness, int Class, int Colour, float Phase);

public static class Starfield2D
{
    /// <summary>Stars a million pixels hold in the even field, in the band at its middle line, and in clusters.</summary>
    public const double FieldPerMpx = 520, BandPerMpx = 900, ClusterPerMpx = 70;
    /// <summary>The band's width as a share of the frame's diagonal, and how many clusters a frame has.</summary>
    public const double BandWidth = 0.11;
    public const int Clusters = 3;

    /// <summary>The classes and colours of data/space/starfield.json (its text).</summary>
    public static (List<StarClass> Classes, List<StarColour> Colours) LoadSpec(string json)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        var classes = root.GetProperty("classes").EnumerateArray().Select(e => new StarClass(e.GetProperty("name").GetString()!, e.GetProperty("share").GetDouble(),
            e.GetProperty("brightness")[0].GetDouble(), e.GetProperty("brightness")[1].GetDouble(), e.GetProperty("size_px").GetDouble())).ToList();
        var colours = root.GetProperty("colours").EnumerateArray().Select(e => new StarColour(e.GetProperty("name").GetString()!, e.GetProperty("share").GetDouble(),
            e.GetProperty("srgb")[0].GetDouble(), e.GetProperty("srgb")[1].GetDouble(), e.GetProperty("srgb")[2].GetDouble())).ToList();
        return (classes, colours);
    }

    /// <summary>The stars for a frame of w by h pixels, the same every time for the same seed and size.</summary>
    public static List<Star> Make(int w, int h, ulong seed, IReadOnlyList<StarClass> classes, IReadOnlyList<StarColour> colours)
    {
        if (w <= 0 || h <= 0 || classes.Count == 0 || colours.Count == 0) return new List<Star>();
        double mpx = w * (double)h / 1e6;
        var pos = new List<(double X, double Y)>();

        // the field: Poisson-disc, its spacing set so the frame holds about FieldPerMpx a million pixels
        int fieldN = (int)Math.Round(FieldPerMpx * mpx);
        double r = Math.Sqrt(w * (double)h / (fieldN * 1.6));
        pos.AddRange(PoissonDisc(w, h, r, Rng.Fork(seed, "starfield.field")));

        // the band: a stripe through the frame at a seeded angle, its middle line wandering by a few seeded waves
        var rb = Rng.Fork(seed, "starfield.band");
        double ang = (rb.Next() * 0.5 + 0.25) * Math.PI * (rb.Next() < 0.5 ? 1 : -1) * 0.5;   // 22 to 68 degrees off flat
        double cx = w * (0.35 + 0.3 * rb.Next()), cy = h * (0.35 + 0.3 * rb.Next());
        double diag = Math.Sqrt(w * (double)w + h * (double)h), half = BandWidth * diag;
        var waves = Enumerable.Range(0, 3).Select(k => (A: half * (0.6 / (k + 1)) * rb.Next(), F: (k + 1) * (1.5 + rb.Next()) / diag, P: rb.Next() * Math.Tau)).ToArray();
        double ux = Math.Cos(ang), uy = Math.Sin(ang);
        int bandTry = (int)Math.Round(BandPerMpx * mpx * 2.2);
        for (int i = 0; i < bandTry; i++)
        {
            double x = rb.Next() * w, y = rb.Next() * h;
            double along = (x - cx) * ux + (y - cy) * uy, across = -(x - cx) * uy + (y - cy) * ux;
            double mid = waves.Sum(v => v.A * Math.Sin(along * v.F * Math.Tau + v.P));
            double d = (across - mid) / half;
            if (rb.Next() < Math.Exp(-0.5 * d * d) * 0.45) pos.Add((x, y));
        }

        // clusters: knots scattered round seeded centres by a Gaussian (Box-Muller)
        var rc = Rng.Fork(seed, "starfield.clusters");
        int perCluster = (int)Math.Round(ClusterPerMpx * mpx / Clusters * 3);
        for (int c = 0; c < Clusters; c++)
        {
            double kx = w * (0.1 + 0.8 * rc.Next()), ky = h * (0.1 + 0.8 * rc.Next()), spread = diag * (0.012 + 0.02 * rc.Next());
            int n = (int)(perCluster * (0.5 + rc.Next()));
            for (int i = 0; i < n; i++)
            {
                double u1 = Math.Max(1e-12, rc.Next()), u2 = rc.Next(), g = Math.Sqrt(-2 * Math.Log(u1));
                double x = kx + spread * g * Math.Cos(Math.Tau * u2), y = ky + spread * g * Math.Sin(Math.Tau * u2);
                if (x >= 0 && y >= 0 && x < w && y < h) pos.Add((x, y));
            }
        }

        // class, brightness, colour and twinkle phase for each, from their own stream
        var rs = Rng.Fork(seed, "starfield.look");
        double clsSum = classes.Sum(c => c.Share), colSum = colours.Sum(c => c.Share);
        var stars = new List<Star>(pos.Count);
        foreach (var (x, y) in pos)
        {
            int cl = Pick(rs.Next() * clsSum, classes.Select(c => c.Share));
            StarClass k = classes[cl];
            double b = k.B0 + (k.B1 - k.B0) * rs.Next();
            int co = Pick(rs.Next() * colSum, colours.Select(c => c.Share));
            stars.Add(new Star((float)x, (float)y, (float)b, cl, co, (float)(rs.Next() * Math.Tau)));
        }
        return stars;
    }

    // the index whose running share first passes x
    private static int Pick(double x, IEnumerable<double> shares)
    {
        int i = 0;
        foreach (double s in shares) { if ((x -= s) <= 0) return i; i++; }
        return i - 1;
    }

    /// <summary>Bridson's Poisson-disc sampling: points in a w by h frame, none closer than r to another, filling it.</summary>
    public static List<(double X, double Y)> PoissonDisc(double w, double h, double r, Rng rng, int tries = 30)
    {
        double cell = r / Math.Sqrt(2);
        int gw = (int)Math.Ceiling(w / cell), gh = (int)Math.Ceiling(h / cell);
        var grid = new int[gw * gh];
        Array.Fill(grid, -1);
        var pts = new List<(double X, double Y)>();
        var active = new List<int>();
        void Add(double x, double y) { grid[(int)(y / cell) * gw + (int)(x / cell)] = pts.Count; active.Add(pts.Count); pts.Add((x, y)); }
        bool Free(double x, double y)
        {
            int gx = (int)(x / cell), gy = (int)(y / cell);
            for (int j = Math.Max(0, gy - 2); j <= Math.Min(gh - 1, gy + 2); j++)
                for (int i = Math.Max(0, gx - 2); i <= Math.Min(gw - 1, gx + 2); i++)
                {
                    int k = grid[j * gw + i];
                    if (k >= 0 && (pts[k].X - x) * (pts[k].X - x) + (pts[k].Y - y) * (pts[k].Y - y) < r * r) return false;
                }
            return true;
        }
        Add(rng.Next() * w, rng.Next() * h);
        while (active.Count > 0)
        {
            int ai = rng.Below(active.Count);
            var (px, py) = pts[active[ai]];
            bool placed = false;
            for (int t = 0; t < tries; t++)
            {
                double a = rng.Next() * Math.Tau, d = r * (1 + rng.Next());
                double x = px + Math.Cos(a) * d, y = py + Math.Sin(a) * d;
                if (x < 0 || y < 0 || x >= w || y >= h || !Free(x, y)) continue;
                Add(x, y);
                placed = true;
                break;
            }
            if (!placed) { active[ai] = active[^1]; active.RemoveAt(active.Count - 1); }
        }
        return pts;
    }
}
