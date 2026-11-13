// Campaign.Crew: the crew's officers level up (change crew-leveling, mockup 39; owner, 2026-11-12: "ensigns provide no bonus
// but gain xp overtime, and can be traded for prestige so you can get more experienced crew, or just level up your crew
// member", "skill tree, each level gets a skill point, the player can auto level officers or pick paths on the tree to go
// down and specialize"; 2026-11-13: "implement the tree in the game"). Every number is data/campaign/crew.json (CrewDef):
// every officer of the crew gains experience from each battle by its grade and from each turn; a promotion spends the next
// level's experience (a shortfall paid in prestige) for a skill point; a point learns a skill on the station's tree once
// its prerequisites are met, by hand or by auto level; an officer transferred out is worth prestige; the pool sells
// officers for prestige by level. The page (docs/mockups/crew-leveling.html) runs the same rules on the same data.
using System;
using System.Collections.Generic;
using System.Linq;

namespace FleetCommand.Campaign;

public sealed partial class Campaign
{
    private CrewDef Cd => Data.Crew;

    /// <summary>The officers on offer at each station, one at each of the data's pool levels, each with a name no one else
    /// has, new every turn.</summary>
    private void RollOfficers()
    {
        S.OfficerPool = new List<Officer>();
        foreach (string st in Data.Officers.Stations)
            foreach (int level in Cd.PoolLevels) S.OfficerPool.Add(NewOfficer(st, level));
    }

    /// <summary>A new officer for a station at a level, their points unplaced, named from data's given and family names.</summary>
    private Officer NewOfficer(string station, int level)
    {
        OfficerDef od = Data.Officers;
        string name = "";
        for (int tries = 0; tries < 64; tries++)
        {
            name = od.Given[R.Below(od.Given.Count)] + " " + od.Family[R.Below(od.Family.Count)];
            if (!S.Crew.Concat(S.OfficerPool).Any(o => o.Name == name)) break;
        }
        return new Officer { Name = name, Station = station, Level = level };
    }

    /// <summary>A save from before crew leveling: each officer's old skill level (Cadet, Skilled, Veteran, Elite, Legendary)
    /// becomes a level (1, 3, 5, 7, 9) with its points spent by auto level, and the pool is rolled anew.</summary>
    internal void CrewFromOldSave()
    {
        if (S.Crew.All(o => o.Level > 0) && S.OfficerPool.All(o => o.Level > 0)) return;
        var names = Data.Officers.Skills.Select(k => k.Skill).ToList();
        foreach (Officer o in S.Crew.Where(o => o.Level <= 0))
        {
            int i = Math.Max(0, names.IndexOf(o.Skill ?? ""));
            o.Level = Math.Clamp(1 + 2 * i, 1, Cd.Levels);
            o.Skill = null;
            o.Auto = true;
            AutoSpend(o);
        }
        RollOfficers();
    }

    /// <summary>An officer's rank, by their level.</summary>
    public string RankOf(Officer o) => Cd.RankOf(o.Level);

    /// <summary>The tree of an officer's station.</summary>
    public CrewTree TreeOf(Officer o) => Cd.Trees[o.Station];

    /// <summary>Skill points an officer has to place: one for each level after the first, less the skills learned.</summary>
    public int Points(Officer o) => o.Level - 1 - o.Skills.Count;

    /// <summary>The experience an officer needs for the next level, or null at the highest.</summary>
    public int? NextXp(Officer o) => o.Level < Cd.Levels ? Cd.XpNext[o.Level - 1] : null;

    /// <summary>The prestige a promotion costs now: the experience still short, at the data's rate (0 when it is banked).</summary>
    public int PromoteCost(Officer o) => NextXp(o) is int need ? Math.Max(0, (int)Math.Ceiling((need - o.Xp) / (double)Cd.XpPerPrestige)) : 0;

    /// <summary>What an officer is worth transferred out, in prestige: their level's worth and their banked experience.</summary>
    public int Worth(Officer o) => Cd.WorthPerLevel * (o.Level - 1) + o.Xp / Cd.XpPerPrestige;

    /// <summary>What an officer on offer costs to hire, in prestige.</summary>
    public int HireCost(Officer o) => Cd.HirePerLevel * (o.Level - 1);

    /// <summary>Whether a skill is open to an officer: every skill in its All learned and one in its Any (when it has any).</summary>
    public static bool Unlocked(Officer o, CrewSkill s) => s.All.All(o.Skills.Contains) && (s.Any.Count == 0 || s.Any.Any(o.Skills.Contains));

    /// <summary>Whether an officer can learn a skill now: open, not yet learned, and a point to spend.</summary>
    public bool CanLearn(Officer o, CrewSkill s) => Points(o) > 0 && !o.Skills.Contains(s.Id) && Unlocked(o, s);

    /// <summary>Learns a skill with one point; picking by hand turns auto level off for the officer. Returns why not, or null.</summary>
    public string? Learn(Officer o, string skillId, bool byHand = true)
    {
        CrewSkill? s = TreeOf(o).Skill(skillId);
        if (s == null) return $"No skill {skillId} at {o.Station}.";
        if (o.Skills.Contains(s.Id)) return $"{o.Name} already knows {s.Name}.";
        if (Points(o) <= 0) return $"{o.Name} has no skill point to spend.";
        if (!Unlocked(o, s)) return $"{s.Name} needs " + string.Join(" and ", s.All.Select(id => TreeOf(o).Skill(id)!.Name).Concat(s.Any.Count > 0 ? new[] { string.Join(" or ", s.Any.Select(id => TreeOf(o).Skill(id)!.Name)) } : Array.Empty<string>())) + ".";
        o.Skills.Add(s.Id);
        if (byHand) o.Auto = false;
        return null;
    }

    /// <summary>Spends an officer's points along their station's auto path, then on the first open skill, until none is left.</summary>
    public void AutoSpend(Officer o)
    {
        CrewTree t = TreeOf(o);
        foreach (string id in t.Auto) if (Points(o) > 0 && t.Skill(id) is { } s && CanLearn(o, s)) o.Skills.Add(id);
        while (Points(o) > 0 && t.Skills.FirstOrDefault(s => CanLearn(o, s)) is { } next) o.Skills.Add(next.Id);
    }

    /// <summary>Turns an officer's auto level on (spending what points they have now) or off.</summary>
    public void SetAuto(Officer o, bool on) { o.Auto = on; if (on) AutoSpend(o); }

    /// <summary>Promotes an officer a level: spends the next level's experience, any shortfall paid in prestige, for one
    /// skill point (spent at once on auto level). Returns why not, or null.</summary>
    public string? Promote(Officer o)
    {
        if (!S.Crew.Contains(o)) return $"{o.Name} is not in the crew.";
        if (NextXp(o) is not int need) return $"{o.Name} is at the highest level.";
        int pay = PromoteCost(o);
        if (pay > S.Prestige) return $"Needs {pay} prestige (you have {S.Prestige}).";
        S.Prestige -= pay;
        o.Xp = Math.Max(0, o.Xp - need);
        o.Level++;
        if (o.Auto) AutoSpend(o);
        return null;
    }

    /// <summary>Transfers an officer out of the crew for their worth in prestige; their station stands vacant.</summary>
    public string? Transfer(Officer o)
    {
        if (!S.Crew.Remove(o)) return $"{o.Name} is not in the crew.";
        S.Prestige += Worth(o);
        return null;
    }

    /// <summary>Takes back every skill an officer has learned, for the data's prestige, and turns auto level off.</summary>
    public string? Retrain(Officer o)
    {
        if (o.Skills.Count == 0) return $"{o.Name} has nothing to retrain.";
        if (Cd.Retrain > S.Prestige) return $"Needs {Cd.Retrain} prestige (you have {S.Prestige}).";
        S.Prestige -= Cd.Retrain;
        o.Skills.Clear();
        o.Auto = false;
        return null;
    }

    /// <summary>Hires an officer on offer for prestige. One officer serves each station: whoever served there transfers out
    /// for their worth; the hired one's place on offer is taken by someone new at the same level.</summary>
    public string? Hire(Officer o)
    {
        if (!Allows("officers")) return Refused("officers", "Officers are recruited at your own ports.");
        int at = S.OfficerPool.IndexOf(o);
        if (at < 0) return $"{o.Name} is not on offer.";
        int cost = HireCost(o);
        Officer? was = AtStation(o.Station);
        int back = was != null ? Worth(was) : 0;
        if (cost > S.Prestige + back) return $"Needs {cost} prestige (you have {S.Prestige}).";
        if (was != null) Transfer(was);
        S.Prestige -= cost;
        S.OfficerPool[at] = NewOfficer(o.Station, o.Level);
        S.Crew.Add(o);
        return null;
    }

    /// <summary>Every officer of the crew gains experience (a battle by its grade, a turn served).</summary>
    public void CrewGains(int xp)
    {
        if (xp <= 0) return;
        foreach (Officer o in S.Crew) o.Xp += xp;
    }

    /// <summary>The experience a battle of a grade gives every officer of the crew.</summary>
    public int BattleXp(string grade) => (int)Math.Round(Cd.XpBattle * Cd.GradeX.GetValueOrDefault(grade));

    /// <summary>The crew as the flagship seats them in battle (Battle.SeatCrew): each officer on the battle stations their
    /// station covers (crew.json covers), with their rank, level, experience and learned skills for the hover card.</summary>
    public List<FleetCommand.Sim.CrewSeat> CrewSeats() =>
        Data.Officers.Stations.Select(AtStation).OfType<Officer>().Select(o => new FleetCommand.Sim.CrewSeat(
            Cd.Covers[o.Station], o.Name, Math.Max(0, Cd.RankLevel.Count(l => l <= o.Level) - 1),
            new FleetCommand.Sim.OfficerCard(o.Station, RankOf(o), o.Level, o.Xp, NextXp(o),
                o.Skills.Select(id => TreeOf(o).Skill(id)).OfType<CrewSkill>().Select(s => new FleetCommand.Sim.OfficerCardSkill(s.Name, s.Text)).ToList()))).ToList();

    /// <summary>What the whole crew's learned skills add up to, by ability (the rules' keys, data/campaign/crew.json fx): what
    /// the flagship's officers bring to a battle (Ship.SkillBonus) and the campaign's own (standing).</summary>
    public Dictionary<string, double> CrewBonus()
    {
        var sum = new Dictionary<string, double>();
        foreach (Officer o in S.Crew)
            foreach (string id in o.Skills)
                if (TreeOf(o).Skill(id) is { } s)
                    foreach (var (ab, v) in s.Fx) sum[ab] = sum.GetValueOrDefault(ab) + v;
        return sum;
    }
}
