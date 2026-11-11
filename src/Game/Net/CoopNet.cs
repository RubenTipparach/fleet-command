// CoopNet: co-op's connection (openspec/changes/bridge-crew-coop, design "Connection"), over Godot's ENet: the admiral's
// game hosts (--host[=port]), each crew member's joins (--join=address[:port] --name=...). One reliable channel of JSON
// messages, one per packet. The pattern is Star Crew's (the owner, 2026-11-11: "we can use our star crew system for
// connecting players together"; its sc-net: hello, welcome, the roster, intents up and the host's word down, a peer dropped
// after silence), carried here by ENet rather than its WebRTC: Godot ships ENet on Windows and Android, and a LAN or a
// forwarded port is enough for the first co-op. What the messages mean is the lockstep's (Coop.Lockstep) and, for starting
// and stopping a battle and the pause, the composition root's (Main): this node only carries them.
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using FleetCommand.Coop;
using Godot;

namespace FleetCommand.Game.Net;

public sealed partial class CoopNet : Node
{
    public const int DefaultPort = 7790;

    public Lockstep Lock { get; private set; } = null!;
    public bool IsHost { get; private set; }
    /// <summary>A crew peer: joined and welcomed by the host.</summary>
    public bool Connected { get; private set; }
    /// <summary>The address this peer hosts on or joined.</summary>
    public string Where { get; private set; } = "";
    /// <summary>A crew peer: told the host's game-flow messages (start, stop, pause, screen).</summary>
    public Action<JsonObject>? OnFlow;
    /// <summary>A crew peer: told the connection was lost.</summary>
    public Action? OnLost;

    private ENetMultiplayerPeer _peer = null!;
    private string _name = "";
    // the host: each connected peer's member id once it said hello
    private readonly Dictionary<int, int> _members = new();

    /// <summary>Hosts on a port: the admiral's game.</summary>
    public Error Host(Lockstep ls, int port)
    {
        Lock = ls; IsHost = true;
        _peer = new ENetMultiplayerPeer();
        Error e = _peer.CreateServer(port, 8);
        if (e != Error.Ok) return e;
        _peer.PeerConnected += id => Log($"COOP_PEER connected {id}");
        _peer.PeerDisconnected += id =>
        {
            if (_members.Remove((int)id, out int m)) Lock.HostLeave(m);
        };
        ls.Send = Broadcast;
        Where = $"port {port}";
        Log($"COOP_HOST listening on {port}");
        return Error.Ok;
    }

    /// <summary>Joins a host at address[:port] as a crew member named <paramref name="name"/>.</summary>
    public Error Join(Lockstep ls, string address, string name)
    {
        Lock = ls; IsHost = false; _name = name;
        int port = DefaultPort;
        string host = address;
        if (address.LastIndexOf(':') is var c and > 0 && int.TryParse(address[(c + 1)..], out int p)) { host = address[..c]; port = p; }
        _peer = new ENetMultiplayerPeer();
        Error e = _peer.CreateClient(host, port);
        if (e != Error.Ok) return e;
        _peer.PeerConnected += id => { if (id == 1) SendTo(1, new JsonObject { ["t"] = "hello", ["name"] = _name }); };
        _peer.PeerDisconnected += id => { if (id == 1) { Connected = false; Log("COOP_LOST the host"); OnLost?.Invoke(); } };
        ls.Send = m => SendTo(1, m);
        Where = $"{host}:{port}";
        Log($"COOP_JOIN connecting to {host}:{port} as {name}");
        return Error.Ok;
    }

    /// <summary>The host: a message to every crew member that has said hello.</summary>
    public void Broadcast(JsonObject m)
    {
        if (_peer == null) return;
        foreach (int id in _members.Keys) SendTo(id, m);
    }

    private void SendTo(int peer, JsonObject m)
    {
        if (_peer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Connected) return;
        _peer.SetTargetPeer(peer);
        _peer.TransferMode = MultiplayerPeer.TransferModeEnum.Reliable;
        _peer.PutPacket(Encoding.UTF8.GetBytes(m.ToJsonString()));
    }

    /// <summary>How many crew members are connected (the host).</summary>
    public int CrewCount => _members.Count;

    public override void _Process(double delta) => Poll();

    /// <summary>Takes in every packet that arrived (also called before the lockstep plays, so nothing waits a frame).</summary>
    public void Poll()
    {
        // a connection that closed (the host left) is polled no more
        if (_peer == null || _peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected) return;
        _peer.Poll();
        while (_peer.GetAvailablePacketCount() > 0)
        {
            int from = _peer.GetPacketPeer();
            byte[] pkt = _peer.GetPacket();
            JsonObject? m;
            try { m = JsonNode.Parse(Encoding.UTF8.GetString(pkt)) as JsonObject; }
            catch (System.Text.Json.JsonException) { m = null; }
            if (m == null) { Log($"COOP_BAD packet from {from}"); continue; }
            if (IsHost) HostGot(from, m);
            else CrewGot(m);
        }
    }

    private void HostGot(int from, JsonObject m)
    {
        if ((string?)m["t"] == "hello")
        {
            if (_members.ContainsKey(from)) return;
            int id = Lock.HostJoin((string?)m["name"] ?? "");
            _members[from] = id;
            SendTo(from, new JsonObject { ["t"] = "welcome", ["you"] = id });
            Lock.SendRoster();
            Joined?.Invoke(from, id);
            return;
        }
        if (_members.TryGetValue(from, out int member)) Lock.HostReceive(member, m);
    }

    /// <summary>The host: told a crew member joined (ENet peer, member id), so the game can send them where things stand.</summary>
    public Action<int, int>? Joined;

    /// <summary>The host: a message to one crew member by their member id.</summary>
    public void SendToMember(int member, JsonObject m)
    {
        foreach (var (peer, id) in _members) if (id == member) SendTo(peer, m);
    }

    private void CrewGot(JsonObject m)
    {
        switch ((string?)m["t"])
        {
            case "welcome": Connected = true; Lock.Receive(m); break;
            case "roster": case "tick": Lock.Receive(m); break;
            default: OnFlow?.Invoke(m); break;
        }
    }

    public override void _ExitTree()
    {
        _peer?.Close();
    }

    private static void Log(string line) => GD.Print(line);
}
