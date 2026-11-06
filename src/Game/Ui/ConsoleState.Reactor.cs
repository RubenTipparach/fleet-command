// ConsoleState.Reactor: the console's own state for the reactor package's pages (the battle console mockup's powerMode
// and repDrag): which mode the Power station shows, and the repair queue's row being dragged. Console state only; the
// battle's state is in Battle.
namespace FleetCommand.Game.Ui;

public sealed partial class ConsoleState
{
    /// <summary>The Power station's mode: "alloc" (Allocation), "reactor" or "warp".</summary>
    public string PowerMode = "alloc";
    /// <summary>The repair job whose row is being dragged, or null; its place when the drag began, and where on the row it
    /// was gripped (px from the row's top).</summary>
    public string? RepairDrag;
    public int RepairDragFrom;
    public float RepairDragDy;
}
