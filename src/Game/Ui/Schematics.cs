// Schematics: a ship's top-down schematic drawn in a skin's colours (owner, 2026-11-05: "generate schematics for each ship,
// top down view ... use 2 differnt layers both white so we can recombine them for each ui skin. I want these to be used in
// game, and in store"). The pictures are made by tools/mockups/ship_schematics.py from the ships' own glb files: a hull
// layer and a wire layer, each white with the drawing in its alpha (assets/ships/schematics/README.md). This class tints
// the hull layer with a dark colour and the wire with a bright one and draws them, nose up, so one picture serves every
// skin: the systems display, the Target box, the shipyard and the outfitter all call Draw.
using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace FleetCommand.Game.Ui;

public static class Schematics
{
    private sealed record Entry(Texture2D Hull, Texture2D Wire, float LengthPx, float BeamPx, Vector2 CanvasPx, Vector2 OriginPx);

    private static Dictionary<string, Entry>? _all;
    private const string Dir = "res://assets/ships/schematics/";

    private static Dictionary<string, Entry> All
    {
        get
        {
            if (_all != null) return _all;
            _all = new Dictionary<string, Entry>();
            using var f = FileAccess.Open(Dir + "manifest.json", FileAccess.ModeFlags.Read);
            if (f == null) { GD.PushError("SCHEMATICS no assets/ships/schematics/manifest.json"); return _all; }
            using JsonDocument doc = JsonDocument.Parse(f.GetAsText());
            foreach (JsonElement s in doc.RootElement.GetProperty("ships").EnumerateArray())
            {
                string key = s.GetProperty("faction").GetString() + "/" + s.GetProperty("id").GetString();
                float mpp = s.GetProperty("m_per_px").GetSingle();
                JsonElement size = s.GetProperty("size_px"), origin = s.GetProperty("origin_px");
                _all[key] = new Entry(GD.Load<Texture2D>(Dir + s.GetProperty("hull").GetString()), GD.Load<Texture2D>(Dir + s.GetProperty("wire").GetString()),
                    s.GetProperty("extent_m").GetProperty("length_z").GetSingle() / mpp, s.GetProperty("extent_m").GetProperty("beam_x").GetSingle() / mpp, new Vector2(size[0].GetSingle(), size[1].GetSingle()),
                    new Vector2(origin[0].GetSingle(), origin[1].GetSingle()));
            }
            return _all;
        }
    }

    /// <summary>The schematic's key (faction/id) for a ship class's model path (assets/ships/concord/albatross.glb), or null.</summary>
    public static string? KeyOfModel(string? model)
    {
        if (model == null || !model.EndsWith(".glb")) return null;
        string[] parts = model[..^4].Split('/');
        return parts.Length >= 2 ? parts[^2] + "/" + parts[^1] : null;
    }

    public static bool Has(string? key) => key != null && All.ContainsKey(key);

    /// <summary>Draws the schematic nose up with its hull's length 2 x <paramref name="halfLength"/>, the hull centred at
    /// <paramref name="centre"/>. False when there is none (a stand-in hull); the caller draws its stand-in.</summary>
    public static bool Draw(CanvasItem ci, string? key, Vector2 centre, float halfLength, Color hull, Color wire, float alpha = 1)
    {
        if (key == null || !All.TryGetValue(key, out Entry? e)) return false;
        float k = 2 * halfLength / e.LengthPx;
        Vector2 size = e.CanvasPx * k, at = centre - size / 2;
        var r = new Rect2(at, size);
        ci.DrawTextureRect(e.Hull, r, false, new Color(hull, hull.A * alpha));
        ci.DrawTextureRect(e.Wire, r, false, new Color(wire, wire.A * alpha));
        return true;
    }

    /// <summary>The rectangle the hull itself covers when drawn by Draw at this centre and half-length (its beam across, its
    /// length down), or null without a schematic; the shop places hardpoints in it.</summary>
    public static Rect2? HullRect(string? key, Vector2 centre, float halfLength)
    {
        if (key == null || !All.TryGetValue(key, out Entry? e)) return null;
        float beam = 2 * halfLength * e.BeamPx / e.LengthPx;
        return new Rect2(centre.X - beam / 2, centre.Y - halfLength, beam, 2 * halfLength);
    }

    /// <summary>The half-length that fits a schematic inside a box (DrawFit's).</summary>
    public static float FitHalf(string? key, Rect2 box)
    {
        if (key == null || !All.TryGetValue(key, out Entry? e)) return box.Size.Y * 0.46f;
        return Math.Min(box.Size.Y * 0.46f, box.Size.X * 0.46f * e.LengthPx / Math.Max(1, e.BeamPx));
    }

    /// <summary>Draws the schematic fitted inside a box (the shipyard's preview), nose up.</summary>
    public static bool DrawFit(CanvasItem ci, string? key, Rect2 box, Color hull, Color wire)
    {
        if (key == null || !All.TryGetValue(key, out Entry? e)) return false;
        // the ship fills the box's height, or its width when the hull is the wider for the box
        return Draw(ci, key, box.GetCenter(), FitHalf(key, box), hull, wire);
    }
}
