// BattleView.Planets: the planets of a battle in 3D, and the planet view (openspec/changes/mission-systems; owner,
// 2026-11-08: "In game Planets: pull in planets from godot-sandbox and falchion. Falchion will especially be important for
// us to beam down or examine planetary installations for exploratory or tactical purposes. Also just zooming down to
// planets is supper cool"). Each planet is its ground (planet.gdshader, after Falchion's planet material) and its air
// (planet_atmosphere.gdshader, after godot-sandbox's scattering shell) in its kind's look (data/space/planets.json,
// PlanetLooks), sunk a little below the battle plane at its radius (Planet.RadiusM). The installations a scan has found
// (Installation.Known) stand on its surface at their latitude and longitude: a small structure, a glow over it and a pool
// of light on the ground, in their state's colour (hostile Alert, held Accord, lost Caution, else Sky); an away team down
// at one (Battle.AwayTeams) pulses there.
// The planet view is the player's: the key (or the overlay's Planet view key) brings the camera to the selected planet
// (Battle.PlanetDest) on a glide, and from there the player orbits it by drag and comes down by the wheel or a pinch, right
// to the surface, where the view tilts toward the horizon as it comes down; the key again glides back to the ship. Nothing
// moves the camera but the player's own key, drag and zoom (CLAUDE.md 8). The view only reads the battle.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class BattleView
{
    /// <summary>How far a planet sits below the battle plane, as a share of its radius (the mockup's syncPlanets).</summary>
    public const float PlanetSink = 0.35f;
    /// <summary>The planet view's nearest and farthest camera heights over the surface, and where it starts, in radii.</summary>
    public const float PlanetAltMin = 0.004f, PlanetAltMax = 10f, PlanetAltStart = 2.2f;
    /// <summary>A site's pool of light on the ground, angular radius in radians, and its glow's least size on screen
    /// (a share of the camera's distance), so a found site still shows from across the battle.</summary>
    public const float SitePoolRad = 0.012f, SiteGlowMinK = 0.012f;
    private const int MaxSites = 8;

    private sealed class PlanetNode
    {
        public required Planet P;
        public required Node3D Root;
        public required ShaderMaterial Ground, Air;
        public readonly List<SiteNode> Sites = new();
    }

    private sealed class SiteNode
    {
        public required Installation I;
        public required Node3D Root;
        public required StandardMaterial3D Body, Glow, Pulse;
        public required MeshInstance3D GlowQuad, PulseQuad;
        public required Vector3 Dir;
    }

    private Node3D? _planetRoot;
    private readonly List<PlanetNode> _planetNodes = new();
    private static Shader? _groundShader, _airShader;
    private static SphereMesh? _groundMesh;

    /// <summary>The camera is on a planet (the planet view), not the ship.</summary>
    public bool PlanetView { get; private set; }
    private string? _viewPlanet;
    private float _planetAlt = PlanetAltStart, _shipDist, _distFrom, _distTo;

    // A new battle: its planets, each in its kind's look; the planet view stays only on a planet the new battle has too.
    private void BuildPlanets(Battle b)
    {
        _planetRoot?.QueueFree();
        _planetNodes.Clear();
        PlanetView = PlanetView && b.Planets.Any(p => p.Id == _viewPlanet);
        _planetRoot = new Node3D { Name = "Planets" };
        AddChild(_planetRoot);
        _groundShader ??= GD.Load<Shader>("res://src/Game/View/planet.gdshader");
        _airShader ??= GD.Load<Shader>("res://src/Game/View/planet_atmosphere.gdshader");
        _groundMesh ??= new SphereMesh { Radius = 1, Height = 2, RadialSegments = 160, Rings = 80 };
        foreach (Planet p in b.Planets)
        {
            PlanetLook look = PlanetLooks.For(p.Kind);
            var ground = new ShaderMaterial { Shader = _groundShader };
            var air = new ShaderMaterial { Shader = _airShader };
            PlanetLooks.Apply(look, PlanetLooks.SeedOf(p.Id), ground, air);
            float r = (float)(p.RadiusM / MPerU);
            var root = new Node3D { Name = p.Id, Position = PlanetCentre(p), Scale = Vector3.One * r };
            _planetRoot.AddChild(root);
            root.AddChild(new MeshInstance3D { Name = "Ground", Mesh = _groundMesh, MaterialOverride = ground, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
            if (look.AtmosphereStrength > 0)
            {
                float top = 1 + look.AtmosphereHeight;
                root.AddChild(new MeshInstance3D { Name = "Air", Mesh = new SphereMesh { Radius = top, Height = 2 * top, RadialSegments = 96, Rings = 48 },
                    MaterialOverride = air, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
            }
            var pn = new PlanetNode { P = p, Root = root, Ground = ground, Air = air };
            foreach (Installation i in p.Installations) pn.Sites.Add(Site(root, i));
            _planetNodes.Add(pn);
        }
    }

    /// <summary>A planet's centre in the scene: at its place, sunk below the plane by PlanetSink of its radius.</summary>
    private static Vector3 PlanetCentre(Planet p)
    {
        float r = (float)(p.RadiusM / MPerU);
        return new Vector3((float)(p.X / MPerU), -r * PlanetSink, (float)(p.Z / MPerU));
    }

    /// <summary>A direction on a planet from latitude and longitude (degrees): longitude 0 faces south (+Z), toward a camera
    /// that looks north, latitude up toward +Y.</summary>
    public static Vector3 SiteDir(double latDeg, double lonDeg)
    {
        float la = Mathf.DegToRad((float)latDeg), lo = Mathf.DegToRad((float)lonDeg);
        return new Vector3(Mathf.Cos(la) * Mathf.Sin(lo), Mathf.Sin(la), Mathf.Cos(la) * Mathf.Cos(lo));
    }

    // One installation's marker on the surface: a low block with a mast (the structure, sized on the planet so it reads up
    // close), the glow over it and the away team's pulse, both facing the camera; hidden until the site is known.
    private SiteNode Site(Node3D planet, Installation i)
    {
        Vector3 dir = SiteDir(i.LatDeg, i.LonDeg);
        Vector3 t = Mathf.Abs(dir.Y) > 0.95f ? Vector3.Right : Vector3.Up.Cross(dir).Normalized();
        var basis = new Basis(t, dir, t.Cross(dir));
        var root = new Node3D { Name = i.Id, Transform = new Transform3D(basis, dir), Visible = false };
        planet.AddChild(root);
        var body = new StandardMaterial3D { AlbedoColor = new Color("#8a9098"), Metallic = 0.4f, Roughness = 0.5f, EmissionEnabled = true, Emission = Colors.Black };
        const float u = 0.0016f;   // a structure about 0.16 % of the radius across (25 m on a 16 km world)
        root.AddChild(new MeshInstance3D { Name = "Block", Mesh = new BoxMesh { Size = new Vector3(u * 1.6f, u * 0.6f, u) }, MaterialOverride = body, Position = new Vector3(0, u * 0.3f, 0) });
        root.AddChild(new MeshInstance3D { Name = "Dome", Mesh = new SphereMesh { Radius = u * 0.45f, Height = u * 0.45f, IsHemisphere = true, RadialSegments = 12, Rings = 4 },
            MaterialOverride = body, Position = new Vector3(u * 0.5f, u * 0.6f, 0) });
        root.AddChild(new MeshInstance3D { Name = "Mast", Mesh = new CylinderMesh { TopRadius = u * 0.04f, BottomRadius = u * 0.07f, Height = u * 1.6f, RadialSegments = 6 },
            MaterialOverride = body, Position = new Vector3(-u * 0.5f, u * 1.1f, 0) });
        _tpQuad ??= new QuadMesh { Size = Vector2.One };
        Color c = Colors.White;
        // drawn over the ground (never cut by the horizon it stands on); hidden when the site is on the far side (SyncPlanets)
        var glow = Additive(c, true);
        var pulse = Additive(c, true);
        glow.NoDepthTest = true;
        pulse.NoDepthTest = true;
        var gq = new MeshInstance3D { Name = "Glow", Mesh = _tpQuad, MaterialOverride = glow, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Position = new Vector3(0, u * 2, 0) };
        var pq = new MeshInstance3D { Name = "Pulse", Mesh = _tpQuad, MaterialOverride = pulse, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Position = new Vector3(0, u * 2, 0), Visible = false };
        root.AddChild(gq);
        root.AddChild(pq);
        return new SiteNode { I = i, Root = root, Body = body, Glow = glow, Pulse = pulse, GlowQuad = gq, PulseQuad = pq, Dir = dir };
    }

    /// <summary>The colour of a site's state: held, lost, armed, or neither.</summary>
    private Color SiteColor(Installation i) => i.Secured ? _x.Kit.C.Accord : i.Failed ? _x.Kit.C.Caution : i.Hostile ? _x.Kit.C.Alert : _x.Kit.C.Sky;

    // Each frame: the sun and the camera's height on every planet's shaders, and each found site's marker and pool.
    private void SyncPlanets(Battle b)
    {
        Vector3 sun = _sun.GlobalTransform.Basis.Z.Normalized();
        float now = Time.GetTicksMsec() / 1000f;
        Vector3 cam = _cam.GlobalPosition;
        foreach (PlanetNode pn in _planetNodes)
        {
            float r = pn.Root.Scale.X;
            float alt = Math.Max(1e-4f, (cam.DistanceTo(pn.Root.Position) - r) / r);
            pn.Ground.SetShaderParameter("sun_dir", sun);
            pn.Ground.SetShaderParameter("sun_energy", _sun.LightEnergy);
            pn.Ground.SetShaderParameter("octaves", Mathf.Clamp(6 + 0.75f * Mathf.Log(0.6f / alt) / Mathf.Log(2), 6, 10.5f));
            pn.Ground.SetShaderParameter("close", 1 - Mathf.SmoothStep(0.02f, 0.4f, alt));
            pn.Air.SetShaderParameter("sun_dir", sun);
            pn.Air.SetShaderParameter("sun_energy", _sun.LightEnergy);
            var dirs = new Color[MaxSites];
            var cols = new Color[MaxSites];
            int n = 0;
            foreach (SiteNode s in pn.Sites)
            {
                s.Root.Visible = s.I.Known;
                if (!s.I.Known) continue;
                bool team = b.AwayTeams.Any(a => a.Planet == pn.P.Id && a.Site == s.I.Id);
                float beat = 0.5f + 0.5f * Mathf.Sin(now * 4f);
                Color col = SiteColor(s.I);
                s.Body.Emission = col * 0.6f;
                s.Glow.AlbedoColor = new Color(col, 0.9f);
                // the glow keeps a least size on screen; the away team's pulse swells and fades round it
                float size = Math.Max(0.012f * r, cam.DistanceTo(s.GlowQuad.GlobalPosition) * SiteGlowMinK) / r;
                s.GlowQuad.Scale = Vector3.One * size;
                bool facing = s.Dir.Dot(cam - s.Root.GlobalPosition) > 0;
                s.GlowQuad.Visible = facing;
                s.PulseQuad.Visible = team && facing;
                if (team)
                {
                    s.PulseQuad.Scale = Vector3.One * size * (1.6f + 1.6f * beat);
                    s.Pulse.AlbedoColor = new Color(_x.Kit.C.Sky, 0.75f * (1 - beat) + 0.15f);
                }
                if (n >= MaxSites) continue;
                dirs[n] = new Color(s.Dir.X, s.Dir.Y, s.Dir.Z, SitePoolRad);
                cols[n] = new Color(col.R, col.G, col.B, team ? 0.6f + 0.6f * beat : 1f);
                n++;
            }
            pn.Ground.SetShaderParameter("site_dir", dirs);
            pn.Ground.SetShaderParameter("site_col", cols);
        }
    }

    /// <summary>The Planet view key: the camera glides to the selected planet and orbits it, the wheel bringing it down to the
    /// surface; again, it glides back to your ship at the distance it had. False when no planet is selected.</summary>
    public bool TogglePlanetView()
    {
        if (PlanetView)
        {
            PlanetView = false;
            StartGlide(_shipDist);
            return true;
        }
        if (_x.Battle.PlanetDest is not { } id || !_x.Battle.Planets.Any(p => p.Id == id)) return false;
        _shipDist = Dist;
        _viewPlanet = id;
        _planetAlt = PlanetAltStart;
        PlanetView = true;
        TargetView = false;
        CamLock = false;
        StartGlide(null);
        return true;
    }

    // The planet the view is on, while it is.
    private Planet? ViewedPlanet() => PlanetView ? _x.Battle.Planets.FirstOrDefault(p => p.Id == _viewPlanet) : null;

    /// <summary>The camera's height over the viewed planet's surface, scene units (0 off the planet view).</summary>
    private float PlanetAltU() => ViewedPlanet() is { } p ? _planetAlt * (float)(p.RadiusM / MPerU) : 0;

    // The planet view's camera: round the planet's centre at the player's yaw and pitch, at the player's height; as it comes
    // down it tilts from the planet's centre toward the horizon ahead, its up turning from the sky to the ground's own up.
    private void PlanetCamera(Planet p, Vector3 focus, float gliding)
    {
        float r = (float)(p.RadiusM / MPerU);
        var dir = new Vector3(Mathf.Sin(Yaw) * Mathf.Cos(Pitch), Mathf.Sin(Pitch), Mathf.Cos(Yaw) * Mathf.Cos(Pitch));
        _cam.Position = focus + dir * Dist;
        float alt = Math.Max(0, Dist - r);
        float tilt = (1 - Mathf.SmoothStep(0.03f, 0.6f, _planetAlt)) * (1 - gliding);
        Vector3 north = (Vector3.Up - dir * dir.Dot(Vector3.Up)).Normalized();
        Vector3 ahead = focus + dir * r + north * alt * 3;
        Vector3 look = focus.Lerp(ahead, tilt);
        Vector3 up = Vector3.Up.Lerp(dir, tilt).Normalized();
        _cam.LookAt(look, up);
    }

    /// <summary>Every found installation on the camera's side of its planet and in front of the camera, with where it is on
    /// screen (console units): what the overlay names in the planet view.</summary>
    public IEnumerable<(Planet P, Installation I, Vector2 At)> SiteMarks()
    {
        Vector3 cam = _cam.GlobalPosition;
        foreach (PlanetNode pn in _planetNodes)
            foreach (SiteNode s in pn.Sites)
            {
                if (!s.I.Known) continue;
                Vector3 at = s.Root.GlobalPosition;
                if (s.Dir.Dot(cam - at) <= 0 || _cam.IsPositionBehind(at)) continue;
                yield return (pn.P, s.I, _cam.UnprojectPosition(at));
            }
    }
}
