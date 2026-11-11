// ScriptedCaptain.Coop: the beats for a co-op game (openspec/changes/bridge-crew-coop), the admiral's and a crew member's,
// each pressing what a player at that seat would. The admiral's: wait_crew (arg n: until n crew members have joined),
// crew_role (arg "role=member": the captain's menu, the role's box in that member's row; "role=1" takes it back),
// crew_ship (arg "n=member": the n-th ship of the fleet after the flagship given to a member, "n=1" back to the AI), and
// dock (to the nearest port of your own, every ship repaired that can be paid for, and a ship bought at the shipyard when
// the credits allow it and a berth is free: owner, 2026-11-11: "when ship gets damaged you dont get auto repair, you have to
// pay to repair your ship", "buy new ships or upgrade current one, I'll leave it up to you"). The crew member's:
// crew_session (arg n: n battles, each fought from the stations they hold, the ship they were given steered and fought as
// its captain would, until the admiral's battle ends, then the next).
using System;
using System.Linq;
using System.Threading.Tasks;
using FleetCommand.Campaign;
using FleetCommand.Coop;
using FleetCommand.Game.Flow;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Playtest;

public sealed partial class ScriptedCaptain
{
    // a ship to give at the start of the next battle (crew_ship off the battle screen)
    private Beat? _shipToCrew;

    /// <summary>At a battle's start: the ship hand-over that was waiting for it.</summary>
    private async Task GiveWaitingShip()
    {
        if (_shipToCrew is not { } b) return;
        _shipToCrew = null;
        if (await CoopBeat(b) is { } why) GD.Print($"PLAYTEST_COOP crew_ship {b.Arg} not done: {why}");
    }

    // a crew peer with no battle shared (between battles, or the admiral's battle stopped)
    private bool CrewIdle() => _x.Coop is { IsHost: false, Battle: null };

    private async Task<string?> CoopBeat(Beat b)
    {
        Lockstep? co = _x.Coop;
        if (co == null) return "not a co-op game (--host or --join)";
        switch (b.Do)
        {
            case "wait_crew":
            {
                int n = int.Parse(b.Arg == "" ? "1" : b.Arg);
                ulong end = Time.GetTicksMsec() + (ulong)((b.Seconds > 0 ? b.Seconds : 600) * 1000);
                while (co.Crew.Members.Count - 1 < n)
                {
                    if (Time.GetTicksMsec() > end) return $"{co.Crew.Members.Count - 1} of {n} crew joined";
                    await Frames(10);
                }
                GD.Print($"PLAYTEST_COOP crew aboard: {string.Join(", ", co.Crew.Members.Select(m => $"{m.Id} {m.Name}"))}");
                return null;
            }
            case "crew_role": case "crew_ship":
            {
                string[] p = b.Arg.Split('=');
                string what = p[0];
                int to = int.Parse(p[1]);
                if (b.Do == "crew_ship")
                {
                    // ships are given in a battle: off the battle screen it waits for the next one (LaunchAndFight)
                    if (co.Battle == null || _flow is { Screen: not FlowScreen.Battle }) { _shipToCrew = b; GD.Print($"PLAYTEST_COOP crew_ship {b.Arg} at the next battle"); return null; }
                    Ship[] others = B.Fleet().Where(s => s != co.Flag).ToArray();
                    int i = int.Parse(what) - 1;
                    if (i < 0 || i >= others.Length) return $"crew_ship: the fleet has {others.Length} ships besides the flagship";
                    what = others[i].Id;
                }
                string? open = await OpenCrew();
                if (open != null) return open;
                string id = b.Do == "crew_role" ? $"crew.role.{what}.{to}" : $"crew.ship.{what}.{to}";
                // a role held by the member already is a lit box: pressing it would hand it back, so leave it
                bool held = b.Do == "crew_role" && co.Crew.HolderOf(co.Crew.BridgeOf(co.Me), what) == to;
                string? st = held ? null : await Click(id);
                await Frames(20);
                await Snap($"crew-{b.Do}-{what}");
                await Click("crew.done");
                GD.Print($"PLAYTEST_COOP {b.Do} {what} to {co.Crew.NameOf(to)}: bridge of {co.Crew.NameOf(to)} is {co.Crew.BridgeOf(to)}");
                return st;
            }
            case "dock": return await Dock();
            case "crew_session": return await CrewSession(int.Parse(b.Arg == "" ? "1" : b.Arg), b.Seconds > 0 ? b.Seconds : 7200);
        }
        return null;
    }

    // the captain's menu: from the name plate's Crew key in a battle, from the menu on the map
    private async Task<string?> OpenCrew()
    {
        if (_x.CrewOpen) return null;
        if (_flow is { Screen: FlowScreen.Battle } || _flow == null) return await Click("crew");
        return await Click("top.menu") ?? await Click("m.crew");
    }

    private async Task<string?> Dock()
    {
        var c = Camp;
        if (await PortBeat(c.S.Side) is { } st) return st;
        await Frames(10);
        foreach (CampShip s in c.S.Fleet.ToList())
        {
            int cost = c.RepairCost(s);
            if (cost <= 0) continue;
            if (cost > c.S.Credits) { GD.Print($"PLAYTEST_COOP cannot pay {cost} cr to repair the {s.Name} ({c.S.Credits:0} cr)"); continue; }
            double before = c.S.Credits;
            await Click($"pf.repair.{s.Id}");
            GD.Print($"PLAYTEST_COOP repaired the {s.Name} for {before - c.S.Credits:0} cr, hull {s.HullPct:0}%");
        }
        await Snap("dock-repaired");
        if (c.S.Fleet.Count < c.Data.Rules.FleetMax && c.Allows("ships"))
        {
            var buy = c.Data.HullsOf(c.YardSide).Where(h => h.ForSale && c.HullPrice(h.Id) <= c.S.Credits).OrderByDescending(h => c.HullPrice(h.Id)).FirstOrDefault();
            if (buy != null && await Click("port.tab.yard") == null)
            {
                await Click($"yard.pick.{buy.Id}");
                int n = c.S.Fleet.Count;
                await Click("yard.buy");
                await Frames(10);
                if (c.S.Fleet.Count > n) { GD.Print($"PLAYTEST_COOP bought the {c.S.Fleet[^1].Name} ({buy.Name}) for {c.HullPrice(buy.Id)} cr, {c.S.Credits:0} cr left"); await Snap("dock-bought"); }
            }
            else GD.Print($"PLAYTEST_COOP no ship affordable ({c.S.Credits:0} cr)");
        }
        await Click("port.close");
        GD.Print($"PLAYTEST_COOP docked: fleet {string.Join(", ", c.S.Fleet.Select(s => $"{s.Name} {s.HullPct:0}%"))}, credits {c.S.Credits:0}");
        return null;
    }

    // a crew member's game: each battle fought from their seat until the admiral's ends, for n battles
    private async Task<string?> CrewSession(int battles, double maxS)
    {
        Lockstep co = _x.Coop!;
        ulong end = Time.GetTicksMsec() + (ulong)(maxS * 1000);
        for (int k = 0; k < battles; k++)
        {
            while (co.Battle == null || co.Battle.Over != null)
            {
                if (co.Lost) { GD.Print($"PLAYTEST_COOP the admiral left after {k} battles"); return co.Desyncs > 0 ? $"{co.Desyncs} desyncs" : null; }
                if (Time.GetTicksMsec() > end) return $"only {k} of {battles} battles started in {maxS:0} s";
                await Frames(10);
            }
            Battle bt = co.Battle;
            await Frames(60);
            string bridge = co.Crew.BridgeOf(co.Me);
            bool captain = co.Crew.CaptainOf(bridge) == co.Me;
            var mine = co.Crew.Roles.List.Where(r => co.Crew.HolderOf(bridge, r.Id) == co.Me).Select(r => r.Id).ToList();
            GD.Print($"PLAYTEST_COOP battle {k + 1} {bt.Scenario.Id}: {(captain ? $"captain of the {co.OwnShip()?.Name}" : $"on the {co.OwnShip()?.Name} holding {string.Join(", ", mine)}")}");
            await Snap($"crew-battle-{k + 1}");
            // a station the member holds is opened, as a player would look at it first
            if (!captain && mine.FirstOrDefault(r => r is "weapons" or "science") is { } first)
                await Click(first == "weapons" ? "st.tactical" : "st.science");
            bool fights = captain || mine.Contains("weapons");
            if (fights) await Fight(maxS, steer: captain || mine.Contains("helm"));
            else while (co.Battle == bt && bt.Over == null) await Frames(10);
            while (co.Battle == bt && bt.Over == null && !CrewIdle()) await Frames(10);
            GD.Print($"PLAYTEST_COOP battle {k + 1} ended {bt.Over ?? "stopped"} at {bt.T:0} s; in sync {co.Synced}, desync {co.Desyncs}");
            await Snap($"crew-battle-{k + 1}-end");
        }
        return co.Desyncs > 0 ? $"{co.Desyncs} desyncs" : null;
    }
}
