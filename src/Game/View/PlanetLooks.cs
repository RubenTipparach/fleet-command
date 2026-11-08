// PlanetLooks: how each kind of planet is drawn (data/space/planets.json; owner, 2026-11-08: "In game Planets: pull in planets
// from godot-sandbox and falchion"). One look per PlanetDef.Kinds, read and validated once, then set on a planet's two
// materials (planet.gdshader for the ground, planet_atmosphere.gdshader for the air) in one place, so every planet of a kind
// looks alike and the numbers live in data, not in the view (CLAUDE.md 6.4). A missing kind, an unknown key or a bad colour
// stops the view naming the file and field. The view only: the simulation never reads it.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FleetCommand.Data;
using Godot;

namespace FleetCommand.Game.View;

/// <summary>One kind of planet's look (a row of data/space/planets.json).</summary>
public sealed record PlanetLook(int Surface, Color[] Palette, float Sea, float IceLat, float Clouds, Color CloudColor, Color Atmosphere,
    float AtmosphereStrength, float AtmosphereHeight, float Detail, bool Craters, float Canyons, Color Glow, Color CityGlow, float Cities, bool Volcanoes);

public static class PlanetLooks
{
    /// <summary>The surface models the planet shader draws, by name: 0 rocky, 1 gas, 2 lava.</summary>
    public static readonly string[] Surfaces = { "rocky", "gas", "lava" };
    private const string File = "data/space/planets.json";
    private static Dictionary<string, PlanetLook>? _looks;

    /// <summary>The look of a kind of planet (an unknown kind is an error naming it).</summary>
    public static PlanetLook For(string kind) =>
        All().TryGetValue(kind, out PlanetLook? l) ? l : throw new DataError($"{File}: no look for planet kind '{kind}'");

    /// <summary>Every look, read from the file once.</summary>
    public static IReadOnlyDictionary<string, PlanetLook> All()
    {
        if (_looks != null) return _looks;
        string text = FileAccess.GetFileAsString("res://" + File);
        if (string.IsNullOrEmpty(text)) throw new DataError($"{File}: cannot be opened");
        StrictJson root = StrictJson.Parse(text, File);
        var looks = new Dictionary<string, PlanetLook>();
        foreach (string kind in root.Keys())
        {
            if (!PlanetDef.Kinds.Contains(kind)) throw new DataError($"{File}.{kind}: not a planet kind (there are: {string.Join(", ", PlanetDef.Kinds)})");
            looks[kind] = Read(root.Obj(kind));
        }
        root.Done();
        foreach (string k in PlanetDef.Kinds)
            if (!looks.ContainsKey(k)) throw new DataError($"{File}: no look for planet kind '{k}'");
        return _looks = looks;
    }

    private static PlanetLook Read(StrictJson o)
    {
        string surface = o.Str("surface");
        int si = Array.IndexOf(Surfaces, surface);
        if (si < 0) throw new DataError($"{o.Where}.surface: one of {string.Join(", ", Surfaces)}, not '{surface}'");
        string[] pal = o.StrList("palette");
        if (pal.Length != 4) throw new DataError($"{o.Where}.palette: four colours");
        var look = new PlanetLook(si, pal.Select((c, i) => Hex(c, $"{o.Where}.palette[{i}]")).ToArray(),
            Range(o, "sea", -2, 1), Range(o, "ice_lat", 0, 2), Range(o, "clouds", 0, 1), Col(o, "cloud_color"), Col(o, "atmosphere"),
            Range(o, "atmosphere_strength", 0, 3), Range(o, "atmosphere_height", 0.005, 0.3), Range(o, "detail", 0.2, 8),
            o.Bool("craters", false), o.Has("canyons") ? Range(o, "canyons", 0, 2) : 0, Col(o, "glow"), Col(o, "city_glow"),
            Range(o, "cities", 0, 1), o.Bool("volcanoes", false));
        o.Done();
        return look;
    }

    private static float Range(StrictJson o, string k, double lo, double hi)
    {
        double v = o.Num(k);
        if (!double.IsFinite(v) || v < lo || v > hi) throw new DataError($"{o.Where}.{k}: between {lo} and {hi}, not {v}");
        return (float)v;
    }

    private static Color Col(StrictJson o, string k) => Hex(o.Str(k), $"{o.Where}.{k}");

    private static Color Hex(string c, string where) =>
        Regex.IsMatch(c, "^#[0-9a-fA-F]{6}$") ? new Color(c) : throw new DataError($"{where}: a colour as #rrggbb, not '{c}'");

    /// <summary>Sets a look on a planet's ground and air materials, with the planet's own seed (its noise offset).</summary>
    public static void Apply(PlanetLook l, Vector3 seed, ShaderMaterial ground, ShaderMaterial air)
    {
        ground.SetShaderParameter("surface", l.Surface);
        for (int i = 0; i < 4; i++) ground.SetShaderParameter($"c{i}", l.Palette[i]);
        ground.SetShaderParameter("sea", l.Sea);
        ground.SetShaderParameter("ice_lat", l.IceLat);
        ground.SetShaderParameter("clouds", l.Clouds);
        ground.SetShaderParameter("cloud_color", l.CloudColor);
        ground.SetShaderParameter("atmo", l.Atmosphere);
        ground.SetShaderParameter("atmo_strength", l.AtmosphereStrength);
        ground.SetShaderParameter("detail", l.Detail);
        ground.SetShaderParameter("craters", l.Craters ? 1f : 0f);
        ground.SetShaderParameter("canyons", l.Canyons);
        ground.SetShaderParameter("glow", l.Glow);
        ground.SetShaderParameter("city_glow", l.CityGlow);
        ground.SetShaderParameter("cities", l.Cities);
        ground.SetShaderParameter("volcanoes", l.Volcanoes ? 1f : 0f);
        ground.SetShaderParameter("seed", seed);
        air.SetShaderParameter("scatter_tint", l.Atmosphere);
        air.SetShaderParameter("scatter_strength", 4f * l.AtmosphereStrength);
        air.SetShaderParameter("atmo_radius", 1f + l.AtmosphereHeight);
    }

    /// <summary>A planet's own noise offset from its id (FNV-1a), so the same planet looks the same in every battle.</summary>
    public static Vector3 SeedOf(string id)
    {
        uint h = 2166136261;
        foreach (char ch in id) { h ^= ch; h *= 16777619; }
        return new Vector3(h % 1000 / 10f, h / 1000 % 1000 / 10f, h / 1000000 % 1000 / 10f);
    }
}
