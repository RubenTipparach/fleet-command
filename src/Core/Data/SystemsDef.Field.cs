// SystemsDef.Field: field operations' numbers (data/battle/systems/field.json, openspec/changes/mission-systems): how far a
// ship's sensors identify a contact, how a planet is scanned, the away team sent to a planet's installation, and where a
// late arrival waits. Its own part of SystemsDef, in its own file, so the packages never edit the same lines (CLAUDE.md 6.4).
using System;

namespace FleetCommand.Data;

/// <summary>The sensors' reach: base_km plus per_e_km per energy box, times health, times passive_x on passive sensors.</summary>
public sealed record SensorReachDef(double BaseKm, double PerEKm, double PassiveX);

/// <summary>An away team: at most TeamMarines marines, AwayS seconds on the surface, then one roll per marine against the
/// garrison (each wins at WinP); a lost roll costs the marine at LossP.</summary>
public sealed record AwayDef(int TeamMarines, double AwayS, double WinP, double LossP);

/// <summary>Everything field operations run on.</summary>
public sealed record FieldDef(SensorReachDef Sensors, double PlanetScanX, double PlanetScanKm, AwayDef Away, double StageKm);

public sealed partial class SystemsDef
{
    /// <summary>Field operations (data/battle/systems/field.json); these defaults until loaded.</summary>
    public FieldDef Field { get; private set; } = new(new SensorReachDef(70, 14, 0.6), 0.6, 60, new AwayDef(4, 18, 0.55, 0.35), 2500);

    partial void LoadField(StrictJson o)
    {
        o.Str("_doc", "");
        StrictJson s = o.Obj("sensors");
        var sensors = new SensorReachDef(Pos(s, "base_km", 70), Non(s, "per_e_km", 14), Frac01(s, "passive_x", 0.6));
        s.Done();
        StrictJson a = o.Obj("away");
        int team = (int)Pos(a, "team_marines", 4);
        var away = new AwayDef(team, Pos(a, "away_s", 18), Frac01(a, "win_p", 0.55), Frac01(a, "loss_p", 0.35));
        a.Done();
        Field = new FieldDef(sensors, Pos(o, "planet_scan_x", 0.6), Pos(o, "planet_scan_km", 60), away, Pos(o, "stage_km", 2500));
        o.Done();
    }

    private static double Frac01(StrictJson o, string k, double d)
    {
        double v = o.Num(k, d);
        if (v is < 0 or > 1) throw new DataError($"{o.Where}.{k}: between 0 and 1");
        return v;
    }
}
