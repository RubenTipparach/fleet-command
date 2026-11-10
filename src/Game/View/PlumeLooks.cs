// PlumeLooks: how each look of nebula plume is drawn (data/space/plumes.json; owner, 2026-11-10: "something like a 3d noise
// texture would be cool? something like how Ace Combat 7 does clouds", "some nebulas are ion storms, which will have cool
// lightning"). One look per NebulaDef.Looks, read and validated once, as PlanetLooks reads the planets' (the same range and
// colour checks), so every plume of a look is alike and the numbers live in data, not in the view (CLAUDE.md 6.4). A missing
// look, an unknown key or a bad colour stops the view naming the file and field. The view only: the simulation never reads it.
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;
using Godot;

namespace FleetCommand.Game.View;

/// <summary>One look of plume (a row of data/space/plumes.json): the cloud's colour in the sun and in its own shadow, the
/// glow of its thin edges, how tall the column stands (in its radii) and how far its head spreads, how fast it swallows
/// light, the size of its billows and wisps, how fast they rise, how far from the camera it is back to its full
/// thickness, and an ion storm's lightning.</summary>
public sealed record PlumeLook(Color Lit, Color Deep, Color Glow, float GlowStrength, float HeightR, float Anvil, float Absorption,
    float BaseScale, float DetailScale, float Erosion, float DriftPerS, float ClearKm, Color Flash, float FlashesPerMin);

public static class PlumeLooks
{
    private const string File = "data/space/plumes.json";
    private static Dictionary<string, PlumeLook>? _looks;

    /// <summary>A look by name (an unknown look is an error naming it).</summary>
    public static PlumeLook For(string look) =>
        All().TryGetValue(look, out PlumeLook? l) ? l : throw new DataError($"{File}: no plume look '{look}'");

    /// <summary>Every look, read from the file once.</summary>
    public static IReadOnlyDictionary<string, PlumeLook> All()
    {
        if (_looks != null) return _looks;
        string text = FileAccess.GetFileAsString("res://" + File);
        if (string.IsNullOrEmpty(text)) throw new DataError($"{File}: cannot be opened");
        StrictJson root = StrictJson.Parse(text, File);
        var looks = new Dictionary<string, PlumeLook>();
        foreach (string look in root.Keys())
        {
            if (!NebulaDef.Looks.Contains(look)) throw new DataError($"{File}.{look}: not a plume look (there are: {string.Join(", ", NebulaDef.Looks)})");
            StrictJson o = root.Obj(look);
            looks[look] = new PlumeLook(PlanetLooks.Col(o, "lit"), PlanetLooks.Col(o, "deep"), PlanetLooks.Col(o, "glow"),
                PlanetLooks.Range(o, "glow_strength", 0, 2), PlanetLooks.Range(o, "height_r", 0.5, 4), PlanetLooks.Range(o, "anvil", 0, 0.8),
                PlanetLooks.Range(o, "absorption", 0.5, 16), PlanetLooks.Range(o, "base_scale", 0.3, 4), PlanetLooks.Range(o, "detail_scale", 1, 12),
                PlanetLooks.Range(o, "erosion", 0, 1), PlanetLooks.Range(o, "drift_per_s", 0, 0.2), PlanetLooks.Range(o, "clear_km", 0, 200), PlanetLooks.Col(o, "flash"),
                PlanetLooks.Range(o, "flashes_per_min", 0, 120));
            o.Done();
        }
        root.Done();
        foreach (string k in NebulaDef.Looks)
            if (!looks.ContainsKey(k)) throw new DataError($"{File}: no look for plume look '{k}'");
        return _looks = looks;
    }
}
