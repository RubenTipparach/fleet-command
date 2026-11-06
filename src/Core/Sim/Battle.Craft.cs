// Battle.Craft: the ship's craft (the battle console mockup's Craft page and its craftStep, shuttleStep and barrageStep,
// docs/mockups/battle-console.html). Owner: "Deploy fighters to take down another ships defenses, and fire a missile
// barage to take it down"; "a disguised shuttle leaves your ship and suicide bombs a station".
// - Fighters launch together from the bay, staggered, and fly a mission: Attack circles the ship's target and strafes it;
//   Defend circles their own ship and shoots at the shots closing on it; Defences strafes the target's point defence
//   first, then its guns. Recall brings them home to the bay.
// - A disguised shuttle flies at the target posing as a freighter, so shields and guns let it through; inside suspect_km
//   the target's crew may see through it; arriving, it detonates against the hull past the shields.
// - A missile barrage fires a salvo from the racks at the target, one missile every interval_s, each a missile of the
//   barrage's grade that point defence can stop (the shots of Battle.Missiles).
// The decoy shuttle is a countermeasure and lives with the missiles (Battle.Missiles); these are the ship's own craft.
// The rolls come from their own stream ("craft"), so a battle still replays exactly from its seed.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

/// <summary>A craft in space: a fighter or a disguised shuttle, where it is (m), its ship, and for a fighter its place on
/// its circle (radians), the seconds until its next pass, its mission and whether it is recalled; for a shuttle the ship
/// it flies at. Fate says how a craft gone left: "recovered", "caught" or "detonated".</summary>
public sealed class Craft
{
    public required string Kind { get; init; }   // "fighter" or "shuttle"
    public required string Home { get; init; }
    public required int Side { get; init; }
    public string? To;
    public double X, Z, A, Cool;
    public string Mission = "attack";
    public bool Recall, Gone;
    public string? Fate;
}

public sealed partial class Battle
{
    private readonly List<Craft> _craft = new();
    /// <summary>Every craft in space, in the order they left their ships.</summary>
    public IReadOnlyList<Craft> Craft => _craft;

    /// <summary>The fighters of a ship in space.</summary>
    public int FightersOut(Ship s) => _craft.Count(c => c.Home == s.Id && c.Kind == "fighter");

    /// <summary>Launches every fighter in the bay on the ship's mission.</summary>
    public void LaunchFighters()
    {
        Ship s = Player;
        if (s.Erratic) { Say("Helm", "Not during erratic manoeuvres."); return; }
        if (!s.Alive || s.Fighters <= 0) return;
        FighterDef fd = Data.Systems.Fighters;
        int n = s.Fighters;
        s.Fighters = 0;
        for (int i = 0; i < n; i++)
            _craft.Add(new Craft { Kind = "fighter", Home = s.Id, Side = s.Side, X = s.X, Z = s.Z, A = i * Math.PI / 2, Cool = fd.LaunchWaitS + i * fd.LaunchStaggerS, Mission = s.Mission });
        Say("Flight deck", $"{n} fighters launched.");
    }

    /// <summary>Recalls the ship's fighters to the bay.</summary>
    public void RecallFighters()
    {
        foreach (Craft c in _craft) if (c.Home == Player.Id && c.Kind == "fighter") c.Recall = true;
    }

    /// <summary>The fighters' mission, for those in the bay and those flying: attack, defend or defences.</summary>
    public void SetMission(string mission)
    {
        if (!ScenarioExtras.Missions.Contains(mission)) return;
        Player.Mission = mission;
        foreach (Craft c in _craft) if (c.Home == Player.Id && c.Kind == "fighter") c.Mission = mission;
    }

    /// <summary>Launches a disguised shuttle at the locked target.</summary>
    public void LaunchShuttle()
    {
        Ship s = Player;
        if (s.Erratic) { Say("Helm", "Not during erratic manoeuvres."); return; }
        Ship? t = TargetOf(s);
        if (t == null || s.Shuttles <= 0 || !s.Alive) return;
        s.Shuttles--;
        _craft.Add(new Craft { Kind = "shuttle", Home = s.Id, Side = s.Side, To = t.Id, X = s.X, Z = s.Z });
        Say("Flight deck", $"Disguised shuttle away toward {t.Name}, squawking a freighter's call sign.");
    }

    /// <summary>Fires a missile barrage at the locked target: a salvo, or what is left in the racks.</summary>
    public void FireBarrage()
    {
        Ship s = Player;
        if (s.Erratic) { Say("Helm", "Not during erratic manoeuvres."); return; }
        if (TargetOf(s) == null || s.Missiles <= 0 || s.Barrage > 0 || !s.Alive) return;
        s.Barrage = Math.Min(Data.Systems.Barrage.Salvo, s.Missiles);
        s.BarrageT = 0;
        Say("Tactical", $"Missile barrage: {s.Barrage} away.");
    }

    partial void CraftStep(double dt)
    {
        foreach (Ship s in _ships) if (s.Alive) BarrageStep(s, dt);
        foreach (Craft c in _craft)
        {
            if (c.Kind == "shuttle") ShuttleStep(c, dt);
            else FighterStep(c, dt);
        }
        _craft.RemoveAll(c => c.Gone);
    }

    // One fighter: it flies to its place on a circle round what it is after (its ship's target, or its own ship when
    // defending or recalled), strikes when it is near, and on Defend shoots at a shot closing on its ship.
    private void FighterStep(Craft c, double dt)
    {
        FighterDef fd = Data.Systems.Fighters;
        Ship? home = ShipById(c.Home);
        if (home == null) { c.Gone = true; c.Fate = "lost"; return; }
        Ship t = c.Recall || !home.Alive || c.Mission == "defend" ? home : TargetOf(home) ?? home;
        c.A += dt * fd.OrbitRadS;
        double r = c.Recall ? 0 : fd.OrbitM, gx = t.X + Math.Cos(c.A) * r, gz = t.Z + Math.Sin(c.A) * r, d = Geometry.Dist(c.X, c.Z, gx, gz), sp = fd.SpeedMS * dt;
        if (d > sp) { c.X += (gx - c.X) / d * sp; c.Z += (gz - c.Z) / d * sp; } else { c.X = gx; c.Z = gz; }
        if (c.Recall && d < fd.RecoverM) { c.Gone = true; c.Fate = "recovered"; home.Fighters++; return; }
        c.Cool -= dt;
        if (!c.Recall && t != home && t.Side != home.Side && c.Cool <= 0 && d < fd.StrikeM)
        {
            c.Cool = fd.StrikeEveryS;
            if (_craftRng.Next() < fd.StrikeHitX)
            {
                // the Defences mission strafes point defence first, then the guns, flying inside the shields
                if (c.Mission == "defences" && (t.Mounts.FirstOrDefault(q => q.Weapon.IsPointDefence && q.Hp > 0) ?? t.Mounts.FirstOrDefault(q => q.Hp > 0)) is { } m)
                {
                    m.Hp = Math.Max(0, m.Hp - fd.StrafeMountHp);
                    if (m.Hp <= 0) { m.Charge = 0; if (home == Player) Say("Flight deck", $"{t.Name}'s {m.Weapon.Name.ToLowerInvariant()} {m.Index + 1} knocked out."); }
                }
                Damage(t, c.X, c.Z, home, fd.StrikeDmgHp, "fighter");
            }
        }
        if (!c.Recall && c.Mission == "defend" && c.Cool <= 0)
        {
            Shot? th = _shots.FirstOrDefault(sh => sh.To == home.Id && sh.Kind != "beam" && !sh.Spent && Geometry.Dist(sh.X, sh.Z, c.X, c.Z) < fd.DefendReachM);
            if (th != null)
            {
                c.Cool = fd.StrikeEveryS;
                if (_craftRng.Next() < fd.DefendKillX) { th.Spent = true; th.Life = 0; }
            }
        }
    }

    // A disguised shuttle: it flies at the target; inside suspect_km the crew may see through it; arriving, it goes off
    // against the hull, past the shields (it was let in as civilian traffic), and leaves one subsystem the worse.
    private void ShuttleStep(Craft c, double dt)
    {
        ShuttleDef sd = Data.Systems.Shuttle;
        Ship? t = ShipById(c.To);
        if (t is not { Alive: true }) { c.Gone = true; c.Fate = "lost"; return; }
        double d = Geometry.Dist(c.X, c.Z, t.X, t.Z), sp = sd.SpeedMS * dt;
        if (d / 1000 <= sd.SuspectKm && _craftRng.Next() < sd.CatchPerS * dt)
        {
            c.Gone = true; c.Fate = "caught";
            Say("Tactical", $"{t.Name} saw through the shuttle and shot it down.");
            return;
        }
        if (d <= sp + HullRadiusM(t))
        {
            c.Gone = true; c.Fate = "detonated";
            double dmg = sd.DmgHp * Tuning.DemoToughX;
            string k = Rules.Subsystems[_craftRng.Below(Rules.Subsystems.Length)];
            t.Sys[k] *= sd.SysLeftX;
            Say("Tactical", $"Shuttle detonated against {t.Name}: {dmg:0} damage to the hull.");
            Damage(t, c.X, c.Z, ShipById(c.Home), Lethal("shuttle") ? t.Hull + Data.Systems.LethalOverkillHp : dmg, "shuttle", 1);
            return;
        }
        c.X += (t.X - c.X) / d * sp; c.Z += (t.Z - c.Z) / d * sp;
    }

    // A barrage under way: one missile from the racks every interval_s while the salvo lasts, from either side in turn.
    private void BarrageStep(Ship s, double dt)
    {
        if (s.Barrage <= 0) return;
        Ship? t = TargetOf(s);
        if (t == null || s.Missiles <= 0) { s.Barrage = 0; return; }
        if ((s.BarrageT -= dt) > 0) return;
        BarrageDef bd = Data.Systems.Barrage;
        s.BarrageT = bd.IntervalS; s.Barrage--; s.Missiles--;
        MissileGrade g = Data.Missiles.Grade(bd.Grade);
        int side = s.Barrage % 2 == 1 ? 1 : -1;
        var r = Geometry.Right(s.H);
        double x = s.X + r.X * side * bd.SideOffsetM, z = s.Z + r.Z * side * bd.SideOffsetM;
        _shots.Add(new Shot
        {
            Kind = "missile", X = x, Z = z, SX = x, SZ = z, TX = t.X, TZ = t.Z, To = t.Id, From = s.Id, Hit = _craftRng.Next() < bd.HitX,
            Damage = Lethal("barrage") ? t.Class.HullHp * Data.Systems.LethalHullShareX : g.Damage, SpeedMS = g.SpeedMS, Life = bd.LifeS, Color = g.Color, Grade = g, PdHitsLeft = g.PdHits,
        });
    }
}
