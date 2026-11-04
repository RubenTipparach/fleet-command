// RenderedPieces: a skin's kit pieces rendered in Blender (CLAUDE.md section 10), loaded from the manifest its generator
// writes (assets/ui/<skin>/manifest.json, e.g. tools/blender/build_ui_krai.py) and drawn as the battle console mockup
// draws them (docs/mockups/battle-console.html nineCanvas and heroImg): a nine-slice piece is cut at its recorded
// margins, its corners kept whole, its edges and centre stretched; a hero piece or a cell is drawn whole. The renders
// are at the manifest's scale (2 device pixels per console pixel), so a slice's source is its margin times that scale.
// One implementation for every rendered skin; a skin's kit class adds what is its own (palette, icons, style).
using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace FleetCommand.Game.Ui.Kit;

public sealed class RenderedPieces
{
    private enum Kind { Nine, Hero, Cell }

    private sealed record Entry(Kind Kind, Texture2D Tex, PieceSpec Spec);

    private readonly Dictionary<string, Entry> _pieces = new();
    private readonly string _skin;

    /// <summary>Every piece the manifest lists.</summary>
    public IEnumerable<string> Names => _pieces.Keys;

    /// <summary>Loads res://assets/ui/<paramref name="skin"/>/manifest.json and every texture it names; a manifest that
    /// cannot be read, or a piece whose texture is missing, stops the skin with its path.</summary>
    public RenderedPieces(string skin)
    {
        _skin = skin;
        string dir = $"res://assets/ui/{skin}/", path = dir + "manifest.json";
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read)
            ?? throw new InvalidOperationException($"{path}: cannot be opened ({FileAccess.GetOpenError()})");
        using JsonDocument doc = JsonDocument.Parse(f.GetAsText());
        foreach (JsonProperty p in doc.RootElement.GetProperty("pieces").EnumerateObject())
        {
            JsonElement e = p.Value;
            string file = e.GetProperty("file").GetString() ?? throw new InvalidOperationException($"{path}: {p.Name} has no file");
            Texture2D tex = GD.Load<Texture2D>(dir + file) ?? throw new InvalidOperationException($"{path}: {p.Name}: {dir + file} is missing");
            Kind kind = e.GetProperty("kind").GetString() switch
            {
                "nine" => Kind.Nine, "hero" => Kind.Hero, "cell" => Kind.Cell,
                string k => throw new InvalidOperationException($"{path}: {p.Name}: unknown kind '{k}'"),
                null => throw new InvalidOperationException($"{path}: {p.Name}: no kind"),
            };
            var size = Vec(e.GetProperty("size"));
            var m = e.TryGetProperty("margins", out JsonElement mm) ? new Pad(mm[0].GetSingle(), mm[1].GetSingle(), mm[2].GetSingle(), mm[3].GetSingle()) : new Pad(0, 0, 0, 0);
            // the mockup's default inset for a nine-slice piece is 3 px each way
            Vector2 inset = e.TryGetProperty("inset", out JsonElement ins) ? Vec(ins) : new Vector2(3, 3);
            float gap = e.TryGetProperty("gap", out JsonElement g) ? g.GetSingle() : 2;
            _pieces[p.Name] = new Entry(kind, tex, new PieceSpec(size, m, inset, gap));
        }
    }

    private static Vector2 Vec(JsonElement a) => new(a[0].GetSingle(), a[1].GetSingle());

    public bool Has(string id) => _pieces.ContainsKey(id);

    public PieceSpec Spec(string id) => Get(id).Spec;

    private Entry Get(string id) =>
        _pieces.TryGetValue(id, out Entry? e) ? e : throw new KeyNotFoundException($"the {_skin} kit has no rendered piece '{id}'");

    /// <summary>Keys, tabs, cells and bar fills shrink their margins with their height, as the mockup's kit did.</summary>
    private static bool ShrinksWithHeight(string id) =>
        id.StartsWith("button.", StringComparison.Ordinal) || id.StartsWith("tab.", StringComparison.Ordinal) ||
        id.StartsWith("cell.", StringComparison.Ordinal) || id.StartsWith("bar.fill", StringComparison.Ordinal);

    /// <summary>Draws a piece at a rectangle: a nine-slice piece cut at its margins, any other whole.</summary>
    public void Draw(CanvasItem c, string id, Rect2 r, float alpha = 1)
    {
        Entry e = Get(id);
        if (r.Size.X <= 0 || r.Size.Y <= 0) return;
        var mod = new Color(1, 1, 1, alpha);
        if (e.Kind != Kind.Nine) { c.DrawTextureRect(e.Tex, r, false, mod); return; }
        PieceSpec s = e.Spec;
        float w = r.Size.X, h = r.Size.Y;
        float k = ShrinksWithHeight(id) ? Math.Min(1, h / s.Size.Y) : 1;
        k = Math.Min(k, Math.Min(w / Math.Max(1, s.M.L + s.M.R), h / Math.Max(1, s.M.T + s.M.B)));
        Vector2 ts = e.Tex.GetSize();
        float sk = ts.X / s.Size.X;
        float[] sx = { 0, s.M.L * sk, ts.X - s.M.R * sk, ts.X }, sy = { 0, s.M.T * sk, ts.Y - s.M.B * sk, ts.Y };
        float[] dx = { 0, s.M.L * k, w - s.M.R * k, w }, dy = { 0, s.M.T * k, h - s.M.B * k, h };
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
            {
                float sw = sx[j + 1] - sx[j], sh = sy[i + 1] - sy[i], dw = dx[j + 1] - dx[j], dh = dy[i + 1] - dy[i];
                if (sw <= 0 || sh <= 0 || dw <= 0 || dh <= 0) continue;
                c.DrawTextureRectRegion(e.Tex, new Rect2(r.Position.X + dx[j], r.Position.Y + dy[i], dw, dh), new Rect2(sx[j], sy[i], sw, sh), mod);
            }
    }

    /// <summary>Draws only the bottom <paramref name="frac"/> of a whole piece (a key lit from the bottom up as it charges).</summary>
    public void DrawBottom(CanvasItem c, string id, Rect2 r, float frac, float alpha = 1)
    {
        Entry e = Get(id);
        float f = Math.Clamp(frac, 0, 1);
        if (f <= 0) return;
        Vector2 ts = e.Tex.GetSize();
        c.DrawTextureRectRegion(e.Tex, new Rect2(r.Position.X, r.End.Y - r.Size.Y * f, r.Size.X, r.Size.Y * f), new Rect2(0, ts.Y * (1 - f), ts.X, ts.Y * f), new Color(1, 1, 1, alpha));
    }

    /// <summary>A new whole piece made from an existing one with its hue turned to <paramref name="hue"/>'s (the mockup's
    /// "hue" blend: the source keeps its saturation and brightness).</summary>
    public void AddHueTurned(string id, string from, Color hue)
    {
        Entry e = Get(from);
        Image img = e.Tex.GetImage() ?? throw new InvalidOperationException($"the {_skin} kit's '{from}' has no image to turn");
        img.Decompress();
        for (int y = 0; y < img.GetHeight(); y++)
            for (int x = 0; x < img.GetWidth(); x++)
            {
                Color p = img.GetPixel(x, y);
                img.SetPixel(x, y, Color.FromHsv(hue.H, p.S, p.V, p.A));
            }
        _pieces[id] = e with { Tex = ImageTexture.CreateFromImage(img) };
    }
}
