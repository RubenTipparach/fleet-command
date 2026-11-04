// Scenarios (data/battle/scenarios.json): both sides playable, each validated on load, and an AI captain's standing orders
// from its scenario (stance, range, erratic, hold fire, the hull and shields it starts with) applied in the battle. Ship
// models: every class's glb exists and is the approved ship the export manifest lists (assets/ships/manifest.json).
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class ScenarioTests
{
    private static Scenario Load(string json) =>
        DataLoader.LoadScenario(StrictJson.Parse(json, "data/battle/scenarios.json"), TestData.Data.Classes, TestData.Data.Factions);

    private const string Ships =
        "\"ships\": [{\"id\": \"a\", \"name\": \"A\", \"captain\": \"C\", \"class\": \"ff\", \"faction\": \"concord\", \"side\": 0, \"x_m\": 0, \"z_m\": 0, \"heading_deg\": 0, \"speed_m_s\": 0},"
        + " {\"id\": \"b\", \"name\": \"B\", \"captain\": \"C\", \"class\": \"krai_ff\", \"faction\": \"krai\", \"side\": 1, \"x_m\": 0, \"z_m\": -5000, \"heading_deg\": 180, \"speed_m_s\": 0 STAND}]";

    private static string One(string side = "concord", string stand = "") =>
        "{\"id\": \"t\", \"name\": \"T\", \"side\": \"" + side + "\", \"seed\": 1, \"objective\": \"O\", \"say\": [\"Comms\", \"Hello.\"], " + Ships.Replace(" STAND", stand) + "}";

    [Fact]
    public void Both_the_Concord_and_the_Krai_have_scenarios_and_each_starts_with_its_own_ship()
    {
        var sides = TestData.Data.Scenarios.GroupBy(s => s.Side).ToDictionary(g => g.Key, g => g.ToList());
        Assert.True(sides.ContainsKey("concord"));
        Assert.True(sides.ContainsKey("krai"));
        foreach (Scenario s in TestData.Data.Scenarios)
        {
            Assert.Equal(s.Side, s.Ships[0].Faction);
            Assert.Equal(0, s.Ships[0].Side);
            Assert.Contains(s.Ships, o => o.Side == 1);
        }
        Assert.Equal(sides["concord"].Select(s => s.Name), sides["krai"].Select(s => s.Name));
    }

    [Fact]
    public void Every_scenario_is_the_Krai_against_the_Concord_in_approved_ships_with_no_stand_ins()
    {
        // owner, 2026-11-04: "NO PLACE HOLDERS SHOULD BE USED FOR KRAI VS CONCORD"
        foreach (Scenario s in TestData.Data.Scenarios)
            foreach (ShipStart o in s.Ships)
            {
                Assert.True(o.Faction is "concord" or "krai", $"{s.Id}: {o.Id} is of the {o.Faction}");
                Assert.True(TestData.Data.Classes[o.Class].Model != null, $"{s.Id}: {o.Id} ({o.Class}) would be a stand-in");
            }
    }

    [Fact]
    public void Every_scenario_starts_a_battle_and_runs_a_minute_without_error()
    {
        foreach (Scenario s in TestData.Data.Scenarios)
        {
            var b = new Battle(TestData.Data, s);
            for (int i = 0; i < 1800; i++) b.Step();
            Assert.All(b.Ships, o => Assert.True(double.IsFinite(o.X) && double.IsFinite(o.Z) && double.IsFinite(o.Hull)));
        }
    }

    [Fact]
    public void A_side_without_a_console_skin_cannot_be_played()
    {
        var e = Assert.Throws<DataError>(() => Load(One("kethran")));
        Assert.Contains("has no console skin", e.Message);
    }

    [Fact]
    public void The_players_ship_must_belong_to_the_scenarios_side()
    {
        var e = Assert.Throws<DataError>(() => Load(One("krai")));
        Assert.Contains("ships[0].faction", e.Message);
    }

    [Fact]
    public void An_unknown_stance_is_refused_naming_the_field()
    {
        var e = Assert.Throws<DataError>(() => Load(One(stand: ", \"stance\": \"charge\"")));
        Assert.Contains("ships[1].stance", e.Message);
    }

    [Fact]
    public void Standing_orders_reach_the_ships()
    {
        Scenario s = Load(One(stand: ", \"stance\": \"hold\", \"range\": \"close\", \"erratic\": true, \"hold_fire\": true, \"hull_frac\": 0.5, \"shields_frac\": 0"));
        var b = new Battle(TestData.Data, s);
        Ship k = b.ShipById("b")!;
        Assert.Equal(Stance.Hold, k.Stance);
        Assert.Equal(AiRange.Close, k.Range);
        Assert.True(k.Erratic);
        Assert.True(k.HoldFire);
        Assert.Equal(k.Class.HullHp * 0.5, k.Hull, 6);
        Assert.All(k.Shields, v => Assert.Equal(0, v));
        Assert.Equal("Hello.", b.Log[^1].Text);
    }

    [Fact]
    public void A_captain_holding_fire_never_shoots()
    {
        Scenario s = Load(One(stand: ", \"hold_fire\": true, \"z_m\": -3000").Replace("\"z_m\": -5000, ", ""));
        var b = new Battle(TestData.Data, s);
        for (int i = 0; i < 1800; i++) b.Step();
        Assert.DoesNotContain(b.Shots, x => x.From == "b");
        Assert.Equal(b.Player.Class.HullHp, b.Player.Hull, 6);
    }

    [Fact]
    public void Every_class_with_a_model_points_at_an_exported_ship_in_the_manifest()
    {
        string root = Path.GetDirectoryName(TestData.DataDir)!;
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "assets", "ships", "manifest.json")));
        string[] listed = manifest.RootElement.GetProperty("ships").EnumerateArray().Select(e => "assets/ships/" + e.GetProperty("file").GetString()).ToArray();
        ShipClass[] modelled = TestData.Data.Classes.Values.Where(c => c.Model != null).ToArray();
        Assert.NotEmpty(modelled);
        foreach (ShipClass c in modelled)
        {
            Assert.True(File.Exists(Path.Combine(root, c.Model!)), $"{c.Id}: {c.Model} is missing");
            Assert.Contains(c.Model, listed);
        }
    }
}
