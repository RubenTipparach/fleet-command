// MapScreen: the campaign map screen. The 3D map (CampaignMap) renders in its own world inside a SubViewport filling the
// screen, and the map's HUD (MapHud: the top bar, the sector panel, the briefing and the port) is drawn over it. A press the
// HUD does not take falls through to here: a click picks a sector, a drag pans, the wheel zooms; the view never turns.
// The camera is the player's alone (CLAUDE.md section 8).
using System.Collections.Generic;
using FleetCommand.Campaign;
using Godot;

namespace FleetCommand.Game.Flow;

public sealed partial class MapScreen : Control
{
    private GameFlow _f = null!;
    private SubViewportContainer _box = null!;
    private SubViewport _vp = null!;
    private MapHud _hud = null!;
    public MapHud Hud => _hud;
    private PortPanel _port = null!;
    public CampaignMap? Map { get; private set; }
    /// <summary>The HUD and the port, the map screen's panels of controls.</summary>
    public IEnumerable<Control> Panels => new Control[] { _hud, _port };
    private Vector2 _press;
    private bool _dragging, _left;
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
        _vp = new SubViewport { OwnWorld3D = true, Msaa3D = LowSpec.Msaa(Viewport.Msaa.Msaa4X), HandleInputLocally = false, Name = "MapWorld" };
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
        Map = new CampaignMap { ClassModel = cls => _f.X.Data.Classes.TryGetValue(cls, out var k) ? k.Model : null };
        _vp.AddChild(Map);
        Map.Init(c);
        Map.Select(_f.Sel);
    }

    // The port covers the whole screen, so it is shown only while a port is open: shut but shown, it took every press meant
    // for the HUD under it (Move here, End turn, Briefing) and passed it on to the map, which picked a sector instead.
    public override void _Process(double delta)
    {
        _port.Visible = _f.Port != null && _f.Camp != null;
        LowSpec.FitToWindow(_vp);
    }

    public void Refresh() { Map?.Rebuild(); Map?.Select(_f.Sel); Map?.ShowRoute(_f.Path, _f.Course); }

    public override void _GuiInput(InputEvent e)
    {
        if (Map == null) return;
        switch (e)
        {
            case InputEventMouseButton b when b.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown && b.Pressed:
                Map.Zoom(b.ButtonIndex == MouseButton.WheelUp ? 0.9f : 1.11f);
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
            case InputEventMouseMotion m:
                Mouse = m.Position;
                if (_left && (m.Position - _press).Length() > 6) _dragging = true;
                if (_left && _dragging) Map.Pan(m.Position - m.Relative, m.Position);
                HoverHex = Map.Pick(m.Position);
                Map.Hover(HoverHex);
                break;
            case InputEventScreenDrag d:
                Map.Pan(d.Position - d.Relative, d.Position);
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

    /// <summary>The player asked to see their fleet: the view glides to it (their order, not the map's).</summary>
    public void CentreOnFleet()
    {
        if (_f.Camp == null || Map == null) return;
        Map.GlideTo(_f.Camp.S.AtC, _f.Camp.S.AtR);
    }
}
