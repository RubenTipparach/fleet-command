// BattleView.Sounds: the battle heard (openspec/changes/sound-effects, design section 3; owner, 2026-11-11: "ok implment
// it"). Each frame, a shot that was not there the frame before has just left its mount, and a HitEvent past the last one
// heard has just landed: the same events the view draws its effects from, so a sound never disagrees with what is shown
// (CLAUDE.md section 6.1). BattleSoundRules picks the cue, data/audio/cues.json gives its file and level, and it plays from
// where it happens in 3D (CLAUDE.md section 11), heard from the camera: full within unit_size_m, quieter and duller
// beyond, silent past max_distance_m. A pool of players, so at most `voices` sounds at once; a new sound takes the voice
// that started longest ago.
using System.Collections.Generic;
using FleetCommand.Audio;
using FleetCommand.Data;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

public sealed partial class BattleView
{
    private AudioStreamPlayer3D[] _voices = System.Array.Empty<AudioStreamPlayer3D>();
    private int _nextVoice;
    private BattleSoundRules _soundRules = null!;
    private HashSet<Shot> _heardShots = new();
    private long _heardHitSeq;
    private readonly Dictionary<string, double> _lastHitT = new();
    private readonly Dictionary<string, AudioStream?> _soundStreams = new();

    // Makes the voices once, from the battle sound table.
    private void InitSounds()
    {
        BattleSoundsDef def = _x.Data.BattleSounds;
        _soundRules = new BattleSoundRules(def);
        _voices = new AudioStreamPlayer3D[def.Voices];
        for (int i = 0; i < def.Voices; i++)
        {
            _voices[i] = new AudioStreamPlayer3D
            {
                Name = $"Voice{i}", Bus = "Effects", UnitSize = (float)(def.UnitSizeM / MPerU), MaxDistance = (float)(def.MaxDistanceM / MPerU),
                AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance,
            };
            AddChild(_voices[i]);
        }
    }

    // A new battle (or a restart): what is already there has been heard.
    private void ResetSounds(Battle b)
    {
        _heardShots = new HashSet<Shot>(b.Shots);
        _heardHitSeq = b.Hits.Count > 0 ? b.Hits[^1].Seq : 0;
        _lastHitT.Clear();
        foreach (AudioStreamPlayer3D v in _voices) v.Stop();
    }

    // Plays every shot that has left its mount and every hit that has landed since the last frame.
    private void SyncSounds(Battle b)
    {
        if (_voices.Length == 0) return;
        foreach (Shot sh in b.Shots)
            if (!_heardShots.Contains(sh) && _soundRules.FireCue(sh) is { } cue)
                PlayAt(cue, new Vector3((float)(sh.SX / MPerU), LiftOf(sh.From), (float)(sh.SZ / MPerU)));
        _heardShots = new HashSet<Shot>(b.Shots);
        foreach (HitEvent e in b.Hits)
        {
            if (e.Seq <= _heardHitSeq) continue;
            _heardHitSeq = e.Seq;
            double? last = _lastHitT.TryGetValue(e.ShipId, out double t) ? t : null;
            _lastHitT[e.ShipId] = e.T;
            Vector3 at = _models.TryGetValue(e.ShipId, out Node3D? m) ? m.GlobalPosition
                : b.ShipById(e.ShipId) is { } s ? new Vector3((float)(s.X / MPerU), LiftOf(s.Id), (float)(s.Z / MPerU)) : Vector3.Zero;
            PlayAt(_soundRules.HitCue(e, last), at);
        }
    }

    private void PlayAt(string cueId, Vector3 at)
    {
        AudioCue cue = _x.Data.Audio.Get(cueId);
        if (!_soundStreams.TryGetValue(cue.File, out AudioStream? stream)) _soundStreams[cue.File] = stream = GD.Load<AudioStream>("res://" + cue.File);
        if (stream == null) return;
        AudioStreamPlayer3D v = _voices[_nextVoice];
        _nextVoice = (_nextVoice + 1) % _voices.Length;
        v.Stream = stream;
        v.Bus = cue.Bus;
        v.VolumeDb = (float)cue.VolumeDb;
        v.GlobalPosition = at;
        v.Play();
        if (_x.Sounds.Log) GD.Print($"PLAYTEST_SOUND {cueId}");
    }
}
