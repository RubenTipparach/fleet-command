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
    /// <summary>Set by the composition root: re-lays out the console after a change of the expanded panel.</summary>
    public Action? OnLayout;
    public Action? OnQuit;

    public GameActions(Func<ConsoleContext> ctx) { _ctx = ctx; }

    private Battle B => _ctx().Battle;
    private ConsoleState S => _ctx().State;

    /// <summary>InputMap action names and what each does (project.godot [input] binds keys to them).</summary>
    public IReadOnlyDictionary<string, Action> Bindings => new Dictionary<string, Action>
    {
        ["helm_port_15"] = () => B.Turn(-15), ["helm_starboard_15"] = () => B.Turn(15),
        ["helm_port_45"] = () => B.Turn(-45), ["helm_starboard_45"] = () => B.Turn(45),
        ["speed_up"] = () => B.SpeedStep(1), ["speed_down"] = () => B.SpeedStep(-1), ["all_stop"] = () => B.AllStop(),
        ["fire"] = () => B.Fire(), ["next_target"] = () => B.NextTarget(), ["toggle_volley"] = () => B.ToggleVolley(),
        ["pause"] = Pause, ["back"] = Back,
    };

    public void Station(string id) => S.Station = id;
    public void ShipPage(string id) => S.ShipPage = id;
    public void Pause() => S.Paused = !S.Paused;
    public void Menu() { S.MenuOpen = !S.MenuOpen; }

    /// <summary>Expands a panel, or returns it when it is already expanded.</summary>
    public void Expand(Region r)
    {
        S.Expanded = S.Expanded == r ? null : r;
        OnLayout?.Invoke();
    }

    public void Return() { if (S.Expanded != null) { S.Expanded = null; OnLayout?.Invoke(); } }

    /// <summary>Esc: closes the menu, else returns an expanded panel, else opens the menu.</summary>
    public void Back()
    {
        if (S.MenuOpen) S.MenuOpen = false;
        else if (S.Expanded != null) Return();
        else S.MenuOpen = true;
    }

    public void Restart() { S.MenuOpen = false; OnRestart?.Invoke(); }
    public void Quit() => OnQuit?.Invoke();
}
