// ShipSkin: dresses an approved ship's glb for the battle from the export manifest (assets/ships/manifest.json, written
// by tools/mockups/export_ships.mjs from the ship mockups). Every surface whose material the manifest marks hull_shader
// gets src/Game/View/hull.gdshader with that material's values (base colour, vertex colours, metalness, the faction's
// detail tile and its size) and the ship's own (its re-centring offset, the Krai livery's uLiv and uLiv2); every other
// material keeps the stand-in the exporter wrote (glows, metals, windows), but the impulse bells, which take
// src/Game/View/bell.gdshader from their heat_rgb (hot inside, ribbed outside, as the mockups draw them). Its lamps come from the named light
// empties' glTF extras: a flood is baked into the hull as a light pool with its own glint (hull.gdshader, the mockups'
// pools(); owner, 2026-11-04: "emissive baked lights + shader based multi specular"), a running light, strobe or beacon a small OmniLight3D;
// strobes flash as their extras say ("double flash every 1.4 s"). A hull that carries its own baked maps (the Ssarith
// Hunt's, mockup 12: the manifest marks its material baked_hull and names its maps in shader_params.maps) gets
// src/Game/View/baked_hull.gdshader instead, its floods already baked into its light map (light_baked_flood_* empties draw
// no light), and its windows and launch bays are quads of their own (Panes, pane.gdshader: the rooms and hangars behind
// them, from the window_* and bay_* empties). A lamp whose extras carry a glow_m (the Hunt's running lights, strobes and
// beacons, which the mockup draws as glowing sprites) gets that glow too (lamp_glow.gdshader). It lives in the view because it only changes how a ship looks: the simulation never reads it. One
// implementation for every ship of every faction.
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
    private static Shader? _baked;
    /// <summary>The shader of a hull that carries its own baked maps (the Ssarith Hunt's).</summary>
    public static Shader BakedHullShader => _baked ??= GD.Load<Shader>("res://src/Game/View/baked_hull.gdshader");
    /// <summary>Whether a shader is one of the hull shaders (ShipDamage finds the hull surfaces by it).</summary>
    public static bool IsHull(Shader? s) => s != null && (s == HullShader || s == BakedHullShader || s == LowShips.LowShader);
    private static Shader? _pane;
    /// <summary>The shader of a ship's window and bay quads (ShipDamage gives them the hull's damage).</summary>
    public static Shader PaneShader => _pane ??= GD.Load<Shader>("res://src/Game/View/pane.gdshader");
    private static Shader? _glow;
    /// <summary>The shader of a lamp's glow sprite.</summary>
    public static Shader GlowShader => _glow ??= GD.Load<Shader>("res://src/Game/View/lamp_glow.gdshader");

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
        // the faction's detail tile, for the tiled hulls (a faction whose ships carry their own maps has none)
        (Texture2D A, Texture2D N)? tex = Root.GetProperty("factions").GetProperty(faction).TryGetProperty("detail", out JsonElement detail)
            ? (GD.Load<Texture2D>("res://assets/ships/" + detail.GetProperty("albedo").GetString()), GD.Load<Texture2D>("res://assets/ships/" + detail.GetProperty("normal").GetString()))
            : null;
        JsonElement o = entry.GetProperty("origin_offset_m");
        var offset = new Vector3(-F(o[0]), F(o[1]), -F(o[2]));
        JsonElement? sp = entry.TryGetProperty("shader_params", out JsonElement p) ? p : null;
        var cache = new Dictionary<string, Material>();
        var strobes = new List<OmniLight3D>();
        int hullSurfaces = 0, lamps = 0, glows = 0;
        Walk(ship, n =>
        {
            if (n is MeshInstance3D mi && mi.Mesh != null)
            {
                for (int i = 0; i < mi.Mesh.GetSurfaceCount(); i++)
                {
                    Material? m = mi.Mesh.SurfaceGetMaterial(i);
                    if (m == null || !materials.TryGetProperty(faction + "/" + m.ResourceName, out JsonElement me)) continue;
                    bool bell = me.TryGetProperty("heat_rgb", out _);
                    bool baked = me.TryGetProperty("baked_hull", out JsonElement bk) && bk.GetBoolean();
                    if (!bell && !baked && !me.GetProperty("hull_shader").GetBoolean()) continue;
                    // the shader works in mockup ship space, so each piece's material carries its own pivot (the node's place in the ship)
                    Vector3 piv = PlaceInShip(mi, ship), at = offset + new Vector3(-piv.X, piv.Y, -piv.Z);
                    string key = m.ResourceName + "@" + piv.ToString("F4");
                    if (!cache.TryGetValue(key, out Material? hm)) cache[key] = hm = bell ? BellMaterial(me) : baked ? BakedMaterial(sp, at, file)
                        : HullMaterial(me, tex ?? throw new InvalidOperationException($"{file}: a tiled hull in a faction with no detail tile"), at, sp);
                    mi.SetSurfaceOverrideMaterial(i, hm);
                    hullSurfaces++;
                }
            }
            else if (n is Node3D e && e.Name.ToString().StartsWith("light_") && e.HasMeta("extras"))
            {
                var x = e.GetMeta("extras").AsGodotDictionary();
                Light3D? l = Lamp(x, unitsPerMetre, out bool strobe);
                if (l == null) return;
                l.Name = "lamp";
                e.AddChild(l);
                lamps++;
                if (Glow(x) is { } g) { e.AddChild(g); glows++; }   // on the empty, not the light: a light's global scale is 1, not the ship's   // on the light, so a strobe's glow flashes with it
                if (strobe && l is OmniLight3D om) strobes.Add(om);
            }
        });
        int pools = Pools(ship, offset, cache.Values);
        int panes = Panes(ship, offset);
        int lenses = Lenses(ship);
        GD.Print($"SHIP_SKIN {file}: hull shader on {hullSurfaces} surfaces, {pools} floodlight pools, {lamps} lamps ({strobes.Count} strobes, {glows} glows), {panes} windows and bays, {lenses} lenses");
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

    /// <summary>A ship's windows and launch bays (mockup 12's paneMesh): one quad each, the pane and its frame, laid on the hull
    /// where its window_* or bay_* empty says (its centre, normal, up and across axes, glass size and frame width), drawn by
    /// pane.gdshader on the same node as the empties, so a breaking body takes its panes into every cell copy. Returns how many.</summary>
    private static int Panes(Node3D ship, Vector3 offset)
    {
        var byNode = new Dictionary<Node3D, List<Node3D>>();
        Walk(ship, n =>
        {
            if (n is not Node3D e || !e.HasMeta("extras") || e.GetParent() is not Node3D parent) return;
            string name = e.Name.ToString();
            if (!name.StartsWith("window_") && !(name.StartsWith("bay_") && e.GetMeta("extras").AsGodotDictionary().ContainsKey("across"))) return;
            if (!byNode.TryGetValue(parent, out var list)) byNode[parent] = list = new List<Node3D>();
            list.Add(e);
        });
        int count = 0;
        foreach (var (parent, list) in byNode)
        {
            var pos = new List<Vector3>(); var nrm = new List<Vector3>(); var idx = new List<int>();
            var c0 = new List<float>(); var c1 = new List<float>(); var c2 = new List<float>(); var c3 = new List<float>();
            foreach (Node3D e in list)
            {
                var x = e.GetMeta("extras").AsGodotDictionary();
                Vector3 n = Vec(x["normal"]).Normalized(), v = Vec(x["up"]).Normalized(), u = Vec(x["across"]).Normalized();
                float hw = (float)x["w_m"].AsDouble() / 2, hh = (float)x["h_m"].AsDouble() / 2, f = (float)x["frame_m"].AsDouble();
                int kind = PaneKind(x["room"].AsString()) + (x["lit"].AsBool() ? 8 : 0);
                Vector3 c = e.Position, at = c + n * PaneStandOffM;
                int b = pos.Count;
                foreach (var (qx, qy) in new[] { (-hw - f, -hh - f), (hw + f, -hh - f), (hw + f, hh + f), (-hw - f, hh + f) })
                {
                    pos.Add(at + u * qx + v * qy); nrm.Add(n);
                    c0.AddRange(new[] { qx, qy, hw, hh });
                    c1.AddRange(new[] { u.X, u.Y, u.Z, kind });
                    c2.AddRange(new[] { v.X, v.Y, v.Z, 0f });
                    c3.AddRange(new[] { c.X, c.Y, c.Z, 0f });
                }
                idx.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
                count++;
            }
            var arr = new Godot.Collections.Array();
            arr.Resize((int)Mesh.ArrayType.Max);
            arr[(int)Mesh.ArrayType.Vertex] = pos.ToArray();
            arr[(int)Mesh.ArrayType.Normal] = nrm.ToArray();
            arr[(int)Mesh.ArrayType.Custom0] = c0.ToArray();
            arr[(int)Mesh.ArrayType.Custom1] = c1.ToArray();
            arr[(int)Mesh.ArrayType.Custom2] = c2.ToArray();
            arr[(int)Mesh.ArrayType.Custom3] = c3.ToArray();
            arr[(int)Mesh.ArrayType.Index] = idx.ToArray();
            long rgba = (long)Mesh.ArrayCustomFormat.RgbaFloat;
            var fmt = (Mesh.ArrayFormat)((rgba << (int)Mesh.ArrayFormat.FormatCustom0Shift) | (rgba << (int)Mesh.ArrayFormat.FormatCustom1Shift)
                | (rgba << (int)Mesh.ArrayFormat.FormatCustom2Shift) | (rgba << (int)Mesh.ArrayFormat.FormatCustom3Shift));
            var mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arr, null, null, fmt);
            Vector3 piv = PlaceInShip(parent, ship);
            var mat = new ShaderMaterial { Shader = PaneShader };
            mat.SetShaderParameter("mockup_offset", offset + new Vector3(-piv.X, piv.Y, -piv.Z));
            var mi = new MeshInstance3D { Name = "panes", Mesh = mesh, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
            mi.SetSurfaceOverrideMaterial(0, mat);
            parent.AddChild(mi);
        }
        return count;
    }

    /// <summary>How far a pane's quad stands off the plating, metres (the page's 0.05 m).</summary>
    public const float PaneStandOffM = 0.05f;

    // a pane's room kind, as the page's PANE_KIND
    private static int PaneKind(string room) => room switch
    {
        "quarters" => 0, "mess" => 1, "workshop" => 2, "hold" => 3, "bay" => 4,
        _ => throw new InvalidOperationException($"a pane of unknown kind: {room}"),
    };

    private static Vector3 Vec(Variant v) { float[] a = v.AsFloat32Array(); return new Vector3(a[0], a[1], a[2]); }

    /// <summary>A lamp's glow sprite, when its extras carry a glow_m (its width in metres): a quad facing the camera, its
    /// colour the lamp's, a beacon's pulsing. Null for a lamp without one.</summary>
    private static MeshInstance3D? Glow(Godot.Collections.Dictionary x)
    {
        if (!x.TryGetValue("glow_m", out Variant gm) || !x.TryGetValue("colour", out Variant cv)) return null;
        float[] c = cv.AsFloat32Array(); float w = (float)gm.AsDouble();
        string kind = x.TryGetValue("kind", out Variant k) ? k.AsString() : "";
        var mat = new ShaderMaterial { Shader = GlowShader };
        mat.SetShaderParameter("colour", new Color(c[0], c[1], c[2]));
        mat.SetShaderParameter("pulse", kind.Contains("beacon") ? 1 : 0);
        return new MeshInstance3D { Name = "glow", Mesh = new QuadMesh { Size = new Vector2(w, w), Material = mat }, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
    }

    private static Shader? _lens;
    /// <summary>A glowing mouth's lens (lens.gdshader).</summary>
    public static Shader LensShader => _lens ??= GD.Load<Shader>("res://src/Game/View/lens.gdshader");

    /// <summary>The ship's glowing mouths (mockup 37's lenses, Ship Design Bible T14): a disc on each lens_* empty, facing out
    /// along its normal, its radius, colour and pulse phase the empty's, 0.03 m proud of the mouth as the page lays it. Named
    /// "glow", so a piece that breaks away puts its lenses out with its lamps (ShipDamage). Returns how many.</summary>
    private static int Lenses(Node3D ship)
    {
        var found = new List<(Node3D E, Godot.Collections.Dictionary X)>();
        Walk(ship, n => { if (n is Node3D e && e.Name.ToString().StartsWith("lens_") && e.HasMeta("extras")) found.Add((e, e.GetMeta("extras").AsGodotDictionary())); });
        foreach (var (e, x) in found)
        {
            Vector3 n = Vec(x["normal"]).Normalized();
            float r = (float)x["radius_m"].AsDouble();
            float[] c = x["colour"].AsFloat32Array();
            var mat = new ShaderMaterial { Shader = LensShader };
            mat.SetShaderParameter("colour", new Color(c[0], c[1], c[2]));
            mat.SetShaderParameter("phase", (float)x["phase"].AsDouble());
            Vector3 up = Mathf.Abs(n.Y) > 0.9f ? Vector3.Forward : Vector3.Up;
            var basis = Basis.LookingAt(-n, up);   // the quad's face (+Z) out along the normal
            e.AddChild(new MeshInstance3D { Name = "glow", Mesh = new QuadMesh { Size = new Vector2(2 * r, 2 * r), Material = mat },
                Transform = new Transform3D(basis, n * 0.03f), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        }
        return found.Count;
    }

    /// <summary>Where a mesh's own origin sits in its ship's glb frame (metres): its parents' offsets, up to the ship node.</summary>
    private static Vector3 PlaceInShip(Node3D n, Node3D ship)
    {
        Transform3D xf = Transform3D.Identity;
        for (Node? p = n; p != null && p != ship; p = p.GetParent()) if (p is Node3D p3) xf = p3.Transform * xf;
        return xf.Origin;
    }

    /// <summary>Turns each strobe (and its glow) on during its double flash at time t (seconds).</summary>
    public static void Flash(IEnumerable<OmniLight3D> strobes, double t)
    {
        float ph = (float)(t % StrobePeriodS);
        bool on = ph < StrobeFlashS || (ph >= StrobeSecondS && ph < StrobeSecondS + StrobeFlashS);
        foreach (OmniLight3D s in strobes)
        {
            s.Visible = on;
            if (s.GetParent()?.GetNodeOrNull<Node3D>("glow") is { } g) g.Visible = on;   // its glow flashes with it
        }
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

    // A hull with its own baked maps (baked_hull.gdshader), from the ship's shader_params: its maps and their light curve.
    private static ShaderMaterial BakedMaterial(JsonElement? sp, Vector3 offset, string file)
    {
        JsonElement p = sp ?? throw new InvalidOperationException($"{file}: a baked hull with no shader_params");
        JsonElement maps = p.GetProperty("maps");
        Texture2D Map(string k) => GD.Load<Texture2D>("res://assets/ships/" + maps.GetProperty(k).GetString()) ?? throw new InvalidOperationException($"{file}: map {k} missing");
        var m = new ShaderMaterial { Shader = BakedHullShader };
        m.SetShaderParameter("base_map", Map("base"));
        m.SetShaderParameter("orm_map", Map("orm"));
        m.SetShaderParameter("normal_map", Map("normal"));
        m.SetShaderParameter("emit_map", Map("emit"));
        m.SetShaderParameter("light_map", Map("light"));
        m.SetShaderParameter("light_gamma", F(p.GetProperty("light_gamma")));
        m.SetShaderParameter("mockup_offset", offset);
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
        if (kind.StartsWith("flood")) return null;   // floods are light pools on the hull (Pools), or baked into its light map (flood_baked), not lights
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
