// Battle.Mines: mines, laid astern or placed by transporter, and set off by an enemy coming near (the battle console
// mockup's MINE, minesStep and the mine orders, docs/mockups/battle-console.html; design 4a). Owner, 2026-11-01: "Mine
// laying should have a cool down system of 3 seconds"; owner: "the player can transport mines to different locations
// within a certain radius of the ship". A mine arms arm_s after it is laid and goes off on the first enemy inside
// trigger_m, for dmg_hp (a scene where mines are lethal: the ship's hull and more). The placement's reach and validity
// (CanPlaceMine) are the one rule the order and the view's preview both use (CLAUDE.md 6.1).
// The mockup spends a transporter pad on a placed mine; pads belong to the transporter (the comms package), so here a
// placed mine needs the transporter powered and working, and the pads are left to that package.
using System;
using System.Collections.Generic;
using FleetCommand.Data;

namespace FleetCommand.Sim;

/// <summary>A mine in space: where it is (m), whose side laid it, and the seconds until it is armed.</summary>
public sealed class Mine
{
    public double X, Z, ArmS;
    public required int Side { get; init; }
    public required string Owner { get; init; }
    public bool Gone;
}

public sealed partial class Battle
{
    private readonly List<Mine> _mines = new();
    /// <summary>Every mine laid and not yet gone off, in the order they were laid.</summary>
    public IReadOnlyList<Mine> Mines => _mines;

    /// <summary>True when the commanded ship can lay a mine now: one aboard, the cool-down over, not evasive.</summary>
    public bool MineReady(Ship s) => s.Alive && s.Mines > 0 && s.MineCool <= 0 && !s.Erratic;

    /// <summary>True when its transporter can place a mine: powered and not disabled.</summary>
    public bool CanBeamMine(Ship s) => s.Power.GetValueOrDefault("transport") > 0 && s.Sys["transport"] >= Tuning.StatusDisabledBelow;

    /// <summary>True when a point (m) is inside the transporter's reach for a mine.</summary>
    public bool InMineReach(Ship s, double x, double z) => Geometry.Dist(s.X, s.Z, x, z) / 1000 <= Data.Systems.Mines.TransporterReachKm;

    /// <summary>True when a living ship is inside a mine's range of a point (m): the placement preview turns red.</summary>
    public bool ShipInMineRange(double x, double z)
    {
        foreach (Ship o in _ships) if (o.Alive && Geometry.Dist(o.X, o.Z, x, z) <= Data.Systems.Mines.TriggerM) return true;
        return false;
    }

    /// <summary>Drops a mine astern of the commanded ship.</summary>
    public void DropMine()
    {
        Ship s = Player;
        if (s.Erratic) { Say("Helm", "Not during erratic manoeuvres."); return; }
        if (!MineReady(s)) return;
        var f = Geometry.Forward(s.H);
        double back = Data.Systems.Mines.DropAsternM;
        Lay(s, s.X - f.X * back, s.Z - f.Z * back);
        Say("Transport", "Mine away.");
    }

    /// <summary>Beams a mine to a point on the plane (m) inside the transporter's reach.</summary>
    public void PlaceMine(double x, double z)
    {
        Ship s = Player;
        if (!double.IsFinite(x) || !double.IsFinite(z)) return;
        if (s.Erratic) { Say("Helm", "Not during erratic manoeuvres."); return; }
        if (!MineReady(s) || !CanBeamMine(s)) return;
        double km = Geometry.Dist(s.X, s.Z, x, z) / 1000;
        if (!InMineReach(s, x, z)) { Say("Transport", $"Out of transporter range ({Data.Systems.Mines.TransporterReachKm:0.0} km)."); return; }
        Lay(s, x, z);
        Say("Transport", $"Mine beamed out to {km:0.0} km.");
    }

    private void Lay(Ship s, double x, double z)
    {
        s.Mines--;
        s.MineCool = Data.Systems.Mines.CooldownS;
        _mines.Add(new Mine { X = x, Z = z, Side = s.Side, Owner = s.Id, ArmS = Data.Systems.Mines.ArmS });
    }

    partial void MinesStep(double dt)
    {
        MineDef md = Data.Systems.Mines;
        foreach (Ship s in _ships) s.MineCool = Math.Max(0, s.MineCool - dt);
        foreach (Mine mn in _mines)
        {
            mn.ArmS -= dt;
            if (mn.ArmS > 0 || mn.Gone) continue;
            foreach (Ship s in _ships)
            {
                if (!s.Alive || s.Side == mn.Side || Geometry.Dist(s.X, s.Z, mn.X, mn.Z) >= md.TriggerM) continue;
                mn.Gone = true;
                Damage(s, mn.X, mn.Z, ShipById(mn.Owner), Lethal("mine") ? s.Hull + Data.Systems.LethalOverkillHp : md.DmgHp, "mine");
                Say("Tactical", $"Mine detonated on {s.Name}.");
                break;
            }
        }
        _mines.RemoveAll(m => m.Gone);
    }
}
