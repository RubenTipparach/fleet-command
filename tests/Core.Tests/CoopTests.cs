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
    public void Chat_pings_and_directives_reach_every_peer_in_the_senders_name()
    {
        var p = new Pair(Sc("concord_battle"));
        Ship wing = p.HB.Fleet().First(s => s != p.HB.Player);
        p.Host.AskCrew(new JsonObject { ["op"] = "ship", ["ship"] = wing.Id, ["to"] = p.Guest.Me });
        p.Steps(1);
        p.Guest.Chat("On station.");
        p.Guest.Ping(1000, -2000, null);
        p.Flush();
        p.HB.Lock(p.HB.EnemiesOf(p.HB.Player).First().Id);
        p.Steps(1);
        p.Host.Direct(p.Guest.Me, "attack");
        p.Flush();
        Assert.Equal("attack", p.Guest.DirectiveFor(p.Guest.Me)?.Order);
        p.Guest.Ack();
        p.Flush();
        foreach (Lockstep side in new[] { p.Host, p.Guest })
        {
            Assert.Contains(side.Events, e => e.Kind == "chat" && e.By == p.Guest.Me && e.Text == "On station.");
            Assert.Contains(side.Events, e => e.Kind == "ping" && e.X == 1000 && e.Z == -2000);
            Assert.Contains(side.Events, e => e.Kind == "ack" && e.By == p.Guest.Me);
            Assert.Null(side.DirectiveFor(p.Guest.Me));
        }
        Assert.Contains(p.HB.Log, l => l.By == "Admiral" && l.Text.StartsWith("Target locked"));
        Assert.Equal(p.Host.Sum(), p.Guest.Sum());
    }

    [Fact]
    public void A_member_moves_to_another_bridge_and_locks_for_the_stations_they_pick()
    {
        var p = new Pair(Sc("concord_battle"));
        int third = p.Host.HostJoin("Third");
        p.Flush();
        Ship wing = p.HB.Fleet().First(s => s != p.HB.Player);
        // the guest captains the wingman; the third member moves to that bridge and is given its science
        p.Host.AskCrew(new JsonObject { ["op"] = "ship", ["ship"] = wing.Id, ["to"] = p.Guest.Me });
        p.Steps(1);
        Assert.Null(p.Host.Crew.MoveTo(third, wing.Id));
        Assert.Equal(wing.Id, p.Host.Crew.BridgeOf(third));
        Assert.NotNull(p.Host.Crew.MoveTo(p.Guest.Me, Crew.Flag));   // a captain stays on their own bridge
        // the admiral, holding every station of the flagship, locks for Science alone
        Ship[] foes = p.HB.EnemiesOf(p.HB.Player).OrderBy(s => s.Order).ToArray();
        p.HB.Lock(foes[0].Id);
        p.Steps(1);
        p.Host.LockFor = () => new[] { "science" };
        p.HB.Lock(foes[1].Id);
        p.Steps(1);
        Ship flag = p.Host.Flag!;
        Assert.Equal(foes[1].Id, p.HB.TargetOf(flag, "science")?.Id);
        Assert.Equal(foes[0].Id, p.HB.TargetOf(flag, "weapons")?.Id);
        Assert.Equal(p.Host.Sum(), p.Guest.Sum());
    }

    [Fact]
    public void A_member_alone_on_a_ship_holds_every_role_and_the_admiral_works_any_ship()
    {
        var p = new Pair(Sc("concord_battle"));
        Ship[] wings = p.HB.Fleet().Where(s => s != p.HB.Player).ToArray();
        p.Host.AskCrew(new JsonObject { ["op"] = "ship", ["ship"] = wings[0].Id, ["to"] = p.Guest.Me });
        p.Steps(1);
        // the one player on the wingman holds every role there
        foreach (RoleDef r in R.List) Assert.Equal(p.Guest.Me, p.Host.Crew.HolderOf(wings[0].Id, r.Id));
        // the admiral opens every role and every tab wherever they stand, and steps onto an AI wingman to fly it
        Assert.All(R.List, r => Assert.True(p.Host.Crew.Opens(Crew.AdmiralId, r.Id)));
        p.Host.AskCrew(new JsonObject { ["op"] = "bridge", ["bridge"] = wings[1].Id });
        p.Steps(1);
        Assert.Contains(wings[1].Id, p.HB.Helmed);
        Assert.Equal(wings[1].Id, p.HB.Player.Id);          // the admiral's console is that ship's
        Assert.Equal(p.Host.Sum(), p.Guest.Sum());
        p.HB.AllStop();
        p.Steps(1);
        Assert.Equal(p.Host.Sum(), p.Guest.Sum());
        p.Steps(29);
        Assert.Equal(0, p.HB.ShipById(wings[1].Id)!.VOrder);
        // and onto the crew member's ship, where the admiral's orders are obeyed too
        p.Host.AskCrew(new JsonObject { ["op"] = "bridge", ["bridge"] = wings[0].Id });
        p.Steps(1);
        Assert.DoesNotContain(wings[1].Id, p.HB.Helmed);    // the AI has the first wingman back
        double h = p.HB.ShipById(wings[0].Id)!.HOrder;
        p.HB.Turn(45);
        p.Steps(1);
        Assert.NotEqual(h, p.HB.ShipById(wings[0].Id)!.HOrder);
        Assert.Equal(p.Host.Sum(), p.Guest.Sum());
        Assert.Equal(0, p.Guest.Desyncs);
    }

    [Fact]
    public void The_admiral_assigns_crew_to_ships_as_captains_or_as_crew()
    {
        var p = new Pair(Sc("concord_battle"));
        int third = p.Host.HostJoin("Third");
        p.Flush();
        Ship wing = p.HB.Fleet().First(s => s != p.HB.Player);
        // to an AI ship: its captain, every role theirs, the AI off it
        p.Host.AskCrew(new JsonObject { ["op"] = "assign", ["member"] = p.Guest.Me, ["bridge"] = wing.Id });
        // to that ship again: its crew under that captain
        p.Host.AskCrew(new JsonObject { ["op"] = "assign", ["member"] = third, ["bridge"] = wing.Id });
        p.Steps(1);
        Assert.Equal(p.Guest.Me, p.Host.Crew.CaptainOf(wing.Id));
        Assert.Equal(wing.Id, p.Host.Crew.BridgeOf(third));
        Assert.Contains(wing.Id, p.HB.Helmed);
        // back to the flagship: the ship goes back to the AI
        p.Host.AskCrew(new JsonObject { ["op"] = "assign", ["member"] = p.Guest.Me, ["bridge"] = Crew.Flag });
        p.Steps(1);
        Assert.Equal(Crew.Flag, p.Host.Crew.BridgeOf(p.Guest.Me));
        Assert.DoesNotContain(wing.Id, p.HB.Helmed);
        // only the admiral assigns
        Assert.NotNull(p.Host.Crew.Assign(p.Guest.Me, third, Crew.Flag, false));
        Assert.Equal(p.Host.Sum(), p.Guest.Sum());
    }

    [Fact]
    public void A_game_takes_eight_players_and_no_more()
    {
        var host = new Lockstep(new Crew(R, "Admiral"), host: true);
        for (int i = 2; i <= Lockstep.MaxPlayers; i++) Assert.Equal(i, host.HostJoin($"P{i}"));
        Assert.Equal(0, host.HostJoin("Ninth"));
        Assert.Equal(Lockstep.MaxPlayers, host.Crew.Members.Count);
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

public class DMathTests
{
    [Fact]
    public void The_simulation_calls_no_platform_transcendental_function()
    {
        string core = Path.Combine(Path.GetDirectoryName(TestData.DataDir)!, "src", "Core");
        var bad = Directory.GetFiles(core, "*.cs", SearchOption.AllDirectories).Where(f => !f.EndsWith("DMath.cs"))
            .SelectMany(f => File.ReadAllLines(f).Select((l, i) => (f, i, l)))
            .Where(x => Regex.IsMatch(x.l, @"(?<![\w.])Math\.(Sin|Cos|Tan|Atan2|Atan|Asin|Acos|Exp|Log|Log2|Log10|Pow|Cbrt|Sinh|Cosh|Tanh)\(|MathF\."))
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}").ToList();
        Assert.Empty(bad);
    }

    [Fact]
    public void Deterministic_functions_agree_with_the_platform_to_a_few_units_in_the_last_place()
    {
        var rng = new Rng(7);
        for (int i = 0; i < 20000; i++)
        {
            double x = (rng.Next() - 0.5) * 80, y = (rng.Next() - 0.5) * 80, p = rng.Next() * 30 + 1e-9;
            Assert.Equal(System.Math.Sin(x), DMath.Sin(x), 1e-14);
            Assert.Equal(System.Math.Cos(x), DMath.Cos(x), 1e-14);
            Assert.Equal(System.Math.Atan2(y, x), DMath.Atan2(y, x), 1e-14);
            Assert.Equal(System.Math.Atan(x), DMath.Atan(x), 1e-14);
            Assert.Equal(System.Math.Log(p), DMath.Log(p), 1e-13);
            Assert.Equal(System.Math.Exp(x / 4), DMath.Exp(x / 4), System.Math.Abs(System.Math.Exp(x / 4)) * 1e-14);
        }
        Assert.Equal(0.125, DMath.Pow(0.5, 3));
    }
}
