// ScriptedCaptain.Freighters: the playtest beats the freighter-missions change adds to the one captain. meet (arg "escort"
// or "raid") sails the fleet into a freighter's sector, as a player chasing one along her route does, presses her Escort or
// Raid key on the sector panel (owner, 2026-11-07: "Just whenever you are on the same tile as freighter, you get a mission
// option"), and plays the encounter out with the contracts' own Launch, fight and debrief. cash_out sails a fleet carrying a
// freighter taken to the nearest port that will buy her cargo and presses Cash out (with none aboard, it says so and goes on).
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FleetCommand.Campaign;
using Godot;

namespace FleetCommand.Game.Playtest;

public sealed partial class ScriptedCaptain
{
    // The freighters whose encounter is the one asked for; one off her route is met by no one.
    private List<Freighter> Quarry(string kind) =>
        Camp.S.Freighters.Where(f => f.Out == 0 && Camp.EncounterOf(f) == kind).ToList();

    private async Task<string?> Meet(string kind, double maxBattleS)
    {
        if (kind != Campaign.Campaign.EscortScript && kind != Campaign.Campaign.RaidScript) return $"meet needs \"escort\" or \"raid\", not '{kind}'";
        var c = Camp;
        Freighter? f = null;
        for (int t = 0; t < 12 && f == null; t++)
        {
            f = Quarry(kind).FirstOrDefault(q => q.Here() == (c.S.AtC, c.S.AtR));
            if (f != null) break;
            // the nearest she can be caught this turn, else the nearest at all: her sector now, before End turn moves her on
            var reach = Quarry(kind).Select(q => (F: q, P: c.PathTo(q.Here().C, q.Here().R))).Where(x => x.P != null).ToList();
            if (reach.Count == 0) return $"no {kind} freighter can be reached";
            var go = reach.OrderBy(x => x.P!.Turns).ThenBy(x => HexGrid.Dist(c.S.AtC, c.S.AtR, x.F.Here().C, x.F.Here().R)).ThenBy(x => x.F.Name).First();
            GD.Print($"PLAYTEST_MISSION chase the {go.F.Name} ({go.F.Fac}) at {go.F.Here().C},{go.F.Here().R} from {c.S.AtC},{c.S.AtR}, {go.P!.Turns} turn(s)");
            if (go.P!.Legs[0].Count > 0)
            {
                string? stuck = await ClickHex(go.F.Here().C, go.F.Here().R) ?? await Click("sec.move");
                if (stuck != null) return stuck;
                await Seconds(1.5);
                if (_flow!.Brief != null) await Click("br.close");   // a mission of the fleet's own waits where it arrived
                f = go.F.Here() == (c.S.AtC, c.S.AtR) ? go.F : null;
            }
            if (f == null && await EndTurn() is { } st) return st;
        }
        if (f == null) return $"no {kind} freighter met in twelve turns";
        // the sector panel of the fleet's own sector, her key on it
        if (await ClickHex(c.S.AtC, c.S.AtR) is { } s1) return s1;
        await Snap($"meet-{kind}");
        if (await Click($"sec.meet.{f.Name}") is { } s2) return s2;
        await Frames(6);
        if (_flow!.Brief is not { } m || m.Script != kind) return $"the {f.Name}'s key opened no {kind} briefing";
        GD.Print($"PLAYTEST_MISSION take {c.ScriptOf(m).Name} #{m.Id} with the {f.Name} ({f.Fac}) at {m.C},{m.R}, reward {m.Reward:0} cr");
        await Snap($"brief-{kind}");
        int hauls = c.S.Fleet.Count(s => s.Haul > 0);
        string? done = await LaunchAndFight(m, maxBattleS);
        GD.Print($"PLAYTEST_MISSION the {f.Name} is {(f.Out > 0 ? $"off her route for {f.Out} turns" : "on her route")}; hauls aboard {hauls} -> {c.S.Fleet.Count(s => s.Haul > 0)} ({c.HaulValue} cr)");
        return done;
    }

    private async Task<string?> CashOut()
    {
        var c = Camp;
        // a raid lost or a freighter destroyed brings no cargo: that is the raid's result, and the beat says so and goes on
        if (c.HaulValue == 0) { GD.Print("PLAYTEST_MISSION nothing to cash out: no freighter taken"); _missions.Add("Cash out: no cargo aboard"); return null; }
        var makers = c.S.Fleet.Where(s => s.Haul > 0).Select(s => s.Maker).ToHashSet();
        // the nearest port that buys: your own, or one that deals with you, not the faction every haul was taken from
        var buyers = c.S.Sectors.Where(h => (c.IsPort(h) || c.IsMarket(h)) && makers.Any(mk => mk != (c.IsPort(h) ? c.S.Side : h.Owner)))
            .Select(h => (H: h, P: c.PathTo(h.C, h.R))).Where(x => (x.H.C, x.H.R) == (c.S.AtC, c.S.AtR) || x.P != null)
            .OrderBy(x => HexGrid.Dist(c.S.AtC, c.S.AtR, x.H.C, x.H.R)).ToList();
        if (buyers.Count == 0) return "no port that buys the cargo can be reached";
        Sector to = buyers[0].H;
        GD.Print($"PLAYTEST_MISSION cash out at {to.PortName} ({to.C},{to.R}) from {c.S.AtC},{c.S.AtR}");
        for (int t = 0; t < 16 && (c.S.AtC, c.S.AtR) != (to.C, to.R); t++)
        {
            if (c.S.OrderC == null || (c.S.OrderC, c.S.OrderR) != (to.C, to.R))
            {
                string? stuck = await ClickHex(to.C, to.R) ?? await Click("sec.move");
                if (stuck != null) return stuck;
                await Seconds(1.5);
                if (_flow!.Brief != null) await Click("br.close");
                if ((c.S.AtC, c.S.AtR) == (to.C, to.R)) break;
            }
            if (await EndTurn() is { } st) return st;
        }
        if ((c.S.AtC, c.S.AtR) != (to.C, to.R)) return $"the fleet never reached {to.PortName}";
        if (await ClickHex(to.C, to.R) is { } s1) return s1;
        int before = c.S.Credits, value = c.HaulValue;
        await Snap("cashout");
        if (await Click("sec.cashout") is { } s2) return s2;
        await Frames(6);
        GD.Print($"PLAYTEST_MISSION cashed out {value} cr of cargo at {to.PortName}: credits {before} -> {c.S.Credits}");
        _missions.Add($"Cash out at {to.PortName}: {c.S.Credits - before} cr");
        return c.S.Credits > before ? null : $"Cash out paid nothing ({_flow!.Msg})";
    }
}
