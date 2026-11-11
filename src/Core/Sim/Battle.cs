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
    /// <summary>Where it was at the start of the last step (NaN until it has flown one), so the view can draw it between
    /// steps (Battle.DrawnAt).</summary>
    public double PX = double.NaN, PZ = double.NaN;
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
    /// <summary>Its share that goes straight through a shield (the forge slug's pierce_x).</summary>
    public double Pierce;
    /// <summary>The ships and lance mounts that joined a chorus lance.</summary>
    public List<(string Ship, int Mount)>? Joined;
    /// <summary>Where its hit is meant to land (Strike: "hull", a subsystem, or "w:" and a mount index), whether its weapon aims
    /// ("precise") or lands by arc, and its chopping block's reach (degrees; 0 is the strike's own).</summary>
    public string Aim = "hull", AimBy = "arc";
    public double ReachDeg;
}

/// <summary>A line of the battle's log: when, which station or system says it, what, and in co-op the player whose order it
/// answers (or who wrote it, for chat).</summary>
public sealed record LogLine(double T, string Who, string Text, string? By = null)
{
    /// <summary>Who speaks, as the log shows it: the station, after the player's name in co-op ("Laptop · Tactical").</summary>
    public string Speaker => By != null ? $"{By} · {Who}" : Who;
}

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
        Rules = new Rules(data.Tuning, data.Systems.Tactical) { SystemsData = data.Systems };
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
        RangeSetup();
        ReactorSetup();
        SystemsSetup();
        TractorSetup();
        CommsSetup();   // the crews first: the marines aboard, which the security teams are formed from
        CloakSetup();
        FieldSetup();
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
            Stance = d.Stance, Range = d.Range, Erratic = d.Erratic, HoldFire = d.HoldFire, Prize = d.Prize,
        };
        s.HOrder = s.H;
        s.HetChance = Rules.HetMax(s);
        for (int i = 0; i < 6; i++) s.Shields[i] = c.ShieldsHp[i] * d.ShieldsFrac;
        foreach (string k in Rules.Systems) s.Power[k] = d.PowerE?.GetValueOrDefault(k, c.PowerE.GetValueOrDefault(k)) ?? c.PowerE.GetValueOrDefault(k);
        foreach (string k in Rules.Subsystems) s.Sys[k] = d.SysHp?.GetValueOrDefault(k, 1) ?? 1;
        // a ship that starts with no shields keeps none: a target drone or a wreck range hulk has them down and unpowered,
        // so they never build back up (owner, 2026-11-06: "drones have too much shields in shake down cruise")
        if (d.ShieldsFrac <= 0) { s.ShieldsUp = false; s.Power["shields"] = 0; }
        var mounts = d.ExtraMounts == null ? c.Mounts : c.Mounts.Concat(d.ExtraMounts).ToList();
        for (int i = 0; i < mounts.Count; i++)
        {
            MountDef md = mounts[i];
            WeaponDef w = Data.Weapons[md.Weapon];
            bool armed = d.ArmOnly == null || d.ArmOnly.Contains(md.Weapon);   // a weapons test scene arms its one weapon
            s.Mounts.Add(new Mount { Index = i, Def = md, Weapon = w, Charge = armed ? 0.6 + (i % 3) * 0.15 : 0, Selected = !w.IsPointDefence,
                State = armed ? Arming.Normal : Arming.Offline });
        }
        return s;
    }

    public Ship? ShipById(string? id) => id == null ? null : _ships.FirstOrDefault(s => s.Id == id);

    /// <summary>A ship's locked target, if it is still alive.</summary>
    public Ship? TargetOf(Ship s) { Ship? t = ShipById(s.Target); return t is { Alive: true } ? t : null; }

    public IEnumerable<Ship> EnemiesOf(Ship s) => _ships.Where(o => o.Alive && !o.Struck && o.Side != s.Side);

    /// <summary>Whether the captain has ordered a withdrawal: the warp drive arms, plots a course out and jumps as soon as
    /// it is ready (Battle.Warp), and the battle ends as an escape.</summary>
    public bool Withdrawing { get; private set; }

    /// <summary>The Withdraw key: a withdrawal ordered, or, pressed again while one is under way, called off.</summary>
    public bool ToggleWithdraw()
    {
        if (Sent(nameof(ToggleWithdraw))) return false;
        if (Withdrawing) { CancelWithdraw(); return true; }
        return Withdraw();
    }

    /// <summary>The menu's Withdraw (owner, 2026-11-07: "select withdraw from menu, then warp out"): refused on a story or
    /// special mission; otherwise the warp drive is armed and plots a course, and the ship jumps out the moment it can.
    /// Returns false when refused.</summary>
    public bool Withdraw()
    {
        if (Sent(nameof(Withdraw))) return false;
        if (Over != null || Withdrawing || !Player.Alive) return false;
        if (!Scenario.CanWithdraw)
        {
            Say("Helm", "We cannot withdraw: this mission must be seen through.");
            Notify("Withdraw refused", "This mission must be seen through", "caution");
            return false;
        }
        Withdrawing = true;
        WarpState w = Player.Warp;
        if (!w.Armed) { w.Armed = true; w.Want = Data.Systems.Warp.CoilE; }
        if (!w.Plotting) PlotCourse();
        // engineering routes the weapons' energy to the coils, as much as the drive wants beyond what is free (the
        // tractor's way of finding power)
        int free = Math.Max(0, (int)Math.Floor(Rules.Output(Player) - Rules.Used(Player)));
        int take = Math.Clamp(w.Want - Player.Power["warp"] - free, 0, Player.Power["weapons"]);
        Player.Power["weapons"] -= take;
        Say("Helm", take > 0 ? "Withdrawing: weapons power to the warp coils, plotting a course out." : "Withdrawing: charging the warp drive and plotting a course out.");
        return true;
    }

    /// <summary>Ends the battle as a completed withdrawal without flying the jump: for the mission preview and tests that
    /// grade a withdrawal, never for the player (who warps out by Withdraw).</summary>
    public void EndWithdrawn()
    {
        if (Sent(nameof(EndWithdrawn))) return;
        if (Over != null) return;
        Over = "withdrawn";
    }

    /// <summary>The mission's objectives are met (a battle that ends on its objectives): it is won where it stands.</summary>
    public void Win() { if (Sent(nameof(Win))) return; if (Over == null) { Over = "won"; Say("Tactical", "Objectives complete."); } }

    private readonly HashSet<string> _reached = new();
    private double _lastHurtT;
    /// <summary>The marker beacons reached so far, by id.</summary>
    public IReadOnlyCollection<string> Reached => _reached;

    /// <summary>A marker beacon of this battle by id, or null.</summary>
    public MarkerDef? ScenarioMarker(string id) => Scenario.Markers?.FirstOrDefault(m => m.Id == id);

    /// <summary>A marker is shown and can be reached once the one it follows has been.</summary>
    public bool MarkerActive(MarkerDef m) => m.After == null || _reached.Contains(m.After);

    // the commanded ship reaches each shown marker by coming within its radius
    private void MarkersStep()
    {
        if (Scenario.Markers == null || !Player.Alive) return;
        foreach (MarkerDef m in Scenario.Markers)
        {
            if (_reached.Contains(m.Id) || !MarkerActive(m)) continue;
            if (Geometry.Dist(Player.X, Player.Z, m.XM, m.ZM) > m.RadiusM) continue;
            _reached.Add(m.Id);
            Say("Helm", $"{m.Label} reached.");
        }
    }

    /// <summary>The battle is lost though ships of side 0 still fly (a mission whose own fleet is gone, its wingmen left).</summary>
    public void Lose() { if (Sent(nameof(Lose))) return; if (Over == null) Over = "lost"; }

    public void Say(string who, string text) => SayAs(OrderBy, who, text);

    /// <summary>A log line in a player's name (co-op: chat, a crew change, a ping), or none.</summary>
    public void SayAs(string? by, string who, string text)
    {
        _log.Add(new LogLine(T, who, text, by));
        if (_log.Count > 80) _log.RemoveAt(0);
    }

    partial void CommsStep(double dt);
    partial void TransportStep(double dt);
    partial void TractorStep(double dt);
    partial void MinesStep(double dt);
    partial void CraftStep(double dt);
    partial void ReactorStep(double dt);
    partial void WarpStep(double dt);
    partial void RepairCrewsStep(double dt);
    partial void CloakStep(double dt);
    partial void NebulaStep(double dt);
    partial void SensorsStep(double dt);
    partial void SecurityStep(double dt);
    partial void TargetingStep(double dt);
    partial void FleetStep(double dt);
    /// <summary>Field operations (Battle.Field): late arrivals, sleepers, planets and away teams set up from the scenario.</summary>
    partial void FieldSetup();
    partial void FieldPreMove(double dt);
    partial void FieldStep(double dt);
    /// <summary>Called once a battle is built, for each system to set its own state up from the scenario.</summary>
    partial void SystemsSetup();
    /// <summary>The scene ends systems have registered: each is checked after every step, before the default end (the
    /// last enemy gone, or the fleet), and one that returns true has ended the battle (set Over).</summary>
    private readonly List<Func<bool>> _ends = new();
    /// <summary>Comms and transport's set-up: crews, pads, holds and planets from the scenario (Battle.Transport).</summary>
    partial void CommsSetup();
    /// <summary>An AI captain's thinking taken over by a system (a prize waiting for its pilot, a capture order); handled
    /// skips the rest of Think.</summary>
    partial void ThinkHook(Ship s, ref bool handled);
    /// <summary>A test scene's goal (Battle.Transport): ended skips the battle's usual end by its enemies.</summary>
    partial void ObjectivesStep(ref bool ended);

    /// <summary>One fixed step of the whole battle.</summary>
    /// <summary>Where a shot is drawn a share <paramref name="a"/> (0 to 1) of the way through the step now playing: between
    /// where it stood before the last step and where it stands now, so a fast shot glides at any frame rate rather than
    /// jumping a step at a time (owner, 2026-11-06: "why does weapon appear jittery?"). View only; the rules read X and Z.</summary>
    public static (double X, double Z) DrawnAt(Shot sh, double a) =>
        double.IsNaN(sh.PX) ? (sh.X, sh.Z) : (sh.PX + (sh.X - sh.PX) * a, sh.PZ + (sh.Z - sh.PZ) * a);

    public void Step()
    {
        // inside a step every order the AI or a system gives is applied, never relayed (Battle.Crew.cs)
        _inside++;
        try { StepOnce(); }
        finally { _inside--; }
    }

    private void StepOnce()
    {
        if (Over != null) return;
        double dt = Tuning.StepS;
        T += dt;
        Steps++;
        // where everything stood before this step, for the view to draw between steps
        foreach (Shot sh in _shots) { sh.PX = sh.X; sh.PZ = sh.Z; }
        foreach (Ship s in _ships) if (s.Alive) SystemsStep(s, dt);
        foreach (Ship s in _ships)
        {
            if (!s.Alive || s.Struck || s == Player && !FlagUnmanned || Helmed.Contains(s.Id)) continue;
            s.ThinkT -= dt;
            if (s.ThinkT <= 0) { s.ThinkT = Tuning.AiThinkS; Think(s); }
        }
        // every ship a player commands steers to its helm's order and fires by its own auto-fire (Battle.Crew.cs)
        Ship admiral = Player;
        foreach (Ship p in Commanded().ToList())
        {
            Player = p;
            if (p.Alive && p.Engaged && p.Het == null && p.Broke <= 0 && NavTarget(p) is { } nt) Steer(p, p.Manoeuvre, nt, Rules.AiRangeKm());
            AutoFireStep();
        }
        Player = admiral;
        FieldPreMove(dt);
        foreach (Ship s in _ships) if (s.Alive && !s.Absent && !s.Fixed) Move(s, dt);
        ChargeStep(dt);
        // the mockup's other systems, each in its own file (Battle.<System>.cs; a hook with no body costs nothing)
        CommsStep(dt); TransportStep(dt); TractorStep(dt); MinesStep(dt); CraftStep(dt); ReactorStep(dt); WarpStep(dt);
        RepairCrewsStep(dt); CloakStep(dt); NebulaStep(dt); SensorsStep(dt); SecurityStep(dt); TargetingStep(dt); FleetStep(dt);
        RangeStep(dt);
        FieldStep(dt);
        if (Over != null) return;
        MarkersStep();
        PointDefenceStep();
        CountermeasuresStep(dt);
        ShotsStep(dt);
        // a prize strikes beaten to her capture line, or with her core gone critical: her crew gives her up rather than burn
        // with her (freighter-missions design, "Taken"), and the boarding party shuts the core down (ReactorStep)
        foreach (Ship s in _ships)
            if (s.Prize && s.Alive && !s.Struck && (s.Hull <= s.Class.HullHp * Tuning.CaptureHullFrac || s.Reactor.Melt > 0))
            {
                s.Struck = true;
                s.VOrder = 0;
                s.Engaged = false;
                Say("Security", $"The {s.Name} has struck. Boarding party away: she is ours.");
                // she is ours: every lock on her is dropped, so no one fires into the boarding party
                DropLocksOn(s.Id);
            }
        // a scene's own end, registered by the system whose objective it is (a ship disabled, not destroyed): checked first
        foreach (Func<bool> end in _ends) if (end()) return;
        // a test scene with a goal ends on it, whatever enemies are left
        bool goalEnds = false;
        ObjectivesStep(ref goalEnds);
        if (goalEnds) return;
        // a damage range never ends: its last ship can be broken up and the range repaired
        if (Scenario.DamageRange || Over != null) return;
        // a battle that cannot be withdrawn from, where no ship has been hurt for stalemate_s, ends as a stalemate (a draw)
        if (!Scenario.CanWithdraw && T - _lastHurtT > Tuning.StalemateS && _ships.Any(s => s.Side == 1 && s.Alive && !s.Struck))
        {
            Over = "stalemate";
            Say("Tactical", "Stalemate: neither side can bring the other to battle. Breaking off.");
            return;
        }
        if (Scenario.EndsOnObjectives) { if (!_ships.Any(s => s.Side == 0 && s.Alive)) Over = "lost"; return; }
        if (!_ships.Any(s => s.Side == 1 && s.Alive && !s.Struck)) { Over = "won"; Say("Tactical", "All hostile ships destroyed."); }
        else if (!_ships.Any(s => s.Side == 0 && s.Alive)) Over = "lost";
    }

    private void SystemsStep(Ship s, double dt)
    {
        ShipClass c = s.Class;
        if (s.CoreDown) { CoreDownStep(s, dt); return; }
        double spare = Rules.Output(s) - Rules.Used(s);
        if (spare >= 0) { s.Battery = Math.Min(c.BatteryE, s.Battery + spare * dt * Tuning.BatteryChargePerSpareES); s.Brown = 1; }
        else { s.Battery = Math.Max(0, s.Battery + spare * dt); s.Brown = s.Battery > 0 ? 1 : Rules.Output(s) / Rules.Used(s); }
        s.Capacitor = Math.Min(c.CapacitorE, s.Capacitor + s.Power["weapons"] * s.Brown * dt * s.Sys["weapons"]);
        SpecialsStep(s, dt);
        foreach (Mount m in s.Mounts)
        {
            if (m.State == Arming.Offline || m.Charge >= 1 || m.Hp <= 0 || m.StunS > 0) continue;
            WeaponMode w = Rules.Mode(m);
            // point defence and missiles charge from their own generators, whatever weapons control's state; every other
            // mount draws on the capacitor, then the battery, and stops once weapons control is disabled (weapon-targeting)
            if (m.Weapon.OwnPower) { m.Charge = Math.Min(1, m.Charge + dt / w.ChargeS); continue; }
            if (!Rules.ControlUp(s)) continue;
            double need = w.CostE * dt / w.ChargeS;
            if (s.Capacitor >= need) s.Capacitor -= need;
            else if (s.Battery >= need) s.Battery -= need;
            else continue;
            m.Charge = Math.Min(1, m.Charge + dt / w.ChargeS);
        }
        if (s.ShieldsHold)   // a cloak's shields neither hold nor regenerate (Battle.Cloak.cs), nor a tripped core's (Battle.Nebula.cs)
        {
            double r = s.Power["shields"] * s.Brown * Tuning.ShieldRegenPerES * dt * s.Sys["shields"];
            for (int i = 0; i < 6; i++)
            {
                double add = s.Reinforce >= 0 ? r * 0.5 / 6 + (i == s.Reinforce ? r * 0.5 : 0) : r / 6;
                s.Shields[i] = Math.Min(c.ShieldsHp[i], s.Shields[i] + add);
            }
        }
        SystemsTail(s, dt);
        // repair energy works through the crews on the repair queue (Battle.Repair, RepairCrewsStep)
    }

    // what runs down whether or not the ship has power: the shield flashes and the helm's timers
    private void SystemsTail(Ship s, double dt)
    {
        for (int i = 0; i < 6; i++) s.Flash[i] = Math.Max(0, s.Flash[i] - dt * 2);
        s.HetChance = Math.Min(Rules.HetMax(s), s.HetChance + Tuning.HetRecoverPerS * dt);
        s.Broke = Math.Max(0, s.Broke - dt);
        s.EstopT = Math.Max(0, s.EstopT - dt);
    }

    // A core tripped by lightning (Battle.Nebula.cs): no power at all, so nothing charges and nothing regenerates. The
    // brownout at 0 stops what scales by it (the drive, the warp, the tractor, the repair crews).
    private void CoreDownStep(Ship s, double dt)
    {
        s.Brown = 0;
        SpecialsStep(s, dt);
        SystemsTail(s, dt);
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
            double turn = c.TurnDegS * Geometry.Deg * dt * (0.5 + 0.5 * s.Sys["engines"]) * (s.Erratic ? Tuning.ErraticTurn : 1) * s.SurgeTurnX;
            double dh = Geometry.Wrap(s.HOrder - s.H);
            s.H = Geometry.Wrap(s.H + Math.Clamp(dh, -turn, turn));
        }
        if (s.Broke > 0) { s.V = 0; s.VOrder = 0; }
        if (s.Estop || s.EstopT > 0) s.VOrder = 0;
        double vmax = Rules.MaxSpeed(s), goal = Math.Clamp(s.VOrder, -Tuning.ReverseMaxMS, vmax), a = c.AccelMS2 * dt * (s.Estop ? Tuning.EstopDecelX : 1) * s.SurgeAccelX;
        s.V += Math.Clamp(goal - s.V, -a, a);
        if (s.Estop && Math.Abs(s.V) < 10) { s.V = 0; s.Estop = false; s.EstopT = Tuning.EstopHoldS; }
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
            case "approach": s.HOrder = toT; s.VOrder = d > rangeKm ? vmax : Math.Max(600, Math.Abs(t.V)); break;
            case "circle": s.HOrder = Geometry.Wrap(toT + Math.PI / 2 - Math.Clamp((d - rangeKm) / rangeKm, -1, 1) * Math.PI / 4); s.VOrder = vmax * 0.8; break;
            case "evade": s.HOrder = Geometry.Wrap(toT + Math.PI); s.VOrder = vmax; break;
            case "follow":
            {
                var f = Geometry.Forward(t.H);
                var r = Geometry.Right(t.H);
                double ax = t.X - f.X * 8000 + r.X * 6000, az = t.Z - f.Z * 8000 + r.Z * 6000, gap = Geometry.Dist(s.X, s.Z, ax, az);
                s.HOrder = gap > 2000 ? Geometry.HeadingTo(s.X, s.Z, ax, az) : t.H;
                s.VOrder = Math.Clamp(Math.Abs(t.V) + gap * 0.2, 0, vmax);
                break;
            }
            case "intercept":
            {
                double tti = Geometry.Dist(s.X, s.Z, t.X, t.Z) / Math.Max(1000, Math.Abs(s.V));
                var f = Geometry.Forward(t.H);
                double lead = Geometry.HeadingTo(s.X, s.Z, t.X + f.X * t.V * tti, t.Z + f.Z * t.V * tti);
                // the lead never puts the target more than intercept_max_lead_deg off the nose, so the forward arcs keep it
                // (Mac playtest 2026-11-08: leading a circling corsair 50 km out left it outside a Krai corvette's 70 degree
                // arcs, and neither ship fired)
                double off = Geometry.Wrap(lead - toT), max = Tuning.InterceptMaxLeadDeg * Geometry.Deg;
                s.HOrder = Geometry.Wrap(toT + Math.Clamp(off, -max, max));
                break;
            }
            case "trail":
            {
                var f = Geometry.Forward(t.H);
                double ax = t.X - f.X * 6000, az = t.Z - f.Z * 6000, gap = Geometry.Dist(s.X, s.Z, ax, az);
                s.HOrder = gap > 2000 ? Geometry.HeadingTo(s.X, s.Z, ax, az) : t.H;
                s.VOrder = Math.Clamp(Math.Abs(t.V) + (gap > 2000 ? gap * 0.2 : 0), 0, vmax);
                break;
            }
            case "hold_range":
                s.HOrder = d > s.HoldKm * 1.1 ? toT : d < s.HoldKm * 0.9 ? Geometry.Wrap(toT + Math.PI) : Geometry.Wrap(toT + Math.PI / 2);
                s.VOrder = d > s.HoldKm * 1.1 || d < s.HoldKm * 0.9 ? vmax : Math.Max(600, Math.Abs(t.V));
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
        if (s.Absent || s.Dormant) return;
        if (s.Runs) { s.VOrder = Rules.MaxSpeed(s); return; }
        bool handled = false;
        ThinkHook(s, ref handled);
        if (handled) return;
        // the enemies it can see (Battle.Cloak.cs); seeing none, it sweeps for one and heads where it last saw one
        Ship[] foes = SeenFoes(s).ToArray();
        if (foes.Length == 0) { NoFoeSeen(s); return; }
        Ship near = foes.OrderBy(o => Geometry.Dist(s.X, s.Z, o.X, o.Z)).ThenBy(o => o.Order).First();
        Ship t = AiTarget(s, foes, near);   // its target rule (Battle.Fleet.cs)
        s.Target = t.Id;
        double range = Rules.AiRangeKm(s.Range);
        // a captain whose fire has not landed for a while closes in, half its range for every ai_close_after_s without a
        // hit (to an eighth): no ship kites at a range where its shots are all decoyed, missed or out of reach
        range *= DMath.Pow(0.5, Math.Min(3, Math.Floor((T - s.LastLandedT) / Tuning.AiCloseAfterS)));
        if (Retreating(s)) Steer(s, "evade", near, range);
        else if (s.Stance == Stance.Hold) Steer(s, "hold", t, range);
        // a lane run (test scenes): straight on at full speed, turning back for another pass once lane_turn_km out
        else if (s.Stance == Stance.Run) { if (Rules.DistKm(s, t) > Data.Systems.LaneTurnKm) Steer(s, "approach", t, range); else s.VOrder = Rules.MaxSpeed(s); }
        else if (s.Stance == Stance.Escort && s.Side == 0 && Player.Alive && s != Player) Steer(s, Rules.DistKm(s, t) < range ? "circle" : "follow", Rules.DistKm(s, t) < range ? t : Player, range);
        else Steer(s, Rules.DistKm(s, t) > range * 1.3 ? "approach" : "circle", t, range);
        ThinkCharge(s, t);
        ThinkSystems(s, t);
        if (s.HoldFire) return;
        foreach (Mount m in s.Mounts)
            if (!m.Weapon.IsPointDefence && Rules.Ready(m) && Rules.InArc(s, m, t) && Rules.HitChance(s, m, t) >= Tuning.AiFireMinChance) FireMount(s, m, t);
    }

    /// <summary>World position of a mount, metres.</summary>
    public static (double X, double Z) MountWorld(Ship s, Mount m)
    {
        double half = s.Class.FrameM / 2;
        var f = Geometry.Forward(s.H);
        var r = Geometry.Right(s.H);
        return (s.X + f.X * m.Def.Y * half + r.X * m.Def.X * half * 0.6, s.Z + f.Z * m.Def.Y * half + r.Z * m.Def.X * half * 0.6);
    }

    /// <summary>Fires one mount at t if it is ready, in arc and in range, aimed at <paramref name="aim"/> (s's own aim when
    /// null). The roll uses Rules.HitChance; the shot carries its aim, how its weapon aims and its block's reach to Damage.</summary>
    private bool FireMount(Ship s, Mount m, Ship t, string? aim = null)
    {
        if (!Rules.Ready(m) || !Rules.InArc(s, m, t) || !Rules.InRange(s, m, t)) return false;
        if (s.HoldsFire || !CanLock(s, t)) return false;   // no fire while cloaking or with the core tripped, none at what cannot be locked (Battle.Cloak.cs)
        _firstFire ??= s.Side;
        string a = aim ?? s.Aim;
        WeaponMode w = Rules.Mode(m);
        bool hit = _hit.Next() < Rules.HitChance(s, m, t, a);
        var (x, z) = MountWorld(s, m);
        m.Charge = 0;
        int mi = s.Mounts.IndexOf(m);
        double reach = Rules.ReachDeg(m);
        if (!m.Weapon.IsPointDefence) VoidDecoy(s);   // firing main weapons voids its decoy (SFC2, printed page 136)
        Shot sh;
        if (m.Weapon.Kind == "chorus") sh = FireChorus(s, m, t, x, z, mi, hit);
        else if (!m.Weapon.Flies)
            sh = new Shot { Kind = "beam", X = x, Z = z, SX = x, SZ = z, TX = t.X, TZ = t.Z, Life = Tuning.BeamLifeS, Color = m.Weapon.Color, To = t.Id, From = s.Id, Mount = mi, Hit = hit };
        else if (m.Weapon.Kind == "missile")
            sh = LaunchMissile(s, m, t, x, z, mi, hit);
        else
            sh = new Shot { Kind = m.Weapon.Kind, X = x, Z = z, SX = x, SZ = z, TX = t.X, TZ = t.Z, To = t.Id, From = s.Id, Mount = mi, Hit = hit, Damage = w.Damage, SpeedMS = m.Weapon.SpeedMS, Life = 12, Color = w.Color ?? m.Weapon.Color,
                Pierce = m.Weapon.Kind == "slug" ? (m.State == Arming.Overload ? Data.Specials.Forge.OverloadPierceX : Data.Specials.Forge.PierceX) : 0 };
        sh.Aim = a; sh.AimBy = m.Weapon.Aim; sh.ReachDeg = reach;
        _shots.Add(sh);
        if (sh.Kind == "beam" && hit) Damage(t, s.X, s.Z, s, w.Damage, m.Weapon.Kind, 0, sh);
        return true;
    }

    /// <summary>Damage from a source at (sx, sz), the one rule every hit takes: the facing it strikes absorbs what it can,
    /// the rest hits the hull, and may hurt what the facing exposes (the chopping block, Strike): a precise weapon's hit
    /// (the shot <paramref name="by"/>, when it carries an aim) lands on the aimed subsystem or mount direct_chance of the
    /// time when the block holds it; any other hit spreads over the block's subsystems spread_chance of the time; and a
    /// hit may damage an exposed mount (weapon-targeting design; tactical.json strike).</summary>
    private void Damage(Ship t, double sx, double sz, Ship? src, double dmg, string kind, double pierce = 0, Shot? by = null)
    {
        if (!t.Alive) return;
        _lastHurtT = T;
        if (src != null) src.LastLandedT = T;
        int f = Rules.FacingFrom(t, sx, sz);
        // a surging Krai bow holds bow_shield_x as much; pierce of the hit goes straight through (Rules.Pierced)
        double bowX = f == 0 && t.SurgeS > 0 ? Data.Specials.Charge.BowShieldX : 1;
        double absorb = t.ShieldsHold ? Rules.Pierced(dmg, pierce, t.Shields[f] * bowX).Held : 0;
        t.Shields[f] -= absorb / bowX;
        t.Flash[f] = 1;
        double rest = dmg - absorb;
        if (rest <= 0) { Record(t, sx, sz, kind, absorb, 0); return; }
        t.Hull -= rest;
        StrikeDef st = Rules.Tactical.Strike;
        string aim = by?.Aim ?? "hull";
        Exposure ex = Strike.Of(t, f, st, by?.ReachDeg ?? 0);
        int mi = Strike.AimMount(aim);
        bool direct = by?.AimBy == "precise" && aim != "hull" && Strike.Exposed(ex, aim) && _dmg.Next() < st.DirectChance;
        string? k = direct ? (mi >= 0 ? null : aim) : ex.Sys.Count > 0 && _dmg.Next() < st.SpreadChance ? ex.Sys[_dmg.Below(ex.Sys.Count)] : null;
        // a direct hit aimed at a weapon lands on that mount
        if (direct && mi >= 0 && mi < t.Mounts.Count) HitMount(t.Mounts[mi], st);
        if (k != null) t.Sys[k] = Math.Max(0, t.Sys[k] - rest * (k == "reactor" ? st.ReactorPerDmg : st.SysPerDmg));
        if (ex.Mounts.Count > 0 && _dmg.Next() < st.MountChance + (direct && aim == "weapons" ? st.MountAimChance : 0))
            HitMount(t.Mounts[ex.Mounts[_dmg.Below(ex.Mounts.Count)]], st);
        // a hit rolls on the reactor; a burn's ticks are not hits (the mockup has no burn: the forge driver's is ours), so they
        // do not, or a burning hull would knock coils and loops out every half second
        if (kind != "burn") ReactorHit(t, rest);
        if (kind is "burn" or "chorus") { }   // a burn and a chorus land in ticks: their own lines say so
        else if (t == Player) Say("Engineering", $"Hull hit through the {Rules.Facings[f]} shield.");
        else if (src == Player) Say("Tactical", $"Hit on {t.Name}, {Rules.Facings[f]} facing.");
        Record(t, sx, sz, kind, absorb, rest);
        if (t.Hull <= 0)
        {
            t.Hull = 0; t.Alive = false; t.V = 0;
            _hits[^1] = _hits[^1] with { Destroyed = true };
            Say("Tactical", $"{t.Name} destroyed.");
            DropLocksOn(t.Id);
            if (t == Player) PromoteNextCommand();
        }
    }

    // a hit on a mount takes mount_hit_frac of its health; one destroyed loses its charge
    private static void HitMount(Mount m, StrikeDef st)
    {
        m.Hp = Math.Max(0, m.Hp - st.MountHitFrac);
        if (m.Hp <= 0) m.Charge = 0;
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
            if (sh.Kind == "chorus") { ChorusStep(sh, dt); continue; }
            if (sh.Kind is "bolt" or "slug") { BoltStep(sh, dt); continue; }
            if (sh.Kind == "missile") { MissileStep(sh, dt); continue; }
            Ship? t = ShipById(sh.To);
            if (t is not { Alive: true }) { sh.Life = Math.Min(sh.Life, 0.4); continue; }
            double d = Geometry.Dist(sh.X, sh.Z, t.X, t.Z), stepM = sh.SpeedMS * dt;
            if (d <= stepM + Tuning.ShotHitRadiusM)
            {
                Ship? from = ShipById(sh.From);
                if (sh.Hit && !sh.Spent) { if (sh.Kind == "quell") Quelled(t, from?.X ?? sh.X, from?.Z ?? sh.Z); else Damage(t, from?.X ?? sh.X, from?.Z ?? sh.Z, from, sh.Damage, sh.Kind, 0, sh); }
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
            if (sh.Hit && !sh.Spent)
            {
                Damage(t, from?.X ?? sx, from?.Z ?? sz, from, sh.Damage, sh.Kind, sh.Pierce, sh);
                if (sh.Kind == "slug") Burn(t, from?.X ?? sx, from?.Z ?? sz);
            }
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
            // the point defence station's mode: Off holds fire, Craft keeps it for craft, Missiles fires on missiles only
            if (!s.Alive || s.CoreDown || s.Pd is PdMode.Off or PdMode.Craft) continue;
            foreach (Mount m in s.Mounts)
            {
                if (!m.Weapon.IsPointDefence || !Rules.Ready(m)) continue;
                // a missile chasing a lure or lost is no threat; the rest are, nearest first in the stable shot order
                Shot? threat = _shots.FirstOrDefault(sh => sh.Kind != "beam" && sh.To == s.Id && !sh.Spent && sh.Lure == null && !sh.Lost
                    && (s.Pd != PdMode.Missiles || sh.Kind == "missile")
                    && Geometry.Dist(sh.X, sh.Z, s.X, s.Z) / 1000 <= m.Weapon.Normal.RangeKm);
                if (threat == null) continue;
                m.Charge = 0;
                var (px, pz) = MountWorld(s, m);
                added.Add(new Shot { Kind = "beam", X = px, Z = pz, SX = px, SZ = pz, TX = threat.X, TZ = threat.Z, Life = 0.15, Color = m.Weapon.Color, Intercept = true, From = s.Id, Mount = s.Mounts.IndexOf(m), At = threat });
                // a burst stops it pd_intercept_chance of the time; a blue or green missile needs two (its grade's pd_hits)
                if (_hit.Next() < Tuning.PdInterceptChance && --threat.PdHitsLeft <= 0) { threat.Spent = true; threat.Life = 0; s.Intercepts++; }
            }
        }
        _shots.AddRange(added);
    }
}
