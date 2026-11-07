// MapPieces: the campaign map's miniatures, built as the mockup builds them (game-flow mockup v15: flightStand,
// stationModel, freighterModel, makeStandee's planet, labelSprite, bannerMesh, strengthFlag, missionStandee, turnBadge).
// Ships and stations stand on clear flight posts over a black hex base ringed in their faction's colour (the player's own
// on gold); planets float in their hex with an atmosphere and an orbit ring; banners are cloth (banner.gdshader). Every
// piece is drawn from what the campaign says it is; nothing here decides anything. CampaignMap places them.
using System;
using System.Collections.Generic;
using FleetCommand.Campaign;
using Godot;

namespace FleetCommand.Game.Flow;

public static class MapPieces
{
    public static readonly StandardMaterial3D Post = new() { AlbedoColor = new Color(0.81f, 0.9f, 1f, 0.35f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, Roughness = 0.1f };
    public static readonly StandardMaterial3D Base = new() { AlbedoColor = new Color("#15181d"), Roughness = 0.55f, Metallic = 0.2f };
    public static readonly StandardMaterial3D Gold = new()
    {
        AlbedoColor = new Color("#d9a93e"), Metallic = 0.95f, Roughness = 0.28f, EmissionEnabled = true, Emission = new Color("#3a2604"), EmissionEnergyMultiplier = 0.6f,
    };
    public static readonly StandardMaterial3D PoleMetal = new() { AlbedoColor = new Color("#c9cdd2"), Metallic = 0.8f, Roughness = 0.35f };
    public static readonly StandardMaterial3D Rock = new() { AlbedoColor = new Color("#8a7a68"), Roughness = 0.95f };
    private static Shader? _banner;
    private static Shader BannerShader => _banner ??= GD.Load<Shader>("res://src/Game/Flow/banner.gdshader");

    /// <summary>The hull tints of the factions' stand-in parts (the mockup's HULL_TINT).</summary>
    public static Color HullTint(string fac) => fac switch
    {
        "concord" => new Color("#b9c6d6"), "krai" => new Color("#6a6062"), "ssarith" => new Color("#8a9478"), "kelvane" => new Color("#c9bfa9"), "vos" => new Color("#b39a6e"), _ => new Color("#a0a6ae"),
    };

    public static StandardMaterial3D Unlit(Color c, float a = 1) => new()
    {
        AlbedoColor = new Color(c, a), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = a < 1 ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
    };

    private static MeshInstance3D M(Mesh mesh, Material mat, Vector3 at) => new() { Mesh = mesh, MaterialOverride = mat, Position = at };

    /// <summary>A flight stand: a hex base, its rim in the faction's colour (gold for the player's), a clear post.</summary>
    public static Node3D FlightStand(Color rim, float height, float size, bool gold)
    {
        var g = new Node3D { Name = "Stand" };
        // CylinderMesh starts its ring on +z; a sixth of a turn more puts a corner on +x, as the board's hexes
        var b = M(new CylinderMesh { TopRadius = 0.22f * size, BottomRadius = 0.26f * size, Height = 0.06f, RadialSegments = 6, Rings = 1 }, gold ? Gold : Base, new Vector3(0, 0.03f, 0));
        b.RotationDegrees = new Vector3(0, 30, 0);
        g.AddChild(b);
        var r = M(new TorusMesh { InnerRadius = 0.24f * size - 0.018f, OuterRadius = 0.24f * size + 0.018f, Rings = 6, RingSegments = 6 }, gold ? Gold : Unlit(rim), new Vector3(0, 0.062f, 0));
        r.RotationDegrees = new Vector3(0, 30, 0);
        r.Scale = new Vector3(1, 0.6f, 1);
        g.AddChild(r);
        g.AddChild(M(new CylinderMesh { TopRadius = 0.018f, BottomRadius = 0.018f, Height = height, RadialSegments = 8 }, Post, new Vector3(0, height / 2 + 0.06f, 0)));
        return g;
    }

    /// <summary>A station: a hub, a ring with a band of the faction's colour, spokes and a mast.</summary>
    public static Node3D Station(string fac, Color col)
    {
        var g = new Node3D { Name = "Station" };
        var metal = new StandardMaterial3D { AlbedoColor = HullTint(fac), Metallic = 0.5f, Roughness = 0.45f };
        var trim = new StandardMaterial3D { AlbedoColor = col, EmissionEnabled = true, Emission = col, EmissionEnergyMultiplier = 0.25f };
        g.AddChild(M(new SphereMesh { Radius = 0.16f, Height = 0.32f, RadialSegments = 20, Rings = 10 }, metal, Vector3.Zero));
        g.AddChild(M(new TorusMesh { InnerRadius = 0.295f, OuterRadius = 0.385f, Rings = 36, RingSegments = 10 }, metal, Vector3.Zero));
        // the band: a short arc of the ring, a little proud of it so the two never share a surface
        var band = new SurfaceTool();
        band.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < 12; i++)
        {
            float a0 = Mathf.Pi / 3 * i / 12f, a1 = Mathf.Pi / 3 * (i + 1) / 12f;
            Vector3 P(float a, float r, float y) => new(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
            foreach (float y in new[] { 0.05f, -0.05f })
            {
                band.AddVertex(P(a0, 0.29f, y)); band.AddVertex(P(a1, 0.29f, y)); band.AddVertex(P(a1, 0.39f, y));
                band.AddVertex(P(a0, 0.29f, y)); band.AddVertex(P(a1, 0.39f, y)); band.AddVertex(P(a0, 0.39f, y));
            }
        }
        band.GenerateNormals();
        var bm = M(band.Commit(), trim, Vector3.Zero);
        g.AddChild(bm);
        for (int k = 0; k < 4; k++)
        {
            var sp = M(new BoxMesh { Size = new Vector3(0.62f, 0.025f, 0.025f) }, metal, Vector3.Zero);
            sp.Rotation = new Vector3(0, k * Mathf.Pi / 4, 0);
            g.AddChild(sp);
        }
        g.AddChild(M(new CylinderMesh { TopRadius = 0.03f, BottomRadius = 0.05f, Height = 0.42f, RadialSegments = 8 }, metal, Vector3.Zero));
        return g;
    }

    /// <summary>A freighter placeholder: a spine with three cargo pods (the middle in the faction's colour), a bridge aft and
    /// a drive. Bow +z.</summary>
    public static Node3D Freighter(string fac, Color col)
    {
        var g = new Node3D { Name = "Freighter" };
        var hull = new StandardMaterial3D { AlbedoColor = HullTint(fac), Metallic = 0.4f, Roughness = 0.55f };
        var band = new StandardMaterial3D { AlbedoColor = col, EmissionEnabled = true, Emission = col, EmissionEnergyMultiplier = 0.2f, Roughness = 0.6f };
        g.AddChild(M(new BoxMesh { Size = new Vector3(0.03f, 0.03f, 0.4f) }, hull, Vector3.Zero));
        for (int k = 0; k < 3; k++) g.AddChild(M(new BoxMesh { Size = new Vector3(0.085f, 0.07f, 0.085f) }, k == 1 ? band : hull, new Vector3(0, 0, 0.1f - k * 0.1f)));
        g.AddChild(M(new BoxMesh { Size = new Vector3(0.06f, 0.05f, 0.06f) }, hull, new Vector3(0, 0.02f, -0.17f)));
        var drive = M(new CylinderMesh { TopRadius = 0.022f, BottomRadius = 0.03f, Height = 0.04f, RadialSegments = 10 }, Unlit(new Color("#9fd2ff")), new Vector3(0, 0, -0.215f));
        drive.RotationDegrees = new Vector3(90, 0, 0);
        g.AddChild(drive);
        return g;
    }

    private static readonly Dictionary<int, Texture2D> PlanetTex = new();

    /// <summary>A planet's banded surface (the mockup's planetTex), made once per hue.</summary>
    public static Texture2D Planet(int hue)
    {
        if (PlanetTex.TryGetValue(hue, out Texture2D? t)) return t;
        var R = new Mulberry32((uint)(hue * 7 + 3));
        const int w = 256, h = 128;
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);
        for (int y = 0; y < h; y++)
        {
            double v = 0.35 + 0.25 * Math.Sin(y * 0.18 + R.Next() * 0.4) + R.Next() * 0.08;
            Color c = Hsl((hue + 20 * Math.Sin(y * 0.06)) / 360.0, 0.45, (18 + v * 40) / 100.0);
            for (int x = 0; x < w; x++) img.SetPixel(x, y, c);
        }
        for (int i = 0; i < 90; i++)
        {
            double cx = R.Next() * w, cy = R.Next() * h, rx = 5 + R.Next() * 30, ry = 1 + R.Next() * 3, l = (50 + R.Next() * 30) / 100.0;
            Color sc = Hsl((hue + 30) / 360.0, 0.5, l);
            for (int y = (int)(cy - ry); y <= (int)(cy + ry); y++)
                for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
                {
                    double dx = (x - cx) / rx, dy = (y - cy) / ry;
                    if (dx * dx + dy * dy > 1 || y < 0 || y >= h) continue;
                    int xx = ((x % w) + w) % w;
                    img.SetPixel(xx, y, img.GetPixel(xx, y).Lerp(sc, 0.12f));
                }
        }
        return PlanetTex[hue] = ImageTexture.CreateFromImage(img);
    }

    private static Color Hsl(double h, double s, double l)
    {
        h = ((h % 1) + 1) % 1;
        double q = l < 0.5 ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
        double T(double t) { t = ((t % 1) + 1) % 1; return t < 1 / 6.0 ? p + (q - p) * 6 * t : t < 0.5 ? q : t < 2 / 3.0 ? p + (q - p) * (2 / 3.0 - t) * 6 : p; }
        return new Color((float)T(h + 1 / 3.0), (float)T(h), (float)T(h - 1 / 3.0));
    }

    private static Texture2D? _glow;
    /// <summary>A soft round glow (the mockup's glowTex), for halos, atmospheres and stars.</summary>
    public static Texture2D Glow
    {
        get
        {
            if (_glow != null) return _glow;
            var g = new Gradient();
            g.Offsets = new[] { 0f, 0.4f, 1f };
            g.Colors = new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0.35f), new Color(1, 1, 1, 0) };
            return _glow = new GradientTexture2D { Gradient = g, Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1, 0.5f), Width = 128, Height = 128 };
        }
    }

    /// <summary>An additive camera-facing glow.</summary>
    public static Sprite3D Halo(Color col, float size, float alpha = 1) => new()
    {
        Texture = Glow, Modulate = new Color(col, alpha), PixelSize = size / 128f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
        Shaded = false, AlphaCut = SpriteBase3D.AlphaCutMode.Disabled, Transparent = true, DoubleSided = true,
    };

    /// <summary>A planet: its banded sphere, an atmosphere, an orbit ring in the holder's colour.</summary>
    public static Node3D PlanetPiece(int hue, Color holder, float size)
    {
        var g = new Node3D { Name = "Planet" };
        float r = 0.36f * size;
        var ball = M(new SphereMesh { Radius = r, Height = 2 * r, RadialSegments = 40, Rings = 20 }, new StandardMaterial3D { AlbedoTexture = Planet(hue), Roughness = 0.9f }, new Vector3(0, r + 0.12f, 0));
        ball.Name = "Ball";
        g.AddChild(ball);
        var atmo = Halo(new Color("#8fc4ff"), r * 3, 0.35f);
        atmo.Position = ball.Position;
        g.AddChild(atmo);
        var orbit = M(new TorusMesh { InnerRadius = r * 1.55f - 0.012f, OuterRadius = r * 1.55f + 0.012f, Rings = 64, RingSegments = 6 }, Unlit(holder, 0.8f), ball.Position);
        orbit.Rotation = new Vector3(0.25f, 0, 0);
        g.AddChild(orbit);
        return g;
    }

    /// <summary>A name label that keeps its size on screen (the mockup's labelSprite), in the faction's colour, outlined.</summary>
    public static Label3D Label(string text, Color col) => new()
    {
        Text = text.ToUpperInvariant(), Modulate = col, FontSize = 44, OutlineSize = 12, OutlineModulate = new Color(0.012f, 0.024f, 0.04f, 0.9f),
        FixedSize = true, PixelSize = 0.00036f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, NoDepthTest = true, RenderPriority = 4, OutlineRenderPriority = 3,
        Font = LabelFont,
    };

    private static FontFile? _font;
    private static FontFile LabelFont => _font ??= GD.Load<FontFile>("res://assets/fonts/Antonio-VariableFont_wght.ttf");

    /// <summary>A cloth banner: w x h, hung from its top or left edge, its shape and pips from banner.gdshader.</summary>
    public static MeshInstance3D Cloth(float w, float h, bool left, Color ink, float seed, float tail, float notch, Vector2 px, int pips = 0)
    {
        var mesh = new PlaneMesh { Size = new Vector2(w, h), Orientation = PlaneMesh.OrientationEnum.Z, SubdivideWidth = 10, SubdivideDepth = 6,
            CenterOffset = new Vector3(left ? w / 2 : 0, -h / 2, 0) };
        var mat = new ShaderMaterial { Shader = BannerShader };
        mat.SetShaderParameter("ink", ink);
        mat.SetShaderParameter("seed", seed);
        mat.SetShaderParameter("hang", left ? 1 : 0);
        mat.SetShaderParameter("tail", tail);
        mat.SetShaderParameter("notch", notch);
        mat.SetShaderParameter("px", px);
        mat.SetShaderParameter("pips", pips);
        return new MeshInstance3D { Mesh = mesh, MaterialOverride = mat, Name = "Cloth" };
    }

    /// <summary>A strength flag on its pole: the faction's colour, one pip per step of strength (1 to 5), longer the stronger.</summary>
    public static Node3D StrengthFlag(Color col, int n, float size, float seed)
    {
        var g = new Node3D { Name = "Strength" };
        float h = 0.95f * size;
        g.AddChild(M(new CylinderMesh { TopRadius = 0.008f, BottomRadius = 0.008f, Height = h, RadialSegments = 6 }, PoleMetal, new Vector3(0, h / 2, 0)));
        g.AddChild(M(new SphereMesh { Radius = 0.018f, Height = 0.036f, RadialSegments = 10, Rings = 6 }, Gold, new Vector3(0, h + 0.01f, 0)));
        float tall = (72 + n * 34) / 256f;
        var flag = Cloth(0.16f * size, 0.64f * size, true, col, seed, tall, 16 / 256f, new Vector2(64, 256), n);
        flag.Position = new Vector3(0.008f, h - 0.02f, 0);
        g.AddChild(flag);
        return g;
    }

    /// <summary>A mission's standee: a pole on a small base, its gonfalon in the mission's ink with the mission's letter.</summary>
    public static Node3D MissionStandee(string glyph, Color ink, float seed)
    {
        var g = new Node3D { Name = "Mission" };
        const float h = 1.05f;
        var b = M(new CylinderMesh { TopRadius = 0.12f, BottomRadius = 0.14f, Height = 0.05f, RadialSegments = 6, Rings = 1 }, Base, new Vector3(0, 0.025f, 0));
        b.RotationDegrees = new Vector3(0, 30, 0);
        g.AddChild(b);
        var rim = M(new TorusMesh { InnerRadius = 0.116f, OuterRadius = 0.144f, Rings = 6, RingSegments = 6 }, Unlit(ink), new Vector3(0, 0.052f, 0));
        rim.RotationDegrees = new Vector3(0, 30, 0);
        rim.Scale = new Vector3(1, 0.6f, 1);
        g.AddChild(rim);
        g.AddChild(M(new CylinderMesh { TopRadius = 0.011f, BottomRadius = 0.011f, Height = h, RadialSegments = 6 }, PoleMetal, new Vector3(0, h / 2 + 0.05f, 0)));
        var bar = M(new CylinderMesh { TopRadius = 0.008f, BottomRadius = 0.008f, Height = 0.3f, RadialSegments = 6 }, PoleMetal, new Vector3(0, h + 0.02f, 0));
        bar.RotationDegrees = new Vector3(0, 0, 90);
        g.AddChild(bar);
        var cloth = Cloth(0.28f, 0.385f, false, ink, seed, 1, 32 / 176f, new Vector2(128, 176));
        cloth.Position = new Vector3(0, h + 0.012f, 0.012f);
        g.AddChild(cloth);
        var letter = new Label3D
        {
            Text = glyph, Modulate = new Color("#1a1006"), FontSize = 120, PixelSize = 0.0012f, Position = new Vector3(0, h + 0.012f - 0.15f, 0.03f),
            Font = LabelFont, OutlineSize = 0, DoubleSided = true, Shaded = false,
        };
        g.AddChild(letter);
        return g;
    }

    /// <summary>A turn badge on a multi-turn route: the turn's number in a ring, filled for this turn.</summary>
    public static Node3D TurnBadge(int n, bool now)
    {
        var g = new Node3D { Name = "Badge" + n };
        g.AddChild(new Sprite3D
        {
            Texture = Disc(now), FixedSize = true, PixelSize = 0.00038f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, NoDepthTest = true, RenderPriority = 6,
            Shaded = false, Transparent = true, AlphaCut = SpriteBase3D.AlphaCutMode.Disabled,
        });
        g.AddChild(new Label3D
        {
            Text = n.ToString(), Modulate = now ? new Color("#1a1006") : new Color("#ffd08a"), FontSize = 52, OutlineSize = 0, Font = LabelFont,
            FixedSize = true, PixelSize = 0.0005f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, NoDepthTest = true, RenderPriority = 7,
        });
        return g;
    }

    private static readonly Texture2D?[] Discs = new Texture2D?[2];
    // a turn badge's disc: an amber ring, filled amber for this turn, dark for later ones
    private static Texture2D Disc(bool now)
    {
        if (Discs[now ? 1 : 0] is { } t) return t;
        const int n = 96;
        var img = Image.CreateEmpty(n, n, false, Image.Format.Rgba8);
        Color amber = new("#ffb547"), dark = new("#1a1206");
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = new Vector2(x + 0.5f - n / 2f, y + 0.5f - n / 2f).Length();
                float a = Math.Clamp(44 - d, 0, 1);
                Color c = d > 36.5f ? amber : now ? amber : dark;
                img.SetPixel(x, y, new Color(c, a));
            }
        return (Discs[now ? 1 : 0] = ImageTexture.CreateFromImage(img))!;
    }

    /// <summary>A flat ring round a hex, between two radii (the selection, the hover).</summary>
    public static MeshInstance3D HexRing(float r0, float r1, Color col)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < 6; i++)
        {
            Vector3 C(int k, float r) { float a = Mathf.Pi / 3 * k; return new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); }
            st.AddVertex(C(i, r0)); st.AddVertex(C(i + 1, r1)); st.AddVertex(C(i, r1));
            st.AddVertex(C(i, r0)); st.AddVertex(C(i + 1, r0)); st.AddVertex(C(i + 1, r1));
        }
        return new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = Unlit(col) };
    }
}
