// Battle.Field: field operations (openspec/changes/mission-systems; owner, 2026-11-08: "The one big thing we need is sensor
// systems", "We have most of transporter abilities working, so implement missions for that as well"). In one place:
// - the sensors' reach (SensorKm): a contact beyond every friendly ship's reach is an unknown contact until it comes within
//   reach or is scanned (Identified), and every finished scan is remembered for the mission (ScannedIds);
// - planets: scanned like a ship from within the survey reach, which finds what stands on the surface; an away team beamed
//   down to an installation, which does its work, fights any garrison, and beams back up when its ship is in reach; the
//   envoy landed on a planet that receives one, or someone waiting on the surface beamed up;
// - ships that arrive late (warping in at their place), sleeping ships that wake at their time or when hit, fixed
//   installations that never move;
// - the battle's time limit and a world condition that cuts every ship's engines at times.
// Numbers are data/battle/systems/field.json (SystemsDef.Field). Randomness is the "away" stream (CLAUDE.md 6.3).
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    private FieldDef Fd => Data.Systems.Field;
    private readonly HashSet<string> _scanned = new(StringComparer.Ordinal);
    private readonly List<AwayTeam> _away = new();
    private Rng _awayRng = new(1);
    private string? _planetScan;
    private int? _firstFire;
    private bool _outage;

    /// <summary>The ships the player's side has finished scanning.</summary>
    public IReadOnlyCollection<string> ScannedIds => _scanned;
    /// <summary>Away teams on planets' surfaces now.</summary>
    public IReadOnlyList<AwayTeam> AwayTeams => _away;
    /// <summary>The planet the player is scanning, or null.</summary>
    public string? PlanetScanId => _planetScan;
    /// <summary>The side that fired first (0 the player's, 1 the enemy's), or null while no one has.</summary>
    public int? FirstFire => _firstFire;
    /// <summary>True once the battle ran to its time limit.</summary>
    public bool TimeUp { get; private set; }
    /// <summary>Seconds left of the time limit, or null with none.</summary>
    public double? TimeLeftS => Scenario.TimeLimitS > 0 ? Math.Max(0, Scenario.TimeLimitS - T) : null;
    /// <summary>True while a dead-engines outage holds every ship's engines.</summary>
    public bool EnginesOut => _outage;

    partial void FieldSetup()
    {
        _awayRng = Rng.Fork(Scenario.Seed, "away");
        for (int i = 0; i < _ships.Count; i++)
        {
            ShipStart d = Scenario.Ships[i];
            Ship s = _ships[i];
            s.Fixed = d.Fixed;
            s.Runs = d.Runs;
            if (d.Fixed) { s.V = 0; s.VOrder = 0; }
            if (d.WakeS > 0)
            {
                s.Dormant = true; s.WakeS = d.WakeS; s.WakeStance = s.Stance; s.WakeHoldFire = s.HoldFire;
                s.HoldFire = true; s.V = 0; s.VOrder = 0; s.DormantHull = s.Hull;
            }
            if (d.ArriveS > 0)
            {
                // it waits far off the field, unseen, and warps in at its place when its time comes
                s.Absent = true; s.ArriveS = d.ArriveS; s.ArriveX = s.X; s.ArriveZ = s.Z;
                s.X += Math.Sign(s.X == 0 ? 1 : s.X) * Fd.StageKm * 1000; s.Z -= Fd.StageKm * 1000;
                s.VOrder = 0; s.V = 0;
            }
        }
    }

    // ---- The sensors ------------------------------------------------------------------------------------------------

    /// <summary>A ship's sensor reach now, km: base plus each energy box in sensors, times the sensors' health, less on passive.</summary>
    public double SensorKm(Ship s) =>
        (Fd.Sensors.BaseKm + Fd.Sensors.PerEKm * Math.Max(0, s.Power["sensors"])) * s.Sys["sensors"] * (s.Passive ? Fd.Sensors.PassiveX : 1);

    /// <summary>Whether the player's side knows what t is (its name and class): its own side, within any friendly ship's
    /// sensor reach, or scanned. Otherwise it is an unknown contact.</summary>
    public bool Identified(Ship t) =>
        t.Side == Player.Side || _scanned.Contains(t.Id) || _ships.Any(o => o.Alive && !o.Absent && o.Side == Player.Side && Rules.DistKm(o, t) <= SensorKm(o));

    /// <summary>Scan planet: the planet selected (SelectPlanet) is scanned from within the survey reach.</summary>
    public void ScanPlanet()
    {
        if (_planets.FirstOrDefault(p => p.Id == PlanetDest) is not { } pl) { Say("Science", "No planet selected to scan."); return; }
        if (pl.Scanned) { Say("Science", $"{pl.Name} is already surveyed."); return; }
        _planetScan = pl.Id;
        double km = PlanetKm(Player, pl);
        Say("Science", km <= Fd.PlanetScanKm ? $"Surveying {pl.Name}." : $"Survey of {pl.Name} begins inside {Fd.PlanetScanKm:0} km; it is {km:0} km away.");
    }

    /// <summary>Whether a planet is within the survey reach of the commanded ship.</summary>
    public bool InSurveyReach(Planet p) => PlanetKm(Player, p) <= Fd.PlanetScanKm;

    // ---- Away teams -------------------------------------------------------------------------------------------------

    /// <summary>Why an away team cannot go to a site now, or null when it can: the site known and not yet held, no team
    /// already there, the planet within the transporter's reach, a pad ready and a marine aboard.</summary>
    public string? CannotBeamDown(string planet, string site)
    {
        Ship s = Player;
        if (_planets.FirstOrDefault(p => p.Id == planet) is not { } pl || pl.Installations.FirstOrDefault(i => i.Id == site) is not { } st) return "No such site.";
        if (!st.Known) return "Scan the planet first.";
        if (st.Secured) return $"{st.Name} is already ours.";
        if (_away.Any(a => a.Planet == planet && a.Site == site)) return "A team is already there.";
        if (PlanetKm(s, pl) > Comms.Transport.RangeKm) return $"{pl.Name} is out of transporter range ({Comms.Transport.RangeKm:0.#} km).";
        if (!PadReady(s)) return "Pads charging.";
        if (s.Marines <= 0) return "No marines aboard.";
        return null;
    }

    /// <summary>Beam down: an away team of up to the field's team size of marines to an installation on a planet.</summary>
    public bool BeamDown(string planet, string site)
    {
        if (CannotBeamDown(planet, site) is { } why) { Say("Transport", why); return false; }
        Ship s = Player;
        Planet pl = _planets.First(p => p.Id == planet);
        Installation st = pl.Installations.First(i => i.Id == site);
        int n = Math.Min(s.Marines, Fd.Away.TeamMarines);
        s.Marines -= n;
        _away.Add(new AwayTeam { Ship = s.Id, Planet = planet, Site = site, Marines = n, T = Fd.Away.AwayS });
        UsePad(s);
        BeamPair(s, pl, "crew");
        Say("Transport", $"Away team of {n} beamed down to {st.Name} on {pl.Name}.");
        return true;
    }

    // the envoy to a planet that receives one (dir above 0), or someone waiting on the surface beamed up (dir below 0)
    private void EnvoyToPlanet(Ship s, Planet p, int dir)
    {
        if (dir > 0)
        {
            if (s.Vip == null) return;
            if (!p.TakesEnvoy) { Say("Transport", $"{p.Name} has not asked for an envoy."); return; }
            p.EnvoyLanded = s.Vip; s.Vip = null;
            Say("Transport", $"{p.EnvoyLanded} beamed down to {p.Name}.");
            UsePad(s); BeamPair(s, p, "cargo");
        }
        else
        {
            if (p.Waiting == null) { Say("Transport", $"No one on {p.Name} is waiting to come up."); return; }
            if (s.Vip != null) { Say("Transport", "Our envoy's berth is taken."); return; }
            s.Vip = p.Waiting; p.Waiting = null;
            Say("Transport", $"{s.Vip} beamed aboard from {p.Name}.");
            UsePad(s); BeamPair(p, s, "cargo");
        }
    }

    // ---- Each step --------------------------------------------------------------------------------------------------

    // a dead-engines outage holds every ship's engines: no speed ordered, and the ships drift to a stop
    partial void FieldPreMove(double dt)
    {
        if (Scenario.DeadEngines is not { } de) return;
        double ph = T % de.EveryS;
        bool now = T >= de.EveryS && ph < de.ForS;
        if (now != _outage) Say("Engineering", now ? "Engines dead! Something out there is draining every drive in the field." : "Engines answering again.");
        _outage = now;
        if (now) foreach (Ship s in _ships) s.VOrder = 0;
    }

    partial void FieldStep(double dt)
    {
        foreach (Ship s in _ships)
        {
            if (!s.Alive) continue;
            if (s.Absent && T >= s.ArriveS)
            {
                s.Absent = false; s.X = s.ArriveX; s.Z = s.ArriveZ; s.V = Rules.MaxSpeed(s) * 0.5; s.VOrder = s.V;
                Say("Science", s.Side == Player.Side ? $"The {s.Name} drops out of warp beside us." : Identified(s) ? $"Contact: the {s.Name} drops out of warp." : "Unknown contact dropping out of warp!");
            }
            if (s.Dormant && (T >= s.WakeS || s.Hull < s.DormantHull))
            {
                s.Dormant = false; s.Stance = s.WakeStance; s.HoldFire = s.WakeHoldFire;
                Say("Science", $"The {s.Name} is powering up!");
            }
        }
        PlanetScanStep(dt);
        AwayStep(dt);
        if (Scenario.TimeLimitS > 0 && T >= Scenario.TimeLimitS && Over == null)
        {
            TimeUp = true;
            Over = "time";
            Say("Tactical", "Time.");
        }
    }

    private void PlanetScanStep(double dt)
    {
        if (_planetScan == null || !Player.Alive || _planets.FirstOrDefault(p => p.Id == _planetScan) is not { } pl) return;
        if (!InSurveyReach(pl)) return;
        Ship s = Player;
        var sr = Cs.Sensors;
        int free = Math.Max(0, s.Power["sensors"] - s.Ecm - s.Eccm);
        double rate = (sr.ScanBasePerS + sr.ScanPerES * free) * (s.Passive ? sr.PassiveScanX : 1) * s.Sys["sensors"] * (1 + OfficerSkill(s, "science") * sr.OfficerScan) * Fd.PlanetScanX;
        pl.ScanP = Math.Min(1, pl.ScanP + dt * rate);
        if (pl.ScanP < 1) return;
        _planetScan = null;
        foreach (Installation i in pl.Installations) i.Known = true;
        string found = pl.Installations.Count == 0 ? "nothing built on the surface" : string.Join(", ", pl.Installations.Select(i => i.Hostile ? $"{i.Name} (armed, {i.Garrison} on guard)" : i.Name));
        Say("Science", $"Survey of {pl.Name} complete: {found}.");
    }

    private void AwayStep(double dt)
    {
        for (int k = 0; k < _away.Count; k++)
        {
            AwayTeam a = _away[k];
            Ship? s = ShipById(a.Ship);
            Planet pl = _planets.First(p => p.Id == a.Planet);
            Installation st = pl.Installations.First(i => i.Id == a.Site);
            if (s is not { Alive: true }) { Say("Transport", $"The away team on {pl.Name} has no ship to come home to."); _away.RemoveAt(k--); continue; }
            if (!a.Done)
            {
                a.T -= dt;
                if (a.T > 0) continue;
                a.Done = true;
                int wins = 0, lost = 0;
                for (int i = 0; i < a.Marines; i++)
                {
                    if (_awayRng.Next() < Fd.Away.WinP) wins++;
                    else if (st.Hostile && _awayRng.Next() < Fd.Away.LossP) lost++;
                }
                a.Marines -= lost;
                if (!st.Hostile || wins >= st.Garrison)
                {
                    st.Secured = true; st.Garrison = 0;
                    Say("Security", $"Away team: {st.Name} is ours. {Found(st)}{(lost > 0 ? $" We lost {lost}." : "")}");
                }
                else
                {
                    st.Failed = true; st.Garrison = Math.Max(1, st.Garrison - wins);
                    Say("Security", $"Away team: driven back at {st.Name}, {st.Garrison} still on guard.{(lost > 0 ? $" We lost {lost}." : "")}");
                }
            }
            // done: it beams back up as soon as its ship is in reach with a pad ready
            if (PlanetKm(s, pl) <= Comms.Transport.RangeKm && PadReady(s))
            {
                s.Marines += a.Marines;
                UsePad(s);
                BeamPair(pl, s, "crew");
                Say("Transport", a.Marines > 0 ? $"Away team beamed up from {pl.Name}: {a.Marines} aboard." : $"No one left to beam up from {pl.Name}.");
                _away.RemoveAt(k--);
            }
        }
    }

    // what a team reports from a site it holds, by the site's kind
    private static string Found(Installation st) => st.Kind switch
    {
        "lab" => "Their research cores are copied.",
        "ruin" => "The ruin's records are recovered.",
        "mine" => "The ore manifests are ours.",
        "battery" => "The guns are spiked.",
        "city" => "The city's council receives us.",
        "wreck" => "The wreck's logs are pulled.",
        _ => "The outpost's logs are pulled.",
    };
}
