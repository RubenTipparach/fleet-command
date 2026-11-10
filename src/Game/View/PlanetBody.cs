// PlanetBody: one planet drawn in layers (owner, 2026-11-09: "We need some really good in game planet shaders ... Make a
// few different planet types ... gas giants with rings"; the planet-shaders skill). The one builder of a planet's look, so
// the battle (BattleView.Planets) and the planet gallery (PlanetGallery) draw the same world. godot-sandbox's stack of
// shells round a unit sphere, each a mesh of its own just above the last:
//   Ground   planet.gdshader (rocky and lava worlds, after Falchion) or planet_gas.gdshader (gas giants), radius 1
//   Craters  planet_craters.gdshader, 1.004, where the kind has a crater cover
//   Clouds   planet_clouds.gdshader, a little under the air's top, where the kind has a cloud layer (the ground's own
//            painted clouds are then off, so there is one weather; the deck wears the air's rim, as the ground does)
//   Rings    planet_rings.gdshader, a flat square in the equator plane cut to the annulus, where the kind has rings
//   Air      planet_atmosphere.gdshader (godot-sandbox's single scattering shell), 1 + the kind's atmosphere height
// Every layer but the ground is under Body, which leans by the kind's axial tilt (so the rings, the ring shadow and the
// installations all share the planet's own frame). Root is the planet's place and scale (its radius in scene units),
// which the caller sets. Each frame Sync gives every layer the battle's sun and the ground its detail by the camera's
// height (Falchion's octaves: more as the camera comes down; and how close it is, which adds grain to the ground and
// fades the rim of air on the ground and the clouds). The look's numbers are data (PlanetLooks), never here.
using System.Collections.Generic;
using Godot;

namespace FleetCommand.Game.View;

public sealed class PlanetBody
{
    private static readonly string Dir = "res://src/Game/View/";
    private static Shader? _rocky, _gas, _air, _craters, _clouds, _rings;
    private static SphereMesh? _groundMesh, _craterMesh;

    /// <summary>The planet's place and scale (the caller's), and the leaning frame its layers stand in.</summary>
    public readonly Node3D Root, Body;
    public readonly ShaderMaterial Ground;
    public readonly ShaderMaterial? Air;
    public readonly PlanetLook Look;
    private readonly List<ShaderMaterial> _lit = new();
    private readonly ShaderMaterial? _cloudMat;

    private PlanetBody(string name, PlanetLook look, Vector3 seed)
    {
        Look = look;
        _rocky ??= GD.Load<Shader>(Dir + "planet.gdshader");
        _gas ??= GD.Load<Shader>(Dir + "planet_gas.gdshader");
        _air ??= GD.Load<Shader>(Dir + "planet_atmosphere.gdshader");
        _craters ??= GD.Load<Shader>(Dir + "planet_craters.gdshader");
        _clouds ??= GD.Load<Shader>(Dir + "planet_clouds.gdshader");
        _rings ??= GD.Load<Shader>(Dir + "planet_rings.gdshader");
        _groundMesh ??= new SphereMesh { Radius = 1, Height = 2, RadialSegments = 160, Rings = 80 };
        _craterMesh ??= new SphereMesh { Radius = 1.004f, Height = 2.008f, RadialSegments = 128, Rings = 64 };

        Root = new Node3D { Name = name };
        Body = new Node3D { Name = "Body", RotationDegrees = new Vector3(0, 0, look.TiltDeg) };
        Root.AddChild(Body);
        bool gas = look.Surface == 1;
        Ground = new ShaderMaterial { Shader = gas ? _gas : _rocky };
        Air = look.AtmosphereStrength > 0 ? new ShaderMaterial { Shader = _air, RenderPriority = 4 } : null;
        PlanetLooks.Apply(look, seed, Ground, Air);
        Body.AddChild(Mesh("Ground", _groundMesh, Ground));

        if (look.CraterCover > 0)
        {
            var m = Lit(_craters, 1);
            m.SetShaderParameter("coverage", look.CraterCover);
            m.SetShaderParameter("seed", seed.X);
            m.SetShaderParameter("air", look.AtmosphereStrength > 0 ? 1f : 0f);
            Body.AddChild(Mesh("Craters", _craterMesh, m));
        }
        if (look.CloudLayer is { } cl)
        {
            var m = _cloudMat = Lit(_clouds, 2);
            m.SetShaderParameter("coverage", cl.Cover);
            m.SetShaderParameter("pattern_scale", cl.Scale);
            m.SetShaderParameter("color_lit", cl.Lit);
            m.SetShaderParameter("color_base", cl.Base);
            m.SetShaderParameter("cloud_opacity", cl.Opacity);
            m.SetShaderParameter("fringe_opacity", cl.Opacity * 0.55f);
            m.SetShaderParameter("atmo", look.Atmosphere);
            m.SetShaderParameter("atmo_strength", look.AtmosphereStrength);
            m.SetShaderParameter("seed", seed);
            float top = 1 + look.AtmosphereHeight * 0.35f;
            Body.AddChild(Mesh("Clouds", new SphereMesh { Radius = top, Height = 2 * top, RadialSegments = 128, Rings = 64 }, m));
        }
        if (look.Rings is { } rg)
        {
            var m = Lit(_rings, 3);
            m.SetShaderParameter("ring_inner", rg.Inner);
            m.SetShaderParameter("ring_outer", rg.Outer);
            m.SetShaderParameter("ring_opacity", rg.Opacity);
            m.SetShaderParameter("ring_seed", seed.Y);
            m.SetShaderParameter("color_in", rg.In);
            m.SetShaderParameter("color_out", rg.Out);
            Body.AddChild(Mesh("Rings", new PlaneMesh { Size = Vector2.One * 2 * rg.Outer }, m));
        }
        if (Air != null)
        {
            float top = 1 + look.AtmosphereHeight;
            Body.AddChild(Mesh("Air", new SphereMesh { Radius = top, Height = 2 * top, RadialSegments = 96, Rings = 48 }, Air));
        }
    }

    /// <summary>A planet of a kind in its look, with its own seed (PlanetLooks.SeedOf its id).</summary>
    public static PlanetBody Build(string name, string kind, Vector3 seed) => new(name, PlanetLooks.For(kind), seed);

    private ShaderMaterial Lit(Shader sh, int priority)
    {
        var m = new ShaderMaterial { Shader = sh, RenderPriority = priority };
        _lit.Add(m);
        return m;
    }

    private static MeshInstance3D Mesh(string name, Mesh mesh, Material mat) =>
        new() { Name = name, Mesh = mesh, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };

    /// <summary>Every layer's sun (the direction toward it, world space, and its strength), and the ground's detail and
    /// the ground's and clouds' closeness for a camera <paramref name="alt"/> radii above the surface.</summary>
    public void Sync(Vector3 sunDir, float sunEnergy, float alt)
    {
        Ground.SetShaderParameter("sun_dir", sunDir);
        Ground.SetShaderParameter("sun_energy", sunEnergy);
        Ground.SetShaderParameter("octaves", Mathf.Clamp(6 + 0.75f * Mathf.Log(0.6f / alt) / Mathf.Log(2), 6, 10.5f));
        float close = 1 - Mathf.SmoothStep(0.02f, 0.4f, alt);
        Ground.SetShaderParameter("close", close);
        _cloudMat?.SetShaderParameter("close", close);
        Air?.SetShaderParameter("sun_dir", sunDir);
        Air?.SetShaderParameter("sun_energy", sunEnergy);
        foreach (ShaderMaterial m in _lit)
        {
            m.SetShaderParameter("sun_dir", sunDir);
            m.SetShaderParameter("sun_energy", sunEnergy);
        }
    }
}
