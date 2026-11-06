// IKit: the console's UI kit, one fixed list of named pieces that every screen draws by name (CLAUDE.md section 7: "One
// UI kit, swapped per faction"). A skin is one implementation of this interface; swapping the skin swaps the look and
// changes no layout or code. KitPieces lists the names, and Verify refuses a skin that lacks one (a missing piece is a
// build error, never a fallback), including the pieces of every optional feature its style declares.
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FleetCommand.Game.Ui.Kit;

/// <summary>A skin's colours, each with one meaning (the Concord rule: at most five colours, plus text and ink).</summary>
public readonly record struct Palette(Color Deep, Color Signal, Color Sky, Color Accord, Color Lilac, Color Caution, Color Alert, Color Text);

/// <summary>A stretchable piece's design: its size in pixels, its fixed margins (top, right, bottom, left), the inset of
/// what sits inside it (a bar track's groove) and the gap between repeated cells.</summary>
public readonly record struct PieceSpec(Vector2 Size, Pad M, Vector2 Inset, float Gap);

public interface IKit
{
    string Name { get; }
    Palette C { get; }
    /// <summary>Everything the skin draws differently besides its named pieces.</summary>
    SkinStyle Style { get; }
    /// <summary>The names this skin can draw (pieces, hero pieces and icons).</summary>
    IReadOnlyCollection<string> Supplies { get; }
    /// <summary>A stretchable piece (frame, bar, button, tab, strip, cell, divider, placard) drawn whole at the rectangle.</summary>
    void Piece(CanvasItem c, string piece, Rect2 r, float alpha = 1);
    /// <summary>A hero piece made whole at its size (name plate, helm pad, dial, lamps, crest, a rendered key).</summary>
    void Hero(CanvasItem c, string hero, Rect2 r, float alpha = 1);
    /// <summary>Only the bottom <paramref name="frac"/> of a rendered key (a mount's key lit from the bottom up as it
    /// charges). Called only when the style has rendered keys; a skin without them refuses it.</summary>
    void HeroBottom(CanvasItem c, string hero, Rect2 r, float frac);
    /// <summary>An icon in a square, inked in <paramref name="ink"/> (the icon guide: colour is for state only).</summary>
    void Icon(CanvasItem c, string icon, Rect2 r, Color ink);
    /// <summary>A stretchable piece's design (size, margins, inset, gap); throws for a name the skin lacks.</summary>
    PieceSpec Spec(string piece);
    /// <summary>A drawn key's shape (the mockup's rr): filled, or outlined when <paramref name="line"/> is above 0.
    /// <paramref name="radius"/> is the rounding a round skin would give it; another skin cuts its own shape that size.</summary>
    void Shape(CanvasItem c, Rect2 r, float radius, Color col, float line = 0);
    /// <summary>A small round knob in this skin's shape (the mockup's knob), filled.</summary>
    void Knob(CanvasItem c, Vector2 centre, float r, Color col);
    /// <summary>The glass of the square key as fractions of the key (X, Y, W, H): the part the skin lights, where a key's
    /// glyph and label go. All of a drawn key; the window inside a rendered key's frame.</summary>
    FleetCommand.Data.Rect KeyGlass { get; }
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

    /// <summary>The rendered key states, each a square key ("key.") and a wide one ("keyw.").</summary>
    public static readonly string[] KeyStates = { "idle", "hover", "on", "ok", "warn", "alert", "off" };

    /// <summary>The pieces each optional feature of a skin's style needs.</summary>
    public static IEnumerable<string> FeaturePieces(SkinStyle s)
    {
        if (s.Column) yield return "console.column";
        if (s.LabelPlacard) yield return "plate.label";
        if (s.TitlePlacard) yield return "plate.title";
        if (s.JoinedStrips) { yield return "strip.top.join"; yield return "strip.bottom.join"; }
        if (s.SymbolBars) foreach (string c in new[] { "on", "off", "warn", "ok" }) yield return "bar.cell." + c;
        if (s.RenderedKeys) foreach (string k in KeyStates) { yield return "key." + k; yield return "keyw." + k; }
    }

    public static IEnumerable<string> All => Pieces.Concat(Heroes).Concat(Icons.Select(i => "icon." + i));

    /// <summary>Throws naming every piece the skin lacks, the fixed list's and its declared features'.</summary>
    public static void Verify(IKit kit)
    {
        string[] missing = All.Concat(FeaturePieces(kit.Style)).Where(n => !kit.Supplies.Contains(n)).ToArray();
        if (missing.Length > 0) throw new System.InvalidOperationException($"UI kit '{kit.Name}' lacks: {string.Join(", ", missing)}");
    }
}
