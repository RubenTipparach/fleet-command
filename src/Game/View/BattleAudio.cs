// BattleAudio: the battle's sounds, played (owner, 2026-11-10: "Implement sound fx in the game"; openspec/changes/
// sound-effects design section 3). BattleSounds (src/Core) says what the battle did since the last frame as cues; this
// node plays them: a placed sound (a shot, a hit, a ship destroyed, a lightning strike) from a 3D player at its place in
// the battle, heard from the camera and quieter with distance; the commanded ship's own (a lock, its hull critical)
// through the console (ConsoleSounds), unplaced. A fixed pool of placed players (a voice cap for phones, survey T3): a
// new sound past the cap takes the voice that has played longest. Each sound's pitch is moved a little at random, so a
// volley does not ring as one note. Every file, bus and level is the cue table's (AudioCues). The view only.
using System;
using System.Collections.Generic;
using FleetCommand.Data;
using FleetCommand.Game.Ui;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class BattleAudio : Node3D
{
    /// <summary>The most placed sounds at once.</summary>
    public const int Voices = 24;
    /// <summary>The distance (scene units, km) within which a placed sound is at its full level; it halves each time the
    /// distance doubles past it: the chase camera, about 14 units off, hears its own ship's fire whole.</summary>
    public const float FullWithinU = 20f;
    /// <summary>How far each sound's pitch may move from its own, either way.</summary>
    public const float PitchJitter = 0.06f;

    private readonly AudioStreamPlayer3D[] _voices = new AudioStreamPlayer3D[Voices];
    private readonly ulong[] _started = new ulong[Voices];
    private readonly Random _rng = new(7);
    private AudioCues _cues = AudioCues.Empty;
    private ConsoleSounds? _console;
    private BattleSounds? _reader;
    private Battle? _for;

    /// <summary>Gives the node its cue table, the console's sounds for the unplaced ones, and the map of events to cues.</summary>
    public void Init(AudioCues cues, ConsoleSounds console) { _cues = cues; _console = console; }

    public override void _Ready()
    {
        Name = "BattleAudio";
        for (int i = 0; i < Voices; i++)
        {
            _voices[i] = new AudioStreamPlayer3D { UnitSize = FullWithinU, MaxDb = 3, AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance };
            AddChild(_voices[i]);
        }
    }

    /// <summary>Plays the sounds of everything the battle did since the last frame; a new battle starts a new reader (which
    /// sounds nothing on its first look).</summary>
    public void Sync(Battle b, float mPerU, Func<string, float> liftOf)
    {
        if (b != _for) { _for = b; _reader = new BattleSounds(b.Data.Sounds); }
        foreach (SoundEvent e in _reader!.Poll(b))
        {
            if (!e.Placed) { _console?.Key(e.Cue); continue; }
            AudioCue cue = _cues.Get(e.Cue);
            if (GD.Load<AudioStream>("res://" + cue.File) is not { } stream) continue;
            AudioStreamPlayer3D v = Voice();
            v.Stream = stream;
            v.Bus = cue.Bus;
            v.VolumeDb = (float)cue.VolumeDb;
            v.PitchScale = 1 + (float)(_rng.NextDouble() * 2 - 1) * PitchJitter;
            v.Position = new Vector3((float)(e.XM / mPerU), e.ShipId != null ? liftOf(e.ShipId) : 0, (float)(e.ZM / mPerU));
            v.Play();
            if (_console is { Log: true }) GD.Print($"PLAYTEST_SOUND {e.Cue} (placed)");
        }
    }

    // a free voice, or the one that has played longest
    private AudioStreamPlayer3D Voice()
    {
        int best = 0;
        for (int i = 0; i < Voices; i++)
        {
            if (!_voices[i].Playing) { best = i; break; }
            if (_started[i] < _started[best]) best = i;
        }
        _started[best] = Time.GetTicksMsec();
        return _voices[best];
    }
}
