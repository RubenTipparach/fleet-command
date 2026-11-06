// Battle.Range: the damage range (owner, 2026-11-05: "make testing scenes for ship breaking tests from weapons fire like the
// artifacts", "and batle scars"), the ship mockups' Damage tab in the game. A shot here is fired by no ship: it starts
// RangeDef.FromKm out on a bearing from the target's bow and flies or lands as a real one does (a beam at once, a bolt along
// its line, a torpedo or missile on its own course), and its damage goes through Damage, the one rule every hit in a battle
// takes, so the shields, the hull, the view's scars and its breaks are exactly a battle's. A program (data/battle/range.json)
// fires a run of them on the battle's own fixed step; its random bearings come from the range's own seeded generator, so a
// run replays exactly. Only a scenario marked damage_range takes these orders.
using System;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    private Rng? _range;
    private RangeProgram? _prog;
    private string? _progShip;
    private int _progShot;
    private double _progWait;

    /// <summary>The program running, and the ship it is firing at; null when none is.</summary>
    public RangeProgram? RangeRunning => _prog;

    /// <summary>The range's targets stand to be hit: their point defence is switched off so every shot arrives, and their
    /// shields start down (the mockups' Damage tab starts that way; Shields raises them).</summary>
    private void RangeSetup()
    {
        if (!Scenario.DamageRange) return;
        _range = Rng.Fork(Scenario.Seed, "range");
        foreach (Ship s in _ships.Where(s => s.Side == 1))
        {
            foreach (Mount m in s.Mounts.Where(m => m.Weapon.IsPointDefence)) m.State = Arming.Offline;
            RangeShields(s.Id, false);
        }
    }

    /// <summary>Fires one range shot of a weapon at a ship, from bearingDeg off its bow (clockwise, 0 dead ahead). Does
    /// nothing outside a range scenario, at a dead ship, or with a point defence weapon.</summary>
    public void RangeFire(string shipId, double bearingDeg, string weaponId, bool overload = false)
    {
        if (!Scenario.DamageRange || ShipById(shipId) is not { Alive: true } t) return;
        if (!Data.Weapons.TryGetValue(weaponId, out WeaponDef? w) || w.IsPointDefence || !double.IsFinite(bearingDeg)) return;
        WeaponMode mode = overload && w.Overload != null ? w.Overload : w.Normal;
        var f = Geometry.Forward(t.H + bearingDeg * Geometry.Deg);
        double from = Data.Range.FromKm * 1000, sx = t.X + f.X * from, sz = t.Z + f.Z * from;
        if (!w.Flies)
        {
            _shots.Add(new Shot { Kind = "beam", X = sx, Z = sz, SX = sx, SZ = sz, TX = t.X, TZ = t.Z, Life = Tuning.BeamLifeS, Color = w.Color, To = t.Id, Hit = true });
            Damage(t, sx, sz, null, mode.Damage, w.Kind);
        }
        else
            _shots.Add(new Shot { Kind = w.Kind, X = sx, Z = sz, SX = sx, SZ = sz, TX = t.X, TZ = t.Z, To = t.Id, Hit = true, Damage = mode.Damage, SpeedMS = w.SpeedMS, Life = 12, Color = w.Color });
    }

    /// <summary>Starts a range program at a ship (replacing any running); an unknown program or a dead ship starts nothing.</summary>
    public void RangeRun(string shipId, string programId)
    {
        if (!Scenario.DamageRange || ShipById(shipId) is not { Alive: true } t) return;
        RangeProgram? p = Data.Range.Programs.FirstOrDefault(q => q.Id == programId);
        if (p == null) return;
        _prog = p; _progShip = shipId; _progShot = 0; _progWait = 0;
        Say("Range", $"{p.Name} on the {t.Name}.");
    }

    /// <summary>Stops the program running, if any.</summary>
    public void RangeStop() { _prog = null; _progShip = null; }

    /// <summary>Raises a range ship's shields at full strength, or drops them.</summary>
    public void RangeShields(string shipId, bool up)
    {
        if (!Scenario.DamageRange || ShipById(shipId) is not { Alive: true } t) return;
        t.ShieldsUp = up;
        for (int i = 0; i < 6; i++) t.Shields[i] = up ? t.Class.ShieldsHp[i] : 0;
    }

    // One step of the running program: the next shot when its time comes, until it has fired them all or its ship is done.
    private void RangeStep(double dt)
    {
        if (_prog == null || _range == null) return;
        Ship? t = ShipById(_progShip);
        if (t is not { Alive: true } || _progShot >= _prog.Count || (_prog.StopBelowHull > 0 && t.Hull < t.Class.HullHp * _prog.StopBelowHull))
        {
            RangeStop();
            return;
        }
        _progWait -= dt;
        if (_progWait > 0) return;
        _progWait += _prog.EveryS;
        string w = _prog.Weapons[_progShot % _prog.Weapons.Count];
        double bearing = _prog.BearingDeg ?? _range.Next() * 360 - 180;
        _progShot++;
        RangeFire(t.Id, bearing, w, _prog.Overload);
    }
}
