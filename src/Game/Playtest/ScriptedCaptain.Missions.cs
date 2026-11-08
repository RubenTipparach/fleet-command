// ScriptedCaptain.Missions: the beats for the mission systems (openspec/changes/mission-systems and faction-standing).
// offer (arg a mission id) puts that mission on offer, as the tests' OfferMission does, so a playtest reaches every mission
// type without waiting on the random draw (the one input no player gives); the contract beat then plays it. loiter sails into
// the nearest hostile space and ends turns there until its hunters catch the fleet, then fights the pursuit. port (arg
// "<faction>" or "<faction>=<standing>", the standing set as offer sets a mission) sails to that faction's nearest port and
// opens its sector panel, saying what it opens to you.
using System.Linq;
using System.Threading.Tasks;
using FleetCommand.Campaign;
using Godot;

namespace FleetCommand.Game.Playtest;

public sealed partial class ScriptedCaptain
{
    private async Task<string?> OfferBeat(string id)
    {
        var c = Camp;
        if (!c.Data.Missions.ContainsKey(id)) return $"offer: no mission '{id}'";
        for (int t = 0; t < 6; t++)
        {
            if (c.OfferMission(id) is { } m)
            {
                _flow!.Redraw();
                GD.Print($"PLAYTEST_MISSION offered {id} #{m.Id} at {m.C},{m.R}, strength {m.Strength}{(c.Needs(m) is { } n ? $", needs: {n}" : "")}");
                return null;
            }
            if (await EndTurn() is { } st) return st;
        }
        return $"offer: '{id}' fits nowhere in six turns";
    }

    private async Task<string?> Loiter(int maxTurns)
    {
        var c = Camp;
        var to = c.S.Sectors.Where(h => c.Hostile(h) && c.Data.Factions.ContainsKey(h.Owner))
            .Select(h => (H: h, P: c.PathTo(h.C, h.R))).Where(x => x.P != null)
            .OrderBy(x => x.P!.Turns).ThenBy(x => HexGrid.Dist(c.S.AtC, c.S.AtR, x.H.C, x.H.R)).Select(x => x.H).FirstOrDefault();
        if (to == null) return "no hostile space can be reached";
        GD.Print($"PLAYTEST_MISSION into {to.Owner} space at {to.C},{to.R} from {c.S.AtC},{c.S.AtR}");
        for (int t = 0; t < maxTurns; t++)
        {
            if (_flow!.Brief is { } b && b.Script == Campaign.Campaign.PursuitScript)
            {
                GD.Print($"PLAYTEST_MISSION caught on turn {c.S.Turn} by the {c.HuntersOf(b)?.Name} ({c.HuntersOf(b)?.Size} ships) at {c.S.AtC},{c.S.AtR}");
                await Snap("caught");
                return await LaunchAndFight(b, 400);
            }
            if (_flow.Brief != null) await Click("br.close");
            if ((c.S.AtC, c.S.AtR) != (to.C, to.R) && (c.S.OrderC, c.S.OrderR) != (to.C, to.R))
            {
                if ((await ClickHex(to.C, to.R) ?? await Click("sec.move")) is { } st) return st;
                await Seconds(1.5);
            }
            if (c.Hostile(c.At))
            {
                if (await ClickHex(c.S.AtC, c.S.AtR) is { } sh) return sh;
                GD.Print($"PLAYTEST_MISSION turn {c.S.Turn} at {c.S.AtC},{c.S.AtR}: {c.At.Owner} alarm {c.AlarmLevel(c.At.Owner)}, hunters {c.HuntersOf(c.At.Owner)?.Size ?? 0}");
                if (t == 1) await Snap("alarm");
            }
            if (await EndTurn() is { } se) return se;
            await Frames(10);
        }
        return $"no hunters caught the fleet in {maxTurns} turns";
    }

    private async Task<string?> PortBeat(string arg)
    {
        var c = Camp;
        string[] p = arg.Split('=');
        string fac = p[0];
        if (!c.Data.Factions.ContainsKey(fac)) return $"port: no faction '{fac}'";
        if (p.Length > 1) c.ChangeStanding(fac, int.Parse(p[1]) - c.Standing(fac));
        var port = c.S.Sectors.Where(h => h.IsPort && h.Owner == fac).Select(h => (H: h, P: c.PathTo(h.C, h.R)))
            .Where(x => (x.H.C, x.H.R) == (c.S.AtC, c.S.AtR) || x.P != null).OrderBy(x => HexGrid.Dist(c.S.AtC, c.S.AtR, x.H.C, x.H.R)).Select(x => x.H).FirstOrDefault();
        if (port == null) return $"port: no {fac} port can be reached";
        for (int t = 0; t < 16 && (c.S.AtC, c.S.AtR) != (port.C, port.R); t++)
        {
            if (_flow!.Brief != null) await Click("br.close");
            if ((c.S.OrderC, c.S.OrderR) != (port.C, port.R))
            {
                if ((await ClickHex(port.C, port.R) ?? await Click("sec.move")) is { } st) return st;
                await Seconds(1.5);
                if ((c.S.AtC, c.S.AtR) == (port.C, port.R)) break;
            }
            if (await EndTurn() is { } se) return se;
        }
        if ((c.S.AtC, c.S.AtR) != (port.C, port.R)) return $"port: the fleet never reached {port.PortName}";
        if (_flow!.Brief != null) await Click("br.close");
        if (await ClickHex(port.C, port.R) is { } s1) return s1;
        GD.Print($"PLAYTEST_MISSION at {port.PortName} ({fac}), standing {c.Standing(fac)}: opens [{string.Join(", ", c.GrantsOf(fac))}]");
        await Snap($"port-{fac}-{c.Standing(fac)}");
        return null;
    }
}
