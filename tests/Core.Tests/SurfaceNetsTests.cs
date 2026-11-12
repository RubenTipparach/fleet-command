// SurfaceNets (the nebula shells' geometry): a sphere's field comes out closed, on the sphere, facing out; a field with
// no surface gives no mesh; bad input is refused.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Space;
using Xunit;

namespace FleetCommand.Tests;

public class SurfaceNetsTests
{
    // a ball of radius r round the middle of a grid of n cells a side, one unit a cell: inside is positive
    private static NetMesh Ball(int n, float r)
    {
        var f = new float[(n + 1) * (n + 1) * (n + 1)];
        float c = n / 2f;
        for (int z = 0; z <= n; z++) for (int y = 0; y <= n; y++) for (int x = 0; x <= n; x++)
            f[x + (n + 1) * (y + (n + 1) * z)] = r - MathF.Sqrt((x - c) * (x - c) + (y - c) * (y - c) + (z - c) * (z - c));
        return SurfaceNets.Build(f, n, n, n, 0, (-c, -c, -c), (1, 1, 1));
    }

    [Fact]
    public void A_ball_is_a_closed_mesh_every_edge_shared_by_two_triangles()
    {
        NetMesh m = Ball(20, 6);
        Assert.True(m.Triangles > 100);
        var edges = new Dictionary<(int, int), int>();
        for (int t = 0; t < m.Indices.Length; t += 3)
            for (int e = 0; e < 3; e++)
            {
                int a = m.Indices[t + e], b = m.Indices[t + (e + 1) % 3];
                var key = (Math.Min(a, b), Math.Max(a, b));
                edges[key] = edges.GetValueOrDefault(key) + 1;
            }
        Assert.All(edges.Values, n => Assert.Equal(2, n));
    }

    [Fact]
    public void Every_vertex_of_a_ball_lies_within_half_a_cell_of_its_sphere()
    {
        NetMesh m = Ball(20, 6);
        for (int i = 0; i < m.Positions.Length; i += 3)
        {
            float d = MathF.Sqrt(m.Positions[i] * m.Positions[i] + m.Positions[i + 1] * m.Positions[i + 1] + m.Positions[i + 2] * m.Positions[i + 2]);
            Assert.InRange(d, 5.5f, 6.5f);
        }
    }

    [Fact]
    public void A_balls_normals_and_triangles_face_out()
    {
        NetMesh m = Ball(20, 6);
        for (int i = 0; i < m.Positions.Length; i += 3)
            Assert.True(m.Positions[i] * m.Normals[i] + m.Positions[i + 1] * m.Normals[i + 1] + m.Positions[i + 2] * m.Normals[i + 2] > 0);
        int outward = 0;
        for (int t = 0; t < m.Indices.Length; t += 3)
        {
            float[] p = m.Positions;
            int a = m.Indices[t] * 3, b = m.Indices[t + 1] * 3, c = m.Indices[t + 2] * 3;
            float ux = p[b] - p[a], uy = p[b + 1] - p[a + 1], uz = p[b + 2] - p[a + 2], vx = p[c] - p[a], vy = p[c + 1] - p[a + 1], vz = p[c + 2] - p[a + 2];
            float nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
            if (nx * (p[a] + p[b] + p[c]) + ny * (p[a + 1] + p[b + 1] + p[c + 1]) + nz * (p[a + 2] + p[b + 2] + p[c + 2]) > 0) outward++;
        }
        // Godot's front faces wind clockwise as seen: the cross product of a front face points in, so every triangle agrees
        Assert.True(outward == 0 || outward == m.Triangles);
    }

    [Fact]
    public void A_field_with_no_surface_gives_no_mesh()
    {
        var f = Enumerable.Repeat(-1f, 9 * 9 * 9).ToArray();
        NetMesh m = SurfaceNets.Build(f, 8, 8, 8, 0, (0, 0, 0), (1, 1, 1));
        Assert.Empty(m.Positions);
        Assert.Empty(m.Indices);
    }

    [Fact]
    public void A_field_of_the_wrong_size_or_a_non_finite_level_is_refused()
    {
        Assert.Throws<ArgumentException>(() => SurfaceNets.Build(new float[10], 8, 8, 8, 0, (0, 0, 0), (1, 1, 1)));
        Assert.Throws<ArgumentException>(() => SurfaceNets.Build(new float[729], 8, 8, 8, float.NaN, (0, 0, 0), (1, 1, 1)));
    }
}
