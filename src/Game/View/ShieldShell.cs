// ShieldShell: a ship's 3D shield display (battle-console design; owner, 2026-11-01: "shield facing should be hexagons
// here"; 2026-11-04: "shield display not matching mockup, also one change needed is to add shield display to all ships as
// well"). A port of the battle console mockup's shieldShell(): a hexagon in the ship's plane, an edge toward the bow,
// each of the six facings up to three glowing bands outward, a small gap between facings, each band a slab of soft light as tall as
// it is wide (bright along its middle, fading to either side), added to the scene. A facing shows as many bands as its strength
// earns out of its class's shield_layers (a frigate one, larger ships more; for three: above two thirds three, above a third two, else one; none when the shields are down or the facing
// is empty), in its strength's colour (the systems display's rule, ShipPanel.ShieldColorOf); a hit brightens its inner
// band. Every ship carries one. View only: it reads the simulation, never writes it.
using System;
using FleetCommand.Game.Ui.Kit;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class ShieldShell : Node3D
{
    /// <summary>The hexagon's inner radius, band width and band spacing, as shares of the ship's length (the mockup's 0.62, 0.1, 0.13 of the radius).</summary>
    public const float RadiusShare = 0.62f, WidthShare = 0.1f, StepShare = 0.13f, Gap = 0.1f;

    private static Texture2D? _band;
    private readonly StandardMaterial3D[,] _mats = new StandardMaterial3D[6, 3];

    // The band's soft profile across its width (the mockup's bandTex).
    private static Texture2D Band => _band ??= new GradientTexture2D
    {
        Width = 4, Height = 64, Fill = GradientTexture2D.FillEnum.Linear, FillFrom = new Vector2(0, 0), FillTo = new Vector2(0, 1),
        Gradient = new Gradient
        {
            Offsets = new[] { 0f, 0.45f, 0.62f, 0.75f, 1f },
            Colors = new[] { new Color(1, 1, 1, 0), new Color(1, 1, 1, 0.35f), new Color(1, 1, 1, 1), new Color(1, 1, 1, 0.3f), new Color(1, 1, 1, 0) },
        },
    };

    /// <summary>Builds the display for a ship lengthU long (scene units), in the frame of its model holder (bow toward -Z, starboard +X).</summary>
    public ShieldShell(float lengthU)
    {
        Name = "Shields";
        float rc = lengthU * RadiusShare, w = rc * WidthShare, step = rc * StepShare;
        float half = 30 * Mathf.Pi / 180, g = 60 * Gap / 2 * Mathf.Pi / 180, k0 = Mathf.Cos(half) / Mathf.Cos(half - g);
        // bearing b, clockwise from the bow seen from above: the bow is -Z, starboard +X
        static Vector3 At(float b, float r) => new(Mathf.Sin(b) * r, 0, -Mathf.Cos(b) * r);
        for (int i = 0; i < 6; i++)
            for (int l = 0; l < 3; l++)
            {
                float r0 = rc + l * step, b0 = i * Mathf.Pi / 3 - half + g, b1 = i * Mathf.Pi / 3 + half - g;
                Vector3 a = At(b0, r0 * k0), b = At(b1, r0 * k0), c = At(b1, (r0 + w) * k0), d = At(b0, (r0 + w) * k0), up = new(0, w / 2, 0);
                // a slab, not a flat strip (owner, 2026-11-06: "Shield faces I think should have zdepth"): as tall as it is wide,
                // its top and bottom faces glowing across the band's width, its inner and outer walls across its height
                Vector3[][] quads =
                {
                    new[] { a + up, b + up, c + up, d + up }, new[] { a - up, b - up, c - up, d - up },
                    new[] { a - up, b - up, b + up, a + up }, new[] { d - up, c - up, c + up, d + up },
                };
                var v = new Vector3[16];
                var uv = new Vector2[16];
                var idx = new int[24];
                for (int q = 0; q < 4; q++)
                {
                    for (int k = 0; k < 4; k++) { v[q * 4 + k] = quads[q][k]; uv[q * 4 + k] = new Vector2(k is 1 or 2 ? 1 : 0, k >= 2 ? 1 : 0); }
                    int n = q * 4;
                    new[] { n, n + 1, n + 2, n, n + 2, n + 3 }.CopyTo(idx, q * 6);
                }
                var arrays = new Godot.Collections.Array();
                arrays.Resize((int)Mesh.ArrayType.Max);
                arrays[(int)Mesh.ArrayType.Vertex] = v;
                arrays[(int)Mesh.ArrayType.TexUV] = uv;
                arrays[(int)Mesh.ArrayType.Index] = idx;
                var mesh = new ArrayMesh();
                mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
                var m = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    BlendMode = BaseMaterial3D.BlendModeEnum.Add, AlbedoTexture = Band, CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                    DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled, DisableReceiveShadows = true, AlbedoColor = new Color(1, 1, 1, 0),
                };
                _mats[i, l] = m;
                AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = m, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
            }
    }

    /// <summary>How many bands a facing shows (the mockup's shieldLayers, out of the class's ShieldLayers: a frigate's one band
    /// at any strength, a cruiser's three above two thirds, two above a third, else one).</summary>
    public static int Layers(Ship s, int i)
    {
        if (!s.ShieldsUp || s.Shields[i] <= 0.5) return 0;
        double f = s.Shields[i] / s.Class.ShieldsHp[i];
        return Math.Clamp((int)Math.Ceiling(f * s.Class.ShieldLayers - 0.02), 1, s.Class.ShieldLayers);
    }

    /// <summary>Lights the bands from the ship's shields, in the console skin's palette.</summary>
    public void Show(Ship s, Palette c)
    {
        for (int i = 0; i < 6; i++)
        {
            int n = Layers(s, i);
            Color col = s.ShieldsUp ? Ui.SystemsDisplay.ShieldColorOf(c, s.Shields[i] / s.Class.ShieldsHp[i]) : c.Deep;
            float flash = (float)Math.Clamp(s.Flash[i], 0, 1);
            for (int l = 0; l < 3; l++)
                _mats[i, l].AlbedoColor = new Color(col, Math.Min(1f, (l < n ? 0.8f : 0f) + (l == 0 ? flash * 0.9f : 0f)));
        }
    }
}
