// DeckPlan: a ship's interior as a walkable graph (boarding defence; the battle console mockup's INTERIOR, route and
// roomOf), built once from a deck plan file of the Ship Design Bible (data/ships/<line>/<ship>_plan.json): a node in
// every room, a door where each room meets its deck's corridor, the doors along each corridor joined in order of z, and a
// ladder between the corridors of neighbouring decks. Metres, the plan's axes (+x port, +z bow), y the deck's floor. It
// lives with the data because it is the plan's shape; the teams and boarders that walk it are Sim/Battle.Security.cs.
using System;
using System.Collections.Generic;
using System.Linq;

namespace FleetCommand.Data;

/// <summary>A room of the plan: its box (metres), the deck it is on (its first), that deck's floor, its middle and its node.</summary>
public sealed record DeckRoom(string Id, string Name, string Kind, int Deck, double X0, double X1, double Z0, double Z1, double Y, int Node)
{
    public double Cx => (X0 + X1) / 2;
    public double Cz => (Z0 + Z1) / 2;
    public bool IsCorridor => Kind == "corridor";
}

/// <summary>A point of the walkable graph and the room it belongs to.</summary>
public sealed record DeckNode(double X, double Y, double Z, string Room);

public sealed class DeckPlan
{
    public required string Id { get; init; }
    /// <summary>The floor of each deck, metres, top deck first.</summary>
    public required double[] DeckFloorM { get; init; }
    public required double HeadroomM { get; init; }
    public required IReadOnlyList<DeckRoom> Rooms { get; init; }
    public required IReadOnlyList<DeckNode> Nodes { get; init; }
    /// <summary>The graph's edges: two nodes and the walk between them, metres.</summary>
    public required IReadOnlyList<(int A, int B, double M)> Edges { get; init; }
    /// <summary>The decks that have a corridor, top first.</summary>
    public required IReadOnlyList<int> Decks { get; init; }
    /// <summary>Where the teams start, in turn; the rooms boarders go for, in turn; the room marines beamed back form up in.</summary>
    public required IReadOnlyList<string> Posts { get; init; }
    public required IReadOnlyList<string> Goals { get; init; }
    public required string PadRoom { get; init; }
    private Dictionary<string, DeckRoom> _byId = new();

    public DeckRoom Room(string id) => _byId.TryGetValue(id, out DeckRoom? r) ? r : throw new KeyNotFoundException($"no room '{id}' in deck plan '{Id}'");
    public bool HasRoom(string id) => _byId.ContainsKey(id);

    /// <summary>Reads a plan file and builds its graph; posts, goals and the pad room must be its rooms.</summary>
    public static DeckPlan Load(string id, StrictJson f, string[] posts, string[] goals, string padRoom, string where)
    {
        f.Str("schema"); f.Str("ship"); f.Num("crew"); f.Num("clearance_m"); f.Num("structure_m"); f.Num("nose_m"); f.Num("tail_m");
        double[] floors = f.NumList("deck_floor_m");
        if (floors.Length == 0) throw new DataError($"{f.Where}.deck_floor_m: at least one deck");
        double head = f.Num("headroom_m");
        var raw = new List<(string Id, string Name, string Kind, int Deck, double X0, double X1, double Z0, double Z1)>();
        foreach (StrictJson r in f.ObjList("rooms"))
        {
            double[] dk = r.NumList("decks"), x = r.NumList("x_m"), z = r.NumList("z_m");
            if (dk.Length == 0 || x.Length != 2 || z.Length != 2 || x[1] <= x[0] || z[1] <= z[0]) throw new DataError($"{r.Where}: a room needs decks and x_m, z_m as [low, high]");
            int deck = (int)dk[0];
            if (deck < 0 || deck >= floors.Length) throw new DataError($"{r.Where}.decks: no deck {deck}");
            raw.Add((r.Str("id"), r.Str("name"), r.Str("kind"), deck, x[0], x[1], z[0], z[1]));
            r.Done();
        }
        f.Done();

        var nodes = new List<DeckNode>();
        var edges = new List<(int, int, double)>();
        int Add(double x, double y, double z, string room) { nodes.Add(new DeckNode(x, y, z, room)); return nodes.Count - 1; }
        void Link(int a, int b) => edges.Add((a, b, Math.Sqrt(Sq(nodes[a].X - nodes[b].X) + Sq(nodes[a].Y - nodes[b].Y) + Sq(nodes[a].Z - nodes[b].Z))));
        var rooms = new List<DeckRoom>();
        var corr = new Dictionary<int, DeckRoom>();
        var along = new Dictionary<int, List<int>>();
        // the corridor of each deck (the last listed, as the mockup's plan has one a deck)
        foreach (var r in raw) if (r.Kind == "corridor") along[r.Deck] = new List<int>();
        foreach (var r in raw)
        {
            int node = Add((r.X0 + r.X1) / 2, floors[r.Deck], (r.Z0 + r.Z1) / 2, r.Id);
            var room = new DeckRoom(r.Id, r.Name, r.Kind, r.Deck, r.X0, r.X1, r.Z0, r.Z1, floors[r.Deck], node);
            rooms.Add(room);
            if (room.IsCorridor) { corr[r.Deck] = room; along[r.Deck].Add(node); }
        }
        foreach (DeckRoom r in rooms)
        {
            if (r.IsCorridor) continue;
            if (!corr.TryGetValue(r.Deck, out DeckRoom? c)) throw new DataError($"{f.Where}.rooms.{r.Id}: deck {r.Deck} has no corridor to walk from");
            int door = Add(c.Cx, r.Y, Math.Clamp(r.Cz, c.Z0 + 0.6, c.Z1 - 0.6), c.Id);
            Link(r.Node, door);
            along[r.Deck].Add(door);
        }
        int[] decks = corr.Keys.OrderBy(k => k).ToArray();
        for (int i = 0; i + 1 < decks.Length; i++)
        {
            DeckRoom a = corr[decks[i]], b = corr[decks[i + 1]];
            double lo = Math.Max(a.Z0, b.Z0), z = lo + 0.3 * (Math.Min(a.Z1, b.Z1) - lo);
            int na = Add(a.Cx, a.Y, z, a.Id), nb = Add(b.Cx, b.Y, z, b.Id);
            along[a.Deck].Add(na); along[b.Deck].Add(nb);
            Link(na, nb);
        }
        foreach (int d in decks)
        {
            int[] l = along[d].OrderBy(p => nodes[p].Z).ToArray();   // a stable sort, as the mockup's
            for (int i = 0; i + 1 < l.Length; i++) Link(l[i], l[i + 1]);
        }
        var plan = new DeckPlan { Id = id, DeckFloorM = floors, HeadroomM = head, Rooms = rooms, Nodes = nodes, Edges = edges, Decks = decks, Posts = posts, Goals = goals, PadRoom = padRoom };
        plan._byId = rooms.ToDictionary(r => r.Id);
        foreach (string p in posts.Concat(goals).Append(padRoom))
            if (!plan.HasRoom(p)) throw new DataError($"{where}: no room '{p}' in {f.Where}");
        if (posts.Length == 0 || goals.Length == 0) throw new DataError($"{where}: at least one post and one goal");
        return plan;
    }

    private static double Sq(double v) => v * v;

    /// <summary>The shortest walk between two nodes (Dijkstra over the graph), as the nodes after <paramref name="from"/>;
    /// empty when there is none or they are the same node.</summary>
    public List<int> Route(int from, int to)
    {
        int n = Nodes.Count;
        var dist = Enumerable.Repeat(double.PositiveInfinity, n).ToArray();
        var prev = Enumerable.Repeat(-1, n).ToArray();
        var done = new bool[n];
        dist[from] = 0;
        for (;;)
        {
            int u = -1;
            for (int i = 0; i < n; i++) if (!done[i] && dist[i] < double.PositiveInfinity && (u < 0 || dist[i] < dist[u])) u = i;
            if (u < 0 || u == to) break;
            done[u] = true;
            foreach (var (a, b, d) in Edges)
            {
                int v = a == u ? b : b == u ? a : -1;
                if (v >= 0 && dist[u] + d < dist[v]) { dist[v] = dist[u] + d; prev[v] = u; }
            }
        }
        var outp = new List<int>();
        if (double.IsPositiveInfinity(dist[to])) return outp;
        for (int v = to; v >= 0 && v != from; v = prev[v]) outp.Insert(0, v);
        return outp;
    }

    /// <summary>The room a point stands in: the room (not a corridor) whose box holds it on its deck, else any room whose box
    /// holds it, else the room of <paramref name="node"/>.</summary>
    public DeckRoom RoomAt(double x, double y, double z, int node)
    {
        foreach (DeckRoom q in Rooms)
            if (Math.Abs(q.Y - y) < 0.5 && x >= q.X0 && x <= q.X1 && z >= q.Z0 && z <= q.Z1 && !q.IsCorridor) return q;
        foreach (DeckRoom q in Rooms)
            if (Math.Abs(q.Y - y) < 0.5 && x >= q.X0 - 0.1 && x <= q.X1 + 0.1 && z >= q.Z0 && z <= q.Z1) return q;
        return Room(Nodes[node].Room);
    }
}
