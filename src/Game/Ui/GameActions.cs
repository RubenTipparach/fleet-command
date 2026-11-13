// GameActions: every order the console, the keyboard and the scripted captain can give, by name (CLAUDE.md section 6.2:
// input is actions). Keys reach these through Godot's InputMap (project.godot [input]); keys, buttons, taps and clicks
// all call the same methods, which pass the battle's orders straight to Battle (one implementation) or change the
// console's own state.
using System;
using System.Collections.Generic;
using FleetCommand.ConsoleModel;
using FleetCommand.Sim;

namespace FleetCommand.Game.Ui;

public sealed class GameActions
{
    private readonly Func<ConsoleContext> _ctx;
    /// <summary>Set by the composition root: rebuilds the battle from its scenario.</summary>
    public Action? OnRestart;
    /// <summary>Starts the scenario with this id (the composition root rebuilds the battle and, for another side, the skin).</summary>
    public Action<string>? OnScene;
    /// <summary>Set by the composition root: re-lays out the console after a change of the expanded panel.</summary>
    public Action? OnLayout;
    public Action? OnQuit;
    /// <summary>Set by the game flow: saves and goes back to the main menu (the campaign map's menu).</summary>
    public Action? OnMainMenu;
    /// <summary>Set by the composition root: the planet view on the selected planet, or back to the ship (BattleView).</summary>
    public Action? OnPlanetView;

    public GameActions(Func<ConsoleContext> ctx) { _ctx = ctx; }

    private Battle B => _ctx().Battle;
    private ConsoleState S => _ctx().State;

    /// <summary>InputMap action names and what each does (project.godot [input] binds keys to them).</summary>
    public IReadOnlyDictionary<string, Action> Bindings => new Dictionary<string, Action>
    {
        ["helm_port_15"] = () => B.Turn(-15), ["helm_starboard_15"] = () => B.Turn(15),
        ["helm_port_45"] = () => B.Turn(-45), ["helm_starboard_45"] = () => B.Turn(45),
        ["speed_up"] = () => B.SpeedStep(1), ["speed_down"] = () => B.SpeedStep(-1), ["all_stop"] = () => B.AllStop(),
        ["fire"] = () => B.Fire(), ["next_target"] = () => B.NextTarget(), ["toggle_volley"] = () => B.ToggleVolley(), ["toggle_auto"] = () => B.ToggleAutoFire(),
        ["planet_view"] = PlanetView, ["pause"] = Pause, ["back"] = Back, ["toggle_fullscreen"] = Fullscreen,
        ["command_1"] = () => Command(1), ["command_2"] = () => Command(2), ["command_3"] = () => Command(3),
        ["command_4"] = () => Command(4), ["command_5"] = () => Command(5), ["command_6"] = () => Command(6),
        ["command_7"] = () => Command(7), ["command_8"] = () => Command(8), ["command_9"] = () => Command(9),
    };

    /// <summary>Keys 1 to 9: take command of the n-th living ship of your fleet, in the scenario's order (the mockup's
    /// takeFleet); a number past the fleet does nothing. The console then follows the ship (Main.FollowCommand).</summary>
    public void Command(int n)
    {
        Ship[] fleet = B.Fleet();
        if (n >= 1 && n <= fleet.Length) B.TakeCommand(fleet[n - 1].Id);
    }

    /// <summary>G, or the view's Planet view key: the camera to the selected planet and down to it, or back to the ship.</summary>
    public void PlanetView() => OnPlanetView?.Invoke();

    public void Station(string id) => S.Station = id;
    public void ShipPage(string id) => S.ShipPage = id;
    /// <summary>Pause: alone, the battle; in co-op only the admiral pauses (the crew's console follows the admiral's).</summary>
    public void Pause() { if (_ctx().Coop is { IsHost: false }) return; S.Paused = !S.Paused; }
    public void Menu() { S.MenuOpen = !S.MenuOpen; }

    /// <summary>Expands a panel, or returns it when it is already expanded.</summary>
    public void Expand(Region r)
    {
        S.Expanded = S.Expanded == r ? null : r;
        OnLayout?.Invoke();
    }

    public void Return() { if (S.Expanded != null) { S.Expanded = null; OnLayout?.Invoke(); } }

    /// <summary>Opens or closes the Options sheet (the volumes and the window mode).</summary>
    public void Options() { S.OptionsOpen = !S.OptionsOpen; }

    /// <summary>Esc: closes the Options sheet, else the menu, else returns an expanded panel, else opens the menu.</summary>
    public void Back()
    {
        if (S.OptionsOpen) S.OptionsOpen = false;
        else if (S.MenuOpen) S.MenuOpen = false;
        else if (S.Expanded != null) Return();
        else S.MenuOpen = true;
    }

    public void Restart() { S.MenuOpen = false; OnRestart?.Invoke(); }
    /// <summary>Plays the scenario with this id, from its start.</summary>
    public void Scene(string id) { S.SceneOpen = false; S.MenuOpen = false; OnScene?.Invoke(id); }
    public void Quit() => OnQuit?.Invoke();
    /// <summary>Shuts the menu and goes back to the main menu.</summary>
    public void MainMenu() { S.MenuOpen = false; OnMainMenu?.Invoke(); }
    /// <summary>Borderless fullscreen or a window (DisplaySettings), remembered.</summary>
    public void Fullscreen() { DisplaySettings.Toggle(); OnLayout?.Invoke(); }
}
