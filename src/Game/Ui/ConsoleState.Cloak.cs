// ConsoleState.Cloak: the console's own state for the Security page (the battle console mockup's secSel, secMode and
// secYaw): the team picked to send, whether it walks or is beamed, and how far the deck plan is turned. Console state,
// not the battle's: the battle only hears the order when a room is clicked (Battle.SecurityGo).
namespace FleetCommand.Game.Ui;

public sealed partial class ConsoleState
{
    /// <summary>The security team picked on the Security page.</summary>
    public string SecSel = "T1";
    /// <summary>Beam the picked team to the room clicked (true), or walk it there.</summary>
    public bool SecBeam;
    /// <summary>The deck plan's turn round its middle, radians (dragged on the view).</summary>
    public float SecYaw = 1.15f;
}
