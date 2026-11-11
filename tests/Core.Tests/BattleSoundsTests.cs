// The battle's sounds (BattleSounds, data/audio/events.json; owner, 2026-11-10: "Implement sound fx in the game"): which
// cue each thing the battle does plays, and where.
using System.Linq;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class BattleSoundsTests
{
    private static Battle Duel() => CloakTests.Pair("ca", "concord", "krai_ff", "krai", -15000, extraB: ", \"hold_fire\": true");

    [Fact]
    public void The_first_look_is_silent_and_a_new_lock_sounds_on_the_console()
    {
        Battle b = Duel();
        var sounds = new BattleSounds(b.Data.Sounds);
        Assert.Empty(sounds.Poll(b));
        b.Lock("b");
        SoundEvent lockSound = Assert.Single(sounds.Poll(b));
        Assert.Equal(b.Data.Sounds.Lock, lockSound.Cue);
        Assert.False(lockSound.Placed);
        Assert.Empty(sounds.Poll(b));
    }

    [Fact]
    public void A_volley_sounds_where_it_is_fired_and_where_it_lands()
    {
        Battle b = Duel();
        var sounds = new BattleSounds(b.Data.Sounds);
        b.Lock("b");
        sounds.Poll(b);
        foreach (Mount m in b.Player.Mounts) { m.Selected = m.Weapon.Kind == "beam"; m.Charge = 1; }
        Assert.True(b.Fire() > 0);
        var fired = sounds.Poll(b);
        // one sound for the volley, however many lances fired (repeat_s), placed at the ship that fired
        SoundEvent shot = Assert.Single(fired, e => e.Cue == b.Data.Sounds.Shots["beam"]);
        Assert.True(shot.Placed);
        Assert.Equal(b.Player.Id, shot.ShipId);
        // (a lance lands as it fires; the hits are in the battle's record by the next look)
        var heard = fired.Concat(Enumerable.Range(0, 60).SelectMany(_ => { b.Step(); return sounds.Poll(b); })).ToList();
        Assert.True(b.Hits.Count > 0, "the volley landed nothing to hear: " + string.Join(", ", heard.Select(e => e.Cue)));
        Assert.Contains(heard, e => e.ShipId == "b" && (e.Cue == b.Data.Sounds.ShieldHit || e.Cue == b.Data.Sounds.HullHit));
    }
}
