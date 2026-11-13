// Battle.Officers: the officers in the stations' slots (openspec/changes/officers; the battle console mockup's roster,
// officerSkill and officerNext; after the Starfleet Command 3 manual's officers, printed pages 28-29, in our own words
// and numbers). Every ship of the player's side that is not a station has its own officers from the roster, named in its
// faction's way (a second ship of a faction takes the names shifted), one in each station's slot by specialty and the
// rest in reserve; enemy ships have none. What an officer brings is Rules.OfficerSkill, read by each ability's own rule:
// the helm's high energy turns (Rules.HetChanceNow), the power officer's coil drift (ReactorStep), the chief engineer's
// repair crews (Battle.Repair). The other abilities' numbers are in Data.Systems.Officers.Ability for their rules to read
// through OfficerSkill below.
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    /// <summary>The skill the officer in a station's slot of a ship brings to it (0 for a vacant slot or a ship without
    /// officers). The one call every officer's ability reads.</summary>
    public int OfficerSkill(Ship s, string station) => Rules.OfficerSkill(s, station);

    // the roster aboard each ship of the player's side (not a station), each in its specialty's slot, the first of it
    private void OfficersSetup()
    {
        OfficersDef d = Data.Systems.Officers;
        if (d.Roster.Count == 0) return;
        var seen = new Dictionary<string, int>();
        int side = _ships[0].Side, n = d.Roster.Count;
        foreach (Ship s in _ships)
        {
            if (s.Side != side || s.Class.Shape == "station") continue;
            string f = s.Faction.Id;
            int k = seen[f] = seen.GetValueOrDefault(f, -1) + 1;
            IReadOnlyDictionary<string, string>? names = d.Factions.TryGetValue(f, out OfficerNames? on) ? on.Names : null;
            for (int i = 0; i < n; i++)
            {
                RosterOfficer o = d.Roster[i], namer = d.Roster[(i + d.NameShift * k) % n];
                string name = names != null && names.TryGetValue(namer.Id, out string? nm) ? nm : namer.Name;
                s.Crew.Add(new StationOfficer($"{s.Id}.{o.Id}", s.Id, name, o.Rank, o.Spec, o.Skill));
            }
            foreach (string st in OfficersDef.Stations) s.Officers[st] = s.Crew.FirstOrDefault(o => o.Spec == st);
        }
    }

    /// <summary>Mans the flagship with the campaign crew's learned skills (change crew-leveling; Campaign.CrewBonus): each
    /// ability's total goes on the flagship, read by that ability's rule (Ship.Skill), and the comms officer's Coordination
    /// (wing_hit) goes on every other ship of its side as a better chance to hit. Non-finite or negative totals are refused.</summary>
    public void ManCrew(string flagshipId, IReadOnlyDictionary<string, double> bonus)
    {
        Ship? f = ShipById(flagshipId);
        if (f == null) return;
        foreach (var (ab, v) in bonus.OrderBy(kv => kv.Key, System.StringComparer.Ordinal))
        {
            if (!double.IsFinite(v) || v < 0) continue;
            if (ab == "wing_hit") { foreach (Ship w in _ships.Where(w => w != f && w.Side == f.Side && w.Class.Shape != "station")) w.SkillBonus["hit"] = w.Skill("hit") + v; }
            else f.SkillBonus[ab] = f.Skill(ab) + v;
        }
    }

    /// <summary>Seats the campaign crew on the flagship (change crew-leveling, design "The two officer systems"): the
    /// roster's officers leave it, and each campaign officer takes the battle stations theirs covers (crew.json covers),
    /// carrying their card; a station with no campaign officer is vacant. What they bring is their learned skills
    /// (ManCrew), so their roster skill is 0.</summary>
    public void SeatCrew(string flagshipId, IReadOnlyList<CrewSeat> seats)
    {
        Ship? f = ShipById(flagshipId);
        if (f == null) return;
        f.Crew.Clear();
        foreach (string st in OfficersDef.Stations) f.Officers[st] = null;
        foreach (CrewSeat seat in seats)
        {
            var o = new StationOfficer($"{f.Id}.crew.{seat.Card.Station}", f.Id, seat.Name, seat.Rank, seat.Stations.FirstOrDefault() ?? "", 0) { Card = seat.Card };
            f.Crew.Add(o);
            foreach (string st in seat.Stations) if (OfficersDef.Stations.Contains(st)) f.Officers[st] = o;
        }
    }

    /// <summary>An officer's rank name in their ship's faction, full or short; a campaign officer's is their campaign rank.</summary>
    public string RankOf(StationOfficer o, bool brief = false) => o.Card?.Rank ?? Data.Systems.Officers.RankName(ShipById(o.Ship)?.Faction.Id ?? "", o.Rank, brief);

    /// <summary>The next officer for a station's slot of the ship you command: those in reserve (in no other slot), in
    /// roster order, after the one there now.</summary>
    public void OfficerNext(string station)
    {
        if (Sent(nameof(OfficerNext), station)) return;
        Ship s = Player;
        if (s.Crew.Count == 0 || !OfficersDef.Stations.Contains(station)) return;
        var taken = new HashSet<string>(s.Officers.Where(kv => kv.Key != station && kv.Value != null).Select(kv => kv.Value!.Id));
        List<StationOfficer> free = s.Crew.Where(o => !taken.Contains(o.Id)).ToList();
        if (free.Count == 0) return;
        StationOfficer? now = s.Officers.GetValueOrDefault(station);
        int i = now == null ? -1 : free.FindIndex(o => o.Id == now.Id);
        StationOfficer next = free[(i + 1) % free.Count];
        s.Officers[station] = next;
        Say("Comms", $"{RankOf(next)} {next.Name} to {StationName(station)}.");
    }

    /// <summary>A station's name as the console says it.</summary>
    public static string StationName(string station) => station switch
    {
        "pd" => "Point defence", _ => char.ToUpperInvariant(station[0]) + station[1..],
    };
}
