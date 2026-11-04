// IControlSource: anything on the console that registers immediate-mode controls (the panels, the strips, the view's
// overlay, the menu). It reports the controls of its last frame drawn in screen coordinates, so the skin parity check can
// compare the two skins control for control, and the scripted captain can press a control the way a player does, by
// pointing at it.
using System.Collections.Generic;
using Godot;

namespace FleetCommand.Game.Ui;

public interface IControlSource
{
    /// <summary>The controls of the last frame drawn: id, screen rectangle, and whether pressing it does anything.</summary>
    IEnumerable<(string Id, Rect2 R, bool Live)> Controls();
    /// <summary>The rectangle the source draws in, in screen coordinates.</summary>
    Rect2 Screen { get; }
}
