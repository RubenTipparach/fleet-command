// Cloaking (Battle.Cloak; openspec/changes/cloaking; owner, 2026-11-04: "we need both sides to cloak. And scan with eccm").
// The numbers are data/battle/systems/cloak.json; each test reads them rather than retyping them.
using System;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class CloakTests
{
    private static CloakSystems Cs => TestData.Data.Systems.Cloak;

    // two ships zKm apart, bows toward each other; extra JSON per ship
    internal static Battle Pair(string a, string fa, string b, string fb, double zM, string extraA = "", string extraB = "", string scene = "")
    {
        string json = "{\"id\": \"s\", \"name\": \"S\", \"side\": \"" + fa + "\", \"sky\": \"quiet\", \"seed\": 7, \"objective\": \"O\", \"say\": [\"Comms\", \"Hi.\"], " + scene + "\"ships\": ["
            + "{\"id\": \"a\", \"name\": \"A\", \"captain\": \"C\", \"class\": \"" + a + "\", \"faction\": \"" + fa + "\", \"side\": 0, \"x_m\": 0, \"z_m\": 0, \"heading_deg\": 0, \"speed_m_s\": 0" + extraA + "},"
            + "{\"id\": \"b\", \"name\": \"B\", \"captain\": \"C\", \"class\": \"" + b + "\", \"faction\": \"" + fb + "\", \"side\": 1, \"x_m\": 0, \"z_m\": " + zM + ", \"heading_deg\": 180, \"speed_m_s\": 0, \"stance\": \"hold\"" + extraB + "}]}";
        return new Battle(TestData.Data, DataLoader.LoadScenario(StrictJson.Parse(json, "test"), TestData.Data.Classes, TestData.Data.Factions));
    }

    private static void Run(Battle b, double s) { for (int i = 0; i < Math.Round(s / b.Tuning.StepS); i++) b.Step(); }

    [Fact]
    public void Every_class_named_as_fitted_with_a_cloak_exists()
    {
        foreach (string c in Cs.Cloak.Fitted) Assert.True(TestData.Data.Classes.ContainsKey(c), $"no class '{c}'");
    }

    [Fact]
    public void A_cloak_fades_in_over_its_fade_and_out_again_when_ordered_off()
    {
        Battle b = Pair("ca", "concord", "krai_ff", "krai", -9000, extraB: ", \"hold_fire\": true");
        b.ToggleCloak();
        Assert.True(b.Player.CloakOn);
        Run(b, Cs.Cloak.FadeS / 2);
        Assert.InRange(b.Player.CloakK, 0.45, 0.55);
        Run(b, Cs.Cloak.FadeS / 2 + 0.1);
        Assert.True(b.Player.Cloaked);
        b.ToggleCloak();
        Run(b, Cs.Cloak.FadeS + 0.1);
        Assert.Equal(0, b.Player.CloakK);
    }

    [Fact]
    public void A_ship_without_a_cloak_cannot_cloak()
    {
        Battle b = Pair("ff", "concord", "krai_ff", "krai", -9000);
        b.ToggleCloak();
        Assert.False(b.Player.CloakOn);
        Assert.Contains(b.Log, l => l.Text == "Cannot cloak: no cloak fitted.");
    }

    [Fact]
    public void Cloaking_draws_its_energy_and_drops_the_shields_and_the_guns()
    {
        Battle b = Pair("ca", "concord", "krai_ff", "krai", -3000, extraB: ", \"hold_fire\": true");
        int before = b.Rules.Used(b.Player);
        b.ToggleCloak();
        Assert.Equal(before + (int)Cs.Cloak.CostE, b.Rules.Used(b.Player));
        b.Step();
        b.Lock("b");
        foreach (Mount m in b.Player.Mounts) m.Charge = 1;
        Assert.Empty(b.WouldFire());
        Assert.Equal(0, b.Fire());
        Assert.Contains(b.Log, l => l.Text == "Cannot fire while cloaked.");
    }

    [Fact]
    public void A_fully_cloaked_enemy_is_unseen_and_unlockable_until_it_comes_within_the_detect_range()
    {
        Battle b = Pair("ca", "concord", "krai_ff", "krai", -9000, extraB: ", \"cloaked\": true, \"hold_fire\": true");
        Ship t = b.ShipById("b")!;
        Assert.False(b.Detected(b.Player, t));
        Assert.Empty(b.SeenFoes(b.Player));
        b.Lock("b");
        Assert.Null(b.Player.Target);
        Assert.True(b.Detected(t, b.Player));   // it sees you
        t.Z = -(Cs.Cloak.DetectKm * 1000 - 100);
        Assert.True(b.Detected(b.Player, t));
    }

    [Fact]
    public void A_sweep_reveals_cloaked_ships_within_its_reach_for_its_reveal_time_and_then_cools_down()
    {
        Battle b = Pair("ca", "concord", "krai_ff", "krai", -9000, extraA: ", \"eccm\": 2, \"power_e\": {\"sensors\": 3}, \"battery_frac\": 1", extraB: ", \"cloaked\": true, \"hold_fire\": true");
        Ship t = b.ShipById("b")!;
        double km = Cs.Cloak.BaseKm + 2 * Cs.Cloak.PerEccmKm;
        Assert.Equal(km, b.SweepKm(b.Player), 9);
        Assert.True(km >= 9);
        double bat = b.Player.Battery;
        b.Sweep();
        Assert.Equal(bat - Cs.Cloak.SweepE, b.Player.Battery, 9);
        Assert.True(b.Detected(b.Player, t));
        Assert.Single(b.Sweeps);
        b.Lock("b");
        Assert.Equal("b", b.Player.Target);
        b.Sweep();   // cooling down: nothing spent
        Assert.Equal(bat - Cs.Cloak.SweepE, b.Player.Battery, 9);
        Run(b, Cs.Cloak.RevealS + 0.2);
        Assert.False(b.Detected(b.Player, t));
        Assert.Null(b.Player.Target);   // the lock is lost with the reveal
        Assert.Contains(b.Log, l => l.Text.StartsWith("Lost lock on B"));
    }

    [Fact]
    public void A_sweep_beyond_its_reach_finds_nothing()
    {
        Battle b = Pair("ca", "concord", "krai_ff", "krai", -9000, extraB: ", \"cloaked\": true, \"hold_fire\": true");
        Assert.Equal(Cs.Cloak.BaseKm, b.SweepKm(b.Player), 9);
        b.Sweep();
        Assert.False(b.Detected(b.Player, b.ShipById("b")!));
    }

    [Fact]
    public void A_revealed_cloaked_ship_is_harder_to_hit_by_the_cloaks_penalty()
    {
        Battle b = Pair("ca", "concord", "krai_ff", "krai", -3000, extraB: ", \"hold_fire\": true");
        Ship t = b.ShipById("b")!;
        Mount m = b.Player.Mounts.First(x => x.Weapon.Id == "lance");
        double open = b.Rules.HitChance(b.Player, m, t);
        t.CloakOn = true; t.CloakK = 1;
        Assert.Equal(open - Cs.Cloak.HitPenalty, b.Rules.HitChance(b.Player, m, t), 9);
    }

    [Fact]
    public void A_cloaking_ships_shields_take_nothing()
    {
        Battle b = Pair("ca", "concord", "krai_ca", "krai", -3000, extraB: ", \"hold_fire\": true");
        b.ToggleCloak();
        b.Step();
        double shields = b.Player.Shields.Sum(), hull = b.Player.Hull;
        // the Krai cruiser's guns on the cloaking Resolute: it still sees it (it is not fully cloaked yet)
        Ship k = b.ShipById("b")!;
        k.HoldFire = false;
        k.ThinkT = 0;
        foreach (Mount m in k.Mounts) m.Charge = 1;
        Run(b, 2.5);   // its bolts in flight land; by 3 s the Resolute is fully cloaked and gone from its sight
        Assert.True(b.Player.Hull < hull);
        Assert.True(b.Player.Shields.Sum() <= shields + 1e-9);
    }

    [Fact]
    public void A_cloaking_captain_decloaks_to_strike_and_cloaks_again_after_its_fight()
    {
        Scenario sc = TestData.Data.ScenarioById("concord_cloak");
        var b = new Battle(TestData.Data, sc);
        Ship v = b.ShipById("vekk")!;
        Assert.True(v.Cloaked);
        bool struck = false, again = false;
        for (int i = 0; i < 120 / b.Tuning.StepS && b.Over == null; i++)
        {
            b.Step();
            if (!v.CloakOn && !struck) { struck = true; Assert.True(Rules.DistKm(v, b.Player) < Cs.AiCloak.StrikeKm); }
            if (struck && v.CloakOn) { again = true; break; }
        }
        Assert.True(struck, "it never decloaked to strike");
        Assert.True(again, "it never cloaked again after its fight");
    }

    [Fact]
    public void A_captain_that_sees_no_enemy_sweeps_for_one()
    {
        Battle b = Pair("krai_ff", "krai", "ca", "concord", -6000, extraA: ", \"cloaked\": true");
        b.Player.CloakOn = true;
        Ship c = b.ShipById("b")!;
        c.Battery = c.Class.BatteryE;
        Run(b, 1);
        Assert.True(c.SweepT > 0);
        Assert.Contains(b.Sweeps, w => w.Side == 1);
    }
}
