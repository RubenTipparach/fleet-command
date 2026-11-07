// Battle.Tractor: the tractor beam, a contest of rounds (openspec/changes/battle-console/design.md 4a; the battle console
// mockup's tractorStep, towStep, beamValue and the tractor orders, docs/mockups/battle-console.html), and the asteroids a
// held ship can be dragged into (its rocksStep, after the-federation docs/13 3.1-3.2). Owner, 2026-11-01: "Hold and break
// should be only two options", "Tractor beam does take 10 seconds to power up", "If enemy tractor beam module is down or
// is less than .5x of the breaking ships strength. The ship breaking the beam then becomes the holder".
// - Hold powers the beam up for powerup_s (paused while the tractor has no energy); powered, it takes the locked target
//   once inside reach_km at grip_start_x. Break lets go at any time, also while powering up.
// - Each round_s both picks are revealed: the beam value (BeamValue, the one rule for both sides and the panel) of the
//   holder against the held ship's, the grip moving step_per_e a point of difference. A grip at 0, or the holder's tractor
//   going down, frees the held ship, which takes hold of the holder (no power-up) when the holder's tractor is down or its
//   value was under reverse_ratio_x of the breaker's. A ship freed cannot be taken by the same holder for relock_s.
// - The tow: the held ship is tethered at the length the beam took hold at, drawn back by the grip every step, its own
//   speed damped; the holder reels the tether in or out, slower when the held ship outweighs it.
// - An AI crew picks its value once a round from its own stream ("tractor"), spare energy first, then from the weapons;
//   out of the contest the tractor's energy goes back to the weapons.
// This file also sets the whole package up from the scenario (TractorSetup): loadouts, a scene's unarmed ship, its rocks.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    private Rng _tractorRng = null!, _craftRng = null!;

    /// <summary>The scenario's asteroids.</summary>
    public IReadOnlyList<RockDef> Rocks => Scenario.Rocks;

    /// <summary>True when a scene makes this tool decisive ("mine", "rock", "shuttle", "barrage").</summary>
    public bool Lethal(string tool) => Scenario.Lethal.Contains(tool);

    // The package's set-up from the scenario: every ship's mines and craft (its class's loadout, or the scene's kit), a
    // test scene's own ship stripped of weapons, and where each ship stands for the first tow's speed.
    private void TractorSetup()
    {
        _tractorRng = Rng.Fork(Scenario.Seed, "tractor");
        _craftRng = Rng.Fork(Scenario.Seed, "craft");
        SystemsDef sd = Data.Systems;
        for (int i = 0; i < _ships.Count; i++)
        {
            Ship s = _ships[i];
            ShipStart d = Scenario.Ships[i];
            Loadout l = sd.LoadoutOf(s.Class.Id);
            s.Mines = d.Kit?.Mines ?? l.Mines;
            s.Fighters = d.Kit?.Fighters ?? l.Fighters;
            s.Shuttles = d.Kit?.Shuttles ?? l.Shuttles;
            s.Missiles = d.Kit?.Missiles ?? l.Missiles;
            s.Mission = d.Kit?.Mission ?? "attack";
            s.PrevX = s.X; s.PrevZ = s.Z;
        }
        if (!Scenario.Armed) Player.Mounts.Clear();
    }

    /// <summary>A ship's beam value for a round: the energy in its tractor times the tractor's health and the brownout
    /// share. The holder's holds, the held ship's resists; the panel's readout is this same number.</summary>
    public static double BeamValue(Ship s) => s.Power.GetValueOrDefault("tractor") * s.Sys["tractor"] * s.Brown;

    /// <summary>A ship's tractor is down with no energy in it, or disabled or destroyed.</summary>
    public bool TractorDown(Ship s) => s.Power.GetValueOrDefault("tractor") <= 0 || s.Sys["tractor"] < Tuning.StatusDisabledBelow;

    /// <summary>The ship whose beam holds this one, or null.</summary>
    public Ship? HeldBy(Ship s) => _ships.FirstOrDefault(o => o.Alive && o.Beam != null && o.Beam.On == s.Id);

    /// <summary>The beam a ship is in, holding or held, and the ship at its other end; null when it is in none.</summary>
    public (TractorBeam Beam, Ship Other, bool Holding)? Contest(Ship s)
    {
        if (s.Beam != null && ShipById(s.Beam.On) is { } h) return (s.Beam, h, true);
        if (HeldBy(s) is { Beam: { } b } by) return (b, by, false);
        return null;
    }

    /// <summary>Hold: the beam powers up, then takes the locked target once it is inside reach. Not while evasive, with no
    /// energy in the tractor, or held by another's beam.</summary>
    public void TractorHold()
    {
        Ship s = Player;
        if (!s.Alive) return;
        if (s.Erratic) { Say("Helm", "Not during erratic manoeuvres."); return; }
        if (s.Tractor != TractorMode.Off || s.Power["tractor"] <= 0 || HeldBy(s) != null) return;
        s.Tractor = TractorMode.Power;
        s.PowerT = Data.Systems.Tractor.PowerupS;
        Say("Tractor", "Beam powering up.");
    }

    /// <summary>Break: lets go of the beam held, or stops the power-up, at any time.</summary>
    public void TractorBreak()
    {
        Ship s = Player;
        if (s.Tractor == TractorMode.Off) return;
        if (s.Beam != null && ShipById(s.Beam.On) is { } h) Say("Tractor", $"Beam on {h.Name} released.");
        s.Tractor = TractorMode.Off; s.PowerT = 0; s.Beam = null;
    }

    /// <summary>Reels the held ship in (dir below 0) or lets it out, a step at a time, between the shortest tether and the
    /// tractor's reach.</summary>
    public void Reel(int dir)
    {
        TractorBeam? b = Player.Beam;
        if (b == null || dir == 0) return;
        TractorDef td = Data.Systems.Tractor;
        b.WantM = Math.Clamp(b.WantM + Math.Sign(dir) * td.ReelStepM, td.MinTetherM, td.ReachKm * 1000);
    }

    private void TakeHold(Ship s, Ship h, string why)
    {
        TractorDef td = Data.Systems.Tractor;
        double len = Geometry.Dist(s.X, s.Z, h.X, h.Z);
        s.Tractor = TractorMode.Hold; s.PowerT = 0;
        s.Beam = new TractorBeam { On = h.Id, Grip = td.GripStartX, RoundT = td.RoundS, LenM = len, WantM = len };
        Say("Tractor", $"{s.Name} {why} {h.Name}, grip {Math.Round(td.GripStartX * 100)} %.");
    }

    partial void TractorStep(double dt)
    {
        foreach (Ship s in _ships) if (s.Alive) BeamStep(s, dt);
        foreach (Ship s in _ships) if (s.Alive && s != Player) CrewPick(s);
        foreach (Ship s in _ships) if (s.Alive && s.Beam != null) TowStep(s, dt);
        RocksStep(dt);
    }

    // One ship's beam, every step: the power-up, taking hold, losing it, and each round's result.
    private void BeamStep(Ship s, double dt)
    {
        TractorDef td = Data.Systems.Tractor;
        if (s.RelockOn != null && (s.RelockT -= dt) <= 0) s.RelockOn = null;
        if (s.Tractor == TractorMode.Power)
        {
            if (s.Power["tractor"] > 0) s.PowerT = Math.Max(0, s.PowerT - dt);
            if (s.PowerT <= 0) s.Tractor = TractorMode.Hold;
            return;
        }
        if (s.Beam == null)
        {
            Ship? t = TargetOf(s);
            if (s.Tractor == TractorMode.Hold && t != null && !TractorDown(s) && HeldBy(s) == null && Rules.DistKm(s, t) <= td.ReachKm && s.RelockOn != t.Id)
                TakeHold(s, t, "takes hold of");
            return;
        }
        TractorBeam b = s.Beam;
        Ship? h = ShipById(b.On);
        // the holder's tractor going down frees the held ship, which takes hold of the holder if its own tractor can
        if (h is { Alive: true } && s.Tractor == TractorMode.Hold && TractorDown(s))
        {
            Free(s, h);
            if (!TractorDown(h) && Rules.DistKm(s, h) <= td.ReachKm) TakeHold(h, s, "breaks free and takes hold of"); else Say("Tractor", $"{h.Name} broke free.");
            return;
        }
        if (s.Tractor != TractorMode.Hold || h is not { Alive: true } || s.Erratic || Rules.DistKm(s, h) > td.ReachKm + td.ReachSlackKm)
        {
            s.Beam = null; s.Tractor = TractorMode.Off;
            Say("Tractor", $"{s.Name}: beam lost.");
            return;
        }
        b.RoundT -= dt;
        if (b.RoundT > 0) return;
        double mine = BeamValue(s), theirs = BeamValue(h), moved = (mine - theirs) * td.StepPerE;
        b.Grip = Math.Clamp(b.Grip + moved, 0, 1);
        b.Last = new TractorRound(mine, theirs, moved);
        b.RoundT += td.RoundS;
        b.Round++;
        if (b.Grip <= 0)
        {
            Free(s, h);
            // the breaker turns the beam when the holder's tractor is down or held with under reverse_ratio_x of its value
            if (!TractorDown(h) && (TractorDown(s) || mine < td.ReverseRatioX * theirs)) TakeHold(h, s, "breaks free and takes hold of");
            else Say("Tractor", $"{h.Name} broke free.");
            return;
        }
        Say("Tractor", $"Round: {mine:0.0} E against {theirs:0.0} E, grip {Math.Round(b.Grip * 100)} %.");
    }

    // the holder lets go and cannot take the same ship again for relock_s
    private void Free(Ship s, Ship h)
    {
        s.Beam = null; s.Tractor = TractorMode.Off;
        s.RelockOn = h.Id; s.RelockT = Data.Systems.Tractor.RelockS;
    }

    // An AI crew in a contest picks its beam value once a round (unseen by the other ship until the round ends): spare
    // energy first, then energy from the weapons. Out of the contest its tractor's energy goes back to the weapons.
    private void CrewPick(Ship s)
    {
        TractorDef td = Data.Systems.Tractor;
        TractorBeam? contest = Contest(s)?.Beam;
        if (contest != null && s.PickRound != contest.Round)
        {
            s.PickRound = contest.Round;
            ReturnToWeapons(s);
            int want = td.AiPickMinE + _tractorRng.Below(td.AiPickMaxE - td.AiPickMinE + 1);
            int spare = Math.Max(0, Math.Min(want, Rules.PowerRoom(s, 0))), take = Math.Min(want - spare, s.Power["weapons"]);
            s.Power["weapons"] -= take;
            s.Power["tractor"] = Math.Min(Rules.BoxesPerSystem, spare + take);
        }
        else if (contest == null && s.PickRound != 0) { s.PickRound = 0; ReturnToWeapons(s); }
    }

    private static void ReturnToWeapons(Ship s)
    {
        s.Power["weapons"] = Math.Min(Rules.BoxesPerSystem, s.Power["weapons"] + s.Power["tractor"]);
        s.Power["tractor"] = 0;
    }

    // The tow (after the-federation docs/13 6.3, "range control belongs to the holder"): the held ship is drawn back to the
    // tether by the grip every step, so the holder drags it along and can swing it into rocks; its own speed is damped.
    private void TowStep(Ship s, double dt)
    {
        TractorBeam b = s.Beam!;
        Ship? h = ShipById(b.On);
        if (h is not { Alive: true }) return;
        TractorDef td = Data.Systems.Tractor;
        double rate = td.ReelMS * b.Grip * Math.Min(1, s.Class.HullHp / h.Class.HullHp) * dt;
        b.LenM += Math.Clamp(b.WantM - b.LenM, -rate, rate);
        double d = Geometry.Dist(s.X, s.Z, h.X, h.Z);
        h.V *= 1 - td.HoldDampPerS * b.Grip * dt;
        if (d > b.LenM && d > 0)
        {
            double k = (d - b.LenM) * b.Grip, ux = (h.X - s.X) / d, uz = (h.Z - s.Z) / d;
            h.X -= ux * k; h.Z -= uz * k;
        }
    }

    /// <summary>A ship's collision radius against rocks, metres.</summary>
    public double HullRadiusM(Ship s) => s.Class.FrameM * Data.Systems.Rocks.HullRadiusX;

    // Asteroids, every step: the micrometeor cloud round each rock grinds a ship flying through it (gathered and dealt in
    // whole points so the log does not chatter); a ship inside a rock is set back on its surface and, once its cool-down
    // has run out, takes the hit, scaled by how fast it was moving (its own speed or the tow), and loses speed. A scene
    // where rocks are lethal destroys an enemy that hits one.
    private void RocksStep(double dt)
    {
        RockRules rr = Data.Systems.Rocks;
        foreach (Ship s in _ships)
        {
            if (!s.Alive) { s.PrevX = s.X; s.PrevZ = s.Z; continue; }
            s.BumpT = Math.Max(0, s.BumpT - dt);
            double grind = 0;
            bool inCloud = false;
            foreach (RockDef r in Rocks)
            {
                double d = Geometry.Dist(s.X, s.Z, r.XM, r.ZM), halo = r.RadiusM * rr.HaloX;
                if (d >= halo) continue;
                inCloud = true;
                double f = Math.Clamp((halo - d) / (halo - r.RadiusM), 0, 1), spd = Math.Max(rr.GrindFloorX, Math.Abs(s.V) / rr.SpeedRefMS);
                grind = Math.Max(grind, rr.GrindDps * f * spd * dt);
            }
            if (inCloud && !s.InCloud && s == Player) Say("Helm", "Into a micrometeor cloud: the shields are taking grit.");
            s.InCloud = inCloud;
            s.Grit += grind;
            if (s.Grit >= rr.GritWholeHp)
            {
                double whole = Math.Floor(s.Grit);
                s.Grit -= whole;
                var fw = Geometry.Forward(s.H);
                Damage(s, s.X + fw.X * rr.GrindAheadM, s.Z + fw.Z * rr.GrindAheadM, null, whole, "grit");
                if (!s.Alive) continue;
            }
            foreach (RockDef r in Rocks)
            {
                double dx = s.X - r.XM, dz = s.Z - r.ZM, d = Math.Max(1e-6, Math.Sqrt(dx * dx + dz * dz)), reach = r.RadiusM + HullRadiusM(s);
                if (d >= reach) continue;
                double spd = Geometry.Dist(s.X, s.Z, s.PrevX, s.PrevZ) / dt;
                s.X = r.XM + dx / d * reach; s.Z = r.ZM + dz / d * reach;
                if (s.BumpT > 0) continue;
                s.BumpT = rr.CooldownS;
                s.V *= rr.SpeedKeepX;
                double dmg = Math.Round(rr.DmgHp * Math.Max(rr.SpeedFloorX, spd / rr.SpeedRefMS));
                Say("Tactical", $"{s.Name} hits {r.Name ?? "an asteroid"} at {Math.Round(spd)} m/s: {dmg} damage.");
                Damage(s, r.XM, r.ZM, null, Lethal("rock") && s != Player ? s.Hull + Data.Systems.LethalOverkillHp : dmg, "rock");
                if (!s.Alive) break;
            }
            s.PrevX = s.X; s.PrevZ = s.Z;
        }
    }
}
