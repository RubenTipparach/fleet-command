// Lockstep: how a co-op crew shares one battle (openspec/changes/bridge-crew-coop, design "Lockstep"). The battle is
// deterministic (CLAUDE.md 6.3), so every peer steps its own copy from the same orders: the host (the admiral) gathers the
// orders, its own and those its crew sends, checks each against the sender's roles (Crew.Refuses), and sends every step as
// a tick: the crew changes and orders to apply, in order, then whether to step. Every peer, the host included, applies a
// tick the same way (Apply). Every StepsPerSum ticks the host adds its checksum after the tick (Battle.Checksum) and each
// crew peer compares its own: a battle that drifts is reported the moment it does (Desyncs).
//
// The transport is not here: the host's Send goes to every crew peer, a crew peer's to the host, and Receive takes what
// arrived (Game: CoopNet, over ENet). Messages are JSON objects, one per packet:
//   crew to host:  hello {name} · order {tab, name, args} · crew {op, ...}
//   host to crew:  welcome {you} · roster {crew} · tick {n, step, ops[], orders[], sum?}  (start, end and screens are the
//   game flow's own, CoopNet)
// Plain .NET: tests run two of these against each other with no network (LockstepTests).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using FleetCommand.Sim;

namespace FleetCommand.Coop;

public sealed class Lockstep
{
    /// <summary>Steps between checksums.</summary>
    public const int StepsPerSum = 30;

    public Crew Crew { get; private set; }
    /// <summary>This peer's member id (1 on the host).</summary>
    public int Me { get; private set; }
    public bool IsHost { get; }
    /// <summary>The battle being shared, or null between battles.</summary>
    public Battle? Battle { get; private set; }
    /// <summary>A crew peer: the connection to the host was lost.</summary>
    public bool Lost { get; set; }
    /// <summary>Ticks applied this battle.</summary>
    public long Ticks { get; private set; }
    /// <summary>Checksums compared and found equal, and found different (a crew peer's count; the host checks none).</summary>
    public int Synced { get; private set; }
    public int Desyncs { get; private set; }
    /// <summary>The console tab this peer has open (a lock from it may step into another's role: Crew.LockRoles).</summary>
    public Func<string?> OpenTab = () => null;
    /// <summary>Sends a message: the host to every crew peer, a crew peer to the host.</summary>
    public Action<JsonObject> Send = _ => { };
    /// <summary>Told a line for the log (joins, refusals, desyncs).</summary>
    public Action<string> Note = _ => { };
    /// <summary>Told when the roster (members, ships, roles) changed.</summary>
    public Action? CrewChanged;

    // the admiral's ship between ticks (Battle.Player is the local member's own ship while the console draws)
    private Ship? _flag;
    // the host's orders and crew changes waiting for the next tick
    private readonly List<JsonObject> _orders = new(), _ops = new();
    // a crew peer's ticks waiting to be played
    private readonly Queue<JsonObject> _ticks = new();

    public Lockstep(Crew crew, bool host)
    {
        Crew = crew;
        IsHost = host;
        Me = host ? Crew.AdmiralId : 0;
    }

    // ---- The battle -----------------------------------------------------------------------------------------------

    /// <summary>Starts sharing a battle that every peer has just built from the same scenario: its orders go through the
    /// lockstep from now on, the flagship keeps a target per role, and the ships given to members are theirs.</summary>
    public void Begin(Battle b)
    {
        Battle = b;
        Ticks = 0;
        _ticks.Clear(); _orders.Clear(); _ops.Clear();
        _flag = b.Player;
        b.CrewShip(b.Player.Id);
        foreach (string id in Crew.Ships.Keys.ToList())
            if (b.ShipById(id) is { Alive: true, Side: 0 } s && s != b.Player) b.SetHelmed(id, true);
            else Crew.Ships.Remove(id);
        b.Relay = Local;
        ViewOwn();
    }

    /// <summary>Stops sharing the battle (it ended, or the crew left).</summary>
    public void End()
    {
        if (Battle != null) { Battle.ViewAs(_flag ?? Battle.Player); Battle.Relay = null; }
        Battle = null;
    }

    /// <summary>The admiral's ship (the flagship) in the battle being shared.</summary>
    public Ship? Flag => _flag;

    /// <summary>The ship this peer's member commands or crews.</summary>
    public Ship? OwnShip()
    {
        if (Battle == null || _flag == null) return null;
        string bridge = Crew.BridgeOf(Me);
        return bridge == Crew.Flag ? _flag : Battle.ShipById(bridge) ?? _flag;
    }

    /// <summary>The battle's checksum as the admiral's ship stands (the console's own view put back after).</summary>
    public ulong Sum()
    {
        if (Battle == null) return 0;
        Battle.ViewAs(_flag ?? Battle.Player);
        ulong h = Battle.Checksum();
        ViewOwn();
        return h;
    }

    private void ViewOwn() { if (Battle != null && OwnShip() is { } s) Battle.ViewAs(s); }

    // an order given on this peer (a key, a click, the scripted captain, the game flow): to the host, or the host's queue
    private void Local(string name, object?[] args)
    {
        // refused here as the host would refuse it, and said on this console's log only (the log is no part of the battle's
        // checksum)
        if (Crew.Refuses(Me, name) is { } why) { Battle?.Say("Bridge", $"Not yours to order: {why}."); Note($"COOP_REFUSED {Crew.NameOf(Me)} {name}: {why}"); return; }
        var o = new JsonObject { ["t"] = "order", ["tab"] = OpenTab(), ["name"] = name, ["args"] = Args(args) };
        if (IsHost) HostOrder(Me, o);
        else Send(o);
    }

    /// <summary>A crew change asked on this peer: give a role (op "role": role, to) or a ship (op "ship": ship, to).</summary>
    public void AskCrew(JsonObject op)
    {
        op["t"] = "crew";
        if (IsHost) HostCrew(Me, op);
        else Send(op);
    }

    private static JsonArray Args(object?[] args)
    {
        var a = new JsonArray();
        foreach (object? v in args)
            a.Add(v switch
            {
                null => null,
                string s => JsonValue.Create(s),
                bool b => JsonValue.Create(b),
                double d => JsonValue.Create(d),
                int i => JsonValue.Create(i),
                Enum e => JsonValue.Create(e.ToString()),
                _ => JsonValue.Create(Convert.ToString(v, CultureInfo.InvariantCulture)),
            });
        return a;
    }

    private static object? Arg(JsonNode? n)
    {
        if (n is not JsonValue v) return null;
        if (v.TryGetValue(out string? s)) return s;
        if (v.TryGetValue(out bool b)) return b;
        if (v.TryGetValue(out double d)) return d;
        return null;
    }

    // ---- The host --------------------------------------------------------------------------------------------------

    private void HostOrder(int by, JsonObject o)
    {
        string name = (string?)o["name"] ?? "";
        if (Crew.Refuses(by, name) is { } why) { Note($"COOP_REFUSED {Crew.NameOf(by)} {name}: {why}"); return; }
        o["by"] = by;
        _orders.Add(o);
    }

    private void HostCrew(int by, JsonObject op)
    {
        op["by"] = by;
        // between battles a crew change takes effect at once, and the roster goes out; in a battle it waits for the next tick
        if (Battle == null) { ApplyCrew(op); SendRoster(); return; }
        _ops.Add(op);
    }

    /// <summary>The host: a message from crew member <paramref name="from"/>.</summary>
    public void HostReceive(int from, JsonObject m)
    {
        switch ((string?)m["t"])
        {
            case "order": HostOrder(from, m); break;
            case "crew": HostCrew(from, m); break;
            case "desync": Desyncs++; Note($"COOP_DESYNC reported by {Crew.NameOf(from)} at tick {(long?)m["n"]}"); break;
        }
    }

    /// <summary>The host: a crew member joined; returns their id (the game sends them welcome and the roster).</summary>
    public int HostJoin(string name)
    {
        CrewMember m = Crew.Join(name);
        Note($"COOP_JOIN {m.Id} {m.Name}");
        SendRoster();
        return m.Id;
    }

    /// <summary>The host: a crew member left (their ship goes back to the AI, their roles to their captain).</summary>
    public void HostLeave(int id)
    {
        if (Battle != null && Crew.Ships.Where(kv => kv.Value == id).Select(kv => kv.Key).FirstOrDefault() is { } ship)
            _ops.Add(new JsonObject { ["t"] = "crew", ["op"] = "ship", ["ship"] = ship, ["to"] = Crew.AdmiralId, ["by"] = Crew.AdmiralId });
        Crew.Leave(id);
        Note($"COOP_LEAVE {id}");
        SendRoster();
    }

    /// <summary>Sends the whole roster (between battles, and when someone joins or leaves).</summary>
    public void SendRoster()
    {
        Send(new JsonObject { ["t"] = "roster", ["crew"] = CrewJson(Crew) });
        CrewChanged?.Invoke();
    }

    /// <summary>The host: one tick, with what is waiting and a step if <paramref name="step"/> (false while paused, when only
    /// orders and crew changes go out, and nothing at all if there are none). Applied here and sent to the crew.</summary>
    public void HostTick(bool step)
    {
        if (!step && _orders.Count == 0 && _ops.Count == 0) return;
        var t = new JsonObject { ["t"] = "tick", ["n"] = Ticks + 1, ["step"] = step };
        var ops = new JsonArray(); foreach (JsonObject o in _ops) ops.Add(o.DeepClone());
        var ords = new JsonArray(); foreach (JsonObject o in _orders) ords.Add(o.DeepClone());
        t["ops"] = ops; t["orders"] = ords;
        _ops.Clear(); _orders.Clear();
        // the host applies the tick as the crew will read it (through its text), so every peer applies the same values
        t = JsonNode.Parse(t.ToJsonString())!.AsObject();
        Apply(t);
        if (Battle != null && step && Battle.Steps % StepsPerSum == 0) t["sum"] = Sum().ToString("x16");
        Send(t);
    }

    // ---- A crew peer -----------------------------------------------------------------------------------------------

    /// <summary>A crew peer: a message from the host. Ticks wait to be played (Play); the rest applies now.</summary>
    public void Receive(JsonObject m)
    {
        switch ((string?)m["t"])
        {
            case "welcome": Me = (int?)m["you"] ?? 0; Note($"COOP_WELCOME {Me}"); break;
            case "roster": ReadCrew(Crew, m["crew"]!.AsObject()); CrewChanged?.Invoke(); ViewOwn(); break;
            case "tick": _ticks.Enqueue(m); break;
        }
    }

    /// <summary>Ticks received and not yet played.</summary>
    public int Waiting => _ticks.Count;

    /// <summary>A crew peer: plays up to <paramref name="steps"/> stepping ticks, and every non-stepping tick before them.
    /// Returns the steps played.</summary>
    public int Play(int steps)
    {
        int n = 0;
        while (_ticks.Count > 0 && Battle != null)
        {
            bool stepping = (bool?)_ticks.Peek()["step"] ?? false;
            if (stepping && n >= steps) break;
            JsonObject t = _ticks.Dequeue();
            Apply(t);
            if (stepping) n++;
            if ((string?)t["sum"] is { } sum)
            {
                string mine = Sum().ToString("x16");
                if (mine == sum) Synced++;
                else
                {
                    Desyncs++;
                    Note($"COOP_DESYNC tick {(long?)t["n"]} step {Battle.Steps}: host {sum}, here {mine}");
                    Send(new JsonObject { ["t"] = "desync", ["n"] = (long?)t["n"] });
                }
            }
        }
        return n;
    }

    // ---- Every peer ------------------------------------------------------------------------------------------------

    // one tick, the same on every peer: crew changes, then orders as their senders, then the step
    private void Apply(JsonObject t)
    {
        Ticks = (long?)t["n"] ?? Ticks + 1;
        Battle? b = Battle;
        if (b != null) b.ViewAs(_flag ?? b.Player);
        foreach (JsonNode? n in t["ops"]?.AsArray() ?? new JsonArray()) ApplyCrew(n!.AsObject());
        if (b != null)
        {
            foreach (JsonNode? n in t["orders"]?.AsArray() ?? new JsonArray())
            {
                JsonObject o = n!.AsObject();
                int by = (int?)o["by"] ?? 0;
                string bridge = Crew.BridgeOf(by);
                Ship ship = bridge == Crew.Flag ? b.Player : b.ShipById(bridge) ?? b.Player;
                if (ship is not { Alive: true }) continue;
                var args = (o["args"]?.AsArray() ?? new JsonArray()).Select(Arg).ToList();
                string name = (string?)o["name"] ?? "";
                bool captain = Crew.CaptainOf(bridge) == by;
                Ship now = b.ApplyAs(ship, Crew.LockRoles(by, (string?)o["tab"]), captain, () => b.ApplyCall(name, args));
                if (now != ship && bridge != Crew.Flag) Crew.MoveShip(ship.Id, now.Id);
            }
            if ((bool?)t["step"] == true) b.Step();
            _flag = b.Player;
            // the admiral's ship lost and command passed to a ship a member captains: that ship is the admiral's now
            if (Crew.Ships.Remove(_flag.Id)) { b.SetHelmed(_flag.Id, false); CrewChanged?.Invoke(); }
            ViewOwn();
        }
    }

    private void ApplyCrew(JsonObject op)
    {
        int by = (int?)op["by"] ?? 0, to = (int?)op["to"] ?? 0;
        string? why;
        switch ((string?)op["op"])
        {
            case "role":
                why = Crew.GiveRole(by, (string?)op["role"] ?? "", to);
                break;
            case "ship":
                string ship = (string?)op["ship"] ?? "";
                if (Battle != null && (Battle.ShipById(ship) is not { Alive: true, Side: 0 } s || s == (_flag ?? Battle.Player))) { why = "not a ship of the fleet the admiral can give"; break; }
                string? had = Crew.Ships.Where(kv => kv.Value == to).Select(kv => kv.Key).FirstOrDefault();
                why = Crew.GiveShip(by, ship, to);
                if (why == null && Battle != null)
                {
                    if (had != null) Battle.SetHelmed(had, false);
                    Battle.SetHelmed(ship, to != Crew.AdmiralId);
                }
                break;
            default: why = "unknown change"; break;
        }
        if (why != null) Note($"COOP_REFUSED {Crew.NameOf(by)} {(string?)op["op"]}: {why}");
        else Note($"COOP_CREW {Crew.NameOf(by)} {(string?)op["op"]} {(string?)op["role"] ?? (string?)op["ship"]} to {Crew.NameOf(to)}");
        CrewChanged?.Invoke();
    }

    // ---- The roster on the wire --------------------------------------------------------------------------------------

    /// <summary>The roster as JSON: members, ships given and roles given.</summary>
    public static JsonObject CrewJson(Crew c)
    {
        var members = new JsonArray(); foreach (CrewMember m in c.Members) members.Add(new JsonObject { ["id"] = m.Id, ["name"] = m.Name });
        var ships = new JsonObject(); foreach (var (k, v) in c.Ships) ships[k] = v;
        var held = new JsonObject(); foreach (var (k, v) in c.Held) held[k] = v;
        return new JsonObject { ["members"] = members, ["ships"] = ships, ["held"] = held };
    }

    /// <summary>Replaces a roster with one sent as JSON (CrewJson).</summary>
    public static void ReadCrew(Crew c, JsonObject j)
    {
        c.Members.Clear(); c.Ships.Clear(); c.Held.Clear();
        foreach (JsonNode? m in j["members"]!.AsArray()) c.Members.Add(new CrewMember((int)m!["id"]!, (string)m["name"]!));
        foreach (var (k, v) in j["ships"]!.AsObject()) c.Ships[k] = (int)v!;
        foreach (var (k, v) in j["held"]!.AsObject()) c.Held[k] = (int)v!;
    }
}
