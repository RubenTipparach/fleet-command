// Battle: the stand-in battle, one fixed step at a time (CLAUDE.md section 6.3). It owns the ships, the shots in flight
// and the battle log, and advances them by Tuning.StepS per Step(). Randomness comes from streams forked per subsystem
// from the scenario's seed, and ships are always visited in their scenario order, so a battle replays exactly from its
// seed and its orders. The player's orders are in Battle.Orders.cs; what numbers are worth is in Rules.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

/// <summary>A shot: a beam (drawn for its life, its damage already dealt) or a projectile flying at its target.
/// X, Z is where the shot is (a beam's is its start); SX, SZ and TX, TZ are its two ends as last resolved by
/// Battle.Ends: the mount it left (Mount, on the ship From) and its target (the ship To, or the shot At for a point
/// defence beam), so a beam or a bolt keeps both ends on their ships as they move (owner, 2026-11-04).</summary>
public sealed class Shot
{
    public required string Kind { get; init; }
    public double X, Z, SX, SZ, TX, TZ;
    public string? To, From;
    public int Mount = -1;
    public Shot? At;
    public bool Hit, Spent, Intercept;
    public double Damage, SpeedMS, Life, TravelledM;
    public required string Color { get; init; }
    /// <summary>A missile's grade (data/battle/missiles.json); null for every other shot.</summary>
    public MissileGrade? Grade;
    /// <summary>Point defence hits still needed to stop it.</summary>
    public int PdHitsLeft = 1;
    /// <summary>The flare or decoy a missile has been fooled into chasing, or null.</summary>
    public Lure? Lure;
    /// <summary>A missile whose flare burnt out: it flies on, straight, and hits nothing.</summary>
    public bool Lost;
    /// <summary>The direction it last flew (a unit vector), for a lost missile.</summary>
    public double DX, DZ;
}

public sealed record LogLine(double T, string Who, string Text);

/// <summary>A hit as it landed, for the view's explosions and scars (it changes nothing): the ship struck, where the shot
/// came from (metres), the weapon's kind, what the shields took and what reached the hull, and whether it destroyed the ship.</summary>
public sealed record HitEvent(long Seq, double T, string ShipId, double FromX, double FromZ, string Kind, double Absorbed, double HullDamage, bool Destroyed);

public sealed partial class Battle
{
    public GameData Data { get; }
    public Rules Rules { get; }
    public Tuning Tuning => Data.Tuning;
    /// <summary>Every ship, in scenario order (the stable order every step uses).</summary>
    public IReadOnlyList<Ship> Ships => _ships;
    public IReadOnlyList<Shot> Shots => _shots;
    public IReadOnlyList<LogLine> Log => _log;
    /// <summary>The last hits that landed, oldest first, at most <see cref="HitMemory"/>; a reader keeps the last Seq it saw.</summary>
    public IReadOnlyList<HitEvent> Hits => _hits;
    /// <summary>How many hits are kept for the view.</summary>
    public const int HitMemory = 64;
    /// <summary>The ship the player commands.</summary>
    public Ship Player { get; private set; }
    /// <summary>Battle time, s.</summary>
    public double T { get; private set; }
    /// <summary>Steps taken; T is Steps times the step.</summary>
    public long Steps { get; private set; }
    /// <summary>The scenario this battle was made from.</summary>
    public Scenario Scenario { get; }
    /// <summary>null while fighting, else "won" or "lost".</summary>
    public string? Over { get; private set; }

    private readonly List<Ship> _ships = new();
    private readonly List<Shot> _shots = new();
    private readonly List<LogLine> _log = new();
    private readonly List<HitEvent> _hits = new();
    private long _hitSeq;
    private readonly Rng _hit, _dmg, _ai, _helm, _cm;

    public Battle(GameData data, Scenario scenario)
    {
        Data = data;
        Rules = new Rules(data.Tuning);
        _hit = Rng.Fork(scenario.Seed, "hit");
        _dmg = Rng.Fork(scenario.Seed, "damage");
        _ai = Rng.Fork(scenario.Seed, "ai");
        _helm = Rng.Fork(scenario.Seed, "helm");
        _cm = Rng.Fork(scenario.Seed, "countermeasures");
        for (int i = 0; i < scenario.Ships.Count; i++) _ships.Add(MakeShip(scenario.Ships[i], i));
        foreach (Ship s in _ships) s.DecoysLeft = data.Missiles.Decoy.PerShip;
        Player = _ships[0];
        Scenario = scenario;
        foreach (Ship s in _ships) if (s.Side == 1) s.Ecm = 1;
        Say(scenario.SayWho, scenario.SayText);
    }

    private Ship MakeShip(ShipStart d, int order)
    {
        ShipClass c = Data.Classes[d.Class];
        var s = new Ship
        {
            Id = d.Id, Order = order, Name = d.Name, Captain = d.Captain, Class = c, Faction = Data.Factions[d.Faction], Side = d.Side,
            X = d.XM, Z = d.ZM, H = Geometry.Wrap(d.HeadingDeg * Geometry.Deg), V = d.SpeedMS, VOrder = d.SpeedMS, Hull = c.HullHp * d.HullFrac,
            Capacitor = c.CapacitorE * Tuning.StartCapacitorFrac, Battery = c.BatteryE * Tuning.StartBatteryFrac,
            Stance = d.Stance, Range = d.Range, Erratic = d.Erratic, HoldFire = d.HoldFire,
        };
        s.HOrder = s.H;
        s.HetChance = Rules.HetMax(s);
        for (int i = 0; i < 6; i++) s.Shields[i] = c.ShieldsHp[i] * d.ShieldsFrac;
        foreach (string k in Rules.Systems) s.Power[k] = c.PowerE.GetValueOrDefault(k);
        foreach (string k in Rules.Subsystems) s.Sys[k] = 1;
        for (int i = 0; i < c.Mounts.Count; i++)
        {
            MountDef md = c.Mounts[i];
            WeaponDef w = Data.Weapons[md.Weapon];
            s.Mounts.Add(new Mount { Index = i, Def = md, Weapon = w, Charge = 0.6 + (i % 3) * 0.15, Selected = !w.IsPointDefence });
        }
        return s;
    }

    public Ship? ShipById(string? id) => id == null ? null : _ships.FirstOrDefault(s => s.Id == id);

    /// <summary>A ship's locked target, if it is still alive.</summary>
    public Ship? TargetOf(Ship s) { Ship? t = ShipById(s.Target); return t is { Alive: true } ? t : null; }

    public IEnumerable<Ship> EnemiesOf(Ship s) => _ships.Where(o => o.Alive && o.Side != s.Side);

    public void Say(string who, string text)
    {
        _log.Add(new LogLine(T, who, text));
        if (_log.Count > 80) _log.RemoveAt(0);
    }

    /// <summary>One fixed step of the whole battle.</summary>
    public void Step()
    {
        if (Over != null) return;
        double dt = Tuning.StepS;
        T += dt;
        Steps++;
        foreach (Ship s in _ships) if (s.Alive) SystemsStep(s, dt);
        foreach (Ship s in _ships)
        {
            if (!s.Alive || s == Player) continue;
            s.ThinkT -= dt;
            if (s.ThinkT <= 0) { s.ThinkT = Tuning.AiThinkS; Think(s); }
        }
        Ship p = Player;
        if (p.Alive && p.Engaged && p.Het == null && p.Broke <= 0 && NavTarget(p) is { } nt) Steer(p, p.Manoeuvre, nt, Rules.AiRangeKm());
        foreach (Ship s in _ships) if (s.Alive) Move(s, dt);
        PointDefenceStep();
        CountermeasuresStep(dt);
        ShotsStep(dt);
        if (!_ships.Any(s => s.Side == 1 && s.Alive)) { Over = "won"; Say("Tactical", "All hostile ships destroyed."); }
        else if (!_ships.Any(s => s.Side == 0 && s.Alive)) Over = "lost";
    }

    private void SystemsStep(Ship s, double dt)
    {
        ShipClass c = s.Class;
        double spare = Rules.Output(s) - Rules.Used(s);
        if (spare >= 0) { s.Battery = Math.Min(c.BatteryE, s.Battery + spare * dt * Tuning.BatteryChargePerSpareES); s.Brown = 1; }
        else { s.Battery = Math.Max(0, s.Battery + spare * dt); s.Brown = s.Battery > 0 ? 1 : Rules.Output(s) / Rules.Used(s); }
        s.Capacitor = Math.Min(c.CapacitorE, s.Capacitor + s.Power["weapons"] * s.Brown * dt * s.Sys["weapons"]);
        foreach (Mount m in s.Mounts)
        {
            if (m.State == Arming.Offline || m.Charge >= 1 || m.Hp <= 0) continue;
            WeaponMode w = Rules.Mode(m);
            double need = w.CostE * dt / w.ChargeS;
            if (s.Capacitor >= need) s.Capacitor -= need;
            else if (s.Battery >= need) s.Battery -= need;
            else continue;
            m.Charge = Math.Min(1, m.Charge + dt / w.ChargeS);
        }
        if (s.ShieldsUp)
        {
            double r = s.Power["shields"] * s.Brown * Tuning.ShieldRegenPerES * dt * s.Sys["shields"];
            for (int i = 0; i < 6; i++)
            {
                double add = s.Reinforce >= 0 ? r * 0.5 / 6 + (i == s.Reinforce ? r * 0.5 : 0) : r / 6;
                s.Shields[i] = Math.Min(c.ShieldsHp[i], s.Shields[i] + add);
            }
        }
        for (int i = 0; i < 6; i++) s.Flash[i] = Math.Max(0, s.Flash[i] - dt * 2);
        s.HetChance = Math.Min(Rules.HetMax(s), s.HetChance + Tuning.HetRecoverPerS * dt);
        s.Broke = Math.Max(0, s.Broke - dt);
        s.EstopT = Math.Max(0, s.EstopT - dt);
        if (s.Power["repair"] > 0)
        {
            string worst = Rules.Subsystems.OrderBy(k => s.Sys[k]).First();
            s.Sys[worst] = Math.Min(1, s.Sys[worst] + s.Power["repair"] * s.Brown * Tuning.RepairPerES * dt);
            foreach (Mount m in s.Mounts) if (m.Hp is > 0 and < 1) m.Hp = Math.Min(1, m.Hp + s.Power["repair"] * Tuning.MountRepairPerES * dt);
        }
    }

    private void Move(Ship s, double dt)
    {
        ShipClass c = s.Class;
        if (s.Het is { } left)
        {
            double st = Math.Sign(left) * Math.Min(Math.Abs(left), Tuning.HetRateDegS * Geometry.Deg * dt);
            s.H = Geometry.Wrap(s.H + st);
            s.HOrder = s.H;
            s.Het = Math.Abs(left - st) < 1e-4 ? null : left - st;
        }
        else if (s.Broke > 0) s.HOrder = s.H;
        else
        {
            double turn = c.TurnDegS * Geometry.Deg * dt * (0.5 + 0.5 * s.Sys["engines"]) * (s.Erratic ? Tuning.ErraticTurn : 1);
            double dh = Geometry.Wrap(s.HOrder - s.H);
            s.H = Geometry.Wrap(s.H + Math.Clamp(dh, -turn, turn));
        }
        if (s.Broke > 0) { s.V = 0; s.VOrder = 0; }
        if (s.Estop || s.EstopT > 0) s.VOrder = 0;
        double vmax = Rules.MaxSpeed(s), goal = Math.Clamp(s.VOrder, -Tuning.ReverseMaxMS, vmax), a = c.AccelMS2 * dt * (s.Estop ? Tuning.EstopDecelX : 1);
        s.V += Math.Clamp(goal - s.V, -a, a);
        if (s.Estop && Math.Abs(s.V) < 1) { s.V = 0; s.Estop = false; s.EstopT = Tuning.EstopHoldS; }
        var f = Geometry.Forward(s.H);
        s.X += f.X * s.V * dt;
        s.Z += f.Z * s.V * dt;
    }

    /// <summary>Steering toward a ship by an order: approach, circle, evade or hold. Wingmen and enemies use this one.</summary>
    private void Steer(Ship s, string order, Ship t, double rangeKm)
    {
        double vmax = Rules.MaxSpeed(s), toT = Geometry.HeadingTo(s.X, s.Z, t.X, t.Z), d = Rules.DistKm(s, t);
        switch (order)
        {
            case "approach": s.HOrder = toT; s.VOrder = d > rangeKm ? vmax : Math.Max(60, Math.Abs(t.V)); break;
            case "circle": s.HOrder = Geometry.Wrap(toT + Math.PI / 2 - Math.Clamp((d - rangeKm) / rangeKm, -1, 1) * Math.PI / 4); s.VOrder = vmax * 0.8; break;
            case "evade": s.HOrder = Geometry.Wrap(toT + Math.PI); s.VOrder = vmax; break;
            case "follow":
            {
                var f = Geometry.Forward(t.H);
                var r = Geometry.Right(t.H);
                double ax = t.X - f.X * 800 + r.X * 600, az = t.Z - f.Z * 800 + r.Z * 600, gap = Geometry.Dist(s.X, s.Z, ax, az);
                s.HOrder = gap > 200 ? Geometry.HeadingTo(s.X, s.Z, ax, az) : t.H;
                s.VOrder = Math.Clamp(Math.Abs(t.V) + gap * 0.2, 0, vmax);
                break;
            }
            case "intercept":
            {
                double tti = Geometry.Dist(s.X, s.Z, t.X, t.Z) / Math.Max(100, Math.Abs(s.V));
                var f = Geometry.Forward(t.H);
                s.HOrder = Geometry.HeadingTo(s.X, s.Z, t.X + f.X * t.V * tti, t.Z + f.Z * t.V * tti);
                break;
            }
            case "trail":
            {
                var f = Geometry.Forward(t.H);
                double ax = t.X - f.X * 600, az = t.Z - f.Z * 600, gap = Geometry.Dist(s.X, s.Z, ax, az);
                s.HOrder = gap > 200 ? Geometry.HeadingTo(s.X, s.Z, ax, az) : t.H;
                s.VOrder = Math.Clamp(Math.Abs(t.V) + (gap > 200 ? gap * 0.2 : 0), 0, vmax);
                break;
            }
            case "hold_range":
                s.HOrder = d > s.HoldKm * 1.1 ? toT : d < s.HoldKm * 0.9 ? Geometry.Wrap(toT + Math.PI) : Geometry.Wrap(toT + Math.PI / 2);
                s.VOrder = d > s.HoldKm * 1.1 || d < s.HoldKm * 0.9 ? vmax : Math.Max(60, Math.Abs(t.V));
                break;
            case "hit_run":
                if (s.RunPhase == "in" && d <= s.RunMinKm) s.RunPhase = "out";
                else if (s.RunPhase == "out" && d >= s.RunMaxKm) s.RunPhase = "in";
                s.HOrder = s.RunPhase == "in" ? toT : Geometry.Wrap(toT + Math.PI);
                s.VOrder = vmax;
                break;
            case "orbit":
            {
                double c2 = Math.Clamp((d - Tuning.OrbitKm) / Tuning.OrbitKm, -1, 1);
                s.HOrder = Geometry.Wrap(toT + s.OrbitDir * (Math.PI / 2 - c2 * Math.PI / 4));
                break;
            }
            default: s.VOrder = 0; break;
        }
    }

    private void Think(Ship s)
    {
        Ship[] foes = EnemiesOf(s).ToArray();
        if (foes.Length == 0) { s.VOrder = 60; return; }
        Ship near = foes.OrderBy(o => Geometry.Dist(s.X, s.Z, o.X, o.Z)).ThenBy(o => o.Order).First();
        Ship t = near;
        if (s.Side == 0 && Player.Alive && TargetOf(Player) is { } pt) t = pt;
        s.Target = t.Id;
        double range = Rules.AiRangeKm(s.Range);
        if (s.Stance == Stance.Hold) Steer(s, "hold", t, range);
        else if (s.Stance == Stance.Escort && s.Side == 0 && Player.Alive && s != Player) Steer(s, Rules.DistKm(s, t) < range ? "circle" : "follow", Rules.DistKm(s, t) < range ? t : Player, range);
        else Steer(s, Rules.DistKm(s, t) > range * 1.3 ? "approach" : "circle", t, range);
        if (s.HoldFire) return;
        foreach (Mount m in s.Mounts)
            if (!m.Weapon.IsPointDefence && Rules.Ready(m) && Rules.InArc(s, m, t) && Rules.HitChance(s, m, t) >= Tuning.AiFireMinChance) FireMount(s, m, t);
    }

    /// <summary>World position of a mount, metres.</summary>
    public static (double X, double Z) MountWorld(Ship s, Mount m)
    {
        double half = s.Class.LengthM / 2;
        var f = Geometry.Forward(s.H);
        var r = Geometry.Right(s.H);
        return (s.X + f.X * m.Def.Y * half + r.X * m.Def.X * half * 0.6, s.Z + f.Z * m.Def.Y * half + r.Z * m.Def.X * half * 0.6);
    }

    /// <summary>Fires one mount at t if it is ready, in arc and in range. The roll uses Rules.HitChance.</summary>
    private bool FireMount(Ship s, Mount m, Ship t)
    {
        if (!Rules.Ready(m) || !Rules.InArc(s, m, t) || !Rules.InRange(s, m, t)) return false;
        WeaponMode w = Rules.Mode(m);
        bool hit = _hit.Next() < Rules.HitChance(s, m, t);
        var (x, z) = MountWorld(s, m);
        m.Charge = 0;
        int mi = s.Mounts.IndexOf(m);
        if (!m.Weapon.IsPointDefence) VoidDecoy(s);   // firing main weapons voids its decoy (SFC2, printed page 136)
        if (!m.Weapon.Flies)
        {
            _shots.Add(new Shot { Kind = "beam", X = x, Z = z, SX = x, SZ = z, TX = t.X, TZ = t.Z, Life = Tuning.BeamLifeS, Color = m.Weapon.Color, To = t.Id, From = s.Id, Mount = mi, Hit = hit });
            if (hit) Damage(t, s.X, s.Z, s, w.Damage, m.Weapon.Kind);
        }
        else if (m.Weapon.Kind == "missile")
            _shots.Add(LaunchMissile(s, m, t, x, z, mi, hit));
        else
            _shots.Add(new Shot { Kind = m.Weapon.Kind, X = x, Z = z, SX = x, SZ = z, TX = t.X, TZ = t.Z, To = t.Id, From = s.Id, Mount = mi, Hit = hit, Damage = w.Damage, SpeedMS = m.Weapon.SpeedMS, Life = 12, Color = m.Weapon.Color });
        return true;
    }

    /// <summary>Damage from a source at (sx, sz): the facing it strikes absorbs what it can, the rest hits the hull and may hurt a subsystem or a mount.</summary>
    private void Damage(Ship t, double sx, double sz, Ship? src, double dmg, string kind)
    {
        if (!t.Alive) return;
        int f = Rules.FacingFrom(t, sx, sz);
        double absorb = t.ShieldsUp ? Math.Min(t.Shields[f], dmg) : 0;
        t.Shields[f] -= absorb;
        t.Flash[f] = 1;
        double rest = dmg - absorb;
        if (rest <= 0) { Record(t, sx, sz, kind, absorb, 0); return; }
        t.Hull -= rest;
        if (_dmg.Next() < Tuning.SubsystemHitChance)
        {
            string k = Rules.Subsystems[_dmg.Below(Rules.Subsystems.Length)];
            t.Sys[k] = Math.Max(0, t.Sys[k] - rest * Tuning.SubsystemDamagePerHp);
        }
        if (t.Mounts.Count > 0 && _dmg.Next() < Tuning.MountHitChance)
        {
            Mount m = t.Mounts[_dmg.Below(t.Mounts.Count)];
            m.Hp = Math.Max(0, m.Hp - Tuning.MountDamage);
            if (m.Hp <= 0) m.Charge = 0;
        }
        if (t == Player) Say("Engineering", $"Hull hit through the {Rules.Facings[f]} shield.");
        else if (src == Player) Say("Tactical", $"Hit on {t.Name}, {Rules.Facings[f]} facing.");
        Record(t, sx, sz, kind, absorb, rest);
        if (t.Hull <= 0)
        {
            t.Hull = 0; t.Alive = false; t.V = 0;
            _hits[^1] = _hits[^1] with { Destroyed = true };
            Say("Tactical", $"{t.Name} destroyed.");
            foreach (Ship o in _ships) if (o.Target == t.Id) o.Target = null;
            if (t == Player) PromoteNextCommand();
        }
    }

    private void Record(Ship t, double sx, double sz, string kind, double absorbed, double hull)
    {
        _hits.Add(new HitEvent(++_hitSeq, T, t.Id, sx, sz, kind, absorbed, hull, false));
        if (_hits.Count > HitMemory) _hits.RemoveAt(0);
    }

    /// <summary>When the commanded ship is lost, command passes to the next living ship of the fleet.</summary>
    private void PromoteNextCommand()
    {
        Ship? next = _ships.FirstOrDefault(o => o.Alive && o.Side == 0);
        if (next != null) { Player = next; Say("Comms", $"Command passes to {next.Name}."); }
    }

    /// <summary>A beam's or a bolt's two ends as the ships now stand (owner, 2026-11-04: "as they fire, the source and
    /// destination of these beams must follow the ship source and ship destination"): its mount on the firing ship, and its
    /// target ship (or, for a point defence beam, the shot it fires on). An end whose ship or shot is gone stays where it was
    /// last. One rule for the bolt's flight and for every view that draws a beam (CLAUDE.md 6.1).</summary>
    public (double SX, double SZ, double TX, double TZ) Ends(Shot sh)
    {
        Ship? s = ShipById(sh.From);
        if (s is { Alive: true } && sh.Mount >= 0 && sh.Mount < s.Mounts.Count) (sh.SX, sh.SZ) = MountWorld(s, s.Mounts[sh.Mount]);
        Ship? t = ShipById(sh.To);
        if (t is { Alive: true }) { sh.TX = t.X; sh.TZ = t.Z; }
        else if (sh.At is { } a) { sh.TX = a.X; sh.TZ = a.Z; }
        if (sh.Kind == "beam") { sh.X = sh.SX; sh.Z = sh.SZ; }
        return (sh.SX, sh.SZ, sh.TX, sh.TZ);
    }

    private void ShotsStep(double dt)
    {
        foreach (Shot sh in _shots)
        {
            sh.Life -= dt;
            if (sh.Kind == "beam") { Ends(sh); continue; }
            if (sh.Kind == "bolt") { BoltStep(sh, dt); continue; }
            if (sh.Kind == "missile") { MissileStep(sh, dt); continue; }
            Ship? t = ShipById(sh.To);
            if (t is not { Alive: true }) { sh.Life = Math.Min(sh.Life, 0.4); continue; }
            double d = Geometry.Dist(sh.X, sh.Z, t.X, t.Z), stepM = sh.SpeedMS * dt;
            if (d <= stepM + Tuning.ShotHitRadiusM)
            {
                Ship? from = ShipById(sh.From);
                if (sh.Hit && !sh.Spent) Damage(t, from?.X ?? sh.X, from?.Z ?? sh.Z, from, sh.Damage, sh.Kind);
                sh.Spent = true; sh.Life = 0;
            }
            else { sh.X += (t.X - sh.X) / d * stepM; sh.Z += (t.Z - sh.Z) / d * stepM; }
        }
        _shots.RemoveAll(sh => sh.Life <= 0);
    }

    /// <summary>A direct-fire bolt: it runs along the live line between its two ends (Ends), so it stays between the
    /// firing mount and its target while they move, and lands when it has run the line's length.</summary>
    private void BoltStep(Shot sh, double dt)
    {
        Ship? t = ShipById(sh.To);
        if (t is not { Alive: true }) { sh.Life = Math.Min(sh.Life, 0.4); return; }
        var (sx, sz, tx, tz) = Ends(sh);
        double len = Geometry.Dist(sx, sz, tx, tz);
        sh.TravelledM += sh.SpeedMS * dt;
        if (sh.TravelledM >= len - Tuning.ShotHitRadiusM)
        {
            Ship? from = ShipById(sh.From);
            if (sh.Hit && !sh.Spent) Damage(t, from?.X ?? sx, from?.Z ?? sz, from, sh.Damage, sh.Kind);
            sh.Spent = true; sh.Life = 0; sh.X = tx; sh.Z = tz;
            return;
        }
        double k = len > 0 ? sh.TravelledM / len : 1;
        sh.X = sx + (tx - sx) * k; sh.Z = sz + (tz - sz) * k;
    }

    private void PointDefenceStep()
    {
        var added = new List<Shot>();
        foreach (Ship s in _ships)
        {
            if (!s.Alive) continue;
            foreach (Mount m in s.Mounts)
            {
                if (!m.Weapon.IsPointDefence || !Rules.Ready(m)) continue;
                // a missile chasing a lure or lost is no threat; the rest are, nearest first in the stable shot order
                Shot? threat = _shots.FirstOrDefault(sh => sh.Kind != "beam" && sh.To == s.Id && !sh.Spent && sh.Lure == null && !sh.Lost
                    && Geometry.Dist(sh.X, sh.Z, s.X, s.Z) / 1000 <= m.Weapon.Normal.RangeKm);
                if (threat == null) continue;
                m.Charge = 0;
                var (px, pz) = MountWorld(s, m);
                added.Add(new Shot { Kind = "beam", X = px, Z = pz, SX = px, SZ = pz, TX = threat.X, TZ = threat.Z, Life = 0.15, Color = m.Weapon.Color, Intercept = true, From = s.Id, Mount = s.Mounts.IndexOf(m), At = threat });
                // a burst stops it pd_intercept_chance of the time; a blue or green missile needs two (its grade's pd_hits)
                if (_hit.Next() < Tuning.PdInterceptChance && --threat.PdHitsLeft <= 0) { threat.Spent = true; threat.Life = 0; }
            }
        }
        _shots.AddRange(added);
    }
}
