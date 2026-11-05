// CampaignMap: the campaign's hex map in 3D (game-flow design, "Campaign map"; the campaign mockup v14,
// docs/mockups/game-flow.html): the sectors as hex tiles coloured by who holds them (ports lighter), territory borders,
// stars, planets and stations, asteroid fields and nebula puffs, and the pieces on their flight stands: your fleet on a
// gold stand, the factions' fleets each with a strength flag and turned the way they last moved, freighters on your and
// your allies' routes, and every mission's banner. A route preview shows each turn's leg, later legs fainter, with a
// numbered badge where each turn ends. The camera is the player's (CLAUDE.md section 8): drag pans, the wheel zooms, a
// right drag turns; nothing moves it on its own except a click on your fleet, which the player asked for.
// It draws campaign state and holds no rules: everything it shows is read from Campaign (src/Core/Campaign).
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Campaign;
using Godot;

namespace FleetCommand.Game.Flow;

public sealed partial class CampaignMap : Node3D
{
    private Campaign.Campaign _c = null!;
    private Camera3D _cam = null!;
    private Node3D _pieces = null!, _routes = null!, _path = null!;
    private MeshInstance3D _ring = null!, _hover = null!;
    private readonly Dictionary<string, Node3D> _fleetNodes = new();
    private readonly Dictionary<string, (Vector3 From, Vector3 To, float T)> _glides = new();
    private Node3D? _own;
    private readonly List<(MeshInstance3D Mesh, float Phase)> _banners = new();
    private float _time;

    /// <summary>Camera: the point it looks at on the map plane, its distance, pitch and yaw.</summary>
    public Vector3 Target;
    public float Dist = 16, Pitch = 0.95f, Yaw;
    public const float MinDist = 5, MaxDist = 42;

    private static readonly Dictionary<string, Color> PlanetHue = new()
    {
        ["Halcyon"] = Color.FromHsv(205 / 360f, 0.45f, 0.8f), ["Meridian"] = Color.FromHsv(160 / 360f, 0.45f, 0.75f), ["Vahr"] = Color.FromHsv(12 / 360f, 0.5f, 0.75f),
        ["Ouro"] = Color.FromHsv(40 / 360f, 0.5f, 0.8f), ["Tessaly"] = Color.FromHsv(280 / 360f, 0.35f, 0.75f),
    };
    /// <summary>The owner's nebula palette (2026-11-05), main colours the puffs are tinted from.</summary>
    private static readonly string[] Nebula = { "#8b5580", "#c38890", "#666092", "#9a4f50", "#7ca1c0", "#416aa3", "#68aca9", "#387080", "#6eaa78", "#557064" };

    public static Vector3 HexPos(int c, int r) { var (x, z) = HexGrid.Pos(c, r); return new Vector3((float)x, 0, (float)z); }

    public Color FacColor(string fac) => _c.Data.Factions.TryGetValue(fac, out CampFaction? f) ? new Color(f.Color) : new Color("#7c8794");

    public void Init(Campaign.Campaign c)
    {
        _c = c;
        Name = "CampaignMap";
        _cam = new Camera3D { Fov = 42, Near = 0.1f, Far = 400, Current = true };
        AddChild(_cam);
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color(0.012f, 0.016f, 0.035f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color(0.55f, 0.6f, 0.75f), AmbientLightEnergy = 0.55f,
            GlowEnabled = true, GlowIntensity = 0.6f, GlowBloom = 0.05f,
        };
        AddChild(new WorldEnvironment { Environment = env });
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-58, -30, 0), LightEnergy = 1.1f });
        BuildStars();
        BuildBoard();
        _routes = new Node3D { Name = "Routes" };
        _pieces = new Node3D { Name = "Pieces" };
        _path = new Node3D { Name = "Path" };
        AddChild(_routes);
        AddChild(_pieces);
        AddChild(_path);
        _ring = Ring(0.92f, 0.06f, new Color(1, 0.85f, 0.35f));
        _hover = Ring(0.95f, 0.035f, new Color(1, 1, 1, 0.55f));
        AddChild(_ring);
        AddChild(_hover);
        _ring.Visible = _hover.Visible = false;
        Rebuild();
        CentreOn(c.S.AtC, c.S.AtR);
    }

    public Camera3D Camera => _cam;

    public void CentreOn(int c, int r) => Target = HexPos(c, r);

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;
        // the camera: orbiting its target, kept over the map
        var (mx, mz) = HexGrid.Pos(_c.S.Cols - 1, _c.S.Rows - 1);
        Target = new Vector3(Math.Clamp(Target.X, -2, (float)mx + 2), 0, Math.Clamp(Target.Z, -2, (float)mz + 2));
        Dist = Math.Clamp(Dist, MinDist, MaxDist);
        Pitch = Math.Clamp(Pitch, 0.45f, 1.35f);
        var off = new Vector3(Mathf.Sin(Yaw) * Mathf.Cos(Pitch), Mathf.Sin(Pitch), Mathf.Cos(Yaw) * Mathf.Cos(Pitch)) * Dist;
        _cam.Position = Target + off;
        _cam.LookAt(Target, Vector3.Up);
        foreach (var (mesh, ph) in _banners)
            if (IsInstanceValid(mesh)) mesh.Rotation = new Vector3(0, Mathf.Sin(_time * 2.2f + ph) * 0.18f, 0);
        foreach (string k in _glides.Keys.ToList())
        {
            var (a, b, t) = _glides[k];
            t = Math.Min(1, t + dt / 0.9f);
            _glides[k] = (a, b, t);
            if (_fleetNodes.TryGetValue(k, out Node3D? n) && IsInstanceValid(n)) n.Position = a.Lerp(b, Mathf.SmoothStep(0, 1, t));
            if (t >= 1) _glides.Remove(k);
        }
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

    public void Pan(Vector2 screenDelta)
    {
        float k = Dist * 0.0022f;
        var right = new Vector3(Mathf.Cos(Yaw), 0, -Mathf.Sin(Yaw));
        var fwd = new Vector3(-Mathf.Sin(Yaw), 0, -Mathf.Cos(Yaw));
        Target += (-right * screenDelta.X + fwd * screenDelta.Y) * k;
    }

    public void Select((int C, int R)? at)
    {
        _ring.Visible = at != null;
        if (at is { } a) _ring.Position = HexPos(a.C, a.R) + new Vector3(0, 0.03f, 0);
    }

    public void Hover((int C, int R)? at)
    {
        _hover.Visible = at != null;
        if (at is { } a) _hover.Position = HexPos(a.C, a.R) + new Vector3(0, 0.025f, 0);
    }

    // ---- The board (built once) ---------------------------------------------------------------------------------------

    private void BuildBoard()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var lines = new SurfaceTool();
        lines.Begin(Mesh.PrimitiveType.Lines);
        foreach (Sector h in _c.S.Sectors)
        {
            Vector3 p = HexPos(h.C, h.R);
            Color col = FacColor(h.Owner).Darkened(h.IsPort ? 0.3f : 0.6f);
            if (h.Owner == "neutral") col = new Color(0.16f, 0.18f, 0.22f);
            if (h.Terrain == "nebula") col = col.Lerp(new Color(0.42f, 0.28f, 0.45f), 0.3f);
            if (h.Terrain == "asteroids") col = col.Lerp(new Color(0.3f, 0.27f, 0.24f), 0.3f);
            if (h.Terrain == "star") col = new Color(0.32f, 0.22f, 0.08f);
            col.A = 0.4f;
            for (int i = 0; i < 6; i++)
            {
                Vector3 a = p + Corner(i, 0.97f), b = p + Corner(i + 1, 0.97f);
                st.SetColor(col); st.AddVertex(p);
                st.SetColor(col); st.AddVertex(b);
                st.SetColor(col); st.AddVertex(a);
                // a border where the next sector is held by someone else, in the holder's colour
                (int, int) n = NeighbourAcross(h.C, h.R, i);
                bool edge = !_c.OnMap(n.Item1, n.Item2) || _c.Hex(n.Item1, n.Item2).Owner != h.Owner;
                Color lc = edge && h.Owner != "neutral" ? FacColor(h.Owner) : new Color(0.5f, 0.6f, 0.75f, 0.18f);
                lines.SetColor(lc); lines.AddVertex(p + Corner(i, edge ? 0.93f : 1f) + Vector3.Up * 0.012f);
                lines.SetColor(lc); lines.AddVertex(p + Corner(i + 1, edge ? 0.93f : 1f) + Vector3.Up * 0.012f);
            }
        }
        var mat = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, Roughness = 0.7f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        AddChild(new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = mat, Name = "Tiles" });
        var lmat = new StandardMaterial3D { VertexColorUseAsAlbedo = true, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
        AddChild(new MeshInstance3D { Mesh = lines.Commit(), MaterialOverride = lmat, Name = "Borders" });

        var rng = new Random(17); // the rocks and puffs: the same every time the map is drawn
        foreach (Sector h in _c.S.Sectors)
        {
            Vector3 p = HexPos(h.C, h.R);
            if (h.Terrain == "star")
            {
                var sun = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.42f, Height = 0.84f }, Position = p + Vector3.Up * 0.5f,
                    MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(1, 0.8f, 0.45f), EmissionEnabled = true, Emission = new Color(1, 0.7f, 0.3f), EmissionEnergyMultiplier = 3 } };
                AddChild(sun);
                AddChild(new OmniLight3D { Position = p + Vector3.Up * 0.8f, LightColor = new Color(1, 0.75f, 0.45f), OmniRange = 3, LightEnergy = 1.2f });
            }
            if (h.Terrain == "asteroids")
                for (int i = 0; i < 7; i++)
                {
                    float a = (float)(rng.NextDouble() * Math.Tau), d = 0.2f + (float)rng.NextDouble() * 0.55f, s = 0.05f + (float)rng.NextDouble() * 0.07f;
                    AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = s, Height = s * 1.6f, RadialSegments = 6, Rings = 3 },
                        Position = p + new Vector3(Mathf.Cos(a) * d, 0.12f + (float)rng.NextDouble() * 0.12f, Mathf.Sin(a) * d),
                        RotationDegrees = new Vector3(rng.Next(90), rng.Next(360), 0), MaterialOverride = Rock });
                }
            if (h.Terrain == "nebula")
            {
                Color nc = new(Nebula[(h.C * 7 + h.R * 3) % Nebula.Length]);
                for (int i = 0; i < 3; i++)
                {
                    float s = 1.1f + (float)rng.NextDouble() * 0.6f;
                    var puff = new MeshInstance3D
                    {
                        Mesh = new QuadMesh { Size = new Vector2(s, s) }, Position = p + new Vector3((float)rng.NextDouble() * 0.6f - 0.3f, 0.25f + i * 0.18f, (float)rng.NextDouble() * 0.6f - 0.3f),
                        MaterialOverride = new StandardMaterial3D
                        {
                            AlbedoTexture = Puff, AlbedoColor = new Color(nc, 0.32f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
                            BlendMode = BaseMaterial3D.BlendModeEnum.Add, NoDepthTest = false, DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
                        },
                    };
                    AddChild(puff);
                }
            }
            if (h.Planet != null)
            {
                Color pc = PlanetHue.TryGetValue(h.Planet, out Color hc) ? hc : Color.FromHsv(h.Planet.Aggregate(0, (n, ch) => (n * 31 + ch) & 255) / 255f, 0.4f, 0.75f);
                Node3D ps = Stand(0.35f, false);
                ps.Position = p + new Vector3(0.1f, 0, -0.35f);
                AddChild(ps);
                var ball = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.3f, Height = 0.6f }, Position = p + new Vector3(0.1f, 0.62f, -0.35f),
                    MaterialOverride = new StandardMaterial3D { AlbedoColor = pc, Roughness = 0.8f } };
                AddChild(ball);
                AddLabel(h.Planet, p + new Vector3(0.1f, 1.08f, -0.35f), FacColor(h.Owner).Lightened(0.4f));
            }
            if (h.Station != null)
            {
                Node3D ss = Stand(0.3f, false);
                ss.Position = p + new Vector3(0.1f, 0, -0.35f);
                AddChild(ss);
                var ring = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 0.16f, OuterRadius = 0.26f }, Position = p + new Vector3(0.1f, 0.5f, -0.35f),
                    MaterialOverride = new StandardMaterial3D { AlbedoColor = FacColor(h.Owner).Lightened(0.3f), Metallic = 0.7f, Roughness = 0.35f } };
                var hub = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.06f, BottomRadius = 0.06f, Height = 0.3f }, Position = ring.Position,
                    MaterialOverride = Metal };
                AddChild(ring);
                AddChild(hub);
                AddLabel(h.Station, p + new Vector3(0.1f, 0.92f, -0.35f), FacColor(h.Owner).Lightened(0.4f));
            }
        }
    }

    private void BuildStars()
    {
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = new QuadMesh { Size = new Vector2(0.18f, 0.18f) } };
        var rng = new Random(7);
        mm.InstanceCount = 1600;
        var (mx, mz) = HexGrid.Pos(_c.S.Cols, _c.S.Rows);
        for (int i = 0; i < mm.InstanceCount; i++)
        {
            var pos = new Vector3((float)(rng.NextDouble() * (mx + 80) - 40), -6 - (float)rng.NextDouble() * 40, (float)(rng.NextDouble() * (mz + 80) - 40));
            mm.SetInstanceTransform(i, new Transform3D(Basis.Identity.Scaled(Vector3.One * (0.5f + (float)rng.NextDouble() * 1.5f)), pos));
            float b = 0.4f + (float)rng.NextDouble() * 0.6f;
            mm.SetInstanceColor(i, new Color(b * 0.85f, b * 0.9f, b));
        }
        AddChild(new MultiMeshInstance3D
        {
            Multimesh = mm, Name = "Stars",
            MaterialOverride = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
                AlbedoTexture = Puff, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            },
        });
    }

    private static Texture2D? _puff;
    /// <summary>A soft round spot, for nebula puffs and stars.</summary>
    private static Texture2D Puff
    {
        get
        {
            if (_puff != null) return _puff;
            var g = new Gradient();
            g.SetColor(0, new Color(1, 1, 1, 1));
            g.SetColor(1, new Color(1, 1, 1, 0));
            _puff = new GradientTexture2D { Gradient = g, Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1, 0.5f), Width = 64, Height = 64 };
            return _puff;
        }
    }

    private static readonly StandardMaterial3D Rock = new() { AlbedoColor = new Color(0.42f, 0.38f, 0.34f), Roughness = 0.95f };
    private static readonly StandardMaterial3D Metal = new() { AlbedoColor = new Color(0.62f, 0.66f, 0.72f), Metallic = 0.8f, Roughness = 0.3f };
    private static readonly StandardMaterial3D Gold = new() { AlbedoColor = new Color(0.95f, 0.72f, 0.25f), Metallic = 0.9f, Roughness = 0.25f, EmissionEnabled = true, Emission = new Color(0.35f, 0.22f, 0.02f) };

    private static Vector3 Corner(int i, float k) { float a = Mathf.Pi / 3 * i; return new Vector3(Mathf.Cos(a) * k, 0, Mathf.Sin(a) * k); }

    // the sector across corner i's edge (between corners i and i + 1) of a flat-topped hex
    private static (int, int) NeighbourAcross(int c, int r, int i)
    {
        Vector3 mid = HexPos(c, r) + (Corner(i, 1) + Corner(i + 1, 1)) * 0.5f * 2;
        return HexGrid.At(mid.X, mid.Z);
    }

    private MeshInstance3D Ring(float r, float w, Color col)
    {
        var t = new TorusMesh { InnerRadius = r - w, OuterRadius = r, Rings = 6, RingSegments = 6 };
        return new MeshInstance3D { Mesh = t, Scale = new Vector3(1, 0.05f, 1),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = col, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha } };
    }

    /// <summary>A flight stand: a base disc and a pole (gold for your fleet).</summary>
    private static Node3D Stand(float height, bool gold)
    {
        var n = new Node3D();
        Material m = gold ? Gold : Metal;
        n.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.16f, BottomRadius = 0.19f, Height = 0.04f }, Position = new Vector3(0, 0.02f, 0), MaterialOverride = m });
        n.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.012f, BottomRadius = 0.012f, Height = height }, Position = new Vector3(0, height / 2, 0), MaterialOverride = m });
        return n;
    }

    private Label3D AddLabel(string text, Vector3 at, Color col, Node3D? parent = null, int size = 40)
    {
        var l = new Label3D { Text = text, Position = at, Modulate = col, FontSize = size, PixelSize = 0.004f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            OutlineSize = 10, OutlineModulate = new Color(0, 0, 0, 0.85f), NoDepthTest = false };
        (parent ?? this).AddChild(l);
        return l;
    }

    // ---- The pieces (rebuilt after every move) --------------------------------------------------------------------------

    private static readonly Dictionary<string, PackedScene?> Models = new();

    /// <summary>The model a faction's fleet stands as: its approved ship, or a stand-in wedge.</summary>
    private Node3D ShipPiece(string fac, int tier, float size)
    {
        string? path = fac switch
        {
            "concord" => tier >= 3 ? "res://assets/ships/concord/albatross.glb" : "res://assets/ships/concord/petrel_level.glb",
            "krai" => tier >= 3 ? "res://assets/ships/krai/cruiser.glb" : "res://assets/ships/krai/frigate.glb",
            _ => null,
        };
        Node3D model;
        if (path != null)
        {
            if (!Models.TryGetValue(path, out PackedScene? ps)) Models[path] = ps = ResourceLoader.Exists(path) ? GD.Load<PackedScene>(path) : null;
            model = ps?.Instantiate<Node3D>() ?? Wedge(fac);
            if (ps != null)
            {
                Aabb box = Bounds(model);
                float len = Math.Max(box.Size.Z, Math.Max(box.Size.X, 0.01f));
                model.Scale = Vector3.One * (0.62f * size / len);
                // the glb's bow is -z; a standee's bow is +z (the map's headings)
                model.RotationDegrees = new Vector3(0, 180, 0);
            }
        }
        else model = Wedge(fac);
        var holder = new Node3D();
        holder.AddChild(model);
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

    private Node3D Wedge(string fac)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 n = new(0, 0, 0.3f), l = new(-0.16f, 0, -0.22f), r = new(0.16f, 0, -0.22f), t = new(0, 0.07f, -0.1f);
        foreach (var tri in new[] { (n, l, t), (n, t, r), (l, r, t), (n, r, l) }) { st.AddVertex(tri.Item1); st.AddVertex(tri.Item2); st.AddVertex(tri.Item3); }
        st.GenerateNormals();
        return new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = new StandardMaterial3D { AlbedoColor = FacColor(fac).Lightened(0.15f), Metallic = 0.5f, Roughness = 0.4f } };
    }

    private int PiecesIn(int c, int r) =>
        (_c.S.AtC == c && _c.S.AtR == r ? 1 : 0) + (_c.Hex(c, r).IsPort ? 1 : 0) + _c.S.Groups.Count(g => g.C == c && g.R == r)
        + _c.S.Missions.Count(m => m.C == c && m.R == r) + _c.S.Freighters.Count(f => f.Route[f.I][0] == c && f.Route[f.I][1] == r && _c.SideOf(f.Fac) == "own");

    // where a piece stands in its sector: centred alone, at its own slot when the sector is shared (owner, 2026-11-05)
    private Vector3 Slot(string role, int c, int r)
    {
        Vector3 p = HexPos(c, r);
        if (PiecesIn(c, r) <= 1) return p;
        return p + role switch
        {
            "fleet" => new Vector3(-0.45f, 0, 0.45f), "ship" => new Vector3(0.45f, 0, 0.42f), "freighter" => new Vector3(0.66f, 0, -0.12f),
            "mission" => new Vector3(-0.5f, 0, -0.28f), _ => Vector3.Zero,
        };
    }

    /// <summary>Redraws every piece from the campaign; moves glide the fleets that moved from where they were.</summary>
    public void Rebuild(IEnumerable<FleetMove>? moves = null, List<(int C, int R)>? ownSteps = null)
    {
        var was = _fleetNodes.ToDictionary(kv => kv.Key, kv => kv.Value.Position);
        foreach (Node n in _pieces.GetChildren()) n.QueueFree();
        foreach (Node n in _routes.GetChildren()) n.QueueFree();
        _fleetNodes.Clear();
        _banners.Clear();

        // your fleet: the flagship's model on a gold stand
        var own = new Node3D { Name = "OwnFleet" };
        own.AddChild(Stand(0.42f, true));
        Hull flag = _c.Data.Hulls[_c.S.Fleet[0].Hull];
        Node3D ship = ShipPiece(_c.S.Side, flag.Tier, 1.1f);
        ship.Position = new Vector3(0, 0.46f, 0);
        ship.Rotation = new Vector3(0, (float)_c.S.Dir, 0);
        own.AddChild(ship);
        own.AddChild(Flag(_c.S.Side, _c.S.Fleet.Count, new Color(1, 0.85f, 0.35f)));
        own.Position = Slot("fleet", _c.S.AtC, _c.S.AtR);
        _pieces.AddChild(own);
        _own = own;
        _fleetNodes["__own"] = own;

        foreach (FactionFleet g in _c.S.Groups)
        {
            var n = new Node3D { Name = g.Name };
            n.AddChild(Stand(0.36f, false));
            Node3D m = ShipPiece(g.Fac, g.Commander ? 3 : g.Size >= 3 ? 3 : 2, 0.85f);
            m.Position = new Vector3(0, 0.4f, 0);
            m.Rotation = new Vector3(0, (float)g.Dir, 0);
            n.AddChild(m);
            n.AddChild(Flag(g.Fac, g.Size, FacColor(g.Fac)));
            n.Position = Slot("ship", g.C, g.R);
            _pieces.AddChild(n);
            _fleetNodes[g.Name] = n;
        }

        // freighters and their routes: your own, and your allies' routes (never a rival's)
        foreach (Freighter f in _c.S.Freighters)
        {
            string? side = _c.SideOf(f.Fac);
            if (side == null) continue;
            var pts = f.Route.Select(k => HexPos(k[0], k[1]) + Vector3.Up * 0.05f).ToList();
            _routes.AddChild(Dashed(pts, new Color(FacColor(f.Fac).Lightened(0.3f), side == "own" ? 0.85f : 0.45f)));
            if (side != "own") continue;
            var fr = new Node3D { Name = "Freighter " + f.Name };
            fr.AddChild(Stand(0.22f, false));
            var hull = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.1f, 0.08f, 0.3f) }, Position = new Vector3(0, 0.27f, 0),
                Rotation = new Vector3(0, (float)f.Dir, 0), MaterialOverride = new StandardMaterial3D { AlbedoColor = FacColor(f.Fac).Lightened(0.45f), Roughness = 0.5f } };
            fr.AddChild(hull);
            fr.Position = Slot("freighter", f.Route[f.I][0], f.Route[f.I][1]);
            _pieces.AddChild(fr);
        }

        // missions: a banner on a pole in its sector, story blue, accepted green, on offer amber
        foreach (Offer m in _c.S.Missions)
        {
            MissionScript def = _c.ScriptOf(m);
            Color ink = def.Story ? new Color("#8fd0ff") : _c.S.Active.Contains(m.Id) ? new Color("#7dffa0") : new Color("#ffb547");
            var b = new Node3D { Name = "Mission " + m.Id };
            b.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.012f, BottomRadius = 0.012f, Height = 0.75f }, Position = new Vector3(0, 0.375f, 0), MaterialOverride = Metal });
            var cloth = new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(0.3f, 0.22f), CenterOffset = new Vector3(0.15f, 0, 0) }, Position = new Vector3(0.012f, 0.62f, 0),
                MaterialOverride = new StandardMaterial3D { AlbedoColor = ink, CullMode = BaseMaterial3D.CullModeEnum.Disabled, Roughness = 0.8f, EmissionEnabled = true, Emission = ink * 0.25f } };
            b.AddChild(cloth);
            _banners.Add((cloth, m.Id * 1.3f));
            AddLabel(def.Glyph, new Vector3(0.15f, 0.62f, 0.01f), new Color(0.05f, 0.05f, 0.08f), cloth, 64).Billboard = BaseMaterial3D.BillboardModeEnum.Disabled;
            b.Position = Slot("mission", m.C, m.R);
            _pieces.AddChild(b);
        }

        // glide what moved from where it stood
        if (moves != null)
            foreach (FleetMove mv in moves)
                if (_fleetNodes.TryGetValue(mv.Name, out Node3D? n) && was.TryGetValue(mv.Name, out Vector3 from)) { _glides[mv.Name] = (from, n.Position, 0); n.Position = from; }
        if (ownSteps is { Count: > 1 } && was.TryGetValue("__own", out Vector3 of)) { _glides["__own"] = (of, own.Position, 0); own.Position = of; }
    }

    /// <summary>A strength flag beside a stand: the number of ships on a small banner in the faction's colour.</summary>
    private Node3D Flag(string fac, int n, Color col)
    {
        var f = new Node3D { Position = new Vector3(0.22f, 0, 0.05f) };
        f.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.008f, BottomRadius = 0.008f, Height = 0.5f }, Position = new Vector3(0, 0.25f, 0), MaterialOverride = Metal });
        var cloth = new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(0.2f, 0.14f), CenterOffset = new Vector3(0.1f, 0, 0) }, Position = new Vector3(0.008f, 0.42f, 0),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = col, CullMode = BaseMaterial3D.CullModeEnum.Disabled, Roughness = 0.8f } };
        f.AddChild(cloth);
        _banners.Add((cloth, n * 0.7f + fac.Length));
        var l = AddLabel(n.ToString(), new Vector3(0.1f, 0, 0.012f), Colors.White, cloth, 48);
        l.Billboard = BaseMaterial3D.BillboardModeEnum.Disabled;
        return f;
    }

    /// <summary>A dashed line through points on the map.</summary>
    private static MeshInstance3D Dashed(List<Vector3> pts, Color col, float dash = 0.16f, float gap = 0.12f)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Lines);
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], b = pts[i + 1];
            float len = a.DistanceTo(b);
            for (float t = 0; t < len; t += dash + gap)
            {
                st.AddVertex(a.Lerp(b, t / len));
                st.AddVertex(a.Lerp(b, Math.Min(len, t + dash) / len));
            }
        }
        if (pts.Count < 2) { st.AddVertex(Vector3.Zero); st.AddVertex(Vector3.Zero); }
        return new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = new StandardMaterial3D { AlbedoColor = col, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha } };
    }

    /// <summary>Shows a route by turn: this turn's leg bright, later legs fainter, a numbered badge where each turn ends.</summary>
    public void ShowRoute(Route? route)
    {
        foreach (Node n in _path.GetChildren()) n.QueueFree();
        if (route == null) return;
        Vector3 start = HexPos(_c.S.AtC, _c.S.AtR) + Vector3.Up * 0.08f;
        for (int i = 0; i < route.Legs.Count; i++)
        {
            var leg = route.Legs[i];
            if (leg.Count == 0) continue;
            var pts = new List<Vector3> { start };
            pts.AddRange(leg.Select(k => HexPos(k.C, k.R) + Vector3.Up * 0.08f));
            float a = i == 0 ? 1f : Math.Max(0.3f, 0.7f - 0.15f * i);
            _path.AddChild(Dashed(pts, new Color(1, 0.86f, 0.4f, a), 0.22f, 0.06f));
            var badge = new Label3D { Text = (i + 1).ToString(), Position = pts[^1] + Vector3.Up * 0.35f, FontSize = 56, PixelSize = 0.004f,
                Modulate = i == 0 ? new Color(1, 0.86f, 0.4f) : new Color(0.85f, 0.85f, 0.9f, a), OutlineSize = 12, OutlineModulate = new Color(0, 0, 0, 0.9f),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled };
            _path.AddChild(badge);
            start = pts[^1];
        }
    }

    /// <summary>The screen point of a sector's centre (for the HUD to anchor to), or null behind the camera.</summary>
    public Vector2? ScreenOf(int c, int r)
    {
        Vector3 p = HexPos(c, r);
        return _cam.IsPositionBehind(p) ? null : _cam.UnprojectPosition(p);
    }
}
