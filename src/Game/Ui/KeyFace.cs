// KeyFace: the face of a key the console draws itself (the helm keys, slider handles, the orbit switch, the systems
// display's status boxes) and the ink that reads on it: one implementation for every such key (the battle console
// mockup's face, facePiece and faceInk; openspec/changes/krai-battle-console, decision 5). A skin with rendered keys shows
// the key's state as the light in its glass ("key.idle" ember, "key.hover", "key.on", "key.ok", "key.warn", "key.alert",
// "key.off" unlit), a key much wider than tall in the nine-sliced "keyw." render so its slanted ends keep their shape.
// Any other skin fills its own key shape (the kit's Shape) with the colour.
using System;
using FleetCommand.Game.Ui.Kit;
using Godot;

namespace FleetCommand.Game.Ui;

public static class KeyFace
{
    /// <summary>The rendered key state for a fill colour. <paramref name="status"/>: a status box (its colour is a lamp:
    /// whole, damaged, down), rather than a control (its colour is at rest, pointed at, or engaged).</summary>
    public static string StateOf(Palette c, Color fill, bool status)
    {
        if (status) return fill == c.Accord ? "ok" : fill == c.Caution ? "warn" : fill == c.Alert ? "alert" : "off";
        return fill == c.Signal ? "idle" : fill == c.Sky ? "hover" : fill == c.Lilac || fill == c.Accord ? "on"
            : fill == c.Caution ? "warn" : fill == c.Alert ? "alert" : "off";
    }

    /// <summary>A key's face at a rectangle; <paramref name="radius"/> is the rounding of a drawn key.</summary>
    public static void Draw(IKit kit, CanvasItem ci, Rect2 r, float radius, Color fill, bool status = false, float alpha = 1)
    {
        if (!kit.Style.RenderedKeys) { kit.Shape(ci, r, radius, new Color(fill, fill.A * alpha)); return; }
        string st = StateOf(kit.C, fill, status);
        if (r.Size.X > r.Size.Y * 1.4f) kit.Piece(ci, "keyw." + st, r, alpha);
        else kit.Hero(ci, "key." + st, r, alpha);
    }

    /// <summary>The ink that reads on a key's face: on rendered glass, the skin's glyph colour on dark glass and its ink
    /// on lit glass; on a drawn key, the ink for the fill.</summary>
    public static Color Ink(IKit kit, Color fill, bool status = false)
    {
        if (!kit.Style.RenderedKeys) return kit.Style.InkOn(fill);
        string st = StateOf(kit.C, fill, status);
        return st is "idle" or "off" ? kit.Style.GlyphOnDark : kit.Style.R.Ink;
    }

    /// <summary>A sheet's close key: the skin's knob with an X in the ink that reads on it (the event log's, the hit
    /// details', the mission goals').</summary>
    public static void Close(IKit kit, CanvasItem ci, Rect2 r, bool hot)
    {
        Color fill = hot ? kit.C.Alert : kit.C.Signal, ink = kit.Style.InkOn(fill);
        Vector2 c = r.GetCenter();
        float h = Math.Min(r.Size.X, r.Size.Y) * 0.2f;
        kit.Knob(ci, c, Math.Min(r.Size.X, r.Size.Y) / 2, fill);
        Draw2D.Line(ci, c - new Vector2(h, h), c + new Vector2(h, h), ink, 2.6f, true);
        Draw2D.Line(ci, c + new Vector2(h, -h), c - new Vector2(h, -h), ink, 2.6f, true);
    }
}
