// Starfield2D: a flat star backdrop from the seeded generator, never a cheap hash (owner, 2026-11-05: "never to use poor RNG
// stuff for stars", "you can see a repetition pattern"). It replays from its seed, covers the frame evenly, keeps the
// shares of data/space/starfield.json, and shows no line or lattice.
using System;
using System.Linq;
using FleetCommand.Sim;
using FleetCommand.Space;
using Xunit;

namespace FleetCommand.Tests;

public class StarfieldTests
{
    private static readonly (System.Collections.Generic.List<StarClass> Classes, System.Collections.Generic.List<StarColour> Colours) Spec =
        Starfield2D.LoadSpec(TestData.Read("space/starfield.json"));

    private static System.Collections.Generic.List<Star> Field(ulong seed = 7) => Starfield2D.Make(1920, 1080, seed, Spec.Classes, Spec.Colours);

    [Fact]
    public void The_same_seed_and_size_give_the_same_stars_and_another_seed_others()
    {
        Assert.Equal(Field(), Field());
        Assert.NotEqual(Field(7).Take(50), Field(8).Take(50));
    }

    [Fact]
    public void Every_star_is_inside_the_frame()
    {
        Assert.All(Field(), s => Assert.True(s.X >= 0 && s.Y >= 0 && s.X < 1920 && s.Y < 1080));
    }

    [Fact]
    public void The_even_field_keeps_its_spacing_so_nothing_clumps()
    {
        var pts = Starfield2D.PoissonDisc(800, 600, 20, new Rng(3));
        for (int i = 0; i < pts.Count; i++)
            for (int j = i + 1; j < pts.Count; j++)
                Assert.True(Math.Pow(pts[i].X - pts[j].X, 2) + Math.Pow(pts[i].Y - pts[j].Y, 2) >= 20 * 20 - 1e-6);
        // and it fills the frame: every 80 px square holds a star
        for (int y = 0; y < 600; y += 80)
            for (int x = 0; x < 800; x += 80)
                Assert.Contains(pts, p => p.X >= x && p.X < x + 80 && p.Y >= y && p.Y < y + 80);
    }

    [Fact]
    public void Most_stars_are_faint_and_the_classes_keep_their_shares()
    {
        var stars = Field();
        for (int c = 0; c < Spec.Classes.Count; c++)
        {
            double share = stars.Count(s => s.Class == c) / (double)stars.Count;
            Assert.InRange(share, Spec.Classes[c].Share - 0.04, Spec.Classes[c].Share + 0.04);
        }
    }

    [Fact]
    public void No_star_follows_the_last_in_a_line_as_a_modular_hash_does()
    {
        // i * 7919 % 1000 puts each point a fixed step from the last; a real generator's steps are uncorrelated
        var s = Field();
        double[] dx = s.Zip(s.Skip(1), (a, b) => (double)(b.X - a.X)).ToArray();
        double mean = dx.Average(), lag = 0, var = 0;
        for (int i = 0; i + 1 < dx.Length; i++) { lag += (dx[i] - mean) * (dx[i + 1] - mean); var += (dx[i] - mean) * (dx[i] - mean); }
        Assert.InRange(lag / var, -0.6, 0.6);
        // and the same step never repeats for long: no two in a row share their step to a tenth of a pixel more than rarely
        int same = 0;
        for (int i = 0; i + 1 < dx.Length; i++) if (Math.Abs(dx[i] - dx[i + 1]) < 0.1) same++;
        Assert.True(same < dx.Length * 0.01, $"{same} repeated steps of {dx.Length}");
    }
}
