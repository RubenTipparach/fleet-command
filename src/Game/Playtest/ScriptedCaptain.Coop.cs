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

    // co-op's talk in a battle, once each per battle: the admiral pings its target and sends each crew member's ship to attack
    // it; a crew member says they are on station and answers a directive with Aye. All through the console's own keys.
    private Battle? _talkedIn;
    private bool _said, _pinged, _directed;
    private double _pingTried = double.MinValue;

    private async Task CoopEach()
    {
        if (_x.Coop is not { Battle: { } bt } co) return;
        if (_talkedIn != bt) { _talkedIn = bt; _said = _pinged = _directed = false; _pingTried = double.MinValue; }
        if (!_said)
        {
            _said = true;
            await ChatSay(co.IsHost ? "Battle stations." : $"On station: {string.Join(", ", co.Crew.Roles.List.Where(r => co.Crew.Opens(co.Me, r.Id)).Select(r => r.Name))}.");
        }
        if (!co.IsHost && co.DirectiveFor(co.Me) != null)
        {
            if (await Click("v.aye") == null) GD.Print($"PLAYTEST_COOP aye to the admiral at {bt.T:0} s");
        }
        if (co.IsHost && _x.MyTarget() is { } t)
        {
            // tried each pass until it lands (a target off the view cannot be clicked), at most every 3 s
            if (!_pinged && bt.T - _pingTried > 3)
            {
                _pingTried = bt.T;
                // the target where the view shows it, else the plane at its place (off screen: the view's edge toward it)
                int before = co.Events.Count;
                if (await Click("v.ping") == null)
                {
                    if (await Select(t.Id) != null) await ClickPlane(t.X, t.Z);
                    await Frames(3);
                    _pinged = co.Events.Count > before;
                    if (_pinged) GD.Print($"PLAYTEST_COOP pinged the {t.Name} at {bt.T:0} s");
                    _x.State.PlacingPing = false;
                }
            }
            if (!_directed && co.Crew.Ships.Count > 0)
            {
                _directed = true;
                // each crew member's ship on the Fleet station: its tab, then Attack my target
                if (await Click("st.fleet") == null && (!_x.State.CrewShown || await Click("fl.view.fleet") == null))
                {
                    foreach (var (ship, m) in co.Crew.Ships.ToList())
                        if (await Click($"fl.{ship}") == null && await Click("fl.dir.attack") == null) GD.Print($"PLAYTEST_COOP directed {co.Crew.NameOf(m)} to attack the {t.Name}");
                    await Frames(20);
                    await Snap("fleet-station");
                    // the targeting station's crew strip (change bridge, lock for), as the admiral sees it
                    if (await Click("st.tactical") == null) { await Frames(10); await Snap("crew-strip"); }
                }
            }
        }
    }

    // a line said in the crew's chat, typed as a player would: the Chat key, the text, Enter
    private async Task ChatSay(string text)
    {
        if (await Click("chat.open") != null) return;
        await Frames(2);
        if (GetViewport().GuiGetFocusOwner() is not LineEdit le) return;
        le.InsertTextAtCaret(text);
        await Frames(2);
        var enter = new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = true };
        GetViewport().PushInput(enter, true);
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = false }, true);
        await Frames(3);
        GD.Print($"PLAYTEST_COOP said: {text}");
    }

    // a crew peer with no battle shared (between battles, or the admiral's battle stopped)
    private bool CrewIdle() => _x.Coop is { IsHost: false, Battle: null };

    private async Task<string?> CoopBeat(Beat b)
    {
        if (b.Do == "dock") return await Dock();   // a campaign beat, alone or in co-op
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
                // "role=member", or "role=member@n" for the roles of the n-th ship of the fleet after the flagship
                string[] p = b.Arg.Split('=');
                string what = p[0];
                string[] tp = p[1].Split('@');
                bool take = tp[0].StartsWith("-");
                int to = int.Parse(tp[0].TrimStart('-'));
                string? onShip = null;
                if (tp.Length > 1)
                {
                    Ship[] fleet = B.Fleet().Where(s => s != co.Flag && !s.Fixed).ToArray();
                    int n = int.Parse(tp[1]) - 1;
                    if (n < 0 || n >= fleet.Length) return $"crew_role: no ship {tp[1]}";
                    onShip = fleet[n].Id;
                }
                if (b.Do == "crew_ship")
                {
                    // ships are given in a battle: off the battle screen it waits for the next one (LaunchAndFight)
                    if (co.Battle == null || _flow is { Screen: not FlowScreen.Battle }) { _shipToCrew = b; GD.Print($"PLAYTEST_COOP crew_ship {b.Arg} at the next battle"); return null; }
                    Ship[] others = B.Fleet().Where(s => s != co.Flag && !s.Fixed).ToArray();
                    int i = int.Parse(what) - 1;
                    if (i < 0 || i >= others.Length) return $"crew_ship: the fleet has {others.Length} ships besides the flagship";
                    what = others[i].Id;
                }
                // a role in the roles grid, a ship in the assignments: both on the Fleet station's Crew view
                string? open = await OpenCrew();
                if (open != null) return open;
                if (onShip != null && await Click($"fl.rb.{onShip}") is { } nb) return nb;
                string id = b.Do == "crew_role" ? $"crew.role.{what}.{to}" : $"fl.as.{to}.{what}";
                // a role held by the member already is a lit box: pressing it would hand it back, so leave it
                // a grant is a toggle: pressed only when it is not already as asked ("-member" takes it back)
                bool held = b.Do == "crew_role" && co.Crew.Holds(onShip ?? co.Crew.BridgeOf(to), what, to);
                string? st = b.Do == "crew_role" && held != take ? null : await Click(id);
                await Frames(20);
                await Snap($"crew-{b.Do}-{what}");
                if (b.Do == "crew_role" && _x.CrewOpen) await Click("crew.done");
                GD.Print($"PLAYTEST_COOP {b.Do} {what} to {co.Crew.NameOf(to)}: bridge of {co.Crew.NameOf(to)} is {co.Crew.BridgeOf(to)}");
                return st;
            }
            case "dock": return await Dock();
            case "admiral_bridge":
            {
                // the admiral steps onto the n-th ship of the fleet after the flagship (0: back to the flagship), from the
                // Tactical station's Bridge row
                int n = int.Parse(b.Arg == "" ? "0" : b.Arg);
                Ship[] fleet = B.Fleet().Where(s => s != co.Flag && !s.Fixed).ToArray();
                string key = n == 0 ? Crew.Flag : fleet[n - 1].Id;
                string? st = await Click("st.tactical") ?? await Click($"cs.br.{key}");
                await Frames(10);
                await Snap($"admiral-bridge-{n}");
                GD.Print($"PLAYTEST_COOP admiral on {co.NameOfBridge(co.Crew.AdmiralAt)}'s bridge, flagship {(B.FlagUnmanned ? "flown by the AI" : "manned")}");
                return st;
            }
            case "crew_session": return await CrewSession(int.Parse(b.Arg == "" ? "1" : b.Arg), b.Seconds > 0 ? b.Seconds : 7200);
        }
        return null;
    }

    // the roles grid: in a battle the Comms station's Crew key, on the map the menu's Crew (the captain's menu)
    private async Task<string?> OpenCrew()
    {
        if (_x.CrewOpen) return null;
        if (_flow is { Screen: FlowScreen.Battle } || _flow == null)
            return await Click("st.fleet") ?? (_x.State.CrewShown ? null : await Click("fl.view.crew"));
        return await Click("top.menu") ?? await Click("m.crew");
    }

    // credits kept back after a purchase, for the repairs the next missions will need
    private const int DockReserveCr = 1000;

    private async Task<string?> Dock()
    {
        var c = Camp;
        if (await PortBeat(c.S.Side) is { } st) return st;
        await Frames(10);
        // into the port from the sector panel: its shipyard, the fleet column (with each ship's Repair key) down its left
        if (await Click("sec.port.yard") is { } sp) return $"dock: the port did not open ({sp})";
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
            var buy = c.Data.HullsOf(c.YardSide).Where(h => h.ForSale && c.HullPrice(h.Id) <= c.S.Credits - DockReserveCr).OrderByDescending(h => c.HullPrice(h.Id)).FirstOrDefault();
            if (buy == null) GD.Print($"PLAYTEST_COOP no ship affordable ({c.S.Credits:0} cr)");
            else if (await Click("port.tab.yard") is { } ty) GD.Print($"PLAYTEST_COOP the shipyard tab did not open: {ty}");
            else
            {
                await Click($"yard.pick.{buy.Id}");
                int n = c.S.Fleet.Count;
                await Click("yard.buy");
                await Frames(10);
                if (c.S.Fleet.Count > n) { GD.Print($"PLAYTEST_COOP bought the {c.S.Fleet[^1].Name} ({buy.Name}) for {c.HullPrice(buy.Id)} cr, {c.S.Credits:0} cr left"); await Snap("dock-bought"); }
            }
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
            // the admiral gives roles and ships in the battle's first seconds: the seat is read once they settle
            await Seconds(8);
            string bridge = co.Crew.BridgeOf(co.Me);
            bool captain = co.Crew.CaptainOf(bridge) == co.Me;
            var mine = co.Crew.Roles.List.Where(r => co.Crew.Holds(bridge, r.Id, co.Me)).Select(r => r.Id).ToList();
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
