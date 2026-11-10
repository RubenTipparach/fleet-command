// NebulaDeck: the one builder of the decks of cloud a battle's nebula plumes rise between (openspec/changes/nebula-plumes;
// owner, 2026-11-10: "The clouds need to stretch up in columns of varying widths across the map", "make a cloud mesh for
// top and bottom of nebula and fade out the edges, so it looks like the nebula columns are connected to a larger structure
// above and below the map"). A battle with any plume has two decks, mirror images about the battle plane, each a box drawn
// by nebula_deck.gdshader over the field of plumes (whole a margin past the outermost column, thinning away beyond it),
// clear round every planet, in the colours of the plumes' own looks. Every plume's column reaches from one deck into the
// other (NebulaPlume, which reads the same numbers: DeckLook). The numbers are data/space/nebula_decks.json, read and
// validated once (a missing or unknown key, or a number out of range, stops the view naming the file and field). The view
// only: the rules know nothing of the decks.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;
using Godot;

namespace FleetCommand.Game.View;

/// <summary>The decks' numbers (data/space/nebula_decks.json), km where the name says so.</summary>
public sealed record DeckLook(float UndersideKm, float ThicknessKm, float HangKm, float JoinKm, float FunnelKm, float MarginKm, float FadeKm,
    float PlanetClearX, float Coverage, float AbsorptionPerKm, float BaseKm, float DetailKm, float Erosion, float DriftPerS, float GlowStrength, float Fill, float ColumnShade,
    float Variety, float FarFromKm, float FarToKm, float Contrast,
    float FogKm, float FogLit, float FogSky,
    string Style, float DeckPuffKm)
{
    private const string File = "data/space/nebula_decks.json";
    private static DeckLook? _look;

    /// <summary>How a nebula is drawn (owner, 2026-11-10: "lets see how all one or the other looks"): marched cloud, puffs
    /// of displaced mesh, or Crab Breeze's lit billboards; "none" draws no cloud at all (the fog stays), a measurement
    /// instrument only: the frame time of a battle without its cloud, against which each style's cost is read.</summary>
    public static readonly string[] Styles = { "volume", "meshes", "billboards", "none" };

    /// <summary>Where a column flares into a deck, km off the plane: the deck's face where it reaches down round a column's
    /// root.</summary>
    public float ColumnFlareKm => UndersideKm - JoinKm;

    /// <summary>The numbers, read from the file once.</summary>
    public static DeckLook Get()
    {
        if (_look != null) return _look;
        string text = FileAccess.GetFileAsString("res://" + File);
        if (string.IsNullOrEmpty(text)) throw new DataError($"{File}: cannot be opened");
        StrictJson o = StrictJson.Parse(text, File);
        var look = new DeckLook(PlanetLooks.Range(o, "underside_km", 5, 200), PlanetLooks.Range(o, "thickness_km", 2, 200), PlanetLooks.Range(o, "hang_km", 0, 50),
            PlanetLooks.Range(o, "join_km", 0, 100), PlanetLooks.Range(o, "funnel_km", 0, 200), PlanetLooks.Range(o, "margin_km", 0, 500), PlanetLooks.Range(o, "fade_km", 1, 500),
            PlanetLooks.Range(o, "planet_clear_x", 0, 5), PlanetLooks.Range(o, "coverage", 0, 1), PlanetLooks.Range(o, "absorption_per_km", 0.01, 10),
            PlanetLooks.Range(o, "base_km", 1, 1000), PlanetLooks.Range(o, "detail_km", 0.5, 500), PlanetLooks.Range(o, "erosion", 0, 1),
            PlanetLooks.Range(o, "drift_per_s", 0, 0.2), PlanetLooks.Range(o, "glow_strength", 0, 2), PlanetLooks.Range(o, "fill", 0, 1),
            PlanetLooks.Range(o, "column_shade", 0, 1), PlanetLooks.Range(o, "variety", 0, 1), PlanetLooks.Range(o, "far_from_km", 0, 2000),
            PlanetLooks.Range(o, "far_to_km", 1, 3000), PlanetLooks.Range(o, "contrast", 0, 1),
            PlanetLooks.Range(o, "fog_km", 5, 5000), PlanetLooks.Range(o, "fog_lit", 0, 1), PlanetLooks.Range(o, "fog_sky", 0, 1),
            o.Str("style"), PlanetLooks.Range(o, "deck_puff_km", 2, 100));
        // the stills tool and a comparison run may name another style (--nebula-style=volume|meshes|billboards)
        foreach (string a in OS.GetCmdlineUserArgs())
            if (a.StartsWith("--nebula-style=")) look = look with { Style = a["--nebula-style=".Length..] };
        if (Array.IndexOf(Styles, look.Style) < 0) throw new DataError($"{File}.style: one of {string.Join(", ", Styles)}, not '{look.Style}'");
        o.Done();
        if (look.FarToKm <= look.FarFromKm) throw new DataError($"{File}.far_to_km: must be beyond far_from_km");
        if (look.JoinKm + look.HangKm * 1.3f >= look.UndersideKm) throw new DataError($"{File}.join_km: join_km and 1.3 x hang_km must stay under underside_km (the deck would reach the battle plane)");
        return _look = look;
    }
}

public sealed class NebulaDeck
{
    /// <summary>A deck's place in the draw order: after every planet layer, before the plumes (NebulaPlume.Priority).</summary>
    public const int Priority = -2;
    /// <summary>The columns and planets a deck knows of (the shader's arrays).</summary>
    public const int Columns = 16, Planets = 4;

    public readonly Node3D Node;
    private readonly ShaderMaterial[] _mats;
    private static Shader? _shader;

    private NebulaDeck(Node3D node, ShaderMaterial[] mats) { Node = node; _mats = mats; }

    /// <summary>The two decks over a battle's plumes (at most <see cref="Columns"/> of them, the widest first), clear round
    /// its planets (x, z and radius, metres), at <paramref name="mPerU"/> metres to the scene's unit; seed (three numbers 0
    /// to 1) cuts them from their own place in the noise.</summary>
    public static NebulaDeck Build(IReadOnlyList<NebulaPlume> plumes, IEnumerable<(double X, double Z, double R)> planets, Vector3 seed, float mPerU)
    {
        DeckLook k = DeckLook.Get();
        float u = 1000f / mPerU;   // scene units a km
        NebulaPlume[] cols = plumes.OrderByDescending(p => p.Def.RM).Take(Columns).ToArray();
        // the field: round the columns' middle, whole a margin past the outermost
        var centre = new Vector2((float)cols.Average(p => p.Def.XM), (float)cols.Average(p => p.Def.ZM)) / mPerU;
        float fieldR = cols.Max(p => new Vector2((float)p.Def.XM, (float)p.Def.ZM).DistanceTo(centre * mPerU) + (float)p.Def.RM) / mPerU + k.MarginKm * u;
        float fadeR = fieldR + k.FadeKm * u;
        var column = new Vector4[Columns];
        var lit = new Color[Columns];
        var deep = new Color[Columns];
        var glow = new Color[Columns];
        for (int i = 0; i < cols.Length; i++)
        {
            // w: which decks reach down to meet it (1 the one above, 2 the one below, 3 both)
            column[i] = new Vector4((float)(cols[i].Def.XM / mPerU), (float)(cols[i].Def.ZM / mPerU), cols[i].RadiusU, (cols[i].JoinsAbove ? 1 : 0) + (cols[i].JoinsBelow ? 2 : 0));
            lit[i] = cols[i].Look.Lit;
            deep[i] = cols[i].Look.Deep;
            glow[i] = cols[i].Look.Glow;
        }
        Color Mean(Color[] c) { var m = new Color(0, 0, 0, 1); for (int i = 0; i < cols.Length; i++) m += c[i] / cols.Length; return m; }
        var planet = new Vector4[Planets];
        int np = 0;
        foreach ((double x, double z, double r) in planets.OrderBy(p => (p.X / mPerU - centre.X) * (p.X / mPerU - centre.X) + (p.Z / mPerU - centre.Y) * (p.Z / mPerU - centre.Y)).Take(Planets))
            planet[np++] = new Vector4((float)(x / mPerU), (float)(z / mPerU), (float)(r / mPerU) * k.PlanetClearX, 0);

        _shader ??= GD.Load<Shader>("res://src/Game/View/nebula_deck.gdshader");
        var root = new Node3D { Name = "Decks" };
        var mats = new ShaderMaterial[2];
        float yIn = (k.UndersideKm - k.HangKm * 1.3f - k.JoinKm) * u, yOut = (k.UndersideKm + k.ThicknessKm) * u;
        for (int s = 0; s < 2; s++)
        {
            float side = s == 0 ? 1 : -1;
            var m = new ShaderMaterial { Shader = _shader, RenderPriority = Priority };
            m.SetShaderParameter("noise_tex", NebulaPlume.Noise());
            m.SetShaderParameter("side", side);
            m.SetShaderParameter("underside", k.UndersideKm * u);
            m.SetShaderParameter("thickness", k.ThicknessKm * u);
            m.SetShaderParameter("hang", k.HangKm * u);
            m.SetShaderParameter("join", k.JoinKm * u);
            m.SetShaderParameter("funnel", k.FunnelKm * u);
            m.SetShaderParameter("centre", centre);
            m.SetShaderParameter("field_r", fieldR);
            m.SetShaderParameter("fade_r", fadeR);
            m.SetShaderParameter("reach", fadeR * 2.5f);
            m.SetShaderParameter("coverage", k.Coverage);
            m.SetShaderParameter("absorption", k.AbsorptionPerKm / u);
            m.SetShaderParameter("base_scale", 1f / (k.BaseKm * u));
            m.SetShaderParameter("detail_scale", 1f / (k.DetailKm * u));
            m.SetShaderParameter("erosion", k.Erosion);
            m.SetShaderParameter("contrast", k.Contrast);
            m.SetShaderParameter("drift", k.DriftPerS);
            m.SetShaderParameter("glow_strength", k.GlowStrength);
            m.SetShaderParameter("fill", k.Fill);
            m.SetShaderParameter("column_shade", k.ColumnShade);
            m.SetShaderParameter("variety", k.Variety);
            m.SetShaderParameter("far_from", k.FarFromKm * u);
            m.SetShaderParameter("far_to", k.FarToKm * u);
            m.SetShaderParameter("seed", seed + new Vector3(0.37f, 0.11f, 0.73f) * s);
            m.SetShaderParameter("columns", cols.Length);
            m.SetShaderParameter("column", column);
            m.SetShaderParameter("column_lit", lit);
            m.SetShaderParameter("column_deep", deep);
            m.SetShaderParameter("column_glow", glow);
            m.SetShaderParameter("mean_lit", Mean(lit));
            m.SetShaderParameter("mean_deep", Mean(deep));
            m.SetShaderParameter("mean_glow", Mean(glow));
            m.SetShaderParameter("planets", np);
            m.SetShaderParameter("planet", planet);
            mats[s] = m;
            root.AddChild(new MeshInstance3D
            {
                Name = s == 0 ? "Above" : "Below", MaterialOverride = m, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Mesh = new BoxMesh { Size = new Vector3(2 * fadeR, yOut - yIn, 2 * fadeR) },
                Position = new Vector3(centre.X, side * (yIn + yOut) / 2, centre.Y),
                // the box bounds the march only; the culler must not drop it when the camera is between the decks
                ExtraCullMargin = fadeR,
            });
        }
        return new NebulaDeck(root, mats);
    }

    /// <summary>The haze of a battle among plumes: its colour (the plumes' looks, each by the area it covers, between its
    /// shadow and its light by fog_lit) and its density a scene unit (one over fog_km: at that distance a thing shows
    /// through a little over a third of the haze).</summary>
    public static (Color Color, float DensityPerU) Fog(IReadOnlyList<NebulaPlume> plumes, float mPerU)
    {
        DeckLook k = DeckLook.Get();
        var c = new Color(0, 0, 0, 1);
        double w = plumes.Sum(p => p.Def.RM * p.Def.RM);
        foreach (NebulaPlume p in plumes)
        {
            float s = (float)(p.Def.RM * p.Def.RM / w);
            Color mixed = p.Look.Deep.Lerp(p.Look.Lit, k.FogLit);
            c = new Color(c.R + mixed.R * s, c.G + mixed.G * s, c.B + mixed.B * s, 1);
        }
        return (c, mPerU / (k.FogKm * 1000f));
    }

    /// <summary>The battle's fog, laid over the cloud at its own depth.</summary>
    public void SetFog(Color color, float densityPerU)
    {
        foreach (ShaderMaterial m in _mats)
        {
            m.SetShaderParameter("fog_color", color);
            m.SetShaderParameter("fog_density", densityPerU);
        }
    }

    /// <summary>The sun (the direction toward it, world space, and its strength) and the battle's clock in seconds.</summary>
    public void Sync(Vector3 sunDir, float sunEnergy, double battleS)
    {
        foreach (ShaderMaterial m in _mats)
        {
            m.SetShaderParameter("sun_dir", sunDir);
            m.SetShaderParameter("sun_energy", sunEnergy);
            m.SetShaderParameter("deck_time", (float)battleS);
        }
    }
}
