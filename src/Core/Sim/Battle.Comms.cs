// Battle.Comms: the comms station's orders and its log (the battle console mockup's stComms and A.hail, A.fleetOrder): a
// hail to the locked ship, answered by its faction's line or not at all (owner, 2026-11-06: "I should be able to hail
// other ships, although most enemies wont respond"), a friendly one saying what it still needs; fleet orders to every
// wingman; and which log lines are refusals (shown in Caution, as the mockup's WARN_RE). Each step it says when the
// transport destination crosses the transporter's reach (owner, 2026-11-03: "when crossing this range transporters become
// active"), once each way.
using System.Linq;
using System.Text.RegularExpressions;

namespace FleetCommand.Sim;

public sealed partial class Battle
{
    private static readonly Regex Warn = new("^(Cannot|No lock|Objective failed|Warp field collapsed|Warp core breach|Out of|No target|No destination|Not during|The queue is empty|Pads charging|Their shield|That ship is ours|A raid is out|A boarding party is out)", RegexOptions.CultureInvariant);

    /// <summary>A refusal (an order that could not be carried out): shown in Caution and marked in the log.</summary>
    public static bool IsWarning(LogLine l) => Warn.IsMatch(l.Text);

    private string? _reachDest;
    private bool _inReach;

    /// <summary>Hails the locked ship. An enemy answers with its faction's line, if it has one; most say nothing. A friendly
    /// ship answers, saying what it still needs, or that it needs a pilot.</summary>
    public void Hail()
    {
        Ship me = Player;
        if (TargetOf(me) is not { } t) { Say("Comms", "No target: click a ship to hail it."); return; }
        var h = Comms.Hail;
        if (t.Side != me.Side)
        {
            if (h.EnemyReply.TryGetValue(t.Faction.Id, out string? reply) && reply != "") Say(t.Name, reply);
            else Say("Comms", $"{t.Name}: {h.NoAnswer}");
            return;
        }
        string needs = NeedsText(t);
        string text = PilotNeeded(t) ? h.PilotNeeded : needs != "" ? h.FriendNeeds : h.FriendReply;
        Say(t.Name, text.Replace("{name}", t.Name).Replace("{captain}", t.Captain).Replace("{needs}", needs));
    }

    /// <summary>Fleet orders from comms: every wingman takes the stance (All attack, Form on me, All hold).</summary>
    public void OrderFleet(Stance stance)
    {
        foreach (Ship s in _ships.Where(o => o.Alive && o.Side == Player.Side && o != Player)) s.Stance = stance;
        Say("Comms", $"Fleet: {stance switch { Stance.Escort => "Form on me", Stance.Hold => "Hold", _ => "Attack" }}.");
    }

    partial void CommsStep(double dt)
    {
        if (InvPick() is not { } d) { _reachDest = null; return; }
        bool inR = d.Km <= Comms.Transport.RangeKm;
        if (_reachDest == d.Id && inR != _inReach) Say("Transport", inR ? $"{d.Name} in transporter range: transporters active." : $"{d.Name} out of transporter range.");
        _reachDest = d.Id;
        _inReach = inR;
    }
}
