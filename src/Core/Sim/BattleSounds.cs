// BattleSounds: what the battle has done since the last look, as sounds (owner, 2026-11-10: "Implement sound fx in the
// game"; openspec/changes/sound-effects design section 3). It reads only the battle's own record of what happened, the
// same the view draws from (CLAUDE.md 6.1): the shots as they appear (Battle.Shots), the hits that landed (Battle.Hits,
// by their sequence), the lightning strikes (Battle.Lightning), the commanded ship's lock and hull. Which event plays
// which cue is data (SoundMap). A placed sound carries where it happened on the battle plane (metres) and the ship it
// happened to; an unplaced one is the commanded ship's own (its console). The first look sounds nothing, so a battle
// opened mid-fight does not play every shot in the air at once. Plain C#, no Godot: the view plays what it returns
// (BattleAudio), a test reads it.
using System;
using System.Collections.Generic;
using FleetCommand.Data;

namespace FleetCommand.Sim;

/// <summary>One sound to play: its cue, and for a placed sound where (metres on the battle plane) and on which ship; an
/// unplaced sound has no place (NaN) and no ship.</summary>
public sealed record SoundEvent(string Cue, double XM, double ZM, string? ShipId)
{
    /// <summary>True when the sound is heard where it happened (placed in 3D), false when it is the console's.</summary>
    public bool Placed => double.IsFinite(XM);
}

public sealed class BattleSounds
{
    private readonly SoundMap _map;
    private readonly HashSet<Shot> _shots = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<LightningHit> _strikes = new();
    private readonly Dictionary<string, double> _lastT = new();
    private long _seq = long.MinValue;
    private string? _target;
    private bool _critical, _primed;

    public BattleSounds(SoundMap map) { _map = map; }

    /// <summary>The sounds of everything the battle has done since the last call (the first call only takes note).</summary>
    public List<SoundEvent> Poll(Battle b)
    {
        var heard = new List<SoundEvent>();
        void Add(string cue, double x, double z, string? ship)
        {
            if (cue == "" || (_lastT.TryGetValue(cue, out double t) && b.T - t < _map.RepeatS)) return;
            _lastT[cue] = b.T;
            heard.Add(new SoundEvent(cue, x, z, ship));
        }
        // shots as they leave their mounts
        var live = new HashSet<Shot>(b.Shots, ReferenceEqualityComparer.Instance);
        foreach (Shot s in b.Shots)
        {
            if (!_shots.Add(s) || !_primed) continue;
            string? cue = s.Intercept ? _map.Intercept : _map.Shots.GetValueOrDefault(s.Kind);
            if (cue != null) Add(cue, s.SX, s.SZ, s.From);
        }
        _shots.IntersectWith(live);
        // hits that landed, each once, by its sequence
        foreach (HitEvent h in b.Hits)
        {
            if (h.Seq <= _seq) continue;
            _seq = h.Seq;
            if (!_primed) continue;
            Ship? on = b.ShipById(h.ShipId);
            double x = on?.X ?? h.FromX, z = on?.Z ?? h.FromZ;
            string? cue = h.Destroyed ? _map.Destroyed
                : h.HullDamage > 0 ? (h.Kind is "torpedo" or "slug" ? _map.TorpedoHit : _map.HullHit)
                : h.Absorbed > 0 ? _map.ShieldHit : null;
            if (cue != null) Add(cue, x, z, h.ShipId);
        }
        // lightning strikes
        foreach (LightningHit l in b.Lightning)
            if (_strikes.Add(l) && _primed) Add(_map.Lightning, l.XM, l.ZM, l.ShipId);
        _strikes.IntersectWith(b.Lightning);
        // the commanded ship's own: a new lock, the hull falling below critical
        Ship me = b.Player;
        string? target = b.TargetOf(me)?.Id;
        if (target != null && target != _target && _primed) Add(_map.Lock, double.NaN, double.NaN, null);
        _target = target;
        bool critical = me.Alive && me.Hull < me.Class.HullHp * _map.HullCriticalFrac;
        if (critical && !_critical && _primed) Add(_map.HullCritical, double.NaN, double.NaN, null);
        _critical = critical;
        _primed = true;
        return heard;
    }
}
