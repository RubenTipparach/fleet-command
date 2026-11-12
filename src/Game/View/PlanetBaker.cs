// PlanetBaker: every planet kind baked to textures, for the low-spec mode on a machine whose GPU cannot compile the planet
// shaders (owner, 2026-11-12: "planets should be textures, have the mac render some planets as textures to be cached and
// used for pi only"; the Raspberry Pi 5's V3D compiler hangs on them). Run as the game with -- --planet-bake=<folder>
// (tools/space/bake_planets.sh), in a window. Each kind is built by PlanetBody, the battle's own builder, with one fixed seed;
// each of its layers is drawn by its own shader compiled with PLANET_BAKE (planet_noise.gdshaderinc), which draws the
// sphere straight into its UVs, lit from overhead, so the maps come from the real shaders and nothing is painted twice:
// - <kind>_ground.png: the ground (a gas giant's tops), times the crater shell where it has one; RGB, 2:1 equirectangular
//   on SphereMesh's own UVs (a SphereMesh with this map shows it where the battle's sphere shows the shader);
// - <kind>_clouds.png: the cloud layer, RGBA, the same map, for a second sphere at the manifest's cloud_radius;
// - <kind>_rings.png: the rings, RGBA, one row from ring_inner (left) to ring_outer (right), in the planet's radii;
// - manifest.json: per kind its seed, layers, sizes, the radii, and the commit of the shaders baked from.
// --planet-bake-check=<folder> also saves each kind's shader planet beside a sphere wearing its map (the placement check).
// Prints BAKE <file> per map and BAKE_END, then quits. A tool: the battle never opens it.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class PlanetBaker : Node
{
    /// <summary>The map size of the ground and clouds (2:1, longitude across) and of the rings' strip.</summary>
    public const int MapW = 1024, MapH = 512, RingW = 1024;
    /// <summary>The noise octaves and closeness a map is drawn at: the detail a 1024 px map can hold.</summary>
    public const float Octaves = 8f, Close = 0.3f;

    /// <summary>The folder the maps go into, and the commit of the shaders (for the manifest).</summary>
    public required string Out;
    public string Commit = "";
    /// <summary>Where to save each kind's check (empty: none): the shader's planet beside a SphereMesh wearing its ground
    /// map, from the same camera, so the map's placement on the sphere can be seen to match.</summary>
    public string CheckDir = "";
    /// <summary>Bake only these kinds (empty: every kind).</summary>
    public string[] Only = Array.Empty<string>();

    private SubViewport _vp = null!;
    private Node3D _stage = null!;
    private Godot.Environment _env = null!;
    private readonly Dictionary<Shader, Shader> _baked = new();

    public override void _Ready() => _ = Guarded();

    // a fault in the bake is printed and ends the run (an async task's exception would otherwise vanish and leave the
    // window open)
    private async Task Guarded()
    {
        try { await Run(); }
        catch (Exception e) { GD.PrintErr($"BAKE_FAILED {e}"); GetTree().Quit(1); }
    }

    private async Task Run()
    {
        string dir = (Out.StartsWith("res://") ? Out : "res://" + Out).TrimEnd('/');
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(dir));
        if (CheckDir.Length > 0) DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://" + CheckDir));
        _env = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color, TonemapMode = Godot.Environment.ToneMapper.Linear };
        _vp = new SubViewport { OwnWorld3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled, Msaa3D = Viewport.Msaa.Msaa4X };
        AddChild(_vp);
        _vp.AddChild(new WorldEnvironment { Environment = _env });
        var cam = new Camera3D { Current = true, Position = new Vector3(0, 0, 6) };
        _vp.AddChild(cam);
        _stage = new Node3D();
        _vp.AddChild(_stage);

        var manifest = new JsonObject { ["_doc"] = "Planet maps baked by src/Game/View/PlanetBaker.cs (tools/space/bake_planets.sh) from the planet shaders at the commit below, for the low-spec mode. Ground and clouds: 2:1 equirectangular on the UVs of a SphereMesh, lit from overhead (no sun). Rings: one row, ring_inner on the left to ring_outer on the right, in planet radii.", ["shaders_commit"] = Commit, ["map_px"] = new JsonArray(MapW, MapH), ["ring_px"] = RingW };
        var kinds = new JsonObject();
        foreach (string kind in FleetCommand.Data.PlanetDef.Kinds.Where(k => Only.Length == 0 || Only.Contains(k)))
        {
            Vector3 seed = PlanetLooks.SeedOf("bake-" + kind);
            PlanetBody p = PlanetBody.Build(kind, kind, seed);
            p.Sync(Vector3.Up, 1, 1);
            var entry = new JsonObject { ["seed"] = new JsonArray(seed.X, seed.Y, seed.Z), ["atmosphere"] = p.Look.Atmosphere.ToHtml(false), ["atmosphere_strength"] = p.Look.AtmosphereStrength, ["atmosphere_height"] = p.Look.AtmosphereHeight };
            var layers = new JsonArray();

            // the ground, and the crater shell multiplied over it (as the battle blends it)
            Image ground = await Layer(Layer(p, "Ground"), new SphereMesh { Radius = 1, Height = 2, RadialSegments = 512, Rings = 256 }, MapW, MapH, Colors.Black, false);
            if (p.Body.GetNodeOrNull<MeshInstance3D>("Craters") is { } cr)
            {
                Image craters = await Layer(Bake((ShaderMaterial)cr.MaterialOverride), new SphereMesh { Radius = 1.004f, Height = 2.008f, RadialSegments = 512, Rings = 256 }, MapW, MapH, Colors.White, false);
                Multiply(ground, craters);
                entry["craters"] = true;
            }
            Save(ground, $"{dir}/{kind}_ground.png");
            layers.Add("ground");

            if (p.Body.GetNodeOrNull<MeshInstance3D>("Clouds") is { } cl)
            {
                float r = ((SphereMesh)cl.Mesh).Radius;
                Image clouds = await Layer(Bake((ShaderMaterial)cl.MaterialOverride), new SphereMesh { Radius = r, Height = 2 * r, RadialSegments = 512, Rings = 256 }, MapW, MapH, new Color(0, 0, 0, 0), true);
                Save(clouds, $"{dir}/{kind}_clouds.png");
                layers.Add("clouds");
                entry["cloud_radius"] = r;
            }
            if (p.Body.GetNodeOrNull<MeshInstance3D>("Rings") is { } rg && p.Look.Rings is { } rl)
            {
                Image plane = await Layer(Bake((ShaderMaterial)rg.MaterialOverride), rg.Mesh, 2048, 2048, new Color(0, 0, 0, 0), true);
                Save(RingStrip(plane, rl.Inner, rl.Outer), $"{dir}/{kind}_rings.png");
                layers.Add("rings");
                entry["ring_inner"] = rl.Inner;
                entry["ring_outer"] = rl.Outer;
            }
            if (CheckDir.Length > 0) await Check(p, ground, $"{CheckDir.TrimEnd('/')}/{kind}_check.png");
            entry["layers"] = layers;
            kinds[kind] = entry;
            p.Root.Free();
        }
        manifest["kinds"] = kinds;
        using (FileAccess f = FileAccess.Open($"{dir}/manifest.json", FileAccess.ModeFlags.Write))
            f.StoreString(manifest.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true, TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver() }) + "\n");
        GD.Print("BAKE_END");
        GetTree().Quit();
    }

    private ShaderMaterial Layer(PlanetBody p, string name) => Bake((ShaderMaterial)p.Body.GetNode<MeshInstance3D>(name).MaterialOverride);

    // the layer's material with its shader compiled for the bake, every parameter carried across
    private ShaderMaterial Bake(ShaderMaterial src)
    {
        if (!_baked.TryGetValue(src.Shader, out Shader? sh))
        {
            string code = src.Shader.Code;
            int line = code.IndexOf('\n', code.IndexOf("shader_type", StringComparison.Ordinal));
            _baked[src.Shader] = sh = new Shader { Code = code[..(line + 1)] + "#define PLANET_BAKE\n" + code[(line + 1)..] };
        }
        var m = new ShaderMaterial { Shader = sh };
        var names = src.Shader.GetShaderUniformList().Select(u => (string)u.AsGodotDictionary()["name"]).ToList();
        foreach (string n in names) m.SetShaderParameter(n, src.GetShaderParameter(n));
        if (names.Contains("octaves")) m.SetShaderParameter("octaves", Octaves);
        if (names.Contains("close")) m.SetShaderParameter("close", Close);
        return m;
    }

    // one layer drawn into its UVs at w x h over a background
    private async Task<Image> Layer(ShaderMaterial mat, Mesh mesh, int w, int h, Color bg, bool alpha)
    {
        foreach (Node c in _stage.GetChildren()) c.Free();
        _stage.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = mat, ExtraCullMargin = 100 });
        _vp.Size = new Vector2I(w, h);
        _vp.TransparentBg = alpha;
        _env.BackgroundColor = bg;
        _vp.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        for (int i = 0; i < 4; i++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Image img = _vp.GetTexture().GetImage();
        img.Convert(alpha ? Image.Format.Rgba8 : Image.Format.Rgb8);
        return img;
    }

    // the check: the shader's planet (lit from the camera), then a SphereMesh wearing the map, each alone in the same place
    private async Task Check(PlanetBody p, Image ground, string path)
    {
        var shots = new List<Image>();
        p.Sync(new Vector3(0, 0.3f, 1).Normalized(), 1, 1);
        var mat = new StandardMaterial3D { AlbedoTexture = ImageTexture.CreateFromImage(ground), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        foreach (MeshInstance3D m in new[] { (MeshInstance3D)p.Body.GetNode<MeshInstance3D>("Ground").Duplicate(),
            new MeshInstance3D { MaterialOverride = mat, Mesh = new SphereMesh { Radius = 1, Height = 2, RadialSegments = 128, Rings = 64 } } })
        {
            foreach (Node c in _stage.GetChildren()) c.Free();
            _stage.AddChild(m);
            _vp.GetCamera3D().Position = new Vector3(0, 0, 2.6f);
            _vp.Size = new Vector2I(512, 512);
            _vp.TransparentBg = false;
            _env.BackgroundColor = Colors.Black;
            _vp.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
            for (int i = 0; i < 4; i++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Image img = _vp.GetTexture().GetImage();
            img.Convert(Image.Format.Rgb8);
            shots.Add(img);
        }
        var both = Image.CreateEmpty(1024, 512, false, Image.Format.Rgb8);
        both.BlitRect(shots[0], new Rect2I(0, 0, 512, 512), Vector2I.Zero);
        both.BlitRect(shots[1], new Rect2I(0, 0, 512, 512), new Vector2I(512, 0));
        Save(both, "res://" + path);
        _vp.GetCamera3D().Position = new Vector3(0, 0, 6);
    }

    private static void Multiply(Image a, Image b)
    {
        for (int y = 0; y < a.GetHeight(); y++)
            for (int x = 0; x < a.GetWidth(); x++)
            {
                Color c = a.GetPixel(x, y), k = b.GetPixel(x, y);
                a.SetPixel(x, y, new Color(c.R * k.R, c.G * k.G, c.B * k.B));
            }
    }

    // the ring plane's top view sampled along eight radii from inner to outer and averaged: one row of RingW
    private static Image RingStrip(Image plane, float inner, float outer)
    {
        var strip = Image.CreateEmpty(RingW, 1, false, Image.Format.Rgba8);
        int w = plane.GetWidth(), h = plane.GetHeight();
        for (int i = 0; i < RingW; i++)
        {
            float r = inner + (outer - inner) * (i + 0.5f) / RingW;
            var sum = new Color(0, 0, 0, 0);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.Tau / 8 + 0.2f;
                float u = 0.5f + r * Mathf.Cos(a) / (2 * outer), v = 0.5f + r * Mathf.Sin(a) / (2 * outer);
                Color c = plane.GetPixel(Math.Clamp((int)(u * w), 0, w - 1), Math.Clamp((int)(v * h), 0, h - 1));
                sum += c;
            }
            strip.SetPixel(i, 0, sum / 8);
        }
        return strip;
    }

    private static void Save(Image img, string path)
    {
        img.SavePng(ProjectSettings.GlobalizePath(path));
        GD.Print($"BAKE {path}");
    }
}
