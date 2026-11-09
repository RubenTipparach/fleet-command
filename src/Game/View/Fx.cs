// Fx: the battle's one-shot effects, after the ship mockups' (docs/mockups/lib/ship-fx.js: emit(), flash(), shockwave(),
// its table of particle looks): a fireball, sparks, smoke, a flame that keeps burning on a broken piece, a flash of light,
// and a shockwave ring. Each makes its own nodes, plays once and frees itself. Sizes are in scene units (the view's 100 m),
// worked out by the caller from metres. What colours mean is the Ship Design Bible's: orange fire, white-yellow sparks
// (power), blue for a shield. View only: the simulation never reads it.
using System;
using Godot;

namespace FleetCommand.Game.View;

public static class Fx
{
    private static Texture2D? _soft;
    private static readonly System.Collections.Generic.Dictionary<(bool, bool), StandardMaterial3D> _mats = new();

    // A soft round spot, the mockups' particle sprite.
    private static Texture2D Soft => _soft ??= new GradientTexture2D
    {
        Width = 64, Height = 64, Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f),
        Gradient = new Gradient { Offsets = new[] { 0f, 0.4f, 1f }, Colors = new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0.45f), new Color(1, 1, 1, 0) } },
    };

    // A billboard for particles: glowing ones add, smoke blends.
    private static StandardMaterial3D Mat(bool additive, bool fixedSize = false)
    {
        if (_mats.TryGetValue((additive, fixedSize), out StandardMaterial3D? m)) return m;
        m = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha, BlendMode = additive ? BaseMaterial3D.BlendModeEnum.Add : BaseMaterial3D.BlendModeEnum.Mix,
            VertexColorUseAsAlbedo = true, AlbedoTexture = Soft, DisableReceiveShadows = true, NoDepthTest = false, DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
            FixedSize = fixedSize,
        };
        _mats[(additive, fixedSize)] = m;
        return m;
    }

    private static Gradient Ramp(params (float At, Color C)[] stops)
    {
        var g = new Gradient();
        g.Offsets = Array.ConvertAll(stops, s => s.At);
        g.Colors = Array.ConvertAll(stops, s => s.C);
        return g;
    }

    private static Curve Grow(float from, float to)
    {
        var c = new Curve();
        c.AddPoint(new Vector2(0, from));
        c.AddPoint(new Vector2(1, to));
        return c;
    }

    // One burst of particles, gone when the last one dies; a continuous emitter when burnS is above 0.
    private static CpuParticles3D Burst(Node parent, Vector3 at, int count, float lifeS, float sizeU, float speedU, Gradient ramp, bool additive,
        float growFrom = 0.3f, float growTo = 1f, float damping = 0f, Vector3? dir = null, float spread = 180f, float burnS = 0f)
    {
        var p = new CpuParticles3D
        {
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Amount = Math.Max(1, count), Lifetime = lifeS, OneShot = burnS <= 0, Explosiveness = burnS <= 0 ? 1f : 0f, Randomness = 0.5f,
            Mesh = new QuadMesh { Size = Vector2.One * sizeU, Material = ramp == FireRamp || ramp == FlameRamp ? FlameMat : Mat(additive) }, LocalCoords = false,
            Direction = dir ?? Vector3.Up, Spread = spread, Gravity = Vector3.Zero,
            InitialVelocityMin = speedU * 0.3f, InitialVelocityMax = speedU, DampingMin = damping, DampingMax = damping,
            ScaleAmountMin = 0.6f, ScaleAmountMax = 1.4f, ScaleAmountCurve = Grow(growFrom, growTo), ColorRamp = ramp,
            EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere, EmissionSphereRadius = sizeU * 0.25f,
            AngleMin = 0, AngleMax = 360,
        };
        parent.AddChild(p);
        p.GlobalPosition = at;
        p.Emitting = true;
        if (burnS <= 0) FreeAfter(p, lifeS * 1.6f + 0.2f);
        else FreeAfter(p, burnS + lifeS * 1.6f + 0.2f, () => p.Emitting = false, burnS);
        return p;
    }

    private static ShaderMaterial? _flameMat;
    // Fire's particles are drawn as torn, flickering tongues of flame (flame.gdshader), not soft dots.
    private static ShaderMaterial FlameMat => _flameMat ??= new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/View/flame.gdshader") };

    /// <summary>Small debris (owner, 2026-11-04: "add in some smaller debris like sparks"): n white-hot fragments drawn as
    /// streaks along their flight, lenU long, flying out at up to speedU (scene units a second) and cooling to red over
    /// lifeS, and half as many embers that drift and flicker out.</summary>
    public static void Streaks(Node parent, Vector3 at, int n, float lenU, float speedU, float lifeS, Vector3? dir = null, float spread = 180f)
    {
        // each spark laid along its own flight (streak.gdshader), so they radiate from the blast
        var mat = _streakMat ??= new ShaderMaterial { Shader = GD.Load<Shader>("res://src/Game/View/streak.gdshader") };
        var p = new CpuParticles3D
        {
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Amount = n, Lifetime = lifeS, OneShot = true, Explosiveness = 0.92f, Randomness = 0.6f, LocalCoords = false,
            Mesh = new QuadMesh { Size = new Vector2(lenU * 0.12f, lenU), Material = mat }, ParticleFlagAlignY = true,
            Direction = dir ?? Vector3.Up, Spread = spread, Gravity = Vector3.Zero, InitialVelocityMin = speedU * 0.25f, InitialVelocityMax = speedU,
            DampingMin = speedU * 0.08f, DampingMax = speedU * 0.2f, ScaleAmountMin = 0.4f, ScaleAmountMax = 1.2f, ScaleAmountCurve = Grow(1f, 0.15f),
            ColorRamp = SparkRamp,
        };
        parent.AddChild(p);
        p.GlobalPosition = at;
        p.Emitting = true;
        FreeAfter(p, lifeS * 1.6f + 0.2f);
        Burst(parent, at, Math.Max(4, n / 2), lifeS * 2.2f, lenU * 0.18f, speedU * 0.3f, EmberRamp, true, 1f, 0.4f, 0.3f, dir, spread);
    }

    private static ShaderMaterial? _streakMat;
    private static readonly Gradient EmberRamp = Ramp((0f, new Color(1f, 0.85f, 0.5f, 1f)), (0.3f, new Color(1f, 0.45f, 0.1f, 0.9f)),
        (0.7f, new Color(0.7f, 0.15f, 0.03f, 0.6f)), (1f, new Color(0.3f, 0.05f, 0.0f, 0f)));

    private static void FreeAfter(Node n, float s, Action? first = null, float firstAt = 0)
    {
        SceneTree tree = (SceneTree)Engine.GetMainLoop();
        if (first != null) tree.CreateTimer(firstAt, false).Timeout += () => { if (GodotObject.IsInstanceValid(n)) first(); };
        tree.CreateTimer(s, false).Timeout += () => { if (GodotObject.IsInstanceValid(n)) n.QueueFree(); };
    }

    private static readonly Gradient FireRamp = Ramp((0f, new Color(1f, 0.78f, 0.45f, 0.6f)), (0.15f, new Color(1f, 0.45f, 0.1f, 0.5f)),
        (0.55f, new Color(0.6f, 0.15f, 0.03f, 0.3f)), (1f, new Color(0.15f, 0.05f, 0.02f, 0f)));
    private static readonly Gradient SparkRamp = Ramp((0f, new Color(1f, 1f, 0.85f, 1f)), (0.5f, new Color(1f, 0.75f, 0.3f, 0.9f)), (1f, new Color(1f, 0.4f, 0.1f, 0f)));
    private static readonly Gradient SmokeRamp = Ramp((0f, new Color(0.2f, 0.18f, 0.17f, 0.0f)), (0.15f, new Color(0.16f, 0.15f, 0.14f, 0.55f)), (1f, new Color(0.1f, 0.1f, 0.1f, 0f)));
    private static readonly Gradient ShieldRamp = Ramp((0f, new Color(0.75f, 0.9f, 1f, 1f)), (0.4f, new Color(0.3f, 0.6f, 1f, 0.7f)), (1f, new Color(0.1f, 0.3f, 1f, 0f)));
    private static readonly Gradient FlameRamp = Ramp((0f, new Color(1f, 0.85f, 0.5f, 0.9f)), (0.35f, new Color(1f, 0.42f, 0.08f, 0.7f)), (1f, new Color(0.3f, 0.06f, 0.02f, 0f)));

    /// <summary>A fireball of radius r (scene units), with sparks and a little smoke; a hit on the hull.</summary>
    public static void Fireball(Node parent, Vector3 at, float r, Vector3 outward)
    {
        Shell(parent, at + outward * r * 0.4f, r * 1.1f, 0.8f, 1.1f, 0.3f);
        Burst(parent, at, 7, 0.7f, r * 1.2f, r * 0.5f, FireRamp, true, 0.4f, 1.3f, 2f, outward, 70f);
        Burst(parent, at, 22, 0.6f, r * 0.12f, r * 7f, SparkRamp, true, 1f, 0.2f, 1.5f, outward, 60f);
        Burst(parent, at, 6, 2.4f, r * 1.4f, r * 0.7f, SmokeRamp, false, 0.4f, 1.6f, 0.6f, outward, 80f);
    }

    /// <summary>A shot stopped by the shield: a blue flare where it struck.</summary>
    public static void ShieldFlare(Node parent, Vector3 at, float r, Vector3 outward)
    {
        Burst(parent, at, 12, 0.45f, r * 1.4f, r * 0.8f, ShieldRamp, true, 0.5f, 1.2f, 3f, outward, 90f);
        Burst(parent, at, 10, 0.4f, r * 0.15f, r * 5f, ShieldRamp, true, 1f, 0.2f, 2f, outward, 70f);
    }

    /// <summary>A big blast (a piece blown off): a ball of fire, sparks, smoke, a shock ring and light.</summary>
    public static void Blast(Node parent, Vector3 at, float r, float lightRangeU, float lightS)
    {
        Shell(parent, at, r * 1.6f, 1.6f, 1.2f, 0.35f);
        Burst(parent, at, 14, 1.6f, r, r * 0.5f, FireRamp, true, 0.3f, 1.4f, 1.5f);
        Burst(parent, at, 90, 1.8f, r * 0.07f, r * 8f, SparkRamp, true, 1f, 0.3f, 0.5f);
        Streaks(parent, at, 60, r * 0.5f, r * 6f, 1.6f);
        Burst(parent, at, 14, 5f, r * 1.2f, r * 0.8f, SmokeRamp, false, 0.5f, 2f, 0.4f);
        Shockwave(parent, at, r * 4f, 1.2f);
        Flash(parent, at, lightRangeU, 5f, lightS);
    }

    /// <summary>A ship's end sized to its length in scene units: twice the first size (owner, 2026-11-04: "2x nova side"),
    /// growing slower than the ship so a dreadnought's does not white out the whole view. Every ship's end, an approved
    /// ship's core breach and a stand-in's, is this one.</summary>
    public static void ShipEnd(Node parent, Vector3 at, float lengthU) => Nova(parent, at, 1.8f * Mathf.Pow(lengthU, 0.7f));

    /// <summary>A ship's end, its core breaching (owner, 2026-11-04: "aww theres no big super nova", "center is white
    /// with pink edges, and a blue ring glowing outward", "I really want it to have this exact look and colors, and the
    /// debris should have trails of flame"): a star of white light with pink edges (novastar.gdshader), a flat disc of
    /// blue light racing out through the battle plane to 3.8 times r (novadisc.gdshader), debris flung out trailing
    /// flame, white sparks, and a blue-white flash over the ships round it. r is the star's size, scene units.</summary>
    public static void Nova(Node parent, Vector3 at, float r)
    {
        // the owner's timing (2026-11-04: "nova should linger 2x as long, the whole explosion 2x slower"): every part runs
        // over twice its first length, the star's glow holding on longest
        Star(parent, at, r * 1.5f, 4.8f);
        Disc(parent, at, r * 3.8f, 5.6f);
        Debris(parent, at, 18, r * 0.04f, r * 2.2f, 6.4f);
        Burst(parent, at, 200, 3.2f, r * 0.03f, r * 2.5f, StarSparkRamp, true, 1f, 0.3f, 0.2f);
        Streaks(parent, at, 160, r * 0.14f, r * 1.3f, 3.4f);
        var l = new OmniLight3D { OmniRange = r * 8f, LightEnergy = 16f, LightColor = new Color(0.75f, 0.8f, 1f), ShadowEnabled = false, OmniAttenuation = 1.2f };
        parent.AddChild(l);
        l.GlobalPosition = at;
        Tween tw = l.CreateTween();
        tw.TweenProperty(l, "light_energy", 0f, 5.2f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        tw.TweenCallback(Callable.From(l.QueueFree));
    }

    private static readonly Gradient StarSparkRamp = Ramp((0f, new Color(1f, 1f, 1f, 1f)), (0.4f, new Color(0.9f, 0.75f, 1f, 0.9f)), (1f, new Color(0.4f, 0.55f, 1f, 0f)));
    private static Shader? _star, _disc;

    // The breach's star: a camera-facing quad that flares to size r and burns down over s seconds.
    private static void Star(Node parent, Vector3 at, float r, float s)
    {
        _star ??= GD.Load<Shader>("res://src/Game/View/novastar.gdshader");
        var mat = new ShaderMaterial { Shader = _star };
        mat.SetShaderParameter("progress", 0f);   // a tween can only drive a parameter that has been set
        mat.SetShaderParameter("spin", (float)GD.RandRange(0.0, 3.14));
        var q = new MeshInstance3D { Mesh = new QuadMesh { Size = Vector2.One * 2 }, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        parent.AddChild(q);
        q.GlobalPosition = at;
        q.Scale = Vector3.One * r;
        Tween tw = q.CreateTween();
        tw.TweenProperty(mat, "shader_parameter/progress", 1f, s);
        tw.TweenCallback(Callable.From(q.QueueFree));
    }

    // The breach's disc: a plane in the battle plane, radius r, its front racing out over s seconds.
    private static void Disc(Node parent, Vector3 at, float r, float s)
    {
        _disc ??= GD.Load<Shader>("res://src/Game/View/novadisc.gdshader");
        var mat = new ShaderMaterial { Shader = _disc };
        mat.SetShaderParameter("progress", 0f);   // a tween can only drive a parameter that has been set
        mat.SetShaderParameter("seed", (float)GD.RandRange(0.0, 50.0));
        var plane = new MeshInstance3D { Mesh = new PlaneMesh { Size = Vector2.One * 2 }, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        parent.AddChild(plane);
        plane.GlobalPosition = at;
        plane.Scale = Vector3.One * r;
        Tween tw = plane.CreateTween();
        tw.TweenProperty(mat, "shader_parameter/progress", 1f, s);
        tw.TweenCallback(Callable.From(plane.QueueFree));
    }

    /// <summary>Burning debris flung out of a blast: n streaks of flame (no solid chunk: owner, 2026-11-04, "remove blocky
    /// debris"), each puff sizeU across, flying out to about reachU (scene units, mostly along the battle plane) over s
    /// seconds.</summary>
    public static void Debris(Node parent, Vector3 at, int n, float sizeU, float reachU, float s)
    {
        for (int i = 0; i < n; i++)
        {
            float ang = (float)GD.RandRange(0, Math.Tau), up = (float)GD.RandRange(-0.35, 0.35);
            Vector3 dir = new Vector3(Mathf.Cos(ang), up, Mathf.Sin(ang)).Normalized();
            float reach = reachU * (float)GD.RandRange(0.45, 1.0), sz = sizeU * (float)GD.RandRange(0.5, 1.3);
            var chunk = new Node3D();
            parent.AddChild(chunk);
            chunk.GlobalPosition = at;
            chunk.RotationDegrees = new Vector3((float)GD.RandRange(0, 360), (float)GD.RandRange(0, 360), 0);
            var trail = new CpuParticles3D
            {
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Amount = 90, Lifetime = 1.6f, LocalCoords = false, Mesh = new QuadMesh { Size = Vector2.One * sz * 1.3f, Material = Mat(true) },
                Gravity = Vector3.Zero, Spread = 180, InitialVelocityMin = 0, InitialVelocityMax = sz * 0.6f, ScaleAmountCurve = Grow(1f, 0.2f),
                ColorRamp = FlameRamp, AngleMin = 0, AngleMax = 360,
            };
            chunk.AddChild(trail);
            trail.Emitting = true;
            Tween tw = chunk.CreateTween().SetParallel();
            tw.TweenProperty(chunk, "global_position", at + dir * reach, s).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            tw.TweenProperty(chunk, "rotation_degrees", chunk.RotationDegrees + new Vector3(360, 200, 90) * (float)GD.RandRange(0.5, 1.5), s);
            tw.Chain().TweenCallback(Callable.From(() => trail.Emitting = false));
            tw.Chain().TweenInterval(1.8f);
            tw.Chain().TweenCallback(Callable.From(chunk.QueueFree));
        }
    }

    private static Shader? _fireball, _ring;

    // A ball of fire (fireball.gdshader): it swells to radius r over the first part of its life and burns away by lifeS.
    private static void Shell(Node parent, Vector3 at, float r, float lifeS, float intensity, float billow)
    {
        _fireball ??= GD.Load<Shader>("res://src/Game/View/fireball.gdshader");
        var mat = new ShaderMaterial { Shader = _fireball };
        mat.SetShaderParameter("progress", 0f);   // a tween can only drive a parameter that has been set
        mat.SetShaderParameter("seed", (float)GD.RandRange(0.0, 50.0));
        mat.SetShaderParameter("intensity", intensity);
        mat.SetShaderParameter("billow", billow);
        var ball = new MeshInstance3D { Mesh = new SphereMesh { Radius = 1, Height = 2, RadialSegments = 48, Rings = 24 }, MaterialOverride = mat,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        parent.AddChild(ball);
        ball.GlobalPosition = at;
        ball.Scale = Vector3.One * r * 0.15f;
        ball.RotationDegrees = new Vector3((float)GD.RandRange(0, 360), (float)GD.RandRange(0, 360), 0);
        Tween tw = ball.CreateTween().SetParallel();
        tw.TweenProperty(ball, "scale", Vector3.One * r, lifeS * 0.55f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        tw.TweenProperty(mat, "shader_parameter/progress", 1f, lifeS).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
        tw.TweenProperty(ball, "rotation_degrees", ball.RotationDegrees + new Vector3(0, 40, 15), lifeS);
        tw.Chain().TweenCallback(Callable.From(ball.QueueFree));
    }

    // The core's flash: a white disc facing the camera that blooms to r and fades.
    private static void Core(Node parent, Vector3 at, float r, float inS, float outS)
    {
        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha, BlendMode = BaseMaterial3D.BlendModeEnum.Add, AlbedoTexture = Soft,
            AlbedoColor = new Color(2.2f, 2.0f, 1.7f, 1f), DisableReceiveShadows = true, DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
        };
        var q = new MeshInstance3D { Mesh = new QuadMesh { Size = Vector2.One * 2 }, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        parent.AddChild(q);
        q.GlobalPosition = at;
        q.Scale = Vector3.One * 0.05f;
        Tween tw = q.CreateTween();
        tw.TweenProperty(q, "scale", Vector3.One * r, inS).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        tw.TweenProperty(mat, "albedo_color", new Color(1.2f, 0.5f, 0.15f, 0f), outS).SetEase(Tween.EaseType.Out);
        tw.TweenCallback(Callable.From(q.QueueFree));
    }

    // A shock ring (shockring.gdshader) on a plane tilted by tiltRad, its front racing out to radius r over s seconds.
    private static void Ring(Node parent, Vector3 at, float r, float s, Vector3 tiltRad, float intensity)
    {
        _ring ??= GD.Load<Shader>("res://src/Game/View/shockring.gdshader");
        var mat = new ShaderMaterial { Shader = _ring };
        mat.SetShaderParameter("progress", 0f);   // a tween can only drive a parameter that has been set
        mat.SetShaderParameter("seed", (float)GD.RandRange(0.0, 50.0));
        mat.SetShaderParameter("intensity", intensity);
        var plane = new MeshInstance3D { Mesh = new PlaneMesh { Size = Vector2.One * 2 }, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        parent.AddChild(plane);
        plane.GlobalPosition = at;
        plane.Rotation = tiltRad;
        plane.Scale = Vector3.One * r;
        Tween tw = plane.CreateTween();
        tw.TweenProperty(mat, "shader_parameter/progress", 1f, s).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tw.TweenCallback(Callable.From(plane.QueueFree));
    }

    /// <summary>A flame streaming off a broken piece for burnS seconds (it follows the piece: parent it to the piece).</summary>
    public static CpuParticles3D Flame(Node3D piece, Vector3 localAt, float r, float burnS)
    {
        // a wake: puffs left behind in space as the piece drifts on, swelling as they cool
        var p = Burst(piece, piece.ToGlobal(localAt), 140, 2.8f, r, r * 0.12f, FlameRamp, true, 0.7f, 1.6f, 0.6f, null, 180f, burnS);
        p.Position = localAt;
        return p;
    }

    /// <summary>A flash of light: an orange OmniLight3D of this range (scene units) fading over s seconds.</summary>
    public static void Flash(Node parent, Vector3 at, float rangeU, float energy, float s)
    {
        var l = new OmniLight3D { OmniRange = rangeU, LightEnergy = energy, LightColor = new Color(1f, 0.62f, 0.3f), ShadowEnabled = false, OmniAttenuation = 1.5f };
        parent.AddChild(l);
        l.GlobalPosition = at;
        Tween tw = l.CreateTween();
        tw.TweenProperty(l, "light_energy", 0f, s).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        tw.TweenCallback(Callable.From(l.QueueFree));
    }

    /// <summary>A shock ring that races out to radius r (scene units) over s seconds, level with the battle plane.</summary>
    public static void Shockwave(Node parent, Vector3 at, float r, float s) => Ring(parent, at, r, s, Vector3.Zero, 1f);
}
