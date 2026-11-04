// ColumnBacking: what lies behind the left column's panels. Black for a skin whose panels carry their own frames (the
// Concord); the skin's one cast plate ("console.column", SkinStyle.Column) for a skin whose docked panels are wells
// formed into it (the Krai Warhost, mockup 8), so no black seam shows between the panels. It also stops pointer input
// reaching the 3D view through the gaps.
using FleetCommand.Game.Ui.Kit;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class ColumnBacking : Control
{
    /// <summary>The console's skin; set again when a scenario of the other side starts.</summary>
    public IKit Kit { get => _kit!; set { _kit = value; QueueRedraw(); } }
    private IKit? _kit;

    public ColumnBacking()
    {
        Name = "Column";
        MouseFilter = MouseFilterEnum.Stop;
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        var r = new Rect2(Vector2.Zero, Size);
        DrawRect(r, Colors.Black);
        if (_kit is { Style.Column: true }) _kit.Hero(this, "console.column", r);
    }
}
