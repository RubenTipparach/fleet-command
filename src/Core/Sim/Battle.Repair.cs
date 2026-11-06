// Battle.Repair: damage control (openspec/changes/damage-control; the battle console mockup's repairPlan, mendJob, jobEta,
// jobHealth and the queue's orders). The captain orders the repair queue; the crews, one for each point of repair
// energy, work down it: at most max_crews on one job, the rest on the next job that still needs them. A crew mends
// tuning's repair_per_e_s of a job's health a second (the weapon mounts mount_repair_per_e_s), browned out with the rest
// of the ship and quicker with a chief engineer (the repair officer). The reactor job mends its worst coolant loop, then
// its confinement; the warp job its worst nacelle; the mounts job its worst mount. The Repair page shows what these return.
using System;
using System.Collections.Generic;
using System.Linq;
using FleetCommand.Data;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    private static readonly string[] Ordinals = { "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth" };

    /// <summary>A repair job's health, 0 to 1: the worst part of it.</summary>
    public static double JobHealth(Ship s, string job) => job switch
    {
        "reactor" => Math.Min(Rules.Confinement(s), s.Reactor.Loops.Select(l => l.Hp).DefaultIfEmpty(1).Min()),
        "warp" => s.Warp.Nacelles.DefaultIfEmpty(1).Min(),
        "mounts" => s.Mounts.Select(m => m.Hp).DefaultIfEmpty(1).Min(),
        _ => s.Sys.GetValueOrDefault(job, 1),
    };

    /// <summary>Who works where: the crews down the queue, at most max_crews on a job, only on jobs not yet whole.</summary>
    public List<(string Job, int Crews)> RepairPlan(Ship s)
    {
        int left = s.Power.GetValueOrDefault("repair"), max = Data.Systems.Repair.MaxCrews;
        var plan = new List<(string, int)>();
        foreach (string k in s.RepairQueue)
        {
            if (left <= 0) break;
            if (JobHealth(s, k) >= 1) continue;
            int n = Math.Min(max, left);
            plan.Add((k, n));
            left -= n;
        }
        return plan;
    }

    /// <summary>The crews on a job now.</summary>
    public int CrewsOn(Ship s, string job) => RepairPlan(s).FirstOrDefault(p => p.Job == job).Crews;

    // what one crew mends a second, before brownout: the mounts their own rate, every other job the crews' rate, the chief
    // engineer's skill adding repair_rate a level to both
    private double CrewRate(Ship s, string job) =>
        (job == "mounts" ? Tuning.MountRepairPerES : Tuning.RepairPerES) * (1 + Rules.OfficerSkill(s, "repair") * Data.Systems.Officers.Ability.RepairRate);

    partial void RepairCrewsStep(double dt)
    {
        foreach (Ship s in _ships)
        {
            if (!s.Alive) continue;
            foreach (var (k, n) in RepairPlan(s)) MendJob(s, k, n * s.Brown * dt * CrewRate(s, k));
        }
    }

    // Health mended on a job: its worst part first.
    private static void MendJob(Ship s, string job, double amount)
    {
        switch (job)
        {
            case "reactor":
            {
                CoolantLoop? worst = s.Reactor.Loops.Length == 0 ? null : s.Reactor.Loops.Aggregate((a, q) => q.Hp < a.Hp ? q : a);
                if (worst != null && worst.Hp < 1 && worst.Hp <= Rules.Confinement(s)) worst.Hp = Math.Min(1, worst.Hp + amount);
                else s.Sys["reactor"] = Math.Min(1, Rules.Confinement(s) + amount);
                break;
            }
            case "warp":
            {
                double[] n = s.Warp.Nacelles;
                if (n.Length == 0) break;
                int i = Array.IndexOf(n, n.Min());
                n[i] = Math.Min(1, n[i] + amount);
                break;
            }
            case "mounts":
            {
                if (s.Mounts.Count == 0) break;
                Mount m = s.Mounts.Aggregate((a, q) => q.Hp < a.Hp ? q : a);
                m.Hp = Math.Min(1, m.Hp + amount);
                break;
            }
            default:
                if (s.Sys.ContainsKey(job)) s.Sys[job] = Math.Min(1, s.Sys[job] + amount);
                break;
        }
    }

    /// <summary>Seconds until a job is whole at the crews it has now: 0 when whole, infinity with no crews on it.</summary>
    public double JobEta(Ship s, string job)
    {
        int n = CrewsOn(s, job);
        double h = JobHealth(s, job);
        if (h >= 1) return 0;
        if (n <= 0 || s.Brown <= 0) return double.PositiveInfinity;
        double rate = n * s.Brown * CrewRate(s, job);
        // the reactor job mends the loops and the confinement in turn; the mounts and nacelles one at a time
        IEnumerable<double> parts = job switch
        {
            "reactor" => s.Reactor.Loops.Select(l => l.Hp).Prepend(Rules.Confinement(s)),
            "warp" => s.Warp.Nacelles,
            "mounts" => s.Mounts.Select(m => m.Hp),
            _ => new[] { h },
        };
        return parts.Sum(v => Math.Max(0, 1 - v)) / rate;
    }

    // ---- The repair queue's orders (the player's ship) -------------------------------------------------------------

    /// <summary>Moves a job to place <paramref name="index"/> of the queue (the drag); quiet while dragging.</summary>
    public void RepairMove(string job, int index, bool quiet = false)
    {
        List<string> q = Player.RepairQueue;
        int j = q.IndexOf(job);
        if (j < 0 || index == j || index < 0 || index >= q.Count) return;
        q.RemoveAt(j);
        q.Insert(index, job);
        if (!quiet) Say("Engineering", $"{Data.Systems.Repair.Job(job).Name} moved to place {index + 1}.");
    }

    /// <summary>A drag of a job ends (the drop): says where it landed when it moved from place <paramref name="from"/>.</summary>
    public void RepairDropped(string job, int from)
    {
        int i = Player.RepairQueue.IndexOf(job);
        if (i < 0 || i == from) return;
        Say("Engineering", $"{Data.Systems.Repair.Job(job).Name}: {Ordinals[Math.Min(i, Ordinals.Length - 1)]} in the repair queue.");
    }

    /// <summary>A job to the top of the queue.</summary>
    public void RepairFirst(string job)
    {
        int i = Player.RepairQueue.IndexOf(job);
        if (i <= 0) return;
        RepairMove(job, 0, true);
        Say("Engineering", $"{Data.Systems.Repair.Job(job).Name} first.");
    }
}
