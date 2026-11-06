// Strike: where a hit can land (openspec/changes/weapon-targeting, the battle console mockup's exposure, aimMount,
// aimExposed, aimHealth and aimName). A hit through a facing reaches the subsystems and mounts on that side of the hull,
// and the internals from any side: the chopping block. The rule is here once, read by Battle.Damage (every hit in a
// battle), the Target box's brackets, the Aim at keys and the hit details (CLAUDE.md 6.1). An aim is "hull", a subsystem
// (Rules.Subsystems), or one of the target's weapons as "w:<mount index>".
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

/// <summary>A chopping block: the subsystems (in Rules.Subsystems order) and the mount indices a facing exposes.</summary>
public sealed record Exposure(IReadOnlyList<string> Sys, IReadOnlyList<int> Mounts);

public static class Strike
{
    /// <summary>What a captain can aim at, in the Aim at keys' order: id, name, icon.</summary>
    public static readonly (string Id, string Name, string Icon)[] Aims =
    {
        ("hull", "Hull", "ship"), ("weapons", "Weapons control", "weapons"), ("engines", "Engines", "engines"), ("sensors", "Sensors", "sensors"),
        ("shields", "Shield generators", "shields"), ("reactor", "Reactor", "reactor"), ("tractor", "Tractor beam", "tractor"), ("transport", "Transporters", "transport"),
    };

    /// <summary>The chopping block of facing <paramref name="facing"/> of t: whatever lies within reachDeg of the facing's
    /// centre (the strike's own reach when reachDeg is 0), and anything within the internal radius of the middle.</summary>
    public static Exposure Of(Ship t, int facing, StrikeDef d, double reachDeg = 0)
    {
        double reach = reachDeg > 0 ? reachDeg : d.ReachDeg, fa = facing * 60;
        bool Near(double x, double y)
        {
            if (Math.Sqrt(x * x + y * y) < d.InternalHalfLen) return true;
            double b = Math.Atan2(x, y) / Geometry.Deg;
            return Math.Abs(((((b - fa) % 360) + 540) % 360) - 180) <= reach;
        }
        var sys = Rules.Subsystems.Where(k => Near(d.SubsystemsHalfLen[k].X, d.SubsystemsHalfLen[k].Y)).ToArray();
        var mounts = t.Mounts.Where(m => Near(m.Def.X, m.Def.Y)).Select(m => m.Index).ToArray();
        return new Exposure(sys, mounts);
    }

    /// <summary>The mount index an aim names ("w:2" is 2), or -1 for the hull or a subsystem.</summary>
    public static int AimMount(string? aim) =>
        aim != null && aim.StartsWith("w:", StringComparison.Ordinal) && int.TryParse(aim.AsSpan(2), out int i) && i >= 0 ? i : -1;

    /// <summary>True when an aim is a real one for t: the hull, a subsystem, or one of its mounts.</summary>
    public static bool Valid(string aim, Ship? t) =>
        aim == "hull" || Array.IndexOf(Rules.Subsystems, aim) >= 0 || (AimMount(aim) is int i && i >= 0 && (t == null || i < t.Mounts.Count));

    /// <summary>Whether a block holds an aim: the hull always, a subsystem or a mount when exposed.</summary>
    public static bool Exposed(Exposure ex, string aim)
    {
        int i = AimMount(aim);
        return aim == "hull" || (i >= 0 ? ex.Mounts.Contains(i) : ex.Sys.Contains(aim));
    }

    /// <summary>What an aim has left on t, 0 to 1: the hull's share, a subsystem's health or a mount's.</summary>
    public static double Health(Ship t, string aim)
    {
        int i = AimMount(aim);
        if (i >= 0) return i < t.Mounts.Count ? t.Mounts[i].Hp : 0;
        return aim == "hull" ? t.Hull / t.Class.HullHp : t.Sys.GetValueOrDefault(aim);
    }

    /// <summary>An aim's name as the console says it: a subsystem's, or a weapon's id and name on t.</summary>
    public static string Name(string aim, Ship? t)
    {
        int i = AimMount(aim);
        if (i >= 0) return t != null && i < t.Mounts.Count ? $"{t.Mounts[i].Label} {t.Mounts[i].Weapon.Name}" : $"Weapon W{i + 1}";
        foreach (var a in Aims) if (a.Id == aim) return a.Name;
        return aim;
    }
}
