// Nebula plumes and ion storms (Rules.Cloak, Battle.Missiles, Battle.Nebula; openspec/changes/nebula-plumes). Owner,
// 2026-11-10: "entering a column of nebula plume can hide you from enemies. weapons have difficulty targetting, missiles
// dont quite work, energy weapons 1/2 range", "and 1/2 acuracy to hit", "some nebulas are ion storms, which will have cool
// lightning", "which can strike ships, and cause a temprorary shut down of the reactor core, the ship loosing power for a
// few seconds completely". The numbers are data/battle/systems/cloak.json's nebula block.
using System;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class NebulaTests
{
    private static NebulaRules Nb => TestData.Data.Systems.Cloak.Nebula;

    // a plume of radius 20 km round the first ship (at the origin), at a density, a storm or not
    private static string Plume(double density = 1, bool storm = false, double zM = 0) =>
        "\"nebulae\": [{\"x_m\": 0, \"z_m\": " + zM + ", \"r_m\": 20000, \"density\": " + density.ToString(System.Globalization.CultureInfo.InvariantCulture) + (storm ? ", \"storm\": true" : "") + "}], ";

    private static void Run(Battle b, double s) { for (int i = 0; i < Math.Round(s / b.Tuning.StepS); i++) b.Step(); }

    private static Battle Duel(string scene, double zM = -30000) =>
        CloakTests.Pair("ca", "concord", "krai_ff", "krai", zM, scene: scene, extraB: ", \"hold_fire\": true");

    [Fact]
    public void In_the_thick_of_a_plume_a_shot_hits_half_as_often()
    {
        Battle open = Duel(""), thick = Duel(Plume()), thin = Duel(Plume(0.5));
        foreach (string kind in new[] { "torpedo", "beam" })
        {
            double Chance(Battle b)
            {
                Ship t = b.ShipById("b")!;
                t.Ecm = 0;
                t.Z = -20000 - 1;   // just outside the plume, well inside every weapon's reach
                return b.Rules.HitChance(b.Player, b.Player.Mounts.First(m => m.Weapon.Kind == kind), t);
            }
            Assert.True(Chance(open) > 0);
            // a torpedo's reach is its own; a beam's is halved too, which costs it more of its chance at this distance
            if (kind == "torpedo")
            {
                Assert.Equal(Chance(open) * Nb.HitX, Chance(thick), 9);
                Assert.Equal(Chance(open) * (1 - (1 - Nb.HitX) * 0.5), Chance(thin), 9);
            }
            else Assert.True(Chance(thick) < Chance(open) * Nb.HitX);
        }
    }

    [Fact]
    public void An_energy_weapon_reaches_half_as_far_from_or_into_a_plume_and_a_torpedo_as_far_as_ever()
    {
        Battle open = Duel(""), inside = Duel(Plume()), into = Duel(Plume(zM: -30000));
        foreach (Battle b in new[] { inside, into })
        {
            Ship me = b.Player, t = b.ShipById("b")!;
            Mount lance = me.Mounts.First(m => m.Weapon.Kind == "beam"), torp = me.Mounts.First(m => m.Weapon.Kind == "torpedo");
            Assert.Equal(lance.Weapon.Normal.RangeKm * Nb.EnergyRangeX, b.Rules.RangeKm(me, lance, t), 9);
            Assert.Equal(torp.Weapon.Normal.RangeKm, b.Rules.RangeKm(me, torp, t), 9);
            if (b == into) continue;
            // out of the plume it fires from, the target is still beyond the halved reach
            t.Z = -(lance.Weapon.Normal.RangeKm * Nb.EnergyRangeX + 5) * 1000;
            Assert.False(b.Rules.InRange(me, lance, t));
            Assert.Equal(0, b.Rules.HitChance(me, lance, t));
        }
        Ship o = open.Player, ot = open.ShipById("b")!;
        Mount ol = o.Mounts.First(m => m.Weapon.Kind == "beam");
        ot.Z = -(ol.Weapon.Normal.RangeKm * Nb.EnergyRangeX + 5) * 1000;
        Assert.True(open.Rules.InRange(o, ol, ot));
        // with no target named, the reach is by where the ship itself is (the radar's ring)
        Assert.Equal(ol.Weapon.Normal.RangeKm * Nb.EnergyRangeX, inside.Rules.RangeKm(inside.Player, inside.Player.Mounts.First(m => m.Weapon.Kind == "beam")), 9);
        Assert.Equal(ol.Weapon.Normal.RangeKm, into.Rules.RangeKm(into.Player, into.Player.Mounts.First(m => m.Weapon.Kind == "beam")), 9);
    }

    [Fact]
    public void Where_plumes_overlap_the_denser_counts()
    {
        Battle b = Duel("\"nebulae\": [{\"x_m\": 0, \"z_m\": 0, \"r_m\": 20000, \"density\": 0.4}, {\"x_m\": 0, \"z_m\": 0, \"r_m\": 30000, \"density\": 1, \"storm\": true}], ");
        Assert.Equal(1, b.Rules.NebulaAt(0, 0), 9);
        Assert.Equal(1, b.Rules.StormAt(0, 0), 9);
        Assert.Equal(0, Duel(Plume()).Rules.StormAt(0, 0));
    }

    // a Krai dreadnought's green missile at a Concord cruiser 40 km off that holds still and holds its fire
    private static bool MissileLost(ulong seed, string scene)
    {
        string json = "{\"id\": \"m\", \"name\": \"M\", \"side\": \"krai\", \"sky\": \"quiet\", \"seed\": " + seed + ", \"objective\": \"O\", \"say\": [\"Comms\", \"Hi.\"], " + scene + "\"ships\": ["
            + "{\"id\": \"k\", \"name\": \"K\", \"captain\": \"C\", \"class\": \"krai_dn\", \"faction\": \"krai\", \"side\": 0, \"x_m\": 0, \"z_m\": 0, \"heading_deg\": 0, \"speed_m_s\": 0},"
            + "{\"id\": \"c\", \"name\": \"C\", \"captain\": \"C\", \"class\": \"ca\", \"faction\": \"concord\", \"side\": 1, \"x_m\": 0, \"z_m\": -40000, \"heading_deg\": 90, \"speed_m_s\": 0, \"hold_fire\": true, \"stance\": \"hold\"}]}";
        var b = new Battle(TestData.Data, DataLoader.LoadScenario(StrictJson.Parse(json, "test"), TestData.Data.Classes, TestData.Data.Factions));
        b.Lock("c");
        foreach (Mount m in b.Player.Mounts) { m.Selected = m.Weapon.Kind == "missile"; m.Charge = 1; }
        Assert.Equal(1, b.Fire());
        Shot sh = b.Shots.Single(s => s.Kind == "missile");
        for (int i = 0; i < 600 && !sh.Spent && !sh.Lost; i++) b.Step();
        return sh.Lost;
    }

    [Fact]
    public void A_missile_after_a_ship_in_a_plume_often_loses_it_and_never_in_open_space()
    {
        string plume = "\"nebulae\": [{\"x_m\": 0, \"z_m\": -40000, \"r_m\": 30000, \"density\": 1}], ";
        int lost = 0;
        for (ulong seed = 1; seed <= 16; seed++)
        {
            if (MissileLost(seed, plume)) lost++;
            Assert.False(MissileLost(seed, ""));
        }
        Assert.InRange(lost, 1, 15);
    }

    [Fact]
    public void A_lightning_strike_trips_the_core_and_the_ship_has_no_power_until_it_restarts()
    {
        Battle b = Duel(Plume());   // no storm: the strike is ordered here, so the timing is the rule's alone
        Ship me = b.Player, t = b.ShipById("b")!;
        b.Lock("b");
        Run(b, 2);
        foreach (Mount m in me.Mounts) m.Charge = 0.5;
        me.Capacitor = 1;
        double battery = me.Battery, shield = me.Shields[0] = me.Class.ShieldsHp[0] / 2;
        b.LightningStrike(me);
        Assert.True(me.CoreDown);
        Assert.Single(b.Lightning);
        Assert.Contains(b.Log, l => l.Who == "Engineering" && l.Text.StartsWith("Lightning strike!"));
        Run(b, Nb.StormShutdownS - 1);
        Assert.True(me.CoreDown);
        Assert.Equal(0, me.Brown);
        Assert.Equal(0, b.Rules.MaxSpeed(me));
        Assert.Equal(1, me.Capacitor);
        Assert.Equal(battery, me.Battery);
        Assert.Equal(shield, me.Shields[0]);
        Assert.All(me.Mounts, m => Assert.Equal(0.5, m.Charge));
        Assert.False(me.ShieldsHold);
        Assert.True(b.FacingDown(me, t));
        foreach (Mount m in me.Mounts) m.Charge = 1;
        t.Z = -15000;
        Assert.Empty(b.WouldFire());
        Assert.Equal(0, b.Fire());
        Run(b, 1.5);
        Assert.False(me.CoreDown);
        Assert.True(me.Brown > 0);
        Assert.True(me.ShieldsHold);
        Assert.Contains(b.Log, l => l.Text.StartsWith("Core restarted"));
        Assert.True(b.Fire() > 0);
    }

    [Fact]
    public void A_ship_with_its_core_down_takes_every_hit_on_the_hull()
    {
        Battle b = Duel(Plume(), zM: -15000);
        Ship me = b.Player, t = b.ShipById("b")!;
        b.Lock("b");
        b.LightningStrike(t);
        double[] shields = t.Shields.ToArray();
        double hull = t.Hull;
        for (int i = 0; i < 40 && t.Hull >= hull; i++)
        {
            foreach (Mount m in me.Mounts) { m.Selected = m.Weapon.Kind == "beam"; m.Charge = 1; }
            b.Fire();
        }
        Assert.True(t.Hull < hull);
        Assert.Equal(shields, t.Shields);
    }

    [Fact]
    public void A_strike_drops_a_cloak()
    {
        Battle b = CloakTests.Pair("krai_ff", "krai", "ca", "concord", -90000, extraB: ", \"hold_fire\": true");
        b.ToggleCloak();
        Run(b, 4);
        Assert.True(b.Player.Cloaked);
        b.LightningStrike(b.Player);
        Assert.False(b.Player.CloakOn);
    }

    [Fact]
    public void Lightning_finds_a_ship_in_a_storm_never_twice_within_the_storms_grace_and_never_in_a_plain_plume()
    {
        Battle storm = Duel(Plume(storm: true)), plain = Duel(Plume());
        var at = new System.Collections.Generic.List<double>();
        for (int i = 0; i < Math.Round(300 / storm.Tuning.StepS); i++)
        {
            bool was = storm.Player.CoreDown;
            storm.Step();
            if (!was && storm.Player.CoreDown) at.Add(storm.T);
            plain.Step();
        }
        // three a minute at full density, less the trip and the grace after each: several in five minutes, not dozens
        Assert.InRange(at.Count, 3, 15);
        for (int i = 1; i < at.Count; i++) Assert.True(at[i] - at[i - 1] >= Nb.StormShutdownS + Nb.StormGraceS - 1e-6);
        Assert.Equal(0, storm.ShipById("b")!.StormGraceS);   // outside the storm, never struck
        Assert.Equal(0, plain.Player.StormGraceS);
        Assert.Empty(plain.Lightning);
    }

    [Fact]
    public void A_scenario_names_each_planets_kind_and_each_plumes_look()
    {
        Scenario Load(string scene) => DataLoader.LoadScenario(StrictJson.Parse(
            "{\"id\": \"s\", \"name\": \"S\", \"side\": \"concord\", \"sky\": \"quiet\", \"seed\": 7, \"objective\": \"O\", \"say\": [\"Comms\", \"Hi.\"], " + scene + "\"ships\": ["
            + "{\"id\": \"a\", \"name\": \"A\", \"captain\": \"C\", \"class\": \"ca\", \"faction\": \"concord\", \"side\": 0, \"x_m\": 0, \"z_m\": 0, \"heading_deg\": 0, \"speed_m_s\": 0},"
            + "{\"id\": \"b\", \"name\": \"B\", \"captain\": \"C\", \"class\": \"krai_ff\", \"faction\": \"krai\", \"side\": 1, \"x_m\": 0, \"z_m\": -50000, \"heading_deg\": 180, \"speed_m_s\": 0}]}", "test"),
            TestData.Data.Classes, TestData.Data.Factions);
        string Planet(string kind) => "\"planets\": [{\"id\": \"p\", \"name\": \"P\", \"x_m\": 0, \"z_m\": -200000, \"radius_m\": 16000, \"color\": \"#4f7fa8\"" + kind + "}], ";
        Assert.Equal("terran", Load(Planet("")).Planets![0].Kind);
        Assert.Equal("gas", Load(Planet(", \"kind\": \"gas\"")).Planets![0].Kind);
        Assert.Throws<DataError>(() => Load(Planet(", \"kind\": \"cheese\"")));
        string Neb(string more) => "\"nebulae\": [{\"x_m\": 0, \"z_m\": 0, \"r_m\": 20000, \"density\": 1" + more + "}], ";
        Assert.Equal("violet", Load(Neb("")).Nebulae![0].Look);
        Assert.Equal(NebulaDef.StormLook, Load(Neb(", \"storm\": true")).Nebulae![0].Look);
        Assert.Equal("cinder", Load(Neb(", \"storm\": true, \"look\": \"cinder\"")).Nebulae![0].Look);
        Assert.Throws<DataError>(() => Load(Neb(", \"look\": \"mauve\"")));
        Assert.Throws<DataError>(() => Load(Neb(", \"density\": 0")));
    }
}
