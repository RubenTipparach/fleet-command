// ShipSkin: dresses an approved ship's glb for the battle from the export manifest (assets/ships/manifest.json, written
// by tools/mockups/export_ships.mjs from the ship mockups). Every surface whose material the manifest marks hull_shader
// gets src/Game/View/hull.gdshader with that material's values (base colour, vertex colours, metalness, the faction's
// detail tile and its size) and the ship's own (its re-centring offset, the Krai livery's uLiv and uLiv2); every other
// material keeps the stand-in the exporter wrote (glows, metals, windows), but the impulse bells, which take
// src/Game/View/bell.gdshader from their heat_rgb (hot inside, ribbed outside, as the mockups draw them). Its lamps come from the named light
// empties' glTF extras: a flood is baked into the hull as a light pool with its own glint (hull.gdshader, the mockups'
// pools(); owner, 2026-11-04: "emissive baked lights + shader based multi specular"), a running light, strobe or beacon a small OmniLight3D;
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
    /// <summary>The hull shader every hull surface is dressed with (ShipDamage finds the hull surfaces by it).</summary>
    public static Shader HullShader => _hull ??= GD.Load<Shader>("res://src/Game/View/hull.gdshader");

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
                    bool bell = me.TryGetProperty("heat_rgb", out _);
                    if (!bell && !me.GetProperty("hull_shader").GetBoolean()) continue;
                    // the shader works in mockup ship space, so each piece's material carries its own pivot (the node's place in the ship)
                    Vector3 piv = PlaceInShip(mi, ship), at = offset + new Vector3(-piv.X, piv.Y, -piv.Z);
                    string key = m.ResourceName + "@" + piv.ToString("F4");
                    if (!cache.TryGetValue(key, out Material? hm)) cache[key] = hm = bell ? BellMaterial(me) : HullMaterial(me, tex, at, sp);
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
        int pools = Pools(ship, offset, cache.Values);
        GD.Print($"SHIP_SKIN {file}: hull shader on {hullSurfaces} surfaces, {pools} floodlight pools, {lamps} lamps ({strobes.Count} strobes)");
        return strobes;
    }

    /// <summary>The ship's floodlights as baked light pools on its hull (hull.gdshader pool_light, the mockups' pools()):
    /// each light_flood_* empty's place and aim taken back into mockup ship space, with its range, strength and colour.
    /// Returns how many (at most 16, as the mockups).</summary>
    private static int Pools(Node3D ship, Vector3 offset, IEnumerable<Material> mats)
    {
        var pos = new Vector4[16]; var dir = new Vector4[16]; var str = new float[16]; var col = new Vector4[16];
        int n = 0;
        Walk(ship, node =>
        {
            if (n >= 16 || node is not Node3D e || !e.Name.ToString().StartsWith("light_flood") || !e.HasMeta("extras")) return;
            var x = e.GetMeta("extras").AsGodotDictionary();
            Vector3 g = PlaceInShip(e, ship), p = offset + new Vector3(-g.X, g.Y, -g.Z);
            float[] d = x["direction"].AsFloat32Array(), c = x["colour"].AsFloat32Array();
            pos[n] = new Vector4(p.X, p.Y, p.Z, (float)x["range_m"].AsDouble());
            dir[n] = new Vector4(-d[0], d[1], -d[2], 0);
            str[n] = (float)x["strength"].AsDouble();
            col[n] = new Vector4(c[0], c[1], c[2], 1);
            n++;
        });
        foreach (Material m in mats)
        {
            if (m is not ShaderMaterial sm || sm.Shader != HullShader) continue;
            sm.SetShaderParameter("pools", pos);
            sm.SetShaderParameter("pool_d", dir);
            sm.SetShaderParameter("pool_a", str);
            sm.SetShaderParameter("pool_c", col);
            sm.SetShaderParameter("pool_n", n);
        }
        return n;
    }

    /// <summary>Where a mesh's own origin sits in its ship's glb frame (metres): its parents' offsets, up to the ship node.</summary>
    private static Vector3 PlaceInShip(Node3D n, Node3D ship)
    {
        Transform3D xf = Transform3D.Identity;
        for (Node? p = n; p != null && p != ship; p = p.GetParent()) if (p is Node3D p3) xf = p3.Transform * xf;
        return xf.Origin;
    }

    /// <summary>Turns each strobe on during its double flash at time t (seconds).</summary>
    public static void Flash(IEnumerable<OmniLight3D> strobes, double t)
    {
        float ph = (float)(t % StrobePeriodS);
        bool on = ph < StrobeFlashS || (ph >= StrobeSecondS && ph < StrobeSecondS + StrobeFlashS);
        foreach (OmniLight3D s in strobes) s.Visible = on;
    }

    private static Shader? _bell;

    // An impulse bell: hot inside, ribbed outside, from the manifest's heat_rgb (the mockup's thrusterMat).
    private static ShaderMaterial BellMaterial(JsonElement me)
    {
        _bell ??= GD.Load<Shader>("res://src/Game/View/bell.gdshader");
        JsonElement h = me.GetProperty("heat_rgb"), a = h.GetProperty("throat"), b = h.GetProperty("lip"), bc = me.GetProperty("base_colour");
        var m = new ShaderMaterial { Shader = _bell };
        m.SetShaderParameter("shell", new Color(F(bc[0]), F(bc[1]), F(bc[2])));
        m.SetShaderParameter("heat_a", new Color(F(a[0]), F(a[1]), F(a[2])));
        m.SetShaderParameter("heat_b", new Color(F(b[0]), F(b[1]), F(b[2])));
        return m;
    }

    private static ShaderMaterial HullMaterial(JsonElement me, (Texture2D A, Texture2D N) tex, Vector3 offset, JsonElement? sp)
    {
        var m = new ShaderMaterial { Shader = HullShader };
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
        if (kind == "flood") return null;   // floods are baked into the hull as light pools (Pools), not lights
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
