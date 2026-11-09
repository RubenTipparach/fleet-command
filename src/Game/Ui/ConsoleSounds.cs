// ConsoleSounds: the console's own sounds, unplaced (CLAUDE.md section 11): a console key pressed plays its skin's key cue
// (SkinStyle.KeySound; owner, 2026-11-07: "Console keys - this should be implemented in the game"), and the bridge bed
// (amb_bridge; owner, 2026-11-07: "use amb_bridge_brown", "put in game now") loops under a battle. Every file, bus and
// level comes from data/audio/cues.json (AudioCues); this class only plays them. Four key players in turn, so fast
// presses overlap instead of cutting each other off.
using FleetCommand.Data;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class ConsoleSounds : Node
{
    private const int Voices = 4;
    private const float BedFadeS = 1.5f;
    private readonly AudioStreamPlayer[] _keys = new AudioStreamPlayer[Voices];
    private AudioStreamPlayer _bed = null!;
    private AudioCues _cues = AudioCues.Empty;
    private int _next;
    private string? _bedCue;

    /// <summary>In a scripted run, each sound played is printed as PLAYTEST_SOUND <cue>, so a playtest can check what it
    /// heard (CLAUDE.md section 9; openspec/changes/sound-effects task 3.7).</summary>
    public bool Log { get; set; }

    /// <summary>Gives the sounds their cue table; call once, before the node enters the tree.</summary>
    public void Init(AudioCues cues) => _cues = cues;

    public override void _Ready()
    {
        Name = "ConsoleSounds";
        for (int i = 0; i < Voices; i++) { _keys[i] = new AudioStreamPlayer(); AddChild(_keys[i]); }
        _bed = new AudioStreamPlayer { VolumeDb = -80 };
        AddChild(_bed);
    }

    /// <summary>Plays a key's sound: the cue's file on its bus at its level, on the next free voice.</summary>
    public void Key(string cueId)
    {
        AudioCue cue = _cues.Get(cueId);
        if (Load(cue) is not { } stream) return;
        AudioStreamPlayer p = _keys[_next];
        _next = (_next + 1) % Voices;
        p.Stream = stream;
        p.Bus = cue.Bus;
        p.VolumeDb = (float)cue.VolumeDb;
        p.Play();
        if (Log) GD.Print($"PLAYTEST_SOUND {cueId}");
    }

    /// <summary>Loops a bed (an ambience cue) under the screen, fading it in; null fades the bed out. The same cue again
    /// changes nothing.</summary>
    public void Bed(string? cueId)
    {
        if (cueId == _bedCue) return;
        _bedCue = cueId;
        Tween tw = CreateTween();
        if (cueId == null)
        {
            tw.TweenProperty(_bed, "volume_db", -80f, BedFadeS);
            tw.TweenCallback(Callable.From(_bed.Stop));
            return;
        }
        AudioCue cue = _cues.Get(cueId);
        if (Load(cue) is not AudioStreamOggVorbis ogg) return;
        ogg.Loop = true;
        _bed.Stream = ogg;
        _bed.Bus = cue.Bus;
        _bed.VolumeDb = -40;
        _bed.Play();
        tw.TweenProperty(_bed, "volume_db", (float)cue.VolumeDb, BedFadeS);
        if (Log) GD.Print($"PLAYTEST_SOUND {cueId} (bed)");
    }

    private static AudioStream? Load(AudioCue cue) => GD.Load<AudioStream>("res://" + cue.File);
}
