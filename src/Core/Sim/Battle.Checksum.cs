// Battle.Checksum: a 64-bit fingerprint of the battle's state, for co-op (openspec/changes/bridge-crew-coop): every peer
// steps the same battle from the same orders (lockstep), and the host and the crew compare this number every second, so a
// battle that has drifted apart is caught the moment it does rather than seen as ships in different places. It hashes what
// the battle's outcome depends on (the time, every ship's place, motion, hull, shields, power, systems, mounts and target,
// every shot in flight), bit for bit, in the battle's stable order; the log and the view's hit memory are left out.
using System;
using System.Collections.Generic;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    /// <summary>The state's fingerprint (FNV-1a over the bits of every number that decides the battle).</summary>
    public ulong Checksum()
    {
        ulong h = 0xCBF29CE484222325UL;
        void U(ulong v) { for (int i = 0; i < 8; i++) { h ^= (v >> (i * 8)) & 0xFF; h *= 0x100000001B3UL; } }
        void D(double v) => U((ulong)BitConverter.DoubleToInt64Bits(v));
        void S(string? s) { if (s == null) { U(0); return; } foreach (char c in s) { h ^= c; h *= 0x100000001B3UL; } U(1); }
        U((ulong)Steps);
        S(Over);
        S(Player.Id);
        foreach (Ship s in _ships)
        {
            S(s.Id); U(s.Alive ? 1UL : 0UL); U((ulong)s.Side);
            D(s.X); D(s.Z); D(s.H); D(s.V); D(s.HOrder); D(s.VOrder); D(s.Hull);
            D(s.Capacitor); D(s.Battery);
            foreach (double v in s.Shields) D(v);
            S(s.Target);
            if (s.RoleTargets != null) foreach (KeyValuePair<string, string?> kv in s.RoleTargets) { S(kv.Key); S(kv.Value); }
            foreach (Mount m in s.Mounts) { D(m.Charge); D(m.Hp); U((ulong)m.State); }
            foreach (KeyValuePair<string, double> kv in s.Sys) { S(kv.Key); D(kv.Value); }
            foreach (KeyValuePair<string, int> kv in s.Power) { S(kv.Key); U((ulong)kv.Value); }
        }
        U((ulong)_shots.Count);
        foreach (Shot sh in _shots) { S(sh.Kind); D(sh.X); D(sh.Z); D(sh.Damage); S(sh.To); }
        return h;
    }
}
