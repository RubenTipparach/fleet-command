// ColumnBacking: what lies behind the left column's panels, and behind an expanded panel (the mockup's drawUI and
// consoleBacking). Black only for a flat skin whose panels carry their own frames on black (the Concord); a skin's one
// cast plate ("console.column", SkinStyle.Column) while its docked panels are wells formed into it (the Krai Warhost);
// otherwise the skin's rendered backing plate ("console.backing", SkinStyle.Backing), so no black shows behind a metallic
// skin's panels (CLAUDE.md section 7; owner, 2026-11-04: "Black background is forbidden for metallic panel based
// factions"). Wider than the column (an expanded panel), the backing is laid in bands of its plain middle, every other one
// mirrored so the light runs on across each seam. It also stops pointer input reaching the 3D view through the gaps.
using FleetCommand.Game.Ui.Kit;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class ColumnBacking : Control
{
    /// <summary>The backing plate's plain band, in its 480 x 1080 reference pixels: clear of its cut corners, bolts, edge
    /// grooves and marks (the mockup's consoleBacking: x 100-390, y 40-1040).</summary>
    private static readonly Rect2 Band = new(100, 40, 290, 1000);

    /// <summary>The console's skin; set again when command passes to a ship of another faction.</summary>
    public IKit Kit { get => _kit!; set { _kit = value; QueueRedraw(); } }
    private IKit? _kit;

    /// <summary>True while a panel is expanded over the area this backs.</summary>
    public bool Expanded { get => _expanded; set { if (_expanded != value) { _expanded = value; QueueRedraw(); } } }
    private bool _expanded;

    public ColumnBacking()
    {
        Name = "Column";
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true; // a band of the backing never runs on into the live view
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        ConsoleCache.RenderFor(this);
        var r = new Rect2(Vector2.Zero, Size);
        DrawRect(r, Colors.Black);
        if (_kit == null) return;
        SkinStyle s = _kit.Style;
        if (s.Column && !_expanded) { _kit.Hero(this, "console.column", r); return; }
        if (!s.Backing) return;
        // the plate whole behind the column; in bands behind anything wider
        if (Size.X <= 480 * Size.Y / 1080 + 0.5f) { _kit.Hero(this, "console.backing", new Rect2(0, 0, Size.X, Size.Y)); return; }
        float bw = Band.Size.X * Size.Y / 1080;
        for (int i = 0; i * bw < Size.X; i++)
            _kit.HeroPart(this, "console.backing", new Rect2(i * bw, 0, bw, Size.Y), Band, i % 2 == 1);
    }
}
