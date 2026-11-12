// BattleView.Bank: a ship rolls into its turns (owner, 2026-11-12: "when ship turns it needs to roll a little. 30 degrees for
// small ship, 20 degrees for medium ship, 10 degrees for large ship", "if turning, slerp to roll angle (for 5 seconds)",
// "if stop turning slerp roll angle back to 0"). The view's only: the simulation's ship has a heading and no roll, so a
// bank changes nothing a battle decides and lockstep peers need not agree on it. A ship is turning while its heading moved
// faster than data/battle/bank.json's turning_from_deg_s over the last simulation step, with at least min_order_deg of
// its ordered turn still to go (owner: "less than 2 degrees dont roll", so an autopilot's small corrections leave it level);
// read on the battle's clock, so a frame with no step keeps the last answer and a pause holds the roll. Its roll then moves toward its size's angle, the
// side it turns to dipping, at the rate that covers the whole angle in ease_s; with no turn it moves back to level the same
// way. A ship's size is its class's turn rate (bank.json says where the steps fall).
using System;
using System.Collections.Generic;
using FleetCommand.Data;
using FleetCommand.Sim;
using Godot;

namespace FleetCommand.Game.View;

/// <summary>The roll into a turn (data/battle/bank.json).</summary>
public sealed record BankLook(double SmallDeg, double MediumDeg, double LargeDeg, double SmallFromTurnDegS, double MediumFromTurnDegS, double EaseS, double TurningFromDegS, double MinOrderDeg)
{
    public const string File = "data/battle/bank.json";
    private static BankLook? _look;

    /// <summary>The roll a class banks to in a turn, degrees: by how fast it turns, the nimble small, the slow large.</summary>
    public double DegFor(ShipClass c) => c.TurnDegS >= SmallFromTurnDegS ? SmallDeg : c.TurnDegS >= MediumFromTurnDegS ? MediumDeg : LargeDeg;

    public static BankLook Get()
    {
        if (_look != null) return _look;
        string text = FileAccess.GetFileAsString("res://" + File);
        if (string.IsNullOrEmpty(text)) throw new DataError($"{File}: cannot be opened");
        StrictJson o = StrictJson.Parse(text, File);
        o.Str("_doc", "");
        double D(string k, double lo, double hi)
        {
            double v = o.Num(k);
            if (!(v >= lo && v <= hi)) throw new DataError($"{File}.{k}: between {lo} and {hi}, not {v}");
            return v;
        }
        var look = new BankLook(D("small_deg", 0, 80), D("medium_deg", 0, 80), D("large_deg", 0, 80), D("small_from_turn_deg_s", 0, 360),
            D("medium_from_turn_deg_s", 0, 360), D("ease_s", 0.05, 60), D("turning_from_deg_s", 0, 90), D("min_order_deg", 0, 90));
        o.Done();
        if (look.MediumFromTurnDegS > look.SmallFromTurnDegS) throw new DataError($"{File}.medium_from_turn_deg_s: not above small_from_turn_deg_s");
        return _look = look;
    }
}

public sealed partial class BattleView
{
    private sealed class Bank
    {
        public double H, T = double.NaN;   // the heading at the last simulation step seen, and that step's battle time
        public int Turn;                    // -1 turning left, +1 right, 0 straight
        public double RollDeg;              // the roll now, + the right side down
    }

    private readonly Dictionary<string, Bank> _banks = new();

    /// <summary>A ship's roll now, radians, about its own fore-and-aft axis (Godot's: + the right side up), after moving it
    /// <paramref name="dt"/> battle seconds toward its turn's angle or level.</summary>
    private float BankOf(Battle b, Ship s, double dt)
    {
        BankLook look = BankLook.Get();
        if (!_banks.TryGetValue(s.Id, out Bank? k)) _banks[s.Id] = k = new Bank { H = s.H, T = b.T };
        if (b.T > k.T)
        {
            double dh = Math.IEEERemainder(s.H - k.H, Math.Tau);
            double rateDegS = dh / (b.T - k.T) * 180 / Math.PI;
            // a turn with less than min_order_deg still to go is not rolled into (a captain's nudge, an AI's small corrections)
            double leftDeg = Math.Abs(Geometry.Wrap(s.HOrder - s.H)) * 180 / Math.PI;
            k.Turn = s.Alive && Math.Abs(rateDegS) > look.TurningFromDegS && leftDeg >= look.MinOrderDeg ? Math.Sign(rateDegS) : 0;
            k.H = s.H; k.T = b.T;
        }
        else if (b.T < k.T) { k.H = s.H; k.T = b.T; k.Turn = 0; }   // a new battle, or one wound back
        double max = look.DegFor(s.Class);
        double goal = k.Turn * max, step = max / look.EaseS * dt;
        k.RollDeg = Math.Abs(goal - k.RollDeg) <= step ? goal : k.RollDeg + Math.Sign(goal - k.RollDeg) * step;
        return -(float)(k.RollDeg * Math.PI / 180);
    }
}
