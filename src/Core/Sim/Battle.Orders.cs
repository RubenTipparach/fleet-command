// Battle.Orders: everything the captain can order, one method per order. The console's keys, the keyboard's InputMap
// actions, the view's clicks and the scripted captain all call these (CLAUDE.md section 6.2: input is actions). An
// order that does not fit changes nothing (section 6.5), and real time with pause means every order works while paused
// (survey G2): orders change the state, only Step() moves time.
using System;
using System.Linq;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    /// <summary>Turns the ordered heading by <paramref name="deg"/> degrees (positive to starboard).</summary>
    public void Turn(double deg)
    {
        if (!double.IsFinite(deg) || !Player.Alive) return;
        Player.Engaged = false;
        Player.HOrder = Geometry.Wrap(Player.HOrder + deg * Geometry.Deg);
    }

    /// <summary>The helm by pointing: the ship comes onto the bearing of a point on the plane (metres).</summary>
    public void HelmTo(double x, double z)
    {
        if (!double.IsFinite(x) || !double.IsFinite(z) || !Player.Alive) return;
        if (Geometry.Dist(Player.X, Player.Z, x, z) < 1) return;
        Player.Engaged = false;
        Player.HOrder = Geometry.HeadingTo(Player.X, Player.Z, x, z);
    }

    /// <summary>Orders a speed in m/s, held within reverse and the engines' limit.</summary>
    public void SetSpeed(double mS)
    {
        if (!double.IsFinite(mS) || !Player.Alive) return;
        if (Player.Engaged && Player.Manoeuvre == "trail") Player.Engaged = false;
        Player.VOrder = Math.Clamp(mS, -Tuning.ReverseMaxMS, Rules.MaxSpeed(Player));
    }

    /// <summary>Steps the ordered speed by whole engine boxes.</summary>
    public void SpeedStep(int boxes) => SetSpeed((Math.Round(Player.VOrder / Tuning.SpeedPerBoxMS) + boxes) * Tuning.SpeedPerBoxMS);

    public void AllStop() { if (Player.Alive) { Player.Engaged = false; Player.VOrder = 0; Say("Helm", "All stop."); } }

    /// <summary>What the helm's manoeuvres steer toward: the locked enemy (friendly destinations come with transport).</summary>
    public Ship? NavTarget(Ship s) => TargetOf(s);

    /// <summary>An order toward the target: intercept, trail, hold_range, orbit or hit_run. The lit order again, or
    /// "free", returns to free heading.</summary>
    public void HelmOrder(string order)
    {
        Ship s = Player;
        if (order == "free" || (s.Engaged && s.Manoeuvre == order)) { s.Engaged = false; Say("Helm", "Free heading."); return; }
        if (order is not ("intercept" or "trail" or "hold_range" or "orbit" or "hit_run")) return;
        Ship? t = NavTarget(s);
        if (t == null) { Say("Helm", "No target: click a ship."); return; }
        s.Manoeuvre = order;
        s.Engaged = true;
        if (order == "hit_run") s.RunPhase = "in";
        string what = order switch
        {
            "intercept" => "Intercepting", "trail" => "Trailing", "orbit" => "Orbiting",
            "hold_range" => $"Holding {s.HoldKm:0.0} km from", _ => $"Hit and run, {s.RunMinKm:0.0}-{s.RunMaxKm:0.0} km, on",
        };
        Say("Helm", $"{what} {t.Name}.");
    }

    public void ToggleOrbitDir() => Player.OrbitDir = -Player.OrbitDir;

    private double HalfKm(double v) => Math.Clamp(Math.Round(v * 2) / 2, Tuning.HoldMinKm, Tuning.HoldMaxKm);

    /// <summary>The distance hold range keeps, in half kilometres.</summary>
    public void SetHoldKm(double km) { if (double.IsFinite(km)) Player.HoldKm = HalfKm(km); }

    /// <summary>Hit and run's nearest and farthest range, at least half a kilometre apart.</summary>
    public void SetRunRange(double lo, double hi)
    {
        if (!double.IsFinite(lo) || !double.IsFinite(hi)) return;
        Player.RunMinKm = Math.Min(HalfKm(lo), HalfKm(hi) - 0.5);
        Player.RunMaxKm = Math.Max(HalfKm(hi), Player.RunMinKm + 0.5);
    }

    /// <summary>True while a high energy turn cannot be ordered (one under way, broken down, or the battery short).</summary>
    public bool HetBusy(Ship s) => s.Het != null || s.Broke > 0 || s.Battery < Tuning.HetCostE || !s.Alive;

    /// <summary>A high energy turn by deg (negative to port), rolled against Rules.HetChanceNow. A failure leaves the
    /// impulse engines damaged and the ship dead in space for het_breakdown_s.</summary>
    public void Het(double deg)
    {
        Ship s = Player;
        if (!double.IsFinite(deg) || s.Het != null || s.Broke > 0 || !s.Alive) return;
        if (s.Battery < Tuning.HetCostE) { Say("Helm", "Not enough power for a high energy turn."); return; }
        s.Battery -= Tuning.HetCostE;
        s.Engaged = false;
        double p = Rules.HetChanceNow(s);
        s.HetChance = Math.Max(0, s.HetChance - Tuning.HetChanceDrop);
        if (_helm.Next() < p)
        {
            s.Het = deg * Geometry.Deg;
            Say("Helm", $"High energy turn, {Math.Abs(deg):0} degrees {(deg < 0 ? "to port" : "to starboard")}.");
        }
        else
        {
            s.Broke = Tuning.HetBreakdownS;
            s.Sys["engines"] = Math.Max(0, s.Sys["engines"] - Tuning.HetBreakdownEngines);
            s.V = 0; s.VOrder = 0;
            Say("Engineering", $"High energy turn failed: impulse engines damaged, the ship is dead in space for {Tuning.HetBreakdownS:0} s.");
        }
    }

    /// <summary>A high energy turn onto the bearing of a point on the plane (the free-angle key, then a click in the view).</summary>
    public void HetTo(double x, double z)
    {
        if (!double.IsFinite(x) || !double.IsFinite(z)) return;
        Het(Math.Round(Geometry.Wrap(Geometry.HeadingTo(Player.X, Player.Z, x, z) - Player.H) / Geometry.Deg));
    }

    public void ToggleErratic()
    {
        Player.Erratic = !Player.Erratic;
        Say("Helm", Player.Erratic ? "Erratic manoeuvres." : "Erratic manoeuvres off.");
    }

    /// <summary>Emergency deceleration: stops as fast as the engines can, then holds them for estop_hold_s.</summary>
    public void EmergencyStop()
    {
        if (Player.EstopT > 0 || !Player.Alive) return;
        Player.Engaged = false;
        Player.Estop = true;
        Say("Helm", "Emergency deceleration.");
    }

    /// <summary>Gives a system <paramref name="boxes"/> energy boxes, within ten and the reactor's output; lowering one is
    /// always allowed, even while the output is short (the mockup's power). Warp is set here too (Battle.Warp).</summary>
    public void SetPower(string system, int boxes)
    {
        if (system == "warp") { SetWarpPower(boxes); return; }
        if (!Rules.Systems.Contains(system)) return;
        int cur = Player.Power[system];
        int n = Math.Clamp(boxes, 0, Math.Max(0, Math.Max(Math.Min(cur, boxes), Math.Min(Rules.BoxesPerSystem, Rules.PowerRoom(Player, cur)))));
        Player.Power[system] = n;
        if (system == "sensors") ClampEw(Player);   // ECM and ECCM fit inside the sensors' energy (Battle.Sensors.cs)
        if (system == "engines") Player.VOrder = Math.Min(Player.VOrder, Rules.MaxSpeed(Player));
    }

    /// <summary>Sets the arming state of every selected mount that has that mode (point defence is never armed here).</summary>
    public void SetArming(Arming state)
    {
        foreach (Mount m in Player.Mounts) if (m.Selected) Arm(m, state);
    }

    // One mount into a mode it has (weapon-arming design, the mockup's arm): offline drains the tube, any other change keeps
    // at most half a charge, so a mode change is never a free shot.
    private static void Arm(Mount m, Arming state)
    {
        if (m.Hp <= 0 || !Rules.CanArm(m.Weapon, state) || m.State == state) return;
        m.Charge = state == Arming.Offline ? 0 : Math.Min(m.Charge, 0.5);
        m.State = state;
    }

    public void ToggleMount(int index)
    {
        if (index < 0 || index >= Player.Mounts.Count) return;
        Player.Mounts[index].Selected = !Player.Mounts[index].Selected;
    }

    public void ToggleVolley() => Player.Volley = !Player.Volley;

    /// <summary>Selects the mounts of one weapon type for the volley (null: every weapon but point defence).</summary>
    public void SelectGroup(string? weapon)
    {
        foreach (Mount m in Player.Mounts) if (!m.Weapon.IsPointDefence) m.Selected = weapon == null || m.Weapon.Id == weapon;
    }

    /// <summary>Sets one mount's arming state.</summary>
    public void ArmOne(int index, Arming state)
    {
        if (index < 0 || index >= Player.Mounts.Count) return;
        Arm(Player.Mounts[index], state);
    }

    /// <summary>True when one mount could fire at the locked target now.</summary>
    public bool CanFire(Mount m) => TargetOf(Player) is { } t && t.Side != Player.Side && !Player.Cloaking && !m.Weapon.IsPointDefence && Rules.Ready(m) && Rules.InArc(Player, m, t) && Rules.InRange(Player, m, t);

    /// <summary>Fires one mount at the locked target.</summary>
    public bool FireOne(int index)
    {
        if (index < 0 || index >= Player.Mounts.Count || TargetOf(Player) is not { } t || t.Side == Player.Side) return false;
        Mount m = Player.Mounts[index];
        return !m.Weapon.IsPointDefence && FireMount(Player, m, t);
    }

    /// <summary>The mounts Fire would loose now: selected, ready, in arc and in range of their target (a pinned mount's
    /// ship, else the locked enemy); one at a time when not in a volley.</summary>
    public Mount[] WouldFire()
    {
        if (Player.Cloaking) return Array.Empty<Mount>();   // no fire while cloaked (Battle.Cloak.cs)
        var all = Fireable().Select(f => f.M).ToArray();
        return Player.Volley ? all : all.Take(1).ToArray();
    }

    /// <summary>Fires the selected mounts, each at its pinned ship with its pinned aim, else at the locked target with the
    /// captain's aim (pinned fire, weapon-targeting design).</summary>
    public int Fire()
    {
        if (Player.Cloaking) { Say("Tactical", "Cannot fire while cloaked."); return 0; }
        Ship? t = TargetOf(Player);
        bool pins = Player.Mounts.Any(m => m.Selected && PinTarget(m) != null);
        if (t == null && !pins) { Say("Tactical", "No target locked."); return 0; }
        if (t != null && t.Side == Player.Side && !pins) { Say("Tactical", $"The {t.Name} is friendly: weapons hold."); return 0; }
        var able = Fireable();
        int n = 0;
        foreach (var (m, tt, aim) in Player.Volley ? able : able.Take(1).ToList()) if (FireMount(Player, m, tt, aim)) n++;
        if (n == 0) Say("Tactical", "No weapon ready in arc and range.");
        return n;
    }

    /// <summary>Locks a ship as the target: an enemy to fire on, or a friendly one to hail, beam to, follow or watch (owner,
    /// 2026-11-06: "I also cant target friendly units"); the weapons never fire on a friendly one. Not the ship you command.</summary>
    public void Lock(string id)
    {
        Ship? t = ShipById(id);
        if (t is not { Alive: true } || t == Player) return;
        // an aim at the old target's weapon becomes an aim at the new one's weapons control
        if (Player.Target != t.Id && Strike.AimMount(Player.Aim) >= 0) Player.Aim = "weapons";
        // a cloaked enemy cannot be locked until it is seen, nor one a nebula hides (Battle.Cloak.cs)
        if (t.Side != Player.Side && !Detected(Player, t)) { SayOnce("Science", "No lock: it is cloaked. Sweep to find it."); return; }
        if (t.Side != Player.Side && !CanLock(Player, t)) { SayOnce("Science", $"No lock: the nebula hides it. Raise ECCM above its ECM, or close to {Cs.Nebula.LockKm:0} km."); return; }
        Player.Target = t.Id;
        Say("Tactical", t.Side == Player.Side ? $"Selected: {t.Name}, {t.Class.Name} (friendly)." : $"Target locked: {t.Name}, {t.Class.Name}.");
    }

    /// <summary>Releases the locked target.</summary>
    public void Unlock() { if (Player.Target != null) { Player.Target = null; if (Player.Engaged) Player.Engaged = false; } }

    /// <summary>Locks the next enemy by range, nearest first, after the one locked now.</summary>
    public void NextTarget()
    {
        Ship[] foes = SeenFoes(Player).OrderBy(o => Rules.DistKm(Player, o)).ThenBy(o => o.Order).ToArray();
        if (foes.Length == 0) return;
        int i = Array.FindIndex(foes, o => o.Id == Player.Target);
        Lock(foes[(i + 1) % foes.Length].Id);
    }

    /// <summary>Takes command of another living ship of the fleet; the ship left behind follows its own orders.</summary>
    public void TakeCommand(string id)
    {
        Ship? s = ShipById(id);
        if (s is not { Alive: true } || s.Side != 0 || s == Player) return;
        Player.ThinkT = 0;
        Player.Rule = TargetRule.Mine;   // the ship left behind follows its new captain's target (the mockup's command)
        Player = s;
        Say("Comms", $"{s.Captain} has the conn of {s.Name}.");
    }

    /// <summary>The fleet's living ships in scenario order, for the plate's Fleet n/m key.</summary>
    public Ship[] Fleet() => _ships.Where(s => s.Alive && s.Side == 0).ToArray();

    /// <summary>Sets the alert condition. Yellow raises shields; red also arms every offline weapon.</summary>
    public void SetAlert(AlertLevel level)
    {
        Player.Alert = level;
        if (level != AlertLevel.Green) Player.ShieldsUp = true;
        if (level == AlertLevel.Red) foreach (Mount m in Player.Mounts) if (m.State == Arming.Offline && m.Hp > 0) m.State = Arming.Normal;
    }

    public void ToggleShields() => Player.ShieldsUp = !Player.ShieldsUp;

    /// <summary>Reinforces a shield facing (0 F to 5 FL); the same facing again clears it.</summary>
    public void Reinforce(int facing)
    {
        if (facing < 0 || facing > 5) return;
        Player.Reinforce = Player.Reinforce == facing ? -1 : facing;
    }
}
