// SoundMap: which sound each thing the battle does plays, read from data/audio/events.json and checked against the cue
// table (data/audio/cues.json) as it loads, so a battle can never name a sound that is not there (CLAUDE.md section 6.4;
// openspec/changes/sound-effects design section 3). Plain records, no Godot: BattleSounds reads it.
using System;
using System.Collections.Generic;

namespace FleetCommand.Data;

/// <summary>The battle's sounds: a cue for each kind of shot as it is fired (a kind not listed is silent), point defence's
/// beam, a hit on the shields, on the hull and by a torpedo, a ship destroyed, a lightning strike; the commanded ship's
/// lock, its hull falling below <see cref="HullCriticalFrac"/>, the pause key; and how soon a cue may play again.</summary>
public sealed record SoundMap(IReadOnlyDictionary<string, string> Shots, string Intercept, string ShieldHit, string HullHit, string TorpedoHit,
    string Destroyed, string Lightning, string Lock, string HullCritical, double HullCriticalFrac, string PauseOn, string PauseOff, double RepeatS)
{
    /// <summary>No sounds (the default before the file is read).</summary>
    public static readonly SoundMap Empty = new(new Dictionary<string, string>(), "", "", "", "", "", "", "", "", 0.25, "", "", 0.1);

    /// <summary>Reads data/audio/events.json: every cue named must be in <paramref name="cues"/>; unknown keys are errors.</summary>
    public static SoundMap Load(StrictJson root, AudioCues cues)
    {
        root.Str("_doc", "");
        string Cue(StrictJson o, string key)
        {
            string id = o.Str(key);
            if (!cues.Cues.ContainsKey(id)) throw new DataError($"{o.Where}.{key}: no cue '{id}' in data/audio/cues.json");
            return id;
        }
        StrictJson shotsJ = root.Obj("shots");
        var shots = new Dictionary<string, string>();
        foreach (string kind in shotsJ.Keys()) shots[kind] = Cue(shotsJ, kind);
        shotsJ.Done();
        double frac = root.Num("hull_critical_frac"), repeat = root.Num("repeat_s");
        if (!(frac > 0 && frac < 1)) throw new DataError($"{root.Where}.hull_critical_frac: above 0 and below 1");
        if (!(repeat >= 0 && repeat <= 2)) throw new DataError($"{root.Where}.repeat_s: from 0 to 2 s");
        var map = new SoundMap(shots, Cue(root, "intercept"), Cue(root, "shield_hit"), Cue(root, "hull_hit"), Cue(root, "torpedo_hit"), Cue(root, "destroyed"),
            Cue(root, "lightning"), Cue(root, "lock"), Cue(root, "hull_critical"), frac, Cue(root, "pause_on"), Cue(root, "pause_off"), repeat);
        root.Done();
        return map;
    }
}
