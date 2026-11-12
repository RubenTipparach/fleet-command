// Campaign: the campaign's rules on CampaignState (openspec/changes/game-flow-engine), ported from the campaign mockup
// (docs/mockups/game-flow.html, v14): building a new campaign from the map data, the cheapest path and multi-turn routes,
// moving the fleet, the factions' turn (their fleets and freighters), offering missions from the mission files, standing,
// and the shipyard, outfitter and supplies. Plain C#, no Godot (CLAUDE.md section 6.2): the game's screens call these, and
// the tests do too. Missions in battle are MissionRun.cs.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FleetCommand.Campaign;

/// <summary>A route by turn: the first leg is what this turn's move points reach, each later leg a full turn's.</summary>
public sealed record Route(List<(int C, int R)> Path, int Cost, List<List<(int C, int R)>> Legs)
{
    public int Turns => Legs.Count;
}

/// <summary>A fleet's move on the map: the sectors it passed, start first, for the view to glide it along.</summary>
public sealed record FleetMove(string Name, List<(int C, int R)> Steps);

public sealed class Campaign
{
    public CampaignData Data { get; }
    public CampaignState S { get; }
    public Mulberry32 R { get; }

    private Campaign(CampaignData data, CampaignState s)
    {
        Data = data;
        S = s;
        R = new Mulberry32(s.Rng);
    }

    private CampRules Ru => Data.Rules;

    // ---- Building and saving ---------------------------------------------------------------------------------------------

    /// <summary>A new campaign for a side: the map from the data (territory grown from seeds, jittered by the seed), one ship
    /// of the side's smallest tier with marines aboard, standing, freighters, officers and the first missions. Without
    /// unfinished, the factions with no territory on the map (CampaignData.Finished) are left out of it.</summary>
    public static Campaign New(CampaignData data, string side, uint? seed = null, bool unfinished = true)
    {
        if (!data.Factions.TryGetValue(side, out CampFaction? fac) || !fac.Playable) throw new ArgumentException($"'{side}' is not a playable faction");
        CampRules ru = data.Rules;
        MapDef md = data.Map;
        uint sd = seed ?? (uint)ru.Seed;
        var s = new CampaignState { Side = side, Seed = sd, Rng = sd, Cols = ru.Cols, Rows = ru.Rows, Unfinished = unfinished };
        var c0 = new Campaign(data, s);
        Mulberry32 R = c0.R;
        for (int c = 0; c < ru.Cols; c++)
            for (int r = 0; r < ru.Rows; r++)
            {
                string best = "neutral";
                double bd = 1e9;
                foreach (var (f, sc, sr) in md.Seeds)
                {
                    double d = HexGrid.Dist(c, r, sc, sr) + R.Next() * md.SeedJitter;
                    if (d < bd) { bd = d; best = f; }
                }
                // one draw a sector, kept from the first map's scattered terrain so the borders stay as approved
                R.Next();
                s.Sectors.Add(new Sector { C = c, R = r, Owner = best });
            }
        c0.MakeSpace();
        foreach (Place p in md.Places)
        {
            Sector h = s.Hex(p.C, p.R);
            h.Terrain = "open";
            if (p.Kind == "planet") h.Planet = p.Name; else h.Station = p.Name;
            if (p.Owner != "") h.Owner = p.Owner;
        }
        for (int i = 0; i < md.Fleets.Count; i++)
        {
            FleetDef g = md.Fleets[i];
            s.Groups.Add(new FactionFleet
            {
                Name = g.Name, Fac = g.Fac, C = g.C, R = g.R, Size = g.Size, Commander = g.Commander, HomeC = g.C, HomeR = g.R, Every = 1 + i % 3,
                Dir = g.Fac == side ? Math.PI / 2 : -Math.PI / 2,
            });
        }
        Sector home = s.Sectors.First(h => h.PortName == fac.Home);
        s.HomeC = s.AtC = home.C;
        s.HomeR = s.AtR = home.R;
        s.Credits = ru.StartCredits;
        s.Mp = ru.MovePoints;
        s.Dir = Math.PI / 2;
        foreach (string f in data.Factions.Keys.Where(f => f != "neutral" && (unfinished || data.Finished(f))))
            s.Standing[f] = f == side ? ru.StandingOwn : f == fac.Rival ? ru.StandingRival : 0;
        CampShip first = c0.NewShip(data.HullsOf(side).First().Id, fac.Prefix + fac.Names[0]);
        first.Supplies["marines"] = Math.Min(ru.StartMarines, c0.SupplyMax(first, "marines"));
        s.Fleet.Add(first);
        c0.MakeFreighters();
        c0.Reveal();
        c0.RollOfficers();
        if (data.Missions.ContainsKey("shakedown")) c0.OfferMission("shakedown");
        for (int i = 0; i < ru.MissionsStart; i++) c0.OfferMission();
        c0.Sync();
        return c0;
    }

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false, IncludeFields = true };

    /// <summary>The campaign as JSON, for the save file.</summary>
    public string Save() { Sync(); return JsonSerializer.Serialize(S, Json); }

    /// <summary>A saved campaign; throws a FormatException naming what is wrong.</summary>
    public static Campaign Load(CampaignData data, string json)
    {
        CampaignState s = JsonSerializer.Deserialize<CampaignState>(json, Json) ?? throw new FormatException("the save is empty");
        if (s.Version != 1) throw new FormatException($"save version {s.Version} is not 1");
        if (s.Sectors.Count != s.Cols * s.Rows || s.Cols < 1) throw new FormatException("the save's map is the wrong size");
        if (!data.Factions.ContainsKey(s.Side)) throw new FormatException($"the save's side '{s.Side}' is no faction");
        var c = new Campaign(data, s);
        // a save from before the fog of war knows nothing yet: what is seen now is revealed
        c.Reveal();
        return c;
    }

    /// <summary>Writes the random generator's state back into the state (before saving).</summary>
    private void Sync() => S.Rng = R.State;

    // ---- Space: star systems and the deep between them ------------------------------------------------------------------

    /// <summary>Lays out space (owner, 2026-11-07: "space is kinda random right now. we should have stars, with planets
    /// asteriods and nebula clustered around them", "rogue planets, dust clouds, and asteroids with no stars are a thing
    /// too"): each system's star, its asteroid belt (a share of the ring belt_r sectors out) and its nebula grown beside it;
    /// then dust clouds and asteroid fields in the deep, clear of every star. Ports are placed over it afterwards.</summary>
    private void MakeSpace()
    {
        MapDef md = Data.Map;
        var places = md.Places.Select(p => (p.C, p.R)).ToHashSet();
        var stars = md.Stars.ToHashSet();
        bool Free((int C, int R) k) => !places.Contains(k) && !stars.Contains(k) && Hex(k.C, k.R).Terrain == "open";
        foreach (var (c, r) in stars) Hex(c, r).Terrain = "star";
        foreach (StarSystem y in md.Systems)
        {
            if (y.BeltR > 0)
                foreach (Sector h in S.Sectors.Where(h => HexGrid.Dist(h.C, h.R, y.C, y.R) == y.BeltR))
                    if (R.Next() < y.BeltShare && Free((h.C, h.R))) h.Terrain = "asteroids";
            if (y.Nebula > 0)
            {
                var start = S.Sectors.Where(h => HexGrid.Dist(h.C, h.R, y.C, y.R) is 2 or 3 && Free((h.C, h.R))).ToList();
                if (start.Count > 0) Grow(start[R.Below(start.Count)], y.Nebula, "nebula", Free);
            }
        }
        DeepSpace dp = md.Deep;
        bool Deep((int C, int R) k) => Free(k) && stars.All(t => HexGrid.Dist(k.C, k.R, t.C, t.R) >= dp.ClearOfStars);
        for (int i = 0; i < dp.DustClouds + dp.Fields; i++)
        {
            bool dust = i < dp.DustClouds;
            var start = S.Sectors.Where(h => Deep((h.C, h.R))).ToList();
            if (start.Count == 0) break;
            int n = dust ? dp.DustMin + R.Below(dp.DustMax - dp.DustMin + 1) : dp.FieldMin + R.Below(dp.FieldMax - dp.FieldMin + 1);
            Grow(start[R.Below(start.Count)], n, dust ? "nebula" : "asteroids", Deep);
        }
    }

    // Grows a patch of terrain from a sector to n sectors, each new one a random free neighbour of the patch so far
    // (neighbours in the grid's fixed order, so the same seed grows the same patch).
    private void Grow(Sector from, int n, string terrain, Func<(int C, int R), bool> free)
    {
        var patch = new List<Sector> { from };
        from.Terrain = terrain;
        while (patch.Count < n)
        {
            var edge = patch.SelectMany(h => HexGrid.Neighbours(h.C, h.R, S.Cols, S.Rows)).Distinct().Where(free).ToList();
            if (edge.Count == 0) break;
            var (c, r) = edge[R.Below(edge.Count)];
            Sector h = Hex(c, r);
            h.Terrain = terrain;
            patch.Add(h);
        }
    }

    /// <summary>Whether a planet is a rogue: no star within three sectors.</summary>
    public bool IsRogue(Sector h) => h.Planet != null && Data.Map.Stars.All(t => HexGrid.Dist(h.C, h.R, t.C, t.R) > 3);

    // ---- The fog of war ----------------------------------------------------------------------------------------------------

    /// <summary>The sectors seen now (the fog's visible state; owner, 2026-11-07: "fog of war has three states: unexplored,
    /// revealed, and visible"): your empire's own sectors, those within your fleet's sight, and those within friendly
    /// ships' sight (your faction's fleets and freighters, and your allies').</summary>
    public HashSet<(int C, int R)> Visible()
    {
        Sight sg = Data.Map.Sight;
        var eyes = new List<(int C, int R, int Range)> { (S.AtC, S.AtR, sg.Fleet) };
        foreach (FactionFleet g in S.Groups.Where(g => Friendly(g.Fac))) eyes.Add((g.C, g.R, sg.Friendly));
        foreach (Freighter f in S.Freighters.Where(f => Friendly(f.Fac) && f.Route.Count > 0))
        {
            int[] at = f.Route[Math.Clamp(f.I, 0, f.Route.Count - 1)];
            eyes.Add((at[0], at[1], sg.Friendly));
        }
        var seen = new HashSet<(int, int)>();
        foreach (Sector h in S.Sectors)
            if (h.Owner == S.Side || eyes.Any(e => HexGrid.Dist(h.C, h.R, e.C, e.R) <= e.Range)) seen.Add((h.C, h.R));
        return seen;
    }

    /// <summary>A faction whose ships share what they see with you: your own, or an ally (standing at AllyStanding or more).</summary>
    public bool Friendly(string fac) => fac == S.Side || SideOf(fac) == "ally";

    /// <summary>Whether a sector is seen now.</summary>
    public bool IsVisible(int c, int r) => Visible().Contains((c, r));

    /// <summary>Marks every sector seen now as revealed, with the empire that holds it ("Any revealed area updates the tile
    /// color (empire controled)"). Called when a campaign starts or loads, after each sector the fleet flies, and after the
    /// factions' turn.</summary>
    public void Reveal()
    {
        foreach (var (c, r) in Visible()) See(Hex(c, r));
    }

    private static void See(Sector h) { h.Known = true; h.KnownOwner = h.Owner; }

    // ---- Map -------------------------------------------------------------------------------------------------------------

    public Sector Hex(int c, int r) => S.Hex(c, r);
    public bool OnMap(int c, int r) => c >= 0 && r >= 0 && c < S.Cols && r < S.Rows;
    public Sector At => Hex(S.AtC, S.AtR);
    public Sector Home => Hex(S.HomeC, S.HomeR);
    public CampFaction Side => Data.Factions[S.Side];
    public string? Rival => Side.Rival;
    /// <summary>The faction this side mostly fights: its rival, or the pirates.</summary>
    public string Enemy => Rival ?? "vos";
    public Terrain TerrainOf(Sector h) => Data.Terrain[h.Terrain];
    public int Standing(string fac) => S.Standing.TryGetValue(fac, out int v) ? v : 0;

    public bool Own(Sector h) => h.Owner == S.Side;
    public bool Hostile(Sector h) => HostileTo(h.Owner);
    /// <summary>A faction you are at odds with: below 0 in standing, your rival, or the pirates.</summary>
    public bool HostileTo(string fac) => fac != S.Side && fac != "neutral" && (Standing(fac) < 0 || fac == Rival || fac == "vos");
    /// <summary>A faction you are on good terms with: above 0 in standing and not hostile.</summary>
    public bool FriendlyTo(string fac) => fac != S.Side && fac != "neutral" && Standing(fac) > 0 && !HostileTo(fac);
    /// <summary>Your own port: shipyard, outfitter and recruiting.</summary>
    public bool IsPort(Sector h) => h.IsPort && h.Owner == S.Side;
    /// <summary>Another faction's port that deals with you (selling only) while your standing with it is 0 or above.</summary>
    public bool IsMarket(Sector h) => h.IsPort && h.Owner != S.Side && h.Owner != "neutral" && Standing(h.Owner) >= 0;
    /// <summary>"own", "ally" (standing at or above the ally line) or null: whose freighters and routes the map shows.</summary>
    /// <summary>What a faction's ports open to you (owner, 2026-11-08: "Friendly factions: allows increasing privilege to use
    /// friendly stations and outposts. Start with parts, supplies, weapons, then ships, and finally officers"): everything at
    /// your own; at another's, each grant whose standing you have reached, nothing while it is hostile to you.</summary>
    public IReadOnlyList<string> GrantsOf(string fac)
    {
        if (fac == S.Side) return CampaignData.Grants;
        if (fac == "neutral" || fac == Rival || Standing(fac) < 0) return Array.Empty<string>();
        return Ru.Privileges.Where(p => Standing(fac) >= p.Standing).Select(p => p.Grant).ToList();
    }

    /// <summary>The standing a faction's ports need for a grant.</summary>
    public int StandingFor(string grant) => Ru.Privileges.First(p => p.Grant == grant).Standing;

    /// <summary>Whether the port where the fleet stands gives a grant: your own port gives all, a friendly faction's port
    /// what your standing with it has reached.</summary>
    public bool Allows(string grant) => At.IsPort && GrantsOf(At.Owner).Contains(grant);

    /// <summary>Whose ships the shipyard where the fleet stands builds for you: the port's own faction's.</summary>
    public string YardSide => At.IsPort && At.Owner != "neutral" ? At.Owner : S.Side;

    public string? SideOf(string fac) => fac == S.Side ? "own" : Standing(fac) >= Ru.AllyStanding ? "ally" : null;
    public FactionFleet? GroupAt(int c, int r) => S.Groups.FirstOrDefault(g => g.C == c && g.R == r);
    public IEnumerable<Sector> Passable => S.Sectors.Where(h => !TerrainOf(h).Impassable);
    public Sector? PortNamed(string name) => S.Sectors.FirstOrDefault(h => h.PortName == name);

    public void ChangeStanding(string fac, int d)
    {
        if (fac == "neutral" || !S.Standing.ContainsKey(fac)) return;
        S.Standing[fac] = Math.Clamp(S.Standing[fac] + d, -100, 100);
    }

    /// <summary>The cheapest path by move cost within a budget of move points (the player's moves, the factions' fleets and
    /// the freight routes all use it); null when there is none, or from a sector to itself. A star is never entered.</summary>
    public (List<(int C, int R)> Path, int Cost)? CheapestPath(int fc, int fr, int tc, int tr, int budget = int.MaxValue)
    {
        if (fc == tc && fr == tr) return null;
        var dist = new Dictionary<(int, int), int> { [(fc, fr)] = 0 };
        var prev = new Dictionary<(int, int), (int, int)>();
        var open = new List<(int, int)> { (fc, fr) };
        while (open.Count > 0)
        {
            // the open sector nearest the start, first added on a tie (the mockup's stable sort)
            int bi = 0;
            for (int i = 1; i < open.Count; i++) if (dist[open[i]] < dist[open[bi]]) bi = i;
            var k = open[bi];
            open.RemoveAt(bi);
            if (k == (tc, tr)) break;
            foreach (var n in HexGrid.Neighbours(k.Item1, k.Item2, S.Cols, S.Rows))
            {
                Terrain t = TerrainOf(Hex(n.C, n.R));
                if (t.Impassable) continue;
                int nd = dist[k] + t.Cost;
                if (nd <= budget && (!dist.TryGetValue(n, out int od) || nd < od)) { dist[n] = nd; prev[n] = k; open.Add(n); }
            }
        }
        if (!dist.ContainsKey((tc, tr))) return null;
        var path = new List<(int, int)>();
        for (var k = (tc, tr); k != (fc, fr); k = prev[k]) path.Insert(0, k);
        return (path, dist[(tc, tr)]);
    }

    /// <summary>The fleet's route to a sector however far, split into legs by turn; null when there is none.</summary>
    public Route? PathTo(int tc, int tr)
    {
        var full = CheapestPath(S.AtC, S.AtR, tc, tr);
        if (full == null) return null;
        var legs = new List<List<(int, int)>> { new() };
        int left = S.Mp;
        foreach (var k in full.Value.Path)
        {
            int cost = TerrainOf(Hex(k.C, k.R)).Cost;
            if (cost > left) { legs.Add(new()); left = Ru.MovePoints; }
            legs[^1].Add(k);
            left -= cost;
        }
        return new Route(full.Value.Path, full.Value.Cost, legs);
    }

    /// <summary>Flies this turn's leg of a route; a longer route becomes the fleet's order, flown on after each End turn.
    /// Returns the sectors passed (start first), empty when nothing could move this turn.</summary>
    public List<(int C, int R)> Move(Route route)
    {
        (int C, int R) target = route.Path[^1];
        var leg = route.Legs[0];
        var steps = new List<(int, int)> { (S.AtC, S.AtR) };
        foreach (var k in leg)
        {
            S.Mp -= TerrainOf(Hex(k.C, k.R)).Cost;
            S.Dir = HexGrid.Heading(S.AtC, S.AtR, k.C, k.R);
            S.AtC = k.C;
            S.AtR = k.R;
            steps.Add(k);
            // the fleet sees as it goes: everything within its sight of each sector it passes is revealed
            Reveal();
        }
        bool arrived = S.AtC == target.C && S.AtR == target.R;
        S.OrderC = arrived ? null : target.C;
        S.OrderR = arrived ? null : target.R;
        return steps;
    }

    public void CancelOrder() { S.OrderC = null; S.OrderR = null; }

    // ---- The factions' turn ----------------------------------------------------------------------------------------------

    /// <summary>Ends the turn: the factions move, move points refill, contracts age and expire (accepted and story missions
    /// wait), new contracts are offered and the officers on offer change. Returns the faction fleets' moves.</summary>
    public List<FleetMove> EndTurn()
    {
        S.Turn++;
        Alarm();
        List<FleetMove> moves = FactionsMove();
        Reveal();
        S.Mp = Ru.MovePoints;
        // an encounter goes with the freighter it was met with, taken or not
        foreach (Offer m in S.Missions.Where(m => ScriptOf(m).Encounter || ScriptOf(m).Event)) S.Active.Remove(m.Id);
        S.Missions = S.Missions.Where(m => !ScriptOf(m).Encounter && !ScriptOf(m).Event && (m.Turns == null || S.Active.Contains(m.Id) || --m.Turns > 0)).ToList();
        int n = 1 + R.Below(2);
        for (int i = 0; i < n && S.Missions.Count < Ru.MissionsMax; i++) OfferMission();
        RollOfficers();
        Sync();
        return moves;
    }

    /// <summary>The fleet's standing order after End turn: its next leg, or null when it has none (or can no longer go).</summary>
    public Route? OrderRoute()
    {
        if (S.OrderC is not int c || S.OrderR is not int r) return null;
        Route? res = PathTo(c, r);
        if (res == null) CancelOrder();
        return res;
    }

    /// <summary>Each faction fleet moves on its own cadence toward a sector its faction sends it to (raiders roam, the rest keep
    /// near home), one or two sectors at a time; each freighter steps along its route. A mission that follows a fleet goes with it.</summary>
    public List<FleetMove> FactionsMove()
    {
        MapDef md = Data.Map;
        var all = Passable.ToList();
        var moves = new List<FleetMove>();
        foreach (FactionFleet g in S.Groups.ToList())
        {
            if (g.Pursuit) { if (Hunt(g) is { } hm) moves.Add(hm); continue; }
            bool atGoal = g.GoalC == g.C && g.GoalR == g.R;
            if ((S.Turn + g.Every) % g.Every != 0 || (g.GoalC != null && atGoal && R.Next() < 0.5)) continue;
            if (g.GoalC == null || atGoal)
            {
                int range = g.Fac == "vos" ? md.RangePirate : g.Size == 1 ? md.RangeLone : md.RangeGroup;
                var pool = all.Where(h => HexGrid.Dist(h.C, h.R, g.HomeC, g.HomeR) <= range && (g.Fac == "vos" || h.Owner == g.Fac || h.Owner == "neutral")).ToList();
                Sector goal = pool.Count > 0 ? pool[(int)Math.Floor(R.Next() * pool.Count)] : Hex(g.HomeC, g.HomeR);
                g.GoalC = goal.C;
                g.GoalR = goal.R;
            }
            var res = CheapestPath(g.C, g.R, g.GoalC!.Value, g.GoalR!.Value);
            if (res == null) { g.GoalC = null; g.GoalR = null; continue; }
            var steps = res.Value.Path.Take(g.Size == 1 ? 2 : 1).ToList();
            var from = (g.C, g.R);
            var before = steps.Count > 1 ? steps[^2] : from;
            g.Dir = HexGrid.Heading(before.Item1, before.Item2, steps[^1].C, steps[^1].R);
            g.C = steps[^1].C;
            g.R = steps[^1].R;
            foreach (Offer m in S.Missions.Where(m => m.Group == g.Name)) { m.C = g.C; m.R = g.R; }
            moves.Add(new FleetMove(g.Name, new[] { from }.Concat(steps).ToList()));
        }
        foreach (Freighter f in S.Freighters)
        {
            if (f.Out > 0) { f.Out--; continue; }   // off her route while she is replaced
            if (f.I + f.Step < 0 || f.I + f.Step >= f.Route.Count) f.Step = -f.Step;
            int[] a = f.Route[f.I];
            f.I += f.Step;
            int[] b = f.Route[f.I];
            f.Dir = HexGrid.Heading(a[0], a[1], b[0], b[1]);
        }
        return moves;
    }

    // ---- Pursuit: a hostile faction hunts a fleet in its territory ---------------------------------------------------------

    /// <summary>The mission file a fleet caught by hunters fights (data/missions/pursuit.json).</summary>
    public const string PursuitScript = "pursuit";

    /// <summary>A faction's alarm at your fleet now.</summary>
    public int HeatOf(string fac) => S.Heat.GetValueOrDefault(fac);

    /// <summary>The alarm level a faction has reached: how many of rules.pursuit.levels its alarm is at or past.</summary>
    public int AlarmLevel(string fac) => Ru.Pursuit.Levels.Count(l => HeatOf(fac) >= l);

    /// <summary>How many hunters a faction sends at an alarm level (0 at none).</summary>
    public int HuntersAt(int level) => level <= 0 ? 0 : Math.Min(Ru.Pursuit.SizeMax, Ru.Pursuit.SizeBase + Ru.Pursuit.SizePerLevel * (level - 1));

    /// <summary>The hunters a faction has out after your fleet, or null.</summary>
    public FactionFleet? HuntersOf(string fac) => S.Groups.FirstOrDefault(g => g.Pursuit && !g.Recalled && g.Fac == fac);

    /// <summary>The alarm at End turn (owner, 2026-11-08: "depending on hostility, enemy factions will persue you with larger
    /// and larger fleets if you cross into hostile territory"): ending a turn in a hostile faction's sector raises its alarm,
    /// more the lower your standing with it; everywhere else every alarm cools. Each level reached sends hunters, or adds
    /// ships to those already out; an alarm that dies down recalls them.</summary>
    private void Alarm()
    {
        PursuitRules pr = Ru.Pursuit;
        Sector at = At;
        string? hot = Hostile(at) && Data.Factions.ContainsKey(at.Owner) ? at.Owner : null;
        foreach (string f in Data.Factions.Keys.Where(f => f != "neutral" && f != S.Side).OrderBy(f => f, StringComparer.Ordinal))
        {
            int heat = HeatOf(f);
            heat = f == hot ? heat + pr.HeatPerTurn + Math.Max(0, -Standing(f)) / pr.HostilityStep : Math.Max(0, heat - pr.CoolPerTurn);
            if (heat > 0) S.Heat[f] = heat; else S.Heat.Remove(f);
            int size = HuntersAt(AlarmLevel(f));
            FactionFleet? g = HuntersOf(f);
            if (size == 0) { if (g != null) { g.Recalled = true; g.GoalC = g.HomeC; g.GoalR = g.HomeR; } continue; }
            if (g == null)
            {
                // sent from the faction's port nearest the fleet (its home sector if it has none)
                Sector from = S.Sectors.Where(h => h.IsPort && h.Owner == f).OrderBy(h => HexGrid.Dist(h.C, h.R, S.AtC, S.AtR)).ThenBy(h => h.C).ThenBy(h => h.R).FirstOrDefault()
                    ?? at;
                S.Groups.Add(new FactionFleet
                {
                    Name = $"{Data.Factions[f].Short} hunters", Fac = f, C = from.C, R = from.R, HomeC = from.C, HomeR = from.R, Size = size, Pursuit = true, Every = 1,
                });
            }
            else if (g.Size < size) g.Size = size;
        }
    }

    // hunters close on the fleet a few sectors a turn; recalled hunters go home and disband there
    private FleetMove? Hunt(FactionFleet g)
    {
        (int C, int R) goal = g.Recalled ? (g.HomeC, g.HomeR) : (S.AtC, S.AtR);
        if (g.Recalled && (g.C, g.R) == goal) { S.Groups.Remove(g); return null; }
        if ((g.C, g.R) == goal) return null;
        var res = CheapestPath(g.C, g.R, goal.C, goal.R);
        if (res == null) return null;
        var steps = res.Value.Path.Take(Ru.Pursuit.Speed).ToList();
        if (steps.Count == 0) return null;
        var from = (g.C, g.R);
        var before = steps.Count > 1 ? steps[^2] : from;
        g.Dir = HexGrid.Heading(before.Item1, before.Item2, steps[^1].C, steps[^1].R);
        (g.C, g.R) = (steps[^1].C, steps[^1].R);
        foreach (Offer m in S.Missions.Where(m => m.Group == g.Name)) { m.C = g.C; m.R = g.R; }
        return new FleetMove(g.Name, new[] { from }.Concat(steps).ToList());
    }

    /// <summary>Hunters in the fleet's sector catch it: their fight is offered there (one per hunting group, the one already
    /// offered this turn kept), handed the group's name, faction and size. Returns the newly offered fights.</summary>
    public List<Offer> Caught()
    {
        var made = new List<Offer>();
        if (!Data.Missions.ContainsKey(PursuitScript)) return made;
        foreach (FactionFleet g in S.Groups.Where(g => g.Pursuit && !g.Recalled && g.C == S.AtC && g.R == S.AtR).ToList())
        {
            if (S.Missions.Any(m => m.Script == PursuitScript && m.Value("hunters") as string == g.Name)) continue;
            Offer o = MakeOffer(Data.Missions[PursuitScript], At, new Dictionary<string, object>
                { ["hunters"] = g.Name, ["hunter_fac"] = g.Fac, ["hunter_size"] = (double)g.Size });
            o.Group = g.Name;
            made.Add(o);
        }
        if (made.Count > 0) Sync();
        return made;
    }

    /// <summary>The hunters a pursuit fight was offered against, or null.</summary>
    public FactionFleet? HuntersOf(Offer m) => S.Groups.FirstOrDefault(g => g.Pursuit && g.Name == m.Value("hunters") as string);

    private void MakeFreighters()
    {
        MapDef md = Data.Map;
        for (int i = 0; i < md.FreightRoutes.Count; i++)
        {
            var (fac, a, b) = md.FreightRoutes[i];
            Sector A = PortNamed(a)!, B = PortNamed(b)!;
            var res = CheapestPath(A.C, A.R, B.C, B.R);
            var route = new List<int[]> { new[] { A.C, A.R } };
            if (res != null) route.AddRange(res.Value.Path.Select(k => new[] { k.C, k.R }));
            S.Freighters.Add(new Freighter { Fac = fac, Name = md.FreighterNames[i % md.FreighterNames.Count], From = a, To = b, Route = route, I = i % route.Count });
        }
    }

    /// <summary>The sectors a faction's freighters run through.</summary>
    public HashSet<(int, int)> RoutesOf(string fac) =>
        S.Freighters.Where(f => f.Fac == fac).SelectMany(f => f.Route).Select(k => (k[0], k[1])).ToHashSet();

    /// <summary>The officers on offer at each station (owner, 2026-11-06: "for each crew slot, I should be able to click on it
    /// and always geta list, even when I hire someone. Always 3 experienced of various levels and 5 cadets"): per station,
    /// data's experienced count at different levels above cadet, then data's cadet count, each with a name no one else has.</summary>
    private void RollOfficers()
    {
        OfficerDef od = Data.Officers;
        S.OfficerPool = new List<Officer>();
        foreach (string st in od.Stations)
        {
            var levels = Enumerable.Range(1, od.Skills.Count - 1).Select(i => (R.Next(), i)).OrderBy(x => x.Item1).Select(x => x.i)
                .Take(od.Experienced).OrderBy(i => i).ToList();
            while (levels.Count < od.Experienced) levels.Add(1 + R.Below(od.Skills.Count - 1));
            foreach (int sk in levels) S.OfficerPool.Add(NewOfficer(st, sk));
            for (int i = 0; i < od.Cadets; i++) S.OfficerPool.Add(NewOfficer(st, 0));
        }
    }

    /// <summary>A new officer for a station at a skill level (0 is a cadet), named from data's given and family names so
    /// no one in the crew or on offer shares the name.</summary>
    private Officer NewOfficer(string station, int skill)
    {
        OfficerDef od = Data.Officers;
        string name = "";
        for (int tries = 0; tries < 64; tries++)
        {
            name = od.Given[R.Below(od.Given.Count)] + " " + od.Family[R.Below(od.Family.Count)];
            if (!S.Crew.Concat(S.OfficerPool).Any(o => o.Name == name)) break;
        }
        return new Officer { Name = name, Station = station, Skill = od.Skills[skill].Skill, Cost = od.Skills[skill].Cost };
    }

    /// <summary>Whether an officer is a cadet (the lowest skill level).</summary>
    public bool IsCadet(Officer o) => o.Skill == Data.Officers.Skills[0].Skill;

    /// <summary>The officers on offer for one station, experienced first by level, then the cadets.</summary>
    public List<Officer> OnOffer(string station) => S.OfficerPool.Where(o => o.Station == station).ToList();

    /// <summary>Puts an officer back on offer at their station in place of the last one of the same kind, so each station
    /// keeps its count of experienced officers and cadets.</summary>
    private void BackOnOffer(Officer o)
    {
        int at = S.OfficerPool.FindLastIndex(x => x.Station == o.Station && IsCadet(x) == IsCadet(o));
        if (at >= 0) S.OfficerPool[at] = o; else S.OfficerPool.Add(o);
    }

    // ---- Missions on offer -----------------------------------------------------------------------------------------------

    /// <summary>Offers a mission: the script's own choice of sector, never one that already has a mission. With no id, a
    /// random contract. <paramref name="at"/> pins the sector and hands the mission data. Returns null when none fits.</summary>
    public Offer? OfferMission(string? id = null, (int C, int R)? at = null, IReadOnlyDictionary<string, object>? data = null)
    {
        var contracts = Data.Contracts.ToList();
        double total = contracts.Sum(m => m.Weight);
        // a contract picked by its weight (one draw, as before)
        string Pick()
        {
            double x = R.Next() * total;
            foreach (MissionScript m in contracts) { if (x < m.Weight) return m.Id; x -= m.Weight; }
            return contracts[^1].Id;
        }
        for (int tries = 0; tries < 20; tries++)
        {
            string sid = id ?? Pick();
            MissionScript def = Data.Missions[sid];
            var pool = (at is { } p ? new List<Sector> { Hex(p.C, p.R) } : Where(def))
                .Where(h => !S.Missions.Any(m => m.C == h.C && m.R == h.R)).ToList();
            if (pool.Count == 0) { if (id != null) return null; continue; }
            return MakeOffer(def, pool[(int)Math.Floor(R.Next() * pool.Count)], data);
        }
        return null;
    }

    // A mission offered in a sector, with the data it is handed: its terms rolled, its sector seen, its setup run.
    private Offer MakeOffer(MissionScript def, Sector h, IReadOnlyDictionary<string, object>? data)
    {
        var m = new Offer
        {
            Id = ++S.Mid, Script = def.Id, C = h.C, R = h.R, Turns = def.Story ? null : def.Turns,
            Strength = Ru.Strengths[(int)Math.Floor(R.Next() * Ru.Strengths.Count)],
            Reward = def.Reward + (def.Story ? 0 : Math.Floor(R.Next() * Ru.RewardSteps) * Ru.RewardStep),
        };
        if (data != null) foreach (var (k, v) in data) m.Set(k, v);
        // a mission's intel shows its sector on the map
        See(h);
        if (def.FollowGroup) m.Group = GroupAt(h.C, h.R)?.Name;
        var sc = new OfferScope(this, m);
        foreach (SetupStep st in def.Setup)
            m.Set(st.Set, st.Pick != null ? st.Pick[(int)Math.Floor(R.Next() * st.Pick.Count)] : st.Value!.Eval(sc));
        S.Missions.Add(m);
        return m;
    }

    // ---- Freighters met in a sector -------------------------------------------------------------------------------------

    /// <summary>The mission files of the two encounters (data/missions/escort.json and raid.json).</summary>
    public const string EscortScript = "escort", RaidScript = "raid";

    /// <summary>The freighters in a sector now; one off her route is in none.</summary>
    public List<Freighter> FreightersAt(int c, int r) => S.Freighters.Where(f => f.Out == 0 && f.Here() == (c, r)).ToList();

    /// <summary>What meeting a freighter offers: an escort for your own faction's or an ally's, a raid on anyone else's.</summary>
    public string EncounterOf(Freighter f) => Friendly(f.Fac) ? EscortScript : RaidScript;

    /// <summary>The encounter with a freighter in the fleet's sector (owner, 2026-11-07: "no mision flag needed. Just whenever
    /// you are on the same tile as freighter, you get a mission option"): the one already offered this turn, or a new one,
    /// handed her name (ship) and faction (freighter_fac). Null when she is not in the fleet's sector.</summary>
    public Offer? Meet(Freighter f)
    {
        if (f.Out > 0 || f.Here() != (S.AtC, S.AtR)) return null;
        Offer? had = S.Missions.FirstOrDefault(m => ScriptOf(m).Encounter && m.Value("ship") as string == f.Name && m.Value("freighter_fac") as string == f.Fac);
        if (had != null) return had;
        Offer met = MakeOffer(Data.Missions[EncounterOf(f)], At, new Dictionary<string, object> { ["ship"] = f.Name, ["freighter_fac"] = f.Fac });
        Sync();
        return met;
    }

    /// <summary>The freighter an encounter was met with, or null.</summary>
    public Freighter? FreighterOf(Offer m) =>
        S.Freighters.FirstOrDefault(f => f.Name == m.Value("ship") as string && f.Fac == m.Value("freighter_fac") as string);

    /// <summary>What the cargo of every freighter taken pays when cashed out, credits.</summary>
    public int HaulValue => S.Fleet.Sum(s => s.Haul);

    /// <summary>Cashes out the cargo of the freighters taken (owner, 2026-11-07: "capturing enemy freighter: bring it to a
    /// station to cash out"), at your own port or one that deals with you, never to the faction she was taken from: her
    /// cargo pays its haul, and she is handed over with it. Returns why not, or null.</summary>
    public string? CashOut()
    {
        var hauls = S.Fleet.Where(s => s.Haul > 0).ToList();
        if (hauls.Count == 0) return "No cargo aboard.";
        Sector at = At;
        string buyer = IsPort(at) ? S.Side : IsMarket(at) ? at.Owner : "";
        if (buyer == "") return "Cargo is cashed out at a port that deals with you.";
        var sold = hauls.Where(s => s.Maker != buyer).ToList();
        if (sold.Count == 0) return $"The {Data.Factions[buyer].Short} will not buy back their own cargo.";
        foreach (CampShip s in sold)
        {
            S.Credits += s.Haul;
            S.Fleet.Remove(s);
            S.Log.Add($"Turn {S.Turn}: the {s.Name}'s cargo sold at {at.PortName} for {s.Haul} cr.");
        }
        // the freighters sold were all the fleet had (its warships lost taking them): a new one, as after a fleet lost
        if (Recommission() is { } fresh) S.Log.Add($"Turn {S.Turn}: the {fresh.Name} commissioned at {Home.PortName} to replace the fleet.");
        return null;
    }

    /// <summary>A fleet with no ships left is given a new one of its side's smallest hull, at home; null when it has ships.
    /// After a battle that lost them all, and after cashing out freighters that were all it had.</summary>
    public CampShip? Recommission()
    {
        if (S.Fleet.Count > 0) return null;
        CampShip fresh = NewShip(Data.HullsOf(S.Side).First().Id, Side.Prefix + Side.Names[S.Battles % Side.Names.Count]);
        S.Fleet.Add(fresh);
        S.AtC = S.HomeC;
        S.AtR = S.HomeR;
        return fresh;
    }

    // ---- Repair: ships do not mend on their own ---------------------------------------------------------------------------

    /// <summary>The supply that is the repair kit (data/campaign/campaign.json supplies, "Repair kits").</summary>
    public const string KitSupply = "parts";

    /// <summary>The repair kits aboard the whole fleet.</summary>
    public int Kits => S.Fleet.Sum(s => s.Supplies.GetValueOrDefault(KitSupply));

    /// <summary>Patches a ship's hull in the field with one repair kit from the fleet's stores (its own first), anywhere,
    /// between battles (owner, 2026-11-07: "ships dont auto heal, you have to repair them at stations", "or use you repair
    /// kits"): rules.kit_repair_pct of the hull, never past whole. Returns why not, or null.</summary>
    public string? FieldRepair(CampShip s)
    {
        if (s.HullPct >= 100) return $"The {s.Name} needs no repair.";
        CampShip? from = s.Supplies.GetValueOrDefault(KitSupply) > 0 ? s : S.Fleet.FirstOrDefault(x => x.Supplies.GetValueOrDefault(KitSupply) > 0);
        if (from == null) return "No repair kits aboard: buy them at a port (Supplies).";
        from.Supplies[KitSupply]--;
        s.HullPct = Math.Min(100, s.HullPct + Ru.KitRepairPct);
        return null;
    }

    /// <summary>The sectors a mission may be offered in: every passable sector its where holds for.</summary>
    public List<Sector> Where(MissionScript def)
    {
        var probe = new Offer { Script = def.Id, Reward = def.Reward };
        return Passable.Where(h => { probe.C = h.C; probe.R = h.R; return def.Where.Bool(new HexScope(this, h, probe)); }).ToList();
    }

    public MissionScript ScriptOf(Offer m) => Data.Missions[m.Script];
    public Offer? MissionAt(int c, int r) => S.Missions.FirstOrDefault(m => m.C == c && m.R == r);
    public string Brief(Offer m) => ScriptOf(m).Brief.Eval(new OfferScope(this, m));
    public string Objective(Offer m) => ScriptOf(m).Objective.Eval(new OfferScope(this, m));
    /// <summary>Why the fleet cannot take the mission yet, or null.</summary>
    public string? Needs(Offer m)
    {
        var sc = new OfferScope(this, m);
        return ScriptOf(m).Needs.FirstOrDefault(n => n.When.Bool(sc))?.Say.Eval(sc);
    }

    /// <summary>The battle the mission starts here, or null for one done on arrival.</summary>
    public MissionBattle? BattleOf(Offer m)
    {
        var sc = new OfferScope(this, m);
        return ScriptOf(m).Battles.FirstOrDefault(b => b.When == null || b.When.Bool(sc));
    }

    public void Accept(Offer m) { if (!S.Active.Contains(m.Id)) S.Active.Add(m.Id); }

    public int Marines => S.Fleet.Sum(s => s.Supplies.GetValueOrDefault("marines"));

    // ---- Ships, the shipyard and the outfitter ---------------------------------------------------------------------------

    public CampShip NewShip(string hullId, string? name = null)
    {
        Hull h = Data.Hulls[hullId];
        Tier t = Data.TierOf(h);
        int id = ++S.ShipSerial;
        var s = new CampShip { Id = id, Hull = hullId, Maker = h.Side, Name = name ?? $"{h.Name} {100 + id}" };
        foreach (var (g, _) in Data.Groups) s.Fit[g] = Enumerable.Repeat<string?>(Data.Stock[g], t.Slots[g]).ToList();
        foreach (Supply u in Data.Supplies) s.Supplies[u.Id] = 0;
        return s;
    }

    public int CountFit(CampShip s, string sys) => s.Fit.Values.Sum(l => l.Count(x => x == sys));
    public int FitCost(CampShip s) => s.Fit.Values.Sum(l => l.Sum(x => x == null ? 0 : Data.Systems[x].Cost));
    public int SupplyCost(CampShip s) => Data.Supplies.Sum(u => s.Supplies.GetValueOrDefault(u.Id) * u.Cost);
    /// <summary>A ship's worth in points: hull, fittings and supplies.</summary>
    public int Points(CampShip s) => Data.TierOf(Data.Hulls[s.Hull]).Cost + FitCost(s) + SupplyCost(s);

    /// <summary>The grant a supply needs at a friendly port: repair kits are parts, the rest supplies.</summary>
    public static string GrantOfSupply(string supplyId) => supplyId == KitSupply ? "parts" : "supplies";

    // why a friendly port will not give a grant yet, or the plain reason away from any port
    private string Refused(string grant, string elsewhere) =>
        At.IsPort && !IsPort(At) && At.Owner != "neutral" && Data.Factions.ContainsKey(At.Owner)
            ? $"{Data.Factions[At.Owner].Name} ports give {grant} at standing {StandingFor(grant)}; yours is {Standing(At.Owner)}." : elsewhere;

    public int SupplyMax(CampShip s, string id)
    {
        Supply u = Data.Supplies.First(x => x.Id == id);
        int hp = Data.TierOf(Data.Hulls[s.Hull]).Hp;
        return u.HpPer > 0 ? (int)Math.Round(hp / (double)u.HpPer, MidpointRounding.AwayFromZero) : u.PerFit > 0 ? u.PerFit * CountFit(s, u.Fit) : u.Fixed;
    }

    private void ClampSupplies(CampShip s) { foreach (Supply u in Data.Supplies) s.Supplies[u.Id] = Math.Min(s.Supplies[u.Id], SupplyMax(s, u.Id)); }

    /// <summary>What a buyer pays for something worth <paramref name="credits"/> new: a share, more from the maker's rival,
    /// less from pirates.</summary>
    public int SalePrice(int credits, string maker, string buyer)
    {
        string? rival = Data.Factions[maker].Rival;
        double mult = rival != null && rival == buyer ? Ru.RivalPays : buyer == "vos" ? Ru.PiratesPay : 1;
        return (int)Math.Floor(credits * Ru.SaleShare * mult);
    }

    private string? Pay(int net)
    {
        if (net > S.Credits) return $"Not enough credits: needs {net}, have {S.Credits}.";
        S.Credits -= net;
        return null;
    }

    /// <summary>The price in credits of a hull built new, fitted with stock systems.</summary>
    public int HullPrice(string hullId)
    {
        CampShip probe = NewShip(hullId, "probe");
        S.ShipSerial--;
        return Points(probe) * Ru.CreditRate;
    }

    /// <summary>Buys a hull at your own port. Returns why not, or null.</summary>
    /// <summary>Why the shipyard will not sell a hull for want of prestige, or null: each tier asks its prestige.</summary>
    public string? PrestigeShort(string hullId)
    {
        int need = Data.TierOf(Data.Hulls[hullId]).Prestige;
        return S.Prestige >= need ? null : $"Needs {need} prestige (you have {S.Prestige:0}).";
    }

    public string? Buy(string hullId)
    {
        if (!Allows("ships")) return At.IsPort && !IsPort(At) ? $"Their shipyard sells to friends of standing {StandingFor("ships")} or more." : "The shipyard is at your own ports.";
        if (S.Fleet.Count >= Ru.FleetMax) return $"A fleet has at most {Ru.FleetMax} ships.";
        Hull h = Data.Hulls[hullId];
        if (h.Side != YardSide || !h.ForSale) return $"{h.Name} is not built here.";
        if (PrestigeShort(hullId) is { } why) return why;
        string? err = Pay(HullPrice(hullId));
        if (err != null) return err;
        S.Fleet.Add(NewShip(hullId, Side.Prefix + Side.Names[S.Fleet.Count % Side.Names.Count]));
        return null;
    }

    /// <summary>What a buyer pays for one of your ships: its worth, by its hull left, at the buyer's rate.</summary>
    public int SaleQuote(CampShip s, string buyer) => SalePrice((int)(Points(s) * Ru.CreditRate * s.HullPct / 100), s.Maker, buyer);

    /// <summary>Sells a ship at a port that deals with you (never the last ship). Returns why not, or null.</summary>
    public string? Sell(CampShip s)
    {
        Sector at = At;
        string buyer = IsPort(at) ? S.Side : IsMarket(at) ? at.Owner : "";
        if (buyer == "") return "Ships are sold at a port that deals with you.";
        if (S.Fleet.Count <= 1) return "You cannot sell your last ship.";
        if (s.Haul > 0) return $"The {s.Name} is a prize with her cargo: cash it out.";
        S.Credits += SaleQuote(s, buyer);
        S.Fleet.Remove(s);
        ChangeStanding(buyer, buyer == "vos" ? Ru.PirateSaleStanding : Ru.SaleStanding);
        return null;
    }

    // ---- Stores: systems bought, held and fitted ------------------------------------------------------------------------
    // Fitting is free (owner, 2026-11-06: "fitting stuff on a ship should be free, have the store below"): a system is
    // bought into the fleet's stores at a port, fitted from the stores at no cost, and goes back to the stores when it comes
    // off. Selling one from the stores returns the yard's share of its price.

    /// <summary>How many of a system the fleet holds in its stores.</summary>
    public int InStores(string sysId) => S.Stores.GetValueOrDefault(sysId);

    private void Stow(string sysId, int n)
    {
        int now = InStores(sysId) + n;
        if (now > 0) S.Stores[sysId] = now; else S.Stores.Remove(sysId);
    }

    /// <summary>What a system costs new in the store.</summary>
    public int SystemPrice(string sysId) => Data.Systems[sysId].Cost * Ru.CreditRate;

    /// <summary>What the yard pays for a system from the stores.</summary>
    public int SystemSale(string sysId) => SalePrice(SystemPrice(sysId), S.Side, S.Side);

    /// <summary>Buys a system into the stores at your own port. Returns why not, or null.</summary>
    public string? BuySystem(string sysId)
    {
        if (!Allows("weapons")) return Refused("weapons", "The store is at your own ports.");
        if (!Data.Systems.ContainsKey(sysId)) return $"No system '{sysId}'.";
        string? err = Pay(SystemPrice(sysId));
        if (err != null) return err;
        Stow(sysId, 1);
        return null;
    }

    /// <summary>Sells one of a system from the stores at your own port. Returns why not, or null.</summary>
    public string? SellSystem(string sysId)
    {
        if (!Allows("weapons")) return Refused("weapons", "The store is at your own ports.");
        if (InStores(sysId) <= 0) return $"No {Data.Systems[sysId].Name} in the stores.";
        Stow(sysId, -1);
        S.Credits += SystemSale(sysId);
        return null;
    }

    /// <summary>Fits a system from the stores to a hardpoint (or empties it with null), free: the system it replaces goes
    /// back to the stores. Returns why not, or null.</summary>
    public string? Refit(CampShip s, string group, int i, string? sysId)
    {
        if (sysId != null && Data.Systems[sysId].Group != group) return $"{Data.Systems[sysId].Name} does not fit a {group} hardpoint.";
        string? old = s.Fit[group][i];
        if (old == sysId) return null;
        if (sysId != null && InStores(sysId) <= 0) return $"No {Data.Systems[sysId].Name} in the stores: buy one below.";
        if (sysId != null) Stow(sysId, -1);
        if (old != null) Stow(old, 1);
        s.Fit[group][i] = sysId;
        ClampSupplies(s);
        return null;
    }

    /// <summary>The hardpoints of a ship that are empty, as (group, index).</summary>
    public List<(string Group, int I)> EmptyHardpoints(CampShip s) =>
        s.Fit.SelectMany(kv => kv.Value.Select((x, i) => (kv.Key, i, x))).Where(t => t.x == null).Select(t => (t.Key, t.i)).ToList();

    /// <summary>What filling every empty hardpoint with the stock system costs: the stock systems the stores do not hold,
    /// at the store's price.</summary>
    public int StockQuote(CampShip s)
    {
        var need = EmptyHardpoints(s).GroupBy(h => Data.Stock[h.Group]);
        return need.Sum(g => Math.Max(0, g.Count() - InStores(g.Key)) * SystemPrice(g.Key));
    }

    /// <summary>Fills every empty hardpoint with the stock system: from the stores first, buying the rest. Buying needs a
    /// port.</summary>
    public string? FitStock(CampShip s)
    {
        var empty = EmptyHardpoints(s);
        if (empty.Count == 0) return null;
        int quote = StockQuote(s);
        if (quote > 0 && !Allows("weapons")) return Refused("weapons", "The store is at your own ports.");
        string? err = Pay(quote);
        if (err != null) return err;
        foreach (var (g, i) in empty)
        {
            string id = Data.Stock[g];
            if (InStores(id) > 0) Stow(id, -1);
            s.Fit[g][i] = id;
        }
        ClampSupplies(s);
        return null;
    }

    /// <summary>What changing a ship's supply by delta (clamped to what it carries) costs; below 0 is a refund.</summary>
    public int SupplyQuote(CampShip s, string id, int delta)
    {
        Supply u = Data.Supplies.First(x => x.Id == id);
        int now = s.Supplies[id], n = Math.Clamp(now + delta, 0, SupplyMax(s, id)) - now;
        return n > 0 ? u.Cost * Ru.CreditRate * n : -SalePrice(u.Cost * Ru.CreditRate, S.Side, S.Side) * -n;
    }

    /// <summary>Buys (delta above 0) or sells back supplies, within what the ship carries.</summary>
    public string? Resupply(CampShip s, string id, int delta)
    {
        Supply u = Data.Supplies.First(x => x.Id == id);
        if (!Allows(GrantOfSupply(id))) return Refused(GrantOfSupply(id), "Supplies are bought at a port that deals with you.");
        int max = SupplyMax(s, id), now = s.Supplies[id];
        int next = Math.Clamp(now + delta, 0, max);
        if (next == now) return delta > 0 ? $"{u.Name}: this ship carries at most {max}." : null;
        string? err = Pay(SupplyQuote(s, id, delta));
        if (err != null) return err;
        s.Supplies[id] = next;
        return null;
    }

    /// <summary>What filling every supply of every ship in the fleet to what it carries costs.</summary>
    public int RestockQuote() => S.Fleet.Sum(s => Stocked().Sum(u => SupplyQuote(s, u.Id, SupplyMax(s, u.Id))));

    // the supplies the port where the fleet stands sells you (all at your own port)
    private IEnumerable<Supply> Stocked() => Data.Supplies.Where(u => Allows(GrantOfSupply(u.Id)));

    /// <summary>Fills every supply of every ship to what it carries, paying for all at once.</summary>
    public string? Restock()
    {
        if (!Allows("parts")) return Refused("parts", "Supplies are bought at a port that deals with you.");
        string? err = Pay(RestockQuote());
        if (err != null) return err;
        foreach (CampShip s in S.Fleet) foreach (Supply u in Stocked()) s.Supplies[u.Id] = SupplyMax(s, u.Id);
        return null;
    }

    /// <summary>What repairing one ship's hull to full costs: the hull's share (rules.repair_share) of its price for what is missing.</summary>
    public int RepairCost(CampShip s) => (int)Math.Ceiling(Data.TierOf(Data.Hulls[s.Hull]).Cost * Ru.CreditRate * (100 - s.HullPct) / 100 * Ru.RepairShare);
    /// <summary>What repairing the whole fleet costs.</summary>
    public int RepairCost() => S.Fleet.Sum(RepairCost);

    /// <summary>Repairs one ship (or, with null, the whole fleet) at your own port.</summary>
    public string? Repair(CampShip? one = null)
    {
        if (!Allows("parts")) return Refused("parts", "Repairs are made at a port that deals with you.");
        string? err = Pay(one != null ? RepairCost(one) : RepairCost());
        if (err != null) return err;
        foreach (CampShip s in one != null ? new[] { one } : S.Fleet.ToArray()) s.HullPct = 100;
        return null;
    }

    /// <summary>The officer serving at a station, or null.</summary>
    public Officer? AtStation(string station) => S.Crew.FirstOrDefault(o => o.Station == station);

    /// <summary>Hires an officer for their station. One officer serves each station: whoever served there before leaves the
    /// crew and goes back on offer, and the hired one's place on offer is taken by someone new of the same kind, so a station
    /// always has its list.</summary>
    public string? Hire(Officer o)
    {
        if (!Allows("officers")) return Refused("officers", "Officers are recruited at your own ports.");
        int at = S.OfficerPool.IndexOf(o);
        if (at < 0) return $"{o.Name} is not on offer.";
        string? err = Pay(o.Cost);
        if (err != null) return err;
        S.OfficerPool[at] = NewOfficer(o.Station, Data.Officers.Skills.ToList().FindIndex(s => s.Skill == o.Skill));
        if (AtStation(o.Station) is { } was) { S.Crew.Remove(was); BackOnOffer(was); }
        S.Crew.Add(o);
        return null;
    }

    /// <summary>Lets an officer go (no refund); they go back on offer.</summary>
    public void Dismiss(Officer o)
    {
        if (!S.Crew.Remove(o)) return;
        BackOnOffer(o);
    }

    /// <summary>A wingman's standing orders (data behaviours); the flagship has none.</summary>
    public void SetBehaviour(CampShip s, string behaviour)
    {
        if (Data.Behaviours.Contains(behaviour) && S.Fleet.IndexOf(s) > 0) s.Behaviour = behaviour;
    }
}
