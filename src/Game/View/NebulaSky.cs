// NebulaSky: a scenario's nebula sky in the battle (owner, 2026-11-04: "make sure to generate lots of cool nebulas to be
// used for our demos, I'm getting sick of the same one being used all the time"). It draws a sky file from
// data/space/skies/ (painted by tools/space/nebula_skies.py, the same files the mockups' lib/space.js draws): the sphere
// of coloured vertices, unlit, far out round the camera, and the file's stars as points in their classes' colours
// (data/space/starfield.json), already dimmed by the dust in front of them. It follows the camera's position, never its
// turn, so the sky stays put as the camera orbits. View only: the simulation never reads it.
using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class NebulaSky : Node3D
{
    /// <summary>The sphere's radius, scene units: well inside the camera's far plane (4,000), well outside any battle.</summary>
    public const float RadiusU = 3000f;
    /// <summary>A star's point size, pixels.</summary>
    public const float StarPx = 2f;

    /// <summary>Builds the sky with this id (a file data/space/skies/&lt;id&gt;.json); a missing or broken file is an error naming it.</summary>
    public static NebulaSky Load(string id)
    {
        string path = $"res://data/space/skies/{id}.json";
        string text = FileAccess.GetFileAsString(path);
        if (string.IsNullOrEmpty(text)) throw new InvalidOperationException($"sky not found: {path}");
        using JsonDocument doc = JsonDocument.Parse(text);
        using JsonDocument field = JsonDocument.Parse(FileAccess.GetFileAsString("res://data/space/starfield.json"));
        var sky = new NebulaSky { Name = "Sky_" + id };
        sky.AddChild(Sphere(doc.RootElement.GetProperty("mesh")));
        sky.AddChild(Stars(doc.RootElement.GetProperty("stars").GetProperty("list"), field.RootElement.GetProperty("colours")));
        return sky;
    }

    /// <summary>Keeps the sky centred on the camera.</summary>
    public void Follow(Vector3 camera) => GlobalPosition = camera;

    private static string Rows(JsonElement e)
    {
        if (e.ValueKind == JsonValueKind.String) return e.GetString()!;
        var parts = new List<string>();
        foreach (JsonElement r in e.EnumerateArray()) parts.Add(r.GetString()!);
        return string.Concat(parts);
    }

    // A vertex is 8 bytes: two little-endian 16-bit octahedral coordinates of its direction, then sRGB red, green, blue
    // and how much of the stars behind it the nebula hides; triangles are three little-endian 16-bit indices.
    private static MeshInstance3D Sphere(JsonElement mesh)
    {
        byte[] v = Convert.FromBase64String(Rows(mesh.GetProperty("vertices_b64")));
        byte[] t = Convert.FromBase64String(Rows(mesh.GetProperty("triangles_b64")));
        int n = v.Length / 8;
        var pos = new Vector3[n];
        var col = new Color[n];
        for (int i = 0; i < n; i++)
        {
            int o = i * 8;
            pos[i] = Octa(BitConverter.ToUInt16(v, o), BitConverter.ToUInt16(v, o + 2)) * RadiusU;
            col[i] = Color.Color8(v[o + 4], v[o + 5], v[o + 6]);
        }
        var idx = new int[t.Length / 2];
        for (int i = 0; i < idx.Length; i++) idx[i] = BitConverter.ToUInt16(t, i * 2);
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = pos;
        arrays[(int)Mesh.ArrayType.Color] = col;
        arrays[(int)Mesh.ArrayType.Index] = idx;
        var am = new ArrayMesh();
        am.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, VertexColorUseAsAlbedo = true, VertexColorIsSrgb = true,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled, DisableReceiveShadows = true, DisableFog = true,
        };
        am.SurfaceSetMaterial(0, mat);
        return new MeshInstance3D { Name = "Nebula", Mesh = am, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, ExtraCullMargin = RadiusU };
    }

    // The stars: [x, y, z, brightness 0-1, class, colour], the colour an index into starfield.json's colours.
    private static MeshInstance3D Stars(JsonElement list, JsonElement colours)
    {
        var tints = new List<Color>();
        foreach (JsonElement c in colours.EnumerateArray())
        {
            JsonElement s = c.GetProperty("srgb");
            tints.Add(new Color((float)s[0].GetDouble(), (float)s[1].GetDouble(), (float)s[2].GetDouble()));
        }
        var pos = new List<Vector3>();
        var col = new List<Color>();
        foreach (JsonElement s in list.EnumerateArray())
        {
            pos.Add(new Vector3((float)s[0].GetDouble(), (float)s[1].GetDouble(), (float)s[2].GetDouble()) * (RadiusU * 0.98f));
            float b = (float)s[3].GetDouble();
            Color tint = tints[Math.Clamp(s[5].GetInt32(), 0, tints.Count - 1)];
            col.Add(new Color(tint.R * b, tint.G * b, tint.B * b));
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = pos.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = col.ToArray();
        var am = new ArrayMesh();
        am.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, arrays);
        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, VertexColorUseAsAlbedo = true, VertexColorIsSrgb = true,
            UsePointSize = true, PointSize = StarPx, DisableFog = true,
        };
        am.SurfaceSetMaterial(0, mat);
        return new MeshInstance3D { Name = "Stars", Mesh = am, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, ExtraCullMargin = RadiusU };
    }

    // The octahedral encoding back to a unit direction (the sky file's frame: +y up).
    private static Vector3 Octa(ushort u, ushort v)
    {
        float x = u / 65535f * 2 - 1, y = v / 65535f * 2 - 1, z = 1 - Math.Abs(x) - Math.Abs(y);
        float t = Math.Max(-z, 0);
        x += x >= 0 ? -t : t;
        y += y >= 0 ? -t : t;
        return new Vector3(x, y, z).Normalized();
    }
}
