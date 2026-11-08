// ScriptedCaptain.Missions: the one beat that is not a player's input. offer (arg a mission id) puts that mission on offer
// in the campaign, as the tests' OfferMission does, so a playtest can reach every mission type without waiting on the
// random draw; End turn until it fits somewhere (six turns at most). The contract beat then plays it as a player does.
using System.Threading.Tasks;
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
}
