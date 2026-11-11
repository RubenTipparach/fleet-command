// BattleSoundTests: which sound a battle event plays (data/audio/battle.json, BattleSoundRules): the real table loads
// against the real cue table, every shot kind a weapon fires has a sound, a hit plays by what it struck, a quick second
// hit on the same ship plays the short alternative, and a real battle's every shot and hit maps to a cue that exists.
using System.Linq;
using FleetCommand.Audio;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class BattleSoundTests
{
    private static BattleSoundRules Rules => new(TestData.Data.BattleSounds);
    private static HitEvent Hit(double t, string kind = "beam", double absorbed = 5, double hull = 0, bool destroyed = false) =>
        new(1, t, "s1", 0, 0, kind, absorbed, hull, destroyed);
    private static Shot Shot(string kind, bool intercept = false) => new() { Kind = kind, Color = "#fff", Intercept = intercept };

    [Fact]
    public void Every_weapon_kind_fires_with_a_sound_and_point_defence_has_its_own()
    {
        // a point defence mount fires beams flagged as intercepts (Battle's point defence step), every other its kind's shot;
        // the blood charge is a ram, not a shot
        foreach (WeaponDef w in TestData.Data.Weapons.Values.Where(w => w.Kind != "ram"))
            Assert.True(Rules.FireCue(w.IsPointDefence ? Shot("beam", intercept: true) : Shot(w.Kind)) != null, $"{w.Id} ({w.Kind}) fires with no sound");
        Assert.Equal("lance_fire", Rules.FireCue(Shot("beam")));
        Assert.Equal("pd_burst", Rules.FireCue(Shot("beam", intercept: true)));
        Assert.Null(Rules.FireCue(Shot("flare")));
    }

    [Fact]
    public void A_hit_plays_by_what_it_struck()
    {
        Assert.Equal("shield_hit", Rules.HitCue(Hit(10), null));
        Assert.Equal("hull_hit", Rules.HitCue(Hit(10, hull: 3), null));
        Assert.Equal("torpedo_detonate", Rules.HitCue(Hit(10, "torpedo", hull: 3), null));
        Assert.Equal("ship_destroyed", Rules.HitCue(Hit(10, hull: 3, destroyed: true), null));
    }

    [Fact]
    public void A_quick_second_hit_on_the_same_ship_plays_the_short_alternative_but_a_kill_plays_in_full()
    {
        Assert.Equal("shield_hit_short", Rules.HitCue(Hit(10.2), 10.0));
        Assert.Equal("shield_hit", Rules.HitCue(Hit(11.0), 10.0));
        Assert.Equal("ship_destroyed", Rules.HitCue(Hit(10.1, hull: 9, destroyed: true), 10.0));
    }

    [Fact]
    public void A_real_battles_every_shot_and_hit_plays_a_cue_in_the_table()
    {
        var b = new Battle(TestData.Data, TestData.Raiders);
        var seen = new System.Collections.Generic.HashSet<Shot>();
        int fired = 0;
        for (int i = 0; i < 3000 && b.Hits.Count < 40; i++)
        {
            b.Step();
            foreach (Shot sh in b.Shots)
                if (seen.Add(sh) && Rules.FireCue(sh) is { } c) { TestData.Data.Audio.Get(c); fired++; }
        }
        Assert.True(fired > 0, "no shot fired a sound");
        Assert.NotEmpty(b.Hits);
        foreach (HitEvent e in b.Hits) TestData.Data.Audio.Get(Rules.HitCue(e, null));
    }

    [Fact]
    public void A_cue_missing_from_the_cue_table_or_a_missing_hit_kind_stops_the_load()
    {
        string ok = "\"fire\": {}, \"short\": {}, \"short_within_s\": 0.5, \"voices\": 8, \"unit_size_m\": 1000, \"max_distance_m\": 30000";
        var e = Assert.Throws<DataError>(() => BattleSoundsDef.Load(StrictJson.Parse(
            "{\"hit\": {\"shield\": \"shield_kazoo\", \"hull\": \"hull_hit\", \"torpedo_hull\": \"hull_hit\", \"destroyed\": \"ship_destroyed\"}, " + ok + "}", "t"), TestData.Data.Audio));
        Assert.Contains("shield_kazoo", e.Message);
        e = Assert.Throws<DataError>(() => BattleSoundsDef.Load(StrictJson.Parse(
            "{\"hit\": {\"shield\": \"shield_hit\", \"hull\": \"hull_hit\", \"torpedo_hull\": \"hull_hit\"}, " + ok + "}", "t"), TestData.Data.Audio));
        Assert.Contains("destroyed", e.Message);
    }
}
