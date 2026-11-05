// BattleView: the 3D view of the battle (CLAUDE.md section 7): the ships, the stars and the light, and the camera. The
// camera orbits the ship you command by the player's drag only, never snapping or turning on its own (section 8), and
// its projection is shifted so that ship sits at a fixed height in the view rectangle at every zoom (battle-console
// design: 74 % down the view), without moving the camera. The 3D renders full screen behind the console, and the
// projection centres it on the view rectangle, so an expanded panel moves the battle to the right third and it keeps
// playing. A class with a model is drawn as its approved ship (assets/ships/, exported from the ship mockups: the Concord
// line, mockup 6, and the Krai line, mockup 7), scaled to the class's battle length; the factions whose ships are not
// designed yet keep their stand-ins (scenes/standins). The sky is the scenario's own nebula (NebulaSky).
// The camera lock and Target view (battle-console design 4e and 4n; owner, 2026-11-02: "camera needs to yaw to face the
// enemy target", "we'll want to smooth that rotation", "break camera lock into two buttons, target view ... camera
// lock, focuses camera on the player from target view"; 2026-11-04: "camera lock is mandatory"): locking a target locks
// the camera, which then turns round your ship toward the target on a critically damped spring (it eases in and out
// and never overshoots), keeping the player's pitch and zoom; a drag frees it, and the Camera lock key sets it again.
// Target view glides the orbit's centre to the target (and on to the next one locked) and back when switched off. Both
// are the player's to set; the camera is never turned against their will (CLAUDE.md 8).
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Game.Ui;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class BattleView : Node3D
{
    /// <summary>Metres per scene unit (the mockup's scale: 1 unit is 100 m).</summary>
    public const float MPerU = 100f;

    private ConsoleContext _x = null!;
    private Camera3D _cam = null!;
    private readonly Dictionary<string, Node3D> _models = new();
    private readonly List<OmniLight3D> _strobes = new();
    private readonly Dictionary<string, ShipDamage> _damage = new();
    private readonly Dictionary<string, ShieldShell> _shields = new();
    private long _hitSeq;
    private NebulaSky? _sky;
    private DirectionalLight3D _sun = null!;
    private ShaderMaterial _skyMat = null!;

    /// <summary>How much of the hulls' ambient light comes from the nebula (the rest is the old cool blue fill).</summary>
    public const float AmbientSkyShare = 1f;
    /// <summary>The ambient light's strength, all of it from the nebula sky (the same baked sky the hulls reflect): 15 %
    /// (owner, 2026-11-04: "also tune down ambient lighting", "which is the reflection probe (ambient light 10-20% str"), so
    /// a hull's shadowed side falls dark in deep space, tinted by its nebula, and its floodlight pools carry it.</summary>
    public const float AmbientEnergy = 0.15f;
    private Rect2 _view;
    private Vector2 _screen;

    /// <summary>Camera orbit: yaw round the ship (radians, 0 looking north from the south), pitch above the plane, distance in units.</summary>
    public float Yaw, Pitch = 0.367f, Dist = 13.9f;

    /// <summary>The camera lock's spring: natural rate (per second) and fastest turn (degrees a second), the mockup's CAM_LOCK.</summary>
    public const float LockOmegaS = 2.5f, LockMaxDegS = 90f;
    /// <summary>How long Target view's glide to a ship (or back) takes, seconds.</summary>
    public const float GlideS = 1.1f;

    /// <summary>The camera turns to keep the target ahead of your ship.</summary>
    public bool CamLock { get; private set; }
    /// <summary>The camera orbits the target instead of your ship.</summary>
    public bool TargetView { get; private set; }
    private float _yawVel;
    private string? _lastTarget;
    private string? _watched;   // the ship Target view last orbited: it stays on its wreck until the next lock
    private Vector3 _focus, _glideFrom;
    private float _glideT = GlideS;
    private bool _focusSet;

    public void Init(ConsoleContext ctx)
    {
        _x = ctx;
        Name = "BattleView";
        // the environment's sky: the scenario's nebula as a baked panorama, for the hulls' reflections and part of their
        // ambient light (owner, 2026-11-04: "reflection probes for all those spacey backgrounds ... but dont crank it up
        // super high"): a 128 radiance map, made once per scenario, not every frame
        _skyMat = new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/View/nebula_sky.gdshader") };
        var sky = new Sky { SkyMaterial = _skyMat, RadianceSize = Sky.RadianceSizeEnum.Size128, ProcessMode = Sky.ProcessModeEnum.Quality };
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky, Sky = sky,
            AmbientLightSource = Godot.Environment.AmbientSource.Sky, AmbientLightColor = new Color("#3a4a6a"), AmbientLightEnergy = AmbientEnergy,
            AmbientLightSkyContribution = AmbientSkyShare, ReflectedLightSource = Godot.Environment.ReflectionSource.Sky,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
        };
        AddChild(new WorldEnvironment { Environment = env, Name = "Environment" });
        // the sun casts high-quality shadows, the ships onto themselves too (owner, 2026-11-04: "need high quality shadows on
        // ships too", "looks like you didnt enable self shadowing"): four cascades out to 60 units (6 km) round the camera,
        // the nearest a few hundred metres for the ship in view; the bias set at the ships' scale (a unit is 100 m, so
        // Godot's default would lift every shadow 10 m off the plating and lose the hull's shadow on itself)
        var sun = new DirectionalLight3D
        {
            Name = "Sun", LightEnergy = (float)Scenario.DeepSpaceSun, LightColor = new Color("#fff4e6"),
            ShadowEnabled = true, DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits,
            DirectionalShadowMaxDistance = 60f, DirectionalShadowSplit1 = 0.04f, DirectionalShadowSplit2 = 0.12f, DirectionalShadowSplit3 = 0.35f,
            DirectionalShadowBlendSplits = true, ShadowBias = 0.004f, ShadowNormalBias = 0.6f, ShadowBlur = 0.8f,
        };
        sun.RotationDegrees = new Vector3(-48, -35, 0);
        AddChild(sun);
        _sun = sun;
        _cam = new Camera3D { Name = "Camera", Current = true };
        AddChild(_cam);
        Rebuild();
    }

    /// <summary>Makes a model for every ship of the battle (on start and on a restart).</summary>
    public void Rebuild()
    {
        foreach (Node3D n in _models.Values) n.QueueFree();
        _models.Clear();
        _strobes.Clear();
        _damage.Clear();
        _shields.Clear();
        _hitSeq = _x.Battle.Hits.Count > 0 ? _x.Battle.Hits[^1].Seq : 0;
        // the scenario's own nebula sky (each scene its own: data/battle/scenarios.json "sky")
        if (_sky == null || _sky.Name != "Sky_" + _x.Battle.Scenario.Sky)
        {
            _sky?.QueueFree();
            _sky = NebulaSky.Load(_x.Battle.Scenario.Sky);
            AddChild(_sky);
            _skyMat.SetShaderParameter("panorama", GD.Load<Texture2D>($"res://assets/skies/{_x.Battle.Scenario.Sky}.jpg"));
        }
        _sun.LightEnergy = (float)_x.Battle.Scenario.SunEnergy;   // the scene's own sun (scenarios.json "sun_energy")
        foreach (Ship s in _x.Battle.Ships)
        {
            Node3D model = s.Class.Model is { } path ? ShipModel(path, s.Class.LengthM, _strobes) : StandIn(s);
            model.Name = s.Id;
            AddChild(model);
            _models[s.Id] = model;
            var shell = new ShieldShell((float)(s.Class.LengthM / MPerU));   // every ship's (owner, 2026-11-04: "add shield display to all ships")
            model.AddChild(shell);
            _shields[s.Id] = shell;
            if (s.Class.Model is { } file && model.GetChild(0) is Node3D glb)
                _damage[s.Id] = new ShipDamage(glb, this, s, file["assets/ships/".Length..], glb.Scale.X, ShipSkin.HullShader);
        }
    }

    /// <summary>An approved ship from its glb (metres, bow toward -Z), scaled so its length is the class's battle length,
    /// dressed from the export manifest (its hull shader and lamps: <see cref="ShipSkin"/>); its strobes join strobes.</summary>
    private static Node3D ShipModel(string path, double lengthM, List<OmniLight3D> strobes)
    {
        var scene = GD.Load<PackedScene>("res://" + path) ?? throw new InvalidOperationException($"ship model not found: res://{path}");
        Node3D ship = scene.Instantiate<Node3D>();
        float len = Bounds(ship, Transform3D.Identity).Size.Z;
        if (!(len > 0)) throw new InvalidOperationException($"ship model has no length: res://{path}");
        var holder = new Node3D();
        holder.AddChild(ship);
        ship.Scale = Vector3.One * (float)(lengthM / MPerU) / len;
        strobes.AddRange(ShipSkin.Apply(ship, path["assets/ships/".Length..], ship.Scale.X));
        return holder;
    }

    /// <summary>The box round every mesh under a node, in the frame <paramref name="xf"/> maps the node into.</summary>
    private static Aabb Bounds(Node n, Transform3D xf)
    {
        Aabb? box = null;
        foreach (Node c in n.GetChildren())
        {
            if (c is not Node3D c3) continue;
            Transform3D cx = xf * c3.Transform;
            if (c is MeshInstance3D mi && mi.Mesh != null) { Aabb b = cx * mi.Mesh.GetAabb(); box = box is { } a ? a.Merge(b) : b; }
            Aabb sub = Bounds(c, cx);
            if (sub.Size != Vector3.Zero) box = box is { } a2 ? a2.Merge(sub) : sub;
        }
        return box ?? new Aabb();
    }

    /// <summary>The stand-in for a class's shape, in its faction's stand-in colours.</summary>
    private static Node3D StandIn(Ship s)
    {
        var model = GD.Load<PackedScene>($"res://scenes/standins/{s.Class.Shape}.tscn").Instantiate<Node3D>();
        model.Scale = Vector3.One * (float)(s.Class.LengthM / MPerU);
        Paint(model, new Color(s.Faction.Hull), new Color(s.Faction.Trim));
        return model;
    }

    private static void Paint(Node n, Color hull, Color trim)
    {
        foreach (Node c in n.GetChildren())
        {
            if (c is MeshInstance3D mi)
                mi.MaterialOverride = new StandardMaterial3D { AlbedoColor = mi.Name.ToString().StartsWith("Trim") ? trim : hull, Metallic = 0.35f, Roughness = 0.45f };
            Paint(c, hull, trim);
        }
    }

    /// <summary>The view rectangle and the whole screen, in console units.</summary>
    public void SetViewRect(Rect2 view, Vector2 screen) { _view = view; _screen = screen; }

    public override void _Process(double delta) => Sync(delta);

    /// <summary>Moves every model to its ship and the camera to the ship you command (or, in Target view, its target);
    /// dt (seconds of real time) turns a locked camera and moves a glide on.</summary>
    public void Sync(double dt = 0)
    {
        Battle b = _x.Battle;
        ShipSkin.Flash(_strobes, Time.GetTicksMsec() / 1000.0);
        Damage(b, _x.State.Paused || _x.State.MenuOpen ? 0 : dt);
        foreach (Ship s in b.Ships)
        {
            if (!_models.TryGetValue(s.Id, out Node3D? m)) continue;
            m.Visible = s.Alive || _damage.ContainsKey(s.Id);   // an approved ship stays to come apart; a stand-in just goes
            m.Position = new Vector3((float)(s.X / MPerU), 0, (float)(s.Z / MPerU));
            m.Rotation = new Vector3(0, -(float)s.H, 0);
            if (_shields.TryGetValue(s.Id, out ShieldShell? sh)) { sh.Visible = s.Alive; if (s.Alive) sh.Show(s, _x.Kit.C); }
        }
        Ship p = b.Player;
        FollowTarget(p);
        LockStep(p, dt);
        Ship? tv = TargetView ? b.TargetOf(p) ?? Wreck(b) : null;
        Ship f = tv ?? p;
        var want = new Vector3((float)(f.X / MPerU), 0, (float)(f.Z / MPerU));
        if (!_focusSet) { _focus = want; _focusSet = true; }
        _glideT = Math.Min(GlideS, _glideT + (float)dt);
        float k = _glideT / GlideS, e = k * k * (3 - 2 * k);   // eased in and out
        _focus = _glideT < GlideS ? _glideFrom.Lerp(want, e) : want;
        Vector3 focus = _focus;
        var off = new Vector3(Mathf.Sin(Yaw) * Mathf.Cos(Pitch), Mathf.Sin(Pitch), Mathf.Cos(Yaw) * Mathf.Cos(Pitch)) * Dist;
        _cam.Position = focus + off;
        _cam.LookAt(focus, Vector3.Up);
        _sky?.Follow(_cam.GlobalPosition);
        Project();
    }

    // Battle damage (ShipDamage): each hit that has landed since the last frame makes its effect and scar, then every
    // ship's damage steps on by dt, real seconds (0 while paused), so a wreck still comes apart once the battle is over.
    private void Damage(Battle b, double dt)
    {
        foreach (HitEvent e in b.Hits)
        {
            if (e.Seq <= _hitSeq) continue;
            _hitSeq = e.Seq;
            if (_damage.TryGetValue(e.ShipId, out ShipDamage? d)) d.OnHit(e);
            else if (_models.TryGetValue(e.ShipId, out Node3D? m))
                Fx.Fireball(this, m.GlobalPosition, e.HullDamage > 0 ? 0.12f : 0.06f, Vector3.Up);
        }
        foreach (ShipDamage d in _damage.Values) d.Step((float)dt);
    }

    // A perspective frustum whose centre lands on the ship's point in the view rectangle (ship_screen_y down the view).
    private void Project()
    {
        if (_screen.X <= 0 || _view.Size.Y <= 0) return;
        LayoutDef l = _x.Data.Layout;
        float near = 0.5f, fov = (float)(l.ViewFovDeg * Math.PI / 180);
        float hs = 2 * near * Mathf.Tan(fov / 2) * (_screen.Y / _view.Size.Y), ws = hs * _screen.X / _screen.Y;
        float cx = _view.Position.X + _view.Size.X / 2, cy = _view.Position.Y + _view.Size.Y * (float)l.ShipScreenY;
        _cam.SetFrustum(hs, new Vector2(ws * (0.5f - cx / _screen.X), hs * (cy / _screen.Y - 0.5f)), near, 4000);
    }

    /// <summary>Where a point on the plane (metres) appears on screen, in console units; null when behind the camera.</summary>
    public Vector2? ScreenOf(double xm, double zm, float yU = 0)
    {
        var p = new Vector3((float)(xm / MPerU), yU, (float)(zm / MPerU));
        if (_cam.IsPositionBehind(p)) return null;
        return _cam.UnprojectPosition(p);
    }

    /// <summary>The point on the battle plane under a screen position, metres; null when the ray misses the plane.</summary>
    public (double X, double Z)? PlaneAt(Vector2 screen)
    {
        // the ray through the camera's own projection, offset frustum and all: Godot's ProjectRayNormal assumes a centred
        // frustum, so with the ship held low in the view it cast every click somewhere else (owner, 2026-11-04: "mouse to
        // screen ray cast is wrong, its not working like the JS version", "cant properly turn my ship in the desired
        // direction"). Unprojecting two depths of the same pixel gives a ray that ScreenOf's UnprojectPosition inverts exactly.
        var (o, d) = Ray(screen);
        if (Mathf.Abs(d.Y) < 1e-5f) return null;
        float t = -o.Y / d.Y;
        if (t < 0) return null;
        Vector3 q = o + d * t;
        return (q.X * MPerU, q.Z * MPerU);
    }

    // The ray (origin, unit direction) through a screen point, in the screen space UnprojectPosition gives (the viewport's
    // visible rectangle), by the inverse of the camera's projection.
    private (Vector3 O, Vector3 D) Ray(Vector2 screen)
    {
        Vector2 vs = GetViewport().GetVisibleRect().Size;
        float nx = screen.X / vs.X * 2 - 1, ny = 1 - screen.Y / vs.Y * 2;
        Projection inv = _cam.GetCameraProjection().Inverse();
        Vector3 At(float z) { Vector4 v = inv * new Vector4(nx, ny, z, 1); return new Vector3(v.X, v.Y, v.Z) / v.W; }
        Transform3D xf = _cam.GlobalTransform;
        Vector3 a = xf * At(0.0f), b = xf * At(0.5f);
        return (a, (b - a).Normalized());
    }

    /// <summary>The worst miss, metres, of a click cast back through PlaneAt from where ScreenOf drew points of the
    /// battle plane round your ship (the layout test's check that a click lands where the player pointed).</summary>
    public double RayMissM()
    {
        Ship p = _x.Battle.Player;
        double worst = 0;
        for (int i = 0; i < 16; i++)
        {
            double a = i * Math.PI / 8, r = 400 + 600 * (i % 4);
            double x = p.X + Math.Sin(a) * r, z = p.Z - Math.Cos(a) * r;
            if (ScreenOf(x, z) is not { } sp || PlaneAt(sp) is not { } q) continue;
            worst = Math.Max(worst, Math.Sqrt((q.X - x) * (q.X - x) + (q.Z - z) * (q.Z - z)));
        }
        return worst;
    }

    /// <summary>The player's drag: turns and tilts the camera, and frees the camera lock (zoom does not).</summary>
    public void Orbit(Vector2 drag)
    {
        CamLock = false;
        Yaw -= drag.X * 0.006f;
        Pitch = Mathf.Clamp(Pitch + drag.Y * 0.004f, 0.08f, 1.45f);
    }

    public void Zoom(float factor) => Dist = Mathf.Clamp(Dist * factor, 5, 220);

    /// <summary>The Camera lock key: back to your own ship (out of Target view) with the camera locked on the target.</summary>
    public void LockCamera()
    {
        if (_x.Battle.TargetOf(_x.Battle.Player) == null) return;
        if (TargetView) { TargetView = false; StartGlide(); }
        CamLock = true;
    }

    /// <summary>The Target view key: the camera glides to the target and orbits it; again, it glides back to your ship.</summary>
    public void ToggleTargetView()
    {
        TargetView = !TargetView && _x.Battle.TargetOf(_x.Battle.Player) != null;
        if (TargetView) _watched = _x.Battle.TargetOf(_x.Battle.Player)!.Id;
        if (TargetView) CamLock = false;
        StartGlide();
    }

    private void StartGlide() { _glideFrom = _focus; _glideT = 0; }

    // Follows the player's target: camera lock is the default when targeting a ship (owner, 2026-11-04: "camera lock is
    // the default when targeting other ships"), so a new lock locks the camera (in Target view it glides there instead);
    // no target, no lock.
    private void FollowTarget(Ship p)
    {
        string? id = _x.Battle.TargetOf(p)?.Id;   // a target destroyed counts as none
        if (id == _lastTarget) return;
        _lastTarget = id;
        if (id == null)
        {
            CamLock = false;
            // a target destroyed: Target view stays on its wreck, to watch it come apart, until the next lock
            if (TargetView && Wreck(_x.Battle) != null) return;
            if (TargetView) StartGlide();
            TargetView = false;
            return;
        }
        _watched = id;
        if (TargetView) StartGlide();
        else CamLock = true;
    }

    // The ship Target view was watching when it was destroyed, while its wreck is still drawn.
    private Ship? Wreck(Battle b) => _watched != null && _damage.ContainsKey(_watched) ? b.Ships.FirstOrDefault(s => s.Id == _watched && !s.Alive) : null;

    // The locked camera's turn: the yaw a critically damped spring toward the bearing that puts the target ahead.
    private void LockStep(Ship p, double dt)
    {
        Ship? t = _x.Battle.TargetOf(p);
        if (!CamLock || TargetView || t == null || dt <= 0) { _yawVel = 0; return; }
        float want = Mathf.Atan2(-(float)(t.X - p.X), -(float)(t.Z - p.Z));
        float d = Mathf.Wrap(want - Yaw, -Mathf.Pi, Mathf.Pi), w = LockOmegaS, vmax = Mathf.DegToRad(LockMaxDegS);
        _yawVel = Mathf.Clamp(_yawVel + (w * w * d - 2 * w * _yawVel) * (float)dt, -vmax, vmax);
        Yaw += _yawVel * (float)dt;
    }
}
