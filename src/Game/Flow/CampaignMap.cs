// CampaignMap: the campaign's hex map in 3D, built as the campaign mockup builds it (docs/mockups/game-flow.html v15:
// buildMap, makeSpace, nebulaRegions, puffSet, hexBorders, makeStandee, buildFreight, showPath). The owner's reference is
// a box-back star map: dark glass hexes over the galaxy, each sector's contents inside its hex, territory told by bright
// ribbons along the borders where the holder changes. Behind it the "quiet" nebula sky with its stars, and three layers of
// stars under the board that slide with parallax as the view pans. Nebulae are billboard puffs (puff.gdshader), one main
// colour per connected cloud from the owner's palette with an accent rising through its upper puffs; asteroid sectors hold
// a curved belt of rocks; stars burn in theirs. The pieces stand on flight posts: planets and stations, the factions'
// fleets facing their last move with a strength flag, the player's fleet on gold, your freighters on their dashed routes
// (and your allies' routes), and every mission's cloth banner. A route shows each turn's leg, later ones fainter, a
// numbered badge where each turn ends. Fleets fly their moves sector by sector, lifting off their stands.
// The camera is the player's (CLAUDE.md section 8): drag pans, the wheel and the +/- keys zoom, its angle is fixed (owner,
// 2026-10-30: the map must not turn); it moves on its own only when the player asks (the Fleet key, a mission in the log).
// It draws campaign state and holds no rules: everything it shows is read from Campaign (src/Core/Campaign).
// Since 2026-11-08 the map is the paper chart (ChartRoom; owner: "implement space chart in game, it looks great!"): a printed
// star chart on a table, its nebulae, suns and belts drawn by hand, its planets drawn by planet.gdshader, its names in ink.
// The glass board above stays as the space look (Paper = false), as the mockup keeps it behind ?look=space.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Campaign;
using FleetCommand.Game.View;
using Godot;

namespace FleetCommand.Game.Flow;

public sealed partial class CampaignMap : Node3D
{
    private Campaign.Campaign _c = null!;
    private Camera3D _cam = null!;
    private Node3D _board = null!, _pieces = null!, _routes = null!, _path = null!;
    private MeshInstance3D _ring = null!, _hover = null!;
    private NebulaSky? _sky;
    private MultiMeshInstance3D _puffs = null!;
    private readonly List<(Vector3 Pos, Vector2 Size, float Seed, Color Tint)> _puffList = new();
    private Vector3 _puffEye = new(1e9f, 0, 0);
    private readonly List<Label3D> _labels = new();
    private readonly List<Node3D> _spin = new();
    // the fog of war as last drawn: the sectors seen, and a key of what was known and seen, so the board is rebuilt only
    // when the fog changes
    private HashSet<(int C, int R)> _vis = new();
    private string _fogKey = "";
    private readonly Dictionary<string, Piece> _fleets = new();
    private float _time;
    /// <summary>The map's look: the paper chart on its table (true), or the glass board over the nebula (false).</summary>
    public static bool Paper = true;
    private ChartRoom? _chart;
    private readonly List<ShaderMaterial> _planets = new();
    private (Vector3 From, Vector3 To, float T)? _camGlide;

    /// <summary>A piece that flies: its node, its model (turned as it flies), the points it flies through, its heading at the end.</summary>
    private sealed class Piece
    {
        public required Node3D Node;
        public Node3D? Model;
        public List<Vector3>? Pts;
        public float T;
        public float EndDir;
        public float Lift;
        /// <summary>The model's heading when its flight began (radians), where its first turn starts from.</summary>
        public float Yaw0;
    }

    /// <summary>The camera: the point it looks at on the map plane and its distance. Its angle is fixed (the mockup's polar
    /// 0.88 rad), and it never turns.</summary>
    public Vector3 Target;
    public float Dist = 18;
    public const float Polar = 0.88f, MinDist = 7, MaxDist = 70;
    /// <summary>Sectors a second a fleet flies (the mockup's FLIGHT_SECTORS_S).</summary>
    public const float FlightSectorsS = 2.6f;
    /// <summary>Seconds a fleet takes to turn half round on the spot before it moves on (owner, 2026-11-07: "turn first,
    /// then move, not turn and move at the same time"); a smaller turn takes its share.</summary>
    public const float FlightTurnS = 0.5f;
    /// <summary>Beyond this distance the name labels leave (they would pile on one another); the pieces never do.</summary>
    public const float FarDist = 34;

    private static readonly Dictionary<string, int> PlanetHue = new() { ["Halcyon"] = 205, ["Meridian"] = 160, ["Vahr"] = 12, ["Ouro"] = 40, ["Tessaly"] = 280 };
    /// <summary>The owner's nebula palette (2026-11-05), each cloud a main colour and an accent from it.</summary>
    private static readonly string[] Palette =
    {
        "#6f6776", "#9a9a97", "#c5ccb8", "#8b5580", "#c38890", "#a593a5", "#666092", "#9a4f50", "#c28d75", "#7ca1c0", "#416aa3",
        "#8d6268", "#be955c", "#68aca9", "#387080", "#6e6962", "#93a167", "#6eaa78", "#557064", "#9d9f7f", "#7e9e99", "#5d6872", "#433455",
    };

    public static Vector3 HexPos(int c, int r) { var (x, z) = HexGrid.Pos(c, r); return new Vector3((float)x, 0, (float)z); }

    public Color FacColor(string fac) => _c.Data.Factions.TryGetValue(fac, out CampFaction? f) ? new Color(f.Color) : new Color("#7c8794");

    private float MapW => 1.5f * (_c.S.Cols - 1);
    private float MapH => Mathf.Sqrt(3) * (_c.S.Rows - 0.5f);

    public Camera3D Camera => _cam;

    public void Init(Campaign.Campaign c)
    {
        _c = c;
        Name = "CampaignMap";
        _cam = new Camera3D { Fov = 42, Near = 0.1f, Far = 4000, Current = true };
        AddChild(_cam);
        MapPieces.Ink = Paper;
        if (Paper)
        {
            AddChild(new WorldEnvironment { Environment = ChartRoom.Environment() });
            _chart = new ChartRoom();
            AddChild(_chart);
            _chart.Init(c, FacColor, NebulaRegions());
        }
        else InitSpace();
        _board = new Node3D { Name = "Board" };
        _routes = new Node3D { Name = "Routes" };
        _pieces = new Node3D { Name = "Pieces" };
        _path = new Node3D { Name = "Path" };
        AddChild(_board);
        AddChild(_routes);
        AddChild(_pieces);
        AddChild(_path);
        if (!Paper) BuildStars();
        _ring = MapPieces.HexRing(0.82f, 0.95f, new Color("#ffb547"));
        _hover = MapPieces.HexRing(0.9f, 0.95f, new Color(1, 1, 1, 0.45f));
        ((StandardMaterial3D)_hover.MaterialOverride).Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        AddChild(_ring);
        AddChild(_hover);
        _ring.Visible = _hover.Visible = false;
        Rebuild();
        CentreOn(c.S.AtC, c.S.AtR);
    }

    // the space look's sky, light and nebula backdrop
    private void InitSpace()
    {
        // the quiet nebula's panorama lights the board and the pieces and is what the glass reflects (the mockup's sky probe)
        var skyMat = new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/View/nebula_sky.gdshader") };
        skyMat.SetShaderParameter("panorama", GD.Load<Texture2D>("res://assets/skies/quiet.jpg"));
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("#03060a"),
            Sky = new Sky { SkyMaterial = skyMat, RadianceSize = Sky.RadianceSizeEnum.Size128 },
            AmbientLightSource = Godot.Environment.AmbientSource.Sky, AmbientLightEnergy = 0.9f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Sky,
            GlowEnabled = true, GlowIntensity = 0.5f, GlowBloom = 0.04f, TonemapMode = Godot.Environment.ToneMapper.Filmic,
        };
        AddChild(new WorldEnvironment { Environment = env });
        var sun = new DirectionalLight3D { LightEnergy = 0.6f, Name = "Sun" };
        AddChild(sun);
        sun.LookAtFromPosition(new Vector3(10, 20, 6), Vector3.Zero, Vector3.Up);
        _sky = NebulaSky.Load("quiet");
        AddChild(_sky);
    }

    public void CentreOn(int c, int r) { _camGlide = null; Target = HexPos(c, r); }

    /// <summary>Glides the view's centre to a sector over a third of a second (the player's Fleet key, a mission in the log).</summary>
    public void GlideTo(int c, int r) => _camGlide = (Target, HexPos(c, r), 0);

    public void Zoom(float k) => Dist = Math.Clamp(Dist * k, MinDist, MaxDist);

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;
        if (_camGlide is { } g)
        {
            float t = Math.Min(1, g.T + dt / 0.35f), k = 1 - MathF.Pow(1 - t, 3);
            Target = g.From.Lerp(g.To, k);
            _camGlide = t >= 1 ? null : (g.From, g.To, t);
        }
        Target = new Vector3(Math.Clamp(Target.X, -1, MapW + 1), 0, Math.Clamp(Target.Z, 0, MapH + 1));
        Dist = Math.Clamp(Dist, MinDist, MaxDist);
        _cam.Position = Target + new Vector3(0, Mathf.Cos(Polar), Mathf.Sin(Polar)) * Dist;
        _cam.LookAt(Target, Vector3.Up);
        _sky?.Follow(_cam.GlobalPosition);
        bool far = Dist > FarDist;
        foreach (Label3D l in _labels) if (IsInstanceValid(l)) l.Visible = !far;
        foreach (Node3D s in _spin) if (IsInstanceValid(s)) s.RotateY(dt * 0.15f);
        foreach (ShaderMaterial pm in _planets) pm.SetShaderParameter("time_s", _time);   // the drawn worlds turn
        SortPuffs();
        StepFlights(dt);
    }

    /// <summary>The sector under a screen point, or null off the map.</summary>
    public (int C, int R)? Pick(Vector2 screen)
    {
        Vector3 o = _cam.ProjectRayOrigin(screen), d = _cam.ProjectRayNormal(screen);
        if (Math.Abs(d.Y) < 1e-4) return null;
        float t = -o.Y / d.Y;
        if (t < 0) return null;
        Vector3 p = o + d * t;
        var (c, r) = HexGrid.At(p.X, p.Z);
        return _c.OnMap(c, r) ? (c, r) : null;
    }

    /// <summary>Pans by a screen drag: the map follows the pointer.</summary>
    public void Pan(Vector2 from, Vector2 to)
    {
        // the map is grabbed: the point of the plane that was under the pointer stays under it (a fixed rate per pixel
        // moved the map about seven times as far as the pointer at the usual distance)
        if (Ground(from) is not { } a || Ground(to) is not { } b) return;
        _camGlide = null;
        Target += a - b;
    }

    // the point of the map's plane under a screen point, or null when the ray misses it
    private Vector3? Ground(Vector2 screen)
    {
        Vector3 o = _cam.ProjectRayOrigin(screen), d = _cam.ProjectRayNormal(screen);
        if (Math.Abs(d.Y) < 1e-4) return null;
        float t = -o.Y / d.Y;
        return t < 0 ? null : o + d * t;
    }

    public void Select((int C, int R)? at)
    {
        _ring.Visible = at != null;
        if (at is { } a) _ring.Position = HexPos(a.C, a.R) + new Vector3(0, 0.02f, 0);
    }

    public void Hover((int C, int R)? at)
    {
        _hover.Visible = at != null;
        if (at is { } a) _hover.Position = HexPos(a.C, a.R) + new Vector3(0, 0.018f, 0);
    }

    // ---- The backdrop -----------------------------------------------------------------------------------------------

    // three layers of stars under the board (the mockup's parallaxStars: [-3, -10, 600], [-12, -32, 1400], [-40, -90, 2600]),
    // in the starfield's classes and colours (data/space/starfield.json)
    private void BuildStars()
    {
        using var doc = System.Text.Json.JsonDocument.Parse(FileAccess.GetFileAsString("res://data/space/starfield.json"));
        var classes = doc.RootElement.GetProperty("classes").EnumerateArray().Select(e => (Share: e.GetProperty("share").GetSingle(),
            B0: e.GetProperty("brightness")[0].GetSingle(), B1: e.GetProperty("brightness")[1].GetSingle(), Px: e.GetProperty("size_px").GetSingle())).ToList();
        var colours = doc.RootElement.GetProperty("colours").EnumerateArray().Select(e => (Share: e.GetProperty("share").GetSingle(),
            Rgb: new Color(e.GetProperty("srgb")[0].GetSingle(), e.GetProperty("srgb")[1].GetSingle(), e.GetProperty("srgb")[2].GetSingle()))).ToList();
        var classShares = classes.Select(x => x.Share).ToList();
        var colourShares = colours.Select(x => x.Share).ToList();
        var R = new Mulberry32(41);
        int Pick(List<float> shares) { double x = R.Next(); for (int i = 0; i < shares.Count; i++) { x -= shares[i]; if (x <= 0) return i; } return shares.Count - 1; }
        Vector3 centre = new(MapW / 2, 0, MapH / 2);
        const float spread = 220;
        var layers = new (float Y0, float Y1, int N)[] { (-3, -10, 600), (-12, -32, 1400), (-40, -90, 2600) };
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = new QuadMesh { Size = new Vector2(1, 1) } };
        mm.InstanceCount = layers.Sum(l => l.N);
        int k = 0;
        foreach (var (y0, y1, n) in layers)
            for (int i = 0; i < n; i++)
            {
                var p = new Vector3(centre.X + (float)(R.Next() - 0.5) * spread, y0 + (y1 - y0) * (float)R.Next(), centre.Z + (float)(R.Next() - 0.5) * spread);
                var cl = classes[Pick(classShares)];
                float b = cl.B0 + (cl.B1 - cl.B0) * (float)R.Next();
                Color col = colours[Pick(colourShares)].Rgb;
                // a star's size grows with its depth so each reads at about its class's pixels on screen
                float size = 0.05f * cl.Px * (1 + -p.Y / 30f);
                mm.SetInstanceTransform(k, new Transform3D(Basis.Identity.Scaled(Vector3.One * size), p));
                mm.SetInstanceColor(k, new Color(col.R * b, col.G * b, col.B * b));
                k++;
            }
        AddChild(new MultiMeshInstance3D
        {
            Multimesh = mm, Name = "ParallaxStars",
            MaterialOverride = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
                AlbedoTexture = MapPieces.Glow, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, BlendMode = BaseMaterial3D.BlendModeEnum.Add, BillboardKeepScale = true,
            },
        });
    }

    // ---- The board (built once per campaign) --------------------------------------------------------------------------

    /// <summary>Each connected run of nebula sectors is one cloud: a main colour (its index in the palette) and an accent far
    /// enough from it, and the cloud's number in the order found (the mockup's nebulaRegions and nebulaAccent, with its
    /// random numbers).</summary>
    public Dictionary<(int, int), (Color Main, Color Accent, int MainIdx, int Id)> NebulaRegions()
    {
        var tints = Palette.Select(h => new Color(h)).ToList();
        var region = new Dictionary<(int, int), (Color, Color, int, int)>();
        var R = new Mulberry32(97);
        int id = 0;
        foreach (Sector h in _c.S.Sectors)
        {
            if (h.Terrain != "nebula" || region.ContainsKey((h.C, h.R))) continue;
            int mi = (int)Math.Floor(R.Next() * tints.Count);
            Color main = tints[mi];
            var far = tints.Where(t => Mathf.Sqrt((t.R - main.R) * (t.R - main.R) + (t.G - main.G) * (t.G - main.G) + (t.B - main.B) * (t.B - main.B)) > 0.28f).ToList();
            Color accent = far[(int)Math.Floor(R.Next() * far.Count)];
            var stack = new Stack<Sector>();
            stack.Push(h);
            region[(h.C, h.R)] = (main, accent, mi, id);
            while (stack.Count > 0)
            {
                Sector q = stack.Pop();
                foreach (var (nc, nr) in HexGrid.Neighbours(q.C, q.R, _c.S.Cols, _c.S.Rows))
                {
                    Sector n = _c.Hex(nc, nr);
                    if (n.Terrain == "nebula" && !region.ContainsKey((nc, nr))) { region[(nc, nr)] = (main, accent, mi, id); stack.Push(n); }
                }
            }
            id++;
        }
        return region;
    }

    // The fog as it stands: the board (glass, borders, stars, clouds, rocks, ports) is rebuilt when what is known or seen
    // has changed since it was last drawn.
    private void Fog()
    {
        _vis = _c.Visible();
        string key = $"{_c.S.Sectors.Count(h => h.Known)}:{string.Join(",", _vis.OrderBy(v => v).Select(v => v.C * 100 + v.R))}:{string.Join(",", _c.S.Sectors.Select(h => h.KnownOwner[0]))}";
        if (key == _fogKey) return;
        _fogKey = key;
        foreach (Node n in _board.GetChildren()) { _board.RemoveChild(n); n.QueueFree(); }
        _puffList.Clear();
        _spin.Clear();
        _planets.Clear();
        _labels.RemoveAll(l => !IsInstanceValid(l) || !l.IsInsideTree());
        BuildBoard();
    }

    private void BuildBoard()
    {
        if (_chart != null)
        {
            // the paper chart: the sheet redraws its holders and fog, the drawn pieces hide in the uncharted
            _chart.Update(_vis);
            BuildPorts();
            return;
        }
        var regions = NebulaRegions();
        // dark glass hexes, each a faint wash of its holder's colour; ports brighter and warmer (owner, 2026-10-30: "tiles with
        // planets or stations should be a different color")
        var hex = new CylinderMesh { TopRadius = 0.95f, BottomRadius = 0.95f, Height = 0.12f, RadialSegments = 6, Rings = 1 };
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = hex, InstanceCount = _c.S.Sectors.Count };
        var turn = new Basis(Vector3.Up, Mathf.Pi / 6);
        var rocks = new List<Transform3D>();
        var fog = new List<Transform3D>();
        for (int i = 0; i < _c.S.Sectors.Count; i++)
        {
            Sector h = _c.S.Sectors[i];
            Vector3 p = HexPos(h.C, h.R);
            mm.SetInstanceTransform(i, new Transform3D(turn, p + new Vector3(0, -0.06f, 0)));
            // the fog of war: an unexplored sector is black glass with nothing in it; a revealed one shows what was seen
            // there and its empire as last known, darker while nothing of yours sees it now (owner, 2026-11-07: "outside the
            // empire it should be black fully, then revealed when either friendly our your ship moves through the fog")
            if (!h.Known) { mm.SetInstanceColor(i, new Color(0, 0, 0, 0)); fog.Add(new Transform3D(turn, p + new Vector3(0, -0.02f, 0))); continue; }
            string owner = h.KnownOwner;
            Color col = new Color("#080c12").Lerp(FacColor(owner), owner == "neutral" ? 0.02f : 0.06f);
            if (h.Terrain == "nebula") col = col.Lerp(regions[(h.C, h.R)].Main, 0.18f);
            if (h.Terrain == "asteroids") col = col.Lerp(new Color("#2a241e"), 0.4f);
            if (h.Terrain == "star") col = new Color("#2a1d0c");
            if (h.IsPort) col = new Color("#1c2430").Lerp(FacColor(owner), owner == "neutral" ? 0.25f : 0.42f).Lerp(new Color("#ffd9a0"), 0.12f);
            if (!_vis.Contains((h.C, h.R))) col = col.Darkened(0.45f);
            mm.SetInstanceColor(i, new Color(col, _vis.Contains((h.C, h.R)) ? 0.5f : 0.7f));

            if (h.Terrain == "star")
            {
                var sun = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.42f, Height = 0.84f, RadialSegments = 24, Rings = 12 }, Position = p + new Vector3(0, 0.5f, 0),
                    MaterialOverride = MapPieces.Unlit(new Color("#ffd27a")) };
                _board.AddChild(sun);
                var halo = MapPieces.Halo(new Color("#ffb547"), 1.6f);
                halo.Position = sun.Position;
                _board.AddChild(halo);
                _board.AddChild(new OmniLight3D { Position = sun.Position, LightColor = new Color("#ffc26a"), OmniRange = 3.5f, LightEnergy = 1.2f });
            }
            if (h.Terrain == "nebula")
            {
                // a cloud in depth: puffs at several heights, big and dim low, smaller and brighter on top, the accent rising
                var R = new Mulberry32((uint)(h.C * 17 + h.R * 5));
                var (main, accent, _, _) = regions[(h.C, h.R)];
                for (int k = 0; k < 14; k++)
                {
                    double y = 0.12 + R.Next() * 0.85, r = 0.32 + R.Next() * 0.32 - y * 0.12, v = 0.75 + R.Next() * 0.4;
                    double w = Math.Min(1, Math.Max(0, (y - 0.3) / 0.6) * (0.35 + 0.9 * R.Next()));
                    Color tint = main.Lerp(accent, (float)w);
                    double px = p.X + (R.Next() - 0.5) * 1.3, pz = p.Z + (R.Next() - 0.5) * 1.3, seed = R.Next() * 100, a = 0.5 + R.Next() * 0.3;
                    // the palette is sRGB, as the mockup's shader reads it; the puff shader writes linear light
                    Color lin = new Color(tint.R * (float)v, tint.G * (float)v, tint.B * (float)v).SrgbToLinear();
                    _puffList.Add((new Vector3((float)px, (float)y, (float)pz), new Vector2((float)r, (float)r * 0.8f), (float)seed, new Color(lin, (float)a)));
                }
            }
            if (h.Terrain == "asteroids")
            {
                // a belt: rocks along a curved band across the hex, the big ones in the middle of the band
                var R = new Mulberry32((uint)(h.C * 31 + h.R));
                double a0 = R.Next() * Math.PI, rad = 0.9 + R.Next() * 0.5;
                double cx = p.X - Math.Cos(a0) * rad, cz = p.Z - Math.Sin(a0) * rad;
                for (int k = 0; k < 34; k++)
                {
                    double u = (R.Next() - 0.5) * 1.1, off = (R.Next() - 0.5) * 0.28, rr = rad + off;
                    double size = 0.025 + R.Next() * (0.07 - Math.Abs(off) * 0.15);
                    var at = new Vector3((float)(cx + Math.Cos(a0 + u) * rr), (float)(0.1 + R.Next() * 0.18), (float)(cz + Math.Sin(a0 + u) * rr));
                    var rot = new Basis(new Vector3((float)R.Next() + 0.01f, (float)R.Next(), (float)R.Next()).Normalized(), (float)R.Next() * 3);
                    if (new Vector2(at.X - p.X, at.Z - p.Z).Length() > 0.85f) continue;
                    rocks.Add(new Transform3D(rot.Scaled(Vector3.One * (float)size), at));
                }
            }
        }
        _board.AddChild(new MultiMeshInstance3D
        {
            Multimesh = mm, Name = "Glass",
            MaterialOverride = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, Roughness = 0.22f, Metallic = 0.35f,
                DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
            },
        });
        // the unexplored: flat black, unlit and opaque, so it reflects nothing and nothing shows through
        if (fog.Count > 0)
        {
            var fm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = new CylinderMesh { TopRadius = 1.0f, BottomRadius = 1.0f, Height = 0.08f, RadialSegments = 6, Rings = 1 }, InstanceCount = fog.Count };
            for (int i = 0; i < fog.Count; i++) fm.SetInstanceTransform(i, fog[i]);
            _board.AddChild(new MultiMeshInstance3D { Multimesh = fm, MaterialOverride = MapPieces.Unlit(Colors.Black), Name = "Fog" });
        }
        var rm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = new SphereMesh { Radius = 1, Height = 1.6f, RadialSegments = 6, Rings = 3 }, InstanceCount = rocks.Count };
        for (int i = 0; i < rocks.Count; i++) rm.SetInstanceTransform(i, rocks[i]);
        _board.AddChild(new MultiMeshInstance3D { Multimesh = rm, MaterialOverride = MapPieces.Rock, Name = "Rocks" });
        BuildBorders();
        BuildPuffs();
        BuildPorts();
    }

    // hex borders: a faint line on every edge in its holder's colour, and a bright ribbon inside each hex along every edge
    // where territory changes, so a border shows both holders' colours (the mockup's hexBorders)
    private void BuildBorders()
    {
        var grid = new SurfaceTool();
        grid.Begin(Mesh.PrimitiveType.Lines);
        var rib = new SurfaceTool();
        rib.Begin(Mesh.PrimitiveType.Triangles);
        bool any = false;
        foreach (Sector h in _c.S.Sectors)
        {
            // borders as last known, and none in the unexplored or into it
            if (!h.Known) continue;
            string owner = h.KnownOwner;
            Vector3 p = HexPos(h.C, h.R);
            Color col = FacColor(owner);
            Vector3 C(int i, float r, float y) { float a = Mathf.Pi / 3 * i; return new Vector3(p.X + r * Mathf.Cos(a), y, p.Z + r * Mathf.Sin(a)); }
            for (int k = 0; k < 6; k++)
            {
                Color gc = col * (owner == "neutral" ? 0.35f : 0.5f);
                gc.A = 0.8f;
                grid.SetColor(gc); grid.AddVertex(C(k, 0.98f, 0.01f));
                grid.SetColor(gc); grid.AddVertex(C(k + 1, 0.98f, 0.01f));
                float dir = Mathf.Pi / 3 * k + Mathf.Pi / 6;
                var (nc, nr) = HexGrid.At(p.X + Mathf.Sqrt(3) * Mathf.Cos(dir), p.Z + Mathf.Sqrt(3) * Mathf.Sin(dir));
                bool same = _c.OnMap(nc, nr) && _c.Hex(nc, nr).KnownOwner == owner;
                if (owner == "neutral" || same || (_c.OnMap(nc, nr) && !_c.Hex(nc, nr).Known)) continue;
                Color rc = new(col, 0.9f);
                Vector3 a = C(k, 0.98f, 0.02f), b = C(k + 1, 0.98f, 0.02f), ai = C(k, 0.86f, 0.02f), bi = C(k + 1, 0.86f, 0.02f);
                foreach (Vector3 v in new[] { a, b, bi, a, bi, ai }) { rib.SetColor(rc); rib.AddVertex(v); }
                any = true;
            }
        }
        var lm = new StandardMaterial3D { VertexColorUseAsAlbedo = true, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
        _board.AddChild(new MeshInstance3D { Mesh = grid.Commit(), MaterialOverride = lm, Name = "Grid" });
        if (!any) return;
        var rmat = new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
        _board.AddChild(new MeshInstance3D { Mesh = rib.Commit(), MaterialOverride = rmat, Name = "Borders" });
    }

    private void BuildPuffs()
    {
        var mm = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, UseCustomData = true,
            Mesh = new QuadMesh { Size = new Vector2(2, 2) }, InstanceCount = _puffList.Count,
        };
        _puffs = new MultiMeshInstance3D
        {
            Multimesh = mm, Name = "Nebulae", MaterialOverride = new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/Flow/puff.gdshader") },
            // the billboards are moved in the shader: keep the whole board's box so a cloud is never culled
            CustomAabb = new Aabb(new Vector3(-2, -1, -2), new Vector3(MapW + 4, 3, MapH + 4)),
        };
        _board.AddChild(_puffs);
        _puffEye = new Vector3(1e9f, 0, 0);
    }

    // the puffs drawn back to front from wherever the camera is (the mockup's sortPuffs)
    private void SortPuffs()
    {
        if (_puffList.Count == 0) return;
        Vector3 eye = _cam.GlobalPosition;
        if (eye.DistanceSquaredTo(_puffEye) < 0.01f) return;
        _puffEye = eye;
        var order = Enumerable.Range(0, _puffList.Count).OrderByDescending(i => _puffList[i].Pos.DistanceSquaredTo(eye)).ToList();
        MultiMesh mm = _puffs.Multimesh;
        for (int o = 0; o < order.Count; o++)
        {
            var q = _puffList[order[o]];
            mm.SetInstanceTransform(o, new Transform3D(Basis.Identity, q.Pos));
            mm.SetInstanceColor(o, q.Tint);
            mm.SetInstanceCustomData(o, new Color(q.Size.X, q.Size.Y, q.Seed, 0));
        }
    }

    // planets and stations, built once: they never move (they turn slowly)
    private void BuildPorts()
    {
        foreach (Sector h in _c.S.Sectors.Where(s => s.IsPort && s.Known))
        {
            string fac = h.KnownOwner;
            Color col = FacColor(fac);
            var g = new Node3D { Name = h.PortName ?? "Port" };
            if (h.Planet != null && Paper)
            {
                // a drawn world on a hexagon flight stand in its holder's colour (owner, 2026-11-08: "give planets those
                // hexagon standees as well"), the disc on the post's top (the page's makeStandee planet)
                const float size = 1.15f, post = 0.42f * size, disc = 0.5f * size;
                g.AddChild(MapPieces.FlightStand(col, post, size, false));
                var (planet, mat) = ChartRoom.Planet(h.Planet, col, disc);
                planet.Position = new Vector3(0, 0.06f + post + 0.02f - disc / 2 * 0.55f, 0);
                g.AddChild(planet);
                _planets.Add(mat);
            }
            else if (h.Planet != null)
            {
                int hue = PlanetHue.TryGetValue(h.Planet, out int hh) ? hh : 200;
                Node3D pl = MapPieces.PlanetPiece(hue, col, 1.15f);
                g.AddChild(pl);
                _spin.Add(pl.GetNode<Node3D>("Ball"));
            }
            else
            {
                g.AddChild(MapPieces.FlightStand(col, 0.5f * 1.05f, 1.05f, false));
                Node3D st = MapPieces.Station(fac, col);
                st.Scale = Vector3.One * 1.05f;
                st.Position = new Vector3(0, 0.06f + 0.58f * 1.05f, 0);
                g.AddChild(st);
                _spin.Add(st);
            }
            Label3D tag = MapPieces.Label(h.PortName!, col.Lightened(0.15f));
            tag.Position = new Vector3(0, -0.05f, 0.1f);
            g.AddChild(tag);
            _labels.Add(tag);
            g.SetMeta("hex", new Vector2I(h.C, h.R));
            g.Position = HexPos(h.C, h.R);
            _board.AddChild(g);
        }
    }

    // ---- The pieces (rebuilt after every move) ------------------------------------------------------------------------

    private static readonly Dictionary<string, PackedScene?> Models = new();

    /// <summary>A battle class's approved glb (data/battle/classes.json model), or null: set by MapScreen from the battle's data.</summary>
    public Func<string, string?> ClassModel { get; init; } = _ => null;

    /// <summary>A ship's model on the map: the hull's own approved glb (its schematic names it, else its battle class's model),
    /// or the faction's by tier for the Concord and the Krai, a stand-in wedge for the rest; a freighter carries her
    /// containers (Cargo). Bow toward +z, <paramref name="length"/> units long.</summary>
    private Node3D ShipModel(string fac, string? hullId, int tier, float length)
    {
        string? path = null;
        if (hullId != null && _c.Data.Hulls.TryGetValue(hullId, out Hull? hull))
            path = hull.Schematic != "" ? $"res://assets/ships/{hull.Schematic}.glb" : ClassModel(hull.BattleClass) is { } m ? "res://" + m : null;
        path ??= fac switch
        {
            "concord" => tier >= 3 ? "res://assets/ships/concord/albatross.glb" : "res://assets/ships/concord/petrel_level.glb",
            "krai" => $"res://assets/ships/krai/{(tier <= 1 ? "corvette" : tier == 2 ? "frigate" : "cruiser")}.glb",
            _ => null,
        };
        var holder = new Node3D { Name = "Model" };
        if (path != null)
        {
            if (!Models.TryGetValue(path, out PackedScene? ps)) Models[path] = ps = ResourceLoader.Exists(path) ? GD.Load<PackedScene>(path) : null;
            if (ps != null)
            {
                Node3D model = ps.Instantiate<Node3D>();
                Aabb box = Bounds(model);
                float len = Math.Max(box.Size.Z, 0.01f);
                model.Scale = Vector3.One * (length / len);
                // the glb's bow is -z; a standee's bow is +z (the map's headings)
                model.RotationDegrees = new Vector3(0, 180, 0);
                try { ShipSkin.Apply(model, path["res://assets/ships/".Length..], model.Scale.X); Cargo.Load(model, path["res://assets/ships/".Length..]); }
                catch (InvalidOperationException) { /* a ship the export manifest does not dress keeps its own materials */ }
                holder.AddChild(model);
                return holder;
            }
        }
        holder.AddChild(Wedge(fac, length));
        return holder;
    }

    private static Aabb Bounds(Node3D n)
    {
        Aabb box = default;
        bool any = false;
        foreach (Node ch in n.FindChildren("*", "MeshInstance3D", true, false))
            if (ch is MeshInstance3D mi && mi.Mesh != null)
            {
                Aabb b = mi.Mesh.GetAabb();
                box = any ? box.Merge(b) : b;
                any = true;
            }
        return box;
    }

    private static MeshInstance3D Wedge(string fac, float length)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        float k = length / 0.52f;
        Vector3 n = new(0, 0, 0.3f * k), l = new(-0.16f * k, 0, -0.22f * k), r = new(0.16f * k, 0, -0.22f * k), t = new(0, 0.07f * k, -0.1f * k);
        foreach (var tri in new[] { (n, l, t), (n, t, r), (l, r, t), (n, r, l) }) { st.AddVertex(tri.Item1); st.AddVertex(tri.Item2); st.AddVertex(tri.Item3); }
        st.GenerateNormals();
        return new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = new StandardMaterial3D { AlbedoColor = MapPieces.HullTint(fac), Metallic = 0.5f, Roughness = 0.4f } };
    }

    private int PiecesIn(int c, int r) =>
        (_c.S.AtC == c && _c.S.AtR == r ? 1 : 0) + (_c.Hex(c, r).IsPort && _c.Hex(c, r).Known ? 1 : 0) + _c.S.Groups.Count(g => g.C == c && g.R == r && _vis.Contains((c, r)))
        + _c.S.Missions.Count(m => m.C == c && m.R == r) + _c.S.Freighters.Count(f => f.Out == 0 && f.Here() == (c, r) && Shown(f));

    // where a piece stands in its sector: centred alone, at its own slot when the sector is shared (owner, 2026-11-05)
    private Vector3 Slot(string role, int c, int r)
    {
        Vector3 p = HexPos(c, r);
        if (PiecesIn(c, r) <= 1) return p;
        return p + role switch
        {
            "fleet" => new Vector3(-0.45f, 0, 0.45f), "place" => new Vector3(0.1f, 0, -0.35f), "ship" => new Vector3(0.45f, 0, 0.42f),
            "freighter" => new Vector3(0.66f, 0, -0.12f), "mission" => new Vector3(-0.5f, 0, -0.28f), _ => Vector3.Zero,
        };
    }

    /// <summary>A freighter's length on the map, units: as long as a fleet's flagship, bigger than its escorts.</summary>
    private const float FreighterLen = 0.9f;

    /// <summary>Whether the map shows a freighter: your own always, anyone's in a sector your ships see now.</summary>
    private bool Shown(Freighter f) => _c.SideOf(f.Fac) == "own" || _c.IsVisible(f.Here().C, f.Here().R);

    /// <summary>A fleet's strength, 1 to 5 (the mockup's fleetStrength and groupStrength).</summary>
    private int FleetStrength() => Math.Clamp((int)Math.Round(_c.S.Fleet.Sum(s => _c.Data.Hulls[s.Hull].Tier) / 1.5, MidpointRounding.AwayFromZero), 1, 5);
    private static int GroupStrength(FactionFleet g) => Math.Clamp(g.Size + (g.Commander ? 2 : 0), 1, 5);

    private Piece ShipStandee(string fac, string label, float size, string? hullId, int tier, float dir, int strength, bool gold)
    {
        var g = new Node3D { Name = label };
        Color col = FacColor(fac);
        g.AddChild(MapPieces.FlightStand(col, 0.42f * size, size, gold));
        Node3D model = ShipModel(fac, hullId, tier, 0.9f * size);
        model.Position = new Vector3(0, 0.06f + 0.46f * size, 0);
        model.Rotation = new Vector3(0, dir, 0);
        g.AddChild(model);
        Node3D flag = MapPieces.StrengthFlag(col, strength, size, label.Length * 1.7f);
        flag.Position = new Vector3(-0.3f * size, 0, 0.08f * size);
        g.AddChild(flag);
        if (gold)
        {
            // a lamp ring round the player's own stand
            g.AddChild(new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 0.5f, OuterRadius = 0.6f, Rings = 32, RingSegments = 4 }, Position = new Vector3(0, 0.07f, 0),
                Scale = new Vector3(1, 0.04f, 1), MaterialOverride = MapPieces.Unlit(new Color("#ffc94a")) });
        }
        Label3D tag = MapPieces.Label(label, col.Lightened(0.15f));
        tag.Position = new Vector3(0, -0.05f, 0.1f);
        g.AddChild(tag);
        _labels.Add(tag);
        return new Piece { Node = g, Model = model, EndDir = dir, Lift = 0.06f + 0.46f * size };
    }

    /// <summary>Redraws every piece from the campaign. The fleets that moved (and the player's, with ownSteps) fly their
    /// moves sector by sector from where they stood.</summary>
    public void Rebuild(IEnumerable<FleetMove>? moves = null, List<(int C, int R)>? ownSteps = null)
    {
        Fog();
        var was = _fleets.ToDictionary(kv => kv.Key, kv => kv.Value.Node.Position);
        // each piece's heading before the rebuild, where its first turn starts from
        var yaws = _fleets.Where(kv => kv.Value.Model != null && IsInstanceValid(kv.Value.Model)).ToDictionary(kv => kv.Key, kv => kv.Value.Model!.Rotation.Y);
        foreach (Node n in _pieces.GetChildren()) { _pieces.RemoveChild(n); n.QueueFree(); }
        foreach (Node n in _routes.GetChildren()) { _routes.RemoveChild(n); n.QueueFree(); }
        _fleets.Clear();
        _labels.RemoveAll(l => !IsInstanceValid(l) || !l.IsInsideTree());

        // the player's fleet: the flagship's own hull on a gold stand, with the fleet's strength
        CampShip flagship = _c.S.Fleet[0];
        Piece own = ShipStandee(_c.S.Side, flagship.Name, 1.15f, flagship.Hull, _c.Data.Hulls[flagship.Hull].Tier, (float)_c.S.Dir, FleetStrength(), true);
        own.Node.Position = Slot("fleet", _c.S.AtC, _c.S.AtR);
        _pieces.AddChild(own.Node);
        _fleets["__own"] = own;

        // other fleets only where you can see them now (the fog's visible sectors)
        foreach (FactionFleet g in _c.S.Groups.Where(g => _vis.Contains((g.C, g.R))))
        {
            Piece p = ShipStandee(g.Fac, g.Name, 1, null, Math.Clamp(g.Size, 1, 3), (float)g.Dir, GroupStrength(g), false);
            p.Node.Position = Slot("ship", g.C, g.R);
            _pieces.AddChild(p.Node);
            _fleets[g.Name] = p;
        }

        // the ports move to their slot when a sector fills, and back to the centre when it empties
        foreach (Node n in _board.GetChildren())
            if (n is Node3D port && port.HasMeta("hex"))
            {
                Vector2I hx = port.GetMeta("hex").AsVector2I();
                port.Position = Slot("place", hx.X, hx.Y);
            }

        // freighters and their routes: your own and your allies' routes (allies' dimmer), never a rival's; your own freighters
        // always, and anyone's where your ships see (owner, 2026-11-07: "whenever you are on the same tile as freighter, you
        // get a mission option"), each her faction's approved freighter with her containers; one off her route, none
        int fi = 0;
        foreach (Freighter f in _c.S.Freighters)
        {
            int n = fi++;
            string? seen = _c.SideOf(f.Fac);
            if (seen != null)
            {
                var lift = new Vector3(((n % 3) - 1) * 0.05f, 0.035f, ((n % 3) - 1) * 0.05f);
                var pts = f.Route.Select(k => HexPos(k[0], k[1]) + lift).ToList();
                _routes.AddChild(Dashed(pts, FacColor(f.Fac), seen == "own" ? 0.95f : 0.5f, 0.22f, 0.14f));
            }
            if (f.Out > 0 || !Shown(f)) continue;
            var g = new Node3D { Name = "Freighter " + f.Name };
            g.AddChild(MapPieces.FlightStand(FacColor(f.Fac), 0.2f, 0.75f, false));
            Node3D model = ShipModel(f.Fac, _c.Data.FreighterOf(f.Fac).Id, 1, FreighterLen);
            var mh = new Node3D { Position = new Vector3(0, 0.06f + 0.26f, 0), Rotation = new Vector3(0, (float)f.Dir, 0) };
            mh.AddChild(model);
            g.AddChild(mh);
            Label3D tag = MapPieces.Label("Freighter " + f.Name, FacColor(f.Fac).Lightened(0.15f));
            tag.Position = new Vector3(0, -0.05f, 0.1f);
            g.AddChild(tag);
            _labels.Add(tag);
            g.Position = Slot("freighter", f.Route[f.I][0], f.Route[f.I][1]);
            _pieces.AddChild(g);
            _fleets["freighter:" + f.Name] = new Piece { Node = g, Model = mh, EndDir = (float)f.Dir, Lift = 0.32f };
        }

        // missions: a standee in its sector, its banner amber on offer, green once accepted, blue for a story mission
        foreach (Offer m in _c.S.Missions.Where(m => _c.Hex(m.C, m.R).Known))
        {
            MissionScript def = _c.ScriptOf(m);
            Color ink = def.Story ? new Color("#8fd0ff") : _c.S.Active.Contains(m.Id) ? new Color("#7dffa0") : new Color("#ffb547");
            Node3D s = MapPieces.MissionStandee(def.Glyph, ink, m.Id * 1.7f);
            s.Position = Slot("mission", m.C, m.R);
            _pieces.AddChild(s);
        }

        // fly what moved from where it stood, sector by sector
        if (moves != null)
            foreach (FleetMove mv in moves)
                if (_fleets.TryGetValue(mv.Name, out Piece? p) && was.TryGetValue(mv.Name, out Vector3 from))
                    Fly(p, mv.Steps, from, Slot("ship", mv.Steps[^1].C, mv.Steps[^1].R), yaws.GetValueOrDefault(mv.Name, p.EndDir));
        foreach (var (k, p) in _fleets.Where(kv => kv.Key.StartsWith("freighter:")))
            if (was.TryGetValue(k, out Vector3 from) && from.DistanceSquaredTo(p.Node.Position) > 1e-4f) { p.Pts = new() { from, p.Node.Position }; p.T = 0; p.Yaw0 = yaws.GetValueOrDefault(k, p.EndDir); p.Node.Position = from; }
        if (ownSteps is { Count: > 1 } && was.TryGetValue("__own", out Vector3 of))
            Fly(own, ownSteps, of, Slot("fleet", ownSteps[^1].C, ownSteps[^1].R), yaws.GetValueOrDefault("__own", own.EndDir));
    }

    private static void Fly(Piece p, List<(int C, int R)> steps, Vector3 from, Vector3 to, float yaw0)
    {
        var pts = new List<Vector3> { from };
        for (int i = 1; i < steps.Count - 1; i++) pts.Add(HexPos(steps[i].C, steps[i].R));
        pts.Add(to);
        p.Pts = pts;
        p.T = 0;
        p.Yaw0 = yaw0;
        p.Node.Position = from;
        if (p.Model != null) p.Model.Rotation = new Vector3(0, yaw0, 0);
    }

    // One phase of a flight: a turn on the spot from one heading to another, or a move from one point to the next.
    private readonly record struct Phase(float S, float Yaw0, float Yaw1, Vector3 A, Vector3 B, bool Turn);

    // A flight as its phases, in order: for each leg a turn to face it (when the heading changes) and then the move,
    // and at the end a turn to the fleet's final heading (owner, 2026-11-07: "turn first, then move", "a move maybe move
    // then turn", "then move again").
    private static List<Phase> Phases(Piece p, List<Vector3> pts)
    {
        var list = new List<Phase>();
        float yaw = p.Yaw0;
        void TurnTo(float to, Vector3 at)
        {
            float d = Mathf.Abs(Mathf.AngleDifference(yaw, to));
            if (d > 1e-3f) list.Add(new Phase(FlightTurnS * d / Mathf.Pi, yaw, to, at, at, true));
            yaw = to;
        }
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], b = pts[i + 1];
            if (a.DistanceSquaredTo(b) <= 1e-6f) continue;
            TurnTo(Mathf.Atan2(b.X - a.X, b.Z - a.Z), a);
            list.Add(new Phase(1 / FlightSectorsS, yaw, yaw, a, b, false));
        }
        TurnTo(p.EndDir, pts[^1]);
        return list;
    }

    // the flights: each piece along its points, turning to face each leg, lifted off its stand while it flies (the
    // mockup's stepFlight and stepFleetGlides)
    private void StepFlights(float dt)
    {
        foreach (Piece p in _fleets.Values)
        {
            if (p.Pts is not { } pts || !IsInstanceValid(p.Node)) continue;
            p.T += dt;
            float t = p.T;
            bool done = true;
            foreach (Phase ph in Phases(p, pts))
            {
                if (t >= ph.S) { t -= ph.S; continue; }
                // eased within each phase, so the fleet comes to rest between a turn and a move
                float k = t / ph.S, e = k * k * (3 - 2 * k);
                p.Node.Position = ph.A.Lerp(ph.B, e);
                if (p.Model != null) p.Model.Rotation = new Vector3(0, Mathf.LerpAngle(ph.Yaw0, ph.Yaw1, e), 0);
                done = false;
                break;
            }
            // the ship slides along its route at its stand's height (owner, 2026-11-06: "when ships move dont hop them lol,
            // it looks funny, just slide them around the map is fine")
            if (p.Model != null) p.Model.Position = new Vector3(0, p.Lift, 0);
            if (done)
            {
                p.Node.Position = pts[^1];
                if (p.Model != null) p.Model.Rotation = new Vector3(0, p.EndDir, 0);
                p.Pts = null;
            }
        }
    }

    /// <summary>A dashed line through points on the map, its dashes running on across the corners.</summary>
    private static MeshInstance3D Dashed(List<Vector3> pts, Color col, float alpha, float dash, float gap)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Lines);
        float run = 0, period = dash + gap;
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], b = pts[i + 1];
            float len = a.DistanceTo(b);
            if (len < 1e-5f) continue;
            for (float t = 0; t < len;)
            {
                float phase = (run + t) % period, step;
                if (phase < dash)
                {
                    step = Math.Min(dash - phase, len - t);
                    st.AddVertex(a.Lerp(b, t / len));
                    st.AddVertex(a.Lerp(b, (t + step) / len));
                }
                else step = Math.Min(period - phase, len - t);
                t += Math.Max(step, 1e-4f);
            }
            run += len;
        }
        if (pts.Count < 2) { st.AddVertex(Vector3.Zero); st.AddVertex(Vector3.Zero); }
        return new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = MapPieces.Unlit(col, alpha) };
    }

    /// <summary>Shows the fleet's course and the route to the picked sector. The course (owner, 2026-11-06: "when I click move
    /// show a line to my destination, dont just remove it when I set course") stays drawn from where it was set until the
    /// fleet arrives; the picked sector's route is drawn over it when it goes elsewhere, in the sky colour.</summary>
    public void ShowRoute(Route? preview, (Route Route, (int C, int R) From)? course = null)
    {
        foreach (Node n in _path.GetChildren()) { _path.RemoveChild(n); n.QueueFree(); }
        if (course is { } cs && cs.Route.Path.Count > 0) DrawRoute(cs.Route, cs.From, new Color("#ffb547"));
        bool same = course is { } cc && preview != null && preview.Path.Count > 0 && cc.Route.Path.Count > 0 && preview.Path[^1] == cc.Route.Path[^1];
        if (preview != null && preview.Path.Count > 0 && !same)
            DrawRoute(preview, (_c.S.AtC, _c.S.AtR), course != null ? new Color("#8fd0ff") : new Color("#ffb547"));
    }

    /// <summary>A route by turn through the sectors' centres (the mockup's showPath): this turn's leg a solid ribbon with
    /// round joints, later turns dotted (owner, 2026-11-06: "future turns should be dotted lines"), a numbered badge where
    /// each turn ends, and an arrowhead.</summary>
    private void DrawRoute(Route route, (int C, int R) from, Color col)
    {
        var mats = new[] { 0.95f, 0.7f, 0.5f }.Select(o => { StandardMaterial3D m = MapPieces.Unlit(col, o); m.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled; return m; }).ToArray();
        var legOf = new List<int>();
        var ends = new List<((int C, int R) At, int Leg)>();
        for (int i = 0; i < route.Legs.Count; i++)
        {
            legOf.AddRange(Enumerable.Repeat(i, route.Legs[i].Count));
            if (route.Legs[i].Count > 0) ends.Add((route.Legs[i][^1], i));
        }
        var pts = new List<Vector3> { HexPos(from.C, from.R) };
        pts.AddRange(route.Path.Select(k => HexPos(k.C, k.R)));
        const float Y = 0.05f, W = 0.07f, Dot = 0.2f;
        var dot = new CylinderMesh { TopRadius = W * 0.85f, BottomRadius = W * 0.85f, Height = 0.001f, RadialSegments = 12 };
        float run = 0;
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Material mat = mats[Math.Min(2, legOf[i])];
            Vector3 a = pts[i], b = pts[i + 1];
            if (i == pts.Count - 2) b = b.Lerp(a, 0.32f / a.DistanceTo(b));
            float len = a.DistanceTo(b);
            if (legOf[i] > 0)
            {
                // a later turn: dots at an even pitch, running on across the corners
                float t0 = (Dot - run % Dot) % Dot;
                for (float t = t0; t <= len; t += Dot)
                    _path.AddChild(new MeshInstance3D { Mesh = dot, MaterialOverride = mat, Position = a.Lerp(b, t / len) with { Y = Y } });
                run += len;
                continue;
            }
            var seg = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(len, W * 2) }, MaterialOverride = mat, Position = new Vector3((a.X + b.X) / 2, Y, (a.Z + b.Z) / 2) };
            seg.Rotation = new Vector3(0, -Mathf.Atan2(b.Z - a.Z, b.X - a.X), 0);
            _path.AddChild(seg);
            if (i > 0) _path.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = W, BottomRadius = W, Height = 0.001f, RadialSegments = 16 }, MaterialOverride = mat, Position = new Vector3(a.X, Y, a.Z) });
        }
        if (route.Legs.Count > 1)
            foreach (var (at, leg) in ends)
            {
                Node3D badge = MapPieces.TurnBadge(leg + 1, leg == 0);
                badge.Position = HexPos(at.C, at.R) + new Vector3(0, 0.32f, 0);
                _path.AddChild(badge);
            }
        Vector3 end = pts[^1], prev = pts[^2];
        float ang = Mathf.Atan2(end.Z - prev.Z, end.X - prev.X);
        var head = new SurfaceTool();
        head.Begin(Mesh.PrimitiveType.Triangles);
        for (int k = 0; k < 3; k++)
        {
            float a0 = Mathf.Tau / 3 * k;
            head.AddVertex(Vector3.Zero);
            head.AddVertex(new Vector3(Mathf.Cos(a0 + Mathf.Tau / 3), 0, Mathf.Sin(a0 + Mathf.Tau / 3)) * 0.32f);
            head.AddVertex(new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 0.32f);
        }
        var hm = new MeshInstance3D { Mesh = head.Commit(), MaterialOverride = mats[Math.Min(2, legOf[^1])],
            Position = new Vector3(end.X - Mathf.Cos(ang) * 0.12f, Y, end.Z - Mathf.Sin(ang) * 0.12f) };
        hm.Rotation = new Vector3(0, -ang, 0);
        _path.AddChild(hm);
    }

    /// <summary>The screen point of a sector's centre (for the HUD to anchor to), or null behind the camera.</summary>
    public Vector2? ScreenOf(int c, int r)
    {
        Vector3 p = HexPos(c, r);
        return _cam.IsPositionBehind(p) ? null : _cam.UnprojectPosition(p);
    }

    /// <summary>One material of every kind the map draws, for the loading screen to compile before the map first shows (Prewarm).</summary>
    public static IEnumerable<Material> MaterialKinds()
    {
        yield return new StandardMaterial3D { VertexColorUseAsAlbedo = true, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, Roughness = 0.22f, Metallic = 0.35f, DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled };
        yield return new StandardMaterial3D { VertexColorUseAsAlbedo = true, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
        yield return new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            AlbedoTexture = MapPieces.Glow, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, BlendMode = BaseMaterial3D.BlendModeEnum.Add, BillboardKeepScale = true,
        };
        yield return new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/Flow/puff.gdshader") };
        yield return new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/Flow/banner.gdshader") };
        yield return MapPieces.Unlit(Colors.White, 0.5f);
        yield return MapPieces.Post;
        yield return MapPieces.Base;
        yield return MapPieces.Gold;
        yield return MapPieces.PoleMetal;
        yield return MapPieces.Rock;
        yield return new StandardMaterial3D { AlbedoTexture = MapPieces.Planet(200), Roughness = 0.9f };
        yield return new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/Flow/chart_sheet.gdshader") };
        yield return new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/Flow/planet.gdshader") };
    }
}
