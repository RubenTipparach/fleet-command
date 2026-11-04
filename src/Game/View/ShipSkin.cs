// ShipSkin: dresses an approved ship's glb for the battle from the export manifest (assets/ships/manifest.json, written
// by tools/mockups/export_ships.mjs from the ship mockups). Every surface whose material the manifest marks hull_shader
// gets src/Game/View/hull.gdshader with that material's values (base colour, vertex colours, metalness, the faction's
// detail tile and its size) and the ship's own (its re-centring offset, the Krai livery's uLiv and uLiv2); every other
// material keeps the stand-in the exporter wrote (glows, metals, bells, windows). Its lamps come from the named light
// empties' glTF extras: a flood is a SpotLight3D down its -Z, a running light, strobe or beacon a small OmniLight3D;
// strobes flash as their extras say ("double flash every 1.4 s"). It lives in the view because it only changes how a
// ship looks: the simulation never reads it. One implementation for every ship of both factions.
using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace FleetCommand.Game.View;

public static class ShipSkin
{
    private const string ManifestPath = "res://assets/ships/manifest.json";
    private static JsonDocument? _manifest;
    private static Shader? _hull;

    /// <summary>Range of a running light, strobe or beacon, metres: enough to tint the plating round it, no more.</summary>
    public const float RunningLightRangeM = 6f;

    /// <summary>A strobe's double flash: period and the two flashes' starts and length, seconds.</summary>
    public const float StrobePeriodS = 1.4f, StrobeFlashS = 0.06f, StrobeSecondS = 0.18f;

    private static JsonElement Root => (_manifest ??= JsonDocument.Parse(
        FileAccess.GetFileAsString(ManifestPath) is { Length: > 0 } s ? s : throw new InvalidOperationException($"ship manifest missing: {ManifestPath}"))).RootElement;

    /// <summary>The manifest's entry for a ship file (its path under assets/ships/, such as "krai/frigate.glb").</summary>
    public static JsonElement ShipEntry(string file)
    {
        foreach (JsonElement s in Root.GetProperty("ships").EnumerateArray())
            if (s.GetProperty("file").GetString() == file) return s;
        throw new InvalidOperationException($"ship not in the manifest: {file}");
    }

    /// <summary>Dresses a ship: hull shader on its hull materials, lamps from its light empties. unitsPerMetre is the scale
    /// the ship is drawn at (scene units per glb metre), since a lamp's range is given in metres and a light's range is not
    /// scaled with its node. Returns the strobes, for <see cref="Flash"/>.</summary>
    public static List<OmniLight3D> Apply(Node3D ship, string file, float unitsPerMetre)
    {
        JsonElement entry = ShipEntry(file);
        string faction = entry.GetProperty("faction").GetString()!;
        JsonElement materials = Root.GetProperty("materials");
        JsonElement detail = Root.GetProperty("factions").GetProperty(faction).GetProperty("detail");
        var tex = (GD.Load<Texture2D>("res://assets/ships/" + detail.GetProperty("albedo").GetString()),
                   GD.Load<Texture2D>("res://assets/ships/" + detail.GetProperty("normal").GetString()));
        JsonElement o = entry.GetProperty("origin_offset_m");
        var offset = new Vector3(-F(o[0]), F(o[1]), -F(o[2]));
        JsonElement? sp = entry.TryGetProperty("shader_params", out JsonElement p) ? p : null;
        var cache = new Dictionary<string, Material>();
        var strobes = new List<OmniLight3D>();
        int hullSurfaces = 0, lamps = 0;
        Walk(ship, n =>
        {
            if (n is MeshInstance3D mi && mi.Mesh != null)
            {
                for (int i = 0; i < mi.Mesh.GetSurfaceCount(); i++)
                {
                    Material? m = mi.Mesh.SurfaceGetMaterial(i);
                    if (m == null || !materials.TryGetProperty(faction + "/" + m.ResourceName, out JsonElement me)) continue;
                    if (!me.GetProperty("hull_shader").GetBoolean()) continue;
                    string key = m.ResourceName;
                    if (!cache.TryGetValue(key, out Material? hm)) cache[key] = hm = HullMaterial(me, tex, offset, sp);
                    mi.SetSurfaceOverrideMaterial(i, hm);
                    hullSurfaces++;
                }
            }
            else if (n is Node3D e && e.Name.ToString().StartsWith("light_") && e.HasMeta("extras"))
            {
                Light3D? l = Lamp(e.GetMeta("extras").AsGodotDictionary(), unitsPerMetre, out bool strobe);
                if (l == null) return;
                l.Name = "lamp";
                e.AddChild(l);
                lamps++;
                if (strobe && l is OmniLight3D om) strobes.Add(om);
            }
        });
        GD.Print($"SHIP_SKIN {file}: hull shader on {hullSurfaces} surfaces, {lamps} lamps ({strobes.Count} strobes)");
        return strobes;
    }

    /// <summary>Turns each strobe on during its double flash at time t (seconds).</summary>
    public static void Flash(IEnumerable<OmniLight3D> strobes, double t)
    {
        float ph = (float)(t % StrobePeriodS);
        bool on = ph < StrobeFlashS || (ph >= StrobeSecondS && ph < StrobeSecondS + StrobeFlashS);
        foreach (OmniLight3D s in strobes) s.Visible = on;
    }

    private static ShaderMaterial HullMaterial(JsonElement me, (Texture2D A, Texture2D N) tex, Vector3 offset, JsonElement? sp)
    {
        _hull ??= GD.Load<Shader>("res://src/Game/View/hull.gdshader");
        var m = new ShaderMaterial { Shader = _hull };
        JsonElement bc = me.GetProperty("base_colour"), tile = me.GetProperty("tile_m");
        m.SetShaderParameter("detail_albedo", tex.A);
        m.SetShaderParameter("detail_normal", tex.N);
        m.SetShaderParameter("tile_m", new Vector2(F(tile[0]), F(tile[1])));
        m.SetShaderParameter("mockup_offset", offset);
        m.SetShaderParameter("base_color", new Color(F(bc[0]), F(bc[1]), F(bc[2])));
        m.SetShaderParameter("use_vertex_color", me.GetProperty("vertex_colours").GetBoolean());
        m.SetShaderParameter("metallic", F(me.GetProperty("metalness")));
        bool livery = me.GetProperty("livery").GetBoolean() && sp is { } s && s.TryGetProperty("uLiv", out _);
        m.SetShaderParameter("livery", livery);
        if (livery)
        {
            JsonElement l1 = sp!.Value.GetProperty("uLiv"), l2 = sp.Value.GetProperty("uLiv2");
            m.SetShaderParameter("liv", new Vector4(F(l1[0]), F(l1[1]), F(l1[2]), F(l1[3])));
            m.SetShaderParameter("liv2", new Vector4(F(l2[0]), F(l2[1]), F(l2[2]), F(l2[3])));
        }
        return m;
    }

    // A lamp from a light empty's extras (kind, colour, and for a flood range_m, strength, cone_half_angle_deg).
    private static Light3D? Lamp(Godot.Collections.Dictionary x, float unitsPerMetre, out bool strobe)
    {
        strobe = false;
        string kind = x.TryGetValue("kind", out Variant k) ? k.AsString() : "";
        if (!x.TryGetValue("colour", out Variant cv)) return null;
        float[] c = cv.AsFloat32Array();
        if (c.Length < 3) return null;
        var col = new Color(c[0], c[1], c[2]);   // the hex values over 255, as the mockup gave its lamps
        if (kind == "flood")
        {
            float range = (x.TryGetValue("range_m", out Variant r) ? (float)r.AsDouble() : 12f) * unitsPerMetre;
            return new SpotLight3D
            {
                LightColor = col, SpotRange = range, LightEnergy = x.TryGetValue("strength", out Variant s) ? (float)s.AsDouble() : 1f,
                SpotAngle = x.TryGetValue("cone_half_angle_deg", out Variant a) ? (float)a.AsDouble() : 30f,
                SpotAttenuation = x.TryGetValue("decay", out Variant d) ? (float)d.AsDouble() : 1f, ShadowEnabled = false,
            };
        }
        strobe = kind.Contains("strobe");
        return new OmniLight3D { LightColor = col, OmniRange = RunningLightRangeM * unitsPerMetre, LightEnergy = 1.2f, ShadowEnabled = false };
    }

    private static void Walk(Node n, Action<Node> f)
    {
        f(n);
        foreach (Node c in n.GetChildren()) Walk(c, f);
    }

    private static float F(JsonElement e) => (float)e.GetDouble();
}
