// IconAtlas: every kit's icons drawn once into one texture and drawn from it as textured rectangles (the owner,
// 2026-11-12: "do icon atlas next, get it down to less than 300 draw calls"; openspec/changes/console-draw-cost D4). An icon
// is several vector fills, lines and arcs, and Godot draws each of those as a draw call of its own; half the console's
// draw calls on a Raspberry Pi 5 were icons. Here a kit's own icon drawer (the one source of how the icon looks) paints it,
// at the exact pixel size the console shows it and in its exact ink, into a cell of an offscreen viewport, which renders
// once each time cells are added; the console then draws the cell as one textured rectangle, and a run of icons (a rail of
// tabs) is one batch. Until a new cell has rendered the icon is drawn as vector, as before, so nothing ever shows blank.
using System;
using System.Collections.Generic;
using FleetCommand.Game.Ui;
using Godot;

namespace FleetCommand.Game.Ui.Kit;

public static partial class IconAtlas
{
    private const int AtlasPx = 1024;
    // clear border round an icon's square, as a share of its size: a stroke may run a little past the square
    private const float Margin = 0.25f;

    private sealed record Cell(string Kit, string Icon, Color Ink, int Px, float K, Rect2 Src, Action<CanvasItem, Rect2, Color> Draw) { public ulong ReadyFrame; }

    private static readonly Dictionary<(string, string, Color, int, int), Cell> Cells = new();
    private static SubViewport? _vp;
    private static Painter? _painter;
    private static int _cx, _cy, _rowH;

    /// <summary>Draws a kit's icon (<paramref name="draw"/>, the kit's pen drawer for it) in a square, from the atlas once
    /// its cell has rendered.</summary>
    public static void Draw(string kit, CanvasItem c, string icon, Rect2 r, Color ink, Action<Pen> draw) =>
        Draw(kit, c, icon, r, ink, (ci, rr, col) => draw(new Pen(ci, rr.Position, rr.Size.X / 48f, col)));

    /// <summary>Draws any small fixed drawing (an icon, a weapon's glyph) in a square r of c, from the atlas once its cell
    /// has rendered: <paramref name="draw"/> paints it into a square in the same units as r, in a colour.</summary>
    public static void Draw(string kit, CanvasItem c, string icon, Rect2 r, Color ink, Action<CanvasItem, Rect2, Color> draw)
    {
        // the square's size in the pixels it lands on (the console's cache renders at the window's pixels), and that scale,
        // which a drawing's fixed line widths are drawn at
        Transform2D toPx = c.GetViewport().GetFinalTransform() * c.GetGlobalTransformWithCanvas();
        float k = toPx.Scale.X;
        int px = (int)MathF.Round(r.Size.X * k);
        if (px < 4 || px > 128 || !EnsureHost(c))
        {
            draw(c, r, ink);
            return;
        }
        var opaque = new Color(ink.R, ink.G, ink.B);
        var key = (kit, icon, opaque, px, (int)MathF.Round(k * 100));
        if (!Cells.TryGetValue(key, out Cell? cell))
        {
            cell = Add(key, k, draw);
            if (cell == null) { draw(c, r, ink); return; }
        }
        if (cell.ReadyFrame == 0 || (ulong)Engine.GetFramesDrawn() < cell.ReadyFrame)
        {
            draw(c, r, ink);
            return;
        }
        // snapped to whole pixels, so the cell's pixels land one to one on the screen's
        Vector2 p0 = toPx * r.Position;
        p0 = new Vector2(MathF.Round(p0.X), MathF.Round(p0.Y));
        float m = (cell.Src.Size.X - px) / 2;
        var dest = new Rect2(toPx.AffineInverse() * (p0 - new Vector2(m, m)), cell.Src.Size / k);
        Draw2D.Flush(c);   // drawn over the untextured shapes before it
        RenderingServer.CanvasItemAddTextureRectRegion(c.GetCanvasItem(), dest, _vp!.GetTexture().GetRid(), cell.Src, new Color(1, 1, 1, ink.A));
    }

    // the offscreen viewport the cells are painted in, under the tree's root
    private static bool EnsureHost(CanvasItem c)
    {
        if (_vp != null && GodotObject.IsInstanceValid(_vp)) return true;
        if (c.GetTree()?.Root is not Window root) return false;
        _vp = new SubViewport
        {
            Name = "IconAtlas", Size = new Vector2I(AtlasPx, AtlasPx), TransparentBg = true, Disable3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled, RenderTargetClearMode = SubViewport.ClearMode.Always,
        };
        _painter = new Painter { Name = "Painter" };
        _vp.AddChild(_painter);
        root.CallDeferred(Node.MethodName.AddChild, _vp);
        Cells.Clear();
        _cx = _cy = _rowH = 0;
        return false;
    }

    private static Cell? Add((string Kit, string Icon, Color Ink, int Px, int K100) key, float k, Action<CanvasItem, Rect2, Color> draw)
    {
        int px = key.Px, size = px + 2 * (int)MathF.Ceiling(px * Margin);
        if (_cx + size > AtlasPx) { _cx = 0; _cy += _rowH + 1; _rowH = 0; }
        if (_cy + size > AtlasPx) return null;   // full: drawn as vector from here on (a few hundred fit)
        var cell = new Cell(key.Kit, key.Icon, key.Ink, px, k, new Rect2(_cx, _cy, size, size), draw);
        _cx += size + 1;
        _rowH = Math.Max(_rowH, size);
        Cells[key] = cell;
        // painted on the next render, which is shown the frame after
        cell.ReadyFrame = (ulong)Engine.GetFramesDrawn() + 2;
        _painter!.QueueRedraw();
        _vp!.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        return cell;
    }

    // paints every cell, each under the console's own scale (the viewport clears on each render, which happens only when
    // cells are added)
    private sealed partial class Painter : Node2D
    {
        public override void _Draw()
        {
            foreach (Cell cell in Cells.Values)
            {
                float m = (cell.Src.Size.X - cell.Px) / 2;
                Draw2D.SetTransform(this, cell.Src.Position + new Vector2(m, m), 0, new Vector2(cell.K, cell.K));
                float side = cell.Px / cell.K;
                cell.Draw(this, new Rect2(0, 0, side, side), cell.Ink);
            }
            Draw2D.SetTransform(this, Vector2.Zero, 0, Vector2.One);
            Draw2D.Flush(this);
        }
    }
}
