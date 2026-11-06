// The special weapons in the battle (Battle.Specials; openspec/changes/special-weapons; owner, 2026-11-05: "Implement
// weapons in game"). The numbers are data/battle/special_weapons.json; each test reads them rather than retyping them.
using System;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class SpecialWeaponTests
{
    private static SpecialWeapons Sp => TestData.Data.Specials;

    // two ships 3 km apart, bows toward each other; the second holds its fire
    private static Battle Duel(string a, string fa, string b, string fb, double zM = -3000)
    {
        string json = "{\"id\": \"s\", \"name\": \"S\", \"side\": \"" + fa + "\", \"sky\": \"quiet\", \"seed\": 5, \"objective\": \"O\", \"say\": [\"Comms\", \"Hi.\"], \"ships\": ["
            + "{\"id\": \"a\", \"name\": \"A\", \"captain\": \"C\", \"class\": \"" + a + "\", \"faction\": \"" + fa + "\", \"side\": 0, \"x_m\": 0, \"z_m\": 0, \"heading_deg\": 0, \"speed_m_s\": 0},"
            + "{\"id\": \"b\", \"name\": \"B\", \"captain\": \"C\", \"class\": \"" + b + "\", \"faction\": \"" + fb + "\", \"side\": 1, \"x_m\": 0, \"z_m\": " + zM + ", \"heading_deg\": 180, \"speed_m_s\": 0, \"hold_fire\": true, \"stance\": \"hold\"}]}";
        var bt = new Battle(TestData.Data, DataLoader.LoadScenario(StrictJson.Parse(json, "test"), TestData.Data.Classes, TestData.Data.Factions));
        bt.Lock("b");
        // the target's point defence off, so what is fired arrives (point defence is MissileTests' and BattleTests')
        foreach (Mount m in bt.ShipById("b")!.Mounts) if (m.Weapon.IsPointDefence) m.State = Arming.Offline;
        return bt;
    }

    private static void FireOnly(Battle b, string weapon)
    {
        foreach (Mount m in b.Player.Mounts) { m.Selected = m.Weapon.Id == weapon; m.Charge = 1; }
        Assert.Equal(1, b.Fire());
    }

    [Fact]
    public void A_hit_that_pierces_sends_its_share_through_a_full_shield()
    {
        var (held, toHull) = Rules.Pierced(18, Sp.Forge.PierceX, 28);
        Assert.Equal(18 * (1 - Sp.Forge.PierceX), held, 9);
        Assert.Equal(18 * Sp.Forge.PierceX, toHull, 9);
        Assert.Equal((10.0, 2.0), Rules.Pierced(12, 0, 10));
    }

    [Fact]
    public void The_chorus_grows_with_every_ship_that_joins_it_up_to_its_limit()
    {
        ChorusDef c = Sp.Chorus;
        Assert.Equal(c.DmgPerS, Rules.ChorusDps(c, 0), 9);
        Assert.Equal(c.DmgPerS * (1 + 2 * c.PerJoinerX), Rules.ChorusDps(c, 2), 9);
        Assert.Equal(Rules.ChorusDps(c, c.MaxJoiners), Rules.ChorusDps(c, c.MaxJoiners + 4), 9);
    }

    [Fact]
    public void A_chorus_lance_burns_its_target_for_its_burn_and_its_ends_stay_on_the_ships()
    {
        Battle b = Duel("ca", "concord", "krai_ca", "krai");
        FireOnly(b, "chorus");
        Shot sh = b.Shots.Single(s => s.Kind == "chorus");
        Assert.Equal(Sp.Chorus.BurnS, sh.Life, 9);
        Ship t = b.ShipById("b")!;
        double before = t.Shields.Sum() + t.Hull;
        int steps = (int)Math.Round(Sp.Chorus.BurnS / TestData.Data.Tuning.StepS) + 2;
        for (int i = 0; i < steps; i++) b.Step();
        double taken = before - (t.Shields.Sum() + t.Hull);
        if (sh.Hit) Assert.True(taken > Sp.Chorus.DmgPerS * Sp.Chorus.BurnS * 0.5, $"took {taken}");
        Assert.DoesNotContain(sh, b.Shots);
    }

    [Fact]
    public void A_quell_drops_the_struck_shield_and_stuns_the_weapons_on_that_side()
    {
        Battle b = Duel("dd", "concord", "krai_ca", "krai");
        Ship t = b.ShipById("b")!;
        FireOnly(b, "quell");
        Shot sh = b.Shots.Single(s => s.Kind == "quell");
        sh.Hit = true;
        double hull = t.Hull;
        for (int i = 0; i < 400 && b.Shots.Contains(sh); i++) b.Step();
        Assert.Equal(hull, t.Hull);   // no damage
        Assert.Equal(0, t.Shields[0], 6);   // the bow shield, struck head on, is down
        Assert.Contains(t.Mounts, m => m.StunS > 0);
        Assert.DoesNotContain(t.Mounts.Where(m => m.StunS > 0), m => m.Def.Y < -0.5 && Math.Abs(m.Def.X) < 0.2);   // not the aft lance
        Assert.DoesNotContain(t.Mounts.Where(m => m.StunS > 0), Rules.Ready);
    }

    [Fact]
    public void A_forge_slug_pierces_the_shield_and_leaves_the_hull_burning()
    {
        Battle b = Duel("krai_ff", "krai", "ca", "concord");
        Ship t = b.ShipById("b")!;
        FireOnly(b, "forge");
        Shot sh = b.Shots.Single(s => s.Kind == "slug");
        sh.Hit = true;
        Assert.Equal(Sp.Forge.PierceX, sh.Pierce, 9);
        double hull = t.Hull;
        for (int i = 0; i < 200 && b.Shots.Contains(sh); i++) b.Step();
        Assert.True(t.BurnS > 0);
        Assert.True(t.Hull < hull, "40 % went through a full shield");
        double h2 = t.Hull;
        for (int i = 0; i < 60; i++) b.Step();
        Assert.True(t.Hull < h2, "the hull burns");
    }

    [Fact]
    public void A_blood_charge_surges_and_rams_harder_the_faster_it_closes()
    {
        Battle b = Duel("krai_ff", "krai", "ca", "concord", -1200);
        Ship k = b.Player, t = b.ShipById("b")!;
        Assert.True(b.CanCharge(k));
        double vmax = b.Rules.MaxSpeed(k);
        Assert.True(b.Charge(k));
        Assert.Equal(vmax * Sp.Charge.SpeedX, b.Rules.MaxSpeed(k), 6);
        Assert.False(b.Charge(k));   // not twice
        double hull = t.Hull;
        k.HOrder = k.H;
        for (int i = 0; i < 600 && k.SurgeS > 0; i++) b.Step();
        Assert.True(t.Hull < hull, "the bayonet went in");
        Assert.True(k.SurgeS <= 0);
        Assert.True(Rules.RamDamage(Sp.Charge, 600, 230) > Rules.RamDamage(Sp.Charge, 300, 230));
    }

    [Fact]
    public void Only_the_Krai_can_order_a_blood_charge()
    {
        Battle b = Duel("ca", "concord", "krai_ca", "krai");
        Assert.False(b.CanCharge(b.Player));
        Assert.False(b.Charge(b.Player));
    }
}
