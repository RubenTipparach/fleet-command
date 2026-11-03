// IKit: the console's UI kit, one fixed list of named pieces that every screen draws by name (CLAUDE.md section 7: "One
// UI kit, swapped per faction"). A skin is one implementation of this interface; swapping the skin swaps the look and
// changes no layout or code. KitPieces lists the names, and Verify refuses a skin that lacks one (a missing piece is a
// build error, never a fallback).
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FleetCommand.Game.Ui.Kit;

/// <summary>A skin's colours, each with one meaning (the Concord rule: at most five colours, plus text and ink).</summary>
public readonly record struct Palette(Color Deep, Color Signal, Color Sky, Color Accord, Color Lilac, Color Caution, Color Alert, Color Text);

public interface IKit
{
    string Name { get; }
    Palette C { get; }
    /// <summary>The names this skin can draw (pieces, hero pieces and icons).</summary>
    IReadOnlyCollection<string> Supplies { get; }
    /// <summary>A stretchable piece (frame, bar, button, tab, strip, cell, divider) drawn whole at the rectangle.</summary>
    void Piece(CanvasItem c, string piece, Rect2 r, float alpha = 1);
    /// <summary>A hero piece made whole at its size (name plate, helm pad, dial, lamps, crest).</summary>
    void Hero(CanvasItem c, string hero, Rect2 r, float alpha = 1);
    /// <summary>An icon in a square, inked in <paramref name="ink"/> (the icon guide: colour is for state only).</summary>
    void Icon(CanvasItem c, string icon, Rect2 r, Color ink);
    /// <summary>A frame piece's fixed margins in pixels: top, right, bottom, left.</summary>
    (float T, float R, float B, float L) Margins(string piece);
}

public static class KitPieces
{
    public static readonly string[] Pieces =
    {
        "frame.panel", "frame.inset", "bar.track", "bar.fill", "bar.fill.red", "bar.fill.accord",
        "button.idle", "button.hover", "button.pressed", "tab.idle", "tab.active", "strip.top", "strip.bottom", "divider",
        "cell.idle", "cell.lit", "cell.warn", "cell.accord", "cell.locked",
    };
    public static readonly string[] Heroes = { "nameplate", "helm.pad", "dial", "lamp.alert.green", "lamp.alert.yellow", "lamp.alert.red", "crest" };
    public static readonly string[] Icons =
    {
        "helm", "power", "tactical", "pd", "science", "comms", "repair", "transport", "fleet",
        "ship", "shields", "sensors", "tractor", "craft", "weapons", "engines", "reactor", "warp", "pause", "menu", "fire",
    };

    public static IEnumerable<string> All => Pieces.Concat(Heroes).Concat(Icons.Select(i => "icon." + i));

    /// <summary>Throws naming every piece the skin lacks.</summary>
    public static void Verify(IKit kit)
    {
        string[] missing = All.Where(n => !kit.Supplies.Contains(n)).ToArray();
        if (missing.Length > 0) throw new System.InvalidOperationException($"UI kit '{kit.Name}' lacks: {string.Join(", ", missing)}");
    }
}
