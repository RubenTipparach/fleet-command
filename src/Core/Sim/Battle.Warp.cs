// Battle.Warp: the warp drive every step, the jump risk and the jump (openspec/changes/warp; the battle console mockup's
// warpStep, warpRisk, warpReady, jump and the warp key). An armed drive takes the reactor's free energy, up to coil_e,
// and gives way first when the systems need more than the output; the nacelles still working split it and charge the
// banks by their health; the course plot builds whatever the ship does (slowed by damaged sensors, stopped when they are
// out); damaged coolant loops leak the reserve. The jump risk is one rule that the Warp page, the warp key's hover card
// and the jump's roll all read (CLAUDE.md 6.1). A jump streaks the ship away; the player's jump ends the battle as an
// escape (Over is "jumped").
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

/// <summary>One factor of the jump risk: its key, its name, its state (0 to 1, 1 good) and the risk it adds.</summary>
public sealed record RiskFactor(string Key, string Label, double State, double Risk);

/// <summary>The jump risk: each factor, the total (1 less the product of 1 less each), the quantum-gravitational
/// instability where the ship is, and why a jump is refused now ("" when it is not).</summary>
public sealed record WarpRisk(IReadOnlyList<RiskFactor> Factors, double Total, double Q, string Why);

public sealed partial class Battle
{
    /// <summary>Quantum-gravitational instability at a point, 0 to 1: the strongest of the scenario's wells there.</summary>
    public double QgAt(double x, double z) => Scenario.Wells is { } ws ? ws.Select(w => w.At(x, z)).DefaultIfEmpty(0).Max() : 0;

    /// <summary>The jump risk and each factor in it, as the ship stands now.</summary>
    public WarpRisk WarpRiskOf(Ship s)
    {
        WarpDef W = Data.Systems.Warp;
        WarpRiskDef R = W.Risk;
        WarpState w = s.Warp;
        double avg = w.Nacelles.Length == 0 ? 0 : w.Nacelles.Average(h => Math.Max(0, h)), q = QgAt(s.X, s.Z), stab = Rules.Stability(s), cool = s.Reactor.Coolant;
        var f = new List<RiskFactor>
        {
            new("nacelles", "Nacelles", avg, w.Nacelles.Length == 0 || w.Nacelles.Any(h => h <= 0) ? R.NacelleOut : (1 - avg) * R.Nacelles),
            new("coolant", "Coolant", cool, Math.Max(0, R.CoolantOk - cool) * R.Coolant),
            new("banks", "Banks", w.Banks / W.BankE, (1 - w.Banks / W.BankE) * R.Banks),
            new("nav", "Navigation", w.Plot, (1 - w.Plot) * R.Nav),
            new("reactor", "Reactor", stab, (1 - stab) * R.Reactor),
            new("qg", "Q-G field", 1 - q, q * (q > W.QgBlock ? R.QgOverride : R.Qg)),
        };
        double total = 1 - f.Aggregate(1.0, (a, x) => a * (1 - Math.Clamp(x.Risk, 0, 1)));
        string why = w.Out > 0 ? "Jumping"
            : w.Banks < W.JumpE ? $"not enough energy stored: {Math.Floor(w.Banks)} of {W.JumpE:0} E"
            : q > W.QgBlock && !w.Override ? "gravity field too strong: turn on the safety override (Power, Warp)" : "";
        return new WarpRisk(f, total, q, why);
    }

    /// <summary>Whether the warp key will jump: armed, the banks at jump_e, the course plotted, nothing refusing the jump.</summary>
    public bool WarpReady(Ship s) => s.Warp.Armed && s.Warp.Banks >= Data.Systems.Warp.JumpE && s.Warp.Plot >= 1 && WarpRiskOf(s).Why == "";

    partial void WarpStep(double dt)
    {
        WarpDef W = Data.Systems.Warp;
        foreach (Ship s in _ships)
        {
            if (!s.Alive) continue;
            WarpState w = s.Warp;
            if (w.Out > 0)
            {
                // a jump under way: the ship streaks away along its heading, then is gone
                var fw = Geometry.Forward(s.H);
                s.X += fw.X * W.OutSpeedMS * dt;
                s.Z += fw.Z * W.OutSpeedMS * dt;
                w.Out -= dt;
                if (w.Out <= 0)
                {
                    w.Out = 0;
                    s.Alive = false; s.Jumped = true; s.V = 0;
                    Say("Helm", $"{s.Name} jumped to warp.");
                    foreach (Ship o in _ships) if (o.Target == s.Id) o.Target = null;
                    if (s == Player && Over == null) Over = "jumped";
                }
                continue;
            }
            // an armed drive takes the free energy, whole points, up to what the coils want; past the output it gives way first
            if (w.Armed)
            {
                double free = Rules.Output(s) - Rules.Used(s);
                int cur = s.Power["warp"];
                if (free >= 1 && cur < w.Want) s.Power["warp"] = Math.Min(w.Want, cur + (int)Math.Floor(free));
                else if (free < 0 && cur > 0) s.Power["warp"] = Math.Max(0, cur - (int)Math.Ceiling(-free));
            }
            double[] draw = Rules.NacelleDraw(s);
            double charge = 0;
            for (int i = 0; i < draw.Length; i++) charge += draw[i] * w.Nacelles[i];
            w.Banks = Math.Min(W.BankE, w.Banks + charge * W.ChargeK * dt);
            s.Reactor.Coolant = Math.Max(0, s.Reactor.Coolant - s.Reactor.Loops.Sum(l => 1 - l.Hp) * W.LeakPerS * dt);
            // slowed by damaged sensors, stopped only when they are knocked out
            if (w.Plotting) w.Plot = Math.Min(1, w.Plot + dt / W.PlotS * Math.Clamp(s.Sys["sensors"], 0, 1));
        }
        // a withdrawal under way: re-plot after a failed jump, and jump the moment the drive is ready
        if (Withdrawing && Over == null && Player.Alive && Player.Warp.Out <= 0)
        {
            if (!Player.Warp.Plotting) PlotCourse();
            if (WarpReady(Player)) Jump(Player);
        }
    }

    // The jump: refused with the reason the Warp page shows; otherwise one roll against the risk. A failure is a mishap
    // scaled by how bad the odds were; a very bad one breaches the core.
    private void Jump(Ship s)
    {
        WarpDef W = Data.Systems.Warp;
        WarpMishapDef M = W.Mishap;
        WarpRisk r = WarpRiskOf(s);
        WarpState w = s.Warp;
        bool me = s == Player;
        if (r.Why != "") { if (me) { Say("Helm", $"Cannot jump: {r.Why}."); Notify("Jump refused", r.Why, "caution"); } return; }
        w.Banks -= W.JumpE;
        s.Reactor.Coolant = Math.Max(0, s.Reactor.Coolant - W.CoolantJump);
        if (_warpRng.Next() >= r.Total)
        {
            w.Out = W.OutS;
            Say("Helm", $"Warp field up. Jumping (risk {Pct(r.Total)}).");
            if (me) Notify("Jumping", $"Risk was {Pct(r.Total)}", "accord");
            return;
        }
        double sev = _warpRng.Next();
        for (int i = 0; i < w.Nacelles.Length; i++) w.Nacelles[i] = Math.Max(0, w.Nacelles[i] - (M.Nacelle + M.NacelleSev * sev) * r.Total * M.NacelleX);
        s.Reactor.Coolant = Math.Max(0, s.Reactor.Coolant - M.CoolantSev * sev);
        w.Banks = 0; w.Plot = 0; w.Plotting = false;
        Damage(s, s.X, s.Z - 1, null, Math.Floor(M.HullDmg + M.HullDmgSev * r.Total * sev + 0.5), "warp");
        // the factor that weighed most, so the captain knows what to fix
        RiskFactor worst = r.Factors.OrderByDescending(x => x.Risk).First();
        string what = $"mostly {worst.Label.ToLowerInvariant()} at {Pct(worst.Risk)}";
        Say("Engineering", $"Warp field collapsed (risk was {Pct(r.Total)}, {what}): nacelles damaged, banks drained, course lost.");
        if (me) Notify("Jump failed", $"Warp field collapsed at {Pct(r.Total)} risk, {what}. Nacelles damaged, banks drained, course lost.", "alert");
        if (r.Total > M.BreachRisk && sev > M.BreachSev) { Say("Engineering", "Warp core breach."); Meltdown(s); }
    }

    // ---- The warp drive's orders (the player's ship) ---------------------------------------------------------------

    /// <summary>Plots a warp course: navigation calculating the jump, whatever the ship does meanwhile.</summary>
    public void PlotCourse()
    {
        WarpState w = Player.Warp;
        if (!Player.Alive || (w.Plotting && w.Plot < 1)) return;
        w.Plotting = true; w.Plot = 0;
        Say("Navigation", "Plotting a warp course.");
    }

    /// <summary>The safety override, needed to jump in a strong quantum-gravitational field.</summary>
    public void ToggleOverride()
    {
        WarpState w = Player.Warp;
        w.Override = !w.Override;
        Say("Helm", w.Override ? "Safety override on." : "Safety override off.");
    }

    /// <summary>The Warp page's Jump key: the jump now, or the reason it is refused.</summary>
    public void JumpNow() { if (Player.Alive) Jump(Player); }

    /// <summary>The warp key at the end of the speed slider. Offline, a click arms the coils (they take free energy) and
    /// plots a course; charging, a click reports the charge and the plot and changes nothing (it never cuts power: Warp 0
    /// in Allocation powers the coils down); ready, a click jumps.</summary>
    public void WarpKey()
    {
        Ship s = Player;
        WarpState w = s.Warp;
        WarpDef W = Data.Systems.Warp;
        if (!s.Alive) return;
        if (!w.Armed)
        {
            w.Armed = true; w.Want = W.CoilE;
            if (!w.Plotting) PlotCourse();
            Say("Engineering", Rules.Output(s) - Rules.Used(s) >= 1 ? "Warp drive on: charging the banks and plotting a course." : "Warp drive on, but no reactor energy is free: power systems down to charge it.");
            return;
        }
        if (WarpReady(s)) { Jump(s); return; }
        string why = WarpRiskOf(s).Why, course = CourseText(s);
        int pw = s.Power["warp"];
        Say("Engineering", w.Banks >= W.JumpE
            ? (w.Plot < 1 ? $"Warp banks charged; still plotting the course, {course}." : $"Cannot jump: {why}.")
            : $"Warp banks {Math.Floor(w.Banks)} of {W.JumpE:0} E{(pw > 0 ? $", charging on {pw} E" : ", not charging: no free energy, power systems down")}; course {course}.");
    }

    /// <summary>The course plot as the console says it: its share, "stopped: sensors out", or "none".</summary>
    public static string CourseText(Ship s) => !s.Warp.Plotting ? "none" : s.Sys["sensors"] <= 0 && s.Warp.Plot < 1 ? "stopped: sensors out" : Pct(s.Warp.Plot);

    // Warp set by hand in Allocation: the coils want that much, and are armed while it is above 0 (SetPower).
    private void SetWarpPower(int n)
    {
        Ship s = Player;
        int cur = s.Power["warp"];
        s.Power["warp"] = Math.Clamp(n, 0, Math.Max(0, Math.Max(Math.Min(cur, n), Math.Min(Rules.BoxesPerSystem, Rules.PowerRoom(s, cur)))));
        s.Warp.Want = s.Power["warp"];
        s.Warp.Armed = s.Power["warp"] > 0;
    }
}
