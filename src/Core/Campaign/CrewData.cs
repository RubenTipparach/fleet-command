// CrewData: the crew leveling rules and the stations' skill trees (change crew-leveling, mockup 39; owner, 2026-11-12:
// "ensigns provide no bonus but gain xp overtime, and can be traded for prestige so you can get more experienced crew, or
// just level up your crew member", "skill tree, each level gets a skill point, the player can auto level officers or pick
// paths on the tree to go down and specialize"; 2026-11-13: "lets do branches, implement that", "implement the tree in the
// game"). Read from data/campaign/crew.json, the one copy the mockup also draws, and checked on load: every prerequisite
// and auto step names a skill of its own tree, every ability is one a rule reads, and the trees are the campaign's stations.
// Plain data and the loader only: the rules that use it are Campaign's (Campaign.Crew.cs).
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Campaign;

/// <summary>One skill on a station's tree. Lane 0 to 2 (a crossover halfway between two), depth 1 (a root) to 5 (the
/// capstone). It opens once every skill in All is learned and at least one in Any (when Any has any); Fx are the abilities
/// it changes, each the key a battle or campaign rule reads and the change per skill.</summary>
public sealed record CrewSkill(string Id, string Name, double Lane, int Depth, string Role, IReadOnlyList<string> All, IReadOnlyList<string> Any,
    IReadOnlyList<(string Ability, double Value)> Fx, string Icon, string Text);

/// <summary>A station's tree: its three lanes' themes, its skills, and the order auto level spends points in.</summary>
public sealed record CrewTree(IReadOnlyList<string> Themes, IReadOnlyList<CrewSkill> Skills, IReadOnlyList<string> Auto)
{
    public CrewSkill? Skill(string id) => Skills.FirstOrDefault(s => s.Id == id);
}

/// <summary>Where a tree's skills stand on its stage, in stage pixels (the mockup's Branches and the game's crew tab both
/// scale it to their space): the stage's size, the first lane's x and the step to the next, each depth's y, the lane names' y.</summary>
public sealed record CrewLayout(float StageW, float StageH, float LaneX0, float LaneDx, IReadOnlyList<float> TierY, float ThemesY)
{
    /// <summary>A skill's place on the stage.</summary>
    public (float X, float Y) At(CrewSkill s) => (LaneX0 + (float)s.Lane * LaneDx, TierY[Math.Clamp(s.Depth - 1, 0, TierY.Count - 1)]);
}

/// <summary>The leveling rules: levels and the ranks they carry, the experience to each next level, what battles and turns
/// give, the rate between experience and prestige, what an officer is worth and costs by level, the pool, and the trees.</summary>
public sealed record CrewDef(int Levels, IReadOnlyList<string> Ranks, IReadOnlyList<int> RankLevel, IReadOnlyList<int> XpNext, int XpBattle,
    IReadOnlyDictionary<string, double> GradeX, int XpTurn, int XpPerPrestige, int WorthPerLevel, int HirePerLevel, IReadOnlyList<int> PoolLevels,
    int Retrain, IReadOnlyDictionary<string, CrewTree> Trees, CrewLayout Layout, IReadOnlyDictionary<string, IReadOnlyList<string>> Covers)
{
    /// <summary>The path, relative to data/.</summary>
    public const string File = "campaign/crew.json";

    /// <summary>Every ability a skill may change, each read by its own rule: the battle's officer abilities (Rules.OfficerSkill's
    /// readers, through Ship.SkillBonus) and the campaign's (standing).</summary>
    public static readonly string[] Abilities =
    {
        "turn", "accel", "speed", "het", "het_safe", "evade", "hit", "charge", "pd", "pd_double", "drift", "shield_rate", "repair_rate",
        "repair_crews", "reactor_e", "scan", "sweep_km", "anticloak", "scan_weak", "jam", "wing_hit", "standing", "aboard", "ambush", "pad_rate",
    };

    /// <summary>The rank an officer of a level holds.</summary>
    public string RankOf(int level)
    {
        int r = 0;
        for (int i = 0; i < RankLevel.Count; i++) if (level >= RankLevel[i]) r = i;
        return Ranks[r];
    }

    /// <summary>Loads and checks data/campaign/crew.json against the campaign's stations.</summary>
    public static CrewDef Load(string text, IReadOnlyList<string> stations)
    {
        StrictJson root = StrictJson.Parse(text, "data/" + File);
        root.Str("_doc", "");
        int levels = root.Int("levels");
        string[] ranks = root.StrList("ranks");
        int[] rankLevel = root.IntList("rank_level");
        int[] xpNext = root.IntList("xp_next");
        string W = root.Where;
        if (levels < 2) throw new DataError($"{W}.levels: at least 2");
        if (ranks.Length == 0 || rankLevel.Length != ranks.Length || rankLevel[0] != 1 || rankLevel.Zip(rankLevel.Skip(1)).Any(t => t.Second <= t.First) || rankLevel[^1] > levels)
            throw new DataError($"{W}.rank_level: one rising level from 1 for each rank, none past levels");
        if (xpNext.Length != levels - 1 || xpNext.Any(x => x <= 0)) throw new DataError($"{W}.xp_next: {levels - 1} amounts above 0, one for each level after the first");
        StrictJson g = root.Obj("grade_x");
        var gradeX = MissionLoader.GradeIds.ToDictionary(id => id, id => g.Num(id));
        g.Done();
        int Pos(string k, int lo) { int v = root.Int(k); if (v < lo) throw new DataError($"{W}.{k}: at least {lo}"); return v; }
        int xpBattle = Pos("xp_battle", 0), xpTurn = Pos("xp_turn", 0), xpPerPrestige = Pos("xp_per_prestige", 1), worth = Pos("worth_per_level", 0),
            hire = Pos("hire_per_level", 0), retrain = Pos("retrain", 0);
        int[] pool = root.IntList("pool_levels");
        if (pool.Any(l => l < 1 || l > levels)) throw new DataError($"{W}.pool_levels: levels from 1 to {levels}");

        StrictJson cv = root.Obj("covers");
        var covers = stations.ToDictionary(st => st, st => (IReadOnlyList<string>)cv.StrList(st));
        cv.Done();
        var battle = covers.Values.SelectMany(v => v).ToList();
        if (battle.Count != OfficersDef.Stations.Length || OfficersDef.Stations.Any(b => battle.Count(x => x == b) != 1))
            throw new DataError($"{W}.covers: every battle station ({string.Join(", ", OfficersDef.Stations)}) under exactly one campaign station");
        StrictJson lo = root.Obj("layout");
        double[] stage = lo.NumList("stage_px"), lane = lo.NumList("lane_x"), tierY = lo.NumList("tier_y");
        var layout = new CrewLayout((float)stage[0], (float)stage[1], (float)lane[0], (float)lane[1], tierY.Select(v => (float)v).ToList(), (float)lo.Num("themes_y"));
        lo.Done();
        if (stage.Length != 2 || lane.Length != 2 || tierY.Length == 0 || tierY.Zip(tierY.Skip(1)).Any(t => t.Second <= t.First) || stage.Concat(lane).Any(v => v <= 0))
            throw new DataError($"{W}.layout: a stage's width and height, a lane's x and step, and rising tier heights, all above 0");
        StrictJson tr = root.Obj("trees");
        var trees = new Dictionary<string, CrewTree>();
        foreach (string st in stations)
        {
            StrictJson t = tr.Obj(st);
            string[] themes = t.StrList("themes");
            var skills = new List<CrewSkill>();
            foreach (StrictJson n in t.ObjList("nodes"))
            {
                var fx = new List<(string, double)>();
                foreach (var (ab, v) in n.NamedNums("fx"))
                {
                    if (!Abilities.Contains(ab)) throw new DataError($"{n.Where}.fx: '{ab}' is no ability a rule reads ({string.Join(", ", Abilities)})");
                    if (v <= 0) throw new DataError($"{n.Where}.fx: '{ab}' needs a value above 0");
                    fx.Add((ab, v));
                }
                string[] all = n.Has("all") ? n.StrList("all") : Array.Empty<string>(), any = n.Has("any") ? n.StrList("any") : Array.Empty<string>();
                var s = new CrewSkill(n.Str("id"), n.Str("name"), n.Num("lane"), n.Int("depth"), n.Str("role"), all, any, fx, n.Str("icon"), n.Str("text"));
                if (fx.Count == 0) throw new DataError($"{n.Where}: a skill changes at least one ability");
                if (s.Depth < 1 || s.Lane < 0 || s.Lane > 2) throw new DataError($"{n.Where}: depth from 1, lane 0 to 2");
                if (!Abilities.Contains(s.Icon)) throw new DataError($"{n.Where}.icon: one of the abilities' icons");
                n.Done();
                skills.Add(s);
            }
            string[] auto = t.StrList("auto");
            t.Done();
            var ids = skills.Select(s => s.Id).ToHashSet();
            if (ids.Count != skills.Count) throw new DataError($"{tr.Where}.{st}: every skill has its own id");
            foreach (CrewSkill s in skills)
            {
                if (s.All.Concat(s.Any).Any(p => !ids.Contains(p) || p == s.Id)) throw new DataError($"{tr.Where}.{st}.{s.Id}: a prerequisite is no other skill of this tree");
                if (s.All.Count + s.Any.Count == 0 && s.Depth != 1) throw new DataError($"{tr.Where}.{st}.{s.Id}: only a root (depth 1) has no prerequisite");
                if (s.All.Concat(s.Any).Any(p => skills.First(q => q.Id == p).Depth >= s.Depth)) throw new DataError($"{tr.Where}.{st}.{s.Id}: a prerequisite sits above it");
            }
            if (auto.Any(a => !ids.Contains(a)) || auto.Distinct().Count() != auto.Length) throw new DataError($"{tr.Where}.{st}.auto: each a skill of this tree, once");
            trees[st] = new CrewTree(themes, skills, auto);
        }
        tr.Done();
        root.Done();
        if (trees.Values.SelectMany(t => t.Skills).Any(s => s.Depth > layout.TierY.Count)) throw new DataError($"{W}.layout.tier_y: a height for every depth");
        return new CrewDef(levels, ranks, rankLevel, xpNext, xpBattle, gradeX, xpTurn, xpPerPrestige, worth, hire, pool, retrain, trees, layout, covers);
    }
}
