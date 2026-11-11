// Determinism: the check that lockstep co-op rests on (openspec/changes/bridge-crew-coop, design "Lockstep"). Every peer
// steps its own copy of the battle from the same orders, so the same orders must give the same battle on every machine,
// the owner's Windows laptop (x64) and the Mac (ARM) alike. Probe plays every scenario for a while with a fixed pattern of
// orders (lock the next enemy, intercept, fire), and returns each scenario's checksum (Battle.Checksum) at fixed steps; two
// machines that print the same lines step the same battles. Run it with `--checksum` (Main) on each machine and compare.
using System;
using System.Collections.Generic;
using FleetCommand.Data;
using FleetCommand.Sim;

namespace FleetCommand.Coop;

public static class Determinism
{
    /// <summary>Each scenario's checksum after every <paramref name="every"/> steps up to <paramref name="steps"/>, one line
    /// per scenario: "id hash hash ...".</summary>
    public static IEnumerable<string> Probe(GameData data, int steps = 3000, int every = 1000)
    {
        foreach (Scenario sc in data.Scenarios)
        {
            var b = new Battle(data, sc);
            var hashes = new List<string>();
            for (int i = 1; i <= steps; i++)
            {
                if (i % 90 == 1) { b.NextTarget(); b.HelmOrder("intercept"); }
                if (i % 15 == 0) b.Fire();
                b.Step();
                if (i % every == 0) hashes.Add(b.Checksum().ToString("x16"));
            }
            yield return $"{sc.Id} {string.Join(' ', hashes)} {b.Over ?? "-"}";
        }
    }
}
