// Rules.Reactor: what the reactor, the warp drive and the officers are worth, each in one place (CLAUDE.md 6.1), after
// the battle console mockup's efficiency, coolantDraw, cooling, reactorStability, nacelleDraw and officerSkill. The
// Power page's readouts, the bottom strip's ticks, the warp key and the steps that change the state all call these.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public sealed partial class Rules
{
    /// <summary>The cap PowerRoom gives while the allocation may draw past the output: no cap.</summary>
    public const int NoCap = 1000;

    /// <summary>The numbers of the mockup's other systems (data/battle/systems/), as the battle was built with them.</summary>
    public SystemsDef SystemsData { get; init; } = new();

    /// <summary>The coils' mean offset from alignment, 0 (aligned) to 1.</summary>
    public static double Misalignment(Ship s) => s.Reactor.Coils.Length == 0 ? 0 : s.Reactor.Coils.Sum(Math.Abs) / s.Reactor.Coils.Length;

    /// <summary>The share of its rating the reactor gives: misaligned coils lose efficiency_loss of their mean offset.</summary>
    public double Efficiency(Ship s) => Math.Clamp(1 - SystemsData.Reactor.EfficiencyLoss * Misalignment(s), 0, 1);

    /// <summary>What the coolant pumps draw, whole E.</summary>
    public int CoolantDraw(Ship s) => s.Reactor.Loops.Sum(l => l.Flow * SystemsData.Reactor.FlowE);

    /// <summary>What the reactor's own pumps and the warp coils draw on top of the systems' boxes, whole E.</summary>
    public int ReactorDraw(Ship s) => CoolantDraw(s) + s.Power.GetValueOrDefault("warp");

    /// <summary>The cooling the loops give: each loop's flow times its health.</summary>
    public static double Cooling(Ship s) => s.Reactor.Loops.Sum(l => l.Flow * l.Hp);

    /// <summary>True while the loops carry no heat away (coolant offline).</summary>
    public bool CoolantOffline(Ship s) => Cooling(s) <= SystemsData.Reactor.CoolingOfflineBelow;

    /// <summary>The reactor's magnetic confinement, 0 to 1: the reactor subsystem's health.</summary>
    public static double Confinement(Ship s) => s.Sys.GetValueOrDefault("reactor", 1);

    /// <summary>The reactor's stability, 0 to 1: confinement, coil alignment and the core's distance from its limit together.</summary>
    public double Stability(Ship s)
    {
        ReactorDef r = SystemsData.Reactor;
        double heat = Math.Clamp((r.CriticalPct - s.Reactor.Temp) / (r.CriticalPct - r.StablePct), 0, 1);
        return Math.Clamp(Confinement(s) * (1 - Misalignment(s)) * heat, 0, 1);
    }

    /// <summary>What each nacelle draws now, E: the warp energy (browned out with the ship) split between the nacelles still working.</summary>
    public static double[] NacelleDraw(Ship s)
    {
        double[] n = s.Warp.Nacelles;
        int live = n.Count(h => h > 0);
        return n.Select(h => h > 0 && live > 0 ? s.Power.GetValueOrDefault("warp") * s.Brown / live : 0).ToArray();
    }

    /// <summary>The skill the officer in a station's slot brings to it: their whole skill in their specialty, half (rounded
    /// down) in any other, 0 for a vacant slot or a ship without officers. Every officer's ability reads this.</summary>
    public static int OfficerSkill(Ship s, string station)
    {
        int crew = s.CrewSkill.GetValueOrDefault(station);   // a player crew's rank on that station (co-op)
        if (!s.Officers.TryGetValue(station, out StationOfficer? o) || o == null) return crew;
        return (o.Spec == station ? o.Skill : o.Skill / 2) + crew;
    }
}
