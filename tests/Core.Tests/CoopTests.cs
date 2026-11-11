// CoopTests: co-op's lockstep, crew roles, independent targets and ships given to players (openspec/changes/bridge-crew-coop),
// a host and a crew peer joined in memory with no network, each stepping its own copy of the same battle.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FleetCommand.Coop;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public class CoopTests
{
    private static Roles R => TestData.Data.CoopRoles!;

    // a host and one crew member, each with its own battle of the scenario, wired by queues (what the network would carry)
    private sealed class Pair
    {
        public readonly Lockstep Host, Guest;
        public readonly Battle HB, GB;
        private readonly Queue<string> _toGuest = new(), _toHost = new();

        public Pair(Scenario sc)
        {
            Host = new Lockstep(new Crew(R, "Admiral"), host: true) { Send = m => _toGuest.Enqueue(m.ToJsonString()) };
            Guest = new Lockstep(new Crew(R, "?"), host: false) { Send = m => _toHost.Enqueue(m.ToJsonString()) };
            int id = Host.HostJoin("Ensign");
            Guest.Receive(new JsonObject { ["t"] = "welcome", ["you"] = id });
            Flush();
            HB = new Battle(TestData.Data, sc); GB = new Battle(TestData.Data, sc);
            Host.Begin(HB); Guest.Begin(GB);
        }

        public void Flush()
        {
            while (_toHost.Count > 0) Host.HostReceive(Guest.Me, JsonNode.Parse(_toHost.Dequeue())!.AsObject());
            while (_toGuest.Count > 0) Guest.Receive(JsonNode.Parse(_toGuest.Dequeue())!.AsObject());
        }

        /// <summary>n steps: the crew's waiting orders reach the host, the host ticks, the crew plays the ticks.</summary>
        public void Steps(int n)
        {
            for (int i = 0; i < n; i++) { Flush(); Host.HostTick(true); Flush(); Guest.Play(int.MaxValue); }
        }
    }

    private static Scenario Sc(string id) => TestData.Data.ScenarioById(id);

    [Fact]
    public void Every_order_the_battle_relays_is_listed_and_every_listed_order_relays()
    {
        string sim = Path.Combine(Path.GetDirectoryName(TestData.DataDir)!, "src", "Core", "Sim");
        var sent = Directory.GetFiles(sim, "Battle*.cs").SelectMany(f => Regex.Matches(File.ReadAllText(f), @"Sent\(nameof\((\w+)\)").Select(m => m.Groups[1].Value)).ToHashSet();
        Assert.Equal(sent.OrderBy(x => x), Battle.RelayedOrders.OrderBy(x => x));
    }

    [Fact]
    public void Orders_and_tabs_belong_to_their_roles()
    {
        Assert.Equal("helm", R.RoleOfOrder("Turn"));
        Assert.Equal("weapons", R.RoleOfOrder("Fire"));
        Assert.Equal("tractor", R.RoleOfTab("sh.tractor"));
    }

    [Fact]
    public void The_captain_opens_every_tab_and_a_new_member_none_until_given_a_role()
    {
        var c = new Crew(R, "A");
        CrewMember b = c.Join("B");
        Assert.True(c.OpensTab(Crew.AdmiralId, "st.helm"));
        Assert.False(c.OpensTab(b.Id, "st.helm"));
        Assert.Null(c.GiveRole(Crew.AdmiralId, "helm", b.Id));
        Assert.True(c.OpensTab(b.Id, "st.helm"));
        Assert.False(c.OpensTab(b.Id, "st.tactical"));
        Assert.True(c.OpensTab(Crew.AdmiralId, "st.helm"));
        Assert.NotNull(c.GiveRole(b.Id, "weapons", b.Id));   // only the captain assigns
    }

    [Fact]
    public void Two_peers_step_the_same_battle_from_the_same_orders()
    {
        var p = new Pair(Sc("concord_battle"));
        p.Host.AskCrew(new JsonObject { ["op"] = "role", ["role"] = "weapons", ["to"] = p.Guest.Me });
        p.Steps(1);
        for (int i = 0; i < 20; i++)
        {
            p.GB.NextTarget();          // the crew's weapons officer locks and fires
            p.GB.Fire();
            p.HB.HelmOrder("intercept"); // the admiral at the helm
            p.Steps(45);
        }
        Assert.Equal(p.Host.Sum(), p.Guest.Sum());
        Assert.True(p.Guest.Synced > 20);
        Assert.Equal(0, p.Guest.Desyncs);
    }

    [Fact]
    public void An_order_from_a_station_not_held_changes_nothing()
    {
        var p = new Pair(Sc("concord_battle"));
        double h = p.HB.Player.HOrder;
        p.GB.Turn(45);
        p.Steps(1);
        Assert.Equal(h, p.HB.Player.HOrder);
        Assert.Equal(p.Host.Sum(), p.Guest.Sum());
    }

    [Fact]
    public void Weapons_and_science_keep_their_own_targets_and_the_captain_steps_in_from_a_tab()
    {
        var p = new Pair(Sc("concord_battle"));
        p.Host.AskCrew(new JsonObject { ["op"] = "role", ["role"] = "science", ["to"] = p.Guest.Me });
        p.Steps(1);
        Ship[] foes = p.HB.EnemiesOf(p.HB.Player).OrderBy(s => s.Order).ToArray();
        Assert.True(foes.Length >= 2);
        p.HB.Lock(foes[0].Id);          // the admiral holds weapons, helm, tractor, transport
        p.GB.Lock(foes[1].Id);          // science locks another ship
        p.Steps(1);
        Ship me = p.HB.Player;
        Assert.Equal(foes[0].Id, p.HB.TargetOf(me, "weapons")?.Id);
        Assert.Equal(foes[0].Id, p.HB.TargetOf(me, "helm")?.Id);
        Assert.Equal(foes[1].Id, p.HB.TargetOf(me, "science")?.Id);
        Assert.Equal(foes[1].Id, p.GB.TargetOf(p.GB.Player, "science")?.Id);
        // the admiral, from the Science tab, retargets science alone
        p.Host.OpenTab = () => "st.science";
        p.HB.Lock(foes[0].Id);
        p.Steps(1);
        Assert.Equal(foes[0].Id, p.HB.TargetOf(p.HB.Player, "science")?.Id);
        Assert.Equal(p.Host.Sum(), p.Guest.Sum());
    }

    [Fact]
    public void A_ship_given_to_a_member_answers_to_them_and_not_to_the_ai()
    {
        var p = new Pair(Sc("concord_battle"));
        Ship wing = p.HB.Fleet().First(s => s != p.HB.Player);
        p.Host.AskCrew(new JsonObject { ["op"] = "ship", ["ship"] = wing.Id, ["to"] = p.Guest.Me });
        p.Steps(1);
        Assert.Contains(wing.Id, p.HB.Helmed);
        Assert.Equal(wing.Id, p.GB.Player.Id);          // the member's console shows their own ship
        Assert.NotEqual(wing.Id, p.HB.Player.Id);        // the admiral's shows the flagship
        p.GB.AllStop();
        p.GB.Turn(90);
        p.Steps(90);
        Ship w = p.HB.ShipById(wing.Id)!;
        Assert.Equal(0, w.VOrder);
        Assert.Equal(p.Host.Sum(), p.Guest.Sum());
        Assert.Equal(0, p.Guest.Desyncs);
    }

    [Fact]
    public void A_battle_with_no_crew_steps_exactly_as_in_single_play()
    {
        Battle solo = new(TestData.Data, Sc("concord_battle"));
        var host = new Lockstep(new Crew(R, "A"), host: true);
        Battle shared = new(TestData.Data, Sc("concord_battle"));
        host.Begin(shared);
        for (int i = 0; i < 600; i++)
        {
            if (i % 90 == 0) { solo.NextTarget(); solo.HelmOrder("intercept"); shared.NextTarget(); shared.HelmOrder("intercept"); }
            if (i % 15 == 0) { solo.Fire(); shared.Fire(); }
            solo.Step();
            host.HostTick(true);
        }
        Assert.Equal(solo.Player.X, shared.Player.X);
        Assert.Equal(solo.Player.Hull, shared.Player.Hull);
        Assert.Equal(solo.Ships.Count(s => s.Alive), shared.Ships.Count(s => s.Alive));
    }
}
