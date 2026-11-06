// MusicPlayer: the score under the game, on the Music bus (AudioSettings): the main theme on the menus and the map, the
// battle cue in a battle, each looped and crossfaded over a second when the screen changes. The cues are the committed
// renders in assets/audio/music (docs/design/music.md); which plays where is this class's one job.
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class MusicPlayer : Node
{
    private const float FadeS = 1.2f;
    private readonly AudioStreamPlayer[] _deck = new AudioStreamPlayer[2];
    private int _on;
    private string? _cue;

    public override void _Ready()
    {
        Name = "Music";
        for (int i = 0; i < 2; i++) { _deck[i] = new AudioStreamPlayer { Bus = "Music", VolumeDb = -80 }; AddChild(_deck[i]); }
    }

    /// <summary>Plays a cue (a file name in assets/audio/music, without .ogg) from its start, looped, fading the last one out;
    /// the same cue again changes nothing.</summary>
    public void Play(string cue)
    {
        if (cue == _cue) return;
        _cue = cue;
        if (GD.Load<AudioStream>($"res://assets/audio/music/{cue}.ogg") is not AudioStreamOggVorbis s) return;
        s.Loop = true;
        AudioStreamPlayer old = _deck[_on];
        _on = 1 - _on;
        AudioStreamPlayer now = _deck[_on];
        now.Stream = s;
        now.VolumeDb = -40;
        now.Play();
        Tween tw = CreateTween().SetParallel();
        tw.TweenProperty(now, "volume_db", 0f, FadeS);
        tw.TweenProperty(old, "volume_db", -80f, FadeS);
        tw.Chain().TweenCallback(Callable.From(old.Stop));
    }
}
