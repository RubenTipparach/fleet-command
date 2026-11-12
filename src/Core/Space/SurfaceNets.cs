// SurfaceNets: a density field on a grid polygonised into a mesh, the nebula shells' one geometry (the owner, 2026-11-12:
// "Nebula shells. The cube marched, generate once static mesh"; openspec/changes/nebula-shells D2). Naive surface nets,
// the table-free cousin of marching cubes: one vertex in each cell the level crosses, at the mean of the crossings on its
// edges, and one quad for each lattice edge whose ends straddle the level, joining the four cells round it. The normal
// is the field's gradient across the cell, pointing out of the cloud. Plain C#: the view builds its meshes from it and
// the tests check it headless.
using System;
using System.Collections.Generic;

namespace FleetCommand.Space;

/// <summary>A polygonised level of a field: positions and normals (x, y, z each), and triangles as index triples.</summary>
public sealed record NetMesh(float[] Positions, float[] Normals, int[] Indices)
{
    public int Triangles => Indices.Length / 3;
}

public static class SurfaceNets
{
    private static readonly int[,] Corner = { { 0, 0, 0 }, { 1, 0, 0 }, { 0, 1, 0 }, { 1, 1, 0 }, { 0, 0, 1 }, { 1, 0, 1 }, { 0, 1, 1 }, { 1, 1, 1 } };
    private static readonly int[,] Edge = { { 0, 1 }, { 2, 3 }, { 4, 5 }, { 6, 7 }, { 0, 2 }, { 1, 3 }, { 4, 6 }, { 5, 7 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };

    /// <summary>The surface where <paramref name="field"/> equals <paramref name="level"/> (above it is inside). The field
    /// holds (nx + 1) x (ny + 1) x (nz + 1) samples, x fastest; sample (i, j, k) stands at origin + (i, j, k) x step.
    /// A non-finite sample counts as outside.</summary>
    public static NetMesh Build(float[] field, int nx, int ny, int nz, float level, (float X, float Y, float Z) origin, (float X, float Y, float Z) step)
    {
        if (nx < 1 || ny < 1 || nz < 1) throw new ArgumentException("a grid of at least one cell each way");
        int sx = nx + 1, sy = ny + 1;
        if (field.Length != sx * sy * (nz + 1)) throw new ArgumentException($"the field holds {field.Length} samples, not {sx * sy * (nz + 1)}");
        if (!float.IsFinite(level)) throw new ArgumentException("the level must be finite");
        float At(int x, int y, int z) { float v = field[x + sx * (y + sy * z)]; return float.IsFinite(v) ? v - level : -1; }

        var cellV = new int[nx * ny * nz];
        Array.Fill(cellV, -1);
        var pos = new List<float>();
        var nrm = new List<float>();
        var v = new float[8];
        for (int z = 0; z < nz; z++)
            for (int y = 0; y < ny; y++)
                for (int x = 0; x < nx; x++)
                {
                    int inside = 0;
                    for (int c = 0; c < 8; c++) { v[c] = At(x + Corner[c, 0], y + Corner[c, 1], z + Corner[c, 2]); if (v[c] > 0) inside++; }
                    if (inside == 0 || inside == 8) continue;
                    float mx = 0, my = 0, mz = 0;
                    int n = 0;
                    for (int e = 0; e < 12; e++)
                    {
                        int a = Edge[e, 0], b = Edge[e, 1];
                        if (v[a] > 0 == v[b] > 0) continue;
                        float t = v[a] / (v[a] - v[b]);
                        mx += Corner[a, 0] + (Corner[b, 0] - Corner[a, 0]) * t;
                        my += Corner[a, 1] + (Corner[b, 1] - Corner[a, 1]) * t;
                        mz += Corner[a, 2] + (Corner[b, 2] - Corner[a, 2]) * t;
                        n++;
                    }
                    cellV[x + nx * (y + ny * z)] = pos.Count / 3;
                    pos.Add(origin.X + (x + mx / n) * step.X);
                    pos.Add(origin.Y + (y + my / n) * step.Y);
                    pos.Add(origin.Z + (z + mz / n) * step.Z);
                    // the gradient across the cell, in world units; the normal points down it (out of the cloud)
                    float gx = (v[1] + v[3] + v[5] + v[7] - v[0] - v[2] - v[4] - v[6]) / step.X;
                    float gy = (v[2] + v[3] + v[6] + v[7] - v[0] - v[1] - v[4] - v[5]) / step.Y;
                    float gz = (v[4] + v[5] + v[6] + v[7] - v[0] - v[1] - v[2] - v[3]) / step.Z;
                    float len = (float)Math.Sqrt(gx * gx + gy * gy + gz * gz);   // exact under IEEE, so allowed in Core (DMathTests)
                    if (len < 1e-12f) { nrm.Add(0); nrm.Add(1); nrm.Add(0); }
                    else { nrm.Add(-gx / len); nrm.Add(-gy / len); nrm.Add(-gz / len); }
                }

        var idx = new List<int>();
        int Cell(int x, int y, int z) => cellV[x + nx * (y + ny * z)];
        for (int z = 1; z < nz; z++)
            for (int y = 1; y < ny; y++)
                for (int x = 1; x < nx; x++)
                {
                    float d0 = At(x, y, z);
                    for (int axis = 0; axis < 3; axis++)
                    {
                        float d1 = axis == 0 ? At(x + 1, y, z) : axis == 1 ? At(x, y + 1, z) : At(x, y, z + 1);
                        if (d0 > 0 == d1 > 0) continue;
                        int q0, q1, q2, q3;
                        if (axis == 0) { q0 = Cell(x, y - 1, z - 1); q1 = Cell(x, y, z - 1); q2 = Cell(x, y, z); q3 = Cell(x, y - 1, z); }
                        else if (axis == 1) { q0 = Cell(x - 1, y, z - 1); q1 = Cell(x - 1, y, z); q2 = Cell(x, y, z); q3 = Cell(x, y, z - 1); }
                        else { q0 = Cell(x - 1, y - 1, z); q1 = Cell(x, y - 1, z); q2 = Cell(x, y, z); q3 = Cell(x - 1, y, z); }
                        if (q0 < 0 || q1 < 0 || q2 < 0 || q3 < 0) continue;
                        // wound so the quad faces out of the cloud
                        if (d0 > 0) idx.AddRange(new[] { q0, q1, q2, q0, q2, q3 });
                        else idx.AddRange(new[] { q0, q2, q1, q0, q3, q2 });
                    }
                }
        return new NetMesh(pos.ToArray(), nrm.ToArray(), idx.ToArray());
    }
}
