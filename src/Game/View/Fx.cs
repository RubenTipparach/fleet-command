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
            Amount = Math.Max(1, count), Lifetime = lifeS, OneShot = burnS <= 0, Explosiveness = burnS <= 0 ? 1f : 0f, Randomness = 0.5f,
            Mesh = new QuadMesh { Size = Vector2.One * sizeU, Material = Mat(additive) }, LocalCoords = false,
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

    /// <summary>A big blast (a piece blown off, the main blast): fireballs, sparks, smoke, debris specks, a shockwave and light.</summary>
    public static void Blast(Node parent, Vector3 at, float r, float lightRangeU, float lightS)
    {
        Burst(parent, at, 16, 1.4f, r, r * 0.5f, FireRamp, true, 0.3f, 1.4f, 1.5f);
        Burst(parent, at, 70, 1.6f, r * 0.08f, r * 7f, SparkRamp, true, 1f, 0.3f, 0.6f);
        Burst(parent, at, 14, 5f, r * 1.2f, r * 0.8f, SmokeRamp, false, 0.5f, 2f, 0.4f);
        Shockwave(parent, at, r * 3f, 1.0f);
        Flash(parent, at, lightRangeU, 4f, lightS);
    }

    /// <summary>A flame streaming off a broken piece for burnS seconds (it follows the piece: parent it to the piece).</summary>
    public static CpuParticles3D Flame(Node3D piece, Vector3 localAt, float r, float burnS)
    {
        var p = Burst(piece, piece.ToGlobal(localAt), (int)Math.Max(6, 30 * burnS / 20f), 1.1f, r, r * 0.6f, FlameRamp, true, 0.5f, 1.2f, 1.5f, null, 180f, burnS);
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

    /// <summary>A thin ring of light that races out to radius r (scene units) and fades.</summary>
    public static void Shockwave(Node parent, Vector3 at, float r, float s)
    {
        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            AlbedoColor = new Color(1f, 0.7f, 0.4f, 0.3f), CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
        var ring = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 0.975f, OuterRadius = 1f, Rings = 64, RingSegments = 6 }, MaterialOverride = mat };
        parent.AddChild(ring);
        ring.GlobalPosition = at;
        ring.Scale = Vector3.One * 0.01f;
        Tween tw = ring.CreateTween().SetParallel();
        tw.TweenProperty(ring, "scale", Vector3.One * r, s).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tw.TweenProperty(mat, "albedo_color:a", 0f, s);
        tw.Chain().TweenCallback(Callable.From(ring.QueueFree));
    }
}
