// ConsoleContext: what every console panel shares, wired once by the composition root (Main): the battle, the skin,
// the fonts (in the skin's type), the actions, the hover card and the console's own state (the open station and ship
// page, the expanded panel, pause and the menu). Panels read it and call its actions; they hold no rules of their own.
using FleetCommand.ConsoleModel;
using FleetCommand.Data;
using FleetCommand.Game.Ui.Kit;
using FleetCommand.Sim;

namespace FleetCommand.Game.Ui;

public sealed partial class ConsoleState
{
    /// <summary>The open station; a battle starts on Comms, its goals and log (owner, 2026-11-06: "Start game with coms panel
    /// open by default").</summary>
    public string Station = "comms";
    public string ShipPage = "systems";
    public Region? Expanded;
    public bool Paused;
    public bool MenuOpen;
    /// <summary>The Options sheet is open (from the main menu or the battle's menu), over everything.</summary>
    public bool OptionsOpen;
    /// <summary>The helm's free-angle high energy turn is waiting for a click in the view.</summary>
    public bool PlacingHet;
    /// <summary>The view's Target key: shows or hides the Target box (the locked target's systems display).</summary>
    public bool ShowTarget = true;
    /// <summary>The view's scene selector is open.</summary>
    public bool SceneOpen;
    /// <summary>The battle is a campaign mission's: the mission strip shows its objectives, and there is no scene selector.</summary>
    public bool InMission;
    /// <summary>The campaign map is the screen: its menu is the same sheet as the battle's, with Main menu in place of
    /// Restart or Withdraw (owner, 2026-11-06: "menu button in campaign map should be the pause menu").</summary>
    public bool OnMap;
    /// <summary>A ship's end is still playing in the view (its core breach, then its nova): the battle's end waits for it, so
    /// the last ship destroyed is seen to go (GameFlow).</summary>
    public bool ShipsEnding;
}

public sealed class ConsoleContext
{
    private IKit? _kit;
    private Fonts? _fonts;

    public required GameData Data { get; init; }
    /// <summary>The console skin: the player's faction's, swapped when a scenario of another side starts; the console's
    /// type follows it.</summary>
    public required IKit Kit { get => _kit!; set { _kit = value; UseSkinType(); } }
    /// <summary>The console's type, drawing in the skin's faces (wired here, once both are given).</summary>
    public required Fonts Fonts { get => _fonts!; init { _fonts = value; UseSkinType(); } }
    public required TipBoard Tips { get; init; }
    public required ConsoleState State { get; init; }
    public required GameActions Act { get; init; }
    /// <summary>How far through the next fixed step the frame is drawn, 0 to 1 (Main's accumulator over the step): shots in
    /// flight are drawn that share of the way from their last step's place (Battle.DrawnAt).</summary>
    public double StepShare = 1;
    /// <summary>The console's own sounds: its keys and the bridge bed (ConsoleSounds).</summary>
    public required ConsoleSounds Sounds { get; init; }
    /// <summary>The battle being fought; replaced on a restart.</summary>
    public required Battle Battle { get; set; }

    // The skin's type is the console's type: every panel's text is drawn in the skin's faces.
    private void UseSkinType()
    {
        if (_kit != null && _fonts != null) _fonts.Use(_kit.Style.Type);
    }
}
