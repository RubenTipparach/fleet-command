// Asteroids: a procedural asteroid (owner, 2026-11-09: "We also need shaders for proc gen asteroids too"; the
// planet-shaders skill). The one builder of a rock: a ball mesh shared by every rock, shaped and coloured on the GPU by
// asteroid.gdshader from the rock's seed, so a field of them costs one mesh and no generation on the CPU. The seed is the
// rock's place in its list, so a restart draws the same rocks. Its colours are a kind's (stone, carbon or ice), from
// data/space/asteroids.json (its looks), read and validated once.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FleetCommand.Data;
using Godot;

namespace FleetCommand.Game.View;

public static class Asteroids
{
    private const string File = "data/space/asteroids.json";
    private static Shader? _shader;
    private static SphereMesh? _mesh;
    private static Dictionary<string, RockLook>? _kinds;
    private sealed record RockLook(Color A, Color B, Color Dust, float Lump, float Facets, float Craters);

    /// <summary>The kinds of rock, in the file's order.</summary>
    public static IReadOnlyList<string> Kinds { get { All(); return _order; } }
    private static readonly List<string> _order = new();

    /// <summary>A rock of a kind, radius 1 before its node's scale, shaped by seed (any whole number).</summary>
    public static MeshInstance3D Make(int seed, string kind = "stone")
    {
        _shader ??= GD.Load<Shader>("res://src/Game/View/asteroid.gdshader");
        _mesh ??= new SphereMesh { Radius = 1, Height = 2, RadialSegments = 96, Rings = 48 };
        if (!All().TryGetValue(kind, out RockLook? k)) throw new DataError($"{File}: no rock kind '{kind}'");
        var m = new ShaderMaterial { Shader = _shader };
        // the seed spread over the noise's domain by FNV-1a, so neighbouring rocks share nothing
        uint h = 2166136261;
        foreach (byte b in BitConverter.GetBytes(seed)) { h ^= b; h *= 16777619; }
        m.SetShaderParameter("seed", new Vector3(h % 997 / 9.7f, h / 997 % 991 / 9.9f, h / 988027 % 983 / 9.8f));
        m.SetShaderParameter("color_a", k.A);
        m.SetShaderParameter("color_b", k.B);
        m.SetShaderParameter("color_dust", k.Dust);
        m.SetShaderParameter("lumpiness", k.Lump);
        m.SetShaderParameter("facets", k.Facets);
        m.SetShaderParameter("craters", k.Craters);
        // the body's swing reaches past the ball: the bounds grow to hold it
        return new MeshInstance3D { Name = "Rock", Mesh = _mesh, MaterialOverride = m, ExtraCullMargin = 1 };
    }

    private static Dictionary<string, RockLook> All()
    {
        if (_kinds != null) return _kinds;
        string text = FileAccess.GetFileAsString("res://" + File);
        if (string.IsNullOrEmpty(text)) throw new DataError($"{File}: cannot be opened");
        StrictJson root = StrictJson.Parse(text, File);
        var kinds = new Dictionary<string, RockLook>();
        foreach (string id in root.Keys())
        {
            StrictJson o = root.Obj(id);
            kinds[id] = new RockLook(Col(o, "color_a"), Col(o, "color_b"), Col(o, "color_dust"), Range(o, "lumpiness", 0, 0.6), Range(o, "facets", 0, 0.5), Range(o, "craters", 0, 1));
            o.Done();
            _order.Add(id);
        }
        root.Done();
        if (!kinds.ContainsKey("stone")) throw new DataError($"{File}: no rock kind 'stone' (the default)");
        return _kinds = kinds;
    }

    private static float Range(StrictJson o, string k, double lo, double hi)
    {
        double v = o.Num(k);
        if (!double.IsFinite(v) || v < lo || v > hi) throw new DataError($"{o.Where}.{k}: between {lo} and {hi}, not {v}");
        return (float)v;
    }

    private static Color Col(StrictJson o, string k)
    {
        string c = o.Str(k);
        return Regex.IsMatch(c, "^#[0-9a-fA-F]{6}$") ? new Color(c) : throw new DataError($"{o.Where}.{k}: a colour as #rrggbb, not '{c}'");
    }
}
