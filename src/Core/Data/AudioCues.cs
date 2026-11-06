// AudioCues: the sounds the game plays, read from data/audio/cues.json and checked (CLAUDE.md section 6.4): per cue its
// file under assets/audio, the bus it plays on and its level. The game's players read a cue here and never write a
// file path or a level themselves (openspec/changes/sound-effects, design section 3). Plain records, no Godot.
using System;
using System.Collections.Generic;

namespace FleetCommand.Data;

/// <summary>One sound: its file (relative to the repository, under assets/audio, an .ogg), its bus (Master, Music or
/// Effects) and its level at that bus in dB.</summary>
public sealed record AudioCue(string Id, string File, string Bus, double VolumeDb);

/// <summary>Every cue by id.</summary>
public sealed record AudioCues(IReadOnlyDictionary<string, AudioCue> Cues)
{
    /// <summary>The buses a cue may name, made by the game's audio settings.</summary>
    public static readonly string[] Buses = { "Master", "Music", "Effects" };

    /// <summary>No cues: the default before the file is read.</summary>
    public static readonly AudioCues Empty = new(new Dictionary<string, AudioCue>());

    /// <summary>A cue by id; throws naming the id when there is none, so a skin naming a missing sound fails loudly.</summary>
    public AudioCue Get(string id) =>
        Cues.TryGetValue(id, out AudioCue? c) ? c : throw new DataError($"data/audio/cues.json: no cue '{id}' ({string.Join(", ", Cues.Keys)})");

    /// <summary>Reads data/audio/cues.json: every key but _doc is a cue; its file is an .ogg under assets/audio, its bus
    /// one of Buses (Effects when absent), its volume_db finite, from -40 to 6 (0 when absent); unknown keys are errors.</summary>
    public static AudioCues Load(StrictJson root)
    {
        root.Str("_doc", "");
        var cues = new Dictionary<string, AudioCue>();
        foreach (string id in root.Keys())
        {
            if (id == "_doc") continue;
            StrictJson c = root.Obj(id);
            string file = c.Str("file");
            if (!file.StartsWith("assets/audio/", StringComparison.Ordinal) || !file.EndsWith(".ogg", StringComparison.Ordinal) || file.Contains(".."))
                throw new DataError($"{c.Where}.file: an .ogg under assets/audio/, not '{file}'");
            string bus = c.Str("bus", "Effects");
            if (Array.IndexOf(Buses, bus) < 0) throw new DataError($"{c.Where}.bus: one of {string.Join(", ", Buses)}, not '{bus}'");
            double db = c.Num("volume_db", 0);
            if (!double.IsFinite(db) || db < -40 || db > 6) throw new DataError($"{c.Where}.volume_db: from -40 to 6 dB");
            c.Done();
            cues[id] = new AudioCue(id, file, bus, db);
        }
        root.Done();
        return new AudioCues(cues);
    }
}
