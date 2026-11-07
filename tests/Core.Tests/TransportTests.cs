// Transport and boarding (Battle.Transport, data/battle/systems/comms.json, data/battle/scenes/comms.json): the pads and
// the 1.5 km reach through a downed facing, the boarding queue, the fight aboard and a capture changing sides, a pilot
// needed after, raids on a subsystem or one weapon, troops and the envoy to a friendly ship, the inventory transporter to
// a ship or a planet, the AI's capture order, and every test scene played to its goal from both sides.
using System;
using System.Linq;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class TransportTests
{
    private static Battle Scene(string id) => new(TestData.Data, TestData.Data.ScenarioById(id));
    private static void Run(Battle b, double s) { for (int i = 0; i < s / b.Tuning.StepS && b.Over == null; i++) b.Step(); }
    private static readonly string[] Scenes = { "boarding", "relief", "vip", "distress", "assault" };

    [Fact]
    public void Every_class_has_its_pads_hold_and_marines_and_a_scene_setup_overrides_them()
    {
        Battle b = Scene("concord_assault");
        CrewDef ca = b.Comms.CrewOf("ca");
        Assert.Equal(ca.Pads, b.Player.Pads.Length);
        Assert.All(b.Player.Pads, p => Assert.Equal(1, p));
        Assert.Equal(ca.Marines, b.Player.Marines);
        Assert.Equal(ca.HoldT, b.Player.HoldCap);
        Assert.Equal(2, b.ShipById("corrin")!.Marines);
        Assert.Equal(4, b.Player.Power["transport"]);
        Assert.Throws<DataError>(() => TestData.Data.Systems.CheckComms(TestData.Data.Classes.Where(kv => kv.Key != "ca").ToDictionary(kv => kv.Key, kv => kv.Value)));
    }

    [Fact]
    public void A_transport_test_scene_puts_no_weapons_on_the_players_ship()
    {
        foreach (string id in Scenes) Assert.Empty(Scene("concord_" + id).Player.Mounts);
        Assert.NotEmpty(Scene("concord_battle").Player.Mounts);
    }

    [Fact]
    public void The_transporter_reaches_an_enemy_only_inside_15_km_and_through_a_facing_that_is_down()
    {
        Battle b = Scene("concord_boarding");
        Ship me = b.Player, t = b.ShipById("vekk")!;
        Assert.Equal(15, b.Comms.Transport.RangeKm);
        Assert.True(b.BeamReach(me, t));
        t.ShieldsUp = true;
        Array.Fill(t.Shields, 20);
        Assert.False(b.BeamReach(me, t));
        t.Shields[Rules.FacingFrom(t, me.X, me.Z)] = 0;
        Assert.True(b.BeamReach(me, t));
        t.X += 20000;
        Assert.False(b.BeamReach(me, t));
    }

    [Fact]
    public void A_pad_spent_recharges_in_pad_charge_s_times_the_pads_charging_over_the_transporter_energy()
    {
        Battle b = Scene("concord_relief");
        Ship me = b.Player;
        Assert.True(b.UsePad(me));
        Assert.True(b.UsePad(me));
        double eta = b.PadEta(me, 0), want = b.Comms.Transport.PadChargeS * 2 / me.Power["transport"];
        Assert.Equal(want, eta, 6);
        Run(b, want - 0.2);
        Assert.True(me.Pads[0] < 1);
        Run(b, 0.4);
        Assert.Equal(1, me.Pads[0]);
        me.Power["transport"] = 0;
        Assert.True(b.UsePad(me));
        Assert.True(double.IsPositiveInfinity(b.PadEta(me, 0)));
    }

    [Fact]
    public void A_role_queued_leaves_the_roster_and_unqueued_returns_to_it()
    {
        Battle b = Scene("concord_boarding");
        int eng = b.Player.Team["engineers"];
        b.QueueRole("capture", "engineers");
        b.QueueRole("capture", "demo");   // demolition marines are not queued for a capture
        Assert.Equal(new[] { "engineers" }, b.Player.Queue["capture"]);
        Assert.Equal(eng - 1, b.Player.Team["engineers"]);
        b.Unqueue("capture", 0);
        Assert.Empty(b.Player.Queue["capture"]);
        Assert.Equal(eng, b.Player.Team["engineers"]);
    }

    [Fact]
    public void With_nothing_queued_the_pads_beam_marines_one_a_pad()
    {
        Battle b = Scene("concord_boarding");
        int m0 = b.Player.Marines;
        b.Lock("vekk");
        Assert.Equal(4, b.EnergizeAll("capture"));
        Assert.Equal(m0 - 4, b.Player.Marines);
        Assert.Equal(4, b.Player.Party!.Of("marines"));
        Assert.Equal("vekk", b.Player.Party.On);
        Assert.False(b.PadReady(b.Player));
        Assert.Equal("Pads charging.", (b.EnergizeAll("capture"), b.Log[^1].Text).Item2);
        Assert.NotEmpty(b.Beams);
    }

    [Fact]
    public void Marines_alone_hold_the_deck_but_it_takes_an_engineer_and_a_hacker_to_take_the_ship()
    {
        Battle b = Scene("concord_boarding");
        Ship t = b.ShipById("vekk")!;
        b.Lock("vekk");
        b.EnergizeAll("capture");
        Run(b, 60);
        Assert.Equal(0, Battle.Defenders(t));
        Assert.Equal(1, t.Side);
        Assert.Contains(b.Log, l => l.Text.Contains("deck is ours, but to take it we need an engineer, a hacker"));
        Assert.Equal(new[] { "engineers", "hackers" }, Battle.MissingRoles(b.Player.Party!));
        b.QueueRole("capture", "engineers");
        b.QueueRole("capture", "hackers");
        b.EnergizeAll("capture");
        Run(b, 1);
        Assert.Equal(0, t.Side);
        Assert.True(t.Captured);
        Assert.Null(b.Player.Party);
        // no pilot went: the prize is ours but holds still and cannot be commanded
        Assert.True(Battle.PilotNeeded(t));
        b.TakeCommand("vekk");
        Assert.NotEqual("vekk", b.Player.Id);
        Assert.Equal("won", b.Over);
    }

    [Fact]
    public void A_prize_with_no_pilot_holds_still_until_a_pilot_is_beamed_over()
    {
        // the scene without its goal and with an enemy far off, so it plays on past the capture
        Scenario sc = TestData.Data.ScenarioById("krai_boarding");
        ShipStart far = sc.Ships[1] with { Id = "far", Name = "Far", ZM = -600000, HoldFire = true, Setup = null };
        Battle b = new(TestData.Data, sc with { Goal = null, Ships = sc.Ships.Append(far).ToList() });
        Ship t = b.ShipById("meridian")!;
        b.Lock("meridian");
        b.EnergizeAll("capture");
        for (int i = 0; i < 3000 && !t.Captured; i++)
        {
            if (b.Player.Party is { } p && Battle.Defenders(t) == 0) foreach (string r in Battle.MissingRoles(p)) if (!b.Player.Queue["capture"].Contains(r)) b.QueueRole("capture", r);
            if (b.PadReady(b.Player) && b.Player.Queue["capture"].Count > 0) b.EnergizeAll("capture");
            b.Step();
        }
        Assert.True(t.Captured);
        Assert.True(Battle.PilotNeeded(t));
        Run(b, 3);
        Assert.Equal(0, t.VOrder);
        b.Lock("meridian");
        b.QueueRole("capture", "pilots");
        for (int i = 0; i < 600 && b.Player.Queue["capture"].Count > 0; i++) { b.EnergizeAll("capture"); b.Step(); }
        Assert.False(Battle.PilotNeeded(t));
        Run(b, 2);
        Assert.Contains(b.Log, l => l.Text == "Pilot aboard the Meridian: under way.");
        b.TakeCommand("meridian");
        Assert.Equal("meridian", b.Player.Id);
    }

    [Fact]
    public void The_fight_aboard_comes_in_volleys_every_volley_s_and_medics_treat_the_wounded()
    {
        Battle b = Scene("concord_boarding");
        Ship t = b.ShipById("vekk")!;
        b.Lock("vekk");
        foreach (string r in new[] { "marines", "marines", "marines", "medics" }) b.QueueRole("capture", r);
        b.EnergizeAll("capture");
        Party p = b.Player.Party!;
        Assert.Equal(b.Comms.Board.VolleyS, p.VolleyT);
        Run(b, b.Comms.Board.VolleyS - 0.5);
        Assert.Null(p.Last);
        Run(b, 1);
        Assert.NotNull(p.Last);
        Assert.Contains(b.Log, l => l.Who == "Boarding" && l.Text.StartsWith("Volley aboard Vekk"));
    }

    [Fact]
    public void A_bombing_raid_with_a_demolition_marine_alive_takes_bomb_dmg_off_the_subsystem()
    {
        Battle b = Scene("concord_boarding");
        Ship t = b.ShipById("vekk")!;
        b.Lock("vekk");
        b.AimRaid("sensors");
        b.RaidKind("bomb");
        foreach (string r in new[] { "marines", "marines", "demo" }) b.QueueRole("hit", r);
        b.EnergizeAll("hit");
        Assert.NotNull(b.Player.Raid);
        Assert.Contains("'s sensors", b.Log[^1].Text);
        Run(b, b.Comms.Board.RaidS + 0.2);
        Assert.Null(b.Player.Raid);
        double s = t.Sys["sensors"];
        Assert.True(s == 1 - b.Comms.Board.BombDmg || s == 1, $"sensors {s}");
        Assert.Contains(b.Log, l => l.Text.StartsWith("Raid on Vekk"));
    }

    [Fact]
    public void A_raid_picked_on_one_weapon_damages_only_that_mount()
    {
        Battle b = Scene("krai_boarding");
        Ship t = b.ShipById("meridian")!;
        b.Lock("meridian");
        b.AimRaid("weapons", 0);
        foreach (string r in new[] { "marines", "marines" }) b.QueueRole("hit", r);
        b.EnergizeAll("hit");
        Assert.Equal(0, b.Player.Raid!.Mount);
        double[] before = t.Mounts.Select(m => m.Hp).ToArray();
        double sys = t.Sys["weapons"];
        Run(b, b.Comms.Board.RaidS + 0.2);
        Assert.True(t.Mounts[0].Hp < before[0]);
        Assert.Equal(before.Skip(1), t.Mounts.Skip(1).Select(m => m.Hp));
        Assert.Equal(sys, t.Sys["weapons"]);
    }

    [Fact]
    public void A_raid_and_a_boarding_party_are_never_out_at_once()
    {
        Battle b = Scene("concord_boarding");
        b.Lock("vekk");
        b.QueueRole("hit", "marines");
        b.Energize(0, "hit");
        b.QueueRole("capture", "marines");
        b.Energize(1, "capture");
        Assert.Equal("A raid is out.", b.Log[^1].Text);
        Assert.Null(b.Player.Party);
    }

    [Fact]
    public void The_inventory_transporter_moves_per_beam_units_and_a_planet_takes_only_what_it_asks_for()
    {
        Battle b = Scene("concord_relief");
        Ship me = b.Player, harbor = b.ShipById("harbor")!;
        b.Transfer("rations", -1);
        Assert.Equal("No destination: click a friendly ship, station or planet.", b.Log[^1].Text);
        b.Lock("harbor");
        b.Transfer("rations", -1);
        Assert.Equal(b.Comms.Inventory.PerBeam, me.Hold["rations"]);
        Assert.Equal(20 - b.Comms.Inventory.PerBeam, harbor.Hold["rations"]);
        b.SelectPlanet("tessaly");
        Assert.Null(me.Target);
        Assert.Equal("tessaly", b.NavTarget(me)!.Id);
        b.Transfer("rations", 1);
        Assert.StartsWith("Out of transporter range: Tessaly", b.Log[^1].Text);
    }

    [Fact]
    public void A_ship_in_distress_given_its_parts_repairs_its_engines_and_gets_under_way()
    {
        Battle b = Scene("concord_distress");
        Ship lark = b.ShipById("lark")!;
        b.Lock("lark");
        b.SetSpeed(3000);
        b.HelmOrder("intercept");
        for (int i = 0; i < 6000 && b.Over == null; i++)
        {
            b.Step();
            if (Rules.DistKm(b.Player, lark) < 14) b.Transfer("parts", 1);
        }
        Assert.Equal("won", b.Over);
        Assert.Equal(10, lark.Got["parts"]);
        Assert.Equal(1, lark.Sys["engines"]);
        Assert.False(lark.Distress);
        Assert.Contains(b.Log, l => l.Text == "Lark: engines repaired, getting under way.");
    }

    // ---- every scene played to its goal, from both sides, by the orders a captain gives -------------------------------

    private static void Relief(string side)
    {
        Battle b = Scene(side + "_relief");
        string station = side switch { "concord" => "harbor", "krai" => "ironmaw", _ => "lyceum" };
        b.Lock(station);
        b.Transfer("rations", -1); b.Transfer("rations", -1); b.Transfer("medicine", -1);
        b.SelectPlanet("tessaly");
        b.SetSpeed(3000);
        b.HelmOrder("intercept");
        Planet pl = b.Planets[0];
        for (int i = 0; i < 9000 && b.Over == null; i++)
        {
            b.Step();
            if (Battle.PlanetKm(b.Player, pl) <= b.Comms.Transport.RangeKm) { b.Transfer("rations", 1); b.Transfer("medicine", 1); }
        }
        Assert.Equal("won", b.Over);
        Assert.True(Battle.NeedsMet(pl));
        Assert.Contains(b.Beams, e => e.BBig);
    }

    [Fact] public void Relief_is_won_from_the_Concord_side() => Relief("concord");
    [Fact] public void Relief_is_won_from_the_Krai_side() => Relief("krai");
    [Fact] public void Relief_is_won_from_the_Kelvane_side() => Relief("kelvane");

    private static void Vip(string side)
    {
        Battle b = Scene(side + "_vip");
        string troops = side switch { "concord" => "corrin", "krai" => "ruhn", _ => "candour" }, outpost = side == "krai" ? "calder" : "varn";
        Ship t = b.ShipById(troops)!;
        Run(b, 5);
        Assert.Equal(0, t.V, 3);   // it waits for the envoy
        b.Lock(troops);
        b.Transfer("vip", 1);
        Assert.NotNull(t.Vip);
        Run(b, 300);
        Assert.Equal("won", b.Over);
        Ship o = b.ShipById(outpost)!;
        Assert.Equal(0, o.Side);
        Assert.NotNull(o.Vip);
    }

    [Fact] public void VIP_is_won_from_the_Concord_side() => Vip("concord");
    [Fact] public void VIP_is_won_from_the_Krai_side() => Vip("krai");
    [Fact] public void VIP_is_won_from_the_Kelvane_side() => Vip("kelvane");

    private static void Assault(string side)
    {
        Battle b = Scene(side + "_assault");
        string troops = side switch { "concord" => "corrin", "krai" => "ruhn", _ => "candour" };
        Ship t = b.ShipById(troops)!;
        b.Lock(troops);
        for (int i = 0; i < 4000 && t.Marines < t.NeedMarines; i++) { if (b.PadReady(b.Player)) b.EnergizeAll("capture"); b.Step(); }
        Assert.Equal(10, t.Marines);
        Assert.Contains(b.Log, l => l.Text.Contains("marines aboard)"));
        Run(b, 300);
        Assert.Equal("won", b.Over);
    }

    [Fact] public void Assault_is_won_from_the_Concord_side() => Assault("concord");
    [Fact] public void Assault_is_won_from_the_Krai_side() => Assault("krai");
    [Fact] public void Assault_is_won_from_the_Kelvane_side() => Assault("kelvane");

    private static void Boarding(string side)
    {
        Battle b = Scene(side + "_boarding");
        b.NextTarget();
        foreach (string r in new[] { "marines", "marines", "marines", "marines", "marines", "engineers", "hackers", "pilots" }) b.QueueRole("capture", r);
        Ship t = b.Ships[1];
        for (int i = 0; i < 6000 && b.Over == null; i++)
        {
            // the deck held, the roles still missing are queued again (the panel rings them in Caution)
            if (b.Player.Party is { } p && Battle.Defenders(t) == 0) foreach (string r in Battle.MissingRoles(p)) if (!b.Player.Queue["capture"].Contains(r)) b.QueueRole("capture", r);
            if (b.PadReady(b.Player) && b.Player.Queue["capture"].Count > 0) b.EnergizeAll("capture");
            b.Step();
        }
        Assert.Equal("won", b.Over);
        Assert.True(t.Captured);
        Assert.Equal(b.Player.Side, t.Side);
    }

    [Fact] public void Boarding_is_won_from_the_Concord_side() => Boarding("concord");
    [Fact] public void Boarding_is_won_from_the_Krai_side() => Boarding("krai");
    [Fact] public void Boarding_is_won_from_the_Kelvane_side() => Boarding("kelvane");

    [Fact]
    public void Every_comms_scene_runs_a_minute_from_both_sides_and_replays_from_its_seed()
    {
        foreach (string id in Scenes)
            foreach (string side in new[] { "concord", "krai", "kelvane" })
            {
                Battle a = Scene($"{side}_{id}"), c = Scene($"{side}_{id}");
                Run(a, 60); Run(c, 60);
                Assert.True(a.Over is null or "won", $"{side}_{id}: {a.Over}");
                Assert.Equal(a.Ships.Select(s => (s.X, s.Hull, s.Side, s.Marines)), c.Ships.Select(s => (s.X, s.Hull, s.Side, s.Marines)));
            }
    }
}
