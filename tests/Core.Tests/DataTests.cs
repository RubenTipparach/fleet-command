using FleetCommand.Data;
using Xunit;

namespace FleetCommand.Tests;

public class DataTests
{
    [Fact]
    public void The_real_data_files_load_and_validate()
    {
        GameData d = TestData.Data;
        Assert.Equal(6, TestData.Raiders.Ships.Count);
        Assert.Equal("resolute", TestData.Raiders.Ships[0].Id);
        Assert.Equal(11, d.Classes["ca"].Mounts.Count);   // nine guns, the chorus lance and a missile rack (blue grade)
        Assert.Equal("chorus", d.Classes["ca"].Mounts[6].Weapon);
        Assert.Equal("blue", d.Classes["ca"].Mounts[10].Grade);
        Assert.Equal(140 * d.Tuning.DemoToughX, d.Classes["ca"].HullHp);
    }

    [Fact]
    public void An_unknown_key_is_an_error_that_names_the_file_and_field()
    {
        var e = Assert.Throws<DataError>(() => DataLoader.LoadWeapons(StrictJson.Parse(
            "{\"lance\": {\"name\": \"Lance\", \"kind\": \"beam\", \"range_km\": 12, \"charge_s\": 3.5, \"cost_e\": 2, \"damage\": 7, \"color\": \"#fff\", \"rnage_km\": 3}}", "data/battle/weapons.json")));
        Assert.Contains("data/battle/weapons.json.lance.rnage_km", e.Message);
        Assert.Contains("unknown key", e.Message);
    }

    [Fact]
    public void A_missing_required_field_is_an_error()
    {
        var e = Assert.Throws<DataError>(() => DataLoader.LoadWeapons(StrictJson.Parse(
            "{\"lance\": {\"name\": \"Lance\", \"kind\": \"beam\", \"charge_s\": 3.5, \"cost_e\": 2, \"damage\": 7, \"color\": \"#fff\"}}", "w.json")));
        Assert.Contains("w.json.lance.range_km: missing", e.Message);
    }

    [Fact]
    public void A_number_too_large_to_be_finite_is_refused()
    {
        var e = Assert.Throws<DataError>(() => StrictJson.Parse("{\"x\": 1e400}", "f.json").Num("x"));
        Assert.Contains("f.json.x", e.Message);
    }

    [Fact]
    public void A_missing_optional_field_takes_the_default_and_a_present_zero_is_zero()
    {
        StrictJson o = StrictJson.Parse("{\"a\": 0}", "f.json");
        Assert.Equal(0, o.Num("a", 5));
        Assert.Equal(5, o.Num("b", 5));
        o.Done();
    }

    [Fact]
    public void A_mount_naming_an_unknown_weapon_is_refused()
    {
        string classes = TestData.Read("battle/classes.json").Replace("\"weapon\": \"lance\"", "\"weapon\": \"lancer\"");
        var e = Assert.Throws<DataError>(() => DataLoader.LoadClasses(StrictJson.Parse(classes, "data/battle/classes.json"), TestData.Data.Weapons, TestData.Data.Tuning, TestData.Data.Missiles));
        Assert.Contains("no weapon 'lancer'", e.Message);
    }
}
