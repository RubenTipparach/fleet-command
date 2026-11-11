// Battle.Transport: the transporter and boarding (the battle console mockup's TRANSPORT, BOARD and INV rules, ported rule
// for rule): the pads, the 1.5 km reach through a downed shield facing, the boarding queues, capture (a party aboard
// fighting in volleys, medics, hackers, the envoy, a deck held until an engineer and a hacker are aboard, the ship taken
// and changing sides, a pilot needed to get it under way), hit and run raids on a subsystem or one weapon, troops beamed
// to a friendly ship, the inventory transporter (goods, supplies and the envoy to or from a friendly ship, station or
// planet), an AI captain's capture order, and a test scene's goal. Every readout calls the rule the outcome uses (CLAUDE.md
// 6.1): BeamReach decides boarding, raids and the panel's "in reach" alike. Randomness is the "boarding" stream (6.3).
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

/// <summary>A transporter beam as it happened, for the view's effect (it changes nothing): its two ends in metres, sender
/// first, whether each end is a planet's (a wider column), and what it carried ("crew", "cargo" or "mine").</summary>
public sealed record BeamEvent(long Seq, double T, double AX, double AZ, double BX, double BZ, bool ABig, bool BBig, string Kind);

/// <summary>A transport destination: a friendly ship or station, or a planet; its distance (to a planet's surface), km.</summary>
public sealed record TransportDest(string Id, string Name, double Km, Ship? Ship, Planet? Planet)
{
    public ICargoHolder Holder => (ICargoHolder?)Ship ?? Planet!;
}

public sealed partial class Battle
{
    /// <summary>Comms, transport and boarding's numbers.</summary>
    public CommsDef Comms => Data.Systems.Comms;
    /// <summary>The planets of this battle (a scene's), in scenario order.</summary>
    public IReadOnlyList<Planet> Planets => _planets;
    /// <summary>The last transporter beams, oldest first; a reader keeps the last Seq it saw.</summary>
    public IReadOnlyList<BeamEvent> Beams => _beams;
    /// <summary>The planet the player selected for transport and the helm, or null.</summary>
    public string? PlanetDest { get; private set; }

    /// <summary>The subsystems by name, as the console and the log say them (the mockup's SUBSYS).</summary>
    public static readonly IReadOnlyDictionary<string, string> SubsystemNames = new Dictionary<string, string>
    {
        ["weapons"] = "Weapons control", ["shields"] = "Shield generators", ["sensors"] = "Sensors", ["reactor"] = "Reactor",
        ["engines"] = "Engines", ["tractor"] = "Tractor beam", ["transport"] = "Transporters",
    };
    /// <summary>One of each role, as the log names it (the mockup's ROLE_ONE).</summary>
    public static readonly IReadOnlyDictionary<string, string> RoleOne = new Dictionary<string, string>
    {
        ["marines"] = "Marine", ["demo"] = "Demolition", ["medics"] = "Medic", ["engineers"] = "Engineer", ["hackers"] = "Hacker",
        ["pilots"] = "Pilot", ["guard"] = "Armed crew", ["vip"] = "Envoy",
    };
    /// <summary>Each item's name (the mockup's ITEMS).</summary>
    public static readonly IReadOnlyDictionary<string, string> ItemNames = new Dictionary<string, string>
    {
        ["goods"] = "Goods", ["parts"] = "Spare parts", ["rations"] = "Rations", ["medicine"] = "Medicine",
    };
    /// <summary>The roles each boarding mode queues: capture any but demolition, hit and run marines and demolition.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> QueueRoles = new Dictionary<string, string[]>
    {
        ["capture"] = new[] { "marines", "medics", "engineers", "hackers", "pilots" }, ["hit"] = new[] { "marines", "demo" },
    };
    public const int BeamMemory = 32;

    private readonly List<Planet> _planets = new();
    private readonly List<BeamEvent> _beams = new();
    private readonly Dictionary<string, Ship> _planetNav = new();
    private long _beamSeq;
    private Rng _board = new(1);

    // Each ship's crew, pads and hold from its class (comms.json), then its scenario setup; the scene's planets; an unarmed
    // test scene's own ship stripped of its weapons (the mockup's armed: false).
    partial void CommsSetup()
    {
        _board = Rng.Fork(Scenario.Seed, "boarding");
        CommsDef cm = Comms;
        foreach (Ship s in _ships)
        {
            CrewDef crew = cm.CrewOf(s.Class.Id);
            s.Pads = Enumerable.Repeat(1.0, crew.Pads).ToArray();
            s.Marines = crew.Marines;
            foreach (string r in CommsDef.Specialists) s.Team[r] = crew.Team.GetValueOrDefault(r);
            s.Guard = cm.Board.GuardCrew;
            s.HoldCap = crew.HoldT;
            foreach (string k in CommsDef.Items) s.Hold[k] = 0;
            if (Scenario.Ships[s.Order].Setup is { } su) Apply(s, su);
        }
        foreach (PlanetDef p in Scenario.Planets ?? Array.Empty<PlanetDef>())
        {
            var pl = new Planet { Id = p.Id, Name = p.Name, X = p.XM, Z = p.ZM, RadiusM = p.RadiusM, Color = p.Color, Needs = p.Needs.Count > 0 ? new Dictionary<string, int>(p.Needs) : null,
                Kind = p.Kind, TakesEnvoy = p.TakesEnvoy, Waiting = p.Waiting };
            foreach (InstallationDef d in p.Installations)
                pl.Installations.Add(new Installation { Id = d.Id, Name = d.Name, Kind = d.Kind, LatDeg = d.LatDeg, LonDeg = d.LonDeg, Hostile = d.Hostile, Garrison = d.Garrison, Known = d.Known });
            foreach (string k in CommsDef.Items) pl.Hold[k] = 0;
            _planets.Add(pl);
        }
        if (!Scenario.Armed) Player.Mounts.Clear();
    }

    private static void Apply(Ship s, ShipSetup su)
    {
        if (su.Marines is { } m) s.Marines = m;
        foreach (var (k, v) in su.Team) s.Team[k] = v;
        foreach (var (k, v) in su.Hold) s.Hold[k] = v;
        if (su.Needs is { Count: > 0 }) s.Needs = new Dictionary<string, int>(su.Needs);
        s.Vip = su.Vip;
        s.CaptureOrder = su.Capture;
        s.NeedMarines = su.NeedMarines;
        s.Distress = su.Distress;
        foreach (var (k, v) in su.Power) s.Power[k] = v;
        foreach (var (k, v) in su.Sys) s.Sys[k] = v;
        if (su.ShieldsDown) { s.ShieldsUp = false; s.Power["shields"] = 0; Array.Fill(s.Shields, 0); }
        for (int i = 0; i < su.MountsHp.Count && i < s.Mounts.Count; i++) { s.Mounts[i].Hp = su.MountsHp[i]; if (su.MountsHp[i] <= 0) s.Mounts[i].Charge = 0; }
    }

    // ---- The pads ---------------------------------------------------------------------------------------------------

    public bool PadReady(Ship s) => s.Pads.Any(c => c >= 1);
    public int PadsCharging(Ship s) => s.Pads.Count(c => c < 1);
    /// <summary>The energy that recharges the pads: the transporter's boxes by its health.</summary>
    public double PadPower(Ship s) => s.Power["transport"] * s.Sys["transport"];
    /// <summary>Seconds until pad i is charged at today's energy and pads charging (infinite with no energy to it).</summary>
    public double PadEta(Ship s, int i)
    {
        if (i < 0 || i >= s.Pads.Length || s.Pads[i] >= 1) return 0;
        double e = PadPower(s);
        return e > 0 ? (1 - s.Pads[i]) * Comms.Transport.PadChargeS * PadsCharging(s) / e : double.PositiveInfinity;
    }

    /// <summary>Spends a charged pad (the first, or pad i); false when it is not charged.</summary>
    public bool UsePad(Ship s, int i = -1)
    {
        if (i < 0) i = Array.FindIndex(s.Pads, c => c >= 1);
        if (i < 0 || i >= s.Pads.Length || !(s.Pads[i] >= 1)) return false;
        s.Pads[i] = 0;
        return true;
    }

    // every step the charging pads share the transporter's energy
    private void PadsStep(Ship s, double dt)
    {
        int n = PadsCharging(s);
        if (n == 0) return;
        double r = PadPower(s) * dt / (Comms.Transport.PadChargeS * n);
        for (int i = 0; i < s.Pads.Length; i++) if (s.Pads[i] < 1) s.Pads[i] = Math.Min(1, s.Pads[i] + r);
    }

    // ---- Who can be reached ---------------------------------------------------------------------------------------

    /// <summary>Whether t's shield facing toward s is down (at or under shield_down_hp, its shields lowered, or its core tripped by lightning).</summary>
    public bool FacingDown(Ship t, Ship s) => !t.ShieldsUp || t.CoreDown || t.Shields[Rules.FacingFrom(t, s.X, s.Z)] <= Comms.Transport.ShieldDownHp;

    /// <summary>Whether s's transporter can reach t now (the-federation's beam_check): inside range, a pad charged, and for
    /// an enemy its facing toward s down. One rule for capture, hit and run and the troops (6.1).</summary>
    public bool BeamReach(Ship s, Ship? t) => t is { Alive: true } && PadReady(s) && Rules.DistKm(s, t) <= Comms.Transport.RangeKm && (t.Side == s.Side || FacingDown(t, s));

    public bool CanBoard(Ship s, Ship? t) => BeamReach(s, t) && t!.Side != s.Side && s.Raid == null && (s.Party == null || s.Party.On == t.Id);
    public bool CanRaid(Ship s, Ship? t) => BeamReach(s, t) && t!.Side != s.Side && s.Party == null && (s.Raid == null || s.Raid.On == t.Id);
    public bool CanReinforce(Ship s, Ship? o) => o is { Alive: true } && o.Side == s.Side && PadReady(s) && Rules.DistKm(s, o) <= Comms.Transport.RangeKm;

    /// <summary>A ship's defenders: its marines and its armed crew.</summary>
    public static int Defenders(Ship t) => t.Marines + t.Guard;
    /// <summary>A party can take a ship with an engineer and a hacker (or the envoy) aboard.</summary>
    public static bool CanTake(Party p) => p.Of("engineers") > 0 && (p.Of("hackers") > 0 || p.Of("vip") > 0);
    /// <summary>The roles a party still lacks to take a ship (the same rule as CanTake).</summary>
    public static string[] MissingRoles(Party p) =>
        (p.Of("engineers") > 0 ? Array.Empty<string>() : new[] { "engineers" }).Concat(p.Of("hackers") > 0 || p.Of("vip") > 0 ? Array.Empty<string>() : new[] { "hackers" }).ToArray();
    /// <summary>A captured ship with no pilot aboard cannot get under way: it holds where it is and does not fight.</summary>
    public static bool PilotNeeded(Ship s) => s.Captured && s.Team.GetValueOrDefault("pilots") <= 0;
    /// <summary>Everyone in a team but the envoy.</summary>
    public static int TeamSize(IReadOnlyDictionary<string, int> team) => team.Where(kv => kv.Key != "vip").Sum(kv => kv.Value);
    /// <summary>How many of a role are aboard s.</summary>
    public static int Have(Ship s, string role) => role == "marines" ? s.Marines : role == "vip" ? (s.Vip != null ? 1 : 0) : s.Team.GetValueOrDefault(role);

    // the queues and the roster hold combat marines and specialists (the envoy travels by the inventory transporter)
    private static void AddRole(Ship s, string role, int n)
    {
        if (role == "marines") s.Marines += n;
        else s.Team[role] = s.Team.GetValueOrDefault(role) + n;
    }

    /// <summary>The ship a Capture beam goes to: the locked ship, an enemy to board or a friendly one to send troops to
    /// (owner, 2026-11-03: "supply troops for an assault (combat transport tab)").</summary>
    public Ship? CaptureDest(Ship s) => TargetOf(s, "transport");

    /// <summary>With nothing queued the pads beam marines, one a pad, from those aboard (owner, 2026-11-04: "by default
    /// marines get transported").</summary>
    public static string[] DefaultLoad(Ship s) => Enumerable.Repeat("marines", Math.Max(0, Math.Min(s.Marines, s.Pads.Length))).ToArray();

    // ---- The player's orders ------------------------------------------------------------------------------------------

    /// <summary>The boarding queue (owner, 2026-11-02: "transport will follow a queue pattern"): one of the role moves off
    /// the roster into the mode's queue.</summary>
    public void QueueRole(string mode, string role)
    {
        if (!QueueRoles.TryGetValue(mode, out string[]? roles) || !roles.Contains(role)) return;
        Ship s = Player;
        List<string> q = s.Queue[mode];
        if (Have(s, role) <= 0 || q.Count >= Comms.Transport.QueueMax) return;
        AddRole(s, role, -1);
        q.Add(role);
    }

    /// <summary>Puts queued member i back on the roster.</summary>
    public void Unqueue(string mode, int i)
    {
        if (!Player.Queue.TryGetValue(mode, out List<string>? q) || i < 0 || i >= q.Count) return;
        AddRole(Player, q[i], 1);
        q.RemoveAt(i);
    }

    /// <summary>Pad i energized beams the head of the queue (owner: "I can click on pads individually").</summary>
    public int Energize(int pad, string mode) => pad >= 0 && pad < Player.Pads.Length && QueueRoles.ContainsKey(mode) ? BeamQueued(Player, mode, new[] { pad }) : 0;
    /// <summary>Every charged pad fires, one person each, in pad order.</summary>
    public int EnergizeAll(string mode) => QueueRoles.ContainsKey(mode) ? BeamQueued(Player, mode, Enumerable.Range(0, Player.Pads.Length).ToArray()) : 0;

    /// <summary>The party beamed back aboard, every role and the envoy.</summary>
    public void RecallParty()
    {
        Ship s = Player;
        if (s.Party is not { } p) return;
        foreach (string r in CommsDef.Roles) AddRole(s, r, p.Of(r));
        if (p.Of("vip") > 0) s.Vip ??= p.VipName;
        Say("Transport", $"{TeamSize(p.Team)} recalled.");
        s.Party = null;
    }

    /// <summary>What the next raid aims at: a subsystem; for weapons, one mount (null: weapons control).</summary>
    public void AimRaid(string sys, int? mount = null)
    {
        if (!SubsystemNames.ContainsKey(sys)) return;
        Player.RaidSys = sys;
        Player.RaidMount = sys == "weapons" ? mount : null;
    }

    /// <summary>Shoot it up, or bomb it.</summary>
    public void RaidKind(string kind) { if (kind is "shoot" or "bomb") Player.RaidKind = kind; }

    /// <summary>What a raid is aimed at, in words: a weapon by its id and name, or the subsystem.</summary>
    public static string RaidName(Raid r, Ship? t) =>
        r.Mount is { } i && t != null && i < t.Mounts.Count ? $"{t.Mounts[i].Label} {t.Mounts[i].Weapon.Name.ToLowerInvariant()}" : SubsystemNames[r.Sys].ToLowerInvariant();

    private static string Count(int n, string role) => $"{n} {RoleOne[role].ToLowerInvariant()}{(n > 1 && role != "demo" ? "s" : "")}";

    // Beams the head of s's queue for the mode through each listed pad that is charged: in Capture each joins the party
    // aboard the target (made with the first) or the troops of a friendly ship, in Hit and run the raid on the subsystem
    // picked. Says what went, or why nothing did.
    private int BeamQueued(Ship s, string mode, IReadOnlyList<int> pads)
    {
        Ship? t = mode == "capture" ? CaptureDest(s) : TargetOf(s, "transport");
        bool byDefault = s.Queue[mode].Count == 0;
        List<string> q = byDefault ? DefaultLoad(s).ToList() : s.Queue[mode];
        var sent = new List<(string Role, int N)>();
        int n = 0;
        bool friend = false;
        string Take()
        {
            string role = q[0];
            q.RemoveAt(0);
            if (byDefault) AddRole(s, role, -1);
            int k = sent.FindIndex(e => e.Role == role);
            if (k < 0) sent.Add((role, 1)); else sent[k] = (role, sent[k].N + 1);
            n++;
            return role;
        }
        foreach (int i in pads)
        {
            if (q.Count == 0) break;
            if (i < 0 || i >= s.Pads.Length || !(s.Pads[i] >= 1)) continue;
            if (mode == "capture" && t != null && t.Side == s.Side)
            {
                if (!CanReinforce(s, t)) break;
                string fr = Take();
                s.Pads[i] = 0; friend = true;
                AddRole(t, fr, 1);
                continue;
            }
            if (!(mode == "capture" ? CanBoard(s, t) : CanRaid(s, t))) break;
            string role = Take();
            s.Pads[i] = 0;
            if (mode == "capture")
            {
                if (s.Party is { } pt) { pt.Team[role] = pt.Of(role) + 1; pt.Held = false; }
                else
                {
                    s.Party = new Party { On = t!.Id, VolleyT = Comms.Board.VolleyS };
                    foreach (string r in CommsDef.Roles) s.Party.Team[r] = 0;
                    s.Party.Team["vip"] = 0;
                    s.Party.Team[role] = 1;
                }
            }
            else if (s.Raid is { } rd) rd.Team[role] = rd.Team.GetValueOrDefault(role) + 1;
            else
            {
                s.Raid = new Raid { On = t!.Id, Sys = s.RaidSys, Mount = s.RaidSys == "weapons" && s.RaidMount is { } m && m < t.Mounts.Count ? m : null, Kind = s.RaidKind, T = Comms.Board.RaidS };
                s.Raid.Team[role] = 1;
            }
        }
        if (n > 0) BeamPair(s, t!, "crew");
        if (s != Player) return n;
        double km = Comms.Transport.RangeKm;
        if (n > 0)
            Say("Transport", $"Energizing: {string.Join(", ", sent.Select(e => Count(e.N, e.Role)))} to {t!.Name}{(mode == "hit" ? $"'s {RaidName(s.Raid!, t)}" : "")}{(friend ? $" ({t.Marines} marines aboard)" : "")}.");
        else
            Say("Transport", q.Count == 0 ? (byDefault ? "No marines aboard to beam." : "The queue is empty.") : t == null ? "No target." : !pads.Any(i => i >= 0 && i < s.Pads.Length && s.Pads[i] >= 1) ? "Pads charging."
                : Rules.DistKm(s, t) > km || t.Side == s.Side ? $"Out of reach ({km:0.0} km)." : mode == "hit" && s.Party != null ? "A boarding party is out."
                : mode == "capture" && s.Raid != null ? "A raid is out." : "Their shield facing is up.");
        return n;
    }

    // ---- The fight aboard and the raids ------------------------------------------------------------------------------

    /// <summary>A boarding party's marines' health, one entry a marine, kept in step with its count (new marines arrive whole).</summary>
    public static List<double> PartyHp(Party p)
    {
        while (p.Hp.Count < p.Of("marines")) p.Hp.Add(1);
        while (p.Hp.Count > p.Of("marines")) p.Hp.RemoveAt(p.Hp.Count - 1);
        return p.Hp;
    }

    private int VolleyHits(int n, double p) { int h = 0; for (int i = 0; i < n; i++) if (_board.Next() < p) h++; return h; }

    // One hit the defenders land on the party: a hacker if they are exposed, else someone at random; a marine is wounded
    // (killed at no health). Returns what happened, for the log.
    private string? WoundParty(Party p)
    {
        BoardDef bd = Comms.Board;
        List<double> hp = PartyHp(p);
        if (p.Of("hackers") > 0 && p.Of("marines") < bd.HackerCover && _board.Next() < bd.HackerExposedP) { p.Team["hackers"]--; return "hacker"; }
        int n = TeamSize(p.Team);
        if (n <= 0) { p.Team["vip"] = 0; return null; }
        double k = _board.Next() * n;
        foreach (string role in CommsDef.Roles)
        {
            if (k < p.Of(role))
            {
                if (role != "marines") { p.Team[role]--; return role; }
                int i = (int)(_board.Next() * hp.Count);
                hp[i] -= bd.Wound;
                if (hp[i] > 0) return "wounded";
                hp.RemoveAt(i);
                p.Team["marines"]--;
                return "marines";
            }
            k -= p.Of(role);
        }
        return null;
    }

    // The ship strikes its colours: it joins s's side, held by the party's marines and specialists.
    private void Capture(Ship s, Ship t, Party p)
    {
        t.Side = s.Side; t.Captured = true; t.Target = null; t.Stance = Stance.Escort; t.HoldFire = false;
        t.Engaged = false; t.Distress = false;
        t.Marines = p.Of("marines");
        foreach (string r in CommsDef.Specialists) t.Team[r] = p.Of(r);
        t.Vip = p.Of("vip") > 0 ? p.VipName : null;
        s.Party = null;
        DropLocksOn(t.Id, o => o.Side != t.Side);
        Say("Transport", $"{t.Name} strikes its colours. {p.Of("marines")} marines hold it{(p.Of("vip") > 0 ? ", the envoy aboard" : "")}.");
        if (PilotNeeded(t)) { t.AwaitPilot = true; Say("Transport", $"{t.Name} is ours but dead in space: pilot needed. Beam a pilot over (Capture, {t.Name} selected)."); }
    }

    // Raids and boarding parties, every step, for the ship that sent them.
    private void BoardingStep(Ship s, double dt)
    {
        BoardDef bd = Comms.Board;
        if (s.Raid is { } r)
        {
            Ship? t = ShipById(r.On);
            if (t is not { Alive: true }) s.Raid = null;
            else if ((r.T -= dt) <= 0)
            {
                string name = RaidName(r, t);
                name = char.ToUpperInvariant(name[0]) + name[1..];
                int esc = r.Team["marines"], marines = 0, demo = 0;
                for (int i = 0; i < r.Team["marines"]; i++) if (_board.Next() >= bd.MarineLoss) marines++;
                for (int i = 0; i < r.Team["demo"]; i++) if (_board.Next() >= bd.DemoLoss / (1 + esc)) demo++;
                double hit = r.Kind == "bomb" ? (demo > 0 ? bd.BombDmg : 0) : Math.Min(bd.ShootMax, marines * bd.ShootPerMarine);
                if (hit > 0)
                {
                    if (r.Mount is { } mi && mi < t.Mounts.Count) { Mount m = t.Mounts[mi]; m.Hp = Math.Max(0, m.Hp - hit); if (m.Hp <= 0) m.Charge = 0; }
                    else
                    {
                        t.Sys[r.Sys] = Math.Max(0, t.Sys[r.Sys] - hit);
                        if (r.Sys == "weapons") foreach (Mount m in t.Mounts) if (_board.Next() < hit) { m.Hp = Math.Max(0, m.Hp - hit); if (m.Hp <= 0) m.Charge = 0; }
                    }
                }
                s.Marines += marines; s.Team["demo"] = s.Team.GetValueOrDefault("demo") + demo; s.Raid = null;
                int went = r.Team["marines"] + r.Team["demo"];
                if (s == Player || t == Player)
                    Say("Transport", hit > 0 ? $"Raid on {t.Name}: {name} {(r.Kind == "bomb" ? "bombed" : "shot up")}. {marines + demo} of {went} back."
                        : $"Raid on {t.Name} failed: {(r.Kind == "bomb" ? "no demolition marine lived to set the charge" : "no one left to shoot")}. {marines + demo} back.");
            }
        }
        if (s.Party is not { } pt) return;
        Ship? on = ShipById(pt.On);
        if (on is not { Alive: true }) { s.Party = null; return; }
        if (on.Side == s.Side) { s.Party = null; return; }
        if (Defenders(on) <= 0)
        {
            if (CanTake(pt)) Capture(s, on, pt);
            else if (!pt.Held)
            {
                pt.Held = true;
                Say("Transport", $"{on.Name}'s deck is ours, but to take it we need {string.Join(", ", MissingRoles(pt).Select(x => x == "engineers" ? "an engineer" : "a hacker"))}.");
            }
            return;
        }
        if ((pt.VolleyT -= dt) > 0) return;
        // hackers speed the fight up; the medics treat the wounded before the volley
        pt.VolleyT += bd.VolleyS * Math.Max(1 - bd.HackerSpeedMax, 1 - bd.HackerSpeed * pt.Of("hackers"));
        List<double> hp = PartyHp(pt);
        bool loud = s == Player || pt.Loud;
        int healed = 0, medics = pt.Of("medics");
        for (int i = 0; i < medics; i++)
        {
            int j = -1;
            for (int k = 0; k < hp.Count; k++) if (hp[k] < 1 && (j < 0 || hp[k] < hp[j])) j = k;
            if (j < 0) break;
            hp[j] = Math.Min(1, hp[j] + bd.Heal);
            healed++;
        }
        if (healed > 0 && loud) Say("Boarding", $"Medic{(medics > 1 ? "s" : "")} aboard {on.Name} treat{(medics > 1 ? "" : "s")} {healed} wounded marine{(healed > 1 ? "s" : "")}.");
        // simultaneous: both sides' shots counted before any hit lands
        int att = VolleyHits(pt.Of("marines"), bd.HitChance), def = VolleyHits(Defenders(on), bd.HitChance), locked = 0, talked = 0;
        for (int i = 0; i < pt.Of("hackers"); i++) if (_board.Next() < bd.HackerLock) locked++;
        if (pt.Of("vip") > 0 && _board.Next() < bd.VipPersuade) talked = 1;
        on.Guard = Math.Max(0, on.Guard - locked);
        int down = att + talked, fromMarines = Math.Min(down, on.Marines);
        on.Marines -= fromMarines;
        on.Guard = Math.Max(0, on.Guard - (down - fromMarines));
        var hurt = new List<(string Role, int N)>();
        for (int i = 0; i < def; i++)
        {
            if (WoundParty(pt) is not { } w) continue;
            int k = hurt.FindIndex(e => e.Role == w);
            if (k < 0) hurt.Add((w, 1)); else hurt[k] = (w, hurt[k].N + 1);
        }
        int Hurt(string role) => hurt.FirstOrDefault(e => e.Role == role).N;
        if (Hurt("hacker") > 0 && loud) Say("Boarding", $"{Hurt("hacker")} hacker{(Hurt("hacker") > 1 ? "s" : "")} shot aboard {on.Name}: too few marines ({pt.Of("marines")}) to cover them.");
        pt.Last = new Volley(att, def, locked, talked);
        if (Defenders(on) <= 0 && CanTake(pt)) Capture(s, on, pt);
        else if (TeamSize(pt.Team) <= 0) { s.Party = null; Say("Transport", $"Boarding party on {on.Name} lost."); }
        else if (loud)
        {
            string[] lost = hurt.Where(e => e.Role is not ("wounded" or "hacker")).Select(e => Count(e.N, e.Role)).ToArray();
            string what = string.Join(", ", new[] { Hurt("wounded") > 0 ? $"{Hurt("wounded")} wounded" : "", lost.Length > 0 ? $"lost {string.Join(", ", lost)}" : "" }.Where(x => x != ""));
            Say("Boarding", $"Volley aboard {on.Name}: {att} hit{(att == 1 ? "" : "s")}{(locked > 0 ? $", {locked} locked out" : "")}{(talked > 0 ? ", 1 talked down" : "")}; {(what == "" ? "no one hurt" : what)}. {pt.Of("marines")} marines against {Defenders(on)}.");
        }
    }

    // ---- The inventory transporter -------------------------------------------------------------------------------------

    /// <summary>What a ship or planet still wants of an item (its needs less what it has been given).</summary>
    public static int StillNeeds(ICargoHolder o, string k) => Math.Max(0, (o.Needs?.GetValueOrDefault(k) ?? 0) - o.Got.GetValueOrDefault(k));
    public static bool NeedsMet(ICargoHolder o) => o.Needs != null && CommsDef.Items.All(k => StillNeeds(o, k) <= 0);
    public static int HoldUsed(Ship s) => CommsDef.Items.Sum(k => s.Hold.GetValueOrDefault(k));

    /// <summary>What a ship or planet still asks for, in words ("10 spare parts, 8 marines"), empty when nothing.</summary>
    public static string NeedsText(ICargoHolder o)
    {
        var want = CommsDef.Items.Where(k => StillNeeds(o, k) > 0).Select(k => $"{StillNeeds(o, k)} {ItemNames[k].ToLowerInvariant()}").ToList();
        if (o is Ship s && s.NeedMarines > 0 && s.Marines < s.NeedMarines) want.Add($"{s.NeedMarines - s.Marines} marines");
        return string.Join(", ", want);
    }

    /// <summary>Transport destinations: friendly ships and stations, and planets (reach measured to the surface), nearest first.</summary>
    public IEnumerable<TransportDest> InvDests(Ship s)
    {
        var list = new List<TransportDest>();
        foreach (Ship o in _ships) if (o.Alive && o != s && o.Side == s.Side) list.Add(new TransportDest(o.Id, o.Name, Rules.DistKm(s, o), o, null));
        foreach (Planet p in _planets) list.Add(new TransportDest(p.Id, p.Name, PlanetKm(s, p), null, p));
        return list.OrderBy(d => d.Km).ThenBy(d => d.Id, StringComparer.Ordinal);
    }

    /// <summary>A planet's distance from a ship to its surface, km.</summary>
    public static double PlanetKm(Ship s, Planet p) => Math.Max(0, Geometry.Dist(s.X, s.Z, p.X, p.Z) - p.RadiusM) / 1000;

    /// <summary>The transport destination (owner, 2026-11-03: "allow my ship to select planets, stations and friendly ships.
    /// same as enemy ships. This is how we commence transport"): the friendly ship locked, else the planet selected. The
    /// panel, the transfers, the range ring and the helm all ask this (6.1).</summary>
    public TransportDest? InvPick()
    {
        Ship s = Player;
        if (TargetOf(s, "transport") is { } t && t.Side == s.Side) return new TransportDest(t.Id, t.Name, Rules.DistKm(s, t), t, null);
        return _planets.FirstOrDefault(p => p.Id == PlanetDest) is { } pl ? new TransportDest(pl.Id, pl.Name, PlanetKm(s, pl), null, pl) : null;
    }

    public bool InvReach(Ship s, TransportDest? d) => d != null && PadReady(s) && d.Km <= Comms.Transport.RangeKm;

    /// <summary>Selects a planet as the transport destination and the helm's (a friendly ship is selected by locking it,
    /// so a friendly lock is released for the planet).</summary>
    public void SelectPlanet(string id)
    {
        if (_planets.FirstOrDefault(p => p.Id == id) is not { } pl) return;
        if (TargetOf(Player, "transport") is { } t && t.Side == Player.Side) Unlock();
        PlanetDest = id;
        double km = PlanetKm(Player, pl), reach = Comms.Transport.RangeKm;
        _reachDest = id; _inReach = km <= reach;
        Say("Transport", $"Selected {pl.Name}: {(km <= reach ? "in transporter range" : $"{km:0.0} km away, transporters reach {reach:0.0} km")}.");
    }

    /// <summary>The inventory transporter (owner: "transport goods onto a planet"): units of an item, or the envoy ("vip"),
    /// to (dir above 0) or from the destination picked, a beam at a time.</summary>
    public void Transfer(string what, int dir)
    {
        Ship s = Player;
        if (what != "vip" && !ItemNames.ContainsKey(what)) return;
        if (InvPick() is not { } d) { Say("Transport", "No destination: click a friendly ship, station or planet."); return; }
        double reach = Comms.Transport.RangeKm;
        if (!InvReach(s, d)) { Say("Transport", d.Km > reach ? $"Out of transporter range: {d.Name} is {d.Km:0.0} km away ({reach:0.0} km)." : "Pads charging."); return; }
        if (what == "vip")
        {
            if (d.Planet is { } pv) { EnvoyToPlanet(s, pv, dir); return; }
            if (d.Ship is not { } o) return;
            if (dir > 0) { if (s.Vip == null) return; o.Vip = s.Vip; s.Vip = null; Say("Transport", $"{o.Vip} beamed to {o.Name}."); }
            else { if (o.Vip == null) return; s.Vip = o.Vip; o.Vip = null; Say("Transport", $"{s.Vip} beamed aboard."); }
            UsePad(s);
            if (dir > 0) BeamPair(s, o, "cargo"); else BeamPair(o, s, "cargo");
            return;
        }
        string label = ItemNames[what].ToLowerInvariant();
        ICargoHolder h = d.Holder;
        if (dir > 0)
        {
            // a planet takes only what it asks for; a ship takes what its hold has room for
            int room = d.Planet != null ? StillNeeds(h, what) : d.Ship!.HoldCap - HoldUsed(d.Ship) + StillNeeds(h, what);
            int n = Math.Min(Comms.Inventory.PerBeam, Math.Min(s.Hold[what], room));
            if (n <= 0) { Say("Transport", s.Hold[what] <= 0 ? $"No {label} aboard." : $"{d.Name} cannot take more {label}."); return; }
            s.Hold[what] -= n;
            int used = Math.Min(n, StillNeeds(h, what));
            h.Got[what] = h.Got.GetValueOrDefault(what) + used;
            if (n > used) h.Hold[what] = h.Hold.GetValueOrDefault(what) + n - used;
            int asked = h.Needs?.GetValueOrDefault(what) ?? 0;
            Say("Transport", $"{n} {label} beamed to {d.Name}{(asked > 0 ? $" ({h.Got[what]} of {asked})" : "")}.");
            if (NeedsMet(h)) Relieve(h);
        }
        else
        {
            if (d.Ship is not { } o) { Say("Transport", $"{d.Name} has nothing to send up."); return; }
            int n = Math.Min(Comms.Inventory.PerBeam, Math.Min(o.Hold[what], s.HoldCap - HoldUsed(s)));
            if (n <= 0) { Say("Transport", o.Hold[what] <= 0 ? $"{d.Name} has no {label}." : "Our hold is full."); return; }
            o.Hold[what] -= n; s.Hold[what] += n;
            Say("Transport", $"{n} {label} beamed aboard from {d.Name}.");
        }
        UsePad(s);
        if (dir > 0) BeamPair(s, (object?)d.Ship ?? d.Planet!, "cargo"); else BeamPair(d.Ship!, s, "cargo");
    }

    // A ship or planet whose needs are all met says so, once; a ship in distress repairs its engines and gets under way.
    private void Relieve(ICargoHolder o)
    {
        if (o is Planet { Relieved: true } || o is Ship { Relieved: true }) return;
        if (o is Planet p) p.Relieved = true;
        Say("Comms", $"{o.Name}: everything we asked for is aboard. Thank you, {Player.Name}.");
        if (o is not Ship s) return;
        s.Relieved = true;
        s.Sys["engines"] = 1; s.Sys["sensors"] = 1;
        if (!s.Distress) return;
        s.Distress = false;
        s.Stance = Stance.Escort;
        s.VOrder = Comms.DistressSpeedMS;
        Say("Comms", $"{s.Name}: engines repaired, getting under way.");
    }

    // ---- The effect's events -------------------------------------------------------------------------------------------

    // both ends of a beam, sender first: a ship's centre, or a planet's surface nearest the other end
    private void BeamPair(object from, object to, string kind)
    {
        static (double X, double Z) C(object o) => o is Planet p ? (p.X, p.Z) : (((Ship)o).X, ((Ship)o).Z);
        (double X, double Z, bool Big) End(object o, object other)
        {
            var (x, z) = C(o);
            if (o is not Planet p) return (x, z, false);
            var (ox, oz) = C(other);
            double d = Math.Max(1, Geometry.Dist(x, z, ox, oz));
            return (x + (ox - x) / d * p.RadiusM, z + (oz - z) / d * p.RadiusM, true);
        }
        var a = End(from, to);
        var b = End(to, from);
        _beams.Add(new BeamEvent(++_beamSeq, T, a.X, a.Z, b.X, b.Z, a.Big, b.Big, kind));
        if (_beams.Count > BeamMemory) _beams.RemoveAt(0);
    }

    // ---- The helm toward a planet, the AI's capture order, and the scene's goal ----------------------------------------

    // A planet as the helm sees it: a still point under the planet's name (the mockup's navTarget pseudo-ship), never in
    // the battle's ship list.
    private Ship? PlanetNav()
    {
        if (_planets.FirstOrDefault(p => p.Id == PlanetDest) is not { } pl) return null;
        if (!_planetNav.TryGetValue(pl.Id, out Ship? m))
            _planetNav[pl.Id] = m = new Ship { Id = pl.Id, Order = -1, Name = pl.Name, Captain = "", Class = Player.Class, Faction = Player.Faction, Side = Player.Side, X = pl.X, Z = pl.Z };
        return m;
    }

    partial void ThinkHook(Ship s, ref bool handled)
    {
        // a prize with no pilot holds still and does not fight; a pilot beamed aboard gets it under way
        if (s.AwaitPilot && !PilotNeeded(s)) { s.AwaitPilot = false; Say("Transport", $"Pilot aboard the {s.Name}: under way."); }
        if (PilotNeeded(s)) { s.VOrder = 0; s.HOrder = s.H; handled = true; return; }
        if (s.CaptureOrder != null && AiCapture(s)) handled = true;
    }

    // An AI captain ordered to capture a ship: it waits for the envoy (the VIP scene) or for the marines it asked for (the
    // Assault scene: owner, 2026-11-03, "be asked to supply troops for an assault"), then closes and boards with them all.
    private bool AiCapture(Ship s)
    {
        Ship? st = ShipById(s.CaptureOrder);
        if (st is not { Alive: true } || st.Side == s.Side) return false;
        bool ready = s.Vip != null || (s.NeedMarines > 0 && s.Marines >= s.NeedMarines);
        if (!ready && s.Party == null) { s.VOrder = 0; return true; }
        if (s.NeedMarines > 0 && !s.Told) { s.Told = true; Say("Comms", $"{s.Name}: {s.Marines} marines aboard. Moving on {st.Name}."); }
        s.Target = st.Id;
        Steer(s, "approach", st, 2);
        if (s.Party == null && CanBoard(s, st))
        {
            var p = new Party { On = st.Id, VolleyT = Comms.Board.VolleyS, Loud = true };
            p.Team["marines"] = s.Marines;
            p.Team["demo"] = 0;
            foreach (string r in new[] { "medics", "engineers", "hackers", "pilots" }) { p.Team[r] = s.Team.GetValueOrDefault(r); s.Team[r] = 0; }
            p.Team["vip"] = s.Vip != null ? 1 : 0;
            p.VipName = s.Vip;
            s.Marines = 0; s.Vip = null;
            UsePad(s);
            BeamPair(s, st, "crew");
            s.Party = p;
            Say("Comms", $"{s.Name}: boarding {st.Name} with {p.Of("marines")} marines{(p.Of("vip") > 0 ? " and the envoy" : "")}.");
        }
        return true;
    }

    partial void ObjectivesStep(ref bool ended)
    {
        if (Scenario.Goal is not { } g) return;
        // a scene with a goal is won by it, whatever enemies are left, and lost with the fleet
        ended = true;
        if (!_ships.Any(s => s.Side == Player.Side && s.Alive)) { Over = "lost"; return; }
        Ship? gs = ShipById(g.Id);
        if (gs is { Alive: false }) { Say("Comms", $"Objective failed: the {gs.Name} was destroyed."); Over = "lost"; return; }
        ICargoHolder? holder = (ICargoHolder?)gs ?? _planets.FirstOrDefault(p => p.Id == g.Id);
        bool met = g.Kind == "taken" ? gs != null && gs.Side == Player.Side : holder != null && NeedsMet(holder);
        if (!met) return;
        Say("Comms", $"Objective complete: {Scenario.Objective}.");
        Win();
    }

    partial void TransportStep(double dt)
    {
        foreach (Ship s in _ships)
        {
            if (!s.Alive) continue;
            PadsStep(s, dt);
            BoardingStep(s, dt);
        }
    }
}
