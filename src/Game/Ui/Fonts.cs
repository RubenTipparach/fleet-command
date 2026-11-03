// Fonts and Text: the console's one typeface (Antonio, OFL, assets/fonts) at fixed sizes, and text that is trimmed with
// an ellipsis to its room and never reflows anything (CLAUDE.md section 7: panels are a fixed size, and their content
// never changes it). Lines are shaped once and cached by their text, size, weight, spacing and room.
using System.Collections.Generic;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed class Fonts
{
    private readonly FontFile _file;
    private readonly Dictionary<(int, int), FontVariation> _vars = new();
    private readonly Dictionary<(string, int, int, int, int), TextLine> _lines = new();

    public Fonts(string path)
    {
        _file = GD.Load<FontFile>(path) ?? throw new System.InvalidOperationException($"font not found: {path}");
    }

    /// <summary>The face at a weight (100 to 700) with extra glyph spacing in pixels.</summary>
    public Font Face(int weight, int spacing)
    {
        if (_vars.TryGetValue((weight, spacing), out FontVariation? v)) return v;
        long wght = TextServerManager.GetPrimaryInterface().NameToTag("wght");
        v = new FontVariation { BaseFont = _file, SpacingGlyph = spacing, VariationOpentype = new Godot.Collections.Dictionary { { wght, weight } } };
        _vars[(weight, spacing)] = v;
        return v;
    }

    /// <summary>A shaped line, trimmed to <paramref name="max"/> pixels with an ellipsis when max is above 0.</summary>
    public TextLine Line(string s, int size, int weight, int spacing, float max)
    {
        var key = (s, size, weight, spacing, (int)max);
        if (_lines.TryGetValue(key, out TextLine? line)) return line;
        if (_lines.Count > 4000) _lines.Clear();
        line = new TextLine();
        line.AddString(s, Face(weight, spacing), size);
        if (max > 0) { line.Width = max; line.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis; }
        _lines[key] = line;
        return line;
    }

    /// <summary>The width of a string as drawn, untrimmed.</summary>
    public float Width(string s, int size, int weight = 600, int spacing = 0) => Line(s, size, weight, spacing, 0).GetLineWidth();

    /// <summary>Draws a line with its baseline at y; x is its left, centre or right by <paramref name="align"/>.</summary>
    public void Draw(CanvasItem c, string s, float x, float y, int size, Color col, HorizontalAlignment align = HorizontalAlignment.Left,
        float max = 0, int weight = 600, int spacing = 0)
    {
        if (string.IsNullOrEmpty(s)) return;
        TextLine line = Line(s, size, weight, spacing, max);
        float w = max > 0 ? System.Math.Min(line.GetLineWidth(), max) : line.GetLineWidth();
        float left = align switch { HorizontalAlignment.Center => x - w / 2, HorizontalAlignment.Right => x - w, _ => x };
        line.Draw(c.GetCanvasItem(), new Vector2(left, y - line.GetLineAscent()), col);
    }

    /// <summary>A line in several colours, upper case, left to right from x, cut off at max pixels.</summary>
    public void DrawRich(CanvasItem c, (string Text, Color Col)[] segs, float x, float y, int size, float max)
    {
        float cx = x;
        foreach (var (t, col) in segs)
        {
            string u = t.ToUpperInvariant();
            float room = max - (cx - x);
            if (room <= 8) return;
            Draw(c, u, cx, y, size, col, HorizontalAlignment.Left, room, 600, 1);
            cx += Width(u, size, 600, 1);
        }
    }

    /// <summary>Draws a line with a dark outline, so it reads over both a bright and a dark fill.</summary>
    public void DrawOutlined(CanvasItem c, string s, float x, float y, int size, Color col, Color outline, int outlinePx, HorizontalAlignment align = HorizontalAlignment.Center)
    {
        TextLine line = Line(s, size, 700, 0, 0);
        float w = line.GetLineWidth();
        float left = align switch { HorizontalAlignment.Center => x - w / 2, HorizontalAlignment.Right => x - w, _ => x };
        var pos = new Vector2(left, y - line.GetLineAscent());
        line.DrawOutline(c.GetCanvasItem(), pos, outlinePx, outline);
        line.Draw(c.GetCanvasItem(), pos, col);
    }
}
