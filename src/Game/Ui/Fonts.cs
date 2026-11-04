// Fonts and Text: the console's type at fixed sizes, and text that is trimmed with an ellipsis to its room and never
// reflows anything (CLAUDE.md section 7: panels are a fixed size, and their content never changes it). The console asks
// for Antonio's sizes and weights (the Concord's type as written); the skin's type set maps them onto its own faces (the
// Krai's Teko and Russo One, sized to Antonio's width), so every slot that fits a Concord label fits the skin's. Lines
// are shaped once and cached by their text, face, spacing and room.
using System.Collections.Generic;
using FleetCommand.Game.Ui.Kit;
using Godot;

namespace FleetCommand.Game.Ui;

public sealed class Fonts
{
    private readonly Dictionary<string, FontFile> _files = new();
    private readonly Dictionary<(Face, int), FontVariation> _vars = new();
    private readonly Dictionary<(string, Face, int, int), TextLine> _lines = new();
    private TypeSet _type;

    /// <summary>The console's type in one face (<paramref name="path"/>, a variable font), until a skin's type is used.</summary>
    public Fonts(string path)
    {
        Load(path);
        _type = new TypeSet(path, path, (size, weight) => new Face(false, size, weight));
    }

    /// <summary>Draws every line from now on in the skin's type.</summary>
    public void Use(TypeSet type)
    {
        Load(type.Body);
        Load(type.Heading);
        _type = type;
        _vars.Clear();
        _lines.Clear();
    }

    private FontFile Load(string path)
    {
        if (_files.TryGetValue(path, out FontFile? f)) return f;
        f = GD.Load<FontFile>(path) ?? throw new System.InvalidOperationException($"font not found: {path}");
        _files[path] = f;
        return f;
    }

    /// <summary>The skin's face for a size (pixels) and weight (100 to 700) the console asks for.</summary>
    public Face Map(int size, int weight) => _type.Map(size, weight);

    /// <summary>A face at its weight with extra glyph spacing in pixels.</summary>
    private Font FontOf(Face face, int spacing)
    {
        if (_vars.TryGetValue((face, spacing), out FontVariation? v)) return v;
        long wght = TextServerManager.GetPrimaryInterface().NameToTag("wght");
        v = new FontVariation
        {
            BaseFont = Load(face.Heading ? _type.Heading : _type.Body), SpacingGlyph = spacing,
            VariationOpentype = new Godot.Collections.Dictionary { { wght, face.Weight } },
        };
        _vars[(face, spacing)] = v;
        return v;
    }

    /// <summary>A shaped line in the skin's face for the size and weight asked, trimmed to <paramref name="max"/> pixels
    /// with an ellipsis when max is above 0.</summary>
    public TextLine Line(string s, int size, int weight, int spacing, float max) => Line(s, Map(size, weight), spacing, max);

    /// <summary>A shaped line in exactly <paramref name="face"/>, trimmed as <see cref="Line(string, int, int, int, float)"/>.</summary>
    public TextLine Line(string s, Face face, int spacing, float max)
    {
        var key = (s, face, spacing, (int)max);
        if (_lines.TryGetValue(key, out TextLine? line)) return line;
        if (_lines.Count > 4000) _lines.Clear();
        line = new TextLine();
        line.AddString(s, FontOf(face, spacing), face.Size);
        if (max > 0) { line.Width = max; line.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis; }
        _lines[key] = line;
        return line;
    }

    /// <summary>The width of a string as drawn, untrimmed.</summary>
    public float Width(string s, int size, int weight = 600, int spacing = 0) => Line(s, size, weight, spacing, 0).GetLineWidth();

    /// <summary>Draws a line with its baseline at y; x is its left, centre or right by <paramref name="align"/>.</summary>
    public void Draw(CanvasItem c, string s, float x, float y, int size, Color col, HorizontalAlignment align = HorizontalAlignment.Left,
        float max = 0, int weight = 600, int spacing = 0) => Draw(c, s, x, y, Map(size, weight), col, align, max, spacing);

    /// <summary>Draws a line in exactly <paramref name="face"/> (the name plate's own face), baseline at y.</summary>
    public void Draw(CanvasItem c, string s, float x, float y, Face face, Color col, HorizontalAlignment align = HorizontalAlignment.Left,
        float max = 0, int spacing = 0)
    {
        if (string.IsNullOrEmpty(s)) return;
        TextLine line = Line(s, face, spacing, max);
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
