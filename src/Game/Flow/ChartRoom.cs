// ChartRoom: the campaign map's paper look (owner, 2026-11-08: "implement space chart in game, it looks great!"; change
// paper-map; mockup 1 and mockup 34, docs/mockups/game-flow.html: buildRoom, paperSheet, paperMesh, the drawn pieces). A
// printed star chart on old paper, lying on a walnut table in a red-carpeted room with dark green walls, lit by one warm lamp
// over the table, the room falling away into shadow round its pool of light. What stands on the chart is drawn by hand: the
// nebulae as inked and washed cloud puffs (one cloud in three a storm) each laying a hatched shadow on its sector, the stars
// as rayed suns, the belts as drawn rocks, the planets drawn live by planet.gdshader; names are hand-lettered.
//
// Everything that looks a certain way comes from the page, exported by tools/mockups/export_chart.mjs into
// assets/scenes/chart/ (the room, the sheet's mesh and still layers, every drawing and where it stands, the lights, the
// planets' kinds and palettes): this file only places what the manifest says. The one thing drawn here is what changes in
// play: each sector's holder and fog, written into two small textures that chart_sheet.gdshader reads (the page's grid and
// fog rules, ported there). It holds no rules: what it shows is read from Campaign.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FleetCommand.Campaign;
using FleetCommand.Game.View;
using Godot;

namespace FleetCommand.Game.Flow;

public sealed partial class ChartRoom : Node3D
{
    public const string Dir = "res://assets/scenes/chart/";
    private static JsonDocument? _manifest;
    /// <summary>The export's manifest (assets/scenes/chart/manifest.json), read once.</summary>
    public static JsonElement Manifest => (_manifest ??= JsonDocument.Parse(FileAccess.GetFileAsString(Dir + "manifest.json"))).RootElement;

    private Campaign.Campaign _c = null!;
    private Func<string, Color> _facColor = null!;
    private Image _ownerImg = null!, _fogImg = null!, _maskImg = null!;
    private ImageTexture _ownerTex = null!, _fogTex = null!, _maskTex = null!;
    private Rect2 _sheet;
    /// <summary>The fog mask's pixels per map unit, and its blur: two box passes this many pixels wide each way, about the
    /// page's 0.28 units of blur (paperSheet's softDraw).</summary>
    private const int MaskPx = 8, MaskBlur = 1;
    /// <summary>The low-spec mode's baked sheet, this many pixels across (the layers are 56 a unit, about 2,800 across).</summary>
    private const int BakePx = 2048;
    /// <summary>The low-spec mode's sheet is cut into this many tiles each way.</summary>
    private const int SheetTiles = 8;
    private SubViewport? _bake;
    private ShaderMaterial? _lowMat;
    private ImageTexture? _baked;
    // the drawn pieces that stand in a sector, hidden while it is uncharted
    private readonly List<(Node3D Node, int C, int R)> _fogged = new();
    // the drawings (billboards) and cloud shadows, batched: one MultiMesh a drawing, each instance its place and sector, so the
    // chart's several hundred drawings cost a few dozen draw calls; an uncharted sector's instances fold to nothing
    private readonly List<(MultiMesh Mm, int I, Transform3D At, int C, int R)> _fogInst = new();

    private static Vector3 V(JsonElement a) => new(a[0].GetSingle(), a[1].GetSingle(), a[2].GetSingle());

    /// <summary>Builds the room, the sheet and the drawn pieces for a campaign; <paramref name="facColor"/> is each holder's ink.</summary>
    public void Init(Campaign.Campaign c, Func<string, Color> facColor, Dictionary<(int, int), (Color Main, Color Accent, int MainIdx, int Id)> regions)
    {
        _c = c;
        _facColor = facColor;
        Name = "ChartRoom";
        JsonElement m = Manifest;

        // the room: the table, carpet, walls and trim, each textured material built from its graph's committed maps
        var mats = new Dictionary<string, Material>();
        foreach (JsonProperty p in m.GetProperty("materials").EnumerateObject())
        {
            JsonElement e = p.Value, rep = e.GetProperty("repeat"), tint = e.GetProperty("tint");
            mats[p.Name] = GraphMaterial.Make(e.GetProperty("graph").GetString()!, new Color(tint[0].GetSingle(), tint[1].GetSingle(), tint[2].GetSingle()),
                new Vector2(rep[0].GetSingle(), rep[1].GetSingle()), e.GetProperty("normal_scale").GetSingle(), e.GetProperty("clearcoat").GetSingle());
        }
        Node3D room = GD.Load<PackedScene>(Dir + "room.glb").Instantiate<Node3D>();
        AddChild(room);
        GraphMaterial.Dress(room, name => mats.GetValueOrDefault(name));
        // the room throws no shadows: the table top lies 0.035 units under the sheet, too close for the lamp's shadow map to
        // tell apart at its range, and would shadow the chart in bands; only the pieces on the chart throw shadows on it
        foreach (Node n in room.FindChildren("*", "MeshInstance3D", true, false)) ((MeshInstance3D)n).CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        // the sheet: its still layers and, between them, the holders and the fog (chart_sheet.gdshader)
        JsonElement sh = m.GetProperty("sheet");
        int cols = c.S.Cols, rows = c.S.Rows;
        _ownerImg = Image.CreateEmpty(cols, rows, false, Image.Format.Rgba8);
        _fogImg = Image.CreateEmpty(cols, rows, false, Image.Format.Rgba8);
        _ownerTex = ImageTexture.CreateFromImage(_ownerImg);
        _fogTex = ImageTexture.CreateFromImage(_fogImg);
        JsonElement sb = sh.GetProperty("bounds");
        _sheet = new Rect2(sb[0].GetSingle(), sb[1].GetSingle(), sb[2].GetSingle() - sb[0].GetSingle(), sb[3].GetSingle() - sb[1].GetSingle());
        _maskImg = Image.CreateEmpty(Mathf.CeilToInt(_sheet.Size.X * MaskPx), Mathf.CeilToInt(_sheet.Size.Y * MaskPx), false, Image.Format.L8);
        _maskTex = ImageTexture.CreateFromImage(_maskImg);
        // the sheet's colour, live on the sheet (chart_sheet.gdshader) or, in the low-spec mode, baked into a texture each time
        // the chart changes (chart_sheet_bake.gdshader, the same code) and lit from it (chart_sheet_low.gdshader)
        var sheetMat = new ShaderMaterial { Shader = GD.Load<Shader>(LowSpec.On ? "res://src/Game/Flow/chart_sheet_bake.gdshader" : "res://src/Game/Flow/chart_sheet.gdshader") };
        foreach (string k in new[] { "base", "screen", "over", "mul" }) sheetMat.SetShaderParameter(k + "_tex", GD.Load<Texture2D>(Dir + $"sheet_{k}.png"));
        sheetMat.SetShaderParameter("owner_tex", _ownerTex);
        sheetMat.SetShaderParameter("fog_tex", _fogTex);
        sheetMat.SetShaderParameter("fog_mask", _maskTex);
        JsonElement b = sh.GetProperty("bounds"), pr = sh.GetProperty("print");
        sheetMat.SetShaderParameter("sheet_bounds", new Vector4(b[0].GetSingle(), b[1].GetSingle(), b[2].GetSingle(), b[3].GetSingle()));
        sheetMat.SetShaderParameter("print_bounds", new Vector4(pr[0].GetSingle(), pr[1].GetSingle(), pr[2].GetSingle(), pr[3].GetSingle()));
        sheetMat.SetShaderParameter("grid_size", new Vector2(cols, rows));
        if (LowSpec.On)
        {
            var px = new Vector2I(BakePx, Mathf.RoundToInt(BakePx * _sheet.Size.Y / _sheet.Size.X));
            _bake = new SubViewport { Name = "SheetBake", Size = px, TransparentBg = true, Disable3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled };
            _bake.AddChild(new ColorRect { Size = px, Material = sheetMat });
            AddChild(_bake);
            sheetMat = _lowMat = new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/Flow/chart_sheet_low.gdshader") };
        }
        Node3D paper = GD.Load<PackedScene>(Dir + "paper.glb").Instantiate<Node3D>();
        AddChild(paper);
        GraphMaterial.Dress(paper, name => name == "chart_sheet" ? sheetMat : null);
        // the sheet takes the pieces' shadows but throws none: lying almost flat under the lamp it would shadow itself in bands
        foreach (Node n in paper.FindChildren("*", "MeshInstance3D", true, false)) ((MeshInstance3D)n).CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        // low-spec: the sheet in tiles, so each lamp's extra pass (one a light an object, in the Compatibility renderer) draws
        // only the tiles in its reach, not the whole sheet under every sun's lamp
        if (LowSpec.On) foreach (Node n in paper.FindChildren("chart_sheet", "MeshInstance3D", true, false)) Tile((MeshInstance3D)n, SheetTiles);

        BuildLights(m.GetProperty("lights"));
        BuildDrawn(m, regions);
    }

    /// <summary>The room's environment: no sky, the dark of the room, a faint warm fill (the page's hemisphere light), the
    /// page's ACES tone mapping.</summary>
    public static Godot.Environment Environment()
    {
        JsonElement h = Manifest.GetProperty("lights").GetProperty("hemisphere");
        Color sky = new(h.GetProperty("sky").GetString()!), ground = new(h.GetProperty("ground").GetString()!);
        return new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("#050302"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = sky.Lerp(ground, 0.4f),
            AmbientLightEnergy = h.GetProperty("intensity").GetSingle() * LightScale,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
            // low-spec: no glow, as in its battle (on a Raspberry Pi 5 the glow's passes cost about a third of a frame)
            TonemapMode = Godot.Environment.ToneMapper.Aces, GlowEnabled = !LowSpec.On, GlowIntensity = 0.3f, GlowBloom = 0.02f,
        };
    }

    /// <summary>The page's lights are three.js r147 legacy intensities; Godot's energies for the same look are about this many
    /// times larger (set by eye against the mockup's shots).</summary>
    private const float LightScale = 2.4f;

    private void BuildLights(JsonElement l)
    {
        JsonElement sun = l.GetProperty("sun");
        var dir = new DirectionalLight3D { Name = "Fill", LightColor = new Color(sun.GetProperty("colour").GetString()!), LightEnergy = sun.GetProperty("intensity").GetSingle() * LightScale };
        AddChild(dir);
        dir.LookAtFromPosition(V(sun.GetProperty("position")), Vector3.Zero, Vector3.Up);
        foreach (JsonElement e in l.GetProperty("room").EnumerateArray())
        {
            Color col = new(e.GetProperty("colour").GetString()!);
            float energy = e.GetProperty("intensity").GetSingle() * LightScale;
            Vector3 at = V(e.GetProperty("position"));
            if (e.GetProperty("kind").GetString() == "spot")
            {
                // the lamp over the table: its pool on the chart, the pieces' shadows thrown onto the paper
                Vector3 to = V(e.GetProperty("target"));
                var lamp = new SpotLight3D
                {
                    Name = "Lamp", LightColor = col, LightEnergy = energy, SpotRange = at.DistanceTo(to) * 3, SpotAttenuation = 0,
                    SpotAngle = Mathf.RadToDeg(e.GetProperty("angle_rad").GetSingle()), SpotAngleAttenuation = 1.2f - e.GetProperty("penumbra").GetSingle(),   // the page's penumbra: its outer part fading
                    ShadowEnabled = !LowSpec.On, ShadowBias = 0.08f, ShadowNormalBias = 1.5f,   // low-spec: no shadow pass, as in its battle
                };
                AddChild(lamp);
                lamp.LookAtFromPosition(at, to, Vector3.Up);
            }
            else AddChild(new OmniLight3D { Name = "Sconce", LightColor = col, LightEnergy = energy, Position = at, OmniRange = e.GetProperty("range").GetSingle(), OmniAttenuation = e.GetProperty("decay").GetSingle() });
        }
    }

    // The drawn pieces on this map, by the page's rules (docs/mockups/game-flow.html buildMap, its paper branch: the same
    // Mulberry32 numbers drawn in the same order, so a sector gets what the page would give it): a puff of cloud at the back
    // of each nebula sector with its shadow on the paper, and in three sectors of five a smaller puff in front, from the bank
    // of drawings by the cloud's palette colour (one cloud in three a storm); a sun on each star with its warm light; half a
    // belt of drawn rocks across each asteroid sector. Billboards face the camera; the shadows lie on the paper.
    private void BuildDrawn(JsonElement m, Dictionary<(int, int), (Color Main, Color Accent, int MainIdx, int Id)> regions)
    {
        var tex = new Dictionary<string, (Texture2D Tex, float Foot)>();
        foreach (JsonProperty p in m.GetProperty("drawn").EnumerateObject())
            tex[p.Name] = (GD.Load<Texture2D>(Dir + p.Value.GetProperty("file").GetString()), p.Value.GetProperty("foot").GetSingle());
        Color tint = new(m.GetProperty("sprite_tint").GetString()!);
        void Put(Node3D n, Sector h) { AddChild(n); _fogged.Add((n, h.C, h.R)); }
        // each drawing's instances: a camera-facing quad w units wide, its foot (the drawing's lowest point) on the position
        var bills = new Dictionary<string, List<(Transform3D At, int C, int R)>>();
        void Bill(string id, float w, Vector3 at, Sector h)
        {
            if (!bills.TryGetValue(id, out var list)) bills[id] = list = new();
            list.Add((new Transform3D(Basis.Identity.Scaled(Vector3.One * w), at), h.C, h.R));
        }
        var shadows = new List<(Transform3D At, int C, int R)>();
        float Rf(Mulberry32 r) => (float)r.Next();
        foreach (Sector h in _c.S.Sectors)
        {
            Vector3 p = CampaignMap.HexPos(h.C, h.R);
            if (h.Terrain == "nebula" && regions.TryGetValue((h.C, h.R), out var reg))
            {
                var R = new Mulberry32((uint)(h.C * 17 + h.R * 5));
                int v = (int)Math.Floor(R.Next() * 3);
                string kind = reg.Id % 3 == 2 ? "storm" : "cloud";
                float w = 1.2f + Rf(R) * 0.3f;
                float x = p.X + (Rf(R) - 0.5f) * 0.4f, z = p.Z - 0.25f + (Rf(R) - 0.5f) * 0.2f;
                Bill($"{kind}:{reg.MainIdx}:{v}", w, new Vector3(x, 0.06f, z), h);
                float sx = p.X + (Rf(R) - 0.5f) * 0.15f;
                shadows.Add((new Transform3D(Basis.Identity, new Vector3(sx, 0.033f, p.Z + 0.05f)), h.C, h.R));
                if (R.Next() < 0.6)
                {
                    int v2 = (v + 1) % 3;
                    float w2 = 0.75f + Rf(R) * 0.2f;
                    float side = R.Next() < 0.5 ? -1 : 1;
                    float x2 = p.X + side * (0.3f + Rf(R) * 0.15f), z2 = p.Z + 0.12f + Rf(R) * 0.15f;
                    Bill($"{kind}:{reg.MainIdx}:{v2}", w2, new Vector3(x2, 0.06f, z2), h);
                }
            }
            else if (h.Terrain == "star")
            {
                Bill("sun:" + (h.C * 7 + h.R) % 3, 2.0f, new Vector3(p.X, 0.02f, p.Z - 0.15f), h);
                // the sun's warm light on the pieces round it (the page's PointLight beside each drawn sun)
                var lamp = new OmniLight3D { LightColor = new Color("#ffc26a"), LightEnergy = 0.8f * LightScale, OmniRange = 3f, Position = new Vector3(p.X, 0.6f, p.Z) };
                Put(lamp, h);
            }
            else if (h.Terrain == "asteroids")
            {
                // a belt along a curved band across the hex, the big rocks in the middle of the band; on paper half of them
                var R = new Mulberry32((uint)(h.C * 31 + h.R));
                double a0 = R.Next() * Math.PI, rad = 0.9 + R.Next() * 0.5;
                double cx = p.X - Math.Cos(a0) * rad, cz = p.Z - Math.Sin(a0) * rad;
                for (int k = 0; k < 34; k++)
                {
                    double u = (R.Next() - 0.5) * 1.1, off = (R.Next() - 0.5) * 0.28, rr = rad + off;
                    double big = 0.025 + R.Next() * (0.07 - Math.Abs(off) * 0.15);
                    int rv = (int)Math.Floor(R.Next() * 12);
                    if (k % 2 == 1) continue;
                    var at = new Vector3((float)(cx + Math.Cos(a0 + u) * rr), (float)(0.03 + R.Next() * 0.06), (float)(cz + Math.Sin(a0 + u) * rr));
                    if (new Vector2(at.X - p.X, at.Z - p.Z).Length() > 0.85f) continue;
                    Bill("rock:" + rv, (float)(big * 5.5), at, h);
                }
            }
        }
        foreach (string id in bills.Keys.Order())
        {
            var (t, foot) = tex[id];
            float aspect = (float)t.GetHeight() / t.GetWidth();
            var quad = new QuadMesh { Size = new Vector2(1, aspect), CenterOffset = new Vector3(0, aspect * (foot - 0.5f), 0) };
            var mat = new StandardMaterial3D
            {
                AlbedoTexture = t, AlbedoColor = tint, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled, BillboardKeepScale = true,
                Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor, AlphaScissorThreshold = 0.5f,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled, TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
            };
            Batch(quad, mat, bills[id], id);
        }
        var shadowMat = new StandardMaterial3D { AlbedoTexture = tex["cloud-shadow"].Tex, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
        Batch(new PlaneMesh { Size = new Vector2(1.75f, 1.55f) }, shadowMat, shadows, "cloud-shadow");
    }

    private void Batch(Mesh mesh, Material mat, List<(Transform3D At, int C, int R)> list, string name)
    {
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = mesh, InstanceCount = list.Count };
        for (int i = 0; i < list.Count; i++)
        {
            mm.SetInstanceTransform(i, list[i].At);
            _fogInst.Add((mm, i, list[i].At, list[i].C, list[i].R));
        }
        AddChild(new MultiMeshInstance3D { Name = "Drawn " + name, Multimesh = mm, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
    }

    /// <summary>Redraws what changes in play: each sector's holder as last known and its fog (uncharted, seen before, seen
    /// now), and the drawn pieces hidden in the uncharted.</summary>
    public void Update(HashSet<(int C, int R)> vis)
    {
        foreach (Sector h in _c.S.Sectors)
        {
            string owner = h.KnownOwner;
            Color ink = _facColor(owner);
            _ownerImg.SetPixel(h.C, h.R, new Color(ink.R, ink.G, ink.B, owner == "neutral" ? 0 : 1));
            float fog = !h.Known ? 0 : vis.Contains((h.C, h.R)) ? 1 : 0.5f;
            _fogImg.SetPixel(h.C, h.R, new Color(fog, 0, 0, 1));
        }
        _ownerTex.Update(_ownerImg);
        _fogTex.Update(_fogImg);
        FogMask(vis);
        foreach (var (n, cc, rr) in _fogged) n.Visible = _c.OnMap(cc, rr) && _c.Hex(cc, rr).Known;
        Rebake();
        var none = new Transform3D(Basis.Identity.Scaled(Vector3.Zero), Vector3.Zero);
        foreach (var (mm, i, at, cc, rr) in _fogInst) mm.SetInstanceTransform(i, _c.OnMap(cc, rr) && _c.Hex(cc, rr).Known ? at : none);
    }

    // cuts a mesh into n x n meshes by where each triangle's middle falls across its box (the same vertices, normals, uvs and
    // material, so it looks the same), each its own node beside the original, which goes
    private static void Tile(MeshInstance3D mi, int n)
    {
        if (mi.Mesh is not { } mesh || mesh.GetSurfaceCount() != 1) return;
        Godot.Collections.Array a = mesh.SurfaceGetArrays(0);
        Vector3[] pos = a[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        int[] idx = a[(int)Mesh.ArrayType.Index].AsInt32Array();
        if (idx.Length == 0) idx = Enumerable.Range(0, pos.Length).ToArray();
        Aabb box = mesh.GetAabb();
        // the sheet lies in the box's two widest axes
        int u = box.Size.X >= box.Size.Z || box.Size.X >= box.Size.Y ? 0 : 1, v = box.Size.Z >= box.Size.Y ? 2 : 1;
        if (v == u) v = u == 0 ? 2 : 0;
        var buckets = new List<int>[n * n];
        for (int t = 0; t + 2 < idx.Length; t += 3)
        {
            Vector3 c = (pos[idx[t]] + pos[idx[t + 1]] + pos[idx[t + 2]]) / 3 - box.Position;
            int bu = Math.Clamp((int)(c[u] / Math.Max(box.Size[u], 1e-6f) * n), 0, n - 1), bv = Math.Clamp((int)(c[v] / Math.Max(box.Size[v], 1e-6f) * n), 0, n - 1);
            (buckets[bv * n + bu] ??= new()).Add(t);
        }
        Material? mat = mi.MaterialOverride ?? mi.GetSurfaceOverrideMaterial(0) ?? mesh.SurfaceGetMaterial(0);
        Node parent = mi.GetParent();
        for (int b = 0; b < buckets.Length; b++)
        {
            if (buckets[b] is not { } tris) continue;
            var remap = new Dictionary<int, int>();
            var order = new List<int>();
            var ix = new int[tris.Count * 3];
            for (int k = 0; k < tris.Count; k++)
                for (int j = 0; j < 3; j++)
                {
                    int o = idx[tris[k] + j];
                    if (!remap.TryGetValue(o, out int nw)) { remap[o] = nw = order.Count; order.Add(o); }
                    ix[k * 3 + j] = nw;
                }
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            if (a[(int)Mesh.ArrayType.Vertex].VariantType != Variant.Type.Nil) arrays[(int)Mesh.ArrayType.Vertex] = order.Select(o => pos[o]).ToArray();
            if (a[(int)Mesh.ArrayType.Normal].VariantType != Variant.Type.Nil) { var src = a[(int)Mesh.ArrayType.Normal].AsVector3Array(); arrays[(int)Mesh.ArrayType.Normal] = order.Select(o => src[o]).ToArray(); }
            if (a[(int)Mesh.ArrayType.Tangent].VariantType != Variant.Type.Nil) { var src = a[(int)Mesh.ArrayType.Tangent].AsFloat32Array(); arrays[(int)Mesh.ArrayType.Tangent] = order.SelectMany(o => new[] { src[o * 4], src[o * 4 + 1], src[o * 4 + 2], src[o * 4 + 3] }).ToArray(); }
            if (a[(int)Mesh.ArrayType.TexUV].VariantType != Variant.Type.Nil) { var src = a[(int)Mesh.ArrayType.TexUV].AsVector2Array(); arrays[(int)Mesh.ArrayType.TexUV] = order.Select(o => src[o]).ToArray(); }
            arrays[(int)Mesh.ArrayType.Index] = ix;
            var am = new ArrayMesh();
            am.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            parent.AddChild(new MeshInstance3D { Name = $"{mi.Name}_tile{b}", Mesh = am, MaterialOverride = mat, Transform = mi.Transform, CastShadow = mi.CastShadow });
        }
        parent.RemoveChild(mi);
        mi.QueueFree();
    }

    // the low-spec mode's sheet: drawn into the bake viewport in this frame, then read back with its mipmaps once it is drawn
    private void Rebake()
    {
        if (_bake == null || _baking) return;
        _baking = true;
        _bake.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        RenderingServer.FramePostDraw += Grab;
    }
    private bool _baking;
    private void Grab()
    {
        RenderingServer.FramePostDraw -= Grab;
        _baking = false;
        if (_bake == null || !IsInstanceValid(_bake) || _lowMat == null) return;
        Image img = _bake.GetTexture().GetImage();
        img.GenerateMipmaps();
        if (_baked == null || _baked.GetSize() != img.GetSize()) { _baked = ImageTexture.CreateFromImage(img); _lowMat.SetShaderParameter("baked", _baked); }
        else _baked.Update(img);
    }

    // The fog's mask, drawn once each time the fog changes rather than in every pixel every frame (a phone holds 60 fps): flat
    // black over the uncharted and off the map, a 0.45 veil over the revealed, nothing over what is seen now, then blurred
    // so its border melts into the charted space (the page's fog mask and its blur).
    private void FogMask(HashSet<(int C, int R)> vis)
    {
        int w = _maskImg.GetWidth(), h = _maskImg.GetHeight();
        var m = new float[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                double px = _sheet.Position.X + (x + 0.5) / MaskPx, pz = _sheet.Position.Y + (y + 0.5) / MaskPx;
                var (c, r) = HexGrid.At(px, pz);
                m[y * w + x] = !_c.OnMap(c, r) || !_c.Hex(c, r).Known ? 1f : vis.Contains((c, r)) ? 0f : 0.45f;
            }
        var tmp = new float[w * h];
        for (int pass = 0; pass < 2; pass++)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float s = 0; int n = 0;
                    for (int d = -MaskBlur; d <= MaskBlur; d++) { int xx = x + d; if (xx >= 0 && xx < w) { s += m[y * w + xx]; n++; } }
                    tmp[y * w + x] = s / n;
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float s = 0; int n = 0;
                    for (int d = -MaskBlur; d <= MaskBlur; d++) { int yy = y + d; if (yy >= 0 && yy < h) { s += tmp[yy * w + x]; n++; } }
                    m[y * w + x] = s / n;
                }
        }
        var bytes = new byte[w * h];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)Math.Round(Math.Clamp(m[i], 0, 1) * 255);
        _maskImg.SetData(w, h, false, Image.Format.L8, bytes);
        _maskTex.Update(_maskImg);
    }

    // ---- The planets and the names -----------------------------------------------------------------------------------

    private static ImageTexture? _lattice;
    /// <summary>The planet shader's noise lattice: 256 x 256 Mulberry32 values from seed 0x5eed1e57, red the value, green the
    /// value 37 across and 17 down (docs/mockups/lib/planet-shader.js latticeTex, the same numbers).</summary>
    public static ImageTexture Lattice
    {
        get
        {
            if (_lattice != null) return _lattice;
            const int N = 256;
            var R = new Mulberry32(0x5eed1e57);
            var v = new byte[N * N];
            for (int i = 0; i < v.Length; i++) v[i] = (byte)Math.Floor(R.Next() * 256);
            var data = new byte[N * N * 4];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    int i = (y * N + x) * 4;
                    data[i] = v[y * N + x];
                    data[i + 1] = v[((y - 17 + N) % N) * N + (x - 37 + N) % N];
                    data[i + 3] = 255;
                }
            return _lattice = ImageTexture.CreateFromImage(Image.CreateFromData(N, N, false, Image.Format.Rgba8, data));
        }
    }

    /// <summary>The page's strSeed: FNV-1a over the string's UTF-16 code units.</summary>
    public static uint StrSeed(string s)
    {
        uint h = 2166136261;
        unchecked { foreach (char ch in s) h = (h ^ ch) * 16777619; }
        return h;
    }

    private static readonly string[] Kinds = { "terran", "desert", "ice", "lava", "gas", "moon" };
    private static Vector3 Rgb(string hex) { Color c = new(hex); return new Vector3(c.R, c.G, c.B); }

    /// <summary>A planet on the chart: the planet shader on a camera-facing quad, its disc <paramref name="disc"/> units
    /// across, ringed in its holder's colour (the page's FCPlanet.material and billboard; its kind from the manifest, a bare
    /// moon where none is named). The node's origin is the quad's foot.</summary>
    public static (Node3D Node, ShaderMaterial Mat) Planet(string name, Color holder, float disc)
    {
        JsonElement pl = Manifest.GetProperty("planets");
        string kind = pl.GetProperty("kinds").TryGetProperty(name, out JsonElement k) ? k.GetString()! : "moon";
        JsonElement K = pl.GetProperty("palettes").GetProperty(kind);
        string[] Arr(string key) => K.TryGetProperty(key, out JsonElement a) && a.ValueKind == JsonValueKind.Array ? a.EnumerateArray().Select(x => x.GetString()!).ToArray() : Array.Empty<string>();
        string? Str(string key) => K.TryGetProperty(key, out JsonElement a) && a.ValueKind == JsonValueKind.String ? a.GetString() : null;
        float Num(string key, float d) => K.TryGetProperty(key, out JsonElement a) && a.ValueKind == JsonValueKind.Number ? a.GetSingle() : d;
        bool Flag(string key) => K.TryGetProperty(key, out JsonElement a) && a.ValueKind == JsonValueKind.True;
        string[] g = Arr("ground").Length > 0 ? Arr("ground") : Arr("bands"), s = Arr("sea").Length > 0 ? Arr("sea") : g;
        string[] bands = Arr("bands").Length > 0 ? Arr("bands") : g.Concat(g.Take(2)).ToArray();
        var R = new Mulberry32(StrSeed(name));
        float seed = (float)(R.Next() * 40), tilt = (float)((R.Next() - 0.5) * 0.7);
        var mat = new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/Flow/planet.gdshader") };
        void P(string n, Variant v) => mat.SetShaderParameter(n, v);
        P("lat", Lattice); P("pixels", pl.GetProperty("pixels").GetSingle()); P("steps", pl.GetProperty("steps").GetSingle());
        P("seed", seed); P("tilt", tilt); P("kind", (float)Array.IndexOf(Kinds, kind)); P("ring", 1f);
        P("clouds", Num("clouds", 0)); P("land", Num("land", 0.5f)); P("caps", Flag("caps") ? 1f : 0f);
        for (int i = 0; i < 4; i++) { P("g" + i, Rgb(g[i])); P("s" + i, Rgb(s[i])); }
        for (int i = 0; i < 6; i++) P("b" + i, Rgb(bands[i % bands.Length]));
        P("cloud_col", Rgb(Str("cloud") ?? "#f4efe2"));
        P("atmo", Str("atmo") is { } at ? Rgb(at) : Vector3.Zero);
        P("glow_col", Str("glow") is { } gl ? Rgb(gl) : Vector3.Zero);
        P("ring_col", new Vector3(holder.R, holder.G, holder.B));
        float w = 2 * disc;
        P("width", w);
        var quad = new MeshInstance3D { Name = "Planet", Mesh = new QuadMesh { Size = Vector2.One }, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            ExtraCullMargin = w * 2 };
        return (quad, mat);
    }

    private static FontFile? _ink;
    /// <summary>A name on the chart, hand-lettered: sepia ink in IM FELL English SC with a halo of paper round it (the page's
    /// inkLabel). It keeps its size on screen.</summary>
    public static Label3D InkLabel(string text) => new()
    {
        Text = text, Modulate = new Color("#2a1b10"), FontSize = 50, OutlineSize = 9, OutlineModulate = new Color(236 / 255f, 224 / 255f, 198 / 255f, 0.94f),
        FixedSize = true, PixelSize = 0.00036f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, NoDepthTest = true, RenderPriority = 4, OutlineRenderPriority = 3,
        Font = _ink ??= GD.Load<FontFile>("res://assets/fonts/IMFellEnglishSC-Regular.ttf"),
    };
}
