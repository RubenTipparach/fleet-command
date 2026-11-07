// Missiles by grade and the countermeasures against them (Battle.Missiles; openspec/changes/missiles). Owner, 2026-11-04:
// "missiles depending on grade will have: red, orange, blue, green", "Different grades have different speeds and damage
// values", "Missiles follow ships around. Can be countered with counter measures like flares, decoy shuttles, or pdc".
using System;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class MissileTests
{
    // a Krai dreadnought (a green rack) 4 km from a Concord cruiser, both holding still; the cruiser holds its fire
    private static Battle Duel(ulong seed = 7)
    {
        string json = "{\"id\": \"m\", \"name\": \"M\", \"side\": \"krai\", \"sky\": \"quiet\", \"seed\": " + seed + ", \"objective\": \"O\", \"say\": [\"Comms\", \"Hi.\"], \"ships\": ["
            + "{\"id\": \"k\", \"name\": \"K\", \"captain\": \"C\", \"class\": \"krai_dn\", \"faction\": \"krai\", \"side\": 0, \"x_m\": 0, \"z_m\": 0, \"heading_deg\": 0, \"speed_m_s\": 0},"
            + "{\"id\": \"c\", \"name\": \"C\", \"captain\": \"C\", \"class\": \"ca\", \"faction\": \"concord\", \"side\": 1, \"x_m\": 0, \"z_m\": -40000, \"heading_deg\": 90, \"speed_m_s\": 0, \"hold_fire\": true, \"stance\": \"hold\"}]}";
        Scenario sc = DataLoader.LoadScenario(StrictJson.Parse(json, "test"), TestData.Data.Classes, TestData.Data.Factions);
        var b = new Battle(TestData.Data, sc);
        b.Lock("c");
        return b;
    }

    private static Shot FireMissile(Battle b)
    {
        foreach (Mount m in b.Player.Mounts) { m.Selected = m.Weapon.Kind == "missile"; m.Charge = 1; }
        Assert.Equal(1, b.Fire());
        return b.Shots.Single(s => s.Kind == "missile");
    }

    [Fact]
    public void The_four_grades_rise_from_red_to_green_in_speed_and_damage()
    {
        var g = TestData.Data.Missiles.Grades;
        Assert.Equal(new[] { "red", "orange", "blue", "green" }, g.Select(x => x.Id).ToArray());
        for (int i = 1; i < g.Count; i++) { Assert.True(g[i].SpeedMS > g[i - 1].SpeedMS); Assert.True(g[i].Damage > g[i - 1].Damage); Assert.True(g[i].FooledByFlareX < g[i - 1].FooledByFlareX); }
    }

    [Fact]
    public void A_missile_flies_at_its_racks_grade_with_its_colour_damage_and_fuel()
    {
        Battle b = Duel();
        Shot sh = FireMissile(b);
        MissileGrade green = TestData.Data.Missiles.Grade("green");
        Assert.Same(green, sh.Grade);
        Assert.Equal(green.SpeedMS, sh.SpeedMS);
        Assert.Equal(green.Damage, sh.Damage);
        Assert.Equal(green.Color, sh.Color);
        Assert.Equal(green.FuelS, sh.Life);
        Assert.Equal(green.PdHits, sh.PdHitsLeft);
    }

    [Fact]
    public void A_ship_throws_flares_when_a_missile_closes_and_the_missiles_it_fools_chase_them()
    {
        int lured = 0, thrown = 0;
        for (ulong seed = 1; seed <= 12; seed++)
        {
            Battle b = Duel(seed);
            Ship c = b.ShipById("c")!;
            Shot sh = FireMissile(b);
            for (int i = 0; i < 2000 && !sh.Spent && sh.Life > 0; i++)
            {
                b.Step();
                if (b.Lures.Any(l => l.Kind == "flare" && l.Owner == "c")) { thrown++; if (sh.Lure is { Kind: "flare" }) lured++; break; }
            }
            Assert.True(c.FlareReadyT > 0 || sh.Spent || sh.Lure != null, $"seed {seed}: no flares and no decoy against a missile closing on the cruiser");
        }
        Assert.True(thrown > 0, "the cruiser never threw flares");
        Assert.True(lured < thrown, "every green missile was fooled; the grade's chance is 15 %");
    }

    [Fact]
    public void Firing_main_weapons_voids_a_ships_decoy()
    {
        Battle b = Duel(3);
        Ship k = b.Player;
        var decoy = new Lure { Kind = "decoy", Owner = k.Id, X = k.X, Z = k.Z + 2000, LifeS = 30 };
        k.Decoy = decoy;
        foreach (Mount m in k.Mounts) m.Charge = 1;
        Assert.True(b.Fire() > 0);
        Assert.False(decoy.Alive);
        Assert.Null(k.Decoy);
    }

    [Fact]
    public void A_missile_fooled_by_a_decoy_chases_it_and_goes_back_for_the_ship_when_it_is_gone()
    {
        Battle b = Duel();
        Shot sh = FireMissile(b);
        var decoy = new Lure { Kind = "decoy", Owner = "c", X = 3000, Z = 0, LifeS = 0.5 };
        sh.Lure = decoy;
        double x0 = sh.X;
        b.Step();
        Assert.True(sh.X > x0, "the missile turns toward the decoy");
        decoy.Alive = false;   // it is gone
        b.Step();
        Assert.Null(sh.Lure);
        Assert.False(sh.Lost);
    }

    [Fact]
    public void A_missile_drifts_dead_when_its_fuel_is_spent()
    {
        Battle b = Duel();
        Shot sh = FireMissile(b);
        Ship c = b.ShipById("c")!;
        c.X = 0; c.Z = -4000000;   // far beyond any missile's reach
        int steps = (int)Math.Ceiling(sh.Life / TestData.Data.Tuning.StepS) + 2;
        for (int i = 0; i < steps; i++) b.Step();
        Assert.DoesNotContain(sh, b.Shots);
        Assert.Equal(c.Class.HullHp, c.Hull);
    }
}
