// Comms (Battle.Comms, data/battle/systems/comms.json): hails answered by the faction's line or not at all, a friendly
// ship saying what it needs, fleet orders, refusals marked as warnings, and the transporter's reach said once each way.
using System.Linq;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class CommsTests
{
    private static Battle Scene(string id) => new(TestData.Data, TestData.Data.ScenarioById(id));
    private static void Run(Battle b, double s) { for (int i = 0; i < s / b.Tuning.StepS; i++) b.Step(); }

    [Fact]
    public void Hailing_a_Krai_ship_gets_the_Warhost_refusal_from_its_own_name()
    {
        Battle b = Scene("concord_boarding");
        b.Lock("vekk");
        b.Hail();
        Assert.Equal("Vekk", b.Log[^1].Who);
        Assert.Equal(b.Comms.Hail.EnemyReply["krai"], b.Log[^1].Text);
    }

    [Fact]
    public void Most_enemies_do_not_answer_a_hail()
    {
        // the Concord has no reply to an enemy, nor does the Ssarith Hunt (owner, 2026-11-06: "most enemies wont respond")
        Assert.False(TestData.Data.Systems.Comms.Hail.EnemyReply.ContainsKey("concord"));
        Assert.False(TestData.Data.Systems.Comms.Hail.EnemyReply.ContainsKey("ssarith"));
        Battle b = Scene("krai_boarding");
        b.Lock("meridian");
        b.Hail();
        Assert.Equal("Comms", b.Log[^1].Who);
        Assert.Equal($"Meridian: {b.Comms.Hail.NoAnswer}", b.Log[^1].Text);
    }

    [Fact]
    public void A_friendly_ship_in_distress_answers_with_what_it_still_needs()
    {
        Battle b = Scene("concord_distress");
        b.Lock("lark");
        b.Hail();
        Assert.Equal("Lark", b.Log[^1].Who);
        Assert.Contains("10 spare parts", b.Log[^1].Text);
    }

    [Fact]
    public void A_friendly_ship_wanting_troops_names_the_marines_it_lacks()
    {
        Battle b = Scene("concord_assault");
        Assert.Equal("8 marines", Battle.NeedsText(b.ShipById("corrin")!));
    }

    [Fact]
    public void A_hail_with_nothing_locked_is_refused_and_marked_as_a_warning()
    {
        Battle b = Scene("concord_relief");
        b.Hail();
        Assert.True(Battle.IsWarning(b.Log[^1]));
        Assert.False(Battle.IsWarning(new LogLine(0, "Transport", "Energizing: 4 marines to Vekk.")));
        Assert.True(Battle.IsWarning(new LogLine(0, "Transport", "Pads charging.")));
    }

    [Fact]
    public void Fleet_orders_set_every_wingmans_stance_and_leave_the_commanded_ship()
    {
        Battle b = Scene("concord_battle");
        b.OrderFleet(Stance.Hold);
        Assert.All(b.Ships.Where(s => s.Side == 0 && s != b.Player), s => Assert.Equal(Stance.Hold, s.Stance));
        Assert.Equal(Stance.Attack, b.Player.Stance);
        Assert.Equal("Fleet: Hold.", b.Log[^1].Text);
    }

    [Fact]
    public void Crossing_the_transporter_reach_of_the_destination_is_said_once_each_way()
    {
        Battle b = Scene("concord_distress");
        b.Lock("lark");
        b.SetSpeed(300);
        b.HelmOrder("intercept");
        for (int i = 0; i < 4000 && Rules.DistKm(b.Player, b.ShipById("lark")!) > 1.2; i++) b.Step();
        b.AllStop();
        Run(b, 20);
        Assert.Single(b.Log, l => l.Text == "Lark in transporter range: transporters active.");
        Assert.DoesNotContain(b.Log, l => l.Text == "Lark out of transporter range.");
    }
}
