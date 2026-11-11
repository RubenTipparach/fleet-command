// HelmRange: the scale of the helm's range sliders (hold distance, and hit and run's nearest and farthest range), the one
// implementation of it (CLAUDE.md 6.1): the helm's orders snap to it and the console's sliders draw and read it. Owner,
// 2026-11-10: "the range sliders on the nav tab should be fixed. dedicate 1/2 of the slider to be from 5km - 15km, and the
// other half is increments of 5km for both sliders": the slider's first half runs from the least range to the split in
// fine steps, its second half from the split to the most in coarse steps. The numbers are the tuning's (hold_min_km,
// hold_split_km, hold_max_km, hold_fine_step_km, hold_step_km).
using System;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public static class HelmRange
{
    /// <summary>A range, km, snapped to its step (fine below the split, coarse above) and held inside the limits.</summary>
    public static double Snap(double km, Tuning t)
    {
        double step = km <= t.HoldSplitKm ? t.HoldFineStepKm : t.HoldStepKm, from = km <= t.HoldSplitKm ? t.HoldMinKm : t.HoldSplitKm;
        return Math.Clamp(from + Math.Round((km - from) / step) * step, t.HoldMinKm, t.HoldMaxKm);
    }

    /// <summary>The step at a range, km: the least gap between hit and run's two ranges there.</summary>
    public static double StepAt(double km, Tuning t) => km <= t.HoldSplitKm ? t.HoldFineStepKm : t.HoldStepKm;

    /// <summary>Where a range sits along the slider, 0 to 1: the first half the least range to the split, the second half the
    /// split to the most.</summary>
    public static double ToFrac(double km, Tuning t) =>
        km <= t.HoldSplitKm
            ? 0.5 * Math.Clamp((km - t.HoldMinKm) / (t.HoldSplitKm - t.HoldMinKm), 0, 1)
            : 0.5 + 0.5 * Math.Clamp((km - t.HoldSplitKm) / (t.HoldMaxKm - t.HoldSplitKm), 0, 1);

    /// <summary>The range at a point along the slider, 0 to 1, snapped (ToFrac's inverse).</summary>
    public static double FromFrac(double f, Tuning t)
    {
        f = Math.Clamp(double.IsFinite(f) ? f : 0, 0, 1);
        double km = f <= 0.5 ? t.HoldMinKm + f / 0.5 * (t.HoldSplitKm - t.HoldMinKm) : t.HoldSplitKm + (f - 0.5) / 0.5 * (t.HoldMaxKm - t.HoldSplitKm);
        return Snap(km, t);
    }
}
