// Battle.Security: boarding defence, after the battle console mockup's makeSec, secStep, boardShip, walkTo and A.secGo
// (docs/mockups/battle-console.html; openspec/changes/boarding-defence; owner, 2026-11-04: "Put a high security officer,
// transport units around the ship to setup ambushes. We need to mock up units moving about the ship.in 3d space"). The
// marines aboard the ship you command are its security teams, each in a room of its deck plan (Data.DeckPlan); a team
// walks the corridors or is beamed to a room; a team and a squad of boarders close on one deck fight in volleys
// (Rules.Volley); a team that has held its room long enough springs an ambush; boarders unopposed at their goal sabotage
// its system, and holding the bridge takes the ship. A boarding captain sends squads whenever it can board.
// The transporter's pads are the comms package's: until it is in, SecurityUsePad leaves a beam always ready (a pad
// is always charged), and boarding needs only transporter range and the facing toward the boarder down.
using System;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    private SecurityRules Sr => Cs.Security;

    /// <summary>Uses one of a ship's transporter pads; <paramref name="used"/> comes back false when none is charged. The
    /// comms package's pads fill this in; without it every beam finds a pad.</summary>
    partial void SecurityUsePad(Ship s, ref bool used);

    private bool UsePad(Ship s)
    {
        bool ok = true;
        SecurityUsePad(s, ref ok);
        return ok;
    }

    /// <summary>Makes a ship's security (its marines in teams at their posts) once its class has a deck plan.</summary>
    public void EnsureSecurity(Ship s)
    {
        if (s.Sec != null || Cs.PlanFor(s.Class.Id) is not { } plan) return;
        var sec = new SecurityState { Plan = plan };
        for (int i = 0; i * Sr.Team < s.Marines; i++)
            sec.Teams.Add(UnitAt($"T{i + 1}", s.Side, Math.Min(Sr.Team, s.Marines - i * Sr.Team), plan.Room(plan.Posts[i % plan.Posts.Count]), plan));
        sec.NextTeam = sec.Teams.Count + 1;
        s.Sec = sec;
    }

    private Unit UnitAt(string id, int side, int n, DeckRoom room, DeckPlan plan)
    {
        DeckNode nd = plan.Nodes[room.Node];
        return new Unit { Id = id, Side = side, N = n, Node = room.Node, X = nd.X, Y = nd.Y, Z = nd.Z, Still = Sr.AmbushS, Goal = room.Id };
    }

    /// <summary>The room a unit stands in.</summary>
    public static DeckRoom RoomOf(SecurityState sec, Unit u) => sec.Plan.RoomAt(u.X, u.Y, u.Z, u.Node);

    /// <summary>What a unit is doing, as the Security page says it.</summary>
    public string UnitState(Unit u)
    {
        if (u.Fight != null) return "Fighting";
        if (u.Path.Count > 0) return "Moving";
        if (u.Side == Player.Side) return u.Still >= Sr.AmbushS ? "Ambush set" : $"Setting ambush {Math.Round(100 * Math.Min(1, u.Still / Sr.AmbushS))} %";
        return u.Sab > 0 ? "Sabotaging" : "Holding";
    }

    // The ship you command's security, every step, and a scene won by repelling boarders.
    partial void SecurityStep(double dt)
    {
        Ship p = Player;
        if (!p.Alive) return;
        EnsureSecurity(p);
        if (p.Sec is { } sec) SecStep(p, sec, dt);
        // the Repel boarders scene: won once every boarding captain is out of marines and none are left aboard
        if (Scenario.Repel && Over == null && p.Sec is { Foes.Count: 0 } && !_ships.Any(o => o.Alive && o.Side != p.Side && o.AiBoard && o.Marines > 0))
        {
            Win();
            Say("Security", "The last boarders are down: the ship is ours.");
        }
    }

    // The teams follow the marines aboard, units walk, fights start and are fought, and boarders at their goal sabotage it
    // or take the bridge.
    private void SecStep(Ship s, SecurityState sec, double dt)
    {
        DeckPlan plan = sec.Plan;
        // the marines aboard are the teams: those beamed away leave the last teams, those back form a team at the pads
        int have = sec.Teams.Sum(u => u.N);
        while (have > s.Marines && sec.Teams.Count > 0)
        {
            Unit u = sec.Teams[^1];
            int k = Math.Min(u.N, have - s.Marines);
            u.N -= k; have -= k;
            if (u.N == 0) { if (u.Fight != null) u.Fight.Done = true; sec.Teams.RemoveAt(sec.Teams.Count - 1); }
        }
        if (have < s.Marines) sec.Teams.Add(UnitAt($"T{sec.NextTeam++}", s.Side, s.Marines - have, plan.Room(plan.PadRoom), plan));
        double sp = Sr.WalkMS * dt;
        foreach (Unit u in sec.Teams.Concat(sec.Foes))
        {
            if (u.Fight != null) continue;
            if (u.Path.Count == 0) { u.Still += dt; continue; }
            DeckNode nd = plan.Nodes[u.Path[0]];
            double dx = nd.X - u.X, dy = nd.Y - u.Y, dz = nd.Z - u.Z, d = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            u.Still = 0;
            if (d <= sp) { u.X = nd.X; u.Y = nd.Y; u.Z = nd.Z; u.Node = u.Path[0]; u.Path.RemoveAt(0); }
            else { u.X += dx / d * sp; u.Y += dy / d * sp; u.Z += dz / d * sp; }
        }
        // contact: a team and a squad close on one deck fight; a team with its ambush set opens it
        foreach (Unit f in sec.Foes)
        {
            if (f.Fight != null) continue;
            Unit? tm = sec.Teams.FirstOrDefault(u => u.Fight == null && Math.Abs(u.Y - f.Y) < 1 && Math.Sqrt((u.X - f.X) * (u.X - f.X) + (u.Z - f.Z) * (u.Z - f.Z)) < Sr.EngageM);
            if (tm == null) continue;
            bool amb = tm.Path.Count == 0 && tm.Still >= Sr.AmbushS;
            var fi = new Fight { A = tm, B = f, T = amb ? 0 : Sr.VolleyS / 2, Ambush = amb };
            tm.Fight = f.Fight = fi;
            sec.Fights.Add(fi);
            if (s == Player) Say("Security", amb ? $"{tm.Id} springs an ambush in {RoomOf(sec, tm).Name}." : $"{tm.Id} meets boarders in {RoomOf(sec, tm).Name}.");
        }
        int chief = OfficerSkill(s, "transport");
        foreach (Fight fi in sec.Fights)
        {
            if (fi.Done) continue;
            fi.T -= dt;
            if (fi.T > 0) continue;
            fi.T = Sr.VolleyS;
            double pa = Sr.HitChance + chief * Sr.OfficerAboard + (fi.Ambush ? Sr.AmbushBonus + chief * Sr.OfficerAmbush : 0);
            int ha = Rules.Volley(fi.A.N, pa, _secRng), hb = fi.Ambush ? 0 : Rules.Volley(fi.B.N, Sr.HitChance, _secRng);
            fi.B.N = Math.Max(0, fi.B.N - ha); fi.A.N = Math.Max(0, fi.A.N - hb); fi.Ambush = false;
            if (s == Player) Say("Security", $"{fi.A.Id} in {RoomOf(sec, fi.A).Name}: {ha} boarder{(ha == 1 ? "" : "s")} down, {hb} of ours. {fi.A.N} against {fi.B.N}.");
            if (fi.A.N == 0 || fi.B.N == 0)
            {
                fi.Done = true;
                if (fi.B.N == 0 && s == Player) Say("Security", $"Boarders in {RoomOf(sec, fi.B).Name} cleared.");
            }
        }
        foreach (Fight fi in sec.Fights) if (fi.Done) { fi.A.Fight = null; fi.B.Fight = null; fi.A.Still = 0; }
        sec.Fights.RemoveAll(f => f.Done);
        sec.Foes.RemoveAll(u => u.N <= 0);
        sec.Teams.RemoveAll(u => u.N <= 0);
        s.Marines = sec.Teams.Sum(u => u.N);
        // boarders unopposed at their goal: sabotage, or the bridge held toward capture
        foreach (Unit f in sec.Foes)
        {
            if (f.Fight != null || f.Path.Count > 0) continue;
            DeckRoom room = RoomOf(sec, f);
            if (!Sr.RoomSystem.TryGetValue(room.Id, out string? k)) continue;
            f.Sab += dt;
            if (k == "bridge")
            {
                if (f.Sab >= Sr.CaptureS && Over == null) { Say("Security", $"Boarders took the bridge of the {s.Name}."); Lose(); }
                continue;
            }
            if (f.Sab < Sr.SabotageS) continue;
            f.Sab = 0;
            if (!s.Sys.ContainsKey(k) || s.Sys[k] <= 0) continue;
            s.Sys[k] = Math.Max(0, s.Sys[k] - Sr.SabotageDmg);
            if (s == Player) Say("Security", $"Boarders sabotaging {room.Name.ToLowerInvariant()}: {k} damaged.");
        }
    }

    /// <summary>Whether s can send boarders onto t: an enemy within transporter range with its facing toward s down.</summary>
    public bool CanBoard(Ship s, Ship t) =>
        t.Alive && s.Alive && t.Side != s.Side && Rules.DistKm(s, t) <= Sr.BoardRangeKm && t.Shields[Rules.FacingFrom(t, s.X, s.Z)] <= Sr.ShieldDownHp;

    // A boarding captain sends a squad aboard the ship you command whenever it can.
    private void ThinkBoard(Ship s, Ship t)
    {
        if (s.AiBoard && t == Player && s.Marines > 0 && CanBoard(s, t) && T - s.AiBoardT >= Sr.BoardEveryS) { s.AiBoardT = T; BoardShip(s, t); }
    }

    /// <summary>Boarders beamed aboard t by s: a squad into a room no team holds (picked by a seeded draw), sent for the
    /// plan's goals in turn. The transporter's boarding parties (the comms package) will send theirs through this too.</summary>
    public void BoardShip(Ship s, Ship t)
    {
        EnsureSecurity(t);
        if (t.Sec is not { } sec) return;
        int n = Math.Min(Sr.BoardSquad, s.Marines);
        if (n <= 0) return;
        DeckRoom[] pick = sec.Plan.Rooms.Where(r => !r.IsCorridor && !sec.Teams.Any(u => RoomOf(sec, u) == r)).ToArray();
        if (pick.Length == 0 || !UsePad(s)) return;
        DeckRoom room = pick[_boardRng.Below(pick.Length)], goal = sec.Plan.Room(sec.Plan.Goals[(sec.NextSquad - 1) % sec.Plan.Goals.Count]);
        Unit u = UnitAt($"B{sec.NextSquad++}", s.Side, n, room, sec.Plan);
        u.Still = 0;
        u.Goal = goal.Id;
        u.Path = sec.Plan.Route(room.Node, goal.Node);
        s.Marines -= n;
        sec.Foes.Add(u);
        if (t == Player) Say("Security", $"Intruder alert: {n} boarders in {room.Name}, heading for {goal.Name}.");
    }

    /// <summary>Sends one of your teams to a room: walking the corridors, or beamed there with a pad charge.</summary>
    public void SecurityGo(string teamId, string roomId, bool beam)
    {
        Ship s = Player;
        if (s.Sec is not { } sec || sec.Teams.FirstOrDefault(q => q.Id == teamId) is not { } u || !sec.Plan.HasRoom(roomId)) return;
        DeckRoom r = sec.Plan.Room(roomId);
        if (u.Fight != null) { Say("Security", $"{u.Id} is fighting in {RoomOf(sec, u).Name}."); return; }
        if (beam)
        {
            if (!UsePad(s)) { Say("Transport", "Pads charging: the team cannot be beamed yet."); return; }
            DeckNode nd = sec.Plan.Nodes[r.Node];
            u.X = nd.X; u.Y = nd.Y; u.Z = nd.Z; u.Node = r.Node; u.Path.Clear(); u.Still = 0; u.Goal = r.Id;
            Say("Security", $"{u.Id} beamed to {r.Name}.");
            return;
        }
        WalkTo(sec.Plan, u, r);
        Say("Security", $"{u.Id} to {r.Name}.");
    }

    // A walk to a room: from the node the unit is heading for, or the one it stands on.
    private static void WalkTo(DeckPlan plan, Unit u, DeckRoom room)
    {
        int from = u.Path.Count > 0 ? u.Path[0] : u.Node;
        var rest = plan.Route(from, room.Node);
        if (u.Path.Count > 0) { int first = u.Path[0]; u.Path = rest; u.Path.Insert(0, first); }
        else u.Path = rest;
        u.Goal = room.Id;
    }
}
