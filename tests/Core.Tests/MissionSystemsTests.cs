// Mission systems tests (openspec/changes/mission-systems): the sensors' reach and unknown contacts, planet surveys,
// installations and away teams, the envoy and the one waiting on a planet, late arrivals, sleepers, truces, the time limit,
// dead engines; then the campaign's side: what a friendly port gives by standing, and hostile factions' hunters.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Campaign;
using FleetCommand.Data;
using FleetCommand.Sim;
using Xunit;

namespace FleetCommand.Tests;

public sealed class MissionSystemsTests
{
    private static CampaignData Camp => CampaignTests.Camp;

    private static void Run(Battle b, double s) { for (int i = 0; i < Math.Round(s / b.Tuning.StepS) && b.Over == null; i++) b.Step(); }

    // a mission offered on a new campaign, planned into its battle
    private static (Campaign.Campaign C, Offer M, MissionPlan P, Battle B) Fly(string id, string side = "concord", Dictionary<string, object>? data = null)
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, side);
        c.S.Missions.Clear();
        Offer? m = null;
        for (int t = 0; t < 6 && m == null; t++) { m = c.OfferMission(id, data: data); if (m == null) c.EndTurn(); }
        Assert.NotNull(m);
        c.S.Fleet[0].Supplies["marines"] = 4;
        MissionPlan plan = MissionRun.Plan(c, m!, TestData.Data)!;
        return (c, m!, plan, new Battle(TestData.Data, plan.Scenario));
    }

    [Fact]
    public void Objectives_asked_of_a_battle_that_is_not_the_missions_are_all_unmet()
    {
        // the goals readout asks while the mission's battle loads, with the last battle still standing (it threw before)
        var (c, _, plan, _) = Fly("away_team");
        var before = new Battle(TestData.Data, TestData.Data.ScenarioById("concord_battle"));
        var met = MissionRun.MetNow(c, plan, before);
        Assert.Equal(plan.Battle.Objectives.Select(o => o.Id).OrderBy(x => x), met.Keys.OrderBy(x => x));
        Assert.All(met.Values, Assert.False);
    }

    // the commanded ship parked beside a planet, inside the transporter's reach of its surface
    private static void Beside(Battle b, Planet p)
    {
        b.Player.X = p.X; b.Player.Z = p.Z + p.RadiusM + 1000; b.Player.V = 0; b.Player.VOrder = 0;
    }

    [Fact]
    public void A_contact_beyond_every_friendly_sensor_reach_is_unknown_until_it_comes_within_reach()
    {
        var (_, _, plan, b) = Fly("patrol");
        Ship e = b.ShipById(plan.Groups["raiders"][0])!;
        double reach = b.SensorKm(b.Player);
        Assert.True(reach > 0);
        e.X = b.Player.X; e.Z = b.Player.Z - (reach + 20) * 1000;
        foreach (Ship o in b.Ships.Where(o => o.Side == 0 && o != b.Player)) { o.X = b.Player.X; o.Z = b.Player.Z; }
        Assert.False(b.Identified(e));
        e.Z = b.Player.Z - (reach - 5) * 1000;
        Assert.True(b.Identified(e));
    }

    [Fact]
    public void More_energy_in_sensors_reaches_further_and_passive_sensors_less()
    {
        var (_, _, _, b) = Fly("patrol");
        Ship p = b.Player;
        double was = b.SensorKm(p);
        p.Power["sensors"] += 2;
        Assert.True(b.SensorKm(p) > was);
        p.Passive = true;
        Assert.True(b.SensorKm(p) < (was + 2 * TestData.Data.Systems.Field.Sensors.PerEKm));
    }

    [Fact]
    public void A_survey_finds_a_planets_sites_and_the_survey_mission_is_met()
    {
        var (c, m, plan, b) = Fly("survey");
        Planet pl = b.Planets.Single();
        Assert.All(pl.Installations, i => Assert.False(i.Known));
        b.SelectPlanet(pl.Id);
        b.ScanPlanet();
        Beside(b, pl);
        Run(b, 120);
        Assert.True(pl.Scanned);
        Assert.All(pl.Installations, i => Assert.True(i.Known));
        Assert.True(MissionRun.MetNow(c, plan, b)["survey"]);
    }

    [Fact]
    public void An_away_team_holds_a_quiet_site_and_beams_back_up()
    {
        var (c, m, plan, b) = Fly("away_team");
        Planet pl = b.Planets.Single();
        foreach (Installation i in pl.Installations) i.Known = true;
        Beside(b, pl);
        int marines = b.Player.Marines;
        Assert.Null(b.CannotBeamDown(pl.Id, "ruin"));
        Assert.True(b.BeamDown(pl.Id, "ruin"));
        Assert.True(b.Player.Marines < marines);
        Assert.Single(b.AwayTeams);
        Run(b, TestData.Data.Systems.Field.Away.AwayS + 20);
        Assert.True(pl.Installations.First(i => i.Id == "ruin").Secured);
        Assert.Empty(b.AwayTeams);
        Assert.Equal(marines, b.Player.Marines);
        var met = MissionRun.MetNow(c, plan, b);
        Assert.True(met["ruin"]);
    }

    [Fact]
    public void An_away_team_cannot_go_to_a_site_not_yet_found_or_out_of_reach()
    {
        var (_, _, _, b) = Fly("away_team");
        Planet pl = b.Planets.Single();
        Assert.Equal("Scan the planet first.", b.CannotBeamDown(pl.Id, "ruin"));
        pl.Installations[0].Known = true;
        b.Player.X = pl.X; b.Player.Z = pl.Z + pl.RadiusM + 200_000;
        Assert.Contains("out of transporter range", b.CannotBeamDown(pl.Id, pl.Installations[0].Id));
    }

    [Fact]
    public void A_garrison_bigger_than_the_team_drives_it_back()
    {
        var (_, _, _, b) = Fly("sabotage", data: null);
        Planet pl = b.Planets.Single();
        Installation depot = pl.Installations.Single();
        depot.Known = true;
        depot.Garrison = 30;
        Beside(b, pl);
        Assert.True(b.BeamDown(pl.Id, depot.Id));
        Run(b, TestData.Data.Systems.Field.Away.AwayS + 5);
        Assert.False(depot.Secured);
        Assert.True(depot.Failed);
    }

    [Fact]
    public void Relief_beamed_down_meets_the_relief_mission()
    {
        var (c, _, plan, b) = Fly("relief");
        Planet pl = b.Planets.Single();
        Assert.True(b.Player.Hold["medicine"] > 0);
        b.SelectPlanet(pl.Id);
        Beside(b, pl);
        b.Player.Power["transport"] = 2;   // the pads recharge only on transporter energy
        for (int k = 0; k < 20 && !pl.Relieved; k++)
        {
            foreach (string item in new[] { "medicine", "rations" }) b.Transfer(item, 1);
            Run(b, 20);
        }
        Assert.True(pl.Relieved, $"{b.Over} {b.T}: " + string.Join(" | ", b.Log.TakeLast(8).Select(l => l.ToString())));
        Assert.True(MissionRun.MetNow(c, plan, b)["relief"]);
    }

    [Fact]
    public void The_envoy_lands_on_a_planet_that_receives_one_and_a_waiting_scientist_comes_up()
    {
        var (c, _, plan, b) = Fly("diplomat_run");
        Planet pl = b.Planets.Single();
        Assert.NotNull(b.Player.Vip);
        b.SelectPlanet(pl.Id);
        Beside(b, pl);
        b.Transfer("vip", 1);
        Assert.NotNull(pl.EnvoyLanded);
        Assert.Null(b.Player.Vip);
        var met = MissionRun.MetNow(c, plan, b);
        Assert.True(met["land"]);
        Assert.True(met["truce"]);

        var (c2, _, plan2, b2) = Fly("extraction");
        Planet p2 = b2.Planets.Single();
        b2.SelectPlanet(p2.Id);
        Beside(b2, p2);
        b2.Transfer("vip", -1);
        Assert.Equal("Dr. Hale", b2.Player.Vip);
        Assert.True(MissionRun.MetNow(c2, plan2, b2)["up"]);
    }

    [Fact]
    public void A_late_arrival_is_unseen_until_its_time_then_warps_in_at_its_place()
    {
        var (_, _, plan, b) = Fly("holding_action");
        Ship late = b.ShipById(plan.Groups["wave3"][0])!;
        Assert.True(late.Absent);
        Assert.DoesNotContain(late, b.SeenFoes(b.Player));
        // the arrival under test, not the fight: the waves already here hold their fire, so the battle cannot end first
        foreach (Ship e in b.Ships.Where(s => s.Side != b.Player.Side)) e.HoldFire = true;
        Run(b, plan.Battle.Groups.First(g => g.Id == "wave3").ArriveS + 1);
        Assert.True(b.Over == null, $"the battle ended first: {b.Over} at {b.T:0} s");
        Assert.False(late.Absent);
        Assert.True(Math.Abs(late.Z) < 200_000);
    }

    [Fact]
    public void A_sleeping_ship_wakes_when_hit()
    {
        var (_, _, plan, b) = Fly("sleeping_giants");
        Ship giant = b.ShipById(plan.Groups["giant2"][0])!;
        Assert.True(giant.Dormant);
        b.Step();
        Assert.True(giant.Dormant);
        giant.Hull -= 5;
        b.Step();
        Assert.False(giant.Dormant);
    }

    [Fact]
    public void A_battle_with_a_time_limit_ends_at_it_and_a_held_post_is_a_win()
    {
        var (c, m, plan, b) = Fly("holding_action");
        // the attackers kept away: the post stands to the end
        foreach (Ship e in b.Ships.Where(s => s.Side == 1)) { e.X += 3_000_000; e.VOrder = 0; e.V = 0; e.HoldFire = true; }
        Run(b, plan.Scenario.TimeLimitS + 2);
        Assert.True(b.TimeUp);
        Assert.Equal("time", b.Over);
        Debrief d = MissionRun.Finish(c, m, plan, b);
        Assert.True(d.Win);
    }

    [Fact]
    public void A_truce_holds_until_the_player_fires()
    {
        var (_, _, plan, b) = Fly("diplomat_run");
        Ship watch = b.ShipById(plan.Groups["watch"][0])!;
        Assert.True(watch.HoldFire);
        Run(b, 10);
        Assert.True(watch.HoldFire);
        Assert.Null(b.FirstFire);
    }

    [Fact]
    public void Dead_engines_hold_every_ship_still_while_the_outage_lasts()
    {
        var (_, _, plan, b) = Fly("dead_engines");
        EngineOutage de = plan.Scenario.DeadEngines!;
        foreach (Ship e in b.Ships.Where(o => o.Side == 1)) { e.HoldFire = true; e.X += 500_000; }
        Run(b, de.EveryS + 1);
        Assert.True(b.EnginesOut, $"{b.Over} {b.T}");
        Assert.All(b.Ships.Where(s => s.Alive), s => Assert.Equal(0, s.VOrder));
        Run(b, de.ForS);
        Assert.False(b.EnginesOut);
    }

    [Fact]
    public void Placeholder_stations_stand_still_and_the_monster_moves()
    {
        var (_, _, plan, b) = Fly("station");
        Ship st = b.ShipById(plan.Groups["base"][0])!;
        Assert.Equal("sb", st.Class.Id);
        double x = st.X, z = st.Z;
        Run(b, 30);
        Assert.Equal((x, z), (st.X, st.Z));
        var (_, _, plan2, b2) = Fly("monster_hunt");
        Ship beast = b2.ShipById(plan2.Groups["beast"][0])!;
        Assert.Equal("mon", beast.Class.Id);
        Assert.Equal("fauna", beast.Faction.Id);
    }

    [Fact]
    public void A_finished_scan_is_remembered_for_the_mission()
    {
        var (c, _, plan, b) = Fly("monster_hunt");
        Ship beast = b.ShipById(plan.Groups["beast"][0])!;
        b.Player.Target = beast.Id;
        b.Scan();
        b.Player.ScanP = 0.999;
        b.Step();
        Assert.Contains(beast.Id, b.ScannedIds);
        Assert.True(b.Identified(beast));
        Assert.True(MissionRun.MetNow(c, plan, b)["scan"]);
    }

    // ---- The campaign: privileges and pursuit ----------------------------------------------------------------------

    // a new Concord campaign with the fleet moved to another faction's port
    private static Campaign.Campaign AtPortOf(string fac, int standing)
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        Sector port = c.S.Sectors.First(h => h.IsPort && h.Owner == fac);
        c.S.AtC = port.C; c.S.AtR = port.R;
        c.S.Standing[fac] = standing;
        c.S.Credits = 1_000_000;
        return c;
    }

    [Fact]
    public void A_friendly_port_gives_parts_then_supplies_weapons_ships_and_officers_as_standing_grows()
    {
        string fac = Camp.Factions.Keys.First(f => f is not ("concord" or "krai" or "neutral" or "vos") && Campaign.Campaign.New(Camp, "concord").S.Sectors.Any(h => h.IsPort && h.Owner == f));
        var order = new List<string>();
        foreach (int st in new[] { 0, 15, 30, 50, 70 })
        {
            Campaign.Campaign c = AtPortOf(fac, st);
            var g = c.GrantsOf(fac);
            Assert.Equal(order.Count + 1, g.Count);
            order = g.ToList();
        }
        Assert.Equal(CampaignData.Grants, order);
        Campaign.Campaign low = AtPortOf(fac, 10);
        Assert.True(low.Allows("parts"));
        Assert.False(low.Allows("ships"));
        Assert.NotNull(low.Buy(Camp.HullsOf(fac).First(h => h.ForSale).Id));
        Assert.NotNull(low.Hire(low.S.OfficerPool[0]));
        Campaign.Campaign high = AtPortOf(fac, 80);
        Assert.Null(high.Buy(Camp.HullsOf(fac).First(h => h.ForSale).Id));
        Assert.Equal(fac, high.S.Fleet[^1].Maker);
        Assert.Null(high.Hire(high.S.OfficerPool[0]));
    }

    [Fact]
    public void A_hostile_or_rival_port_gives_nothing()
    {
        Campaign.Campaign c = AtPortOf("krai", 80);
        Assert.Empty(c.GrantsOf("krai"));
        Assert.False(c.Allows("parts"));
    }

    [Fact]
    public void Lingering_in_hostile_space_sends_ever_larger_hunters_who_catch_the_fleet()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        Sector deep = c.S.Sectors.Where(h => h.Owner == "krai" && !c.TerrainOf(h).Impassable).OrderBy(h => h.C).ThenBy(h => h.R).First();
        c.S.AtC = deep.C; c.S.AtR = deep.R;
        Assert.True(c.Hostile(deep));
        int lastSize = 0;
        bool caught = false;
        for (int t = 0; t < 14; t++)
        {
            c.S.AtC = deep.C; c.S.AtR = deep.R;
            c.EndTurn();
            FactionFleet? h = c.HuntersOf("krai");
            if (h != null) { Assert.True(h.Size >= lastSize); lastSize = h.Size; }
            caught |= c.Caught().Count > 0;
        }
        Assert.True(c.AlarmLevel("krai") >= 2);
        Assert.True(lastSize >= 2);
        Assert.True(caught);
        Offer fight = c.S.Missions.First(m => m.Script == Campaign.Campaign.PursuitScript);
        Assert.Equal("Krai hunters", c.HuntersOf(fight)!.Name);
    }

    [Fact]
    public void Leaving_hostile_space_cools_the_alarm_and_the_hunters_go_home()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        c.S.Heat["krai"] = 3;
        for (int t = 0; t < 30; t++) c.EndTurn();
        Assert.Equal(0, c.HeatOf("krai"));
        Assert.Null(c.HuntersOf("krai"));
        Assert.DoesNotContain(c.S.Groups, g => g.Pursuit && g.Fac == "krai");
    }

    [Fact]
    public void Beaten_hunters_are_broken_up()
    {
        Campaign.Campaign c = Campaign.Campaign.New(Camp, "concord");
        c.S.Groups.Add(new FactionFleet { Name = "Krai hunters", Fac = "krai", C = c.S.AtC, R = c.S.AtR, HomeC = 0, HomeR = 0, Size = 2, Pursuit = true });
        Offer m = c.Caught().Single();
        MissionPlan plan = MissionRun.Plan(c, m, TestData.Data)!;
        var b = new Battle(TestData.Data, plan.Scenario);
        foreach (string id in plan.Groups["hunters"]) { Ship s = b.ShipById(id)!; s.Hull = 0; s.Alive = false; }
        b.Step();
        Debrief d = MissionRun.Finish(c, m, plan, b);
        Assert.True(d.Win);
        Assert.DoesNotContain(c.S.Groups, g => g.Name == "Krai hunters");
    }
}
