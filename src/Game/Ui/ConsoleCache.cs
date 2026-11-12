// ConsoleCache: the console's panels and column drawn into their own viewport, which renders only on a frame where one
// of them redrew; every other frame the console is the last render, one textured rectangle (the owner, 2026-11-12: "I
// expect ui draw calls to be pretty small if we do this right"; openspec/changes/console-draw-cost D3). Godot replays a
// canvas item's drawing every frame even when it is not redrawn, so the panels cost their draw calls on every frame until
// they are cached like this. A panel asks for a render when it queues a redraw (ConsolePanel), the column when it
// changes, and the console when it is laid out again. The container passes input through to the panels, which sit at
// the window's origin and scale, so their controls' screen rectangles are what they were. The viewport renders at the
// window's own pixels (its 2D size overridden to the console's layout size), so text and edges are drawn at the screen's
// resolution as they are on the main canvas, not drawn at layout size and rescaled (which broke thin letters at phone
// sizes). The render is composited with
// premultiplied alpha: the viewport holds colour already multiplied by its alpha, and blending it again would darken
// every antialiased edge and see-through fill.
using System;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed partial class ConsoleCache : SubViewportContainer
{
    private readonly SubViewport _vp;

    /// <summary>The parent of the cached panels and column, the window's size.</summary>
    public Control Panels { get; }

    public ConsoleCache()
    {
        Name = "ConsoleCache";
        Stretch = false;
        MouseFilter = MouseFilterEnum.Pass;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha };
        _vp = new SubViewport
        {
            Name = "Console", TransparentBg = true, HandleInputLocally = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once, RenderTargetClearMode = SubViewport.ClearMode.Always,
        };
        AddChild(_vp);
        Panels = new Control { Name = "Panels", MouseFilter = MouseFilterEnum.Ignore };
        _vp.AddChild(Panels);
        Panels.ChildEnteredTree += _ => Render();
    }

    /// <summary>Fills the window (<paramref name="size"/>, in the console's layout units) and renders once.</summary>
    public void Place(Vector2 size)
    {
        Vector2I px = GetWindow().Size;
        if (px.X <= 0 || px.Y <= 0 || size.X <= 0 || size.Y <= 0) return;
        _vp.Size = px;
        _vp.Size2DOverride = new Vector2I((int)MathF.Round(size.X), (int)MathF.Round(size.Y));
        _vp.Size2DOverrideStretch = true;
        Position = Vector2.Zero;
        Size = px;
        Scale = size / px;   // the window's pixels back to layout units
        Panels.Position = Vector2.Zero;
        Panels.Size = size;
        Render();
    }

    /// <summary>Renders the cached console on this frame (whatever redrew in it is drawn this frame too).</summary>
    public void Render() => _vp.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;

    /// <summary>Asks the cache holding <paramref name="n"/>, if any, to render this frame.</summary>
    public static void RenderFor(Node n)
    {
        if (n.GetViewport() is SubViewport vp && vp.GetParent() is ConsoleCache c) c.Render();
    }
}
