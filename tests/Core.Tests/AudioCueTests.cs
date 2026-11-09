// AudioCueTests: the sound table (data/audio/cues.json) the game's players read: every cue names a committed file under
// assets/audio, a bus the game makes and a level in range, and a bad entry stops the load naming its place.
using System.IO;
using FleetCommand.Data;
using Xunit;

namespace FleetCommand.Tests;

public class AudioCueTests
{
    private static string Repo => Path.GetDirectoryName(TestData.DataDir)!;

    [Fact]
    public void Every_cue_in_the_table_names_a_file_that_is_in_the_repository()
    {
        Assert.NotEmpty(TestData.Data.Audio.Cues);
        foreach (AudioCue c in TestData.Data.Audio.Cues.Values)
            Assert.True(File.Exists(Path.Combine(Repo, c.File)), $"{c.Id}: {c.File} is missing");
    }

    [Fact]
    public void The_console_keys_and_the_bridge_bed_have_their_cues()
    {
        Assert.Equal("assets/audio/sfx/ui_chirp.ogg", TestData.Data.Audio.Get("ui_chirp").File);
        Assert.Equal("assets/audio/sfx/ui_clunk.ogg", TestData.Data.Audio.Get("ui_clunk").File);
        Assert.Equal("assets/audio/ambience/amb_bridge.ogg", TestData.Data.Audio.Get("amb_bridge").File);
        Assert.All(TestData.Data.Audio.Cues.Values, c => Assert.Equal("Effects", c.Bus));
    }

    [Fact]
    public void A_cue_the_table_lacks_is_refused_by_name()
    {
        var e = Assert.Throws<DataError>(() => TestData.Data.Audio.Get("ui_kazoo"));
        Assert.Contains("ui_kazoo", e.Message);
    }

    [Fact]
    public void A_bus_the_game_does_not_make_is_refused()
    {
        var e = Assert.Throws<DataError>(() => AudioCues.Load(StrictJson.Parse(
            "{\"k\": {\"file\": \"assets/audio/sfx/k.ogg\", \"bus\": \"Radio\"}}", "data/audio/cues.json")));
        Assert.Contains("k.bus", e.Message);
    }

    [Fact]
    public void A_file_outside_assets_audio_or_not_ogg_is_refused()
    {
        Assert.Throws<DataError>(() => AudioCues.Load(StrictJson.Parse("{\"k\": {\"file\": \"../secrets.ogg\"}}", "t")));
        Assert.Throws<DataError>(() => AudioCues.Load(StrictJson.Parse("{\"k\": {\"file\": \"assets/audio/sfx/k.wav\"}}", "t")));
    }

    [Fact]
    public void A_level_out_of_range_and_an_unknown_key_are_refused()
    {
        Assert.Throws<DataError>(() => AudioCues.Load(StrictJson.Parse("{\"k\": {\"file\": \"assets/audio/sfx/k.ogg\", \"volume_db\": 20}}", "t")));
        Assert.Throws<DataError>(() => AudioCues.Load(StrictJson.Parse("{\"k\": {\"file\": \"assets/audio/sfx/k.ogg\", \"loud\": true}}", "t")));
    }

    [Fact]
    public void A_missing_bus_and_level_take_the_effects_bus_at_zero_decibels()
    {
        AudioCue c = AudioCues.Load(StrictJson.Parse("{\"k\": {\"file\": \"assets/audio/sfx/k.ogg\"}}", "t")).Get("k");
        Assert.Equal(("Effects", 0.0), (c.Bus, c.VolumeDb));
    }
}
