// ScriptedCaptain.Systems: the all_out beat (owner, 2026-11-10: "make sure the bot uses as many systems at their
// disposal: shuttles, fighters, missiles, tractor beams, mines, and trransporters", "and of course special weapons").
// It is the fight beat (lock, Intercept, fire: the one Fight loop, ScriptedCaptain.cs) with a captain's hands on the rest
// of the ship between volleys, every order given through the console's own keys where they are drawn:
// - power: two boxes from the shields, one each to the tractor and the transporter (the classes start both at none);
// - fighters: the Craft page's Defences mission and Launch, once the target is near enough to watch them arrive;
// - missiles: the Craft page's Barrage, a salvo at a time while the racks last;
// - shuttle: the Craft page's Shuttle, a disguised shuttle at the target;
// - tractor: the Tractor page's Hold as the target comes inside reach;
// - mines: the Transport station's Mines tab, Place mine and a click on the target in the view, or Drop astern;
// - transporter: a hit and run raid on the target's weapons control (two marines and a demolition, Bomb, Energize all),
//   whenever the facing toward the ship is down and the target is inside transporter range;
// - special weapons: the Chorus lance, Quell torpedo and Forge driver are mounts, so the Fire key looses them with the
//   rest; each one loosed is counted. The Krai blood charge has no key on the player's helm yet (special-weapons task
//   2.4), so only AI captains order it.
// With arg "hop" the captain takes each living ship of the fleet in turn (keys 1 to 9), so every ship's bays, racks and
// pads are used, as a player commanding a fleet switches between them. One order every few seconds, as hands allow. What
// was used is printed as PLAYTEST_SYSTEMS when the beat ends. The beat's own numbers (how near, how often) are the
// captain's habits, not game tuning; what each system does is the simulation's (data/battle/systems).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.Playtest;

public sealed partial class ScriptedCaptain
{
    // the captain's habits: seconds between orders and between ships, and how near (km) the target is before each is tried
    private const double OrderEveryS = 4, HopEveryS = 40, FightersKm = 90, BarrageKm = 100, ShuttleKm = 45, TractorKm = 40, MineDropKm = 10;
    private static readonly string[] SpecialIds = { "chorus", "quell", "forge" };

    private readonly SortedDictionary<string, int> _used = new();
    private readonly HashSet<string> _powered = new();
    private double _orderT = double.MinValue, _hopT;

    private void Used(string what) => _used[what] = _used.GetValueOrDefault(what) + 1;

    // the special weapons among the mounts the Fire key is about to loose
    private void NoteSpecials(Mount[] loose)
    {
        foreach (Mount m in loose) if (SpecialIds.Contains(m.Weapon.Id)) Used(m.Weapon.Id);
    }

    private async Task<string?> AllOut(double maxS, string arg)
    {
        if (arg is not ("" or "hop")) return $"all_out takes no arg or \"hop\", not '{arg}'";
        _hopT = B.T;
        string? stuck = await Fight(maxS, each: me => UseSystems(me, arg == "hop"));
        GD.Print($"PLAYTEST_SYSTEMS {(_used.Count == 0 ? "none" : string.Join(" ", _used.Select(kv => $"{kv.Key}={kv.Value}")))}");
        // the account of the fight: the ships lost, every lightning strike and core restart, and what the flight deck, the
        // transporter room, the tractor and the missile racks reported
        PrintAccount(l => l.Who is "Flight deck" or "Transport" or "Tractor" || l.Text.Contains("ightning") || l.Text.StartsWith("Core restarted")
                          || l.Text.StartsWith("Missile barrage") || l.Text.StartsWith("Shuttle") || l.Text.StartsWith("Mine"));
        return stuck;
    }

    // clicks each control in turn; false at the first that is not there or does nothing yet
    private async Task<bool> Keys(params string[] ids)
    {
        foreach (string id in ids) if (await Click(id) != null) return false;
        return true;
    }

    // One order, if one is due and something aboard can be used on the locked enemy now.
    private async Task UseSystems(Ship me, bool hop)
    {
        if (B.T - _orderT < OrderEveryS) return;
        if (hop && B.T - _hopT >= HopEveryS && B.Fleet() is { Length: > 1 } fleet)
        {
            _hopT = B.T; _orderT = B.T;
            Press($"command_{(Array.FindIndex(fleet, s => s.Id == me.Id) + 1) % fleet.Length + 1}");
            await Frames(3);
            Used("ships_taken");
            return;
        }
        if (_x.MyTarget() is not { } t || t.Side == me.Side || !t.Alive) return;
        double km = Rules.DistKm(me, t);
        double before = B.T;
        _orderT = B.T;
        if (_powered.Add(me.Id))
        {
            // the tractor and the transporter start with no energy: two boxes off the shields, one to each
            if (me.Power["tractor"] == 0 && me.Power["transport"] == 0 && me.Power["shields"] >= 3
                && await SetBoxes($"shields={me.Power["shields"] - 2}") == null && await SetBoxes("tractor=1") == null && await SetBoxes("transport=1") == null)
                Used("power");
        }
        else if (me.Fighters > 0 && km <= FightersKm)
        {
            int n = me.Fighters;
            if (await Keys("sh.craft", "cr.m.defences", "cr.l") && me.Fighters == 0) _used["fighters"] = _used.GetValueOrDefault("fighters") + n;
        }
        else if (me.Missiles > 0 && me.Barrage == 0 && km <= BarrageKm)
        {
            if (await Keys("sh.craft", "cr.b") && me.Barrage > 0) Used("barrages");
        }
        else if (me.Shuttles > 0 && km <= ShuttleKm)
        {
            int n = me.Shuttles;
            if (await Keys("sh.craft", "cr.s") && me.Shuttles < n) Used("shuttles");
        }
        else if (me.Tractor == TractorMode.Off && !B.TractorDown(me) && B.HeldBy(me) == null && km <= TractorKm)
        {
            if (await Keys("sh.tractor", "trc.hold") && me.Tractor != TractorMode.Off) Used("tractor");
        }
        else if (B.CanRaid(me, t) && me.Raid == null && me.Marines > 0)
        {
            if (await Keys("st.transport", "tr.tab.hit", "tr.sys.weapons", "tr.kind.bomb"))
            {
                // two marines and a demolition; a role with nobody left aboard is passed over
                await Click("tr.q.hit.marines"); await Click("tr.q.hit.marines"); await Click("tr.q.hit.demo");
                if (await Click("tr.pad.hit.all") == null && me.Raid != null) Used("raids");
            }
        }
        else if (B.MineReady(me) && km <= B.Data.Systems.Mines.TransporterReachKm && B.CanBeamMine(me))
        {
            // beamed onto the target; off screen, the key pressed again is Cancel, and the mine goes astern when it is close
            if (await PlaceMine(t.X - me.X, t.Z - me.Z) == null) Used("mines");
            else
            {
                if (_x.State.PlacingMine) await Click("mn.place");
                int n = me.Mines;
                if (km <= MineDropKm && await Click("mn.drop") == null && me.Mines < n) Used("mines");
            }
        }
        else if (B.MineReady(me) && km <= MineDropKm)
        {
            int n = me.Mines;
            if (await Keys("st.transport", "tr.tab.mines", "mn.drop") && me.Mines < n) Used("mines");
        }
        else _orderT = before - OrderEveryS;   // nothing to order: look again at once
    }
}
