// ConsoleContext: what every console panel shares, wired once by the composition root (Main): the battle, the skin,
// the fonts (in the skin's type), the actions, the hover card and the console's own state (the open station and ship
// page, the expanded panel, pause and the menu). Panels read it and call its actions; they hold no rules of their own.
using FleetCommand.ConsoleModel;
using FleetCommand.Data;
using FleetCommand.Game.Ui.Kit;
using FleetCommand.Sim;

namespace FleetCommand.Game.Ui;

public sealed class ConsoleState
{
    public string Station = "helm";
    public string ShipPage = "systems";
    public Region? Expanded;
    public bool Paused;
    public bool MenuOpen;
    /// <summary>The helm's free-angle high energy turn is waiting for a click in the view.</summary>
    public bool PlacingHet;
    /// <summary>The view's Target key: shows or hides the Target box (the locked target's systems display).</summary>
    public bool ShowTarget = true;
}

public sealed class ConsoleContext
{
    private IKit? _kit;
    private Fonts? _fonts;

    public required GameData Data { get; init; }
    public required IKit Kit { get => _kit!; init { _kit = value; UseSkinType(); } }
    /// <summary>The console's type, drawing in the skin's faces (wired here, once both are given).</summary>
    public required Fonts Fonts { get => _fonts!; init { _fonts = value; UseSkinType(); } }
    public required TipBoard Tips { get; init; }
    public required ConsoleState State { get; init; }
    public required GameActions Act { get; init; }
    /// <summary>The battle being fought; replaced on a restart.</summary>
    public required Battle Battle { get; set; }

    // The skin's type is the console's type: every panel's text is drawn in the skin's faces.
    private void UseSkinType()
    {
        if (_kit != null && _fonts != null) _fonts.Use(_kit.Style.Type);
    }
}
