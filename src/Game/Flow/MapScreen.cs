// MapScreen: the campaign map screen. The 3D map (CampaignMap) renders in its own world inside a SubViewport filling the
// screen, and the map's HUD (MapHud: the top bar, the sector panel, the briefing and the port) is drawn over it. A press the
// HUD does not take falls through to here: a click picks a sector, a left drag pans, a right drag turns, the wheel zooms.
// The camera is the player's alone (CLAUDE.md section 8).
using FleetCommand.Campaign;
using Godot;

namespace FleetCommand.Game.Flow;

public sealed partial class MapScreen : Control
{
    private GameFlow _f = null!;
    private SubViewportContainer _box = null!;
    private SubViewport _vp = null!;
    private MapHud _hud = null!;
    private PortPanel _port = null!;
    public CampaignMap? Map { get; private set; }
    private Vector2 _press;
    private bool _dragging, _left, _right;
    public (int C, int R)? HoverHex { get; private set; }
    public Vector2 Mouse { get; private set; }

    public void Init(GameFlow f)
    {
        _f = f;
        Name = "MapScreen";
        MouseFilter = MouseFilterEnum.Stop;
        _box = new SubViewportContainer { Stretch = true, MouseFilter = MouseFilterEnum.Ignore, Name = "View" };
        _box.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_box);
        _vp = new SubViewport { OwnWorld3D = true, Msaa3D = Viewport.Msaa.Msaa4X, HandleInputLocally = false, Name = "MapWorld" };
        _box.AddChild(_vp);
        _hud = new MapHud();
        _hud.Bind(f);
        _hud.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_hud);
        _port = new PortPanel();
        _port.Bind(f);
        _port.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_port);
    }

    /// <summary>Builds the 3D map for a campaign (a new one, or one loaded).</summary>
    public void NewMap(Campaign.Campaign c)
    {
        Map?.QueueFree();
        Map = new CampaignMap();
        _vp.AddChild(Map);
        Map.Init(c);
        Map.Select(_f.Sel);
    }

    public void Refresh() { Map?.Rebuild(); Map?.Select(_f.Sel); Map?.ShowRoute(_f.Path); }

    public override void _GuiInput(InputEvent e)
    {
        if (Map == null) return;
        switch (e)
        {
            case InputEventMouseButton b when b.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown && b.Pressed:
                Map.Dist *= b.ButtonIndex == MouseButton.WheelUp ? 0.9f : 1.11f;
                AcceptEvent();
                break;
            case InputEventMouseButton b when b.ButtonIndex == MouseButton.Left:
                if (b.Pressed) { _press = b.Position; _dragging = false; _left = true; }
                else
                {
                    if (_left && !_dragging) Click(b.Position);
                    _left = false;
                }
                AcceptEvent();
                break;
            case InputEventMouseButton b when b.ButtonIndex == MouseButton.Right:
                _right = b.Pressed;
                AcceptEvent();
                break;
            case InputEventMouseMotion m:
                Mouse = m.Position;
                if (_left && (m.Position - _press).Length() > 6) _dragging = true;
                if (_left && _dragging) Map.Pan(m.Relative);
                else if (_right) { Map.Yaw -= m.Relative.X * 0.006f; Map.Pitch += m.Relative.Y * 0.004f; }
                HoverHex = Map.Pick(m.Position);
                Map.Hover(HoverHex);
                break;
            case InputEventScreenDrag d:
                Map.Pan(d.Relative);
                AcceptEvent();
                break;
        }
    }

    private void Click(Vector2 at)
    {
        if (_f.Camp == null || Map == null) return;
        var hex = Map.Pick(at);
        if (hex == null) return;
        _f.Port = null;
        _f.SelectHex(hex);
    }

    /// <summary>The player asked to see their fleet: the camera's centre moves to it (their order, not the map's).</summary>
    public void CentreOnFleet()
    {
        if (_f.Camp == null || Map == null) return;
        Map.CentreOn(_f.Camp.S.AtC, _f.Camp.S.AtR);
    }
}
