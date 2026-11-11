// BattleSoundRules: which sound a battle event plays (data/audio/battle.json; openspec/changes/sound-effects, design
// section 3). The view hears the battle through the same events it draws its effects from, a shot leaving its mount and a
// HitEvent landing (CLAUDE.md section 6.1: a sound can never disagree with what is shown), and asks this class which cue
// each plays. Plain C#, no Godot, tested headless.
using System.Collections.Generic;
using FleetCommand.Data;
using FleetCommand.Sim;

namespace FleetCommand.Audio;

/// <summary>The battle's sound table, read from data/audio/battle.json: the cue each shot kind fires with, the cue each
/// kind of hit lands with, each hit's short alternative and when it is used, and the 3D player's limits.</summary>
public sealed record BattleSoundsDef(IReadOnlyDictionary<string, string> Fire, IReadOnlyDictionary<string, string> Hit,
    IReadOnlyDictionary<string, string> Short, double ShortWithinS, int Voices, double UnitSizeM, double MaxDistanceM)
{
    /// <summary>No sounds: the default before the file is read.</summary>
    public static readonly BattleSoundsDef Empty = new(new Dictionary<string, string>(), new Dictionary<string, string>(),
        new Dictionary<string, string>(), 0, 0, 1000, 30000);

    private static readonly string[] HitKinds = { "shield", "hull", "torpedo_hull", "destroyed" };

    /// <summary>Reads data/audio/battle.json; every cue it names must be in <paramref name="cues"/>, every hit kind given,
    /// the numbers finite and in range; unknown keys are errors.</summary>
    public static BattleSoundsDef Load(StrictJson root, AudioCues cues)
    {
        root.Str("_doc", "");
        Dictionary<string, string> Map(string key)
        {
            var m = new Dictionary<string, string>();
            StrictJson o = root.Obj(key);
            foreach (string k in o.Keys())
            {
                string cue = o.Str(k);
                if (!cues.Cues.ContainsKey(cue)) throw new DataError($"{o.Where}.{k}: no cue '{cue}' in data/audio/cues.json");
                m[k] = cue;
            }
            o.Done();
            return m;
        }
        var fire = Map("fire");
        var hit = Map("hit");
        foreach (string k in HitKinds)
            if (!hit.ContainsKey(k)) throw new DataError($"{root.Where}.hit.{k}: missing (every hit plays a sound: {string.Join(", ", HitKinds)})");
        var sht = Map("short");
        double within = root.Num("short_within_s");
        int voices = root.Int("voices");
        double unit = root.Num("unit_size_m"), max = root.Num("max_distance_m");
        if (!double.IsFinite(within) || within < 0 || within > 5) throw new DataError($"{root.Where}.short_within_s: 0 to 5 s");
        if (voices is < 1 or > 64) throw new DataError($"{root.Where}.voices: 1 to 64");
        if (!double.IsFinite(unit) || unit <= 0 || !double.IsFinite(max) || max <= unit)
            throw new DataError($"{root.Where}: unit_size_m above 0 and max_distance_m above it");
        root.Done();
        return new BattleSoundsDef(fire, hit, sht, within, voices, unit, max);
    }
}

/// <summary>Picks the cue for a shot leaving its mount and for a hit landing.</summary>
public sealed class BattleSoundRules
{
    private readonly BattleSoundsDef _def;
    public BattleSoundRules(BattleSoundsDef def) => _def = def;

    /// <summary>The cue a shot fires with: a point defence intercept plays "pd"'s, any other shot its kind's; null for a
    /// kind that makes no firing sound (craft, flares, decoys).</summary>
    public string? FireCue(Shot shot) => _def.Fire.TryGetValue(shot.Intercept ? "pd" : shot.Kind, out string? c) ? c : null;

    /// <summary>The cue a hit lands with: destroyed, a torpedo through to the hull, the hull, or the shields; its short
    /// alternative when <paramref name="lastHitOnShipT"/> (the battle time of the hit before on the same ship) is less
    /// than short_within_s before it. A ship destroyed always plays in full.</summary>
    public string HitCue(HitEvent hit, double? lastHitOnShipT)
    {
        string cue = hit.Destroyed ? _def.Hit["destroyed"]
            : hit.HullDamage > 0 ? _def.Hit[hit.Kind == "torpedo" ? "torpedo_hull" : "hull"]
            : _def.Hit["shield"];
        if (!hit.Destroyed && lastHitOnShipT is double t && hit.T - t < _def.ShortWithinS && _def.Short.TryGetValue(cue, out string? s)) cue = s;
        return cue;
    }
}
