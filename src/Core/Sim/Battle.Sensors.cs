// Battle.Sensors: the sensors' share of the Science station, after the battle console mockup's systemsStep scan, A.ecm,
// A.scan and A.sensorMode: ECM and ECCM as points of the sensors' energy (together never more than its boxes, each at
// most the station's cells), active or passive sensors, and the scan of the locked target, which runs on the sensor
// points left over, the sensors' health and the science officer. What ECM and ECCM are worth in a shot is Rules.Ew; what
// they do to a lock in a nebula is Battle.CanLock; what ECCM does to a sweep is Rules.SweepKm.
using System;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    // The scan of each ship's target, every step: a scan of another ship than the target is dropped.
    partial void SensorsStep(double dt)
    {
        var sr = Cs.Sensors;
        foreach (Ship s in _ships)
        {
            if (!s.Alive || s.ScanId == null) continue;
            if (s.ScanId != s.Target) { s.ScanId = null; s.ScanP = 0; continue; }
            int free = Math.Max(0, s.Power["sensors"] - s.Ecm - s.Eccm);
            double rate = (sr.ScanBasePerS + sr.ScanPerES * free) * (s.Passive ? sr.PassiveScanX : 1) * s.Sys["sensors"] * (1 + OfficerSkill(s, "science") * sr.OfficerScan);
            s.ScanP = Math.Min(1, s.ScanP + dt * rate);
        }
    }

    /// <summary>Scan target: starts scanning the locked target (a scan of it already under way carries on).</summary>
    public void Scan()
    {
        Ship s = Player;
        if (TargetOf(s) is not { } t) return;
        s.ScanP = s.ScanId == t.Id ? s.ScanP : 0;
        s.ScanId = t.Id;
    }

    /// <summary>True once the commanded ship's scan of t is complete.</summary>
    public bool Scanned(Ship t) => Player.ScanId == t.Id && Player.ScanP >= 1;

    /// <summary>Sets the commanded ship's ECM (eccm false) or ECCM (true) to n points, within the cells and what the
    /// sensors' energy leaves after the other.</summary>
    public void SetEw(bool eccm, int n)
    {
        Ship s = Player;
        int other = eccm ? s.Ecm : s.Eccm, v = Math.Clamp(n, 0, Math.Max(0, Math.Min(Cs.Sensors.EwCells, s.Power["sensors"] - other)));
        if (eccm) s.Eccm = v; else s.Ecm = v;
    }

    /// <summary>Active (false) or passive (true) sensors for the commanded ship.</summary>
    public void SetPassive(bool passive) => Player.Passive = passive;

    // ECM and ECCM fit inside the sensors' energy: when it is cut, ECM keeps what it can, then ECCM.
    private void ClampEw(Ship s)
    {
        s.Ecm = Math.Clamp(s.Ecm, 0, Math.Max(0, s.Power["sensors"]));
        s.Eccm = Math.Clamp(s.Eccm, 0, Math.Max(0, s.Power["sensors"] - s.Ecm));
    }
}
