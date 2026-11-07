// Weapon targeting (openspec/changes/weapon-targeting): the chopping block a facing exposes, precise weapons that aim and
// by-arc weapons that do not, the price of aiming, pinned fire and its end, aims at a target's weapons, the Disable scene's
// objective, point defence's modes, and the weapons that keep their own power when weapons control fails. Every hit goes
// through Battle.Damage, so these fire real shots on the real data.
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class TargetingTests
{
    // a Concord cruiser at the origin heading north (-z), and enemies where asked, from the real classes
    internal static Battle Duel(params (string Id, string Class, double X, double Z, double HeadingDeg)[] foes)
    {
        string ships = "{\"id\": \"me\", \"name\": \"Me\", \"captain\": \"C\", \"class\": \"ca\", \"faction\": \"concord\", \"side\": 0, \"x_m\": 0, \"z_m\": 0, \"heading_deg\": 0, \"speed_m_s\": 0}";
        foreach (var f in foes)
            ships += $", {{\"id\": \"{f.Id}\", \"name\": \"{f.Id.ToUpperInvariant()}\", \"captain\": \"C\", \"class\": \"{f.Class}\", \"faction\": \"krai\", \"side\": 1, \"x_m\": {f.X}, \"z_m\": {f.Z}, \"heading_deg\": {f.HeadingDeg}, \"speed_m_s\": 0}}";
        string json = "{\"id\": \"t\", \"name\": \"T\", \"side\": \"concord\", \"sky\": \"quiet\", \"seed\": 7, \"objective\": \"O\", \"say\": [\"Comms\", \"Hello.\"], \"ships\": [" + ships + "]}";
        Scenario sc = DataLoader.LoadScenario(StrictJson.Parse(json, "test"), TestData.Data.Classes, TestData.Data.Factions);
        return new Battle(TestData.Data, sc);
    }

    // fires the cruiser's forward lances at the locked target n times (charged by hand), its shields down and its hull deep
    private static void Volleys(Battle b, int n, params int[] mounts)
    {
        Ship t = b.TargetOf(b.Player)!;
        t.ShieldsUp = false;
        for (int i = 0; i < n; i++)
        {
            t.Hull = 1e6;
            foreach (Mount m in b.Player.Mounts) m.Selected = mounts.Contains(m.Index);
            foreach (int k in mounts) b.Player.Mounts[k].Charge = 1;
            b.Fire();
        }
    }

    [Fact]
    public void A_hit_through_the_bow_exposes_the_bow_and_the_internals_and_one_through_the_stern_the_engines()
    {
        Battle b = Duel(("t", "krai_ff", 0, -30000, 0));
        Ship t = b.ShipById("t")!;
        StrikeDef st = b.Rules.Tactical.Strike;
        Exposure bow = Strike.Of(t, 0, st), stern = Strike.Of(t, 3, st);
        Assert.Contains("sensors", bow.Sys);
        Assert.Contains("shields", bow.Sys);      // the internals, from any side
        Assert.DoesNotContain("engines", bow.Sys);
        Assert.Contains("engines", stern.Sys);
        Assert.Contains("shields", stern.Sys);
        Assert.DoesNotContain("sensors", stern.Sys);
    }

    [Fact]
    public void A_wider_reach_puts_the_mounts_of_the_neighbouring_facings_on_the_block()
    {
        Battle b = Duel(("t", "krai_ff", 0, -30000, 0));
        StrikeDef st = b.Rules.Tactical.Strike;
        Ship me = b.Player;   // the cruiser's broadside lances sit 85 degrees off the bow
        Assert.True(Strike.Of(me, 0, st, 120).Mounts.Count > Strike.Of(me, 0, st).Mounts.Count);
    }

    [Fact]
    public void Precise_lances_aimed_at_the_engines_wreck_them_from_astern_and_never_touch_them_from_ahead()
    {
        // the target's stern toward us: its engines are on the block
        Battle astern = Duel(("t", "krai_ff", 0, -30000, 0));
        astern.Lock("t"); astern.Aim("engines");
        Volleys(astern, 40, 0, 1);
        // its bow toward us: the engines are not on the block, so no hit, aimed or spread, can reach them
        Battle ahead = Duel(("t", "krai_ff", 0, -30000, 180));
        ahead.Lock("t"); ahead.Aim("engines");
        Volleys(ahead, 40, 0, 1);
        Assert.True(astern.ShipById("t")!.Sys["engines"] < 0.5, $"engines from astern at {astern.ShipById("t")!.Sys["engines"]:0.00}");
        Assert.Equal(1, ahead.ShipById("t")!.Sys["engines"]);
        Assert.True(ahead.ShipById("t")!.Sys["sensors"] < 1, "the bow's own subsystems take the spread");
    }

    [Fact]
    public void Every_shot_carries_its_aim_and_whether_its_weapon_aims_to_where_it_lands()
    {
        Battle b = Duel(("t", "krai_ff", 0, -30000, 0));
        b.Lock("t"); b.Aim("engines");
        b.Player.Mounts[0].Charge = 1;
        b.Player.Mounts[4].Charge = 1;
        b.FireOne(0);
        Shot beam = b.Shots.Last();
        b.FireOne(4);
        Shot torp = b.Shots.Last();
        Assert.Equal(("engines", "precise"), (beam.Aim, beam.AimBy));
        Assert.Equal(("engines", "arc"), (torp.Aim, torp.AimBy));   // a torpedo lands anywhere on the block, whatever its aim
        Assert.Equal(b.Rules.Tactical.Strike.ReachDeg, torp.ReachDeg);
    }

    [Fact]
    public void Aiming_at_a_subsystem_costs_the_captain_ten_points_of_hit_chance()
    {
        Battle b = Duel(("t", "krai_ff", 0, -30000, 0));
        Ship me = b.Player, t = b.ShipById("t")!;
        double hull = b.Rules.HitChance(me, me.Mounts[0], t, "hull"), engines = b.Rules.HitChance(me, me.Mounts[0], t, "engines");
        Assert.Equal(b.Rules.Tactical.AimHitPenalty, hull - engines, 6);
        // the enemy's captain never pays it: its side does not aim
        Assert.Equal(b.Rules.HitChance(t, t.Mounts[1], me, "hull"), b.Rules.HitChance(t, t.Mounts[1], me, "engines"), 6);
    }

    [Fact]
    public void A_direct_hit_aimed_at_a_weapon_lands_on_that_mount()
    {
        Battle b = Duel(("t", "krai_ff", 0, -30000, 180));
        b.Lock("t");
        Ship t = b.ShipById("t")!;
        int bow = b.ExposureFrom(t, b.Player).Mounts.First();
        b.Aim($"w:{bow}");
        Volleys(b, 20, 0, 1);
        Assert.Equal(0, t.Mounts[bow].Hp);
    }

    [Fact]
    public void Locking_a_new_target_turns_an_aim_at_a_weapon_into_an_aim_at_weapons_control()
    {
        Battle b = Duel(("a", "krai_ff", 0, -30000, 180), ("c", "krai_cv", 20000, -30000, 180));
        b.Lock("a");
        b.Aim("w:1");
        b.Lock("c");
        Assert.Equal("weapons", b.Player.Aim);
    }

    [Fact]
    public void A_pinned_weapon_fires_at_its_own_ship_with_its_own_aim_whatever_is_locked()
    {
        Battle b = Duel(("a", "krai_ff", 0, -30000, 180), ("c", "krai_cv", -25000, -30000, 180));
        b.Lock("a"); b.Aim("engines");
        b.Pin(0, "c");
        b.Aim("hull");
        foreach (Mount m in b.Player.Mounts) m.Charge = 1;
        var f = b.Fireable().First(x => x.M.Index == 0);
        Assert.Equal("c", f.T.Id);
        Assert.Equal("engines", f.Aim);
        Assert.Equal("a", b.Fireable().First(x => x.M.Index == 1).T.Id);
        b.Pin(0, "c");   // the same pin again unpins it
        Assert.Null(b.Player.Mounts[0].Pin);
    }

    [Fact]
    public void A_pin_stops_and_says_why_when_its_ship_or_its_aimed_subsystem_is_destroyed()
    {
        Battle b = Duel(("a", "krai_ff", 0, -30000, 180), ("c", "krai_cv", -25000, -30000, 180));
        b.Lock("a"); b.Aim("sensors");
        b.Pin(0, "a");
        b.Aim("hull");
        b.Pin(1, "c");
        b.ShipById("a")!.Sys["sensors"] = 0;
        b.ShipById("c")!.Alive = false;
        b.Step();
        Assert.Null(b.Player.Mounts[0].Pin);
        Assert.Null(b.Player.Mounts[1].Pin);
        Assert.Contains(b.Log, l => l.Text == "W1 unpinned: A's sensors destroyed.");
        Assert.Contains(b.Log, l => l.Text == "W2 unpinned: C destroyed.");
    }

    [Fact]
    public void The_hit_details_chance_on_the_aim_is_the_hit_chance_times_the_direct_chance_only_when_exposed()
    {
        Battle b = Duel(("t", "krai_ff", 0, -30000, 0));
        Ship me = b.Player, t = b.ShipById("t")!;
        Assert.Equal(b.Rules.HitChance(me, me.Mounts[0], t, "engines") * b.Rules.Tactical.Strike.DirectChance, b.DirectHitChance(me, me.Mounts[0], t, "engines"), 6);
        Assert.Equal(0, b.DirectHitChance(me, me.Mounts[0], t, "sensors"));   // the bow is turned away
        Assert.Equal(0, b.DirectHitChance(me, me.Mounts[4], t, "engines"));   // a torpedo does not aim
    }

    [Fact]
    public void The_Disable_scene_is_won_by_disabling_its_ship_and_lost_by_destroying_it()
    {
        foreach (string id in new[] { "concord_disable", "krai_disable", "kelvane_disable" })
        {
            DisableObjective o = TestData.Data.Systems.Tactical.Objectives.Single(x => x.Scenario == id);
            var won = new Battle(TestData.Data, TestData.Data.ScenarioById(id));
            foreach (string k in o.Systems) won.ShipById(o.Ship)!.Sys[k] = 0.2;
            won.Step();
            Assert.Equal("won", won.Over);
            var lost = new Battle(TestData.Data, TestData.Data.ScenarioById(id));
            lost.ShipById(o.Ship)!.Alive = false;
            lost.Step();
            Assert.Equal("lost", lost.Over);
        }
    }

    [Fact]
    public void Every_disable_objective_names_a_scene_and_an_enemy_ship_in_it()
    {
        foreach (DisableObjective o in TestData.Data.Systems.Tactical.Objectives)
        {
            Scenario sc = TestData.Data.ScenarioById(o.Scenario);
            Assert.Contains(sc.Ships, s => s.Id == o.Ship && s.Side == 1);
        }
    }

    [Fact]
    public void Point_defence_switched_off_never_fires_and_on_auto_it_stops_missiles()
    {
        int Intercepts(PdMode mode, out bool fired)
        {
            Battle b = TestData.NewBattle();
            foreach (Ship s in b.Ships.Where(s => s.Side == 0)) s.Pd = mode;
            fired = false;
            for (int i = 0; i < 3600; i++)
            {
                b.Step();
                fired |= b.Shots.Any(sh => sh.Intercept && b.ShipById(sh.From)!.Side == 0);
            }
            return b.Ships.Where(s => s.Side == 0).Sum(s => s.Intercepts);
        }
        Assert.Equal(0, Intercepts(PdMode.Off, out bool offFired));
        Assert.False(offFired);
        Assert.True(Intercepts(PdMode.Auto, out bool autoFired) > 0);
        Assert.True(autoFired);
    }

    [Fact]
    public void Point_defence_and_missiles_keep_charging_when_weapons_control_is_disabled_and_the_rest_stop()
    {
        Battle b = Duel(("t", "krai_ff", 0, -300000, 0));
        Ship me = b.Player;
        me.Sys["weapons"] = 0.2;
        foreach (Mount m in me.Mounts) m.Charge = 0;
        for (int i = 0; i < 30; i++) b.Step();
        Assert.All(me.Mounts.Where(m => m.Weapon.OwnPower), m => Assert.True(m.Charge > 0));
        Assert.All(me.Mounts.Where(m => !m.Weapon.OwnPower), m => Assert.Equal(0, m.Charge));
        Assert.All(me.Mounts.Where(m => !m.Weapon.OwnPower), m => Assert.Equal(SystemStatus.Off, b.Rules.Status(me, m)));
        Assert.All(me.Mounts.Where(m => m.Weapon.OwnPower), m => Assert.Equal(SystemStatus.Active, b.Rules.Status(me, m)));
    }
}
