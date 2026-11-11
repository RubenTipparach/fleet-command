// FactionStage: the faction screen's 3D scene (mockup 1, docs/mockups/game-flow.html buildFactionScene; owner, 2026-11-06: "I
// need the light with the dust particle, and a wooden table, selecting a faction shines a light, and the emblem hovers a bit
// and turns with my mouse movement"). A walnut table with each playable faction's badge floating over a brass ring let into
// it, a lamp above each with its beam and the dust in it. Every shape and number is the page's, exported by
// tools/mockups/export_faction.mjs (assets/scenes/faction/: the table and badge glbs and manifest.json); this class only puts
// them together and moves them: the lamps' levels (full on the picked faction, about half under the pointer), the badges' bob
// and their turn toward the pointer. It runs in its own world inside FactionView's SubViewport; FactionScreen draws the
// labels and keys over it and tells it where the pointer is.
using System;
using System.Collections.Generic;
using System.Text.Json;
using FleetCommand.Game.View;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Flow;

public sealed partial class FactionStage : Node3D
{
    private const string Dir = "res://assets/scenes/faction/";

    private sealed class Slot
    {
        public string Fac = "";
        public Node3D Root = null!, Holder = null!;
        public SpotLight3D Lamp = null!;
        public ShaderMaterial Beam = null!, Dust = null!;
        public float On, Yaw, Pitch;
        public Vector3 Home;   // its place on the full table
        public Node3D? Inlay;  // its brass ring in the table (table.glb's inlay_<faction>), and that ring's place
        public Vector3 InlayHome;
    }

    private readonly List<Slot> _slots = new();
    private readonly List<ShaderMaterial> _holo = new();
    private Camera3D _cam = null!;
    private float _t, _float, _bob, _lift, _fitW;
    private Vector3 _camAt, _camTarget;
    private float _yawK, _pitchK, _maxYaw, _maxPitch, _others, _rate, _bobRate, _bobPhase;

    /// <summary>The faction under the pointer (its index), or -1.</summary>
    public int Hover = -1;
    /// <summary>The picked faction, whose lamp is full on.</summary>
    public string? Picked;
    /// <summary>The pointer in the view's normalised coordinates (-1 to 1, y up).</summary>
    public Vector2 Pointer;
    /// <summary>The screen the stage is seen on, pixels (FactionScreen's size): the camera is fitted to it.</summary>
    public Vector2 ScreenSize;
    /// <summary>A still: time stands at 6 s and every level and turn is where it is heading (the page's factionShot).</summary>
    public bool Frozen;
    public IReadOnlyList<string> Factions => _slots.ConvertAll(s => s.Fac);
    /// <summary>Which badges stand on the table (Options, unfinished factions: off shows the factions in the game, owner
    /// 2026-11-09). The rest are hidden, and the shown ones moved so their rows sit round the table's middle.</summary>
    public Func<string, bool> Keep = _ => true;
    /// <summary>Whether a badge is on the table now.</summary>
    public bool Shown(int i) => i >= 0 && i < _slots.Count && _slots[i].Root.Visible;

    public void Build()
    {
        Name = "FactionStage";
        using JsonDocument doc = JsonDocument.Parse(FileAccess.GetFileAsString(Dir + "manifest.json"));
        JsonElement m = doc.RootElement;
        Vector3 V(JsonElement e) => new(e[0].GetSingle(), e[1].GetSingle(), e[2].GetSingle());
        JsonElement table = m.GetProperty("table"), turn = m.GetProperty("turn"), cam = m.GetProperty("camera");
        _float = table.GetProperty("float").GetSingle();
        _bob = table.GetProperty("bob").GetSingle();
        _lift = table.GetProperty("lift").GetSingle();
        _fitW = table.GetProperty("fitW").GetSingle();
        _yawK = turn.GetProperty("yaw_per_ndc").GetSingle(); _pitchK = turn.GetProperty("pitch_per_ndc").GetSingle();
        _maxYaw = turn.GetProperty("max_yaw_rad").GetSingle(); _maxPitch = turn.GetProperty("max_pitch_rad").GetSingle();
        _others = turn.GetProperty("others").GetSingle(); _rate = turn.GetProperty("rate_per_s").GetSingle();
        _bobRate = turn.GetProperty("bob_rate").GetSingle(); _bobPhase = turn.GetProperty("bob_phase").GetSingle();

        // the camera, and a dim hall: the verdigris nebula behind, a cold rim from behind, a faint fill
        _cam = new Camera3D { Fov = cam.GetProperty("fov_deg").GetSingle(), Near = 0.1f, Far = 4000, Current = true };
        AddChild(_cam);
        _camAt = V(cam.GetProperty("position"));
        _camTarget = V(cam.GetProperty("target"));
        _cam.LookAtFromPosition(_camAt, _camTarget, Vector3.Up);
        // what the metal reflects is a soft studio (the page's FCEmblemStudio: a bright overhead, a grey horizon, a dark
        // floor), not the space behind, which the NebulaSky draws
        var studio = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color("#c8ccd4"), SkyHorizonColor = new Color("#6c7078"), SkyCurve = 0.25f, SkyEnergyMultiplier = 1.0f,
            GroundBottomColor = new Color("#0c0b0a"), GroundHorizonColor = new Color("#3a3833"), GroundCurve = 0.1f,
        };
        AddChild(new WorldEnvironment { Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("#020306"),
            Sky = new Sky { SkyMaterial = studio, RadianceSize = Sky.RadianceSizeEnum.Size128 },
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color("#8aa6d8"), AmbientLightEnergy = 0.12f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Sky,
            GlowEnabled = true, GlowIntensity = 0.4f, GlowBloom = 0.03f, TonemapMode = Godot.Environment.ToneMapper.Filmic,
        } });
        var rim = new DirectionalLight3D { LightColor = new Color("#a9c4ff"), LightEnergy = 0.35f, Name = "Rim" };
        AddChild(rim);
        rim.LookAtFromPosition(new Vector3(-6, 5, -8), Vector3.Zero, Vector3.Up);
        NebulaSky sky = NebulaSky.Load("verdigris");
        AddChild(sky);
        sky.Follow(_cam.GlobalPosition);

        // the table, its wood from the table_wood graph's maps
        Node3D tbl = GD.Load<PackedScene>(Dir + "table.glb").Instantiate<Node3D>();
        AddChild(tbl);
        var wood = Wood(Colors.White, 0.35f, 0.28f);
        var under = Wood(new Color(0.55f, 0.5f, 0.46f), 0.2f, 0.4f);
        GraphMaterial.Dress(tbl, name => name switch { "table_wood" => wood, "table_under" => under, _ => null });

        // each badge's materials: its Material Maker graph's maps, or the holographic field
        var badgeMats = new Dictionary<string, Material?>();
        foreach (JsonProperty p in m.GetProperty("badge_materials").EnumerateObject()) badgeMats[p.Name] = BadgeMaterial(p.Value);

        JsonElement lamp = m.GetProperty("lamp"), beam = m.GetProperty("beam");
        float angle = lamp.GetProperty("angle_rad").GetSingle(), coneR = beam.GetProperty("cone_radius_m").GetSingle(), coneL = beam.GetProperty("cone_length_m").GetSingle();
        int motes = m.GetProperty("dust").GetProperty("count").GetInt32();
        Shader beamSh = GD.Load<Shader>("res://src/Game/Flow/faction_beam.gdshader"), dustSh = GD.Load<Shader>("res://src/Game/Flow/faction_dust.gdshader");
        int k = 0;
        foreach (JsonElement s in m.GetProperty("slots").EnumerateArray())
        {
            string fac = s.GetProperty("faction").GetString()!;
            var slot = new Slot { Fac = fac, Root = new Node3D { Name = fac, Position = new Vector3(s.GetProperty("x").GetSingle(), 0, s.TryGetProperty("z", out JsonElement z) ? z.GetSingle() : 0) } };
            slot.Home = slot.Root.Position;
            if (tbl.FindChild("inlay_" + fac, true, false) is Node3D inl) { slot.Inlay = inl; slot.InlayHome = inl.Position; }
            AddChild(slot.Root);
            slot.Holder = new Node3D { Name = "Holder", Position = new Vector3(0, _float, 0) };
            slot.Root.AddChild(slot.Holder);
            var tilt = new Node3D { Name = "Tilt", Rotation = new Vector3(s.GetProperty("badge_tilt_rad").GetSingle(), 0, 0) };
            slot.Holder.AddChild(tilt);
            Node3D badge = GD.Load<PackedScene>(Dir + fac + ".glb").Instantiate<Node3D>();
            tilt.AddChild(badge);
            GraphMaterial.Dress(badge, name =>
            {
                if (name == "holo_field") { var h = new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/Flow/holo_field.gdshader") }; _holo.Add(h); return h; }
                return badgeMats.GetValueOrDefault(name);
            });

            // the lamp, aimed through the badge onto the wood; its beam from the lamp to the table, and the dust in it
            Vector3 tip = V(s.GetProperty("lamp").GetProperty("position")), at = V(s.GetProperty("lamp").GetProperty("target"));
            slot.Lamp = new SpotLight3D
            {
                Name = "Lamp", LightColor = new Color(lamp.GetProperty("colour").GetString()!), SpotRange = lamp.GetProperty("range_m").GetSingle(),
                SpotAngle = Mathf.RadToDeg(angle), SpotAngleAttenuation = 1.6f, SpotAttenuation = lamp.GetProperty("decay").GetSingle(), ShadowEnabled = true,
            };
            AddChild(slot.Lamp);
            slot.Lamp.LookAtFromPosition(tip, at, Vector3.Up);
            Vector3 aim = (at - tip).Normalized();
            slot.Beam = new ShaderMaterial { Shader = beamSh };
            slot.Beam.SetShaderParameter("colour", new Color(beam.GetProperty("colour").GetString()!));
            var cone = new MeshInstance3D
            {
                Name = "Beam", Mesh = new CylinderMesh { TopRadius = 0, BottomRadius = coneR, Height = coneL, RadialSegments = 48, Rings = 1, CapTop = false, CapBottom = false },
                MaterialOverride = slot.Beam, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            AddChild(cone);
            // the cylinder's axis is its +Y, its tip at the top: turn -Y onto the aim and put its middle half way down the beam
            cone.Basis = new Basis(new Quaternion(Vector3.Down, aim));
            cone.Position = tip + aim * (coneL / 2);
            slot.Dust = new ShaderMaterial { Shader = dustSh };
            slot.Dust.SetShaderParameter("tip", tip);
            slot.Dust.SetShaderParameter("axis", aim);
            slot.Dust.SetShaderParameter("cos_cone", Mathf.Cos(angle));
            AddChild(new MeshInstance3D { Name = "Dust", Mesh = Motes(tip, aim, angle, coneL, motes, 77UL + (ulong)k), MaterialOverride = slot.Dust,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, ExtraCullMargin = 16 });
            _slots.Add(slot);
            k++;
        }
    }

    // A wood material on the table_wood maps (the glb's uvs already count tiles).
    private static OrmMaterial3D Wood(Color tint, float coat, float coatRough) => new()
    {
        AlbedoTexture = GD.Load<Texture2D>("res://assets/textures/materials/table_wood/albedo.png"), AlbedoColor = tint,
        NormalEnabled = true, NormalTexture = GD.Load<Texture2D>("res://assets/textures/materials/table_wood/normal.png"),
        OrmTexture = GD.Load<Texture2D>("res://assets/textures/materials/table_wood/orm.png"), Roughness = 1, Metallic = 1,
        ClearcoatEnabled = coat > 0, Clearcoat = coat, ClearcoatRoughness = coatRough, TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
        // the page dims the studio in the wood to a twentieth (envMapIntensity 0.05); Godot has no per-material environment
        // strength, so the wood takes none of it: the lamps and the rim light it, and its pools of lamplight are the look
        DisableAmbientLight = true,
    };

    // A badge material from the manifest: its graph's maps, tinted, at its uv scale; or its own colour where it has no graph.
    private static Material BadgeMaterial(JsonElement e)
    {
        Color C(string k) { JsonElement a = e.GetProperty(k); return new Color(a[0].GetSingle(), a[1].GetSingle(), a[2].GetSingle()); }
        float F(string k) => e.GetProperty(k).GetSingle();
        // the page's colours are linear (its three.js reads them so); Godot's are sRGB
        var mat = new OrmMaterial3D { AlbedoColor = C("tint").LinearToSrgb(), TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic };
        if (e.GetProperty("graph").ValueKind == JsonValueKind.String)
        {
            float uv = F("uv_scale");
            mat = GraphMaterial.Make(e.GetProperty("graph").GetString()!, C("tint").LinearToSrgb(), new Vector2(uv, uv), F("normal_scale"));
        }
        else { mat.Roughness = F("roughness"); mat.Metallic = F("metalness"); }
        if (F("clearcoat") > 0) { mat.ClearcoatEnabled = true; mat.Clearcoat = F("clearcoat"); mat.ClearcoatRoughness = F("clearcoat_roughness"); }
        if (F("sheen") > 0) { mat.RimEnabled = true; mat.Rim = 0.5f * F("sheen"); mat.RimTint = 0.6f; }
        Color em = C("emissive");
        if (em.R + em.G + em.B > 0) { mat.EmissionEnabled = true; mat.Emission = em.LinearToSrgb(); mat.EmissionEnergyMultiplier = F("emissive_strength"); }
        return mat;
    }

    // The motes in a beam (the page's dust: each at a depth down the cone and a radius inside it), their seeds in UV.x.
    private static ArrayMesh Motes(Vector3 tip, Vector3 aim, float angle, float len, int n, ulong seed)
    {
        var r = Rng.Fork(seed, "faction-dust");
        var q = new Quaternion(Vector3.Down, aim);
        var pos = new Vector3[n];
        var uv = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float t = 0.15f + (float)r.Next() * 0.85f, a = (float)r.Next() * Mathf.Tau, rr = Mathf.Sqrt((float)r.Next()) * Mathf.Tan(angle) * t * len;
            pos[i] = tip + q * new Vector3(Mathf.Cos(a) * rr, -t * len, Mathf.Sin(a) * rr);
            uv[i] = new Vector2((float)r.Next(), 0);
        }
        var arr = new Godot.Collections.Array();
        arr.Resize((int)Mesh.ArrayType.Max);
        arr[(int)Mesh.ArrayType.Vertex] = pos;
        arr[(int)Mesh.ArrayType.TexUV] = uv;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, arr);
        return mesh;
    }

    /// <summary>The camera backs off along its line to the table's middle until the badges fit across it (fitW either side
    /// of the middle; the page's fitFacCam).</summary>
    private void FitCamera(float aspect)
    {
        Vector3 dir = _camAt - _camTarget;
        float d = Math.Max(dir.Length(), _fitW / (Mathf.Tan(Mathf.DegToRad(_cam.Fov) / 2) * aspect));
        _cam.LookAtFromPosition(_camTarget + dir.Normalized() * d, _camTarget, Vector3.Up);
    }

    /// <summary>A point in the scene on a screen of this size (pixels, y down), or null behind the camera. Worked from the
    /// camera's own pose and field of view, not its viewport, so it holds before the view has a size (a headless run).</summary>
    public Vector2? Project(Vector3 p, Vector2 size)
    {
        if (_cam == null || size.X < 1 || size.Y < 1) return null;
        Vector3 v = _cam.GlobalTransform.AffineInverse() * p;
        if (v.Z > -0.01f) return null;
        float f = 1 / Mathf.Tan(Mathf.DegToRad(_cam.Fov) / 2), x = v.X * f / (size.X / size.Y) / -v.Z, y = v.Y * f / -v.Z;
        return new Vector2((x + 1) / 2 * size.X, (1 - y) / 2 * size.Y);
    }

    /// <summary>Where a faction's badge sits on a screen of this size: a square round its centre, or null behind the camera.</summary>
    public Rect2? ScreenRect(int i, Vector2 size)
    {
        if (!Shown(i)) return null;
        Vector3 c = _slots[i].Holder.GlobalPosition;
        if (Project(c, size) is not { } p || Project(c + _cam.GlobalBasis.X * 0.62f, size) is not { } e) return null;
        float r = p.DistanceTo(e);
        return new Rect2(p - new Vector2(r, r), new Vector2(2 * r, 2 * r));
    }

    // the badges Keep allows on the table, their rows moved so the middle of them is the table's middle
    private void Restage()
    {
        float zMid = 0;
        int n = 0;
        foreach (Slot s in _slots) if (Keep(s.Fac)) { zMid += s.Home.Z; n++; }
        zMid = n > 0 ? zMid / n : 0;
        foreach (Slot s in _slots)
        {
            s.Root.Visible = Keep(s.Fac);
            s.Root.Position = s.Home - new Vector3(0, 0, zMid);
            if (s.Inlay is { } inl)
            {
                inl.Visible = s.Root.Visible;
                // the ring moves with its badge, in the table's own space (the inlay is the table's child)
                inl.Position = s.InlayHome - inl.GetParent<Node3D>().GlobalBasis.Inverse() * new Vector3(0, 0, zMid);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_slots.Count == 0) return;
        float dt = Frozen ? 0 : (float)Math.Min(delta, 0.05);
        _t = Frozen ? 6 : _t + dt;
        Vector2 vp = ScreenSize.X >= 1 && ScreenSize.Y >= 1 ? ScreenSize : GetViewport().GetVisibleRect().Size;
        if (vp.X < 1 || vp.Y < 1) vp = new Vector2(1920, 1080);
        FitCamera(vp.X / vp.Y);
        Restage();
        int chosen = Picked == null ? -1 : _slots.FindIndex(s => s.Fac == Picked);
        for (int i = 0; i < _slots.Count; i++)
        {
            Slot s = _slots[i];
            // the lamp: full on the picked faction, about half on the one under the pointer
            float want = i == chosen ? 1 : i == Hover ? 0.55f : 0;
            s.On = Frozen ? want : s.On + (want - s.On) * Math.Min(1, dt * (want > s.On ? 5 : 2.5f));
            // the page's intensity is in three.js 0.147's legacy light units, about pi times a physical (and Godot) unit's
            // light, so the pool on the wood is as warm and bright as the page's (owner, 2026-11-06: "that light cone is wrong")
            s.Lamp.LightEnergy = (0.25f + 6.5f * s.On) * Mathf.Pi;
            s.Lamp.ShadowEnabled = s.On > 0.05f;   // only a lit lamp casts shadows: thirteen shadowed lamps are too many for a phone (the page's stepFaction)
            s.Beam.SetShaderParameter("level", s.On);
            s.Dust.SetShaderParameter("level", s.On);
            s.Dust.SetShaderParameter("t", _t);
            s.Dust.SetShaderParameter("px", vp.Y / 1080f);
            // the badge floats, bobbing on its own phase, rising a little higher while its lamp is on
            s.Holder.Position = new Vector3(0, _float + Mathf.Sin(_t * _bobRate + i * _bobPhase) * _bob + _lift * s.On, 0);
            // every badge turns toward the pointer, the one under it most
            Vector2 c = Project(s.Holder.GlobalPosition, vp) ?? vp / 2;
            Vector2 cn = new(c.X / vp.X * 2 - 1, 1 - c.Y / vp.Y * 2);
            float kk = i == Hover ? 1 : _others;
            float yaw = Math.Clamp((Pointer.X - cn.X) * _yawK * kk, -_maxYaw, _maxYaw), pitch = Math.Clamp(-(Pointer.Y - cn.Y) * _pitchK * kk, -_maxPitch, _maxPitch);
            float f = Frozen ? 1 : Math.Min(1, dt * _rate);
            s.Yaw += (yaw - s.Yaw) * f;
            s.Pitch += (pitch - s.Pitch) * f;
            s.Holder.Rotation = new Vector3(s.Pitch, s.Yaw, 0);
        }
        foreach (ShaderMaterial h in _holo) h.SetShaderParameter("time", Frozen ? 4f : _t);
    }
}
