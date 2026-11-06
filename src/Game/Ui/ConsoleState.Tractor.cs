// ConsoleState.Tractor: the console's own state for the tractor-and-craft package: whether the next click in the view
// places a mine by transporter (the mockup's placingMine). Battle state lives in the simulation; this is only the console.
namespace FleetCommand.Game.Ui;

public sealed partial class ConsoleState
{
    /// <summary>Place mine is armed: the next left click in the view beams a mine there.</summary>
    public bool PlacingMine;
}
