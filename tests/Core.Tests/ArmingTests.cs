// Weapon arming (openspec/changes/weapon-arming): a weapon is armed Offline, Normal, or one of its own modes; every rule and
// readout uses the armed mode's numbers; a mode it lacks is refused. Overloaded lances reach only 4 km; proximity torpedoes
// ignore the target's speed, hit more often for less damage, and reach the facings beside the one they burst on.
using System.Linq;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class ArmingTests
{
    [Fact]
    public void An_overloaded_lance_hits_harder_but_is_out_of_range_at_6_km_until_it_closes_to_4()
    {
        Battle b = TargetingTests.Duel(("t", "krai_ff", 0, -6000, 0));
        Ship me = b.Player, t = b.ShipById("t")!;
        Mount lance = me.Mounts[0];
        b.ArmOne(0, Arming.Overload);
        Assert.Equal(Arming.Overload, lance.State);
        Assert.True(Rules.Mode(lance).Damage > lance.Weapon.Normal.Damage);
        Assert.False(Rules.InRange(me, lance, t));
        Assert.Equal(0, b.Rules.HitChance(me, lance, t));
        t.Z = -3900;
        Assert.True(b.Rules.HitChance(me, lance, t) > 0);
    }

    [Fact]
    public void A_proximity_torpedo_hits_a_fast_jinking_frigate_more_often_than_a_normal_one_for_less_damage()
    {
        Battle b = TargetingTests.Duel(("t", "krai_ff", 0, -6000, 0));
        Ship me = b.Player, t = b.ShipById("t")!;
        t.V = 300; t.Erratic = true;
        Mount torp = me.Mounts[4];
        double normal = b.Rules.HitChance(me, torp, t), dmg = Rules.Mode(torp).Damage;
        b.ArmOne(4, Arming.Proximity);
        Assert.True(b.Rules.HitChance(me, torp, t) > normal);
        Assert.True(Rules.Mode(torp).Damage < dmg);
        Assert.True(b.Rules.ReachDeg(torp) > b.Rules.Tactical.Strike.ReachDeg);
        Assert.Equal("Wide arc", Rules.AimsBy(torp));
    }

    [Fact]
    public void A_proximity_shot_flies_in_its_own_colour_and_carries_the_wider_block_to_the_hit()
    {
        Battle b = TargetingTests.Duel(("t", "krai_ff", 0, -6000, 0));
        b.Lock("t");
        b.ArmOne(4, Arming.Proximity);
        b.Player.Mounts[4].Charge = 1;
        Assert.True(b.FireOne(4));
        Shot sh = b.Shots.Last();
        Assert.Equal(b.Player.Mounts[4].Weapon.Proximity!.Color, sh.Color);
        Assert.Equal(b.Player.Mounts[4].Weapon.Proximity!.ReachDeg, sh.ReachDeg);
        Assert.Equal("arc", sh.AimBy);
    }

    [Fact]
    public void A_mode_the_weapon_lacks_is_refused()
    {
        Battle b = TargetingTests.Duel(("t", "krai_ff", 0, -6000, 0));
        Ship me = b.Player;
        b.ArmOne(0, Arming.Proximity);                       // a lance has no proximity fuse
        Assert.Equal(Arming.Normal, me.Mounts[0].State);
        int pd = me.Mounts.First(m => m.Weapon.IsPointDefence).Index;
        b.ArmOne(pd, Arming.Overload);                       // point defence is never armed from here
        Assert.Equal(Arming.Normal, me.Mounts[pd].State);
        Assert.False(Rules.CanArm(me.Mounts[10].Weapon, Arming.Overload));   // nor the missile rack overloaded
    }

    [Fact]
    public void Changing_mode_keeps_at_most_half_a_charge_and_going_offline_drains_it()
    {
        Battle b = TargetingTests.Duel(("t", "krai_ff", 0, -6000, 0));
        Mount lance = b.Player.Mounts[0];
        lance.Charge = 1;
        b.ArmOne(0, Arming.Overload);
        Assert.Equal(0.5, lance.Charge, 6);
        b.ArmOne(0, Arming.Offline);
        Assert.Equal(0, lance.Charge);
    }

    [Fact]
    public void Arming_the_selected_weapons_arms_only_those_that_have_the_mode()
    {
        Battle b = TargetingTests.Duel(("t", "krai_ff", 0, -6000, 0));
        b.SelectGroup(null);
        b.SetArming(Arming.Proximity);
        Assert.All(b.Player.Mounts.Where(m => m.Weapon.Id == "torpedo"), m => Assert.Equal(Arming.Proximity, m.State));
        Assert.All(b.Player.Mounts.Where(m => m.Weapon.Id == "lance"), m => Assert.Equal(Arming.Normal, m.State));
    }
}
