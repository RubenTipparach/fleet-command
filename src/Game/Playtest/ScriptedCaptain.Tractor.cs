// ScriptedCaptain.Tractor: the playtest beat the tractor-and-craft package adds to the one captain: mine_at, which places
// a mine by transporter as a player does, with the Craft page's Place mine key and then a click in the view at a point on
// the battle plane (arg "dx,dz": metres east and south of the captain's ship, a point a player would pick in the lane).
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FleetCommand.Game.View;
using Godot;

namespace FleetCommand.Game.Playtest;

public sealed partial class ScriptedCaptain
{
    private async Task<string?> MineAt(string arg)
    {
        string[] p = arg.Split(',');
        if (p.Length != 2 || !double.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double dx) || !double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double dz))
            return $"mine_at needs \"dx,dz\" in metres, not '{arg}'";
        return await PlaceMine(dx, dz);
    }

    // A mine beamed to a point dx metres east and dz south of the captain's ship: Place mine, then a click in the view.
    private async Task<string?> PlaceMine(double dx, double dz)
    {
        int before = B.Mines.Count;
        // the Mines tab of the Transport station, then Place mine
        string? stuck = await Click("st.transport") ?? await Click("tr.tab.mines") ?? await Click("mn.place");
        if (stuck != null) return stuck;
        ViewOverlay? view = _sources().OfType<ViewOverlay>().FirstOrDefault();
        if (view?.ScreenOfPlane(B.Player.X + dx, B.Player.Z + dz) is not { } at) return "the point is not on screen";
        Viewport vp = GetViewport();
        vp.PushInput(new InputEventMouseMotion { Position = at, GlobalPosition = at }, true);
        await Frames(2);
        vp.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = at, GlobalPosition = at }, true);
        vp.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = at, GlobalPosition = at }, true);
        await Frames(3);
        return B.Mines.Count > before ? null : "no mine was placed";
    }
}
