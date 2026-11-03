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
        Player.HOrder = Geometry.Wrap(Player.HOrder + deg * Geometry.Deg);
    }

    /// <summary>The helm by pointing: the ship comes onto the bearing of a point on the plane (metres).</summary>
    public void HelmTo(double x, double z)
    {
        if (!double.IsFinite(x) || !double.IsFinite(z) || !Player.Alive) return;
        if (Geometry.Dist(Player.X, Player.Z, x, z) < 1) return;
        Player.HOrder = Geometry.HeadingTo(Player.X, Player.Z, x, z);
    }

    /// <summary>Orders a speed in m/s, held within reverse and the engines' limit.</summary>
    public void SetSpeed(double mS)
    {
        if (!double.IsFinite(mS) || !Player.Alive) return;
        Player.VOrder = Math.Clamp(mS, -Tuning.ReverseMaxMS, Rules.MaxSpeed(Player));
    }

    /// <summary>Steps the ordered speed by whole engine boxes.</summary>
    public void SpeedStep(int boxes) => SetSpeed(Player.VOrder + boxes * Tuning.SpeedPerBoxMS);

    public void AllStop() { if (Player.Alive) { Player.VOrder = 0; Say("Helm", "All stop."); } }

    /// <summary>Gives a system <paramref name="boxes"/> energy boxes, within ten and the reactor's output.</summary>
    public void SetPower(string system, int boxes)
    {
        if (!Rules.Systems.Contains(system)) return;
        int cur = Player.Power[system];
        int n = Math.Clamp(boxes, 0, Math.Min(Rules.BoxesPerSystem, Rules.PowerRoom(Player, cur)));
        Player.Power[system] = n;
        if (system == "engines") Player.VOrder = Math.Min(Player.VOrder, Rules.MaxSpeed(Player));
    }

    /// <summary>Sets the arming state of every selected mount (point defence stays as it is unless it is selected).</summary>
    public void SetArming(Arming state)
    {
        foreach (Mount m in Player.Mounts)
        {
            if (!m.Selected || m.Hp <= 0) continue;
            if (state == Arming.Overload && m.Weapon.Overload == null) continue;
            if (m.State == state) continue;
            // switching to or from overload keeps the charge already in the tube; offline drains it
            if (state == Arming.Offline) m.Charge = 0;
            m.State = state;
        }
    }

    public void ToggleMount(int index)
    {
        if (index < 0 || index >= Player.Mounts.Count) return;
        Player.Mounts[index].Selected = !Player.Mounts[index].Selected;
    }

    public void ToggleVolley() => Player.Volley = !Player.Volley;

    /// <summary>The mounts Fire would loose now at the locked target: selected, ready, in arc and in range.</summary>
    public Mount[] WouldFire()
    {
        Ship? t = TargetOf(Player);
        if (t == null || !Player.Alive) return Array.Empty<Mount>();
        Mount[] all = Player.Mounts.Where(m => m.Selected && !m.Weapon.IsPointDefence && Rules.Ready(m) && Rules.InArc(Player, m, t) && Rules.InRange(Player, m, t)).ToArray();
        return Player.Volley ? all : all.Take(1).ToArray();
    }

    /// <summary>Fires the selected mounts at the locked target.</summary>
    public int Fire()
    {
        Ship? t = TargetOf(Player);
        if (t == null) { Say("Tactical", "No target locked."); return 0; }
        int n = 0;
        foreach (Mount m in WouldFire()) if (FireMount(Player, m, t)) n++;
        if (n == 0) Say("Tactical", "No weapon ready in arc and range.");
        return n;
    }

    /// <summary>Locks an enemy ship as the target. A friendly ship cannot be locked.</summary>
    public void Lock(string id)
    {
        Ship? t = ShipById(id);
        if (t is not { Alive: true } || t.Side == Player.Side) return;
        Player.Target = t.Id;
        Say("Tactical", $"Target locked: {t.Name}, {t.Class.Name}.");
    }

    /// <summary>Locks the next enemy by range, nearest first, after the one locked now.</summary>
    public void NextTarget()
    {
        Ship[] foes = EnemiesOf(Player).OrderBy(o => Rules.DistKm(Player, o)).ThenBy(o => o.Order).ToArray();
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
