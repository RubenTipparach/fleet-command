// LowShips: a ship in the low-spec mode as one mesh a part with two materials, base and emission (the owner, 2026-11-12:
// "bake hull lights for pi into 1 layer, the mesh should be two materials: base and emission"). A dressed ship (ShipSkin)
// draws each part as a dozen meshes, one a material (plating, metals, markings, glows, bells), and lights its lamps with
// real lights; on a Raspberry Pi 5 every one is a draw call. Here, once per ship model, every surface of the plating,
// metal and markings is unwrapped together into one atlas (Godot's xatlas, ArrayMesh.LightmapUnwrap) and painted onto it
// as the battle draws it (ship_bake.gdshader, which reads the live hull's own look from hull_look.gdshaderinc): its base
// colour, and its hull lights (the floodlight pools, a baked hull's lamps, strips and light map) as one emission layer.
// Each part then becomes one mesh: a base surface (ship_low.gdshader: the two maps, lit by the sun, battle damage as the
// live hull) and an emission surface (every glow, engine throat, window and lens of the part, each vertex its colour,
// unlit: ship_low_emit.gdshader). Its lamps' lights go, their light on the plating baked into the emission layer; its lamp glows, which face the camera, stay as they are. A part
// keeps its node, so the damage (ShipDamage) finds its pieces and bodies as before. The maps and meshes are cached in
// memory and in the machine's user folder (user://lowspec_ships), so a ship is baked once per machine, never committed.
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FleetCommand.Game.View;

public static class LowShips
{
    private const string Version = "v7";   // v7: the look placed in each part's frame; v6: maps read as linear; v4: metalness and roughness in the maps' alpha; v3: 2048 px atlas; v2: the lamps' light, wider seam fill
    private const int MaxAtlasPx = 2048;
    /// <summary>The emission layer is stored at this share of its strength, so bright lamps fit in a byte.</summary>
    public const float EmitScale = 4f;

    private static Shader? _bake, _low, _lowEmit;
    public static Shader LowShader => _low ??= GD.Load<Shader>("res://src/Game/View/ship_low.gdshader");
    private static Shader LowEmitShader => _lowEmit ??= GD.Load<Shader>("res://src/Game/View/ship_low_emit.gdshader");
    private static Shader BakeShader => _bake ??= GD.Load<Shader>("res://src/Game/View/ship_bake.gdshader");

    private sealed record Part(string Name, ArrayMesh Mesh, Vector3 Offset);
    private sealed record Baked(Part[] Parts, ImageTexture Base, ImageTexture Emit);
    private static readonly Dictionary<string, Baked> Cache = new();

    private enum Use { Base, Emit, Keep }
    private sealed record Item(Node3D Part, MeshInstance3D Mi, int Surface, Use Use, Material? Mat, int Kind, Color Glow);

    /// <summary>Turns a dressed ship (ShipSkin.Apply done; its node not yet in the battle) into its low-spec form: one
    /// mesh a part, base and emission. Baked on first use and cached.</summary>
    public static void Convert(Node3D ship, string file)
    {
        if (ship.GetChildCount() == 0 || ship.GetChild(0) is not Node3D root) return;
        List<Item> items = Collect(root);
        if (!Cache.TryGetValue(file, out Baked? baked))
        {
            baked = Load(file) ?? Bake(root, file, items, ship.Scale.X);
            if (baked == null) return;
            Cache[file] = baked;
        }
        // the merged meshes and the lamps' lights go; each part takes its one mesh, with materials of its own (the
        // damage writes its holes into a ship's materials)
        foreach (Item it in items.Where(i => i.Use != Use.Keep)) if (GodotObject.IsInstanceValid(it.Mi)) { it.Mi.GetParent()?.RemoveChild(it.Mi); it.Mi.QueueFree(); }
        foreach (OmniLight3D l in Lights(root)) { l.GetParent()?.RemoveChild(l); l.QueueFree(); }
        foreach (Part p in baked.Parts)
        {
            if (root.GetNodeOrNull<Node3D>(p.Name) is not { } node) continue;
            var mi = new MeshInstance3D { Name = p.Name + "_low", Mesh = p.Mesh, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
            var bm = new ShaderMaterial { Shader = LowShader };
            bm.SetShaderParameter("base_map", baked.Base);
            bm.SetShaderParameter("emit_map", baked.Emit);
            bm.SetShaderParameter("emit_scale", EmitScale);
            bm.SetShaderParameter("mockup_offset", p.Offset);
            mi.SetSurfaceOverrideMaterial(0, bm);
            if (p.Mesh.GetSurfaceCount() > 1) mi.SetSurfaceOverrideMaterial(1, new ShaderMaterial { Shader = LowEmitShader });
            node.AddChild(mi);
        }
    }

    private static IEnumerable<OmniLight3D> Lights(Node n)
    {
        foreach (Node c in n.GetChildren())
        {
            if (c is OmniLight3D l) yield return l;
            foreach (OmniLight3D x in Lights(c)) yield return x;
        }
    }

    // every surface of every part: base (plating, metal, markings), emission (glows, throats, windows, lenses) or kept
    private static List<Item> Collect(Node3D root)
    {
        var items = new List<Item>();
        foreach (Node c in root.GetChildren())
            if (c is Node3D part) Walk(part, part, items);
        return items;
    }

    private static void Walk(Node3D part, Node n, List<Item> items)
    {
        if (n is MeshInstance3D mi && mi.Mesh != null)
            for (int i = 0; i < mi.Mesh.GetSurfaceCount(); i++)
            {
                Material? m = mi.GetSurfaceOverrideMaterial(i) ?? mi.MaterialOverride ?? mi.Mesh.SurfaceGetMaterial(i);
                items.Add(Classify(part, mi, i, m));
            }
        foreach (Node c in n.GetChildren()) Walk(part, c, items);
    }

    private static Item Classify(Node3D part, MeshInstance3D mi, int i, Material? m)
    {
        if (m is ShaderMaterial sm)
        {
            if (sm.Shader == ShipSkin.HullShader) return new Item(part, mi, i, Use.Base, m, 0, default);
            if (sm.Shader == ShipSkin.BakedHullShader) return new Item(part, mi, i, Use.Base, m, 1, default);
            if (sm.Shader == ShipSkin.PaneShader) return new Item(part, mi, i, Use.Emit, m, 0, new Color(1.0f, 0.78f, 0.5f) * 0.7f);
            if (sm.Shader == ShipSkin.LensShader || sm.Shader.ResourcePath.EndsWith("bell.gdshader"))
                return new Item(part, mi, i, Use.Emit, m, 0, ParamColor(sm, "heat_rgb", "color", "tint") ?? new Color(1, 0.6f, 0.3f));
            return new Item(part, mi, i, Use.Keep, m, 0, default);   // the lamp glows (they face the camera) and anything unknown
        }
        if (m is StandardMaterial3D st)
        {
            if (st.EmissionEnabled) return new Item(part, mi, i, Use.Emit, m, 0, st.Emission * st.EmissionEnergyMultiplier + st.AlbedoColor * 0.15f);
            if (st.ShadingMode == BaseMaterial3D.ShadingModeEnum.Unshaded) return new Item(part, mi, i, Use.Emit, m, 0, st.AlbedoColor);
            return new Item(part, mi, i, Use.Base, m, 2, default);
        }
        return new Item(part, mi, i, m == null ? Use.Base : Use.Keep, m, 2, default);
    }

    private static Color? ParamColor(ShaderMaterial sm, params string[] names)
    {
        foreach (string n in names)
        {
            Variant v = sm.GetShaderParameter(n);
            if (v.VariantType == Variant.Type.Color) return v.AsColor();
            if (v.VariantType == Variant.Type.Vector3) { Vector3 c = v.AsVector3(); return new Color(c.X, c.Y, c.Z); }
        }
        return null;
    }

    // ---- the bake ---------------------------------------------------------------------------------------------------

    private static Baked? Bake(Node3D root, string file, List<Item> items, float unitsPerMetre)
    {
        // the lamps (ShipSkin's omni lights), whose light on the plating is baked with the hull lights: in the ship's frame
        var lampPos = new List<Vector4>();
        var lampCol = new List<Vector4>();
        foreach (OmniLight3D l in Lights(root).Take(24))
        {
            Transform3D xf = Transform3D.Identity;
            for (Node? n = l; n != null && n != root; n = n.GetParent()) if (n is Node3D n3) xf = n3.Transform * xf;
            lampPos.Add(new Vector4(xf.Origin.X, xf.Origin.Y, xf.Origin.Z, l.OmniRange / Math.Max(unitsPerMetre, 1e-6f)));
            lampCol.Add(new Vector4(l.LightColor.R, l.LightColor.G, l.LightColor.B, l.LightEnergy));
        }
        ulong t0 = Time.GetTicksMsec();
        List<Item> bases = items.Where(i => i.Use == Use.Base).ToList();
        if (bases.Count == 0) return null;
        // every base surface in the ship's frame, so one unwrap covers the whole ship
        var mesh = new ArrayMesh();
        double area = 0;
        foreach (Item it in bases)
        {
            Godot.Collections.Array a = it.Mi.Mesh.SurfaceGetArrays(it.Surface);
            Transform3D xf = it.Part.Transform * PartLocal(it);
            Vector3[] v = a[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            for (int k = 0; k < v.Length; k++) v[k] = xf * v[k];
            a[(int)Mesh.ArrayType.Vertex] = v;
            if (a[(int)Mesh.ArrayType.Normal].VariantType != Variant.Type.Nil)
            {
                Vector3[] nm = a[(int)Mesh.ArrayType.Normal].AsVector3Array();
                Basis nb = xf.Basis.Inverse().Transposed();
                for (int k = 0; k < nm.Length; k++) nm[k] = (nb * nm[k]).Normalized();
                a[(int)Mesh.ArrayType.Normal] = nm;
            }
            a[(int)Mesh.ArrayType.Tangent] = default;
            a[(int)Mesh.ArrayType.TexUV2] = default;
            int[] idx = a[(int)Mesh.ArrayType.Index].VariantType == Variant.Type.Nil ? Enumerable.Range(0, v.Length).ToArray() : a[(int)Mesh.ArrayType.Index].AsInt32Array();
            a[(int)Mesh.ArrayType.Index] = idx;
            for (int k = 0; k + 2 < idx.Length; k += 3) area += (v[idx[k + 1]] - v[idx[k]]).Cross(v[idx[k + 2]] - v[idx[k]]).Length() / 2;
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, a);
        }
        // a texel size that fills about 60 % of the atlas
        float texel = (float)Math.Sqrt(area / (0.6 * MaxAtlasPx * MaxAtlasPx));
        if (mesh.LightmapUnwrap(Transform3D.Identity, Math.Max(texel, 1e-3f)) != Error.Ok)
        {
            GD.PushError($"LowShips: {file}: the unwrap failed; the ship keeps its full materials");
            return null;
        }
        Vector2I size = mesh.LightmapSizeHint;
        size = new Vector2I(Math.Clamp(size.X, 64, MaxAtlasPx), Math.Clamp(size.Y, 64, MaxAtlasPx));

        // render both layers into the unwrap, now, through the rendering server alone (a battle's ships are built while
        // the scene tree is still setting up, when no viewport can join it)
        var mats = new List<ShaderMaterial>();
        for (int s = 0; s < bases.Count; s++)
        {
            var bm = new ShaderMaterial { Shader = BakeShader };
            Item it = bases[s];
            bm.SetShaderParameter("kind", it.Kind);
            bm.SetShaderParameter("to_part", new Projection(it.Part.Transform.AffineInverse()));
            bm.SetShaderParameter("emit_scale", EmitScale);
            bm.SetShaderParameter("lamp_n", lampPos.Count);
            if (lampPos.Count > 0) { bm.SetShaderParameter("lamp_pos", lampPos.ToArray()); bm.SetShaderParameter("lamp_col", lampCol.ToArray()); }
            if (it.Mat is ShaderMaterial src)
                foreach (Godot.Collections.Dictionary u in src.Shader.GetShaderUniformList())
                {
                    string name = u["name"].AsString();
                    Variant val = src.GetShaderParameter(name);
                    if (val.VariantType != Variant.Type.Nil) bm.SetShaderParameter(name, val);
                }
            else if (it.Mat is StandardMaterial3D st)
            {
                bm.SetShaderParameter("albedo", st.AlbedoColor);
                bm.SetShaderParameter("std_metallic", st.Metallic);
                bm.SetShaderParameter("std_roughness", st.Roughness);
                if (st.AlbedoTexture != null) bm.SetShaderParameter("albedo_tex", st.AlbedoTexture);
            }
            mesh.SurfaceSetMaterial(s, bm);
            mats.Add(bm);
        }
        Rid vp = RenderingServer.ViewportCreate(), scen = RenderingServer.ScenarioCreate(), cam = RenderingServer.CameraCreate();
        RenderingServer.ViewportSetSize(vp, size.X, size.Y);
        RenderingServer.ViewportSetTransparentBackground(vp, true);
        RenderingServer.ViewportSetUpdateMode(vp, RenderingServer.ViewportUpdateMode.Always);
        RenderingServer.ViewportSetScenario(vp, scen);
        RenderingServer.CameraSetPerspective(cam, 60, 0.1f, 100);
        RenderingServer.CameraSetTransform(cam, new Transform3D(Basis.Identity, new Vector3(0, 0, 10)));
        RenderingServer.ViewportAttachCamera(vp, cam);
        RenderingServer.ViewportSetActive(vp, true);
        Rid inst = RenderingServer.InstanceCreate2(mesh.GetRid(), scen);
        RenderingServer.InstanceSetCustomAabb(inst, new Aabb(-Vector3.One * 1e6f, Vector3.One * 2e6f));
        Image Render(int pass)
        {
            foreach (ShaderMaterial m in mats) m.SetShaderParameter("bake_pass", pass);
            RenderingServer.ForceDraw(false);
            Image img = RenderingServer.Texture2DGet(RenderingServer.ViewportGetTexture(vp));
            img.Convert(Image.Format.Rgba8);
            Dilate(img, 8);
            return img;
        }
        Image baseImg = Render(0), emitImg = Render(1), ormImg = Render(2);
        // roughness into the base map's alpha, metalness into the emission map's (ship_low.gdshader reads them there)
        byte[] bd = baseImg.GetData(), ed = emitImg.GetData(), od = ormImg.GetData();
        // (the viewport stores what it draws as linear values, and ship_low.gdshader reads both maps as linear)
        for (int k = 0; k < bd.Length; k += 4) { bd[k + 3] = od[k + 1]; ed[k + 3] = od[k]; }
        baseImg.SetData(size.X, size.Y, false, Image.Format.Rgba8, bd);
        emitImg.SetData(size.X, size.Y, false, Image.Format.Rgba8, ed);
        baseImg.GenerateMipmaps();
        emitImg.GenerateMipmaps();
        RenderingServer.FreeRid(inst);
        RenderingServer.FreeRid(vp);
        RenderingServer.FreeRid(cam);
        RenderingServer.FreeRid(scen);

        // each part: its base surfaces (in its own frame, the unwrap its UVs) and its glowing ones, merged
        var parts = new List<Part>();
        foreach (Node3D part in root.GetChildren().OfType<Node3D>())
        {
            var b = new Merge();
            for (int s = 0; s < bases.Count; s++)
            {
                if (bases[s].Part != part) continue;
                Godot.Collections.Array a = mesh.SurfaceGetArrays(s);
                b.Add(a, part.Transform.AffineInverse(), uvFrom: Mesh.ArrayType.TexUV2, colour: null);
            }
            var e = new Merge();
            foreach (Item it in items.Where(i => i.Use == Use.Emit && i.Part == part))
                e.Add(it.Mi.Mesh.SurfaceGetArrays(it.Surface), PartLocal(it), uvFrom: null, colour: it.Glow);
            if (b.Empty && e.Empty) continue;
            var pm = new ArrayMesh();
            b.AddTo(pm);
            e.AddTo(pm);
            Vector3 offset = bases.FirstOrDefault(i => i.Part == part)?.Mat is ShaderMaterial hm && hm.GetShaderParameter("mockup_offset").VariantType == Variant.Type.Vector3
                ? hm.GetShaderParameter("mockup_offset").AsVector3() : Vector3.Zero;
            if (b.Empty) continue;   // a part of glows alone keeps its own meshes (none in the approved lines)
            parts.Add(new Part(part.Name, pm, offset));
        }
        var baked = new Baked(parts.ToArray(), ImageTexture.CreateFromImage(baseImg), ImageTexture.CreateFromImage(emitImg));
        Save(file, baked, baseImg, emitImg);
        GD.Print($"LOWSHIP {file}: {bases.Count} base surfaces into a {size.X} x {size.Y} atlas, {parts.Count} parts, baked in {(Time.GetTicksMsec() - t0) / 1000.0:0.0} s");
        return baked;
    }

    // a mesh's frame in its part's
    private static Transform3D PartLocal(Item it)
    {
        Transform3D xf = Transform3D.Identity;
        for (Node? n = it.Mi; n != null && n != it.Part; n = n.GetParent())
            if (n is Node3D n3) xf = n3.Transform * xf;
        return xf;
    }

    // a surface built from several: positions and normals moved into one frame, the unwrap as its UVs (base), or every
    // vertex one glowing colour (emission)
    private sealed class Merge
    {
        private readonly List<Vector3> _v = new(), _n = new();
        private readonly List<Vector2> _uv = new();
        private readonly List<Color> _c = new();
        private readonly List<int> _i = new();
        private bool _hasUv, _hasC;
        public bool Empty => _i.Count == 0;

        public void Add(Godot.Collections.Array a, Transform3D xf, Mesh.ArrayType? uvFrom, Color? colour)
        {
            Vector3[] v = a[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            Vector3[] n = a[(int)Mesh.ArrayType.Normal].VariantType != Variant.Type.Nil ? a[(int)Mesh.ArrayType.Normal].AsVector3Array() : new Vector3[v.Length];
            int[] idx = a[(int)Mesh.ArrayType.Index].VariantType != Variant.Type.Nil ? a[(int)Mesh.ArrayType.Index].AsInt32Array() : Enumerable.Range(0, v.Length).ToArray();
            Vector2[]? uv = uvFrom is { } u && a[(int)u].VariantType != Variant.Type.Nil ? a[(int)u].AsVector2Array() : null;
            int start = _v.Count;
            Basis nb = xf.Basis.Inverse().Transposed();
            for (int k = 0; k < v.Length; k++)
            {
                _v.Add(xf * v[k]);
                _n.Add(n[k] == Vector3.Zero ? Vector3.Up : (nb * n[k]).Normalized());
                if (uv != null) { _uv.Add(uv[k]); _hasUv = true; }
                if (colour is { } c) { _c.Add(c); _hasC = true; }
            }
            foreach (int k in idx) _i.Add(start + k);
        }

        public void AddTo(ArrayMesh m)
        {
            if (Empty) return;
            var a = new Godot.Collections.Array();
            a.Resize((int)Mesh.ArrayType.Max);
            a[(int)Mesh.ArrayType.Vertex] = _v.ToArray();
            a[(int)Mesh.ArrayType.Normal] = _n.ToArray();
            if (_hasUv) a[(int)Mesh.ArrayType.TexUV] = _uv.ToArray();
            if (_hasC) a[(int)Mesh.ArrayType.Color] = _c.ToArray();
            a[(int)Mesh.ArrayType.Index] = _i.ToArray();
            m.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, a);
        }
    }

    // the empty texels round each island filled from their painted neighbours, so filtering at an edge never reads clear
    private static void Dilate(Image img, int passes)
    {
        int w = img.GetWidth(), h = img.GetHeight();
        byte[] d = img.GetData();
        for (int p = 0; p < passes; p++)
        {
            byte[] o = (byte[])d.Clone();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = (y * w + x) * 4;
                    if (d[i + 3] != 0) continue;
                    int r = 0, g = 0, b = 0, n = 0;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int xx = x + dx, yy = y + dy;
                            if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                            int j = (yy * w + xx) * 4;
                            if (d[j + 3] == 0) continue;
                            r += d[j]; g += d[j + 1]; b += d[j + 2]; n++;
                        }
                    if (n == 0) continue;
                    o[i] = (byte)(r / n); o[i + 1] = (byte)(g / n); o[i + 2] = (byte)(b / n); o[i + 3] = 255;
                }
            d = o;
        }
        img.SetData(w, h, false, Image.Format.Rgba8, d);
    }

    // ---- the cache on this machine ------------------------------------------------------------------------------------

    private static string Dir(string file)
    {
        string md5 = FileAccess.GetMd5("res://assets/ships/" + file);
        return $"user://lowspec_ships/{Version}/{file.Replace('/', '_')}_{md5}/";
    }

    private static void Save(string file, Baked b, Image baseImg, Image emitImg)
    {
        string dir = Dir(file);
        DirAccess.MakeDirRecursiveAbsolute(dir);
        baseImg.SavePng(dir + "base.png");
        emitImg.SavePng(dir + "emit.png");
        foreach (Part p in b.Parts) ResourceSaver.Save(p.Mesh, dir + p.Name + ".res");
        using var f = FileAccess.Open(dir + "parts.json", FileAccess.ModeFlags.Write);
        f.StoreString(Json.Stringify(new Godot.Collections.Array(b.Parts.Select(p => (Variant)new Godot.Collections.Dictionary
            { ["name"] = p.Name, ["offset"] = new Godot.Collections.Array { p.Offset.X, p.Offset.Y, p.Offset.Z } }))));
    }

    private static Baked? Load(string file)
    {
        string dir = Dir(file);
        if (!FileAccess.FileExists(dir + "parts.json")) return null;
        var list = Json.ParseString(FileAccess.GetFileAsString(dir + "parts.json")).AsGodotArray();
        var parts = new List<Part>();
        foreach (Variant pv in list)
        {
            var d = pv.AsGodotDictionary();
            string name = d["name"].AsString();
            var o = d["offset"].AsGodotArray();
            if (ResourceLoader.Load<ArrayMesh>(dir + name + ".res", null, ResourceLoader.CacheMode.Ignore) is not { } m) return null;
            parts.Add(new Part(name, m, new Vector3((float)o[0].AsDouble(), (float)o[1].AsDouble(), (float)o[2].AsDouble())));
        }
        Image bi = Image.LoadFromFile(dir + "base.png"), ei = Image.LoadFromFile(dir + "emit.png");
        if (bi == null || ei == null) return null;
        bi.GenerateMipmaps(); ei.GenerateMipmaps();
        return new Baked(parts.ToArray(), ImageTexture.CreateFromImage(bi), ImageTexture.CreateFromImage(ei));
    }
}
