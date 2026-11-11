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

/// <summary>Something a crew member said or did for the others to see (owner, 2026-11-11: "someway for players to see and
/// interact with crew members in game. Adding that real time strategy game layer"): chat, a ping on a point or a ship, the
/// admiral's directive to a ship's captain and its answer, or a crew change. None changes the battle: they travel beside
/// the ticks, the host stamping who sent each and passing it to everyone. At is the battle time it was sent (0 between
/// battles); Received is this machine's clock when it arrived, in ms, for how long the view shows it.</summary>
public sealed record CoopEvent(int By, string Kind, string Text, double At, double X = 0, double Z = 0, string? Ship = null, int To = 0, string? Order = null)
{
    public long Received { get; init; }
}

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

    /// <summary>Every crew event of this game, oldest first (the last 200).</summary>
    public List<CoopEvent> Events { get; } = new();
    /// <summary>Told each crew event as it arrives (the console puts it in the log).</summary>
    public Action<CoopEvent>? OnEvent;
    /// <summary>This machine's clock in ms (set by the game), for how long a ping shows.</summary>
    public Func<long> Clock = () => Environment.TickCount64;

    /// <summary>The admiral's directives, by name, and what each tells a ship's captain.</summary>
    public static readonly (string Id, string Label)[] Directives =
        { ("attack", "Attack my target"), ("escort", "Escort me"), ("hold", "Hold position"), ("free", "Engage freely") };

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

    // ---- Crew events -----------------------------------------------------------------------------------------------

    /// <summary>Says something to the whole crew.</summary>
    public void Chat(string text)
    {
        text = text.Trim();
        if (text.Length == 0) return;
        Emit(new JsonObject { ["kind"] = "chat", ["text"] = text.Length > 200 ? text[..200] : text });
    }

    /// <summary>Marks a point on the battle plane (metres), or a ship, for the whole crew.</summary>
    public void Ping(double x, double z, string? ship)
    {
        if (!double.IsFinite(x) || !double.IsFinite(z)) return;
        string what = ship != null && Battle?.ShipById(ship) is { } s ? s.Name : $"{x / 1000:0.0}, {z / 1000:0.0} km";
        Emit(new JsonObject { ["kind"] = "ping", ["text"] = $"marks {what}", ["x"] = x, ["z"] = z, ["ship"] = ship });
    }

    /// <summary>The admiral's directive to the captain of a ship given to a crew member: attack (the admiral's target),
    /// escort (the flagship), hold, or engage freely.</summary>
    public void Direct(int to, string order)
    {
        if (Me != Crew.AdmiralId || !Directives.Any(d => d.Id == order) || !Crew.Ships.ContainsValue(to)) return;
        string? ship = order == "attack" ? Battle?.TargetOf(_flag ?? Battle.Player, "weapons")?.Id : order == "escort" ? _flag?.Id : null;
        if (order == "attack" && ship == null) return;
        string label = Directives.First(d => d.Id == order).Label;
        string text = order switch
        {
            "attack" => $"{Crew.NameOf(to)}: attack the {Battle!.ShipById(ship)!.Name}",
            "escort" => $"{Crew.NameOf(to)}: escort the {_flag?.Name}",
            _ => $"{Crew.NameOf(to)}: {label.ToLowerInvariant()}",
        };
        Emit(new JsonObject { ["kind"] = "directive", ["text"] = text, ["to"] = to, ["order"] = order, ["ship"] = ship });
    }

    /// <summary>A ship's captain answers the admiral's last directive to them.</summary>
    public void Ack()
    {
        if (DirectiveFor(Me) is not { } d) return;
        Emit(new JsonObject { ["kind"] = "ack", ["text"] = $"aye, {d.Text[(d.Text.IndexOf(':') + 2)..]}", ["to"] = d.By, ["order"] = d.Order });
    }

    /// <summary>The admiral's last directive to a member that they have not answered, or null.</summary>
    public CoopEvent? DirectiveFor(int member)
    {
        for (int i = Events.Count - 1; i >= 0; i--)
        {
            CoopEvent e = Events[i];
            if (e.Kind == "ack" && e.By == member) return null;
            if (e.Kind == "directive" && e.To == member) return e;
        }
        return null;
    }

    // a crew event from this machine: to the host, which stamps and passes it on (the host passes its own on at once)
    private void Emit(JsonObject e)
    {
        e["t"] = "event";
        e["at"] = Battle?.T ?? 0;
        if (IsHost) HostEvent(Me, e);
        else Send(e);
    }

    private void HostEvent(int by, JsonObject e)
    {
        e["by"] = by;
        Send(e);
        AddEvent(e);
    }

    // an event arrived (or was made here): kept, and told to the console
    private void AddEvent(JsonObject e)
    {
        var ev = new CoopEvent((int?)e["by"] ?? 0, (string?)e["kind"] ?? "chat", (string?)e["text"] ?? "", (double?)e["at"] ?? 0,
            (double?)e["x"] ?? 0, (double?)e["z"] ?? 0, (string?)e["ship"], (int?)e["to"] ?? 0, (string?)e["order"]) { Received = Clock() };
        Events.Add(ev);
        if (Events.Count > 200) Events.RemoveAt(0);
        Note($"COOP_EVENT {ev.Kind} {Crew.NameOf(ev.By)}: {ev.Text}");
        OnEvent?.Invoke(ev);
    }

    // a crew change seen here (each peer applies it from the tick, so each notes it itself)
    private void LocalEvent(int by, string text) => AddEvent(new JsonObject { ["kind"] = "crew", ["text"] = text, ["by"] = by, ["at"] = Battle?.T ?? 0 });

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
            case "event": HostEvent(from, m); break;
            case "desync": Desyncs++; Note($"COOP_DESYNC reported by {Crew.NameOf(from)} at tick {(long?)m["n"]}"); break;
        }
    }

    /// <summary>The host: a crew member joined; returns their id (the game sends them welcome and the roster).</summary>
    public int HostJoin(string name)
    {
        CrewMember m = Crew.Join(name);
        Note($"COOP_JOIN {m.Id} {m.Name}");
        SendRoster();
        HostEvent(m.Id, new JsonObject { ["t"] = "event", ["kind"] = "crew", ["text"] = "came aboard", ["at"] = Battle?.T ?? 0 });
        return m.Id;
    }

    /// <summary>The host: a crew member left (their ship goes back to the AI, their roles to their captain).</summary>
    public void HostLeave(int id)
    {
        if (Battle != null && Crew.Ships.Where(kv => kv.Value == id).Select(kv => kv.Key).FirstOrDefault() is { } ship)
            _ops.Add(new JsonObject { ["t"] = "crew", ["op"] = "ship", ["ship"] = ship, ["to"] = Crew.AdmiralId, ["by"] = Crew.AdmiralId });
        string who = Crew.NameOf(id);
        Crew.Leave(id);
        Note($"COOP_LEAVE {id}");
        HostEvent(Crew.AdmiralId, new JsonObject { ["t"] = "event", ["kind"] = "crew", ["text"] = $"{who} left the bridge", ["at"] = Battle?.T ?? 0 });
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
            case "event": AddEvent(m); break;
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
                Ship now = b.ApplyAs(ship, Crew.LockRoles(by, (string?)o["tab"]), captain, () => b.ApplyCall(name, args), Crew.NameOf(by));
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
        else
        {
            Note($"COOP_CREW {Crew.NameOf(by)} {(string?)op["op"]} {(string?)op["role"] ?? (string?)op["ship"]} to {Crew.NameOf(to)}");
            string what = (string?)op["op"] == "role" ? Crew.Roles[(string)op["role"]!].Name
                : Battle?.ShipById((string?)op["ship"])?.Name is { } sn ? $"command of the {sn}" : "a ship";
            bool ship = (string?)op["op"] == "ship";
            LocalEvent(by, ship && to == Crew.AdmiralId ? $"gave {what} back to the AI" : !ship && to == Crew.CaptainOf(Crew.BridgeOf(by)) ? $"took back {what}" : $"gave {what} to {Crew.NameOf(to)}");
        }
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
