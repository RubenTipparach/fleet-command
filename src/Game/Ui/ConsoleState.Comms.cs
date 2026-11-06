// ConsoleState.Comms: the console's own state for the comms and transport stations (the battle console mockup's logOpen,
// trTab, cargoView, qPage and raidMountOpen): what is open and shown, never a rule of the battle. Its own file so the
// system packages never edit the same lines of ConsoleState.
using System.Collections.Generic;

namespace FleetCommand.Game.Ui;

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
}
