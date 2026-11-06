// ConsoleState.Comms: the console's own state for the comms and transport stations (the battle console mockup's logOpen,
// trTab, cargoView, qPage and raidMountOpen): what is open and shown, never a rule of the battle. Its own file so the
// system packages never edit the same lines of ConsoleState. A mission's goals are here too: the mission strip over the view
// and the Comms station both list them from Goals (owner, 2026-11-06: "add close button to goals, and put them in comms panel").
using System;
using System.Collections.Generic;

namespace FleetCommand.Game.Ui;

/// <summary>One of a mission's goals as the console shows it: its words, whether it is a primary goal, and met now.</summary>
public sealed record GoalLine(string Text, bool Primary, bool Met);

public sealed partial class ConsoleState
{
    /// <summary>The transport station's mode: capture, hit (hit and run), cargo, mines or security.</summary>
    public string TransportTab = "hit";
    /// <summary>The cargo mode's view: "sup" (supplies and the envoy) or "misc" (trade goods).</summary>
    public string CargoView = "sup";
    /// <summary>The boarding queues' page, ten a page, by mode.</summary>
    public readonly Dictionary<string, int> QueuePage = new() { ["capture"] = 0, ["hit"] = 0 };
    /// <summary>The raid's weapon list is open.</summary>
    public bool RaidMountOpen;
    /// <summary>The mission's goals now, set by the game flow while a campaign mission plays (null in a skirmish).</summary>
    public Func<IReadOnlyList<GoalLine>>? Goals;
    /// <summary>The goals over the view: shut by their X, shown again from Comms.</summary>
    public bool GoalsShown = true;
}
