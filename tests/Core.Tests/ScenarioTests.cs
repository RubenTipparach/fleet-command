// Scenarios (data/battle/scenarios.json): both sides playable, each validated on load, and an AI captain's standing orders
// from its scenario (stance, range, erratic, hold fire, the hull and shields it starts with) applied in the battle. Ship
// models: every class's glb exists and is the approved ship the export manifest lists (assets/ships/manifest.json), and
// the export is not stale: each ship page still has the sha256 the manifest recorded when the ships were exported.
using System;
using System.Collections.Generic;
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
        + " {\"id\": \"b\", \"name\": \"B\", \"captain\": \"C\", \"class\": \"krai_ff\", \"faction\": \"krai\", \"side\": 1, \"x_m\": 0, \"z_m\": -50000, \"heading_deg\": 180, \"speed_m_s\": 0 STAND}]";

    private static string One(string side = "concord", string stand = "") =>
        "{\"id\": \"t\", \"name\": \"T\", \"side\": \"" + side + "\", \"sky\": \"quiet\", \"seed\": 1, \"objective\": \"O\", \"say\": [\"Comms\", \"Hello.\"], " + Ships.Replace(" STAND", stand) + "}";

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
            // a test scene with a goal (a delivery, a ship in distress) may have no enemy at all
            if (s.Goal == null) Assert.Contains(s.Ships, o => o.Side == 1);
        }
        Assert.Equal(sides["concord"].Select(s => s.Name), sides["krai"].Select(s => s.Name));
    }

    [Fact]
    public void Every_scenario_is_fought_by_factions_with_approved_ships_in_those_ships_with_no_stand_ins()
    {
        // owner, 2026-11-04: "NO PLACE HOLDERS SHOULD BE USED FOR KRAI VS CONCORD"; the Ssarith Hunt joined them once its ships
        // were approved for the game (owner, 2026-11-06: "looks good, implement in game")
        // and the Kelvane Institute once its ships were (owner, 2026-11-07: "perfect, implment these ships in game").
        // The one exception is the battle console mockup's allied scenes (openspec/changes/fleet-skins), as the mockup has
        // them: their foes are its Kethran and Vos stand-ins. The ships you can command beside them are approved ones.
        foreach (Scenario s in TestData.Data.Scenarios)
            foreach (ShipStart o in s.Ships)
            {
                bool allied = s.Name is "Allied fleet" or "Four races";
                if (allied && o.Side == 1 && o.Faction is "kethran" or "vos") continue;
                Assert.True(o.Faction is "concord" or "krai" or "ssarith" or "kelvane", $"{s.Id}: {o.Id} is of the {o.Faction}");
                // a station is drawn as the mockup's station (its stationModel: scenes/standins/station.tscn), not a ship's stand-in
                ShipClass c = TestData.Data.Classes[o.Class];
                Assert.True(c.Model != null || c.Shape == "station", $"{s.Id}: {o.Id} ({o.Class}) would be a stand-in");
            }
    }

    [Fact]
    public void The_Ssarith_Hunt_is_played_as_itself_in_its_approved_ships()
    {
        Scenario[] hunt = TestData.Data.Scenarios.Where(s => s.Side == "ssarith").ToArray();
        foreach (string name in new[] { "Battle", "Duel", "Arming", "Allied fleet", "Four races" }) Assert.Contains(hunt, s => s.Name == name);
        Assert.All(hunt, s => Assert.All(s.Ships.Where(o => o.Faction == "ssarith"), o => Assert.NotNull(TestData.Data.Classes[o.Class].Model)));
        Assert.Equal("ssarith", TestData.Data.Factions["ssarith"].Skin);
        Assert.Equal("kelvane", TestData.Data.Factions["kelvane"].Skin);
    }

    [Fact]
    public void The_Kelvane_Institute_is_played_as_itself_and_fought_in_its_approved_ships()
    {
        Scenario[] own = TestData.Data.Scenarios.Where(s => s.Side == "kelvane").ToArray();
        foreach (string name in new[] { "Battle", "Duel", "Arming", "Four races" }) Assert.Contains(own, s => s.Name == name);
        foreach (string side in new[] { "concord", "krai" })
            foreach (string name in new[] { "The Institute", "Institute wrecks" })
                Assert.Contains(TestData.Data.Scenarios, s => s.Side == side && s.Name == name && s.Ships.Any(o => o.Side == 1 && o.Faction == "kelvane"));
        // every class of the line is in the game, each its own approved ship, and each is fought somewhere
        string[] line = { "kelvane_ff", "kelvane_dd", "kelvane_rv", "kelvane_bc", "kelvane_cv" };
        foreach (string c in line)
        {
            Assert.StartsWith("assets/ships/kelvane/", TestData.Data.Classes[c].Model);
            Assert.Contains(TestData.Data.Scenarios, s => s.Ships.Any(o => o.Class == c && o.Faction == "kelvane"));
        }
        Assert.All(TestData.Data.Scenarios, s => Assert.All(s.Ships.Where(o => o.Faction == "kelvane"), o => Assert.Contains(o.Class, line)));
    }

    [Fact]
    public void In_the_allied_scenes_the_fleet_holds_a_ship_of_each_race_with_a_console_to_hop_between()
    {
        foreach (Scenario s in TestData.Data.Scenarios.Where(s => s.Name is "Allied fleet" or "Four races"))
        {
            string[] own = s.Ships.Where(o => o.Side == 0).Select(o => o.Faction).ToArray();
            Assert.Equal(own.Length, own.Distinct().Count());
            Assert.Superset(new HashSet<string> { "concord", "krai", "ssarith" }, own.ToHashSet());
            Assert.All(own, f => Assert.NotNull(TestData.Data.Factions[f].Skin));
            if (s.Name == "Four races") Assert.Contains("kelvane", own);
            else Assert.DoesNotContain("kelvane", own);
            var b = new Battle(TestData.Data, s);
            foreach (Ship o in b.Fleet()) { b.TakeCommand(o.Id); Assert.Same(o, b.Player); }
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
        Scenario s = Load(One(stand: ", \"hold_fire\": true, \"z_m\": -30000").Replace("\"z_m\": -50000, ", ""));
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

    [Fact]
    public void The_ship_export_is_not_stale_every_page_it_came_from_is_unchanged()
    {
        string root = Path.GetDirectoryName(TestData.DataDir)!;
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "assets", "ships", "manifest.json")));
        foreach (JsonProperty f in manifest.RootElement.GetProperty("factions").EnumerateObject())
        {
            JsonElement src = f.Value.GetProperty("source");
            string page = src.GetProperty("page").GetString()!;
            string now = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(root, page)))).ToLowerInvariant();
            Assert.True(now == src.GetProperty("sha256").GetString(),
                $"{f.Name}: {page} changed since the ships were exported; re-run tools/mockups/export_ships.mjs");
        }
    }

    [Fact]
    public void Every_scenario_is_fought_under_its_own_nebula_sky_and_its_sky_file_and_panorama_exist()
    {
        string root = Path.GetDirectoryName(TestData.DataDir)!;
        foreach (Scenario s in TestData.Data.Scenarios)
        {
            Assert.True(File.Exists(Path.Combine(root, "data", "space", "skies", s.Sky + ".json")), $"{s.Id}: no sky data/space/skies/{s.Sky}.json");
            Assert.True(File.Exists(Path.Combine(root, "assets", "skies", s.Sky + ".jpg")), $"{s.Id}: no panorama assets/skies/{s.Sky}.jpg (tools/space/sky_panorama.py)");
        }
        var shared = TestData.Data.Scenarios.GroupBy(s => s.Sky).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        Assert.True(shared.Length == 0, $"skies used by more than one scenario: {string.Join(", ", shared)}");
    }

    [Fact]
    public void Every_mount_a_scene_adds_names_a_weapon_that_exists()
    {
        foreach (Scenario sc in TestData.Data.Scenarios)
            foreach (ShipStart st in sc.Ships)
                foreach (MountDef m in st.ExtraMounts ?? Array.Empty<MountDef>())
                    Assert.True(TestData.Data.Weapons.ContainsKey(m.Weapon), $"{sc.Id}/{st.Id}: no weapon '{m.Weapon}'");
    }
}
